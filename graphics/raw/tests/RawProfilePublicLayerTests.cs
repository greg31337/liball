using raw;

namespace raw.Tests;

public class RawProfilePublicLayerTests
{
    [Test]
    public void ReviewedFactoryExposesProfileDetailsMatchingAndCompatibilityThroughPublicContracts()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        RawProfileCatalogue catalogue = codec.CreateBuiltInProfileCatalogue(limits);
        IRawProfileCatalogue profiles = catalogue;
        IRawProfileRegistry registry = catalogue;
        RawReadProfile read = profiles.GetReadProfiles()[0];
        RawWriteProfile write = profiles.GetWriteProfiles()[0];
        RawProfileMatchInput input = CreateMatchInput(read);

        RawProfileMatchResult match = profiles.MatchReadProfile(input);
        RawWriteCompatibilityMatrix compatibility = profiles.GetWriteCompatibilityMatrix(write.Identity.Format);
        RawWriteCompatibilityEntry[] entries = compatibility.Entries.Where(item => item.ProfileId == write.Id).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(profiles.GetReadProfiles(), Is.Not.Empty);
            Assert.That(profiles.GetWriteProfiles(), Is.Not.Empty);
            Assert.That(registry.GetReadProfileSources(), Is.All.EqualTo(RawProfileRegistrationSource.BuiltIn));
            Assert.That(registry.GetWriteProfileSources(), Is.All.EqualTo(RawProfileRegistrationSource.BuiltIn));
            Assert.That(read.Provenance, Is.Not.Empty);
            Assert.That(write.Provenance, Is.Not.Empty);
            Assert.That(write.Evidence, Is.Not.Empty);
            Assert.That(write.Experimental, Is.True);
            Assert.That(match.Matched, Is.True);
            Assert.That(match.Ambiguous, Is.False);
            Assert.That(match.ReadProfile, Is.Not.Null);
            Assert.That((match.ReadProfile ?? throw new InvalidOperationException()).Id, Is.EqualTo(read.Id));
            Assert.That(match.Candidates.Any(item => item.ProfileId == read.Id && item.Eligible), Is.True);
            Assert.That(compatibility.Format, Is.EqualTo(write.Identity.Format));
            Assert.That(entries, Is.Not.Empty);
            Assert.That(entries, Is.All.Matches<RawWriteCompatibilityEntry>(item => item.Experimental == write.Experimental));
        });
    }

    [Test]
    public void PublicProfileJsonIsDeterministicValidatedAndUsedForCustomOverridesWithBothSourcesReported()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        RawProfileCatalogue catalogue = codec.CreateBuiltInProfileCatalogue(limits);
        IRawProfileCatalogue profiles = catalogue;
        IRawProfileRegistry registry = catalogue;
        RawWriteProfile originalWrite = profiles.GetWriteProfiles()[0];
        RawReadProfile originalRead = profiles.FindReadProfile(originalWrite.ReadProfileId) ?? throw new InvalidOperationException();
        RawProfileSchemaDocument source = new RawProfileSchemaDocument();
        source.ReadProfiles = new RawReadProfile[] { originalRead };
        source.WriteProfiles = new RawWriteProfile[] { originalWrite };

        byte[] firstJson = codec.WriteProfileDocument(source, limits);
        RawProfileSchemaDocument custom = codec.ReadProfileDocument(firstJson, limits);
        byte[] secondJson = codec.WriteProfileDocument(custom, limits);
        custom.ReadProfiles[0].Identity.Model = "Custom " + custom.ReadProfiles[0].Identity.Model;
        custom.WriteProfiles[0].Identity.Model = "Custom " + custom.WriteProfiles[0].Identity.Model;
        RawValidationReport validation = codec.ValidateProfileDocument(custom, limits);
        RawProfileRegistrationResult registration = registry.RegisterCustomDocument(custom, false, limits);
        RawReadProfile[] currentReads = profiles.GetReadProfiles();
        RawWriteProfile[] currentWrites = profiles.GetWriteProfiles();
        int readIndex = Array.FindIndex(currentReads, item => item.Id == originalRead.Id);
        int writeIndex = Array.FindIndex(currentWrites, item => item.Id == originalWrite.Id);
        RawProfileRegistrationSource[] readSources = registry.GetReadProfileSources();
        RawProfileRegistrationSource[] writeSources = registry.GetWriteProfileSources();
        readSources[readIndex] = RawProfileRegistrationSource.Unknown;
        writeSources[writeIndex] = RawProfileRegistrationSource.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(secondJson, Is.EqualTo(firstJson));
            Assert.That((validation.Messages ?? Array.Empty<RawValidationMessage>()).Any(item => item.Severity == RawSeverity.Error), Is.False);
            Assert.That(registration.Accepted, Is.True);
            Assert.That(registration.ReplacedReadProfiles.Single().Id, Is.EqualTo(originalRead.Id));
            Assert.That(registration.ReplacedWriteProfiles.Single().Id, Is.EqualTo(originalWrite.Id));
            Assert.That((profiles.FindReadProfile(originalRead.Id) ?? throw new InvalidOperationException()).Identity.Model, Does.StartWith("Custom "));
            Assert.That((profiles.FindWriteProfile(originalWrite.Id) ?? throw new InvalidOperationException()).Identity.Model, Does.StartWith("Custom "));
            Assert.That(registry.GetReadProfileSources()[readIndex], Is.EqualTo(RawProfileRegistrationSource.Custom));
            Assert.That(registry.GetWriteProfileSources()[writeIndex], Is.EqualTo(RawProfileRegistrationSource.Custom));
        });

        custom.SchemaVersion = 99;
        Assert.Multiple(() =>
        {
            Assert.That((codec.ValidateProfileDocument(custom, limits).Messages ?? Array.Empty<RawValidationMessage>()).Any(item => item.Severity == RawSeverity.Error), Is.True);
            Assert.Throws<RawWriteException>(() => codec.WriteProfileDocument(custom, limits));
        });
    }

    [Test]
    public void PublicRegistrationAndMatchingReportAmbiguousCustomProfiles()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        RawProfileCatalogue builtIn = codec.CreateBuiltInProfileCatalogue(limits);
        RawReadProfile template = builtIn.GetReadProfiles()[0];
        RawProfileSchemaDocument one = new RawProfileSchemaDocument();
        one.ReadProfiles = new RawReadProfile[] { template };
        RawReadProfile first = codec.ReadProfileDocument(codec.WriteProfileDocument(one, limits), limits).ReadProfiles[0];
        RawReadProfile second = codec.ReadProfileDocument(codec.WriteProfileDocument(one, limits), limits).ReadProfiles[0];
        first.Id = "custom.ambiguous.first";
        second.Id = "custom.ambiguous.second";
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        document.ReadProfiles = new RawReadProfile[] { first, second };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        IRawProfileCatalogue profiles = catalogue;
        IRawProfileRegistry registry = catalogue;

        RawProfileRegistrationResult registration = registry.RegisterCustomDocument(document, false, limits);
        RawProfileMatchResult match = profiles.MatchReadProfile(CreateMatchInput(first));

        Assert.Multiple(() =>
        {
            Assert.That(registration.Accepted, Is.True);
            Assert.That(registration.Diagnostics.Any(item => item.Code == RawDiagnosticCode.AmbiguousProfile), Is.True);
            Assert.That(registration.AddedReadProfiles.Select(item => item.Id), Is.EqualTo(new string[] { first.Id, second.Id }));
            Assert.That(registry.GetReadProfileSources(), Is.All.EqualTo(RawProfileRegistrationSource.Custom));
            Assert.That(match.Matched, Is.False);
            Assert.That(match.Ambiguous, Is.True);
            Assert.That(match.ReadProfile, Is.Null);
            Assert.That(match.Candidates.Select(item => item.ProfileId), Is.EqualTo(new string[] { first.Id, second.Id }));
            Assert.That(match.Diagnostics.Any(item => item.Code == RawDiagnosticCode.AmbiguousProfile), Is.True);
        });
    }

    static RawProfileMatchInput CreateMatchInput(RawReadProfile profile)
    {
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = profile.Identity.Format;
        input.Make = profile.Identity.Make;
        input.Model = profile.Identity.Model ?? string.Empty;
        input.CameraId = profile.Identity.CameraId;
        input.Firmware = profile.Identity.FirmwareMinimum;
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
        input.StructuralHash = profile.Fingerprint.StructuralHash;
        input.AllowExperimental = true;
        return input;
    }
}
