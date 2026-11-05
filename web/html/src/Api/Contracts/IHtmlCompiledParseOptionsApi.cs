using System;
using System.Text;

namespace html;
/// <summary>Exposes immutable compiled parse settings.</summary>
public interface IHtmlCompiledParseOptionsApi
{
    /// <summary>Gets the response URL.</summary>
    public string GetResponseUrl();
    /// <summary>Gets the fallback base URL.</summary>
    public string GetBaseUrl();
    /// <summary>Gets the response content language.</summary>
    public string GetContentLanguage();
    /// <summary>Reports whether an explicit encoding is configured.</summary>
    public bool HasEncoding();
    /// <summary>Gets a defensive copy of the explicit encoding.</summary>
    public Encoding? GetEncoding();
    /// <summary>Gets the transport encoding label.</summary>
    public string GetTransportEncodingLabel();
    /// <summary>Reports whether scripting is enabled.</summary>
    public bool GetScriptingEnabled();
    /// <summary>Reports whether source locations are tracked.</summary>
    public bool GetTrackSourceLocations();
    /// <summary>Reports whether byte offsets are tracked.</summary>
    public bool GetTrackByteOffsets();
    /// <summary>Reports whether reader and stream inputs remain open.</summary>
    public bool GetLeaveOpen();
    /// <summary>Gets a defensive copy of the resource limits.</summary>
    public HtmlLimits GetLimits();
}
