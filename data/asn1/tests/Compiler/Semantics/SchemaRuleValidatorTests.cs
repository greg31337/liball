using asn1;

namespace asn1.Tests;

public class SchemaRuleValidatorTests
{
    [Test]
    public void ValidatorAssignsEnumeratedNumbersAcrossRootAndExtensions()
    {
        ModuleSyntax module = ParseModule(
            "enumerated.asn",
            "Enumerated DEFINITIONS ::= BEGIN "
                + "Status ::= ENUMERATED { pending, explicit(4), running, "
                + "..., added, last(8) }; "
                + "Compact ::= ENUMERATED { first, second, reserved(0) }; "
                + "Holder ::= SEQUENCE { state Status DEFAULT running }; "
                + "selected Status ::= running; future Status ::= added END");
        ResolutionFixture fixture = Resolve([module]);
        ParsedTypeSyntax status = GetTypeSyntax(fixture.Tables[0], "Status");
        ParsedTypeSyntax compact = GetTypeSyntax(fixture.Tables[0], "Compact");
        ResolvedValueSyntax selected = GetValue(fixture.Tables[0], "selected");
        ResolvedValueSyntax future = GetValue(fixture.Tables[0], "future");
        ResolvedTypeSyntax holder = fixture.Tables[0]
            .FindSymbol("Holder", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(status.RootNamedValues.Select(item => item.Value?.IntegerValue.ToString()),
            Is.EqualTo(new[] { "0", "4", "1" }));
        Assert.That(status.ExtensionNamedValues.Select(item => item.Value?.IntegerValue.ToString()),
            Is.EqualTo(new[] { "2", "8" }));
        Assert.That(compact.RootNamedValues.Select(item => item.Value?.IntegerValue.ToString()),
            Is.EqualTo(new[] { "1", "2", "0" }));
        Assert.That(selected.EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(1)));
        Assert.That(future.EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(2)));
        Assert.That(holder.Components[0].DefaultValue?.EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(1)));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ValidatorRejectsDuplicateAndRegressingEnumeratedNumbers()
    {
        ModuleSyntax module = ParseModule(
            "bad-enumerated.asn",
            "BadEnumerated DEFINITIONS ::= BEGIN "
                + "DuplicateName ::= ENUMERATED { same, same }; "
                + "DuplicateRoot ::= ENUMERATED { first(1), second(1) }; "
                + "DuplicateExtension ::= ENUMERATED { first, second, ..., third(0) }; "
                + "RegressingExtension ::= ENUMERATED { root, ..., added(5), later(4) } END");
        ResolutionFixture fixture = Resolve([module]);
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(codes, Does.Contain("ASNS0048"));
        Assert.That(codes, Does.Contain("ASNS0049"));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Where(diagnostic => diagnostic.Code == "ASNS0048"
                    || diagnostic.Code == "ASNS0049")
                .All(diagnostic => diagnostic.RelatedLocations.Length == 1),
            Is.True);
        AsnDiagnostic duplicateName = fixture.Diagnostics.GetDiagnostics()
            .First(diagnostic => diagnostic.Code == "ASNS0048"
                && diagnostic.Message.Contains("identifier"));
        Assert.That(duplicateName.Span, Is.Not.Null);
        Assert.That(
            duplicateName.RelatedLocations[0].Span.Start.Offset,
            Is.LessThan(duplicateName.Span == null
                ? int.MaxValue
                : duplicateName.Span.Start.Offset));
    }

    [Test]
    public void ValidatorAcceptsConsistentVersionedExtensionGroups()
    {
        ModuleSyntax module = ParseModule(
            "versions.asn",
            "Versions DEFINITIONS ::= BEGIN "
                + "Data ::= SEQUENCE { root INTEGER, ..., "
                + "[[ 2: added INTEGER ]], [[ 3: later BOOLEAN ]], ... }; "
                + "Option ::= CHOICE { root INTEGER, ..., "
                + "[[ 2: added BOOLEAN ]], ... }; "
                + "Trailing ::= SET { ..., ..., tail UTF8String } END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ValidatorRejectsInconsistentVersionBracketUse()
    {
        ModuleSyntax module = ParseModule(
            "bad-versions.asn",
            "BadVersions DEFINITIONS ::= BEGIN "
                + "Versioned ::= SEQUENCE { root INTEGER, ..., [[ 2: added INTEGER ]] }; "
                + "Ungrouped ::= SEQUENCE { root INTEGER, ..., added INTEGER }; "
                + "Unnumbered ::= CHOICE { root INTEGER, ..., [[ added BOOLEAN ]] }; "
                + "TooEarly ::= SET { root INTEGER, ..., [[ 1: added INTEGER ]] }; "
                + "Descending ::= SEQUENCE { root INTEGER, ..., "
                + "[[ 3: first INTEGER ]], [[ 2: second BOOLEAN ]] } END");
        ResolutionFixture fixture = Resolve([module]);
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(codes, Does.Contain("ASNS0051"));
        Assert.That(codes, Does.Contain("ASNS0052"));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Where(diagnostic => diagnostic.Code == "ASNS0051")
                .All(diagnostic => diagnostic.RelatedLocations.Length == 1),
            Is.True);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Any(diagnostic => diagnostic.Code == "ASNS0052"
                    && diagnostic.RelatedLocations.Length == 1),
            Is.True);
    }

    [Test]
    public void ValidatorRejectsMissingChoiceAndEnumeratedExtensionRoots()
    {
        ModuleSyntax module = ParseModule(
            "bad-roots.asn",
            "BadRoots DEFINITIONS ::= BEGIN "
                + "ChoiceOnlyExtension ::= CHOICE { ..., added INTEGER }; "
                + "EnumeratedOnlyExtension ::= ENUMERATED { ..., added } END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Count(
                diagnostic => diagnostic.Code == "ASNS0045"),
            Is.EqualTo(2));
    }

    [Test]
    public void ValidatorRejectsDuplicateExpandedComponentNames()
    {
        ModuleSyntax module = ParseModule(
            "duplicate-components.asn",
            "DuplicateComponents DEFINITIONS ::= BEGIN "
                + "Base ::= SEQUENCE { item INTEGER }; "
                + "Duplicate ::= SEQUENCE { COMPONENTS OF Base, item BOOLEAN }; "
                + "DuplicateChoice ::= CHOICE { selected INTEGER, selected BOOLEAN } END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Count(
                diagnostic => diagnostic.Code == "ASNS0047"),
            Is.EqualTo(2));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Where(diagnostic => diagnostic.Code == "ASNS0047")
                .All(diagnostic => diagnostic.RelatedLocations.Length == 1),
            Is.True);
        AsnDiagnostic duplicate = fixture.Diagnostics.GetDiagnostics()
            .First(diagnostic => diagnostic.Code == "ASNS0047");
        Assert.That(duplicate.Span, Is.Not.Null);
        Assert.That(
            duplicate.RelatedLocations[0].Span.Start.Offset,
            Is.LessThan(duplicate.Span == null
                ? int.MaxValue
                : duplicate.Span.Start.Offset));
    }

    [Test]
    public void ValidatorRejectsInconsistentOptionalAndDefaultState()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SchemaRuleValidator validator = new SchemaRuleValidator(diagnostics);
        ParsedComponentSyntax component = new ParsedComponentSyntax
        {
            Component = new AsnComponent
            {
                Name = "value",
                Optional = true,
                HasDefaultValue = true,
                DefaultValue = new AsnValue
                {
                    Kind = AsnValueKind.Integer,
                    HasIntegerValue = true,
                    IntegerValue = System.Numerics.BigInteger.One
                }
            }
        };

        bool success = validator.ValidateComponentOptionality(
            AsnTypeKind.Sequence,
            component);

        Assert.That(success, Is.False);
        Assert.That(
            diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNS0050"));
    }

    [Test]
    public void ValidatorRejectsInconsistentExtensionMarkerState()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SchemaRuleValidator validator = new SchemaRuleValidator(diagnostics);
        ParsedComponentSyntax component = new ParsedComponentSyntax
        {
            Component = new AsnComponent { Name = "added", Extension = true },
            Extension = true,
            AfterExtensionEnd = true,
            ExtensionGroupIndex = 0
        };
        ParsedTypeSyntax syntax = new ParsedTypeSyntax
        {
            Type = new AsnType { Kind = AsnTypeKind.Sequence },
            ComponentSyntax = [component],
            HasExtensionEndMarker = true
        };
        ResolvedTypeSyntax resolution = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            Components = [new ResolvedComponentSyntax
            {
                Component = component.Component
            }]
        };

        bool success = validator.ValidateStructuredType(resolution, syntax, false);

        Assert.That(success, Is.False);
        Assert.That(
            diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNS0046"));
    }

    private static ResolutionFixture Resolve(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        new AssignmentClassifier(diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult result = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Result = result,
            Tables = tables
        };
    }

    private static ParsedTypeSyntax GetTypeSyntax(SymbolTable table, string name)
    {
        return table.FindSymbol(name, AsnAssignmentKind.Type)?.DeclaredTypeSyntax
            ?? throw new InvalidOperationException();
    }

    private static ResolvedValueSyntax GetValue(SymbolTable table, string name)
    {
        return table.FindSymbol(name, AsnAssignmentKind.Value)?.ResolvedValue
            ?? throw new InvalidOperationException();
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(
            System.Environment.NewLine,
            diagnostics.GetDiagnostics().Select(
                diagnostic => diagnostic.Code + ": " + diagnostic.Message));
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
        internal ValueResolutionResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
