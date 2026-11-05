using rtp;

namespace rtp.Tests.Wire;

public class RtpHeaderExtensionValidationCoverageTests
{
    [Test]
    public void ElementOperationsRejectEveryInvalidEditableInput()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension automatic = CreateAutomaticExtension();
        RtpHeaderExtension opaque = new RtpHeaderExtension();
        opaque.Profile = 0x1234;
        opaque.Data = new byte[] { 1, 2, 3, 4 };
        RtpHeaderExtension invalidAutomatic = CreateAutomaticExtension();
        invalidAutomatic.ApplicationBits = 16;
        RtpHeaderExtensionElement valid = CreateElement(1, new byte[] { 1 });
        RtpHeaderExtensionElement badIdentifier = CreateElement(0, []);
        RtpHeaderExtensionElement missingData = CreateElement(2, []);
        missingData.Data = null!;
        RtpHeaderExtensionElement oversized = CreateElement(2, new byte[256]);
        RtpHeaderExtensionElement absent = CreateElement(2, []);

        Assert.Multiple(() =>
        {
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(null!, valid));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(opaque, valid));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(invalidAutomatic, valid));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(automatic, null!));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(automatic, badIdentifier));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(automatic, missingData));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(automatic, oversized));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.AddRtpHeaderExtensionElement(automatic, valid));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.ReplaceRtpHeaderExtensionElement(null!, valid));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.ReplaceRtpHeaderExtensionElement(automatic, null!));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.ReplaceRtpHeaderExtensionElement(automatic, absent));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.RemoveRtpHeaderExtensionElement(null!, 1));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.RemoveRtpHeaderExtensionElement(automatic, 0));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.RemoveRtpHeaderExtensionElement(automatic, 2));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.FindRtpHeaderExtensionElement(null!, 1));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.FindRtpHeaderExtensionElement(automatic, 256));
            Assert.That(codec.FindRtpHeaderExtensionElement(automatic, 2), Is.Null);
        });
    }

    [Test]
    public void OneByteSpecificOperationsCoverSuccessValidationCapacityAndParsing()
    {
        RtpHeaderExtension valid = CreateOneByteExtension();
        byte[] destination = new byte[8];
        RtpCodecResult encoded = RtpHeaderExtensionWireCodec
            .TryEncodeOneByteEnvelope(valid, destination);
        RtpHeaderExtension decoded = RtpHeaderExtensionWireCodec
            .DecodeOneByteEnvelope(destination);
        RtpHeaderExtension wrongFormat = CreateOneByteExtension();
        wrongFormat.Format = RtpHeaderExtensionFormat.TwoByte;
        RtpHeaderExtension wrongProfile = CreateOneByteExtension();
        wrongProfile.Profile = 0x1000;
        RtpHeaderExtension appBits = CreateOneByteExtension();
        appBits.ApplicationBits = 1;
        RtpHeaderExtension noData = CreateOneByteExtension();
        noData.Data = null!;
        RtpHeaderExtension nullElement = CreateOneByteExtension();
        nullElement.Elements = new RtpHeaderExtensionElement[] { null! };
        RtpHeaderExtension nullElementData = CreateOneByteExtension();
        nullElementData.Elements[0].Data = null!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Elements[0].Data, Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(RtpHeaderExtensionWireCodec.GetOneByteEnvelopeEncodedSize(valid),
                Is.EqualTo(8));
            Assert.That(RtpHeaderExtensionWireCodec.EncodeOneByteEnvelope(
                valid,
                destination), Is.EqualTo(8));
            Assert.That(RtpHeaderExtensionWireCodec.TryEncodeOneByteEnvelope(
                valid,
                new byte[7]).Success, Is.False);
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtpHeaderExtensionWireCodec.EncodeOneByteEnvelope(valid, new byte[7]));
            Assert.Throws<RtpPacketFormatException>(() =>
                RtpHeaderExtensionWireCodec.GetOneByteEnvelopeEncodedSize(wrongFormat));
            Assert.That(RtpHeaderExtensionWireCodec.GetOneByteValidationError(wrongProfile),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetOneByteValidationError(appBits),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetOneByteValidationError(noData),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetOneByteValidationError(nullElement),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetOneByteValidationError(
                nullElementData), Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.TryDecodeOneByteEnvelope([]).Success,
                Is.False);
            Assert.That(RtpHeaderExtensionWireCodec.TryDecodeOneByteEnvelope(
                new byte[] { 0x10, 0, 0, 0 }).Success, Is.False);
            Assert.That(RtpHeaderExtensionWireCodec.ParseOneByteData(
                0x1000,
                new byte[4]).Success, Is.False);
            Assert.That(RtpHeaderExtensionWireCodec.ParseOneByteData(
                RtpHeaderExtensionProfiles.OneByte,
                new byte[1]).Success, Is.False);
        });
    }

    [Test]
    public void TwoByteSpecificOperationsCoverSuccessValidationCapacityAndParsing()
    {
        RtpHeaderExtension valid = CreateTwoByteExtension();
        byte[] destination = new byte[8];
        RtpCodecResult encoded = RtpHeaderExtensionWireCodec
            .TryEncodeTwoByteEnvelope(valid, destination);
        RtpHeaderExtension decoded = RtpHeaderExtensionWireCodec
            .DecodeTwoByteEnvelope(destination);
        RtpHeaderExtension wrongFormat = CreateTwoByteExtension();
        wrongFormat.Format = RtpHeaderExtensionFormat.OneByte;
        RtpHeaderExtension wrongProfile = CreateTwoByteExtension();
        wrongProfile.Profile = 0x2000;
        RtpHeaderExtension noElements = CreateTwoByteExtension();
        noElements.Elements = null!;
        RtpHeaderExtension nullElement = CreateTwoByteExtension();
        nullElement.Elements = new RtpHeaderExtensionElement[] { null! };
        RtpHeaderExtension nullElementData = CreateTwoByteExtension();
        nullElementData.Elements[0].Data = null!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Elements[0].Data, Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(RtpHeaderExtensionWireCodec.GetTwoByteEnvelopeEncodedSize(valid),
                Is.EqualTo(8));
            Assert.That(RtpHeaderExtensionWireCodec.EncodeTwoByteEnvelope(
                valid,
                destination), Is.EqualTo(8));
            Assert.That(RtpHeaderExtensionWireCodec.TryEncodeTwoByteEnvelope(
                valid,
                new byte[7]).Success, Is.False);
            Assert.Throws<RtpBufferCapacityException>(() =>
                RtpHeaderExtensionWireCodec.EncodeTwoByteEnvelope(valid, new byte[7]));
            Assert.Throws<RtpPacketFormatException>(() =>
                RtpHeaderExtensionWireCodec.GetTwoByteEnvelopeEncodedSize(wrongFormat));
            Assert.That(RtpHeaderExtensionWireCodec.GetTwoByteValidationError(wrongProfile),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetTwoByteValidationError(noElements),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetTwoByteValidationError(nullElement),
                Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.GetTwoByteValidationError(
                nullElementData), Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec.TryDecodeTwoByteEnvelope([]).Success,
                Is.False);
            Assert.That(RtpHeaderExtensionWireCodec.TryDecodeTwoByteEnvelope(
                new byte[] { 0xBE, 0xDE, 0, 0 }).Success, Is.False);
            Assert.That(RtpHeaderExtensionWireCodec.ParseTwoByteData(
                RtpHeaderExtensionProfiles.OneByte,
                new byte[4]).Success, Is.False);
            Assert.That(RtpHeaderExtensionWireCodec.ParseTwoByteData(
                RtpHeaderExtensionProfiles.TwoByte,
                new byte[1]).Success, Is.False);
        });
    }

    static RtpHeaderExtension CreateAutomaticExtension()
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[] { 1 })
        };
        return extension;
    }

    static RtpHeaderExtension CreateOneByteExtension()
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = RtpHeaderExtensionProfiles.OneByte;
        extension.Format = RtpHeaderExtensionFormat.OneByte;
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[] { 0xAA })
        };
        return extension;
    }

    static RtpHeaderExtension CreateTwoByteExtension()
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = RtpHeaderExtensionProfiles.TwoByte;
        extension.Format = RtpHeaderExtensionFormat.TwoByte;
        extension.Elements = new RtpHeaderExtensionElement[]
        {
            CreateElement(1, new byte[] { 0xAA })
        };
        return extension;
    }

    static RtpHeaderExtensionElement CreateElement(int identifier, byte[] data)
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = data;
        return element;
    }
}
