using System;

namespace iso20022;

internal static class BooleanLexical
{
    internal static IsoBooleanValue? ParseBoolean(ReadOnlySpan<char> text)
    {
        if (text.SequenceEqual("true") || text.SequenceEqual("1"))
        {
            IsoBooleanValue trueResult = new IsoBooleanValue();
            trueResult.Value = true;
            trueResult.LexicalValue = text.ToString();
            return trueResult;
        }

        if (text.SequenceEqual("false") || text.SequenceEqual("0"))
        {
            IsoBooleanValue falseResult = new IsoBooleanValue();
            falseResult.Value = false;
            falseResult.LexicalValue = text.ToString();
            return falseResult;
        }

        return null;
    }

    internal static string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }
}
