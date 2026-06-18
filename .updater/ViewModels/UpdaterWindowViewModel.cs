using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Update;

namespace rEFIndConfigEditor.Updater.ViewModels;

internal enum UpdaterUiState
{
    Checking,
    UpToDate,
    UpdateAvailable,
    Downloading,
    Done,
    Error,
}

internal sealed partial class UpdaterWindowViewModel : ObservableObject
{
    private readonly UpdaterLaunchContext _context;
    private readonly CancellationTokenSource _lifetime = new();
    private GitHubRelease? _pendingRelease;
    private Task? _backgroundTask;
    private volatile bool _closed;

    [ObservableProperty] private string _titleText = AppBranding.UpdaterTitle;
    [ObservableProperty] private string _statusText = "Checking for updates...";
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private bool _progressVisible;
    [ObservableProperty] private bool _actionVisible;
    [ObservableProperty] private string _actionText = "Close";

    public UpdaterUiState State { get; private set; } = UpdaterUiState.Checking;

    public UpdaterWindowViewModel(UpdaterLaunchContext context)
    {
        _context = context;
        TitleText = $"{AppBranding.UpdaterTitle} v{context.UpdaterVersion}";
    }

    public async Task OnShownAsync()
    {
        if (_context.InstallMode)
        {
            BeginInstallFlow(null);
            return;
        }

        await RunCheckAsync().ConfigureAwait(true);
    }

    public void OnClosing()
    {
        _closed = true;
        if (_backgroundTask is { IsCompleted: false })
            _lifetime.Cancel();
    }

    [RelayCommand]
    private void Action()
    {
        switch (State)
        {
            case UpdaterUiState.UpToDate:
                RequestClose?.Invoke();
                break;
            case UpdaterUiState.UpdateAvailable:
                BeginInstallFromRelease();
                break;
            case UpdaterUiState.Done:
                LaunchAppAndExit();
                break;
            case UpdaterUiState.Error:
                if (_context.InstallMode)
                    BeginInstallFlow(null);
                else
                    _ = RunCheckAsync();
                break;
        }
    }

    public event Action? RequestClose;

    private async Task RunCheckAsync()
    {
        SetState(UpdaterUiState.Checking, "Checking for updates...");
        try
        {
            var result = await GitHubReleaseService
                .CheckForUpdateAsync(_context.InstallDirectory, _lifetime.Token)
                .ConfigureAwait(true);

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                SetError(result.ErrorMessage);
                return;
            }

            if (!result.IsOutdated && !_context.ForceUpdate)
            {
                SetState(UpdaterUiState.UpToDate, $"{AppBranding.DisplayName} is up to date (v{result.CurrentVersion}).");
                ShowAction("Close");
                return;
            }

            _pendingRelease = result.Release;
            var status = _context.ForceUpdate && !result.IsOutdated
                ? $"Reinstalling latest release v{result.LatestVersion} (current v{result.CurrentVersion})."
                : $"Update available: v{result.LatestVersion} (current v{result.CurrentVersion}).";
            SetState(UpdaterUiState.UpdateAvailable, status);
            ShowAction("Update Now");
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Check failed: {ex}");
            SetError(ex.Message);
        }
    }

    private void BeginInstallFlow(GitHubRelease? release)
    {
        SetState(UpdaterUiState.Downloading, "Starting update...");
        ProgressVisible = true;
        ActionVisible = false;
        _backgroundTask = Task.Run(() => RunInstallAsync(release, _lifetime.Token));
    }

    private void BeginInstallFromRelease()
    {
        if (_pendingRelease is null)
        {
            SetError("No release information is available.");
            return;
        }

        BeginInstallFlow(_pendingRelease);
    }

    private async Task RunInstallAsync(GitHubRelease? release, CancellationToken cancellationToken)
    {
        try
        {
            var progress = new Progress<(float Progress, string Status)>(report =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateProgress(report.Progress, report.Status));
            });

            await UpdateInstallRunner
                .RunAsync(_context.InstallDirectory, release, progress, cancellationToken, _context.HostProcessId)
                .ConfigureAwait(false);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SetState(UpdaterUiState.Done, "Update complete!");
                ProgressValue = 100;
                ShowAction($"Launch {AppBranding.DisplayName}");
            });
        }
        catch (OperationCanceledException)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SetState(UpdaterUiState.Error, "Update cancelled.");
                ProgressVisible = false;
                ShowAction("Close");
            });
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Install failed: {ex}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() => SetError(ex.Message));
        }
    }

    private void LaunchAppAndExit()
    {
        try
        {
            var process = ProcessLaunchService.LaunchApp(_context.InstallDirectory);
            if (process is null)
            {
                RequestClose?.Invoke();
                return;
            }

            Task.Run(() =>
            {
                ProcessLaunchService.WaitForAppWindowReady(process);
                if (!_closed)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => RequestClose?.Invoke());
            });
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Launch failed: {ex}");
            SetError(ex.Message);
        }
    }

    private void SetState(UpdaterUiState state, string status)
    {
        State = state;
        StatusText = status;
    }

    private void SetError(string message)
    {
        State = UpdaterUiState.Error;
        StatusText = $"Error: {message}";
        ProgressVisible = false;
        ShowAction("Retry");
    }

    private void UpdateProgress(float progress, string status)
    {
        StatusText = status;
        ProgressVisible = true;
        ProgressValue = Math.Clamp(progress * 100f, 0, 100);
    }

    private void ShowAction(string text)
    {
        ActionText = text;
        ActionVisible = true;
    }
}
