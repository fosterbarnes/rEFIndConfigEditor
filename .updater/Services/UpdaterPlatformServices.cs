using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Platform;

namespace rEFIndConfigEditor.Updater.Services;

internal sealed class UpdaterPlatformServices : IPlatformServices
{
    public Task<string?> PickFileAsync(string? title, string? filter) => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> PickFilesAsync(string? title, string? filter) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<string?> PickSaveFileAsync(string? title, string? filter, string? defaultFileName = null) =>
        Task.FromResult<string?>(null);
    public Task<string?> PickFolderAsync(string? title) => Task.FromResult<string?>(null);
    public void OpenUrl(string url) { }

    public bool IsSystemDarkTheme()
    {
        if (Application.Current?.PlatformSettings?.GetColorValues() is { } colors)
            return colors.ThemeVariant == PlatformThemeVariant.Dark;
        return false;
    }

    public void ShowWarning(string title, string message) => ShowDialog(title, message);

    public void ShowError(string title, string message) => ShowDialog(title, message);

    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(false);

    public Task<SaveConflictResolution?> AskSaveConflictAsync(string title, string message) =>
        Task.FromResult<SaveConflictResolution?>(SaveConflictResolution.Cancel);

    public Task<YesNoCancelChoice?> AskYesNoCancelAsync(string title, string message) =>
        Task.FromResult<YesNoCancelChoice?>(YesNoCancelChoice.Cancel);

    private static void ShowDialog(string title, string message)
    {
        var window = GetMainWindow();
        if (window is null)
            return;

        Dispatcher.UIThread.Post(async () =>
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = HorizontalAlignment.Right,
                        },
                    },
                },
            };

            if (dialog.Content is StackPanel panel && panel.Children[^1] is Button ok)
                ok.Click += (_, _) => dialog.Close();

            await dialog.ShowDialog(window);
        });
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
