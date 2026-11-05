using asn1;

namespace asn1.Tests;

public class EncodingNeutralIrLowererTests
{
    [Test]
    public void LowererCapturesTheResolvedEncodingNeutralSchema()
    {
        LoweringFixture fixture = Compile(
            "Ir DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type };\n"
                + "first ITEM ::= { &id 1, &Type INTEGER };\n"
                + "second ITEM ::= { &id 2, &Type UTF8String };\n"
                + "Items ITEM ::= { first, ..., second };\n"
                + "Base ::= SEQUENCE { count INTEGER (1..9) DEFAULT 3, "
                + "..., note UTF8String OPTIONAL };\n"
                + "Payload ::= [XER:ATTRIBUTE payload] ITEM.&Type ({Items});\n"
                + "answer INTEGER ::= 5;\n"
                + "bits BIT STRING ::= 'A0'H;\n"
                + "algorithm OBJECT IDENTIFIER ::= { 1 2 840 113549 };\n"
                + "Digits INTEGER ::= { 1..9 }\n"
                + "ENCODING-CONTROL XER\nBase ATTRIBUTE\nEND");
        LoweredCompilationIr compilation = new EncodingNeutralIrLowerer()
            .LowerCompilation(fixture.Tables);
        LoweredModuleIr module = compilation.Modules.Single();
        LoweredAssignmentIr baseAssignment = RequireAssignment(module, "Base");
        LoweredTypeIr baseType = baseAssignment.Type
            ?? throw new InvalidOperationException();
        LoweredFieldIr count = baseType.Fields[0];
        LoweredFieldIr note = baseType.Fields[1];
        LoweredAssignmentIr payloadAssignment = RequireAssignment(module, "Payload");
        LoweredTypeIr payload = payloadAssignment.Type
            ?? throw new InvalidOperationException();
        LoweredObjectTableIr table = payload.ObjectTables.Single();

        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Tags.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.AutomaticTags.Success, Is.True,
            FormatDiagnostics(fixture));
        Assert.That(module.Name, Is.EqualTo("Ir"));
        Assert.That(module.StableId, Is.Not.Empty);
        Assert.That(module.TagDefault, Is.EqualTo(AsnTaggingMode.Automatic));
        Assert.That(module.Assignments.Select(item => item.Kind), Does.Contain(
            AsnAssignmentKind.ObjectClass));
        Assert.That(module.Assignments.Select(item => item.Kind), Does.Contain(
            AsnAssignmentKind.Object));
        Assert.That(module.Assignments.Select(item => item.Kind), Does.Contain(
            AsnAssignmentKind.ObjectSet));
        Assert.That(module.Assignments.Select(item => item.Kind), Does.Contain(
            AsnAssignmentKind.ValueSet));
        Assert.That(baseType.Kind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(baseAssignment.StableId, Is.Not.Empty);
        Assert.That(baseType.StableId, Is.Not.Empty);
        Assert.That(baseType.Fields.Select(item => item.Name),
            Is.EqualTo(new[] { "count", "note" }));
        Assert.That(count.Tags.Single().AutomaticallyGenerated, Is.True);
        Assert.That(count.StableId, Is.Not.Empty);
        Assert.That(count.Tags.Single().StableId, Is.Not.Empty);
        Assert.That(count.Tags.Single().Number.ToString(), Is.EqualTo("0"));
        Assert.That(count.HasDefaultValue, Is.True);
        Assert.That(count.DefaultValue?.IntegerValue.ToString(), Is.EqualTo("3"));
        Assert.That(count.DefaultValue?.StableId, Is.Not.Empty);
        Assert.That(note.Optional, Is.True);
        Assert.That(baseType.Extensions.Extensible, Is.True);
        Assert.That(baseType.Extensions.StableId, Is.Not.Empty);
        Assert.That(baseType.Extensions.RootFields.Select(item => item.Name),
            Is.EqualTo(new[] { "count" }));
        Assert.That(baseType.Extensions.AdditionalFields.Select(item => item.Name),
            Is.EqualTo(new[] { "note" }));
        Assert.That(ContainsInteger(baseType.Fields[0].Type, 1), Is.True);
        Assert.That(ContainsInteger(baseType.Fields[0].Type, 9), Is.True);
        Assert.That(ContainsInteger(baseType.Fields[0].Type, 10), Is.False);
        Assert.That(baseType.Fields[0].Type?.Constraints.Single().StableId,
            Is.Not.Empty);
        Assert.That(payload.Instructions.Single().EncodingReference,
            Is.EqualTo("XER"));
        Assert.That(payload.Instructions.Single().Name, Is.EqualTo("ATTRIBUTE"));
        Assert.That(payload.Instructions.Single().StableId, Is.Not.Empty);
        Assert.That(payload.Instructions.Single().Targets,
            Is.EqualTo(new[] { "payload" }));
        Assert.That(table.Kind, Is.EqualTo(AsnConstraintKind.SimpleTable));
        Assert.That(table.StableId, Is.Not.Empty);
        Assert.That(table.ObjectClassName, Is.EqualTo("ITEM"));
        Assert.That(table.Entries.Select(item => item.Key?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        Assert.That(table.Entries.Select(item => item.Type?.Kind),
            Is.EqualTo(new[] { AsnTypeKind.Integer, AsnTypeKind.Utf8String }));
        Assert.That(table.Entries.All(item => item.StableId.Length > 0), Is.True);
        Assert.That(RequireAssignment(module, "Items").ObjectSet?.Extensible,
            Is.True);
        Assert.That(RequireAssignment(module, "Items").ObjectSet?.StableId,
            Is.Not.Empty);
        Assert.That(RequireAssignment(module, "Items").ObjectSet
            ?.RootObjects.Select(item => item.Name), Is.EqualTo(new[] { "first" }));
        Assert.That(RequireAssignment(module, "Items").ObjectSet
            ?.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));
        Assert.That(RequireAssignment(module, "answer").Value
            ?.IntegerValue.ToString(), Is.EqualTo("5"));
        Assert.That(RequireAssignment(module, "answer").Value?.StableId,
            Is.Not.Empty);
        Assert.That(RequireAssignment(module, "bits").Value?.BitStringValue
            ?.Bytes, Is.EqualTo(new byte[] { 0xa0 }));
        Assert.That(RequireAssignment(module, "bits").Value?.BitStringValue
            ?.BitLength, Is.EqualTo(8));
        Assert.That(RequireAssignment(module, "algorithm").Value
            ?.ObjectIdentifierValue?.Arcs,
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2, 840, 113549 }));
        Assert.That(RequireAssignment(module, "ITEM").ObjectClass
            ?.Fields.Select(item => item.Name),
            Is.EqualTo(new[] { "&id", "&Type" }));
        Assert.That(RequireAssignment(module, "ITEM").ObjectClass?.StableId,
            Is.Not.Empty);
        Assert.That(RequireAssignment(module, "ITEM").ObjectClass?.Fields
            .All(item => item.StableId.Length > 0), Is.True);
        Assert.That(module.Instructions.Single().EncodingReference,
            Is.EqualTo("XER"));
        Assert.That(module.Instructions.Single().Name, Is.EqualTo("Base"));
        Assert.That(module.Instructions.Single().StableId, Is.Not.Empty);
        Assert.That(module.Instructions.Single().Targets,
            Is.EqualTo(new[] { "ATTRIBUTE" }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    private static bool ContainsInteger(LoweredTypeIr? type, int value)
    {
        if (type == null || type.Constraints.Length == 0)
        {
            return false;
        }

        return ContainsIntegerConstraint(
            type.Constraints[0],
            new System.Numerics.BigInteger(value));
    }

    private static bool ContainsIntegerConstraint(
        LoweredConstraintIr constraint,
        System.Numerics.BigInteger value)
    {
        int index = 0;
        while (index < constraint.RootIntervals.Length)
        {
            LoweredIntervalIr interval = constraint.RootIntervals[index];
            bool lower = !interval.HasLowerBound || value >= interval.LowerBound;
            bool upper = !interval.HasUpperBound || value <= interval.UpperBound;
            if (lower && upper)
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < constraint.Applications.Length)
        {
            if (ContainsIntegerConstraint(constraint.Applications[index], value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    private static LoweredAssignmentIr RequireAssignment(
        LoweredModuleIr module,
        string name)
    {
        return module.Assignments.Single(item => item.Name == name);
    }

    private static LoweringFixture Compile(string source)
    {
        ModuleSyntax module = ParseModule(source);
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
        Assert.That(classification.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
                item.Code + ":" + item.Message)));
        Assert.That(types.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
                item.Code + ":" + item.Message)));
        return new LoweringFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values,
            Tags = tags,
            AutomaticTags = automatic
        };
    }

    private static ModuleSyntax ParseModule(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "lowered-ir.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static string FormatDiagnostics(LoweringFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item =>
            item.Code + ":" + item.Message));
    }

    private class LoweringFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
        internal TagResolutionResult Tags = null!;
        internal AutomaticTaggingResult AutomaticTags = null!;
    }
}
