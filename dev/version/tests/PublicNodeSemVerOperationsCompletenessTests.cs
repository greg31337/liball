using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicNodeSemVerOperationsCompletenessTests
{
    [Test]
    public void InputGuards_ApplyTheNodeSemVerLengthBoundaryToPublicResultPaths()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerCleanOptions strict = new NodeSemVerCleanOptions(false);
        string boundaryVersion = CreateBuildVersion(256);
        string tooLongVersion = CreateBuildVersion(257);
        string boundaryCoercion = new string('x', 251) + "1.2.3";
        string tooLongCoercion = new string('x', 252) + "1.2.3";
        string boundaryRange = CreateComparator(256);
        string tooLongRange = CreateComparator(257);

        Assert.That(operations.Valid(boundaryVersion, strict).valid, Is.True);
        Assert.That(operations.Clean(boundaryVersion, strict).valid, Is.True);
        Assert.That(operations.Parse(boundaryVersion).success, Is.True);
        Assert.That(operations.Coerce(boundaryCoercion).success, Is.True);
        Assert.That(operations.CoerceRightToLeft(boundaryCoercion).success, Is.True);
        Assert.That(operations.ParseRange(boundaryRange).success, Is.True);

        AssertDiagnostic(operations.Valid(tooLongVersion, strict).diagnostics, SemVerDiagnosticCodes.InputTooLong, 256);
        AssertDiagnostic(operations.Clean(tooLongVersion, strict).diagnostics, SemVerDiagnosticCodes.InputTooLong, 256);
        AssertDiagnostic(operations.Parse(tooLongVersion).diagnostics, SemVerDiagnosticCodes.InputTooLong, 256);
        AssertDiagnostic(operations.Coerce(tooLongCoercion).diagnostics, SemVerDiagnosticCodes.InputTooLong, 256);
        AssertDiagnostic(operations.CoerceRightToLeft(tooLongCoercion).diagnostics, SemVerDiagnosticCodes.InputTooLong, 256);
        AssertDiagnostic(operations.ParseRange(tooLongRange).diagnostics, NodeRangeDiagnosticCodes.InputTooLong, 256);
    }

    [Test]
    public void InputGuards_ReturnDiagnosticResultsForNullEmptyAndWhitespace()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerCleanOptions strict = new NodeSemVerCleanOptions(false);

        AssertDiagnostic(operations.Valid(null, strict).diagnostics, SemVerDiagnosticCodes.NullInput, 0);
        AssertDiagnostic(operations.Clean(null, strict).diagnostics, SemVerDiagnosticCodes.NullInput, 0);
        AssertDiagnostic(operations.Parse(null).diagnostics, SemVerDiagnosticCodes.NullInput, 0);
        AssertDiagnostic(operations.Coerce(null).diagnostics, SemVerDiagnosticCodes.NullInput, 0);
        AssertDiagnostic(operations.CoerceRightToLeft(null).diagnostics, SemVerDiagnosticCodes.NullInput, 0);
        AssertDiagnostic(operations.ParseRange(null).diagnostics, NodeRangeDiagnosticCodes.NullInput, 0);

        AssertDiagnostic(operations.Parse("").diagnostics, SemVerDiagnosticCodes.MissingMajor, 0);
        AssertDiagnostic(operations.Parse("  ").diagnostics, SemVerDiagnosticCodes.MissingMajor, 2);
        AssertDiagnostic(operations.Coerce("").diagnostics, SemVerDiagnosticCodes.NoCoercibleVersion, 0);
        AssertDiagnostic(operations.CoerceRightToLeft("  ").diagnostics, SemVerDiagnosticCodes.NoCoercibleVersion, 2);
        Assert.That(operations.ParseRange("").success, Is.True);
        Assert.That(operations.ValidRange("  "), Is.EqualTo("*"));
    }

    [TestCase("v1.2.3", "1.2.3")]
    [TestCase("V1.2.3", "1.2.3")]
    [TestCase("=1.2.3", "1.2.3")]
    [TestCase("= v1.2.3", "1.2.3")]
    [TestCase(" 1.2.3 ", "1.2.3")]
    [TestCase(" \t= v01.002.0003-alpha.01+build.0007 \r\n", "1.2.3-alpha.1+build.7")]
    public void LooseValidationAndParsing_AcceptNodeCompatibleInputs(string text, string normalized)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerCleanOptions loose = new NodeSemVerCleanOptions(true);

        NodeSemVerValidResult valid = operations.Valid(text, loose);
        NodeSemVerValidResult clean = operations.Clean(text, loose);
        VersionParseResult parsed = operations.Parse(text);

        Assert.That(valid.valid, Is.True, text);
        Assert.That(valid.versionText, Is.EqualTo(normalized));
        Assert.That(clean.valid, Is.True, text);
        Assert.That(clean.versionText, Is.EqualTo(normalized));
        Assert.That(parsed.success, Is.True, text);
        Assert.That(FormatNormalized(parsed.version!), Is.EqualTo(normalized));
    }

    [TestCase("1.2")]
    [TestCase("version 1.2.3")]
    [TestCase("1.2.3-alpha_1")]
    [TestCase("v1.2.3 trailing")]
    public void LooseValidationAndParsing_DoNotCoercePartialEmbeddedOrMalformedInputs(string text)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerCleanOptions loose = new NodeSemVerCleanOptions(true);

        Assert.That(operations.Valid(text, loose).valid, Is.False);
        Assert.That(operations.Clean(text, loose).valid, Is.False);
        Assert.That(operations.Parse(text).success, Is.False);
    }

    [Test]
    public void StrictCleaning_RebasesDiagnosticsToTheOriginalSource()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerCleanOptions strict = new NodeSemVerCleanOptions(false);
        string text = " \t= v1.2 ";

        NodeSemVerValidResult result = operations.Clean(text, strict);

        Assert.That(result.valid, Is.False);
        Assert.That(result.sourceText, Is.EqualTo(text));
        AssertDiagnostic(result.diagnostics, SemVerDiagnosticCodes.MissingPatch, 8);
        Assert.That(result.diagnostics[0].sourceText, Is.EqualTo(text));
    }

    [TestCase("1.2.3.4", "1.2.3", "2.3.4")]
    [TestCase("1.2.3/4", "1.2.3", "4.0.0")]
    [TestCase("1.2.3.4.5", "1.2.3", "3.4.5")]
    [TestCase("v3.4 replaces v3.3.1", "3.4.0", "3.3.1")]
    [TestCase("release 10 then 20.30", "10.0.0", "20.30.0")]
    public void CoercionDirection_SelectsLeftMostOrRightMostCompleteTuple(
        string text,
        string leftToRight,
        string rightToLeft)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        VersionParseResult leftResult = operations.Coerce(text);
        VersionParseResult rightResult = operations.CoerceRightToLeft(text);

        Assert.That(leftResult.success, Is.True);
        Assert.That(rightResult.success, Is.True);
        Assert.That(FormatNormalized(leftResult.version!), Is.EqualTo(leftToRight));
        Assert.That(FormatNormalized(rightResult.version!), Is.EqualTo(rightToLeft));
    }

    [Test]
    public void PrecedenceComparison_OrdersTheCompleteSpecificationChain()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] versions = new string[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0-beta",
            "1.0.0-beta.2",
            "1.0.0-beta.11",
            "1.0.0-rc.1",
            "1.0.0"
        };

        int leftIndex = 0;
        while (leftIndex < versions.Length)
        {
            int rightIndex = leftIndex + 1;
            while (rightIndex < versions.Length)
            {
                Assert.That(operations.Compare(versions[leftIndex], versions[rightIndex]), Is.LessThan(0));
                Assert.That(operations.Compare(versions[rightIndex], versions[leftIndex]), Is.GreaterThan(0));
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    [TestCase("=", true)]
    [TestCase("==", true)]
    [TestCase("===", true)]
    [TestCase("!=", false)]
    [TestCase("!==", false)]
    [TestCase(">", false)]
    [TestCase(">=", true)]
    [TestCase("<", false)]
    [TestCase("<=", true)]
    public void PrimitiveComparisonOperators_UsePrecedenceEquality(string operatorText, bool expected)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(
            operations.CompareByOperator("1.2.3+one", operatorText, "1.2.3+two"),
            Is.EqualTo(expected));
    }

    [Test]
    public void ComparisonHelpers_SeparateStrictLooseAndBuildAwareBehavior()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.Equal("1.2.3+one", "1.2.3+two"), Is.True);
        Assert.That(operations.NotEqual("1.2.3", "1.2.4"), Is.True);
        Assert.That(operations.GreaterThan("2.0.0", "1.9.9"), Is.True);
        Assert.That(operations.GreaterThanOrEqual("1.2.3", "1.2.3"), Is.True);
        Assert.That(operations.LessThan("1.2.3-alpha", "1.2.3"), Is.True);
        Assert.That(operations.LessThanOrEqual("1.2.3", "1.2.3"), Is.True);
        Assert.That(operations.ReverseCompare("1.2.3", "1.2.4"), Is.GreaterThan(0));
        Assert.That(operations.CompareLoose("= v01.002.0003", "v1.2.4"), Is.LessThan(0));
        Assert.That(operations.Compare("1.2.3+one", "1.2.3+two"), Is.Zero);
        Assert.That(operations.CompareBuild("1.2.3+one", "1.2.3+two"), Is.Not.Zero);
        Assert.That(() => operations.Compare("v1.2.3", "1.2.3"), Throws.ArgumentException);
        Assert.That(() => operations.CompareByOperator("1.2.3", "~", "1.2.3"), Throws.ArgumentException);
    }

    [TestCase("1.2.3", "1.2.3", null, NodeSemVerDiffKind.None)]
    [TestCase("1.2.3", "2.0.0", "major", NodeSemVerDiffKind.Major)]
    [TestCase("1.2.3", "1.3.0", "minor", NodeSemVerDiffKind.Minor)]
    [TestCase("1.2.3", "1.2.4", "patch", NodeSemVerDiffKind.Patch)]
    [TestCase("1.2.3-alpha.1", "1.2.3-alpha.2", "prerelease", NodeSemVerDiffKind.Prerelease)]
    [TestCase("1.2.3+one", "1.2.3+two", "build", NodeSemVerDiffKind.Build)]
    public void Diff_ClassifiesTheHighestOrderIdentityDifference(
        string leftText,
        string rightText,
        string? expectedText,
        NodeSemVerDiffKind expectedKind)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.Diff(leftText, rightText), Is.EqualTo(expectedText));
        Assert.That(operations.DiffKind(leftText, rightText), Is.EqualTo(expectedKind));
    }

    [TestCase(NodeSemVerIncrementReleaseType.Major, "1.2.3", "2.0.0")]
    [TestCase(NodeSemVerIncrementReleaseType.Minor, "1.2.3", "1.3.0")]
    [TestCase(NodeSemVerIncrementReleaseType.Patch, "1.2.3", "1.2.4")]
    [TestCase(NodeSemVerIncrementReleaseType.Premajor, "1.2.3", "2.0.0-0")]
    [TestCase(NodeSemVerIncrementReleaseType.Preminor, "1.2.3", "1.3.0-0")]
    [TestCase(NodeSemVerIncrementReleaseType.Prepatch, "1.2.3", "1.2.4-0")]
    [TestCase(NodeSemVerIncrementReleaseType.Prerelease, "1.2.3-alpha.9", "1.2.3-alpha.10")]
    [TestCase(NodeSemVerIncrementReleaseType.Release, "1.2.3-alpha.1", "1.2.3")]
    public void Increment_AllReleaseModesMatchNodeSemVerFixtures(
        NodeSemVerIncrementReleaseType releaseType,
        string text,
        string expected)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerIncrementOptions options = new NodeSemVerIncrementOptions(releaseType);

        Assert.That(operations.Increment(text, options), Is.EqualTo(expected));
    }

    [Test]
    public void Increment_IdentifierBaseLooseModeAndBuildRemovalMatchFixtures()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        NodeSemVerIncrementOptions betaOne = new NodeSemVerIncrementOptions(NodeSemVerIncrementReleaseType.Prerelease);
        betaOne.identifier = "beta";
        betaOne.identifierBase = 1;

        NodeSemVerIncrementOptions betaWithoutBase = new NodeSemVerIncrementOptions(NodeSemVerIncrementReleaseType.Preminor);
        betaWithoutBase.identifier = "beta";
        betaWithoutBase.identifierBaseEnabled = false;

        NodeSemVerIncrementOptions loosePatch = new NodeSemVerIncrementOptions(NodeSemVerIncrementReleaseType.Patch);
        loosePatch.loose = true;

        Assert.That(operations.Increment("1.2.3", betaOne), Is.EqualTo("1.2.4-beta.1"));
        Assert.That(operations.Increment("1.2.4-beta.1", betaOne), Is.EqualTo("1.2.4-beta.2"));
        Assert.That(operations.Increment("1.2.4-alpha.1", betaOne), Is.EqualTo("1.2.4-beta.1"));
        Assert.That(operations.Increment("1.2.3", betaWithoutBase), Is.EqualTo("1.3.0-beta"));
        Assert.That(operations.Increment("= v01.002.0003", loosePatch), Is.EqualTo("1.2.4"));
        Assert.That(operations.IncrementPatch("1.2.3+build.7"), Is.EqualTo("1.2.4"));
        Assert.That(operations.IncrementPrerelease("1.2.3-alpha+build.7"), Is.EqualTo("1.2.3-alpha.0"));
        Assert.That(operations.IncrementRelease("1.2.3"), Is.Null);
    }

    [TestCase(">=1.2.7", "1.2.7", true)]
    [TestCase(">=1.2.7", "1.2.6", false)]
    [TestCase("1.2.3 - 2.3.4", "2.3.4", true)]
    [TestCase("1.2.3 - 2.3.4", "2.3.5", false)]
    [TestCase("1.2.x", "1.2.99", true)]
    [TestCase("1.2.x", "1.3.0", false)]
    [TestCase("1", "1.9.9", true)]
    [TestCase("1", "2.0.0", false)]
    [TestCase("~1.2.3", "1.2.99", true)]
    [TestCase("~1.2.3", "1.3.0", false)]
    [TestCase("^1.2.3", "1.9.9", true)]
    [TestCase("^1.2.3", "2.0.0", false)]
    [TestCase("^0.2.3", "0.2.99", true)]
    [TestCase("^0.2.3", "0.3.0", false)]
    [TestCase("^0.0.3", "0.0.3", true)]
    [TestCase("^0.0.3", "0.0.4", false)]
    [TestCase("1.2.x || >=2.5.0 <3.0.0", "2.9.9", true)]
    [TestCase("1.2.x || >=2.5.0 <3.0.0", "3.0.0", false)]
    public void AdvancedRanges_MatchRepresentativeUpstreamFixtures(string rangeText, string versionText, bool expected)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.Satisfies(versionText, rangeText), Is.EqualTo(expected));
    }

    [Test]
    public void PrereleaseGates_RespectExplicitTuplesAndSelectionOptions()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        VersionSelectionOptions include = new VersionSelectionOptions();
        include.prereleasePolicy = PrereleasePolicy.Include;
        VersionSelectionOptions exclude = new VersionSelectionOptions();
        exclude.prereleasePolicy = PrereleasePolicy.Exclude;

        Assert.That(operations.Satisfies("1.2.3-alpha.7", ">1.2.3-alpha.3"), Is.True);
        Assert.That(operations.Satisfies("3.4.5-alpha.9", ">1.2.3-alpha.3"), Is.False);
        Assert.That(
            operations.SatisfiesWithOptions("3.4.5-alpha.9", ">1.2.3-alpha.3", include),
            Is.True);
        Assert.That(
            operations.SatisfiesWithOptions("1.2.3-alpha.7", ">1.2.3-alpha.3", exclude),
            Is.False);
    }

    [Test]
    public void RangeFormattingAndMinimumHelpers_ExposeExpandedComparators()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[][] sets = operations.ToComparators("^1.2.3 || ~2.0");

        Assert.That(sets, Has.Length.EqualTo(2));
        Assert.That(sets[0], Is.EqualTo(new string[] { ">=1.2.3", "<2.0.0-0" }));
        Assert.That(sets[1], Is.EqualTo(new string[] { ">=2.0.0", "<2.1.0-0" }));
        Assert.That(operations.ValidRange("1.2.x"), Is.EqualTo(">=1.2.0 <1.3.0-0"));
        Assert.That(operations.ValidRange(">=bad"), Is.Null);
        Assert.That(operations.MinVersion(">=1.2.3 <2.0.0"), Is.EqualTo("1.2.3"));
        Assert.That(operations.MinVersion(">1.2.3 <2.0.0"), Is.EqualTo("1.2.4"));
        Assert.That(operations.MinVersion(">1.2.3 <1.2.4"), Is.Null);
    }

    [TestCase("2.0.0", ">=1.0.0 <2.0.0", ">", true)]
    [TestCase("1.5.0", ">=1.0.0 <2.0.0", ">", false)]
    [TestCase("0.9.9", ">=1.0.0 <2.0.0", "<", true)]
    [TestCase("1.0.0", ">=1.0.0 <2.0.0", "<", false)]
    [TestCase("4.0.0", "<2.0.0 || >3.0.0", ">", false)]
    public void Outside_ClassifiesVersionsRelativeToCompleteRanges(
        string versionText,
        string rangeText,
        string direction,
        bool expected)
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.Outside(versionText, rangeText, direction), Is.EqualTo(expected));
    }

    [Test]
    public void OutsideAndRangeRelations_HandleBoundariesDisjunctionsAndInvalidInput()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(operations.GreaterThanRange("2.0.0", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(operations.LessThanRange("0.9.9", ">=1.0.0 <2.0.0"), Is.True);
        Assert.That(operations.Intersects("^1.2.3", ">=1.5.0 <2.0.0"), Is.True);
        Assert.That(operations.Intersects("^1.2.3", ">=2.0.0 <3.0.0"), Is.False);
        Assert.That(operations.Subset(">=1.2.3 <2.0.0", ">=1.0.0 <3.0.0"), Is.True);
        Assert.That(operations.Subset(">=1.0.0 <3.0.0", ">=1.2.3 <2.0.0"), Is.False);
        Assert.That(operations.Intersects(null, "*"), Is.False);
        Assert.That(operations.Subset(">=bad", "*"), Is.False);
        Assert.That(() => operations.Outside("1.2.3", "*", "~"), Throws.ArgumentException);
    }

    [Test]
    public void SimplifyRange_UsesFiniteCandidateMembershipAndRejectsInvalidCandidates()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        Assert.That(
            operations.SimplifyRange(
                "1.2.x",
                new string[] { "1.0.0", "1.2.0", "1.2.1", "2.0.0" }),
            Is.EqualTo("=1.2.0 || =1.2.1"));
        Assert.That(
            operations.SimplifyRange("1.2.x", new string[] { "1.0.0", "2.0.0" }),
            Is.EqualTo(""));
        Assert.That(operations.SimplifyRange("1.2.x", new string[] { "bad" }), Is.Null);
        Assert.That(operations.SimplifyRange(">=bad", new string[] { "1.2.3" }), Is.Null);
    }

    [Test]
    public void Sorting_OrdersPrecedenceFixturesAndPreservesEqualPrecedenceInputOrder()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] input = new string[]
        {
            "1.0.0",
            "1.0.0+z",
            "1.0.0-alpha.beta",
            "1.0.0-alpha",
            "2.0.0",
            "1.0.0+a",
            "1.0.0-alpha.1"
        };
        string[] expected = new string[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0",
            "1.0.0+z",
            "1.0.0+a",
            "2.0.0"
        };

        string[] ascending = operations.Sort(input);
        string[] descending = operations.ReverseSort(input);

        Assert.That(ascending, Is.EqualTo(expected));
        Assert.That(descending, Is.EqualTo(new string[]
        {
            "2.0.0",
            "1.0.0",
            "1.0.0+z",
            "1.0.0+a",
            "1.0.0-alpha.beta",
            "1.0.0-alpha.1",
            "1.0.0-alpha"
        }));
        Assert.That(input[0], Is.EqualTo("1.0.0"));
        Assert.That(() => operations.Sort(new string[] { "1.0.0", "v1.2.3" }), Throws.ArgumentException);
        Assert.That(() => operations.ReverseSort(new string[] { "1.0.0", null! }), Throws.ArgumentException);
    }

    [Test]
    public void MinAndMaxSatisfying_SelectFiniteCandidatesAndIgnoreInvalidEntries()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] candidates = new string[]
        {
            "bad",
            "2.0.0",
            "1.9.0",
            "1.2.3+build.7",
            "1.5.0",
            null!
        };

        Assert.That(operations.MinSatisfying(candidates, "^1.2.0"), Is.EqualTo("1.2.3+build.7"));
        Assert.That(operations.MaxSatisfying(candidates, "^1.2.0"), Is.EqualTo("1.9.0"));
        Assert.That(operations.MinSatisfying(candidates, ">=3.0.0"), Is.Null);
        Assert.That(operations.MaxSatisfying(candidates, ">=bad"), Is.Null);
        Assert.That(operations.MinSatisfying(new string[0], "*"), Is.Null);
    }

    [Test]
    public void MinAndMaxSatisfying_ApplyPrereleasePoliciesAndExplicitPrereleaseRanges()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();
        string[] prereleases = new string[]
        {
            "1.5.0-beta.1",
            "1.5.0-alpha.1"
        };
        VersionSelectionOptions include = new VersionSelectionOptions();
        include.prereleasePolicy = PrereleasePolicy.Include;
        VersionSelectionOptions exclude = new VersionSelectionOptions();
        exclude.prereleasePolicy = PrereleasePolicy.Exclude;

        Assert.That(operations.MinSatisfying(prereleases, ">=1.0.0"), Is.Null);
        Assert.That(
            operations.MinSatisfyingWithOptions(prereleases, ">=1.0.0", include),
            Is.EqualTo("1.5.0-alpha.1"));
        Assert.That(
            operations.MaxSatisfyingWithOptions(prereleases, ">=1.0.0", include),
            Is.EqualTo("1.5.0-beta.1"));
        Assert.That(
            operations.MaxSatisfyingWithOptions(prereleases, ">=1.0.0", exclude),
            Is.Null);
        Assert.That(
            operations.MaxSatisfying(
                new string[] { "1.2.3-alpha.7", "1.2.3-alpha.4" },
                ">1.2.3-alpha.3 <1.2.3"),
            Is.EqualTo("1.2.3-alpha.7"));
    }

    static string CreateBuildVersion(int totalLength)
    {
        string prefix = "1.2.3+";
        return prefix + new string('a', totalLength - prefix.Length);
    }

    static string CreateComparator(int totalLength)
    {
        string prefix = ">=1.2.3+";
        return prefix + new string('a', totalLength - prefix.Length);
    }

    static string FormatNormalized(UniversalVersion version)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < version.numericComponents.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(NormalizeNumeric(version.numericComponents[index]));
            index = index + 1;
        }

        if (version.prereleaseIdentifiers.Length > 0)
        {
            builder.Append('-');
            AppendIdentifiers(builder, version.prereleaseIdentifiers);
        }

        if (version.buildIdentifiers.Length > 0)
        {
            builder.Append('+');
            AppendIdentifiers(builder, version.buildIdentifiers);
        }

        return builder.ToString();
    }

    static void AppendIdentifiers(
        System.Text.StringBuilder builder,
        VersionIdentifier[] identifiers)
    {
        int index = 0;
        while (index < identifiers.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            if (identifiers[index].numeric)
            {
                builder.Append(NormalizeNumeric(identifiers[index].text));
            }
            else
            {
                builder.Append(identifiers[index].text);
            }

            index = index + 1;
        }
    }

    static string NormalizeNumeric(string text)
    {
        int index = 0;
        while (index < text.Length - 1 && text[index] == '0')
        {
            index = index + 1;
        }

        return text.Substring(index);
    }

    static void AssertDiagnostic(
        VersionDiagnostic[] diagnostics,
        string expectedCode,
        int expectedIndex)
    {
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].code, Is.EqualTo(expectedCode));
        Assert.That(diagnostics[0].index, Is.EqualTo(expectedIndex));
    }
}
