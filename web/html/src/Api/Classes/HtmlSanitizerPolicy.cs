using System;
using System.Collections.Generic;

namespace html;
/// <summary>Builds mutable sanitizer rules that can be compiled into a validated reusable policy.</summary>
public class HtmlSanitizerPolicy : IHtmlSanitizerPolicy
{
    internal static readonly HtmlCompiledSanitizerPolicy textOnlyPreset = HtmlSanitizerPresets.CreateTextOnly();
    internal static readonly HtmlCompiledSanitizerPolicy basicFormattingPreset = HtmlSanitizerPresets.CreateBasicFormatting();
    internal static readonly HtmlCompiledSanitizerPolicy richTextPreset = HtmlSanitizerPresets.CreateRichText();
    internal static readonly HtmlCompiledSanitizerPolicy htmlOnlyPreset = HtmlSanitizerPresets.CreateHtmlOnly();
    internal List<HtmlSanitizerElementName> allowedElements = new List<HtmlSanitizerElementName>();
    internal List<HtmlSanitizerBlockedElementRule> blockedElements = new List<HtmlSanitizerBlockedElementRule>();
    internal List<string> allowedAttributes = new List<string>();
    internal List<HtmlSanitizerElementAttributeRule> elementAttributes = new List<HtmlSanitizerElementAttributeRule>();
    internal List<HtmlSanitizerForcedAttributeRule> forcedAttributes = new List<HtmlSanitizerForcedAttributeRule>();
    internal List<HtmlSanitizerAttributeRule> attributeRules = new List<HtmlSanitizerAttributeRule>();
    internal List<HtmlSanitizerAttributeValueRule> attributeValueRules = new List<HtmlSanitizerAttributeValueRule>();
    internal List<HtmlSanitizerAttributeTokenRule> attributeTokenRules = new List<HtmlSanitizerAttributeTokenRule>();
    internal List<string> urlSchemes = new List<string>();
    internal List<string> urlHosts = new List<string>();
    internal List<int> urlPorts = new List<int>();
    internal bool allowUrlCredentials;
    internal bool allowRelativeUrls;
    internal bool allowProtocolRelativeUrls;
    internal bool allowFragmentUrls;
    internal string urlBase = "";
    internal bool rewriteRelativeUrls;
    internal List<HtmlSanitizerUrlAttributeRule> urlAttributeRules = new List<HtmlSanitizerUrlAttributeRule>();
    internal List<string> dataUrlMediaTypes = new List<string>();
    internal int maximumDataUrlBytes;
    internal List<string> cssProperties = new List<string>();
    internal List<string> blockedCssFunctions = new List<string>();
    internal List<string> blockedCssKeywords = new List<string>();
    internal List<string> blockedCssUnits = new List<string>();
    internal bool allowCssCustomProperties;
    internal bool allowDataAttributes;
    internal bool allowAriaAttributes;
    internal string domClobberPrefix = "user-content-";
    internal bool allowCustomElements;
    internal bool allowCustomizedBuiltInElements;
    internal bool allowComments;
    internal bool allowDoctype;
    internal bool allowTemplateElements;
    internal bool neutralizeServerTemplateMarkers;
    internal List<string> foreignNamespaces = new List<string>();
    /// <summary>Creates a policy that retains text while removing markup.</summary>
    /// <returns>An independently mutable policy that removes elements while retaining their text.</returns>
    internal static HtmlSanitizerPolicy CreateTextOnly()
    {
        return textOnlyPreset.CreatePolicy();
    }

    /// <summary>Creates a policy that retains a conservative set of basic formatting elements.</summary>
    /// <returns>An independently mutable policy for conservative inline and block formatting.</returns>
    internal static HtmlSanitizerPolicy CreateBasicFormatting()
    {
        return basicFormattingPreset.CreatePolicy();
    }

    /// <summary>Creates a policy for common rich-text content without active content.</summary>
    /// <returns>An independently mutable policy for common rich-text markup without active content.</returns>
    internal static HtmlSanitizerPolicy CreateRichText()
    {
        return richTextPreset.CreatePolicy();
    }

    /// <summary>Creates a policy that retains ordinary HTML while excluding foreign namespaces.</summary>
    /// <returns>An independently mutable policy for ordinary HTML elements without SVG or MathML.</returns>
    internal static HtmlSanitizerPolicy CreateHtmlOnly()
    {
        return htmlOnlyPreset.CreatePolicy();
    }

    /// <summary>Creates an empty, default-deny policy that must be populated before content can be retained.</summary>
    public HtmlSanitizerPolicy()
    {
        this.Initialize();
    }

    /// <summary>Creates an independently mutable policy builder by copying a compiled policy.</summary>
    /// <param name = "compiled">The compiled policy to copy into mutable settings.</param>
    /// <returns>An independently mutable policy containing the compiled settings.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static HtmlSanitizerPolicy Create(HtmlCompiledSanitizerPolicy compiled)
    {
        if (compiled == null)
        {
            throw new ArgumentNullException("compiled");
        }

        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.CopyFrom(compiled);
        return policy;
    }

    internal void CopyFrom(HtmlCompiledSanitizerPolicy compiled)
    {
        HtmlSanitizerElementName[] elementNames = compiled.GetAllowedElementRules();
        int index = 0;
        while (index < elementNames.Length)
        {
            this.allowedElements.Add(elementNames[index].Copy());
            index = index + 1;
        }

        HtmlSanitizerBlockedElementRule[] blocked = compiled.GetBlockedElementRules();
        index = 0;
        while (index < blocked.Length)
        {
            this.blockedElements.Add(blocked[index].Copy());
            index = index + 1;
        }

        string[] names = compiled.GetAllowedAttributes();
        index = 0;
        while (index < names.Length)
        {
            this.allowedAttributes.Add(names[index]);
            index = index + 1;
        }

        HtmlSanitizerElementAttributeRule[] elementRules = compiled.GetElementAttributeRules();
        index = 0;
        while (index < elementRules.Length)
        {
            this.elementAttributes.Add(elementRules[index].Copy());
            index = index + 1;
        }

        HtmlSanitizerForcedAttributeRule[] forced = compiled.GetForcedAttributeRules();
        index = 0;
        while (index < forced.Length)
        {
            this.forcedAttributes.Add(forced[index].Copy());
            index = index + 1;
        }

        HtmlSanitizerAttributeRule[] attributeRuleValues = compiled.GetSanitizerAttributeRules();
        index = 0;
        while (index < attributeRuleValues.Length)
        {
            this.attributeRules.Add(attributeRuleValues[index].Copy());
            index = index + 1;
        }

        HtmlSanitizerAttributeValueRule[] valueRuleValues = compiled.GetSanitizerAttributeValueRules();
        index = 0;
        while (index < valueRuleValues.Length)
        {
            this.attributeValueRules.Add(valueRuleValues[index].Copy());
            index = index + 1;
        }

        HtmlSanitizerAttributeTokenRule[] tokenRuleValues = compiled.GetSanitizerAttributeTokenRules();
        index = 0;
        while (index < tokenRuleValues.Length)
        {
            this.attributeTokenRules.Add(tokenRuleValues[index].Copy());
            index = index + 1;
        }

        AddStrings(this.urlSchemes, compiled.GetAllowedUrlSchemes());
        AddStrings(this.urlHosts, compiled.GetAllowedUrlHosts());
        HtmlSanitizerUrlPolicy globalUrlPolicy = compiled.GetGlobalUrlPolicy();
        AddInts(this.urlPorts, globalUrlPolicy.GetAllowedPorts());
        this.allowUrlCredentials = globalUrlPolicy.GetAllowCredentials();
        this.allowRelativeUrls = globalUrlPolicy.GetAllowRelativeUrls();
        this.allowProtocolRelativeUrls = globalUrlPolicy.GetAllowProtocolRelativeUrls();
        this.allowFragmentUrls = globalUrlPolicy.GetAllowFragmentUrls();
        this.urlBase = globalUrlPolicy.GetBaseUrl();
        this.rewriteRelativeUrls = globalUrlPolicy.GetRewriteRelativeUrls();
        AddStrings(this.dataUrlMediaTypes, globalUrlPolicy.GetAllowedDataMediaTypes());
        this.maximumDataUrlBytes = globalUrlPolicy.GetMaximumDataUrlBytes();
        HtmlSanitizerUrlAttributeRule[] urlRules = compiled.GetUrlAttributeRules();
        index = 0;
        while (index < urlRules.Length)
        {
            this.urlAttributeRules.Add(urlRules[index].Copy());
            index = index + 1;
        }

        AddStrings(this.cssProperties, compiled.GetAllowedCssProperties());
        AddStrings(this.blockedCssFunctions, compiled.GetBlockedCssFunctions());
        AddStrings(this.blockedCssKeywords, compiled.GetBlockedCssKeywords());
        AddStrings(this.blockedCssUnits, compiled.GetBlockedCssUnits());
        this.allowCssCustomProperties = compiled.GetAllowCssCustomProperties();
        this.allowDataAttributes = compiled.GetAllowDataAttributes();
        this.allowAriaAttributes = compiled.GetAllowAriaAttributes();
        this.domClobberPrefix = compiled.GetDomClobberPrefix();
        this.allowCustomElements = compiled.GetAllowCustomElements();
        this.allowCustomizedBuiltInElements = compiled.GetAllowCustomizedBuiltInElements();
        this.allowComments = compiled.GetAllowComments();
        this.allowDoctype = compiled.GetAllowDoctype();
        this.allowTemplateElements = compiled.GetAllowTemplateElements();
        this.neutralizeServerTemplateMarkers = compiled.GetNeutralizeServerTemplateMarkers();
        AddStrings(this.foreignNamespaces, compiled.GetAllowedForeignNamespaces());
    }

    internal void Initialize()
    {
        this.allowedElements = new List<HtmlSanitizerElementName>();
        this.blockedElements = new List<HtmlSanitizerBlockedElementRule>();
        this.allowedAttributes = new List<string>();
        this.elementAttributes = new List<HtmlSanitizerElementAttributeRule>();
        this.forcedAttributes = new List<HtmlSanitizerForcedAttributeRule>();
        this.attributeRules = new List<HtmlSanitizerAttributeRule>();
        this.attributeValueRules = new List<HtmlSanitizerAttributeValueRule>();
        this.attributeTokenRules = new List<HtmlSanitizerAttributeTokenRule>();
        this.urlSchemes = new List<string>();
        this.urlHosts = new List<string>();
        this.urlPorts = new List<int>();
        this.allowUrlCredentials = false;
        this.allowRelativeUrls = true;
        this.allowProtocolRelativeUrls = false;
        this.allowFragmentUrls = true;
        this.urlBase = "";
        this.rewriteRelativeUrls = false;
        this.urlAttributeRules = new List<HtmlSanitizerUrlAttributeRule>();
        this.dataUrlMediaTypes = new List<string>();
        this.maximumDataUrlBytes = 0;
        this.cssProperties = new List<string>();
        this.blockedCssFunctions = new List<string>();
        this.blockedCssKeywords = new List<string>();
        this.blockedCssUnits = new List<string>();
        this.allowCssCustomProperties = false;
        this.allowDataAttributes = false;
        this.allowAriaAttributes = false;
        this.domClobberPrefix = "user-content-";
        this.allowCustomElements = false;
        this.allowCustomizedBuiltInElements = false;
        this.allowComments = false;
        this.allowDoctype = false;
        this.allowTemplateElements = false;
        this.neutralizeServerTemplateMarkers = false;
        this.foreignNamespaces = new List<string>();
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent policy containing copies of the current rules and configuration.</returns>
    public HtmlSanitizerPolicy Copy()
    {
        HtmlSanitizerPolicy copy = new HtmlSanitizerPolicy();
        int index = 0;
        while (index < this.allowedElements.Count)
        {
            copy.allowedElements.Add(this.allowedElements[index].Copy());
            index = index + 1;
        }

        index = 0;
        while (index < this.blockedElements.Count)
        {
            copy.blockedElements.Add(this.blockedElements[index].Copy());
            index = index + 1;
        }

        AddStrings(copy.allowedAttributes, this.allowedAttributes.ToArray());
        index = 0;
        while (index < this.elementAttributes.Count)
        {
            copy.elementAttributes.Add(this.elementAttributes[index].Copy());
            index = index + 1;
        }

        index = 0;
        while (index < this.forcedAttributes.Count)
        {
            copy.forcedAttributes.Add(this.forcedAttributes[index].Copy());
            index = index + 1;
        }

        index = 0;
        while (index < this.attributeRules.Count)
        {
            copy.attributeRules.Add(this.attributeRules[index].Copy());
            index = index + 1;
        }

        index = 0;
        while (index < this.attributeValueRules.Count)
        {
            copy.attributeValueRules.Add(this.attributeValueRules[index].Copy());
            index = index + 1;
        }

        index = 0;
        while (index < this.attributeTokenRules.Count)
        {
            copy.attributeTokenRules.Add(this.attributeTokenRules[index].Copy());
            index = index + 1;
        }

        AddStrings(copy.urlSchemes, this.urlSchemes.ToArray());
        AddStrings(copy.urlHosts, this.urlHosts.ToArray());
        AddInts(copy.urlPorts, this.urlPorts.ToArray());
        copy.allowUrlCredentials = this.allowUrlCredentials;
        copy.allowRelativeUrls = this.allowRelativeUrls;
        copy.allowProtocolRelativeUrls = this.allowProtocolRelativeUrls;
        copy.allowFragmentUrls = this.allowFragmentUrls;
        copy.urlBase = this.urlBase;
        copy.rewriteRelativeUrls = this.rewriteRelativeUrls;
        AddStrings(copy.dataUrlMediaTypes, this.dataUrlMediaTypes.ToArray());
        copy.maximumDataUrlBytes = this.maximumDataUrlBytes;
        index = 0;
        while (index < this.urlAttributeRules.Count)
        {
            copy.urlAttributeRules.Add(this.urlAttributeRules[index].Copy());
            index = index + 1;
        }

        AddStrings(copy.cssProperties, this.cssProperties.ToArray());
        AddStrings(copy.blockedCssFunctions, this.blockedCssFunctions.ToArray());
        AddStrings(copy.blockedCssKeywords, this.blockedCssKeywords.ToArray());
        AddStrings(copy.blockedCssUnits, this.blockedCssUnits.ToArray());
        copy.allowCssCustomProperties = this.allowCssCustomProperties;
        copy.allowDataAttributes = this.allowDataAttributes;
        copy.allowAriaAttributes = this.allowAriaAttributes;
        copy.domClobberPrefix = this.domClobberPrefix;
        copy.allowCustomElements = this.allowCustomElements;
        copy.allowCustomizedBuiltInElements = this.allowCustomizedBuiltInElements;
        copy.allowComments = this.allowComments;
        copy.allowDoctype = this.allowDoctype;
        copy.allowTemplateElements = this.allowTemplateElements;
        copy.neutralizeServerTemplateMarkers = this.neutralizeServerTemplateMarkers;
        AddStrings(copy.foreignNamespaces, this.foreignNamespaces.ToArray());
        return copy;
    }

    internal void Validate()
    {
        HtmlSanitizerPolicyNames.ValidateClobberPrefix(this.domClobberPrefix, "DomClobberPrefix");
        int index = 0;
        while (index < this.elementAttributes.Count)
        {
            HtmlSanitizerElementAttributeRule rule = this.elementAttributes[index];
            if (!ContainsElement(this.allowedElements, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), rule.GetElementName()))
            {
                throw new InvalidOperationException("An element-specific attribute rule targets an element that is not allowed: " + rule.GetElementName() + ".");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.forcedAttributes.Count)
        {
            HtmlSanitizerForcedAttributeRule rule = this.forcedAttributes[index];
            if (!ContainsElement(this.allowedElements, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), rule.GetElementName()))
            {
                throw new InvalidOperationException("A forced attribute targets an element that is not allowed: " + rule.GetElementName() + ".");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.attributeRules.Count)
        {
            HtmlSanitizerAttributeRule rule = this.attributeRules[index];
            if (rule.IsElementSpecific() && !this.IsConfiguredElementAllowed(rule.GetElementNamespaceUri(), rule.GetElementLocalName()))
            {
                throw new InvalidOperationException("An attribute action targets an element that is not allowed: " + rule.GetElementLocalName() + ".");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.attributeValueRules.Count)
        {
            HtmlSanitizerAttributeValueRule rule = this.attributeValueRules[index];
            if (!ContainsElement(this.allowedElements, rule.GetElementNamespaceUri(), rule.GetElementLocalName()))
            {
                throw new InvalidOperationException("A required or forced attribute targets an element that is not allowed: " + rule.GetElementLocalName() + ".");
            }

            if (HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(rule.GetAttributeNamespaceUri(), rule.GetAttributeLocalName()) && (!this.allowCustomizedBuiltInElements || rule.GetElementNamespaceUri() != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() || HtmlSanitizerMarkupNames.IsCustomElementName(rule.GetElementLocalName()) || !HtmlSanitizerMarkupNames.IsCustomElementName(rule.GetValue())))
            {
                throw new InvalidOperationException("A required or forced is attribute must configure a valid customized built-in element.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.attributeTokenRules.Count)
        {
            HtmlSanitizerAttributeTokenRule rule = this.attributeTokenRules[index];
            if (!ContainsElement(this.allowedElements, rule.GetElementNamespaceUri(), rule.GetElementLocalName()))
            {
                throw new InvalidOperationException("A forced attribute token rule targets an element that is not allowed: " + rule.GetElementLocalName() + ".");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.urlAttributeRules.Count)
        {
            HtmlSanitizerUrlAttributeRule rule = this.urlAttributeRules[index];
            if (!this.IsConfiguredElementAllowed(rule.GetElementNamespaceUri(), rule.GetElementLocalName()))
            {
                throw new InvalidOperationException("A URL attribute override targets an element that is not allowed: " + rule.GetElementLocalName() + ".");
            }

            index = index + 1;
        }
    }

    internal bool IsConfiguredElementAllowed(string namespaceUri, string localName)
    {
        if (ContainsElement(this.allowedElements, namespaceUri, localName))
        {
            return true;
        }

        if (namespaceUri == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            return this.allowTemplateElements && localName == "template" || this.allowCustomElements && HtmlSanitizerMarkupNames.IsCustomElementName(localName);
        }

        return Contains(this.foreignNamespaces, namespaceUri);
    }

    /// <summary>Allows the element in the mutable policy.</summary>
    /// <param name = "name">The HTML element local name to allow.</param>
    public void AllowElement(string name)
    {
        this.AllowElementInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), name);
    }

    /// <summary>Allows the element in the mutable policy.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    public void AllowElementInNamespace(string namespaceUri, string localName)
    {
        HtmlSanitizerElementName element = new HtmlSanitizerElementName(namespaceUri, localName);
        AddUniqueElement(this.allowedElements, element);
        RemoveBlocked(this.blockedElements, element.GetNamespaceUri(), element.GetLocalName());
    }

    /// <summary>Blocks the element according to the selected action.</summary>
    /// <param name = "name">The HTML element local name to block.</param>
    /// <param name = "action">The action to apply when the rule matches.</param>
    public void BlockElement(string name, HtmlBlockedElementAction action)
    {
        this.BlockElementInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), name, action);
    }

    /// <summary>Blocks the element according to the selected action.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <param name = "action">The action to apply when the rule matches.</param>
    public void BlockElementInNamespace(string namespaceUri, string localName, HtmlBlockedElementAction action)
    {
        HtmlSanitizerBlockedElementRule rule = new HtmlSanitizerBlockedElementRule(namespaceUri, localName, action);
        HtmlSanitizerPolicyNames.ValidateAction(action, "action");
        RemoveElement(this.allowedElements, rule.GetNamespaceUri(), rule.GetLocalName());
        int index = FindBlocked(this.blockedElements, rule.GetNamespaceUri(), rule.GetLocalName());
        if (index < 0)
        {
            this.blockedElements.Add(rule);
        }
        else
        {
            this.blockedElements[index] = rule;
        }
    }

    /// <summary>Allows the attribute in the mutable policy.</summary>
    /// <param name = "name">The unnamespaced attribute local name to allow.</param>
    public void AllowAttribute(string name)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeMarkupName(name, "name");
        AddUnique(this.allowedAttributes, normalized);
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", "", normalized, HtmlSanitizerAttributeAction.Keep, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Allows the attribute in the mutable policy.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    public void AllowAttributeInNamespace(string namespaceUri, string localName)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", namespaceUri, localName, HtmlSanitizerAttributeAction.Keep, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Allows the attribute on element in the mutable policy.</summary>
    /// <param name = "attributeName">The attribute name.</param>
    /// <param name = "elementName">The element name.</param>
    public void AllowAttributeOnElement(string attributeName, string elementName)
    {
        string attribute = HtmlSanitizerPolicyNames.NormalizeMarkupName(attributeName, "attributeName");
        string element = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementName, "elementName");
        if (FindElementAttribute(this.elementAttributes, element, attribute) < 0)
        {
            this.elementAttributes.Add(new HtmlSanitizerElementAttributeRule(element, attribute));
        }

        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), element, "", attribute, HtmlSanitizerAttributeAction.Keep, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Allows the attribute on element in the mutable policy.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    public void AllowAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Keep, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Blocks the attribute according to the selected action.</summary>
    /// <param name = "name">The unnamespaced attribute local name to block.</param>
    public void BlockAttribute(string name)
    {
        this.BlockAttributeInNamespace("", name);
    }

    /// <summary>Blocks the attribute according to the selected action.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    public void BlockAttributeInNamespace(string namespaceUri, string localName)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", namespaceUri, localName, HtmlSanitizerAttributeAction.Drop, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Blocks the attribute on element according to the selected action.</summary>
    /// <param name = "attributeName">The attribute name.</param>
    /// <param name = "elementName">The element name.</param>
    public void BlockAttributeOnElement(string attributeName, string elementName)
    {
        this.BlockAttributeOnNamespacedElement("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName);
    }

    /// <summary>Blocks the attribute on element according to the selected action.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    public void BlockAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Drop, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Renames an unnamespaced attribute wherever that attribute is accepted.</summary>
    /// <param name = "attributeName">The source HTML attribute name.</param>
    /// <param name = "targetName">The unnamespaced local name assigned to retained values.</param>
    /// <remarks>Names are normalized using HTML name rules. A later rule for the same source attribute replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name is empty or invalid.</exception>
    public void RenameAttribute(string attributeName, string targetName)
    {
        this.RenameAttributeInNamespace("", attributeName, "", targetName, "");
    }

    /// <summary>Renames a namespaced attribute wherever that attribute is accepted.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "targetNamespaceUri">The namespace URI assigned after a rename.</param>
    /// <param name = "targetLocalName">The local name assigned after a rename.</param>
    /// <param name = "targetPrefix">The namespace prefix assigned after a rename.</param>
    /// <remarks>A later rule for the same source namespace and local name replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or namespace is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name, namespace, or prefix combination is invalid.</exception>
    public void RenameAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, string targetNamespaceUri, string targetLocalName, string targetPrefix)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Rename, targetNamespaceUri, targetLocalName, targetPrefix, HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Renames one unnamespaced attribute only on the named HTML element.</summary>
    /// <param name = "attributeName">The source HTML attribute name.</param>
    /// <param name = "elementName">The HTML element local name to which the rule applies.</param>
    /// <param name = "targetName">The unnamespaced local name assigned to retained values.</param>
    /// <remarks>Element-specific rules take precedence over global attribute rules. A later rule for the same element and attribute replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name is empty or invalid.</exception>
    public void RenameAttributeOnElement(string attributeName, string elementName, string targetName)
    {
        this.RenameAttributeOnNamespacedElement("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", targetName, "");
    }

    /// <summary>Renames one namespaced attribute only on the specified element.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "targetNamespaceUri">The namespace URI assigned after a rename.</param>
    /// <param name = "targetLocalName">The local name assigned after a rename.</param>
    /// <param name = "targetPrefix">The namespace prefix assigned after a rename.</param>
    /// <remarks>Element-specific rules take precedence over global attribute rules. A later rule for the same element and attribute replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or namespace is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name, namespace, or prefix combination is invalid.</exception>
    public void RenameAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, string targetNamespaceUri, string targetLocalName, string targetPrefix)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Rename, targetNamespaceUri, targetLocalName, targetPrefix, HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Normalizes an unnamespaced attribute wherever that attribute is accepted.</summary>
    /// <param name = "attributeName">The source HTML attribute local name.</param>
    /// <param name = "normalization">The normalization steps applied to accepted values.</param>
    /// <remarks>A later rule for the same source attribute replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when <c>attributeName</c> is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the attribute name or normalization value is invalid.</exception>
    public void NormalizeAttribute(string attributeName, HtmlSanitizerAttributeNormalization normalization)
    {
        this.NormalizeAttributeInNamespace("", attributeName, normalization);
    }

    /// <summary>Normalizes a namespaced attribute wherever that attribute is accepted.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "normalization">The normalization steps applied to accepted values.</param>
    /// <remarks>A later rule for the same source namespace and local name replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or namespace is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the attribute name, namespace, or normalization value is invalid.</exception>
    public void NormalizeAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, HtmlSanitizerAttributeNormalization normalization)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Normalize, "", "", "", normalization, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Normalizes one unnamespaced attribute only on the named HTML element.</summary>
    /// <param name = "attributeName">The source HTML attribute local name.</param>
    /// <param name = "elementName">The HTML element local name to which the rule applies.</param>
    /// <param name = "normalization">The normalization steps applied to accepted values.</param>
    /// <remarks>Element-specific rules take precedence over global attribute rules. A later rule for the same element and attribute replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name or normalization value is invalid.</exception>
    public void NormalizeAttributeOnElement(string attributeName, string elementName, HtmlSanitizerAttributeNormalization normalization)
    {
        this.NormalizeAttributeOnNamespacedElement("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, normalization);
    }

    /// <summary>Normalizes one namespaced attribute only on the specified element.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "normalization">The normalization steps applied to accepted values.</param>
    /// <remarks>Element-specific rules take precedence over global attribute rules. A later rule for the same element and attribute replaces this rule when the policy is compiled.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or namespace is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name, namespace, or normalization value is invalid.</exception>
    public void NormalizeAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, HtmlSanitizerAttributeNormalization normalization)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Normalize, "", "", "", normalization, HtmlSanitizerAttributeValidation.NonEmpty, ""));
    }

    /// <summary>Validates an unnamespaced attribute wherever it is accepted.</summary>
    /// <param name = "attributeName">The source attribute local name.</param>
    /// <param name = "validation">The built-in validation to apply.</param>
    public void ValidateAttribute(string attributeName, HtmlSanitizerAttributeValidation validation)
    {
        this.ValidateAttributeInNamespace("", attributeName, validation);
    }

    /// <summary>Validates a namespace-aware attribute wherever it is accepted.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "validation">The built-in validation to apply.</param>
    public void ValidateAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, HtmlSanitizerAttributeValidation validation)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Validate, "", "", "", HtmlSanitizerAttributeNormalization.Trim, validation, ""));
    }

    /// <summary>Validates an attribute only on one HTML element.</summary>
    /// <param name = "attributeName">The source attribute local name.</param>
    /// <param name = "elementName">The HTML element local name.</param>
    /// <param name = "validation">The built-in validation to apply.</param>
    public void ValidateAttributeOnElement(string attributeName, string elementName, HtmlSanitizerAttributeValidation validation)
    {
        this.ValidateAttributeOnNamespacedElement("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, validation);
    }

    /// <summary>Validates a namespace-aware attribute only on one element.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "validation">The built-in validation to apply.</param>
    public void ValidateAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, HtmlSanitizerAttributeValidation validation)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Validate, "", "", "", HtmlSanitizerAttributeNormalization.Trim, validation, ""));
    }

    /// <summary>Ensures a prefix on an unnamespaced attribute wherever it is accepted.</summary>
    /// <param name = "attributeName">The source attribute local name.</param>
    /// <param name = "prefix">The non-empty value prefix.</param>
    public void PrefixAttribute(string attributeName, string prefix)
    {
        this.PrefixAttributeInNamespace("", attributeName, prefix);
    }

    /// <summary>Ensures a prefix on a namespace-aware attribute wherever it is accepted.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "prefix">The non-empty value prefix.</param>
    public void PrefixAttributeInNamespace(string attributeNamespaceUri, string attributeLocalName, string prefix)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(false, "", "", attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Prefix, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, prefix));
    }

    /// <summary>Ensures a prefix on an attribute only on one HTML element.</summary>
    /// <param name = "attributeName">The source attribute local name.</param>
    /// <param name = "elementName">The HTML element local name.</param>
    /// <param name = "prefix">The non-empty value prefix.</param>
    public void PrefixAttributeOnElement(string attributeName, string elementName, string prefix)
    {
        this.PrefixAttributeOnNamespacedElement("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, prefix);
    }

    /// <summary>Ensures a prefix on a namespace-aware attribute only on one element.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "prefix">The non-empty value prefix.</param>
    public void PrefixAttributeOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, string prefix)
    {
        this.SetAttributeRule(new HtmlSanitizerAttributeRule(true, elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, HtmlSanitizerAttributeAction.Prefix, "", "", "", HtmlSanitizerAttributeNormalization.Trim, HtmlSanitizerAttributeValidation.NonEmpty, prefix));
    }

    /// <summary>Forces an unnamespaced attribute to a fixed value on the named HTML element.</summary>
    /// <param name = "elementName">The HTML element local name to which the rule applies.</param>
    /// <param name = "attributeName">The unnamespaced attribute local name.</param>
    /// <param name = "value">The exact value written to every matching retained element.</param>
    /// <remarks>The attribute is added when absent and overwritten when present. Adding another rule for the same element and attribute replaces the earlier value.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or value is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name is empty or invalid.</exception>
    public void ForceAttribute(string elementName, string attributeName, string value)
    {
        string element = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementName, "elementName");
        string attribute = HtmlSanitizerPolicyNames.NormalizeMarkupName(attributeName, "attributeName");
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        HtmlSanitizerForcedAttributeRule rule = new HtmlSanitizerForcedAttributeRule(element, attribute, value);
        int index = FindForcedAttribute(this.forcedAttributes, element, attribute);
        if (index < 0)
        {
            this.forcedAttributes.Add(rule);
        }
        else
        {
            this.forcedAttributes[index] = rule;
        }

        this.SetAttributeValueRule(new HtmlSanitizerAttributeValueRule(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), element, "", attribute, "", value, true));
    }

    /// <summary>Forces a namespaced attribute to a fixed value on the specified element.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "attributePrefix">The attribute namespace prefix.</param>
    /// <param name = "value">The exact value written to every matching retained element.</param>
    /// <remarks>The attribute is added when absent and overwritten when present. Adding another rule for the same element and attribute replaces the earlier value.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name, namespace, or value is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name, namespace, or prefix combination is invalid.</exception>
    public void ForceAttributeInNamespace(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName, string attributePrefix, string value)
    {
        this.SetAttributeValueRule(new HtmlSanitizerAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, attributePrefix, value, true));
    }

    /// <summary>Ensures a named HTML element has an unnamespaced attribute, adding a default value when absent.</summary>
    /// <param name = "elementName">The HTML element local name to which the rule applies.</param>
    /// <param name = "attributeName">The unnamespaced attribute local name.</param>
    /// <param name = "value">The value added when a retained matching element has no accepted value for the attribute.</param>
    /// <remarks>An existing accepted attribute value is preserved. Use <see cref = "ForceAttribute(string, string, string)"/> when every matching element must receive the configured value.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or value is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name is empty or invalid.</exception>
    public void RequireAttribute(string elementName, string attributeName, string value)
    {
        this.RequireAttributeInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", attributeName, "", value);
    }

    /// <summary>Ensures the specified element has a namespaced attribute, adding a default value when absent.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "attributePrefix">The attribute namespace prefix.</param>
    /// <param name = "value">The value added when a retained matching element has no accepted value for the attribute.</param>
    /// <remarks>An existing accepted attribute value is preserved. Use a forced-attribute rule when every matching element must receive the configured value.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name, namespace, or value is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name, namespace, or prefix combination is invalid.</exception>
    public void RequireAttributeInNamespace(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName, string attributePrefix, string value)
    {
        HtmlSanitizerAttributeValueRule rule = new HtmlSanitizerAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, attributePrefix, value, false);
        if (rule.GetElementNamespaceUri() == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() && rule.GetAttributeNamespaceUri().Length == 0)
        {
            int legacyIndex = FindForcedAttribute(this.forcedAttributes, rule.GetElementLocalName(), rule.GetAttributeLocalName());
            if (legacyIndex >= 0)
            {
                this.forcedAttributes.RemoveAt(legacyIndex);
            }
        }

        this.SetAttributeValueRule(rule);
    }

    /// <summary>Forces an unnamespaced attribute to contain a fixed set of space-separated tokens.</summary>
    /// <param name = "elementName">The HTML element local name to which the rule applies.</param>
    /// <param name = "attributeName">The unnamespaced attribute local name.</param>
    /// <param name = "tokens">The non-empty, whitespace-free, ASCII-case-insensitively unique tokens to write in array order.</param>
    /// <remarks>The attribute is added when absent and overwritten when present. The input array is copied while the rule is created.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or the token array is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name or token is invalid, the array is empty, or tokens repeat ignoring ASCII case.</exception>
    public void ForceAttributeTokens(string elementName, string attributeName, string[] tokens)
    {
        this.ForceAttributeTokensInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", attributeName, "", tokens);
    }

    /// <summary>Forces a namespaced attribute to contain a fixed set of space-separated tokens.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "attributePrefix">The attribute namespace prefix.</param>
    /// <param name = "tokens">The non-empty, whitespace-free, ASCII-case-insensitively unique tokens to write in array order.</param>
    /// <remarks>The attribute is added when absent and overwritten when present. The input array is copied while the rule is created.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name, namespace, or the token array is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name, namespace, prefix, or token is invalid, the array is empty, or tokens repeat ignoring ASCII case.</exception>
    public void ForceAttributeTokensInNamespace(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName, string attributePrefix, string[] tokens)
    {
        this.SetAttributeTokenRule(new HtmlSanitizerAttributeTokenRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, attributePrefix, tokens));
    }

    /// <summary>Allows the URL scheme in the mutable policy.</summary>
    /// <param name = "scheme">The URL scheme to test or apply.</param>
    public void AllowUrlScheme(string scheme)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeScheme(scheme, "scheme");
        AddUnique(this.urlSchemes, normalized);
    }

    /// <summary>Allows the URL host in the mutable policy.</summary>
    /// <param name = "host">The normalized host name to test.</param>
    public void AllowUrlHost(string host)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeHost(host, "host");
        AddUnique(this.urlHosts, normalized);
    }

    /// <summary>Allows the URL port in the mutable policy.</summary>
    /// <param name = "port">The numeric network port to test.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void AllowUrlPort(int port)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentOutOfRangeException("port", port, "A URL port must be between 1 and 65535.");
        }

        if (!Contains(this.urlPorts, port))
        {
            this.urlPorts.Add(port);
        }
    }

    /// <summary>Controls whether accepted absolute URLs may contain a user name or password.</summary>
    /// <param name = "value"><c>true</c> to retain URL credentials; <c>false</c> to reject URLs that contain them.</param>
    public void SetAllowUrlCredentials(bool value)
    {
        this.allowUrlCredentials = value;
    }

    /// <summary>Controls whether relative URL references may survive sanitization.</summary>
    /// <param name = "value"><c>true</c> to accept relative references; <c>false</c> to reject them.</param>
    public void SetAllowRelativeUrls(bool value)
    {
        this.allowRelativeUrls = value;
    }

    /// <summary>Controls whether network-path references beginning with <c>//</c> may survive sanitization.</summary>
    /// <param name = "value"><c>true</c> to accept protocol-relative references; <c>false</c> to reject them.</param>
    public void SetAllowProtocolRelativeUrls(bool value)
    {
        this.allowProtocolRelativeUrls = value;
    }

    /// <summary>Controls whether same-document references beginning with <c>#</c> may survive sanitization.</summary>
    /// <param name = "value"><c>true</c> to accept fragment-only references; <c>false</c> to reject them.</param>
    public void SetAllowFragmentUrls(bool value)
    {
        this.allowFragmentUrls = value;
    }

    /// <summary>Sets the trusted absolute base used to resolve and optionally rewrite relative URLs.</summary>
    /// <param name = "value">A well-formed absolute base URL, or an empty string to remove the base.</param>
    /// <exception cref = "ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the non-empty value is not a supported absolute URL.</exception>
    public void SetUrlBase(string value)
    {
        HtmlSanitizerUrlPolicy policy = new HtmlSanitizerUrlPolicy(this.urlSchemes.ToArray(), this.urlHosts.ToArray(), this.urlPorts.ToArray(), this.allowUrlCredentials, this.allowRelativeUrls, this.allowProtocolRelativeUrls, this.allowFragmentUrls, value, this.rewriteRelativeUrls);
        this.urlBase = policy.GetBaseUrl();
    }

    /// <summary>Controls whether accepted relative references are replaced by resolved absolute URLs.</summary>
    /// <param name = "value"><c>true</c> to resolve and rewrite relative references; <c>false</c> to retain their relative spelling.</param>
    /// <exception cref = "InvalidOperationException">Thrown when rewriting is enabled without an absolute URL base.</exception>
    public void SetRewriteRelativeUrls(bool value)
    {
        if (value && this.urlBase.Length == 0)
        {
            throw new InvalidOperationException("Relative URL rewriting requires an absolute base URL.");
        }

        this.rewriteRelativeUrls = value;
    }

    /// <summary>Replaces every global URL and <c>data</c>-URL setting with a copy of another URL policy.</summary>
    /// <param name = "value">The policy whose schemes, hosts, ports, flags, base, media types, and size limit are copied.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetUrlPolicy(HtmlSanitizerUrlPolicy value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.urlSchemes.Clear();
        this.urlHosts.Clear();
        this.urlPorts.Clear();
        AddStrings(this.urlSchemes, value.GetAllowedSchemes());
        AddStrings(this.urlHosts, value.GetAllowedHosts());
        AddInts(this.urlPorts, value.GetAllowedPorts());
        this.allowUrlCredentials = value.GetAllowCredentials();
        this.allowRelativeUrls = value.GetAllowRelativeUrls();
        this.allowProtocolRelativeUrls = value.GetAllowProtocolRelativeUrls();
        this.allowFragmentUrls = value.GetAllowFragmentUrls();
        this.urlBase = value.GetBaseUrl();
        this.rewriteRelativeUrls = value.GetRewriteRelativeUrls();
        this.dataUrlMediaTypes.Clear();
        AddStrings(this.dataUrlMediaTypes, value.GetAllowedDataMediaTypes());
        this.maximumDataUrlBytes = value.GetMaximumDataUrlBytes();
    }

    /// <summary>Adds a media type to the allowlist used for decoded <c>data</c> URLs.</summary>
    /// <param name = "value">The type/subtype media type; parameters are not accepted and ASCII letters are normalized to lowercase.</param>
    /// <exception cref = "ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the media type is empty, malformed, or contains parameters.</exception>
    public void AllowDataUrlMediaType(string value)
    {
        string normalized = HtmlSanitizerUrlPolicy.NormalizeMediaType(value, "value");
        AddUnique(this.dataUrlMediaTypes, normalized);
    }

    /// <summary>Sets the largest decoded payload accepted from a <c>data</c> URL.</summary>
    /// <param name = "value">The maximum decoded byte count; <c>0</c> disables non-empty data payloads.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetMaximumDataUrlBytes(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "The maximum decoded data URL size cannot be negative.");
        }

        this.maximumDataUrlBytes = value;
    }

    /// <summary>Assigns a URL policy to one unnamespaced attribute on one HTML element.</summary>
    /// <param name = "attributeName">The unnamespaced URL-bearing attribute local name.</param>
    /// <param name = "elementName">The HTML element local name to which the rule applies.</param>
    /// <param name = "value">The URL policy copied into the element-specific rule.</param>
    /// <remarks>Element-specific URL rules override the global URL policy for the same element and attribute. Adding another rule for the same pair replaces the earlier rule.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name or policy is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name is empty or invalid.</exception>
    public void SetUrlPolicyOnElement(string attributeName, string elementName, HtmlSanitizerUrlPolicy value)
    {
        this.SetUrlPolicyOnNamespacedElement("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, value);
    }

    /// <summary>Assigns a URL policy to one namespace-aware attribute on one specified element.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "value">The URL policy copied into the element-specific rule.</param>
    /// <remarks>Element-specific URL rules override the global URL policy for the same element and attribute. Adding another rule for the same pair replaces the earlier rule.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required name, namespace, or policy is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when a name or namespace is invalid.</exception>
    public void SetUrlPolicyOnNamespacedElement(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName, HtmlSanitizerUrlPolicy value)
    {
        HtmlSanitizerUrlAttributeRule rule = new HtmlSanitizerUrlAttributeRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, value);
        int index = 0;
        while (index < this.urlAttributeRules.Count)
        {
            HtmlSanitizerUrlAttributeRule current = this.urlAttributeRules[index];
            if (current.GetElementNamespaceUri() == rule.GetElementNamespaceUri() && current.GetElementLocalName() == rule.GetElementLocalName() && current.GetAttributeNamespaceUri() == rule.GetAttributeNamespaceUri() && current.GetAttributeLocalName() == rule.GetAttributeLocalName())
            {
                this.urlAttributeRules[index] = rule;
                return;
            }

            index = index + 1;
        }

        this.urlAttributeRules.Add(rule);
    }

    /// <summary>Allows the CSS property in the mutable policy.</summary>
    /// <param name = "propertyName">The CSS property name.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public void AllowCssProperty(string propertyName)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(propertyName, "propertyName");
        if (CssSafeData.GetPropertyProfile(normalized) == CssSafePropertyProfile.Unknown)
        {
            throw new ArgumentException("The CSS property is outside the conservative inline style allowlist.", "propertyName");
        }

        AddUnique(this.cssProperties, normalized);
    }

    /// <summary>Blocks one otherwise conservative CSS function.</summary>
    /// <param name = "functionName">The known safe function name to block.</param>
    public void BlockCssFunction(string functionName)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(functionName, "functionName");
        if (CssSafeData.GetFunctionProfile(normalized) == CssSafeFunctionProfile.Unknown)
        {
            throw new ArgumentException("The CSS function is not in the conservative function set.", "functionName");
        }

        AddUnique(this.blockedCssFunctions, normalized);
    }

    /// <summary>Blocks one otherwise conservative CSS keyword.</summary>
    /// <param name = "keyword">The known safe keyword to block.</param>
    public void BlockCssKeyword(string keyword)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(keyword, "keyword");
        if (!CssSafeData.IsKeyword(normalized))
        {
            throw new ArgumentException("The CSS keyword is not in the conservative keyword set.", "keyword");
        }

        AddUnique(this.blockedCssKeywords, normalized);
    }

    /// <summary>Blocks one otherwise conservative CSS unit.</summary>
    /// <param name = "unit">The known safe unit to block.</param>
    public void BlockCssUnit(string unit)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(unit, "unit");
        if (CssSafeData.GetUnitKind(normalized) == CssSafeUnitKind.Unknown)
        {
            throw new ArgumentException("The CSS unit is not in the conservative unit set.", "unit");
        }

        AddUnique(this.blockedCssUnits, normalized);
    }

    /// <summary>Controls whether custom-property declarations and <c>var()</c> references are accepted.</summary>
    /// <param name = "value"><c>true</c> to accept conservatively parsed custom properties; otherwise, <c>false</c>.</param>
    public void SetAllowCssCustomProperties(bool value)
    {
        this.allowCssCustomProperties = value;
    }

    /// <summary>Controls whether otherwise-unlisted unnamespaced <c>data-*</c> attributes are retained.</summary>
    /// <param name = "value"><c>true</c> to allow valid custom data attributes; <c>false</c> to require an explicit attribute rule.</param>
    public void SetAllowDataAttributes(bool value)
    {
        this.allowDataAttributes = value;
    }

    /// <summary>Controls whether otherwise-unlisted unnamespaced <c>aria-*</c> attributes are retained.</summary>
    /// <param name = "value"><c>true</c> to allow valid ARIA attributes; <c>false</c> to require an explicit attribute rule.</param>
    public void SetAllowAriaAttributes(bool value)
    {
        this.allowAriaAttributes = value;
    }

    /// <summary>Controls whether valid autonomous custom-element names are eligible for retention.</summary>
    /// <param name = "value"><c>true</c> to permit allowed custom elements; <c>false</c> to treat them as blocked elements.</param>
    public void SetAllowCustomElements(bool value)
    {
        this.allowCustomElements = value;
    }

    /// <summary>Controls whether the <c>is</c> attribute may select an allowed customized built-in element.</summary>
    /// <param name = "value"><c>true</c> to permit allowed customized built-ins; <c>false</c> to drop the customization.</param>
    public void SetAllowCustomizedBuiltInElements(bool value)
    {
        this.allowCustomizedBuiltInElements = value;
    }

    /// <summary>Controls whether comment nodes are retained in sanitized output.</summary>
    /// <param name = "value"><c>true</c> to retain comments after safe serialization checks; <c>false</c> to remove them.</param>
    public void SetAllowComments(bool value)
    {
        this.allowComments = value;
    }

    /// <summary>Controls whether a document type node is retained in document output.</summary>
    /// <param name = "value"><c>true</c> to retain an allowed document type; <c>false</c> to remove it.</param>
    public void SetAllowDoctype(bool value)
    {
        this.allowDoctype = value;
    }

    /// <summary>Controls whether HTML <c>template</c> elements and their inert content are retained.</summary>
    /// <param name = "value"><c>true</c> to sanitize and retain templates; <c>false</c> to apply the blocked-element action.</param>
    public void SetAllowTemplateElements(bool value)
    {
        this.allowTemplateElements = value;
    }

    /// <summary>Controls whether recognized server-template markers are neutralized.</summary>
    /// <param name = "value"><c>true</c> to rewrite recognized template delimiters in retained text and attributes; <c>false</c> to leave them unchanged.</param>
    /// <remarks>This defense is enabled by default. Disable it only when sanitized output will never be interpreted by a server-side template engine.</remarks>
    public void SetNeutralizeServerTemplateMarkers(bool value)
    {
        this.neutralizeServerTemplateMarkers = value;
    }

    /// <summary>Allows the foreign namespace in the mutable policy.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    public void AllowForeignNamespace(string namespaceUri)
    {
        string value = HtmlSanitizerMarkupConfiguration.ValidateForeignNamespace(namespaceUri, "namespaceUri");
        AddUnique(this.foreignNamespaces, value);
    }

    /// <summary>Blocks the foreign namespace according to the selected action.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    public void BlockForeignNamespace(string namespaceUri)
    {
        string value = HtmlSanitizerMarkupConfiguration.ValidateForeignNamespace(namespaceUri, "namespaceUri");
        int index = 0;
        while (index < this.foreignNamespaces.Count)
        {
            if (this.foreignNamespaces[index] == value)
            {
                this.foreignNamespaces.RemoveAt(index);
                return;
            }

            index = index + 1;
        }
    }

    /// <summary>Sets the prefix used when rewriting dangerous or duplicate <c>id</c> and <c>name</c> values.</summary>
    /// <param name = "value">A non-empty prefix containing only safe ASCII name characters.</param>
    /// <exception cref = "ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the prefix is empty or contains an unsafe character.</exception>
    public void SetDomClobberPrefix(string value)
    {
        HtmlSanitizerPolicyNames.ValidateClobberPrefix(value, "value");
        this.domClobberPrefix = value;
    }

    /// <summary>Validates the current settings and returns an immutable configuration for repeated use.</summary>
    /// <returns>A validated compiled configuration that can be reused safely.</returns>
    public HtmlCompiledSanitizerPolicy Compile()
    {
        this.Validate();
        HtmlSanitizerMarkupConfiguration markupConfiguration = new HtmlSanitizerMarkupConfiguration(this.allowCustomElements, this.allowCustomizedBuiltInElements, this.allowComments, this.allowDoctype, this.allowTemplateElements, this.neutralizeServerTemplateMarkers, this.foreignNamespaces.ToArray());
        HtmlSanitizerUrlPolicy urlPolicy = HtmlSanitizerUrlPolicy.CreateWithDataUrls(this.urlSchemes.ToArray(), this.urlHosts.ToArray(), this.urlPorts.ToArray(), this.allowUrlCredentials, this.allowRelativeUrls, this.allowProtocolRelativeUrls, this.allowFragmentUrls, this.urlBase, this.rewriteRelativeUrls, this.dataUrlMediaTypes.ToArray(), this.maximumDataUrlBytes);
        return new HtmlCompiledSanitizerPolicy(this.allowedElements.ToArray(), this.blockedElements.ToArray(), this.allowedAttributes.ToArray(), this.elementAttributes.ToArray(), this.forcedAttributes.ToArray(), this.urlSchemes.ToArray(), this.urlHosts.ToArray(), this.cssProperties.ToArray(), this.allowDataAttributes, this.allowAriaAttributes, this.domClobberPrefix, this.attributeRules.ToArray(), this.attributeValueRules.ToArray(), markupConfiguration, urlPolicy, this.urlAttributeRules.ToArray(), this.attributeTokenRules.ToArray(), this.blockedCssFunctions.ToArray(), this.blockedCssKeywords.ToArray(), this.blockedCssUnits.ToArray(), this.allowCssCustomProperties);
    }

    internal void SetAttributeRule(HtmlSanitizerAttributeRule value)
    {
        int index = 0;
        while (index < this.attributeRules.Count)
        {
            HtmlSanitizerAttributeRule current = this.attributeRules[index];
            if (SameAttributeRuleIdentity(current, value))
            {
                this.attributeRules[index] = value;
                return;
            }

            index = index + 1;
        }

        this.attributeRules.Add(value);
    }

    internal void SetAttributeValueRule(HtmlSanitizerAttributeValueRule value)
    {
        int index = 0;
        while (index < this.attributeValueRules.Count)
        {
            HtmlSanitizerAttributeValueRule current = this.attributeValueRules[index];
            if (current.GetElementNamespaceUri() == value.GetElementNamespaceUri() && current.GetElementLocalName() == value.GetElementLocalName() && current.GetAttributeNamespaceUri() == value.GetAttributeNamespaceUri() && current.GetAttributeLocalName() == value.GetAttributeLocalName())
            {
                this.attributeValueRules[index] = value;
                return;
            }

            index = index + 1;
        }

        this.attributeValueRules.Add(value);
    }

    internal void SetAttributeTokenRule(HtmlSanitizerAttributeTokenRule value)
    {
        int index = 0;
        while (index < this.attributeTokenRules.Count)
        {
            HtmlSanitizerAttributeTokenRule current = this.attributeTokenRules[index];
            if (current.GetElementNamespaceUri() == value.GetElementNamespaceUri() && current.GetElementLocalName() == value.GetElementLocalName() && current.GetAttributeNamespaceUri() == value.GetAttributeNamespaceUri() && current.GetAttributeLocalName() == value.GetAttributeLocalName())
            {
                this.attributeTokenRules[index] = value;
                return;
            }

            index = index + 1;
        }

        this.attributeTokenRules.Add(value);
    }

    internal static bool SameAttributeRuleIdentity(HtmlSanitizerAttributeRule left, HtmlSanitizerAttributeRule right)
    {
        return left.IsElementSpecific() == right.IsElementSpecific() && left.GetElementNamespaceUri() == right.GetElementNamespaceUri() && left.GetElementLocalName() == right.GetElementLocalName() && left.GetAttributeNamespaceUri() == right.GetAttributeNamespaceUri() && left.GetAttributeLocalName() == right.GetAttributeLocalName();
    }

    internal static void AddStrings(List<string> target, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            target.Add(values[index]);
            index = index + 1;
        }
    }

    internal static void AddInts(List<int> target, int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            target.Add(values[index]);
            index = index + 1;
        }
    }

    internal static void AddUnique(List<string> values, string value)
    {
        if (!Contains(values, value))
        {
            values.Add(value);
        }
    }

    internal static bool Contains(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool Contains(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AddUniqueElement(List<HtmlSanitizerElementName> values, HtmlSanitizerElementName value)
    {
        int index = 0;
        while (index < values.Count)
        {
            HtmlSanitizerElementName current = values[index];
            if (current.GetNamespaceUri() == value.GetNamespaceUri() && current.GetLocalName() == value.GetLocalName())
            {
                return;
            }

            index = index + 1;
        }

        values.Add(value);
    }

    internal static bool ContainsElement(List<HtmlSanitizerElementName> values, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < values.Count)
        {
            HtmlSanitizerElementName current = values[index];
            if (current.GetNamespaceUri() == namespaceUri && current.GetLocalName() == localName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void RemoveElement(List<HtmlSanitizerElementName> values, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < values.Count)
        {
            HtmlSanitizerElementName current = values[index];
            if (current.GetNamespaceUri() == namespaceUri && current.GetLocalName() == localName)
            {
                values.RemoveAt(index);
                return;
            }

            index = index + 1;
        }
    }

    internal static int FindBlocked(List<HtmlSanitizerBlockedElementRule> values, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].GetNamespaceUri() == namespaceUri && values[index].GetLocalName() == localName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void RemoveBlocked(List<HtmlSanitizerBlockedElementRule> values, string namespaceUri, string localName)
    {
        int index = FindBlocked(values, namespaceUri, localName);
        if (index >= 0)
        {
            values.RemoveAt(index);
        }
    }

    internal static int FindElementAttribute(List<HtmlSanitizerElementAttributeRule> values, string elementName, string attributeName)
    {
        int index = 0;
        while (index < values.Count)
        {
            HtmlSanitizerElementAttributeRule rule = values[index];
            if (rule.GetElementName() == elementName && rule.GetAttributeName() == attributeName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindForcedAttribute(List<HtmlSanitizerForcedAttributeRule> values, string elementName, string attributeName)
    {
        int index = 0;
        while (index < values.Count)
        {
            HtmlSanitizerForcedAttributeRule rule = values[index];
            if (rule.GetElementName() == elementName && rule.GetAttributeName() == attributeName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
