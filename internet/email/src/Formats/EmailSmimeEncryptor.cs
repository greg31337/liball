using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
internal class EmailSmimeEncryptedRecipient
{
    X509Certificate2 certificate;
    EmailSmimeKeyTransportAlgorithm algorithm;
    byte[] encryptedKey;

    internal EmailSmimeEncryptedRecipient(X509Certificate2 certificate, EmailSmimeKeyTransportAlgorithm algorithm, byte[] encryptedKey)
    {
        this.certificate = certificate;
        this.algorithm = algorithm;
        this.encryptedKey = encryptedKey;
    }

    internal X509Certificate2 GetCertificate() { return certificate; }
    internal EmailSmimeKeyTransportAlgorithm GetAlgorithm() { return algorithm; }
    internal byte[] GetEncryptedKey() { return encryptedKey; }
}
internal class EmailSmimeEncryptedPayload
{
    EmailSmimeContentEncryptionAlgorithm algorithm;
    byte[] parameters;
    byte[] ciphertext;
    byte[]? authenticationTag;

    internal EmailSmimeEncryptedPayload(EmailSmimeContentEncryptionAlgorithm algorithm, byte[] parameters, byte[] ciphertext, byte[]? authenticationTag)
    {
        this.algorithm = algorithm;
        this.parameters = parameters;
        this.ciphertext = ciphertext;
        this.authenticationTag = authenticationTag;
    }

    internal EmailSmimeContentEncryptionAlgorithm GetAlgorithm() { return algorithm; }
    internal byte[] GetParameters() { return parameters; }
    internal byte[] GetCiphertext() { return ciphertext; }
    internal byte[]? GetAuthenticationTag() { return authenticationTag; }
}
internal static class EmailSmimeEnvelopeCms
{
    internal const string RsaOaepOid = "1.2.840.113549.1.1.7";
    internal const string Mgf1Oid = "1.2.840.113549.1.1.8";
    internal const string Aes128CbcOid = "2.16.840.1.101.3.4.1.2";
    internal const string Aes256CbcOid = "2.16.840.1.101.3.4.1.42";
    internal const string Aes128GcmOid = "2.16.840.1.101.3.4.1.6";
    internal const string Aes256GcmOid = "2.16.840.1.101.3.4.1.46";

    internal static int KeyBytes(EmailSmimeContentEncryptionAlgorithm algorithm)
    {
        return algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Cbc || algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Gcm ? 16 : 32;
    }

    internal static bool IsGcm(EmailSmimeContentEncryptionAlgorithm algorithm)
    {
        return algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Gcm || algorithm == EmailSmimeContentEncryptionAlgorithm.Aes256Gcm;
    }

    internal static string ContentOid(EmailSmimeContentEncryptionAlgorithm algorithm)
    {
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Cbc) return Aes128CbcOid;
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes256Cbc) return Aes256CbcOid;
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Gcm) return Aes128GcmOid;
        if (algorithm == EmailSmimeContentEncryptionAlgorithm.Aes256Gcm) return Aes256GcmOid;
        return "";
    }

    internal static EmailSmimeContentEncryptionAlgorithm ContentFromOid(string? oid)
    {
        if (oid == Aes128CbcOid) return EmailSmimeContentEncryptionAlgorithm.Aes128Cbc;
        if (oid == Aes256CbcOid) return EmailSmimeContentEncryptionAlgorithm.Aes256Cbc;
        if (oid == Aes128GcmOid) return EmailSmimeContentEncryptionAlgorithm.Aes128Gcm;
        if (oid == Aes256GcmOid) return EmailSmimeContentEncryptionAlgorithm.Aes256Gcm;
        return EmailSmimeContentEncryptionAlgorithm.Unknown;
    }

    internal static byte[] Encode(EmailSmimeEncryptedPayload payload, List<EmailSmimeEncryptedRecipient> recipients)
    {
        AsnWriter envelope = new AsnWriter(AsnEncodingRules.DER);
        envelope.PushSequence();
        envelope.WriteInteger(0);
        WriteRecipientSet(envelope, recipients);
        WriteEncryptedContentInfo(envelope, payload);
        if (IsGcm(payload.GetAlgorithm())) envelope.WriteOctetString(payload.GetAuthenticationTag());
        envelope.PopSequence();
        string contentType = IsGcm(payload.GetAlgorithm()) ? EmailCmsReaderConstants.AuthEnvelopedDataObjectIdentifier : EmailCmsReaderConstants.EnvelopedDataObjectIdentifier;
        AsnWriter contentInfo = new AsnWriter(AsnEncodingRules.DER);
        contentInfo.PushSequence();
        contentInfo.WriteObjectIdentifier(contentType);
        Asn1Tag explicitTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
        contentInfo.PushSequence(explicitTag);
        contentInfo.WriteEncodedValue(envelope.Encode());
        contentInfo.PopSequence(explicitTag);
        contentInfo.PopSequence();
        return contentInfo.Encode();
    }

    static void WriteRecipientSet(AsnWriter writer, List<EmailSmimeEncryptedRecipient> recipients)
    {
        writer.PushSetOf();
        int index = 0;
        while (index < recipients.Count)
        {
            EmailSmimeEncryptedRecipient recipient = recipients[index];
            writer.PushSequence();
            writer.WriteInteger(0);
            writer.PushSequence();
            writer.WriteEncodedValue(recipient.GetCertificate().IssuerName.RawData);
            writer.WriteIntegerUnsigned(EmailSmimeCms.SerialBytes(recipient.GetCertificate()));
            writer.PopSequence();
            WriteKeyTransportAlgorithm(writer, recipient.GetAlgorithm());
            writer.WriteOctetString(recipient.GetEncryptedKey());
            writer.PopSequence();
            index++;
        }
        writer.PopSetOf();
    }

    static void WriteKeyTransportAlgorithm(AsnWriter writer, EmailSmimeKeyTransportAlgorithm algorithm)
    {
        writer.PushSequence();
        if (algorithm == EmailSmimeKeyTransportAlgorithm.RsaPkcs1)
        {
            writer.WriteObjectIdentifier(EmailSmimeCms.RsaEncryptionOid);
            writer.WriteNull();
        }
        else
        {
            writer.WriteObjectIdentifier(RsaOaepOid);
            writer.PushSequence();
            Asn1Tag hashTag = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            writer.PushSequence(hashTag);
            EmailSmimeCms.WriteAlgorithmIdentifier(writer, EmailSmimeCms.Sha256Oid, true);
            writer.PopSequence(hashTag);
            Asn1Tag maskTag = new Asn1Tag(TagClass.ContextSpecific, 1, true);
            writer.PushSequence(maskTag);
            writer.PushSequence();
            writer.WriteObjectIdentifier(Mgf1Oid);
            EmailSmimeCms.WriteAlgorithmIdentifier(writer, EmailSmimeCms.Sha256Oid, true);
            writer.PopSequence();
            writer.PopSequence(maskTag);
            writer.PopSequence();
        }
        writer.PopSequence();
    }

    static void WriteEncryptedContentInfo(AsnWriter writer, EmailSmimeEncryptedPayload payload)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(EmailCmsReaderConstants.DataObjectIdentifier);
        writer.PushSequence();
        writer.WriteObjectIdentifier(ContentOid(payload.GetAlgorithm()));
        if (IsGcm(payload.GetAlgorithm()))
        {
            writer.PushSequence();
            writer.WriteOctetString(payload.GetParameters());
            writer.WriteInteger((payload.GetAuthenticationTag() ?? throw new InvalidOperationException()).Length);
            writer.PopSequence();
        }
        else
        {
            writer.WriteOctetString(payload.GetParameters());
        }
        writer.PopSequence();
        writer.WriteOctetString(payload.GetCiphertext(), new Asn1Tag(TagClass.ContextSpecific, 0));
        writer.PopSequence();
    }
}
