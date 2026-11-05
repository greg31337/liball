using raw;

namespace raw.Tests;

public class RawBuiltInProfileCatalogueTests
{
    [Test]
    public void FactoryLoadsReviewedReadProfilesAndExperimentalWriteProfilesAsBuiltIn()
    {
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(7));
            Assert.That(catalogue.WriteProfiles, Has.Length.EqualTo(3));
            Assert.That(catalogue.ReadProfileSources, Is.All.EqualTo(RawProfileRegistrationSource.BuiltIn));
            Assert.That(catalogue.WriteProfileSources, Is.All.EqualTo(RawProfileRegistrationSource.BuiltIn));
            Assert.That(catalogue.Diagnostics, Has.Length.EqualTo(3));
            Assert.That(catalogue.Diagnostics, Is.All.Matches<RawDiagnostic>(item => item.Code == RawDiagnosticCode.AmbiguousProfile && item.Severity == RawSeverity.Warning && item.Recovered));
        });
    }

    [Test]
    public void NikonZ8ReadProfileIsExactExperimentalAndFullyProvenanced()
    {
        RawReadProfile? profile = (RawProfileCatalogue.CreateBuiltIn(new RawLimits()).ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == NikonNefMetadataParser.Z8LosslessProfile);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Identity.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(profile.Identity.Model, Is.EqualTo("NIKON Z 8"));
            Assert.That(profile.Fingerprint.Width, Is.EqualTo(8280));
            Assert.That(profile.Fingerprint.Height, Is.EqualTo(5520));
            Assert.That(profile.Fingerprint.Compression, Is.EqualTo(34713));
            Assert.That(profile.Fingerprint.MakerNoteSignature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That((profile.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, Is.EqualTo(8256));
            Assert.That(profile.Geometry.ActiveArea.Height, Is.EqualTo(5504));
            Assert.That((profile.Geometry.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That(profile.Codec.StoredWordBits, Is.EqualTo(14));
            Assert.That(profile.Experimental, Is.True);
            Assert.That(profile.Provenance, Has.Length.EqualTo(42));
            Assert.That(profile.Provenance.Select(item => item.Field).Distinct().Count(), Is.EqualTo(profile.Provenance.Length));
            Assert.That(profile.Provenance.All(item => item.Source.Length > 0 && (item.License ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length > 0 && item.Notes.Length > 0), Is.True);
        });
    }

    [Test]
    public void ConservativeCr2WriteProfileDefinesContainerCodecPreviewMakerNoteAndEvidenceRules()
    {
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawWriteProfile? profile = (catalogue.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Identity.Format == RawFormat.Cr2);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Id, Is.EqualTo(RawBuiltInProfileCatalogue.Eos6DMarkIiWriteProfile));
            Assert.That(profile.ReadProfileId, Is.EqualTo(CanonCr2MetadataParser.Eos6DMarkIiProfile));
            Assert.That(profile.Identity.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(profile.Identity.Make, Is.EqualTo("Canon"));
            Assert.That(profile.Identity.Model, Is.EqualTo("Canon EOS 6D Mark II"));
            Assert.That(profile.Identity.CameraId, Is.EqualTo("2147484678"));
            Assert.That(profile.Identity.FirmwareMinimum, Is.EqualTo("1.0.2"));
            Assert.That(profile.Identity.FirmwareMaximum, Is.EqualTo("1.0.2"));
            Assert.That(profile.Fingerprint.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(profile.Fingerprint.RequiredIfds, Is.EqualTo(new string[] { "IFD0", "IFD1", "IFD2", "IFD3", "IFD0/Exif" }));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/271"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/272"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD1/513"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD1/514"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/Exif/37500"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD3/50752"));
            Assert.That(profile.Fingerprint.MakerNoteSignature, Is.EqualTo("Canon"));
            Assert.That(profile.Geometry.StoredWidth, Is.EqualTo(6384));
            Assert.That(profile.Geometry.StoredHeight, Is.EqualTo(4224));
            Assert.That((profile.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, Is.EqualTo(6240));
            Assert.That(profile.Geometry.ActiveArea.Height, Is.EqualTo(4160));
            Assert.That((profile.Geometry.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That(profile.Codec.Name, Is.EqualTo("canon-cr2-lossless-jpeg-sof3-write"));
            Assert.That(profile.Codec.StoredWordBits, Is.EqualTo(14));
            Assert.That(profile.Codec.Predictor, Is.EqualTo(1));
            Assert.That(profile.Codec.SliceWidths, Is.EqualTo(new int[] { 6384 }));
            Assert.That(profile.Codec.Quirks, Does.Contain("sof3-four-components-3192x2112"));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2HeaderVersionQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2IfdChainQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2Ifd0PreviewQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2Ifd1JpegThumbnailQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2Ifd2PreviewQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2JpegPointTransformQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2JpegRestartQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2JpegInterleavedQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.Cr2JpegOptimizedHuffmanQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.CanonMakerNoteRequiredQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.CanonMakerNoteReviewedTagsQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.CanonMakerNoteRelocationQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.CanonMakerNoteUnknownTagsQuirk));
            Assert.That(profile.Evidence, Has.Length.EqualTo(2));
            Assert.That(profile.Evidence[0].Kind, Is.EqualTo(RawProfileEvidenceKind.Structural));
            Assert.That(profile.Evidence[0].Result, Is.EqualTo(RawProfileEvidenceResult.Passed));
            Assert.That(profile.Evidence[0].Check, Is.EqualTo("new-lossless-parse-write-parse"));
            Assert.That(profile.Evidence[1].Kind, Is.EqualTo(RawProfileEvidenceKind.PixelIdentity));
            Assert.That(profile.Evidence[1].Result, Is.EqualTo(RawProfileEvidenceResult.Passed));
            Assert.That(profile.Evidence[1].Check, Is.EqualTo("decode-compare-stored-pixel-identity"));
            Assert.That(profile.Experimental, Is.True);
            Assert.That(profile.Provenance.Select(item => item.Field).Distinct().Count(), Is.EqualTo(profile.Provenance.Length));
            Assert.That(profile.Provenance.Any(item => item.Field == "readProfileId"), Is.True);
            Assert.That(profile.Provenance.Any(item => item.Field == "evidence"), Is.True);
            Assert.That(profile.Provenance.All(item => item.Notes.Contains(RawBuiltInProfileCatalogue.SampleHash)), Is.True);
            Assert.That(RawBuiltInProfileCatalogue.CreateDocument().Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ConservativeNefWriteProfileDefinesContainerCodecPreviewMakerNoteAndEvidenceRules()
    {
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawWriteProfile? profile = (catalogue.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == RawBuiltInProfileCatalogue.NikonZ8WriteProfile);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Id, Is.EqualTo(RawBuiltInProfileCatalogue.NikonZ8WriteProfile));
            Assert.That(profile.ReadProfileId, Is.EqualTo(NikonNefMetadataParser.Z8LosslessProfile));
            Assert.That(profile.Identity.Make, Is.EqualTo("NIKON CORPORATION"));
            Assert.That(profile.Identity.Model, Is.EqualTo("NIKON Z 8"));
            Assert.That(profile.Identity.Aliases, Is.EqualTo(new string[] { "Z 8" }));
            Assert.That(profile.Fingerprint.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(profile.Fingerprint.BigTiff, Is.False);
            Assert.That(profile.Fingerprint.RequiredIfds, Is.EqualTo(new string[] { "IFD0", "IFD0/Exif", "IFD0/SubIFD0", "IFD0/SubIFD1" }));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/271"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/272"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/Exif/37500"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/SubIFD0/278"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/SubIFD1/273"));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD0/SubIFD1/279"));
            Assert.That(profile.Fingerprint.MakerNoteSignature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That(profile.Geometry.StoredWidth, Is.EqualTo(8280));
            Assert.That(profile.Geometry.StoredHeight, Is.EqualTo(5520));
            Assert.That((profile.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(12));
            Assert.That(profile.Geometry.ActiveArea.Y, Is.EqualTo(8));
            Assert.That(profile.Geometry.ActiveArea.Width, Is.EqualTo(8256));
            Assert.That(profile.Geometry.ActiveArea.Height, Is.EqualTo(5504));
            Assert.That((profile.Geometry.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Layout, Is.EqualTo(RawCfaLayout.Rggb));
            Assert.That(profile.Codec.Name, Is.EqualTo(NikonCompressionCodec.LosslessCodecName));
            Assert.That(profile.Codec.Compression, Is.EqualTo(34713));
            Assert.That(profile.Codec.StoredWordBits, Is.EqualTo(14));
            Assert.That(profile.Codec.SliceWidths, Is.EqualTo(new int[] { 8280 }));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefIfdTopologyQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefSingleRawStripQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefLosslessCompressionInfoQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefLosslessHuffmanTableQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefLosslessPredictorQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefLosslessCurveQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NefJpegPreviewQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NikonMakerNoteRequiredQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NikonMakerNoteEmbeddedTiffBaseQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NikonMakerNoteReviewedTagsQuirk));
            Assert.That(profile.Codec.Quirks, Does.Contain(RawBuiltInProfileCatalogue.NikonMakerNoteUnknownTagsQuirk));
            Assert.That(profile.Evidence, Has.Length.EqualTo(2));
            Assert.That(profile.Evidence[0].Kind, Is.EqualTo(RawProfileEvidenceKind.Structural));
            Assert.That(profile.Evidence[0].Result, Is.EqualTo(RawProfileEvidenceResult.Passed));
            Assert.That(profile.Evidence[0].Check, Is.EqualTo("new-lossless-parse-write-parse"));
            Assert.That(profile.Evidence[1].Kind, Is.EqualTo(RawProfileEvidenceKind.PixelIdentity));
            Assert.That(profile.Evidence[1].Check, Is.EqualTo("decode-compare-stored-pixel-identity"));
            Assert.That(profile.Experimental, Is.True);
            Assert.That(profile.Provenance.Select(item => item.Field).Distinct().Count(), Is.EqualTo(profile.Provenance.Length));
            Assert.That(profile.Provenance.Any(item => item.Field == "readProfileId"), Is.True);
            Assert.That(profile.Provenance.Any(item => item.Field == "evidence"), Is.True);
            Assert.That(RawBuiltInProfileCatalogue.CreateDocument().Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ExperimentalNefWriteProfileRequiresOptInAndBuiltInFactoryReturnsIndependentCopies()
    {
        RawProfileCatalogue first = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawWriteProfile? profile = (first.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == RawBuiltInProfileCatalogue.NikonZ8WriteProfile);
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = RawFormat.Nef;
        input.Make = "NIKON CORPORATION";
        input.Model = "Z 8";
        input.HasByteOrder = true;
        input.ByteOrder = profile.Fingerprint.ByteOrder;
        input.HasBigTiff = true;
        input.BigTiff = profile.Fingerprint.BigTiff;
        input.Width = profile.Fingerprint.Width;
        input.Height = profile.Fingerprint.Height;
        input.BitDepth = profile.Fingerprint.BitDepth;
        input.Compression = profile.Fingerprint.Compression;
        input.SamplesPerPixel = profile.Fingerprint.SamplesPerPixel;
        input.IfdPaths = profile.Fingerprint.RequiredIfds;
        input.Tags = profile.Fingerprint.RequiredTags;
        input.MakerNoteSignature = profile.Fingerprint.MakerNoteSignature;

        RawProfileMatchResult blocked = first.MatchWriteProfile(input);
        input.AllowExperimental = true;
        RawProfileMatchResult enabled = first.MatchWriteProfile(input);
        profile.Codec.SliceWidths[0] = 1;
        RawProfileCatalogue second = RawProfileCatalogue.CreateBuiltIn(new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Matched, Is.False);
            Assert.That(blocked.WriteProfile, Is.Null);
            Assert.That(blocked.Candidates.Single().Eligible, Is.False);
            Assert.That(blocked.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That(enabled.Matched, Is.True);
            Assert.That((enabled.WriteProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo(RawBuiltInProfileCatalogue.NikonZ8WriteProfile));
            Assert.That(((second.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == RawBuiltInProfileCatalogue.NikonZ8WriteProfile).Codec.SliceWidths ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new int[] { 8280 }));
        });
    }

    [Test]
    public void ExperimentalCr2WriteProfileRequiresOptInAndBuiltInFactoryReturnsIndependentCopies()
    {
        RawProfileCatalogue first = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawWriteProfile? profile = (first.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Identity.Format == RawFormat.Cr2);
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = profile.Identity.Format;
        input.Make = "Canon";
        input.Model = "EOS 6D Mark II";
        input.CameraId = profile.Identity.CameraId;
        input.Firmware = "1.0.2";
        input.HasByteOrder = true;
        input.ByteOrder = profile.Fingerprint.ByteOrder;
        input.HasBigTiff = true;
        input.BigTiff = profile.Fingerprint.BigTiff;
        input.Width = profile.Fingerprint.Width;
        input.Height = profile.Fingerprint.Height;
        input.BitDepth = profile.Fingerprint.BitDepth;
        input.Compression = profile.Fingerprint.Compression;
        input.IfdPaths = profile.Fingerprint.RequiredIfds;
        input.Tags = profile.Fingerprint.RequiredTags;
        input.MakerNoteSignature = profile.Fingerprint.MakerNoteSignature;

        RawProfileMatchResult blocked = first.MatchWriteProfile(input);
        input.AllowExperimental = true;
        RawProfileMatchResult enabled = first.MatchWriteProfile(input);
        profile.Codec.SliceWidths[0] = 1;
        RawProfileCatalogue second = RawProfileCatalogue.CreateBuiltIn(new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Matched, Is.False);
            Assert.That(blocked.WriteProfile, Is.Null);
            Assert.That(blocked.Candidates.Single().Eligible, Is.False);
            Assert.That(blocked.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That(enabled.Matched, Is.True);
            Assert.That((enabled.WriteProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo(RawBuiltInProfileCatalogue.Eos6DMarkIiWriteProfile));
            Assert.That(((second.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Identity.Format == RawFormat.Cr2).Codec.SliceWidths ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(new int[] { 6384 }));
        });
    }

    [Test]
    public void CanonEos6DMarkIiProfileContainsOnlyObservedConservativeValues()
    {
        RawReadProfile? profile = (RawProfileCatalogue.CreateBuiltIn(new RawLimits()).ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == CanonCr2MetadataParser.Eos6DMarkIiProfile);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Id, Is.EqualTo("canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read"));
            Assert.That(profile.Identity.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(profile.Identity.Make, Is.EqualTo("Canon"));
            Assert.That(profile.Identity.Model, Is.EqualTo("Canon EOS 6D Mark II"));
            Assert.That(profile.Identity.CameraId, Is.EqualTo("2147484678"));
            Assert.That(profile.Identity.Aliases, Is.EqualTo(new string[] { "EOS 6D Mark II" }));
            Assert.That(profile.Identity.FirmwareMinimum, Is.EqualTo("1.0.2"));
            Assert.That(profile.Identity.FirmwareMaximum, Is.EqualTo("1.0.2"));
            Assert.That(profile.Fingerprint.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(profile.Fingerprint.BigTiff, Is.False);
            Assert.That(profile.Fingerprint.Width, Is.EqualTo(6384));
            Assert.That(profile.Fingerprint.Height, Is.EqualTo(4224));
            Assert.That(profile.Fingerprint.BitDepth, Is.EqualTo(14));
            Assert.That(profile.Fingerprint.Compression, Is.EqualTo(6));
            Assert.That(profile.Fingerprint.SamplesPerPixel, Is.Zero);
            Assert.That(profile.Fingerprint.RequiredIfds, Is.EqualTo(new string[] { "IFD0", "IFD3" }));
            Assert.That(profile.Fingerprint.RequiredTags, Does.Contain("IFD3/50752"));
            Assert.That(profile.Geometry.StoredWidth, Is.EqualTo(6384));
            Assert.That(profile.Geometry.StoredHeight, Is.EqualTo(4224));
            Assert.That((profile.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(132));
            Assert.That(profile.Geometry.ActiveArea.Y, Is.EqualTo(56));
            Assert.That(profile.Geometry.ActiveArea.Width, Is.EqualTo(6240));
            Assert.That(profile.Geometry.ActiveArea.Height, Is.EqualTo(4160));
            Assert.That(profile.Geometry.MaskedAreas, Is.Empty);
            Assert.That(profile.Geometry.Cfa, Is.Null);
            Assert.That(profile.Codec.Name, Is.EqualTo("canon-cr2-lossless-jpeg-sof3"));
            Assert.That(profile.Codec.Compression, Is.EqualTo(6));
            Assert.That(profile.Codec.StoredWordBits, Is.EqualTo(14));
            Assert.That(profile.Codec.Predictor, Is.EqualTo(1));
            Assert.That(profile.Codec.SliceWidths, Is.EqualTo(new int[] { 6384 }));
            Assert.That(profile.Calibration.BlackLevels, Is.Empty);
            Assert.That(profile.Calibration.ColorMatrix, Is.Empty);
            Assert.That(profile.Experimental, Is.True);
        });
    }

    [Test]
    public void EveryBuiltInProfileFieldHasSpecificProvenance()
    {
        RawReadProfile? profile = (RawProfileCatalogue.CreateBuiltIn(new RawLimits()).ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == CanonCr2MetadataParser.Eos6DMarkIiProfile);
        string[] expectedFields = new string[]
        {
            "id",
            "identity.format",
            "identity.make",
            "identity.model",
            "identity.cameraId",
            "identity.aliases",
            "identity.firmwareMinimum",
            "identity.firmwareMaximum",
            "fingerprint.byteOrder",
            "fingerprint.bigTiff",
            "fingerprint.width",
            "fingerprint.height",
            "fingerprint.bitDepth",
            "fingerprint.compression",
            "fingerprint.samplesPerPixel",
            "fingerprint.requiredIfds",
            "fingerprint.requiredTags",
            "fingerprint.makerNoteSignature",
            "fingerprint.structuralHash",
            "geometry.storedWidth",
            "geometry.storedHeight",
            "geometry.activeArea",
            "geometry.maskedAreas",
            "geometry.cfa",
            "codec.name",
            "codec.compression",
            "codec.packing",
            "codec.storedWordBits",
            "codec.predictor",
            "codec.planarConfiguration",
            "codec.rowAlignment",
            "codec.splitRow",
            "codec.sliceWidths",
            "codec.quirks",
            "calibration.blackLevels",
            "calibration.whiteLevels",
            "calibration.whiteBalance",
            "calibration.colorMatrix",
            "calibration.forwardMatrix",
            "calibration.illuminant",
            "calibration.baselineExposure",
            "experimental"
        };

        Assert.Multiple(() =>
        {
            Assert.That(profile.Provenance.Select(item => item.Field), Is.EquivalentTo(expectedFields));
            Assert.That(profile.Provenance.Select(item => item.Field).Distinct().Count(), Is.EqualTo(profile.Provenance.Length));
            Assert.That(profile.Provenance.All(item => item.Source.Length > 0), Is.True);
            Assert.That(profile.Provenance.All(item => (item.License ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length > 0), Is.True);
            Assert.That(profile.Provenance.All(item => item.Retrieved == "2026-08-19"), Is.True);
            Assert.That(profile.Provenance.All(item => item.Notes.Length > 0), Is.True);
            Assert.That(profile.Provenance.Any(item => item.Kind == RawProfileProvenanceKind.Manufacturer), Is.True);
            Assert.That(profile.Provenance.Any(item => item.Kind == RawProfileProvenanceKind.Sample && item.License == "CC0-1.0" && item.Notes.Contains("sha256:712815b910fa473f7aaeb97c0804292d996b16dd6b974a3ab9a7195a2e41d90c")), Is.True);
        });
    }

    [Test]
    public void ExperimentalReadProfileMatchesOnlyWithExplicitOptInAndFactoryCopiesData()
    {
        RawProfileCatalogue first = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = RawFormat.Cr2;
        input.Make = "Canon";
        input.Model = "EOS 6D Mark II";
        input.CameraId = "2147484678";
        input.Firmware = "1.0.2";
        input.Width = 6384;
        input.Height = 4224;

        RawProfileMatchResult blocked = first.MatchReadProfile(input);
        input.AllowExperimental = true;
        RawProfileMatchResult enabled = first.MatchReadProfile(input);
        (first.ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == CanonCr2MetadataParser.Eos6DMarkIiProfile).Identity.Model = "changed";
        RawProfileCatalogue second = RawProfileCatalogue.CreateBuiltIn(new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Matched, Is.False);
            Assert.That(blocked.Candidates.Single().Eligible, Is.False);
            Assert.That(enabled.Matched, Is.True);
            Assert.That((enabled.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read"));
            Assert.That(((second.ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == CanonCr2MetadataParser.Eos6DMarkIiProfile).Identity.Model ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo("Canon EOS 6D Mark II"));
        });
    }

    [Test]
    public void EarlyCanonProfilesAreExactExperimentalObservedVariants()
    {
        RawReadProfile[]? profiles = RawProfileCatalogue.CreateBuiltIn(new RawLimits()).ReadProfiles;
        RawReadProfile? eos20d = (profiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == CanonCr2MetadataParser.Eos20dProfile);
        RawReadProfile eos1d = profiles.Single(item => item.Id == CanonCr2MetadataParser.Eos1dProfile);

        Assert.Multiple(() =>
        {
            Assert.That(eos20d.Identity.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(eos20d.Identity.FirmwareMinimum, Is.EqualTo("2.0.3"));
            Assert.That(eos20d.Geometry.StoredWidth, Is.EqualTo(3596));
            Assert.That(eos20d.Geometry.StoredHeight, Is.EqualTo(2360));
            Assert.That(eos20d.Codec.Quirks, Does.Contain(CanonEarlyRawLayout.DimensionlessIfdQuirk));
            Assert.That(eos20d.Experimental, Is.True);
            Assert.That(eos20d.Provenance.All(item => item.License == "CC0-1.0" && item.Notes.Contains(RawBuiltInProfileCatalogue.Eos20dSampleHash)), Is.True);
            Assert.That(eos1d.Identity.Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(eos1d.Identity.FirmwareMinimum, Is.EqualTo("1.4.0"));
            Assert.That(eos1d.Geometry.StoredWidth, Is.EqualTo(2496));
            Assert.That(eos1d.Geometry.StoredHeight, Is.EqualTo(1662));
            Assert.That(eos1d.Codec.Quirks, Does.Contain(CanonEarlyRawLayout.RawOffsetQuirk));
            Assert.That(eos1d.Codec.Quirks, Does.Contain(CanonEarlyRawLayout.HalfHeightDoubleWidthQuirk));
            Assert.That(eos1d.Experimental, Is.True);
            Assert.That(eos1d.Provenance.All(item => item.License == "CC0-1.0" && item.Notes.Contains(RawBuiltInProfileCatalogue.Eos1dSampleHash)), Is.True);
        });
    }

    [Test]
    public void CanonSubsampledProfilesSeparateMRawAndSRawByObservedLayout()
    {
        RawReadProfile[]? profiles = RawProfileCatalogue.CreateBuiltIn(new RawLimits()).ReadProfiles;
        RawReadProfile? mraw = (profiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Id == CanonCr2MetadataParser.Eos6DMarkIiMRawProfile);
        RawReadProfile sraw = profiles.Single(item => item.Id == CanonCr2MetadataParser.Eos6DMarkIiSRawProfile);

        Assert.Multiple(() =>
        {
            Assert.That(mraw.Fingerprint.Width, Is.EqualTo(4680));
            Assert.That(mraw.Fingerprint.Height, Is.EqualTo(3132));
            Assert.That(mraw.Fingerprint.RequiredTags, Does.Contain("IFD3/50885"));
            Assert.That(mraw.Codec.SliceWidths, Is.EqualTo(new int[] { 2808, 2808, 2808, 2808, 2808 }));
            Assert.That(mraw.Codec.Quirks, Does.Contain(CanonSRawDecoder.Layout420Quirk));
            Assert.That(mraw.Codec.Quirks, Does.Contain(CanonSRawDecoder.Generation2Quirk));
            Assert.That(mraw.Codec.Quirks, Does.Contain(CanonSRawDecoder.NewHueQuirk));
            Assert.That(mraw.Provenance.All(item => item.Notes.Contains(RawBuiltInProfileCatalogue.Eos6DMarkIiMRawSampleHash)), Is.True);
            Assert.That(sraw.Fingerprint.Width, Is.EqualTo(3120));
            Assert.That(sraw.Fingerprint.Height, Is.EqualTo(2082));
            Assert.That(sraw.Codec.SliceWidths, Is.EqualTo(new int[] { 1560, 1560, 1560, 1560 }));
            Assert.That(sraw.Codec.Quirks, Does.Contain(CanonSRawDecoder.Layout422Quirk));
            Assert.That(sraw.Codec.Quirks, Does.Contain(CanonSRawDecoder.InvertWhiteBalanceQuirk));
            Assert.That(sraw.Provenance.All(item => item.Notes.Contains(RawBuiltInProfileCatalogue.Eos6DMarkIiSRawSampleHash)), Is.True);
        });
    }
}
