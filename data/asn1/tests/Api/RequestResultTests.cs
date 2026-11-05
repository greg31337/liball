using asn1;

namespace asn1.Tests;

public class RequestResultTests
{
    [Test]
    public void CompilationPayloadCarriesEveryRequestedPhase()
    {
        AsnSourceUnit source = new AsnSourceUnit { Name = "module.asn", Text = "Module DEFINITIONS ::= BEGIN END" };
        AsnXsdInput xsd = new AsnXsdInput
        {
            Name = "schema.xsd",
            Text = "<schema />",
            HasBaseUri = true,
            BaseUri = "urn:example:schema"
        };
        AsnCompilerOptions compilerOptions = new AsnCompilerOptions
        {
            EnableLegacyAny = true,
            TreatWarningsAsErrors = true,
            EncodingRules = AsnEncodingRules.Der,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve
        };
        AsnGenerationOptions generationOptions = new AsnGenerationOptions
        {
            Target = AsnCodeGenerationTarget.Combined,
            NamespaceName = "Generated.Model",
            IncludeSourceMappings = true
        };
        AsnXsdImportOptions xsdOptions = new AsnXsdImportOptions
        {
            HasModuleName = true,
            ModuleName = "MappedModule",
            PreserveAnnotations = true
        };
        AsnCompilationInput input = new AsnCompilationInput
        {
            SourceUnits = [source],
            XsdInputs = [xsd],
            XsdImportOptions = xsdOptions,
            CompilerOptions = compilerOptions,
            GenerationOptions = generationOptions
        };

        AsnLexerResult lexer = new AsnLexerResult
        {
            Tokens = [new AsnToken { Kind = AsnTokenKind.ModuleReference }],
            ConsumedCharacterCount = source.Text.Length,
            Complete = true
        };
        AsnParserResult parser = new AsnParserResult
        {
            Assignments = [new AsnAssignment { Kind = AsnAssignmentKind.Type }],
            ConsumedTokenCount = 1,
            Complete = true
        };
        AsnValidationResult validation = new AsnValidationResult
        {
            Success = true,
            Assignments = parser.Assignments
        };
        AsnGeneratedFile generated = new AsnGeneratedFile
        {
            RelativePath = "Generated/Module.cs",
            Content = "namespace Generated.Model;",
            SourceSpans = [source.GetSpan(0, 6)]
        };
        AsnXsdImportResult xsdResult = new AsnXsdImportResult
        {
            Success = true,
            SourceUnits = [source]
        };
        AsnCompilationResult result = new AsnCompilationResult
        {
            Success = true,
            LexerResults = [lexer],
            ParserResults = [parser],
            ValidationResult = validation,
            GeneratedFiles = [generated],
            XsdImportResults = [xsdResult]
        };

        Assert.That(input.SourceUnits[0], Is.SameAs(source));
        Assert.That(input.XsdInputs[0].BaseUri, Is.EqualTo("urn:example:schema"));
        Assert.That(input.CompilerOptions, Is.SameAs(compilerOptions));
        Assert.That(input.GenerationOptions?.NamespaceName, Is.EqualTo("Generated.Model"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.LexerResults[0].ConsumedCharacterCount, Is.EqualTo(source.Text.Length));
        Assert.That(result.ParserResults[0].ConsumedTokenCount, Is.EqualTo(1));
        Assert.That(result.ValidationResult?.Success, Is.True);
        Assert.That(result.GeneratedFiles[0].RelativePath, Is.EqualTo("Generated/Module.cs"));
        Assert.That(result.XsdImportResults[0].Success, Is.True);
    }

    [Test]
    public void PhaseResultDefaultsProvideEmptyCollections()
    {
        AsnCompilationInput input = new AsnCompilationInput();
        AsnCompilationResult compilation = new AsnCompilationResult();
        AsnLexerResult lexer = new AsnLexerResult();
        AsnParserResult parser = new AsnParserResult();
        AsnValidationResult validation = new AsnValidationResult();
        AsnGeneratedFile generated = new AsnGeneratedFile();
        AsnXsdImportResult xsd = new AsnXsdImportResult();

        Assert.That(input.SourceUnits, Is.Empty);
        Assert.That(input.XsdInputs, Is.Empty);
        Assert.That(compilation.Diagnostics, Is.Empty);
        Assert.That(compilation.LexerResults, Is.Empty);
        Assert.That(compilation.ParserResults, Is.Empty);
        Assert.That(compilation.GeneratedFiles, Is.Empty);
        Assert.That(compilation.XsdImportResults, Is.Empty);
        Assert.That(lexer.Tokens, Is.Empty);
        Assert.That(lexer.Diagnostics, Is.Empty);
        Assert.That(parser.Assignments, Is.Empty);
        Assert.That(parser.Diagnostics, Is.Empty);
        Assert.That(validation.Assignments, Is.Empty);
        Assert.That(validation.Diagnostics, Is.Empty);
        Assert.That(generated.SourceSpans, Is.Empty);
        Assert.That(generated.Diagnostics, Is.Empty);
        Assert.That(xsd.SourceUnits, Is.Empty);
        Assert.That(xsd.Diagnostics, Is.Empty);
    }

    [Test]
    public void XsdPresenceFlagsDistinguishAbsentAndEmptyValues()
    {
        AsnXsdInput absentBase = new AsnXsdInput();
        AsnXsdInput emptyBase = new AsnXsdInput { HasBaseUri = true, BaseUri = "" };
        AsnXsdImportOptions absentModule = new AsnXsdImportOptions();
        AsnXsdImportOptions emptyModule = new AsnXsdImportOptions { HasModuleName = true, ModuleName = "" };

        Assert.That(absentBase.HasBaseUri, Is.False);
        Assert.That(emptyBase.HasBaseUri, Is.True);
        Assert.That(absentBase.BaseUri, Is.EqualTo(emptyBase.BaseUri));
        Assert.That(absentModule.HasModuleName, Is.False);
        Assert.That(emptyModule.HasModuleName, Is.True);
        Assert.That(absentModule.ModuleName, Is.EqualTo(emptyModule.ModuleName));
    }

    [Test]
    public void CodecPresenceFlagsDistinguishAbsentAndZeroValues()
    {
        AsnDecodeRequest wholeInput = new AsnDecodeRequest { Data = [1, 2, 3] };
        AsnDecodeRequest emptyRegion = new AsnDecodeRequest { Data = [1, 2, 3], HasCount = true, Count = 0 };
        AsnIncrementalInput wholeChunk = new AsnIncrementalInput { Data = [1] };
        AsnIncrementalInput emptyChunk = new AsnIncrementalInput { Data = [1], HasCount = true, Count = 0 };
        AsnStreamProgress unknownTotal = new AsnStreamProgress();
        AsnStreamProgress zeroTotal = new AsnStreamProgress { HasExpectedCount = true, ExpectedCount = 0 };
        AsnCanonicalityResult noViolation = new AsnCanonicalityResult();
        AsnCanonicalityResult firstByteViolation = new AsnCanonicalityResult
        {
            Status = AsnCanonicalityStatus.NonCanonical,
            HasViolationOffset = true,
            ViolationOffset = 0
        };

        Assert.That(wholeInput.HasCount, Is.False);
        Assert.That(emptyRegion.HasCount, Is.True);
        Assert.That(wholeInput.Count, Is.EqualTo(emptyRegion.Count));
        Assert.That(wholeChunk.HasCount, Is.False);
        Assert.That(emptyChunk.HasCount, Is.True);
        Assert.That(unknownTotal.HasExpectedCount, Is.False);
        Assert.That(zeroTotal.HasExpectedCount, Is.True);
        Assert.That(noViolation.HasViolationOffset, Is.False);
        Assert.That(firstByteViolation.HasViolationOffset, Is.True);
        Assert.That(firstByteViolation.ViolationOffset, Is.Zero);
    }

    [Test]
    public void CodecPayloadsPreserveValuesProgressAndDiagnostics()
    {
        AsnDiagnostic diagnostic = new AsnDiagnostic { Code = "ASN2001", Message = "Incomplete value." };
        AsnRuntimeValue value = new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Integer };
        AsnResourceLimits limits = new AsnResourceLimits { Unlimited = true };
        AsnCodecOptions options = new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Uper,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = false,
            ResourceLimits = limits
        };
        AsnEncodeRequest encodeRequest = new AsnEncodeRequest
        {
            TypeName = "Example.Value",
            Value = value,
            Options = options
        };
        AsnStreamProgress progress = new AsnStreamProgress
        {
            ConsumedCount = 1,
            WrittenCount = 2,
            Complete = true
        };
        AsnEncodeResult encodeResult = new AsnEncodeResult
        {
            Success = true,
            Data = [0x01, 0x02],
            WrittenCount = 2,
            StreamProgress = progress
        };
        AsnDecodeResult decodeResult = new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = value,
            ConsumedCount = 2,
            StreamProgress = progress
        };
        AsnIncrementalResult incrementalResult = new AsnIncrementalResult
        {
            Status = AsnDecodeStatus.NeedMoreData,
            ConsumedCount = 1,
            Diagnostics = [diagnostic]
        };

        Assert.That(encodeRequest.Options?.ResourceLimits?.Unlimited, Is.True);
        Assert.That(encodeRequest.Value, Is.SameAs(value));
        Assert.That(encodeResult.Data, Is.EqualTo(new byte[] { 0x01, 0x02 }));
        Assert.That(encodeResult.WrittenCount, Is.EqualTo(2));
        Assert.That(decodeResult.Value, Is.SameAs(value));
        Assert.That(decodeResult.StreamProgress, Is.SameAs(progress));
        Assert.That(incrementalResult.Status, Is.EqualTo(AsnDecodeStatus.NeedMoreData));
        Assert.That(incrementalResult.Diagnostics[0], Is.SameAs(diagnostic));
    }

    [Test]
    public void CodecContractReturnsDistinctOperationResults()
    {
        ResultCodec codec = new ResultCodec();
        AsnEncodeRequest encodeRequest = new AsnEncodeRequest();
        AsnDecodeRequest decodeRequest = new AsnDecodeRequest();
        AsnCodecOptions options = new AsnCodecOptions();
        byte[] destination = new byte[1];

        AsnEncodeResult encodeResult = codec.Encode(encodeRequest);
        AsnDecodeResult decodeResult = codec.Decode(decodeRequest);
        AsnDecodeResult spanDecodeResult = codec.TryDecodeSpan([], "Example.Value", options);
        AsnEncodeResult spanEncodeResult = codec.EncodeToSpan(encodeRequest, destination);
        AsnCanonicalityResult canonicalityResult = codec.CheckCanonicality(decodeRequest);
        AsnTranscodeResult transcodeResult = codec.Transcode(
            new AsnTranscodeRequest());

        Assert.That(encodeResult, Is.SameAs(codec.EncodeResult));
        Assert.That(decodeResult, Is.SameAs(codec.DecodeResult));
        Assert.That(spanDecodeResult, Is.SameAs(codec.DecodeResult));
        Assert.That(spanEncodeResult, Is.SameAs(codec.EncodeResult));
        Assert.That(canonicalityResult, Is.SameAs(codec.CanonicalityResult));
        Assert.That(transcodeResult, Is.SameAs(codec.TranscodeResult));
    }

    private class ResultCodec : IAsnCodec
    {
        public AsnEncodeResult EncodeResult = new AsnEncodeResult();
        public AsnDecodeResult DecodeResult = new AsnDecodeResult();
        public AsnCanonicalityResult CanonicalityResult = new AsnCanonicalityResult();
        public AsnTranscodeResult TranscodeResult = new AsnTranscodeResult();

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
            throw new NotSupportedException();
        }

        public IAsnIncrementalDecoder CreateIncrementalDecoder(
            string typeName,
            AsnCodecOptions options)
        {
            throw new NotSupportedException();
        }

    }
}
