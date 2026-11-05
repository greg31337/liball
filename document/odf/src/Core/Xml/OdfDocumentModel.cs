using System;
using System.Collections.Generic;

namespace odf;

internal static class OdfDocumentModel
{
    internal static void ApplyContext(
        OdfDocument document,
        OdfLoadOptions loadOptions,
        OdfSaveOptions saveOptions,
        string mediaType)
    {
        if (document == null
            || loadOptions == null
            || saveOptions == null
            || mediaType == null)
        {
            throw new OdfDocumentException(
                "A document, load options, save options, and media type are required.");
        }
        document.LoadOptions = CopyLoadOptions(loadOptions);
        document.SaveOptions = CopySaveOptions(saveOptions);
        ApplyLoadContentPolicy(document);
        document.Parts = CreateParts(document);
        document.MediaType = SelectMediaType(document, mediaType);
        PopulateDocumentState(document);
    }

    internal static OdfNode[] PreparePartNodes(
        OdfPart part,
        OdfSaveOptions options)
    {
        if (part == null || options == null)
        {
            throw new OdfDocumentException(
                "A document part and save options are required.");
        }
        OdfNode[] nodes = CloneNodes(part.Nodes);
        CompatibilityProfilePolicy policy =
            OdfCompatibilityProfileRules.Resolve(
                options.CompatibilityProfile);
        if (!policy.IsValid)
        {
            throw new OdfDocumentException(
                "The requested output compatibility profile is not recognized.");
        }
        if (options.TargetConformanceClass
                != OdfConformanceClass.Unknown
            && policy.ConformanceClass != OdfConformanceClass.Unknown
            && options.TargetConformanceClass
                != policy.ConformanceClass)
        {
            throw new OdfDocumentException(
                "The target conformance class conflicts with the output compatibility profile.");
        }
        string mappingPath = part.Path;
        if (part.IsFlatDocument)
        {
            mappingPath = "";
        }
        OdfPartRootMap map = OdfPartRootRules.MapRoot(nodes, mappingPath);
        bool preserveUnknown = options.PreserveUnknownContent;
        if (policy.IsActive)
        {
            preserveUnknown = true;
        }
        else if (part.IsFutureVersion
            && !policy.IsActive
            && options.TargetVersion == OdfVersion.Unknown)
        {
            preserveUnknown = true;
        }
        if (!preserveUnknown)
        {
            RemoveUnmappedSections(map);
        }
        if (policy.IsActive)
        {
            FilterExtensionContent(map.Root, policy);
        }
        string targetVersion = TargetVersionText(options.TargetVersion);
        if (targetVersion.Length > 0)
        {
            SetRootVersion(map.Root, targetVersion);
        }
        return nodes;
    }

    static void FilterExtensionContent(
        OdfNode node,
        CompatibilityProfilePolicy policy)
    {
        List<OdfNamespaceDeclaration> declarations =
            new List<OdfNamespaceDeclaration>();
        int declarationIndex = 0;
        while (declarationIndex < node.NamespaceDeclarations.Length)
        {
            OdfNamespaceDeclaration declaration =
                node.NamespaceDeclarations[declarationIndex];
            if (OdfCompatibilityProfileRules.AllowsNamespace(
                policy,
                declaration.NamespaceName))
            {
                declarations.Add(declaration);
            }
            declarationIndex = declarationIndex + 1;
        }
        node.NamespaceDeclarations = declarations.ToArray();

        List<OdfAttribute> attributes = new List<OdfAttribute>();
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[attributeIndex];
            if (OdfCompatibilityProfileRules.AllowsNamespace(
                policy,
                attribute.Name.NamespaceName))
            {
                attributes.Add(attribute);
            }
            attributeIndex = attributeIndex + 1;
        }
        node.Attributes = attributes.ToArray();

        List<OdfNode> children = new List<OdfNode>();
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfNode child = node.Children[childIndex];
            bool allowed = child.Kind != OdfNodeKind.Element
                || child.Name != null
                && OdfCompatibilityProfileRules.AllowsNamespace(
                    policy,
                    child.Name.NamespaceName);
            if (allowed)
            {
                child.Parent = node;
                FilterExtensionContent(child, policy);
                children.Add(child);
            }
            else
            {
                child.Parent = null;
            }
            childIndex = childIndex + 1;
        }
        node.Children = children.ToArray();
    }

    internal static OdfPart CreateStandalonePart(
        OdfNode[] nodes,
        string partPath,
        string mediaType)
    {
        if (nodes == null || partPath == null || mediaType == null)
        {
            throw new OdfDocumentException(
                "XML nodes, a part path, and a media type are required.");
        }
        OdfPartRootMap map = OdfPartRootRules.MapRoot(nodes, partPath);
        return CreatePart(map, nodes, partPath, mediaType);
    }

    internal static void RememberSaveOptions(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document == null || options == null)
        {
            throw new OdfDocumentException(
                "A document and save options are required.");
        }
        document.SaveOptions = CopySaveOptions(options);
    }

    static OdfPart[] CreateParts(OdfDocument document)
    {
        if (document.PartTrees == null)
        {
            return [];
        }
        List<OdfPart> result = new List<OdfPart>();
        OdfDocumentPartTrees trees = document.PartTrees;
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            if (trees.FlatRoot != null)
            {
                result.Add(CreatePart(
                    trees.FlatRoot,
                    trees.FlatNodes,
                    "",
                    ""));
            }
            return result.ToArray();
        }
        if (trees.ContentRoot != null)
        {
            result.Add(CreatePart(
                trees.ContentRoot,
                trees.ContentNodes,
                OdfPartNames.Content,
                OdfMediaTypes.Xml));
        }
        if (trees.StylesRoot != null)
        {
            result.Add(CreatePart(
                trees.StylesRoot,
                trees.StylesNodes,
                OdfPartNames.Styles,
                OdfMediaTypes.Xml));
        }
        if (trees.MetadataRoot != null)
        {
            result.Add(CreatePart(
                trees.MetadataRoot,
                trees.MetadataNodes,
                OdfPartNames.Metadata,
                OdfMediaTypes.Xml));
        }
        if (trees.SettingsRoot != null)
        {
            result.Add(CreatePart(
                trees.SettingsRoot,
                trees.SettingsNodes,
                OdfPartNames.Settings,
                OdfMediaTypes.Xml));
        }
        return result.ToArray();
    }

    static void ApplyLoadContentPolicy(OdfDocument document)
    {
        if (document.LoadOptions.PreserveUnknownContent
            || document.PartTrees == null)
        {
            return;
        }
        OdfDocumentPartTrees trees = document.PartTrees;
        OdfPartRootMap? contentRoot = trees.ContentRoot;
        if (contentRoot != null && CanRemoveLoadPassthrough(contentRoot))
        {
            RemoveUnmappedSections(contentRoot);
            trees.ContentRoot = OdfPartRootRules.MapRoot(
                trees.ContentNodes,
                OdfPartNames.Content);
        }
        OdfPartRootMap? stylesRoot = trees.StylesRoot;
        if (stylesRoot != null && CanRemoveLoadPassthrough(stylesRoot))
        {
            RemoveUnmappedSections(stylesRoot);
            trees.StylesRoot = OdfPartRootRules.MapRoot(
                trees.StylesNodes,
                OdfPartNames.Styles);
        }
        OdfPartRootMap? metadataRoot = trees.MetadataRoot;
        if (metadataRoot != null && CanRemoveLoadPassthrough(metadataRoot))
        {
            RemoveUnmappedSections(metadataRoot);
            trees.MetadataRoot = OdfPartRootRules.MapRoot(
                trees.MetadataNodes,
                OdfPartNames.Metadata);
        }
        OdfPartRootMap? settingsRoot = trees.SettingsRoot;
        if (settingsRoot != null && CanRemoveLoadPassthrough(settingsRoot))
        {
            RemoveUnmappedSections(settingsRoot);
            trees.SettingsRoot = OdfPartRootRules.MapRoot(
                trees.SettingsNodes,
                OdfPartNames.Settings);
        }
        OdfPartRootMap? flatRoot = trees.FlatRoot;
        if (flatRoot != null && CanRemoveLoadPassthrough(flatRoot))
        {
            RemoveUnmappedSections(flatRoot);
            trees.FlatRoot = OdfPartRootRules.MapRoot(trees.FlatNodes, "");
        }
    }

    static bool CanRemoveLoadPassthrough(OdfPartRootMap? map)
    {
        if (map == null)
        {
            return false;
        }
        OdfVersionInfo? version = ReadVersion(map.Root);
        return version == null || !version.IsFuture;
    }

    static OdfPart CreatePart(
        OdfPartRootMap map,
        OdfNode[] nodes,
        string path,
        string mediaType)
    {
        OdfPart result = new OdfPart();
        result.Path = path;
        result.MediaType = mediaType;
        result.Nodes = CopyNodes(nodes);
        result.Root = map.Root;
        result.RootName = map.RootName;
        result.Sections = CopyNodes(map.Sections);
        result.PassthroughNodes = CopyNodes(map.UnmappedSections);
        result.Scripts = map.Scripts;
        result.FontFaceDeclarations = map.FontFaceDeclarations;
        result.Styles = map.Styles;
        result.AutomaticStyles = map.AutomaticStyles;
        result.MasterStyles = map.MasterStyles;
        result.Metadata = map.Metadata;
        result.Settings = map.Settings;
        result.Body = map.Body;
        result.VersionInfo = ReadVersion(map.Root);
        result.IsFlatDocument = map.IsFlatDocument;
        result.IsFutureVersion = result.VersionInfo != null
            && result.VersionInfo.IsFuture;
        result.RootMap = map;
        return result;
    }

    static void PopulateDocumentState(OdfDocument document)
    {
        document.VersionInfo = null;
        document.Body = null;
        document.IsFutureVersion = false;
        List<OdfNode> passthrough = new List<OdfNode>();
        foreach (OdfPart part in document.Parts)
        {
            if (document.VersionInfo == null && part.VersionInfo != null)
            {
                document.VersionInfo = part.VersionInfo;
            }
            if (document.Body == null && part.Body != null)
            {
                document.Body = part.Body;
            }
            if (part.IsFutureVersion)
            {
                document.IsFutureVersion = true;
            }
            foreach (OdfNode node in part.PassthroughNodes)
            {
                passthrough.Add(node);
            }
        }
        document.PassthroughNodes = passthrough.ToArray();
        if (document.VersionInfo != null)
        {
            string producer = FindGenerator(document.Parts);
            OdfVersionRules.ApplyProducerProfile(
                document.VersionInfo,
                producer);
        }
        document.Kind = DetectDocumentKind(
            document.Body,
            document.MediaType);
        if (document.PackageKind == OdfPackageKind.Flat
            && document.Parts.Length > 0)
        {
            document.Parts[0].MediaType = document.MediaType;
        }
        MetadataDocumentModel.LoadMetadata(document);
        RdfaDocumentModel.LoadRdfa(document);
        StyleDocumentModel.LoadStyles(document);
        FontFaceDocumentModel.LoadFontFaces(document);
        PageLayoutDocumentModel.LoadPageLayouts(document);
        DataStyleDocumentModel.LoadDataStyles(document);
        ListStyleDocumentModel.LoadListStyles(document);
        StyleResourceDocumentModel.LoadStyleResources(document);
        TextDocumentModel.LoadTextBlocks(document);
        TextFlowDocumentModel.LoadTextFlows(document);
        DrawingDocumentModel.LoadDrawings(document);
        ImageResourceManager.LoadImages(document);
        MediaResourceManager.LoadMediaResources(document);
        EmbeddedContentManager.LoadEmbeddedContent(document);
        DrawingResourceManager.RefreshOrphans(document);
        PresentationDocumentModel.LoadPresentation(document);
        MasterTextDocumentModel.LoadMasterText(document);
        TextListDocumentModel.LoadTextLists(document);
        TextFieldDocumentModel.LoadTextFields(document);
        NoteDocumentModel.LoadNotes(document);
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
        TextChangeDocumentModel.LoadChanges(document);
        TextIndexDocumentModel.LoadTextIndexes(document);
        TableDocumentModel.LoadTables(document);
        FormDocumentModel.LoadForms(document);
        XFormsDocumentModel.LoadXForms(document);
        DatabaseDocumentModel.LoadDatabase(document);
        SettingDocumentModel.LoadSettings(document);
        ScriptDocumentModel.LoadScripts(document);
    }

    static OdfDocumentKind DetectDocumentKind(
        OdfNode? body,
        string mediaType)
    {
        OdfDocumentKind declared = MimetypeRules.GetDocumentKind(mediaType);
        if (declared == OdfDocumentKind.Formula
            || declared == OdfDocumentKind.FormulaTemplate)
        {
            return declared;
        }
        OdfDocumentKind bodyKind = BodyDocumentKind(body);
        if (bodyKind == OdfDocumentKind.Unknown)
        {
            return declared;
        }
        if (IsMatchingKindFamily(bodyKind, declared))
        {
            return declared;
        }
        return bodyKind;
    }

    static OdfDocumentKind BodyDocumentKind(OdfNode? body)
    {
        if (body == null)
        {
            return OdfDocumentKind.Unknown;
        }
        foreach (OdfNode child in body.Children)
        {
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Office)
            {
                string name = child.Name.LocalName;
                if (name == "text")
                {
                    return OdfDocumentKind.Text;
                }
                if (name == "spreadsheet")
                {
                    return OdfDocumentKind.Spreadsheet;
                }
                if (name == "presentation")
                {
                    return OdfDocumentKind.Presentation;
                }
                if (name == "drawing")
                {
                    return OdfDocumentKind.Drawing;
                }
                if (name == "chart")
                {
                    return OdfDocumentKind.Chart;
                }
                if (name == "image")
                {
                    return OdfDocumentKind.Image;
                }
                if (name == "database")
                {
                    return OdfDocumentKind.Database;
                }
                return OdfDocumentKind.Unknown;
            }
        }
        return OdfDocumentKind.Unknown;
    }

    static bool IsMatchingKindFamily(
        OdfDocumentKind bodyKind,
        OdfDocumentKind declared)
    {
        if (bodyKind == OdfDocumentKind.Text)
        {
            return declared == OdfDocumentKind.Text
                || declared == OdfDocumentKind.TextTemplate
                || declared == OdfDocumentKind.TextMaster
                || declared == OdfDocumentKind.TextMasterTemplate;
        }
        if (bodyKind == OdfDocumentKind.Spreadsheet)
        {
            return declared == OdfDocumentKind.Spreadsheet
                || declared == OdfDocumentKind.SpreadsheetTemplate;
        }
        if (bodyKind == OdfDocumentKind.Presentation)
        {
            return declared == OdfDocumentKind.Presentation
                || declared == OdfDocumentKind.PresentationTemplate;
        }
        if (bodyKind == OdfDocumentKind.Drawing)
        {
            return declared == OdfDocumentKind.Drawing
                || declared == OdfDocumentKind.DrawingTemplate;
        }
        if (bodyKind == OdfDocumentKind.Chart)
        {
            return declared == OdfDocumentKind.Chart
                || declared == OdfDocumentKind.ChartTemplate;
        }
        if (bodyKind == OdfDocumentKind.Database)
        {
            return declared == OdfDocumentKind.Database;
        }
        return bodyKind == OdfDocumentKind.Image
            && (declared == OdfDocumentKind.Image
                || declared == OdfDocumentKind.ImageTemplate);
    }

    static string SelectMediaType(OdfDocument document, string supplied)
    {
        if (supplied.Length > 0)
        {
            return supplied;
        }
        if (document.PartTrees != null
            && document.PartTrees.FlatRoot != null)
        {
            return FindRootAttribute(
                document.PartTrees.FlatRoot.Root,
                OdfNamespaces.Office,
                "mimetype");
        }
        if (document.Manifest != null)
        {
            foreach (OdfManifestEntry entry in document.Manifest.Entries)
            {
                if (entry.FullPath == OdfPartNames.Root)
                {
                    return entry.MediaType;
                }
            }
        }
        return "";
    }

    static OdfVersionInfo? ReadVersion(OdfNode root)
    {
        string lexical = FindRootAttribute(
            root,
            OdfNamespaces.Office,
            "version");
        if (lexical.Length == 0)
        {
            return null;
        }
        return OdfVersionRules.ParseVersion(lexical);
    }

    static string FindRootAttribute(
        OdfNode root,
        string namespaceName,
        string localName)
    {
        foreach (OdfAttribute attribute in root.Attributes)
        {
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
        }
        return "";
    }

    static string FindGenerator(OdfPart[] parts)
    {
        foreach (OdfPart part in parts)
        {
            if (part.Metadata != null)
            {
                string value = FindGeneratorNode(part.Metadata);
                if (value.Length > 0)
                {
                    return value;
                }
            }
        }
        return "";
    }

    static string FindGeneratorNode(OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Metadata
            && node.Name.LocalName == "generator")
        {
            OdfTreeEditor editor = new OdfTreeEditor([node]);
            return editor.GetTextContent(node);
        }
        foreach (OdfNode child in node.Children)
        {
            string value = FindGeneratorNode(child);
            if (value.Length > 0)
            {
                return value;
            }
        }
        return "";
    }

    static void RemoveUnmappedSections(OdfPartRootMap map)
    {
        List<OdfNode> retained = new List<OdfNode>();
        foreach (OdfNode child in map.Root.Children)
        {
            if (!IsUnmappedNode(map, child))
            {
                retained.Add(child);
            }
            else
            {
                child.Parent = null;
            }
        }
        map.Root.Children = retained.ToArray();
    }

    static bool IsUnmappedNode(OdfPartRootMap map, OdfNode node)
    {
        foreach (OdfNode unmapped in map.UnmappedSections)
        {
            if (node == unmapped)
            {
                return true;
            }
        }
        return false;
    }

    static void SetRootVersion(OdfNode root, string lexical)
    {
        foreach (OdfAttribute attribute in root.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && attribute.Name.LocalName == "version")
            {
                attribute.Value = lexical;
                return;
            }
        }
        OdfAttribute[] attributes = new OdfAttribute[root.Attributes.Length + 1];
        Array.Copy(root.Attributes, attributes, root.Attributes.Length);
        attributes[attributes.Length - 1] = new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Office,
                LocalName = "version",
                Prefix = OdfNamespaces.OfficePrefix
            },
            Value = lexical
        };
        root.Attributes = attributes;
    }

    static string TargetVersionText(OdfVersion version)
    {
        if (version == OdfVersion.Version10)
        {
            return "1.0";
        }
        if (version == OdfVersion.Version11)
        {
            return "1.1";
        }
        if (version == OdfVersion.Version12)
        {
            return "1.2";
        }
        if (version == OdfVersion.Version13)
        {
            return "1.3";
        }
        if (version == OdfVersion.Version14)
        {
            return "1.4";
        }
        return "";
    }

    static OdfNode[] CloneNodes(OdfNode[] nodes)
    {
        OdfNode[] result = new OdfNode[nodes.Length];
        OdfTreeEditor editor = new OdfTreeEditor([]);
        int index = 0;
        while (index < nodes.Length)
        {
            result[index] = editor.CloneSubtree(nodes[index]);
            index++;
        }
        return result;
    }

    static OdfNode[] CopyNodes(OdfNode[] nodes)
    {
        OdfNode[] result = new OdfNode[nodes.Length];
        Array.Copy(nodes, result, nodes.Length);
        return result;
    }

    static OdfLoadOptions CopyLoadOptions(OdfLoadOptions source)
    {
        OdfLoadOptions result = new OdfLoadOptions();
        result.SecurityLimits = CopySecurityLimits(source.SecurityLimits);
        result.Cancellation = CopyCancellation(source.Cancellation);
        result.StreamSelection = CopyStreamSelection(source.StreamSelection);
        result.LeaveInputOpen = source.LeaveInputOpen;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveUnknownContent = source.PreserveUnknownContent;
        result.MaterializeXmlTrees = source.MaterializeXmlTrees;
        result.IndexPackageEntries = source.IndexPackageEntries;
        result.ValidateOnLoad = source.ValidateOnLoad;
        result.AllowRecovery = source.AllowRecovery;
        return result;
    }

    static OdfSaveOptions CopySaveOptions(OdfSaveOptions source)
    {
        OdfSaveOptions result = new OdfSaveOptions();
        result.Cancellation = CopyCancellation(source.Cancellation);
        result.LeaveOutputOpen = source.LeaveOutputOpen;
        result.PreserveUnknownContent = source.PreserveUnknownContent;
        result.PreserveLexicalValues = source.PreserveLexicalValues;
        result.PreserveTimestamps = source.PreserveTimestamps;
        result.Deterministic = source.Deterministic;
        result.IndentXml = source.IndentXml;
        result.SortManifestEntries = source.SortManifestEntries;
        result.CompatibilityProfile = source.CompatibilityProfile;
        result.CompressionMethod = source.CompressionMethod;
        result.TargetVersion = source.TargetVersion;
        result.TargetConformanceClass = source.TargetConformanceClass;
        return result;
    }

    static OdfSecurityLimits CopySecurityLimits(OdfSecurityLimits source)
    {
        OdfSecurityLimits result = new OdfSecurityLimits();
        result.MaximumPackageEntries = source.MaximumPackageEntries;
        result.MaximumTotalExpandedBytes = source.MaximumTotalExpandedBytes;
        result.MaximumSingleEntryBytes = source.MaximumSingleEntryBytes;
        result.MaximumCompressionRatio = source.MaximumCompressionRatio;
        result.MaximumPathLength = source.MaximumPathLength;
        result.MaximumXmlDepth = source.MaximumXmlDepth;
        result.MaximumXmlAttributesPerElement = source.MaximumXmlAttributesPerElement;
        result.MaximumXmlTextLength = source.MaximumXmlTextLength;
        result.MaximumFormulaDepth = source.MaximumFormulaDepth;
        result.MaximumEmbeddedPackageDepth = source.MaximumEmbeddedPackageDepth;
        return result;
    }

    static OdfCancellationState CopyCancellation(OdfCancellationState source)
    {
        OdfCancellationState result = new OdfCancellationState();
        result.IsCancellationRequested = source.IsCancellationRequested;
        result.Reason = source.Reason;
        return result;
    }

    static OdfStreamSelection CopyStreamSelection(OdfStreamSelection source)
    {
        OdfStreamSelection result = new OdfStreamSelection();
        result.PartPaths = new string[source.PartPaths.Length];
        Array.Copy(
            source.PartPaths,
            result.PartPaths,
            source.PartPaths.Length);
        result.ElementNames = new OdfQualifiedName[source.ElementNames.Length];
        int index = 0;
        while (index < source.ElementNames.Length)
        {
            OdfQualifiedName name = source.ElementNames[index];
            result.ElementNames[index] = new OdfQualifiedName
            {
                NamespaceName = name.NamespaceName,
                LocalName = name.LocalName,
                Prefix = name.Prefix
            };
            index++;
        }
        result.IncludePackageEntryEvents = source.IncludePackageEntryEvents;
        result.IncludeElementEvents = source.IncludeElementEvents;
        result.IncludeTextEvents = source.IncludeTextEvents;
        result.IncludeCommentEvents = source.IncludeCommentEvents;
        result.IncludeProcessingInstructionEvents = source.IncludeProcessingInstructionEvents;
        result.IncludeBinaryContent = source.IncludeBinaryContent;
        result.SkipUnselectedSubtrees = source.SkipUnselectedSubtrees;
        return result;
    }
}
