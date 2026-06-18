using Avalonia.Controls;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.Services;
using rEFIndConfigEditor.Views;

namespace rEFIndConfigEditor.UI;

internal static class BootPanelWiring
{
    public static void Wire(BootEntriesPanelHandles panel, RefindDocumentService documentService, Action markDirty)
    {
        panel.ImportButton.Click += async (_, _) =>
            await ImportFromLogAsync(panel, documentService, markDirty).ConfigureAwait(true);
        panel.AddButton.Click += async (_, _) =>
            await EditEntryAsync(null, panel, documentService, markDirty).ConfigureAwait(true);
        panel.EditButton.Click += async (_, _) =>
            await EditSelectedAsync(panel, documentService, markDirty).ConfigureAwait(true);
        panel.DuplicateButton.Click += (_, _) => Duplicate(panel, documentService, markDirty);
        panel.RemoveButton.Click += (_, _) => Remove(panel, documentService, markDirty);
        panel.MoveUpButton.Click += (_, _) => Move(panel, documentService, markDirty, -1);
        panel.MoveDownButton.Click += (_, _) => Move(panel, documentService, markDirty, 1);
        panel.SkipOtherEntries.IsCheckedChanged += (_, _) => markDirty();
    }

    private static async Task ImportFromLogAsync(
        BootEntriesPanelHandles panel,
        RefindDocumentService documentService,
        Action markDirty)
    {
        var owner = DialogHost.GetOwner();
        if (owner is null)
            return;

        var logPath = await RefindLogPickerHelper.PickLogPathAsync(documentService.FilePath).ConfigureAwait(true);
        if (logPath is null)
            return;

        try
        {
            var candidates = RefindLogParser.Parse(RefindLogIO.ReadText(logPath));
            var selected = await RefindLogImportWindow.PickBootCandidatesAsync(owner, candidates).ConfigureAwait(true);
            if (selected.Count == 0)
                return;

            if (selected.Count == 1)
            {
                await EditEntryAsync(selected[0].ToMenuEntry(), panel, documentService, markDirty).ConfigureAwait(true);
                return;
            }

            foreach (var c in selected)
                documentService.Document.Rows.Add(new MenuEntryRow(c.ToMenuEntry()));
            panel.RefreshList(documentService.Document);
            markDirty();
        }
        catch (Exception ex)
        {
            PlatformServices.Current.ShowError(AppBranding.DisplayName, ex.Message);
        }
    }

    private static async Task EditSelectedAsync(
        BootEntriesPanelHandles panel,
        RefindDocumentService documentService,
        Action markDirty)
    {
        if (panel.BootList.SelectedIndex < 0)
            return;

        var entry = documentService.Document.MenuEntries.ElementAt(panel.BootList.SelectedIndex);
        await EditEntryAsync(entry, panel, documentService, markDirty).ConfigureAwait(true);
    }

    private static async Task EditEntryAsync(
        MenuEntry? entry,
        BootEntriesPanelHandles panel,
        RefindDocumentService documentService,
        Action markDirty)
    {
        var owner = DialogHost.GetOwner();
        if (owner is null)
            return;

        var isNew = entry is null || !documentService.Document.MenuEntries.Contains(entry);
        var result = await BootEntryEditorWindow.EditAsync(
            owner,
            entry,
            () => documentService.FilePath,
            () => documentService.Document,
            isNew).ConfigureAwait(true);

        if (result is null)
            return;

        if (isNew)
            documentService.Document.Rows.Add(new MenuEntryRow(result));

        panel.RefreshList(documentService.Document);
        markDirty();
    }

    private static void Duplicate(
        BootEntriesPanelHandles panel,
        RefindDocumentService documentService,
        Action markDirty)
    {
        if (panel.BootList.SelectedIndex < 0)
            return;

        var src = documentService.Document.MenuEntries.ElementAt(panel.BootList.SelectedIndex);
        documentService.Document.Rows.Add(new MenuEntryRow(CloneEntry(src)));
        panel.RefreshList(documentService.Document);
        markDirty();
    }

    private static void Remove(
        BootEntriesPanelHandles panel,
        RefindDocumentService documentService,
        Action markDirty)
    {
        if (panel.BootList.SelectedIndex < 0)
            return;

        var entry = documentService.Document.MenuEntries.ElementAt(panel.BootList.SelectedIndex);
        documentService.Document.Rows.RemoveAll(r => r is MenuEntryRow m && ReferenceEquals(m.Entry, entry));
        panel.RefreshList(documentService.Document);
        markDirty();
    }

    private static void Move(
        BootEntriesPanelHandles panel,
        RefindDocumentService documentService,
        Action markDirty,
        int delta)
    {
        var i = panel.BootList.SelectedIndex;
        if (i < 0)
            return;

        var j = i + delta;
        var menuRows = documentService.Document.Rows.OfType<MenuEntryRow>().ToList();
        if (j < 0 || j >= menuRows.Count)
            return;

        var all = documentService.Document.Rows.ToList();
        var idxA = all.IndexOf(menuRows[i]);
        var idxB = all.IndexOf(menuRows[j]);
        (all[idxA], all[idxB]) = (all[idxB], all[idxA]);
        documentService.Document.Rows.Clear();
        documentService.Document.Rows.AddRange(all);
        panel.RefreshList(documentService.Document);
        panel.BootList.SelectedIndex = j;
        markDirty();
    }

    private static MenuEntry CloneEntry(MenuEntry src)
    {
        var e = new MenuEntry { Title = src.Title + " (copy)", Disabled = src.Disabled };
        foreach (var (k, v) in src.Fields)
            e.Fields[k] = v.Copy();
        e.Trailers.AddRange(src.Trailers);
        foreach (var sub in src.Submenus)
        {
            var s = new SubmenuEntry { Title = sub.Title, Disabled = sub.Disabled };
            foreach (var (k, v) in sub.Fields)
                s.Fields[k] = v.Copy();
            s.Trailers.AddRange(sub.Trailers);
            e.Submenus.Add(s);
        }
        return e;
    }
}
