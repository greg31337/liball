using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ContentTemplateExpander
{
    internal DomNode ExpandTextContent(UiNode node, DomNode[] logicalChildren)
    {
        UiText? content = node.TextContent;
        if (content == null)
        {
            throw CreateRenderException(node, "A text control requires a text description.");
        }

        switch (content.Kind)
        {
            case UiTextKind.Plain:
                return ExpandOrdinaryText(node, content, HtmlElements.Span, "htmlgui-text", logicalChildren);
            case UiTextKind.Heading:
                return ExpandHeading(node, content, logicalChildren);
            case UiTextKind.Paragraph:
                return ExpandOrdinaryText(node, content, HtmlElements.Paragraph, "htmlgui-paragraph", logicalChildren);
            case UiTextKind.Label:
                return ExpandOrdinaryText(node, content, HtmlElements.Label, "htmlgui-label", logicalChildren);
            case UiTextKind.Code:
                return ExpandOrdinaryText(node, content, HtmlElements.Code, "htmlgui-code", logicalChildren);
            case UiTextKind.Preformatted:
                return ExpandOrdinaryText(node, content, HtmlElements.Preformatted, "htmlgui-preformatted", logicalChildren);
            case UiTextKind.Quote:
                return ExpandOrdinaryText(node, content, HtmlElements.BlockQuote, "htmlgui-quote", logicalChildren);
            case UiTextKind.Link:
                return ExpandLink(node, content, logicalChildren);
            case UiTextKind.Icon:
                return ExpandIcon(node, content);
            case UiTextKind.Image:
                return ExpandImage(node, content);
            case UiTextKind.Figure:
                return ExpandFigure(node, content, logicalChildren);
            case UiTextKind.Badge:
                return ExpandOrdinaryText(node, content, HtmlElements.Span, "htmlgui-badge", logicalChildren);
            case UiTextKind.Chip:
                return ExpandOrdinaryText(node, content, HtmlElements.Span, "htmlgui-chip", logicalChildren);
            case UiTextKind.Status:
                return ExpandStatus(node, content, logicalChildren);
            case UiTextKind.Landmark:
                return ExpandLandmark(node, content, logicalChildren);
            case UiTextKind.Localized:
                return ExpandLocalized(node, content, logicalChildren);
            default:
                throw new ArgumentOutOfRangeException("kind", content.Kind, "The text content kind is not supported.");
        }
    }

    internal bool IsTextControlName(string name)
    {
        switch (name)
        {
            case "text":
            case "heading":
            case "paragraph":
            case "label":
            case "code":
            case "pre":
            case "preformatted":
            case "quote":
            case "link":
            case "icon":
            case "image":
            case "figure":
            case "badge":
            case "chip":
            case "status":
            case "landmark":
            case "localized":
            case "localized-text":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandOrdinaryText(UiNode node, UiText content, string elementName, string className, DomNode[] logicalChildren)
    {
        return CreateElement(
            node,
            elementName,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = className } },
            PrependText(node, content.Text, logicalChildren));
    }

    internal DomNode ExpandHeading(UiNode node, UiText content, DomNode[] logicalChildren)
    {
        int level = 1;
        if (content.HasHeadingLevel)
        {
            level = content.HeadingLevel;
        }

        string elementName;
        switch (level)
        {
            case 1:
                elementName = HtmlElements.Heading1;
                break;
            case 2:
                elementName = HtmlElements.Heading2;
                break;
            case 3:
                elementName = HtmlElements.Heading3;
                break;
            case 4:
                elementName = HtmlElements.Heading4;
                break;
            case 5:
                elementName = HtmlElements.Heading5;
                break;
            case 6:
                elementName = HtmlElements.Heading6;
                break;
            default:
                throw new ArgumentOutOfRangeException("headingLevel", level, "Heading levels must be between one and six.");
        }

        return CreateElement(
            node,
            elementName,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-heading" } },
            PrependText(node, content.Text, logicalChildren));
    }

    internal DomNode ExpandLink(UiNode node, UiText content, DomNode[] logicalChildren)
    {
        string url = RequireUrl(node, content, "A link requires a non-empty URL.");
        DomAttribute[] attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-link" },
            new DomAttribute { Name = HtmlAttributes.HyperlinkReference, Value = url }
        };
        SortAttributeArray(attributes);
        return CreateElement(node, HtmlElements.Anchor, attributes, PrependText(node, content.Text, logicalChildren));
    }

    internal DomNode ExpandIcon(UiNode node, UiText content)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        string? alternativeText = content.AlternativeText;
        if (alternativeText != null && alternativeText.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = alternativeText });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Image });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" });
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-icon" });
        SortAttributeList(attributes);
        return CreateElement(node, HtmlElements.Span, attributes.ToArray(), new DomNode[] { CreateText(node, "$icon-text", "icon-text", content.Text) });
    }

    internal DomNode ExpandImage(UiNode node, UiText content)
    {
        string url = RequireUrl(node, content, "An image requires a non-empty URL.");
        DomAttribute[] attributes = CreateImageAttributes(content, url, "htmlgui-image");
        return CreateElement(node, HtmlElements.Image, attributes, Array.Empty<DomNode>());
    }

    internal DomNode ExpandFigure(UiNode node, UiText content, DomNode[] logicalChildren)
    {
        List<DomNode> children = new List<DomNode>();
        if (content.Url != null && !string.IsNullOrWhiteSpace(content.Url))
        {
            string imageUrl = RequireUrl(node, content, "A figure image requires a safe non-empty URL.");
            DomNode image = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = "$figure-image",
                Id = CreateSyntheticId(node, "figure-image"),
                Name = HtmlElements.Image,
                Attributes = CreateImageAttributes(content, imageUrl, "htmlgui-figure-image"),
                Version = GetNodeVersion(node)
            };
            children.Add(image);
        }

        int logicalIndex = 0;
        while (logicalIndex < logicalChildren.Length)
        {
            children.Add(logicalChildren[logicalIndex]);
            logicalIndex++;
        }

        if (content.Text.Length > 0)
        {
            DomNode caption = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = "$figure-caption",
                Id = CreateSyntheticId(node, "figure-caption"),
                Name = HtmlElements.FigureCaption,
                Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-figure-caption" } },
                Children = new DomNode[] { CreateText(node, "$figure-caption-text", "figure-caption-text", content.Text) },
                Version = GetNodeVersion(node)
            };
            children.Add(caption);
        }

        return CreateElement(
            node,
            HtmlElements.Figure,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-figure" } },
            children.ToArray());
    }

    internal DomNode ExpandStatus(UiNode node, UiText content, DomNode[] logicalChildren)
    {
        DomAttribute[] attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-status" },
            new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Status }
        };
        SortAttributeArray(attributes);
        return CreateElement(node, HtmlElements.Span, attributes, PrependText(node, content.Text, logicalChildren));
    }

    internal DomNode ExpandLandmark(UiNode node, UiText content, DomNode[] logicalChildren)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        if (content.AlternativeText != null && content.AlternativeText.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = content.AlternativeText });
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-landmark" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Region });
        SortAttributeList(attributes);
        return CreateElement(node, HtmlElements.Section, attributes.ToArray(), PrependText(node, content.Text, logicalChildren));
    }

    internal DomNode ExpandLocalized(UiNode node, UiText content, DomNode[] logicalChildren)
    {
        string? key = content.LocalizationKey;
        if (key == null || string.IsNullOrWhiteSpace(key))
        {
            key = content.Text;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw CreateRenderException(node, "Localized text requires a non-empty localization key or fallback text.");
        }

        DomAttribute[] attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-localized-text" },
            new DomAttribute { Name = HtmlAttributes.DataPrefix + "localization-key", Value = key }
        };
        SortAttributeArray(attributes);
        return CreateElement(node, HtmlElements.Span, attributes, PrependText(node, content.Text, logicalChildren));
    }

    internal DomAttribute[] CreateImageAttributes(UiText content, string url, string className)
    {
        DomAttribute[] attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.AlternativeText, Value = content.AlternativeText ?? string.Empty },
            new DomAttribute { Name = HtmlAttributes.Class, Value = className },
            new DomAttribute { Name = HtmlAttributes.Source, Value = url }
        };
        SortAttributeArray(attributes);
        return attributes;
    }

    internal string RequireUrl(UiNode node, UiText content, string message)
    {
        if (content.Url == null || string.IsNullOrWhiteSpace(content.Url))
        {
            throw CreateRenderException(node, message);
        }

        bool isHyperlink = content.Kind == UiTextKind.Link;
        bool isSafe = isHyperlink ? HtmlSecurityPolicy.IsSafeHyperlinkUrl(content.Url) : HtmlSecurityPolicy.IsSafeResourceUrl(content.Url);
        if (!isSafe)
        {
            throw CreateRenderException(node, "The content URL is malformed or uses an unsafe scheme.");
        }

        if (isHyperlink)
        {
            return HtmlSecurityPolicy.NormalizeHyperlinkUrl(content.Url);
        }

        return HtmlSecurityPolicy.NormalizeResourceUrl(content.Url);
    }

    internal DomNode CreateElement(UiNode node, string name, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = name,
            Attributes = attributes,
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode[] PrependText(UiNode node, string text, DomNode[] logicalChildren)
    {
        DomNode[] children = new DomNode[logicalChildren.Length + 1];
        children[0] = CreateText(node, "$text", "text", text);
        Array.Copy(logicalChildren, 0, children, 1, logicalChildren.Length);
        return children;
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal string CreateSyntheticId(UiNode node, string suffix)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "content", suffix });
        return "ui-content-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        if (node.Identity.AuthorKey.Length > 0)
        {
            return node.Identity.AuthorKey;
        }

        return node.Identity.SiblingIdentity;
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

    internal void SortAttributeArray(DomAttribute[] attributes)
    {
        int index = 1;
        while (index < attributes.Length)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal void SortAttributeList(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
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
