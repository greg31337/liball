namespace tiff;

internal class TiffDecodedTileReadResult
{
    internal bool Success;
    internal ulong TileIndex;
    internal ulong ExpectedDecodedByteCount;
    internal TiffCompression Compression;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffDecodedTileReadResult()
    {
        Success = false;
        TileIndex = 0;
        ExpectedDecodedByteCount = 0;
        Compression = TiffCompression.Unknown;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffDecodedTileReadResult Create(ulong tileIndex, ulong expectedDecodedByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffDecodedTileReadResult value = new TiffDecodedTileReadResult();
        value.Initialize(tileIndex, expectedDecodedByteCount, compression, bytes, diagnostics);
        return value;
    }

    internal void Initialize(ulong tileIndex, ulong expectedDecodedByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TileIndex = tileIndex;
        ExpectedDecodedByteCount = expectedDecodedByteCount;
        Compression = compression;
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffDecodedTileReadResult Copy()
    {
        return TiffDecodedTileReadResult.Create(TileIndex, ExpectedDecodedByteCount, Compression, Bytes, Diagnostics);
    }
}
