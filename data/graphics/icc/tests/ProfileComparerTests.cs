using icc;

namespace icc.Tests;

public class ProfileComparerTests
{
    [Test]
    public void ExactProfilesAgreeAcrossEveryComparisonOperation()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile first = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        IccProfile second = ProfileClone.CloneProfile(first);

        byte[] firstFingerprint = comparer.ComputeProfileFingerprint(first);
        byte[] secondFingerprint = comparer.ComputeProfileFingerprint(second);
        bool exact = comparer.CompareProfilesExactly(first, second);
        double maximum = comparer.CompareProfileSamples(first, second, 13);
        IccComparisonReport report = comparer.CompareProfiles(first, second, 13, 0.0);
        IccOperationResult probe = comparer.ProbeProfileComparison(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(firstFingerprint, Is.EqualTo(secondFingerprint));
            Assert.That(exact, Is.True);
            Assert.That(maximum, Is.Zero);
            Assert.That(report.IsExactMatch, Is.True);
            Assert.That(report.IsApproximatelyEquivalent, Is.True);
            Assert.That(report.FirstFingerprint, Is.EqualTo(firstFingerprint));
            Assert.That(report.SecondFingerprint, Is.EqualTo(secondFingerprint));
            Assert.That(report.Differences, Has.Length.EqualTo(13));
            Assert.That(report.Differences.All(value => value.DeltaE == 0.0), Is.True);
            Assert.That(report.Statistics.Mean, Is.Zero);
            Assert.That(report.Statistics.Maximum, Is.Zero);
            Assert.That(report.Statistics.Percentile95, Is.Zero);
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(report.Result.Message, Does.Contain("substitutable"));
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void SemanticFingerprintIsStableAcrossLayoutAndChangesWithDecodedSemantics()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile first = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        IccProfile reordered = ProfileClone.CloneProfile(first);
        System.Array.Reverse(reordered.Tags);
        IccProfile changed = ProfileClone.CloneProfile(first);
        changed.Header.DeviceManufacturerSignature = 0x74657374;

        byte[] firstFingerprint = comparer.ComputeProfileFingerprint(first);
        byte[] reorderedFingerprint = comparer.ComputeProfileFingerprint(reordered);
        byte[] changedFingerprint = comparer.ComputeProfileFingerprint(changed);

        Assert.Multiple(() =>
        {
            Assert.That(firstFingerprint, Has.Length.EqualTo(32));
            Assert.That(reorderedFingerprint, Is.EqualTo(firstFingerprint));
            Assert.That(changedFingerprint, Is.Not.EqualTo(firstFingerprint));
            Assert.That(comparer.CompareProfilesExactly(first, reordered), Is.True);
            Assert.That(comparer.CompareProfilesExactly(first, changed), Is.False);
        });
    }

    [Test]
    public void SampleComparisonRecognizesEquivalentDifferentCurveRepresentations()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile gamma = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        IccProfile sampled = CreateRgbProfile(CreateSampledCurve(), 1.0);

        IccComparisonReport report = comparer.CompareProfiles(gamma, sampled, 17, 0.000000001);
        double maximum = comparer.CompareProfileSamples(gamma, sampled, 17);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsExactMatch, Is.False);
            Assert.That(report.IsApproximatelyEquivalent, Is.True);
            Assert.That(report.FirstFingerprint, Has.Length.EqualTo(32));
            Assert.That(report.SecondFingerprint, Has.Length.EqualTo(32));
            Assert.That(report.Differences, Has.Length.EqualTo(17));
            Assert.That(report.Differences.All(value => value.Formula == IccColorDifferenceFormula.Ciede2000), Is.True);
            Assert.That(report.Statistics.SampleCount, Is.EqualTo(17));
            Assert.That(report.Statistics.Mean, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.Statistics.Maximum, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(report.Statistics.Percentile95, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(maximum, Is.EqualTo(report.Statistics.Maximum));
            Assert.That(report.TransformPlan, Is.Not.Null);
            Assert.That(report.TransformPlan!.SourceChannels, Is.EqualTo(3));
            Assert.That(report.TransformPlan.DestinationChannels, Is.EqualTo(3));
            Assert.That(report.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(report.Result.Message, Does.Contain("substitutable"));
        });
    }

    [Test]
    public void ComparisonToleranceControlsSubstitutabilityWithoutChangingMeasuredDifference()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile first = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        IccProfile changed = CreateRgbProfile(CreateGammaCurve(1.0), 0.9);

        IccComparisonReport strict = comparer.CompareProfiles(first, changed, 19, 0.01);
        IccComparisonReport lenient = comparer.CompareProfiles(first, changed, 19, 1000.0);

        Assert.Multiple(() =>
        {
            Assert.That(strict.IsExactMatch, Is.False);
            Assert.That(strict.IsApproximatelyEquivalent, Is.False);
            Assert.That(strict.Statistics.Maximum, Is.GreaterThan(0.01));
            Assert.That(lenient.IsApproximatelyEquivalent, Is.True);
            Assert.That(lenient.Statistics.Maximum, Is.EqualTo(strict.Statistics.Maximum));
            Assert.That(strict.Result.Message, Does.Contain("not substitutable"));
        });
    }

    [Test]
    public void SampleComparisonMeasuresDecodedLutOutputDifferences()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile identity = CreateLutProfile(1.0);
        IccProfile reducedRed = CreateLutProfile(0.8);

        double identicalDifference = comparer.CompareProfileSamples(identity, identity, 23);
        IccComparisonReport changed = comparer.CompareProfiles(identity, reducedRed, 23, 0.01);

        Assert.Multiple(() =>
        {
            Assert.That(identicalDifference, Is.EqualTo(0.0).Within(0.000000001));
            Assert.That(changed.Statistics.Maximum, Is.GreaterThan(0.01));
            Assert.That(changed.IsApproximatelyEquivalent, Is.False);
            Assert.That(changed.TransformPlan, Is.Not.Null);
            Assert.That(changed.TransformPlan!.Stages.Any(stage => stage.Kind == IccProcessingElementKind.Clut),
                Is.True);
        });
    }

    [Test]
    public void ProbeAndSampleValidationReportIncompatibleOrInvalidRequests()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile rgb = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        IccProfile gray = ProfileClone.CloneProfile(rgb);
        gray.Header.DataColorSpace = IccColorSpaceKind.Gray;
        gray.Header.DataColorSpaceSignature = IccColorSpaceSignatures.Gray;
        IccProfile missing = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        missing.Tags = new IccTag[0];

        Assert.Multiple(() =>
        {
            Assert.That(comparer.ProbeProfileComparison(rgb, rgb).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(comparer.ProbeProfileComparison(rgb, gray).Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(comparer.ProbeProfileComparison(rgb, missing).Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.Throws<IccProfileValidationException>(() => comparer.CompareProfiles(rgb, rgb, 0, 1.0));
            Assert.Throws<IccProfileValidationException>(() => comparer.CompareProfiles(rgb, rgb, 4, double.NaN));
            Assert.Throws<IccTransformException>(() => comparer.CompareProfileSamples(rgb, gray, 4));
        });
    }

    [Test]
    public void MissingTransformsStillPermitExactComparisonAndFingerprinting()
    {
        IIccProfileComparer comparer = new IccProfileComparer();
        IccProfile first = CreateRgbProfile(CreateGammaCurve(1.0), 1.0);
        first.Tags = new IccTag[0];
        IccProfile second = ProfileClone.CloneProfile(first);

        byte[] firstFingerprint = comparer.ComputeProfileFingerprint(first);
        byte[] secondFingerprint = comparer.ComputeProfileFingerprint(second);
        IccOperationResult probe = comparer.ProbeProfileComparison(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(firstFingerprint, Has.Length.EqualTo(32));
            Assert.That(secondFingerprint, Is.EqualTo(firstFingerprint));
            Assert.That(comparer.CompareProfilesExactly(first, second), Is.True);
            Assert.That(probe.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(probe.Message, Does.Contain("device-to-PCS transform"));
            Assert.Throws<IccTransformException>(() => comparer.CompareProfileSamples(first, second, 4));
            Assert.Throws<IccTransformException>(() => comparer.CompareProfiles(first, second, 4, 1.0));
        });
    }

    private static IccProfile CreateRgbProfile(IccCurve curve, double redScale)
    {
        IccProfile profile = CreateHeaderProfile();
        profile.Tags = new IccTag[]
        {
            CreateCurveTag(IccTagSignatures.RedTrc, ExtendedTagDataClone.CloneCurve(curve, new ProfileCloneContext())),
            CreateCurveTag(IccTagSignatures.GreenTrc, ExtendedTagDataClone.CloneCurve(curve, new ProfileCloneContext())),
            CreateCurveTag(IccTagSignatures.BlueTrc, ExtendedTagDataClone.CloneCurve(curve, new ProfileCloneContext())),
            CreateXyzTag(IccTagSignatures.RedMatrixColumn, redScale, 0.0, 0.0),
            CreateXyzTag(IccTagSignatures.GreenMatrixColumn, 0.0, 1.0, 0.0),
            CreateXyzTag(IccTagSignatures.BlueMatrixColumn, 0.0, 0.0, 1.0)
        };
        return profile;
    }

    private static IccProfile CreateLutProfile(double redScale)
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
            PcsEncoding = IccPcsEncodingKind.Version2,
            Matrix = new IccMatrix
            {
                Rows = 3,
                Columns = 3,
                Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 }
            },
            InputTables = new double[][]
            {
                new double[] { 0.0, 1.0 },
                new double[] { 0.0, 1.0 },
                new double[] { 0.0, 1.0 }
            },
            Clut = new IccClut
            {
                InputChannels = 3,
                OutputChannels = 3,
                GridPoints = new int[] { 2, 2, 2 },
                Values = CreateLutValues(redScale)
            },
            OutputTables = new double[][]
            {
                new double[] { 0.0, 1.0 },
                new double[] { 0.0, 1.0 },
                new double[] { 0.0, 1.0 }
            }
        };
        profile.Tags = new IccTag[] { tag };
        return profile;
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
        profile.Header.CreationDateTime.Year = 2026;
        profile.Header.CreationDateTime.Month = 1;
        profile.Header.CreationDateTime.Day = 2;
        profile.Header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        profile.Header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        profile.Header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        profile.Header.RenderingIntent = (int)IccRenderingIntent.MediaRelativeColorimetric;
        profile.Header.RenderingIntentKind = IccRenderingIntent.MediaRelativeColorimetric;
        return profile;
    }

    private static double[] CreateLutValues(double redScale)
    {
        double[] values = new double[24];
        int point = 0;
        while (point < 8)
        {
            values[point * 3] = ((point >> 2) & 1) * redScale;
            values[(point * 3) + 1] = (point >> 1) & 1;
            values[(point * 3) + 2] = point & 1;
            point = point + 1;
        }
        return values;
    }

    private static IccCurve CreateGammaCurve(double gamma)
    {
        return new IccCurve { Kind = IccCurveKind.Gamma, Gamma = gamma };
    }

    private static IccCurve CreateSampledCurve()
    {
        return new IccCurve
        {
            Kind = IccCurveKind.Sampled,
            Samples = new double[] { 0.0, 0.25, 0.5, 0.75, 1.0 }
        };
    }

    private static IccTag CreateCurveTag(uint signature, IccCurve curve)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data.Kind = IccTagDataKind.Curve;
        tag.Data.Curve = curve;
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
}
