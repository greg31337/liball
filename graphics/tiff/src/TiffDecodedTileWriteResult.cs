namespace tiff;

internal class TiffDecodedTileWriteResult
{
    internal bool Success;
    internal ulong TileIndex;
    internal ulong ExpectedDecodedByteCount;
    internal TiffCompression Compression;
    internal ulong BytesWritten;
    internal TiffDiagnostics Diagnostics;

    internal TiffDecodedTileWriteResult()
    {
        Success = false;
        TileIndex = 0;
        ExpectedDecodedByteCount = 0;
        Compression = TiffCompression.Unknown;
        BytesWritten = 0;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDecodedTileWriteResult Create(ulong tileIndex, ulong expectedDecodedByteCount, TiffCompression compression, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TiffDecodedTileWriteResult value = new TiffDecodedTileWriteResult();
        value.Initialize(tileIndex, expectedDecodedByteCount, compression, bytesWritten, diagnostics);
        return value;
    }

    internal void Initialize(ulong tileIndex, ulong expectedDecodedByteCount, TiffCompression compression, ulong bytesWritten, TiffDiagnostics diagnostics)
    {
        TileIndex = tileIndex;
        ExpectedDecodedByteCount = expectedDecodedByteCount;
        Compression = compression;
        BytesWritten = bytesWritten;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffDecodedTileWriteResult Copy()
    {
        return TiffDecodedTileWriteResult.Create(TileIndex, ExpectedDecodedByteCount, Compression, BytesWritten, Diagnostics);
    }
}
