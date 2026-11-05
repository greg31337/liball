using asn1;

namespace asn1.Tests;

public class TypeResolverTests
{
    [Test]
    public void ResolverRetainsAliasIdentitySeparatelyFromImportedEffectiveType()
    {
        ModuleSyntax provider = ParseModule(
            "provider.asn",
            "Provider DEFINITIONS ::= BEGIN Base ::= INTEGER END");
        ModuleSyntax consumer = ParseModule(
            "consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN IMPORTS Base FROM Provider; "
                + "Alias ::= Provider.Base END");
        ResolutionFixture fixture = Resolve([provider, consumer]);

        SymbolTableEntry alias = fixture.Tables[1]
            .FindSymbol("Alias", AsnAssignmentKind.Type)
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax resolution = alias.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(resolution.SourceType?.Kind, Is.EqualTo(AsnTypeKind.Defined));
        Assert.That(resolution.SourceType?.Name, Is.EqualTo("Provider.Base"));
        Assert.That(resolution.EffectiveType?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(resolution.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(resolution.AliasTarget?.SourceDeclaration?.QualifiedName,
            Is.EqualTo("Provider.Base"));
        Assert.That(resolution.EffectiveDeclaration?.QualifiedName,
            Is.EqualTo("Provider.Base"));
        Assert.That(resolution.SourceType, Is.Not.SameAs(resolution.EffectiveType));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverSelectsAnAlternativeThroughAChoiceAlias()
    {
        ModuleSyntax module = ParseModule(
            "selection.asn",
            "Selections DEFINITIONS ::= BEGIN "
                + "BaseChoice ::= CHOICE { count INTEGER, enabled BOOLEAN }; "
                + "ChoiceAlias ::= BaseChoice; "
                + "Selected ::= enabled < ChoiceAlias END");
        ResolutionFixture fixture = Resolve([module]);

        SymbolTableEntry selected = fixture.Tables[0]
            .FindSymbol("Selected", AsnAssignmentKind.Type)
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax resolution = selected.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Classification.Success, Is.True);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(resolution.SourceType?.Kind, Is.EqualTo(AsnTypeKind.Selection));
        Assert.That(resolution.SelectedComponent?.Component?.Name, Is.EqualTo("enabled"));
        Assert.That(resolution.EffectiveType?.Kind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(resolution.EffectiveKind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(resolution.SourceType, Is.Not.SameAs(resolution.EffectiveType));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverUsesTheEffectiveElementTypeForCollections()
    {
        ModuleSyntax module = ParseModule(
            "collection.asn",
            "Collections DEFINITIONS ::= BEGIN "
                + "Item ::= INTEGER; Items ::= SEQUENCE OF Item END");
        ResolutionFixture fixture = Resolve([module]);

        SymbolTableEntry items = fixture.Tables[0]
            .FindSymbol("Items", AsnAssignmentKind.Type)
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax resolution = items.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(resolution.SourceType?.ElementType?.Kind, Is.EqualTo(AsnTypeKind.Defined));
        Assert.That(resolution.EffectiveType?.Kind, Is.EqualTo(AsnTypeKind.SequenceOf));
        Assert.That(resolution.EffectiveType?.ElementType?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ComponentsOfCopiesOnlyRootComponentsAndRetainsExpansionProvenance()
    {
        ModuleSyntax module = ParseModule(
            "components.asn",
            "Components DEFINITIONS ::= BEGIN "
                + "Base ::= SEQUENCE { root INTEGER, ..., [[ 2: added BOOLEAN ]], "
                + "..., tail UTF8String }; "
                + "Derived ::= SEQUENCE { before NULL, ..., "
                + "[[ 3: COMPONENTS OF Base ]], ..., after OCTET STRING } END");
        ResolutionFixture fixture = Resolve([module]);

        SymbolTableEntry derived = fixture.Tables[0]
            .FindSymbol("Derived", AsnAssignmentKind.Type)
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax resolution = derived.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(
            resolution.Components.Select(component => component.Component?.Name),
            Is.EqualTo(new[] { "before", "root", "tail", "after" }));
        Assert.That(
            resolution.EffectiveType?.Components.Select(component => component.Name),
            Is.EqualTo(new[] { "before", "root", "tail", "after" }));
        Assert.That(
            resolution.Components.Select(component => component.Component?.Extension),
            Is.EqualTo(new[] { false, true, true, false }));

        ResolvedComponentSyntax root = resolution.Components[1];
        ResolvedComponentSyntax tail = resolution.Components[2];
        Assert.That(root.Provenance, Has.Length.EqualTo(2));
        Assert.That(root.Provenance[0].Declaration?.Name, Is.EqualTo("Base"));
        Assert.That(root.Provenance[0].ComponentsOf, Is.False);
        Assert.That(root.Provenance[0].Extension, Is.False);
        Assert.That(root.Provenance[1].Declaration?.Name, Is.EqualTo("Derived"));
        Assert.That(root.Provenance[1].ComponentsOf, Is.True);
        Assert.That(root.Provenance[1].Extension, Is.True);
        Assert.That(root.Provenance[1].ExtensionGroupIndex, Is.EqualTo(0));
        Assert.That(root.Provenance[1].HasVersionNumber, Is.True);
        Assert.That(root.Provenance[1].VersionNumber.ToString(), Is.EqualTo("3"));
        Assert.That(tail.Provenance[0].AfterExtensionEnd, Is.True);
        Assert.That(
            resolution.Components.Any(component => component.Component?.Name == "added"),
            Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverAllowsTypeRecursionAcrossAComponentBoundary()
    {
        ModuleSyntax module = ParseModule(
            "recursive.asn",
            "Recursive DEFINITIONS ::= BEGIN "
                + "Node ::= SEQUENCE { next Node OPTIONAL } END");
        ResolutionFixture fixture = Resolve([module]);

        ResolvedTypeSyntax node = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax next = node.Components.Single().TypeResolution
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(next.RecursiveReference, Is.True);
        Assert.That(next.AliasTarget, Is.SameAs(node));
        Assert.That(next.EffectiveKind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase(
        "Bad ::= missing < CHOICE { present INTEGER }",
        "ASNS0002")]
    [TestCase(
        "Bad ::= choice < INTEGER",
        "ASNS0001")]
    [TestCase(
        "Base ::= SET { item INTEGER }; Bad ::= SEQUENCE { COMPONENTS OF Base }",
        "ASNS0003")]
    public void ResolverDiagnosesInvalidSelectionAndComponentsOfEffectiveTypes(
        string assignments,
        string expectedCode)
    {
        ModuleSyntax module = ParseModule(
            "invalid.asn",
            "Invalid DEFINITIONS ::= BEGIN " + assignments + " END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain(expectedCode));
        if (expectedCode == "ASNS0003")
        {
            Assert.That(
                fixture.Diagnostics.GetDiagnostics()
                    .Single(diagnostic => diagnostic.Code == expectedCode)
                    .RelatedLocations,
                Has.Length.EqualTo(1));
        }
    }

    [Test]
    public void ResolverDiagnosesAnUndefinedTypeReference()
    {
        ModuleSyntax module = ParseModule(
            "undefined.asn",
            "Undefined DEFINITIONS ::= BEGIN Bad ::= Missing END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Classification.Success, Is.True);
        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNB0012" }));
    }

    [Test]
    public void ResolverRejectsAliasOnlyCycles()
    {
        ModuleSyntax module = ParseModule(
            "cycle.asn",
            "Cycle DEFINITIONS ::= BEGIN First ::= Second; Second ::= First END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNB0013"));
    }

    private static ResolutionFixture Resolve(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult result = new TypeResolver(diagnostics).ResolveTypes(tables);
        return new ResolutionFixture
        {
            Classification = classification,
            Diagnostics = diagnostics,
            Result = result,
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
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        return result.Module ?? throw new InvalidOperationException();
    }

    private class ResolutionFixture
    {
        internal AssignmentClassificationResult Classification = null!;
        internal DiagnosticBag Diagnostics = null!;
        internal TypeResolutionResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
