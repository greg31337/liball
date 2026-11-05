using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeFormattingAndMinimumTests
{
    [TestCase(RangeSyntax.NodeSemVer, ">=1.2.3 <2.0.0", ">=1.2.3 <2.0.0")]
    [TestCase(RangeSyntax.Cargo, ">=1.2, <2.0", ">=1.2.0, <2.0.0")]
    [TestCase(RangeSyntax.NuGet, "[1.0,2.0)", "[1.0.0,2.0.0)")]
    [TestCase(RangeSyntax.Maven, "[1.0,2.0)", "[1,2)")]
    [TestCase(RangeSyntax.RubyGems, ">= 1.0, < 2.0", ">= 1.0, < 2.0")]
    [TestCase(RangeSyntax.Python, ">=1.0,<2.0", ">=1.0, <2.0")]
    public void FormatNormalizedRange_UsesEverySupportedSyntax(
        RangeSyntax syntax,
        string sourceText,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, sourceText);

        string normalized = operations.FormatNormalizedRange(range, syntax);

        Assert.That(normalized, Is.EqualTo(expectedText));
    }

    [TestCase(RangeSyntax.NodeSemVer, ">=1.2.3 <2.0.0", "1.2.3")]
    [TestCase(RangeSyntax.Cargo, ">=1.2, <2.0", "1.2.0")]
    [TestCase(RangeSyntax.NuGet, "[1.0,2.0)", "1.0.0")]
    [TestCase(RangeSyntax.Maven, "[1.0,2.0)", "1")]
    [TestCase(RangeSyntax.RubyGems, ">= 1.0, < 2.0", "1.0")]
    [TestCase(RangeSyntax.Python, ">=1.0,<2.0", "1.0")]
    public void MinimumPossibleVersion_UsesEachSyntaxComparisonSemantics(
        RangeSyntax syntax,
        string sourceText,
        string expectedVersionText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        IVersionFormatting formatting = new VersionService();
        VersionRange range = ParseRange(syntax, sourceText);

        VersionRangeMinimumResult result = operations.GetMinimumPossibleVersion(range, syntax);

        Assert.That(result.hasValue, Is.True);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(formatting.FormatNormalized(result.version!), Is.EqualTo(expectedVersionText));
    }

    [TestCase(RangeSyntax.NodeSemVer, ">1.2.3 <2.0.0", "1.2.4")]
    [TestCase(RangeSyntax.Cargo, ">1.2.3, <2.0.0", "1.2.4")]
    [TestCase(RangeSyntax.NuGet, "(1.0,2.0]", "1.0.0.1")]
    public void StrictRanges_ReturnTheNextRepresentableDiscreteVersion(
        RangeSyntax syntax,
        string sourceText,
        string expectedVersionText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        IVersionFormatting formatting = new VersionService();
        VersionRange range = ParseRange(syntax, sourceText);

        VersionRangeMinimumResult result = operations.GetMinimumPossibleVersion(range, syntax);

        Assert.That(result.hasValue, Is.True);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(formatting.FormatNormalized(result.version!), Is.EqualTo(expectedVersionText));
    }

    [TestCase(RangeSyntax.Maven, "(1.0,2.0]")]
    [TestCase(RangeSyntax.RubyGems, "> 1.0")]
    [TestCase(RangeSyntax.Python, ">1.0")]
    public void StrictDenseRanges_ReturnExplicitNoValue(
        RangeSyntax syntax,
        string sourceText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, sourceText);

        VersionRangeMinimumResult result = operations.GetMinimumPossibleVersion(range, syntax);

        Assert.That(result.hasValue, Is.False);
        Assert.That(result.version, Is.Null);
    }

    [Test]
    public void Disjunction_ReturnsARepresentedMinimumBelowAnUnrepresentableBranch()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        IVersionFormatting formatting = new VersionService();
        VersionRange range = ParseRange(RangeSyntax.Maven, "[0,1],(2,3]");

        VersionRangeMinimumResult result = operations.GetMinimumPossibleVersion(
            range,
            RangeSyntax.Maven);

        Assert.That(result.hasValue, Is.True);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(formatting.FormatNormalized(result.version!), Is.EqualTo("0"));
    }

    [Test]
    public void EmptyAndUniversalRanges_HaveExplicitMinimumResults()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        IVersionFormatting formatting = new VersionService();
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange universal = operations.CreateUniversalRange();

        VersionRangeMinimumResult emptyResult = operations.GetMinimumPossibleVersion(
            empty,
            RangeSyntax.NodeSemVer);
        VersionRangeMinimumResult universalResult = operations.GetMinimumPossibleVersion(
            universal,
            RangeSyntax.NodeSemVer);

        Assert.That(emptyResult.hasValue, Is.False);
        Assert.That(emptyResult.version, Is.Null);
        Assert.That(universalResult.hasValue, Is.True);
        Assert.That(universalResult.version, Is.Not.Null);
        Assert.That(formatting.FormatNormalized(universalResult.version!), Is.EqualTo("0.0.0"));
    }

    [Test]
    public void NuGetFloatingFormatting_PreservesTheNormalizedPattern()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(RangeSyntax.NuGet, "01.002.*-*");

        string normalized = operations.FormatNormalizedRange(range, RangeSyntax.NuGet);

        Assert.That(normalized, Is.EqualTo("1.2.*-*"));
    }

    [Test]
    public void MinimumResult_RejectsInconsistentPublicState()
    {
        UniversalVersion version = ParseVersion("1.0.0");

        Assert.That(() => new VersionRangeMinimumResult(true, null), Throws.ArgumentException);
        Assert.That(() => new VersionRangeMinimumResult(false, version), Throws.ArgumentException);
        Assert.That(new VersionRangeMinimumResult(false, null).hasValue, Is.False);
        Assert.That(new VersionRangeMinimumResult(true, version).version, Is.SameAs(version));
    }

    [Test]
    public void FormattingAndMinimumOperations_ValidatePublicInputs()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = operations.CreateUniversalRange();

        Assert.That(
            () => operations.FormatNormalizedRange(null!, RangeSyntax.NodeSemVer),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.GetMinimumPossibleVersion(null!, RangeSyntax.NodeSemVer),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.FormatNormalizedRange(range, RangeSyntax.Unknown),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(
            () => operations.GetMinimumPossibleVersion(range, RangeSyntax.Unknown),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    static VersionRange ParseRange(RangeSyntax syntax, string text)
    {
        IVersionRangeParsing parsing = new VersionService();
        return parsing.ParseRange(text, RangeOptions(syntax));
    }

    static UniversalVersion ParseVersion(string text)
    {
        IVersionParsing parsing = new VersionService();
        return parsing.Parse(text, VersionOptions());
    }

    static VersionRangeParseOptions RangeOptions(RangeSyntax syntax)
    {
        return new VersionRangeParseOptions(
            syntax,
            PrereleasePolicy.Include,
            4096,
            true,
            true,
            true,
            true,
            true);
    }

    static VersionParseOptions VersionOptions()
    {
        return new VersionParseOptions(
            VersionDialect.NodeSemVer,
            VersionParseMode.Strict,
            4096,
            true,
            true,
            PrereleasePolicy.Include);
    }
}
