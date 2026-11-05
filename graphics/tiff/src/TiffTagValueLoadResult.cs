namespace tiff;

internal class TiffTagValueLoadResult
{
    internal bool Success;
    internal bool WasInline;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffTagValueLoadResult()
    {
        Success = false;
        WasInline = false;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffTagValueLoadResult Create(byte[] bytes, bool wasInline, TiffDiagnostics diagnostics)
    {
        TiffTagValueLoadResult value = new TiffTagValueLoadResult();
        value.Initialize(bytes, wasInline, diagnostics);
        return value;
    }

    internal void Initialize(byte[] bytes, bool wasInline, TiffDiagnostics diagnostics)
    {
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        WasInline = wasInline;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffTagValueLoadResult Copy()
    {
        return TiffTagValueLoadResult.Create(Bytes, WasInline, Diagnostics);
    }
}
