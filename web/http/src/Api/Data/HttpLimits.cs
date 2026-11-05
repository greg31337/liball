namespace http;

/// <summary>
/// Provides shared signed resource limits for HTTP codecs and semantic parsers.
/// </summary>
public class HttpLimits
{
    /// <summary>
    /// The maximum start-line size in octets.
    /// </summary>
    public int MaximumStartLineSize = 8192;

    /// <summary>
    /// The maximum size of one field line in octets.
    /// </summary>
    public int MaximumFieldLineSize = 8192;

    /// <summary>
    /// The maximum total field-section size in octets.
    /// </summary>
    public int MaximumFieldSectionSize = 65536;

    /// <summary>
    /// The maximum number of fields in one field section.
    /// </summary>
    public int MaximumFieldCount = 100;

    /// <summary>
    /// The maximum field-value size in octets.
    /// </summary>
    public int MaximumFieldValueSize = 8192;

    /// <summary>
    /// The maximum trailer-section size in octets.
    /// </summary>
    public int MaximumTrailerSectionSize = 16384;

    /// <summary>
    /// The maximum number of trailer fields.
    /// </summary>
    public int MaximumTrailerCount = 100;

    /// <summary>
    /// The maximum retained message content size in octets.
    /// </summary>
    public int MaximumContentSize = 16777216;

    /// <summary>
    /// The maximum size of one HTTP/1 chunk in octets.
    /// </summary>
    public int MaximumChunkSize = 8388608;

    /// <summary>
    /// The maximum compressed field-block size in octets.
    /// </summary>
    public int MaximumCompressedBlockSize = 65536;

    /// <summary>
    /// The maximum decoded HTTP/2 field-list size in octets.
    /// </summary>
    public int MaximumHeaderListSize = 65536;

    /// <summary>
    /// The maximum HPACK string size in octets.
    /// </summary>
    public int MaximumStringSize = 16384;

    /// <summary>
    /// The maximum accepted HTTP/2 frame payload size in octets.
    /// </summary>
    public int MaximumFrameSize = Http2Constants.DefaultMaximumFrameSize;

    /// <summary>
    /// The maximum number of requested ranges.
    /// </summary>
    public int MaximumRangeCount = 16;

    /// <summary>
    /// The maximum number of parsed parameters on one value.
    /// </summary>
    public int MaximumParameterCount = 64;

    /// <summary>
    /// The maximum number of members in one parsed list or dictionary.
    /// </summary>
    public int MaximumListMemberCount = 256;

    /// <summary>
    /// The maximum number of bytes retained for pending events or partial input.
    /// </summary>
    public int MaximumBufferedSize = 65536;
}
