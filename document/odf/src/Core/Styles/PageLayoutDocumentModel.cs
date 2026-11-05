using System;
using System.Collections.Generic;

namespace odf;

internal class PageLayoutDocumentState
{
    internal OdfNode[] OriginalLayoutNodes = [];
    internal OdfNode[] OriginalMasterNodes = [];
}

internal class PagePropertyWrapperResult
{
    internal OdfNode? WrapperNode;
    internal OdfNode? PropertyNode;
}

internal static class PageLayoutDocumentModel
{
    internal static void LoadPageLayouts(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for page-layout materialization.");
        }
        List<OdfPageLayout> layouts = new List<OdfPageLayout>();
        List<OdfMasterPage> masters = new List<OdfMasterPage>();
        List<OdfNode> layoutNodes = new List<OdfNode>();
        List<OdfNode> masterNodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            LoadLayoutSection(part, part.Styles, layouts, layoutNodes);
            LoadLayoutSection(
                part,
                part.AutomaticStyles,
                layouts,
                layoutNodes);
            LoadMasterSection(part, part.MasterStyles, masters, masterNodes);
            partIndex = partIndex + 1;
        }
        document.PageLayouts = layouts.ToArray();
        document.MasterPages = masters.ToArray();
        PageLayoutDocumentState state = new PageLayoutDocumentState();
        state.OriginalLayoutNodes = layoutNodes.ToArray();
        state.OriginalMasterNodes = masterNodes.ToArray();
        document.PageLayoutState = state;
        ResolvePageRelationships(document);
    }

    internal static void SynchronizePageLayouts(OdfDocument document)
    {
        if (document == null
            || document.PageLayouts == null
            || document.MasterPages == null)
        {
            throw new OdfDocumentException(
                "Page-layout and master-page collections are required for writing.");
        }
        PageLayoutDocumentState state = document.PageLayoutState
            ?? new PageLayoutDocumentState();
        RemoveDeletedLayouts(
            document.PageLayouts,
            state.OriginalLayoutNodes);
        RemoveDeletedMasters(
            document.MasterPages,
            state.OriginalMasterNodes);
        List<OdfNode> layoutNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.PageLayouts.Length)
        {
            OdfPageLayout layout = document.PageLayouts[index];
            if (layout == null)
            {
                throw new OdfDocumentException(
                    "The page-layout collection contains a null layout.");
            }
            SynchronizeLayout(document, layout);
            if (layout.OriginalNode != null)
            {
                layoutNodes.Add(layout.OriginalNode);
            }
            index = index + 1;
        }
        List<OdfNode> masterNodes = new List<OdfNode>();
        index = 0;
        while (index < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[index];
            if (master == null)
            {
                throw new OdfDocumentException(
                    "The master-page collection contains a null master page.");
            }
            SynchronizeMaster(document, master);
            if (master.OriginalNode != null)
            {
                masterNodes.Add(master.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalLayoutNodes = layoutNodes.ToArray();
        state.OriginalMasterNodes = masterNodes.ToArray();
        document.PageLayoutState = state;
        ResolvePageRelationships(document);
        StyleResolver.ResolveRelationships(document);
    }

    static void LoadLayoutSection(
        OdfPart part,
        OdfNode? section,
        List<OdfPageLayout> layouts,
        List<OdfNode> nodes)
    {
        if (section == null)
        {
            return;
        }
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode node = section.Children[index];
            if (IsPageLayoutNode(node))
            {
                layouts.Add(CreateLayout(part, node));
                nodes.Add(node);
            }
            index = index + 1;
        }
    }

    static void LoadMasterSection(
        OdfPart part,
        OdfNode? section,
        List<OdfMasterPage> masters,
        List<OdfNode> nodes)
    {
        if (section == null)
        {
            return;
        }
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode node = section.Children[index];
            if (IsMasterPageNode(node))
            {
                masters.Add(CreateMaster(part, node));
                nodes.Add(node);
            }
            index = index + 1;
        }
    }

    static OdfPageLayout CreateLayout(OdfPart part, OdfNode node)
    {
        OdfPageLayout result = new OdfPageLayout();
        result.Name = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "name");
        result.PageUsage = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "page-usage");
        result.IsDefault = node.Name != null
            && node.Name.LocalName == "default-page-layout";
        result.PartPath = part.Path;
        OdfNode? pageProperties = FindStyleChild(
            node,
            "page-layout-properties");
        result.PagePropertiesNode = pageProperties;
        result.OriginalPagePropertiesNode = pageProperties;
        result.PageProperties =
            StylePropertyDocumentModel.ReadPropertyAttributes(pageProperties);
        result.OriginalPagePropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.PageProperties);
        result.PagePropertyChildren =
            StylePropertyDocumentModel.PropertyChildren(pageProperties);
        result.OriginalPagePropertyChildren =
            StylePropertyDocumentModel.CopyNodes(result.PagePropertyChildren);
        OdfNode? headerStyle = FindStyleChild(node, "header-style");
        OdfNode? headerProperties = FindStyleChild(
            headerStyle,
            "header-footer-properties");
        result.OriginalHeaderStyleNode = headerStyle;
        result.HeaderPropertiesNode = headerProperties;
        result.OriginalHeaderPropertiesNode = headerProperties;
        result.HeaderProperties =
            StylePropertyDocumentModel.ReadPropertyAttributes(
                headerProperties);
        result.OriginalHeaderPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                result.HeaderProperties);
        result.HeaderPropertyChildren =
            StylePropertyDocumentModel.PropertyChildren(headerProperties);
        result.OriginalHeaderPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                result.HeaderPropertyChildren);
        OdfNode? footerStyle = FindStyleChild(node, "footer-style");
        OdfNode? footerProperties = FindStyleChild(
            footerStyle,
            "header-footer-properties");
        result.OriginalFooterStyleNode = footerStyle;
        result.FooterPropertiesNode = footerProperties;
        result.OriginalFooterPropertiesNode = footerProperties;
        result.FooterProperties =
            StylePropertyDocumentModel.ReadPropertyAttributes(
                footerProperties);
        result.OriginalFooterPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                result.FooterProperties);
        result.FooterPropertyChildren =
            StylePropertyDocumentModel.PropertyChildren(footerProperties);
        result.OriginalFooterPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                result.FooterPropertyChildren);
        RefreshLayoutConvenienceFields(result);
        result.Node = node;
        result.OriginalNode = node;
        SnapshotLayout(result);
        return result;
    }

    static OdfMasterPage CreateMaster(OdfPart part, OdfNode node)
    {
        OdfMasterPage result = new OdfMasterPage();
        result.Name = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "name");
        result.DisplayName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "display-name");
        result.PageLayoutName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "page-layout-name");
        result.DrawingStyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Drawing,
            "style-name");
        result.NextStyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "next-style-name");
        result.PartPath = part.Path;
        result.Header = FindStyleChild(node, "header");
        result.HeaderLeft = FindStyleChild(node, "header-left");
        result.HeaderFirst = FindStyleChild(node, "header-first");
        result.Footer = FindStyleChild(node, "footer");
        result.FooterLeft = FindStyleChild(node, "footer-left");
        result.FooterFirst = FindStyleChild(node, "footer-first");
        result.Content = ReadMasterContent(node);
        result.Node = node;
        result.OriginalNode = node;
        SnapshotMaster(result);
        return result;
    }

    static OdfNode[] ReadMasterContent(OdfNode node)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (!IsHeaderFooterVariant(child))
            {
                result.Add(child);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void SynchronizeLayout(
        OdfDocument document,
        OdfPageLayout layout)
    {
        OdfNode section = EnsureLayoutSection(document, layout);
        OdfNode? node = layout.OriginalNode;
        bool isNew = node == null;
        if (node == null)
        {
            node = CreateStyleElement(
                layout.IsDefault
                    ? "default-page-layout"
                    : "page-layout",
                section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
            layout.OriginalNode = node;
        }
        else if (node.Parent != section)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
            StyleDocumentModel.AppendNode(section, node);
        }
        ApplyLayoutFields(layout, node, isNew);
        ApplyLayoutConvenienceChanges(layout);
        layout.PagePropertyChildren = ApplyPageChildChange(
            layout.PagePropertyChildren,
            layout.BackgroundImageNode,
            layout.OriginalBackgroundImageNode,
            "background-image");
        layout.PagePropertyChildren = ApplyPageChildChange(
            layout.PagePropertyChildren,
            layout.ColumnsNode,
            layout.OriginalColumnsNode,
            "columns");
        layout.PagePropertyChildren = ApplyPageChildChange(
            layout.PagePropertyChildren,
            layout.FootnoteSeparatorNode,
            layout.OriginalFootnoteSeparatorNode,
            "footnote-sep");
        OdfNode? pageProperties =
            StylePropertyDocumentModel.SynchronizePropertySet(
                node,
                "page-layout-properties",
                layout.PageProperties,
                layout.PagePropertyChildren,
                layout.OriginalPagePropertiesNode,
                layout.OriginalPagePropertyAttributes,
                layout.OriginalPagePropertyChildren);
        layout.PagePropertiesNode = pageProperties;
        layout.OriginalPagePropertiesNode = pageProperties;
        layout.OriginalPagePropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                layout.PageProperties);
        layout.OriginalPagePropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                layout.PagePropertyChildren);
        PagePropertyWrapperResult header = SynchronizePropertyWrapper(
            node,
            "header-style",
            layout.HeaderProperties,
            layout.HeaderPropertyChildren,
            layout.OriginalHeaderStyleNode,
            layout.OriginalHeaderPropertiesNode,
            layout.OriginalHeaderPropertyAttributes,
            layout.OriginalHeaderPropertyChildren);
        layout.OriginalHeaderStyleNode = header.WrapperNode;
        layout.HeaderPropertiesNode = header.PropertyNode;
        layout.OriginalHeaderPropertiesNode = header.PropertyNode;
        layout.OriginalHeaderPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                layout.HeaderProperties);
        layout.OriginalHeaderPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                layout.HeaderPropertyChildren);
        PagePropertyWrapperResult footer = SynchronizePropertyWrapper(
            node,
            "footer-style",
            layout.FooterProperties,
            layout.FooterPropertyChildren,
            layout.OriginalFooterStyleNode,
            layout.OriginalFooterPropertiesNode,
            layout.OriginalFooterPropertyAttributes,
            layout.OriginalFooterPropertyChildren);
        layout.OriginalFooterStyleNode = footer.WrapperNode;
        layout.FooterPropertiesNode = footer.PropertyNode;
        layout.OriginalFooterPropertiesNode = footer.PropertyNode;
        layout.OriginalFooterPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(
                layout.FooterProperties);
        layout.OriginalFooterPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                layout.FooterPropertyChildren);
        layout.Node = node;
        layout.PartPath = section.Location.PartPath;
        RefreshLayoutConvenienceFields(layout);
        SnapshotLayout(layout);
    }

    static void SynchronizeMaster(
        OdfDocument document,
        OdfMasterPage master)
    {
        OdfNode section = EnsureMasterSection(document);
        OdfNode? node = master.OriginalNode;
        bool isNew = node == null;
        if (node == null)
        {
            node = CreateStyleElement(
                "master-page",
                section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
            master.OriginalNode = node;
        }
        else if (node.Parent != section)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
            StyleDocumentModel.AppendNode(section, node);
        }
        ApplyMasterFields(master, node, isNew);
        if (MasterChildrenChanged(master))
        {
            SynchronizeMasterChildren(master, node);
        }
        master.Node = node;
        master.PartPath = section.Location.PartPath;
        SnapshotMaster(master);
    }

    static OdfNode EnsureLayoutSection(
        OdfDocument document,
        OdfPageLayout layout)
    {
        bool automatic = !layout.IsDefault;
        string partPath = SelectLayoutPartPath(document, layout);
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        OdfNode? section = automatic
            ? part.AutomaticStyles
            : part.Styles;
        if (section == null)
        {
            section = StyleDocumentModel.CreateStyleSection(
                document,
                part,
                automatic);
        }
        return section;
    }

    static string SelectLayoutPartPath(
        OdfDocument document,
        OdfPageLayout layout)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (!layout.IsDefault
            && (layout.PartPath == OdfPartNames.Content
                || layout.PartPath == OdfPartNames.Styles))
        {
            return layout.PartPath;
        }
        if (!layout.IsDefault
            && (layout.OriginalPartPath == OdfPartNames.Content
                || layout.OriginalPartPath == OdfPartNames.Styles))
        {
            return layout.OriginalPartPath;
        }
        return OdfPartNames.Styles;
    }

    static OdfNode EnsureMasterSection(OdfDocument document)
    {
        string partPath = document.PackageKind == OdfPackageKind.Flat
            ? ""
            : OdfPartNames.Styles;
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        if (part.MasterStyles != null)
        {
            return part.MasterStyles;
        }
        if (part.Root == null)
        {
            throw new OdfDocumentException(
                "A master-style section requires a materialized root.");
        }
        OdfNode section = CreateOfficeElement(
            "master-styles",
            part.Path);
        section.NamespaceDeclarations =
        [
            NamespaceDeclaration(
                OdfNamespaces.OfficePrefix,
                OdfNamespaces.Office,
                part.Path),
            NamespaceDeclaration(
                OdfNamespaces.StylePrefix,
                OdfNamespaces.Style,
                part.Path),
            NamespaceDeclaration(
                OdfNamespaces.DrawingPrefix,
                OdfNamespaces.Drawing,
                part.Path),
            NamespaceDeclaration(
                OdfNamespaces.TextPrefix,
                OdfNamespaces.Text,
                part.Path)
        ];
        StyleDocumentModel.InsertSection(
            part.Root,
            section,
            StyleDocumentModel.SectionRank("master-styles"));
        part.MasterStyles = section;
        StyleDocumentModel.RefreshPartMapping(document, part);
        return section;
    }

    static void ApplyLayoutFields(
        OdfPageLayout layout,
        OdfNode node,
        bool isNew)
    {
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        if (isNew || layout.IsDefault != layout.OriginalIsDefault)
        {
            node.Name.NamespaceName = OdfNamespaces.Style;
            node.Name.LocalName = layout.IsDefault
                ? "default-page-layout"
                : "page-layout";
            node.Name.Prefix = OdfNamespaces.StylePrefix;
        }
        WriteChangedAttribute(
            node,
            OdfNamespaces.Style,
            "name",
            OdfNamespaces.StylePrefix,
            layout.IsDefault ? "" : layout.Name,
            layout.OriginalName,
            isNew || layout.IsDefault != layout.OriginalIsDefault);
        WriteChangedAttribute(
            node,
            OdfNamespaces.Style,
            "page-usage",
            OdfNamespaces.StylePrefix,
            layout.PageUsage,
            layout.OriginalPageUsage,
            isNew);
    }

    static void ApplyMasterFields(
        OdfMasterPage master,
        OdfNode node,
        bool isNew)
    {
        WriteChangedAttribute(node, OdfNamespaces.Style, "name", OdfNamespaces.StylePrefix, master.Name, master.OriginalName, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "display-name", OdfNamespaces.StylePrefix, master.DisplayName, master.OriginalDisplayName, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "page-layout-name", OdfNamespaces.StylePrefix, master.PageLayoutName, master.OriginalPageLayoutName, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Drawing, "style-name", OdfNamespaces.DrawingPrefix, master.DrawingStyleName, master.OriginalDrawingStyleName, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "next-style-name", OdfNamespaces.StylePrefix, master.NextStyleName, master.OriginalNextStyleName, isNew);
    }

    static void WriteChangedAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string originalValue,
        bool isNew)
    {
        if (!isNew && value == originalValue)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static void ApplyLayoutConvenienceChanges(OdfPageLayout layout)
    {
        layout.PageProperties = ApplyConvenienceProperty(
            layout.PageProperties,
            "num-format",
            layout.NumberFormat,
            layout.OriginalNumberFormat);
        layout.PageProperties = ApplyConvenienceProperty(
            layout.PageProperties,
            "num-prefix",
            layout.NumberPrefix,
            layout.OriginalNumberPrefix);
        layout.PageProperties = ApplyConvenienceProperty(
            layout.PageProperties,
            "num-suffix",
            layout.NumberSuffix,
            layout.OriginalNumberSuffix);
        layout.PageProperties = ApplyConvenienceProperty(
            layout.PageProperties,
            "num-letter-sync",
            layout.NumberLetterSync,
            layout.OriginalNumberLetterSync);
        layout.PageProperties = ApplyConvenienceProperty(
            layout.PageProperties,
            "first-page-number",
            layout.FirstPageNumber,
            layout.OriginalFirstPageNumber);
        layout.PageProperties = ApplyConvenienceProperty(
            layout.PageProperties,
            "print-orientation",
            layout.PrintOrientation,
            layout.OriginalPrintOrientation);
    }

    static OdfStyleProperty[] ApplyConvenienceProperty(
        OdfStyleProperty[] properties,
        string localName,
        string value,
        string originalValue)
    {
        if (properties == null)
        {
            throw new OdfDocumentException(
                "A page property collection is required for writing.");
        }
        if (value == originalValue)
        {
            return properties;
        }
        List<OdfStyleProperty> result = new List<OdfStyleProperty>();
        OdfStyleProperty? existing = null;
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property == null || property.Name == null)
            {
                throw new OdfDocumentException(
                    "A page property and qualified name are required for writing.");
            }
            if (property.Name != null
                && property.Name.NamespaceName == OdfNamespaces.Style
                && property.Name.LocalName == localName)
            {
                existing = property;
            }
            else
            {
                result.Add(property);
            }
            index = index + 1;
        }
        if (value.Length > 0)
        {
            if (existing == null)
            {
                existing = new OdfStyleProperty();
                existing.Name.NamespaceName = OdfNamespaces.Style;
                existing.Name.LocalName = localName;
                existing.Name.Prefix = OdfNamespaces.StylePrefix;
            }
            existing.LexicalValue = value;
            result.Add(existing);
        }
        return result.ToArray();
    }

    static OdfNode[] ApplyPageChildChange(
        OdfNode[] children,
        OdfNode? current,
        OdfNode? original,
        string localName)
    {
        if (children == null)
        {
            throw new OdfDocumentException(
                "A page property child collection is required for writing.");
        }
        if (current == original)
        {
            return children;
        }
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (child != original && !IsStyleNode(child, localName))
            {
                result.Add(child);
            }
            index = index + 1;
        }
        if (current != null)
        {
            NormalizeStyleElement(current, localName);
            InsertPagePropertyChild(result, current, localName);
        }
        return result.ToArray();
    }

    static void InsertPagePropertyChild(
        List<OdfNode> children,
        OdfNode child,
        string localName)
    {
        int rank = PagePropertyChildRank(localName);
        int insertion = children.Count;
        int index = 0;
        while (index < children.Count)
        {
            OdfNode existing = children[index];
            int existingRank = existing.Name == null
                ? 0
                : PagePropertyChildRank(existing.Name.LocalName);
            if (existingRank > rank)
            {
                insertion = index;
                index = children.Count;
            }
            else
            {
                index = index + 1;
            }
        }
        children.Insert(insertion, child);
    }

    static int PagePropertyChildRank(string localName)
    {
        if (localName == "background-image")
        {
            return 10;
        }
        if (localName == "columns")
        {
            return 20;
        }
        if (localName == "footnote-sep")
        {
            return 30;
        }
        return 0;
    }

    static PagePropertyWrapperResult SynchronizePropertyWrapper(
        OdfNode layoutNode,
        string wrapperName,
        OdfStyleProperty[] properties,
        OdfNode[] children,
        OdfNode? originalWrapper,
        OdfNode? originalProperties,
        OdfAttribute[] originalAttributes,
        OdfNode[] originalChildren)
    {
        if (properties == null || children == null)
        {
            throw new OdfDocumentException(
                "Header and footer property collections are required for writing.");
        }
        bool needsWrapper = properties.Length > 0 || children.Length > 0;
        OdfNode? wrapper = originalWrapper;
        if (wrapper == null && needsWrapper)
        {
            wrapper = CreateStyleElement(
                wrapperName,
                layoutNode.Location.PartPath);
            InsertLayoutWrapper(layoutNode, wrapper, wrapperName);
        }
        if (wrapper == null)
        {
            return new PagePropertyWrapperResult();
        }
        OdfNode? propertyNode =
            StylePropertyDocumentModel.SynchronizePropertySet(
                wrapper,
                "header-footer-properties",
                properties,
                children,
                originalProperties,
                originalAttributes,
                originalChildren);
        if (propertyNode == null
            && wrapper.Attributes.Length == 0
            && wrapper.Children.Length == 0
            && needsWrapper == false)
        {
            StyleDocumentModel.RemoveNodeFromParent(wrapper);
            wrapper = null;
        }
        PagePropertyWrapperResult result = new PagePropertyWrapperResult();
        result.WrapperNode = wrapper;
        result.PropertyNode = propertyNode;
        return result;
    }

    static void InsertLayoutWrapper(
        OdfNode layoutNode,
        OdfNode wrapper,
        string wrapperName)
    {
        int rank = wrapperName == "header-style" ? 20 : 30;
        int insertion = layoutNode.Children.Length;
        int index = 0;
        while (index < layoutNode.Children.Length)
        {
            OdfNode child = layoutNode.Children[index];
            int childRank = LayoutChildRank(child);
            if (childRank > rank)
            {
                insertion = index;
                index = layoutNode.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[layoutNode.Children.Length + 1];
        Array.Copy(layoutNode.Children, 0, children, 0, insertion);
        children[insertion] = wrapper;
        Array.Copy(
            layoutNode.Children,
            insertion,
            children,
            insertion + 1,
            layoutNode.Children.Length - insertion);
        layoutNode.Children = children;
        wrapper.Parent = layoutNode;
        layoutNode.WasEmptyElement = false;
    }

    static int LayoutChildRank(OdfNode node)
    {
        if (IsStyleNode(node, "page-layout-properties"))
        {
            return 10;
        }
        if (IsStyleNode(node, "header-style"))
        {
            return 20;
        }
        if (IsStyleNode(node, "footer-style"))
        {
            return 30;
        }
        return 0;
    }

    static bool MasterChildrenChanged(OdfMasterPage master)
    {
        return master.Header != master.OriginalHeader
            || master.HeaderLeft != master.OriginalHeaderLeft
            || master.HeaderFirst != master.OriginalHeaderFirst
            || master.Footer != master.OriginalFooter
            || master.FooterLeft != master.OriginalFooterLeft
            || master.FooterFirst != master.OriginalFooterFirst
            || !NodesMatch(master.Content, master.OriginalContent);
    }

    static void SynchronizeMasterChildren(
        OdfMasterPage master,
        OdfNode node)
    {
        if (master.Content == null)
        {
            throw new OdfDocumentException(
                "A master-page content collection is required for writing.");
        }
        List<OdfNode> children = new List<OdfNode>();
        AddMasterVariant(children, master.Header, "header");
        AddMasterVariant(children, master.HeaderLeft, "header-left");
        AddMasterVariant(children, master.HeaderFirst, "header-first");
        AddMasterVariant(children, master.Footer, "footer");
        AddMasterVariant(children, master.FooterLeft, "footer-left");
        AddMasterVariant(children, master.FooterFirst, "footer-first");
        int index = 0;
        while (index < master.Content.Length)
        {
            OdfNode child = master.Content[index];
            if (child == null)
            {
                throw new OdfDocumentException(
                    "Master-page content cannot contain a null node.");
            }
            if (children.Contains(child))
            {
                throw new OdfDocumentException(
                    "A master-page child cannot occur more than once.");
            }
            children.Add(child);
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            OdfNode old = node.Children[index];
            if (!children.Contains(old))
            {
                old.Parent = null;
            }
            index = index + 1;
        }
        index = 0;
        while (index < children.Count)
        {
            OdfNode child = children[index];
            if (child.Parent != null && child.Parent != node)
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            child.Parent = node;
            if (child.Location.PartPath.Length == 0)
            {
                child.Location.PartPath = node.Location.PartPath;
            }
            index = index + 1;
        }
        node.Children = children.ToArray();
        node.WasEmptyElement = children.Count == 0;
    }

    static void AddMasterVariant(
        List<OdfNode> children,
        OdfNode? node,
        string localName)
    {
        if (node == null)
        {
            return;
        }
        if (children.Contains(node))
        {
            throw new OdfDocumentException(
                "A header or footer variant cannot occur more than once.");
        }
        NormalizeStyleElement(node, localName);
        children.Add(node);
    }

    static void RefreshLayoutConvenienceFields(OdfPageLayout layout)
    {
        layout.NumberFormat = ReadPropertyValue(
            layout.PageProperties,
            "num-format");
        layout.NumberPrefix = ReadPropertyValue(
            layout.PageProperties,
            "num-prefix");
        layout.NumberSuffix = ReadPropertyValue(
            layout.PageProperties,
            "num-suffix");
        layout.NumberLetterSync = ReadPropertyValue(
            layout.PageProperties,
            "num-letter-sync");
        layout.FirstPageNumber = ReadPropertyValue(
            layout.PageProperties,
            "first-page-number");
        layout.PrintOrientation = ReadPropertyValue(
            layout.PageProperties,
            "print-orientation");
        layout.BackgroundImageNode = FindStyleChildInArray(
            layout.PagePropertyChildren,
            "background-image");
        layout.ColumnsNode = FindStyleChildInArray(
            layout.PagePropertyChildren,
            "columns");
        layout.FootnoteSeparatorNode = FindStyleChildInArray(
            layout.PagePropertyChildren,
            "footnote-sep");
        layout.OriginalBackgroundImageNode = layout.BackgroundImageNode;
        layout.OriginalColumnsNode = layout.ColumnsNode;
        layout.OriginalFootnoteSeparatorNode =
            layout.FootnoteSeparatorNode;
    }

    static string ReadPropertyValue(
        OdfStyleProperty[] properties,
        string localName)
    {
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property.Name.NamespaceName == OdfNamespaces.Style
                && property.Name.LocalName == localName)
            {
                return property.LexicalValue;
            }
            index = index + 1;
        }
        return "";
    }

    static void ResolvePageRelationships(OdfDocument document)
    {
        int index = 0;
        while (index < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[index];
            master.PageLayout = FindPageLayout(
                document.PageLayouts,
                master.PageLayoutName);
            master.NextMasterPage = FindMasterPage(
                document.MasterPages,
                master.NextStyleName,
                master);
            index = index + 1;
        }
    }

    static OdfPageLayout? FindPageLayout(
        OdfPageLayout[] layouts,
        string name)
    {
        int index = 0;
        while (index < layouts.Length)
        {
            OdfPageLayout layout = layouts[index];
            if (!layout.IsDefault && layout.Name == name)
            {
                return layout;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfMasterPage? FindMasterPage(
        OdfMasterPage[] masters,
        string name,
        OdfMasterPage target)
    {
        if (name.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < masters.Length)
        {
            OdfMasterPage master = masters[index];
            if (master != target && master.Name == name)
            {
                return master;
            }
            index = index + 1;
        }
        return null;
    }

    static void RemoveDeletedLayouts(
        OdfPageLayout[] layouts,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!LayoutRetainsNode(layouts, node) && node.Parent != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool LayoutRetainsNode(OdfPageLayout[] layouts, OdfNode node)
    {
        int index = 0;
        while (index < layouts.Length)
        {
            OdfPageLayout layout = layouts[index];
            if (layout != null && layout.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedMasters(
        OdfMasterPage[] masters,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!MasterRetainsNode(masters, node) && node.Parent != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool MasterRetainsNode(OdfMasterPage[] masters, OdfNode node)
    {
        int index = 0;
        while (index < masters.Length)
        {
            OdfMasterPage master = masters[index];
            if (master != null && master.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode? FindStyleChild(OdfNode? parent, string localName)
    {
        if (parent == null)
        {
            return null;
        }
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsStyleNode(child, localName))
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindStyleChildInArray(
        OdfNode[] children,
        string localName)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (IsStyleNode(children[index], localName))
            {
                return children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsPageLayoutNode(OdfNode node)
    {
        return IsStyleNode(node, "page-layout")
            || IsStyleNode(node, "default-page-layout");
    }

    static bool IsMasterPageNode(OdfNode node)
    {
        return IsStyleNode(node, "master-page");
    }

    static bool IsHeaderFooterVariant(OdfNode node)
    {
        return IsStyleNode(node, "header")
            || IsStyleNode(node, "header-left")
            || IsStyleNode(node, "header-first")
            || IsStyleNode(node, "footer")
            || IsStyleNode(node, "footer-left")
            || IsStyleNode(node, "footer-first");
    }

    static bool IsStyleNode(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName == localName;
    }

    static void NormalizeStyleElement(OdfNode node, string localName)
    {
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Kind = OdfNodeKind.Element;
        node.Name.NamespaceName = OdfNamespaces.Style;
        node.Name.LocalName = localName;
        node.Name.Prefix = OdfNamespaces.StylePrefix;
    }

    static OdfNode CreateStyleElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Style,
            localName,
            OdfNamespaces.StylePrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNode CreateOfficeElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Office,
            localName,
            OdfNamespaces.OfficePrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNamespaceDeclaration NamespaceDeclaration(
        string prefix,
        string namespaceName,
        string partPath)
    {
        OdfNamespaceDeclaration result = new OdfNamespaceDeclaration();
        result.Prefix = prefix;
        result.NamespaceName = namespaceName;
        result.Location.PartPath = partPath;
        return result;
    }

    static bool NodesMatch(OdfNode[] first, OdfNode[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void SnapshotLayout(OdfPageLayout layout)
    {
        layout.OriginalName = layout.Name;
        layout.OriginalPageUsage = layout.PageUsage;
        layout.OriginalPartPath = layout.PartPath;
        layout.OriginalNumberFormat = layout.NumberFormat;
        layout.OriginalNumberPrefix = layout.NumberPrefix;
        layout.OriginalNumberSuffix = layout.NumberSuffix;
        layout.OriginalNumberLetterSync = layout.NumberLetterSync;
        layout.OriginalFirstPageNumber = layout.FirstPageNumber;
        layout.OriginalPrintOrientation = layout.PrintOrientation;
        layout.OriginalIsDefault = layout.IsDefault;
    }

    static void SnapshotMaster(OdfMasterPage master)
    {
        master.OriginalName = master.Name;
        master.OriginalDisplayName = master.DisplayName;
        master.OriginalPageLayoutName = master.PageLayoutName;
        master.OriginalDrawingStyleName = master.DrawingStyleName;
        master.OriginalNextStyleName = master.NextStyleName;
        master.OriginalPartPath = master.PartPath;
        master.OriginalHeader = master.Header;
        master.OriginalHeaderLeft = master.HeaderLeft;
        master.OriginalHeaderFirst = master.HeaderFirst;
        master.OriginalFooter = master.Footer;
        master.OriginalFooterLeft = master.FooterLeft;
        master.OriginalFooterFirst = master.FooterFirst;
        master.OriginalContent = StylePropertyDocumentModel.CopyNodes(
            master.Content);
    }
}
