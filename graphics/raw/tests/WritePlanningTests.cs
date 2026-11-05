using raw;

namespace raw.Tests;

public class WritePlanningTests
{
    [Test]
    public void Cr2NewContainerPlanReportsExactSizeEncodingActionsAndCompatibility()
    {
        RawProfileCatalogue catalogue = Cr2Catalogue();
        RawCr2WriteInput input = Cr2EncoderTests.Input(8, 4, 8);
        RawWriteOptions options = Cr2EncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.cr2.plan.write";

        RawWritePlan plan = RawWriter.PlanCr2(input, catalogue, options);
        byte[] encoded = RawWriter.EncodeCr2(input, catalogue, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(encoded.Length));
            Assert.That(plan.ProfileName, Is.EqualTo("test.cr2.plan.write"));
            Assert.That(plan.Changes.Any(change => change.Kind == RawWriteChangeKind.Reencoded && change.Location == "IFD3/Strip0"), Is.True);
            Assert.That(plan.Changes.Count(change => change.Kind == RawWriteChangeKind.Added), Is.EqualTo(4));
            Assert.That(plan.Compatibility, Has.Length.EqualTo(1));
            Assert.That(plan.Compatibility[0].Application, Is.EqualTo("Independent CR2 Planner"));
            Assert.That(plan.HasConfirmedCompatibility(), Is.True);
            Assert.That(plan.HasErrors(), Is.False);
        });
    }

    [Test]
    public void LossyNefPlanIsInspectableButBlockedWithoutDataLossConsent()
    {
        RawProfileCatalogue catalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawNefWriteInput input = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions options = NefEncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.lossy.write";
        options.AllowLossy = true;
        options.DataLossConsent = false;

        RawWritePlan plan = RawWriter.PlanNef(input, catalogue, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.False);
            Assert.That(plan.PredictedSize, Is.GreaterThan(0));
            Assert.That(plan.RequiresDataLossConsent(), Is.True);
            Assert.That(plan.HasErrors(), Is.True);
            Assert.That(plan.Diagnostics.Any(item => item.Code == RawDiagnosticCode.DataLossConsentRequired && item.Severity == RawSeverity.Error), Is.True);
            Assert.That(plan.Changes.Any(item => item.Kind == RawWriteChangeKind.Reencoded && item.RequiresConsent), Is.True);
            Assert.That(plan.Changes.Any(item => item.Location.EndsWith("0x0096", StringComparison.Ordinal) && item.RequiresConsent), Is.True);
        });
    }

    [Test]
    public void UneditedCr2RewritePlanReportsByteCopiesWithoutMutatingSource()
    {
        byte[] source = Cr2Source();
        RawDocument document = Open(source);

        RawWritePlan plan = RawWriter.PlanRewriteCr2(document, new RawWriteOptions());
        byte[] rewritten = RawWriter.RewriteCr2(document, new RawWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(rewritten.Length));
            Assert.That(plan.Changes, Is.Not.Empty);
            Assert.That(plan.Changes.All(change => change.Kind == RawWriteChangeKind.Copied || change.Kind == RawWriteChangeKind.Relocated), Is.True);
            Assert.That(plan.Changes.Any(change => change.Kind == RawWriteChangeKind.Copied), Is.True);
            Assert.That(document.Source.ToArray(), Is.EqualTo(source));
            Assert.That(document.Metadata.PendingEdits, Is.Empty);
            Assert.That(document.PendingPreviewEdits, Is.Empty);
        });
    }

    [Test]
    public void EditedCr2RewritePlanReportsChangedDroppedAndRelocatedBlocksWithExactSize()
    {
        byte[] source = Cr2Source();
        RawDocument document = Open(source);
        string rawPath = document.PayloadMap.Find(RawPayloadKind.RawImage).Single().Path;
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Replace("Artist", new string[] { "A substantially longer dry-run creator" }, null, RawMetadataSource.Exif);
        edits.Remove("Copyright", null, RawMetadataSource.Exif);
        edits.ReplacePreview(0, RawThumbnailKind.Jpeg, 6, 6, RawOrientation.Normal, "image/jpeg", Preview(6, 6, 171));
        Assert.That(edits.Apply(document, false).Succeeded(), Is.True);

        RawWritePlan plan = RawWriter.PlanRewriteCr2(document, new RawWriteOptions());
        byte[] rewritten = RawWriter.RewriteCr2(document, new RawWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.PredictedSize, Is.EqualTo(rewritten.Length));
            Assert.That(plan.Changes.Any(change => change.Kind == RawWriteChangeKind.Changed && change.Location.EndsWith("0x013B", StringComparison.Ordinal)), Is.True);
            Assert.That(plan.Changes.Any(change => change.Kind == RawWriteChangeKind.Dropped && change.Location.EndsWith("0x8298", StringComparison.Ordinal)), Is.True);
            Assert.That(plan.Changes.Any(change => change.Kind == RawWriteChangeKind.Relocated && change.Location == rawPath), Is.True);
            Assert.That(plan.RequiresDataLossConsent(), Is.False);
            Assert.That(plan.HasWarnings(), Is.True);
            Assert.That(plan.HasErrors(), Is.False);
            Assert.That(document.Source.ToArray(), Is.EqualTo(source));
        });
    }

    [Test]
    public void GenericRewritePlannerDispatchesNefAndMatchesCommittedSize()
    {
        NefFixture fixture = NefParserTests.BuildNef();
        RawDocument document = Open(fixture.Bytes);

        RawWritePlan plan = RawWriter.PlanRewrite(document, new RawWriteOptions());
        byte[] rewritten = RawWriter.RewriteNef(document, new RawWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(rewritten.Length));
            Assert.That(plan.Changes, Is.Not.Empty);
            Assert.That(plan.Changes.All(change => change.Kind == RawWriteChangeKind.Copied || change.Kind == RawWriteChangeKind.Relocated), Is.True);
            Assert.That(plan.HasErrors(), Is.False);
        });
    }

    private static RawProfileCatalogue Cr2Catalogue()
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateCanonEos6DMarkIi();
        read.Id = "test.cr2.plan.read";
        read.Identity.Model = "Synthetic Plan CR2";
        read.Fingerprint.Width = 8;
        read.Fingerprint.Height = 4;
        read.Fingerprint.BitDepth = 14;
        read.Geometry.StoredWidth = 8;
        read.Geometry.StoredHeight = 4;
        read.Geometry.ActiveArea = new RawArea(RawAreaKind.Active, 0, 0, 8, 4);
        read.Codec.StoredWordBits = 14;
        read.Codec.SliceWidths = new int[] { 8 };
        read.Experimental = false;
        RawWriteProfile write = Cr2EncoderTests.SmallProfile();
        write.Id = "test.cr2.plan.write";
        write.ReadProfileId = read.Id;
        write.Identity.Model = read.Identity.Model;
        write.Experimental = false;
        write.Evidence = new RawProfileEvidence[]
        {
            Evidence(RawProfileEvidenceKind.Structural, "Independent Structure Planner"),
            Evidence(RawProfileEvidenceKind.Application, "Independent CR2 Planner")
        };
        RawProfileSchemaDocument profiles = new RawProfileSchemaDocument();
        profiles.ReadProfiles = new RawReadProfile[] { read };
        profiles.WriteProfiles = new RawWriteProfile[] { write };
        RawProfileCatalogue catalogue = new RawProfileCatalogue();
        Assert.That(catalogue.RegisterCustomDocument(profiles, false, new RawLimits()).Accepted, Is.True);
        return catalogue;
    }

    private static RawProfileEvidence Evidence(RawProfileEvidenceKind kind, string tool)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = kind;
        evidence.Tool = tool;
        evidence.Version = "1.0";
        evidence.SampleId = "generated-plan-test";
        evidence.Check = kind == RawProfileEvidenceKind.Application ? "open-render-metadata" : "parse-write-parse";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Passed.";
        return evidence;
    }

    private static byte[] Cr2Source()
    {
        return Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options());
    }

    private static RawDocument Open(byte[] source)
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return RawCodec.Open(source, null, options);
    }

    private static byte[] Preview(int width, int height, byte start)
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = new byte[width * height];
        for (int index = 0; index < plane.Samples.Length; index++)
        {
            plane.Samples[index] = (byte)(start + index % 13);
        }
        image.Planes.Add(plane);
        return BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None);
    }
}
