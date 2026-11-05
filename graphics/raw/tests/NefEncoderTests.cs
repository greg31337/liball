using raw;

namespace raw.Tests;

public class NefEncoderTests
{
    [Test]
    public void SmallProfileEncodesUncompressedNefWithExactPixelsExifMakerNoteAndPreview()
    {
        RawWriteProfile profile = SmallProfile();
        RawNefWriteInput input = Input(8, 4, 10);
        RawWriteOptions options = Options();

        byte[] encoded = NefEncoder.Encode(input, profile, options);
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        openOptions.ReadPreviews = true;
        RawDocument document = RawCodec.Open(encoded, null, openOptions);
        RawImageInfo? rawInfo = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
        RawIfd? ifd0 = document.Metadata.FindIfd("IFD0");
        RawIfd? rawIfd = document.Metadata.FindIfd("IFD0/SubIFD0");
        RawIfd? previewIfd = document.Metadata.FindIfd("IFD0/SubIFD1");
        RawIfd? exif = document.Metadata.FindIfd("IFD0/Exif");
        RawMakerNote note = document.Metadata.MakerNotes.Single();
        ushort[] decoded = DecodeRows(document.ExtractPayload(rawInfo.Payload), input.Width, input.Height);

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(((ifd0 ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(271) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("NIKON CORPORATION"));
            Assert.That((ifd0.FindTag(272) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("NIKON TEST"));
            Assert.That((ifd0.FindTag(305) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("raw-tests"));
            Assert.That((ifd0.FindTag(315) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("Test Creator"));
            Assert.That((ifd0.FindTag(33432) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("Test Rights"));
            Assert.That(((rawIfd ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(258) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 14 }));
            Assert.That((rawIfd.FindTag(259) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 1 }));
            Assert.That((rawIfd.FindTag(278) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values(), Is.EqualTo(new uint[] { 4 }));
            Assert.That((rawIfd.FindTag(33421) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 2, 2 }));
            Assert.That((rawIfd.FindTag(33422) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetByteValues(), Is.EqualTo(new byte[] { 0, 1, 1, 2 }));
            Assert.That((previewIfd ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(273), Is.Not.Null);
            Assert.That((previewIfd.FindTag(279) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values()[0], Is.EqualTo((uint)(input.Preview ?? throw new System.InvalidOperationException("A required value is unavailable.")).EncodedBytes.Length));
            Assert.That(((exif ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(36867) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("2026:08:20 12:34:56"));
            Assert.That((exif.FindTag(36881) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("-07:00"));
            Assert.That((exif.FindTag(37521) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("123"));
            Assert.That(note.Signature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That(note.OffsetBase, Is.EqualTo(RawMakerNoteOffsetBase.EmbeddedTiffHeader));
            Assert.That(note.NikonVersion, Is.EqualTo("0200"));
            Assert.That((note.FindNikonTag(0x0002) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 0, 400 }));
            Assert.That((note.FindNikonTag(0x0004) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("RAW"));
            Assert.That((note.FindNikonTag(0x0005) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetText(), Is.EqualTo("Auto"));
            Assert.That((note.FindNikonTag(0x0093) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { NefEncoder.Uncompressed14MakerCode }));
            Assert.That(document.Thumbnails, Has.Length.EqualTo(1));
            Assert.That(document.Thumbnails[0].EncodedBytes, Is.EqualTo(input.Preview.EncodedBytes));
            Assert.That(decoded, Is.EqualTo(ContiguousSamples(input)));
        });
    }

    [Test]
    public void EncoderRejectsInvalidSamplesMetadataPreviewAndPackingProfile()
    {
        RawWriteOptions options = Options();
        RawNefWriteInput samples = Input(8, 4, 8);
        samples.StoredSamples[0] = 16384;
        RawNefWriteInput metadata = Input(8, 4, 8);
        metadata.Metadata.IsoSensitivity = 0;
        RawNefWriteInput preview = Input(8, 4, 8);
        (preview.Preview ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width = 7;
        RawWriteProfile invalidProfile = SmallProfile();
        invalidProfile.Codec.Packing = RawPacking.ByteAligned;

        RawWriteException? range = Assert.Throws<RawWriteException>(() => NefEncoder.Encode(samples, SmallProfile(), options));
        RawWriteException? missing = Assert.Throws<RawWriteException>(() => NefEncoder.Encode(metadata, SmallProfile(), options));
        RawWriteException? image = Assert.Throws<RawWriteException>(() => NefEncoder.Encode(preview, SmallProfile(), options));
        RawWriteException? profile = Assert.Throws<RawWriteException>(() => NefEncoder.Encode(Input(8, 4, 8), invalidProfile, options));

        Assert.Multiple(() =>
        {
            Assert.That((range ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((missing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((image ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
        });
    }

    [Test]
    public void PublicEncoderRequiresNewContainerModeSupportedProfileAndExperimentalOptIn()
    {
        RawNefWriteInput input = Input(8, 4, 8);
        RawWriteOptions wrongMode = Options();
        wrongMode.Mode = RawWriteMode.PreservingRewrite;
        RawWriteOptions blocked = Options();
        blocked.AllowExperimentalProfiles = false;
        blocked.ProfileName = RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile;
        RawWriteOptions compressed = Options();
        compressed.ProfileName = RawBuiltInProfileCatalogue.NikonZ8WriteProfile;
        RawWriteOptions missing = Options();
        missing.ProfileName = "missing.nef.write.profile";

        RawWriteException? mode = Assert.Throws<RawWriteException>(() => RawWriter.EncodeNef(input, wrongMode));
        RawUnsupportedException? optIn = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeNef(input, blocked));
        RawWriteException? geometry = Assert.Throws<RawWriteException>(() => RawWriter.EncodeNef(input, compressed));
        RawUnsupportedException? profile = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeNef(input, missing));

        Assert.Multiple(() =>
        {
            Assert.That((mode ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((optIn ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((geometry ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [Test]
    public void EveryEnabledLosslessProfileEncodesExactPixelsAndRequiredNikonParameters()
    {
        RawWriteProfile[] profiles = RawBuiltInProfileCatalogue.CreateDocument().WriteProfiles.Where(item => item.Codec.Name == NikonCompressionCodec.LosslessCodecName).ToArray();
        Assert.That(profiles, Has.Length.GreaterThan(0));
        int profileIndex = 0;
        while (profileIndex < profiles.Length)
        {
            RawWriteProfile profile = profiles[profileIndex];
            profile.Id = "test.lossless." + profileIndex;
            profile.ReadProfileId = "test.lossless.read." + profileIndex;
            profile.Identity.Model = "NIKON TEST LOSSLESS";
            profile.Identity.Aliases = new string[] { "Nikon Test Lossless" };
            profile.Fingerprint.Width = 8;
            profile.Fingerprint.Height = 4;
            profile.Geometry.StoredWidth = 8;
            profile.Geometry.StoredHeight = 4;
            profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
            profile.Codec.SliceWidths = new int[] { 8 };
            RawNefWriteInput input = ExtremeInput(8, 4, 10);

            byte[] encoded = NefEncoder.Encode(input, profile, Options());
            RawOpenOptions openOptions = new RawOpenOptions();
            openOptions.AllowExperimentalProfiles = true;
            openOptions.ReadPreviews = true;
            RawDocument document = RawCodec.Open(encoded, null, openOptions);
            RawImageInfo? rawInfo = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
            RawIfd? rawIfd = document.Metadata.FindIfd("IFD0/SubIFD0");
            RawMakerNote note = document.Metadata.MakerNotes.Single();
            RawNikonMakerNoteTag? compressionTag = note.FindNikonTag(0x0096);
            NikonCompressionInfo compression = NikonCompressionCodec.ParseInfo((compressionTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), note.ByteOrder, 14, input.Height);
            ushort[] decoded = DecodeLossless(document.ExtractPayload(rawInfo.Payload), input.Width, input.Height, compression);

            Assert.Multiple(() =>
            {
                Assert.That(((rawIfd ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(259) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 34713 }));
                Assert.That((note.FindNikonTag(0x0093) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { NefEncoder.LosslessMakerCode }));
                Assert.That(compressionTag.CopyBytes()[0], Is.EqualTo(0x46));
                Assert.That(compression.Lossless, Is.True);
                Assert.That(compression.TreeIndex, Is.EqualTo(5));
                Assert.That(decoded, Is.EqualTo(ContiguousSamples(input)));
                Assert.That(document.Thumbnails.Single().EncodedBytes, Is.EqualTo((input.Preview ?? throw new System.InvalidOperationException("A required value is unavailable.")).EncodedBytes));
            });
            profileIndex++;
        }
    }

    [Test]
    public void ConfirmedLossyProfileRequiresConsentReportsQuantizationAndHonorsErrorBound()
    {
        RawProfileCatalogue catalogue = QualifiedLossyCatalogue();
        RawNefWriteInput input = ExtremeInput(8, 4, 10);
        RawWriteOptions blocked = Options();
        blocked.AllowExperimentalProfiles = false;
        blocked.ProfileName = "test.nikon.nef.lossy.write";
        RawUnsupportedException? noOptIn = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeNef(input, catalogue, blocked));
        RawWriteOptions noConsent = Options();
        noConsent.AllowExperimentalProfiles = false;
        noConsent.ProfileName = "test.nikon.nef.lossy.write";
        noConsent.AllowLossy = true;
        RawWriteException? consent = Assert.Throws<RawWriteException>(() => RawWriter.EncodeNef(input, catalogue, noConsent));
        RawWriteOptions options = Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.lossy.write";
        options.AllowLossy = true;
        options.DataLossConsent = true;

        byte[] encoded = RawWriter.EncodeNef(input, catalogue, options);
        RawWritePlan plan = RawWriter.PlanNef(input, catalogue, options);
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.ReadPreviews = true;
        RawDocument document = RawCodec.Open(encoded, null, openOptions);
        RawImageInfo? rawInfo = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
        RawMakerNote note = document.Metadata.MakerNotes.Single();
        RawNikonMakerNoteTag? compressionTag = note.FindNikonTag(0x0096);
        NikonCompressionInfo compression = NikonCompressionCodec.ParseInfo((compressionTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), note.ByteOrder, 14, input.Height);
        ushort[] decoded = DecodeLossless(document.ExtractPayload(rawInfo.Payload), input.Width, input.Height, compression);
        ushort[] source = ContiguousSamples(input);
        int[] errors = decoded.Zip(source, (actual, original) => Math.Abs(actual - original)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That((noOptIn ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((consent ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((note.FindNikonTag(0x0093) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { NefEncoder.LossyMakerCode }));
            Assert.That(compressionTag.CopyBytes()[0], Is.EqualTo(0x44));
            Assert.That(compression.Lossless, Is.False);
            Assert.That(compression.TreeIndex, Is.EqualTo(3));
            Assert.That(compression.Curve, Has.Length.EqualTo(8193));
            Assert.That(compression.Curve[1], Is.EqualTo(2));
            Assert.That(compression.Curve[^1], Is.EqualTo(16383));
            Assert.That(errors, Is.All.LessThanOrEqualTo(NikonCompressionCodec.LossyMaximumError));
            Assert.That(errors, Does.Contain(1));
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(encoded.Length));
            Assert.That(plan.ProfileName, Is.EqualTo("test.nikon.nef.lossy.write"));
            Assert.That(plan.RequiresDataLossConsent(), Is.True);
            Assert.That(plan.Changes.Single(item => item.Location == "IFD0/SubIFD0/Strip0").Description, Does.Contain("tree 3").And.Contain("maximum absolute stored-sample error is 1"));
            Assert.That(plan.Changes.Single(item => item.Location.EndsWith("0x0096")).Description, Does.Contain("8193-entry").And.Contain("min(2*i, 16383)").And.Contain("irreversible"));
        });
    }

    [Test]
    public void LossyProfileWithoutIndependentApplicationEvidenceIsRejected()
    {
        RawProfileCatalogue catalogue = QualifiedLossyCatalogue();
        (catalogue.FindWriteProfile("test.nikon.nef.lossy.write") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Evidence = new RawProfileEvidence[] { Evidence(RawProfileEvidenceKind.Structural, "raw structural validator") };
        RawWriteOptions options = Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.lossy.write";
        options.AllowLossy = true;
        options.DataLossConsent = true;

        RawUnsupportedException? exception = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeNef(ExtremeInput(8, 4, 10), catalogue, options));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
    }

    [Test]
    public void PublicCatalogueOverloadEncodesAnIndependentlyQualifiedAdditionalProfile()
    {
        RawProfileCatalogue catalogue = QualifiedCatalogue();
        RawNefWriteInput input = Input(8, 4, 8);
        RawWriteOptions options = Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.uncompressed.write";

        byte[] encoded = RawWriter.EncodeNef(input, catalogue, options);
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.ReadPreviews = true;
        RawDocument document = RawCodec.Open(encoded, null, openOptions);
        RawImageInfo? rawInfo = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(document.Thumbnails.Single().EncodedBytes, Is.EqualTo((input.Preview ?? throw new System.InvalidOperationException("A required value is unavailable.")).EncodedBytes));
            Assert.That(DecodeRows(document.ExtractPayload(rawInfo.Payload), 8, 4), Is.EqualTo(ContiguousSamples(input)));
        });
    }

    [Test]
    public void BuiltInZ7ProfilesBindTheUncompressedWriterToItsDecoderAndEvidence()
    {
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawReadProfile? read = (catalogue.ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == NikonNefMetadataParser.Z7UncompressedProfile);
        RawWriteProfile? write = (catalogue.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile);

        Assert.Multiple(() =>
        {
            Assert.That(write.ReadProfileId, Is.EqualTo(read.Id));
            Assert.That(read.Identity.Model, Is.EqualTo("NIKON Z 7"));
            Assert.That(read.Geometry.StoredWidth, Is.EqualTo(8256));
            Assert.That(read.Geometry.StoredHeight, Is.EqualTo(5504));
            Assert.That(read.Codec.Name, Is.EqualTo(NefUncompressedDecoder.CodecName));
            Assert.That(read.Codec.Packing, Is.EqualTo(RawPacking.Raw14));
            Assert.That(read.Codec.Compression, Is.EqualTo(1));
            Assert.That(write.Fingerprint.RequiredIfds, Is.EqualTo(new string[] { "IFD0", "IFD0/Exif", "IFD0/SubIFD0", "IFD0/SubIFD1" }));
            Assert.That(write.Fingerprint.RequiredTags, Does.Contain("IFD0/SubIFD0/278"));
            Assert.That(write.Fingerprint.RequiredTags, Does.Contain("IFD0/SubIFD1/273"));
            Assert.That(write.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefZ7SingleRawStripQuirk));
            Assert.That(write.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NikonMakerNoteEmbeddedTiffBaseQuirk));
            Assert.That(write.Evidence.Single(item => item.Kind == RawProfileEvidenceKind.Structural).Check, Is.EqualTo("new-uncompressed-parse-write-parse"));
            Assert.That(write.Evidence.Single(item => item.Kind == RawProfileEvidenceKind.Structural).Details, Does.Contain("External-application compatibility remains unconfirmed"));
            Assert.That(write.Evidence.Single(item => item.Kind == RawProfileEvidenceKind.PixelIdentity).Check, Is.EqualTo("decode-compare-stored-pixel-identity"));
            Assert.That(write.Experimental, Is.True);
        });
    }

    internal static RawWriteProfile SmallProfile()
    {
        RawWriteProfile profile = RawBuiltInProfileCatalogue.CreateNikonZ7UncompressedWrite();
        profile.Id = "test.nikon.nef.uncompressed.write";
        profile.ReadProfileId = "test.nikon.nef.uncompressed.read";
        profile.Identity.Model = "NIKON TEST";
        profile.Identity.Aliases = new string[] { "Nikon Test" };
        profile.Fingerprint.Width = 8;
        profile.Fingerprint.Height = 4;
        profile.Geometry.StoredWidth = 8;
        profile.Geometry.StoredHeight = 4;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        profile.Codec.SliceWidths = new int[] { 8 };
        return profile;
    }

    static RawProfileCatalogue QualifiedCatalogue()
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateNikonZ7Uncompressed();
        read.Id = "test.nikon.nef.uncompressed.read";
        read.Identity.Model = "NIKON TEST";
        read.Identity.Aliases = new string[] { "Nikon Test" };
        read.Fingerprint.Width = 8;
        read.Fingerprint.Height = 4;
        read.Geometry.StoredWidth = 8;
        read.Geometry.StoredHeight = 4;
        read.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        read.Codec.SliceWidths = new int[] { 8 };
        read.Experimental = false;
        RawWriteProfile write = SmallProfile();
        write.Experimental = false;
        write.Evidence = new RawProfileEvidence[]
        {
            Evidence(RawProfileEvidenceKind.Structural, "raw structural validator"),
            Evidence(RawProfileEvidenceKind.Application, "Independent NEF Application")
        };
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileRegistrationResult registration = catalogue.RegisterCustomDocument(document, false, new RawLimits());
        Assert.That(registration.Accepted, Is.True);
        return catalogue;
    }

    internal static RawProfileCatalogue QualifiedLossyCatalogue()
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateNikonZ8Lossless();
        read.Id = "test.nikon.nef.lossy.read";
        read.Identity.Model = "NIKON TEST LOSSY";
        read.Identity.Aliases = new string[] { "Nikon Test Lossy" };
        read.Fingerprint.Width = 8;
        read.Fingerprint.Height = 4;
        read.Geometry.StoredWidth = 8;
        read.Geometry.StoredHeight = 4;
        read.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        read.Codec.Name = NikonCompressionCodec.LossyCodecName;
        read.Codec.SliceWidths = new int[] { 8 };
        read.Codec.Quirks = LossyQuirks();
        read.Experimental = false;
        RawWriteProfile write = RawBuiltInProfileCatalogue.CreateNikonZ8LosslessWrite();
        write.Id = "test.nikon.nef.lossy.write";
        write.ReadProfileId = read.Id;
        write.Identity.Model = read.Identity.Model;
        write.Identity.Aliases = read.Identity.Aliases;
        write.Fingerprint.Width = 8;
        write.Fingerprint.Height = 4;
        write.Geometry.StoredWidth = 8;
        write.Geometry.StoredHeight = 4;
        write.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        write.Codec.Name = NikonCompressionCodec.LossyCodecName;
        write.Codec.SliceWidths = new int[] { 8 };
        write.Codec.Quirks = LossyQuirks();
        write.Evidence = new RawProfileEvidence[]
        {
            Evidence(RawProfileEvidenceKind.Structural, "raw structural validator"),
            Evidence(RawProfileEvidenceKind.ErrorBound, "raw NEF error-bound verifier"),
            Evidence(RawProfileEvidenceKind.Application, "Independent NEF Application")
        };
        write.Experimental = false;
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileRegistrationResult registration = catalogue.RegisterCustomDocument(document, false, new RawLimits());
        Assert.That(registration.Accepted, Is.True);
        return catalogue;
    }

    static string[] LossyQuirks()
    {
        return new string[]
        {
            NikonCompressionCodec.LossyTableQuirk,
            NikonCompressionCodec.LossyCurveQuirk,
            NikonCompressionCodec.LossyQuantizationQuirk
        };
    }

    static RawProfileEvidence Evidence(RawProfileEvidenceKind kind, string tool)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = kind;
        evidence.Tool = tool;
        evidence.Version = "1.0";
        evidence.SampleId = "generated-test-nef";
        evidence.Check = kind == RawProfileEvidenceKind.Structural ? "parse-write-parse" : kind == RawProfileEvidenceKind.ErrorBound ? "decode-maximum-absolute-error-1" : "application-open";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Passed.";
        return evidence;
    }

    internal static RawWriteOptions Options()
    {
        RawWriteOptions options = new RawWriteOptions();
        options.Mode = RawWriteMode.NewContainer;
        options.AllowExperimentalProfiles = true;
        options.VerifyPixelIdentity = true;
        return options;
    }

    internal static RawNefWriteInput Input(int width, int height, int stride)
    {
        RawNefWriteInput input = new RawNefWriteInput();
        input.Width = width;
        input.Height = height;
        input.RowStrideSamples = stride;
        input.StoredSamples = new ushort[stride * height];
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                input.StoredSamples[y * stride + x] = (ushort)(100 + y * width + x);
                x++;
            }
            y++;
        }
        input.Preview = Preview();
        input.Metadata.Software = "raw-tests";
        input.Metadata.Artist = "Test Creator";
        input.Metadata.Copyright = "Test Rights";
        input.Metadata.OriginalTime = new RawExifTimestamp("2026:08:20 12:34:56", "-07:00", "123");
        input.Metadata.WhiteBalance = "Auto";
        input.Metadata.IsoSensitivity = 400;
        return input;
    }

    internal static RawNefWriteInput ExtremeInput(int width, int height, int stride)
    {
        RawNefWriteInput input = Input(width, height, stride);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int pattern = (y * width + x) & 3;
                input.StoredSamples[y * stride + x] = pattern == 0 ? (ushort)0 : pattern == 1 ? (ushort)16383 : pattern == 2 ? (ushort)1 : (ushort)16382;
                x++;
            }
            y++;
        }
        return input;
    }

    static RawThumbnail Preview()
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = 8;
        image.Height = 8;
        image.Precision = 8;
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = 8;
        plane.Height = 8;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = new byte[64];
        int index = 0;
        while (index < plane.Samples.Length)
        {
            plane.Samples[index] = (byte)(31 + index % 7);
            index++;
        }
        image.Planes.Add(plane);
        byte[] encoded = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
        return new RawThumbnail(0, RawThumbnailKind.Jpeg, 8, 8, RawOrientation.Normal, "image/jpeg", encoded, null, null);
    }

    internal static ushort[] DecodeRows(byte[] raw, int width, int height)
    {
        int rowBytes = BitPacking.GetNamedPackedByteCount(width, RawPacking.Raw14);
        uint[] row = new uint[width];
        ushort[] result = new ushort[width * height];
        int y = 0;
        while (y < height)
        {
            BitPacking.UnpackNamed(raw.AsSpan(y * rowBytes, rowBytes), width, RawPacking.Raw14, row);
            int x = 0;
            while (x < width)
            {
                result[y * width + x] = (ushort)row[x];
                x++;
            }
            y++;
        }
        return result;
    }

    internal static ushort[] DecodeLossless(byte[] raw, int width, int height, NikonCompressionInfo compression)
    {
        ushort[] result = new ushort[width * height];
        MsbBitReader reader = new MsbBitReader(new BoundedInput(raw, 0, raw.Length, new RawLimits()));
        NikonCompressionCodec.DecodeSamples(reader, result, width, height, compression, new RawDecodeOptions());
        reader.AlignToByteWithExpectedPadding(0);
        reader.RequireEnd();
        return result;
    }

    internal static ushort[] ContiguousSamples(RawNefWriteInput input)
    {
        ushort[] result = new ushort[input.Width * input.Height];
        int y = 0;
        while (y < input.Height)
        {
            Array.Copy(input.StoredSamples, y * input.RowStrideSamples, result, y * input.Width, input.Width);
            y++;
        }
        return result;
    }
}
