using System;

namespace fix;

internal static class TagValueFrameScanner
{
    internal static FrameResult ScanFrame(ReadOnlySpan<byte> source, FrameOptions options)
    {
        FrameResult result = new FrameResult();
        if (options.MaxFrameSize < 0)
        {
            SetInvalidResult(result, FrameStatus.Invalid, 0, 0, "invalid-limit");
            return result;
        }
        if (options.MaxFrameSize == 0)
        {
            SetInvalidResult(result, FrameStatus.LimitExceeded, 0, 1, "limit-exceeded");
            return result;
        }
        if (source.Length == 0)
        {
            SetIncompleteResult(result, 1, 0, options.MaxFrameSize);
            return result;
        }
        if (source[0] != 56)
        {
            SetInvalidResult(result, FrameStatus.Invalid, 0, 0, "missing-begin-string");
            return result;
        }
        if (source.Length < 2)
        {
            SetIncompleteResult(result, 2, source.Length, options.MaxFrameSize);
            return result;
        }
        if (source[1] != 61)
        {
            SetInvalidResult(result, FrameStatus.Invalid, 1, 0, "missing-begin-string");
            return result;
        }

        int beginDelimiter = ByteSearch.IndexOfByte(source, FixDefaults.Soh);
        if (beginDelimiter < 0)
        {
            if (source.Length >= options.MaxFrameSize)
            {
                SetInvalidResult(result, FrameStatus.LimitExceeded, source.Length, source.Length + 1, "limit-exceeded");
                return result;
            }
            SetIncompleteResult(result, source.Length + 1, source.Length, options.MaxFrameSize);
            return result;
        }
        if (beginDelimiter == 2)
        {
            SetInvalidResult(result, FrameStatus.Invalid, 2, 0, "empty-begin-string");
            return result;
        }

        int lengthFieldStart = beginDelimiter + 1;
        if (source.Length <= lengthFieldStart)
        {
            SetIncompleteResult(result, lengthFieldStart + 2, source.Length, options.MaxFrameSize);
            return result;
        }
        if (source[lengthFieldStart] != 57)
        {
            SetInvalidResult(result, FrameStatus.Invalid, lengthFieldStart, 0, "missing-body-length");
            return result;
        }
        if (source.Length <= lengthFieldStart + 1)
        {
            SetIncompleteResult(result, lengthFieldStart + 2, source.Length, options.MaxFrameSize);
            return result;
        }
        if (source[lengthFieldStart + 1] != 61)
        {
            SetInvalidResult(result, FrameStatus.Invalid, lengthFieldStart + 1, 0, "missing-body-length");
            return result;
        }

        int lengthValueStart = lengthFieldStart + 2;
        int relativeLengthDelimiter = ByteSearch.IndexOfByte(source.Slice(lengthValueStart), FixDefaults.Soh);
        if (relativeLengthDelimiter < 0)
        {
            if (!ValidatePartialBodyLength(source, lengthValueStart, options.MaxFrameSize, result))
            {
                return result;
            }
            SetIncompleteResult(result, source.Length + 1, source.Length, options.MaxFrameSize);
            return result;
        }
        int lengthDelimiter = lengthValueStart + relativeLengthDelimiter;
        AsciiIntegerResult parsedLength = AsciiDigits.ParseUnsignedInt(source.Slice(lengthValueStart, lengthDelimiter - lengthValueStart), int.MaxValue);
        if (!parsedLength.Success)
        {
            int errorOffset = parsedLength.ErrorIndex < 0 ? lengthValueStart : lengthValueStart + parsedLength.ErrorIndex;
            FrameStatus status = parsedLength.ErrorCode == "overflow" ? FrameStatus.LimitExceeded : FrameStatus.Invalid;
            string errorCode = parsedLength.ErrorCode == "overflow" ? "limit-exceeded" : "invalid-body-length";
            SetInvalidResult(result, status, errorOffset, parsedLength.Required, errorCode);
            return result;
        }
        if (parsedLength.Value == 0)
        {
            SetInvalidResult(result, FrameStatus.Invalid, lengthValueStart, 0, "zero-body-length");
            return result;
        }

        int bodyStart = lengthDelimiter + 1;
        long expectedChecksumStartValue = (long)bodyStart + parsedLength.Value;
        long expectedFrameLengthValue = expectedChecksumStartValue + 7L;
        if (expectedFrameLengthValue > int.MaxValue)
        {
            SetInvalidResult(result, FrameStatus.LimitExceeded, lengthValueStart, int.MaxValue, "limit-exceeded");
            return result;
        }
        int expectedChecksumStart = (int)expectedChecksumStartValue;
        int expectedFrameLength = (int)expectedFrameLengthValue;
        if (expectedFrameLength > options.MaxFrameSize)
        {
            SetInvalidResult(result, FrameStatus.LimitExceeded, lengthValueStart, expectedFrameLength, "limit-exceeded");
            return result;
        }

        if (source.Length < expectedFrameLength)
        {
            ChecksumFieldResult earlyChecksum = FindChecksumField(source, bodyStart, options.VerifyCheckSum);
            if (earlyChecksum.Success && earlyChecksum.Start != expectedChecksumStart)
            {
                if (options.VerifyBodyLength)
                {
                    SetInvalidResult(result, FrameStatus.Invalid, lengthValueStart, earlyChecksum.Start + 7, "body-length-mismatch");
                    return result;
                }
                return CompleteFrame(source, earlyChecksum, options);
            }
            SetIncompleteResult(result, expectedFrameLength, source.Length, options.MaxFrameSize);
            return result;
        }

        ChecksumFieldResult checksum = ReadChecksumField(source, expectedChecksumStart);
        if (!checksum.Success)
        {
            ChecksumFieldResult alternateChecksum = FindChecksumField(source, bodyStart, options.VerifyCheckSum);
            if (alternateChecksum.Success && alternateChecksum.Start != expectedChecksumStart)
            {
                if (options.VerifyBodyLength)
                {
                    SetInvalidResult(result, FrameStatus.Invalid, lengthValueStart, alternateChecksum.Start + 7, "body-length-mismatch");
                    return result;
                }
                return CompleteFrame(source, alternateChecksum, options);
            }
            SetInvalidResult(result, FrameStatus.Invalid, checksum.ErrorOffset, expectedFrameLength, checksum.ErrorCode);
            return result;
        }
        return CompleteFrame(source, checksum, options);
    }

    internal static FrameResult CompleteFrame(ReadOnlySpan<byte> source, ChecksumFieldResult checksum, FrameOptions options)
    {
        FrameResult result = new FrameResult();
        int frameLength = checksum.Start + 7;
        if (frameLength > options.MaxFrameSize)
        {
            SetInvalidResult(result, FrameStatus.LimitExceeded, checksum.Start, frameLength, "limit-exceeded");
            return result;
        }
        if (options.VerifyCheckSum)
        {
            int calculated = ChecksumCalculator.CalculateChecksum(source.Slice(0, checksum.Start));
            if (calculated != checksum.DeclaredChecksum)
            {
                SetInvalidResult(result, FrameStatus.Invalid, checksum.Start + 3, frameLength, "checksum-mismatch");
                return result;
            }
        }
        result.Status = FrameStatus.Complete;
        result.FrameLength = frameLength;
        result.Consumed = frameLength;
        result.Required = frameLength;
        return result;
    }

    internal static ChecksumFieldResult ReadChecksumField(ReadOnlySpan<byte> source, int start)
    {
        ChecksumFieldResult result = new ChecksumFieldResult();
        result.Start = start;
        if (start < 0 || start > source.Length - 7)
        {
            result.ErrorOffset = start < 0 ? 0 : source.Length;
            result.ErrorCode = "incomplete-checksum-field";
            return result;
        }
        if (source[start] != 49 || source[start + 1] != 48 || source[start + 2] != 61)
        {
            result.ErrorOffset = start;
            result.ErrorCode = "invalid-checksum-field";
            return result;
        }
        AsciiIntegerResult parsed = ChecksumCalculator.ParseChecksum(source.Slice(start + 3, 3));
        if (!parsed.Success)
        {
            result.ErrorOffset = start + 3 + (parsed.ErrorIndex < 0 ? 0 : parsed.ErrorIndex);
            result.ErrorCode = "invalid-checksum-field";
            return result;
        }
        if (source[start + 6] != FixDefaults.Soh)
        {
            result.ErrorOffset = start + 6;
            result.ErrorCode = "invalid-checksum-delimiter";
            return result;
        }
        result.Success = true;
        result.DeclaredChecksum = parsed.Value;
        return result;
    }

    internal static ChecksumFieldResult FindChecksumField(ReadOnlySpan<byte> source, int bodyStart, bool requireMatchingChecksum)
    {
        ChecksumFieldResult notFound = new ChecksumFieldResult();
        int lastStart = source.Length - 7;
        for (int start = bodyStart; start <= lastStart; start++)
        {
            if (start > bodyStart && source[start - 1] != FixDefaults.Soh)
            {
                continue;
            }
            if (source[start] == 49 && source[start + 1] == 48 && source[start + 2] == 61 && source[start + 6] == FixDefaults.Soh)
            {
                ChecksumFieldResult candidate = ReadChecksumField(source, start);
                if (candidate.Success)
                {
                    if (!requireMatchingChecksum || ChecksumCalculator.CalculateChecksum(source.Slice(0, start)) == candidate.DeclaredChecksum)
                    {
                        return candidate;
                    }
                }
            }
        }
        notFound.ErrorOffset = source.Length;
        notFound.ErrorCode = "checksum-field-not-found";
        return notFound;
    }

    internal static bool ValidatePartialBodyLength(ReadOnlySpan<byte> source, int start, int maximumFrameSize, FrameResult result)
    {
        if (start == source.Length)
        {
            return true;
        }
        for (int index = start; index < source.Length; index++)
        {
            if (!AsciiDigits.IsDigit(source[index]))
            {
                SetInvalidResult(result, FrameStatus.Invalid, index, 0, "invalid-body-length");
                return false;
            }
        }
        AsciiIntegerResult parsed = AsciiDigits.ParseUnsignedInt(source.Slice(start), int.MaxValue);
        if (!parsed.Success)
        {
            int errorOffset = parsed.ErrorIndex < 0 ? start : start + parsed.ErrorIndex;
            FrameStatus status = parsed.ErrorCode == "overflow" ? FrameStatus.LimitExceeded : FrameStatus.Invalid;
            string errorCode = parsed.ErrorCode == "overflow" ? "limit-exceeded" : "invalid-body-length";
            SetInvalidResult(result, status, errorOffset, parsed.Required, errorCode);
            return false;
        }
        long minimumFrameLength = (long)source.Length + 1L + parsed.Value + 7L;
        if (minimumFrameLength > maximumFrameSize)
        {
            int required = minimumFrameLength > int.MaxValue ? int.MaxValue : (int)minimumFrameLength;
            SetInvalidResult(result, FrameStatus.LimitExceeded, start, required, "limit-exceeded");
            return false;
        }
        return true;
    }

    internal static void SetIncompleteResult(FrameResult result, int required, int actual, int maximumFrameSize)
    {
        if (required > maximumFrameSize)
        {
            SetInvalidResult(result, FrameStatus.LimitExceeded, actual, required, "limit-exceeded");
            return;
        }
        result.Status = FrameStatus.Incomplete;
        result.Required = required;
        result.ErrorOffset = actual;
        result.ErrorCode = "incomplete";
        result.ErrorMessage = "More bytes are required to determine the complete FIX frame.";
    }

    internal static void SetInvalidResult(FrameResult result, FrameStatus status, int errorOffset, int required, string errorCode)
    {
        result.Status = status;
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX frame could not be scanned: {errorCode}.";
    }
}
