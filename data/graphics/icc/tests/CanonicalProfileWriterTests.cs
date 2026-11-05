using icc;

namespace icc.Tests;

public class CanonicalProfileWriterTests
{
    [Test]
    public void CanonicalLayoutSortsUnsignedSignaturesAndStabilizesPayloadOrder()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        IccTag high = CreateRawTag(uint.MaxValue, CreateSignaturePayload(3));
        IccTag low = CreateRawTag(1, CreateSignaturePayload(1));
        IccTag signBit = CreateRawTag(0x80000000, CreateSignaturePayload(2));
        IccProfile firstProfile = CreateProfile(new IccTag[] { high, low, signBit });
        IccProfile secondProfile = CreateProfile(new IccTag[] { signBit, high, low });
        IccWriteOptions options = new IccWriteOptions();
        options.InternIdenticalTagPayloads = false;

        byte[] first = writer.WriteProfileBytes(firstProfile, options);
        byte[] second = writer.WriteProfileBytes(secondProfile, options);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Has.Length.EqualTo(204));
            Assert.That(ReadUInt32(first, 132), Is.EqualTo(1));
            Assert.That(ReadUInt32(first, 136), Is.EqualTo(168));
            Assert.That(ReadUInt32(first, 144), Is.EqualTo(0x80000000));
            Assert.That(ReadUInt32(first, 148), Is.EqualTo(180));
            Assert.That(ReadUInt32(first, 156), Is.EqualTo(uint.MaxValue));
            Assert.That(ReadUInt32(first, 160), Is.EqualTo(192));
            Assert.That(ReadUInt32(first, 176), Is.EqualTo(1));
            Assert.That(ReadUInt32(first, 188), Is.EqualTo(2));
            Assert.That(ReadUInt32(first, 200), Is.EqualTo(3));
        });
    }

    [Test]
    public void CanonicalLayoutOptionallyInternsOnlyByteIdenticalPayloads()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        byte[] sharedPayload = CreateTextPayload((byte)'x');
        IccProfile profile = CreateProfile(new IccTag[]
        {
            CreateRawTag(1, sharedPayload),
            CreateRawTag(2, CreateSignaturePayload(9)),
            CreateRawTag(3, (byte[])sharedPayload.Clone())
        });
        profile.Tags[0].PaddingBytes = new byte[] { 7, 8, 9 };
        IccWriteOptions intern = new IccWriteOptions();
        IccWriteOptions separate = new IccWriteOptions();
        separate.InternIdenticalTagPayloads = false;

        int internedSize = writer.MeasureProfileBytes(profile, intern);
        byte[] interned = writer.WriteProfileBytes(profile, intern);
        byte[] notInterned = writer.WriteProfileBytes(profile, separate);
        IccProfile decoded = ProfileCodec.ReadEnvelope(interned, 0, 0, new IccReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(internedSize, Is.EqualTo(192));
            Assert.That(interned, Has.Length.EqualTo(192));
            Assert.That(notInterned, Has.Length.EqualTo(204));
            Assert.That(ReadUInt32(interned, 136), Is.EqualTo(168));
            Assert.That(ReadUInt32(interned, 148), Is.EqualTo(180));
            Assert.That(ReadUInt32(interned, 160), Is.EqualTo(168));
            Assert.That(ReadUInt32(notInterned, 160), Is.EqualTo(192));
            Assert.That(interned[177], Is.Zero);
            Assert.That(interned[178], Is.Zero);
            Assert.That(interned[179], Is.Zero);
            Assert.That(decoded.ValidationIssues, Is.Empty);
            Assert.That(decoded.TagTableEntries[0].SharedDataId, Is.Zero);
            Assert.That(decoded.TagTableEntries[1].SharedDataId, Is.EqualTo(-1));
            Assert.That(decoded.TagTableEntries[2].SharedDataId, Is.Zero);
            Assert.That(writer.WriteProfileBytes(profile, intern), Is.EqualTo(interned));
        });
    }

    [Test]
    public void CanonicalInterningCountsOnlyStoredPayloadsAgainstTotalLimit()
    {
        IIccProfileWriter writer = new IccProfileWriter();
        byte[] payload = CreateSignaturePayload(4);
        IccProfile profile = CreateProfile(new IccTag[]
        {
            CreateRawTag(1, payload),
            CreateRawTag(2, (byte[])payload.Clone())
        });
        IccWriteOptions intern = new IccWriteOptions();
        intern.ResourceLimits.MaximumTotalTagBytes = 12;
        IccWriteOptions separate = new IccWriteOptions();
        separate.InternIdenticalTagPayloads = false;
        separate.ResourceLimits.MaximumTotalTagBytes = 12;

        Assert.Multiple(() =>
        {
            Assert.That(writer.MeasureProfileBytes(profile, intern), Is.EqualTo(168));
            Assert.Throws<IccProfileWriteException>(() => writer.MeasureProfileBytes(profile, separate));
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
        return profile;
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

    private static byte[] CreateTextPayload(byte value)
    {
        byte[] payload = new byte[10];
        WriteUInt32(payload, 0, IccTypeSignatures.Text);
        payload[8] = value;
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
}
