namespace rEFIndConfigEditor.Models;

public enum SaveConflictResolution
{
    ApplyRaw,
    ApplyGui,
    Cancel
}

public enum SaveResult
{
    Success,
    Cancelled,
    ValidationFailed,
    StructureFailed,
    WriteFailed,
    ParseFailed
}
