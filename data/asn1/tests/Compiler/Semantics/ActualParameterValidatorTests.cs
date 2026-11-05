using asn1;

namespace asn1.Tests;

public class ActualParameterValidatorTests
{
    [Test]
    public void ValidatorAcceptsEveryCategoryWithDummyGovernors()
    {
        ValidationFixture fixture = Compile(
            "ValidActuals DEFINITIONS ::= BEGIN "
                + "ITEM-CLASS ::= CLASS { &id INTEGER UNIQUE }; "
                + "item ITEM-CLASS ::= { &id 3 }; "
                + "Items ITEM-CLASS ::= { item }; "
                + "Template { T, T : value, T : Values, ITEM-GOVERNOR, "
                + "ITEM-GOVERNOR : object, ITEM-GOVERNOR : Objects } "
                + "::= INTEGER; "
                + "Use ::= Template { INTEGER (0..10), 3, { 1 | 2 }, "
                + "ITEM-CLASS, item, { item } } END");
        SymbolTableEntry use = RequireSymbol(fixture.Tables[0], "Use");
        ParsedParameterizedReferenceSyntax reference =
            use.ParameterizedReferences.Single();

        Assert.That(reference.Validated, Is.True, FormatDiagnostics(fixture));
        Assert.That(reference.Substitution?.Success, Is.True,
            FormatDiagnostics(fixture));
        Assert.That(reference.Target?.Name, Is.EqualTo("Template"));
        Assert.That(reference.ActualParameters.Select(item => item.Kind),
            Is.EqualTo(new[]
            {
                AsnParameterKind.Type,
                AsnParameterKind.Value,
                AsnParameterKind.ValueSet,
                AsnParameterKind.ObjectClass,
                AsnParameterKind.Object,
                AsnParameterKind.ObjectSet
            }));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ValidatorRejectsMissingExcessAndWrongCategoryActuals()
    {
        ValidationFixture fixture = Compile(
            "InvalidLists DEFINITIONS ::= BEGIN "
                + "Pair { T, U } ::= INTEGER; "
                + "ValueTemplate { INTEGER : value } ::= INTEGER; "
                + "Missing ::= Pair { INTEGER }; "
                + "Excess ::= Pair { INTEGER, BOOLEAN, NULL }; "
                + "Wrong ::= ValueTemplate { UTF8String } END");
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code)
            .ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(codes.Count(item => item == "ASNB0064"), Is.EqualTo(2),
            FormatDiagnostics(fixture));
        Assert.That(codes, Does.Contain("ASNB0072"), FormatDiagnostics(fixture));
        Assert.That(codes, Does.Contain("ASNB0073"), FormatDiagnostics(fixture));
        Assert.That(codes, Does.Contain("ASNB0065"), FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNB0064"
                || item.Code == "ASNB0065"
                || item.Code == "ASNB0072"
                || item.Code == "ASNB0073")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void ValidatorDiagnosesParameterizedReferenceMisuse()
    {
        ValidationFixture fixture = Compile(
            "ReferenceMisuse DEFINITIONS ::= BEGIN "
                + "Plain ::= INTEGER; "
                + "Template { T } ::= T; "
                + "UnexpectedActuals ::= Plain { INTEGER }; "
                + "MissingActuals ::= Template END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNB0074")
            .ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics, Has.Length.EqualTo(2), FormatDiagnostics(fixture));
        Assert.That(diagnostics.Any(item => item.Message.Contains(
            "cannot be referenced with")), Is.True);
        Assert.That(diagnostics.Any(item => item.Message.Contains(
            "must be referenced with")), Is.True);
        Assert.That(diagnostics.All(item => item.Span != null), Is.True);
    }

    [Test]
    public void ValidatorDiagnosesSchemasInvalidatedBySubstitution()
    {
        ValidationFixture fixture = Compile(
            "InvalidInstantiation DEFINITIONS ::= BEGIN "
                + "Template { T } ::= SEQUENCE { COMPONENTS OF T }; "
                + "Tagged { INTEGER : tag } ::= [tag] INTEGER; "
                + "Use ::= Template { INTEGER }; "
                + "BadTag ::= Tagged { 0 - 1 } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Any(item => item.Code == "ASNB0076"
            && item.Message.Contains("invalid type schema")), Is.True,
            FormatDiagnostics(fixture));
        Assert.That(diagnostics.Any(item => item.Code == "ASNB0076"
            && item.Message.Contains("could not be substituted")), Is.True,
            FormatDiagnostics(fixture));
        Assert.That(diagnostics.Where(item => item.Code == "ASNB0076")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void ValidatorChecksSubtypeCompatibilityAndValueConstraints()
    {
        ValidationFixture fixture = Compile(
            "ValueChecks DEFINITIONS ::= BEGIN "
                + "Small ::= INTEGER (0..5); "
                + "small Small ::= 3; "
                + "text UTF8String ::= \"three\"; "
                + "Template { INTEGER (0..10) : value, "
                + "INTEGER (0..10) : Values } ::= INTEGER; "
                + "Compatible ::= Template { small, { 1..5 } }; "
                + "WrongType ::= Template { text, { 1..5 } }; "
                + "WrongValue ::= Template { 20, { 1..5 } }; "
                + "EmptySet ::= Template { 3, { 20..30 } } END");
        SymbolTableEntry compatible = RequireSymbol(
            fixture.Tables[0],
            "Compatible");
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code)
            .ToArray();

        Assert.That(compatible.ParameterizedReferences.Single().Validated,
            Is.True, FormatDiagnostics(fixture));
        Assert.That(codes, Does.Contain("ASNB0067"), FormatDiagnostics(fixture));
        Assert.That(codes.Count(item => item == "ASNB0069"),
            Is.GreaterThanOrEqualTo(2), FormatDiagnostics(fixture));
    }

    [Test]
    public void ValidatorChecksObjectClassCompatibilityAndInlineSetRules()
    {
        ValidationFixture fixture = Compile(
            "ObjectChecks DEFINITIONS ::= BEGIN "
                + "FIRST-CLASS ::= CLASS { &id INTEGER }; "
                + "SECOND-CLASS ::= CLASS { &name UTF8String }; "
                + "second SECOND-CLASS ::= { &name \"two\" }; "
                + "Seconds SECOND-CLASS ::= { second }; "
                + "Template { FIRST-CLASS : object, FIRST-CLASS : Objects } "
                + "::= INTEGER; "
                + "WrongObject ::= Template { second, { Seconds } }; "
                + "InvalidSet ::= Template { { &id 1 }, "
                + "{ { &unknown 2 } } } END");
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code)
            .ToArray();

        Assert.That(codes.Count(item => item == "ASNB0068"),
            Is.EqualTo(2), FormatDiagnostics(fixture));
        Assert.That(codes, Does.Contain("ASNB0069"), FormatDiagnostics(fixture));
    }

    [Test]
    public void ValidatorAcceptsForwardedFormalObjectSetArguments()
    {
        ValidationFixture fixture = Compile(
            "Forwarded DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER }; "
                + "Element { ITEM : Items } ::= INTEGER; "
                + "Container { ITEM : Items } ::= "
                + "SEQUENCE OF Element {{Items}} END");

        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    private static ValidationFixture Compile(string source)
    {
        ModuleSyntax module = ParseModule("actual-parameters.asn", source);
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
        return new ValidationFixture
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

    private static SymbolTableEntry RequireSymbol(SymbolTable table, string name)
    {
        return table.Symbols.Single(item => item.Name == name);
    }

    private static string FormatDiagnostics(ValidationFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class ValidationFixture
    {
        internal AssignmentClassificationResult Classification = null!;
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal TypeResolutionResult Types = null!;
        internal ValueResolutionResult Values = null!;
    }
}
