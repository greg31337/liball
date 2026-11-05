using System;
using System.Collections.Generic;

namespace http;

internal static class HpackHeaderBlockEncoder
{
    internal static HpackResult Encode(
        HttpHeaders headers,
        HpackOptions options,
        HpackDynamicTable dynamicTable,
        bool emitTableSizeUpdate,
        int minimumTableSize,
        int finalTableSize)
    {
        HpackResult result = new HpackResult();
        if (headers == null || options == null || dynamicTable == null)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-hpack-encode-arguments";
            result.ErrorMessage =
                "HPACK headers, options, and dynamic table are required.";
            return result;
        }

        if (!HasValidLimits(options))
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-hpack-encode-limits";
            result.ErrorMessage =
                "HPACK encoder limits are required and cannot be negative.";
            PopulateTableState(result, dynamicTable);
            return result;
        }

        int maximumHeaderCount = Minimum(
            options.MaximumHeaderCount,
            options.Limits.MaximumFieldCount);
        int maximumHeaderListSize = Minimum(
            options.MaximumHeaderListSize,
            options.Limits.MaximumHeaderListSize);
        int maximumCompressedBlockSize = Minimum(
            options.MaximumCompressedBlockSize,
            options.Limits.MaximumCompressedBlockSize);
        int maximumStringSize = Minimum(
            options.MaximumStringSize,
            options.Limits.MaximumStringSize);
        if (headers.Fields.Count > maximumHeaderCount)
        {
            return RejectLimit(
                result,
                dynamicTable,
                "hpack-header-count-limit-exceeded",
                "The HPACK header count exceeds the configured limit.");
        }

        long headerListSize = 0;
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (field == null || field.Name == null || field.Value == null ||
                !IsLatin1(field.Name))
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorCode = "invalid-hpack-header-field";
                result.ErrorMessage =
                    "Each HPACK field requires a Latin-1 name and value bytes.";
                PopulateTableState(result, dynamicTable);
                return result;
            }

            if (field.Name.Length > maximumStringSize ||
                field.Value.Length > maximumStringSize)
            {
                return RejectLimit(
                    result,
                    dynamicTable,
                    "hpack-string-limit-exceeded",
                    "An HPACK field name or value exceeds the configured string limit.");
            }

            headerListSize = headerListSize +
                field.Name.Length + field.Value.Length + 32;
            if (headerListSize > maximumHeaderListSize)
            {
                result.HeaderListSize = headerListSize > int.MaxValue ?
                    int.MaxValue : (int)headerListSize;
                return RejectLimit(
                    result,
                    dynamicTable,
                    "hpack-header-list-size-limit-exceeded",
                    "The HPACK header-list size exceeds the configured limit.");
            }

            fieldIndex = fieldIndex + 1;
        }

        result.HeaderListSize = (int)headerListSize;
        List<byte> output = new List<byte>();
        if (emitTableSizeUpdate)
        {
            AppendInteger(output, minimumTableSize, 5, 0x20);
            if (finalTableSize != minimumTableSize)
            {
                AppendInteger(output, finalTableSize, 5, 0x20);
            }

            if (output.Count > maximumCompressedBlockSize)
            {
                return RejectCompressedSize(result, dynamicTable);
            }
        }

        fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            byte[] name = GetLatin1Name(field.Name);
            HpackIndexingMode mode = SelectIndexingMode(field, options);
            if (mode != HpackIndexingMode.NeverIndexed)
            {
                HpackTableEntry exact = dynamicTable.FindExact(name, field.Value);
                if (exact.HasIndex)
                {
                    AppendInteger(output, exact.Index, 7, 0x80);
                    if (output.Count > maximumCompressedBlockSize)
                    {
                        return RejectCompressedSize(result, dynamicTable);
                    }

                    fieldIndex = fieldIndex + 1;
                    continue;
                }
            }

            HpackTableEntry named = dynamicTable.FindName(name);
            int prefixWidth = 4;
            byte prefixBits = 0x00;
            if (mode == HpackIndexingMode.Incremental)
            {
                prefixWidth = 6;
                prefixBits = 0x40;
            }
            else if (mode == HpackIndexingMode.NeverIndexed)
            {
                prefixBits = 0x10;
            }

            int nameIndex = 0;
            if (named.HasIndex)
            {
                nameIndex = named.Index;
            }

            AppendInteger(output, nameIndex, prefixWidth, prefixBits);
            if (!named.HasIndex)
            {
                AppendString(output, name, options);
            }

            AppendString(output, field.Value, options);
            if (output.Count > maximumCompressedBlockSize)
            {
                return RejectCompressedSize(result, dynamicTable);
            }

            if (mode == HpackIndexingMode.Incremental)
            {
                dynamicTable.Insert(name, field.Value);
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Status = HttpOperationStatus.Done;
        result.Bytes = output.ToArray();
        result.Written = output.Count;
        result.RequiredSize = output.Count;
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackIndexingMode SelectIndexingMode(
        HttpHeaderField field,
        HpackOptions options)
    {
        if (field.NeverIndex ||
            field.IsSensitive && options.NeverIndexSensitiveFields)
        {
            return HpackIndexingMode.NeverIndexed;
        }

        if (options.IndexingMode == HpackIndexingMode.Automatic)
        {
            return HpackIndexingMode.Incremental;
        }

        return options.IndexingMode;
    }

    static void AppendInteger(
        List<byte> output,
        long value,
        int prefixWidth,
        byte prefixBits)
    {
        HpackIntegerResult encoded = HpackIntegerCodec.Encode(
            value, prefixWidth, prefixBits);
        output.AddRange(encoded.Bytes);
    }

    static void AppendString(
        List<byte> output,
        ReadOnlySpan<byte> value,
        HpackOptions options)
    {
        HpackHuffmanResult selected = HpackHuffmanCodec.SelectString(
            value, options);
        byte prefixBits = 0x00;
        if (selected.UsedHuffman)
        {
            prefixBits = 0x80;
        }

        AppendInteger(output, selected.Bytes.Length, 7, prefixBits);
        output.AddRange(selected.Bytes);
    }

    static HpackResult RejectCompressedSize(
        HpackResult result,
        HpackDynamicTable dynamicTable)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = "hpack-compressed-block-limit-exceeded";
        result.ErrorMessage =
            "The encoded HPACK block exceeds the configured limit.";
        PopulateTableState(result, dynamicTable);
        return result;
    }

    static HpackResult RejectLimit(
        HpackResult result,
        HpackDynamicTable dynamicTable,
        string errorCode,
        string errorMessage)
    {
        result.Status = HttpOperationStatus.Rejected;
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

    static bool HasValidLimits(HpackOptions options)
    {
        if (options.Limits == null || options.MaximumDynamicTableSize < 0 ||
            options.DynamicTableSize < 0 || options.MaximumHeaderCount < 0 ||
            options.MaximumHeaderListSize < 0 ||
            options.MaximumCompressedBlockSize < 0 ||
            options.MaximumStringSize < 0)
        {
            return false;
        }

        if (options.Limits.MaximumFieldCount < 0 ||
            options.Limits.MaximumHeaderListSize < 0 ||
            options.Limits.MaximumCompressedBlockSize < 0 ||
            options.Limits.MaximumStringSize < 0)
        {
            return false;
        }

        return options.IndexingMode == HpackIndexingMode.Incremental ||
            options.IndexingMode == HpackIndexingMode.WithoutIndexing ||
            options.IndexingMode == HpackIndexingMode.NeverIndexed ||
            options.IndexingMode == HpackIndexingMode.Automatic;
    }

    static int Minimum(int left, int right)
    {
        return left < right ? left : right;
    }

    static bool IsLatin1(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0xff)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static byte[] GetLatin1Name(string value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = (byte)value[index];
            index = index + 1;
        }

        return result;
    }
}
