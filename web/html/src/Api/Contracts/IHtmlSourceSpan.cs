using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSourceSpan"/>.</summary>
public interface IHtmlSourceSpan
{
    /// <summary>Executes the IsKnown operation.</summary>
    public bool IsKnown();
    /// <summary>Executes the GetStart operation.</summary>
    public HtmlSourcePosition GetStart();
    /// <summary>Executes the GetEnd operation.</summary>
    public HtmlSourcePosition GetEnd();
    /// <summary>Executes the GetNormalizedCharacterLength operation.</summary>
    public int GetNormalizedCharacterLength();
    /// <summary>Executes the GetCharacterLength operation.</summary>
    public int GetCharacterLength();
    /// <summary>Executes the GetByteLength operation.</summary>
    public long GetByteLength();
}
