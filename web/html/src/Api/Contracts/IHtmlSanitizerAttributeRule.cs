using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerAttributeRule"/>.</summary>
public interface IHtmlSanitizerAttributeRule
{
    /// <summary>Executes the IsElementSpecific operation.</summary>
    public bool IsElementSpecific();
    /// <summary>Executes the GetElementNamespaceUri operation.</summary>
    public string GetElementNamespaceUri();
    /// <summary>Executes the GetElementLocalName operation.</summary>
    public string GetElementLocalName();
    /// <summary>Executes the GetAttributeNamespaceUri operation.</summary>
    public string GetAttributeNamespaceUri();
    /// <summary>Executes the GetAttributeLocalName operation.</summary>
    public string GetAttributeLocalName();
    /// <summary>Executes the GetAction operation.</summary>
    public HtmlSanitizerAttributeAction GetAction();
    /// <summary>Executes the GetTargetNamespaceUri operation.</summary>
    public string GetTargetNamespaceUri();
    /// <summary>Executes the GetTargetLocalName operation.</summary>
    public string GetTargetLocalName();
    /// <summary>Executes the GetTargetPrefix operation.</summary>
    public string GetTargetPrefix();
    /// <summary>Executes the GetNormalization operation.</summary>
    public HtmlSanitizerAttributeNormalization GetNormalization();
    /// <summary>Gets the built-in value validation.</summary>
    public HtmlSanitizerAttributeValidation GetValidation();
    /// <summary>Gets the value prefix applied by a prefix rule.</summary>
    public string GetValuePrefix();
}
