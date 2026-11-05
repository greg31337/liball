using raw;

namespace raw.Tests;

public class Cr2EncoderTests
{
    [Test]
    public void SmallProfileEncodesCr2WithExactPixelsSlicesPreviewsExifAndCallerProvenance()
    {
        RawWriteProfile profile = SmallProfile();
        RawCr2WriteInput input = Input(8, 4, 10);
        RawWriteOptions options = Options();

        byte[] encoded = Cr2Encoder.Encode(input, profile, options);
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.ReadPreviews = true;
        RawDocument document = RawCodec.Open(encoded, null, openOptions);
        RawImageInfo? rawInfo = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
        LosslessJpegImage jpeg = LosslessJpegCodec.Decode(document.ExtractPayload(rawInfo.Payload), new RawLimits(), CancellationToken.None);
        ushort[] decoded = Cr2LosslessJpegDecoder.DecodeSamples(jpeg, 8, 4, profile.Codec.SliceWidths, 4, 14, 1, new RawLimits(), CancellationToken.None);
        RawIfd? ifd0 = document.Metadata.FindIfd("IFD0");
        RawIfd? exif = document.Metadata.FindIfd("IFD0/Exif");
        RawIfd? ifd3 = document.Metadata.FindIfd("IFD3");

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).MajorVersion, Is.EqualTo(2));
            Assert.That((document.FormatDetails.Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).MinorVersion, Is.Zero);
            Assert.That(document.FormatDetails.Cr2Header.FirstIfdOffset, Is.EqualTo(16));
            Assert.That(document.FormatDetails.Cr2Header.RawIfdPath, Is.EqualTo("IFD3"));
            Assert.That(document.Metadata.Ifds.Where(item => item.Path.StartsWith("IFD", StringComparison.Ordinal) && !item.Path.Contains('/')).Select(item => item.Path), Is.EqualTo(new string[] { "IFD0", "IFD1", "IFD2", "IFD3" }));
            Assert.That(((ifd0 ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(305) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("raw-tests"));
            Assert.That((ifd0.FindTag(315) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("Test Creator"));
            Assert.That((ifd0.FindTag(33432) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("Test Rights"));
            Assert.That(((exif ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(36867) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("2026:08:20 12:34:56"));
            Assert.That((exif.FindTag(36881) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("-07:00"));
            Assert.That((exif.FindTag(37521) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetText().Text, Is.EqualTo("123"));
            Assert.That(((ifd3 ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindTag(50752) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values(), Is.EqualTo(new ushort[] { 1, 4, 4 }));
            Assert.That(document.Metadata.MakerNotes, Has.Length.EqualTo(1));
            Assert.That(document.Thumbnails, Has.Length.EqualTo(3));
            Assert.That(document.Thumbnails.Any(item => item.EncodedBytes.SequenceEqual((input.PrimaryPreview ?? throw new System.InvalidOperationException("A required value is unavailable.")).EncodedBytes)), Is.True);
            Assert.That(document.Thumbnails.Any(item => item.EncodedBytes.SequenceEqual((input.Thumbnail ?? throw new System.InvalidOperationException("A required value is unavailable.")).EncodedBytes)), Is.True);
            Assert.That(document.Thumbnails.Any(item => item.EncodedBytes.SequenceEqual((input.SecondaryPreview ?? throw new System.InvalidOperationException("A required value is unavailable.")).EncodedBytes)), Is.True);
            Assert.That(decoded, Is.EqualTo(ContiguousSamples(input)));
        });
    }

    [Test]
    public void PublicEncoderRequiresNewContainerModeExactProfileAndExperimentalOptIn()
    {
        RawCr2WriteInput input = Input(8, 4, 8);
        RawWriteOptions wrongMode = Options();
        wrongMode.Mode = RawWriteMode.PreservingRewrite;
        RawWriteOptions blocked = Options();
        blocked.AllowExperimentalProfiles = false;
        blocked.ProfileName = RawBuiltInProfileCatalogue.Eos6DMarkIiWriteProfile;
        RawWriteOptions missing = Options();
        missing.ProfileName = "missing.write.profile";

        RawWriteException? mode = Assert.Throws<RawWriteException>(() => RawWriter.EncodeCr2(input, wrongMode));
        RawUnsupportedException? optIn = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeCr2(input, blocked));
        RawUnsupportedException? profile = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeCr2(input, missing));

        Assert.Multiple(() =>
        {
            Assert.That((mode ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((optIn ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [Test]
    public void EncoderRejectsMissingTelemetryInvalidSamplesAndMismatchedPreviewFrames()
    {
        RawWriteProfile profile = SmallProfile();
        RawWriteOptions options = Options();
        RawCr2WriteInput telemetry = Input(8, 4, 8);
        telemetry.CanonColorData = new ushort[10];
        RawCr2WriteInput samples = Input(8, 4, 8);
        samples.StoredSamples[0] = 16384;
        RawCr2WriteInput preview = Input(8, 4, 8);
        (preview.PrimaryPreview ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width = 7;

        RawWriteException? metadata = Assert.Throws<RawWriteException>(() => Cr2Encoder.Encode(telemetry, profile, options));
        RawWriteException? range = Assert.Throws<RawWriteException>(() => Cr2Encoder.Encode(samples, profile, options));
        RawWriteException? image = Assert.Throws<RawWriteException>(() => Cr2Encoder.Encode(preview, profile, options));

        Assert.Multiple(() =>
        {
            Assert.That((metadata ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((range ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((image ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        });
    }

    [Test]
    public void AdditionalProfileEncodesOnlyWithIndependentPassedStructuralAndApplicationEvidence()
    {
        RawCr2WriteInput input = Input(8, 4, 8);
        input.CanonModelId = 2147484679;
        RawWriteOptions options = Options();
        options.ProfileName = "test.canon.cr2.write";
        RawProfileCatalogue qualified = Catalogue(true, true);
        RawProfileCatalogue missingApplication = Catalogue(false, true);
        RawProfileCatalogue sameTool = Catalogue(true, false);

        byte[] encoded = RawWriter.EncodeCr2(input, qualified, options);
        RawUnsupportedException? failed = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeCr2(input, missingApplication, options));
        RawUnsupportedException? dependent = Assert.Throws<RawUnsupportedException>(() => RawWriter.EncodeCr2(input, sameTool, options));
        RawDocument document = RawCodec.Open(encoded, null, new RawOpenOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath, Is.EqualTo("IFD3"));
            Assert.That((document.Metadata.MakerNotes[0].FindCanonTag(0x0010) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt32Values()[0], Is.EqualTo(2147484679));
            Assert.That((failed ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That((dependent ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    private static RawProfileCatalogue Catalogue(bool applicationPassed, bool independentTools)
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateCanonEos6DMarkIi();
        read.Id = "test.canon.cr2.read";
        read.Identity.Model = "Canon Synthetic";
        read.Identity.CameraId = "2147484679";
        read.Fingerprint.Width = 8;
        read.Fingerprint.Height = 4;
        read.Geometry.StoredWidth = 8;
        read.Geometry.StoredHeight = 4;
        read.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        read.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        read.Codec.SliceWidths = new int[] { 4, 4 };
        read.Experimental = false;
        RawWriteProfile write = SmallProfile();
        write.ReadProfileId = read.Id;
        write.Experimental = !applicationPassed;
        RawProfileEvidence structural = Evidence(RawProfileEvidenceKind.Structural, "raw structural validator", RawProfileEvidenceResult.Passed);
        RawProfileEvidence application = Evidence(RawProfileEvidenceKind.Application, independentTools ? "Independent CR2 App" : "raw structural validator", applicationPassed ? RawProfileEvidenceResult.Passed : RawProfileEvidenceResult.Failed);
        write.Evidence = new RawProfileEvidence[] { structural, application };
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileRegistrationResult registration = catalogue.RegisterCustomDocument(document, false, new RawLimits());
        Assert.That(registration.Accepted, Is.True);
        return catalogue;
    }

    private static RawProfileEvidence Evidence(RawProfileEvidenceKind kind, string tool, RawProfileEvidenceResult result)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = kind;
        evidence.Tool = tool;
        evidence.Version = "1.0";
        evidence.SampleId = "generated-test-cr2";
        evidence.Check = kind == RawProfileEvidenceKind.Structural ? "parse-write-parse" : "application-open";
        evidence.Result = result;
        evidence.Details = result == RawProfileEvidenceResult.Passed ? "Passed." : "Not qualified.";
        return evidence;
    }

    internal static RawWriteProfile SmallProfile()
    {
        RawWriteProfile profile = RawBuiltInProfileCatalogue.CreateCanonEos6DMarkIiWrite();
        profile.Id = "test.canon.cr2.write";
        profile.Identity.Model = "Canon Synthetic";
        profile.Geometry.StoredWidth = 8;
        profile.Geometry.StoredHeight = 4;
        profile.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        profile.Fingerprint.Width = 8;
        profile.Fingerprint.Height = 4;
        profile.Codec.SliceWidths = new int[] { 4, 4 };
        return profile;
    }

    internal static RawWriteOptions Options()
    {
        RawWriteOptions options = new RawWriteOptions();
        options.Mode = RawWriteMode.NewContainer;
        options.AllowExperimentalProfiles = true;
        options.VerifyPixelIdentity = true;
        return options;
    }

    internal static RawCr2WriteInput Input(int width, int height, int stride)
    {
        RawCr2WriteInput input = new RawCr2WriteInput();
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
        input.PrimaryPreview = Preview(31);
        input.Thumbnail = Preview(97);
        input.SecondaryPreview = Preview(163);
        input.CanonCameraSettings = new short[47];
        input.CanonCameraSettings[3] = 4;
        input.CanonCameraSettings[46] = 0;
        input.CanonColorData = new ushort[1602];
        input.CanonColorData[0] = 15;
        input.CanonColorData[0x3f] = 2000;
        input.CanonColorData[0x40] = 1024;
        input.CanonColorData[0x41] = 1024;
        input.CanonColorData[0x42] = 1800;
        input.CanonColorData[0x30a] = 512;
        input.CanonColorData[0x30b] = 513;
        input.CanonColorData[0x30c] = 514;
        input.CanonColorData[0x30d] = 515;
        input.CanonColorData[0x30e] = 15000;
        input.CanonModelId = 2147484678;
        input.Metadata.Software = "raw-tests";
        input.Metadata.Artist = "Test Creator";
        input.Metadata.Copyright = "Test Rights";
        input.Metadata.OriginalTime = new RawExifTimestamp("2026:08:20 12:34:56", "-07:00", "123");
        return input;
    }

    private static RawThumbnail Preview(byte start)
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
            plane.Samples[index] = (byte)(start + index % 7);
            index++;
        }
        image.Planes.Add(plane);
        byte[] encoded = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
        return new RawThumbnail(0, RawThumbnailKind.Jpeg, 8, 8, RawOrientation.Normal, "image/jpeg", encoded, null, null);
    }

    private static ushort[] ContiguousSamples(RawCr2WriteInput input)
    {
        ushort[] result = new ushort[input.Width * input.Height];
        int y = 0;
        while (y < input.Height)
        {
            System.Array.Copy(input.StoredSamples, y * input.RowStrideSamples, result, y * input.Width, input.Width);
            y++;
        }
        return result;
    }
}
