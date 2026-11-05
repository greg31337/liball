using System.Text;
using tftp;

var codec = new TftpPacketCodec();

// Model a read request, one data block, and its acknowledgement.
var packets = new[]
{
    new TftpPacket
    {
        Opcode = TftpOpcode.ReadRequest,
        Filename = "hello.txt",
        Mode = "octet"
    },
    new TftpPacket
    {
        Opcode = TftpOpcode.Data,
        BlockNumber = 1,
        Payload = Encoding.ASCII.GetBytes("Hello TFTP!")
    },
    new TftpPacket
    {
        Opcode = TftpOpcode.Acknowledgement,
        BlockNumber = 1
    }
};

Console.WriteLine("A TFTP exchange, entirely in memory");
foreach (var packet in packets)
{
    // Allocate the exact wire size; the decoder receives only the bytes that were written.
    byte[] wire = new byte[codec.GetEncodedLength(packet)];
    int written = codec.Encode(packet, wire);
    var decoded = codec.Decode(wire.AsSpan(0, written), 512);
    string detail = decoded.Opcode switch
    {
        TftpOpcode.ReadRequest => $"file={decoded.Filename}, mode={decoded.Mode}",
        TftpOpcode.Data => $"block={decoded.BlockNumber}, data='{Encoding.ASCII.GetString(decoded.Payload)}'",
        _ => $"block={decoded.BlockNumber}"
    };

    Console.WriteLine($"{decoded.Opcode,-16} {written,2} bytes | {detail}");
    if (decoded.Opcode != packet.Opcode || !decoded.Payload.SequenceEqual(packet.Payload))
    {
        throw new InvalidOperationException("TFTP packet changed.");
    }
}
