namespace tiff;

internal class TiffRegionReadResult
{
    internal bool Success;
    internal ulong Column;
    internal ulong Row;
    internal ulong Width;
    internal ulong Length;
    internal int SampleIndex;
    internal ulong RowByteCount;
    internal ulong TotalByteCount;
    internal TiffCompression Compression;
    internal byte[] Bytes;
    internal TiffDiagnostics Diagnostics;

    internal TiffRegionReadResult()
    {
        Success = false;
        Column = 0;
        Row = 0;
        Width = 0;
        Length = 0;
        SampleIndex = 0;
        RowByteCount = 0;
        TotalByteCount = 0;
        Compression = TiffCompression.Unknown;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRegionReadResult Create(ulong column, ulong row, ulong width, ulong length, int sampleIndex, ulong rowByteCount, ulong totalByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffRegionReadResult value = new TiffRegionReadResult();
        value.Initialize(column, row, width, length, sampleIndex, rowByteCount, totalByteCount, compression, bytes, diagnostics);
        return value;
    }

    internal void Initialize(ulong column, ulong row, ulong width, ulong length, int sampleIndex, ulong rowByteCount, ulong totalByteCount, TiffCompression compression, byte[] bytes, TiffDiagnostics diagnostics)
    {
        Column = column;
        Row = row;
        Width = width;
        Length = length;
        SampleIndex = sampleIndex;
        RowByteCount = rowByteCount;
        TotalByteCount = totalByteCount;
        Compression = compression;
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    internal TiffRegionReadResult Copy()
    {
        return TiffRegionReadResult.Create(Column, Row, Width, Length, SampleIndex, RowByteCount, TotalByteCount, Compression, Bytes, Diagnostics);
    }
}
