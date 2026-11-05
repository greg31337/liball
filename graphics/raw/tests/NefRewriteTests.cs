using raw;

namespace raw.Tests;

public class NefRewriteTests
{
    [Test]
    public void RewriteAppliesExifAndPreviewEditsWhilePreservingRawCaptureEditAndMakerNoteValues()
    {
        NefFixture fixture = NefParserTests.BuildNef();
        RawDocument document = Open(fixture.Bytes);
        byte[][] rawPayloads = document.PayloadMap.Find(RawPayloadKind.RawImage).Select(document.ExtractPayload).ToArray();
        RawMakerNote originalNote = document.Metadata.MakerNotes.Single();
        byte[] capture = (originalNote.FindNikonTag(0x0e01) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes();
        byte[] captureEdit = (originalNote.FindNikonTag(0x0e13) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes();
        byte[] retainedThumbnail = document.Thumbnails[1].EncodedBytes.ToArray();
        byte[] replacement = Preview(6, 6, 173);
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Add("Artist", new string[] { "Reviewed NEF Creator" }, null, RawMetadataSource.Exif);
        edits.ReplacePreview(0, RawThumbnailKind.Jpeg, 6, 6, RawOrientation.Normal, "image/jpeg", replacement);
        RawMetadataEditReport report = edits.Apply(document, false);

        byte[] rewritten = RawWriter.RewriteNef(document, new RawWriteOptions());
        RawDocument reopened = Open(rewritten);
        RawMakerNote reopenedNote = reopened.Metadata.MakerNotes.Single();
        byte[][] reopenedRaw = reopened.PayloadMap.Find(RawPayloadKind.RawImage).Select(reopened.ExtractPayload).ToArray();
        RawPayloadBlock reopenedCapture = reopened.PayloadMap.Find(RawPayloadKind.Metadata).Single(block => block.Path.EndsWith("/NikonCaptureData", StringComparison.Ordinal));
        RawPayloadBlock reopenedEdit = reopened.PayloadMap.Find(RawPayloadKind.Metadata).Single(block => block.Path.EndsWith("/NikonCaptureEditVersions", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(report.Succeeded(), Is.True);
            Assert.That(((reopened.Metadata.FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(315) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("Reviewed NEF Creator"));
            Assert.That(reopened.Thumbnails[0].EncodedBytes, Is.EqualTo(replacement));
            Assert.That(reopened.Thumbnails[0].Width, Is.EqualTo(6));
            Assert.That(reopened.Thumbnails[0].Height, Is.EqualTo(6));
            Assert.That(reopened.Thumbnails[1].EncodedBytes, Is.EqualTo(retainedThumbnail));
            Assert.That(reopenedRaw, Is.EqualTo(rawPayloads));
            Assert.That(reopened.ExtractPayload(reopenedCapture), Is.EqualTo(fixture.Capture));
            Assert.That(reopened.ExtractPayload(reopenedEdit), Is.EqualTo(fixture.Edit));
            Assert.That((reopenedNote.FindNikonTag(0x0e01) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo(capture));
            Assert.That((reopenedNote.FindNikonTag(0x0e13) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo(captureEdit));
            Assert.That(reopenedNote.OffsetBase, Is.EqualTo(RawMakerNoteOffsetBase.EmbeddedTiffHeader));
            Assert.That(reopenedNote.Offset, Is.Not.EqualTo(originalNote.Offset));
            Assert.That(reopened.Capabilities.PreservingRewrite, Is.True);
        });
    }

    [Test]
    public void RewriteRejectsUnknownMakerNoteRelocationAndNonPreservingPolicy()
    {
        RawDocument unknown = Open(NefParserTests.BuildNef().Bytes);
        RawMakerNote original = unknown.Metadata.MakerNotes.Single();
        unknown.Metadata.MakerNotes[0] = new RawMakerNote(original.CopyBytes(), "NIKON CORPORATION", "Unknown", original.Offset, original.EntryOffset, original.IfdPath, original.ByteOrder, RawMakerNoteOffsetBase.Unknown, -1, -1, Array.Empty<RawMakerNoteOffsetReference>(), false);
        RawUnsupportedException? makerNote = Assert.Throws<RawUnsupportedException>(() => RawWriter.RewriteNef(unknown, new RawWriteOptions()));
        RawDocument policyDocument = Open(NefParserTests.BuildNef().Bytes);
        RawWriteOptions policy = new RawWriteOptions();
        policy.UnknownDataPolicy = RawUnknownDataPolicy.DropWithConsent;
        policy.DataLossConsent = true;
        RawUnsupportedException? unknownData = Assert.Throws<RawUnsupportedException>(() => RawWriter.RewriteNef(policyDocument, policy));

        Assert.Multiple(() =>
        {
            Assert.That((makerNote ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((unknownData ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [Test]
    public void RewriteRequiresPreservingModeAndRejectsPartialXmpEdits()
    {
        RawDocument document = Open(NefParserTests.BuildNef().Bytes);
        RawWriteOptions wrongMode = new RawWriteOptions();
        wrongMode.Mode = RawWriteMode.NewContainer;
        RawWriteException? mode = Assert.Throws<RawWriteException>(() => RawWriter.RewriteNef(document, wrongMode));
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Add("Headline", new string[] { "new headline" }, null, RawMetadataSource.Xmp);
        Assert.That(edits.Apply(document, false).Succeeded(), Is.True);
        RawUnsupportedException? representation = Assert.Throws<RawUnsupportedException>(() => RawWriter.RewriteNef(document, new RawWriteOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((mode ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((representation ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    static RawDocument Open(byte[] source)
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return RawCodec.Open(source, null, options);
    }

    static byte[] Preview(int width, int height, byte start)
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
            plane.Samples[index] = (byte)(start + index % 13);
            index++;
        }
        image.Planes.Add(plane);
        return BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
    }
}
