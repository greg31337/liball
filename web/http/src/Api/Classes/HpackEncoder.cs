namespace http;

/// <summary>
/// Encodes HPACK header blocks while retaining the encoder dynamic table.
/// </summary>
public class HpackEncoder : IHpackEncoder
{
    internal HpackDynamicTable dynamicTable = new HpackDynamicTable(
        Http2Constants.DefaultHeaderTableSize);
    internal int maximumDynamicTableSize =
        Http2Constants.DefaultHeaderTableSize;
    internal int minimumPendingDynamicTableSize =
        Http2Constants.DefaultHeaderTableSize;
    internal int finalPendingDynamicTableSize =
        Http2Constants.DefaultHeaderTableSize;
    internal bool requiresDynamicTableSizeUpdate;

    /// <summary>Creates an HPACK encoder with the default dynamic-table size.</summary>
    public HpackEncoder()
    {
    }

    /// <summary>Encodes one ordered HTTP header block.</summary>
    /// <param name="headers">The fields to encode in order.</param>
    /// <param name="options">The HPACK limits and encoder policy.</param>
    /// <returns>The encoded block and resulting table state.</returns>
    public HpackResult EncodeHeaderBlock(
        HttpHeaders headers,
        HpackOptions options)
    {
        if (headers == null || options == null)
        {
            HpackResult invalid = new HpackResult();
            invalid.Status = HttpOperationStatus.Rejected;
            invalid.ErrorCode = "invalid-hpack-encode-arguments";
            invalid.ErrorMessage = "HPACK headers and options are required.";
            return invalid;
        }

        HpackDynamicTable workingTable = dynamicTable.CloneTable();
        bool emitTableSizeUpdate = requiresDynamicTableSizeUpdate;
        int minimumTableSize = minimumPendingDynamicTableSize;
        int finalTableSize = finalPendingDynamicTableSize;
        if (options.DynamicTableSize >= 0 &&
            options.MaximumDynamicTableSize >= 0)
        {
            int requestedTableSize = options.DynamicTableSize;
            if (options.MaximumDynamicTableSize < requestedTableSize)
            {
                requestedTableSize = options.MaximumDynamicTableSize;
            }

            if (maximumDynamicTableSize < requestedTableSize)
            {
                requestedTableSize = maximumDynamicTableSize;
            }

            if (requestedTableSize != workingTable.GetLimit())
            {
                workingTable.Resize(requestedTableSize);
                if (!emitTableSizeUpdate)
                {
                    minimumTableSize = requestedTableSize;
                }
                else if (requestedTableSize < minimumTableSize)
                {
                    minimumTableSize = requestedTableSize;
                }

                finalTableSize = requestedTableSize;
                emitTableSizeUpdate = true;
            }
        }

        HpackResult result = HpackHeaderBlockEncoder.Encode(
            headers,
            options,
            workingTable,
            emitTableSizeUpdate,
            minimumTableSize,
            finalTableSize);
        if (result.Status == HttpOperationStatus.Done)
        {
            dynamicTable = workingTable;
            requiresDynamicTableSizeUpdate = false;
            minimumPendingDynamicTableSize = dynamicTable.GetLimit();
            finalPendingDynamicTableSize = dynamicTable.GetLimit();
        }
        else
        {
            HpackDynamicTableResult state = dynamicTable.Inspect(int.MaxValue);
            result.DynamicTable = state.Entries;
            result.DynamicTableSize = state.CurrentSize;
            result.DynamicTableLimit = state.Limit;
        }

        return result;
    }

    /// <summary>Updates the encoder table limit advertised by the peer.</summary>
    /// <param name="maximumSize">The peer-advertised maximum size.</param>
    /// <returns>The resulting table state.</returns>
    public HpackResult UpdateEncoderTableLimit(int maximumSize)
    {
        if (maximumSize < 0)
        {
            return CreateTableResult(dynamicTable.Resize(maximumSize));
        }

        maximumDynamicTableSize = maximumSize;
        if (maximumSize != dynamicTable.GetLimit())
        {
            if (!requiresDynamicTableSizeUpdate)
            {
                minimumPendingDynamicTableSize = maximumSize;
            }
            else if (maximumSize < minimumPendingDynamicTableSize)
            {
                minimumPendingDynamicTableSize = maximumSize;
            }

            finalPendingDynamicTableSize = maximumSize;
            requiresDynamicTableSizeUpdate = true;
        }

        HpackDynamicTableResult update = dynamicTable.Resize(maximumSize);
        return CreateTableResult(update);
    }

    /// <summary>Inspects a copy of encoder dynamic-table state.</summary>
    /// <returns>The current table state.</returns>
    public HpackResult InspectEncoderTable()
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
