using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.UI;
using rEFIndConfigEditor.Updater.Services;
using rEFIndConfigEditor.Updater.ViewModels;
using rEFIndConfigEditor.Updater.Views;

namespace rEFIndConfigEditor.Updater;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        PlatformServices.Current = new UpdaterPlatformServices();
        UiTheme.ApplyAppTheme(this, UiThemeKind.System);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && Program.LaunchContext is { } context)
        {
            desktop.MainWindow = new UpdaterWindow
            {
                DataContext = new UpdaterWindowViewModel(context),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
