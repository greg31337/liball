using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a DKIM header selection.
/// </summary>
public class EmailDkimHeaderSelection : IEmailDkimHeaderSelectionContract
{
    string requestedName;
    int sourceIndex;
    EmailHeader? header;
    /// <summary>
    /// Creates a new DKIM header selection.
    /// </summary>
    /// <param name = "requestedName">The requested name used by the operation.</param>
    /// <param name = "sourceIndex">The zero-based source index.</param>
    /// <param name = "header">The header used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimHeaderSelection(string requestedName, int sourceIndex, EmailHeader? header)
    {
        if (requestedName == null)
            throw new ArgumentNullException("requestedName");
        if (sourceIndex < -1)
            throw new ArgumentOutOfRangeException("sourceIndex");
        if (sourceIndex == -1 && header != null || sourceIndex >= 0 && header == null)
            throw new ArgumentException("The source index and selected header must either both be present or both be absent.", "header");
        this.requestedName = requestedName;
        this.sourceIndex = sourceIndex;
        this.header = header;
    }

    /// <summary>
    /// Gets the requested name.
    /// </summary>
    /// <returns>The current requested name.</returns>
    public string GetRequestedName()
    {
        return requestedName;
    }

    /// <summary>
    /// Determines whether the header is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header is available; otherwise, <c>false</c>.</returns>
    public bool HasHeader()
    {
        return header != null;
    }

    /// <summary>
    /// Gets the source index.
    /// </summary>
    /// <returns>The current source index.</returns>
    public int GetSourceIndex()
    {
        return sourceIndex;
    }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    public EmailHeader? GetHeader()
    {
        return header;
    }
}
