using NUnit.Framework;
using version;

namespace version.ConsumerTests;

[TestFixture]
public class PublicConsumerReachabilityTests
{
    [Test]
    public void SemVerService_IsReachableThroughItsPublicContract()
    {
        ISemVerOperations operations = new SemVerOperations();

        UniversalVersion version = operations.ParseStrict("1.2.3-alpha+build");
        VersionParseResult failure = operations.TryParseStrict("1.2");

        Assert.That(operations.FormatCanonical(version), Is.EqualTo("1.2.3-alpha+build"));
        Assert.That(operations.ComparePrecedence("1.2.3", "2.0.0"), Is.LessThan(0));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.version, Is.Null);
        Assert.That(failure.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.ParseStrict("1.2"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void NodeService_IsReachableThroughItsPublicContract()
    {
        INodeSemVerOperations operations = new NodeSemVerOperations();

        VersionParseResult parsed = operations.Parse("v1.2.3");
        VersionRangeParseResult range = operations.ParseRange("^1.0.0");
        VersionParseResult failure = operations.Parse("invalid");

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.version, Is.Not.Null);
        Assert.That(range.success, Is.True);
        Assert.That(operations.Satisfies("1.5.0", "^1.0.0"), Is.True);
        Assert.That(operations.MaxSatisfying(new string[] { "1.0.0", "1.5.0", "2.0.0" }, "^1.0.0"), Is.EqualTo("1.5.0"));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
    }

    [Test]
    public void NuGetService_IsReachableThroughItsPublicContract()
    {
        INuGetResolution operations = new NuGetOperations();

        VersionParseResult parsed = operations.ParseVersion("1.2.3.4");
        VersionRangeParseResult range = operations.ParseRange("[1.0,2.0)");
        VersionParseResult failure = operations.ParseVersion("invalid version");

        Assert.That(parsed.success, Is.True);
        Assert.That(operations.NormalizeVersion("1.2.3.0"), Is.EqualTo("1.2.3"));
        Assert.That(range.success, Is.True);
        Assert.That(operations.CompareVersions("1.0.0", "2.0.0"), Is.LessThan(0));
        Assert.That(operations.ParseTargetFramework("net8.0").success, Is.True);
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
        Assert.That(operations.NormalizeVersion("invalid version"), Is.Empty);
    }

    [Test]
    public void MavenService_IsReachableThroughItsPublicContract()
    {
        IMavenResolution operations = new MavenOperations();

        VersionParseResult parsed = operations.ParseVersion("1.0-alpha-1");
        VersionRangeParseResult range = operations.ParseRange("[1.0,2.0)");
        VersionParseResult failure = operations.ParseVersion("");

        Assert.That(parsed.success, Is.True);
        Assert.That(operations.NormalizeVersion("1.0-ga"), Is.EqualTo("1"));
        Assert.That(range.success, Is.True);
        Assert.That(operations.CompareVersions("1.0", "2.0"), Is.LessThan(0));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.NormalizeVersion(""), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void GoService_IsReachableThroughItsPublicContract()
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        VersionParseResult parsed = operations.ParseVersion("v1.2.3");
        GoModulePathParseResult path = operations.ParseModulePath("example.com/mod/v2");
        GoSemanticImportVersionResult semantic = operations.ValidateSemanticImportVersion(
            "example.com/mod/v2",
            "v2.1.0");
        VersionParseResult failure = operations.ParseVersion("1.2.3");

        Assert.That(parsed.success, Is.True);
        Assert.That(operations.CanonicalizeVersion("v1.2"), Is.EqualTo("v1.2.0"));
        Assert.That(path.success, Is.True);
        Assert.That(semantic.success, Is.True);
        Assert.That(operations.SelectHighestSemanticImportVersion(
            "example.com/mod/v2",
            new string[] { "v2.0.0", "v2.1.0", "v3.0.0" }), Is.EqualTo("v2.1.0"));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
    }

    [Test]
    public void CargoService_IsReachableThroughItsPublicContract()
    {
        ICargoResolution operations = new CargoOperations();

        VersionParseResult parsed = operations.ParseVersion("1.2.3");
        VersionRangeParseResult requirement = operations.ParseRequirement("^1.2");
        CargoManifest manifest = operations.ParseManifest("[dependencies]\nserde = \"1\"");
        VersionParseResult failure = operations.ParseVersion("1.2");

        Assert.That(parsed.success, Is.True);
        Assert.That(requirement.success, Is.True);
        Assert.That(manifest.dependencies, Has.Length.EqualTo(1));
        Assert.That(operations.CompareVersions("1.0.0", "2.0.0"), Is.LessThan(0));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.ParseManifest("[dependencies\n"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void RubyGemsService_IsReachableThroughItsPublicContract()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();

        VersionParseResult parsed = operations.ParseVersion("1.2.3.pre");
        VersionRangeParseResult requirement = operations.ParseRequirement("~> 1.2");
        VersionParseResult failure = operations.ParseVersion("bad version!");

        Assert.That(parsed.success, Is.True);
        Assert.That(operations.NormalizeVersion("1.2.0"), Is.EqualTo("1.2.0"));
        Assert.That(operations.IsPrereleaseVersion("1.2.3.pre"), Is.True);
        Assert.That(requirement.success, Is.True);
        Assert.That(operations.CreateDefaultRequirement().requirementTexts, Is.Not.Empty);
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.NormalizeVersion("bad version!"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void PythonService_IsReachableThroughItsPublicContract()
    {
        IPythonOperations operations = new PythonOperations();

        VersionParseResult parsed = operations.ParseVersion("1!2.0rc1.post2");
        VersionRangeParseResult specifier = operations.ParseSpecifier(">=2.0,<3");
        VersionParseResult failure = operations.ParseVersion("bad version!");

        Assert.That(parsed.success, Is.True);
        Assert.That(operations.NormalizeVersion("V1.0RC1"), Is.EqualTo("1.0rc1"));
        Assert.That(specifier.success, Is.True);
        Assert.That(
            operations.SpecifierAllowsVersion(specifier.range!, "2.5", new VersionSelectionOptions()),
            Is.True);
        Assert.That(
            operations.SelectHighestCandidate(
                specifier.range!,
                new string[] { "2.0", "2.5", "3.0" },
                new VersionSelectionOptions()),
            Is.EqualTo("2.5"));
        Assert.That(failure.success, Is.False);
        Assert.That(failure.diagnostics, Is.Not.Empty);
        Assert.That(() => operations.NormalizeVersion("bad version!"), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void SharedVersionService_ExposesAllFocusedPublicContracts()
    {
        VersionService service = new VersionService();
        IVersionParsing parsing = service;
        IVersionRangeParsing rangeParsing = service;
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        IVersionFormatting formatting = service;
        IVersionInspection inspection = service;
        VersionParseOptions parseOptions = SemVerParseOptions();
        VersionRangeParseOptions rangeOptions = MavenRangeOptions();
        VersionComparisonOptions comparisonOptions = SemVerComparisonOptions();
        VersionSelectionOptions selectionOptions = new VersionSelectionOptions();

        UniversalVersion lower = parsing.Parse("1.2.3", parseOptions);
        UniversalVersion higher = parsing.Parse("1.5.0", parseOptions);
        VersionParseResult parseFailure = parsing.TryParse("invalid", parseOptions);
        VersionRange range = rangeParsing.ParseRange("[1.0,2.0)", rangeOptions);
        VersionRangeParseResult rangeFailure = rangeParsing.TryParseRange("[invalid", rangeOptions);

        Assert.That(comparison.CompareVersions(lower, higher, comparisonOptions), Is.LessThan(0));
        Assert.That(comparison.SortTextsDescending(new string[] { "1.0.0", "2.0.0" }, comparisonOptions), Is.EqualTo(new string[] { "2.0.0", "1.0.0" }));
        Assert.That(selection.Filter(range, new UniversalVersion[] { lower, higher }, selectionOptions), Has.Length.EqualTo(2));
        Assert.That(selection.MaxSatisfying(range, new UniversalVersion[] { lower, higher }, selectionOptions), Is.SameAs(higher));
        Assert.That(formatting.FormatCanonical(higher), Is.EqualTo("1.5.0"));
        Assert.That(inspection.GetDialect(higher), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(inspection.IsStable(higher), Is.True);
        Assert.That(parseFailure.success, Is.False);
        Assert.That(rangeFailure.success, Is.False);
        Assert.That(selection.MaximumVersion(new UniversalVersion[0], comparisonOptions), Is.Null);
        Assert.That(() => comparison.Compare("invalid", "1.0.0", comparisonOptions), Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => formatting.FormatCanonical(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => inspection.GetPythonVersion(higher), Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void RangeService_IsReachableThroughItsPublicContract()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        IVersionParsing parsing = new VersionService();
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange universal = operations.CreateUniversalRange();
        UniversalVersion version = parsing.Parse("1.2.3", SemVerParseOptions());

        Assert.That(operations.IsEmpty(empty), Is.True);
        Assert.That(operations.IsUniversal(universal), Is.True);
        Assert.That(operations.ContainsVersion(universal, version), Is.True);
        Assert.That(operations.IsDisjoint(empty, universal), Is.True);
        Assert.That(operations.FormatNormalizedRange(universal, RangeSyntax.NodeSemVer), Is.EqualTo("*"));
        Assert.That(() => operations.ContainsVersion(null!, version), Throws.TypeOf<System.ArgumentNullException>());
    }

    [Test]
    public void SyntaxInspectionService_IsReachableThroughItsPublicContract()
    {
        IVersionSyntaxInspection operations = new VersionSyntaxInspection();
        VersionInterval interval = new VersionInterval(
            BoundKind.Unbounded,
            null,
            BoundKind.Unbounded,
            null,
            VersionComparisonMode.Precedence);

        VersionToken[] tokens = operations.Tokenize("1.2.3-alpha");
        VersionComparatorSet comparatorSet = operations.ConvertInterval(interval);

        Assert.That(tokens, Is.Not.Empty);
        Assert.That(tokens[tokens.Length - 1].kind, Is.EqualTo(VersionTokenKind.End));
        Assert.That(comparatorSet.comparators, Is.Empty);
        Assert.That(() => operations.Tokenize(null!), Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(() => operations.ConvertInterval(null!), Throws.TypeOf<System.ArgumentNullException>());
    }

    private static VersionParseOptions SemVerParseOptions()
    {
        return new VersionParseOptions(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            4096,
            true,
            true,
            PrereleasePolicy.Default);
    }

    private static VersionRangeParseOptions MavenRangeOptions()
    {
        return new VersionRangeParseOptions(
            RangeSyntax.Maven,
            PrereleasePolicy.Default,
            4096,
            true,
            true,
            true,
            true,
            true);
    }

    private static VersionComparisonOptions SemVerComparisonOptions()
    {
        return new VersionComparisonOptions(
            VersionDialect.SemVer,
            VersionComparisonMode.Precedence,
            true,
            false,
            InvalidVersionOrdering.Reject);
    }
}
