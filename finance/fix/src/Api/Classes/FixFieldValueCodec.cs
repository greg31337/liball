using System;

namespace fix;

/// <summary>Parses and canonically formats individual FIX field values.</summary>
public class FixFieldValueCodec : IFieldValueCodec
{
    /// <summary>Creates a field-value codec.</summary>
    public FixFieldValueCodec()
    {
    }

    /// <summary>Parses one raw field value according to its FIX datatype.</summary>
    /// <param name="source">The field value bytes without tag, equals sign, or delimiter.</param>
    /// <param name="tag">The field tag associated with the value.</param>
    /// <param name="fieldType">The FIX datatype to parse.</param>
    /// <param name="options">The decode limits and text encoding.</param>
    /// <returns>The explicit field parse outcome.</returns>
    public FieldParseResult DecodeFieldValue(ReadOnlySpan<byte> source, int tag, FixFieldType fieldType, DecodeOptions options)
    {
        FieldParseResult result = new FieldParseResult();
        if (!IsValidTag(tag))
        {
            SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "invalid-tag");
            return result;
        }
        if (options.MaxFieldSize < 0)
        {
            SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "invalid-limit");
            return result;
        }
        if (!IsSupportedTextEncoding(options.TextEncodingName))
        {
            SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "unsupported-text-encoding");
            return result;
        }
        if (!IsSupportedFieldType(fieldType))
        {
            SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "unsupported-field-type");
            return result;
        }
        if (source.Length > options.MaxFieldSize)
        {
            SetDecodeError(result, DecodeStatus.LimitExceeded, 0, source.Length, "limit-exceeded");
            return result;
        }
        if (source.Length == 0 && options.AllowEmptyValues)
        {
            return CreateDecodeSuccess(source, tag, fieldType);
        }
        if (IsAsciiEncoding(options.TextEncodingName) && IsTextualType(fieldType))
        {
            int nonAscii = FindNonAsciiByte(source);
            if (nonAscii >= 0)
            {
                SetDecodeError(result, DecodeStatus.Invalid, nonAscii, source.Length, "invalid-character");
                return result;
            }
        }
        if (IsOpenEndedResendValue(tag, fieldType, source))
        {
            return CreateDecodeSuccess(source, tag, fieldType);
        }
        if (IsScalarType(fieldType))
        {
            ScalarParseResult parsedScalar = ScalarParser.ParseScalar(source, fieldType, options.MaxFieldSize);
            if (!parsedScalar.Success)
            {
                CopyScalarDecodeError(result, parsedScalar);
                return result;
            }
            return CreateDecodeSuccess(source, tag, fieldType);
        }
        if (IsTemporalType(fieldType))
        {
            TimestampParseResult parsedTimestamp = TimestampParser.ParseTimestamp(source, fieldType, options.MaxFieldSize);
            if (!parsedTimestamp.Success)
            {
                CopyTimestampDecodeError(result, parsedTimestamp);
                return result;
            }
            return CreateDecodeSuccess(source, tag, fieldType);
        }
        if (fieldType == FixFieldType.Data || fieldType == FixFieldType.XmlData)
        {
            if (source.Length == 0)
            {
                SetDecodeError(result, DecodeStatus.Incomplete, 0, 1, "incomplete");
                return result;
            }
            return CreateDecodeSuccess(source, tag, fieldType);
        }

        SetDecodeError(result, DecodeStatus.Invalid, 0, 0, "unsupported-field-type");
        return result;
    }

    /// <summary>Formats one field value into caller-owned storage.</summary>
    /// <param name="field">The field carrying the value bytes.</param>
    /// <param name="fieldType">The FIX datatype to format.</param>
    /// <param name="destination">The destination storage.</param>
    /// <param name="options">The encode limits and text encoding.</param>
    /// <returns>The explicit encode outcome.</returns>
    public EncodeResult EncodeFieldValue(FixField field, FixFieldType fieldType, Span<byte> destination, EncodeOptions options)
    {
        EncodeResult result = new EncodeResult();
        if (!IsValidTag(field.Tag))
        {
            SetEncodeError(result, EncodeStatus.Invalid, 0, "invalid-tag");
            return result;
        }
        if (options.MaxFieldSize < 0)
        {
            SetEncodeError(result, EncodeStatus.Invalid, 0, "invalid-limit");
            return result;
        }
        if (!IsSupportedTextEncoding(options.TextEncodingName))
        {
            SetEncodeError(result, EncodeStatus.Invalid, 0, "unsupported-text-encoding");
            return result;
        }
        if (!IsSupportedFieldType(fieldType))
        {
            SetEncodeError(result, EncodeStatus.Invalid, 0, "unsupported-field-type");
            return result;
        }
        if (field.Value == null)
        {
            SetEncodeError(result, EncodeStatus.Invalid, 0, "missing-field-value");
            return result;
        }
        if (field.Value.Length > options.MaxFieldSize)
        {
            SetEncodeError(result, EncodeStatus.LimitExceeded, field.Value.Length, "limit-exceeded");
            return result;
        }
        if (IsAsciiEncoding(options.TextEncodingName) && IsTextualType(fieldType) && FindNonAsciiByte(field.Value) >= 0)
        {
            SetEncodeError(result, EncodeStatus.Invalid, 0, "invalid-character");
            return result;
        }
        if (IsOpenEndedResendValue(field.Tag, fieldType, field.Value))
        {
            return CopyEncodedData(field.Value, destination);
        }
        if (fieldType == FixFieldType.Data || fieldType == FixFieldType.XmlData)
        {
            return CopyEncodedData(field.Value, destination);
        }
        if (IsScalarType(fieldType))
        {
            ScalarParseResult parsedScalar = ScalarParser.ParseScalar(field.Value, fieldType, options.MaxFieldSize);
            if (!parsedScalar.Success)
            {
                SetEncodeError(result, StatusForEncodeError(parsedScalar.ErrorCode), parsedScalar.Required, parsedScalar.ErrorCode);
                return result;
            }
            ScalarFormatResult formattedScalar = FormatParsedScalar(parsedScalar, destination);
            return CreateEncodeResult(formattedScalar, options.MaxFieldSize);
        }
        if (IsTemporalType(fieldType))
        {
            TimestampParseResult parsedTimestamp = TimestampParser.ParseTimestamp(field.Value, fieldType, options.MaxFieldSize);
            if (!parsedTimestamp.Success)
            {
                SetEncodeError(result, StatusForEncodeError(parsedTimestamp.ErrorCode), parsedTimestamp.Required, parsedTimestamp.ErrorCode);
                return result;
            }
            ScalarFormatResult formattedTimestamp = TimestampFormatter.FormatTimestamp(parsedTimestamp, destination);
            return CreateEncodeResult(formattedTimestamp, options.MaxFieldSize);
        }

        SetEncodeError(result, EncodeStatus.Invalid, 0, "unsupported-field-type");
        return result;
    }

    internal static FieldParseResult CreateDecodeSuccess(ReadOnlySpan<byte> source, int tag, FixFieldType fieldType)
    {
        FieldParseResult result = new FieldParseResult();
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = new byte[source.Length];
        source.CopyTo(field.Value);
        field.IsEncodedData = fieldType == FixFieldType.Data || fieldType == FixFieldType.XmlData;
        result.Status = DecodeStatus.Success;
        result.Field = field;
        result.Consumed = source.Length;
        result.Required = source.Length;
        return result;
    }

    internal static bool IsOpenEndedResendValue(int tag, FixFieldType fieldType, ReadOnlySpan<byte> value)
    {
        return tag == FixTags.EndSeqNo && fieldType == FixFieldType.SeqNum && value.Length == 1 && value[0] == 48;
    }

    internal static ScalarFormatResult FormatParsedScalar(ScalarParseResult parsed, Span<byte> destination)
    {
        if (parsed.FieldType == FixFieldType.Int || parsed.FieldType == FixFieldType.SeqNum || parsed.FieldType == FixFieldType.NumInGroup || parsed.FieldType == FixFieldType.Length || parsed.FieldType == FixFieldType.TagNum)
        {
            return ScalarFormatter.FormatInteger(parsed.IntegerValue, parsed.FieldType, destination);
        }
        if (parsed.FieldType == FixFieldType.Float || parsed.FieldType == FixFieldType.Qty || parsed.FieldType == FixFieldType.Price || parsed.FieldType == FixFieldType.PriceOffset || parsed.FieldType == FixFieldType.Amt || parsed.FieldType == FixFieldType.Percentage)
        {
            return ScalarFormatter.FormatDecimal(parsed.DecimalValue, parsed.FieldType, destination);
        }
        if (parsed.FieldType == FixFieldType.Char)
        {
            return ScalarFormatter.FormatCharacter(parsed.CharacterValue, parsed.FieldType, destination);
        }
        if (parsed.FieldType == FixFieldType.Boolean)
        {
            return ScalarFormatter.FormatBoolean(parsed.BooleanValue, destination);
        }
        if (parsed.FieldType == FixFieldType.String || parsed.FieldType == FixFieldType.Country || parsed.FieldType == FixFieldType.Currency || parsed.FieldType == FixFieldType.Exchange)
        {
            return ScalarFormatter.FormatText(parsed.TextValue, parsed.FieldType, destination);
        }
        if (parsed.FieldType == FixFieldType.MultipleCharValue || parsed.FieldType == FixFieldType.MultipleStringValue)
        {
            return ScalarFormatter.FormatMultipleValues(parsed.Values, parsed.FieldType, destination);
        }
        ScalarFormatResult unsupported = new ScalarFormatResult();
        unsupported.ErrorCode = "unsupported-field-type";
        return unsupported;
    }

    internal static EncodeResult CopyEncodedData(ReadOnlySpan<byte> value, Span<byte> destination)
    {
        EncodeResult result = new EncodeResult();
        result.RequiredSize = value.Length;
        if (value.Length == 0)
        {
            SetEncodeError(result, EncodeStatus.Invalid, 1, "empty-data");
            return result;
        }
        if (destination.Length < value.Length)
        {
            SetEncodeError(result, EncodeStatus.DestinationTooSmall, value.Length, "destination-too-small");
            return result;
        }
        value.CopyTo(destination);
        result.Status = EncodeStatus.Success;
        result.BytesWritten = value.Length;
        return result;
    }

    internal static EncodeResult CreateEncodeResult(ScalarFormatResult formatted, int maximumFieldSize)
    {
        EncodeResult result = new EncodeResult();
        result.RequiredSize = formatted.Required;
        if (!formatted.Success)
        {
            SetEncodeError(result, StatusForEncodeError(formatted.ErrorCode), formatted.Required, formatted.ErrorCode);
            return result;
        }
        if (formatted.Written > maximumFieldSize)
        {
            SetEncodeError(result, EncodeStatus.LimitExceeded, formatted.Written, "limit-exceeded");
            return result;
        }
        result.Status = EncodeStatus.Success;
        result.BytesWritten = formatted.Written;
        result.RequiredSize = formatted.Written;
        return result;
    }

    internal static bool IsScalarType(FixFieldType fieldType)
    {
        return fieldType >= FixFieldType.Int && fieldType <= FixFieldType.Exchange;
    }

    internal static bool IsTemporalType(FixFieldType fieldType)
    {
        return fieldType >= FixFieldType.MonthYear && fieldType <= FixFieldType.TzTimestamp;
    }

    internal static bool IsSupportedFieldType(FixFieldType fieldType)
    {
        return IsScalarType(fieldType) || IsTemporalType(fieldType) || fieldType == FixFieldType.Data || fieldType == FixFieldType.XmlData;
    }

    internal static bool IsTextualType(FixFieldType fieldType)
    {
        return fieldType == FixFieldType.Char || fieldType == FixFieldType.String || fieldType == FixFieldType.MultipleCharValue || fieldType == FixFieldType.MultipleStringValue || fieldType == FixFieldType.Country || fieldType == FixFieldType.Currency || fieldType == FixFieldType.Exchange;
    }

    internal static bool IsValidTag(int tag)
    {
        return tag >= 1 && tag <= FixDefaults.MaxTagNumber;
    }

    internal static bool IsSupportedTextEncoding(string name)
    {
        return string.Equals(name, FixDefaults.Latin1EncodingName, StringComparison.OrdinalIgnoreCase) || string.Equals(name, "latin1", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "iso8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "us-ascii", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ascii", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsAsciiEncoding(string name)
    {
        return string.Equals(name, "us-ascii", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ascii", StringComparison.OrdinalIgnoreCase);
    }

    internal static int FindNonAsciiByte(ReadOnlySpan<byte> source)
    {
        for (int index = 0; index < source.Length; index++)
        {
            if (source[index] > 127)
            {
                return index;
            }
        }
        return -1;
    }

    internal static void CopyScalarDecodeError(FieldParseResult result, ScalarParseResult parsed)
    {
        SetDecodeError(result, StatusForDecodeError(parsed.ErrorCode), parsed.ErrorIndex, parsed.Required, parsed.ErrorCode);
    }

    internal static void CopyTimestampDecodeError(FieldParseResult result, TimestampParseResult parsed)
    {
        SetDecodeError(result, StatusForDecodeError(parsed.ErrorCode), parsed.ErrorIndex, parsed.Required, parsed.ErrorCode);
    }

    internal static DecodeStatus StatusForDecodeError(string errorCode)
    {
        if (errorCode == "incomplete")
        {
            return DecodeStatus.Incomplete;
        }
        if (errorCode == "limit-exceeded")
        {
            return DecodeStatus.LimitExceeded;
        }
        return DecodeStatus.Invalid;
    }

    internal static EncodeStatus StatusForEncodeError(string errorCode)
    {
        if (errorCode == "destination-too-small")
        {
            return EncodeStatus.DestinationTooSmall;
        }
        if (errorCode == "limit-exceeded")
        {
            return EncodeStatus.LimitExceeded;
        }
        return EncodeStatus.Invalid;
    }

    internal static void SetDecodeError(FieldParseResult result, DecodeStatus status, int errorOffset, int required, string errorCode)
    {
        result.Status = status;
        result.Required = required;
        result.ErrorOffset = errorOffset;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX field value could not be decoded: {errorCode}.";
    }

    internal static void SetEncodeError(EncodeResult result, EncodeStatus status, int required, string errorCode)
    {
        result.Status = status;
        result.RequiredSize = required;
        result.ErrorCode = errorCode;
        result.ErrorMessage = $"The FIX field value could not be encoded: {errorCode}.";
    }
}
