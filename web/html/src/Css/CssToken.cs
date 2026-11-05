using System;

namespace html;

internal enum CssTokenKind
{
    End = 0,
    Whitespace = 1,
    Identifier = 2,
    Function = 3,
    AtKeyword = 4,
    Hash = 5,
    String = 6,
    BadString = 7,
    Url = 8,
    BadUrl = 9,
    Number = 10,
    Percentage = 11,
    Dimension = 12,
    Delimiter = 13,
    Colon = 14,
    Semicolon = 15,
    Comma = 16,
    OpenSquare = 17,
    CloseSquare = 18,
    OpenParenthesis = 19,
    CloseParenthesis = 20,
    OpenCurly = 21,
    CloseCurly = 22,
    Cdo = 23,
    Cdc = 24
}

internal enum CssNumericType
{
    Integer = 0,
    Number = 1
}

internal enum CssHashType
{
    Unrestricted = 0,
    Id = 1
}

internal class CssToken
{
    internal CssTokenKind kind;
    internal string value;
    internal string unit;
    internal string representation;
    internal string numberRepresentation;
    internal double number;
    internal CssNumericType numericType;
    internal CssHashType hashType;
    internal HtmlSourceSpan sourceSpan;

    internal CssToken(CssTokenKind kind, string value, string unit,
        string representation, string numberRepresentation, double number,
        CssNumericType numericType, CssHashType hashType,
        HtmlSourceSpan sourceSpan)
    {
        if ((int)kind < 0 || (int)kind > (int)CssTokenKind.Cdc)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

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

        if ((int)numericType < 0
            || (int)numericType > (int)CssNumericType.Number)
        {
            throw new ArgumentOutOfRangeException("numericType");
        }

        if ((int)hashType < 0 || (int)hashType > (int)CssHashType.Id)
        {
            throw new ArgumentOutOfRangeException("hashType");
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
        this.numericType = numericType;
        this.hashType = hashType;
        this.sourceSpan = sourceSpan;
    }

    internal CssTokenKind GetKind()
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

    internal CssNumericType GetNumericType()
    {
        return this.numericType;
    }

    internal CssHashType GetHashType()
    {
        return this.hashType;
    }

    internal HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }
}

internal class CssTokenizeResult
{
    internal CssToken[] tokens;
    internal bool success;
    internal bool truncated;
    internal int errorCount;

    internal CssTokenizeResult(CssToken[] tokens, bool success,
        bool truncated, int errorCount)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (errorCount < 0)
        {
            throw new ArgumentOutOfRangeException("errorCount");
        }

        this.tokens = CopyTokens(tokens);
        this.success = success;
        this.truncated = truncated;
        this.errorCount = errorCount;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool WasTruncated()
    {
        return this.truncated;
    }

    internal int GetErrorCount()
    {
        return this.errorCount;
    }

    internal CssToken[] GetTokens()
    {
        return CopyTokens(this.tokens);
    }

    internal static CssToken[] CopyTokens(CssToken[] values)
    {
        CssToken[] result = new CssToken[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A CSS token collection cannot contain null.", "tokens");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal class CssNumberData
{
    internal string representation;
    internal double value;
    internal CssNumericType type;

    internal CssNumberData(string representation, double value,
        CssNumericType type)
    {
        this.representation = representation;
        this.value = value;
        this.type = type;
    }

    internal string GetRepresentation()
    {
        return this.representation;
    }

    internal double GetValue()
    {
        return this.value;
    }

    internal CssNumericType GetNumericType()
    {
        return this.type;
    }
}
