using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with DKIM canonicalizer.
/// </summary>
public class EmailDkimCanonicalizer : IEmailDkimCanonicalizerContract
{
    internal static EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimCanonicalizationKind kind)
    {
        EmailDkimHeaderCanonicalizationOptions options = new EmailDkimHeaderCanonicalizationOptions();
        options.SetKind(kind);
        return CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, signedHeaderNames, signatureHeader, options);
    }

    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signedHeaderNames">The signed header names used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    public EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimCanonicalizationKind kind)
    {
        return EmailDkimCanonicalizer.CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndKind(headers, signedHeaderNames, signatureHeader, kind);
    }

    internal static EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        return EmailDkimHeaderCanonicalizer.Canonicalize(headers, signedHeaderNames, signatureHeader, options);
    }

    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signedHeaderNames">The signed header names used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    public EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        return EmailDkimCanonicalizer.CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, signedHeaderNames, signatureHeader, options);
    }

    internal static EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersImplementationWithHeadersAndSignatureAndSignatureHeader(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader)
    {
        if (signature == null)
            throw new ArgumentNullException("signature");
        EmailDkimHeaderCanonicalizationOptions options = new EmailDkimHeaderCanonicalizationOptions();
        options.SetKind(signature.GetHeaderCanonicalizationKind());
        return CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, signature.GetSignedHeaders(), signatureHeader, options);
    }

    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeader(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader)
    {
        return EmailDkimCanonicalizer.CanonicalizeHeadersImplementationWithHeadersAndSignatureAndSignatureHeader(headers, signature, signatureHeader);
    }

    internal static EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersImplementationWithHeadersAndSignatureAndSignatureHeaderAndOptions(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        if (signature == null)
            throw new ArgumentNullException("signature");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDkimHeaderCanonicalizationOptions snapshot = options.Clone();
        snapshot.SetKind(signature.GetHeaderCanonicalizationKind());
        return CanonicalizeHeadersImplementationWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, signature.GetSignedHeaders(), signatureHeader, snapshot);
    }

    /// <summary>
    /// Determines whether the instance can onicalize headers.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimHeaderCanonicalizationResult CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeaderAndOptions(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        return EmailDkimCanonicalizer.CanonicalizeHeadersImplementationWithHeadersAndSignatureAndSignatureHeaderAndOptions(headers, signature, signatureHeader, options);
    }

    internal static EmailDkimBodyCanonicalizationOutput CanonicalizeBodyImplementationWithInputAndKind(ReadOnlySpan<byte> input, EmailDkimCanonicalizationKind kind)
    {
        EmailDkimBodyCanonicalizationOptions options = new EmailDkimBodyCanonicalizationOptions();
        options.SetKind(kind);
        return CanonicalizeBodyImplementationWithInputAndOptions(input, options);
    }

    /// <summary>
    /// Determines whether the instance can onicalize body.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The DKIM body canonicalization output produced by the operation.</returns>
    public EmailDkimBodyCanonicalizationOutput CanonicalizeBodyWithInputAndKind(ReadOnlySpan<byte> input, EmailDkimCanonicalizationKind kind)
    {
        return EmailDkimCanonicalizer.CanonicalizeBodyImplementationWithInputAndKind(input, kind);
    }

    internal static EmailDkimBodyCanonicalizationOutput CanonicalizeBodyImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimBodyCanonicalizationOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDkimBodyCanonicalizer canonicalizer = new EmailDkimBodyCanonicalizer(options);
        List<byte> output = new List<byte>();
        output.AddRange(canonicalizer.Feed(input));
        output.AddRange(canonicalizer.Complete());
        return new EmailDkimBodyCanonicalizationOutput(output.ToArray(), canonicalizer.GetResult());
    }

    /// <summary>
    /// Determines whether the instance can onicalize body.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM body canonicalization output produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimBodyCanonicalizationOutput CanonicalizeBodyWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimBodyCanonicalizationOptions options)
    {
        return EmailDkimCanonicalizer.CanonicalizeBodyImplementationWithInputAndOptions(input, options);
    }
}
