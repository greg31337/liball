using asn1;

namespace asn1.Tests;

public class DeterministicIrIdentityTests
{
    [Test]
    public void StableIdsAndTopologicalOrderIgnoreInputOrderAndCulture()
    {
        string providerFirst = "Zulu DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
            + "EXPORTS Container; "
            + "Dependent ::= Local; Independent ::= BOOLEAN; Local ::= INTEGER; "
            + "Container ::= SEQUENCE { local Local } END";
        string providerReordered = "Zulu DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
            + "EXPORTS Container; "
            + "Local ::= INTEGER; Container ::= SEQUENCE { local Local }; "
            + "Independent ::= BOOLEAN; Dependent ::= Local END";
        string consumer = "Alpha DEFINITIONS ::= BEGIN "
            + "IMPORTS Container FROM Zulu; Use ::= Container END";
        LoweredCompilationIr first = Compile([providerFirst, consumer]);
        LoweredCompilationIr repeated = Compile([providerFirst, consumer]);
        System.Globalization.CultureInfo originalCulture =
            System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture =
            System.Globalization.CultureInfo.CurrentUICulture;
        LoweredCompilationIr reordered;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                new System.Globalization.CultureInfo("tr-TR");
            System.Globalization.CultureInfo.CurrentUICulture =
                new System.Globalization.CultureInfo("tr-TR");
            reordered = Compile([consumer, providerReordered]);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }

        string[] firstSnapshot = Snapshot(first);
        Assert.That(first.Modules.Select(item => item.Name),
            Is.EqualTo(new[] { "Zulu", "Alpha" }));
        Assert.That(first.Modules[0].Assignments.Select(item => item.Name),
            Is.EqualTo(new[]
            {
                "Independent",
                "Local",
                "Container",
                "Dependent"
            }));
        Assert.That(first.Modules.Select(item => item.EmissionOrder),
            Is.EqualTo(new[] { 0, 1 }));
        Assert.That(first.Modules[0].Assignments.Select(item => item.EmissionOrder),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(Snapshot(repeated), Is.EqualTo(firstSnapshot));
        Assert.That(Snapshot(reordered), Is.EqualTo(firstSnapshot));
        Assert.That(firstSnapshot.Distinct().Count(), Is.EqualTo(firstSnapshot.Length));
        Assert.That(first.Modules[0].StableId,
            Is.EqualTo("ir:module/Zulu;oid=;iri=;version-module=;version="));
        Assert.That(first.Modules[0].Assignments[1].StableId,
            Does.EndWith("/assignment/1/Local"));
        Assert.That(first.Modules[0].Assignments[2].Type?.Fields[0].StableId,
            Does.Contain("/field/local/0"));
    }

    [Test]
    public void StableIdEscapingUsesPortableOrdinalCodeUnits()
    {
        DeterministicIrIdentityAssigner assigner =
            new DeterministicIrIdentityAssigner();

        Assert.That(assigner.EscapeStablePart("A/B:\u0130"),
            Is.EqualTo("A%002FB%003A%0130"));
        Assert.That(assigner.EscapeStablePart("simple-Name_9.0"),
            Is.EqualTo("simple-Name_9.0"));
    }

    private static string[] Snapshot(LoweredCompilationIr compilation)
    {
        System.Collections.Generic.List<string> ids =
            new System.Collections.Generic.List<string>();
        ids.Add(compilation.StableId);
        int moduleIndex = 0;
        while (moduleIndex < compilation.Modules.Length)
        {
            LoweredModuleIr module = compilation.Modules[moduleIndex];
            ids.Add(module.StableId);
            int importIndex = 0;
            while (importIndex < module.Imports.Length)
            {
                ids.Add(module.Imports[importIndex].StableId);
                importIndex = importIndex + 1;
            }

            int assignmentIndex = 0;
            while (assignmentIndex < module.Assignments.Length)
            {
                LoweredAssignmentIr assignment = module.Assignments[assignmentIndex];
                ids.Add(assignment.StableId);
                if (assignment.Type != null)
                {
                    AddTypeIds(ids, assignment.Type);
                }

                assignmentIndex = assignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return ids.ToArray();
    }

    private static void AddTypeIds(
        System.Collections.Generic.List<string> ids,
        LoweredTypeIr type)
    {
        if (ids.Contains(type.StableId))
        {
            return;
        }

        ids.Add(type.StableId);
        int tagIndex = 0;
        while (tagIndex < type.Tags.Length)
        {
            ids.Add(type.Tags[tagIndex].StableId);
            tagIndex = tagIndex + 1;
        }

        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            LoweredFieldIr field = type.Fields[fieldIndex];
            ids.Add(field.StableId);
            int fieldTagIndex = 0;
            while (fieldTagIndex < field.Tags.Length)
            {
                ids.Add(field.Tags[fieldTagIndex].StableId);
                fieldTagIndex = fieldTagIndex + 1;
            }

            if (field.Type != null)
            {
                AddTypeIds(ids, field.Type);
            }

            fieldIndex = fieldIndex + 1;
        }

        if (type.ElementType != null)
        {
            AddTypeIds(ids, type.ElementType);
        }
    }

    private static LoweredCompilationIr Compile(string[] sources)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        ModuleSyntax[] modules = new ModuleSyntax[sources.Length];
        int index = 0;
        while (index < sources.Length)
        {
            IncrementalLexSession session = new IncrementalLexSession(
                "deterministic-" + index.ToString() + ".asn",
                false,
                new AsnResourceLimits());
            session.SupplyTextChunk(sources[index], true);
            ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
                .ParseModule();
            Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
            Assert.That(session.GetDiagnostics(), Is.Empty);
            modules[index] = parsed.Module ?? throw new InvalidOperationException();
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
        Assert.That(classification.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(types.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(values.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(tags.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(automatic.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(diagnostics));
        return new EncodingNeutralIrLowerer().LowerCompilation(tables);
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
            item.Code + ":" + item.Message));
    }
}
