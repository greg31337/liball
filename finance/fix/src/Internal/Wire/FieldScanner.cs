using System;

namespace fix;

internal static class FieldScanner
{
    internal static FieldParseResult ScanField(ReadOnlySpan<byte> source, int start, int wireIndex, int maximumValueLength, bool allowEmptyValue)
    {
        FieldParseResult result = new FieldParseResult();
        if (maximumValueLength < 0)
        {
            SetFieldError(result, DecodeStatus.Invalid, start, 0, "invalid-limit");
            return result;
        }
        if (start < 0 || start > source.Length)
        {
            SetFieldError(result, DecodeStatus.Invalid, start, 0, "invalid-offset");
            return result;
        }
        if (start == source.Length)
        {
            SetFieldError(result, DecodeStatus.Incomplete, start, start + 1, "incomplete");
            return result;
        }

        int equals = DataFieldScanner.FindFieldEquals(source, start);
        if (equals < 0)
        {
            int delimiter = ByteSearch.IndexOfByte(source.Slice(start), FixDefaults.Soh);
            if (delimiter >= 0)
            {
                SetFieldError(result, DecodeStatus.Invalid, start + delimiter, 0, "missing-equals");
                return result;
            }
            SetFieldError(result, DecodeStatus.Incomplete, source.Length, source.Length + 1, "incomplete");
            return result;
        }
        AsciiIntegerResult parsedTag = DataFieldScanner.ParseFieldTag(source, start, equals);
        if (!parsedTag.Success)
        {
            int errorOffset = parsedTag.ErrorIndex < 0 ? start : start + parsedTag.ErrorIndex;
            SetFieldError(result, DecodeStatus.Invalid, errorOffset, parsedTag.Required, parsedTag.ErrorCode);
            return result;
        }

        int valueStart = equals + 1;
        int relativeDelimiter = ByteSearch.IndexOfByte(source.Slice(valueStart), FixDefaults.Soh);
        if (relativeDelimiter < 0)
        {
            int currentLength = source.Length - valueStart;
            if (currentLength > maximumValueLength)
            {
                SetFieldError(result, DecodeStatus.LimitExceeded, valueStart + maximumValueLength, source.Length + 1, "limit-exceeded");
                return result;
            }
            SetFieldError(result, DecodeStatus.Incomplete, source.Length, source.Length + 1, "incomplete");
            return result;
        }
        int valueLength = relativeDelimiter;
        if (valueLength > maximumValueLength)
        {
            SetFieldError(result, DecodeStatus.LimitExceeded, valueStart + maximumValueLength, valueStart + valueLength + 1, "limit-exceeded");
            return result;
        }
        if (valueLength == 0 && !allowEmptyValue)
        {
            SetFieldError(result, DecodeStatus.Invalid, valueStart, valueStart + 1, "empty-value");
            return result;
        }

        FixField field = new FixField();
        field.Tag = parsedTag.Value;
        field.Value = new byte[valueLength];
        source.Slice(valueStart, valueLength).CopyTo(field.Value);
        field.WireIndex = wireIndex;
        field.SourceOffset = start;
        field.SourceLength = valueStart + valueLength + 1 - start;
        result.Status = DecodeStatus.Success;
        result.Field = field;
        result.Consumed = field.SourceLength;
        result.Required = field.SourceLength;
        return result;
    }

    internal static void SetFieldError(FieldParseResult result, DecodeStatus status, int errorOffset, int required, string errorCode)
    {
        result.Status = status;
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX field could not be scanned: {errorCode}.";
    }
}
