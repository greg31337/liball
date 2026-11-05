using raw;

namespace raw.Tests;

public class Cr2WriterCompatibilityTests
{
    [TestCase(12, 11, 0)]
    [TestCase(14, 10, 1)]
    [TestCase(16, 12, 2)]
    public void QualifiedWriterProfilesRoundTripEveryStoredPixelExactly(int precision, int stride, int sliceLayout)
    {
        int[] slices = Slices(sliceLayout);
        RawProfileCatalogue catalogue = Catalogue(precision, slices, true);
        RawCr2WriteInput input = Cr2EncoderTests.Input(8, 4, stride);
        FillBoundarySamples(input, precision);
        RawWriteOptions options = new RawWriteOptions();
        options.Mode = RawWriteMode.NewContainer;
        options.ProfileName = ProfileId(precision);
        options.VerifyPixelIdentity = true;

        byte[] first = RawWriter.EncodeCr2(input, catalogue, options);
        byte[] second = RawWriter.EncodeCr2(input, catalogue, options);
        RawDocument document = RawCodec.Open(first, null, OpenOptions());
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
        LosslessJpegImage jpeg = LosslessJpegCodec.Decode(document.ExtractPayload(rawImage.Payload), new RawLimits(), CancellationToken.None);
        ushort[] decoded = Cr2LosslessJpegDecoder.DecodeSamples(jpeg, input.Width, input.Height, slices, 4, precision, 1, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(decoded, Is.EqualTo(Contiguous(input)));
            Assert.That(((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath, Is.EqualTo("IFD3"));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.RawImage), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void MatrixListsExactApplicationVersionsAndExplicitlyShowsUnconfirmedExperimentalProfiles()
    {
        RawProfileCatalogue builtIn = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawWriteCompatibilityMatrix unconfirmed = builtIn.GetWriteCompatibilityMatrix(RawFormat.Cr2);
        RawProfileCatalogue qualifiedCatalogue = Catalogue(14, new int[] { 3, 5 }, true);
        RawWriteCompatibilityMatrix qualified = qualifiedCatalogue.GetWriteCompatibilityMatrix(RawFormat.Cr2);
        RawWriteCompatibilityEntry passed = qualified.Entries.Single(item => item.Result == RawProfileEvidenceResult.Passed);
        RawWriteCompatibilityEntry failed = qualified.Entries.Single(item => item.Result == RawProfileEvidenceResult.Failed);

        Assert.Multiple(() =>
        {
            Assert.That(unconfirmed.Entries, Has.Length.EqualTo(1));
            Assert.That(unconfirmed.Entries[0].Application, Is.Empty);
            Assert.That(unconfirmed.Entries[0].Version, Is.Empty);
            Assert.That(unconfirmed.Entries[0].Result, Is.EqualTo(RawProfileEvidenceResult.Unknown));
            Assert.That(unconfirmed.Entries[0].Experimental, Is.True);
            Assert.That(unconfirmed.HasConfirmedApplication(RawBuiltInProfileCatalogue.Eos6DMarkIiWriteProfile), Is.False);
            Assert.That(qualified.Entries, Has.Length.EqualTo(2));
            Assert.That(passed.Application, Is.EqualTo("Independent CR2 Viewer"));
            Assert.That(passed.Version, Is.EqualTo("7.4.1"));
            Assert.That(passed.Experimental, Is.False);
            Assert.That(failed.Application, Is.EqualTo("Other CR2 Viewer"));
            Assert.That(failed.Version, Is.EqualTo("2.0"));
            Assert.That(qualified.HasConfirmedApplication(ProfileId(14)), Is.True);
            Assert.That(qualified.IsConfirmed(ProfileId(14), " independent cr2 viewer ", "7.4.1"), Is.True);
            Assert.That(qualified.IsConfirmed(ProfileId(14), "Independent CR2 Viewer", "7.4.0"), Is.False);
            Assert.That(qualified.FindProfile(ProfileId(14)), Has.Length.EqualTo(2));
        });
    }

    private static RawProfileCatalogue Catalogue(int precision, int[] slices, bool includeFailedApplication)
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateCanonEos6DMarkIi();
        read.Id = "test.cr2.read." + precision.ToString(System.Globalization.CultureInfo.InvariantCulture);
        read.Identity.Model = "Synthetic Exact " + precision.ToString(System.Globalization.CultureInfo.InvariantCulture);
        read.Fingerprint.Width = 8;
        read.Fingerprint.Height = 4;
        read.Fingerprint.BitDepth = precision;
        read.Geometry.StoredWidth = 8;
        read.Geometry.StoredHeight = 4;
        read.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        read.Codec.StoredWordBits = precision;
        read.Codec.SliceWidths = slices;
        read.Experimental = false;

        RawWriteProfile write = Cr2EncoderTests.SmallProfile();
        write.Id = ProfileId(precision);
        write.ReadProfileId = read.Id;
        write.Identity.Model = read.Identity.Model;
        write.Fingerprint.BitDepth = precision;
        write.Codec.StoredWordBits = precision;
        write.Codec.SliceWidths = slices;
        write.Experimental = false;
        RawProfileEvidence structural = Evidence(RawProfileEvidenceKind.Structural, "raw structural validator", "schema-1", RawProfileEvidenceResult.Passed);
        RawProfileEvidence pixels = Evidence(RawProfileEvidenceKind.PixelIdentity, "raw pixel verifier", "decoder-1", RawProfileEvidenceResult.Passed);
        RawProfileEvidence application = Evidence(RawProfileEvidenceKind.Application, "Independent CR2 Viewer", "7.4.1", RawProfileEvidenceResult.Passed);
        if (includeFailedApplication)
        {
            RawProfileEvidence failed = Evidence(RawProfileEvidenceKind.Application, "Other CR2 Viewer", "2.0", RawProfileEvidenceResult.Failed);
            write.Evidence = new RawProfileEvidence[] { structural, pixels, application, failed };
        }
        else
        {
            write.Evidence = new RawProfileEvidence[] { structural, pixels, application };
        }

        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        Assert.That(catalogue.RegisterCustomDocument(document, false, new RawLimits()).Accepted, Is.True);
        return catalogue;
    }

    private static RawProfileEvidence Evidence(RawProfileEvidenceKind kind, string tool, string version, RawProfileEvidenceResult result)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = kind;
        evidence.Tool = tool;
        evidence.Version = version;
        evidence.SampleId = "sha256:generated-cr2-" + kind.ToString();
        evidence.Check = kind == RawProfileEvidenceKind.Application ? "open-render-metadata" : kind == RawProfileEvidenceKind.PixelIdentity ? "decode-compare" : "parse-write-parse";
        evidence.Result = result;
        evidence.Details = result == RawProfileEvidenceResult.Passed ? "Passed." : "Rejected by the application.";
        return evidence;
    }

    private static string ProfileId(int precision)
    {
        return "test.cr2.write." + precision.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int[] Slices(int layout)
    {
        if (layout == 0) return new int[] { 8 };
        if (layout == 1) return new int[] { 3, 5 };
        return new int[] { 2, 2, 4 };
    }

    private static void FillBoundarySamples(RawCr2WriteInput input, int precision)
    {
        ushort maximum = precision == 16 ? ushort.MaxValue : (ushort)((1 << precision) - 1);
        int y = 0;
        while (y < input.Height)
        {
            int x = 0;
            while (x < input.Width)
            {
                int selector = (y * input.Width + x) % 6;
                ushort value = selector == 0 ? (ushort)0 : selector == 1 ? maximum : selector == 2 ? (ushort)1 : selector == 3 ? (ushort)(maximum - 1) : selector == 4 ? (ushort)(maximum / 2) : (ushort)(17 + x + y);
                input.StoredSamples[y * input.RowStrideSamples + x] = value;
                x++;
            }
            y++;
        }
    }

    private static ushort[] Contiguous(RawCr2WriteInput input)
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

    private static RawOpenOptions OpenOptions()
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        return options;
    }
}
