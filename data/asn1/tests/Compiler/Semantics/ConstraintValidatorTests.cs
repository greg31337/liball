using asn1;

namespace asn1.Tests;

public class ConstraintValidatorTests
{
    [Test]
    public void ValidatorDiagnosesProvablyEmptyEffectiveConstraints()
    {
        ResolutionFixture fixture = Resolve(
            "Empty DEFINITIONS ::= BEGIN "
                + "Numbers ::= INTEGER (1..3) (4..6); "
                + "Flags ::= BOOLEAN (TRUE) (FALSE); "
                + "Text ::= UTF8String (SIZE(1..MAX)) "
                + "(FROM (\"A\")) (FROM (\"B\")) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNC0027").ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics, Has.Length.EqualTo(3), FormatDiagnostics(fixture));
        Assert.That(diagnostics.All(item => item.Span != null), Is.True);
        Assert.That(diagnostics.All(item => item.RelatedLocations.Length > 0), Is.True);
        Assert.That(RequireType(fixture, "Numbers").EffectiveConstraintEmpty, Is.True);
        Assert.That(RequireType(fixture, "Flags").EffectiveConstraintEmpty, Is.True);
        Assert.That(RequireType(fixture, "Text").EffectiveConstraintEmpty, Is.True);
    }

    [Test]
    public void ValidatorRejectsConstraintKindsOnInvalidParentTypes()
    {
        ResolutionFixture fixture = Resolve(
            "Invalid DEFINITIONS ::= BEGIN "
                + "BadSize ::= INTEGER (SIZE(1)); "
                + "BadAlphabet ::= CHARACTER STRING (FROM (\"A\")); "
                + "BadPattern ::= INTEGER (PATTERN \"[0-9]+\"); "
                + "BadSettings ::= INTEGER (SETTINGS \"Basic=Date\") END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNC0028").ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics, Has.Length.EqualTo(4), FormatDiagnostics(fixture));
        Assert.That(diagnostics.All(item => item.Span != null), Is.True);
        Assert.That(diagnostics.Select(item => item.Message),
            Has.Some.Contains("Size"));
        Assert.That(diagnostics.Select(item => item.Message),
            Has.Some.Contains("Pattern"));
        Assert.That(RequireType(fixture, "BadSize").ConstraintValidationSuccess, Is.False);
    }

    [Test]
    public void ValidatorRejectsContainedSubtypesWithoutValueMappings()
    {
        ResolutionFixture fixture = Resolve(
            "Mappings DEFINITIONS ::= BEGIN "
                + "Numbers ::= INTEGER (0..10); "
                + "Far ::= INTEGER (20..30); "
                + "WrongShape ::= SEQUENCE { different INTEGER }; "
                + "Expected ::= SEQUENCE { value INTEGER }; "
                + "NoOverlap ::= Numbers (INCLUDES Far | 5); "
                + "BadShape ::= Expected (INCLUDES WrongShape); "
                + "Compatible ::= Expected "
                + "(INCLUDES SEQUENCE { value INTEGER }); "
                + "BadKind ::= INTEGER (INCLUDES UTF8String) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNC0029").ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics, Has.Length.EqualTo(3), FormatDiagnostics(fixture));
        Assert.That(diagnostics.All(item => item.Span != null), Is.True);
        Assert.That(diagnostics.All(item => item.RelatedLocations.Length == 1), Is.True);
        Assert.That(RequireType(fixture, "NoOverlap").EffectiveConstraintEmpty, Is.False);
        Assert.That(RequireType(fixture, "NoOverlap").IntegerConstraints?.KnownValues?
            .Contains(new System.Numerics.BigInteger(5)), Is.True);
        Assert.That(RequireType(fixture, "Compatible").ConstraintValidationSuccess, Is.True);
    }

    [Test]
    public void ValidatorDetectsSequentialPresenceAndTimeContradictions()
    {
        ResolutionFixture fixture = Resolve(
            "Contradictions DEFINITIONS ::= BEGIN "
                + "Record ::= SEQUENCE { value INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., value PRESENT }) "
                + "(WITH COMPONENTS { ..., value ABSENT }); "
                + "Option ::= CHOICE { number INTEGER, flag BOOLEAN } "
                + "(WITH COMPONENTS { ..., number PRESENT }) "
                + "(WITH COMPONENTS { ..., flag PRESENT }); "
                + "When ::= TIME (SETTINGS \"Basic=Date\") "
                + "(SETTINGS \"Basic=Time\") END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNC0027").ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics, Has.Length.EqualTo(3), FormatDiagnostics(fixture));
        Assert.That(diagnostics.All(item => item.Span != null), Is.True);
        Assert.That(diagnostics.All(item => item.RelatedLocations.Length == 1), Is.True);
        Assert.That(RequireType(fixture, "Record").EffectiveConstraintEmpty, Is.True);
        Assert.That(RequireType(fixture, "Option").EffectiveConstraintEmpty, Is.True);
        Assert.That(RequireType(fixture, "When").EffectiveConstraintEmpty, Is.True);
    }

    [Test]
    public void ValidatorReportsContainedSubtypeCyclesWithEveryCycleLocation()
    {
        ResolutionFixture fixture = Resolve(
            "Cycles DEFINITIONS ::= BEGIN "
                + "First ::= INTEGER (INCLUDES Second); "
                + "Second ::= INTEGER (INCLUDES Third); "
                + "Third ::= INTEGER (INCLUDES First) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics()
            .Where(item => item.Code == "ASNC0030").ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics, Has.Length.EqualTo(1), FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Span, Is.Not.Null);
        Assert.That(diagnostics[0].RelatedLocations, Has.Length.EqualTo(2));
        Assert.That(diagnostics[0].RelatedLocations.All(item => item.Span != null), Is.True);
        Assert.That(RequireType(fixture, "First").ConstraintValidationSuccess, Is.False);
        Assert.That(RequireType(fixture, "Second").ConstraintValidationSuccess, Is.False);
        Assert.That(RequireType(fixture, "Third").ConstraintValidationSuccess, Is.False);
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "constraint-validation.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(types.Success, Is.True, FormatDiagnostics(diagnostics));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private static string FormatDiagnostics(ResolutionFixture fixture)
    {
        return FormatDiagnostics(fixture.Diagnostics);
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
            + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
