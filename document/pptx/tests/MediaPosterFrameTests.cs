using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MediaPosterFrameTests
{
    [Test]
    public void Slide_GetMediaIncludesPosterFrameMetadata()
    {
        PptxPresentation presentation = LoadPosterPresentation();
        PptxMediaFile media = presentation.GetSlide(0).GetMedia()[0];
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(shape.IsMediaShape(), Is.True);
        Assert.That(shape.HasPosterFrame(), Is.True);
        Assert.That(shape.GetMediaRelationshipId(), Is.EqualTo("rIdVideo"));
        Assert.That(shape.GetImageRelationshipId(), Is.EqualTo("rIdPoster"));
        Assert.That(media.Type, Is.EqualTo(PptxMediaType.Video));
        Assert.That(media.HasPosterFrame, Is.True);
        Assert.That(media.PosterRelationshipId, Is.EqualTo("rIdPoster"));
        Assert.That(media.PosterPartName, Is.EqualTo("/ppt/media/poster1.png"));
        Assert.That(media.PosterBytes, Is.EqualTo(CreatePngBytes(320, 180)));
        Assert.That(media.PosterInfo.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(media.PosterInfo.WidthPixels, Is.EqualTo(320));
        Assert.That(shape.ExtractPosterFrame().Info.Type, Is.EqualTo(PptxImageType.Png));
    }

    [Test]
    public void Shape_ReplacePosterFrameRetargetsPosterOnly()
    {
        PptxPresentation presentation = LoadPosterPresentation();
        PptxShape shape = presentation.GetSlide(0).GetShapeTree().Get(0);

        shape.ReplacePosterFrame(CreateJpegBytes(640, 360));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxMediaFile media = reopened.GetSlide(0).GetMedia()[0];
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);

        Assert.That(reopenedShape.GetMediaRelationshipId(), Is.EqualTo("rIdVideo"));
        Assert.That(reopenedShape.GetImageRelationshipId(), Is.EqualTo("rIdPoster"));
        Assert.That(media.Bytes, Is.EqualTo(CreateVideoBytes()));
        Assert.That(media.PosterInfo.Type, Is.EqualTo(PptxImageType.Jpeg));
        Assert.That(media.PosterInfo.WidthPixels, Is.EqualTo(640));
        Assert.That(reopenedShape.ExtractPosterFrame().Info.Type, Is.EqualTo(PptxImageType.Jpeg));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CloneSlide_PreservesMediaPosterFramePart()
    {
        PptxPresentation presentation = LoadPosterPresentation();
        PptxMediaFile original = presentation.GetSlide(0).GetMedia()[0];

        PptxSlide clone = presentation.CloneSlide(0);
        PptxMediaFile cloned = clone.GetMedia()[0];
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxMediaFile reopenedClone = reopened.GetSlide(1).GetMedia()[0];

        Assert.That(cloned.HasPosterFrame, Is.True);
        Assert.That(cloned.PosterPartName, Is.Not.EqualTo(original.PosterPartName));
        Assert.That(cloned.PosterBytes, Is.EqualTo(original.PosterBytes));
        Assert.That(reopenedClone.HasPosterFrame, Is.True);
        Assert.That(reopenedClone.PosterBytes, Is.EqualTo(CreatePngBytes(320, 180)));
        Assert.That(reopenedClone.PosterInfo.WidthPixels, Is.EqualTo(320));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadPosterPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreatePosterSlideXml()));
        package.AddPart("/ppt/media/video1.mp4", PptxContentTypes.Mp4, CreateVideoBytes());
        package.AddPart("/ppt/media/poster1.png", PptxContentTypes.Png, CreatePngBytes(320, 180));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdVideo", PptxRelationshipTypes.Video, "../media/video1.mp4", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdPoster", PptxRelationshipTypes.Image, "../media/poster1.png", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreatePosterSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:pic>" +
            "<p:nvPicPr><p:cNvPr id=\"2\" name=\"Video\"/><p:cNvPicPr/><p:nvPr><a:videoFile r:link=\"rIdVideo\"/></p:nvPr></p:nvPicPr>" +
            "<p:blipFill><a:blip r:embed=\"rIdPoster\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>" +
            "<p:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"320\" cy=\"180\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "</p:pic>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static byte[] CreateVideoBytes()
    {
        return new byte[] { 0, 0, 0, 24, 102, 116, 121, 112 };
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
