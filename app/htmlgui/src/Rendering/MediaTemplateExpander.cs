using System;
using System.Collections.Generic;

namespace htmlgui;

internal class MediaTemplateExpander
{
    internal DomNode ExpandMedia(UiNode node, DomNode[] logicalChildren, DomNode[] fallbackChildren, string[] capabilities)
    {
        UiMedia? media = node.Media;
        if (media == null)
        {
            throw CreateRenderException(node, "A media control requires a media description.");
        }

        ValidateMediaDescription(node, media);
        if (!HasMediaCapability(media, capabilities))
        {
            if (fallbackChildren.Length == 0)
            {
                throw CreateRenderException(node, "The required media capability is not enabled and no fallback content is available.");
            }

            return CreateFragment(node, fallbackChildren);
        }

        switch (media.Kind)
        {
            case "audio":
                return ExpandAudio(node, media, CombineChildren(logicalChildren, fallbackChildren));
            case "video":
                return ExpandVideo(node, media, CombineChildren(logicalChildren, fallbackChildren));
            case "embed":
                return ExpandEmbed(node, media, CombineChildren(logicalChildren, fallbackChildren));
            case "inline-svg":
                return ExpandInlineSvg(node, media, logicalChildren);
            default:
                throw CreateRenderException(node, "The media kind is not supported.");
        }
    }

    internal bool IsMediaControlName(string name)
    {
        return name == "audio" || name == "video" || name == "embed" || name == "inline-svg";
    }

    internal bool HasMediaCapability(UiMedia media, string[]? capabilities)
    {
        if (capabilities == null)
        {
            return false;
        }

        string dottedName = "media." + media.Kind;
        string colonName = "media:" + media.Kind;
        int index = 0;
        while (index < capabilities.Length)
        {
            string? capability = capabilities[index];
            if (capability == "media" || capability == media.Kind || capability == dottedName || capability == colonName)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal DomNode ExpandAudio(UiNode node, UiMedia media, DomNode[] children)
    {
        List<DomAttribute> attributes = CreateMediaAttributes(media, "audio");
        AddPlaybackAttributes(media, attributes);
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Audio, null, attributes.ToArray(), children);
    }

    internal DomNode ExpandVideo(UiNode node, UiMedia media, DomNode[] children)
    {
        List<DomAttribute> attributes = CreateMediaAttributes(media, "video");
        AddPlaybackAttributes(media, attributes);
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Video, null, attributes.ToArray(), children);
    }

    internal DomNode ExpandEmbed(UiNode node, UiMedia media, DomNode[] children)
    {
        List<DomAttribute> attributes = CreateMediaAttributes(media, "embed");
        attributes.Add(new DomAttribute { Name = "sandbox", Value = string.Empty });
        if (media.AlternativeText != null && media.AlternativeText.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Title, Value = media.AlternativeText });
        }

        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.InlineFrame, null, attributes.ToArray(), children);
    }

    internal DomNode ExpandInlineSvg(UiNode node, UiMedia media, DomNode[] children)
    {
        ValidateSvgChildren(node, children);
        List<DomAttribute> attributes = new List<DomAttribute>();
        if (media.AlternativeText != null && media.AlternativeText.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = media.AlternativeText });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Image });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" });
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-inline-svg" });
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Svg, "http://www.w3.org/2000/svg", attributes.ToArray(), children);
    }

    internal List<DomAttribute> CreateMediaAttributes(UiMedia media, string classToken)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        if (media.AlternativeText != null && media.AlternativeText.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = media.AlternativeText });
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-" + classToken });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Source, Value = HtmlSecurityPolicy.NormalizeResourceUrl(media.Source) });
        if (media.MimeType != null && media.MimeType.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "mime-type", Value = media.MimeType });
        }

        return attributes;
    }

    internal void AddPlaybackAttributes(UiMedia media, List<DomAttribute> attributes)
    {
        if (media.ShowControls)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Controls, Value = string.Empty, IsBoolean = true });
        }

        if (media.AutoPlay)
        {
            attributes.Add(new DomAttribute { Name = "autoplay", Value = string.Empty, IsBoolean = true });
        }

        if (media.Muted)
        {
            attributes.Add(new DomAttribute { Name = "muted", Value = string.Empty, IsBoolean = true });
        }

        if (media.Loop)
        {
            attributes.Add(new DomAttribute { Name = "loop", Value = string.Empty, IsBoolean = true });
        }
    }

    internal void ValidateMediaDescription(UiNode node, UiMedia media)
    {
        if (!IsMediaControlName(media.Kind))
        {
            throw CreateRenderException(node, "The media kind is not supported.");
        }

        if (media.Kind == "inline-svg")
        {
            if (!string.IsNullOrEmpty(media.Source))
            {
                throw CreateRenderException(node, "Inline SVG must use structured namespaced children instead of source markup.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(media.Source))
        {
            throw CreateRenderException(node, "External media requires a non-empty source URL.");
        }

        if (!IsSafeResourceUrl(media.Source))
        {
            throw CreateRenderException(node, "The media source URL uses an unsafe or unsupported form.");
        }
    }

    internal bool IsSafeResourceUrl(string value)
    {
        return HtmlSecurityPolicy.IsSafeResourceUrl(value);
    }

    internal void ValidateSvgChildren(UiNode node, DomNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            ValidateSvgDescendant(node, children[index]);
            index++;
        }
    }

    internal void ValidateSvgDescendant(UiNode node, DomNode child)
    {
        if (child.Kind == DomNodeKind.Element && child.NamespaceUri != "http://www.w3.org/2000/svg")
        {
            throw CreateRenderException(node, "Inline SVG element children must use the SVG namespace.");
        }

        if (child.Kind == DomNodeKind.RawContent)
        {
            throw CreateRenderException(node, "Inline SVG cannot contain raw markup content.");
        }

        int index = 0;
        while (index < child.Children.Length)
        {
            ValidateSvgDescendant(node, child.Children[index]);
            index++;
        }
    }

    internal DomNode[] CombineChildren(DomNode[] logicalChildren, DomNode[] fallbackChildren)
    {
        if (logicalChildren.Length == 0)
        {
            return fallbackChildren;
        }

        if (fallbackChildren.Length == 0)
        {
            return logicalChildren;
        }

        DomNode[] result = new DomNode[logicalChildren.Length + fallbackChildren.Length];
        Array.Copy(logicalChildren, result, logicalChildren.Length);
        Array.Copy(fallbackChildren, 0, result, logicalChildren.Length, fallbackChildren.Length);
        return result;
    }

    internal DomNode CreateFragment(UiNode node, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateElement(UiNode node, string name, string? namespaceUri, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            NamespaceUri = namespaceUri,
            Name = name,
            Attributes = attributes,
            Children = children,
            Version = GetNodeVersion(node)
        };
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

    internal void SortAttributes(List<DomAttribute> attributes)
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
