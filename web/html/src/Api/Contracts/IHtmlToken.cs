using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlToken"/>.</summary>
public interface IHtmlToken
{
    /// <summary>Executes the GetKind operation.</summary>
    public HtmlTokenKind GetKind();
    /// <summary>Executes the HasName operation.</summary>
    public bool HasName();
    /// <summary>Executes the GetName operation.</summary>
    public string GetName();
    /// <summary>Executes the GetData operation.</summary>
    public string GetData();
    /// <summary>Executes the GetAttributes operation.</summary>
    public HtmlAttribute[] GetAttributes();
    /// <summary>Executes the IsSelfClosing operation.</summary>
    public bool IsSelfClosing();
    /// <summary>Executes the IsSelfClosingAcknowledged operation.</summary>
    public bool IsSelfClosingAcknowledged();
    /// <summary>Executes the RequiresSelfClosingAcknowledgement operation.</summary>
    public bool RequiresSelfClosingAcknowledgement();
    /// <summary>Executes the HasPublicIdentifier operation.</summary>
    public bool HasPublicIdentifier();
    /// <summary>Executes the GetPublicIdentifier operation.</summary>
    public string GetPublicIdentifier();
    /// <summary>Executes the HasSystemIdentifier operation.</summary>
    public bool HasSystemIdentifier();
    /// <summary>Executes the GetSystemIdentifier operation.</summary>
    public string GetSystemIdentifier();
    /// <summary>Executes the IsForceQuirks operation.</summary>
    public bool IsForceQuirks();
    /// <summary>Executes the GetSourceSpan operation.</summary>
    public HtmlSourceSpan GetSourceSpan();
}
