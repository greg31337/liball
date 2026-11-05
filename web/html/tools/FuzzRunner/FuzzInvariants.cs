using html;

namespace UniversalHtml.FuzzRunner;

internal sealed class FuzzInvariantException : Exception
{
    public FuzzInvariantException(string codeValue, string message)
        : base(message)
    {
        Code = codeValue;
    }

    public string Code { get; }
}

internal static class FuzzInvariants
{
    const string HtmlNamespace = "http://www.w3.org/1999/xhtml";

    public static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new FuzzInvariantException(code, message);
        }
    }

    public static void AssertTree(HtmlDocument document, HtmlNode root,
        int maximumVisits)
    {
        Require(document != null, "tree-owner-missing",
            "A parsed tree must have an owner document.");
        Require(root != null, "tree-root-missing",
            "A parsed tree must have a root.");
        HashSet<HtmlNode> visited = new HashSet<HtmlNode>(
            ReferenceEqualityComparer.Instance);
        Stack<TreeFrame> pending = new Stack<TreeFrame>();
        pending.Push(new TreeFrame(root, null));
        while (pending.Count > 0)
        {
            TreeFrame frame = pending.Pop();
            HtmlNode node = frame.Node;
            Require(visited.Add(node), "tree-cycle-or-alias",
                "A node was reached more than once.");
            Require(visited.Count <= maximumVisits, "tree-unbounded",
                "Tree traversal exceeded the configured visit bound.");
            Require(ReferenceEquals(node.GetParent(), frame.Parent),
                "tree-parent-mismatch",
                "A child does not point back to its parent.");
            if (node.GetKind() == HtmlNodeKind.Document)
            {
                Require(node.GetOwnerDocument() == null,
                    "document-owner-mismatch",
                    "The document root must not expose an owner document.");
            }
            else
            {
                Require(ReferenceEquals(node.GetOwnerDocument(), document),
                    "tree-owner-mismatch",
                    "A node has the wrong owner document.");
            }

            HtmlNode[] children = node.GetChildren();
            Require(ReferenceEquals(node.GetFirstChild(),
                children.Length == 0 ? null : children[0]),
                "tree-first-child-mismatch",
                "The first-child link disagrees with the child snapshot.");
            Require(ReferenceEquals(node.GetLastChild(),
                children.Length == 0 ? null : children[^1]),
                "tree-last-child-mismatch",
                "The last-child link disagrees with the child snapshot.");
            for (int index = children.Length - 1; index >= 0; index--)
            {
                HtmlNode child = children[index];
                Require(child != null, "tree-null-child",
                    "A child snapshot contains null.");
                Require(ReferenceEquals(child.GetPreviousSibling(),
                    index == 0 ? null : children[index - 1]),
                    "tree-previous-sibling-mismatch",
                    "A previous-sibling link is inconsistent.");
                Require(ReferenceEquals(child.GetNextSibling(),
                    index + 1 == children.Length ? null : children[index + 1]),
                    "tree-next-sibling-mismatch",
                    "A next-sibling link is inconsistent.");
                pending.Push(new TreeFrame(child, node));
            }

            HtmlNode template = node.GetTemplateContent();
            if (template != null)
            {
                Require(template.GetKind() == HtmlNodeKind.Fragment,
                    "template-content-kind",
                    "Template content must be a document fragment.");
                Require(template.GetParent() == null,
                    "template-content-parent",
                    "Template content must be detached from the element tree.");
                pending.Push(new TreeFrame(template, null));
            }
        }
    }

    public static void AssertSafeSanitizedTree(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy)
    {
        Stack<HtmlNode> pending = new Stack<HtmlNode>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            HtmlNode node = pending.Pop();
            if (node.GetKind() == HtmlNodeKind.Element)
            {
                Require(node.GetNamespaceUri() == HtmlNamespace,
                    "sanitizer-foreign-element",
                    "The HTML-only sanitizer retained foreign content.");
                string name = node.GetLocalName();
                Require(!IsDangerousElement(name),
                    "sanitizer-dangerous-element",
                    "A dangerous element survived sanitizer reparsing: "
                        + name + ".");
                foreach (HtmlAttribute attribute in node.GetAttributes())
                {
                    Require(!attribute.GetLocalName().StartsWith("on",
                        StringComparison.OrdinalIgnoreCase),
                        "sanitizer-event-attribute",
                        "An event-handler attribute survived sanitizer reparsing.");
                    if (HtmlDomUrls.GetAttributeKind(node, attribute)
                        != HtmlUrlAttributeKind.None)
                    {
                        HtmlSanitizerUrlDecision decision =
                            HtmlSanitizerUrlAttributes.Evaluate(node,
                                attribute, policy);
                        Require(decision.IsAllowed(),
                            "sanitizer-unsafe-url",
                            "A URL rejected by the compiled policy survived reparsing.");
                    }
                }
            }

            HtmlNode[] children = node.GetChildren();
            for (int index = children.Length - 1; index >= 0; index--)
            {
                pending.Push(children[index]);
            }

            HtmlNode template = node.GetTemplateContent();
            if (template != null)
            {
                pending.Push(template);
            }
        }
    }

    static bool IsDangerousElement(string name)
    {
        return name == "script" || name == "style" || name == "iframe"
            || name == "object" || name == "embed" || name == "template"
            || name == "noscript" || name == "noembed"
            || name == "frameset" || name == "plaintext" || name == "xmp";
    }

    sealed class TreeFrame
    {
        public TreeFrame(HtmlNode nodeValue, HtmlNode parentValue)
        {
            Node = nodeValue;
            Parent = parentValue;
        }

        public HtmlNode Node { get; }
        public HtmlNode Parent { get; }
    }
}
