namespace rEFIndConfigEditor.Models;

public static class UiFontDefaults
{
    public const int Min = 8;
    public const int Max = 24;
    public const int Main = 12;
    public const int Tab = 12;
    public const int Token = 11;

    public static int Clamp(int size) => Math.Clamp(size, Min, Max);
}
