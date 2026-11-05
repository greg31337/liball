using icc;

namespace icc.Tests;

public class ChromaticAdaptationTests
{
    [Test]
    public void BradfordD65ToD50MatchesThePublishedIccMatrix()
    {
        IIccChromaticAdapter adapter = new IccChromaticAdapter();
        double[] d65 = new double[] { 0.9504, 1.0, 1.0889 };
        double[] d50 = D50();
        double[] expected = new double[]
        {
            1.04790738171017, 0.0229333845542104, -0.0502016347980104,
            0.0296059594177168, 0.990456039910785, -0.01707552919587,
            -0.00924679432678241, 0.0150626801401488, 0.751791232609078
        };

        double[] matrix = adapter.BuildBradfordMatrix(d65, d50);

        Assert.Multiple(() =>
        {
            Assert.That(matrix, Is.EqualTo(expected).Within(2e-14));
            Assert.That(adapter.ApplyAdaptationMatrix(matrix, d65), Is.EqualTo(d50).Within(2e-15));
            Assert.That(d65, Is.EqualTo(new double[] { 0.9504, 1.0, 1.0889 }));
            Assert.That(d50, Is.EqualTo(D50()));
        });
    }

    [Test]
    public void BradfordConstructionUsesWhiteChromaticityRatherThanLuminanceScale()
    {
        IIccChromaticAdapter adapter = new IccChromaticAdapter();
        double[] scaledD65 = new double[]
        {
            IccStandardColorimetry.D65X * 100.0,
            IccStandardColorimetry.D65Y * 100.0,
            IccStandardColorimetry.D65Z * 100.0
        };
        double[] scaledD50 = new double[]
        {
            IccStandardColorimetry.D50X * 25.0,
            IccStandardColorimetry.D50Y * 25.0,
            IccStandardColorimetry.D50Z * 25.0
        };

        double[] normalizedMatrix = adapter.BuildBradfordMatrix(D65(), D50());
        double[] scaledMatrix = adapter.BuildBradfordMatrix(scaledD65, scaledD50);
        double[] identity = adapter.BuildBradfordMatrix(D65(), scaledD65);

        Assert.Multiple(() =>
        {
            Assert.That(scaledMatrix, Is.EqualTo(normalizedMatrix).Within(2e-15));
            Assert.That(identity, Is.EqualTo(Identity3x3()));
        });
    }

    [Test]
    public void ExplicitChadApplicationAndInversionRoundTripExtendedXyz()
    {
        IIccChromaticAdapter adapter = new IccChromaticAdapter();
        double[] chad = adapter.BuildBradfordMatrix(D65(), D50());
        double[] inverse = adapter.InvertAdaptationMatrix(chad);
        double[] xyz = new double[] { -0.02, 0.4, 1.25 };

        double[] adapted = adapter.ApplyAdaptationMatrix(chad, xyz);
        double[] roundTrip = adapter.ApplyAdaptationMatrix(inverse, adapted);

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip, Is.EqualTo(xyz).Within(2e-15));
            Assert.That(adapted[0], Is.LessThan(0.0));
            Assert.That(xyz, Is.EqualTo(new double[] { -0.02, 0.4, 1.25 }));
            Assert.That(MatrixMath.MultiplyMatrices3x3(inverse, chad), Is.EqualTo(Identity3x3()).Within(2e-15));
        });
    }

    [Test]
    public void D50ConveniencesAdaptInBothDirections()
    {
        double[] d65 = D65();
        double[] xyz = new double[] { 0.25, 0.4, 0.1 };
        double[] toD50 = ChromaticAdaptationCore.BuildToD50Matrix(d65);
        double[] fromD50 = ChromaticAdaptationCore.BuildFromD50Matrix(d65);

        double[] adaptedWhite = ChromaticAdaptationCore.AdaptToD50(d65, d65);
        double[] adapted = ChromaticAdaptationCore.AdaptToD50(xyz, d65);
        double[] roundTrip = ChromaticAdaptationCore.AdaptFromD50(adapted, d65);

        Assert.Multiple(() =>
        {
            Assert.That(adaptedWhite, Is.EqualTo(D50()).Within(2e-15));
            Assert.That(roundTrip, Is.EqualTo(xyz).Within(2e-15));
            Assert.That(MatrixMath.MultiplyMatrices3x3(fromD50, toD50), Is.EqualTo(Identity3x3()).Within(2e-15));
        });
    }

    [Test]
    public void ChadConvertsMediaWhiteBetweenActualAndPcsAdoptedConditions()
    {
        double[] chad = new double[]
        {
            1.04790738171017, 0.0229333845542104, -0.0502016347980104,
            0.0296059594177168, 0.990456039910785, -0.01707552919587,
            -0.00924679432678241, 0.0150626801401488, 0.751791232609078
        };
        double[] mediaWhite = new double[] { 0.8, 0.9, 0.7 };

        double[] pcsWhite = ChromaticAdaptationCore.ConvertMediaWhiteToPcsAdoptedWhite(mediaWhite, chad);
        double[] recovered = ChromaticAdaptationCore.ConvertPcsAdoptedWhiteToMediaWhite(pcsWhite, chad);

        Assert.Multiple(() =>
        {
            Assert.That(pcsWhite, Is.EqualTo(new double[] { 0.8238248071083182, 0.9031423330167709, 0.5324128394910626 }).Within(2e-15));
            Assert.That(recovered, Is.EqualTo(mediaWhite).Within(2e-15));
        });
    }

    [Test]
    public void ChromaticAdaptationRejectsInvalidWhitesMatricesAndValues()
    {
        IIccChromaticAdapter adapter = new IccChromaticAdapter();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => adapter.BuildBradfordMatrix(null!, D50()));
            Assert.Throws<IccTransformException>(() => adapter.BuildBradfordMatrix(new double[2], D50()));
            Assert.Throws<IccTransformException>(() => adapter.BuildBradfordMatrix(new double[] { 1.0, 0.0, 1.0 }, D50()));
            Assert.Throws<IccTransformException>(() => adapter.BuildBradfordMatrix(new double[] { 100.0, 1.0, 1.0 }, D50()));
            Assert.Throws<IccTransformException>(() => adapter.BuildBradfordMatrix(new double[] { double.MaxValue, double.Epsilon, 1.0 }, D50()));
            Assert.Throws<IccTransformException>(() => adapter.ApplyAdaptationMatrix(new double[8], new double[3]));
            Assert.Throws<IccTransformException>(() => adapter.ApplyAdaptationMatrix(Identity3x3(), new double[] { 0.0, double.NaN, 0.0 }));
            Assert.Throws<IccTransformException>(() => adapter.InvertAdaptationMatrix(new double[9]));
        });
    }

    private static double[] D50()
    {
        return new double[]
        {
            IccStandardColorimetry.D50X,
            IccStandardColorimetry.D50Y,
            IccStandardColorimetry.D50Z
        };
    }

    private static double[] D65()
    {
        return new double[]
        {
            IccStandardColorimetry.D65X,
            IccStandardColorimetry.D65Y,
            IccStandardColorimetry.D65Z
        };
    }

    private static double[] Identity3x3()
    {
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        };
    }
}
