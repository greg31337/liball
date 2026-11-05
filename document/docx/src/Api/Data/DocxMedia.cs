using System.IO;

namespace docx;

/// <summary>
/// Manages image parts and their relationships in a DOCX package.
/// </summary>
public class DocxMedia
{
    internal DocxPackage? Package;
    /// <summary>
    /// The discovered image parts.
    /// </summary>
    public List<DocxImage> Images;

    /// <summary>
    /// Creates a media instance.
    /// </summary>
    public DocxMedia()
    {
        this.Package = null;
        this.Images = new List<DocxImage>();
    }

    internal static DocxMedia Discover(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxMedia media = new DocxMedia();
        media.SetPackage(package);
        media.DiscoverImages(package);
        return media;
    }

    internal void SetPackage(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        this.Package = package;
    }

    internal int DiscoverImages(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        this.Images = new List<DocxImage>();

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            DiscoverImagesFromRelationshipSet(package, relationshipSet);
        }

        return this.Images.Count;
    }

    internal void DiscoverImagesFromRelationshipSet(DocxPackage package, DocxRelationshipSet relationshipSet)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeImage && !relationship.IsExternal)
            {
                AddImageFromRelationship(package, relationshipSet, relationship);
            }
        }
    }

    internal bool AddImageFromRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, DocxRelationship relationship)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (relationship.Type != DocxConstants.RelationshipTypeImage || relationship.IsExternal)
        {
            return false;
        }

        string partName = relationshipSet.ResolveTargetPartName(relationship);
        DocxPart? part = package.FindPart(partName);

        if (part == null)
        {
            return false;
        }

        string contentType = part.ContentType;
        byte[] bytes = part.GetBytes();

        if (contentType == "")
        {
            contentType = DocxImage.DetectContentType(bytes);
        }

        DocxImage image = DocxImage.Create(
            relationshipSet.SourcePartName,
            part.PartName,
            relationship.Id,
            contentType,
            bytes);

        this.Images.Add(image);
        return true;
    }

    internal DocxImage AddImage(byte[] bytes, string contentType)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        string resolvedContentType = ResolveImageContentType(bytes, contentType);
        string partName = AllocateImagePartName(resolvedContentType);

        this.Package.AddBinaryPart(partName, resolvedContentType, bytes);

        DocxImage image = DocxImage.Create("", partName, "", resolvedContentType, bytes);
        this.Images.Add(image);
        return image;
    }

    internal DocxImage AddImageFromStream(Stream stream, string contentType)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        return AddImage(ReadAllBytes(stream), contentType);
    }

    internal DocxImage AddImageWithRelationship(byte[] bytes, string contentType, string sourcePartName)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        DocxImage image = AddImage(bytes, contentType);
        CreateImageRelationship(image, sourcePartName);
        return image;
    }

    internal DocxRelationship CreateImageRelationship(DocxImage image, string sourcePartName)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        if (image.PartName == "")
        {
            throw new ArgumentException("Image part name is required.", "image");
        }

        if (!this.Package.ContainsPart(image.PartName))
        {
            throw new InvalidOperationException("Image part is missing from the package.");
        }

        DocxRelationshipSet relationshipSet = GetOrCreateRelationshipSet(sourcePartName);
        DocxRelationship? existingRelationship = FindImageRelationship(relationshipSet, image);

        if (existingRelationship != null)
        {
            image.SetPartInfo(relationshipSet.SourcePartName, image.PartName, existingRelationship.Id, image.ContentType);
            WriteRelationshipSetPart(relationshipSet);
            return existingRelationship;
        }

        string target = GetRelationshipTarget(relationshipSet.SourcePartName, image.PartName);
        DocxRelationship relationship = relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeImage, target, false);
        image.SetPartInfo(relationshipSet.SourcePartName, image.PartName, relationship.Id, image.ContentType);
        WriteRelationshipSetPart(relationshipSet);
        return relationship;
    }

    internal DocxRelationship? FindImageRelationship(DocxRelationshipSet relationshipSet, DocxImage image)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeImage && !relationship.IsExternal)
            {
                string partName = relationshipSet.ResolveTargetPartName(relationship);

                if (partName == image.PartName)
                {
                    return relationship;
                }
            }
        }

        return null;
    }

    internal DocxRelationshipSet GetOrCreateRelationshipSet(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        string normalizedSourcePartName = DocxPartName.Normalize(sourcePartName);

        if (!this.Package.ContainsPart(normalizedSourcePartName))
        {
            throw new InvalidOperationException("Relationship source part is missing from the package.");
        }

        DocxRelationshipSet? relationshipSet = this.Package.FindRelationshipSet(normalizedSourcePartName);

        if (relationshipSet != null)
        {
            return relationshipSet;
        }

        relationshipSet = new DocxRelationshipSet(normalizedSourcePartName);
        this.Package.RelationshipSets.Add(relationshipSet);
        return relationshipSet;
    }

    internal void WriteRelationshipSetPart(DocxRelationshipSet relationshipSet)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        string relationshipPartName = "";

        if (relationshipSet.SourcePartName == "/")
        {
            relationshipPartName = "/_rels/.rels";
        }
        else
        {
            relationshipPartName = DocxRelationshipSet.GetRelationshipPartName(relationshipSet.SourcePartName);
        }

        string xml = relationshipSet.WriteXml();

        if (this.Package.ContainsPart(relationshipPartName))
        {
            DocxPart? part = this.Package.FindPart(relationshipPartName);

            if (part != null)
            {
                xml = DocxXmlPreservation.PreserveRelationshipContent(part.GetText(), xml);
            }

            this.Package.ReplaceXmlPart(relationshipPartName, DocxConstants.ContentTypeRelationships, xml);
        }
        else
        {
            this.Package.AddXmlPartWithContent(relationshipPartName, DocxConstants.ContentTypeRelationships, xml);
        }
    }

    internal bool ReplaceImage(DocxImage image, byte[] bytes, string contentType)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        string resolvedContentType = ResolveImageContentType(bytes, contentType);

        if (image.PartName == "")
        {
            return false;
        }

        bool replaced = this.Package.ReplaceBinaryPart(image.PartName, resolvedContentType, bytes);

        if (!replaced)
        {
            return false;
        }

        image.ContentType = resolvedContentType;
        image.ReplaceBytes(bytes);
        return true;
    }

    internal bool ReplaceImageBytes(DocxImage image, byte[] bytes)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return ReplaceImage(image, bytes, image.ContentType);
    }

    internal DocxMediaImportMap ImportImages(DocxMedia media, string targetSourcePartName)
    {
        if (media == null)
        {
            throw new ArgumentNullException("media");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        DocxMediaImportMap map = new DocxMediaImportMap();

        foreach (DocxImage image in media.Images)
        {
            ImportImageWithMap(image, targetSourcePartName, map);
        }

        return map;
    }

    internal DocxMediaImportMap ImportImage(DocxImage image, string targetSourcePartName)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        DocxMediaImportMap map = new DocxMediaImportMap();
        ImportImageWithMap(image, targetSourcePartName, map);
        return map;
    }

    internal DocxImage ImportImageWithMap(DocxImage image, string targetSourcePartName, DocxMediaImportMap map)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        if (map == null)
        {
            throw new ArgumentNullException("map");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        DocxImage importedImage = AddImage(image.GetBytes(), image.ContentType);
        CreateImageRelationship(importedImage, targetSourcePartName);
        map.Add(
            image.SourcePartName,
            image.RelationshipId,
            importedImage.SourcePartName,
            importedImage.RelationshipId,
            image.PartName,
            importedImage.PartName);
        return importedImage;
    }

    internal string AllocateImagePartName(string contentType)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        string extension = GetImageExtension(contentType);
        int index = 1;

        while (true)
        {
            if (!ImagePartIndexExists(index))
            {
                return "/word/media/image" + index.ToString() + "." + extension;
            }

            index = index + 1;
        }
    }

    internal bool ImagePartIndexExists(int index)
    {
        if (this.Package == null)
        {
            throw new InvalidOperationException("Media is not attached to a package.");
        }

        return this.Package.ContainsPart("/word/media/image" + index.ToString() + ".png")
            || this.Package.ContainsPart("/word/media/image" + index.ToString() + ".jpg")
            || this.Package.ContainsPart("/word/media/image" + index.ToString() + ".jpeg")
            || this.Package.ContainsPart("/word/media/image" + index.ToString() + ".gif");
    }

    internal static string ResolveImageContentType(byte[] bytes, string contentType)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType != "")
        {
            return contentType;
        }

        string detectedContentType = DocxImage.DetectContentType(bytes);

        if (detectedContentType == "")
        {
            throw new ArgumentException("Image content type is required when it cannot be detected.", "contentType");
        }

        return detectedContentType;
    }

    internal static string GetImageExtension(string contentType)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (contentType == DocxConstants.ContentTypePng)
        {
            return "png";
        }

        if (contentType == DocxConstants.ContentTypeJpeg)
        {
            return "jpg";
        }

        if (contentType == DocxConstants.ContentTypeGif)
        {
            return "gif";
        }

        throw new ArgumentException("Unsupported image content type.", "contentType");
    }

    internal static string GetRelationshipTarget(string sourcePartName, string targetPartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        string sourceBaseDirectory = DocxRelationshipSet.GetSourceBaseDirectory(sourcePartName);
        string normalizedTargetPartName = DocxPartName.Normalize(targetPartName);

        if (normalizedTargetPartName.StartsWith(sourceBaseDirectory))
        {
            return normalizedTargetPartName.Substring(sourceBaseDirectory.Length);
        }

        return normalizedTargetPartName;
    }

    internal static byte[] ReadAllBytes(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        using (MemoryStream memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }

    internal int ImageCount()
    {
        return this.Images.Count;
    }

    internal DocxImage GetImage(int index)
    {
        return this.Images[index];
    }

    internal DocxImage? FindImage(string sourcePartName, string relationshipId)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        string normalizedSourcePartName = DocxImage.NormalizeSourcePartName(sourcePartName);

        foreach (DocxImage image in this.Images)
        {
            if (image.SourcePartName == normalizedSourcePartName && image.RelationshipId == relationshipId)
            {
                return image;
            }
        }

        return null;
    }
}
