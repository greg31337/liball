using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcAuthenticationResults"/>.
/// </summary>
public interface IEmailArcAuthenticationResultsContract
{
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the instance tag.
    /// </summary>
    /// <returns>The current instance tag.</returns>
    EmailSignatureTag GetInstanceTag();
    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    EmailAuthenticationResults GetAuthenticationResults();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithOptions(EmailSignatureFormatOptions options);
}
