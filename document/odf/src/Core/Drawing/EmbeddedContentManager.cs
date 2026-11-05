using System.Collections.Generic;
using System.IO;
using System.Text;

namespace odf;

internal static class EmbeddedContentManager
{
    internal static void LoadEmbeddedContent(OdfDocument document)
    {
        List<OdfEmbeddedObject> objects = new List<OdfEmbeddedObject>();
        AddDiscoveredSubdocuments(document, objects);
        AttachDrawingObjects(document, objects);
        AddUnreferencedMathParts(document, objects);
        document.EmbeddedObjects = objects.ToArray();
        document.Charts = LoadCharts(document, document.EmbeddedObjects);
        document.Thumbnails = LoadThumbnails(document);
    }

    internal static void ReloadChartsAfterReferenceUpdate(
        OdfDocument document)
    {
        RefreshChartStylePropertyValues(document);
        int index = 0;
        while (index < document.EmbeddedObjects.Length)
        {
            document.EmbeddedObjects[index].Chart = null;
            index = index + 1;
        }
        document.Charts = LoadCharts(document, document.EmbeddedObjects);
        ChartDocumentModel.AttachChartDataTables(document);
    }

    internal static void SynchronizeEmbeddedContent(OdfDocument document)
    {
        bool packageChanged = false;
        int index = 0;
        while (index < document.EmbeddedObjects.Length)
        {
            OdfEmbeddedObject item = document.EmbeddedObjects[index];
            if (item == null)
            {
                throw new OdfEditException(
                    "The embedded-object collection contains a null object.");
            }
            SynchronizeEmbeddedObject(document, item);
            if (item.SourceKind == OdfResourceSourceKind.Package
                && item.PackageEntryExists
                && item.PackagePath.Length > 0
                && !item.PackagePath.EndsWith(
                    "/",
                    global::System.StringComparison.Ordinal)
                && !BytesMatch(item.Data, item.OriginalData))
            {
                ReplacePackageBytes(document, item.PackagePath, item.Data);
                item.OriginalData = CopyBytes(item.Data);
                packageChanged = true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.Charts.Length)
        {
            SynchronizeChart(document.Charts[index]);
            index = index + 1;
        }
        index = 0;
        while (index < document.Thumbnails.Length)
        {
            OdfThumbnail thumbnail = document.Thumbnails[index];
            if (thumbnail == null)
            {
                throw new OdfEditException(
                    "The thumbnail collection contains a null thumbnail.");
            }
            if (!BytesMatch(thumbnail.Data, thumbnail.OriginalData))
            {
                RequireThumbnailData(thumbnail);
                ReplacePackageBytes(document, thumbnail.Path, thumbnail.Data);
                thumbnail.OriginalData = CopyBytes(thumbnail.Data);
                packageChanged = true;
            }
            index = index + 1;
        }
        if (packageChanged)
        {
            RefreshDocumentPackage(document);
        }
        LoadEmbeddedContent(document);
        ChartDocumentModel.AttachChartDataTables(document);
    }

    internal static byte[] ExtractEmbeddedObjectData(
        OdfDocument document,
        OdfEmbeddedObject item)
    {
        if (item.SourceKind == OdfResourceSourceKind.External)
        {
            throw new OdfEditException(
                "External embedded objects are never fetched during extraction.");
        }
        if (item.SourceKind == OdfResourceSourceKind.Inline)
        {
            if (!item.IsValid)
            {
                throw new OdfEditException(
                    "Invalid inline embedded-object data cannot be extracted.");
            }
            return CopyBytes(item.Data);
        }
        if (item.SourceKind == OdfResourceSourceKind.Package
            && item.PackageEntryExists
            && !item.IsDirectoryPackage)
        {
            return ReadPackageBytes(document, item.PackagePath);
        }
        throw new OdfEditException(
            "The embedded object does not have a single extractable byte source.");
    }

    internal static byte[] ExtractThumbnailData(OdfThumbnail thumbnail)
    {
        if (!thumbnail.IsContentAvailable || !thumbnail.IsValid)
        {
            throw new OdfEditException(
                "The thumbnail does not have valid available image bytes.");
        }
        return CopyBytes(thumbnail.Data);
    }

    static void AddDiscoveredSubdocuments(
        OdfDocument document,
        List<OdfEmbeddedObject> objects)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return;
        }
        EmbeddedPackageTraversal traversal =
            document.Package.EntryIndex.DiscoverEmbeddedPackages(
                document.LoadOptions.SecurityLimits);
        int index = 0;
        while (index < traversal.Packages.Length)
        {
            AddPackageNode(
                document,
                traversal.Packages[index],
                objects);
            index = index + 1;
        }
    }

    static OdfEmbeddedObject AddPackageNode(
        OdfDocument document,
        EmbeddedPackageNode node,
        List<OdfEmbeddedObject> objects)
    {
        OdfEmbeddedObject result = new OdfEmbeddedObject();
        result.Kind = KindFromMediaType(node.MediaType);
        result.SourceKind = OdfResourceSourceKind.Package;
        result.PackagePath = node.QualifiedPath;
        result.MediaType = node.MediaType;
        result.DocumentKind = MimetypeRules.GetDocumentKind(node.MediaType);
        result.PackageEntryExists = true;
        result.IsDirectoryPackage = node.IsDirectoryPackage;
        result.IsArchivePackage = node.IsArchivePackage;
        result.IsEncrypted = HasEncryptedPackageContent(
            document,
            node.Path,
            node.Entries);
        result.IsContentAvailable = node.IsValid
            && !node.TraversalStopped
            && !result.IsEncrypted;
        result.IsValid = node.IsValid;
        result.PackageEntries = node.Entries;
        if (node.IsArchivePackage
            && node.QualifiedPath.IndexOf("!/") < 0
            && PackageContentAvailable(document, node.Path))
        {
            result.Data = ReadPackageBytes(document, node.Path);
            result.OriginalData = CopyBytes(result.Data);
        }
        objects.Add(result);
        List<OdfEmbeddedObject> children = new List<OdfEmbeddedObject>();
        int index = 0;
        while (index < node.Children.Length)
        {
            children.Add(AddPackageNode(
                document,
                node.Children[index],
                objects));
            index = index + 1;
        }
        result.Children = children.ToArray();
        return result;
    }

    static void AttachDrawingObjects(
        OdfDocument document,
        List<OdfEmbeddedObject> objects)
    {
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            drawing.EmbeddedObject = null;
            if (drawing.Kind == OdfDrawingKind.EmbeddedObject
                && drawing.Node != null)
            {
                OdfEmbeddedObject? existing = FindReferencedPackageObject(
                    drawing,
                    objects);
                OdfEmbeddedObject item = existing == null
                    ? new OdfEmbeddedObject()
                    : existing;
                PopulateDrawingObject(document, drawing, item);
                if (existing == null)
                {
                    objects.Add(item);
                }
                item.ReplacementImage = FindReplacementImage(
                    document,
                    drawing);
                drawing.EmbeddedObject = item;
            }
            index = index + 1;
        }
    }

    static OdfEmbeddedObject? FindReferencedPackageObject(
        OdfDrawingObject drawing,
        List<OdfEmbeddedObject> objects)
    {
        OdfNode node = drawing.Node ?? new OdfNode();
        string href = Read(node, OdfNamespaces.XLink, "href");
        if (href.Length == 0
            || !OdfIriResolver.IsPackageRelativeReference(href))
        {
            return null;
        }
        string resolved = ResourcePath(
            PackagePathRules.ResolveRelativeReference(
                drawing.PartPath,
                href));
        int index = 0;
        while (index < objects.Count)
        {
            string path = objects[index].PackagePath;
            if (path == resolved
                || path == resolved + "/"
                || path.EndsWith(
                    "/",
                    global::System.StringComparison.Ordinal)
                && path.Substring(0, path.Length - 1) == resolved)
            {
                return objects[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void PopulateDrawingObject(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfEmbeddedObject item)
    {
        OdfNode node = drawing.Node ?? new OdfNode();
        item.Drawing = drawing;
        item.Node = node;
        item.Href = Read(node, OdfNamespaces.XLink, "href");
        item.ClassId = Read(node, OdfNamespaces.Drawing, "class-id");
        item.NotifyOnUpdateOfRanges = Read(
            node,
            OdfNamespaces.Drawing,
            "notify-on-update-of-ranges");
        item.XLinkType = Read(node, OdfNamespaces.XLink, "type");
        item.Show = Read(node, OdfNamespaces.XLink, "show");
        item.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        item.MathNode = FindDescendant(
            node,
            OdfNamespaces.Math,
            "math");
        OdfNode? chartNode = FindDescendant(
            node,
            OdfNamespaces.Chart,
            "chart");
        item.BinaryDataNode = FindDirectChild(
            node,
            OdfNamespaces.Office,
            "binary-data");
        if (drawing.ElementName == "object-ole")
        {
            item.Kind = OdfEmbeddedObjectKind.Ole;
        }
        else if (item.MathNode != null)
        {
            item.Kind = OdfEmbeddedObjectKind.MathMl;
        }
        else if (chartNode != null)
        {
            item.Kind = OdfEmbeddedObjectKind.Chart;
        }
        if (item.BinaryDataNode != null)
        {
            LoadInlineBinary(item);
        }
        else
        {
            ResolveDrawingSource(document, drawing, item);
        }
        if (item.MathNode != null || chartNode != null)
        {
            item.SourceKind = OdfResourceSourceKind.Inline;
            item.IsReferenceValid = true;
            item.IsContentAvailable = true;
            item.IsValid = true;
        }
        CaptureOriginalValues(item);
    }

    static void ResolveDrawingSource(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfEmbeddedObject item)
    {
        if (item.Href.Length == 0)
        {
            item.IsReferenceValid = item.MathNode != null;
            return;
        }
        item.IsReferenceValid = OdfIriResolver.IsIriReference(item.Href);
        if (!item.IsReferenceValid)
        {
            return;
        }
        if (document.PackageKind == OdfPackageKind.Package
            && OdfIriResolver.IsPackageRelativeReference(item.Href))
        {
            item.SourceKind = OdfResourceSourceKind.Package;
            if (item.PackagePath.Length == 0)
            {
                item.PackagePath = ResourcePath(
                    PackagePathRules.ResolveRelativeReference(
                        drawing.PartPath,
                        item.Href));
            }
            ResolveExactPackageObject(document, item);
        }
        else
        {
            item.SourceKind = OdfResourceSourceKind.External;
            item.IsValid = true;
        }
    }

    static void ResolveExactPackageObject(
        OdfDocument document,
        OdfEmbeddedObject item)
    {
        if (item.PackageEntryExists)
        {
            return;
        }
        DiscoveredPackagePart? part = FindPackagePart(
            document,
            item.PackagePath);
        if (part == null)
        {
            return;
        }
        item.PackageEntryExists = true;
        item.MediaType = part.MediaType;
        item.DocumentKind = MimetypeRules.GetDocumentKind(part.MediaType);
        item.IsEncrypted = part.IsEncrypted;
        item.IsContentAvailable = PackageContentAvailable(
            document,
            item.PackagePath);
        item.IsValid = item.IsContentAvailable;
        OdfEmbeddedObjectKind mediaKind = KindFromMediaType(part.MediaType);
        if (item.Kind == OdfEmbeddedObjectKind.Unknown)
        {
            item.Kind = mediaKind;
        }
        if (item.IsContentAvailable)
        {
            item.Data = ReadPackageBytes(document, item.PackagePath);
            item.OriginalData = CopyBytes(item.Data);
        }
    }

    static void LoadInlineBinary(OdfEmbeddedObject item)
    {
        string text = TextContent(item.BinaryDataNode);
        OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(text);
        item.SourceKind = OdfResourceSourceKind.Inline;
        item.IsReferenceValid = true;
        item.IsContentAvailable = parsed.IsValid;
        item.IsValid = parsed.IsValid;
        if (parsed.IsValid)
        {
            item.Data = CopyBytes(parsed.BinaryValue);
            item.OriginalData = CopyBytes(item.Data);
        }
    }

    static OdfImage? FindReplacementImage(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        int index = 0;
        while (index < document.Images.Length)
        {
            OdfImage image = document.Images[index];
            if (image.Drawing != null
                && image.Drawing.Parent == drawing.Parent
                && image.Drawing != drawing
                && (image.PackagePath.StartsWith(
                        OdfPartNames.ObjectReplacementsDirectory,
                        global::System.StringComparison.Ordinal)
                    || image.Href.StartsWith(
                        OdfPartNames.ObjectReplacementsDirectory,
                        global::System.StringComparison.Ordinal)))
            {
                return image;
            }
            index = index + 1;
        }
        return null;
    }

    static void AddUnreferencedMathParts(
        OdfDocument document,
        List<OdfEmbeddedObject> objects)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return;
        }
        PackagePartCatalog catalog = document.Package.EntryIndex.DiscoverParts();
        int index = 0;
        while (index < catalog.AllParts.Length)
        {
            DiscoveredPackagePart part = catalog.AllParts[index];
            if (part.MediaType == OdfMediaTypes.MathMl
                && !ContainsPackagePath(objects, part.Entry.Path))
            {
                OdfEmbeddedObject item = new OdfEmbeddedObject();
                item.Kind = OdfEmbeddedObjectKind.MathMl;
                item.SourceKind = OdfResourceSourceKind.Package;
                item.PackagePath = part.Entry.Path;
                item.MediaType = part.MediaType;
                item.DocumentKind = OdfDocumentKind.Formula;
                item.PackageEntryExists = true;
                item.IsEncrypted = part.IsEncrypted;
                item.IsContentAvailable = PackageContentAvailable(
                    document,
                    part.Entry.Path);
                item.IsValid = item.IsContentAvailable;
                if (item.IsContentAvailable)
                {
                    item.Data = ReadPackageBytes(document, part.Entry.Path);
                    item.OriginalData = CopyBytes(item.Data);
                }
                objects.Add(item);
            }
            index = index + 1;
        }
    }

    static OdfChart[] LoadCharts(
        OdfDocument document,
        OdfEmbeddedObject[] objects)
    {
        List<OdfChart> charts = new List<OdfChart>();
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Root != null)
            {
                CollectChartNodes(
                    document,
                    part.Root,
                    part.Path,
                    objects,
                    charts);
            }
            index = index + 1;
        }
        index = 0;
        while (index < objects.Length)
        {
            OdfEmbeddedObject item = objects[index];
            if (item.Kind == OdfEmbeddedObjectKind.Chart
                && item.Chart == null)
            {
                OdfChart chart = new OdfChart();
                chart.EmbeddedObject = item;
                chart.PartPath = item.PackagePath;
                item.Chart = chart;
                charts.Add(chart);
            }
            index = index + 1;
        }
        return charts.ToArray();
    }

    static void RefreshChartStylePropertyValues(OdfDocument document)
    {
        int styleIndex = 0;
        while (styleIndex < document.Styles.Length)
        {
            OdfStyleProperty[] properties =
                document.Styles[styleIndex].ChartProperties;
            int propertyIndex = 0;
            while (propertyIndex < properties.Length)
            {
                OdfStyleProperty property = properties[propertyIndex];
                if (property.Attribute != null)
                {
                    property.LexicalValue = property.Attribute.Value;
                    property.OriginalLexicalValue = property.Attribute.Value;
                }
                propertyIndex = propertyIndex + 1;
            }
            styleIndex = styleIndex + 1;
        }
    }

    static void CollectChartNodes(
        OdfDocument document,
        OdfNode node,
        string partPath,
        OdfEmbeddedObject[] objects,
        List<OdfChart> charts)
    {
        if (IsElement(node, OdfNamespaces.Chart, "chart"))
        {
            OdfChart chart = CreateChart(document, node, partPath);
            OdfEmbeddedObject? owner = FindEmbeddedOwner(node, objects);
            if (owner != null)
            {
                chart.EmbeddedObject = owner;
                owner.Chart = chart;
                owner.Kind = OdfEmbeddedObjectKind.Chart;
            }
            charts.Add(chart);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectChartNodes(
                document,
                node.Children[index],
                partPath,
                objects,
                charts);
            index = index + 1;
        }
    }

    static OdfChart CreateChart(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfChart result = new OdfChart();
        result.Node = node;
        result.PartPath = partPath;
        result.ClassName = Read(node, OdfNamespaces.Chart, "class");
        result.Kind = ParseChartKind(result.ClassName);
        result.StyleName = Read(node, OdfNamespaces.Chart, "style-name");
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.XLinkType = Read(node, OdfNamespaces.XLink, "type");
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.Height = Read(node, OdfNamespaces.Svg, "height");
        ChartDocumentModel.LoadChartStructure(document, result);
        CaptureChartOriginalValues(result);
        return result;
    }

    static OdfChartKind ParseChartKind(string value)
    {
        int colon = value.LastIndexOf(':');
        string localName = colon < 0
            ? value
            : value.Substring(colon + 1);
        if (localName == "bar") return OdfChartKind.Bar;
        if (localName == "line") return OdfChartKind.Line;
        if (localName == "area") return OdfChartKind.Area;
        if (localName == "circle" || localName == "pie") return OdfChartKind.Pie;
        if (localName == "ring") return OdfChartKind.Ring;
        if (localName == "scatter") return OdfChartKind.Scatter;
        if (localName == "radar") return OdfChartKind.Radar;
        if (localName == "filled-radar") return OdfChartKind.FilledRadar;
        if (localName == "stock") return OdfChartKind.Stock;
        if (localName == "bubble") return OdfChartKind.Bubble;
        if (localName == "surface") return OdfChartKind.Surface;
        if (localName == "gantt") return OdfChartKind.Gantt;
        return OdfChartKind.Unknown;
    }

    static OdfEmbeddedObject? FindEmbeddedOwner(
        OdfNode chart,
        OdfEmbeddedObject[] objects)
    {
        int index = 0;
        while (index < objects.Length)
        {
            if (objects[index].Node != null
                && ContainsNode(objects[index].Node ?? new OdfNode(), chart))
            {
                return objects[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfThumbnail[] LoadThumbnails(OdfDocument document)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return [];
        }
        PackagePartCatalog catalog = document.Package.EntryIndex.DiscoverParts();
        List<OdfThumbnail> result = new List<OdfThumbnail>();
        int index = 0;
        while (index < catalog.ThumbnailParts.Length)
        {
            DiscoveredPackagePart part = catalog.ThumbnailParts[index];
            OdfThumbnail thumbnail = new OdfThumbnail();
            thumbnail.Path = part.Entry.Path;
            thumbnail.MediaType = part.MediaType;
            thumbnail.IsEncrypted = part.IsEncrypted;
            thumbnail.IsContentAvailable = PackageContentAvailable(
                document,
                part.Entry.Path);
            if (thumbnail.IsContentAvailable)
            {
                thumbnail.Data = ReadPackageBytes(document, part.Entry.Path);
                ApplyThumbnailInspection(thumbnail);
            }
            thumbnail.OriginalData = CopyBytes(thumbnail.Data);
            result.Add(thumbnail);
            index = index + 1;
        }
        return result.ToArray();
    }

    static void ApplyThumbnailInspection(OdfThumbnail thumbnail)
    {
        ImageInspection inspection = ImageSniffer.Inspect(thumbnail.Data);
        thumbnail.DetectedFormat = inspection.Format;
        thumbnail.DetectedMediaType = inspection.MediaType;
        thumbnail.IsValid = inspection.IsRecognized;
        thumbnail.HasMediaTypeMismatch = thumbnail.MediaType.Length > 0
            && inspection.MediaType.Length > 0
            && NormalizeMediaType(thumbnail.MediaType)
                != NormalizeMediaType(inspection.MediaType);
    }

    static void RequireThumbnailData(OdfThumbnail thumbnail)
    {
        ApplyThumbnailInspection(thumbnail);
        if (!thumbnail.IsValid)
        {
            throw new OdfEditException(
                "Thumbnail data does not have a recognized safe image signature.");
        }
        if (thumbnail.HasMediaTypeMismatch)
        {
            throw new OdfEditException(
                "The thumbnail media type does not match its image data.");
        }
    }

    static void SynchronizeEmbeddedObject(
        OdfDocument document,
        OdfEmbeddedObject item)
    {
        if (item.Node != null)
        {
            WriteChanged(item.Node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, item.Href, item.OriginalHref);
            WriteChanged(item.Node, OdfNamespaces.Drawing, "class-id", OdfNamespaces.DrawingPrefix, item.ClassId, item.OriginalClassId);
            WriteChanged(item.Node, OdfNamespaces.Drawing, "notify-on-update-of-ranges", OdfNamespaces.DrawingPrefix, item.NotifyOnUpdateOfRanges, item.OriginalNotifyOnUpdateOfRanges);
            WriteChanged(item.Node, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, item.XLinkType, item.OriginalXLinkType);
            WriteChanged(item.Node, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, item.Show, item.OriginalShow);
            WriteChanged(item.Node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, item.Actuate, item.OriginalActuate);
        }
        if (item.SourceKind == OdfResourceSourceKind.Inline
            && item.BinaryDataNode != null
            && !BytesMatch(item.Data, item.OriginalData))
        {
            SetTextContent(
                item.BinaryDataNode,
                global::System.Convert.ToBase64String(item.Data));
            item.IsValid = true;
            item.IsContentAvailable = true;
            item.OriginalData = CopyBytes(item.Data);
        }
        CaptureOriginalValues(item);
    }

    static void SynchronizeChart(OdfChart chart)
    {
        if (chart == null || chart.Node == null)
        {
            return;
        }
        WriteChanged(chart.Node, OdfNamespaces.Chart, "class", OdfNamespaces.ChartPrefix, chart.ClassName, chart.OriginalClassName);
        WriteChanged(chart.Node, OdfNamespaces.Chart, "style-name", OdfNamespaces.ChartPrefix, chart.StyleName, chart.OriginalStyleName);
        WriteChanged(chart.Node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, chart.Href, chart.OriginalHref);
        WriteChanged(chart.Node, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, chart.XLinkType, chart.OriginalXLinkType);
        WriteChanged(chart.Node, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, chart.Width, chart.OriginalWidth);
        WriteChanged(chart.Node, OdfNamespaces.Svg, "height", OdfNamespaces.SvgPrefix, chart.Height, chart.OriginalHeight);
        ChartDocumentModel.SynchronizeChartStructure(chart);
        chart.Kind = ParseChartKind(chart.ClassName);
        CaptureChartOriginalValues(chart);
    }

    static OdfEmbeddedObjectKind KindFromMediaType(string mediaType)
    {
        OdfDocumentKind documentKind = MimetypeRules.GetDocumentKind(mediaType);
        if (documentKind == OdfDocumentKind.Chart
            || documentKind == OdfDocumentKind.ChartTemplate)
        {
            return OdfEmbeddedObjectKind.Chart;
        }
        if (documentKind == OdfDocumentKind.Formula
            || documentKind == OdfDocumentKind.FormulaTemplate
            || mediaType == OdfMediaTypes.MathMl)
        {
            return OdfEmbeddedObjectKind.MathMl;
        }
        if (documentKind != OdfDocumentKind.Unknown)
        {
            return OdfEmbeddedObjectKind.Subdocument;
        }
        return OdfEmbeddedObjectKind.Unknown;
    }

    static DiscoveredPackagePart? FindPackagePart(
        OdfDocument document,
        string path)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return null;
        }
        PackagePartCatalog catalog = document.Package.EntryIndex.DiscoverParts();
        int index = 0;
        while (index < catalog.AllParts.Length)
        {
            if (catalog.AllParts[index].Entry.Path == path)
            {
                return catalog.AllParts[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool ContainsPackagePath(
        List<OdfEmbeddedObject> objects,
        string path)
    {
        int index = 0;
        while (index < objects.Count)
        {
            if (objects[index].PackagePath == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool PackageContentAvailable(OdfDocument document, string path)
    {
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return false;
        }
        EditablePackageEntry[] entries =
            document.Package.EntryIndex.GetEditableEntries();
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].Entry.Path == path)
            {
                return entries[index].IsContentAvailable;
            }
            index = index + 1;
        }
        return false;
    }

    static byte[] ReadPackageBytes(OdfDocument document, string path)
    {
        OdfPackage package = document.Package ?? new OdfPackage();
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        using (Stream input = index.OpenEntry(path))
        using (MemoryStream output = new MemoryStream())
        {
            input.CopyTo(output);
            return output.ToArray();
        }
    }

    static void ReplacePackageBytes(
        OdfDocument document,
        string path,
        byte[] data)
    {
        if (document.Package == null)
        {
            throw new OdfEditException(
                "A package document is required to replace embedded content.");
        }
        OdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult result = writer.ReplacePackageEntry(
            document.Package,
            path,
            data);
        if (result.Status != OdfEditStatus.Success
            && result.Status != OdfEditStatus.NoChange)
        {
            throw new OdfEditException(
                "Could not replace embedded package content '" + path + "'.");
        }
    }

    static void RefreshDocumentPackage(OdfDocument document)
    {
        OdfPackage package = document.Package ?? new OdfPackage();
        document.PackageEntries = package.Entries;
        document.Manifest = package.Manifest;
    }

    static OdfNode? FindDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (IsElement(node, namespaceName, localName))
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? found = FindDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (found != null)
            {
                return found;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(node.Children[index], namespaceName, localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool ContainsNode(OdfNode root, OdfNode target)
    {
        if (root == target)
        {
            return true;
        }
        int index = 0;
        while (index < root.Children.Length)
        {
            if (ContainsNode(root.Children[index], target))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static string TextContent(OdfNode? node)
    {
        if (node == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(node.Children[index], result);
            index = index + 1;
        }
    }

    static void SetTextContent(OdfNode node, string value)
    {
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static string ResourcePath(string reference)
    {
        int question = reference.IndexOf('?');
        int fragment = reference.IndexOf('#');
        int end = reference.Length;
        if (question >= 0 && question < end)
        {
            end = question;
        }
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }
        return reference.Substring(0, end);
    }

    static string NormalizeMediaType(string value)
    {
        string lower = value.Trim().ToLowerInvariant();
        if (lower == "image/jpg" || lower == "image/pjpeg")
        {
            return OdfMediaTypes.Jpeg;
        }
        if (lower == "image/x-png")
        {
            return OdfMediaTypes.Png;
        }
        return lower;
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original)
    {
        if (value == original)
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

    static void CaptureOriginalValues(OdfEmbeddedObject item)
    {
        item.OriginalHref = item.Href;
        item.OriginalClassId = item.ClassId;
        item.OriginalNotifyOnUpdateOfRanges = item.NotifyOnUpdateOfRanges;
        item.OriginalXLinkType = item.XLinkType;
        item.OriginalShow = item.Show;
        item.OriginalActuate = item.Actuate;
    }

    static bool HasEncryptedEntry(OdfPackageEntry[] entries)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].IsEncrypted)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasEncryptedPackageContent(
        OdfDocument document,
        string path,
        OdfPackageEntry[] entries)
    {
        if (HasEncryptedEntry(entries))
        {
            return true;
        }
        if (document.Package == null || document.Package.EntryIndex == null)
        {
            return false;
        }
        PackagePartCatalog catalog = document.Package.EntryIndex.DiscoverParts();
        bool directory = path.EndsWith(
            "/",
            global::System.StringComparison.Ordinal);
        int index = 0;
        while (index < catalog.AllParts.Length)
        {
            string candidate = catalog.AllParts[index].Entry.Path;
            if (catalog.AllParts[index].IsEncrypted
                && (candidate == path
                    || directory && candidate.StartsWith(
                        path,
                        global::System.StringComparison.Ordinal)))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void CaptureChartOriginalValues(OdfChart chart)
    {
        chart.OriginalClassName = chart.ClassName;
        chart.OriginalStyleName = chart.StyleName;
        chart.OriginalHref = chart.Href;
        chart.OriginalXLinkType = chart.XLinkType;
        chart.OriginalWidth = chart.Width;
        chart.OriginalHeight = chart.Height;
    }

    static byte[] CopyBytes(byte[] data)
    {
        byte[] result = new byte[data.Length];
        global::System.Array.Copy(data, result, data.Length);
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
