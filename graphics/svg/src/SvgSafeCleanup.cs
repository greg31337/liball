namespace svg;

internal static class SvgSafeCleanup
{
    internal static void RemoveComments(SvgProcessContext context)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Comment && node.Parent != null) RemoveNode(context, node);
            index = index - 1;
        }
    }

    internal static void RemoveEditorMetadata(SvgProcessContext context)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element && node.Parent != null && IsEditorNamespace(node.NamespaceUri)) RemoveNode(context, node);
            index = index - 1;
        }
        index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element && object.ReferenceEquals(node.OwnerDocument, context.Document)) RemoveEditorAttributes(context, node);
            index = index + 1;
        }
    }

    internal static void RemoveUnusedNamespaces(SvgProcessContext context)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[nodeIndex];
            if (node.Kind == SvgNodeKind.Element && object.ReferenceEquals(node.OwnerDocument, context.Document))
            {
                int attributeIndex = node.Attributes.Count - 1;
                while (attributeIndex >= 0 && !context.IsFailed())
                {
                    SvgAttribute attribute = node.Attributes[attributeIndex];
                    if (attribute.NamespaceUri == SvgNamespaces.Xmlns)
                    {
                        string prefix = NamespacePrefix(attribute);
                        if (InheritedBinding(node, prefix, attribute.Value) || !BindingUsed(node, attribute, prefix, attribute.Value)) RemoveAttribute(context, node, attribute);
                    }
                    attributeIndex = attributeIndex - 1;
                }
            }
            nodeIndex = nodeIndex + 1;
        }
    }

    internal static void RemoveDefaultAttributes(SvgProcessContext context)
    {
        SvgElementCatalog elements = new SvgCatalogFactory().CreateElementCatalog();
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        bool selectorsPresent = HasStyleSheet(context.Document);
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[nodeIndex];
            if (node.Kind == SvgNodeKind.Element && object.ReferenceEquals(node.OwnerDocument, context.Document))
            {
                int attributeIndex = node.Attributes.Count - 1;
                while (attributeIndex >= 0 && !context.IsFailed())
                {
                    SvgAttribute attribute = node.Attributes[attributeIndex];
                    if (RedundantHref(node, attribute) || !selectorsPresent && DefaultAttribute(node, attribute, elements, properties)) RemoveAttribute(context, node, attribute);
                    attributeIndex = attributeIndex - 1;
                }
            }
            nodeIndex = nodeIndex + 1;
        }
    }

    internal static void RemoveEmptyContainers(SvgProcessContext context)
    {
        System.Collections.Generic.HashSet<string> references = ReferencedIds(context.Document);
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Parent != null && object.ReferenceEquals(node.OwnerDocument, context.Document) && EmptyCandidate(node) && !MeaningfulContent(node) && !Referenced(node, references) && !ProtectedSemantics(node)) RemoveNode(context, node);
            index = index - 1;
        }
    }

    internal static void RemoveUnreferencedInvisibleNodes(SvgProcessContext context)
    {
        if (HasStyleSheet(context.Document)) return;
        System.Collections.Generic.HashSet<string> references = ReferencedIds(context.Document);
        SvgElementCatalog elements = new SvgCatalogFactory().CreateElementCatalog();
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int index = nodes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (node.Parent != null && object.ReferenceEquals(node.OwnerDocument, context.Document) && InvisibleCandidate(node, elements) && !SubtreeReferenced(node, references) && !ProtectedSemantics(node) && !ContainsAnimation(node)) RemoveNode(context, node);
            index = index - 1;
        }
    }

    static void RemoveEditorAttributes(SvgProcessContext context, SvgNode node)
    {
        int index = node.Attributes.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgAttribute attribute = node.Attributes[index];
            if (IsEditorNamespace(attribute.NamespaceUri)) RemoveAttribute(context, node, attribute);
            index = index - 1;
        }
    }

    static bool IsEditorNamespace(string? value)
    {
        if (value == null || value.Length == 0) return false;
        if (value == "http://www.inkscape.org/namespaces/inkscape") return true;
        if (value == "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd") return true;
        if (value.StartsWith("http://ns.adobe.com/", System.StringComparison.Ordinal)) return true;
        if (value.StartsWith("http://www.bohemiancoding.com/sketch/ns", System.StringComparison.Ordinal)) return true;
        if (value.StartsWith("http://www.serif.com/", System.StringComparison.Ordinal)) return true;
        if (value.StartsWith("http://schemas.microsoft.com/visio/", System.StringComparison.Ordinal)) return true;
        if (value.StartsWith("http://product.corel.com/", System.StringComparison.Ordinal)) return true;
        return false;
    }

    static bool RedundantHref(SvgNode node, SvgAttribute attribute)
    {
        if (attribute.NamespaceUri != SvgNamespaces.XLink || attribute.LocalName != SvgAttributeNames.Href) return false;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute current = node.Attributes[index];
            if (current.NamespaceUri.Length == 0 && current.LocalName == SvgAttributeNames.Href) return true;
            index = index + 1;
        }
        return false;
    }

    static bool DefaultAttribute(SvgNode node, SvgAttribute attribute, SvgElementCatalog elements, SvgPropertyCatalog properties)
    {
        if (attribute.AttributeKind == SvgAttributeKind.NamespaceDeclaration) return false;
        if (attribute.LocalName == SvgAttributeNames.ViewBox || AccessibilityAttribute(attribute)) return false;
        SvgElementDefinition? element = elements.Find(node.ElementKind);
        if (element == null) return false;
        SvgElementAttributeDefinition? definition = element.FindAttribute(attribute.NamespaceUri, attribute.LocalName);
        if (definition == null || definition.Required || definition.Global || definition.DefaultKind != SvgAttributeDefaultKind.Literal) return false;
        if (!EquivalentDefault(attribute.Value, definition.DefaultValue)) return false;
        if (!definition.Presentation) return true;
        SvgPropertySupportEntry? property = properties.FindPresentationAttribute(attribute.LocalName);
        return property != null && !property.Inherited && property.InitialValue == definition.DefaultValue;
    }

    static bool EquivalentDefault(string value, string defaultValue)
    {
        string authored = value.Trim();
        if (string.Equals(authored, defaultValue, System.StringComparison.OrdinalIgnoreCase)) return true;
        SvgNumberParseResult authoredNumber = SvgNumberParser.ParseNumber(authored, 128);
        SvgNumberParseResult defaultNumber = SvgNumberParser.ParseNumber(defaultValue, 128);
        return authoredNumber.Success && defaultNumber.Success && authoredNumber.Value == defaultNumber.Value;
    }

    static bool EmptyCandidate(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element) return false;
        SvgElementKind kind = node.ElementKind;
        return kind == SvgElementKind.G || kind == SvgElementKind.Defs || kind == SvgElementKind.Switch || kind == SvgElementKind.Symbol || kind == SvgElementKind.ClipPath || kind == SvgElementKind.Marker || kind == SvgElementKind.Mask || kind == SvgElementKind.Pattern || kind == SvgElementKind.Filter || kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Style || kind == SvgElementKind.Metadata || kind == SvgElementKind.A;
    }

    static bool MeaningfulContent(SvgNode node)
    {
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[attributeIndex];
            if ((attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink) || attribute.LocalName == SvgAttributeNames.ViewBox) && attribute.Value.Trim().Length != 0) return true;
            attributeIndex = attributeIndex + 1;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Element) return true;
            if ((child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) && child.Value.Trim().Length != 0) return true;
            if (child.Kind == SvgNodeKind.ProcessingInstruction) return true;
            index = index + 1;
        }
        return false;
    }

    static bool InvisibleCandidate(SvgNode node, SvgElementCatalog elements)
    {
        if (node.Kind != SvgNodeKind.Element || node.ElementKind == SvgElementKind.Svg) return false;
        SvgElementDefinition? definition = elements.Find(node.ElementKind);
        if (definition == null) return false;
        if (definition.RenderMode != SvgElementRenderMode.Direct && definition.RenderMode != SvgElementRenderMode.Container && definition.RenderMode != SvgElementRenderMode.Text) return false;
        string display = AttributeValue(node, "display");
        if (string.Equals(display.Trim(), "none", System.StringComparison.OrdinalIgnoreCase)) return true;
        string visibility = AttributeValue(node, "visibility");
        if ((string.Equals(visibility.Trim(), "hidden", System.StringComparison.OrdinalIgnoreCase) || string.Equals(visibility.Trim(), "collapse", System.StringComparison.OrdinalIgnoreCase)) && !HasElementChild(node)) return true;
        if (ZeroNumber(AttributeValue(node, "opacity"))) return true;
        string style = AttributeValue(node, SvgAttributeNames.Style);
        if (style.Length != 0 && StyleHides(style, !HasElementChild(node))) return true;
        if (node.ElementKind == SvgElementKind.Path && AttributeValue(node, "d").Trim().Length == 0) return true;
        if ((node.ElementKind == SvgElementKind.Polyline || node.ElementKind == SvgElementKind.Polygon) && AttributeValue(node, "points").Trim().Length == 0) return true;
        return false;
    }

    static bool StyleHides(string value, bool leaf)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(value, new SvgCssLimits());
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            if (declaration.IsValid && declaration.Name == "display" && string.Equals(declaration.ValueText.Trim(), "none", System.StringComparison.OrdinalIgnoreCase)) return true;
            if (leaf && declaration.IsValid && declaration.Name == "visibility" && (string.Equals(declaration.ValueText.Trim(), "hidden", System.StringComparison.OrdinalIgnoreCase) || string.Equals(declaration.ValueText.Trim(), "collapse", System.StringComparison.OrdinalIgnoreCase))) return true;
            if (declaration.IsValid && declaration.Name == "opacity" && ZeroNumber(declaration.ValueText)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ProtectedSemantics(SvgNode node)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(node);
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (current.Kind == SvgNodeKind.Element)
            {
                if (PreservedElementKind(current.ElementKind)) return true;
                int attributeIndex = 0;
                while (attributeIndex < current.Attributes.Count)
                {
                    SvgAttribute attribute = current.Attributes[attributeIndex];
                    if (AccessibilityAttribute(attribute) || attribute.NamespaceUri.Length == 0 && attribute.LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase)) return true;
                    if (current.ElementKind == SvgElementKind.A && attribute.LocalName == SvgAttributeNames.Href) return true;
                    attributeIndex = attributeIndex + 1;
                }
            }
            int childIndex = current.Children.Count - 1;
            while (childIndex >= 0)
            {
                pending.Add(current.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return false;
    }

    static bool AccessibilityAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        return attribute.LocalName == "role" || attribute.LocalName == SvgAttributeNames.TabIndex || attribute.LocalName.StartsWith("aria-", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool PreservedElementKind(SvgElementKind kind)
    {
        return kind == SvgElementKind.Unknown || kind == SvgElementKind.Title || kind == SvgElementKind.Desc || kind == SvgElementKind.Metadata || kind == SvgElementKind.Script || kind == SvgElementKind.ForeignObject || kind == SvgElementKind.Audio || kind == SvgElementKind.Video || kind == SvgElementKind.Iframe || kind == SvgElementKind.Canvas;
    }

    static bool ContainsAnimation(SvgNode node)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(node);
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (current.Kind == SvgNodeKind.Element && (current.ElementKind == SvgElementKind.Animate || current.ElementKind == SvgElementKind.Set || current.ElementKind == SvgElementKind.AnimateMotion || current.ElementKind == SvgElementKind.AnimateTransform || current.ElementKind == SvgElementKind.Discard)) return true;
            int index = current.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(current.Children[index]);
                index = index - 1;
            }
        }
        return false;
    }

    static bool HasElementChild(SvgNode node)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            if (node.Children[index].Kind == SvgNodeKind.Element) return true;
            index = index + 1;
        }
        return false;
    }

    static bool HasStyleSheet(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style && new SvgNodeApi().GetTextContent(node).Trim().Length != 0) return true;
            if (node.Kind == SvgNodeKind.Element && node.LocalName == "link" && AttributeValue(node, "rel").IndexOf("stylesheet", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (node.Kind == SvgNodeKind.ProcessingInstruction && string.Equals(node.LocalName, "xml-stylesheet", System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }

    static System.Collections.Generic.HashSet<string> ReferencedIds(SvgDocument document)
    {
        System.Collections.Generic.HashSet<string> result = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            SvgNode node = nodes[nodeIndex];
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                SvgAttribute attribute = node.Attributes[attributeIndex];
                if (attribute.NamespaceUri.Length != 0 || attribute.LocalName != SvgAttributeNames.Id)
                {
                    AddHashReferences(attribute.Value, result);
                    if (IdReferenceAttribute(attribute)) AddWords(attribute.Value, result);
                    if (attribute.LocalName == "begin" || attribute.LocalName == "end") AddTimingReferences(attribute.Value, document, result);
                }
                attributeIndex = attributeIndex + 1;
            }
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style) AddHashReferences(new SvgNodeApi().GetTextContent(node), result);
            nodeIndex = nodeIndex + 1;
        }
        return result;
    }

    static void AddHashReferences(string value, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '#')
            {
                index = index + 1;
                continue;
            }
            int start = index + 1;
            index = start;
            while (index < value.Length && ReferenceCharacter(value[index])) index = index + 1;
            if (index > start) result.Add(value.Substring(start, index - start));
        }
    }

    static bool ReferenceCharacter(char value)
    {
        return !char.IsWhiteSpace(value) && value != ')' && value != '(' && value != '\'' && value != '"' && value != ';' && value != ',';
    }

    static bool IdReferenceAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        string name = attribute.LocalName;
        return name == "aria-labelledby" || name == "aria-describedby" || name == "aria-controls" || name == "aria-owns" || name == "aria-details" || name == "aria-errormessage";
    }

    static void AddWords(string value, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (index > start) result.Add(value.Substring(start, index - start));
        }
    }

    static void AddTimingReferences(string value, SvgDocument document, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            int start = index;
            while (index < value.Length && value[index] != '.' && value[index] != ';') index = index + 1;
            if (index < value.Length && value[index] == '.')
            {
                string id = value.Substring(start, index - start).Trim();
                if (id.Length != 0 && document.Index.GetById(id) != null) result.Add(id);
            }
            while (index < value.Length && value[index] != ';') index = index + 1;
            if (index < value.Length) index = index + 1;
        }
    }

    static bool Referenced(SvgNode node, System.Collections.Generic.HashSet<string> references)
    {
        string id = AttributeValue(node, SvgAttributeNames.Id);
        return id.Length != 0 && references.Contains(id);
    }

    static bool SubtreeReferenced(SvgNode node, System.Collections.Generic.HashSet<string> references)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(node);
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (Referenced(current, references)) return true;
            int index = current.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(current.Children[index]);
                index = index - 1;
            }
        }
        return false;
    }

    static bool ZeroNumber(string value)
    {
        if (value == null || value.Length == 0) return false;
        SvgNumberParseResult parsed = SvgNumberParser.ParseCssNumber(value, 128);
        return parsed.Success && parsed.Value == 0;
    }

    static string AttributeValue(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    static string NamespacePrefix(SvgAttribute attribute)
    {
        return attribute.QualifiedName == "xmlns" ? "" : attribute.LocalName;
    }

    static bool InheritedBinding(SvgNode node, string prefix, string value)
    {
        SvgNode? current = node.Parent;
        while (current != null)
        {
            if (current.Kind == SvgNodeKind.Element)
            {
                int index = 0;
                while (index < current.Attributes.Count)
                {
                    SvgAttribute attribute = current.Attributes[index];
                    if (attribute.NamespaceUri == SvgNamespaces.Xmlns && NamespacePrefix(attribute) == prefix) return attribute.Value == value;
                    index = index + 1;
                }
            }
            current = current.Parent;
        }
        return false;
    }

    static bool BindingUsed(SvgNode owner, SvgAttribute declaration, string prefix, string value)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(owner);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (node.Kind == SvgNodeKind.Element)
            {
                if (ElementUsesBinding(node, prefix, value)) return true;
                int attributeIndex = 0;
                while (attributeIndex < node.Attributes.Count)
                {
                    SvgAttribute attribute = node.Attributes[attributeIndex];
                    if (!object.ReferenceEquals(attribute, declaration) && attribute.NamespaceUri != SvgNamespaces.Xmlns && attribute.Prefix == prefix && prefix.Length != 0 && attribute.NamespaceUri == value) return true;
                    if (attribute.NamespaceUri != SvgNamespaces.Xmlns && prefix.Length != 0 && ValueUsesPrefix(attribute.Value, prefix)) return true;
                    attributeIndex = attributeIndex + 1;
                }
            }
            if ((node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData) && prefix.Length != 0 && ValueUsesPrefix(node.Value, prefix)) return true;
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                SvgNode child = node.Children[childIndex];
                if (!ShadowsBinding(child, prefix)) pending.Add(child);
                childIndex = childIndex - 1;
            }
        }
        return false;
    }

    static bool ValueUsesPrefix(string value, string prefix)
    {
        return value.IndexOf(prefix + ":", System.StringComparison.Ordinal) >= 0;
    }

    static bool ElementUsesBinding(SvgNode node, string prefix, string value)
    {
        if (prefix.Length == 0) return node.Prefix.Length == 0 && node.NamespaceUri == value;
        return node.Prefix == prefix && node.NamespaceUri == value;
    }

    static bool ShadowsBinding(SvgNode node, string prefix)
    {
        if (node.Kind != SvgNodeKind.Element) return false;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == SvgNamespaces.Xmlns && NamespacePrefix(attribute) == prefix) return true;
            index = index + 1;
        }
        return false;
    }

    static void RemoveNode(SvgProcessContext context, SvgNode node)
    {
        SvgMutationResult mutation = context.Mutations.RemoveChild(node.Parent, node);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void RemoveAttribute(SvgProcessContext context, SvgNode node, SvgAttribute attribute)
    {
        SvgMutationResult mutation = context.Mutations.RemoveAttribute(node, attribute);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(document.DocumentNode);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
}
