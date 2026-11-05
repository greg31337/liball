using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace email;

internal static class EmailOpenPgpMimeUtility
{
    internal static byte[]? CanonicalEntity(MimeEntity entity, EmailCryptographicLimits limits)
    {
        if (entity == null) throw new ArgumentNullException("entity");
        EmailWriteResult written = new EmailWriter().WriteEntityWithEntityAndOptions(entity, new EmailWriteOptionsOperations().CreateCanonicalInternet());
        if (!written.IsSuccess()) return null;
        byte[] bytes = written.GetBytes();
        Bound(bytes.Length, limits.GetMaxInputBytes(), "MaxOpenPgpMimeContentBytes");
        return bytes;
    }

    internal static bool IsSafeSignedMime(ReadOnlySpan<byte> content)
    {
        int lineStart = 0;
        int index = 0;
        while (index < content.Length)
        {
            if (content[index] > 127) return false;
            if (content[index] == 10)
            {
                if (index == 0 || content[index - 1] != 13) return false;
                int end = index - 1;
                if (end > lineStart && (content[end - 1] == 32 || content[end - 1] == 9)) return false;
                lineStart = index + 1;
            }
            else if (content[index] == 13 && (index + 1 >= content.Length || content[index + 1] != 10)) return false;
            index++;
        }
        return lineStart == content.Length || NoTrailingWhitespace(content.Slice(lineStart));
    }

    internal static byte[]? SignatureHashInput(ReadOnlySpan<byte> content, EmailOpenPgpSignaturePacket signature)
    {
        int version = signature.GetVersion();
        if (version == 3) return Join([content.ToArray(), signature.GetHashedSubpackets()]);
        if (version != 4 && version != 6) return null;
        byte[] hashed = signature.GetHashedSubpackets();
        int lengthBytes = version == 4 ? 2 : 4;
        byte[] fields = new byte[4 + lengthBytes + hashed.Length];
        fields[0] = (byte)version;
        fields[1] = (byte)signature.GetSignatureType();
        fields[2] = (byte)signature.GetPublicKeyAlgorithm();
        fields[3] = (byte)signature.GetHashAlgorithm();
        WriteUnsigned(fields, 4, (uint)hashed.Length, lengthBytes);
        hashed.CopyTo(fields, 4 + lengthBytes);
        byte[] trailer = new byte[6];
        trailer[0] = (byte)version;
        trailer[1] = 0xFF;
        WriteUnsigned(trailer, 2, (uint)fields.Length, 4);
        return version == 6 ? Join([signature.GetSalt(), content.ToArray(), fields, trailer]) : Join([content.ToArray(), fields, trailer]);
    }

    internal static EmailOpenPgpSignatureGeneration SignV4(ReadOnlySpan<byte> content, EmailOpenPgpSigningCredential credential, uint creationTime, EmailOpenPgpAlgorithmOptions options)
    {
        byte[] hashed = [5, 0x82, (byte)(creationTime >> 24), (byte)(creationTime >> 16 & 255), (byte)(creationTime >> 8 & 255), (byte)(creationTime & 255)];
        byte[] keyId = credential.GetKeyIdentifier();
        byte[] unhashed = [9, 16, keyId[0], keyId[1], keyId[2], keyId[3], keyId[4], keyId[5], keyId[6], keyId[7]];
        byte[] fields = Join([[(byte)4, (byte)0, (byte)credential.GetPublicKeyAlgorithm(), (byte)credential.GetHashAlgorithm(), 0, (byte)hashed.Length], hashed]);
        byte[] trailer = [4, 0xFF, 0, 0, 0, (byte)fields.Length];
        byte[] hashInput = Join([content.ToArray(), fields, trailer]);
        EmailOpenPgpOperationResult digest = new EmailOpenPgpAlgorithms().ComputeHashWithDataAndHashAlgorithmAndOptions(hashInput, credential.GetHashAlgorithm(), options);
        if (!digest.IsSuccess()) return new EmailOpenPgpSignatureGeneration(null, digest);
        EmailOpenPgpOperationResult? operation = new EmailOpenPgpAlgorithms().SignHashWithHashAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(digest.GetOutput(), credential.GetHashAlgorithm(), credential.GetPublicKeyAlgorithm(), credential.GetPrivateKey(), options);
        if (!(operation ?? throw new InvalidOperationException()).IsSuccess()) { digest.Dispose(); return new EmailOpenPgpSignatureGeneration(null, operation); }
        byte[] hash = digest.GetOutput();
        byte[] body = Join([fields, [0, (byte)unhashed.Length], unhashed, [hash[0], hash[1]], operation.GetOutput()]);
        byte[] packet = Packet(2, body);
        CryptographicOperations.ZeroMemory(hash);
        digest.Dispose();
        return new EmailOpenPgpSignatureGeneration(packet, operation);
    }

    internal static byte[] Packet(int typeIdentifier, ReadOnlySpan<byte> body)
    {
        if (typeIdentifier < 0 || typeIdentifier > 63) throw new ArgumentOutOfRangeException("typeIdentifier");
        byte[] length;
        if (body.Length < 192) length = [(byte)body.Length];
        else if (body.Length <= 8383)
        {
            int adjusted = body.Length - 192;
            length = [(byte)((adjusted >> 8) + 192), (byte)(adjusted & 255)];
        }
        else length = [255, (byte)(body.Length >> 24 & 255), (byte)(body.Length >> 16 & 255), (byte)(body.Length >> 8 & 255), (byte)(body.Length & 255)];
        byte[] result = new byte[1 + length.Length + body.Length];
        result[0] = (byte)(0xC0 | typeIdentifier);
        length.CopyTo(result, 1);
        body.CopyTo(new Span<byte>(result, 1 + length.Length, body.Length));
        return result;
    }

    internal static byte[] LiteralPacket(ReadOnlySpan<byte> content)
    {
        byte[] body = new byte[6 + content.Length];
        body[0] = (byte)'b';
        content.CopyTo(new Span<byte>(body, 6, content.Length));
        return Packet(11, body);
    }

    internal static byte[] SignedMime(ReadOnlySpan<byte> signedEntity, ReadOnlySpan<byte> armoredSignature, int hashAlgorithm)
    {
        string boundary = Boundary([signedEntity.ToArray(), armoredSignature.ToArray()]);
        string headers = "Content-Type: multipart/signed; boundary=\"" + boundary + "\"; protocol=\"application/pgp-signature\"; micalg=" + MicAlgorithm(hashAlgorithm) + "\r\n\r\n--" + boundary + "\r\n";
        string signatureHeaders = "\r\n--" + boundary + "\r\nContent-Type: application/pgp-signature; name=\"signature.asc\"\r\nContent-Disposition: attachment; filename=\"signature.asc\"\r\nContent-Transfer-Encoding: 7bit\r\n\r\n";
        string ending = "\r\n--" + boundary + "--\r\n";
        return Join([Encoding.ASCII.GetBytes(headers), signedEntity.ToArray(), Encoding.ASCII.GetBytes(signatureHeaders), armoredSignature.ToArray(), Encoding.ASCII.GetBytes(ending)]);
    }

    internal static byte[] EncryptedMime(ReadOnlySpan<byte> armoredMessage)
    {
        string boundary = Boundary([armoredMessage.ToArray()]);
        string start = "Content-Type: multipart/encrypted; boundary=\"" + boundary + "\"; protocol=\"application/pgp-encrypted\"\r\n\r\n--" + boundary + "\r\nContent-Type: application/pgp-encrypted\r\nContent-Transfer-Encoding: 7bit\r\n\r\nVersion: 1\r\n\r\n--" + boundary + "\r\nContent-Type: application/octet-stream; name=\"encrypted.asc\"\r\nContent-Disposition: inline; filename=\"encrypted.asc\"\r\nContent-Transfer-Encoding: 7bit\r\n\r\n";
        string end = "\r\n--" + boundary + "--\r\n";
        return Join([Encoding.ASCII.GetBytes(start), armoredMessage.ToArray(), Encoding.ASCII.GetBytes(end)]);
    }

    internal static byte[]? EntityContent(MimeEntity? entity, EmailCryptographicLimits limits)
    {
        if (entity == null || !entity.HasContent()) return null;
        EmailContent? content = entity.HasDecodedContent() ? entity.GetDecodedContent() : entity.GetContent();
        return (content ?? throw new InvalidOperationException()).Materialize(limits.GetMaxInputBytes());
    }

    internal static byte[] SignerIdentifier(EmailOpenPgpSignaturePacket signature)
    {
        byte[]? value = Subpacket(signature.GetParsedHashedSubpackets(), 33);
        if (value == null) value = Subpacket(signature.GetParsedUnhashedSubpackets(), 33);
        if (value != null && (value.Length == 21 || value.Length == 33)) return Slice(value, 1, value.Length - 1);
        value = Subpacket(signature.GetParsedHashedSubpackets(), 16);
        if (value == null) value = Subpacket(signature.GetParsedUnhashedSubpackets(), 16);
        if (value != null && value.Length == 8) return value;
        return signature.GetIssuerKeyIdentifier();
    }

    internal static EmailOpenPgpVerificationKey? FindVerificationKey(List<EmailOpenPgpVerificationKey> keys, byte[] identifier, int publicKeyAlgorithm)
    {
        int index = 0;
        while (index < keys.Count)
        {
            EmailOpenPgpVerificationKey key = keys[index];
            if (key.GetPublicKeyAlgorithm() == publicKeyAlgorithm && IdentifierMatches(key.GetIdentifier(), identifier)) return key;
            index++;
        }
        return null;
    }

    internal static bool IdentifierMatches(byte[] available, byte[] requested)
    {
        if (requested.Length == 0) return true;
        if (available.AsSpan().SequenceEqual(requested)) return true;
        if (requested.Length == 8 && available.Length == 20) return available.AsSpan(12, 8).SequenceEqual(requested);
        if (requested.Length == 8 && available.Length == 32) return available.AsSpan(0, 8).SequenceEqual(requested);
        if (available.Length == 8 && requested.Length == 20) return requested.AsSpan(12, 8).SequenceEqual(available);
        if (available.Length == 8 && requested.Length == 32) return requested.AsSpan(0, 8).SequenceEqual(available);
        return false;
    }

    internal static string MicAlgorithm(int hashAlgorithm)
    {
        if (hashAlgorithm == 2) return "pgp-sha1";
        if (hashAlgorithm == 8) return "pgp-sha256";
        if (hashAlgorithm == 9) return "pgp-sha384";
        if (hashAlgorithm == 10) return "pgp-sha512";
        if (hashAlgorithm == 12) return "pgp-sha3-256";
        if (hashAlgorithm == 14) return "pgp-sha3-512";
        return "pgp-unknown";
    }

    internal static byte[] Join(byte[][] values)
    {
        long total = 0;
        int index = 0;
        while (index < values.Length) { if (values[index] == null) throw new ArgumentNullException("values"); total += values[index].Length; index++; }
        if (total > int.MaxValue) throw new EmailLimitException("MaxOpenPgpCombinedBytes", int.MaxValue, total);
        byte[] result = new byte[(int)total];
        int position = 0;
        index = 0;
        while (index < values.Length) { values[index].CopyTo(result, position); position += values[index].Length; index++; }
        return result;
    }

    internal static byte[] Slice(byte[]? value, int offset, int count) { byte[] result = new byte[count]; Array.Copy((value ?? throw new InvalidOperationException()), offset, result, 0, count); return result; }
    internal static void WriteUnsigned(byte[] output, int offset, ulong value, int count) { int index = count - 1; while (index >= 0) { output[offset + index] = (byte)(value & 255); value >>= 8; index--; } }
    internal static void Bound(long observed, long limit, string name) { if (observed > limit) throw new EmailLimitException(name, limit, observed); }
    internal static EmailIssue Issue(EmailIssueCode code, string description, string recovery) { return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, null, recovery); }

    static byte[]? Subpacket(List<EmailOpenPgpSignatureSubpacket> values, int type)
    {
        int index = 0;
        while (index < values.Count) { if (values[index].GetTypeIdentifier() == type) return values[index].GetData(); index++; }
        return null;
    }

    static string Boundary(byte[][] invalid)
    {
        int attempt = 0;
        while (attempt < 32)
        {
            string candidate = "=_OpenPgp_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
            byte[] bytes = Encoding.ASCII.GetBytes(candidate);
            bool found = false;
            int index = 0;
            while (index < invalid.Length && !found) { found = invalid[index].IndexOf(bytes) >= 0; index++; }
            if (!found) return candidate;
            attempt++;
        }
        throw new CryptographicException("A collision-free MIME boundary could not be generated.");
    }

    static bool NoTrailingWhitespace(ReadOnlySpan<byte> value) { return value.Length == 0 || value[value.Length - 1] != 32 && value[value.Length - 1] != 9; }
}
internal class EmailOpenPgpSignatureGeneration
{
    byte[]? signature;
    EmailOpenPgpOperationResult operation;

    internal EmailOpenPgpSignatureGeneration(byte[]? signature, EmailOpenPgpOperationResult operation)
    {
        this.signature = signature;
        this.operation = operation;
    }

    internal byte[]? GetSignature() { return signature; }
    internal EmailOpenPgpOperationResult GetOperation() { return operation; }
}
