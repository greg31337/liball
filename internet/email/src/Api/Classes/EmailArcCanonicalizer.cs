using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with ARC canonicalizer.
/// </summary>
public class EmailArcCanonicalizer : IEmailArcCanonicalizerContract
{
    internal static EmailDkimHeaderCanonicalizationResult CanonicalizeMessageSignatureImplementationWithHeadersAndSignatureAndSignatureHeader(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader)
    {
        return CanonicalizeMessageSignatureImplementationWithHeadersAndSignatureAndSignatureHeaderAndOptions(headers, signature, signatureHeader, new EmailDkimHeaderCanonicalizationOptions());
    }

    /// <summary>
    /// Determines whether the instance can onicalize message signature.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    public EmailDkimHeaderCanonicalizationResult CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeader(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader)
    {
        return EmailArcCanonicalizer.CanonicalizeMessageSignatureImplementationWithHeadersAndSignatureAndSignatureHeader(headers, signature, signatureHeader);
    }

    internal static EmailDkimHeaderCanonicalizationResult CanonicalizeMessageSignatureImplementationWithHeadersAndSignatureAndSignatureHeaderAndOptions(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        if (signature == null)
            throw new ArgumentNullException("signature");
        if (!signature.IsArcMessageSignature())
            throw new ArgumentException("ARC message canonicalization requires an ARC-Message-Signature value.", "signature");
        return new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeaderAndOptions(headers, signature, signatureHeader, options);
    }

    /// <summary>
    /// Determines whether the instance can onicalize message signature.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "signatureHeader">The signature header used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM header canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimHeaderCanonicalizationResult CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeaderAndOptions(EmailHeaders headers, EmailDkimSignature signature, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        return EmailArcCanonicalizer.CanonicalizeMessageSignatureImplementationWithHeadersAndSignatureAndSignatureHeaderAndOptions(headers, signature, signatureHeader, options);
    }

    internal static EmailArcSealCanonicalizationResult CanonicalizeSealImplementationWithCollectionAndSealInstance(EmailArcSetCollectionResult collection, int sealInstance)
    {
        return CanonicalizeSealImplementationWithCollectionAndSealInstanceAndOptions(collection, sealInstance, new EmailArcSealCanonicalizationOptions());
    }

    /// <summary>
    /// Determines whether the instance can onicalize seal.
    /// </summary>
    /// <param name = "collection">The collection used by the operation.</param>
    /// <param name = "sealInstance">The seal instance used by the operation.</param>
    /// <returns>The ARC seal canonicalization result produced by the operation.</returns>
    public EmailArcSealCanonicalizationResult CanonicalizeSealWithCollectionAndSealInstance(EmailArcSetCollectionResult collection, int sealInstance)
    {
        return EmailArcCanonicalizer.CanonicalizeSealImplementationWithCollectionAndSealInstance(collection, sealInstance);
    }

    internal static EmailArcSealCanonicalizationResult CanonicalizeSealImplementationWithCollectionAndSealInstanceAndOptions(EmailArcSetCollectionResult collection, int sealInstance, EmailArcSealCanonicalizationOptions options)
    {
        if (collection == null)
            throw new ArgumentNullException("collection");
        if (options == null)
            throw new ArgumentNullException("options");
        if (sealInstance < 1 || sealInstance > 50)
            throw new ArgumentOutOfRangeException("sealInstance");
        EmailArcSet? target = collection.GetSetByInstance(sealInstance);
        if (target == null || !target.IsComplete())
            throw new ArgumentException("The requested ARC set is missing or incomplete.", "sealInstance");
        EmailArcSealCanonicalizationOptions snapshot = options.Clone();
        bool failedChainScope = target.GetDeclaredChainStatus() == EmailArcChainStatus.Fail;
        int firstInstance = failedChainScope ? sealInstance : 1;
        List<byte> output = new List<byte>();
        List<EmailArcSealHeaderSelection> selections = new List<EmailArcSealHeaderSelection>();
        long sourceByteCount = 0;
        int instance = firstInstance;
        while (instance <= sealInstance)
        {
            EmailArcSet? set = collection.GetSetByInstance(instance);
            if (set == null || !set.IsComplete())
                throw new ArgumentException("The ARC seal scope contains a missing or incomplete set.", "collection");
            sourceByteCount = AddField(output, selections, set.GetAuthenticationResultsField(0), false, true, sourceByteCount, snapshot);
            sourceByteCount = AddField(output, selections, set.GetMessageSignatureField(0), false, true, sourceByteCount, snapshot);
            bool targetSeal = instance == sealInstance;
            sourceByteCount = AddField(output, selections, set.GetSealField(0), targetSeal, !targetSeal, sourceByteCount, snapshot);
            instance++;
        }

        return new EmailArcSealCanonicalizationResult(sealInstance, output.ToArray(), selections, sourceByteCount, failedChainScope);
    }

    /// <summary>
    /// Determines whether the instance can onicalize seal.
    /// </summary>
    /// <param name = "collection">The collection used by the operation.</param>
    /// <param name = "sealInstance">The seal instance used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC seal canonicalization result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSealCanonicalizationResult CanonicalizeSealWithCollectionAndSealInstanceAndOptions(EmailArcSetCollectionResult collection, int sealInstance, EmailArcSealCanonicalizationOptions options)
    {
        return EmailArcCanonicalizer.CanonicalizeSealImplementationWithCollectionAndSealInstanceAndOptions(collection, sealInstance, options);
    }

    static long AddField(List<byte> output, List<EmailArcSealHeaderSelection> selections, EmailArcHeaderField field, bool blankSignatureValue, bool appendLineEnding, long sourceByteCount, EmailArcSealCanonicalizationOptions options)
    {
        if (selections.Count >= options.GetMaximumHeaderCount())
            throw new EmailLimitException("MaxArcSealHeaderCount", options.GetMaximumHeaderCount(), selections.Count + 1);
        EmailHeader header = field.GetHeader();
        byte[] raw = header.GetRawFieldBytes();
        if (raw.LongLength > options.GetMaximumInputBytes() - sourceByteCount)
            throw new EmailLimitException("MaxArcSealHeaderInputBytes", options.GetMaximumInputBytes(), sourceByteCount + raw.LongLength);
        byte[] canonical = EmailDkimHeaderCanonicalizer.CanonicalizeArcField(header, blankSignatureValue, appendLineEnding);
        if (canonical.LongLength > options.GetMaximumCanonicalizedBytes() - output.Count)
            throw new EmailLimitException("MaxArcSealCanonicalizedHeaderBytes", options.GetMaximumCanonicalizedBytes(), (long)output.Count + canonical.LongLength);
        output.AddRange(canonical);
        selections.Add(new EmailArcSealHeaderSelection(field.GetInstance(), field.GetKind(), field.GetSourceIndex(), header, blankSignatureValue));
        return sourceByteCount + raw.LongLength;
    }
}
