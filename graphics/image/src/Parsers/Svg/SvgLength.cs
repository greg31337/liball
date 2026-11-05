namespace image;

internal class SvgLength
{
    internal string Text;
    internal double Pixels;
    internal string Unit;
    internal bool Valid;
    internal bool Resolved;

    internal SvgLength()
    {
        Text = "";
        Pixels = 0;
        Unit = "";
        Valid = false;
        Resolved = false;
    }
}
