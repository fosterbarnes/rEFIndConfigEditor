using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Storage;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Services;

internal sealed class RefindDocumentService
{
    public RefindDocument Document { get; private set; } = new();
    public string? FilePath { get; private set; }
    public bool IsDirty { get; private set; }
    public bool RawEdited { get; private set; }
    public bool GuiEdited { get; private set; }

    public bool NeedsSaveConflictResolution => RawEdited && GuiEdited;

    public bool HasOpenDocument => !string.IsNullOrEmpty(FilePath) || IsDirty;

    public void NewDocument()
    {
        Document = new RefindDocument();
        Document.Rows.Add(new RawConfigRow("# rEFInd configuration — open refind.conf or edit below"));
        FilePath = null;
        SetDirty(false);
        RawEdited = false;
        GuiEdited = false;
    }

    public bool LoadFromFile(string path, out string? warning)
    {
        warning = null;
        try
        {
            var text = SafeFileIO.ReadAllText(path, SafeFileIO.MaxConfigBytes);
            if (!RefindStructureValidator.TryValidate(text, out var structureErr))
            {
                warning =
                    structureErr + "\n\nThe file was loaded, but fix structure errors before saving.";
            }

            Document = RefindParser.Parse(text);
            FilePath = path;
            SetDirty(false);
            RawEdited = false;
            GuiEdited = false;
            return true;
        }
        catch (Exception ex)
        {
            warning = ex.Message;
            return false;
        }
    }

    public SaveResult SaveToPath(
        string path,
        SaveConflictResolution? conflictResolution,
        IEnumerable<OptionBinding> bindings,
        string? rawText,
        bool stripCommentsOnApply,
        Action<RefindDocument>? saveExtras = null)
    {
        if (!TryApplyEditsBeforeSave(conflictResolution, bindings, rawText, saveExtras, out var parseError))
        {
            if (parseError is not null)
                return SaveResult.ParseFailed;
            return SaveResult.Cancelled;
        }

        if (stripCommentsOnApply)
            RefindDocumentCleaner.StripComments(Document);

        Document.CollapseDuplicateGlobals();

        if (!Validate(out _))
            return SaveResult.ValidationFailed;

        var text = RefindWriter.Write(Document);
        if (!RefindStructureValidator.TryValidate(text, out _))
            return SaveResult.StructureFailed;

        try
        {
            AtomicFile.WriteAllBytes(path, new System.Text.UTF8Encoding(false).GetBytes(text));
        }
        catch
        {
            return SaveResult.WriteFailed;
        }

        FilePath = path;
        SetDirty(false);
        RawEdited = false;
        GuiEdited = false;
        return SaveResult.Success;
    }

    public void ApplyFromUi(
        IEnumerable<OptionBinding> bindings,
        Action<RefindDocument>? saveThemeInclude = null)
    {
        saveThemeInclude?.Invoke(Document);
        RefindDocumentBridge.SaveBindings(bindings, Document);
        GuiEdited = false;
    }

    public void RefreshUi(
        IEnumerable<OptionBinding> bindings,
        Action<RefindDocument>? loadThemeInclude = null)
    {
        loadThemeInclude?.Invoke(Document);
        RefindDocumentBridge.LoadBindings(bindings, Document);
    }

    public string GetRawText() => ToRawEditorText(RefindWriter.Write(Document));

    public bool TryParseRaw(string text, out string? error)
    {
        if (!RefindStructureValidator.TryValidate(text, out error))
            return false;

        try
        {
            Document = RefindParser.Parse(text);
            RawEdited = false;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public bool Validate(out string message) => RefindValidator.TryValidate(Document, out message);

    public void MarkGuiEdited()
    {
        if (IsApplyingFromDocument)
            return;
        GuiEdited = true;
        SetDirty(true);
    }

    public void MarkRawEdited()
    {
        if (IsApplyingFromDocument)
            return;
        RawEdited = true;
        SetDirty(true);
    }

    public void ClearRawEdited() => RawEdited = false;

    public void SetDirty(bool dirty)
    {
        IsDirty = dirty;
        if (!dirty)
            GuiEdited = false;
    }

    public void StripComments()
    {
        RefindDocumentCleaner.StripComments(Document);
        RawEdited = false;
        GuiEdited = false;
        SetDirty(true);
    }

    internal bool IsApplyingFromDocument { get; set; }

    private bool TryApplyEditsBeforeSave(
        SaveConflictResolution? conflictResolution,
        IEnumerable<OptionBinding> bindings,
        string? rawText,
        Action<RefindDocument>? saveExtras,
        out string? parseError)
    {
        parseError = null;
        if (NeedsSaveConflictResolution)
        {
            switch (conflictResolution)
            {
                case SaveConflictResolution.ApplyRaw:
                    if (rawText is null || !TryParseRaw(rawText, out parseError))
                        return false;
                    break;
                case SaveConflictResolution.ApplyGui:
                    ApplyFromUi(bindings, saveExtras);
                    break;
                default:
                    return false;
            }
        }
        else if (RawEdited)
        {
            if (rawText is null || !TryParseRaw(rawText, out parseError))
                return false;
        }
        else
            ApplyFromUi(bindings, saveExtras);

        return true;
    }

    internal static string ToRawEditorText(string lfText) =>
        lfText.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", Environment.NewLine);
}
