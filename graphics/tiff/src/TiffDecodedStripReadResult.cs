namespace tiff;

internal class TiffDecodedStripReadResult
{
    internal bool Success;
    internal ulong StripIndex;
    internal ulong ExpectedDecodedByteCount;
    internal TiffCompression Compression;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffDecodedStripReadResult()
    {
        Success = false;
        StripIndex = 0;
        ExpectedDecodedByteCount = 0;
        Compression = TiffCompression.Unknown;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDecodedStripReadResult Create(ulong stripIndex, ulong expectedDecodedByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffDecodedStripReadResult value = new TiffDecodedStripReadResult();
        value.Initialize(stripIndex, expectedDecodedByteCount, compression, bytes, diagnostics);
        return value;
    }

    internal void Initialize(ulong stripIndex, ulong expectedDecodedByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        StripIndex = stripIndex;
        ExpectedDecodedByteCount = expectedDecodedByteCount;
        Compression = compression;
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffDecodedStripReadResult Copy()
    {
        return TiffDecodedStripReadResult.Create(StripIndex, ExpectedDecodedByteCount, Compression, Bytes, Diagnostics);
    }
}
