namespace rEFIndConfigEditor.UI;

internal static class AppFormIcon
{
    private static Icon? _cached;

    public static void Apply(Form form)
    {
        try
        {
            _cached ??= Extract();
            if (_cached is not null)
                form.Icon = (Icon)_cached.Clone();
        }
        catch
        {
            // keep default form icon
        }
    }

    private static Icon? Extract()
    {
        using var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return extracted is null ? null : (Icon)extracted.Clone();
    }
}
