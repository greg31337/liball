using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlMetadataCandidate"/>.</summary>
public interface IHtmlMetadataCandidate
{
    /// <summary>Executes the GetField operation.</summary>
    public HtmlMetadataField GetField();
    /// <summary>Executes the GetValue operation.</summary>
    public string GetValue();
    /// <summary>Executes the GetRawValue operation.</summary>
    public string? GetRawValue();
    /// <summary>Executes the WasRawValueTruncated operation.</summary>
    public bool WasRawValueTruncated();
    /// <summary>Executes the GetSource operation.</summary>
    public HtmlMetadataSource GetSource();
    /// <summary>Executes the GetPrecedence operation.</summary>
    public int GetPrecedence();
    /// <summary>Executes the GetDocumentOrder operation.</summary>
    public int GetDocumentOrder();
}
