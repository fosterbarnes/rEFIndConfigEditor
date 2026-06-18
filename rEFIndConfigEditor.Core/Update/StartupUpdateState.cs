namespace rEFIndConfigEditor.Update;

public static class StartupUpdateState
{
    private static string? _pendingStatusMessage;

    public static void SetPendingStatusMessage(string message) =>
        _pendingStatusMessage = message;

    public static string? ConsumePendingStatusMessage()
    {
        var message = _pendingStatusMessage;
        _pendingStatusMessage = null;
        return message;
    }
}
