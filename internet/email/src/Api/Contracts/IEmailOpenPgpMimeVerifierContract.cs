using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpMimeVerifier"/>.
/// </summary>
public interface IEmailOpenPgpMimeVerifierContract
{
    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    EmailOpenPgpVerificationResult VerifyWithRootAndKeys(MimeEntity root, List<EmailOpenPgpVerificationKey> keys);
    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpVerificationResult VerifyWithRootAndKeysAndOptions(MimeEntity root, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options);
    /// <summary>
    /// Verifies the cleartext.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    EmailOpenPgpVerificationResult VerifyCleartextWithInputAndKeys(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys);
    /// <summary>
    /// Verifies the cleartext.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpVerificationResult VerifyCleartextWithInputAndKeysAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options);
    /// <summary>
    /// Verifies the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    EmailOpenPgpVerificationResult VerifyInlineWithInputAndKeys(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys);
    /// <summary>
    /// Verifies the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpVerificationResult VerifyInlineWithInputAndKeysAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options);
}
