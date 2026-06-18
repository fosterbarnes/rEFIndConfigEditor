using System.Diagnostics;
using System.Runtime.InteropServices;

namespace rEFIndConfigEditor.Update;

public static class ProcessLaunchService
{
    private const int WindowWaitTimeoutMs = 15_000;
    private const int WindowPollIntervalMs = 200;

    public static Process? LaunchApp(string installDirectory)
    {
        var exePath = Path.Combine(installDirectory, UpdateConstants.AppExeName);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Could not find {UpdateConstants.AppExeName}.", exePath);

        return Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = installDirectory,
            UseShellExecute = true,
        });
    }

    public static void WaitForAppWindowReady(Process process)
    {
        Thread.Sleep(500);
        var deadline = Environment.TickCount64 + WindowWaitTimeoutMs;

        while (Environment.TickCount64 < deadline)
        {
            if (process.HasExited)
                break;

            if (TryFindMainWindow(process))
            {
                Thread.Sleep(1000);
                return;
            }

            Thread.Sleep(WindowPollIntervalMs);
        }

        UpdaterLogger.Write($"Timed out waiting for {AppBranding.DisplayName} main window.");
    }

    private static bool TryFindMainWindow(Process process)
    {
        process.Refresh();
        if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
            return true;

        if (!OperatingSystem.IsWindows())
            return process.MainWindowHandle != IntPtr.Zero;

        var handle = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid != process.Id)
                return true;

            if (!IsWindowVisible(hwnd))
                return true;

            handle = hwnd;
            return false;
        }, IntPtr.Zero);

        return handle != IntPtr.Zero;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lProcessId);
}
