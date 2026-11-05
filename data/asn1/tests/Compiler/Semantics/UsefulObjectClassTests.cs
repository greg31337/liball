using asn1;

namespace asn1.Tests;

public class UsefulObjectClassTests
{
    [Test]
    public void ResolverMaterializesTypeIdentifierAndDerivedClassSemantics()
    {
        ResolutionFixture fixture = Resolve(
            "Useful DEFINITIONS ::= BEGIN "
                + "direct TYPE-IDENTIFIER ::= { INTEGER IDENTIFIED BY { 1 2 3 } }; "
                + "CONTENT-TYPE ::= TYPE-IDENTIFIER; "
                + "derived CONTENT-TYPE ::= { UTF8String IDENTIFIED BY { 1 2 4 } }; "
                + "Types TYPE-IDENTIFIER ::= { direct }; "
                + "Payload ::= TYPE-IDENTIFIER.&Type ({Types}) END");
        ParsedInformationObjectSyntax direct = RequireObject(fixture, "direct");
        ParsedInformationObjectSyntax derived = RequireObject(fixture, "derived");
        ParsedObjectClassSyntax contentType = RequireClass(fixture, "CONTENT-TYPE");
        ResolvedTypeSyntax payload = RequireType(fixture, "Payload");

        Assert.That(direct.Object.ObjectClass?.Name, Is.EqualTo("TYPE-IDENTIFIER"));
        Assert.That(direct.Object.ObjectClass?.Fields.Select(field => field.Name),
            Is.EqualTo(new[] { "&id", "&Type" }));
        Assert.That(direct.Object.FieldSettings.Select(setting => setting.FieldName),
            Is.EqualTo(new[] { "&id", "&Type" }));
        Assert.That(direct.Object.FieldSettings[0].Value?.TextValue,
            Is.EqualTo("1.2.3"));
        Assert.That(direct.Object.FieldSettings[1].Type?.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(contentType.ObjectClass.Name, Is.EqualTo("CONTENT-TYPE"));
        Assert.That(derived.Object.ObjectClass, Is.SameAs(contentType.ObjectClass));
        Assert.That(derived.Object.ObjectClass, Is.Not.SameAs(direct.Object.ObjectClass));
        Assert.That(payload.EffectiveKind, Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(payload.ObjectClassField?.ObjectClassSymbol, Is.Null);
        Assert.That(payload.ObjectClassField?.ObjectClassSyntax?.ObjectClass.Name,
            Is.EqualTo("TYPE-IDENTIFIER"));
        Assert.That(payload.ObjectClassField?.Field?.Name, Is.EqualTo("&Type"));
        Assert.That(payload.TableConstraints.Single().DispatchEntries.Single().Type?.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ResolverAppliesAbstractSyntaxPropertyDefaultAndExplicitSetting()
    {
        ResolutionFixture fixture = Resolve(
            "Useful DEFINITIONS ::= BEGIN "
                + "plain ABSTRACT-SYNTAX ::= { UTF8String IDENTIFIED BY { 2 5 4 3 } }; "
                + "handling ABSTRACT-SYNTAX ::= { INTEGER IDENTIFIED BY { 1 3 6 1 } "
                + "HAS PROPERTY { handles-invalid-encodings } }; "
                + "AbstractValue ::= INSTANCE OF ABSTRACT-SYNTAX END");
        ParsedInformationObjectSyntax plain = RequireObject(fixture, "plain");
        ParsedInformationObjectSyntax handling = RequireObject(fixture, "handling");
        ResolvedTypeSyntax instance = RequireType(fixture, "AbstractValue");

        Assert.That(plain.Object.ObjectClass?.Name, Is.EqualTo("ABSTRACT-SYNTAX"));
        Assert.That(plain.Object.ObjectClass?.Fields.Select(field => field.Name),
            Is.EqualTo(new[] { "&id", "&Type", "&property" }));
        Assert.That(plain.Object.ObjectClass?.Fields[2].Type?.NamedValues.Single().Name,
            Is.EqualTo("handles-invalid-encodings"));
        Assert.That(plain.Object.FieldSettings[2].FieldName, Is.EqualTo("&property"));
        Assert.That(plain.Object.FieldSettings[2].Value?.Kind,
            Is.EqualTo(AsnValueKind.BitString));
        Assert.That(plain.Object.FieldSettings[2].Value?.NamedValues, Is.Empty);
        Assert.That(handling.Object.FieldSettings[2].Value?.NamedValues.Single().Name,
            Is.EqualTo("handles-invalid-encodings"));
        Assert.That(instance.Components[0].TypeResolution?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.ObjectIdentifier));
        Assert.That(instance.ObjectClassField?.ObjectClassSyntax?.ObjectClass.Name,
            Is.EqualTo("ABSTRACT-SYNTAX"));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ResolverMaterializesInstanceOfStructureAndValidatesValues()
    {
        ResolutionFixture fixture = Resolve(
            "Useful DEFINITIONS ::= BEGIN "
                + "Wrapped ::= INSTANCE OF TYPE-IDENTIFIER; "
                + "wrapped Wrapped ::= { type-id { 1 2 3 }, value INTEGER: 9 } END");
        ResolvedTypeSyntax wrappedType = RequireType(fixture, "Wrapped");
        SymbolTableEntry wrappedSymbol = fixture.Tables[0]
            .FindSymbol("wrapped", AsnAssignmentKind.Value)
            ?? throw new InvalidOperationException();
        ResolvedValueSyntax wrappedValue = wrappedSymbol.ResolvedValue
            ?? throw new InvalidOperationException();

        Assert.That(wrappedType.EffectiveKind, Is.EqualTo(AsnTypeKind.InstanceOf));
        Assert.That(wrappedType.EffectiveType?.Components.Select(component => component.Name),
            Is.EqualTo(new[] { "type-id", "value" }));
        Assert.That(wrappedType.Components[0].TypeResolution?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.ObjectIdentifier));
        Assert.That(wrappedType.Components[1].Component?.Type?.Kind,
            Is.EqualTo(AsnTypeKind.Tagged));
        Assert.That(wrappedType.Components[1].Component?.Type?.Tag?.Class,
            Is.EqualTo(AsnTagClass.ContextSpecific));
        Assert.That(wrappedType.Components[1].Component?.Type?.Tag?.Mode,
            Is.EqualTo(AsnTaggingMode.Explicit));
        Assert.That(wrappedType.Components[1].Component?.Type?.Tag?.Number,
            Is.EqualTo(System.Numerics.BigInteger.Zero));
        Assert.That(wrappedType.Components[1].TypeResolution?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(wrappedType.ObjectClassField?.Field?.Name, Is.EqualTo("&Type"));
        Assert.That(wrappedSymbol.GoverningType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.InstanceOf));
        Assert.That(wrappedSymbol.GoverningType?.Components, Has.Length.EqualTo(2));
        Assert.That(wrappedSymbol.DeclaredValueSyntax?.Value.Kind,
            Is.EqualTo(AsnValueKind.InstanceOf));
        Assert.That(wrappedValue.ComponentValues.Select(value => value.Name),
            Is.EqualTo(new[] { "type-id", "value" }));
        Assert.That(wrappedValue.ComponentValues[1].Value?.Items.Single()
                .EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(9)));
        Assert.That(fixture.Values.Success, Is.True, FormatDiagnostics(fixture));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture));
    }

    [Test]
    public void ResolverRejectsInstanceOfClassesWithoutRequiredDispatchFields()
    {
        ResolutionFixture fixture = Resolve(
            "Useful DEFINITIONS ::= BEGIN "
                + "NO-UNIQUE ::= CLASS { &id INTEGER, &Type }; "
                + "NO-TYPE ::= CLASS { &id INTEGER UNIQUE }; "
                + "MissingUnique ::= INSTANCE OF NO-UNIQUE; "
                + "MissingType ::= INSTANCE OF NO-TYPE END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(item => item.Code == "ASNB0057"), Is.EqualTo(1));
        Assert.That(diagnostics.Count(item => item.Code == "ASNB0058"), Is.EqualTo(1));
        Assert.That(diagnostics.Where(item => item.Code == "ASNB0057"
                || item.Code == "ASNB0058").All(item => item.Span != null), Is.True);
    }

    private static ParsedObjectClassSyntax RequireClass(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.ObjectClass)?.ResolvedObjectClassSyntax
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

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "useful-object-classes.asn",
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
        return string.Join(" | ", diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
