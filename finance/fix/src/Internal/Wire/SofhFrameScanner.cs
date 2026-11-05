using System;

namespace fix;

internal static class SofhFrameScanner
{
    internal static FrameResult ScanFrame(ReadOnlySpan<byte> source, FrameOptions options)
    {
        FrameResult result = new FrameResult();
        result.IsSofh = true;
        if (options.MaxFrameSize < 0)
        {
            SetFailureResult(result, FrameStatus.Invalid, 0, 0, "invalid-limit");
            return result;
        }
        if (options.MaxFrameSize < FixDefaults.SofhHeaderSize)
        {
            SetFailureResult(result, FrameStatus.LimitExceeded, 0, FixDefaults.SofhHeaderSize, "limit-exceeded");
            return result;
        }
        if (source.Length < FixDefaults.SofhHeaderSize)
        {
            SetIncompleteResult(result, FixDefaults.SofhHeaderSize, source.Length);
            return result;
        }

        long bigLength = ReadBigEndianLength(source);
        long littleLength = ReadLittleEndianLength(source);
        ushort bigEncoding = ReadBigEndianEncoding(source);
        ushort littleEncoding = ReadLittleEndianEncoding(source);
        bool bigSupported = IsSupportedEncoding(bigEncoding);
        bool littleSupported = IsSupportedEncoding(littleEncoding);
        if (!bigSupported && !littleSupported)
        {
            result.EncodingType = bigEncoding;
            SetFailureResult(result, FrameStatus.Invalid, 4, FixDefaults.SofhHeaderSize, "unknown-encoding-type");
            return result;
        }

        bool littleEndian = ChooseLittleEndian(bigLength, bigEncoding, bigSupported, littleLength, littleEncoding, littleSupported, source.Length, options.MaxFrameSize);
        long declaredLength = littleEndian ? littleLength : bigLength;
        result.EncodingType = littleEndian ? littleEncoding : bigEncoding;
        if (declaredLength < FixDefaults.SofhHeaderSize)
        {
            SetFailureResult(result, FrameStatus.Invalid, 0, FixDefaults.SofhHeaderSize, "invalid-message-length");
            return result;
        }
        if (declaredLength > int.MaxValue)
        {
            SetFailureResult(result, FrameStatus.LimitExceeded, 0, int.MaxValue, "limit-exceeded");
            return result;
        }

        int frameLength = (int)declaredLength;
        if (frameLength > options.MaxFrameSize)
        {
            SetFailureResult(result, FrameStatus.LimitExceeded, 0, frameLength, "limit-exceeded");
            return result;
        }
        if (source.Length < frameLength)
        {
            SetIncompleteResult(result, frameLength, source.Length);
            return result;
        }

        result.Status = FrameStatus.Complete;
        result.FrameLength = frameLength;
        result.Consumed = frameLength;
        result.Required = frameLength;
        return result;
    }

    internal static long ReadBigEndianLength(ReadOnlySpan<byte> source)
    {
        return (long)source[0] * 16777216L + (long)source[1] * 65536L + (long)source[2] * 256L + source[3];
    }

    internal static long ReadLittleEndianLength(ReadOnlySpan<byte> source)
    {
        return source[0] + (long)source[1] * 256L + (long)source[2] * 65536L + (long)source[3] * 16777216L;
    }

    internal static ushort ReadBigEndianEncoding(ReadOnlySpan<byte> source)
    {
        return (ushort)(source[4] * 256 + source[5]);
    }

    internal static ushort ReadLittleEndianEncoding(ReadOnlySpan<byte> source)
    {
        return (ushort)(source[4] + source[5] * 256);
    }

    internal static bool ChooseLittleEndian(long bigLength, ushort bigEncoding, bool bigSupported, long littleLength, ushort littleEncoding, bool littleSupported, int available, int maximum)
    {
        if (!bigSupported)
        {
            return true;
        }
        if (!littleSupported)
        {
            return false;
        }

        bool bigValid = bigLength >= FixDefaults.SofhHeaderSize;
        bool littleValid = littleLength >= FixDefaults.SofhHeaderSize;
        if (bigValid != littleValid)
        {
            return littleValid;
        }
        bool bigWithinLimit = bigValid && bigLength <= int.MaxValue && bigLength <= maximum;
        bool littleWithinLimit = littleValid && littleLength <= int.MaxValue && littleLength <= maximum;
        if (bigWithinLimit != littleWithinLimit)
        {
            return littleWithinLimit;
        }
        bool bigExact = bigWithinLimit && bigLength == available;
        bool littleExact = littleWithinLimit && littleLength == available;
        if (bigExact != littleExact)
        {
            return littleExact;
        }
        bool bigComplete = bigWithinLimit && bigLength <= available;
        bool littleComplete = littleWithinLimit && littleLength <= available;
        if (bigComplete != littleComplete)
        {
            return littleComplete;
        }
        bool bigPrivate = IsPrivateEncoding(bigEncoding);
        bool littlePrivate = IsPrivateEncoding(littleEncoding);
        if (bigPrivate != littlePrivate)
        {
            return bigPrivate;
        }
        return false;
    }

    internal static bool IsSupportedEncoding(ushort encoding)
    {
        return IsPrivateEncoding(encoding) || encoding == FixEncodingTypes.GoogleProtocolBuffersVersion10 || encoding == FixEncodingTypes.SbeVersion10BigEndian || encoding == FixEncodingTypes.SbeVersion20BigEndian || encoding == FixEncodingTypes.Asn1PerVersion10 || encoding == FixEncodingTypes.Asn1BerVersion10 || encoding == FixEncodingTypes.Asn1OerVersion10 || encoding == FixEncodingTypes.SbeVersion10LittleEndian || encoding == FixEncodingTypes.SbeVersion20LittleEndian || encoding == FixEncodingTypes.TagValue || encoding == FixEncodingTypes.FixmlVersion10 || encoding == FixEncodingTypes.Json || encoding >= FixEncodingTypes.FastMinimum && encoding <= FixEncodingTypes.FastMaximum || encoding == FixEncodingTypes.Bson;
    }

    internal static bool IsPrivateEncoding(ushort encoding)
    {
        return encoding >= FixEncodingTypes.PrivateUseMinimum && encoding <= FixEncodingTypes.PrivateUseMaximum;
    }

    internal static void SetIncompleteResult(FrameResult result, int required, int errorOffset)
    {
        result.Status = FrameStatus.Incomplete;
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = "incomplete";
        result.ErrorMessage = "More bytes are required to determine the complete SOFH frame.";
    }

    internal static void SetFailureResult(FrameResult result, FrameStatus status, int errorOffset, int required, string errorCode)
    {
        result.Status = status;
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The SOFH frame is invalid: {errorCode}.";
    }
}
