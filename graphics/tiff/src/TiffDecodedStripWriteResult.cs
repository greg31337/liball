namespace tiff;

internal class TiffDecodedStripWriteResult
{
    internal bool Success;
    internal ulong StripIndex;
    internal ulong ExpectedDecodedByteCount;
    internal TiffCompression Compression;
    internal ulong BytesWritten;
    internal TiffDiagnostics Diagnostics;

    internal TiffDecodedStripWriteResult()
    {
        Success = false;
        StripIndex = 0;
        ExpectedDecodedByteCount = 0;
        Compression = TiffCompression.Unknown;
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDecodedStripWriteResult Create(ulong stripIndex, ulong expectedDecodedByteCount, TiffCompression compression, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffDecodedStripWriteResult value = new TiffDecodedStripWriteResult();
        value.Initialize(stripIndex, expectedDecodedByteCount, compression, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong stripIndex, ulong expectedDecodedByteCount, TiffCompression compression, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        StripIndex = stripIndex;
        ExpectedDecodedByteCount = expectedDecodedByteCount;
        Compression = compression;
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffDecodedStripWriteResult Copy()
    {
        return TiffDecodedStripWriteResult.Create(StripIndex, ExpectedDecodedByteCount, Compression, BytesWritten, Diagnostics);
    }
}
