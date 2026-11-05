namespace pdf;

internal class PdfGraphicsState
{
    internal string Name;
    internal bool HasFillOpacity;
    internal double FillOpacity;
    internal bool HasStrokeOpacity;
    internal double StrokeOpacity;
    internal bool HasBlendMode;
    internal PdfBlendMode BlendMode;

    internal PdfGraphicsState(string name, bool hasFillOpacity, double fillOpacity, bool hasStrokeOpacity, double strokeOpacity, bool hasBlendMode, PdfBlendMode blendMode)
    {
        Name = name;
        HasFillOpacity = hasFillOpacity;
        FillOpacity = fillOpacity;
        HasStrokeOpacity = hasStrokeOpacity;
        StrokeOpacity = strokeOpacity;
        HasBlendMode = hasBlendMode;
        BlendMode = blendMode;
    }

    internal string GetBlendModeName()
    {
        if (BlendMode == PdfBlendMode.Normal)
        {
            return "Normal";
        }

        if (BlendMode == PdfBlendMode.Multiply)
        {
            return "Multiply";
        }

        if (BlendMode == PdfBlendMode.Screen)
        {
            return "Screen";
        }

        if (BlendMode == PdfBlendMode.Overlay)
        {
            return "Overlay";
        }

        if (BlendMode == PdfBlendMode.Darken)
        {
            return "Darken";
        }

        if (BlendMode == PdfBlendMode.Lighten)
        {
            return "Lighten";
        }

        if (BlendMode == PdfBlendMode.ColorDodge)
        {
            return "ColorDodge";
        }

        if (BlendMode == PdfBlendMode.ColorBurn)
        {
            return "ColorBurn";
        }

        if (BlendMode == PdfBlendMode.HardLight)
        {
            return "HardLight";
        }

        if (BlendMode == PdfBlendMode.SoftLight)
        {
            return "SoftLight";
        }

        if (BlendMode == PdfBlendMode.Difference)
        {
            return "Difference";
        }

        if (BlendMode == PdfBlendMode.Exclusion)
        {
            return "Exclusion";
        }

        if (BlendMode == PdfBlendMode.Hue)
        {
            return "Hue";
        }

        if (BlendMode == PdfBlendMode.Saturation)
        {
            return "Saturation";
        }

        if (BlendMode == PdfBlendMode.Color)
        {
            return "Color";
        }

        return "Luminosity";
    }
}
