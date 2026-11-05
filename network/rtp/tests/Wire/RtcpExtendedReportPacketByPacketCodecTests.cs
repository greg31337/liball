using rtp;

namespace rtp.Tests.Wire;

public class RtcpExtendedReportPacketByPacketCodecTests
{
    [Test]
    public void LossAndDuplicateRleBlocksRoundTripChunkForms()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 9,
            1, 2, 3, 4,
            1, 2, 0, 3,
            0x11, 0x11, 0x11, 0x11,
            0x35, 0xFD, 0x36, 0x2A,
            0xFD, 0xF0, 0, 0,
            2, 0, 0, 3,
            0x22, 0x22, 0x22, 0x22,
            0, 100, 0, 120,
            0x40, 5, 0, 15
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock loss = packet.ExtendedReport!.Blocks[0];
        RtcpExtendedReportBlock duplicate = packet.ExtendedReport.Blocks[1];

        Assert.Multiple(() =>
        {
            Assert.That(loss.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.LossRunLength));
            Assert.That(loss.Thinning, Is.EqualTo(2));
            Assert.That(loss.SourceIdentifier, Is.EqualTo(0x11111111U));
            Assert.That(loss.BeginSequenceNumber, Is.EqualTo(13821));
            Assert.That(loss.EndSequenceNumber, Is.EqualTo(13866));
            Assert.That(loss.RunLengthChunks.Length, Is.EqualTo(1));
            Assert.That(loss.RunLengthChunks[0].IsBitVector, Is.True);
            Assert.That(loss.RunLengthChunks[0].BitVector, Is.EqualTo(0x7DF0));
            Assert.That(duplicate.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.DuplicateRunLength));
            Assert.That(duplicate.RunLengthChunks.Length, Is.EqualTo(2));
            Assert.That(duplicate.RunLengthChunks[0].PositiveRun, Is.True);
            Assert.That(duplicate.RunLengthChunks[0].RunLength, Is.EqualTo(5));
            Assert.That(duplicate.RunLengthChunks[1].PositiveRun, Is.False);
            Assert.That(duplicate.RunLengthChunks[1].RunLength, Is.EqualTo(15));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void ReceiptTimesFollowThinnedSequenceRangeAcrossWrap()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 7,
            1, 2, 3, 4,
            3, 1, 0, 5,
            0x33, 0x33, 0x33, 0x33,
            0xFF, 0xFE, 0, 4,
            1, 2, 3, 4,
            0x11, 0x22, 0x33, 0x44,
            0xAA, 0xBB, 0xCC, 0xDD
        };

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock block = packet.ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(block.Type,
                Is.EqualTo(RtcpExtendedReportBlockType.PacketReceiptTimes));
            Assert.That(block.Thinning, Is.EqualTo(1));
            Assert.That(block.SourceIdentifier, Is.EqualTo(0x33333333U));
            Assert.That(block.BeginSequenceNumber, Is.EqualTo(65534));
            Assert.That(block.EndSequenceNumber, Is.EqualTo(4));
            Assert.That(block.ReceiptTimes,
                Is.EqualTo(new uint[]
                {
                    0x01020304U,
                    0x11223344U,
                    0xAABBCCDDU
                }));
            Assert.That(codec.EncodeRtcpPacketToArray(packet), Is.EqualTo(source));
        });
    }

    [Test]
    public void TypedPacketByPacketDataOverridesRawBlockBytesAndLengths()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpRunLengthChunk chunk = new RtcpRunLengthChunk();
        chunk.PositiveRun = true;
        chunk.RunLength = 3;
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.LossRunLength;
        block.WireType = 0;
        block.TypeSpecific = 0xF9;
        block.BlockLengthInWordsMinusOne = 99;
        block.SourceIdentifier = 0x10203040U;
        block.Thinning = 0;
        block.BeginSequenceNumber = 10;
        block.EndSequenceNumber = 13;
        block.RunLengthChunks = new RtcpRunLengthChunk[] { chunk };
        block.Data = new byte[] { 9, 9, 9, 9 };
        RtcpPacket packet = CreatePacket(block);

        byte[] encoded = codec.EncodeRtcpPacketToArray(packet);

        Assert.That(encoded,
            Is.EqualTo(new byte[]
            {
                0x80, 207, 0, 5,
                1, 2, 3, 4,
                1, 0, 0, 3,
                0x10, 0x20, 0x30, 0x40,
                0, 10, 0, 13,
                0x40, 3, 0, 0
            }));
    }

    [Test]
    public void DecoderIgnoresReservedTypeSpecificBitsAndCanonicalizesThem()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[] source = new byte[]
        {
            0x80, 207, 0, 4,
            1, 2, 3, 4,
            3, 0xF2, 0, 2,
            0, 0, 0, 9,
            0, 1, 0, 2
        };
        byte[] canonical = (byte[])source.Clone();
        canonical[9] = 2;

        RtcpPacket packet = codec.DecodeRtcpPacket(source);
        RtcpExtendedReportBlock block = packet.ExtendedReport!.Blocks[0];

        Assert.Multiple(() =>
        {
            Assert.That(block.TypeSpecific, Is.EqualTo(0xF2));
            Assert.That(block.Thinning, Is.EqualTo(2));
            Assert.That(block.ReceiptTimes, Is.Empty);
            Assert.That(codec.EncodeRtcpPacketToArray(packet),
                Is.EqualTo(canonical));
        });
    }

    [Test]
    public void DecoderRejectsMalformedPacketByPacketBlocks()
    {
        IRtcpPacketCodec codec = CreateCodec();
        byte[][] malformedPackets = new byte[][]
        {
            CreateSingleBlockPacket(1, 0, 1, new byte[]
            {
                0, 0, 0, 1
            }),
            CreateSingleBlockPacket(1, 0, 2, new byte[]
            {
                0, 0, 0, 1,
                0, 0, 0xFF, 0xFE
            }),
            CreateSingleBlockPacket(1, 0, 3, new byte[]
            {
                0, 0, 0, 1,
                0, 10, 0, 11,
                0x40, 0, 0, 0
            }),
            CreateSingleBlockPacket(1, 0, 3, new byte[]
            {
                0, 0, 0, 1,
                0, 10, 0, 11,
                0, 0, 0x40, 1
            }),
            CreateSingleBlockPacket(1, 0, 3, new byte[]
            {
                0, 0, 0, 1,
                0, 10, 0, 12,
                0x40, 1, 0, 0
            }),
            CreateSingleBlockPacket(1, 0, 3, new byte[]
            {
                0, 0, 0, 1,
                0, 10, 0, 11,
                0x40, 2, 0, 0
            }),
            CreateSingleBlockPacket(1, 0, 3, new byte[]
            {
                0, 0, 0, 1,
                0, 10, 0, 13,
                0xD0, 1, 0, 0
            }),
            CreateSingleBlockPacket(3, 0, 3, new byte[]
            {
                0, 0, 0, 1,
                0, 10, 0, 12,
                1, 2, 3, 4
            })
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
    public void EncoderRejectsInvalidPacketByPacketModelsAtomically()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpRunLengthChunk chunk = new RtcpRunLengthChunk();
        chunk.PositiveRun = true;
        chunk.RunLength = 1;
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.LossRunLength;
        block.SourceIdentifier = 1;
        block.BeginSequenceNumber = 10;
        block.EndSequenceNumber = 11;
        block.RunLengthChunks = new RtcpRunLengthChunk[] { chunk };
        RtcpPacket packet = CreatePacket(block);
        byte[] destination = new byte[64];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        block.Thinning = 16;
        AssertEncodingFails(codec, packet, destination);
        block.Thinning = 0;

        block.EndSequenceNumber = 65534;
        block.BeginSequenceNumber = 0;
        AssertEncodingFails(codec, packet, destination);
        block.BeginSequenceNumber = 10;
        block.EndSequenceNumber = 11;

        block.RunLengthChunks = null!;
        AssertEncodingFails(codec, packet, destination);
        block.RunLengthChunks = new RtcpRunLengthChunk[] { null! };
        AssertEncodingFails(codec, packet, destination);
        block.RunLengthChunks = new RtcpRunLengthChunk[] { chunk };

        chunk.RunLength = 0;
        AssertEncodingFails(codec, packet, destination);
        chunk.RunLength = 16384;
        AssertEncodingFails(codec, packet, destination);
        chunk.RunLength = 1;

        chunk.IsBitVector = true;
        chunk.BitVector = 32768;
        AssertEncodingFails(codec, packet, destination);
        chunk.BitVector = 1;
        AssertEncodingFails(codec, packet, destination);
        chunk.IsBitVector = false;
        chunk.BitVector = 0;

        block.EndSequenceNumber = 12;
        AssertEncodingFails(codec, packet, destination);
        block.EndSequenceNumber = 11;

        block.Type = RtcpExtendedReportBlockType.PacketReceiptTimes;
        block.ReceiptTimes = new uint[0];
        AssertEncodingFails(codec, packet, destination);

        Assert.That(destination, Has.All.EqualTo(77));
    }

    static byte[] CreateSingleBlockPacket(
        byte blockType,
        byte typeSpecific,
        ushort blockLength,
        byte[] data)
    {
        int packetSize = 12 + data.Length;
        byte[] packet = new byte[packetSize];
        packet[0] = 0x80;
        packet[1] = 207;
        packet[2] = (byte)((packetSize / 4 - 1) >> 8);
        packet[3] = (byte)(packetSize / 4 - 1);
        packet[7] = 9;
        packet[8] = blockType;
        packet[9] = typeSpecific;
        packet[10] = (byte)(blockLength >> 8);
        packet[11] = (byte)blockLength;
        data.CopyTo(packet, 12);
        return packet;
    }

    static RtcpPacket CreatePacket(RtcpExtendedReportBlock block)
    {
        RtcpExtendedReport report = new RtcpExtendedReport();
        report.SenderSynchronizationSource = 0x01020304U;
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

    static IRtcpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }
}
