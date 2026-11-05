namespace raw;

internal static class RawBuiltInProfileCatalogue
{
    internal const string CanonSpecification = "https://www.canon-europe.com/media/EOS_6D_Mark_II_PR_Spec_Sheet_EM_Final_tcm13-1580718.pdf";
    internal const string CanonSpecificationLicense = "Canon copyright; factual specification data independently transcribed";
    internal const string Sample = "https://raw.pixls.us/getfile.php/1625/nice/Canon%20-%20EOS%206D%20Mark%20II%20-%20RAW%20%283%3A2%29.CR2";
    internal const string SampleLicense = "CC0-1.0";
    internal const string Retrieved = "2026-08-19";
    internal const string SampleHash = "712815b910fa473f7aaeb97c0804292d996b16dd6b974a3ab9a7195a2e41d90c";
    internal const string Eos20dSample = "https://raw.pixls.us/getfile.php/1018/nice/Canon%20-%20EOS%2020D%20-%20RAW%20%283%3A2%29.CR2";
    internal const string Eos20dSampleHash = "db78660d48589130d99e0bc621424851eb0f84df052634fb5bc89c7a02fe7033";
    internal const string Eos1dSample = "https://raw.pixls.us/getfile.php/1958/nice/Canon%20-%20EOS-1D%20-%20RAW%20%283%3A2%29.TIF";
    internal const string Eos1dSampleHash = "e9856cb62ca32d6cbe83dbb35f16fe10b5d7d0ef92e84bdcb40be70aabe87e6a";
    internal const string Eos6DMarkIiSRawSample = "https://raw.pixls.us/getfile.php/1623/nice/Canon%20-%20EOS%206D%20Mark%20II%20-%20sRAW2%20%28sRAW%29%20%283%3A2%29.CR2";
    internal const string Eos6DMarkIiSRawSampleHash = "a4bf9842c2222b2eeb95ec71065f9d981c9b5be619b96db7f121e395968a119b";
    internal const string Eos6DMarkIiMRawSample = "https://raw.pixls.us/getfile.php/1624/nice/Canon%20-%20EOS%206D%20Mark%20II%20-%20sRAW1%20%28mRAW%29%20%283%3A2%29.CR2";
    internal const string Eos6DMarkIiMRawSampleHash = "c34f6dc459901b44a837cf30eacee19fef548ce5a66c4611d68ed3fae8997498";
    internal const string Eos6DMarkIiWriteProfile = "canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.write.experimental";
    internal const string Cr2HeaderVersionQuirk = "cr2-header-version-2.0";
    internal const string Cr2IfdChainQuirk = "cr2-root-ifd-chain-ifd0-ifd1-ifd2-ifd3";
    internal const string Cr2Ifd0PreviewQuirk = "require-ifd0-preview";
    internal const string Cr2Ifd1JpegThumbnailQuirk = "require-ifd1-jpeg-thumbnail";
    internal const string Cr2Ifd2PreviewQuirk = "require-ifd2-preview";
    internal const string Cr2JpegPointTransformQuirk = "lossless-jpeg-point-transform-0";
    internal const string Cr2JpegRestartQuirk = "lossless-jpeg-restart-interval-0";
    internal const string Cr2JpegInterleavedQuirk = "lossless-jpeg-interleaved-scan";
    internal const string Cr2JpegOptimizedHuffmanQuirk = "lossless-jpeg-optimized-huffman";
    internal const string CanonMakerNoteRequiredQuirk = "canon-maker-note-required";
    internal const string CanonMakerNoteReviewedTagsQuirk = "canon-maker-note-reviewed-tags-only";
    internal const string CanonMakerNoteRelocationQuirk = "canon-maker-note-fixed-anchor-or-proven-relocation";
    internal const string CanonMakerNoteUnknownTagsQuirk = "preserve-unknown-maker-note-tags";
    internal const string NikonZ8Specification = "https://onlinemanual.nikonimglib.com/z8/en/specifications_378.html";
    internal const string NikonZ7Specification = "https://onlinemanual.nikonimglib.com/z7_z6/en/14_technical_notes_06.html";
    internal const string NikonSpecificationLicense = "Nikon copyright; factual specification data independently transcribed";
    internal const string NikonSampleCatalogue = "https://raw.pixls.us/";
    internal const string NikonSampleMetadataLicense = "Sample-specific factual metadata; redistribution is not asserted by this profile";
    internal const string NikonZ8WriteProfile = "nikon.z-8.nef.lossless-14bit.fx.write.experimental";
    internal const string NikonZ7UncompressedWriteProfile = "nikon.z-7.nef.uncompressed-14bit.fx.write.experimental";
    internal const string NefIfdTopologyQuirk = "nef-ifd0-exif-raw-subifd0-preview-subifd1";
    internal const string NefSingleRawStripQuirk = "nef-raw-single-strip-8280x5520-no-tiles";
    internal const string NefZ7SingleRawStripQuirk = "nef-raw-single-strip-8256x5504-no-tiles";
    internal const string NefLosslessCompressionInfoQuirk = "nikon-compression-info-0x0096-version-0x46";
    internal const string NefLosslessHuffmanTableQuirk = "nikon-lossless-14bit-huffman-table-5";
    internal const string NefLosslessPredictorQuirk = "nikon-lossless-four-row-parity-predictors";
    internal const string NefLosslessCurveQuirk = "nikon-lossless-identity-curve-16384";
    internal const string NefJpegPreviewQuirk = "require-subifd1-baseline-jpeg-preview";
    internal const string NikonMakerNoteRequiredQuirk = "nikon-maker-note-required";
    internal const string NikonMakerNoteEmbeddedTiffBaseQuirk = "nikon-maker-note-embedded-tiff-header-offset-base";
    internal const string NikonMakerNoteReviewedTagsQuirk = "nikon-maker-note-reviewed-tags-only";
    internal const string NikonMakerNoteUnknownTagsQuirk = "preserve-unknown-nikon-maker-note-tags";

    internal static byte[] CreateJson(RawLimits limits)
    {
        return RawProfileSchemaCodec.Write(CreateDocument(), limits);
    }

    internal static RawProfileSchemaDocument CreateDocument()
    {
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { CreateCanonEos6DMarkIi(), CreateCanonEos6DMarkIiMRaw(), CreateCanonEos6DMarkIiSRaw(), CreateCanonEos20d(), CreateCanonEos1d(), CreateNikonZ7Uncompressed(), CreateNikonZ8Lossless() };
        document.WriteProfiles = new RawWriteProfile[] { CreateCanonEos6DMarkIiWrite(), CreateNikonZ7UncompressedWrite(), CreateNikonZ8LosslessWrite() };
        return document;
    }

    internal static RawReadProfile CreateNikonZ7Uncompressed()
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = NikonNefMetadataParser.Z7UncompressedProfile;
        profile.Identity.Format = RawFormat.Nef;
        profile.Identity.Make = "NIKON CORPORATION";
        profile.Identity.Model = "NIKON Z 7";
        profile.Identity.Aliases = new string[] { "Z 7", "Nikon Z 7" };
        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = 8256;
        profile.Fingerprint.Height = 5504;
        profile.Fingerprint.BitDepth = 14;
        profile.Fingerprint.Compression = 1;
        profile.Fingerprint.SamplesPerPixel = 1;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD0/Exif", "IFD0/SubIFD0", "IFD0/SubIFD1" };
        profile.Fingerprint.RequiredTags = NikonZ7RequiredTags();
        profile.Fingerprint.MakerNoteSignature = "NikonEmbeddedTiff";
        profile.Geometry.StoredWidth = 8256;
        profile.Geometry.StoredHeight = 5504;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8256, 5504);
        profile.Geometry.MaskedAreas = System.Array.Empty<RawArea>();
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        profile.Codec.Name = NefUncompressedDecoder.CodecName;
        profile.Codec.Compression = 1;
        profile.Codec.Packing = RawPacking.Raw14;
        profile.Codec.StoredWordBits = 14;
        profile.Codec.Predictor = 0;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = 1;
        profile.Codec.SplitRow = -1;
        profile.Codec.SliceWidths = new int[] { 8256 };
        profile.Codec.Quirks = new string[]
        {
            "nikon-nefcompression-maker-note-10",
            NefIfdTopologyQuirk,
            NefZ7SingleRawStripQuirk,
            NefJpegPreviewQuirk,
            NikonMakerNoteRequiredQuirk,
            NikonMakerNoteEmbeddedTiffBaseQuirk
        };
        profile.Experimental = true;
        profile.Provenance = CreateNikonZ7Provenance();
        return profile;
    }

    internal static RawWriteProfile CreateNikonZ7UncompressedWrite()
    {
        RawReadProfile read = CreateNikonZ7Uncompressed();
        RawWriteProfile profile = new RawWriteProfile();
        profile.Id = NikonZ7UncompressedWriteProfile;
        profile.ReadProfileId = read.Id;
        profile.Identity = read.Identity;
        profile.Fingerprint = read.Fingerprint;
        profile.Geometry = read.Geometry;
        profile.Codec = read.Codec;
        profile.Calibration = read.Calibration;
        profile.Provenance = CreateNikonZ7WriteProvenance();
        profile.Evidence = new RawProfileEvidence[] { CreateNikonZ7WriteEvidence(), CreateNikonZ7PixelEvidence() };
        profile.Experimental = true;
        return profile;
    }

    internal static string[] NikonZ7RequiredTags()
    {
        return new string[]
        {
            "IFD0/271", "IFD0/272", "IFD0/305", "IFD0/330", "IFD0/34665",
            "IFD0/Exif/36864", "IFD0/Exif/36867", "IFD0/Exif/37500", "IFD0/Exif/40962", "IFD0/Exif/40963",
            "IFD0/SubIFD0/254", "IFD0/SubIFD0/256", "IFD0/SubIFD0/257", "IFD0/SubIFD0/258", "IFD0/SubIFD0/259", "IFD0/SubIFD0/262",
            "IFD0/SubIFD0/273", "IFD0/SubIFD0/277", "IFD0/SubIFD0/278", "IFD0/SubIFD0/279", "IFD0/SubIFD0/284",
            "IFD0/SubIFD0/33421", "IFD0/SubIFD0/33422", "IFD0/SubIFD0/50829",
            "IFD0/SubIFD1/254", "IFD0/SubIFD1/256", "IFD0/SubIFD1/257", "IFD0/SubIFD1/259", "IFD0/SubIFD1/262", "IFD0/SubIFD1/273", "IFD0/SubIFD1/277", "IFD0/SubIFD1/278", "IFD0/SubIFD1/279"
        };
    }

    internal static RawProfileProvenance[] CreateNikonZ7Provenance()
    {
        string[] fields = new string[]
        {
            "id", "identity.format", "identity.make", "identity.model", "identity.cameraId", "identity.aliases", "identity.firmwareMinimum", "identity.firmwareMaximum",
            "fingerprint.byteOrder", "fingerprint.bigTiff", "fingerprint.width", "fingerprint.height", "fingerprint.bitDepth", "fingerprint.compression", "fingerprint.samplesPerPixel",
            "fingerprint.requiredIfds", "fingerprint.requiredTags", "fingerprint.makerNoteSignature", "fingerprint.structuralHash", "geometry.storedWidth", "geometry.storedHeight",
            "geometry.activeArea", "geometry.maskedAreas", "geometry.cfa", "codec.name", "codec.compression", "codec.packing", "codec.storedWordBits", "codec.predictor",
            "codec.planarConfiguration", "codec.rowAlignment", "codec.splitRow", "codec.sliceWidths", "codec.quirks", "calibration.blackLevels", "calibration.whiteLevels",
            "calibration.whiteBalance", "calibration.colorMatrix", "calibration.forwardMatrix", "calibration.illuminant", "calibration.baselineExposure", "experimental"
        };
        RawProfileProvenance[] result = new RawProfileProvenance[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            RawProfileProvenance item = new RawProfileProvenance();
            item.Field = fields[index];
            item.Source = NikonZ7Specification;
            item.Kind = RawProfileProvenanceKind.Manufacturer;
            item.License = NikonSpecificationLicense;
            item.Retrieved = "2026-08-20";
            item.Notes = "Nikon confirms Z 7 large FX dimensions, 14-bit NEF, and uncompressed recording; the conservative TIFF, preview, and maker-note layout remains experimental until generated output receives independent application testing.";
            result[index] = item;
            index++;
        }
        return result;
    }

    internal static RawProfileProvenance[] CreateNikonZ7WriteProvenance()
    {
        RawProfileProvenance[] observed = CreateNikonZ7Provenance();
        System.Collections.Generic.List<RawProfileProvenance> records = new System.Collections.Generic.List<RawProfileProvenance>(observed);
        Add(records, "readProfileId", NikonZ7Specification, RawProfileProvenanceKind.Manufacturer, NikonSpecificationLicense, "The writer reopens through the exact Z 7 large-FX uncompressed 14-bit profile.");
        Add(records, "evidence", NikonZ7Specification, RawProfileProvenanceKind.Manufacturer, NikonSpecificationLicense, "Structural and pixel validation are required after every write; external-application compatibility remains unconfirmed.");
        return records.ToArray();
    }

    internal static RawProfileEvidence CreateNikonZ7WriteEvidence()
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Structural;
        evidence.Tool = "raw NEF writer validator";
        evidence.Version = "schema-1";
        evidence.SampleId = NikonNefMetadataParser.Z7UncompressedProfile;
        evidence.Check = "new-uncompressed-parse-write-parse";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "The writer reopens its TIFF graph, Exif, Nikon type-3 maker note, preview, and packed raw strip. External-application compatibility remains unconfirmed, so the profile remains experimental.";
        return evidence;
    }

    internal static RawProfileEvidence CreateNikonZ7PixelEvidence()
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.PixelIdentity;
        evidence.Tool = "raw NEF pixel verifier";
        evidence.Version = "decoder-1";
        evidence.SampleId = NikonNefMetadataParser.Z7UncompressedProfile;
        evidence.Check = "decode-compare-stored-pixel-identity";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Every decoded stored sample is compared with its input after output reopen and must be identical.";
        return evidence;
    }

    internal static RawReadProfile CreateNikonZ8Lossless()
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = NikonNefMetadataParser.Z8LosslessProfile;
        profile.Identity.Format = RawFormat.Nef;
        profile.Identity.Make = "NIKON CORPORATION";
        profile.Identity.Model = "NIKON Z 8";
        profile.Identity.Aliases = new string[] { "Z 8" };
        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = 8280;
        profile.Fingerprint.Height = 5520;
        profile.Fingerprint.BitDepth = 14;
        profile.Fingerprint.Compression = 34713;
        profile.Fingerprint.SamplesPerPixel = 1;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD0/Exif", "IFD0/SubIFD0" };
        profile.Fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD0/272", "IFD0/330", "IFD0/34665", "IFD0/Exif/37500", "IFD0/SubIFD0/256", "IFD0/SubIFD0/257", "IFD0/SubIFD0/258", "IFD0/SubIFD0/259", "IFD0/SubIFD0/262", "IFD0/SubIFD0/273", "IFD0/SubIFD0/279", "IFD0/SubIFD0/33421", "IFD0/SubIFD0/33422" };
        profile.Fingerprint.MakerNoteSignature = "NikonEmbeddedTiff";
        profile.Geometry.StoredWidth = 8280;
        profile.Geometry.StoredHeight = 5520;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 12, 8, 8256, 5504);
        profile.Geometry.MaskedAreas = System.Array.Empty<RawArea>();
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        profile.Codec.Name = "nikon-nef-tiff-34713-lossless";
        profile.Codec.Compression = 34713;
        profile.Codec.Packing = RawPacking.PackedBits;
        profile.Codec.StoredWordBits = 14;
        profile.Codec.Predictor = 0;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = 1;
        profile.Codec.SplitRow = -1;
        profile.Codec.Quirks = new string[] { "nikon-nefcompression-maker-note-3" };
        profile.Experimental = true;
        profile.Provenance = CreateNikonZ8Provenance();
        return profile;
    }

    internal static RawProfileProvenance[] CreateNikonZ8Provenance()
    {
        string[] fields = new string[]
        {
            "id", "identity.format", "identity.make", "identity.model", "identity.cameraId", "identity.aliases", "identity.firmwareMinimum", "identity.firmwareMaximum",
            "fingerprint.byteOrder", "fingerprint.bigTiff", "fingerprint.width", "fingerprint.height", "fingerprint.bitDepth", "fingerprint.compression", "fingerprint.samplesPerPixel",
            "fingerprint.requiredIfds", "fingerprint.requiredTags", "fingerprint.makerNoteSignature", "fingerprint.structuralHash", "geometry.storedWidth", "geometry.storedHeight",
            "geometry.activeArea", "geometry.maskedAreas", "geometry.cfa", "codec.name", "codec.compression", "codec.packing", "codec.storedWordBits", "codec.predictor",
            "codec.planarConfiguration", "codec.rowAlignment", "codec.splitRow", "codec.sliceWidths", "codec.quirks", "calibration.blackLevels", "calibration.whiteLevels",
            "calibration.whiteBalance", "calibration.colorMatrix", "calibration.forwardMatrix", "calibration.illuminant", "calibration.baselineExposure", "experimental"
        };
        RawProfileProvenance[] result = new RawProfileProvenance[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            string field = fields[index];
            bool manufacturer = field == "identity.format" || field == "identity.make" || field == "identity.model" || field == "identity.aliases" || field == "fingerprint.bitDepth" || field == "geometry.activeArea" || field == "codec.storedWordBits" || field == "experimental";
            RawProfileProvenance item = new RawProfileProvenance();
            item.Field = field;
            item.Source = manufacturer ? NikonZ8Specification : NikonSampleCatalogue;
            item.Kind = manufacturer ? RawProfileProvenanceKind.Manufacturer : RawProfileProvenanceKind.Sample;
            item.License = manufacturer ? NikonSpecificationLicense : NikonSampleMetadataLicense;
            item.Retrieved = "2026-08-20";
            item.Notes = manufacturer ? "Nikon publishes the Z 8 identity, 8256 by 5504 FX output size, 14-bit NEF recording, and lossless-compression option; unsupported values remain neutral." : "Observed Z 8 lossless NEF structure supplies the exact classic-TIFF, 8280 by 5520 stored-grid, CFA, compression, and maker-note fingerprint; unsupported calibration values remain empty.";
            result[index] = item;
            index++;
        }
        return result;
    }

    internal static RawWriteProfile CreateNikonZ8LosslessWrite()
    {
        RawWriteProfile profile = new RawWriteProfile();
        profile.Id = NikonZ8WriteProfile;
        profile.ReadProfileId = NikonNefMetadataParser.Z8LosslessProfile;

        profile.Identity.Format = RawFormat.Nef;
        profile.Identity.Make = "NIKON CORPORATION";
        profile.Identity.Model = "NIKON Z 8";
        profile.Identity.Aliases = new string[] { "Z 8" };

        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = 8280;
        profile.Fingerprint.Height = 5520;
        profile.Fingerprint.BitDepth = 14;
        profile.Fingerprint.Compression = 34713;
        profile.Fingerprint.SamplesPerPixel = 1;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD0/Exif", "IFD0/SubIFD0", "IFD0/SubIFD1" };
        profile.Fingerprint.RequiredTags = new string[]
        {
            "IFD0/271", "IFD0/272", "IFD0/330", "IFD0/34665",
            "IFD0/Exif/37500",
            "IFD0/SubIFD0/256", "IFD0/SubIFD0/257", "IFD0/SubIFD0/258", "IFD0/SubIFD0/259", "IFD0/SubIFD0/262",
            "IFD0/SubIFD0/273", "IFD0/SubIFD0/277", "IFD0/SubIFD0/278", "IFD0/SubIFD0/279", "IFD0/SubIFD0/284",
            "IFD0/SubIFD0/33421", "IFD0/SubIFD0/33422",
            "IFD0/SubIFD1/256", "IFD0/SubIFD1/257", "IFD0/SubIFD1/259", "IFD0/SubIFD1/262", "IFD0/SubIFD1/273", "IFD0/SubIFD1/277", "IFD0/SubIFD1/278", "IFD0/SubIFD1/279"
        };
        profile.Fingerprint.MakerNoteSignature = "NikonEmbeddedTiff";

        profile.Geometry.StoredWidth = 8280;
        profile.Geometry.StoredHeight = 5520;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 12, 8, 8256, 5504);
        profile.Geometry.MaskedAreas = System.Array.Empty<RawArea>();
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);

        profile.Codec.Name = NikonCompressionCodec.LosslessCodecName;
        profile.Codec.Compression = 34713;
        profile.Codec.Packing = RawPacking.PackedBits;
        profile.Codec.StoredWordBits = 14;
        profile.Codec.Predictor = 0;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = 1;
        profile.Codec.SplitRow = -1;
        profile.Codec.SliceWidths = new int[] { 8280 };
        profile.Codec.Quirks = new string[]
        {
            "nikon-nefcompression-maker-note-3",
            NefIfdTopologyQuirk,
            NefSingleRawStripQuirk,
            NefLosslessCompressionInfoQuirk,
            NefLosslessHuffmanTableQuirk,
            NefLosslessPredictorQuirk,
            NefLosslessCurveQuirk,
            NefJpegPreviewQuirk,
            NikonMakerNoteRequiredQuirk,
            NikonMakerNoteEmbeddedTiffBaseQuirk,
            NikonMakerNoteReviewedTagsQuirk,
            NikonMakerNoteUnknownTagsQuirk
        };

        profile.Calibration.BlackLevels = System.Array.Empty<double>();
        profile.Calibration.WhiteLevels = System.Array.Empty<double>();
        profile.Calibration.WhiteBalance = System.Array.Empty<double>();
        profile.Calibration.ColorMatrix = System.Array.Empty<double>();
        profile.Calibration.ForwardMatrix = System.Array.Empty<double>();
        profile.Calibration.Illuminant = string.Empty;
        profile.Calibration.BaselineExposure = 0;
        profile.Provenance = CreateNikonZ8WriteProvenance();
        profile.Evidence = new RawProfileEvidence[] { CreateNikonZ8WriteEvidence(), CreateNikonZ8PixelEvidence() };
        profile.Experimental = true;
        return profile;
    }

    internal static RawProfileProvenance[] CreateNikonZ8WriteProvenance()
    {
        RawProfileProvenance[] observed = CreateNikonZ8Provenance();
        System.Collections.Generic.List<RawProfileProvenance> records = new System.Collections.Generic.List<RawProfileProvenance>(observed);
        AddNikonSample(records, "readProfileId", "The write profile reopens only through the exact observed Z 8 lossless 14-bit read profile.");
        AddNikonSample(records, "evidence", "The writer reopens generated output and proves decoded stored-pixel identity; external-application compatibility remains unconfirmed.");
        return records.ToArray();
    }

    internal static RawProfileEvidence CreateNikonZ8WriteEvidence()
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Structural;
        evidence.Tool = "raw profile validator";
        evidence.Version = "schema-1";
        evidence.SampleId = NikonNefMetadataParser.Z8LosslessProfile;
        evidence.Check = "new-lossless-parse-write-parse";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "The writer reopens its TIFF graph, Exif, Nikon type-3 maker note, compression parameters, preview, and entropy strip. External-application compatibility remains unconfirmed, so the profile remains experimental.";
        return evidence;
    }

    internal static RawProfileEvidence CreateNikonZ8PixelEvidence()
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.PixelIdentity;
        evidence.Tool = "raw NEF pixel verifier";
        evidence.Version = "decoder-1";
        evidence.SampleId = NikonNefMetadataParser.Z8LosslessProfile;
        evidence.Check = "decode-compare-stored-pixel-identity";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Every decoded stored sample is compared with its input after output reopen and must be identical.";
        return evidence;
    }

    internal static void AddNikonSample(System.Collections.Generic.List<RawProfileProvenance> records, string field, string notes)
    {
        Add(records, field, NikonSampleCatalogue, RawProfileProvenanceKind.Sample, NikonSampleMetadataLicense, notes);
    }

    internal static RawReadProfile CreateCanonEos6DMarkIi()
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = "canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read";

        profile.Identity.Format = RawFormat.Cr2;
        profile.Identity.Make = "Canon";
        profile.Identity.Model = "Canon EOS 6D Mark II";
        profile.Identity.CameraId = "2147484678";
        profile.Identity.Aliases = new string[] { "EOS 6D Mark II" };
        profile.Identity.FirmwareMinimum = "1.0.2";
        profile.Identity.FirmwareMaximum = "1.0.2";

        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = 6384;
        profile.Fingerprint.Height = 4224;
        profile.Fingerprint.BitDepth = 14;
        profile.Fingerprint.Compression = 6;
        profile.Fingerprint.SamplesPerPixel = 0;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD3" };
        profile.Fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD0/272", "IFD3/256", "IFD3/257", "IFD3/259", "IFD3/273", "IFD3/279", "IFD3/50752" };
        profile.Fingerprint.MakerNoteSignature = "Canon";
        profile.Fingerprint.StructuralHash = string.Empty;

        profile.Geometry.StoredWidth = 6384;
        profile.Geometry.StoredHeight = 4224;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 132, 56, 6240, 4160);
        profile.Geometry.MaskedAreas = System.Array.Empty<RawArea>();
        profile.Geometry.Cfa = null;

        profile.Codec.Name = "canon-cr2-lossless-jpeg-sof3";
        profile.Codec.Compression = 6;
        profile.Codec.Packing = RawPacking.ByteAligned;
        profile.Codec.StoredWordBits = 14;
        profile.Codec.Predictor = 1;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = 1;
        profile.Codec.SplitRow = -1;
        profile.Codec.SliceWidths = new int[] { 6384 };
        profile.Codec.Quirks = new string[] { "sof3-four-components-3192x2112" };

        profile.Calibration.BlackLevels = System.Array.Empty<double>();
        profile.Calibration.WhiteLevels = System.Array.Empty<double>();
        profile.Calibration.WhiteBalance = System.Array.Empty<double>();
        profile.Calibration.ColorMatrix = System.Array.Empty<double>();
        profile.Calibration.ForwardMatrix = System.Array.Empty<double>();
        profile.Calibration.Illuminant = string.Empty;
        profile.Calibration.BaselineExposure = 0;
        profile.Experimental = true;
        profile.Provenance = CreateCanonEos6DMarkIiProvenance();
        return profile;
    }

    internal static RawReadProfile CreateCanonEos20d()
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = "canon.eos-20d.cr2.raw-3x2.firmware-2.0.3.read";
        profile.Identity.Format = RawFormat.Cr2;
        profile.Identity.Make = "Canon";
        profile.Identity.Model = "Canon EOS 20D";
        profile.Identity.CameraId = "2147484021";
        profile.Identity.Aliases = new string[] { "EOS 20D" };
        profile.Identity.FirmwareMinimum = "2.0.3";
        profile.Identity.FirmwareMaximum = "2.0.3";
        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = 3596;
        profile.Fingerprint.Height = 2360;
        profile.Fingerprint.BitDepth = 12;
        profile.Fingerprint.Compression = 6;
        profile.Fingerprint.SamplesPerPixel = 0;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD3" };
        profile.Fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD0/272", "IFD3/259", "IFD3/273", "IFD3/279" };
        profile.Fingerprint.MakerNoteSignature = "Canon";
        profile.Geometry.StoredWidth = 3596;
        profile.Geometry.StoredHeight = 2360;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 84, 19, 3504, 2336);
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        profile.Codec.Name = "canon-cr2-lossless-jpeg-early-offset";
        profile.Codec.Compression = 6;
        profile.Codec.StoredWordBits = 12;
        profile.Codec.Predictor = 1;
        profile.Codec.SliceWidths = new int[] { 3596 };
        profile.Codec.Quirks = new string[] { CanonEarlyRawLayout.DimensionlessIfdQuirk };
        profile.Experimental = true;
        profile.Provenance = CreateObservedProfileProvenance(Eos20dSample, Eos20dSampleHash, "The CC0 EOS 20D sample records the exact early dimensionless IFD3, two-component SOF3 frame, identity, firmware, and sensor geometry used by this profile.");
        return profile;
    }

    internal static RawWriteProfile CreateCanonEos6DMarkIiWrite()
    {
        RawWriteProfile profile = new RawWriteProfile();
        profile.Id = Eos6DMarkIiWriteProfile;
        profile.ReadProfileId = CanonCr2MetadataParser.Eos6DMarkIiProfile;

        profile.Identity.Format = RawFormat.Cr2;
        profile.Identity.Make = "Canon";
        profile.Identity.Model = "Canon EOS 6D Mark II";
        profile.Identity.CameraId = "2147484678";
        profile.Identity.Aliases = new string[] { "EOS 6D Mark II" };
        profile.Identity.FirmwareMinimum = "1.0.2";
        profile.Identity.FirmwareMaximum = "1.0.2";

        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = 6384;
        profile.Fingerprint.Height = 4224;
        profile.Fingerprint.BitDepth = 14;
        profile.Fingerprint.Compression = 6;
        profile.Fingerprint.SamplesPerPixel = 0;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD1", "IFD2", "IFD3", "IFD0/Exif" };
        profile.Fingerprint.RequiredTags = new string[]
        {
            "IFD0/256", "IFD0/257", "IFD0/258", "IFD0/259", "IFD0/262", "IFD0/271", "IFD0/272", "IFD0/273", "IFD0/279", "IFD0/34665",
            "IFD1/256", "IFD1/257", "IFD1/259", "IFD1/262", "IFD1/513", "IFD1/514",
            "IFD2/256", "IFD2/257", "IFD2/258", "IFD2/259", "IFD2/262", "IFD2/273", "IFD2/279",
            "IFD3/256", "IFD3/257", "IFD3/258", "IFD3/259", "IFD3/262", "IFD3/273", "IFD3/279", "IFD3/50752",
            "IFD0/Exif/37500"
        };
        profile.Fingerprint.MakerNoteSignature = "Canon";
        profile.Fingerprint.StructuralHash = string.Empty;

        profile.Geometry.StoredWidth = 6384;
        profile.Geometry.StoredHeight = 4224;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 132, 56, 6240, 4160);
        profile.Geometry.MaskedAreas = System.Array.Empty<RawArea>();
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);

        profile.Codec.Name = "canon-cr2-lossless-jpeg-sof3-write";
        profile.Codec.Compression = 6;
        profile.Codec.Packing = RawPacking.ByteAligned;
        profile.Codec.StoredWordBits = 14;
        profile.Codec.Predictor = 1;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = 1;
        profile.Codec.SplitRow = -1;
        profile.Codec.SliceWidths = new int[] { 6384 };
        profile.Codec.Quirks = new string[]
        {
            "sof3-four-components-3192x2112",
            Cr2HeaderVersionQuirk,
            Cr2IfdChainQuirk,
            Cr2Ifd0PreviewQuirk,
            Cr2Ifd1JpegThumbnailQuirk,
            Cr2Ifd2PreviewQuirk,
            Cr2JpegPointTransformQuirk,
            Cr2JpegRestartQuirk,
            Cr2JpegInterleavedQuirk,
            Cr2JpegOptimizedHuffmanQuirk,
            CanonMakerNoteRequiredQuirk,
            CanonMakerNoteReviewedTagsQuirk,
            CanonMakerNoteRelocationQuirk,
            CanonMakerNoteUnknownTagsQuirk
        };

        profile.Calibration.BlackLevels = System.Array.Empty<double>();
        profile.Calibration.WhiteLevels = System.Array.Empty<double>();
        profile.Calibration.WhiteBalance = System.Array.Empty<double>();
        profile.Calibration.ColorMatrix = System.Array.Empty<double>();
        profile.Calibration.ForwardMatrix = System.Array.Empty<double>();
        profile.Calibration.Illuminant = string.Empty;
        profile.Calibration.BaselineExposure = 0;
        profile.Provenance = CreateCanonEos6DMarkIiWriteProvenance();
        profile.Evidence = new RawProfileEvidence[] { CreateCanonEos6DMarkIiWriteEvidence(), CreateCanonEos6DMarkIiPixelEvidence() };
        profile.Experimental = true;
        return profile;
    }

    internal static RawReadProfile CreateCanonEos6DMarkIiMRaw()
    {
        return CreateCanonEos6DMarkIiSubsampled(CanonCr2MetadataParser.Eos6DMarkIiMRawProfile, 4680, 3132, new RawArea(RawAreaKind.Active, 0, 12, 4680, 3120),
            new int[] { 2808, 2808, 2808, 2808, 2808 }, CanonSRawDecoder.Layout420Quirk, Eos6DMarkIiMRawSample, Eos6DMarkIiMRawSampleHash,
            "The CC0 EOS 6D Mark II mRAW sample records SRAWQuality one, raw-type four, 2x2 component sampling, the five-slice packed width, firmware, and output geometry used by this profile.");
    }

    internal static RawReadProfile CreateCanonEos6DMarkIiSRaw()
    {
        return CreateCanonEos6DMarkIiSubsampled(CanonCr2MetadataParser.Eos6DMarkIiSRawProfile, 3120, 2082, new RawArea(RawAreaKind.Active, 0, 2, 3120, 2080),
            new int[] { 1560, 1560, 1560, 1560 }, CanonSRawDecoder.Layout422Quirk, Eos6DMarkIiSRawSample, Eos6DMarkIiSRawSampleHash,
            "The CC0 EOS 6D Mark II sRAW sample records SRAWQuality two, raw-type four, 2x1 component sampling, the four-slice packed width, firmware, and output geometry used by this profile.");
    }

    internal static RawReadProfile CreateCanonEos6DMarkIiSubsampled(string id, int width, int height, RawArea activeArea, int[] sliceWidths, string layoutQuirk, string sample, string hash, string notes)
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = id;
        profile.Identity.Format = RawFormat.Cr2;
        profile.Identity.Make = "Canon";
        profile.Identity.Model = "Canon EOS 6D Mark II";
        profile.Identity.CameraId = "2147484678";
        profile.Identity.Aliases = new string[] { "EOS 6D Mark II" };
        profile.Identity.FirmwareMinimum = "1.0.2";
        profile.Identity.FirmwareMaximum = "1.0.2";
        profile.Fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.Width = width;
        profile.Fingerprint.Height = height;
        profile.Fingerprint.BitDepth = 15;
        profile.Fingerprint.Compression = 6;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0", "IFD3" };
        profile.Fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD0/272", "IFD3/256", "IFD3/257", "IFD3/259", "IFD3/273", "IFD3/279", "IFD3/50752", "IFD3/50885" };
        profile.Fingerprint.MakerNoteSignature = "Canon";
        profile.Geometry.StoredWidth = width;
        profile.Geometry.StoredHeight = height;
        profile.Geometry.ActiveArea = activeArea;
        profile.Geometry.Cfa = null;
        profile.Codec.Name = "canon-cr2-sraw-lossless-jpeg-sof3";
        profile.Codec.Compression = 6;
        profile.Codec.StoredWordBits = 15;
        profile.Codec.Predictor = 1;
        profile.Codec.SliceWidths = sliceWidths;
        profile.Codec.Quirks = new string[] { CanonSRawDecoder.SubsampledQuirk, layoutQuirk, CanonSRawDecoder.Generation2Quirk, CanonSRawDecoder.NewHueQuirk, CanonSRawDecoder.InvertWhiteBalanceQuirk };
        profile.Experimental = true;
        profile.Provenance = CreateObservedProfileProvenance(sample, hash, notes);
        return profile;
    }

    internal static RawReadProfile CreateCanonEos1d()
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = "canon.eos-1d.tiff.raw-3x2.firmware-1.4.0.read";
        profile.Identity.Format = RawFormat.Tiff;
        profile.Identity.Make = "Canon";
        profile.Identity.Model = "Canon EOS-1D";
        profile.Identity.CameraId = "2147483649";
        profile.Identity.Aliases = new string[] { "EOS-1D" };
        profile.Identity.FirmwareMinimum = "1.4.0";
        profile.Identity.FirmwareMaximum = "1.4.0";
        profile.Fingerprint.ByteOrder = RawByteOrder.BigEndian;
        profile.Fingerprint.BigTiff = false;
        profile.Fingerprint.RequiredIfds = new string[] { "IFD0" };
        profile.Fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD0/272" };
        profile.Fingerprint.MakerNoteSignature = "Canon";
        profile.Geometry.StoredWidth = 2496;
        profile.Geometry.StoredHeight = 1662;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 2496, 1662);
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Grbg);
        profile.Codec.Name = "canon-tiff-lossless-jpeg-raw-offset";
        profile.Codec.Compression = 6;
        profile.Codec.StoredWordBits = 12;
        profile.Codec.Predictor = 1;
        profile.Codec.SliceWidths = new int[] { 2496 };
        profile.Codec.Quirks = new string[] { CanonEarlyRawLayout.RawOffsetQuirk, CanonEarlyRawLayout.HalfHeightDoubleWidthQuirk };
        profile.Experimental = true;
        profile.Provenance = CreateObservedProfileProvenance(Eos1dSample, Eos1dSampleHash, "The CC0 EOS-1D sample records the exact maker-note raw offset, half-height/double-width SOF3 layout, identity, firmware, and decoded geometry used by this profile.");
        return profile;
    }

    internal static RawProfileProvenance[] CreateObservedProfileProvenance(string source, string hash, string notes)
    {
        string[] fields = new string[]
        {
            "id", "identity.format", "identity.make", "identity.model", "identity.cameraId", "identity.aliases", "identity.firmwareMinimum", "identity.firmwareMaximum",
            "fingerprint.byteOrder", "fingerprint.bigTiff", "fingerprint.width", "fingerprint.height", "fingerprint.bitDepth", "fingerprint.compression", "fingerprint.samplesPerPixel",
            "fingerprint.requiredIfds", "fingerprint.requiredTags", "fingerprint.makerNoteSignature", "fingerprint.structuralHash", "geometry.storedWidth", "geometry.storedHeight",
            "geometry.activeArea", "geometry.maskedAreas", "geometry.cfa", "codec.name", "codec.compression", "codec.packing", "codec.storedWordBits", "codec.predictor",
            "codec.planarConfiguration", "codec.rowAlignment", "codec.splitRow", "codec.sliceWidths", "codec.quirks", "calibration.blackLevels", "calibration.whiteLevels",
            "calibration.whiteBalance", "calibration.colorMatrix", "calibration.forwardMatrix", "calibration.illuminant", "calibration.baselineExposure", "experimental"
        };
        RawProfileProvenance[] result = new RawProfileProvenance[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            RawProfileProvenance item = new RawProfileProvenance();
            item.Field = fields[index];
            item.Source = source;
            item.Kind = RawProfileProvenanceKind.Sample;
            item.License = "CC0-1.0";
            item.Retrieved = Retrieved;
            item.Notes = "sha256:" + hash + "; " + notes;
            result[index] = item;
            index++;
        }
        return result;
    }

    internal static RawProfileProvenance[] CreateCanonEos6DMarkIiProvenance()
    {
        System.Collections.Generic.List<RawProfileProvenance> records = new System.Collections.Generic.List<RawProfileProvenance>();
        AddSample(records, "id", "Stable library identifier for this exact observed sample variant.");
        AddManufacturer(records, "identity.format", "Canon identifies the recorded still-image type as Canon original RAW; the CC0 sample has a CR2 header.");
        AddSample(records, "identity.make", "IFD0 Make is Canon.");
        AddSample(records, "identity.model", "IFD0 Model is Canon EOS 6D Mark II.");
        AddSample(records, "identity.cameraId", "Canon ModelID is 2147484678.");
        AddManufacturer(records, "identity.aliases", "The public specification names the product EOS 6D Mark II.");
        AddSample(records, "identity.firmwareMinimum", "The observed sample records firmware 1.0.2; no broader structural claim is made.");
        AddSample(records, "identity.firmwareMaximum", "The observed sample records firmware 1.0.2; no broader structural claim is made.");
        AddSample(records, "fingerprint.byteOrder", "The file begins with the little-endian TIFF marker II.");
        AddSample(records, "fingerprint.bigTiff", "The file begins with classic TIFF magic 42 rather than BigTIFF magic 43.");
        AddSample(records, "fingerprint.width", "IFD3 ImageWidth is 6384.");
        AddSample(records, "fingerprint.height", "IFD3 ImageLength is 4224.");
        AddManufacturer(records, "fingerprint.bitDepth", "Canon specifies 14-bit RAW recording.");
        AddSample(records, "fingerprint.compression", "IFD3 Compression is TIFF value 6 and its strip begins with a lossless JPEG SOF3 stream.");
        AddSample(records, "fingerprint.samplesPerPixel", "IFD3 omits SamplesPerPixel, so zero deliberately leaves it outside matching.");
        AddSample(records, "fingerprint.requiredIfds", "The sample's TIFF chain places the sensor payload in IFD3.");
        AddSample(records, "fingerprint.requiredTags", "The listed identity, dimensions, compression, strip, and CR2 slice tags are present in the sample.");
        AddSample(records, "fingerprint.makerNoteSignature", "The Exif maker note begins with the Canon signature.");
        AddSample(records, "fingerprint.structuralHash", "No structure-only digest is published; the field is deliberately empty.");
        AddSample(records, "geometry.storedWidth", "IFD3 and Canon SensorInfo report 6384 stored columns.");
        AddSample(records, "geometry.storedHeight", "IFD3 and Canon SensorInfo report 4224 stored rows.");
        AddSample(records, "geometry.activeArea", "Canon SensorInfo borders 132, 56, 6371, 4215 yield a 6240 by 4160 inclusive active area, matching Canon's published RAW size.");
        AddSample(records, "geometry.maskedAreas", "No independently documented optical-black rectangles are installed; the collection is deliberately empty.");
        AddSample(records, "geometry.cfa", "The sample has no standard CFA tag and no independently documented CFA value is installed.");
        AddSample(records, "codec.name", "The raw strip contains a lossless JPEG SOF3 marker.");
        AddSample(records, "codec.compression", "IFD3 Compression is TIFF value 6.");
        AddSample(records, "codec.packing", "The compressed stream is byte-addressed; no separate packed-word convention is asserted.");
        AddSample(records, "codec.storedWordBits", "The lossless JPEG SOF3 precision is 14 bits, corroborated by Canon's specification.");
        AddSample(records, "codec.predictor", "The lossless JPEG scan selection value is predictor 1.");
        AddSample(records, "codec.planarConfiguration", "The single scan carries all four lossless JPEG components together.");
        AddSample(records, "codec.rowAlignment", "No row-padding requirement is asserted; byte alignment is the neutral value.");
        AddSample(records, "codec.splitRow", "The sample contains one scan and no independently observed split-row transition.");
        AddSample(records, "codec.sliceWidths", "CR2 tag 0xC640 records zero regular slices and a final width of 6384.");
        AddSample(records, "codec.quirks", "SOF3 records four components over a 3192 by 2112 coded frame for the 6384 by 4224 sensor grid.");
        AddSample(records, "calibration.blackLevels", "No unambiguous public black-level values are installed; the collection is deliberately empty.");
        AddSample(records, "calibration.whiteLevels", "No unambiguous public white-level values are installed; the collection is deliberately empty.");
        AddSample(records, "calibration.whiteBalance", "No undocumented maker-note color-data interpretation is installed; the collection is deliberately empty.");
        AddSample(records, "calibration.colorMatrix", "No independently published color matrix is installed; the collection is deliberately empty.");
        AddSample(records, "calibration.forwardMatrix", "No independently published forward matrix is installed; the collection is deliberately empty.");
        AddSample(records, "calibration.illuminant", "No matrix is installed, so the illuminant is deliberately empty.");
        AddSample(records, "calibration.baselineExposure", "No independently published baseline exposure is installed; the neutral value is zero.");
        AddSample(records, "experimental", "Read support remains opt-in until the later CR2 decoder tasks qualify the payload path.");
        return records.ToArray();
    }

    internal static RawProfileProvenance[] CreateCanonEos6DMarkIiWriteProvenance()
    {
        RawProfileProvenance[] observed = CreateObservedProfileProvenance(Sample, SampleHash,
            "The CC0 reference sample records the exact CR2 2.0 header, IFD0/IFD1/IFD2/IFD3 chain, identity tags, preview locations, Canon maker note, 6384-pixel slice, and four-component 14-bit SOF3 parameters required by this conservative writer profile.");
        System.Collections.Generic.List<RawProfileProvenance> records = new System.Collections.Generic.List<RawProfileProvenance>(observed);
        AddSample(records, "readProfileId", "The write profile reopens only through the exact observed EOS 6D Mark II read profile.");
        AddSample(records, "evidence", "The writer reopens generated output and proves decoded stored-pixel identity; external-application compatibility remains unconfirmed.");
        return records.ToArray();
    }

    internal static RawProfileEvidence CreateCanonEos6DMarkIiWriteEvidence()
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Structural;
        evidence.Tool = "raw CR2 writer validator";
        evidence.Version = "schema-1";
        evidence.SampleId = Eos6DMarkIiWriteProfile;
        evidence.Check = "new-lossless-parse-write-parse";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "The writer reopens its CR2 graph, Exif, Canon maker note, previews, slice table, and lossless JPEG payload. External-application compatibility remains unconfirmed, so the profile remains experimental.";
        return evidence;
    }

    internal static RawProfileEvidence CreateCanonEos6DMarkIiPixelEvidence()
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.PixelIdentity;
        evidence.Tool = "raw CR2 pixel verifier";
        evidence.Version = "decoder-1";
        evidence.SampleId = Eos6DMarkIiWriteProfile;
        evidence.Check = "decode-compare-stored-pixel-identity";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Every decoded stored sample is compared with its input after output reopen and must be identical.";
        return evidence;
    }

    internal static void AddSample(System.Collections.Generic.List<RawProfileProvenance> records, string field, string notes)
    {
        Add(records, field, Sample, RawProfileProvenanceKind.Sample, SampleLicense, "sha256:" + SampleHash + "; " + notes);
    }

    internal static void AddManufacturer(System.Collections.Generic.List<RawProfileProvenance> records, string field, string notes)
    {
        Add(records, field, CanonSpecification, RawProfileProvenanceKind.Manufacturer, CanonSpecificationLicense, notes);
    }

    internal static void Add(System.Collections.Generic.List<RawProfileProvenance> records, string field, string source, RawProfileProvenanceKind kind, string license, string notes)
    {
        RawProfileProvenance provenance = new RawProfileProvenance();
        provenance.Field = field;
        provenance.Source = source;
        provenance.Kind = kind;
        provenance.License = license;
        provenance.Retrieved = Retrieved;
        provenance.Notes = notes;
        records.Add(provenance);
    }
}
