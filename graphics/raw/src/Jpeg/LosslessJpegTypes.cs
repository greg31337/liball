namespace raw;

internal class LosslessJpegPlane
{
    internal byte Identifier;
    internal int Width;
    internal int Height;
    internal int HorizontalSampling;
    internal int VerticalSampling;
    internal int Predictor;
    internal int PointTransform;
    internal ushort[]? Samples;
    internal int WorkingWidth;
    internal int WorkingHeight;
    internal int[]? ReducedSamples;
}

internal class LosslessJpegImage
{
    internal int Width;
    internal int Height;
    internal int Precision;
    internal System.Collections.Generic.List<LosslessJpegPlane> Planes;

    internal LosslessJpegImage()
    {
        Planes = new System.Collections.Generic.List<LosslessJpegPlane>();
    }

    internal LosslessJpegPlane GetPlane(byte identifier)
    {
        int index = 0;
        while (index < Planes.Count)
        {
            if (Planes[index].Identifier == identifier)
            {
                return Planes[index];
            }
            index++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG scan references a component plane that is not present.", -1);
    }
}
