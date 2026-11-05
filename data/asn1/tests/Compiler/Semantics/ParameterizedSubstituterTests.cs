using asn1;

namespace asn1.Tests;

public class ParameterizedSubstituterTests
{
    [Test]
    public void SubstituterMaterializesTagsConstraintsPathsAndEncodingInstructions()
    {
        TargetFixture fixture = BuildTargets(
            "Substitution DEFINITIONS ::= BEGIN "
                + "ITEM-CLASS ::= CLASS { &Type, &id INTEGER }; "
                + "Template { INTEGER : tag, INTEGER : limit, T, "
                + "INTEGER : rule, ITEM-CLASS : item } ::= "
                + "[tag] [PER:SIZE rule] SEQUENCE { "
                + "field T (limit..10), selected item.&Type } END");
        ParameterSubstitutionResult result = Substitute(
            fixture,
            "Template { 3, 1, INTEGER, 4, objectValue }",
            "Template",
            AsnParameterKind.Type,
            "Caller");
        Assert.That(result.Success, Is.True,
            result.FailureMessage + " " + string.Join(" ",
                result.Tokens.Select(item => item.Text)));
        ParsedTypeSyntax tagged = result.AssignmentSyntax?.TypeSyntax
            ?? throw new InvalidOperationException();
        ParsedTypeSyntax prefixed = tagged.InnerTypeSyntax
            ?? throw new InvalidOperationException();
        ParsedTypeSyntax sequence = prefixed.InnerTypeSyntax
            ?? throw new InvalidOperationException();

        Assert.That(tagged.Type.Tag?.Number.ToString(), Is.EqualTo("3"));
        Assert.That(tagged.HasNumericTagNumber, Is.True);
        Assert.That(prefixed.EncodingInstructionTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "SIZE", "4" }));
        Assert.That(sequence.ComponentSyntax[0].TypeSyntax?.Type.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(sequence.ComponentSyntax[0].TypeSyntax?.ConstraintSyntax
            .Single().SourceTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "(", "1", "..", "10", ")" }));
        Assert.That(sequence.ComponentSyntax[1].TypeSyntax?.InformationFromObjects
            ?.ReferenceName, Is.EqualTo("objectValue"));
        Assert.That(sequence.ComponentSyntax[1].TypeSyntax?.InformationFromObjects
            ?.FieldPath, Is.EqualTo(new[] { "&Type" }));
    }

    [Test]
    public void SubstituterDoesNotCaptureInsertedReferences()
    {
        TargetFixture fixture = BuildTargets(
            "Substitution DEFINITIONS ::= BEGIN "
                + "U ::= UTF8String; "
                + "Pair { T, U } ::= SEQUENCE { "
                + "first T, second U, third Substitution.U } END");
        ParameterSubstitutionResult result = Substitute(
            fixture,
            "Pair { U, INTEGER }",
            "Pair",
            AsnParameterKind.Type,
            "Caller");
        Assert.That(result.Success, Is.True,
            result.FailureMessage + " " + string.Join(" ",
                result.Tokens.Select(item => item.Text)));
        ParsedTypeSyntax type = result.AssignmentSyntax?.TypeSyntax
            ?? throw new InvalidOperationException();
        SubstitutionTokenOrigin inserted = result.Origins.Single(item =>
            item.Inserted && item.SourceToken?.Text == "U");
        SubstitutionTokenOrigin qualifiedTemplate = result.Origins.Single(item =>
            !item.Inserted
            && item.SourceToken?.Text == "U"
            && item.Token != null
            && result.Tokens[Array.IndexOf(result.Tokens, item.Token) - 1].Kind
                == AsnTokenKind.Dot);

        Assert.That(type.ComponentSyntax[0].TypeSyntax?.ReferenceName,
            Is.EqualTo("U"));
        Assert.That(type.ComponentSyntax[1].TypeSyntax?.Type.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(type.ComponentSyntax[2].TypeSyntax?.ReferenceModule,
            Is.EqualTo("Substitution"));
        Assert.That(inserted.Module?.ModuleName, Is.EqualTo("Caller"));
        Assert.That(inserted.FormalParameter?.Parameter.Name, Is.EqualTo("T"));
        Assert.That(qualifiedTemplate.Module, Is.SameAs(fixture.Table));
    }

    [Test]
    public void SubstituterReparsesEveryAssignmentCategory()
    {
        TargetFixture fixture = BuildTargets(
            "Categories DEFINITIONS ::= BEGIN "
                + "ITEM-CLASS ::= CLASS { &Type, &id INTEGER }; "
                + "valueTemplate { INTEGER : value } INTEGER ::= value; "
                + "SetTemplate { INTEGER : Values } INTEGER ::= { Values }; "
                + "CLASS-TEMPLATE { T, INTEGER : value } ::= CLASS { "
                + "&Type DEFAULT T, &id INTEGER DEFAULT value }; "
                + "objectTemplate { T, INTEGER : value } ITEM-CLASS ::= { "
                + "&Type T, &id value }; "
                + "ObjectsTemplate { ITEM-CLASS : item } ITEM-CLASS ::= { item }; "
                + "InstanceTemplate { CLASS-GOVERNOR } ::= "
                + "INSTANCE OF CLASS-GOVERNOR; "
                + "SetUnionTemplate { ITEM-CLASS : Objects } ITEM-CLASS ::= "
                + "{ Objects } END");
        ParameterSubstitutionResult value = Substitute(
            fixture,
            "valueTemplate { 7 }",
            "valueTemplate",
            AsnParameterKind.Value,
            "Caller");
        ParameterSubstitutionResult valueSet = Substitute(
            fixture,
            "SetTemplate { { 1 | 2 } }",
            "SetTemplate",
            AsnParameterKind.ValueSet,
            "Caller");
        ParameterSubstitutionResult objectClass = Substitute(
            fixture,
            "CLASS-TEMPLATE { UTF8String, 7 }",
            "CLASS-TEMPLATE",
            AsnParameterKind.ObjectClass,
            "Caller");
        ParameterSubstitutionResult informationObject = Substitute(
            fixture,
            "objectTemplate { UTF8String, 7 }",
            "objectTemplate",
            AsnParameterKind.Object,
            "Caller");
        ParameterSubstitutionResult objectSet = Substitute(
            fixture,
            "ObjectsTemplate { itemValue }",
            "ObjectsTemplate",
            AsnParameterKind.ObjectSet,
            "Caller");
        ParameterSubstitutionResult objectClassActual = Substitute(
            fixture,
            "InstanceTemplate { ITEM-CLASS }",
            "InstanceTemplate",
            AsnParameterKind.Type,
            "Caller");
        ParameterSubstitutionResult objectSetActual = Substitute(
            fixture,
            "SetUnionTemplate { { itemValue } }",
            "SetUnionTemplate",
            AsnParameterKind.ObjectSet,
            "Caller");

        Assert.That(value.Success, Is.True, value.FailureMessage);
        Assert.That(value.AssignmentSyntax?.ValueSyntax?.Value.IntegerValue.ToString(),
            Is.EqualTo("7"));
        Assert.That(valueSet.Success, Is.True, valueSet.FailureMessage);
        Assert.That(valueSet.AssignmentSyntax?.ValueSetTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "{", "1", "|", "2", "}" }));
        Assert.That(objectClass.Success, Is.True, objectClass.FailureMessage);
        Assert.That(objectClass.AssignmentSyntax?.ObjectClassSyntax?.FieldSyntax[0]
            .DefaultTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(objectClass.AssignmentSyntax?.ObjectClassSyntax?.FieldSyntax[1]
            .DefaultValueSyntax?.Value.IntegerValue.ToString(), Is.EqualTo("7"));
        Assert.That(informationObject.Success, Is.True, informationObject.FailureMessage);
        Assert.That(informationObject.AssignmentSyntax?.ObjectSyntax?.FieldSettingSyntax[0]
            .TypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(informationObject.AssignmentSyntax?.ObjectSyntax?.FieldSettingSyntax[1]
            .ValueSyntax?.Value.IntegerValue.ToString(), Is.EqualTo("7"));
        Assert.That(objectSet.Success, Is.True, objectSet.FailureMessage);
        Assert.That(objectSet.AssignmentSyntax?.ObjectSetSyntax?.RootElement
            ?.ObjectSyntax?.ReferenceName, Is.EqualTo("itemValue"));
        Assert.That(objectClassActual.Success, Is.True,
            objectClassActual.FailureMessage);
        Assert.That(objectClassActual.AssignmentSyntax?.TypeSyntax?.ReferenceName,
            Is.EqualTo("ITEM-CLASS"));
        Assert.That(objectSetActual.Success, Is.True,
            objectSetActual.FailureMessage);
        Assert.That(objectSetActual.AssignmentSyntax?.ObjectSetSyntax?.RootElement
            ?.ObjectSyntax?.ReferenceName, Is.EqualTo("itemValue"));
    }

    [Test]
    public void SubstituterSplicesValueSetsIntoContainedSubtypeConstraints()
    {
        TargetFixture fixture = BuildTargets(
            "Constraints DEFINITIONS ::= BEGIN "
                + "ConstraintTemplate { INTEGER : Values } ::= INTEGER (Values) END");
        ParameterSubstitutionResult result = Substitute(
            fixture,
            "ConstraintTemplate { { 1 | 2 } }",
            "ConstraintTemplate",
            AsnParameterKind.Type,
            "Caller");
        ParsedConstraintSyntax constraint = result.AssignmentSyntax?.TypeSyntax
            ?.ConstraintSyntax.Single()
            ?? throw new InvalidOperationException();

        Assert.That(result.Success, Is.True, result.FailureMessage);
        Assert.That(constraint.SourceTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "(", "1", "|", "2", ")" }));
        Assert.That(constraint.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Union));
    }

    private static ParameterSubstitutionResult Substitute(
        TargetFixture fixture,
        string referenceSource,
        string targetName,
        AsnParameterKind kind,
        string callerModuleName)
    {
        SymbolTableEntry target = fixture.Table.Symbols.Single(item =>
            item.Name == targetName);
        AsnToken[] tokens = Lex(referenceSource);
        ParameterizedReferenceParseResult parsed = new ParameterizedParser(
            tokens,
            fixture.Diagnostics,
            false).ParseParameterizedReferenceSyntax(
                kind,
                target.FormalParameterSyntax);
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.ConsumedTokenCount, Is.EqualTo(tokens.Length));
        ParsedParameterizedReferenceSyntax reference = parsed.Syntax
            ?? throw new InvalidOperationException();
        reference.Target = target;
        reference.Validated = true;
        SymbolTable caller = new SymbolTable { ModuleName = callerModuleName };
        SymbolTableEntry declaration = new SymbolTableEntry
        {
            Name = "Use",
            QualifiedName = callerModuleName + ".Use"
        };
        return new ParameterizedSubstituter(fixture.Diagnostics)
            .SubstituteReference(reference, caller, declaration, fixture.Table);
    }

    private static TargetFixture BuildTargets(string source)
    {
        ModuleSyntax module = ParseModule("substitution.asn", source);
        DiagnosticBag diagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics)
            .BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult result = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        Assert.That(result.Success, Is.True, string.Join(" | ",
            diagnostics.GetDiagnostics().Select(item => item.Code + ":" + item.Message)));
        return new TargetFixture
        {
            Diagnostics = diagnostics,
            Table = tables[0]
        };
    }

    private static ModuleSyntax ParseModule(string sourceName, string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            sourceName,
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static AsnToken[] Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "caller.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        List<AsnToken> tokens = [];
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }

            Assert.That(result.Status, Is.EqualTo(IncrementalLexStatus.Token));
            AsnToken token = result.Token ?? throw new InvalidOperationException();
            if (token.Kind != AsnTokenKind.Whitespace
                && token.Kind != AsnTokenKind.Comment
                && token.Kind != AsnTokenKind.EndOfInput)
            {
                tokens.Add(token);
            }
        }

        Assert.That(session.GetDiagnostics(), Is.Empty);
        return tokens.ToArray();
    }

    private class TargetFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable Table = null!;
    }
}
