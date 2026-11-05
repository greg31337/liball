using rtp;

// Use a small synthetic audio payload; this example only serializes the RTP packet.
var packet = new RtpPacket
{
    PayloadType = 0,
    SequenceNumber = 100,
    Timestamp = 16000,
    SynchronizationSource = 0x11223344,
    Marker = true,
    Payload = new byte[] { 0xFF, 0x7F, 0x80, 0x00 }
};

var codec = new RtpPacketCodec(new RtpSessionConfiguration());
byte[] wire = codec.EncodeRtpPacketToArray(packet);

// Decode the wire bytes and verify both payload content and sequence number.
var decoded = codec.DecodeRtpPacket(wire);
Console.WriteLine($"RTP version {decoded.Version}; encoded size: {wire.Length} bytes");
Console.WriteLine(
    $"Sequence: {decoded.SequenceNumber}; timestamp: {decoded.Timestamp}; SSRC: 0x{decoded.SynchronizationSource:X8}");
Console.WriteLine($"Marker: {decoded.Marker}; payload type: {decoded.PayloadType}");
Console.WriteLine($"Payload bytes: {Convert.ToHexString(decoded.Payload)}");
if (!decoded.Payload.SequenceEqual(packet.Payload) || decoded.SequenceNumber != packet.SequenceNumber)
{
    throw new InvalidOperationException("RTP fields changed.");
}
