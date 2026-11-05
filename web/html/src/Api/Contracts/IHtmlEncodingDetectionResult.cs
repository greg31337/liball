using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlEncodingDetectionResult"/>.</summary>
public interface IHtmlEncodingDetectionResult
{
    /// <summary>Executes the GetEncodingName operation.</summary>
    public string GetEncodingName();
    /// <summary>Executes the GetConfidence operation.</summary>
    public HtmlEncodingConfidence GetConfidence();
    /// <summary>Executes the GetSource operation.</summary>
    public HtmlEncodingSource GetSource();
    /// <summary>Executes the GetByteOrderMarkLength operation.</summary>
    public int GetByteOrderMarkLength();
    /// <summary>Executes the HasDeclarationByteOffset operation.</summary>
    public bool HasDeclarationByteOffset();
    /// <summary>Executes the GetDeclarationByteOffset operation.</summary>
    public int GetDeclarationByteOffset();
}
