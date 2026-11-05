using raw;

namespace raw.Tests;

public class RawProfileCatalogueTests
{
    [Test]
    public void BuiltInJsonLoadsValidatedReadAndWriteProfilesWithSourceTracking()
    {
        RawProfileSchemaDocument builtIn = CreateDocument("camera.read", "camera.write", "Camera A", "builtin-codec");
        byte[] json = RawProfileSchemaCodec.Write(builtIn, new RawLimits());

        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateFromBuiltInJson(json, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(1));
            Assert.That(catalogue.WriteProfiles, Has.Length.EqualTo(1));
            Assert.That(catalogue.ReadProfileSources, Is.EqualTo(new RawProfileRegistrationSource[] { RawProfileRegistrationSource.BuiltIn }));
            Assert.That(catalogue.WriteProfileSources, Is.EqualTo(new RawProfileRegistrationSource[] { RawProfileRegistrationSource.BuiltIn }));
            Assert.That((catalogue.FindReadProfile("camera.read") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Identity.Model, Is.EqualTo("Camera A"));
            Assert.That((catalogue.FindWriteProfile("camera.write") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Codec.Name, Is.EqualTo("builtin-codec"));
            Assert.That(catalogue.FindReadProfile("missing"), Is.Null);
            Assert.That(catalogue.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CustomProfilesAutomaticallyOverrideBuiltInsAndKeepStablePositions()
    {
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateFromBuiltInJson(RawProfileSchemaCodec.Write(CreateDocument("camera.read", "camera.write", "Camera A", "builtin-codec"), new RawLimits()), new RawLimits());
        RawProfileSchemaDocument custom = CreateDocument("camera.read", "camera.write", "Camera A Custom", "custom-codec");

        RawProfileRegistrationResult result = catalogue.RegisterCustom(RawProfileSchemaCodec.Write(custom, new RawLimits()), false, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.AddedReadProfiles, Is.Empty);
            Assert.That(result.AddedWriteProfiles, Is.Empty);
            Assert.That(result.ReplacedReadProfiles.Single().Identity.Model, Is.EqualTo("Camera A"));
            Assert.That(result.ReplacedWriteProfiles.Single().Codec.Name, Is.EqualTo("builtin-codec"));
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(1));
            Assert.That(catalogue.WriteProfiles, Has.Length.EqualTo(1));
            Assert.That((catalogue.ReadProfiles ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Identity.Model, Is.EqualTo("Camera A Custom"));
            Assert.That((catalogue.WriteProfiles ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Codec.Name, Is.EqualTo("custom-codec"));
            Assert.That((catalogue.ReadProfileSources ?? throw new System.InvalidOperationException("A required value is unavailable."))[0], Is.EqualTo(RawProfileRegistrationSource.Custom));
            Assert.That((catalogue.WriteProfileSources ?? throw new System.InvalidOperationException("A required value is unavailable."))[0], Is.EqualTo(RawProfileRegistrationSource.Custom));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Severity == RawSeverity.Information), Is.True);
        });
    }

    [Test]
    public void RepeatedCustomIdentifiersRequirePermissionAndRejectedRegistrationsAreAtomic()
    {
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileSchemaDocument first = CreateDocument("camera.read", "camera.write", "First", "first-codec");
        RawProfileSchemaDocument second = CreateDocument("camera.read", "camera.write", "Second", "second-codec");
        Assert.That(catalogue.RegisterCustomDocument(first, false, new RawLimits()).Accepted, Is.True);

        RawProfileRegistrationResult rejected = catalogue.RegisterCustomDocument(second, false, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Diagnostics.Any(diagnostic => diagnostic.Severity == RawSeverity.Error), Is.True);
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(1));
            Assert.That(catalogue.WriteProfiles, Has.Length.EqualTo(1));
            Assert.That((catalogue.FindReadProfile("camera.read") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Identity.Model, Is.EqualTo("First"));
            Assert.That((catalogue.FindWriteProfile("camera.write") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Codec.Name, Is.EqualTo("first-codec"));
        });

        RawProfileRegistrationResult replaced = catalogue.RegisterCustomDocument(second, true, new RawLimits());
        Assert.Multiple(() =>
        {
            Assert.That(replaced.Accepted, Is.True);
            Assert.That((catalogue.FindReadProfile("camera.read") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Identity.Model, Is.EqualTo("Second"));
            Assert.That((catalogue.FindWriteProfile("camera.write") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Codec.Name, Is.EqualTo("second-codec"));
        });
    }

    [Test]
    public void RuntimeRegistrationCopiesCallerModelsAndAddsNewProfiles()
    {
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileSchemaDocument runtime = CreateReadOnlyDocument("runtime.read", "Runtime Camera", "runtime-codec");

        RawProfileRegistrationResult result = catalogue.RegisterCustomDocument(runtime, false, new RawLimits());
        runtime.ReadProfiles[0].Identity.Model = "Mutated After Registration";
        runtime.ReadProfiles[0].Codec.Quirks[0] = "mutated-quirk";

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.AddedReadProfiles, Has.Length.EqualTo(1));
            Assert.That(result.ReplacedReadProfiles, Is.Empty);
            Assert.That((catalogue.FindReadProfile("runtime.read") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Identity.Model, Is.EqualTo("Runtime Camera"));
            Assert.That((catalogue.FindReadProfile("runtime.read") ?? throw new System.InvalidOperationException("A required value is unavailable.")).Codec.Quirks, Is.EqualTo(new string[] { "observed-layout" }));
            Assert.That(((catalogue.ReadProfileSources ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single()), Is.EqualTo(RawProfileRegistrationSource.Custom));
        });
    }

    [Test]
    public void DistinctIdsWithSameCanonicalIdentityAreAcceptedWithAmbiguityDiagnostics()
    {
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileSchemaDocument first = CreateReadOnlyDocument("camera.variant.one", "Shared Camera", "codec-one");
        RawProfileSchemaDocument second = CreateReadOnlyDocument("camera.variant.two", "Shared Camera", "codec-two");
        Assert.That(catalogue.RegisterCustomDocument(first, false, new RawLimits()).Accepted, Is.True);

        RawProfileRegistrationResult result = catalogue.RegisterCustomDocument(second, false, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.AmbiguousProfile));
            Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(RawSeverity.Warning));
            Assert.That(result.Diagnostics.Single().Recovered, Is.True);
            Assert.That(((catalogue.Diagnostics ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(diagnostic => diagnostic.Code == RawDiagnosticCode.AmbiguousProfile)), Is.True);
        });
    }

    [Test]
    public void NormalizedIdentityConflictInOneBatchProducesOneStableAmbiguityReport()
    {
        RawProfileSchemaDocument document = CreateReadOnlyDocument("camera.variant-one", "Camera-X", "codec-one");
        RawReadProfile second = CreateReadOnlyDocument("camera.variant-two", "camera x", "codec-two").ReadProfiles[0];
        document.ReadProfiles[0].Identity.Make = "Example Camera Company";
        document.ReadProfiles[0].Identity.CameraId = "CAMERA-42";
        second.Identity.Make = "example-camera company";
        second.Identity.CameraId = "camera 42";
        document.ReadProfiles = new RawReadProfile[] { document.ReadProfiles[0], second };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();

        RawProfileRegistrationResult registration = catalogue.RegisterCustomDocument(document, false, new RawLimits());
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = RawFormat.Cr2;
        input.Make = "EXAMPLE CAMERA COMPANY";
        input.Model = "camera x";
        input.CameraId = "camera42";
        input.Firmware = "1.5";
        RawProfileMatchResult match = catalogue.MatchReadProfile(input);

        Assert.Multiple(() =>
        {
            Assert.That(registration.Accepted, Is.True);
            Assert.That(registration.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(registration.Diagnostics[0].Code, Is.EqualTo(RawDiagnosticCode.AmbiguousProfile));
            Assert.That(registration.Diagnostics[0].Message, Does.Contain("normalized canonical camera identity"));
            Assert.That(match.Matched, Is.False);
            Assert.That(match.Ambiguous, Is.True);
            Assert.That(match.Candidates.Select(candidate => candidate.ProfileId), Is.EqualTo(new string[] { "camera.variant-one", "camera.variant-two" }));
        });
    }

    [Test]
    public void BuiltInCollisionsAndInvalidRuntimeDocumentsAreRejectedWithoutMutation()
    {
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        byte[] builtIn = RawProfileSchemaCodec.Write(CreateDocument("camera.read", "camera.write", "Built In", "codec"), new RawLimits());
        Assert.That(catalogue.LoadBuiltIn(builtIn, new RawLimits()).Accepted, Is.True);

        RawProfileRegistrationResult duplicate = catalogue.LoadBuiltIn(builtIn, new RawLimits());
        RawProfileSchemaDocument invalid = CreateReadOnlyDocument("invalid.read", "Invalid", "codec");
        invalid.ReadProfiles[0].Geometry.StoredWidth = 0;

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(1));
            Assert.That(catalogue.WriteProfiles, Has.Length.EqualTo(1));
            Assert.Throws<RawWriteException>(() => catalogue.RegisterCustomDocument(invalid, false, new RawLimits()));
            Assert.Throws<RawProfileException>(() => catalogue.RegisterCustomDocument((RawProfileSchemaDocument?)null, false, new RawLimits()));
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ConcurrentRegistrationsProduceOneAtomicWinnerAndStableSnapshots()
    {
        const int attemptCount = 32;
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        RawProfileRegistrationResult[] results = new RawProfileRegistrationResult[attemptCount];
        Task[] tasks = new Task[attemptCount];
        using ManualResetEventSlim start = new ManualResetEventSlim(false);
        for (int index = 0; index < tasks.Length; index++)
        {
            int captured = index;
            RawProfileSchemaDocument document = CreateReadOnlyDocument("concurrent.read", "Camera " + captured, "codec-" + captured);
            tasks[index] = Task.Run(() =>
            {
                start.Wait();
                results[captured] = catalogue.RegisterCustomDocument(document, false, new RawLimits());
            });
        }

        start.Set();
        Task.WaitAll(tasks);
        RawProfileRegistrationResult accepted = results.Single(result => result.Accepted);
        RawReadProfile[] snapshot = catalogue.GetReadProfilesSnapshot();
        ((System.Array)snapshot).SetValue(null, 0);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.AddedReadProfiles, Has.Length.EqualTo(1));
            Assert.That(results.Count(result => !result.Accepted), Is.EqualTo(attemptCount - 1));
            Assert.That(catalogue.ReadProfiles, Has.Length.EqualTo(1));
            Assert.That(catalogue.ReadProfileSources, Is.EqualTo(new RawProfileRegistrationSource[] { RawProfileRegistrationSource.Custom }));
            Assert.That(((catalogue.Diagnostics ?? throw new System.InvalidOperationException("A required value is unavailable.")).Count(diagnostic => diagnostic.Severity == RawSeverity.Error)), Is.EqualTo(attemptCount - 1));
            Assert.That(catalogue.FindReadProfile("concurrent.read"), Is.SameAs(accepted.AddedReadProfiles[0]));
            Assert.That(catalogue.GetReadProfilesSnapshot()[0], Is.Not.Null);
        });
    }

    static RawProfileSchemaDocument CreateDocument(string readId, string writeId, string model, string codecName)
    {
        RawProfileSchemaDocument document = CreateReadOnlyDocument(readId, model, codecName);
        RawWriteProfile write = new RawWriteProfile();
        write.Id = writeId;
        write.ReadProfileId = readId;
        write.Identity = CreateIdentity(model);
        write.Fingerprint = CreateFingerprint();
        write.Geometry = CreateGeometry();
        write.Codec = CreateCodec(codecName);
        write.Calibration = new RawProfileCalibration();
        write.Provenance = CreateProvenance();
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = RawProfileEvidenceKind.Application;
        evidence.Tool = "Compatibility App";
        evidence.Version = "1.0";
        evidence.SampleId = "generated-sample";
        evidence.Check = "open";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Opened successfully.";
        write.Evidence = new RawProfileEvidence[] { evidence };
        write.Experimental = false;
        document.WriteProfiles = new RawWriteProfile[] { write };
        return document;
    }

    static RawProfileSchemaDocument CreateReadOnlyDocument(string readId, string model, string codecName)
    {
        RawProfileSchemaDocument document = new RawProfileSchemaDocument();
        RawReadProfile read = new RawReadProfile();
        read.Id = readId;
        read.Identity = CreateIdentity(model);
        read.Fingerprint = CreateFingerprint();
        read.Geometry = CreateGeometry();
        read.Codec = CreateCodec(codecName);
        read.Calibration = new RawProfileCalibration();
        read.Provenance = CreateProvenance();
        read.Experimental = false;
        document.ReadProfiles = new RawReadProfile[] { read };
        return document;
    }

    static RawProfileIdentity CreateIdentity(string model)
    {
        RawProfileIdentity identity = new RawProfileIdentity();
        identity.Format = RawFormat.Cr2;
        identity.Make = "Example Camera Company";
        identity.Model = model;
        identity.CameraId = "camera-42";
        identity.Aliases = new string[] { model + " Alias" };
        identity.FirmwareMinimum = "1.0";
        identity.FirmwareMaximum = "2.0";
        return identity;
    }

    static RawProfileFingerprint CreateFingerprint()
    {
        RawProfileFingerprint fingerprint = new RawProfileFingerprint();
        fingerprint.Width = 8;
        fingerprint.Height = 6;
        fingerprint.BitDepth = 12;
        fingerprint.Compression = 1;
        fingerprint.SamplesPerPixel = 1;
        fingerprint.RequiredIfds = new string[] { "IFD0", "IFD3" };
        fingerprint.RequiredTags = new string[] { "IFD0/271", "IFD3/273" };
        fingerprint.MakerNoteSignature = "CanonIfd";
        fingerprint.StructuralHash = string.Empty;
        return fingerprint;
    }

    static RawProfileGeometry CreateGeometry()
    {
        RawProfileGeometry geometry = new RawProfileGeometry();
        geometry.StoredWidth = 8;
        geometry.StoredHeight = 6;
        geometry.ActiveArea = new RawArea(RawAreaKind.Active, 1, 1, 6, 4);
        geometry.MaskedAreas = new RawArea[] { new RawArea(RawAreaKind.OpticalBlack, 0, 0, 1, 6) };
        geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        return geometry;
    }

    static RawProfileCodec CreateCodec(string name)
    {
        RawProfileCodec codec = new RawProfileCodec();
        codec.Name = name;
        codec.Compression = 1;
        codec.StoredWordBits = 12;
        codec.Predictor = 1;
        codec.Quirks = new string[] { "observed-layout" };
        return codec;
    }

    static RawProfileProvenance[] CreateProvenance()
    {
        RawProfileProvenance provenance = new RawProfileProvenance();
        provenance.Field = "identity";
        provenance.Source = "sample-42";
        provenance.Kind = RawProfileProvenanceKind.Sample;
        provenance.License = "CC0";
        provenance.Retrieved = "2026-08-19";
        provenance.Notes = string.Empty;
        return new RawProfileProvenance[] { provenance };
    }
}
