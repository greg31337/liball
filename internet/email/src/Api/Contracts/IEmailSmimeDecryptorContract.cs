using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecryptor"/>.
/// </summary>
public interface IEmailSmimeDecryptorContract
{
    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    EmailSmimeDecryptionResult? DecryptWithCmsAndCredentials(ReadOnlySpan<byte> cms, List<EmailSmimeDecryptionCredential> credentials);
    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeDecryptionResult? DecryptWithCmsAndCredentialsAndOptions(ReadOnlySpan<byte> cms, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options);
    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    EmailSmimeDecryptionResult? DecryptWithEntityAndCredentials(MimeEntity entity, List<EmailSmimeDecryptionCredential> credentials);
    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailSmimeDecryptionResult? DecryptWithEntityAndCredentialsAndOptions(MimeEntity entity, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options);
}
