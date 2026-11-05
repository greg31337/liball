namespace svg;

/// <summary>Builds editable SVG documents through data-driven fluent operations.</summary>
/// <remarks>The builder uses <see cref="SvgElementKind"/> and the element catalog instead of element subclasses or generic builder types.</remarks>
public class SvgBuilder
{
    SvgElementCatalog catalog;
    SvgLegacyCatalog legacyCatalog;
    SvgDocumentApi documents;
    SvgNodeApi nodes;

    /// <summary>The document currently being built.</summary>
    public SvgDocument? Document;
    /// <summary>The document's root element.</summary>
    public SvgNode? Root;
    /// <summary>The element that receives attributes and new child content.</summary>
    public SvgNode? Current;
    /// <summary>The node most recently created, selected, or appended.</summary>
    public SvgNode? LastNode;
    /// <summary>The attribute most recently created or replaced.</summary>
    public SvgAttribute? LastAttribute;
    /// <summary>The first sticky builder failure, or <c>None</c>.</summary>
    public SvgBuilderError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string? Message;
    /// <summary>The underlying document mutation failure, or <c>None</c>.</summary>
    public SvgMutationError MutationError;

    /// <summary>Creates a new clean SVG-rooted document builder.</summary>
    public SvgBuilder()
    {
        catalog = new SvgCatalogFactory().CreateElementCatalog();
        legacyCatalog = new SvgCatalogFactory().CreateLegacyCatalog();
        documents = new SvgDocumentApi();
        nodes = new SvgNodeApi();
        Initialize(documents.CreateDocument(), true);
    }

    /// <summary>Creates a builder positioned at an existing document element.</summary>
    /// <param name="document">The editable document, which receives an SVG root when empty.</param>
    /// <returns>A new builder positioned at the document.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    internal static SvgBuilder Create(SvgDocument? document)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        SvgBuilder result = new SvgBuilder();
        result.Initialize(document, false);
        return result;
    }

    /// <summary>Replaces the current output with a new clean SVG-rooted document.</summary>
    /// <returns>This builder.</returns>
    internal SvgBuilder NewDocument()
    {
        Initialize(documents.CreateDocument(), true);
        return this;
    }

    /// <summary>Positions the builder at another editable document.</summary>
    /// <param name="document">The document, which receives an SVG root when empty.</param>
    /// <returns>This builder, or this builder in an error state for a missing document.</returns>
    internal SvgBuilder UseDocument(SvgDocument? document)
    {
        if (document == null) return Fail(SvgBuilderError.InvalidArgument, "A document is required.");
        Initialize(document, false);
        return this;
    }

    /// <summary>Returns the editable output document.</summary>
    /// <returns>The current document, including any content built before an error.</returns>
    internal SvgDocument? Build()
    {
        return Document;
    }

    /// <summary>Tests whether no builder operation has failed.</summary>
    /// <returns><c>true</c> when the builder has no sticky error.</returns>
    internal bool Succeeded()
    {
        return Error == SvgBuilderError.None;
    }

    /// <summary>Clears the sticky error without changing the document or current position.</summary>
    /// <returns>This builder.</returns>
    internal SvgBuilder ClearError()
    {
        Error = SvgBuilderError.None;
        Message = "";
        MutationError = SvgMutationError.None;
        return this;
    }

    /// <summary>Appends and enters one recognized current or legacy SVG element.</summary>
    /// <param name="kind">A recognized element kind from the default catalogs.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder BeginElement(SvgElementKind kind)
    {
        if (Error != SvgBuilderError.None) return this;
        string? localName = ElementName(kind);
        if (localName == null) return Fail(SvgBuilderError.UnsupportedElement, "The requested element is not in a recognized SVG element catalog.");
        SvgNode element = documents.CreateElement(localName, "", localName, SvgNamespaces.Svg);
        if (!AppendToCurrent(element)) return this;
        Current = element;
        LastNode = element;
        return this;
    }

    /// <summary>Appends and enters one namespace-aware element.</summary>
    /// <param name="qualifiedName">The exact qualified element name.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local element name.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder BeginElementWithNamespace(string qualifiedName, string prefix, string localName, string namespaceUri)
    {
        if (Error != SvgBuilderError.None) return this;
        if (!ValidExpandedName(qualifiedName, prefix, localName, namespaceUri, false)) return Fail(SvgBuilderError.InvalidArgument, "The element name and namespace are inconsistent.");
        SvgNode element = documents.CreateElement(qualifiedName, prefix, localName, namespaceUri);
        if (!AppendToCurrent(element)) return this;
        Current = element;
        LastNode = element;
        return this;
    }

    /// <summary>Appends one recognized current or legacy SVG element without changing the current position.</summary>
    /// <param name="kind">A recognized element kind from the default catalogs.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder AddElement(SvgElementKind kind)
    {
        if (Error != SvgBuilderError.None) return this;
        string? localName = ElementName(kind);
        if (localName == null) return Fail(SvgBuilderError.UnsupportedElement, "The requested element is not in a recognized SVG element catalog.");
        SvgNode element = documents.CreateElement(localName, "", localName, SvgNamespaces.Svg);
        if (AppendToCurrent(element)) LastNode = element;
        return this;
    }

    /// <summary>Appends one namespace-aware element without changing the current position.</summary>
    /// <param name="qualifiedName">The exact qualified element name.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local element name.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder AddElementWithNamespace(string qualifiedName, string prefix, string localName, string namespaceUri)
    {
        if (Error != SvgBuilderError.None) return this;
        if (!ValidExpandedName(qualifiedName, prefix, localName, namespaceUri, false)) return Fail(SvgBuilderError.InvalidArgument, "The element name and namespace are inconsistent.");
        SvgNode element = documents.CreateElement(qualifiedName, prefix, localName, namespaceUri);
        if (AppendToCurrent(element)) LastNode = element;
        return this;
    }

    /// <summary>Leaves the current element and selects its parent element.</summary>
    /// <returns>This builder.</returns>
    internal SvgBuilder EndElement()
    {
        if (Error != SvgBuilderError.None) return this;
        if (!ValidCurrent() || object.ReferenceEquals(Current, Root) || (Current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parent == null || (Current.Parent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Kind != SvgNodeKind.Element) return Fail(SvgBuilderError.InvalidState, "The root element cannot be exited.");
        LastNode = Current;
        Current = Current.Parent;
        return this;
    }

    /// <summary>Selects an element in the current document as the new insertion position.</summary>
    /// <param name="element">An attached element owned by the current document.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder Select(SvgNode? element)
    {
        if (Error != SvgBuilderError.None) return this;
        if (element == null || element.Kind != SvgNodeKind.Element || !object.ReferenceEquals(element.OwnerDocument, Document)) return Fail(SvgBuilderError.InvalidArgument, "The selected node must be an element in the current document.");
        Current = element;
        LastNode = element;
        return this;
    }

    /// <summary>Appends a detached non-document node at the current position.</summary>
    /// <param name="node">The detached node to append.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder Append(SvgNode? node)
    {
        if (Error != SvgBuilderError.None) return this;
        if (node == null || node.Kind == SvgNodeKind.Document) return Fail(SvgBuilderError.InvalidArgument, "A detached non-document node is required.");
        if (AppendToCurrent(node)) LastNode = node;
        return this;
    }

    /// <summary>Creates or replaces one unqualified attribute on the current element.</summary>
    /// <param name="localName">The unqualified attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder SetAttribute(string localName, string value)
    {
        return SetNamespacedAttribute(localName, "", localName, "", value);
    }

    /// <summary>Creates or replaces one namespace-aware attribute on the current element.</summary>
    /// <param name="qualifiedName">The exact qualified attribute name.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local attribute name.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder SetNamespacedAttribute(string qualifiedName, string prefix, string localName, string namespaceUri, string? value)
    {
        if (Error != SvgBuilderError.None) return this;
        if (value == null || !ValidExpandedName(qualifiedName, prefix, localName, namespaceUri, true)) return Fail(SvgBuilderError.InvalidArgument, "The attribute name, namespace, and value are invalid.");
        if (!ValidCurrent()) return Fail(SvgBuilderError.InvalidState, "An attached current element is required.");
        SvgAttribute? current = FindAttribute(Current, namespaceUri, localName);
        if (current != null && current.QualifiedName == qualifiedName && current.Prefix == prefix && current.Value == value)
        {
            LastAttribute = current;
            return this;
        }
        SvgAttribute replacement = documents.CreateAttribute(qualifiedName, prefix, localName, namespaceUri, value);
        SvgMutationResult mutation = current == null ? nodes.AppendAttribute(Current, replacement) : nodes.ReplaceAttribute(Current, current, replacement);
        if (!Accept(mutation)) return this;
        LastAttribute = replacement;
        LastNode = Current;
        return this;
    }

    /// <summary>Removes one unqualified attribute from the current element when present.</summary>
    /// <param name="localName">The unqualified attribute name.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder RemoveAttribute(string localName)
    {
        return RemoveNamespacedAttribute("", localName);
    }

    /// <summary>Removes one expanded-name attribute from the current element when present.</summary>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="localName">The local attribute name.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder RemoveNamespacedAttribute(string? namespaceUri, string localName)
    {
        if (Error != SvgBuilderError.None) return this;
        if (namespaceUri == null || !ValidNamePart(localName)) return Fail(SvgBuilderError.InvalidArgument, "A namespace URI and local attribute name are required.");
        if (!ValidCurrent()) return Fail(SvgBuilderError.InvalidState, "An attached current element is required.");
        SvgAttribute? attribute = FindAttribute(Current, namespaceUri, localName);
        if (attribute == null)
        {
            LastAttribute = null;
            return this;
        }
        if (!Accept(nodes.RemoveAttribute(Current, attribute))) return this;
        LastAttribute = null;
        LastNode = Current;
        return this;
    }

    /// <summary>Replaces the current element's complete inline style declaration list.</summary>
    /// <param name="value">A valid CSS style-attribute value, or an empty string to remove the attribute.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder SetStyleText(string? value)
    {
        if (Error != SvgBuilderError.None) return this;
        if (value == null) return Fail(SvgBuilderError.InvalidArgument, "Style text is required.");
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(value, new SvgCssLimits());
        if (!ValidDeclarations(parsed)) return Fail(SvgBuilderError.InvalidStyle, "The inline style declaration list is malformed.");
        if (value.Length == 0) return RemoveAttribute(SvgAttributeNames.Style);
        return SetAttribute(SvgAttributeNames.Style, value);
    }

    /// <summary>Creates or replaces one non-important inline style declaration.</summary>
    /// <param name="name">The CSS property or custom-property name.</param>
    /// <param name="value">The CSS value text.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder SetStyle(string name, string value)
    {
        return SetStyleWithPriority(name, value, false);
    }

    /// <summary>Creates or replaces one inline style declaration.</summary>
    /// <param name="name">The CSS property or custom-property name.</param>
    /// <param name="value">The CSS value text.</param>
    /// <param name="important">Whether to append an important suffix.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder SetStyleWithPriority(string name, string value, bool important)
    {
        if (Error != SvgBuilderError.None) return this;
        if (!ValidCurrent()) return Fail(SvgBuilderError.InvalidState, "An attached current element is required.");
        SvgBuilderStyleResult built = BuildStyle(name, value, important);
        if (!built.Success) return Fail(SvgBuilderError.InvalidStyle, "The inline style declaration is malformed.");
        return SetAttribute(SvgAttributeNames.Style, built.Text);
    }

    /// <summary>Removes every inline declaration for one property.</summary>
    /// <param name="name">The CSS property or custom-property name.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder RemoveStyle(string? name)
    {
        if (Error != SvgBuilderError.None) return this;
        if (name == null || name.Length == 0) return Fail(SvgBuilderError.InvalidArgument, "A CSS property name is required.");
        if (!ValidCurrent()) return Fail(SvgBuilderError.InvalidState, "An attached current element is required.");
        SvgAttribute? style = FindAttribute(Current, "", SvgAttributeNames.Style);
        if (style == null) return this;
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(style.Value, new SvgCssLimits());
        if (!ValidDeclarations(parsed)) return Fail(SvgBuilderError.InvalidStyle, "The existing inline style declaration list is malformed.");
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            SvgCssDeclaration declaration = parsed.Declarations[index];
            if (!SameProperty(declaration.Name, name)) AppendDeclaration(text, declaration);
            index = index + 1;
        }
        if (text.Length == 0) return RemoveAttribute(SvgAttributeNames.Style);
        return SetAttribute(SvgAttributeNames.Style, text.ToString());
    }

    /// <summary>Appends a validated SVG <c>style</c> element without changing the current position.</summary>
    /// <param name="css">The stylesheet text.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder AddStyleSheet(string? css)
    {
        if (Error != SvgBuilderError.None) return this;
        if (css == null) return Fail(SvgBuilderError.InvalidArgument, "Stylesheet text is required.");
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(css, new SvgCssLimits());
        if (!ValidStyleSheet(parsed)) return Fail(SvgBuilderError.InvalidStyle, "The stylesheet is malformed.");
        SvgElementDefinition? definition = catalog.Find(SvgElementKind.Style);
        SvgNode style = documents.CreateElement((definition ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LocalName, "", definition.LocalName, SvgNamespaces.Svg);
        if (!AppendToCurrent(style)) return this;
        if (css.Length != 0)
        {
            SvgNode text = documents.CreateText(css);
            if (!Accept(nodes.AppendChild(style, text))) return this;
        }
        LastNode = style;
        return this;
    }

    /// <summary>Appends a text node to the current element.</summary>
    /// <param name="value">The exact text value.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder AddText(string? value)
    {
        if (Error != SvgBuilderError.None) return this;
        if (value == null) return Fail(SvgBuilderError.InvalidArgument, "Text is required.");
        SvgNode text = documents.CreateText(value);
        if (AppendToCurrent(text)) LastNode = text;
        return this;
    }

    /// <summary>Replaces the current element's children with one text node, or no children for empty text.</summary>
    /// <param name="value">The exact replacement text.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder SetText(string? value)
    {
        if (Error != SvgBuilderError.None) return this;
        if (value == null) return Fail(SvgBuilderError.InvalidArgument, "Text is required.");
        if (!ValidCurrent()) return Fail(SvgBuilderError.InvalidState, "An attached current element is required.");
        if (!Accept(nodes.SetTextContent(Current, value))) return this;
        LastNode = (Current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count == 0 ? Current : Current.Children[0];
        return this;
    }

    /// <summary>Appends a CDATA node to the current element.</summary>
    /// <param name="value">The exact CDATA value.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder AddCData(string? value)
    {
        if (Error != SvgBuilderError.None) return this;
        if (value == null) return Fail(SvgBuilderError.InvalidArgument, "CDATA text is required.");
        SvgNode node = documents.CreateCData(value);
        if (AppendToCurrent(node)) LastNode = node;
        return this;
    }

    /// <summary>Appends a comment node to the current element.</summary>
    /// <param name="value">The exact comment value.</param>
    /// <returns>This builder.</returns>
    internal SvgBuilder AddComment(string? value)
    {
        if (Error != SvgBuilderError.None) return this;
        if (value == null) return Fail(SvgBuilderError.InvalidArgument, "Comment text is required.");
        SvgNode node = documents.CreateComment(value);
        if (AppendToCurrent(node)) LastNode = node;
        return this;
    }

    internal void Initialize(SvgDocument document, bool newDocument)
    {
        Document = document;
        Root = document.DocumentElement;
        Current = Root;
        LastNode = Root;
        LastAttribute = null;
        Error = SvgBuilderError.None;
        Message = "";
        MutationError = SvgMutationError.None;
        if (Root == null)
        {
            Root = documents.CreateElement(SvgElementNames.Svg, "", SvgElementNames.Svg, SvgNamespaces.Svg);
            SvgAttribute declaration = documents.CreateAttribute("xmlns", "", "xmlns", SvgNamespaces.Xmlns, SvgNamespaces.Svg);
            if (newDocument)
            {
                document.AttachInitialChild(document.DocumentNode, Root);
                document.AttachInitialAttribute(Root, declaration);
            }
            else
            {
                SvgMutationResult attributeMutation = nodes.AppendAttribute(Root, declaration);
                if (!Accept(attributeMutation))
                {
                    Root = null;
                    Current = null;
                    LastNode = null;
                    return;
                }
                SvgMutationResult rootMutation = nodes.AppendChild(document.DocumentNode, Root);
                if (!Accept(rootMutation))
                {
                    Root = null;
                    Current = null;
                    LastNode = null;
                    return;
                }
            }
        }
        Current = Root;
        LastNode = Root;
    }

    internal string? ElementName(SvgElementKind kind)
    {
        SvgElementDefinition? current = catalog.Find(kind);
        if (current != null) return current.LocalName;
        SvgLegacyFeatureDefinition? legacy = legacyCatalog.FindElement(kind);
        return legacy == null ? null : legacy.LocalName;
    }

    internal bool AppendToCurrent(SvgNode node)
    {
        if (!ValidCurrent())
        {
            Fail(SvgBuilderError.InvalidState, "An attached current element is required.");
            return false;
        }
        return Accept(nodes.AppendChild(Current, node));
    }

    internal bool ValidCurrent()
    {
        return Current != null && Current.Kind == SvgNodeKind.Element && object.ReferenceEquals(Current.OwnerDocument, Document);
    }

    internal bool Accept(SvgMutationResult result)
    {
        if (result.Success) return true;
        MutationError = result.Error;
        Fail(SvgBuilderError.MutationFailed, result.Message);
        return false;
    }

    internal SvgBuilder Fail(SvgBuilderError error, string message)
    {
        if (Error == SvgBuilderError.None)
        {
            Error = error;
            Message = message;
        }
        return this;
    }

    internal SvgBuilderStyleResult BuildStyle(string name, string value, bool important)
    {
        if (name == null || value == null || name.Length == 0 || value.Length == 0) return new SvgBuilderStyleResult(false, "");
        string candidateText = name + ":" + value + (important ? "!important" : "") + ";";
        SvgCssParseResult candidate = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(candidateText, new SvgCssLimits());
        if (!ValidDeclarations(candidate) || candidate.Declarations.Count != 1) return new SvgBuilderStyleResult(false, "");
        SvgCssDeclaration added = candidate.Declarations[0];
        if (!SameProperty(added.Name, name) || added.Important != important) return new SvgBuilderStyleResult(false, "");
        SvgAttribute? style = FindAttribute(Current, "", SvgAttributeNames.Style);
        SvgCssParseResult existing = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(style == null ? "" : style.Value, new SvgCssLimits());
        if (!ValidDeclarations(existing)) return new SvgBuilderStyleResult(false, "");
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < existing.Declarations.Count)
        {
            SvgCssDeclaration declaration = existing.Declarations[index];
            if (!SameProperty(declaration.Name, added.Name)) AppendDeclaration(text, declaration);
            index = index + 1;
        }
        AppendDeclaration(text, added);
        return new SvgBuilderStyleResult(true, text.ToString());
    }

    internal static void AppendDeclaration(System.Text.StringBuilder text, SvgCssDeclaration declaration)
    {
        text.Append(declaration.Name);
        text.Append(':');
        text.Append(declaration.ValueText.Trim());
        if (declaration.Important) text.Append("!important");
        text.Append(';');
    }

    internal static bool ValidDeclarations(SvgCssParseResult? parsed)
    {
        if (parsed == null || !parsed.Success || parsed.UnknownSyntax.Count != 0) return false;
        int index = 0;
        while (index < parsed.Declarations.Count)
        {
            if (!parsed.Declarations[index].IsValid) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool ValidStyleSheet(SvgCssParseResult? parsed)
    {
        if (parsed == null || !parsed.Success) return false;
        int index = 0;
        while (index < parsed.UnknownSyntax.Count)
        {
            if (!parsed.UnknownSyntax[index].IsValid) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool SameProperty(string first, string second)
    {
        if (first.StartsWith("--", System.StringComparison.Ordinal) || second.StartsWith("--", System.StringComparison.Ordinal)) return first == second;
        return string.Equals(first, second, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static SvgAttribute? FindAttribute(SvgNode? element, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < (element ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }

    internal static bool ValidExpandedName(string? qualifiedName, string? prefix, string? localName, string? namespaceUri, bool attribute)
    {
        if (qualifiedName == null || prefix == null || localName == null || namespaceUri == null || !ValidNamePart(localName)) return false;
        if (prefix.Length != 0 && !ValidNamePart(prefix)) return false;
        string expected = prefix.Length == 0 ? localName : prefix + ":" + localName;
        if (qualifiedName != expected || prefix.Length != 0 && namespaceUri.Length == 0) return false;
        if (prefix == "xml" && namespaceUri != SvgNamespaces.Xml || namespaceUri == SvgNamespaces.Xml && prefix != "xml") return false;
        if (!attribute && (prefix == "xmlns" || namespaceUri == SvgNamespaces.Xmlns)) return false;
        if (attribute)
        {
            bool declaration = namespaceUri == SvgNamespaces.Xmlns;
            bool declarationName = prefix == "xmlns" || prefix.Length == 0 && localName == "xmlns";
            if (declaration != declarationName) return false;
        }
        return true;
    }

    internal static bool ValidNamePart(string value)
    {
        return value != null && value.Length != 0 && System.Xml.XmlConvert.EncodeLocalName(value) == value;
    }
}
