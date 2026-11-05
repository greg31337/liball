using icc;

namespace icc.Tests;

public class PcsAbsoluteConversionTests
{
    [Test]
    public void ColorimetricMediaWhiteMapsBetweenPcsWhiteAndItsAbsoluteValue()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] relativeWhite = new double[] { 0.8, 0.9, 0.7 };
        double[] absoluteWhite = D50();
        double[] mediaRelativeWhite = D50();

        double[] absolute = colorMath.ConvertMediaRelativeToIccAbsolute(mediaRelativeWhite, relativeWhite, absoluteWhite);
        double[] roundTrip = colorMath.ConvertIccAbsoluteToMediaRelative(absolute, relativeWhite, absoluteWhite);

        Assert.Multiple(() =>
        {
            Assert.That(absolute, Is.EqualTo(relativeWhite).Within(1e-15));
            Assert.That(roundTrip, Is.EqualTo(mediaRelativeWhite).Within(1e-15));
            Assert.That(relativeWhite, Is.EqualTo(new double[] { 0.8, 0.9, 0.7 }));
            Assert.That(absoluteWhite, Is.EqualTo(D50()));
        });
    }

    [Test]
    public void ReflectiveAndTransmissiveSpectralPcsUseUnitAbsoluteWhite()
    {
        IIccColorMath colorMath = new IccColorMath();
        uint reflectiveSignature = IccColorSpaceSignatures.ReflectanceSpectralPrefix | 4;
        uint transmissiveSignature = IccColorSpaceSignatures.TransmissionSpectralPrefix | 4;
        double[] relativeWhite = new double[] { 0.8, 0.9, 0.7, 0.6 };
        double[] relativeValues = new double[] { 1.0, 0.5, 2.0, -0.25 };

        double[] reflectiveWhite = PcsConverter.ResolveSpectralAbsoluteWhite(reflectiveSignature, null!);
        double[] transmissiveWhite = PcsConverter.ResolveSpectralAbsoluteWhite(transmissiveSignature, null!);
        double[] absolute = colorMath.ConvertMediaRelativeToIccAbsolute(relativeValues, relativeWhite, reflectiveWhite);
        double[] roundTrip = colorMath.ConvertIccAbsoluteToMediaRelative(absolute, relativeWhite, reflectiveWhite);

        Assert.Multiple(() =>
        {
            Assert.That(reflectiveWhite, Is.EqualTo(new double[] { 1.0, 1.0, 1.0, 1.0 }));
            Assert.That(transmissiveWhite, Is.EqualTo(reflectiveWhite));
            Assert.That(absolute, Is.EqualTo(new double[] { 0.8, 0.45, 1.4, -0.15 }).Within(1e-15));
            Assert.That(roundTrip, Is.EqualTo(relativeValues).Within(1e-15));
        });
    }

    [Test]
    public void EmissiveSpectralPcsUsesThePccIlluminantAsAbsoluteWhite()
    {
        IIccColorMath colorMath = new IccColorMath();
        uint emissiveSignature = IccColorSpaceSignatures.RadiantSpectralPrefix | 3;
        double[] illuminant = new double[] { 2.0, 4.0, 8.0 };
        double[] relativeWhite = new double[] { 1.0, 2.0, 4.0 };
        double[] relativeValues = new double[] { 1.0, 0.5, 2.0 };

        double[] absoluteWhite = PcsConverter.ResolveSpectralAbsoluteWhite(emissiveSignature, illuminant);
        double[] absolute = colorMath.ConvertMediaRelativeToIccAbsolute(relativeValues, relativeWhite, absoluteWhite);
        double[] roundTrip = colorMath.ConvertIccAbsoluteToMediaRelative(absolute, relativeWhite, absoluteWhite);

        Assert.Multiple(() =>
        {
            Assert.That(absoluteWhite, Is.EqualTo(illuminant));
            Assert.That(absolute, Is.EqualTo(new double[] { 0.5, 0.25, 1.0 }).Within(1e-15));
            Assert.That(roundTrip, Is.EqualTo(relativeValues).Within(1e-15));
            Assert.That(absoluteWhite, Is.Not.SameAs(illuminant));
        });
    }

    [Test]
    public void SceneAndExtendedRangeValuesRemainUnclipped()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] sceneEncodingWhite = new double[] { 1.5, 2.0, 1.2 };
        double[] pcsWhite = D50();
        double[] sceneRelative = new double[] { 1.2, 2.5, -0.1 };
        double[] expectedAbsolute = new double[]
        {
            sceneRelative[0] * sceneEncodingWhite[0] / pcsWhite[0],
            sceneRelative[1] * sceneEncodingWhite[1] / pcsWhite[1],
            sceneRelative[2] * sceneEncodingWhite[2] / pcsWhite[2]
        };

        double[] absolute = colorMath.ConvertMediaRelativeToIccAbsolute(sceneRelative, sceneEncodingWhite, pcsWhite);
        double[] roundTrip = colorMath.ConvertIccAbsoluteToMediaRelative(absolute, sceneEncodingWhite, pcsWhite);

        Assert.Multiple(() =>
        {
            Assert.That(absolute, Is.EqualTo(expectedAbsolute).Within(1e-15));
            Assert.That(absolute[0], Is.GreaterThan(1.0));
            Assert.That(absolute[1], Is.EqualTo(5.0));
            Assert.That(absolute[2], Is.LessThan(0.0));
            Assert.That(roundTrip, Is.EqualTo(sceneRelative).Within(1e-15));
        });
    }

    [Test]
    public void AbsoluteRelativeConversionRejectsInvalidVectorsAndUnrepresentableResults()
    {
        IIccColorMath colorMath = new IccColorMath();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.ConvertMediaRelativeToIccAbsolute(null!, new double[] { 1.0 }, new double[] { 1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertMediaRelativeToIccAbsolute(new double[0], new double[0], new double[0]));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertMediaRelativeToIccAbsolute(new double[] { 1.0, 2.0 }, new double[] { 1.0 }, new double[] { 1.0, 1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertMediaRelativeToIccAbsolute(new double[] { double.NaN }, new double[] { 1.0 }, new double[] { 1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertMediaRelativeToIccAbsolute(new double[] { 1.0 }, new double[] { 0.0 }, new double[] { 1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertIccAbsoluteToMediaRelative(new double[] { 1.0 }, new double[] { 1.0 }, new double[] { -1.0 }));
            Assert.Throws<IccTransformException>(() => colorMath.ConvertMediaRelativeToIccAbsolute(new double[] { double.MaxValue }, new double[] { 2.0 }, new double[] { 1.0 }));
        });
    }

    [Test]
    public void SpectralWhiteResolutionRejectsMissingOrUnsupportedMetadata()
    {
        uint emissiveSignature = IccColorSpaceSignatures.RadiantSpectralPrefix | 3;
        uint fluorescentSignature = IccColorSpaceSignatures.BiSpectralPrefix | 3;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => PcsConverter.ResolveSpectralAbsoluteWhite(IccColorSpaceSignatures.ReflectanceSpectralPrefix, null!));
            Assert.Throws<IccTransformException>(() => PcsConverter.ResolveSpectralAbsoluteWhite(emissiveSignature, new double[] { 1.0, 2.0 }));
            Assert.Throws<IccTransformException>(() => PcsConverter.ResolveSpectralAbsoluteWhite(emissiveSignature, new double[] { 1.0, 0.0, 1.0 }));
            Assert.Throws<IccTransformException>(() => PcsConverter.ResolveSpectralAbsoluteWhite(fluorescentSignature, new double[] { 1.0, 1.0, 1.0 }));
            Assert.Throws<IccTransformException>(() => PcsConverter.ResolveSpectralAbsoluteWhite(IccColorSpaceSignatures.Xyz, D50()));
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
