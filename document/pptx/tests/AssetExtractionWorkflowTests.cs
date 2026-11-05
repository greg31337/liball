using System.IO;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class AssetExtractionWorkflowTests
{
    [Test]
    public void Slide_ExtractImagesToDirectoryWritesFilesAndMetadata()
    {
        PptxPresentation presentation = LoadImagePresentation();
        string directory = CreateCleanDirectory("asset-extraction-images");

        PptxAssetExtractionResult result = presentation.GetSlide(0).ExtractImagesToDirectory(directory);
        PptxExtractedAsset first = result.Get(0);
        PptxExtractedAsset second = result.Get(1);

        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.WrittenCount(), Is.EqualTo(2));
        Assert.That(first.Kind, Is.EqualTo("image"));
        Assert.That(first.SlideIndex, Is.EqualTo(0));
        Assert.That(first.SlidePartName, Is.EqualTo(PptxPartNames.FirstSlide));
        Assert.That(first.RelationshipId, Is.EqualTo("rIdImage1"));
        Assert.That(first.PartName, Is.EqualTo("/ppt/media/image1.png"));
        Assert.That(first.ContentType, Is.EqualTo(PptxContentTypes.Png));
        Assert.That(first.FileName, Is.EqualTo("slide1_image_rIdImage1_image1.png"));
        Assert.That(File.Exists(first.FilePath), Is.True);
        Assert.That(File.ReadAllBytes(first.FilePath), Is.EqualTo(CreatePngBytes(320, 200)));
        Assert.That(second.FileName, Is.EqualTo("slide1_image_rIdImage2_image1.png"));
        Assert.That(File.ReadAllBytes(second.FilePath), Is.EqualTo(CreatePngBytes(320, 200)));
        Assert.That(first.ImageInfo.WidthPixels, Is.EqualTo(320));
        Assert.That(first.ImageInfo.HeightPixels, Is.EqualTo(200));
    }

    [Test]
    public void Presentation_ExtractMediaToDirectoryWritesEmbeddedMediaAndReportsLinkedMedia()
    {
        PptxPresentation presentation = LoadMediaPresentation();
        string directory = CreateCleanDirectory("asset-extraction-media");

        PptxAssetExtractionResult result = presentation.ExtractMediaToDirectory(directory);
        PptxExtractedAsset audio = result.Get(0);
        PptxExtractedAsset video = result.Get(1);

        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.WrittenCount(), Is.EqualTo(1));
        Assert.That(result.LinkedCount(), Is.EqualTo(1));
        Assert.That(audio.Kind, Is.EqualTo("media"));
        Assert.That(audio.MediaType, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(audio.Embedded, Is.True);
        Assert.That(audio.Written, Is.True);
        Assert.That(audio.ContentType, Is.EqualTo(PptxContentTypes.Mp3));
        Assert.That(audio.FileName, Is.EqualTo("slide1_audio_rIdAudio_audio1.mp3"));
        Assert.That(File.ReadAllBytes(audio.FilePath), Is.EqualTo(CreateAudioBytes()));
        Assert.That(video.MediaType, Is.EqualTo(PptxMediaType.Video));
        Assert.That(video.Linked, Is.True);
        Assert.That(video.Written, Is.False);
        Assert.That(video.FileName, Is.EqualTo(""));
        Assert.That(video.Target, Is.EqualTo("https://example.com/video.mp4"));
    }

    [Test]
    public void Slide_ExtractMediaToDirectoryWritesPosterFramesWithMedia()
    {
        PptxPresentation presentation = LoadPosterPresentation();
        string directory = CreateCleanDirectory("asset-extraction-poster");

        PptxAssetExtractionResult result = presentation.GetSlide(0).ExtractMediaToDirectory(directory);
        PptxExtractedAsset video = result.Get(0);
        PptxExtractedAsset poster = result.Get(1);

        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.WrittenCount(), Is.EqualTo(2));
        Assert.That(video.Kind, Is.EqualTo("media"));
        Assert.That(video.MediaType, Is.EqualTo(PptxMediaType.Video));
        Assert.That(video.FileName, Is.EqualTo("slide1_video_rIdVideo_video1.mp4"));
        Assert.That(File.ReadAllBytes(video.FilePath), Is.EqualTo(CreateVideoBytes()));
        Assert.That(poster.Kind, Is.EqualTo("poster"));
        Assert.That(poster.RelationshipId, Is.EqualTo("rIdPoster"));
        Assert.That(poster.PartName, Is.EqualTo("/ppt/media/poster1.png"));
        Assert.That(poster.MediaType, Is.EqualTo(PptxMediaType.Video));
        Assert.That(poster.FileName, Is.EqualTo("slide1_poster_rIdPoster_poster1.png"));
        Assert.That(File.ReadAllBytes(poster.FilePath), Is.EqualTo(CreatePngBytes(320, 180)));
        Assert.That(poster.ImageInfo.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(poster.ImageInfo.WidthPixels, Is.EqualTo(320));
    }

    [Test]
    public void ExtractToDirectoryRejectsBlankDirectory()
    {
        PptxPresentation presentation = LoadMediaPresentation();

        Assert.That(
            delegate
            {
                presentation.ExtractMediaToDirectory(" ");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static PptxPresentation LoadImagePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart("/ppt/media/image1.png", PptxContentTypes.Png, CreatePngBytes(320, 200));
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdImage1", PptxRelationshipTypes.Image, "../media/image1.png", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdImage2", PptxRelationshipTypes.Image, "../media/image1.png", PptxRelationshipTargetMode.Internal);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadMediaPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart("/ppt/media/audio1.mp3", PptxContentTypes.Mp3, CreateAudioBytes());
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdAudio", PptxRelationshipTypes.Audio, "../media/audio1.mp3", PptxRelationshipTargetMode.Internal);
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdVideo", PptxRelationshipTypes.Video, "https://example.com/video.mp4", PptxRelationshipTargetMode.External);
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
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

    static string CreateCleanDirectory(string name)
    {
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, name);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        Directory.CreateDirectory(directory);
        return directory;
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

    static byte[] CreateAudioBytes()
    {
        return new byte[] { 73, 68, 51, 4, 0, 0, 0, 0 };
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

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
