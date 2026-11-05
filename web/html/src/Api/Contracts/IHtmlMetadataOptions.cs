using System;

namespace html;
/// <summary>Defines configurable metadata fields, sources, and extraction limits.</summary>
public interface IHtmlMetadataOptions
{
    /// <summary>Creates an independent copy of the metadata settings.</summary>
    public HtmlMetadataOptions Copy();
    /// <summary>Reports whether a metadata field is included.</summary>
    public bool GetIncludeField(HtmlMetadataField field);
    /// <summary>Controls whether a metadata field is included.</summary>
    public void SetIncludeField(HtmlMetadataField field, bool value);
    /// <summary>Reports whether a metadata source is included.</summary>
    public bool GetIncludeSource(HtmlMetadataSource source);
    /// <summary>Controls whether a metadata source is included.</summary>
    public void SetIncludeSource(HtmlMetadataSource source, bool value);
    /// <summary>Gets an independent copy of the extraction limits.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Sets the extraction limits.</summary>
    public void SetLimits(HtmlLimits value);
}
