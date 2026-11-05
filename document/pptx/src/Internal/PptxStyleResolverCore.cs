using System.Xml;

namespace pptx;

internal static class PptxStyleResolverCore
{
    internal static PptxColor ResolveColor(PptxTheme theme, PptxColor color)
    {
        if (theme == null)
        {
            throw new System.ArgumentNullException("theme");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        return ResolveColorDepth(theme, color, 0);
    }
    internal static PptxColor ResolveDirectColor(PptxColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        PptxColor resolved = PptxColor.CreateRgb(PptxColorCore.ApplyColorTransforms(PptxColorCore.ResolveBase(color), color));
        resolved.Alpha = PptxColor.ClampPercentage(color.Alpha);
        return resolved;
    }
    internal static PptxColor ResolveColorDepth(PptxTheme theme, PptxColor color, int depth)
    {
        if (depth > 16)
        {
            return PptxColor.CreateRgb("000000");
        }

        string baseRgb = PptxColorCore.ResolveBase(color);
        int alpha = PptxColor.ClampPercentage(color.Alpha);
        if (color.Type == PptxColor.Scheme)
        {
            PptxColor themeColor = theme.GetColor(color.Value);
            PptxColor themeResolved = ResolveColorDepth(theme, themeColor, depth + 1);
            baseRgb = themeResolved.Value;
            alpha = (int)((long)themeResolved.Alpha * alpha / 100000L);
        }

        PptxColor resolved = PptxColor.CreateRgb(PptxColorCore.ApplyColorTransforms(baseRgb, color));
        resolved.Alpha = alpha;
        return resolved;
    }
    internal static PptxFill ResolveFill(PptxTheme theme, PptxFill fill)
    {
        if (fill == null)
        {
            throw new System.ArgumentNullException("fill");
        }

        if (fill.Type == PptxFill.Solid)
        {
            PptxFill resolved = PptxFill.CreateSolid(ResolveColor(theme, fill.Color));
            resolved.Transparency = fill.Transparency;
            return resolved;
        }

        if (fill.Type == PptxFill.Gradient)
        {
            PptxFill resolved = new PptxFill();
            resolved.Type = PptxFill.Gradient;
            resolved.Present = true;
            resolved.GradientAngleUnits = fill.GradientAngleUnits;
            resolved.GradientScaled = fill.GradientScaled;
            resolved.RotateWithShape = fill.RotateWithShape;
            resolved.Transparency = fill.Transparency;
            foreach (PptxGradientStop stop in fill.GradientStops)
            {
                resolved.GradientStops.Add(PptxGradientStop.Create(stop.Position, ResolveColor(theme, stop.Color)));
            }

            if (resolved.GradientStops.Count > 0)
            {
                resolved.Color = resolved.GradientStops[0].Color;
                resolved.SecondaryColor = resolved.GradientStops[resolved.GradientStops.Count - 1].Color;
            }
            else
            {
                resolved.Color = ResolveColor(theme, fill.Color);
                resolved.SecondaryColor = ResolveColor(theme, fill.SecondaryColor);
            }

            return resolved;
        }

        if (fill.Type == PptxFill.Pattern)
        {
            PptxFill resolved = PptxFill.CreatePattern(fill.PatternType, ResolveColor(theme, fill.Color), ResolveColor(theme, fill.SecondaryColor));
            resolved.Transparency = fill.Transparency;
            return resolved;
        }

        if (fill.Type == PptxFill.Picture)
        {
            PptxFill resolved = PptxFill.CreatePicture(fill.RelationshipId);
            resolved.PictureIsLinked = fill.PictureIsLinked;
            resolved.RotateWithShape = fill.RotateWithShape;
            resolved.Transparency = fill.Transparency;
            resolved.Crop.Left = fill.Crop.Left;
            resolved.Crop.Top = fill.Crop.Top;
            resolved.Crop.Right = fill.Crop.Right;
            resolved.Crop.Bottom = fill.Crop.Bottom;
            resolved.UseStretch = fill.UseStretch;
            resolved.UseTile = fill.UseTile;
            resolved.Stretch.Left = fill.Stretch.Left;
            resolved.Stretch.Top = fill.Stretch.Top;
            resolved.Stretch.Right = fill.Stretch.Right;
            resolved.Stretch.Bottom = fill.Stretch.Bottom;
            resolved.Tile.OffsetX = fill.Tile.OffsetX;
            resolved.Tile.OffsetY = fill.Tile.OffsetY;
            resolved.Tile.ScaleX = fill.Tile.ScaleX;
            resolved.Tile.ScaleY = fill.Tile.ScaleY;
            resolved.Tile.Alignment = fill.Tile.Alignment;
            resolved.Tile.Flip = fill.Tile.Flip;
            return resolved;
        }

        if (fill.Type == PptxFill.Group)
        {
            return PptxFill.CreateGroup();
        }

        return PptxFill.CreateNone();
    }
    internal static PptxLine ResolveLine(PptxTheme theme, PptxLine line)
    {
        if (line == null)
        {
            throw new System.ArgumentNullException("line");
        }

        if (line.Type != PptxLine.Solid)
        {
            return PptxLine.CreateNone();
        }

        PptxLine resolved = PptxLine.CreateSolid(ResolveColor(theme, line.Color), line.Width);
        resolved.Dash = line.Dash;
        resolved.Cap = line.Cap;
        resolved.Compound = line.Compound;
        resolved.Alignment = line.Alignment;
        resolved.Join = line.Join;
        resolved.MiterLimit = line.MiterLimit;
        resolved.HeadArrowType = line.HeadArrowType;
        resolved.HeadArrowWidth = line.HeadArrowWidth;
        resolved.HeadArrowLength = line.HeadArrowLength;
        resolved.TailArrowType = line.TailArrowType;
        resolved.TailArrowWidth = line.TailArrowWidth;
        resolved.TailArrowLength = line.TailArrowLength;
        resolved.Transparency = line.Transparency;
        foreach (PptxLineDash dash in line.CustomDashes)
        {
            resolved.CustomDashes.Add(PptxLineDash.Create(dash.DashLength, dash.SpaceLength));
        }

        return resolved;
    }
    internal static PptxEffect ResolveEffect(PptxTheme theme, PptxEffect effect)
    {
        if (effect == null)
        {
            throw new System.ArgumentNullException("effect");
        }

        XmlDocument document = new XmlDocument();
        XmlElement properties = document.CreateElement("a", "spPr", PptxNamespaces.Drawing);
        document.AppendChild(properties);
        properties.AppendChild(effect.ToElement(document));
        effect.AppendThreeD(document, properties);
        PptxEffect resolved = PptxEffect.FromShapeProperties(properties);
        if (resolved.HasShadow)
        {
            resolved.ShadowColor = ResolveColor(theme, resolved.ShadowColor);
        }

        if (resolved.HasGlow)
        {
            resolved.GlowColor = ResolveColor(theme, resolved.GlowColor);
        }

        if (resolved.ThreeD.ExtrusionColor.Type != PptxColor.Empty)
        {
            resolved.ThreeD.ExtrusionColor = ResolveColor(theme, resolved.ThreeD.ExtrusionColor);
        }

        if (resolved.ThreeD.ContourColor.Type != PptxColor.Empty)
        {
            resolved.ThreeD.ContourColor = ResolveColor(theme, resolved.ThreeD.ContourColor);
        }

        return resolved;
    }
    internal static PptxFill ResolveShapeFill(PptxTheme theme, PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (shape.Fill.Present)
        {
            return ResolveFill(theme, shape.Fill);
        }

        if (theme.FillStyleCount() > 0)
        {
            return ResolveFill(theme, theme.GetFillStyle(0));
        }

        return PptxFill.CreateNone();
    }
    internal static PptxLine ResolveShapeLine(PptxTheme theme, PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (shape.Line.Present)
        {
            return ResolveLine(theme, shape.Line);
        }

        if (theme.LineStyleCount() > 0)
        {
            return ResolveLine(theme, theme.GetLineStyle(0));
        }

        return PptxLine.CreateNone();
    }
    internal static string ResolveTextFont(PptxTheme theme, string role)
    {
        if (theme == null)
        {
            throw new System.ArgumentNullException("theme");
        }

        string font = theme.GetFont(role);
        if (font.Length > 0)
        {
            return font;
        }

        return theme.MinorLatinFont;
    }
    internal static PptxColor ResolveTextColor(PptxTheme theme)
    {
        return ResolveColor(theme, PptxColor.CreateScheme("dk1"));
    }

    internal static PptxColor CloneColor(PptxColor color)
    {
        PptxColor clone = new PptxColor();
        clone.Type = color.Type;
        clone.Value = color.Value;
        clone.FallbackValue = color.FallbackValue;
        clone.HslHueAngleUnits = color.HslHueAngleUnits;
        clone.HslSaturation = color.HslSaturation;
        clone.HslLuminance = color.HslLuminance;
        clone.Alpha = color.Alpha;
        clone.Tint = color.Tint;
        clone.Shade = color.Shade;
        clone.Luminance = color.Luminance;
        clone.LuminanceModulation = color.LuminanceModulation;
        clone.LuminanceOffset = color.LuminanceOffset;
        clone.Saturation = color.Saturation;
        clone.SaturationModulation = color.SaturationModulation;
        clone.SaturationOffset = color.SaturationOffset;
        clone.HueAngleUnits = color.HueAngleUnits;
        clone.HueModulation = color.HueModulation;
        clone.HueOffsetAngleUnits = color.HueOffsetAngleUnits;
        clone.Complement = color.Complement;
        clone.Inverse = color.Inverse;
        clone.Grayscale = color.Grayscale;
        foreach (string xml in color.UnsupportedTransformXml)
        {
            clone.UnsupportedTransformXml.Add(xml);
        }

        return clone;
    }

    internal static string ApplyTransforms(string rgb, int tint, int shade)
    {
        PptxColor color = PptxColor.CreateRgb(rgb);
        color.Tint = tint;
        color.Shade = shade;
        return PptxColorCore.ApplyColorTransforms(rgb, color);
    }

    internal static int HexChannel(string rgb, int start)
    {
        return System.Convert.ToInt32(rgb.Substring(start, 2), 16);
    }

    internal static int ApplyTint(int channel, int tint)
    {
        return channel + ((255 - channel) * tint / 100000);
    }

    internal static int ApplyShade(int channel, int shade)
    {
        return channel * shade / 100000;
    }

    internal static string ChannelHex(int channel)
    {
        if (channel < 0)
        {
            channel = 0;
        }

        if (channel > 255)
        {
            channel = 255;
        }

        return channel.ToString("X2");
    }
}
