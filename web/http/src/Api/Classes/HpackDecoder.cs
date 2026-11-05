namespace http;

/// <summary>
/// Decodes HPACK header blocks while retaining the decoder dynamic table.
/// </summary>
public class HpackDecoder : IHpackDecoder
{
    internal HpackDynamicTable dynamicTable = new HpackDynamicTable(
        Http2Constants.DefaultHeaderTableSize);
    internal int maximumDynamicTableSize =
        Http2Constants.DefaultHeaderTableSize;
    internal int minimumPendingDynamicTableSize =
        Http2Constants.DefaultHeaderTableSize;
    internal bool requiresDynamicTableSizeUpdate;

    /// <summary>Creates an HPACK decoder with the default dynamic-table size.</summary>
    public HpackDecoder()
    {
    }

    /// <summary>Decodes one complete HPACK header block.</summary>
    /// <param name="input">The compressed header-block bytes.</param>
    /// <param name="options">The HPACK limits and decoder policy.</param>
    /// <returns>The decoded fields, progress, and table state.</returns>
    public HpackResult DecodeHeaderBlock(byte[] input, HpackOptions options)
    {
        if (input == null || options == null)
        {
            HpackResult invalid = new HpackResult();
            invalid.Status = HttpOperationStatus.Rejected;
            invalid.ErrorCode = "invalid-hpack-decode-arguments";
            invalid.ErrorMessage = "HPACK input and options are required.";
            return invalid;
        }

        if (options.MaximumDynamicTableSize < 0)
        {
            HpackResult invalidMaximum = new HpackResult();
            invalidMaximum.Status = HttpOperationStatus.Rejected;
            invalidMaximum.ErrorCode = "invalid-hpack-dynamic-table-size";
            invalidMaximum.ErrorMessage =
                "An HPACK dynamic-table size cannot be negative.";
            return invalidMaximum;
        }

        if (options.Limits == null || options.MaximumHeaderCount < 0 ||
            options.MaximumHeaderListSize < 0 ||
            options.MaximumCompressedBlockSize < 0 ||
            options.MaximumStringSize < 0 ||
            options.Limits.MaximumFieldCount < 0 ||
            options.Limits.MaximumHeaderListSize < 0 ||
            options.Limits.MaximumCompressedBlockSize < 0 ||
            options.Limits.MaximumStringSize < 0)
        {
            HpackResult invalidLimits = new HpackResult();
            invalidLimits.Status = HttpOperationStatus.Rejected;
            invalidLimits.ErrorCode = "invalid-hpack-decode-limits";
            invalidLimits.ErrorMessage =
                "HPACK decoder limits are required and cannot be negative.";
            return invalidLimits;
        }

        int effectiveMaximum = maximumDynamicTableSize;
        if (options.MaximumDynamicTableSize < effectiveMaximum)
        {
            effectiveMaximum = options.MaximumDynamicTableSize;
        }

        int maximumHeaderCount = options.MaximumHeaderCount;
        if (options.Limits.MaximumFieldCount < maximumHeaderCount)
        {
            maximumHeaderCount = options.Limits.MaximumFieldCount;
        }

        int maximumHeaderListSize = options.MaximumHeaderListSize;
        if (options.Limits.MaximumHeaderListSize < maximumHeaderListSize)
        {
            maximumHeaderListSize = options.Limits.MaximumHeaderListSize;
        }

        int maximumCompressedBlockSize = options.MaximumCompressedBlockSize;
        if (options.Limits.MaximumCompressedBlockSize <
            maximumCompressedBlockSize)
        {
            maximumCompressedBlockSize =
                options.Limits.MaximumCompressedBlockSize;
        }

        int maximumStringSize = options.MaximumStringSize;
        if (options.Limits.MaximumStringSize < maximumStringSize)
        {
            maximumStringSize = options.Limits.MaximumStringSize;
        }

        if (input.Length > maximumCompressedBlockSize)
        {
            HpackResult tooLarge = CreateTableResult(
                dynamicTable.Inspect(int.MaxValue));
            tooLarge.Status = HttpOperationStatus.Rejected;
            tooLarge.HasErrorPosition = true;
            tooLarge.ErrorPosition = maximumCompressedBlockSize;
            tooLarge.ErrorCode = "hpack-compressed-block-limit-exceeded";
            tooLarge.ErrorMessage =
                "The compressed HPACK block exceeds the configured limit.";
            return tooLarge;
        }

        bool requiresUpdate = requiresDynamicTableSizeUpdate;
        int requiredMinimum = minimumPendingDynamicTableSize;
        if (effectiveMaximum < dynamicTable.GetLimit())
        {
            requiresUpdate = true;
            if (!requiresDynamicTableSizeUpdate ||
                effectiveMaximum < requiredMinimum)
            {
                requiredMinimum = effectiveMaximum;
            }
        }

        HpackResult result = HpackHeaderBlockCodec.Decode(
            input,
            dynamicTable,
            effectiveMaximum,
            requiresUpdate,
            requiredMinimum,
            maximumHeaderCount,
            maximumHeaderListSize,
            maximumStringSize);
        if (requiresUpdate &&
            (result.Status == HttpOperationStatus.Done ||
            !result.IsCompressionError))
        {
            requiresDynamicTableSizeUpdate = false;
            minimumPendingDynamicTableSize = maximumDynamicTableSize;
        }

        return result;
    }

    /// <summary>Updates the decoder table-size limit advertised to the peer.</summary>
    /// <param name="maximumSize">The new maximum size.</param>
    /// <returns>The updated table state.</returns>
    public HpackResult UpdateDecoderTableLimit(int maximumSize)
    {
        if (maximumSize < 0)
        {
            return CreateTableResult(dynamicTable.Resize(maximumSize));
        }

        maximumDynamicTableSize = maximumSize;
        if (maximumSize < dynamicTable.GetLimit())
        {
            if (!requiresDynamicTableSizeUpdate ||
                maximumSize < minimumPendingDynamicTableSize)
            {
                minimumPendingDynamicTableSize = maximumSize;
            }

            requiresDynamicTableSizeUpdate = true;
        }

        return CreateTableResult(dynamicTable.Inspect(int.MaxValue));
    }

    /// <summary>Inspects a copy of decoder dynamic-table state.</summary>
    /// <returns>The current table state.</returns>
    public HpackResult InspectDecoderTable()
    {
        HpackDynamicTableResult inspection = dynamicTable.Inspect(int.MaxValue);
        return CreateTableResult(inspection);
    }

    internal HpackResult CreateTableResult(HpackDynamicTableResult state)
    {
        HpackResult result = new HpackResult();
        result.Status = state.Status;
        result.DynamicTable = state.Entries;
        result.DynamicTableSize = state.CurrentSize;
        result.DynamicTableLimit = state.Limit;
        result.ErrorCode = state.ErrorCode;
        result.ErrorMessage = state.ErrorMessage;
        return result;
    }
}
