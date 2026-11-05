using System;

namespace uri;

internal static class IriPathParser
{
    internal static PathParseResult ParseAbEmpty(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length == 0)
        {
            return new PathParseResult(true, 0, null);
        }

        if (text[0] != '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        return ValidateResult(text, baseOffset, false, PathParser.CountSlashes(text));
    }

    internal static PathParseResult ParseAbsolute(ReadOnlySpan<char> text, int baseOffset)
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

        int segmentCount = PathParser.CountSlashes(text);
        if (text.Length == 1)
        {
            segmentCount = 0;
        }

        return ValidateResult(text, baseOffset, false, segmentCount);
    }

    internal static PathParseResult ParseNoScheme(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        if (text[0] == '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        return ValidateResult(
            text,
            baseOffset,
            true,
            PathParser.CountSlashes(text) + 1);
    }

    internal static PathParseResult ParseRootless(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length == 0)
        {
            return CreateFailure(baseOffset, 0, 0);
        }

        if (text[0] == '/')
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        return ValidateResult(
            text,
            baseOffset,
            false,
            PathParser.CountSlashes(text) + 1);
    }

    internal static PathParseResult ParseEmpty(ReadOnlySpan<char> text, int baseOffset)
    {
        if (text.Length != 0)
        {
            return CreateFailure(baseOffset, 0, 1);
        }

        return new PathParseResult(true, 0, null);
    }

    internal static PathParseResult ValidateResult(
        ReadOnlySpan<char> text,
        int baseOffset,
        bool forbidColonInFirstSegment,
        int segmentCount)
    {
        UriDiagnostic? diagnostic = IriComponentScanner.ValidatePath(
            text,
            baseOffset,
            forbidColonInFirstSegment);
        if (diagnostic != null)
        {
            return new PathParseResult(false, 0, diagnostic);
        }

        return new PathParseResult(true, segmentCount, null);
    }

    internal static PathParseResult CreateFailure(
        int baseOffset,
        int relativeOffset,
        int length)
    {
        UriDiagnostic diagnostic = IriComponentScanner.CreateComponentFailure(
            UriComponent.Path,
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            length);
        return new PathParseResult(false, 0, diagnostic);
    }
}
