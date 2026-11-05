using System.Buffers.Binary;

namespace avif.Tests;

public class FileTypeBoxTests
{
    [Test]
    public void WriterAndParserRoundTripFileTypeBrands()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 128);
        AvifResultCode writeCode = FileTypeBox.Write(
            writer,
            "avif",
            7,
            new string[] { "mif1", "miaf", "MA1B", "zzzz" });
        IsoBoxWriteResult written = writer.Finish();

        FileTypeParseResult parsed = FileTypeParser.Parse(written.Data, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(writeCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.FileType ?? throw new InvalidOperationException("Invalid internal state.")).MajorBrand, Is.EqualTo("avif"));
            Assert.That(parsed.FileType.MajorBrandValue, Is.EqualTo(IsoBoxType.GetValue("avif")));
            Assert.That(parsed.FileType.MinorVersion, Is.EqualTo(7));
            Assert.That(
                parsed.FileType.CompatibleBrands,
                Is.EqualTo(new string[] { "mif1", "miaf", "MA1B", "zzzz" }));
            Assert.That(parsed.FileType.HasBrand("avif"), Is.True);
            Assert.That(parsed.FileType.HasBrand("MA1B"), Is.True);
            Assert.That(parsed.FileType.HasBrand("avis"), Is.False);
        });
    }

    [Test]
    public void ParserRejectsMalformedOrMisplacedFileTypeBoxes()
    {
        byte[] shortPayload = CreateBox(12, "ftyp");
        byte[] misalignedPayload = CreateBox(17, "ftyp");
        byte[] otherBox = CreateHeader(8, "free");

        FileTypeParseResult shortResult = FileTypeParser.Parse(shortPayload, CreateLimits());
        FileTypeParseResult misalignedResult = FileTypeParser.Parse(
            misalignedPayload,
            CreateLimits());
        FileTypeParseResult otherResult = FileTypeParser.Parse(otherBox, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(misalignedResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(otherResult.Code, Is.EqualTo(AvifResultCode.NotFound));
        });
    }

    [Test]
    public void ParserBoundsRetainedCompatibleBrandStorage()
    {
        byte[] source = CreateFileTypeBytes(
            "avif",
            new string[] { "mif1", "miaf" });
        AvifSecurityLimits limited = CreateLimits();
        limited.MaxTemporaryBytes = 63;
        AvifSecurityLimits allowed = CreateLimits();
        allowed.MaxTemporaryBytes = 64;

        FileTypeParseResult rejected = FileTypeParser.Parse(source, limited);
        FileTypeParseResult parsed = FileTypeParser.Parse(source, allowed);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Code,
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.FileType ?? throw new InvalidOperationException("Invalid internal state.")).CompatibleBrands,
                Is.EqualTo(new string[] { "mif1", "miaf" }));
        });
    }

    [Test]
    public void WriterValidatesBrandsAndCapacityBeforeWriting()
    {
        IsoBoxWriter invalidWriter = new IsoBoxWriter(0, 64);
        AvifResultCode invalidCode = FileTypeBox.Write(
            invalidWriter,
            "avif",
            0,
            new string[] { "bad" });

        IsoBoxWriter boundedWriter = new IsoBoxWriter(0, 15);
        AvifResultCode boundedCode = FileTypeBox.Write(
            boundedWriter,
            "avif",
            0,
            System.Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(invalidCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidWriter.Output.Length, Is.Zero);
            Assert.That(boundedCode, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(boundedWriter.Output.Length, Is.Zero);
        });
    }

    [Test]
    public void ResolverReportsAvifCapabilitiesMimeTypeAndUnknownBrands()
    {
        FileTypeBox? fileType = CreateFileType(
            "zzzz",
            new string[] { "avif", "mif1", "miaf", "MA1B", "av01", "yyyy", "yyyy" });

        BrandCapabilityReport report = BrandResolver.Resolve(fileType);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsAvif, Is.True);
            Assert.That(report.HasImageItems, Is.True);
            Assert.That(report.HasImageSequence, Is.False);
            Assert.That(report.HasBaselineProfile, Is.True);
            Assert.That(report.HasMiaf, Is.True);
            Assert.That(report.HasHeifImageBrand, Is.True);
            Assert.That(report.HasAv1CodecBrand, Is.True);
            Assert.That(report.HasUnknownMajorBrand, Is.True);
            Assert.That(report.HasForwardCompatibleBrands, Is.True);
            Assert.That(report.MimeType, Is.EqualTo("image/avif"));
            Assert.That(report.UnknownBrands, Is.EqualTo(new string[] { "zzzz", "yyyy" }));
        });
    }

    [Test]
    public void ResolverFallsBackToGenericHeifMimeTypes()
    {
        BrandCapabilityReport image = BrandResolver.Resolve(
            CreateFileType("mif1", System.Array.Empty<string>()));
        BrandCapabilityReport sequence = BrandResolver.Resolve(
            CreateFileType("msf1", System.Array.Empty<string>()));
        BrandCapabilityReport lowOverhead = BrandResolver.Resolve(
            CreateFileType("mif3", System.Array.Empty<string>()));
        BrandCapabilityReport unknown = BrandResolver.Resolve(
            CreateFileType("zzzz", System.Array.Empty<string>()));

        Assert.Multiple(() =>
        {
            Assert.That(image.MimeType, Is.EqualTo("image/heif"));
            Assert.That(sequence.MimeType, Is.EqualTo("image/heif-sequence"));
            Assert.That(lowOverhead.MimeType, Is.EqualTo("image/heif"));
            Assert.That(lowOverhead.HasLowOverheadBrand, Is.True);
            Assert.That(unknown.MimeType, Is.Empty);
        });
    }

    [Test]
    public void ProbeNeedsOnlyTheCompleteFileTypeBox()
    {
        byte[] fileType = CreateFileTypeBytes(
            "avif",
            new string[] { "mif1", "miaf", "MA1B" });
        byte[] source = new byte[fileType.Length + 8];
        fileType.CopyTo(source, 0);
        CreateHeader(20, "free").CopyTo(source, fileType.Length);

        FileTypeProbeResult probe = FileTypeProbe.Probe(source, CreateLimits());
        FileTypeParseResult fullParse = FileTypeParser.Parse(source, CreateLimits());
        FileTypeProbeResult incomplete = FileTypeProbe.Probe(
            CreateHeader(20, "ftyp"),
            CreateLimits());
        FileTypeProbeResult wrongType = FileTypeProbe.Probe(
            CreateHeader(8, "free"),
            CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(probe.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.Capabilities.MimeType, Is.EqualTo("image/avif"));
            Assert.That((probe.Validation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(fullParse.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(incomplete.Code, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(wrongType.Code, Is.EqualTo(AvifResultCode.NotFound));
        });
    }

    [Test]
    public void ValidatorEnforcesRequiredBrandsAndReportsRecommendedBrands()
    {
        BrandValidationResult conforming = BrandValidator.Validate(
            CreateFileType("avif", new string[] { "mif1", "miaf" }),
            null);
        BrandValidationResult missingMiaf = BrandValidator.Validate(
            CreateFileType("avif", new string[] { "mif1" }),
            null);
        BrandValidationResult profileWithoutAvif = BrandValidator.Validate(
            CreateFileType("MA1B", new string[] { "mif1", "miaf" }),
            null);
        BrandValidationResult missingMif1 = BrandValidator.Validate(
            CreateFileType("avif", new string[] { "miaf" }),
            null);

        Assert.Multiple(() =>
        {
            Assert.That(conforming.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(conforming.Diagnostics.Diagnostics, Is.Empty);
            Assert.That(missingMiaf.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That((missingMiaf.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath, Is.EqualTo("/ftyp"));
            Assert.That(profileWithoutAvif.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(missingMif1.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(missingMif1.Diagnostics.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                (missingMif1.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Severity,
                Is.EqualTo(AvifDiagnosticSeverity.Warning));
        });
    }

    [Test]
    public void BaselineProfileValidationAcceptsBoundaryAndRejectsExcess()
    {
        FileTypeBox? fileType = CreateFileType(
            "avif",
            new string[] { "mif1", "miaf", "MA1B" });
        BrandValidationResult boundary = BrandValidator.Validate(
            fileType,
            new AvifCodedProfileFacts(0, 13, 8192, 1088, false));
        BrandValidationResult wrongProfile = BrandValidator.Validate(
            fileType,
            new AvifCodedProfileFacts(1, 13, 8192, 1088, false));
        BrandValidationResult tooManyPixels = BrandValidator.Validate(
            fileType,
            new AvifCodedProfileFacts(0, 13, 8192, 1089, false));

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(wrongProfile.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(tooManyPixels.Code, Is.EqualTo(AvifResultCode.NonConformingData));
        });
    }

    [Test]
    public void AdvancedProfileValidationDistinguishesItemsAndSequences()
    {
        FileTypeBox? itemFileType = CreateFileType(
            "avif",
            new string[] { "mif1", "miaf", "MA1A" });
        FileTypeBox? sequenceFileType = CreateFileType(
            "avis",
            new string[] { "msf1", "miaf", "MA1A" });

        BrandValidationResult itemBoundary = BrandValidator.Validate(
            itemFileType,
            new AvifCodedProfileFacts(1, 16, 16384, 2176, false));
        BrandValidationResult itemMainProfile = BrandValidator.Validate(
            itemFileType,
            new AvifCodedProfileFacts(0, 16, 16384, 2176, false));
        BrandValidationResult sequenceBoundary = BrandValidator.Validate(
            sequenceFileType,
            new AvifCodedProfileFacts(1, 13, 8192, 1088, true));
        BrandValidationResult sequenceLevelTooHigh = BrandValidator.Validate(
            sequenceFileType,
            new AvifCodedProfileFacts(1, 14, 8192, 1088, true));

        Assert.Multiple(() =>
        {
            Assert.That(itemBoundary.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(itemMainProfile.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(sequenceBoundary.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(sequenceLevelTooHigh.Code, Is.EqualTo(AvifResultCode.NonConformingData));
        });
    }

    private static FileTypeBox? CreateFileType(string majorBrand, string[] compatibleBrands)
    {
        byte[] source = CreateFileTypeBytes(majorBrand, compatibleBrands);
        FileTypeParseResult result = FileTypeParser.Parse(source, CreateLimits());
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.FileType;
    }

    private static byte[] CreateFileTypeBytes(string majorBrand, string[] compatibleBrands)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 256);
        AvifResultCode code = FileTypeBox.Write(writer, majorBrand, 0, compatibleBrands);
        Assert.That(code, Is.EqualTo(AvifResultCode.Success));
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static byte[] CreateHeader(uint size, string type)
    {
        byte[] header = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), size);
        header[4] = (byte)type[0];
        header[5] = (byte)type[1];
        header[6] = (byte)type[2];
        header[7] = (byte)type[3];
        return header;
    }

    private static byte[] CreateBox(int size, string type)
    {
        byte[] box = new byte[size];
        CreateHeader((uint)size, type).CopyTo(box, 0);
        return box;
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 1024;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 64;
        return limits;
    }
}
