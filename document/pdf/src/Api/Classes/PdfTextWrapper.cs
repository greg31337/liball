using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Measures and splits text into lines that fit a requested width using the library's built-in font metrics.
/// </summary>
public class PdfTextWrapper : IPdfTextWrapper
{
    /// <summary>
    /// Specifies the max width in PDF points.
    /// </summary>
    internal double MaxWidth;
    /// <summary>
    /// Stores the numeric font size used by this object.
    /// </summary>
    internal double FontSize;
    /// <summary>
    /// Stores the font used by this object.
    /// </summary>
    internal PdfFont Font;

    /// <summary>
    /// Initializes a new text wrapper.
    /// </summary>
    /// <param name="maxWidth">The max width in PDF points.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    public PdfTextWrapper(double maxWidth, double fontSize, PdfFont font)
    {
        Validate(maxWidth, fontSize);

        MaxWidth = maxWidth;
        FontSize = fontSize;
        Font = font;
    }

    /// <summary>
    /// Creates a text wrapper that uses the regular font.
    /// </summary>
    /// <param name="maxWidth">The max width in PDF points.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The resulting text wrapper.</returns>
    internal static PdfTextWrapper Regular(double maxWidth, double fontSize)
    {
        return new PdfTextWrapper(maxWidth, fontSize, PdfFont.Regular);
    }

    /// <summary>
    /// Creates a text wrapper from an existing text style.
    /// </summary>
    /// <param name="maxWidth">The max width in PDF points.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <returns>The resulting text wrapper.</returns>
    internal static PdfTextWrapper FromStyle(double maxWidth, PdfTextStyle style)
    {
        ValidateTextStyle(style);
        return new PdfTextWrapper(maxWidth, style.FontSize, style.Font);
    }

    /// <summary>
    /// Splits the supplied text into lines that fit the requested width.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The resulting values in their processing order.</returns>
    public string[] Wrap(string text)
    {
        return WrapText(text, MaxWidth, FontSize, Font);
    }

    /// <summary>
    /// Splits the supplied text into lines that fit the requested width.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="maxWidth">The max width in PDF points.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <returns>The resulting values in their processing order.</returns>
    internal static string[] WrapText(string text, double maxWidth, double fontSize)
    {
        return WrapText(text, maxWidth, fontSize, PdfFont.Regular);
    }

    /// <summary>
    /// Splits the supplied text into lines that fit the requested width.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="maxWidth">The max width in PDF points.</param>
    /// <param name="fontSize">The font size in PDF points.</param>
    /// <param name="font">The font to use for this operation.</param>
    /// <returns>The resulting values in their processing order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static string[] WrapText(string text, double maxWidth, double fontSize, PdfFont font)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Validate(maxWidth, fontSize);

        List<string> lines = new List<string>();
        string line = string.Empty;
        string word = string.Empty;

        foreach (char character in text)
        {
            if (character == '\r')
            {
                continue;
            }

            if (character == '\n')
            {
                line = AppendWord(lines, line, word, maxWidth, fontSize, font);
                word = string.Empty;
                lines.Add(line);
                line = string.Empty;
            }
            else if (IsWordSeparator(character))
            {
                line = AppendWord(lines, line, word, maxWidth, fontSize, font);
                word = string.Empty;
            }
            else
            {
                word += character.ToString();
            }
        }

        line = AppendWord(lines, line, word, maxWidth, fontSize, font);
        if (line.Length > 0)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Splits the supplied text into lines that fit the requested width.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="maxWidth">The max width in PDF points.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <returns>The resulting values in their processing order.</returns>
    internal static string[] WrapText(string text, double maxWidth, PdfTextStyle style)
    {
        ValidateTextStyle(style);
        if (style.TrueTypeFont == null)
        {
            return WrapText(text, maxWidth, style.FontSize, style.Font);
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Validate(maxWidth, style.FontSize);
        List<string> lines = new List<string>();
        string line = string.Empty;
        string word = string.Empty;
        foreach (char character in text)
        {
            if (character == '\r')
            {
                continue;
            }

            if (character == '\n')
            {
                line = AppendStyledWord(lines, line, word, maxWidth, style);
                word = string.Empty;
                lines.Add(line);
                line = string.Empty;
            }
            else if (IsWordSeparator(character))
            {
                line = AppendStyledWord(lines, line, word, maxWidth, style);
                word = string.Empty;
            }
            else
            {
                word += character.ToString();
            }
        }

        line = AppendStyledWord(lines, line, word, maxWidth, style);
        if (line.Length > 0)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    internal static string AppendStyledWord(List<string> lines, string line, string word, double maxWidth, PdfTextStyle style)
    {
        if (word.Length == 0)
        {
            return line;
        }

        if (line.Length == 0)
        {
            return AppendStyledWordToEmptyLine(lines, word, maxWidth, style);
        }

        string candidate = line + " " + word;
        if (PdfCanvas.MeasureText(candidate, style) <= maxWidth)
        {
            return candidate;
        }

        lines.Add(line);
        return AppendStyledWordToEmptyLine(lines, word, maxWidth, style);
    }

    internal static string AppendStyledWordToEmptyLine(List<string> lines, string word, double maxWidth, PdfTextStyle style)
    {
        if (PdfCanvas.MeasureText(word, style) <= maxWidth)
        {
            return word;
        }

        return SplitLongStyledWord(lines, word, maxWidth, style);
    }

    internal static string SplitLongStyledWord(List<string> lines, string word, double maxWidth, PdfTextStyle style)
    {
        string line = string.Empty;
        foreach (char character in word)
        {
            string candidate = line + character.ToString();
            if (line.Length == 0 || PdfCanvas.MeasureText(candidate, style) <= maxWidth)
            {
                line = candidate;
            }
            else
            {
                lines.Add(line);
                line = character.ToString();
            }
        }

        return line;
    }

    internal static string AppendWord(List<string> lines, string line, string word, double maxWidth, double fontSize, PdfFont font)
    {
        if (word.Length == 0)
        {
            return line;
        }

        if (line.Length == 0)
        {
            return AppendWordToEmptyLine(lines, word, maxWidth, fontSize, font);
        }

        string candidate = line + " " + word;
        if (PdfCanvas.MeasureText(candidate, fontSize, font) <= maxWidth)
        {
            return candidate;
        }

        lines.Add(line);
        return AppendWordToEmptyLine(lines, word, maxWidth, fontSize, font);
    }

    internal static string AppendWordToEmptyLine(List<string> lines, string word, double maxWidth, double fontSize, PdfFont font)
    {
        if (PdfCanvas.MeasureText(word, fontSize, font) <= maxWidth)
        {
            return word;
        }

        return SplitLongWord(lines, word, maxWidth, fontSize, font);
    }

    internal static string SplitLongWord(List<string> lines, string word, double maxWidth, double fontSize, PdfFont font)
    {
        string line = string.Empty;

        foreach (char character in word)
        {
            string candidate = line + character.ToString();
            if (line.Length == 0 || PdfCanvas.MeasureText(candidate, fontSize, font) <= maxWidth)
            {
                line = candidate;
            }
            else
            {
                lines.Add(line);
                line = character.ToString();
            }
        }

        return line;
    }

    internal static bool IsWordSeparator(char character)
    {
        if (character == ' ')
        {
            return true;
        }

        if (character == '\t')
        {
            return true;
        }

        if (character == '\f')
        {
            return true;
        }

        if (character == '\v')
        {
            return true;
        }

        return false;
    }

    internal static void Validate(double maxWidth, double fontSize)
    {
        if (double.IsNaN(maxWidth) || double.IsInfinity(maxWidth) || maxWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("maxWidth", "Maximum width must be finite and positive.");
        }

        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException("fontSize", "Font size must be finite and positive.");
        }
    }

    internal static void ValidateTextStyle(PdfTextStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }
    }
}
