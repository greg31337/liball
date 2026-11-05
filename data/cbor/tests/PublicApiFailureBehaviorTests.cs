using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class PublicApiFailureBehaviorTests
{
    [Test]
    public void EncoderAndSequenceDecoderReportMissingInputs()
    {
        ICborEncoder encoder = new CborEncoder();
        ICborDecoder decoder = new CborDecoder();
        CborValue value = Integer(1);

        CborEncodeException encode = Assert.Throws<CborEncodeException>(() =>
            encoder.Encode(value, null!))!;
        CborWriteResult encodeInto = encoder.EncodeInto(
            value,
            [],
            null!);
        CborEncodeException measure = Assert.Throws<CborEncodeException>(() =>
            encoder.Measure(value, null!))!;
        CborEncodeException sequenceOptions =
            Assert.Throws<CborEncodeException>(() =>
                encoder.EncodeSequence([value], null!))!;
        CborEncodeException sequenceValues =
            Assert.Throws<CborEncodeException>(() =>
                encoder.EncodeSequence(null!, new CborEncodeOptions()))!;
        CborReadResult decodeOptions = decoder.DecodeSequence(
            [],
            null!)[0];
        CborReadResult decodeSource = decoder.DecodeSequence(
            null!,
            new CborDecodeOptions())[0];

        Assert.Multiple(() =>
        {
            Assert.That(encode.ErrorCode,
                Is.EqualTo("CBOR_ENCODE_OPTIONS_MISSING"));
            Assert.That(encode.ErrorPath, Is.EqualTo("$"));
            AssertWriteError(encodeInto, "CBOR_ENCODE_OPTIONS_MISSING");
            Assert.That(measure.ErrorCode,
                Is.EqualTo("CBOR_ENCODE_OPTIONS_MISSING"));
            Assert.That(sequenceOptions.ErrorCode,
                Is.EqualTo("CBOR_ENCODE_OPTIONS_MISSING"));
            Assert.That(sequenceValues.ErrorCode,
                Is.EqualTo("CBOR_SEQUENCE_VALUES_MISSING"));
            AssertReadError(decodeOptions, "CBOR_DECODE_OPTIONS_MISSING");
            AssertReadError(decodeSource, "CBOR_SOURCE_MISSING");
        });
    }

    [Test]
    public void ParserReportsMissingInputsThroughBothApiStyles()
    {
        ICddlParser parser = new CddlParser();
        CddlParseResult missingOptions = parser.TryParseDocument(
            "root = uint",
            null!);
        CddlParseOptions options = new CddlParseOptions
        {
            SourceName = "missing-source.cddl"
        };
        CddlParseResult missingSource = parser.TryParseDocument(null!, options);
        CddlParseException throwingOptions =
            Assert.Throws<CddlParseException>(() =>
                parser.ParseDocument("root = uint", null!))!;
        CddlParseException throwingSource =
            Assert.Throws<CddlParseException>(() =>
                parser.ParseDocument(null!, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingOptions.IsSuccess, Is.False);
            Assert.That(missingOptions.Document, Is.Null);
            Assert.That(missingOptions.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_OPTIONS_MISSING"));
            Assert.That(missingSource.IsSuccess, Is.False);
            Assert.That(missingSource.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_SOURCE_MISSING"));
            Assert.That(missingSource.Diagnostics[0].Span.SourceName,
                Is.EqualTo("missing-source.cddl"));
            Assert.That(throwingOptions.ErrorCode,
                Is.EqualTo("CDDL_PARSE_OPTIONS_MISSING"));
            Assert.That(throwingSource.ErrorCode,
                Is.EqualTo("CDDL_PARSE_SOURCE_MISSING"));
        });
    }

    [Test]
    public void CompilerReportsMissingInputsAndInvalidRegistrationCollections()
    {
        ICddlCompiler compiler = new CddlCompiler();
        CddlDocument document = new CddlParser().ParseDocument(
            "root = uint",
            new CddlParseOptions());
        CddlCompileResult missingOptions = compiler.TryCompileSchema(
            document,
            null!);
        CddlCompileOptions options = new CddlCompileOptions();
        CddlCompileResult missingDocument = compiler.TryCompileSchema(
            null!,
            options);
        CddlCompileOptions missingRegistrations = new CddlCompileOptions();
        missingRegistrations.ControlRegistrations = null!;
        CddlCompileResult missingCollection = compiler.TryCompileSchema(
            document,
            missingRegistrations);
        CddlCompileOptions missingEntry = new CddlCompileOptions();
        missingEntry.ControlRegistrations.Add(null!);
        CddlCompileResult invalidEntry = compiler.TryCompileSchema(
            document,
            missingEntry);
        CddlCompileOptions invalidPolicyOptions = new CddlCompileOptions
        {
            UnknownControlPolicy = (CddlUnknownControlPolicy)999
        };
        CddlCompileResult invalidPolicy = compiler.TryCompileSchema(
            document,
            invalidPolicyOptions);
        CddlCompileException throwing =
            Assert.Throws<CddlCompileException>(() =>
                compiler.CompileSchema(null!, options))!;

        Assert.Multiple(() =>
        {
            AssertCompileFailure(
                missingOptions,
                "CDDL_COMPILE_OPTIONS_MISSING");
            AssertCompileFailure(
                missingDocument,
                "CDDL_COMPILE_DOCUMENT_MISSING");
            AssertCompileFailure(
                missingCollection,
                "CDDL_COMPILE_CONTROL_REGISTRATIONS_MISSING");
            AssertCompileFailure(
                invalidEntry,
                "CDDL_COMPILE_CONTROL_REGISTRATION_INVALID");
            AssertCompileFailure(
                invalidPolicy,
                "CDDL_COMPILE_UNKNOWN_CONTROL_POLICY_INVALID");
            Assert.That(throwing.ErrorCode,
                Is.EqualTo("CDDL_COMPILE_DOCUMENT_MISSING"));
        });
    }

    [Test]
    public void EncodedCddlValidationReportsEveryMissingInputCategory()
    {
        ICddlValidator validator = new CddlValidator();
        CddlSchema schema = Compile("root = uint");
        CddlValidationOptions validationOptions =
            new CddlValidationOptions();
        CborDecodeOptions decodeOptions = new CborDecodeOptions();

        CddlValidationResult missingValidationOptions =
            validator.ValidateEncodedCbor(
                schema,
                [0x00],
                decodeOptions,
                null!);
        CddlValidationResult missingSchema = validator.ValidateEncodedCbor(
            null!,
            [0x00],
            decodeOptions,
            validationOptions);
        CddlValidationResult missingDecodeOptions =
            validator.ValidateEncodedCbor(
                schema,
                [0x00],
                null!,
                validationOptions);
        CddlValidationResult missingSource = validator.ValidateEncodedCbor(
            schema,
            null!,
            decodeOptions,
            validationOptions);

        Assert.Multiple(() =>
        {
            AssertValidationFailure(
                missingValidationOptions,
                CddlValidationCode.InvalidSchema,
                "validation options");
            AssertValidationFailure(
                missingSchema,
                CddlValidationCode.InvalidSchema,
                "a compiled schema");
            AssertValidationFailure(
                missingDecodeOptions,
                CddlValidationCode.InvalidEncoding,
                "decoding options");
            AssertValidationFailure(
                missingSource,
                CddlValidationCode.InvalidEncoding,
                "one encoded CBOR data item");
        });
    }

    [Test]
    public void EncodedCddlValidationForwardsCustomTagRegistrations()
    {
        CddlSchema schema = Compile("root = #6.100(uint)");
        ValidTagHandler handler = new ValidTagHandler();
        CborDecodeOptions decodeOptions = new CborDecodeOptions
        {
            TagPolicy = CborTagPolicy.RejectUnknown,
            TagRegistrations =
            [
                new CborTagRegistration
                {
                    TagNumber = 100,
                    Handler = handler
                }
            ]
        };

        CddlValidationResult result = new CddlValidator().ValidateEncodedCbor(
            schema,
            Convert.FromHexString("D86401"),
            decodeOptions,
            new CddlValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True,
                result.Issues.Length == 0
                    ? null
                    : result.Issues[0].Message);
            Assert.That(handler.ValidationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void StreamReaderReportsConfigurationCapabilityAndPolicyFailures()
    {
        CborReadResult missingInput = new CborStreamReader(
            null!,
            new CborDecodeOptions()).ReadNext();
        CborReadResult missingOptions = new CborStreamReader(
            new MemoryStream(),
            null!).ReadNext();
        MemoryStream closed = new MemoryStream([0x00]);
        closed.Dispose();
        CborReadResult unreadable = new CborStreamReader(
            closed,
            new CborDecodeOptions()).ReadNext();
        CborReadResult invalidSequence = new CborStreamReader(
            new MemoryStream([0x00]),
            new CborDecodeOptions { MaxSequenceItems = -1 }).ReadNext();
        CborDecodeOptions missingTags = new CborDecodeOptions();
        missingTags.TagRegistrations = null!;
        CborReadResult invalidTags = new CborStreamReader(
            new MemoryStream([0x00]),
            missingTags).ReadNext();

        Assert.Multiple(() =>
        {
            AssertReadError(
                missingInput,
                "CBOR_STREAM_READER_CONFIGURATION_INVALID");
            AssertReadError(
                missingOptions,
                "CBOR_STREAM_READER_CONFIGURATION_INVALID");
            AssertReadError(unreadable, "CBOR_STREAM_NOT_READABLE");
            AssertReadError(
                invalidSequence,
                "CBOR_MAX_SEQUENCE_ITEMS");
            AssertReadError(
                invalidTags,
                "CBOR_TAG_REGISTRATIONS_MISSING");
        });
    }

    [Test]
    public void StreamWriterReportsConfigurationCapabilityAndPolicyFailures()
    {
        CborValue value = Integer(1);
        CborWriteResult missingOutput = new CborStreamWriter(
            null!,
            new CborEncodeOptions()).WriteNext(value);
        CborWriteResult missingOptions = new CborStreamWriter(
            new MemoryStream(),
            null!).WriteNext(value);
        CborWriteResult invalidSequence = new CborStreamWriter(
            new MemoryStream(),
            new CborEncodeOptions { MaxSequenceItems = -1 }).WriteNext(value);
        MemoryStream readOnly = new MemoryStream(new byte[8], false);
        CborWriteResult unwritable = new CborStreamWriter(
            readOnly,
            new CborEncodeOptions()).WriteNext(value);

        CborStreamWriter missingFlushConfiguration = new CborStreamWriter(
            null!,
            new CborEncodeOptions());
        CborEncodeException flushConfiguration =
            Assert.Throws<CborEncodeException>(() =>
                missingFlushConfiguration.FlushOutput())!;
        CborStreamWriter invalidFlushSequence = new CborStreamWriter(
            new MemoryStream(),
            new CborEncodeOptions { MaxSequenceItems = -1 });
        CborEncodeException flushSequence =
            Assert.Throws<CborEncodeException>(() =>
                invalidFlushSequence.FlushOutput())!;
        MemoryStream flushReadOnly = new MemoryStream(new byte[8], false);
        CborStreamWriter unwritableFlush = new CborStreamWriter(
            flushReadOnly,
            new CborEncodeOptions());
        CborEncodeException flushCapability =
            Assert.Throws<CborEncodeException>(() =>
                unwritableFlush.FlushOutput())!;

        Assert.Multiple(() =>
        {
            AssertWriteError(
                missingOutput,
                "CBOR_STREAM_WRITER_CONFIGURATION_INVALID");
            AssertWriteError(
                missingOptions,
                "CBOR_STREAM_WRITER_CONFIGURATION_INVALID");
            AssertWriteError(
                invalidSequence,
                "CBOR_MAX_SEQUENCE_ITEMS");
            AssertWriteError(unwritable, "CBOR_STREAM_NOT_WRITABLE");
            Assert.That(flushConfiguration.ErrorCode,
                Is.EqualTo("CBOR_STREAM_WRITER_CONFIGURATION_INVALID"));
            Assert.That(flushSequence.ErrorCode,
                Is.EqualTo("CBOR_MAX_SEQUENCE_ITEMS"));
            Assert.That(flushCapability.ErrorCode,
                Is.EqualTo("CBOR_STREAM_NOT_WRITABLE"));
        });
    }

    [Test]
    public void GenerationRejectsReentryFromItsPublicHook()
    {
        CddlSchema schema = Compile("root = uint");
        CddlValueGenerator generator = new CddlValueGenerator();
        ReenteringGenerationHook hook = new ReenteringGenerationHook();
        hook.Generator = generator;
        hook.Schema = schema;

        CddlGenerationResult outer = generator.GenerateValue(
            schema,
            new CddlGenerationOptions { Hook = hook });

        Assert.Multiple(() =>
        {
            Assert.That(outer.IsSuccess, Is.True, outer.ErrorMessage);
            Assert.That(hook.BeginCalls, Is.EqualTo(1));
            Assert.That(hook.EndCalls, Is.EqualTo(1));
            Assert.That(hook.Nested, Is.Not.Null);
            Assert.That(hook.Nested!.IsSuccess, Is.False);
            Assert.That(hook.Nested.ErrorCode,
                Is.EqualTo("CDDL_GENERATION_REENTRANT"));
            Assert.That(hook.Nested.Value, Is.Null);
        });
    }

    private static CddlSchema Compile(string source)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions());
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions());
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static void AssertReadError(
        CborReadResult result,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
        Assert.That(result.Value, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(code));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    private static void AssertWriteError(
        CborWriteResult result,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
        Assert.That(result.ErrorCode, Is.EqualTo(code));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    private static void AssertCompileFailure(
        CddlCompileResult result,
        string code)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Schema, Is.Null);
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics[0].Message, Is.Not.Empty);
    }

    private static void AssertValidationFailure(
        CddlValidationResult result,
        CddlValidationCode code,
        string expected)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Expected, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Actual, Is.EqualTo("null"));
    }

    private class ReenteringGenerationHook : ICddlGenerationHook
    {
        public CddlValueGenerator? Generator;
        public CddlSchema? Schema;
        public CddlGenerationResult? Nested;
        public int BeginCalls;
        public int EndCalls;

        public void BeginGeneration(CddlGenerationContext context)
        {
            BeginCalls = BeginCalls + 1;
            Nested = Generator!.GenerateValue(
                Schema!,
                new CddlGenerationOptions());
        }

        public CddlControlResult GenerateNode(CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
            };
        }

        public void EndGeneration(
            CddlGenerationContext context,
            CddlGenerationResult result)
        {
            EndCalls = EndCalls + 1;
        }
    }

    private class ValidTagHandler : ICborTagHandler
    {
        public int ValidationCalls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls = ValidationCalls + 1;
            return ValidResult(content);
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return ValidResult(content);
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult
            {
                Handled = true,
                IsValid = true,
                NativeValue = nativeValue
            };
        }

        private CborTagResult ValidResult(CborValue value)
        {
            return new CborTagResult
            {
                Handled = true,
                IsValid = true,
                Value = value
            };
        }
    }
}
