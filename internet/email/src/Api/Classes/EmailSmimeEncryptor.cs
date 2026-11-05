using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Provides operations for working with S/MIME encryptor.
/// </summary>
public class EmailSmimeEncryptor : IEmailSmimeEncryptorContract
{
    internal static EmailSmimeEncryptionResult? EncryptImplementationWithContentAndRecipients(ReadOnlySpan<byte> content, List<X509Certificate2> recipients)
    {
        return EncryptImplementationWithContentAndRecipientsAndOptions(content, recipients, new EmailSmimeEncryptionOptions());
    }

    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <returns>The S/MIME encryption result produced by the operation.</returns>
    public EmailSmimeEncryptionResult? EncryptWithContentAndRecipients(ReadOnlySpan<byte> content, List<X509Certificate2> recipients)
    {
        return EmailSmimeEncryptor.EncryptImplementationWithContentAndRecipients(content, recipients);
    }

    internal static EmailSmimeEncryptionResult? EncryptImplementationWithContentAndRecipientsAndOptions(ReadOnlySpan<byte> content, List<X509Certificate2> recipients, EmailSmimeEncryptionOptions options)
    {
        if (recipients == null)
            throw new ArgumentNullException("recipients");
        if (options == null)
            throw new ArgumentNullException("options");
        ValidateRecipients(recipients);
        EmailSmimeEncryptionOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (content.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxSmimeContentBytes", limits.GetMaxInputBytes(), content.Length);
        if (recipients.Count > limits.GetMaxRecipientCount())
            throw new EmailLimitException("MaxRecipientCount", limits.GetMaxRecipientCount(), recipients.Count);
        if ((long)recipients.Count + 1 > limits.GetMaxOperationCount())
            throw new EmailLimitException("MaxCryptographicOperationCount", limits.GetMaxOperationCount(), recipients.Count + 1);
        EmailIssueList issues = new EmailIssueList();
        if (recipients.Count == 0)
        {
            issues.Add(Issue(EmailIssueCode.SmimeEncryptionFailed, "At least one S/MIME encryption recipient is required.", "No CMS output was emitted."));
            return new EmailSmimeEncryptionResult(new byte[0], issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), 0);
        }

        EmailSmimeEncryptionPolicy policy = snapshot.GetPolicy();
        if (!policy.IsContentAlgorithmAllowed(snapshot.GetContentAlgorithm()) || !policy.IsKeyTransportAllowed(snapshot.GetKeyTransportAlgorithm(), true))
        {
            issues.Add(Issue(EmailIssueCode.SmimeEncryptionPolicyRejected, "The requested content or key-transport algorithm is prohibited for encryption.", "Select algorithms enabled by the encryption policy."));
            return new EmailSmimeEncryptionResult(new byte[0], issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), recipients.Count);
        }

        long certificateBytes = 0;
        int index = 0;
        while (index < recipients.Count)
        {
            certificateBytes += recipients[index].RawData.Length;
            if (certificateBytes > limits.GetMaxCertificateBytes())
                throw new EmailLimitException("MaxCertificateBytes", limits.GetMaxCertificateBytes(), certificateBytes);
            using (RSA? rsa = recipients[index].GetRSAPublicKey())
            {
                if (rsa == null)
                {
                    issues.Add(Issue(EmailIssueCode.SmimeKeyTransportUnsupported, "An encryption recipient does not have an RSA public key.", "Use an RSA recipient certificate for the supported key-transport algorithms."));
                    return new EmailSmimeEncryptionResult(new byte[0], issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), recipients.Count);
                }

                if (!policy.IsRsaKeySizeAllowed(rsa.KeySize))
                {
                    issues.Add(Issue(EmailIssueCode.SmimeEncryptionPolicyRejected, "A recipient RSA key violates the configured size policy.", "Use a recipient key within the configured size range."));
                    return new EmailSmimeEncryptionResult(new byte[0], issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), recipients.Count);
                }
            }

            index++;
        }

        byte[] contentKey = RandomNumberGenerator.GetBytes(EmailSmimeEnvelopeCms.KeyBytes(snapshot.GetContentAlgorithm()));
        byte[] contentBytes = content.ToArray();
        EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() =>
        {
            EmailSmimeEncryptedPayload payload = EncryptContent(contentBytes, contentKey, snapshot.GetContentAlgorithm());
            List<EmailSmimeEncryptedRecipient> encryptedRecipients = EncryptKeys(contentKey, recipients, snapshot.GetKeyTransportAlgorithm());
            byte[] cms = EmailSmimeEnvelopeCms.Encode(payload, encryptedRecipients);
            if (cms.LongLength > limits.GetMaxInputBytes())
                throw new EmailLimitException("MaxCmsOutputBytes", limits.GetMaxInputBytes(), cms.LongLength);
            EmailCmsReaderOptions readerOptions = new EmailCmsReaderOptions();
            readerOptions.SetLimits(limits);
            readerOptions.SetAllowBer(false);
            EmailCmsReadResult validation = new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(cms, readerOptions);
            if (!validation.IsSuccess())
            {
                issues.Add(Issue(EmailIssueCode.SmimeEncryptionFailed, "The generated CMS envelope failed bounded DER validation.", "The invalid output was withheld."));
                return new EmailSmimeEncryptionResult(new byte[0], issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), recipients.Count);
            }

            return new EmailSmimeEncryptionResult(cms, issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), recipients.Count);
        });
        CryptographicOperations.ZeroMemory(contentBytes);
        CryptographicOperations.ZeroMemory(contentKey);
        if (captured.IsSuccess())
            return (EmailSmimeEncryptionResult? )captured.GetValue();
        Exception? exception = captured.GetException();
        if (exception is CryptographicException || exception is ArgumentException || exception is InvalidOperationException)
        {
            issues.Add(Issue(EmailIssueCode.SmimeEncryptionFailed, "S/MIME encryption failed without producing output.", "Check recipient keys and enabled cryptographic providers."));
            return new EmailSmimeEncryptionResult(new byte[0], issues, snapshot.GetContentAlgorithm(), snapshot.GetKeyTransportAlgorithm(), recipients.Count);
        }

        throw (exception ?? throw new InvalidOperationException());
    }

    /// <summary>
    /// Encrypts the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The S/MIME encryption result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailSmimeEncryptionResult? EncryptWithContentAndRecipientsAndOptions(ReadOnlySpan<byte> content, List<X509Certificate2> recipients, EmailSmimeEncryptionOptions options)
    {
        return EmailSmimeEncryptor.EncryptImplementationWithContentAndRecipientsAndOptions(content, recipients, options);
    }

    static EmailSmimeEncryptedPayload EncryptContent(byte[] content, byte[] key, EmailSmimeContentEncryptionAlgorithm algorithm)
    {
        if (EmailSmimeEnvelopeCms.IsGcm(algorithm))
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] ciphertext = new byte[content.Length];
            byte[] tag = new byte[16];
            using (AesGcm aes = new AesGcm(key, tag.Length))
                aes.Encrypt(nonce, content, ciphertext, tag);
            return new EmailSmimeEncryptedPayload(algorithm, nonce, ciphertext, tag);
        }

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] ciphertext = encryptor.TransformFinalBlock(content, 0, content.Length);
                return new EmailSmimeEncryptedPayload(algorithm, aes.IV, ciphertext, null);
            }
        }
    }

    static List<EmailSmimeEncryptedRecipient> EncryptKeys(byte[] key, List<X509Certificate2> recipients, EmailSmimeKeyTransportAlgorithm algorithm)
    {
        List<EmailSmimeEncryptedRecipient> values = new List<EmailSmimeEncryptedRecipient>();
        RSAEncryptionPadding padding = algorithm == EmailSmimeKeyTransportAlgorithm.RsaOaepSha256 ? RSAEncryptionPadding.OaepSHA256 : RSAEncryptionPadding.Pkcs1;
        int index = 0;
        while (index < recipients.Count)
        {
            using (RSA? rsa = recipients[index].GetRSAPublicKey())
            {
                values.Add(new EmailSmimeEncryptedRecipient(recipients[index], algorithm, (rsa ?? throw new InvalidOperationException()).Encrypt(key, padding)));
            }

            index++;
        }

        return values;
    }

    static void ValidateRecipients(List<X509Certificate2> recipients)
    {
        int index = 0;
        while (index < recipients.Count)
        {
            if (recipients[index] == null)
                throw new ArgumentException("The recipient list cannot contain null entries.", "recipients");
            index++;
        }
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, recovery);
    }
}
