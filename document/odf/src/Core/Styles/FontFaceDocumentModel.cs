using System;
using System.Collections.Generic;

namespace odf;

internal class FontFaceDocumentState
{
    internal OdfNode[] OriginalNodes = [];
}

internal static class FontFaceDocumentModel
{
    internal static void LoadFontFaces(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for font-face materialization.");
        }
        List<OdfFontFace> faces = new List<OdfFontFace>();
        List<OdfNode> nodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            LoadDeclarationSection(
                document,
                part,
                part.FontFaceDeclarations,
                faces,
                nodes);
            partIndex = partIndex + 1;
        }
        document.FontFaces = faces.ToArray();
        FontFaceDocumentState state = new FontFaceDocumentState();
        state.OriginalNodes = nodes.ToArray();
        document.FontFaceState = state;
    }

    internal static void SynchronizeFontFaces(OdfDocument document)
    {
        if (document == null || document.FontFaces == null)
        {
            throw new OdfDocumentException(
                "A document and font-face collection are required for writing.");
        }
        FontFaceDocumentState state = document.FontFaceState
            ?? new FontFaceDocumentState();
        RemoveDeletedFaces(document.FontFaces, state.OriginalNodes);
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < document.FontFaces.Length)
        {
            OdfFontFace face = document.FontFaces[index];
            if (face == null)
            {
                throw new OdfDocumentException(
                    "The font-face collection contains a null declaration.");
            }
            SynchronizeFace(document, face);
            if (face.OriginalNode != null)
            {
                nodes.Add(face.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalNodes = nodes.ToArray();
        document.FontFaceState = state;
    }

    static void LoadDeclarationSection(
        OdfDocument document,
        OdfPart part,
        OdfNode? section,
        List<OdfFontFace> faces,
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
            if (IsFontFaceNode(node))
            {
                OdfFontFace face = CreateFace(document, part, node);
                faces.Add(face);
                nodes.Add(node);
            }
            index = index + 1;
        }
    }

    static OdfFontFace CreateFace(
        OdfDocument document,
        OdfPart part,
        OdfNode node)
    {
        OdfFontFace result = new OdfFontFace();
        result.Name = ReadAttribute(node, OdfNamespaces.Style, "name");
        result.FontFamily = ReadAttribute(
            node,
            OdfNamespaces.Svg,
            "font-family");
        result.FontStyle = ReadAttribute(node, OdfNamespaces.Svg, "font-style");
        result.FontVariant = ReadAttribute(
            node,
            OdfNamespaces.Svg,
            "font-variant");
        result.FontWeight = ReadAttribute(
            node,
            OdfNamespaces.Svg,
            "font-weight");
        result.FontStretch = ReadAttribute(
            node,
            OdfNamespaces.Svg,
            "font-stretch");
        result.FontSize = ReadAttribute(node, OdfNamespaces.Svg, "font-size");
        result.UnicodeRange = ReadAttribute(
            node,
            OdfNamespaces.Svg,
            "unicode-range");
        result.UnitsPerEm = ReadAttribute(node, OdfNamespaces.Svg, "units-per-em");
        result.Panose1 = ReadAttribute(node, OdfNamespaces.Svg, "panose-1");
        result.StemVertical = ReadAttribute(node, OdfNamespaces.Svg, "stemv");
        result.StemHorizontal = ReadAttribute(node, OdfNamespaces.Svg, "stemh");
        result.Slope = ReadAttribute(node, OdfNamespaces.Svg, "slope");
        result.CapHeight = ReadAttribute(node, OdfNamespaces.Svg, "cap-height");
        result.XHeight = ReadAttribute(node, OdfNamespaces.Svg, "x-height");
        result.AccentHeight = ReadAttribute(node, OdfNamespaces.Svg, "accent-height");
        result.Ascent = ReadAttribute(node, OdfNamespaces.Svg, "ascent");
        result.Descent = ReadAttribute(node, OdfNamespaces.Svg, "descent");
        result.Widths = ReadAttribute(node, OdfNamespaces.Svg, "widths");
        result.BoundingBox = ReadAttribute(node, OdfNamespaces.Svg, "bbox");
        result.Ideographic = ReadAttribute(node, OdfNamespaces.Svg, "ideographic");
        result.Alphabetic = ReadAttribute(node, OdfNamespaces.Svg, "alphabetic");
        result.Mathematical = ReadAttribute(node, OdfNamespaces.Svg, "mathematical");
        result.Hanging = ReadAttribute(node, OdfNamespaces.Svg, "hanging");
        result.VerticalIdeographic = ReadAttribute(node, OdfNamespaces.Svg, "v-ideographic");
        result.VerticalAlphabetic = ReadAttribute(node, OdfNamespaces.Svg, "v-alphabetic");
        result.VerticalMathematical = ReadAttribute(node, OdfNamespaces.Svg, "v-mathematical");
        result.VerticalHanging = ReadAttribute(node, OdfNamespaces.Svg, "v-hanging");
        result.UnderlinePosition = ReadAttribute(node, OdfNamespaces.Svg, "underline-position");
        result.UnderlineThickness = ReadAttribute(node, OdfNamespaces.Svg, "underline-thickness");
        result.StrikethroughPosition = ReadAttribute(node, OdfNamespaces.Svg, "strikethrough-position");
        result.StrikethroughThickness = ReadAttribute(node, OdfNamespaces.Svg, "strikethrough-thickness");
        result.OverlinePosition = ReadAttribute(node, OdfNamespaces.Svg, "overline-position");
        result.OverlineThickness = ReadAttribute(node, OdfNamespaces.Svg, "overline-thickness");
        result.FontAdornments = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "font-adornments");
        result.GenericFamily = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "font-family-generic");
        result.Pitch = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "font-pitch");
        result.Charset = ReadAttribute(
            node,
            OdfNamespaces.Style,
            "font-charset");
        result.PartPath = part.Path;
        result.Sources = ReadSources(document, part.Path, node);
        result.OriginalSourceNodes = SourceNodes(result.Sources);
        result.Node = node;
        result.OriginalNode = node;
        SnapshotFace(result);
        return result;
    }

    static OdfFontFaceSource[] ReadSources(
        OdfDocument document,
        string partPath,
        OdfNode faceNode)
    {
        List<OdfFontFaceSource> result = new List<OdfFontFaceSource>();
        int index = 0;
        while (index < faceNode.Children.Length)
        {
            OdfNode child = faceNode.Children[index];
            if (IsSvgNode(child, "font-face-src"))
            {
                ReadSourceContainer(document, partPath, child, result);
            }
            else if (IsSvgNode(child, "definition-src"))
            {
                result.Add(CreateSource(document, partPath, child, true));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ReadSourceContainer(
        OdfDocument document,
        string partPath,
        OdfNode container,
        List<OdfFontFaceSource> sources)
    {
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode node = container.Children[index];
            if (IsSvgNode(node, "font-face-uri")
                || IsSvgNode(node, "font-face-name"))
            {
                sources.Add(CreateSource(document, partPath, node, false));
            }
            index = index + 1;
        }
    }

    static OdfFontFaceSource CreateSource(
        OdfDocument document,
        string partPath,
        OdfNode node,
        bool definition)
    {
        OdfFontFaceSource result = new OdfFontFaceSource();
        result.Name = ReadAttribute(node, OdfNamespaces.Svg, "name");
        result.Href = ReadAttribute(node, OdfNamespaces.XLink, "href");
        result.XLinkType = ReadAttribute(node, OdfNamespaces.XLink, "type");
        result.Actuate = ReadAttribute(node, OdfNamespaces.XLink, "actuate");
        result.Formats = ReadFormats(node);
        result.IsDefinitionSource = definition;
        result.Node = node;
        result.OriginalNode = node;
        ResolveResourceReference(document, partPath, result);
        SnapshotSource(result);
        return result;
    }

    static string[] ReadFormats(OdfNode node)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsSvgNode(child, "font-face-format"))
            {
                result.Add(ReadAttribute(
                    child,
                    OdfNamespaces.Svg,
                    "string"));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ResolveResourceReference(
        OdfDocument document,
        string partPath,
        OdfFontFaceSource source)
    {
        source.IsEmbedded = false;
        source.IsExternal = false;
        source.ResolvedPath = "";
        source.ResourceExists = false;
        if (source.Href.Length == 0)
        {
            return;
        }
        if (OdfIriResolver.IsAbsoluteIri(source.Href)
            || source.Href.StartsWith("//", StringComparison.Ordinal)
            || document.PackageKind == OdfPackageKind.Flat)
        {
            source.IsExternal = true;
            return;
        }
        source.IsEmbedded = true;
        string hrefPath = RemoveQueryAndFragment(source.Href);
        source.ResolvedPath = PackagePathRules.ResolveRelativeReference(
            partPath,
            hrefPath);
        source.ResourceExists = PackageEntryExists(
            document,
            source.ResolvedPath);
    }

    static string RemoveQueryAndFragment(string href)
    {
        int end = href.Length;
        int query = href.IndexOf('?');
        if (query >= 0 && query < end)
        {
            end = query;
        }
        int fragment = href.IndexOf('#');
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }
        return href.Substring(0, end);
    }

    static bool PackageEntryExists(OdfDocument document, string path)
    {
        if (path.Length == 0)
        {
            return false;
        }
        OdfPackageEntry[] entries = document.PackageEntries;
        if (document.Package != null)
        {
            entries = document.Package.Entries;
        }
        int index = 0;
        while (index < entries.Length)
        {
            OdfPackageEntry entry = entries[index];
            string entryPath = entry.CanonicalPath;
            if (entryPath.Length == 0)
            {
                entryPath = entry.Path;
            }
            if (entryPath == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedFaces(
        OdfFontFace[] faces,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!FaceRetainsNode(faces, node) && node.Parent != null)
            {
                RemoveNode(node);
            }
            index = index + 1;
        }
    }

    static bool FaceRetainsNode(OdfFontFace[] faces, OdfNode node)
    {
        int index = 0;
        while (index < faces.Length)
        {
            OdfFontFace face = faces[index];
            if (face != null && face.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void SynchronizeFace(OdfDocument document, OdfFontFace face)
    {
        OdfNode section = EnsureDeclarationSection(document, face);
        OdfNode? node = face.OriginalNode;
        bool isNew = node == null;
        if (node == null)
        {
            node = CreateElement(
                OdfNamespaces.Style,
                "font-face",
                OdfNamespaces.StylePrefix,
                section.Location.PartPath);
            AppendChild(section, node);
            face.OriginalNode = node;
        }
        else if (node.Parent != section)
        {
            RemoveNode(node);
            AppendChild(section, node);
        }
        ApplyFaceFields(face, node, isNew);
        face.Node = node;
        face.PartPath = section.Location.PartPath;
        SynchronizeSources(document, face, node);
        SnapshotFace(face);
    }

    static OdfNode EnsureDeclarationSection(
        OdfDocument document,
        OdfFontFace face)
    {
        string partPath = SelectPartPath(document, face);
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        if (part.FontFaceDeclarations != null)
        {
            return part.FontFaceDeclarations;
        }
        if (part.Root == null)
        {
            throw new OdfDocumentException(
                "A font-face section requires a materialized XML part root.");
        }
        OdfNode section = CreateElement(
            OdfNamespaces.Office,
            "font-face-decls",
            OdfNamespaces.OfficePrefix,
            part.Path);
        section.NamespaceDeclarations =
        [
            Declaration(OdfNamespaces.OfficePrefix, OdfNamespaces.Office),
            Declaration(OdfNamespaces.StylePrefix, OdfNamespaces.Style),
            Declaration(OdfNamespaces.SvgPrefix, OdfNamespaces.Svg),
            Declaration(OdfNamespaces.XLinkPrefix, OdfNamespaces.XLink)
        ];
        StyleDocumentModel.InsertSection(
            part.Root,
            section,
            StyleDocumentModel.SectionRank("font-face-decls"));
        part.FontFaceDeclarations = section;
        StyleDocumentModel.RefreshPartMapping(document, part);
        return section;
    }

    static string SelectPartPath(
        OdfDocument document,
        OdfFontFace face)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (face.PartPath == OdfPartNames.Content
            || face.PartPath == OdfPartNames.Styles)
        {
            return face.PartPath;
        }
        if (face.OriginalPartPath == OdfPartNames.Content
            || face.OriginalPartPath == OdfPartNames.Styles)
        {
            return face.OriginalPartPath;
        }
        return OdfPartNames.Styles;
    }

    static void ApplyFaceFields(
        OdfFontFace face,
        OdfNode node,
        bool isNew)
    {
        WriteChangedAttribute(node, OdfNamespaces.Style, "name", OdfNamespaces.StylePrefix, face.Name, face.OriginalName, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "font-family", OdfNamespaces.SvgPrefix, face.FontFamily, face.OriginalFontFamily, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "font-style", OdfNamespaces.SvgPrefix, face.FontStyle, face.OriginalFontStyle, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "font-variant", OdfNamespaces.SvgPrefix, face.FontVariant, face.OriginalFontVariant, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "font-weight", OdfNamespaces.SvgPrefix, face.FontWeight, face.OriginalFontWeight, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "font-stretch", OdfNamespaces.SvgPrefix, face.FontStretch, face.OriginalFontStretch, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "font-size", OdfNamespaces.SvgPrefix, face.FontSize, face.OriginalFontSize, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "unicode-range", OdfNamespaces.SvgPrefix, face.UnicodeRange, face.OriginalUnicodeRange, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "units-per-em", OdfNamespaces.SvgPrefix, face.UnitsPerEm, face.OriginalUnitsPerEm, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "panose-1", OdfNamespaces.SvgPrefix, face.Panose1, face.OriginalPanose1, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "stemv", OdfNamespaces.SvgPrefix, face.StemVertical, face.OriginalStemVertical, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "stemh", OdfNamespaces.SvgPrefix, face.StemHorizontal, face.OriginalStemHorizontal, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "slope", OdfNamespaces.SvgPrefix, face.Slope, face.OriginalSlope, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "cap-height", OdfNamespaces.SvgPrefix, face.CapHeight, face.OriginalCapHeight, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "x-height", OdfNamespaces.SvgPrefix, face.XHeight, face.OriginalXHeight, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "accent-height", OdfNamespaces.SvgPrefix, face.AccentHeight, face.OriginalAccentHeight, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "ascent", OdfNamespaces.SvgPrefix, face.Ascent, face.OriginalAscent, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "descent", OdfNamespaces.SvgPrefix, face.Descent, face.OriginalDescent, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "widths", OdfNamespaces.SvgPrefix, face.Widths, face.OriginalWidths, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "bbox", OdfNamespaces.SvgPrefix, face.BoundingBox, face.OriginalBoundingBox, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "ideographic", OdfNamespaces.SvgPrefix, face.Ideographic, face.OriginalIdeographic, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "alphabetic", OdfNamespaces.SvgPrefix, face.Alphabetic, face.OriginalAlphabetic, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "mathematical", OdfNamespaces.SvgPrefix, face.Mathematical, face.OriginalMathematical, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "hanging", OdfNamespaces.SvgPrefix, face.Hanging, face.OriginalHanging, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "v-ideographic", OdfNamespaces.SvgPrefix, face.VerticalIdeographic, face.OriginalVerticalIdeographic, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "v-alphabetic", OdfNamespaces.SvgPrefix, face.VerticalAlphabetic, face.OriginalVerticalAlphabetic, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "v-mathematical", OdfNamespaces.SvgPrefix, face.VerticalMathematical, face.OriginalVerticalMathematical, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "v-hanging", OdfNamespaces.SvgPrefix, face.VerticalHanging, face.OriginalVerticalHanging, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "underline-position", OdfNamespaces.SvgPrefix, face.UnderlinePosition, face.OriginalUnderlinePosition, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "underline-thickness", OdfNamespaces.SvgPrefix, face.UnderlineThickness, face.OriginalUnderlineThickness, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "strikethrough-position", OdfNamespaces.SvgPrefix, face.StrikethroughPosition, face.OriginalStrikethroughPosition, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "strikethrough-thickness", OdfNamespaces.SvgPrefix, face.StrikethroughThickness, face.OriginalStrikethroughThickness, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "overline-position", OdfNamespaces.SvgPrefix, face.OverlinePosition, face.OriginalOverlinePosition, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Svg, "overline-thickness", OdfNamespaces.SvgPrefix, face.OverlineThickness, face.OriginalOverlineThickness, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "font-adornments", OdfNamespaces.StylePrefix, face.FontAdornments, face.OriginalFontAdornments, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "font-family-generic", OdfNamespaces.StylePrefix, face.GenericFamily, face.OriginalGenericFamily, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "font-pitch", OdfNamespaces.StylePrefix, face.Pitch, face.OriginalPitch, isNew);
        WriteChangedAttribute(node, OdfNamespaces.Style, "font-charset", OdfNamespaces.StylePrefix, face.Charset, face.OriginalCharset, isNew);
    }

    static void SynchronizeSources(
        OdfDocument document,
        OdfFontFace face,
        OdfNode faceNode)
    {
        if (face.Sources == null)
        {
            throw new OdfDocumentException(
                "A font-face source collection is required for writing.");
        }
        RemoveDeletedSources(face.Sources, face.OriginalSourceNodes);
        int index = 0;
        while (index < face.Sources.Length)
        {
            OdfFontFaceSource source = face.Sources[index];
            if (source == null)
            {
                throw new OdfDocumentException(
                    "The font-face source collection contains a null source.");
            }
            SynchronizeSource(faceNode, source);
            ResolveResourceReference(document, face.PartPath, source);
            SnapshotSource(source);
            index = index + 1;
        }
        RemoveEmptySourceContainers(faceNode);
        face.OriginalSourceNodes = SourceNodes(face.Sources);
    }

    static void RemoveDeletedSources(
        OdfFontFaceSource[] sources,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!SourceRetainsNode(sources, node) && node.Parent != null)
            {
                RemoveNode(node);
            }
            index = index + 1;
        }
    }

    static bool SourceRetainsNode(
        OdfFontFaceSource[] sources,
        OdfNode node)
    {
        int index = 0;
        while (index < sources.Length)
        {
            OdfFontFaceSource source = sources[index];
            if (source != null && source.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode[] SourceNodes(OdfFontFaceSource[] sources)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < sources.Length)
        {
            OdfFontFaceSource source = sources[index];
            if (source != null && source.OriginalNode != null)
            {
                result.Add(source.OriginalNode);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void SynchronizeSource(
        OdfNode faceNode,
        OdfFontFaceSource source)
    {
        OdfNode parent = source.IsDefinitionSource
            ? faceNode
            : EnsureSourceContainer(faceNode);
        OdfNode? node = source.OriginalNode;
        bool isNew = node == null;
        if (node == null)
        {
            node = CreateSourceElement(source, faceNode.Location.PartPath);
            AppendChild(parent, node);
            source.OriginalNode = node;
        }
        else if (node.Parent != parent)
        {
            RemoveNode(node);
            AppendChild(parent, node);
        }
        ApplySourceFields(source, node, isNew);
        source.Node = node;
    }

    static OdfNode EnsureSourceContainer(OdfNode faceNode)
    {
        int index = 0;
        while (index < faceNode.Children.Length)
        {
            if (IsSvgNode(faceNode.Children[index], "font-face-src"))
            {
                return faceNode.Children[index];
            }
            index = index + 1;
        }
        OdfNode result = CreateElement(
            OdfNamespaces.Svg,
            "font-face-src",
            OdfNamespaces.SvgPrefix,
            faceNode.Location.PartPath);
        AppendChild(faceNode, result);
        return result;
    }

    static OdfNode CreateSourceElement(
        OdfFontFaceSource source,
        string partPath)
    {
        string localName = "font-face-name";
        if (source.IsDefinitionSource)
        {
            localName = "definition-src";
        }
        else if (source.Href.Length > 0)
        {
            localName = "font-face-uri";
        }
        return CreateElement(
            OdfNamespaces.Svg,
            localName,
            OdfNamespaces.SvgPrefix,
            partPath);
    }

    static void ApplySourceFields(
        OdfFontFaceSource source,
        OdfNode node,
        bool isNew)
    {
        bool usesHref = source.IsDefinitionSource || source.Href.Length > 0;
        string localName = usesHref ? "font-face-uri" : "font-face-name";
        if (source.IsDefinitionSource)
        {
            localName = "definition-src";
        }
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Name.NamespaceName = OdfNamespaces.Svg;
        node.Name.LocalName = localName;
        node.Name.Prefix = OdfNamespaces.SvgPrefix;
        if (usesHref)
        {
            if (source.XLinkType.Length == 0)
            {
                source.XLinkType = "simple";
            }
            WriteChangedAttribute(node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, source.Href, source.OriginalHref, isNew);
            WriteChangedAttribute(node, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, source.XLinkType, source.OriginalXLinkType, isNew);
            WriteChangedAttribute(node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, source.Actuate, source.OriginalActuate, isNew);
            RemoveAttribute(node, OdfNamespaces.Svg, "name");
        }
        else
        {
            WriteChangedAttribute(node, OdfNamespaces.Svg, "name", OdfNamespaces.SvgPrefix, source.Name, source.OriginalName, isNew);
            RemoveAttribute(node, OdfNamespaces.XLink, "href");
            RemoveAttribute(node, OdfNamespaces.XLink, "type");
            RemoveAttribute(node, OdfNamespaces.XLink, "actuate");
        }
        if (isNew || !StringsMatch(source.Formats, source.OriginalFormats))
        {
            WriteFormats(node, source.Formats);
        }
    }

    static void WriteFormats(OdfNode node, string[] formats)
    {
        if (formats == null)
        {
            throw new OdfDocumentException(
                "A font-face format collection is required for writing.");
        }
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            if (!IsSvgNode(node.Children[index], "font-face-format"))
            {
                children.Add(node.Children[index]);
            }
            index = index + 1;
        }
        index = 0;
        while (index < formats.Length)
        {
            OdfNode format = CreateElement(
                OdfNamespaces.Svg,
                "font-face-format",
                OdfNamespaces.SvgPrefix,
                node.Location.PartPath);
            if (formats[index].Length > 0)
            {
                SetAttribute(
                    format,
                    OdfNamespaces.Svg,
                    "string",
                    OdfNamespaces.SvgPrefix,
                    formats[index]);
            }
            format.Parent = node;
            children.Add(format);
            index = index + 1;
        }
        node.Children = children.ToArray();
    }

    static void RemoveEmptySourceContainers(OdfNode faceNode)
    {
        int index = 0;
        while (index < faceNode.Children.Length)
        {
            OdfNode child = faceNode.Children[index];
            if (IsSvgNode(child, "font-face-src")
                && child.Children.Length == 0)
            {
                RemoveNode(child);
            }
            else
            {
                index = index + 1;
            }
        }
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
            RemoveAttribute(node, namespaceName, localName);
        }
        else
        {
            SetAttribute(node, namespaceName, localName, prefix, value);
        }
    }

    static void SetAttribute(
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
                attribute.Name.Prefix = prefix;
                attribute.Value = value;
                return;
            }
            index = index + 1;
        }
        OdfAttribute[] attributes = new OdfAttribute[
            node.Attributes.Length + 1];
        Array.Copy(node.Attributes, attributes, node.Attributes.Length);
        OdfAttribute created = new OdfAttribute();
        created.Name.NamespaceName = namespaceName;
        created.Name.LocalName = localName;
        created.Name.Prefix = prefix;
        created.Value = value;
        created.Location.PartPath = node.Location.PartPath;
        attributes[attributes.Length - 1] = created;
        node.Attributes = attributes;
    }

    static void RemoveAttribute(
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

    static string ReadAttribute(
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

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Name.Prefix = prefix;
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNamespaceDeclaration Declaration(
        string prefix,
        string namespaceName)
    {
        OdfNamespaceDeclaration result = new OdfNamespaceDeclaration();
        result.Prefix = prefix;
        result.NamespaceName = namespaceName;
        return result;
    }

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static void RemoveNode(OdfNode node)
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

    static bool IsFontFaceNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName == "font-face";
    }

    static bool IsSvgNode(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Svg
            && node.Name.LocalName == localName;
    }

    static bool StringsMatch(string[] first, string[] second)
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

    static string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static void SnapshotFace(OdfFontFace face)
    {
        face.OriginalName = face.Name;
        face.OriginalFontFamily = face.FontFamily;
        face.OriginalFontStyle = face.FontStyle;
        face.OriginalFontVariant = face.FontVariant;
        face.OriginalFontWeight = face.FontWeight;
        face.OriginalFontStretch = face.FontStretch;
        face.OriginalFontSize = face.FontSize;
        face.OriginalUnicodeRange = face.UnicodeRange;
        face.OriginalUnitsPerEm = face.UnitsPerEm;
        face.OriginalPanose1 = face.Panose1;
        face.OriginalStemVertical = face.StemVertical;
        face.OriginalStemHorizontal = face.StemHorizontal;
        face.OriginalSlope = face.Slope;
        face.OriginalCapHeight = face.CapHeight;
        face.OriginalXHeight = face.XHeight;
        face.OriginalAccentHeight = face.AccentHeight;
        face.OriginalAscent = face.Ascent;
        face.OriginalDescent = face.Descent;
        face.OriginalWidths = face.Widths;
        face.OriginalBoundingBox = face.BoundingBox;
        face.OriginalIdeographic = face.Ideographic;
        face.OriginalAlphabetic = face.Alphabetic;
        face.OriginalMathematical = face.Mathematical;
        face.OriginalHanging = face.Hanging;
        face.OriginalVerticalIdeographic = face.VerticalIdeographic;
        face.OriginalVerticalAlphabetic = face.VerticalAlphabetic;
        face.OriginalVerticalMathematical = face.VerticalMathematical;
        face.OriginalVerticalHanging = face.VerticalHanging;
        face.OriginalUnderlinePosition = face.UnderlinePosition;
        face.OriginalUnderlineThickness = face.UnderlineThickness;
        face.OriginalStrikethroughPosition = face.StrikethroughPosition;
        face.OriginalStrikethroughThickness = face.StrikethroughThickness;
        face.OriginalOverlinePosition = face.OverlinePosition;
        face.OriginalOverlineThickness = face.OverlineThickness;
        face.OriginalFontAdornments = face.FontAdornments;
        face.OriginalGenericFamily = face.GenericFamily;
        face.OriginalPitch = face.Pitch;
        face.OriginalCharset = face.Charset;
        face.OriginalPartPath = face.PartPath;
    }

    static void SnapshotSource(OdfFontFaceSource source)
    {
        source.OriginalName = source.Name;
        source.OriginalHref = source.Href;
        source.OriginalXLinkType = source.XLinkType;
        source.OriginalActuate = source.Actuate;
        source.OriginalFormats = CopyStrings(source.Formats);
        source.OriginalIsDefinitionSource = source.IsDefinitionSource;
    }
}
