using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace rEFIndConfigEditor.UI;

internal static class AppIconLoader
{
    public static void TryApplyWindowIcon(Window window)
    {
        foreach (var path in ResolveWindowIconPaths())
        {
            if (!File.Exists(path))
                continue;

            try
            {
                window.Icon = new WindowIcon(path);
                return;
            }
            catch
            {
            }
        }
    }

    public static void TryLoadAboutIcon(Image image)
    {
        foreach (var path in ResolvePngPaths())
        {
            if (!File.Exists(path))
                continue;

            try
            {
                image.Source = new Bitmap(path);
                return;
            }
            catch
            {
            }
        }
    }

    private static IEnumerable<string> ResolveWindowIconPaths()
    {
        foreach (var path in ResolveIcoPaths())
            yield return path;

        if (OperatingSystem.IsMacOS())
        {
            foreach (var path in ResolvePngPaths())
                yield return path;
        }
    }

    private static IEnumerable<string> ResolveIcoPaths()
    {
        var dir = AppContext.BaseDirectory;
        yield return Path.Combine(dir, AppBranding.IconAssetsFolder, AppBranding.IconFileName);
        yield return Path.Combine(dir, AppBranding.IconFileName);
    }

    private static IEnumerable<string> ResolvePngPaths()
    {
        var dir = AppContext.BaseDirectory;
        yield return Path.Combine(dir, AppBranding.IconAssetsFolder, AppBranding.IconPngFileName);
        yield return Path.Combine(dir, AppBranding.IconPngFileName);
    }
}
