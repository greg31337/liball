using raw;

namespace raw.Tests;

public class InterfaceArchitectureTests
{
    [Test]
    public void CodecServiceProvidesCanonicalWorkflowThroughInterface()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 0, 255 };

        RawProbe probe = codec.Probe(source, "sample.raw", RawFormat.GenericRaw, true, new RawOpenOptions());
        RawDocument document = codec.Open(source, descriptor, new RawOpenOptions());
        RawSensorImage decoded = codec.Decode(document, new RawDecodeOptions());
        RawRenderOptions renderOptions = new RawRenderOptions();
        renderOptions.DemosaicMode = RawDemosaicMode.None;
        renderOptions.WhiteBalanceMode = RawWhiteBalanceMode.None;
        renderOptions.ColorSpace = RawColorSpace.RawCamera;
        renderOptions.TransferFunction = RawTransferFunction.Linear;
        renderOptions.OutputBitDepth = 8;
        RawRgbImage rendered = codec.Render(document, renderOptions);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Format, Is.EqualTo(RawFormat.GenericRaw));
            Assert.That(decoded.IntegerSamples, Is.EqualTo(new ushort[] { 0, 65535 }));
            Assert.That(rendered.ByteSamples, Is.EqualTo(source));
            Assert.That(codec.Validate(document, new RawValidationOptions()).IsValid(), Is.True);
            Assert.That(codec.GetCapabilities(RawFormat.GenericRaw, descriptor).Decoded, Is.True);
        });
    }

    [Test]
    public void WriterPlanningConsumesReadOnlyProfileCatalogueContract()
    {
        RawReadProfile read = RawBuiltInProfileCatalogue.CreateCanonEos6DMarkIi();
        read.Id = "test.interface.cr2.read";
        RawWriteProfile write = Cr2EncoderTests.SmallProfile();
        write.Id = "test.interface.cr2.write";
        write.ReadProfileId = read.Id;
        write.Experimental = false;
        write.Evidence = new RawProfileEvidence[]
        {
            Evidence(RawProfileEvidenceKind.Structural, "Independent Structure Tool"),
            Evidence(RawProfileEvidenceKind.Application, "Independent Application Tool")
        };
        TestProfileCatalogue catalogue = new TestProfileCatalogue(read, write);
        RawWriteOptions options = Cr2EncoderTests.Options();
        options.ProfileName = write.Id;

        RawWritePlan plan = RawWriter.PlanCr2(Cr2EncoderTests.Input(8, 4, 8), (IRawProfileCatalogue)catalogue, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ProfileName, Is.EqualTo(write.Id));
            Assert.That(plan.PredictedSize, Is.GreaterThan(0));
            Assert.That(catalogue.FindWriteCalls, Is.EqualTo(1));
            Assert.That(catalogue.FindReadCalls, Is.EqualTo(1));
            Assert.That(catalogue.CompatibilityCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConcreteCatalogueExposesIndependentSnapshotsThroughReadOnlyContract()
    {
        RawProfileCatalogue concrete = RawProfileCatalogue.CreateBuiltIn(new RawLimits());
        IRawProfileCatalogue catalogue = concrete;

        RawReadProfile[] reads = catalogue.GetReadProfiles();
        RawWriteProfile[] writes = catalogue.GetWriteProfiles();
        ((System.Array)reads).SetValue(null, 0);
        ((System.Array)writes).SetValue(null, 0);

        Assert.Multiple(() =>
        {
            Assert.That(catalogue.GetReadProfiles()[0], Is.Not.Null);
            Assert.That(catalogue.GetWriteProfiles()[0], Is.Not.Null);
        });
    }

    [Test]
    public void OpenRoutesContainerProfileMatchingThroughConfiguredContract()
    {
        TestProfileCatalogue catalogue = new TestProfileCatalogue(null, null);
        RawOpenOptions options = new RawOpenOptions();
        options.ProfileCatalogue = catalogue;

        RawDocument document = RawCodec.Open(NefParserTests.BuildNef().Bytes, null, options);

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(catalogue.MatchReadCalls, Is.EqualTo(1));
        });
    }

    private static RawProfileEvidence Evidence(RawProfileEvidenceKind kind, string tool)
    {
        RawProfileEvidence evidence = new RawProfileEvidence();
        evidence.Kind = kind;
        evidence.Tool = tool;
        evidence.Version = "1.0";
        evidence.SampleId = "interface-test";
        evidence.Check = "interface-substitution";
        evidence.Result = RawProfileEvidenceResult.Passed;
        evidence.Details = "Passed.";
        return evidence;
    }
}

internal class TestProfileCatalogue : IRawProfileCatalogue
{
    internal RawReadProfile? Read;
    internal RawWriteProfile? Write;
    internal int FindReadCalls;
    internal int FindWriteCalls;
    internal int CompatibilityCalls;
    internal int MatchReadCalls;

    internal TestProfileCatalogue(RawReadProfile? read, RawWriteProfile? write)
    {
        Read = read;
        Write = write;
    }

    public RawReadProfile? FindReadProfile(string id)
    {
        FindReadCalls++;
        return Read != null && Read.Id == id ? Read : null;
    }

    public RawWriteProfile? FindWriteProfile(string id)
    {
        FindWriteCalls++;
        return Write != null && Write.Id == id ? Write : null;
    }

    public RawProfileMatchResult MatchReadProfile(RawProfileMatchInput input)
    {
        MatchReadCalls++;
        return RawProfileMatcher.MatchRead(GetReadProfiles(), input);
    }

    public RawProfileMatchResult MatchWriteProfile(RawProfileMatchInput input)
    {
        return RawProfileMatcher.MatchWrite(GetWriteProfiles(), input);
    }

    public RawWriteCompatibilityMatrix GetWriteCompatibilityMatrix(RawFormat format)
    {
        CompatibilityCalls++;
        return new RawWriteCompatibilityMatrix(format, System.Array.Empty<RawWriteCompatibilityEntry>());
    }

    public RawReadProfile[] GetReadProfiles()
    {
        return Read == null ? System.Array.Empty<RawReadProfile>() : new RawReadProfile[] { Read };
    }

    public RawWriteProfile[] GetWriteProfiles()
    {
        return Write == null ? System.Array.Empty<RawWriteProfile>() : new RawWriteProfile[] { Write };
    }
}
