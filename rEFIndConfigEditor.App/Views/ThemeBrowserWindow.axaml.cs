using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Views;

public partial class ThemeBrowserWindow : Window
{
    private const int PageSize = 15;

    private readonly Func<string?>? _getRefindConfPath;
    private readonly IReadOnlyList<ThemeCatalogEntry> _allEntries;
    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(150) };

    private IReadOnlyList<ThemeCatalogEntry> _visibleEntries = [];
    private int _pageIndex;

    public event EventHandler? ThemeDownloaded;

    public ThemeBrowserWindow() : this(null) { }

    public ThemeBrowserWindow(Func<string?>? getRefindConfPath = null)
    {
        _getRefindConfPath = getRefindConfPath;
        _allEntries = ThemeCatalog.Load();
        InitializeComponent();
        AppIconLoader.TryApplyWindowIcon(this);

        foreach (ThemeBrowserCategory category in Enum.GetValues<ThemeBrowserCategory>())
            CategoryFilter.Items.Add(ThemeCatalogQuery.CategoryLabel(category));
        CategoryFilter.SelectedIndex = 0;

        foreach (ThemeBrowserSort sort in Enum.GetValues<ThemeBrowserSort>())
            SortBy.Items.Add(ThemeCatalogQuery.SortLabel(sort));
        SortBy.SelectedIndex = 0;

        CategoryFilter.SelectionChanged += (_, _) => ApplyFilters(resetPage: true);
        SortBy.SelectionChanged += (_, _) => ApplyFilters(resetPage: true);
        PrevButton.Click += (_, _) => ShowPage(_pageIndex - 1);
        NextButton.Click += (_, _) => ShowPage(_pageIndex + 1);

        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            UpdateSearch();
        };
        SearchBox.TextChanged += (_, _) =>
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        };
        SearchResults.SelectionChanged += (_, _) =>
        {
            if (SearchResults.SelectedItem is ThemeSearchHit hit)
            {
                SearchPopup.IsOpen = false;
                SearchBox.Text = "";
                NavigateToTheme(hit.Entry);
                SearchResults.SelectedItem = null;
            }
        };

        ApplyFilters(resetPage: false);
        ShowPage(0);
    }

    private int PageCount =>
        _visibleEntries.Count == 0 ? 1 : (_visibleEntries.Count + PageSize - 1) / PageSize;

    private void ApplyFilters(bool resetPage)
    {
        var category = (ThemeBrowserCategory)(CategoryFilter.SelectedIndex < 0 ? 0 : CategoryFilter.SelectedIndex);
        var sort = (ThemeBrowserSort)(SortBy.SelectedIndex < 0 ? 0 : SortBy.SelectedIndex);
        _visibleEntries = ThemeCatalogQuery.FilterAndSort(_allEntries, category, sort);
        if (resetPage)
            ShowPage(0);
        else
            UpdatePager();
    }

    private void ShowPage(int page)
    {
        CardsPanel.Children.Clear();
        if (_visibleEntries.Count == 0)
        {
            UpdatePager();
            return;
        }

        page = Math.Clamp(page, 0, PageCount - 1);
        _pageIndex = page;
        CardsScroll.Offset = new Avalonia.Vector(0, 0);

        var start = page * PageSize;
        var end = Math.Min(start + PageSize, _visibleEntries.Count);
        for (var i = start; i < end; i++)
            CardsPanel.Children.Add(BuildCard(_visibleEntries[i]));

        UpdatePager();
    }

    private void UpdatePager()
    {
        if (_visibleEntries.Count == 0)
        {
            PageLabel.Text = _allEntries.Count == 0
                ? "No themes in catalog"
                : "No themes match the current filters";
            PrevButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            return;
        }

        var filterNote = _visibleEntries.Count == _allEntries.Count
            ? $"{_allEntries.Count} themes"
            : $"{_visibleEntries.Count} of {_allEntries.Count} themes";
        PageLabel.Text = $"Page {_pageIndex + 1} of {PageCount} · {filterNote}";
        PrevButton.IsEnabled = _pageIndex > 0;
        NextButton.IsEnabled = _pageIndex < PageCount - 1;
    }

    private Control BuildCard(ThemeCatalogEntry entry)
    {
        var card = new Border
        {
            Padding = new Avalonia.Thickness(8),
            Classes = { "setting-input-frame" },
        };

        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 8,
        };

        var preview = new Image
        {
            Width = 320,
            Height = 180,
            Stretch = Avalonia.Media.Stretch.Uniform,
        };
        var previewPath = ThemeCatalog.PreviewPath(entry);
        if (previewPath is not null)
            LoadPreviewAsync(preview, previewPath);

        var textHost = new StackPanel { Spacing = 6 };
        textHost.Children.Add(new TextBlock
        {
            Text = entry.Name,
            Classes = { "setting-label" },
            FontWeight = Avalonia.Media.FontWeight.Bold,
        });
        textHost.Children.Add(new TextBlock
        {
            Text = BuildMetaLine(entry),
            Opacity = 0.75,
        });
        textHost.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(entry.Description) ? " " : entry.Description,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxHeight = 96,
        });
        textHost.Children.Add(UiLinks.Command(entry.Github, new CommunityToolkit.Mvvm.Input.RelayCommand(
            () => PlatformServices.Current.OpenUrl(entry.Github))));

        var refindPath = _getRefindConfPath?.Invoke();
        var canDownload = !string.IsNullOrWhiteSpace(refindPath);
        var download = new Button
        {
            Content = "Download",
            HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = canDownload,
            Margin = new Avalonia.Thickness(0, 6, 0, 0),
        };
        if (!canDownload)
            ToolTip.SetTip(download, "Open a refind.conf file first.");
        download.Click += async (_, _) => await DownloadThemeAsync(entry, download).ConfigureAwait(true);
        textHost.Children.Add(download);

        Grid.SetColumn(preview, 0);
        Grid.SetColumn(textHost, 1);
        layout.Children.Add(preview);
        layout.Children.Add(textHost);
        card.Child = layout;
        card.Tag = entry.Id;
        return card;
    }

    private static string BuildMetaLine(ThemeCatalogEntry entry)
    {
        var parts = new List<string>();
        if (entry.GithubStars > 0)
            parts.Add($"★ {entry.GithubStars:N0}");
        if (entry.Created != default)
            parts.Add(entry.Created.ToString("yyyy-MM-dd"));

        var traits = new List<string>();
        if (entry.IsDark) traits.Add("Dark");
        if (entry.IsLight) traits.Add("Light");
        if (entry.IsMinimal) traits.Add("Minimal");
        if (traits.Count > 0)
            parts.Add(string.Join(" · ", traits));

        return parts.Count == 0 ? " " : string.Join(" · ", parts);
    }

    private async Task DownloadThemeAsync(ThemeCatalogEntry entry, Button downloadBtn)
    {
        var refindPath = _getRefindConfPath?.Invoke();
        if (string.IsNullOrWhiteSpace(refindPath))
        {
            PlatformServices.Current.ShowWarning("Download theme",
                "Open or save a refind.conf file first so the theme can be installed under its rEFInd directory.");
            return;
        }

        downloadBtn.IsEnabled = false;
        var priorText = downloadBtn.Content?.ToString() ?? "Download";
        downloadBtn.Content = "Downloading…";

        try
        {
            var result = await ThemeInstaller.InstallAsync(entry, refindPath).ConfigureAwait(true);
            if (!result.Success)
            {
                PlatformServices.Current.ShowWarning("Download theme", result.Error ?? "Download failed.");
                return;
            }

            ThemeDownloaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            PlatformServices.Current.ShowWarning("Download theme", ex.Message);
        }
        finally
        {
            downloadBtn.Content = priorText;
            downloadBtn.IsEnabled = true;
        }
    }

    private static void LoadPreviewAsync(Image preview, string path)
    {
        Task.Run(() =>
        {
            try
            {
                using var stream = File.OpenRead(path);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }).ContinueWith(t =>
        {
            if (t.Result is null)
                return;
            Dispatcher.UIThread.Post(() => preview.Source = t.Result);
        });
    }

    private void UpdateSearch()
    {
        var q = SearchBox.Text;
        if (string.IsNullOrWhiteSpace(q))
        {
            SearchPopup.IsOpen = false;
            return;
        }

        var hits = ThemeNameSearch.Find(_allEntries, q);
        if (hits.Count == 0)
        {
            SearchPopup.IsOpen = false;
            return;
        }

        SearchResults.ItemsSource = hits;
        SearchResults.SelectedIndex = -1;
        SearchPopup.IsOpen = true;
    }

    private void NavigateToTheme(ThemeCatalogEntry entry)
    {
        if (IndexOfEntry(_visibleEntries, entry) < 0)
        {
            CategoryFilter.SelectedIndex = 0;
            ApplyFilters(resetPage: false);
        }

        var index = IndexOfEntry(_visibleEntries, entry);
        if (index < 0)
            return;

        ShowPage(index / PageSize);
        ScrollToThemeCard(entry.Id);
    }

    private static int IndexOfEntry(IReadOnlyList<ThemeCatalogEntry> entries, ThemeCatalogEntry entry)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].Id, entry.Id, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    private void ScrollToThemeCard(string entryId)
    {
        foreach (var child in CardsPanel.Children)
        {
            if (child is Border { Tag: string id } && string.Equals(id, entryId, StringComparison.Ordinal))
            {
                child.BringIntoView();
                return;
            }
        }
    }
}
