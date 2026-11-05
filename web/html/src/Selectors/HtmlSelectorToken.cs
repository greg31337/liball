using System;

namespace html;

internal enum HtmlSelectorTokenKind
{
    End = 0,
    Whitespace = 1,
    Identifier = 2,
    Function = 3,
    Hash = 4,
    String = 5,
    BadString = 6,
    Number = 7,
    Percentage = 8,
    Dimension = 9,
    Delimiter = 10
}

internal class HtmlSelectorToken
{
    internal HtmlSelectorTokenKind kind;
    internal string value;
    internal string unit;
    internal string representation;
    internal string numberRepresentation;
    internal double number;
    internal bool integer;
    internal bool idHash;
    internal HtmlSourceSpan sourceSpan;

    internal HtmlSelectorToken(HtmlSelectorTokenKind kind, string value,
        string unit, string representation, string numberRepresentation,
        double number, bool integer, bool idHash, HtmlSourceSpan sourceSpan)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (unit == null)
        {
            throw new ArgumentNullException("unit");
        }

        if (representation == null)
        {
            throw new ArgumentNullException("representation");
        }

        if (numberRepresentation == null)
        {
            throw new ArgumentNullException("numberRepresentation");
        }

        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.kind = kind;
        this.value = value;
        this.unit = unit;
        this.representation = representation;
        this.numberRepresentation = numberRepresentation;
        this.number = number;
        this.integer = integer;
        this.idHash = idHash;
        this.sourceSpan = sourceSpan;
    }

    internal HtmlSelectorTokenKind GetKind()
    {
        return this.kind;
    }

    internal string GetValue()
    {
        return this.value;
    }

    internal string GetUnit()
    {
        return this.unit;
    }

    internal string GetRepresentation()
    {
        return this.representation;
    }

    internal string GetNumberRepresentation()
    {
        return this.numberRepresentation;
    }

    internal double GetNumber()
    {
        return this.number;
    }

    internal bool IsInteger()
    {
        return this.integer;
    }

    internal bool IsIdHash()
    {
        return this.idHash;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }
}

internal class HtmlSelectorNumberData
{
    internal string representation;
    internal double value;
    internal bool integer;

    internal HtmlSelectorNumberData(string representation, double value,
        bool integer)
    {
        this.representation = representation;
        this.value = value;
        this.integer = integer;
    }

    internal string GetRepresentation()
    {
        return this.representation;
    }

    internal double GetValue()
    {
        return this.value;
    }

    internal bool IsInteger()
    {
        return this.integer;
    }
}
