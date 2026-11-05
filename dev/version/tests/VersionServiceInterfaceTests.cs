using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionServiceInterfaceTests
{
    [Test]
    public void VersionService_ImplementsFocusedOperationInterfaces()
    {
        VersionService service = new VersionService();
        IVersionParsing parsing = service;
        IVersionRangeParsing rangeParsing = service;
        IVersionComparison comparison = service;
        IVersionSelection selection = service;
        IVersionFormatting formatting = service;
        IVersionInspection inspection = service;

        UniversalVersion parsed = parsing.Parse("1.2.3", VersionParseOptions.CreateStrict(VersionDialect.SemVer));
        VersionRange range = rangeParsing.ParseRange("^1.0.0", VersionRangeParseOptions.CreateNodeSemVer());
        UniversalVersion higher = parsing.Parse("1.5.0", VersionParseOptions.CreateStrict(VersionDialect.SemVer));
        UniversalVersion rejected = parsing.Parse("2.0.0", VersionParseOptions.CreateStrict(VersionDialect.SemVer));

        Assert.That(comparison.CompareVersions(parsed, higher, VersionComparisonOptions.CreatePrecedence(VersionDialect.SemVer)), Is.LessThan(0));
        Assert.That(selection.Filter(range, new UniversalVersion[] { parsed, higher, rejected }, VersionSelectionOptions.CreateDefault()), Is.EqualTo(new UniversalVersion[] { parsed, higher }));
        Assert.That(formatting.FormatCanonical(higher), Is.EqualTo("1.5.0"));
        Assert.That(inspection.IsStable(higher), Is.True);
    }

    [Test]
    public void ParsingConsumer_CanUseAControlledTestDouble()
    {
        VersionParseResult controlledResult = SemVer.Parse("9.8.7");
        FakeVersionParsing fake = new FakeVersionParsing(controlledResult);
        ParsingConsumer consumer = new ParsingConsumer(fake);

        string normalized = consumer.NormalizeIfValid("not parsed by the fake");

        Assert.That(normalized, Is.EqualTo("9.8.7"));
        Assert.That(fake.lastText, Is.EqualTo("not parsed by the fake"));
    }

    [TestCase(VersionDialect.SemVer, "1.2.3")]
    [TestCase(VersionDialect.NodeSemVer, "v1.2.3")]
    [TestCase(VersionDialect.Cargo, "1.2.3")]
    [TestCase(VersionDialect.NuGet, "1.2")]
    [TestCase(VersionDialect.Maven, "1.0-alpha")]
    [TestCase(VersionDialect.Go, "v1.2.3")]
    [TestCase(VersionDialect.RubyGems, "1.0.a")]
    [TestCase(VersionDialect.Python, "1.0rc1")]
    public void DialectStrategies_ParseEverySupportedDialect(VersionDialect dialect, string text)
    {
        VersionService service = new VersionService();

        VersionParseResult result = service.TryParse(text, VersionParseOptions.CreateForDialect(dialect));

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().GetDialect(), Is.EqualTo(dialect));
    }

    [TestCase(RangeSyntax.NodeSemVer, "^1.2.3")]
    [TestCase(RangeSyntax.Cargo, "1.2")]
    [TestCase(RangeSyntax.NuGet, "[1.0,2.0)")]
    [TestCase(RangeSyntax.Maven, "[1.0,2.0)")]
    [TestCase(RangeSyntax.RubyGems, "~> 1.2")]
    [TestCase(RangeSyntax.Python, "~=1.4")]
    public void RangeStrategies_ParseEverySupportedSyntax(RangeSyntax syntax, string text)
    {
        VersionService service = new VersionService();

        VersionRangeParseResult result = service.TryParseRange(text, VersionRangeParseOptions.CreateForSyntax(syntax));

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(syntax));
    }
}

public class ParsingConsumer
{
    public IVersionParsing parsing;

    public ParsingConsumer(IVersionParsing parsingValue)
    {
        parsing = parsingValue;
    }

    public string NormalizeIfValid(string text)
    {
        VersionParseResult result = parsing.TryParse(text, VersionParseOptions.CreateStrict(VersionDialect.SemVer));
        if (result.IsFailure())
        {
            return "";
        }

        return result.GetVersion().ToNormalizedString();
    }
}

public class FakeVersionParsing : IVersionParsing
{
    public VersionParseResult result;
    public string lastText;

    public FakeVersionParsing(VersionParseResult resultValue)
    {
        result = resultValue;
        lastText = "";
    }

    public UniversalVersion Parse(string text, VersionParseOptions options)
    {
        lastText = text;
        return result.GetVersion();
    }

    public VersionParseResult TryParse(string text, VersionParseOptions options)
    {
        lastText = text;
        return result;
    }
}
