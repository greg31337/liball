namespace version;

internal static class VersionOptionDispatcher
{
    internal static VersionParseResult Parse(string text, VersionParseOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateParseOptions(options);

        if (text.Length > options.GetMaxInputLength())
        {
            return CreateVersionInputTooLongFailure(text, options.GetDialect(), options.GetMaxInputLength());
        }

        VersionParseResult result = ParseWithMode(text, options);
        return ApplyVersionParseSwitches(result, options);
    }

    internal static VersionRangeParseResult ParseRange(string text, VersionRangeParseOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateRangeParseOptions(options);

        if (text.Length > options.GetMaxInputLength())
        {
            return CreateRangeInputTooLongFailure(text, options.GetRangeSyntax(), options.GetMaxInputLength());
        }

        VersionRangeParseResult result = ParseRangeWithSyntax(text, options.GetRangeSyntax());
        if (result.IsFailure())
        {
            return result;
        }

        return ApplyRangeParseSwitches(result, options);
    }

    internal static int Compare(string leftText, string rightText, VersionComparisonOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(leftText);
        System.ArgumentNullException.ThrowIfNull(rightText);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateComparisonOptions(options);

        VersionParseOptions parseOptions = VersionParseOptions.CreateForDialect(options.GetDialect());
        VersionParseResult leftResult = Parse(leftText, parseOptions);
        VersionParseResult rightResult = Parse(rightText, parseOptions);

        if (leftResult.IsFailure() || rightResult.IsFailure())
        {
            return CompareInvalidResults(leftResult, rightResult, options.GetInvalidVersionOrdering());
        }

        return CompareVersions(leftResult.GetVersion(), rightResult.GetVersion(), options);
    }

    internal static int CompareVersions(UniversalVersion left, UniversalVersion right, VersionComparisonOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);
        System.ArgumentNullException.ThrowIfNull(options);
        ValidateComparisonOptions(options);

        VersionComparisonMode mode = options.GetComparisonMode();
        if (mode == VersionComparisonMode.StabilityAware)
        {
            if (left.IsPrerelease() && !right.IsPrerelease())
            {
                return -1;
            }

            if (!left.IsPrerelease() && right.IsPrerelease())
            {
                return 1;
            }

            mode = VersionComparisonMode.Dialect;
        }

        if (options.IncludesBuildMetadata()
            && (mode == VersionComparisonMode.Precedence || mode == VersionComparisonMode.Dialect))
        {
            mode = VersionComparisonMode.Identity;
        }

        if (!options.IsCaseSensitive() && UsesSemVerFamilyComparison(left, right, options.GetDialect()))
        {
            if (mode == VersionComparisonMode.Identity)
            {
                return SemVerVersionComparer.CompareIdentityCaseInsensitive(left, right);
            }

            return SemVerVersionComparer.ComparePrecedenceCaseInsensitive(left, right);
        }

        if (options.GetDialect() != VersionDialect.Unknown)
        {
            return VersionDialectStrategies.Get(options.GetDialect()).Compare(left, right, mode);
        }

        return VersionDialectStrategies.GetForComparison(left, right).Compare(left, right, mode);
    }

    internal static bool UsesSemVerFamilyComparison(
        UniversalVersion left,
        UniversalVersion right,
        VersionDialect dialect)
    {
        if (dialect != VersionDialect.Unknown)
        {
            return IsSemVerFamilyDialect(dialect);
        }

        return IsSemVerFamilyDialect(left.GetDialect()) && IsSemVerFamilyDialect(right.GetDialect());
    }

    internal static void ValidateParseOptions(VersionParseOptions options)
    {
        VersionParseOptions.ValidateDialect(options.GetDialect());
        VersionParseOptions.ValidateParseMode(options.GetParseMode());
        VersionParseOptions.ValidateMaxInputLength(options.GetMaxInputLength());
        VersionParseOptions.ValidatePrereleasePolicy(options.GetPrereleasePolicy());
    }

    internal static void ValidateRangeParseOptions(VersionRangeParseOptions options)
    {
        VersionRangeParseOptions.ValidateRangeSyntax(options.GetRangeSyntax());
        VersionRangeParseOptions.ValidatePrereleasePolicy(options.GetPrereleasePolicy());
        VersionRangeParseOptions.ValidateMaxInputLength(options.GetMaxInputLength());
    }

    internal static void ValidateComparisonOptions(VersionComparisonOptions options)
    {
        VersionComparisonOptions.ValidateDialect(options.GetDialect());
        VersionComparisonOptions.ValidateComparisonMode(options.GetComparisonMode());
        VersionComparisonOptions.ValidateInvalidVersionOrdering(options.GetInvalidVersionOrdering());
    }

    internal static bool IsSemVerFamilyDialect(VersionDialect dialect)
    {
        return dialect == VersionDialect.SemVer
            || dialect == VersionDialect.NodeSemVer
            || dialect == VersionDialect.Cargo
            || dialect == VersionDialect.Go;
    }

    internal static VersionParseOptions CreateParseOptionsForDialect(VersionParseOptions options, VersionDialect dialect)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (options.GetDialect() != VersionDialect.Unknown)
        {
            return options;
        }

        return new VersionParseOptions(
            dialect,
            options.GetParseMode(),
            options.GetMaxInputLength(),
            options.AllowsPrereleaseVersions(),
            options.AllowsBuildMetadata(),
            options.GetPrereleasePolicy());
    }

    internal static VersionRangeParseOptions CreateRangeOptionsForSyntax(VersionRangeParseOptions options, RangeSyntax rangeSyntax)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (options.GetRangeSyntax() != RangeSyntax.Unknown)
        {
            return options;
        }

        return new VersionRangeParseOptions(
            rangeSyntax,
            options.GetPrereleasePolicy(),
            options.GetMaxInputLength(),
            options.AllowsWildcardVersions(),
            options.AllowsHyphenRanges(),
            options.AllowsFloatingVersions(),
            options.AllowsMultipleSets(),
            options.AllowsUnboundedRanges());
    }

    internal static VersionComparisonOptions CreateComparisonOptionsForDialect(
        VersionComparisonOptions options,
        VersionDialect dialect)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (options.GetDialect() != VersionDialect.Unknown)
        {
            return options;
        }

        return new VersionComparisonOptions(
            dialect,
            options.GetComparisonMode(),
            options.IsCaseSensitive(),
            options.IncludesBuildMetadata(),
            options.GetInvalidVersionOrdering());
    }

    internal static VersionParseResult ParseWithMode(string text, VersionParseOptions options)
    {
        if (options.GetParseMode() == VersionParseMode.Strict)
        {
            return ParseStrict(text, options.GetDialect());
        }

        if (options.GetParseMode() == VersionParseMode.Loose)
        {
            return ParseLoose(text, options.GetDialect());
        }

        if (options.GetParseMode() == VersionParseMode.Coerce)
        {
            return ParseCoerce(text, options.GetDialect());
        }

        if (options.GetParseMode() == VersionParseMode.Partial)
        {
            return ParsePartial(text, options.GetDialect());
        }

        return ParseDialectSpecific(text, options.GetDialect());
    }

    internal static VersionParseResult ParseDialectSpecific(string text, VersionDialect dialect)
    {
        return VersionDialectStrategies.Get(dialect).Parse(text);
    }

    internal static VersionParseResult ParseStrict(string text, VersionDialect dialect)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);
        return RetagSemVerFamilyResult(result, text, dialect);
    }

    internal static VersionParseResult ParseLoose(string text, VersionDialect dialect)
    {
        VersionParseResult result = SemVerParser.ParseLoose(text);
        return RetagSemVerFamilyResult(result, text, dialect);
    }

    internal static VersionParseResult ParseCoerce(string text, VersionDialect dialect)
    {
        VersionParseResult result = SemVerParser.Coerce(text);
        return RetagSemVerFamilyResult(result, text, dialect);
    }

    internal static VersionParseResult ParsePartial(string text, VersionDialect dialect)
    {
        VersionParseResult result = SemVerParser.ParsePartial(text);
        return RetagSemVerFamilyResult(result, text, dialect);
    }

    internal static VersionParseResult RetagSemVerFamilyResult(VersionParseResult result, string text, VersionDialect dialect)
    {
        if (result.IsFailure())
        {
            return result;
        }

        if (dialect != VersionDialect.NodeSemVer && dialect != VersionDialect.Cargo)
        {
            return result;
        }

        UniversalVersion version = result.GetVersion();
        UniversalVersion retagged = new UniversalVersion(
            version.GetOriginalText(),
            dialect,
            version.GetNumericComponents(),
            version.GetPrereleaseIdentifiers(),
            version.GetBuildIdentifiers(),
            version.GetMissingComponentCount(),
            version.HasLeadingVPrefix(),
            version.HasRevisionComponent(),
            version.GetRevisionComponent(),
            null);

        return VersionParseResult.CreateSuccess(text, dialect, retagged);
    }

    internal static VersionParseResult ApplyVersionParseSwitches(VersionParseResult result, VersionParseOptions options)
    {
        if (result.IsFailure())
        {
            return result;
        }

        UniversalVersion version = result.GetVersion();
        if (!options.AllowsPrereleaseVersions() && version.IsPrerelease())
        {
            return CreateVersionOptionFailure(
                result.GetSourceText(),
                result.GetDialect(),
                "VERSION_PRERELEASE_NOT_ALLOWED",
                "Prerelease versions are not allowed by these parse options.");
        }

        if (!options.AllowsBuildMetadata() && version.HasBuildMetadata())
        {
            return CreateVersionOptionFailure(
                result.GetSourceText(),
                result.GetDialect(),
                "VERSION_BUILD_METADATA_NOT_ALLOWED",
                "Build metadata is not allowed by these parse options.");
        }

        return result;
    }

    internal static VersionRangeParseResult ParseRangeWithSyntax(string text, RangeSyntax rangeSyntax)
    {
        return RangeSyntaxStrategies.Get(rangeSyntax).Parse(text);
    }

    internal static VersionRangeParseResult ApplyRangeParseSwitches(VersionRangeParseResult result, VersionRangeParseOptions options)
    {
        VersionRange range = result.GetRange();

        if (!options.AllowsMultipleSets() && range.GetSetCount() > 1)
        {
            return CreateRangeOptionFailure(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                "Multiple range sets are not allowed by these parse options.");
        }

        if (!options.AllowsUnboundedRanges() && range.IsUniversal())
        {
            return CreateRangeOptionFailure(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                "Unbounded ranges are not allowed by these parse options.");
        }

        if (!options.AllowsWildcardVersions() && ContainsWildcard(result.GetSourceText()))
        {
            return CreateRangeOptionFailure(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                "Wildcard versions are not allowed by these parse options.");
        }

        if (!options.AllowsHyphenRanges() && ContainsHyphenRange(result.GetSourceText()))
        {
            return CreateRangeOptionFailure(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                "Hyphen ranges are not allowed by these parse options.");
        }

        if (!options.AllowsFloatingVersions() && result.GetRangeSyntax() == RangeSyntax.NuGet && ContainsWildcard(result.GetSourceText()))
        {
            return CreateRangeOptionFailure(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                "Floating versions are not allowed by these parse options.");
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Include)
        {
            return VersionRangeParseResult.CreateSuccess(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                VersionRange.CreateWithPrereleaseAdmissionFromRange(range, true));
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Exclude)
        {
            return VersionRangeParseResult.CreateSuccess(
                result.GetSourceText(),
                result.GetRangeSyntax(),
                VersionRange.CreateWithPrereleaseAdmissionFromRange(range, false));
        }

        return result;
    }

    internal static bool ContainsWildcard(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '*' || text[index] == 'x' || text[index] == 'X')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsHyphenRange(string text)
    {
        int index = 1;

        while (index < text.Length - 1)
        {
            if (text[index] == '-' && VersionChar.IsWhitespace(text[index - 1]) && VersionChar.IsWhitespace(text[index + 1]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int CompareInvalidResults(
        VersionParseResult leftResult,
        VersionParseResult rightResult,
        InvalidVersionOrdering invalidVersionOrdering)
    {
        if (invalidVersionOrdering == InvalidVersionOrdering.Reject)
        {
            throw new System.ArgumentException("Invalid version text.");
        }

        if (leftResult.IsFailure() && rightResult.IsFailure())
        {
            return 0;
        }

        if (invalidVersionOrdering == InvalidVersionOrdering.TreatAsEqual)
        {
            return 0;
        }

        if (leftResult.IsFailure())
        {
            if (invalidVersionOrdering == InvalidVersionOrdering.SortBeforeValid)
            {
                return -1;
            }

            return 1;
        }

        if (invalidVersionOrdering == InvalidVersionOrdering.SortBeforeValid)
        {
            return 1;
        }

        return -1;
    }

    internal static VersionParseResult CreateVersionInputTooLongFailure(string text, VersionDialect dialect, int maxInputLength)
    {
        return VersionParseResult.CreateFailure(
            text,
            dialect,
            VersionDiagnostic.FromIndex(
                SemVerDiagnosticCodes.InputTooLong,
                "Version input exceeds the configured maximum length.",
                text,
                maxInputLength));
    }

    internal static VersionParseResult CreateVersionOptionFailure(string text, VersionDialect dialect, string code, string message)
    {
        return VersionParseResult.CreateFailure(
            text,
            dialect,
            VersionDiagnostic.FromIndex(code, message, text, 0));
    }

    internal static VersionRangeParseResult CreateRangeInputTooLongFailure(string text, RangeSyntax rangeSyntax, int maxInputLength)
    {
        return VersionRangeParseResult.CreateFailure(
            text,
            rangeSyntax,
            VersionDiagnostic.FromIndex(
                NodeRangeDiagnosticCodes.InputTooLong,
                "Range input exceeds the configured maximum length.",
                text,
                maxInputLength));
    }

    internal static VersionRangeParseResult CreateRangeOptionFailure(string text, RangeSyntax rangeSyntax, string message)
    {
        return VersionRangeParseResult.CreateFailure(
            text,
            rangeSyntax,
            VersionDiagnostic.FromIndex(
                "RANGE_OPTION_NOT_ALLOWED",
                message,
                text,
                0));
    }
}
