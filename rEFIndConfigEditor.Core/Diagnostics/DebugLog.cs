using rEFIndConfigEditor;

namespace rEFIndConfigEditor.Diagnostics;

public static class DebugLog
{
    private const int MaxLines = 500;
    private static readonly object Gate = new();
    private static readonly Queue<string> Buffer = new();
    private static bool _enabled;
    private static string? _logPath;
    private static string? _runHeader;

    public static bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
                return;

            if (value)
            {
                _enabled = true;
                BeginRunIfNeeded();
                Write("[Prefs] Debug logging enabled");
            }
            else
            {
                _enabled = false;
                lock (Gate)
                    Buffer.Clear();
            }
        }
    }

    public static event Action<string>? EntryAdded;

    public static void Write(string message)
    {
        if (!_enabled)
            return;

        AppendLine(message);
    }

    public static void Write(string category, string message)
    {
        if (!_enabled)
            return;

        AppendLine($"[{category}] {message}");
    }

    public static string GetText()
    {
        if (!_enabled)
            return string.Empty;

        lock (Gate)
            return string.Join(Environment.NewLine, Buffer);
    }

    private static void AppendLine(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        lock (Gate)
        {
            Buffer.Enqueue(line);
            while (Buffer.Count > MaxLines)
                Buffer.Dequeue();

            try
            {
                var path = GetLogPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch
            {
            }
        }

        EntryAdded?.Invoke(line);
    }

    private static void BeginRunIfNeeded()
    {
        if (_runHeader is not null)
        {
            lock (Gate)
            {
                if (Buffer.Count == 0)
                    Buffer.Enqueue(_runHeader);
            }

            return;
        }

        _runHeader =
            $"========== Run {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} (pid {Environment.ProcessId}) ==========";

        try
        {
            var path = GetLogPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(path) && new FileInfo(path).Length > 0)
                File.AppendAllText(path, Environment.NewLine);
            File.AppendAllText(path, _runHeader + Environment.NewLine);
        }
        catch
        {
        }

        lock (Gate)
            Buffer.Enqueue(_runHeader);
    }

    private static string GetLogPath()
    {
        _logPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppBranding.Slug,
            "debug.log");
        return _logPath;
    }
}
