using System;

namespace odf;

internal static class DocumentMutationModel
{
    internal static OdfEditResult CheckBodyKind(
        OdfDocument document,
        OdfDocumentKind expectedKind)
    {
        RequireDocument(document);
        if (expectedKind == OdfDocumentKind.Unknown)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "A recognized expected document kind is required.",
                "");
        }
        OdfDocumentKind actualKind = DetectBodyKind(document);
        if (actualKind == OdfDocumentKind.Unknown)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The document body kind could not be determined.",
                BodyPartPath(document));
        }
        if (!KindsMatch(actualKind, expectedKind))
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The document body does not match the expected document kind.",
                BodyPartPath(document));
        }
        return Result(
            OdfEditStatus.Success,
            false,
            1,
            "The document body matches the expected document kind.",
            BodyPartPath(document));
    }

    internal static OdfEditResult AddDocumentPart(
        OdfDocument document,
        string path,
        OdfNode[] nodes,
        string mediaType)
    {
        RequirePartInput(document, path, nodes, mediaType);
        OdfEditResult packageCheck = CheckPackageMutation(document, path);
        if (packageCheck.Status != OdfEditStatus.Success)
        {
            return packageCheck;
        }
        if (!IsStandardPartPath(path))
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Only standard materialized XML parts can be added as document parts.",
                path);
        }
        if (mediaType.Length > 0 && mediaType != OdfMediaTypes.Xml)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "A standard document part must use the XML media type.",
                path);
        }
        if (FindPart(document, path) != null)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The document part already exists.",
                path);
        }
        OdfPart part = PreparePart(nodes, path);
        byte[] data = new OdfXmlWriter().WriteXmlBytes(
            part,
            document.SaveOptions);
        OdfPackage package = document.Package ?? new OdfPackage();
        OdfEditResult added = new OdfPackageWriter().AddPackageEntry(
            package,
            path,
            data,
            OdfCompressionMethod.Deflated);
        if (added.Status != OdfEditStatus.Success)
        {
            return added;
        }
        SetPartTree(document, path, part.Nodes, part.RootMap);
        RefreshDocument(document);
        return Result(
            OdfEditStatus.Success,
            true,
            1,
            "Added the materialized document part.",
            path);
    }

    internal static OdfEditResult RemoveDocumentPart(
        OdfDocument document,
        string path)
    {
        RequirePathInput(document, path);
        OdfEditResult packageCheck = CheckPackageMutation(document, path);
        if (packageCheck.Status != OdfEditStatus.Success)
        {
            return packageCheck;
        }
        if (!IsStandardPartPath(path))
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Only standard materialized XML parts can be removed as document parts.",
                path);
        }
        if (path == OdfPartNames.Content)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The required content.xml part cannot be removed.",
                path);
        }
        if (FindPart(document, path) == null)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The document part does not exist.",
                path);
        }
        OdfPackage package = document.Package ?? new OdfPackage();
        OdfEditResult removed = new OdfPackageWriter()
            .RemovePackageEntry(package, path);
        if (removed.Status != OdfEditStatus.Success)
        {
            return removed;
        }
        ClearPartTree(document, path);
        RefreshDocument(document);
        return Result(
            OdfEditStatus.Success,
            true,
            1,
            "Removed the materialized document part.",
            path);
    }

    internal static OdfEditResult CopyDocumentPart(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        string destinationPath)
    {
        RequirePathInput(sourceDocument, sourcePath);
        RequirePathInput(targetDocument, destinationPath);
        OdfPart? source = FindPart(sourceDocument, sourcePath);
        if (source == null)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The source document part does not exist.",
                sourcePath);
        }
        return AddDocumentPart(
            targetDocument,
            destinationPath,
            source.Nodes,
            source.MediaType);
    }

    internal static OdfEditResult AddDocumentResource(
        OdfDocument document,
        string path,
        byte[] data,
        string mediaType,
        OdfCompressionMethod compressionMethod)
    {
        RequireResourceInput(document, path, data, mediaType);
        OdfEditResult resourceCheck = CheckResourceMutation(document, path);
        if (resourceCheck.Status != OdfEditStatus.Success)
        {
            return resourceCheck;
        }
        if (mediaType.Length > 0
            && !OdfMediaTypeRules.ParseMediaType(mediaType).IsValid)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The resource media type is not valid.",
                path);
        }
        OdfPackage package = document.Package ?? new OdfPackage();
        OdfEditResult added = new OdfPackageWriter().AddPackageEntry(
            package,
            path,
            data,
            compressionMethod);
        if (added.Status != OdfEditStatus.Success)
        {
            return added;
        }
        if (mediaType.Length > 0)
        {
            SetManifestMediaType(package.Manifest, path, mediaType);
            SynchronizeManifest(package);
        }
        RefreshDocument(document);
        return Result(
            OdfEditStatus.Success,
            true,
            1,
            "Added the document resource.",
            path);
    }

    internal static OdfEditResult RemoveDocumentResource(
        OdfDocument document,
        string path)
    {
        RequirePathInput(document, path);
        OdfEditResult resourceCheck = CheckResourceMutation(document, path);
        if (resourceCheck.Status != OdfEditStatus.Success)
        {
            return resourceCheck;
        }
        OdfPackage package = document.Package ?? new OdfPackage();
        OdfEditResult removed = new OdfPackageWriter()
            .RemovePackageEntry(package, path);
        if (removed.Status != OdfEditStatus.Success)
        {
            return removed;
        }
        RefreshDocument(document);
        return Result(
            OdfEditStatus.Success,
            true,
            1,
            "Removed the document resource.",
            path);
    }

    internal static OdfEditResult CopyDocumentResource(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        string destinationPath)
    {
        RequirePathInput(sourceDocument, sourcePath);
        RequirePathInput(targetDocument, destinationPath);
        OdfEditResult resourceCheck = CheckResourceMutation(
            targetDocument,
            destinationPath);
        if (resourceCheck.Status != OdfEditStatus.Success)
        {
            return resourceCheck;
        }
        EditablePackageEntry? source = FindPackageEntry(
            sourceDocument,
            sourcePath);
        if (source == null)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The source document resource does not exist.",
                sourcePath);
        }
        if (!source.IsContentAvailable)
        {
            return Result(
                OdfEditStatus.Failed,
                false,
                0,
                "The source document resource content is unavailable.",
                sourcePath);
        }
        string mediaType = ManifestMediaType(
            sourceDocument.Manifest,
            sourcePath);
        return AddDocumentResource(
            targetDocument,
            destinationPath,
            source.Data,
            mediaType,
            source.Entry.CompressionMethod);
    }

    internal static OdfDocument ConvertToPackageDocument(
        OdfDocument document)
    {
        RequireDocument(document);
        if (document.PackageKind == OdfPackageKind.Package)
        {
            return document;
        }
        if (document.PackageKind != OdfPackageKind.Flat)
        {
            throw new OdfEditException(
                "Only a flat document can be converted to a package document.");
        }
        RequireConsistentBody(document);
        RequireConversionMediaType(document);
        OdfDocumentPartTrees trees =
            OdfDocumentAssembly.DisassembleSeparateParts(document);
        OdfDocument result = OdfDocumentAssembly.AssembleSeparateParts(
            trees.ContentNodes,
            trees.StylesNodes,
            trees.MetadataNodes,
            trees.SettingsNodes);
        OdfDocument shell = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPackage package = shell.Package ?? new OdfPackage();
        OdfEditResult mediaRepair = PackageApiModel
            .RequireEntryIndex(package)
            .RepairMimetypeEntry(document.MediaType);
        if (mediaRepair.Status != OdfEditStatus.Success
            && mediaRepair.Status != OdfEditStatus.NoChange)
        {
            throw new OdfEditException(
                "The package media type could not be initialized.");
        }
        package.MediaType = document.MediaType;
        package.DocumentKind = document.Kind;
        PackageApiModel.RefreshPackage(package);
        result.Package = package;
        result.PackageEntries = package.Entries;
        result.Manifest = package.Manifest;
        OdfDocumentModel.ApplyContext(
            result,
            document.LoadOptions,
            document.SaveOptions,
            document.MediaType);
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            result,
            document.SaveOptions);
        OdfDocument converted = new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "converted.odf",
            document.LoadOptions);
        OdfDocumentModel.RememberSaveOptions(
            converted,
            document.SaveOptions);
        return converted;
    }

    internal static OdfDocument ConvertToFlatDocument(
        OdfDocument document)
    {
        RequireDocument(document);
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return document;
        }
        if (document.PackageKind != OdfPackageKind.Package)
        {
            throw new OdfEditException(
                "Only a package document can be converted to a flat document.");
        }
        RequireConsistentBody(document);
        RequireConversionMediaType(document);
        if (document.Kind == OdfDocumentKind.Formula
            || document.Kind == OdfDocumentKind.FormulaTemplate)
        {
            throw new OdfEditException(
                "A package formula document cannot be represented by the supported flat office document model.");
        }
        if (HasNonConvertiblePackageContent(document))
        {
            throw new OdfEditException(
                "The package contains resources that cannot be represented in a flat document.");
        }
        OdfNode[] flatNodes =
            OdfDocumentAssembly.DisassembleFlatDocument(document);
        SetFlatMediaType(flatNodes, document.MediaType);
        OdfDocument result = OdfDocumentAssembly.AssembleFlatDocument(
            flatNodes);
        OdfDocumentModel.ApplyContext(
            result,
            document.LoadOptions,
            document.SaveOptions,
            document.MediaType);
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            result,
            document.SaveOptions);
        OdfDocument converted = new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "converted.fodf",
            document.LoadOptions);
        OdfDocumentModel.RememberSaveOptions(
            converted,
            document.SaveOptions);
        return converted;
    }

    internal static OdfRepairAnalysis ProposeDocumentRepairs(
        OdfDocument document)
    {
        RequireDocument(document);
        return new OdfRepairService().ProposeRepairs(document);
    }

    internal static OdfRepairResult ApplyDocumentRepairs(
        OdfDocument document,
        OdfRepairProposal[] proposals)
    {
        RequireDocument(document);
        OdfRepairResult result = new OdfRepairService().ApplyRepairs(
            document,
            proposals);
        if (result.Changed)
        {
            RefreshDocument(document);
        }
        return result;
    }

    static OdfEditResult CheckPackageMutation(
        OdfDocument document,
        string path)
    {
        if (document.PackageKind != OdfPackageKind.Package
            || document.Package == null
            || document.Package.EntryIndex == null
            || document.PartTrees == null)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "A loaded editable package document is required.",
                path);
        }
        return Result(OdfEditStatus.Success, false, 0, "", path);
    }

    static OdfEditResult CheckResourceMutation(
        OdfDocument document,
        string path)
    {
        OdfEditResult packageCheck = CheckPackageMutation(document, path);
        if (packageCheck.Status != OdfEditStatus.Success)
        {
            return packageCheck;
        }
        if (IsReservedPackagePath(path))
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Document infrastructure and standard XML parts are not resources.",
                path);
        }
        return packageCheck;
    }

    static OdfPart PreparePart(OdfNode[] nodes, string path)
    {
        OdfNode[] copies = CloneNodes(nodes);
        int index = 0;
        while (index < copies.Length)
        {
            SetPartPath(copies[index], path);
            index = index + 1;
        }
        return OdfDocumentModel.CreateStandalonePart(
            copies,
            path,
            OdfMediaTypes.Xml);
    }

    static void SetPartTree(
        OdfDocument document,
        string path,
        OdfNode[] nodes,
        OdfPartRootMap? map)
    {
        OdfDocumentPartTrees trees = document.PartTrees
            ?? new OdfDocumentPartTrees();
        if (map == null)
        {
            throw new OdfEditException(
                "The added document part does not have a mapped root.");
        }
        if (path == OdfPartNames.Content)
        {
            trees.ContentNodes = nodes;
            trees.ContentRoot = map;
        }
        else if (path == OdfPartNames.Styles)
        {
            trees.StylesNodes = nodes;
            trees.StylesRoot = map;
        }
        else if (path == OdfPartNames.Metadata)
        {
            trees.MetadataNodes = nodes;
            trees.MetadataRoot = map;
        }
        else if (path == OdfPartNames.Settings)
        {
            trees.SettingsNodes = nodes;
            trees.SettingsRoot = map;
        }
        document.PartTrees = trees;
    }

    static void ClearPartTree(OdfDocument document, string path)
    {
        OdfDocumentPartTrees trees = document.PartTrees
            ?? new OdfDocumentPartTrees();
        if (path == OdfPartNames.Styles)
        {
            trees.StylesNodes = [];
            trees.StylesRoot = null;
        }
        else if (path == OdfPartNames.Metadata)
        {
            trees.MetadataNodes = [];
            trees.MetadataRoot = null;
        }
        else if (path == OdfPartNames.Settings)
        {
            trees.SettingsNodes = [];
            trees.SettingsRoot = null;
        }
    }

    static void RefreshDocument(OdfDocument document)
    {
        OdfPackage? package = document.Package;
        if (package != null)
        {
            PackageApiModel.RefreshPackage(package);
            document.PackageEntries = package.Entries;
            document.Manifest = package.Manifest;
        }
        document.Nodes = CombinePartNodes(document.PartTrees);
        OdfDocumentModel.ApplyContext(
            document,
            document.LoadOptions,
            document.SaveOptions,
            document.MediaType);
    }

    static OdfNode[] CombinePartNodes(OdfDocumentPartTrees? trees)
    {
        if (trees == null)
        {
            return [];
        }
        if (trees.FlatRoot != null)
        {
            return CopyNodeReferences(trees.FlatNodes);
        }
        int length = trees.ContentNodes.Length
            + trees.StylesNodes.Length
            + trees.MetadataNodes.Length
            + trees.SettingsNodes.Length;
        OdfNode[] result = new OdfNode[length];
        int destination = 0;
        destination = AppendNodeReferences(
            trees.ContentNodes,
            result,
            destination);
        destination = AppendNodeReferences(
            trees.StylesNodes,
            result,
            destination);
        destination = AppendNodeReferences(
            trees.MetadataNodes,
            result,
            destination);
        AppendNodeReferences(
            trees.SettingsNodes,
            result,
            destination);
        return result;
    }

    static int AppendNodeReferences(
        OdfNode[] source,
        OdfNode[] target,
        int destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[destination] = source[index];
            destination = destination + 1;
            index = index + 1;
        }
        return destination;
    }

    static OdfNode[] CopyNodeReferences(OdfNode[] source)
    {
        OdfNode[] result = new OdfNode[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static OdfNode[] CloneNodes(OdfNode[] source)
    {
        OdfNode[] result = new OdfNode[source.Length];
        OdfTreeEditor editor = new OdfTreeEditor([]);
        int index = 0;
        while (index < source.Length)
        {
            result[index] = editor.CloneSubtree(source[index]);
            index = index + 1;
        }
        return result;
    }

    static void SetPartPath(OdfNode node, string path)
    {
        node.Location.PartPath = path;
        int index = 0;
        while (index < node.Attributes.Length)
        {
            node.Attributes[index].Location.PartPath = path;
            index = index + 1;
        }
        index = 0;
        while (index < node.NamespaceDeclarations.Length)
        {
            node.NamespaceDeclarations[index].Location.PartPath = path;
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            SetPartPath(node.Children[index], path);
            index = index + 1;
        }
    }

    static OdfPart? FindPart(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].Path == path)
            {
                return document.Parts[index];
            }
            index = index + 1;
        }
        return null;
    }

    static EditablePackageEntry? FindPackageEntry(
        OdfDocument document,
        string path)
    {
        if (document.Package == null
            || document.Package.EntryIndex == null)
        {
            return null;
        }
        EditablePackageEntry[] entries = document.Package.EntryIndex
            .GetEditableEntries();
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].Entry.Path == path)
            {
                return entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void SetManifestMediaType(
        OdfManifest? manifest,
        string path,
        string mediaType)
    {
        if (manifest == null)
        {
            return;
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                manifest.Entries[index].MediaType = mediaType;
                return;
            }
            index = index + 1;
        }
    }

    static string ManifestMediaType(OdfManifest? manifest, string path)
    {
        if (manifest == null)
        {
            return "";
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return manifest.Entries[index].MediaType;
            }
            index = index + 1;
        }
        return "";
    }

    static void SynchronizeManifest(OdfPackage package)
    {
        if (package.Manifest == null)
        {
            return;
        }
        OdfEditResult result = new OdfManifestService()
            .SynchronizePackageManifest(package);
        if (result.Status != OdfEditStatus.Success
            && result.Status != OdfEditStatus.NoChange)
        {
            throw new OdfEditException(
                "The package manifest could not be synchronized.");
        }
    }

    static OdfDocumentKind DetectBodyKind(OdfDocument document)
    {
        if (document.Kind == OdfDocumentKind.Formula
            || document.Kind == OdfDocumentKind.FormulaTemplate)
        {
            return document.Kind;
        }
        OdfNode? body = document.Body;
        if (body == null)
        {
            return OdfDocumentKind.Unknown;
        }
        int index = 0;
        while (index < body.Children.Length)
        {
            OdfNode child = body.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Office)
            {
                return KindForBodyName(child.Name.LocalName);
            }
            index = index + 1;
        }
        return OdfDocumentKind.Unknown;
    }

    static OdfDocumentKind KindForBodyName(string name)
    {
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

    static bool KindsMatch(
        OdfDocumentKind actualKind,
        OdfDocumentKind expectedKind)
    {
        if (actualKind == OdfDocumentKind.Text)
        {
            return expectedKind == OdfDocumentKind.Text
                || expectedKind == OdfDocumentKind.TextTemplate
                || expectedKind == OdfDocumentKind.TextMaster
                || expectedKind == OdfDocumentKind.TextMasterTemplate;
        }
        if (actualKind == OdfDocumentKind.Spreadsheet)
        {
            return expectedKind == OdfDocumentKind.Spreadsheet
                || expectedKind == OdfDocumentKind.SpreadsheetTemplate;
        }
        if (actualKind == OdfDocumentKind.Presentation)
        {
            return expectedKind == OdfDocumentKind.Presentation
                || expectedKind == OdfDocumentKind.PresentationTemplate;
        }
        if (actualKind == OdfDocumentKind.Drawing)
        {
            return expectedKind == OdfDocumentKind.Drawing
                || expectedKind == OdfDocumentKind.DrawingTemplate;
        }
        if (actualKind == OdfDocumentKind.Chart)
        {
            return expectedKind == OdfDocumentKind.Chart
                || expectedKind == OdfDocumentKind.ChartTemplate;
        }
        if (actualKind == OdfDocumentKind.Image)
        {
            return expectedKind == OdfDocumentKind.Image
                || expectedKind == OdfDocumentKind.ImageTemplate;
        }
        if (actualKind == OdfDocumentKind.Formula
            || actualKind == OdfDocumentKind.FormulaTemplate)
        {
            return expectedKind == OdfDocumentKind.Formula
                || expectedKind == OdfDocumentKind.FormulaTemplate;
        }
        return actualKind == expectedKind;
    }

    static void RequireConsistentBody(OdfDocument document)
    {
        OdfDocumentKind declared = MimetypeRules.GetDocumentKind(
            document.MediaType);
        if (declared == OdfDocumentKind.Unknown)
        {
            return;
        }
        OdfEditResult check = CheckBodyKind(document, declared);
        if (check.Status != OdfEditStatus.Success)
        {
            throw new OdfEditException(
                "The declared media type conflicts with the document body kind.");
        }
    }

    static void RequireConversionMediaType(OdfDocument document)
    {
        OdfMediaTypeValue value = OdfMediaTypeRules.ParseMediaType(
            document.MediaType);
        if (!value.IsValid
            || MimetypeRules.GetDocumentKind(document.MediaType)
                == OdfDocumentKind.Unknown)
        {
            throw new OdfEditException(
                "Conversion requires a recognized OpenDocument media type.");
        }
    }

    static bool HasNonConvertiblePackageContent(OdfDocument document)
    {
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            OdfPackageEntry entry = document.PackageEntries[index];
            if (!IsConvertiblePackagePath(entry.Path))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsConvertiblePackagePath(string path)
    {
        return path == OdfPartNames.Mimetype
            || path == OdfPartNames.Manifest
            || path == OdfPartNames.Content
            || path == OdfPartNames.Styles
            || path == OdfPartNames.Metadata
            || path == OdfPartNames.Settings;
    }

    static void SetFlatMediaType(OdfNode[] nodes, string mediaType)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            OdfNode node = nodes[index];
            if (node.Kind == OdfNodeKind.Element)
            {
                OdfQualifiedName name = new OdfQualifiedName();
                name.NamespaceName = OdfNamespaces.Office;
                name.LocalName = "mimetype";
                name.Prefix = OdfNamespaces.OfficePrefix;
                new OdfTreeEditor(nodes).SetAttribute(
                    node,
                    name,
                    mediaType);
                return;
            }
            index = index + 1;
        }
    }

    static bool IsStandardPartPath(string path)
    {
        return path == OdfPartNames.Content
            || path == OdfPartNames.Styles
            || path == OdfPartNames.Metadata
            || path == OdfPartNames.Settings;
    }

    static bool IsReservedPackagePath(string path)
    {
        return IsStandardPartPath(path)
            || path == OdfPartNames.Mimetype
            || path == OdfPartNames.Manifest
            || path.StartsWith(
                "META-INF/",
                StringComparison.Ordinal);
    }

    static string BodyPartPath(OdfDocument document)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        return OdfPartNames.Content;
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfEditException("A document is required.");
        }
    }

    static void RequirePathInput(OdfDocument document, string path)
    {
        RequireDocument(document);
        if (path == null || path.Length == 0)
        {
            throw new OdfEditException(
                "A non-empty package-relative path is required.");
        }
    }

    static void RequirePartInput(
        OdfDocument document,
        string path,
        OdfNode[] nodes,
        string mediaType)
    {
        RequirePathInput(document, path);
        if (nodes == null || mediaType == null)
        {
            throw new OdfEditException(
                "Document part nodes and a media type are required.");
        }
    }

    static void RequireResourceInput(
        OdfDocument document,
        string path,
        byte[] data,
        string mediaType)
    {
        RequirePathInput(document, path);
        if (data == null || mediaType == null)
        {
            throw new OdfEditException(
                "Resource data and a media type are required.");
        }
    }

    static OdfEditResult Result(
        OdfEditStatus status,
        bool changed,
        int affectedCount,
        string message,
        string path)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affectedCount;
        result.Message = message;
        result.Location.PartPath = path;
        return result;
    }
}
