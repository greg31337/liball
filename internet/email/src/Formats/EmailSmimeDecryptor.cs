using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
internal class EmailSmimeEnvelopeLayout
{
    EmailCmsAsn1Node recipients;
    EmailSmimeEncryptedContentLayout encryptedContent;
    byte[]? authenticationTag;
    byte[] associatedData;

    internal EmailSmimeEnvelopeLayout(EmailCmsAsn1Node recipients, EmailSmimeEncryptedContentLayout encryptedContent, byte[]? authenticationTag, byte[] associatedData)
    {
        this.recipients = recipients;
        this.encryptedContent = encryptedContent;
        this.authenticationTag = authenticationTag;
        this.associatedData = associatedData;
    }

    internal int GetRecipientCount() { return recipients.GetChildCount(); }
    internal EmailCmsAsn1Node GetRecipientAt(int index) { return recipients.GetChildAt(index); }
    internal EmailSmimeEncryptedContentLayout GetEncryptedContent() { return encryptedContent; }
    internal EmailSmimeContentEncryptionAlgorithm GetContentAlgorithm() { return encryptedContent.GetAlgorithm(); }
    internal byte[]? GetAuthenticationTag() { return authenticationTag; }
    internal byte[] GetAssociatedData() { return associatedData; }
}
internal class EmailSmimeEncryptedContentLayout
{
    EmailSmimeContentEncryptionAlgorithm algorithm;
    byte[] parameters;
    byte[] ciphertext;
    int tagLength;

    internal EmailSmimeEncryptedContentLayout(EmailSmimeContentEncryptionAlgorithm algorithm, byte[] parameters, byte[] ciphertext, int tagLength)
    {
        this.algorithm = algorithm;
        this.parameters = parameters;
        this.ciphertext = ciphertext;
        this.tagLength = tagLength;
    }

    internal EmailSmimeContentEncryptionAlgorithm GetAlgorithm() { return algorithm; }
    internal byte[] GetParameters() { return parameters; }
    internal byte[] GetCiphertext() { return ciphertext; }
    internal int GetTagLength() { return tagLength; }
}
internal class EmailSmimeKeyRecipientLayout
{
    EmailCmsAsn1Node identifier;
    EmailSmimeKeyTransportAlgorithm algorithm;
    byte[] encryptedKey;

    internal EmailSmimeKeyRecipientLayout(EmailCmsAsn1Node identifier, EmailSmimeKeyTransportAlgorithm algorithm, byte[] encryptedKey)
    {
        this.identifier = identifier;
        this.algorithm = algorithm;
        this.encryptedKey = encryptedKey;
    }

    internal EmailCmsAsn1Node GetIdentifier() { return identifier; }
    internal EmailSmimeKeyTransportAlgorithm GetAlgorithm() { return algorithm; }
    internal byte[] GetEncryptedKey() { return encryptedKey; }
}
internal class EmailSmimeUnwrappedKey
{
    byte[]? key;
    EmailSmimeKeyTransportAlgorithm algorithm;
    EmailSmimeCertificateInfo? certificate;
    bool unsupported;
    bool policyRejected;
    string failureReason;

    EmailSmimeUnwrappedKey(byte[]? key, EmailSmimeKeyTransportAlgorithm algorithm, EmailSmimeCertificateInfo? certificate, bool unsupported, bool policyRejected, string failureReason)
    {
        this.key = key;
        this.algorithm = algorithm;
        this.certificate = certificate;
        this.unsupported = unsupported;
        this.policyRejected = policyRejected;
        this.failureReason = failureReason;
    }

    internal static EmailSmimeUnwrappedKey Success(byte[]? key, EmailSmimeKeyTransportAlgorithm algorithm, EmailSmimeCertificateInfo certificate) { return new EmailSmimeUnwrappedKey(key, algorithm, certificate, false, false, ""); }
    internal static EmailSmimeUnwrappedKey Failure(EmailSmimeKeyTransportAlgorithm algorithm, EmailSmimeCertificateInfo? certificate, bool unsupported, bool policyRejected, string reason) { return new EmailSmimeUnwrappedKey(null, algorithm, certificate, unsupported, policyRejected, reason); }
    internal bool HasKey() { return key != null; }
    internal byte[]? GetKey() { return key; }
    internal EmailSmimeKeyTransportAlgorithm GetAlgorithm() { return algorithm; }
    internal EmailSmimeCertificateInfo? GetCertificateInfo() { return certificate; }
    internal bool IsUnsupported() { return unsupported; }
    internal bool IsPolicyRejected() { return policyRejected; }
    internal string GetFailureReason() { return failureReason; }
}
