using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpMimeSigner"/>.
/// </summary>
public interface IEmailOpenPgpMimeSignerContract
{
    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    EmailOpenPgpSigningResult SignWithContentAndCredential(MimeEntity content, EmailOpenPgpSigningCredential credential);
    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpSigningResult SignWithContentAndCredentialAndOptions(MimeEntity content, EmailOpenPgpSigningCredential credential, EmailOpenPgpMimeOptions options);
    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    EmailOpenPgpSigningResult SignWithCanonicalMimeEntityAndCredential(ReadOnlySpan<byte> canonicalMimeEntity, EmailOpenPgpSigningCredential credential);
    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailOpenPgpSigningResult SignWithCanonicalMimeEntityAndCredentialAndOptions(ReadOnlySpan<byte> canonicalMimeEntity, EmailOpenPgpSigningCredential credential, EmailOpenPgpMimeOptions options);
}
