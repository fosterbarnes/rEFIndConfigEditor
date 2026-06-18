namespace rEFIndConfigEditor;

public static class AppBranding
{
    public const string DisplayName = "rEFInd Config Editor";

    public const string Slug = "rEFIndConfigEditor";

    public const string RepoOwner = "fosterbarnes";

    public const string CopyrightHolder = "Foster Barnes";

    public static string UpdaterTitle => $"{DisplayName} Updater";

    public static string RepoUrl => $"https://github.com/{RepoOwner}/{Slug}";

    public const string ExeFileName = Slug + ".exe";

    public const string SolutionFileName = Slug + ".sln";

    public const string IconFileName = "refind.ico";

    public const string IconPngFileName = "refind256.png";

    public const string IconAssetsFolder = "Assets";

    public const string UserAgent = Slug + "-updater";

    public static string TempUpdateFolderPrefix => $"{Slug}-update";

    public const string RefindDocsUrl = "https://www.rodsbooks.com/refind/";

    public const string ConfigDocUrl = "https://www.rodsbooks.com/refind/configfile.html";

    public const string WikiUrl = "https://github.com/fosterbarnes/rEFIndConfigEditor/wiki";

    public const string WikiTokensUrl = WikiUrl + "#refind-configuration-tokens";

    public const string WikiThemesUrl = WikiUrl + "#refind-supported-themes";
}
