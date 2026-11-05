using System;
using System.Collections.Generic;

namespace xml;

internal class TreeBuilder
{
    internal XmlDocument Metadata;
    internal bool Fragment;
    internal XmlDocument Document;
    internal XmlNode Container;
    internal List<XmlNode> OpenParents;
    internal List<XmlElementDeclaration> ElementDeclarations;
    internal List<XmlAttributeDeclaration> AttributeDeclarations;
    internal List<XmlEntityDeclaration> EntityDeclarations;
    internal List<XmlNotationDeclaration> NotationDeclarations;
    internal List<XmlDiagnostic> Diagnostics;
    internal XmlDocumentType? RetainedDocumentType;
    internal long NextIdentity;
    internal bool Built;

    internal TreeBuilder(XmlDocument metadata, bool fragment)
    {
        Guard.NotNull(metadata, "metadata");
        Metadata = metadata;
        Fragment = fragment;
        Document = new XmlDocument();
        Container = new XmlNode();
        OpenParents = new List<XmlNode>();
        ElementDeclarations = new List<XmlElementDeclaration>();
        AttributeDeclarations = new List<XmlAttributeDeclaration>();
        EntityDeclarations = new List<XmlEntityDeclaration>();
        NotationDeclarations = new List<XmlNotationDeclaration>();
        Diagnostics = new List<XmlDiagnostic>();
        RetainedDocumentType = null;
        NextIdentity = 1;
        Built = false;
    }

    internal XmlDocument BuildXmlEventTree(XmlEvent[] events)
    {
        Guard.NotNull(events, "events");
        if (Built)
        {
            throw new InvalidOperationException("An XML event tree builder can build only one document.");
        }

        Built = true;
        InitializeXmlEventDocument();
        int index = 0;
        while (index < events.Length)
        {
            XmlEvent? optionalItem = events[index];
            if (optionalItem == null)
            {
                throw new ArgumentException("An XML event stream cannot contain a null event.", "events");
            }

            ProcessXmlTreeEvent(optionalItem);
            index = index + 1;
        }

        if (OpenParents.Count != 0)
        {
            throw new ArgumentException("The XML event stream ended with an open element or entity.", "events");
        }

        CompleteXmlEventDocument();
        TreeInvariants invariants = new TreeInvariants();
        invariants.ValidateXmlDocumentTree(Document);
        return Document;
    }

    internal void InitializeXmlEventDocument()
    {
        Document = new XmlDocument();
        Document.Version = Metadata.Version;
        Document.Standalone = Metadata.Standalone;
        Document.DocumentUri = Metadata.DocumentUri;
        Document.BaseUri = Metadata.BaseUri;
        Document.EncodingName = Metadata.EncodingName;
        Document.Diagnostics = [];
        Document.MutationVersion = 0;
        Document.IsReadOnly = false;
        NextIdentity = 1;
        XmlNodeKind kind = Fragment ? XmlNodeKind.DocumentFragment : XmlNodeKind.Document;
        Container = CreateXmlEventTreeNode(kind, null, null, Metadata.DocumentNode == null ? null : Metadata.DocumentNode.Range);
        Document.DocumentNode = Container;
    }

    internal void ProcessXmlTreeEvent(XmlEvent item)
    {
        if (item.Kind == XmlEventKind.StartDocument || item.Kind == XmlEventKind.EndDocument || item.Kind == XmlEventKind.StartNamespace || item.Kind == XmlEventKind.EndNamespace || item.Kind == XmlEventKind.EndDtd)
        {
            return;
        }

        if (item.Kind == XmlEventKind.StartDtd)
        {
            AddXmlEventDocumentType(item);
            return;
        }

        if (item.Kind == XmlEventKind.ElementDeclaration || item.Kind == XmlEventKind.AttributeDeclaration || item.Kind == XmlEventKind.EntityDeclaration || item.Kind == XmlEventKind.NotationDeclaration)
        {
            RetainXmlEventDeclaration(item);
            return;
        }

        if (item.Kind == XmlEventKind.StartElement)
        {
            StartXmlEventElement(item);
            return;
        }

        if (item.Kind == XmlEventKind.EndElement)
        {
            EndXmlEventElement(item);
            return;
        }

        if (item.Kind == XmlEventKind.StartEntity)
        {
            StartXmlEventEntity(item);
            return;
        }

        if (item.Kind == XmlEventKind.EndEntity)
        {
            EndXmlEventEntity(item);
            return;
        }

        if (item.Kind == XmlEventKind.SkippedEntity)
        {
            XmlNode skipped = CreateXmlEventTreeNode(XmlNodeKind.EntityReference, CopyXmlEventTreeName(item.Name), null, item.Range);
            AppendXmlEventTreeChild(CurrentXmlEventTreeParent(), skipped);
            return;
        }

        if (item.Kind == XmlEventKind.Text || item.Kind == XmlEventKind.IgnorableWhitespace)
        {
            AddXmlEventValueNode(XmlNodeKind.Text, item.Value, item.Range);
            return;
        }

        if (item.Kind == XmlEventKind.CData)
        {
            AddXmlEventValueNode(XmlNodeKind.CData, item.Value, item.Range);
            return;
        }

        if (item.Kind == XmlEventKind.Comment)
        {
            AddXmlEventValueNode(XmlNodeKind.Comment, item.Value, item.Range);
            return;
        }

        if (item.Kind == XmlEventKind.ProcessingInstruction)
        {
            XmlNode processingInstruction = CreateXmlEventTreeNode(XmlNodeKind.ProcessingInstruction, CopyXmlEventTreeName(item.Name), item.Value, item.Range);
            AppendXmlEventTreeChild(CurrentXmlEventTreeParent(), processingInstruction);
            return;
        }

        if (item.Kind == XmlEventKind.Diagnostic)
        {
            if (item.Diagnostic != null)
            {
                Diagnostics.Add(item.Diagnostic);
            }

            return;
        }

        throw new ArgumentOutOfRangeException("item.Kind");
    }

    internal void StartXmlEventElement(XmlEvent item)
    {
        XmlNode parent = CurrentXmlEventTreeParent();
        XmlNode element = CreateXmlEventTreeNode(XmlNodeKind.Element, CopyXmlEventTreeName(item.Name), null, item.Range);
        element.Attributes = CreateXmlEventAttributeNodes(item.Attributes, element);
        element.NamespaceBindings = CopyXmlEventNamespaceBindings(item.NamespaceBindings);
        AppendXmlEventTreeChild(parent, element);
        if (!Fragment && parent == Container && Document.DocumentElement == null)
        {
            Document.DocumentElement = element;
        }

        OpenParents.Add(element);
    }

    internal void EndXmlEventElement(XmlEvent item)
    {
        if (OpenParents.Count == 0)
        {
            throw new ArgumentException("An end-element event has no matching start element.", "item");
        }

        int index = OpenParents.Count - 1;
        XmlNode element = OpenParents[index];
        if (element.Kind != XmlNodeKind.Element || !SameXmlEventTreeName(element.Name, item.Name))
        {
            throw new ArgumentException("An end-element event does not match the open element.", "item");
        }

        OpenParents.RemoveAt(index);
        if (element.Range != null && item.Range != null && item.Range.End != null)
        {
            element.Range.End = CopyXmlEventTreeLocation(item.Range.End);
        }
    }

    internal void StartXmlEventEntity(XmlEvent item)
    {
        XmlNode entity = CreateXmlEventTreeNode(XmlNodeKind.EntityReference, CopyXmlEventTreeName(item.Name), null, item.Range);
        AppendXmlEventTreeChild(CurrentXmlEventTreeParent(), entity);
        OpenParents.Add(entity);
    }

    internal void EndXmlEventEntity(XmlEvent item)
    {
        if (OpenParents.Count == 0)
        {
            throw new ArgumentException("An end-entity event has no matching start entity.", "item");
        }

        int index = OpenParents.Count - 1;
        XmlNode entity = OpenParents[index];
        if (entity.Kind != XmlNodeKind.EntityReference || !SameXmlEventTreeName(entity.Name, item.Name))
        {
            throw new ArgumentException("An end-entity event does not match the open entity.", "item");
        }

        OpenParents.RemoveAt(index);
        if (entity.Range != null && item.Range != null && item.Range.End != null)
        {
            entity.Range.End = CopyXmlEventTreeLocation(item.Range.End);
        }
    }

    internal void AddXmlEventValueNode(XmlNodeKind kind, string? value, SourceRange? range)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }

        XmlNode parent = CurrentXmlEventTreeParent();
        if (kind == XmlNodeKind.Text && parent.Children != null && parent.Children.Length > 0)
        {
            XmlNode previous = parent.Children[parent.Children.Length - 1];
            if (previous.Kind == XmlNodeKind.Text)
            {
                previous.Value = (previous.Value == null ? "" : previous.Value) + value;
                if (previous.Range != null && range != null && range.End != null)
                {
                    previous.Range.End = CopyXmlEventTreeLocation(range.End);
                }

                return;
            }
        }

        XmlNode node = CreateXmlEventTreeNode(kind, null, value, range);
        AppendXmlEventTreeChild(parent, node);
    }

    internal void AddXmlEventDocumentType(XmlEvent item)
    {
        XmlDocumentType? source = Metadata.DocumentType;
        if (source == null)
        {
            return;
        }

        XmlDocumentType documentType = new XmlDocumentType();
        documentType.Name = CopyXmlEventTreeName(item.Name == null ? source.Name : item.Name);
        documentType.PublicIdentifier = item.PublicIdentifier;
        documentType.SystemIdentifier = item.SystemIdentifier;
        documentType.InternalSubset = source.InternalSubset;
        documentType.BaseUri = source.BaseUri;
        documentType.ElementDeclarations = [];
        documentType.AttributeDeclarations = [];
        documentType.EntityDeclarations = [];
        documentType.NotationDeclarations = [];
        documentType.Range = CopyXmlEventTreeRange(item.Range == null ? source.Range : item.Range);
        RetainedDocumentType = documentType;
        Document.DocumentType = documentType;

        XmlNode node = CreateXmlEventTreeNode(XmlNodeKind.DocumentType, CopyXmlEventTreeName(documentType.Name), null, documentType.Range);
        AppendXmlEventTreeChild(Container, node);
    }

    internal void RetainXmlEventDeclaration(XmlEvent item)
    {
        if (RetainedDocumentType == null)
        {
            return;
        }

        if (item.Kind == XmlEventKind.ElementDeclaration && item.ElementDeclaration != null)
        {
            ElementDeclarations.Add(item.ElementDeclaration);
        }
        else if (item.Kind == XmlEventKind.AttributeDeclaration && item.AttributeDeclaration != null)
        {
            AttributeDeclarations.Add(item.AttributeDeclaration);
        }
        else if (item.Kind == XmlEventKind.EntityDeclaration && item.EntityDeclaration != null)
        {
            EntityDeclarations.Add(item.EntityDeclaration);
        }
        else if (item.Kind == XmlEventKind.NotationDeclaration && item.NotationDeclaration != null)
        {
            NotationDeclarations.Add(item.NotationDeclaration);
        }
    }

    internal void CompleteXmlEventDocument()
    {
        Document.Diagnostics = Diagnostics.ToArray();
        Document.NextIdentity = NextIdentity;
        XmlDocumentType? optionalDocumentType = RetainedDocumentType;
        if (optionalDocumentType != null)
        {
            optionalDocumentType.ElementDeclarations = ElementDeclarations.ToArray();
            optionalDocumentType.AttributeDeclarations = AttributeDeclarations.ToArray();
            optionalDocumentType.EntityDeclarations = EntityDeclarations.ToArray();
            optionalDocumentType.NotationDeclarations = NotationDeclarations.ToArray();
        }
    }

    internal XmlNode CurrentXmlEventTreeParent()
    {
        if (OpenParents.Count == 0)
        {
            return Container;
        }

        return OpenParents[OpenParents.Count - 1];
    }

    internal void AppendXmlEventTreeChild(XmlNode parent, XmlNode child)
    {
        int count = parent.Children == null ? 0 : parent.Children.Length;
        XmlNode[] children = new XmlNode[count + 1];
        if (parent.Children != null)
        {
            Array.Copy(parent.Children, children, count);
        }

        children[count] = child;
        parent.Children = children;
        child.Parent = parent;
    }

    internal XmlNode CreateXmlEventTreeNode(XmlNodeKind kind, XmlName? name, string? value, SourceRange? range)
    {
        XmlNode node = new XmlNode();
        node.Kind = kind;
        node.Name = name;
        node.Value = value;
        node.OwnerDocument = Document;
        node.Children = [];
        node.Range = CopyXmlEventTreeRange(range);
        node.Identity = NextIdentity;
        node.MutationVersion = 0;
        node.IsReadOnly = false;
        NextIdentity = NextIdentity + 1;
        return node;
    }

    internal XmlNode[] CreateXmlEventAttributeNodes(XmlAttributeData[]? attributes, XmlNode element)
    {
        if (attributes == null)
        {
            return [];
        }

        XmlNode[] nodes = new XmlNode[attributes.Length];
        int index = 0;
        while (index < attributes.Length)
        {
            XmlAttributeData source = attributes[index];
            XmlNode attribute = CreateXmlEventTreeNode(XmlNodeKind.Attribute, CopyXmlEventTreeName(source.Name), source.Value, source.Range);
            attribute.Parent = element;
            nodes[index] = attribute;
            index = index + 1;
        }

        return nodes;
    }

    internal XmlName? CopyXmlEventTreeName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName copy = new XmlName();
        copy.Prefix = source.Prefix;
        copy.LocalName = source.LocalName;
        copy.NamespaceUri = source.NamespaceUri;
        copy.QualifiedName = source.QualifiedName;
        return copy;
    }

    internal XmlNamespaceBinding[]? CopyXmlEventNamespaceBindings(XmlNamespaceBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlNamespaceBinding[] copies = new XmlNamespaceBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlNamespaceBinding binding = new XmlNamespaceBinding();
            binding.Prefix = source[index].Prefix;
            binding.NamespaceUri = source[index].NamespaceUri;
            binding.IsUndeclaration = source[index].IsUndeclaration;
            binding.Range = CopyXmlEventTreeRange(source[index].Range);
            copies[index] = binding;
            index = index + 1;
        }

        return copies;
    }

    internal SourceRange? CopyXmlEventTreeRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange copy = new SourceRange();
        copy.Start = CopyXmlEventTreeLocation(source.Start);
        copy.End = CopyXmlEventTreeLocation(source.End);
        return copy;
    }

    internal SourceLocation? CopyXmlEventTreeLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation copy = new SourceLocation();
        copy.SourceUri = source.SourceUri;
        copy.CharacterOffset = source.CharacterOffset;
        copy.ByteOffset = source.ByteOffset;
        copy.Line = source.Line;
        copy.Column = source.Column;
        return copy;
    }

    internal bool SameXmlEventTreeName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return string.Equals(left.LocalName, right.LocalName, StringComparison.Ordinal) && string.Equals(left.NamespaceUri, right.NamespaceUri, StringComparison.Ordinal);
    }
}
