using icc;

namespace icc.Tests;

public class ProfileQualityTests
{
    [Test]
    public void QualityAnalysisReportsDeterministicFirstAndSecondRoundTrips()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = CreateRgbProfile(1.0);

        IccQualityStatistics first = validator.AnalyzeProfileQuality(profile, 29);
        IccQualityStatistics second = validator.AnalyzeProfileQuality(profile, 29);

        Assert.Multiple(() =>
        {
            Assert.That(first.SampleCount, Is.EqualTo(29));
            Assert.That(first.FirstRoundTripMean, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(first.FirstRoundTripMaximum, Is.GreaterThanOrEqualTo(first.FirstRoundTripPercentile95));
            Assert.That(first.FirstRoundTripMaximum, Is.GreaterThanOrEqualTo(first.FirstRoundTripMean));
            Assert.That(first.SecondRoundTripMean, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(first.SecondRoundTripMaximum, Is.GreaterThanOrEqualTo(first.SecondRoundTripPercentile95));
            Assert.That(first.SecondRoundTripMaximum, Is.GreaterThanOrEqualTo(first.SecondRoundTripMean));
            Assert.That(first.Maximum, Is.EqualTo(System.Math.Max(first.FirstRoundTripMaximum,
                first.SecondRoundTripMaximum)));
            Assert.That(first.CurveCount, Is.EqualTo(6));
            Assert.That(first.CurvesAreContinuous, Is.True);
            Assert.That(first.CurvesAreInvertible, Is.True);
            Assert.That(first.CurveMaximumRoundTripError, Is.LessThan(0.000000000001));
            Assert.That(first.TransformContinuityMaximum, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(first.TransformSmoothnessMaximum, Is.GreaterThanOrEqualTo(
                first.TransformSmoothnessPercentile95));
            Assert.That(first.TransformSmoothnessMaximum, Is.GreaterThanOrEqualTo(
                first.TransformSmoothnessMean));
            AssertStatisticsEqual(first, second);
        });
    }

    [Test]
    public void RoundTripAggregationCalculatesMeanMaximumAndNearestRankPercentile95()
    {
        double[] firstValues = Enumerable.Range(1, 20).Select(value => (double)value).ToArray();
        double[] secondValues = Enumerable.Range(1, 20).Select(value => value * 2.0).ToArray();
        IccQualityStatistics statistics = new IccQualityStatistics();

        QualityValidator.PopulateRoundTripStatistics(statistics, firstValues, secondValues);

        Assert.Multiple(() =>
        {
            Assert.That(statistics.FirstRoundTripMean, Is.EqualTo(10.5));
            Assert.That(statistics.FirstRoundTripMaximum, Is.EqualTo(20.0));
            Assert.That(statistics.FirstRoundTripPercentile95, Is.EqualTo(19.0));
            Assert.That(statistics.SecondRoundTripMean, Is.EqualTo(21.0));
            Assert.That(statistics.SecondRoundTripMaximum, Is.EqualTo(40.0));
            Assert.That(statistics.SecondRoundTripPercentile95, Is.EqualTo(38.0));
            Assert.That(statistics.Mean, Is.EqualTo(15.75));
            Assert.That(statistics.Maximum, Is.EqualTo(40.0));
            Assert.That(statistics.Percentile95, Is.EqualTo(36.0));
        });
    }

    [Test]
    public void SmoothnessAggregationCalculatesIndependentGoldenStatistics()
    {
        double[] values = Enumerable.Range(1, 20).Select(value => (double)value).ToArray();
        IccQualityStatistics statistics = new IccQualityStatistics();

        QualityValidator.PopulateSmoothnessStatistics(statistics, values, 42.0);

        Assert.Multiple(() =>
        {
            Assert.That(statistics.TransformContinuityMaximum, Is.EqualTo(42.0));
            Assert.That(statistics.TransformSmoothnessMean, Is.EqualTo(10.5));
            Assert.That(statistics.TransformSmoothnessMaximum, Is.EqualTo(20.0));
            Assert.That(statistics.TransformSmoothnessPercentile95, Is.EqualTo(19.0));
        });
    }

    [Test]
    public void QualitySamplingUsesExactDeterministicEndpointAndRadicalInverseVectors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QualityValidator.CreateLabSample(0, 5),
                Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }));
            Assert.That(QualityValidator.CreateLabSample(1, 5),
                Is.EqualTo(new double[] { 100.0, 0.0, 0.0 }));
            Assert.That(QualityValidator.CreateLabSample(2, 5),
                Is.EqualTo(new double[]
                {
                    50.0, (255.0 / 3.0) - 128.0, (255.0 / 5.0) - 128.0
                }).Within(0.000000000001));
            Assert.That(ProfileSampleComparison.CreateSample(0, 5, 3),
                Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }));
            Assert.That(ProfileSampleComparison.CreateSample(1, 5, 3),
                Is.EqualTo(new double[] { 1.0, 1.0, 1.0 }));
            Assert.That(ProfileSampleComparison.CreateSample(2, 5, 3),
                Is.EqualTo(new double[] { 0.5, 1.0 / 3.0, 1.0 / 5.0 })
                    .Within(0.000000000001));
        });
    }

    [Test]
    public void PlateauCurvesReportFailedInverseRoundTripsWithoutBlockingProfileSampling()
    {
        IccProfile profile = CreateRgbProfile(1.0);
        int index = 0;
        while (index < 3)
        {
            profile.Tags[index].Data.Curve = new IccCurve
            {
                Kind = IccCurveKind.Sampled,
                Samples = new double[] { 0.0, 0.25, 0.25, 1.0 }
            };
            index++;
        }

        IccQualityStatistics statistics = new IccProfileValidator()
            .AnalyzeProfileQuality(profile, 17);

        Assert.Multiple(() =>
        {
            Assert.That(statistics.SampleCount, Is.EqualTo(17));
            Assert.That(statistics.CurveCount, Is.EqualTo(6));
            Assert.That(statistics.CurvesAreContinuous, Is.True);
            Assert.That(statistics.CurvesAreInvertible, Is.False);
            Assert.That(statistics.CurveMaximumRoundTripError, Is.GreaterThan(0.0));
            Assert.That(statistics.Maximum, Is.GreaterThanOrEqualTo(statistics.Mean));
        });
    }

    [Test]
    public void VersionFiveBidirectionalMpeSupportsDeterministicQualityAnalysis()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile profile = CreateVersionFiveIdentityProfile();

        IccOperationResult probe = validator.ProbeProfileQuality(profile);
        IccQualityStatistics first = validator.AnalyzeProfileQuality(profile, 19);
        IccQualityStatistics second = validator.AnalyzeProfileQuality(profile, 19);

        Assert.Multiple(() =>
        {
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(first.SampleCount, Is.EqualTo(19));
            Assert.That(first.CurveCount, Is.Zero);
            Assert.That(first.FirstRoundTripMaximum, Is.LessThan(0.000000001));
            Assert.That(first.SecondRoundTripMaximum, Is.LessThan(0.000000001));
            AssertStatisticsEqual(first, second);
        });
    }

    [Test]
    public void SmoothnessMetricRespondsToCurveCurvature()
    {
        IIccProfileValidator validator = new IccProfileValidator();

        IccQualityStatistics linear = validator.AnalyzeProfileQuality(CreateRgbProfile(1.0), 31);
        IccQualityStatistics curved = validator.AnalyzeProfileQuality(CreateRgbProfile(0.25), 31);

        Assert.Multiple(() =>
        {
            Assert.That(linear.TransformSmoothnessMaximum, Is.LessThan(0.001));
            Assert.That(curved.TransformSmoothnessMaximum,
                Is.GreaterThan(linear.TransformSmoothnessMaximum));
            Assert.That(curved.TransformContinuityMaximum, Is.GreaterThan(0.0));
        });
    }

    [Test]
    public void QualityProbeAndSampleBoundsDescribeNotApplicableRequests()
    {
        IIccProfileValidator validator = new IccProfileValidator();
        IccProfile bidirectional = CreateRgbProfile(1.0);
        IccProfile oneWay = CreateOneWayLutProfile();
        IccProfile unsupported = CreateRgbProfile(1.0);
        unsupported.Header.ProfileClass = IccProfileClass.DeviceLink;
        IccProfile unsupportedGeneration = CreateRgbProfile(1.0);
        unsupportedGeneration.Header.Version.Kind = IccVersionKind.Unknown;

        Assert.Multiple(() =>
        {
            Assert.That(validator.ProbeProfileQuality(bidirectional).Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(validator.ProbeProfileQuality(oneWay).Status,
                Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(validator.ProbeProfileQuality(unsupported).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(validator.ProbeProfileQuality(unsupportedGeneration).Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.Throws<IccProfileValidationException>(() =>
                validator.AnalyzeProfileQuality(bidirectional, 2));
            Assert.Throws<IccTransformException>(() =>
                validator.AnalyzeProfileQuality(oneWay, 17));
            Assert.Throws<IccTransformException>(() =>
                validator.AnalyzeProfileQuality(unsupportedGeneration, 17));
        });
    }

    private static void AssertStatisticsEqual(IccQualityStatistics first, IccQualityStatistics second)
    {
        Assert.That(second.Mean, Is.EqualTo(first.Mean));
        Assert.That(second.Maximum, Is.EqualTo(first.Maximum));
        Assert.That(second.Percentile95, Is.EqualTo(first.Percentile95));
        Assert.That(second.FirstRoundTripMean, Is.EqualTo(first.FirstRoundTripMean));
        Assert.That(second.FirstRoundTripMaximum, Is.EqualTo(first.FirstRoundTripMaximum));
        Assert.That(second.FirstRoundTripPercentile95, Is.EqualTo(first.FirstRoundTripPercentile95));
        Assert.That(second.SecondRoundTripMean, Is.EqualTo(first.SecondRoundTripMean));
        Assert.That(second.SecondRoundTripMaximum, Is.EqualTo(first.SecondRoundTripMaximum));
        Assert.That(second.SecondRoundTripPercentile95, Is.EqualTo(first.SecondRoundTripPercentile95));
        Assert.That(second.TransformContinuityMaximum, Is.EqualTo(first.TransformContinuityMaximum));
        Assert.That(second.TransformSmoothnessMean, Is.EqualTo(first.TransformSmoothnessMean));
        Assert.That(second.TransformSmoothnessMaximum, Is.EqualTo(first.TransformSmoothnessMaximum));
        Assert.That(second.TransformSmoothnessPercentile95,
            Is.EqualTo(first.TransformSmoothnessPercentile95));
    }

    private static IccProfile CreateRgbProfile(double gamma)
    {
        IccProfile profile = CreateHeaderProfile();
        profile.Tags = new IccTag[]
        {
            CreateCurveTag(IccTagSignatures.RedTrc, gamma),
            CreateCurveTag(IccTagSignatures.GreenTrc, gamma),
            CreateCurveTag(IccTagSignatures.BlueTrc, gamma),
            CreateXyzTag(IccTagSignatures.RedMatrixColumn, 1.0, 0.0, 0.0),
            CreateXyzTag(IccTagSignatures.GreenMatrixColumn, 0.0, 1.0, 0.0),
            CreateXyzTag(IccTagSignatures.BlueMatrixColumn, 0.0, 0.0, 1.0)
        };
        return profile;
    }

    private static IccProfile CreateOneWayLutProfile()
    {
        IccProfile profile = CreateHeaderProfile();
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.AToB1;
        tag.TableEntry.Signature = tag.Signature;
        tag.Data.Kind = IccTagDataKind.Lut;
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = IccTypeSignatures.Lut16;
        tag.Data.Lut = new IccLut
        {
            InputChannels = 3,
            OutputChannels = 3,
            PcsEncoding = IccPcsEncodingKind.Version4,
            Matrix = IdentityMatrix(),
            InputTables = IdentityTables(),
            Clut = new IccClut
            {
                InputChannels = 3,
                OutputChannels = 3,
                GridPoints = new int[] { 2, 2, 2 },
                Values = IdentityClutValues()
            },
            OutputTables = IdentityTables()
        };
        profile.Tags = new IccTag[] { tag };
        return profile;
    }

    internal static IccProfile CreateVersionFiveIdentityProfile()
    {
        IccProfile profile = CreateHeaderProfile();
        profile.Header.Version.Kind = IccVersionKind.Version5;
        profile.Header.Version.Major = 5;
        profile.Header.Version.Minor = 0;
        profile.Tags = new IccTag[]
        {
            CreateVersionFiveMatrixTag(IccTagSignatures.AToB1),
            CreateVersionFiveMatrixTag(IccTagSignatures.BToA1)
        };
        return profile;
    }

    private static IccTag CreateVersionFiveMatrixTag(uint signature)
    {
        IccProcessingElement matrix = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = 3,
            OutputChannels = 3,
            Matrix = new IccMatrix
            {
                Rows = 3,
                Columns = 4,
                Values = new double[]
                {
                    1.0, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0
                }
            }
        };
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.MultiProcessElements;
        tag.Data.MultiProcessElements = new IccMultiProcessElements
        {
            InputChannels = 3,
            OutputChannels = 3,
            InputRanges = FullRanges(3),
            OutputRanges = FullRanges(3),
            Elements = new IccProcessingElement[] { matrix },
            IsExecutable = true
        };
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = IccTypeSignatures.MultiProcessElements;
        return tag;
    }

    private static IccEncodingRange[] FullRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange
            {
                Minimum = float.MinValue,
                Maximum = float.MaxValue
            };
            index++;
        }
        return ranges;
    }

    private static IccProfile CreateHeaderProfile()
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
        return profile;
    }

    private static IccTag CreateCurveTag(uint signature, double gamma)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Curve;
        tag.Data.Curve = new IccCurve { Kind = IccCurveKind.Gamma, Gamma = gamma };
        tag.Data.RawData.HasTypeSignature = true;
        tag.Data.RawData.TypeSignature = IccTypeSignatures.Curve;
        return tag;
    }

    private static IccTag CreateXyzTag(uint signature, double x, double y, double z)
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

    private static IccMatrix IdentityMatrix()
    {
        return new IccMatrix
        {
            Rows = 3,
            Columns = 3,
            Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
        };
    }

    private static double[][] IdentityTables()
    {
        return new double[][]
        {
            new double[] { 0.0, 1.0 },
            new double[] { 0.0, 1.0 },
            new double[] { 0.0, 1.0 }
        };
    }

    private static double[] IdentityClutValues()
    {
        double[] values = new double[24];
        int point = 0;
        while (point < 8)
        {
            values[point * 3] = (point >> 2) & 1;
            values[(point * 3) + 1] = (point >> 1) & 1;
            values[(point * 3) + 2] = point & 1;
            point = point + 1;
        }
        return values;
    }
}
