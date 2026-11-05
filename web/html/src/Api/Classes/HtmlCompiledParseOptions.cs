using System;
using System.Text;

namespace html;
/// <summary>Contains a validated immutable snapshot of parse settings.</summary>
public class HtmlCompiledParseOptions : IHtmlParseConfiguration, IHtmlCompiledParseOptionsApi
{
    internal HtmlParseOptions options;

    internal HtmlCompiledParseOptions(HtmlParseOptions optionsValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        optionsValue.Validate();
        this.options = optionsValue.Copy();
    }

    /// <summary>Returns this validated immutable configuration.</summary>
    public HtmlCompiledParseOptions Compile()
    {
        return this;
    }

    /// <summary>Gets the response URL.</summary>
    public string GetResponseUrl()
    {
        return this.options.GetResponseUrl();
    }

    /// <summary>Gets the fallback base URL.</summary>
    public string GetBaseUrl()
    {
        return this.options.GetBaseUrl();
    }

    /// <summary>Gets the response content language.</summary>
    public string GetContentLanguage()
    {
        return this.options.GetContentLanguage();
    }

    /// <summary>Reports whether an explicit encoding is configured.</summary>
    public bool HasEncoding()
    {
        return this.options.HasEncoding();
    }

    /// <summary>Gets a defensive copy of the explicit encoding.</summary>
    public Encoding? GetEncoding()
    {
        return this.options.GetEncoding();
    }

    /// <summary>Gets the transport encoding label.</summary>
    public string GetTransportEncodingLabel()
    {
        return this.options.GetTransportEncodingLabel();
    }

    /// <summary>Reports whether scripting is enabled.</summary>
    public bool GetScriptingEnabled()
    {
        return this.options.GetScriptingEnabled();
    }

    /// <summary>Reports whether source locations are tracked.</summary>
    public bool GetTrackSourceLocations()
    {
        return this.options.GetTrackSourceLocations();
    }

    /// <summary>Reports whether byte offsets are tracked.</summary>
    public bool GetTrackByteOffsets()
    {
        return this.options.GetTrackByteOffsets();
    }

    /// <summary>Reports whether reader and stream inputs remain open.</summary>
    public bool GetLeaveOpen()
    {
        return this.options.GetLeaveOpen();
    }

    /// <summary>Gets a defensive copy of the resource limits.</summary>
    public HtmlLimits GetLimits()
    {
        return this.options.GetLimits();
    }

    internal HtmlParseOptions CreateOptions()
    {
        return this.options.Copy();
    }
}
