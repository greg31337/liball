using asn1;

namespace asn1.Tests;

public class ObjectClassBinderTests
{
    [Test]
    public void BinderResolvesEveryFieldKindAndForwardGovernor()
    {
        BindingFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN "
                + "Payload ::= INTEGER; "
                + "PARENT ::= CLASS { "
                + "&open &Later DEFAULT TRUE, "
                + "&OpenValues &Later DEFAULT { TRUE }, "
                + "&child CHILD OPTIONAL, "
                + "&Children CHILD OPTIONAL, "
                + "&deep &child.&Type DEFAULT \"text\", "
                + "&fixed Payload DEFAULT 7, "
                + "&FixedValues Payload OPTIONAL, "
                + "&id INTEGER UNIQUE, "
                + "&Later DEFAULT BOOLEAN }; "
                + "CHILD ::= CLASS { &Type DEFAULT UTF8String }; "
                + "ALIAS ::= PARENT END");
        ParsedObjectClassSyntax parent = RequireClass(fixture, "PARENT");
        ParsedObjectClassSyntax alias = RequireClass(fixture, "ALIAS");
        AsnObjectClassField[] fields = parent.ObjectClass.Fields;

        Assert.That(fields.Select(field => field.Kind), Is.EqualTo(new[]
        {
            AsnObjectFieldKind.VariableTypeValue,
            AsnObjectFieldKind.VariableTypeValueSet,
            AsnObjectFieldKind.Object,
            AsnObjectFieldKind.ObjectSet,
            AsnObjectFieldKind.VariableTypeValue,
            AsnObjectFieldKind.FixedTypeValue,
            AsnObjectFieldKind.FixedTypeValueSet,
            AsnObjectFieldKind.FixedTypeValue,
            AsnObjectFieldKind.Type
        }));
        Assert.That(fields[0].GovernorFieldPath, Is.EqualTo(new[] { "&Later" }));
        Assert.That(fields[0].DefaultSetting?.Value?.Kind, Is.EqualTo(AsnValueKind.Boolean));
        Assert.That(fields[4].GovernorFieldPath,
            Is.EqualTo(new[] { "&child", "&Type" }));
        Assert.That(fields[2].ObjectClass?.Name, Is.EqualTo("CHILD"));
        Assert.That(fields[3].ObjectClass, Is.SameAs(fields[2].ObjectClass));
        Assert.That(fields[5].Type?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(fields[5].DefaultSetting?.Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(7)));
        Assert.That(fields[8].DefaultSetting?.Type?.Kind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(alias, Is.SameAs(parent));
        Assert.That(fixture.Values.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
    }

    [Test]
    public void BinderRejectsInvalidUniqueOptionalityAndFieldGovernors()
    {
        BindingFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN "
                + "BAD ::= CLASS { "
                + "&id INTEGER UNIQUE DEFAULT 1, "
                + "&other INTEGER UNIQUE, "
                + "&Type OPTIONAL, "
                + "&required &Type, "
                + "&Values &missing OPTIONAL, "
                + "&link INTEGER, "
                + "&via &link.&Type OPTIONAL } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNB0023"),
            Is.EqualTo(2));
        Assert.That(diagnostics.Count(item => item.Code == "ASNB0024"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Count(item => item.Code == "ASNB0022"),
            Is.EqualTo(2));
        Assert.That(diagnostics.Where(item => item.Code == "ASNB0022"
                || item.Code == "ASNB0023"
                || item.Code == "ASNB0024")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void BinderRejectsDuplicateFieldsUnknownClassesAndMandatoryRecursion()
    {
        BindingFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN "
                + "BAD ::= CLASS { "
                + "&value INTEGER, "
                + "&value BOOLEAN, "
                + "&child MISSING, "
                + "&self BAD }; "
                + "LEFT ::= CLASS { &right RIGHT }; "
                + "RIGHT ::= CLASS { &left LEFT } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0018"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0019"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0024"));
    }

    [TestCase("&Type OPTIONAL, &value &Type DEFAULT 1")]
    [TestCase("&Type, &Values &Type DEFAULT { 1 | 2 }")]
    public void BinderRequiresDefaultedTypeGovernorForVariableDefaults(string fields)
    {
        BindingFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN BAD ::= CLASS { " + fields + " } END");

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNB0025"));
    }

    [Test]
    public void BinderTypeChecksDefaultsAfterResolvingDefinedGovernors()
    {
        BindingFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN "
                + "Payload ::= INTEGER; "
                + "BAD ::= CLASS { &value Payload DEFAULT TRUE } END");

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNB0025"));
    }

    private static ParsedObjectClassSyntax RequireClass(
        BindingFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.ObjectClass)?.ResolvedObjectClassSyntax
            ?? throw new InvalidOperationException();
    }

    private static BindingFixture Bind(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "object-class-binding.asn",
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
        return new BindingFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class BindingFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
