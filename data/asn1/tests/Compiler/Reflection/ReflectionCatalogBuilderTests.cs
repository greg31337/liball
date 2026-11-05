using asn1;

namespace asn1.Tests;

public class ReflectionCatalogBuilderTests
{
    [Test]
    public void BuilderPublishesTheCompleteLoweredSchemaGraph()
    {
        LoweredCompilationIr ir = Compile([
            "Library DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "EXPORTS Base, Template; "
                + "Base ::= SEQUENCE { count INTEGER (1..9) DEFAULT 3, "
                + "..., note UTF8String OPTIONAL }; "
                + "Template { T } ::= SEQUENCE OF T END",
            "Client DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "IMPORTS Base, Template FROM Library; "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type UTF8String }; "
                + "Items ITEM ::= { first, ..., second }; "
                + "Payload ::= [XER:ATTRIBUTE payload] ITEM.&Type ({Items}); "
                + "Use ::= Template { Base }; "
                + "answer INTEGER ::= 5 "
                + "ENCODING-CONTROL XER\nUse ATTRIBUTE\nEND"
        ]);
        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(ir);
        AsnModuleDescriptor library = catalog.Modules.Single(item =>
            item.Name == "Library");
        AsnModuleDescriptor client = catalog.Modules.Single(item =>
            item.Name == "Client");
        AsnAssignmentDescriptor baseAssignment = library.Assignments.Single(item =>
            item.Name == "Base");
        AsnTypeDescriptor baseType = baseAssignment.Type
            ?? throw new InvalidOperationException();
        AsnFieldDescriptor count = baseType.Fields.Single(item =>
            item.Name == "count");
        AsnFieldDescriptor note = baseType.Fields.Single(item =>
            item.Name == "note");
        AsnAssignmentDescriptor template = library.Assignments.Single(item =>
            item.Name == "Template");
        AsnAssignmentDescriptor payload = client.Assignments.Single(item =>
            item.Name == "Payload");
        AsnAssignmentDescriptor items = client.Assignments.Single(item =>
            item.Name == "Items");
        AsnObjectTableDescriptor table = payload.ObjectTables.Single();

        Assert.That(catalog.StableId, Is.EqualTo("reflection:ir:compilation"));
        Assert.That(catalog.Modules.Select(item => item.Name),
            Is.EqualTo(new[] { "Library", "Client" }));
        Assert.That(catalog.Assignments.Length,
            Is.EqualTo(library.Assignments.Length + client.Assignments.Length));
        Assert.That(library.GeneratedName, Is.EqualTo("Library"));
        Assert.That(library.Exports, Is.EqualTo(new[] { "Base", "Template" }));
        Assert.That(client.Imports.Single().ModuleName, Is.EqualTo("Library"));
        Assert.That(client.Imports.Single().Symbols,
            Is.EqualTo(new[] { "Base", "Template" }));
        Assert.That(client.Imports.Single().StableId, Is.Not.Empty);
        Assert.That(client.Imports.Single().Span?.Start.SourceName,
            Does.StartWith("reflection-"));
        Assert.That(baseAssignment.QualifiedName, Is.EqualTo("Library.Base"));
        Assert.That(baseAssignment.GeneratedName, Is.EqualTo("Base"));
        Assert.That(baseAssignment.SourceSpan?.Start.SourceName,
            Is.EqualTo("reflection-0.asn"));
        Assert.That(baseType.Kind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(baseType.GeneratedName, Is.EqualTo("Base"));
        Assert.That(baseType.Extensible, Is.True);
        Assert.That(baseType.RootFields, Is.EqualTo(new[] { count }));
        Assert.That(baseType.ExtensionFields, Is.EqualTo(new[] { note }));
        Assert.That(count.DefaultValue?.Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(3)));
        Assert.That(count.Tags.Single().AutomaticallyGenerated, Is.True);
        Assert.That(count.Tags.Single().Derivations.Select(item => item.Kind),
            Does.Contain(AsnDerivationKind.AutomaticTag));
        Assert.That(count.Type?.Constraints.Single().RootIntervals.Single()
            .LowerBound, Is.EqualTo(new System.Numerics.BigInteger(1)));
        Assert.That(count.Type?.Constraints.Single().RootIntervals.Single()
            .UpperBound, Is.EqualTo(new System.Numerics.BigInteger(9)));
        Assert.That(note.Optional, Is.True);
        Assert.That(note.Extension, Is.True);
        Assert.That(template.Parameterized, Is.True);
        Assert.That(template.Parameters.Single().Name, Is.EqualTo("T"));
        Assert.That(template.Parameters.Single().GeneratedName, Is.EqualTo("T"));
        Assert.That(table.ObjectClassName, Is.EqualTo("ITEM"));
        Assert.That(table.UniqueFieldName, Is.EqualTo("&Type"));
        Assert.That(table.Entries.Select(item => item.Key?.Value?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        Assert.That(table.Entries.Select(item => item.Type?.Kind),
            Is.EqualTo(new[] { AsnTypeKind.Integer, AsnTypeKind.Utf8String }));
        Assert.That(items.ObjectSet?.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(items.ObjectSet?.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));
        Assert.That(client.Assignments.Single(item => item.Name == "ITEM")
            .ObjectClass?.Fields.Select(item => item.Name),
            Is.EqualTo(new[] { "&id", "&Type" }));
        Assert.That(client.Values.Single(item => item.Name == "answer")
            .Value?.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(5)));
        Assert.That(payload.Type?.Encodings.Single().EncodingReference,
            Is.EqualTo("XER"));
        Assert.That(payload.Type?.Encodings.Single().Plan?.Steps.Single().Path,
            Is.EqualTo("payload"));
        Assert.That(client.Encodings.Single().EncodingRules,
            Is.EqualTo(AsnEncodingRules.ExtendedXer));
        Assert.That(client.Encodings.Single().Plan?.Steps.Single().Path,
            Is.EqualTo("ATTRIBUTE"));
        Assert.That(client.SourceModule?.Assignments.Length,
            Is.EqualTo(client.Assignments.Length));
        Assert.That(client.SourceModule?.Imports.Single(),
            Is.SameAs(client.Imports.Single()));
    }

    [Test]
    public void BuilderPreservesRecursiveDescriptorIdentity()
    {
        LoweredTypeIr recursive = new LoweredTypeIr
        {
            Name = "Node",
            Kind = AsnTypeKind.Sequence
        };
        LoweredFieldIr next = new LoweredFieldIr
        {
            Name = "next",
            Type = recursive,
            Optional = true
        };
        recursive.Fields = [next];
        recursive.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = [next]
        };
        LoweredCompilationIr ir = new LoweredCompilationIr
        {
            Modules = [new LoweredModuleIr
            {
                Name = "Recursive",
                Assignments = [new LoweredAssignmentIr
                {
                    Name = "Node",
                    QualifiedName = "Recursive.Node",
                    Kind = AsnAssignmentKind.Type,
                    Type = recursive
                }]
            }]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(ir);

        AsnTypeDescriptor descriptor = new ReflectionCatalogBuilder()
            .BuildCatalog(ir).Modules[0].Types.Single();

        Assert.That(descriptor.Fields[0].Type, Is.SameAs(descriptor));
        Assert.That(descriptor.RootFields[0], Is.SameAs(descriptor.Fields[0]));
        Assert.That(descriptor.SourceType?.Components[0].Type,
            Is.SameAs(descriptor.SourceType));
    }

    [Test]
    public void BuilderRetainsPerExtensionPartitionsAndGroups()
    {
        LoweredCompilationIr ir = Compile(
        [
            "PerSchema DEFINITIONS ::= BEGIN "
                + "Status ::= ENUMERATED { ready, ..., retired } "
                + "Grouped ::= SEQUENCE { ..., "
                + "[[ first BOOLEAN OPTIONAL, second BOOLEAN OPTIONAL ]] } "
                + "END"
        ]);

        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(ir);
        AsnTypeDescriptor status = catalog.Modules[0].Types.Single(
            item => item.Name == "Status");
        AsnTypeDescriptor grouped = catalog.Modules[0].Types.Single(
            item => item.Name == "Grouped");

        Assert.Multiple(() =>
        {
            Assert.That(status.RootNamedValues.Select(item => item.Name),
                Is.EqualTo(new[] { "ready" }));
            Assert.That(status.ExtensionNamedValues.Select(item => item.Name),
                Is.EqualTo(new[] { "retired" }));
            Assert.That(grouped.ExtensionFields.Select(
                item => item.ExtensionGroupIndex),
                Is.EqualTo(new[] { 0, 0 }));
        });
    }

    private static LoweredCompilationIr Compile(string[] sources)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        ModuleSyntax[] modules = new ModuleSyntax[sources.Length];
        int index = 0;
        while (index < sources.Length)
        {
            IncrementalLexSession session = new IncrementalLexSession(
                "reflection-" + index.ToString() + ".asn",
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
        Assert.That(new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph).Success, Is.True,
            FormatDiagnostics(diagnostics));
        Assert.That(new TypeResolver(diagnostics).ResolveTypes(tables).Success,
            Is.True, FormatDiagnostics(diagnostics));
        Assert.That(new ValueResolver(diagnostics).ResolveValues(tables).Success,
            Is.True, FormatDiagnostics(diagnostics));
        Assert.That(new TagResolver(diagnostics).ResolveTags(tables).Success,
            Is.True, FormatDiagnostics(diagnostics));
        Assert.That(new AutomaticTagger(diagnostics).ApplyAutomaticTags(tables)
            .Success, Is.True, FormatDiagnostics(diagnostics));
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
