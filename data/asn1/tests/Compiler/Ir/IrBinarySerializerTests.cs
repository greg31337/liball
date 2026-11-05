using asn1;

namespace asn1.Tests;

public class IrBinarySerializerTests
{
    [Test]
    public void RepresentativeIrRoundTripsToTheSameDeterministicBytes()
    {
        LoweredCompilationIr compilation = Compile(
            "BinaryIr DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type };\n"
                + "first ITEM ::= { &id 1, &Type INTEGER };\n"
                + "second ITEM ::= { &id 2, &Type UTF8String };\n"
                + "Items ITEM ::= { first, ..., second };\n"
                + "Base ::= SEQUENCE { count INTEGER (1..9) DEFAULT 3, "
                + "..., note UTF8String OPTIONAL, "
                + "[[ alpha BOOLEAN OPTIONAL, beta BOOLEAN OPTIONAL ]] };\n"
                + "Payload ::= [XER:ATTRIBUTE payload] ITEM.&Type ({Items});\n"
                + "answer INTEGER ::= 5\n"
                + "ENCODING-CONTROL XER\nBase ATTRIBUTE\nEND");
        IrBinarySerializer serializer = new IrBinarySerializer();
        AsnResourceLimits limits = new AsnResourceLimits();

        byte[] first = serializer.Serialize(compilation, limits);
        byte[] repeated = serializer.Serialize(compilation, limits);
        LoweredCompilationIr restored = serializer.Deserialize(first, limits);
        byte[] roundTrip = serializer.Serialize(restored, limits);
        LoweredModuleIr module = restored.Modules.Single();
        LoweredAssignmentIr baseAssignment = RequireAssignment(module, "Base");
        LoweredTypeIr baseType = baseAssignment.Type
            ?? throw new InvalidOperationException();
        LoweredAssignmentIr items = RequireAssignment(module, "Items");

        Assert.That(first, Is.EqualTo(repeated));
        Assert.That(roundTrip, Is.EqualTo(first));
        Assert.That(first.Take(IrBinaryFormat.Magic.Length),
            Is.EqualTo(IrBinaryFormat.Magic));
        Assert.That(module.StableId, Is.EqualTo(compilation.Modules[0].StableId));
        Assert.That(baseType.Fields.Select(item => item.Name),
            Is.EqualTo(new[] { "count", "note", "alpha", "beta" }));
        Assert.That(baseType.Extensions.RootFields[0],
            Is.SameAs(baseType.Fields[0]));
        Assert.That(baseType.Extensions.AdditionalFields[0],
            Is.SameAs(baseType.Fields[1]));
        Assert.That(baseType.Fields.Skip(2).Select(
            item => item.ExtensionGroupIndex),
            Is.EqualTo(new[] { 0, 0 }));
        Assert.That(baseType.Fields[0].DefaultValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(3)));
        Assert.That(baseType.Fields[0].Type?.Constraints[0].RootIntervals[0]
            .LowerBound, Is.EqualTo(new System.Numerics.BigInteger(1)));
        Assert.That(baseType.Fields[0].Tags[0].AutomaticallyGenerated, Is.True);
        Assert.That(baseType.Fields[0].Tags[0].SourceSpan?.Start.SourceName,
            Is.EqualTo("binary-ir.asn"));
        Assert.That(items.ObjectSet?.RootObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "first" }));
        Assert.That(items.ObjectSet?.AdditionalObjects.Select(item => item.Name),
            Is.EqualTo(new[] { "second" }));
        Assert.That(RequireAssignment(module, "Payload").Type?.ObjectTables[0]
            .Entries.Select(item => item.Type?.Kind),
            Is.EqualTo(new[] { AsnTypeKind.Integer, AsnTypeKind.Utf8String }));
        Assert.That(module.Instructions[0].Derivations[0].Kind,
            Is.EqualTo(LoweredDerivationKind.EncodingControl));
        Assert.That(RequireAssignment(module, "Payload").Type?.Instructions[0]
            .Arguments, Is.EqualTo(new[] { "payload" }));
    }

    [Test]
    public void SerializedIrRetainsQueryableReflectionMetadataAndSourceTraces()
    {
        LoweredCompilationIr compilation = Compile(
            "ReflectedIr DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
                + "ITEM ::= CLASS { &id INTEGER UNIQUE, &Type };\n"
                + "first ITEM ::= { &id 1, &Type INTEGER };\n"
                + "second ITEM ::= { &id 2, &Type UTF8String };\n"
                + "Items ITEM ::= { first, ..., second };\n"
                + "Base ::= SEQUENCE { count INTEGER (1..9) DEFAULT 3, "
                + "..., note UTF8String OPTIONAL, "
                + "[[ alpha BOOLEAN OPTIONAL, beta BOOLEAN OPTIONAL ]] };\n"
                + "Alias ::= Base;\n"
                + "Payload ::= [XER:ATTRIBUTE payload] ITEM.&Type ({Items});\n"
                + "answer INTEGER ::= 5\n"
                + "ENCODING-CONTROL XER\nAlias ATTRIBUTE\nEND");
        IrBinarySerializer serializer = new IrBinarySerializer();
        AsnResourceLimits limits = new AsnResourceLimits();
        byte[] bytes = serializer.Serialize(compilation, limits);
        LoweredCompilationIr restored = serializer.Deserialize(bytes, limits);
        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(restored);
        IAsnReflectionService service = new ReflectionServiceImplementation();
        LoweredModuleIr loweredModule = restored.Modules.Single();
        AsnModuleDescriptor module = catalog.Modules.Single();
        AsnAssignmentDescriptor baseAssignment = module.Assignments.Single(
            item => item.Name == "Base");
        AsnTypeDescriptor baseType = baseAssignment.Type
            ?? throw new InvalidOperationException();
        AsnAssignmentDescriptor alias = module.Assignments.Single(
            item => item.Name == "Alias");
        AsnAssignmentDescriptor items = module.Assignments.Single(
            item => item.Name == "Items");
        AsnAssignmentDescriptor payload = module.Assignments.Single(
            item => item.Name == "Payload");
        AsnAssignmentDescriptor answer = module.Assignments.Single(
            item => item.Name == "answer");
        AsnEncodingDescriptor typeEncoding = payload.Type?.Encodings.Single()
            ?? throw new InvalidOperationException();
        AsnEncodingDescriptor moduleEncoding = module.Encodings.Single();

        Assert.Multiple(() =>
        {
            Assert.That(serializer.Serialize(restored, limits), Is.EqualTo(bytes));
            Assert.That(catalog.StableId,
                Is.EqualTo("reflection:" + restored.StableId));
            Assert.That(module.StableId, Is.EqualTo(loweredModule.StableId));
            Assert.That(module.Assignments.Select(item => item.StableId),
                Is.EqualTo(loweredModule.Assignments.Select(item =>
                    item.StableId)));
            Assert.That(catalog.Assignments[0],
                Is.SameAs(module.Assignments[0]));
            Assert.That(module.Types.Single(item => item.Name == "Base"),
                Is.SameAs(baseType));
            Assert.That(baseType.RootFields.Single(),
                Is.SameAs(baseType.Fields[0]));
            Assert.That(baseType.ExtensionFields,
                Is.EqualTo(baseType.Fields.Skip(1)));
            Assert.That(baseType.ExtensionFields.Select(item =>
                item.ExtensionGroupIndex), Is.EqualTo(new[] { -1, 0, 0 }));
            Assert.That(items.ObjectSet?.RootObjects.Single(),
                Is.SameAs(items.ObjectSet?.Objects[0]));
            Assert.That(items.ObjectSet?.AdditionalObjects.Single(),
                Is.SameAs(items.ObjectSet?.Objects[1]));
            Assert.That(payload.ObjectTables.Single(),
                Is.SameAs(payload.Type?.ObjectTables.Single()));
            Assert.That(payload.ObjectTables.Single().Entries.Select(item =>
                item.Type?.Kind), Is.EqualTo(new[]
                {
                    AsnTypeKind.Integer,
                    AsnTypeKind.Utf8String
                }));
            Assert.That(typeEncoding.Instructions.Single(),
                Is.SameAs(typeEncoding.Plan?.Instructions.Single()));
            Assert.That(typeEncoding.Plan?.Steps.Single().Instruction,
                Is.SameAs(typeEncoding.Instructions.Single()));
            Assert.That(typeEncoding.Plan?.Steps.Single().Path,
                Is.EqualTo("payload"));
            Assert.That(moduleEncoding.Plan?.Steps.Single().Path,
                Is.EqualTo("ATTRIBUTE"));
            Assert.That(service.ResolveModule(catalog, module.StableId),
                Is.SameAs(module));
            Assert.That(service.ResolveType(catalog, "", "ReflectedIr.Base"),
                Is.SameAs(baseType));
            Assert.That(service.ResolveType(
                catalog,
                "ReflectedIr",
                baseAssignment.StableId), Is.SameAs(baseType));
            Assert.That(service.ResolveValue(
                catalog,
                "",
                "ReflectedIr.answer"), Is.SameAs(answer.Value));
            Assert.That(service.GetTypes(catalog), Does.Contain(baseType));
        });

        Assert.Multiple(() =>
        {
            Assert.That(module.SourceSpan?.Start.SourceName,
                Is.EqualTo("binary-ir.asn"));
            Assert.That(module.Assignments.All(item =>
                item.SourceSpan?.Start.SourceName == "binary-ir.asn"), Is.True);
            Assert.That(baseType.Fields.All(item =>
                item.SourceSpan?.Start.SourceName == "binary-ir.asn"), Is.True);
            Assert.That(baseType.Fields[0].Tags.Single().SourceSpan?.Start
                .SourceName, Is.EqualTo("binary-ir.asn"));
            Assert.That(baseType.Fields[0].Tags.Single().Derivations.Single(
                item => item.Kind == AsnDerivationKind.AutomaticTag).Span?.Start
                .SourceName, Is.EqualTo("binary-ir.asn"));
            Assert.That(alias.Type?.Derivations.Single(item =>
                item.Kind == AsnDerivationKind.Alias).Span?.Start.SourceName,
                Is.EqualTo("binary-ir.asn"));
            Assert.That(payload.ObjectTables.Single().SourceSpan?.Start.SourceName,
                Is.EqualTo("binary-ir.asn"));
            Assert.That(typeEncoding.SourceSpan?.Start.SourceName,
                Is.EqualTo("binary-ir.asn"));
            Assert.That(moduleEncoding.SourceSpan?.Start.SourceName,
                Is.EqualTo("binary-ir.asn"));
        });
    }

    [Test]
    public void TypeTablePreservesSharedAndRecursiveReferences()
    {
        LoweredTypeIr recursive = new LoweredTypeIr
        {
            Name = "Node",
            Kind = AsnTypeKind.Sequence
        };
        LoweredFieldIr next = new LoweredFieldIr
        {
            Name = "next",
            Type = recursive,
            Optional = true
        };
        recursive.Fields = [next];
        recursive.Extensions = new LoweredExtensionPartitionIr
        {
            RootFields = [next]
        };
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            Modules = [new LoweredModuleIr
            {
                Name = "Recursive",
                Assignments = [new LoweredAssignmentIr
                {
                    Name = "Node",
                    QualifiedName = "Recursive.Node",
                    Kind = AsnAssignmentKind.Type,
                    Type = recursive
                }]
            }]
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(
            compilation);
        IrBinarySerializer serializer = new IrBinarySerializer();

        LoweredCompilationIr restored = serializer.Deserialize(
            serializer.Serialize(compilation, new AsnResourceLimits()),
            new AsnResourceLimits());
        LoweredTypeIr restoredType = restored.Modules[0].Assignments[0].Type
            ?? throw new InvalidOperationException();

        Assert.That(restoredType.Fields[0].Type, Is.SameAs(restoredType));
        Assert.That(restoredType.Extensions.RootFields[0],
            Is.SameAs(restoredType.Fields[0]));
    }

    [Test]
    public void ReaderRejectsUnknownVersionsAndMalformedPayloads()
    {
        IrBinarySerializer serializer = new IrBinarySerializer();
        byte[] valid = serializer.Serialize(
            new LoweredCompilationIr(),
            new AsnResourceLimits());
        byte[] unknownVersion = (byte[])valid.Clone();
        unknownVersion[IrBinaryFormat.Magic.Length] =
            (byte)(IrBinaryFormat.Version + 1);
        byte[] invalidHeader = (byte[])valid.Clone();
        invalidHeader[0] = 0;
        byte[] trailing = new byte[valid.Length + 1];
        Array.Copy(valid, trailing, valid.Length);

        Assert.That(() => serializer.Deserialize(
            unknownVersion,
            new AsnResourceLimits()), Throws.TypeOf<System.IO.InvalidDataException>());
        Assert.That(() => serializer.Deserialize(
            invalidHeader,
            new AsnResourceLimits()), Throws.TypeOf<System.IO.InvalidDataException>());
        Assert.That(() => serializer.Deserialize(
            valid[..^1],
            new AsnResourceLimits()), Throws.TypeOf<System.IO.InvalidDataException>());
        Assert.That(() => serializer.Deserialize(
            trailing,
            new AsnResourceLimits()), Throws.TypeOf<System.IO.InvalidDataException>());
    }

    [Test]
    public void SerializerAndReaderEnforceConfiguredBounds()
    {
        IrBinarySerializer serializer = new IrBinarySerializer();
        LoweredCompilationIr oneModule = new LoweredCompilationIr
        {
            Modules = [new LoweredModuleIr { Name = "Only" }]
        };
        AsnResourceLimits noCollections = new AsnResourceLimits
        {
            MaximumCollectionElements = 0
        };
        LoweredCompilationIr longText = new LoweredCompilationIr
        {
            StableId = "long"
        };
        AsnResourceLimits shortText = new AsnResourceLimits
        {
            MaximumTextCharacters = 2
        };
        LoweredValueIr inner = new LoweredValueIr();
        LoweredValueIr outer = new LoweredValueIr { Items = [inner] };
        LoweredCompilationIr nested = new LoweredCompilationIr
        {
            Modules = [new LoweredModuleIr
            {
                Assignments = [new LoweredAssignmentIr
                {
                    Value = outer,
                    Kind = AsnAssignmentKind.Value
                }]
            }]
        };
        AsnResourceLimits oneLevel = new AsnResourceLimits
        {
            MaximumNestingDepth = 1
        };
        byte[] nestedBytes = serializer.Serialize(nested, new AsnResourceLimits());
        byte[] emptyBytes = serializer.Serialize(
            new LoweredCompilationIr(),
            new AsnResourceLimits());
        AsnResourceLimits tooSmall = new AsnResourceLimits
        {
            MaximumTotalAllocationBytes = emptyBytes.Length - 1
        };
        byte[] excessiveCount = (byte[])emptyBytes.Clone();
        int countOffset = IrBinaryFormat.Magic.Length + 4;
        excessiveCount[countOffset] = 11;
        AsnResourceLimits tenElements = new AsnResourceLimits
        {
            MaximumCollectionElements = 10
        };

        Assert.That(() => serializer.Serialize(oneModule, noCollections),
            Throws.TypeOf<AsnResourceLimitException>());
        Assert.That(() => serializer.Serialize(longText, shortText),
            Throws.TypeOf<AsnResourceLimitException>());
        Assert.That(() => serializer.Serialize(nested, oneLevel),
            Throws.TypeOf<AsnResourceLimitException>());
        Assert.That(() => serializer.Deserialize(nestedBytes, oneLevel),
            Throws.TypeOf<AsnResourceLimitException>());
        Assert.That(() => serializer.Deserialize(emptyBytes, tooSmall),
            Throws.TypeOf<AsnResourceLimitException>());
        Assert.That(() => serializer.Deserialize(excessiveCount, tenElements),
            Throws.TypeOf<AsnResourceLimitException>());
    }

    private static LoweredAssignmentIr RequireAssignment(
        LoweredModuleIr module,
        string name)
    {
        return module.Assignments.Single(item => item.Name == name);
    }

    private static LoweredCompilationIr Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "binary-ir.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        ModuleSyntax module = parsed.Module ?? throw new InvalidOperationException();
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics)
            .BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics)
            .BuildGraph(tables);
        Assert.That(new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph).Success, Is.True,
            FormatDiagnostics(diagnostics));
        Assert.That(new TypeResolver(diagnostics).ResolveTypes(tables).Success,
            Is.True, FormatDiagnostics(diagnostics));
        Assert.That(new ValueResolver(diagnostics).ResolveValues(tables).Success,
            Is.True, FormatDiagnostics(diagnostics));
        Assert.That(new TagResolver(diagnostics).ResolveTags(tables).Success,
            Is.True, FormatDiagnostics(diagnostics));
        Assert.That(new AutomaticTagger(diagnostics).ApplyAutomaticTags(tables)
            .Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(diagnostics));
        return new EncodingNeutralIrLowerer().LowerCompilation(tables);
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item =>
            item.Code + ":" + item.Message));
    }
}
