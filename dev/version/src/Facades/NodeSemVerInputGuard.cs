namespace version;

internal static class NodeSemVerInputGuard
{
    internal const int MaxInputLength = 256;

    internal static bool IsTooLong(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return text.Length > MaxInputLength;
    }

    internal static VersionDiagnostic CreateVersionNullDiagnostic()
    {
        return VersionDiagnostic.FromIndex(
            SemVerDiagnosticCodes.NullInput,
            "Version text cannot be null.",
            "",
            0);
    }

    internal static VersionDiagnostic CreateVersionLengthDiagnostic(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return VersionDiagnostic.FromIndex(
            SemVerDiagnosticCodes.InputTooLong,
            "Node SemVer input exceeds the maximum length.",
            text,
            MaxInputLength);
    }

    internal static VersionDiagnostic CreateRangeNullDiagnostic()
    {
        return VersionDiagnostic.FromIndex(
            NodeRangeDiagnosticCodes.NullInput,
            "Node SemVer range text cannot be null.",
            "",
            0);
    }

    internal static VersionDiagnostic CreateRangeLengthDiagnostic(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return VersionDiagnostic.FromIndex(
            NodeRangeDiagnosticCodes.InputTooLong,
            "Node SemVer range input exceeds the maximum length.",
            text,
            MaxInputLength);
    }

    internal static VersionParseResult CreateVersionNullFailure()
    {
        return VersionParseResult.CreateFailure("", VersionDialect.NodeSemVer, CreateVersionNullDiagnostic());
    }

    internal static VersionParseResult CreateVersionLengthFailure(string text)
    {
        return VersionParseResult.CreateFailure(text, VersionDialect.NodeSemVer, CreateVersionLengthDiagnostic(text));
    }

    internal static VersionRangeParseResult CreateRangeNullFailure()
    {
        return VersionRangeParseResult.CreateFailure("", RangeSyntax.NodeSemVer, CreateRangeNullDiagnostic());
    }

    internal static VersionRangeParseResult CreateRangeLengthFailure(string text)
    {
        return VersionRangeParseResult.CreateFailure(text, RangeSyntax.NodeSemVer, CreateRangeLengthDiagnostic(text));
    }

    internal static VersionComparatorParseResult CreateComparatorNullFailure()
    {
        return VersionComparatorParseResult.CreateFailure("", RangeSyntax.NodeSemVer, CreateRangeNullDiagnostic());
    }

    internal static VersionComparatorParseResult CreateComparatorLengthFailure(string text)
    {
        return VersionComparatorParseResult.CreateFailure(text, RangeSyntax.NodeSemVer, CreateRangeLengthDiagnostic(text));
    }
}
