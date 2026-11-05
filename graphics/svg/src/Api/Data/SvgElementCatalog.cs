namespace svg;

/// <summary>Defines one allowed child kind and its per-kind cardinality.</summary>
public class SvgElementChildRule
{
    /// <summary>The allowed current element kind.</summary>
    public SvgElementKind Kind;
    /// <summary>The minimum required count.</summary>
    public int Minimum;
    /// <summary>The maximum count, or <c>-1</c> when unbounded.</summary>
    public int Maximum;

    /// <summary>Creates one child-kind cardinality rule.</summary>
    /// <param name="kind">The allowed current element kind.</param>
    /// <param name="minimum">The nonnegative minimum count.</param>
    /// <param name="maximum">The maximum count, or <c>-1</c>.</param>
    public SvgElementChildRule(SvgElementKind kind, int minimum, int maximum)
    {
        if (!new SvgNodeNames().IsSvg2Element(kind)) throw new System.ArgumentOutOfRangeException("kind");
        if (minimum < 0 || maximum < -1 || maximum >= 0 && maximum < minimum) throw new System.ArgumentOutOfRangeException("minimum");
        Kind = kind;
        Minimum = minimum;
        Maximum = maximum;
    }
}

/// <summary>Defines text, foreign-content, child-kind, and cardinality constraints.</summary>
public class SvgElementContentModel
{
    /// <summary>Whether character data is allowed.</summary>
    public bool AllowsText;
    /// <summary>Whether elements outside the SVG namespace are allowed.</summary>
    public bool AllowsForeignElements;
    /// <summary>The minimum total element-child count.</summary>
    public int MinimumElementChildren;
    /// <summary>The maximum total element-child count, or <c>-1</c> when unbounded.</summary>
    public int MaximumElementChildren;
    /// <summary>The allowed current SVG child kinds.</summary>
    public System.Collections.Generic.List<SvgElementChildRule> ChildRules;

    internal SvgElementContentModel(bool allowsText, bool allowsForeignElements)
    {
        AllowsText = allowsText;
        AllowsForeignElements = allowsForeignElements;
        MinimumElementChildren = 0;
        MaximumElementChildren = -1;
        ChildRules = new System.Collections.Generic.List<SvgElementChildRule>();
    }

    /// <summary>Tests whether one current SVG element kind is permitted.</summary>
    /// <param name="kind">The proposed child kind.</param>
    /// <returns><c>true</c> when the kind is listed by this model.</returns>
    internal bool AllowsElement(SvgElementKind kind)
    {
        int index = 0;
        while (index < ChildRules.Count)
        {
            if (ChildRules[index].Kind == kind) return true;
            index = index + 1;
        }
        return false;
    }

    /// <summary>Tests a complete ordered element-child snapshot against all cardinalities.</summary>
    /// <param name="children">The ordered current SVG child kinds.</param>
    /// <returns><c>true</c> when every child and count is allowed.</returns>
    internal bool AllowsElementChildren(System.Collections.Generic.List<SvgElementKind>? children)
    {
        if (children == null) return false;
        if (children.Count < MinimumElementChildren) return false;
        if (MaximumElementChildren >= 0 && children.Count > MaximumElementChildren) return false;
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            if (!AllowsElement(children[childIndex])) return false;
            childIndex = childIndex + 1;
        }
        int ruleIndex = 0;
        while (ruleIndex < ChildRules.Count)
        {
            SvgElementChildRule rule = ChildRules[ruleIndex];
            int count = 0;
            childIndex = 0;
            while (childIndex < children.Count)
            {
                if (children[childIndex] == rule.Kind) count = count + 1;
                childIndex = childIndex + 1;
            }
            if (count < rule.Minimum || rule.Maximum >= 0 && count > rule.Maximum) return false;
            ruleIndex = ruleIndex + 1;
        }
        return true;
    }
}

/// <summary>Defines applicability, requirement, and default metadata for one attribute.</summary>
public class SvgElementAttributeDefinition
{
    /// <summary>The attribute namespace URI.</summary>
    public string NamespaceUri;
    /// <summary>The case-sensitive local name.</summary>
    public string LocalName;
    /// <summary>Whether the attribute is required by the element model.</summary>
    public bool Required;
    /// <summary>How the absent attribute obtains its default.</summary>
    public SvgAttributeDefaultKind DefaultKind;
    /// <summary>The literal default or referenced sibling attribute name.</summary>
    public string DefaultValue;
    /// <summary>Whether this is a global attribute.</summary>
    public bool Global;
    /// <summary>Whether this is a CSS presentation attribute.</summary>
    public bool Presentation;

    /// <summary>Creates one element attribute definition.</summary>
    /// <param name="namespaceUri">The non-null namespace URI.</param>
    /// <param name="localName">The nonempty local name.</param>
    /// <param name="required">Whether the attribute is required.</param>
    /// <param name="defaultKind">The absent-value rule.</param>
    /// <param name="defaultValue">The default text or related attribute name.</param>
    /// <param name="global">Whether this is a global attribute.</param>
    /// <param name="presentation">Whether this is a presentation attribute.</param>
    public SvgElementAttributeDefinition(string namespaceUri, string localName, bool required, SvgAttributeDefaultKind defaultKind, string defaultValue, bool global, bool presentation)
    {
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (localName == null || localName.Length == 0) throw new System.ArgumentOutOfRangeException("localName");
        if (defaultKind < SvgAttributeDefaultKind.None || defaultKind > SvgAttributeDefaultKind.ContextDependent) throw new System.ArgumentOutOfRangeException("defaultKind");
        if (defaultValue == null) throw new System.ArgumentNullException("defaultValue");
        if (defaultKind == SvgAttributeDefaultKind.None || defaultKind == SvgAttributeDefaultKind.ContextDependent)
        {
            if (defaultValue.Length != 0) throw new System.ArgumentException("This default kind requires empty default text.", "defaultValue");
        }
        else if (defaultKind == SvgAttributeDefaultKind.OtherAttribute && defaultValue.Length == 0) throw new System.ArgumentException("A related-attribute default requires a nonempty attribute name.", "defaultValue");
        NamespaceUri = namespaceUri;
        LocalName = localName;
        Required = required;
        DefaultKind = defaultKind;
        DefaultValue = defaultValue;
        Global = global;
        Presentation = presentation;
    }
}

/// <summary>Defines one current SVG element's categories, content, attributes, and rendering behavior.</summary>
public class SvgElementDefinition
{
    /// <summary>The current SVG element kind.</summary>
    public SvgElementKind Kind;
    /// <summary>The case-sensitive SVG local name.</summary>
    public string LocalName;
    /// <summary>The semantic categories assigned to the element.</summary>
    public System.Collections.Generic.List<SvgElementCategory> Categories;
    /// <summary>The element's child-content model.</summary>
    public SvgElementContentModel ContentModel;
    /// <summary>The element-specific attributes, excluding global and presentation attributes.</summary>
    public System.Collections.Generic.List<SvgElementAttributeDefinition> Attributes;
    /// <summary>The element's rendering behavior.</summary>
    public SvgElementRenderMode RenderMode;
    /// <summary>Whether conditional-processing attributes are applicable.</summary>
    public bool AllowsConditionalAttributes;
    System.Collections.Generic.List<SvgElementAttributeDefinition> globalAttributes;
    System.Collections.Generic.List<SvgElementAttributeDefinition> presentationAttributes;

    internal SvgElementDefinition(SvgElementKind kind, string localName, System.Collections.Generic.List<SvgElementCategory> categories, SvgElementContentModel contentModel, System.Collections.Generic.List<SvgElementAttributeDefinition> attributes, SvgElementRenderMode renderMode, bool allowsConditionalAttributes, System.Collections.Generic.List<SvgElementAttributeDefinition> globals, System.Collections.Generic.List<SvgElementAttributeDefinition> presentations)
    {
        Kind = kind;
        LocalName = localName;
        Categories = categories;
        ContentModel = contentModel;
        Attributes = attributes;
        RenderMode = renderMode;
        AllowsConditionalAttributes = allowsConditionalAttributes;
        globalAttributes = globals;
        presentationAttributes = presentations;
    }

    /// <summary>Finds an applicable attribute by expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The attribute definition, or <c>null</c>.</returns>
    internal SvgElementAttributeDefinition? FindAttribute(string? namespaceUri, string? localName)
    {
        if (namespaceUri == null || localName == null || localName.Length == 0) return null;
        SvgElementAttributeDefinition? found = FindIn(Attributes, namespaceUri, localName);
        if (found != null) return found;
        found = FindIn(globalAttributes, namespaceUri, localName);
        if (found != null) return found;
        if (namespaceUri.Length == 0 && AllowsConditionalAttributes && (localName == SvgAttributeNames.RequiredExtensions || localName == SvgAttributeNames.SystemLanguage))
        {
            return new SvgElementAttributeDefinition("", localName, false, SvgAttributeDefaultKind.None, "", true, false);
        }
        if (namespaceUri.Length == 0)
        {
            SvgAttributeKind attributeKind = new SvgNodeNames().GetAttributeKind("", localName);
            if (attributeKind == SvgAttributeKind.Aria || attributeKind == SvgAttributeKind.Data)
            {
                return new SvgElementAttributeDefinition("", localName, false, SvgAttributeDefaultKind.None, "", true, false);
            }
            if (attributeKind == SvgAttributeKind.Svg2 && localName.StartsWith("on", System.StringComparison.Ordinal))
            {
                return new SvgElementAttributeDefinition("", localName, false, SvgAttributeDefaultKind.None, "", true, false);
            }
        }
        if (namespaceUri == SvgNamespaces.XLink && (localName == SvgAttributeNames.Href || localName == SvgAttributeNames.Title))
        {
            SvgElementAttributeDefinition? unqualified = FindIn(Attributes, "", localName);
            if (unqualified != null) return new SvgElementAttributeDefinition(namespaceUri, localName, unqualified.Required, unqualified.DefaultKind, unqualified.DefaultValue, false, false);
        }
        return FindIn(presentationAttributes, namespaceUri, localName);
    }

    /// <summary>Tests whether an expanded attribute name is applicable.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns><c>true</c> when the catalog accepts the attribute.</returns>
    internal bool IsAttributeApplicable(string namespaceUri, string localName)
    {
        return FindAttribute(namespaceUri, localName) != null;
    }

    /// <summary>Returns an independent ordered snapshot of all fixed applicable attributes.</summary>
    /// <returns>Element-specific, global, and presentation attribute definitions.</returns>
    internal System.Collections.Generic.List<SvgElementAttributeDefinition> GetApplicableAttributes()
    {
        System.Collections.Generic.List<SvgElementAttributeDefinition> result = new System.Collections.Generic.List<SvgElementAttributeDefinition>();
        AppendUnique(result, Attributes);
        AppendUnique(result, globalAttributes);
        if (AllowsConditionalAttributes)
        {
            result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.RequiredExtensions, false, SvgAttributeDefaultKind.None, "", true, false));
            result.Add(new SvgElementAttributeDefinition("", SvgAttributeNames.SystemLanguage, false, SvgAttributeDefaultKind.None, "", true, false));
        }
        AppendUnique(result, presentationAttributes);
        return result;
    }

    /// <summary>Tests whether the element contributes rendering in the supplied context.</summary>
    /// <param name="referenced">Whether the element is being resolved through a reference.</param>
    /// <param name="hostCallbackAvailable">Whether host-content rendering is available.</param>
    /// <returns><c>true</c> when the element can contribute rendered output.</returns>
    internal bool IsRenderable(bool referenced, bool hostCallbackAvailable)
    {
        if (RenderMode == SvgElementRenderMode.Direct || RenderMode == SvgElementRenderMode.Container || RenderMode == SvgElementRenderMode.Text) return true;
        if (RenderMode == SvgElementRenderMode.Referenced) return referenced;
        if (RenderMode == SvgElementRenderMode.HostCallback) return hostCallbackAvailable;
        return false;
    }

    internal static SvgElementAttributeDefinition? FindIn(System.Collections.Generic.List<SvgElementAttributeDefinition> items, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < items.Count)
        {
            SvgElementAttributeDefinition item = items[index];
            if (item.NamespaceUri == namespaceUri && item.LocalName == localName) return item;
            index = index + 1;
        }
        return null;
    }

    internal static void AppendUnique(System.Collections.Generic.List<SvgElementAttributeDefinition> target, System.Collections.Generic.List<SvgElementAttributeDefinition> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            SvgElementAttributeDefinition item = source[index];
            if (FindIn(target, item.NamespaceUri, item.LocalName) == null) target.Add(item);
            index = index + 1;
        }
    }
}
