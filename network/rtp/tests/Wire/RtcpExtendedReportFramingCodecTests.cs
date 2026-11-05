using rtp;

namespace rtp.Tests.Wire;

public class RtcpExtendedReportFramingCodecTests
{
    [Test]
    public void EmptyExtendedReportRoundTripsSenderSsrc()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0x80, 207, 0, 1,
            0xDE, 0xAD, 0xBE, 0xEF
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Type, Is.EqualTo(RtcpPacketType.ExtendedReport));
            Assert.That(packet.UnknownPacket, Is.Null);
            Assert.That(packet.ExtendedReport?.SenderSynchronizationSource,
                Is.EqualTo(0xDEADBEEFU));
            Assert.That(packet.ExtendedReport?.Blocks, Is.Empty);
            Assert.That(codec.GetRtcpPacketEncodedSize(packet), Is.EqualTo(8));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void UnknownAndRecognizedBlocksRoundTripRawContentsLosslessly()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0x80, 207, 0, 8,
            1, 2, 3, 4,
            200, 0xA5, 0, 2,
            0x11, 0x22, 0x33, 0x44,
            0x55, 0x66, 0x77, 0x88,
            255, 0x7F, 0, 0,
            4, 0, 0, 2,
            0xDE, 0xAD, 0xBE, 0xEF,
            1, 2, 3, 4
        };
        byte[] expected = (byte[])source.Clone();

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        source[12] = 0;
        RtcpExtendedReportBlock[] blocks = packet.ExtendedReport!.Blocks;
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(blocks.Length, Is.EqualTo(3));
            Assert.That(blocks[0].Type,
                Is.EqualTo(RtcpExtendedReportBlockType.Unknown));
            Assert.That(blocks[0].WireType, Is.EqualTo(200));
            Assert.That(blocks[0].TypeSpecific, Is.EqualTo(0xA5));
            Assert.That(blocks[0].BlockLengthInWordsMinusOne, Is.EqualTo(2));
            Assert.That(blocks[0].Data,
                Is.EqualTo(new byte[]
                {
                    0x11, 0x22, 0x33, 0x44,
                    0x55, 0x66, 0x77, 0x88
                }));
            Assert.That(blocks[1].Type,
                Is.EqualTo(RtcpExtendedReportBlockType.Unknown));
            Assert.That(blocks[1].WireType, Is.EqualTo(255));
            Assert.That(blocks[1].Data, Is.Empty);
            Assert.That(blocks[2].Type,
                Is.EqualTo(RtcpExtendedReportBlockType.ReceiverReferenceTime));
            Assert.That(blocks[2].WireType, Is.EqualTo(4));
            Assert.That(blocks[2].Data,
                Is.EqualTo(new byte[]
                {
                    0xDE, 0xAD, 0xBE, 0xEF,
                    1, 2, 3, 4
                }));
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    [Test]
    public void EncoderDerivesPacketAndBlockLengthsFromTypedData()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.ReceiverReferenceTime;
        block.TypeSpecific = 0x31;
        block.BlockLengthInWordsMinusOne = 99;
        block.ReceiverReferenceTime = 0x0102030405060708UL;
        block.Data = new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8
        };
        RtcpPacket packet = CreatePacket(block);
        packet.Header.Count = 31;
        packet.Header.PacketType = 1;
        packet.Header.LengthInWordsMinusOne = 99;

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);
        RtcpPacket decoded = codec.DecodeRtcpPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x80, 207, 0, 4,
                    0x10, 0x20, 0x30, 0x40,
                    4, 0, 0, 2,
                    1, 2, 3, 4,
                    5, 6, 7, 8
                }));
            Assert.That(decoded.ExtendedReport?.Blocks[0]
                .BlockLengthInWordsMinusOne, Is.EqualTo(2));
        });
    }

    [Test]
    public void ExtendedReportPreservesOuterRtcpPadding()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0xA0, 207, 0, 2,
            1, 2, 3, 4,
            0xAA, 0xBB, 0xCC, 4
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);

        Assert.Multiple(() =>
        {
            Assert.That(packet.ExtendedReport?.Blocks, Is.Empty);
            Assert.That(packet.Padding,
                Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 4 }));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void DecoderRejectsMissingSenderMisalignmentAndTruncatedBlocks()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[][] malformedPackets = new byte[][]
        {
            new byte[]
            {
                0x80, 207, 0, 0
            },
            new byte[]
            {
                0xA0, 207, 0, 2,
                1, 2, 3, 4,
                9, 8, 7, 1
            },
            new byte[]
            {
                0x80, 207, 0, 3,
                1, 2, 3, 4,
                200, 0, 0, 2,
                5, 6, 7, 8
            },
            new byte[]
            {
                0x80, 207, 0, 2,
                1, 2, 3, 4,
                200, 0, 0xFF, 0xFF
            }
        };

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < malformedPackets.Length)
            {
                Assert.That(codec.TryDecodeRtcpPacket(malformedPackets[index]).Success,
                    Is.False,
                    $"Malformed vector {index} was accepted.");
                index = index + 1;
            }
        });
    }

    [Test]
    public void ReservedPacketBitsAreIgnoredAndNormalized()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        byte[] source = new byte[]
        {
            0x9F, 207, 0, 1,
            1, 2, 3, 4
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Header.Count, Is.EqualTo(31));
            Assert.That(packet.ExtendedReport, Is.Not.Null);
            Assert.That(encoded,
                Is.EqualTo(new byte[]
                {
                    0x80, 207, 0, 1,
                    1, 2, 3, 4
                }));
        });
    }

    [Test]
    public void EncoderRejectsInvalidBlockModelsAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.WireType = 200;
        RtcpPacket packet = CreatePacket(block);
        byte[] destination = new byte[32];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        packet.ExtendedReport = null;
        AssertEncodingFails(codec, packet, destination);
        packet.ExtendedReport = new RtcpExtendedReport();

        packet.ExtendedReport.Blocks = null!;
        AssertEncodingFails(codec, packet, destination);
        packet.ExtendedReport.Blocks =
            new RtcpExtendedReportBlock[] { null! };
        AssertEncodingFails(codec, packet, destination);
        packet.ExtendedReport.Blocks =
            new RtcpExtendedReportBlock[] { block };

        block.Type = (RtcpExtendedReportBlockType)8;
        AssertEncodingFails(codec, packet, destination);
        block.Type = RtcpExtendedReportBlockType.LossRunLength;
        block.WireType = 2;
        AssertEncodingFails(codec, packet, destination);
        block.Type = RtcpExtendedReportBlockType.Unknown;
        block.WireType = 200;

        block.Data = null!;
        AssertEncodingFails(codec, packet, destination);
        block.Data = new byte[] { 1 };
        AssertEncodingFails(codec, packet, destination);

        Assert.That(destination, Has.All.EqualTo(77));
    }

    static RtcpPacket CreatePacket(RtcpExtendedReportBlock block)
    {
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = 0x10203040U;
        report.Blocks = new RtcpExtendedReportBlock[] { block };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ExtendedReport;
        packet.ExtendedReport = report;
        return packet;
    }

    static void AssertEncodingFails(
        IRtcpPacketCodec codec,
        RtcpPacket packet,
        byte[] destination)
    {
        RtcpCodecResult result = codec.TryEncodeRtcpPacket(packet, destination);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    static IRtcpPacketCodec CreateCodec(bool preservePadding)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = preservePadding;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
