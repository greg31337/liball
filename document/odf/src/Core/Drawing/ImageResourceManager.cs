using System.Collections.Generic;
using System.IO;
using System.Text;

namespace odf;

internal static class ImageResourceManager
{
    internal static void LoadImages(OdfDocument document)
    {
        List<OdfImage> images = new List<OdfImage>();
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            drawing.Image = null;
            if (drawing.Kind == OdfDrawingKind.Image && drawing.Node != null)
            {
                OdfImage image = CreateDrawingImage(document, drawing);
                drawing.Image = image;
                images.Add(image);
            }
            index = index + 1;
        }
        AddUnreferencedPackageImages(document, images);
        document.Images = images.ToArray();
    }

    internal static void SynchronizeImages(OdfDocument document)
    {
        int index = 0;
        while (index < document.Images.Length)
        {
            OdfImage image = document.Images[index];
            if (image == null)
            {
                throw new OdfDocumentException(
                    "The document image collection contains a null image.");
            }
            SynchronizeImage(document, image);
            index = index + 1;
        }
        LoadImages(document);
    }

    internal static OdfImage AddPackageImage(
        OdfDocument document,
        string path,
        byte[] data,
        string declaredMediaType,
        bool deduplicate)
    {
        RequirePackage(document);
        ImageInspection inspection = RequireImageData(
            data,
            declaredMediaType);
        if (deduplicate)
        {
            OdfImage? duplicate = FindDuplicatePackageImage(document, data);
            if (duplicate != null)
            {
                return duplicate;
            }
        }
        OdfPackage package = document.Package ?? new OdfPackage();
        OdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult edit = writer.AddPackageEntry(
            package,
            path,
            data,
            OdfCompressionMethod.Deflated);
        RequireEdit(edit, "add", path);
        SetManifestMediaType(
            package.Manifest,
            path,
            SelectedMediaType(declaredMediaType, inspection.MediaType));
        SynchronizeManifest(package);
        RefreshDocumentPackage(document);
        OdfImage result = CreatePackageImage(
            document,
            path,
            SelectedMediaType(declaredMediaType, inspection.MediaType),
            null);
        document.Images = AppendImage(document.Images, result);
        return result;
    }

    internal static OdfEditResult ReplaceImageData(
        OdfDocument document,
        OdfImage image,
        byte[] data,
        string declaredMediaType)
    {
        ImageInspection inspection = RequireImageData(
            data,
            declaredMediaType);
        string mediaType = SelectedMediaType(
            declaredMediaType,
            inspection.MediaType);
        if (image.SourceKind == OdfImageSourceKind.Package)
        {
            RequirePackage(document);
            OdfPackage package = document.Package ?? new OdfPackage();
            OdfPackageWriter writer = new OdfPackageWriter();
            OdfEditResult result = writer.ReplacePackageEntry(
                package,
                image.PackagePath,
                data);
            RequireEdit(result, "replace", image.PackagePath);
            SetManifestMediaType(
                package.Manifest,
                image.PackagePath,
                mediaType);
            SynchronizeManifest(package);
            RefreshDocumentPackage(document);
            image.Data = CopyBytes(data);
            image.OriginalData = CopyBytes(data);
            ApplyInspection(image, inspection, mediaType);
            return result;
        }
        if (image.SourceKind == OdfImageSourceKind.Base64)
        {
            image.Data = CopyBytes(data);
            image.DeclaredMediaType = mediaType;
            image.DetectedMediaType = inspection.MediaType;
            image.DetectedFormat = inspection.Format;
            image.IsValid = true;
            image.HasMediaTypeMismatch = false;
            return CreateEditResult(
                OdfEditStatus.Success,
                true,
                "Replaced inline image data.",
                image.PackagePath);
        }
        return CreateEditResult(
            OdfEditStatus.ValidationFailed,
            false,
            "External image data cannot be replaced without fetching the external resource.",
            image.Href);
    }

    internal static byte[] ExtractImageData(
        OdfDocument document,
        OdfImage image)
    {
        if (image.SourceKind == OdfImageSourceKind.External)
        {
            throw new OdfEditException(
                "External images are never fetched during extraction.");
        }
        if (image.SourceKind == OdfImageSourceKind.Package)
        {
            RequirePackage(document);
            return ReadPackageBytes(document, image.PackagePath);
        }
        if (image.SourceKind == OdfImageSourceKind.Base64)
        {
            if (!image.IsValid)
            {
                throw new OdfEditException(
                    "Invalid inline base64 image data cannot be extracted.");
            }
            return CopyBytes(image.Data);
        }
        throw new OdfEditException("The image has no extractable local source.");
    }

    internal static OdfImage? FindDuplicatePackageImage(
        OdfDocument document,
        byte[] data)
    {
        int index = 0;
        while (index < document.Images.Length)
        {
            OdfImage image = document.Images[index];
            if (image != null
                && image.SourceKind == OdfImageSourceKind.Package
                && BytesMatch(image.Data, data))
            {
                return image;
            }
            index = index + 1;
        }
        return null;
    }

    internal static void SetExternalImage(
        OdfImage image,
        string href,
        string declaredMediaType)
    {
        if (!OdfIriResolver.IsAbsoluteIri(href))
        {
            throw new OdfEditException(
                "An external image requires an absolute IRI.");
        }
        image.SourceKind = OdfImageSourceKind.External;
        image.Href = href;
        image.PackagePath = "";
        image.DeclaredMediaType = declaredMediaType;
        image.DetectedMediaType = "";
        image.DetectedFormat = OdfImageFormat.Unknown;
        image.Data = [];
        image.Base64Value = "";
        image.IsValid = true;
        image.HasMediaTypeMismatch = false;
    }

    internal static void SetBase64Image(
        OdfImage image,
        byte[] data,
        string declaredMediaType)
    {
        ImageInspection inspection = RequireImageData(
            data,
            declaredMediaType);
        image.SourceKind = OdfImageSourceKind.Base64;
        image.Href = "";
        image.PackagePath = "";
        image.DeclaredMediaType = SelectedMediaType(
            declaredMediaType,
            inspection.MediaType);
        image.DetectedMediaType = inspection.MediaType;
        image.DetectedFormat = inspection.Format;
        image.Data = CopyBytes(data);
        image.Base64Value = global::System.Convert.ToBase64String(data);
        image.IsValid = true;
        image.HasMediaTypeMismatch = false;
    }

    static OdfImage CreateDrawingImage(
        OdfDocument document,
        OdfDrawingObject drawing)
    {
        OdfNode node = drawing.Node ?? new OdfNode();
        OdfImage result = new OdfImage();
        result.Drawing = drawing;
        result.Node = node;
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.XLinkType = Read(node, OdfNamespaces.XLink, "type");
        result.Show = Read(node, OdfNamespaces.XLink, "show");
        result.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        result.DeclaredMediaType = Read(
            node,
            OdfNamespaces.Drawing,
            "mime-type");
        result.BinaryDataNode = FindDirectChild(
            node,
            OdfNamespaces.Office,
            "binary-data");
        if (result.BinaryDataNode != null)
        {
            LoadBase64Image(result);
        }
        else if (result.Href.Length > 0
            && document.PackageKind == OdfPackageKind.Package
            && OdfIriResolver.IsPackageRelativeReference(result.Href))
        {
            LoadPackageReference(document, drawing, result);
        }
        else if (result.Href.Length > 0)
        {
            result.SourceKind = OdfImageSourceKind.External;
            result.IsValid = OdfIriResolver.IsIriReference(result.Href);
        }
        CaptureOriginalValues(result);
        return result;
    }

    static OdfImage CreatePackageImage(
        OdfDocument document,
        string path,
        string declaredMediaType,
        OdfDrawingObject? drawing)
    {
        OdfImage result = new OdfImage();
        result.SourceKind = OdfImageSourceKind.Package;
        result.Href = path;
        result.PackagePath = path;
        result.DeclaredMediaType = declaredMediaType;
        result.Drawing = drawing;
        result.Data = ReadPackageBytes(document, path);
        ImageInspection inspection = ImageSniffer.Inspect(result.Data);
        ApplyInspection(result, inspection, declaredMediaType);
        CaptureOriginalValues(result);
        return result;
    }

    static void LoadBase64Image(OdfImage image)
    {
        image.SourceKind = OdfImageSourceKind.Base64;
        image.Base64Value = TextContent(image.BinaryDataNode);
        OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(
            image.Base64Value);
        if (!parsed.IsValid)
        {
            image.IsValid = false;
            return;
        }
        image.Data = CopyBytes(parsed.BinaryValue);
        ImageInspection inspection = ImageSniffer.Inspect(image.Data);
        ApplyInspection(image, inspection, image.DeclaredMediaType);
    }

    static void LoadPackageReference(
        OdfDocument document,
        OdfDrawingObject drawing,
        OdfImage image)
    {
        image.SourceKind = OdfImageSourceKind.Package;
        string resolved = PackagePathRules.ResolveRelativeReference(
            drawing.PartPath,
            image.Href);
        image.PackagePath = ResourcePath(resolved);
        if (image.PackagePath.Length == 0
            || !HasPackageEntry(document, image.PackagePath)
            || !IsPackageContentAvailable(document, image.PackagePath))
        {
            image.IsValid = false;
            return;
        }
        if (image.DeclaredMediaType.Length == 0)
        {
            image.DeclaredMediaType = ManifestMediaType(
                document.Manifest,
                image.PackagePath);
        }
        image.Data = ReadPackageBytes(document, image.PackagePath);
        ImageInspection inspection = ImageSniffer.Inspect(image.Data);
        ApplyInspection(image, inspection, image.DeclaredMediaType);
    }

    static void AddUnreferencedPackageImages(
        OdfDocument document,
        List<OdfImage> images)
    {
        if (document.Package == null)
        {
            return;
        }
        PackagePartCatalog catalog = document.Package.EntryIndex == null
            ? new PackagePartCatalog()
            : document.Package.EntryIndex.DiscoverParts();
        int index = 0;
        while (index < catalog.ImageParts.Length)
        {
            DiscoveredPackagePart part = catalog.ImageParts[index];
            if (!ContainsPackagePath(images, part.Entry.Path)
                && !part.Entry.IsDirectory
                && !part.IsEncrypted
                && IsPackageContentAvailable(document, part.Entry.Path))
            {
                images.Add(CreatePackageImage(
                    document,
                    part.Entry.Path,
                    part.MediaType,
                    null));
            }
            index = index + 1;
        }
    }

    static void SynchronizeImage(OdfDocument document, OdfImage image)
    {
        if (image.Node != null)
        {
            WriteChanged(image.Node, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, image.Href, image.OriginalHref);
            WriteChanged(image.Node, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, image.XLinkType, image.OriginalXLinkType);
            WriteChanged(image.Node, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, image.Show, image.OriginalShow);
            WriteChanged(image.Node, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, image.Actuate, image.OriginalActuate);
            WriteChanged(image.Node, OdfNamespaces.Drawing, "mime-type", OdfNamespaces.DrawingPrefix, image.DeclaredMediaType, image.OriginalDeclaredMediaType);
        }
        if (image.SourceKind != OdfImageSourceKind.Base64
            && image.BinaryDataNode != null
            && image.Node != null)
        {
            RemoveChild(image.Node, image.BinaryDataNode);
            image.BinaryDataNode = null;
        }
        if (image.SourceKind == OdfImageSourceKind.Base64)
        {
            SynchronizeBase64Image(image);
        }
        else if (image.SourceKind == OdfImageSourceKind.Package
            && !BytesMatch(image.Data, image.OriginalData))
        {
            OdfEditResult result = ReplaceImageData(
                document,
                image,
                image.Data,
                image.DeclaredMediaType);
            RequireEdit(result, "replace", image.PackagePath);
        }
        CaptureOriginalValues(image);
    }

    static void SynchronizeBase64Image(OdfImage image)
    {
        string value = image.Base64Value;
        if (!BytesMatch(image.Data, image.OriginalData)
            && value == image.OriginalBase64Value)
        {
            ImageInspection inspection = RequireImageData(
                image.Data,
                image.DeclaredMediaType);
            value = global::System.Convert.ToBase64String(image.Data);
            image.Base64Value = value;
            ApplyInspection(image, inspection, image.DeclaredMediaType);
        }
        else if (value != image.OriginalBase64Value)
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(value);
            image.IsValid = parsed.IsValid;
            if (parsed.IsValid)
            {
                image.Data = CopyBytes(parsed.BinaryValue);
                ImageInspection inspection = ImageSniffer.Inspect(image.Data);
                ApplyInspection(image, inspection, image.DeclaredMediaType);
            }
        }
        OdfNode? binary = image.BinaryDataNode;
        if (binary == null && image.Node != null && value.Length > 0)
        {
            binary = CreateBinaryDataNode(image.Node.Location.PartPath);
            AppendChild(image.Node, binary);
            image.BinaryDataNode = binary;
        }
        if (binary != null && value != image.OriginalBase64Value)
        {
            SetTextContent(binary, value);
        }
    }

    static ImageInspection RequireImageData(
        byte[] data,
        string declaredMediaType)
    {
        ImageInspection inspection = ImageSniffer.Inspect(data);
        if (!inspection.IsRecognized)
        {
            throw new OdfEditException(
                "Image data does not have a recognized safe image signature.");
        }
        if (declaredMediaType.Length > 0
            && !MediaTypesMatch(declaredMediaType, inspection.MediaType))
        {
            throw new OdfEditException(
                "The declared image media type does not match the detected image data.");
        }
        return inspection;
    }

    static void ApplyInspection(
        OdfImage image,
        ImageInspection inspection,
        string declaredMediaType)
    {
        image.DetectedFormat = inspection.Format;
        image.DetectedMediaType = inspection.MediaType;
        image.IsValid = inspection.IsRecognized;
        image.HasMediaTypeMismatch = declaredMediaType.Length > 0
            && inspection.MediaType.Length > 0
            && !MediaTypesMatch(declaredMediaType, inspection.MediaType);
    }

    static bool MediaTypesMatch(string first, string second)
    {
        string normalizedFirst = NormalizeMediaType(first);
        string normalizedSecond = NormalizeMediaType(second);
        return normalizedFirst == normalizedSecond;
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
        if (lower == "image/x-ms-bmp")
        {
            return OdfMediaTypes.Bmp;
        }
        return lower;
    }

    static string SelectedMediaType(string declared, string detected)
    {
        return declared.Length == 0 ? detected : declared;
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

    static void SynchronizeManifest(OdfPackage package)
    {
        OdfManifestService service = new OdfManifestService();
        OdfEditResult result = service.SynchronizePackageManifest(package);
        if (result.Status != OdfEditStatus.Success
            && result.Status != OdfEditStatus.NoChange)
        {
            throw new OdfEditException(
                "The package manifest could not be synchronized for an image resource.");
        }
    }

    static void RefreshDocumentPackage(OdfDocument document)
    {
        OdfPackage package = document.Package ?? new OdfPackage();
        document.PackageEntries = package.Entries;
        document.Manifest = package.Manifest;
    }

    static void RequirePackage(OdfDocument document)
    {
        if (document.PackageKind != OdfPackageKind.Package
            || document.Package == null)
        {
            throw new OdfEditException(
                "A package document is required for package image resources.");
        }
    }

    static void RequireEdit(
        OdfEditResult result,
        string operation,
        string path)
    {
        if (result.Status != OdfEditStatus.Success
            && result.Status != OdfEditStatus.NoChange)
        {
            throw new OdfEditException(
                "Could not " + operation + " image resource '" + path + "'.");
        }
    }

    static OdfEditResult CreateEditResult(
        OdfEditStatus status,
        bool changed,
        string message,
        string path)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = changed ? 1 : 0;
        result.Message = message;
        result.Location.PartPath = path;
        return result;
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

    static bool HasPackageEntry(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            if (document.PackageEntries[index].Path == path
                && !document.PackageEntries[index].IsDirectory)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsPackageContentAvailable(
        OdfDocument document,
        string path)
    {
        if (document.Package == null
            || document.Package.EntryIndex == null)
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

    static bool ContainsPackagePath(
        List<OdfImage> images,
        string path)
    {
        int index = 0;
        while (index < images.Count)
        {
            if (images[index].SourceKind == OdfImageSourceKind.Package
                && images[index].PackagePath == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
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

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == namespaceName
                && child.Name.LocalName == localName)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode CreateBinaryDataNode(string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Office;
        result.Name.LocalName = "binary-data";
        result.Name.Prefix = OdfNamespaces.OfficePrefix;
        result.Location.PartPath = partPath;
        return result;
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

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        global::System.Array.Copy(
            parent.Children,
            children,
            parent.Children.Length);
        children[children.Length - 1] = child;
        child.Parent = parent;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != child)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        child.Parent = null;
        parent.Children = children.ToArray();
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

    static void CaptureOriginalValues(OdfImage image)
    {
        image.OriginalHref = image.Href;
        image.OriginalDeclaredMediaType = image.DeclaredMediaType;
        image.OriginalBase64Value = image.Base64Value;
        image.OriginalXLinkType = image.XLinkType;
        image.OriginalShow = image.Show;
        image.OriginalActuate = image.Actuate;
        image.OriginalData = CopyBytes(image.Data);
    }

    static OdfImage[] AppendImage(OdfImage[] images, OdfImage image)
    {
        OdfImage[] result = new OdfImage[images.Length + 1];
        global::System.Array.Copy(images, result, images.Length);
        result[result.Length - 1] = image;
        return result;
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
