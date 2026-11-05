using System;
using System.Collections.Generic;

namespace odf;

internal class PresentationDependencyCopy
{
    internal string Kind = "";
    internal string SourceName = "";
    internal string TargetName = "";
    internal string SourcePartPath = "";
    internal string TargetPartPath = "";
    internal OdfNode SourceNode = new OdfNode();
    internal OdfNode TargetNode = new OdfNode();
    internal OdfNode TargetParent = new OdfNode();
}

internal class PresentationImportContext
{
    internal OdfDocument SourceDocument = new OdfDocument();
    internal OdfDocument TargetDocument = new OdfDocument();
    internal List<PresentationDependencyCopy> Dependencies =
        new List<PresentationDependencyCopy>();
    internal List<OdfNode> SourceResourceNodes = new List<OdfNode>();
    internal List<OdfNode> TargetResourceNodes = new List<OdfNode>();
    internal List<string> SourceResourceParts = new List<string>();
    internal List<string> TargetResourceParts = new List<string>();
    internal OdfNode? SourceHandout;
    internal OdfNode? TargetHandout;
    internal OdfNode? TargetHandoutParent;
}

internal static class PresentationImportModel
{
    internal static OdfSlide ImportSlide(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfSlide sourceSlide,
        int insertionIndex)
    {
        RequireImportDocuments(sourceDocument, targetDocument);
        PresentationSlideEditingModel.RequireOwnedSlide(
            sourceDocument,
            sourceSlide);
        if (insertionIndex < 0
            || insertionIndex > targetDocument.Slides.Length)
        {
            throw new OdfEditException(
                "The imported slide index is outside the target presentation.");
        }
        SynchronizeImportDocument(sourceDocument);
        SynchronizeImportDocument(targetDocument);
        PresentationImportContext context = CreateContext(
            sourceDocument,
            targetDocument);
        OdfNode targetPresentation =
            PresentationSlideEditingModel.RequirePresentation(
                targetDocument);
        OdfNode sourceNode = sourceSlide.Node ?? new OdfNode();
        OdfNode clone = CloneForPart(
            sourceNode,
            targetPresentation.Location.PartPath);
        AddResourcePair(
            context,
            sourceNode,
            clone,
            sourceNode.Location.PartPath,
            targetPresentation.Location.PartPath);
        RewriteDependencies(sourceNode, clone, context);
        CopyPackageResources(context);
        AppendDependencies(context);
        DrawingDocumentModel.LoadDrawings(targetDocument);
        DrawingEditingModel.PrepareCopiedIdentifiers(
            targetDocument,
            clone);
        EnsureNodeNamespaces(targetDocument, clone);
        int childIndex =
            PresentationSlideEditingModel.SlideChildInsertionIndex(
                targetPresentation,
                insertionIndex);
        PresentationSlideEditingModel.InsertChild(
            targetPresentation,
            childIndex,
            clone);
        RefreshImportedPresentation(targetDocument);
        return PresentationSlideEditingModel.RequireSlideByNode(
            targetDocument,
            clone);
    }

    internal static OdfSlide[] MergePresentation(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        int insertionIndex)
    {
        RequireImportDocuments(sourceDocument, targetDocument);
        if (insertionIndex < 0
            || insertionIndex > targetDocument.Slides.Length)
        {
            throw new OdfEditException(
                "The presentation merge index is outside the target presentation.");
        }
        SynchronizeImportDocument(sourceDocument);
        SynchronizeImportDocument(targetDocument);
        PresentationImportContext context = CreateContext(
            sourceDocument,
            targetDocument);
        OdfNode targetPresentation =
            PresentationSlideEditingModel.RequirePresentation(
                targetDocument);
        OdfNode[] clones = new OdfNode[sourceDocument.Slides.Length];
        int index = 0;
        while (index < sourceDocument.Slides.Length)
        {
            OdfNode sourceNode = sourceDocument.Slides[index].Node
                ?? new OdfNode();
            OdfNode clone = CloneForPart(
                sourceNode,
                targetPresentation.Location.PartPath);
            clones[index] = clone;
            AddResourcePair(
                context,
                sourceNode,
                clone,
                sourceNode.Location.PartPath,
                targetPresentation.Location.PartPath);
            RewriteDependencies(sourceNode, clone, context);
            index = index + 1;
        }
        AddHandoutForMerge(context);
        CopyPackageResources(context);
        AppendDependencies(context);
        AppendHandout(context);
        DrawingDocumentModel.LoadDrawings(targetDocument);
        index = 0;
        while (index < clones.Length)
        {
            DrawingEditingModel.PrepareCopiedIdentifiers(
                targetDocument,
                clones[index]);
            EnsureNodeNamespaces(targetDocument, clones[index]);
            int childIndex =
                PresentationSlideEditingModel.SlideChildInsertionIndex(
                    targetPresentation,
                    insertionIndex + index);
            PresentationSlideEditingModel.InsertChild(
                targetPresentation,
                childIndex,
                clones[index]);
            DrawingDocumentModel.LoadDrawings(targetDocument);
            index = index + 1;
        }
        RefreshImportedPresentation(targetDocument);
        OdfSlide[] result = new OdfSlide[clones.Length];
        index = 0;
        while (index < clones.Length)
        {
            result[index] =
                PresentationSlideEditingModel.RequireSlideByNode(
                    targetDocument,
                    clones[index]);
            index = index + 1;
        }
        return result;
    }

    static PresentationImportContext CreateContext(
        OdfDocument sourceDocument,
        OdfDocument targetDocument)
    {
        PresentationImportContext result = new PresentationImportContext();
        result.SourceDocument = sourceDocument;
        result.TargetDocument = targetDocument;
        return result;
    }

    static void SynchronizeImportDocument(OdfDocument document)
    {
        MasterTextDocumentModel.SynchronizeMasterText(document);
        TextFlowDocumentModel.SynchronizeTextFlows(document);
        FontFaceDocumentModel.SynchronizeFontFaces(document);
        StyleDocumentModel.SynchronizeStyles(document);
        DataStyleDocumentModel.SynchronizeDataStyles(document);
        ListStyleDocumentModel.SynchronizeListStyles(document);
        StyleResourceDocumentModel.SynchronizeStyleResources(document);
        PageLayoutDocumentModel.SynchronizePageLayouts(document);
        PresentationDocumentModel.SynchronizePresentation(document);
        TextDocumentModel.SynchronizeTextBlocks(document);
        ImageResourceManager.SynchronizeImages(document);
        MediaResourceManager.SynchronizeMediaResources(document);
        EmbeddedContentManager.SynchronizeEmbeddedContent(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
    }

    static void RefreshImportedPresentation(OdfDocument document)
    {
        FontFaceDocumentModel.LoadFontFaces(document);
        StyleDocumentModel.LoadStyles(document);
        DataStyleDocumentModel.LoadDataStyles(document);
        ListStyleDocumentModel.LoadListStyles(document);
        StyleResourceDocumentModel.LoadStyleResources(document);
        PageLayoutDocumentModel.LoadPageLayouts(document);
        PresentationSlideEditingModel.RefreshPresentation(document);
    }

    static void RewriteDependencies(
        OdfNode source,
        OdfNode target,
        PresentationImportContext context)
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
            string kind = ReferenceKind(source, sourceAttribute);
            if (kind.Length > 0 && sourceAttribute.Value.Length > 0)
            {
                string mapped = EnsureDependency(
                    context,
                    kind,
                    sourceAttribute.Value);
                if (mapped.Length > 0)
                {
                    target.Attributes[index].Value = mapped;
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
            RewriteDependencies(
                source.Children[index],
                target.Children[index],
                context);
            index = index + 1;
        }
    }

    static string EnsureDependency(
        PresentationImportContext context,
        string kind,
        string sourceName)
    {
        PresentationDependencyCopy? existing = FindDependency(
            context,
            kind,
            sourceName);
        if (existing != null)
        {
            return existing.TargetName;
        }
        OdfNode? sourceNode = FindDefinition(
            context.SourceDocument,
            kind,
            sourceName);
        if (sourceNode == null)
        {
            return sourceName;
        }
        PresentationDependencyCopy copy = new PresentationDependencyCopy();
        copy.Kind = kind;
        copy.SourceName = sourceName;
        copy.TargetName = UniqueDependencyName(
            context,
            kind,
            sourceName);
        copy.SourceNode = sourceNode;
        copy.SourcePartPath = SourcePartPath(
            context.SourceDocument,
            sourceNode);
        copy.TargetParent = ResolveTargetParent(
            context.TargetDocument,
            sourceNode,
            kind);
        copy.TargetPartPath = copy.TargetParent.Location.PartPath;
        copy.TargetNode = CloneForPart(
            sourceNode,
            copy.TargetPartPath);
        WriteDefinitionName(
            copy.TargetNode,
            kind,
            copy.TargetName);
        context.Dependencies.Add(copy);
        AddResourcePair(
            context,
            copy.SourceNode,
            copy.TargetNode,
            copy.SourcePartPath,
            copy.TargetPartPath);
        RewriteDependencies(
            copy.SourceNode,
            copy.TargetNode,
            context);
        return copy.TargetName;
    }

    static PresentationDependencyCopy? FindDependency(
        PresentationImportContext context,
        string kind,
        string sourceName)
    {
        int index = 0;
        while (index < context.Dependencies.Count)
        {
            PresentationDependencyCopy candidate =
                context.Dependencies[index];
            if (candidate.Kind == kind
                && candidate.SourceName == sourceName)
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    static string UniqueDependencyName(
        PresentationImportContext context,
        string kind,
        string requested)
    {
        if (!TargetDefinitionExists(context, kind, requested))
        {
            return requested;
        }
        int number = 2;
        string candidate = requested + " Copy 2";
        while (TargetDefinitionExists(context, kind, candidate))
        {
            number = number + 1;
            candidate = requested + " Copy " + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    static bool TargetDefinitionExists(
        PresentationImportContext context,
        string kind,
        string name)
    {
        if (FindDefinition(context.TargetDocument, kind, name) != null)
        {
            return true;
        }
        int index = 0;
        while (index < context.Dependencies.Count)
        {
            PresentationDependencyCopy candidate =
                context.Dependencies[index];
            if (candidate.Kind == kind && candidate.TargetName == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode? FindDefinition(
        OdfDocument document,
        string kind,
        string name)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] roots = document.Parts[partIndex].Nodes;
            int index = 0;
            while (index < roots.Length)
            {
                OdfNode? result = FindDefinitionInNode(
                    roots[index],
                    kind,
                    name);
                if (result != null)
                {
                    return result;
                }
                index = index + 1;
            }
            partIndex = partIndex + 1;
        }
        return null;
    }

    static OdfNode? FindDefinitionInNode(
        OdfNode node,
        string kind,
        string name)
    {
        if (DefinitionKind(node) == kind && DefinitionName(node) == name)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDefinitionInNode(
                node.Children[index],
                kind,
                name);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static string DefinitionKind(OdfNode node)
    {
        if (node.Name == null)
        {
            return "";
        }
        string namespaceName = node.Name.NamespaceName;
        string localName = node.Name.LocalName;
        if (namespaceName == OdfNamespaces.Style && localName == "style")
        {
            return "style:" + Read(
                node,
                OdfNamespaces.Style,
                "family");
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "master-page")
        {
            return "master-page";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "page-layout")
        {
            return "page-layout";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "presentation-page-layout")
        {
            return "presentation-layout";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "font-face")
        {
            return "font-face";
        }
        if (namespaceName == OdfNamespaces.Number
            && Read(node, OdfNamespaces.Style, "name").Length > 0)
        {
            return "data-style";
        }
        if (namespaceName == OdfNamespaces.Text
            && (localName == "list-style"
                || localName == "outline-style"))
        {
            return "list-style";
        }
        if (namespaceName == OdfNamespaces.Presentation
            && (localName == "header-decl"
                || localName == "footer-decl"
                || localName == "date-time-decl"))
        {
            return "declaration:" + localName;
        }
        if (namespaceName == OdfNamespaces.Drawing
            && IsNamedResourceElement(localName))
        {
            return "resource:" + localName;
        }
        return "";
    }

    static string DefinitionName(OdfNode node)
    {
        string kind = DefinitionKind(node);
        if (kind.StartsWith(
            "declaration:",
            global::System.StringComparison.Ordinal))
        {
            return Read(node, OdfNamespaces.Presentation, "name");
        }
        if (kind.StartsWith(
            "resource:",
            global::System.StringComparison.Ordinal))
        {
            return Read(node, OdfNamespaces.Drawing, "name");
        }
        return Read(node, OdfNamespaces.Style, "name");
    }

    static void WriteDefinitionName(
        OdfNode node,
        string kind,
        string name)
    {
        if (kind.StartsWith(
            "declaration:",
            global::System.StringComparison.Ordinal))
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Presentation,
                "name",
                OdfNamespaces.PresentationPrefix,
                name);
        }
        else if (kind.StartsWith(
            "resource:",
            global::System.StringComparison.Ordinal))
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Drawing,
                "name",
                OdfNamespaces.DrawingPrefix,
                name);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Style,
                "name",
                OdfNamespaces.StylePrefix,
                name);
        }
    }

    static string ReferenceKind(OdfNode node, OdfAttribute attribute)
    {
        string namespaceName = attribute.Name.NamespaceName;
        string localName = attribute.Name.LocalName;
        if (namespaceName == OdfNamespaces.Drawing
            && localName == "master-page-name")
        {
            return "master-page";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "master-page-name")
        {
            return "master-page";
        }
        if (namespaceName == OdfNamespaces.Presentation
            && localName == "presentation-page-layout-name")
        {
            return "presentation-layout";
        }
        if (namespaceName == OdfNamespaces.Presentation
            && localName == "use-header-name")
        {
            return "declaration:header-decl";
        }
        if (namespaceName == OdfNamespaces.Presentation
            && localName == "use-footer-name")
        {
            return "declaration:footer-decl";
        }
        if (namespaceName == OdfNamespaces.Presentation
            && localName == "use-date-time-name")
        {
            return "declaration:date-time-decl";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "page-layout-name")
        {
            return "page-layout";
        }
        if (namespaceName == OdfNamespaces.Style
            && localName == "list-style-name")
        {
            return "list-style";
        }
        if (namespaceName == OdfNamespaces.Style
            && (localName == "data-style-name"
                || localName == "apply-style-name"))
        {
            return "data-style";
        }
        if (namespaceName == OdfNamespaces.Style
            && (localName == "font-name"
                || localName == "font-name-asian"
                || localName == "font-name-complex"))
        {
            return "font-face";
        }
        if (namespaceName == OdfNamespaces.Style
            && (localName == "parent-style-name"
                || localName == "next-style-name"))
        {
            string definition = ContainingDefinitionKind(node);
            return definition == "master-page"
                ? "master-page"
                : definition;
        }
        if (namespaceName == OdfNamespaces.Drawing
            && localName == "style-name")
        {
            return DrawingStyleReferenceKind(node);
        }
        if (namespaceName == OdfNamespaces.Presentation
            && localName == "style-name")
        {
            return "style:presentation";
        }
        if (namespaceName == OdfNamespaces.Drawing
            && localName == "text-style-name")
        {
            return "style:paragraph";
        }
        if (namespaceName == OdfNamespaces.Text
            && localName == "style-name")
        {
            return TextStyleReferenceKind(node);
        }
        if (namespaceName == OdfNamespaces.Table
            && localName == "style-name")
        {
            return TableStyleReferenceKind(node);
        }
        if (namespaceName == OdfNamespaces.Chart
            && localName == "style-name")
        {
            return "style:chart";
        }
        if (namespaceName == OdfNamespaces.Drawing)
        {
            return DrawingResourceReferenceKind(localName);
        }
        return "";
    }

    static string ContainingDefinitionKind(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            string kind = DefinitionKind(current);
            if (kind.Length > 0)
            {
                return kind;
            }
            current = current.Parent;
        }
        return "";
    }

    static string DrawingStyleReferenceKind(OdfNode node)
    {
        if (node.Name != null
            && (node.Name.NamespaceName == OdfNamespaces.Drawing
                && node.Name.LocalName == "page"
                || node.Name.NamespaceName == OdfNamespaces.Presentation
                && (node.Name.LocalName == "notes"
                    || node.Name.LocalName == "handout-master")
                || node.Name.NamespaceName == OdfNamespaces.Style
                && node.Name.LocalName == "master-page"))
        {
            return "style:drawing-page";
        }
        return "style:graphic";
    }

    static string TextStyleReferenceKind(OdfNode node)
    {
        if (node.Name == null)
        {
            return "style:text";
        }
        string localName = node.Name.LocalName;
        if (localName == "p" || localName == "h"
            || localName == "numbered-paragraph")
        {
            return "style:paragraph";
        }
        if (localName == "section")
        {
            return "style:section";
        }
        if (localName == "list")
        {
            return "list-style";
        }
        if (localName == "ruby")
        {
            return "style:ruby";
        }
        return "style:text";
    }

    static string TableStyleReferenceKind(OdfNode node)
    {
        if (node.Name == null)
        {
            return "style:table";
        }
        string localName = node.Name.LocalName;
        if (localName == "table-row")
        {
            return "style:table-row";
        }
        if (localName == "table-column")
        {
            return "style:table-column";
        }
        if (localName == "table-cell"
            || localName == "covered-table-cell")
        {
            return "style:table-cell";
        }
        return "style:table";
    }

    static string DrawingResourceReferenceKind(string localName)
    {
        if (localName == "fill-gradient-name")
        {
            return "resource:gradient";
        }
        if (localName == "fill-hatch-name")
        {
            return "resource:hatch";
        }
        if (localName == "fill-image-name")
        {
            return "resource:fill-image";
        }
        if (localName == "opacity-name")
        {
            return "resource:opacity";
        }
        if (localName == "stroke-dash")
        {
            return "resource:stroke-dash";
        }
        if (localName == "marker-start" || localName == "marker-end")
        {
            return "resource:marker";
        }
        return "";
    }

    static bool IsNamedResourceElement(string localName)
    {
        return localName == "gradient"
            || localName == "hatch"
            || localName == "fill-image"
            || localName == "opacity"
            || localName == "stroke-dash"
            || localName == "marker";
    }

    static OdfNode ResolveTargetParent(
        OdfDocument targetDocument,
        OdfNode sourceNode,
        string kind)
    {
        if (kind.StartsWith(
            "declaration:",
            global::System.StringComparison.Ordinal))
        {
            return PresentationSlideEditingModel.RequirePresentation(
                targetDocument);
        }
        if (kind == "master-page")
        {
            return EnsureOfficeSection(
                targetDocument,
                "master-styles",
                false);
        }
        if (kind == "font-face")
        {
            return EnsureOfficeSection(
                targetDocument,
                "font-face-decls",
                false);
        }
        string sourceSection = ContainingOfficeSection(sourceNode);
        bool automatic = sourceSection == "automatic-styles";
        return EnsureOfficeSection(
            targetDocument,
            automatic ? "automatic-styles" : "styles",
            automatic);
    }

    static OdfNode EnsureOfficeSection(
        OdfDocument document,
        string localName,
        bool automatic)
    {
        string partPath = document.PackageKind == OdfPackageKind.Flat
            ? ""
            : automatic
                ? OdfPartNames.Content
                : OdfPartNames.Styles;
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null && partPath == OdfPartNames.Styles)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        if (part == null || part.Root == null)
        {
            throw new OdfEditException(
                "The target presentation has no XML part for imported styles.");
        }
        OdfNode? existing = Section(part, localName);
        if (existing != null)
        {
            return existing;
        }
        if (localName == "styles" || localName == "automatic-styles")
        {
            return StyleDocumentModel.CreateStyleSection(
                document,
                part,
                localName == "automatic-styles");
        }
        OdfNode section = new OdfNode();
        section.Kind = OdfNodeKind.Element;
        section.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Office,
            localName,
            OdfNamespaces.OfficePrefix);
        section.Location.PartPath = part.Path;
        StyleDocumentModel.InsertSection(
            part.Root,
            section,
            StyleDocumentModel.SectionRank(localName));
        StyleDocumentModel.RefreshPartMapping(document, part);
        return section;
    }

    static OdfNode? Section(OdfPart part, string localName)
    {
        if (localName == "styles")
        {
            return part.Styles;
        }
        if (localName == "automatic-styles")
        {
            return part.AutomaticStyles;
        }
        if (localName == "master-styles")
        {
            return part.MasterStyles;
        }
        if (localName == "font-face-decls")
        {
            return part.FontFaceDeclarations;
        }
        return null;
    }

    static string ContainingOfficeSection(OdfNode node)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (current.Name != null
                && current.Name.NamespaceName == OdfNamespaces.Office)
            {
                string localName = current.Name.LocalName;
                if (localName == "styles"
                    || localName == "automatic-styles"
                    || localName == "master-styles"
                    || localName == "font-face-decls")
                {
                    return localName;
                }
            }
            current = current.Parent;
        }
        return "";
    }

    static void AddHandoutForMerge(PresentationImportContext context)
    {
        if (context.SourceDocument.PresentationHandout == null
            || context.SourceDocument.PresentationHandout.Node == null
            || context.TargetDocument.PresentationHandout != null)
        {
            return;
        }
        OdfNode source = context.SourceDocument.PresentationHandout.Node;
        OdfNode parent = EnsureOfficeSection(
            context.TargetDocument,
            "master-styles",
            false);
        OdfNode target = CloneForPart(
            source,
            parent.Location.PartPath);
        context.SourceHandout = source;
        context.TargetHandout = target;
        context.TargetHandoutParent = parent;
        AddResourcePair(
            context,
            source,
            target,
            source.Location.PartPath,
            parent.Location.PartPath);
        RewriteDependencies(source, target, context);
    }

    static void AppendHandout(PresentationImportContext context)
    {
        if (context.TargetHandout == null
            || context.TargetHandoutParent == null)
        {
            return;
        }
        DrawingEditingModel.PrepareCopiedIdentifiers(
            context.TargetDocument,
            context.TargetHandout);
        EnsureNodeNamespaces(
            context.TargetDocument,
            context.TargetHandout);
        StyleDocumentModel.AppendNode(
            context.TargetHandoutParent,
            context.TargetHandout);
    }

    static void CopyPackageResources(PresentationImportContext context)
    {
        DrawingResourceManager.CopyNodeDependencies(
            context.SourceDocument,
            context.TargetDocument,
            context.SourceResourceNodes.ToArray(),
            context.TargetResourceNodes.ToArray(),
            context.SourceResourceParts.ToArray(),
            context.TargetResourceParts.ToArray());
    }

    static void AddResourcePair(
        PresentationImportContext context,
        OdfNode source,
        OdfNode target,
        string sourcePartPath,
        string targetPartPath)
    {
        context.SourceResourceNodes.Add(source);
        context.TargetResourceNodes.Add(target);
        context.SourceResourceParts.Add(sourcePartPath);
        context.TargetResourceParts.Add(targetPartPath);
    }

    static void AppendDependencies(PresentationImportContext context)
    {
        int index = 0;
        while (index < context.Dependencies.Count)
        {
            PresentationDependencyCopy copy = context.Dependencies[index];
            if (copy.Kind == "master-page")
            {
                DrawingEditingModel.PrepareCopiedIdentifiers(
                    context.TargetDocument,
                    copy.TargetNode);
            }
            EnsureNodeNamespaces(
                context.TargetDocument,
                copy.TargetNode);
            if (copy.Kind.StartsWith(
                "declaration:",
                global::System.StringComparison.Ordinal))
            {
                int childIndex =
                    PresentationSlideEditingModel.SlideChildInsertionIndex(
                        copy.TargetParent,
                        0);
                PresentationSlideEditingModel.InsertChild(
                    copy.TargetParent,
                    childIndex,
                    copy.TargetNode);
            }
            else
            {
                StyleDocumentModel.AppendNode(
                    copy.TargetParent,
                    copy.TargetNode);
            }
            index = index + 1;
        }
    }

    static OdfNode CloneForPart(OdfNode source, string partPath)
    {
        OdfNode result = new OdfTreeEditor([]).CloneSubtree(source);
        PresentationSlideEditingModel.SetPartPath(result, partPath);
        return result;
    }

    static string SourcePartPath(OdfDocument document, OdfNode node)
    {
        if (node.Location.PartPath.Length > 0
            || document.PackageKind == OdfPackageKind.Flat)
        {
            return node.Location.PartPath;
        }
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfNode? root = document.Parts[index].Root;
            if (root != null && IsDescendantOrSelf(node, root))
            {
                return document.Parts[index].Path;
            }
            index = index + 1;
        }
        return "";
    }

    static bool IsDescendantOrSelf(OdfNode node, OdfNode root)
    {
        OdfNode? current = node;
        while (current != null)
        {
            if (current == root)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static void EnsureNodeNamespaces(
        OdfDocument document,
        OdfNode node)
    {
        OdfPart? part = StyleDocumentModel.FindPart(
            document,
            node.Location.PartPath);
        if (part == null || part.Root == null)
        {
            return;
        }
        AddNodeNamespaces(part.Root, node);
    }

    static void AddNodeNamespaces(OdfNode root, OdfNode node)
    {
        if (node.Name != null)
        {
            EnsureNamespace(
                root,
                node.Name.Prefix,
                node.Name.NamespaceName);
        }
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfQualifiedName name = node.Attributes[index].Name;
            EnsureNamespace(root, name.Prefix, name.NamespaceName);
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            AddNodeNamespaces(root, node.Children[index]);
            index = index + 1;
        }
    }

    static void EnsureNamespace(
        OdfNode root,
        string prefix,
        string namespaceName)
    {
        if (prefix.Length == 0 || prefix == "xml"
            || namespaceName.Length == 0)
        {
            return;
        }
        int index = 0;
        while (index < root.NamespaceDeclarations.Length)
        {
            OdfNamespaceDeclaration declaration =
                root.NamespaceDeclarations[index];
            if (declaration.Prefix == prefix
                && declaration.NamespaceName == namespaceName)
            {
                return;
            }
            index = index + 1;
        }
        OdfNamespaceDeclaration added = new OdfNamespaceDeclaration();
        added.Prefix = prefix;
        added.NamespaceName = namespaceName;
        added.Location.PartPath = root.Location.PartPath;
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[
                root.NamespaceDeclarations.Length + 1];
        Array.Copy(
            root.NamespaceDeclarations,
            declarations,
            root.NamespaceDeclarations.Length);
        declarations[declarations.Length - 1] = added;
        root.NamespaceDeclarations = declarations;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static void RequireImportDocuments(
        OdfDocument sourceDocument,
        OdfDocument targetDocument)
    {
        if (sourceDocument == null || targetDocument == null
            || sourceDocument == targetDocument)
        {
            throw new OdfEditException(
                "Distinct source and target presentation documents are required.");
        }
        if ((sourceDocument.Kind != OdfDocumentKind.Presentation
                && sourceDocument.Kind
                    != OdfDocumentKind.PresentationTemplate)
            || (targetDocument.Kind != OdfDocumentKind.Presentation
                && targetDocument.Kind
                    != OdfDocumentKind.PresentationTemplate))
        {
            throw new OdfEditException(
                "Presentation import requires presentation documents.");
        }
    }
}
