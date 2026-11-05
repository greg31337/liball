using icc;

namespace icc.Tests;

public class TagCodecDispatcherTests
{
    [TestCase(IccTypeSignatures.TextDescription, IccVersionKind.Version2, true)]
    [TestCase(IccTypeSignatures.TextDescription, IccVersionKind.Version4, false)]
    [TestCase(IccTypeSignatures.MultiLocalizedUnicode, IccVersionKind.Version2, false)]
    [TestCase(IccTypeSignatures.MultiLocalizedUnicode, IccVersionKind.Version4, true)]
    [TestCase(IccTypeSignatures.Utf8, IccVersionKind.Version4, false)]
    [TestCase(IccTypeSignatures.Utf8, IccVersionKind.Version5, true)]
    [TestCase(IccTypeSignatures.Text, IccVersionKind.Version2, true)]
    [TestCase(IccTypeSignatures.Text, IccVersionKind.Version4, true)]
    [TestCase(IccTypeSignatures.Text, IccVersionKind.Version5, true)]
    [TestCase(IccTypeSignatures.Dictionary, IccVersionKind.Version2, true)]
    [TestCase(IccTypeSignatures.Dictionary, IccVersionKind.Version5, true)]
    [TestCase(0x7a7a7a7au, IccVersionKind.Version5, false)]
    public void TypeSupportIsResolvedByProfileGeneration(uint typeSignature, IccVersionKind version, bool expected)
    {
        Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(typeSignature, version), Is.EqualTo(expected));
    }

    [TestCase(0x04400000u, IccTypeSignatures.TextDescription, "ICC.1 version 4", "unknown or unsupported")]
    public void DispatchOutcomeIdentifiesTheGenerationUsedForAType(uint packedVersion, uint typeSignature, string generation, string classification)
    {
        byte[] source = CreateProfile(152, packedVersion, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 8);
        WriteUInt32(source, 144, typeSignature);
        IccReadOptions options = new IccReadOptions();
        options.PreserveOriginalBytes = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);
        IccTagData data = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(data.RawData.TypeSignature, Is.EqualTo(typeSignature));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(profile.Tags[0].DecodeResult.Message, Does.Contain(generation).And.Contain(classification));
        });
    }

    [Test]
    public void UnknownTypeFallsBackLosslesslyEvenWithoutPreservationMode()
    {
        byte[] source = CreateProfile(156, 0x04400000, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 12);
        WriteUInt32(source, 144, 0x7a7a7a7a);
        source[152] = 1;
        source[153] = 2;
        source[154] = 3;
        source[155] = 4;
        IccReadOptions options = new IccReadOptions();
        options.PreserveOriginalBytes = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);
        IccTagData data = TagDecoder.GetDecodedData(profile.Tags[0]);
        source[152] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(data.RawData.HasTypeSignature, Is.True);
            Assert.That(data.RawData.TypeSignature, Is.EqualTo(0x7a7a7a7a));
            Assert.That(data.RawData.Bytes, Is.EqualTo(new byte[] { 0x7a, 0x7a, 0x7a, 0x7a, 0, 0, 0, 0, 1, 2, 3, 4 }));
            Assert.That(data.RawData.Bytes, Is.Not.SameAs(source));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(profile.Tags[0].DecodeResult.Message, Does.Contain("retained losslessly"));
        });
    }

    [Test]
    public void PreservationModeRetainsMalformedPayloadForRoundTripRecovery()
    {
        byte[] source = CreateProfile(152, 0x04400000, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 6);
        WriteUInt32(source, 144, IccTypeSignatures.Text);
        source[148] = 0xa1;
        source[149] = 0xa2;
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);
        IccTagData data = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(data.RawData.HasTypeSignature, Is.True);
            Assert.That(data.RawData.TypeSignature, Is.EqualTo(IccTypeSignatures.Text));
            Assert.That(data.RawData.Bytes, Is.EqualTo(new byte[] { 0x74, 0x65, 0x78, 0x74, 0xa1, 0xa2 }));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(profile.Tags[0].DecodeResult.Message, Does.Contain("at least 8 bytes"));
        });
    }

    private static byte[] CreateProfile(int declaredSize, uint packedVersion, uint tagCount)
    {
        byte[] source = new byte[declaredSize];
        WriteUInt32(source, 0, (uint)declaredSize);
        WriteUInt32(source, 8, packedVersion);
        WriteUInt32(source, 12, IccProfileClassSignatures.Display);
        WriteUInt32(source, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(source, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(source, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(source, 128, tagCount);
        return source;
    }

    private static void WriteEntry(byte[] destination, int entryIndex, uint signature, uint payloadOffset, uint payloadSize)
    {
        int offset = 132 + (entryIndex * 12);
        WriteUInt32(destination, offset, signature);
        WriteUInt32(destination, offset + 4, payloadOffset);
        WriteUInt32(destination, offset + 8, payloadSize);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
