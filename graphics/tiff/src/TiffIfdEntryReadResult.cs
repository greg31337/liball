namespace tiff;

internal class TiffIfdEntryReadResult
{
    internal bool Success;
    internal TiffIfdEntry Entry;
    internal TiffDiagnostics Diagnostics;

    internal TiffIfdEntryReadResult()
    {
        Success = false;
        Entry = new TiffIfdEntry();
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffIfdEntryReadResult Create(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        TiffIfdEntryReadResult value = new TiffIfdEntryReadResult();
        value.Initialize(entry, diagnostics);
        return value;
    }

    internal void Initialize(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        Entry = entry;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
