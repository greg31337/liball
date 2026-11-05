using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents spreadsheet color resolver data used by the public workbook API.
/// </summary>
public class XlsxSpreadsheetColorResolver : IXlsxSpreadsheetColorResolver
{
    /// <summary>
    /// Initializes a new spreadsheet color resolver with its default state.
    /// </summary>
    public XlsxSpreadsheetColorResolver()
    {
    }

    /// <summary>
    /// Performs the resolve operation using the current model state.
    /// </summary>
    /// <param name = "color">The color value used by the operation.</param>
    /// <param name = "theme">The theme value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxSpreadsheetColorResolutionResult Resolve(XlsxSpreadsheetColor color, XlsxThemePart theme)
    {
        XlsxSpreadsheetColorResolutionResult result = new XlsxSpreadsheetColorResolutionResult();
        if (color == null)
        {
            result.AddError("color.null", "Spreadsheet color must not be null.");
            return result;
        }

        if (color.HasRgb)
        {
            result = ResolveRgb(color.Rgb);
            result.Source = "rgb";
        }
        else if (color.HasIndexed)
        {
            result = ResolveIndexed(color.Indexed);
        }
        else if (color.HasTheme)
        {
            result = ResolveTheme(color.Theme, theme);
        }
        else if (color.IsAuto)
        {
            result = ResolveAutomatic();
        }
        else
        {
            result.AddError("color.unresolved", "Spreadsheet color does not specify rgb, indexed, theme, or automatic color information.");
            return result;
        }

        if (result.IsValid && color.HasTint)
        {
            ApplySpreadsheetTint(result, color.Tint);
        }

        return result;
    }

    internal static XlsxSpreadsheetColorResolutionResult ResolveRgb(string? rgb)
    {
        XlsxSpreadsheetColorResolutionResult result = new XlsxSpreadsheetColorResolutionResult();
        string value = NormalizeColorText(rgb);
        if (value.StartsWith("#"))
        {
            value = value.Substring(1);
        }

        if (value.Length == 6)
        {
            value = "FF" + value;
        }

        if (value.Length != 8)
        {
            result.AddError("color.rgbInvalid", "RGB color must contain six RGB or eight ARGB hexadecimal characters.");
            return result;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!XlsxXmlValueParser.IsHexDigit(value[index]))
            {
                result.AddError("color.rgbInvalid", "RGB color contains a non-hexadecimal character.");
                return result;
            }

            index = index + 1;
        }

        SetResolvedColor(result, value.ToUpperInvariant(), "rgb");
        return result;
    }

    internal static XlsxSpreadsheetColorResolutionResult ResolveIndexed(uint indexed)
    {
        if (indexed == 64)
        {
            XlsxSpreadsheetColorResolutionResult automaticResult = ResolveAutomatic();
            automaticResult.Source = "indexed";
            return automaticResult;
        }

        if (indexed == 65)
        {
            XlsxSpreadsheetColorResolutionResult systemBackgroundResult = ResolveRgb("FFFFFF");
            systemBackgroundResult.Source = "indexed";
            return systemBackgroundResult;
        }

        string rgb = GetDefaultIndexedRgb(indexed);
        if (rgb.Length == 0)
        {
            XlsxSpreadsheetColorResolutionResult result = new XlsxSpreadsheetColorResolutionResult();
            result.AddError("color.indexedRange", "Indexed color is outside the default indexed palette.");
            return result;
        }

        XlsxSpreadsheetColorResolutionResult resolved = ResolveRgb(rgb);
        resolved.Source = "indexed";
        return resolved;
    }

    internal static XlsxSpreadsheetColorResolutionResult ResolveTheme(uint themeIndex, XlsxThemePart theme)
    {
        XlsxSpreadsheetColorResolutionResult result = new XlsxSpreadsheetColorResolutionResult();
        if (theme == null || !theme.HasColorScheme || theme.ColorScheme == null || theme.ColorScheme.Colors == null)
        {
            result.AddError("color.themeMissing", "Theme color resolution requires a theme color scheme.");
            return result;
        }

        string slot = GetThemeSlotName(themeIndex);
        if (slot.Length == 0)
        {
            result.AddError("color.themeRange", "Theme color index is outside the supported theme color range.");
            return result;
        }

        XlsxThemeColor? color = FindThemeColor(theme.ColorScheme, slot);
        if (color == null)
        {
            result.AddError("color.themeSlotMissing", "Theme color scheme does not contain the requested color slot.");
            return result;
        }

        result = ResolveThemeColor(color);
        if (result.IsValid)
        {
            result.Source = "theme";
        }

        return result;
    }

    internal static XlsxSpreadsheetColorResolutionResult ResolveThemeColor(XlsxThemeColor color)
    {
        XlsxSpreadsheetColorResolutionResult result = new XlsxSpreadsheetColorResolutionResult();
        if (color == null)
        {
            result.AddError("color.themeColorNull", "Theme color must not be null.");
            return result;
        }

        if (IsColorKind(color.Kind, "srgbClr"))
        {
            result = ResolveRgb(color.Value);
        }
        else if (IsColorKind(color.Kind, "sysClr"))
        {
            result = ResolveSystemColor(color.Value, color.LastColor);
        }
        else
        {
            result.AddError("color.themeKindUnsupported", "Theme color kind is not supported for RGB resolution.");
            return result;
        }

        if (result.IsValid)
        {
            ApplyDrawingColorTransforms(result, color);
            result.Source = "theme";
        }

        return result;
    }

    internal static XlsxSpreadsheetColorResolutionResult ResolveSystemColor(string? systemColor, string? lastColor)
    {
        string fallback = NormalizeColorText(lastColor);
        if (fallback.Length > 0)
        {
            XlsxSpreadsheetColorResolutionResult fallbackResult = ResolveRgb(fallback);
            if (fallbackResult.IsValid)
            {
                fallbackResult.Source = "system";
                return fallbackResult;
            }
        }

        string rgb = GetSystemColorRgb(systemColor);
        if (rgb.Length == 0)
        {
            XlsxSpreadsheetColorResolutionResult result = new XlsxSpreadsheetColorResolutionResult();
            result.AddError("color.systemUnknown", "System color is unknown and does not provide a valid fallback color.");
            return result;
        }

        XlsxSpreadsheetColorResolutionResult resolved = ResolveRgb(rgb);
        resolved.Source = "system";
        return resolved;
    }

    internal static XlsxSpreadsheetColorResolutionResult ResolveAutomatic()
    {
        XlsxSpreadsheetColorResolutionResult result = ResolveRgb("000000");
        result.Source = "automatic";
        return result;
    }

    internal static void ApplySpreadsheetTint(XlsxSpreadsheetColorResolutionResult result, double tint)
    {
        if (result == null || !result.IsValid || !result.HasColor)
        {
            return;
        }

        if (tint < -1d || tint > 1d)
        {
            result.AddError("color.tintRange", "Color tint must be between -1 and 1.");
            return;
        }

        result.Red = ApplySpreadsheetTintChannel(result.Red, tint);
        result.Green = ApplySpreadsheetTintChannel(result.Green, tint);
        result.Blue = ApplySpreadsheetTintChannel(result.Blue, tint);
        SetResolvedColor(result, ComponentsToArgb(result.Alpha, result.Red, result.Green, result.Blue), result.Source);
    }

    internal static int ApplySpreadsheetTintChannel(int channel, double tint)
    {
        double value = (double)channel;
        if (tint < 0d)
        {
            value = value * (1d + tint);
        }
        else
        {
            value = value + ((255d - value) * tint);
        }

        return ClampColorChannel((int)System.Math.Round(value, System.MidpointRounding.AwayFromZero));
    }

    internal static void ApplyDrawingColorTransforms(XlsxSpreadsheetColorResolutionResult result, XlsxThemeColor color)
    {
        if (color.HasTint)
        {
            ApplyDrawingTint(result, color.Tint);
        }

        if (color.HasShade)
        {
            ApplyDrawingShade(result, color.Shade);
        }

        if (color.HasLuminanceModulation)
        {
            ApplyLuminanceModulation(result, color.LuminanceModulation);
        }

        if (color.HasLuminanceOffset)
        {
            ApplyLuminanceOffset(result, color.LuminanceOffset);
        }
    }

    internal static void ApplyDrawingTint(XlsxSpreadsheetColorResolutionResult result, uint value)
    {
        double tint = GetPercentage(value);
        result.Red = BlendColorChannel(result.Red, 255, tint);
        result.Green = BlendColorChannel(result.Green, 255, tint);
        result.Blue = BlendColorChannel(result.Blue, 255, tint);
        SetResolvedColor(result, ComponentsToArgb(result.Alpha, result.Red, result.Green, result.Blue), result.Source);
    }

    internal static void ApplyDrawingShade(XlsxSpreadsheetColorResolutionResult result, uint value)
    {
        double shade = GetPercentage(value);
        result.Red = ClampColorChannel((int)System.Math.Round((double)result.Red * shade, System.MidpointRounding.AwayFromZero));
        result.Green = ClampColorChannel((int)System.Math.Round((double)result.Green * shade, System.MidpointRounding.AwayFromZero));
        result.Blue = ClampColorChannel((int)System.Math.Round((double)result.Blue * shade, System.MidpointRounding.AwayFromZero));
        SetResolvedColor(result, ComponentsToArgb(result.Alpha, result.Red, result.Green, result.Blue), result.Source);
    }

    internal static void ApplyLuminanceModulation(XlsxSpreadsheetColorResolutionResult result, uint value)
    {
        ApplyDrawingShade(result, value);
    }

    internal static void ApplyLuminanceOffset(XlsxSpreadsheetColorResolutionResult result, uint value)
    {
        double offset = GetPercentage(value);
        result.Red = ClampColorChannel(result.Red + (int)System.Math.Round(255d * offset, System.MidpointRounding.AwayFromZero));
        result.Green = ClampColorChannel(result.Green + (int)System.Math.Round(255d * offset, System.MidpointRounding.AwayFromZero));
        result.Blue = ClampColorChannel(result.Blue + (int)System.Math.Round(255d * offset, System.MidpointRounding.AwayFromZero));
        SetResolvedColor(result, ComponentsToArgb(result.Alpha, result.Red, result.Green, result.Blue), result.Source);
    }

    internal static double GetPercentage(uint value)
    {
        if (value > 100000)
        {
            return 1d;
        }

        return (double)value / 100000d;
    }

    internal static int BlendColorChannel(int channel, int target, double amount)
    {
        double value = (double)channel + (((double)target - (double)channel) * amount);
        return ClampColorChannel((int)System.Math.Round(value, System.MidpointRounding.AwayFromZero));
    }

    internal static void SetResolvedColor(XlsxSpreadsheetColorResolutionResult result, string argb, string source)
    {
        string value = NormalizeColorText(argb);
        result.Alpha = ParseHexByte(value, 0);
        result.Red = ParseHexByte(value, 2);
        result.Green = ParseHexByte(value, 4);
        result.Blue = ParseHexByte(value, 6);
        result.Argb = ComponentsToArgb(result.Alpha, result.Red, result.Green, result.Blue);
        result.Rgb = result.Argb.Substring(2);
        result.HasColor = true;
        result.Source = source;
    }

    internal static string ComponentsToArgb(int alpha, int red, int green, int blue)
    {
        return ClampColorChannel(alpha).ToString("X2", CultureInfo.InvariantCulture) + ClampColorChannel(red).ToString("X2", CultureInfo.InvariantCulture) + ClampColorChannel(green).ToString("X2", CultureInfo.InvariantCulture) + ClampColorChannel(blue).ToString("X2", CultureInfo.InvariantCulture);
    }

    internal static int ClampColorChannel(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return value;
    }

    internal static int ParseHexByte(string value, int start)
    {
        return (HexValue(value[start]) * 16) + HexValue(value[start + 1]);
    }

    internal static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return value - 'a' + 10;
    }

    internal static string NormalizeColorText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text.Trim().ToUpperInvariant();
    }

    internal static bool IsColorKind(string actual, string expected)
    {
        return string.Equals(actual, expected, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static XlsxThemeColor? FindThemeColor(XlsxThemeColorScheme scheme, string slot)
    {
        foreach (XlsxThemeColor color in scheme.Colors)
        {
            if (string.Equals(color.Slot, slot, System.StringComparison.OrdinalIgnoreCase))
            {
                return color;
            }
        }

        return null;
    }

    internal static string GetThemeSlotName(uint themeIndex)
    {
        switch (themeIndex)
        {
            case 0:
                return "lt1";
            case 1:
                return "dk1";
            case 2:
                return "lt2";
            case 3:
                return "dk2";
            case 4:
                return "accent1";
            case 5:
                return "accent2";
            case 6:
                return "accent3";
            case 7:
                return "accent4";
            case 8:
                return "accent5";
            case 9:
                return "accent6";
            case 10:
                return "hlink";
            case 11:
                return "folHlink";
            default:
                return "";
        }
    }

    internal static string GetSystemColorRgb(string? systemColor)
    {
        string value = NormalizeColorText(systemColor);
        if (value == "WINDOW")
        {
            return "FFFFFF";
        }

        if (value == "WINDOWTEXT")
        {
            return "000000";
        }

        if (value == "BTNTEXT" || value == "MENUTEXT" || value == "CAPTIONTEXT" || value == "INFOTEXT")
        {
            return "000000";
        }

        if (value == "BTNFACE" || value == "MENU" || value == "INFOBK")
        {
            return "F0F0F0";
        }

        if (value == "HIGHLIGHT")
        {
            return "3399FF";
        }

        if (value == "HIGHLIGHTTEXT")
        {
            return "FFFFFF";
        }

        if (value == "GRAYTEXT")
        {
            return "808080";
        }

        if (value == "ACTIVECAPTION")
        {
            return "99B4D1";
        }

        if (value == "INACTIVECAPTION")
        {
            return "BFCDDB";
        }

        if (value == "INACTIVECAPTIONTEXT")
        {
            return "434E54";
        }

        return "";
    }

    internal static string GetDefaultIndexedRgb(uint indexed)
    {
        switch (indexed)
        {
            case 0:
                return "000000";
            case 1:
                return "FFFFFF";
            case 2:
                return "FF0000";
            case 3:
                return "00FF00";
            case 4:
                return "0000FF";
            case 5:
                return "FFFF00";
            case 6:
                return "FF00FF";
            case 7:
                return "00FFFF";
            case 8:
                return "000000";
            case 9:
                return "FFFFFF";
            case 10:
                return "FF0000";
            case 11:
                return "00FF00";
            case 12:
                return "0000FF";
            case 13:
                return "FFFF00";
            case 14:
                return "FF00FF";
            case 15:
                return "00FFFF";
            case 16:
                return "800000";
            case 17:
                return "008000";
            case 18:
                return "000080";
            case 19:
                return "808000";
            case 20:
                return "800080";
            case 21:
                return "008080";
            case 22:
                return "C0C0C0";
            case 23:
                return "808080";
            case 24:
                return "9999FF";
            case 25:
                return "993366";
            case 26:
                return "FFFFCC";
            case 27:
                return "CCFFFF";
            case 28:
                return "660066";
            case 29:
                return "FF8080";
            case 30:
                return "0066CC";
            case 31:
                return "CCCCFF";
            case 32:
                return "000080";
            case 33:
                return "FF00FF";
            case 34:
                return "FFFF00";
            case 35:
                return "00FFFF";
            case 36:
                return "800080";
            case 37:
                return "800000";
            case 38:
                return "008080";
            case 39:
                return "0000FF";
            case 40:
                return "00CCFF";
            case 41:
                return "CCFFFF";
            case 42:
                return "CCFFCC";
            case 43:
                return "FFFF99";
            case 44:
                return "99CCFF";
            case 45:
                return "FF99CC";
            case 46:
                return "CC99FF";
            case 47:
                return "FFCC99";
            case 48:
                return "3366FF";
            case 49:
                return "33CCCC";
            case 50:
                return "99CC00";
            case 51:
                return "FFCC00";
            case 52:
                return "FF9900";
            case 53:
                return "FF6600";
            case 54:
                return "666699";
            case 55:
                return "969696";
            case 56:
                return "003366";
            case 57:
                return "339966";
            case 58:
                return "003300";
            case 59:
                return "333300";
            case 60:
                return "993300";
            case 61:
                return "993366";
            case 62:
                return "333399";
            case 63:
                return "333333";
            default:
                return "";
        }
    }
}
