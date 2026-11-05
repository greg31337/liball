using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Provides operations for working with S/MIME decryptor.
/// </summary>
public class EmailSmimeDecryptor : IEmailSmimeDecryptorContract
{
    internal static EmailSmimeDecryptionResult? DecryptImplementationWithCmsAndCredentials(ReadOnlySpan<byte> cms, List<EmailSmimeDecryptionCredential> credentials)
    {
        return DecryptImplementationWithCmsAndCredentialsAndOptions(cms, credentials, new EmailSmimeDecryptionOptions());
    }

    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    public EmailSmimeDecryptionResult? DecryptWithCmsAndCredentials(ReadOnlySpan<byte> cms, List<EmailSmimeDecryptionCredential> credentials)
    {
        return EmailSmimeDecryptor.DecryptImplementationWithCmsAndCredentials(cms, credentials);
    }

    internal static EmailSmimeDecryptionResult? DecryptImplementationWithCmsAndCredentialsAndOptions(ReadOnlySpan<byte> cms, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options)
    {
        if (credentials == null)
            throw new ArgumentNullException("credentials");
        if (options == null)
            throw new ArgumentNullException("options");
        ValidateCredentials(credentials);
        return DecryptBytes(cms.ToArray(), credentials, options.Clone());
    }

    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "cms">The cms used by the operation.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeDecryptionResult? DecryptWithCmsAndCredentialsAndOptions(ReadOnlySpan<byte> cms, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options)
    {
        return EmailSmimeDecryptor.DecryptImplementationWithCmsAndCredentialsAndOptions(cms, credentials, options);
    }

    internal static EmailSmimeDecryptionResult? DecryptImplementationWithEntityAndCredentials(MimeEntity entity, List<EmailSmimeDecryptionCredential> credentials)
    {
        return DecryptImplementationWithEntityAndCredentialsAndOptions(entity, credentials, new EmailSmimeDecryptionOptions());
    }

    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    public EmailSmimeDecryptionResult? DecryptWithEntityAndCredentials(MimeEntity entity, List<EmailSmimeDecryptionCredential> credentials)
    {
        return EmailSmimeDecryptor.DecryptImplementationWithEntityAndCredentials(entity, credentials);
    }

    internal static EmailSmimeDecryptionResult? DecryptImplementationWithEntityAndCredentialsAndOptions(MimeEntity entity, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (credentials == null)
            throw new ArgumentNullException("credentials");
        if (options == null)
            throw new ArgumentNullException("options");
        ValidateCredentials(credentials);
        EmailSmimeDecryptionOptions snapshot = options.Clone();
        using (System.IO.MemoryStream decoded = new System.IO.MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytes(entity, decoded, snapshot.GetLimits().GetMaxInputBytes());
            return DecryptBytes(decoded.ToArray(), credentials, snapshot);
        }
    }

    /// <summary>
    /// Decrypts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "credentials">The credentials used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME decryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeDecryptionResult? DecryptWithEntityAndCredentialsAndOptions(MimeEntity entity, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options)
    {
        return EmailSmimeDecryptor.DecryptImplementationWithEntityAndCredentialsAndOptions(entity, credentials, options);
    }

    static EmailSmimeDecryptionResult? DecryptBytes(byte[] cms, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeDecryptionOptions options)
    {
        EmailCryptographicLimits limits = options.GetLimits();
        if (cms.LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxCmsInputBytes", limits.GetMaxInputBytes(), cms.LongLength);
        if (credentials.Count > limits.GetMaxRecipientCount())
            throw new EmailLimitException("MaxRecipientKeyCount", limits.GetMaxRecipientCount(), credentials.Count);
        EmailIssueList issues = new EmailIssueList();
        EmailCmsReaderOptions readerOptions = new EmailCmsReaderOptions();
        readerOptions.SetLimits(limits);
        EmailCmsReadResult parsed = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(cms, readerOptions);
        if (!parsed.IsSuccess() || (parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() != EmailCmsContentKind.EnvelopedData && (parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() != EmailCmsContentKind.AuthEnvelopedData)
            return Failure(EmailSmimeDecryptionStatus.Malformed, EmailIssueCode.SmimeEnvelopedDataMalformed, "The input is not a complete EnvelopedData or AuthEnvelopedData CMS value.", issues, EmailSmimeContentEncryptionAlgorithm.Unknown, EmailSmimeKeyTransportAlgorithm.Unknown, null);
        bool authenticatedEnvelope = (parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentKind() == EmailCmsContentKind.AuthEnvelopedData;
        EmailSmimeEnvelopeLayout? layout = ParseEnvelope((parsed.GetContentInfo() ?? throw new InvalidOperationException()).GetContentNode(), authenticatedEnvelope, limits);
        if (layout == null)
            return Failure(EmailSmimeDecryptionStatus.Malformed, EmailIssueCode.SmimeEnvelopedDataMalformed, "The CMS envelope fields are malformed or unsupported.", issues, EmailSmimeContentEncryptionAlgorithm.Unknown, EmailSmimeKeyTransportAlgorithm.Unknown, null);
        if (layout.GetRecipientCount() > limits.GetMaxRecipientCount())
            throw new EmailLimitException("MaxRecipientCount", limits.GetMaxRecipientCount(), layout.GetRecipientCount());
        long operations = (long)layout.GetRecipientCount() * Math.Max(credentials.Count, 1) + 1;
        if (operations > limits.GetMaxOperationCount())
            throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), operations);
        EmailSmimeContentEncryptionAlgorithm contentAlgorithm = layout.GetContentAlgorithm();
        EmailSmimeEncryptionPolicy policy = options.GetPolicy();
        if (contentAlgorithm == EmailSmimeContentEncryptionAlgorithm.Unknown)
            return Failure(EmailSmimeDecryptionStatus.UnsupportedAlgorithm, EmailIssueCode.SmimeContentEncryptionUnsupported, "The CMS content-encryption algorithm is unsupported.", issues, contentAlgorithm, EmailSmimeKeyTransportAlgorithm.Unknown, null);
        if (!policy.IsContentAlgorithmAllowed(contentAlgorithm))
            return Failure(EmailSmimeDecryptionStatus.PolicyRejected, EmailIssueCode.SmimeEncryptionPolicyRejected, "The CMS content-encryption algorithm is rejected by policy.", issues, contentAlgorithm, EmailSmimeKeyTransportAlgorithm.Unknown, null);
        if (authenticatedEnvelope != EmailSmimeEnvelopeCms.IsGcm(contentAlgorithm))
            return Failure(EmailSmimeDecryptionStatus.Malformed, EmailIssueCode.SmimeEnvelopedDataMalformed, "Authenticated algorithms must use AuthEnvelopedData and CBC algorithms must use EnvelopedData.", issues, contentAlgorithm, EmailSmimeKeyTransportAlgorithm.Unknown, null);
        EmailSmimeUnwrappedKey? unwrapped = FindAndUnwrapKey(layout, credentials, policy, limits);
        if (unwrapped == null)
            return Failure(EmailSmimeDecryptionStatus.RecipientNotFound, EmailIssueCode.SmimeRecipientNotFound, "No caller-supplied certificate matches a supported CMS recipient identifier.", issues, contentAlgorithm, EmailSmimeKeyTransportAlgorithm.Unknown, null);
        if (!unwrapped.HasKey())
        {
            EmailSmimeDecryptionStatus status = unwrapped.IsPolicyRejected() ? EmailSmimeDecryptionStatus.PolicyRejected : unwrapped.IsUnsupported() ? EmailSmimeDecryptionStatus.UnsupportedAlgorithm : EmailSmimeDecryptionStatus.KeyUnwrapFailed;
            EmailIssueCode code = unwrapped.IsPolicyRejected() ? EmailIssueCode.SmimeEncryptionPolicyRejected : unwrapped.IsUnsupported() ? EmailIssueCode.SmimeKeyTransportUnsupported : EmailIssueCode.SmimeDecryptionFailed;
            return Failure(status, code, unwrapped.GetFailureReason(), issues, contentAlgorithm, unwrapped.GetAlgorithm(), unwrapped.GetCertificateInfo());
        }

        byte[]? key = unwrapped.GetKey();
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            if ((key ?? throw new InvalidOperationException()).Length != EmailSmimeEnvelopeCms.KeyBytes(contentAlgorithm))
                return Failure(EmailSmimeDecryptionStatus.KeyUnwrapFailed, EmailIssueCode.SmimeDecryptionFailed, "The unwrapped content key has an invalid length.", issues, contentAlgorithm, unwrapped.GetAlgorithm(), unwrapped.GetCertificateInfo());
            byte[]? plaintext = DecryptContent(layout, key, limits);
            if (plaintext == null)
            {
                EmailSmimeDecryptionStatus status = EmailSmimeEnvelopeCms.IsGcm(contentAlgorithm) ? EmailSmimeDecryptionStatus.AuthenticationFailed : EmailSmimeDecryptionStatus.DecryptionFailed;
                EmailIssueCode code = EmailSmimeEnvelopeCms.IsGcm(contentAlgorithm) ? EmailIssueCode.SmimeAuthenticationFailed : EmailIssueCode.SmimeDecryptionFailed;
                string reason = EmailSmimeEnvelopeCms.IsGcm(contentAlgorithm) ? "Authenticated S/MIME decryption failed; no plaintext was released." : "S/MIME content decryption failed; no plaintext was released.";
                return Failure(status, code, reason, issues, contentAlgorithm, unwrapped.GetAlgorithm(), unwrapped.GetCertificateInfo());
            }

            EmailSmimeDecryptionResult result = new EmailSmimeDecryptionResult(EmailSmimeDecryptionStatus.Success, plaintext, issues, contentAlgorithm, unwrapped.GetAlgorithm(), unwrapped.GetCertificateInfo(), EmailSmimeEnvelopeCms.IsGcm(contentAlgorithm));
            CryptographicOperations.ZeroMemory(plaintext);
            return result;
        });
        CryptographicOperations.ZeroMemory(key);
        if (!captured.IsSuccess())
            throw (captured.GetException() ?? throw new InvalidOperationException());
        return (EmailSmimeDecryptionResult? )captured.GetValue();
    }

    static EmailSmimeEnvelopeLayout? ParseEnvelope(EmailCmsAsn1Node node, bool authenticated, EmailCryptographicLimits limits)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() < 3)
            return null;
        if (!node.GetChildAt(0).HasTag(EmailCmsAsn1TagClass.Universal, 2))
            return null;
        int index = 1;
        if (node.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0))
            index++;
        if (index >= node.GetChildCount())
            return null;
        EmailCmsAsn1Node recipients = node.GetChildAt(index++);
        if (!recipients.HasTag(EmailCmsAsn1TagClass.Universal, 17) || !recipients.IsConstructed())
            return null;
        if (index >= node.GetChildCount())
            return null;
        EmailSmimeEncryptedContentLayout? encryptedContent = ParseEncryptedContentInfo(node.GetChildAt(index++), limits);
        if (encryptedContent == null)
            return null;
        byte[]? associatedData = new byte[0];
        byte[]? authenticationTag = null;
        if (authenticated)
        {
            if (index < node.GetChildCount() && node.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 1))
            {
                associatedData = ChangeImplicitSetTag(node.GetChildAt(index).GetEncodedBytes(), 161);
                if (associatedData == null)
                    return null;
                index++;
            }

            if (index >= node.GetChildCount())
                return null;
            EmailCmsAsn1Node tag = node.GetChildAt(index++);
            if (!tag.HasTag(EmailCmsAsn1TagClass.Universal, 4) || tag.IsConstructed())
                return null;
            authenticationTag = tag.GetContentBytes();
            if (index < node.GetChildCount())
            {
                if (!node.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 2))
                    return null;
                index++;
            }
        }
        else if (index < node.GetChildCount())
        {
            if (!node.GetChildAt(index).HasTag(EmailCmsAsn1TagClass.ContextSpecific, 1))
                return null;
            index++;
        }

        if (index != node.GetChildCount())
            return null;
        return new EmailSmimeEnvelopeLayout(recipients, encryptedContent, authenticationTag, associatedData);
    }

    static EmailSmimeEncryptedContentLayout? ParseEncryptedContentInfo(EmailCmsAsn1Node node, EmailCryptographicLimits limits)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() != 3)
            return null;
        if (ReadOid(node.GetChildAt(0)) != EmailCmsReaderConstants.DataObjectIdentifier)
            return null;
        EmailCmsAsn1Node algorithmNode = node.GetChildAt(1);
        if (!algorithmNode.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !algorithmNode.IsConstructed() || algorithmNode.GetChildCount() != 2)
            return null;
        EmailSmimeContentEncryptionAlgorithm algorithm = EmailSmimeEnvelopeCms.ContentFromOid(ReadOid(algorithmNode.GetChildAt(0)));
        EmailCmsAsn1Node parameters = algorithmNode.GetChildAt(1);
        byte[] nonce;
        int tagLength = 0;
        if (EmailSmimeEnvelopeCms.IsGcm(algorithm))
        {
            if (!parameters.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !parameters.IsConstructed() || parameters.GetChildCount() < 1 || parameters.GetChildCount() > 2)
                return null;
            EmailCmsAsn1Node nonceNode = parameters.GetChildAt(0);
            if (!nonceNode.HasTag(EmailCmsAsn1TagClass.Universal, 4) || nonceNode.IsConstructed())
                return null;
            nonce = nonceNode.GetContentBytes();
            tagLength = parameters.GetChildCount() == 2 ? ReadSmallPositiveInteger(parameters.GetChildAt(1)) : 12;
            if (nonce.Length != 12 || tagLength < 12 || tagLength > 16)
                return null;
        }
        else
        {
            if (!parameters.HasTag(EmailCmsAsn1TagClass.Universal, 4) || parameters.IsConstructed())
                return null;
            nonce = parameters.GetContentBytes();
            if (nonce.Length != 16)
                return null;
        }

        byte[]? ciphertext = ReadImplicitOctets(node.GetChildAt(2), limits.GetMaxInputBytes());
        if (ciphertext == null)
            return null;
        return new EmailSmimeEncryptedContentLayout(algorithm, nonce, ciphertext, tagLength);
    }

    static EmailSmimeUnwrappedKey? FindAndUnwrapKey(EmailSmimeEnvelopeLayout layout, List<EmailSmimeDecryptionCredential> credentials, EmailSmimeEncryptionPolicy policy, EmailCryptographicLimits limits)
    {
        bool matched = false;
        bool unsupported = false;
        bool policyRejected = false;
        EmailSmimeKeyTransportAlgorithm lastAlgorithm = EmailSmimeKeyTransportAlgorithm.Unknown;
        EmailSmimeCertificateInfo? lastCertificate = null;
        int recipientIndex = 0;
        while (recipientIndex < layout.GetRecipientCount())
        {
            EmailSmimeKeyRecipientLayout? recipient = ParseRecipient(layout.GetRecipientAt(recipientIndex), limits);
            if (recipient != null)
            {
                int credentialIndex = 0;
                while (credentialIndex < credentials.Count)
                {
                    EmailSmimeDecryptionCredential credential = credentials[credentialIndex];
                    if (Matches(recipient.GetIdentifier(), credential.GetCertificate()))
                    {
                        matched = true;
                        lastAlgorithm = recipient.GetAlgorithm();
                        lastCertificate = new EmailSmimeCertificateInfo(credential.GetCertificate());
                        if (recipient.GetAlgorithm() == EmailSmimeKeyTransportAlgorithm.Unknown)
                            unsupported = true;
                        else if (!policy.IsKeyTransportAllowed(recipient.GetAlgorithm(), false))
                            policyRejected = true;
                        else
                        {
                            RSA? rsa = credential.HasPrivateKey() ? credential.GetPrivateKey() : null;
                            bool dispose = false;
                            if (rsa == null)
                            {
                                rsa = credential.GetCertificate().GetRSAPrivateKey();
                                dispose = rsa != null;
                            }

                            if (rsa != null && policy.IsRsaKeySizeAllowed(rsa.KeySize))
                            {
                                RSAEncryptionPadding padding = recipient.GetAlgorithm() == EmailSmimeKeyTransportAlgorithm.RsaOaepSha256 ? RSAEncryptionPadding.OaepSHA256 : RSAEncryptionPadding.Pkcs1;
                                EmailCapturedOperation decrypted = EmailOperationCapture.RunWithOperationUsingFunc(() => rsa.Decrypt(recipient.GetEncryptedKey(), padding));
                                if (dispose)
                                    rsa.Dispose();
                                if (decrypted.IsSuccess())
                                    return EmailSmimeUnwrappedKey.Success((byte[]? )decrypted.GetValue(), recipient.GetAlgorithm(), lastCertificate);
                            }
                            else if (rsa != null)
                                policyRejected = true;
                            if (dispose && rsa != null)
                                rsa.Dispose();
                        }
                    }

                    credentialIndex++;
                }
            }

            recipientIndex++;
        }

        if (!matched)
            return null;
        if (policyRejected)
            return EmailSmimeUnwrappedKey.Failure(lastAlgorithm, lastCertificate, false, true, "The matching recipient key or key-transport algorithm is rejected by policy.");
        if (unsupported)
            return EmailSmimeUnwrappedKey.Failure(lastAlgorithm, lastCertificate, true, false, "The matching CMS recipient uses an unsupported key-transport algorithm.");
        return EmailSmimeUnwrappedKey.Failure(lastAlgorithm, lastCertificate, false, false, "A matching recipient was found, but its content key could not be unwrapped.");
    }

    static EmailSmimeKeyRecipientLayout? ParseRecipient(EmailCmsAsn1Node node, EmailCryptographicLimits limits)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() != 4)
            return null;
        EmailCmsAsn1Node identifier = node.GetChildAt(1);
        EmailCmsAsn1Node algorithm = node.GetChildAt(2);
        EmailCmsAsn1Node encryptedKey = node.GetChildAt(3);
        if (!encryptedKey.HasTag(EmailCmsAsn1TagClass.Universal, 4) || encryptedKey.IsConstructed())
            return null;
        byte[] encrypted = encryptedKey.GetContentBytes();
        if (encrypted.LongLength > limits.GetMaxKeyBytes())
            throw new EmailLimitException("MaxEncryptedKeyBytes", limits.GetMaxKeyBytes(), encrypted.LongLength);
        EmailSmimeKeyTransportAlgorithm kind = ParseKeyTransportAlgorithm(algorithm);
        return new EmailSmimeKeyRecipientLayout(identifier, kind, encrypted);
    }

    static EmailSmimeKeyTransportAlgorithm ParseKeyTransportAlgorithm(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() < 1 || node.GetChildCount() > 2)
            return EmailSmimeKeyTransportAlgorithm.Unknown;
        string? oid = ReadOid(node.GetChildAt(0));
        if (oid == EmailSmimeCms.RsaEncryptionOid)
        {
            if (node.GetChildCount() == 2 && (!node.GetChildAt(1).HasTag(EmailCmsAsn1TagClass.Universal, 5) || node.GetChildAt(1).GetContentBytes().Length != 0))
                return EmailSmimeKeyTransportAlgorithm.Unknown;
            return EmailSmimeKeyTransportAlgorithm.RsaPkcs1;
        }

        if (oid != EmailSmimeEnvelopeCms.RsaOaepOid || node.GetChildCount() != 2)
            return EmailSmimeKeyTransportAlgorithm.Unknown;
        return IsSha256OaepParameters(node.GetChildAt(1)) ? EmailSmimeKeyTransportAlgorithm.RsaOaepSha256 : EmailSmimeKeyTransportAlgorithm.Unknown;
    }

    static bool IsSha256OaepParameters(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed())
            return false;
        bool hash = false;
        bool mask = false;
        int index = 0;
        while (index < node.GetChildCount())
        {
            EmailCmsAsn1Node field = node.GetChildAt(index);
            if (!field.IsConstructed() || field.GetChildCount() != 1)
                return false;
            if (field.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0))
                hash = IsAlgorithm(field.GetChildAt(0), EmailSmimeCms.Sha256Oid);
            else if (field.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 1))
            {
                EmailCmsAsn1Node mgf = field.GetChildAt(0);
                mask = mgf.HasTag(EmailCmsAsn1TagClass.Universal, 16) && mgf.IsConstructed() && mgf.GetChildCount() == 2 && ReadOid(mgf.GetChildAt(0)) == EmailSmimeEnvelopeCms.Mgf1Oid && IsAlgorithm(mgf.GetChildAt(1), EmailSmimeCms.Sha256Oid);
            }
            else if (!field.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 2))
                return false;
            index++;
        }

        return hash && mask;
    }

    static bool IsAlgorithm(EmailCmsAsn1Node node, string expectedOid)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 16) || !node.IsConstructed() || node.GetChildCount() < 1 || node.GetChildCount() > 2 || ReadOid(node.GetChildAt(0)) != expectedOid)
            return false;
        return node.GetChildCount() == 1 || node.GetChildAt(1).HasTag(EmailCmsAsn1TagClass.Universal, 5) && node.GetChildAt(1).GetContentBytes().Length == 0;
    }

    static bool Matches(EmailCmsAsn1Node identifier, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        if (identifier.HasTag(EmailCmsAsn1TagClass.Universal, 16) && identifier.IsConstructed() && identifier.GetChildCount() == 2)
        {
            byte[]? serial = NormalizeUnsigned(identifier.GetChildAt(1));
            return serial != null && identifier.GetChildAt(0).GetEncodedBytes().AsSpan().SequenceEqual(certificate.IssuerName.RawData) && serial.AsSpan().SequenceEqual(EmailSmimeCms.SerialBytes(certificate));
        }

        if (identifier.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0) && !identifier.IsConstructed())
        {
            System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension? extension = certificate.Extensions["2.5.29.14"] as System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension;
            return extension != null && Convert.FromHexString((extension.SubjectKeyIdentifier ?? throw new InvalidOperationException())).AsSpan().SequenceEqual(identifier.GetContentBytes());
        }

        return false;
    }

    static byte[]? DecryptContent(EmailSmimeEnvelopeLayout layout, byte[] key, EmailCryptographicLimits limits)
    {
        EmailSmimeEncryptedContentLayout encrypted = layout.GetEncryptedContent();
        if (encrypted.GetCiphertext().LongLength > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxEncryptedContentBytes", limits.GetMaxInputBytes(), encrypted.GetCiphertext().LongLength);
        if (EmailSmimeEnvelopeCms.IsGcm(encrypted.GetAlgorithm()))
        {
            if (layout.GetAuthenticationTag() == null || (layout.GetAuthenticationTag() ?? throw new InvalidOperationException()).Length != encrypted.GetTagLength())
                return null;
            byte[] plaintext = new byte[encrypted.GetCiphertext().Length];
            EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingAction(() =>
            {
                using (AesGcm aes = new AesGcm(key, encrypted.GetTagLength()))
                    aes.Decrypt(encrypted.GetParameters(), encrypted.GetCiphertext(), (layout.GetAuthenticationTag() ?? throw new InvalidOperationException()), plaintext, layout.GetAssociatedData());
            });
            if (captured.IsSuccess())
                return plaintext;
            CryptographicOperations.ZeroMemory(plaintext);
            return null;
        }

        EmailCapturedOperation decrypted = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = encrypted.GetParameters();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    return decryptor.TransformFinalBlock(encrypted.GetCiphertext(), 0, encrypted.GetCiphertext().Length);
            }
        });
        return decrypted.IsSuccess() ? (byte[]? )decrypted.GetValue() : null;
    }

    static byte[]? ReadImplicitOctets(EmailCmsAsn1Node node, long maximumBytes)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.ContextSpecific, 0))
            return null;
        if (!node.IsConstructed())
            return node.GetContentBytes().LongLength <= maximumBytes ? node.GetContentBytes() : throw new EmailLimitException("MaxEncryptedContentBytes", maximumBytes, node.GetContentBytes().LongLength);
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < node.GetChildCount())
        {
            EmailCmsAsn1Node child = node.GetChildAt(index);
            if (!child.HasTag(EmailCmsAsn1TagClass.Universal, 4) || child.IsConstructed())
                return null;
            byte[] content = child.GetContentBytes();
            if ((long)bytes.Count + content.Length > maximumBytes)
                throw new EmailLimitException("MaxEncryptedContentBytes", maximumBytes, (long)bytes.Count + content.Length);
            bytes.AddRange(content);
            index++;
        }

        return bytes.ToArray();
    }

    static int ReadSmallPositiveInteger(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 2) || node.IsConstructed())
            return -1;
        byte[] content = node.GetContentBytes();
        if (content.Length == 0 || content.Length > 4 || (content[0] & 128) != 0)
            return -1;
        int value = 0;
        int index = 0;
        while (index < content.Length)
        {
            value = value * 256 + content[index];
            index++;
        }

        return value;
    }

    static byte[]? NormalizeUnsigned(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 2) || node.IsConstructed())
            return null;
        byte[] content = node.GetContentBytes();
        if (content.Length == 0 || (content[0] & 128) != 0)
            return null;
        int start = 0;
        while (start + 1 < content.Length && content[start] == 0)
            start++;
        byte[] value = new byte[content.Length - start];
        new ReadOnlySpan<byte>(content, start, value.Length).CopyTo(value);
        return value;
    }

    static byte[]? ChangeImplicitSetTag(byte[] encoded, byte expectedTag)
    {
        if (encoded.Length == 0 || encoded[0] != expectedTag)
            return null;
        byte[] copy = new byte[encoded.Length];
        encoded.CopyTo(copy, 0);
        copy[0] = 49;
        return copy;
    }

    static string? ReadOid(EmailCmsAsn1Node node)
    {
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 6) || node.IsConstructed())
            return null;
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            AsnReader reader = new AsnReader(node.GetEncodedBytes(), AsnEncodingRules.BER);
            string oid = reader.ReadObjectIdentifier();
            return reader.HasData ? null : oid;
        });
        return captured.IsSuccess() ? (string? )captured.GetValue() : null;
    }

    static void ValidateCredentials(List<EmailSmimeDecryptionCredential> credentials)
    {
        int index = 0;
        while (index < credentials.Count)
        {
            if (credentials[index] == null)
                throw new ArgumentException("The decryption credential list cannot contain null entries.", "credentials");
            index++;
        }
    }

    static EmailSmimeDecryptionResult Failure(EmailSmimeDecryptionStatus status, EmailIssueCode code, string description, EmailIssueList issues, EmailSmimeContentEncryptionAlgorithm contentAlgorithm, EmailSmimeKeyTransportAlgorithm keyTransportAlgorithm, EmailSmimeCertificateInfo? certificate)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, "No plaintext was released."));
        return new EmailSmimeDecryptionResult(status, null, issues, contentAlgorithm, keyTransportAlgorithm, certificate, false);
    }
}
