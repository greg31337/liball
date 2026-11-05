using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with MIME defaults.
/// </summary>
public class EmailMimeDefaults : IEmailMimeDefaultsContract
{
    /// <summary>
    /// The default transfer encoding name used when the caller does not provide an override.
    /// </summary>
    internal const string DefaultTransferEncodingName = "7bit";
    internal static EmailMediaType CreateDefaultMediaTypeImplementationWith()
    {
        return new EmailMediaTypeOperations().CreateDefaultWith();
    }

    /// <summary>
    /// Creates a default media type configured for the requested behavior.
    /// </summary>
    /// <returns>The media type produced by the operation.</returns>
    public EmailMediaType CreateDefaultMediaTypeWith()
    {
        return EmailMimeDefaults.CreateDefaultMediaTypeImplementationWith();
    }

    internal static EmailMediaType CreateDefaultMediaTypeImplementationWithParentMediaType(EmailMediaType? parentMediaType)
    {
        if (UsesDigestChildDefaultImplementation(parentMediaType))
        {
            return CreateMessageMediaTypeImplementation(true);
        }

        return new EmailMediaTypeOperations().CreateDefaultWith();
    }

    /// <summary>
    /// Creates a default media type configured for the requested behavior.
    /// </summary>
    /// <param name = "parentMediaType">The parent media type used by the operation.</param>
    /// <returns>The media type produced by the operation.</returns>
    public EmailMediaType CreateDefaultMediaTypeWithParentMediaType(EmailMediaType? parentMediaType)
    {
        return EmailMimeDefaults.CreateDefaultMediaTypeImplementationWithParentMediaType(parentMediaType);
    }

    internal static MimeEntityKind GetDefaultEntityKindImplementation(EmailMediaType parentMediaType)
    {
        return UsesDigestChildDefaultImplementation(parentMediaType) ? MimeEntityKind.Message : MimeEntityKind.Leaf;
    }

    /// <summary>
    /// Gets the default entity kind.
    /// </summary>
    /// <param name = "parentMediaType">The parent media type used by the operation.</param>
    /// <returns>The current default entity kind.</returns>
    public MimeEntityKind GetDefaultEntityKind(EmailMediaType parentMediaType)
    {
        return EmailMimeDefaults.GetDefaultEntityKindImplementation(parentMediaType);
    }

    internal static bool UsesDigestChildDefaultImplementation(EmailMediaType? parentMediaType)
    {
        return parentMediaType != null && parentMediaType.IsType("multipart", "digest");
    }

    /// <summary>
    /// Determines whether the current value or state uses digest child default.
    /// </summary>
    /// <param name = "parentMediaType">The parent media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state uses digest child default; otherwise, <c>false</c>.</returns>
    public bool UsesDigestChildDefault(EmailMediaType? parentMediaType)
    {
        return EmailMimeDefaults.UsesDigestChildDefaultImplementation(parentMediaType);
    }

    internal static string GetDefaultTransferEncodingNameImplementation()
    {
        return DefaultTransferEncodingName;
    }

    /// <summary>
    /// Gets the default transfer encoding name.
    /// </summary>
    /// <returns>The current default transfer encoding name.</returns>
    public string GetDefaultTransferEncodingName()
    {
        return EmailMimeDefaults.GetDefaultTransferEncodingNameImplementation();
    }

    internal static EmailTransferEncodingKind GetDefaultTransferEncodingKindImplementation()
    {
        return EmailTransferEncodingKind.SevenBit;
    }

    /// <summary>
    /// Gets the default transfer encoding kind.
    /// </summary>
    /// <returns>The current default transfer encoding kind.</returns>
    public EmailTransferEncodingKind GetDefaultTransferEncodingKind()
    {
        return EmailMimeDefaults.GetDefaultTransferEncodingKindImplementation();
    }

    internal static string GetEffectiveTextCharsetImplementation(EmailMediaType? mediaType)
    {
        if (mediaType == null)
        {
            throw new ArgumentNullException("mediaType");
        }

        if (!string.Equals(mediaType.GetTopLevelType(), "text", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only text media types have an effective MIME charset.");
        }

        EmailRfc2231DecodedParameter? decoded = mediaType.GetParameters().GetDecoded("charset");
        if (decoded != null)
        {
            return Encoding.ASCII.GetString(decoded.GetDecodedBytes());
        }

        EmailParameter? physical = mediaType.GetParameters().GetFirst("charset");
        if (physical != null)
        {
            return Encoding.ASCII.GetString(physical.GetValueBytes());
        }

        return "us-ascii";
    }

    /// <summary>
    /// Gets the effective text charset.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The current effective text charset.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public string GetEffectiveTextCharset(EmailMediaType? mediaType)
    {
        return EmailMimeDefaults.GetEffectiveTextCharsetImplementation(mediaType);
    }

    internal static EmailMediaType CreateMessageMediaTypeImplementation(bool defaulted)
    {
        return new EmailMediaType("message", "rfc822", EmptyParameters(), new byte[0], new EmailSourcePosition(0, 1, 1), defaulted);
    }

    /// <summary>
    /// Creates a message media type configured for the requested behavior.
    /// </summary>
    /// <param name = "defaulted">Whether to enable the defaulted behavior.</param>
    /// <returns>The media type produced by the operation.</returns>
    public EmailMediaType CreateMessageMediaType(bool defaulted)
    {
        return EmailMimeDefaults.CreateMessageMediaTypeImplementation(defaulted);
    }

    static EmailParameterList EmptyParameters()
    {
        return new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>());
    }
}
