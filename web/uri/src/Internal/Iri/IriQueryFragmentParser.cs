using System;

namespace uri;

internal static class IriQueryFragmentParser
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
            return new QueryFragmentScanResult(true, false, startOffset, 0, startOffset, null);
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

        UriDiagnostic? diagnostic = IriComponentScanner.ValidateQuery(
            text.Slice(valueOffset, componentEnd - valueOffset),
            UriParserCore.GetAbsoluteOffset(baseOffset, valueOffset));
        if (diagnostic != null)
        {
            return new QueryFragmentScanResult(false, false, 0, 0, 0, diagnostic);
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
            return new QueryFragmentScanResult(true, false, startOffset, 0, startOffset, null);
        }

        int valueOffset = startOffset + 1;
        UriDiagnostic? diagnostic = IriComponentScanner.ValidateFragment(
            text.Slice(valueOffset),
            UriParserCore.GetAbsoluteOffset(baseOffset, valueOffset));
        if (diagnostic != null)
        {
            return new QueryFragmentScanResult(false, false, 0, 0, 0, diagnostic);
        }

        return new QueryFragmentScanResult(
            true,
            true,
            valueOffset,
            text.Length - valueOffset,
            text.Length,
            null);
    }

    internal static QueryFragmentScanResult CreateInvalidInputFailure()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        return new QueryFragmentScanResult(false, false, 0, 0, 0, diagnostic);
    }
}
