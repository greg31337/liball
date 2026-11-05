namespace version.Tests;

[TestFixture]
public class PythonOperationsTests
{
    [Test]
    public void VersionOperations_ParseNormalizeFormatAndComparePep440Versions()
    {
        IPythonOperations operations = new PythonOperations();
        string original = "v0002!01.0PREVIEW-0004.post_0005.dev-0006+LOCAL_0007";
        VersionParseResult result = operations.ParseVersion(original);

        Assert.That(result.success, Is.True);
        Assert.That(result.dialect, Is.EqualTo(VersionDialect.Python));
        Assert.That(result.version, Is.Not.Null);
        Assert.That(
            operations.NormalizeVersion(original),
            Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
        Assert.That(
            operations.FormatCanonicalVersion(result.version!),
            Is.EqualTo("2!1.0rc4.post5.dev6+local.7"));
        Assert.That(operations.FormatDisplayVersion(result.version!), Is.EqualTo(original));
        Assert.That(operations.CompareVersions("1.0.dev1", "1.0a1"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0rc1", "1.0"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1!1.0", "2.0"), Is.GreaterThan(0));
        Assert.That(operations.CompareVersions("1.0", "1.0.0"), Is.EqualTo(0));
    }

    [Test]
    public void SpecifierOperations_MatchWildcardsCompatibleReleasesAndExclusions()
    {
        IPythonOperations operations = new PythonOperations();
        VersionSelectionOptions options = new VersionSelectionOptions();
        VersionRange wildcard = RequireRange(operations, "==1.4.*");
        VersionRange compatible = RequireRange(operations, "~=1.4.5, !=1.4.7");

        Assert.That(operations.SpecifierAllowsVersion(wildcard, "1.4.99", options), Is.True);
        Assert.That(operations.SpecifierAllowsVersion(wildcard, "1.5.0", options), Is.False);
        Assert.That(operations.SpecifierAllowsVersion(compatible, "1.4.5", options), Is.True);
        Assert.That(operations.SpecifierAllowsVersion(compatible, "1.4.7", options), Is.False);
        Assert.That(operations.SpecifierAllowsVersion(compatible, "1.4.99", options), Is.True);
        Assert.That(operations.SpecifierAllowsVersion(compatible, "1.5.0", options), Is.False);
    }

    [Test]
    public void CandidateOperations_ApplyExplicitAndFallbackPrereleasePolicies()
    {
        IPythonOperations operations = new PythonOperations();
        VersionRange range = RequireRange(operations, ">=1.0, <2.0");
        string[] candidates = new string[] { "bad", "1.5a1", "1.4", "2.0" };
        VersionSelectionOptions defaults = new VersionSelectionOptions();
        VersionSelectionOptions include = new VersionSelectionOptions();
        include.prereleasePolicy = PrereleasePolicy.Include;
        VersionSelectionOptions fallback = new VersionSelectionOptions();
        fallback.prereleasePolicy = PrereleasePolicy.AutoIfNoStableCandidate;

        Assert.That(
            operations.FilterCandidates(range, candidates, defaults),
            Is.EqualTo(new string[] { "1.4" }));
        Assert.That(
            operations.FilterCandidates(range, candidates, include),
            Is.EqualTo(new string[] { "1.5a1", "1.4" }));
        Assert.That(
            operations.FilterCandidates(range, candidates, fallback),
            Is.EqualTo(new string[] { "1.4" }));
        Assert.That(
            operations.FilterCandidates(
                range,
                new string[] { "1.5a1", "2.0a1" },
                fallback),
            Is.EqualTo(new string[] { "1.5a1", "2.0a1" }));
        Assert.That(operations.SelectHighestCandidate(range, candidates, include), Is.EqualTo("1.5a1"));
        Assert.That(
            operations.SelectHighestCandidate(
                range,
                new string[] { "bad", "2.0" },
                defaults),
            Is.Null);
    }

    [Test]
    public void RangeRelations_CompareParsedPythonSpecifierSets()
    {
        IPythonOperations operations = new PythonOperations();
        VersionRange broad = RequireRange(operations, ">=1.0, <2.0");
        VersionRange narrow = RequireRange(operations, ">=1.4, <1.5");
        VersionRange outside = RequireRange(operations, ">=2.0, <3.0");

        Assert.That(operations.IsSpecifierSubset(narrow, broad), Is.True);
        Assert.That(operations.IsSpecifierSubset(broad, narrow), Is.False);
        Assert.That(operations.IsSpecifierSuperset(broad, narrow), Is.True);
        Assert.That(operations.AreSpecifiersDisjoint(broad, outside), Is.True);
        Assert.That(operations.AreSpecifiersDisjoint(broad, narrow), Is.False);
    }

    [Test]
    public void ParseOperations_ReturnDiagnosticsForMalformedInput()
    {
        IPythonOperations operations = new PythonOperations();

        VersionParseResult version = operations.ParseVersion("1..0");
        VersionRangeParseResult specifier = operations.ParseSpecifier("~=1");

        Assert.That(version.success, Is.False);
        Assert.That(version.version, Is.Null);
        Assert.That(version.diagnostics, Has.Length.EqualTo(1));
        Assert.That(specifier.success, Is.False);
        Assert.That(specifier.range, Is.Null);
        Assert.That(specifier.syntax, Is.EqualTo(RangeSyntax.Python));
        Assert.That(specifier.diagnostics, Has.Length.EqualTo(1));
        Assert.That(
            delegate
            {
                operations.NormalizeVersion("bad");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static VersionRange RequireRange(IPythonOperations operations, string text)
    {
        VersionRangeParseResult result = operations.ParseSpecifier(text);
        Assert.That(result.success, Is.True, text);
        Assert.That(result.range, Is.Not.Null, text);
        return result.range!;
    }
}
