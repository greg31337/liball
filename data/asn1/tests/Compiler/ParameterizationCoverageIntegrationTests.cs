using asn1;

namespace asn1.Tests;

public class ParameterizationCoverageIntegrationTests
{
    [Test]
    public void PipelineInfersAndInstantiatesEveryFormalAndActualCategory()
    {
        ResolutionFixture fixture = Resolve(
        [
            "Categories DEFINITIONS ::= BEGIN "
                + "ITEM-CLASS ::= CLASS { &id INTEGER UNIQUE }; "
                + "item ITEM-CLASS ::= { &id 3 }; "
                + "Items ITEM-CLASS ::= { item }; "
                + "Template { T, T : value, T : Values, ITEM-GOVERNOR, "
                + "ITEM-GOVERNOR : object, ITEM-GOVERNOR : Objects } ::= "
                + "T; "
                + "Use ::= Template { INTEGER (0..10), 3, { 1 | 2 }, "
                + "ITEM-CLASS, item, { item } } END"
        ]);
        SymbolTableEntry template = RequireSymbol(fixture, 0, "Template");
        ParsedParameterizedReferenceSyntax reference = RequireReference(
            fixture,
            0,
            "Use",
            "Template");
        AsnParameterKind[] expected = System.Enum.GetValues<AsnParameterKind>()
            .Where(item => item != AsnParameterKind.Unknown).ToArray();

        Assert.That(template.FormalParameters.Select(item => item.Kind),
            Is.EqualTo(expected));
        Assert.That(reference.ActualParameters.Select(item => item.Kind),
            Is.EqualTo(expected));
        Assert.That(template.FormalParameterSyntax[1].DummyGovernor,
            Is.SameAs(template.FormalParameterSyntax[0]));
        Assert.That(template.FormalParameterSyntax[2].DummyGovernor,
            Is.SameAs(template.FormalParameterSyntax[0]));
        Assert.That(template.FormalParameterSyntax[4].DummyGovernor,
            Is.SameAs(template.FormalParameterSyntax[3]));
        Assert.That(template.FormalParameterSyntax[5].DummyGovernor,
            Is.SameAs(template.FormalParameterSyntax[3]));
        Assert.That(reference.Validated, Is.True, Format(fixture.Diagnostics));
        Assert.That(reference.Instantiation?.Success, Is.True,
            reference.Instantiation?.FailureMessage);
        ResolvedTypeSyntax use = RequireType(fixture, 0, "Use");
        Assert.That(use.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(use.IntegerConstraints?.Root?.Contains(
            new System.Numerics.BigInteger(10)), Is.True);
        Assert.That(use.IntegerConstraints?.Root?.Contains(
            new System.Numerics.BigInteger(11)), Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            Format(fixture.Diagnostics));
    }

    [Test]
    public void ImportedNestedAndRecursiveInstantiationsReuseStableMemoizedSchemas()
    {
        ResolutionFixture fixture = Resolve(
        [
            "Library DEFINITIONS ::= BEGIN "
                + "EXPORTS Identity, Wrapper, List; "
                + "Identity { T } ::= T; "
                + "Wrapper { T } ::= SEQUENCE OF Identity { T }; "
                + "List { T } ::= SEQUENCE { elem T, "
                + "next List { T } OPTIONAL } END",
            "Client DEFINITIONS ::= BEGIN "
                + "IMPORTS Identity, Wrapper, List FROM Library; "
                + "First ::= Wrapper { INTEGER }; "
                + "Second ::= Wrapper{INTEGER}; "
                + "Nested ::= Wrapper { Identity { BOOLEAN } }; "
                + "IntegerList ::= List { INTEGER }; "
                + "SecondList ::= List{INTEGER} END"
        ]);
        ParsedParameterizedReferenceSyntax first = RequireReference(
            fixture,
            1,
            "First",
            "Wrapper");
        ParsedParameterizedReferenceSyntax second = RequireReference(
            fixture,
            1,
            "Second",
            "Wrapper");
        ParsedParameterizedReferenceSyntax integerList = RequireReference(
            fixture,
            1,
            "IntegerList",
            "List");
        ParsedParameterizedReferenceSyntax secondList = RequireReference(
            fixture,
            1,
            "SecondList",
            "List");

        Assert.That(first.Target?.QualifiedName, Is.EqualTo("Library.Wrapper"));
        Assert.That(first.Instantiation?.CacheHit, Is.False);
        Assert.That(second.Instantiation?.CacheHit, Is.True);
        Assert.That(second.Instantiation?.StableId,
            Is.EqualTo(first.Instantiation?.StableId));
        Assert.That(second.Instantiation?.AssignmentSyntax,
            Is.SameAs(first.Instantiation?.AssignmentSyntax));
        Assert.That(RequireType(fixture, 1, "First").EffectiveKind,
            Is.EqualTo(AsnTypeKind.SequenceOf));
        Assert.That(RequireType(fixture, 1, "First").InnerType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(RequireType(fixture, 1, "Nested").EffectiveKind,
            Is.EqualTo(AsnTypeKind.SequenceOf));
        Assert.That(RequireType(fixture, 1, "Nested").InnerType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.Boolean));

        Assert.That(integerList.Instantiation?.DeferredFixups.Single()
            .ReplacementName, Is.EqualTo("IntegerList"));
        Assert.That(secondList.Instantiation?.CacheHit, Is.True);
        Assert.That(secondList.Instantiation?.StableId,
            Is.EqualTo(integerList.Instantiation?.StableId));
        Assert.That(secondList.Instantiation?.DeferredFixups.Single()
            .ReplacementName, Is.EqualTo("SecondList"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            Format(fixture.Diagnostics));
    }

    [Test]
    public void SubstitutionKeepsInsertedReferencesOutsideDummyScope()
    {
        ResolutionFixture fixture = Resolve(
        [
            "Capture DEFINITIONS ::= BEGIN "
                + "U ::= UTF8String; "
                + "Pair { T, U } ::= SEQUENCE { first T, second U, "
                + "third Capture.U }; "
                + "Use ::= Pair { U, INTEGER } END"
        ]);
        ResolvedTypeSyntax use = RequireType(fixture, 0, "Use");
        ParsedParameterizedReferenceSyntax reference = RequireReference(
            fixture,
            0,
            "Use",
            "Pair");

        Assert.That(use.Components.Select(item => item.TypeResolution?.EffectiveKind),
            Is.EqualTo(new[]
            {
                AsnTypeKind.Utf8String,
                AsnTypeKind.Integer,
                AsnTypeKind.Utf8String
            }));
        SubstitutionTokenOrigin inserted = reference.Instantiation?.BodyOrigins
            .Single(item => item.Inserted && item.SourceToken?.Text == "U")
            ?? throw new InvalidOperationException();
        Assert.That(inserted.Module?.ModuleName, Is.EqualTo("Capture"));
        Assert.That(inserted.FormalParameter?.Parameter.Name, Is.EqualTo("T"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            Format(fixture.Diagnostics));
    }

    [Test]
    public void PipelineReportsEveryFormalActualArityAndRecursionFailureClass()
    {
        ResolutionFixture formal = ResolveAllowingFailure(
        [
            "FormalFailures DEFINITIONS ::= BEGIN "
                + "Duplicate { T, T } ::= INTEGER; "
                + "Invalid { INTEGER : Values, Values : item } ::= INTEGER; "
                + "BadDummy { item } ::= INTEGER; "
                + "Unknown { Missing : item } ::= INTEGER END"
        ]);
        AssertCodes(formal, "ASNB0060", "ASNB0061", "ASNB0062", "ASNB0063");

        ResolutionFixture actual = ResolveAllowingFailure(
        [
            "ActualFailures DEFINITIONS ::= BEGIN "
                + "Pair { T, U } ::= INTEGER; "
                + "ValueTemplate { INTEGER : value } ::= INTEGER; "
                + "Missing ::= Pair { INTEGER }; "
                + "Excess ::= Pair { INTEGER, BOOLEAN, NULL }; "
                + "Wrong ::= ValueTemplate { UTF8String } END"
        ]);
        AssertCodes(actual, "ASNB0064", "ASNB0065", "ASNB0072", "ASNB0073");

        ResolutionFixture changing = ResolveAllowingFailure(
        [
            "ChangingGovernor DEFINITIONS ::= BEGIN "
                + "List { T, T : Values } ::= SEQUENCE { item T (Values), "
                + "next List { INTEGER, { 1 } } OPTIONAL }; "
                + "Use ::= List { BOOLEAN, { TRUE } } END"
        ]);
        ResolutionFixture unguarded = ResolveAllowingFailure(
        [
            "Unguarded DEFINITIONS ::= BEGIN "
                + "Loop { T } ::= SEQUENCE { next Loop { T } }; "
                + "Use ::= Loop { INTEGER } END"
        ]);
        ResolutionFixture nonTerminating = ResolveAllowingFailure(
        [
            "NonTerminating DEFINITIONS ::= BEGIN "
                + "List { T } ::= SEQUENCE { elem T, "
                + "next List { [0] T } OPTIONAL }; "
                + "Use ::= List { INTEGER } END"
        ]);

        AssertCycleFailure(changing, "changes a dummy governor");
        AssertCycleFailure(unguarded, "not guarded");
        AssertCycleFailure(nonTerminating, "non-terminating");
    }

    private static void AssertCodes(
        ResolutionFixture fixture,
        params string[] expectedCodes)
    {
        string[] actual = fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code).ToArray();
        Assert.That(fixture.Classification.Success
            && fixture.Types.Success
            && fixture.Values.Success, Is.False);
        foreach (string code in expectedCodes)
        {
            Assert.That(actual, Does.Contain(code), Format(fixture.Diagnostics));
        }
    }

    private static void AssertCycleFailure(
        ResolutionFixture fixture,
        string messagePart)
    {
        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Any(item =>
            item.Code == "ASNB0075" && item.Message.Contains(messagePart)),
            Is.True, Format(fixture.Diagnostics));
    }

    private static ParsedParameterizedReferenceSyntax RequireReference(
        ResolutionFixture fixture,
        int moduleIndex,
        string symbolName,
        string referenceName)
    {
        return RequireSymbol(fixture, moduleIndex, symbolName)
            .ParameterizedReferences.First(item => item.ReferenceName == referenceName);
    }

    private static SymbolTableEntry RequireSymbol(
        ResolutionFixture fixture,
        int moduleIndex,
        string name)
    {
        return fixture.Tables[moduleIndex].Symbols.Single(item => item.Name == name);
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        int moduleIndex,
        string name)
    {
        return fixture.Tables[moduleIndex].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string[] sources)
    {
        ResolutionFixture fixture = ResolveAllowingFailure(sources);
        Assert.That(fixture.Values.Success, Is.True, Format(fixture.Diagnostics));
        return fixture;
    }

    private static ResolutionFixture ResolveAllowingFailure(string[] sources)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        ModuleSyntax[] modules = new ModuleSyntax[sources.Length];
        int index = 0;
        while (index < sources.Length)
        {
            modules[index] = ParseModule(
                "parameter-coverage-" + index.ToString() + ".asn",
                sources[index]);
            index = index + 1;
        }

        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(
            diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Classification = classification,
            Diagnostics = diagnostics,
            Tables = tables,
            Types = types,
            Values = values
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

    private static string Format(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
            + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal AssignmentClassificationResult Classification = null!;
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal TypeResolutionResult Types = null!;
        internal ValueResolutionResult Values = null!;
    }
}
