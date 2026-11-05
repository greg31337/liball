using asn1;

namespace asn1.Tests;

public class ParameterInstantiationEngineTests
{
    [Test]
    public void InstantiatorMaterializesEveryAssignmentCategory()
    {
        InstantiationFixture fixture = BuildTargets(
            "Categories DEFINITIONS ::= BEGIN "
                + "ITEM-CLASS ::= CLASS { &Type, &id INTEGER }; "
                + "item ITEM-CLASS ::= { &Type INTEGER, &id 3 }; "
                + "TypeTemplate { T } ::= SEQUENCE OF T; "
                + "valueTemplate { INTEGER : value } INTEGER ::= value; "
                + "SetTemplate { INTEGER : Values } INTEGER ::= { Values }; "
                + "CLASS-TEMPLATE { T, INTEGER : value } ::= CLASS { "
                + "&Type DEFAULT T, &id INTEGER DEFAULT value }; "
                + "objectTemplate { T, INTEGER : value } ITEM-CLASS ::= { "
                + "&Type T, &id value }; "
                + "ObjectsTemplate { ITEM-CLASS : Objects } ITEM-CLASS ::= "
                + "{ Objects } END");

        AssertBody(Instantiate(fixture, "TypeTemplate { INTEGER }",
            "TypeTemplate", AsnParameterKind.Type), "SEQUENCE OF INTEGER");
        AssertBody(Instantiate(fixture, "valueTemplate { 7 }",
            "valueTemplate", AsnParameterKind.Value), "7");
        AssertBody(Instantiate(fixture, "SetTemplate { { 1 | 2 } }",
            "SetTemplate", AsnParameterKind.ValueSet), "INTEGER ( 1 | 2 )");
        AssertBody(Instantiate(fixture, "CLASS-TEMPLATE { UTF8String, 7 }",
            "CLASS-TEMPLATE", AsnParameterKind.ObjectClass),
            "CLASS { &Type DEFAULT UTF8String , &id INTEGER DEFAULT 7 }");
        AssertBody(Instantiate(fixture, "objectTemplate { UTF8String, 7 }",
            "objectTemplate", AsnParameterKind.Object),
            "{ &Type UTF8String , &id 7 }");
        AssertBody(Instantiate(fixture, "ObjectsTemplate { { item } }",
            "ObjectsTemplate", AsnParameterKind.ObjectSet), "{ item }");
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorExpandsNestedActualListsAndValueSetTypes()
    {
        InstantiationFixture fixture = Compile(
            "Nested DEFINITIONS ::= BEGIN "
                + "Identity { T } ::= T; "
                + "Wrapper { T } ::= SEQUENCE OF Identity { T }; "
                + "SetTemplate { INTEGER : Values } INTEGER ::= { Values }; "
                + "NestedIdentity ::= Identity { Identity { INTEGER } }; "
                + "Use ::= Wrapper { SetTemplate { { 1 | 2 } } } END");
        ParameterInstantiationResult nested = RequireInstantiation(
            fixture,
            "NestedIdentity");
        ParameterInstantiationResult valueSet = RequireInstantiation(
            fixture,
            "Use");

        Assert.That(Text(nested.BodyTokens), Is.EqualTo("INTEGER"));
        Assert.That(nested.NestedReferences, Has.Length.EqualTo(1));
        Assert.That(nested.NestedReferences[0].ReferenceName,
            Is.EqualTo("Identity"));
        Assert.That(Text(valueSet.BodyTokens),
            Is.EqualTo("SEQUENCE OF INTEGER ( 1 | 2 )"));
        Assert.That(valueSet.AssignmentSyntax?.TypeSyntax?.InnerTypeSyntax
            ?.ConstraintSyntax, Has.Length.EqualTo(1));
        Assert.That(valueSet.NestedReferences, Has.Length.EqualTo(1));
        Assert.That(valueSet.NestedReferences[0].Instantiation?.NestedReferences,
            Has.Length.EqualTo(1));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorMemoizesNormalizedArgumentsAndKeepsUseTraces()
    {
        string source = "Memo DEFINITIONS ::= BEGIN "
            + "Template { T } ::= SEQUENCE OF T; "
            + "Other { T } ::= SET OF T; "
            + "First ::= Template { INTEGER }; "
            + "Second ::= Template{INTEGER}; "
            + "Different ::= Template { BOOLEAN }; "
            + "OtherUse ::= Other { INTEGER } END";
        InstantiationFixture fixture = Compile(source);
        ParameterInstantiationResult first = RequireInstantiation(fixture, "First");
        ParameterInstantiationResult second = RequireInstantiation(fixture, "Second");
        ParameterInstantiationResult different = RequireInstantiation(
            fixture,
            "Different");
        ParameterInstantiationResult other = RequireInstantiation(
            fixture,
            "OtherUse");
        ParameterInstantiationResult repeated = RequireInstantiation(
            Compile(source),
            "First");

        Assert.That(first.CacheHit, Is.False);
        Assert.That(second.CacheHit, Is.True);
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.AssignmentSyntax, Is.SameAs(first.AssignmentSyntax));
        Assert.That(second.StableId, Is.EqualTo(first.StableId));
        Assert.That(second.CacheKey, Is.EqualTo(first.CacheKey));
        Assert.That(first.StableId, Is.EqualTo(repeated.StableId));
        Assert.That(different.CacheHit, Is.False);
        Assert.That(different.StableId, Is.Not.EqualTo(first.StableId));
        Assert.That(other.StableId, Is.Not.EqualTo(first.StableId));
        Assert.That(first.Trace?.Declaration?.Name, Is.EqualTo("First"));
        Assert.That(second.Trace?.Declaration?.Name, Is.EqualTo("Second"));
        Assert.That(second.Trace, Is.Not.SameAs(first.Trace));
        SubstitutionTokenOrigin secondInteger = second.BodyOrigins.Single(item =>
            item.Inserted && item.SourceToken?.Text == "INTEGER");
        Assert.That(secondInteger.Declaration?.Name, Is.EqualTo("Second"));
        Assert.That(secondInteger.SourceToken?.Span?.Start.Offset,
            Is.Not.EqualTo(first.BodyOrigins.Single(item =>
                item.Inserted && item.SourceToken?.Text == "INTEGER")
                .SourceToken?.Span?.Start.Offset));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorDefersOptionalRecursiveTypeReferences()
    {
        InstantiationFixture fixture = Compile(
            "Recursive DEFINITIONS ::= BEGIN "
                + "List { T } ::= SEQUENCE { "
                + "elem T, next List { T } OPTIONAL }; "
                + "IntegerList ::= List { INTEGER }; "
                + "SecondIntegerList ::= List { INTEGER } END");
        ParameterInstantiationResult result = RequireInstantiation(
            fixture,
            "IntegerList");
        ParameterInstantiationResult second = RequireInstantiation(
            fixture,
            "SecondIntegerList");
        ParameterInstantiationFixup fixup = result.DeferredFixups.Single();

        Assert.That(Text(result.BodyTokens), Is.EqualTo(
            "SEQUENCE { elem INTEGER , next IntegerList OPTIONAL }"));
        Assert.That(fixup.ReplacementName, Is.EqualTo("IntegerList"));
        Assert.That(fixup.OptionalBoundary, Is.True);
        Assert.That(fixup.TargetStableId, Is.EqualTo(result.StableId));
        Assert.That(result.AssignmentSyntax?.TypeSyntax?.ComponentSyntax[1]
            .TypeSyntax?.ReferenceName, Is.EqualTo("IntegerList"));
        Assert.That(second.CacheHit, Is.True);
        Assert.That(second.StableId, Is.EqualTo(result.StableId));
        Assert.That(Text(second.BodyTokens), Is.EqualTo(
            "SEQUENCE { elem INTEGER , next SecondIntegerList OPTIONAL }"));
        Assert.That(second.DeferredFixups.Single().ReplacementName,
            Is.EqualTo("SecondIntegerList"));
        Assert.That(second.AssignmentSyntax?.TypeSyntax?.ComponentSyntax[1]
            .TypeSyntax?.ReferenceName, Is.EqualTo("SecondIntegerList"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorDefersRecursiveTypesWithTerminatingChoice()
    {
        InstantiationFixture fixture = Compile(
            "ChoiceRecursion DEFINITIONS ::= BEGIN "
                + "List { T } ::= CHOICE { "
                + "empty NULL, next List { T } }; "
                + "IntegerList ::= List { INTEGER } END");
        ParameterInstantiationResult result = RequireInstantiation(
            fixture,
            "IntegerList");
        ParameterInstantiationFixup fixup = result.DeferredFixups.Single();

        Assert.That(Text(result.BodyTokens), Is.EqualTo(
            "CHOICE { empty NULL , next IntegerList }"));
        Assert.That(fixup.ChoiceBoundary, Is.True);
        Assert.That(fixup.OptionalBoundary, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorTerminatesFiniteDummyParameterPermutations()
    {
        InstantiationFixture fixture = Compile(
            "Permutation DEFINITIONS ::= BEGIN "
                + "Pair { T, U } ::= SEQUENCE { "
                + "first T, second U, "
                + "next Pair { U, T } OPTIONAL }; "
                + "IntegerBooleanPair ::= Pair { INTEGER, BOOLEAN } END");
        ParameterInstantiationResult result = RequireInstantiation(
            fixture,
            "IntegerBooleanPair");

        Assert.That(Text(result.BodyTokens), Is.EqualTo(
            "SEQUENCE { first INTEGER , second BOOLEAN , next SEQUENCE { "
                + "first BOOLEAN , second INTEGER , next IntegerBooleanPair "
                + "OPTIONAL } OPTIONAL }"));
        Assert.That(result.DeferredFixups.Single().ReplacementName,
            Is.EqualTo("IntegerBooleanPair"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorRejectsNonTerminatingRecursiveActuals()
    {
        InstantiationFixture fixture = Compile(
            "Infinite DEFINITIONS ::= BEGIN "
                + "List { T } ::= SEQUENCE { "
                + "elem T, next List { [0] T } OPTIONAL }; "
                + "IntegerList ::= List { INTEGER } END");

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Any(item =>
            item.Code == "ASNB0075"
            && item.Message.Contains("non-terminating")), Is.True,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorRejectsUnguardedRecursiveTypes()
    {
        InstantiationFixture fixture = Compile(
            "Unguarded DEFINITIONS ::= BEGIN "
                + "Loop { T } ::= SEQUENCE { next Loop { T } }; "
                + "Use ::= Loop { INTEGER } END");

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Any(item =>
            item.Code == "ASNB0075"
            && item.Message.Contains("not guarded")), Is.True,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void InstantiatorRejectsChangingDummyGovernors()
    {
        InstantiationFixture fixture = Compile(
            "ChangingGovernor DEFINITIONS ::= BEGIN "
                + "List { T, T : Values } ::= SEQUENCE { "
                + "item T (Values), "
                + "next List { INTEGER, { 1 } } OPTIONAL }; "
                + "BooleanList ::= List { BOOLEAN, { TRUE } } END");

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Any(item =>
            item.Code == "ASNB0075"
            && item.Message.Contains("changes a dummy governor")), Is.True,
            FormatDiagnostics(fixture));
    }

    private static void AssertBody(
        ParameterInstantiationResult result,
        string expected)
    {
        Assert.That(result.Success, Is.True, result.FailureMessage);
        Assert.That(Text(result.BodyTokens), Is.EqualTo(expected));
    }

    private static ParameterInstantiationResult Instantiate(
        InstantiationFixture fixture,
        string referenceSource,
        string targetName,
        AsnParameterKind kind)
    {
        SymbolTableEntry target = fixture.Tables[0].Symbols.Single(item =>
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
        SymbolTable caller = new SymbolTable { ModuleName = "Caller" };
        SymbolTableEntry declaration = new SymbolTableEntry
        {
            Name = "Use",
            QualifiedName = "Caller.Use"
        };
        reference.Substitution = new ParameterizedSubstituter(fixture.Diagnostics)
            .SubstituteReference(
                reference,
                caller,
                declaration,
                fixture.Tables[0]);
        Assert.That(reference.Substitution.Success, Is.True,
            reference.Substitution.FailureMessage);
        ActualParameterValidator validator = new ActualParameterValidator(
            new ValueResolver(fixture.Diagnostics));
        validator.Tables = fixture.Tables;
        return new ParameterInstantiationEngine(validator).InstantiateReference(
            reference,
            caller,
            declaration,
            fixture.Tables[0]);
    }

    private static ParameterInstantiationResult RequireInstantiation(
        InstantiationFixture fixture,
        string symbolName)
    {
        SymbolTableEntry symbol = fixture.Tables[0].Symbols.Single(item =>
            item.Name == symbolName);
        ParsedParameterizedReferenceSyntax reference =
            symbol.ParameterizedReferences.First();
        Assert.That(reference.Validated, Is.True, FormatDiagnostics(fixture));
        Assert.That(reference.Substitution?.Success, Is.True,
            FormatDiagnostics(fixture));
        return reference.Instantiation ?? throw new InvalidOperationException();
    }

    private static string Text(AsnToken[] tokens)
    {
        return string.Join(" ", tokens.Select(item => item.Text));
    }

    private static InstantiationFixture Compile(string source)
    {
        ModuleSyntax module = ParseModule("instantiation.asn", source);
        DiagnosticBag diagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics)
            .BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult classification =
            new AssignmentClassifier(diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics)
            .ResolveTypes(tables);
        ValueResolutionResult values = new ValueResolver(diagnostics)
            .ResolveValues(tables);
        Assert.That(classification.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics()
                .Select(item => item.Code + ":" + item.Message)));
        return new InstantiationFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Types = types,
            Values = values
        };
    }

    private static InstantiationFixture BuildTargets(string source)
    {
        ModuleSyntax module = ParseModule("instantiation-targets.asn", source);
        DiagnosticBag diagnostics = new DiagnosticBag(
            false,
            new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics)
            .BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult classification =
            new AssignmentClassifier(diagnostics).ClassifyAssignments(tables, graph);
        Assert.That(classification.Success, Is.True,
            string.Join(" | ", diagnostics.GetDiagnostics()
                .Select(item => item.Code + ":" + item.Message)));
        return new InstantiationFixture
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

    private static AsnToken[] Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "instantiation-reference.asn",
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

    private static string FormatDiagnostics(InstantiationFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class InstantiationFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal TypeResolutionResult Types = null!;
        internal ValueResolutionResult Values = null!;
    }
}
