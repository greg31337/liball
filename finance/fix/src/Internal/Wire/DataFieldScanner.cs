using System;

namespace fix;

internal static class DataFieldScanner
{
    internal static DataFieldScanResult ScanAssociatedData(ReadOnlySpan<byte> source, FixFieldDefinition lengthDefinition, FixFieldDefinition dataDefinition, int maximumDataLength)
    {
        DataFieldScanResult result = new DataFieldScanResult();
        result.LengthTag = lengthDefinition.Tag;
        result.DataTag = dataDefinition.Tag;
        string definitionError = ValidateAssociation(lengthDefinition, dataDefinition);
        if (definitionError.Length != 0)
        {
            result.ErrorCode = definitionError;
            return result;
        }
        if (maximumDataLength < 0)
        {
            result.ErrorCode = "invalid-limit";
            return result;
        }
        if (source.Length == 0)
        {
            SetIncomplete(result, 1, 0);
            return result;
        }

        int lengthEquals = FindFieldEquals(source, 0);
        if (lengthEquals < 0)
        {
            SetFieldHeaderError(result, source, 0);
            return result;
        }
        AsciiIntegerResult lengthTag = ParseFieldTag(source, 0, lengthEquals);
        if (!lengthTag.Success)
        {
            CopyTagError(result, lengthTag, 0);
            return result;
        }
        if (lengthTag.Value != lengthDefinition.Tag)
        {
            SetError(result, 0, "unexpected-length-tag");
            return result;
        }

        int lengthDelimiterRelative = ByteSearch.IndexOfByte(source.Slice(lengthEquals + 1), FixDefaults.Soh);
        if (lengthDelimiterRelative < 0)
        {
            SetIncomplete(result, source.Length + 1, source.Length);
            return result;
        }
        int lengthDelimiter = lengthEquals + 1 + lengthDelimiterRelative;
        ScalarParseResult parsedLength = ScalarParser.ParseIntegerValue(source.Slice(lengthEquals + 1, lengthDelimiter - lengthEquals - 1), FixFieldType.Length);
        if (!parsedLength.Success)
        {
            result.ErrorOffset = lengthEquals + 1 + parsedLength.ErrorIndex;
            result.Required = parsedLength.Required;
            result.ErrorCode = parsedLength.ErrorCode;
            return result;
        }
        result.DeclaredLength = parsedLength.IntegerValue;
        if (result.DeclaredLength > maximumDataLength)
        {
            SetError(result, lengthEquals + 1, "limit-exceeded");
            return result;
        }

        int dataFieldStart = lengthDelimiter + 1;
        if (dataFieldStart >= source.Length)
        {
            SetIncomplete(result, dataFieldStart + 1, dataFieldStart);
            return result;
        }
        int dataEquals = FindFieldEquals(source, dataFieldStart);
        if (dataEquals < 0)
        {
            SetFieldHeaderError(result, source, dataFieldStart);
            return result;
        }
        AsciiIntegerResult dataTag = ParseFieldTag(source, dataFieldStart, dataEquals);
        if (!dataTag.Success)
        {
            CopyTagError(result, dataTag, dataFieldStart);
            return result;
        }
        if (dataTag.Value != dataDefinition.Tag)
        {
            SetError(result, dataFieldStart, "data-field-not-adjacent");
            return result;
        }

        int valueStart = dataEquals + 1;
        long totalRequired = (long)valueStart + result.DeclaredLength + 1L;
        if (totalRequired > int.MaxValue)
        {
            SetError(result, valueStart, "length-overflow");
            return result;
        }
        if (source.Length < (int)totalRequired)
        {
            SetIncomplete(result, (int)totalRequired, source.Length);
            return result;
        }
        int dataDelimiter = valueStart + result.DeclaredLength;
        if (source[dataDelimiter] != FixDefaults.Soh)
        {
            SetError(result, dataDelimiter, "data-length-mismatch");
            return result;
        }

        result.ValueStart = valueStart;
        result.Value = new byte[result.DeclaredLength];
        source.Slice(valueStart, result.DeclaredLength).CopyTo(result.Value);
        result.Success = true;
        result.Consumed = dataDelimiter + 1;
        result.Required = result.Consumed;
        return result;
    }

    internal static ScalarFormatResult WriteAssociatedData(FixFieldDefinition lengthDefinition, FixFieldDefinition dataDefinition, ReadOnlySpan<byte> value, Span<byte> destination, int maximumDataLength)
    {
        ScalarFormatResult result = new ScalarFormatResult();
        string definitionError = ValidateAssociation(lengthDefinition, dataDefinition);
        if (definitionError.Length != 0)
        {
            result.ErrorCode = definitionError;
            return result;
        }
        if (maximumDataLength < 0)
        {
            result.ErrorCode = "invalid-limit";
            return result;
        }
        if (value.Length == 0)
        {
            result.Required = 1;
            result.ErrorCode = "empty-data";
            return result;
        }
        if (value.Length > maximumDataLength)
        {
            result.ErrorCode = "limit-exceeded";
            return result;
        }

        int lengthTagDigits = CountDecimalDigits(lengthDefinition.Tag);
        int lengthDigits = CountDecimalDigits(value.Length);
        int dataTagDigits = CountDecimalDigits(dataDefinition.Tag);
        long requiredValue = (long)lengthTagDigits + lengthDigits + dataTagDigits + value.Length + 4L;
        if (requiredValue > int.MaxValue)
        {
            result.ErrorCode = "length-overflow";
            return result;
        }
        int required = (int)requiredValue;
        result.Required = required;
        if (destination.Length < required)
        {
            result.ErrorCode = "destination-too-small";
            return result;
        }

        int index = 0;
        AsciiIntegerResult writtenLengthTag = AsciiDigits.WriteUnsignedInt(lengthDefinition.Tag, destination.Slice(index, lengthTagDigits));
        index += writtenLengthTag.Count;
        destination[index] = 61;
        index++;
        AsciiIntegerResult writtenLength = AsciiDigits.WriteUnsignedInt(value.Length, destination.Slice(index, lengthDigits));
        index += writtenLength.Count;
        destination[index] = FixDefaults.Soh;
        index++;
        AsciiIntegerResult writtenDataTag = AsciiDigits.WriteUnsignedInt(dataDefinition.Tag, destination.Slice(index, dataTagDigits));
        index += writtenDataTag.Count;
        destination[index] = 61;
        index++;
        value.CopyTo(destination.Slice(index, value.Length));
        index += value.Length;
        destination[index] = FixDefaults.Soh;
        index++;

        result.Success = true;
        result.Written = index;
        return result;
    }

    internal static string ValidateAssociation(FixFieldDefinition lengthDefinition, FixFieldDefinition dataDefinition)
    {
        if (lengthDefinition.Tag < 1 || lengthDefinition.Tag > FixDefaults.MaxTagNumber || dataDefinition.Tag < 1 || dataDefinition.Tag > FixDefaults.MaxTagNumber || lengthDefinition.Tag == dataDefinition.Tag)
        {
            return "invalid-associated-tag";
        }
        if (lengthDefinition.FieldType != FixFieldType.Length)
        {
            return "invalid-length-field-type";
        }
        if (dataDefinition.FieldType != FixFieldType.Data && dataDefinition.FieldType != FixFieldType.XmlData)
        {
            return "invalid-data-field-type";
        }
        if (!lengthDefinition.HasDataTag || !dataDefinition.HasLengthTag)
        {
            return "missing-length-association";
        }
        if (lengthDefinition.DataTag != dataDefinition.Tag || dataDefinition.LengthTag != lengthDefinition.Tag)
        {
            return "mismatched-length-association";
        }
        return "";
    }

    internal static int FindFieldEquals(ReadOnlySpan<byte> source, int start)
    {
        for (int index = start; index < source.Length; index++)
        {
            if (source[index] == 61)
            {
                return index;
            }
            if (source[index] == FixDefaults.Soh)
            {
                return -1;
            }
        }
        return -1;
    }

    internal static AsciiIntegerResult ParseFieldTag(ReadOnlySpan<byte> source, int start, int equals)
    {
        AsciiIntegerResult result = AsciiDigits.ParseUnsignedInt(source.Slice(start, equals - start), FixDefaults.MaxTagNumber);
        if (result.Success && equals - start > 1 && source[start] == 48)
        {
            result.Success = false;
            result.ErrorIndex = 0;
            result.ErrorCode = "leading-zero";
        }
        if (result.Success && result.Value == 0)
        {
            result.Success = false;
            result.ErrorIndex = 0;
            result.ErrorCode = "zero-not-allowed";
        }
        return result;
    }

    internal static int CountDecimalDigits(int value)
    {
        int digits = 1;
        int remaining = value;
        while (remaining >= 10)
        {
            remaining = remaining / 10;
            digits++;
        }
        return digits;
    }

    internal static void SetFieldHeaderError(DataFieldScanResult result, ReadOnlySpan<byte> source, int start)
    {
        int delimiter = ByteSearch.IndexOfByte(source.Slice(start), FixDefaults.Soh);
        if (delimiter >= 0)
        {
            SetError(result, start + delimiter, "missing-equals");
            return;
        }
        SetIncomplete(result, source.Length + 1, source.Length);
    }

    internal static void CopyTagError(DataFieldScanResult result, AsciiIntegerResult tag, int start)
    {
        result.ErrorOffset = start + tag.ErrorIndex;
        result.ErrorCode = tag.ErrorCode;
    }

    internal static void SetIncomplete(DataFieldScanResult result, int required, int errorOffset)
    {
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = "incomplete";
    }

    internal static void SetError(DataFieldScanResult result, int offset, string errorCode)
    {
        result.ErrorOffset = offset;
        result.ErrorCode = errorCode;
    }
}
