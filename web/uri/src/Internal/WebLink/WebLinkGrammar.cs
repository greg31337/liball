using System;

namespace uri;

internal static class WebLinkGrammar
{
    internal const string InvalidTokenMessage = "The Web Link parameter token is invalid.";
    internal const string InvalidAngleTargetMessage = "The Web Link target must be enclosed in angle brackets.";
    internal const string ExpectedSemicolonMessage = "A semicolon is required before the Web Link parameter.";
    internal const string ExpectedCommaMessage = "A comma is required between Web Link values.";

    internal static bool IsOptionalWhitespaceCharacter(char value)
    {
        return value == ' ' || value == '\t';
    }

    internal static bool IsTokenCharacter(char value)
    {
        return Ascii.IsAlphaNumeric(value) ||
            value == '!' ||
            value == '#' ||
            value == '$' ||
            value == '%' ||
            value == '&' ||
            value == '\'' ||
            value == '*' ||
            value == '+' ||
            value == '-' ||
            value == '.' ||
            value == '^' ||
            value == '_' ||
            value == '`' ||
            value == '|' ||
            value == '~';
    }

    internal static WebLinkScanResult ScanOptionalWhitespace(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        if (!IsValidOffset(text, offset))
        {
            return CreateInvalidInput(offset);
        }

        int nextOffset = offset;
        while (nextOffset < text.Length &&
            IsOptionalWhitespaceCharacter(text[nextOffset]))
        {
            nextOffset = nextOffset + 1;
        }

        return CreateSuccess(
            text.Slice(offset, nextOffset - offset).ToString(),
            offset,
            nextOffset,
            baseOffset);
    }

    internal static WebLinkScanResult ScanToken(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        if (!IsValidOffset(text, offset))
        {
            return CreateInvalidInput(offset);
        }

        if (offset == text.Length || !IsTokenCharacter(text[offset]))
        {
            int length = 0;
            if (offset < text.Length)
            {
                length = 1;
            }

            return CreateFailure(
                offset,
                offset,
                baseOffset,
                UriDiagnosticCode.InvalidLinkParameter,
                UriComponent.LinkParameter,
                offset,
                length,
                InvalidTokenMessage);
        }

        int nextOffset = offset + 1;
        while (nextOffset < text.Length &&
            IsTokenCharacter(text[nextOffset]))
        {
            nextOffset = nextOffset + 1;
        }

        return CreateSuccess(
            text.Slice(offset, nextOffset - offset).ToString(),
            offset,
            nextOffset,
            baseOffset);
    }

    internal static WebLinkScanResult ScanAngleTarget(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        if (!IsValidOffset(text, offset))
        {
            return CreateInvalidInput(offset);
        }

        if (offset == text.Length || text[offset] != '<')
        {
            int length = 0;
            if (offset < text.Length)
            {
                length = 1;
            }

            return CreateFailure(
                offset,
                offset,
                baseOffset,
                UriDiagnosticCode.InvalidLinkTarget,
                UriComponent.LinkTarget,
                offset,
                length,
                InvalidAngleTargetMessage);
        }

        int nextOffset = offset + 1;
        while (nextOffset < text.Length && text[nextOffset] != '>')
        {
            nextOffset = nextOffset + 1;
        }

        if (nextOffset == text.Length)
        {
            return CreateFailure(
                offset,
                offset,
                baseOffset,
                UriDiagnosticCode.InvalidLinkTarget,
                UriComponent.LinkTarget,
                nextOffset,
                0,
                InvalidAngleTargetMessage);
        }

        string value = text.Slice(
            offset + 1,
            nextOffset - offset - 1).ToString();
        return CreateSuccess(
            value,
            offset,
            nextOffset + 1,
            baseOffset);
    }

    internal static WebLinkScanResult ScanSemicolon(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        return ScanDelimiter(
            text,
            offset,
            baseOffset,
            ';',
            ExpectedSemicolonMessage);
    }

    internal static WebLinkScanResult ScanComma(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset)
    {
        return ScanDelimiter(
            text,
            offset,
            baseOffset,
            ',',
            ExpectedCommaMessage);
    }

    internal static WebLinkScanResult ScanDelimiter(
        ReadOnlySpan<char> text,
        int offset,
        int baseOffset,
        char delimiter,
        string message)
    {
        if (!IsValidOffset(text, offset))
        {
            return CreateInvalidInput(offset);
        }

        if (offset == text.Length || text[offset] != delimiter)
        {
            int length = 0;
            if (offset < text.Length)
            {
                length = 1;
            }

            return CreateFailure(
                offset,
                offset,
                baseOffset,
                UriDiagnosticCode.InvalidWebLink,
                UriComponent.WebLink,
                offset,
                length,
                message);
        }

        return CreateSuccess(
            delimiter.ToString(),
            offset,
            offset + 1,
            baseOffset);
    }

    internal static bool IsValidOffset(
        ReadOnlySpan<char> text,
        int offset)
    {
        return offset >= 0 && offset <= text.Length;
    }

    internal static WebLinkScanResult CreateSuccess(
        string value,
        int startOffset,
        int nextOffset,
        int baseOffset)
    {
        int absoluteOffset = UriParserCore.GetAbsoluteOffset(
            baseOffset,
            startOffset);
        return new WebLinkScanResult(
            true,
            value,
            startOffset,
            nextOffset,
            new TextRange(absoluteOffset, nextOffset - startOffset),
            null);
    }

    internal static WebLinkScanResult CreateInvalidInput(int offset)
    {
        return new WebLinkScanResult(
            false,
            "",
            offset,
            offset,
            new TextRange(0, 0),
            DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.WebLink));
    }

    internal static WebLinkScanResult CreateFailure(
        int startOffset,
        int nextOffset,
        int baseOffset,
        UriDiagnosticCode code,
        UriComponent component,
        int errorOffset,
        int errorLength,
        string message)
    {
        int absoluteStart = UriParserCore.GetAbsoluteOffset(
            baseOffset,
            startOffset);
        int absoluteError = UriParserCore.GetAbsoluteOffset(
            baseOffset,
            errorOffset);
        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            code,
            component,
            absoluteError,
            errorLength,
            message);
        return new WebLinkScanResult(
            false,
            "",
            startOffset,
            nextOffset,
            new TextRange(absoluteStart, 0),
            diagnostic);
    }
}
