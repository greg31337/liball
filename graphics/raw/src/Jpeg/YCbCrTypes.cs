namespace raw;

internal enum YCbCrChromaPositioning
{
    Centered = 0,
    Cosited = 1
}

internal enum YCbCrUpsamplingMode
{
    NearestNeighbor = 0,
    Bilinear = 1
}

internal class YCbCrConversionParameters
{
    internal double RedLumaWeight;
    internal double BlueLumaWeight;
    internal double YBlack;
    internal double YWhite;
    internal double CbBlack;
    internal double CbWhite;
    internal double CrBlack;
    internal double CrWhite;
    internal YCbCrChromaPositioning Positioning;
    internal YCbCrUpsamplingMode Upsampling;

    internal YCbCrConversionParameters(double redLumaWeight, double blueLumaWeight, double yBlack, double yWhite, double cbBlack, double cbWhite, double crBlack, double crWhite, YCbCrChromaPositioning positioning, YCbCrUpsamplingMode upsampling)
    {
        RedLumaWeight = redLumaWeight;
        BlueLumaWeight = blueLumaWeight;
        YBlack = yBlack;
        YWhite = yWhite;
        CbBlack = cbBlack;
        CbWhite = cbWhite;
        CrBlack = crBlack;
        CrWhite = crWhite;
        Positioning = positioning;
        Upsampling = upsampling;
    }
}

internal class YCbCrPlaneData
{
    internal int Width;
    internal int Height;
    internal byte[]? ByteSamples;
    internal ushort[]? WordSamples;

    internal int GetSample(int index)
    {
        if (ByteSamples != null)
        {
            return ByteSamples[index];
        }
        return (WordSamples ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
    }

    internal long GetSampleCount()
    {
        return CheckedMath.Multiply(Width, Height);
    }
}

internal class YCbCrImageData
{
    internal int Width;
    internal int Height;
    internal int Precision;
    internal int HorizontalSubsampling;
    internal int VerticalSubsampling;
    internal YCbCrPlaneData? Y;
    internal YCbCrPlaneData? Cb;
    internal YCbCrPlaneData? Cr;
}

internal class RgbByteImage
{
    internal int Width;
    internal int Height;
    internal byte[]? Samples;
}
