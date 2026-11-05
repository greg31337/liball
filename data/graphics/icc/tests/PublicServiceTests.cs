using icc;

namespace icc.Tests;

public class PublicServiceTests
{
    [Test]
    public void ProfileBuilderCreatesAndClonesIndependentProfiles()
    {
        IIccProfileBuilder builder = new IccProfileBuilder();
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        IccProfile source = builder.CreateXyzIdentityProfile(options);
        IccProfileHeader header = source.Header;
        IccTag[] sourceTags = source.Tags;
        IccTag description = sourceTags.Single(tag =>
            tag.Signature == IccTagSignatures.ProfileDescription);

        IccProfile created = builder.CreateProfile(header, sourceTags);
        header.DeclaredSize = 512;
        description.Data.LocalizedStrings[0].Text = "changed source";
        IccProfile copied = builder.CloneProfile(created);
        copied.Tags.Single(tag => tag.Signature == IccTagSignatures.ProfileDescription)
            .Data.LocalizedStrings[0].Text = "changed copy";
        IccTag createdDescription = created.Tags.Single(tag =>
            tag.Signature == IccTagSignatures.ProfileDescription);

        Assert.Multiple(() =>
        {
            Assert.That(created.Header.DeclaredSize, Is.EqualTo(created.OriginalBytes.Length));
            Assert.That(created.Header.DeclaredSize, Is.Not.EqualTo(512));
            Assert.That(created.Header, Is.Not.SameAs(header));
            Assert.That(created.Tags, Is.Not.SameAs(sourceTags));
            Assert.That(createdDescription, Is.Not.SameAs(description));
            Assert.That(createdDescription.Data.LocalizedStrings[0].Text,
                Is.EqualTo("ICC profile"));
            Assert.That(created.TagTableEntries[0], Is.SameAs(created.Tags[0].TableEntry));
            Assert.That(copied.Tags.Single(tag =>
                tag.Signature == IccTagSignatures.ProfileDescription)
                .Data.LocalizedStrings[0].Text, Is.EqualTo("changed copy"));
        });
    }

    [Test]
    public void ProbeServicesReturnFreshUnsupportedResults()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IIccToneCurveEvaluator curveEvaluator = new IccToneCurveEvaluator();
        IIccTransformFactory transformFactory = new IccTransformFactory();
        IIccColorTransform transform = new IccColorTransform();
        IIccProfileLinker linker = new IccProfileLinker();
        IIccGamutAnalyzer gamutAnalyzer = new IccGamutAnalyzer();
        IIccProfileComparer comparer = new IccProfileComparer();
        IIccEmbeddedProfileReader embeddedReader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter embeddedWriter = new IccEmbeddedProfileWriter();
        IccProfile profile = new IccProfile();

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateProfileData(profile).Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(curveEvaluator.AnalyzeSampledCurve(new double[] { 0.0, 1.0 }).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(transformFactory.ProbeProfileTransform(profile, profile, IccRenderingIntent.Perceptual).Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            AssertUnsupported(transform.ProbeTransformColor(new float[0]));
            Assert.That(linker.ProbeProfileSequence(new IccProfile[] { profile }, IccRenderingIntent.Perceptual).Status,
                Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(gamutAnalyzer.ProbeProfileGamut(profile).Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(comparer.ProbeProfileComparison(profile, profile).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(embeddedReader.ProbeEmbeddedProfiles(new byte[0],
                new IccResourceLimits()).Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(embeddedWriter.ProbeEmbeddingOperation(IccEmbeddedFormat.Png,
                IccEmbeddedOperation.Insert).Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(transform.GetSourceChannelCount(), Is.Zero);
            Assert.That(transform.GetDestinationChannelCount(), Is.Zero);
            Assert.That(embeddedReader.DetectCarrierBytes(new byte[0]), Is.EqualTo(IccEmbeddedFormat.Unknown));
        });
    }

    [Test]
    public void ThrowingServicesUseTheUnsupportedFeatureExceptionUntilTheirCoresAreAvailable()
    {
        IIccColorMath colorMath = new IccColorMath();
        IIccChromaticAdapter adapter = new IccChromaticAdapter();
        IIccProfileWriter writer = new IccProfileWriter();
        IIccProfileIdCalculator idCalculator = new IccProfileIdCalculator();
        IIccToneCurveEvaluator curveEvaluator = new IccToneCurveEvaluator();
        IIccTransformFactory transformFactory = new IccTransformFactory();
        IIccColorTransform transform = new IccColorTransform();
        IIccProfileLinker linker = new IccProfileLinker();
        IIccGamutAnalyzer gamutAnalyzer = new IccGamutAnalyzer();
        IIccProfileComparer comparer = new IccProfileComparer();
        IIccEmbeddedProfileReader embeddedReader = new IccEmbeddedProfileReader();
        IIccEmbeddedProfileWriter embeddedWriter = new IccEmbeddedProfileWriter();
        IccProfile profile = new IccProfile();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyzToLab(new double[3], new double[3]));
            Assert.Throws<IccTransformException>(() => adapter.BuildBradfordMatrix(new double[3], new double[3]));
            Assert.Throws<IccProfileWriteException>(() => writer.WriteProfileBytes(profile, new IccWriteOptions()));
            Assert.Throws<IccProfileFormatException>(() => idCalculator.ComputeProfileIdBytes(new byte[0]));
            Assert.That(curveEvaluator.SampleGammaCurve(2.0, 3), Is.EqualTo(new double[] { 0.0, 0.25, 1.0 }));
            Assert.Throws<IccTransformException>(() => transformFactory.CreateProfileTransform(profile, profile, IccRenderingIntent.Perceptual, IccIntentFallback.None, IccInterpolationKind.Linear));
            Assert.Throws<IccUnsupportedFeatureException>(() => transform.TransformSingleColor(new float[0]));
            Assert.Throws<IccProfileValidationException>(() => linker.LinkProfileSequence(new IccProfile[] { profile }, IccRenderingIntent.Perceptual, new IccWriteOptions()));
            Assert.That(gamutAnalyzer.EvaluateProfileGamut(profile, new float[0]), Is.EqualTo(IccGamutStatus.Unknown));
            Assert.Throws<IccProfileValidationException>(() => comparer.CompareProfilesExactly(profile, profile));
            Assert.Throws<IccEmbeddingException>(() => embeddedReader.ExtractProfilesBytes(
                new byte[0], new IccReadOptions()));
            Assert.Throws<IccProfileWriteException>(() => embeddedWriter.InsertProfileBytes(
                new byte[0], profile, IccEmbeddedFormat.Png,
                new IccWriteOptions()));
        });
    }

    private static IccTag CreateTag(uint signature, byte value)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Binary;
        tag.Data.BinaryData = new byte[] { value };
        return tag;
    }

    private static void AssertUnsupported(IccOperationResult result)
    {
        Assert.That(result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
        Assert.That(result.Message, Does.Contain("not available"));
        Assert.That(result.Location, Is.Not.Null);
    }
}
