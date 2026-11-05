using System;

namespace pdf;

/// <summary>
/// Provides approximate measurements for the standard PDF fonts used by text drawing and layout.
/// </summary>
public class PdfFontMetrics : IPdfFontMetrics
{
    /// <summary>
    /// Stores the font used by this object.
    /// </summary>
    internal PdfFont Font;
    /// <summary>
    /// Stores the base font name used by this object.
    /// </summary>
    internal string BaseFontName;
    /// <summary>
    /// Stores the numeric default width used by this object.
    /// </summary>
    internal int DefaultWidth;
    /// <summary>
    /// Stores the numeric space width used by this object.
    /// </summary>
    internal int SpaceWidth;
    /// <summary>
    /// Stores the numeric ascent used by this object.
    /// </summary>
    internal int Ascent;
    /// <summary>
    /// Stores the numeric descent used by this object.
    /// </summary>
    internal int Descent;
    /// <summary>
    /// Stores the numeric cap height used by this object.
    /// </summary>
    internal int CapHeight;
    /// <summary>
    /// Stores the numeric x height used by this object.
    /// </summary>
    internal int XHeight;
    /// <summary>
    /// Stores the numeric line gap used by this object.
    /// </summary>
    internal int LineGap;

    /// <summary>
    /// Initializes metrics for one standard PDF font using its width table and vertical measurements.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="baseFontName">The base font name to use for this operation.</param>
    /// <param name="defaultWidth">The default width in PDF points.</param>
    /// <param name="spaceWidth">The space width in PDF points.</param>
    /// <param name="ascent">The ascent to use for this operation.</param>
    /// <param name="descent">The descent to use for this operation.</param>
    /// <param name="capHeight">The cap height in PDF points.</param>
    /// <param name="xHeight">The x height in PDF points.</param>
    /// <param name="lineGap">The line gap to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when the base font name is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the base font name is empty.</exception>
    public PdfFontMetrics(PdfFont font, string baseFontName, int defaultWidth, int spaceWidth, int ascent, int descent, int capHeight, int xHeight, int lineGap)
    {
        if (baseFontName == null)
        {
            throw new ArgumentNullException("baseFontName");
        }

        if (baseFontName.Length == 0)
        {
            throw new ArgumentException("Base font name cannot be empty.", "baseFontName");
        }

        Font = font;
        BaseFontName = baseFontName;
        DefaultWidth = defaultWidth;
        SpaceWidth = spaceWidth;
        Ascent = ascent;
        Descent = descent;
        CapHeight = capHeight;
        XHeight = xHeight;
        LineGap = lineGap;
    }

    /// <summary>
    /// Returns the character width calculated from the object's current state.
    /// </summary>
    /// <param name="character">The character to use for this operation.</param>
    /// <returns>The calculated integer value.</returns>
    public int GetCharacterWidth(char character)
    {
        if (IsCourierFont(BaseFontName))
        {
            return GetCourierWidth(character);
        }

        if (IsTimesFont(BaseFontName))
        {
            return GetTimesWidth(character);
        }

        if (BaseFontName == "Symbol")
        {
            return GetSymbolWidth(character);
        }

        if (BaseFontName == "ZapfDingbats")
        {
            return GetZapfDingbatsWidth(character);
        }

        return GetHelveticaWidth(character);
    }

    /// <summary>
    /// Measures a text width without emitting drawing commands.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public double MeasureTextWidth(string text, double fontSize)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize < 0)
        {
            throw new ArgumentOutOfRangeException("fontSize", "Font size must be finite and nonnegative.");
        }

        long width = 0;

        foreach (char character in text)
        {
            width += GetCharacterWidth(character);
        }

        return width * fontSize / 1000.0;
    }

    /// <summary>
    /// Returns the line height calculated from the object's current state.
    /// </summary>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public double GetLineHeight(double fontSize)
    {
        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize < 0)
        {
            throw new ArgumentOutOfRangeException("fontSize", "Font size must be finite and nonnegative.");
        }

        return ((double)Ascent - Descent + LineGap) * fontSize / 1000.0;
    }

    /// <summary>
    /// Returns the metrics calculated from the object's current state.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting font metrics.</returns>
    internal static PdfFontMetrics GetMetrics(PdfFont font)
    {
        string baseFontName = PdfFontRegistry.GetStandardBaseFontName(font);

        if (IsCourierFont(baseFontName))
        {
            return new PdfFontMetrics(font, baseFontName, 600, 600, 629, -157, 562, 426, 200);
        }

        if (IsTimesFont(baseFontName))
        {
            return new PdfFontMetrics(font, baseFontName, 444, 250, 683, -217, 662, 450, 200);
        }

        if (baseFontName == "Symbol")
        {
            return new PdfFontMetrics(font, baseFontName, 500, 250, 1010, -293, 0, 0, 200);
        }

        if (baseFontName == "ZapfDingbats")
        {
            return new PdfFontMetrics(font, baseFontName, 500, 278, 820, -143, 0, 0, 200);
        }

        return new PdfFontMetrics(font, baseFontName, 500, 278, 718, -207, 718, 523, 200);
    }

    /// <summary>
    /// Measures the text without emitting drawing commands.
    /// </summary>
    /// <param name="font">The font to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    internal static double MeasureText(PdfFont font, string text, double fontSize)
    {
        PdfFontMetrics metrics = GetMetrics(font);
        return metrics.MeasureTextWidth(text, fontSize);
    }

    internal static int GetCourierWidth(char character)
    {
        return 600;
    }

    internal static int GetHelveticaWidth(char character)
    {
        if (character == ' ')
        {
            return 278;
        }

        if (ContainsCharacter("!(),.:;[]`{}", character))
        {
            return 278;
        }

        if (ContainsCharacter("\"'*+-/\\_~", character))
        {
            return 333;
        }

        if (ContainsCharacter("fijlrI", character))
        {
            return 278;
        }

        if (ContainsCharacter("mwMW", character))
        {
            return 889;
        }

        if (character >= '0' && character <= '9')
        {
            return 556;
        }

        if (character >= 'A' && character <= 'Z')
        {
            return 667;
        }

        if (character >= 'a' && character <= 'z')
        {
            return 500;
        }

        return 500;
    }

    internal static int GetTimesWidth(char character)
    {
        if (character == ' ')
        {
            return 250;
        }

        if (ContainsCharacter("!(),.:;[]`{}", character))
        {
            return 250;
        }

        if (ContainsCharacter("\"'*+-/\\_~", character))
        {
            return 333;
        }

        if (ContainsCharacter("fijlr", character))
        {
            return 278;
        }

        if (character == 'I')
        {
            return 333;
        }

        if (ContainsCharacter("mwMW", character))
        {
            return 778;
        }

        if (character >= '0' && character <= '9')
        {
            return 500;
        }

        if (character >= 'A' && character <= 'Z')
        {
            return 667;
        }

        if (character >= 'a' && character <= 'z')
        {
            return 444;
        }

        return 444;
    }

    internal static int GetSymbolWidth(char character)
    {
        if (character == ' ')
        {
            return 250;
        }

        return 500;
    }

    internal static int GetZapfDingbatsWidth(char character)
    {
        if (character == ' ')
        {
            return 278;
        }

        return 500;
    }

    internal static bool IsCourierFont(string baseFontName)
    {
        return baseFontName.StartsWith("Courier", StringComparison.Ordinal);
    }

    internal static bool IsTimesFont(string baseFontName)
    {
        return baseFontName.StartsWith("Times", StringComparison.Ordinal);
    }

    internal static bool ContainsCharacter(string characters, char character)
    {
        foreach (char current in characters)
        {
            if (current == character)
            {
                return true;
            }
        }

        return false;
    }
}
