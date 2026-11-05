using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a list command.
/// </summary>
public class EmailListCommand : IEmailListCommandContract
{
    string uri;
    byte[] rawBytes;
    byte[] rawUriBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new list command.
    /// </summary>
    /// <param name = "uri">The uri used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "rawUriBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailListCommand(string uri, byte[] rawBytes, byte[] rawUriBytes, EmailSourcePosition sourcePosition)
    {
        if (uri == null || uri.Length == 0)
        {
            throw new ArgumentException("A list command URI is required.", "uri");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (rawUriBytes == null)
        {
            throw new ArgumentNullException("rawUriBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.uri = uri;
        this.rawBytes = Copy(rawBytes);
        this.rawUriBytes = Copy(rawUriBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the URI.
    /// </summary>
    /// <returns>The current URI.</returns>
    public string GetUri()
    {
        return uri;
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
    /// Gets the raw URI bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw URI bytes.</returns>
    public byte[] GetRawUriBytes()
    {
        return Copy(rawUriBytes);
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
