namespace svg;

internal class SvgFontMatchRank
{
    internal int Category;
    internal double Distance;

    internal SvgFontMatchRank(int category, double distance)
    {
        Category = category;
        Distance = distance;
    }
}
