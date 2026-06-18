namespace rEFIndConfigEditor.Config;

public static class StanzaEditorFields
{
    public static readonly HashSet<string> BootEntryTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "volume", "loader", "initrd", "icon", "firmware_bootnum", "ostype", "graphics", "options", "add_options"
    };

    public static readonly HashSet<string> SubmenuEntryTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "loader", "initrd", "firmware_bootnum", "graphics", "options", "add_options"
    };

    public static void RemoveManagedFields(Dictionary<string, StanzaValue> fields, HashSet<string> managed)
    {
        foreach (var key in fields.Keys.ToList())
        {
            if (managed.Contains(key))
                fields.Remove(key);
        }
    }
}
