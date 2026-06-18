using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Platform;

public static class PlatformServices
{
    public static IPlatformServices Current { get; set; } = NullPlatformServices.Instance;
}

internal sealed class NullPlatformServices : IPlatformServices
{
    public static NullPlatformServices Instance { get; } = new();

    public Task<string?> PickFileAsync(string? title, string? filter) => Task.FromResult<string?>(null);
    public Task<IReadOnlyList<string>> PickFilesAsync(string? title, string? filter) =>
        Task.FromResult<IReadOnlyList<string>>([]);
    public Task<string?> PickSaveFileAsync(string? title, string? filter, string? defaultFileName = null) =>
        Task.FromResult<string?>(null);
    public Task<string?> PickFolderAsync(string? title) => Task.FromResult<string?>(null);
    public void OpenUrl(string url) { }
    public bool IsSystemDarkTheme() => false;
    public void ShowWarning(string title, string message) { }
    public void ShowError(string title, string message) { }
    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(false);
    public Task<SaveConflictResolution?> AskSaveConflictAsync(string title, string message) =>
        Task.FromResult<SaveConflictResolution?>(SaveConflictResolution.Cancel);
    public Task<YesNoCancelChoice?> AskYesNoCancelAsync(string title, string message) =>
        Task.FromResult<YesNoCancelChoice?>(YesNoCancelChoice.Cancel);
}
