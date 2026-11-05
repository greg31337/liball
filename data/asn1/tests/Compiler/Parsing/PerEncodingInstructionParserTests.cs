using asn1;

namespace asn1.Tests;

public class PerEncodingInstructionParserTests
{
    [Test]
    public void PrefixParserRetainsPositiveAndNegatingInstructionDetails()
    {
        TypeFixture positive = ParseType("[PER:SIZE 4 OCTETS] OCTET STRING");
        TypeFixture negating = ParseType("[PER:NOT SIZE 4] OCTET STRING");

        ParsedTypeSyntax positiveType = RequireType(positive);
        ParsedTypeSyntax negatingType = RequireType(negating);
        AsnEncodingInstruction positiveInstruction = positiveType.EncodingInstructions.Single();
        AsnEncodingInstruction negatingInstruction = negatingType.EncodingInstructions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(positiveInstruction.Name, Is.EqualTo("SIZE"));
            Assert.That(positiveInstruction.Kind, Is.EqualTo(AsnInstructionKind.PerApplication));
            Assert.That(positiveInstruction.Negated, Is.False);
            Assert.That(positiveInstruction.Arguments, Is.EqualTo(new[] { "4", "OCTETS" }));
            Assert.That(positiveType.EncodingInstructionSyntax.Single().ArgumentTokens
                .Select(token => token.Text), Is.EqualTo(new[] { "4", "OCTETS" }));
            Assert.That(negatingInstruction.Name, Is.EqualTo("SIZE"));
            Assert.That(negatingInstruction.Negated, Is.True);
            Assert.That(negatingInstruction.Arguments, Is.EqualTo(new[] { "4" }));
            Assert.That(positive.Diagnostics.GetDiagnostics(), Is.Empty);
            Assert.That(negating.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ControlListParsesEveryTargetFormIntoStructuredSyntax()
    {
        ParseFixture fixture = ParseModule(
            "Controls DEFINITIONS ::= BEGIN\n"
                + "Packet ::= SEQUENCE { header SEQUENCE { length INTEGER }, "
                + "payload SEQUENCE OF OCTET STRING, field1 INTEGER, field2 BOOLEAN }\n"
                + "ENCODING-CONTROL PER\n"
                + "[SIZE 8] Packet, Packet.header.length, Packet.payload.*, "
                + "Packet.header.ALL, INTEGER, OCTET STRING, UTF8String\n"
                + "[NOT ALIGNMENT 16] field1, field2 IN Packet, ALL IN Packet, "
                + "COMPONENTS IN Packet.header, ALL\n"
                + "[EMPTY]\n"
                + "END");

        EncodingControlSectionSyntax section = RequireSection(fixture);
        Assert.That(section.Instructions, Has.Length.EqualTo(3));
        ParsedEncodingInstructionSyntax sized = section.Instructions[0];
        ParsedEncodingInstructionSyntax aligned = section.Instructions[1];
        Assert.Multiple(() =>
        {
            Assert.That(sized.Instruction.Name, Is.EqualTo("SIZE"));
            Assert.That(sized.Instruction.Arguments, Is.EqualTo(new[] { "8" }));
            Assert.That(sized.Instruction.Targets, Is.EqualTo(new[]
            {
                "Packet",
                "Packet.header.length",
                "Packet.payload.*",
                "Packet.header.ALL",
                "INTEGER",
                "OCTET STRING",
                "UTF8String"
            }));
            Assert.That(sized.PerTargets.Select(target => target.Kind), Is.EqualTo(new[]
            {
                PerEncodingInstructionTargetKind.TypeReference,
                PerEncodingInstructionTargetKind.TypeReference,
                PerEncodingInstructionTargetKind.TypeReference,
                PerEncodingInstructionTargetKind.TypeReference,
                PerEncodingInstructionTargetKind.BuiltInType,
                PerEncodingInstructionTargetKind.BuiltInType,
                PerEncodingInstructionTargetKind.BuiltInType
            }));
            Assert.That(sized.PerTargets[1].TypeReference, Is.EqualTo("Packet"));
            Assert.That(sized.PerTargets[1].ComponentPath,
                Is.EqualTo(new[] { "header", "length" }));
            Assert.That(sized.PerTargets[2].ComponentPath,
                Is.EqualTo(new[] { "payload", "*" }));
            Assert.That(sized.PerTargets[3].ComponentPath,
                Is.EqualTo(new[] { "header", "ALL" }));
            Assert.That(sized.PerTargets[5].BuiltInType, Is.EqualTo("OCTET STRING"));
            Assert.That(aligned.Instruction.Name, Is.EqualTo("ALIGNMENT"));
            Assert.That(aligned.Instruction.Negated, Is.True);
            Assert.That(aligned.Instruction.Arguments, Is.EqualTo(new[] { "16" }));
            Assert.That(aligned.Instruction.Targets, Is.EqualTo(new[]
            {
                "field1,field2 IN Packet",
                "ALL IN Packet",
                "COMPONENTS IN Packet.header",
                "ALL"
            }));
            Assert.That(section.Instructions[2].Instruction.Targets, Is.Empty);
            Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
        });

        Assert.Multiple(() =>
        {
            Assert.That(aligned.PerTargets[0].Kind,
                Is.EqualTo(PerEncodingInstructionTargetKind.IdentifiersInContext));
            Assert.That(aligned.PerTargets[0].Identifiers,
                Is.EqualTo(new[] { "field1", "field2" }));
            Assert.That(aligned.PerTargets[0].TypeReference, Is.EqualTo("Packet"));
            Assert.That(aligned.PerTargets[1].AllIdentifiers, Is.True);
            Assert.That(aligned.PerTargets[2].Components, Is.True);
            Assert.That(aligned.PerTargets[2].ComponentPath, Is.EqualTo(new[] { "header" }));
            Assert.That(aligned.PerTargets[3].Kind,
                Is.EqualTo(PerEncodingInstructionTargetKind.AllTypes));
        });
    }

    [Test]
    public void BuiltInTargetsIncludeConstructorsModernOidFormsAndStringTypes()
    {
        ParseFixture fixture = ParseModule(
            "BuiltIns DEFINITIONS ::= BEGIN\n"
                + "Value ::= INTEGER\n"
                + "ENCODING-CONTROL PER\n"
                + "[RULE] BIT STRING, CHARACTER STRING, CHOICE, EMBEDDED PDV, "
                + "INSTANCE OF, OBJECT IDENTIFIER, OID-IRI, RELATIVE-OID-IRI, "
                + "SEQUENCE OF, SET OF, TIME-OF-DAY, BMPString, VisibleString\n"
                + "END");

        ParsedEncodingInstructionSyntax instruction =
            RequireSection(fixture).Instructions.Single();
        Assert.That(instruction.PerTargets, Has.Length.EqualTo(13));
        Assert.That(instruction.PerTargets.Select(target => target.Kind),
            Is.All.EqualTo(PerEncodingInstructionTargetKind.BuiltInType));
        Assert.That(instruction.PerTargets.Select(target => target.BuiltInType),
            Is.EqualTo(new[]
            {
                "BIT STRING",
                "CHARACTER STRING",
                "CHOICE",
                "EMBEDDED PDV",
                "INSTANCE OF",
                "OBJECT IDENTIFIER",
                "OID-IRI",
                "RELATIVE-OID-IRI",
                "SEQUENCE OF",
                "SET OF",
                "TIME-OF-DAY",
                "BMPString",
                "VisibleString"
            }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserRejectsMissingKeywordsAndNonTerminalAllPathComponents()
    {
        TypeFixture prefix = ParseType("[PER:NOT] INTEGER");
        ParseFixture section = ParseModule(
            "Invalid DEFINITIONS ::= BEGIN\n"
                + "Packet ::= INTEGER\n"
                + "ENCODING-CONTROL PER\n"
                + "[RULE] Packet.ALL.value\n"
                + "END");

        Assert.Multiple(() =>
        {
            Assert.That(prefix.Result.Success, Is.False);
            Assert.That(prefix.Diagnostics.GetDiagnostics(), Is.Not.Empty);
            Assert.That(RequireSection(section).Instructions, Is.Empty);
            Assert.That(section.Session.GetDiagnostics(), Is.Not.Empty);
        });
    }

    private static ParseFixture ParseModule(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "per-controls.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        return new ParseFixture
        {
            Session = session,
            Result = new ModuleParser(new TokenCursor(session, 4)).ParseModule()
        };
    }

    private static TypeFixture ParseType(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "per-prefix.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens = new System.Collections.Generic.List<AsnToken>();
        TokenCursor cursor = new TokenCursor(session, 4);
        TokenCursorResult current = cursor.ReadNext();
        while (current.Status == TokenCursorStatus.Token && current.Token != null)
        {
            tokens.Add(current.Token);
            current = cursor.ReadNext();
        }

        return new TypeFixture
        {
            Diagnostics = diagnostics,
            Result = new TypeParser(tokens.ToArray(), diagnostics, true).ParseTypeSyntax()
        };
    }

    private static ModuleSyntax RequireModule(ParseFixture fixture)
    {
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(fixture.Result.Module, Is.Not.Null);
        return fixture.Result.Module ?? throw new InvalidOperationException();
    }

    private static EncodingControlSectionSyntax RequireSection(ParseFixture fixture)
    {
        return RequireModule(fixture).EncodingControlSections.Single();
    }

    private static ParsedTypeSyntax RequireType(TypeFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.Syntax, Is.Not.Null);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private class ParseFixture
    {
        internal IncrementalLexSession Session = null!;
        internal ModuleParseResult Result = null!;
    }

    private class TypeFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal TypeParseResult Result = null!;
    }
}
