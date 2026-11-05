using NUnit.Framework;
using System.IO;

namespace docx.Tests;

[TestFixture]
public class DocxMediaTests
{
    [Test]
    public void MediaDiscovery_FindsInternalImageRelationshipsAcrossDocumentParts()
    {
        global::docx.DocxPackage package = CreatePackageWithImages();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.ImageCount(), Is.EqualTo(2));
        Assert.That(document.GetMedia().ImageCount(), Is.EqualTo(2));
        Assert.That(document.GetImage(0).SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(document.GetImage(0).PartName, Is.EqualTo("/word/media/image1.png"));
        Assert.That(document.GetImage(0).RelationshipId, Is.EqualTo("rImageMain"));
        Assert.That(document.GetImage(0).ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(document.GetImage(0).ByteCount(), Is.EqualTo(3));
        Assert.That(document.GetImage(1).SourcePartName, Is.EqualTo("/word/header1.xml"));
        Assert.That(document.GetImage(1).PartName, Is.EqualTo("/word/media/header-image.gif"));
        Assert.That(document.GetImage(1).RelationshipId, Is.EqualTo("rImageHeader"));
        Assert.That(document.GetImage(1).ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeGif));
        Assert.That(document.FindImage("/word/document.xml", "rImageMain"), Is.SameAs(document.GetImage(0)));
        Assert.That(document.FindImage("word/header1.xml", "rImageHeader"), Is.SameAs(document.GetImage(1)));
        Assert.That(document.FindImage("/word/document.xml", "rMissing"), Is.Null);
        Assert.That(document.FindImage("/word/document.xml", "rExternal"), Is.Null);
    }

    [Test]
    public void MediaDiscovery_ReturnsFalseForMissingExternalAndNonImageRelationships()
    {
        global::docx.DocxPackage package = CreatePackageWithImages();
        global::docx.DocxMedia media = new global::docx.DocxMedia();
        global::docx.DocxRelationshipSet relationships = package.FindRelationshipSet("/word/document.xml")!;

        Assert.That(media.AddImageFromRelationship(package, relationships, relationships.FindById("rImageMain")!), Is.True);
        Assert.That(media.AddImageFromRelationship(package, relationships, relationships.FindById("rMissing")!), Is.False);
        Assert.That(media.AddImageFromRelationship(package, relationships, relationships.FindById("rExternal")!), Is.False);
        Assert.That(media.AddImageFromRelationship(package, relationships, relationships.FindById("rHeader1")!), Is.False);
        Assert.That(media.ImageCount(), Is.EqualTo(1));
    }

    [Test]
    public void ImageModel_CopiesBytesAndNormalizesPartInfo()
    {
        byte[] bytes = new byte[] { 1, 2, 3 };
        global::docx.DocxImage image = global::docx.DocxImage.Create("word/document.xml", "word/media/image1.png", "rId1", global::docx.DocxConstants.ContentTypePng, bytes);

        bytes[0] = 9;
        byte[] copy = image.GetBytes();
        copy[1] = 8;

        Assert.That(image.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(image.PartName, Is.EqualTo("/word/media/image1.png"));
        Assert.That(image.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(image.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(image.GetBytes()[0], Is.EqualTo(1));
        Assert.That(image.GetBytes()[1], Is.EqualTo(2));

        image.SetPartInfo("/", "/media/root.png", "rRoot", global::docx.DocxConstants.ContentTypePng);
        image.SetBytes(new byte[] { 5 });

        Assert.That(image.SourcePartName, Is.EqualTo("/"));
        Assert.That(image.PartName, Is.EqualTo("/media/root.png"));
        Assert.That(image.RelationshipId, Is.EqualTo("rRoot"));
        Assert.That(image.ByteCount(), Is.EqualTo(1));
        Assert.That(image.GetBytes()[0], Is.EqualTo(5));
    }

    [Test]
    public void ImageContentTypeDetection_ReadsKnownFileHeaders()
    {
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0 };
        byte[] jpeg = new byte[] { 255, 216, 255, 224, 0 };
        byte[] gif87 = new byte[] { 71, 73, 70, 56, 55, 97, 0 };
        byte[] gif89 = new byte[] { 71, 73, 70, 56, 57, 97, 0 };

        Assert.That(global::docx.DocxImage.HasPngHeader(png), Is.True);
        Assert.That(global::docx.DocxImage.HasJpegHeader(jpeg), Is.True);
        Assert.That(global::docx.DocxImage.HasGifHeader(gif87), Is.True);
        Assert.That(global::docx.DocxImage.HasGifHeader(gif89), Is.True);
        Assert.That(global::docx.DocxImage.DetectContentType(png), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(global::docx.DocxImage.DetectContentType(jpeg), Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
        Assert.That(global::docx.DocxImage.DetectContentType(gif87), Is.EqualTo(global::docx.DocxConstants.ContentTypeGif));
        Assert.That(global::docx.DocxImage.DetectContentType(gif89), Is.EqualTo(global::docx.DocxConstants.ContentTypeGif));
    }

    [Test]
    public void ImageContentTypeDetection_ReturnsEmptyForUnknownOrShortBytes()
    {
        Assert.That(global::docx.DocxImage.DetectContentType(new byte[0]), Is.EqualTo(""));
        Assert.That(global::docx.DocxImage.DetectContentType(new byte[] { 137, 80, 78 }), Is.EqualTo(""));
        Assert.That(global::docx.DocxImage.DetectContentType(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }), Is.EqualTo(""));
        Assert.That(global::docx.DocxImage.HasPngHeader(new byte[] { 137, 80, 78 }), Is.False);
        Assert.That(global::docx.DocxImage.HasJpegHeader(new byte[] { 255, 216 }), Is.False);
        Assert.That(global::docx.DocxImage.HasGifHeader(new byte[] { 71, 73, 70, 56, 57 }), Is.False);
    }

    [Test]
    public void ImageContentTypeDetection_UpdatesImageContentTypeWhenKnown()
    {
        global::docx.DocxImage image = global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.jpg", "rId1", "", new byte[] { 255, 216, 255, 225 });

        Assert.That(image.DetectContentType(), Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));

        image.SetDetectedContentType();

        Assert.That(image.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));

        image.SetBytes(new byte[] { 1, 2, 3 });
        image.SetDetectedContentType();

        Assert.That(image.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
    }

    [Test]
    public void MediaDiscovery_UsesDetectedContentTypeWhenPackageMetadataIsMissing()
    {
        global::docx.DocxPackage package = CreatePackageWithImageMissingContentType();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.ImageCount(), Is.EqualTo(1));
        Assert.That(document.GetImage(0).PartName, Is.EqualTo("/word/media/image1.jpg"));
        Assert.That(document.GetImage(0).ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
    }

    [Test]
    public void AddImage_AddsPackagePartAndImageModel()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        byte[] gif = new byte[] { 71, 73, 70, 56, 57, 97 };

        global::docx.DocxImage firstImage = document.AddImage(png, "");
        global::docx.DocxImage secondImage = document.AddImageFromStream(new MemoryStream(gif), global::docx.DocxConstants.ContentTypeGif);

        Assert.That(document.ImageCount(), Is.EqualTo(2));
        Assert.That(firstImage.PartName, Is.EqualTo("/word/media/image1.png"));
        Assert.That(firstImage.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(firstImage.SourcePartName, Is.EqualTo(""));
        Assert.That(firstImage.RelationshipId, Is.EqualTo(""));
        Assert.That(firstImage.GetBytes(), Is.EqualTo(png));
        Assert.That(document.Package.FindPart("/word/media/image1.png")!.GetBytes(), Is.EqualTo(png));
        Assert.That(document.Package.ContentTypes.GetContentType("/word/media/image1.png"), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(secondImage.PartName, Is.EqualTo("/word/media/image2.gif"));
        Assert.That(secondImage.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeGif));
        Assert.That(secondImage.GetBytes(), Is.EqualTo(gif));
        Assert.That(document.Package.FindPart("/word/media/image2.gif")!.GetBytes(), Is.EqualTo(gif));
    }

    [Test]
    public void AddImageToMainDocument_WritesImageRelationshipPartAndUpdatesModel()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        global::docx.DocxImage image = document.AddImageToMainDocument(png, "");
        global::docx.DocxRelationshipSet relationshipSet = document.Package.FindRelationshipSet("/word/document.xml")!;
        global::docx.DocxRelationship relationship = relationshipSet.FindById(image.RelationshipId)!;
        global::docx.DocxPart relationshipPart = document.Package.FindPart("/word/_rels/document.xml.rels")!;

        Assert.That(image.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(image.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(image.PartName, Is.EqualTo("/word/media/image1.png"));
        Assert.That(document.FindImage("/word/document.xml", "rId1"), Is.SameAs(image));
        Assert.That(relationship.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeImage));
        Assert.That(relationship.Target, Is.EqualTo("media/image1.png"));
        Assert.That(relationship.IsExternal, Is.False);
        Assert.That(relationshipPart.GetText(), Does.Contain("media/image1.png"));
        Assert.That(relationshipPart.GetText(), Does.Contain(global::docx.DocxConstants.RelationshipTypeImage));

        global::docx.DocxRelationship reused = document.CreateImageRelationship(image, "/word/document.xml");

        Assert.That(reused, Is.SameAs(relationship));
        Assert.That(relationshipSet.Count(), Is.EqualTo(1));
        Assert.That(global::docx.DocxMedia.Discover(document.Package).FindImage("/word/document.xml", "rId1")!.PartName, Is.EqualTo("/word/media/image1.png"));
    }

    [Test]
    public void AddImageWithRelationship_WritesRelationshipForRequestedSourcePart()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxHeader header = document.CreateHeader();
        byte[] gif = new byte[] { 71, 73, 70, 56, 57, 97 };

        global::docx.DocxImage image = document.AddImageWithRelationship(gif, global::docx.DocxConstants.ContentTypeGif, header.PartName);
        global::docx.DocxRelationshipSet relationshipSet = document.Package.FindRelationshipSet(header.PartName)!;
        global::docx.DocxRelationship relationship = relationshipSet.FindById(image.RelationshipId)!;
        global::docx.DocxPart relationshipPart = document.Package.FindPart("/word/_rels/header1.xml.rels")!;

        Assert.That(image.SourcePartName, Is.EqualTo("/word/header1.xml"));
        Assert.That(image.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(image.PartName, Is.EqualTo("/word/media/image1.gif"));
        Assert.That(relationship.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeImage));
        Assert.That(relationship.Target, Is.EqualTo("media/image1.gif"));
        Assert.That(relationshipPart.GetText(), Does.Contain("media/image1.gif"));
        Assert.That(document.FindImage("/word/header1.xml", "rId1"), Is.SameAs(image));
    }

    [Test]
    public void CreateImageRelationship_ReusesExistingRelationshipForSameSourcePart()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxHeader header = document.CreateHeader();
        global::docx.DocxImage image = document.AddImage(new byte[] { 71, 73, 70, 56, 57, 97 }, global::docx.DocxConstants.ContentTypeGif);

        global::docx.DocxRelationship first = document.CreateImageRelationship(image, header.PartName);
        global::docx.DocxRelationship second = document.CreateImageRelationship(image, header.PartName);

        global::docx.DocxRelationshipSet relationshipSet = document.Package.FindRelationshipSet(header.PartName)!;

        Assert.That(second.Id, Is.EqualTo(first.Id));
        Assert.That(relationshipSet.Count(), Is.EqualTo(1));
        Assert.That(image.SourcePartName, Is.EqualTo("/word/header1.xml"));
        Assert.That(image.RelationshipId, Is.EqualTo(first.Id));
        Assert.That(document.FindImage("/word/header1.xml", first.Id), Is.SameAs(image));
        Assert.That(document.Package.FindPart("/word/_rels/header1.xml.rels")!.GetText(), Does.Contain("media/image1.gif"));
    }

    [Test]
    public void MediaImport_CopiesImagesAndRemapsRelationshipsToMainDocument()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.Create();
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreatePackageWithImages());
        byte[] existingPng = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        target.AddImageToMainDocument(existingPng, global::docx.DocxConstants.ContentTypePng);

        global::docx.DocxMediaImportMap map = target.ImportMedia(source);

        Assert.That(map.Count(), Is.EqualTo(2));
        Assert.That(map.GetRelationshipId("/word/document.xml", "rImageMain"), Is.EqualTo("rId2"));
        Assert.That(map.GetRelationshipId("/word/header1.xml", "rImageHeader"), Is.EqualTo("rId3"));
        Assert.That(map.GetRelationshipId("/word/document.xml", "missing"), Is.EqualTo("missing"));
        Assert.That(map.GetImagePartName("/word/media/image1.png"), Is.EqualTo("/word/media/image2.png"));
        Assert.That(map.GetImagePartName("/word/media/header-image.gif"), Is.EqualTo("/word/media/image3.gif"));

        global::docx.DocxImage importedMain = target.FindImage("/word/document.xml", "rId2")!;
        global::docx.DocxImage importedHeader = target.FindImage("/word/document.xml", "rId3")!;
        global::docx.DocxRelationshipSet relationships = target.Package.FindRelationshipSet("/word/document.xml")!;

        Assert.That(target.ImageCount(), Is.EqualTo(3));
        Assert.That(importedMain.PartName, Is.EqualTo("/word/media/image2.png"));
        Assert.That(importedMain.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(importedHeader.PartName, Is.EqualTo("/word/media/image3.gif"));
        Assert.That(importedHeader.GetBytes(), Is.EqualTo(new byte[] { 4, 5 }));
        Assert.That(target.Package.FindPart("/word/media/image2.png")!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(target.Package.FindPart("/word/media/image3.gif")!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeGif));
        Assert.That(relationships.FindById("rId2")!.Target, Is.EqualTo("media/image2.png"));
        Assert.That(relationships.FindById("rId3")!.Target, Is.EqualTo("media/image3.gif"));
        Assert.That(global::docx.DocxMedia.Discover(target.Package).ImageCount(), Is.EqualTo(3));
    }

    [Test]
    public void MediaImport_CanTargetRequestedSourcePart()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.Create();
        global::docx.DocxHeader header = target.CreateHeader();
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreatePackageWithImages());

        global::docx.DocxMediaImportMap map = target.ImportMediaCollectionToPart(source.GetMedia(), header.PartName);

        Assert.That(map.Get(0).TargetSourcePartName, Is.EqualTo("/word/header1.xml"));
        Assert.That(map.GetRelationshipId("/word/document.xml", "rImageMain"), Is.EqualTo("rId1"));
        Assert.That(target.FindImage("/word/header1.xml", "rId1")!.PartName, Is.EqualTo("/word/media/image1.png"));
        Assert.That(target.Package.FindRelationshipSet("/word/header1.xml")!.FindById("rId1")!.Target, Is.EqualTo("media/image1.png"));
    }

    [Test]
    public void ImageRelationshipTarget_UsesRelativeTargetWhenSourceSharesBaseDirectory()
    {
        Assert.That(global::docx.DocxMedia.GetRelationshipTarget("/word/document.xml", "/word/media/image1.png"), Is.EqualTo("media/image1.png"));
        Assert.That(global::docx.DocxMedia.GetRelationshipTarget("/word/header1.xml", "/word/media/image1.png"), Is.EqualTo("media/image1.png"));
        Assert.That(global::docx.DocxMedia.GetRelationshipTarget("/custom/item.xml", "/word/media/image1.png"), Is.EqualTo("/word/media/image1.png"));
    }

    [Test]
    public void ReplaceImage_UpdatesPackagePartAndImageModel()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxImage image = document.AddImage(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, global::docx.DocxConstants.ContentTypePng);
        byte[] replacement = new byte[] { 9, 8, 7 };

        Assert.That(document.ReplaceImage(image, replacement, global::docx.DocxConstants.ContentTypePng), Is.True);

        Assert.That(image.GetBytes(), Is.EqualTo(replacement));
        Assert.That(document.Package.FindPart(image.PartName)!.GetBytes(), Is.EqualTo(replacement));
        Assert.That(document.Package.FindPart(image.PartName)!.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));

        byte[] secondReplacement = new byte[] { 6, 5, 4 };

        Assert.That(document.ReplaceImageBytes(image, secondReplacement), Is.True);
        Assert.That(image.GetBytes(), Is.EqualTo(secondReplacement));
        Assert.That(document.Package.FindPart(image.PartName)!.GetBytes(), Is.EqualTo(secondReplacement));

        global::docx.DocxImage missingImage = global::docx.DocxImage.Create("", "/word/media/missing.png", "", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 });

        Assert.That(document.ReplaceImage(missingImage, new byte[] { 2 }, global::docx.DocxConstants.ContentTypePng), Is.False);
        Assert.That(missingImage.GetBytes(), Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public void ImageReplaceBytes_UpdatesStandaloneImageBytes()
    {
        global::docx.DocxImage image = global::docx.DocxImage.Create("", "/word/media/image1.png", "", global::docx.DocxConstants.ContentTypePng, new byte[] { 1, 2 });

        image.ReplaceBytes(new byte[] { 3, 4, 5 });

        Assert.That(image.GetBytes(), Is.EqualTo(new byte[] { 3, 4, 5 }));
        Assert.That(image.ByteCount(), Is.EqualTo(3));
    }

    [Test]
    public void MediaManagement_DetectsContentTypesAndReturnsDefensiveImageBytes()
    {
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument document = persistence.CreateDocument();
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1 };
        byte[] jpeg = new byte[] { 255, 216, 255, 224 };
        byte[] gif = new byte[] { 71, 73, 70, 56, 57, 97 };

        Assert.That(media.DetectImageContentType(png), Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(media.DetectImageContentType(jpeg), Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
        Assert.That(media.DetectImageContentTypeFromStream(new MemoryStream(gif)), Is.EqualTo(global::docx.DocxConstants.ContentTypeGif));
        Assert.That(media.DetectImageContentType(new byte[] { 1, 2, 3 }), Is.EqualTo(""));

        global::docx.DocxImage image = media.AddImageToMainDocument(document, png, "");
        byte[] copy = media.GetImageBytes(image);
        copy[0] = 0;

        Assert.That(image.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypePng));
        Assert.That(media.GetImageByteCount(image), Is.EqualTo(png.Length));
        Assert.That(media.GetImageBytes(image)[0], Is.EqualTo(137));
        Assert.That(media.GetImages(document), Is.EqualTo(new[] { image }));
        Assert.That(media.FindImage(document, image.SourcePartName, image.RelationshipId), Is.SameAs(image));
    }

    [Test]
    public void MediaManagement_AddsAndReusesRelationshipsAcrossMainHeaderAndFooterParts()
    {
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSection section = sections.CreateSection();
        sections.SetFinalSection(document, section);
        global::docx.DocxHeader header = sections.CreateHeader(document, section, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxFooter footer = sections.CreateFooter(document, section, global::docx.DocxHeaderFooterKind.Default);
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        byte[] gif = new byte[] { 71, 73, 70, 56, 57, 97 };
        global::docx.DocxImage main = media.AddImageStreamToMainDocument(document, new MemoryStream(png), "");
        global::docx.DocxImage headerImage = media.AddImageToHeader(document, header, gif, "");
        global::docx.DocxImage footerImage = media.AddImageToFooter(document, footer, png, "");
        global::docx.DocxImage sharedHeader = media.RelateImageToHeader(document, main, header);
        global::docx.DocxImage sharedFooter = media.RelateImageToFooter(document, main, footer);

        Assert.That(main.SourcePartName, Is.EqualTo("/word/document.xml"));
        Assert.That(sharedHeader.SourcePartName, Is.EqualTo(header.PartName));
        Assert.That(sharedFooter.SourcePartName, Is.EqualTo(footer.PartName));
        Assert.That(sharedHeader.PartName, Is.EqualTo(main.PartName));
        Assert.That(sharedFooter.PartName, Is.EqualTo(main.PartName));
        Assert.That(headerImage.PartName, Is.Not.EqualTo(main.PartName));
        Assert.That(footerImage.PartName, Is.Not.EqualTo(main.PartName));
        Assert.That(media.GetImages(document).Count, Is.EqualTo(5));
        Assert.That(media.RelateImageToHeader(document, main, header), Is.SameAs(sharedHeader));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        Assert.That(media.GetImages(reopened).Count, Is.EqualTo(5));
        Assert.That(media.FindImage(reopened, "/word/header1.xml", sharedHeader.RelationshipId)!.PartName, Is.EqualTo(main.PartName));
        Assert.That(media.FindImage(reopened, "/word/footer1.xml", sharedFooter.RelationshipId)!.PartName, Is.EqualTo(main.PartName));
    }

    [Test]
    public void MediaManagement_StreamReplacementUpdatesEveryRelationshipModel()
    {
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSection section = sections.CreateSection();
        sections.SetFinalSection(document, section);
        global::docx.DocxHeader header = sections.CreateHeader(document, section, global::docx.DocxHeaderFooterKind.Default);
        byte[] original = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        byte[] replacement = new byte[] { 255, 216, 255, 224, 1, 2 };
        global::docx.DocxImage main = media.AddImageToMainDocument(document, original, "");
        global::docx.DocxImage related = media.RelateImageToHeader(document, main, header);

        Assert.That(media.ReplaceImageFromStream(document, related, new MemoryStream(replacement), ""), Is.True);
        Assert.That(main.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
        Assert.That(related.ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
        Assert.That(media.GetImageBytes(main), Is.EqualTo(replacement));
        Assert.That(media.GetImageBytes(related), Is.EqualTo(replacement));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        Assert.That(media.GetImages(reopened).Count, Is.EqualTo(2));
        Assert.That(media.GetImages(reopened)[0].ContentType, Is.EqualTo(global::docx.DocxConstants.ContentTypeJpeg));
        Assert.That(media.GetImageBytes(media.GetImages(reopened)[1]), Is.EqualTo(replacement));
    }

    [Test]
    public void MediaManagement_RejectsImagesOwnedByAnotherDocument()
    {
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxDocument target = persistence.CreateDocument();
        global::docx.DocxDocument source = persistence.CreateDocument();
        byte[] targetBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1 };
        byte[] sourceBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 2 };
        byte[] replacement = new byte[] { 255, 216, 255, 224 };
        global::docx.DocxImage targetImage = media.AddImageToMainDocument(target, targetBytes, "");
        global::docx.DocxImage sourceImage = media.AddImageToMainDocument(source, sourceBytes, "");

        Assert.That(sourceImage.PartName, Is.EqualTo(targetImage.PartName));
        Assert.Throws<ArgumentException>(() => media.RelateImageToPart(target, sourceImage, "/word/document.xml"));
        Assert.Throws<ArgumentException>(() => media.ReplaceImage(target, sourceImage, replacement, ""));
        Assert.That(media.GetImageBytes(targetImage), Is.EqualTo(targetBytes));
        Assert.That(target.Package.FindPart(targetImage.PartName)!.GetBytes(), Is.EqualTo(targetBytes));
        Assert.That(media.GetImageBytes(sourceImage), Is.EqualTo(sourceBytes));
        Assert.That(target.Package.FindRelationshipSet("/word/document.xml")!.Count(), Is.EqualTo(1));
        Assert.That(media.GetImages(target), Is.EqualTo(new[] { targetImage }));
    }

    [Test]
    public void MediaManagement_CreatesAltTextInlineDrawingWithEmuSizingAndRoundTrips()
    {
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxParagraph paragraph = factory.CreateParagraph();
        byte[] png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        long width = media.PixelsToEmu(192, 96);
        long height = media.PixelsToEmu(96, 96);

        global::docx.DocxDrawing drawing = media.AddInlineImageToParagraph(
            document,
            paragraph,
            png,
            "",
            "Logo",
            "Company logo",
            width,
            height);
        document.GetBody().AddBlock(factory.CreateParagraphBlock(paragraph));

        Assert.That(width, Is.EqualTo(1828800));
        Assert.That(height, Is.EqualTo(914400));
        Assert.That(drawing.Description, Is.EqualTo("Company logo"));
        Assert.That(media.IsInlineDrawing(drawing), Is.True);
        Assert.That(media.GetDrawingXml(drawing), Does.Contain("descr=\"Company logo\""));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxDrawing parsed = reopened.GetBody().GetParagraph(0).GetRun(0).GetDrawing(0);
        Assert.That(media.IsInlineDrawing(parsed), Is.True);
        Assert.That(parsed.RelationshipId, Is.EqualTo(media.GetImages(reopened)[0].RelationshipId));
        Assert.That(parsed.Name, Is.EqualTo("Logo"));
        Assert.That(parsed.Description, Is.EqualTo("Company logo"));
        Assert.That(parsed.WidthEmu, Is.EqualTo(1828800));
        Assert.That(parsed.HeightEmu, Is.EqualTo(914400));
    }

    [Test]
    public void MediaManagement_ClassifiesAndPreservesUnsupportedDrawingAndObjectMarkup()
    {
        global::docx.IDocxMediaManagement media = new global::docx.DocxMediaManagement();
        global::docx.DocxRun run = new global::docx.DocxRun(
            "<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
            + "<w:drawing><wp:anchor><a:graphic><a:graphicData><a:srcRect l=\"100\"/><c:chart r:id=\"rChart\"/></a:graphicData></a:graphic></wp:anchor></w:drawing>"
            + "<w:drawing><wp:inline><a:graphic><a:graphicData><dgm:relIds r:dm=\"rDiagram\"/></a:graphicData></a:graphic></wp:inline></w:drawing>"
            + "<w:pict><v:shape id=\"vml\"/></w:pict>"
            + "<w:object><v:shape id=\"ole\"/><o:OLEObject r:id=\"rOle\"/></w:object>"
            + "</w:r>");
        List<global::docx.DocxDrawing> drawings = run.GetDrawings();

        Assert.That(media.IsFloatingDrawing(drawings[0]), Is.True);
        Assert.That(media.DrawingContainsChart(drawings[0]), Is.True);
        Assert.That(media.GetDrawingXml(drawings[0]), Does.Contain("srcRect l=\"100\""));
        Assert.That(media.IsInlineDrawing(drawings[1]), Is.True);
        Assert.That(media.DrawingContainsDiagram(drawings[1]), Is.True);
        Assert.That(media.DrawingContainsVml(drawings[2]), Is.True);
        Assert.That(media.DrawingContainsEmbeddedObject(drawings[3]), Is.True);
        Assert.That(media.GetDrawingXml(drawings[3]), Does.Contain("r:id=\"rOle\""));
    }

    [Test]
    public void MediaDiscovery_RejectsInvalidArguments()
    {
        global::docx.DocxMedia media = new global::docx.DocxMedia();
        global::docx.DocxPackage package = CreatePackageWithImages();
        global::docx.DocxRelationshipSet relationships = package.FindRelationshipSet("/word/document.xml")!;
        global::docx.DocxRelationship relationship = relationships.FindById("rImageMain")!;
        global::docx.DocxImage image = new global::docx.DocxImage();
        global::docx.DocxMedia attachedMedia = global::docx.DocxMedia.Discover(package);
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.Discover(null!));
        Assert.Throws<ArgumentNullException>(() => media.SetPackage(null!));
        Assert.Throws<ArgumentNullException>(() => media.DiscoverImages(null!));
        Assert.Throws<ArgumentNullException>(() => media.DiscoverImagesFromRelationshipSet(null!, relationships));
        Assert.Throws<ArgumentNullException>(() => media.DiscoverImagesFromRelationshipSet(package, null!));
        Assert.Throws<ArgumentNullException>(() => media.AddImageFromRelationship(null!, relationships, relationship));
        Assert.Throws<ArgumentNullException>(() => media.AddImageFromRelationship(package, null!, relationship));
        Assert.Throws<ArgumentNullException>(() => media.AddImageFromRelationship(package, relationships, null!));
        Assert.Throws<ArgumentNullException>(() => media.AddImage(null!, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => media.AddImage(new byte[] { 1 }, null!));
        Assert.Throws<ArgumentNullException>(() => media.AddImageFromStream(null!, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => media.AddImageFromStream(new MemoryStream(new byte[] { 1 }), null!));
        Assert.Throws<ArgumentNullException>(() => media.AddImageWithRelationship(null!, global::docx.DocxConstants.ContentTypePng, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => media.AddImageWithRelationship(new byte[] { 1 }, null!, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => media.AddImageWithRelationship(new byte[] { 1 }, global::docx.DocxConstants.ContentTypePng, null!));
        Assert.Throws<ArgumentNullException>(() => media.CreateImageRelationship(null!, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => media.CreateImageRelationship(image, null!));
        Assert.Throws<ArgumentNullException>(() => media.FindImageRelationship(null!, image));
        Assert.Throws<ArgumentNullException>(() => media.FindImageRelationship(relationships, null!));
        Assert.Throws<ArgumentNullException>(() => media.GetOrCreateRelationshipSet(null!));
        Assert.Throws<ArgumentNullException>(() => media.WriteRelationshipSetPart(null!));
        Assert.Throws<ArgumentNullException>(() => media.ReplaceImage(null!, new byte[] { 1 }, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => media.ReplaceImage(image, null!, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => media.ReplaceImage(image, new byte[] { 1 }, null!));
        Assert.Throws<ArgumentNullException>(() => media.ReplaceImageBytes(null!, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => media.ReplaceImageBytes(image, null!));
        Assert.Throws<ArgumentNullException>(() => media.ImportImages(null!, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => media.ImportImages(attachedMedia, null!));
        Assert.Throws<ArgumentNullException>(() => media.ImportImage(null!, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => media.ImportImage(attachedMedia.GetImage(0), null!));
        Assert.Throws<ArgumentNullException>(() => media.ImportImageWithMap(null!, "/word/document.xml", new global::docx.DocxMediaImportMap()));
        Assert.Throws<ArgumentNullException>(() => media.ImportImageWithMap(attachedMedia.GetImage(0), null!, new global::docx.DocxMediaImportMap()));
        Assert.Throws<ArgumentNullException>(() => media.ImportImageWithMap(attachedMedia.GetImage(0), "/word/document.xml", null!));
        Assert.Throws<ArgumentNullException>(() => media.AllocateImagePartName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.ResolveImageContentType(null!, ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.ResolveImageContentType(new byte[] { 1 }, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.GetImageExtension(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.GetRelationshipTarget(null!, "/word/media/image1.png"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.GetRelationshipTarget("/word/document.xml", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMedia.ReadAllBytes(null!));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().AddImage(new byte[] { 1 }, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().CreateImageRelationship(image, "/word/document.xml"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().GetOrCreateRelationshipSet("/word/document.xml"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().WriteRelationshipSetPart(relationships));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().ReplaceImage(image, new byte[] { 1 }, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().ImportImages(attachedMedia, "/word/document.xml"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().AllocateImagePartName(global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxMedia().ImagePartIndexExists(1));
        Assert.Throws<ArgumentException>(() => attachedMedia.AddImage(new byte[] { 1 }, ""));
        Assert.Throws<ArgumentException>(() => attachedMedia.CreateImageRelationship(new global::docx.DocxImage(), "/word/document.xml"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxMedia.ResolveImageContentType(new byte[] { 1 }, ""));
        Assert.Throws<ArgumentException>(() => global::docx.DocxMedia.GetImageExtension("image/bmp"));
        Assert.Throws<ArgumentNullException>(() => media.FindImage(null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => media.FindImage("/word/document.xml", null!));
        Assert.Throws<InvalidOperationException>(() => attachedMedia.CreateImageRelationship(global::docx.DocxImage.Create("", "/word/media/missing.png", "", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 }), "/word/document.xml"));
        Assert.Throws<InvalidOperationException>(() => attachedMedia.CreateImageRelationship(attachedMedia.GetImage(0), "/word/missing.xml"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.Create(null!, "/word/media/image1.png", "rId1", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.Create("/word/document.xml", null!, "rId1", global::docx.DocxConstants.ContentTypePng, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.png", null!, global::docx.DocxConstants.ContentTypePng, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.png", "rId1", null!, new byte[] { 1 }));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.Create("/word/document.xml", "/word/media/image1.png", "rId1", global::docx.DocxConstants.ContentTypePng, null!));
        Assert.Throws<ArgumentNullException>(() => image.SetPartInfo(null!, "/word/media/image1.png", "rId1", global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => image.SetPartInfo("/word/document.xml", null!, "rId1", global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => image.SetPartInfo("/word/document.xml", "/word/media/image1.png", null!, global::docx.DocxConstants.ContentTypePng));
        Assert.Throws<ArgumentNullException>(() => image.SetPartInfo("/word/document.xml", "/word/media/image1.png", "rId1", null!));
        Assert.Throws<ArgumentNullException>(() => image.SetBytes(null!));
        Assert.Throws<ArgumentNullException>(() => image.ReplaceBytes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.NormalizeSourcePartName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.DetectContentType(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.HasPngHeader(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.HasJpegHeader(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxImage.HasGifHeader(null!));

        global::docx.DocxMediaImportMap map = new global::docx.DocxMediaImportMap();

        Assert.Throws<ArgumentNullException>(() => map.Add(null!, "rId1", "/word/document.xml", "rId2", "/word/media/image1.png", "/word/media/image2.png"));
        Assert.Throws<ArgumentNullException>(() => map.Add("/word/document.xml", null!, "/word/document.xml", "rId2", "/word/media/image1.png", "/word/media/image2.png"));
        Assert.Throws<ArgumentNullException>(() => map.Add("/word/document.xml", "rId1", null!, "rId2", "/word/media/image1.png", "/word/media/image2.png"));
        Assert.Throws<ArgumentNullException>(() => map.Add("/word/document.xml", "rId1", "/word/document.xml", null!, "/word/media/image1.png", "/word/media/image2.png"));
        Assert.Throws<ArgumentNullException>(() => map.Add("/word/document.xml", "rId1", "/word/document.xml", "rId2", null!, "/word/media/image2.png"));
        Assert.Throws<ArgumentNullException>(() => map.Add("/word/document.xml", "rId1", "/word/document.xml", "rId2", "/word/media/image1.png", null!));
        Assert.Throws<ArgumentNullException>(() => map.GetRelationshipId(null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => map.GetRelationshipId("/word/document.xml", null!));
        Assert.Throws<ArgumentNullException>(() => map.GetImagePartName(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxMediaRelationshipMap(null!, "rId1", "/word/document.xml", "rId2", "/word/media/image1.png", "/word/media/image2.png"));
        Assert.Throws<ArgumentNullException>(() => document.ImportMedia((global::docx.DocxDocument)null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportMediaCollection((global::docx.DocxMedia)null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportMediaToPart((global::docx.DocxDocument)null!, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => document.ImportMediaToPart(global::docx.DocxDocument.Create(), null!));
        Assert.Throws<ArgumentNullException>(() => document.ImportMediaCollectionToPart((global::docx.DocxMedia)null!, "/word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => document.ImportMediaCollectionToPart(attachedMedia, null!));
    }

    static global::docx.DocxPackage CreatePackageWithImages()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet headerRelationships = new global::docx.DocxRelationshipSet("/word/header1.xml");

        documentRelationships.CreateRelationshipWithId("rImageMain", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
        documentRelationships.CreateRelationshipWithId("rMissing", global::docx.DocxConstants.RelationshipTypeImage, "media/missing.png", false);
        documentRelationships.CreateRelationshipWithId("rExternal", global::docx.DocxConstants.RelationshipTypeImage, "https://example.com/image.png", true);
        documentRelationships.CreateRelationshipWithId("rHeader1", global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", false);
        headerRelationships.CreateRelationshipWithId("rImageHeader", global::docx.DocxConstants.RelationshipTypeImage, "media/header-image.gif", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetDefault("png", global::docx.DocxConstants.ContentTypePng);
        package.ContentTypes.SetDefault("gif", global::docx.DocxConstants.ContentTypeGif);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddXmlPartWithContent("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        package.AddBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 1, 2, 3 });
        package.AddBinaryPart("/word/media/header-image.gif", global::docx.DocxConstants.ContentTypeGif, new byte[] { 4, 5 });
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/header1.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, headerRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithImageMissingContentType()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rImageMain", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.jpg", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, global::docx.DocxDocument.CreateMinimalMainDocumentXml());
        package.AddBinaryPart("/word/media/image1.jpg", global::docx.DocxConstants.ContentTypeJpeg, new byte[] { 255, 216, 255, 224 });
        package.ContentTypes.RemoveContentTypeForPart("/word/media/image1.jpg");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxDocument RoundTripDocument(global::docx.IDocxDocumentPersistence persistence, global::docx.DocxDocument document)
    {
        MemoryStream stream = new MemoryStream();
        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        return persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
    }
}
