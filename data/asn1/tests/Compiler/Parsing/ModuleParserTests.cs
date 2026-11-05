using System.Numerics;
using asn1;

namespace asn1.Tests;

public class ModuleParserTests
{
    [Test]
    public void ParserAcceptsTheMinimalModuleAndAppliesHeaderDefaults()
    {
        ParseFixture fixture = Parse("Example DEFINITIONS ::= BEGIN END");

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(5));
        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(module.Name, Is.EqualTo("Example"));
        Assert.That(module.HasDefinitiveOid, Is.False);
        Assert.That(module.HasDefinitiveIri, Is.False);
        Assert.That(module.EncodingReference, Is.EqualTo("TAG"));
        Assert.That(module.EncodingReferenceSpecified, Is.False);
        Assert.That(module.TagDefault, Is.EqualTo(AsnTaggingMode.Explicit));
        Assert.That(module.TagDefaultSpecified, Is.False);
        Assert.That(module.ExtensibilityImplied, Is.False);
        Assert.That(module.BodyTokens, Is.Empty);
        Assert.That(module.EncodingControlSections, Is.Empty);
        Assert.That(module.Span?.Start.Offset, Is.Zero);
        Assert.That(module.Span?.End.Offset, Is.EqualTo(33));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserRetainsDefinitiveIdentificationDefaultsBodyAndControlSections()
    {
        string source = "Example { iso(1) 2 organization(3) } \"/ISO/2/Organization\" "
            + "DEFINITIONS OER INSTRUCTIONS AUTOMATIC TAGS EXTENSIBILITY IMPLIED ::= BEGIN\n"
            + "Value ::= INTEGER\n"
            + "ENCODING-CONTROL XER\n-- keep --\nValue ATTRIBUTE\n"
            + "ENCODING-CONTROL PER\nValue ALIGNED\n"
            + "END";

        ParseFixture fixture = Parse(source);

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(module.DefinitiveOidComponents, Has.Length.EqualTo(3));
        Assert.That(module.DefinitiveOidComponents[0].Name, Is.EqualTo("iso"));
        Assert.That(module.DefinitiveOidComponents[0].Number, Is.EqualTo(new BigInteger(1)));
        Assert.That(module.DefinitiveOidComponents[0].HasName, Is.True);
        Assert.That(module.DefinitiveOidComponents[0].HasNumber, Is.True);
        Assert.That(module.DefinitiveOidComponents[1].HasName, Is.False);
        Assert.That(module.DefinitiveOidComponents[1].Number, Is.EqualTo(new BigInteger(2)));
        Assert.That(module.DefinitiveOidComponents[2].Name, Is.EqualTo("organization"));
        Assert.That(module.DefinitiveOidComponents[2].Number, Is.EqualTo(new BigInteger(3)));
        Assert.That(module.HasDefinitiveIri, Is.True);
        Assert.That(module.DefinitiveIri, Is.EqualTo("/ISO/2/Organization"));
        Assert.That(module.EncodingReference, Is.EqualTo("OER"));
        Assert.That(module.EncodingReferenceSpecified, Is.True);
        Assert.That(module.TagDefault, Is.EqualTo(AsnTaggingMode.Automatic));
        Assert.That(module.TagDefaultSpecified, Is.True);
        Assert.That(module.ExtensibilityImplied, Is.True);
        Assert.That(module.BodyTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "Value", "::=", "INTEGER"
        }));
        Assert.That(module.EncodingControlSections, Has.Length.EqualTo(2));
        Assert.That(module.EncodingControlSections[0].EncodingReference, Is.EqualTo("XER"));
        Assert.That(
            SignificantTexts(module.EncodingControlSections[0].InstructionTokens),
            Is.EqualTo(new[] { "Value", "ATTRIBUTE" }));
        Assert.That(
            module.EncodingControlSections[0].InstructionTokens.Select(token => token.Kind),
            Does.Contain(AsnTokenKind.Comment));
        Assert.That(
            module.EncodingControlSections[0].InstructionTokens.Select(token => token.Kind),
            Does.Contain(AsnTokenKind.Whitespace));
        Assert.That(module.EncodingControlSections[1].EncodingReference, Is.EqualTo("PER"));
        Assert.That(
            SignificantTexts(module.EncodingControlSections[1].InstructionTokens),
            Is.EqualTo(new[] { "Value", "ALIGNED" }));
        Assert.That(module.Span?.End.Offset, Is.EqualTo(source.Length));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("EXPLICIT", AsnTaggingMode.Explicit)]
    [TestCase("IMPLICIT", AsnTaggingMode.Implicit)]
    [TestCase("AUTOMATIC", AsnTaggingMode.Automatic)]
    public void ParserRecognizesEveryTagDefault(string keyword, AsnTaggingMode expected)
    {
        ParseFixture fixture = Parse(
            "Tags DEFINITIONS " + keyword + " TAGS ::= BEGIN END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.TagDefault, Is.EqualTo(expected));
        Assert.That(module.TagDefaultSpecified, Is.True);
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserPreservesNamedAndArbitrarySizeDefinitiveOidComponents()
    {
        string huge = "1234567890123456789012345678901234567890";
        ParseFixture fixture = Parse(
            "Identity { joint-iso-itu-t " + huge + " member-body(2) } DEFINITIONS ::= BEGIN END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.DefinitiveOidComponents[0].Name, Is.EqualTo("joint-iso-itu-t"));
        Assert.That(module.DefinitiveOidComponents[0].HasNumber, Is.False);
        Assert.That(module.DefinitiveOidComponents[1].Number, Is.EqualTo(BigInteger.Parse(huge)));
        Assert.That(module.DefinitiveOidComponents[2].Name, Is.EqualTo("member-body"));
        Assert.That(module.DefinitiveOidComponents[2].Number, Is.EqualTo(new BigInteger(2)));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserDiagnosesEmptyDefinitiveOidWithoutThrowing()
    {
        ParseFixture fixture = Parse("Broken {} DEFINITIONS ::= BEGIN END");

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(fixture.Result.Module?.HasDefinitiveOid, Is.True);
        Assert.That(fixture.Session.GetDiagnostics().Select(item => item.Code), Does.Contain("ASNP0002"));
    }

    [Test]
    public void ParserDiagnosesTagAndDuplicateEncodingControlReferences()
    {
        string source = "Controls DEFINITIONS ::= BEGIN "
            + "ENCODING-CONTROL TAG ignored "
            + "ENCODING-CONTROL XER first "
            + "ENCODING-CONTROL XER second END";
        ParseFixture fixture = Parse(source);

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(RequireModule(fixture.Result).EncodingControlSections, Has.Length.EqualTo(3));
        Assert.That(
            fixture.Session.GetDiagnostics().Select(item => item.Code),
            Is.EqualTo(new[] { "ASNP0005", "ASNP0006" }));
    }

    [Test]
    public void ParserKeepsEndAvailableWhenARequiredHeaderTokenIsMissing()
    {
        ParseFixture fixture = Parse("Broken DEFINITIONS AUTOMATIC END");

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(fixture.Cursor.PeekNext(0).Token?.Text, Is.EqualTo("END"));
        Assert.That(fixture.Session.GetDiagnostics().Select(item => item.Code), Does.Contain("ASNP0001"));
    }

    private static ParseFixture Parse(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "module.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        TokenCursor cursor = new TokenCursor(session, 4);
        ModuleParser parser = new ModuleParser(cursor);
        return new ParseFixture
        {
            Session = session,
            Cursor = cursor,
            Result = parser.ParseModule()
        };
    }

    private static ModuleSyntax RequireModule(ModuleParseResult result)
    {
        Assert.That(result.Module, Is.Not.Null);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static IEnumerable<string> SignificantTexts(AsnToken[] tokens)
    {
        return tokens
            .Where(token => token.Kind != AsnTokenKind.Whitespace && token.Kind != AsnTokenKind.Comment)
            .Select(token => token.Text);
    }

    private class ParseFixture
    {
        internal IncrementalLexSession Session = null!;
        internal TokenCursor Cursor = null!;
        internal ModuleParseResult Result = null!;
    }
}
