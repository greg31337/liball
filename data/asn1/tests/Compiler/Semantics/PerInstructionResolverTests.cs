using asn1;

namespace asn1.Tests;

public class PerInstructionResolverTests
{
    [Test]
    public void ResolverAppliesReferenceSpecificityNegationAndPrefixOrder()
    {
        ResolutionFixture fixture = Resolve(
            "Resolution DEFINITIONS ::= BEGIN\n"
                + "Base ::= [PER:BASE original] INTEGER;\n"
                + "Alias ::= Base;\n"
                + "Cleared ::= [PER:NOT RESET] Base;\n"
                + "PrefixOrder ::= [PER:MODE outer] [PER:MODE inner] INTEGER;\n"
                + "TargetThenPrefix ::= [PER:MODE prefix] INTEGER\n"
                + "ENCODING-CONTROL PER\n"
                + "[MODE alias] Alias\n"
                + "[MODE base] Base\n"
                + "[MODE targeted] TargetThenPrefix\n"
                + "END");

        ResolvedTypeSyntax baseType = FindType(fixture, 0, "Base");
        ResolvedTypeSyntax alias = FindType(fixture, 0, "Alias");
        ResolvedTypeSyntax cleared = FindType(fixture, 0, "Cleared");
        ResolvedTypeSyntax prefixOrder = FindType(fixture, 0, "PrefixOrder");
        ResolvedTypeSyntax targetThenPrefix = FindType(fixture, 0, "TargetThenPrefix");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(Names(baseType), Is.EqualTo(new[] { "MODE", "BASE" }));
            Assert.That(Arguments(baseType, "MODE"), Is.EqualTo(new[] { "base" }));
            Assert.That(Names(alias), Is.EqualTo(new[] { "BASE", "MODE" }));
            Assert.That(Arguments(alias, "MODE"), Is.EqualTo(new[] { "alias" }));
            Assert.That(cleared.FinalPerEncodingInstructions, Is.Empty);
            Assert.That(Names(prefixOrder), Is.EqualTo(new[] { "MODE" }));
            Assert.That(Arguments(prefixOrder, "MODE"), Is.EqualTo(new[] { "outer" }));
            Assert.That(Names(targetThenPrefix), Is.EqualTo(new[] { "MODE" }));
            Assert.That(Arguments(targetThenPrefix, "MODE"), Is.EqualTo(new[] { "prefix" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ResolverCombinesPositiveAssignmentsAndClearsAllOnNegation()
    {
        ResolutionFixture fixture = Resolve(
            "Combination DEFINITIONS ::= BEGIN\n"
                + "Replaced ::= INTEGER; Cleared ::= INTEGER\n"
                + "ENCODING-CONTROL PER\n"
                + "[FIRST one] Replaced\n"
                + "[SECOND two] Replaced\n"
                + "[FIRST three] Replaced\n"
                + "[FIRST one] Cleared\n"
                + "[SECOND two] Cleared\n"
                + "[NOT UNRELATED] Cleared\n"
                + "[THIRD four] Cleared\n"
                + "END");

        ResolvedTypeSyntax replaced = FindType(fixture, 0, "Replaced");
        ResolvedTypeSyntax cleared = FindType(fixture, 0, "Cleared");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(Names(replaced), Is.EqualTo(new[] { "SECOND", "FIRST" }));
            Assert.That(Arguments(replaced, "FIRST"), Is.EqualTo(new[] { "three" }));
            Assert.That(Names(cleared), Is.EqualTo(new[] { "THIRD" }));
            Assert.That(cleared.FinalPerEncodingInstructions[0].Negated, Is.False);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ResolverExpandsPathsBuiltInsAllAndContextTargets()
    {
        ResolutionFixture fixture = Resolve(
            "Targets DEFINITIONS ::= BEGIN\n"
                + "Root ::= SEQUENCE { first INTEGER, nested SEQUENCE { second INTEGER }, "
                + "items SEQUENCE OF INTEGER };\n"
                + "Other ::= INTEGER; Alias ::= Other\n"
                + "ENCODING-CONTROL PER\n"
                + "[BUILTIN integer] INTEGER\n"
                + "[PATH direct] Root.nested.second\n"
                + "[STAR element] Root.items.*\n"
                + "[DESCENDANTS nested] Root.nested.ALL\n"
                + "[CONTEXT selected] first, nested IN Root\n"
                + "[DEEP recursive] ALL IN Root\n"
                + "[COMPONENTS top] COMPONENTS IN Root\n"
                + "[GLOBAL every] ALL\n"
                + "END");

        ResolvedTypeSyntax root = FindType(fixture, 0, "Root");
        ResolvedTypeSyntax first = RequireComponent(root, "first");
        ResolvedTypeSyntax nested = RequireComponent(root, "nested");
        ResolvedTypeSyntax second = RequireComponent(nested, "second");
        ResolvedTypeSyntax items = RequireComponent(root, "items");
        ResolvedTypeSyntax element = items.InnerType ?? throw new InvalidOperationException();
        ResolvedTypeSyntax other = FindType(fixture, 0, "Other");
        ResolvedTypeSyntax alias = FindType(fixture, 0, "Alias");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(Names(root), Is.EqualTo(new[] { "GLOBAL" }));
            Assert.That(Names(first), Is.EqualTo(new[]
            {
                "BUILTIN", "CONTEXT", "DEEP", "COMPONENTS", "GLOBAL"
            }));
            Assert.That(Names(nested), Is.EqualTo(new[]
            {
                "CONTEXT", "DEEP", "COMPONENTS", "GLOBAL"
            }));
            Assert.That(Names(second), Is.EqualTo(new[]
            {
                "BUILTIN", "PATH", "DESCENDANTS", "DEEP", "GLOBAL"
            }));
            Assert.That(Names(items), Is.EqualTo(new[]
            {
                "DEEP", "COMPONENTS", "GLOBAL"
            }));
            Assert.That(Names(element), Is.EqualTo(new[]
            {
                "BUILTIN", "STAR", "DEEP", "GLOBAL"
            }));
            Assert.That(Names(other), Is.EqualTo(new[] { "BUILTIN", "GLOBAL" }));
            Assert.That(Names(alias), Is.EqualTo(new[] { "BUILTIN", "GLOBAL" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ResolverInheritsFinalInstructionsAcrossImportedAndSelectionReferences()
    {
        ResolutionFixture fixture = ResolveModules(
        [
            "Provider DEFINITIONS ::= BEGIN EXPORTS Base; "
                + "Base ::= [PER:ORIGIN provider] INTEGER END",
            "Consumer DEFINITIONS ::= BEGIN IMPORTS Base FROM Provider; "
                + "Use ::= Base; "
                + "Choices ::= CHOICE { selected [PER:ORIGIN choice] INTEGER }; "
                + "Selection ::= selected < Choices "
                + "ENCODING-CONTROL PER "
                + "[LOCAL use] Use "
                + "[LOCAL selection] Selection END"
        ]);

        ResolvedTypeSyntax provider = FindType(fixture, 0, "Base");
        ResolvedTypeSyntax use = FindType(fixture, 1, "Use");
        ResolvedTypeSyntax selection = FindType(fixture, 1, "Selection");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(Names(provider), Is.EqualTo(new[] { "ORIGIN" }));
            Assert.That(Names(use), Is.EqualTo(new[] { "ORIGIN", "LOCAL" }));
            Assert.That(Arguments(use, "ORIGIN"), Is.EqualTo(new[] { "provider" }));
            Assert.That(Names(selection), Is.EqualTo(new[] { "ORIGIN", "LOCAL" }));
            Assert.That(Arguments(selection, "ORIGIN"), Is.EqualTo(new[] { "choice" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ComponentsOfTargetsOverrideOnlyTheProducedComponentOccurrence()
    {
        ResolutionFixture fixture = Resolve(
            "Components DEFINITIONS ::= BEGIN "
                + "Base ::= SEQUENCE { value [PER:SOURCE original] INTEGER }; "
                + "Derived ::= SEQUENCE { COMPONENTS OF Base } "
                + "ENCODING-CONTROL PER "
                + "[LOCAL base] Base.value "
                + "[LOCAL derived] Derived.value END");

        ResolvedComponentSyntax original = RequireComponentSyntax(
            FindType(fixture, 0, "Base"),
            "value");
        ResolvedComponentSyntax produced = RequireComponentSyntax(
            FindType(fixture, 0, "Derived"),
            "value");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(original.FinalPerEncodingInstructions.Select(item => item.Name),
                Is.EqualTo(new[] { "LOCAL", "SOURCE" }));
            Assert.That(original.FinalPerEncodingInstructions
                .Single(item => item.Name == "LOCAL").Arguments,
                Is.EqualTo(new[] { "base" }));
            Assert.That(produced.FinalPerEncodingInstructions.Select(item => item.Name),
                Is.EqualTo(new[] { "SOURCE", "LOCAL" }));
            Assert.That(produced.FinalPerEncodingInstructions
                .Single(item => item.Name == "LOCAL").Arguments,
                Is.EqualTo(new[] { "derived" }));
        });
    }

    [Test]
    public void PerResolutionDoesNotOverwriteXerFinalInstructions()
    {
        ResolutionFixture fixture = Resolve(
            "Independent DEFINITIONS ::= BEGIN "
                + "Value ::= [XER:ATTRIBUTE] [PER:RULE detail] INTEGER "
                + "END");
        ResolvedTypeSyntax value = FindType(fixture, 0, "Value");
        XerInstructionResolutionResult xer = new XerInstructionResolver(fixture.Diagnostics)
            .ResolveInstructions(fixture.Tables);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Result.Success, Is.True, Format(fixture));
            Assert.That(xer.Success, Is.True, Format(fixture));
            Assert.That(value.FinalPerEncodingInstructions.Select(item => item.Name),
                Is.EqualTo(new[] { "RULE" }));
            Assert.That(value.FinalEncodingInstructions.Select(item => item.Kind),
                Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        });
    }

    [Test]
    public void ConstraintTypesRetainInstructionsOnlyForTypeAndContentsConstraints()
    {
        ResolutionFixture fixture = ResolveWithValues(
            "Restrictions DEFINITIONS ::= BEGIN "
                + "Payload ::= [PER:SOURCE inherited] INTEGER; "
                + "OpenPayload ::= ANY ([PER:KEEP type] Payload); "
                + "Octets ::= OCTET STRING "
                + "(CONTAINING [PER:KEEP contents] Payload); "
                + "Subtype ::= INTEGER "
                + "(INCLUDES [PER:DROP subtype] Payload); "
                + "Exception ::= INTEGER "
                + "(1..10 ! [PER:DROP exception] Payload : 5) END");

        ResolvedTypeSyntax payload = FindType(fixture, 0, "Payload");
        ResolvedTypeSyntax constrained = FindType(fixture, 0, "OpenPayload")
            .TypeConstraints.Single().ConstrainedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax contained = FindType(fixture, 0, "Octets")
            .ContentsConstraints.Single().ContainedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax subtype = FindType(fixture, 0, "Subtype")
            .EffectiveConstraint?.RootPartition?.Expression?.ResolvedContainedSubtype
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax exception = FindType(fixture, 0, "Exception")
            .ConstraintExceptions.Single().ExceptionType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(Names(payload), Is.EqualTo(new[] { "SOURCE" }));
            Assert.That(Names(constrained), Is.EqualTo(new[] { "SOURCE", "KEEP" }));
            Assert.That(Names(contained), Is.EqualTo(new[] { "SOURCE", "KEEP" }));
            Assert.That(subtype.FinalPerEncodingInstructions, Is.Empty);
            Assert.That(exception.FinalPerEncodingInstructions, Is.Empty);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ContentsConstraintWildcardTargetsTheContainedType()
    {
        ResolutionFixture fixture = ResolveWithValues(
            "ContentsTargets DEFINITIONS ::= BEGIN "
                + "Payload ::= [PER:SOURCE inherited] INTEGER; "
                + "Octets ::= OCTET STRING (CONTAINING Payload); "
                + "Bits ::= BIT STRING (CONTAINING Payload) "
                + "ENCODING-CONTROL PER "
                + "[TARGET octets] Octets.* "
                + "[TARGET bits] Bits.* END");

        ResolvedTypeSyntax octetContained = FindType(fixture, 0, "Octets")
            .ContentsConstraints.Single().ContainedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax bitContained = FindType(fixture, 0, "Bits")
            .ContentsConstraints.Single().ContainedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.Multiple(() =>
        {
            Assert.That(Names(octetContained), Is.EqualTo(new[] { "SOURCE", "TARGET" }));
            Assert.That(Names(bitContained), Is.EqualTo(new[] { "SOURCE", "TARGET" }));
            Assert.That(Arguments(octetContained, "TARGET"),
                Is.EqualTo(new[] { "octets" }));
            Assert.That(Arguments(bitContained, "TARGET"), Is.EqualTo(new[] { "bits" }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    private static ResolutionFixture Resolve(string source)
    {
        return ResolveModules([source]);
    }

    private static ResolutionFixture ResolveWithValues(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        diagnostics.ApplyCompilerOptions(new AsnCompilerOptions { EnableLegacyAny = true });
        ModuleSyntax module = ParseModule(source);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification =
            new AssignmentClassifier(diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(types.Success, Is.True, FormatDiagnostics(diagnostics));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        Assert.That(values.Success, Is.True, FormatDiagnostics(diagnostics));
        PerInstructionResolutionResult result = new PerInstructionResolver(diagnostics)
            .ResolveInstructions(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Result = result,
            Tables = tables
        };
    }

    private static ResolutionFixture ResolveModules(string[] sources)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        ModuleSyntax[] modules = new ModuleSyntax[sources.Length];
        int index = 0;
        while (index < sources.Length)
        {
            modules[index] = ParseModule(sources[index]);
            index = index + 1;
        }

        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True, FormatDiagnostics(diagnostics));
        PerInstructionResolutionResult result = new PerInstructionResolver(diagnostics)
            .ResolveInstructions(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Result = result,
            Tables = tables
        };
    }

    private static ModuleSyntax ParseModule(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "per-resolution.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed),
            FormatDiagnostics(session));
        Assert.That(result.Module, Is.Not.Null);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static ResolvedTypeSyntax FindType(
        ResolutionFixture fixture,
        int tableIndex,
        string name)
    {
        return fixture.Tables[tableIndex].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolvedTypeSyntax RequireComponent(ResolvedTypeSyntax type, string name)
    {
        return RequireComponentSyntax(type, name).TypeResolution
            ?? throw new InvalidOperationException();
    }

    private static ResolvedComponentSyntax RequireComponentSyntax(
        ResolvedTypeSyntax type,
        string name)
    {
        return type.Components.Single(item => item.Component?.Name == name);
    }

    private static string[] Names(ResolvedTypeSyntax type)
    {
        return type.FinalPerEncodingInstructions.Select(item => item.Name).ToArray();
    }

    private static string[] Arguments(ResolvedTypeSyntax type, string name)
    {
        return type.FinalPerEncodingInstructions.Single(item => item.Name == name).Arguments;
    }

    private static string Format(ResolutionFixture fixture)
    {
        return FormatDiagnostics(fixture.Diagnostics);
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join("\n", diagnostics.GetDiagnostics().Select(item =>
            item.Code + ": " + item.Message));
    }

    private static string FormatDiagnostics(IncrementalLexSession session)
    {
        return string.Join("\n", session.GetDiagnostics().Select(item =>
            item.Code + ": " + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal PerInstructionResolutionResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
