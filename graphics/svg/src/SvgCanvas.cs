namespace svg;

internal class SvgCanvasState
{
    internal SvgMatrix Transform;
    internal string Fill;
    internal string Stroke;
    internal double StrokeWidth;
    internal double Opacity;
    internal string Clip;
    internal string Mask;
    internal string Filter;
    internal string FontFamily;
    internal double FontSize;
    internal string FontStyle;
    internal string FontWeight;
    internal string TextAnchor;

    internal SvgCanvasState()
    {
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        Fill = "black";
        Stroke = "black";
        StrokeWidth = 1;
        Opacity = 1;
        Clip = "";
        Mask = "";
        Filter = "";
        FontFamily = "sans-serif";
        FontSize = 10;
        FontStyle = "normal";
        FontWeight = "normal";
        TextAnchor = "start";
    }

    internal SvgCanvasState Copy()
    {
        SvgCanvasState copy = new SvgCanvasState();
        copy.Transform = new SvgMatrix(Transform.A, Transform.B, Transform.C, Transform.D, Transform.E, Transform.F);
        copy.Fill = Fill;
        copy.Stroke = Stroke;
        copy.StrokeWidth = StrokeWidth;
        copy.Opacity = Opacity;
        copy.Clip = Clip;
        copy.Mask = Mask;
        copy.Filter = Filter;
        copy.FontFamily = FontFamily;
        copy.FontSize = FontSize;
        copy.FontStyle = FontStyle;
        copy.FontWeight = FontWeight;
        copy.TextAnchor = TextAnchor;
        return copy;
    }
}
