using System;
using System.Collections.Generic;

namespace htmlgui;

internal class RenderTreeBuilder
{
    internal DomNode BuildLowLevelNode(UiNode node, DomNode[] children)
    {
        switch (node.Kind)
        {
            case UiNodeKind.Element:
                return BuildElement(node, children);
            case UiNodeKind.Text:
                return BuildText(node);
            case UiNodeKind.Comment:
                return BuildComment(node);
            case UiNodeKind.RawContent:
                return BuildRawContent(node);
            default:
                throw CreateRenderException(node, "The node is not a low-level render node.");
        }
    }

    internal DomNode BuildElement(UiNode node, DomNode[] children)
    {
        ValidateElementName(node);
        ValidateElementChildren(node, children);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            NamespaceUri = node.NamespaceUri,
            Name = node.Name,
            Attributes = BuildAttributes(node),
            Properties = BuildProperties(node),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode BuildText(UiNode node)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Text = node.Text ?? string.Empty,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode BuildComment(UiNode node)
    {
        string text = node.Text ?? string.Empty;
        if (text.Contains("--", StringComparison.Ordinal) || text.StartsWith("-", StringComparison.Ordinal) || text.EndsWith("-", StringComparison.Ordinal))
        {
            throw CreateRenderException(node, "Comment text cannot contain a double hyphen or begin or end with a hyphen.");
        }

        return new DomNode
        {
            Kind = DomNodeKind.Comment,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode BuildRawContent(UiNode node)
    {
        UiRawContentPolicy? policy = node.RawContentPolicy;
        string policyId;
        if (policy == null || !policy.IsTrusted || !HtmlSecurityPolicy.IsTrustPolicyIdentifier(policy.PolicyId))
        {
            throw CreateRenderException(node, "Raw content requires explicit trust from a named policy.");
        }

        string text = node.Text ?? string.Empty;
        if (!HtmlSecurityPolicy.IsSafeRawContentUnicode(text))
        {
            throw CreateRenderException(node, "Raw content must contain valid HTML Unicode scalar values.");
        }

        policyId = HtmlSecurityPolicy.ValidateRawContentTrust(policy, text);
        return new DomNode
        {
            Kind = DomNodeKind.RawContent,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Text = text,
            RawContentPolicyId = policyId,
            Version = GetNodeVersion(node)
        };
    }

    internal DomAttribute[] BuildAttributes(UiNode node)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        DomAttribute[]? suppliedAttributes = node.Attributes;
        if (suppliedAttributes != null)
        {
            int attributeIndex = 0;
            while (attributeIndex < suppliedAttributes.Length)
            {
                DomAttribute? attribute = suppliedAttributes[attributeIndex];
                if (attribute == null)
                {
                    throw CreateRenderException(node, "Element attributes cannot contain null.");
                }

                ValidateAttributeName(node, attribute.Name);
                DomAttribute copy = new DomAttribute
                {
                    NamespaceUri = attribute.NamespaceUri,
                    Prefix = attribute.Prefix,
                    Name = attribute.Name,
                    Value = attribute.Value,
                    IsBoolean = attribute.IsBoolean
                };
                if (copy.IsBoolean)
                {
                    copy.Value = string.Empty;
                }
                else if (IsHyperlinkAttribute(copy.Name))
                {
                    if (!HtmlSecurityPolicy.IsSafeHyperlinkUrl(copy.Value))
                    {
                        throw CreateRenderException(node, "The hyperlink attribute is malformed or uses an unsafe scheme.");
                    }

                    copy.Value = HtmlSecurityPolicy.NormalizeHyperlinkUrl(copy.Value);
                }
                else if (IsResourceUrlAttribute(copy.Name))
                {
                    if (!HtmlSecurityPolicy.IsSafeResourceUrl(copy.Value))
                    {
                        throw CreateRenderException(node, "The resource URL attribute is malformed or uses an unsafe scheme.");
                    }

                    copy.Value = HtmlSecurityPolicy.NormalizeResourceUrl(copy.Value);
                }

                AddUniqueAttribute(node, attributes, copy);
                attributeIndex++;
            }
        }

        AddTokenLists(node, attributes, node.TokenLists);
        AddDataAttributes(node, attributes, node.DataAttributes);
        SortAttributes(attributes);
        return attributes.ToArray();
    }

    internal void AddTokenLists(UiNode node, List<DomAttribute> attributes, UiTokenList[]? tokenLists)
    {
        if (tokenLists == null)
        {
            return;
        }

        int index = 0;
        while (index < tokenLists.Length)
        {
            UiTokenList? tokenList = tokenLists[index];
            if (tokenList == null)
            {
                throw CreateRenderException(node, "Element token lists cannot contain null.");
            }

            ValidateAttributeName(node, tokenList.Name);
            DomAttribute attribute = new DomAttribute { Name = tokenList.Name, Value = CanonicalizeTokens(node, tokenList.Tokens) };
            AddUniqueAttribute(node, attributes, attribute);
            index++;
        }
    }

    internal void AddDataAttributes(UiNode node, List<DomAttribute> attributes, UiValueEntry[]? dataAttributes)
    {
        if (dataAttributes == null)
        {
            return;
        }

        int index = 0;
        while (index < dataAttributes.Length)
        {
            UiValueEntry? entry = dataAttributes[index];
            if (entry == null || !IsValidDataName(entry.Key))
            {
                throw CreateRenderException(node, "Data attribute names must be non-empty lowercase names without the data prefix.");
            }

            DomAttribute attribute = new DomAttribute
            {
                Name = HtmlAttributes.DataPrefix + entry.Key,
                Value = FormatAttributeValue(entry.Value)
            };
            AddUniqueAttribute(node, attributes, attribute);
            index++;
        }
    }

    internal DomProperty[] BuildProperties(UiNode node)
    {
        DomProperty[]? supplied = node.Properties;
        if (supplied == null)
        {
            return Array.Empty<DomProperty>();
        }

        DomProperty[] result = new DomProperty[supplied.Length];
        int index = 0;
        while (index < supplied.Length)
        {
            DomProperty? property = supplied[index];
            if (property == null || !IsValidMemberName(property.Name))
            {
                throw CreateRenderException(node, "DOM properties require valid non-empty names.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (result[earlierIndex].Name == property.Name)
                {
                    throw CreateRenderException(node, "DOM property names must be unique.");
                }

                earlierIndex++;
            }

            result[index] = new DomProperty
            {
                Name = property.Name,
                Value = ValueJsonCodec.CloneValue(property.Value, 64, 10000, 1048576, 1048576)
            };
            index++;
        }

        SortProperties(result);
        return result;
    }

    internal string CanonicalizeTokens(UiNode node, string[]? tokens)
    {
        if (tokens == null || tokens.Length == 0)
        {
            return string.Empty;
        }

        List<string> unique = new List<string>();
        int index = 0;
        while (index < tokens.Length)
        {
            string? token = tokens[index];
            if (string.IsNullOrWhiteSpace(token) || ContainsHtmlWhiteSpace(token))
            {
                throw CreateRenderException(node, "Attribute tokens must be non-empty and cannot contain HTML whitespace.");
            }

            if (!ContainsToken(unique, token))
            {
                unique.Add(token);
            }

            index++;
        }

        unique.Sort(StringComparer.Ordinal);
        return string.Join(" ", unique);
    }

    internal bool ContainsToken(List<string> tokens, string candidate)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index] == candidate)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void AddUniqueAttribute(UiNode node, List<DomAttribute> attributes, DomAttribute candidate)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            DomAttribute existing = attributes[index];
            if (existing.NamespaceUri == candidate.NamespaceUri && existing.Name == candidate.Name)
            {
                throw CreateRenderException(node, "Element attribute names must be unique within a namespace.");
            }

            index++;
        }

        attributes.Add(candidate);
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && CompareAttributes(attributes[insertionIndex], candidate) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal int CompareAttributes(DomAttribute left, DomAttribute right)
    {
        int namespaceResult = string.CompareOrdinal(left.NamespaceUri ?? string.Empty, right.NamespaceUri ?? string.Empty);
        if (namespaceResult != 0)
        {
            return namespaceResult;
        }

        int nameResult = string.CompareOrdinal(left.Name, right.Name);
        if (nameResult != 0)
        {
            return nameResult;
        }

        return string.CompareOrdinal(left.Prefix ?? string.Empty, right.Prefix ?? string.Empty);
    }

    internal void SortProperties(DomProperty[] properties)
    {
        int index = 1;
        while (index < properties.Length)
        {
            DomProperty candidate = properties[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(properties[insertionIndex].Name, candidate.Name) > 0)
            {
                properties[insertionIndex + 1] = properties[insertionIndex];
                insertionIndex--;
            }

            properties[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal void ValidateElementName(UiNode node)
    {
        if (!IsValidElementName(node.Name))
        {
            throw CreateRenderException(node, "Element names must use valid ASCII name characters.");
        }

        bool hasHyphen = node.Name.Contains("-", StringComparison.Ordinal);
        if (node.IsCustomElement)
        {
            if (!hasHyphen || !IsLowerAsciiName(node.Name))
            {
                throw CreateRenderException(node, "Custom element names must be lowercase and contain a hyphen.");
            }
        }
        else if (hasHyphen && node.NamespaceUri == null)
        {
            throw CreateRenderException(node, "Custom element names require explicit opt-in.");
        }
    }

    internal void ValidateElementChildren(UiNode node, DomNode[] children)
    {
        if (node.NamespaceUri != null)
        {
            return;
        }

        if (IsVoidElement(node.Name) && children.Length > 0)
        {
            throw CreateRenderException(node, "Void HTML elements cannot contain child nodes.");
        }

        if (IsRawTextElement(node.Name) || IsEscapableRawTextElement(node.Name))
        {
            int index = 0;
            while (index < children.Length)
            {
                if (children[index].Kind != DomNodeKind.Text)
                {
                    throw CreateRenderException(node, "Raw-text HTML elements can contain only text nodes.");
                }

                index++;
            }
        }
    }

    internal void ValidateAttributeName(UiNode node, string name)
    {
        if (!IsValidMemberName(name))
        {
            throw CreateRenderException(node, "Element attributes require valid non-empty names.");
        }
    }

    internal bool IsValidElementName(string? name)
    {
        if (name == null || name.Length == 0 || !IsAsciiLetter(name[0]))
        {
            return false;
        }

        int index = 1;
        while (index < name.Length)
        {
            char character = name[index];
            if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '-' && character != '_')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsValidMemberName(string? name)
    {
        if (name == null || name.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (!IsAsciiLetter(character) && !IsAsciiDigit(character) && character != '-' && character != '_' && character != ':' && character != '.')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsHyperlinkAttribute(string name)
    {
        return name == HtmlAttributes.HyperlinkReference || name == "action" || name == "formaction" || name == "cite";
    }

    internal bool IsResourceUrlAttribute(string name)
    {
        return name == HtmlAttributes.Source || name == "poster" || name == "background";
    }

    internal bool IsValidDataName(string? name)
    {
        if (!IsValidMemberName(name) || name == null || name.StartsWith(HtmlAttributes.DataPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (character >= 'A' && character <= 'Z')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsLowerAsciiName(string name)
    {
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (character >= 'A' && character <= 'Z')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsAsciiLetter(char character)
    {
        return character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z';
    }

    internal bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal bool ContainsHtmlWhiteSpace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == ' ' || character == '\t' || character == '\n' || character == '\r' || character == '\f')
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool IsVoidElement(string name)
    {
        return name == "area" || name == HtmlElements.Base || name == HtmlElements.LineBreak || name == HtmlElements.Column || name == HtmlElements.Embed || name == HtmlElements.HorizontalRule || name == HtmlElements.Image || name == HtmlElements.Input || name == HtmlElements.Link || name == HtmlElements.Meta || name == HtmlElements.Source || name == HtmlElements.Track || name == "wbr";
    }

    internal bool IsRawTextElement(string name)
    {
        return name == HtmlElements.Script || name == HtmlElements.Style;
    }

    internal bool IsEscapableRawTextElement(string name)
    {
        return name == HtmlElements.Title || name == HtmlElements.TextArea;
    }

    internal string FormatAttributeValue(UiValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        switch (value.Kind)
        {
            case UiValueKind.Null:
                return string.Empty;
            case UiValueKind.Boolean:
                return InvariantFormatter.FormatBoolean(value.BooleanValue);
            case UiValueKind.Integer:
                return InvariantFormatter.FormatInt64(value.IntegerValue);
            case UiValueKind.FloatingPoint:
                return InvariantFormatter.FormatDouble(value.FloatingPointValue);
            case UiValueKind.Decimal:
                return InvariantFormatter.FormatDecimal(value.DecimalValue);
            case UiValueKind.String:
                return value.StringValue ?? string.Empty;
            case UiValueKind.DateTime:
                return value.DateTimeValue ?? string.Empty;
            case UiValueKind.Binary:
                return Convert.ToBase64String(value.BinaryValue ?? Array.Empty<byte>());
            case UiValueKind.Array:
            case UiValueKind.Object:
                return ValueJsonCodec.SerializeValue(value, 64, 10000, 1048576, 1048576);
            default:
                throw new ArgumentOutOfRangeException("kind", value.Kind, "The attribute value kind is not supported.");
        }
    }

    internal string GetNodeKey(UiNode node)
    {
        UiNodeIdentity? identity = node.Identity;
        if (identity == null)
        {
            return string.Empty;
        }

        if (identity.AuthorKey.Length > 0)
        {
            return identity.AuthorKey;
        }

        return identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
