using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace pdf;

internal class PdfSvgNumberReader
{
    internal string Text;
    internal int Position;

    internal PdfSvgNumberReader(string text)
    {
        Text = text;
        Position = 0;
    }

    internal void SkipSeparators()
    {
        while (Position < Text.Length)
        {
            char value = Text[Position];
            if (char.IsWhiteSpace(value) || value == ',')
            {
                Position++;
            }
            else
            {
                return;
            }
        }
    }

    internal bool HasMore()
    {
        SkipSeparators();
        return Position < Text.Length;
    }

    internal bool NextIsNumber()
    {
        SkipSeparators();
        if (Position >= Text.Length)
        {
            return false;
        }

        char value = Text[Position];
        return value == '+' || value == '-' || value == '.' || value >= '0' && value <= '9';
    }

    internal double ReadNumber()
    {
        SkipSeparators();
        int start = Position;
        if (Position < Text.Length && (Text[Position] == '+' || Text[Position] == '-'))
        {
            Position++;
        }

        bool hasDigits = false;
        while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
        {
            Position++;
            hasDigits = true;
        }

        if (Position < Text.Length && Text[Position] == '.')
        {
            Position++;
            while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
            {
                Position++;
                hasDigits = true;
            }
        }

        if (!hasDigits)
        {
            throw new InvalidDataException("SVG numeric value is invalid.");
        }

        if (Position < Text.Length && (Text[Position] == 'e' || Text[Position] == 'E'))
        {
            Position++;
            if (Position < Text.Length && (Text[Position] == '+' || Text[Position] == '-'))
            {
                Position++;
            }

            int exponentStart = Position;
            while (Position < Text.Length && Text[Position] >= '0' && Text[Position] <= '9')
            {
                Position++;
            }

            if (Position == exponentStart)
            {
                throw new InvalidDataException("SVG numeric exponent is invalid.");
            }
        }

        double result = double.Parse(Text.Substring(start, Position - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new InvalidDataException("SVG numeric values must be finite.");
        }

        return result;
    }

    internal char ReadCommand()
    {
        SkipSeparators();
        if (Position >= Text.Length || !char.IsLetter(Text[Position]))
        {
            throw new InvalidDataException("SVG path command is missing.");
        }

        char command = Text[Position];
        Position++;
        return command;
    }

    internal string ReadIdentifier()
    {
        SkipSeparators();
        int start = Position;
        while (Position < Text.Length && char.IsLetter(Text[Position]))
        {
            Position++;
        }

        if (Position == start)
        {
            throw new InvalidDataException("SVG identifier is missing.");
        }

        return Text.Substring(start, Position - start);
    }

    internal void RequireCharacter(char expected)
    {
        SkipSeparators();
        if (Position >= Text.Length || Text[Position] != expected)
        {
            throw new InvalidDataException("SVG syntax is missing a required delimiter.");
        }

        Position++;
    }
}

internal static class PdfSvgValueParser
{
    internal static double[] ParseNumberList(string value)
    {
        PdfSvgNumberReader reader = new PdfSvgNumberReader(value);
        List<double> values = new List<double>();
        while (reader.HasMore())
        {
            values.Add(reader.ReadNumber());
        }

        return values.ToArray();
    }

    internal static double ParseLengthPoints(string value)
    {
        return ParseLengthValue(value, true);
    }

    internal static double ParseLengthUserUnits(string value)
    {
        return ParseLengthValue(value, false);
    }

    internal static double ParseLengthValue(string value, bool points)
    {
        PdfSvgNumberReader reader = new PdfSvgNumberReader(value.Trim());
        double number = reader.ReadNumber();
        reader.SkipSeparators();
        string unit = reader.Text.Substring(reader.Position).ToLowerInvariant();
        double pointFactor = 0.75;
        double userFactor = 1;
        if (unit.Length == 0 || unit == "px")
        {
            pointFactor = 0.75;
            userFactor = 1;
        }
        else if (unit == "pt")
        {
            pointFactor = 1;
            userFactor = 4.0 / 3.0;
        }
        else if (unit == "pc")
        {
            pointFactor = 12;
            userFactor = 16;
        }
        else if (unit == "in")
        {
            pointFactor = 72;
            userFactor = 96;
        }
        else if (unit == "cm")
        {
            pointFactor = 72 / 2.54;
            userFactor = 96 / 2.54;
        }
        else if (unit == "mm")
        {
            pointFactor = 72 / 25.4;
            userFactor = 96 / 25.4;
        }
        else
        {
            throw new InvalidDataException("SVG length unit is unsupported.");
        }

        return number * (points ? pointFactor : userFactor);
    }
}
