using asn1;

namespace asn1.Tests;

public class ObjectSetEvaluatorTests
{
    [Test]
    public void EvaluatorAppliesSetAlgebraWithStableObjectIdentity()
    {
        EvaluationFixture fixture = Evaluate(
            "Sets DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type BOOLEAN }; "
                + "third ITEM ::= { &id 3, &Type UTF8String }; "
                + "Base ITEM ::= { first | second | third }; "
                + "Tail ITEM ::= { second | third }; "
                + "Unioned ITEM ::= { Base | first }; "
                + "Common ITEM ::= { Base ^ Tail }; "
                + "Reduced ITEM ::= { Base EXCEPT Tail }; "
                + "Complement ITEM ::= { ALL EXCEPT second } END");

        Assert.That(ObjectNames(fixture, "Unioned"),
            Is.EqualTo(new[] { "first", "second", "third" }));
        Assert.That(ObjectNames(fixture, "Common"),
            Is.EqualTo(new[] { "second", "third" }));
        Assert.That(ObjectNames(fixture, "Reduced"),
            Is.EqualTo(new[] { "first" }));
        Assert.That(ObjectNames(fixture, "Complement"),
            Is.EqualTo(new[] { "first", "third" }));
        Assert.That(RequireSet(fixture, "Unioned").ObjectSet.Objects
            .Select(item => item.StableId),
            Is.EqualTo(new[] { "Sets.first", "Sets.second", "Sets.third" }));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void EvaluatorPreservesExtensionPartitionsAndEvaluatesNestedSetFields()
    {
        EvaluationFixture fixture = Evaluate(
            "Sets DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "HOLDER ::= CLASS { &Members ITEM }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type BOOLEAN }; "
                + "third ITEM ::= { &id 3, &Type UTF8String }; "
                + "Base ITEM ::= { first | second | third }; "
                + "Tail ITEM ::= { second | third }; "
                + "Extended ITEM ::= { first, ..., third | second }; "
                + "holder HOLDER ::= { &Members { first | second EXCEPT second } } END");
        ParsedInformationObjectSetSyntax extended = RequireSet(fixture, "Extended");
        ParsedInformationObjectSyntax holder = fixture.Tables[0]
            .FindSymbol("holder", AsnAssignmentKind.Object)?.DeclaredObjectSyntax
            ?? throw new InvalidOperationException();
        AsnInformationObjectSet nested = holder.Object.FieldSettings.Single()
            .ObjectSet ?? throw new InvalidOperationException();

        Assert.That(extended.ObjectSet.Extensible, Is.True);
        Assert.That(extended.RootMembers.Select(item => item.Object.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(extended.AdditionalMembers.Select(item => item.Object.Name),
            Is.EqualTo(new[] { "third", "second" }));
        Assert.That(extended.AdditionalMembers.All(item => item.Extension), Is.True);
        Assert.That(extended.ObjectSet.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(extended.ObjectSet.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "third", "second" }));
        Assert.That(extended.ObjectSet.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "first", "third", "second" }));
        Assert.That(nested.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(nested.AdditionalObjects, Is.Empty);
        Assert.That(nested.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(nested.StableId, Is.EqualTo("Sets.holder:&Members"));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void EvaluatorInheritsExtensibilityThroughReferencesAndSetArithmetic()
    {
        EvaluationFixture fixture = Evaluate(
            "Sets DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type BOOLEAN }; "
                + "third ITEM ::= { &id 3, &Type UTF8String }; "
                + "Extended ITEM ::= { first, ..., second }; "
                + "Inherited ITEM ::= { Extended }; "
                + "Unioned ITEM ::= { third | Extended }; "
                + "Intersected ITEM ::= { Extended ^ (first | second) } END");
        ParsedInformationObjectSetSyntax inherited = RequireSet(fixture, "Inherited");
        ParsedInformationObjectSetSyntax unioned = RequireSet(fixture, "Unioned");
        ParsedInformationObjectSetSyntax intersected = RequireSet(
            fixture,
            "Intersected");

        Assert.That(inherited.ObjectSet.Extensible, Is.True);
        Assert.That(inherited.ObjectSet.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(inherited.ObjectSet.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));
        Assert.That(unioned.ObjectSet.Extensible, Is.True);
        Assert.That(unioned.ObjectSet.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "third", "first" }));
        Assert.That(unioned.ObjectSet.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));
        Assert.That(intersected.ObjectSet.Extensible, Is.True);
        Assert.That(intersected.ObjectSet.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(intersected.ObjectSet.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void EvaluatorSubstitutesParameterizedObjectOperands()
    {
        EvaluationFixture fixture = Evaluate(
            "Sets DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type BOOLEAN }; "
                + "PICKS { ITEM : item } ITEM ::= { item }; "
                + "Chosen ITEM ::= { PICKS { first } | second } END");
        ParsedInformationObjectSetSyntax chosen = RequireSet(fixture, "Chosen");

        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(chosen.ObjectSet.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "first", "second" }), FormatDiagnostics(fixture));
        Assert.That(chosen.RootMembers[0].StableId, Is.EqualTo("Sets.first"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void EvaluatorRejectsDuplicateUniqueKeysAfterIdentityDeduplication()
    {
        EvaluationFixture fixture = Evaluate(
            "Sets DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "duplicate ITEM ::= { &id 1, &Type BOOLEAN }; "
                + "Repeated ITEM ::= { first | first }; "
                + "Invalid ITEM ::= { first | duplicate } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(ObjectNames(fixture, "Repeated"), Is.EqualTo(new[] { "first" }));
        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNB0048"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Single(item => item.Code == "ASNB0048").Span,
            Is.Not.Null);
    }

    private static string[] ObjectNames(EvaluationFixture fixture, string name)
    {
        return RequireSet(fixture, name).ObjectSet.Objects
            .Select(item => item.Name).ToArray();
    }

    private static ParsedInformationObjectSetSyntax RequireSet(
        EvaluationFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.ObjectSet)?.DeclaredObjectSetSyntax
            ?? throw new InvalidOperationException();
    }

    private static EvaluationFixture Evaluate(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "object-set-evaluation.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        ModuleSyntax module = parsed.Module ?? throw new InvalidOperationException();
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
        Assert.That(types.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new EvaluationFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private static string FormatDiagnostics(EvaluationFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class EvaluationFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
