namespace image;

internal class SvgViewBox
{
    internal string Text;
    internal double MinX;
    internal double MinY;
    internal double Width;
    internal double Height;
    internal bool Valid;

    internal SvgViewBox()
    {
        Text = "";
        MinX = 0;
        MinY = 0;
        Width = 0;
        Height = 0;
        Valid = false;
    }
}
