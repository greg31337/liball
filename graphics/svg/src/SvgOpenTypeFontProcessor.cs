namespace svg;

internal class SvgOpenTypeProcessorState
{
    internal SvgFont Font;
    internal SvgOpenTypeProfileOptions Options;
    internal SvgOpenTypeFontProfile Profile;

    internal SvgOpenTypeProcessorState(SvgFont font, SvgOpenTypeProfileOptions options, SvgOpenTypeFontProfile profile)
    {
        Font = font;
        Options = options;
        Profile = profile;
    }
}

internal class SvgOpenTypeCoordinateState
{
    internal System.Collections.Generic.List<SvgOpenTypeNormalizedCoordinate> Coordinates;
    internal double[] Values;

    internal SvgOpenTypeCoordinateState(int count)
    {
        Coordinates = new System.Collections.Generic.List<SvgOpenTypeNormalizedCoordinate>();
        Values = new double[count];
    }
}
