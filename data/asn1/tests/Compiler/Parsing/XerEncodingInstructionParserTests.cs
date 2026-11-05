using asn1;

namespace asn1.Tests;

public class XerEncodingInstructionParserTests
{
    [Test]
    public void RecommendedControlListParsesTargetsNegationAndNamespaceSettings()
    {
        ParseFixture fixture = ParseModule(
            "Controls DEFINITIONS ::= BEGIN\n"
                + "Person ::= SEQUENCE { name UTF8String, id INTEGER, values SEQUENCE OF INTEGER }\n"
                + "ENCODING-CONTROL XER\n"
                + "[GLOBAL-DEFAULTS CONTROL-NAMESPACE \"urn:control\" PREFIX \"xc\"]\n"
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS]\n"
                + "[ATTRIBUTE] Person.name, Person.id\n"
                + "[NOT LIST] Person.values\n"
                + "[ELEMENT] Person\n"
                + "[NAMESPACE AS \"urn:people\" PREFIX \"p\"] Person\n"
                + "END");

        EncodingControlSectionSyntax section = RequireSection(fixture);
        Assert.That(section.Instructions, Has.Length.EqualTo(6));
        Assert.That(section.Instructions.Select(item => item.Instruction.Kind), Is.EqualTo(new[]
        {
            AsnInstructionKind.GlobalDefaults,
            AsnInstructionKind.GlobalDefaults,
            AsnInstructionKind.Attribute,
            AsnInstructionKind.List,
            AsnInstructionKind.Element,
            AsnInstructionKind.Namespace
        }));
        Assert.That(section.Instructions[0].ControlNamespace, Is.True);
        Assert.That(section.Instructions[0].NamespaceUri, Is.EqualTo("urn:control"));
        Assert.That(section.Instructions[0].NamespacePrefix, Is.EqualTo("xc"));
        Assert.That(section.Instructions[1].ModifiedEncodings, Is.True);
        Assert.That(section.Instructions[2].Instruction.Targets,
            Is.EqualTo(new[] { "Person.name", "Person.id" }));
        Assert.That(section.Instructions[3].Instruction.Negated, Is.True);
        Assert.That(section.Instructions[3].Instruction.Targets,
            Is.EqualTo(new[] { "Person.values" }));
        Assert.That(section.Instructions[4].Instruction.Negated, Is.True);
        Assert.That(section.Instructions[5].NamespaceUri, Is.EqualTo("urn:people"));
        Assert.That(section.Instructions[5].NamespacePrefix, Is.EqualTo("p"));
        Assert.That(section.Instructions[5].Instruction.Targets,
            Is.EqualTo(new[] { "Person" }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void HistoricalControlListSeparatesTargetsFromInstructionArguments()
    {
        ParseFixture fixture = ParseModule(
            "Controls DEFINITIONS ::= BEGIN\n"
                + "My-Type ::= SEQUENCE { field1 INTEGER, field2 UTF8String }\n"
                + "ENCODING-CONTROL XER\n"
                + "ATTRIBUTE field1 IN My-Type NAME My-Type.field2 AS \"display-name\"\n"
                + "WHITESPACE My-Type.field2 COLLAPSE\n"
                + "END");

        EncodingControlSectionSyntax section = RequireSection(fixture);
        Assert.That(section.Instructions, Has.Length.EqualTo(3));
        Assert.That(section.Instructions[0].Instruction.Targets,
            Is.EqualTo(new[] { "field1 IN My-Type" }));
        Assert.That(section.Instructions[1].Instruction.Targets,
            Is.EqualTo(new[] { "My-Type.field2" }));
        Assert.That(section.Instructions[1].ArgumentTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "AS", "\"display-name\"" }));
        Assert.That(section.Instructions[2].Instruction.Targets,
            Is.EqualTo(new[] { "My-Type.field2" }));
        Assert.That(section.Instructions[2].ArgumentTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "COLLAPSE" }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void PrefixParserRetainsNegationAndNamespaceDeclarationArguments()
    {
        TypeFixture negated = ParseType("[XER:NOT ATTRIBUTE] UTF8String");
        TypeFixture namespaced = ParseType(
            "[XER:NAMESPACE AS \"urn:item\" PREFIX \"i\"] UTF8String");

        ParsedTypeSyntax negatedSyntax = RequireType(negated);
        ParsedTypeSyntax namespaceSyntax = RequireType(namespaced);
        Assert.That(negatedSyntax.EncodingInstructions[0].Kind,
            Is.EqualTo(AsnInstructionKind.Attribute));
        Assert.That(negatedSyntax.EncodingInstructions[0].Negated, Is.True);
        Assert.That(namespaceSyntax.EncodingInstructionSyntax[0].NamespaceUri,
            Is.EqualTo("urn:item"));
        Assert.That(namespaceSyntax.EncodingInstructionSyntax[0].NamespacePrefix,
            Is.EqualTo("i"));
        Assert.That(negated.Diagnostics.GetDiagnostics(), Is.Empty);
        Assert.That(namespaced.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void WildcardNamespaceRestrictionsAreSeparatedAndRetained()
    {
        ParseFixture fixture = ParseModule(
            "Wildcards DEFINITIONS ::= BEGIN\n"
                + "Payload ::= UTF8String\n"
                + "ENCODING-CONTROL XER\n"
                + "ANY-ELEMENT Payload FROM \"urn:one\" ABSENT\n"
                + "END");
        TypeFixture prefixed = ParseType(
            "[XER:ANY-ATTRIBUTES EXCEPT \"urn:blocked\" ABSENT] "
                + "SEQUENCE OF UTF8String");

        EncodingControlSectionSyntax section = RequireSection(fixture);
        ParsedTypeSyntax prefix = RequireType(prefixed);
        Assert.Multiple(() =>
        {
            Assert.That(section.Instructions[0].Instruction.Targets,
                Is.EqualTo(new[] { "Payload" }));
            Assert.That(section.Instructions[0].Instruction.Arguments,
                Is.EqualTo(new[] { "FROM", "\"urn:one\"", "ABSENT" }));
            Assert.That(prefix.EncodingInstructions[0].Arguments,
                Is.EqualTo(new[] { "EXCEPT", "\"urn:blocked\"", "ABSENT" }));
        });
    }

    [Test]
    public void LowererEmitsEveryParsedControlSectionInstruction()
    {
        ParseFixture fixture = ParseModule(
            "Controls DEFINITIONS ::= BEGIN\n"
                + "Value ::= INTEGER\n"
                + "ENCODING-CONTROL XER\n"
                + "[ATTRIBUTE] Value\n"
                + "[NOT TEXT] Value\n"
                + "END");

        ModuleSyntax module = RequireModule(fixture);
        LoweredInstructionIr[] lowered = new EncodingNeutralIrLowerer()
            .LowerModuleInstructions(module);

        Assert.That(lowered, Has.Length.EqualTo(2));
        Assert.That(lowered.Select(item => item.Name),
            Is.EqualTo(new[] { "ATTRIBUTE", "TEXT" }));
        Assert.That(lowered.Select(item => item.Kind),
            Is.EqualTo(new[] { AsnInstructionKind.Attribute, AsnInstructionKind.Text }));
        Assert.That(lowered.Select(item => item.Negated),
            Is.EqualTo(new[] { false, true }));
        Assert.That(lowered.Select(item => item.Targets.Single()),
            Is.EqualTo(new[] { "Value", "Value" }));
    }

    private static ParseFixture ParseModule(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "xer-controls.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        return new ParseFixture
        {
            Session = session,
            Result = result
        };
    }

    private static TypeFixture ParseType(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "xer-prefix.asn",
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

        AsnToken[] tokenArray = tokens.ToArray();
        return new TypeFixture
        {
            Diagnostics = diagnostics,
            Result = new TypeParser(tokenArray, diagnostics, true).ParseTypeSyntax()
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
