namespace tiff;

internal class TiffHeaderReadResult
{
    internal bool Success;
    internal TiffHeader Header;
    internal TiffDiagnostics Diagnostics;

    internal TiffHeaderReadResult()
    {
        Success = false;
        Header = new TiffHeader();
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffHeaderReadResult Create(TiffHeader header, TiffDiagnostics diagnostics)
    {
        TiffHeaderReadResult value = new TiffHeaderReadResult();
        value.Initialize(header, diagnostics);
        return value;
    }

    internal void Initialize(TiffHeader header, TiffDiagnostics diagnostics)
    {
        Header = header;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
