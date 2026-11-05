using icc;

namespace icc.Tests;

public class ProfileReaderTests
{
    [Test]
    public void SpanInputIsOwnedWhileMemoryInputCanRemainBorrowedForLazyDecoding()
    {
        byte[] spanSource = CreateTaggedProfile();
        byte[] memorySource = CreateTaggedProfile();
        IccReadOptions options = CreateBorrowedOptions();
        IIccProfileReader reader = new IccProfileReader();

        IccProfile spanProfile = reader.ReadProfileBytes(spanSource.AsSpan(), options);
        IccProfile memoryProfile = reader.ReadProfileMemory(memorySource.AsMemory(), options);
        IndependentBigEndian.WriteUInt32(spanSource, 144,
            IccTypeSignatures.Data);
        IndependentBigEndian.WriteUInt32(memorySource, 144,
            IccTypeSignatures.Data);
        IccTagData spanData = TagDecoder.GetDecodedData(spanProfile.Tags[0]);
        IccTagData memoryData = TagDecoder.GetDecodedData(memoryProfile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(spanData.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Text));
            Assert.That(memoryData.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Data));
            Assert.That(spanProfile.Location.Offset, Is.Zero);
            Assert.That(memoryProfile.Location.Offset, Is.Zero);
        });
    }

    [Test]
    public void IndependentGoldenFixtureDecodesHeaderAndTextTag()
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(0x74657874, 12);
        payload[8] = (byte)'R';
        payload[9] = (byte)'G';
        payload[10] = (byte)'B';
        byte[] bytes = IccBinaryFixture.CreateProfile(
            0x04300000, 0x6d6e7472, 0x52474220, 0x58595a20, 2,
            new IccBinaryTagFixture(0x63707274, payload));
        IIccProfileReader reader = new IccProfileReader();

        IccProfile profile = reader.ReadProfileBytes(bytes,
            new IccReadOptions());
        IccTagData data = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.DeclaredSize, Is.EqualTo(156));
            Assert.That(profile.Header.Version.PackedValue,
                Is.EqualTo(0x04300000));
            Assert.That(profile.Header.ProfileClass,
                Is.EqualTo(IccProfileClass.Display));
            Assert.That(profile.Header.DataColorSpace,
                Is.EqualTo(IccColorSpaceKind.Rgb));
            Assert.That(profile.Header.ProfileConnectionSpace,
                Is.EqualTo(IccPcsKind.Xyz));
            Assert.That(profile.Header.RenderingIntentKind,
                Is.EqualTo(IccRenderingIntent.Saturation));
            Assert.That(profile.Tags[0].Signature,
                Is.EqualTo(0x63707274));
            Assert.That(data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(data.Text!.Text, Is.EqualTo("RGB"));
        });
    }

    [Test]
    public void SeekableStreamReadsFromItsCurrentPositionAndReportsAbsoluteLocations()
    {
        byte[] profileBytes = CreateMinimalProfile();
        byte[] source = new byte[7 + profileBytes.Length];
        System.Array.Copy(profileBytes, 0, source, 7, profileBytes.Length);
        using MemoryStream stream = new MemoryStream(source);
        stream.Position = 7;
        IIccProfileReader reader = new IccProfileReader();

        IccProfile profile = reader.ReadProfileStream(stream, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.DeclaredSize, Is.EqualTo(132));
            Assert.That(profile.Location.Offset, Is.EqualTo(7));
            Assert.That(profile.ReadResult.Location.Offset, Is.EqualTo(7));
            Assert.That(stream.Position, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void NonSeekableStreamSupportsPartialReadsWithoutUsingLengthOrPosition()
    {
        byte[] profileBytes = CreateMinimalProfile();
        using ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(profileBytes, 3);
        IIccProfileReader reader = new IccProfileReader();

        IccProfile profile = reader.ReadProfileStream(stream, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.DeclaredSize, Is.EqualTo(132));
            Assert.That(profile.Location.Offset, Is.Zero);
            Assert.That(stream.BytesRead, Is.EqualTo(132));
        });
    }

    [Test]
    public void NonExactStreamReadStopsAtTheDeclaredProfileBoundary()
    {
        byte[] profileBytes = CreateMinimalProfile();
        byte[] source = new byte[profileBytes.Length + 2];
        System.Array.Copy(profileBytes, source, profileBytes.Length);
        source[132] = 11;
        source[133] = 12;
        using MemoryStream stream = new MemoryStream(source);
        IccReadOptions options = new IccReadOptions();
        options.RequireExactProfileSize = false;
        IIccProfileReader reader = new IccProfileReader();

        IccProfile profile = reader.ReadProfileStream(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.ReadResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(profile.TrailingBytes, Is.Empty);
            Assert.That(stream.Position, Is.EqualTo(132));
            Assert.That(stream.ReadByte(), Is.EqualTo(11));
        });
    }

    [Test]
    public void ExactStreamReadRejectsTrailingBytes()
    {
        byte[] profileBytes = CreateMinimalProfile();
        byte[] source = new byte[profileBytes.Length + 1];
        System.Array.Copy(profileBytes, source, profileBytes.Length);
        using MemoryStream stream = new MemoryStream(source);
        IIccProfileReader reader = new IccProfileReader();

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => reader.ReadProfileStream(stream, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("1 trailing byte").And.Contain("exact profile size"));
    }

    [Test]
    public void ByteAndMemoryProbesReportExpectedInvalidInputsWithoutThrowing()
    {
        byte[] valid = CreateMinimalProfile();
        byte[] badSignature = CreateMinimalProfile();
        IndependentBigEndian.WriteUInt32(badSignature, 36, 0x42414421);
        byte[] unsupportedVersion = CreateMinimalProfile();
        IndependentBigEndian.WriteUInt32(unsupportedVersion, 8, 0x06000000);
        byte[] excessiveSize = CreateMinimalProfile();
        IndependentBigEndian.WriteUInt32(excessiveSize, 0, 1000);
        IccReadOptions limited = new IccReadOptions();
        limited.ResourceLimits.MaximumProfileBytes = 200;
        IIccProfileReader reader = new IccProfileReader();

        IccOperationResult success = reader.ProbeProfileBytes(valid, new IccReadOptions());
        IccOperationResult truncated = reader.ProbeProfileMemory(valid.AsMemory(0, 100), new IccReadOptions());
        IccOperationResult signature = reader.ProbeProfileBytes(badSignature, new IccReadOptions());
        IccOperationResult version = reader.ProbeProfileBytes(unsupportedVersion, new IccReadOptions());
        IccOperationResult size = reader.ProbeProfileBytes(excessiveSize, limited);

        Assert.Multiple(() =>
        {
            Assert.That(success.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(success.Location.Length, Is.EqualTo(132));
            Assert.That(truncated.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(signature.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(signature.Location.Offset, Is.EqualTo(36));
            Assert.That(version.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(version.Location.Offset, Is.EqualTo(8));
            Assert.That(size.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
        });
    }

    [Test]
    public void ProbeRejectsATagTableThatCannotFitInsideTheDeclaredProfile()
    {
        byte[] profileBytes = CreateMinimalProfile();
        IndependentBigEndian.WriteUInt32(profileBytes, 128, 1);
        IIccProfileReader reader = new IccProfileReader();

        IccOperationResult result = reader.ProbeProfileBytes(profileBytes, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(result.Message, Does.Contain("tag table"));
            Assert.That(result.Location.Offset, Is.EqualTo(128));
        });
    }

    [Test]
    public void StreamProbeUsesABoundedPrefixAndRestoresASeekablePosition()
    {
        byte[] profileBytes = CreateMinimalProfile();
        byte[] source = new byte[5 + profileBytes.Length + 20];
        System.Array.Copy(profileBytes, 0, source, 5, profileBytes.Length);
        using MemoryStream stream = new MemoryStream(source);
        stream.Position = 5;
        IccReadOptions options = new IccReadOptions();
        options.RequireExactProfileSize = false;
        IIccProfileReader reader = new IccProfileReader();

        IccOperationResult result = reader.ProbeProfileStream(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(result.Location.Offset, Is.EqualTo(5));
            Assert.That(result.Location.Length, Is.EqualTo(132));
            Assert.That(stream.Position, Is.EqualTo(5));
        });
    }

    [Test]
    public void NonSeekableStreamProbeHandlesPartialReadsAndInvalidData()
    {
        byte[] profileBytes = CreateMinimalProfile();
        IndependentBigEndian.WriteUInt32(profileBytes, 36, 0x42414421);
        using ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(profileBytes, 2);
        IIccProfileReader reader = new IccProfileReader();

        IccOperationResult result = reader.ProbeProfileStream(stream, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(result.Location.Offset, Is.EqualTo(36));
            Assert.That(stream.BytesRead, Is.EqualTo(132));
        });
    }

    [Test]
    public void StreamProbeReadsAndValidatesTheDeclaredTagTablePrefix()
    {
        byte[] profileBytes = CreateTaggedProfile();
        IndependentBigEndian.WriteUInt32(profileBytes, 136, 1000);
        using ChunkedNonSeekableStream stream = new ChunkedNonSeekableStream(profileBytes, 3);
        IIccProfileReader reader = new IccProfileReader();

        IccOperationResult result = reader.ProbeProfileStream(stream, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(result.Message, Does.Contain("payload offset"));
            Assert.That(result.Location.Offset, Is.EqualTo(136));
            Assert.That(stream.BytesRead, Is.EqualTo(144));
        });
    }

    private static IccReadOptions CreateBorrowedOptions()
    {
        IccReadOptions options = new IccReadOptions();
        options.CopyInputBytes = false;
        options.PreserveOriginalBytes = false;
        return options;
    }

    private static byte[] CreateMinimalProfile()
    {
        return IccBinaryFixture.CreateProfile(
            0x04400000, 0x6d6e7472, 0x52474220, 0x58595a20, 0);
    }

    private static byte[] CreateTaggedProfile()
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(0x74657874, 16);
        return IccBinaryFixture.CreateProfile(
            0x04400000, 0x6d6e7472, 0x52474220, 0x58595a20, 0,
            new IccBinaryTagFixture(0x63707274, payload));
    }

    private class ChunkedNonSeekableStream : Stream
    {
        private readonly MemoryStream inner;
        private readonly int maximumRead;

        public int BytesRead;

        public ChunkedNonSeekableStream(byte[] bytes, int maximumRead)
        {
            inner = new MemoryStream(bytes);
            this.maximumRead = maximumRead;
            BytesRead = 0;
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
            int read = inner.Read(buffer, offset, Math.Min(count, maximumRead));
            BytesRead += read;
            return read;
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
