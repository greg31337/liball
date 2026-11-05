using rtp;

namespace rtp.Tests.Wire;

public class RtcpCompoundCodecTests
{
    [Test]
    public void CompoundIterationDispatchesTypedAndUnknownPacketsLosslessly()
    {
        IRtcpPacketCodec codec = CreateCodec(65535, 32);
        byte[] source = new byte[]
        {
            0x80, 200, 0, 6,
            0, 0, 0, 1,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x80, 201, 0, 1,
            0, 0, 0, 1,
            0x81, 202, 0, 2,
            0, 0, 0, 1,
            1, 1, 0x78, 0,
            0x85, 204, 0, 2,
            0, 0, 0, 1,
            0x54, 0x45, 0x53, 0x54,
            0x87, 222, 0, 1,
            9, 8, 7, 6,
            0x81, 203, 0, 1,
            0, 0, 0, 1
        };
        byte[] expected = (byte[])source.Clone();

        RtcpCodecResult result = codec.TryDecodeRtcpCompoundPacket(source);
        source[64] = 0;
        byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(
            result.CompoundPacket!);
        RtcpPacket[] packets = result.CompoundPacket!.Packets;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(76));
            Assert.That(packets.Length, Is.EqualTo(6));
            Assert.That(packets[0].Type, Is.EqualTo(RtcpPacketType.SenderReport));
            Assert.That(packets[0].SenderReport?.SenderSynchronizationSource,
                Is.EqualTo(1U));
            Assert.That(packets[1].Type, Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(packets[1].ReceiverReport?.SenderSynchronizationSource,
                Is.EqualTo(1U));
            Assert.That(packets[2].Type, Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(packets[2].SourceDescription?.Chunks[0].Items[0].Value,
                Is.EqualTo(new byte[] { 0x78 }));
            Assert.That(packets[3].Type, Is.EqualTo(RtcpPacketType.ApplicationDefined));
            Assert.That(packets[3].ApplicationDefined?.Subtype, Is.EqualTo(5));
            Assert.That(packets[4].Type, Is.EqualTo(RtcpPacketType.Unknown));
            Assert.That(packets[4].UnknownPacket?.PacketType, Is.EqualTo(222));
            Assert.That(packets[4].UnknownPacket?.Count, Is.EqualTo(7));
            Assert.That(packets[4].UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(packets[5].Type, Is.EqualTo(RtcpPacketType.Goodbye));
            Assert.That(packets[5].Goodbye?.Sources, Is.EqualTo(new uint[] { 1U }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void AggregatePacketAndByteLimitsApplyToDecodeAndEncode()
    {
        byte[] source = CreateThreePacketCompound();
        IRtcpPacketCodec unrestrictedCodec = CreateCodec(64, 3);
        RtcpCompoundPacket compound = unrestrictedCodec.DecodeRtcpCompoundPacket(source);
        RtpSessionConfiguration countConfiguration = CreateConfiguration(64, 2);
        IRtcpPacketCodec countLimitedCodec = new RtcpPacketCodec(countConfiguration);
        countConfiguration.MaximumRtcpCompoundPacketCount = 3;
        IRtcpPacketCodec byteLimitedCodec = CreateCodec(44, 3);
        IRtcpPacketCodec boundaryCodec = CreateCodec(40, 2);
        byte[] destination = new byte[48];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        RtcpCodecResult countDecode = countLimitedCodec.TryDecodeRtcpCompoundPacket(source);
        RtcpCodecResult byteDecode = byteLimitedCodec.TryDecodeRtcpCompoundPacket(source);
        RtcpCodecResult boundaryDecode = boundaryCodec.TryDecodeRtcpCompoundPacket(source[..40]);
        RtcpCodecResult countEncode = countLimitedCodec.TryEncodeRtcpCompoundPacket(
            compound,
            destination);
        RtcpCodecResult byteEncode = byteLimitedCodec.TryEncodeRtcpCompoundPacket(
            compound,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(countDecode.Success, Is.False);
            Assert.That(countDecode.CompoundPacket, Is.Null);
            Assert.That(byteDecode.Success, Is.False);
            Assert.That(byteDecode.CompoundPacket, Is.Null);
            Assert.That(boundaryDecode.Success, Is.True);
            Assert.That(boundaryDecode.ConsumedCount, Is.EqualTo(40));
            Assert.That(boundaryDecode.CompoundPacket?.Packets.Length, Is.EqualTo(2));
            Assert.That(countEncode.Success, Is.False);
            Assert.That(byteEncode.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                countLimitedCodec.GetRtcpCompoundPacketEncodedSize(compound));
        });
    }

    [Test]
    public void MalformedMiddlePacketsAndTrailingFragmentsFailTheWholeCompound()
    {
        IRtcpPacketCodec codec = CreateCodec(64, 3);
        byte[] validPrefix = CreateThreePacketCompound()[..40];
        byte[] malformedMiddle = (byte[])validPrefix.Clone();
        malformedMiddle[39] = 1;
        byte[] trailingFragment = new byte[42];
        validPrefix.CopyTo(trailingFragment, 0);

        RtcpCodecResult malformedResult = codec.TryDecodeRtcpCompoundPacket(
            malformedMiddle);
        RtcpCodecResult trailingResult = codec.TryDecodeRtcpCompoundPacket(
            trailingFragment);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.Success, Is.False);
            Assert.That(malformedResult.ConsumedCount, Is.Zero);
            Assert.That(malformedResult.CompoundPacket, Is.Null);
            Assert.That(trailingResult.Success, Is.False);
            Assert.That(trailingResult.ConsumedCount, Is.Zero);
            Assert.That(trailingResult.CompoundPacket, Is.Null);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpCompoundPacket(malformedMiddle));
        });
    }

    [Test]
    public void CompoundPacketCountConfigurationEnforcesWireBounds()
    {
        RtpSessionConfiguration zero = new RtpSessionConfiguration();
        zero.MaximumRtcpCompoundPacketCount = 0;
        RtpSessionConfiguration tooLarge = new RtpSessionConfiguration();
        tooLarge.MaximumRtcpCompoundPacketCount =
            RtcpProtocolConstants.MaximumCompoundPacketCount + 1;

        Assert.Multiple(() =>
        {
            Assert.That(RtcpProtocolConstants.MaximumCompoundPacketCount,
                Is.EqualTo(65536));
            Assert.Throws<RtpConfigurationException>(() => new RtcpPacketCodec(zero));
            Assert.Throws<RtpConfigurationException>(() => new RtcpPacketCodec(tooLarge));
        });
    }

    static byte[] CreateThreePacketCompound()
    {
        return new byte[]
        {
            0x80, 200, 0, 6,
            0, 0, 0, 1,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0x81, 202, 0, 2,
            0, 0, 0, 1,
            1, 1, 0x78, 0,
            0x81, 203, 0, 1,
            0, 0, 0, 1
        };
    }

    static RtpSessionConfiguration CreateConfiguration(
        int maximumPacketSize,
        int maximumPacketCount)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = maximumPacketSize;
        configuration.MaximumRtcpCompoundPacketCount = maximumPacketCount;
        return configuration;
    }

    static IRtcpPacketCodec CreateCodec(
        int maximumPacketSize,
        int maximumPacketCount)
    {
        return new RtcpPacketCodec(CreateConfiguration(
            maximumPacketSize,
            maximumPacketCount));
    }
}
