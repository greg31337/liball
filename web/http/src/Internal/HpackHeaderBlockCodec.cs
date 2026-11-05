using System;
using System.Text;

namespace http;

internal class HpackStringDecodeResult
{
    internal HttpOperationStatus Status;
    internal byte[] Bytes = Array.Empty<byte>();
    internal int Consumed;
    internal int RequiredSize;
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal bool IsCompressionError;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class HpackLiteralDecodeResult
{
    internal HttpOperationStatus Status;
    internal HttpHeaderField Field = new HttpHeaderField();
    internal int Consumed;
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal bool IsCompressionError;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class HpackDecodeLimitState
{
    internal int MaximumHeaderCount;
    internal int MaximumHeaderListSize;
    internal int HeaderCount;
    internal long HeaderListSize;
    internal bool HasError;
    internal int ErrorPosition;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class HpackHeaderBlockCodec
{
    internal static HpackResult Decode(
        ReadOnlySpan<byte> input,
        HpackDynamicTable dynamicTable,
        int maximumTableSize,
        bool requiresTableSizeUpdate,
        int requiredMinimumTableSize,
        int maximumHeaderCount,
        int maximumHeaderListSize,
        int maximumStringSize)
    {
        HpackResult result = new HpackResult();
        if (dynamicTable == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsCompressionError = true;
            result.ErrorCode = "invalid-hpack-dynamic-table";
            result.ErrorMessage = "An HPACK decoder dynamic table is required.";
            return result;
        }

        if (maximumTableSize < 0 || maximumHeaderCount < 0 ||
            maximumHeaderListSize < 0 || maximumStringSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-hpack-decode-limits";
            result.ErrorMessage = "HPACK decoder limits cannot be negative.";
            PopulateTableState(result, dynamicTable);
            return result;
        }

        HpackDecodeLimitState limits = new HpackDecodeLimitState();
        limits.MaximumHeaderCount = maximumHeaderCount;
        limits.MaximumHeaderListSize = maximumHeaderListSize;

        int position = 0;
        int tableSizeUpdateCount = 0;
        long firstTableSizeUpdate = 0;
        bool fieldRepresentationSeen = false;
        while (position < input.Length)
        {
            byte first = input[position];
            if ((first & 0xe0) == 0x20)
            {
                if (fieldRepresentationSeen)
                {
                    return RejectTableSizeUpdate(
                        result,
                        dynamicTable,
                        position,
                        position,
                        "invalid-hpack-table-size-update-placement",
                        "An HPACK dynamic-table size update must precede all header fields.");
                }

                if (tableSizeUpdateCount == 2)
                {
                    return RejectTableSizeUpdate(
                        result,
                        dynamicTable,
                        position,
                        position,
                        "too-many-hpack-table-size-updates",
                        "An HPACK header block can begin with at most two dynamic-table size updates.");
                }

                HpackIntegerResult sizeResult = HpackIntegerCodec.Decode(
                    input.Slice(position), 5);
                if (sizeResult.Status != HttpOperationStatus.Done)
                {
                    return RejectInteger(
                        result,
                        dynamicTable,
                        position,
                        sizeResult,
                        "dynamic-table-size-update");
                }

                if (sizeResult.Value > int.MaxValue ||
                    sizeResult.Value > maximumTableSize)
                {
                    return RejectTableSizeUpdate(
                        result,
                        dynamicTable,
                        position,
                        position + sizeResult.Consumed,
                        "hpack-table-size-update-exceeds-maximum",
                        "The HPACK dynamic-table size update exceeds the decoder maximum.");
                }

                if (tableSizeUpdateCount == 0 &&
                    requiresTableSizeUpdate &&
                    sizeResult.Value > requiredMinimumTableSize)
                {
                    return RejectTableSizeUpdate(
                        result,
                        dynamicTable,
                        position,
                        position + sizeResult.Consumed,
                        "invalid-hpack-table-size-update-minimum",
                        "The first HPACK dynamic-table size update does not acknowledge the smallest pending maximum.");
                }

                if (tableSizeUpdateCount == 1 &&
                    sizeResult.Value < firstTableSizeUpdate)
                {
                    return RejectTableSizeUpdate(
                        result,
                        dynamicTable,
                        position,
                        position + sizeResult.Consumed,
                        "invalid-hpack-table-size-update-sequence",
                        "The smallest of two HPACK dynamic-table size updates must be sent first.");
                }

                if (tableSizeUpdateCount == 0)
                {
                    firstTableSizeUpdate = sizeResult.Value;
                }

                dynamicTable.Resize((int)sizeResult.Value);
                tableSizeUpdateCount = tableSizeUpdateCount + 1;
                position = position + sizeResult.Consumed;
                continue;
            }

            if (requiresTableSizeUpdate && tableSizeUpdateCount == 0)
            {
                return RejectTableSizeUpdate(
                    result,
                    dynamicTable,
                    position,
                    position,
                    "required-hpack-table-size-update",
                    "The peer must begin this header block with a dynamic-table size update.");
            }

            fieldRepresentationSeen = true;
            if ((first & 0x80) != 0)
            {
                HpackIntegerResult indexResult = HpackIntegerCodec.Decode(
                    input.Slice(position), 7);
                if (indexResult.Status != HttpOperationStatus.Done)
                {
                    return RejectInteger(
                        result,
                        dynamicTable,
                        position,
                        indexResult,
                        "indexed-field");
                }

                if (indexResult.Value == 0 || indexResult.Value > int.MaxValue)
                {
                    return RejectIndex(
                        result,
                        dynamicTable,
                        position,
                        indexResult.Consumed,
                        "invalid-hpack-field-index");
                }

                HpackTableEntry entry = dynamicTable.LookupByIndex(
                    (int)indexResult.Value);
                if (!entry.HasIndex)
                {
                    return RejectIndex(
                        result,
                        dynamicTable,
                        position,
                        indexResult.Consumed,
                        "invalid-hpack-field-index");
                }

                TrackField(
                    result,
                    CreateField(entry.Name, entry.Value, false),
                    limits,
                    position);
                position = position + indexResult.Consumed;
                continue;
            }

            if ((first & 0x40) != 0)
            {
                HpackLiteralDecodeResult literal = DecodeLiteral(
                    input.Slice(position),
                    6,
                    true,
                    false,
                    dynamicTable,
                    maximumStringSize);
                if (literal.Status != HttpOperationStatus.Done)
                {
                    return RejectLiteral(result, dynamicTable, position, literal);
                }

                TrackField(result, literal.Field, limits, position);
                position = position + literal.Consumed;
                continue;
            }

            bool neverIndexed = (first & 0x10) != 0;
            HpackLiteralDecodeResult unindexed = DecodeLiteral(
                input.Slice(position),
                4,
                false,
                neverIndexed,
                dynamicTable,
                maximumStringSize);
            if (unindexed.Status != HttpOperationStatus.Done)
            {
                return RejectLiteral(result, dynamicTable, position, unindexed);
            }

            TrackField(result, unindexed.Field, limits, position);
            position = position + unindexed.Consumed;
        }

        if (requiresTableSizeUpdate && tableSizeUpdateCount == 0)
        {
            return RejectTableSizeUpdate(
                result,
                dynamicTable,
                position,
                position,
                "required-hpack-table-size-update",
                "The peer must begin this header block with a dynamic-table size update.");
        }

        if (limits.HasError)
        {
            return RejectResourceLimit(
                result,
                dynamicTable,
                limits.ErrorPosition,
                position,
                limits.ErrorCode,
                limits.ErrorMessage);
        }

        result.Status = HttpOperationStatus.Done;
        result.Consumed = position;
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackLiteralDecodeResult DecodeLiteral(
        ReadOnlySpan<byte> input,
        int prefixWidth,
        bool addToTable,
        bool neverIndexed,
        HpackDynamicTable dynamicTable,
        int maximumStringSize)
    {
        HpackLiteralDecodeResult result = new HpackLiteralDecodeResult();
        HpackIntegerResult nameIndexResult = HpackIntegerCodec.Decode(
            input, prefixWidth);
        if (nameIndexResult.Status != HttpOperationStatus.Done)
        {
            SetLiteralIntegerError(result, nameIndexResult);
            return result;
        }

        int position = nameIndexResult.Consumed;
        byte[] name;
        if (nameIndexResult.Value == 0)
        {
            HpackStringDecodeResult literalName = DecodeString(
                input.Slice(position), maximumStringSize);
            if (literalName.Status != HttpOperationStatus.Done)
            {
                SetLiteralStringError(result, literalName, position, "name");
                return result;
            }

            name = literalName.Bytes;
            position = position + literalName.Consumed;
        }
        else
        {
            if (nameIndexResult.Value > int.MaxValue)
            {
                return RejectLiteralNameIndex(result, nameIndexResult.Consumed);
            }

            HpackTableEntry indexedName = dynamicTable.LookupByIndex(
                (int)nameIndexResult.Value);
            if (!indexedName.HasIndex)
            {
                return RejectLiteralNameIndex(result, nameIndexResult.Consumed);
            }

            name = indexedName.Name;
        }

        HpackStringDecodeResult literalValue = DecodeString(
            input.Slice(position), maximumStringSize);
        if (literalValue.Status != HttpOperationStatus.Done)
        {
            SetLiteralStringError(result, literalValue, position, "value");
            return result;
        }

        position = position + literalValue.Consumed;
        result.Field = CreateField(name, literalValue.Bytes, neverIndexed);
        if (addToTable)
        {
            dynamicTable.Insert(name, literalValue.Bytes);
        }

        result.Status = HttpOperationStatus.Done;
        result.Consumed = position;
        return result;
    }

    static HpackStringDecodeResult DecodeString(
        ReadOnlySpan<byte> input,
        int maximumStringSize)
    {
        HpackStringDecodeResult result = new HpackStringDecodeResult();
        HpackIntegerResult lengthResult = HpackIntegerCodec.Decode(input, 7);
        if (lengthResult.Status != HttpOperationStatus.Done)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.Consumed = lengthResult.Consumed;
            result.RequiredSize = lengthResult.RequiredSize;
            result.HasErrorPosition = lengthResult.HasErrorPosition;
            result.ErrorPosition = lengthResult.ErrorPosition;
            result.ErrorCode = lengthResult.Status == HttpOperationStatus.NeedInput ?
                "truncated-hpack-string-length" : lengthResult.ErrorCode;
            result.ErrorMessage = lengthResult.Status == HttpOperationStatus.NeedInput ?
                "The HPACK string length is truncated." : lengthResult.ErrorMessage;
            result.IsCompressionError = true;
            return result;
        }

        if (lengthResult.Value > int.MaxValue)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.Consumed = lengthResult.Consumed;
            result.HasErrorPosition = true;
            result.ErrorPosition = lengthResult.Consumed - 1;
            result.ErrorCode = "hpack-string-length-overflow";
            result.ErrorMessage = "The HPACK string length exceeds the supported range.";
            result.IsCompressionError = true;
            return result;
        }

        int encodedLength = (int)lengthResult.Value;
        int dataPosition = lengthResult.Consumed;
        bool isHuffmanEncoded = (input[0] & 0x80) != 0;
        if (!isHuffmanEncoded && encodedLength > maximumStringSize)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.Consumed = dataPosition;
            result.HasErrorPosition = true;
            result.ErrorPosition = 0;
            result.ErrorCode = "hpack-string-limit-exceeded";
            result.ErrorMessage =
                "The decoded HPACK string exceeds the configured limit.";
            return result;
        }

        if (encodedLength > input.Length - dataPosition)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.Consumed = input.Length;
            result.RequiredSize = int.MaxValue;
            if (encodedLength <= int.MaxValue - dataPosition)
            {
                result.RequiredSize = dataPosition + encodedLength;
            }

            result.HasErrorPosition = true;
            result.ErrorPosition = input.Length;
            result.ErrorCode = "truncated-hpack-string-data";
            result.ErrorMessage = "The HPACK string data is truncated.";
            result.IsCompressionError = true;
            return result;
        }

        ReadOnlySpan<byte> encoded = input.Slice(dataPosition, encodedLength);
        if (isHuffmanEncoded)
        {
            HpackHuffmanResult huffman = HpackHuffmanCodec.Decode(
                encoded, maximumStringSize);
            if (huffman.Status != HttpOperationStatus.Done)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.Consumed = dataPosition + huffman.Consumed;
                result.RequiredSize = result.Consumed;
                result.HasErrorPosition = huffman.HasErrorPosition;
                result.ErrorPosition = dataPosition + huffman.ErrorPosition;
                result.ErrorCode = huffman.ErrorCode;
                result.ErrorMessage = huffman.ErrorMessage;
                result.IsCompressionError = huffman.IsMalformed;
                return result;
            }

            result.Bytes = huffman.Bytes;
        }
        else
        {
            result.Bytes = CopyOctets(encoded);
        }

        result.Status = HttpOperationStatus.Done;
        result.Consumed = dataPosition + encodedLength;
        result.RequiredSize = result.Consumed;
        return result;
    }

    static HttpHeaderField CreateField(
        ReadOnlySpan<byte> name,
        ReadOnlySpan<byte> value,
        bool neverIndexed)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = Encoding.Latin1.GetString(name);
        field.Value = CopyOctets(value);
        field.NeverIndex = neverIndexed;
        return field;
    }

    static void TrackField(
        HpackResult result,
        HttpHeaderField field,
        HpackDecodeLimitState limits,
        int position)
    {
        long fieldSize = (long)field.Name.Length + field.Value.Length + 32;
        limits.HeaderListSize = limits.HeaderListSize + fieldSize;
        if (limits.HeaderCount < int.MaxValue)
        {
            limits.HeaderCount = limits.HeaderCount + 1;
        }

        if (limits.HeaderListSize > int.MaxValue)
        {
            result.HeaderListSize = int.MaxValue;
        }
        else
        {
            result.HeaderListSize = (int)limits.HeaderListSize;
        }

        if (!limits.HasError &&
            limits.HeaderCount > limits.MaximumHeaderCount)
        {
            limits.HasError = true;
            limits.ErrorPosition = position;
            limits.ErrorCode = "hpack-header-count-limit-exceeded";
            limits.ErrorMessage =
                "The decoded HPACK header count exceeds the configured limit.";
        }
        else if (!limits.HasError &&
            limits.HeaderListSize > limits.MaximumHeaderListSize)
        {
            limits.HasError = true;
            limits.ErrorPosition = position;
            limits.ErrorCode = "hpack-header-list-size-limit-exceeded";
            limits.ErrorMessage =
                "The decoded HPACK header-list size exceeds the configured limit.";
        }

        if (!limits.HasError)
        {
            result.Headers.Fields.Add(field);
        }
    }

    static HpackLiteralDecodeResult RejectLiteralNameIndex(
        HpackLiteralDecodeResult result,
        int consumed)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = consumed;
        result.HasErrorPosition = true;
        result.ErrorPosition = consumed - 1;
        result.ErrorCode = "invalid-hpack-name-index";
        result.ErrorMessage = "The HPACK literal name index does not exist.";
        result.IsCompressionError = true;
        return result;
    }

    static void SetLiteralIntegerError(
        HpackLiteralDecodeResult result,
        HpackIntegerResult integer)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = integer.Consumed;
        result.HasErrorPosition = integer.HasErrorPosition;
        result.ErrorPosition = integer.ErrorPosition;
        result.ErrorCode = integer.Status == HttpOperationStatus.NeedInput ?
            "truncated-hpack-name-index" : integer.ErrorCode;
        result.ErrorMessage = integer.Status == HttpOperationStatus.NeedInput ?
            "The HPACK literal name index is truncated." : integer.ErrorMessage;
        result.IsCompressionError = true;
    }

    static void SetLiteralStringError(
        HpackLiteralDecodeResult result,
        HpackStringDecodeResult value,
        int position,
        string component)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = position + value.Consumed;
        result.HasErrorPosition = value.HasErrorPosition;
        result.ErrorPosition = position + value.ErrorPosition;
        result.ErrorCode = value.ErrorCode;
        result.ErrorMessage = $"The HPACK literal {component} is invalid. " +
            value.ErrorMessage;
        result.IsCompressionError = value.IsCompressionError;
    }

    static HpackResult RejectInteger(
        HpackResult result,
        HpackDynamicTable dynamicTable,
        int position,
        HpackIntegerResult integer,
        string component)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = position + integer.Consumed;
        result.IsCompressionError = true;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        if (integer.HasErrorPosition)
        {
            result.ErrorPosition = position + integer.ErrorPosition;
        }

        result.ErrorCode = integer.Status == HttpOperationStatus.NeedInput ?
            "truncated-hpack-integer" : integer.ErrorCode;
        result.ErrorMessage = $"The HPACK {component} integer is invalid.";
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackResult RejectResourceLimit(
        HpackResult result,
        HpackDynamicTable dynamicTable,
        int position,
        int consumed,
        string? errorCode,
        string? errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = consumed;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackResult RejectIndex(
        HpackResult result,
        HpackDynamicTable dynamicTable,
        int position,
        int consumed,
        string errorCode)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = position + consumed;
        result.IsCompressionError = true;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        result.ErrorCode = errorCode;
        result.ErrorMessage = "The HPACK index does not identify a table entry.";
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackResult RejectLiteral(
        HpackResult result,
        HpackDynamicTable dynamicTable,
        int position,
        HpackLiteralDecodeResult literal)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = position + literal.Consumed;
        result.IsCompressionError = literal.IsCompressionError;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        if (literal.HasErrorPosition)
        {
            result.ErrorPosition = position + literal.ErrorPosition;
        }

        result.ErrorCode = literal.ErrorCode;
        result.ErrorMessage = literal.ErrorMessage;
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackResult RejectTableSizeUpdate(
        HpackResult result,
        HpackDynamicTable dynamicTable,
        int position,
        int consumed,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = consumed;
        result.IsCompressionError = true;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static void PopulateTableState(
        HpackResult result,
        HpackDynamicTable dynamicTable)
    {
        HpackDynamicTableResult state = dynamicTable.Inspect(int.MaxValue);
        result.DynamicTable = state.Entries;
        result.DynamicTableSize = state.CurrentSize;
        result.DynamicTableLimit = state.Limit;
    }

    static byte[] CopyOctets(ReadOnlySpan<byte> value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = value[index];
            index = index + 1;
        }

        return result;
    }
}
