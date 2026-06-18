using Avalonia;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Platform;

namespace rEFIndConfigEditor.Updater;

internal static class Program
{
    internal static UpdaterLaunchContext? LaunchContext { get; set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var silentExit = UpdaterEntry.TryRunSilent(args);
        if (silentExit.HasValue)
        {
            Environment.Exit(silentExit.Value);
            return;
        }

        try
        {
            LaunchContext = UpdaterEntry.CreateContext(args);
        }
        catch (InvalidOperationException ex)
        {
            NativeDialog.ShowError(AppBranding.UpdaterTitle, ex.Message);
            Environment.Exit(1);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
