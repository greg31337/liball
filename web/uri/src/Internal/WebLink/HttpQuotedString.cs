using System;

namespace uri;

internal static class HttpQuotedString
{
    internal const string InvalidQuotedStringMessage = "The Web Link quoted-string is invalid or incomplete.";
    internal const string InvalidQuotedPairMessage = "The Web Link quoted-pair is invalid or incomplete.";

    internal static bool IsQuotedTextCharacter(char value)
    {
        return value == '\t' ||
            value == ' ' ||
            value == '!' ||
            (value >= '#' && value <= '[') ||
            (value >= ']' && value <= '~') ||
            (value >= '\u0080' && value <= '\u00ff');
    }

    internal static bool IsQuotedPairCharacter(char value)
    {
        return value == '\t' ||
            (value >= ' ' && value <= '~') ||
            (value >= '\u0080' && value <= '\u00ff');
    }

    internal static WebLinkScanResult ScanQuotedPair(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        if (!WebLinkGrammar.IsValidOffset(text, offset))
        {
            return WebLinkGrammar.CreateInvalidInput(offset);
        }

        if (offset == text.Length || text[offset] != '\\')
        {
            int length = 0;
            if (offset < text.Length)
            {
                length = 1;
            }

            return CreateQuotedFailure(
                offset,
                offset,
                baseOffset,
                offset,
                length,
                InvalidQuotedPairMessage);
        }

        if (offset + 1 >= text.Length)
        {
            return CreateQuotedFailure(
                offset,
                offset,
                baseOffset,
                text.Length,
                0,
                InvalidQuotedPairMessage);
        }

        if (!IsQuotedPairCharacter(text[offset + 1]))
        {
            return CreateQuotedFailure(
                offset,
                offset,
                baseOffset,
                offset + 1,
                1,
                InvalidQuotedPairMessage);
        }

        return WebLinkGrammar.CreateSuccess(
            text[offset + 1].ToString(),
            offset,
            offset + 2,
            baseOffset);
    }

    internal static WebLinkScanResult ScanQuotedString(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        if (!WebLinkGrammar.IsValidOffset(text, offset))
        {
            return WebLinkGrammar.CreateInvalidInput(offset);
        }

        if (offset == text.Length || text[offset] != '"')
        {
            int length = 0;
            if (offset < text.Length)
            {
                length = 1;
            }

            return CreateQuotedFailure(
                offset,
                offset,
                baseOffset,
                offset,
                length,
                InvalidQuotedStringMessage);
        }

        TextOutput output = new TextOutput(text.Length - offset);
        int nextOffset = offset + 1;
        while (nextOffset < text.Length)
        {
            char value = text[nextOffset];
            if (value == '"')
            {
                return WebLinkGrammar.CreateSuccess(
                    output.GetText(),
                    offset,
                    nextOffset + 1,
                    baseOffset);
            }

            if (value == '\\')
            {
                WebLinkScanResult pair = ScanQuotedPair(
                    text,
                    nextOffset,
                    baseOffset);
                if (!pair.Success)
                {
                    return new WebLinkScanResult(
                        false,
                        "",
                        offset,
                        offset,
                        new TextRange(
                            UriParserCore.GetAbsoluteOffset(
                                baseOffset,
                                offset),
                            0),
                        pair.Diagnostic);
                }

                output.AppendText(pair.Value);
                nextOffset = pair.NextOffset;
                continue;
            }

            if (!IsQuotedTextCharacter(value))
            {
                return CreateQuotedFailure(
                    offset,
                    offset,
                    baseOffset,
                    nextOffset,
                    1,
                    InvalidQuotedStringMessage);
            }

            output.AppendCharacter(value);
            nextOffset = nextOffset + 1;
        }

        return CreateQuotedFailure(
            offset,
            offset,
            baseOffset,
            text.Length,
            0,
            InvalidQuotedStringMessage);
    }

    internal static WebLinkScanResult CreateQuotedFailure(
        int startOffset,
        int nextOffset,
        int baseOffset,
        int errorOffset,
        int errorLength,
        string message)
    {
        return WebLinkGrammar.CreateFailure(
            startOffset,
            nextOffset,
            baseOffset,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            errorOffset,
            errorLength,
            message);
    }
}
