using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using rEFIndConfigEditor.Diagnostics;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.Services;
using rEFIndConfigEditor.Storage;
using rEFIndConfigEditor.UI;
using rEFIndConfigEditor.Update;
using rEFIndConfigEditor.ViewModels;
using rEFIndConfigEditor.Views;

namespace rEFIndConfigEditor;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        PlatformServices.Current = new DesktopPlatformServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            _ = InitializeDesktopAsync(desktop);

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var store = new AppConfigStore();
            var prefs = LoadPreferences(store);
            DebugLog.Enabled = prefs.EnableDebugLogging;
            UiFontService.Apply(Current!, prefs);
            UiTheme.ApplyAppTheme(Current!, prefs.Theme);

            if (await StartupUpdateGate.EvaluateAsync(AppContext.BaseDirectory, prefs)
                == StartupUpdateGateResult.ExitWithoutMainUi)
            {
                desktop.Shutdown();
                return;
            }

            var installDirectory = AppContext.BaseDirectory;
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(store, prefs),
            };

            _ = Task.Run(() => PostUpdateUpdaterRefresh.TryApplyPendingRefresh(installDirectory));

            DebugLog.Write("Startup", "Main UI shown");
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Fatal error: {ex}");
            if (DebugLog.Enabled)
                DebugLog.Write("Startup", $"Fatal error: {ex}");
            NativeDialog.ShowError(AppBranding.DisplayName, $"An unexpected error occurred:\n\n{ex.Message}");
            desktop.Shutdown(1);
        }
    }

    private static UiPreferences LoadPreferences(AppConfigStore store)
    {
        try
        {
            return store.LoadUiPreferences();
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Failed to load UI preferences: {ex}");
            if (DebugLog.Enabled)
                DebugLog.Write("Startup", $"Failed to load UI preferences: {ex}");
            return new UiPreferences();
        }
    }
}
