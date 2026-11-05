using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlEnumsTests
{
    [Test]
    public void TriviaKindsAndPlacements_CoverPublicTriviaSurface()
    {
        Assert.That(Enum.GetNames(typeof(global::yaml.YamlTriviaKind)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "Comment",
            "BlankLine"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlTriviaPlacement)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "Before",
            "Inline",
            "Following"
        }));
    }

    [Test]
    public void TokenKinds_CoverScannerTokenSurface()
    {
        Assert.That((int)global::yaml.YamlTokenKind.Unknown, Is.EqualTo(0));
        Assert.That(Enum.GetNames(typeof(global::yaml.YamlTokenKind)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "StreamStart",
            "StreamEnd",
            "DocumentStart",
            "DocumentEnd",
            "Directive",
            "Indent",
            "Dedent",
            "BlockSequenceEntry",
            "BlockMappingKey",
            "BlockMappingValue",
            "FlowSequenceStart",
            "FlowSequenceEnd",
            "FlowMappingStart",
            "FlowMappingEnd",
            "FlowEntry",
            "Comment",
            "BlankLine",
            "Tag",
            "Anchor",
            "Alias",
            "PlainScalar",
            "SingleQuotedScalar",
            "DoubleQuotedScalar",
            "LiteralScalar",
            "FoldedScalar",
            "Error",
            "Whitespace"
        }));
    }

    [Test]
    public void EventKinds_CoverParserEventSurface()
    {
        Assert.That((int)global::yaml.YamlEventKind.Unknown, Is.EqualTo(0));
        Assert.That(Enum.GetNames(typeof(global::yaml.YamlEventKind)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "StreamStart",
            "StreamEnd",
            "DocumentStart",
            "DocumentEnd",
            "SequenceStart",
            "SequenceEnd",
            "MappingStart",
            "MappingEnd",
            "Scalar",
            "Alias",
            "Comment",
            "Diagnostic",
            "YamlVersionDirective",
            "TagDirective",
            "ReservedDirective"
        }));
    }

    [Test]
    public void NodeKinds_CoverDocumentModelKinds()
    {
        Assert.That((int)global::yaml.YamlNodeKind.Unknown, Is.EqualTo(0));
        Assert.That(Enum.GetNames(typeof(global::yaml.YamlNodeKind)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "Empty",
            "Scalar",
            "Sequence",
            "Mapping",
            "Alias"
        }));
    }

    [Test]
    public void PathSegmentKinds_CoverPathNavigationKinds()
    {
        Assert.That((int)global::yaml.YamlPathSegmentKind.Unknown, Is.EqualTo(0));
        Assert.That(Enum.GetNames(typeof(global::yaml.YamlPathSegmentKind)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "MappingKey",
            "SequenceIndex"
        }));
    }

    [Test]
    public void StyleVersionAndPolicyEnums_CoverPublicOptionSurface()
    {
        Assert.That(Enum.GetNames(typeof(global::yaml.YamlScalarStyle)), Is.EquivalentTo(new string[]
        {
            "Any",
            "Plain",
            "SingleQuoted",
            "DoubleQuoted",
            "Literal",
            "Folded"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlCollectionStyle)), Is.EquivalentTo(new string[]
        {
            "Any",
            "Block",
            "Flow"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlInvalidValuePolicy)), Is.EquivalentTo(new string[]
        {
            "Error",
            "QuoteAsString"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlVersion)), Is.EquivalentTo(new string[]
        {
            "Unknown",
            "Version11",
            "Version12"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlSchemaKind)), Is.EquivalentTo(new string[]
        {
            "Failsafe",
            "Json",
            "Core",
            "Yaml11"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlNamingConvention)), Is.EquivalentTo(new string[]
        {
            "Exact",
            "CamelCase",
            "PascalCase",
            "SnakeCase",
            "KebabCase"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlDiagnosticSeverity)), Is.EquivalentTo(new string[]
        {
            "Info",
            "Warning",
            "Error"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlDuplicateKeyPolicy)), Is.EquivalentTo(new string[]
        {
            "Error",
            "Warn",
            "FirstWins",
            "LastWins",
            "PreserveAll"
        }));

        Assert.That(Enum.GetNames(typeof(global::yaml.YamlUnknownTagPolicy)), Is.EquivalentTo(new string[]
        {
            "Preserve",
            "Warn",
            "Error"
        }));
    }
}
