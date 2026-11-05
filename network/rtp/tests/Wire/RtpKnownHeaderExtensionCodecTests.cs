using System.Text;
using rtp;

namespace rtp.Tests.Wire;

public class RtpKnownHeaderExtensionCodecTests
{
    [Test]
    public void ClientAudioLevelRoundTripsVoiceActivityAndMagnitude()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpAudioLevelExtension value = new RtpAudioLevelExtension();
        value.VoiceActivity = true;
        value.Level = 42;
        byte[] destination = new byte[1];

        RtpCodecResult encodeResult = codec.TryEncodeAudioLevelExtension(
            value,
            destination);
        RtpAudioLevelExtension decoded = codec.DecodeAudioLevelExtension(destination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetAudioLevelExtensionEncodedSize(value), Is.EqualTo(1));
            Assert.That(encodeResult.Success, Is.True);
            Assert.That(encodeResult.WrittenCount, Is.EqualTo(1));
            Assert.That(destination, Is.EqualTo(new byte[] { 170 }));
            Assert.That(decoded.VoiceActivity, Is.True);
            Assert.That(decoded.Level, Is.EqualTo(42));
        });
    }

    [Test]
    public void MixerAudioLevelsRoundTripInCsrcOrder()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpMixerAudioLevelExtension value = new RtpMixerAudioLevelExtension();
        value.Levels = new byte[] { 0, 64, 127 };
        byte[] destination = new byte[3];

        int written = codec.EncodeMixerAudioLevelExtension(value, destination);
        RtpCodecResult decodeResult = codec.TryDecodeMixerAudioLevelExtension(destination);
        RtpMixerAudioLevelExtension decoded =
            codec.DecodeMixerAudioLevelExtension(destination);
        destination[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(3));
            Assert.That(decodeResult.Success, Is.True);
            Assert.That(decodeResult.ConsumedCount, Is.EqualTo(3));
            Assert.That(decodeResult.MixerAudioLevelExtension?.Levels,
                Is.EqualTo(new byte[] { 0, 64, 127 }));
            Assert.That(decoded.Levels,
                Is.EqualTo(new byte[] { 0, 64, 127 }));
        });
    }

    [Test]
    public void Ntp64UsesNetworkByteOrder()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpNtp64Extension value = new RtpNtp64Extension();
        value.Timestamp = 0x0123456789ABCDEFUL;
        byte[] destination = new byte[8];

        int written = codec.EncodeNtp64Extension(value, destination);
        RtpNtp64Extension decoded = codec.DecodeNtp64Extension(destination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetNtp64ExtensionEncodedSize(value), Is.EqualTo(8));
            Assert.That(written, Is.EqualTo(8));
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF
            }));
            Assert.That(decoded.Timestamp, Is.EqualTo(0x0123456789ABCDEFUL));
        });
    }

    [Test]
    public void MidRidAndRepairedRidUseTheirRequiredTextEncodings()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpStreamIdentifierExtension mid = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "映像");
        RtpStreamIdentifierExtension rid = CreateStreamIdentifier(
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            "Video7");
        RtpStreamIdentifierExtension repairedRid = CreateStreamIdentifier(
            RtpHeaderExtensionUris.RepairedRtpStreamIdentifier,
            "Repair2");
        byte[] midBytes = new byte[codec.GetStreamIdentifierExtensionEncodedSize(mid)];
        byte[] ridBytes = new byte[codec.GetStreamIdentifierExtensionEncodedSize(rid)];
        byte[] repairedRidBytes =
            new byte[codec.GetStreamIdentifierExtensionEncodedSize(repairedRid)];

        codec.EncodeStreamIdentifierExtension(mid, midBytes);
        codec.EncodeStreamIdentifierExtension(rid, ridBytes);
        codec.EncodeStreamIdentifierExtension(repairedRid, repairedRidBytes);

        Assert.Multiple(() =>
        {
            Assert.That(midBytes, Is.EqualTo(Encoding.UTF8.GetBytes("映像")));
            Assert.That(codec.DecodeStreamIdentifierExtension(
                midBytes,
                RtpHeaderExtensionUris.MediaIdentifier).Value, Is.EqualTo("映像"));
            Assert.That(codec.DecodeStreamIdentifierExtension(
                ridBytes,
                RtpHeaderExtensionUris.RtpStreamIdentifier).Value, Is.EqualTo("Video7"));
            Assert.That(codec.DecodeStreamIdentifierExtension(
                repairedRidBytes,
                RtpHeaderExtensionUris.RepairedRtpStreamIdentifier).Value,
                Is.EqualTo("Repair2"));
        });
    }

    [Test]
    public void InvalidKnownPayloadsAndSmallDestinationsFailWithoutPartialWrites()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpAudioLevelExtension invalidAudio = new RtpAudioLevelExtension();
        invalidAudio.Level = 128;
        RtpMixerAudioLevelExtension invalidMixer = new RtpMixerAudioLevelExtension();
        invalidMixer.Levels = new byte[] { 1, 128 };
        RtpStreamIdentifierExtension invalidRid = CreateStreamIdentifier(
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            "not-valid");
        byte[] untouched = new byte[] { 77 };

        RtpCodecResult audioResult = codec.TryEncodeAudioLevelExtension(
            invalidAudio,
            untouched);
        RtpCodecResult mixerResult = codec.TryEncodeMixerAudioLevelExtension(
            invalidMixer,
            untouched);
        RtpCodecResult ntpResult = codec.TryEncodeNtp64Extension(
            new RtpNtp64Extension(),
            untouched);
        RtpCodecResult ridResult = codec.TryEncodeStreamIdentifierExtension(
            invalidRid,
            untouched);

        Assert.Multiple(() =>
        {
            Assert.That(audioResult.Success, Is.False);
            Assert.That(mixerResult.Success, Is.False);
            Assert.That(ntpResult.Success, Is.False);
            Assert.That(ridResult.Success, Is.False);
            Assert.That(untouched, Is.EqualTo(new byte[] { 77 }));
            Assert.That(codec.TryDecodeAudioLevelExtension(new byte[0]).Success, Is.False);
            Assert.That(codec.TryDecodeMixerAudioLevelExtension(
                new byte[] { 128 }).Success, Is.False);
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.DecodeMixerAudioLevelExtension(new byte[] { 128 }));
            Assert.That(codec.TryDecodeNtp64Extension(new byte[7]).Success, Is.False);
            Assert.That(codec.TryDecodeStreamIdentifierExtension(
                new byte[] { 0xC0, 0x80 },
                RtpHeaderExtensionUris.MediaIdentifier).Success, Is.False);
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.DecodeNtp64Extension(new byte[9]));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeNtp64Extension(new RtpNtp64Extension(), untouched));
        });
    }

    [Test]
    public void UnknownUriDecodingReturnsAnOwnedRawFallback()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        byte[] source = new byte[] { 0xFF, 0x00, 0x81 };

        RtpCodecResult result = codec.TryDecodeStreamIdentifierExtension(
            source,
            "urn:example:future-extension");
        source[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(3));
            Assert.That(result.StreamIdentifierExtension, Is.Null);
            Assert.That(result.Data, Is.EqualTo(new byte[] { 0xFF, 0x00, 0x81 }));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.DecodeStreamIdentifierExtension(
                    result.Data,
                    "urn:example:future-extension"));
        });
    }

    static RtpStreamIdentifierExtension CreateStreamIdentifier(string uri, string value)
    {
        RtpStreamIdentifierExtension identifier = new RtpStreamIdentifierExtension();
        identifier.Uri = uri;
        identifier.Value = value;
        return identifier;
    }
}
