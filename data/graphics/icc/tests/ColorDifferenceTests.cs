using icc;

namespace icc.Tests;

public class ColorDifferenceTests
{
    [Test]
    public void Cie76UsesEuclideanLabDistance()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] first = new double[] { 50.0, 20.0, 30.0 };
        double[] second = new double[] { 60.0, 25.0, 20.0 };

        double result = colorMath.CalculateColorDifference(first, second, IccColorDifferenceFormula.Cie76);

        Assert.That(result, Is.EqualTo(15.0).Within(0.000000000001));
    }

    [TestCase(IccColorDifferenceFormula.Cie94GraphicArts, 12.171640953745172)]
    [TestCase(IccColorDifferenceFormula.Cie94Textiles, 8.670898717269491)]
    public void Cie94UsesTheSelectedApplicationParameterSet(IccColorDifferenceFormula formula, double expected)
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] reference = new double[] { 50.0, 20.0, 30.0 };
        double[] sample = new double[] { 60.0, 25.0, 20.0 };

        double result = colorMath.CalculateColorDifference(reference, sample, formula);

        Assert.That(result, Is.EqualTo(expected).Within(0.000000000001));
    }

    [TestCase(IccColorDifferenceFormula.Cie94GraphicArts, 94.28090415820634)]
    [TestCase(IccColorDifferenceFormula.Cie94Textiles, 101.01525445522108)]
    public void Cie94KeepsARepresentableResultForLargeOrthogonalChromas(IccColorDifferenceFormula formula, double expected)
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] first = new double[] { 50.0, 1e200, 0.0 };
        double[] second = new double[] { 50.0, 0.0, 1e200 };

        double result = colorMath.CalculateColorDifference(first, second, formula);

        Assert.That(result, Is.EqualTo(expected).Within(0.000000000001));
    }

    // Sharma, Wu, and Dalal supplementary test data for the CIEDE2000 implementation notes:
    // https://hajim.rochester.edu/ece/sites/gsharma/ciede2000/dataNprograms/ciede2000testdata.txt
    [TestCase(50.0000, 2.6772, -79.7751, 50.0000, 0.0000, -82.7485, 2.0425)]
    [TestCase(50.0000, 3.1571, -77.2803, 50.0000, 0.0000, -82.7485, 2.8615)]
    [TestCase(50.0000, 2.8361, -74.0200, 50.0000, 0.0000, -82.7485, 3.4412)]
    [TestCase(50.0000, -1.3802, -84.2814, 50.0000, 0.0000, -82.7485, 1.0000)]
    [TestCase(50.0000, -1.1848, -84.8006, 50.0000, 0.0000, -82.7485, 1.0000)]
    [TestCase(50.0000, -0.9009, -85.5211, 50.0000, 0.0000, -82.7485, 1.0000)]
    [TestCase(50.0000, 0.0000, 0.0000, 50.0000, -1.0000, 2.0000, 2.3669)]
    [TestCase(50.0000, -1.0000, 2.0000, 50.0000, 0.0000, 0.0000, 2.3669)]
    [TestCase(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0009, 7.1792)]
    [TestCase(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0010, 7.1792)]
    [TestCase(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0011, 7.2195)]
    [TestCase(50.0000, 2.4900, -0.0010, 50.0000, -2.4900, 0.0012, 7.2195)]
    [TestCase(50.0000, -0.0010, 2.4900, 50.0000, 0.0009, -2.4900, 4.8045)]
    [TestCase(50.0000, -0.0010, 2.4900, 50.0000, 0.0010, -2.4900, 4.8045)]
    [TestCase(50.0000, -0.0010, 2.4900, 50.0000, 0.0011, -2.4900, 4.7461)]
    [TestCase(50.0000, 2.5000, 0.0000, 50.0000, 0.0000, -2.5000, 4.3065)]
    [TestCase(50.0000, 2.5000, 0.0000, 73.0000, 25.0000, -18.0000, 27.1492)]
    [TestCase(50.0000, 2.5000, 0.0000, 61.0000, -5.0000, 29.0000, 22.8977)]
    [TestCase(50.0000, 2.5000, 0.0000, 56.0000, -27.0000, -3.0000, 31.9030)]
    [TestCase(50.0000, 2.5000, 0.0000, 58.0000, 24.0000, 15.0000, 19.4535)]
    [TestCase(50.0000, 2.5000, 0.0000, 50.0000, 3.1736, 0.5854, 1.0000)]
    [TestCase(50.0000, 2.5000, 0.0000, 50.0000, 3.2972, 0.0000, 1.0000)]
    [TestCase(50.0000, 2.5000, 0.0000, 50.0000, 1.8634, 0.5757, 1.0000)]
    [TestCase(50.0000, 2.5000, 0.0000, 50.0000, 3.2592, 0.3350, 1.0000)]
    [TestCase(60.2574, -34.0099, 36.2677, 60.4626, -34.1751, 39.4387, 1.2644)]
    [TestCase(63.0109, -31.0961, -5.8663, 62.8187, -29.7946, -4.0864, 1.2630)]
    [TestCase(61.2901, 3.7196, -5.3901, 61.4292, 2.2480, -4.9620, 1.8731)]
    [TestCase(35.0831, -44.1164, 3.7933, 35.0232, -40.0716, 1.5901, 1.8645)]
    [TestCase(22.7233, 20.0904, -46.6940, 23.0331, 14.9730, -42.5619, 2.0373)]
    [TestCase(36.4612, 47.8580, 18.3852, 36.2715, 50.5065, 21.2231, 1.4146)]
    [TestCase(90.8027, -2.0831, 1.4410, 91.1528, -1.6435, 0.0447, 1.4441)]
    [TestCase(90.9257, -0.5406, -0.9208, 88.6381, -0.8985, -0.7239, 1.5381)]
    [TestCase(6.7747, -0.2908, -2.4247, 5.8714, -0.0985, -2.2286, 0.6377)]
    [TestCase(2.0776, 0.0795, -1.1350, 0.9033, -0.0636, -0.5514, 0.9082)]
    public void Ciede2000MatchesPublishedReferenceVectors(
        double firstL,
        double firstA,
        double firstB,
        double secondL,
        double secondA,
        double secondB,
        double expected)
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] first = new double[] { firstL, firstA, firstB };
        double[] second = new double[] { secondL, secondA, secondB };

        double forward = colorMath.CalculateColorDifference(first, second, IccColorDifferenceFormula.Ciede2000);
        double reverse = colorMath.CalculateColorDifference(second, first, IccColorDifferenceFormula.Ciede2000);

        Assert.Multiple(() =>
        {
            Assert.That(forward, Is.EqualTo(expected).Within(0.00005));
            Assert.That(reverse, Is.EqualTo(forward).Within(0.000000000001));
        });
    }

    [TestCase(IccColorDifferenceFormula.Cie76)]
    [TestCase(IccColorDifferenceFormula.Cie94GraphicArts)]
    [TestCase(IccColorDifferenceFormula.Cie94Textiles)]
    [TestCase(IccColorDifferenceFormula.Ciede2000)]
    public void EveryFormulaReturnsZeroForIdenticalColours(IccColorDifferenceFormula formula)
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] lab = new double[] { 42.0, -12.0, 7.0 };

        double result = colorMath.CalculateColorDifference(lab, lab, formula);

        Assert.That(result, Is.Zero);
    }

    [Test]
    public void ColorDifferenceRejectsInvalidInputsAndFormula()
    {
        IIccColorMath colorMath = new IccColorMath();
        double[] valid = new double[] { 50.0, 0.0, 0.0 };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(null!, valid, IccColorDifferenceFormula.Cie76));
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(valid, null!, IccColorDifferenceFormula.Cie76));
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(new double[2], valid, IccColorDifferenceFormula.Cie76));
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(valid, new double[4], IccColorDifferenceFormula.Cie76));
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(new double[] { 50.0, double.NaN, 0.0 }, valid, IccColorDifferenceFormula.Cie76));
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(valid, new double[] { 50.0, 0.0, double.PositiveInfinity }, IccColorDifferenceFormula.Ciede2000));
            Assert.Throws<IccTransformException>(() => colorMath.CalculateColorDifference(valid, valid, (IccColorDifferenceFormula)99));
        });
    }
}
