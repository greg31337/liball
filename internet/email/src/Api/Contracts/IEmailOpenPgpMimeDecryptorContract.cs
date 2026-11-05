using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpMimeDecryptor"/>.
/// </summary>
public interface IEmailOpenPgpMimeDecryptorContract
{
    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    EmailOpenPgpDecryptionResult DecryptWithRootAndCredentials(MimeEntity root, List<EmailOpenPgpDecryptionCredential> credentials);
    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpDecryptionResult DecryptWithRootAndCredentialsAndOptions(MimeEntity root, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options);
    /// <summary>
    /// Decrypts the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    EmailOpenPgpDecryptionResult DecryptInlineWithInputAndCredentials(ReadOnlySpan<byte> input, List<EmailOpenPgpDecryptionCredential> credentials);
    /// <summary>
    /// Decrypts the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpDecryptionResult DecryptInlineWithInputAndCredentialsAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpDecryptionCredential> credentials, EmailOpenPgpMimeOptions options);
}
