using asn1;

namespace asn1.Tests;

public class ParameterInstantiationIntegrationTests
{
    [Test]
    public void ImportedInstantiationFeedsComponentsTagsConstraintsAndInstructions()
    {
        ResolutionFixture fixture = Resolve([
            "Library DEFINITIONS ::= BEGIN EXPORTS Template; "
                + "Template { INTEGER : target, T } ::= "
                + "[XER:ATTRIBUTE target] SEQUENCE { "
                + "COMPONENTS OF T, count INTEGER (4..9) } END",
            "Client DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "IMPORTS Template FROM Library; "
                + "selected INTEGER ::= 4; "
                + "Parts ::= SEQUENCE { enabled BOOLEAN }; "
                + "Use ::= Template { selected, Parts } END"
        ]);
        ResolvedTypeSyntax use = RequireType(fixture, 1, "Use");
        ResolvedTypeSyntax sequence = use.InnerType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax count = sequence.Components[1].TypeResolution
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Tags.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.AutomaticTags.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(use.EffectiveKind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(use.SourceSyntax?.EncodingInstructions.Single().Kind,
            Is.EqualTo(AsnInstructionKind.Attribute));
        Assert.That(Text(use.SourceSyntax?.EncodingInstructionTokens ?? []),
            Is.EqualTo("ATTRIBUTE selected"));
        Assert.That(sequence.Components.Select(item => item.Component?.Name),
            Is.EqualTo(new[] { "enabled", "count" }));
        Assert.That(sequence.AutomaticTaggingApplied, Is.True);
        Assert.That(sequence.Components.Select(item =>
                item.AutomaticTag?.Tag.Number.ToString()),
            Is.EqualTo(new[] { "0", "1" }));
        Assert.That(count.IntegerConstraints?.Root?.Contains(
            new System.Numerics.BigInteger(4)), Is.True);
        Assert.That(count.IntegerConstraints?.Root?.Contains(
            new System.Numerics.BigInteger(3)), Is.False);
        Assert.That(fixture.Tables[1].FindSymbol("Use", AsnAssignmentKind.Type)
            ?.ParameterizedReferences.Single().Target?.QualifiedName,
            Is.EqualTo("Library.Template"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatedObjectsAndSetsFeedOpenTypeDispatchTables()
    {
        ResolutionFixture fixture = Resolve([
            "Objects DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "firstTemplate { T } ITEM ::= { &id 1, &Type T }; "
                + "secondTemplate { T } ITEM ::= { &id 2, &Type T }; "
                + "SetTemplate { ITEM : Objects } ITEM ::= { Objects }; "
                + "first ITEM ::= firstTemplate { INTEGER }; "
                + "second ITEM ::= secondTemplate { UTF8String }; "
                + "Items ITEM ::= SetTemplate { { first | second } }; "
                + "Payload ::= ITEM.&Type ({Items}) END"
        ]);
        ResolvedTypeSyntax payload = RequireType(fixture, 0, "Payload");
        TableConstraintRelationship relationship = payload.TableConstraints.Single();

        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(relationship.DispatchEntries.Select(item => item.Key?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        Assert.That(relationship.DispatchEntries.Select(item => item.Type?.Kind),
            Is.EqualTo(new[] { AsnTypeKind.Integer, AsnTypeKind.Utf8String }));
        Assert.That(relationship.DispatchMap?.Buckets, Has.Length.EqualTo(2));
        Assert.That(fixture.Tables[0].FindSymbol("first", AsnAssignmentKind.Object)
            ?.DeclaredObjectSyntax?.FieldSettingSyntax, Has.Length.EqualTo(2));
        Assert.That(fixture.Tables[0].FindSymbol("Items", AsnAssignmentKind.ObjectSet)
            ?.DeclaredObjectSetSyntax?.RootMembers, Has.Length.EqualTo(2));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    private static ResolutionFixture Resolve(string[] sources)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        ModuleSyntax[] modules = new ModuleSyntax[sources.Length];
        int index = 0;
        while (index < sources.Length)
        {
            modules[index] = ParseModule(
                "parameter-integration-" + index.ToString() + ".asn",
                sources[index]);
            index = index + 1;
        }

        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult classification =
            new AssignmentClassifier(diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        TagResolutionResult tags = new TagResolver(diagnostics).ResolveTags(tables);
        AutomaticTaggingResult automatic = new AutomaticTagger(diagnostics)
            .ApplyAutomaticTags(tables);
        Assert.That(classification.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
                item.Code + ":" + item.Message)));
        Assert.That(types.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
                item.Code + ":" + item.Message)));
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values,
            Tags = tags,
            AutomaticTags = automatic
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

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        int moduleIndex,
        string name)
    {
        return fixture.Tables[moduleIndex].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType ?? throw new InvalidOperationException();
    }

    private static string Text(AsnToken[] tokens)
    {
        return string.Join(" ", tokens.Select(item => item.Text));
    }

    private static string FormatDiagnostics(ResolutionFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item =>
            item.Code + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
        internal TagResolutionResult Tags = null!;
        internal AutomaticTaggingResult AutomaticTags = null!;
    }
}
