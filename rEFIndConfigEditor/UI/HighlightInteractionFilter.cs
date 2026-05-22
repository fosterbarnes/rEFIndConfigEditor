namespace rEFIndConfigEditor.UI;

internal sealed class HighlightInteractionFilter(Action onInteract) : IMessageFilter
{
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_MOUSEWHEEL = 0x020A;

    private bool _fired;

    public bool PreFilterMessage(ref Message m)
    {
        if (_fired)
            return false;
        switch (m.Msg)
        {
            case WM_LBUTTONDOWN:
            case WM_RBUTTONDOWN:
            case WM_MBUTTONDOWN:
            case WM_KEYDOWN:
            case WM_MOUSEWHEEL:
                _fired = true;
                onInteract();
                break;
        }
        return false;
    }
}
