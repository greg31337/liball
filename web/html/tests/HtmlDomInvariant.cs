
namespace UniversalHtml.Tests;

static class HtmlDomInvariant
{
    public static string FindViolation(HtmlDocument document)
    {
        return FindViolation(document, Array.Empty<HtmlNode>());
    }

    public static string FindViolation(HtmlDocument document,
        params HtmlNode[] detachedRoots)
    {
        if (document == null)
        {
            return "The document is null.";
        }

        HtmlNode root = document.GetRoot();
        if (root == null || root.GetKind() != HtmlNodeKind.Document)
        {
            return "The document root is missing or has the wrong kind.";
        }

        if (root.GetOwnerDocument() != null
            || root.GetOwnerDocumentStorage() != document)
        {
            return "The document root has inconsistent ownership.";
        }

        string documentViolation = FindDocumentChildViolation(document, root);
        if (documentViolation.Length > 0)
        {
            return documentViolation;
        }

        HashSet<HtmlNode> visited = new HashSet<HtmlNode>(
            ReferenceEqualityComparer.Instance);
        string violation = Visit(root, document, null, null, null, visited);
        if (violation.Length > 0)
        {
            return violation;
        }

        foreach (HtmlNode detached in detachedRoots)
        {
            if (detached == null || visited.Contains(detached)
                || detached.GetParent() != null)
            {
                continue;
            }

            violation = Visit(detached, document, null, null, null, visited);
            if (violation.Length > 0)
            {
                return violation;
            }
        }

        foreach (HtmlNode detached in detachedRoots)
        {
            if (detached == null || visited.Contains(detached))
            {
                continue;
            }

            HashSet<HtmlNode> parentChain = new HashSet<HtmlNode>(
                ReferenceEqualityComparer.Instance);
            HtmlNode detachedRoot = detached;
            while (detachedRoot.GetParent() != null)
            {
                if (!parentChain.Add(detachedRoot))
                {
                    return "A detached parent chain contains a cycle at "
                        + detached.GetName() + ".";
                }

                detachedRoot = detachedRoot.GetParent();
            }

            if (visited.Contains(detachedRoot))
            {
                return "A parent link is not represented by its child list at "
                    + detached.GetName() + ".";
            }

            violation = Visit(detachedRoot, document, null, null, null,
                visited);
            if (violation.Length > 0)
            {
                return violation;
            }
        }

        return "";
    }

    static string Visit(HtmlNode node, HtmlDocument document,
        HtmlNode expectedParent, HtmlNode expectedPrevious,
        HtmlNode expectedNext, HashSet<HtmlNode> visited)
    {
        if (!visited.Add(node))
        {
            return "A node is reachable more than once or forms a cycle: "
                + node.GetName() + ".";
        }

        if (node.GetParent() != expectedParent
            || node.GetPreviousSibling() != expectedPrevious
            || node.GetNextSibling() != expectedNext)
        {
            return "Parent or sibling symmetry failed at "
                + node.GetName() + ".";
        }

        if (node.GetKind() != HtmlNodeKind.Document
            && node.GetOwnerDocument() != document)
        {
            return "Owner-document consistency failed at "
                + node.GetName() + ".";
        }

        string ancestorViolation = FindAncestorViolation(node);
        if (ancestorViolation.Length > 0)
        {
            return ancestorViolation;
        }

        HtmlNode[] children = node.GetChildren();
        bool canContain = node.GetKind() == HtmlNodeKind.Document
            || node.GetKind() == HtmlNodeKind.Fragment
            || node.GetKind() == HtmlNodeKind.Element;
        if (!canContain && children.Length > 0)
        {
            return "A leaf node contains children: " + node.GetName() + ".";
        }

        HtmlNode expectedFirst = children.Length == 0 ? null : children[0];
        HtmlNode expectedLast = children.Length == 0
            ? null : children[children.Length - 1];
        if (node.GetFirstChild() != expectedFirst
            || node.GetLastChild() != expectedLast)
        {
            return "First/last child consistency failed at "
                + node.GetName() + ".";
        }

        int index = 0;
        while (index < children.Length)
        {
            HtmlNode previous = index == 0 ? null : children[index - 1];
            HtmlNode next = index + 1 == children.Length
                ? null : children[index + 1];
            if (children[index] == null
                || children[index].GetKind() == HtmlNodeKind.Document)
            {
                return "A child slot is null or contains a document node.";
            }

            string violation = Visit(children[index], document, node,
                previous, next, visited);
            if (violation.Length > 0)
            {
                return violation;
            }

            index++;
        }

        if (node.GetKind() == HtmlNodeKind.Element
            && node.GetTemplateContent() != null)
        {
            HtmlNode content = node.GetTemplateContent();
            if (content.GetKind() != HtmlNodeKind.Fragment
                || content.GetTemplateHostStorage() != node
                || content.GetParent() != null)
            {
                return "Template-content ownership failed at "
                    + node.GetName() + ".";
            }

            string violation = Visit(content, document, null, null, null,
                visited);
            if (violation.Length > 0)
            {
                return violation;
            }
        }

        return "";
    }

    static string FindAncestorViolation(HtmlNode node)
    {
        HashSet<HtmlNode> ancestors = new HashSet<HtmlNode>(
            ReferenceEqualityComparer.Instance);
        HtmlNode current = node;
        while (current != null)
        {
            if (!ancestors.Add(current))
            {
                return "The parent chain contains a cycle at "
                    + node.GetName() + ".";
            }

            current = current.GetParent();
        }

        return "";
    }

    static string FindDocumentChildViolation(HtmlDocument document,
        HtmlNode root)
    {
        HtmlNode[] children = root.GetChildren();
        int elementCount = 0;
        int doctypeCount = 0;
        bool elementSeen = false;
        HtmlNode documentElement = null;
        foreach (HtmlNode child in children)
        {
            if (child.GetKind() == HtmlNodeKind.Element)
            {
                elementCount++;
                elementSeen = true;
                if (documentElement == null)
                {
                    documentElement = child;
                }
            }
            else if (child.GetKind() == HtmlNodeKind.DocumentType)
            {
                doctypeCount++;
                if (elementSeen)
                {
                    return "A document type follows the document element.";
                }
            }
            else if (child.GetKind() != HtmlNodeKind.Comment)
            {
                return "A document contains a disallowed child kind.";
            }
        }

        if (elementCount > 1 || doctypeCount > 1)
        {
            return "A document contains duplicate element or doctype children.";
        }

        if (document.GetDocumentElement() != documentElement)
        {
            return "GetDocumentElement disagrees with the document children.";
        }

        return "";
    }
}
