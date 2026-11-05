using System.Security.Cryptography;

namespace rtp;

/// <summary>
/// Provides cryptographically secure RTP identifiers and RTCP interval randomness.
/// </summary>
public class SecureRtpRandomSource : IRtpRandomSource
{
    /// <summary>
    /// Initializes a cryptographically secure RTP random source.
    /// </summary>
    public SecureRtpRandomSource()
    {
    }

    /// <summary>Creates a uniformly distributed 32-bit synchronization source value.</summary>
    /// <returns>The new SSRC value.</returns>
    public uint CreateSynchronizationSource()
    {
        byte[] bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        return NetworkBinaryReader.ReadUInt32(bytes, 0);
    }

    /// <summary>Creates a uniformly distributed 16-bit initial sequence number.</summary>
    /// <returns>The new RTP sequence number.</returns>
    public ushort CreateSequenceNumber()
    {
        byte[] bytes = new byte[2];
        RandomNumberGenerator.Fill(bytes);
        return NetworkBinaryReader.ReadUInt16(bytes, 0);
    }

    /// <summary>Creates a uniformly distributed 32-bit initial RTP timestamp.</summary>
    /// <returns>The new RTP timestamp.</returns>
    public uint CreateTimestamp()
    {
        byte[] bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        return NetworkBinaryReader.ReadUInt32(bytes, 0);
    }

    /// <summary>Creates an unbiased 53-bit random fraction from zero up to one.</summary>
    /// <returns>A uniformly distributed value greater than or equal to zero and less than one.</returns>
    public double CreateUnitIntervalValue()
    {
        byte[] bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        ulong randomBits = NetworkBinaryReader.ReadUInt64(bytes, 0) >> 11;
        return (double)randomBits / 9007199254740992.0;
    }
}
