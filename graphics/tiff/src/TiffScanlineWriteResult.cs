namespace tiff;

internal class TiffScanlineWriteResult
{
    internal bool Success;
    internal ulong Row;
    internal int SampleIndex;
    internal ulong StripIndex;
    internal ulong ScanlineByteCount;
    internal TiffCompression Compression;
    internal ulong BytesWritten;
    internal TiffDiagnostics Diagnostics;

    internal TiffScanlineWriteResult()
    {
        Success = false;
        Row = 0;
        SampleIndex = 0;
        StripIndex = 0;
        ScanlineByteCount = 0;
        Compression = TiffCompression.Unknown;
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffScanlineWriteResult Create(ulong row, int sampleIndex, ulong stripIndex, ulong scanlineByteCount, TiffCompression compression, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffScanlineWriteResult value = new TiffScanlineWriteResult();
        value.Initialize(row, sampleIndex, stripIndex, scanlineByteCount, compression, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong row, int sampleIndex, ulong stripIndex, ulong scanlineByteCount, TiffCompression compression, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        Row = row;
        SampleIndex = sampleIndex;
        StripIndex = stripIndex;
        ScanlineByteCount = scanlineByteCount;
        Compression = compression;
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffScanlineWriteResult Copy()
    {
        return TiffScanlineWriteResult.Create(Row, SampleIndex, StripIndex, ScanlineByteCount, Compression, BytesWritten, Diagnostics);
    }
}
