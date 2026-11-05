using icc;

namespace icc.Tests;

public class PcsEncodingTests
{
    [Test]
    public void Version2LabUsesLegacyIntegerScaleAndRoundTripsWithinItsPrecision()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] lab = new double[] { 42.123, -36.789, 99.456 };
        ushort[] anchors = colorMath.EncodePcsUInt16(
            new double[] { 100.0, 0.0, (65535.0 / 256.0) - 128.0 },
            IccPcsKind.Lab,
            IccPcsEncodingKind.Version2);
        ushort[] encoded = colorMath.EncodePcsUInt16(lab, IccPcsKind.Lab, IccPcsEncodingKind.Version2);
        double[] decoded = colorMath.DecodePcsUInt16(encoded, IccPcsKind.Lab, IccPcsEncodingKind.Version2);

        Assert.Multiple(() =>
        {
            Assert.That(anchors, Is.EqualTo(new ushort[] { 65280, 32768, 65535 }));
            Assert.That(decoded[0], Is.EqualTo(lab[0]).Within((100.0 / 65280.0) / 2.0 + 1e-12));
            Assert.That(decoded[1], Is.EqualTo(lab[1]).Within((1.0 / 256.0) / 2.0 + 1e-12));
            Assert.That(decoded[2], Is.EqualTo(lab[2]).Within((1.0 / 256.0) / 2.0 + 1e-12));
            Assert.That(lab, Is.EqualTo(new double[] { 42.123, -36.789, 99.456 }));
        });
    }

    [Test]
    public void Version4LabUsesFullRangeIntegerScaleAndRoundTripsWithinItsPrecision()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] lab = new double[] { 42.123, -36.789, 99.456 };
        ushort[] anchors = colorMath.EncodePcsUInt16(
            new double[] { 100.0, 0.0, 127.0 },
            IccPcsKind.Lab,
            IccPcsEncodingKind.Version4);
        ushort[] encoded = colorMath.EncodePcsUInt16(lab, IccPcsKind.Lab, IccPcsEncodingKind.Version4);
        double[] decoded = colorMath.DecodePcsUInt16(encoded, IccPcsKind.Lab, IccPcsEncodingKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(anchors, Is.EqualTo(new ushort[] { 65535, 32896, 65535 }));
            Assert.That(decoded[0], Is.EqualTo(lab[0]).Within((100.0 / 65535.0) / 2.0 + 1e-12));
            Assert.That(decoded[1], Is.EqualTo(lab[1]).Within((255.0 / 65535.0) / 2.0 + 1e-12));
            Assert.That(decoded[2], Is.EqualTo(lab[2]).Within((255.0 / 65535.0) / 2.0 + 1e-12));
        });
    }

    [Test]
    public void XyzIntegerEncodingUsesU1Fixed15AndClipsUnrepresentableValues()
    {
        IIccColorMath colorMath = new IccColorMath();
        double maximum = 65535.0 / 32768.0;
        double[] xyz = new double[] { -0.25, 1.0, 2.5 };
        ushort[] version2 = colorMath.EncodePcsUInt16(xyz, IccPcsKind.Xyz, IccPcsEncodingKind.Version2);
        ushort[] version4 = colorMath.EncodePcsUInt16(xyz, IccPcsKind.Xyz, IccPcsEncodingKind.Version4);
        double[] decoded = colorMath.DecodePcsUInt16(version4, IccPcsKind.Xyz, IccPcsEncodingKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(version2, Is.EqualTo(new ushort[] { 0, 32768, 65535 }));
            Assert.That(version4, Is.EqualTo(version2));
            Assert.That(decoded, Is.EqualTo(new double[] { 0.0, 1.0, maximum }).Within(1e-15));
            Assert.That(xyz, Is.EqualTo(new double[] { -0.25, 1.0, 2.5 }));
        });
    }

    [Test]
    public void LabIntegerEncodingClipsToEachGenerationsRepresentablePcsRange()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] outside = new double[] { 110.0, -200.0, 200.0 };

        ushort[] version2 = colorMath.EncodePcsUInt16(outside, IccPcsKind.Lab, IccPcsEncodingKind.Version2);
        ushort[] version4 = colorMath.EncodePcsUInt16(outside, IccPcsKind.Lab, IccPcsEncodingKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(version2, Is.EqualTo(new ushort[] { 65280, 0, 65535 }));
            Assert.That(version4, Is.EqualTo(new ushort[] { 65535, 0, 65535 }));
        });
    }

    [Test]
    public void NormalizationPreservesGenerationSpecificLabScalesWithoutQuantizing()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] lab = new double[] { 73.125, -20.25, 81.75 };
        double[] version2 = colorMath.NormalizePcs(lab, IccPcsKind.Lab, IccPcsEncodingKind.Version2);
        double[] version4 = colorMath.NormalizePcs(lab, IccPcsKind.Lab, IccPcsEncodingKind.Version4);
        double[] version2RoundTrip = colorMath.DenormalizePcs(version2, IccPcsKind.Lab, IccPcsEncodingKind.Version2);
        double[] version4RoundTrip = colorMath.DenormalizePcs(version4, IccPcsKind.Lab, IccPcsEncodingKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(version2[0], Is.EqualTo(lab[0] * 652.8 / 65535.0).Within(1e-15));
            Assert.That(version4[0], Is.EqualTo(lab[0] / 100.0).Within(1e-15));
            Assert.That(version2RoundTrip, Is.EqualTo(lab).Within(1e-12));
            Assert.That(version4RoundTrip, Is.EqualTo(lab).Within(1e-12));
        });
    }

    [Test]
    public void EveryValidSelectedIntegerCodeRoundTripsExactly()
    {
        IIccColorMath colorMath = new IccColorMath();
        ushort[][] version2LabCodes = new ushort[][]
        {
            new ushort[] { 0, 0, 0 },
            new ushort[] { 12345, 32768, 54321 },
            new ushort[] { 65280, 65535, 65535 }
        };
        ushort[][] fullRangeCodes = new ushort[][]
        {
            new ushort[] { 0, 0, 0 },
            new ushort[] { 1, 32768, 65534 },
            new ushort[] { 65535, 65535, 65535 }
        };

        int index = 0;
        while (index < version2LabCodes.Length)
        {
            double[] decoded = colorMath.DecodePcsUInt16(version2LabCodes[index], IccPcsKind.Lab, IccPcsEncodingKind.Version2);
            Assert.That(colorMath.EncodePcsUInt16(decoded, IccPcsKind.Lab, IccPcsEncodingKind.Version2), Is.EqualTo(version2LabCodes[index]));
            index++;
        }

        index = 0;
        while (index < fullRangeCodes.Length)
        {
            double[] decodedLab = colorMath.DecodePcsUInt16(fullRangeCodes[index], IccPcsKind.Lab, IccPcsEncodingKind.Version4);
            double[] decodedXyz = colorMath.DecodePcsUInt16(fullRangeCodes[index], IccPcsKind.Xyz, IccPcsEncodingKind.Version4);
            Assert.That(colorMath.EncodePcsUInt16(decodedLab, IccPcsKind.Lab, IccPcsEncodingKind.Version4), Is.EqualTo(fullRangeCodes[index]));
            Assert.That(colorMath.EncodePcsUInt16(decodedXyz, IccPcsKind.Xyz, IccPcsEncodingKind.Version4), Is.EqualTo(fullRangeCodes[index]));
            index++;
        }
    }

    [Test]
    public void Float32PcsKeepsNegativeXyzAndDoesNotApplyIntegerClipping()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] xyz = new double[] { -0.125, 1.234567890123, 2.5 };

        float[] encoded = colorMath.EncodePcsFloat32(xyz, IccPcsKind.Xyz);
        double[] decoded = colorMath.DecodePcsFloat32(encoded, IccPcsKind.Xyz);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new float[] { -0.125f, (float)1.234567890123, 2.5f }));
            Assert.That(decoded, Is.EqualTo(new double[] { -0.125, (double)(float)1.234567890123, 2.5 }));
            Assert.That(decoded[2], Is.GreaterThan(65535.0 / 32768.0));
            Assert.That(xyz, Is.EqualTo(new double[] { -0.125, 1.234567890123, 2.5 }));
        });
    }

    [Test]
    public void PcsEncodingRejectsInvalidShapesKindsEncodingsAndNonFiniteValues()
    {
        IIccColorMath colorMath = new IccColorMath();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.EncodePcsUInt16(null!, IccPcsKind.Xyz, IccPcsEncodingKind.Version4));
            Assert.Throws<IccTransformException>(() => colorMath.EncodePcsUInt16(new double[2], IccPcsKind.Xyz, IccPcsEncodingKind.Version4));
            Assert.Throws<IccTransformException>(() => colorMath.EncodePcsUInt16(new double[] { 0.0, double.NaN, 0.0 }, IccPcsKind.Xyz, IccPcsEncodingKind.Version4));
            Assert.Throws<IccTransformException>(() => colorMath.EncodePcsUInt16(new double[3], IccPcsKind.None, IccPcsEncodingKind.Version4));
            Assert.Throws<IccTransformException>(() => colorMath.EncodePcsUInt16(new double[3], IccPcsKind.Xyz, IccPcsEncodingKind.Normalized));
            Assert.Throws<IccTransformException>(() => colorMath.DecodePcsUInt16(new ushort[] { 65281, 0, 0 }, IccPcsKind.Lab, IccPcsEncodingKind.Version2));
            Assert.Throws<IccTransformException>(() => colorMath.EncodePcsFloat32(new double[] { 0.0, double.MaxValue, 0.0 }, IccPcsKind.Xyz));
            Assert.Throws<IccTransformException>(() => colorMath.DecodePcsFloat32(new float[] { 0.0f, float.PositiveInfinity, 0.0f }, IccPcsKind.Xyz));
            Assert.Throws<IccTransformException>(() => colorMath.DenormalizePcs(new double[] { 0.0, 0.0, double.NegativeInfinity }, IccPcsKind.Lab, IccPcsEncodingKind.Version4));
        });
    }
}
