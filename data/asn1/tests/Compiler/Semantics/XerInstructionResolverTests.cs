using asn1;

namespace asn1.Tests;

public class XerInstructionResolverTests
{
    [Test]
    public void ResolverAppliesInheritanceTargetsNegationAndPrefixPrecedence()
    {
        ResolutionFixture fixture = Resolve(
            "Resolution DEFINITIONS ::= BEGIN\n"
                + "Base ::= [XER:ATTRIBUTE] INTEGER;\n"
                + "Alias ::= Base;\n"
                + "Override ::= [XER:NOT ATTRIBUTE] Base;\n"
                + "PrefixOrder ::= [XER:ATTRIBUTE] [XER:NOT ATTRIBUTE] INTEGER;\n"
                + "Container ::= SEQUENCE { value Base, items SEQUENCE OF INTEGER }\n"
                + "ENCODING-CONTROL XER\n"
                + "[ATTRIBUTE] Container.value\n"
                + "[NOT ATTRIBUTE] Container.value\n"
                + "[LIST] Container.items\n"
                + "END");

        ResolvedTypeSyntax baseType = FindType(fixture, "Base");
        ResolvedTypeSyntax alias = FindType(fixture, "Alias");
        ResolvedTypeSyntax overridden = FindType(fixture, "Override");
        ResolvedTypeSyntax prefixOrder = FindType(fixture, "PrefixOrder");
        ResolvedTypeSyntax container = FindType(fixture, "Container");
        ResolvedTypeSyntax value = RequireComponent(container, "value");
        ResolvedTypeSyntax items = RequireComponent(container, "items");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.That(Kinds(baseType), Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        Assert.That(Kinds(alias), Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        Assert.That(overridden.FinalEncodingInstructions, Is.Empty);
        Assert.That(Kinds(prefixOrder), Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        Assert.That(value.FinalEncodingInstructions, Is.Empty);
        Assert.That(Kinds(items), Is.EqualTo(new[] { AsnInstructionKind.List }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverExpandsBuiltInContextAndRecursiveComponentTargets()
    {
        ResolutionFixture fixture = Resolve(
            "Targets DEFINITIONS ::= BEGIN\n"
                + "Root ::= SEQUENCE { first INTEGER, nested SEQUENCE { second INTEGER } };\n"
                + "Other ::= INTEGER\n"
                + "ENCODING-CONTROL XER\n"
                + "[TEXT] INTEGER\n"
                + "[ATTRIBUTE] first, nested IN Root\n"
                + "[NAME AS UPPERCASED] Root.ALL\n"
                + "END");

        ResolvedTypeSyntax root = FindType(fixture, "Root");
        ResolvedTypeSyntax first = RequireComponent(root, "first");
        ResolvedTypeSyntax nested = RequireComponent(root, "nested");
        ResolvedTypeSyntax second = RequireComponent(nested, "second");
        ResolvedTypeSyntax other = FindType(fixture, "Other");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.That(Kinds(first), Is.EqualTo(new[]
        {
            AsnInstructionKind.Text,
            AsnInstructionKind.Attribute,
            AsnInstructionKind.Name
        }));
        Assert.That(Kinds(nested), Is.EqualTo(new[]
        {
            AsnInstructionKind.Attribute,
            AsnInstructionKind.Name
        }));
        Assert.That(Kinds(second), Is.EqualTo(new[]
        {
            AsnInstructionKind.Text,
            AsnInstructionKind.Name
        }));
        Assert.That(Kinds(other), Is.EqualTo(new[] { AsnInstructionKind.Text }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ImportListInstructionsModifyUsesWithoutChangingTheExportedType()
    {
        ResolutionFixture fixture = ResolveModules(
        [
            "Provider DEFINITIONS ::= BEGIN EXPORTS Base; "
                + "Base ::= [XER:ATTRIBUTE] INTEGER END",
            "Consumer DEFINITIONS ::= BEGIN IMPORTS Base FROM Provider; Use ::= Base "
                + "ENCODING-CONTROL XER [NOT ATTRIBUTE] ALL IMPORTS FROM Provider END"
        ]);
        ResolvedTypeSyntax exported = fixture.Tables[0]
            .FindSymbol("Base", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax use = fixture.Tables[1]
            .FindSymbol("Use", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.That(Kinds(exported), Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        Assert.That(use.FinalEncodingInstructions, Is.Empty);
    }

    [Test]
    public void ResolverDoesNotInheritNameOrNamespaceFromATypeReference()
    {
        ResolutionFixture fixture = Resolve(
            "Inheritance DEFINITIONS ::= BEGIN\n"
                + "Base ::= [XER:NAME AS \"base\"] [XER:NAMESPACE AS \"urn:base\"] "
                + "[XER:ATTRIBUTE] INTEGER;\n"
                + "Alias ::= Base\n"
                + "ENCODING-CONTROL XER\n"
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS]\n"
                + "END");

        ResolvedTypeSyntax baseType = FindType(fixture, "Base");
        ResolvedTypeSyntax alias = FindType(fixture, "Alias");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.That(Kinds(baseType), Is.EqualTo(new[]
        {
            AsnInstructionKind.Attribute,
            AsnInstructionKind.Namespace,
            AsnInstructionKind.Name
        }));
        Assert.That(Kinds(alias), Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
    }

    [Test]
    public void ResolverPropagatesModifiedEncodingsAndControlNamespace()
    {
        ResolutionFixture fixture = Resolve(
            "Globals DEFINITIONS ::= BEGIN\n"
                + "Number ::= REAL\n"
                + "ENCODING-CONTROL XER\n"
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS]\n"
                + "[GLOBAL-DEFAULTS CONTROL-NAMESPACE \"urn:control\" PREFIX \"xc\"]\n"
                + "[DECIMAL] Number\n"
                + "END");

        ResolvedTypeSyntax number = FindType(fixture, "Number");
        ModuleSyntax module = fixture.Tables[0].Module ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.That(module.XerModifiedEncodings, Is.True);
        Assert.That(module.XerControlNamespaceUri, Is.EqualTo("urn:control"));
        Assert.That(module.XerControlNamespacePrefix, Is.EqualTo("xc"));
        Assert.That(number.XerModifiedEncodings, Is.True);
        Assert.That(Kinds(number), Is.EqualTo(new[] { AsnInstructionKind.Decimal }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase(
        "Bad ::= INTEGER ENCODING-CONTROL XER [NAMESPACE AS \"urn:bad\"] Bad",
        "ASNS0304")]
    [TestCase(
        "Bad ::= REAL ENCODING-CONTROL XER [GLOBAL-DEFAULTS MODIFIED-ENCODINGS] [BASE64] Bad",
        "ASNS0303")]
    [TestCase(
        "Bad ::= SEQUENCE {} ENCODING-CONTROL XER [GLOBAL-DEFAULTS MODIFIED-ENCODINGS] "
            + "[USE-NIL] Bad [USE-QNAME] Bad",
        "ASNS0305")]
    public void ResolverRejectsIllegalFinalInstructions(
        string moduleBody,
        string expectedCode)
    {
        ResolutionFixture fixture = Resolve(
            "Invalid DEFINITIONS ::= BEGIN " + moduleBody + " END");

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    [Test]
    public void ResolverValidatesWildcardTypesRestrictionsAndContainerShape()
    {
        ResolutionFixture valid = Resolve(
            "Wildcards DEFINITIONS ::= BEGIN "
                + "Container ::= SEQUENCE { attributes "
                + "[XER:ANY-ATTRIBUTES FROM \"urn:allowed\" ABSENT] "
                + "SEQUENCE OF UTF8String, payload "
                + "[XER:ANY-ELEMENT EXCEPT \"urn:blocked\"] UTF8String } "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] END");
        ResolutionFixture optional = Resolve(
            "BadOptional DEFINITIONS ::= BEGIN "
                + "Container ::= SEQUENCE { attributes "
                + "[XER:ANY-ATTRIBUTES] SEQUENCE OF UTF8String OPTIONAL } "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] END");
        ResolutionFixture malformed = Resolve(
            "BadRestriction DEFINITIONS ::= BEGIN "
                + "Payload ::= [XER:ANY-ELEMENT FROM ABSENT ABSENT] UTF8String "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] END");
        AsnCompilationResult compiled = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits = [new AsnSourceUnit
                {
                    Name = "wildcard.asn",
                    Text = "Published DEFINITIONS ::= BEGIN "
                        + "Payload ::= [XER:ANY-ELEMENT FROM \"urn:item\" ABSENT] "
                        + "UTF8String ENCODING-CONTROL XER "
                        + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] END"
                }]
            });
        ResolvedTypeSyntax container = FindType(valid, "Container");
        AsnAssignmentDescriptor published = compiled.ReflectionCatalog?.Assignments
            .Single(item => item.Name == "Payload")
            ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(valid.Result.Success, Is.True, Format(valid));
            Assert.That(RequireComponent(container, "attributes")
                .FinalEncodingInstructions[0].Arguments,
                Is.EqualTo(new[] { "FROM", "\"urn:allowed\"", "ABSENT" }));
            Assert.That(optional.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0309"));
            Assert.That(malformed.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0308"));
            Assert.That(compiled.Success, Is.True);
            Assert.That(published.Type?.Encodings.Single().Instructions[0].Arguments,
                Is.EqualTo(new[] { "FROM", "\"urn:item\"", "ABSENT" }));
        });
    }

    [Test]
    public void LowererUsesResolvedFinalInstructionsInsteadOfNegatingAssignments()
    {
        ResolutionFixture fixture = Resolve(
            "Lowering DEFINITIONS ::= BEGIN\n"
                + "Value ::= [XER:ATTRIBUTE] INTEGER; Alias ::= [XER:NOT ATTRIBUTE] Value\n"
                + "END");
        LoweredCompilationIr lowered = new EncodingNeutralIrLowerer()
            .LowerCompilation(fixture.Tables);
        LoweredModuleIr module = lowered.Modules.Single();
        LoweredTypeIr value = module.Assignments.Single(item => item.Name == "Value").Type
            ?? throw new InvalidOperationException();
        LoweredTypeIr alias = module.Assignments.Single(item => item.Name == "Alias").Type
            ?? throw new InvalidOperationException();

        Assert.That(value.Instructions.Select(item => item.Kind),
            Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        Assert.That(alias.Instructions, Is.Empty);
    }

    [Test]
    public void CompilerPipelinePublishesTheResolvedFinalInstructionSet()
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits = [new AsnSourceUnit
                {
                    Name = "compiled-xer.asn",
                    Text = "CompiledXer DEFINITIONS ::= BEGIN "
                        + "Value ::= [XER:ATTRIBUTE] INTEGER; "
                        + "Alias ::= [XER:NOT ATTRIBUTE] Value END"
                }]
            });
        AsnAssignmentDescriptor value = result.ReflectionCatalog?.Assignments
            .Single(item => item.Name == "Value")
            ?? throw new InvalidOperationException();
        AsnAssignmentDescriptor alias = result.ReflectionCatalog?.Assignments
            .Single(item => item.Name == "Alias")
            ?? throw new InvalidOperationException();

        Assert.That(result.Success, Is.True,
            string.Join("\n", result.Diagnostics.Select(item => item.Message)));
        Assert.That(value.Type?.Encodings.Single().Instructions.Select(item => item.Kind),
            Is.EqualTo(new[] { AsnInstructionKind.Attribute }));
        Assert.That(alias.Type?.Encodings, Is.Empty);
    }

    [Test]
    public void ElementRemovesTheEffectiveUntaggedInstruction()
    {
        ResolutionFixture fixture = Resolve(
            "ElementRule DEFINITIONS ::= BEGIN "
                + "Wrapped ::= [XER:ELEMENT] [XER:UNTAGGED] INTEGER "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] "
                + "END");

        ResolvedTypeSyntax wrapped = FindType(fixture, "Wrapped");

        Assert.That(fixture.Result.Success, Is.True, Format(fixture));
        Assert.That(wrapped.FinalEncodingInstructions, Is.Empty);
    }

    [Test]
    public void ResolverValidatesModifiedRepresentationInstructionShapes()
    {
        ResolutionFixture valid = Resolve(
            "ModifiedForms DEFINITIONS ::= BEGIN "
                + "QName ::= SEQUENCE { namespace UTF8String OPTIONAL, name UTF8String }; "
                + "Union ::= CHOICE { number INTEGER, text UTF8String }; "
                + "Payload ::= [XER:BASE64] OCTET STRING; "
                + "Amount ::= [XER:DECIMAL] REAL; "
                + "State ::= [XER:USE-NUMBER] ENUMERATED { off(0), on(1) }; "
                + "Token ::= [XER:WHITESPACE COLLAPSE] UTF8String "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] "
                + "[USE-QNAME] QName [USE-UNION] Union END");
        ResolutionFixture invalid = Resolve(
            "InvalidForms DEFINITIONS ::= BEGIN "
                + "BadQName ::= SEQUENCE { name UTF8String }; "
                + "BadUnion ::= CHOICE { nested SEQUENCE { value INTEGER } }; "
                + "BadSpace ::= [XER:WHITESPACE] UTF8String "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] "
                + "[USE-QNAME] BadQName [USE-UNION] BadUnion END");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Result.Success, Is.True, Format(valid));
            Assert.That(Kinds(FindType(valid, "Payload")),
                Is.EqualTo(new[] { AsnInstructionKind.Base64 }));
            Assert.That(Kinds(FindType(valid, "Amount")),
                Is.EqualTo(new[] { AsnInstructionKind.Decimal }));
            Assert.That(Kinds(FindType(valid, "State")),
                Is.EqualTo(new[] { AsnInstructionKind.UseNumber }));
            Assert.That(Kinds(FindType(valid, "QName")),
                Is.EqualTo(new[] { AsnInstructionKind.UseQName }));
            Assert.That(Kinds(FindType(valid, "Union")),
                Is.EqualTo(new[] { AsnInstructionKind.UseUnion }));
            Assert.That(invalid.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0303"));
            Assert.That(invalid.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0313"));
        });
    }

    [Test]
    public void ResolverValidatesModifiedSequenceInstructionLayouts()
    {
        ResolutionFixture valid = Resolve(
            "ModifiedSequences DEFINITIONS ::= BEGIN "
                + "Defaulted ::= [XER:DEFAULT-FOR-EMPTY AS \"missing\"] UTF8String; "
                + "Embedded ::= [XER:EMBED-VALUES] SEQUENCE { "
                + "text SEQUENCE OF UTF8String, value INTEGER }; "
                + "Maybe ::= [XER:USE-NIL] SEQUENCE { value UTF8String OPTIONAL }; "
                + "Ordered ::= [XER:USE-ORDER] SEQUENCE { "
                + "order SEQUENCE OF ENUMERATED { first, second }, "
                + "first INTEGER, second UTF8String }; "
                + "Annotated ::= [XER:PI-OR-COMMENT AS \"<!--note-->\" BEFORE-TAG] INTEGER "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] END");
        ResolutionFixture invalid = Resolve(
            "BadModifiedSequences DEFINITIONS ::= BEGIN "
                + "BadEmbedded ::= [XER:EMBED-VALUES] SEQUENCE { value INTEGER }; "
                + "BadNil ::= [XER:USE-NIL] SEQUENCE { value UTF8String }; "
                + "BadOrder ::= [XER:USE-ORDER] SEQUENCE { "
                + "order SEQUENCE OF INTEGER, value INTEGER }; "
                + "BadDefault ::= [XER:DEFAULT-FOR-EMPTY] UTF8String; "
                + "BadPi ::= [XER:PI-OR-COMMENT AS \"<!--note-->\" MIDDLE] INTEGER "
                + "ENCODING-CONTROL XER "
                + "[GLOBAL-DEFAULTS MODIFIED-ENCODINGS] END");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Result.Success, Is.True, Format(valid));
            Assert.That(Kinds(FindType(valid, "Defaulted")),
                Does.Contain(AsnInstructionKind.DefaultForEmpty));
            Assert.That(Kinds(FindType(valid, "Embedded")),
                Does.Contain(AsnInstructionKind.EmbedValues));
            Assert.That(Kinds(FindType(valid, "Maybe")),
                Does.Contain(AsnInstructionKind.UseNil));
            Assert.That(Kinds(FindType(valid, "Ordered")),
                Does.Contain(AsnInstructionKind.UseOrder));
            Assert.That(Kinds(FindType(valid, "Annotated")),
                Does.Contain(AsnInstructionKind.PiOrComment));
            Assert.That(invalid.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0303"));
            Assert.That(invalid.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0314"));
            Assert.That(invalid.Diagnostics.GetDiagnostics()
                .Select(item => item.Code), Does.Contain("ASNS0316"));
        });
    }

    private static ResolutionFixture Resolve(string source)
    {
        return ResolveModules([source]);
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
        XerInstructionResolutionResult result = new XerInstructionResolver(diagnostics)
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
            "xer-resolution.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed),
            FormatDiagnostics(session));
        Assert.That(result.Module, Is.Not.Null);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static ResolvedTypeSyntax FindType(ResolutionFixture fixture, string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolvedTypeSyntax RequireComponent(ResolvedTypeSyntax type, string name)
    {
        return type.Components.Single(item => item.Component?.Name == name).TypeResolution
            ?? throw new InvalidOperationException();
    }

    private static AsnInstructionKind[] Kinds(ResolvedTypeSyntax type)
    {
        return type.FinalEncodingInstructions.Select(item => item.Kind).ToArray();
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
        internal XerInstructionResolutionResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
