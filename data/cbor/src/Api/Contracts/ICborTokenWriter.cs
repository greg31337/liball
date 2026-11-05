using System;
using System.Buffers;

namespace cbor;

/// <summary>
/// Writes low-level CBOR tokens while enforcing container state.
/// </summary>
public interface ICborTokenWriter
{
    /// <summary>Writes one token into the supplied destination.</summary>
    /// <param name="token">The token to write.</param>
    /// <param name="destination">The destination byte array.</param>
    /// <returns>The write outcome and size information.</returns>
    CborWriteResult WriteToken(CborToken token, byte[] destination);

    /// <summary>Writes one token into the supplied span.</summary>
    /// <param name="token">The token to write.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The write outcome and size information.</returns>
    CborWriteResult WriteTokenToSpan(CborToken token, Span<byte> destination);

    /// <summary>Writes one token into the supplied growable buffer.</summary>
    /// <param name="token">The token to write.</param>
    /// <param name="destination">The destination buffer writer.</param>
    /// <returns>The write outcome and size information.</returns>
    CborWriteResult WriteTokenToBuffer(CborToken token, IBufferWriter<byte> destination);

    /// <summary>Completes the current token stream and verifies container counts.</summary>
    /// <param name="destination">The destination for any final encoded bytes.</param>
    /// <returns>The completion outcome and size information.</returns>
    CborWriteResult CompleteWriter(byte[] destination);

    /// <summary>Resets the writer to its initial state.</summary>
    void ResetWriter();
}
