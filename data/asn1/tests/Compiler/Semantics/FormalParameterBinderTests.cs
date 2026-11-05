using asn1;

namespace asn1.Tests;

public class FormalParameterBinderTests
{
    [Test]
    public void BinderInfersKindsFromForwardGlobalAndDummyGovernors()
    {
        ModuleSyntax module = ParseModule(
            "parameters.asn",
            "Parameters DEFINITIONS ::= BEGIN "
                + "Template { LaterType : value, LaterType : Values, "
                + "LATER-CLASS : object, LATER-CLASS : Objects } ::= INTEGER; "
                + "Forward { Later : item, Later } ::= "
                + "SEQUENCE { value Later DEFAULT item }; "
                + "FORWARD-CLASS { LATER-GOV : item, LATER-GOV } ::= "
                + "CLASS { &item LATER-GOV DEFAULT item }; "
                + "LaterType ::= INTEGER; "
                + "LATER-CLASS ::= CLASS { &id INTEGER } END");
        BindingFixture fixture = Bind([module]);
        SymbolTableEntry template = RequireSymbol(fixture.Tables[0], "Template");
        SymbolTableEntry forward = RequireSymbol(fixture.Tables[0], "Forward");
        SymbolTableEntry forwardClass = RequireSymbol(
            fixture.Tables[0],
            "FORWARD-CLASS");

        Assert.That(template.FormalParameters.Select(item => item.Kind),
            Is.EqualTo(new[]
            {
                AsnParameterKind.Value,
                AsnParameterKind.ValueSet,
                AsnParameterKind.Object,
                AsnParameterKind.ObjectSet
            }));
        Assert.That(template.FormalParameters[0].GovernorType?.Name,
            Is.EqualTo("LaterType"));
        Assert.That(template.FormalParameters[2].GovernorObjectClass?.Name,
            Is.EqualTo("LATER-CLASS"));
        Assert.That(forward.FormalParameters.Select(item => item.Kind),
            Is.EqualTo(new[] { AsnParameterKind.Value, AsnParameterKind.Type }));
        Assert.That(forward.FormalParameterSyntax[0].DummyGovernor,
            Is.SameAs(forward.FormalParameterSyntax[1]));
        Assert.That(forwardClass.FormalParameters.Select(item => item.Kind),
            Is.EqualTo(new[]
            {
                AsnParameterKind.Object,
                AsnParameterKind.ObjectClass
            }));
        Assert.That(forwardClass.FormalParameterSyntax[0].DummyGovernor,
            Is.SameAs(forwardClass.FormalParameterSyntax[1]));
        Assert.That(fixture.Result.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void BinderAppliesDummyScopeAndPreservesQualifiedReferences()
    {
        ModuleSyntax provider = ParseModule(
            "provider.asn",
            "Provider DEFINITIONS ::= BEGIN External ::= INTEGER END");
        ModuleSyntax consumer = ParseModule(
            "consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN "
                + "IMPORTS External FROM Provider; "
                + "Template { External, External : value } ::= SEQUENCE { "
                + "local External, qualified Provider.External, "
                + "number INTEGER DEFAULT value } END");
        BindingFixture fixture = Bind([provider, consumer]);
        SymbolTableEntry template = RequireSymbol(fixture.Tables[1], "Template");
        SymbolTableEntry providerType = RequireSymbol(fixture.Tables[0], "External");

        Assert.That(template.FormalParameterSyntax[0].ReferenceTokens,
            Has.Length.EqualTo(2));
        Assert.That(template.FormalParameterSyntax[0].ReferenceTokens
            .All(item => item.Text == "External"), Is.True);
        Assert.That(template.FormalParameterSyntax[1].DummyGovernor,
            Is.SameAs(template.FormalParameterSyntax[0]));
        Assert.That(fixture.Graph.AssignmentEdges.Count(edge =>
            edge.Source?.Symbol == template && edge.Target?.Symbol == providerType),
            Is.EqualTo(1));
        Assert.That(fixture.Result.Success, Is.True, FormatDiagnostics(fixture));

        TypeResolutionResult types = new TypeResolver(fixture.Diagnostics)
            .ResolveTypes(fixture.Tables);
        Assert.That(types.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(template.ResolvedType?.Components[0].TypeResolution
            ?.ParameterReference, Is.True);
        Assert.That(template.ResolvedType?.Components[1].TypeResolution
            ?.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
    }

    [Test]
    public void BinderKeepsAssignmentGovernorOutsideDummyScope()
    {
        ModuleSyntax module = ParseModule(
            "scope.asn",
            "Scope DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER; item { Base } Base ::= 1 END");
        BindingFixture fixture = Bind([module]);
        SymbolTableEntry item = RequireSymbol(fixture.Tables[0], "item");
        SymbolTableEntry basis = RequireSymbol(fixture.Tables[0], "Base");

        Assert.That(item.Kind, Is.EqualTo(AsnAssignmentKind.Value));
        Assert.That(item.FormalParameters.Single().Kind,
            Is.EqualTo(AsnParameterKind.Type));
        Assert.That(fixture.Graph.AssignmentEdges.Any(edge =>
            edge.Source?.Symbol == item && edge.Target?.Symbol == basis), Is.True);
        Assert.That(fixture.Result.Success, Is.True, FormatDiagnostics(fixture));
    }

    [Test]
    public void BinderResolvesSingleLetterTypeDummyInNestedTypePosition()
    {
        ModuleSyntax module = ParseModule(
            "single-letter.asn",
            "SingleLetter DEFINITIONS ::= BEGIN "
                + "Options { T } ::= CHOICE { open T, number INTEGER } END");
        BindingFixture fixture = Bind([module]);
        SymbolTableEntry options = RequireSymbol(fixture.Tables[0], "Options");

        Assert.That(options.FormalParameters.Single().Kind,
            Is.EqualTo(AsnParameterKind.Type), FormatDiagnostics(fixture));
        TypeResolutionResult types = new TypeResolver(fixture.Diagnostics)
            .ResolveTypes(fixture.Tables);

        Assert.That(types.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(options.ResolvedType?.Components[0].TypeResolution
            ?.ParameterReference, Is.True);
    }

    [Test]
    public void BinderRejectsDuplicateInvalidAndUnknownFormalGovernors()
    {
        ModuleSyntax module = ParseModule(
            "invalid-parameters.asn",
            "InvalidParameters DEFINITIONS ::= BEGIN "
                + "Duplicate { T, T } ::= INTEGER; "
                + "Invalid { INTEGER : Values, Values : item } ::= INTEGER; "
                + "BadDummy { item } ::= INTEGER; "
                + "Unknown { Missing : item } ::= INTEGER END");
        BindingFixture fixture = Bind([module]);
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code)
            .ToArray();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(codes, Does.Contain("ASNB0060"));
        Assert.That(codes, Does.Contain("ASNB0061"));
        Assert.That(codes, Does.Contain("ASNB0062"));
        Assert.That(codes, Does.Contain("ASNB0063"));
        Assert.That(fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNB0060"
                || item.Code == "ASNB0061"
                || item.Code == "ASNB0062"
                || item.Code == "ASNB0063")
            .All(item => item.Span != null), Is.True);
    }

    private static BindingFixture Bind(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult result = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        return new BindingFixture
        {
            Diagnostics = diagnostics,
            Graph = graph,
            Result = result,
            Tables = tables
        };
    }

    private static SymbolTableEntry RequireSymbol(SymbolTable table, string name)
    {
        return table.Symbols.Single(item => item.Name == name);
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

    private static string FormatDiagnostics(BindingFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class BindingFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal CompilationGraph Graph = null!;
        internal AssignmentClassificationResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
