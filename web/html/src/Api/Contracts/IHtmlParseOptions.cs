using System;
using System.Text;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlParseOptions"/>.</summary>
public interface IHtmlParseOptions
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlParseOptions Copy();
    /// <summary>Executes the GetResponseUrl operation.</summary>
    public string GetResponseUrl();
    /// <summary>Executes the SetResponseUrl operation.</summary>
    public void SetResponseUrl(string value);
    /// <summary>Executes the GetBaseUrl operation.</summary>
    public string GetBaseUrl();
    /// <summary>Executes the SetBaseUrl operation.</summary>
    public void SetBaseUrl(string value);
    /// <summary>Executes the GetContentLanguage operation.</summary>
    public string GetContentLanguage();
    /// <summary>Executes the SetContentLanguage operation.</summary>
    public void SetContentLanguage(string value);
    /// <summary>Executes the HasEncoding operation.</summary>
    public bool HasEncoding();
    /// <summary>Executes the GetEncoding operation.</summary>
    public Encoding? GetEncoding();
    /// <summary>Executes the SetEncoding operation.</summary>
    public void SetEncoding(Encoding value);
    /// <summary>Executes the GetTransportEncodingLabel operation.</summary>
    public string GetTransportEncodingLabel();
    /// <summary>Executes the SetTransportEncodingLabel operation.</summary>
    public void SetTransportEncodingLabel(string value);
    /// <summary>Executes the GetScriptingEnabled operation.</summary>
    public bool GetScriptingEnabled();
    /// <summary>Executes the SetScriptingEnabled operation.</summary>
    public void SetScriptingEnabled(bool value);
    /// <summary>Executes the GetTrackSourceLocations operation.</summary>
    public bool GetTrackSourceLocations();
    /// <summary>Executes the SetTrackSourceLocations operation.</summary>
    public void SetTrackSourceLocations(bool value);
    /// <summary>Executes the GetTrackByteOffsets operation.</summary>
    public bool GetTrackByteOffsets();
    /// <summary>Executes the SetTrackByteOffsets operation.</summary>
    public void SetTrackByteOffsets(bool value);
    /// <summary>Executes the GetLeaveOpen operation.</summary>
    public bool GetLeaveOpen();
    /// <summary>Executes the SetLeaveOpen operation.</summary>
    public void SetLeaveOpen(bool value);
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
}
