using System;

namespace tftp;

/// <summary>
/// Encodes and decodes TFTP packets.
/// </summary>
public interface ITftpPacketCodec
{
    /// <summary>
    /// Gets the number of bytes required to encode a packet.
    /// </summary>
    /// <param name="packet">The packet whose encoded length is required.</param>
    /// <returns>The encoded packet length in bytes.</returns>
    int GetEncodedLength(TftpPacket packet);

    /// <summary>
    /// Encodes a packet into a caller-provided destination.
    /// </summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="destination">The destination that receives the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    int Encode(TftpPacket packet, Span<byte> destination);

    /// <summary>
    /// Decodes a packet and throws when the source is invalid.
    /// </summary>
    /// <param name="source">The encoded packet bytes.</param>
    /// <param name="maximumDataLength">The maximum accepted DATA payload length.</param>
    /// <returns>The decoded packet.</returns>
    TftpPacket Decode(ReadOnlySpan<byte> source, int maximumDataLength);

    /// <summary>
    /// Attempts to decode a packet without reporting validation failures through exceptions.
    /// </summary>
    /// <param name="source">The encoded packet bytes.</param>
    /// <param name="maximumDataLength">The maximum accepted DATA payload length.</param>
    /// <returns>The decode outcome.</returns>
    TftpPacketDecodeResult TryDecode(ReadOnlySpan<byte> source, int maximumDataLength);
}
