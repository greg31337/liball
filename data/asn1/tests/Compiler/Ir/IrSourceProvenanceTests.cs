using asn1;

namespace asn1.Tests;

public class IrSourceProvenanceTests
{
    [Test]
    public void LowererPreservesAliasComponentParameterTagAndControlChains()
    {
        string source = "Trace DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
            + "Base ::= SEQUENCE { original INTEGER };\n"
            + "Alias ::= Base;\n"
            + "Template { T } ::= SEQUENCE { COMPONENTS OF T, extra BOOLEAN };\n"
            + "Use ::= Template { Alias }\n"
            + "ENCODING-CONTROL XER\n"
            + "Use ATTRIBUTE\n"
            + "END";
        LoweringFixture fixture = Compile("trace.asn", source);
        LoweredModuleIr module = new EncodingNeutralIrLowerer()
            .LowerCompilation(fixture.Tables).Modules.Single();
        LoweredAssignmentIr alias = RequireAssignment(module, "Alias");
        LoweredAssignmentIr use = RequireAssignment(module, "Use");
        LoweredTypeIr useType = use.Type ?? throw new InvalidOperationException();
        LoweredTypeIr sequence = useType.Fields.Length == 0
            ? useType.ElementType ?? throw new InvalidOperationException()
            : useType;
        LoweredFieldIr original = sequence.Fields.Single(item =>
            item.Name == "original");
        LoweredTagIr automaticTag = original.Tags.Single();
        LoweredInstructionIr control = module.Instructions.Single();

        Assert.That(alias.SourceSpan?.Start.SourceName, Is.EqualTo("trace.asn"));
        Assert.That(alias.Type?.Derivations.Select(item => item.Kind),
            Does.Contain(LoweredDerivationKind.Alias));
        Assert.That(alias.Type?.Derivations.Single(item =>
            item.Kind == LoweredDerivationKind.Alias).Name,
            Is.EqualTo("Trace.Base"));
        Assert.That(use.Derivations.Select(item => item.Kind),
            Does.Contain(LoweredDerivationKind.ParameterInstantiation));
        Assert.That(sequence.Derivations[0].Kind,
            Is.EqualTo(LoweredDerivationKind.ParameterInstantiation));
        Assert.That(original.Derivations.Select(item => item.Kind),
            Is.EqualTo(new[]
            {
                LoweredDerivationKind.ParameterInstantiation,
                LoweredDerivationKind.ComponentsOf
            }));
        Assert.That(original.SourceSpan?.Start.Offset,
            Is.EqualTo(source.IndexOf("original", StringComparison.Ordinal)));
        Assert.That(automaticTag.AutomaticallyGenerated, Is.True);
        Assert.That(automaticTag.Derivations.Select(item => item.Kind),
            Is.EqualTo(new[]
            {
                LoweredDerivationKind.ParameterInstantiation,
                LoweredDerivationKind.AutomaticTag
            }));
        Assert.That(control.Derivations.Select(item => item.Kind),
            Is.EqualTo(new[] { LoweredDerivationKind.EncodingControl }));
        Assert.That(control.SourceSpan?.Start.SourceName, Is.EqualTo("trace.asn"));
        Assert.That(AllDerivationIdsAreAssigned(alias.Type?.Derivations ?? []),
            Is.True);
        Assert.That(AllDerivationIdsAreAssigned(original.Derivations), Is.True);
        Assert.That(AllDerivationIdsAreAssigned(automaticTag.Derivations), Is.True);
        Assert.That(AllDerivationIdsAreAssigned(control.Derivations), Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void LowererPreservesExplicitXsdMappingsOnGeneratedNodes()
    {
        string generated = "Mapped DEFINITIONS ::= BEGIN\nData ::= INTEGER\nEND";
        ModuleSyntax module = ParseModule("generated.asn", generated);
        AsnSourceUnit xsd = new AsnSourceUnit
        {
            Name = "schema.xsd",
            Text = "<xs:element name=\"Data\"/>"
        };
        AsnSourceSpan sourceSpan = xsd.GetSpan(0, xsd.Text.Length);
        module.XsdSourceMappings = [new XsdSourceMappingSyntax
        {
            ComponentName = "{urn:example}Data",
            GeneratedSpan = module.Span,
            SourceSpan = sourceSpan
        }];
        LoweringFixture fixture = CompileModule(module);
        LoweredModuleIr lowered = new EncodingNeutralIrLowerer()
            .LowerCompilation(fixture.Tables).Modules.Single();
        LoweredAssignmentIr data = RequireAssignment(lowered, "Data");
        LoweredTypeIr dataType = data.Type ?? throw new InvalidOperationException();
        LoweredDerivationIr assignmentMapping = data.Derivations.Single(item =>
            item.Kind == LoweredDerivationKind.XsdMapping);
        LoweredDerivationIr typeMapping = dataType.Derivations.Single(item =>
            item.Kind == LoweredDerivationKind.XsdMapping);

        Assert.That(data.SourceSpan?.Start.SourceName, Is.EqualTo("generated.asn"));
        Assert.That(assignmentMapping.Name, Is.EqualTo("{urn:example}Data"));
        Assert.That(assignmentMapping.Span?.Start.SourceName,
            Is.EqualTo("schema.xsd"));
        Assert.That(typeMapping.Span?.End.Offset, Is.EqualTo(xsd.Text.Length));
        Assert.That(assignmentMapping.StableId, Does.StartWith(data.StableId));
        Assert.That(typeMapping.StableId, Does.StartWith(dataType.StableId));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    private static bool AllDerivationIdsAreAssigned(
        LoweredDerivationIr[] derivations)
    {
        return derivations.All(item => item.StableId.Length > 0);
    }

    private static LoweredAssignmentIr RequireAssignment(
        LoweredModuleIr module,
        string name)
    {
        return module.Assignments.Single(item => item.Name == name);
    }

    private static LoweringFixture Compile(string sourceName, string source)
    {
        return CompileModule(ParseModule(sourceName, source));
    }

    private static LoweringFixture CompileModule(ModuleSyntax module)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics)
            .BuildTables([module]);
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
        Assert.That(classification.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(types.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(values.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(tags.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(automatic.Success, Is.True, FormatDiagnostics(diagnostics));
        return new LoweringFixture
        {
            Diagnostics = diagnostics,
            Tables = tables
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

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
            item.Code + ":" + item.Message));
    }

    private class LoweringFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
    }
}
