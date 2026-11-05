using System;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCharsetLookupResult"/>.
/// </summary>
public interface IEmailCharsetLookupResultContract
{
    /// <summary>
    /// Determines whether the current value or state is found.
    /// </summary>
    /// <returns><c>true</c> when the value or state is found; otherwise, <c>false</c>.</returns>
    bool IsFound();
    /// <summary>
    /// Gets the requested name.
    /// </summary>
    /// <returns>The current requested name.</returns>
    string GetRequestedName();
    /// <summary>
    /// Gets the canonical name.
    /// </summary>
    /// <returns>The current canonical name.</returns>
    string? GetCanonicalName();
    /// <summary>
    /// Gets the code page.
    /// </summary>
    /// <returns>The current code page.</returns>
    int GetCodePage();
    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    Encoding? GetEncoding();
}
