namespace raw;

internal class BaselineJpegPlane
{
    internal byte Identifier;
    internal int Width;
    internal int Height;
    internal int HorizontalSampling;
    internal int VerticalSampling;
    internal byte[]? Samples;
}

internal class BaselineJpegImage
{
    internal int Width;
    internal int Height;
    internal int Precision;
    internal System.Collections.Generic.List<BaselineJpegPlane> Planes;

    internal BaselineJpegImage()
    {
        Planes = new System.Collections.Generic.List<BaselineJpegPlane>();
    }

    internal BaselineJpegPlane GetPlane(byte identifier)
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
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG scan references a component plane that is not present.", -1);
    }
}
