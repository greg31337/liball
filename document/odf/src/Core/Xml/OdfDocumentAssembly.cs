using System;
using System.Collections.Generic;

namespace odf;

internal class OdfDocumentPartTrees
{
    internal OdfNode[] ContentNodes = [];
    internal OdfNode[] StylesNodes = [];
    internal OdfNode[] MetadataNodes = [];
    internal OdfNode[] SettingsNodes = [];
    internal OdfNode[] FlatNodes = [];
    internal OdfPartRootMap? ContentRoot;
    internal OdfPartRootMap? StylesRoot;
    internal OdfPartRootMap? MetadataRoot;
    internal OdfPartRootMap? SettingsRoot;
    internal OdfPartRootMap? FlatRoot;
}

internal static class OdfDocumentAssembly
{
    internal static OdfDocument AssembleSeparateParts(
        OdfNode[] contentNodes,
        OdfNode[] stylesNodes,
        OdfNode[] metadataNodes,
        OdfNode[] settingsNodes)
    {
        if (contentNodes == null
            || stylesNodes == null
            || metadataNodes == null
            || settingsNodes == null)
        {
            throw new OdfDocumentException("Document part node collections are required.");
        }
        if (contentNodes.Length == 0)
        {
            throw new OdfDocumentException("A separate-part document requires content.xml.");
        }

        OdfDocumentPartTrees parts = new OdfDocumentPartTrees();
        parts.ContentNodes = CopyNodes(contentNodes);
        parts.StylesNodes = CopyNodes(stylesNodes);
        parts.MetadataNodes = CopyNodes(metadataNodes);
        parts.SettingsNodes = CopyNodes(settingsNodes);
        parts.ContentRoot = OdfPartRootRules.MapRoot(
            parts.ContentNodes,
            OdfPartNames.Content);
        if (parts.StylesNodes.Length > 0)
        {
            parts.StylesRoot = OdfPartRootRules.MapRoot(
                parts.StylesNodes,
                OdfPartNames.Styles);
        }
        if (parts.MetadataNodes.Length > 0)
        {
            parts.MetadataRoot = OdfPartRootRules.MapRoot(
                parts.MetadataNodes,
                OdfPartNames.Metadata);
        }
        if (parts.SettingsNodes.Length > 0)
        {
            parts.SettingsRoot = OdfPartRootRules.MapRoot(
                parts.SettingsNodes,
                OdfPartNames.Settings);
        }

        OdfDocument document = new OdfDocument();
        document.PackageKind = OdfPackageKind.Package;
        document.Nodes = CombineSeparateNodes(parts);
        document.PartTrees = parts;
        OdfDocumentModel.ApplyContext(
            document,
            new OdfLoadOptions(),
            new OdfSaveOptions(),
            "");
        return document;
    }

    internal static OdfDocument AssembleFlatDocument(OdfNode[] flatNodes)
    {
        if (flatNodes == null || flatNodes.Length == 0)
        {
            throw new OdfDocumentException("A flat document XML tree is required.");
        }
        OdfDocumentPartTrees parts = new OdfDocumentPartTrees();
        parts.FlatNodes = CopyNodes(flatNodes);
        parts.FlatRoot = OdfPartRootRules.MapRoot(parts.FlatNodes, "");
        if (!parts.FlatRoot.IsFlatDocument)
        {
            throw new OdfDocumentException(
                "A flat document requires an office:document root.");
        }

        OdfDocument document = new OdfDocument();
        document.PackageKind = OdfPackageKind.Flat;
        document.Nodes = CopyNodes(parts.FlatNodes);
        document.PartTrees = parts;
        OdfDocumentModel.ApplyContext(
            document,
            new OdfLoadOptions(),
            new OdfSaveOptions(),
            "");
        return document;
    }

    internal static OdfDocumentPartTrees DisassembleSeparateParts(
        OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException("A document is required for disassembly.");
        }
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            OdfPartRootMap flatRoot = OdfPartRootRules.MapRoot(document.Nodes, "");
            if (!flatRoot.IsFlatDocument)
            {
                throw new OdfDocumentException(
                    "The flat document aggregate does not contain an office:document root.");
            }
            return SplitFlatRoot(flatRoot, document.Nodes);
        }
        if (document.PartTrees == null
            || document.PartTrees.ContentRoot == null)
        {
            throw new OdfDocumentException(
                "The document aggregate does not retain separate XML part trees.");
        }
        return CopySeparateParts(document.PartTrees);
    }

    internal static OdfNode[] DisassembleFlatDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException("A document is required for disassembly.");
        }
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            OdfPartRootMap flatRoot = OdfPartRootRules.MapRoot(document.Nodes, "");
            if (!flatRoot.IsFlatDocument)
            {
                throw new OdfDocumentException(
                    "The flat document aggregate does not contain an office:document root.");
            }
            return CopyNodes(document.Nodes);
        }
        if (document.PartTrees == null
            || document.PartTrees.ContentRoot == null)
        {
            throw new OdfDocumentException(
                "The document aggregate does not retain separate XML part trees.");
        }
        return MergeSeparateParts(document.PartTrees);
    }

    static OdfDocumentPartTrees CopySeparateParts(OdfDocumentPartTrees source)
    {
        OdfDocumentPartTrees result = new OdfDocumentPartTrees();
        result.ContentNodes = CopyNodes(source.ContentNodes);
        result.StylesNodes = CopyNodes(source.StylesNodes);
        result.MetadataNodes = CopyNodes(source.MetadataNodes);
        result.SettingsNodes = CopyNodes(source.SettingsNodes);
        result.ContentRoot = OdfPartRootRules.MapRoot(
            result.ContentNodes,
            OdfPartNames.Content);
        if (result.StylesNodes.Length > 0)
        {
            result.StylesRoot = OdfPartRootRules.MapRoot(
                result.StylesNodes,
                OdfPartNames.Styles);
        }
        if (result.MetadataNodes.Length > 0)
        {
            result.MetadataRoot = OdfPartRootRules.MapRoot(
                result.MetadataNodes,
                OdfPartNames.Metadata);
        }
        if (result.SettingsNodes.Length > 0)
        {
            result.SettingsRoot = OdfPartRootRules.MapRoot(
                result.SettingsNodes,
                OdfPartNames.Settings);
        }
        return result;
    }

    static OdfDocumentPartTrees SplitFlatRoot(
        OdfPartRootMap flatMap,
        OdfNode[] flatNodes)
    {
        OdfNode contentRoot = CreatePartRoot(
            flatMap.Root,
            "document-content",
            OdfPartNames.Content);
        OdfNode stylesRoot = CreatePartRoot(
            flatMap.Root,
            "document-styles",
            OdfPartNames.Styles);
        OdfNode metadataRoot = CreatePartRoot(
            flatMap.Root,
            "document-meta",
            OdfPartNames.Metadata);
        OdfNode settingsRoot = CreatePartRoot(
            flatMap.Root,
            "document-settings",
            OdfPartNames.Settings);
        List<OdfNode> contentChildren = new List<OdfNode>();
        List<OdfNode> stylesChildren = new List<OdfNode>();
        List<OdfNode> metadataChildren = new List<OdfNode>();
        List<OdfNode> settingsChildren = new List<OdfNode>();

        foreach (OdfNode child in flatMap.Root.Children)
        {
            OdfNode clone = CloneNode(child);
            string localName = SectionLocalName(child);
            if (localName == "meta")
            {
                metadataChildren.Add(clone);
            }
            else if (localName == "settings")
            {
                settingsChildren.Add(clone);
            }
            else if (localName == "font-face-decls"
                || localName == "styles"
                || localName == "master-styles")
            {
                stylesChildren.Add(clone);
            }
            else
            {
                contentChildren.Add(clone);
            }
        }
        AttachChildren(contentRoot, contentChildren);
        AttachChildren(stylesRoot, stylesChildren);
        AttachChildren(metadataRoot, metadataChildren);
        AttachChildren(settingsRoot, settingsChildren);

        OdfDocumentPartTrees result = new OdfDocumentPartTrees();
        result.ContentNodes = CopyFlatTopLevelNodes(flatNodes, contentRoot);
        result.StylesNodes = [stylesRoot];
        result.MetadataNodes = [metadataRoot];
        result.SettingsNodes = [settingsRoot];
        result.ContentRoot = OdfPartRootRules.MapRoot(
            result.ContentNodes,
            OdfPartNames.Content);
        result.StylesRoot = OdfPartRootRules.MapRoot(
            result.StylesNodes,
            OdfPartNames.Styles);
        result.MetadataRoot = OdfPartRootRules.MapRoot(
            result.MetadataNodes,
            OdfPartNames.Metadata);
        result.SettingsRoot = OdfPartRootRules.MapRoot(
            result.SettingsNodes,
            OdfPartNames.Settings);
        return result;
    }

    static OdfNode[] MergeSeparateParts(OdfDocumentPartTrees parts)
    {
        if (parts.ContentRoot == null)
        {
            throw new OdfDocumentException("A content root is required for flat conversion.");
        }
        OdfNode flatRoot = CreatePartRoot(parts.ContentRoot.Root, "document", "");
        List<OdfNode> children = new List<OdfNode>();
        AddMappedSection(children, MetadataSection(parts));
        AddMappedSection(children, SettingsSection(parts));
        AddMappedSection(children, parts.ContentRoot.Scripts);
        AddMergedSection(
            children,
            parts.ContentRoot.FontFaceDeclarations,
            StylesFontFaceSection(parts));
        AddMappedSection(children, StylesNamedSection(parts));
        AddMergedSection(
            children,
            parts.ContentRoot.AutomaticStyles,
            StylesAutomaticSection(parts));
        AddMappedSection(children, StylesMasterSection(parts));
        AddUnmappedSections(children, parts.MetadataRoot);
        AddUnmappedSections(children, parts.SettingsRoot);
        AddUnmappedSections(children, parts.StylesRoot);
        AddUnmappedSections(children, parts.ContentRoot);
        AddAdditionalSections(children, parts.MetadataRoot);
        AddAdditionalSections(children, parts.SettingsRoot);
        AddAdditionalSections(children, parts.StylesRoot);
        AddAdditionalSections(children, parts.ContentRoot);
        AddRootScalarChildren(children, parts.MetadataRoot);
        AddRootScalarChildren(children, parts.SettingsRoot);
        AddRootScalarChildren(children, parts.StylesRoot);
        AddRootScalarChildren(children, parts.ContentRoot);
        AddMappedSection(children, parts.ContentRoot.Body);
        AttachChildren(flatRoot, children);

        return CreateFlatTopLevelNodes(parts, flatRoot);
    }

    static OdfNode? MetadataSection(OdfDocumentPartTrees parts)
    {
        if (parts.MetadataRoot == null)
        {
            return null;
        }
        return parts.MetadataRoot.Metadata;
    }

    static OdfNode? SettingsSection(OdfDocumentPartTrees parts)
    {
        if (parts.SettingsRoot == null)
        {
            return null;
        }
        return parts.SettingsRoot.Settings;
    }

    static OdfNode? StylesFontFaceSection(OdfDocumentPartTrees parts)
    {
        if (parts.StylesRoot == null)
        {
            return null;
        }
        return parts.StylesRoot.FontFaceDeclarations;
    }

    static OdfNode? StylesNamedSection(OdfDocumentPartTrees parts)
    {
        if (parts.StylesRoot == null)
        {
            return null;
        }
        return parts.StylesRoot.Styles;
    }

    static OdfNode? StylesAutomaticSection(OdfDocumentPartTrees parts)
    {
        if (parts.StylesRoot == null)
        {
            return null;
        }
        return parts.StylesRoot.AutomaticStyles;
    }

    static OdfNode? StylesMasterSection(OdfDocumentPartTrees parts)
    {
        if (parts.StylesRoot == null)
        {
            return null;
        }
        return parts.StylesRoot.MasterStyles;
    }

    static void AddMappedSection(
        List<OdfNode> children,
        OdfNode? section)
    {
        if (section != null)
        {
            children.Add(CloneNode(section));
        }
    }

    static void AddMergedSection(
        List<OdfNode> children,
        OdfNode? first,
        OdfNode? second)
    {
        if (first == null)
        {
            AddMappedSection(children, second);
            return;
        }
        OdfNode merged = CloneNode(first);
        if (second != null)
        {
            OdfNode secondClone = CloneNode(second);
            List<OdfNode> mergedChildren = new List<OdfNode>();
            foreach (OdfNode child in merged.Children)
            {
                mergedChildren.Add(child);
            }
            foreach (OdfNode child in secondClone.Children)
            {
                mergedChildren.Add(child);
            }
            AttachChildren(merged, mergedChildren);
        }
        children.Add(merged);
    }

    static void AddUnmappedSections(
        List<OdfNode> children,
        OdfPartRootMap? map)
    {
        if (map == null)
        {
            return;
        }
        foreach (OdfNode section in map.UnmappedSections)
        {
            children.Add(CloneNode(section));
        }
    }

    static void AddAdditionalSections(
        List<OdfNode> children,
        OdfPartRootMap? map)
    {
        if (map == null)
        {
            return;
        }
        foreach (OdfNode section in map.StandardSections)
        {
            if (!IsPrimarySection(map, section))
            {
                children.Add(CloneNode(section));
            }
        }
    }

    static bool IsPrimarySection(
        OdfPartRootMap map,
        OdfNode section)
    {
        return section == map.Scripts
            || section == map.FontFaceDeclarations
            || section == map.Styles
            || section == map.AutomaticStyles
            || section == map.MasterStyles
            || section == map.Metadata
            || section == map.Settings
            || section == map.Body;
    }

    static void AddRootScalarChildren(
        List<OdfNode> children,
        OdfPartRootMap? map)
    {
        if (map == null)
        {
            return;
        }
        foreach (OdfNode child in map.Root.Children)
        {
            if (child.Kind != OdfNodeKind.Element)
            {
                children.Add(CloneNode(child));
            }
        }
    }

    static OdfNode CreatePartRoot(
        OdfNode template,
        string localName,
        string partPath)
    {
        OdfNode root = CloneNode(template);
        root.Name = new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Office,
            LocalName = localName,
            Prefix = OdfNamespaces.OfficePrefix
        };
        root.Attributes = CopyRootAttributes(template.Attributes, localName);
        root.Children = [];
        root.Parent = null;
        root.WasEmptyElement = false;
        root.Location.PartPath = partPath;
        return root;
    }

    static OdfAttribute[] CopyRootAttributes(
        OdfAttribute[] attributes,
        string rootName)
    {
        List<OdfAttribute> result = new List<OdfAttribute>();
        foreach (OdfAttribute attribute in attributes)
        {
            if (rootName == "document"
                || attribute.Name.NamespaceName != OdfNamespaces.Office
                || attribute.Name.LocalName != "mimetype")
            {
                result.Add(new OdfAttribute
                {
                    Name = new OdfQualifiedName
                    {
                        NamespaceName = attribute.Name.NamespaceName,
                        LocalName = attribute.Name.LocalName,
                        Prefix = attribute.Name.Prefix
                    },
                    Value = attribute.Value
                });
            }
        }
        return result.ToArray();
    }

    static string SectionLocalName(OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office)
        {
            return node.Name.LocalName;
        }
        return "";
    }

    static void AttachChildren(
        OdfNode parent,
        List<OdfNode> children)
    {
        parent.Children = children.ToArray();
        foreach (OdfNode child in parent.Children)
        {
            child.Parent = parent;
        }
        if (parent.Children.Length > 0)
        {
            parent.WasEmptyElement = false;
        }
        SetPartPath(parent, parent.Location.PartPath);
    }

    static OdfNode[] CopyFlatTopLevelNodes(
        OdfNode[] flatNodes,
        OdfNode contentRoot)
    {
        List<OdfNode> result = new List<OdfNode>();
        foreach (OdfNode node in flatNodes)
        {
            if (node.Kind == OdfNodeKind.Element)
            {
                result.Add(contentRoot);
            }
            else
            {
                OdfNode clone = CloneNode(node);
                SetPartPath(clone, OdfPartNames.Content);
                result.Add(clone);
            }
        }
        return result.ToArray();
    }

    static OdfNode[] CreateFlatTopLevelNodes(
        OdfDocumentPartTrees parts,
        OdfNode flatRoot)
    {
        List<OdfNode> result = new List<OdfNode>();
        bool rootAdded = false;
        foreach (OdfNode node in parts.ContentNodes)
        {
            if (node.Kind == OdfNodeKind.Element)
            {
                AddPartScalarNodes(result, parts.StylesNodes);
                AddPartScalarNodes(result, parts.MetadataNodes);
                AddPartScalarNodes(result, parts.SettingsNodes);
                result.Add(flatRoot);
                rootAdded = true;
            }
            else
            {
                OdfNode clone = CloneNode(node);
                SetPartPath(clone, "");
                result.Add(clone);
            }
        }
        if (!rootAdded)
        {
            result.Add(flatRoot);
        }
        return result.ToArray();
    }

    static void AddPartScalarNodes(
        List<OdfNode> result,
        OdfNode[] nodes)
    {
        foreach (OdfNode node in nodes)
        {
            if (node.Kind != OdfNodeKind.Element)
            {
                OdfNode clone = CloneNode(node);
                SetPartPath(clone, "");
                result.Add(clone);
            }
        }
    }

    static void SetPartPath(OdfNode node, string partPath)
    {
        node.Location.PartPath = partPath;
        foreach (OdfNamespaceDeclaration declaration in node.NamespaceDeclarations)
        {
            declaration.Location.PartPath = partPath;
        }
        foreach (OdfAttribute attribute in node.Attributes)
        {
            attribute.Location.PartPath = partPath;
        }
        foreach (OdfNode child in node.Children)
        {
            SetPartPath(child, partPath);
        }
    }

    static OdfNode CloneNode(OdfNode node)
    {
        OdfTreeEditor editor = new OdfTreeEditor([]);
        return editor.CloneSubtree(node);
    }

    static OdfNode[] CopyNodes(OdfNode[] nodes)
    {
        OdfNode[] result = new OdfNode[nodes.Length];
        Array.Copy(nodes, result, nodes.Length);
        return result;
    }

    static OdfNode[] CombineSeparateNodes(OdfDocumentPartTrees parts)
    {
        List<OdfNode> result = new List<OdfNode>();
        AppendNodes(result, parts.ContentNodes);
        AppendNodes(result, parts.StylesNodes);
        AppendNodes(result, parts.MetadataNodes);
        AppendNodes(result, parts.SettingsNodes);
        return result.ToArray();
    }

    static void AppendNodes(List<OdfNode> result, OdfNode[] nodes)
    {
        foreach (OdfNode node in nodes)
        {
            result.Add(node);
        }
    }
}
