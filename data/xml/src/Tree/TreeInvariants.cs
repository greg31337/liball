using System;
using System.Collections.Generic;

namespace xml;

internal class TreeInvariants
{
    internal TreeInvariants()
    {
    }

    internal void ValidateXmlDocumentTree(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        XmlNode? optionalContainer = document.DocumentNode;
        if (optionalContainer == null)
        {
            throw new ArgumentException("An XML document requires a document container.", "document");
        }

        if (optionalContainer.Kind != XmlNodeKind.Document && optionalContainer.Kind != XmlNodeKind.DocumentFragment)
        {
            throw new ArgumentException("An XML document container must be a document or document-fragment node.", "document");
        }

        if (optionalContainer.Parent != null)
        {
            throw new ArgumentException("An XML document container cannot have a parent.", "document");
        }

        if (optionalContainer.OwnerDocument != document)
        {
            throw new ArgumentException("The XML document container must be owned by its document.", "document");
        }

        ValidateXmlContainerChildren(document, optionalContainer);
        HashSet<XmlNode> ancestors = new HashSet<XmlNode>();
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        ValidateXmlOwnedSubtree(optionalContainer, null, document, ancestors, visited);
    }

    internal void ValidateXmlChildPlacement(XmlNode parent, XmlNode child)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(child, "child");
        EnsureXmlTreeMutationAllowed(parent);
        EnsureXmlTreeMutationAllowed(child);
        if (!IsXmlLegalChildKind(parent.Kind, child.Kind))
        {
            throw new ArgumentException("The child node kind is not permitted for the parent node.", "child");
        }

        XmlDocument? parentDocument = parent.OwnerDocument;
        if (parentDocument == null || child.OwnerDocument != parentDocument)
        {
            throw new ArgumentException("A child and parent must have the same owner document.", "child");
        }

        HashSet<XmlNode> ancestors = new HashSet<XmlNode>();
        XmlNode? ancestor = parent;
        while (ancestor != null)
        {
            if (!ancestors.Add(ancestor))
            {
                throw new ArgumentException("The XML parent chain already contains a cycle.", "parent");
            }

            if (ancestor == child)
            {
                throw new ArgumentException("An XML tree mutation cannot create a cycle.", "child");
            }

            ancestor = ancestor.Parent;
        }
    }

    internal void ValidateXmlChildSequence(XmlNode parent, XmlNode[] children)
    {
        Guard.NotNull(parent, "parent");
        Guard.NotNull(children, "children");
        int index = 0;
        while (index < children.Length)
        {
            XmlNode? optionalChild = children[index];
            if (optionalChild == null)
            {
                throw new ArgumentException("An XML child sequence cannot contain a null node.", "children");
            }

            if (!IsXmlLegalChildKind(parent.Kind, optionalChild.Kind))
            {
                throw new ArgumentException("The child node kind is not permitted for the parent node.", "children");
            }

            int preceding = 0;
            while (preceding < index)
            {
                if (children[preceding] == optionalChild)
                {
                    throw new ArgumentException("An XML child sequence cannot contain the same node twice.", "children");
                }

                preceding = preceding + 1;
            }

            index = index + 1;
        }

        if (parent.Kind == XmlNodeKind.Document)
        {
            ValidateXmlDocumentChildSequence(children);
        }
        else if (parent.Kind == XmlNodeKind.DocumentFragment)
        {
            int fragmentIndex = 0;
            while (fragmentIndex < children.Length)
            {
                if (children[fragmentIndex].Kind == XmlNodeKind.DocumentType)
                {
                    throw new ArgumentException("A document fragment cannot contain a document type.", "children");
                }

                fragmentIndex = fragmentIndex + 1;
            }
        }
    }

    internal void ValidateXmlAttributePlacement(XmlNode element, XmlNode attribute, XmlNode? replaced)
    {
        Guard.NotNull(element, "element");
        Guard.NotNull(attribute, "attribute");
        EnsureXmlTreeMutationAllowed(element);
        EnsureXmlTreeMutationAllowed(attribute);
        if (element.Kind != XmlNodeKind.Element || attribute.Kind != XmlNodeKind.Attribute)
        {
            throw new ArgumentException("Only an attribute node can be attached to an element.", "attribute");
        }

        if (element.OwnerDocument == null || attribute.OwnerDocument != element.OwnerDocument)
        {
            throw new ArgumentException("An attribute and element must have the same owner document.", "attribute");
        }

        if (attribute.Name == null)
        {
            throw new ArgumentException("An attribute node requires a name.", "attribute");
        }

        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode existing = attributes[index];
            if (existing != replaced && existing != attribute && SameXmlExpandedName(existing.Name, attribute.Name))
            {
                throw new ArgumentException("An element cannot contain duplicate expanded attribute names.", "attribute");
            }

            index = index + 1;
        }
    }

    internal void ValidateXmlAttributeSequence(XmlNode element, XmlNode[] attributes)
    {
        Guard.NotNull(element, "element");
        Guard.NotNull(attributes, "attributes");
        if (element.Kind != XmlNodeKind.Element)
        {
            throw new ArgumentException("Only an element can contain attributes.", "element");
        }

        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode? optionalAttribute = attributes[index];
            if (optionalAttribute == null || optionalAttribute.Kind != XmlNodeKind.Attribute || optionalAttribute.Name == null)
            {
                throw new ArgumentException("An element attribute sequence must contain named attribute nodes.", "attributes");
            }

            int preceding = 0;
            while (preceding < index)
            {
                if (attributes[preceding] == optionalAttribute || SameXmlExpandedName(attributes[preceding].Name, optionalAttribute.Name))
                {
                    throw new ArgumentException("An element cannot contain duplicate attributes or expanded attribute names.", "attributes");
                }

                preceding = preceding + 1;
            }

            index = index + 1;
        }
    }

    internal void EnsureXmlTreeMutationAllowed(XmlNode node)
    {
        Guard.NotNull(node, "node");
        XmlDocument? document = node.OwnerDocument;
        if (document != null && document.IsReadOnly)
        {
            throw new InvalidOperationException("The owner document is read-only.");
        }

        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        XmlNode? current = node;
        while (current != null)
        {
            if (!visited.Add(current))
            {
                throw new InvalidOperationException("The XML parent chain contains a cycle.");
            }

            if (current.IsReadOnly)
            {
                throw new InvalidOperationException("The XML node or one of its ancestors is read-only.");
            }

            current = current.Parent;
        }
    }

    internal void ValidateXmlContainerChildren(XmlDocument document, XmlNode container)
    {
        XmlNode[] children = container.Children == null ? [] : container.Children;
        ValidateXmlChildSequence(container, children);
        if (container.Kind == XmlNodeKind.DocumentFragment)
        {
            if (document.DocumentElement != null || document.DocumentType != null)
            {
                throw new ArgumentException("A document fragment cannot expose document-element or document-type metadata.", "document");
            }

            return;
        }

        XmlNode? element = null;
        int documentTypeCount = 0;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                element = children[index];
            }
            else if (children[index].Kind == XmlNodeKind.DocumentType)
            {
                documentTypeCount = documentTypeCount + 1;
            }

            index = index + 1;
        }

        if (element == null || document.DocumentElement != element)
        {
            throw new ArgumentException("A document must expose its single document element.", "document");
        }

        if ((documentTypeCount == 0 && document.DocumentType != null) || (documentTypeCount == 1 && document.DocumentType == null))
        {
            throw new ArgumentException("Document-type node and metadata retention must agree.", "document");
        }
    }

    internal void ValidateXmlDocumentChildSequence(XmlNode[] children)
    {
        int elementCount = 0;
        int documentTypeCount = 0;
        bool sawElement = false;
        int index = 0;
        while (index < children.Length)
        {
            XmlNodeKind kind = children[index].Kind;
            if (kind == XmlNodeKind.Element)
            {
                elementCount = elementCount + 1;
                sawElement = true;
            }
            else if (kind == XmlNodeKind.DocumentType)
            {
                documentTypeCount = documentTypeCount + 1;
                if (sawElement)
                {
                    throw new ArgumentException("A document type must precede the document element.", "children");
                }
            }

            index = index + 1;
        }

        if (elementCount != 1)
        {
            throw new ArgumentException("A document must contain exactly one document element.", "children");
        }

        if (documentTypeCount > 1)
        {
            throw new ArgumentException("A document can contain at most one document type.", "children");
        }
    }

    internal void ValidateXmlOwnedSubtree(XmlNode node, XmlNode? expectedParent, XmlDocument document, HashSet<XmlNode> ancestors, HashSet<XmlNode> visited)
    {
        if (ancestors.Contains(node))
        {
            throw new ArgumentException("The XML tree contains a cycle.", "document");
        }

        if (!visited.Add(node))
        {
            throw new ArgumentException("The same XML node appears more than once in the tree.", "document");
        }

        if (node.Parent != expectedParent)
        {
            throw new ArgumentException("An XML node parent link does not match its container.", "document");
        }

        if (node.OwnerDocument != document)
        {
            throw new ArgumentException("Every XML tree node must have the same owner document.", "document");
        }

        ancestors.Add(node);
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        if (node.Kind != XmlNodeKind.Element && attributes.Length != 0)
        {
            throw new ArgumentException("Only element nodes can contain attributes.", "document");
        }

        if (node.Kind == XmlNodeKind.Element)
        {
            ValidateXmlAttributeSequence(node, attributes);
        }

        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            ValidateXmlOwnedSubtree(attributes[attributeIndex], node, document, ancestors, visited);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            ValidateXmlChildSequence(node, children);
        }

        int childIndex = 0;
        while (childIndex < children.Length)
        {
            if (!IsXmlLegalChildKind(node.Kind, children[childIndex].Kind))
            {
                throw new ArgumentException("The XML tree contains an illegal parent and child node combination.", "document");
            }

            ValidateXmlOwnedSubtree(children[childIndex], node, document, ancestors, visited);
            childIndex = childIndex + 1;
        }

        ancestors.Remove(node);
    }

    internal bool IsXmlLegalChildKind(XmlNodeKind parent, XmlNodeKind child)
    {
        if (parent == XmlNodeKind.Document)
        {
            return child == XmlNodeKind.Element || child == XmlNodeKind.ProcessingInstruction || child == XmlNodeKind.Comment || child == XmlNodeKind.DocumentType;
        }

        if (parent == XmlNodeKind.DocumentFragment || parent == XmlNodeKind.Element || parent == XmlNodeKind.EntityReference)
        {
            return child == XmlNodeKind.Element || child == XmlNodeKind.Text || child == XmlNodeKind.CData || child == XmlNodeKind.EntityReference || child == XmlNodeKind.ProcessingInstruction || child == XmlNodeKind.Comment;
        }

        return false;
    }

    internal bool SameXmlExpandedName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        string? leftLocal = left.LocalName == null ? left.QualifiedName : left.LocalName;
        string? rightLocal = right.LocalName == null ? right.QualifiedName : right.LocalName;
        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return string.Equals(leftLocal, rightLocal, StringComparison.Ordinal) && string.Equals(leftNamespace, rightNamespace, StringComparison.Ordinal);
    }
}
