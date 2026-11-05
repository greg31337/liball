using System;

namespace html;
/// <summary>Contains validated sanitizer rules ready for repeated use.</summary>
public class HtmlCompiledSanitizerPolicy : IHtmlCompiledSanitizerPolicy
{
    internal HtmlSanitizerElementName[] allowedElements = Array.Empty<HtmlSanitizerElementName>();
    internal HtmlSanitizerBlockedElementRule[] blockedElements = Array.Empty<HtmlSanitizerBlockedElementRule>();
    internal string[] allowedAttributes = Array.Empty<string>();
    internal HtmlSanitizerElementAttributeRule[] elementAttributes = Array.Empty<HtmlSanitizerElementAttributeRule>();
    internal HtmlSanitizerForcedAttributeRule[] forcedAttributes = Array.Empty<HtmlSanitizerForcedAttributeRule>();
    internal HtmlSanitizerAttributeRule[] sanitizerAttributeRules = Array.Empty<HtmlSanitizerAttributeRule>();
    internal HtmlSanitizerAttributeValueRule[] sanitizerAttributeValueRules = Array.Empty<HtmlSanitizerAttributeValueRule>();
    internal HtmlSanitizerAttributeTokenRule[] sanitizerAttributeTokenRules = Array.Empty<HtmlSanitizerAttributeTokenRule>();
    internal string[] urlSchemes = Array.Empty<string>();
    internal string[] urlHosts = Array.Empty<string>();
    internal string[] cssProperties = Array.Empty<string>();
    internal string[] blockedCssFunctions = Array.Empty<string>();
    internal string[] blockedCssKeywords = Array.Empty<string>();
    internal string[] blockedCssUnits = Array.Empty<string>();
    internal bool allowCssCustomProperties;
    internal bool allowDataAttributes;
    internal bool allowAriaAttributes;
    internal string domClobberPrefix = "";
    internal HtmlSanitizerMarkupConfiguration markupConfiguration = CreateDefaultMarkupConfiguration();
    internal HtmlSanitizerUrlPolicy urlPolicy = CreateDefaultUrlPolicy(Array.Empty<string>(), Array.Empty<string>());
    internal HtmlSanitizerUrlAttributeRule[] urlAttributeRules = Array.Empty<HtmlSanitizerUrlAttributeRule>();
    internal HtmlCompiledSanitizerPolicy(HtmlSanitizerElementName[] allowedElementsValue, HtmlSanitizerBlockedElementRule[] blockedElementsValue, string[] allowedAttributesValue, HtmlSanitizerElementAttributeRule[] elementAttributesValue, HtmlSanitizerForcedAttributeRule[] forcedAttributesValue, string[] urlSchemesValue, string[] urlHostsValue, string[] cssPropertiesValue, bool allowDataAttributesValue, bool allowAriaAttributesValue, string domClobberPrefixValue, HtmlSanitizerAttributeRule[] sanitizerAttributeRulesValue, HtmlSanitizerAttributeValueRule[] sanitizerAttributeValueRulesValue, HtmlSanitizerMarkupConfiguration markupConfigurationValue, HtmlSanitizerUrlPolicy urlPolicyValue, HtmlSanitizerUrlAttributeRule[] urlAttributeRulesValue, HtmlSanitizerAttributeTokenRule[] sanitizerAttributeTokenRulesValue, string[] blockedCssFunctionsValue, string[] blockedCssKeywordsValue, string[] blockedCssUnitsValue, bool allowCssCustomPropertiesValue)
    {
        this.Initialize(allowedElementsValue, blockedElementsValue, allowedAttributesValue, elementAttributesValue, forcedAttributesValue, urlSchemesValue, urlHostsValue, cssPropertiesValue, allowDataAttributesValue, allowAriaAttributesValue, domClobberPrefixValue, sanitizerAttributeRulesValue, sanitizerAttributeValueRulesValue, markupConfigurationValue, urlPolicyValue, urlAttributeRulesValue, sanitizerAttributeTokenRulesValue, blockedCssFunctionsValue, blockedCssKeywordsValue, blockedCssUnitsValue, allowCssCustomPropertiesValue);
    }

    internal void Initialize(HtmlSanitizerElementName[] allowedElementsValue, HtmlSanitizerBlockedElementRule[] blockedElementsValue, string[] allowedAttributesValue, HtmlSanitizerElementAttributeRule[] elementAttributesValue, HtmlSanitizerForcedAttributeRule[] forcedAttributesValue, string[] urlSchemesValue, string[] urlHostsValue, string[] cssPropertiesValue, bool allowDataAttributesValue, bool allowAriaAttributesValue, string domClobberPrefixValue, HtmlSanitizerAttributeRule[] sanitizerAttributeRulesValue, HtmlSanitizerAttributeValueRule[] sanitizerAttributeValueRulesValue, HtmlSanitizerMarkupConfiguration markupConfigurationValue, HtmlSanitizerUrlPolicy urlPolicyValue, HtmlSanitizerUrlAttributeRule[] urlAttributeRulesValue, HtmlSanitizerAttributeTokenRule[] sanitizerAttributeTokenRulesValue, string[] blockedCssFunctionsValue, string[] blockedCssKeywordsValue, string[] blockedCssUnitsValue, bool allowCssCustomPropertiesValue)
    {
        this.allowedElements = CopyAndSort(allowedElementsValue);
        this.blockedElements = CopyAndSort(blockedElementsValue);
        this.allowedAttributes = CopyAndSortMarkupNames(allowedAttributesValue);
        this.elementAttributes = CopyAndSort(elementAttributesValue);
        this.forcedAttributes = CopyAndSort(forcedAttributesValue);
        this.sanitizerAttributeRules = CopyAndSort(sanitizerAttributeRulesValue);
        this.sanitizerAttributeValueRules = CopyAndSort(sanitizerAttributeValueRulesValue);
        this.sanitizerAttributeTokenRules = CopyAndSort(sanitizerAttributeTokenRulesValue);
        this.urlSchemes = CopyAndSortSchemes(urlSchemesValue);
        this.urlHosts = CopyAndSortHosts(urlHostsValue);
        this.cssProperties = CopyAndSortCssProperties(cssPropertiesValue);
        this.blockedCssFunctions = CopyAndSortCssIdentifiers(blockedCssFunctionsValue);
        this.blockedCssKeywords = CopyAndSortCssIdentifiers(blockedCssKeywordsValue);
        this.blockedCssUnits = CopyAndSortCssIdentifiers(blockedCssUnitsValue);
        this.allowCssCustomProperties = allowCssCustomPropertiesValue;
        this.allowDataAttributes = allowDataAttributesValue;
        this.allowAriaAttributes = allowAriaAttributesValue;
        HtmlSanitizerPolicyNames.ValidateClobberPrefix(domClobberPrefixValue, "domClobberPrefixValue");
        this.domClobberPrefix = domClobberPrefixValue;
        if (markupConfigurationValue == null)
        {
            throw new ArgumentNullException("markupConfigurationValue");
        }

        this.markupConfiguration = markupConfigurationValue.Copy();
        if (urlPolicyValue == null)
        {
            throw new ArgumentNullException("urlPolicyValue");
        }

        this.urlPolicy = urlPolicyValue.Copy();
        if (!SameStrings(this.urlSchemes, this.urlPolicy.GetAllowedSchemes()) || !SameStrings(this.urlHosts, this.urlPolicy.GetAllowedHosts()))
        {
            throw new ArgumentException("The compiled URL policy must match its scheme and host tables.");
        }

        this.urlAttributeRules = CopyUrlAttributeRules(urlAttributeRulesValue);
        this.ValidateTables();
    }

    /// <summary>Creates a mutable policy initialized from this compiled policy.</summary>
    /// <returns>An independently mutable policy containing the same allowlists, blocklists, and value rules.</returns>
    public HtmlSanitizerPolicy CreatePolicy()
    {
        return HtmlSanitizerPolicy.Create(this);
    }

    /// <summary>Reports whether the supplied element is permitted by the policy.</summary>
    /// <param name = "name">The HTML element local name to test.</param>
    /// <returns><c>true</c> when the element is permitted by policy; otherwise, <c>false</c>.</returns>
    public bool IsElementAllowed(string name)
    {
        return this.IsElementAllowedInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), name);
    }

    /// <summary>Reports whether the supplied element is permitted by the policy.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns><c>true</c> when the element is permitted by policy; otherwise, <c>false</c>.</returns>
    public bool IsElementAllowedInNamespace(string namespaceUri, string localName)
    {
        HtmlSanitizerElementName element = new HtmlSanitizerElementName(namespaceUri, localName);
        string elementNamespace = element.GetNamespaceUri();
        string elementLocalName = element.GetLocalName();
        if (FindElement(this.allowedElements, elementNamespace, elementLocalName) >= 0)
        {
            return true;
        }

        if (this.FindBlocked(elementNamespace, elementLocalName) >= 0)
        {
            return false;
        }

        if (elementNamespace == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            return this.markupConfiguration.GetAllowTemplateElements() && elementLocalName == "template" || this.markupConfiguration.GetAllowCustomElements() && HtmlSanitizerMarkupNames.IsCustomElementName(elementLocalName);
        }

        return elementNamespace.Length > 0 && this.markupConfiguration.IsForeignNamespaceAllowed(elementNamespace);
    }

    /// <summary>Reports whether the supplied element has an explicit blocking rule.</summary>
    /// <param name = "name">The HTML element local name to test.</param>
    /// <returns><c>true</c> when the element has an explicit blocking rule; otherwise, <c>false</c>.</returns>
    public bool IsElementBlocked(string name)
    {
        return this.IsElementBlockedInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), name);
    }

    /// <summary>Reports whether the supplied element has an explicit blocking rule.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns><c>true</c> when the element has an explicit blocking rule; otherwise, <c>false</c>.</returns>
    public bool IsElementBlockedInNamespace(string namespaceUri, string localName)
    {
        HtmlSanitizerElementName element = new HtmlSanitizerElementName(namespaceUri, localName);
        return this.FindBlocked(element.GetNamespaceUri(), element.GetLocalName()) >= 0;
    }

    /// <summary>Gets how an explicitly blocked HTML element is removed or replaced.</summary>
    /// <param name = "name">The HTML element local name whose blocking action is requested.</param>
    /// <returns>The action stored by the matching blocking rule.</returns>
    public HtmlBlockedElementAction GetBlockedElementAction(string name)
    {
        return this.GetBlockedElementActionInNamespace(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), name);
    }

    /// <summary>Gets how an explicitly blocked namespace-aware element is removed or replaced.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns>The action stored by the matching blocking rule.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public HtmlBlockedElementAction GetBlockedElementActionInNamespace(string namespaceUri, string localName)
    {
        HtmlSanitizerElementName element = new HtmlSanitizerElementName(namespaceUri, localName);
        int index = this.FindBlocked(element.GetNamespaceUri(), element.GetLocalName());
        if (index < 0)
        {
            throw new InvalidOperationException("The element has no blocked-element rule.");
        }

        return this.blockedElements[index].GetAction();
    }

    /// <summary>Reports whether the supplied attribute is permitted by the policy.</summary>
    /// <param name = "attributeName">The attribute name.</param>
    /// <param name = "elementName">The element name.</param>
    /// <returns><c>true</c> when the attribute is permitted by policy; otherwise, <c>false</c>.</returns>
    public bool IsAttributeAllowed(string attributeName, string elementName)
    {
        return this.IsAttributeAllowedInNamespaces("", attributeName, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName);
    }

    /// <summary>Reports whether the supplied attribute is permitted by the policy.</summary>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <returns><c>true</c> when the attribute is permitted by policy; otherwise, <c>false</c>.</returns>
    public bool IsAttributeAllowedInNamespaces(string attributeNamespaceUri, string attributeLocalName, string elementNamespaceUri, string elementLocalName)
    {
        string attributeNamespace = HtmlSanitizerPolicyNames.ValidateNamespaceUri(attributeNamespaceUri, "attributeNamespaceUri");
        string attribute = HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(attributeNamespace, attributeLocalName, "attributeLocalName");
        string elementNamespace = HtmlSanitizerPolicyNames.ValidateNamespaceUri(elementNamespaceUri, "elementNamespaceUri");
        string element = HtmlSanitizerPolicyNames.NormalizeElementLocalName(elementNamespace, elementLocalName, "elementLocalName");
        if (HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(attributeNamespace, attribute) && !this.markupConfiguration.GetAllowCustomizedBuiltInElements())
        {
            return false;
        }

        int specific = this.FindSanitizerAttributeRule(true, elementNamespace, element, attributeNamespace, attribute);
        if (specific >= 0)
        {
            return this.sanitizerAttributeRules[specific].GetAction() != HtmlSanitizerAttributeAction.Drop || this.HasAttributeValueRule(elementNamespace, element, attributeNamespace, attribute) || this.HasAttributeTokenRule(elementNamespace, element, attributeNamespace, attribute);
        }

        if (elementNamespace == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() && attributeNamespace.Length == 0 && FindElementAttribute(element, attribute) >= 0)
        {
            return true;
        }

        int global = this.FindSanitizerAttributeRule(false, "", "", attributeNamespace, attribute);
        if (global >= 0)
        {
            return this.sanitizerAttributeRules[global].GetAction() != HtmlSanitizerAttributeAction.Drop || this.HasAttributeValueRule(elementNamespace, element, attributeNamespace, attribute) || this.HasAttributeTokenRule(elementNamespace, element, attributeNamespace, attribute);
        }

        return attributeNamespace.Length == 0 && Find(this.allowedAttributes, attribute) >= 0 || this.allowDataAttributes && HtmlSanitizerMarkupNames.IsDataAttribute(attributeNamespace, attribute) || this.allowAriaAttributes && HtmlSanitizerMarkupNames.IsAriaAttribute(attributeNamespace, attribute) || this.markupConfiguration.GetAllowCustomizedBuiltInElements() && HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(attributeNamespace, attribute) || this.HasAttributeValueRule(elementNamespace, element, attributeNamespace, attribute) || this.HasAttributeTokenRule(elementNamespace, element, attributeNamespace, attribute) || elementNamespace == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() && attributeNamespace.Length == 0 && FindForcedAttribute(element, attribute) >= 0;
    }

    /// <summary>Indicates whether this object has the forced attribute.</summary>
    /// <param name = "elementName">The element name.</param>
    /// <param name = "attributeName">The attribute name.</param>
    /// <returns><c>true</c> when the attribute is forced; otherwise, <c>false</c>.</returns>
    public bool HasForcedAttribute(string elementName, string attributeName)
    {
        return this.HasForcedAttributeInNamespaces(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", attributeName);
    }

    /// <summary>Indicates whether this object has the forced attribute.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <returns><c>true</c> when the attribute is forced; otherwise, <c>false</c>.</returns>
    public bool HasForcedAttributeInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        int index = this.FindAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName);
        if (index >= 0)
        {
            return this.sanitizerAttributeValueRules[index].IsForced();
        }

        return elementNamespaceUri == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() && attributeNamespaceUri.Length == 0 && FindForcedAttribute(HtmlSanitizerPolicyNames.NormalizeMarkupName(elementLocalName, "elementLocalName"), HtmlSanitizerPolicyNames.NormalizeMarkupName(attributeLocalName, "attributeLocalName")) >= 0;
    }

    /// <summary>Indicates whether this object has the required attribute.</summary>
    /// <param name = "elementName">The element name.</param>
    /// <param name = "attributeName">The attribute name.</param>
    /// <returns><c>true</c> when the attribute is required; otherwise, <c>false</c>.</returns>
    public bool HasRequiredAttribute(string elementName, string attributeName)
    {
        return this.HasRequiredAttributeInNamespaces(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", attributeName);
    }

    /// <summary>Indicates whether this object has the required attribute.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <returns><c>true</c> when the attribute is required; otherwise, <c>false</c>.</returns>
    public bool HasRequiredAttributeInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        int index = this.FindAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName);
        return index >= 0 && !this.sanitizerAttributeValueRules[index].IsForced();
    }

    /// <summary>Gets the forced attribute value.</summary>
    /// <param name = "elementName">The element name.</param>
    /// <param name = "attributeName">The attribute name.</param>
    /// <returns>The forced attribute value.</returns>
    public string GetForcedAttributeValue(string elementName, string attributeName)
    {
        return this.GetForcedAttributeValueInNamespaces(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", attributeName);
    }

    /// <summary>Gets the forced attribute value.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <returns>The forced attribute value.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public string GetForcedAttributeValueInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        int valueIndex = this.FindAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName);
        if (valueIndex >= 0 && this.sanitizerAttributeValueRules[valueIndex].IsForced())
        {
            return this.sanitizerAttributeValueRules[valueIndex].GetValue();
        }

        if (elementNamespaceUri == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() && attributeNamespaceUri.Length == 0)
        {
            string element = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementLocalName, "elementLocalName");
            string attribute = HtmlSanitizerPolicyNames.NormalizeMarkupName(attributeLocalName, "attributeLocalName");
            int legacyIndex = FindForcedAttribute(element, attribute);
            if (legacyIndex >= 0)
            {
                return this.forcedAttributes[legacyIndex].GetValue();
            }
        }

        throw new InvalidOperationException("The element has no forced value for this attribute.");
    }

    /// <summary>Gets the required attribute value.</summary>
    /// <param name = "elementName">The element name.</param>
    /// <param name = "attributeName">The attribute name.</param>
    /// <returns>The required attribute value.</returns>
    public string GetRequiredAttributeValue(string elementName, string attributeName)
    {
        return this.GetRequiredAttributeValueInNamespaces(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), elementName, "", attributeName);
    }

    /// <summary>Gets the required attribute value.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <returns>The required attribute value.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public string GetRequiredAttributeValueInNamespaces(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        int index = this.FindAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName);
        if (index < 0 || this.sanitizerAttributeValueRules[index].IsForced())
        {
            throw new InvalidOperationException("The element has no required value for this attribute.");
        }

        return this.sanitizerAttributeValueRules[index].GetValue();
    }

    /// <summary>Reports whether the supplied URL scheme is permitted by the policy.</summary>
    /// <param name = "scheme">The URL scheme to test or apply.</param>
    /// <returns><c>true</c> when the URL scheme is permitted; otherwise, <c>false</c>.</returns>
    public bool IsUrlSchemeAllowed(string scheme)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeScheme(scheme, "scheme");
        return Find(this.urlSchemes, normalized) >= 0;
    }

    /// <summary>Reports whether the supplied URL host is permitted by the policy.</summary>
    /// <param name = "host">The normalized host name to test.</param>
    /// <returns><c>true</c> when the URL host is permitted; otherwise, <c>false</c>.</returns>
    public bool IsUrlHostAllowed(string host)
    {
        return this.urlPolicy.IsHostAllowed(host);
    }

    /// <summary>Reports whether the supplied URL port is permitted by the policy.</summary>
    /// <param name = "port">The numeric network port to test.</param>
    /// <returns><c>true</c> when the URL port is permitted; otherwise, <c>false</c>.</returns>
    public bool IsUrlPortAllowed(int port)
    {
        return this.urlPolicy.IsPortAllowed(port);
    }

    /// <summary>Reports whether the supplied CSS property is permitted by the policy.</summary>
    /// <param name = "propertyName">The CSS property name.</param>
    /// <returns><c>true</c> when the CSS property is permitted; otherwise, <c>false</c>.</returns>
    public bool IsCssPropertyAllowed(string propertyName)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(propertyName, "propertyName");
        return Find(this.cssProperties, normalized) >= 0
            || this.allowCssCustomProperties
                && CssValueSanitizer.StartsCustomName(normalized);
    }

    /// <summary>Gets conservative CSS functions explicitly disabled by this policy.</summary>
    /// <returns>A caller-owned sorted snapshot.</returns>
    public string[] GetBlockedCssFunctions()
    {
        return Copy(this.blockedCssFunctions);
    }

    /// <summary>Gets conservative CSS keywords explicitly disabled by this policy.</summary>
    /// <returns>A caller-owned sorted snapshot.</returns>
    public string[] GetBlockedCssKeywords()
    {
        return Copy(this.blockedCssKeywords);
    }

    /// <summary>Gets conservative CSS units explicitly disabled by this policy.</summary>
    /// <returns>A caller-owned sorted snapshot.</returns>
    public string[] GetBlockedCssUnits()
    {
        return Copy(this.blockedCssUnits);
    }

    /// <summary>Reports whether a known conservative CSS function remains enabled.</summary>
    /// <param name = "functionName">The function name to test.</param>
    /// <returns><c>true</c> when the function is known and not blocked; otherwise, <c>false</c>.</returns>
    public bool IsCssFunctionAllowed(string functionName)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(functionName, "functionName");
        return CssSafeData.GetFunctionProfile(normalized) != CssSafeFunctionProfile.Unknown
            && Find(this.blockedCssFunctions, normalized) < 0;
    }

    /// <summary>Reports whether a known conservative CSS keyword remains enabled.</summary>
    /// <param name = "keyword">The keyword to test.</param>
    /// <returns><c>true</c> when the keyword is known and not blocked; otherwise, <c>false</c>.</returns>
    public bool IsCssKeywordAllowed(string keyword)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(keyword, "keyword");
        return CssSafeData.IsKeyword(normalized)
            && Find(this.blockedCssKeywords, normalized) < 0;
    }

    /// <summary>Reports whether a known conservative CSS unit remains enabled.</summary>
    /// <param name = "unit">The unit to test.</param>
    /// <returns><c>true</c> when the unit is known and not blocked; otherwise, <c>false</c>.</returns>
    public bool IsCssUnitAllowed(string unit)
    {
        string normalized = HtmlSanitizerPolicyNames.NormalizeCssProperty(unit, "unit");
        return CssSafeData.GetUnitKind(normalized) != CssSafeUnitKind.Unknown
            && Find(this.blockedCssUnits, normalized) < 0;
    }

    /// <summary>Reports whether custom-property declarations and references are allowed.</summary>
    /// <returns><c>true</c> when conservative custom properties are enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowCssCustomProperties()
    {
        return this.allowCssCustomProperties;
    }

    /// <summary>Reports whether the policy permits <c>data-*</c> attributes.</summary>
    /// <returns><c>true</c> when <c>data-*</c> attributes are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowDataAttributes()
    {
        return this.allowDataAttributes;
    }

    /// <summary>Reports whether the policy permits <c>aria-*</c> attributes.</summary>
    /// <returns><c>true</c> when <c>aria-*</c> attributes are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowAriaAttributes()
    {
        return this.allowAriaAttributes;
    }

    /// <summary>Reports whether the policy permits custom elements.</summary>
    /// <returns><c>true</c> when custom elements are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowCustomElements()
    {
        return this.markupConfiguration.GetAllowCustomElements();
    }

    /// <summary>Reports whether the policy permits customized built-in elements.</summary>
    /// <returns><c>true</c> when customized built-in elements are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowCustomizedBuiltInElements()
    {
        return this.markupConfiguration.GetAllowCustomizedBuiltInElements();
    }

    /// <summary>Reports whether the policy preserves HTML comments.</summary>
    /// <returns><c>true</c> when comments are preserved; otherwise, <c>false</c>.</returns>
    public bool GetAllowComments()
    {
        return this.markupConfiguration.GetAllowComments();
    }

    /// <summary>Reports whether the policy preserves document type nodes.</summary>
    /// <returns><c>true</c> when document type nodes are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowDoctype()
    {
        return this.markupConfiguration.GetAllowDoctype();
    }

    /// <summary>Reports whether the policy permits HTML template elements.</summary>
    /// <returns><c>true</c> when template elements are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowTemplateElements()
    {
        return this.markupConfiguration.GetAllowTemplateElements();
    }

    /// <summary>Reports whether recognized server-template markers are neutralized.</summary>
    /// <returns><c>true</c> when recognized server-template markers are neutralized; otherwise, <c>false</c>.</returns>
    public bool GetNeutralizeServerTemplateMarkers()
    {
        return this.markupConfiguration.GetNeutralizeServerTemplateMarkers();
    }

    /// <summary>Gets the non-HTML namespace URIs that the compiled policy may retain.</summary>
    /// <returns>A snapshot of the allowed foreign namespaces; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedForeignNamespaces()
    {
        return this.markupConfiguration.GetForeignNamespaces();
    }

    /// <summary>Reports whether the supplied foreign namespace is permitted by the policy.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <returns><c>true</c> when the namespace is permitted by policy; otherwise, <c>false</c>.</returns>
    public bool IsForeignNamespaceAllowed(string namespaceUri)
    {
        return this.markupConfiguration.IsForeignNamespaceAllowed(namespaceUri);
    }

    /// <summary>Reports whether the supplied customized built-in element is permitted.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "isValue">Whether the supplied text is a value rather than a name.</param>
    /// <returns><c>true</c> when the customized built-in element is permitted; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public bool IsCustomizedBuiltInElementAllowed(string elementNamespaceUri, string elementLocalName, string isValue)
    {
        string namespaceUri = HtmlSanitizerPolicyNames.ValidateNamespaceUri(elementNamespaceUri, "elementNamespaceUri");
        string localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(namespaceUri, elementLocalName, "elementLocalName");
        if (isValue == null)
        {
            throw new ArgumentNullException("isValue");
        }

        return this.markupConfiguration.GetAllowCustomizedBuiltInElements() && namespaceUri == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() && this.IsElementAllowedInNamespace(namespaceUri, localName) && !HtmlSanitizerMarkupNames.IsCustomElementName(localName) && HtmlSanitizerMarkupNames.IsCustomElementName(isValue);
    }

    /// <summary>Gets the trusted prefix prepended when an unsafe <c>id</c> or <c>name</c> value is rewritten.</summary>
    /// <returns>The non-empty prefix used by DOM-clobbering defenses.</returns>
    public string GetDomClobberPrefix()
    {
        return this.domClobberPrefix;
    }

    /// <summary>Gets the allowed HTML-namespace element names as a convenience view.</summary>
    /// <returns>A snapshot of the allowed elements; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedElements()
    {
        string htmlNamespace = HtmlSanitizerPolicyNames.GetHtmlNamespaceUri();
        int count = 0;
        int index = 0;
        while (index < this.allowedElements.Length)
        {
            if (this.allowedElements[index].GetNamespaceUri() == htmlNamespace)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        string[] result = new string[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.allowedElements.Length)
        {
            HtmlSanitizerElementName element = this.allowedElements[index];
            if (element.GetNamespaceUri() == htmlNamespace)
            {
                result[outputIndex] = element.GetLocalName();
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the explicitly blocked HTML-namespace element names as a convenience view.</summary>
    /// <returns>A snapshot of the blocked elements; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetBlockedElements()
    {
        string htmlNamespace = HtmlSanitizerPolicyNames.GetHtmlNamespaceUri();
        int count = 0;
        int index = 0;
        while (index < this.blockedElements.Length)
        {
            if (this.blockedElements[index].GetNamespaceUri() == htmlNamespace)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        string[] result = new string[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.blockedElements.Length)
        {
            HtmlSanitizerBlockedElementRule rule = this.blockedElements[index];
            if (rule.GetNamespaceUri() == htmlNamespace)
            {
                result[outputIndex] = rule.GetLocalName();
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets independent copies of all namespace-aware allowed-element rules.</summary>
    /// <returns>A snapshot of the allowed element rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerElementName[] GetAllowedElementRules()
    {
        HtmlSanitizerElementName[] result = new HtmlSanitizerElementName[this.allowedElements.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.allowedElements[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the unnamespaced attributes allowed globally on eligible elements.</summary>
    /// <returns>A snapshot of the allowed attributes; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedAttributes()
    {
        return Copy(this.allowedAttributes);
    }

    /// <summary>Gets attributes allowed specifically on the named HTML element.</summary>
    /// <param name = "elementName">The element name.</param>
    /// <returns>A snapshot of the allowed attributes for element; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedAttributesForElement(string elementName)
    {
        string element = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementName, "elementName");
        int count = 0;
        int index = 0;
        while (index < this.elementAttributes.Length)
        {
            if (this.elementAttributes[index].GetElementName() == element)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        string[] result = new string[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.elementAttributes.Length)
        {
            HtmlSanitizerElementAttributeRule rule = this.elementAttributes[index];
            if (rule.GetElementName() == element)
            {
                result[outputIndex] = rule.GetAttributeName();
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets attributes that the compiled policy forces onto the named HTML element.</summary>
    /// <param name = "elementName">The element name.</param>
    /// <returns>A snapshot of the forced attributes for element; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetForcedAttributesForElement(string elementName)
    {
        string element = HtmlSanitizerPolicyNames.NormalizeMarkupName(elementName, "elementName");
        int count = 0;
        int index = 0;
        while (index < this.forcedAttributes.Length)
        {
            if (this.forcedAttributes[index].GetElementName() == element)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        string[] result = new string[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.forcedAttributes.Length)
        {
            HtmlSanitizerForcedAttributeRule rule = this.forcedAttributes[index];
            if (rule.GetElementName() == element)
            {
                result[outputIndex] = rule.GetAttributeName();
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets schemes accepted by the global fallback URL policy.</summary>
    /// <returns>A snapshot of the allowed URL schemes; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedUrlSchemes()
    {
        return Copy(this.urlSchemes);
    }

    /// <summary>Gets hosts accepted by the global fallback URL policy.</summary>
    /// <returns>A snapshot of the allowed URL hosts; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedUrlHosts()
    {
        return Copy(this.urlHosts);
    }

    /// <summary>Gets the fallback URL policy applied when no element-and-attribute-specific URL rule exists.</summary>
    /// <returns>An immutable policy value whose allowlist arrays are independent snapshots.</returns>
    public HtmlSanitizerUrlPolicy GetGlobalUrlPolicy()
    {
        return this.urlPolicy.Copy();
    }

    /// <summary>Gets ports accepted by the global fallback URL policy.</summary>
    /// <returns>A snapshot of the allowed URL ports; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public int[] GetAllowedUrlPorts()
    {
        return this.urlPolicy.GetAllowedPorts();
    }

    /// <summary>Reports whether the URL policy permits embedded user credentials.</summary>
    /// <returns><c>true</c> when URL credentials are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowUrlCredentials()
    {
        return this.urlPolicy.GetAllowCredentials();
    }

    /// <summary>Reports whether the policy permits relative URLs.</summary>
    /// <returns><c>true</c> when relative URLs are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowRelativeUrls()
    {
        return this.urlPolicy.GetAllowRelativeUrls();
    }

    /// <summary>Reports whether the policy permits protocol-relative URLs.</summary>
    /// <returns><c>true</c> when protocol-relative URLs are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowProtocolRelativeUrls()
    {
        return this.urlPolicy.GetAllowProtocolRelativeUrls();
    }

    /// <summary>Reports whether the policy permits fragment-only URLs.</summary>
    /// <returns><c>true</c> when fragment-only URLs are permitted; otherwise, <c>false</c>.</returns>
    public bool GetAllowFragmentUrls()
    {
        return this.urlPolicy.GetAllowFragmentUrls();
    }

    /// <summary>Gets the trusted base used by the global URL policy to resolve relative references.</summary>
    /// <returns>The configured absolute base URL, or an empty string when the policy has no base.</returns>
    public string GetUrlBase()
    {
        return this.urlPolicy.GetBaseUrl();
    }

    /// <summary>Reports whether accepted relative URLs are rewritten as absolute URLs.</summary>
    /// <returns><c>true</c> when accepted relative URLs are rewritten as absolute URLs; otherwise, <c>false</c>.</returns>
    public bool GetRewriteRelativeUrls()
    {
        return this.urlPolicy.GetRewriteRelativeUrls();
    }

    /// <summary>Gets <c>data:</c> media types accepted by the global fallback URL policy.</summary>
    /// <returns>A snapshot of the allowed data URL media types; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedDataUrlMediaTypes()
    {
        return this.urlPolicy.GetAllowedDataMediaTypes();
    }

    /// <summary>Gets the largest decoded payload accepted from an allowed <c>data:</c> URL.</summary>
    /// <returns>The positive payload ceiling in bytes.</returns>
    public int GetMaximumDataUrlBytes()
    {
        return this.urlPolicy.GetMaximumDataUrlBytes();
    }

    /// <summary>Gets independent copies of the element-and-attribute-specific URL rules.</summary>
    /// <returns>A snapshot of the URL attribute rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerUrlAttributeRule[] GetUrlAttributeRules()
    {
        HtmlSanitizerUrlAttributeRule[] result = new HtmlSanitizerUrlAttributeRule[this.urlAttributeRules.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.urlAttributeRules[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the matching attribute-specific URL policy, falling back to the global policy.</summary>
    /// <param name = "elementNamespaceUri">The element namespace URI.</param>
    /// <param name = "elementLocalName">The element local name.</param>
    /// <param name = "attributeNamespaceUri">The attribute namespace URI, or an empty string for no namespace.</param>
    /// <param name = "attributeLocalName">The attribute local name.</param>
    /// <returns>An immutable policy value whose allowlist arrays are independent snapshots.</returns>
    public HtmlSanitizerUrlPolicy GetUrlPolicy(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        HtmlSanitizerUrlAttributeRule probe = new HtmlSanitizerUrlAttributeRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName, this.urlPolicy);
        int index = 0;
        while (index < this.urlAttributeRules.Length)
        {
            HtmlSanitizerUrlAttributeRule rule = this.urlAttributeRules[index];
            if (SameUrlAttributeIdentity(rule, probe))
            {
                return rule.GetPolicy();
            }

            index = index + 1;
        }

        return this.urlPolicy.Copy();
    }

    /// <summary>Gets inline CSS property names accepted by the compiled policy.</summary>
    /// <returns>A snapshot of the allowed CSS properties; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAllowedCssProperties()
    {
        return Copy(this.cssProperties);
    }

    /// <summary>Gets independent copies of all namespace-aware blocked-element rules.</summary>
    /// <returns>A snapshot of the blocked element rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerBlockedElementRule[] GetBlockedElementRules()
    {
        HtmlSanitizerBlockedElementRule[] result = new HtmlSanitizerBlockedElementRule[this.blockedElements.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.blockedElements[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets independent copies of HTML element-specific attribute allow rules.</summary>
    /// <returns>A snapshot of the element attribute rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerElementAttributeRule[] GetElementAttributeRules()
    {
        HtmlSanitizerElementAttributeRule[] result = new HtmlSanitizerElementAttributeRule[this.elementAttributes.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.elementAttributes[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets independent copies of HTML element-specific forced-attribute rules.</summary>
    /// <returns>A snapshot of the forced attribute rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerForcedAttributeRule[] GetForcedAttributeRules()
    {
        HtmlSanitizerForcedAttributeRule[] result = new HtmlSanitizerForcedAttributeRule[this.forcedAttributes.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.forcedAttributes[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets independent copies of namespace-aware attribute action and normalization rules.</summary>
    /// <returns>A snapshot of the sanitizer attribute rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerAttributeRule[] GetSanitizerAttributeRules()
    {
        HtmlSanitizerAttributeRule[] result = new HtmlSanitizerAttributeRule[this.sanitizerAttributeRules.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.sanitizerAttributeRules[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets independent copies of rules that require or force complete attribute values.</summary>
    /// <returns>A snapshot of the sanitizer attribute value rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerAttributeValueRule[] GetSanitizerAttributeValueRules()
    {
        HtmlSanitizerAttributeValueRule[] result = new HtmlSanitizerAttributeValueRule[this.sanitizerAttributeValueRules.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.sanitizerAttributeValueRules[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets independent copies of rules that require tokens within attribute values.</summary>
    /// <returns>A snapshot of the sanitizer attribute token rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSanitizerAttributeTokenRule[] GetSanitizerAttributeTokenRules()
    {
        HtmlSanitizerAttributeTokenRule[] result = new HtmlSanitizerAttributeTokenRule[this.sanitizerAttributeTokenRules.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.sanitizerAttributeTokenRules[index].Copy();
            index = index + 1;
        }

        return result;
    }

    internal int FindBlocked(string namespaceUri, string localName)
    {
        int low = 0;
        int high = this.blockedElements.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            HtmlSanitizerBlockedElementRule rule = this.blockedElements[middle];
            int comparison = CompareElement(rule.GetNamespaceUri(), rule.GetLocalName(), namespaceUri, localName);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal int FindSanitizerAttributeRule(bool elementSpecific, string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        int index = 0;
        while (index < this.sanitizerAttributeRules.Length)
        {
            HtmlSanitizerAttributeRule rule = this.sanitizerAttributeRules[index];
            if (rule.IsElementSpecific() == elementSpecific && rule.GetElementNamespaceUri() == elementNamespaceUri && rule.GetElementLocalName() == elementLocalName && rule.GetAttributeNamespaceUri() == attributeNamespaceUri && rule.GetAttributeLocalName() == attributeLocalName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool HasAttributeValueRule(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        return this.FindAttributeValueRule(elementNamespaceUri, elementLocalName, attributeNamespaceUri, attributeLocalName) >= 0;
    }

    internal bool HasAttributeTokenRule(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        int index = 0;
        while (index < this.sanitizerAttributeTokenRules.Length)
        {
            HtmlSanitizerAttributeTokenRule rule = this.sanitizerAttributeTokenRules[index];
            if (rule.GetElementNamespaceUri() == elementNamespaceUri && rule.GetElementLocalName() == elementLocalName && rule.GetAttributeNamespaceUri() == attributeNamespaceUri && rule.GetAttributeLocalName() == attributeLocalName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int FindAttributeValueRule(string elementNamespaceUri, string elementLocalName, string attributeNamespaceUri, string attributeLocalName)
    {
        string elementNamespace = HtmlSanitizerPolicyNames.ValidateNamespaceUri(elementNamespaceUri, "elementNamespaceUri");
        string element = HtmlSanitizerPolicyNames.NormalizeElementLocalName(elementNamespace, elementLocalName, "elementLocalName");
        string attributeNamespace = HtmlSanitizerPolicyNames.ValidateNamespaceUri(attributeNamespaceUri, "attributeNamespaceUri");
        string attribute = HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(attributeNamespace, attributeLocalName, "attributeLocalName");
        int index = 0;
        while (index < this.sanitizerAttributeValueRules.Length)
        {
            HtmlSanitizerAttributeValueRule rule = this.sanitizerAttributeValueRules[index];
            if (rule.GetElementNamespaceUri() == elementNamespace && rule.GetElementLocalName() == element && rule.GetAttributeNamespaceUri() == attributeNamespace && rule.GetAttributeLocalName() == attribute)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindElementAttribute(string elementName, string attributeName)
    {
        int low = 0;
        int high = this.elementAttributes.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            HtmlSanitizerElementAttributeRule rule = this.elementAttributes[middle];
            int comparison = CompareRule(rule.GetElementName(), rule.GetAttributeName(), elementName, attributeName);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal int FindForcedAttribute(string elementName, string attributeName)
    {
        int low = 0;
        int high = this.forcedAttributes.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            HtmlSanitizerForcedAttributeRule rule = this.forcedAttributes[middle];
            int comparison = CompareRule(rule.GetElementName(), rule.GetAttributeName(), elementName, attributeName);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static int Find(string[] values, string value)
    {
        int low = 0;
        int high = values.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = string.CompareOrdinal(values[middle], value);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static int FindElement(HtmlSanitizerElementName[] values, string namespaceUri, string localName)
    {
        int low = 0;
        int high = values.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            HtmlSanitizerElementName element = values[middle];
            int comparison = CompareElement(element.GetNamespaceUri(), element.GetLocalName(), namespaceUri, localName);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static int CompareElement(string leftNamespace, string leftLocalName, string rightNamespace, string rightLocalName)
    {
        int result = string.CompareOrdinal(leftNamespace, rightNamespace);
        return result == 0 ? string.CompareOrdinal(leftLocalName, rightLocalName) : result;
    }

    internal static HtmlSanitizerMarkupConfiguration CreateDefaultMarkupConfiguration()
    {
        return new HtmlSanitizerMarkupConfiguration(false, false, false, false, false, false, new string[0]);
    }

    internal static HtmlSanitizerUrlPolicy CreateDefaultUrlPolicy(string[] schemes, string[] hosts)
    {
        return new HtmlSanitizerUrlPolicy(schemes, hosts, new int[0], false, true, false, true, "", false);
    }

    internal static int CompareRule(string leftElement, string leftAttribute, string rightElement, string rightAttribute)
    {
        int result = string.CompareOrdinal(leftElement, rightElement);
        return result == 0 ? string.CompareOrdinal(leftAttribute, rightAttribute) : result;
    }

    internal void ValidateTables()
    {
        ValidateUniqueElements(this.allowedElements);
        ValidateUnique(this.allowedAttributes, "allowed attribute");
        ValidateUnique(this.urlSchemes, "URL scheme");
        ValidateUnique(this.urlHosts, "URL host");
        ValidateUnique(this.cssProperties, "CSS property");
        int index = 0;
        while (index < this.blockedElements.Length)
        {
            HtmlSanitizerBlockedElementRule rule = this.blockedElements[index];
            if (index > 0 && CompareBlocked(this.blockedElements[index - 1], rule) == 0)
            {
                throw new ArgumentException("A compiled policy contains duplicate blocked elements.");
            }

            if (FindElement(this.allowedElements, rule.GetNamespaceUri(), rule.GetLocalName()) >= 0)
            {
                throw new ArgumentException("A compiled policy both allows and blocks an element.");
            }

            index = index + 1;
        }

        ValidateElementAttributeTables();
        ValidateSanitizerAttributeTables();
        ValidateUrlAttributeRules();
    }

    internal void ValidateUrlAttributeRules()
    {
        int index = 0;
        while (index < this.urlAttributeRules.Length)
        {
            HtmlSanitizerUrlAttributeRule rule = this.urlAttributeRules[index];
            if (!this.IsElementAllowedInNamespace(rule.GetElementNamespaceUri(), rule.GetElementLocalName()))
            {
                throw new ArgumentException("A URL attribute override targets a disallowed element.");
            }

            int previous = 0;
            while (previous < index)
            {
                if (SameUrlAttributeIdentity(this.urlAttributeRules[previous], rule))
                {
                    throw new ArgumentException("A compiled policy contains duplicate URL attribute overrides.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }
    }

    internal void ValidateSanitizerAttributeTables()
    {
        int index = 0;
        while (index < this.sanitizerAttributeRules.Length)
        {
            HtmlSanitizerAttributeRule rule = this.sanitizerAttributeRules[index];
            if (rule.IsElementSpecific()
                && !this.IsElementAllowedInNamespace(
                    rule.GetElementNamespaceUri(),
                    rule.GetElementLocalName()))
            {
                throw new ArgumentException("An attribute action targets a disallowed element.");
            }

            if (index > 0 && CompareAttributeRule(this.sanitizerAttributeRules[index - 1], rule) == 0)
            {
                throw new ArgumentException("A compiled policy contains duplicate attribute actions.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.sanitizerAttributeValueRules.Length)
        {
            HtmlSanitizerAttributeValueRule rule = this.sanitizerAttributeValueRules[index];
            if (FindElement(this.allowedElements, rule.GetElementNamespaceUri(), rule.GetElementLocalName()) < 0)
            {
                throw new ArgumentException("A required or forced attribute targets a disallowed element.");
            }

            if (HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(rule.GetAttributeNamespaceUri(), rule.GetAttributeLocalName()) && (!this.markupConfiguration.GetAllowCustomizedBuiltInElements() || rule.GetElementNamespaceUri() != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri() || HtmlSanitizerMarkupNames.IsCustomElementName(rule.GetElementLocalName()) || !HtmlSanitizerMarkupNames.IsCustomElementName(rule.GetValue())))
            {
                throw new ArgumentException("A compiled is value rule is not a valid customized built-in element.");
            }

            if (index > 0 && CompareAttributeValueRule(this.sanitizerAttributeValueRules[index - 1], rule) == 0)
            {
                throw new ArgumentException("A compiled policy contains duplicate attribute value rules.");
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.sanitizerAttributeTokenRules.Length)
        {
            HtmlSanitizerAttributeTokenRule rule = this.sanitizerAttributeTokenRules[index];
            if (FindElement(this.allowedElements, rule.GetElementNamespaceUri(), rule.GetElementLocalName()) < 0)
            {
                throw new ArgumentException("A forced attribute token rule targets a disallowed element.");
            }

            if (index > 0 && CompareAttributeTokenRule(this.sanitizerAttributeTokenRules[index - 1], rule) == 0)
            {
                throw new ArgumentException("A compiled policy contains duplicate forced attribute token rules.");
            }

            index = index + 1;
        }
    }

    internal void ValidateElementAttributeTables()
    {
        int index = 0;
        while (index < this.elementAttributes.Length)
        {
            HtmlSanitizerElementAttributeRule rule = this.elementAttributes[index];
            if (FindElement(this.allowedElements, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), rule.GetElementName()) < 0)
            {
                throw new ArgumentException("An element-attribute rule targets a disallowed element.");
            }

            if (index > 0)
            {
                HtmlSanitizerElementAttributeRule previous = this.elementAttributes[index - 1];
                if (CompareRule(previous.GetElementName(), previous.GetAttributeName(), rule.GetElementName(), rule.GetAttributeName()) == 0)
                {
                    throw new ArgumentException("A compiled policy contains duplicate element-attribute rules.");
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.forcedAttributes.Length)
        {
            HtmlSanitizerForcedAttributeRule rule = this.forcedAttributes[index];
            if (FindElement(this.allowedElements, HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), rule.GetElementName()) < 0)
            {
                throw new ArgumentException("A forced attribute targets a disallowed element.");
            }

            if (index > 0)
            {
                HtmlSanitizerForcedAttributeRule previous = this.forcedAttributes[index - 1];
                if (CompareRule(previous.GetElementName(), previous.GetAttributeName(), rule.GetElementName(), rule.GetAttributeName()) == 0)
                {
                    throw new ArgumentException("A compiled policy contains duplicate forced attributes.");
                }
            }

            index = index + 1;
        }
    }

    internal static void ValidateUnique(string[] values, string description)
    {
        int index = 1;
        while (index < values.Length)
        {
            if (values[index - 1] == values[index])
            {
                throw new ArgumentException("A compiled policy contains a duplicate " + description + ".");
            }

            index = index + 1;
        }
    }

    internal static bool SameStrings(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateUniqueElements(HtmlSanitizerElementName[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            HtmlSanitizerElementName previous = values[index - 1];
            HtmlSanitizerElementName current = values[index];
            if (CompareElement(previous.GetNamespaceUri(), previous.GetLocalName(), current.GetNamespaceUri(), current.GetLocalName()) == 0)
            {
                throw new ArgumentException("A compiled policy contains a duplicate allowed element.");
            }

            index = index + 1;
        }
    }

    internal static string[] CopyAndSortMarkupNames(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = HtmlSanitizerPolicyNames.NormalizeMarkupName(values[index], "values");
            index = index + 1;
        }

        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    internal static HtmlSanitizerElementName[] CreateHtmlElementNames(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerElementName[] result = new HtmlSanitizerElementName[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = new HtmlSanitizerElementName(HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(), values[index]);
            index = index + 1;
        }

        return result;
    }

    internal static HtmlSanitizerElementName[] CopyAndSort(HtmlSanitizerElementName[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerElementName[] result = new HtmlSanitizerElementName[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("An allowed-element rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            HtmlSanitizerElementName current = result[index];
            int position = index;
            while (position > 0 && CompareElement(result[position - 1].GetNamespaceUri(), result[position - 1].GetLocalName(), current.GetNamespaceUri(), current.GetLocalName()) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        return result;
    }

    internal static string[] CopyAndSortSchemes(string[] values)
    {
        return NormalizeAndSort(values, 0);
    }

    internal static string[] CopyAndSortHosts(string[] values)
    {
        return NormalizeAndSort(values, 1);
    }

    internal static string[] CopyAndSortCssProperties(string[] values)
    {
        return NormalizeAndSort(values, 2);
    }

    internal static string[] CopyAndSortCssIdentifiers(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = HtmlSanitizerPolicyNames.NormalizeCssProperty(
                values[index], "values");
            index = index + 1;
        }

        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    internal static string[] NormalizeAndSort(string[] values, int kind)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (kind == 0)
            {
                result[index] = HtmlSanitizerPolicyNames.NormalizeScheme(values[index], "values");
            }
            else if (kind == 1)
            {
                result[index] = HtmlSanitizerPolicyNames.NormalizeHost(values[index], "values");
            }
            else
            {
                result[index] = HtmlSanitizerPolicyNames.NormalizeCssProperty(values[index], "values");
                if (CssSafeData.GetPropertyProfile(result[index]) == CssSafePropertyProfile.Unknown)
                {
                    throw new ArgumentException("A CSS property is outside the conservative inline style allowlist.", "values");
                }
            }

            index = index + 1;
        }

        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }

    internal static HtmlSanitizerBlockedElementRule[] CopyAndSort(HtmlSanitizerBlockedElementRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerBlockedElementRule[] result = new HtmlSanitizerBlockedElementRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A blocked-element rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            HtmlSanitizerBlockedElementRule current = result[index];
            int position = index;
            while (position > 0 && CompareBlocked(result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        return result;
    }

    internal static int CompareBlocked(HtmlSanitizerBlockedElementRule left, HtmlSanitizerBlockedElementRule right)
    {
        return CompareElement(left.GetNamespaceUri(), left.GetLocalName(), right.GetNamespaceUri(), right.GetLocalName());
    }

    internal static HtmlSanitizerElementAttributeRule[] CopyAndSort(HtmlSanitizerElementAttributeRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerElementAttributeRule[] result = new HtmlSanitizerElementAttributeRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("An element-attribute rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        SortElementAttributes(result);
        return result;
    }

    internal static HtmlSanitizerAttributeRule[] CopyAndSort(HtmlSanitizerAttributeRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerAttributeRule[] result = new HtmlSanitizerAttributeRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A sanitizer attribute rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            HtmlSanitizerAttributeRule current = result[index];
            int position = index;
            while (position > 0 && CompareAttributeRule(result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        return result;
    }

    internal static HtmlSanitizerUrlAttributeRule[] CopyUrlAttributeRules(HtmlSanitizerUrlAttributeRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("urlAttributeRulesValue");
        }

        HtmlSanitizerUrlAttributeRule[] result = new HtmlSanitizerUrlAttributeRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A URL attribute override cannot be null.", "urlAttributeRulesValue");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            HtmlSanitizerUrlAttributeRule current = result[index];
            int position = index;
            while (position > 0 && CompareUrlAttributeRule(result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        return result;
    }

    internal static int CompareUrlAttributeRule(HtmlSanitizerUrlAttributeRule left, HtmlSanitizerUrlAttributeRule right)
    {
        int result = CompareElement(left.GetElementNamespaceUri(), left.GetElementLocalName(), right.GetElementNamespaceUri(), right.GetElementLocalName());
        if (result == 0)
        {
            result = CompareElement(left.GetAttributeNamespaceUri(), left.GetAttributeLocalName(), right.GetAttributeNamespaceUri(), right.GetAttributeLocalName());
        }

        return result;
    }

    internal static bool SameUrlAttributeIdentity(HtmlSanitizerUrlAttributeRule left, HtmlSanitizerUrlAttributeRule right)
    {
        return left.GetElementNamespaceUri() == right.GetElementNamespaceUri() && left.GetElementLocalName() == right.GetElementLocalName() && left.GetAttributeNamespaceUri() == right.GetAttributeNamespaceUri() && left.GetAttributeLocalName() == right.GetAttributeLocalName();
    }

    internal static HtmlSanitizerAttributeValueRule[] CopyAndSort(HtmlSanitizerAttributeValueRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerAttributeValueRule[] result = new HtmlSanitizerAttributeValueRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A sanitizer attribute value rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            HtmlSanitizerAttributeValueRule current = result[index];
            int position = index;
            while (position > 0 && CompareAttributeValueRule(result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        return result;
    }

    internal static HtmlSanitizerAttributeTokenRule[] CopyAndSort(HtmlSanitizerAttributeTokenRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerAttributeTokenRule[] result = new HtmlSanitizerAttributeTokenRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A sanitizer attribute token rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            HtmlSanitizerAttributeTokenRule current = result[index];
            int position = index;
            while (position > 0 && CompareAttributeTokenRule(result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        return result;
    }

    internal static int CompareAttributeRule(HtmlSanitizerAttributeRule left, HtmlSanitizerAttributeRule right)
    {
        int result = left.IsElementSpecific().CompareTo(right.IsElementSpecific());
        if (result == 0)
        {
            result = CompareElement(left.GetElementNamespaceUri(), left.GetElementLocalName(), right.GetElementNamespaceUri(), right.GetElementLocalName());
        }

        if (result == 0)
        {
            result = CompareElement(left.GetAttributeNamespaceUri(), left.GetAttributeLocalName(), right.GetAttributeNamespaceUri(), right.GetAttributeLocalName());
        }

        return result;
    }

    internal static int CompareAttributeValueRule(HtmlSanitizerAttributeValueRule left, HtmlSanitizerAttributeValueRule right)
    {
        int result = CompareElement(left.GetElementNamespaceUri(), left.GetElementLocalName(), right.GetElementNamespaceUri(), right.GetElementLocalName());
        if (result == 0)
        {
            result = CompareElement(left.GetAttributeNamespaceUri(), left.GetAttributeLocalName(), right.GetAttributeNamespaceUri(), right.GetAttributeLocalName());
        }

        return result;
    }

    internal static int CompareAttributeTokenRule(HtmlSanitizerAttributeTokenRule left, HtmlSanitizerAttributeTokenRule right)
    {
        int result = CompareElement(left.GetElementNamespaceUri(), left.GetElementLocalName(), right.GetElementNamespaceUri(), right.GetElementLocalName());
        if (result == 0)
        {
            result = CompareElement(left.GetAttributeNamespaceUri(), left.GetAttributeLocalName(), right.GetAttributeNamespaceUri(), right.GetAttributeLocalName());
        }

        return result;
    }

    internal static HtmlSanitizerForcedAttributeRule[] CopyAndSort(HtmlSanitizerForcedAttributeRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        HtmlSanitizerForcedAttributeRule[] result = new HtmlSanitizerForcedAttributeRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A forced-attribute rule cannot be null.", "values");
            }

            result[index] = values[index].Copy();
            index = index + 1;
        }

        SortForcedAttributes(result);
        return result;
    }

    internal static void SortElementAttributes(HtmlSanitizerElementAttributeRule[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            HtmlSanitizerElementAttributeRule current = values[index];
            int position = index;
            while (position > 0 && CompareRule(values[position - 1].GetElementName(), values[position - 1].GetAttributeName(), current.GetElementName(), current.GetAttributeName()) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }

            values[position] = current;
            index = index + 1;
        }
    }

    internal static void SortForcedAttributes(HtmlSanitizerForcedAttributeRule[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            HtmlSanitizerForcedAttributeRule current = values[index];
            int position = index;
            while (position > 0 && CompareRule(values[position - 1].GetElementName(), values[position - 1].GetAttributeName(), current.GetElementName(), current.GetAttributeName()) > 0)
            {
                values[position] = values[position - 1];
                position = position - 1;
            }

            values[position] = current;
            index = index + 1;
        }
    }

    internal static string[] Copy(string[] values)
    {
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A compiled policy name cannot be null.", "values");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
