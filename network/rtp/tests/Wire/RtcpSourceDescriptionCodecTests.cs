using rtp;

namespace rtp.Tests.Wire;

public class RtcpSourceDescriptionCodecTests
{
    [Test]
    public void EveryStandardItemAndAnUnknownItemRoundTripLosslessly()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x81, 202, 0, 9,
            1, 2, 3, 4,
            1, 1, 0x61,
            2, 1, 0x62,
            3, 1, 0x63,
            4, 1, 0x64,
            5, 1, 0x65,
            6, 1, 0x66,
            7, 1, 0x67,
            8, 3, 1, 0x70, 0x76,
            42, 2, 0xAA, 0xBB,
            0, 0
        };
        byte[] expected = (byte[])source.Clone();

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        source[10] = 0;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpSourceDescriptionItem[] items = packet.SourceDescription!.Chunks[0].Items;

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(packet.UnknownPacket, Is.Null);
            Assert.That(packet.Header.Count, Is.EqualTo(1));
            Assert.That(packet.Header.PacketType, Is.EqualTo(202));
            Assert.That(packet.Header.LengthInWordsMinusOne, Is.EqualTo(9));
            Assert.That(packet.SourceDescription.Chunks[0].SourceIdentifier,
                Is.EqualTo(0x01020304U));
            Assert.That(items.Length, Is.EqualTo(9));
            Assert.That(items[0].Type, Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(items[1].Type, Is.EqualTo(RtcpSdesItemType.Name));
            Assert.That(items[2].Type, Is.EqualTo(RtcpSdesItemType.Email));
            Assert.That(items[3].Type, Is.EqualTo(RtcpSdesItemType.Phone));
            Assert.That(items[4].Type, Is.EqualTo(RtcpSdesItemType.Location));
            Assert.That(items[5].Type, Is.EqualTo(RtcpSdesItemType.Tool));
            Assert.That(items[6].Type, Is.EqualTo(RtcpSdesItemType.Note));
            Assert.That(items[7].Type, Is.EqualTo(RtcpSdesItemType.Private));
            Assert.That(items[7].PrivatePrefix, Is.EqualTo(new byte[] { 0x70 }));
            Assert.That(items[7].Value, Is.EqualTo(new byte[] { 0x76 }));
            Assert.That(items[8].Type, Is.EqualTo(RtcpSdesItemType.Unknown));
            Assert.That(items[8].WireType, Is.EqualTo(42));
            Assert.That(items[8].Value, Is.EqualTo(new byte[] { 0xAA, 0xBB }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void EncoderTerminatesAndAlignsEachChunkIndependently()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSourceDescriptionItem canonicalName = new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = new byte[] { 0x61, 0x62 };
        RtcpSourceDescriptionChunk emptyChunk = new RtcpSourceDescriptionChunk();
        emptyChunk.SourceIdentifier = 1U;
        RtcpSourceDescriptionChunk namedChunk = new RtcpSourceDescriptionChunk();
        namedChunk.SourceIdentifier = 2U;
        namedChunk.Items = new RtcpSourceDescriptionItem[] { canonicalName };
        RtcpPacket packet = CreateSourceDescriptionPacket(
            new RtcpSourceDescriptionChunk[] { emptyChunk, namedChunk });
        packet.Header.Count = 17;
        packet.Header.PacketType = 220;
        packet.Header.LengthInWordsMinusOne = 99;

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                0x82, 202, 0, 5,
                0, 0, 0, 1,
                0, 0, 0, 0,
                0, 0, 0, 2,
                1, 2, 0x61, 0x62,
                0, 0, 0, 0
            }));
            Assert.That(decoded.SourceDescription?.Chunks.Length, Is.EqualTo(2));
            Assert.That(decoded.SourceDescription?.Chunks[0].Items, Is.Empty);
            Assert.That(decoded.SourceDescription?.Chunks[1].Items[0].Value,
                Is.EqualTo(new byte[] { 0x61, 0x62 }));
        });
    }

    [Test]
    public void SourceDescriptionSupportsThirtyOneChunks()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSourceDescriptionChunk[] chunks = new RtcpSourceDescriptionChunk[31];
        int chunkIndex = 0;
        while (chunkIndex < chunks.Length)
        {
            chunks[chunkIndex] = new RtcpSourceDescriptionChunk();
            chunks[chunkIndex].SourceIdentifier = (uint)(chunkIndex + 1);
            chunkIndex = chunkIndex + 1;
        }

        RtcpPacket packet = CreateSourceDescriptionPacket(chunks);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(252));
            Assert.That(encoded[..4], Is.EqualTo(new byte[] { 0x9F, 202, 0, 62 }));
            Assert.That(decoded.SourceDescription?.Chunks.Length, Is.EqualTo(31));
            Assert.That(decoded.SourceDescription?.Chunks[30].SourceIdentifier,
                Is.EqualTo(31U));
        });
    }

    [Test]
    public void PrivateItemSupportsItsMaximumCombinedPayloadLength()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpSourceDescriptionItem privateItem = new RtcpSourceDescriptionItem();
        privateItem.Type = RtcpSdesItemType.Private;
        privateItem.PrivatePrefix = new byte[253];
        privateItem.Value = new byte[] { 0x7F };
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.Items = new RtcpSourceDescriptionItem[] { privateItem };
        RtcpPacket packet = CreateSourceDescriptionPacket(
            new RtcpSourceDescriptionChunk[] { chunk });

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Length, Is.EqualTo(268));
            Assert.That(encoded[8], Is.EqualTo(8));
            Assert.That(encoded[9], Is.EqualTo(255));
            Assert.That(encoded[10], Is.EqualTo(253));
            Assert.That(decoded.SourceDescription?.Chunks[0].Items[0].PrivatePrefix.Length,
                Is.EqualTo(253));
            Assert.That(decoded.SourceDescription?.Chunks[0].Items[0].Value,
                Is.EqualTo(new byte[] { 0x7F }));
        });
    }

    [Test]
    public void DecoderRejectsMissingEndsTruncationAndInvalidAlignment()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] missingEnd = new byte[]
        {
            0x81, 202, 0, 1,
            0, 0, 0, 1
        };
        byte[] truncatedValue = new byte[]
        {
            0x81, 202, 0, 2,
            0, 0, 0, 1,
            1, 4, 0x61, 0x62
        };
        byte[] invalidAlignment = new byte[]
        {
            0x81, 202, 0, 2,
            0, 0, 0, 1,
            0, 0, 9, 0
        };
        byte[] countMismatch = new byte[]
        {
            0x80, 202, 0, 1,
            0, 0, 0, 0
        };
        byte[] emptyPrivateItem = new byte[]
        {
            0x81, 202, 0, 2,
            0, 0, 0, 1,
            8, 0, 0, 0
        };
        byte[] oversizedPrivatePrefix = new byte[]
        {
            0x81, 202, 0, 3,
            0, 0, 0, 1,
            8, 2, 2, 0x61,
            0, 0, 0, 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeRtcpPacket(missingEnd).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(truncatedValue).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(invalidAlignment).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(countMismatch).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(emptyPrivateItem).Success, Is.False);
            Assert.That(codec.TryDecodeRtcpPacket(oversizedPrivatePrefix).Success, Is.False);
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.DecodeRtcpPacket(missingEnd));
        });
    }

    [Test]
    public void InvalidSourceDescriptionsFailWithoutPartiallyWriting()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpPacket missingBody = new RtcpPacket();
        missingBody.Type = RtcpPacketType.SourceDescription;

        RtcpSourceDescriptionChunk[] tooManyChunks = new RtcpSourceDescriptionChunk[32];
        int chunkIndex = 0;
        while (chunkIndex < tooManyChunks.Length)
        {
            tooManyChunks[chunkIndex] = new RtcpSourceDescriptionChunk();
            chunkIndex = chunkIndex + 1;
        }

        RtcpPacket excessiveCount = CreateSourceDescriptionPacket(tooManyChunks);
        RtcpSourceDescriptionItem oversizedItem = new RtcpSourceDescriptionItem();
        oversizedItem.Type = RtcpSdesItemType.Name;
        oversizedItem.Value = new byte[256];
        RtcpSourceDescriptionChunk oversizedChunk = new RtcpSourceDescriptionChunk();
        oversizedChunk.Items = new RtcpSourceDescriptionItem[] { oversizedItem };
        RtcpPacket excessiveValue = CreateSourceDescriptionPacket(
            new RtcpSourceDescriptionChunk[] { oversizedChunk });
        RtcpSourceDescriptionItem invalidUnknown = new RtcpSourceDescriptionItem();
        invalidUnknown.Type = RtcpSdesItemType.Unknown;
        invalidUnknown.WireType = 8;
        RtcpSourceDescriptionChunk unknownChunk = new RtcpSourceDescriptionChunk();
        unknownChunk.Items = new RtcpSourceDescriptionItem[] { invalidUnknown };
        RtcpPacket invalidWireType = CreateSourceDescriptionPacket(
            new RtcpSourceDescriptionChunk[] { unknownChunk });
        byte[] destination = new byte[300];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        RtcpCodecResult missingResult = codec.TryEncodeRtcpPacket(missingBody, destination);
        RtcpCodecResult countResult = codec.TryEncodeRtcpPacket(excessiveCount, destination);
        RtcpCodecResult valueResult = codec.TryEncodeRtcpPacket(excessiveValue, destination);
        RtcpCodecResult typeResult = codec.TryEncodeRtcpPacket(invalidWireType, destination);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Success, Is.False);
            Assert.That(countResult.Success, Is.False);
            Assert.That(valueResult.Success, Is.False);
            Assert.That(typeResult.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.GetRtcpPacketEncodedSize(excessiveValue));
        });
    }

    static RtcpPacket CreateSourceDescriptionPacket(
        RtcpSourceDescriptionChunk[] chunks)
    {
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = chunks;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static IRtcpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
