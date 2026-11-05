using System;
using System.Collections.Generic;

namespace odf;

internal class DrawingChartStyleCopy
{
    internal OdfStyle Source = new OdfStyle();
    internal OdfStyle Target = new OdfStyle();
}

internal static class DrawingResourceManager
{
    internal static void RefreshOrphans(OdfDocument document)
    {
        document.OrphanedResourcePaths = FindOrphanedResources(document);
    }

    internal static string[] FindOrphanedResources(OdfDocument document)
    {
        RequireDocument(document);
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return [];
        }
        List<string> referenced = new List<string>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            int nodeIndex = 0;
            while (nodeIndex < part.Nodes.Length)
            {
                CollectReferences(
                    part.Nodes[nodeIndex],
                    part.Path,
                    referenced);
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        EditablePackageEntry[] entries =
            document.Package.EntryIndex.GetEditableEntries();
        List<string> result = new List<string>();
        int index = 0;
        while (index < entries.Length)
        {
            string path = entries[index].Entry.Path;
            if (IsResourcePath(document, path)
                && !IsReferenced(path, referenced))
            {
                result.Add(path);
            }
            index = index + 1;
        }
        result.Sort(global::System.StringComparer.Ordinal);
        return result.ToArray();
    }

    internal static OdfEditResult CollectGarbage(OdfDocument document)
    {
        RequirePackage(document);
        string[] orphans = FindOrphanedResources(document);
        if (orphans.Length == 0)
        {
            RefreshOrphans(document);
            return Result(
                OdfEditStatus.NoChange,
                false,
                0,
                "The document has no orphaned package resources.",
                "");
        }
        Array.Sort(orphans, CompareRemovalPaths);
        OdfPackage package = document.Package ?? new OdfPackage();
        OdfPackageWriter writer = new OdfPackageWriter();
        int removed = 0;
        int index = 0;
        while (index < orphans.Length)
        {
            OdfEditResult edit = writer.RemovePackageEntry(
                package,
                orphans[index]);
            if (edit.Status == OdfEditStatus.Success)
            {
                removed = removed + 1;
            }
            else if (edit.Status != OdfEditStatus.Conflict)
            {
                throw new OdfEditException(
                    "Could not remove orphaned package resource '"
                    + orphans[index] + "'.");
            }
            index = index + 1;
        }
        RefreshPackageState(document);
        RefreshResourceModels(document);
        RefreshOrphans(document);
        return Result(
            removed == 0 ? OdfEditStatus.NoChange : OdfEditStatus.Success,
            removed > 0,
            removed,
            "Removed orphaned package resources.",
            "");
    }

    internal static OdfEditResult MovePackageResource(
        OdfDocument document,
        string sourcePath,
        string destinationPath)
    {
        RequirePackage(document);
        PackagePathValue source = PackagePathRules.CanonicalizeEntryPath(
            sourcePath);
        PackagePathValue destination = PackagePathRules.CanonicalizeEntryPath(
            destinationPath);
        if (!source.IsValid || !source.IsCanonical
            || !destination.IsValid || !destination.IsCanonical)
        {
            return Result(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Resource paths must be canonical package paths.",
                destinationPath);
        }
        if (sourcePath == destinationPath)
        {
            return Result(
                OdfEditStatus.NoChange,
                false,
                0,
                "The resource already has the requested path.",
                sourcePath);
        }
        OdfPackage package = document.Package ?? new OdfPackage();
        EditablePackageEntry[] entries = RequireEntries(document);
        List<string> sources = ResourceEntries(entries, sourcePath);
        if (sources.Count == 0)
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The package resource does not exist.",
                sourcePath);
        }
        string sourceRoot = ResourceRoot(sourcePath, sources);
        string destinationRoot = DirectoryDestination(
            sourcePath,
            destinationPath,
            sourceRoot);
        if (MoveHasCollision(
            entries,
            sources,
            sourceRoot,
            destinationRoot))
        {
            return Result(
                OdfEditStatus.Conflict,
                false,
                0,
                "The destination package resource already exists.",
                destinationPath);
        }
        OdfPackageWriter writer = new OdfPackageWriter();
        int moved = 0;
        int index = 0;
        while (index < sources.Count)
        {
            string target = MappedPath(
                sources[index],
                sourceRoot,
                destinationRoot);
            OdfEditResult edit = writer.MovePackageEntry(
                package,
                sources[index],
                target);
            if (edit.Status != OdfEditStatus.Success)
            {
                throw new OdfEditException(
                    "Could not move package resource '"
                    + sources[index] + "'.");
            }
            moved = moved + 1;
            index = index + 1;
        }
        UpdateReferences(
            document,
            sourceRoot,
            destinationRoot);
        RefreshPackageState(document);
        RefreshResourceModels(document);
        RefreshOrphans(document);
        return Result(
            OdfEditStatus.Success,
            true,
            moved,
            "Moved the package resource and updated its references.",
            destinationRoot);
    }

    internal static string[] CopyDrawingDependencies(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfDrawingObject sourceDrawing,
        OdfNode targetNode,
        string targetPartPath)
    {
        if (sourceDrawing == null || sourceDrawing.Node == null
            || targetNode == null)
        {
            throw new OdfEditException(
                "Source drawing content and its target clone are required.");
        }
        string[] copied = CopyNodeDependencies(
            sourceDocument,
            targetDocument,
            [sourceDrawing.Node],
            [targetNode],
            [sourceDrawing.PartPath],
            [targetPartPath]);
        CopyDrawingStyles(
            sourceDocument,
            targetDocument,
            sourceDrawing,
            targetNode);
        CopyChartStyles(
            sourceDocument,
            targetDocument,
            sourceDrawing.Node,
            targetNode,
            new List<DrawingChartStyleCopy>());
        return copied;
    }

    internal static string[] CopyNodeDependencies(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfNode[] sourceNodes,
        OdfNode[] targetNodes,
        string[] sourcePartPaths,
        string[] targetPartPaths)
    {
        if (sourceDocument == null || targetDocument == null
            || sourceNodes == null || targetNodes == null
            || sourcePartPaths == null || targetPartPaths == null
            || sourceNodes.Length != targetNodes.Length
            || sourceNodes.Length != sourcePartPaths.Length
            || sourceNodes.Length != targetPartPaths.Length)
        {
            throw new OdfEditException(
                "Aligned source and target dependency subtrees are required.");
        }
        List<ResourceCopyMapping> mappings = new List<ResourceCopyMapping>();
        List<string> references = new List<string>();
        int index = 0;
        while (index < sourceNodes.Length)
        {
            CollectReferences(
                sourceNodes[index],
                sourcePartPaths[index],
                references);
            index = index + 1;
        }
        if (references.Count == 0)
        {
            return [];
        }
        RequirePackage(sourceDocument);
        RequirePackage(targetDocument);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            CopyDependency(
                sourceDocument,
                targetDocument,
                references[referenceIndex],
                mappings);
            referenceIndex = referenceIndex + 1;
        }
        index = 0;
        while (index < sourceNodes.Length)
        {
            RewriteCopiedReferences(
                sourceNodes[index],
                targetNodes[index],
                sourcePartPaths[index],
                targetPartPaths[index],
                mappings);
            index = index + 1;
        }
        RefreshPackageState(targetDocument);
        RefreshOrphans(targetDocument);
        List<string> copied = new List<string>();
        index = 0;
        while (index < mappings.Count)
        {
            int pathIndex = 0;
            while (pathIndex < mappings[index].CopiedPaths.Length)
            {
                copied.Add(mappings[index].CopiedPaths[pathIndex]);
                pathIndex = pathIndex + 1;
            }
            index = index + 1;
        }
        return copied.ToArray();
    }

    static void CopyDependency(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        List<ResourceCopyMapping> mappings)
    {
        if (FindMapping(mappings, sourcePath) != null)
        {
            return;
        }
        EditablePackageEntry[] sourceEntries = RequireEntries(sourceDocument);
        List<string> paths = ResourceEntries(sourceEntries, sourcePath);
        if (paths.Count == 0)
        {
            return;
        }
        string sourceRoot = ResourceRoot(sourcePath, paths);
        string targetRoot = SelectCopyRoot(
            sourceDocument,
            targetDocument,
            sourceRoot,
            paths);
        ResourceCopyMapping mapping = new ResourceCopyMapping();
        mapping.SourceRoot = sourceRoot;
        mapping.TargetRoot = targetRoot;
        List<string> copied = new List<string>();
        OdfPackage targetPackage = targetDocument.Package ?? new OdfPackage();
        OdfPackageWriter writer = new OdfPackageWriter();
        int index = 0;
        while (index < paths.Count)
        {
            string targetPath = MappedPath(
                paths[index],
                sourceRoot,
                targetRoot);
            if (!HasEntry(targetDocument, targetPath))
            {
                EditablePackageEntry sourceEntry = FindEntry(
                    sourceEntries,
                    paths[index]);
                if (!sourceEntry.IsContentAvailable)
                {
                    throw new OdfEditException(
                        "Encrypted or unavailable drawing dependencies cannot be copied.");
                }
                OdfEditResult edit = writer.AddPackageEntry(
                    targetPackage,
                    targetPath,
                    CopyBytes(sourceEntry.Data),
                    sourceEntry.Entry.CompressionMethod);
                if (edit.Status != OdfEditStatus.Success)
                {
                    throw new OdfEditException(
                        "Could not copy drawing dependency '"
                        + paths[index] + "'.");
                }
                copied.Add(targetPath);
            }
            index = index + 1;
        }
        if (copied.Count > 0)
        {
            CopyResourceManifestMetadata(
                sourceDocument,
                targetDocument,
                sourceRoot,
                targetRoot,
                paths);
        }
        mapping.CopiedPaths = copied.ToArray();
        mappings.Add(mapping);
    }

    static void CopyResourceManifestMetadata(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourceRoot,
        string targetRoot,
        List<string> sourcePaths)
    {
        OdfManifest? sourceManifest = sourceDocument.Package?.Manifest;
        OdfManifest? targetManifest = targetDocument.Package?.Manifest;
        ZipEntryIndex? targetIndex = targetDocument.Package?.EntryIndex;
        if (sourceManifest == null || targetManifest == null
            || targetIndex == null)
        {
            return;
        }
        int index = 0;
        while (index < sourceManifest.Entries.Length)
        {
            OdfManifestEntry sourceEntry = sourceManifest.Entries[index];
            if (ContainsString(sourcePaths, sourceEntry.FullPath))
            {
                string targetPath = MappedPath(
                    sourceEntry.FullPath,
                    sourceRoot,
                    targetRoot);
                OdfManifestEntry? targetEntry = FindManifestEntry(
                    targetManifest,
                    targetPath);
                if (targetEntry != null)
                {
                    targetEntry.MediaType = sourceEntry.MediaType;
                    targetEntry.Version = sourceEntry.Version;
                    targetEntry.PreferredViewMode =
                        sourceEntry.PreferredViewMode;
                }
            }
            index = index + 1;
        }
        OdfEditResult repair = targetIndex.RepairManifestEntry(
            targetManifest);
        if (repair.Status != OdfEditStatus.Success
            && repair.Status != OdfEditStatus.NoChange)
        {
            throw new OdfEditException(
                "Could not preserve copied resource manifest metadata.");
        }
    }

    static OdfManifestEntry? FindManifestEntry(
        OdfManifest manifest,
        string path)
    {
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return manifest.Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    static string SelectCopyRoot(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourceRoot,
        List<string> sourcePaths)
    {
        if (DependenciesMatch(
            sourceDocument,
            targetDocument,
            sourceRoot,
            sourceRoot,
            sourcePaths))
        {
            return sourceRoot;
        }
        if (!ResourceRootExists(targetDocument, sourceRoot))
        {
            return sourceRoot;
        }
        int copyNumber = 2;
        string candidate = CopyPath(sourceRoot, copyNumber);
        while (ResourceRootExists(targetDocument, candidate))
        {
            copyNumber = copyNumber + 1;
            candidate = CopyPath(sourceRoot, copyNumber);
        }
        return candidate;
    }

    static bool DependenciesMatch(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourceRoot,
        string targetRoot,
        List<string> sourcePaths)
    {
        if (!ResourceRootExists(targetDocument, targetRoot))
        {
            return false;
        }
        EditablePackageEntry[] sourceEntries = RequireEntries(sourceDocument);
        EditablePackageEntry[] targetEntries = RequireEntries(targetDocument);
        int index = 0;
        while (index < sourcePaths.Count)
        {
            string targetPath = MappedPath(
                sourcePaths[index],
                sourceRoot,
                targetRoot);
            EditablePackageEntry? target = FindOptionalEntry(
                targetEntries,
                targetPath);
            if (target == null
                || !BytesMatch(
                    FindEntry(sourceEntries, sourcePaths[index]).Data,
                    target.Data))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void CopyDrawingStyles(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfDrawingObject drawing,
        OdfNode targetNode)
    {
        if (drawing.StyleName.Length > 0)
        {
            OdfStyle? source = FindStyle(
                sourceDocument,
                drawing.StyleName,
                drawing.StyleNamespace == OdfNamespaces.Presentation
                    ? OdfStyleFamily.Presentation
                    : OdfStyleFamily.Graphic);
            if (source != null)
            {
                OdfStyle copy = StyleMaintenanceModel.CopyStyleAcrossDocuments(
                    sourceDocument,
                    targetDocument,
                    source,
                    drawing.StyleName);
                WriteAttribute(
                    targetNode,
                    drawing.StyleNamespace == OdfNamespaces.Presentation
                        ? OdfNamespaces.Presentation
                        : OdfNamespaces.Drawing,
                    "style-name",
                    copy.Name);
            }
        }
        if (drawing.TextStyleName.Length > 0)
        {
            OdfStyle? source = FindStyle(
                sourceDocument,
                drawing.TextStyleName,
                OdfStyleFamily.Paragraph);
            if (source != null)
            {
                OdfStyle copy = StyleMaintenanceModel.CopyStyleAcrossDocuments(
                    sourceDocument,
                    targetDocument,
                    source,
                    drawing.TextStyleName);
                WriteAttribute(
                    targetNode,
                    OdfNamespaces.Drawing,
                    "text-style-name",
                    copy.Name);
            }
        }
    }

    static void CopyChartStyles(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfNode sourceNode,
        OdfNode targetNode,
        List<DrawingChartStyleCopy> copies)
    {
        int attributeCount = sourceNode.Attributes.Length;
        if (targetNode.Attributes.Length < attributeCount)
        {
            attributeCount = targetNode.Attributes.Length;
        }
        int index = 0;
        while (index < attributeCount)
        {
            OdfAttribute sourceAttribute = sourceNode.Attributes[index];
            if (sourceAttribute.Name.NamespaceName == OdfNamespaces.Chart
                && sourceAttribute.Name.LocalName == "style-name"
                && sourceAttribute.Value.Length > 0)
            {
                OdfStyle? sourceStyle = FindStyle(
                    sourceDocument,
                    sourceAttribute.Value,
                    OdfStyleFamily.Chart);
                if (sourceStyle != null)
                {
                    OdfStyle targetStyle = CopyChartStyle(
                        sourceDocument,
                        targetDocument,
                        sourceStyle,
                        copies);
                    targetNode.Attributes[index].Value = targetStyle.Name;
                }
            }
            index = index + 1;
        }
        int childCount = sourceNode.Children.Length;
        if (targetNode.Children.Length < childCount)
        {
            childCount = targetNode.Children.Length;
        }
        index = 0;
        while (index < childCount)
        {
            CopyChartStyles(
                sourceDocument,
                targetDocument,
                sourceNode.Children[index],
                targetNode.Children[index],
                copies);
            index = index + 1;
        }
    }

    static OdfStyle CopyChartStyle(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfStyle sourceStyle,
        List<DrawingChartStyleCopy> copies)
    {
        int index = 0;
        while (index < copies.Count)
        {
            if (copies[index].Source == sourceStyle)
            {
                return copies[index].Target;
            }
            index = index + 1;
        }
        OdfStyle targetStyle = StyleMaintenanceModel.CopyStyleAcrossDocuments(
            sourceDocument,
            targetDocument,
            sourceStyle,
            sourceStyle.Name);
        DrawingChartStyleCopy copy = new DrawingChartStyleCopy();
        copy.Source = sourceStyle;
        copy.Target = targetStyle;
        copies.Add(copy);
        return targetStyle;
    }

    static OdfStyle? FindStyle(
        OdfDocument document,
        string name,
        OdfStyleFamily family)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name && style.Family == family)
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static void UpdateReferences(
        OdfDocument document,
        string sourceRoot,
        string destinationRoot)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            int nodeIndex = 0;
            while (nodeIndex < part.Nodes.Length)
            {
                UpdateNodeReferences(
                    part.Nodes[nodeIndex],
                    part.Path,
                    sourceRoot,
                    destinationRoot);
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
    }

    static void UpdateNodeReferences(
        OdfNode node,
        string partPath,
        string sourceRoot,
        string destinationRoot)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsResourceReference(attribute))
            {
                string resolved = ResolvedResourcePath(
                    partPath,
                    attribute.Value);
                if (PathBelongsToRoot(resolved, sourceRoot))
                {
                    string suffix = ReferenceSuffix(attribute.Value);
                    string mapped = MappedPath(
                        resolved,
                        sourceRoot,
                        destinationRoot);
                    attribute.Value = RelativeReference(partPath, mapped)
                        + suffix;
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            UpdateNodeReferences(
                node.Children[index],
                partPath,
                sourceRoot,
                destinationRoot);
            index = index + 1;
        }
    }

    static void RewriteCopiedReferences(
        OdfNode source,
        OdfNode target,
        string sourcePartPath,
        string targetPartPath,
        List<ResourceCopyMapping> mappings)
    {
        int attributeCount = source.Attributes.Length;
        if (target.Attributes.Length < attributeCount)
        {
            attributeCount = target.Attributes.Length;
        }
        int index = 0;
        while (index < attributeCount)
        {
            OdfAttribute sourceAttribute = source.Attributes[index];
            OdfAttribute targetAttribute = target.Attributes[index];
            if (IsResourceReference(sourceAttribute))
            {
                string resolved = ResolvedResourcePath(
                    sourcePartPath,
                    sourceAttribute.Value);
                ResourceCopyMapping? mapping = FindMapping(
                    mappings,
                    resolved);
                if (mapping != null)
                {
                    string mapped = MappedPath(
                        resolved,
                        mapping.SourceRoot,
                        mapping.TargetRoot);
                    targetAttribute.Value = RelativeReference(
                        targetPartPath,
                        mapped) + ReferenceSuffix(sourceAttribute.Value);
                }
            }
            index = index + 1;
        }
        int childCount = source.Children.Length;
        if (target.Children.Length < childCount)
        {
            childCount = target.Children.Length;
        }
        index = 0;
        while (index < childCount)
        {
            RewriteCopiedReferences(
                source.Children[index],
                target.Children[index],
                sourcePartPath,
                targetPartPath,
                mappings);
            index = index + 1;
        }
    }

    static void CollectReferences(
        OdfNode node,
        string partPath,
        List<string> references)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (IsResourceReference(attribute))
            {
                string resolved = ResolvedResourcePath(
                    partPath,
                    attribute.Value);
                if (resolved.Length > 0
                    && !ContainsString(references, resolved))
                {
                    references.Add(resolved);
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            CollectReferences(node.Children[index], partPath, references);
            index = index + 1;
        }
    }

    static bool IsResourceReference(OdfAttribute attribute)
    {
        return attribute.Name.NamespaceName == OdfNamespaces.XLink
            && attribute.Name.LocalName == "href"
            && OdfIriResolver.IsPackageRelativeReference(attribute.Value);
    }

    static string ResolvedResourcePath(string partPath, string reference)
    {
        string resolved = PackagePathRules.ResolveRelativeReference(
            partPath,
            reference);
        int suffix = PathSuffixStart(resolved);
        string path = resolved.Substring(0, suffix);
        if (path.EndsWith("/", global::System.StringComparison.Ordinal))
        {
            path = path.Substring(0, path.Length - 1);
        }
        return path;
    }

    static string ReferenceSuffix(string reference)
    {
        int start = PathSuffixStart(reference);
        return reference.Substring(start);
    }

    static int PathSuffixStart(string value)
    {
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (query < 0)
        {
            return fragment < 0 ? value.Length : fragment;
        }
        if (fragment < 0)
        {
            return query;
        }
        return query < fragment ? query : fragment;
    }

    static string RelativeReference(string partPath, string targetPath)
    {
        int separator = partPath.LastIndexOf('/');
        string directory = separator < 0
            ? ""
            : partPath.Substring(0, separator);
        string[] baseSegments = directory.Length == 0
            ? []
            : directory.Split('/');
        string[] targetSegments = targetPath.Split('/');
        int common = 0;
        while (common < baseSegments.Length
            && common < targetSegments.Length
            && baseSegments[common] == targetSegments[common])
        {
            common = common + 1;
        }
        global::System.Text.StringBuilder result =
            new global::System.Text.StringBuilder();
        int index = common;
        while (index < baseSegments.Length)
        {
            result.Append("../");
            index = index + 1;
        }
        index = common;
        while (index < targetSegments.Length)
        {
            if (result.Length > 0
                && result[result.Length - 1] != '/')
            {
                result.Append('/');
            }
            result.Append(targetSegments[index]);
            index = index + 1;
        }
        return result.ToString();
    }

    static List<string> ResourceEntries(
        EditablePackageEntry[] entries,
        string requestedPath)
    {
        string root = requestedPath.EndsWith(
            "/",
            global::System.StringComparison.Ordinal)
            ? requestedPath.Substring(0, requestedPath.Length - 1)
            : requestedPath;
        List<string> result = new List<string>();
        int index = 0;
        while (index < entries.Length)
        {
            string path = entries[index].Entry.Path;
            if (path == root || path == root + "/"
                || path.StartsWith(
                    root + "/",
                    global::System.StringComparison.Ordinal))
            {
                result.Add(path);
            }
            index = index + 1;
        }
        result.Sort(global::System.StringComparer.Ordinal);
        return result;
    }

    static string ResourceRoot(string requestedPath, List<string> paths)
    {
        string root = requestedPath.EndsWith(
            "/",
            global::System.StringComparison.Ordinal)
            ? requestedPath.Substring(0, requestedPath.Length - 1)
            : requestedPath;
        if (paths.Count == 1 && paths[0] == root)
        {
            return root;
        }
        return root;
    }

    static string DirectoryDestination(
        string sourcePath,
        string destinationPath,
        string sourceRoot)
    {
        if (sourcePath.EndsWith(
            "/",
            global::System.StringComparison.Ordinal)
            || sourceRoot != sourcePath)
        {
            return destinationPath.EndsWith(
                "/",
                global::System.StringComparison.Ordinal)
                ? destinationPath.Substring(0, destinationPath.Length - 1)
                : destinationPath;
        }
        return destinationPath;
    }

    static bool MoveHasCollision(
        EditablePackageEntry[] entries,
        List<string> sourcePaths,
        string sourceRoot,
        string destinationRoot)
    {
        int index = 0;
        while (index < sourcePaths.Count)
        {
            string destination = MappedPath(
                sourcePaths[index],
                sourceRoot,
                destinationRoot);
            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                if (entries[entryIndex].Entry.Path == destination
                    && !ContainsString(
                        sourcePaths,
                        entries[entryIndex].Entry.Path))
                {
                    return true;
                }
                entryIndex = entryIndex + 1;
            }
            index = index + 1;
        }
        return false;
    }

    static string MappedPath(
        string path,
        string sourceRoot,
        string destinationRoot)
    {
        if (path == sourceRoot)
        {
            return destinationRoot;
        }
        return destinationRoot + path.Substring(sourceRoot.Length);
    }

    static bool PathBelongsToRoot(string path, string root)
    {
        return path == root
            || path.StartsWith(
                root + "/",
                global::System.StringComparison.Ordinal);
    }

    static ResourceCopyMapping? FindMapping(
        List<ResourceCopyMapping> mappings,
        string path)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (PathBelongsToRoot(path, mappings[index].SourceRoot))
            {
                return mappings[index];
            }
            index = index + 1;
        }
        return null;
    }

    static string CopyPath(string path, int copyNumber)
    {
        bool directory = path.EndsWith(
            "/",
            global::System.StringComparison.Ordinal);
        string value = directory
            ? path.Substring(0, path.Length - 1)
            : path;
        int separator = value.LastIndexOf('/');
        int dot = value.LastIndexOf('.');
        if (dot <= separator)
        {
            dot = value.Length;
        }
        string result = value.Substring(0, dot)
            + "-copy-" + copyNumber.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture)
            + value.Substring(dot);
        return directory ? result + "/" : result;
    }

    static bool ResourceRootExists(OdfDocument document, string root)
    {
        EditablePackageEntry[] entries = RequireEntries(document);
        int index = 0;
        while (index < entries.Length)
        {
            if (PathBelongsToRoot(entries[index].Entry.Path, root)
                || entries[index].Entry.Path == root + "/")
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasEntry(OdfDocument document, string path)
    {
        return FindOptionalEntry(RequireEntries(document), path) != null;
    }

    static EditablePackageEntry FindEntry(
        EditablePackageEntry[] entries,
        string path)
    {
        EditablePackageEntry? result = FindOptionalEntry(entries, path);
        if (result == null)
        {
            throw new OdfEditException(
                "The package dependency '" + path + "' is missing.");
        }
        return result;
    }

    static EditablePackageEntry? FindOptionalEntry(
        EditablePackageEntry[] entries,
        string path)
    {
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

    static bool IsReferenced(string path, List<string> references)
    {
        int index = 0;
        while (index < references.Count)
        {
            string reference = references[index];
            if (path == reference
                || path == reference + "/"
                || path.StartsWith(
                    reference + "/",
                    global::System.StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsResourcePath(OdfDocument document, string path)
    {
        if (path == OdfPartNames.Thumbnail
            || path.StartsWith(
                "META-INF/",
                global::System.StringComparison.Ordinal))
        {
            return false;
        }
        if (path.StartsWith(
                "Pictures/",
                global::System.StringComparison.Ordinal)
            || path.StartsWith(
                "Media/",
                global::System.StringComparison.Ordinal)
            || path.StartsWith(
                "Resources/",
                global::System.StringComparison.Ordinal)
            || path.StartsWith(
                "ObjectReplacements/",
                global::System.StringComparison.Ordinal))
        {
            return true;
        }
        int index = 0;
        while (index < document.Images.Length)
        {
            if (document.Images[index].PackagePath == path)
            {
                return true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.MediaResources.Length)
        {
            if (document.MediaResources[index].PackagePath == path)
            {
                return true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.EmbeddedObjects.Length)
        {
            string root = document.EmbeddedObjects[index].PackagePath;
            if (root.EndsWith(
                "/",
                global::System.StringComparison.Ordinal))
            {
                root = root.Substring(0, root.Length - 1);
            }
            if (root.Length > 0 && PathBelongsToRoot(path, root))
            {
                return true;
            }
            index = index + 1;
        }
        return IsManifestResource(document.Manifest, path);
    }

    static bool IsManifestResource(OdfManifest? manifest, string path)
    {
        if (manifest == null)
        {
            return false;
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            OdfManifestEntry entry = manifest.Entries[index];
            if (entry.FullPath == path)
            {
                string mediaType = entry.MediaType;
                return mediaType.StartsWith(
                        "image/",
                        global::System.StringComparison.OrdinalIgnoreCase)
                    || mediaType.StartsWith(
                        "audio/",
                        global::System.StringComparison.OrdinalIgnoreCase)
                    || mediaType.StartsWith(
                        "video/",
                        global::System.StringComparison.OrdinalIgnoreCase)
                    || mediaType == OdfMediaTypes.MathMl
                    || MimetypeRules.GetDocumentKind(mediaType)
                        != OdfDocumentKind.Unknown;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsString(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static int CompareRemovalPaths(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return right.Length.CompareTo(left.Length);
        }
        return global::System.StringComparer.Ordinal.Compare(left, right);
    }

    static void WriteAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
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
    }

    static EditablePackageEntry[] RequireEntries(OdfDocument document)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            throw new OdfEditException(
                "A materialized package entry index is required.");
        }
        return document.Package.EntryIndex.GetEditableEntries();
    }

    static void RefreshPackageState(OdfDocument document)
    {
        OdfPackage package = document.Package ?? new OdfPackage();
        PackageApiModel.RefreshPackage(package);
        document.PackageEntries = package.Entries;
        document.Manifest = package.Manifest;
    }

    static void RefreshResourceModels(OdfDocument document)
    {
        ImageResourceManager.LoadImages(document);
        MediaResourceManager.LoadMediaResources(document);
        EmbeddedContentManager.LoadEmbeddedContent(document);
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException("A document is required.");
        }
    }

    static void RequirePackage(OdfDocument document)
    {
        RequireDocument(document);
        if (document.PackageKind != OdfPackageKind.Package
            || document.Package == null
            || document.Package.EntryIndex == null)
        {
            throw new OdfEditException(
                "A package document is required for drawing resources.");
        }
    }

    static OdfEditResult Result(
        OdfEditStatus status,
        bool changed,
        int count,
        string message,
        string target)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = count;
        result.Message = message;
        result.Location.PartPath = target;
        return result;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static bool BytesMatch(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
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
}

internal class ResourceCopyMapping
{
    internal string SourceRoot = "";
    internal string TargetRoot = "";
    internal string[] CopiedPaths = [];
}
