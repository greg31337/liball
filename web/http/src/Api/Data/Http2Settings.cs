using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents ordered HTTP/2 settings and their effective known values.
/// </summary>
public class Http2Settings
{
    /// <summary>
    /// The settings in received or emission order, including unknown identifiers and duplicates.
    /// </summary>
    public List<Http2Setting> Values = new List<Http2Setting>();

    /// <summary>Indicates that HEADER_TABLE_SIZE was explicitly supplied.</summary>
    public bool HasHeaderTableSize;
    /// <summary>The effective HEADER_TABLE_SIZE value.</summary>
    public long HeaderTableSize = Http2Constants.DefaultHeaderTableSize;
    /// <summary>Indicates that ENABLE_PUSH was explicitly supplied.</summary>
    public bool HasEnablePush;
    /// <summary>The effective ENABLE_PUSH value.</summary>
    public bool EnablePush = true;
    /// <summary>Indicates that MAX_CONCURRENT_STREAMS was explicitly supplied.</summary>
    public bool HasMaxConcurrentStreams;
    /// <summary>The effective MAX_CONCURRENT_STREAMS value.</summary>
    public long MaxConcurrentStreams;
    /// <summary>Indicates that INITIAL_WINDOW_SIZE was explicitly supplied.</summary>
    public bool HasInitialWindowSize;
    /// <summary>The effective INITIAL_WINDOW_SIZE value.</summary>
    public int InitialWindowSize = Http2Constants.DefaultInitialWindowSize;
    /// <summary>Indicates that MAX_FRAME_SIZE was explicitly supplied.</summary>
    public bool HasMaxFrameSize;
    /// <summary>The effective MAX_FRAME_SIZE value.</summary>
    public int MaxFrameSize = Http2Constants.DefaultMaximumFrameSize;
    /// <summary>Indicates that MAX_HEADER_LIST_SIZE was explicitly supplied.</summary>
    public bool HasMaxHeaderListSize;
    /// <summary>The effective MAX_HEADER_LIST_SIZE value.</summary>
    public long MaxHeaderListSize;
    /// <summary>Indicates that ENABLE_CONNECT_PROTOCOL was explicitly supplied.</summary>
    public bool HasEnableConnectProtocol;
    /// <summary>The effective ENABLE_CONNECT_PROTOCOL value.</summary>
    public bool EnableConnectProtocol;
    /// <summary>Indicates that NO_RFC7540_PRIORITIES was explicitly supplied.</summary>
    public bool HasNoRfc7540Priorities;
    /// <summary>The effective NO_RFC7540_PRIORITIES value.</summary>
    public bool NoRfc7540Priorities;
}
