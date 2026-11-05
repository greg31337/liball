using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace pdf;

internal class PdfSvgStyle
{
    internal string Fill;
    internal string Stroke;
    internal double StrokeWidth;
    internal bool FillRuleEvenOdd;
    internal bool ClipRuleEvenOdd;
    internal double FillOpacity;
    internal double StrokeOpacity;
    internal double Opacity;
    internal double FontSize;
    internal bool FontBold;
    internal bool FontItalic;
    internal PdfLineCap LineCap;
    internal PdfLineJoin LineJoin;
    internal double[] DashPattern;
    internal double DashOffset;
    internal string TextAnchor;
    internal bool Visible;

    internal PdfSvgStyle()
    {
        Fill = "black";
        Stroke = "none";
        StrokeWidth = 1;
        FillOpacity = 1;
        StrokeOpacity = 1;
        Opacity = 1;
        FontSize = 16;
        LineCap = PdfLineCap.Butt;
        LineJoin = PdfLineJoin.Miter;
        DashPattern = new double[0];
        TextAnchor = "start";
        Visible = true;
    }

    internal PdfSvgStyle Copy()
    {
        PdfSvgStyle copied = new PdfSvgStyle();
        copied.Fill = Fill;
        copied.Stroke = Stroke;
        copied.StrokeWidth = StrokeWidth;
        copied.FillRuleEvenOdd = FillRuleEvenOdd;
        copied.ClipRuleEvenOdd = ClipRuleEvenOdd;
        copied.FillOpacity = FillOpacity;
        copied.StrokeOpacity = StrokeOpacity;
        copied.Opacity = Opacity;
        copied.FontSize = FontSize;
        copied.FontBold = FontBold;
        copied.FontItalic = FontItalic;
        copied.LineCap = LineCap;
        copied.LineJoin = LineJoin;
        copied.DashPattern = DashPattern;
        copied.DashOffset = DashOffset;
        copied.TextAnchor = TextAnchor;
        copied.Visible = Visible;
        return copied;
    }
}

internal static class PdfSvgStyleParser
{
    internal static PdfSvgStyle Parse(XmlElement element, PdfSvgStyle inherited)
    {
        RejectUnsafeAttributes(element);
        PdfSvgStyle style = inherited.Copy();
        ApplyAttribute(element, "fill", style);
        ApplyAttribute(element, "stroke", style);
        ApplyAttribute(element, "stroke-width", style);
        ApplyAttribute(element, "fill-rule", style);
        ApplyAttribute(element, "clip-rule", style);
        ApplyAttribute(element, "fill-opacity", style);
        ApplyAttribute(element, "stroke-opacity", style);
        ApplyAttribute(element, "opacity", style);
        ApplyAttribute(element, "font-size", style);
        ApplyAttribute(element, "font-family", style);
        ApplyAttribute(element, "font-weight", style);
        ApplyAttribute(element, "font-style", style);
        ApplyAttribute(element, "stroke-linecap", style);
        ApplyAttribute(element, "stroke-linejoin", style);
        ApplyAttribute(element, "stroke-dasharray", style);
        ApplyAttribute(element, "stroke-dashoffset", style);
        ApplyAttribute(element, "text-anchor", style);
        ApplyAttribute(element, "display", style);
        ApplyAttribute(element, "visibility", style);

        if (element.HasAttribute("style"))
        {
            string[] declarations = element.GetAttribute("style").Split(';');
            for (int index = 0; index < declarations.Length; index++)
            {
                string declaration = declarations[index].Trim();
                if (declaration.Length == 0)
                {
                    continue;
                }

                int separator = declaration.IndexOf(':');
                if (separator <= 0 || separator == declaration.Length - 1)
                {
                    throw new InvalidDataException("SVG style declaration is invalid.");
                }

                string name = declaration.Substring(0, separator).Trim().ToLowerInvariant();
                string value = declaration.Substring(separator + 1).Trim();
                ApplyProperty(name, value, style);
            }
        }

        return style;
    }

    internal static void ApplyAttribute(XmlElement element, string name, PdfSvgStyle style)
    {
        if (element.HasAttribute(name))
        {
            ApplyProperty(name, element.GetAttribute(name), style);
        }
    }

    internal static void ApplyProperty(string name, string value, PdfSvgStyle style)
    {
        string normalized = value.Trim();
        if (name == "fill")
        {
            ValidatePaint(normalized);
            style.Fill = normalized;
        }
        else if (name == "stroke")
        {
            ValidatePaint(normalized);
            style.Stroke = normalized;
        }
        else if (name == "stroke-width")
        {
            style.StrokeWidth = PdfSvgValueParser.ParseLengthUserUnits(normalized);
            if (style.StrokeWidth < 0)
            {
                throw new InvalidDataException("SVG stroke width cannot be negative.");
            }
        }
        else if (name == "fill-rule")
        {
            style.FillRuleEvenOdd = ParseRule(normalized);
        }
        else if (name == "clip-rule")
        {
            style.ClipRuleEvenOdd = ParseRule(normalized);
        }
        else if (name == "fill-opacity")
        {
            style.FillOpacity = ParseOpacity(normalized);
        }
        else if (name == "stroke-opacity")
        {
            style.StrokeOpacity = ParseOpacity(normalized);
        }
        else if (name == "opacity")
        {
            style.Opacity = ParseOpacity(normalized);
        }
        else if (name == "font-size")
        {
            style.FontSize = PdfSvgValueParser.ParseLengthUserUnits(normalized);
            if (style.FontSize <= 0)
            {
                throw new InvalidDataException("SVG font size must be positive.");
            }
        }
        else if (name == "font-weight")
        {
            style.FontBold = normalized == "bold" || normalized == "700" || normalized == "800" || normalized == "900";
            if (!style.FontBold && normalized != "normal" && normalized != "400")
            {
                throw new InvalidDataException("SVG font weight is unsupported.");
            }
        }
        else if (name == "font-style")
        {
            style.FontItalic = normalized == "italic" || normalized == "oblique";
            if (!style.FontItalic && normalized != "normal")
            {
                throw new InvalidDataException("SVG font style is unsupported.");
            }
        }
        else if (name == "font-family")
        {
            if (normalized.Length == 0)
            {
                throw new InvalidDataException("SVG font family cannot be empty.");
            }
        }
        else if (name == "stroke-linecap")
        {
            style.LineCap = ParseLineCap(normalized);
        }
        else if (name == "stroke-linejoin")
        {
            style.LineJoin = ParseLineJoin(normalized);
        }
        else if (name == "stroke-dasharray")
        {
            style.DashPattern = ParseDashArray(normalized);
        }
        else if (name == "stroke-dashoffset")
        {
            style.DashOffset = PdfSvgValueParser.ParseLengthUserUnits(normalized);
            if (style.DashOffset < 0)
            {
                throw new InvalidDataException("SVG negative dash offsets are unsupported.");
            }
        }
        else if (name == "text-anchor")
        {
            if (normalized != "start" && normalized != "middle" && normalized != "end")
            {
                throw new InvalidDataException("SVG text anchor is unsupported.");
            }

            style.TextAnchor = normalized;
        }
        else if (name == "display")
        {
            if (normalized == "none")
            {
                style.Visible = false;
            }
            else if (normalized != "inline")
            {
                throw new InvalidDataException("SVG display mode is unsupported.");
            }
        }
        else if (name == "visibility")
        {
            if (normalized == "hidden" || normalized == "collapse")
            {
                style.Visible = false;
            }
            else if (normalized != "visible")
            {
                throw new InvalidDataException("SVG visibility mode is unsupported.");
            }
        }
        else
        {
            throw new InvalidDataException("SVG style property is unsupported: " + name + ".");
        }
    }

    internal static bool ParseRule(string value)
    {
        if (value == "evenodd")
        {
            return true;
        }

        if (value == "nonzero")
        {
            return false;
        }

        throw new InvalidDataException("SVG fill or clip rule is unsupported.");
    }

    internal static double ParseOpacity(string value)
    {
        string normalized = value.Trim();
        double opacity;
        if (normalized.EndsWith("%", StringComparison.Ordinal))
        {
            opacity = double.Parse(normalized.Substring(0, normalized.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture) / 100;
        }
        else
        {
            opacity = double.Parse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        if (opacity < 0 || opacity > 1 || double.IsNaN(opacity) || double.IsInfinity(opacity))
        {
            throw new InvalidDataException("SVG opacity must be between zero and one.");
        }

        return opacity;
    }

    internal static PdfLineCap ParseLineCap(string value)
    {
        if (value == "round")
        {
            return PdfLineCap.Round;
        }

        if (value == "square")
        {
            return PdfLineCap.Square;
        }

        if (value == "butt")
        {
            return PdfLineCap.Butt;
        }

        throw new InvalidDataException("SVG stroke line cap is unsupported.");
    }

    internal static PdfLineJoin ParseLineJoin(string value)
    {
        if (value == "round")
        {
            return PdfLineJoin.Round;
        }

        if (value == "bevel")
        {
            return PdfLineJoin.Bevel;
        }

        if (value == "miter")
        {
            return PdfLineJoin.Miter;
        }

        throw new InvalidDataException("SVG stroke line join is unsupported.");
    }

    internal static double[] ParseDashArray(string value)
    {
        if (value == "none")
        {
            return new double[0];
        }

        double[] values = PdfSvgValueParser.ParseNumberList(value);
        if (values.Length == 0)
        {
            throw new InvalidDataException("SVG stroke dash array cannot be empty.");
        }

        for (int index = 0; index < values.Length; index++)
        {
            if (values[index] < 0)
            {
                throw new InvalidDataException("SVG stroke dash values cannot be negative.");
            }
        }

        return values;
    }

    internal static void ValidatePaint(string value)
    {
        if (value.IndexOf("url(", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidDataException("SVG paint servers and external paint references are unsupported.");
        }

        if (value != "none" && value != "transparent")
        {
            ParseColor(value);
        }
    }

    internal static PdfColor ParseColor(string value)
    {
        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.StartsWith("#", StringComparison.Ordinal))
        {
            string hex = normalized.Substring(1);
            if (hex.Length == 3)
            {
                int red = int.Parse(new string(hex[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int green = int.Parse(new string(hex[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int blue = int.Parse(new string(hex[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return PdfColor.Rgb(red / 255.0, green / 255.0, blue / 255.0);
            }

            if (hex.Length == 6)
            {
                int red = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int green = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int blue = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return PdfColor.Rgb(red / 255.0, green / 255.0, blue / 255.0);
            }

            throw new InvalidDataException("SVG hexadecimal color must use three or six digits.");
        }

        if (normalized.StartsWith("rgb(", StringComparison.Ordinal) && normalized.EndsWith(")", StringComparison.Ordinal))
        {
            string[] components = normalized.Substring(4, normalized.Length - 5).Split(',');
            if (components.Length != 3)
            {
                throw new InvalidDataException("SVG rgb color must contain three components.");
            }

            double red = ParseColorComponent(components[0]);
            double green = ParseColorComponent(components[1]);
            double blue = ParseColorComponent(components[2]);
            return PdfColor.Rgb(red, green, blue);
        }

        if (normalized == "black")
        {
            return PdfColor.Rgb(0, 0, 0);
        }

        if (normalized == "white")
        {
            return PdfColor.Rgb(1, 1, 1);
        }

        if (normalized == "red")
        {
            return PdfColor.Rgb(1, 0, 0);
        }

        if (normalized == "green")
        {
            return PdfColor.Rgb(0, 0.501960784314, 0);
        }

        if (normalized == "blue")
        {
            return PdfColor.Rgb(0, 0, 1);
        }

        if (normalized == "yellow")
        {
            return PdfColor.Rgb(1, 1, 0);
        }

        if (normalized == "cyan" || normalized == "aqua")
        {
            return PdfColor.Rgb(0, 1, 1);
        }

        if (normalized == "magenta" || normalized == "fuchsia")
        {
            return PdfColor.Rgb(1, 0, 1);
        }

        if (normalized == "gray" || normalized == "grey")
        {
            return PdfColor.Rgb(0.501960784314, 0.501960784314, 0.501960784314);
        }

        throw new InvalidDataException("SVG color is unsupported.");
    }

    internal static double ParseColorComponent(string value)
    {
        string normalized = value.Trim();
        double component;
        if (normalized.EndsWith("%", StringComparison.Ordinal))
        {
            component = double.Parse(normalized.Substring(0, normalized.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture) / 100;
        }
        else
        {
            component = double.Parse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture) / 255;
        }

        if (component < 0 || component > 1)
        {
            throw new InvalidDataException("SVG color component is outside its supported range.");
        }

        return component;
    }

    internal static void RejectUnsafeAttributes(XmlElement element)
    {
        foreach (XmlAttribute attribute in element.Attributes)
        {
            string name = attribute.LocalName.ToLowerInvariant();
            if (name.StartsWith("on", StringComparison.Ordinal) || name == "href" || name == "filter" || name == "mask" || name.StartsWith("marker", StringComparison.Ordinal))
            {
                throw new InvalidDataException("SVG element contains an unsafe or unsupported reference attribute.");
            }

            if (attribute.Value.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) >= 0 || attribute.Value.IndexOf("data:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidDataException("SVG element contains an unsafe resource value.");
            }
        }
    }
}
