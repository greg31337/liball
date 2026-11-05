using cbor;

namespace cbor.Tests;

[TestFixture]
public class ApiReachabilityCoverageTests
{
    [Test]
    public void StreamReaderReportsSharingFailureAfterTokenAssembly()
    {
        CborStreamReader reader = new CborStreamReader(
            new MemoryStream([0xd8, 0x1d, 0x00]),
            new CborDecodeOptions
            {
                SharedValuePolicy = CborSharedValuePolicy.Resolve
            });

        CborReadResult failure = reader.ReadNext();
        CborReadResult retry = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(failure.ErrorCode,
                Is.EqualTo("CBOR_SHARED_REFERENCE_NOT_FOUND"));
            Assert.That(failure.ErrorPath, Is.EqualTo("$"));
            Assert.That(failure.Consumed, Is.EqualTo(3));
            Assert.That(retry.ErrorCode,
                Is.EqualTo("CBOR_STREAM_READER_FAULTED"));
        });
    }

    [Test]
    public void StreamSequenceByteLimitsRejectCompletedOversizedItems()
    {
        CborStreamReader reader = new CborStreamReader(
            new MemoryStream([0x61, 0x78]),
            new CborDecodeOptions { MaxSequenceBytes = 1 });
        MemoryStream output = new MemoryStream();
        CborStreamWriter writer = new CborStreamWriter(
            output,
            new CborEncodeOptions { MaxSequenceBytes = 1 });
        CborValue text = new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = "x"
        };

        CborReadResult read = reader.ReadNext();
        CborWriteResult write = writer.WriteNext(text);

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(read.ErrorCode,
                Is.EqualTo("CBOR_MAX_SEQUENCE_BYTES"));
            Assert.That(read.Consumed, Is.EqualTo(2));
            Assert.That(write.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(write.ErrorCode,
                Is.EqualTo("CBOR_MAX_SEQUENCE_BYTES"));
            Assert.That(write.Required, Is.EqualTo(2));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void StreamWriterDisposesSafelyWithMissingConstructorInputs()
    {
        DisposeTrackingStream output = new DisposeTrackingStream();
        CborStreamWriter missingOptions = new CborStreamWriter(
            output,
            null!);
        CborStreamWriter missingOutput = new CborStreamWriter(
            null!,
            new CborEncodeOptions());

        missingOptions.Dispose();
        missingOutput.Dispose();
        missingOutput.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(output.DisposeCalls, Is.EqualTo(1));
            Assert.That(output.IsDisposed, Is.True);
        });
    }

    [Test]
    public void JsonValidationCanSuppressMissingInputIssues()
    {
        CddlSchema schema = Compile("root = uint");
        CddlValidationOptions options = new CddlValidationOptions
        {
            MaxIssues = 0
        };
        ICddlJsonValidator validator = new CddlJsonValidator();

        CddlValidationResult missingJson = validator.ValidateJson(
            schema,
            null!,
            options);
        CddlValidationResult missingSchema = validator.ValidateJson(
            null!,
            "0",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(missingJson.IsValid, Is.False);
            Assert.That(missingJson.Issues, Is.Empty);
            Assert.That(missingJson.Options, Is.SameAs(options));
            Assert.That(missingSchema.IsValid, Is.False);
            Assert.That(missingSchema.Issues, Is.Empty);
        });
    }

    [Test]
    public void CompilerRejectsUnknownControlPolicyBelowDefinedRange()
    {
        CddlDocument document = new CddlParser().ParseDocument(
            "root = uint",
            new CddlParseOptions());
        CddlCompileOptions options = new CddlCompileOptions
        {
            UnknownControlPolicy = (CddlUnknownControlPolicy)(-1)
        };

        CddlCompileResult result = new CddlCompiler().TryCompileSchema(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_COMPILE_UNKNOWN_CONTROL_POLICY_INVALID"));
        });
    }

    [Test]
    public void EncodedValidationReportsEmptyInputWithoutDiagnosticsWhenCapped()
    {
        CddlValidationOptions options = new CddlValidationOptions
        {
            MaxIssues = 0
        };
        CddlValidationResult result = new CddlValidator().ValidateEncodedCbor(
            Compile("root = uint"),
            [],
            new CborDecodeOptions(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Options, Is.SameAs(options));
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

    private class DisposeTrackingStream : Stream
    {
        public int DisposeCalls;
        public bool IsDisposed;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position
        {
            get { return 0; }
            set { }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalls = DisposeCalls + 1;
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
