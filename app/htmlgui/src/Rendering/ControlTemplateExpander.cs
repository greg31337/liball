using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ControlTemplateExpander
{
    internal DomNode ExpandControl(UiNode node, DomNode[] logicalChildren, DomNode[] disclosureChildren)
    {
        switch (node.Name)
        {
            case "spacer":
                return ExpandSpacer(node);
            case "separator":
                return ExpandSeparator(node);
            case "card":
                return ExpandCard(node, logicalChildren);
            case "group-box":
            case "groupbox":
                return ExpandGroupBox(node, logicalChildren);
            case "field-set":
            case "fieldset":
                return ExpandFieldSet(node, logicalChildren);
            case "disclosure":
                return ExpandDisclosure(node, disclosureChildren);
            case "collapsible-region":
            case "collapsible":
                return ExpandCollapsibleRegion(node, disclosureChildren);
            case "resizable-panel":
                return ExpandResizablePanel(node, logicalChildren);
            default:
                return CreateFragment(node, logicalChildren);
        }
    }

    internal bool IgnoresLogicalChildren(string name)
    {
        return name == "spacer" || name == "separator";
    }

    internal bool UsesDisclosureContent(string name)
    {
        return name == "disclosure" || name == "collapsible-region" || name == "collapsible";
    }

    internal DomNode ExpandSpacer(UiNode node)
    {
        return CreateElement(
            node,
            HtmlElements.Division,
            new DomAttribute[]
            {
                new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" },
                new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-spacer" }
            },
            Array.Empty<DomNode>());
    }

    internal DomNode ExpandSeparator(UiNode node)
    {
        return CreateElement(
            node,
            HtmlElements.HorizontalRule,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-separator" } },
            Array.Empty<DomNode>());
    }

    internal DomNode ExpandCard(UiNode node, DomNode[] children)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-card" });
        DomNode[] content = children;
        if (!string.IsNullOrEmpty(node.Text))
        {
            string titleId = CreateSyntheticId(node, "card-title");
            attributes.Add(new DomAttribute { Name = AriaAttributes.LabelledBy, Value = titleId });
            DomNode title = CreateLabelElement(node, HtmlElements.Header, "htmlgui-card-title", "$card-title", titleId, node.Text);
            content = PrependChild(title, children);
        }

        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Article, attributes.ToArray(), content);
    }

    internal DomNode ExpandGroupBox(UiNode node, DomNode[] children)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-group-box" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Group });
        DomNode[] content = children;
        if (!string.IsNullOrEmpty(node.Text))
        {
            string labelId = CreateSyntheticId(node, "group-box-label");
            attributes.Add(new DomAttribute { Name = AriaAttributes.LabelledBy, Value = labelId });
            DomNode label = CreateLabelElement(node, HtmlElements.Division, "htmlgui-group-box-label", "$group-box-label", labelId, node.Text);
            content = PrependChild(label, children);
        }

        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Section, attributes.ToArray(), content);
    }

    internal DomNode ExpandFieldSet(UiNode node, DomNode[] children)
    {
        DomNode[] content = children;
        if (!string.IsNullOrEmpty(node.Text))
        {
            DomNode legend = CreateLabelElement(
                node,
                HtmlElements.Legend,
                "htmlgui-field-set-legend",
                "$field-set-legend",
                CreateSyntheticId(node, "field-set-legend"),
                node.Text);
            content = PrependChild(legend, children);
        }

        return CreateElement(
            node,
            HtmlElements.FieldSet,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-field-set" } },
            content);
    }

    internal DomNode ExpandDisclosure(UiNode node, DomNode[] children)
    {
        UiDisclosure disclosure = RequireDisclosure(node);
        List<DomAttribute> attributes = new List<DomAttribute>();
        if (disclosure.IsDisabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-disclosure" });
        if (disclosure.IsExpanded)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Open, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        string summaryId = CreateSyntheticId(node, "disclosure-summary");
        DomNode summary = CreateLabelElement(
            node,
            HtmlElements.Summary,
            "htmlgui-disclosure-summary",
            "$disclosure-summary",
            summaryId,
            disclosure.Summary);
        return CreateElement(node, HtmlElements.Details, attributes.ToArray(), PrependChild(summary, children));
    }

    internal DomNode ExpandCollapsibleRegion(UiNode node, DomNode[] children)
    {
        UiDisclosure disclosure = RequireDisclosure(node);
        string toggleId = CreateSyntheticId(node, "collapsible-toggle");
        string regionId = CreateSyntheticId(node, "collapsible-content");
        List<DomAttribute> toggleAttributes = new List<DomAttribute>();
        toggleAttributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = regionId });
        toggleAttributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(disclosure.IsExpanded) });
        toggleAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-collapsible-toggle" });
        if (disclosure.IsDisabled)
        {
            toggleAttributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        toggleAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        SortAttributes(toggleAttributes);
        DomNode toggle = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$collapsible-toggle",
            Id = toggleId,
            Name = HtmlElements.Button,
            Attributes = toggleAttributes.ToArray(),
            Children = new DomNode[] { CreateTextNode(node, "$collapsible-toggle-text", "collapsible-toggle-text", disclosure.Summary) },
            Version = GetNodeVersion(node)
        };

        List<DomAttribute> regionAttributes = new List<DomAttribute>();
        regionAttributes.Add(new DomAttribute { Name = AriaAttributes.LabelledBy, Value = toggleId });
        regionAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-collapsible-content" });
        if (!disclosure.IsExpanded)
        {
            regionAttributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        regionAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Region });
        SortAttributes(regionAttributes);
        DomNode region = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$collapsible-content",
            Id = regionId,
            Name = HtmlElements.Division,
            Attributes = regionAttributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };

        return CreateElement(
            node,
            HtmlElements.Section,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-collapsible-region" } },
            new DomNode[] { toggle, region });
    }

    internal DomNode ExpandResizablePanel(UiNode node, DomNode[] children)
    {
        UiOrientation orientation = UiOrientation.Horizontal;
        if (node.Layout != null)
        {
            orientation = node.Layout.ResizeOrientation;
        }

        string token;
        switch (orientation)
        {
            case UiOrientation.Horizontal:
                token = "horizontal";
                break;
            case UiOrientation.Vertical:
                token = "vertical";
                break;
            default:
                throw new ArgumentOutOfRangeException("resizeOrientation", orientation, "The resize orientation is not supported.");
        }

        return CreateElement(
            node,
            HtmlElements.Division,
            new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-resizable-panel" },
                new DomAttribute { Name = HtmlAttributes.DataPrefix + "resize", Value = token },
                new DomAttribute { Name = HtmlAttributes.Style, Value = "overflow:auto;resize:" + token + ";" }
            },
            children);
    }

    internal UiDisclosure RequireDisclosure(UiNode node)
    {
        if (node.Disclosure == null)
        {
            string nodeId = GetNodeId(node);
            if (nodeId.Length == 0)
            {
                nodeId = "unknown";
            }

            throw new UiRenderException("A disclosure control requires a disclosure description.", nodeId, "dom");
        }

        return node.Disclosure;
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

    internal DomNode CreateLabelElement(UiNode node, string name, string className, string key, string id, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = id,
            Name = name,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = className } },
            Children = new DomNode[] { CreateTextNode(node, key + "-text", key + "-text", text) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTextNode(UiNode node, string key, string suffix, string text)
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

    internal DomNode[] PrependChild(DomNode child, DomNode[] children)
    {
        DomNode[] result = new DomNode[children.Length + 1];
        result[0] = child;
        Array.Copy(children, 0, result, 1, children.Length);
        return result;
    }

    internal string CreateSyntheticId(UiNode node, string suffix)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), suffix });
        return "ui-control-" + token.Substring(0, 20);
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

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
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
}
