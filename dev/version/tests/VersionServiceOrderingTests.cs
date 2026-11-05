using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionServiceOrderingTests
{
    [TestCase(VersionDialect.SemVer, "1.0.0", "2.0.0")]
    [TestCase(VersionDialect.NodeSemVer, "v1.0.0", "v2.0.0")]
    [TestCase(VersionDialect.NuGet, "1.0", "2.0")]
    [TestCase(VersionDialect.Maven, "1.0", "2.0")]
    [TestCase(VersionDialect.Go, "v1.0.0", "v2.0.0")]
    [TestCase(VersionDialect.Cargo, "1.0.0", "2.0.0")]
    [TestCase(VersionDialect.RubyGems, "1.0", "2.0")]
    [TestCase(VersionDialect.Python, "1.0", "2.0")]
    public void EveryDialect_SortsAndSelectsParsedAndTextualCandidates(
        VersionDialect dialect,
        string lowerText,
        string upperText)
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = OptionsForDialect(
            dialect,
            VersionComparisonMode.Dialect,
            true,
            false,
            InvalidVersionOrdering.Reject);
        UniversalVersion lower = ParseForDialect(service, lowerText, dialect);
        UniversalVersion upper = ParseForDialect(service, upperText, dialect);
        UniversalVersion[] versions = new UniversalVersion[] { upper, lower };
        string[] texts = new string[] { upperText, lowerText };

        Assert.That(comparison.SortVersionsAscending(versions, options), Is.EqualTo(new UniversalVersion[] { lower, upper }));
        Assert.That(comparison.SortVersionsDescending(versions, options), Is.EqualTo(new UniversalVersion[] { upper, lower }));
        Assert.That(selection.MinimumVersion(versions, options), Is.SameAs(lower));
        Assert.That(selection.MaximumVersion(versions, options), Is.SameAs(upper));
        Assert.That(comparison.SortTextsAscending(texts, options), Is.EqualTo(new string[] { lowerText, upperText }));
        Assert.That(comparison.SortTextsDescending(texts, options), Is.EqualTo(new string[] { upperText, lowerText }));
        Assert.That(selection.MinimumText(texts, options), Is.EqualTo(lowerText));
        Assert.That(selection.MaximumText(texts, options), Is.EqualTo(upperText));
    }

    [Test]
    public void ParsedCandidates_SortAndSelectExtremaWithoutChangingInput()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(VersionComparisonMode.Precedence, true, false, InvalidVersionOrdering.Reject);
        UniversalVersion one = Parse(service, "1.0.0");
        UniversalVersion oneFive = Parse(service, "1.5.0");
        UniversalVersion two = Parse(service, "2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[] { two, one, oneFive };

        UniversalVersion[] ascending = comparison.SortVersionsAscending(candidates, options);
        UniversalVersion[] descending = comparison.SortVersionsDescending(candidates, options);

        Assert.That(ascending, Is.EqualTo(new UniversalVersion[] { one, oneFive, two }));
        Assert.That(descending, Is.EqualTo(new UniversalVersion[] { two, oneFive, one }));
        Assert.That(selection.MinimumVersion(candidates, options), Is.SameAs(one));
        Assert.That(selection.MaximumVersion(candidates, options), Is.SameAs(two));
        Assert.That(candidates, Is.EqualTo(new UniversalVersion[] { two, one, oneFive }));
        Assert.That(ascending, Is.Not.SameAs(candidates));
        Assert.That(descending, Is.Not.SameAs(candidates));
    }

    [Test]
    public void TextCandidates_SortAndSelectExtremaUsingIdentityOptions()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(VersionComparisonMode.Identity, true, true, InvalidVersionOrdering.Reject);
        string[] candidates = new string[] { "2.0.0", "1.0.0+build.2", "1.0.0+build.1" };

        string[] ascending = comparison.SortTextsAscending(candidates, options);
        string[] descending = comparison.SortTextsDescending(candidates, options);

        Assert.That(ascending, Is.EqualTo(new string[] { "1.0.0+build.1", "1.0.0+build.2", "2.0.0" }));
        Assert.That(descending, Is.EqualTo(new string[] { "2.0.0", "1.0.0+build.2", "1.0.0+build.1" }));
        Assert.That(selection.MinimumText(candidates, options), Is.EqualTo("1.0.0+build.1"));
        Assert.That(selection.MaximumText(candidates, options), Is.EqualTo("2.0.0"));
        Assert.That(candidates, Is.EqualTo(new string[] { "2.0.0", "1.0.0+build.2", "1.0.0+build.1" }));
    }

    [Test]
    public void PrecedencePreservesBuildTiesWhileIdentityOrdersThem()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions precedence = Options(VersionComparisonMode.Precedence, true, false, InvalidVersionOrdering.Reject);
        VersionComparisonOptions identity = Options(VersionComparisonMode.Identity, true, false, InvalidVersionOrdering.Reject);
        UniversalVersion buildTwo = Parse(service, "1.0.0+build.2");
        UniversalVersion buildOne = Parse(service, "1.0.0+build.1");
        UniversalVersion[] versions = new UniversalVersion[] { buildTwo, buildOne };
        string[] texts = new string[] { "1.0.0+build.2", "1.0.0+build.1" };

        Assert.That(comparison.SortVersionsAscending(versions, precedence), Is.EqualTo(versions));
        Assert.That(comparison.SortVersionsDescending(versions, precedence), Is.EqualTo(versions));
        Assert.That(selection.MinimumVersion(versions, precedence), Is.SameAs(buildTwo));
        Assert.That(selection.MaximumVersion(versions, precedence), Is.SameAs(buildTwo));
        Assert.That(comparison.SortVersionsAscending(versions, identity), Is.EqualTo(new UniversalVersion[] { buildOne, buildTwo }));
        Assert.That(comparison.SortVersionsDescending(versions, identity), Is.EqualTo(new UniversalVersion[] { buildTwo, buildOne }));
        Assert.That(selection.MinimumVersion(versions, identity), Is.SameAs(buildOne));
        Assert.That(selection.MaximumVersion(versions, identity), Is.SameAs(buildTwo));
        Assert.That(comparison.SortTextsAscending(texts, precedence), Is.EqualTo(texts));
        Assert.That(comparison.SortTextsAscending(texts, identity), Is.EqualTo(new string[] { "1.0.0+build.1", "1.0.0+build.2" }));
    }

    [Test]
    public void PrecedenceOrdersPrereleasesBeforeTheAssociatedStableRelease()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(VersionComparisonMode.Precedence, true, false, InvalidVersionOrdering.Reject);
        string[] candidates = new string[] { "1.0.0", "1.0.0-beta", "0.9.0", "1.0.0-alpha" };

        Assert.That(
            comparison.SortTextsAscending(candidates, options),
            Is.EqualTo(new string[] { "0.9.0", "1.0.0-alpha", "1.0.0-beta", "1.0.0" }));
        Assert.That(
            comparison.SortTextsDescending(candidates, options),
            Is.EqualTo(new string[] { "1.0.0", "1.0.0-beta", "1.0.0-alpha", "0.9.0" }));
        Assert.That(selection.MinimumText(candidates, options), Is.EqualTo("0.9.0"));
        Assert.That(selection.MaximumText(candidates, options), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void StabilityAwareOrdering_PrefersStableCandidatesAcrossNumericCores()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(VersionComparisonMode.StabilityAware, true, false, InvalidVersionOrdering.Reject);
        string[] candidates = new string[] { "3.0.0-alpha", "1.0.0", "2.0.0-beta", "0.5.0" };

        string[] ascending = comparison.SortTextsAscending(candidates, options);
        string[] descending = comparison.SortTextsDescending(candidates, options);

        Assert.That(ascending, Is.EqualTo(new string[] { "2.0.0-beta", "3.0.0-alpha", "0.5.0", "1.0.0" }));
        Assert.That(descending, Is.EqualTo(new string[] { "1.0.0", "0.5.0", "3.0.0-alpha", "2.0.0-beta" }));
        Assert.That(selection.MinimumText(candidates, options), Is.EqualTo("2.0.0-beta"));
        Assert.That(selection.MaximumText(candidates, options), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void CasePolicy_ControlsSemVerPrereleaseTextOrdering()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        VersionComparisonOptions sensitive = Options(VersionComparisonMode.Precedence, true, false, InvalidVersionOrdering.Reject);
        VersionComparisonOptions insensitive = Options(VersionComparisonMode.Precedence, false, false, InvalidVersionOrdering.Reject);
        string[] candidates = new string[] { "1.0.0-alpha", "1.0.0-BETA" };

        string[] sensitiveOrder = comparison.SortTextsAscending(candidates, sensitive);
        string[] insensitiveOrder = comparison.SortTextsAscending(candidates, insensitive);

        Assert.That(sensitiveOrder, Is.EqualTo(new string[] { "1.0.0-BETA", "1.0.0-alpha" }));
        Assert.That(insensitiveOrder, Is.EqualTo(new string[] { "1.0.0-alpha", "1.0.0-BETA" }));
        Assert.That(comparison.Compare("1.0.0-ALPHA", "1.0.0-alpha", insensitive), Is.EqualTo(0));
    }

    [Test]
    public void TextOrdering_HonorsExplicitInvalidVersionPlacement()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(
            VersionComparisonMode.Precedence,
            true,
            false,
            InvalidVersionOrdering.SortBeforeValid);
        string[] candidates = new string[] { "2.0.0", "not-a-version", "1.0.0" };

        Assert.That(
            comparison.SortTextsAscending(candidates, options),
            Is.EqualTo(new string[] { "not-a-version", "1.0.0", "2.0.0" }));
        Assert.That(selection.MinimumText(candidates, options), Is.EqualTo("not-a-version"));
        Assert.That(selection.MaximumText(candidates, options), Is.EqualTo("2.0.0"));
    }

    [Test]
    public void InvalidOrderingPolicies_HandleAfterEqualAndRejectBehavior()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        string[] candidates = new string[] { "2.0.0", "not-a-version", "1.0.0" };
        VersionComparisonOptions after = Options(
            VersionComparisonMode.Precedence,
            true,
            false,
            InvalidVersionOrdering.SortAfterValid);
        VersionComparisonOptions equal = Options(
            VersionComparisonMode.Precedence,
            true,
            false,
            InvalidVersionOrdering.TreatAsEqual);
        VersionComparisonOptions reject = Options(
            VersionComparisonMode.Precedence,
            true,
            false,
            InvalidVersionOrdering.Reject);

        Assert.That(
            comparison.SortTextsAscending(candidates, after),
            Is.EqualTo(new string[] { "1.0.0", "2.0.0", "not-a-version" }));
        Assert.That(selection.MinimumText(candidates, after), Is.EqualTo("1.0.0"));
        Assert.That(selection.MaximumText(candidates, after), Is.EqualTo("not-a-version"));
        Assert.That(comparison.SortTextsAscending(candidates, equal), Is.EqualTo(candidates));
        Assert.That(selection.MinimumText(candidates, equal), Is.EqualTo("1.0.0"));
        Assert.That(selection.MaximumText(candidates, equal), Is.EqualTo("2.0.0"));
        Assert.That(
            delegate
            {
                comparison.SortTextsAscending(new string[] { "not-a-version" }, reject);
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                selection.MaximumText(new string[] { "not-a-version" }, reject);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void UnboundedNumericComponents_SortAndSelectWithoutIntegerConversion()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(VersionComparisonMode.Precedence, true, false, InvalidVersionOrdering.Reject);
        string lowerText = new string('9', 700) + ".0.0";
        string upperText = "1" + new string('0', 700) + ".0.0";
        UniversalVersion lower = Parse(service, lowerText);
        UniversalVersion upper = Parse(service, upperText);
        UniversalVersion[] versions = new UniversalVersion[] { upper, lower };
        string[] texts = new string[] { upperText, lowerText };

        Assert.That(comparison.SortVersionsAscending(versions, options), Is.EqualTo(new UniversalVersion[] { lower, upper }));
        Assert.That(comparison.SortTextsDescending(texts, options), Is.EqualTo(new string[] { upperText, lowerText }));
        Assert.That(selection.MinimumVersion(versions, options), Is.SameAs(lower));
        Assert.That(selection.MaximumVersion(versions, options), Is.SameAs(upper));
        Assert.That(selection.MinimumText(texts, options), Is.EqualTo(lowerText));
        Assert.That(selection.MaximumText(texts, options), Is.EqualTo(upperText));
    }

    [Test]
    public void EmptyCandidateArrays_ReturnCopiesAndNoExtrema()
    {
        VersionService service = new VersionService();
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        VersionComparisonOptions options = Options(VersionComparisonMode.Precedence, true, false, InvalidVersionOrdering.Reject);
        UniversalVersion[] versions = new UniversalVersion[0];
        string[] texts = new string[0];

        Assert.That(comparison.SortVersionsAscending(versions, options), Is.Empty);
        Assert.That(comparison.SortTextsDescending(texts, options), Is.Empty);
        Assert.That(selection.MinimumVersion(versions, options), Is.Null);
        Assert.That(selection.MaximumVersion(versions, options), Is.Null);
        Assert.That(selection.MinimumText(texts, options), Is.Null);
        Assert.That(selection.MaximumText(texts, options), Is.Null);
    }

    static UniversalVersion Parse(IVersionParsing parsing, string text)
    {
        VersionParseOptions options = new VersionParseOptions(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            4096,
            true,
            true,
            PrereleasePolicy.Default);

        return parsing.Parse(text, options);
    }

    static UniversalVersion ParseForDialect(IVersionParsing parsing, string text, VersionDialect dialect)
    {
        VersionParseOptions options = new VersionParseOptions(
            dialect,
            VersionParseMode.DialectSpecific,
            4096,
            true,
            true,
            PrereleasePolicy.Default);

        return parsing.Parse(text, options);
    }

    static VersionComparisonOptions Options(
        VersionComparisonMode comparisonMode,
        bool caseSensitive,
        bool includeBuildMetadata,
        InvalidVersionOrdering invalidVersionOrdering)
    {
        return new VersionComparisonOptions(
            VersionDialect.SemVer,
            comparisonMode,
            caseSensitive,
            includeBuildMetadata,
            invalidVersionOrdering);
    }

    static VersionComparisonOptions OptionsForDialect(
        VersionDialect dialect,
        VersionComparisonMode comparisonMode,
        bool caseSensitive,
        bool includeBuildMetadata,
        InvalidVersionOrdering invalidVersionOrdering)
    {
        return new VersionComparisonOptions(
            dialect,
            comparisonMode,
            caseSensitive,
            includeBuildMetadata,
            invalidVersionOrdering);
    }
}
