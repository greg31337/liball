using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlSanitizerPolicy"/>.</summary>
public interface IHtmlSanitizerPolicy
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlSanitizerPolicy Copy();
    /// <summary>Executes the AllowElement operation.</summary>
    public void AllowElement(string name);
    /// <summary>Executes the AllowElementInNamespace operation.</summary>
    public void AllowElementInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the BlockElement operation.</summary>
    public void BlockElement(string name, HtmlBlockedElementAction action);
    /// <summary>Executes the BlockElementInNamespace operation.</summary>
    public void BlockElementInNamespace(string namespaceUri, string localName, HtmlBlockedElementAction action);
    /// <summary>Executes the AllowAttribute operation.</summary>
    public void AllowAttribute(string name);
    /// <summary>Executes the AllowAttributeInNamespace operation.</summary>
    public void AllowAttributeInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the AllowAttributeOnElement operation.</summary>
    public void AllowAttributeOnElement(string attributeName, string elementName);
    /// <summary>Executes the AllowAttributeOnNamespacedElement operation.</summary>
    public void AllowAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName);
    /// <summary>Executes the BlockAttribute operation.</summary>
    public void BlockAttribute(string name);
    /// <summary>Executes the BlockAttributeInNamespace operation.</summary>
    public void BlockAttributeInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the BlockAttributeOnElement operation.</summary>
    public void BlockAttributeOnElement(string attributeName, string elementName);
    /// <summary>Executes the BlockAttributeOnNamespacedElement operation.</summary>
    public void BlockAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName);
    /// <summary>Executes the RenameAttribute operation.</summary>
    public void RenameAttribute(string attributeName, string targetName);
    /// <summary>Executes the RenameAttributeInNamespace operation.</summary>
    public void RenameAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, string targetNamespaceUri, string targetLocalName, string targetPrefix);
    /// <summary>Executes the RenameAttributeOnElement operation.</summary>
    public void RenameAttributeOnElement(string attributeName, string elementName, string targetName);
    /// <summary>Executes the RenameAttributeOnNamespacedElement operation.</summary>
    public void RenameAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, string targetNamespaceUri, string targetLocalName, string targetPrefix);
    /// <summary>Executes the NormalizeAttribute operation.</summary>
    public void NormalizeAttribute(string attributeName, HtmlSanitizerAttributeNormalization normalization);
    /// <summary>Executes the NormalizeAttributeInNamespace operation.</summary>
    public void NormalizeAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, HtmlSanitizerAttributeNormalization normalization);
    /// <summary>Executes the NormalizeAttributeOnElement operation.</summary>
    public void NormalizeAttributeOnElement(string attributeName, string elementName, HtmlSanitizerAttributeNormalization normalization);
    /// <summary>Executes the NormalizeAttributeOnNamespacedElement operation.</summary>
    public void NormalizeAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, HtmlSanitizerAttributeNormalization normalization);
    /// <summary>Validates an unnamespaced attribute wherever it is accepted.</summary>
    public void ValidateAttribute(string attributeName, HtmlSanitizerAttributeValidation validation);
    /// <summary>Validates a namespace-aware attribute wherever it is accepted.</summary>
    public void ValidateAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, HtmlSanitizerAttributeValidation validation);
    /// <summary>Validates an attribute only on one HTML element.</summary>
    public void ValidateAttributeOnElement(string attributeName, string elementName, HtmlSanitizerAttributeValidation validation);
    /// <summary>Validates a namespace-aware attribute only on one element.</summary>
    public void ValidateAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, HtmlSanitizerAttributeValidation validation);
    /// <summary>Ensures a prefix on an unnamespaced attribute wherever it is accepted.</summary>
    public void PrefixAttribute(string attributeName, string prefix);
    /// <summary>Ensures a prefix on a namespace-aware attribute wherever it is accepted.</summary>
    public void PrefixAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, string prefix);
    /// <summary>Ensures a prefix on an attribute only on one HTML element.</summary>
    public void PrefixAttributeOnElement(string attributeName, string elementName, string prefix);
    /// <summary>Ensures a prefix on a namespace-aware attribute only on one element.</summary>
    public void PrefixAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, string prefix);
    /// <summary>Executes the ForceAttribute operation.</summary>
    public void ForceAttribute(string elementName, string attributeName, string value);
    /// <summary>Executes the ForceAttributeInNamespace operation.</summary>
    public void ForceAttributeInNamespace(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName, string attributePrefix, string value);
    /// <summary>Executes the RequireAttribute operation.</summary>
    public void RequireAttribute(string elementName, string attributeName, string value);
    /// <summary>Executes the RequireAttributeInNamespace operation.</summary>
    public void RequireAttributeInNamespace(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName, string attributePrefix, string value);
    /// <summary>Executes the ForceAttributeTokens operation.</summary>
    public void ForceAttributeTokens(string elementName, string attributeName, string[] tokens);
    /// <summary>Executes the ForceAttributeTokensInNamespace operation.</summary>
    public void ForceAttributeTokensInNamespace(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName, string attributePrefix, string[] tokens);
    /// <summary>Executes the AllowUrlScheme operation.</summary>
    public void AllowUrlScheme(string scheme);
    /// <summary>Executes the AllowUrlHost operation.</summary>
    public void AllowUrlHost(string host);
    /// <summary>Executes the AllowUrlPort operation.</summary>
    public void AllowUrlPort(int port);
    /// <summary>Executes the SetAllowUrlCredentials operation.</summary>
    public void SetAllowUrlCredentials(bool value);
    /// <summary>Executes the SetAllowRelativeUrls operation.</summary>
    public void SetAllowRelativeUrls(bool value);
    /// <summary>Executes the SetAllowProtocolRelativeUrls operation.</summary>
    public void SetAllowProtocolRelativeUrls(bool value);
    /// <summary>Executes the SetAllowFragmentUrls operation.</summary>
    public void SetAllowFragmentUrls(bool value);
    /// <summary>Executes the SetUrlBase operation.</summary>
    public void SetUrlBase(string value);
    /// <summary>Executes the SetRewriteRelativeUrls operation.</summary>
    public void SetRewriteRelativeUrls(bool value);
    /// <summary>Executes the SetUrlPolicy operation.</summary>
    public void SetUrlPolicy(HtmlSanitizerUrlPolicy value);
    /// <summary>Executes the AllowDataUrlMediaType operation.</summary>
    public void AllowDataUrlMediaType(string value);
    /// <summary>Executes the SetMaximumDataUrlBytes operation.</summary>
    public void SetMaximumDataUrlBytes(int value);
    /// <summary>Executes the SetUrlPolicyOnElement operation.</summary>
    public void SetUrlPolicyOnElement(string attributeName, string elementName, HtmlSanitizerUrlPolicy value);
    /// <summary>Executes the SetUrlPolicyOnNamespacedElement operation.</summary>
    public void SetUrlPolicyOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, HtmlSanitizerUrlPolicy value);
    /// <summary>Executes the AllowCssProperty operation.</summary>
    public void AllowCssProperty(string propertyName);
    /// <summary>Blocks a conservative CSS function from inline styles.</summary>
    public void BlockCssFunction(string functionName);
    /// <summary>Blocks a conservative CSS keyword from inline styles.</summary>
    public void BlockCssKeyword(string keyword);
    /// <summary>Blocks a conservative CSS unit from inline styles.</summary>
    public void BlockCssUnit(string unit);
    /// <summary>Controls custom-property declarations and references.</summary>
    public void SetAllowCssCustomProperties(bool value);
    /// <summary>Executes the SetAllowDataAttributes operation.</summary>
    public void SetAllowDataAttributes(bool value);
    /// <summary>Executes the SetAllowAriaAttributes operation.</summary>
    public void SetAllowAriaAttributes(bool value);
    /// <summary>Executes the SetAllowCustomElements operation.</summary>
    public void SetAllowCustomElements(bool value);
    /// <summary>Executes the SetAllowCustomizedBuiltInElements operation.</summary>
    public void SetAllowCustomizedBuiltInElements(bool value);
    /// <summary>Executes the SetAllowComments operation.</summary>
    public void SetAllowComments(bool value);
    /// <summary>Executes the SetAllowDoctype operation.</summary>
    public void SetAllowDoctype(bool value);
    /// <summary>Executes the SetAllowTemplateElements operation.</summary>
    public void SetAllowTemplateElements(bool value);
    /// <summary>Executes the SetNeutralizeServerTemplateMarkers operation.</summary>
    public void SetNeutralizeServerTemplateMarkers(bool value);
    /// <summary>Executes the AllowForeignNamespace operation.</summary>
    public void AllowForeignNamespace(string namespaceUri);
    /// <summary>Executes the BlockForeignNamespace operation.</summary>
    public void BlockForeignNamespace(string namespaceUri);
    /// <summary>Executes the SetDomClobberPrefix operation.</summary>
    public void SetDomClobberPrefix(string value);
    /// <summary>Executes the Compile operation.</summary>
    public HtmlCompiledSanitizerPolicy Compile();
}
