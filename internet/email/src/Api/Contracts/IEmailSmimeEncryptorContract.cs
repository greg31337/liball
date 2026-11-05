using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeEncryptor"/>.
/// </summary>
public interface IEmailSmimeEncryptorContract
{
    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <returns>The S/MIME encryption result produced by the operation.</returns>
    EmailSmimeEncryptionResult? EncryptWithContentAndRecipients(ReadOnlySpan<byte> content, List<X509Certificate2> recipients);
    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME encryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailSmimeEncryptionResult? EncryptWithContentAndRecipientsAndOptions(ReadOnlySpan<byte> content, List<X509Certificate2> recipients, EmailSmimeEncryptionOptions options);
}
