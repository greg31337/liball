using System;
using System.Collections.Generic;

namespace xml;

internal class TreeEmission
{
    internal XmlParserOptions Options;
    internal List<XmlEvent> Events;
    internal TreeMutation Mutation;
    internal TreeNavigation Navigation;

    internal TreeEmission()
    {
        Options = new XmlParserOptions();
        Events = new List<XmlEvent>();
        Mutation = new TreeMutation();
        Navigation = new TreeNavigation();
    }

    internal XmlEventBatch EmitXmlNodeBatch(XmlNode node, XmlParserOptions options)
    {
        Guard.NotNull(node, "node");
        BeginXmlTreeEmission(options);
        ValidateXmlEmissionNode(node);
        if (node.Kind == XmlNodeKind.Document)
        {
            EmitXmlDocumentEvents(node);
        }
        else if (node.Kind == XmlNodeKind.DocumentFragment)
        {
            EmitXmlFragmentEvents(node);
        }
        else
        {
            EmitXmlTreeNode(node, 0, true);
        }

        return CompleteXmlTreeEmission();
    }

    internal XmlEventBatch EmitXmlSelectionBatch(XmlNodeSelection selection, XmlParserOptions options)
    {
        Guard.NotNull(selection, "selection");
        BeginXmlTreeEmission(options);
        XmlNode[] nodes = selection.Nodes == null ? [] : selection.Nodes;
        XmlDocument? document = selection.Document;
        if (!selection.IsFrozen && document != null && selection.MutationVersion != document.MutationVersion)
        {
            throw new InvalidOperationException("The XML node selection was invalidated by a document mutation.");
        }

        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode? optionalNode = nodes[index];
            if (optionalNode == null)
            {
                throw new ArgumentException("An XML node selection cannot contain a null node.", "selection");
            }

            XmlNode node = optionalNode;
            if (!selection.IsFrozen && document != null && node.OwnerDocument != document)
            {
                throw new ArgumentException("Every live selected node must belong to the selection document.", "selection");
            }

            ValidateXmlEmissionNode(node);
            if (node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.Namespace)
            {
                throw new ArgumentException("Standalone attributes and namespace nodes cannot be represented by normalized event boundaries.", "selection");
            }

            index = index + 1;
        }

        index = 0;
        while (index < nodes.Length)
        {
            if (!IsXmlEmissionCoveredSelectionNode(nodes, index))
            {
                XmlNode node = nodes[index];
                if (node.Kind == XmlNodeKind.Document)
                {
                    EmitXmlDocumentEvents(node);
                }
                else if (node.Kind == XmlNodeKind.DocumentFragment)
                {
                    EmitXmlFragmentEvents(node);
                }
                else
                {
                    EmitXmlTreeNode(node, 0, true);
                }
            }

            index = index + 1;
        }

        return CompleteXmlTreeEmission();
    }

    internal void BeginXmlTreeEmission(XmlParserOptions options)
    {
        Guard.NotNull(options, "options");
        if (options.NamespaceHandling != XmlNamespaceHandling.Disabled && options.NamespaceHandling != XmlNamespaceHandling.Namespaces10 && options.NamespaceHandling != XmlNamespaceHandling.Namespaces11)
        {
            throw new ArgumentOutOfRangeException("options.NamespaceHandling");
        }

        if (options.EntityHandling != XmlEntityHandling.Expand && options.EntityHandling != XmlEntityHandling.PreserveReferences)
        {
            throw new ArgumentOutOfRangeException("options.EntityHandling");
        }

        Options = options;
        Events = new List<XmlEvent>();
    }

    internal XmlEventBatch CompleteXmlTreeEmission()
    {
        XmlEventBatch batch = new XmlEventBatch();
        batch.Events = Events.ToArray();
        batch.Offset = 0;
        batch.Count = Events.Count;
        batch.Status = XmlParserStatus.Completed;
        batch.IsFinal = true;
        batch.ReaderState = null;
        return batch;
    }

    internal void ValidateXmlEmissionNode(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Document)
        {
            XmlDocument? document = node.OwnerDocument;
            if (document == null || document.DocumentNode != node)
            {
                throw new ArgumentException("A document event root must be its owner document container.", "node");
            }

            Mutation.Invariants.ValidateXmlDocumentTree(document);
            return;
        }

        Mutation.ValidateXmlMutationSource(node);
    }

    internal void EmitXmlDocumentEvents(XmlNode node)
    {
        Events.Add(CreateXmlEmissionEvent(XmlEventKind.StartDocument, XmlNodeKind.Document, null, null, 0, node.Range));
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            EmitXmlTreeNode(children[index], 0, false);
            index = index + 1;
        }

        XmlDocument? document = node.OwnerDocument;
        if (document != null && document.Diagnostics != null)
        {
            index = 0;
            while (index < document.Diagnostics.Length)
            {
                XmlDiagnostic diagnostic = document.Diagnostics[index];
                XmlEvent item = CreateXmlEmissionEvent(XmlEventKind.Diagnostic, XmlNodeKind.Document, null, null, 0, diagnostic.Range);
                item.Diagnostic = diagnostic;
                Events.Add(item);
                index = index + 1;
            }
        }

        Events.Add(CreateXmlEmissionEvent(XmlEventKind.EndDocument, XmlNodeKind.Document, null, null, 0, node.Range));
    }

    internal void EmitXmlFragmentEvents(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            EmitXmlTreeNode(children[index], 0, true);
            index = index + 1;
        }
    }

    internal void EmitXmlTreeNode(XmlNode node, int depth, bool subtreeRoot)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            EmitXmlElementEvents(node, depth, subtreeRoot);
            return;
        }

        if (node.Kind == XmlNodeKind.Text)
        {
            Events.Add(CreateXmlEmissionEvent(XmlEventKind.Text, XmlNodeKind.Text, null, node.Value, depth, node.Range));
            return;
        }

        if (node.Kind == XmlNodeKind.CData)
        {
            XmlEventKind kind = Options.PreserveCData ? XmlEventKind.CData : XmlEventKind.Text;
            XmlNodeKind nodeKind = Options.PreserveCData ? XmlNodeKind.CData : XmlNodeKind.Text;
            Events.Add(CreateXmlEmissionEvent(kind, nodeKind, null, node.Value, depth, node.Range));
            return;
        }

        if (node.Kind == XmlNodeKind.EntityReference)
        {
            EmitXmlEntityEvents(node, depth);
            return;
        }

        if (node.Kind == XmlNodeKind.Comment)
        {
            if (Options.PreserveComments)
            {
                Events.Add(CreateXmlEmissionEvent(XmlEventKind.Comment, XmlNodeKind.Comment, null, node.Value, depth, node.Range));
            }

            return;
        }

        if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            if (Options.PreserveProcessingInstructions)
            {
                Events.Add(CreateXmlEmissionEvent(XmlEventKind.ProcessingInstruction, XmlNodeKind.ProcessingInstruction, node.Name, node.Value, depth, node.Range));
            }

            return;
        }

        if (node.Kind == XmlNodeKind.DocumentType)
        {
            EmitXmlDocumentTypeEvents(node, depth);
            return;
        }

        if (node.Kind == XmlNodeKind.Document)
        {
            EmitXmlDocumentEvents(node);
            return;
        }

        if (node.Kind == XmlNodeKind.DocumentFragment)
        {
            EmitXmlFragmentEvents(node);
            return;
        }

        throw new ArgumentException("The XML node kind cannot be emitted as a standalone normalized event subtree.", "node");
    }

    internal void EmitXmlElementEvents(XmlNode node, int depth, bool subtreeRoot)
    {
        XmlNamespaceBinding[] declarations = SelectXmlEmissionDeclarations(node, subtreeRoot);
        int index = 0;
        if (Options.NamespaceHandling != XmlNamespaceHandling.Disabled)
        {
            while (index < declarations.Length)
            {
                XmlNamespaceBinding binding = declarations[index];
                XmlEvent startNamespace = CreateXmlEmissionEvent(XmlEventKind.StartNamespace, XmlNodeKind.Namespace, null, binding.NamespaceUri, depth, binding.Range);
                startNamespace.NamespaceBindings = [CopyXmlEmissionBinding(binding)];
                Events.Add(startNamespace);
                index = index + 1;
            }
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        XmlEvent start = CreateXmlEmissionEvent(XmlEventKind.StartElement, XmlNodeKind.Element, node.Name, null, depth, node.Range);
        start.Attributes = CreateXmlEmissionAttributes(node);
        start.NamespaceBindings = CopyXmlEmissionBindings(declarations);
        start.IsEmptyElement = children.Length == 0;
        Events.Add(start);
        index = 0;
        while (index < children.Length)
        {
            EmitXmlTreeNode(children[index], depth + 1, false);
            index = index + 1;
        }

        XmlEvent end = CreateXmlEmissionEvent(XmlEventKind.EndElement, XmlNodeKind.Element, node.Name, null, depth, node.Range);
        end.IsEmptyElement = children.Length == 0;
        Events.Add(end);
        if (Options.NamespaceHandling != XmlNamespaceHandling.Disabled)
        {
            index = declarations.Length - 1;
            while (index >= 0)
            {
                XmlNamespaceBinding binding = declarations[index];
                XmlEvent endNamespace = CreateXmlEmissionEvent(XmlEventKind.EndNamespace, XmlNodeKind.Namespace, null, binding.NamespaceUri, depth, binding.Range);
                endNamespace.NamespaceBindings = [CopyXmlEmissionBinding(binding)];
                Events.Add(endNamespace);
                index = index - 1;
            }
        }
    }

    internal void EmitXmlEntityEvents(XmlNode node, int depth)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        bool known = IsXmlEmissionKnownEntity(node);
        if (children.Length == 0 && !known)
        {
            Events.Add(CreateXmlEmissionEvent(XmlEventKind.SkippedEntity, XmlNodeKind.EntityReference, node.Name, null, depth, node.Range));
            return;
        }

        bool preserve = Options.PreserveEntityReferences || Options.EntityHandling == XmlEntityHandling.PreserveReferences;
        if (preserve)
        {
            Events.Add(CreateXmlEmissionEvent(XmlEventKind.StartEntity, XmlNodeKind.EntityReference, node.Name, null, depth, node.Range));
        }

        int index = 0;
        while (index < children.Length)
        {
            EmitXmlTreeNode(children[index], depth, false);
            index = index + 1;
        }

        if (preserve)
        {
            Events.Add(CreateXmlEmissionEvent(XmlEventKind.EndEntity, XmlNodeKind.EntityReference, node.Name, null, depth, node.Range));
        }
    }

    internal void EmitXmlDocumentTypeEvents(XmlNode node, int depth)
    {
        XmlDocumentType? documentType = node.OwnerDocument == null ? null : node.OwnerDocument.DocumentType;
        XmlName? name = documentType == null ? node.Name : documentType.Name;
        XmlEvent start = CreateXmlEmissionEvent(XmlEventKind.StartDtd, XmlNodeKind.DocumentType, name, null, depth, node.Range);
        if (documentType != null)
        {
            start.PublicIdentifier = documentType.PublicIdentifier;
            start.SystemIdentifier = documentType.SystemIdentifier;
        }

        Events.Add(start);
        if (documentType != null)
        {
            EmitXmlDtdDeclarations(documentType);
        }

        XmlEvent end = CreateXmlEmissionEvent(XmlEventKind.EndDtd, XmlNodeKind.DocumentType, name, null, depth, node.Range);
        end.PublicIdentifier = start.PublicIdentifier;
        end.SystemIdentifier = start.SystemIdentifier;
        Events.Add(end);
    }

    internal void EmitXmlDtdDeclarations(XmlDocumentType documentType)
    {
        XmlElementDeclaration[] elements = documentType.ElementDeclarations == null ? [] : documentType.ElementDeclarations;
        int index = 0;
        while (index < elements.Length)
        {
            XmlEvent item = CreateXmlEmissionEvent(XmlEventKind.ElementDeclaration, XmlNodeKind.DocumentType, elements[index].Name, null, 0, elements[index].Range);
            item.ElementDeclaration = elements[index];
            Events.Add(item);
            index = index + 1;
        }

        XmlAttributeDeclaration[] attributes = documentType.AttributeDeclarations == null ? [] : documentType.AttributeDeclarations;
        index = 0;
        while (index < attributes.Length)
        {
            XmlEvent item = CreateXmlEmissionEvent(XmlEventKind.AttributeDeclaration, XmlNodeKind.DocumentType, attributes[index].Name, null, 0, attributes[index].Range);
            item.AttributeDeclaration = attributes[index];
            Events.Add(item);
            index = index + 1;
        }

        XmlEntityDeclaration[] entities = documentType.EntityDeclarations == null ? [] : documentType.EntityDeclarations;
        index = 0;
        while (index < entities.Length)
        {
            XmlName entityName = CreateXmlEmissionLexicalName(entities[index].Name);
            XmlEvent item = CreateXmlEmissionEvent(XmlEventKind.EntityDeclaration, XmlNodeKind.DocumentType, entityName, entities[index].ReplacementText, 0, entities[index].Range);
            item.EntityDeclaration = entities[index];
            item.PublicIdentifier = entities[index].PublicIdentifier;
            item.SystemIdentifier = entities[index].SystemIdentifier;
            Events.Add(item);
            index = index + 1;
        }

        XmlNotationDeclaration[] notations = documentType.NotationDeclarations == null ? [] : documentType.NotationDeclarations;
        index = 0;
        while (index < notations.Length)
        {
            XmlName notationName = CreateXmlEmissionLexicalName(notations[index].Name);
            XmlEvent item = CreateXmlEmissionEvent(XmlEventKind.NotationDeclaration, XmlNodeKind.DocumentType, notationName, null, 0, notations[index].Range);
            item.NotationDeclaration = notations[index];
            item.PublicIdentifier = notations[index].PublicIdentifier;
            item.SystemIdentifier = notations[index].SystemIdentifier;
            Events.Add(item);
            index = index + 1;
        }
    }

    internal XmlEvent CreateXmlEmissionEvent(XmlEventKind kind, XmlNodeKind nodeKind, XmlName? name, string? value, int depth, SourceRange? range)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        item.NodeKind = nodeKind;
        item.Name = CopyXmlEmissionName(name);
        item.Value = value;
        item.Depth = depth;
        item.IsEmptyElement = false;
        item.IsIgnorableWhitespace = false;
        item.Range = Options.PreserveSourceLocations ? CopyXmlEmissionRange(range) : null;
        return item;
    }

    internal XmlAttributeData[] CreateXmlEmissionAttributes(XmlNode element)
    {
        XmlNode[] nodes = element.Attributes == null ? [] : element.Attributes;
        List<XmlAttributeData> result = new List<XmlAttributeData>();
        int index = 0;
        while (index < nodes.Length)
        {
            XmlNode node = nodes[index];
            bool namespaceDeclaration = IsXmlEmissionNamespaceAttribute(node);
            if (Options.RetainNamespaceDeclarations || !namespaceDeclaration)
            {
                XmlAttributeData attribute = new XmlAttributeData();
                attribute.Name = CopyXmlEmissionName(node.Name);
                attribute.Value = node.Value;
                attribute.RawValue = node.Value;
                attribute.Quote = '"';
                attribute.IsSpecified = true;
                attribute.IsDefaulted = false;
                attribute.IsFixed = false;
                attribute.IsNamespaceDeclaration = namespaceDeclaration;
                attribute.Range = Options.PreserveSourceLocations ? CopyXmlEmissionRange(node.Range) : null;
                result.Add(attribute);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XmlNamespaceBinding[] SelectXmlEmissionDeclarations(XmlNode element, bool subtreeRoot)
    {
        if (Options.NamespaceHandling == XmlNamespaceHandling.Disabled)
        {
            return [];
        }

        if (!subtreeRoot)
        {
            return CopyXmlEmissionBindings(element.NamespaceBindings == null ? [] : element.NamespaceBindings);
        }

        XmlNamespaceBinding[] inScope = Navigation.SelectXmlNamespacesOperation(element);
        List<XmlNamespaceBinding> result = new List<XmlNamespaceBinding>();
        int index = 0;
        while (index < inScope.Length)
        {
            string prefix = XmlEmissionBindingPrefix(inScope[index]);
            if (!string.Equals(prefix, "xml", StringComparison.Ordinal))
            {
                result.Add(CopyXmlEmissionBinding(inScope[index]));
            }

            index = index + 1;
        }

        if (element.Parent != null && element.Parent.Kind == XmlNodeKind.DocumentFragment && Options.FragmentNamespaceBindings != null)
        {
            XmlNamespaceBinding[] inherited = Options.FragmentNamespaceBindings;
            index = 0;
            while (index < inherited.Length)
            {
                string prefix = XmlEmissionBindingPrefix(inherited[index]);
                if (!ContainsXmlEmissionPrefix(result, prefix) && !string.Equals(prefix, "xml", StringComparison.Ordinal))
                {
                    result.Add(CopyXmlEmissionBinding(inherited[index]));
                }

                index = index + 1;
            }
        }

        return result.ToArray();
    }

    internal bool ContainsXmlEmissionPrefix(List<XmlNamespaceBinding> bindings, string prefix)
    {
        int index = 0;
        while (index < bindings.Count)
        {
            if (string.Equals(XmlEmissionBindingPrefix(bindings[index]), prefix, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string XmlEmissionBindingPrefix(XmlNamespaceBinding binding)
    {
        string? prefix = binding.Prefix;
        return prefix == null ? "" : prefix;
    }

    internal XmlNamespaceBinding[] CopyXmlEmissionBindings(XmlNamespaceBinding[] bindings)
    {
        XmlNamespaceBinding[] result = new XmlNamespaceBinding[bindings.Length];
        int index = 0;
        while (index < bindings.Length)
        {
            result[index] = CopyXmlEmissionBinding(bindings[index]);
            index = index + 1;
        }

        return result;
    }

    internal XmlNamespaceBinding CopyXmlEmissionBinding(XmlNamespaceBinding source)
    {
        XmlNamespaceBinding result = new XmlNamespaceBinding();
        result.Prefix = source.Prefix;
        result.NamespaceUri = source.NamespaceUri;
        result.IsUndeclaration = source.IsUndeclaration;
        result.Range = Options.PreserveSourceLocations ? CopyXmlEmissionRange(source.Range) : null;
        return result;
    }

    internal XmlName? CopyXmlEmissionName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = Options.PreservePrefixes ? source.Prefix : null;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = Options.PreservePrefixes ? source.QualifiedName : source.LocalName;
        return result;
    }

    internal XmlName CreateXmlEmissionLexicalName(string? value)
    {
        XmlName result = new XmlName();
        result.Prefix = null;
        result.LocalName = value;
        result.NamespaceUri = null;
        result.QualifiedName = value;
        return result;
    }

    internal SourceRange? CopyXmlEmissionRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXmlEmissionLocation(source.Start);
        result.End = CopyXmlEmissionLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXmlEmissionLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }

    internal bool IsXmlEmissionNamespaceAttribute(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null)
        {
            return false;
        }

        return string.Equals(name.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal) || string.Equals(name.QualifiedName, "xmlns", StringComparison.Ordinal) || string.Equals(name.Prefix, "xmlns", StringComparison.Ordinal);
    }

    internal bool IsXmlEmissionKnownEntity(XmlNode node)
    {
        XmlDocument? document = node.OwnerDocument;
        XmlDocumentType? documentType = document == null ? null : document.DocumentType;
        if (documentType == null || documentType.EntityDeclarations == null || node.Name == null)
        {
            return false;
        }

        string? name = node.Name.QualifiedName == null ? node.Name.LocalName : node.Name.QualifiedName;
        int index = 0;
        while (index < documentType.EntityDeclarations.Length)
        {
            if (string.Equals(documentType.EntityDeclarations[index].Name, name, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXmlEmissionCoveredSelectionNode(XmlNode[] nodes, int index)
    {
        int preceding = 0;
        while (preceding < index)
        {
            if (nodes[preceding] == nodes[index])
            {
                return true;
            }

            preceding = preceding + 1;
        }

        int candidate = 0;
        while (candidate < nodes.Length)
        {
            if (candidate != index && Navigation.IsXmlNavigationAncestor(nodes[candidate], nodes[index]))
            {
                return true;
            }

            candidate = candidate + 1;
        }

        return false;
    }
}
