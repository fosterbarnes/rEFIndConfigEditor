using System.Runtime.InteropServices;

namespace rEFIndConfigEditor.UI;

internal static class RecycleBinHelper
{
    private const int FoDelete = 0x0003;
    private const int FofAllowUndo = 0x0040;
    private const int FofNoConfirmation = 0x0010;
    private const int FofSilent = 0x0004;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHFileOperation(ref SHFileOpStruct fileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    private struct SHFileOpStruct
    {
        public IntPtr Hwnd;
        public int WFunc;
        public string PFrom;
        public string PTo;
        public short FFlags;
        public bool FAnyOperationsAborted;
        public IntPtr HNameMappings;
        public string LpszProgressTitle;
    }

    public static bool TrySendToRecycleBin(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath) && !fullPath.EndsWith(Path.DirectorySeparatorChar))
            fullPath += Path.DirectorySeparatorChar;

        var fileOp = new SHFileOpStruct
        {
            WFunc = FoDelete,
            PFrom = fullPath + '\0' + '\0',
            FFlags = FofAllowUndo | FofNoConfirmation | FofSilent
        };

        return SHFileOperation(ref fileOp) == 0;
    }
}
