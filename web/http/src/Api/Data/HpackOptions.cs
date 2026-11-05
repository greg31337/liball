namespace http;

/// <summary>
/// Configures HPACK table bounds, input limits, and deterministic encoder policies.
/// </summary>
public class HpackOptions
{
    /// <summary>
    /// The maximum dynamic-table size permitted by the decoder or peer setting.
    /// </summary>
    public int MaximumDynamicTableSize = Http2Constants.DefaultHeaderTableSize;

    /// <summary>
    /// The active dynamic-table size selected for a new encoder or decoder context.
    /// </summary>
    public int DynamicTableSize = Http2Constants.DefaultHeaderTableSize;

    /// <summary>
    /// The maximum decoded header-list size in octets using RFC entry accounting.
    /// </summary>
    public int MaximumHeaderListSize = 65536;

    /// <summary>
    /// The maximum compressed header-block size in octets.
    /// </summary>
    public int MaximumCompressedBlockSize = 65536;

    /// <summary>
    /// The maximum decoded name or value string size in octets.
    /// </summary>
    public int MaximumStringSize = 16384;

    /// <summary>
    /// The maximum number of decoded header fields.
    /// </summary>
    public int MaximumHeaderCount = 100;

    /// <summary>
    /// The default representation strategy used by the encoder.
    /// </summary>
    public HpackIndexingMode IndexingMode = HpackIndexingMode.Automatic;

    /// <summary>
    /// Allows the encoder to use the RFC 7541 Huffman code.
    /// </summary>
    public bool UseHuffman = true;

    /// <summary>
    /// Restricts Huffman encoding to strings for which it reduces encoded size.
    /// </summary>
    public bool UseHuffmanOnlyWhenSmaller = true;

    /// <summary>
    /// Requires sensitive fields to use the never-indexed representation.
    /// </summary>
    public bool NeverIndexSensitiveFields = true;

    /// <summary>
    /// The shared resource limits used by HPACK operations.
    /// </summary>
    public HttpLimits Limits = new HttpLimits();
}
