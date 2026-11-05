using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMimeDefaults"/>.
/// </summary>
public interface IEmailMimeDefaultsContract
{
    /// <summary>
    /// Creates a default media type configured for the requested behavior.
    /// </summary>
    /// <returns>The media type produced by the operation.</returns>
    EmailMediaType CreateDefaultMediaTypeWith();
    /// <summary>
    /// Creates a default media type configured for the requested behavior.
    /// </summary>
    /// <param name = "parentMediaType">The parent media type used by the operation.</param>
    /// <returns>The media type produced by the operation.</returns>
    EmailMediaType CreateDefaultMediaTypeWithParentMediaType(EmailMediaType? parentMediaType);
    /// <summary>
    /// Gets the default entity kind.
    /// </summary>
    /// <param name = "parentMediaType">The parent media type used by the operation.</param>
    /// <returns>The current default entity kind.</returns>
    MimeEntityKind GetDefaultEntityKind(EmailMediaType parentMediaType);
    /// <summary>
    /// Determines whether the current value or state uses digest child default.
    /// </summary>
    /// <param name = "parentMediaType">The parent media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state uses digest child default; otherwise, <c>false</c>.</returns>
    bool UsesDigestChildDefault(EmailMediaType? parentMediaType);
    /// <summary>
    /// Gets the default transfer encoding name.
    /// </summary>
    /// <returns>The current default transfer encoding name.</returns>
    string GetDefaultTransferEncodingName();
    /// <summary>
    /// Gets the default transfer encoding kind.
    /// </summary>
    /// <returns>The current default transfer encoding kind.</returns>
    EmailTransferEncodingKind GetDefaultTransferEncodingKind();
    /// <summary>
    /// Gets the effective text charset.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The current effective text charset.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    string GetEffectiveTextCharset(EmailMediaType? mediaType);
    /// <summary>
    /// Creates a message media type configured for the requested behavior.
    /// </summary>
    /// <param name = "defaulted">Whether to enable the defaulted behavior.</param>
    /// <returns>The media type produced by the operation.</returns>
    EmailMediaType CreateMessageMediaType(bool defaulted);
}
