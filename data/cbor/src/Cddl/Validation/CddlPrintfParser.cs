using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CddlPrintfParser
{
    internal string Format;
    internal int Offset;
    internal int ArgumentCount = 1;
    internal string? Error;
    internal List<CddlPrintfPart> Parts = new List<CddlPrintfPart>();

    internal CddlPrintfParser(string format)
    {
        Format = format;
    }

    internal bool ParseFormat()
    {
        StringBuilder literal = new StringBuilder();
        while (Offset < Format.Length)
        {
            char current = Format[Offset];
            if (current != '%')
            {
                literal.Append(current);
                Offset = Offset + 1;
                continue;
            }
            if (Offset + 1 < Format.Length
                && Format[Offset + 1] == '%')
            {
                literal.Append('%');
                Offset = Offset + 2;
                continue;
            }
            AddLiteral(literal);
            if (!ParseDirective())
            {
                return false;
            }
        }
        AddLiteral(literal);
        return true;
    }

    internal void AddLiteral(StringBuilder literal)
    {
        if (literal.Length == 0)
        {
            return;
        }
        CddlPrintfPart part = new CddlPrintfPart();
        part.Literal = literal.ToString();
        Parts.Add(part);
        literal.Clear();
    }

    internal bool ParseDirective()
    {
        Offset = Offset + 1;
        CddlPrintfPart part = new CddlPrintfPart();
        ParseFlags(part);
        if (!ParseWidth(part) || !ParsePrecision(part))
        {
            return false;
        }
        if (Offset >= Format.Length)
        {
            Error = "A printf directive is incomplete.";
            return false;
        }
        char specifier = Format[Offset];
        if (IsLengthModifier(specifier))
        {
            Error = "Printf length modifiers are not supported by RFC 9741.";
            return false;
        }
        if (!IsSupportedSpecifier(specifier))
        {
            Error = "The printf directive %" + specifier
                + " is not supported.";
            return false;
        }
        part.Specifier = specifier;
        part.ArgumentIndex = ArgumentCount;
        ArgumentCount = ArgumentCount + 1;
        Offset = Offset + 1;
        if (!ValidateFlags(part))
        {
            return false;
        }
        Parts.Add(part);
        return true;
    }

    internal void ParseFlags(CddlPrintfPart part)
    {
        bool reading = true;
        while (reading && Offset < Format.Length)
        {
            char current = Format[Offset];
            if (current == '-')
            {
                part.LeftAligned = true;
            }
            else if (current == '+')
            {
                part.ShowSign = true;
            }
            else if (current == ' ')
            {
                part.SpaceSign = true;
            }
            else if (current == '#')
            {
                part.Alternate = true;
            }
            else if (current == '0')
            {
                part.ZeroPadded = true;
            }
            else
            {
                reading = false;
            }
            if (reading)
            {
                Offset = Offset + 1;
            }
        }
    }

    internal bool ParseWidth(CddlPrintfPart part)
    {
        if (Offset < Format.Length && Format[Offset] == '*')
        {
            Error = "Dynamic printf widths are not supported.";
            return false;
        }
        int width = ParseDecimalNumber();
        if (width == -2)
        {
            Error = "The printf field width is too large.";
            return false;
        }
        part.Width = width;
        return true;
    }

    internal bool ParsePrecision(CddlPrintfPart part)
    {
        if (Offset >= Format.Length || Format[Offset] != '.')
        {
            return true;
        }
        Offset = Offset + 1;
        if (Offset < Format.Length && Format[Offset] == '*')
        {
            Error = "Dynamic printf precisions are not supported.";
            return false;
        }
        int precision = ParseDecimalNumber();
        if (precision == -2)
        {
            Error = "The printf precision is too large.";
            return false;
        }
        part.Precision = precision < 0 ? 0 : precision;
        return true;
    }

    internal int ParseDecimalNumber()
    {
        if (Offset >= Format.Length
            || Format[Offset] < '0'
            || Format[Offset] > '9')
        {
            return -1;
        }
        int value = 0;
        while (Offset < Format.Length
            && Format[Offset] >= '0'
            && Format[Offset] <= '9')
        {
            int digit = Format[Offset] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return -2;
            }
            value = value * 10 + digit;
            Offset = Offset + 1;
        }
        return value;
    }

    internal bool IsLengthModifier(char value)
    {
        return value == 'h'
            || value == 'l'
            || value == 'j'
            || value == 'z'
            || value == 't'
            || value == 'L'
            || value == 'w';
    }

    internal bool IsSupportedSpecifier(char value)
    {
        return value == 'd'
            || value == 'i'
            || value == 'u'
            || value == 'o'
            || value == 'x'
            || value == 'X'
            || value == 'b'
            || value == 'B'
            || value == 'c'
            || value == 's';
    }

    internal bool ValidateFlags(CddlPrintfPart part)
    {
        char specifier = part.Specifier;
        bool signed = specifier == 'd' || specifier == 'i';
        bool based = specifier == 'o'
            || specifier == 'x'
            || specifier == 'X'
            || specifier == 'b'
            || specifier == 'B';
        if ((specifier == 'c' || specifier == 's')
            && (part.ShowSign
                || part.SpaceSign
                || part.Alternate
                || part.ZeroPadded))
        {
            Error = "The printf flags are incompatible with %"
                + specifier + ".";
            return false;
        }
        if (specifier == 'c' && part.Precision >= 0)
        {
            Error = "A printf %c directive cannot have a precision.";
            return false;
        }
        if (!signed && (part.ShowSign || part.SpaceSign))
        {
            Error = "Sign flags require a signed decimal printf directive.";
            return false;
        }
        if (!based && part.Alternate)
        {
            Error = "The alternate-form flag requires an octal, hexadecimal, or binary directive.";
            return false;
        }
        return true;
    }
}
