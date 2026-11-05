using raw;

namespace raw.Tests;

public class PublicContainerWriterDestinationTests
{
    [Test]
    public void CustomCreateStreamsRemainOpenAndMatchPostValidatedMemoryOutputs()
    {
        IRawWriter writer = new RawWriterService();
        RawProfileCatalogue cr2Catalogue = CreateCr2Catalogue();
        RawCr2WriteInput cr2Input = Cr2EncoderTests.Input(8, 4, 8);
        RawWriteOptions cr2Options = Cr2EncoderTests.Options();
        cr2Options.AllowExperimentalProfiles = false;
        cr2Options.ProfileName = "test.destination.cr2.write";
        byte[] expectedCr2 = writer.EncodeCr2WithProfiles(cr2Input, cr2Catalogue, cr2Options);
        byte[] repeatedCr2 = writer.EncodeCr2WithProfiles(cr2Input, cr2Catalogue, cr2Options);
        RawWritePlan cr2Plan = writer.PlanCr2WithProfiles(cr2Input, cr2Catalogue, cr2Options);
        using MemoryStream cr2Stream = new MemoryStream();

        writer.EncodeCr2WithProfilesToStream(cr2Stream, cr2Input, cr2Catalogue, cr2Options);
        byte[] streamedCr2 = cr2Stream.ToArray();

        RawProfileCatalogue nefCatalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawNefWriteInput nefInput = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions nefOptions = LossyNefOptions();
        byte[] expectedNef = writer.EncodeNefWithProfiles(nefInput, nefCatalogue, nefOptions);
        byte[] repeatedNef = writer.EncodeNefWithProfiles(nefInput, nefCatalogue, nefOptions);
        RawWritePlan nefPlan = writer.PlanNefWithProfiles(nefInput, nefCatalogue, nefOptions);
        using MemoryStream nefStream = new MemoryStream();

        writer.EncodeNefWithProfilesToStream(nefStream, nefInput, nefCatalogue, nefOptions);
        byte[] streamedNef = nefStream.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(cr2Stream.CanWrite, Is.True);
            Assert.That(nefStream.CanWrite, Is.True);
            Assert.That(streamedCr2, Is.EqualTo(expectedCr2));
            Assert.That(streamedNef, Is.EqualTo(expectedNef));
            Assert.That(repeatedCr2, Is.EqualTo(expectedCr2));
            Assert.That(repeatedNef, Is.EqualTo(expectedNef));
            Assert.That(cr2Plan.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.Identity));
            Assert.That(cr2Plan.PixelValidation.Required, Is.True);
            Assert.That(nefPlan.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.ErrorBound));
            Assert.That(nefPlan.PixelValidation.Required, Is.True);
            Assert.That(nefPlan.PixelValidation.AllowedMaximumAbsoluteError, Is.EqualTo(1));
            Assert.That(Open(streamedCr2).Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(Open(streamedNef).Format, Is.EqualTo(RawFormat.Nef));
        });
    }

    [Test]
    public void NonSeekableStreamsCoverCr2AndNefCreateAndRewriteDestinations()
    {
        IRawWriter writer = new RawWriterService();
        RawProfileCatalogue cr2Catalogue = CreateCr2Catalogue();
        RawCr2WriteInput cr2Input = Cr2EncoderTests.Input(8, 4, 8);
        RawWriteOptions cr2Options = Cr2EncoderTests.Options();
        cr2Options.AllowExperimentalProfiles = false;
        cr2Options.ProfileName = "test.destination.cr2.write";
        byte[] cr2Created = writer.EncodeCr2WithProfiles(cr2Input, cr2Catalogue, cr2Options);
        RawProfileCatalogue nefCatalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawNefWriteInput nefInput = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions nefOptions = LossyNefOptions();
        byte[] nefCreated = writer.EncodeNefWithProfiles(nefInput, nefCatalogue, nefOptions);
        ContainerWriteTrackingStream cr2CreateOutput = new ContainerWriteTrackingStream();
        ContainerWriteTrackingStream nefCreateOutput = new ContainerWriteTrackingStream();

        writer.EncodeCr2WithProfilesToStream(cr2CreateOutput, cr2Input, cr2Catalogue, cr2Options);
        writer.EncodeNefWithProfilesToStream(nefCreateOutput, nefInput, nefCatalogue, nefOptions);

        RawDocument cr2Document = Open(cr2Created);
        RawDocument nefDocument = Open(NefParserTests.BuildNef().Bytes);
        byte[] cr2Rewritten = writer.RewriteCr2(cr2Document, new RawWriteOptions());
        byte[] nefRewritten = writer.RewriteNef(nefDocument, new RawWriteOptions());
        ContainerWriteTrackingStream cr2RewriteOutput = new ContainerWriteTrackingStream();
        ContainerWriteTrackingStream nefRewriteOutput = new ContainerWriteTrackingStream();
        writer.RewriteCr2ToStream(cr2RewriteOutput, cr2Document, new RawWriteOptions());
        writer.RewriteNefToStream(nefRewriteOutput, nefDocument, new RawWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(cr2CreateOutput.Bytes(), Is.EqualTo(cr2Created));
            Assert.That(nefCreateOutput.Bytes(), Is.EqualTo(nefCreated));
            Assert.That(cr2RewriteOutput.Bytes(), Is.EqualTo(cr2Rewritten));
            Assert.That(nefRewriteOutput.Bytes(), Is.EqualTo(nefRewritten));
            Assert.That(new ContainerWriteTrackingStream[] { cr2CreateOutput, nefCreateOutput, cr2RewriteOutput, nefRewriteOutput }, Has.All.Matches<ContainerWriteTrackingStream>(stream => !stream.Disposed && !stream.CanSeek));
            Assert.That(Open(cr2CreateOutput.Bytes()).Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(Open(nefCreateOutput.Bytes()).Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(Open(cr2RewriteOutput.Bytes()).Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(Open(nefRewriteOutput.Bytes()).Format, Is.EqualTo(RawFormat.Nef));
        });
    }

    [Test]
    public void PublicPreviewEditsFlowThroughCr2AndNefRewritesWithoutChangingRawPayloads()
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawDocument cr2 = Open(Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options()));
        RawDocument nef = Open(NefParserTests.BuildNef().Bytes);
        byte[] cr2Raw = RawPayload(codec, cr2);
        byte[] nefRaw = RawPayload(codec, nef);
        RawThumbnail replacement = codec.EncodeJpegThumbnail(PreviewImage(), 0, 90, new RawLimits(), CancellationToken.None);
        RawMetadataEditSet cr2Edits = codec.CreateMetadataEditSet();
        RawMetadataEditSet nefEdits = codec.CreateMetadataEditSet();
        codec.AddPreviewReplacementEdit(cr2Edits, 0, replacement.Kind, replacement.Width, replacement.Height, replacement.Orientation, replacement.MimeType, replacement.EncodedBytes);
        codec.AddPreviewReplacementEdit(nefEdits, 0, replacement.Kind, replacement.Width, replacement.Height, replacement.Orientation, replacement.MimeType, replacement.EncodedBytes);

        RawMetadataEditReport cr2Report = codec.ApplyDocumentMetadataEdits(cr2, cr2Edits);
        RawMetadataEditReport nefReport = codec.ApplyDocumentMetadataEdits(nef, nefEdits);
        RawDocument rewrittenCr2 = Open(writer.RewriteCr2(cr2, new RawWriteOptions()));
        RawDocument rewrittenNef = Open(writer.RewriteNef(nef, new RawWriteOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(codec.MetadataEditSucceeded(cr2Report), Is.True);
            Assert.That(codec.MetadataEditSucceeded(nefReport), Is.True);
            Assert.That(rewrittenCr2.Thumbnails[0].EncodedBytes, Is.EqualTo(replacement.EncodedBytes));
            Assert.That(rewrittenNef.Thumbnails[0].EncodedBytes, Is.EqualTo(replacement.EncodedBytes));
            Assert.That(RawPayload(codec, rewrittenCr2), Is.EqualTo(cr2Raw));
            Assert.That(RawPayload(codec, rewrittenNef), Is.EqualTo(nefRaw));
            Assert.That(rewrittenCr2.Metadata.MakerNotes.Single().RawBytes, Is.EqualTo(cr2.Metadata.MakerNotes.Single().RawBytes));
            Assert.That(rewrittenNef.Metadata.MakerNotes.Single().RawBytes, Is.EqualTo(nef.Metadata.MakerNotes.Single().RawBytes));
        });
    }

    [Test]
    public void RewriteStreamsRemainOpenAndPreserveRawPayloadIdentity()
    {
        IRawWriter writer = new RawWriterService();
        IRawCodec codec = new RawCodecService();
        byte[] cr2Source = Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options());
        RawDocument cr2Document = Open(cr2Source);
        byte[] expectedCr2 = writer.RewriteCr2(cr2Document, new RawWriteOptions());
        using MemoryStream cr2Stream = new MemoryStream();
        writer.RewriteCr2ToStream(cr2Stream, cr2Document, new RawWriteOptions());
        RawDocument rewrittenCr2 = Open(cr2Stream.ToArray());

        byte[] nefSource = NefParserTests.BuildNef().Bytes;
        RawDocument nefDocument = Open(nefSource);
        byte[] expectedNef = writer.RewriteNef(nefDocument, new RawWriteOptions());
        using MemoryStream nefStream = new MemoryStream();
        writer.RewriteNefToStream(nefStream, nefDocument, new RawWriteOptions());
        RawDocument rewrittenNef = Open(nefStream.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(cr2Stream.CanWrite, Is.True);
            Assert.That(nefStream.CanWrite, Is.True);
            Assert.That(cr2Stream.ToArray(), Is.EqualTo(expectedCr2));
            Assert.That(nefStream.ToArray(), Is.EqualTo(expectedNef));
            Assert.That(RawPayload(codec, rewrittenCr2), Is.EqualTo(RawPayload(codec, cr2Document)));
            Assert.That(RawPayload(codec, rewrittenNef), Is.EqualTo(RawPayload(codec, nefDocument)));
        });
    }

    [Test]
    public void CustomCreatePathsCommitValidatedCr2AndNefThroughSiblingStages()
    {
        IRawWriter writer = new RawWriterService();
        string directory = CreateDirectory();
        string cr2Path = Path.Combine(directory, "created.cr2");
        string nefPath = Path.Combine(directory, "created.nef");
        RawProfileCatalogue cr2Catalogue = CreateCr2Catalogue();
        RawCr2WriteInput cr2Input = Cr2EncoderTests.Input(8, 4, 8);
        RawWriteOptions cr2Options = Cr2EncoderTests.Options();
        cr2Options.AllowExperimentalProfiles = false;
        cr2Options.ProfileName = "test.destination.cr2.write";
        byte[] expectedCr2 = writer.EncodeCr2WithProfiles(cr2Input, cr2Catalogue, cr2Options);
        RawProfileCatalogue nefCatalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawNefWriteInput nefInput = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions nefOptions = LossyNefOptions();
        byte[] expectedNef = writer.EncodeNefWithProfiles(nefInput, nefCatalogue, nefOptions);

        RawPathWriteResult cr2Result = writer.EncodeCr2WithProfilesToPath(cr2Path, cr2Input, cr2Catalogue, cr2Options);
        RawPathWriteResult nefResult = writer.EncodeNefWithProfilesToPath(nefPath, nefInput, nefCatalogue, nefOptions);

        Assert.Multiple(() =>
        {
            AssertCommitted(cr2Result, cr2Path, expectedCr2);
            AssertCommitted(nefResult, nefPath, expectedNef);
            Assert.That(Open(File.ReadAllBytes(cr2Path)).Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(Open(File.ReadAllBytes(nefPath)).Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(2));
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void RewritePathsCommitValidatedCr2AndNefAndExposeStagingResults()
    {
        IRawWriter writer = new RawWriterService();
        string directory = CreateDirectory();
        string cr2Path = Path.Combine(directory, "rewrite.cr2");
        string nefPath = Path.Combine(directory, "rewrite.nef");
        RawDocument cr2Document = Open(Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options()));
        RawDocument nefDocument = Open(NefParserTests.BuildNef().Bytes);
        byte[] expectedCr2 = writer.RewriteCr2(cr2Document, new RawWriteOptions());
        byte[] expectedNef = writer.RewriteNef(nefDocument, new RawWriteOptions());
        File.WriteAllBytes(cr2Path, new byte[] { 1, 2, 3 });
        File.WriteAllBytes(nefPath, new byte[] { 4, 5, 6 });

        RawPathWriteResult cr2Result = writer.RewriteCr2ToPath(cr2Path, cr2Document, new RawWriteOptions());
        RawPathWriteResult nefResult = writer.RewriteNefToPath(nefPath, nefDocument, new RawWriteOptions());

        Assert.Multiple(() =>
        {
            AssertCommitted(cr2Result, cr2Path, expectedCr2);
            AssertCommitted(nefResult, nefPath, expectedNef);
            Assert.That(cr2Result.TemporaryPath, Is.Empty);
            Assert.That(nefResult.TemporaryPath, Is.Empty);
            Assert.That(cr2Result.Diagnostics, Is.Empty);
            Assert.That(nefResult.Diagnostics, Is.Empty);
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void BuiltInCreateDestinationsRejectUnavailableProfilesBeforeWritingOrStaging()
    {
        IRawWriter writer = new RawWriterService();
        RawWriteOptions cr2Options = Cr2EncoderTests.Options();
        cr2Options.ProfileName = "missing.cr2.profile";
        RawWriteOptions nefOptions = NefEncoderTests.Options();
        nefOptions.ProfileName = "missing.nef.profile";
        using MemoryStream cr2Stream = new MemoryStream();
        using MemoryStream nefStream = new MemoryStream();
        string directory = CreateDirectory();
        string cr2Path = Path.Combine(directory, "missing.cr2");
        string nefPath = Path.Combine(directory, "missing.nef");

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => writer.EncodeCr2ToStream(cr2Stream, Cr2EncoderTests.Input(8, 4, 8), cr2Options));
            Assert.Throws<RawUnsupportedException>(() => writer.EncodeNefToStream(nefStream, NefEncoderTests.Input(8, 4, 8), nefOptions));
            Assert.Throws<RawUnsupportedException>(() => writer.EncodeCr2ToPath(cr2Path, Cr2EncoderTests.Input(8, 4, 8), cr2Options));
            Assert.Throws<RawUnsupportedException>(() => writer.EncodeNefToPath(nefPath, NefEncoderTests.Input(8, 4, 8), nefOptions));
            Assert.That(cr2Stream.CanWrite, Is.True);
            Assert.That(nefStream.CanWrite, Is.True);
            Assert.That(cr2Stream.Length, Is.Zero);
            Assert.That(nefStream.Length, Is.Zero);
            Assert.That(Directory.GetFiles(directory), Is.Empty);
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void PublicLossyPlanAndDestinationsRequireExplicitConsentBeforeOutput()
    {
        IRawWriter writer = new RawWriterService();
        RawProfileCatalogue catalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawNefWriteInput input = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions options = NefEncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.lossy.write";
        options.AllowLossy = true;
        options.DataLossConsent = false;
        RawWritePlan plan = writer.PlanNefWithProfiles(input, catalogue, options);
        ContainerWriteTrackingStream output = new ContainerWriteTrackingStream();
        string directory = CreateDirectory();
        string path = Path.Combine(directory, "blocked.nef");

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.False);
            Assert.That(plan.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.ErrorBound));
            Assert.That(plan.PixelValidation.Required, Is.True);
            Assert.That(plan.PixelValidation.AllowedMaximumAbsoluteError, Is.EqualTo(1));
            Assert.That(plan.Changes.Any(change => change.RequiresConsent), Is.True);
            Assert.That(plan.Diagnostics.Any(item => item.Code == RawDiagnosticCode.DataLossConsentRequired), Is.True);
            Assert.Throws<RawWriteException>(() => writer.EncodeNefWithProfiles(input, catalogue, options));
            Assert.Throws<RawWriteException>(() => writer.EncodeNefWithProfilesToStream(output, input, catalogue, options));
            Assert.Throws<RawWriteException>(() => writer.EncodeNefWithProfilesToPath(path, input, catalogue, options));
            Assert.That(output.Bytes(), Is.Empty);
            Assert.That(output.Disposed, Is.False);
            Assert.That(File.Exists(path), Is.False);
        });
        Directory.Delete(directory, true);
    }

    static RawProfileCatalogue CreateCr2Catalogue()
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateCanonEos6DMarkIi();
        read.Id = "test.destination.cr2.read";
        read.Fingerprint.Width = 8;
        read.Fingerprint.Height = 4;
        read.Geometry.StoredWidth = 8;
        read.Geometry.StoredHeight = 4;
        read.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        read.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        read.Codec.SliceWidths = new int[] { 4, 4 };
        read.Experimental = false;
        RawWriteProfile write = Cr2EncoderTests.SmallProfile();
        write.Id = "test.destination.cr2.write";
        write.ReadProfileId = read.Id;
        write.Experimental = false;
        write.Evidence = new RawProfileEvidence[]
        {
            Evidence(RawProfileEvidenceKind.Structural, "Independent Structure Validator"),
            Evidence(RawProfileEvidenceKind.Application, "Independent CR2 Application")
        };
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileRegistrationResult registration = catalogue.RegisterCustomDocument(document, false, new RawLimits());
        Assert.That(registration.Accepted, Is.True);
        return catalogue;
    }

    static RawProfileEvidence Evidence(RawProfileEvidenceKind kind, string tool)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = kind;
        evidence.Tool = tool;
        evidence.Version = "1.0";
        evidence.SampleId = "destination-test";
        evidence.Check = "encode-reopen-verify";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Passed.";
        return evidence;
    }

    static RawWriteOptions LossyNefOptions()
    {
        RawWriteOptions options = NefEncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.lossy.write";
        options.AllowLossy = true;
        options.DataLossConsent = true;
        return options;
    }

    static RawDocument Open(byte[] source)
    {
        IRawCodec codec = new RawCodecService();
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return codec.OpenBytes(source, null, options);
    }

    static byte[] RawPayload(IRawCodec codec, RawDocument document)
    {
        RawPayloadBlock[] blocks = document.PayloadMap.Blocks.Where(item => item.Kind == RawPayloadKind.RawImage).OrderBy(item => item.Offset).ToArray();
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < blocks.Length)
        {
            bytes.AddRange(codec.ExtractDocumentPayload(document, blocks[index], new RawLimits(), CancellationToken.None));
            index++;
        }
        return bytes.ToArray();
    }

    static RawRgbImage PreviewImage()
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        return new RawRgbImage(3, 2, 3, 9, new byte[]
        {
            255, 0, 0, 0, 255, 0, 0, 0, 255,
            255, 255, 0, 0, 255, 255, 255, 0, 255
        }, null, null, channels, RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, true, true, null);
    }

    static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "raw-public-container-destination-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    static void AssertCommitted(RawPathWriteResult result, string path, byte[] expected)
    {
        Assert.That(result.Validated, Is.True);
        Assert.That(result.Committed, Is.True);
        Assert.That(result.DestinationPath, Is.EqualTo(Path.GetFullPath(path)));
        Assert.That(result.TemporaryPath, Is.Empty);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
    }
}

internal class ContainerWriteTrackingStream : Stream
{
    internal MemoryStream Inner;
    internal bool Disposed;

    internal ContainerWriteTrackingStream()
    {
        Inner = new MemoryStream();
        Disposed = false;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    internal byte[] Bytes()
    {
        return Inner.ToArray();
    }

    public override void Flush()
    {
        Inner.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Inner.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        Inner.Dispose();
        base.Dispose(disposing);
    }
}
