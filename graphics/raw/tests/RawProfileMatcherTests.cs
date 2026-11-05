using raw;

namespace raw.Tests;

public class RawProfileMatcherTests
{
    [Test]
    public void NormalizedCanonicalAliasCameraIdAndFirmwareMatchingSelectsProfile()
    {
        RawReadProfile profile = CreateReadProfile("example.x100.read", "X-100", 100, false);
        profile.Identity.Aliases = new string[] { "Example X100", "EXAMPLE-CAMERA X 100" };
        profile.Identity.FirmwareMinimum = "1.2";
        profile.Identity.FirmwareMaximum = "1.11";
        RawProfileCatalogue catalogue = Catalogue(profile);
        RawProfileMatchInput input = CreateInput();
        input.Make = "  EXAMPLE camera, INC. ";
        input.Model = "Example X100";
        input.CameraId = "0x-42";
        input.Firmware = "1.10";

        RawProfileMatchResult result = catalogue.MatchReadProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(RawProfileMatcher.NormalizeIdentityText("  Éxample-Camera 42 "), Is.EqualTo("éxamplecamera42"));
            Assert.That(RawProfileMatcher.NormalizeIdentityText("E\u0301xample"), Is.EqualTo(RawProfileMatcher.NormalizeIdentityText("Éxample")));
            Assert.That(result.Matched, Is.True);
            Assert.That(result.Ambiguous, Is.False);
            Assert.That((result.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("example.x100.read"));
            Assert.That(result.WriteProfile, Is.Null);
            Assert.That(result.Candidates.Single().IdentityScore, Is.GreaterThan(200));
            Assert.That(result.Candidates.Single().FingerprintScore, Is.GreaterThan(0));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CombinedMakeAndModelAliasMatchesWithoutCanonicalMake()
    {
        RawReadProfile profile = CreateReadProfile("example.regional.read", "X-100", 100, false);
        profile.Identity.Aliases = new string[] { "Regional Camera X100" };
        RawProfileMatchInput input = CreateInput();
        input.Make = "REGIONAL-camera";
        input.Model = "X 100";
        input.CameraId = string.Empty;

        RawProfileMatchResult result = Catalogue(profile).MatchReadProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Matched, Is.True);
            Assert.That((result.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("example.regional.read"));
            Assert.That(result.Candidates.Single().IdentityScore, Is.EqualTo(150));
        });
    }

    [Test]
    public void StructuralFingerprintEvidenceBreaksIdentityTieAndContradictionsRejectCandidates()
    {
        RawReadProfile narrow = CreateReadProfile("camera.narrow", "Shared Camera", 100, false);
        RawReadProfile wide = CreateReadProfile("camera.wide", "Shared Camera", 200, false);
        RawProfileCatalogue catalogue = Catalogue(narrow, wide);
        RawProfileMatchInput noStructure = CreateInput();
        noStructure.Model = "Shared Camera";
        noStructure.Width = 0;
        noStructure.Height = 0;
        noStructure.BitDepth = 0;
        noStructure.Compression = 0;
        noStructure.SamplesPerPixel = 0;
        noStructure.HasByteOrder = false;
        noStructure.HasBigTiff = false;
        noStructure.IfdPaths = Array.Empty<string>();
        noStructure.Tags = Array.Empty<string>();
        noStructure.MakerNoteSignature = string.Empty;

        RawProfileMatchResult ambiguous = catalogue.MatchReadProfile(noStructure);
        RawProfileMatchInput observed = CreateInput();
        observed.Model = "Shared Camera";
        observed.Width = 200;
        RawProfileMatchResult selected = catalogue.MatchReadProfile(observed);
        observed.Tags = new string[] { "IFD0/271" };
        RawProfileMatchResult missingRequiredTag = catalogue.MatchReadProfile(observed);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Matched, Is.False);
            Assert.That(ambiguous.Ambiguous, Is.True);
            Assert.That(ambiguous.Candidates.Select(candidate => candidate.ProfileId), Is.EqualTo(new string[] { "camera.narrow", "camera.wide" }));
            Assert.That(ambiguous.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.AmbiguousProfile));
            Assert.That(selected.Matched, Is.True);
            Assert.That((selected.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("camera.wide"));
            Assert.That(selected.Candidates, Has.Length.EqualTo(1));
            Assert.That(missingRequiredTag.Matched, Is.False);
            Assert.That(missingRequiredTag.Candidates, Is.Empty);
            Assert.That(missingRequiredTag.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [Test]
    public void CompleteFingerprintContributesDeterministicScoreAndHashMismatchRejects()
    {
        RawReadProfile? profile = CreateReadProfile("camera.scored", "X-100", 100, false);
        RawProfileCatalogue catalogue = Catalogue(profile);
        RawProfileMatchInput input = CreateInput();
        input.StructuralHash = "ABCDEF0123";
        profile = catalogue.FindReadProfile("camera.scored");
        (profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Fingerprint.StructuralHash = "abcdef0123";

        RawProfileMatchResult match = catalogue.MatchReadProfile(input);
        input.StructuralHash = "different";
        RawProfileMatchResult mismatch = catalogue.MatchReadProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(match.Matched, Is.True);
            Assert.That(match.Candidates.Single().FingerprintScore, Is.GreaterThanOrEqualTo(100));
            Assert.That(match.Candidates.Single().Score, Is.EqualTo(match.Candidates.Single().IdentityScore + match.Candidates.Single().FingerprintScore));
            Assert.That(mismatch.Matched, Is.False);
            Assert.That(mismatch.Candidates, Is.Empty);
        });
    }

    [Test]
    public void ExperimentalReadProfilesRequireExplicitOptIn()
    {
        RawProfileCatalogue catalogue = Catalogue(CreateReadProfile("camera.experimental", "X-100", 100, true));
        RawProfileMatchInput input = CreateInput();

        RawProfileMatchResult blocked = catalogue.MatchReadProfile(input);
        input.AllowExperimental = true;
        RawProfileMatchResult enabled = catalogue.MatchReadProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Matched, Is.False);
            Assert.That(blocked.Candidates.Single().Experimental, Is.True);
            Assert.That(blocked.Candidates.Single().Eligible, Is.False);
            Assert.That(blocked.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
            Assert.That(blocked.Diagnostics.Single().Message, Does.Contain("explicit opt-in"));
            Assert.That(enabled.Matched, Is.True);
            Assert.That((enabled.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("camera.experimental"));
            Assert.That(enabled.Candidates.Single().Eligible, Is.True);
        });
    }

    [Test]
    public void ExperimentalWriteProfilesAreMatchedSeparatelyAndRequireOptIn()
    {
        RawReadProfile read = CreateReadProfile("camera.read", "X-100", 100, false);
        RawWriteProfile write = CreateWriteProfile("camera.write", read, true);
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { read };
        document.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        Assert.That(catalogue.RegisterCustomDocument(document, false, new RawLimits()).Accepted, Is.True);
        RawProfileMatchInput input = CreateInput();

        RawProfileMatchResult readResult = catalogue.MatchReadProfile(input);
        RawProfileMatchResult blockedWrite = catalogue.MatchWriteProfile(input);
        input.AllowExperimental = true;
        RawProfileMatchResult enabledWrite = catalogue.MatchWriteProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Matched, Is.True);
            Assert.That((readResult.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("camera.read"));
            Assert.That(blockedWrite.Matched, Is.False);
            Assert.That(blockedWrite.WriteProfile, Is.Null);
            Assert.That(enabledWrite.Matched, Is.True);
            Assert.That(enabledWrite.ReadProfile, Is.Null);
            Assert.That((enabledWrite.WriteProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("camera.write"));
        });
    }

    [Test]
    public void ReadProfileDoesNotImplyWriteCapability()
    {
        RawProfileCatalogue catalogue = Catalogue(CreateReadProfile("camera.read-only", "X-100", 100, false));
        RawProfileMatchInput input = CreateInput();

        RawProfileMatchResult read = catalogue.MatchReadProfile(input);
        RawProfileMatchResult write = catalogue.MatchWriteProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(read.Matched, Is.True);
            Assert.That((read.ReadProfile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Id, Is.EqualTo("camera.read-only"));
            Assert.That(write.Matched, Is.False);
            Assert.That(write.WriteProfile, Is.Null);
            Assert.That(write.Candidates, Is.Empty);
            Assert.That(write.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [TestCase("")]
    [TestCase("1.1")]
    [TestCase("1.12")]
    public void MissingOrOutOfRangeFirmwareRejectsBoundedProfile(string firmware)
    {
        RawReadProfile profile = CreateReadProfile("camera.firmware", "X-100", 100, false);
        profile.Identity.FirmwareMinimum = "1.2";
        profile.Identity.FirmwareMaximum = "1.11";
        RawProfileMatchInput input = CreateInput();
        input.Firmware = firmware;

        RawProfileMatchResult result = Catalogue(profile).MatchReadProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Matched, Is.False);
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.UnsupportedProfile));
        });
    }

    [Test]
    public void InvalidMatchInputsAreRejectedBeforeScoring()
    {
        RawProfileCatalogue catalogue = Catalogue(CreateReadProfile("camera.read", "X-100", 100, false));
        RawProfileMatchInput unknownFormat = CreateInput();
        unknownFormat.Format = RawFormat.Unknown;
        RawProfileMatchInput noIdentity = CreateInput();
        noIdentity.Make = string.Empty;
        noIdentity.Model = string.Empty;
        noIdentity.CameraId = string.Empty;
        RawProfileMatchInput negative = CreateInput();
        negative.Width = -1;
        RawProfileMatchInput contradictoryMake = CreateInput();
        contradictoryMake.Make = "Different Manufacturer";

        Assert.Multiple(() =>
        {
            Assert.Throws<RawProfileException>(() => catalogue.MatchReadProfile(null));
            Assert.Throws<RawProfileException>(() => catalogue.MatchReadProfile(unknownFormat));
            Assert.Throws<RawProfileException>(() => catalogue.MatchReadProfile(noIdentity));
            Assert.Throws<RawProfileException>(() => catalogue.MatchReadProfile(negative));
            Assert.That(catalogue.MatchReadProfile(contradictoryMake).Matched, Is.False);
        });
    }

    static RawProfileCatalogue Catalogue(params RawReadProfile[] profiles)
    {
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = profiles;
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        Assert.That(catalogue.RegisterCustomDocument(document, false, new RawLimits()).Accepted, Is.True);
        return catalogue;
    }

    static RawReadProfile CreateReadProfile(string id, string model, int width, bool experimental)
    {
        RawReadProfile profile = new RawReadProfile();
        profile.Id = id;
        profile.Identity = CreateIdentity(model);
        profile.Fingerprint = CreateFingerprint(width);
        profile.Geometry = CreateGeometry(width);
        profile.Codec.Name = "test-codec";
        profile.Codec.StoredWordBits = 12;
        profile.Provenance = CreateProvenance();
        profile.Experimental = experimental;
        return profile;
    }

    static RawWriteProfile CreateWriteProfile(string id, RawReadProfile read, bool experimental)
    {
        RawWriteProfile profile = new RawWriteProfile();
        profile.Id = id;
        profile.ReadProfileId = read.Id;
        profile.Identity = CreateIdentity(read.Identity.Model);
        profile.Fingerprint = CreateFingerprint(read.Fingerprint.Width);
        profile.Geometry = CreateGeometry(read.Fingerprint.Width);
        profile.Codec.Name = "test-writer";
        profile.Codec.StoredWordBits = 12;
        profile.Provenance = CreateProvenance();
        profile.Experimental = experimental;
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Application;
        evidence.Tool = "test-tool";
        evidence.Version = "1";
        evidence.SampleId = "sample";
        evidence.Check = "open";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = string.Empty;
        profile.Evidence = new RawProfileEvidence[] { evidence };
        return profile;
    }

    static RawProfileIdentity CreateIdentity(string? model)
    {
        RawProfileIdentity identity = new RawProfileIdentity();
        identity.Format = RawFormat.Cr2;
        identity.Make = "Example Camera, Inc.";
        identity.Model = model;
        identity.CameraId = "0X42";
        identity.Aliases = new string[] { "Example Camera " + model };
        return identity;
    }

    static RawProfileFingerprint CreateFingerprint(int width)
    {
        RawProfileFingerprint fingerprint = new RawProfileFingerprint();
        fingerprint.ByteOrder = RawByteOrder.LittleEndian;
        fingerprint.BigTiff = false;
        fingerprint.Width = width;
        fingerprint.Height = 80;
        fingerprint.BitDepth = 12;
        fingerprint.Compression = 6;
        fingerprint.SamplesPerPixel = 1;
        fingerprint.RequiredIfds = new string[] { "IFD0", "IFD3" };
        fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD3/273" };
        fingerprint.MakerNoteSignature = "CanonIfd";
        fingerprint.StructuralHash = string.Empty;
        return fingerprint;
    }

    static RawProfileGeometry CreateGeometry(int width)
    {
        RawProfileGeometry geometry = new RawProfileGeometry();
        geometry.StoredWidth = width;
        geometry.StoredHeight = 80;
        geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, width, 80);
        geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        return geometry;
    }

    static RawProfileProvenance[] CreateProvenance()
    {
        RawProfileProvenance provenance = new RawProfileProvenance();
        provenance.Field = "identity";
        provenance.Source = "sample";
        provenance.Kind = RawProfileProvenanceKind.Sample;
        provenance.License = string.Empty;
        provenance.Retrieved = string.Empty;
        provenance.Notes = string.Empty;
        return new RawProfileProvenance[] { provenance };
    }

    static RawProfileMatchInput CreateInput()
    {
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = RawFormat.Cr2;
        input.Make = "Example Camera Inc";
        input.Model = "X-100";
        input.CameraId = "0x42";
        input.Firmware = string.Empty;
        input.HasByteOrder = true;
        input.ByteOrder = RawByteOrder.LittleEndian;
        input.HasBigTiff = true;
        input.BigTiff = false;
        input.Width = 100;
        input.Height = 80;
        input.BitDepth = 12;
        input.Compression = 6;
        input.SamplesPerPixel = 1;
        input.IfdPaths = new string[] { "IFD0", "IFD3", "IFD0/Exif" };
        input.Tags = new string[] { "IFD0/271", "IFD3/273", "IFD0/34665" };
        input.MakerNoteSignature = "canon-ifd";
        input.StructuralHash = string.Empty;
        return input;
    }
}
