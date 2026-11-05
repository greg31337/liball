using raw;

namespace raw.Tests;

public class PublicGenericWriterDestinationTests
{
    [Test]
    public void DryRunsCoverEveryStoredAndNormalizedInputAndPredictExactOutput()
    {
        IRawWriter writer = new RawWriterService();
        RawDescriptor unsignedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor signedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
        RawDescriptor floatingDescriptor = RawDescriptor.CreateGray(2, 1, 32, 32, RawByteOrder.LittleEndian);
        floatingDescriptor.SampleKind = RawSampleKind.Single;
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();

        byte[] unsignedBytes = writer.EncodeGenericUnsigned(unsignedDescriptor, new uint[] { 1, 250 }, 0, 2, options);
        byte[] signedBytes = writer.EncodeGenericSigned(signedDescriptor, new int[] { -100, 100 }, 0, 2, options);
        byte[] floatingBytes = writer.EncodeGenericFloating(floatingDescriptor, new float[] { -1.25F, 2.5F }, 0, 2, options);
        byte[] normalizedUnsignedBytes = writer.EncodeGenericNormalizedUnsigned(unsignedDescriptor, new ushort[] { 0, 65535 }, 0, 2, options);
        byte[] normalizedFloatingBytes = writer.EncodeGenericNormalizedFloating(unsignedDescriptor, new float[] { 0.25F, 0.75F }, 0, 2, options);

        AssertPlan(writer.PlanGenericUnsigned(unsignedDescriptor, new uint[] { 1, 250 }, 0, 2, options), unsignedBytes.Length);
        AssertPlan(writer.PlanGenericSigned(signedDescriptor, new int[] { -100, 100 }, 0, 2, options), signedBytes.Length);
        AssertPlan(writer.PlanGenericFloating(floatingDescriptor, new float[] { -1.25F, 2.5F }, 0, 2, options), floatingBytes.Length);
        AssertPlan(writer.PlanGenericNormalizedUnsigned(unsignedDescriptor, new ushort[] { 0, 65535 }, 0, 2, options), normalizedUnsignedBytes.Length);
        AssertPlan(writer.PlanGenericNormalizedFloating(unsignedDescriptor, new float[] { 0.25F, 0.75F }, 0, 2, options), normalizedFloatingBytes.Length);
    }

    [Test]
    public void CallerOwnedStreamsCoverEveryInputFormAndRemainOpen()
    {
        IRawWriter writer = new RawWriterService();
        RawDescriptor unsignedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor signedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
        RawDescriptor floatingDescriptor = RawDescriptor.CreateGray(2, 1, 32, 32, RawByteOrder.BigEndian);
        floatingDescriptor.SampleKind = RawSampleKind.Single;
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();
        uint[] unsigned = new uint[] { 1, 250 };
        int[] signed = new int[] { -100, 100 };
        float[] floating = new float[] { -1.25F, 2.5F };
        ushort[] normalizedUnsigned = new ushort[] { 0, 65535 };
        float[] normalizedFloating = new float[] { 0.25F, 0.75F };
        GenericWriteTrackingStream unsignedOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream signedOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream floatingOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream normalizedUnsignedOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream normalizedFloatingOutput = new GenericWriteTrackingStream();

        writer.EncodeGenericUnsignedToStream(unsignedOutput, unsignedDescriptor, unsigned, 0, 2, options);
        writer.EncodeGenericSignedToStream(signedOutput, signedDescriptor, signed, 0, 2, options);
        writer.EncodeGenericFloatingToStream(floatingOutput, floatingDescriptor, floating, 0, 2, options);
        writer.EncodeGenericNormalizedUnsignedToStream(normalizedUnsignedOutput, unsignedDescriptor, normalizedUnsigned, 0, 2, options);
        writer.EncodeGenericNormalizedFloatingToStream(normalizedFloatingOutput, unsignedDescriptor, normalizedFloating, 0, 2, options);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedOutput.Bytes(), Is.EqualTo(writer.EncodeGenericUnsigned(unsignedDescriptor, unsigned, 0, 2, options)));
            Assert.That(signedOutput.Bytes(), Is.EqualTo(writer.EncodeGenericSigned(signedDescriptor, signed, 0, 2, options)));
            Assert.That(floatingOutput.Bytes(), Is.EqualTo(writer.EncodeGenericFloating(floatingDescriptor, floating, 0, 2, options)));
            Assert.That(normalizedUnsignedOutput.Bytes(), Is.EqualTo(writer.EncodeGenericNormalizedUnsigned(unsignedDescriptor, normalizedUnsigned, 0, 2, options)));
            Assert.That(normalizedFloatingOutput.Bytes(), Is.EqualTo(writer.EncodeGenericNormalizedFloating(unsignedDescriptor, normalizedFloating, 0, 2, options)));
            Assert.That(new GenericWriteTrackingStream[] { unsignedOutput, signedOutput, floatingOutput, normalizedUnsignedOutput, normalizedFloatingOutput }, Has.All.Matches<GenericWriteTrackingStream>(output => !output.Disposed));
        });
    }

    [Test]
    public void ValidatedPathsCoverEveryInputFormAndCommitExactBytes()
    {
        IRawWriter writer = new RawWriterService();
        RawDescriptor unsignedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawDescriptor signedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
        RawDescriptor floatingDescriptor = RawDescriptor.CreateGray(2, 1, 32, 32, RawByteOrder.LittleEndian);
        floatingDescriptor.SampleKind = RawSampleKind.Single;
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();
        uint[] unsigned = new uint[] { 1, 250 };
        int[] signed = new int[] { -100, 100 };
        float[] floating = new float[] { -1.25F, 2.5F };
        ushort[] normalizedUnsigned = new ushort[] { 0, 65535 };
        float[] normalizedFloating = new float[] { 0.25F, 0.75F };
        string directory = Path.Combine(Path.GetTempPath(), "raw-public-generic-writer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string unsignedPath = Path.Combine(directory, "unsigned.raw");
            string signedPath = Path.Combine(directory, "signed.raw");
            string floatingPath = Path.Combine(directory, "floating.raw");
            string normalizedUnsignedPath = Path.Combine(directory, "normalized-unsigned.raw");
            string normalizedFloatingPath = Path.Combine(directory, "normalized-floating.raw");
            RawPathWriteResult unsignedResult = writer.EncodeGenericUnsignedToPath(unsignedPath, unsignedDescriptor, unsigned, 0, 2, options);
            RawPathWriteResult signedResult = writer.EncodeGenericSignedToPath(signedPath, signedDescriptor, signed, 0, 2, options);
            RawPathWriteResult floatingResult = writer.EncodeGenericFloatingToPath(floatingPath, floatingDescriptor, floating, 0, 2, options);
            RawPathWriteResult normalizedUnsignedResult = writer.EncodeGenericNormalizedUnsignedToPath(normalizedUnsignedPath, unsignedDescriptor, normalizedUnsigned, 0, 2, options);
            RawPathWriteResult normalizedFloatingResult = writer.EncodeGenericNormalizedFloatingToPath(normalizedFloatingPath, unsignedDescriptor, normalizedFloating, 0, 2, options);

            AssertCommitted(unsignedResult, unsignedPath, writer.EncodeGenericUnsigned(unsignedDescriptor, unsigned, 0, 2, options));
            AssertCommitted(signedResult, signedPath, writer.EncodeGenericSigned(signedDescriptor, signed, 0, 2, options));
            AssertCommitted(floatingResult, floatingPath, writer.EncodeGenericFloating(floatingDescriptor, floating, 0, 2, options));
            AssertCommitted(normalizedUnsignedResult, normalizedUnsignedPath, writer.EncodeGenericNormalizedUnsigned(unsignedDescriptor, normalizedUnsigned, 0, 2, options));
            AssertCommitted(normalizedFloatingResult, normalizedFloatingPath, writer.EncodeGenericNormalizedFloating(unsignedDescriptor, normalizedFloating, 0, 2, options));
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(5));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DestinationOperationsRetainAllExplicitNumericAndNonsamplePolicies()
    {
        IRawWriter writer = new RawWriterService();
        RawDescriptor unsignedDescriptor = RawDescriptor.CreateGray(3, 1, 4, 8, RawByteOrder.LittleEndian);
        RawGenericEncodeOptions towardZero = new RawGenericEncodeOptions();
        towardZero.RoundingMode = RawSampleRoundingMode.TowardZero;
        RawGenericEncodeOptions clamp = new RawGenericEncodeOptions();
        clamp.RangePolicy = RawSampleRangePolicy.Clamp;
        GenericWriteTrackingStream nearestOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream roundedOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream clampedOutput = new GenericWriteTrackingStream();
        writer.EncodeGenericNormalizedFloatingToStream(nearestOutput, unsignedDescriptor, new float[] { 0.25F, 0.5F, 0.75F }, 0, 3, new RawGenericEncodeOptions());
        writer.EncodeGenericNormalizedFloatingToStream(roundedOutput, unsignedDescriptor, new float[] { 0.25F, 0.5F, 0.75F }, 0, 3, towardZero);
        writer.EncodeGenericNormalizedFloatingToStream(clampedOutput, unsignedDescriptor, new float[] { -1.0F, 0.5F, 2.0F }, 0, 3, clamp);

        RawDescriptor signedDescriptor = RawDescriptor.CreateGray(3, 1, 4, 8, RawByteOrder.LittleEndian);
        signedDescriptor.SampleKind = RawSampleKind.SignedInteger;
        RawGenericEncodeOptions nonnegative = new RawGenericEncodeOptions();
        nonnegative.SignedPolicy = RawSignedEncodingPolicy.NonNegative;
        GenericWriteTrackingStream fullRangeOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream signedOutput = new GenericWriteTrackingStream();
        writer.EncodeGenericNormalizedUnsignedToStream(fullRangeOutput, signedDescriptor, new ushort[] { 0, 32768, 65535 }, 0, 3, new RawGenericEncodeOptions());
        writer.EncodeGenericNormalizedUnsignedToStream(signedOutput, signedDescriptor, new ushort[] { 0, 32768, 65535 }, 0, 3, nonnegative);

        RawDescriptor floatingDescriptor = RawDescriptor.CreateGray(3, 1, 32, 32, RawByteOrder.LittleEndian);
        floatingDescriptor.SampleKind = RawSampleKind.Single;
        RawGenericEncodeOptions finiteClamp = new RawGenericEncodeOptions();
        finiteClamp.NonFinitePolicy = RawNonFinitePolicy.Clamp;
        GenericWriteTrackingStream floatingOutput = new GenericWriteTrackingStream();
        writer.EncodeGenericFloatingToStream(floatingOutput, floatingDescriptor, new float[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }, 0, 3, finiteClamp);
        float[] decodedFloating = new float[3];
        IRawCodec codec = new RawCodecService();
        codec.DecodeGenericFloating(floatingOutput.Bytes(), floatingDescriptor, WholeRegion(floatingDescriptor), decodedFloating, new RawDecodeOptions());

        RawDescriptor paddedDescriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        paddedDescriptor.DataOffset = 2;
        paddedDescriptor.DataLength = 5;
        paddedDescriptor.AllowTrailingBytes = true;
        RawGenericEncodeOptions preserve = new RawGenericEncodeOptions();
        preserve.PaddingByte = 0xA5;
        preserve.TrailingBytePolicy = RawTrailingBytePolicy.Preserve;
        preserve.PreservedBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        RawGenericEncodeOptions zeroFill = new RawGenericEncodeOptions();
        zeroFill.PaddingByte = 0xA5;
        zeroFill.TrailingBytePolicy = RawTrailingBytePolicy.ZeroFill;
        GenericWriteTrackingStream zeroFilledOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream preservedOutput = new GenericWriteTrackingStream();
        writer.EncodeGenericUnsignedToStream(zeroFilledOutput, paddedDescriptor, new uint[] { 10, 20 }, 0, 2, zeroFill);
        writer.EncodeGenericUnsignedToStream(preservedOutput, paddedDescriptor, new uint[] { 10, 20 }, 0, 2, preserve);

        Assert.Multiple(() =>
        {
            Assert.That(nearestOutput.Bytes(), Is.EqualTo(new byte[] { 4, 8, 11 }));
            Assert.That(roundedOutput.Bytes(), Is.EqualTo(new byte[] { 3, 7, 11 }));
            Assert.That(clampedOutput.Bytes(), Is.EqualTo(new byte[] { 0, 8, 15 }));
            Assert.That(fullRangeOutput.Bytes(), Is.EqualTo(new byte[] { 0xF8, 0, 7 }));
            Assert.That(signedOutput.Bytes(), Is.EqualTo(new byte[] { 0, 4, 7 }));
            Assert.That(decodedFloating[0], Is.Zero);
            Assert.That(decodedFloating[1], Is.EqualTo(float.MaxValue));
            Assert.That(decodedFloating[2], Is.EqualTo(-float.MaxValue));
            Assert.That(zeroFilledOutput.Bytes(), Is.EqualTo(new byte[] { 0xA5, 0xA5, 10, 20, 0, 0, 0 }));
            Assert.That(preservedOutput.Bytes(), Is.EqualTo(new byte[] { 1, 2, 10, 20, 5, 6, 7 }));
            Assert.Throws<RawWriteException>(() => writer.PlanGenericNormalizedFloating(unsignedDescriptor, new float[] { -0.1F, 0.5F, 1.1F }, 0, 3, new RawGenericEncodeOptions()));
            Assert.Throws<RawWriteException>(() => writer.PlanGenericFloating(floatingDescriptor, new float[] { float.NaN, 0, 1 }, 0, 3, RejectNonFinite()));
            Assert.Throws<RawWriteException>(() => writer.PlanGenericUnsigned(paddedDescriptor, new uint[] { 10, 20 }, 0, 2, new RawGenericEncodeOptions()));
        });
    }

    [Test]
    public void InvalidAndCancelledOperationsFailBeforeWritingOrCreatingDestinations()
    {
        IRawWriter writer = new RawWriterService();
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        GenericWriteTrackingStream invalidOutput = new GenericWriteTrackingStream();
        GenericWriteTrackingStream cancelledOutput = new GenericWriteTrackingStream();
        RawGenericEncodeOptions cancelled = new RawGenericEncodeOptions();
        cancelled.CancellationToken = new CancellationToken(true);
        string path = Path.Combine(Path.GetTempPath(), "raw-public-generic-cancelled-" + Guid.NewGuid().ToString("N") + ".raw");

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => writer.EncodeGenericUnsignedToStream(invalidOutput, descriptor, new uint[] { 1, 256 }, 0, 2, new RawGenericEncodeOptions()));
            Assert.Throws<OperationCanceledException>(() => writer.EncodeGenericUnsignedToStream(cancelledOutput, descriptor, new uint[] { 1, 2 }, 0, 2, cancelled));
            Assert.Throws<OperationCanceledException>(() => writer.PlanGenericUnsigned(descriptor, new uint[] { 1, 2 }, 0, 2, cancelled));
            Assert.Throws<OperationCanceledException>(() => writer.EncodeGenericUnsignedToPath(path, descriptor, new uint[] { 1, 2 }, 0, 2, cancelled));
            Assert.That(invalidOutput.Bytes(), Is.Empty);
            Assert.That(cancelledOutput.Bytes(), Is.Empty);
            Assert.That(invalidOutput.Disposed, Is.False);
            Assert.That(cancelledOutput.Disposed, Is.False);
            Assert.That(File.Exists(path), Is.False);
        });
    }

    private static void AssertPlan(RawWritePlan plan, int expectedSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(expectedSize));
            Assert.That(plan.Diagnostics, Is.Empty);
            Assert.That(plan.Changes, Has.Length.EqualTo(1));
            Assert.That(plan.Changes[0].Kind, Is.EqualTo(RawWriteChangeKind.Added));
            Assert.That(plan.Changes[0].Location, Is.EqualTo("GenericRaw/Data"));
            Assert.That(plan.Changes[0].PlannedOffset, Is.Zero);
            Assert.That(plan.Changes[0].PlannedLength, Is.EqualTo(expectedSize));
        });
    }

    private static void AssertCommitted(RawPathWriteResult result, string path, byte[] expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Validated, Is.True);
            Assert.That(result.Committed, Is.True);
            Assert.That(result.DestinationPath, Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(result.TemporaryPath, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
        });
    }

    private static RawDecodeRegion WholeRegion(RawDescriptor descriptor)
    {
        return new RawDecodeRegion(0, 0, descriptor.Width, descriptor.Height, 0, descriptor.ChannelCount, 0, descriptor.Width * descriptor.ChannelCount);
    }

    private static RawGenericEncodeOptions RejectNonFinite()
    {
        RawGenericEncodeOptions options = new RawGenericEncodeOptions();
        options.NonFinitePolicy = RawNonFinitePolicy.Reject;
        return options;
    }
}

internal class GenericWriteTrackingStream : Stream
{
    internal MemoryStream Inner;
    internal bool Disposed;

    internal GenericWriteTrackingStream()
    {
        Inner = new MemoryStream();
        Disposed = false;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    internal byte[] Bytes()
    {
        return Inner.ToArray();
    }

    public override void Flush()
    {
        Inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Inner.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        Inner.Dispose();
        base.Dispose(disposing);
    }
}
