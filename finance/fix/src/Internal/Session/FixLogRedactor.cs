using System;
using System.Collections.Generic;
using System.Text;

namespace fix;

internal class FixLogRedactionResult
{
    internal byte[] Bytes = [];
    internal bool WasRedacted;
    internal bool WasMalformed;
}

internal static class FixLogRedactor
{
    internal const int NewPasswordTag = 925;
    internal const int EncryptedPasswordLengthTag = 1401;
    internal const int EncryptedPasswordTag = 1402;
    internal const int EncryptedNewPasswordLengthTag = 1403;
    internal const int EncryptedNewPasswordTag = 1404;

    internal static FixLogRedactionResult RedactFixLogBytes(byte[] source)
    {
        FixLogRedactionResult result = new FixLogRedactionResult();
        if (source == null || source.Length == 0)
        {
            return result;
        }
        List<byte> output = [];
        int index = 0;
        int pendingDataTag = 0;
        int pendingDataLength = 0;
        while (index < source.Length)
        {
            int fieldStart = index;
            int tag = 0;
            int tagDigits = 0;
            while (index < source.Length && source[index] >= 48 && source[index] <= 57)
            {
                long candidate = (long)tag * 10L + source[index] - 48;
                if (candidate > FixDefaults.MaxTagNumber)
                {
                    return CreateMalformedRedaction();
                }
                tag = (int)candidate;
                tagDigits++;
                index++;
            }
            if (tagDigits == 0 || tag == 0 || index >= source.Length || source[index] != 61)
            {
                return CreateMalformedRedaction();
            }
            int equalsIndex = index;
            index++;
            int valueStart = index;
            int valueEnd;
            if (pendingDataTag != 0)
            {
                if (tag != pendingDataTag || pendingDataLength > source.Length - valueStart - 1)
                {
                    return CreateMalformedRedaction();
                }
                valueEnd = valueStart + pendingDataLength;
                if (valueEnd >= source.Length || source[valueEnd] != FixDefaults.Soh)
                {
                    return CreateMalformedRedaction();
                }
                pendingDataTag = 0;
                pendingDataLength = 0;
            }
            else
            {
                if (IsSensitiveDataTag(tag))
                {
                    return CreateMalformedRedaction();
                }
                valueEnd = FindFixFieldEnd(source, valueStart);
                if (valueEnd < 0)
                {
                    return CreateMalformedRedaction();
                }
            }

            AppendLogBytes(output, source, fieldStart, equalsIndex - fieldStart + 1);
            int dataTag = ResolveSensitiveDataTag(tag);
            if (dataTag != 0)
            {
                int length = ParseFixLength(source, valueStart, valueEnd);
                if (length < 0)
                {
                    return CreateMalformedRedaction();
                }
                AppendRedactionMarker(output);
                pendingDataTag = dataTag;
                pendingDataLength = length;
                result.WasRedacted = true;
            }
            else if (IsSensitiveValueTag(tag))
            {
                AppendRedactionMarker(output);
                result.WasRedacted = true;
            }
            else
            {
                AppendLogBytes(output, source, valueStart, valueEnd - valueStart);
            }
            output.Add(FixDefaults.Soh);
            index = valueEnd + 1;
        }
        if (pendingDataTag != 0)
        {
            return CreateMalformedRedaction();
        }
        result.Bytes = output.ToArray();
        return result;
    }

    internal static int FindFixFieldEnd(byte[] source, int start)
    {
        for (int index = start; index < source.Length; index++)
        {
            if (source[index] == FixDefaults.Soh) return index;
        }
        return -1;
    }

    internal static int ParseFixLength(byte[] source, int start, int end)
    {
        if (start == end) return -1;
        int value = 0;
        for (int index = start; index < end; index++)
        {
            if (source[index] < 48 || source[index] > 57) return -1;
            long candidate = (long)value * 10L + source[index] - 48;
            if (candidate > FixDefaults.MaxMessageSize) return -1;
            value = (int)candidate;
        }
        return value;
    }

    internal static int ResolveSensitiveDataTag(int lengthTag)
    {
        if (lengthTag == FixTags.SecureDataLength) return FixTags.SecureData;
        if (lengthTag == FixTags.RawDataLength) return FixTags.RawData;
        if (lengthTag == EncryptedPasswordLengthTag) return EncryptedPasswordTag;
        if (lengthTag == EncryptedNewPasswordLengthTag) return EncryptedNewPasswordTag;
        return 0;
    }

    internal static bool IsSensitiveDataTag(int tag)
    {
        return tag == FixTags.SecureData || tag == FixTags.RawData || tag == EncryptedPasswordTag || tag == EncryptedNewPasswordTag;
    }

    internal static bool IsSensitiveValueTag(int tag)
    {
        return tag == FixTags.Username || tag == FixTags.Password || tag == NewPasswordTag || IsSensitiveDataTag(tag);
    }

    internal static void AppendRedactionMarker(List<byte> output)
    {
        output.Add(42);
        output.Add(42);
        output.Add(42);
    }

    internal static void AppendLogBytes(List<byte> output, byte[] source, int start, int count)
    {
        for (int index = start; index < start + count; index++)
        {
            output.Add(source[index]);
        }
    }

    internal static FixLogRedactionResult CreateMalformedRedaction()
    {
        FixLogRedactionResult result = new FixLogRedactionResult();
        result.Bytes = Encoding.ASCII.GetBytes("<redacted-malformed-fix-message>");
        result.WasRedacted = true;
        result.WasMalformed = true;
        return result;
    }
}
