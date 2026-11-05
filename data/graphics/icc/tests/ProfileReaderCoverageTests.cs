using icc;

namespace icc.Tests;

public class ProfileReaderCoverageTests
{
    [Test]
    public void EveryReadAndProbeInputAcceptsTheSameValidProfile()
    {
        byte[] bytes = CreateTaggedProfile(0x04400000);
        IccReadOptions options = new IccReadOptions();

        IccProfile[] profiles = ReadEveryInput(bytes, options);
        IccOperationResult[] probes = ProbeEveryInput(bytes, options);

        Assert.Multiple(() =>
        {
            foreach (IccProfile profile in profiles)
            {
                Assert.That(profile.ReadResult.Status,
                    Is.EqualTo(IccOperationStatus.Success));
                Assert.That(profile.Header.DeclaredSize,
                    Is.EqualTo(bytes.Length));
                Assert.That(profile.Tags, Has.Length.EqualTo(1));
            }

            foreach (IccOperationResult probe in probes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.Success));
                Assert.That(probe.Location.Length, Is.EqualTo(bytes.Length));
            }
        });
    }

    [Test]
    public void FutureMinorVersionsAreRejectedStrictlyAndRetainedInRecovery()
    {
        byte[] bytes = CreateTaggedProfile(0x04520000);
        IccReadOptions strict = new IccReadOptions();
        IccReadOptions recovery = new IccReadOptions();
        recovery.Mode = IccReadMode.Recovery;

        AssertEveryReadThrows<IccUnsupportedFeatureException>(bytes, strict);
        IccOperationResult[] strictProbes = ProbeEveryInput(bytes, strict);
        IccProfile[] recovered = ReadEveryInput(bytes, recovery);
        IccOperationResult[] recoveryProbes = ProbeEveryInput(bytes, recovery);

        Assert.Multiple(() =>
        {
            foreach (IccOperationResult probe in strictProbes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.UnsupportedFeature));
                Assert.That(probe.Location.Offset, Is.EqualTo(8));
            }

            foreach (IccProfile profile in recovered)
            {
                Assert.That(profile.Header.Version.Kind,
                    Is.EqualTo(IccVersionKind.Version4));
                Assert.That(profile.Header.Version.IsFutureMinor, Is.True);
                Assert.That(profile.Header.Version.IsSupported, Is.False);
                Assert.That(profile.Header.ValidationIssues.Any(issue =>
                    issue.RuleId == IccValidationRuleIds.HeaderVersion),
                    Is.True);
            }

            foreach (IccOperationResult probe in recoveryProbes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.Success));
            }
        });
    }

    [Test]
    public void RecoveryModeRetainsInvalidFileSignaturesForEveryInputKind()
    {
        byte[] bytes = CreateTaggedProfile(0x04400000);
        IndependentBigEndian.WriteUInt32(bytes, 36, 0x42414421);
        IccReadOptions strict = new IccReadOptions();
        IccReadOptions recovery = new IccReadOptions();
        recovery.Mode = IccReadMode.Recovery;

        AssertEveryReadThrows<IccProfileFormatException>(bytes, strict);
        IccOperationResult[] strictProbes = ProbeEveryInput(bytes, strict);
        IccProfile[] recovered = ReadEveryInput(bytes, recovery);
        IccOperationResult[] recoveryProbes = ProbeEveryInput(bytes, recovery);

        Assert.Multiple(() =>
        {
            foreach (IccOperationResult probe in strictProbes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.InvalidData));
                Assert.That(probe.Location.Offset, Is.EqualTo(36));
            }

            foreach (IccProfile profile in recovered)
            {
                Assert.That(profile.ReadResult.Status,
                    Is.EqualTo(IccOperationStatus.Success));
                Assert.That(profile.Header.ProfileFileSignature,
                    Is.EqualTo(0x42414421));
                Assert.That(profile.ValidationIssues.Any(issue =>
                    issue.RuleId == IccValidationRuleIds.HeaderFileSignature),
                    Is.True);
            }

            foreach (IccOperationResult probe in recoveryProbes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.Success));
            }
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(127)]
    [TestCase(131)]
    public void EveryReadAndProbeInputReportsPartialProfiles(int length)
    {
        byte[] complete = CreateMinimalProfile(0x04400000);
        byte[] partial = complete[..length];
        IccReadOptions options = new IccReadOptions();

        AssertEveryReadThrows<IccProfileFormatException>(partial, options);
        IccOperationResult[] probes = ProbeEveryInput(partial, options);

        Assert.Multiple(() =>
        {
            foreach (IccOperationResult probe in probes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.InvalidData));
                Assert.That(probe.Location.Length,
                    Is.LessThanOrEqualTo(length));
            }
        });
    }

    [Test]
    public void EveryBoundedEnvelopeLimitIsEnforcedByEveryOperation()
    {
        byte[] bytes = CreateTaggedProfile(0x04400000);
        (string Name, Action<IccResourceLimits> Configure)[] cases =
        {
            ("profile bytes", limits => limits.MaximumProfileBytes =
                bytes.Length - 1),
            ("tag count", limits => limits.MaximumTagCount = 0),
            ("tag bytes", limits => limits.MaximumTagBytes = 11),
            ("total tag bytes",
                limits => limits.MaximumTotalTagBytes = 11)
        };

        foreach ((string name, Action<IccResourceLimits> configure) in cases)
        {
            IccReadOptions options = new IccReadOptions();
            configure(options.ResourceLimits);
            AssertEveryReadThrows<IccProfileFormatException>(bytes, options,
                name);
            IccOperationResult[] probes = ProbeEveryInput(bytes, options);
            foreach (IccOperationResult probe in probes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.ResourceLimitExceeded), name);
            }
        }
    }

    [Test]
    public void EveryReaderLimitIsValidatedBeforeAStreamIsConsumed()
    {
        byte[] bytes = CreateMinimalProfile(0x04400000);
        (string Name, Action<IccResourceLimits> Configure)[] cases =
        {
            ("profile", limits => limits.MaximumProfileBytes = 131),
            ("tag count", limits => limits.MaximumTagCount = -1),
            ("tag bytes", limits => limits.MaximumTagBytes = -1),
            ("total tag bytes",
                limits => limits.MaximumTotalTagBytes = -1),
            ("nesting", limits => limits.MaximumNestingDepth = -1),
            ("decompression",
                limits => limits.MaximumDecompressedBytes = -1),
            ("instructions",
                limits => limits.MaximumCalculatorInstructions = -1),
            ("stack", limits => limits.MaximumCalculatorStackDepth = -1),
            ("storage",
                limits => limits.MaximumCalculatorStorageValues = -1),
            ("diagnostics", limits => limits.MaximumDiagnosticCount = -1)
        };
        IIccProfileReader reader = new IccProfileReader();

        foreach ((string name, Action<IccResourceLimits> configure) in cases)
        {
            IccReadOptions options = new IccReadOptions();
            configure(options.ResourceLimits);
            using MemoryStream readStream = new MemoryStream(bytes);
            using MemoryStream probeStream = new MemoryStream(bytes);

            Assert.Throws<IccProfileValidationException>(() =>
                reader.ReadProfileBytes(bytes, options), name + " bytes");
            Assert.Throws<IccProfileValidationException>(() =>
                reader.ReadProfileMemory(bytes, options), name + " memory");
            Assert.Throws<IccProfileValidationException>(() =>
                reader.ReadProfileStream(readStream, options), name + " read");
            Assert.Throws<IccProfileValidationException>(() =>
                reader.ProbeProfileBytes(bytes, options), name + " probe bytes");
            Assert.Throws<IccProfileValidationException>(() =>
                reader.ProbeProfileMemory(bytes, options), name + " probe memory");
            Assert.Throws<IccProfileValidationException>(() =>
                reader.ProbeProfileStream(probeStream, options),
                name + " probe stream");
            Assert.That(readStream.Position, Is.Zero,
                name + " read stream position");
            Assert.That(probeStream.Position, Is.Zero,
                name + " probe stream position");
        }
    }

    [Test]
    public void DecodeLimitsArePropagatedAndCarrierLimitIsNotAppliedToProfiles()
    {
        byte[] bytes = CreateTaggedProfile(0x04400000);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumProfileBytes = 1000;
        options.ResourceLimits.MaximumCarrierBytes = 0;
        options.ResourceLimits.MaximumTagCount = 5;
        options.ResourceLimits.MaximumTagBytes = 50;
        options.ResourceLimits.MaximumTotalTagBytes = 60;
        options.ResourceLimits.MaximumNestingDepth = 7;
        options.ResourceLimits.MaximumDecompressedBytes = 1234;
        options.ResourceLimits.MaximumCalculatorInstructions = 55;
        options.ResourceLimits.MaximumCalculatorStackDepth = 44;
        options.ResourceLimits.MaximumCalculatorStorageValues = 33;
        options.ResourceLimits.MaximumDiagnosticCount = 6;
        IIccProfileReader reader = new IccProfileReader();

        IccProfile profile = reader.ReadProfileBytes(bytes, options);
        TagDecodeState state = profile.Tags[0].DecodeState!;
        IccOperationResult[] probes = ProbeEveryInput(bytes, options);

        Assert.Multiple(() =>
        {
            Assert.That(state.MaximumProfileBytes, Is.EqualTo(1000));
            Assert.That(state.MaximumTagCount, Is.EqualTo(5));
            Assert.That(state.MaximumTagBytes, Is.EqualTo(50));
            Assert.That(state.MaximumTotalTagBytes, Is.EqualTo(60));
            Assert.That(state.MaximumNestingDepth, Is.EqualTo(7));
            Assert.That(state.MaximumDecompressedBytes, Is.EqualTo(1234));
            Assert.That(state.MaximumCalculatorInstructions, Is.EqualTo(55));
            Assert.That(state.MaximumCalculatorStackDepth, Is.EqualTo(44));
            Assert.That(state.MaximumCalculatorStorageValues, Is.EqualTo(33));
            Assert.That(state.MaximumDiagnosticCount, Is.EqualTo(6));
            foreach (IccOperationResult probe in probes)
            {
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.Success));
            }
        });
    }

    [Test]
    public void StreamOperationsReportNullAndUnreadableInputsConsistently()
    {
        IIccProfileReader reader = new IccProfileReader();
        IccReadOptions options = new IccReadOptions();
        MemoryStream unreadable = new MemoryStream();
        unreadable.Dispose();

        IccProfileValidationException readNull =
            Assert.Throws<IccProfileValidationException>(() =>
                reader.ReadProfileStream(null!, options))!;
        IccProfileFormatException readUnreadable =
            Assert.Throws<IccProfileFormatException>(() =>
                reader.ReadProfileStream(unreadable, options))!;
        IccOperationResult probeNull = reader.ProbeProfileStream(null!, options);
        IccOperationResult probeUnreadable = reader.ProbeProfileStream(
            unreadable, options);

        Assert.Multiple(() =>
        {
            Assert.That(readNull.Message, Does.Contain("cannot be null"));
            Assert.That(readUnreadable.Message, Does.Contain("not readable"));
            Assert.That(probeNull.Status, Is.EqualTo(IccOperationStatus.Failed));
            Assert.That(probeNull.Message, Does.Contain("cannot be null"));
            Assert.That(probeUnreadable.Status,
                Is.EqualTo(IccOperationStatus.Failed));
            Assert.That(probeUnreadable.Message, Does.Contain("not readable"));
        });
    }

    private static byte[] CreateMinimalProfile(uint version)
    {
        return IccBinaryFixture.CreateProfile(version,
            0x6d6e7472, 0x52474220, 0x58595a20, 0);
    }

    private static byte[] CreateTaggedProfile(uint version)
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(0x74657874, 12);
        payload[8] = (byte)'I';
        payload[9] = (byte)'C';
        payload[10] = (byte)'C';
        return IccBinaryFixture.CreateProfile(version,
            0x6d6e7472, 0x52474220, 0x58595a20, 0,
            new IccBinaryTagFixture(0x63707274, payload));
    }

    private static IccProfile[] ReadEveryInput(byte[] bytes,
        IccReadOptions options)
    {
        IIccProfileReader reader = new IccProfileReader();
        using MemoryStream seekable = new MemoryStream(bytes);
        using ChunkedInputStream nonSeekable = new ChunkedInputStream(bytes, 3);
        return new IccProfile[]
        {
            reader.ReadProfileBytes(bytes, options),
            reader.ReadProfileMemory(bytes, options),
            reader.ReadProfileStream(seekable, options),
            reader.ReadProfileStream(nonSeekable, options)
        };
    }

    private static IccOperationResult[] ProbeEveryInput(byte[] bytes,
        IccReadOptions options)
    {
        IIccProfileReader reader = new IccProfileReader();
        using MemoryStream seekable = new MemoryStream(bytes);
        using ChunkedInputStream nonSeekable = new ChunkedInputStream(bytes, 3);
        return new IccOperationResult[]
        {
            reader.ProbeProfileBytes(bytes, options),
            reader.ProbeProfileMemory(bytes, options),
            reader.ProbeProfileStream(seekable, options),
            reader.ProbeProfileStream(nonSeekable, options)
        };
    }

    private static void AssertEveryReadThrows<T>(byte[] bytes,
        IccReadOptions options, string message = "") where T : Exception
    {
        IIccProfileReader reader = new IccProfileReader();
        using MemoryStream seekable = new MemoryStream(bytes);
        using ChunkedInputStream nonSeekable = new ChunkedInputStream(bytes, 3);
        Assert.Throws<T>(() => reader.ReadProfileBytes(bytes, options),
            message + " bytes");
        Assert.Throws<T>(() => reader.ReadProfileMemory(bytes, options),
            message + " memory");
        Assert.Throws<T>(() => reader.ReadProfileStream(seekable, options),
            message + " seekable stream");
        Assert.Throws<T>(() => reader.ReadProfileStream(nonSeekable, options),
            message + " non-seekable stream");
    }

    private sealed class ChunkedInputStream : Stream
    {
        private readonly MemoryStream inner;
        private readonly int maximumRead;

        internal ChunkedInputStream(byte[] bytes, int maximumRead)
        {
            inner = new MemoryStream(bytes);
            this.maximumRead = maximumRead;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return inner.Read(buffer, offset, Math.Min(count, maximumRead));
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
