using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class HtmlSerializer
{
    internal string SerializeNode(DomNode? node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        StringBuilder result = new StringBuilder();
        AppendDomNode(result, node, null, null, new List<DomNode>());
        return result.ToString();
    }

    internal string SerializeFragment(DomNode[]? nodes)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException("nodes");
        }

        StringBuilder result = new StringBuilder();
        List<DomNode> ancestors = new List<DomNode>();
        int index = 0;
        while (index < nodes.Length)
        {
            DomNode? node = nodes[index];
            if (node == null)
            {
                throw new ArgumentException("HTML node collections cannot contain null.", "nodes");
            }

            AppendDomNode(result, node, null, null, ancestors);
            index++;
        }

        return result.ToString();
    }

    internal void AppendDomNode(StringBuilder result, DomNode node, string? parentName, string? parentNamespaceUri, List<DomNode> ancestors)
    {
        if (ContainsAncestor(ancestors, node))
        {
            throw new ArgumentException("The DOM tree contains a cycle.", "node");
        }

        ancestors.Add(node);
        switch (node.Kind)
        {
            case DomNodeKind.Element:
                AppendElementNode(result, node, parentNamespaceUri, ancestors);
                break;
            case DomNodeKind.Text:
                AppendTextNode(result, node, parentName, parentNamespaceUri);
                break;
            case DomNodeKind.Comment:
                AppendCommentNode(result, node);
                break;
            case DomNodeKind.Fragment:
                AppendFragmentNode(result, node, parentName, parentNamespaceUri, ancestors);
                break;
            case DomNodeKind.RawContent:
                AppendRawContentNode(result, node);
                break;
            default:
                throw new ArgumentException("The DOM node kind is not supported.", "node");
        }

        ancestors.RemoveAt(ancestors.Count - 1);
    }

    internal void AppendElementNode(StringBuilder result, DomNode node, string? parentNamespaceUri, List<DomNode> ancestors)
    {
        ValidateElementNode(node);
        bool isHtml = IsHtmlNamespace(node.NamespaceUri);
        string serializedName = isHtml ? HtmlSecurityPolicy.ToLowerAsciiText(node.Name) : node.Name;
        result.Append('<');
        result.Append(serializedName);
        AppendCanonicalAttributes(result, node, parentNamespaceUri, isHtml);
        result.Append('>');

        bool isVoid = isHtml && IsVoidHtmlElement(serializedName);
        DomNode[] children = node.Children ?? Array.Empty<DomNode>();
        if (isVoid)
        {
            if (children.Length > 0)
            {
                throw new ArgumentException("Void HTML elements cannot contain children.", "node");
            }

            return;
        }

        if (isHtml && IsRawTextHtmlElement(serializedName))
        {
            AppendRawTextChildren(result, node, serializedName);
        }
        else
        {
            AppendOrdinaryChildren(result, children, serializedName, node.NamespaceUri, ancestors);
        }

        result.Append("</");
        result.Append(serializedName);
        result.Append('>');
    }

    internal void AppendTextNode(StringBuilder result, DomNode node, string? parentName, string? parentNamespaceUri)
    {
        string text = InvariantFormatter.NormalizeLineEndings(node.Text ?? string.Empty);
        if (IsHtmlNamespace(parentNamespaceUri) && parentName != null && IsRawTextHtmlElement(parentName))
        {
            throw new ArgumentException("Raw-text children must be serialized with their parent element.", "node");
        }

        result.Append(HtmlEscaper.EscapeText(text, HtmlInvalidCharacterPolicy.Reject));
    }

    internal void AppendCommentNode(StringBuilder result, DomNode node)
    {
        string text = InvariantFormatter.NormalizeLineEndings(node.Text ?? string.Empty);
        result.Append("<!--");
        result.Append(HtmlEscaper.EscapeComment(text, HtmlInvalidCharacterPolicy.Reject));
        result.Append("-->");
    }

    internal void AppendFragmentNode(StringBuilder result, DomNode node, string? parentName, string? parentNamespaceUri, List<DomNode> ancestors)
    {
        DomNode[] children = node.Children ?? Array.Empty<DomNode>();
        AppendOrdinaryChildren(result, children, parentName, parentNamespaceUri, ancestors);
    }

    internal void AppendRawContentNode(StringBuilder result, DomNode node)
    {
        string text = InvariantFormatter.NormalizeLineEndings(node.Text ?? string.Empty);
        if (node.RawContentPolicyId == "$renderer:doctype")
        {
            if (text != "<!doctype html>")
            {
                throw new ArgumentException("The reserved renderer raw-content policy can emit only the canonical doctype.", "node");
            }
        }
        else if (!HtmlSecurityPolicy.IsTrustPolicyIdentifier(node.RawContentPolicyId))
        {
            throw new ArgumentException("Raw content requires valid trust-policy provenance.", "node");
        }

        result.Append(HtmlEscaper.NormalizeHtmlUnicode(text, HtmlInvalidCharacterPolicy.Reject));
    }

    internal void AppendRawTextChildren(StringBuilder result, DomNode node, string elementName)
    {
        DomNode[] children = node.Children ?? Array.Empty<DomNode>();
        StringBuilder content = new StringBuilder();
        int index = 0;
        while (index < children.Length)
        {
            DomNode? child = children[index];
            if (child == null || child.Kind != DomNodeKind.Text)
            {
                throw new ArgumentException("Raw-text HTML elements can contain only text nodes.", "node");
            }

            content.Append(InvariantFormatter.NormalizeLineEndings(child.Text ?? string.Empty));
            index++;
        }

        string combined = content.ToString();
        if (elementName == HtmlElements.Script)
        {
            if (IsJsonScriptElement(node))
            {
                result.Append(HtmlSecurityPolicy.ProtectJsonClosingTokens(combined));
            }
            else
            {
                result.Append(HtmlSecurityPolicy.ProtectScriptClosingTokens(combined));
            }
        }
        else if (elementName == HtmlElements.Style)
        {
            result.Append(HtmlEscaper.EscapeRawText(combined, HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject));
        }
        else
        {
            result.Append(HtmlEscaper.EscapeText(combined, HtmlInvalidCharacterPolicy.Reject));
        }
    }

    internal bool IsJsonScriptElement(DomNode node)
    {
        DomAttribute[] attributes = node.Attributes ?? Array.Empty<DomAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute != null && attribute.NamespaceUri == null && string.Equals(attribute.Name, HtmlAttributes.Type, StringComparison.OrdinalIgnoreCase))
            {
                string type = HtmlSecurityPolicy.ToLowerAsciiText(attribute.Value.Trim());
                return type == "application/json" || type == "application/ld+json" || type == "importmap" || type == "speculationrules" || type.EndsWith("+json", StringComparison.Ordinal);
            }

            index++;
        }

        return false;
    }

    internal void AppendOrdinaryChildren(StringBuilder result, DomNode[] children, string? parentName, string? parentNamespaceUri, List<DomNode> ancestors)
    {
        int index = 0;
        while (index < children.Length)
        {
            DomNode? child = children[index];
            if (child == null)
            {
                throw new ArgumentException("HTML node collections cannot contain null.", "node");
            }

            AppendDomNode(result, child, parentName, parentNamespaceUri, ancestors);
            index++;
        }
    }

    internal void AppendCanonicalAttributes(StringBuilder result, DomNode node, string? parentNamespaceUri, bool isHtml)
    {
        DomAttribute[] attributes = CopyAndSortAttributes(node.Attributes, isHtml);
        if (node.NamespaceUri != parentNamespaceUri)
        {
            result.Append(" xmlns=\"");
            result.Append(HtmlEscaper.EscapeAttributeValue(InvariantFormatter.NormalizeLineEndings(node.NamespaceUri ?? string.Empty), HtmlInvalidCharacterPolicy.Reject));
            result.Append('"');
        }

        AppendNamespacePrefixes(result, attributes);
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute attribute = attributes[index];
            string name = isHtml && attribute.NamespaceUri == null ? HtmlSecurityPolicy.ToLowerAsciiText(attribute.Name) : attribute.Name;
            result.Append(' ');
            if (attribute.Prefix != null)
            {
                result.Append(attribute.Prefix);
                result.Append(':');
            }

            result.Append(name);
            if (!attribute.IsBoolean)
            {
                result.Append("=\"");
                string value = InvariantFormatter.NormalizeLineEndings(attribute.Value ?? string.Empty);
                result.Append(HtmlEscaper.EscapeAttributeValue(value, HtmlInvalidCharacterPolicy.Reject));
                result.Append('"');
            }

            index++;
        }
    }

    internal void AppendNamespacePrefixes(StringBuilder result, DomAttribute[] attributes)
    {
        List<string> prefixes = new List<string>();
        List<string> namespaceUris = new List<string>();
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute attribute = attributes[index];
            if (attribute.NamespaceUri != null && attribute.Prefix != null)
            {
                AddNamespacePrefix(prefixes, namespaceUris, attribute.Prefix, attribute.NamespaceUri);
            }

            index++;
        }

        index = 0;
        while (index < prefixes.Count)
        {
            result.Append(" xmlns:");
            result.Append(prefixes[index]);
            result.Append("=\"");
            string namespaceUri = InvariantFormatter.NormalizeLineEndings(namespaceUris[index]);
            result.Append(HtmlEscaper.EscapeAttributeValue(namespaceUri, HtmlInvalidCharacterPolicy.Reject));
            result.Append('"');
            index++;
        }
    }

    internal void AddNamespacePrefix(List<string> prefixes, List<string> namespaceUris, string prefix, string namespaceUri)
    {
        int index = 0;
        while (index < prefixes.Count)
        {
            int comparison = string.CompareOrdinal(prefixes[index], prefix);
            if (comparison == 0)
            {
                if (namespaceUris[index] != namespaceUri)
                {
                    throw new ArgumentException("One namespace prefix cannot identify multiple namespace URIs.", "node");
                }

                return;
            }

            if (comparison > 0)
            {
                prefixes.Insert(index, prefix);
                namespaceUris.Insert(index, namespaceUri);
                return;
            }

            index++;
        }

        prefixes.Add(prefix);
        namespaceUris.Add(namespaceUri);
    }

    internal DomAttribute[] CopyAndSortAttributes(DomAttribute[]? supplied, bool isHtml)
    {
        DomAttribute[] source = supplied ?? Array.Empty<DomAttribute>();
        DomAttribute[] result = new DomAttribute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            DomAttribute? attribute = source[index];
            if (attribute == null)
            {
                throw new ArgumentException("HTML attribute collections cannot contain null.", "node");
            }

            ValidateAttribute(attribute);
            DomAttribute copy = new DomAttribute
            {
                NamespaceUri = attribute.NamespaceUri,
                Prefix = attribute.Prefix,
                Name = attribute.Name,
                Value = attribute.Value,
                IsBoolean = attribute.IsBoolean
            };
            InsertCanonicalAttribute(result, index, copy, isHtml);
            index++;
        }

        ValidateUniqueAttributes(result, isHtml);
        return result;
    }

    internal void InsertCanonicalAttribute(DomAttribute[] attributes, int count, DomAttribute candidate, bool isHtml)
    {
        int index = count;
        while (index > 0 && CompareCanonicalAttributes(attributes[index - 1], candidate, isHtml) > 0)
        {
            attributes[index] = attributes[index - 1];
            index--;
        }

        attributes[index] = candidate;
    }

    internal int CompareCanonicalAttributes(DomAttribute left, DomAttribute right, bool isHtml)
    {
        int namespaceResult = string.CompareOrdinal(left.NamespaceUri ?? string.Empty, right.NamespaceUri ?? string.Empty);
        if (namespaceResult != 0)
        {
            return namespaceResult;
        }

        string leftName = isHtml && left.NamespaceUri == null ? HtmlSecurityPolicy.ToLowerAsciiText(left.Name) : left.Name;
        string rightName = isHtml && right.NamespaceUri == null ? HtmlSecurityPolicy.ToLowerAsciiText(right.Name) : right.Name;
        int nameResult = string.CompareOrdinal(leftName, rightName);
        if (nameResult != 0)
        {
            return nameResult;
        }

        return string.CompareOrdinal(left.Prefix ?? string.Empty, right.Prefix ?? string.Empty);
    }

    internal void ValidateUniqueAttributes(DomAttribute[] attributes, bool isHtml)
    {
        int index = 1;
        while (index < attributes.Length)
        {
            DomAttribute previous = attributes[index - 1];
            DomAttribute current = attributes[index];
            string previousName = isHtml && previous.NamespaceUri == null ? HtmlSecurityPolicy.ToLowerAsciiText(previous.Name) : previous.Name;
            string currentName = isHtml && current.NamespaceUri == null ? HtmlSecurityPolicy.ToLowerAsciiText(current.Name) : current.Name;
            if (previous.NamespaceUri == current.NamespaceUri && previousName == currentName)
            {
                throw new ArgumentException("HTML attributes must be unique within a namespace.", "node");
            }

            index++;
        }
    }

    internal void ValidateElementNode(DomNode node)
    {
        if (!IsValidMarkupName(node.Name))
        {
            throw new ArgumentException("HTML elements require valid non-empty names.", "node");
        }

        if (node.NamespaceUri != null)
        {
            HtmlEscaper.NormalizeHtmlUnicode(node.NamespaceUri, HtmlInvalidCharacterPolicy.Reject);
        }
    }

    internal void ValidateAttribute(DomAttribute attribute)
    {
        if (!IsValidMarkupName(attribute.Name))
        {
            throw new ArgumentException("HTML attributes require valid non-empty names.", "node");
        }

        if (attribute.Prefix != null && (!IsValidMarkupName(attribute.Prefix) || attribute.NamespaceUri == null || attribute.Prefix == "xmlns"))
        {
            throw new ArgumentException("Namespaced HTML attributes require a valid non-reserved prefix and namespace URI.", "node");
        }

        if (attribute.NamespaceUri != null && attribute.Prefix == null)
        {
            throw new ArgumentException("Namespaced HTML attributes require an explicit prefix.", "node");
        }

        if (attribute.Name == "xmlns" && attribute.NamespaceUri == null)
        {
            throw new ArgumentException("Namespace declarations are generated by the serializer.", "node");
        }
    }

    internal bool IsValidMarkupName(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isLetter = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (index == 0 && !isLetter || index > 0 && !isLetter && !isDigit && character != '-' && character != '_' && character != '.')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsHtmlNamespace(string? namespaceUri)
    {
        return namespaceUri == null || namespaceUri == "http://www.w3.org/1999/xhtml";
    }

    internal bool IsVoidHtmlElement(string name)
    {
        return name == "area" || name == HtmlElements.Base || name == HtmlElements.LineBreak || name == HtmlElements.Column || name == HtmlElements.Embed || name == HtmlElements.HorizontalRule || name == HtmlElements.Image || name == HtmlElements.Input || name == HtmlElements.Link || name == HtmlElements.Meta || name == HtmlElements.Source || name == HtmlElements.Track || name == "wbr";
    }

    internal bool IsRawTextHtmlElement(string name)
    {
        return name == HtmlElements.Script || name == HtmlElements.Style || name == HtmlElements.Title || name == HtmlElements.TextArea;
    }

    internal bool ContainsAncestor(List<DomNode> ancestors, DomNode candidate)
    {
        int index = 0;
        while (index < ancestors.Count)
        {
            if (object.ReferenceEquals(ancestors[index], candidate))
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
