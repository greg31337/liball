using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSourcePosition"/>.</summary>
public interface IHtmlSourcePosition
{
    /// <summary>Executes the IsKnown operation.</summary>
    public bool IsKnown();
    /// <summary>Executes the GetNormalizedCharacterOffset operation.</summary>
    public int GetNormalizedCharacterOffset();
    /// <summary>Executes the GetCharacterOffset operation.</summary>
    public int GetCharacterOffset();
    /// <summary>Executes the GetLine operation.</summary>
    public int GetLine();
    /// <summary>Executes the GetColumn operation.</summary>
    public int GetColumn();
    /// <summary>Executes the HasByteOffset operation.</summary>
    public bool HasByteOffset();
    /// <summary>Executes the GetByteOffset operation.</summary>
    public long GetByteOffset();
}
