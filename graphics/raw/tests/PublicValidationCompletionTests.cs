using raw;

namespace raw.Tests;

public class PublicValidationCompletionTests
{
    [Test]
    public void EveryPublicOptionFamilyCanBeValidatedAndFindingsUseItsSelectedCategory()
    {
        IRawCodec codec = new RawCodecService();
        RawValidationOptions all = new RawValidationOptions();
        RawValidationReport[] defaults = new RawValidationReport[]
        {
            codec.ValidateOpenOptions(new RawOpenOptions(), all),
            codec.ValidateDecodeOptions(new RawDecodeOptions(), all),
            codec.ValidateNormalizationOptions(new RawNormalizationOptions(), all),
            codec.ValidateDefectOptions(new RawDefectOptions(), all),
            codec.ValidateLensCorrectionOptions(new RawLensCorrectionOptions(), all),
            codec.ValidatePreviewOptions(new RawPreviewOptions(), all),
            codec.ValidateAnalysisOptions(new RawAnalysisOptions(), all),
            codec.ValidateHistogramOptions(new RawHistogramOptions(), all),
            codec.ValidateRenderOptions(new RawRenderOptions(), all),
            codec.ValidateIccTransformOptions(new RawIccTransformOptions(), all),
            codec.ValidateMetadataOptions(new RawMetadataOptions(), all),
            codec.ValidateWriteOptions(new RawWriteOptions(), all),
            codec.ValidateGenericEncodeOptions(new RawGenericEncodeOptions(), all),
            codec.ValidateValidationOptions(new RawValidationOptions())
        };
        RawDecodeOptions decode = new RawDecodeOptions();
        decode.ImageIndex = -1;
        RawMetadataOptions metadata = new RawMetadataOptions();
        metadata.ReadExif = false;
        metadata.ReadGps = false;
        metadata.ReadXmp = false;
        metadata.ReadIptc = false;
        metadata.ReadIcc = false;
        metadata.ReadMakerNotes = false;
        RawWriteOptions write = new RawWriteOptions();
        write.ValidateAfterWrite = false;

        Assert.Multiple(() =>
        {
            Assert.That(defaults.All(codec.ValidationReportIsValid), Is.True);
            Assert.That(codec.ValidateDecodeOptions(decode, Only(RawValidationCategory.Semantic, true)).Messages, Is.All.Matches<RawValidationMessage>(item => item.Category == RawValidationCategory.Semantic));
            Assert.That(codec.ValidateMetadataOptions(metadata, Only(RawValidationCategory.Metadata, true)).Messages, Is.All.Matches<RawValidationMessage>(item => item.Category == RawValidationCategory.Metadata));
            Assert.That(codec.ValidateWriteOptions(write, Only(RawValidationCategory.Writer, true)).Messages, Is.All.Matches<RawValidationMessage>(item => item.Category == RawValidationCategory.Writer));
            Assert.That(codec.ValidationReportIsValid(codec.ValidateDecodeOptions(decode, Only(RawValidationCategory.Metadata, true))), Is.True);
        });
    }

    [Test]
    public void DescriptorProfileAndWritePlanValidationHonorSelectionAndStrictness()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = codec.CreateGrayDescriptor(2, 2, 8, 8, RawByteOrder.LittleEndian);
        RawProfileSchemaDocument profiles = new RawProfileSchemaDocument();
        RawWritePlan plan = new RawWritePlan(true, 0, string.Empty, Array.Empty<RawWriteChange>(), new RawDiagnostic[]
        {
            new RawDiagnostic(RawDiagnosticCode.SignatureInvalidated, RawSeverity.Warning, RawFormat.Tiff, -1, string.Empty, -1, "Writer", "warning", false)
        });
        RawValidationReport descriptorReport = codec.ValidateDescriptorModel(descriptor, Only(RawValidationCategory.Structural, true));
        RawValidationReport profileReport = codec.ValidateProfileDocumentModel(profiles, Only(RawValidationCategory.Profile, true));
        RawValidationReport strict = codec.ValidateWritePlan(plan, Only(RawValidationCategory.Writer, true));
        RawValidationReport tolerant = codec.ValidateWritePlan(plan, Only(RawValidationCategory.Writer, false));

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidationReportIsValid(descriptorReport), Is.True);
            Assert.That(codec.ValidationReportIsValid(profileReport), Is.True);
            Assert.That(codec.ValidationReportHasWarnings(strict), Is.True);
            Assert.That(codec.ValidationReportIsConformant(strict), Is.False);
            Assert.That(codec.ValidationReportIsConformant(tolerant), Is.True);
            Assert.That((codec.FindValidationCategory(strict, RawValidationCategory.Writer) ?? throw new InvalidOperationException()).Selected, Is.True);
            Assert.That((codec.FindValidationCategory(strict, RawValidationCategory.Metadata) ?? throw new InvalidOperationException()).Selected, Is.False);
        });
    }

    [Test]
    public void StandaloneMetadataModelsValidateAndMutableInconsistenciesAreReported()
    {
        IRawCodec codec = new RawCodecService();
        RawValidationOptions metadataOnly = Only(RawValidationCategory.Metadata, true);
        string xml = "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description xmlns:dc=\"http://purl.org/dc/elements/1.1/\" dc:title=\"test\"/></rdf:RDF></x:xmpmeta>";
        RawXmpPacket xmp = codec.ParseXmpPacket(System.Text.Encoding.UTF8.GetBytes(xml), new RawLimits());
        RawIptcDataset dataset = codec.CreateTextIptcDataset(2, 5, "Object", false);
        RawIptcRecord iptc = codec.CreateIptcRecord(new RawIptcDataset[] { dataset }, new RawLimits());
        RawIccProfile icc = codec.ParseIccProfile(IccCodecTests.CreateProfile(), new RawLimits());
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        RawDocument nef = codec.OpenBytes(NefParserTests.BuildNef().Bytes, null, openOptions);
        RawMakerNote makerNote = nef.Metadata.MakerNotes.First();

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidationReportIsValid(codec.ValidateXmpPacket(xmp, metadataOnly)), Is.True);
            Assert.That(codec.ValidationReportIsValid(codec.ValidateIptcRecord(iptc, metadataOnly)), Is.True);
            Assert.That(codec.ValidationReportIsValid(codec.ValidateIccProfileModel(icc, metadataOnly)), Is.True);
            Assert.That(codec.ValidationReportIsValid(codec.ValidateMakerNoteModel(makerNote, metadataOnly)), Is.True);
        });

        (xmp.XmlBytes ?? throw new InvalidOperationException())[0] ^= 1;
        iptc.PaddingBytes = new byte[] { 0 };
        icc.Tags = null;
        makerNote.RawBytes = null;

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidationReportIsValid(codec.ValidateXmpPacket(xmp, metadataOnly)), Is.False);
            Assert.That(codec.ValidationReportIsValid(codec.ValidateIptcRecord(iptc, metadataOnly)), Is.False);
            Assert.That(codec.ValidationReportIsValid(codec.ValidateIccProfileModel(icc, metadataOnly)), Is.False);
            Assert.That(codec.ValidationReportIsValid(codec.ValidateMakerNoteModel(makerNote, metadataOnly)), Is.False);
        });
    }

    [Test]
    public void PublicImageModelValidatorsCoverEveryImageLayer()
    {
        IRawCodec codec = new RawCodecService();
        RawValidationOptions semantic = Only(RawValidationCategory.Semantic, true);
        RawArea area = new RawArea(RawAreaKind.Unknown, -1, 0, 0, 1);
        RawPlaneInfo plane = new RawPlaneInfo(-1, string.Empty, RawChannelKind.Unknown, 0, 0, 0, 0);
        RawImageInfo info = new RawImageInfo(-1, RawImageKind.Unknown, 0, 0, 0, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Unknown, RawCompression.Unknown, RawOrientation.Unknown, Array.Empty<RawChannelDescription>(), Array.Empty<RawPlaneInfo>(), null, area, null, null, null);
        RawStoredImage stored = new RawStoredImage(info, Array.Empty<uint>(), null, null, 0, true, null);
        RawSensorImage sensor = new RawSensorImage(info, Array.Empty<ushort>(), null, 0, true, null, null, null, null, null, null, null, null);
        RawRgbImage rgb = new RawRgbImage(0, 0, 0, 0, null, null, null, Array.Empty<RawChannelDescription>(), RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Unknown, false, true, null);
        RawThumbnail thumbnail = new RawThumbnail(-1, RawThumbnailKind.Unknown, 0, 0, RawOrientation.Unknown, string.Empty, Array.Empty<byte>(), null, null);
        RawValidationReport[] reports = new RawValidationReport[]
        {
            codec.ValidateArea(area, 1, 1, semantic),
            codec.ValidatePlaneInfo(plane, semantic),
            codec.ValidateImageInfo(info, semantic),
            codec.ValidateStoredImage(stored, semantic),
            codec.ValidateSensorImage(sensor, semantic),
            codec.ValidateRgbImage(rgb, semantic),
            codec.ValidateThumbnail(thumbnail, semantic)
        };

        Assert.Multiple(() =>
        {
            Assert.That(reports.All(item => !codec.ValidationReportIsValid(item)), Is.True);
            Assert.That(reports.SelectMany(item => item.Messages ?? Array.Empty<RawValidationMessage>()), Is.All.Matches<RawValidationMessage>(item => item.Category == RawValidationCategory.Semantic));
        });
    }

    static RawValidationOptions Only(RawValidationCategory category, bool strict)
    {
        RawValidationOptions options = new RawValidationOptions();
        options.Strict = strict;
        options.AllowRecovery = !strict;
        options.ValidateStructure = category == RawValidationCategory.Structural;
        options.ValidateCodec = category == RawValidationCategory.Codec;
        options.ValidateSemantic = category == RawValidationCategory.Semantic;
        options.ValidateProfile = category == RawValidationCategory.Profile;
        options.ValidateMetadata = category == RawValidationCategory.Metadata;
        options.ValidateWriter = category == RawValidationCategory.Writer;
        return options;
    }
}
