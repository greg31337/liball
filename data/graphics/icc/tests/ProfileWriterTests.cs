using icc;

namespace icc.Tests;

public class ProfileWriterTests
{
    [Test]
    public void MeasureBytesBufferAndStreamUseOneStableExactLayout()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccTag text = CreateTextTag(0x7a303031, "A");
        IccTag signature = CreateRawTag(0x61303032, CreateSignaturePayload(0x74657374));
        IccProfile profile = CreateProfile(new IccTag[] { text, signature });
        IccWriteOptions options = new IccWriteOptions();
        options.Mode = IccWriteMode.Preservation;

        int measured = writer.MeasureProfileBytes(profile, options);
        byte[] first = writer.WriteProfileBytes(profile, options);
        byte[] second = writer.WriteProfileBytes(profile, options);
        byte[] buffer = new byte[measured + 3];
        System.Array.Fill(buffer, (byte)0xcc);
        IccOperationResult bufferResult = writer.TryWriteProfileBuffer(profile, buffer, options);
        byte[] small = new byte[measured - 1];
        System.Array.Fill(small, (byte)0xaa);
        IccOperationResult smallResult = writer.TryWriteProfileBuffer(profile, small, options);
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        stream.WriteByte(0x7e);
        writer.WriteProfileStream(profile, stream, options);
        IccProfile lazyRoundTrip = ProfileCodec.ReadEnvelope(first, 0, 0, new IccReadOptions());
        byte[] lazyOutput = writer.WriteProfileBytes(lazyRoundTrip, options);
        IccReadOptions eagerOptions = new IccReadOptions();
        eagerOptions.DecodeTagsLazily = false;
        IccProfile eagerRoundTrip = ProfileCodec.ReadEnvelope(first, 0, 0, eagerOptions);

        Assert.Multiple(() =>
        {
            Assert.That(measured, Is.EqualTo(180));
            Assert.That(first, Has.Length.EqualTo(measured));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(ReadUInt32(first, 0), Is.EqualTo(180));
            Assert.That(ReadUInt32(first, 128), Is.EqualTo(2));
            Assert.That(ReadUInt32(first, 132), Is.EqualTo(text.Signature));
            Assert.That(ReadUInt32(first, 136), Is.EqualTo(156));
            Assert.That(ReadUInt32(first, 140), Is.EqualTo(10));
            Assert.That(ReadUInt32(first, 144), Is.EqualTo(signature.Signature));
            Assert.That(ReadUInt32(first, 148), Is.EqualTo(168));
            Assert.That(ReadUInt32(first, 152), Is.EqualTo(12));
            Assert.That(first[166], Is.Zero);
            Assert.That(first[167], Is.Zero);
            Assert.That(bufferResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(bufferResult.Location.Length, Is.EqualTo(measured));
            Assert.That(buffer.Take(measured), Is.EqualTo(first));
            Assert.That(buffer.Skip(measured), Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc }));
            Assert.That(smallResult.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(smallResult.Location.Length, Is.EqualTo(measured));
            Assert.That(small, Is.All.EqualTo(0xaa));
            Assert.That(stream.Position, Is.EqualTo(measured + 1));
            Assert.That(stream.ToArray()[0], Is.EqualTo(0x7e));
            Assert.That(stream.ToArray().Skip(1), Is.EqualTo(first));
            Assert.That(lazyOutput, Is.EqualTo(first));
            Assert.That(eagerRoundTrip.Tags, Has.Length.EqualTo(2));
            Assert.That(eagerRoundTrip.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(eagerRoundTrip.Tags[0].Data.Text!.Text, Is.EqualTo("A"));
        });
    }

    [Test]
    public void StreamIsUntouchedWhenPayloadPreEncodingFails()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccTag valid = CreateRawTag(0x74303031, CreateSignaturePayload(1));
        IccTag invalid = new IccTag();
        invalid.Signature = 0x74303032;
        invalid.TableEntry.Signature = invalid.Signature;
        IccProfile profile = CreateProfile(new IccTag[] { valid, invalid });
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        stream.Write(new byte[] { 3, 4, 5 });

        IccProfileWriteException exception = Assert.Throws<IccProfileWriteException>(() =>
            writer.WriteProfileStream(profile, stream, new IccWriteOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("profile tag 1").And.Contain("0x74303032").And.Contain("no writable codec"));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 3, 4, 5 }));
            Assert.That(stream.Position, Is.EqualTo(3));
        });
    }

    [Test]
    public void PreflightEnforcesTagTotalProfileAndCountLimits()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccProfile profile = CreateProfile(new IccTag[]
        {
            CreateTextTag(0x74303031, "A"),
            CreateRawTag(0x74303032, CreateSignaturePayload(2))
        });
        IccWriteOptions tagLimit = new IccWriteOptions();
        tagLimit.ResourceLimits.MaximumTagBytes = 9;
        IccWriteOptions totalLimit = new IccWriteOptions();
        totalLimit.ResourceLimits.MaximumTotalTagBytes = 21;
        IccWriteOptions profileLimit = new IccWriteOptions();
        profileLimit.ResourceLimits.MaximumProfileBytes = 179;
        IccWriteOptions countLimit = new IccWriteOptions();
        countLimit.ResourceLimits.MaximumTagCount = 1;

        IccProfileWriteException tagException = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, tagLimit))!;
        IccProfileWriteException totalException = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, totalLimit))!;
        IccProfileWriteException profileException = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, profileLimit))!;
        IccProfileWriteException countException = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, countLimit))!;

        Assert.Multiple(() =>
        {
            Assert.That(tagException.Message, Does.Contain("tag 0").And.Contain("maximum"));
            Assert.That(totalException.Message, Does.Contain("total 22 bytes").And.Contain("maximum"));
            Assert.That(profileException.Message, Does.Contain("final profile size").And.Contain("limit"));
            Assert.That(countException.Message, Does.Contain("2 tags").And.Contain("maximum"));
        });
    }

    [Test]
    public void PreflightRejectsInvalidInputsAndCanHonorExplicitInvalidProfilePolicy()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccProfile profile = CreateProfile(new IccTag[0]);
        IccValidationIssue issue = new IccValidationIssue();
        issue.Severity = IccValidationSeverity.Error;
        profile.ValidationIssues = new IccValidationIssue[] { issue };
        IccWriteOptions tolerant = new IccWriteOptions();
        tolerant.RejectInvalidProfile = false;
        IccProfile missingHeader = CreateProfile(new IccTag[0]);
        missingHeader.Header.RawBytes = new byte[0];
        IccWriteOptions badMode = new IccWriteOptions();
        badMode.Mode = (IccWriteMode)20;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, new IccWriteOptions()));
            Assert.That(writer.MeasureProfileBytes(profile, tolerant), Is.EqualTo(132));
            Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(missingHeader, new IccWriteOptions()));
            Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, badMode));
            Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileStream(profile, new System.IO.MemoryStream(new byte[4], false), tolerant));
            Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileStream(
                profile, null!, tolerant));
            Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileBytes(null!, new IccWriteOptions()));
            Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileBytes(profile, null!));
        });
    }

    [Test]
    public void PreflightReportsSourceTagIdentityAfterCanonicalSorting()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccTag high = CreateRawTag(0x7a7a7a7a, CreateSignaturePayload(1));
        IccTag low = CreateRawTag(0x61616161, CreateSignaturePayload(2));
        IccProfile profile = CreateProfile(new IccTag[] { high, low });
        IccWriteOptions options = new IccWriteOptions();
        options.ResourceLimits.MaximumTotalTagBytes = 12;

        IccProfileWriteException exception = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, options))!;

        Assert.That(exception.Message, Does.Contain("total 24 bytes").And.Contain("profile tag 0").And.Contain("0x7A7A7A7A"));
    }

    [Test]
    public void PreflightIdentifiesAnOversizedHierarchicalElement()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccTagData nested = new IccTagData();
        nested.Kind = IccTagDataKind.Raw;
        nested.RawData.Bytes = new byte[40];
        WriteUInt32(nested.RawData.Bytes, 0, 0x70726976);
        nested.RawData.HasTypeSignature = true;
        nested.RawData.TypeSignature = 0x70726976;
        IccTag tag = new IccTag();
        tag.Signature = 0x61727279;
        tag.TableEntry.Signature = tag.Signature;
        tag.Data.Kind = IccTagDataKind.TagArray;
        tag.Data.TagArray = new IccTagArray();
        tag.Data.TagArray.ArraySignature = 0x64656d6f;
        tag.Data.TagArray.Values = new IccTagData[] { nested };
        IccProfile profile = CreateProfile(new IccTag[] { tag });
        IccWriteOptions options = new IccWriteOptions();
        options.ResourceLimits.MaximumTagBytes = 32;

        IccProfileWriteException exception = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, options))!;

        Assert.That(exception.Message, Does.Contain("profile tag 0").And.Contain("0x61727279")
            .And.Contain("tag-array element 0").And.Contain("40 bytes").And.Contain("maximum of 32"));
    }

    [Test]
    public void PreflightIdentifiesTagsAndProcessingElementsWithoutWritableCodecs()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccTag unsupportedTag = new IccTag();
        unsupportedTag.Signature = 0x63616c63;
        unsupportedTag.TableEntry.Signature = unsupportedTag.Signature;
        unsupportedTag.Data.Kind = IccTagDataKind.Calculator;
        IccProfile tagProfile = CreateProfile(new IccTag[] { unsupportedTag });
        IccProcessingElement element = new IccProcessingElement();
        element.Kind = IccProcessingElementKind.Calculator;
        element.Signature = IccProcessingElementSignatures.Calculator;
        element.InputChannels = 1;
        element.OutputChannels = 1;
        IccMultiProcessElements pipeline = new IccMultiProcessElements();
        pipeline.InputChannels = 1;
        pipeline.OutputChannels = 1;
        pipeline.Elements = new IccProcessingElement[] { element };
        IccTag pipelineTag = new IccTag();
        pipelineTag.Signature = 0x6d706574;
        pipelineTag.TableEntry.Signature = pipelineTag.Signature;
        pipelineTag.Data.Kind = IccTagDataKind.MultiProcessElements;
        pipelineTag.Data.MultiProcessElements = pipeline;
        IccProfile elementProfile = CreateProfile(new IccTag[] { pipelineTag });

        IccProfileWriteException tagException = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(tagProfile, new IccWriteOptions()))!;
        IccProfileWriteException elementException = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(elementProfile, new IccWriteOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(tagException.Message, Does.Contain("profile tag 0").And.Contain("0x63616C63").And.Contain("data kind Calculator").And.Contain("no writable codec"));
            Assert.That(elementException.Message, Does.Contain("profile tag 0").And.Contain("0x6D706574")
                .And.Contain("processing element 0").And.Contain("kind Calculator").And.Contain("no writable codec for ICC version 4"));
        });
    }

    [Test]
    public void UnknownDataPolicyAlsoRejectsUndecodedLazyPayloadsAcrossEveryWriterOperation()
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(0x70726976, 10);
        payload[8] = 0xaa;
        payload[9] = 0x55;
        byte[] source = IccBinaryFixture.CreateProfile(
            0x04400000,
            IccProfileClassSignatures.Display,
            IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz,
            0,
            new IccBinaryTagFixture(0x70726976, payload));
        IccProfile profile = new IccProfileReader().ReadProfileMemory(source, new IccReadOptions());
        IccWriteOptions options = new IccWriteOptions();
        options.PreserveUnknownData = false;
        IIccProfileWriter writer = new IccProfileWriter();
        byte[] destination = Enumerable.Repeat((byte)0xcc, 200).ToArray();
        MemoryStream stream = new MemoryStream();
        stream.WriteByte(0x7e);

        IccProfileWriteException measure = Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, options))!;
        IccProfileWriteException bytes = Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileBytes(profile, options))!;
        IccProfileWriteException buffer = Assert.Throws<IccProfileWriteException>(() => writer.TryWriteProfileBuffer(profile, destination, options))!;
        IccProfileWriteException streamed = Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileStream(profile, stream, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Unknown));
            Assert.That(measure.Message, Is.EqualTo("Unknown tag 0x70726976 cannot be written when unknown-data preservation is disabled."));
            Assert.That(bytes.Message, Is.EqualTo(measure.Message));
            Assert.That(buffer.Message, Is.EqualTo(measure.Message));
            Assert.That(streamed.Message, Is.EqualTo(measure.Message));
            Assert.That(destination, Is.All.EqualTo(0xcc));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x7e }));
        });
    }

    [Test]
    public void LazyUnknownPayloadIsRetainedWhenPolicyAllowsIt()
    {
        byte[] payload = IccBinaryFixture.CreateTagPayload(0x70726976, 11);
        payload[8] = 1;
        payload[9] = 2;
        payload[10] = 3;
        byte[] source = IccBinaryFixture.CreateProfile(
            0x04400000,
            IccProfileClassSignatures.Display,
            IccColorSpaceSignatures.Rgb,
            IccColorSpaceSignatures.Xyz,
            0,
            new IccBinaryTagFixture(0x74657374, payload));
        IccProfile profile = new IccProfileReader().ReadProfileMemory(source, new IccReadOptions());
        IccWriteOptions options = new IccWriteOptions();
        options.ComputeProfileId = false;
        IIccProfileWriter writer = new IccProfileWriter();

        byte[] written = writer.WriteProfileBytes(profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(writer.MeasureProfileBytes(profile, options), Is.EqualTo(156));
            Assert.That(ReadUInt32(written, 136), Is.EqualTo(144));
            Assert.That(ReadUInt32(written, 140), Is.EqualTo(11));
            Assert.That(written.Skip(144).Take(11), Is.EqualTo(payload));
            Assert.That(written[155], Is.Zero);
        });
    }

    [Test]
    public void UnknownDataPolicyRejectsPrivateNestedPayloads()
    {
        IccTagData privateData = new IccTagData();
        privateData.Kind = IccTagDataKind.Raw;
        privateData.RawData.HasTypeSignature = true;
        privateData.RawData.TypeSignature = 0x70726976;
        privateData.RawData.Bytes = IccBinaryFixture.CreateTagPayload(0x70726976, 9);
        privateData.RawData.Bytes[8] = 0x5a;
        IccTag tag = new IccTag();
        tag.Signature = 0x61727279;
        tag.TableEntry.Signature = tag.Signature;
        tag.Data.Kind = IccTagDataKind.TagStructure;
        tag.Data.TagStructure = new IccTagStructure
        {
            StructureSignature = 0x64656d6f,
            Members = new IccStructureMember[]
            {
                new IccStructureMember { Signature = 0x70726976, Data = privateData }
            }
        };
        IccProfile profile = CreateProfile(new IccTag[] { tag });
        IccWriteOptions options = new IccWriteOptions();
        options.PreserveUnknownData = false;

        IccProfileWriteException exception = Assert.Throws<IccProfileWriteException>(() =>
            new IccProfileWriter().WriteProfileBytes(profile, options))!;

        Assert.That(exception.Message, Does.Contain("Nested raw tag data").And.Contain("preservation is disabled"));
    }

    [Test]
    public void CanonicalParseWriteParsePreservesSemanticsAndRepeatedBytes()
    {
        IccProfile original = CreateProfile(new IccTag[]
        {
            CreateRawTag(0x7a303031, CreateSignaturePayload(0x41424344)),
            CreateTextTag(0x61303032, "writer semantic identity")
        });
        IccWriteOptions options = new IccWriteOptions();
        options.ComputeProfileId = false;
        IIccProfileWriter writer = new IccProfileWriter();
        IccReadOptions readOptions = new IccReadOptions();
        readOptions.DecodeTagsLazily = false;

        byte[] first = writer.WriteProfileBytes(original, options);
        IccProfile parsed = new IccProfileReader().ReadProfileBytes(first, readOptions);
        byte[] second = writer.WriteProfileBytes(parsed, options);
        IccProfile reparsed = new IccProfileReader().ReadProfileBytes(second, readOptions);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(reparsed.Header.Version.PackedValue, Is.EqualTo(original.Header.Version.PackedValue));
            Assert.That(reparsed.Header.ProfileClass, Is.EqualTo(original.Header.ProfileClass));
            Assert.That(reparsed.Header.DataColorSpace, Is.EqualTo(original.Header.DataColorSpace));
            Assert.That(reparsed.Header.ProfileConnectionSpace, Is.EqualTo(original.Header.ProfileConnectionSpace));
            Assert.That(reparsed.Tags.Select(tag => tag.Signature), Is.EqualTo(new uint[] { 0x61303032, 0x7a303031 }));
            Assert.That(reparsed.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Text));
            Assert.That(reparsed.Tags[0].Data.Text!.Text, Is.EqualTo("writer semantic identity"));
            Assert.That(reparsed.Tags[1].Data.Kind, Is.EqualTo(IccTagDataKind.Signature));
            Assert.That(reparsed.Tags[1].Data.Signature, Is.EqualTo(0x41424344));
        });
    }

    [Test]
    public void NonSeekableStreamReceivesTheExactPreflightedProfile()
    {
        IccProfile profile = CreateProfile(new IccTag[] { CreateTextTag(0x74657374, "non-seekable") });
        IccWriteOptions options = new IccWriteOptions();
        options.ComputeProfileId = false;
        IIccProfileWriter writer = new IccProfileWriter();
        byte[] expected = writer.WriteProfileBytes(profile, options);
        using WriteOnlyNonSeekableStream stream = new WriteOnlyNonSeekableStream();

        writer.WriteProfileStream(profile, stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(writer.MeasureProfileBytes(profile, options), Is.EqualTo(expected.Length));
            Assert.That(stream.Bytes, Is.EqualTo(expected));
            Assert.That(stream.WriteCalls, Is.EqualTo(1));
        });
    }

    private static IccProfile CreateProfile(IccTag[] tags)
    {
        byte[] bytes = new byte[132];
        WriteUInt32(bytes, 0, 132);
        WriteUInt32(bytes, 8, 0x04400000);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        IccProfile profile = ProfileCodec.ReadEnvelope(bytes, 0, 0, new IccReadOptions());
        profile.Tags = tags;
        profile.TagTableEntries = new IccTagTableEntry[tags.Length];
        int index = 0;
        while (index < tags.Length)
        {
            profile.TagTableEntries[index] = tags[index].TableEntry;
            index = index + 1;
        }

        return profile;
    }

    private static IccTag CreateTextTag(uint signature, string text)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Text;
        tag.Data.Text = new IccTextData();
        tag.Data.Text.TypeSignature = IccTypeSignatures.Text;
        tag.Data.Text.Text = text;
        return tag;
    }

    private static IccTag CreateRawTag(uint signature, byte[] payload)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Raw;
        tag.Data.RawData.Bytes = payload;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = ReadUInt32(payload, 0);
        return tag;
    }

    private static byte[] CreateSignaturePayload(uint value)
    {
        byte[] payload = new byte[12];
        WriteUInt32(payload, 0, IccTypeSignatures.Signature);
        WriteUInt32(payload, 8, value);
        return payload;
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) | ((uint)source[offset + 1] << 16) | ((uint)source[offset + 2] << 8) | source[offset + 3];
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    private sealed class WriteOnlyNonSeekableStream : Stream
    {
        private readonly MemoryStream storage = new MemoryStream();

        internal byte[] Bytes => storage.ToArray();
        internal int WriteCalls;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
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
            WriteCalls++;
            storage.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                storage.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
