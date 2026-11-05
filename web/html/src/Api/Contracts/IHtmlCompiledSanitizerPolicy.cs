using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlCompiledSanitizerPolicy"/>.</summary>
public interface IHtmlCompiledSanitizerPolicy
{
    /// <summary>Executes the CreatePolicy operation.</summary>
    public HtmlSanitizerPolicy CreatePolicy();
    /// <summary>Executes the IsElementAllowed operation.</summary>
    public bool IsElementAllowed(string name);
    /// <summary>Executes the IsElementAllowedInNamespace operation.</summary>
    public bool IsElementAllowedInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the IsElementBlocked operation.</summary>
    public bool IsElementBlocked(string name);
    /// <summary>Executes the IsElementBlockedInNamespace operation.</summary>
    public bool IsElementBlockedInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the GetBlockedElementAction operation.</summary>
    public HtmlBlockedElementAction GetBlockedElementAction(string name);
    /// <summary>Executes the GetBlockedElementActionInNamespace operation.</summary>
    public HtmlBlockedElementAction GetBlockedElementActionInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the IsAttributeAllowed operation.</summary>
    public bool IsAttributeAllowed(string attributeName, string elementName);
    /// <summary>Executes the IsAttributeAllowedInNamespaces operation.</summary>
    public bool IsAttributeAllowedInNamespaces(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName);
    /// <summary>Executes the HasForcedAttribute operation.</summary>
    public bool HasForcedAttribute(string elementName, string attributeName);
    /// <summary>Executes the HasForcedAttributeInNamespaces operation.</summary>
    public bool HasForcedAttributeInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName);
    /// <summary>Executes the HasRequiredAttribute operation.</summary>
    public bool HasRequiredAttribute(string elementName, string attributeName);
    /// <summary>Executes the HasRequiredAttributeInNamespaces operation.</summary>
    public bool HasRequiredAttributeInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName);
    /// <summary>Executes the GetForcedAttributeValue operation.</summary>
    public string GetForcedAttributeValue(string elementName, string attributeName);
    /// <summary>Executes the GetForcedAttributeValueInNamespaces operation.</summary>
    public string GetForcedAttributeValueInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName);
    /// <summary>Executes the GetRequiredAttributeValue operation.</summary>
    public string GetRequiredAttributeValue(string elementName, string attributeName);
    /// <summary>Executes the GetRequiredAttributeValueInNamespaces operation.</summary>
    public string GetRequiredAttributeValueInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName);
    /// <summary>Executes the IsUrlSchemeAllowed operation.</summary>
    public bool IsUrlSchemeAllowed(string scheme);
    /// <summary>Executes the IsUrlHostAllowed operation.</summary>
    public bool IsUrlHostAllowed(string host);
    /// <summary>Executes the IsUrlPortAllowed operation.</summary>
    public bool IsUrlPortAllowed(int port);
    /// <summary>Executes the IsCssPropertyAllowed operation.</summary>
    public bool IsCssPropertyAllowed(string propertyName);
    /// <summary>Executes the GetAllowDataAttributes operation.</summary>
    public bool GetAllowDataAttributes();
    /// <summary>Executes the GetAllowAriaAttributes operation.</summary>
    public bool GetAllowAriaAttributes();
    /// <summary>Executes the GetAllowCustomElements operation.</summary>
    public bool GetAllowCustomElements();
    /// <summary>Executes the GetAllowCustomizedBuiltInElements operation.</summary>
    public bool GetAllowCustomizedBuiltInElements();
    /// <summary>Executes the GetAllowComments operation.</summary>
    public bool GetAllowComments();
    /// <summary>Executes the GetAllowDoctype operation.</summary>
    public bool GetAllowDoctype();
    /// <summary>Executes the GetAllowTemplateElements operation.</summary>
    public bool GetAllowTemplateElements();
    /// <summary>Executes the GetNeutralizeServerTemplateMarkers operation.</summary>
    public bool GetNeutralizeServerTemplateMarkers();
    /// <summary>Executes the GetAllowedForeignNamespaces operation.</summary>
    public string[] GetAllowedForeignNamespaces();
    /// <summary>Executes the IsForeignNamespaceAllowed operation.</summary>
    public bool IsForeignNamespaceAllowed(string namespaceUri);
    /// <summary>Executes the IsCustomizedBuiltInElementAllowed operation.</summary>
    public bool IsCustomizedBuiltInElementAllowed(string elementNamespaceUri, string elementLocalName, string isValue);
    /// <summary>Executes the GetDomClobberPrefix operation.</summary>
    public string GetDomClobberPrefix();
    /// <summary>Executes the GetAllowedElements operation.</summary>
    public string[] GetAllowedElements();
    /// <summary>Executes the GetBlockedElements operation.</summary>
    public string[] GetBlockedElements();
    /// <summary>Executes the GetAllowedElementRules operation.</summary>
    public HtmlSanitizerElementName[] GetAllowedElementRules();
    /// <summary>Executes the GetAllowedAttributes operation.</summary>
    public string[] GetAllowedAttributes();
    /// <summary>Executes the GetAllowedAttributesForElement operation.</summary>
    public string[] GetAllowedAttributesForElement(string elementName);
    /// <summary>Executes the GetForcedAttributesForElement operation.</summary>
    public string[] GetForcedAttributesForElement(string elementName);
    /// <summary>Executes the GetAllowedUrlSchemes operation.</summary>
    public string[] GetAllowedUrlSchemes();
    /// <summary>Executes the GetAllowedUrlHosts operation.</summary>
    public string[] GetAllowedUrlHosts();
    /// <summary>Executes the GetGlobalUrlPolicy operation.</summary>
    public HtmlSanitizerUrlPolicy GetGlobalUrlPolicy();
    /// <summary>Executes the GetAllowedUrlPorts operation.</summary>
    public int[] GetAllowedUrlPorts();
    /// <summary>Executes the GetAllowUrlCredentials operation.</summary>
    public bool GetAllowUrlCredentials();
    /// <summary>Executes the GetAllowRelativeUrls operation.</summary>
    public bool GetAllowRelativeUrls();
    /// <summary>Executes the GetAllowProtocolRelativeUrls operation.</summary>
    public bool GetAllowProtocolRelativeUrls();
    /// <summary>Executes the GetAllowFragmentUrls operation.</summary>
    public bool GetAllowFragmentUrls();
    /// <summary>Executes the GetUrlBase operation.</summary>
    public string GetUrlBase();
    /// <summary>Executes the GetRewriteRelativeUrls operation.</summary>
    public bool GetRewriteRelativeUrls();
    /// <summary>Executes the GetAllowedDataUrlMediaTypes operation.</summary>
    public string[] GetAllowedDataUrlMediaTypes();
    /// <summary>Executes the GetMaximumDataUrlBytes operation.</summary>
    public int GetMaximumDataUrlBytes();
    /// <summary>Executes the GetUrlAttributeRules operation.</summary>
    public HtmlSanitizerUrlAttributeRule[] GetUrlAttributeRules();
    /// <summary>Executes the GetUrlPolicy operation.</summary>
    public HtmlSanitizerUrlPolicy GetUrlPolicy(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName);
    /// <summary>Executes the GetAllowedCssProperties operation.</summary>
    public string[] GetAllowedCssProperties();
    /// <summary>Gets blocked conservative CSS functions.</summary>
    public string[] GetBlockedCssFunctions();
    /// <summary>Gets blocked conservative CSS keywords.</summary>
    public string[] GetBlockedCssKeywords();
    /// <summary>Gets blocked conservative CSS units.</summary>
    public string[] GetBlockedCssUnits();
    /// <summary>Reports whether a CSS function is allowed.</summary>
    public bool IsCssFunctionAllowed(string functionName);
    /// <summary>Reports whether a CSS keyword is allowed.</summary>
    public bool IsCssKeywordAllowed(string keyword);
    /// <summary>Reports whether a CSS unit is allowed.</summary>
    public bool IsCssUnitAllowed(string unit);
    /// <summary>Reports whether custom CSS properties are allowed.</summary>
    public bool GetAllowCssCustomProperties();
    /// <summary>Executes the GetBlockedElementRules operation.</summary>
    public HtmlSanitizerBlockedElementRule[] GetBlockedElementRules();
    /// <summary>Executes the GetElementAttributeRules operation.</summary>
    public HtmlSanitizerElementAttributeRule[] GetElementAttributeRules();
    /// <summary>Executes the GetForcedAttributeRules operation.</summary>
    public HtmlSanitizerForcedAttributeRule[] GetForcedAttributeRules();
    /// <summary>Executes the GetSanitizerAttributeRules operation.</summary>
    public HtmlSanitizerAttributeRule[] GetSanitizerAttributeRules();
    /// <summary>Executes the GetSanitizerAttributeValueRules operation.</summary>
    public HtmlSanitizerAttributeValueRule[] GetSanitizerAttributeValueRules();
    /// <summary>Executes the GetSanitizerAttributeTokenRules operation.</summary>
    public HtmlSanitizerAttributeTokenRule[] GetSanitizerAttributeTokenRules();
}
