using System;

namespace uuid;

internal class ByteArrayTools
{
    internal static byte[] Copy(byte[]? bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        byte[] copy = new byte[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            copy[index] = bytes[index];
            index = index + 1;
        }

        return copy;
    }

    internal static byte[] CopyLength(byte[]? bytes, int length)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (bytes.Length != length)
        {
            throw new ArgumentException("byte array length does not match expected length", "bytes");
        }

        return Copy(bytes);
    }

    internal static bool AreEqual(byte[]? left, byte[]? right)
    {
        if (left == right)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int Compare(byte[]? left, byte[]? right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return -1;
        }

        if (right == null)
        {
            return 1;
        }

        int sharedLength = left.Length;
        if (right.Length < sharedLength)
        {
            sharedLength = right.Length;
        }

        int index = 0;
        while (index < sharedLength)
        {
            int leftByte = left[index];
            int rightByte = right[index];
            if (leftByte < rightByte)
            {
                return -1;
            }

            if (leftByte > rightByte)
            {
                return 1;
            }

            index = index + 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    internal static bool HasLength(byte[]? bytes, int length)
    {
        if (bytes == null)
        {
            return false;
        }

        return bytes.Length == length;
    }

    internal static IdentifierValidationResult ValidateLength(byte[]? bytes, int length)
    {
        return ValidateLength(bytes, length, IdentifierKind.Unknown);
    }

    internal static IdentifierValidationResult ValidateLength(byte[]? bytes, int length, IdentifierKind kind)
    {
        if (length < 0)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected length cannot be negative");
        }

        if (bytes == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "byte array is null");
        }

        if (bytes.Length != length)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidLength, "expected " + length.ToString() + " bytes but found " + bytes.Length.ToString());
        }

        return IdentifierValidationResult.Success(kind, "");
    }
}
