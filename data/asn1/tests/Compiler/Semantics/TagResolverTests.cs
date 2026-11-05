using asn1;

namespace asn1.Tests;

public class TagResolverTests
{
    [TestCase("Flag ::= BOOLEAN", AsnUniversalTags.Boolean)]
    [TestCase("Number ::= INTEGER", AsnUniversalTags.Integer)]
    [TestCase("Rows ::= SEQUENCE OF UTF8String", AsnUniversalTags.SequenceOf)]
    [TestCase("Fields ::= SET { value INTEGER }", AsnUniversalTags.Set)]
    public void ResolverAssignsUniversalTagsToBuiltinAndConstructedTypes(
        string assignment,
        int expectedNumber)
    {
        ResolutionFixture fixture = Resolve(
            "universal.asn",
            "UniversalTags DEFINITIONS ::= BEGIN " + assignment + " END");
        ResolvedTypeSyntax type = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(type.TagChain, Has.Length.EqualTo(1));
        Assert.That(type.TagChain[0].Tag.Class, Is.EqualTo(AsnTagClass.Universal));
        Assert.That(type.TagChain[0].Tag.Number.ToString(), Is.EqualTo(expectedNumber.ToString()));
        Assert.That(type.TagChain[0].Universal, Is.True);
        Assert.That(type.TagChain[0].OriginDeclaration?.Name,
            Is.EqualTo(fixture.Tables[0].TypeSymbols.Single().Name));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void UntaggedChoiceHasNoUniversalTag()
    {
        ResolutionFixture fixture = Resolve(
            "choice.asn",
            "Choices DEFINITIONS ::= BEGIN Value ::= CHOICE { number INTEGER } END");
        ResolvedTypeSyntax type = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(type.TagChain, Is.Empty);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ExplicitAndImplicitTagsBuildAnOutermostFirstChain()
    {
        ResolutionFixture fixture = Resolve(
            "nested.asn",
            "Nested DEFINITIONS ::= BEGIN "
                + "Tagged ::= [APPLICATION 3] EXPLICIT [PRIVATE 4] IMPLICIT INTEGER END");
        ResolvedTypeSyntax type = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(type.TagChain, Has.Length.EqualTo(2));
        AssertTag(type.TagChain[0], AsnTagClass.Application, "3", AsnTaggingMode.Explicit);
        AssertTag(type.TagChain[1], AsnTagClass.Private, "4", AsnTaggingMode.Implicit);
        Assert.That(type.TagChain.All(tag => tag.OriginDeclaration?.Name == "Tagged"), Is.True);
        Assert.That(type.TagChain.All(tag => !tag.Universal), Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("EXPLICIT", AsnTaggingMode.Explicit, 2)]
    [TestCase("IMPLICIT", AsnTaggingMode.Implicit, 1)]
    [TestCase("AUTOMATIC", AsnTaggingMode.Implicit, 1)]
    public void OmittedModesUseTheModuleTagDefault(
        string tagDefault,
        AsnTaggingMode expectedMode,
        int expectedChainLength)
    {
        ResolutionFixture fixture = Resolve(
            "default.asn",
            "Defaults DEFINITIONS " + tagDefault
                + " TAGS ::= BEGIN Tagged ::= [5] INTEGER END");
        ResolvedTypeSyntax type = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(type.TagChain, Has.Length.EqualTo(expectedChainLength));
        Assert.That(type.TagChain[0].Tag.Mode, Is.EqualTo(expectedMode));
        Assert.That(type.TagChain[0].DeclaredMode, Is.EqualTo(AsnTaggingMode.Unspecified));
        Assert.That(type.TagChain[0].DefaultModeApplied, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ImplicitModuleDefaultBecomesExplicitForAnUntaggedChoice()
    {
        ResolutionFixture fixture = Resolve(
            "default-choice.asn",
            "Defaults DEFINITIONS IMPLICIT TAGS ::= BEGIN "
                + "Tagged ::= [5] CHOICE { number INTEGER } END");
        ResolvedTypeSyntax type = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(type.TagChain, Has.Length.EqualTo(1));
        Assert.That(type.TagChain[0].Tag.Mode, Is.EqualTo(AsnTaggingMode.Explicit));
        Assert.That(type.TagChain[0].DefaultModeApplied, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AliasRetainsTheImportedTagOrigin()
    {
        ModuleSyntax provider = ParseModule(
            "provider.asn",
            "Provider DEFINITIONS IMPLICIT TAGS ::= BEGIN "
                + "Base ::= [APPLICATION 7] INTEGER END");
        ModuleSyntax consumer = ParseModule(
            "consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN IMPORTS Base FROM Provider; Alias ::= Base END");
        ResolutionFixture fixture = ResolveModules([provider, consumer]);
        ResolvedTypeSyntax baseType = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax alias = fixture.Tables[1].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(alias.TagChain, Has.Length.EqualTo(1));
        Assert.That(alias.TagChain[0], Is.SameAs(baseType.TagChain[0]));
        Assert.That(alias.TagChain[0].OriginDeclaration?.QualifiedName,
            Is.EqualTo("Provider.Base"));
        Assert.That(alias.TagChain[0].OriginModule?.Name, Is.EqualTo("Provider"));
        Assert.That(alias.TagChain[0].Tag.Mode, Is.EqualTo(AsnTaggingMode.Implicit));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SelectionUsesTheSelectedAlternativeTagAndOrigin()
    {
        ResolutionFixture fixture = Resolve(
            "selection.asn",
            "Selections DEFINITIONS ::= BEGIN "
                + "Options ::= CHOICE { text [2] IMPLICIT UTF8String, count INTEGER }; "
                + "Selected ::= text < Options END");
        ResolvedTypeSyntax selected = fixture.Tables[0]
            .FindSymbol("Selected", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(selected.TagChain, Has.Length.EqualTo(1));
        AssertTag(
            selected.TagChain[0],
            AsnTagClass.ContextSpecific,
            "2",
            AsnTaggingMode.Implicit);
        Assert.That(selected.TagChain[0].OriginDeclaration?.Name, Is.EqualTo("Options"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("Bad ::= [0] IMPLICIT CHOICE { number INTEGER }")]
    [TestCase("Bad { T } ::= [0] IMPLICIT T")]
    public void ExplicitImplicitTagRejectsAnUntaggedType(string assignment)
    {
        ResolutionFixture fixture = Resolve(
            "illegal.asn",
            "Illegal DEFINITIONS ::= BEGIN " + assignment + " END");

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNS0005" }));
    }

    [Test]
    public void ImplicitTagCanReplaceTheTagOfATaggedChoiceAlias()
    {
        ResolutionFixture fixture = Resolve(
            "tagged-choice.asn",
            "TaggedChoice DEFINITIONS ::= BEGIN "
                + "ChoiceType ::= [3] EXPLICIT CHOICE { number INTEGER }; "
                + "Retagged ::= [4] IMPLICIT ChoiceType END");
        ResolvedTypeSyntax retagged = fixture.Tables[0]
            .FindSymbol("Retagged", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(retagged.TagChain, Has.Length.EqualTo(1));
        AssertTag(
            retagged.TagChain[0],
            AsnTagClass.ContextSpecific,
            "4",
            AsnTaggingMode.Implicit);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void TagNumberReferenceIsRetainedForLaterValueEvaluation()
    {
        ResolutionFixture fixture = Resolve(
            "defined-number.asn",
            "DefinedNumber DEFINITIONS ::= BEGIN Tagged ::= [futureTag] INTEGER END");
        ResolvedTypeSyntax tagged = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(tagged.TagChain[0].HasNumericNumber, Is.False);
        Assert.That(tagged.TagChain[0].NumberReference, Is.EqualTo("futureTag"));
        Assert.That(tagged.TagChain[0].OriginSyntax?.TagNumberReference,
            Is.EqualTo("futureTag"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static void AssertTag(
        ResolvedTagSyntax tag,
        AsnTagClass expectedClass,
        string expectedNumber,
        AsnTaggingMode expectedMode)
    {
        Assert.That(tag.Tag.Class, Is.EqualTo(expectedClass));
        Assert.That(tag.Tag.Number.ToString(), Is.EqualTo(expectedNumber));
        Assert.That(tag.Tag.Mode, Is.EqualTo(expectedMode));
    }

    private static ResolutionFixture Resolve(string sourceName, string source)
    {
        return ResolveModules([ParseModule(sourceName, source)]);
    }

    private static ResolutionFixture ResolveModules(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        TagResolutionResult result = new TagResolver(diagnostics).ResolveTags(tables);
        return new ResolutionFixture
        {
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
        internal DiagnosticBag Diagnostics = null!;
        internal TagResolutionResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
