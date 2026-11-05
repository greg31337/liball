using System;
using System.Collections.Generic;

namespace xml;

internal class NamespaceFixup
{
    internal XmlNamespaceHandling Handling;
    internal QualifiedNameParser NameParser;
    internal NamespaceDeclarationProcessor DeclarationValidator;
    internal long NextPrefixNumber;

    internal NamespaceFixup(XmlNamespaceHandling handling, IXmlErrorReporter? errorReporter)
    {
        if (handling != XmlNamespaceHandling.Namespaces10 && handling != XmlNamespaceHandling.Namespaces11)
        {
            throw new ArgumentOutOfRangeException("handling");
        }

        Handling = handling;
        NameParser = new QualifiedNameParser(handling, errorReporter);
        DeclarationValidator = new NamespaceDeclarationProcessor(handling, errorReporter);
        NextPrefixNumber = 1;
    }

    internal void FixupXmlNamespaceSubtree(XmlNode root, NamespaceContext inheritedContext)
    {
        Guard.NotNull(root, "root");
        Guard.NotNull(inheritedContext, "inheritedContext");
        NextPrefixNumber = 1;
        FixupXmlNamespaceNode(root, inheritedContext);
    }

    internal void FixupXmlNamespaceNode(XmlNode node, NamespaceContext inheritedContext)
    {
        Guard.NotNull(node, "node");
        Guard.NotNull(inheritedContext, "inheritedContext");
        NamespaceContext childContext = inheritedContext;
        if (node.Kind == XmlNodeKind.Element)
        {
            childContext = FixupXmlElementNamespaces(node, inheritedContext);
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            FixupXmlNamespaceNode(children[index], childContext);
            index = index + 1;
        }
    }

    internal NamespaceContext FixupXmlElementNamespaces(XmlNode element, NamespaceContext inheritedContext)
    {
        Guard.NotNull(element, "element");
        Guard.NotNull(inheritedContext, "inheritedContext");
        if (element.Kind != XmlNodeKind.Element)
        {
            throw new ArgumentException("Namespace fixup requires an element node.", "element");
        }

        XmlName? optionalElementName = element.Name;
        if (optionalElementName == null)
        {
            throw new ArgumentException("An element requires a name.", "element");
        }

        List<XmlNamespaceBinding> declarations = CopyAndValidateXmlNamespaceBindings(element.NamespaceBindings);
        Dictionary<string, string> requiredPrefixes = new Dictionary<string, string>(StringComparer.Ordinal);
        XmlName fixedElementName = FixupXmlElementName(optionalElementName, element.Range, declarations, inheritedContext, requiredPrefixes);
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        XmlName[] fixedAttributeNames = new XmlName[attributes.Length];
        bool[] namespaceAttributes = new bool[attributes.Length];
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode? optionalAttribute = attributes[index];
            if (optionalAttribute == null || optionalAttribute.Kind != XmlNodeKind.Attribute || optionalAttribute.Name == null)
            {
                throw new ArgumentException("Every element attribute must be a named attribute node.", "element");
            }

            bool namespaceAttribute = IsXmlNamespaceDeclarationName(optionalAttribute.Name);
            namespaceAttributes[index] = namespaceAttribute;
            fixedAttributeNames[index] = namespaceAttribute ? FixupXmlNamespaceDeclarationName(optionalAttribute.Name) : FixupXmlOrdinaryAttributeName(optionalAttribute.Name, optionalAttribute.Range, declarations, inheritedContext, requiredPrefixes);
            index = index + 1;
        }

        index = 0;
        while (index < fixedAttributeNames.Length)
        {
            if (!namespaceAttributes[index])
            {
                int preceding = 0;
                while (preceding < index)
                {
                    if (!namespaceAttributes[preceding] && NameParser.SameExpandedXmlName(fixedAttributeNames[preceding], fixedAttributeNames[index]))
                    {
                        throw NameParser.CreateXmlQualifiedNameError("Two attributes on the same element have the expanded name '" + NameParser.ExpandedXmlNameText(fixedAttributeNames[index]) + "'", XmlErrorCodes.NamespaceDuplicateAttribute, attributes[index].Range);
                    }

                    preceding = preceding + 1;
                }
            }

            index = index + 1;
        }

        element.Name = fixedElementName;
        index = 0;
        while (index < attributes.Length)
        {
            attributes[index].Name = fixedAttributeNames[index];
            index = index + 1;
        }

        XmlNamespaceBinding[] finalDeclarations = declarations.ToArray();
        element.NamespaceBindings = finalDeclarations;
        return new NamespaceContext(inheritedContext, finalDeclarations);
    }

    internal XmlName FixupXmlElementName(XmlName source, SourceRange? range, List<XmlNamespaceBinding> declarations, NamespaceContext inheritedContext, Dictionary<string, string> requiredPrefixes)
    {
        QualifiedNameParts parts = PrepareXmlFixupName(source, range);
        string localName = parts.LocalName;
        string? namespaceUri = source.NamespaceUri;
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            NamespaceContext context = CurrentXmlFixupContext(inheritedContext, declarations);
            if (context.DefaultXmlNamespaceUri() != null)
            {
                SetLocalXmlNamespaceBinding(declarations, "", "", true);
            }

            return CreateXmlFixupName(null, localName, null);
        }

        if (string.Equals(namespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal))
        {
            requiredPrefixes["xml"] = namespaceUri;
            return CreateXmlFixupName("xml", localName, namespaceUri);
        }

        if (string.Equals(namespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            throw NameParser.CreateXmlQualifiedNameError("The namespace-declaration namespace cannot be used for an element name", XmlErrorCodes.NamespaceReservedBinding, range);
        }

        string? requestedPrefix = parts.Prefix;
        string? selectedPrefix = null;
        if (requestedPrefix == null)
        {
            int localDefault = FindLocalXmlNamespaceBinding(declarations, "");
            if (localDefault < 0 || SameLocalXmlNamespaceBinding(declarations, localDefault, namespaceUri))
            {
                NamespaceContext context = CurrentXmlFixupContext(inheritedContext, declarations);
                if (!string.Equals(context.DefaultXmlNamespaceUri(), namespaceUri, StringComparison.Ordinal))
                {
                    SetLocalXmlNamespaceBinding(declarations, "", namespaceUri, false);
                }

                return CreateXmlFixupName(null, localName, namespaceUri);
            }
        }
        else if (CanUseXmlFixupPrefix(requestedPrefix))
        {
            int localIndex = FindLocalXmlNamespaceBinding(declarations, requestedPrefix);
            if (localIndex < 0 || SameLocalXmlNamespaceBinding(declarations, localIndex, namespaceUri))
            {
                selectedPrefix = requestedPrefix;
                NamespaceContext context = CurrentXmlFixupContext(inheritedContext, declarations);
                if (!string.Equals(context.LookupXmlNamespaceUri(selectedPrefix), namespaceUri, StringComparison.Ordinal))
                {
                    SetLocalXmlNamespaceBinding(declarations, selectedPrefix, namespaceUri, false);
                }
            }
        }

        if (selectedPrefix == null)
        {
            selectedPrefix = ReuseOrGenerateXmlPrefix(namespaceUri, declarations, inheritedContext, requiredPrefixes);
        }

        requiredPrefixes[selectedPrefix] = namespaceUri;
        return CreateXmlFixupName(selectedPrefix, localName, namespaceUri);
    }

    internal XmlName FixupXmlOrdinaryAttributeName(XmlName source, SourceRange? range, List<XmlNamespaceBinding> declarations, NamespaceContext inheritedContext, Dictionary<string, string> requiredPrefixes)
    {
        QualifiedNameParts parts = PrepareXmlFixupName(source, range);
        string localName = parts.LocalName;
        string? namespaceUri = source.NamespaceUri;
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            return CreateXmlFixupName(null, localName, null);
        }

        if (string.Equals(namespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal))
        {
            requiredPrefixes["xml"] = namespaceUri;
            return CreateXmlFixupName("xml", localName, namespaceUri);
        }

        if (string.Equals(namespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            throw NameParser.CreateXmlQualifiedNameError("Only namespace declaration attributes can use the namespace-declaration namespace", XmlErrorCodes.NamespaceReservedBinding, range);
        }

        string? requestedPrefix = parts.Prefix;
        string? selectedPrefix = null;
        if (requestedPrefix != null && CanUseXmlFixupPrefix(requestedPrefix))
        {
            bool requiredConflict = requiredPrefixes.ContainsKey(requestedPrefix) && !string.Equals(requiredPrefixes[requestedPrefix], namespaceUri, StringComparison.Ordinal);
            int localIndex = FindLocalXmlNamespaceBinding(declarations, requestedPrefix);
            bool localConflict = localIndex >= 0 && !SameLocalXmlNamespaceBinding(declarations, localIndex, namespaceUri);
            if (!requiredConflict && !localConflict)
            {
                selectedPrefix = requestedPrefix;
                NamespaceContext context = CurrentXmlFixupContext(inheritedContext, declarations);
                if (!string.Equals(context.LookupXmlNamespaceUri(selectedPrefix), namespaceUri, StringComparison.Ordinal))
                {
                    SetLocalXmlNamespaceBinding(declarations, selectedPrefix, namespaceUri, false);
                }
            }
        }

        if (selectedPrefix == null)
        {
            selectedPrefix = ReuseOrGenerateXmlPrefix(namespaceUri, declarations, inheritedContext, requiredPrefixes);
        }

        requiredPrefixes[selectedPrefix] = namespaceUri;
        return CreateXmlFixupName(selectedPrefix, localName, namespaceUri);
    }

    internal QualifiedNameParts PrepareXmlFixupName(XmlName source, SourceRange? range)
    {
        Guard.NotNull(source, "source");
        string? localName = source.LocalName;
        string? prefix = source.Prefix;
        if (localName == null)
        {
            string? qualifiedName = source.QualifiedName;
            if (qualifiedName == null)
            {
                throw new ArgumentException("Namespace fixup requires a local or qualified name.", "source");
            }

            QualifiedNameParts parsed = NameParser.ParseXmlQualifiedName(qualifiedName, range);
            localName = parsed.LocalName;
            if (prefix == null)
            {
                prefix = parsed.Prefix;
            }
        }

        if (!NameParser.IsValidXmlNcName(localName))
        {
            throw NameParser.CreateXmlQualifiedNameError("Namespace fixup requires a valid NCName local part", XmlErrorCodes.MalformedMarkup, range);
        }

        if (prefix != null && prefix.Length == 0)
        {
            prefix = null;
        }

        string qualified = prefix == null ? localName : prefix + ":" + localName;
        return new QualifiedNameParts(prefix, localName, qualified);
    }

    internal bool IsXmlNamespaceDeclarationName(XmlName name)
    {
        Guard.NotNull(name, "name");
        if (string.Equals(name.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(name.Prefix, "xmlns", StringComparison.Ordinal))
        {
            return true;
        }

        return name.Prefix == null && string.Equals(name.LocalName, "xmlns", StringComparison.Ordinal);
    }

    internal XmlName FixupXmlNamespaceDeclarationName(XmlName source)
    {
        string? localName = source.LocalName;
        if (localName == null)
        {
            string qualifiedName = source.QualifiedName == null ? "xmlns" : source.QualifiedName;
            localName = string.Equals(qualifiedName, "xmlns", StringComparison.Ordinal) ? "xmlns" : qualifiedName.Substring(qualifiedName.IndexOf(':') + 1);
        }

        string? prefix = string.Equals(localName, "xmlns", StringComparison.Ordinal) ? null : "xmlns";
        return CreateXmlFixupName(prefix, localName, XmlNamespaces.Xmlns);
    }

    internal List<XmlNamespaceBinding> CopyAndValidateXmlNamespaceBindings(XmlNamespaceBinding[]? bindings)
    {
        List<XmlNamespaceBinding> declarations = new List<XmlNamespaceBinding>();
        HashSet<string> prefixes = new HashSet<string>(StringComparer.Ordinal);
        XmlNamespaceBinding[] sourceBindings = bindings == null ? [] : bindings;
        int index = 0;
        while (index < sourceBindings.Length)
        {
            XmlNamespaceBinding? optionalBinding = sourceBindings[index];
            if (optionalBinding == null || optionalBinding.Prefix == null || optionalBinding.NamespaceUri == null)
            {
                throw new ArgumentException("Namespace fixup requires complete namespace bindings.", "bindings");
            }

            string prefix = optionalBinding.Prefix;
            string namespaceUri = optionalBinding.IsUndeclaration ? "" : optionalBinding.NamespaceUri;
            if (!prefixes.Add(prefix))
            {
                throw DeclarationValidator.CreateXmlNamespaceDeclarationError("The namespace prefix '" + prefix + "' is declared more than once on the same element", XmlErrorCodes.NamespaceDuplicateAttribute, optionalBinding.Range);
            }

            DeclarationValidator.ValidateReservedXmlNamespaceBinding(prefix, namespaceUri, optionalBinding.Range);
            DeclarationValidator.ValidateXmlNamespaceUndeclaration(prefix, namespaceUri, optionalBinding.Range);
            XmlNamespaceBinding copy = new XmlNamespaceBinding();
            copy.Prefix = prefix;
            copy.NamespaceUri = namespaceUri;
            copy.IsUndeclaration = namespaceUri.Length == 0;
            copy.Range = optionalBinding.Range;
            declarations.Add(copy);
            index = index + 1;
        }

        return declarations;
    }

    internal NamespaceContext CurrentXmlFixupContext(NamespaceContext inheritedContext, List<XmlNamespaceBinding> declarations)
    {
        return new NamespaceContext(inheritedContext, declarations.ToArray());
    }

    internal int FindLocalXmlNamespaceBinding(List<XmlNamespaceBinding> declarations, string prefix)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (string.Equals(declarations[index].Prefix, prefix, StringComparison.Ordinal))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool SameLocalXmlNamespaceBinding(List<XmlNamespaceBinding> declarations, int index, string namespaceUri)
    {
        XmlNamespaceBinding binding = declarations[index];
        return !binding.IsUndeclaration && string.Equals(binding.NamespaceUri, namespaceUri, StringComparison.Ordinal);
    }

    internal void SetLocalXmlNamespaceBinding(List<XmlNamespaceBinding> declarations, string prefix, string namespaceUri, bool undeclaration)
    {
        int index = FindLocalXmlNamespaceBinding(declarations, prefix);
        XmlNamespaceBinding binding;
        if (index < 0)
        {
            binding = new XmlNamespaceBinding();
            binding.Prefix = prefix;
            declarations.Add(binding);
        }
        else
        {
            binding = declarations[index];
        }

        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = undeclaration;
    }

    internal string ReuseOrGenerateXmlPrefix(string namespaceUri, List<XmlNamespaceBinding> declarations, NamespaceContext inheritedContext, Dictionary<string, string> requiredPrefixes)
    {
        NamespaceContext context = CurrentXmlFixupContext(inheritedContext, declarations);
        XmlNamespaceBinding[] inScope = context.EnumerateInScopeXmlNamespaces();
        int index = 0;
        while (index < inScope.Length)
        {
            string? optionalPrefix = inScope[index].Prefix;
            string prefix = optionalPrefix == null ? "" : optionalPrefix;
            bool requiredConflict = requiredPrefixes.ContainsKey(prefix) && !string.Equals(requiredPrefixes[prefix], namespaceUri, StringComparison.Ordinal);
            if (prefix.Length > 0 && CanUseXmlFixupPrefix(prefix) && !requiredConflict && string.Equals(inScope[index].NamespaceUri, namespaceUri, StringComparison.Ordinal))
            {
                return prefix;
            }

            index = index + 1;
        }

        string generated = GenerateXmlNamespacePrefix(declarations, inheritedContext, requiredPrefixes);
        SetLocalXmlNamespaceBinding(declarations, generated, namespaceUri, false);
        return generated;
    }

    internal string GenerateXmlNamespacePrefix(List<XmlNamespaceBinding> declarations, NamespaceContext inheritedContext, Dictionary<string, string> requiredPrefixes)
    {
        while (true)
        {
            string candidate = "ns" + NextPrefixNumber;
            NextPrefixNumber = NextPrefixNumber + 1;
            NamespaceContext context = CurrentXmlFixupContext(inheritedContext, declarations);
            if (context.LookupXmlNamespaceUri(candidate) == null && !requiredPrefixes.ContainsKey(candidate))
            {
                return candidate;
            }
        }
    }

    internal bool CanUseXmlFixupPrefix(string prefix)
    {
        return NameParser.IsValidXmlNcName(prefix) && !string.Equals(prefix, "xml", StringComparison.Ordinal) && !string.Equals(prefix, "xmlns", StringComparison.Ordinal);
    }

    internal XmlName CreateXmlFixupName(string? prefix, string localName, string? namespaceUri)
    {
        XmlName name = new XmlName();
        name.Prefix = prefix;
        name.LocalName = localName;
        name.NamespaceUri = namespaceUri;
        name.QualifiedName = prefix == null ? localName : prefix + ":" + localName;
        return name;
    }
}
