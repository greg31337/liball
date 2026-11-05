using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Provides operations for working with S/MIME signer.
/// </summary>
public class EmailSmimeSigner : IEmailSmimeSignerContract
{
    internal static EmailSmimeSigningResult SignDetachedImplementationWithContentAndSigners(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers)
    {
        return Sign(content, signers, true, new EmailSmimeSigningOptions());
    }

    /// <summary>
    /// Signs the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    public EmailSmimeSigningResult SignDetachedWithContentAndSigners(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers)
    {
        return EmailSmimeSigner.SignDetachedImplementationWithContentAndSigners(content, signers);
    }

    internal static EmailSmimeSigningResult SignDetachedImplementationWithContentAndSignersAndOptions(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, EmailSmimeSigningOptions options)
    {
        return Sign(content, signers, true, options);
    }

    /// <summary>
    /// Signs the detached.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    public EmailSmimeSigningResult SignDetachedWithContentAndSignersAndOptions(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, EmailSmimeSigningOptions options)
    {
        return EmailSmimeSigner.SignDetachedImplementationWithContentAndSignersAndOptions(content, signers, options);
    }

    internal static EmailSmimeSigningResult SignOpaqueImplementationWithContentAndSigners(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers)
    {
        return Sign(content, signers, false, new EmailSmimeSigningOptions());
    }

    /// <summary>
    /// Signs the opaque.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    public EmailSmimeSigningResult SignOpaqueWithContentAndSigners(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers)
    {
        return EmailSmimeSigner.SignOpaqueImplementationWithContentAndSigners(content, signers);
    }

    internal static EmailSmimeSigningResult SignOpaqueImplementationWithContentAndSignersAndOptions(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, EmailSmimeSigningOptions options)
    {
        return Sign(content, signers, false, options);
    }

    /// <summary>
    /// Signs the opaque.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME signing result produced by the operation.</returns>
    public EmailSmimeSigningResult SignOpaqueWithContentAndSignersAndOptions(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, EmailSmimeSigningOptions options)
    {
        return EmailSmimeSigner.SignOpaqueImplementationWithContentAndSignersAndOptions(content, signers, options);
    }

    static EmailSmimeSigningResult Sign(ReadOnlySpan<byte> content, List<EmailSmimeSigningCredential> signers, bool detached, EmailSmimeSigningOptions options)
    {
        if (signers == null)
            throw new ArgumentNullException("signers");
        if (options == null)
            throw new ArgumentNullException("options");
        ValidateSigners(signers);
        EmailSmimeSigningOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (content.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxSmimeContentBytes", limits.GetMaxInputBytes(), content.Length);
        if (signers.Count > limits.GetMaxSignatureCount())
            throw new EmailLimitException("MaxSignatureCount", limits.GetMaxSignatureCount(), signers.Count);
        if ((long)signers.Count * 2 > limits.GetMaxOperationCount())
            throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), signers.Count * 2);
        EmailIssueList issues = new EmailIssueList();
        if (signers.Count == 0)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSigningFailed, "At least one S/MIME signer is required.", "No CMS SignedData value was emitted."));
            return new EmailSmimeSigningResult(new byte[0], issues, detached, 0);
        }

        byte[] contentBytes = content.ToArray();
        List<EmailSmimePreparedSigner> prepared = new List<EmailSmimePreparedSigner>();
        long certificateBytes = 0;
        int index = 0;
        while (index < signers.Count)
        {
            EmailSmimeSigningCredential credential = signers[index];
            X509Certificate2 certificate = credential.GetCertificate();
            certificateBytes += certificate.RawData.Length;
            if (certificateBytes > limits.GetMaxCertificateBytes())
                throw new EmailLimitException("MaxCertificateBytes", limits.GetMaxCertificateBytes(), certificateBytes);
            EmailSmimePreparedSigner? value = PrepareSigner(contentBytes, credential, snapshot.GetAlgorithmPolicy(), issues);
            if (value == null)
                return new EmailSmimeSigningResult(new byte[0], issues, detached, prepared.Count);
            prepared.Add(value);
            index++;
        }

        EmailCapturedOperation encoded = EmailOperationCapture.RunWithOperationUsingFunc(() => EncodeSignedData(contentBytes, prepared, snapshot.GetIncludeCertificates(), detached));
        if (!encoded.IsSuccess())
        {
            Exception? exception = encoded.GetException();
            if (!(exception is CryptographicException || exception is InvalidOperationException || exception is ArgumentException))
                throw (exception ?? throw new InvalidOperationException());
            issues.Add(Issue(EmailIssueCode.SmimeSigningFailed, "CMS SignedData encoding failed without exposing provider diagnostics.", "No partial signature output was returned."));
            return new EmailSmimeSigningResult(new byte[0], issues, detached, prepared.Count);
        }

        byte[]? cms = (byte[]? )encoded.GetValue();
        if ((cms ?? throw new InvalidOperationException()).LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsOutputBytes", limits.GetMaxInputBytes(), cms.LongLength);
        EmailCmsReaderOptions readerOptions = new EmailCmsReaderOptions();
        readerOptions.SetLimits(limits);
        readerOptions.SetAllowBer(false);
        EmailCmsReadResult validation = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(cms, readerOptions);
        if (!validation.IsSuccess() || (validation.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() != EmailCmsContentKind.SignedData)
        {
            issues.Add(Issue(EmailIssueCode.SmimeSigningFailed, "The generated CMS SignedData value failed bounded DER validation.", "The invalid output was withheld."));
            return new EmailSmimeSigningResult(new byte[0], issues, detached, prepared.Count);
        }

        return new EmailSmimeSigningResult(cms, issues, detached, prepared.Count);
    }

    static EmailSmimePreparedSigner? PrepareSigner(byte[] content, EmailSmimeSigningCredential credential, EmailSmimeAlgorithmPolicy policy, EmailIssueList issues)
    {
        EmailSmimeDigestAlgorithm digestAlgorithm = credential.GetDigestAlgorithm();
        if (!policy.IsDigestAllowed(digestAlgorithm, true))
        {
            issues.Add(Issue(EmailIssueCode.SmimeAlgorithmPolicyRejected, "The requested digest algorithm is prohibited for S/MIME signing.", "Choose an enabled SHA-2 digest algorithm."));
            return null;
        }

        X509Certificate2 certificate = credential.GetCertificate();
        byte[] digest = EmailSmimeCms.Hash(content, digestAlgorithm);
        byte[] signedAttributes = EmailSmimeCms.EncodeSignedAttributes(EmailCmsReaderConstants.DataObjectIdentifier, digest);
        byte[] attributeHash = EmailSmimeCms.Hash(signedAttributes, digestAlgorithm);
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            if (credential.HasPrivateKey())
            {
                RSA? suppliedRsa = credential.GetPrivateKey() as RSA;
                if (suppliedRsa != null)
                    return PrepareRsa(certificate, digestAlgorithm, signedAttributes, attributeHash, suppliedRsa, policy, issues);
                ECDsa? suppliedEcdsa = credential.GetPrivateKey() as ECDsa;
                if (suppliedEcdsa != null)
                    return PrepareEcdsa(certificate, digestAlgorithm, signedAttributes, attributeHash, suppliedEcdsa, policy, issues);
            }

            using (RSA? rsa = certificate.GetRSAPrivateKey())
            {
                if (rsa != null)
                    return PrepareRsa(certificate, digestAlgorithm, signedAttributes, attributeHash, rsa, policy, issues);
            }

            using (ECDsa? ecdsa = certificate.GetECDsaPrivateKey())
            {
                if (ecdsa != null)
                    return PrepareEcdsa(certificate, digestAlgorithm, signedAttributes, attributeHash, ecdsa, policy, issues);
            }

            return null;
        });
        CryptographicOperations.ZeroMemory(digest);
        CryptographicOperations.ZeroMemory(attributeHash);
        if (!captured.IsSuccess())
        {
            if (!(captured.GetException() is CryptographicException))
                throw (captured.GetException() ?? throw new InvalidOperationException());
            issues.Add(Issue(EmailIssueCode.SmimeSigningFailed, "The signer private-key operation failed without exposing provider diagnostics.", "No CMS output was returned."));
            return null;
        }

        if (captured.GetValue() != null)
            return (EmailSmimePreparedSigner? )captured.GetValue();
        issues.Add(Issue(EmailIssueCode.SmimeSigningFailed, "The signing certificate does not expose a supported RSA or ECDSA private key.", "Supply a certificate associated with a supported private key."));
        return null;
    }

    static EmailSmimePreparedSigner? PrepareRsa(X509Certificate2 certificate, EmailSmimeDigestAlgorithm digestAlgorithm, byte[] signedAttributes, byte[] attributeHash, RSA rsa, EmailSmimeAlgorithmPolicy policy, EmailIssueList issues)
    {
        if (!policy.GetAllowRsaPkcs1() || !policy.IsRsaKeySizeAllowed(rsa.KeySize))
        {
            issues.Add(Issue(EmailIssueCode.SmimeAlgorithmPolicyRejected, "The RSA signer is disabled or violates the configured key-size policy.", "Use an enabled RSA key within the configured size range."));
            return null;
        }

        byte[] signature = rsa.SignHash(attributeHash, EmailSmimeCms.HashName(digestAlgorithm), RSASignaturePadding.Pkcs1);
        return new EmailSmimePreparedSigner(certificate, digestAlgorithm, EmailSmimeSignatureAlgorithm.RsaPkcs1, signedAttributes, signature);
    }

    static EmailSmimePreparedSigner? PrepareEcdsa(X509Certificate2 certificate, EmailSmimeDigestAlgorithm digestAlgorithm, byte[] signedAttributes, byte[] attributeHash, ECDsa ecdsa, EmailSmimeAlgorithmPolicy policy, EmailIssueList issues)
    {
        if (!policy.GetAllowEcdsa())
        {
            issues.Add(Issue(EmailIssueCode.SmimeAlgorithmPolicyRejected, "ECDSA signing is disabled by policy.", "Enable ECDSA or supply an allowed RSA signer."));
            return null;
        }

        byte[] signature = ecdsa.SignHash(attributeHash, DSASignatureFormat.Rfc3279DerSequence);
        return new EmailSmimePreparedSigner(certificate, digestAlgorithm, EmailSmimeSignatureAlgorithm.Ecdsa, signedAttributes, signature);
    }

    static byte[] EncodeSignedData(byte[] content, List<EmailSmimePreparedSigner> signers, bool includeCertificates, bool detached)
    {
        AsnWriter signedData = new AsnWriter(AsnEncodingRules.DER);
        signedData.PushSequence();
        signedData.WriteInteger(1);
        signedData.PushSetOf();
        List<EmailSmimeDigestAlgorithm> digests = UniqueDigests(signers);
        int index = 0;
        while (index < digests.Count)
        {
            EmailSmimeCms.WriteAlgorithmIdentifier(signedData, EmailSmimeCms.DigestOid(digests[index]), false);
            index++;
        }

        signedData.PopSetOf();
        signedData.PushSequence();
        signedData.WriteObjectIdentifier(EmailCmsReaderConstants.DataObjectIdentifier);
        if (!detached)
        {
            Asn1Tag contentTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            signedData.PushSequence(contentTag);
            signedData.WriteOctetString(content);
            signedData.PopSequence(contentTag);
        }

        signedData.PopSequence();
        if (includeCertificates)
        {
            Asn1Tag certificateTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            signedData.PushSetOf(certificateTag);
            List<byte[]> certificates = UniqueCertificates(signers);
            index = 0;
            while (index < certificates.Count)
            {
                signedData.WriteEncodedValue(certificates[index]);
                index++;
            }

            signedData.PopSetOf(certificateTag);
        }

        signedData.PushSetOf();
        index = 0;
        while (index < signers.Count)
        {
            WriteSignerInfo(signedData, signers[index]);
            index++;
        }

        signedData.PopSetOf();
        signedData.PopSequence();
        AsnWriter contentInfo = new AsnWriter(AsnEncodingRules.DER);
        contentInfo.PushSequence();
        contentInfo.WriteObjectIdentifier(EmailCmsReaderConstants.SignedDataObjectIdentifier);
        Asn1Tag explicitTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        contentInfo.PushSequence(explicitTag);
        contentInfo.WriteEncodedValue(signedData.Encode());
        contentInfo.PopSequence(explicitTag);
        contentInfo.PopSequence();
        return contentInfo.Encode();
    }

    static void WriteSignerInfo(AsnWriter writer, EmailSmimePreparedSigner signer)
    {
        writer.PushSequence();
        writer.WriteInteger(1);
        writer.PushSequence();
        writer.WriteEncodedValue(signer.GetCertificate().IssuerName.RawData);
        writer.WriteIntegerUnsigned(EmailSmimeCms.SerialBytes(signer.GetCertificate()));
        writer.PopSequence();
        EmailSmimeCms.WriteAlgorithmIdentifier(writer, EmailSmimeCms.DigestOid(signer.GetDigestAlgorithm()), false);
        writer.WriteEncodedValue(EmailSmimeCms.ChangeSetToContextZero(signer.GetSignedAttributes()));
        string signatureOid = EmailSmimeCms.SignatureOid(signer.GetSignatureAlgorithm(), signer.GetDigestAlgorithm());
        EmailSmimeCms.WriteAlgorithmIdentifier(writer, signatureOid, signer.GetSignatureAlgorithm() == EmailSmimeSignatureAlgorithm.RsaPkcs1);
        writer.WriteOctetString(signer.GetSignature());
        writer.PopSequence();
    }

    static List<EmailSmimeDigestAlgorithm> UniqueDigests(List<EmailSmimePreparedSigner> signers)
    {
        List<EmailSmimeDigestAlgorithm> values = new List<EmailSmimeDigestAlgorithm>();
        int index = 0;
        while (index < signers.Count)
        {
            EmailSmimeDigestAlgorithm value = signers[index].GetDigestAlgorithm();
            if (!values.Contains(value))
                values.Add(value);
            index++;
        }

        return values;
    }

    static List<byte[]> UniqueCertificates(List<EmailSmimePreparedSigner> signers)
    {
        List<byte[]> values = new List<byte[]>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        while (index < signers.Count)
        {
            X509Certificate2 certificate = signers[index].GetCertificate();
            if (seen.Add(certificate.Thumbprint))
                values.Add(certificate.RawData);
            index++;
        }

        return values;
    }

    static void ValidateSigners(List<EmailSmimeSigningCredential> signers)
    {
        int index = 0;
        while (index < signers.Count)
        {
            if (signers[index] == null)
                throw new ArgumentException("The signer list cannot contain null entries.", "signers");
            index++;
        }
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, recovery);
    }
}
