namespace rEFIndConfigEditor.Config;

internal static class RefindValidator
{
    public static bool TryValidate(RefindDocument doc, out string message)
    {
        foreach (var opt in doc.GlobalOptions)
        {
            if (!opt.IsActive)
                continue;

            var def = OptionCatalog.All.FirstOrDefault(d => RefindTokens.TokenEquals(d.Token, opt.Token));
            if (def is null)
                continue;

            switch (def.Kind)
            {
                case OptionControlKind.MultiSelect:
                case OptionControlKind.Text:
                case OptionControlKind.Choice:
                case OptionControlKind.Numeric:
                    if (opt.Values.Count == 0)
                    {
                        message = $"Option \"{opt.Token}\" is enabled but has no value.";
                        return false;
                    }
                    break;
            }
        }

        foreach (var entry in doc.MenuEntries)
        {
            if (entry.Disabled)
                continue;

            if (!HasBootTarget(entry.GetField("loader"), entry.GetField("firmware_bootnum")))
            {
                message = $"Boot entry \"{entry.Title}\" needs loader or firmware_bootnum.";
                return false;
            }

            foreach (var sub in entry.Submenus)
            {
                if (sub.Disabled)
                    continue;
                if (!HasBootTarget(sub.GetField("loader"), sub.GetField("firmware_bootnum")))
                {
                    message = $"Submenu \"{sub.Title}\" in \"{entry.Title}\" needs loader or firmware_bootnum.";
                    return false;
                }
            }
        }
        message = "";
        return true;
    }

    private static bool HasBootTarget(string? loader, string? firmware) =>
        !string.IsNullOrEmpty(loader) || !string.IsNullOrEmpty(firmware);
}
