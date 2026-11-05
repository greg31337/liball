using System;

namespace email;
/// <summary>
/// Provides operations for working with openPGP MIME signer.
/// </summary>
public class EmailOpenPgpMimeSigner : IEmailOpenPgpMimeSignerContract
{
    internal static EmailOpenPgpSigningResult SignImplementationWithContentAndCredential(MimeEntity content, EmailOpenPgpSigningCredential credential)
    {
        return SignImplementationWithContentAndCredentialAndOptions(content, credential, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    public EmailOpenPgpSigningResult SignWithContentAndCredential(MimeEntity content, EmailOpenPgpSigningCredential credential)
    {
        return EmailOpenPgpMimeSigner.SignImplementationWithContentAndCredential(content, credential);
    }

    internal static EmailOpenPgpSigningResult SignImplementationWithContentAndCredentialAndOptions(MimeEntity content, EmailOpenPgpSigningCredential credential, EmailOpenPgpMimeOptions options)
    {
        if (content == null)
            throw new ArgumentNullException("content");
        if (options == null)
            throw new ArgumentNullException("options");
        byte[]? canonical = EmailOpenPgpMimeUtility.CanonicalEntity(content, options.GetLimits());
        if (canonical == null)
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(EmailOpenPgpMimeUtility.Issue(EmailIssueCode.OpenPgpSigningFailed, "The MIME entity could not be serialized canonically.", "No PGP/MIME signature was emitted."));
            return new EmailOpenPgpSigningResult(new byte[0], new byte[0], issues);
        }

        return SignImplementationWithCanonicalMimeEntityAndCredentialAndOptions(canonical, credential, options);
    }

    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpSigningResult SignWithContentAndCredentialAndOptions(MimeEntity content, EmailOpenPgpSigningCredential credential, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeSigner.SignImplementationWithContentAndCredentialAndOptions(content, credential, options);
    }

    internal static EmailOpenPgpSigningResult SignImplementationWithCanonicalMimeEntityAndCredential(ReadOnlySpan<byte> canonicalMimeEntity, EmailOpenPgpSigningCredential credential)
    {
        return SignImplementationWithCanonicalMimeEntityAndCredentialAndOptions(canonicalMimeEntity, credential, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    public EmailOpenPgpSigningResult SignWithCanonicalMimeEntityAndCredential(ReadOnlySpan<byte> canonicalMimeEntity, EmailOpenPgpSigningCredential credential)
    {
        return EmailOpenPgpMimeSigner.SignImplementationWithCanonicalMimeEntityAndCredential(canonicalMimeEntity, credential);
    }

    internal static EmailOpenPgpSigningResult SignImplementationWithCanonicalMimeEntityAndCredentialAndOptions(ReadOnlySpan<byte> canonicalMimeEntity, EmailOpenPgpSigningCredential credential, EmailOpenPgpMimeOptions options)
    {
        if (credential == null)
            throw new ArgumentNullException("credential");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        EmailOpenPgpMimeUtility.Bound(canonicalMimeEntity.Length, limits.GetMaxInputBytes(), "MaxOpenPgpMimeContentBytes");
        EmailCryptographicWorkBudget work = new EmailCryptographicWorkBudget(limits);
        work.Charge(2);
        EmailIssueList issues = new EmailIssueList();
        if (!EmailOpenPgpMimeUtility.IsSafeSignedMime(canonicalMimeEntity))
        {
            issues.Add(EmailOpenPgpMimeUtility.Issue(EmailIssueCode.OpenPgpSigningFailed, "PGP/MIME signed content must use canonical CRLF, contain only seven-bit octets, and have no trailing line whitespace.", "Apply a safe MIME content-transfer encoding before signing."));
            return new EmailOpenPgpSigningResult(new byte[0], new byte[0], issues);
        }

        uint creationTime = snapshot.HasSignatureCreationTime() ? snapshot.GetSignatureCreationTime() : (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        EmailOpenPgpSignatureGeneration generated = EmailOpenPgpMimeUtility.SignV4(canonicalMimeEntity, credential, creationTime, snapshot.GetAlgorithmOptions());
        EmailOpenPgpOperationResult operation = generated.GetOperation();
        byte[]? signature = generated.GetSignature();
        if (signature == null)
        {
            issues.AddRange(operation.GetIssues());
            issues.Add(EmailOpenPgpMimeUtility.Issue(EmailIssueCode.OpenPgpSigningFailed, "The OpenPGP detached-signature operation failed.", "No PGP/MIME entity was emitted."));
            operation.Dispose();
            return new EmailOpenPgpSigningResult(new byte[0], new byte[0], issues);
        }

        operation.Dispose();
        byte[] armor = new EmailOpenPgpArmor().EncodeWithDataAndKindAndIncludeChecksum(signature, EmailOpenPgpArmorKind.Signature, snapshot.GetIncludeArmorChecksum());
        byte[] mime = EmailOpenPgpMimeUtility.SignedMime(canonicalMimeEntity, armor, credential.GetHashAlgorithm());
        EmailOpenPgpMimeUtility.Bound(mime.Length, limits.GetMaxInputBytes(), "MaxOpenPgpMimeOutputBytes");
        return new EmailOpenPgpSigningResult(mime, signature, issues);
    }

    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "canonicalMimeEntity">The canonical MIME entity used by the operation.</param>
    /// <param name = "credential">The credential used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP signing result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpSigningResult SignWithCanonicalMimeEntityAndCredentialAndOptions(ReadOnlySpan<byte> canonicalMimeEntity, EmailOpenPgpSigningCredential credential, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeSigner.SignImplementationWithCanonicalMimeEntityAndCredentialAndOptions(canonicalMimeEntity, credential, options);
    }
}
