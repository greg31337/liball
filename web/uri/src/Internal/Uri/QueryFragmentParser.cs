using System;

namespace uri;

internal static class QueryFragmentParser
{
    internal static QueryFragmentScanResult ScanQuery(
        ReadOnlySpan<char> text,
        int startOffset,
        int baseOffset)
    {
        if (startOffset < 0 || startOffset > text.Length)
        {
            return CreateInvalidInputFailure();
        }

        if (startOffset >= text.Length || text[startOffset] != '?')
        {
            return new QueryFragmentScanResult(
                true,
                false,
                startOffset,
                0,
                startOffset,
                null);
        }

        int valueOffset = startOffset + 1;
        int componentEnd = UriParserCore.FindFirstCharacter(
            text,
            valueOffset,
            text.Length,
            '#');
        if (componentEnd < 0)
        {
            componentEnd = text.Length;
        }

        int offset = valueOffset;
        while (offset < componentEnd)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text.Slice(0, componentEnd),
                    offset,
                    baseOffset,
                    UriComponent.Query);
                if (!triplet.Success)
                {
                    UriDiagnostic percent = CreatePercentFailure(
                        triplet,
                        UriComponent.Query,
                        baseOffset,
                        offset);
                    return new QueryFragmentScanResult(false, false, 0, 0, 0, percent);
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!PercentAllowlist.IsQueryCharacter(text[offset]))
            {
                UriDiagnostic invalid = DiagnosticFactory.CreateInvalidQuery(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
                return new QueryFragmentScanResult(false, false, 0, 0, 0, invalid);
            }

            offset = offset + 1;
        }

        return new QueryFragmentScanResult(
            true,
            true,
            valueOffset,
            componentEnd - valueOffset,
            componentEnd,
            null);
    }

    internal static QueryFragmentScanResult ScanFragment(
        ReadOnlySpan<char> text,
        int startOffset,
        int baseOffset)
    {
        if (startOffset < 0 || startOffset > text.Length)
        {
            return CreateInvalidInputFailure();
        }

        if (startOffset >= text.Length || text[startOffset] != '#')
        {
            return new QueryFragmentScanResult(
                true,
                false,
                startOffset,
                0,
                startOffset,
                null);
        }

        int valueOffset = startOffset + 1;
        int offset = valueOffset;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    baseOffset,
                    UriComponent.Fragment);
                if (!triplet.Success)
                {
                    UriDiagnostic percent = CreatePercentFailure(
                        triplet,
                        UriComponent.Fragment,
                        baseOffset,
                        offset);
                    return new QueryFragmentScanResult(false, false, 0, 0, 0, percent);
                }

                offset = triplet.NextOffset;
                continue;
            }

            if (!PercentAllowlist.IsFragmentCharacter(text[offset]))
            {
                UriDiagnostic invalid = DiagnosticFactory.CreateInvalidFragment(
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    1);
                return new QueryFragmentScanResult(false, false, 0, 0, 0, invalid);
            }

            offset = offset + 1;
        }

        return new QueryFragmentScanResult(
            true,
            true,
            valueOffset,
            offset - valueOffset,
            offset,
            null);
    }

    internal static QueryFragmentScanResult CreateInvalidInputFailure()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        return new QueryFragmentScanResult(false, false, 0, 0, 0, diagnostic);
    }

    internal static UriDiagnostic CreatePercentFailure(
        PercentTripletResult triplet,
        UriComponent component,
        int baseOffset,
        int relativeOffset)
    {
        UriDiagnostic? diagnostic = triplet.Diagnostic;
        if (diagnostic != null)
        {
            return DiagnosticFactory.CreateUriInvalidPercentEncoding(
                component,
                diagnostic.Range.Offset,
                diagnostic.Range.Length);
        }

        return DiagnosticFactory.CreateUriInvalidPercentEncoding(
            component,
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            1);
    }
}
