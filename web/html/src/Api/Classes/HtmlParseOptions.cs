using System;
using System.Text;

namespace html;
/// <summary>Configures document and fragment parsing, source tracking, scripting behavior, and resource limits.</summary>
public class HtmlParseOptions : IHtmlParseOptions, IHtmlParseConfiguration
{
    internal string responseUrl;
    internal string baseUrl;
    internal string contentLanguage;
    internal Encoding? encoding;
    internal string transportEncodingLabel;
    internal bool scriptingEnabled;
    internal bool trackSourceLocations;
    internal bool trackByteOffsets;
    internal bool leaveOpen;
    internal HtmlLimits limits;
    /// <summary>Creates parse options initialized to the library defaults.</summary>
    public HtmlParseOptions()
    {
        this.responseUrl = "";
        this.baseUrl = "";
        this.contentLanguage = "";
        this.encoding = null;
        this.transportEncodingLabel = "";
        this.scriptingEnabled = false;
        this.trackSourceLocations = true;
        this.trackByteOffsets = false;
        this.leaveOpen = true;
        this.limits = HtmlLimits.CreateDefault();
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <returns>A new independently mutable parse-options object initialized with the library defaults.</returns>
    internal static HtmlParseOptions CreateDefault()
    {
        return new HtmlParseOptions();
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent parse-options object containing the same settings.</returns>
    public HtmlParseOptions Copy()
    {
        HtmlParseOptions copy = new HtmlParseOptions();
        copy.responseUrl = this.responseUrl;
        copy.baseUrl = this.baseUrl;
        copy.contentLanguage = this.contentLanguage;
        copy.encoding = CopyEncoding(this.encoding);
        copy.transportEncodingLabel = this.transportEncodingLabel;
        copy.scriptingEnabled = this.scriptingEnabled;
        copy.trackSourceLocations = this.trackSourceLocations;
        copy.trackByteOffsets = this.trackByteOffsets;
        copy.leaveOpen = this.leaveOpen;
        copy.limits = this.limits.Copy();
        return copy;
    }

    /// <summary>Creates a validated immutable snapshot for concurrent reuse.</summary>
    /// <returns>An immutable compiled parse configuration.</returns>
    public HtmlCompiledParseOptions Compile()
    {
        return new HtmlCompiledParseOptions(this);
    }

    internal void Validate()
    {
        if (this.responseUrl == null || this.baseUrl == null
            || this.contentLanguage == null
            || this.transportEncodingLabel == null || this.limits == null)
        {
            throw new InvalidOperationException("Parse options contain a missing required value.");
        }

        this.limits.Validate();
    }

    /// <summary>Gets the URL from which the HTML response was obtained.</summary>
    /// <returns>The configured response URL, or an empty string when the input has no associated response.</returns>
    public string GetResponseUrl()
    {
        return this.responseUrl;
    }

    /// <summary>Sets the URL associated with the HTML response.</summary>
    /// <param name = "value">The response URL to record on parsed documents, or an empty string when none is available.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetResponseUrl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.responseUrl = value;
    }

    /// <summary>Gets the fallback URL used to resolve relative references during parsing and later DOM operations.</summary>
    /// <returns>The configured base URL, or an empty string when no fallback was supplied.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Sets the base URL used by subsequent operations.</summary>
    /// <param name = "value">An absolute base URL, or an empty string to parse without a caller-supplied base.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetBaseUrl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.baseUrl = value;
    }

    /// <summary>Gets the response content language supplied by the caller.</summary>
    /// <returns>The configured content-language value, or an empty string when none was supplied.</returns>
    public string GetContentLanguage()
    {
        return this.contentLanguage;
    }

    /// <summary>Sets the response content language recorded for parsed input.</summary>
    /// <param name = "value">The response content-language value, or an empty string when none is available.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetContentLanguage(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.contentLanguage = value;
    }

    /// <summary>Reports whether byte input uses an explicit <see cref = "Encoding"/> instead of label detection.</summary>
    /// <returns><c>true</c> when an explicit input encoding is configured; otherwise, <c>false</c>.</returns>
    public bool HasEncoding()
    {
        return this.encoding != null;
    }

    /// <summary>Gets the explicit .NET encoding selected for byte input.</summary>
    /// <returns>An independent encoding instance. Returns null when no explicit encoding object is configured.</returns>
    public Encoding? GetEncoding()
    {
        return CopyEncoding(this.encoding);
    }

    /// <summary>Sets the encoding used by subsequent operations.</summary>
    /// <param name = "value">The encoding to copy and use instead of label detection; setting it clears the configured encoding label.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetEncoding(Encoding value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.encoding = CopyEncoding(value);
        this.transportEncodingLabel = "";
    }

    /// <summary>Gets the web encoding label supplied by the transport layer.</summary>
    /// <returns>The configured transport label, or an empty string when no label is configured or an explicit <see cref = "Encoding"/> was set.</returns>
    public string GetTransportEncodingLabel()
    {
        return this.transportEncodingLabel;
    }

    /// <summary>Sets the web encoding label supplied by the transport layer.</summary>
    /// <param name = "value">The transport encoding label to normalize during byte decoding; setting it clears the explicit encoding object.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetTransportEncodingLabel(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.transportEncodingLabel = value;
        this.encoding = null;
    }

    internal string GetFallbackBaseUrl()
    {
        return this.baseUrl.Length == 0 ? this.responseUrl : this.baseUrl;
    }

    /// <summary>Reports whether tree construction uses the scripting-enabled HTML parsing branches.</summary>
    /// <returns><c>true</c> when HTML scripting behavior is enabled; otherwise, <c>false</c>.</returns>
    public bool GetScriptingEnabled()
    {
        return this.scriptingEnabled;
    }

    /// <summary>Controls the parser scripting flag used for HTML tree-construction rules.</summary>
    /// <param name = "value"><c>true</c> to use scripting-enabled parsing branches such as the HTML <c>noscript</c> rules; the library never executes script.</param>
    public void SetScriptingEnabled(bool value)
    {
        this.scriptingEnabled = value;
    }

    /// <summary>Reports whether source positions are retained.</summary>
    /// <returns><c>true</c> when source-location retention is enabled; otherwise, <c>false</c>.</returns>
    public bool GetTrackSourceLocations()
    {
        return this.trackSourceLocations;
    }

    /// <summary>Controls whether source positions are retained.</summary>
    /// <param name = "value"><c>true</c> to retain character offsets, lines, and columns on parsed tokens and nodes; otherwise, <c>false</c>.</param>
    public void SetTrackSourceLocations(bool value)
    {
        this.trackSourceLocations = value;
    }

    /// <summary>Reports whether source positions retain original byte offsets.</summary>
    /// <returns><c>true</c> when source positions include original byte offsets; otherwise, <c>false</c>.</returns>
    public bool GetTrackByteOffsets()
    {
        return this.trackByteOffsets;
    }

    /// <summary>Controls whether decoded source positions retain original byte offsets.</summary>
    /// <param name = "value"><c>true</c> to map retained source positions back to original byte offsets; byte input and source-location tracking are also required.</param>
    public void SetTrackByteOffsets(bool value)
    {
        this.trackByteOffsets = value;
    }

    /// <summary>Reports whether parsing leaves caller-provided streams and readers open.</summary>
    /// <returns><c>true</c> when disposing the decoder leaves the input stream open; otherwise, <c>false</c>.</returns>
    public bool GetLeaveOpen()
    {
        return this.leaveOpen;
    }

    /// <summary>Controls whether caller-provided streams and readers remain open after parsing.</summary>
    /// <param name = "value"><c>true</c> to leave caller-provided streams and readers open after parsing; otherwise, <c>false</c>.</param>
    public void SetLeaveOpen(bool value)
    {
        this.leaveOpen = value;
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    /// <summary>Sets the limits used by subsequent operations.</summary>
    /// <param name = "value">The validated limits to copy; later changes to the supplied object do not affect these options.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLimits(HtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.limits = value.Copy();
    }

    internal static Encoding? CopyEncoding(Encoding? value)
    {
        return value == null ? null : (Encoding)value.Clone();
    }
}
