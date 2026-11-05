namespace qrcode;

internal class FinderGroup
{
    internal FinderCandidate TopLeft;

    internal FinderCandidate TopRight;

    internal FinderCandidate BottomLeft;

    internal QrPoint EstimatedBottomRight = new QrPoint();

    internal QrSymbolKind EstimatedSymbolKind = QrSymbolKind.Model2;

    internal bool Model2Possible;

    internal bool LegacyModel1Possible;

    internal int EstimatedVersion;

    internal int EstimatedDimension;

    internal double ModuleSize;

    internal double HorizontalDistance;

    internal double VerticalDistance;

    internal double RightAngleCosine;

    internal double OrientationRadians;

    internal double Confidence;

    internal QrPolarity Polarity = QrPolarity.Automatic;

    internal FinderGroup(
        FinderCandidate topLeft,
        FinderCandidate topRight,
        FinderCandidate bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
    }
}
