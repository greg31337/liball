using System;
using System.Buffers;

namespace cbor;

/// <summary>
/// Provides stateful low-level CBOR token writing.
/// </summary>
public class CborTokenWriter : ICborTokenWriter
{
    internal CborEncodeOptions Options;
    internal CborTokenWriterCore Core;

    /// <summary>Creates a token writer with encoding policies and limits.</summary>
    /// <param name="options">The encoding policies and limits.</param>
    public CborTokenWriter(CborEncodeOptions options)
    {
        Options = options;
        Core = new CborTokenWriterCore(options);
    }

    /// <summary>Writes one token into the supplied destination.</summary>
    /// <param name="token">The token to write.</param>
    /// <param name="destination">The destination byte array.</param>
    /// <returns>The write outcome and size information.</returns>
    public CborWriteResult WriteToken(CborToken token, byte[] destination)
    {
        return Core.WriteTokenToSpan(token, destination);
    }

    /// <summary>Writes one token into the supplied span.</summary>
    /// <param name="token">The token to write.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The write outcome and size information.</returns>
    public CborWriteResult WriteTokenToSpan(CborToken token, Span<byte> destination)
    {
        return Core.WriteTokenToSpan(token, destination);
    }

    /// <summary>Writes one token into the supplied growable buffer.</summary>
    /// <param name="token">The token to write.</param>
    /// <param name="destination">The destination buffer writer.</param>
    /// <returns>The write outcome and size information.</returns>
    public CborWriteResult WriteTokenToBuffer(
        CborToken token,
        IBufferWriter<byte> destination)
    {
        return Core.WriteTokenToBuffer(token, destination);
    }

    /// <summary>Completes the current token stream and verifies container counts.</summary>
    /// <param name="destination">The destination for any final encoded bytes.</param>
    /// <returns>The completion outcome and size information.</returns>
    public CborWriteResult CompleteWriter(byte[] destination)
    {
        return Core.CompleteWriter(destination);
    }

    /// <summary>Resets the writer to its initial state.</summary>
    public void ResetWriter()
    {
        Core.Reset();
    }
}
