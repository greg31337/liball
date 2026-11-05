using System;
using System.Collections.Generic;

namespace xml;

internal class TreeAttributeRepair
{
    internal XmlName Name;
    internal XmlNamespaceBinding[] Bindings;
    internal string? Value;
    internal bool Changed;

    internal TreeAttributeRepair(XmlName name, XmlNamespaceBinding[] bindings, string? value, bool changed)
    {
        Name = name;
        Bindings = bindings;
        Value = value;
        Changed = changed;
    }
}

internal class TreeContentMutation
{
    internal TreeMutation Core;
    internal TreeInvariants Invariants;

    internal TreeContentMutation(TreeMutation core)
    {
        Core = core;
        Invariants = core.Invariants;
    }

    internal void NormalizeXmlTreeNodeOperation(XmlNode node)
    {
        Guard.NotNull(node, "node");
        Core.ValidateXmlMutationSource(node);
        EnsureXmlContentSubtreeWritable(node);
        XmlDocument document = RequireXmlContentDocument(node, "node");
        if (NormalizeXmlContentSubtree(node))
        {
            long version = Core.AdvanceXmlMutationVersion(document);
            Core.SetXmlMutationSubtreeVersion(node, version);
        }
    }

    internal void SetXmlTreeTextOperation(XmlNode node, string value)
    {
        Guard.NotNull(node, "node");
        Guard.NotNull(value, "value");
        Invariants.EnsureXmlTreeMutationAllowed(node);
        XmlDocument document = RequireXmlContentDocument(node, "node");
        if (node.Kind == XmlNodeKind.Element || node.Kind == XmlNodeKind.DocumentFragment || node.Kind == XmlNodeKind.EntityReference)
        {
            SetXmlContainerText(node, value, document);
            return;
        }

        if (node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.Namespace || node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData || node.Kind == XmlNodeKind.ProcessingInstruction || node.Kind == XmlNodeKind.Comment)
        {
            string selectedValue = node.Kind == XmlNodeKind.Attribute && Core.IdentifierIndex.IsXmlIdName(node.Name) ? Core.IdentifierIndex.CollapseXmlIdentifierWhitespace(value) : value;
            if (string.Equals(node.Value, selectedValue, StringComparison.Ordinal))
            {
                return;
            }

            node.Value = selectedValue;
            node.Range = null;
            CompleteXmlContentChange(document, node, node.Parent, null);
            return;
        }

        throw new ArgumentException("Text content cannot be assigned to this node kind.", "node");
    }

    internal void SetXmlTreeAttributeOperation(XmlNode element, XmlNode attribute)
    {
        Guard.NotNull(element, "element");
        Guard.NotNull(attribute, "attribute");
        XmlDocument document = RequireXmlContentDocument(element, "element");
        XmlNode[] targetSource = element.Attributes == null ? [] : element.Attributes;
        Invariants.ValidateXmlAttributeSequence(element, targetSource);
        XmlNode? replaced = FindXmlContentAttribute(targetSource, attribute.Name);
        Invariants.ValidateXmlAttributePlacement(element, attribute, replaced);
        Core.ValidateXmlMutationSource(attribute);
        XmlNode? sourceParent = attribute.Parent;
        XmlNode[]? sourceCandidate = null;
        if (sourceParent != null && sourceParent != element)
        {
            Invariants.EnsureXmlTreeMutationAllowed(sourceParent);
            if (sourceParent.Kind != XmlNodeKind.Element)
            {
                throw new ArgumentException("An attached attribute must belong to an element.", "attribute");
            }

            sourceCandidate = Core.RemoveXmlMutationItem(sourceParent.Attributes, attribute, "attribute");
            Invariants.ValidateXmlAttributeSequence(sourceParent, sourceCandidate);
        }

        XmlNode[] targetCandidate;
        if (sourceParent == element)
        {
            if (replaced != attribute)
            {
                throw new ArgumentException("The target element contains inconsistent attribute names.", "element");
            }

            targetCandidate = targetSource;
        }
        else if (replaced == null)
        {
            targetCandidate = Core.InsertXmlMutationItem(targetSource, attribute, targetSource.Length);
        }
        else
        {
            Invariants.EnsureXmlTreeMutationAllowed(replaced);
            int replacementIndex = Core.IndexXmlMutationItem(targetSource, replaced);
            targetCandidate = Core.ReplaceXmlMutationItem(targetSource, attribute, replacementIndex);
        }

        Invariants.ValidateXmlAttributePlacement(element, attribute, replaced);
        Invariants.ValidateXmlAttributeSequence(element, targetCandidate);
        TreeAttributeRepair repair = PlanXmlAttributeNamespaceRepair(element, attribute);
        bool structuralChange = sourceParent != element || replaced != attribute;
        if (!structuralChange && !repair.Changed)
        {
            return;
        }

        if (sourceParent != null && sourceParent != element)
        {
            sourceParent.Attributes = sourceCandidate;
        }

        element.Attributes = targetCandidate;
        element.NamespaceBindings = repair.Bindings;
        attribute.Name = repair.Name;
        attribute.Value = repair.Value;
        attribute.Parent = element;
        if (replaced != null && replaced != attribute)
        {
            replaced.Parent = null;
        }

        CompleteXmlContentChange(document, element, attribute, replaced);
        if (sourceParent != null && sourceParent != element)
        {
            sourceParent.MutationVersion = document.MutationVersion;
        }
    }

    internal XmlNode? RemoveXmlTreeAttributeOperation(XmlNode element, XmlName name)
    {
        Guard.NotNull(element, "element");
        Guard.NotNull(name, "name");
        Invariants.EnsureXmlTreeMutationAllowed(element);
        if (element.Kind != XmlNodeKind.Element)
        {
            throw new ArgumentException("Attributes can only be removed from an element.", "element");
        }

        XmlDocument document = RequireXmlContentDocument(element, "element");
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        Invariants.ValidateXmlAttributeSequence(element, attributes);
        XmlNode? removed = FindXmlContentAttribute(attributes, name);
        if (removed == null)
        {
            return null;
        }

        Invariants.EnsureXmlTreeMutationAllowed(removed);
        XmlNode[] candidate = Core.RemoveXmlMutationItem(attributes, removed, "name");
        Invariants.ValidateXmlAttributeSequence(element, candidate);
        XmlNamespaceBinding[]? bindingCandidate = RemoveXmlContentNamespaceDeclaration(element.NamespaceBindings, removed.Name);
        element.Attributes = candidate;
        element.NamespaceBindings = bindingCandidate;
        removed.Parent = null;
        CompleteXmlContentChange(document, element, removed, null);
        return removed;
    }

    internal XmlNamespaceBinding[]? RemoveXmlContentNamespaceDeclaration(XmlNamespaceBinding[]? bindings, XmlName? name)
    {
        if (name == null || !string.Equals(name.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            return bindings;
        }

        XmlNamespaceBinding[] source = bindings == null ? [] : bindings;
        string local = XmlContentLocalName(name);
        string prefix = string.Equals(local, "xmlns", StringComparison.Ordinal) ? "" : local;
        int index = 0;
        while (index < source.Length)
        {
            if (string.Equals(XmlContentBindingPrefix(source[index]), prefix, StringComparison.Ordinal))
            {
                XmlNamespaceBinding[] result = new XmlNamespaceBinding[source.Length - 1];
                if (index > 0)
                {
                    Array.Copy(source, 0, result, 0, index);
                }

                if (index + 1 < source.Length)
                {
                    Array.Copy(source, index + 1, result, index, source.Length - index - 1);
                }

                return result;
            }

            index = index + 1;
        }

        return source;
    }

    internal void FreezeXmlTreeOperation(XmlNode node, bool deep)
    {
        Guard.NotNull(node, "node");
        Core.ValidateXmlMutationSource(node);
        XmlDocument document = RequireXmlContentDocument(node, "node");
        if (document.IsReadOnly)
        {
            return;
        }

        List<XmlNode> selected = new List<XmlNode>();
        AddXmlFreezeSelection(selected, node, deep);
        bool changed = false;
        int index = 0;
        while (index < selected.Count)
        {
            if (!selected[index].IsReadOnly)
            {
                changed = true;
            }

            index = index + 1;
        }

        if (!changed)
        {
            return;
        }

        long version = Core.AdvanceXmlMutationVersion(document);
        index = 0;
        while (index < selected.Count)
        {
            selected[index].IsReadOnly = true;
            selected[index].MutationVersion = version;
            index = index + 1;
        }
    }

    internal bool NormalizeXmlContentSubtree(XmlNode node)
    {
        bool changed = false;
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            if (NormalizeXmlContentSubtree(children[childIndex]))
            {
                changed = true;
            }

            childIndex = childIndex + 1;
        }

        if (children.Length == 0)
        {
            return changed;
        }

        List<XmlNode> normalized = new List<XmlNode>();
        XmlNode? precedingText = null;
        childIndex = 0;
        while (childIndex < children.Length)
        {
            XmlNode child = children[childIndex];
            if (child.Kind == XmlNodeKind.Text)
            {
                string text = child.Value == null ? "" : child.Value;
                if (text.Length == 0)
                {
                    child.Parent = null;
                    changed = true;
                }
                else if (precedingText != null)
                {
                    string preceding = precedingText.Value == null ? "" : precedingText.Value;
                    precedingText.Value = preceding + text;
                    precedingText.Range = null;
                    child.Parent = null;
                    changed = true;
                }
                else
                {
                    normalized.Add(child);
                    precedingText = child;
                }
            }
            else
            {
                normalized.Add(child);
                precedingText = null;
            }

            childIndex = childIndex + 1;
        }

        if (normalized.Count != children.Length)
        {
            node.Children = normalized.ToArray();
        }

        return changed;
    }

    internal void SetXmlContainerText(XmlNode node, string value, XmlDocument document)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        if (children.Length == 0 && value.Length == 0)
        {
            return;
        }

        if (children.Length == 1 && children[0].Kind == XmlNodeKind.Text && string.Equals(children[0].Value, value, StringComparison.Ordinal))
        {
            return;
        }

        int index = 0;
        while (index < children.Length)
        {
            Invariants.EnsureXmlTreeMutationAllowed(children[index]);
            index = index + 1;
        }

        XmlNode[] candidate = [];
        if (value.Length != 0)
        {
            XmlNode text = Core.CreateXmlTreeNodeOperation(document, XmlNodeKind.Text, null, value);
            text.Parent = node;
            candidate = [text];
        }

        Invariants.ValidateXmlChildSequence(node, candidate);
        index = 0;
        while (index < children.Length)
        {
            children[index].Parent = null;
            index = index + 1;
        }

        node.Children = candidate;
        node.Range = null;
        long version = Core.AdvanceXmlMutationVersion(document);
        node.MutationVersion = version;
        index = 0;
        while (index < children.Length)
        {
            children[index].MutationVersion = version;
            index = index + 1;
        }

        if (candidate.Length != 0)
        {
            candidate[0].MutationVersion = version;
        }
    }

    internal TreeAttributeRepair PlanXmlAttributeNamespaceRepair(XmlNode element, XmlNode attribute)
    {
        XmlName? optionalName = attribute.Name;
        if (optionalName == null)
        {
            throw new ArgumentException("An attribute requires a name.", "attribute");
        }

        XmlName source = optionalName;
        string local = XmlContentLocalName(source);
        string uri = source.NamespaceUri == null ? "" : source.NamespaceUri;
        string? prefix = source.Prefix;
        XmlNamespaceBinding[] bindings = element.NamespaceBindings == null ? [] : element.NamespaceBindings;
        bool addBinding = false;
        if (uri.Length == 0)
        {
            prefix = null;
        }
        else if (string.Equals(uri, XmlNamespaces.Xml, StringComparison.Ordinal))
        {
            prefix = "xml";
        }
        else if (string.Equals(uri, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            prefix = string.Equals(local, "xmlns", StringComparison.Ordinal) ? null : "xmlns";
            bindings = PlanXmlNamespaceDeclaration(bindings, local, attribute.Value);
            addBinding = true;
        }
        else
        {
            if (prefix == null || prefix.Length == 0 || string.Equals(prefix, "xml", StringComparison.Ordinal) || string.Equals(prefix, "xmlns", StringComparison.Ordinal))
            {
                prefix = FindXmlContentPrefix(element, uri);
                if (prefix == null)
                {
                    prefix = GenerateXmlContentPrefix(element);
                    bindings = AppendXmlContentBinding(bindings, prefix, uri);
                    addBinding = true;
                }
            }
            else if (!XmlContentPrefixMatches(element, prefix, uri))
            {
                if (!XmlContentPrefixExists(element, prefix))
                {
                    bindings = AppendXmlContentBinding(bindings, prefix, uri);
                    addBinding = true;
                }
                else
                {
                    prefix = FindXmlContentPrefix(element, uri);
                    if (prefix == null)
                    {
                        prefix = GenerateXmlContentPrefix(element);
                        bindings = AppendXmlContentBinding(bindings, prefix, uri);
                        addBinding = true;
                    }
                }
            }
        }

        string qualified = prefix == null || prefix.Length == 0 ? local : prefix + ":" + local;
        XmlName repaired = new XmlName();
        repaired.Prefix = prefix;
        repaired.LocalName = local;
        repaired.NamespaceUri = source.NamespaceUri;
        repaired.QualifiedName = qualified;
        string? repairedValue = Core.IdentifierIndex.IsXmlIdName(repaired) ? Core.IdentifierIndex.CollapseXmlIdentifierWhitespace(attribute.Value) : attribute.Value;
        bool changed = addBinding || !string.Equals(source.Prefix, repaired.Prefix, StringComparison.Ordinal) || !string.Equals(source.LocalName, repaired.LocalName, StringComparison.Ordinal) || !string.Equals(source.QualifiedName, repaired.QualifiedName, StringComparison.Ordinal) || !string.Equals(attribute.Value, repairedValue, StringComparison.Ordinal);
        return new TreeAttributeRepair(repaired, bindings, repairedValue, changed);
    }

    internal XmlNamespaceBinding[] PlanXmlNamespaceDeclaration(XmlNamespaceBinding[] bindings, string local, string? value)
    {
        string declaredPrefix = string.Equals(local, "xmlns", StringComparison.Ordinal) ? "" : local;
        string uri = value == null ? "" : value;
        if (string.Equals(declaredPrefix, "xmlns", StringComparison.Ordinal) || string.Equals(uri, XmlNamespaces.Xmlns, StringComparison.Ordinal) || string.Equals(declaredPrefix, "xml", StringComparison.Ordinal) && !string.Equals(uri, XmlNamespaces.Xml, StringComparison.Ordinal))
        {
            throw new ArgumentException("The namespace declaration uses a reserved prefix or URI.", "attribute");
        }

        XmlNamespaceBinding replacement = new XmlNamespaceBinding();
        replacement.Prefix = declaredPrefix;
        replacement.NamespaceUri = uri;
        replacement.IsUndeclaration = uri.Length == 0;
        replacement.Range = null;
        int index = 0;
        while (index < bindings.Length)
        {
            string existingPrefix = XmlContentBindingPrefix(bindings[index]);
            if (string.Equals(existingPrefix, declaredPrefix, StringComparison.Ordinal))
            {
                XmlNamespaceBinding[] copies = new XmlNamespaceBinding[bindings.Length];
                Array.Copy(bindings, copies, bindings.Length);
                copies[index] = replacement;
                return copies;
            }

            index = index + 1;
        }

        XmlNamespaceBinding[] result = new XmlNamespaceBinding[bindings.Length + 1];
        Array.Copy(bindings, result, bindings.Length);
        result[bindings.Length] = replacement;
        return result;
    }

    internal XmlNamespaceBinding[] AppendXmlContentBinding(XmlNamespaceBinding[] bindings, string prefix, string uri)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = uri;
        binding.IsUndeclaration = false;
        binding.Range = null;
        XmlNamespaceBinding[] result = new XmlNamespaceBinding[bindings.Length + 1];
        Array.Copy(bindings, result, bindings.Length);
        result[bindings.Length] = binding;
        return result;
    }

    internal string? FindXmlContentPrefix(XmlNode element, string uri)
    {
        HashSet<string> hidden = new HashSet<string>(StringComparer.Ordinal);
        XmlNode? current = element;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = 0;
            while (index < bindings.Length)
            {
                string prefix = XmlContentBindingPrefix(bindings[index]);
                if (prefix.Length != 0 && hidden.Add(prefix) && string.Equals(bindings[index].NamespaceUri, uri, StringComparison.Ordinal))
                {
                    return prefix;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal bool XmlContentPrefixMatches(XmlNode element, string prefix, string uri)
    {
        if (string.Equals(prefix, "xml", StringComparison.Ordinal))
        {
            return string.Equals(uri, XmlNamespaces.Xml, StringComparison.Ordinal);
        }

        XmlNode? current = element;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = 0;
            while (index < bindings.Length)
            {
                string candidate = XmlContentBindingPrefix(bindings[index]);
                if (string.Equals(candidate, prefix, StringComparison.Ordinal))
                {
                    return string.Equals(bindings[index].NamespaceUri, uri, StringComparison.Ordinal);
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return false;
    }

    internal string GenerateXmlContentPrefix(XmlNode element)
    {
        int suffix = 1;
        while (true)
        {
            string prefix = "ns" + suffix;
            if (!XmlContentPrefixExists(element, prefix))
            {
                return prefix;
            }

            suffix = suffix + 1;
        }
    }

    internal bool XmlContentPrefixExists(XmlNode element, string prefix)
    {
        XmlNode? current = element;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = 0;
            while (index < bindings.Length)
            {
                string candidate = XmlContentBindingPrefix(bindings[index]);
                if (string.Equals(candidate, prefix, StringComparison.Ordinal))
                {
                    return true;
                }

                index = index + 1;
            }

            current = current.Parent;
        }

        return false;
    }

    internal string XmlContentLocalName(XmlName name)
    {
        string? local = name.LocalName;
        if (local != null && local.Length != 0)
        {
            return local;
        }

        string? qualified = name.QualifiedName;
        if (qualified == null || qualified.Length == 0)
        {
            throw new ArgumentException("An attribute requires a local name.", "attribute");
        }

        int colon = qualified.IndexOf(':');
        return colon < 0 ? qualified : qualified.Substring(colon + 1);
    }

    internal string XmlContentBindingPrefix(XmlNamespaceBinding binding)
    {
        string? prefix = binding.Prefix;
        return prefix == null ? "" : prefix;
    }

    internal XmlNode? FindXmlContentAttribute(XmlNode[] attributes, XmlName? name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            if (Invariants.SameXmlExpandedName(attributes[index].Name, name))
            {
                return attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlDocument RequireXmlContentDocument(XmlNode node, string parameterName)
    {
        XmlDocument? optionalDocument = node.OwnerDocument;
        if (optionalDocument == null)
        {
            throw new ArgumentException("The node must have an owner document.", parameterName);
        }

        return optionalDocument;
    }

    internal void EnsureXmlContentSubtreeWritable(XmlNode node)
    {
        Invariants.EnsureXmlTreeMutationAllowed(node);
        List<XmlNode> pending = new List<XmlNode>();
        HashSet<XmlNode> visited = new HashSet<XmlNode>();
        pending.Add(node);
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            XmlNode item = pending[last];
            pending.RemoveAt(last);
            if (visited.Add(item))
            {
                if (item.IsReadOnly)
                {
                    throw new InvalidOperationException("A normalized subtree cannot contain a read-only node.");
                }

                Core.AddXmlMutationNodes(pending, item.Attributes);
                Core.AddXmlMutationNodes(pending, item.Children);
            }
        }
    }

    internal void AddXmlFreezeSelection(List<XmlNode> selected, XmlNode node, bool deep)
    {
        selected.Add(node);
        if (!deep)
        {
            return;
        }

        int index = 0;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        while (index < attributes.Length)
        {
            AddXmlFreezeSelection(selected, attributes[index], true);
            index = index + 1;
        }

        index = 0;
        XmlNode[] children = node.Children == null ? [] : node.Children;
        while (index < children.Length)
        {
            AddXmlFreezeSelection(selected, children[index], true);
            index = index + 1;
        }
    }

    internal void CompleteXmlContentChange(XmlDocument document, XmlNode? first, XmlNode? second, XmlNode? third)
    {
        Core.CompleteXmlMutationChange(document, first, second, third);
    }
}
