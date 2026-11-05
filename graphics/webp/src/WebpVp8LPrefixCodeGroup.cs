namespace webp;

internal class WebpVp8LPrefixCodeGroup
{
    internal WebpVp8LPrefixCodeTable Green;
    internal WebpVp8LPrefixCodeTable Red;
    internal WebpVp8LPrefixCodeTable Blue;
    internal WebpVp8LPrefixCodeTable Alpha;
    internal WebpVp8LPrefixCodeTable Distance;

    internal WebpVp8LPrefixCodeGroup()
    {
        Green = new WebpVp8LPrefixCodeTable();
        Red = new WebpVp8LPrefixCodeTable();
        Blue = new WebpVp8LPrefixCodeTable();
        Alpha = new WebpVp8LPrefixCodeTable();
        Distance = new WebpVp8LPrefixCodeTable();
    }
}
