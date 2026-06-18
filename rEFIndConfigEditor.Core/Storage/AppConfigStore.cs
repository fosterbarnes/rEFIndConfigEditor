using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Storage;

public sealed class AppConfigStore
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.None,
        MaxDepth = 32,
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        Converters = { new StringEnumConverter() }
    };

    private readonly string _baseDirectory;
    private readonly string _uiPreferencesFilePath;
    private readonly string _catalogSettingsFilePath;

    public AppConfigStore()
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppBranding.Slug);
        _uiPreferencesFilePath = Path.Combine(_baseDirectory, "ui.json");
        _catalogSettingsFilePath = Path.Combine(_baseDirectory, "settings.json");
    }

    public UiPreferences LoadUiPreferences()
    {
        if (!File.Exists(_uiPreferencesFilePath))
            return new UiPreferences();

        var json = SafeFileIO.ReadAllText(_uiPreferencesFilePath, SafeFileIO.MaxJsonBytes);
        var token = JToken.Parse(json);
        if (token is not JObject jo)
            return new UiPreferences();

        MigrateTheme(jo);
        var prefs = jo.ToObject<UiPreferences>(JsonSerializer.Create(SerializerSettings)) ?? new UiPreferences();
        NormalizeLegacy(prefs, jo);
        ClampFontSizes(prefs);
        NormalizeFontFamilies(prefs);
        return prefs;
    }

    public void SaveUiPreferences(UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        ClampFontSizes(preferences);
        NormalizeFontFamilies(preferences);
        Directory.CreateDirectory(_baseDirectory);
        var json = JsonConvert.SerializeObject(preferences, SerializerSettings);
        AtomicFile.WriteAllBytes(_uiPreferencesFilePath, Encoding.UTF8.GetBytes(json));
    }

    public CatalogSettingsDocument LoadCatalogSettings()
    {
        if (!File.Exists(_catalogSettingsFilePath))
            return new CatalogSettingsDocument();

        var json = SafeFileIO.ReadAllText(_catalogSettingsFilePath, SafeFileIO.MaxJsonBytes);
        return JsonConvert.DeserializeObject<CatalogSettingsDocument>(json, SerializerSettings)
            ?? new CatalogSettingsDocument();
    }

    public void SaveCatalogSettings(CatalogSettingsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Directory.CreateDirectory(_baseDirectory);
        var json = JsonConvert.SerializeObject(document, SerializerSettings);
        AtomicFile.WriteAllBytes(_catalogSettingsFilePath, Encoding.UTF8.GetBytes(json));
    }

    private static void NormalizeLegacy(UiPreferences prefs, JObject jo)
    {
        if (jo["Theme"] is JToken { Type: JTokenType.Integer } legacyInt)
            prefs.Theme = MapLegacyThemeInt(legacyInt.Value<int>());

        if (jo["DarkMode"] is JToken { Type: JTokenType.Boolean } darkMode
            && darkMode.Value<bool>()
            && prefs.Theme == UiThemeKind.System)
        {
            prefs.Theme = UiThemeKind.Dark;
        }

        if (string.IsNullOrEmpty(prefs.LastSelectedTabKey)
            && jo["LastSelectedTabIndex"] is JToken { Type: JTokenType.Integer } legacyTabIndex)
        {
            prefs.LastSelectedTabKey = MapLegacyTabIndex(legacyTabIndex.Value<int>());
        }

        if (jo["BodyFontSize"] is JToken { Type: JTokenType.Integer } body
            && jo["MainFontSize"] is null)
        {
            prefs.MainFontSize = body.Value<int>();
        }
    }

    private static string? MapLegacyTabIndex(int index) => index switch
    {
        0 => "AppSettings",
        1 => "About",
        _ => null
    };

    private static void MigrateTheme(JObject jo)
    {
        if (jo["Theme"] is not JToken themeToken)
            return;

        switch (themeToken.Type)
        {
            case JTokenType.String:
                var name = themeToken.Value<string>();
                if (string.Equals(name, "DraculaLight", StringComparison.OrdinalIgnoreCase))
                    jo["Theme"] = nameof(UiThemeKind.Light);
                else if (string.Equals(name, "DraculaDark", StringComparison.OrdinalIgnoreCase))
                    jo["Theme"] = nameof(UiThemeKind.Dracula);
                break;
            case JTokenType.Integer:
                var themeInt = themeToken.Value<int>();
                if (themeInt is 3 or 4)
                    jo["Theme"] = nameof(UiThemeKind.Dracula);
                break;
        }
    }

    private static UiThemeKind MapLegacyThemeInt(int value) => value switch
    {
        0 => UiThemeKind.Light,
        1 => UiThemeKind.Dark,
        2 => UiThemeKind.Light,
        3 => UiThemeKind.Dracula,
        4 => UiThemeKind.Dracula,
        _ => UiThemeKind.System
    };

    private static void ClampFontSizes(UiPreferences prefs)
    {
        prefs.MainFontSize = UiFontDefaults.Clamp(prefs.MainFontSize);
        prefs.TabFontSize = UiFontDefaults.Clamp(prefs.TabFontSize);
        prefs.TokenFontSize = UiFontDefaults.Clamp(prefs.TokenFontSize);
    }

    private static void NormalizeFontFamilies(UiPreferences prefs)
    {
        prefs.MainFontFamily = UiFontFamilies.NormalizeMain(prefs.MainFontFamily);
        prefs.MonoFontFamily = UiFontFamilies.NormalizeMono(prefs.MonoFontFamily);
    }
}
