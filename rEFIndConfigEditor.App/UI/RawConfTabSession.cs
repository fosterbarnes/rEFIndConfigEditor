using Avalonia.Controls;
using rEFIndConfigEditor.Services;

namespace rEFIndConfigEditor.UI;

internal sealed class RawConfTabSession
{
    private readonly RefindDocumentService _documentService;
    private TextBox? _editor;
    private bool _rawRefreshNeeded;
    private bool _suppressDirty;
    private Action? _onRawEdited;

    public RawConfTabSession(RefindDocumentService documentService) =>
        _documentService = documentService;

    public void Wire(TextBox editor, Action? onRawEdited = null)
    {
        _editor = editor;
        _onRawEdited = onRawEdited;
        editor.TextChanged += (_, _) =>
        {
            if (_suppressDirty)
                return;
            _documentService.MarkRawEdited();
            _onRawEdited?.Invoke();
        };
    }

    public void OnDocumentRefreshed(bool isRawTabSelected)
    {
        _rawRefreshNeeded = true;
        if (isRawTabSelected)
            FlushRawRefreshIfNeeded();
    }

    public void OnTabSelected(bool isRawTabSelected)
    {
        if (isRawTabSelected)
            FlushRawRefreshIfNeeded();
    }

    public void FlushRawRefreshIfNeeded()
    {
        if (!_rawRefreshNeeded || _editor is null)
            return;

        RefreshRawOnly();
        _rawRefreshNeeded = false;
    }

    public string? CurrentRawText => _editor?.Text;

    private void RefreshRawOnly()
    {
        if (_editor is null)
            return;

        _suppressDirty = true;
        _documentService.IsApplyingFromDocument = true;
        try
        {
            _editor.Text = _documentService.GetRawText();
            _documentService.ClearRawEdited();
        }
        finally
        {
            _documentService.IsApplyingFromDocument = false;
            _suppressDirty = false;
        }
    }

    public bool ApplyFromRawConfirmed(out string? error)
    {
        if (_editor is null)
        {
            error = null;
            return false;
        }

        if (!_documentService.TryParseRaw(_editor.Text ?? string.Empty, out error))
            return false;

        _rawRefreshNeeded = true;
        _documentService.SetDirty(true);
        error = null;
        return true;
    }
}
