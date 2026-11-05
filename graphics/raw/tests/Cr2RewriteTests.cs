using raw;

namespace raw.Tests;

public class Cr2RewriteTests
{
    [Test]
    public void RewriteAppliesExifAndPreviewEditsWhilePreservingRawAndMakerNoteValues()
    {
        byte[] source = CreateSource();
        RawDocument document = Open(source);
        RawPayloadBlock originalRawBlock = document.PayloadMap.Find(RawPayloadKind.RawImage).Single();
        byte[] originalRaw = document.ExtractPayload(originalRawBlock);
        RawMakerNote originalMaker = document.Metadata.MakerNotes.Single();
        byte[] cameraSettings = (originalMaker.FindCanonTag(0x0001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes();
        byte[] colorData = (originalMaker.FindCanonTag(0x4001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes();
        byte[] retainedSecondPreview = document.Thumbnails[1].EncodedBytes.ToArray();
        byte[] replacement = Preview(6, 6, 211);
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Replace("Artist", new string[] { "A substantially longer reviewed creator" }, null, RawMetadataSource.Exif);
        edits.Remove("Copyright", null, RawMetadataSource.Exif);
        edits.Add("LensModel", new string[] { "Reviewed 50mm" }, null, RawMetadataSource.Exif);
        edits.ReplacePreview(0, RawThumbnailKind.Jpeg, 6, 6, RawOrientation.Normal, "image/jpeg", replacement);
        RawMetadataEditReport editReport = edits.Apply(document, false);

        RawWriteOptions options = new RawWriteOptions();
        byte[] rewritten = RawWriter.RewriteCr2(document, options);
        RawDocument reopened = Open(rewritten);
        RawMakerNote rewrittenMaker = reopened.Metadata.MakerNotes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(editReport.Succeeded(), Is.True);
            Assert.That(((reopened.Metadata.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(315) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("A substantially longer reviewed creator"));
            Assert.That((reopened.Metadata.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(33432), Is.Null);
            Assert.That(((reopened.Metadata.FindIfd("IFD0/Exif") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(42036) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("Reviewed 50mm"));
            Assert.That(reopened.Thumbnails[0].EncodedBytes, Is.EqualTo(replacement));
            Assert.That(reopened.Thumbnails[0].Width, Is.EqualTo(6));
            Assert.That(reopened.Thumbnails[0].Height, Is.EqualTo(6));
            Assert.That(reopened.Thumbnails[1].EncodedBytes, Is.EqualTo(retainedSecondPreview));
            Assert.That(reopened.ExtractPayload(reopened.PayloadMap.Find(RawPayloadKind.RawImage).Single()), Is.EqualTo(originalRaw));
            Assert.That((rewrittenMaker.FindCanonTag(0x0001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(cameraSettings));
            Assert.That((rewrittenMaker.FindCanonTag(0x4001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.CopyBytes(), Is.EqualTo(colorData));
            Assert.That(rewrittenMaker.Offset, Is.Not.EqualTo(originalMaker.Offset));
            Assert.That(((reopened.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath, Is.EqualTo("IFD3"));
            Assert.That(reopened.Capabilities.PreservingRewrite, Is.True);
        });
    }

    [Test]
    public void RewriteRejectsRelocationWhenMakerNoteOffsetSemanticsAreUnknown()
    {
        RawDocument document = Open(CreateSource());
        RawMakerNote original = document.Metadata.MakerNotes.Single();
        document.Metadata.MakerNotes[0] = new RawMakerNote(original.CopyBytes(), "Canon", "Unknown", original.Offset, original.EntryOffset, original.IfdPath, original.ByteOrder, RawMakerNoteOffsetBase.Unknown, -1, -1, Array.Empty<RawMakerNoteOffsetReference>(), false);
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Replace("Artist", new string[] { new string('x', 80) }, null, RawMetadataSource.Exif);
        Assert.That(edits.Apply(document, false).Succeeded(), Is.True);

        RawUnsupportedException? error = Assert.Throws<RawUnsupportedException>(() => RawWriter.RewriteCr2(document, new RawWriteOptions()));

        Assert.That((error ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
    }

    [Test]
    public void RewriteRequiresPreservingModeAndRejectsPartialXmpEdits()
    {
        RawDocument document = Open(CreateSource());
        RawWriteOptions wrongMode = new RawWriteOptions();
        wrongMode.Mode = RawWriteMode.NewContainer;
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Add("Headline", new string[] { "new headline" }, null, RawMetadataSource.Xmp);
        Assert.That(edits.Apply(document, false).Succeeded(), Is.True);

        RawWriteException? mode = Assert.Throws<RawWriteException>(() => RawWriter.RewriteCr2(document, wrongMode));
        RawUnsupportedException? representation = Assert.Throws<RawUnsupportedException>(() => RawWriter.RewriteCr2(document, new RawWriteOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((mode ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((representation ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    private static byte[] CreateSource()
    {
        RawWriteProfile profile = Cr2EncoderTests.SmallProfile();
        RawCr2WriteInput input = Cr2EncoderTests.Input(8, 4, 8);
        RawWriteOptions options = Cr2EncoderTests.Options();
        return Cr2Encoder.Encode(input, profile, options);
    }

    private static RawDocument Open(byte[] source)
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return RawCodec.Open(source, null, options);
    }

    private static byte[] Preview(int width, int height, byte start)
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = new byte[width * height];
        int index = 0;
        while (index < plane.Samples.Length)
        {
            plane.Samples[index] = (byte)(start + index % 11);
            index++;
        }
        image.Planes.Add(plane);
        return BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
    }
}
