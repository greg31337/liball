using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ImageApiCompletionTests
{
    [Test]
    public void ImageDetector_DetectSpanReadsPlannedHeaderWithoutAllocationApi()
    {
        byte[] png = CreatePngBytes(321, 123);
        System.ReadOnlySpan<byte> span = png;

        PptxImageInfo info = new PptxImageDetector().DetectSpan(span);

        Assert.That(info.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(info.ContentType, Is.EqualTo(PptxContentTypes.Png));
        Assert.That(info.WidthPixels, Is.EqualTo(321));
        Assert.That(info.HeightPixels, Is.EqualTo(123));
    }

    [Test]
    public void ImageDetector_RejectsSvgLookalikesCommentsAndIncompleteTags()
    {
        PptxImageDetector detector = new PptxImageDetector();
        byte[] lookalike = System.Text.Encoding.UTF8.GetBytes("<svgish width=\"640\" height=\"480\">opaque</svgish>");
        byte[] commented = System.Text.Encoding.UTF8.GetBytes("<!-- <svg width=\"640\" height=\"480\"> --><asset />");
        byte[] incomplete = System.Text.Encoding.UTF8.GetBytes("<svg width=\"640\" height=\"480\"");

        PptxImageInfo lookalikeInfo = detector.Detect(lookalike);
        PptxImageInfo commentedInfo = detector.Detect(commented);
        PptxImageInfo incompleteInfo = detector.Detect(incomplete);

        Assert.That(lookalikeInfo.Type, Is.EqualTo(PptxImageType.Unknown));
        Assert.That(lookalikeInfo.ContentType, Is.EqualTo(PptxContentTypes.Binary));
        Assert.That(commentedInfo.Type, Is.EqualTo(PptxImageType.Unknown));
        Assert.That(incompleteInfo.Type, Is.EqualTo(PptxImageType.Unknown));
    }

    [Test]
    public void ImageDetector_ReadsOnlyExactAttributesOnSvgStartTag()
    {
        byte[] svg = System.Text.Encoding.UTF8.GetBytes("<svg stroke-width=\"5\" width = \"320px\" height = '180px'><rect width=\"1\" height=\"2\" /></svg>");
        byte[] childDimensionsOnly = System.Text.Encoding.UTF8.GetBytes("<svg><rect width=\"90\" height=\"45\" /></svg>");
        PptxImageDetector detector = new PptxImageDetector();

        PptxImageInfo info = detector.Detect(svg);
        PptxImageInfo childInfo = detector.Detect(childDimensionsOnly);

        Assert.That(info.Type, Is.EqualTo(PptxImageType.Svg));
        Assert.That(info.WidthPixels, Is.EqualTo(320));
        Assert.That(info.HeightPixels, Is.EqualTo(180));
        Assert.That(childInfo.Type, Is.EqualTo(PptxImageType.Svg));
        Assert.That(childInfo.WidthPixels, Is.EqualTo(0));
        Assert.That(childInfo.HeightPixels, Is.EqualTo(0));
    }

    [Test]
    public void ImageOperations_UnsupportedPayloadPreservesBytesAndContentType()
    {
        byte[] unsupported = System.Text.Encoding.UTF8.GetBytes("<svgish width=\"640\" height=\"480\">opaque</svgish>");
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxImageOperations images = new PptxImageOperations();
        images.AddEmbedded(presentation.GetSlide(0), unsupported, "image/x-unsupported", new PptxBounds(0L, 0L, 640L, 480L));

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxImageFile> extracted = reopened.GetSlide(0).ExtractImages();

        Assert.That(extracted.Count, Is.EqualTo(1));
        Assert.That(extracted[0].Bytes, Is.EqualTo(unsupported));
        Assert.That(extracted[0].Info.Type, Is.EqualTo(PptxImageType.Unknown));
        Assert.That(extracted[0].Info.ContentType, Is.EqualTo("image/x-unsupported"));
        Assert.That(extracted[0].PartName, Does.EndWith(".bin"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ImageOperations_AddEmbeddedHonorsContentTypeAndDeduplicatesEquivalentParts()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxImageOperations images = new PptxImageOperations();
        byte[] png = CreatePngBytes(80, 40);

        images.AddEmbedded(slide, png, "image/x-example", new PptxBounds(0L, 0L, 80L, 40L));
        images.AddEmbedded(slide, png, "image/x-example", new PptxBounds(80L, 0L, 80L, 40L));
        images.AddEmbedded(slide, png, PptxContentTypes.Png, new PptxBounds(160L, 0L, 80L, 40L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxImageFile> extracted = reopened.GetSlide(0).ExtractImages();

        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(2));
        Assert.That(extracted[0].PartName, Is.EqualTo(extracted[1].PartName));
        Assert.That(extracted[0].Info.ContentType, Is.EqualTo("image/x-example"));
        Assert.That(extracted[0].Info.WidthPixels, Is.EqualTo(80));
        Assert.That(extracted[2].Info.ContentType, Is.EqualTo(PptxContentTypes.Png));
        Assert.That(extracted[2].PartName, Is.Not.EqualTo(extracted[0].PartName));
    }

    [Test]
    public void ImageOperations_AddLinkedAndReplaceByShapeRoundTripsRelationshipKind()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxImageOperations images = new PptxImageOperations();
        PptxShape shape = images.AddLinked(slide, "https://example.test/image.png", new PptxBounds(0L, 0L, 100L, 50L));

        PptxImageFile linked = shape.ExtractImage();
        Assert.That(linked.Linked, Is.True);
        Assert.That(linked.Target, Is.EqualTo("https://example.test/image.png"));
        Assert.That(linked.Bytes, Is.Empty);
        Assert.That(shape.Element.OuterXml, Does.Contain("r:link"));
        Assert.That(CountMediaParts(presentation.Package), Is.EqualTo(0));

        images.ReplaceShapeEmbedded(shape, CreatePngBytes(100, 50), PptxContentTypes.Png);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("r:embed"));
        Assert.That(reopenedShape.Element.OuterXml, Does.Not.Contain("r:link"));
        Assert.That(reopenedShape.ExtractImage().Linked, Is.False);
        Assert.That(reopenedShape.ExtractImage().Info.Type, Is.EqualTo(PptxImageType.Png));
    }

    [Test]
    public void ImageOperations_ReplaceByRelationshipUpdatesEveryReferencingPicture()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxImageOperations images = new PptxImageOperations();
        PptxShape first = images.AddEmbedded(slide, CreatePngBytes(40, 20), PptxContentTypes.Png, new PptxBounds(0L, 0L, 40L, 20L));
        string relationshipId = first.GetImageRelationshipId();
        PptxShape second = slide.GetShapeTree().AddPicture(relationshipId, new PptxBounds(40L, 0L, 40L, 20L));

        images.ReplaceRelationshipLinked(slide, relationshipId, "https://example.test/replacement.png");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedFirst = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxShape reopenedSecond = reopened.GetSlide(0).GetShapeTree().Get(1);

        Assert.That(first.GetImageRelationshipId(), Is.EqualTo(second.GetImageRelationshipId()));
        Assert.That(reopenedFirst.Element.OuterXml, Does.Contain("r:link"));
        Assert.That(reopenedSecond.Element.OuterXml, Does.Contain("r:link"));
        Assert.That(reopenedFirst.ExtractImage().Target, Is.EqualTo("https://example.test/replacement.png"));
        Assert.That(reopened.GetSlide(0).ExtractImages().Count, Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).ExtractImages()[0].Linked, Is.True);
    }

    [Test]
    public void ImageOperations_DeduplicateRetargetsExistingDuplicateParts()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        byte[] png = CreatePngBytes(60, 30);
        PptxShape first = slide.AddImage(png, new PptxBounds(0L, 0L, 60L, 30L));
        PptxPart duplicate = presentation.Package.AddPart("/ppt/media/duplicate.png", PptxContentTypes.Png, png);
        PptxRelationship relationship = presentation.Package.AddRelationship(slide.PartName, PptxRelationshipTypes.Image, "../media/duplicate.png", PptxRelationshipTargetMode.Internal);
        PptxShape second = slide.GetShapeTree().AddPicture(relationship.Id, new PptxBounds(60L, 0L, 60L, 30L));

        int removed = new PptxImageOperations().Deduplicate(presentation);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(duplicate.PartName, Is.EqualTo("/ppt/media/duplicate.png"));
        Assert.That(removed, Is.EqualTo(1));
        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).GetShapeTree().Get(0).ExtractImage().PartName, Is.EqualTo(reopened.GetSlide(0).GetShapeTree().Get(1).ExtractImage().PartName));
        Assert.That(first.GetImageRelationshipId(), Is.Not.EqualTo(second.GetImageRelationshipId()));
    }

    [Test]
    public void ImageOperations_TypedStretchAndTileSettingsRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddImage(CreatePngBytes(100, 50), new PptxBounds(0L, 0L, 100L, 50L));
        PptxImageOperations images = new PptxImageOperations();
        PptxFillStretch stretch = new PptxFillStretch();
        stretch.Left = 1000;
        stretch.Top = 2000;
        stretch.Right = 3000;
        stretch.Bottom = 4000;
        images.SetStretch(shape, stretch);
        Assert.That(images.GetStretch(shape).Right, Is.EqualTo(3000));

        PptxFillTile tile = new PptxFillTile();
        tile.OffsetX = 10L;
        tile.OffsetY = 20L;
        tile.ScaleX = 50000;
        tile.ScaleY = 75000;
        tile.Alignment = "ctr";
        tile.Flip = "x";
        images.SetTile(shape, tile);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxFillTile reopenedTile = images.GetTile(reopenedShape);

        Assert.That(reopenedShape.IsImageTiled(), Is.True);
        Assert.That(reopenedShape.IsImageStretched(), Is.False);
        Assert.That(reopenedTile.OffsetX, Is.EqualTo(10L));
        Assert.That(reopenedTile.OffsetY, Is.EqualTo(20L));
        Assert.That(reopenedTile.ScaleX, Is.EqualTo(50000));
        Assert.That(reopenedTile.ScaleY, Is.EqualTo(75000));
        Assert.That(reopenedTile.Alignment, Is.EqualTo("ctr"));
        Assert.That(reopenedTile.Flip, Is.EqualTo("x"));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static int CountMediaParts(PptxPackage package)
    {
        int count = 0;
        for (int index = 0; index < package.Parts.Count; index++)
        {
            if (package.Parts[index].PartName.StartsWith("/ppt/media/", System.StringComparison.Ordinal))
            {
                count = count + 1;
            }
        }

        return count;
    }

    static byte[] CreatePngBytes(int width, int height)
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        WriteInt32Big(bytes, 16, width);
        WriteInt32Big(bytes, 20, height);
        return bytes;
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
