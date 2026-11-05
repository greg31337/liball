using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSerializerOptions"/>.</summary>
public interface IHtmlSerializerOptions
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlSerializerOptions Copy();
    /// <summary>Executes the GetNameMode operation.</summary>
    public HtmlSerializerNameMode GetNameMode();
    /// <summary>Executes the SetNameMode operation.</summary>
    public void SetNameMode(HtmlSerializerNameMode value);
    /// <summary>Executes the GetQuoteMode operation.</summary>
    public HtmlSerializerQuoteMode GetQuoteMode();
    /// <summary>Executes the SetQuoteMode operation.</summary>
    public void SetQuoteMode(HtmlSerializerQuoteMode value);
    /// <summary>Executes the GetAsciiOnly operation.</summary>
    public bool GetAsciiOnly();
    /// <summary>Executes the SetAsciiOnly operation.</summary>
    public void SetAsciiOnly(bool value);
    /// <summary>Executes the GetSortAttributes operation.</summary>
    public bool GetSortAttributes();
    /// <summary>Executes the SetSortAttributes operation.</summary>
    public void SetSortAttributes(bool value);
    /// <summary>Executes the GetSelfCloseForeignElements operation.</summary>
    public bool GetSelfCloseForeignElements();
    /// <summary>Executes the SetSelfCloseForeignElements operation.</summary>
    public void SetSelfCloseForeignElements(bool value);
    /// <summary>Executes the GetPretty operation.</summary>
    public bool GetPretty();
    /// <summary>Executes the SetPretty operation.</summary>
    public void SetPretty(bool value);
    /// <summary>Executes the GetOmitOptionalTags operation.</summary>
    public bool GetOmitOptionalTags();
    /// <summary>Executes the SetOmitOptionalTags operation.</summary>
    public void SetOmitOptionalTags(bool value);
    /// <summary>Executes the GetIndentation operation.</summary>
    public string GetIndentation();
    /// <summary>Executes the SetIndentation operation.</summary>
    public void SetIndentation(string value);
    /// <summary>Executes the GetNewLine operation.</summary>
    public string GetNewLine();
    /// <summary>Executes the SetNewLine operation.</summary>
    public void SetNewLine(string value);
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
}
