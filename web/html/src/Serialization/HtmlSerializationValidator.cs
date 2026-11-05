using System;
using System.Collections.Generic;
using System.Threading;

namespace html;

internal static class HtmlSerializationValidator
{
    internal static void Validate(HtmlNode root,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        HtmlDocument document = root.GetOwnerDocumentStorage();
        if (document == null)
        {
            ThrowMalformed("A node has no owner document.");
            return;
        }

        if (root.GetKindStorage() == HtmlNodeKind.Document
            && document.GetRootStorage() != root)
        {
            ThrowMalformed(
                "A document node is not its owner document's root.");
        }

        HashSet<HtmlNode> visited = new HashSet<HtmlNode>();
        ValidateNode(root, document, null, null, null, false, visited,
            cancellationToken);
    }

    internal static void ValidateNode(HtmlNode node, HtmlDocument? document,
        HtmlNode? expectedParent, HtmlNode? expectedPrevious,
        HtmlNode? expectedNext, bool validateLinks,
        HashSet<HtmlNode> visited, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (node == null)
        {
            ThrowMalformed("A child collection contains a null node.");
            return;
        }

        if (!visited.Add(node))
        {
            ThrowMalformed(
                "A node is reachable more than once or forms a cycle.");
        }

        if (node.GetOwnerDocumentStorage() != document)
        {
            ThrowMalformed("A node belongs to a different owner document.");
        }

        if (validateLinks
            && (node.GetParentStorage() != expectedParent
                || node.GetPreviousSiblingStorage() != expectedPrevious
                || node.GetNextSiblingStorage() != expectedNext))
        {
            ThrowMalformed("Parent or sibling links are inconsistent.");
        }

        HtmlNodeKind kind = node.GetKindStorage();
        ValidateKind(kind);
        ValidateAttributes(node, kind, cancellationToken);

        HtmlNode[] children = node.GetChildrenStorage();
        bool container = kind == HtmlNodeKind.Document
            || kind == HtmlNodeKind.Fragment
            || kind == HtmlNodeKind.Element;
        if (!container && children.Length != 0)
        {
            ThrowMalformed("A leaf node contains child nodes.");
        }

        HtmlNode? expectedFirst = children.Length == 0 ? null : children[0];
        HtmlNode? expectedLast = children.Length == 0
            ? null : children[children.Length - 1];
        if (node.GetFirstChildStorage() != expectedFirst
            || node.GetLastChildStorage() != expectedLast)
        {
            ThrowMalformed("First or last child links are inconsistent.");
        }

        ValidateDocumentChildren(kind, children);
        int index = 0;
        while (index < children.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlNode child = children[index];
            ValidateChildKind(kind, child);
            HtmlNode? previous = index == 0 ? null : children[index - 1];
            HtmlNode? next = index + 1 == children.Length
                ? null : children[index + 1];
            ValidateNode(child, document, node, previous, next, true, visited,
                cancellationToken);
            index = index + 1;
        }

        ValidateTemplate(node, kind, document, visited, cancellationToken);
    }

    internal static void ValidateKind(HtmlNodeKind kind)
    {
        switch (kind)
        {
            case HtmlNodeKind.Document:
            case HtmlNodeKind.Fragment:
            case HtmlNodeKind.DocumentType:
            case HtmlNodeKind.Element:
            case HtmlNodeKind.Text:
            case HtmlNodeKind.Comment:
                return;
            default:
                ThrowMalformed("A node has an unknown stored kind.");
                return;
        }
    }

    internal static void ValidateAttributes(HtmlNode node, HtmlNodeKind kind,
        CancellationToken cancellationToken)
    {
        HtmlAttribute[] attributes = node.GetAttributesStorage();
        if (kind != HtmlNodeKind.Element && attributes.Length != 0)
        {
            ThrowMalformed("A non-element node contains attributes.");
        }

        int index = 0;
        while (index < attributes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlAttribute attribute = attributes[index];
            if (attribute == null)
            {
                ThrowMalformed("An element contains a null attribute.");
                return;
            }

            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                HtmlAttribute previous = attributes[duplicateIndex];
                if (previous.GetNamespaceUri() == attribute.GetNamespaceUri()
                    && previous.GetLocalName() == attribute.GetLocalName())
                {
                    ThrowMalformed(
                        "An element contains duplicate expanded attribute names.");
                }

                duplicateIndex = duplicateIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateDocumentChildren(HtmlNodeKind kind,
        HtmlNode[] children)
    {
        if (kind != HtmlNodeKind.Document)
        {
            return;
        }

        int elementCount = 0;
        int doctypeCount = 0;
        bool elementSeen = false;
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child != null)
            {
                HtmlNodeKind childKind = child.GetKindStorage();
                if (childKind == HtmlNodeKind.Element)
                {
                    elementCount = elementCount + 1;
                    elementSeen = true;
                }
                else if (childKind == HtmlNodeKind.DocumentType)
                {
                    doctypeCount = doctypeCount + 1;
                    if (elementSeen)
                    {
                        ThrowMalformed(
                            "A document type follows the document element.");
                    }
                }
            }

            index = index + 1;
        }

        if (elementCount > 1 || doctypeCount > 1)
        {
            ThrowMalformed(
                "A document has duplicate element or document-type children.");
        }
    }

    internal static void ValidateChildKind(HtmlNodeKind parentKind, HtmlNode child)
    {
        if (child == null)
        {
            ThrowMalformed("A child collection contains a null node.");
            return;
        }

        HtmlNodeKind childKind = child.GetKindStorage();
        if (parentKind == HtmlNodeKind.Document)
        {
            if (childKind != HtmlNodeKind.Element
                && childKind != HtmlNodeKind.Comment
                && childKind != HtmlNodeKind.DocumentType)
            {
                ThrowMalformed("A document contains a disallowed node kind.");
            }

            return;
        }

        if ((parentKind == HtmlNodeKind.Fragment
                || parentKind == HtmlNodeKind.Element)
            && childKind != HtmlNodeKind.Element
            && childKind != HtmlNodeKind.Text
            && childKind != HtmlNodeKind.Comment)
        {
            ThrowMalformed("A container contains a disallowed node kind.");
        }
    }

    internal static void ValidateTemplate(HtmlNode node, HtmlNodeKind kind,
        HtmlDocument? document, HashSet<HtmlNode> visited,
        CancellationToken cancellationToken)
    {
        HtmlNode? content = node.GetTemplateContentStorage();
        HtmlNode? host = node.GetTemplateHostStorage();
        if (host != null)
        {
            if (kind != HtmlNodeKind.Fragment
                || host.GetTemplateContentStorage() != node
                || node.GetParentStorage() != null)
            {
                ThrowMalformed("Template-content ownership is inconsistent.");
            }
        }

        if (content == null)
        {
            if (kind == HtmlNodeKind.Element
                && node.GetNamespaceStorage() == HtmlNamespace.Html
                && node.GetLocalNameStorage() == "template")
            {
                ThrowMalformed(
                    "An HTML template element has no content fragment.");
            }

            return;
        }

        if (kind != HtmlNodeKind.Element
            || node.GetNamespaceStorage() != HtmlNamespace.Html
            || node.GetLocalNameStorage() != "template"
            || content.GetKindStorage() != HtmlNodeKind.Fragment
            || content.GetTemplateHostStorage() != node
            || content.GetParentStorage() != null
            || node.GetChildrenStorage().Length != 0)
        {
            ThrowMalformed("Template-content ownership is inconsistent.");
        }

        ValidateNode(content, document, null, null, null, true, visited,
            cancellationToken);
    }

    internal static void ThrowMalformed(string message)
    {
        throw new HtmlSerializationException(
            "Cannot serialize a malformed HTML tree. " + message);
    }
}
