using System.Collections.Generic;

namespace pptx;

internal static class PptxColorCore
{
    internal static readonly Dictionary<string, string> PresetColors = CreatePresetColors();

    internal static string ResolveBase(PptxColor color)
    {
        if (color.Type == PptxColor.Preset)
        {
            string name = PptxDiagnostic.CleanText(color.Value);
            if (PresetColors.ContainsKey(name))
            {
                return PresetColors[name];
            }

            return "000000";
        }

        if (color.Type == PptxColor.System)
        {
            if (IsRgb(color.FallbackValue))
            {
                return PptxColor.NormalizeRgb(color.FallbackValue);
            }

            return ResolveSystemColor(color.Value);
        }

        if (color.Type == PptxColor.Hsl)
        {
            return HslToRgb(color.HslHueAngleUnits / 60000.0, color.HslSaturation / 100000.0, color.HslLuminance / 100000.0);
        }

        if (IsRgb(color.Value))
        {
            return PptxColor.NormalizeRgb(color.Value);
        }

        return "000000";
    }

    internal static string ApplyColorTransforms(string rgb, PptxColor color)
    {
        string clean = ResolveRgb(rgb);
        int red = PptxStyleResolverCore.HexChannel(clean, 0);
        int green = PptxStyleResolverCore.HexChannel(clean, 2);
        int blue = PptxStyleResolverCore.HexChannel(clean, 4);

        if (color.Tint >= 0)
        {
            red = PptxStyleResolverCore.ApplyTint(red, PptxColor.ClampPercentage(color.Tint));
            green = PptxStyleResolverCore.ApplyTint(green, PptxColor.ClampPercentage(color.Tint));
            blue = PptxStyleResolverCore.ApplyTint(blue, PptxColor.ClampPercentage(color.Tint));
        }

        if (color.Shade >= 0)
        {
            red = PptxStyleResolverCore.ApplyShade(red, PptxColor.ClampPercentage(color.Shade));
            green = PptxStyleResolverCore.ApplyShade(green, PptxColor.ClampPercentage(color.Shade));
            blue = PptxStyleResolverCore.ApplyShade(blue, PptxColor.ClampPercentage(color.Shade));
        }

        double[] hsl = RgbToHsl(red, green, blue);
        if (color.HueAngleUnits >= 0)
        {
            hsl[0] = color.HueAngleUnits / 60000.0;
        }

        hsl[0] = hsl[0] * PptxColor.ClampPercentage(color.HueModulation) / 100000.0;
        hsl[0] = hsl[0] + color.HueOffsetAngleUnits / 60000.0;
        if (color.Saturation >= 0)
        {
            hsl[1] = PptxColor.ClampPercentage(color.Saturation) / 100000.0;
        }

        hsl[1] = hsl[1] * PptxColor.ClampPercentage(color.SaturationModulation) / 100000.0;
        hsl[1] = ClampUnit(hsl[1] + PptxColor.ClampSignedPercentage(color.SaturationOffset) / 100000.0);
        if (color.Luminance >= 0)
        {
            hsl[2] = PptxColor.ClampPercentage(color.Luminance) / 100000.0;
        }

        hsl[2] = hsl[2] * PptxColor.ClampPercentage(color.LuminanceModulation) / 100000.0;
        hsl[2] = ClampUnit(hsl[2] + PptxColor.ClampSignedPercentage(color.LuminanceOffset) / 100000.0);
        if (color.Complement)
        {
            hsl[0] = hsl[0] + 180.0;
        }

        clean = HslToRgb(hsl[0], ClampUnit(hsl[1]), ClampUnit(hsl[2]));
        red = PptxStyleResolverCore.HexChannel(clean, 0);
        green = PptxStyleResolverCore.HexChannel(clean, 2);
        blue = PptxStyleResolverCore.HexChannel(clean, 4);
        if (color.Inverse)
        {
            red = 255 - red;
            green = 255 - green;
            blue = 255 - blue;
        }

        if (color.Grayscale)
        {
            int gray = (int)System.Math.Round(red * 0.299 + green * 0.587 + blue * 0.114);
            red = gray;
            green = gray;
            blue = gray;
        }

        return PptxStyleResolverCore.ChannelHex(red) + PptxStyleResolverCore.ChannelHex(green) + PptxStyleResolverCore.ChannelHex(blue);
    }

    internal static bool IsRgb(string value)
    {
        string original = PptxDiagnostic.CleanText(value);
        if (original.Length == 0)
        {
            return false;
        }

        string clean = PptxColor.NormalizeRgb(original);
        if (clean.Length != 6)
        {
            return false;
        }

        foreach (char character in clean)
        {
            bool digit = character >= '0' && character <= '9';
            bool upper = character >= 'A' && character <= 'F';
            if (!digit && !upper)
            {
                return false;
            }
        }

        return true;
    }

    internal static string ResolveRgb(string value)
    {
        if (IsRgb(value))
        {
            return PptxColor.NormalizeRgb(value);
        }

        return "000000";
    }

    internal static string ResolveSystemColor(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        if (clean == "window" || clean == "btnHighlight")
        {
            return "FFFFFF";
        }

        if (clean == "windowText" || clean == "menuText" || clean == "btnText" || clean == "captionText" || clean == "infoText")
        {
            return "000000";
        }

        if (clean == "highlight" || clean == "menuHighlight")
        {
            return "3399FF";
        }

        if (clean == "highlightText")
        {
            return "FFFFFF";
        }

        if (clean == "grayText" || clean == "inactiveCaptionText")
        {
            return "808080";
        }

        if (clean == "menu" || clean == "btnFace" || clean == "menuBar")
        {
            return "F0F0F0";
        }

        if (clean == "scrollBar")
        {
            return "C8C8C8";
        }

        if (clean == "activeCaption")
        {
            return "99B4D1";
        }

        if (clean == "inactiveCaption")
        {
            return "BFCEDC";
        }

        if (clean == "windowFrame")
        {
            return "646464";
        }

        if (clean == "activeBorder")
        {
            return "B4B4B4";
        }

        if (clean == "inactiveBorder")
        {
            return "F4F7FC";
        }

        if (clean == "appWorkspace")
        {
            return "ABABAB";
        }

        if (clean == "btnShadow")
        {
            return "A0A0A0";
        }

        if (clean == "3dDkShadow")
        {
            return "696969";
        }

        if (clean == "3dLight")
        {
            return "E3E3E3";
        }

        if (clean == "infoBk")
        {
            return "FFFFE1";
        }

        if (clean == "hotLight")
        {
            return "0066CC";
        }

        if (clean == "gradientActiveCaption")
        {
            return "B9D1EA";
        }

        if (clean == "gradientInactiveCaption")
        {
            return "D7E4F2";
        }

        return "000000";
    }

    internal static double[] RgbToHsl(int red, int green, int blue)
    {
        double r = red / 255.0;
        double g = green / 255.0;
        double b = blue / 255.0;
        double maximum = System.Math.Max(r, System.Math.Max(g, b));
        double minimum = System.Math.Min(r, System.Math.Min(g, b));
        double delta = maximum - minimum;
        double hue = 0.0;
        double luminance = (maximum + minimum) / 2.0;
        double saturation = 0.0;
        if (delta > 0.0)
        {
            saturation = delta / (1.0 - System.Math.Abs(2.0 * luminance - 1.0));
            if (maximum == r)
            {
                hue = 60.0 * (((g - b) / delta) % 6.0);
            }
            else if (maximum == g)
            {
                hue = 60.0 * (((b - r) / delta) + 2.0);
            }
            else
            {
                hue = 60.0 * (((r - g) / delta) + 4.0);
            }
        }

        if (hue < 0.0)
        {
            hue = hue + 360.0;
        }

        return new double[] { hue, saturation, luminance };
    }

    internal static string HslToRgb(double hue, double saturation, double luminance)
    {
        double normalizedHue = NormalizeHue(hue);
        double cleanSaturation = ClampUnit(saturation);
        double cleanLuminance = ClampUnit(luminance);
        double chroma = (1.0 - System.Math.Abs(2.0 * cleanLuminance - 1.0)) * cleanSaturation;
        double section = normalizedHue / 60.0;
        double intermediate = chroma * (1.0 - System.Math.Abs(section % 2.0 - 1.0));
        double r = 0.0;
        double g = 0.0;
        double b = 0.0;
        if (section < 1.0)
        {
            r = chroma;
            g = intermediate;
        }
        else if (section < 2.0)
        {
            r = intermediate;
            g = chroma;
        }
        else if (section < 3.0)
        {
            g = chroma;
            b = intermediate;
        }
        else if (section < 4.0)
        {
            g = intermediate;
            b = chroma;
        }
        else if (section < 5.0)
        {
            r = intermediate;
            b = chroma;
        }
        else
        {
            r = chroma;
            b = intermediate;
        }

        double match = cleanLuminance - chroma / 2.0;
        int red = (int)System.Math.Round((r + match) * 255.0);
        int green = (int)System.Math.Round((g + match) * 255.0);
        int blue = (int)System.Math.Round((b + match) * 255.0);
        return PptxStyleResolverCore.ChannelHex(red) + PptxStyleResolverCore.ChannelHex(green) + PptxStyleResolverCore.ChannelHex(blue);
    }

    internal static double NormalizeHue(double value)
    {
        double result = value % 360.0;
        if (result < 0.0)
        {
            result = result + 360.0;
        }

        return result;
    }

    internal static double ClampUnit(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }

        if (value > 1.0)
        {
            return 1.0;
        }

        return value;
    }

    internal static Dictionary<string, string> CreatePresetColors()
    {
        Dictionary<string, string> colors = new Dictionary<string, string>();
        colors["aliceBlue"] = "F0F8FF";
        colors["antiqueWhite"] = "FAEBD7";
        colors["aqua"] = "00FFFF";
        colors["aquamarine"] = "7FFFD4";
        colors["azure"] = "F0FFFF";
        colors["beige"] = "F5F5DC";
        colors["bisque"] = "FFE4C4";
        colors["black"] = "000000";
        colors["blanchedAlmond"] = "FFEBCD";
        colors["blue"] = "0000FF";
        colors["blueViolet"] = "8A2BE2";
        colors["brown"] = "A52A2A";
        colors["burlyWood"] = "DEB887";
        colors["cadetBlue"] = "5F9EA0";
        colors["chartreuse"] = "7FFF00";
        colors["chocolate"] = "D2691E";
        colors["coral"] = "FF7F50";
        colors["cornflowerBlue"] = "6495ED";
        colors["cornsilk"] = "FFF8DC";
        colors["crimson"] = "DC143C";
        colors["cyan"] = "00FFFF";
        colors["dkBlue"] = "00008B";
        colors["dkCyan"] = "008B8B";
        colors["dkGoldenrod"] = "B8860B";
        colors["dkGray"] = "A9A9A9";
        colors["dkGreen"] = "006400";
        colors["dkKhaki"] = "BDB76B";
        colors["dkMagenta"] = "8B008B";
        colors["dkOliveGreen"] = "556B2F";
        colors["dkOrange"] = "FF8C00";
        colors["dkOrchid"] = "9932CC";
        colors["dkRed"] = "8B0000";
        colors["dkSalmon"] = "E9967A";
        colors["dkSeaGreen"] = "8FBC8F";
        colors["dkSlateBlue"] = "483D8B";
        colors["dkSlateGray"] = "2F4F4F";
        colors["dkTurquoise"] = "00CED1";
        colors["dkViolet"] = "9400D3";
        colors["deepPink"] = "FF1493";
        colors["deepSkyBlue"] = "00BFFF";
        colors["dimGray"] = "696969";
        colors["dodgerBlue"] = "1E90FF";
        colors["firebrick"] = "B22222";
        colors["floralWhite"] = "FFFAF0";
        colors["forestGreen"] = "228B22";
        colors["fuchsia"] = "FF00FF";
        colors["gainsboro"] = "DCDCDC";
        colors["ghostWhite"] = "F8F8FF";
        colors["gold"] = "FFD700";
        colors["goldenrod"] = "DAA520";
        colors["gray"] = "808080";
        colors["green"] = "008000";
        colors["greenYellow"] = "ADFF2F";
        colors["honeydew"] = "F0FFF0";
        colors["hotPink"] = "FF69B4";
        colors["indianRed"] = "CD5C5C";
        colors["indigo"] = "4B0082";
        colors["ivory"] = "FFFFF0";
        colors["khaki"] = "F0E68C";
        colors["lavender"] = "E6E6FA";
        colors["lavenderBlush"] = "FFF0F5";
        colors["lawnGreen"] = "7CFC00";
        colors["lemonChiffon"] = "FFFACD";
        colors["ltBlue"] = "ADD8E6";
        colors["ltCoral"] = "F08080";
        colors["ltCyan"] = "E0FFFF";
        colors["ltGoldenrodYellow"] = "FAFAD2";
        colors["ltGray"] = "D3D3D3";
        colors["ltGreen"] = "90EE90";
        colors["ltPink"] = "FFB6C1";
        colors["ltSalmon"] = "FFA07A";
        colors["ltSeaGreen"] = "20B2AA";
        colors["ltSkyBlue"] = "87CEFA";
        colors["ltSlateGray"] = "778899";
        colors["ltSteelBlue"] = "B0C4DE";
        colors["ltYellow"] = "FFFFE0";
        colors["lime"] = "00FF00";
        colors["limeGreen"] = "32CD32";
        colors["linen"] = "FAF0E6";
        colors["magenta"] = "FF00FF";
        colors["maroon"] = "800000";
        colors["medAquamarine"] = "66CDAA";
        colors["medBlue"] = "0000CD";
        colors["medOrchid"] = "BA55D3";
        colors["medPurple"] = "9370DB";
        colors["medSeaGreen"] = "3CB371";
        colors["medSlateBlue"] = "7B68EE";
        colors["medSpringGreen"] = "00FA9A";
        colors["medTurquoise"] = "48D1CC";
        colors["medVioletRed"] = "C71585";
        colors["midnightBlue"] = "191970";
        colors["mintCream"] = "F5FFFA";
        colors["mistyRose"] = "FFE4E1";
        colors["moccasin"] = "FFE4B5";
        colors["navajoWhite"] = "FFDEAD";
        colors["navy"] = "000080";
        colors["oldLace"] = "FDF5E6";
        colors["olive"] = "808000";
        colors["oliveDrab"] = "6B8E23";
        colors["orange"] = "FFA500";
        colors["orangeRed"] = "FF4500";
        colors["orchid"] = "DA70D6";
        colors["paleGoldenrod"] = "EEE8AA";
        colors["paleGreen"] = "98FB98";
        colors["paleTurquoise"] = "AFEEEE";
        colors["paleVioletRed"] = "DB7093";
        colors["papayaWhip"] = "FFEFD5";
        colors["peachPuff"] = "FFDAB9";
        colors["peru"] = "CD853F";
        colors["pink"] = "FFC0CB";
        colors["plum"] = "DDA0DD";
        colors["powderBlue"] = "B0E0E6";
        colors["purple"] = "800080";
        colors["red"] = "FF0000";
        colors["rosyBrown"] = "BC8F8F";
        colors["royalBlue"] = "4169E1";
        colors["saddleBrown"] = "8B4513";
        colors["salmon"] = "FA8072";
        colors["sandyBrown"] = "F4A460";
        colors["seaGreen"] = "2E8B57";
        colors["seaShell"] = "FFF5EE";
        colors["sienna"] = "A0522D";
        colors["silver"] = "C0C0C0";
        colors["skyBlue"] = "87CEEB";
        colors["slateBlue"] = "6A5ACD";
        colors["slateGray"] = "708090";
        colors["snow"] = "FFFAFA";
        colors["springGreen"] = "00FF7F";
        colors["steelBlue"] = "4682B4";
        colors["tan"] = "D2B48C";
        colors["teal"] = "008080";
        colors["thistle"] = "D8BFD8";
        colors["tomato"] = "FF6347";
        colors["turquoise"] = "40E0D0";
        colors["violet"] = "EE82EE";
        colors["wheat"] = "F5DEB3";
        colors["white"] = "FFFFFF";
        colors["whiteSmoke"] = "F5F5F5";
        colors["yellow"] = "FFFF00";
        colors["yellowGreen"] = "9ACD32";
        return colors;
    }
}
