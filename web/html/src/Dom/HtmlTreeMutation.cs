using System;
using System.Collections.Generic;

namespace html;

internal static class HtmlTreeMutation
{
    internal static HtmlNode GetMutationTarget(HtmlNode node)
    {
        HtmlNode? content = node.GetTemplateContentStorage();
        return content == null ? node : content;
    }

    internal static void ValidateContainer(HtmlNode node)
    {
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind != HtmlNodeKind.Document
            && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            throw new InvalidOperationException(
                "This node kind cannot contain child nodes.");
        }
    }

    internal static void ValidateInsertion(HtmlNode parent, HtmlNode child)
    {
        ValidateContainer(parent);
        if (parent.GetOwnerDocumentStorage()
            != child.GetOwnerDocumentStorage())
        {
            throw new ArgumentException(
                "A child node belongs to a different document.", "child");
        }

        if (IsInclusiveAncestor(child, parent))
        {
            throw new InvalidOperationException(
                "A node cannot be inserted into itself or one of its descendants.");
        }

        if (child.GetKindStorage() == HtmlNodeKind.Fragment)
        {
            HtmlNode[] fragmentChildren = child.GetChildrenStorage();
            int index = 0;
            while (index < fragmentChildren.Length)
            {
                ValidateInsertion(parent, fragmentChildren[index]);
                index = index + 1;
            }

            return;
        }

        ValidateChildKind(parent, child);
    }

    internal static void ValidateChildren(HtmlNode parent, HtmlNode[] children)
    {
        ValidateContainer(parent);
        int elementCount = 0;
        int doctypeCount = 0;
        bool foundElement = false;
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child == null)
            {
                throw new InvalidOperationException(
                    "A child collection cannot contain null.");
            }

            if (parent.GetOwnerDocumentStorage()
                != child.GetOwnerDocumentStorage())
            {
                throw new ArgumentException(
                    "A child node belongs to a different document.", "children");
            }

            if (IsInclusiveAncestor(child, parent))
            {
                throw new InvalidOperationException(
                    "A node cannot be inserted into itself or one of its descendants.");
            }

            ValidateChildKind(parent, child);
            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                if (children[duplicateIndex] == child)
                {
                    throw new InvalidOperationException(
                        "A child node cannot occur more than once.");
                }

                duplicateIndex = duplicateIndex + 1;
            }

            if (parent.GetKindStorage() == HtmlNodeKind.Document)
            {
                HtmlNodeKind childKind = child.GetKindStorage();
                if (childKind == HtmlNodeKind.Element)
                {
                    elementCount = elementCount + 1;
                    foundElement = true;
                }
                else if (childKind == HtmlNodeKind.DocumentType)
                {
                    doctypeCount = doctypeCount + 1;
                    if (foundElement)
                    {
                        throw new InvalidOperationException(
                            "A document type must precede the document element.");
                    }
                }
            }

            index = index + 1;
        }

        if (elementCount > 1)
        {
            throw new InvalidOperationException(
                "A document cannot contain more than one document element.");
        }

        if (doctypeCount > 1)
        {
            throw new InvalidOperationException(
                "A document cannot contain more than one document type.");
        }
    }

    internal static string NormalizeAttributeName(HtmlNode element, string name)
    {
        ValidateAttributeName(name);
        return NormalizeAttributeLookupName(element, name);
    }

    internal static string NormalizeAttributeLookupName(HtmlNode element,
        string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (element.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return name;
        }

        char[] result = new char[name.Length];
        int index = 0;
        while (index < name.Length)
        {
            result[index] = Ascii.ToLower(name[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static void ValidateClassToken(string token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("className");
        }

        if (token.Length == 0)
        {
            throw new ArgumentException(
                "A class token cannot be empty.", "className");
        }

        int index = 0;
        while (index < token.Length)
        {
            if (Ascii.IsHtmlWhitespace(token[index]))
            {
                throw new ArgumentException(
                    "A class token cannot contain HTML whitespace.",
                    "className");
            }

            index = index + 1;
        }
    }

    internal static bool ContainsClassToken(string value, string token)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && Ascii.IsHtmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < value.Length
                && !Ascii.IsHtmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            if (TokenEquals(value, start, index, token))
            {
                return true;
            }
        }

        return false;
    }

    internal static string AddClassToken(string value, string token)
    {
        List<string> tokens = ParseClassTokens(value, "");
        tokens.Add(token);
        return JoinClassTokens(tokens);
    }

    internal static string RemoveClassToken(string value, string token)
    {
        List<string> tokens = ParseClassTokens(value, token);
        return JoinClassTokens(tokens);
    }

    internal static string ReplaceClassToken(string value, string oldToken,
        string newToken)
    {
        List<string> tokens = ParseClassTokens(value, "");
        int oldIndex = -1;
        int newIndex = -1;
        int index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index] == oldToken)
            {
                oldIndex = index;
            }

            if (tokens[index] == newToken)
            {
                newIndex = index;
            }

            index = index + 1;
        }

        if (oldIndex < 0)
        {
            return value;
        }

        if (newIndex >= 0 && newIndex != oldIndex)
        {
            tokens.RemoveAt(oldIndex);
        }
        else
        {
            tokens[oldIndex] = newToken;
        }

        return JoinClassTokens(tokens);
    }

    internal static HtmlAttribute CreateNamespacedAttribute(
        string namespaceUri, string qualifiedName, string value,
        HtmlSourceSpan sourceSpan)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        HtmlDocument.ValidateQualifiedName(qualifiedName);
        int colon = qualifiedName.IndexOf(':');
        string prefix = colon < 0 ? "" : qualifiedName.Substring(0, colon);
        string localName = colon < 0 ? qualifiedName
            : qualifiedName.Substring(colon + 1);
        HtmlDocument.ValidateNamespaceName(namespaceUri, qualifiedName,
            prefix);
        return HtmlAttribute.CreateNamespaced(qualifiedName, localName,
            prefix, namespaceUri, value, sourceSpan);
    }

    internal static string DatasetNameToAttribute(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException(
                "A dataset name cannot be empty.", "name");
        }

        string result = "data-";
        int index = 0;
        while (index < name.Length)
        {
            char current = name[index];
            if (current == '-' && index + 1 < name.Length
                && name[index + 1] >= 'a' && name[index + 1] <= 'z')
            {
                throw new ArgumentException(
                    "A dataset name cannot contain a hyphen followed by an ASCII lowercase letter.",
                    "name");
            }

            if (current >= 'A' && current <= 'Z')
            {
                result = result + "-" + Ascii.ToLower(current);
            }
            else
            {
                result = result + current;
            }

            index = index + 1;
        }

        ValidateAttributeName(result);
        return result;
    }

    internal static string? AttributeToDatasetName(string attributeName)
    {
        if (attributeName.Length < 6 || !StartsWithData(attributeName))
        {
            return null;
        }

        int uppercaseIndex = 5;
        while (uppercaseIndex < attributeName.Length)
        {
            char candidate = attributeName[uppercaseIndex];
            if (candidate >= 'A' && candidate <= 'Z')
            {
                return null;
            }

            uppercaseIndex = uppercaseIndex + 1;
        }

        string result = "";
        int index = 5;
        while (index < attributeName.Length)
        {
            char current = attributeName[index];
            if (current == '-' && index + 1 < attributeName.Length
                && attributeName[index + 1] >= 'a'
                && attributeName[index + 1] <= 'z')
            {
                result = result + Ascii.ToUpper(attributeName[index + 1]);
                index = index + 2;
            }
            else
            {
                result = result + current;
                index = index + 1;
            }
        }

        return result.Length == 0 ? null : result;
    }

    internal static bool StartsWithData(string value)
    {
        return value.Length >= 5 && value[0] == 'd' && value[1] == 'a'
            && value[2] == 't' && value[3] == 'a' && value[4] == '-';
    }

    internal static void ValidateChildKind(HtmlNode parent, HtmlNode child)
    {
        HtmlNodeKind childKind = child.GetKindStorage();
        if (parent.GetKindStorage() == HtmlNodeKind.Document)
        {
            if (childKind != HtmlNodeKind.Element
                && childKind != HtmlNodeKind.Comment
                && childKind != HtmlNodeKind.DocumentType)
            {
                throw new InvalidOperationException(
                    "A document can contain only elements, comments, and a document type.");
            }

            return;
        }

        if (childKind != HtmlNodeKind.Element
            && childKind != HtmlNodeKind.Text
            && childKind != HtmlNodeKind.Comment)
        {
            throw new InvalidOperationException(
                "This node kind cannot be inserted into this parent.");
        }
    }

    internal static bool IsInclusiveAncestor(HtmlNode ancestor, HtmlNode node)
    {
        HtmlNode? current = node;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            HtmlNode? host = current.GetTemplateHostStorage();
            current = host == null ? current.GetParentStorage() : host;
        }

        return false;
    }

    internal static void ValidateAttributeName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException(
                "An attribute name cannot be empty.", "name");
        }

        int index = 0;
        while (index < name.Length)
        {
            char current = name[index];
            bool allowed;
            if (index == 0)
            {
                allowed = Ascii.IsAlpha(current) || current == '_'
                    || current == ':' || current >= 128;
            }
            else
            {
                allowed = Ascii.IsAlphanumeric(current) || current == '_'
                    || current == ':' || current == '-' || current == '.'
                    || current >= 128;
            }

            if (!allowed || UnicodeTools.IsLowSurrogate(current))
            {
                throw new ArgumentException(
                    "An attribute name contains an invalid character.",
                    "name");
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= name.Length
                    || !UnicodeTools.IsLowSurrogate(name[index + 1]))
                {
                    throw new ArgumentException(
                        "An attribute name contains an invalid Unicode scalar.",
                        "name");
                }

                index = index + 1;
            }

            index = index + 1;
        }
    }

    internal static bool TokenEquals(string value, int start, int end, string token)
    {
        if (end - start != token.Length)
        {
            return false;
        }

        int index = 0;
        while (index < token.Length)
        {
            if (value[start + index] != token[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static List<string> ParseClassTokens(string value, string excluded)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && Ascii.IsHtmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < value.Length
                && !Ascii.IsHtmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            if (start < index)
            {
                string token = value.Substring(start, index - start);
                if (token != excluded && !ContainsToken(result, token))
                {
                    result.Add(token);
                }
            }
        }

        return result;
    }

    internal static bool ContainsToken(List<string> values, string token)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == token)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string JoinClassTokens(List<string> values)
    {
        string result = "";
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                result = result + " ";
            }

            result = result + values[index];
            index = index + 1;
        }

        return result;
    }
}
