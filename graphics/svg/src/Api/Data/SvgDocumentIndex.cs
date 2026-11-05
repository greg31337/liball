namespace svg;

/// <summary>Provides deterministic version-aware indexes over one editable document tree.</summary>
public class SvgDocumentIndex
{
    SvgDocument document;
    System.Collections.Generic.Dictionary<string, SvgNode> ids;
    System.Collections.Generic.Dictionary<string, SvgAttribute> idAttributes;
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> classes;
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> qualifiedNames;
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> namespaces;
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>> expandedNames;
    System.Collections.Generic.List<SvgNode> sourceNodes;
    System.Collections.Generic.List<SvgDiagnostic> duplicateIdDiagnostics;

    /// <summary>The document version represented by the current cache, or <c>-1</c> while invalid.</summary>
    public long IndexedVersion;

    /// <summary>Creates an invalid index owned by a document.</summary>
    /// <param name="owner">The document whose connected tree is indexed.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>owner</c> is <c>null</c>.</exception>
    internal SvgDocumentIndex(SvgDocument owner)
    {
        if (owner == null) throw new System.ArgumentNullException("owner");
        document = owner;
        ids = new System.Collections.Generic.Dictionary<string, SvgNode>(System.StringComparer.Ordinal);
        idAttributes = new System.Collections.Generic.Dictionary<string, SvgAttribute>(System.StringComparer.Ordinal);
        classes = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>(System.StringComparer.Ordinal);
        qualifiedNames = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>(System.StringComparer.Ordinal);
        namespaces = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>(System.StringComparer.Ordinal);
        expandedNames = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>>(System.StringComparer.Ordinal);
        sourceNodes = new System.Collections.Generic.List<SvgNode>();
        duplicateIdDiagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        IndexedVersion = -1;
    }

    /// <summary>Ensures every index represents the document's current version.</summary>
    /// <returns>The indexed document version.</returns>
    internal long Refresh()
    {
        if (IndexedVersion != document.Version) Rebuild();
        return IndexedVersion;
    }

    /// <summary>Returns the first element in document order with an exact nonempty ID.</summary>
    /// <param name="id">The case-sensitive ID.</param>
    /// <returns>The first matching element, or <c>null</c>.</returns>
    internal SvgNode? GetById(string? id)
    {
        Refresh();
        if (id == null || id.Length == 0 || !ids.ContainsKey(id)) return null;
        return ids[id];
    }

    /// <summary>Returns elements containing an exact class token in document order.</summary>
    /// <param name="className">The case-sensitive non-whitespace class token.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> GetByClass(string? className)
    {
        Refresh();
        if (className == null || className.Length == 0 || !classes.ContainsKey(className)) return new System.Collections.Generic.List<SvgNode>();
        return CopyNodes(classes[className]);
    }

    /// <summary>Returns elements with an exact case-sensitive qualified name in document order.</summary>
    /// <param name="qualifiedName">The qualified XML name.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> GetByQualifiedName(string? qualifiedName)
    {
        Refresh();
        if (qualifiedName == null || !qualifiedNames.ContainsKey(qualifiedName)) return new System.Collections.Generic.List<SvgNode>();
        return CopyNodes(qualifiedNames[qualifiedName]);
    }

    /// <summary>Returns elements in an exact namespace URI in document order.</summary>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> GetByNamespace(string? namespaceUri)
    {
        Refresh();
        if (namespaceUri == null || !namespaces.ContainsKey(namespaceUri)) return new System.Collections.Generic.List<SvgNode>();
        return CopyNodes(namespaces[namespaceUri]);
    }

    /// <summary>Returns elements with an exact namespace URI and local name in document order.</summary>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> GetByExpandedName(string? namespaceUri, string? localName)
    {
        Refresh();
        if (namespaceUri == null || localName == null || !expandedNames.ContainsKey(namespaceUri)) return new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> localNames = expandedNames[namespaceUri];
        if (!localNames.ContainsKey(localName)) return new System.Collections.Generic.List<SvgNode>();
        return CopyNodes(localNames[localName]);
    }

    /// <summary>Returns source-located nodes in deterministic resource and source order.</summary>
    /// <returns>An independently allocated snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> GetSourceOrderedNodes()
    {
        Refresh();
        return CopyNodes(sourceNodes);
    }

    /// <summary>Returns nodes whose retained source span exactly matches a span.</summary>
    /// <param name="span">The exact source span.</param>
    /// <returns>An independently allocated source-ordered snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> GetBySourceSpan(SvgSourceSpan? span)
    {
        Refresh();
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (span == null) return result;
        int index = 0;
        while (index < sourceNodes.Count)
        {
            SvgSourceSpan? candidate = sourceNodes[index].SourceSpan;
            if (SpansEqual(candidate, span)) result.Add(sourceNodes[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Returns nodes whose retained source spans contain a supplied span.</summary>
    /// <param name="span">The contained source span.</param>
    /// <returns>An independently allocated source-ordered snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> FindContainingSourceSpan(SvgSourceSpan? span)
    {
        Refresh();
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (span == null) return result;
        int index = 0;
        while (index < sourceNodes.Count)
        {
            if ((sourceNodes[index].SourceSpan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ContainsSpan(span)) result.Add(sourceNodes[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Returns nodes whose nonempty retained source spans overlap a supplied span.</summary>
    /// <param name="span">The candidate overlapping span.</param>
    /// <returns>An independently allocated source-ordered snapshot.</returns>
    internal System.Collections.Generic.List<SvgNode> FindOverlappingSourceSpan(SvgSourceSpan? span)
    {
        Refresh();
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        if (span == null) return result;
        int index = 0;
        while (index < sourceNodes.Count)
        {
            if ((sourceNodes[index].SourceSpan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Overlaps(span)) result.Add(sourceNodes[index]);
            index = index + 1;
        }
        return result;
    }

    /// <summary>Returns duplicate-ID warnings produced by the current index build.</summary>
    /// <returns>An independently allocated document-order snapshot.</returns>
    internal System.Collections.Generic.List<SvgDiagnostic> GetDuplicateIdDiagnostics()
    {
        Refresh();
        return new System.Collections.Generic.List<SvgDiagnostic>(duplicateIdDiagnostics);
    }

    internal void Invalidate()
    {
        IndexedVersion = -1;
        RemoveDuplicateDiagnostics();
    }

    internal void Rebuild()
    {
        RemoveDuplicateDiagnostics();
        ids.Clear();
        idAttributes.Clear();
        classes.Clear();
        qualifiedNames.Clear();
        namespaces.Clear();
        expandedNames.Clear();
        sourceNodes.Clear();
        System.Collections.Generic.List<SvgSourceIndexEntry> sourceEntries = new System.Collections.Generic.List<SvgSourceIndexEntry>();
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        int topIndex = document.DocumentNode.Children.Count - 1;
        while (topIndex >= 0)
        {
            stack.Add(document.DocumentNode.Children[topIndex]);
            topIndex = topIndex - 1;
        }
        int order = 0;
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (node.SourceSpan != null) sourceEntries.Add(new SvgSourceIndexEntry(node, order));
            if (node.Kind == SvgNodeKind.Element) IndexElement(node);
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
            order = order + 1;
        }
        sourceEntries.Sort(CompareSourceEntries);
        int sourceIndex = 0;
        while (sourceIndex < sourceEntries.Count)
        {
            sourceNodes.Add(sourceEntries[sourceIndex].Node);
            sourceIndex = sourceIndex + 1;
        }
        IndexedVersion = document.Version;
    }

    internal void IndexElement(SvgNode element)
    {
        AddNode(qualifiedNames, element.QualifiedName, element);
        AddNode(namespaces, element.NamespaceUri, element);
        if (!expandedNames.ContainsKey(element.NamespaceUri)) expandedNames[element.NamespaceUri] = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>(System.StringComparer.Ordinal);
        AddNode(expandedNames[element.NamespaceUri], element.LocalName, element);
        int attributeIndex = 0;
        while (attributeIndex < element.Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[attributeIndex];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "id") IndexId(element, attribute);
            else if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "class") IndexClasses(element, attribute.Value);
            attributeIndex = attributeIndex + 1;
        }
    }

    internal void IndexId(SvgNode element, SvgAttribute attribute)
    {
        string id = attribute.Value;
        if (id.Length == 0) return;
        if (!ids.ContainsKey(id))
        {
            ids[id] = element;
            idAttributes[id] = attribute;
            return;
        }
        SvgAttribute firstAttribute = idAttributes[id];
        SvgSourceSpan? firstSpan = firstAttribute.SourceSpan == null ? ids[id].SourceSpan : firstAttribute.SourceSpan;
        SvgSourceSpan? duplicateSpan = attribute.SourceSpan == null ? element.SourceSpan : attribute.SourceSpan;
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.DuplicateId, "Duplicate ID '" + id + "'; the first element in document order is indexed.", duplicateSpan);
        if (firstSpan != null) diagnostic.AddRelatedSpan(firstSpan);
        duplicateIdDiagnostics.Add(diagnostic);
        document.Diagnostics.Add(diagnostic);
    }

    internal void IndexClasses(SvgNode element, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && IsClassWhitespace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !IsClassWhitespace(value[index])) index = index + 1;
            if (index > start)
            {
                string className = value.Substring(start, index - start);
                if (!classes.ContainsKey(className) || !ContainsNode(classes[className], element)) AddNode(classes, className, element);
            }
        }
    }

    internal void RemoveDuplicateDiagnostics()
    {
        int duplicateIndex = 0;
        while (duplicateIndex < duplicateIdDiagnostics.Count)
        {
            SvgDiagnostic diagnostic = duplicateIdDiagnostics[duplicateIndex];
            int documentIndex = document.Diagnostics.Count - 1;
            while (documentIndex >= 0)
            {
                if (object.ReferenceEquals(document.Diagnostics[documentIndex], diagnostic)) document.Diagnostics.RemoveAt(documentIndex);
                documentIndex = documentIndex - 1;
            }
            duplicateIndex = duplicateIndex + 1;
        }
        duplicateIdDiagnostics.Clear();
    }

    internal static void AddNode(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> index, string key, SvgNode node)
    {
        if (!index.ContainsKey(key)) index[key] = new System.Collections.Generic.List<SvgNode>();
        index[key].Add(node);
    }

    internal static bool ContainsNode(System.Collections.Generic.List<SvgNode> nodes, SvgNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], node)) return true;
            index = index + 1;
        }
        return false;
    }

    internal static bool IsClassWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r' || value == '\f';
    }

    internal static System.Collections.Generic.List<SvgNode> CopyNodes(System.Collections.Generic.List<SvgNode> nodes)
    {
        return new System.Collections.Generic.List<SvgNode>(nodes);
    }

    internal static bool SpansEqual(SvgSourceSpan? left, SvgSourceSpan right)
    {
        return object.ReferenceEquals((left ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Source, right.Source) && left.StartOffset == right.StartOffset && left.EndOffset == right.EndOffset && left.StartLine == right.StartLine && left.StartColumn == right.StartColumn && left.EndLine == right.EndLine && left.EndColumn == right.EndColumn;
    }

    internal static int CompareSourceEntries(SvgSourceIndexEntry left, SvgSourceIndexEntry right)
    {
        int order = new SvgSourceMapping().Compare((left.Node.SourceSpan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (right.Node.SourceSpan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (order != 0) return order;
        if (left.Order < right.Order) return -1;
        if (left.Order > right.Order) return 1;
        return 0;
    }
}
