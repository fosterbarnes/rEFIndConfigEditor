using System.Runtime.InteropServices;

namespace rEFIndConfigEditor.Update;

public static class ArchitectureHelper
{
    public static string GetCurrentArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => "x64",
        };
    }
}
