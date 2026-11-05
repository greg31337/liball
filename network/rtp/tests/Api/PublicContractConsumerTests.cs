using rtp;

namespace rtp.Tests.Api;

public class PublicContractConsumerTests
{
    [Test]
    public void RtpPacketCodecContractSupportsSpanAndOwnedArrayOperations()
    {
        IRtpPacketCodec codec = new TestPacketCodec();
        RtpPacket packet = new RtpPacket();
        packet.Payload = new byte[] { 1, 2, 3 };
        byte[] destination = new byte[3];

        int size = codec.GetRtpPacketEncodedSize(packet);
        int written = codec.EncodeRtpPacket(packet, destination);
        RtpCodecResult tryEncode = codec.TryEncodeRtpPacket(packet, destination);
        byte[] encoded = codec.EncodeRtpPacketToArray(packet);
        RtpPacket decoded = codec.DecodeRtpPacket(encoded);
        RtpCodecResult tryDecode = codec.TryDecodeRtpPacket(encoded);
        packet.Payload[0] = 99;
        encoded[0] = 98;

        Assert.Multiple(() =>
        {
            Assert.That(size, Is.EqualTo(3));
            Assert.That(written, Is.EqualTo(3));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(tryEncode.Success, Is.True);
            Assert.That(tryEncode.WrittenCount, Is.EqualTo(3));
            Assert.That(decoded.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(tryDecode.Success, Is.True);
            Assert.That(tryDecode.ConsumedCount, Is.EqualTo(3));
            Assert.That(tryDecode.Packet?.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ApplicationMediaContractsComposePacketizationAndRepairOperations()
    {
        TestMediaPlugin plugin = new TestMediaPlugin();
        IRtpPayloadPacketizer packetizer = plugin;
        IRtpPayloadDepacketizer depacketizer = plugin;
        IRtpRepairEncoder repairEncoder = plugin;
        IRtpRepairDecoder repairDecoder = plugin;
        byte[] media = new byte[] { 10, 11, 12, 13 };

        RtpPacketizationResult packetization = packetizer.PacketizeRtpPayload(media, 2);
        RtpPacket first = new RtpPacket();
        first.Payload = (byte[])packetization.Payloads[0].Clone();
        RtpPacket second = new RtpPacket();
        second.Payload = (byte[])packetization.Payloads[1].Clone();
        RtpPacket[] packets = new RtpPacket[] { first, second };
        RtpDepacketizationResult depacketization = depacketizer.DepacketizeRtpPayload(packets);
        RtpRepairRequest request = new RtpRepairRequest();
        request.Kind = RtpRepairKind.ApplicationDefined;
        request.SequenceNumbers = new ushort[] { 7 };
        RtpRepairResult encodedRepair = repairEncoder.EncodeRtpRepair(request, packets);
        RtpRepairResult decodedRepair = repairDecoder.DecodeRtpRepair(
            request,
            encodedRepair.Data);
        media[0] = 99;
        first.Payload[0] = 98;
        encodedRepair.Data[0] = 97;

        Assert.Multiple(() =>
        {
            Assert.That(packetization.Success, Is.True);
            Assert.That(packetization.ConsumedCount, Is.EqualTo(4));
            Assert.That(packetization.Payloads[0], Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(packetization.Payloads[1], Is.EqualTo(new byte[] { 12, 13 }));
            Assert.That(packetization.Markers, Is.EqualTo(new bool[] { false, true }));
            Assert.That(depacketization.Success, Is.True);
            Assert.That(depacketization.Payload, Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
            Assert.That(encodedRepair.Kind, Is.EqualTo(RtpRepairKind.ApplicationDefined));
            Assert.That(decodedRepair.Success, Is.True);
            Assert.That(decodedRepair.RepairedPackets[0].Payload,
                Is.EqualTo(new byte[] { 10, 11, 12, 13 }));
        });
    }

    private class TestPacketCodec : IRtpPacketCodec
    {
        public int GetRtpPacketEncodedSize(RtpPacket packet)
        {
            return packet.Payload.Length;
        }

        public int EncodeRtpPacket(RtpPacket packet, Span<byte> destination)
        {
            packet.Payload.CopyTo(destination);
            return packet.Payload.Length;
        }

        public RtpCodecResult TryEncodeRtpPacket(RtpPacket packet, Span<byte> destination)
        {
            RtpCodecResult result = new RtpCodecResult();
            if (destination.Length < packet.Payload.Length)
            {
                result.ErrorMessage = "capacity";
                return result;
            }

            packet.Payload.CopyTo(destination);
            result.Success = true;
            result.WrittenCount = packet.Payload.Length;
            return result;
        }

        public byte[] EncodeRtpPacketToArray(RtpPacket packet)
        {
            return (byte[])packet.Payload.Clone();
        }

        public RtpPacket DecodeRtpPacket(ReadOnlySpan<byte> source)
        {
            RtpPacket packet = new RtpPacket();
            packet.Payload = source.ToArray();
            return packet;
        }

        public RtpCodecResult TryDecodeRtpPacket(ReadOnlySpan<byte> source)
        {
            RtpCodecResult result = new RtpCodecResult();
            result.Success = true;
            result.ConsumedCount = source.Length;
            result.Packet = DecodeRtpPacket(source);
            return result;
        }
    }

    private class TestMediaPlugin :
        IRtpPayloadPacketizer,
        IRtpPayloadDepacketizer,
        IRtpRepairEncoder,
        IRtpRepairDecoder
    {
        public RtpPacketizationResult PacketizeRtpPayload(
            ReadOnlySpan<byte> mediaData,
            int maximumPayloadSize)
        {
            RtpPacketizationResult result = new RtpPacketizationResult();
            result.Success = true;
            result.ConsumedCount = mediaData.Length;
            result.Payloads = new byte[][]
            {
                mediaData.Slice(0, maximumPayloadSize).ToArray(),
                mediaData.Slice(maximumPayloadSize).ToArray()
            };
            result.Markers = new bool[] { false, true };
            return result;
        }

        public RtpDepacketizationResult DepacketizeRtpPayload(RtpPacket[] packets)
        {
            RtpDepacketizationResult result = new RtpDepacketizationResult();
            result.Success = true;
            result.ConsumedPacketCount = packets.Length;
            result.Payload = new byte[packets[0].Payload.Length + packets[1].Payload.Length];
            packets[0].Payload.CopyTo(result.Payload, 0);
            packets[1].Payload.CopyTo(result.Payload, packets[0].Payload.Length);
            return result;
        }

        public RtpRepairResult EncodeRtpRepair(
            RtpRepairRequest request,
            RtpPacket[] sourcePackets)
        {
            RtpRepairResult result = new RtpRepairResult();
            result.Success = true;
            result.Kind = request.Kind;
            result.Data = new byte[sourcePackets[0].Payload.Length + sourcePackets[1].Payload.Length];
            sourcePackets[0].Payload.CopyTo(result.Data, 0);
            sourcePackets[1].Payload.CopyTo(result.Data, sourcePackets[0].Payload.Length);
            return result;
        }

        public RtpRepairResult DecodeRtpRepair(
            RtpRepairRequest request,
            ReadOnlySpan<byte> repairData)
        {
            RtpPacket packet = new RtpPacket();
            packet.Payload = repairData.ToArray();
            RtpRepairResult result = new RtpRepairResult();
            result.Success = true;
            result.Kind = request.Kind;
            result.RepairedPackets = new RtpPacket[] { packet };
            return result;
        }
    }
}
