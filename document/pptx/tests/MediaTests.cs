using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MediaTests
{
    [Test]
    public void Slide_GetMediaInventoriesEmbeddedAudioAndLinkedVideo()
    {
        PptxPresentation presentation = LoadMediaPresentation();
        List<PptxMediaFile> media = presentation.GetSlide(0).GetMedia();
        PptxMediaFile audio = media[0];
        PptxMediaFile video = media[1];

        Assert.That(media.Count, Is.EqualTo(2));
        Assert.That(audio.Type, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(audio.Embedded, Is.True);
        Assert.That(audio.Linked, Is.False);
        Assert.That(audio.PartName, Is.EqualTo("/ppt/media/audio1.mp3"));
        Assert.That(audio.ContentType, Is.EqualTo(PptxContentTypes.Mp3));
        Assert.That(audio.Bytes, Is.EqualTo(CreateAudioBytes()));
        Assert.That(video.Type, Is.EqualTo(PptxMediaType.Video));
        Assert.That(video.Linked, Is.True);
        Assert.That(video.Embedded, Is.False);
        Assert.That(video.Target, Is.EqualTo("https://example.com/video.mp4"));
        Assert.That(presentation.GetMedia().Count, Is.EqualTo(2));
    }

    [Test]
    public void CloneSlide_PreservesEmbeddedMediaPartsAndLinkedMediaTargets()
    {
        PptxPresentation presentation = LoadMediaPresentation();
        PptxMediaFile originalAudio = presentation.GetSlide(0).GetMedia()[0];

        PptxSlide clone = presentation.CloneSlide(0);
        List<PptxMediaFile> cloneMedia = clone.GetMedia();
        PptxMediaFile clonedAudio = cloneMedia[0];
        PptxMediaFile clonedVideo = cloneMedia[1];
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxMediaFile> reopenedCloneMedia = reopened.GetSlide(1).GetMedia();

        Assert.That(cloneMedia.Count, Is.EqualTo(2));
        Assert.That(clonedAudio.Type, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(clonedAudio.Embedded, Is.True);
        Assert.That(clonedAudio.PartName, Is.Not.EqualTo(originalAudio.PartName));
        Assert.That(clonedAudio.Bytes, Is.EqualTo(originalAudio.Bytes));
        Assert.That(clonedVideo.Type, Is.EqualTo(PptxMediaType.Video));
        Assert.That(clonedVideo.Linked, Is.True);
        Assert.That(clonedVideo.Target, Is.EqualTo("https://example.com/video.mp4"));
        Assert.That(reopened.GetMedia().Count, Is.EqualTo(4));
        Assert.That(reopenedCloneMedia[0].PartName, Is.EqualTo(clonedAudio.PartName));
        Assert.That(reopenedCloneMedia[0].Bytes, Is.EqualTo(CreateAudioBytes()));
        Assert.That(reopenedCloneMedia[1].Target, Is.EqualTo("https://example.com/video.mp4"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
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

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static byte[] CreateAudioBytes()
    {
        return new byte[] { 73, 68, 51, 4, 0, 0, 0, 0 };
    }
}
