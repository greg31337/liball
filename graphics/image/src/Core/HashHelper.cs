using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace image;

internal class HashHelper
{
    internal const string HexAlphabet = "0123456789abcdef";

    internal static byte[] ComputeSha256(byte[]? bytes)
    {
        return ComputeSha256Range(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static byte[] ComputeSha256Range(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);

        if (range.Length == 0 && !ValidateRange(bytes, offset, count, null, blockType))
        {
            return new byte[0];
        }

        return SHA256.HashData(range);
    }

    internal static byte[] ComputeSha1(byte[]? bytes)
    {
        return ComputeSha1Range(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static byte[] ComputeSha1Range(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);

        if (range.Length == 0 && !ValidateRange(bytes, offset, count, null, blockType))
        {
            return new byte[0];
        }

        return SHA1.HashData(range);
    }

    internal static byte[] ComputeMd5(byte[]? bytes)
    {
        return ComputeMd5Range(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static byte[] ComputeMd5Range(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        byte[] range = CopyRange(bytes, offset, count, diagnostics, blockType);

        if (range.Length == 0 && !ValidateRange(bytes, offset, count, null, blockType))
        {
            return new byte[0];
        }

        return MD5.HashData(range);
    }

    internal static byte[] ComputeHash(byte[]? bytes, string? algorithmName, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (algorithmName == null || algorithmName.Length == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "hash.algorithm.empty", "Hash algorithm name was empty.", blockType, 0, 0);
            return new byte[0];
        }

        if (IsAlgorithmName(algorithmName, "SHA256") || IsAlgorithmName(algorithmName, "SHA-256"))
        {
            return ComputeSha256Range(bytes, 0, ByteLength(bytes), diagnostics, blockType);
        }

        if (IsAlgorithmName(algorithmName, "SHA1") || IsAlgorithmName(algorithmName, "SHA-1"))
        {
            return ComputeSha1Range(bytes, 0, ByteLength(bytes), diagnostics, blockType);
        }

        if (IsAlgorithmName(algorithmName, "MD5"))
        {
            return ComputeMd5Range(bytes, 0, ByteLength(bytes), diagnostics, blockType);
        }

        MetadataDiagnostic.AddUnsupported(diagnostics, "hash.algorithm.unsupported", "Hash algorithm was unsupported.", blockType);
        return new byte[0];
    }

    internal static string ComputeSha256Hex(byte[]? bytes)
    {
        return ToHex(ComputeSha256(bytes));
    }

    internal static string ComputeHashHex(byte[]? bytes, string? algorithmName, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        return ToHex(ComputeHash(bytes, algorithmName, diagnostics, blockType));
    }

    internal static string ToHex(byte[]? bytes)
    {
        if (bytes == null)
        {
            return "";
        }

        char[] chars = new char[bytes.Length * 2];
        int index = 0;

        while (index < bytes.Length)
        {
            int value = bytes[index];
            chars[index * 2] = HexAlphabet[value >> 4];
            chars[(index * 2) + 1] = HexAlphabet[value & 15];
            index = index + 1;
        }

        return new string(chars);
    }

    internal static byte[] CopyRange(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (!ValidateRange(bytes, offset, count, diagnostics, blockType))
        {
            return new byte[0];
        }

        if (bytes == null)
        {
            return new byte[0];
        }

        byte[] result = new byte[count];
        int index = 0;

        while (index < count)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static bool ValidateRange(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "hash.bytes.null", "Hash byte array was null.", blockType, offset, count);
            return false;
        }

        if (offset < 0 || count < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "hash.range.negative", "Hash range offset or count was negative.", blockType, offset, count);
            return false;
        }

        if (offset > bytes.Length)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "hash.offset.outside", "Hash range offset exceeded byte array length.", blockType, offset, count);
            return false;
        }

        if (count > bytes.Length - offset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "hash.range.truncated", "Hash range exceeded byte array length.", blockType, offset, count);
            return false;
        }

        return true;
    }

    internal static bool IsAlgorithmName(string? left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static int ByteLength(byte[]? bytes)
    {
        if (bytes == null)
        {
            return 0;
        }

        return bytes.Length;
    }
}
