namespace chart;
/// <summary>
/// Represents a color used by the chart model or rendering pipeline.
/// </summary>
public class ChartColor : IChartColor
{
    internal byte R;
    internal byte G;
    internal byte B;
    internal byte A;
    /// <summary>
    /// Initializes a color from the supplied r, g, b, and a.
    /// </summary>
    /// <param name = "r">The red channel from 0 through 255.</param>
    /// <param name = "g">The green channel from 0 through 255.</param>
    /// <param name = "b">The blue channel from 0 through 255.</param>
    /// <param name = "a">The alpha channel from 0 for transparent through 255 for opaque.</param>
    public ChartColor(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    internal static ChartColor FromRgb(byte r, byte g, byte b)
    {
        return new ChartColor(r, g, b, 255);
    }

    internal static ChartColor FromRgba(byte r, byte g, byte b, byte a)
    {
        return new ChartColor(r, g, b, a);
    }

    internal static ChartColor Transparent()
    {
        return new ChartColor(0, 0, 0, 0);
    }

    internal static ChartColor Black()
    {
        return new ChartColor(0, 0, 0, 255);
    }

    internal static ChartColor White()
    {
        return new ChartColor(255, 255, 255, 255);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartColor Copy()
    {
        return new ChartColor(R, G, B, A);
    }

    /// <summary>
    /// Compares all four color channels with another color.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool EqualsColor(ChartColor color)
    {
        return R == color.R && G == color.G && B == color.B && A == color.A;
    }

    /// <summary>
    /// Determines whether this value is opaque.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsOpaque()
    {
        return A == 255;
    }

    /// <summary>
    /// Determines whether this value is transparent.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsTransparent()
    {
        return A == 0;
    }

    /// <summary>
    /// Creates a copy with the specified alpha.
    /// </summary>
    /// <param name = "alpha">The alpha channel from 0 for transparent through 255 for opaque.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor WithAlpha(byte alpha)
    {
        return new ChartColor(R, G, B, alpha);
    }

    /// <summary>
    /// Creates a copy with the specified opacity.
    /// </summary>
    /// <param name = "opacity">The opacity from zero for transparent through one for opaque.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor WithOpacity(double opacity)
    {
        double clamped = ClampDouble(opacity, 0.0, 1.0);
        byte alpha = RoundToByte(clamped * 255.0);
        return WithAlpha(alpha);
    }

    /// <summary>
    /// Scales the red, green, and blue channels by the alpha fraction.
    /// </summary>
    /// <returns>The resulting color.</returns>
    public ChartColor PremultiplyAlpha()
    {
        double factor = A / 255.0;
        byte r = RoundToByte(R * factor);
        byte g = RoundToByte(G * factor);
        byte b = RoundToByte(B * factor);
        return new ChartColor(r, g, b, A);
    }

    /// <summary>
    /// Restores straight color channels from a premultiplied-alpha color.
    /// </summary>
    /// <returns>The resulting color.</returns>
    public ChartColor UnpremultiplyAlpha()
    {
        if (A == 0)
        {
            return new ChartColor(0, 0, 0, 0);
        }

        double factor = 255.0 / A;
        byte r = RoundToByte(R * factor);
        byte g = RoundToByte(G * factor);
        byte b = RoundToByte(B * factor);
        return new ChartColor(r, g, b, A);
    }

    /// <summary>
    /// Converts this value to hex rgb.
    /// </summary>
    /// <returns>The converted string.</returns>
    public string ToHexRgb()
    {
        return "#" + ByteToHex(R) + ByteToHex(G) + ByteToHex(B);
    }

    /// <summary>
    /// Converts this value to hex rgba.
    /// </summary>
    /// <returns>The converted string.</returns>
    public string ToHexRgba()
    {
        return "#" + ByteToHex(R) + ByteToHex(G) + ByteToHex(B) + ByteToHex(A);
    }

    internal static ChartColor Lerp(ChartColor start, ChartColor end, double amount)
    {
        double clamped = ClampDouble(amount, 0.0, 1.0);
        byte r = LerpByte(start.R, end.R, clamped);
        byte g = LerpByte(start.G, end.G, clamped);
        byte b = LerpByte(start.B, end.B, clamped);
        byte a = LerpByte(start.A, end.A, clamped);
        return new ChartColor(r, g, b, a);
    }

    internal static ChartColor Parse(string text)
    {
        ChartColor? color = TryParse(text);
        if (color == null)
        {
            throw new System.FormatException("Unsupported chart color.");
        }

        return color;
    }

    internal static ChartColor? TryParse(string? text)
    {
        if (text == null)
        {
            return null;
        }

        string value = text.Trim();
        if (value.Length == 0)
        {
            return null;
        }

        if (value[0] == '#')
        {
            return TryParseHex(value);
        }

        string lower = value.ToLowerInvariant();
        ChartColor? named = TryParseNamed(lower);
        if (named != null)
        {
            return named;
        }

        if (lower.StartsWith("rgb(", System.StringComparison.Ordinal) && lower.EndsWith(")", System.StringComparison.Ordinal))
        {
            return TryParseRgbFunction(lower, false);
        }

        if (lower.StartsWith("rgba(", System.StringComparison.Ordinal) && lower.EndsWith(")", System.StringComparison.Ordinal))
        {
            return TryParseRgbFunction(lower, true);
        }

        return null;
    }

    internal static ChartColor? TryParseHex(string value)
    {
        if (value.Length == 4)
        {
            int r = HexDigit(value[1]);
            int g = HexDigit(value[2]);
            int b = HexDigit(value[3]);
            if (r < 0 || g < 0 || b < 0)
            {
                return null;
            }

            return new ChartColor((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), 255);
        }

        if (value.Length == 5)
        {
            int r = HexDigit(value[1]);
            int g = HexDigit(value[2]);
            int b = HexDigit(value[3]);
            int a = HexDigit(value[4]);
            if (r < 0 || g < 0 || b < 0 || a < 0)
            {
                return null;
            }

            return new ChartColor((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), (byte)(a * 17));
        }

        if (value.Length == 7)
        {
            int r = HexByte(value, 1);
            int g = HexByte(value, 3);
            int b = HexByte(value, 5);
            if (r < 0 || g < 0 || b < 0)
            {
                return null;
            }

            return new ChartColor((byte)r, (byte)g, (byte)b, 255);
        }

        if (value.Length == 9)
        {
            int r = HexByte(value, 1);
            int g = HexByte(value, 3);
            int b = HexByte(value, 5);
            int a = HexByte(value, 7);
            if (r < 0 || g < 0 || b < 0 || a < 0)
            {
                return null;
            }

            return new ChartColor((byte)r, (byte)g, (byte)b, (byte)a);
        }

        return null;
    }

    internal static ChartColor? TryParseNamed(string lower)
    {
        if (lower == "transparent")
        {
            return Transparent();
        }

        if (lower == "black")
        {
            return Black();
        }

        if (lower == "white")
        {
            return White();
        }

        if (lower == "red")
        {
            return new ChartColor(255, 0, 0, 255);
        }

        if (lower == "green")
        {
            return new ChartColor(0, 128, 0, 255);
        }

        if (lower == "blue")
        {
            return new ChartColor(0, 0, 255, 255);
        }

        if (lower == "yellow")
        {
            return new ChartColor(255, 255, 0, 255);
        }

        if (lower == "cyan")
        {
            return new ChartColor(0, 255, 255, 255);
        }

        if (lower == "magenta")
        {
            return new ChartColor(255, 0, 255, 255);
        }

        if (lower == "gray" || lower == "grey")
        {
            return new ChartColor(128, 128, 128, 255);
        }

        return null;
    }

    internal static ChartColor? TryParseRgbFunction(string lower, bool hasAlpha)
    {
        int open = lower.IndexOf('(');
        int close = lower.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return null;
        }

        string body = lower.Substring(open + 1, close - open - 1);
        string[] parts = body.Split(new char[] { ',' });
        if (!hasAlpha && parts.Length != 3)
        {
            return null;
        }

        if (hasAlpha && parts.Length != 4)
        {
            return null;
        }

        int r = ParseByteComponent(parts[0]);
        int g = ParseByteComponent(parts[1]);
        int b = ParseByteComponent(parts[2]);
        if (r < 0 || g < 0 || b < 0)
        {
            return null;
        }

        int a = 255;
        if (hasAlpha)
        {
            a = ParseAlphaComponent(parts[3]);
            if (a < 0)
            {
                return null;
            }
        }

        return new ChartColor((byte)r, (byte)g, (byte)b, (byte)a);
    }

    internal static int ParseByteComponent(string text)
    {
        string value = text.Trim();
        if (value.EndsWith("%", System.StringComparison.Ordinal))
        {
            int percent = ParseInteger(value.Substring(0, value.Length - 1));
            if (percent < 0 || percent > 100)
            {
                return -1;
            }

            return RoundToByte(percent * 255.0 / 100.0);
        }

        int component = ParseInteger(value);
        if (component < 0 || component > 255)
        {
            return -1;
        }

        return component;
    }

    internal static int ParseAlphaComponent(string text)
    {
        string value = text.Trim();
        if (value.EndsWith("%", System.StringComparison.Ordinal))
        {
            int percent = ParseInteger(value.Substring(0, value.Length - 1));
            if (percent < 0 || percent > 100)
            {
                return -1;
            }

            return RoundToByte(percent * 255.0 / 100.0);
        }

        if (ContainsDecimalPoint(value))
        {
            double alpha = ParseDecimal01(value);
            if (alpha < 0.0)
            {
                return -1;
            }

            return RoundToByte(alpha * 255.0);
        }

        int component = ParseInteger(value);
        if (component < 0 || component > 255)
        {
            return -1;
        }

        return component;
    }

    internal static int ParseInteger(string text)
    {
        string value = text.Trim();
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char c = value[index];
            if (c < '0' || c > '9')
            {
                return -1;
            }

            result = result * 10 + (c - '0');
            if (result > 1000000)
            {
                return -1;
            }

            index = index + 1;
        }

        return result;
    }

    internal static double ParseDecimal01(string text)
    {
        string value = text.Trim();
        if (value.Length == 0)
        {
            return -1.0;
        }

        double result = 0.0;
        double factor = 0.1;
        bool seenPoint = false;
        int index = 0;
        while (index < value.Length)
        {
            char c = value[index];
            if (c == '.')
            {
                if (seenPoint)
                {
                    return -1.0;
                }

                seenPoint = true;
            }
            else if (c >= '0' && c <= '9')
            {
                if (seenPoint)
                {
                    result = result + (c - '0') * factor;
                    factor = factor * 0.1;
                }
                else
                {
                    result = result * 10.0 + (c - '0');
                }
            }
            else
            {
                return -1.0;
            }

            index = index + 1;
        }

        if (result < 0.0 || result > 1.0)
        {
            return -1.0;
        }

        return result;
    }

    internal static bool ContainsDecimalPoint(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '.')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int HexByte(string value, int index)
    {
        int high = HexDigit(value[index]);
        int low = HexDigit(value[index + 1]);
        if (high < 0 || low < 0)
        {
            return -1;
        }

        return high * 16 + low;
    }

    internal static int HexDigit(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'a' && c <= 'f')
        {
            return c - 'a' + 10;
        }

        if (c >= 'A' && c <= 'F')
        {
            return c - 'A' + 10;
        }

        return -1;
    }

    internal static byte LerpByte(byte start, byte end, double amount)
    {
        return RoundToByte(start + (end - start) * amount);
    }

    internal static byte RoundToByte(double value)
    {
        return ClampToByte(System.Math.Floor(value + 0.5));
    }

    internal static byte ClampToByte(double value)
    {
        if (value <= 0.0)
        {
            return 0;
        }

        if (value >= 255.0)
        {
            return 255;
        }

        return (byte)value;
    }

    internal static double ClampDouble(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    internal static string ByteToHex(byte value)
    {
        return value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the current r.</summary>
    /// <returns>The current field value.</returns>
    public byte GetR()
    {
        return R;
    }

    /// <summary>Replaces the current r.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetR(byte value)
    {
        R = value;
    }

    /// <summary>Returns the current g.</summary>
    /// <returns>The current field value.</returns>
    public byte GetG()
    {
        return G;
    }

    /// <summary>Replaces the current g.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetG(byte value)
    {
        G = value;
    }

    /// <summary>Returns the current b.</summary>
    /// <returns>The current field value.</returns>
    public byte GetB()
    {
        return B;
    }

    /// <summary>Replaces the current b.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetB(byte value)
    {
        B = value;
    }

    /// <summary>Returns the current a.</summary>
    /// <returns>The current field value.</returns>
    public byte GetA()
    {
        return A;
    }

    /// <summary>Replaces the current a.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetA(byte value)
    {
        A = value;
    }
}
