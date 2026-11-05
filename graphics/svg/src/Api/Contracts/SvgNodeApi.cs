namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgNodeApi"/>.</summary>
public interface ISvgNodeApi
{
    /// <summary>Appends a detached child to a document or element.</summary>
    /// <param name="parent">The destination document or element.</param>
    /// <param name="child">The detached non-document child.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult AppendChild(SvgNode? parent, SvgNode? child);
    /// <summary>Inserts a detached child at a zero-based child index.</summary>
    /// <param name="parent">The destination document or element.</param>
    /// <param name="index">The insertion index from zero through the child count.</param>
    /// <param name="child">The detached non-document child.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult InsertChild(SvgNode? parent, int index, SvgNode? child);
    /// <summary>Replaces one direct child with a detached node.</summary>
    /// <param name="parent">The current parent.</param>
    /// <param name="current">The direct child to replace.</param>
    /// <param name="replacement">The detached replacement.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult ReplaceChild(SvgNode? parent, SvgNode? current, SvgNode? replacement);
    /// <summary>Removes one direct child and clears ownership throughout its subtree.</summary>
    /// <param name="parent">The current parent.</param>
    /// <param name="child">The direct child to remove.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult RemoveChild(SvgNode? parent, SvgNode? child);
    /// <summary>Moves an attached child to a final zero-based index under another document or element.</summary>
    /// <param name="child">The attached child to move.</param>
    /// <param name="newParent">The destination document or element.</param>
    /// <param name="index">The final destination index, or the destination count to append.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult MoveChild(SvgNode? child, SvgNode? newParent, int index);
    /// <summary>Creates an independent detached copy of a node.</summary>
    /// <param name="node">The node to copy.</param>
    /// <param name="deep">Whether descendants are copied recursively.</param>
    /// <returns>A generated node with copied attributes and, when requested, descendants.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    SvgNode CloneNode(SvgNode node, bool deep);
    /// <summary>Returns the textual content of a node.</summary>
    /// <param name="node">The node whose textual content is read.</param>
    /// <returns>The node value for character-like nodes, concatenated descendant text for containers, or an empty string for a doctype.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    string GetTextContent(SvgNode node);
    /// <summary>Replaces the textual content of an element or character-like node.</summary>
    /// <param name="node">The element or character-like node to change.</param>
    /// <param name="value">The replacement text.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult SetTextContent(SvgNode? node, string? value);
    /// <summary>Recursively merges adjacent text nodes and removes empty text nodes.</summary>
    /// <param name="node">The container subtree to normalize.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult NormalizeText(SvgNode? node);
    /// <summary>Returns ancestors from the direct parent through the outermost document node.</summary>
    /// <param name="node">The node whose ancestors are inspected.</param>
    /// <returns>An independently allocated nearest-first snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetAncestors(SvgNode node);
    /// <summary>Returns all descendants in preorder document order.</summary>
    /// <param name="node">The node whose descendants are inspected.</param>
    /// <returns>An independently allocated snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetDescendants(SvgNode node);
    /// <summary>Returns the immediately preceding sibling, or <c>null</c>.</summary>
    /// <param name="node">The node whose sibling is inspected.</param>
    /// <returns>The preceding sibling, or <c>null</c> when none exists.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    SvgNode? GetPreviousSibling(SvgNode node);
    /// <summary>Returns the immediately following sibling, or <c>null</c>.</summary>
    /// <param name="node">The node whose sibling is inspected.</param>
    /// <returns>The following sibling, or <c>null</c> when none exists.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    SvgNode? GetNextSibling(SvgNode node);
    /// <summary>Returns all preceding siblings in document order.</summary>
    /// <param name="node">The node whose siblings are inspected.</param>
    /// <returns>An independently allocated snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetPreviousSiblings(SvgNode node);
    /// <summary>Returns all following siblings in document order.</summary>
    /// <param name="node">The node whose siblings are inspected.</param>
    /// <returns>An independently allocated snapshot.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    System.Collections.Generic.List<SvgNode> GetNextSiblings(SvgNode node);
    /// <summary>Tests whether one node is a strict ancestor of another node.</summary>
    /// <param name="ancestor">The possible ancestor.</param>
    /// <param name="node">The possible descendant.</param>
    /// <returns><c>true</c> when the ancestry relation exists.</returns>
    bool IsAncestor(SvgNode? ancestor, SvgNode? node);
    /// <summary>Tests whether one node is a strict descendant of another node.</summary>
    /// <param name="descendant">The possible descendant.</param>
    /// <param name="node">The possible ancestor.</param>
    /// <returns><c>true</c> when the ancestry relation exists.</returns>
    bool IsDescendant(SvgNode descendant, SvgNode node);
    /// <summary>Resolves a namespace prefix in the node's in-scope namespace context.</summary>
    /// <param name="node">The context node.</param>
    /// <param name="prefix">The prefix, or an empty string for the default namespace.</param>
    /// <returns>The namespace URI, or an empty string when no nonempty binding exists.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    string LookupNamespaceUri(SvgNode node, string prefix);
    /// <summary>Finds an in-scope prefix for a namespace URI.</summary>
    /// <param name="node">The context node.</param>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <returns>The nearest prefix, an empty string for a matching default namespace, or an empty string when no prefix exists.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    string LookupPrefix(SvgNode node, string namespaceUri);
    /// <summary>Tests whether a namespace URI is the context node's default namespace.</summary>
    /// <param name="node">The context node.</param>
    /// <param name="namespaceUri">The namespace URI.</param>
    /// <returns><c>true</c> when the default namespace resolves to the supplied URI.</returns>
    bool IsDefaultNamespace(SvgNode? node, string? namespaceUri);
    /// <summary>Finds the nearest inherited attribute in the XML namespace.</summary>
    /// <param name="node">The node whose element context is inspected.</param>
    /// <param name="localName">The case-sensitive XML attribute local name.</param>
    /// <returns>The nearest matching attribute, or <c>null</c> when absent.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgAttribute? GetInheritedXmlAttribute(SvgNode node, string localName);
    /// <summary>Returns the nearest inherited authored <c>xml:base</c> value or the document base URI.</summary>
    /// <param name="node">The node whose base context is inspected.</param>
    /// <returns>The nearest authored value, the document base URI, or an empty string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    string LookupXmlBase(SvgNode node);
    /// <summary>Resolves the document base URI and each ancestor <c>xml:base</c> value.</summary>
    /// <param name="node">The node whose effective base URI is resolved.</param>
    /// <returns>The effective URI text, or the innermost unresolved reference when no absolute base is available.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    string ResolveXmlBase(SvgNode node);
    /// <summary>Returns the nearest inherited <c>xml:lang</c> value.</summary>
    /// <param name="node">The node whose language context is inspected.</param>
    /// <returns>The inherited language tag or an empty string when unspecified or explicitly reset.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    string LookupXmlLanguage(SvgNode node);
    /// <summary>Returns the nearest inherited XML white-space intention.</summary>
    /// <param name="node">The node whose white-space context is inspected.</param>
    /// <returns>The inherited mode, the default when absent, or <c>Invalid</c> for an invalid nearest value.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    SvgXmlSpaceMode LookupXmlSpace(SvgNode node);
    /// <summary>Returns inherited legacy processing flags and SVG 2 CSS priority metadata.</summary>
    /// <param name="node">The node whose white-space context is inspected.</param>
    /// <param name="profile">The SVG text compatibility profile.</param>
    /// <returns>A newly allocated policy for later text layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when <c>profile</c> is invalid.</exception>
    SvgTextWhitespacePolicy GetTextWhitespacePolicy(SvgNode node, SvgTextWhitespaceProfile profile);
    /// <summary>Applies the SVG 1.1 legacy algorithm to one text string using inherited <c>xml:space</c>.</summary>
    /// <param name="node">The node providing the inherited white-space context.</param>
    /// <param name="value">The XML-normalized character data to process.</param>
    /// <returns>A newly allocated processed text string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    string ApplyLegacyTextWhitespace(SvgNode node, string value);
    /// <summary>Tests two node subtrees for namespace-aware semantic equality.</summary>
    /// <param name="first">The first node.</param>
    /// <param name="second">The second node.</param>
    /// <returns><c>true</c> when node kinds, expanded names, values, unordered attributes, and ordered descendants are equal.</returns>
    bool DeepEquals(SvgNode? first, SvgNode? second);
    /// <summary>Appends a detached attribute to an element.</summary>
    /// <param name="element">The destination element.</param>
    /// <param name="attribute">The detached attribute.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult AppendAttribute(SvgNode? element, SvgAttribute? attribute);
    /// <summary>Inserts a detached attribute at a zero-based ordered index.</summary>
    /// <param name="element">The destination element.</param>
    /// <param name="index">The insertion index from zero through the attribute count.</param>
    /// <param name="attribute">The detached attribute.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult InsertAttribute(SvgNode? element, int index, SvgAttribute? attribute);
    /// <summary>Replaces one owned attribute with a detached attribute.</summary>
    /// <param name="element">The owning element.</param>
    /// <param name="current">The attribute to replace.</param>
    /// <param name="replacement">The detached replacement attribute.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult ReplaceAttribute(SvgNode? element, SvgAttribute? current, SvgAttribute? replacement);
    /// <summary>Removes one owned attribute and clears its ownership.</summary>
    /// <param name="element">The owning element.</param>
    /// <param name="attribute">The attribute to remove.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult RemoveAttribute(SvgNode? element, SvgAttribute? attribute);
    /// <summary>Moves an attached attribute to a final zero-based index on another element.</summary>
    /// <param name="attribute">The attached attribute to move.</param>
    /// <param name="newOwner">The destination element.</param>
    /// <param name="index">The final destination index, or the destination count to append.</param>
    /// <returns>The mutation result.</returns>
    SvgMutationResult MoveAttribute(SvgAttribute? attribute, SvgNode? newOwner, int index);
}
