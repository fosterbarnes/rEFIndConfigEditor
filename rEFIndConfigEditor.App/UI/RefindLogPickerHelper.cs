using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Platform;

namespace rEFIndConfigEditor.UI;

internal static class RefindLogPickerHelper
{
    public static async Task<string?> PickLogPathAsync(string? confPath)
    {
        var beside = RefindLogPicker.LogPathBesideConf(confPath);
        if (beside is not null)
            return beside;

        return await PlatformServices.Current.PickFileAsync(
            "Open refind.log",
            "rEFInd log (refind.log)|refind.log|Log files (*.log)|*.log|All files (*.*)|*.*");
    }
}
