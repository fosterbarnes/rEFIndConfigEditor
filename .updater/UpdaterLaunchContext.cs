namespace rEFIndConfigEditor.Updater;

internal sealed class UpdaterLaunchContext
{
    public required string InstallDirectory { get; init; }
    public required bool InstallMode { get; init; }
    public required bool ForceUpdate { get; init; }
    public int? HostProcessId { get; init; }
    public required string UpdaterVersion { get; init; }
}
