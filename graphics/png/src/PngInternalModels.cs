namespace png;

internal class PngIhdr
{
    internal int Width;
    internal int Height;
    internal PngBitDepth BitDepth;
    internal PngColorType ColorType;
    internal PngCompressionMethod CompressionMethod;
    internal PngFilterMethod FilterMethod;
    internal PngInterlaceMethod InterlaceMethod;
}

internal class PngIend
{
    internal bool IsEnd = true;
}

internal class PngScanlineLayout
{
    internal int Width;
    internal int Height;
    internal PngColorType ColorType;
    internal PngBitDepth BitDepth;
    internal int SamplesPerPixel;
    internal int BitsPerSample;
    internal int BitsPerPixel;
    internal int FilterBytesPerPixel;
    internal long RowDataBytes;
    internal long FilteredRowBytes;
    internal long ImageDataBytes;
    internal long FilteredImageBytes;
}

internal class PngFilterTrial
{
    internal PngFilterType FilterType;
    internal byte[] FilteredRow = new byte[0];
    internal long Score;
}

internal class PngFilterTrialResult
{
    internal PngFilterTrial[] Trials = new PngFilterTrial[0];
    internal PngFilterTrial BestTrial = new PngFilterTrial();
    internal bool TriedAllFilters;
    internal bool UsedFixedFilter;
}

internal class PngAdam7PassGeometry
{
    internal int PassIndex;
    internal int XStart;
    internal int YStart;
    internal int XStep;
    internal int YStep;
    internal int Width;
    internal int Height;
    internal PngColorType ColorType;
    internal PngBitDepth BitDepth;
    internal int BitsPerPixel;
    internal int FilterBytesPerPixel;
    internal long RowDataBytes;
    internal long FilteredRowBytes;
    internal long ImageDataBytes;
    internal long FilteredImageBytes;
    internal bool IsEmpty;
}
