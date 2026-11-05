using asn1;

namespace asn1.Tests;

public class InformationFromObjectsResolverTests
{
    [Test]
    public void ResolverAppliesClassFieldSelectionsInsideConstructedTypes()
    {
        ResolutionFixture fixture = Resolve(
            "NestedSelections DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id OBJECT IDENTIFIER UNIQUE, &Type }; "
                + "Container ::= SEQUENCE { id [0] IMPLICIT ITEM.&id, "
                + "value [1] EXPLICIT ITEM.&Type }; "
                + "Deferred { ITEM-CLASS, ITEM-CLASS:Items } ::= SEQUENCE { "
                + "id ITEM-CLASS.&id({Items}) } END");
        ResolvedTypeSyntax container = RequireType(fixture, "Container");
        ResolvedTypeSyntax deferred = RequireType(fixture, "Deferred");
        TagResolutionResult tags = new TagResolver(fixture.Diagnostics)
            .ResolveTags(fixture.Tables);
        AutomaticTaggingResult automatic = new AutomaticTagger(fixture.Diagnostics)
            .ApplyAutomaticTags(fixture.Tables);

        Assert.That(container.Components[0].TypeResolution?.InnerType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.ObjectIdentifier));
        Assert.That(container.Components[1].TypeResolution?.InnerType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(deferred.Components[0].TypeResolution?.ParameterReference, Is.True);
        Assert.That(tags.Success, Is.True);
        Assert.That(automatic.Success, Is.True);
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ResolverSelectsEveryInformationCategoryAndChainedField()
    {
        ResolutionFixture fixture = Resolve(
            "Selections DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER, &Type }; "
                + "ITEM ::= CLASS { &Type, &id INTEGER, &Values INTEGER, "
                + "&child CHILD, &Children CHILD }; "
                + "childOne CHILD ::= { &code 11, &Type BOOLEAN }; "
                + "childTwo CHILD ::= { &code 12, &Type UTF8String }; "
                + "first ITEM ::= { &Type UTF8String, &id 1, &Values { 1 | 2 }, "
                + "&child childOne, &Children { childOne | childTwo } }; "
                + "second ITEM ::= { &Type BOOLEAN, &id 2, &Values { 2 | 3 }, "
                + "&child childTwo, &Children { childTwo } }; "
                + "Items ITEM ::= { first | second }; "
                + "SelectedType ::= first.&Type; "
                + "selectedValue INTEGER ::= first.&id; "
                + "SelectedValues INTEGER ::= Items.&id; "
                + "SelectedDeclaredValues INTEGER ::= Items.&Values; "
                + "selectedChild CHILD ::= first.&child; "
                + "SelectedChildren CHILD ::= Items.&child; "
                + "SelectedNestedChildren CHILD ::= first.&Children; "
                + "chainedCode INTEGER ::= first.&child.&code; "
                + "ClassIdType ::= ITEM.&id; "
                + "NestedCodeType ::= ITEM.&child.&code END");

        Assert.That(RequireType(fixture, "SelectedType").EffectiveKind,
            Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(RequireValue(fixture, "selectedValue").EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(1)));
        AsnType selectedValues = fixture.Tables[0]
            .FindSymbol("SelectedValues", AsnAssignmentKind.ValueSet)
            ?.ResolvedValueSetType ?? throw new InvalidOperationException();
        Assert.That(selectedValues.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(selectedValues.Constraints.Single().Kind,
            Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(selectedValues.Constraints.Single().Elements
            .Select(item => item.Value?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        AsnType selectedDeclaredValues = fixture.Tables[0]
            .FindSymbol("SelectedDeclaredValues", AsnAssignmentKind.ValueSet)
            ?.ResolvedValueSetType ?? throw new InvalidOperationException();
        Assert.That(selectedDeclaredValues.Constraints.Single().Elements
            .All(item => item.Kind == AsnConstraintKind.ContainedSubtype), Is.True);
        Assert.That(RequireObject(fixture, "selectedChild").Object.FieldSettings[0]
                .Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(11)));
        Assert.That(RequireSet(fixture, "SelectedChildren").ObjectSet.Objects
            .Select(item => item.Name),
            Is.EqualTo(new[] { "childOne", "childTwo" }));
        Assert.That(RequireSet(fixture, "SelectedNestedChildren").ObjectSet.Objects
            .Select(item => item.Name),
            Is.EqualTo(new[] { "childOne", "childTwo" }));
        Assert.That(RequireValue(fixture, "chainedCode").EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(11)));
        ResolvedTypeSyntax classField = RequireType(fixture, "ClassIdType");
        Assert.That(classField.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(classField.ObjectClassField?.Field?.Name, Is.EqualTo("&id"));
        ResolvedTypeSyntax nestedClassField = RequireType(fixture, "NestedCodeType");
        Assert.That(nestedClassField.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(nestedClassField.ObjectClassField?.ObjectClassSymbol?.Name,
            Is.EqualTo("CHILD"));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ResolverAppliesSelectionsInsideObjectFieldSettings()
    {
        ResolutionFixture fixture = Resolve(
            "Selections DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "ITEM ::= CLASS { &Type, &id INTEGER, &child CHILD, &Children CHILD }; "
                + "COPY ::= CLASS { &Type, &id INTEGER, &Ids INTEGER, "
                + "&child CHILD, &Children CHILD }; "
                + "childOne CHILD ::= { &code 21 }; "
                + "childTwo CHILD ::= { &code 22 }; "
                + "first ITEM ::= { &Type UTF8String, &id 4, &child childOne, "
                + "&Children { childOne | childTwo } }; "
                + "second ITEM ::= { &Type BOOLEAN, &id 5, &child childTwo, "
                + "&Children { childTwo } }; "
                + "Items ITEM ::= { first | second }; "
                + "copy COPY ::= { &Type first.&Type, &id first.&id, "
                + "&Ids Items.&id, &child first.&child, "
                + "&Children first.&Children } END");
        AsnObjectFieldSetting[] settings = RequireObject(fixture, "copy")
            .Object.FieldSettings;

        Assert.That(settings[0].Type?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(settings[1].Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(4)));
        Assert.That(settings[2].ValueSetType?.Constraints.Single().Elements
            .Select(item => item.Value?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 4, 5 }));
        Assert.That(settings[3].Object?.Name, Is.EqualTo("childOne"));
        Assert.That(settings[4].ObjectSet?.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "childOne", "childTwo" }));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ResolverRejectsUnknownFieldsInvalidChainsAndGovernorMismatches()
    {
        ResolutionFixture fixture = Resolve(
            "Selections DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "ITEM ::= CLASS { &id INTEGER, &child CHILD }; "
                + "childOne CHILD ::= { &code 1 }; "
                + "first ITEM ::= { &id 7, &child childOne }; "
                + "wrong BOOLEAN ::= first.&id; "
                + "UnknownType ::= first.&Missing; "
                + "InvalidChain ::= first.&id.&Type END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0050"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0051"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0052"));
        Assert.That(diagnostics.Where(item => item.Code == "ASNB0050"
                || item.Code == "ASNB0051"
                || item.Code == "ASNB0052")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void ResolverRejectsAmbiguousScalarSelectionFromObjectSet()
    {
        ResolutionFixture fixture = Resolve(
            "Selections DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &Type }; "
                + "PARENT ::= CLASS { &Children CHILD }; "
                + "first CHILD ::= { &Type INTEGER }; "
                + "second CHILD ::= { &Type BOOLEAN }; "
                + "parent PARENT ::= { &Children { first | second } }; "
                + "Selected ::= parent.&Children.&Type END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0054"));
        Assert.That(diagnostics.Single(item => item.Code == "ASNB0054").Span,
            Is.Not.Null);
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolvedValueSyntax RequireValue(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.Value)?.ResolvedValue
            ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectSyntax RequireObject(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.Object)?.DeclaredObjectSyntax
            ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectSetSyntax RequireSet(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.ObjectSet)?.DeclaredObjectSetSyntax
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "information-from-objects.asn",
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
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private static string FormatDiagnostics(ResolutionFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
