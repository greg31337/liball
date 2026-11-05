using icc;
using System.Security.Cryptography;

namespace icc.Tests;

public class ReferenceEngineDifferentialTests
{
    [TestCase("v2.1-display-gamma.icc", IccVersionKind.Version2, 2, 1, 0,
        IccProfileClass.Display, IccColorSpaceKind.Rgb, IccPcsKind.Xyz, 9)]
    [TestCase("v2.1-display-gamma-2.398.icc", IccVersionKind.Version2, 2, 1, 0,
        IccProfileClass.Display, IccColorSpaceKind.Rgb, IccPcsKind.Xyz, 9)]
    [TestCase("v4.2-colorspace-srgb.icc", IccVersionKind.Version4, 4, 2, 0,
        IccProfileClass.ColorSpace, IccColorSpaceKind.Rgb, IccPcsKind.Lab, 9)]
    [TestCase("v4.4-display-parametric.icc", IccVersionKind.Version4, 4, 0, 4,
        IccProfileClass.Display, IccColorSpaceKind.Rgb, IccPcsKind.Xyz, 9)]
    public void ParsingMatchesLittleCms2191(string fileName,
        IccVersionKind versionKind, int major, int minor, int bugFix,
        IccProfileClass profileClass, IccColorSpaceKind dataSpace,
        IccPcsKind pcs, int tagCount)
    {
        IccProfile profile = ReadValidProfile(fileName);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.Version.Kind, Is.EqualTo(versionKind));
            Assert.That(profile.Header.Version.Major, Is.EqualTo(major));
            Assert.That(profile.Header.Version.Minor, Is.EqualTo(minor));
            Assert.That(profile.Header.Version.BugFix, Is.EqualTo(bugFix));
            Assert.That(profile.Header.ProfileClass, Is.EqualTo(profileClass));
            Assert.That(profile.Header.DataColorSpace, Is.EqualTo(dataSpace));
            Assert.That(profile.Header.ProfileConnectionSpace, Is.EqualTo(pcs));
            Assert.That(profile.Header.RenderingIntentKind,
                Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(profile.Tags, Has.Length.EqualTo(tagCount));
        });
    }

    [TestCase("v2.1-display-gamma.icc",
        0.417274485594, 0.464900969478, 0.777301788789)]
    [TestCase("v2.1-display-gamma-2.398.icc",
        0.173196533979, 0.179515677290, 0.500057577092)]
    [TestCase("v4.2-colorspace-srgb.icc",
        0.189189005904, 0.206486099288, 0.399877317539)]
    [TestCase("v4.4-display-parametric.icc",
        0.160353001893, 0.174195581988, 0.376909792997)]
    public void DeviceToPcsTransformMatchesLittleCms2191(string fileName,
        double expectedX, double expectedY, double expectedZ)
    {
        IccProfile profile = ReadValidProfile(fileName);
        ProfileDirectionPlan direction =
            ProfileTransformCompiler.CompileProfileDirection(profile,
                IccTransformDirection.DeviceToPcs,
                IccRenderingIntent.MediaRelativeColorimetric,
                IccIntentFallback.AllowAny,
                IccInterpolationKind.Multilinear, 0);

        double[] result = BlackPointCompensation.ExecuteDirection(direction,
            new double[] { 0.25, 0.5, 0.75 });
        if (direction.PcsKind == IccPcsKind.Lab)
        {
            result = new IccColorMath().ConvertLabToXyz(result,
                new double[]
                {
                    IccStandardColorimetry.D50X,
                    IccStandardColorimetry.D50Y,
                    IccStandardColorimetry.D50Z
                });
        }

        Assert.That(result,
            Is.EqualTo(new double[] { expectedX, expectedY, expectedZ })
                .Within(0.00002), fileName);
    }

    [TestCase("v4.4-display-parametric.icc",
        "689322B8473DF7337564667BAF7261B5")]
    public void VersionFourProfileIdMatchesLittleCms2191(string fileName,
        string expectedId)
    {
        byte[] bytes = ReadValidBytes(fileName);

        IccProfileId result = new IccProfileIdCalculator()
            .ComputeProfileIdBytes(bytes);

        Assert.That(Convert.ToHexString(result.Bytes), Is.EqualTo(expectedId));
    }

    [Test]
    public void LittleCmsNormalizedProfileIdDifferenceIsIntentional()
    {
        byte[] bytes = ReadValidBytes("v4.2-colorspace-srgb.icc");

        IccProfileId result = new IccProfileIdCalculator()
            .ComputeProfileIdBytes(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(result.Bytes),
                Is.EqualTo("34562ABF994CCD066D2C5721D0D68C5D"),
                "The supplied profile bytes carry their own standard-computed ID.");
            Assert.That(Convert.ToHexString(result.Bytes),
                Is.Not.EqualTo("C31AA9ED3A6D2D553D53005F6D280EF2"),
                "Little CMS 2.19.1 computes this fixture's ID after normalizing its in-memory serialization; this API hashes the supplied ICC byte sequence.");
        });
    }

    [Test]
    public void VersionTwoProfileIdDifferenceIsIntentional()
    {
        byte[] bytes = ReadValidBytes("v2.1-display-gamma.icc");

        Assert.That(
            () => new IccProfileIdCalculator().ComputeProfileIdBytes(bytes),
            Throws.TypeOf<IccUnsupportedFeatureException>().With.Message
                .Contains("version 4 and version 5"));
    }

    [TestCase("v4.2-colorspace-srgb.icc", 60960)]
    [TestCase("v4.4-display-parametric.icc", 480)]
    public void PreservationSerializationMatchesLittleCmsSizeAndSourceBytes(
        string fileName, int littleCmsSerializedSize)
    {
        byte[] source = ReadValidBytes(fileName);
        IccProfile profile = ReadValidProfile(fileName);
        IccWriteOptions options = new IccWriteOptions();
        options.Mode = IccWriteMode.Preservation;
        options.ComputeProfileId = false;
        options.RejectInvalidProfile = false;

        byte[] serialized = new IccProfileWriter().WriteProfileBytes(profile,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(serialized, Has.Length.EqualTo(littleCmsSerializedSize));
            Assert.That(serialized, Is.EqualTo(source));
        });
    }

    [TestCase("v2.1-display-gamma.icc", 488,
        "866F73C601029C57032B537DF5C3F47063CC885A269D0C1BC43D649A81FC3DEF")]
    [TestCase("v2.1-display-gamma-2.398.icc", 496,
        "3EF9E9DAF90565D95EEF3A0B6D29558C66C02DCF3D034F6F38DDE9EEC9DDAE91")]
    public void VersionTwoSerializationDifferenceIsIntentional(
        string fileName, int littleCmsSerializedSize,
        string littleCmsSerializedHash)
    {
        byte[] source = ReadValidBytes(fileName);
        byte[] expected = source.ToArray();
        System.Array.Clear(expected, 84, 16);
        IccProfile profile = ReadValidProfile(fileName);
        IccWriteOptions options = new IccWriteOptions();
        options.Mode = IccWriteMode.Preservation;
        options.ComputeProfileId = false;
        options.RejectInvalidProfile = false;

        byte[] serialized = new IccProfileWriter().WriteProfileBytes(profile,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(serialized, Has.Length.EqualTo(littleCmsSerializedSize));
            Assert.That(serialized, Is.EqualTo(expected),
                "ICC v2 defines header bytes 84 through 99 as reserved, so this writer zeros the non-standard Profile ID stored there by the fixture.");
            Assert.That(Convert.ToHexString(SHA256.HashData(serialized)),
                Is.Not.EqualTo(littleCmsSerializedHash),
                "Little CMS 2.19.1 writes its convenience Profile ID into the reserved ICC v2 header bytes.");
        });
    }

    [Test]
    public void CanonicalSerializationDifferenceFromLittleCmsIsIntentional()
    {
        IccProfile profile = ReadValidProfile("v4.2-colorspace-srgb.icc");
        IccWriteOptions options = new IccWriteOptions();
        options.PreserveOriginalCreationDateTime = false;
        options.RejectInvalidProfile = false;

        byte[] canonical = new IccProfileWriter().WriteProfileBytes(profile,
            options);
        string canonicalHash = Convert.ToHexString(SHA256.HashData(canonical));

        Assert.Multiple(() =>
        {
            Assert.That(canonicalHash,
                Is.Not.EqualTo("521AE7DBDF2F79374A1652859EAF86867EDE18440A5C165930C9E2F9CF0510C1"),
                "Little CMS preserves tag order and representations; canonical mode deliberately normalizes them.");
            Assert.That(new IccProfileReader().ProbeProfileBytes(canonical,
                new IccReadOptions()).Status,
                Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [TestCase("v2.1-abstract-fuzz.icc")]
    [TestCase("v4.2-colorspace-clut-overflow.icc")]
    [TestCase("v4.3-display-no-data-space.icc")]
    public void SecurityValidationIsIntentionallyStricterThanLittleCmsOpen(
        string fileName)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(CorpusDirectory(),
            "malformed", fileName));
        IccValidationReport report = new IccProfileValidator()
            .ValidateProfileBytesReport(bytes, RecoveryOptions());
        bool littleCmsAcceptedProfile = true;

        Assert.Multiple(() =>
        {
            Assert.That(littleCmsAcceptedProfile, Is.True,
                "Little CMS 2.19.1 cmsOpenProfileFromFile accepted this upstream iccDEV regression input.");
            Assert.That(report.IsValid, Is.Not.EqualTo(littleCmsAcceptedProfile));
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(report.Issues.Any(issue =>
                issue.Severity == IccValidationSeverity.Error), Is.True);
        });
    }

    private static IccProfile ReadValidProfile(string fileName)
    {
        return new IccProfileReader().ReadProfileBytes(
            ReadValidBytes(fileName), RecoveryOptions());
    }

    private static byte[] ReadValidBytes(string fileName)
    {
        return File.ReadAllBytes(Path.Combine(CorpusDirectory(), "valid",
            fileName));
    }

    private static string CorpusDirectory()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..",
            "Corpus", "iccdev"));
    }

    private static IccReadOptions RecoveryOptions()
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ValidationMode = IccValidationMode.SecurityStrict;
        options.DecodeTagsLazily = false;
        return options;
    }
}
