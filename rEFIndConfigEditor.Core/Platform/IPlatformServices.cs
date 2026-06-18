using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Platform;

public interface IPlatformServices
{
    Task<string?> PickFileAsync(string? title, string? filter);
    Task<IReadOnlyList<string>> PickFilesAsync(string? title, string? filter);
    Task<string?> PickSaveFileAsync(string? title, string? filter, string? defaultFileName = null);
    Task<string?> PickFolderAsync(string? title);
    void OpenUrl(string url);
    bool IsSystemDarkTheme();
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
    Task<bool> ConfirmAsync(string title, string message);
    Task<SaveConflictResolution?> AskSaveConflictAsync(string title, string message);
    Task<YesNoCancelChoice?> AskYesNoCancelAsync(string title, string message);
}

public enum YesNoCancelChoice
{
    Yes,
    No,
    Cancel
}
