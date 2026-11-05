using System;
using System.Collections.Generic;

namespace xml;

internal class XPointerEvaluation
{
    internal XmlNode? Node;
    internal bool IsError;
    internal string? ErrorMessage;
    internal XmlNamespaceBinding[] NamespaceBindings;

    internal XPointerEvaluation(XmlNode? node, bool isError, string? errorMessage, XmlNamespaceBinding[] namespaceBindings)
    {
        Node = node;
        IsError = isError;
        ErrorMessage = errorMessage;
        NamespaceBindings = namespaceBindings;
    }
}

internal class XPointerEvaluator
{
    internal XPointerParser Parser;
    internal TreeNavigation Navigation;

    internal XPointerEvaluator()
    {
        Parser = new XPointerParser();
        Navigation = new TreeNavigation();
    }

    internal XPointerEvaluation EvaluateXPointer(XmlDocument document, string pointer)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(pointer, "pointer");
        List<XmlNamespaceBinding> bindings = CreateInitialXPointerBindings();
        XPointerExpression expression = Parser.ParseXPointer(pointer, document.Version);
        if (!expression.IsValid)
        {
            return CreateXPointerEvaluation(null, true, expression.ErrorMessage, bindings);
        }

        string? shorthand = expression.Shorthand;
        if (shorthand != null)
        {
            XmlNode? selected = Navigation.FindXmlNodeByIdOperation(document, shorthand);
            string? error = selected == null ? "The XPointer shorthand did not identify an element." : null;
            return CreateXPointerEvaluation(selected, selected == null, error, bindings);
        }

        int index = 0;
        while (index < expression.Parts.Length)
        {
            XPointerPart part = expression.Parts[index];
            if (part.Prefix == null && string.Equals(part.LocalName, "xmlns", StringComparison.Ordinal))
            {
                ApplyXPointerNamespaceBinding(bindings, part.SchemeData, document.Version);
            }
            else if (part.Prefix == null && string.Equals(part.LocalName, "element", StringComparison.Ordinal))
            {
                XmlNode? selected = EvaluateXPointerElementPart(document, part.SchemeData);
                if (selected != null)
                {
                    return CreateXPointerEvaluation(selected, false, null, bindings);
                }
            }
            else
            {
                ResolveXPointerSchemeNamespace(bindings, part.Prefix);
            }

            index = index + 1;
        }

        return CreateXPointerEvaluation(null, true, "No XPointer pointer part identified an element.", bindings);
    }

    internal XmlNode? EvaluateXPointerElementPart(XmlDocument document, string data)
    {
        if (data.Length == 0)
        {
            return null;
        }

        QualifiedNameParser nameParser = Parser.CreateXPointerNameParser(document.Version);
        XmlNode? current;
        int offset;
        if (data[0] == '/')
        {
            current = document.DocumentNode;
            offset = 0;
        }
        else
        {
            int slash = data.IndexOf('/');
            string identifier = slash < 0 ? data : data.Substring(0, slash);
            if (!nameParser.IsValidXmlNcName(identifier))
            {
                return null;
            }

            current = Navigation.FindXmlNodeByIdOperation(document, identifier);
            if (current == null)
            {
                return null;
            }

            offset = slash < 0 ? data.Length : slash;
        }

        while (offset < data.Length)
        {
            if (data[offset] != '/' || offset + 1 >= data.Length || data[offset + 1] < '1' || data[offset + 1] > '9')
            {
                return null;
            }

            offset = offset + 1;
            int childNumber = 0;
            bool overflow = false;
            while (offset < data.Length && data[offset] >= '0' && data[offset] <= '9')
            {
                int digit = data[offset] - '0';
                if (childNumber > (int.MaxValue - digit) / 10)
                {
                    overflow = true;
                }
                else if (!overflow)
                {
                    childNumber = childNumber * 10 + digit;
                }

                offset = offset + 1;
            }

            if (overflow || offset < data.Length && data[offset] != '/')
            {
                return null;
            }

            current = FindXPointerElementChild(current, childNumber);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    internal XmlNode? FindXPointerElementChild(XmlNode? parent, int position)
    {
        if (parent == null || position <= 0)
        {
            return null;
        }

        XmlNode[] children = parent.Children == null ? [] : parent.Children;
        int elementPosition = 0;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                elementPosition = elementPosition + 1;
                if (elementPosition == position)
                {
                    return child;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal void ApplyXPointerNamespaceBinding(List<XmlNamespaceBinding> bindings, string data, XmlVersion version)
    {
        int equals = data.IndexOf('=');
        if (equals <= 0)
        {
            return;
        }

        int prefixEnd = equals;
        while (prefixEnd > 0 && XmlLexicalCharacterRules.IsXmlSpaceCharacter(data[prefixEnd - 1]))
        {
            prefixEnd = prefixEnd - 1;
        }

        string prefix = data.Substring(0, prefixEnd);
        int between = prefixEnd;
        while (between < equals)
        {
            if (!XmlLexicalCharacterRules.IsXmlSpaceCharacter(data[between]))
            {
                return;
            }

            between = between + 1;
        }

        QualifiedNameParser nameParser = Parser.CreateXPointerNameParser(version);
        if (!nameParser.IsValidXmlNcName(prefix))
        {
            return;
        }

        int namespaceStart = equals + 1;
        while (namespaceStart < data.Length && XmlLexicalCharacterRules.IsXmlSpaceCharacter(data[namespaceStart]))
        {
            namespaceStart = namespaceStart + 1;
        }

        string namespaceName = data.Substring(namespaceStart);
        if (string.Equals(prefix, "xmlns", StringComparison.Ordinal) || string.Equals(namespaceName, XmlNamespaces.Xmlns, StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(prefix, "xml", StringComparison.Ordinal))
        {
            if (!string.Equals(namespaceName, XmlNamespaces.Xml, StringComparison.Ordinal))
            {
                return;
            }
        }
        else if (string.Equals(namespaceName, XmlNamespaces.Xml, StringComparison.Ordinal))
        {
            return;
        }

        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceName;
        binding.IsUndeclaration = namespaceName.Length == 0;
        binding.Range = null;
        int index = 0;
        while (index < bindings.Count)
        {
            if (string.Equals(bindings[index].Prefix, prefix, StringComparison.Ordinal))
            {
                bindings[index] = binding;
                return;
            }

            index = index + 1;
        }

        bindings.Add(binding);
    }

    internal List<XmlNamespaceBinding> CreateInitialXPointerBindings()
    {
        List<XmlNamespaceBinding> result = new List<XmlNamespaceBinding>();
        XmlNamespaceBinding xml = new XmlNamespaceBinding();
        xml.Prefix = "xml";
        xml.NamespaceUri = XmlNamespaces.Xml;
        xml.IsUndeclaration = false;
        xml.Range = null;
        result.Add(xml);
        return result;
    }

    internal string? ResolveXPointerSchemeNamespace(List<XmlNamespaceBinding> bindings, string? prefix)
    {
        if (prefix == null)
        {
            return null;
        }

        int index = bindings.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(bindings[index].Prefix, prefix, StringComparison.Ordinal))
            {
                return bindings[index].NamespaceUri;
            }

            index = index - 1;
        }

        return null;
    }

    internal XPointerEvaluation CreateXPointerEvaluation(XmlNode? node, bool isError, string? errorMessage, List<XmlNamespaceBinding> bindings)
    {
        XmlNamespaceBinding[] snapshot = new XmlNamespaceBinding[bindings.Count];
        int index = 0;
        while (index < bindings.Count)
        {
            XmlNamespaceBinding source = bindings[index];
            XmlNamespaceBinding copy = new XmlNamespaceBinding();
            copy.Prefix = source.Prefix;
            copy.NamespaceUri = source.NamespaceUri;
            copy.IsUndeclaration = source.IsUndeclaration;
            copy.Range = null;
            snapshot[index] = copy;
            index = index + 1;
        }

        return new XPointerEvaluation(node, isError, errorMessage, snapshot);
    }
}
