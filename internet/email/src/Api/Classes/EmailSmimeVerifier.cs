using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Provides operations for working with S/MIME verifier.
/// </summary>
public class EmailSmimeVerifier : IEmailSmimeVerifierContract
{
    internal static EmailSmimeVerificationResult VerifyDetachedImplementationWithContentAndCmsSignature(ReadOnlySpan<byte> content, ReadOnlySpan<byte> cmsSignature)
    {
        return VerifyDetachedImplementationWithContentAndCmsSignatureAndOptions(content, cmsSignature, new EmailSmimeVerificationOptions());
    }

    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "cmsSignature">The cms signature used by the operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    public EmailSmimeVerificationResult VerifyDetachedWithContentAndCmsSignature(ReadOnlySpan<byte> content, ReadOnlySpan<byte> cmsSignature)
    {
        return EmailSmimeVerifier.VerifyDetachedImplementationWithContentAndCmsSignature(content, cmsSignature);
    }

    internal static EmailSmimeVerificationResult VerifyDetachedImplementationWithContentAndCmsSignatureAndOptions(ReadOnlySpan<byte> content, ReadOnlySpan<byte> cmsSignature, EmailSmimeVerificationOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        return Verify(content.ToArray(), cmsSignature.ToArray(), true, options);
    }

    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "cmsSignature">The cms signature used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeVerificationResult VerifyDetachedWithContentAndCmsSignatureAndOptions(ReadOnlySpan<byte> content, ReadOnlySpan<byte> cmsSignature, EmailSmimeVerificationOptions options)
    {
        return EmailSmimeVerifier.VerifyDetachedImplementationWithContentAndCmsSignatureAndOptions(content, cmsSignature, options);
    }

    internal static EmailSmimeVerificationResult VerifyDetachedImplementationWithMultipart(EmailSecurityMultipart multipart)
    {
        return VerifyDetachedImplementationWithMultipartAndOptions(multipart, new EmailSmimeVerificationOptions());
    }

    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    public EmailSmimeVerificationResult VerifyDetachedWithMultipart(EmailSecurityMultipart multipart)
    {
        return EmailSmimeVerifier.VerifyDetachedImplementationWithMultipart(multipart);
    }

    internal static EmailSmimeVerificationResult VerifyDetachedImplementationWithMultipartAndOptions(EmailSecurityMultipart multipart, EmailSmimeVerificationOptions options)
    {
        if (multipart == null)
            throw new ArgumentNullException("multipart");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailCryptographicLimits limits = options.GetLimits();
        if (!multipart.AreInvariantsSatisfied() || !multipart.IsSigned() || !multipart.HasExactSignedOctets() || !multipart.HasSecurityPart())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "Detached S/MIME verification requires exact signed octets and a signature part.", "Parse a complete multipart/signed entity with exact raw content preservation."));
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, null, true);
        }

        byte[] content = multipart.MaterializeExactSignedOctets(limits.GetMaxInputBytes());
        MimeEntity? signaturePart = multipart.GetSignaturePart();
        if (!(signaturePart ?? throw new InvalidOperationException()).HasContent())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "The detached S/MIME signature part has no content.", "Retain or decode the application/pkcs7-signature body."));
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, content, true);
        }

        using (System.IO.MemoryStream decodedSignature = new System.IO.MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(signaturePart, decodedSignature, limits.GetMaxInputBytes());
            return Verify(content, decodedSignature.ToArray(), true, options);
        }
    }

    /// <summary>
    /// Verifies the detached.
    /// </summary>
    /// <param name = "multipart">The multipart used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeVerificationResult VerifyDetachedWithMultipartAndOptions(EmailSecurityMultipart multipart, EmailSmimeVerificationOptions options)
    {
        return EmailSmimeVerifier.VerifyDetachedImplementationWithMultipartAndOptions(multipart, options);
    }

    internal static EmailSmimeVerificationResult VerifyOpaqueImplementationWithCms(ReadOnlySpan<byte> cms)
    {
        return VerifyOpaqueImplementationWithCmsAndOptions(cms, new EmailSmimeVerificationOptions());
    }

    /// <summary>
    /// Verifies the opaque.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    public EmailSmimeVerificationResult VerifyOpaqueWithCms(ReadOnlySpan<byte> cms)
    {
        return EmailSmimeVerifier.VerifyOpaqueImplementationWithCms(cms);
    }

    internal static EmailSmimeVerificationResult VerifyOpaqueImplementationWithCmsAndOptions(ReadOnlySpan<byte> cms, EmailSmimeVerificationOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        return Verify(null, cms.ToArray(), false, options);
    }

    /// <summary>
    /// Verifies the opaque.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeVerificationResult VerifyOpaqueWithCmsAndOptions(ReadOnlySpan<byte> cms, EmailSmimeVerificationOptions options)
    {
        return EmailSmimeVerifier.VerifyOpaqueImplementationWithCmsAndOptions(cms, options);
    }

    static EmailSmimeVerificationResult Verify(byte[]? externalContent, byte[] cms, bool detached, EmailSmimeVerificationOptions options)
    {
        EmailSmimeVerificationOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (cms.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsInputBytes", limits.GetMaxInputBytes(), cms.LongLength);
        if (externalContent != null && externalContent.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxSmimeContentBytes", limits.GetMaxInputBytes(), externalContent.LongLength);
        EmailIssueList issues = new EmailIssueList();
        EmailCmsReaderOptions readerOptions = new EmailCmsReaderOptions();
        readerOptions.SetLimits(limits);
        EmailCmsReadResult parsed = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(cms, readerOptions);
        if (!parsed.IsSuccess() || (parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() != EmailCmsContentKind.SignedData)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "The input is not a complete CMS SignedData ContentInfo value.", "No signer was trusted or verified."));
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, externalContent, detached);
        }

        EmailCmsAsn1Node signedData = (parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentNode();
        EmailSmimeSignedDataLayout? layout = ParseSignedData(signedData, limits, issues);
        if (layout == null)
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, externalContent, detached);
        if (detached && layout.HasEmbeddedContent() || !detached && !layout.HasEmbeddedContent())
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, detached ? "A detached signature unexpectedly contains encapsulated content." : "Opaque SignedData does not contain encapsulated content.", "Use the verification entry point that matches the CMS representation."));
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, externalContent, detached);
        }

        byte[]? content = detached ? externalContent : layout.GetEmbeddedContent();
        if (content == null)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "No signed content is available.", "Supply detached content or an opaque SignedData value with eContent."));
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, null, detached);
        }

        List<X509Certificate2>? certificates = ParseCertificates(layout.GetCertificateNode(), snapshot.CopyExtraCertificates(), limits, issues);
        if (certificates == null)
            return new EmailSmimeVerificationResult(new List<EmailSmimeSignerVerificationResult>(), issues, content, detached);
        EmailCmsAsn1Node signerSet = layout.GetSignerInfos();
        if (signerSet.GetChildCount() > limits.GetMaxSignatureCount())
            throw new EmailLimitException("MaxSignatureCount", limits.GetMaxSignatureCount(), signerSet.GetChildCount());
        if ((long)signerSet.GetChildCount() * 2 > limits.GetMaxOperationCount())
            throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), signerSet.GetChildCount() * 2);
        List<EmailSmimeSignerVerificationResult> results = new List<EmailSmimeSignerVerificationResult>();
        int index = 0;
        while (index < signerSet.GetChildCount())
        {
            results.Add(VerifySigner(signerSet.GetChildAt(index), layout, content, certificates, snapshot, issues));
            index++;
        }

        if (results.Count == 0)
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "CMS SignedData contains no signer information.", "At least one signer is required for S/MIME verification."));
        return new EmailSmimeVerificationResult(results, issues, content, detached);
    }

    static EmailSmimeSignedDataLayout? ParseSignedData(EmailCmsAsn1Node node, EmailCryptographicLimits limits, EmailIssueList issues)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() < 4)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData is not a complete SEQUENCE.", "No signer information was processed."));
            return null;
        }

        if (!node.GetChildAt(0).HasTag(EmailCmsAsn1TagClass.Universal, 2) || !node.GetChildAt(1).HasTag(EmailCmsAsn1TagClass.Universal, 17))
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData has malformed version or digestAlgorithms fields.", "No signer information was processed."));
            return null;
        }

        HashSet<string> digestOids = new HashSet<string>(StringComparer.Ordinal);
        EmailCmsAsn1Node digestSet = node.GetChildAt(1);
        int digestIndex = 0;
        while (digestIndex < digestSet.GetChildCount())
        {
            string? oid = ReadAlgorithmOid(digestSet.GetChildAt(digestIndex));
            if (oid == null)
            {
                issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData contains a malformed digest algorithm identifier.", "No signer information was processed."));
                return null;
            }

            digestOids.Add(oid);
            digestIndex++;
        }

        EmailCmsAsn1Node encap = node.GetChildAt(2);
        if (!encap.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !encap.IsConstructed() || encap.GetChildCount() < 1 || encap.GetChildCount() > 2)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData has malformed encapsulated content information.", "No signer information was processed."));
            return null;
        }

        string? contentType = ReadObjectIdentifier(encap.GetChildAt(0));
        if (contentType == null)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "The encapsulated content type is malformed.", "No signer information was processed."));
            return null;
        }

        byte[]? embeddedContent = null;
        if (encap.GetChildCount() == 2)
        {
            EmailCmsAsn1Node wrapper = encap.GetChildAt(1);
            if (!wrapper.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0) || !wrapper.IsConstructed() || wrapper.GetChildCount() != 1)
            {
                issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "The encapsulated content wrapper is malformed.", "No signer information was processed."));
                return null;
            }

            EmailCmsOctetCollector collector = new EmailCmsOctetCollector(limits.GetMaxInputBytes());
            collector.Add(wrapper.GetChildAt(0));
            if (!collector.IsValid())
            {
                issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "Encapsulated S/MIME content is not an OCTET STRING.", "No signer information was processed."));
                return null;
            }

            embeddedContent = collector.GetBytes();
        }

        int index = 3;
        EmailCmsAsn1Node? certificateNode = null;
        if (index < node.GetChildCount() && node.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0))
        {
            certificateNode = node.GetChildAt(index);
            index++;
        }

        if (index < node.GetChildCount() && node.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 1))
            index++;
        if (index != node.GetChildCount() - 1)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData contains misplaced or unexpected fields.", "No signer information was processed."));
            return null;
        }

        EmailCmsAsn1Node signerInfos = node.GetChildAt(index);
        if (!signerInfos.HasTag(EmailCmsAsn1TagClass.Universal, 17) || !signerInfos.IsConstructed())
        {
            issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData signerInfos is not a SET OF values.", "No signer information was processed."));
            return null;
        }

        return new EmailSmimeSignedDataLayout(contentType, embeddedContent, certificateNode, signerInfos, digestOids);
    }

    static List<X509Certificate2>? ParseCertificates(EmailCmsAsn1Node? certificateNode, List<X509Certificate2> extras, EmailCryptographicLimits limits, EmailIssueList issues)
    {
        List<X509Certificate2> values = new List<X509Certificate2>();
        long bytes = 0;
        int index = 0;
        if (certificateNode != null)
        {
            if (!certificateNode.IsConstructed())
            {
                issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "The SignedData certificate set is malformed.", "No signer certificate was selected."));
                return null;
            }

            while (index < certificateNode.GetChildCount())
            {
                byte[] encoded = certificateNode.GetChildAt(index).GetEncodedBytes();
                bytes += encoded.Length;
                if (bytes > limits.GetMaxCertificateBytes())
                    throw new EmailLimitException("MaxCertificateBytes", limits.GetMaxCertificateBytes(), bytes);
                EmailCapturedOperation certificate = EmailOperationCapture.RunWithOperationUsingFunc(() => X509CertificateLoader.LoadCertificate(encoded));
                if (!certificate.IsSuccess())
                {
                    issues.Add(Issue(EmailIssueCode.SmimeSignedDataMalformed, "SignedData contains a malformed certificate choice.", "No signer certificate was selected."));
                    return null;
                }

                values.Add(((X509Certificate2? )certificate.GetValue() ?? throw new InvalidOperationException()));
                index++;
            }
        }

        index = 0;
        while (index < extras.Count)
        {
            bytes += extras[index].RawData.Length;
            if (bytes > limits.GetMaxCertificateBytes())
                throw new EmailLimitException("MaxCertificateBytes", limits.GetMaxCertificateBytes(), bytes);
            values.Add(extras[index]);
            index++;
        }

        return values;
    }

    static EmailSmimeSignerVerificationResult VerifySigner(EmailCmsAsn1Node signerInfo, EmailSmimeSignedDataLayout layout, byte[] content, List<X509Certificate2> certificates, EmailSmimeVerificationOptions options, EmailIssueList issues)
    {
        if (!signerInfo.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !signerInfo.IsConstructed() || signerInfo.GetChildCount() < 5 || signerInfo.GetChildCount() > 7)
            return Failure(EmailSmimeSignerVerificationStatus.Malformed, EmailIssueCode.SmimeSignedDataMalformed, "A SignerInfo value is malformed.", EmailSmimeDigestAlgorithm.Unknown, EmailSmimeSignatureAlgorithm.Unknown, null, false, false, issues);
        int index = 0;
        if (!signerInfo.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.Universal, 2))
            return Failure(EmailSmimeSignerVerificationStatus.Malformed, EmailIssueCode.SmimeSignedDataMalformed, "SignerInfo has no valid version.", EmailSmimeDigestAlgorithm.Unknown, EmailSmimeSignatureAlgorithm.Unknown, null, false, false, issues);
        index++;
        EmailCmsAsn1Node signerIdentifier = signerInfo.GetChildAt(index++);
        string? digestOid = ReadAlgorithmOid(signerInfo.GetChildAt(index++));
        EmailSmimeDigestAlgorithm digestAlgorithm = EmailSmimeCms.DigestFromOid(digestOid);
        EmailCmsAsn1Node? signedAttributes = null;
        if (index < signerInfo.GetChildCount() && signerInfo.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0))
        {
            signedAttributes = signerInfo.GetChildAt(index);
            index++;
        }

        if (index + 1 >= signerInfo.GetChildCount())
            return Failure(EmailSmimeSignerVerificationStatus.Malformed, EmailIssueCode.SmimeSignedDataMalformed, "SignerInfo is missing its signature fields.", digestAlgorithm, EmailSmimeSignatureAlgorithm.Unknown, null, false, false, issues);
        string? signatureOid = ReadAlgorithmOid(signerInfo.GetChildAt(index++));
        EmailSmimeSignatureAlgorithm signatureAlgorithm = EmailSmimeCms.SignatureFromOid(signatureOid);
        EmailCmsAsn1Node signatureNode = signerInfo.GetChildAt(index++);
        if (index < signerInfo.GetChildCount())
        {
            EmailCmsAsn1Node unsigned = signerInfo.GetChildAt(index++);
            if (!unsigned.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 1))
                return Failure(EmailSmimeSignerVerificationStatus.Malformed, EmailIssueCode.SmimeSignedDataMalformed, "SignerInfo has an unexpected trailing field.", digestAlgorithm, signatureAlgorithm, null, false, false, issues);
        }

        if (index != signerInfo.GetChildCount() || !signatureNode.HasTag(EmailCmsAsn1TagClass.Universal, 4) || signatureNode.IsConstructed())
            return Failure(EmailSmimeSignerVerificationStatus.Malformed, EmailIssueCode.SmimeSignedDataMalformed, "SignerInfo has a malformed signature value.", digestAlgorithm, signatureAlgorithm, null, false, false, issues);
        if (digestAlgorithm == EmailSmimeDigestAlgorithm.Unknown || signatureAlgorithm == EmailSmimeSignatureAlgorithm.Unknown || !layout.ContainsDigestOid(digestOid) || !EmailSmimeCms.SignatureDigestMatchesOid(signatureOid, digestAlgorithm))
            return Failure(EmailSmimeSignerVerificationStatus.UnsupportedAlgorithm, EmailIssueCode.SmimeUnsupportedAlgorithm, "SignerInfo uses an unsupported or inconsistent algorithm identifier.", digestAlgorithm, signatureAlgorithm, null, false, false, issues);
        X509Certificate2? certificate = FindCertificate(signerIdentifier, certificates);
        if (certificate == null)
            return Failure(EmailSmimeSignerVerificationStatus.CertificateNotFound, EmailIssueCode.SmimeSignerCertificateMissing, "No supplied certificate matches the SignerIdentifier.", digestAlgorithm, signatureAlgorithm, null, false, false, issues);
        EmailSmimeCertificateInfo certificateInfo = new EmailSmimeCertificateInfo(certificate);
        EmailSmimeAlgorithmPolicy policy = options.GetAlgorithmPolicy();
        if (!policy.IsDigestAllowed(digestAlgorithm, false) || signatureAlgorithm == EmailSmimeSignatureAlgorithm.RsaPkcs1 && !policy.GetAllowRsaPkcs1() || signatureAlgorithm == EmailSmimeSignatureAlgorithm.Ecdsa && !policy.GetAllowEcdsa())
            return Failure(EmailSmimeSignerVerificationStatus.PolicyRejected, EmailIssueCode.SmimeAlgorithmPolicyRejected, "The signer algorithm is rejected by verification policy.", digestAlgorithm, signatureAlgorithm, certificateInfo, false, false, issues);
        if (signatureAlgorithm == EmailSmimeSignatureAlgorithm.RsaPkcs1)
        {
            using (RSA? rsa = certificate.GetRSAPublicKey())
            {
                if (rsa == null)
                    return Failure(EmailSmimeSignerVerificationStatus.UnsupportedAlgorithm, EmailIssueCode.SmimeUnsupportedAlgorithm, "The signer certificate does not contain an RSA public key.", digestAlgorithm, signatureAlgorithm, certificateInfo, false, false, issues);
                if (!policy.IsRsaKeySizeAllowed(rsa.KeySize))
                    return Failure(EmailSmimeSignerVerificationStatus.PolicyRejected, EmailIssueCode.SmimeAlgorithmPolicyRejected, "The signer RSA key violates the configured key-size policy.", digestAlgorithm, signatureAlgorithm, certificateInfo, false, false, issues);
            }
        }
        else
        {
            using (ECDsa? ecdsa = certificate.GetECDsaPublicKey())
            {
                if (ecdsa == null)
                    return Failure(EmailSmimeSignerVerificationStatus.UnsupportedAlgorithm, EmailIssueCode.SmimeUnsupportedAlgorithm, "The signer certificate does not contain an ECDSA public key.", digestAlgorithm, signatureAlgorithm, certificateInfo, false, false, issues);
            }
        }

        byte[] verificationInput = content;
        bool digestValid = true;
        if (signedAttributes != null)
        {
            EmailSmimeAttributeValues? attributes = ParseSignedAttributes(signedAttributes, layout.GetContentType());
            if (attributes == null)
                return Failure(EmailSmimeSignerVerificationStatus.Malformed, EmailIssueCode.SmimeSignedDataMalformed, "SignerInfo signed attributes are malformed, duplicated, or not canonical DER.", digestAlgorithm, signatureAlgorithm, certificateInfo, false, false, issues);
            byte[] actualDigest = EmailSmimeCms.Hash(content, digestAlgorithm);
            digestValid = CryptographicOperations.FixedTimeEquals(actualDigest, attributes.GetMessageDigest());
            if (!digestValid)
                return Failure(EmailSmimeSignerVerificationStatus.DigestMismatch, EmailIssueCode.SmimeDigestMismatch, "The signed message-digest attribute does not match the content.", digestAlgorithm, signatureAlgorithm, certificateInfo, false, false, issues);
            verificationInput = attributes.GetEncodedSet();
        }

        byte[] inputHash = EmailSmimeCms.Hash(verificationInput, digestAlgorithm);
        bool signatureValid = VerifySignature(certificate, signatureAlgorithm, digestAlgorithm, inputHash, signatureNode.GetContentBytes(), policy);
        if (!signatureValid)
            return Failure(EmailSmimeSignerVerificationStatus.SignatureMismatch, EmailIssueCode.SmimeSignatureMismatch, "The CMS signature does not match the signed content and attributes.", digestAlgorithm, signatureAlgorithm, certificateInfo, digestValid, false, issues);
        EmailSmimeChainResult? chainResult = null;
        if (options.GetVerifyCertificateChain())
        {
            chainResult = BuildChain(certificate, certificates, options);
            if (!chainResult.IsValid())
            {
                EmailSmimeSignerVerificationResult failure = new EmailSmimeSignerVerificationResult(EmailSmimeSignerVerificationStatus.CertificateChainInvalid, "The signature is valid but its certificate chain is not trusted.", digestAlgorithm, signatureAlgorithm, certificateInfo, chainResult, digestValid, true);
                issues.Add(Issue(EmailIssueCode.SmimeCertificateChainInvalid, failure.GetReason(), "Inspect the per-signer chain statuses or provide appropriate trust roots and intermediates."));
                return failure;
            }
        }

        return new EmailSmimeSignerVerificationResult(EmailSmimeSignerVerificationStatus.Pass, "The S/MIME signer digest, signature, certificate chain, and algorithm policy are valid.", digestAlgorithm, signatureAlgorithm, certificateInfo, chainResult, digestValid, true);
    }

    static EmailSmimeAttributeValues? ParseSignedAttributes(EmailCmsAsn1Node node, string expectedContentType)
    {
        if (!node.IsConstructed() || node.GetChildCount() == 0 || !IsSorted(node))
            return null;
        EmailCapturedOperation changed = EmailOperationCapture.RunWithOperationUsingFunc(() => EmailSmimeCms.ChangeContextZeroToSet(node.GetEncodedBytes()));
        if (!changed.IsSuccess())
            return null;
        byte[]? encodedSet = (byte[]? )changed.GetValue();
        Asn1Tag tag = default(Asn1Tag);
        int offset = 0;
        int length = 0;
        int consumed = 0;
        if (!AsnDecoder.TryReadEncodedValue(encodedSet, AsnEncodingRules.DER, out tag, out offset, out length, out consumed) || consumed != (encodedSet ?? throw new InvalidOperationException()).Length)
            return null;
        byte[]? digest = null;
        bool contentTypeFound = false;
        int index = 0;
        while (index < node.GetChildCount())
        {
            EmailCmsAsn1Node attribute = node.GetChildAt(index);
            if (!attribute.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !attribute.IsConstructed() || attribute.GetChildCount() != 2)
                return null;
            string? oid = ReadObjectIdentifier(attribute.GetChildAt(0));
            EmailCmsAsn1Node values = attribute.GetChildAt(1);
            if (oid == null || !values.HasTag(EmailCmsAsn1TagClass.Universal, 17) || !values.IsConstructed() || values.GetChildCount() != 1)
                return null;
            if (oid == EmailSmimeCms.ContentTypeAttributeOid)
            {
                if (contentTypeFound || ReadObjectIdentifier(values.GetChildAt(0)) != expectedContentType)
                    return null;
                contentTypeFound = true;
            }
            else if (oid == EmailSmimeCms.MessageDigestAttributeOid)
            {
                EmailCmsAsn1Node value = values.GetChildAt(0);
                if (digest != null || !value.HasTag(EmailCmsAsn1TagClass.Universal, 4) || value.IsConstructed())
                    return null;
                digest = value.GetContentBytes();
            }

            index++;
        }

        if (!contentTypeFound || digest == null)
            return null;
        return new EmailSmimeAttributeValues(encodedSet, digest);
    }

    static bool IsSorted(EmailCmsAsn1Node node)
    {
        int index = 1;
        while (index < node.GetChildCount())
        {
            byte[] left = node.GetChildAt(index - 1).GetEncodedBytes();
            byte[] right = node.GetChildAt(index).GetEncodedBytes();
            int common = Math.Min(left.Length, right.Length);
            int byteIndex = 0;
            while (byteIndex < common && left[byteIndex] == right[byteIndex])
                byteIndex++;
            if (byteIndex < common && left[byteIndex] > right[byteIndex] || byteIndex == common && left.Length > right.Length)
                return false;
            index++;
        }

        return true;
    }

    static X509Certificate2? FindCertificate(EmailCmsAsn1Node sid, List<X509Certificate2> certificates)
    {
        int index = 0;
        if (sid.HasTag(EmailCmsAsn1TagClass.Universal, 16) && sid.IsConstructed() && sid.GetChildCount() == 2)
        {
            byte[] issuer = sid.GetChildAt(0).GetEncodedBytes();
            byte[]? serial = NormalizeUnsigned(sid.GetChildAt(1));
            if (serial == null)
                return null;
            while (index < certificates.Count)
            {
                if (issuer.AsSpan().SequenceEqual(certificates[index].IssuerName.RawData) && serial.AsSpan().SequenceEqual(EmailSmimeCms.SerialBytes(certificates[index])))
                    return certificates[index];
                index++;
            }

            return null;
        }

        if (sid.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0) && !sid.IsConstructed())
        {
            byte[] expected = sid.GetContentBytes();
            while (index < certificates.Count)
            {
                X509SubjectKeyIdentifierExtension? extension = certificates[index].Extensions["2.5.29.14"] as X509SubjectKeyIdentifierExtension;
                if (extension != null && Convert.FromHexString((extension.SubjectKeyIdentifier ?? throw new InvalidOperationException())).AsSpan().SequenceEqual(expected))
                    return certificates[index];
                index++;
            }
        }

        return null;
    }

    static byte[]? NormalizeUnsigned(EmailCmsAsn1Node integer)
    {
        if (!integer.HasTag(EmailCmsAsn1TagClass.Universal, 2) || integer.IsConstructed())
            return null;
        byte[] value = integer.GetContentBytes();
        if (value.Length == 0 || (value[0] & 128) != 0)
            return null;
        int start = 0;
        while (start + 1 < value.Length && value[start] == 0)
            start++;
        byte[] normalized = new byte[value.Length - start];
        new ReadOnlySpan<byte>(value, start, normalized.Length).CopyTo(normalized);
        return normalized;
    }

    static bool VerifySignature(X509Certificate2 certificate, EmailSmimeSignatureAlgorithm signatureAlgorithm, EmailSmimeDigestAlgorithm digestAlgorithm, byte[] hash, byte[] signature, EmailSmimeAlgorithmPolicy policy)
    {
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            if (signatureAlgorithm == EmailSmimeSignatureAlgorithm.RsaPkcs1)
            {
                using (RSA? rsa = certificate.GetRSAPublicKey())
                {
                    if (rsa == null || !policy.IsRsaKeySizeAllowed(rsa.KeySize))
                        return false;
                    return (object)rsa.VerifyHash(hash, signature, EmailSmimeCms.HashName(digestAlgorithm), RSASignaturePadding.Pkcs1);
                }
            }

            if (signatureAlgorithm == EmailSmimeSignatureAlgorithm.Ecdsa)
            {
                using (ECDsa? ecdsa = certificate.GetECDsaPublicKey())
                {
                    return (object)(ecdsa != null && ecdsa.VerifyHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence));
                }
            }

            return (object)false;
        });
        return captured.IsSuccess() && (bool)(captured.GetValue() ?? throw new InvalidOperationException());
    }

    static EmailSmimeChainResult BuildChain(X509Certificate2 certificate, List<X509Certificate2> certificates, EmailSmimeVerificationOptions options)
    {
        using (X509Chain chain = new X509Chain())
        {
            chain.ChainPolicy.RevocationMode = options.GetRevocationMode();
            chain.ChainPolicy.DisableCertificateDownloads = true;
            if (options.HasVerificationTime())
                chain.ChainPolicy.VerificationTime = options.GetVerificationTime();
            List<X509Certificate2> roots = options.CopyCustomTrustRoots();
            if (roots.Count > 0)
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                int rootIndex = 0;
                while (rootIndex < roots.Count)
                {
                    chain.ChainPolicy.CustomTrustStore.Add(roots[rootIndex]);
                    rootIndex++;
                }
            }

            int index = 0;
            while (index < certificates.Count)
            {
                if (!certificates[index].RawData.AsSpan().SequenceEqual(certificate.RawData))
                    chain.ChainPolicy.ExtraStore.Add(certificates[index]);
                index++;
            }

            bool valid = chain.Build(certificate);
            List<EmailSmimeChainStatus> statuses = new List<EmailSmimeChainStatus>();
            index = 0;
            while (index < chain.ChainStatus.Length)
            {
                statuses.Add(new EmailSmimeChainStatus(chain.ChainStatus[index].Status, chain.ChainStatus[index].StatusInformation));
                index++;
            }

            List<EmailSmimeCertificateInfo> elements = new List<EmailSmimeCertificateInfo>();
            index = 0;
            while (index < chain.ChainElements.Count)
            {
                elements.Add(new EmailSmimeCertificateInfo(chain.ChainElements[index].Certificate));
                index++;
            }

            return new EmailSmimeChainResult(valid, statuses, elements);
        }
    }

    static string? ReadAlgorithmOid(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() < 1 || node.GetChildCount() > 2)
            return null;
        if (node.GetChildCount() == 2)
        {
            EmailCmsAsn1Node parameters = node.GetChildAt(1);
            if (!parameters.HasTag(EmailCmsAsn1TagClass.Universal, 5) || parameters.IsConstructed() || parameters.GetContentBytes().Length != 0)
                return null;
        }

        return ReadObjectIdentifier(node.GetChildAt(0));
    }

    static string? ReadObjectIdentifier(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 6) || node.IsConstructed())
            return null;
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            AsnReader reader = new AsnReader(node.GetEncodedBytes(), AsnEncodingRules.BER);
            string value = reader.ReadObjectIdentifier();
            return reader.HasData ? null : value;
        });
        return captured.IsSuccess() ? (string? )captured.GetValue() : null;
    }

    static EmailSmimeSignerVerificationResult Failure(EmailSmimeSignerVerificationStatus status, EmailIssueCode issueCode, string reason, EmailSmimeDigestAlgorithm digestAlgorithm, EmailSmimeSignatureAlgorithm signatureAlgorithm, EmailSmimeCertificateInfo? certificate, bool digestValid, bool signatureValid, EmailIssueList issues)
    {
        issues.Add(Issue(issueCode, reason, "The affected signer was rejected without trusting its result."));
        return new EmailSmimeSignerVerificationResult(status, reason, digestAlgorithm, signatureAlgorithm, certificate, null, digestValid, signatureValid);
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, recovery);
    }
}
