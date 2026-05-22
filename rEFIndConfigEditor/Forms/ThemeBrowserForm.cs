using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Forms;

internal sealed class ThemeBrowserForm : Form
{
    private const int PageSize = 15;

    private readonly int _baselineDpi;
    private readonly Func<string?>? _getRefindConfPath;
    private readonly IReadOnlyList<ThemeCatalogEntry> _allEntries;
    private readonly Panel _filterRow = new() { Dock = DockStyle.Top };
    private readonly Panel _searchHost = new() { Dock = DockStyle.Right };
    private readonly TextBox _searchBox = new();
    private readonly ToolStripDropDown _searchDrop = new()
    {
        AutoClose = false,
        Padding = Padding.Empty,
        DropShadowEnabled = true,
    };
    private readonly ListBox _searchResults = new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false,
        TabStop = false,
    };
    private ToolStripControlHost _searchResultsHost = null!;
    private readonly FlowLayoutPanel _filterBar;
    private readonly ComboBox _categoryFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _sortBy = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Panel _scrollHost;
    private readonly TableLayoutPanel _cards;
    private readonly Panel _footer;
    private readonly Label _pageLabel = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _prev = new() { Text = "Previous", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _next = new() { Text = "Next", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly ToolTip _toolTip = new();
    private readonly System.Windows.Forms.Timer _searchDebounceTimer = new() { Interval = 150 };

    private IReadOnlyList<ThemeCatalogEntry> _visibleEntries = [];
    private int _pageIndex;
    private bool _pageShown;

    public event EventHandler? ThemeDownloaded;

    public ThemeBrowserForm(int dpi, Func<string?>? getRefindConfPath = null)
    {
        _baselineDpi = dpi;
        _getRefindConfPath = getRefindConfPath;
        _allEntries = ThemeCatalog.Load();

        AppFormIcon.Apply(this);
        Text = "Theme browser";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.Manual;

        _filterBar = new FlowLayoutPanel
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        BuildSearch();
        _searchHost.Controls.Add(_searchBox);
        _filterRow.Controls.Add(_searchHost);
        _filterRow.Controls.Add(_filterBar);
        _filterRow.Resize += (_, _) => LayoutFilterSearch();

        var categoryLabel = new Label { Text = "Category:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        categoryLabel.Margin = new Padding(0, 6, 0, 0);
        _categoryFilter.Margin = new Padding(0, 0, 12, 0);
        foreach (ThemeBrowserCategory category in Enum.GetValues<ThemeBrowserCategory>())
            _categoryFilter.Items.Add(CategoryItem.From(category));
        _categoryFilter.SelectedIndex = 0;
        _categoryFilter.SelectedIndexChanged += (_, _) => ApplyFilters(resetPage: true);

        var sortLabel = new Label { Text = "Sort by:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        sortLabel.Margin = new Padding(0, 6, 0, 0);
        _sortBy.Margin = new Padding(0, 0, 0, 0);
        foreach (ThemeBrowserSort sort in Enum.GetValues<ThemeBrowserSort>())
            _sortBy.Items.Add(SortItem.From(sort));
        _sortBy.SelectedIndex = 0;
        _sortBy.SelectedIndexChanged += (_, _) => ApplyFilters(resetPage: true);

        _filterBar.Controls.AddRange([categoryLabel, _categoryFilter, sortLabel, _sortBy]);

        _cards = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top
        };
        _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        _scrollHost.Controls.Add(_cards);

        _prev.Click += (_, _) => ShowPage(_pageIndex - 1);
        _next.Click += (_, _) => ShowPage(_pageIndex + 1);

        _footer = new Panel { Dock = DockStyle.Bottom };
        _footer.Controls.AddRange([_pageLabel, _prev, _next]);
        _footer.Resize += (_, _) => LayoutFooter();

        Controls.Add(_scrollHost);
        Controls.Add(_footer);
        Controls.Add(_filterRow);

        DpiChanged += (_, e) => ApplyLayoutMetrics(e.DeviceDpiNew);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyLayoutMetrics(DeviceDpi > 0 ? DeviceDpi : _baselineDpi);
        FormPlacement.CenterOnDisplay(this, Owner as Control);
        ApplyFilters(resetPage: false);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_pageShown)
            return;
        _pageShown = true;
        ShowPage(0);
    }

    private int PageCount =>
        _visibleEntries.Count == 0 ? 1 : (_visibleEntries.Count + PageSize - 1) / PageSize;

    private void ApplyFilters(bool resetPage)
    {
        var category = SelectedCategory();
        var sort = SelectedSort();
        _visibleEntries = ThemeCatalogQuery.FilterAndSort(_allEntries, category, sort);
        if (resetPage)
            ShowPage(0);
        else
            UpdatePager();
    }

    private ThemeBrowserCategory SelectedCategory()
    {
        if (_categoryFilter.SelectedItem is CategoryItem item)
            return item.Category;
        return ThemeBrowserCategory.All;
    }

    private ThemeBrowserSort SelectedSort()
    {
        if (_sortBy.SelectedItem is SortItem item)
            return item.Sort;
        return ThemeBrowserSort.NameAsc;
    }

    private void ShowPage(int page)
    {
        if (_visibleEntries.Count == 0)
        {
            ClearCards();
            UpdatePager();
            return;
        }

        page = Math.Clamp(page, 0, PageCount - 1);
        _pageIndex = page;
        ClearCards();
        _scrollHost.AutoScrollPosition = new Point(0, 0);

        var start = page * PageSize;
        var end = Math.Min(start + PageSize, _visibleEntries.Count);
        for (var i = start; i < end; i++)
            AddThemeCard(_visibleEntries[i]);

        UpdatePager();
    }

    private void UpdatePager()
    {
        if (_visibleEntries.Count == 0)
        {
            _pageLabel.Text = _allEntries.Count == 0
                ? "No themes in catalog"
                : "No themes match the current filters";
            _prev.Enabled = false;
            _next.Enabled = false;
            return;
        }

        var filterNote = _visibleEntries.Count == _allEntries.Count
            ? $"{_allEntries.Count} themes"
            : $"{_visibleEntries.Count} of {_allEntries.Count} themes";
        _pageLabel.Text = $"Page {_pageIndex + 1} of {PageCount} · {filterNote}";
        _prev.Enabled = _pageIndex > 0;
        _next.Enabled = _pageIndex < PageCount - 1;
    }

    private void ClearCards()
    {
        DisposeCardImages(_cards);
        for (var i = _cards.Controls.Count - 1; i >= 0; i--)
        {
            var c = _cards.Controls[i];
            _cards.Controls.RemoveAt(i);
            c.Dispose();
        }
        _cards.RowStyles.Clear();
        _cards.RowCount = 0;
    }

    private void AddThemeCard(ThemeCatalogEntry entry)
    {
        var rowIndex = _cards.RowCount;
        _cards.RowCount++;
        _cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _cards.Controls.Add(BuildCard(entry), 0, rowIndex);
    }

    private Panel BuildCard(ThemeCatalogEntry entry)
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : _baselineDpi;
        var thumbW = UiMetrics.Scale(320, dpi);
        var thumbH = UiMetrics.Scale(180, dpi);
        var pad = UiMetrics.Scale(8, dpi);

        var card = new Panel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(pad),
            Margin = new Padding(0, 0, 0, pad),
            Tag = entry.Id
        };

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Top
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, thumbW + pad));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var preview = new PictureBox
        {
            Size = new Size(thumbW, thumbH),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.None,
            Margin = new Padding(0, 0, pad, 0)
        };

        var previewPath = ThemeCatalog.PreviewPath(entry);
        if (previewPath is not null)
            LoadPreviewAsync(preview, previewPath);

        var textHost = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };
        textHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var baseFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
        var titleSize = Math.Max(13f, baseFont.SizeInPoints * 1.35f);
        var descSize = Math.Max(11f, baseFont.SizeInPoints * 1.15f);
        var metaSize = Math.Max(10f, baseFont.SizeInPoints * 0.95f);
        var textWidth = UiMetrics.Scale(420, dpi);

        var name = new Label
        {
            Text = entry.Name,
            AutoSize = true,
            Font = new Font(baseFont.FontFamily, titleSize, FontStyle.Bold),
            MaximumSize = new Size(textWidth, 0)
        };

        var meta = new Label
        {
            Text = BuildMetaLine(entry),
            AutoSize = true,
            Font = new Font(baseFont.FontFamily, metaSize, baseFont.Style),
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(textWidth, 0)
        };

        var description = new Label
        {
            Text = string.IsNullOrWhiteSpace(entry.Description) ? " " : entry.Description,
            AutoSize = true,
            Font = new Font(baseFont.FontFamily, descSize, baseFont.Style),
            MaximumSize = new Size(textWidth, UiMetrics.Scale(96, dpi))
        };

        var github = new LinkLabel
        {
            Text = entry.Github,
            AutoSize = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            MaximumSize = new Size(textWidth, 0)
        };
        github.LinkArea = new LinkArea(0, github.Text.Length);
        github.LinkClicked += (_, _) => TokenDocLinks.OpenInBrowser(entry.Github);

        var refindPath = _getRefindConfPath?.Invoke();
        var canDownload = !string.IsNullOrWhiteSpace(refindPath);
        var download = new Button
        {
            Text = "Download",
            AutoSize = true,
            Enabled = canDownload,
            Margin = new Padding(0, UiMetrics.Scale(6, dpi), 0, 0)
        };
        download.Height = UiMetrics.ButtonHeight(dpi);
        if (!canDownload)
            _toolTip.SetToolTip(download, "Open a refind.conf file first.");
        download.Click += (_, _) => DownloadTheme(entry, download);

        textHost.Controls.Add(name, 0, 0);
        textHost.Controls.Add(meta, 0, 1);
        textHost.Controls.Add(description, 0, 2);
        textHost.Controls.Add(github, 0, 3);
        textHost.Controls.Add(download, 0, 4);

        layout.Controls.Add(preview, 0, 0);
        layout.Controls.Add(textHost, 1, 0);
        card.Controls.Add(layout);

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

    private void DownloadTheme(ThemeCatalogEntry entry, Button downloadBtn)
    {
        var refindPath = _getRefindConfPath?.Invoke();
        if (string.IsNullOrWhiteSpace(refindPath))
        {
            MessageBox.Show(this,
                "Open or save a refind.conf file first so the theme can be installed under its rEFInd directory.",
                "Download theme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        downloadBtn.Enabled = false;
        var priorText = downloadBtn.Text;
        downloadBtn.Text = "Downloading…";

        Task.Run(async () => await ThemeInstaller.InstallAsync(entry, refindPath))
            .ContinueWith(t =>
            {
                if (IsDisposed || downloadBtn.IsDisposed)
                    return;

                downloadBtn.Text = priorText;
                downloadBtn.Enabled = true;

                if (t.IsFaulted)
                {
                    var message = t.Exception?.GetBaseException().Message ?? "Download failed.";
                    MessageBox.Show(this, message, "Download theme",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = t.Result;
                if (!result.Success)
                {
                    MessageBox.Show(this, result.Error, "Download theme",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ThemeDownloaded?.Invoke(this, EventArgs.Empty);
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void LoadPreviewAsync(PictureBox preview, string path)
    {
        Task.Run(() =>
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var temp = Image.FromStream(stream);
                return (Image)new Bitmap(temp);
            }
            catch
            {
                return null;
            }
        }).ContinueWith(t =>
        {
            if (IsDisposed || preview.IsDisposed || preview.FindForm() is null)
            {
                t.Result?.Dispose();
                return;
            }

            if (t.Result is null)
                return;

            var prior = preview.Image;
            preview.Image = t.Result;
            prior?.Dispose();
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void BuildSearch()
    {
        _searchBox.PlaceholderText = "Search";

        _searchResults.DisplayMember = nameof(ThemeSearchHit.DisplayText);
        _searchResults.Click += (_, _) => PickSearchResult();
        _searchResultsHost = new ToolStripControlHost(_searchResults)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        _searchDrop.Items.Add(_searchResultsHost);

        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            UpdateSearch();
        };
        _searchBox.TextChanged += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        };
        _searchBox.LostFocus += (_, _) => BeginInvoke(() =>
        {
            if (IsDisposed || Disposing)
                return;
            if (!_searchResults.Focused)
                _searchDrop.Close();
        });
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                _searchDrop.Close();
                e.Handled = true;
            }
        };
    }

    private void UpdateSearch()
    {
        var q = _searchBox.Text;
        if (string.IsNullOrWhiteSpace(q))
        {
            _searchDrop.Close();
            return;
        }

        var hits = ThemeNameSearch.Find(_allEntries, q);
        if (hits.Count == 0)
        {
            _searchDrop.Close();
            return;
        }

        _searchResults.BeginUpdate();
        _searchResults.Items.Clear();
        foreach (var h in hits)
            _searchResults.Items.Add(h);
        _searchResults.SelectedIndex = -1;
        _searchResults.EndUpdate();

        var dpi = DeviceDpi > 0 ? DeviceDpi : _baselineDpi;
        var itemHeight = Math.Max(_searchResults.ItemHeight, 16);
        int w = Math.Max(_searchBox.Width, UiMetrics.Scale(360, dpi));
        int dropH = Math.Min(UiMetrics.Scale(240, dpi), hits.Count * itemHeight + 4);
        _searchResultsHost.Size = new Size(w, dropH);

        if (!_searchDrop.Visible)
            _searchDrop.Show(_searchBox, new Point(0, _searchBox.Height));
    }

    private void PickSearchResult()
    {
        if (_searchResults.SelectedItem is not ThemeSearchHit hit)
            return;

        _searchDrop.Close();
        _searchBox.Text = "";
        NavigateToTheme(hit.Entry);
    }

    private void NavigateToTheme(ThemeCatalogEntry entry)
    {
        if (IndexOfEntry(_visibleEntries, entry) < 0)
        {
            _categoryFilter.SelectedIndex = 0;
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
        foreach (Control c in _cards.Controls)
        {
            if (c is Panel { Tag: string id } && string.Equals(id, entryId, StringComparison.Ordinal))
            {
                _scrollHost.ScrollControlIntoView(c);
                return;
            }
        }
    }

    private void ApplyLayoutMetrics(int dpi)
    {
        ClientSize = UiMetrics.ScaleSize(960, 760, dpi);
        MinimumSize = UiMetrics.ScaleSize(640, 520, dpi);

        var filterPad = UiMetrics.ScalePadding(8, 6, dpi);
        _filterRow.Padding = filterPad;
        _filterRow.Height = filterPad.Vertical + UiMetrics.ControlHeight(dpi);

        var margin = UiMetrics.Scale(6, dpi);
        _searchBox.Size = UiMetrics.SearchBoxSize(dpi);
        _searchHost.Width = _searchBox.Width + margin * 2;

        _categoryFilter.Width = UiMetrics.Scale(220, dpi);
        _sortBy.Width = UiMetrics.Scale(180, dpi);
        _categoryFilter.Height = UiMetrics.ControlHeight(dpi);
        _sortBy.Height = UiMetrics.ControlHeight(dpi);

        _footer.Height = UiMetrics.StatusFooterHeight(dpi);
        _footer.Padding = UiMetrics.StatusFooterPadding(dpi);
        _pageLabel.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

        LayoutFilterSearch();
        LayoutFooter();
    }

    private void LayoutFilterSearch()
    {
        if (_searchHost.ClientSize.Height <= 0)
            return;

        var dpi = DeviceDpi > 0 ? DeviceDpi : _baselineDpi;
        var margin = UiMetrics.Scale(6, dpi);
        var rowH = _searchHost.ClientSize.Height;
        _searchBox.Location = new Point(margin, Math.Max(0, (rowH - _searchBox.Height) / 2));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            _searchBox.Focus();
            _searchBox.SelectAll();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void LayoutFooter()
    {
        if (_footer.ClientSize.Width <= 0 || _footer.ClientSize.Height <= 0)
            return;

        var dpi = DeviceDpi > 0 ? DeviceDpi : _baselineDpi;
        var btnW = UiMetrics.ToolbarFileButtonWidth(dpi);
        var btnH = UiMetrics.ButtonHeight(dpi);
        var gap = UiMetrics.Scale(UiMetrics.StatusFooterButtonGapPx, dpi);

        var innerW = _footer.ClientSize.Width;
        var innerH = _footer.ClientSize.Height;
        var y = Math.Max(0, (innerH - btnH) / 2);

        _next.SetBounds(innerW - btnW, y, btnW, btnH);
        _prev.SetBounds(_next.Left - gap - btnW, y, btnW, btnH);
        _pageLabel.SetBounds(0, 0, Math.Max(0, _prev.Left - gap), innerH);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearCards();
            _searchDebounceTimer.Dispose();
            _searchDrop.Dispose();
            _toolTip.Dispose();
        }
        base.Dispose(disposing);
    }

    private static void DisposeCardImages(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is PictureBox { Image: not null } pb)
            {
                pb.Image.Dispose();
                pb.Image = null;
            }
            DisposeCardImages(c);
        }
    }

    private sealed class CategoryItem(ThemeBrowserCategory category)
    {
        public ThemeBrowserCategory Category { get; } = category;

        public static CategoryItem From(ThemeBrowserCategory category) => new(category);

        public override string ToString() => ThemeCatalogQuery.CategoryLabel(Category);
    }

    private sealed class SortItem(ThemeBrowserSort sort)
    {
        public ThemeBrowserSort Sort { get; } = sort;

        public static SortItem From(ThemeBrowserSort sort) => new(sort);

        public override string ToString() => ThemeCatalogQuery.SortLabel(Sort);
    }
}
