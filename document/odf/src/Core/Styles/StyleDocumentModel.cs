using System;
using System.Collections.Generic;

namespace odf;

internal class StyleDocumentState
{
    internal OdfNode[] OriginalNodes = [];
}

internal static class StyleDocumentModel
{
    internal static void LoadStyles(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for style materialization.");
        }
        List<OdfStyle> styles = new List<OdfStyle>();
        List<OdfNode> nodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            LoadStyleSection(part, part.Styles, false, styles, nodes);
            LoadStyleSection(
                part,
                part.AutomaticStyles,
                true,
                styles,
                nodes);
            partIndex = partIndex + 1;
        }
        document.Styles = styles.ToArray();
        StyleDocumentState state = new StyleDocumentState();
        state.OriginalNodes = nodes.ToArray();
        document.StyleState = state;
        StyleResolver.ResolveRelationships(document);
    }

    internal static void SynchronizeStyles(OdfDocument document)
    {
        if (document == null || document.Styles == null)
        {
            throw new OdfDocumentException(
                "A document and style collection are required for style writing.");
        }
        StyleDocumentState state = document.StyleState
            ?? new StyleDocumentState();
        RemoveDeletedStyles(document.Styles, state.OriginalNodes);
        List<OdfNode> currentNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style == null)
            {
                throw new OdfDocumentException(
                    "The document style collection contains a null style.");
            }
            SynchronizeStyle(document, style);
            if (style.OriginalNode != null)
            {
                currentNodes.Add(style.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalNodes = currentNodes.ToArray();
        document.StyleState = state;
        StyleResolver.ResolveRelationships(document);
    }

    static void LoadStyleSection(
        OdfPart part,
        OdfNode? section,
        bool automatic,
        List<OdfStyle> styles,
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
            if (IsStyleDefinition(node))
            {
                OdfStyle style = CreateStyle(part, node, automatic);
                styles.Add(style);
                nodes.Add(node);
            }
            index = index + 1;
        }
    }

    static OdfStyle CreateStyle(
        OdfPart part,
        OdfNode node,
        bool automatic)
    {
        OdfStyle result = new OdfStyle();
        result.Name = ReadAttribute(node, OdfNamespaces.Style, "name");
        result.DisplayName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "display-name");
        result.FamilyName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "family");
        result.Family = ParseFamily(result.FamilyName);
        result.PartPath = part.Path;
        result.ParentStyleName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "parent-style-name");
        result.NextStyleName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "next-style-name");
        result.ListStyleName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "list-style-name");
        result.MasterPageName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "master-page-name");
        result.DataStyleName = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "data-style-name");
        result.IsDefault = node.Name != null
            && node.Name.LocalName == "default-style";
        result.IsAutomatic = automatic;
        result.IsCommon = !automatic;
        result.IsNamed = !result.IsDefault && result.Name.Length > 0;
        result.Node = node;
        result.OriginalNode = node;
        StylePropertyDocumentModel.LoadStyleProperties(result, node);
        SnapshotStyle(result);
        return result;
    }

    static void RemoveDeletedStyles(
        OdfStyle[] styles,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!StyleRetainsNode(styles, node) && node.Parent != null)
            {
                RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool StyleRetainsNode(OdfStyle[] styles, OdfNode node)
    {
        int index = 0;
        while (index < styles.Length)
        {
            OdfStyle style = styles[index];
            if (style != null && style.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void SynchronizeStyle(OdfDocument document, OdfStyle style)
    {
        OdfNode? node = style.OriginalNode;
        bool isNew = node == null;
        OdfNode section = EnsureTargetSection(document, style);
        if (node == null)
        {
            node = CreateStyleNode(style, section.Location.PartPath);
            AppendNode(section, node);
            style.OriginalNode = node;
        }
        else if (node.Parent != section)
        {
            if (node.Parent != null)
            {
                RemoveNodeFromParent(node);
            }
            AppendNode(section, node);
        }
        ApplyStyleFields(style, node, isNew);
        StylePropertyDocumentModel.SynchronizeStyleProperties(style, node);
        style.Node = node;
        style.PartPath = section.Location.PartPath;
        style.IsAutomatic = IsAutomaticSection(section);
        style.IsCommon = !style.IsAutomatic;
        style.IsNamed = !style.IsDefault && style.Name.Length > 0;
        SnapshotStyle(style);
    }

    static OdfNode EnsureTargetSection(
        OdfDocument document,
        OdfStyle style)
    {
        bool automatic = style.IsAutomatic;
        string partPath = SelectTargetPartPath(document, style, automatic);
        OdfPart? part = FindPart(document, partPath);
        if (part == null)
        {
            part = CreateStylesPart(document);
        }
        OdfNode? section = automatic
            ? part.AutomaticStyles
            : part.Styles;
        if (section == null)
        {
            section = CreateStyleSection(document, part, automatic);
        }
        return section;
    }

    static string SelectTargetPartPath(
        OdfDocument document,
        OdfStyle style,
        bool automatic)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (!automatic)
        {
            return OdfPartNames.Styles;
        }
        if (style.PartPath == OdfPartNames.Content
            || style.PartPath == OdfPartNames.Styles)
        {
            return style.PartPath;
        }
        if (style.OriginalPartPath == OdfPartNames.Content
            || style.OriginalPartPath == OdfPartNames.Styles)
        {
            return style.OriginalPartPath;
        }
        return OdfPartNames.Content;
    }

    internal static OdfPart? FindPart(OdfDocument document, string partPath)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Path == partPath)
            {
                return part;
            }
            index = index + 1;
        }
        return null;
    }

    internal static OdfPart CreateStylesPart(OdfDocument document)
    {
        if (document.PackageKind != OdfPackageKind.Package)
        {
            throw new OdfDocumentException(
                "A common style requires a flat root or a package styles part.");
        }
        OdfNode root = new OdfNode();
        root.Kind = OdfNodeKind.Element;
        root.Name = QualifiedName(
            OdfNamespaces.Office,
            "document-styles",
            OdfNamespaces.OfficePrefix);
        root.NamespaceDeclarations =
        [
            NamespaceDeclaration(
                OdfNamespaces.OfficePrefix,
                OdfNamespaces.Office,
                OdfPartNames.Styles),
            NamespaceDeclaration(
                OdfNamespaces.StylePrefix,
                OdfNamespaces.Style,
                OdfPartNames.Styles)
        ];
        root.Attributes =
        [
            Attribute(
                OdfNamespaces.Office,
                "version",
                OdfNamespaces.OfficePrefix,
                VersionText(document),
                OdfPartNames.Styles)
        ];
        root.Location.PartPath = OdfPartNames.Styles;
        OdfPart part = new OdfPart();
        part.Path = OdfPartNames.Styles;
        part.MediaType = OdfMediaTypes.Xml;
        part.Nodes = [root];
        part.Root = root;
        part.RootName = "document-styles";
        part.VersionInfo = OdfVersionRules.ParseVersion(VersionText(document));
        AppendPart(document, part);
        return part;
    }

    static void AppendPart(OdfDocument document, OdfPart part)
    {
        OdfPart[] parts = new OdfPart[document.Parts.Length + 1];
        Array.Copy(document.Parts, parts, document.Parts.Length);
        parts[parts.Length - 1] = part;
        document.Parts = parts;
        OdfNode[] nodes = new OdfNode[document.Nodes.Length + 1];
        Array.Copy(document.Nodes, nodes, document.Nodes.Length);
        nodes[nodes.Length - 1] = part.Root ?? new OdfNode();
        document.Nodes = nodes;
    }

    internal static OdfNode CreateStyleSection(
        OdfDocument document,
        OdfPart part,
        bool automatic)
    {
        if (part.Root == null)
        {
            throw new OdfDocumentException(
                "A style section requires a materialized XML part root.");
        }
        string localName = automatic ? "automatic-styles" : "styles";
        OdfNode section = new OdfNode();
        section.Kind = OdfNodeKind.Element;
        section.Name = QualifiedName(
            OdfNamespaces.Office,
            localName,
            OdfNamespaces.OfficePrefix);
        section.Location.PartPath = part.Path;
        section.NamespaceDeclarations =
        [
            NamespaceDeclaration(
                OdfNamespaces.OfficePrefix,
                OdfNamespaces.Office,
                part.Path),
            NamespaceDeclaration(
                OdfNamespaces.StylePrefix,
                OdfNamespaces.Style,
                part.Path)
        ];
        InsertSection(part.Root, section, SectionRank(localName));
        if (automatic)
        {
            part.AutomaticStyles = section;
        }
        else
        {
            part.Styles = section;
        }
        RefreshPartMapping(document, part);
        return section;
    }

    internal static void InsertSection(
        OdfNode root,
        OdfNode section,
        int rank)
    {
        int insertion = root.Children.Length;
        int index = 0;
        while (index < root.Children.Length)
        {
            OdfNode child = root.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Office
                && SectionRank(child.Name.LocalName) > rank)
            {
                insertion = index;
                index = root.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[root.Children.Length + 1];
        Array.Copy(root.Children, 0, children, 0, insertion);
        children[insertion] = section;
        Array.Copy(
            root.Children,
            insertion,
            children,
            insertion + 1,
            root.Children.Length - insertion);
        root.Children = children;
        section.Parent = root;
        root.WasEmptyElement = false;
    }

    internal static int SectionRank(string localName)
    {
        if (localName == "meta")
        {
            return 1;
        }
        if (localName == "settings")
        {
            return 2;
        }
        if (localName == "scripts")
        {
            return 3;
        }
        if (localName == "font-face-decls")
        {
            return 4;
        }
        if (localName == "styles")
        {
            return 5;
        }
        if (localName == "automatic-styles")
        {
            return 6;
        }
        if (localName == "master-styles")
        {
            return 7;
        }
        if (localName == "body")
        {
            return 8;
        }
        return 0;
    }

    internal static void RefreshPartMapping(OdfDocument document, OdfPart part)
    {
        if (part.Root == null || part.Nodes.Length == 0)
        {
            return;
        }
        string mappingPath = part.IsFlatDocument ? "" : part.Path;
        OdfPartRootMap map = OdfPartRootRules.MapRoot(
            part.Nodes,
            mappingPath);
        part.Root = map.Root;
        part.RootName = map.RootName;
        part.Sections = map.Sections;
        part.PassthroughNodes = map.UnmappedSections;
        part.Scripts = map.Scripts;
        part.FontFaceDeclarations = map.FontFaceDeclarations;
        part.Styles = map.Styles;
        part.AutomaticStyles = map.AutomaticStyles;
        part.MasterStyles = map.MasterStyles;
        part.Metadata = map.Metadata;
        part.Settings = map.Settings;
        part.Body = map.Body;
        part.RootMap = map;
        if (document.PartTrees == null)
        {
            return;
        }
        if (part.IsFlatDocument)
        {
            document.PartTrees.FlatNodes = part.Nodes;
            document.PartTrees.FlatRoot = map;
        }
        else if (part.Path == OdfPartNames.Content)
        {
            document.PartTrees.ContentNodes = part.Nodes;
            document.PartTrees.ContentRoot = map;
        }
        else if (part.Path == OdfPartNames.Styles)
        {
            document.PartTrees.StylesNodes = part.Nodes;
            document.PartTrees.StylesRoot = map;
        }
        else if (part.Path == OdfPartNames.Settings)
        {
            document.PartTrees.SettingsNodes = part.Nodes;
            document.PartTrees.SettingsRoot = map;
        }
    }

    static OdfNode CreateStyleNode(OdfStyle style, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = QualifiedName(
            OdfNamespaces.Style,
            style.IsDefault ? "default-style" : "style",
            OdfNamespaces.StylePrefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void ApplyStyleFields(
        OdfStyle style,
        OdfNode node,
        bool isNew)
    {
        if (node.Name == null)
        {
            node.Name = QualifiedName(
                OdfNamespaces.Style,
                style.IsDefault ? "default-style" : "style",
                OdfNamespaces.StylePrefix);
        }
        if (isNew || style.IsDefault != style.OriginalIsDefault)
        {
            node.Name.NamespaceName = OdfNamespaces.Style;
            node.Name.LocalName = style.IsDefault
                ? "default-style"
                : "style";
            node.Name.Prefix = OdfNamespaces.StylePrefix;
        }
        if (isNew || style.Name != style.OriginalName
            || style.IsDefault != style.OriginalIsDefault)
        {
            if (style.IsDefault || style.Name.Length == 0)
            {
                RemoveAttribute(node, OdfNamespaces.Style, "name");
            }
            else
            {
                WriteAttribute(
                    node,
                    OdfNamespaces.Style,
                    "name",
                    OdfNamespaces.StylePrefix,
                    style.Name);
            }
        }
        if (isNew || style.DisplayName != style.OriginalDisplayName)
        {
            if (style.DisplayName.Length == 0)
            {
                RemoveAttribute(
                    node,
                    OdfNamespaces.Style,
                    "display-name");
            }
            else
            {
                WriteAttribute(
                    node,
                    OdfNamespaces.Style,
                    "display-name",
                    OdfNamespaces.StylePrefix,
                    style.DisplayName);
            }
        }
        string familyName = style.FamilyName;
        if (style.Family != style.OriginalFamily)
        {
            familyName = FormatFamily(style.Family);
            style.FamilyName = familyName;
        }
        if (isNew
            || style.Family != style.OriginalFamily
            || familyName != style.OriginalFamilyName)
        {
            if (familyName.Length == 0)
            {
                RemoveAttribute(node, OdfNamespaces.Style, "family");
            }
            else
            {
                WriteAttribute(
                    node,
                    OdfNamespaces.Style,
                    "family",
                    OdfNamespaces.StylePrefix,
                    familyName);
            }
        }
        WriteRelationshipAttribute(
            node,
            "parent-style-name",
            style.ParentStyleName,
            style.OriginalParentStyleName,
            isNew);
        WriteRelationshipAttribute(
            node,
            "next-style-name",
            style.NextStyleName,
            style.OriginalNextStyleName,
            isNew);
        WriteRelationshipAttribute(
            node,
            "list-style-name",
            style.ListStyleName,
            style.OriginalListStyleName,
            isNew);
        WriteRelationshipAttribute(
            node,
            "master-page-name",
            style.MasterPageName,
            style.OriginalMasterPageName,
            isNew);
        WriteRelationshipAttribute(
            node,
            "data-style-name",
            style.DataStyleName,
            style.OriginalDataStyleName,
            isNew);
    }

    static void WriteRelationshipAttribute(
        OdfNode node,
        string localName,
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
            RemoveAttribute(node, OdfNamespaces.Style, localName);
        }
        else
        {
            WriteAttribute(
                node,
                OdfNamespaces.Style,
                localName,
                OdfNamespaces.StylePrefix,
                value);
        }
    }

    static void SnapshotStyle(OdfStyle style)
    {
        style.OriginalFamily = style.Family;
        style.OriginalName = style.Name;
        style.OriginalDisplayName = style.DisplayName;
        style.OriginalFamilyName = style.FamilyName;
        style.OriginalPartPath = style.PartPath;
        style.OriginalParentStyleName = style.ParentStyleName;
        style.OriginalNextStyleName = style.NextStyleName;
        style.OriginalListStyleName = style.ListStyleName;
        style.OriginalMasterPageName = style.MasterPageName;
        style.OriginalDataStyleName = style.DataStyleName;
        style.OriginalIsDefault = style.IsDefault;
        style.OriginalIsAutomatic = style.IsAutomatic;
    }

    static bool IsStyleDefinition(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && (node.Name.LocalName == "style"
                || node.Name.LocalName == "default-style");
    }

    internal static bool IsAutomaticSection(OdfNode section)
    {
        return section.Name != null
            && section.Name.NamespaceName == OdfNamespaces.Office
            && section.Name.LocalName == "automatic-styles";
    }

    internal static string ReadAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    internal static void WriteAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                attribute.Value = value;
                return;
            }
            index = index + 1;
        }
        OdfAttribute[] attributes = new OdfAttribute[
            node.Attributes.Length + 1];
        Array.Copy(node.Attributes, attributes, node.Attributes.Length);
        attributes[attributes.Length - 1] = Attribute(
            namespaceName,
            localName,
            prefix,
            value,
            node.Location.PartPath);
        node.Attributes = attributes;
    }

    internal static void RemoveAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int match = -1;
        int index = 0;
        while (index < node.Attributes.Length && match < 0)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                match = index;
            }
            index = index + 1;
        }
        if (match < 0)
        {
            return;
        }
        OdfAttribute[] attributes = new OdfAttribute[
            node.Attributes.Length - 1];
        int source = 0;
        int destination = 0;
        while (source < node.Attributes.Length)
        {
            if (source != match)
            {
                attributes[destination] = node.Attributes[source];
                destination = destination + 1;
            }
            source = source + 1;
        }
        node.Attributes = attributes;
    }

    internal static void AppendNode(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    internal static void RemoveNodeFromParent(OdfNode node)
    {
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            return;
        }
        int match = -1;
        int index = 0;
        while (index < parent.Children.Length && match < 0)
        {
            if (parent.Children[index] == node)
            {
                match = index;
            }
            index = index + 1;
        }
        if (match < 0)
        {
            node.Parent = null;
            return;
        }
        OdfNode[] children = new OdfNode[parent.Children.Length - 1];
        int source = 0;
        int destination = 0;
        while (source < parent.Children.Length)
        {
            if (source != match)
            {
                children[destination] = parent.Children[source];
                destination = destination + 1;
            }
            source = source + 1;
        }
        parent.Children = children;
        node.Parent = null;
    }

    internal static OdfQualifiedName QualifiedName(
        string namespaceName,
        string localName,
        string prefix)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = namespaceName;
        result.LocalName = localName;
        result.Prefix = prefix;
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

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string partPath)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name = QualifiedName(namespaceName, localName, prefix);
        result.Value = value;
        result.Location.PartPath = partPath;
        return result;
    }

    static string VersionText(OdfDocument document)
    {
        if (document.VersionInfo != null
            && document.VersionInfo.LexicalValue.Length > 0)
        {
            return document.VersionInfo.LexicalValue;
        }
        return "1.4";
    }

    static OdfStyleFamily ParseFamily(string familyName)
    {
        if (familyName == "paragraph")
        {
            return OdfStyleFamily.Paragraph;
        }
        if (familyName == "text")
        {
            return OdfStyleFamily.Text;
        }
        if (familyName == "section")
        {
            return OdfStyleFamily.Section;
        }
        if (familyName == "table")
        {
            return OdfStyleFamily.Table;
        }
        if (familyName == "table-column")
        {
            return OdfStyleFamily.TableColumn;
        }
        if (familyName == "table-row")
        {
            return OdfStyleFamily.TableRow;
        }
        if (familyName == "table-cell")
        {
            return OdfStyleFamily.TableCell;
        }
        if (familyName == "graphic")
        {
            return OdfStyleFamily.Graphic;
        }
        if (familyName == "chart")
        {
            return OdfStyleFamily.Chart;
        }
        if (familyName == "drawing-page")
        {
            return OdfStyleFamily.DrawingPage;
        }
        if (familyName == "presentation")
        {
            return OdfStyleFamily.Presentation;
        }
        if (familyName == "ruby")
        {
            return OdfStyleFamily.Ruby;
        }
        if (familyName == "list")
        {
            return OdfStyleFamily.List;
        }
        return OdfStyleFamily.Unknown;
    }

    static string FormatFamily(OdfStyleFamily family)
    {
        if (family == OdfStyleFamily.Paragraph)
        {
            return "paragraph";
        }
        if (family == OdfStyleFamily.Text)
        {
            return "text";
        }
        if (family == OdfStyleFamily.Section)
        {
            return "section";
        }
        if (family == OdfStyleFamily.Table)
        {
            return "table";
        }
        if (family == OdfStyleFamily.TableColumn)
        {
            return "table-column";
        }
        if (family == OdfStyleFamily.TableRow)
        {
            return "table-row";
        }
        if (family == OdfStyleFamily.TableCell)
        {
            return "table-cell";
        }
        if (family == OdfStyleFamily.Graphic)
        {
            return "graphic";
        }
        if (family == OdfStyleFamily.Chart)
        {
            return "chart";
        }
        if (family == OdfStyleFamily.DrawingPage)
        {
            return "drawing-page";
        }
        if (family == OdfStyleFamily.Presentation)
        {
            return "presentation";
        }
        if (family == OdfStyleFamily.Ruby)
        {
            return "ruby";
        }
        if (family == OdfStyleFamily.List)
        {
            return "list";
        }
        return "";
    }
}
