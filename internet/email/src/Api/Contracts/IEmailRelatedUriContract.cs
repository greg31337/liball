using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRelatedUri"/>.
/// </summary>
public interface IEmailRelatedUriContract
{
    /// <summary>
    /// Normalizes the content ID.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string NormalizeContentIdWithValue(string value);
    /// <summary>
    /// Normalizes the content ID.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string NormalizeContentIdWithIdentifier(EmailMessageId? identifier);
    /// <summary>
    /// Normalizes the location.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string NormalizeLocation(string? value);
    /// <summary>
    /// Determines whether the current value or state is absolute.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is absolute; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool IsAbsolute(string value);
    /// <summary>
    /// Resolves the location.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "baseLocation">The base location used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string ResolveLocation(string value, string? baseLocation);
}
