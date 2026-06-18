using System.Diagnostics;
using System.Runtime.InteropServices;

namespace rEFIndConfigEditor.Platform;

// Pre-main-window errors: Win32 MessageBoxW on Windows, osascript alert on macOS, zenity on Linux.
public static class NativeDialog
{
    public static void ShowError(string title, string message)
    {
        if (OperatingSystem.IsWindows())
        {
            MessageBoxW(IntPtr.Zero, message, title, MbIconError);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            RunOsascriptAlert(title, message);
            return;
        }

        if (OperatingSystem.IsLinux())
            RunZenityError(title, message);
    }

    private static void RunZenityError(string title, string message)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "zenity",
            ArgumentList = { "--error", $"--title={title}", $"--text={message}", "--no-wrap" },
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        process?.WaitForExit(5000);
    }

    private static void RunOsascriptAlert(string title, string message)
    {
        var script = $"display alert {Quote(title)} message {Quote(message)} as critical";
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "osascript",
            ArgumentList = { "-e", script },
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        process?.WaitForExit(5000);
    }

    private static string Quote(string value) =>
        '"' + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + '"';

    private const uint MbIconError = 0x00000010;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
