using raw;
using System.Text;
using System.Text.Json;

namespace raw.Tests;

public class RawProfileSchemaCodecTests
{
    [Test]
    public void VersionOneRoundTripRetainsReadWriteAliasesFirmwareFingerprintGeometryCodecCalibrationProvenanceAndEvidence()
    {
        RawProfileSchemaDocument source = CreateDocument();

        byte[] firstJson = RawProfileSchemaCodec.Write(source, new RawLimits());
        RawProfileSchemaDocument result = RawProfileSchemaCodec.Read(firstJson, new RawLimits());
        byte[] secondJson = RawProfileSchemaCodec.Write(result, new RawLimits());
        RawReadProfile read = result.ReadProfiles.Single();
        RawWriteProfile write = result.WriteProfiles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(secondJson, Is.EqualTo(firstJson));
            Assert.That(result.SchemaVersion, Is.EqualTo(RawProfileSchemaCodec.CurrentSchemaVersion));
            Assert.That(read.Id, Is.EqualTo("canon.eos-r5.read.v1"));
            Assert.That(read.Identity.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(read.Identity.Aliases, Is.EqualTo(new string[] { "Canon EOS R5", "EOSR5" }));
            Assert.That(read.Identity.FirmwareMinimum, Is.EqualTo("1.0.0"));
            Assert.That(read.Identity.FirmwareMaximum, Is.EqualTo("2.1.0"));
            Assert.That(read.Fingerprint.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(read.Fingerprint.RequiredIfds, Is.EqualTo(new string[] { "IFD0", "IFD0/Exif", "IFD3" }));
            Assert.That(read.Fingerprint.RequiredTags, Is.EqualTo(new string[] { "IFD0/271", "IFD3/273", "IFD3/50752" }));
            Assert.That((read.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawAreaKind.Active));
            Assert.That(read.Geometry.MaskedAreas.Single().Kind, Is.EqualTo(RawAreaKind.OpticalBlack));
            Assert.That((read.Geometry.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Colors, Is.EqualTo(RawCfaPattern.CreateBayer(RawCfaLayout.Rggb).Colors));
            Assert.That(read.Codec.Name, Is.EqualTo("canon-lossless-jpeg"));
            Assert.That(read.Codec.Quirks, Is.EqualTo(new string[] { "cr2-slice-table", "predictor-reset-per-slice" }));
            Assert.That(read.Calibration.BlackLevels, Is.EqualTo(new double[] { 512, 513, 511, 512 }));
            Assert.That(read.Calibration.ColorMatrix, Has.Length.EqualTo(9));
            Assert.That(read.Provenance.Single().Kind, Is.EqualTo(RawProfileProvenanceKind.Sample));
            Assert.That(read.Experimental, Is.False);
            Assert.That(write.Id, Is.EqualTo("canon.eos-r5.write.v1"));
            Assert.That(write.ReadProfileId, Is.EqualTo(read.Id));
            Assert.That(write.Evidence.Single().Kind, Is.EqualTo(RawProfileEvidenceKind.Application));
            Assert.That(write.Evidence.Single().Result, Is.EqualTo(RawProfileEvidenceResult.Passed));
            Assert.That(write.Experimental, Is.False);
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });

        string jsonText = Encoding.UTF8.GetString(firstJson);
        Assert.Multiple(() =>
        {
            Assert.That(jsonText, Does.Contain("\"schemaVersion\": 1"));
            Assert.That(jsonText, Does.Contain("\"readProfiles\""));
            Assert.That(jsonText, Does.Contain("\"writeProfiles\""));
            Assert.That(jsonText, Does.Contain("\"format\": \"Cr2\""));
            Assert.That(jsonText, Does.Contain("\"kind\": \"Sample\""));
            Assert.That(jsonText, Does.Contain("\"result\": \"Passed\""));
        });
    }

    [Test]
    public void ReadAndWriteProfilesRemainSeparateAndWriterMustReferenceAReadProfile()
    {
        RawProfileSchemaDocument valid = CreateDocument();
        RawProfileSchemaDocument readOnly = CreateDocument();
        readOnly.WriteProfiles = Array.Empty<RawWriteProfile>();
        RawProfileSchemaDocument writeOnly = CreateDocument();
        writeOnly.ReadProfiles = Array.Empty<RawReadProfile>();
        RawProfileSchemaDocument missingReference = CreateDocument();
        missingReference.WriteProfiles[0].ReadProfileId = "missing.read.profile";

        RawProfileSchemaDocument decodedReadOnly = RawProfileSchemaCodec.Read(RawProfileSchemaCodec.Write(readOnly, new RawLimits()), new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(decodedReadOnly.ReadProfiles, Has.Length.EqualTo(1));
            Assert.That(decodedReadOnly.WriteProfiles, Is.Empty);
            Assert.That(writeOnly.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(missingReference.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.Throws<RawWriteException>(() => RawProfileSchemaCodec.Write(writeOnly, new RawLimits()));
            Assert.Throws<RawWriteException>(() => RawProfileSchemaCodec.Write(missingReference, new RawLimits()));
        });
    }

    [Test]
    public void UnknownDuplicateMalformedAndFutureSchemaMembersAreRejected()
    {
        string valid = Encoding.UTF8.GetString(RawProfileSchemaCodec.Write(CreateDocument(), new RawLimits()));
        byte[] unknown = Encoding.UTF8.GetBytes(valid.Replace("\"make\": \"Canon\",", "\"make\": \"Canon\",\n        \"futureIdentity\": true,", StringComparison.Ordinal));
        byte[] duplicate = Encoding.UTF8.GetBytes(valid.Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1,\n  \"schemaVersion\": 1,", StringComparison.Ordinal));
        byte[] future = Encoding.UTF8.GetBytes(valid.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal));
        byte[] wrongType = Encoding.UTF8.GetBytes(valid.Replace("\"storedWidth\": 8192", "\"storedWidth\": \"8192\"", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(() => RawProfileSchemaCodec.Read(unknown, new RawLimits()), Throws.InstanceOf<JsonException>());
            Assert.Throws<RawFormatException>(() => RawProfileSchemaCodec.Read(duplicate, new RawLimits()));
            Assert.Throws<RawUnsupportedException>(() => RawProfileSchemaCodec.Read(future, new RawLimits()));
            Assert.That(() => RawProfileSchemaCodec.Read(wrongType, new RawLimits()), Throws.InstanceOf<JsonException>());
            Assert.That(() => RawProfileSchemaCodec.Read(Encoding.UTF8.GetBytes("{"), new RawLimits()), Throws.InstanceOf<JsonException>());
        });
    }

    [Test]
    public void InvalidGeometryCalibrationProvenanceEvidenceIdsAndExperimentalStateAreReported()
    {
        RawProfileSchemaDocument geometry = CreateDocument();
        (geometry.ReadProfiles[0].Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width = 9000;
        RawProfileSchemaDocument calibration = CreateDocument();
        calibration.ReadProfiles[0].Calibration.ColorMatrix = new double[] { 1, 2, 3 };
        RawProfileSchemaDocument provenance = CreateDocument();
        provenance.ReadProfiles[0].Provenance = Array.Empty<RawProfileProvenance>();
        RawProfileSchemaDocument evidence = CreateDocument();
        evidence.WriteProfiles[0].Evidence[0].Result = RawProfileEvidenceResult.Failed;
        RawProfileSchemaDocument evidenceKind = CreateDocument();
        evidenceKind.WriteProfiles[0].Evidence[0].Kind = (RawProfileEvidenceKind)99;
        RawProfileSchemaDocument unconfirmed = CreateDocument();
        unconfirmed.WriteProfiles[0].Evidence[0].Kind = RawProfileEvidenceKind.Structural;
        RawProfileSchemaDocument missingApplicationVersion = CreateDocument();
        missingApplicationVersion.WriteProfiles[0].Evidence[0].Version = string.Empty;
        RawProfileSchemaDocument duplicateIds = CreateDocument();
        duplicateIds.ReadProfiles = new RawReadProfile[] { duplicateIds.ReadProfiles[0], CreateReadProfile() };

        Assert.Multiple(() =>
        {
            Assert.That(geometry.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(calibration.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(provenance.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(evidence.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(evidenceKind.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(unconfirmed.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(missingApplicationVersion.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(duplicateIds.Validate(new RawLimits()).IsValid(), Is.False);
        });

        evidence.WriteProfiles[0].Experimental = true;
        unconfirmed.WriteProfiles[0].Experimental = true;
        Assert.Multiple(() =>
        {
            Assert.That(evidence.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(unconfirmed.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void ProvenanceDetailsAndIndependentSourcesRoundTripAndMalformedRecordsFailValidation()
    {
        RawProfileSchemaDocument source = CreateDocument();
        RawProfileProvenance manufacturer = new RawProfileProvenance();
        manufacturer.Field = "identity.model";
        manufacturer.Source = "https://manufacturer.example/specification";
        manufacturer.Kind = RawProfileProvenanceKind.Manufacturer;
        manufacturer.License = "Facts independently transcribed";
        manufacturer.Retrieved = "2026-08-19";
        manufacturer.Notes = "Published model spelling.";
        source.ReadProfiles[0].Provenance = new RawProfileProvenance[] { source.ReadProfiles[0].Provenance[0], manufacturer };

        RawProfileSchemaDocument decoded = RawProfileSchemaCodec.Read(RawProfileSchemaCodec.Write(source, new RawLimits()), new RawLimits());
        RawProfileSchemaDocument emptyField = CreateDocument();
        emptyField.ReadProfiles[0].Provenance[0].Field = string.Empty;
        RawProfileSchemaDocument emptySource = CreateDocument();
        emptySource.ReadProfiles[0].Provenance[0].Source = string.Empty;
        RawProfileSchemaDocument unknownKind = CreateDocument();
        unknownKind.ReadProfiles[0].Provenance[0].Kind = RawProfileProvenanceKind.Unknown;
        RawProfileSchemaDocument nullDetail = CreateDocument();
        nullDetail.ReadProfiles[0].Provenance[0].License = null;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.ReadProfiles[0].Provenance, Has.Length.EqualTo(2));
            Assert.That(decoded.ReadProfiles[0].Provenance[1].Field, Is.EqualTo("identity.model"));
            Assert.That(decoded.ReadProfiles[0].Provenance[1].Kind, Is.EqualTo(RawProfileProvenanceKind.Manufacturer));
            Assert.That(decoded.ReadProfiles[0].Provenance[1].License, Is.EqualTo("Facts independently transcribed"));
            Assert.That(decoded.ReadProfiles[0].Provenance[1].Retrieved, Is.EqualTo("2026-08-19"));
            Assert.That(decoded.ReadProfiles[0].Provenance[1].Notes, Is.EqualTo("Published model spelling."));
            Assert.That(emptyField.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(emptySource.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(unknownKind.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.That(nullDetail.Validate(new RawLimits()).IsValid(), Is.False);
        });
    }

    [Test]
    public void JsonInputAndOutputHonorMetadataAllocationFileDepthAndMemberLimits()
    {
        byte[] json = RawProfileSchemaCodec.Write(CreateDocument(), new RawLimits());
        RawLimits size = new RawLimits();
        size.MaxMetadataBytes = json.Length - 1;
        size.MaxTagValueBytes = json.Length - 1;
        size.MaxXmpBytes = json.Length - 1;
        RawLimits members = new RawLimits();
        members.MaxTagCount = 5;
        RawLimits depth = new RawLimits();
        depth.MaxRecursionDepth = 3;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => RawProfileSchemaCodec.Read(json, size));
            Assert.Throws<RawLimitException>(() => RawProfileSchemaCodec.Write(CreateDocument(), size));
            Assert.Throws<RawLimitException>(() => RawProfileSchemaCodec.Read(json, members));
            Assert.That(() => RawProfileSchemaCodec.Read(json, depth), Throws.InstanceOf<JsonException>());
        });
    }

    static RawProfileSchemaDocument CreateDocument()
    {
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        RawReadProfile read = CreateReadProfile();
        RawWriteProfile write = new RawWriteProfile();
        write.Id = "canon.eos-r5.write.v1";
        write.ReadProfileId = read.Id;
        write.Identity = CreateIdentity();
        write.Fingerprint = CreateFingerprint();
        write.Geometry = CreateGeometry();
        write.Codec = CreateCodec();
        write.Calibration = CreateCalibration();
        write.Provenance = CreateProvenance();
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Application;
        evidence.Tool = "ExifTool";
        evidence.Version = "13.40";
        evidence.SampleId = "sha256:generated-r5-profile";
        evidence.Check = "metadata-and-lossless-payload-open";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Reopened with exact decoded samples.";
        write.Evidence = new RawProfileEvidence[] { evidence };
        write.Experimental = false;
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        return document;
    }

    static RawReadProfile CreateReadProfile()
    {
        RawReadProfile read = new RawReadProfile();
        read.Id = "canon.eos-r5.read.v1";
        read.Identity = CreateIdentity();
        read.Fingerprint = CreateFingerprint();
        read.Geometry = CreateGeometry();
        read.Codec = CreateCodec();
        read.Calibration = CreateCalibration();
        read.Provenance = CreateProvenance();
        read.Experimental = false;
        return read;
    }

    static RawProfileIdentity CreateIdentity()
    {
        RawProfileIdentity identity = new RawProfileIdentity();
        identity.Format = RawFormat.Cr2;
        identity.Make = "Canon";
        identity.Model = "EOS R5";
        identity.CameraId = "0x80000421";
        identity.Aliases = new string[] { "Canon EOS R5", "EOSR5" };
        identity.FirmwareMinimum = "1.0.0";
        identity.FirmwareMaximum = "2.1.0";
        return identity;
    }

    static RawProfileFingerprint CreateFingerprint()
    {
        RawProfileFingerprint fingerprint = new RawProfileFingerprint();
        fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        fingerprint.BigTiff = false;
        fingerprint.Width = 8192;
        fingerprint.Height = 5464;
        fingerprint.BitDepth = 14;
        fingerprint.Compression = 6;
        fingerprint.SamplesPerPixel = 1;
        fingerprint.RequiredIfds = new string[] { "IFD0", "IFD0/Exif", "IFD3" };
        fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD3/273", "IFD3/50752" };
        fingerprint.MakerNoteSignature = "CanonIfd";
        fingerprint.StructuralHash = "0123456789abcdef";
        return fingerprint;
    }

    static RawProfileGeometry CreateGeometry()
    {
        RawProfileGeometry geometry = new RawProfileGeometry();
        geometry.StoredWidth = 8192;
        geometry.StoredHeight = 5464;
        geometry.ActiveArea = new RawArea(RawAreaKind.Active, 160, 120, 8000, 5320);
        geometry.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 128, 5464) };
        geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        return geometry;
    }

    static RawProfileCodec CreateCodec()
    {
        RawProfileCodec codec = new RawProfileCodec();
        codec.Name = "canon-lossless-jpeg";
        codec.Compression = 6;
        codec.Packing = RawPacking.ByteAligned;
        codec.StoredWordBits = 14;
        codec.Predictor = 1;
        codec.PlanarConfiguration = 1;
        codec.RowAlignment = 2;
        codec.SplitRow = -1;
        codec.SliceWidths = new int[] { 2048, 2048, 2048, 2048 };
        codec.Quirks = new string[] { "cr2-slice-table", "predictor-reset-per-slice" };
        return codec;
    }

    static RawProfileCalibration CreateCalibration()
    {
        RawProfileCalibration calibration = new RawProfileCalibration();
        calibration.BlackLevels = new double[] { 512, 513, 511, 512 };
        calibration.WhiteLevels = new double[] { 15000, 15000, 15000, 15000 };
        calibration.WhiteBalance = new double[] { 2.05, 1, 1.52, 1 };
        calibration.ColorMatrix = new double[] { 0.7, -0.2, -0.1, -0.1, 1.1, 0.1, 0, 0.2, 0.6 };
        calibration.ForwardMatrix = new double[] { 0.8, 0.1, 0.1, 0.2, 0.7, 0.1, 0.1, 0.1, 0.8 };
        calibration.Illuminant = "D65";
        calibration.BaselineExposure = -0.25;
        return calibration;
    }

    static RawProfileProvenance[] CreateProvenance()
    {
        RawProfileProvenance provenance = new RawProfileProvenance();
        provenance.Field = "geometry";
        provenance.Source = "sha256:redistributable-r5-sample";
        provenance.Kind = RawProfileProvenanceKind.Sample;
        provenance.License = "CC0 fixture";
        provenance.Retrieved = "2026-08-19";
        provenance.Notes = "Dimensions and active borders independently inspected.";
        return new RawProfileProvenance[] { provenance };
    }
}
