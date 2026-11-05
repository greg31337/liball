namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgQuery"/>.</summary>
public interface ISvgQuery
{
    /// <summary>Returns the first element in document order with an exact nonempty ID.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="id">The case-sensitive ID.</param>
    /// <returns>The first matching element, or <c>null</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    SvgNode? GetById(SvgDocument document, string id);
    /// <summary>Returns elements containing an exact class token in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="className">The case-sensitive class token.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetByClass(SvgDocument document, string className);
    /// <summary>Returns elements with an exact case-sensitive qualified name in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="qualifiedName">The qualified XML name.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetByQualifiedName(SvgDocument document, string qualifiedName);
    /// <summary>Returns elements in an exact namespace URI in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetByNamespace(SvgDocument document, string namespaceUri);
    /// <summary>Returns elements with an exact namespace URI and local name in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetByExpandedName(SvgDocument document, string namespaceUri, string localName);
    /// <summary>Returns nodes with an exact retained source span in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="span">The exact retained source span.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetBySourceSpan(SvgDocument document, SvgSourceSpan span);
    /// <summary>Returns nodes whose retained source spans contain a supplied span in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="span">The contained source span.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> FindContainingSourceSpan(SvgDocument document, SvgSourceSpan span);
    /// <summary>Returns nodes whose retained source spans overlap a supplied span in document order.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="span">The candidate overlapping span.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> FindOverlappingSourceSpan(SvgDocument document, SvgSourceSpan span);
    /// <summary>Returns the first document node accepted by a predicate in preorder.</summary>
    /// <param name="document">The document to traverse without exposing its synthetic document node.</param>
    /// <param name="predicate">The callback that accepts a node.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgNode? FindFirst(SvgDocument document, System.Func<SvgNode, bool> predicate);
    /// <summary>Returns the first node accepted by a predicate in root-inclusive preorder.</summary>
    /// <param name="root">The traversal root, which is tested first.</param>
    /// <param name="predicate">The callback that accepts a node.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgNode? FindFirstFromNode(SvgNode root, System.Func<SvgNode, bool> predicate);
    /// <summary>Returns every document node accepted by a predicate in preorder.</summary>
    /// <param name="document">The document to traverse without exposing its synthetic document node.</param>
    /// <param name="predicate">The callback that accepts nodes.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> FindAll(SvgDocument document, System.Func<SvgNode, bool> predicate);
    /// <summary>Returns every node accepted by a predicate in root-inclusive preorder.</summary>
    /// <param name="root">The traversal root, which is tested first.</param>
    /// <param name="predicate">The callback that accepts nodes.</param>
    /// <returns>An independently allocated matching snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> FindAllFromNode(SvgNode root, System.Func<SvgNode, bool> predicate);
    /// <summary>Visits every document node once in preorder.</summary>
    /// <param name="document">The document to traverse without exposing its synthetic document node.</param>
    /// <param name="visitor">The callback invoked for each snapshotted node.</param>
    /// <returns>The number of visited nodes.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    int Traverse(SvgDocument document, System.Action<SvgNode> visitor);
    /// <summary>Visits a root and each descendant once in preorder.</summary>
    /// <param name="root">The traversal root, which is visited first.</param>
    /// <param name="visitor">The callback invoked for each snapshotted node.</param>
    /// <returns>The number of visited nodes.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    int TraverseFromNode(SvgNode root, System.Action<SvgNode> visitor);
    /// <summary>Returns the first document element matching a parsed selector list.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded query result containing at most one element.</returns>
    SvgSelectorQueryResult QuerySelector(SvgDocument document, SvgSelector selector, SvgSelectorMatchOptions options);
    /// <summary>Returns every document element matching a parsed selector list in preorder.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded document-ordered query result.</returns>
    SvgSelectorQueryResult QuerySelectorAll(SvgDocument document, SvgSelector selector, SvgSelectorMatchOptions options);
    /// <summary>Returns the first root-inclusive element matching a parsed selector list.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded query result containing at most one element.</returns>
    SvgSelectorQueryResult QuerySelectorFromNode(SvgNode root, SvgSelector selector, SvgSelectorMatchOptions options);
    /// <summary>Returns every root-inclusive element matching a parsed selector list in preorder.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selector">The parsed selector list.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded preorder query result.</returns>
    SvgSelectorQueryResult QuerySelectorAllFromNode(SvgNode root, SvgSelector selector, SvgSelectorMatchOptions options);
    /// <summary>Parses a selector and returns the first matching document element.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and query result containing at most one element.</returns>
    SvgSelectorQueryResult QuerySelectorText(SvgDocument document, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options);
    /// <summary>Parses a selector and returns every matching document element in preorder.</summary>
    /// <param name="document">The document to query.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and document-ordered query result.</returns>
    SvgSelectorQueryResult QuerySelectorAllText(SvgDocument document, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options);
    /// <summary>Parses a selector and returns the first matching root-inclusive element.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and query result containing at most one element.</returns>
    SvgSelectorQueryResult QuerySelectorTextFromNode(SvgNode root, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options);
    /// <summary>Parses a selector and returns every matching root-inclusive element in preorder.</summary>
    /// <param name="root">The query scope and traversal root.</param>
    /// <param name="selectorText">The selector source.</param>
    /// <param name="limits">The selector parsing limits.</param>
    /// <param name="options">The namespace, state, scope, and work options.</param>
    /// <returns>A bounded parse and preorder query result.</returns>
    SvgSelectorQueryResult QuerySelectorAllTextFromNode(SvgNode root, string selectorText, SvgCssLimits limits, SvgSelectorMatchOptions options);
}
