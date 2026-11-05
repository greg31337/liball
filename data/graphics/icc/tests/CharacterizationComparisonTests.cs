using icc;

namespace icc.Tests;

public class CharacterizationComparisonTests
{
    [Test]
    public void ComparisonNormalizesMediaRelativeReferencesAndReturnsPerSampleDifferences()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = CreateProfile(true);
        IccCharacterizationSample white = Sample(
            IccStandardColorimetry.D50X * 0.5,
            IccStandardColorimetry.D50Y * 0.5,
            IccStandardColorimetry.D50Z * 0.5,
            100.0, 0.0, 0.0, true);
        IccCharacterizationSample black = Sample(0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, false);

        IccComparisonReport report = validator.CompareProfileCharacterization(profile,
            new IccCharacterizationSample[] { white, black }, 0.000000001);
        IccComparisonReport unnormalized = validator.CompareProfileCharacterization(profile,
            new IccCharacterizationSample[]
            {
                Sample(white.DeviceColor.Components[0], white.DeviceColor.Components[1],
                    white.DeviceColor.Components[2], 100.0, 0.0, 0.0, false)
            }, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(report.IsApproximatelyEquivalent, Is.True);
            Assert.That(report.Differences, Has.Length.EqualTo(2));
            Assert.That(report.Differences.All(value =>
                value.Formula == IccColorDifferenceFormula.Ciede2000), Is.True);
            Assert.That(report.Differences[0].First.L, Is.LessThan(100.0));
            Assert.That(report.Differences[0].DeltaE, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.Differences[1].DeltaE, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.Statistics.SampleCount, Is.EqualTo(2));
            Assert.That(report.Statistics.Mean, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.Statistics.Maximum, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.Statistics.Percentile95, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.TransformPlan, Is.Not.Null);
            Assert.That(report.TransformPlan!.SourceChannels, Is.EqualTo(3));
            Assert.That(white.ReferenceLab.L, Is.EqualTo(100.0));
            Assert.That(unnormalized.IsApproximatelyEquivalent, Is.False);
            Assert.That(unnormalized.Statistics.Maximum, Is.GreaterThan(1.0));
        });
    }

    [Test]
    public void AbsentCharacterizationIsExplicitlyNotApplicable()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = CreateProfile(true);

        IccComparisonReport missing = validator.CompareProfileCharacterization(profile, null!, 1.0);
        IccComparisonReport empty = validator.CompareProfileCharacterization(profile,
            new IccCharacterizationSample[0], 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(missing.Result.Message, Does.Contain("not applicable"));
            Assert.That(missing.Differences, Is.Empty);
            Assert.That(missing.Statistics.SampleCount, Is.Zero);
            Assert.That(empty.Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(empty.Result.Message, Is.EqualTo(missing.Result.Message));
        });
    }

    [Test]
    public void InvalidPairsAndUnavailableNormalizationReturnSpecificOutcomes()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = CreateProfile(true);
        IccCharacterizationSample mismatch = Sample(0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, false);
        mismatch.DeviceColor.ColorSpaceSignature = IccColorSpaceSignatures.Cmyk;
        IccCharacterizationSample relative = Sample(0.0, 0.0, 0.0,
            0.0, 0.0, 0.0, true);
        IccProfile unsupportedGeneration = CreateProfile(true);
        unsupportedGeneration.Header.Version.Kind = IccVersionKind.Unknown;

        IccComparisonReport conflict = validator.CompareProfileCharacterization(profile,
            new IccCharacterizationSample[] { mismatch }, 1.0);
        IccComparisonReport noWhite = validator.CompareProfileCharacterization(CreateProfile(false),
            new IccCharacterizationSample[] { relative }, 1.0);
        IccComparisonReport unsupported = validator.CompareProfileCharacterization(
            unsupportedGeneration, new IccCharacterizationSample[] { relative }, 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(conflict.Result.Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(conflict.Result.Message, Does.Contain("sample 0"));
            Assert.That(noWhite.Result.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(noWhite.Result.Message, Does.Contain("media-white-point"));
            Assert.That(unsupported.Result.Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.Throws<IccProfileValidationException>(() =>
                validator.CompareProfileCharacterization(profile,
                    new IccCharacterizationSample[] { relative }, double.NaN));
        });
    }

    [Test]
    public void NonzeroComparisonMatchesIndependentMeanMaximumAndNearestRankPercentile()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IIccColorMath colorMath = new IccColorMath();
        IccProfile profile = CreateProfile(true);
        IccCharacterizationSample[] samples = Enumerable.Range(1, 20)
            .Select(value => Sample(0.0, 0.0, 0.0,
                value, 0.0, 0.0, false)).ToArray();
        double[] expected = Enumerable.Range(1, 20).Select(value =>
            colorMath.CalculateColorDifference(
                new double[] { value, 0.0, 0.0 },
                new double[] { 0.0, 0.0, 0.0 },
                IccColorDifferenceFormula.Ciede2000)).ToArray();
        double expectedMean = expected.Sum() / expected.Length;
        double expectedMaximum = expected.Max();
        double expectedPercentile = expected.OrderBy(value => value).ElementAt(18);

        IccComparisonReport strict = validator.CompareProfileCharacterization(
            profile, samples, expectedMaximum - 0.000000001);
        IccComparisonReport boundary = validator.CompareProfileCharacterization(
            profile, samples, expectedMaximum);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(strict.IsApproximatelyEquivalent, Is.False);
            Assert.That(boundary.IsApproximatelyEquivalent, Is.True);
            Assert.That(strict.Differences.Select(value => value.DeltaE),
                Is.EqualTo(expected).Within(0.000000000001));
            Assert.That(strict.Statistics.SampleCount, Is.EqualTo(20));
            Assert.That(strict.Statistics.Mean,
                Is.EqualTo(expectedMean).Within(0.000000000001));
            Assert.That(strict.Statistics.Maximum,
                Is.EqualTo(expectedMaximum).Within(0.000000000001));
            Assert.That(strict.Statistics.Percentile95,
                Is.EqualTo(expectedPercentile).Within(0.000000000001));
            Assert.That(boundary.Differences.Select(value => value.DeltaE),
                Is.EqualTo(strict.Differences.Select(value => value.DeltaE)));
        });
    }

    [Test]
    public void VersionFiveCharacterizationUsesItsForwardMpePipeline()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IIccColorMath colorMath = new IccColorMath();
        IccProfile profile = ProfileQualityTests.CreateVersionFiveIdentityProfile();
        double[] device = new double[] { 0.2, 0.3, 0.4 };
        double[] reference = colorMath.ConvertXyzToLab(device, new double[]
        {
            IccStandardColorimetry.D50X,
            IccStandardColorimetry.D50Y,
            IccStandardColorimetry.D50Z
        });
        IccCharacterizationSample sample = Sample(device[0], device[1], device[2],
            reference[0], reference[1], reference[2], false);

        IccComparisonReport report = validator.CompareProfileCharacterization(profile,
            new IccCharacterizationSample[] { sample }, 0.000000001);

        Assert.Multiple(() =>
        {
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(report.IsApproximatelyEquivalent, Is.True);
            Assert.That(report.Differences, Has.Length.EqualTo(1));
            Assert.That(report.Differences[0].DeltaE, Is.LessThan(0.000000001));
            Assert.That(report.TransformPlan!.Stages, Has.Length.EqualTo(1));
        });
    }

    private static IccCharacterizationSample Sample(double red, double green, double blue,
        double lightness, double a, double b, bool mediaRelative)
    {
        IccCharacterizationSample sample = new IccCharacterizationSample();
        sample.DeviceColor.ColorSpace = IccColorSpaceKind.Rgb;
        sample.DeviceColor.ColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        sample.DeviceColor.Components = new double[] { red, green, blue };
        sample.ReferenceLab.L = lightness;
        sample.ReferenceLab.A = a;
        sample.ReferenceLab.B = b;
        sample.ReferenceIsMediaRelative = mediaRelative;
        return sample;
    }

    private static IccProfile CreateProfile(bool includeMediaWhite)
    {
        IccProfile profile = new IccProfile();
        profile.Header.Version.Kind = IccVersionKind.Version4;
        profile.Header.Version.Major = 4;
        profile.Header.Version.Minor = 4;
        profile.Header.ProfileClass = IccProfileClass.Display;
        profile.Header.ProfileClassSignature = IccProfileClassSignatures.Display;
        profile.Header.DataColorSpace = IccColorSpaceKind.Rgb;
        profile.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Rgb;
        profile.Header.ProfileConnectionSpace = IccPcsKind.Xyz;
        profile.Header.ProfileConnectionSpaceSignature = IccColorSpaceSignatures.Xyz;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        profile.Header.RenderingIntent = (int)IccRenderingIntent.MediaRelativeColorimetric;
        profile.Header.RenderingIntentKind = IccRenderingIntent.MediaRelativeColorimetric;
        System.Collections.Generic.List<IccTag> tags = new System.Collections.Generic.List<IccTag>();
        tags.Add(CurveTag(IccTagSignatures.RedTrc));
        tags.Add(CurveTag(IccTagSignatures.GreenTrc));
        tags.Add(CurveTag(IccTagSignatures.BlueTrc));
        tags.Add(XyzTag(IccTagSignatures.RedMatrixColumn, 1.0, 0.0, 0.0));
        tags.Add(XyzTag(IccTagSignatures.GreenMatrixColumn, 0.0, 1.0, 0.0));
        tags.Add(XyzTag(IccTagSignatures.BlueMatrixColumn, 0.0, 0.0, 1.0));
        if (includeMediaWhite)
        {
            tags.Add(XyzTag(IccTagSignatures.MediaWhitePoint,
                IccStandardColorimetry.D50X * 0.5,
                IccStandardColorimetry.D50Y * 0.5,
                IccStandardColorimetry.D50Z * 0.5));
        }
        profile.Tags = tags.ToArray();
        return profile;
    }

    private static IccTag CurveTag(uint signature)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Curve;
        tag.Data.Curve = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 1.0 };
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = IccTypeSignatures.Curve;
        return tag;
    }

    private static IccTag XyzTag(uint signature, double x, double y, double z)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.XyzArray;
        tag.Data.XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } };
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = IccTypeSignatures.Xyz;
        return tag;
    }
}
