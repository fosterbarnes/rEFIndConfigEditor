using Avalonia.Controls;
using Avalonia.Media.Imaging;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Views;

internal sealed partial class IconPickerWindow : Window
{
    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IReadOnlyList<RefindIconChoice> _choices;

    public string? SelectedPath { get; private set; }

    private IconPickerWindow(IReadOnlyList<RefindIconChoice> choices)
    {
        _choices = choices;
        InitializeComponent();
        AppIconLoader.TryApplyWindowIcon(this);
        PopulateList();
        OkButton.Click += (_, _) => ConfirmSelection();
        CancelButton.Click += (_, _) => Finish(false);
        IconList.DoubleTapped += (_, _) => ConfirmSelection();
        Closed += (_, _) =>
        {
            if (!_result.Task.IsCompleted)
                _result.TrySetResult(false);
        };
    }

    public static async Task<string?> PickAsync(
        Window owner,
        string? refindConfPath,
        RefindDocument? document)
    {
        var choices = RefindIconHelper.BuildChoices(refindConfPath, document);
        var window = new IconPickerWindow(choices);
        await window.ShowDialog(owner).ConfigureAwait(true);
        return window._result.Task.Result ? window.SelectedPath : null;
    }

    private void PopulateList()
    {
        var onDisk = _choices.Where(c => c.OnDisk).ToList();
        var catalog = _choices.Where(c => !c.OnDisk).ToList();

        var items = new List<IconListItem>();
        foreach (var choice in onDisk)
            items.Add(CreateItem(choice, "On this install"));
        foreach (var choice in catalog)
            items.Add(CreateItem(choice, "Standard rEFInd icons"));

        IconList.ItemsSource = items;
        if (items.Count > 0)
            IconList.SelectedIndex = 0;
    }

    private static IconListItem CreateItem(RefindIconChoice choice, string group)
    {
        Bitmap? thumb = null;
        if (RefindIconHelper.CanLoadThumbnail(choice.AbsolutePath))
        {
            try { thumb = new Bitmap(choice.AbsolutePath!); }
            catch { /* skip thumbnail */ }
        }

        var label = choice.CatalogOnly ? $"{choice.Label} (not on disk)" : choice.Label;
        return new IconListItem(group, label, choice.ConfigPath, thumb, choice);
    }

    private void ConfirmSelection()
    {
        if (IconList.SelectedItem is not IconListItem item)
            return;
        SelectedPath = item.ConfigPath;
        Finish(true);
    }

    private void Finish(bool accepted)
    {
        _result.TrySetResult(accepted);
        Close();
    }

    private sealed record IconListItem(
        string Group,
        string Label,
        string ConfigPath,
        Bitmap? Thumbnail,
        RefindIconChoice Choice)
    {
        public override string ToString() => $"{Label} — {ConfigPath}";
    }
}
