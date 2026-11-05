using System;

namespace email;

internal class EmailDkimParsedKeyMaterial
{
    EmailDkimKeyAlgorithmKind algorithmKind;
    string keyType;
    byte[]? subjectPublicKeyInfo;
    byte[]? rsaPublicKey;
    byte[]? ed25519PublicKey;
    int keyBitCount;

    internal EmailDkimParsedKeyMaterial(EmailDkimKeyAlgorithmKind algorithmKind, string keyType, byte[]? subjectPublicKeyInfo, byte[]? rsaPublicKey, byte[]? ed25519PublicKey, int keyBitCount)
    {
        this.algorithmKind = algorithmKind;
        this.keyType = keyType;
        this.subjectPublicKeyInfo = CopyOptional(subjectPublicKeyInfo);
        this.rsaPublicKey = CopyOptional(rsaPublicKey);
        this.ed25519PublicKey = CopyOptional(ed25519PublicKey);
        this.keyBitCount = keyBitCount;
    }

    internal EmailDkimKeyAlgorithmKind GetAlgorithmKind() { return algorithmKind; }
    internal string GetKeyType() { return keyType; }
    internal byte[]? GetSubjectPublicKeyInfo() { return CopyOptional(subjectPublicKeyInfo); }
    internal byte[]? GetRsaPublicKey() { return CopyOptional(rsaPublicKey); }
    internal byte[]? GetEd25519PublicKey() { return CopyOptional(ed25519PublicKey); }
    internal int GetKeyBitCount() { return keyBitCount; }

    static byte[]? CopyOptional(byte[]? value)
    {
        if (value == null) return null;
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
internal class EmailDkimDerElement
{
    int tag;
    int start;
    int contentStart;
    int contentLength;
    int end;

    internal EmailDkimDerElement(int tag, int start, int contentStart, int contentLength, int end)
    {
        this.tag = tag;
        this.start = start;
        this.contentStart = contentStart;
        this.contentLength = contentLength;
        this.end = end;
    }

    internal int GetTag() { return tag; }
    internal int GetStart() { return start; }
    internal int GetContentStart() { return contentStart; }
    internal int GetContentLength() { return contentLength; }
    internal int GetEnd() { return end; }
}
internal class EmailDkimKeyDer
{
    static readonly byte[] rsaOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
    static readonly byte[] ed25519Oid = new byte[] { 0x2b, 0x65, 0x70 };

    internal static EmailDkimParsedKeyMaterial? ParseSubjectPublicKeyInfo(byte[] value)
    {
        if (value == null) throw new ArgumentNullException("value");
        EmailDkimDerElement? outer = ReadElement(value, 0, value.Length);
        if (!IsExact(outer, 0x30, 0, value.Length)) return null;
        int cursor = (outer ?? throw new InvalidOperationException()).GetContentStart();
        EmailDkimDerElement? algorithm = ReadElement(value, cursor, outer.GetEnd());
        if (algorithm == null || algorithm.GetTag() != 0x30) return null;
        cursor = algorithm.GetEnd();
        EmailDkimDerElement? bits = ReadElement(value, cursor, outer.GetEnd());
        if (bits == null || bits.GetTag() != 0x03 || bits.GetEnd() != outer.GetEnd() || bits.GetContentLength() < 1 || value[bits.GetContentStart()] != 0) return null;
        EmailDkimKeyAlgorithmKind kind = ParseAlgorithm(value, algorithm);
        if (kind == EmailDkimKeyAlgorithmKind.Rsa)
        {
            byte[] rsa = Slice(value, bits.GetContentStart() + 1, bits.GetEnd());
            EmailDkimParsedKeyMaterial? parsed = ParseRsaPublicKey(rsa);
            if (parsed == null) return null;
            return new EmailDkimParsedKeyMaterial(EmailDkimKeyAlgorithmKind.Rsa, "rsa", value, rsa, null, parsed.GetKeyBitCount());
        }

        if (kind == EmailDkimKeyAlgorithmKind.Ed25519)
        {
            if (bits.GetContentLength() != 33) return null;
            byte[] ed = Slice(value, bits.GetContentStart() + 1, bits.GetEnd());
            return new EmailDkimParsedKeyMaterial(EmailDkimKeyAlgorithmKind.Ed25519, "ed25519", value, null, ed, 256);
        }

        return null;
    }

    internal static EmailDkimParsedKeyMaterial? ParseRsaPublicKey(byte[] value)
    {
        if (value == null) throw new ArgumentNullException("value");
        EmailDkimDerElement? outer = ReadElement(value, 0, value.Length);
        if (!IsExact(outer, 0x30, 0, value.Length)) return null;
        int cursor = (outer ?? throw new InvalidOperationException()).GetContentStart();
        EmailDkimDerElement? modulus = ReadElement(value, cursor, outer.GetEnd());
        if (!IsPositiveInteger(value, modulus)) return null;
        cursor = (modulus ?? throw new InvalidOperationException()).GetEnd();
        EmailDkimDerElement? exponent = ReadElement(value, cursor, outer.GetEnd());
        if (!IsPositiveInteger(value, exponent) || (exponent ?? throw new InvalidOperationException()).GetEnd() != outer.GetEnd() || !IsValidExponent(value, exponent)) return null;
        int bits = GetIntegerBitCount(value, modulus);
        if (bits == 0) return null;
        return new EmailDkimParsedKeyMaterial(EmailDkimKeyAlgorithmKind.Rsa, "rsa", null, value, null, bits);
    }

    static EmailDkimKeyAlgorithmKind ParseAlgorithm(byte[] value, EmailDkimDerElement algorithm)
    {
        int cursor = algorithm.GetContentStart();
        EmailDkimDerElement? oid = ReadElement(value, cursor, algorithm.GetEnd());
        if (oid == null || oid.GetTag() != 0x06) return EmailDkimKeyAlgorithmKind.Unknown;
        cursor = oid.GetEnd();
        if (Matches(value, oid, rsaOid))
        {
            if (cursor == algorithm.GetEnd()) return EmailDkimKeyAlgorithmKind.Rsa;
            EmailDkimDerElement? parameter = ReadElement(value, cursor, algorithm.GetEnd());
            if (parameter != null && parameter.GetTag() == 0x05 && parameter.GetContentLength() == 0 && parameter.GetEnd() == algorithm.GetEnd()) return EmailDkimKeyAlgorithmKind.Rsa;
            return EmailDkimKeyAlgorithmKind.Unknown;
        }

        if (Matches(value, oid, ed25519Oid) && cursor == algorithm.GetEnd()) return EmailDkimKeyAlgorithmKind.Ed25519;
        return EmailDkimKeyAlgorithmKind.Unknown;
    }

    static EmailDkimDerElement? ReadElement(byte[] value, int offset, int end)
    {
        if (offset < 0 || offset >= end || end > value.Length) return null;
        int start = offset;
        int tag = value[offset];
        offset++;
        if (offset >= end) return null;
        int first = value[offset];
        offset++;
        long length;
        if (first < 128)
        {
            length = first;
        }
        else
        {
            int count = first & 0x7f;
            if (count == 0 || count > 4 || offset > end - count || value[offset] == 0) return null;
            length = 0;
            int index = 0;
            while (index < count)
            {
                length = length * 256 + value[offset + index];
                index++;
            }

            if (length < 128) return null;
            offset += count;
        }

        if (length > end - offset || length > int.MaxValue) return null;
        int elementEnd = offset + (int)length;
        return new EmailDkimDerElement(tag, start, offset, (int)length, elementEnd);
    }

    static bool IsExact(EmailDkimDerElement? value, int tag, int start, int end)
    {
        return value != null && value.GetTag() == tag && value.GetStart() == start && value.GetEnd() == end;
    }

    static bool IsPositiveInteger(byte[] value, EmailDkimDerElement? element)
    {
        if (element == null || element.GetTag() != 0x02 || element.GetContentLength() == 0) return false;
        int start = element.GetContentStart();
        if ((value[start] & 0x80) != 0) return false;
        if (element.GetContentLength() > 1 && value[start] == 0 && (value[start + 1] & 0x80) == 0) return false;
        return GetIntegerBitCount(value, element) > 0;
    }

    static bool IsValidExponent(byte[] value, EmailDkimDerElement element)
    {
        int start = element.GetContentStart();
        int length = element.GetContentLength();
        if (value[start] == 0)
        {
            start++;
            length--;
        }

        if (length == 0 || length > 8 || (value[start + length - 1] & 1) == 0) return false;
        if (length == 1 && value[start] < 3) return false;
        return true;
    }

    static int GetIntegerBitCount(byte[] value, EmailDkimDerElement element)
    {
        int start = element.GetContentStart();
        int end = element.GetEnd();
        while (start < end && value[start] == 0) start++;
        if (start == end) return 0;
        int leading = 0;
        int mask = 0x80;
        while ((value[start] & mask) == 0)
        {
            leading++;
            mask >>= 1;
        }

        return (end - start) * 8 - leading;
    }

    static bool Matches(byte[] value, EmailDkimDerElement element, byte[] expected)
    {
        if (element.GetContentLength() != expected.Length) return false;
        int index = 0;
        while (index < expected.Length)
        {
            if (value[element.GetContentStart() + index] != expected[index]) return false;
            index++;
        }

        return true;
    }

    static byte[] Slice(byte[] value, int start, int end)
    {
        byte[] result = new byte[end - start];
        Array.Copy(value, start, result, 0, result.Length);
        return result;
    }
}
