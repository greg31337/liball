using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
internal class EmailSmimePreparedSigner
{
    X509Certificate2 certificate;
    EmailSmimeDigestAlgorithm digestAlgorithm;
    EmailSmimeSignatureAlgorithm signatureAlgorithm;
    byte[] signedAttributes;
    byte[] signature;

    internal EmailSmimePreparedSigner(X509Certificate2 certificate, EmailSmimeDigestAlgorithm digestAlgorithm, EmailSmimeSignatureAlgorithm signatureAlgorithm, byte[] signedAttributes, byte[] signature)
    {
        this.certificate = certificate;
        this.digestAlgorithm = digestAlgorithm;
        this.signatureAlgorithm = signatureAlgorithm;
        this.signedAttributes = signedAttributes;
        this.signature = signature;
    }

    internal X509Certificate2 GetCertificate() { return certificate; }
    internal EmailSmimeDigestAlgorithm GetDigestAlgorithm() { return digestAlgorithm; }
    internal EmailSmimeSignatureAlgorithm GetSignatureAlgorithm() { return signatureAlgorithm; }
    internal byte[] GetSignedAttributes() { return signedAttributes; }
    internal byte[] GetSignature() { return signature; }
}
internal static class EmailSmimeCms
{
    internal const string Sha1Oid = "1.3.14.3.2.26";
    internal const string Sha256Oid = "2.16.840.1.101.3.4.2.1";
    internal const string Sha384Oid = "2.16.840.1.101.3.4.2.2";
    internal const string Sha512Oid = "2.16.840.1.101.3.4.2.3";
    internal const string RsaEncryptionOid = "1.2.840.113549.1.1.1";
    internal const string Sha1WithRsaOid = "1.2.840.113549.1.1.5";
    internal const string Sha256WithRsaOid = "1.2.840.113549.1.1.11";
    internal const string Sha384WithRsaOid = "1.2.840.113549.1.1.12";
    internal const string Sha512WithRsaOid = "1.2.840.113549.1.1.13";
    internal const string EcdsaSha256Oid = "1.2.840.10045.4.3.2";
    internal const string EcdsaSha384Oid = "1.2.840.10045.4.3.3";
    internal const string EcdsaSha512Oid = "1.2.840.10045.4.3.4";
    internal const string ContentTypeAttributeOid = "1.2.840.113549.1.9.3";
    internal const string MessageDigestAttributeOid = "1.2.840.113549.1.9.4";

    internal static string DigestOid(EmailSmimeDigestAlgorithm algorithm)
    {
        if (algorithm == EmailSmimeDigestAlgorithm.Sha1) return Sha1Oid;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha256) return Sha256Oid;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha384) return Sha384Oid;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha512) return Sha512Oid;
        return "";
    }

    internal static EmailSmimeDigestAlgorithm DigestFromOid(string? oid)
    {
        if (oid == Sha1Oid) return EmailSmimeDigestAlgorithm.Sha1;
        if (oid == Sha256Oid) return EmailSmimeDigestAlgorithm.Sha256;
        if (oid == Sha384Oid) return EmailSmimeDigestAlgorithm.Sha384;
        if (oid == Sha512Oid) return EmailSmimeDigestAlgorithm.Sha512;
        return EmailSmimeDigestAlgorithm.Unknown;
    }

    internal static string SignatureOid(EmailSmimeSignatureAlgorithm signature, EmailSmimeDigestAlgorithm digest)
    {
        if (signature == EmailSmimeSignatureAlgorithm.RsaPkcs1) return RsaEncryptionOid;
        if (digest == EmailSmimeDigestAlgorithm.Sha256) return EcdsaSha256Oid;
        if (digest == EmailSmimeDigestAlgorithm.Sha384) return EcdsaSha384Oid;
        if (digest == EmailSmimeDigestAlgorithm.Sha512) return EcdsaSha512Oid;
        return "";
    }

    internal static EmailSmimeSignatureAlgorithm SignatureFromOid(string? oid)
    {
        if (oid == RsaEncryptionOid || oid == Sha1WithRsaOid || oid == Sha256WithRsaOid || oid == Sha384WithRsaOid || oid == Sha512WithRsaOid) return EmailSmimeSignatureAlgorithm.RsaPkcs1;
        if (oid == EcdsaSha256Oid || oid == EcdsaSha384Oid || oid == EcdsaSha512Oid) return EmailSmimeSignatureAlgorithm.Ecdsa;
        return EmailSmimeSignatureAlgorithm.Unknown;
    }

    internal static bool SignatureDigestMatchesOid(string? oid, EmailSmimeDigestAlgorithm digest)
    {
        if (oid == RsaEncryptionOid) return true;
        if (oid == Sha1WithRsaOid) return digest == EmailSmimeDigestAlgorithm.Sha1;
        if (oid == Sha256WithRsaOid || oid == EcdsaSha256Oid) return digest == EmailSmimeDigestAlgorithm.Sha256;
        if (oid == Sha384WithRsaOid || oid == EcdsaSha384Oid) return digest == EmailSmimeDigestAlgorithm.Sha384;
        if (oid == Sha512WithRsaOid || oid == EcdsaSha512Oid) return digest == EmailSmimeDigestAlgorithm.Sha512;
        return false;
    }

    internal static HashAlgorithmName HashName(EmailSmimeDigestAlgorithm algorithm)
    {
        if (algorithm == EmailSmimeDigestAlgorithm.Sha1) return HashAlgorithmName.SHA1;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha256) return HashAlgorithmName.SHA256;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha384) return HashAlgorithmName.SHA384;
        if (algorithm == EmailSmimeDigestAlgorithm.Sha512) return HashAlgorithmName.SHA512;
        return default(HashAlgorithmName);
    }

    internal static byte[] Hash(byte[] value, EmailSmimeDigestAlgorithm algorithm)
    {
        if (algorithm == EmailSmimeDigestAlgorithm.Sha1) return SHA1.HashData(value);
        if (algorithm == EmailSmimeDigestAlgorithm.Sha256) return SHA256.HashData(value);
        if (algorithm == EmailSmimeDigestAlgorithm.Sha384) return SHA384.HashData(value);
        if (algorithm == EmailSmimeDigestAlgorithm.Sha512) return SHA512.HashData(value);
        throw new CryptographicException("Unsupported CMS digest algorithm.");
    }

    internal static byte[] EncodeSignedAttributes(string contentType, byte[] digest)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSetOf();
        writer.PushSequence();
        writer.WriteObjectIdentifier(ContentTypeAttributeOid);
        writer.PushSetOf();
        writer.WriteObjectIdentifier(contentType);
        writer.PopSetOf();
        writer.PopSequence();
        writer.PushSequence();
        writer.WriteObjectIdentifier(MessageDigestAttributeOid);
        writer.PushSetOf();
        writer.WriteOctetString(digest);
        writer.PopSetOf();
        writer.PopSequence();
        writer.PopSetOf();
        return writer.Encode();
    }

    internal static void WriteAlgorithmIdentifier(AsnWriter writer, string oid, bool writeNull)
    {
        writer.PushSequence();
        writer.WriteObjectIdentifier(oid);
        if (writeNull) writer.WriteNull();
        writer.PopSequence();
    }

    internal static byte[] ChangeSetToContextZero(byte[] encodedSet)
    {
        byte[] value = Copy(encodedSet);
        if (value.Length == 0 || value[0] != 49) throw new CryptographicException("Signed attributes are not a DER SET OF value.");
        value[0] = 160;
        return value;
    }

    internal static byte[] ChangeContextZeroToSet(byte[] encodedAttributes)
    {
        byte[] value = Copy(encodedAttributes);
        if (value.Length == 0 || value[0] != 160) throw new CryptographicException("Signed attributes do not use context-specific tag zero.");
        value[0] = 49;
        return value;
    }

    internal static byte[] SerialBytes(X509Certificate2 certificate)
    {
        byte[] littleEndian = certificate.GetSerialNumber();
        Array.Reverse(littleEndian);
        int start = 0;
        while (start + 1 < littleEndian.Length && littleEndian[start] == 0) start++;
        byte[] value = new byte[littleEndian.Length - start];
        new ReadOnlySpan<byte>(littleEndian, start, value.Length).CopyTo(value);
        return value;
    }

    static byte[] Copy(byte[] value) { byte[] copy = new byte[value.Length]; value.CopyTo(copy, 0); return copy; }
}
