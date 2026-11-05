namespace webp;

internal class WebpVp8LEntropyCodeGroup
{
    internal WebpVp8LEntropyCode Green;
    internal WebpVp8LEntropyCode Red;
    internal WebpVp8LEntropyCode Blue;
    internal WebpVp8LEntropyCode Alpha;
    internal WebpVp8LEntropyCode Distance;

    internal WebpVp8LEntropyCodeGroup()
    {
        Green = new WebpVp8LEntropyCode();
        Red = new WebpVp8LEntropyCode();
        Blue = new WebpVp8LEntropyCode();
        Alpha = new WebpVp8LEntropyCode();
        Distance = new WebpVp8LEntropyCode();
    }
}
