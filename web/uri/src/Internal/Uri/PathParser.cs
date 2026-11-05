using System;

namespace uri;

internal static class PathParser
{
    internal static PathParseResult ParseAbEmpty(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        if (text.Length == 0)
        {
            return new PathParseResult(true, 0, null);
        }

        if (text[0] != '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        UriDiagnostic? diagnostic = ValidatePathCharacters(text, baseOffset, false);
        if (diagnostic != null)
        {
            return new PathParseResult(false, 0, diagnostic);
        }

        return new PathParseResult(true, CountSlashes(text), null);
    }

    internal static PathParseResult ParseAbsolute(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        if (text[0] != '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        if (text.Length > 1 && text[1] == '/')
        {
            return CreateFailure(baseOffset, 1, 1);
        }

        UriDiagnostic? diagnostic = ValidatePathCharacters(text, baseOffset, false);
        if (diagnostic != null)
        {
            return new PathParseResult(false, 0, diagnostic);
        }

        int segmentCount = CountSlashes(text);
        if (text.Length == 1)
        {
            segmentCount = 0;
        }

        return new PathParseResult(true, segmentCount, null);
    }

    internal static PathParseResult ParseNoScheme(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        if (text[0] == '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        UriDiagnostic? diagnostic = ValidatePathCharacters(text, baseOffset, true);
        if (diagnostic != null)
        {
            return new PathParseResult(false, 0, diagnostic);
        }

        return new PathParseResult(true, CountSlashes(text) + 1, null);
    }

    internal static PathParseResult ParseRootless(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        if (text[0] == '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        UriDiagnostic? diagnostic = ValidatePathCharacters(text, baseOffset, false);
        if (diagnostic != null)
        {
            return new PathParseResult(false, 0, diagnostic);
        }

        return new PathParseResult(true, CountSlashes(text) + 1, null);
    }

    internal static PathParseResult ParseEmpty(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        if (text.Length != 0)
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        return new PathParseResult(true, 0, null);
    }

    internal static UriDiagnostic? ValidatePathCharacters(
        ReadOnlySpan<char> text,
        int baseOffset,
        bool forbidColonInFirstSegment)
    {
        bool firstSegment = true;
        int offset = 0;
        while (offset < text.Length)
        {
            char value = text[offset];
            if (value == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    baseOffset,
                    UriComponent.Path);
                if (!triplet.Success)
                {
                    return CreatePercentFailure(triplet, baseOffset, offset);
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!PercentAllowlist.IsPathCharacter(value))
            {
                return DiagnosticFactory.CreateInvalidPath(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
            }

            if (forbidColonInFirstSegment && firstSegment && value == ':')
            {
                return DiagnosticFactory.CreateInvalidPath(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
            }

            if (value == '/')
            {
                firstSegment = false;
            }

            offset = offset + 1;
        }

        return null;
    }

    internal static int CountSlashes(ReadOnlySpan<char> text)
    {
        int count = 0;
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '/')
            {
                count = count + 1;
            }

            offset = offset + 1;
        }

        return count;
    }

    internal static UriDiagnostic CreatePercentFailure(
        PercentTripletResult triplet,
        int baseOffset,
        int relativeOffset)
    {
        UriDiagnostic? diagnostic = triplet.Diagnostic;
        if (diagnostic != null)
        {
            return DiagnosticFactory.CreateUriInvalidPercentEncoding(
                UriComponent.Path,
                diagnostic.Range.Offset,
                diagnostic.Range.Length);
        }

        return DiagnosticFactory.CreateUriInvalidPercentEncoding(
            UriComponent.Path,
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            1);
    }

    internal static PathParseResult CreateFailure(
        int baseOffset,
        int relativeOffset,
        int length)
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidPath(
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            length);
        return new PathParseResult(false, 0, diagnostic);
    }
}
