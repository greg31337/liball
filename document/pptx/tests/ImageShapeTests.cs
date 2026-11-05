using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ImageShapeTests
{
    [Test]
    public void Slide_AddImageDeduplicatesMediaPartsAndExtractsImages()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        byte[] png = CreatePngBytes(320, 200);

        PptxShape first = slide.AddImage(png, new PptxBounds(0L, 0L, 320L, 200L));
        PptxShape second = slide.AddImage(png, new PptxBounds(320L, 0L, 320L, 200L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxImageFile> images = reopened.GetSlide(0).ExtractImages();

        Assert.That(first.IsPicture(), Is.True);
        Assert.That(second.IsPicture(), Is.True);
        Assert.That(first.GetImageRelationshipId(), Is.Not.EqualTo(second.GetImageRelationshipId()));
        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(1));
        Assert.That(images.Count, Is.EqualTo(2));
        Assert.That(images[0].PartName, Is.EqualTo(images[1].PartName));
        Assert.That(images[0].Bytes, Is.EqualTo(png));
        Assert.That(images[0].Info.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(images[0].Info.WidthPixels, Is.EqualTo(320));
        Assert.That(images[0].Info.HeightPixels, Is.EqualTo(200));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_ReplaceImageRetargetsOnlySelectedPicture()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        byte[] png = CreatePngBytes(100, 50);
        byte[] jpeg = CreateJpegBytes(640, 480);

        slide.AddImage(png, new PptxBounds(0L, 0L, 100L, 50L));
        slide.AddImage(png, new PptxBounds(100L, 0L, 100L, 50L));
        presentation.GetSlide(0).GetShapeTree().Get(0).ReplaceImage(jpeg);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape first = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxShape second = reopened.GetSlide(0).GetShapeTree().Get(1);

        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(2));
        Assert.That(first.ExtractImage().Info.Type, Is.EqualTo(PptxImageType.Jpeg));
        Assert.That(first.ExtractImage().Info.WidthPixels, Is.EqualTo(640));
        Assert.That(second.ExtractImage().Info.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(second.ExtractImage().Info.WidthPixels, Is.EqualTo(100));
        Assert.That(first.ExtractImage().PartName, Is.Not.EqualTo(second.ExtractImage().PartName));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_CanSetCropStretchAndTile()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddImage(CreatePngBytes(200, 100), new PptxBounds(0L, 0L, 200L, 100L));

        shape.SetImageCrop(1000, 2000, 3000, 4000);
        shape.SetImageTile();
        Assert.That(shape.GetImageCrop().Left, Is.EqualTo(1000));
        Assert.That(shape.GetImageCrop().Top, Is.EqualTo(2000));
        Assert.That(shape.GetImageCrop().Right, Is.EqualTo(3000));
        Assert.That(shape.GetImageCrop().Bottom, Is.EqualTo(4000));
        Assert.That(shape.IsImageTiled(), Is.True);
        Assert.That(shape.IsImageStretched(), Is.False);

        shape.SetImageStretch();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(reopenedShape.GetImageCrop().Left, Is.EqualTo(1000));
        Assert.That(reopenedShape.IsImageTiled(), Is.False);
        Assert.That(reopenedShape.IsImageStretched(), Is.True);
        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("srcRect"));
        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("stretch"));
        Assert.That(reopenedShape.Element.OuterXml, Does.Not.Contain("tile"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
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
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        WriteInt32Big(bytes, 16, width);
        WriteInt32Big(bytes, 20, height);
        return bytes;
    }

    static byte[] CreateJpegBytes(int width, int height)
    {
        byte[] bytes = new byte[39];
        bytes[0] = 255;
        bytes[1] = 216;
        bytes[2] = 255;
        bytes[3] = 224;
        WriteInt16Big(bytes, 4, 16);
        bytes[20] = 255;
        bytes[21] = 192;
        WriteInt16Big(bytes, 22, 17);
        bytes[24] = 8;
        WriteInt16Big(bytes, 25, height);
        WriteInt16Big(bytes, 27, width);
        bytes[29] = 3;
        return bytes;
    }

    static void WriteInt16Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value / 256) & 255);
        bytes[offset + 1] = (byte)(value & 255);
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value / 16777216) & 255);
        bytes[offset + 1] = (byte)((value / 65536) & 255);
        bytes[offset + 2] = (byte)((value / 256) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
