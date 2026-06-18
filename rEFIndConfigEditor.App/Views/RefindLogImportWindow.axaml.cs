using System.Collections;
using Avalonia.Controls;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Views;

internal enum RefindLogImportPurpose
{
    BootEntries,
    DefaultSelection,
    Volumes
}

internal sealed partial class RefindLogImportWindow : Window
{
    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly RefindLogImportPurpose _purpose;
    private readonly IReadOnlyList<RefindLogBootCandidate> _candidates;
    private readonly string _pickPrompt;

    public IReadOnlyList<RefindLogBootCandidate> SelectedCandidates { get; private set; } = [];
    public IReadOnlyList<string> SelectedVolumes { get; private set; } = [];

    private RefindLogImportWindow(
        RefindLogImportPurpose purpose,
        IReadOnlyList<RefindLogBootCandidate> candidates,
        string pickPrompt)
    {
        _purpose = purpose;
        _candidates = candidates;
        _pickPrompt = pickPrompt;
        InitializeComponent();
        AppIconLoader.TryApplyWindowIcon(this);
        CancelButton.Click += (_, _) => Finish(false);
        AcceptButton.Click += (_, _) =>
        {
            if (ValidateSelection())
                Finish(true);
        };
        ItemList.DoubleTapped += (_, _) =>
        {
            if (ValidateSelection())
                Finish(true);
        };
        Closed += (_, _) =>
        {
            if (!_result.Task.IsCompleted)
                _result.TrySetResult(false);
        };
    }

    public static async Task<IReadOnlyList<string>> PickVolumesAsync(
        Window owner,
        IReadOnlyList<string> items,
        string windowTitle,
        string hint,
        string acceptText = "Add",
        string? pickPrompt = null)
    {
        var window = new RefindLogImportWindow(
            RefindLogImportPurpose.Volumes,
            [],
            pickPrompt ?? "Select at least one item.")
        {
            Title = windowTitle,
        };
        window.HintText.Text = hint;
        window.AcceptButton.Content = acceptText;
        window.ItemList.ItemsSource = items.ToList();
        if (items.Count > 0)
            window.ItemList.SelectedIndex = 0;
        await window.ShowDialog(owner).ConfigureAwait(true);
        return window._result.Task.Result ? window.SelectedVolumes : [];
    }

    public static async Task<IReadOnlyList<RefindLogBootCandidate>> PickBootCandidatesAsync(
        Window owner,
        IReadOnlyList<RefindLogBootCandidate> candidates,
        RefindLogImportPurpose purpose = RefindLogImportPurpose.BootEntries)
    {
        var single = purpose == RefindLogImportPurpose.DefaultSelection;
        var window = new RefindLogImportWindow(purpose, candidates, "")
        {
            Title = single ? "Choose default from refind.log" : "Import from refind.log",
        };
        window.HintText.Text = single
            ? "Config boot entries are listed first ([Manual Entry]), then loaders from the log. Select one for the default boot choice:"
            : "Select one or more boot loaders discovered in the log:";
        window.AcceptButton.Content = single ? "Use" : "Create entries";
        window.ItemList.SelectionMode = single ? SelectionMode.Single : SelectionMode.Multiple;
        window.ItemList.ItemsSource = candidates.Select(c => c.Summary).ToList();
        if (candidates.Count > 0)
            window.ItemList.SelectedIndex = 0;
        await window.ShowDialog(owner).ConfigureAwait(true);
        return window._result.Task.Result ? window.SelectedCandidates : [];
    }

    private void Finish(bool accepted)
    {
        _result.TrySetResult(accepted);
        Close();
    }

    private bool ValidateSelection()
    {
        if (ItemList.SelectedItems is null || ItemList.SelectedItems.Count == 0)
        {
            PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName, PickPrompt());
            return false;
        }

        switch (_purpose)
        {
            case RefindLogImportPurpose.Volumes:
            {
                var volumes = ItemList.SelectedItems
                    .OfType<string>()
                    .Where(v => v.Length > 0)
                    .ToList();
                if (volumes.Count == 0)
                {
                    PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName, PickPrompt());
                    return false;
                }
                SelectedVolumes = volumes;
                return true;
            }
            default:
            {
                var selected = new List<RefindLogBootCandidate>();
                if (ItemList.ItemsSource is IList sourceList && ItemList.SelectedItems is not null)
                {
                    foreach (var item in ItemList.SelectedItems)
                    {
                        var idx = sourceList.IndexOf(item);
                        if (idx >= 0 && idx < _candidates.Count)
                            selected.Add(_candidates[idx]);
                    }
                }
                if (selected.Count == 0)
                {
                    PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName, PickPrompt());
                    return false;
                }
                if (_purpose == RefindLogImportPurpose.DefaultSelection && selected.Count != 1)
                {
                    PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName,
                        "Select exactly one boot loader entry.");
                    return false;
                }
                SelectedCandidates = selected;
                return true;
            }
        }
    }

    private string PickPrompt() => _purpose switch
    {
        RefindLogImportPurpose.Volumes => _pickPrompt,
        RefindLogImportPurpose.DefaultSelection => "Select a boot loader entry.",
        _ => "Select at least one boot loader entry."
    };
}
