using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodedWordEncodeResult"/>.
/// </summary>
public interface IEmailEncodedWordEncodeResultContract
{
    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    byte[] GetBytes();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    EmailEncodedWordEncoding GetEncoding();
    /// <summary>
    /// Gets the encoded word count.
    /// </summary>
    /// <returns>The current encoded word count.</returns>
    int GetEncodedWordCount();
    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    int GetFoldCount();
    /// <summary>
    /// Gets the final line column.
    /// </summary>
    /// <returns>The current final line column.</returns>
    int GetFinalLineColumn();
}
