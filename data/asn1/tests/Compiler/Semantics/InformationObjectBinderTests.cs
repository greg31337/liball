using asn1;

namespace asn1.Tests;

public class InformationObjectBinderTests
{
    [Test]
    public void BinderAppliesDefaultsAndBindsEverySettingKind()
    {
        BindingFixture fixture = Bind(
            "Objects DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "ITEM ::= CLASS { "
                + "&Type DEFAULT BOOLEAN, "
                + "&required INTEGER, "
                + "&open &Type, "
                + "&Values INTEGER DEFAULT { 1 | 2 }, "
                + "&child CHILD DEFAULT { &code 9 }, "
                + "&Children CHILD DEFAULT { { &code 10 } }, "
                + "&optional UTF8String OPTIONAL }; "
                + "entry ITEM ::= { &required 7, &open TRUE } END");
        ParsedInformationObjectSyntax entry = RequireObject(fixture, "entry");
        AsnObjectFieldSetting[] settings = entry.Object.FieldSettings;

        Assert.That(settings.Select(item => item.FieldName), Is.EqualTo(new[]
        {
            "&Type", "&required", "&open", "&Values", "&child", "&Children"
        }));
        Assert.That(settings[0].Type?.Kind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(settings[1].Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(7)));
        Assert.That(settings[2].Value?.Kind, Is.EqualTo(AsnValueKind.Boolean));
        Assert.That(settings[3].ValueSetType?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(settings[3].ValueSetType?.Constraints.Length, Is.EqualTo(1));
        Assert.That(settings[4].Object?.FieldSettings.Single().Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(9)));
        Assert.That(settings[5].ObjectSet?.Objects.Single()
                .FieldSettings.Single().Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(10)));
        Assert.That(fixture.Values.Success, Is.True,
            FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void BinderBindsDefinedSyntaxUsingEarlierTypeSettings()
    {
        BindingFixture fixture = Bind(
            "Objects DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "DEFINED ::= CLASS { &Type, &value &Type, &child CHILD } "
                + "WITH SYNTAX { TYPE &Type VALUE &value LINK &child }; "
                + "entry DEFINED ::= { TYPE UTF8String VALUE \"ok\" "
                + "LINK { &code 3 } } END");
        AsnObjectFieldSetting[] settings = RequireObject(fixture, "entry")
            .Object.FieldSettings;

        Assert.That(settings.Select(item => item.FieldName),
            Is.EqualTo(new[] { "&Type", "&value", "&child" }));
        Assert.That(settings[0].Type?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(settings[1].Value?.TextValue, Is.EqualTo("ok"));
        Assert.That(settings[2].Object?.ObjectClass?.Name, Is.EqualTo("CHILD"));
        Assert.That(settings[2].Object?.FieldSettings.Single().Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(3)));
        Assert.That(fixture.Values.Success, Is.True,
            FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void BinderRejectsMissingDuplicateAndUnknownSettings()
    {
        BindingFixture fixture = Bind(
            "Objects DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &required INTEGER, &optional BOOLEAN OPTIONAL }; "
                + "bad ITEM ::= { &optional TRUE, &optional FALSE, &unknown 3 } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0036"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0037"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0038"));
        Assert.That(diagnostics.Where(item => item.Code == "ASNB0036"
                || item.Code == "ASNB0037"
                || item.Code == "ASNB0038")
            .All(item => item.Span != null), Is.True);
    }

    [Test]
    public void BinderRejectsMismatchedValueAndObjectGovernors()
    {
        BindingFixture fixture = Bind(
            "Objects DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "OTHER ::= CLASS { &flag BOOLEAN }; "
                + "ITEM ::= CLASS { &id INTEGER, &child CHILD }; "
                + "flag BOOLEAN ::= TRUE; "
                + "other OTHER ::= { &flag TRUE }; "
                + "bad ITEM ::= { &id flag, &child other } END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0035"));
        Assert.That(diagnostics.Select(item => item.Code), Does.Contain("ASNB0040"));
    }

    [Test]
    public void BinderValidatesLinkedDefaultsWithoutAnObjectUse()
    {
        BindingFixture valid = Bind(
            "ValidDefaults DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "child CHILD ::= { &code 7 }; "
                + "PARENT ::= CLASS { &child CHILD DEFAULT child, "
                + "&Children CHILD DEFAULT { child } } END");
        ParsedObjectClassSyntax parent = valid.Tables[0]
            .FindSymbol("PARENT", AsnAssignmentKind.ObjectClass)
            ?.ResolvedObjectClassSyntax ?? throw new InvalidOperationException();

        Assert.That(valid.Values.Success, Is.True, FormatDiagnostics(valid));
        Assert.That(parent.ObjectClass.Fields[0].DefaultSetting?.Object?.Name,
            Is.EqualTo("child"));
        Assert.That(parent.ObjectClass.Fields[1].DefaultSetting?.ObjectSet?.Objects
            .Single().Name, Is.EqualTo("child"));

        BindingFixture invalid = Bind(
            "InvalidDefaults DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "OTHER ::= CLASS { &flag BOOLEAN }; "
                + "other OTHER ::= { &flag TRUE }; "
                + "PARENT ::= CLASS { &child CHILD DEFAULT other, "
                + "&Children CHILD DEFAULT { other } } END");
        string[] codes = invalid.Diagnostics.GetDiagnostics()
            .Select(item => item.Code)
            .ToArray();

        Assert.That(invalid.Values.Success, Is.False);
        Assert.That(codes, Does.Contain("ASNB0042"));
        Assert.That(codes, Does.Contain("ASNB0043"));

        BindingFixture recursive = Bind(
            "RecursiveDefault DEFINITIONS ::= BEGIN "
                + "NODE ::= CLASS { &next NODE DEFAULT { } } END");

        Assert.That(recursive.Values.Success, Is.False);
        Assert.That(recursive.Diagnostics.GetDiagnostics()
            .Any(item => item.Code == "ASNB0042"
                && item.Message.Contains("recursively")), Is.True);
    }

    private static ParsedInformationObjectSyntax RequireObject(
        BindingFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.Object)?.DeclaredObjectSyntax
            ?? throw new InvalidOperationException();
    }

    private static BindingFixture Bind(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "information-object-binding.asn",
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

    private static string FormatDiagnostics(BindingFixture fixture)
    {
        return string.Join(" | ", fixture.Diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class BindingFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
