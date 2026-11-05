using System;
using System.Text;

namespace email;
/// <summary>
/// Describes the outcome of a charset lookup operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailCharsetLookupResult : IEmailCharsetLookupResultContract
{
    bool found;
    string requestedName;
    string? canonicalName;
    Encoding? encoding;
    /// <summary>
    /// Creates a new charset lookup result.
    /// </summary>
    /// <param name = "found">Whether to enable the found behavior.</param>
    /// <param name = "requestedName">The requested name used by the operation.</param>
    /// <param name = "canonicalName">The canonical name used by the operation.</param>
    /// <param name = "encoding">The encoding used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCharsetLookupResult(bool found, string requestedName, string? canonicalName, Encoding? encoding)
    {
        if (requestedName == null)
        {
            throw new ArgumentNullException("requestedName");
        }

        if (found && canonicalName == null)
        {
            throw new ArgumentNullException("canonicalName");
        }

        if (found && encoding == null)
        {
            throw new ArgumentNullException("encoding");
        }

        if (!found && (canonicalName != null || encoding != null))
        {
            throw new ArgumentException("A missing charset lookup cannot contain resolved data.", "found");
        }

        this.found = found;
        this.requestedName = requestedName;
        this.canonicalName = canonicalName;
        if (encoding != null)
        {
            this.encoding = (Encoding)encoding.Clone();
        }
        else
        {
            this.encoding = null;
        }
    }

    /// <summary>
    /// Determines whether the current value or state is found.
    /// </summary>
    /// <returns><c>true</c> when the value or state is found; otherwise, <c>false</c>.</returns>
    public bool IsFound()
    {
        return found;
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
    /// Gets the canonical name.
    /// </summary>
    /// <returns>The current canonical name.</returns>
    public string? GetCanonicalName()
    {
        return canonicalName;
    }

    /// <summary>
    /// Gets the code page.
    /// </summary>
    /// <returns>The current code page.</returns>
    public int GetCodePage()
    {
        if (!found)
        {
            return -1;
        }

        return (encoding ?? throw new InvalidOperationException()).CodePage;
    }

    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    public Encoding? GetEncoding()
    {
        if (!found)
        {
            return null;
        }

        return (Encoding)(encoding ?? throw new InvalidOperationException()).Clone();
    }
}
