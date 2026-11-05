using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents the result of an HPACK encode, decode, or table operation.
/// </summary>
public class HpackResult
{
    /// <summary>
    /// The operation status.
    /// </summary>
    public HttpOperationStatus Status;

    /// <summary>
    /// The number of compressed input octets consumed.
    /// </summary>
    public int Consumed;

    /// <summary>
    /// The number of output octets written to caller-provided storage.
    /// </summary>
    public int Written;

    /// <summary>
    /// The exact output size required for a complete bounded write.
    /// </summary>
    public int RequiredSize;

    /// <summary>
    /// The ordered decoded header fields.
    /// </summary>
    public HttpHeaders Headers = new HttpHeaders();

    /// <summary>
    /// The owned encoded header-block bytes.
    /// </summary>
    public byte[] Bytes = Array.Empty<byte>();

    /// <summary>
    /// The uncompressed header-list size using RFC 7541 entry accounting.
    /// </summary>
    public int HeaderListSize;

    /// <summary>
    /// A newest-first snapshot of the dynamic table.
    /// </summary>
    public List<HpackTableEntry> DynamicTable = new List<HpackTableEntry>();

    /// <summary>
    /// The current dynamic-table size in octets.
    /// </summary>
    public int DynamicTableSize;

    /// <summary>
    /// The active dynamic-table size limit in octets.
    /// </summary>
    public int DynamicTableLimit;

    /// <summary>
    /// Indicates that an input-relative error position is available.
    /// </summary>
    public bool HasErrorPosition;

    /// <summary>
    /// The input-relative position at which malformed data was detected.
    /// </summary>
    public int ErrorPosition;

    /// <summary>
    /// Indicates that the failure maps to an HTTP/2 compression error.
    /// </summary>
    public bool IsCompressionError;

    /// <summary>
    /// A stable error identifier, or <c>null</c> when no error occurred.
    /// </summary>
    public string? ErrorCode;

    /// <summary>
    /// A caller-readable error explanation, or <c>null</c> when no error occurred.
    /// </summary>
    public string? ErrorMessage;
}
