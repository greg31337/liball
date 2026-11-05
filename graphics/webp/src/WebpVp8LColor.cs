namespace webp;

internal class WebpVp8LColor
{
    internal byte Red;
    internal byte Green;
    internal byte Blue;
    internal byte Alpha;

    internal WebpVp8LColor()
    {
        Red = 0;
        Green = 0;
        Blue = 0;
        Alpha = 0;
    }

    internal uint ToArgb()
    {
        uint value = (uint)Alpha << 24;
        value = value | ((uint)Red << 16);
        value = value | ((uint)Green << 8);
        value = value | Blue;
        return value;
    }

    internal WebpVp8LColor Copy()
    {
        WebpVp8LColor copy = new WebpVp8LColor();
        copy.Red = Red;
        copy.Green = Green;
        copy.Blue = Blue;
        copy.Alpha = Alpha;
        return copy;
    }
}
