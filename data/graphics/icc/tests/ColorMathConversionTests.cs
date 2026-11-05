using icc;

namespace icc.Tests;

public class ColorMathConversionTests
{
    [Test]
    public void XyzAndLabMatchD50EndpointsAndTheIccPiecewiseBoundary()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] white = D50();
        double[] boundaryXyz = new double[]
        {
            white[0] * IccStandardColorimetry.LabEpsilon,
            white[1] * IccStandardColorimetry.LabEpsilon,
            white[2] * IccStandardColorimetry.LabEpsilon
        };

        double[] whiteLab = colorMath.ConvertXyzToLab(white, white);
        double[] blackLab = colorMath.ConvertXyzToLab(new double[3], white);
        double[] boundaryLab = colorMath.ConvertXyzToLab(boundaryXyz, white);
        double[] boundaryRoundTrip = colorMath.ConvertLabToXyz(boundaryLab, white);

        Assert.Multiple(() =>
        {
            Assert.That(whiteLab, Is.EqualTo(new double[] { 100.0, 0.0, 0.0 }).Within(1e-12));
            Assert.That(blackLab, Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }).Within(1e-12));
            Assert.That(boundaryLab, Is.EqualTo(new double[] { 8.0, 0.0, 0.0 }).Within(1e-12));
            Assert.That(boundaryRoundTrip, Is.EqualTo(boundaryXyz).Within(1e-15));
            Assert.That(white, Is.EqualTo(D50()));
        });
    }

    [Test]
    public void XyzAndLabMatchAnIndependentD65RedVectorAndRoundTripNegativeXyz()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] d65 = new double[]
        {
            IccStandardColorimetry.D65X,
            IccStandardColorimetry.D65Y,
            IccStandardColorimetry.D65Z
        };
        double[] redXyz = new double[] { 0.4124564, 0.2126729, 0.0193339 };
        double[] redLab = colorMath.ConvertXyzToLab(redXyz, d65);
        double[] redRoundTrip = colorMath.ConvertLabToXyz(redLab, d65);
        double[] negativeXyz = new double[] { -0.01, 0.02, -0.03 };
        double[] negativeRoundTrip = colorMath.ConvertLabToXyz(colorMath.ConvertXyzToLab(negativeXyz, D50()), D50());

        Assert.Multiple(() =>
        {
            Assert.That(redLab[0], Is.EqualTo(53.24079414130722).Within(1e-12));
            Assert.That(redLab[1], Is.EqualTo(80.09432789489057).Within(1e-12));
            Assert.That(redLab[2], Is.EqualTo(67.2068339996872).Within(1e-12));
            Assert.That(redRoundTrip, Is.EqualTo(redXyz).Within(1e-14));
            Assert.That(negativeRoundTrip, Is.EqualTo(negativeXyz).Within(1e-14));
        });
    }

    [Test]
    public void XyzAndXyYRoundTripAndDefineBlackWithoutInventingChromaticity()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] xyz = new double[] { 0.25, 0.4, 0.35 };
        double[] xyY = colorMath.ConvertXyzToXyY(xyz);
        double[] roundTrip = colorMath.ConvertXyYToXyz(xyY);
        double[] blackXyY = colorMath.ConvertXyzToXyY(new double[3]);
        double[] blackXyz = colorMath.ConvertXyYToXyz(new double[] { 0.3, 0.0, 0.0 });

        Assert.Multiple(() =>
        {
            Assert.That(xyY, Is.EqualTo(new double[] { 0.25, 0.4, 0.4 }).Within(1e-15));
            Assert.That(roundTrip, Is.EqualTo(xyz).Within(1e-15));
            Assert.That(blackXyY, Is.EqualTo(new double[3]));
            Assert.That(blackXyz, Is.EqualTo(new double[3]));
            Assert.That(xyz, Is.EqualTo(new double[] { 0.25, 0.4, 0.35 }));
        });
    }

    [Test]
    public void LabAndLchNormalizeHueAndKeepAchromaticResultsStable()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] lch = colorMath.ConvertLabToLch(new double[] { 50.0, 40.0, 30.0 });
        double[] roundTrip = colorMath.ConvertLchToLab(lch);
        double[] thirdQuadrant = colorMath.ConvertLabToLch(new double[] { 20.0, -1.0, -System.Math.Sqrt(3.0) });
        double[] normalized = colorMath.ConvertLchToLab(new double[] { 60.0, 20.0, 450.0 });
        double[] achromaticLch = colorMath.ConvertLabToLch(new double[] { 75.0, -0.0, 0.0 });
        double[] achromaticLab = colorMath.ConvertLchToLab(new double[] { 75.0, 0.0, 271.0 });

        Assert.Multiple(() =>
        {
            Assert.That(lch, Is.EqualTo(new double[] { 50.0, 50.0, 36.86989764584402 }).Within(1e-12));
            Assert.That(roundTrip, Is.EqualTo(new double[] { 50.0, 40.0, 30.0 }).Within(1e-12));
            Assert.That(thirdQuadrant[1], Is.EqualTo(2.0).Within(1e-15));
            Assert.That(thirdQuadrant[2], Is.EqualTo(240.0).Within(1e-12));
            Assert.That(normalized, Is.EqualTo(new double[] { 60.0, 0.0, 20.0 }).Within(1e-12));
            Assert.That(achromaticLch, Is.EqualTo(new double[] { 75.0, 0.0, 0.0 }));
            Assert.That(achromaticLab, Is.EqualTo(new double[] { 75.0, 0.0, 0.0 }));
        });
    }

    [Test]
    public void ConversionsRejectInvalidShapesNonFiniteValuesAndSingularCoordinates()
    {
        IIccColorMath colorMath = new IccColorMath();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyzToLab(null!, D50()));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyzToLab(new double[2], D50()));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyzToLab(new double[] { 0.0, double.NaN, 0.0 }, D50()));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyzToLab(new double[3], new double[] { 1.0, 0.0, 1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertLabToXyz(new double[] { double.PositiveInfinity, 0.0, 0.0 }, D50()));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyzToXyY(new double[] { 1.0, -1.0, 0.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertXyYToXyz(new double[] { 0.3, 0.0, 1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertLchToLab(new double[] { 50.0, -1.0, 0.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertLabToLch(new double[] { 50.0, 0.0, double.NegativeInfinity }));
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
}
