namespace tiff;

internal class TiffScanlineReadResult
{
    internal bool Success;
    internal ulong Row;
    internal int SampleIndex;
    internal ulong StripIndex;
    internal ulong ScanlineByteCount;
    internal TiffCompression Compression;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffScanlineReadResult()
    {
        Success = false;
        Row = 0;
        SampleIndex = 0;
        StripIndex = 0;
        ScanlineByteCount = 0;
        Compression = TiffCompression.Unknown;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffScanlineReadResult Create(ulong row, int sampleIndex, ulong stripIndex, ulong scanlineByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffScanlineReadResult value = new TiffScanlineReadResult();
        value.Initialize(row, sampleIndex, stripIndex, scanlineByteCount, compression, bytes, diagnostics);
        return value;
    }

    internal void Initialize(ulong row, int sampleIndex, ulong stripIndex, ulong scanlineByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        Row = row;
        SampleIndex = sampleIndex;
        StripIndex = stripIndex;
        ScanlineByteCount = scanlineByteCount;
        Compression = compression;
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffScanlineReadResult Copy()
    {
        return TiffScanlineReadResult.Create(Row, SampleIndex, StripIndex, ScanlineByteCount, Compression, Bytes, Diagnostics);
    }
}
