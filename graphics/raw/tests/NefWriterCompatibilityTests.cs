using raw;

namespace raw.Tests;

public class NefWriterCompatibilityTests
{
    [TestCase(RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile)]
    [TestCase(RawBuiltInProfileCatalogue.NikonZ8WriteProfile)]
    public void BuiltInWriterModesDeterministicallyRoundTripBoundaryPixelsExactly(string profileId)
    {
        RawWriteProfile profile = RawBuiltInProfileCatalogue.CreateDocument().WriteProfiles.Single(item => item.Id == profileId);
        ConfigureSmallProfile(profile);
        RawNefWriteInput input = NefEncoderTests.ExtremeInput(8, 4, 11);
        RawWriteOptions options = NefEncoderTests.Options();

        byte[] first = NefEncoder.Encode(input, profile, options);
        byte[] second = NefEncoder.Encode(input, profile, options);
        RawDocument document = RawCodec.Open(first, null, OpenOptions());
        ushort[] decoded = Decode(document, input, profile);
        RawProfileEvidence pixels = profile.Evidence.Single(item => item.Kind == RawProfileEvidenceKind.PixelIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(decoded, Is.EqualTo(NefEncoderTests.ContiguousSamples(input)));
            Assert.That(pixels.Result, Is.EqualTo(RawProfileEvidenceResult.Passed));
            Assert.That(pixels.Check, Is.EqualTo("decode-compare-stored-pixel-identity"));
            Assert.That(profile.Experimental, Is.True);
            Assert.That(profile.Evidence.Any(item => item.Kind == RawProfileEvidenceKind.Application), Is.False);
        });
    }

    [Test]
    public void QualifiedLossyWriterHonorsItsErrorBoundAcrossEveryFourteenBitValue()
    {
        const int width = 16384;
        const int height = 4;
        RawProfileCatalogue catalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawWriteProfile? profile = catalogue.FindWriteProfile("test.nikon.nef.lossy.write");
        RawReadProfile? read = catalogue.FindReadProfile((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).ReadProfileId);
        ConfigureGeometry((read ?? throw new System.InvalidOperationException("A required value is unavailable.")).Fingerprint, read.Geometry, read.Codec, width, height);
        ConfigureGeometry(profile.Fingerprint, profile.Geometry, profile.Codec, width, height);
        RawNefWriteInput input = NefEncoderTests.Input(width, height, width + 3);
        FillEveryFourteenBitValue(input);
        RawWriteOptions options = NefEncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.AllowLossy = true;
        options.DataLossConsent = true;
        options.ProfileName = profile.Id;

        byte[] encoded = RawWriter.EncodeNef(input, catalogue, options);
        RawDocument document = RawCodec.Open(encoded, null, OpenOptions());
        ushort[] decoded = Decode(document, input, profile);
        ushort[] source = NefEncoderTests.ContiguousSamples(input);
        int maximumError = 0;
        int valuesAtBound = 0;
        int index = 0;
        while (index < source.Length)
        {
            int error = Math.Abs(decoded[index] - source[index]);
            if (error > maximumError)
            {
                maximumError = error;
            }
            if (error == NikonCompressionCodec.LossyMaximumError)
            {
                valuesAtBound++;
            }
            index++;
        }
        RawProfileEvidence errorBound = profile.Evidence.Single(item => item.Kind == RawProfileEvidenceKind.ErrorBound);

        Assert.Multiple(() =>
        {
            Assert.That(maximumError, Is.EqualTo(NikonCompressionCodec.LossyMaximumError));
            Assert.That(valuesAtBound, Is.EqualTo(source.Length / 2 - height));
            Assert.That(errorBound.Result, Is.EqualTo(RawProfileEvidenceResult.Passed));
            Assert.That(errorBound.Check, Is.EqualTo("decode-maximum-absolute-error-1"));
        });
    }

    [Test]
    public void MatrixListsExactApplicationVersionsAndKeepsUnconfirmedProfilesExperimental()
    {
        RawProfileCatalogue builtIn = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        RawWriteCompatibilityMatrix unconfirmed = builtIn.GetWriteCompatibilityMatrix(RawFormat.Nef);
        RawProfileCatalogue qualifiedCatalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawWriteProfile? qualifiedProfile = qualifiedCatalogue.FindWriteProfile("test.nikon.nef.lossy.write");
        RawProfileEvidence failed = ApplicationEvidence("Other NEF Viewer", "2.5", RawProfileEvidenceResult.Failed);
        (qualifiedProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Evidence = qualifiedProfile.Evidence.Concat(new RawProfileEvidence[] { failed }).ToArray();
        RawWriteCompatibilityMatrix qualified = qualifiedCatalogue.GetWriteCompatibilityMatrix(RawFormat.Nef);
        RawWriteCompatibilityEntry passed = qualified.Entries.Single(item => item.Result == RawProfileEvidenceResult.Passed);
        RawWriteCompatibilityEntry rejected = qualified.Entries.Single(item => item.Result == RawProfileEvidenceResult.Failed);

        Assert.Multiple(() =>
        {
            Assert.That(unconfirmed.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(unconfirmed.Entries, Has.Length.EqualTo(2));
            Assert.That(unconfirmed.Entries.All(item => item.Application.Length == 0 && item.Version.Length == 0), Is.True);
            Assert.That(unconfirmed.Entries.All(item => item.Result == RawProfileEvidenceResult.Unknown), Is.True);
            Assert.That(unconfirmed.Entries.All(item => item.Experimental), Is.True);
            Assert.That(unconfirmed.HasConfirmedApplication(RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile), Is.False);
            Assert.That(unconfirmed.HasConfirmedApplication(RawBuiltInProfileCatalogue.NikonZ8WriteProfile), Is.False);
            Assert.That(((builtIn.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable.")).Where(item => item.Identity.Format == RawFormat.Nef).All(item => item.Experimental)), Is.True);
            Assert.That(qualified.Entries, Has.Length.EqualTo(2));
            Assert.That(passed.Application, Is.EqualTo("Independent NEF Application"));
            Assert.That(passed.Version, Is.EqualTo("1.0"));
            Assert.That(passed.Experimental, Is.False);
            Assert.That(rejected.Application, Is.EqualTo("Other NEF Viewer"));
            Assert.That(rejected.Version, Is.EqualTo("2.5"));
            Assert.That(qualified.HasConfirmedApplication(qualifiedProfile.Id), Is.True);
            Assert.That(qualified.IsConfirmed(qualifiedProfile.Id, " independent nef application ", "1.0"), Is.True);
            Assert.That(qualified.IsConfirmed(qualifiedProfile.Id, "Independent NEF Application", "1.1"), Is.False);
            Assert.That(qualified.FindProfile(qualifiedProfile.Id), Has.Length.EqualTo(2));
        });
    }

    private static void ConfigureSmallProfile(RawWriteProfile profile)
    {
        profile.Id = "test.nef.pixel." + profile.Codec.Name;
        profile.ReadProfileId = "test.nef.pixel.read." + profile.Codec.Name;
        profile.Identity.Model = "NIKON TEST PIXEL " + profile.Codec.Name;
        profile.Identity.Aliases = new string[] { profile.Identity.Model };
        ConfigureGeometry(profile.Fingerprint, profile.Geometry, profile.Codec, 8, 4);
    }

    private static void ConfigureGeometry(RawProfileFingerprint fingerprint, RawProfileGeometry geometry, RawProfileCodec codec, int width, int height)
    {
        fingerprint.Width = width;
        fingerprint.Height = height;
        geometry.StoredWidth = width;
        geometry.StoredHeight = height;
        geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, width, height);
        codec.SliceWidths = new int[] { width };
    }

    private static void FillEveryFourteenBitValue(RawNefWriteInput input)
    {
        int y = 0;
        while (y < input.Height)
        {
            int x = 0;
            while (x < input.Width)
            {
                input.StoredSamples[y * input.RowStrideSamples + x] = (ushort)((x + y * 4093) & 16383);
                x++;
            }
            y++;
        }
    }

    private static ushort[] Decode(RawDocument document, RawNefWriteInput input, RawWriteProfile profile)
    {
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
        byte[] raw = document.ExtractPayload(rawImage.Payload);
        if (profile.Codec.Name == NefUncompressedDecoder.CodecName)
        {
            return NefEncoderTests.DecodeRows(raw, input.Width, input.Height);
        }
        RawMakerNote note = document.Metadata.MakerNotes.Single();
        RawNikonMakerNoteTag? compressionTag = note.FindNikonTag(0x0096);
        NikonCompressionInfo compression = NikonCompressionCodec.ParseInfo((compressionTag ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), note.ByteOrder, 14, input.Height);
        return NefEncoderTests.DecodeLossless(raw, input.Width, input.Height, compression);
    }

    private static RawProfileEvidence ApplicationEvidence(string application, string version, RawProfileEvidenceResult result)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Application;
        evidence.Tool = application;
        evidence.Version = version;
        evidence.SampleId = "generated-test-nef";
        evidence.Check = "application-open";
        evidence.Result = result;
        evidence.Details = result == RawProfileEvidenceResult.Passed ? "Passed." : "Rejected by the application.";
        return evidence;
    }

    private static RawOpenOptions OpenOptions()
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        return options;
    }
}
