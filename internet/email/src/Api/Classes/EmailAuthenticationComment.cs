using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an authentication comment.
/// </summary>
public class EmailAuthenticationComment : IEmailAuthenticationCommentContract
{
    string value;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new authentication comment.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAuthenticationComment(string value, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.value = value;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
