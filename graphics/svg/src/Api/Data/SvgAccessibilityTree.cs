namespace svg;

/// <summary>Configures bounded platform-neutral accessibility-tree construction.</summary>
public class SvgAccessibilityTreeOptions
{
    /// <summary>The positive maximum resolved nodes visited.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum logical text characters retained.</summary>
    public int MaximumTextCharacters;
    /// <summary>The bounded per-node accessible-semantics options.</summary>
    public SvgAccessibilityOptions Semantics;

    /// <summary>Creates secure deterministic accessibility-tree defaults.</summary>
    public SvgAccessibilityTreeOptions()
    {
        MaximumNodes = 1000000;
        MaximumTextCharacters = 1000000;
        Semantics = new SvgAccessibilityOptions();
    }
}

/// <summary>Associates a logical tree-text range with exact source and instance identity.</summary>
public class SvgAccessibilityTextRange
{
    /// <summary>The zero-based UTF-16 start in the tree's logical text.</summary>
    public int Start;
    /// <summary>The positive UTF-16 length.</summary>
    public int Length;
    /// <summary>The normalized text represented by this range.</summary>
    public string Text;
    /// <summary>The resolved text node that supplied the range.</summary>
    public SvgResolvedNode ResolvedNode;
    /// <summary>The corresponding authored source text node.</summary>
    public SvgNode? SourceNode;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>The accessible element that owns this direct text range.</summary>
    public SvgAccessibilityTreeNode Owner;

    internal SvgAccessibilityTextRange(SvgResolvedNode node, SvgAccessibilityTreeNode owner, int start, string text)
    {
        Start = start;
        Length = text.Length;
        Text = text;
        ResolvedNode = node;
        SourceNode = node.SourceNode;
        SourcePath = node.SourcePath;
        InstancePath = node.InstancePath;
        Owner = owner;
    }
}

/// <summary>Contains one platform-neutral accessible object in logical reading order.</summary>
public class SvgAccessibilityTreeNode
{
    /// <summary>The owning accessibility tree.</summary>
    public SvgAccessibilityTree Tree;
    /// <summary>The accessible parent, or <c>null</c> for the root.</summary>
    public SvgAccessibilityTreeNode? Parent;
    /// <summary>The accessible children in logical reading order.</summary>
    public System.Collections.Generic.List<SvgAccessibilityTreeNode> Children;
    /// <summary>The corresponding resolved source or instance node.</summary>
    public SvgResolvedNode ResolvedNode;
    /// <summary>The corresponding authored source element.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source element version captured during construction.</summary>
    public long SourceVersion;
    /// <summary>The stable authored source path.</summary>
    public string SourcePath;
    /// <summary>The stable use-instance path, or an empty string.</summary>
    public string InstancePath;
    /// <summary>Whether this object belongs to a generated use instance.</summary>
    public bool IsInstance;
    /// <summary>The calculated accessible semantic snapshot.</summary>
    public SvgAccessibleSemantics Semantics;
    /// <summary>The zero-based logical preorder position.</summary>
    public int ReadingOrder;
    /// <summary>The unioned world-space geometry, or <c>null</c> when unavailable or empty.</summary>
    public SvgRect? Bounds;
    /// <summary>Whether nonempty world-space geometry is available.</summary>
    public bool HasGeometry;
    /// <summary>Whether this object can receive focus directly.</summary>
    public bool Focusable;
    /// <summary>Whether this object participates in sequential focus navigation.</summary>
    public bool Sequential;
    /// <summary>Whether a valid explicit <c>tabindex</c> supplied focusability.</summary>
    public bool HasTabIndex;
    /// <summary>The explicit tab index, or zero for implicit focusability.</summary>
    public int TabIndex;
    /// <summary>The first UTF-16 offset covered by this subtree, or minus one.</summary>
    public int TextStart;
    /// <summary>The UTF-16 span covered by this subtree's logical text.</summary>
    public int TextLength;
    /// <summary>The direct logical text ranges owned by this object.</summary>
    public System.Collections.Generic.List<SvgAccessibilityTextRange> TextRanges;

    internal SvgAccessibilityTreeNode(SvgAccessibilityTree tree, SvgAccessibilityTreeNode? parent, SvgResolvedNode resolved, SvgAccessibleSemantics semantics)
    {
        Tree = tree;
        Parent = parent;
        Children = new System.Collections.Generic.List<SvgAccessibilityTreeNode>();
        ResolvedNode = resolved;
        SourceNode = resolved.SourceNode;
        SourceVersion = (resolved.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Version;
        SourcePath = resolved.SourcePath;
        InstancePath = resolved.InstancePath;
        IsInstance = resolved.InstancePath.Length != 0;
        Semantics = semantics;
        ReadingOrder = -1;
        Bounds = null;
        HasGeometry = false;
        Focusable = false;
        Sequential = false;
        HasTabIndex = false;
        TabIndex = 0;
        TextStart = -1;
        TextLength = 0;
        TextRanges = new System.Collections.Generic.List<SvgAccessibilityTextRange>();
    }

    /// <summary>Checks whether this accessible object's source identity remains current.</summary>
    /// <returns><c>true</c> when the source and semantic snapshot remain current.</returns>
    internal bool IsCurrent()
    {
        return SourceNode != null && SourceNode.Version == SourceVersion && Semantics != null && Semantics.IsCurrent();
    }
}

/// <summary>Contains a bounded platform-neutral accessibility tree and its alternate orders.</summary>
public class SvgAccessibilityTree
{
    /// <summary>The resolved document used for construction.</summary>
    public SvgResolvedDocument? Document;
    /// <summary>The authored document used for construction.</summary>
    public SvgDocument? SourceDocument;
    /// <summary>The authored document version captured during construction.</summary>
    public long SourceVersion;
    /// <summary>The accessible root, or <c>null</c> for an entirely hidden document.</summary>
    public SvgAccessibilityTreeNode? Root;
    /// <summary>Every accessible object in logical preorder.</summary>
    public System.Collections.Generic.List<SvgAccessibilityTreeNode> Nodes;
    /// <summary>The same accessible objects in explicit logical reading order.</summary>
    public System.Collections.Generic.List<SvgAccessibilityTreeNode> ReadingOrder;
    /// <summary>The sequentially focusable objects in tab order.</summary>
    public System.Collections.Generic.List<SvgAccessibilityTreeNode> FocusOrder;
    /// <summary>Every direct source text range in logical order.</summary>
    public System.Collections.Generic.List<SvgAccessibilityTextRange> TextRanges;
    /// <summary>The normalized logical text addressed by the ranges.</summary>
    public string Text;
    /// <summary>The number of resolved nodes visited during construction.</summary>
    public int VisitedNodeCount;
    /// <summary>Located semantic, focus, limit, and geometry diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The stable construction failure.</summary>
    public SvgAccessibilityTreeError Error;
    /// <summary>Whether bounded tree construction completed.</summary>
    public bool Success;

    internal SvgAccessibilityTree(SvgResolvedDocument? document)
    {
        Document = document;
        SourceDocument = document == null ? null : document.SourceDocument;
        SourceVersion = SourceDocument == null ? 0 : SourceDocument.Version;
        Root = null;
        Nodes = new System.Collections.Generic.List<SvgAccessibilityTreeNode>();
        ReadingOrder = new System.Collections.Generic.List<SvgAccessibilityTreeNode>();
        FocusOrder = new System.Collections.Generic.List<SvgAccessibilityTreeNode>();
        TextRanges = new System.Collections.Generic.List<SvgAccessibilityTextRange>();
        Text = "";
        VisitedNodeCount = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Error = SvgAccessibilityTreeError.None;
        Success = false;
    }

    /// <summary>Finds an accessible object by authored source and exact instance path.</summary>
    /// <param name="source">The authored source element.</param>
    /// <param name="instancePath">The exact instance path, or an empty string.</param>
    /// <returns>The matching accessible object, or <c>null</c>.</returns>
    internal SvgAccessibilityTreeNode? Find(SvgNode? source, string? instancePath)
    {
        if (source == null || instancePath == null) return null;
        int index = 0;
        while (index < Nodes.Count)
        {
            SvgAccessibilityTreeNode node = Nodes[index];
            if (object.ReferenceEquals(node.SourceNode, source) && node.InstancePath == instancePath) return node;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Checks every retained document, source, and semantic version.</summary>
    /// <returns><c>true</c> when this tree still describes current source.</returns>
    internal bool IsCurrent()
    {
        if (SourceDocument == null || SourceDocument.Version != SourceVersion || Document == null || !Document.IsCurrent()) return false;
        int index = 0;
        while (index < Nodes.Count)
        {
            if (!Nodes[index].IsCurrent()) return false;
            index = index + 1;
        }
        return true;
    }
}
