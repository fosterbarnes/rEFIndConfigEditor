using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Storage;

public sealed class AppConfigStore
{
    private static readonly JsonSerializerSettings UiPreferencesSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 32,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        Converters = { new StringEnumConverter() }
    };

    private readonly string _baseDirectory;
    private readonly string _uiPreferencesFilePath;

    public AppConfigStore()
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "rEFIndConfigEditor");
        _uiPreferencesFilePath = Path.Combine(_baseDirectory, "ui.json");
    }

    public UiPreferences LoadUiPreferences()
    {
        var prefs = new UiPreferences();
        try
        {
            if (!File.Exists(_uiPreferencesFilePath))
                return prefs;

            string json = SafeFileIO.ReadAllText(_uiPreferencesFilePath, SafeFileIO.MaxJsonBytes);
            using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 32 };
            var jo = JObject.Load(reader);

            prefs.Theme = ReadTheme(jo) ?? prefs.Theme;
            prefs.WindowX = jo["WindowX"]?.Value<int?>();
            prefs.WindowY = jo["WindowY"]?.Value<int?>();
            prefs.WindowWidth = jo["WindowWidth"]?.Value<int?>();
            prefs.WindowHeight = jo["WindowHeight"]?.Value<int?>();
            prefs.WindowMaximized = jo["WindowMaximized"]?.Value<bool>() ?? false;
            prefs.AutoLoadLastConfOnLaunch = jo["AutoLoadLastConfOnLaunch"]?.Value<bool>() ?? true;
            prefs.RememberLastSelectedTab = jo["RememberLastSelectedTab"]?.Value<bool>() ?? false;
            prefs.LastSelectedTabIndex = jo["LastSelectedTabIndex"]?.Value<int?>();
            prefs.LastConfPath = jo["LastConfPath"]?.Value<string>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"LoadUiPreferences failed: {ex}");
        }
        return prefs;
    }

    private static UiThemeKind? ReadTheme(JObject jo)
    {
        if (jo["Theme"] is JToken t)
        {
            if (t.Type == JTokenType.String)
            {
                string name = t.ToString();
                if (string.Equals(name, "Dracula", StringComparison.OrdinalIgnoreCase))
                    return UiThemeKind.DraculaLight;
                if (Enum.TryParse(name, true, out UiThemeKind byName))
                    return byName;
            }
            if (t.Type == JTokenType.Integer)
                return MapLegacyThemeInt(t.Value<int>());
        }
        if (jo["DarkMode"] is JToken darkMode && darkMode.Type == JTokenType.Boolean && darkMode.Value<bool>())
            return UiThemeKind.Dark;
        return null;
    }

    public void SaveUiPreferences(UiPreferences preferences)
    {
        if (preferences == null)
            return;

        try
        {
            Directory.CreateDirectory(_baseDirectory);
            string json = JsonConvert.SerializeObject(preferences, UiPreferencesSerializerSettings);
            AtomicFile.WriteAllBytes(_uiPreferencesFilePath, Encoding.UTF8.GetBytes(json));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"SaveUiPreferences failed: {ex}");
        }
    }

    private static UiThemeKind MapLegacyThemeInt(int v)
    {
        return v switch
        {
            0 => UiThemeKind.Light,
            1 => UiThemeKind.Dark,
            2 => UiThemeKind.DraculaLight,
            3 => UiThemeKind.DraculaDark,
            _ => UiThemeKind.System
        };
    }
}
