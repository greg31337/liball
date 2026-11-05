using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MediaApiCompletionTests
{
    [Test]
    public void MediaOperations_CreateEmbeddedAndLinkedAudioVideoRoundTripsInventory()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxMediaOperations media = new PptxMediaOperations();

        media.AddEmbedded(slide, CreateAudioBytes(), PptxContentTypes.Mp3, PptxMediaType.Audio, BoundsAt(0L));
        media.AddEmbedded(slide, CreateVideoBytes(), PptxContentTypes.Mp4, PptxMediaType.Video, BoundsAt(100L));
        media.AddLinked(slide, "https://example.test/audio.mp3", PptxMediaType.Audio, BoundsAt(200L));
        media.AddLinked(slide, "http://example.test/video.mp4", PptxMediaType.Video, BoundsAt(300L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxMediaFile> inventory = media.GetInventory(reopened);

        Assert.That(inventory, Has.Count.EqualTo(4));
        Assert.That(inventory[0].Type, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(inventory[0].Embedded, Is.True);
        Assert.That(inventory[0].ContentType, Is.EqualTo(PptxContentTypes.Mp3));
        Assert.That(inventory[0].Bytes, Is.EqualTo(CreateAudioBytes()));
        Assert.That(inventory[1].Type, Is.EqualTo(PptxMediaType.Video));
        Assert.That(inventory[1].Embedded, Is.True);
        Assert.That(inventory[1].ContentType, Is.EqualTo(PptxContentTypes.Mp4));
        Assert.That(inventory[2].Type, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(inventory[2].Linked, Is.True);
        Assert.That(inventory[3].Type, Is.EqualTo(PptxMediaType.Video));
        Assert.That(inventory[3].Target, Is.EqualTo("http://example.test/video.mp4"));
        Assert.That(media.GetSlideInventory(reopened.GetSlide(0)), Has.Count.EqualTo(4));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void MediaOperations_ReplaceEmbeddedAndLinkedPreservesShapeAndCleansOldParts()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxMediaOperations media = new PptxMediaOperations();
        PptxShape shape = media.AddEmbedded(slide, CreateVideoBytes(), PptxContentTypes.Mp4, PptxMediaType.Video, BoundsAt(0L));
        int shapeId = shape.Id;

        media.ReplaceLinked(shape, "https://example.test/replacement.mp4");
        Assert.That(CountMediaParts(presentation.Package), Is.EqualTo(0));
        Assert.That(media.GetSlideInventory(slide)[0].Linked, Is.True);

        byte[] replacement = new byte[] { 0, 0, 0, 28, 102, 116, 121, 112, 109, 112, 52, 50 };
        media.ReplaceEmbedded(shape, replacement, PptxContentTypes.Mp4);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxMediaFile reopenedMedia = media.GetInventory(reopened)[0];

        Assert.That(reopenedShape.Id, Is.EqualTo(shapeId));
        Assert.That(reopenedMedia.Embedded, Is.True);
        Assert.That(reopenedMedia.Linked, Is.False);
        Assert.That(reopenedMedia.Bytes, Is.EqualTo(replacement));
        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(1));
    }

    [Test]
    public void MediaOperations_SetReplaceAndRemovePosterFrameRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxMediaOperations media = new PptxMediaOperations();
        PptxShape shape = media.AddEmbedded(slide, CreateVideoBytes(), PptxContentTypes.Mp4, PptxMediaType.Video, BoundsAt(0L));

        media.SetPosterFrame(shape, CreatePngBytes(320, 180), PptxContentTypes.Png);
        Assert.That(shape.HasPosterFrame(), Is.True);
        Assert.That(media.GetSlideInventory(slide)[0].PosterInfo.Type, Is.EqualTo(PptxImageType.Png));
        Assert.That(CountMediaParts(presentation.Package), Is.EqualTo(2));

        media.SetPosterFrame(shape, CreateJpegBytes(640, 360), PptxContentTypes.Jpeg);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        PptxMediaFile reopenedMedia = media.GetInventory(reopened)[0];

        Assert.That(reopenedMedia.HasPosterFrame, Is.True);
        Assert.That(reopenedMedia.PosterInfo.Type, Is.EqualTo(PptxImageType.Jpeg));
        Assert.That(reopenedMedia.PosterInfo.WidthPixels, Is.EqualTo(640));
        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(2));

        media.RemovePosterFrame(reopenedShape);
        PptxPresentation withoutPoster = SaveReopenAndLoad(reopened);
        Assert.That(media.GetInventory(withoutPoster)[0].HasPosterFrame, Is.False);
        Assert.That(CountMediaParts(withoutPoster.Package), Is.EqualTo(1));
    }

    [Test]
    public void MediaOperations_RemoveDeletesShapeRelationshipsAndOwnedParts()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxMediaOperations media = new PptxMediaOperations();
        PptxShape audio = media.AddEmbedded(slide, CreateAudioBytes(), PptxContentTypes.Mp3, PptxMediaType.Audio, BoundsAt(0L));
        PptxShape video = media.AddEmbedded(slide, CreateVideoBytes(), PptxContentTypes.Mp4, PptxMediaType.Video, BoundsAt(100L));
        media.SetPosterFrame(video, CreatePngBytes(100, 50), PptxContentTypes.Png);

        media.Remove(video);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(media.GetInventory(reopened), Has.Count.EqualTo(1));
        Assert.That(media.GetInventory(reopened)[0].Type, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(reopened.GetSlide(0).GetShapeTree().Count(), Is.EqualTo(1));
        Assert.That(CountMediaParts(reopened.Package), Is.EqualTo(1));
        Assert.That(audio.IsMediaShape(), Is.True);
    }

    [Test]
    public void MediaOperations_AuditExternalReportsSecureAndInsecureTargets()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxMediaOperations media = new PptxMediaOperations();
        media.AddLinked(slide, "https://cdn.example.test/audio.mp3", PptxMediaType.Audio, BoundsAt(0L));
        media.AddLinked(slide, "file:///shared/video.mp4", PptxMediaType.Video, BoundsAt(100L));
        media.AddEmbedded(slide, CreateAudioBytes(), PptxContentTypes.Mp3, PptxMediaType.Audio, BoundsAt(200L));

        PptxExternalMediaAuditResult audit = media.AuditExternal(presentation);

        Assert.That(audit.ExternalCount, Is.EqualTo(2));
        Assert.That(audit.InsecureCount, Is.EqualTo(1));
        Assert.That(audit.MissingTargetCount, Is.EqualTo(0));
        Assert.That(audit.Links[0].Secure, Is.True);
        Assert.That(audit.Links[0].MediaType, Is.EqualTo(PptxMediaType.Audio));
        Assert.That(audit.Links[1].Secure, Is.False);
        Assert.That(audit.Links[1].Target, Is.EqualTo("file:///shared/video.mp4"));
    }

    [Test]
    public void MediaOperations_RejectsMismatchedContentType()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxMediaOperations media = new PptxMediaOperations();

        Assert.That(
            () => media.AddEmbedded(presentation.GetSlide(0), CreateVideoBytes(), PptxContentTypes.Mp3, PptxMediaType.Video, BoundsAt(0L)),
            Throws.ArgumentException);
        Assert.That(media.GetInventory(presentation), Is.Empty);
    }

    static PptxBounds BoundsAt(long x)
    {
        return new PptxBounds(x, 0L, 100L, 50L);
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
        return bytes;
    }

    static void WriteInt16Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 255);
        bytes[offset + 1] = (byte)(value & 255);
    }

    static void WriteInt32Big(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }
}
