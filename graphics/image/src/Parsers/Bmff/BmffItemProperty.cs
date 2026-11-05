namespace image;

internal class BmffItemProperty
{
    internal int Index;
    internal string Type;
    internal BmffBox Box;
    internal int Width;
    internal int Height;
    internal int ChannelCount;
    internal int BitDepth;
    internal int RotationDegrees;
    internal int MirrorAxis;
    internal string ColorType;
    internal int ColorPrimaries;
    internal int TransferCharacteristics;
    internal int MatrixCoefficients;
    internal bool FullRange;
    internal string AuxiliaryType;
    internal uint CleanWidthNumerator;
    internal uint CleanWidthDenominator;
    internal uint CleanHeightNumerator;
    internal uint CleanHeightDenominator;
    internal int HorizontalOffsetNumerator;
    internal uint HorizontalOffsetDenominator;
    internal int VerticalOffsetNumerator;
    internal uint VerticalOffsetDenominator;
    internal uint HorizontalSpacing;
    internal uint VerticalSpacing;
    internal byte[] ProfileData;
    internal bool ProfileExtracted;

    internal BmffItemProperty()
    {
        Index = 0;
        Type = "";
        Box = new BmffBox();
        Width = 0;
        Height = 0;
        ChannelCount = 0;
        BitDepth = 0;
        RotationDegrees = 0;
        MirrorAxis = -1;
        ColorType = "";
        ColorPrimaries = 0;
        TransferCharacteristics = 0;
        MatrixCoefficients = 0;
        FullRange = false;
        AuxiliaryType = "";
        CleanWidthNumerator = 0;
        CleanWidthDenominator = 0;
        CleanHeightNumerator = 0;
        CleanHeightDenominator = 0;
        HorizontalOffsetNumerator = 0;
        HorizontalOffsetDenominator = 0;
        VerticalOffsetNumerator = 0;
        VerticalOffsetDenominator = 0;
        HorizontalSpacing = 0;
        VerticalSpacing = 0;
        ProfileData = new byte[0];
        ProfileExtracted = false;
    }
}
