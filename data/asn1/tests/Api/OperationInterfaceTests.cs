using asn1;

namespace asn1.Tests;

public class OperationInterfaceTests
{
    [Test]
    public void CompilerAndSchemaContractsCarryTheirDedicatedResults()
    {
        OperationFacade facade = new OperationFacade();
        AsnSourceUnit source = new AsnSourceUnit { Name = "module.asn", Text = "Module" };
        AsnToken[] tokens = [new AsnToken { Kind = AsnTokenKind.ModuleReference }];
        AsnModule[] modules = [new AsnModule { Name = "Module" }];

        Assert.That(facade.Compile(new AsnCompilationInput()), Is.SameAs(facade.CompilationResult));
        Assert.That(facade.TokenizeSource(source), Is.SameAs(facade.LexerResult));
        Assert.That(facade.TokenizeSpan(source.Text.AsSpan(), source.Name), Is.SameAs(facade.LexerResult));
        Assert.That(facade.ContinueTokenization(source, 1), Is.SameAs(facade.LexerResult));
        Assert.That(facade.ParseTokens(tokens), Is.SameAs(facade.ParserResult));
        Assert.That(facade.ParseSource(source), Is.SameAs(facade.ParserResult));
        Assert.That(facade.ContinueParsing(tokens, 0), Is.SameAs(facade.ParserResult));
        Assert.That(facade.ValidateModules(modules), Is.SameAs(facade.ValidationResult));
        Assert.That(facade.ValidateRuntimeValue(new AsnRuntimeValue(), facade.TypeDescriptor), Is.SameAs(facade.Diagnostics));
        Assert.That(facade.GenerateFiles(facade.ValidationResult, new AsnGenerationOptions()), Is.SameAs(facade.GeneratedFiles));
        Assert.That(facade.BuildCatalog(facade.ValidationResult), Is.SameAs(facade.Catalog));
        Assert.That(facade.ResolveModule(facade.Catalog, "Module"), Is.SameAs(facade.ModuleDescriptor));
        Assert.That(facade.ResolveType(facade.Catalog, "Module", "Type"), Is.SameAs(facade.TypeDescriptor));
        Assert.That(facade.ResolveValue(facade.Catalog, "Module", "value"), Is.SameAs(facade.ValueDescriptor));
        Assert.That(facade.GetTypes(facade.Catalog), Is.SameAs(facade.TypeDescriptors));
    }

    [Test]
    public void CodecContractsSupportOneShotSpanStreamAndIncrementalCalls()
    {
        OperationFacade facade = new OperationFacade();
        AsnCodecOptions options = new AsnCodecOptions();
        AsnEncodeRequest encodeRequest = new AsnEncodeRequest();
        AsnDecodeRequest decodeRequest = new AsnDecodeRequest();
        AsnTranscodeRequest transcodeRequest = new AsnTranscodeRequest();
        byte[] input = [0x01];
        byte[] destination = new byte[1];
        MemoryStream inputStream = new MemoryStream(input);
        MemoryStream outputStream = new MemoryStream();

        Assert.That(facade.CreateCodec(facade.Catalog, options), Is.SameAs(facade));
        Assert.That(facade.Encode(encodeRequest), Is.SameAs(facade.EncodeResult));
        Assert.That(facade.Decode(decodeRequest), Is.SameAs(facade.DecodeResult));
        Assert.That(facade.TryDecodeSpan(input, "Module.Type", options), Is.SameAs(facade.DecodeResult));
        Assert.That(facade.EncodeToSpan(encodeRequest, destination), Is.SameAs(facade.EncodeResult));
        Assert.That(destination[0], Is.EqualTo(0x2a));
        Assert.That(facade.CheckCanonicality(decodeRequest), Is.SameAs(facade.CanonicalityResult));
        Assert.That(facade.Transcode(transcodeRequest), Is.SameAs(facade.TranscodeResult));
        Assert.That(facade.CreateStreamingCodec(), Is.SameAs(facade));
        Assert.That(facade.CreateIncrementalDecoder(
            "Module.Type",
            options), Is.SameAs(facade));
        Assert.That(facade.EncodeStream(encodeRequest, outputStream), Is.SameAs(facade.EncodeResult));
        Assert.That(outputStream.ToArray(), Is.EqualTo(new byte[] { 0x2a }));
        Assert.That(facade.DecodeStream(inputStream, "Module.Type", options), Is.SameAs(facade.DecodeResult));
        Assert.That(facade.Consume(new AsnIncrementalInput()), Is.SameAs(facade.IncrementalResult));
        Assert.That(facade.Complete(), Is.SameAs(facade.IncrementalResult));
        facade.Reset();
        Assert.That(facade.ResetCount, Is.EqualTo(1));

        Assert.That(facade.Read(new BerReadRequest()), Is.SameAs(facade.ReadResult));
        Assert.That(facade.ReadSpan(input, AsnEncodingRules.Ber), Is.SameAs(facade.ReadResult));
        inputStream.Position = 0;
        Assert.That(facade.ReadStream(inputStream, AsnEncodingRules.Ber), Is.SameAs(facade.ReadResult));
        Assert.That(facade.Write(new BerWriteRequest()), Is.SameAs(facade.WriteResult));
        destination[0] = 0;
        Assert.That(facade.WriteSpan(new BerWriteRequest(), destination), Is.SameAs(facade.WriteResult));
        Assert.That(destination[0], Is.EqualTo(0x2b));
        Assert.That(facade.WriteStream(new BerWriteRequest(), outputStream), Is.SameAs(facade.WriteResult));
    }

    [Test]
    public void EcnXsdAndPerExtensionContractsAreSynchronousAndComposable()
    {
        OperationFacade facade = new OperationFacade();
        AsnSourceUnit ecnSource = new AsnSourceUnit { Name = "encoding.ecn", Text = "encoding" };
        AsnModule[] modules = [new AsnModule { Name = "Module" }];
        AsnXsdInput[] xsdInputs = [new AsnXsdInput { Name = "schema.xsd", Text = "<schema />" }];
        AsnPerInstructionRegistration registration = new AsnPerInstructionRegistration
        {
            Keyword = "APPLICATION-RULE",
            HandlerId = "handler"
        };

        Assert.That(facade.CompileEcn(ecnSource, modules), Is.SameAs(facade.CompilationResult));
        Assert.That(facade.ImportSchemas(xsdInputs, new AsnXsdImportOptions()), Is.SameAs(facade.XsdImportResult));
        facade.RegisterHandler(registration, facade);
        Assert.That(facade.ResolveHandler(registration.Keyword), Is.SameAs(facade));
        Assert.That(facade.GetRegistrations(), Is.SameAs(facade.Registrations));
        Assert.That(facade.ValidateInstruction(new AsnEncodingInstruction(), new AsnType()), Is.SameAs(facade.Diagnostics));
        Assert.That(
            facade.ApplyInstruction(new AsnEncodingInstruction(), new AsnType(), new AsnEncodingPlan()),
            Is.SameAs(facade.PlanSteps));
    }

    private class OperationFacade :
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
        public AsnCompilationResult CompilationResult = new AsnCompilationResult();
        public AsnLexerResult LexerResult = new AsnLexerResult();
        public AsnParserResult ParserResult = new AsnParserResult();
        public AsnValidationResult ValidationResult = new AsnValidationResult();
        public AsnDiagnostic[] Diagnostics = [];
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
        public AsnPerInstructionRegistration[] Registrations = [];
        public AsnEncodingPlanStep[] PlanSteps = [new AsnEncodingPlanStep()];
        public IPerInstructionHandler? Handler;
        public int ResetCount;

        public AsnCompilationResult Compile(AsnCompilationInput input)
        {
            return this.CompilationResult;
        }

        public AsnLexerResult TokenizeSource(AsnSourceUnit source)
        {
            return this.LexerResult;
        }

        public AsnLexerResult TokenizeSpan(ReadOnlySpan<char> text, string sourceName)
        {
            return this.LexerResult;
        }

        public AsnLexerResult ContinueTokenization(AsnSourceUnit source, int offset)
        {
            return this.LexerResult;
        }

        public AsnParserResult ParseTokens(ReadOnlySpan<AsnToken> tokens)
        {
            return this.ParserResult;
        }

        public AsnParserResult ParseSource(AsnSourceUnit source)
        {
            return this.ParserResult;
        }

        public AsnParserResult ContinueParsing(ReadOnlySpan<AsnToken> tokens, int tokenOffset)
        {
            return this.ParserResult;
        }

        public AsnValidationResult ValidateModules(ReadOnlySpan<AsnModule> modules)
        {
            return this.ValidationResult;
        }

        public AsnDiagnostic[] ValidateRuntimeValue(AsnRuntimeValue value, AsnTypeDescriptor type)
        {
            return this.Diagnostics;
        }

        public AsnGeneratedFile[] GenerateFiles(AsnValidationResult validation, AsnGenerationOptions options)
        {
            return this.GeneratedFiles;
        }

        public AsnReflectionCatalog BuildCatalog(AsnValidationResult validation)
        {
            return this.Catalog;
        }

        public AsnModuleDescriptor? ResolveModule(AsnReflectionCatalog catalog, string moduleName)
        {
            return this.ModuleDescriptor;
        }

        public AsnTypeDescriptor? ResolveType(AsnReflectionCatalog catalog, string moduleName, string typeName)
        {
            return this.TypeDescriptor;
        }

        public AsnValueDescriptor? ResolveValue(AsnReflectionCatalog catalog, string moduleName, string valueName)
        {
            return this.ValueDescriptor;
        }

        public AsnTypeDescriptor[] GetTypes(AsnReflectionCatalog catalog)
        {
            return this.TypeDescriptors;
        }

        public AsnEncodeResult Encode(AsnEncodeRequest request)
        {
            return this.EncodeResult;
        }

        public AsnDecodeResult Decode(AsnDecodeRequest request)
        {
            return this.DecodeResult;
        }

        public AsnDecodeResult TryDecodeSpan(ReadOnlySpan<byte> input, string typeName, AsnCodecOptions options)
        {
            return this.DecodeResult;
        }

        public AsnEncodeResult EncodeToSpan(AsnEncodeRequest request, Span<byte> destination)
        {
            if (destination.Length > 0)
            {
                destination[0] = 0x2a;
            }

            return this.EncodeResult;
        }

        public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)
        {
            return this.CanonicalityResult;
        }

        public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
        {
            return this.TranscodeResult;
        }

        public IAsnStreamingCodec CreateStreamingCodec()
        {
            return this;
        }

        public IAsnIncrementalDecoder CreateIncrementalDecoder(
            string typeName,
            AsnCodecOptions options)
        {
            return this;
        }

        public IAsnCodec CreateCodec(AsnReflectionCatalog catalog, AsnCodecOptions options)
        {
            return this;
        }

        public AsnEncodeResult EncodeStream(AsnEncodeRequest request, Stream output)
        {
            output.WriteByte(0x2a);
            return this.EncodeResult;
        }

        public AsnDecodeResult DecodeStream(Stream input, string typeName, AsnCodecOptions options)
        {
            input.ReadByte();
            return this.DecodeResult;
        }

        public AsnIncrementalResult Consume(AsnIncrementalInput input)
        {
            return this.IncrementalResult;
        }

        public AsnIncrementalResult Complete()
        {
            return this.IncrementalResult;
        }

        public void Reset()
        {
            this.ResetCount = this.ResetCount + 1;
        }

        public BerReadResult Read(BerReadRequest request)
        {
            return this.ReadResult;
        }

        public BerReadResult ReadSpan(ReadOnlySpan<byte> input, AsnEncodingRules rules)
        {
            return this.ReadResult;
        }

        public BerReadResult ReadStream(Stream input, AsnEncodingRules rules)
        {
            return this.ReadResult;
        }

        public BerWriteResult Write(BerWriteRequest request)
        {
            return this.WriteResult;
        }

        public BerWriteResult WriteSpan(BerWriteRequest request, Span<byte> destination)
        {
            if (destination.Length > 0)
            {
                destination[0] = 0x2b;
            }

            return this.WriteResult;
        }

        public BerWriteResult WriteStream(BerWriteRequest request, Stream output)
        {
            output.WriteByte(0x2b);
            return this.WriteResult;
        }

        public AsnCompilationResult CompileEcn(AsnSourceUnit source, ReadOnlySpan<AsnModule> modules)
        {
            return this.CompilationResult;
        }

        public AsnXsdImportResult ImportSchemas(ReadOnlySpan<AsnXsdInput> inputs, AsnXsdImportOptions options)
        {
            return this.XsdImportResult;
        }

        public void RegisterHandler(AsnPerInstructionRegistration registration, IPerInstructionHandler handler)
        {
            this.Handler = handler;
            this.Registrations = [registration];
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            return this.Handler;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            return this.Registrations;
        }

        public AsnDiagnostic[] ValidateInstruction(AsnEncodingInstruction instruction, AsnType type)
        {
            return this.Diagnostics;
        }

        public AsnEncodingPlanStep[] ApplyInstruction(
            AsnEncodingInstruction instruction,
            AsnType type,
            AsnEncodingPlan plan)
        {
            return this.PlanSteps;
        }
    }
}
