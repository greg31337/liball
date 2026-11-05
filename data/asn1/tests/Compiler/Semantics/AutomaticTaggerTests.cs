using asn1;

namespace asn1.Tests;

public class AutomaticTaggerTests
{
    [Test]
    public void SelectionProcessesTheSelectedAlternative()
    {
        ResolutionFixture fixture = Resolve(
            "selection.asn",
            "Selection DEFINITIONS ::= BEGIN "
                + "Options ::= CHOICE { number INTEGER, flag BOOLEAN }; "
                + "Selected ::= number < Options END");
        ResolvedTypeSyntax selected = fixture.Tables[0]
            .FindSymbol("Selected", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(selected.AutomaticTaggingSuccess, Is.True);
        Assert.That(selected.SelectedComponent?.TypeResolution?.AutomaticTaggingSuccess,
            Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AutomaticTagsNumberAllRootComponentsBeforeExtensionAdditions()
    {
        ResolutionFixture fixture = Resolve(
            "partitions.asn",
            "Partitions DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Data ::= SEQUENCE { root INTEGER, ..., [[ 2: added BOOLEAN ]], "
                + "..., tail UTF8String } END");
        ResolvedTypeSyntax data = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(data.AutomaticTaggingApplied, Is.True);
        Assert.That(
            data.Components.Select(component => component.Component?.Name),
            Is.EqualTo(new[] { "root", "added", "tail" }));
        Assert.That(
            data.Components.Select(component => component.AutomaticTag?.AutomaticIndex),
            Is.EqualTo(new[] { 0, 2, 1 }));
        Assert.That(
            data.Components.Select(component => component.TagChain[0].Tag.Number.ToString()),
            Is.EqualTo(new[] { "0", "2", "1" }));
        Assert.That(
            data.Components.All(component =>
                component.TagChain[0].Tag.Mode == AsnTaggingMode.Implicit),
            Is.True);
        Assert.That(data.Components[1].AutomaticTag?.OriginComponentSyntax?.HasVersionNumber,
            Is.True);
        Assert.That(data.Components[1].AutomaticTag?.OriginDeclaration?.Name, Is.EqualTo("Data"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AutomaticTagUsesExplicitModeForAnUntaggedChoiceAlternative()
    {
        ResolutionFixture fixture = Resolve(
            "choice-mode.asn",
            "ChoiceMode DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Outer ::= CHOICE { nested CHOICE { value INTEGER }, number INTEGER } END");
        ResolvedTypeSyntax outer = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(outer.Components[0].AutomaticTag?.Tag.Mode,
            Is.EqualTo(AsnTaggingMode.Explicit));
        Assert.That(outer.Components[0].TagChain, Has.Length.EqualTo(1));
        Assert.That(outer.Components[1].AutomaticTag?.Tag.Mode,
            Is.EqualTo(AsnTaggingMode.Implicit));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AutomaticTagMakesADummyReferenceAlternativeUnambiguous()
    {
        ResolutionFixture fixture = Resolve(
            "automatic-open.asn",
            "AutomaticOpen DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Options { T } ::= CHOICE { open T, number INTEGER } END");
        ResolvedTypeSyntax options = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(options.Components[0].AutomaticTag?.Tag.Mode,
            Is.EqualTo(AsnTaggingMode.Explicit));
        Assert.That(options.Components[0].AutomaticTag?.Tag.Number.ToString(), Is.EqualTo("0"));
        Assert.That(options.Components[1].AutomaticTag?.Tag.Number.ToString(), Is.EqualTo("1"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void TextualTagSuppressesAutomaticTagsForTheWholeComponentList()
    {
        ResolutionFixture fixture = Resolve(
            "manual.asn",
            "Manual DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Mixed ::= CHOICE { manual [5] INTEGER, natural BOOLEAN } END");
        ResolvedTypeSyntax mixed = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(mixed.AutomaticTaggingApplied, Is.False);
        Assert.That(mixed.AutomaticTaggingSuppressed, Is.True);
        Assert.That(mixed.Components.All(component => component.AutomaticTag == null), Is.True);
        AssertTag(mixed.Components[0].TagChain[0], AsnTagClass.ContextSpecific, "5");
        AssertTag(mixed.Components[1].TagChain[0], AsnTagClass.Universal, "1");
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void TagsInsideComponentsOfDoNotSuppressTagsAtTheExpansionSite()
    {
        ResolutionFixture fixture = Resolve(
            "components.asn",
            "Components DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Base ::= SEQUENCE { manual [5] INTEGER, natural BOOLEAN }; "
                + "Derived ::= SEQUENCE { COMPONENTS OF Base, final UTF8String } END");
        ResolvedTypeSyntax baseType = fixture.Tables[0]
            .FindSymbol("Base", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax derived = fixture.Tables[0]
            .FindSymbol("Derived", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(baseType.AutomaticTaggingSuppressed, Is.True);
        Assert.That(derived.AutomaticTaggingApplied, Is.True);
        Assert.That(
            derived.Components.Select(component => component.Component?.Name),
            Is.EqualTo(new[] { "manual", "natural", "final" }));
        Assert.That(
            derived.Components.Select(component => component.TagChain[0].Tag.Number.ToString()),
            Is.EqualTo(new[] { "0", "1", "2" }));
        Assert.That(derived.Components.All(component =>
            component.AutomaticTag?.AutomaticallyGenerated == true), Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AutomaticTagsRemoveOtherwiseDuplicateChoiceTags()
    {
        ResolutionFixture fixture = Resolve(
            "automatic-choice.asn",
            "AutomaticChoice DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Good ::= CHOICE { first INTEGER, second INTEGER } END");
        ResolvedTypeSyntax choice = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(
            choice.Components.Select(component => component.TagChain[0].Tag.Number.ToString()),
            Is.EqualTo(new[] { "0", "1" }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void EmptyStructuredTypeHasNoAutomaticTransformation()
    {
        ResolutionFixture fixture = Resolve(
            "empty.asn",
            "Empty DEFINITIONS AUTOMATIC TAGS ::= BEGIN Data ::= SEQUENCE {} END");
        ResolvedTypeSyntax data = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(data.AutomaticTaggingApplied, Is.False);
        Assert.That(data.AutomaticTaggingSuppressed, Is.False);
        Assert.That(data.Components, Is.Empty);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("Bad ::= CHOICE { first INTEGER, second INTEGER }")]
    [TestCase("Bad ::= SET { first INTEGER, second INTEGER }")]
    public void DuplicateEffectiveTagsAreRejectedForChoiceAndSet(string assignment)
    {
        ResolutionFixture fixture = Resolve(
            "duplicates.asn",
            "Duplicates DEFINITIONS ::= BEGIN " + assignment + " END");

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNS0006" }));
        Assert.That(fixture.Diagnostics.GetDiagnostics()[0].RelatedLocations,
            Has.Length.EqualTo(1));
    }

    [Test]
    public void NestedChoiceLeadingTagSetsAreCompared()
    {
        ResolutionFixture fixture = Resolve(
            "nested-collision.asn",
            "NestedCollision DEFINITIONS ::= BEGIN "
                + "Left ::= CHOICE { zero [0] INTEGER, one [1] INTEGER }; "
                + "Right ::= CHOICE { one [1] BOOLEAN, two [2] BOOLEAN }; "
                + "Bad ::= CHOICE { left Left, right Right } END");

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNS0006" }));
        Assert.That(fixture.Diagnostics.GetDiagnostics()[0].Message,
            Does.Contain("ContextSpecific 1"));
    }

    [Test]
    public void OpenAlternativeCollisionsAreRejected()
    {
        ResolutionFixture fixture = Resolve(
            "open.asn",
            "OpenAlternatives DEFINITIONS ::= BEGIN "
                + "Bad { T } ::= CHOICE { open T, number INTEGER } END");

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNS0007" }));
    }

    [Test]
    public void ManualDuplicateTagsStillCollideInAnAutomaticModule()
    {
        ResolutionFixture fixture = Resolve(
            "manual-duplicate.asn",
            "ManualDuplicate DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Bad ::= CHOICE { first [1] INTEGER, second [1] BOOLEAN } END");
        ResolvedTypeSyntax bad = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(bad.AutomaticTaggingSuppressed, Is.True);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNS0006" }));
    }

    private static void AssertTag(
        ResolvedTagSyntax tag,
        AsnTagClass expectedClass,
        string expectedNumber)
    {
        Assert.That(tag.Tag.Class, Is.EqualTo(expectedClass));
        Assert.That(tag.Tag.Number.ToString(), Is.EqualTo(expectedNumber));
    }

    private static ResolutionFixture Resolve(string sourceName, string source)
    {
        ModuleSyntax module = ParseModule(sourceName, source);
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        TagResolutionResult tags = new TagResolver(diagnostics).ResolveTags(tables);
        Assert.That(tags.Success, Is.True);
        AutomaticTaggingResult result = new AutomaticTagger(diagnostics)
            .ApplyAutomaticTags(tables);
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
        internal AutomaticTaggingResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
