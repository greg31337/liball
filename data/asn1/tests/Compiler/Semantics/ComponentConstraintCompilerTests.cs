using asn1;

namespace asn1.Tests;

public class ComponentConstraintCompilerTests
{
    [Test]
    public void CompilerRebindsCollectionElementConstraintToElementDomain()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Numbers ::= SEQUENCE (WITH COMPONENT (1..9)) OF INTEGER; "
                + "RawNumbers ::= SEQUENCE OF INTEGER; "
                + "AliasNumbers ::= RawNumbers (WITH COMPONENT (2..8)) END");
        ResolvedTypeSyntax numbers = RequireType(fixture, "Numbers");
        ResolvedTypeSyntax element = numbers.InnerType ?? throw new InvalidOperationException();
        NormalizedComponentConstraintSet set = numbers.ComponentConstraintSets.Single();
        NormalizedConstraintGraph nested = set.Components.Single().ValueConstraint
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax raw = RequireType(fixture, "RawNumbers");
        NormalizedComponentConstraintSet aliasSet = RequireType(fixture, "AliasNumbers")
            .ComponentConstraintSets.Single();

        Assert.That(set.CollectionElement, Is.True);
        Assert.That(set.GoverningType, Is.SameAs(numbers));
        Assert.That(set.Components.Single().ComponentType, Is.SameAs(element));
        Assert.That(nested.GoverningType, Is.SameAs(element));
        Assert.That(nested.Domain, Is.SameAs(element.ConstraintDomain));
        Assert.That(nested.RootPartition?.Expression?.Kind,
            Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(nested.RootPartition?.Expression?.UpperValue?
            .IntegerValue.ToString(), Is.EqualTo("9"));
        Assert.That(aliasSet.Components.Single().ComponentType, Is.SameAs(raw.InnerType));
        Assert.That(aliasSet.Components.Single().ValueConstraint?.RootPartition?
            .Expression?.UpperValue?.IntegerValue.ToString(), Is.EqualTo("8"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void FullSequenceSpecificationMaterializesImplicitPresenceRules()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Record ::= SEQUENCE { required INTEGER, count INTEGER OPTIONAL, "
                + "label UTF8String OPTIONAL, note BOOLEAN OPTIONAL } "
                + "(WITH COMPONENTS { required (0..9), count (1..5), label OPTIONAL }) END");
        ResolvedTypeSyntax record = RequireType(fixture, "Record");
        NormalizedComponentConstraintSet set = record.ComponentConstraintSets.Single();
        NormalizedComponentConstraint required = FindComponent(set, "required");
        NormalizedComponentConstraint count = FindComponent(set, "count");
        NormalizedComponentConstraint label = FindComponent(set, "label");
        NormalizedComponentConstraint note = FindComponent(set, "note");

        Assert.That(set.PartialSpecification, Is.False);
        Assert.That(required.Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Unspecified));
        Assert.That(count.Presence, Is.EqualTo(ComponentPresenceConstraintKind.Present));
        Assert.That(label.Presence, Is.EqualTo(ComponentPresenceConstraintKind.Optional));
        Assert.That(note.Presence, Is.EqualTo(ComponentPresenceConstraintKind.Absent));
        Assert.That(note.Implicit, Is.True);
        Assert.That(required.ValueConstraint?.GoverningType,
            Is.SameAs(required.ComponentType));
        Assert.That(count.ValueConstraint?.Domain,
            Is.SameAs(count.ComponentType?.ConstraintDomain));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void PartialSpecificationDoesNotImplyPresenceOrAbsence()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Record ::= SET { count INTEGER OPTIONAL, label UTF8String OPTIONAL, "
                + "note BOOLEAN OPTIONAL } "
                + "(WITH COMPONENTS { ..., count (1..5), label PRESENT }) END");
        NormalizedComponentConstraintSet set = RequireType(fixture, "Record")
            .ComponentConstraintSets.Single();

        Assert.That(set.PartialSpecification, Is.True);
        Assert.That(set.Components, Has.Length.EqualTo(2));
        Assert.That(FindComponent(set, "count").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Unspecified));
        Assert.That(FindComponent(set, "label").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Present));
        Assert.That(set.Components.Any(component => component.Name == "note"), Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void FullChoiceSpecificationMarksOmittedAlternativesAbsent()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Selection ::= CHOICE { text UTF8String, number INTEGER, flag BOOLEAN } "
                + "(WITH COMPONENTS { text PRESENT, number ABSENT }) END");
        NormalizedComponentConstraintSet set = RequireType(fixture, "Selection")
            .ComponentConstraintSets.Single();

        Assert.That(FindComponent(set, "text").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Present));
        Assert.That(FindComponent(set, "number").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Absent));
        Assert.That(FindComponent(set, "flag").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Absent));
        Assert.That(FindComponent(set, "flag").Implicit, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerValidatesNestedComponentSpecificationsRecursively()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Outer ::= SEQUENCE { inner SEQUENCE { value INTEGER OPTIONAL, "
                + "other BOOLEAN OPTIONAL } OPTIONAL } "
                + "(WITH COMPONENTS { inner "
                + "(WITH COMPONENTS { value PRESENT }) PRESENT }) END");
        ResolvedTypeSyntax outer = RequireType(fixture, "Outer");
        NormalizedComponentConstraintSet outerSet = outer.ComponentConstraintSets[0];
        NormalizedComponentConstraintSet innerSet = outer.ComponentConstraintSets[1];
        NormalizedComponentConstraint inner = FindComponent(outerSet, "inner");

        Assert.That(outer.ComponentConstraintSets, Has.Length.EqualTo(2));
        Assert.That(inner.Presence, Is.EqualTo(ComponentPresenceConstraintKind.Present));
        Assert.That(inner.ValueConstraint?.GoverningType, Is.SameAs(inner.ComponentType));
        Assert.That(innerSet.GoverningType, Is.SameAs(inner.ComponentType));
        Assert.That(FindComponent(innerSet, "value").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Present));
        Assert.That(FindComponent(innerSet, "other").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Absent));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerRetainsInheritedAndSequentialComponentSpecifications()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Base ::= SEQUENCE { first INTEGER OPTIONAL, second INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., first PRESENT }); "
                + "Derived ::= Base (WITH COMPONENTS { ..., second ABSENT }) END");
        NormalizedComponentConstraintSet[] baseSets = RequireType(fixture, "Base")
            .ComponentConstraintSets;
        NormalizedComponentConstraintSet[] derivedSets = RequireType(fixture, "Derived")
            .ComponentConstraintSets;

        Assert.That(baseSets, Has.Length.EqualTo(1));
        Assert.That(derivedSets, Has.Length.EqualTo(2));
        Assert.That(FindComponent(derivedSets[0], "first").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Present));
        Assert.That(FindComponent(derivedSets[1], "second").Presence,
            Is.EqualTo(ComponentPresenceConstraintKind.Absent));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerDiagnosesInvalidNamesOrderPresenceAndGoverningTypes()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Unknown ::= SEQUENCE { a INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., missing PRESENT }); "
                + "Duplicate ::= SET { a INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., a PRESENT, a ABSENT }); "
                + "Order ::= SEQUENCE { a INTEGER OPTIONAL, b INTEGER OPTIONAL } "
                + "(WITH COMPONENTS { ..., b PRESENT, a PRESENT }); "
                + "Mandatory ::= SEQUENCE { a INTEGER } "
                + "(WITH COMPONENTS { ..., a PRESENT }); "
                + "ChoiceOptional ::= CHOICE { a INTEGER, b BOOLEAN } "
                + "(WITH COMPONENTS { ..., a OPTIONAL }); "
                + "ChoicePresent ::= CHOICE { a INTEGER, b BOOLEAN } "
                + "(WITH COMPONENTS { ..., a PRESENT, b PRESENT }); "
                + "ScalarOne ::= INTEGER (WITH COMPONENT (1)); "
                + "ScalarMany ::= INTEGER (WITH COMPONENTS { ..., a PRESENT }) END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0006"),
            Is.EqualTo(2));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0007"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0008"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0009"),
            Is.EqualTo(3));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0010"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("ASNC"))
            .All(diagnostic => diagnostic.Span != null), Is.True);
    }

    private static NormalizedComponentConstraint FindComponent(
        NormalizedComponentConstraintSet set,
        string name)
    {
        return set.Components.Single(component => component.Name == name);
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
            "component-constraints.asn",
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
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
