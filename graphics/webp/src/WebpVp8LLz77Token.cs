namespace webp;

internal class WebpVp8LLz77Token
{
    internal bool IsLiteral;
    internal WebpVp8LColor Color;
    internal int Length;
    internal int Distance;
    internal int DistanceCode;

    internal WebpVp8LLz77Token()
    {
        IsLiteral = true;
        Color = new WebpVp8LColor();
        Length = 1;
        Distance = 0;
        DistanceCode = 0;
    }
}
