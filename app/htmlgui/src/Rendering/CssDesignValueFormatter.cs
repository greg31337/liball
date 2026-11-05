using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace htmlgui;

internal class CssTypographyValue
{
    internal string[] FontFamilies = Array.Empty<string>();
    internal UiLength? FontSize;
    internal bool HasFontWeight;
    internal int FontWeight;
    internal string FontStyle = "normal";
    internal bool HasLineHeight;
    internal double LineHeight;
    internal UiLength? LetterSpacing;
}

internal class CssBorderValue
{
    internal UiLength Width = new UiLength();
    internal string Style = "solid";
    internal string Color = "currentcolor";
}

internal class CssRadiusValue
{
    internal UiLength BlockStartStart = new UiLength();
    internal UiLength BlockStartEnd = new UiLength();
    internal UiLength BlockEndEnd = new UiLength();
    internal UiLength BlockEndStart = new UiLength();
}

internal class CssShadowValue
{
    internal UiLength OffsetX = new UiLength();
    internal UiLength OffsetY = new UiLength();
    internal UiLength Blur = new UiLength();
    internal UiLength Spread = new UiLength();
    internal string Color = "currentcolor";
    internal bool Inset;
}

internal class CssFocusRingValue
{
    internal UiLength Width = new UiLength();
    internal UiLength Offset = new UiLength();
    internal string Style = "solid";
    internal string Color = "currentcolor";
}

internal static class CssDesignValueFormatter
{
    internal static string FormatLength(UiLength? length, bool allowFraction)
    {
        if (length == null)
        {
            throw new ArgumentNullException("length");
        }

        if (length.Expression != null && length.Expression.Length > 0)
        {
            string expression = length.Expression.Trim();
            if (!IsSafeLengthExpression(expression))
            {
                throw new ArgumentException("The CSS length expression is not supported or contains unsafe syntax.", "length");
            }

            return expression;
        }

        if (!length.HasValue)
        {
            throw new ArgumentException("A CSS length requires a numeric value or safe expression.", "length");
        }

        if (length.Unit == UiLengthUnit.Auto)
        {
            throw new ArgumentException("A numeric CSS length requires a concrete unit.", "length");
        }

        if (length.Unit == UiLengthUnit.Fraction && !allowFraction)
        {
            throw new ArgumentException("Fractional grid units are not valid in this CSS length context.", "length");
        }

        return InvariantFormatter.FormatDouble(length.Value) + GetLengthUnitSuffix(length.Unit);
    }

    internal static string GetLengthUnitSuffix(UiLengthUnit unit)
    {
        switch (unit)
        {
            case UiLengthUnit.Pixel:
                return CssUnits.Pixel;
            case UiLengthUnit.Percent:
                return CssUnits.Percent;
            case UiLengthUnit.Em:
                return CssUnits.Em;
            case UiLengthUnit.Rem:
                return CssUnits.Rem;
            case UiLengthUnit.ViewportWidth:
                return CssUnits.ViewportWidth;
            case UiLengthUnit.ViewportHeight:
                return CssUnits.ViewportHeight;
            case UiLengthUnit.Fraction:
                return CssUnits.Fraction;
            case UiLengthUnit.Character:
                return CssUnits.Character;
            case UiLengthUnit.Centimeter:
                return CssUnits.Centimeter;
            case UiLengthUnit.Millimeter:
                return CssUnits.Millimeter;
            case UiLengthUnit.Inch:
                return CssUnits.Inch;
            case UiLengthUnit.Point:
                return CssUnits.Point;
            case UiLengthUnit.Pica:
                return CssUnits.Pica;
            default:
                throw new ArgumentOutOfRangeException("unit", unit, "The CSS length unit is not supported.");
        }
    }

    internal static bool IsSafeLengthExpression(string expression)
    {
        if (expression.Length == 0 || !CssEscaper.IsSafeDeclarationToken(expression, true) || expression.Contains("url(", StringComparison.OrdinalIgnoreCase) || expression.Contains("\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (expression == "auto" || expression == "min-content" || expression == "max-content" || expression == "fit-content")
        {
            return true;
        }

        return StartsWithFunction(expression, "calc")
            || StartsWithFunction(expression, "min")
            || StartsWithFunction(expression, "max")
            || StartsWithFunction(expression, "clamp")
            || StartsWithFunction(expression, "var")
            || StartsWithFunction(expression, "env");
    }

    internal static bool StartsWithFunction(string expression, string name)
    {
        return expression.StartsWith(name + "(", StringComparison.Ordinal) && expression[expression.Length - 1] == ')';
    }

    internal static string NormalizeColor(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string color = value.Trim();
        if (color.Length == 0)
        {
            throw new ArgumentException("CSS colors cannot be empty.", "value");
        }

        if (color[0] == '#')
        {
            return NormalizeHexColor(color);
        }

        string lower = color.ToLowerInvariant();
        if (lower.StartsWith("rgb(", StringComparison.Ordinal) || lower.StartsWith("rgba(", StringComparison.Ordinal))
        {
            return NormalizeRgbColor(lower);
        }

        if (IsKnownColorName(lower))
        {
            return lower;
        }

        throw new ArgumentException("The CSS color format or named color is not supported.", "value");
    }

    internal static string NormalizeHexColor(string color)
    {
        int digitCount = color.Length - 1;
        if (digitCount != 3 && digitCount != 4 && digitCount != 6 && digitCount != 8)
        {
            throw new ArgumentException("Hex colors require three, four, six, or eight digits.", "color");
        }

        StringBuilder digits = new StringBuilder();
        int index = 1;
        while (index < color.Length)
        {
            char character = color[index];
            if (!IsHexDigit(character))
            {
                throw new ArgumentException("Hex colors can contain only hexadecimal digits.", "color");
            }

            char lower = character;
            if (lower >= 'A' && lower <= 'F')
            {
                lower = (char)(lower + ('a' - 'A'));
            }

            digits.Append(lower);
            if (digitCount == 3 || digitCount == 4)
            {
                digits.Append(lower);
            }

            index++;
        }

        return "#" + digits.ToString();
    }

    internal static bool IsHexDigit(char character)
    {
        return character >= '0' && character <= '9'
            || character >= 'a' && character <= 'f'
            || character >= 'A' && character <= 'F';
    }

    internal static string NormalizeRgbColor(string color)
    {
        bool legacyAlpha = color.StartsWith("rgba(", StringComparison.Ordinal);
        int prefixLength = legacyAlpha ? 5 : 4;
        if (color[color.Length - 1] != ')')
        {
            throw new ArgumentException("RGB colors require a closing parenthesis.", "color");
        }

        string content = color.Substring(prefixLength, color.Length - prefixLength - 1);
        bool hasComma = content.Contains(",", StringComparison.Ordinal);
        bool hasSlash = content.Contains("/", StringComparison.Ordinal);
        if (hasComma && hasSlash)
        {
            throw new ArgumentException("RGB colors cannot mix legacy commas with modern alpha syntax.", "color");
        }

        string separated = content.Replace(",", " ", StringComparison.Ordinal).Replace("/", " / ", StringComparison.Ordinal);
        string[] parts = separated.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool hasAlpha = !hasComma && parts.Length == 5 && parts[3] == "/" || hasComma && legacyAlpha && parts.Length == 4;
        if ((!hasAlpha && parts.Length != 3) || hasAlpha && parts.Length != 4 && parts.Length != 5)
        {
            throw new ArgumentException("RGB colors require three channels and an optional alpha channel.", "color");
        }

        string red = NormalizeRgbComponent(parts[0]);
        string green = NormalizeRgbComponent(parts[1]);
        string blue = NormalizeRgbComponent(parts[2]);
        string result = "rgb(" + red + " " + green + " " + blue;
        if (hasAlpha)
        {
            string alphaToken = parts.Length == 5 ? parts[4] : parts[3];
            result = result + " / " + NormalizeAlphaComponent(alphaToken);
        }

        return result + ")";
    }

    internal static string NormalizeRgbComponent(string token)
    {
        bool isPercent = token.EndsWith("%", StringComparison.Ordinal);
        string number = isPercent ? token.Substring(0, token.Length - 1) : token;
        decimal value = ParseCssDecimal(number);
        decimal maximum = isPercent ? 100m : 255m;
        if (value < 0m || value > maximum)
        {
            throw new ArgumentOutOfRangeException("token", token, "RGB color channels are outside their allowed range.");
        }

        return InvariantFormatter.FormatDecimal(value) + (isPercent ? "%" : string.Empty);
    }

    internal static string NormalizeAlphaComponent(string token)
    {
        bool isPercent = token.EndsWith("%", StringComparison.Ordinal);
        string number = isPercent ? token.Substring(0, token.Length - 1) : token;
        decimal value = ParseCssDecimal(number);
        decimal maximum = isPercent ? 100m : 1m;
        if (value < 0m || value > maximum)
        {
            throw new ArgumentOutOfRangeException("token", token, "CSS alpha values are outside their allowed range.");
        }

        return InvariantFormatter.FormatDecimal(value) + (isPercent ? "%" : string.Empty);
    }

    internal static decimal ParseCssDecimal(string token)
    {
        if (token.Length == 0)
        {
            throw new ArgumentException("CSS numbers cannot be empty.", "token");
        }

        bool hasDigit = false;
        bool hasPoint = false;
        int index = 0;
        while (index < token.Length)
        {
            char character = token[index];
            if (character >= '0' && character <= '9')
            {
                hasDigit = true;
            }
            else if (character == '.' && !hasPoint)
            {
                hasPoint = true;
            }
            else
            {
                throw new ArgumentException("CSS numbers can contain only decimal digits and one point.", "token");
            }

            index++;
        }

        if (!hasDigit)
        {
            throw new ArgumentException("CSS numbers require at least one digit.", "token");
        }

        return decimal.Parse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }

    internal static bool IsKnownColorName(string value)
    {
        switch (value)
        {
            case "aliceblue":
            case "antiquewhite":
            case "aquamarine":
            case "azure":
            case "beige":
            case "bisque":
            case "blanchedalmond":
            case "blueviolet":
            case "burlywood":
            case "cadetblue":
            case "chartreuse":
            case "chocolate":
            case "coral":
            case "cornflowerblue":
            case "cornsilk":
            case "crimson":
            case "cyan":
            case "darkblue":
            case "darkcyan":
            case "darkgoldenrod":
            case "darkgray":
            case "darkgreen":
            case "darkgrey":
            case "darkkhaki":
            case "darkmagenta":
            case "darkolivegreen":
            case "darkorange":
            case "darkorchid":
            case "darkred":
            case "darksalmon":
            case "darkseagreen":
            case "darkslateblue":
            case "darkslategray":
            case "darkslategrey":
            case "darkturquoise":
            case "darkviolet":
            case "deeppink":
            case "deepskyblue":
            case "dimgray":
            case "dimgrey":
            case "dodgerblue":
            case "firebrick":
            case "floralwhite":
            case "forestgreen":
            case "gainsboro":
            case "ghostwhite":
            case "gold":
            case "goldenrod":
            case "greenyellow":
            case "honeydew":
            case "hotpink":
            case "indianred":
            case "indigo":
            case "ivory":
            case "khaki":
            case "lavender":
            case "lavenderblush":
            case "lawngreen":
            case "lemonchiffon":
            case "lightblue":
            case "lightcoral":
            case "lightcyan":
            case "lightgoldenrodyellow":
            case "lightgray":
            case "lightgreen":
            case "lightgrey":
            case "lightpink":
            case "lightsalmon":
            case "lightseagreen":
            case "lightskyblue":
            case "lightslategray":
            case "lightslategrey":
            case "lightsteelblue":
            case "lightyellow":
            case "limegreen":
            case "linen":
            case "magenta":
            case "mediumaquamarine":
            case "mediumblue":
            case "mediumorchid":
            case "mediumpurple":
            case "mediumseagreen":
            case "mediumslateblue":
            case "mediumspringgreen":
            case "mediumturquoise":
            case "mediumvioletred":
            case "midnightblue":
            case "mintcream":
            case "mistyrose":
            case "moccasin":
            case "navajowhite":
            case "oldlace":
            case "olivedrab":
            case "orangered":
            case "orchid":
            case "palegoldenrod":
            case "palegreen":
            case "paleturquoise":
            case "palevioletred":
            case "papayawhip":
            case "peachpuff":
            case "peru":
            case "plum":
            case "powderblue":
            case "rebeccapurple":
            case "rosybrown":
            case "royalblue":
            case "saddlebrown":
            case "salmon":
            case "sandybrown":
            case "seagreen":
            case "seashell":
            case "sienna":
            case "skyblue":
            case "slateblue":
            case "slategray":
            case "slategrey":
            case "snow":
            case "springgreen":
            case "steelblue":
            case "tan":
            case "thistle":
            case "tomato":
            case "turquoise":
            case "violet":
            case "wheat":
            case "whitesmoke":
            case "yellowgreen":
            case "transparent":
            case "currentcolor":
            case "black":
            case "silver":
            case "gray":
            case "grey":
            case "white":
            case "maroon":
            case "red":
            case "purple":
            case "fuchsia":
            case "green":
            case "lime":
            case "olive":
            case "yellow":
            case "navy":
            case "blue":
            case "teal":
            case "aqua":
            case "orange":
            case "pink":
            case "brown":
            case "accentcolor":
            case "accentcolortext":
            case "activetext":
            case "buttonborder":
            case "buttonface":
            case "buttontext":
            case "canvas":
            case "canvastext":
            case "field":
            case "fieldtext":
            case "graytext":
            case "highlight":
            case "highlighttext":
            case "linktext":
            case "mark":
            case "marktext":
            case "selecteditem":
            case "selecteditemtext":
            case "visitedtext":
                return true;
            default:
                return false;
        }
    }

    internal static string FormatFontFamilies(string[]? families)
    {
        if (families == null)
        {
            throw new ArgumentNullException("families");
        }

        if (families.Length == 0)
        {
            throw new ArgumentException("Typography requires at least one font family.", "families");
        }

        List<string> result = new List<string>();
        int index = 0;
        while (index < families.Length)
        {
            string? family = families[index];
            if (string.IsNullOrWhiteSpace(family))
            {
                throw new ArgumentException("Font family names cannot be empty.", "families");
            }

            string formatted = FormatFontFamily(family);
            if (!ContainsOrdinal(result, formatted))
            {
                result.Add(formatted);
            }

            index++;
        }

        return string.Join(",", result);
    }

    internal static string FormatFontFamily(string family)
    {
        string value = family.Trim();
        string lower = value.ToLowerInvariant();
        if (lower == "serif" || lower == "sans-serif" || lower == "monospace" || lower == "cursive" || lower == "fantasy" || lower == "system-ui" || lower == "ui-serif" || lower == "ui-sans-serif" || lower == "ui-monospace" || lower == "ui-rounded" || lower == "emoji" || lower == "math" || lower == "fangsong")
        {
            return lower;
        }

        return CssEscaper.EscapeString(value);
    }

    internal static bool ContainsOrdinal(List<string> values, string candidate)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], candidate, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static UiCssDeclaration[] CreateTypographyDeclarations(CssTypographyValue? typography)
    {
        if (typography == null)
        {
            throw new ArgumentNullException("typography");
        }

        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        if (typography.FontFamilies.Length > 0)
        {
            AddGeneratedDeclaration(declarations, CssProperties.FontFamily, FormatFontFamilies(typography.FontFamilies));
        }

        if (typography.FontSize != null)
        {
            AddGeneratedDeclaration(declarations, CssProperties.FontSize, FormatNonNegativeLength(typography.FontSize, "fontSize"));
        }

        string fontStyle = typography.FontStyle.ToLowerInvariant();
        if (fontStyle != "normal" && fontStyle != "italic" && fontStyle != "oblique")
        {
            throw new ArgumentException("The font style is not supported.", "typography");
        }

        AddGeneratedDeclaration(declarations, CssProperties.FontStyle, fontStyle);
        if (typography.HasFontWeight)
        {
            if (typography.FontWeight < 1 || typography.FontWeight > 1000)
            {
                throw new ArgumentOutOfRangeException("typography", typography.FontWeight, "Font weight must be between 1 and 1000.");
            }

            AddGeneratedDeclaration(declarations, CssProperties.FontWeight, InvariantFormatter.FormatInt32(typography.FontWeight));
        }

        if (typography.HasLineHeight)
        {
            if (typography.LineHeight <= 0 || double.IsNaN(typography.LineHeight) || double.IsInfinity(typography.LineHeight))
            {
                throw new ArgumentOutOfRangeException("typography", typography.LineHeight, "Line height must be a positive finite number.");
            }

            AddGeneratedDeclaration(declarations, CssProperties.LineHeight, InvariantFormatter.FormatDouble(typography.LineHeight));
        }

        if (typography.LetterSpacing != null)
        {
            AddGeneratedDeclaration(declarations, CssProperties.LetterSpacing, FormatLength(typography.LetterSpacing, false));
        }

        return declarations.ToArray();
    }

    internal static UiCssDeclaration[] CreateSpacingDeclarations(UiSpacing? spacing, string? prefix)
    {
        if (spacing == null)
        {
            throw new ArgumentNullException("spacing");
        }

        if (prefix != "margin" && prefix != "padding")
        {
            throw new ArgumentException("Spacing declarations support only margin or padding.", "prefix");
        }

        bool nonNegative = prefix == "padding";
        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        AddSpacingEdge(declarations, spacing.BlockStart, prefix + "-block-start", nonNegative);
        AddSpacingEdge(declarations, spacing.InlineEnd, prefix + "-inline-end", nonNegative);
        AddSpacingEdge(declarations, spacing.BlockEnd, prefix + "-block-end", nonNegative);
        AddSpacingEdge(declarations, spacing.InlineStart, prefix + "-inline-start", nonNegative);
        return declarations.ToArray();
    }

    internal static void AddSpacingEdge(List<UiCssDeclaration> declarations, UiLength? length, string property, bool nonNegative)
    {
        if (length == null || !length.HasValue && (length.Expression == null || length.Expression.Length == 0))
        {
            return;
        }

        string value = nonNegative ? FormatNonNegativeLength(length, property) : FormatLength(length, false);
        AddGeneratedDeclaration(declarations, property, value);
    }

    internal static UiCssDeclaration[] CreateBorderDeclarations(CssBorderValue? border)
    {
        if (border == null)
        {
            throw new ArgumentNullException("border");
        }

        string style = border.Style.ToLowerInvariant();
        if (!IsBorderStyle(style))
        {
            throw new ArgumentException("The border style is not supported.", "border");
        }

        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        AddGeneratedDeclaration(declarations, "border-color", NormalizeColor(border.Color));
        AddGeneratedDeclaration(declarations, "border-style", style);
        AddGeneratedDeclaration(declarations, "border-width", FormatNonNegativeLength(border.Width, "borderWidth"));
        return declarations.ToArray();
    }

    internal static bool IsBorderStyle(string value)
    {
        return value == "none" || value == "hidden" || value == "dotted" || value == "dashed" || value == "solid" || value == "double" || value == "groove" || value == "ridge" || value == "inset" || value == "outset";
    }

    internal static UiCssDeclaration[] CreateRadiusDeclarations(CssRadiusValue? radius)
    {
        if (radius == null)
        {
            throw new ArgumentNullException("radius");
        }

        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        AddOptionalNonNegativeLength(declarations, radius.BlockStartStart, "border-start-start-radius");
        AddOptionalNonNegativeLength(declarations, radius.BlockStartEnd, "border-start-end-radius");
        AddOptionalNonNegativeLength(declarations, radius.BlockEndEnd, "border-end-end-radius");
        AddOptionalNonNegativeLength(declarations, radius.BlockEndStart, "border-end-start-radius");
        return declarations.ToArray();
    }

    internal static void AddOptionalNonNegativeLength(List<UiCssDeclaration> declarations, UiLength length, string property)
    {
        if (!length.HasValue && (length.Expression == null || length.Expression.Length == 0))
        {
            return;
        }

        AddGeneratedDeclaration(declarations, property, FormatNonNegativeLength(length, property));
    }

    internal static string FormatShadows(CssShadowValue[]? shadows)
    {
        if (shadows == null)
        {
            throw new ArgumentNullException("shadows");
        }

        if (shadows.Length == 0)
        {
            return "none";
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < shadows.Length)
        {
            CssShadowValue? shadow = shadows[index];
            if (shadow == null)
            {
                throw new ArgumentException("Shadow collections cannot contain null.", "shadows");
            }

            if (index > 0)
            {
                result.Append(',');
            }

            if (shadow.Inset)
            {
                result.Append("inset ");
            }

            result.Append(FormatLength(shadow.OffsetX, false));
            result.Append(' ');
            result.Append(FormatLength(shadow.OffsetY, false));
            result.Append(' ');
            result.Append(FormatNonNegativeLength(shadow.Blur, "blur"));
            if (shadow.Spread.HasValue || shadow.Spread.Expression != null && shadow.Spread.Expression.Length > 0)
            {
                result.Append(' ');
                result.Append(FormatLength(shadow.Spread, false));
            }

            result.Append(' ');
            result.Append(NormalizeColor(shadow.Color));
            index++;
        }

        return result.ToString();
    }

    internal static UiCssDeclaration[] CreateFocusRingDeclarations(CssFocusRingValue? focusRing)
    {
        if (focusRing == null)
        {
            throw new ArgumentNullException("focusRing");
        }

        string style = focusRing.Style.ToLowerInvariant();
        if (!IsBorderStyle(style) || style == "hidden" || style == "none")
        {
            throw new ArgumentException("The focus-ring style is not supported.", "focusRing");
        }

        List<UiCssDeclaration> declarations = new List<UiCssDeclaration>();
        AddGeneratedDeclaration(declarations, "outline-color", NormalizeColor(focusRing.Color));
        AddGeneratedDeclaration(declarations, CssProperties.OutlineOffset, FormatLength(focusRing.Offset, false));
        AddGeneratedDeclaration(declarations, "outline-style", style);
        AddGeneratedDeclaration(declarations, "outline-width", FormatNonNegativeLength(focusRing.Width, "focusRingWidth"));
        return declarations.ToArray();
    }

    internal static string FormatNonNegativeLength(UiLength length, string parameterName)
    {
        if (length.HasValue && length.Value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, length.Value, "The CSS length cannot be negative in this context.");
        }

        return FormatLength(length, false);
    }

    internal static string FormatZLayer(string? layer, int offset)
    {
        if (layer == null)
        {
            throw new ArgumentNullException("layer");
        }

        int baseValue;
        switch (layer)
        {
            case "base":
                baseValue = 0;
                break;
            case "dropdown":
                baseValue = 1000;
                break;
            case "sticky":
                baseValue = 1100;
                break;
            case "overlay":
                baseValue = 1200;
                break;
            case "modal":
                baseValue = 1300;
                break;
            case "popover":
                baseValue = 1400;
                break;
            case "tooltip":
                baseValue = 1500;
                break;
            case "toast":
                baseValue = 1600;
                break;
            default:
                throw new ArgumentException("The CSS z-layer is not supported.", "layer");
        }

        if (offset < -99 || offset > 99)
        {
            throw new ArgumentOutOfRangeException("offset", offset, "Z-layer offsets must be between -99 and 99.");
        }

        return InvariantFormatter.FormatInt32(baseValue + offset);
    }

    internal static string CreateControlStateSelector(string? scopeId, string? controlSelector, string? state)
    {
        string scope = Guard.ArgumentNotWhiteSpace(scopeId, "scopeId");
        string selector = CssEscaper.EscapeSelector(controlSelector);
        string checkedState = Guard.ArgumentNotWhiteSpace(state, "state");
        if (selector.Contains(",", StringComparison.Ordinal))
        {
            throw new ArgumentException("Control-state selector bases cannot contain selector lists.", "controlSelector");
        }

        string prefix = "." + CssEscaper.EscapeIdentifier(scope) + " " + selector;
        switch (checkedState)
        {
            case "default":
                return prefix;
            case "hover":
                return prefix + ":hover";
            case "focus":
                return prefix + ":focus-visible";
            case "active":
                return prefix + ":active";
            case "disabled":
                return prefix + ":disabled," + prefix + "[aria-disabled=\"true\"]";
            case "checked":
                return prefix + ":checked," + prefix + "[aria-checked=\"true\"]";
            case "selected":
                return prefix + "[aria-selected=\"true\"]";
            case "expanded":
                return prefix + "[aria-expanded=\"true\"]";
            case "invalid":
                return prefix + ":invalid," + prefix + "[aria-invalid=\"true\"]";
            case "busy":
                return prefix + "[aria-busy=\"true\"]";
            case "read-only":
                return prefix + ":read-only";
            default:
                throw new ArgumentException("The control state is not supported.", "state");
        }
    }

    internal static void AddGeneratedDeclaration(List<UiCssDeclaration> declarations, string property, string value)
    {
        UiCssDeclaration declaration = new UiCssDeclaration { Property = property, Value = value, IsTrusted = true };
        int index = declarations.Count;
        declarations.Add(declaration);
        while (index > 0 && string.CompareOrdinal(declarations[index - 1].Property, property) > 0)
        {
            declarations[index] = declarations[index - 1];
            index--;
        }

        declarations[index] = declaration;
    }
}
