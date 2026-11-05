using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a list unsubscribe post.
/// </summary>
public class EmailListUnsubscribePost : IEmailListUnsubscribePostContract
{
    bool oneClick;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new list unsubscribe post.
    /// </summary>
    /// <param name = "oneClick">Whether to enable the one click behavior.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailListUnsubscribePost(bool oneClick, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.oneClick = oneClick;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Determines whether the current value or state is one click.
    /// </summary>
    /// <returns><c>true</c> when the value or state is one click; otherwise, <c>false</c>.</returns>
    public bool IsOneClick()
    {
        return oneClick;
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
