using asn1;

namespace asn1.Tests;

public class InformationObjectOpenTypeIntegrationTests
{
    [Test]
    public void PipelineBindsEveryFieldKindDefaultsDefinedSyntaxAndOptionalGroups()
    {
        ResolutionFixture fixture = Resolve(
            "Bindings DEFINITIONS ::= BEGIN "
                + "TYPED-CHILD ::= CLASS { &Type DEFAULT UTF8String }; "
                + "ALL-FIELDS ::= CLASS { "
                + "&open &Later DEFAULT TRUE, "
                + "&OpenValues &Later DEFAULT { TRUE }, "
                + "&child TYPED-CHILD OPTIONAL, "
                + "&Children TYPED-CHILD OPTIONAL, "
                + "&fixed INTEGER DEFAULT 7, "
                + "&FixedValues INTEGER OPTIONAL, "
                + "&Later DEFAULT BOOLEAN }; "
                + "CHILD ::= CLASS { &code INTEGER }; "
                + "DEFAULTED ::= CLASS { "
                + "&Type DEFAULT BOOLEAN, &required INTEGER, &value &Type, "
                + "&Values INTEGER DEFAULT { 1 | 2 }, "
                + "&OpenValues &Type DEFAULT { TRUE }, "
                + "&child CHILD DEFAULT { &code 9 }, "
                + "&Children CHILD DEFAULT { { &code 10 } }, "
                + "&optional UTF8String OPTIONAL }; "
                + "defaults DEFAULTED ::= { &required 7, &value TRUE }; "
                + "DEFINED ::= CLASS { &Type, &value &Type, "
                + "&label UTF8String OPTIONAL } "
                + "WITH SYNTAX { TYPE &Type VALUE &value [ LABEL &label ] }; "
                + "short DEFINED ::= { TYPE INTEGER VALUE 3 }; "
                + "complete DEFINED ::= { TYPE UTF8String VALUE \"ok\" "
                + "LABEL \"shown\" } END");
        ParsedObjectClassSyntax all = RequireClass(fixture, "ALL-FIELDS");
        AsnObjectFieldKind[] expectedKinds = System.Enum
            .GetValues<AsnObjectFieldKind>()
            .Where(item => item != AsnObjectFieldKind.Unknown).ToArray();

        Assert.That(all.ObjectClass.Fields.Select(item => item.Kind).Distinct()
            .OrderBy(item => item), Is.EqualTo(expectedKinds));
        AsnObjectFieldSetting[] defaults = RequireObject(fixture, "defaults")
            .Object.FieldSettings;
        Assert.That(defaults.Select(item => item.FieldName), Is.EqualTo(new[]
        {
            "&Type", "&required", "&value", "&Values", "&OpenValues",
            "&child", "&Children"
        }));
        Assert.That(defaults[0].Type?.Kind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(defaults[3].ValueSetType?.Constraints.Single().Kind,
            Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(defaults[4].ValueSetType?.Kind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(defaults[5].Object?.FieldSettings.Single().Value?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(9)));
        Assert.That(defaults[6].ObjectSet?.Objects.Single().FieldSettings.Single()
            .Value?.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(10)));

        Assert.That(RequireObject(fixture, "short").Object.FieldSettings
            .Select(item => item.FieldName), Is.EqualTo(new[] { "&Type", "&value" }));
        Assert.That(RequireObject(fixture, "complete").Object.FieldSettings
            .Select(item => item.FieldName),
            Is.EqualTo(new[] { "&Type", "&value", "&label" }));
        Assert.That(RequireObject(fixture, "complete").Object.FieldSettings[2]
            .Value?.TextValue, Is.EqualTo("shown"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            Format(fixture.Diagnostics));
    }

    [Test]
    public void PipelineEvaluatesSetAlgebraUsefulClassesAndInformationSelections()
    {
        ResolutionFixture fixture = Resolve(
            "Selections DEFINITIONS ::= BEGIN "
                + "CHILD ::= CLASS { &code INTEGER, &Type }; "
                + "ITEM ::= CLASS { &Type, &id INTEGER UNIQUE, &Values INTEGER, "
                + "&child CHILD, &Children CHILD }; "
                + "childOne CHILD ::= { &code 11, &Type BOOLEAN }; "
                + "childTwo CHILD ::= { &code 12, &Type UTF8String }; "
                + "first ITEM ::= { &Type UTF8String, &id 1, &Values { 1 | 2 }, "
                + "&child childOne, &Children { childOne | childTwo } }; "
                + "second ITEM ::= { &Type BOOLEAN, &id 2, &Values { 2 | 3 }, "
                + "&child childTwo, &Children { childTwo } }; "
                + "Items ITEM ::= { first | second }; "
                + "Common ITEM ::= { Items ^ (first | second) }; "
                + "Reduced ITEM ::= { Items EXCEPT second }; "
                + "Complement ITEM ::= { ALL EXCEPT second }; "
                + "Extended ITEM ::= { first, ..., second }; "
                + "SelectedType ::= first.&Type; "
                + "selectedValue INTEGER ::= first.&id; "
                + "SelectedValues INTEGER ::= Items.&id; "
                + "selectedChild CHILD ::= first.&child; "
                + "SelectedChildren CHILD ::= first.&Children; "
                + "chainedCode INTEGER ::= first.&child.&code; "
                + "direct TYPE-IDENTIFIER ::= { INTEGER IDENTIFIED BY { 1 2 3 } }; "
                + "Types TYPE-IDENTIFIER ::= { direct }; "
                + "Payload ::= TYPE-IDENTIFIER.&Type ({Types}) END");

        Assert.That(ObjectNames(fixture, "Common"),
            Is.EqualTo(new[] { "first", "second" }));
        Assert.That(ObjectNames(fixture, "Reduced"), Is.EqualTo(new[] { "first" }));
        Assert.That(ObjectNames(fixture, "Complement"), Is.EqualTo(new[] { "first" }));
        AsnInformationObjectSet extended = RequireSet(fixture, "Extended").ObjectSet;
        Assert.That(extended.Extensible, Is.True);
        Assert.That(extended.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(extended.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));

        Assert.That(RequireType(fixture, "SelectedType").EffectiveKind,
            Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(RequireValue(fixture, "selectedValue").EffectiveValue?.IntegerValue,
            Is.EqualTo(System.Numerics.BigInteger.One));
        Assert.That(fixture.Tables[0].FindSymbol("SelectedValues", AsnAssignmentKind.ValueSet)
            ?.ResolvedValueSetType?.Constraints.Single().Elements
            .Select(item => item.Value?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        Assert.That(RequireObject(fixture, "selectedChild").Object.FieldSettings[0]
            .Value?.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(11)));
        Assert.That(ObjectNames(fixture, "SelectedChildren"),
            Is.EqualTo(new[] { "childOne", "childTwo" }));
        Assert.That(RequireValue(fixture, "chainedCode").EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(11)));

        ParsedInformationObjectSyntax direct = RequireObject(fixture, "direct");
        Assert.That(direct.Object.ObjectClass?.Name, Is.EqualTo("TYPE-IDENTIFIER"));
        Assert.That(RequireType(fixture, "Payload").TableConstraints.Single()
            .DispatchEntries.Single().Type?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            Format(fixture.Diagnostics));
    }

    [Test]
    public void StaticAndDynamicTablesResolveEntriesAndAmbiguityIsDiagnosed()
    {
        ResolutionFixture valid = Resolve(
            "Tables DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "first ITEM ::= { &id 1, &Type INTEGER }; "
                + "second ITEM ::= { &id 2, &Type BOOLEAN }; "
                + "Items ITEM ::= { first, ..., second }; "
                + "Payload ::= ITEM.&Type ({Items}) END");
        AsnInformationObjectSet items = RequireSet(valid, "Items").ObjectSet;
        IAsnObjectSetRegistry registry = new AsnObjectSetRegistry();

        Assert.That(items.DispatchEntries.Select(item => item.Key?.IntegerValue),
            Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
        Assert.That(registry.ResolveDispatchEntry(items, IntegerValue(1))?.Type?.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        registry.RegisterObject(
            items,
            new AsnInformationObject { StableId = "Tables.third", Name = "third" },
            new AsnOpenTypeDispatchEntry
            {
                StableId = "Tables.Items:3",
                Key = IntegerValue(3),
                Type = new AsnType { Name = "ThirdPayload", Kind = AsnTypeKind.Utf8String }
            });
        Assert.That(registry.GetEffectiveObjects(items).Select(item => item.Name),
            Is.EqualTo(new[] { "first", "second", "third" }));
        Assert.That(registry.ResolveDispatchEntry(items, IntegerValue(3))?.Type?.Name,
            Is.EqualTo("ThirdPayload"));

        ResolutionFixture ambiguous = ResolveAllowingFailure(
            "Ambiguous DEFINITIONS ::= BEGIN "
                + "Code ::= INTEGER { named(1) }; "
                + "ITEM ::= CLASS { &id Code UNIQUE, &Type }; "
                + "namedItem ITEM ::= { &id named, &Type INTEGER }; "
                + "numericItem ITEM ::= { &id 1, &Type BOOLEAN }; "
                + "Items ITEM ::= { namedItem | numericItem }; "
                + "Payload ::= ITEM.&Type ({Items}) END");
        Assert.That(ambiguous.Values.Success, Is.False);
        Assert.That(ambiguous.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNC0035"));
    }

    [Test]
    public void UnknownOpenPayloadsFollowPreserveSkipAndRejectPolicies()
    {
        ResolutionFixture fixture = Resolve(
            "Unknowns DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type }; "
                + "known ITEM ::= { &id 1, &Type INTEGER }; "
                + "Items ITEM ::= { known, ... }; "
                + "Payload ::= ITEM.&Type ({Items}) END");
        AsnInformationObjectSet items = RequireSet(fixture, "Items").ObjectSet;
        OpenTypeRuntimeCodec runtime = new OpenTypeRuntimeCodec(
            new UnusedCodec(),
            new AsnObjectSetRegistry());
        AsnRuntimeValue key = IntegerRuntimeValue(99);
        byte[] payload = [0xde, 0xad];
        AsnCodecOptions preserve = Options(
            AsnEncodingRules.Uper,
            AsnUnknownDataPolicy.Preserve);

        AsnDecodeResult decoded = runtime.DecodePayload(items, key, payload, preserve);
        AsnOpenTypeValue open = decoded.Value?.OpenTypeValue
            ?? throw new InvalidOperationException();
        payload[0] = 0;
        Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
        Assert.That(open.Unresolved, Is.True);
        Assert.That(open.OriginalData, Is.EqualTo(new byte[] { 0xde, 0xad }));
        Assert.That(runtime.EncodePayload(items, open, preserve).Data,
            Is.EqualTo(new byte[] { 0xde, 0xad }));
        Assert.That(runtime.EncodePayload(
            items,
            open,
            Options(AsnEncodingRules.Oer, AsnUnknownDataPolicy.Preserve))
            .Diagnostics.Single().Code, Is.EqualTo("ASNE0007"));

        AsnCodecOptions skip = Options(
            AsnEncodingRules.Uper,
            AsnUnknownDataPolicy.Skip);
        AsnOpenTypeValue skipped = runtime.DecodePayload(items, key, [1, 2], skip)
            .Value?.OpenTypeValue ?? throw new InvalidOperationException();
        Assert.That(skipped.Unresolved, Is.True);
        Assert.That(skipped.HasOriginalData, Is.False);
        Assert.That(runtime.EncodePayload(items, skipped, skip).Success, Is.False);

        AsnCodecOptions reject = Options(
            AsnEncodingRules.Uper,
            AsnUnknownDataPolicy.Reject);
        Assert.That(runtime.DecodePayload(items, key, [1], reject)
            .Diagnostics.Single().Code, Is.EqualTo("ASND0001"));
    }

    private static string[] ObjectNames(ResolutionFixture fixture, string name)
    {
        return RequireSet(fixture, name).ObjectSet.Objects
            .Select(item => item.Name).ToArray();
    }

    private static ParsedObjectClassSyntax RequireClass(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.ObjectClass)
            ?.ResolvedObjectClassSyntax ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectSyntax RequireObject(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Object)
            ?.DeclaredObjectSyntax ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectSetSyntax RequireSet(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.ObjectSet)
            ?.DeclaredObjectSetSyntax ?? throw new InvalidOperationException();
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType ?? throw new InvalidOperationException();
    }

    private static ResolvedValueSyntax RequireValue(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Value)
            ?.ResolvedValue ?? throw new InvalidOperationException();
    }

    private static AsnValue IntegerValue(int value)
    {
        return new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
    }

    private static AsnRuntimeValue IntegerRuntimeValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
    }

    private static AsnCodecOptions Options(
        AsnEncodingRules rules,
        AsnUnknownDataPolicy policy)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = policy
        };
    }

    private static ResolutionFixture Resolve(string source)
    {
        ResolutionFixture fixture = ResolveAllowingFailure(source);
        Assert.That(fixture.Values.Success, Is.True, Format(fixture.Diagnostics));
        return fixture;
    }

    private static ResolutionFixture ResolveAllowingFailure(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "information-object-integration.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(
            diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True, Format(diagnostics));
        Assert.That(types.Success, Is.True, Format(diagnostics));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private static string Format(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
            + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }

    private class UnusedCodec : IAsnCodec
    {
        public AsnEncodeResult Encode(AsnEncodeRequest request)
        {
            throw new InvalidOperationException();
        }

        public AsnDecodeResult Decode(AsnDecodeRequest request)
        {
            throw new InvalidOperationException();
        }

        public AsnDecodeResult TryDecodeSpan(
            ReadOnlySpan<byte> input,
            string typeName,
            AsnCodecOptions options)
        {
            throw new InvalidOperationException();
        }

        public AsnEncodeResult EncodeToSpan(
            AsnEncodeRequest request,
            Span<byte> destination)
        {
            throw new InvalidOperationException();
        }

        public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)
        {
            throw new InvalidOperationException();
        }

        public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
        {
            throw new InvalidOperationException();
        }

        public IAsnStreamingCodec CreateStreamingCodec()
        {
            throw new InvalidOperationException();
        }

        public IAsnIncrementalDecoder CreateIncrementalDecoder(
            string typeName,
            AsnCodecOptions options)
        {
            throw new InvalidOperationException();
        }
    }
}
