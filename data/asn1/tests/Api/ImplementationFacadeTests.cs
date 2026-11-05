using asn1;

namespace asn1.Tests;

public class ImplementationFacadeTests
{
    [Test]
    public void CompilerLexerAndParserFacadesForwardOperations()
    {
        RecordingImplementation implementation = new RecordingImplementation();
        AsnSourceUnit source = new AsnSourceUnit();
        AsnCompilationInput compilationInput = new AsnCompilationInput();
        AsnToken[] tokens = [new AsnToken()];

        IAsnCompiler compiler = new AsnCompiler(implementation);
        Assert.That(compiler.Compile(compilationInput), Is.SameAs(implementation.CompilationResult));

        IAsnLexer lexer = new AsnLexer(implementation);
        Assert.That(lexer.TokenizeSource(source), Is.SameAs(implementation.LexerResult));
        Assert.That(lexer.TokenizeSpan("ABC".AsSpan(), "input.asn"), Is.SameAs(implementation.LexerResult));
        Assert.That(lexer.ContinueTokenization(source, 2), Is.SameAs(implementation.LexerResult));

        IAsnParser parser = new AsnParser(implementation);
        Assert.That(parser.ParseTokens(tokens), Is.SameAs(implementation.ParserResult));
        Assert.That(parser.ParseSource(source), Is.SameAs(implementation.ParserResult));
        Assert.That(parser.ContinueParsing(tokens, 1), Is.SameAs(implementation.ParserResult));

        Assert.That(implementation.Calls, Is.EqualTo(new string[]
        {
            "Compile",
            "TokenizeSource",
            "TokenizeSpan:3:input.asn",
            "ContinueTokenization:2",
            "ParseTokens:1",
            "ParseSource",
            "ContinueParsing:1:1"
        }));
        Assert.That(implementation.LastObject, Is.SameAs(source));
    }

    [Test]
    public void ValidationGenerationReflectionAndImportFacadesForwardOperations()
    {
        RecordingImplementation implementation = new RecordingImplementation();
        AsnModule[] modules = [new AsnModule()];
        AsnValidationResult validation = new AsnValidationResult();
        AsnGenerationOptions generationOptions = new AsnGenerationOptions();
        AsnRuntimeValue value = new AsnRuntimeValue();
        AsnTypeDescriptor type = new AsnTypeDescriptor();
        AsnReflectionCatalog catalog = new AsnReflectionCatalog();
        AsnSourceUnit source = new AsnSourceUnit();
        AsnXsdInput[] inputs = [new AsnXsdInput()];
        AsnXsdImportOptions importOptions = new AsnXsdImportOptions();

        ISchemaValidator validator = new AsnSchemaValidator(implementation);
        IAsnValueValidator valueValidator = new AsnValueValidator(implementation);
        IAsnCodeGenerator generator = new AsnCodeGenerator(implementation);
        Assert.That(validator.ValidateModules(modules), Is.SameAs(implementation.ValidationResult));
        Assert.That(valueValidator.ValidateRuntimeValue(value, type), Is.SameAs(implementation.Diagnostics));
        Assert.That(generator.GenerateFiles(validation, generationOptions), Is.SameAs(implementation.GeneratedFiles));

        IAsnReflectionService reflection = new AsnReflectionService(implementation);
        Assert.That(reflection.BuildCatalog(validation), Is.SameAs(implementation.Catalog));
        Assert.That(reflection.ResolveModule(catalog, "Module"), Is.SameAs(implementation.ModuleDescriptor));
        Assert.That(reflection.ResolveType(catalog, "Module", "Type"), Is.SameAs(implementation.TypeDescriptor));
        Assert.That(reflection.ResolveValue(catalog, "Module", "value"), Is.SameAs(implementation.ValueDescriptor));
        Assert.That(reflection.GetTypes(catalog), Is.SameAs(implementation.TypeDescriptors));

        IEcnCompiler ecn = new EcnCompiler(implementation);
        IXsdSchemaImporter xsd = new XsdSchemaImporter(implementation);
        Assert.That(ecn.CompileEcn(source, modules), Is.SameAs(implementation.CompilationResult));
        Assert.That(xsd.ImportSchemas(inputs, importOptions), Is.SameAs(implementation.XsdImportResult));

        Assert.That(implementation.Calls, Is.EqualTo(new string[]
        {
            "ValidateModules:1",
            "ValidateRuntimeValue",
            "GenerateFiles",
            "BuildCatalog",
            "ResolveModule:Module",
            "ResolveType:Module:Type",
            "ResolveValue:Module:value",
            "GetTypes",
            "CompileEcn:1",
            "ImportSchemas:1"
        }));
    }

    [Test]
    public void CodecFacadesForwardBufferStreamAndIncrementalOperations()
    {
        RecordingImplementation implementation = new RecordingImplementation();
        IAsnCodec codec = new AsnCodec(implementation);
        AsnEncodeRequest encodeRequest = new AsnEncodeRequest();
        AsnDecodeRequest decodeRequest = new AsnDecodeRequest();
        AsnTranscodeRequest transcodeRequest = new AsnTranscodeRequest();
        AsnCodecOptions options = new AsnCodecOptions();
        AsnReflectionCatalog catalog = new AsnReflectionCatalog();
        byte[] input = [1, 2, 3];
        byte[] destination = new byte[2];
        MemoryStream stream = new MemoryStream();

        Assert.That(codec.Encode(encodeRequest), Is.SameAs(implementation.EncodeResult));
        Assert.That(codec.Decode(decodeRequest), Is.SameAs(implementation.DecodeResult));
        Assert.That(codec.TryDecodeSpan(input, "Module.Type", options), Is.SameAs(implementation.DecodeResult));
        Assert.That(codec.EncodeToSpan(encodeRequest, destination), Is.SameAs(implementation.EncodeResult));
        Assert.That(codec.CheckCanonicality(decodeRequest), Is.SameAs(implementation.CanonicalityResult));
        Assert.That(codec.Transcode(transcodeRequest), Is.SameAs(implementation.TranscodeResult));
        Assert.That(codec.CreateStreamingCodec(), Is.SameAs(implementation));
        Assert.That(codec.CreateIncrementalDecoder(
            "Module.Type",
            options), Is.SameAs(implementation));
        Assert.That(destination[0], Is.EqualTo(42));

        IAsnCodecProvider provider = new AsnCodecProvider(implementation);
        Assert.That(provider.CreateCodec(catalog, options), Is.SameAs(implementation));

        IAsnStreamingCodec streaming = new AsnStreamingCodec(implementation);
        Assert.That(streaming.EncodeStream(encodeRequest, stream), Is.SameAs(implementation.EncodeResult));
        Assert.That(streaming.DecodeStream(stream, "Module.Type", options), Is.SameAs(implementation.DecodeResult));

        IAsnIncrementalDecoder incremental = new AsnIncrementalDecoder(implementation);
        Assert.That(incremental.Consume(new AsnIncrementalInput()), Is.SameAs(implementation.IncrementalResult));
        Assert.That(incremental.Complete(), Is.SameAs(implementation.IncrementalResult));
        incremental.Reset();

        Assert.That(implementation.Calls, Is.EqualTo(new string[]
        {
            "Encode",
            "Decode",
            "TryDecodeSpan:3:Module.Type",
            "EncodeToSpan:2",
            "CheckCanonicality",
            "Transcode",
            "CreateStreamingCodec",
            "CreateIncrementalDecoder:Module.Type",
            "CreateCodec",
            "EncodeStream",
            "DecodeStream:Module.Type",
            "Consume",
            "Complete",
            "Reset"
        }));
    }

    [Test]
    public void TlvAndRegistryFacadesForwardOperations()
    {
        RecordingImplementation implementation = new RecordingImplementation();
        BerReadRequest readRequest = new BerReadRequest();
        BerWriteRequest writeRequest = new BerWriteRequest();
        byte[] input = [1, 2];
        byte[] destination = new byte[3];
        MemoryStream stream = new MemoryStream();

        IBerTlvReader reader = new BerTlvReader(implementation);
        Assert.That(reader.Read(readRequest), Is.SameAs(implementation.ReadResult));
        Assert.That(reader.ReadSpan(input, AsnEncodingRules.Der), Is.SameAs(implementation.ReadResult));
        Assert.That(reader.ReadStream(stream, AsnEncodingRules.Cer), Is.SameAs(implementation.ReadResult));

        IBerTlvWriter writer = new BerTlvWriter(implementation);
        Assert.That(writer.Write(writeRequest), Is.SameAs(implementation.WriteResult));
        Assert.That(writer.WriteSpan(writeRequest, destination), Is.SameAs(implementation.WriteResult));
        Assert.That(writer.WriteStream(writeRequest, stream), Is.SameAs(implementation.WriteResult));
        Assert.That(destination[0], Is.EqualTo(24));

        IPerInstructionRegistry registry = new PerInstructionRegistry(implementation);
        AsnPerInstructionRegistration registration =
            new AsnPerInstructionRegistration { Keyword = "ADDED" };
        registry.RegisterHandler(registration, implementation);
        Assert.That(registry.ResolveHandler("CUSTOM"), Is.SameAs(implementation));
        Assert.That(registry.GetRegistrations(), Is.SameAs(implementation.Registrations));

        Assert.That(implementation.Calls, Is.EqualTo(new string[]
        {
            "Read",
            "ReadSpan:2:Der",
            "ReadStream:Cer",
            "Write",
            "WriteSpan:3",
            "WriteStream",
            "GetRegistrations",
            "RegisterHandler:ADDED",
            "ResolveHandler:CUSTOM",
            "GetRegistrations"
        }));
    }

    [Test]
    public void PerInstructionRegistryRejectsDuplicateKeywordsAndMissingArguments()
    {
        RecordingImplementation implementation = new RecordingImplementation();
        IPerInstructionRegistry registry = new PerInstructionRegistry(implementation);

        Assert.Multiple(() =>
        {
            Assert.That(() => registry.RegisterHandler(
                    implementation.Registrations[0],
                    implementation),
                Throws.ArgumentException);
            Assert.That(() => registry.RegisterHandler(null!, implementation),
                Throws.ArgumentNullException);
            Assert.That(() => registry.RegisterHandler(
                    new AsnPerInstructionRegistration { Keyword = "NEW" },
                    null!),
                Throws.ArgumentNullException);
            Assert.That(() => registry.RegisterHandler(
                    new AsnPerInstructionRegistration(),
                    implementation),
                Throws.ArgumentException);
            Assert.That(() => registry.RegisterHandler(
                    new AsnPerInstructionRegistration { Keyword = null! },
                    implementation),
                Throws.ArgumentException);
            Assert.That(() => registry.ResolveHandler(null!),
                Throws.ArgumentNullException);
            Assert.That(() => registry.ResolveHandler(""),
                Throws.ArgumentException);
        });
    }

    [Test]
    public void PerInstructionRegistryNormalizesInvalidBackendCollections()
    {
        RecordingImplementation missing = new RecordingImplementation
        {
            Registrations = null!
        };
        RecordingImplementation nullEntry = new RecordingImplementation
        {
            Registrations = [null!]
        };
        IPerInstructionRegistry missingFacade = new PerInstructionRegistry(missing);
        IPerInstructionRegistry nullEntryFacade = new PerInstructionRegistry(nullEntry);

        Assert.That(() => missingFacade.GetRegistrations(),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => missingFacade.RegisterHandler(
                new AsnPerInstructionRegistration { Keyword = "NEW" },
                missing),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => nullEntryFacade.GetRegistrations(),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => nullEntryFacade.RegisterHandler(
                new AsnPerInstructionRegistration { Keyword = "NEW" },
                nullEntry),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void InterfaceTypedImplementationsSupportRepeatedIndependentConcurrentUse()
    {
        System.Collections.Concurrent.ConcurrentBag<int> callCounts = [];

        System.Threading.Tasks.Parallel.For(0, 16, iteration =>
        {
            RecordingImplementation implementation = new RecordingImplementation();
            IAsnCompiler compiler = new AsnCompiler(implementation);
            IAsnLexer lexer = new AsnLexer(implementation);
            IAsnParser parser = new AsnParser(implementation);
            ISchemaValidator schema = new AsnSchemaValidator(implementation);
            IAsnValueValidator values = new AsnValueValidator(implementation);
            IAsnCodeGenerator generator = new AsnCodeGenerator(implementation);
            IAsnReflectionService reflection = new AsnReflectionService(implementation);
            IAsnCodec codec = new AsnCodec(implementation);
            IAsnCodecProvider provider = new AsnCodecProvider(implementation);
            IAsnStreamingCodec streaming = new AsnStreamingCodec(implementation);
            IAsnIncrementalDecoder incremental = new AsnIncrementalDecoder(implementation);
            IBerTlvReader reader = new BerTlvReader(implementation);
            IBerTlvWriter writer = new BerTlvWriter(implementation);
            IEcnCompiler ecn = new EcnCompiler(implementation);
            IPerInstructionRegistry per = new PerInstructionRegistry(implementation);
            IXsdSchemaImporter xsd = new XsdSchemaImporter(implementation);

            compiler.Compile(new AsnCompilationInput());
            compiler.Compile(new AsnCompilationInput());
            lexer.TokenizeSource(new AsnSourceUnit());
            parser.ParseSource(new AsnSourceUnit());
            schema.ValidateModules([]);
            values.ValidateRuntimeValue(new AsnRuntimeValue(), new AsnTypeDescriptor());
            generator.GenerateFiles(
                new AsnValidationResult(),
                new AsnGenerationOptions());
            reflection.GetTypes(new AsnReflectionCatalog());
            codec.Encode(new AsnEncodeRequest());
            provider.CreateCodec(new AsnReflectionCatalog(), new AsnCodecOptions());
            streaming.EncodeStream(
                new AsnEncodeRequest(),
                new MemoryStream());
            incremental.Complete();
            reader.Read(new BerReadRequest());
            writer.Write(new BerWriteRequest());
            ecn.CompileEcn(new AsnSourceUnit(), []);
            per.GetRegistrations();
            xsd.ImportSchemas([], new AsnXsdImportOptions());

            IAsnObjectSetRegistry objectSets = new AsnObjectSetRegistry();
            Assert.That(objectSets.GetEffectiveObjects(
                new AsnInformationObjectSet()), Is.Empty);
            IAsnValueValidator runtime = new AsnRuntimeConstraintEvaluator();
            Assert.That(runtime.ValidateRuntimeValue(
                new AsnRuntimeValue(),
                new AsnTypeDescriptor()), Is.Empty);
            callCounts.Add(implementation.Calls.Count);
        });

        Assert.That(callCounts, Has.Count.EqualTo(16));
        Assert.That(callCounts, Has.All.EqualTo(17));
    }

    [Test]
    public void FacadesRejectMissingImplementations()
    {
        Assert.That(() => new AsnCompiler(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnLexer(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnParser(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnSchemaValidator(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnValueValidator(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnCodeGenerator(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnReflectionService(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnCodec(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnCodecProvider(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnStreamingCodec(null!), Throws.ArgumentNullException);
        Assert.That(() => new AsnIncrementalDecoder(null!), Throws.ArgumentNullException);
        Assert.That(() => new BerTlvReader(null!), Throws.ArgumentNullException);
        Assert.That(() => new BerTlvWriter(null!), Throws.ArgumentNullException);
        Assert.That(() => new EcnCompiler(null!), Throws.ArgumentNullException);
        Assert.That(() => new XsdSchemaImporter(null!), Throws.ArgumentNullException);
        Assert.That(() => new PerInstructionRegistry(null!), Throws.ArgumentNullException);
    }

    private class RecordingImplementation :
        IAsnCompiler,
        IAsnLexer,
        IAsnParser,
        ISchemaValidator,
        IAsnValueValidator,
        IAsnCodeGenerator,
        IAsnReflectionService,
        IAsnCodec,
        IAsnCodecProvider,
        IAsnStreamingCodec,
        IAsnIncrementalDecoder,
        IBerTlvReader,
        IBerTlvWriter,
        IEcnCompiler,
        IXsdSchemaImporter,
        IPerInstructionRegistry,
        IPerInstructionHandler
    {
        public List<string> Calls = [];
        public object? LastObject;
        public AsnCompilationResult CompilationResult = new AsnCompilationResult();
        public AsnLexerResult LexerResult = new AsnLexerResult();
        public AsnParserResult ParserResult = new AsnParserResult();
        public AsnValidationResult ValidationResult = new AsnValidationResult();
        public AsnDiagnostic[] Diagnostics = [new AsnDiagnostic()];
        public AsnGeneratedFile[] GeneratedFiles = [new AsnGeneratedFile()];
        public AsnReflectionCatalog Catalog = new AsnReflectionCatalog();
        public AsnModuleDescriptor ModuleDescriptor = new AsnModuleDescriptor();
        public AsnTypeDescriptor TypeDescriptor = new AsnTypeDescriptor();
        public AsnValueDescriptor ValueDescriptor = new AsnValueDescriptor();
        public AsnTypeDescriptor[] TypeDescriptors = [new AsnTypeDescriptor()];
        public AsnEncodeResult EncodeResult = new AsnEncodeResult();
        public AsnDecodeResult DecodeResult = new AsnDecodeResult();
        public AsnCanonicalityResult CanonicalityResult = new AsnCanonicalityResult();
        public AsnTranscodeResult TranscodeResult = new AsnTranscodeResult();
        public AsnIncrementalResult IncrementalResult = new AsnIncrementalResult();
        public BerReadResult ReadResult = new BerReadResult();
        public BerWriteResult WriteResult = new BerWriteResult();
        public AsnXsdImportResult XsdImportResult = new AsnXsdImportResult();
        public AsnPerInstructionRegistration[] Registrations = [new AsnPerInstructionRegistration { Keyword = "CUSTOM" }];

        public AsnCompilationResult Compile(AsnCompilationInput input)
        {
            this.Calls.Add("Compile");
            this.LastObject = input;
            return this.CompilationResult;
        }

        public AsnLexerResult TokenizeSource(AsnSourceUnit source)
        {
            this.Calls.Add("TokenizeSource");
            this.LastObject = source;
            return this.LexerResult;
        }

        public AsnLexerResult TokenizeSpan(ReadOnlySpan<char> text, string sourceName)
        {
            this.Calls.Add($"TokenizeSpan:{text.Length}:{sourceName}");
            return this.LexerResult;
        }

        public AsnLexerResult ContinueTokenization(AsnSourceUnit source, int offset)
        {
            this.Calls.Add($"ContinueTokenization:{offset}");
            this.LastObject = source;
            return this.LexerResult;
        }

        public AsnParserResult ParseTokens(ReadOnlySpan<AsnToken> tokens)
        {
            this.Calls.Add($"ParseTokens:{tokens.Length}");
            return this.ParserResult;
        }

        public AsnParserResult ParseSource(AsnSourceUnit source)
        {
            this.Calls.Add("ParseSource");
            this.LastObject = source;
            return this.ParserResult;
        }

        public AsnParserResult ContinueParsing(ReadOnlySpan<AsnToken> tokens, int tokenOffset)
        {
            this.Calls.Add($"ContinueParsing:{tokens.Length}:{tokenOffset}");
            return this.ParserResult;
        }

        public AsnValidationResult ValidateModules(ReadOnlySpan<AsnModule> modules)
        {
            this.Calls.Add($"ValidateModules:{modules.Length}");
            return this.ValidationResult;
        }

        public AsnDiagnostic[] ValidateRuntimeValue(AsnRuntimeValue value, AsnTypeDescriptor type)
        {
            this.Calls.Add("ValidateRuntimeValue");
            return this.Diagnostics;
        }

        public AsnGeneratedFile[] GenerateFiles(AsnValidationResult validation, AsnGenerationOptions options)
        {
            this.Calls.Add("GenerateFiles");
            return this.GeneratedFiles;
        }

        public AsnReflectionCatalog BuildCatalog(AsnValidationResult validation)
        {
            this.Calls.Add("BuildCatalog");
            return this.Catalog;
        }

        public AsnModuleDescriptor? ResolveModule(AsnReflectionCatalog catalog, string moduleName)
        {
            this.Calls.Add($"ResolveModule:{moduleName}");
            return this.ModuleDescriptor;
        }

        public AsnTypeDescriptor? ResolveType(AsnReflectionCatalog catalog, string moduleName, string typeName)
        {
            this.Calls.Add($"ResolveType:{moduleName}:{typeName}");
            return this.TypeDescriptor;
        }

        public AsnValueDescriptor? ResolveValue(AsnReflectionCatalog catalog, string moduleName, string valueName)
        {
            this.Calls.Add($"ResolveValue:{moduleName}:{valueName}");
            return this.ValueDescriptor;
        }

        public AsnTypeDescriptor[] GetTypes(AsnReflectionCatalog catalog)
        {
            this.Calls.Add("GetTypes");
            return this.TypeDescriptors;
        }

        public AsnEncodeResult Encode(AsnEncodeRequest request)
        {
            this.Calls.Add("Encode");
            return this.EncodeResult;
        }

        public AsnDecodeResult Decode(AsnDecodeRequest request)
        {
            this.Calls.Add("Decode");
            return this.DecodeResult;
        }

        public AsnDecodeResult TryDecodeSpan(ReadOnlySpan<byte> input, string typeName, AsnCodecOptions options)
        {
            this.Calls.Add($"TryDecodeSpan:{input.Length}:{typeName}");
            return this.DecodeResult;
        }

        public AsnEncodeResult EncodeToSpan(AsnEncodeRequest request, Span<byte> destination)
        {
            this.Calls.Add($"EncodeToSpan:{destination.Length}");
            destination[0] = 42;
            return this.EncodeResult;
        }

        public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)
        {
            this.Calls.Add("CheckCanonicality");
            return this.CanonicalityResult;
        }

        public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
        {
            this.Calls.Add("Transcode");
            return this.TranscodeResult;
        }

        public IAsnStreamingCodec CreateStreamingCodec()
        {
            this.Calls.Add("CreateStreamingCodec");
            return this;
        }

        public IAsnIncrementalDecoder CreateIncrementalDecoder(
            string typeName,
            AsnCodecOptions options)
        {
            this.Calls.Add("CreateIncrementalDecoder:" + typeName);
            return this;
        }

        public IAsnCodec CreateCodec(AsnReflectionCatalog catalog, AsnCodecOptions options)
        {
            this.Calls.Add("CreateCodec");
            return this;
        }

        public AsnEncodeResult EncodeStream(AsnEncodeRequest request, Stream output)
        {
            this.Calls.Add("EncodeStream");
            return this.EncodeResult;
        }

        public AsnDecodeResult DecodeStream(Stream input, string typeName, AsnCodecOptions options)
        {
            this.Calls.Add($"DecodeStream:{typeName}");
            return this.DecodeResult;
        }

        public AsnIncrementalResult Consume(AsnIncrementalInput input)
        {
            this.Calls.Add("Consume");
            return this.IncrementalResult;
        }

        public AsnIncrementalResult Complete()
        {
            this.Calls.Add("Complete");
            return this.IncrementalResult;
        }

        public void Reset()
        {
            this.Calls.Add("Reset");
        }

        public BerReadResult Read(BerReadRequest request)
        {
            this.Calls.Add("Read");
            return this.ReadResult;
        }

        public BerReadResult ReadSpan(ReadOnlySpan<byte> input, AsnEncodingRules rules)
        {
            this.Calls.Add($"ReadSpan:{input.Length}:{rules}");
            return this.ReadResult;
        }

        public BerReadResult ReadStream(Stream input, AsnEncodingRules rules)
        {
            this.Calls.Add($"ReadStream:{rules}");
            return this.ReadResult;
        }

        public BerWriteResult Write(BerWriteRequest request)
        {
            this.Calls.Add("Write");
            return this.WriteResult;
        }

        public BerWriteResult WriteSpan(BerWriteRequest request, Span<byte> destination)
        {
            this.Calls.Add($"WriteSpan:{destination.Length}");
            destination[0] = 24;
            return this.WriteResult;
        }

        public BerWriteResult WriteStream(BerWriteRequest request, Stream output)
        {
            this.Calls.Add("WriteStream");
            return this.WriteResult;
        }

        public AsnCompilationResult CompileEcn(AsnSourceUnit source, ReadOnlySpan<AsnModule> modules)
        {
            this.Calls.Add($"CompileEcn:{modules.Length}");
            return this.CompilationResult;
        }

        public AsnXsdImportResult ImportSchemas(ReadOnlySpan<AsnXsdInput> inputs, AsnXsdImportOptions options)
        {
            this.Calls.Add($"ImportSchemas:{inputs.Length}");
            return this.XsdImportResult;
        }

        public void RegisterHandler(AsnPerInstructionRegistration registration, IPerInstructionHandler handler)
        {
            this.Calls.Add($"RegisterHandler:{registration.Keyword}");
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            this.Calls.Add($"ResolveHandler:{keyword}");
            return this;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            this.Calls.Add("GetRegistrations");
            return this.Registrations;
        }

        public AsnDiagnostic[] ValidateInstruction(AsnEncodingInstruction instruction, AsnType type)
        {
            return this.Diagnostics;
        }

        public AsnEncodingPlanStep[] ApplyInstruction(AsnEncodingInstruction instruction, AsnType type, AsnEncodingPlan plan)
        {
            return [];
        }
    }
}
