namespace svg;

/// <summary>Performs deterministic index-backed, selector, and callback-based queries without global state.</summary>
public class SvgQuery : ISvgQuery
{
    /// <summary>Creates a stateless document query API.</summary>
    public SvgQuery()
    {
    }

    /// <summary>Returns the first element in document order with an exact nonempty ID.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="id">The case-sensitive ID.</param>
    /// <returns>The first matching element, or <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public SvgNode? GetById(SvgDocument document, string id)
    {
        ValidateDocument(document);
        return document.Index.GetById(id);
    }

    /// <summary>Returns elements containing an exact class token in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="className">The case-sensitive class token.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetByClass(SvgDocument document, string className)
    {
        ValidateDocument(document);
        return document.Index.GetByClass(className);
    }

    /// <summary>Returns elements with an exact case-sensitive qualified name in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="qualifiedName">The qualified XML name.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetByQualifiedName(SvgDocument document, string qualifiedName)
    {
        ValidateDocument(document);
        return document.Index.GetByQualifiedName(qualifiedName);
    }

    /// <summary>Returns elements in an exact namespace URI in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetByNamespace(SvgDocument document, string namespaceUri)
    {
        ValidateDocument(document);
        return document.Index.GetByNamespace(namespaceUri);
    }

    /// <summary>Returns elements with an exact namespace URI and local name in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetByExpandedName(SvgDocument document, string namespaceUri, string localName)
    {
        ValidateDocument(document);
        return document.Index.GetByExpandedName(namespaceUri, localName);
    }

    /// <summary>Returns nodes with an exact retained source span in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="span">The exact retained source span.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> GetBySourceSpan(SvgDocument document, SvgSourceSpan span)
    {
        ValidateDocument(document);
        return OrderInDocument(document, document.Index.GetBySourceSpan(span));
    }

    /// <summary>Returns nodes whose retained source spans contain a supplied span in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="span">The contained source span.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> FindContainingSourceSpan(SvgDocument document, SvgSourceSpan span)
    {
        ValidateDocument(document);
        return OrderInDocument(document, document.Index.FindContainingSourceSpan(span));
    }

    /// <summary>Returns nodes whose retained source spans overlap a supplied span in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="span">The candidate overlapping span.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> FindOverlappingSourceSpan(SvgDocument document, SvgSourceSpan span)
    {
        ValidateDocument(document);
        return OrderInDocument(document, document.Index.FindOverlappingSourceSpan(span));
    }

    /// <summary>Returns the first document node accepted by a predicate in preorder.</summary>
    /// <param name="document">The document to traverse without exposing its synthetic document node.</param>
    /// <param name="predicate">The callback that accepts a node.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgNode? FindFirst(SvgDocument document, System.Func<SvgNode, bool> predicate)
    {
        ValidateDocument(document);
        ValidatePredicate(predicate);
        return FindFirstSnapshot(CreateSnapshot(document.DocumentNode, false), predicate);
    }

    /// <summary>Returns the first node accepted by a predicate in root-inclusive preorder.</summary>
    /// <param name="root">The traversal root, which is tested first.</param>
    /// <param name="predicate">The callback that accepts a node.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgNode? FindFirstFromNode(SvgNode root, System.Func<SvgNode, bool> predicate)
    {
        ValidateNode(root);
        ValidatePredicate(predicate);
        return FindFirstSnapshot(CreateSnapshot(root, true), predicate);
    }

    /// <summary>Returns every document node accepted by a predicate in preorder.</summary>
    /// <param name="document">The document to traverse without exposing its synthetic document node.</param>
    /// <param name="predicate">The callback that accepts nodes.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> FindAll(SvgDocument document, System.Func<SvgNode, bool> predicate)
    {
        ValidateDocument(document);
        ValidatePredicate(predicate);
        return FindAllSnapshot(CreateSnapshot(document.DocumentNode, false), predicate);
    }

    /// <summary>Returns every node accepted by a predicate in root-inclusive preorder.</summary>
    /// <param name="root">The traversal root, which is tested first.</param>
    /// <param name="predicate">The callback that accepts nodes.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public System.Collections.Generic.List<SvgNode> FindAllFromNode(SvgNode root, System.Func<SvgNode, bool> predicate)
    {
        ValidateNode(root);
        ValidatePredicate(predicate);
        return FindAllSnapshot(CreateSnapshot(root, true), predicate);
    }

    /// <summary>Visits every document node once in preorder.</summary>
    /// <param name="document">The document to traverse without exposing its synthetic document node.</param>
    /// <param name="visitor">The callback invoked for each snapshotted node.</param>
    /// <returns>The number of visited nodes.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public int Traverse(SvgDocument document, System.Action<SvgNode> visitor)
    {
        ValidateDocument(document);
        ValidateVisitor(visitor);
        return TraverseSnapshot(CreateSnapshot(document.DocumentNode, false), visitor);
    }

    /// <summary>Visits a root and each descendant once in preorder.</summary>
    /// <param name="root">The traversal root, which is visited first.</param>
    /// <param name="visitor">The callback invoked for each snapshotted node.</param>
    /// <returns>The number of visited nodes.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public int TraverseFromNode(SvgNode root, System.Action<SvgNode> visitor)
    {
        ValidateNode(root);
        ValidateVisitor(visitor);
        return TraverseSnapshot(CreateSnapshot(root, true), visitor);
    }

    /// <summary>Returns the first document element matching a parsed selector list.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded query result containing at most one element.</returns>
    public SvgSelectorQueryResult QuerySelector(SvgDocument document, SvgSelector selector, SvgSelectorMatchOptions options)
    {
        ValidateDocument(document);
        if (document.DocumentElement == null) return EmptySelectorQuery(selector, options);
        return new SvgSelectorMatcher().Query(document.DocumentElement, selector, options, true);
    }

    /// <summary>Returns every document element matching a parsed selector list in preorder.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded document-ordered query result.</returns>
    public SvgSelectorQueryResult QuerySelectorAll(SvgDocument document, SvgSelector selector, SvgSelectorMatchOptions options)
    {
        ValidateDocument(document);
        if (document.DocumentElement == null) return EmptySelectorQuery(selector, options);
        return new SvgSelectorMatcher().Query(document.DocumentElement, selector, options, false);
    }

    /// <summary>Returns the first root-inclusive element matching a parsed selector list.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded query result containing at most one element.</returns>
    public SvgSelectorQueryResult QuerySelectorFromNode(SvgNode root, SvgSelector selector, SvgSelectorMatchOptions options)
    {
        ValidateNode(root);
        return new SvgSelectorMatcher().Query(root, selector, options, true);
    }

    /// <summary>Returns every root-inclusive element matching a parsed selector list in preorder.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded preorder query result.</returns>
    public SvgSelectorQueryResult QuerySelectorAllFromNode(SvgNode root, SvgSelector selector, SvgSelectorMatchOptions options)
    {
        ValidateNode(root);
        return new SvgSelectorMatcher().Query(root, selector, options, false);
    }

    /// <summary>Parses a selector and returns the first matching document element.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and query result containing at most one element.</returns>
    public SvgSelectorQueryResult QuerySelectorText(SvgDocument document, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options)
    {
        ValidateDocument(document);
        if (limits == null) throw new System.ArgumentNullException("limits");
        SvgSelectorParseResult parsed = new SvgSelectorParser().ParseWithValueAndLimits(selectorText, limits);
        if (document.DocumentElement == null) return EmptySelectorQuery(parsed.Selector, options);
        return new SvgSelectorMatcher().Query(document.DocumentElement, parsed.Selector, options, true);
    }

    /// <summary>Parses a selector and returns every matching document element in preorder.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and document-ordered query result.</returns>
    public SvgSelectorQueryResult QuerySelectorAllText(SvgDocument document, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options)
    {
        ValidateDocument(document);
        if (limits == null) throw new System.ArgumentNullException("limits");
        SvgSelectorParseResult parsed = new SvgSelectorParser().ParseWithValueAndLimits(selectorText, limits);
        if (document.DocumentElement == null) return EmptySelectorQuery(parsed.Selector, options);
        return new SvgSelectorMatcher().Query(document.DocumentElement, parsed.Selector, options, false);
    }

    /// <summary>Parses a selector and returns the first matching root-inclusive element.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and query result containing at most one element.</returns>
    public SvgSelectorQueryResult QuerySelectorTextFromNode(SvgNode root, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options)
    {
        ValidateNode(root);
        if (limits == null) throw new System.ArgumentNullException("limits");
        SvgSelectorParseResult parsed = new SvgSelectorParser().ParseWithValueAndLimits(selectorText, limits);
        return new SvgSelectorMatcher().Query(root, parsed.Selector, options, true);
    }

    /// <summary>Parses a selector and returns every matching root-inclusive element in preorder.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and preorder query result.</returns>
    public SvgSelectorQueryResult QuerySelectorAllTextFromNode(SvgNode root, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options)
    {
        ValidateNode(root);
        if (limits == null) throw new System.ArgumentNullException("limits");
        SvgSelectorParseResult parsed = new SvgSelectorParser().ParseWithValueAndLimits(selectorText, limits);
        return new SvgSelectorMatcher().Query(root, parsed.Selector, options, false);
    }

    static SvgSelectorQueryResult EmptySelectorQuery(SvgSelector selector, SvgSelectorMatchOptions options)
    {
        System.Collections.Generic.List<SvgDiagnostic> diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        bool validOptions = options != null && options.Namespaces != null && options.Namespaces.DefaultNamespace != null && options.Namespaces.NamedNamespaces != null && options.DynamicState != null && options.MaxWork > 0;
        bool success = selector != null && selector.IsValid && validOptions;
        if (selector != null)
        {
            int index = 0;
            while (index < selector.Diagnostics.Count)
            {
                diagnostics.Add(selector.Diagnostics[index]);
                index = index + 1;
            }
        }
        if (selector == null || !validOptions) diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "Selector query arguments are invalid.", null));
        return new SvgSelectorQueryResult(success, new System.Collections.Generic.List<SvgNode>(), false, 0, diagnostics);
    }

    static System.Collections.Generic.List<SvgNode> CreateSnapshot(SvgNode root, bool includeRoot)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.HashSet<SvgNode> visited = new System.Collections.Generic.HashSet<SvgNode>();
        if (includeRoot) stack.Add(root);
        else
        {
            int childIndex = root.Children.Count - 1;
            while (childIndex >= 0)
            {
                stack.Add(root.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (!visited.Add(node)) continue;
            result.Add(node);
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return result;
    }

    static SvgNode? FindFirstSnapshot(System.Collections.Generic.List<SvgNode> nodes, System.Func<SvgNode, bool> predicate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (predicate(nodes[index])) return nodes[index];
            index = index + 1;
        }
        return null;
    }

    static System.Collections.Generic.List<SvgNode> FindAllSnapshot(System.Collections.Generic.List<SvgNode> nodes, System.Func<SvgNode, bool> predicate)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            if (predicate(nodes[index])) result.Add(nodes[index]);
            index = index + 1;
        }
        return result;
    }

    static int TraverseSnapshot(System.Collections.Generic.List<SvgNode> nodes, System.Action<SvgNode> visitor)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            visitor(nodes[index]);
            index = index + 1;
        }
        return nodes.Count;
    }

    static System.Collections.Generic.List<SvgNode> OrderInDocument(SvgDocument document, System.Collections.Generic.List<SvgNode> candidates)
    {
        System.Collections.Generic.HashSet<SvgNode> candidateSet = new System.Collections.Generic.HashSet<SvgNode>(candidates);
        System.Collections.Generic.List<SvgNode> ordered = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> nodes = CreateSnapshot(document.DocumentNode, false);
        int index = 0;
        while (index < nodes.Count)
        {
            if (candidateSet.Contains(nodes[index])) ordered.Add(nodes[index]);
            index = index + 1;
        }
        return ordered;
    }

    static void ValidateDocument(SvgDocument document)
    {
        if (document == null) throw new System.ArgumentNullException("document");
    }

    static void ValidateNode(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("root");
    }

    static void ValidatePredicate(System.Func<SvgNode, bool> predicate)
    {
        if (predicate == null) throw new System.ArgumentNullException("predicate");
    }

    static void ValidateVisitor(System.Action<SvgNode> visitor)
    {
        if (visitor == null) throw new System.ArgumentNullException("visitor");
    }
}
