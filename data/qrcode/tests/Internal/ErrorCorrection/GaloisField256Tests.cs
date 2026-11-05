using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class GaloisField256Tests
{
    [Test]
    public void ExponentAndLogarithmTablesCoverEveryNonzeroElement()
    {
        bool[] seen = new bool[256];
        int expected = 1;
        for (int exponent = 0; exponent < 255; exponent++)
        {
            int actual = GaloisField256.GetExponent(exponent);
            Assert.That(actual, Is.EqualTo(expected), "exponent " + exponent);
            Assert.That(seen[actual], Is.False, "duplicate element " + actual);
            Assert.That(GaloisField256.GetLogarithm(actual), Is.EqualTo(exponent));
            seen[actual] = true;
            expected = MultiplyReference(expected, 2);
        }

        Assert.Multiple(() =>
        {
            Assert.That(GaloisField256.PrimitivePolynomial, Is.EqualTo(0x11D));
            Assert.That(GaloisField256.GetExponent(0), Is.EqualTo(1));
            Assert.That(GaloisField256.GetExponent(7), Is.EqualTo(128));
            Assert.That(GaloisField256.GetExponent(8), Is.EqualTo(29));
            Assert.That(GaloisField256.GetExponent(255), Is.EqualTo(1));
            Assert.That(GaloisField256.GetExponent(256), Is.EqualTo(2));
            Assert.That(GaloisField256.GetExponent(-1), Is.EqualTo(GaloisField256.GetExponent(254)));
        });
    }

    [Test]
    public void AdditionAndMultiplicationMatchIndependentBitwiseArithmetic()
    {
        for (int left = 0; left < 256; left++)
        {
            for (int right = 0; right < 256; right++)
            {
                Assert.That(GaloisField256.Add(left, right), Is.EqualTo(left ^ right));
                Assert.That(
                    GaloisField256.Multiply(left, right),
                    Is.EqualTo(MultiplyReference(left, right)),
                    left + " * " + right);
            }
        }
    }

    [Test]
    public void DivisionAndInverseRestoreEveryFieldElement()
    {
        for (int value = 1; value < 256; value++)
        {
            int inverse = GaloisField256.Inverse(value);
            Assert.That(GaloisField256.Multiply(value, inverse), Is.EqualTo(1));
            Assert.That(GaloisField256.Divide(1, value), Is.EqualTo(inverse));

            for (int factor = 0; factor < 256; factor++)
            {
                int product = MultiplyReference(value, factor);
                Assert.That(
                    GaloisField256.Divide(product, value),
                    Is.EqualTo(factor),
                    product + " / " + value);
            }
        }

        Assert.That(GaloisField256.Divide(0, 173), Is.Zero);
    }

    [Test]
    public void PolynomialEvaluationMatchesIndependentHornerEvaluation()
    {
        int[] coefficients = new int[] { 32, 91, 11, 120, 209 };
        for (int value = 0; value < 256; value++)
        {
            int expected = 0;
            for (int index = 0; index < coefficients.Length; index++)
            {
                expected = MultiplyReference(expected, value) ^ coefficients[index];
            }

            Assert.That(
                GaloisPolynomial.Evaluate(coefficients, value),
                Is.EqualTo(expected),
                "x = " + value);
        }

        Assert.Multiple(() =>
        {
            Assert.That(GaloisPolynomial.Evaluate(new int[0], 37), Is.Zero);
            Assert.That(GaloisPolynomial.Evaluate(new int[] { 173 }, 99), Is.EqualTo(173));
            Assert.That(GaloisPolynomial.Evaluate(new int[] { 1, 0 }, 29), Is.EqualTo(29));
        });
    }

    [Test]
    public void RejectsValuesOutsideTheFieldAndUndefinedOperations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => GaloisField256.Add(-1, 0), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.Add(0, 256), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.Multiply(256, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.Divide(1, 0), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.Divide(-1, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.Inverse(0), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.GetLogarithm(0), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisField256.GetLogarithm(256), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisPolynomial.Evaluate(null!, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisPolynomial.Evaluate(new int[] { 1 }, -1),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => GaloisPolynomial.Evaluate(new int[] { 256 }, 1),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static int MultiplyReference(int left, int right)
    {
        int product = 0;
        int multiplicand = left;
        int multiplier = right;
        while (multiplier != 0)
        {
            if ((multiplier & 1) != 0)
            {
                product ^= multiplicand;
            }

            multiplier >>= 1;
            multiplicand <<= 1;
            if ((multiplicand & 0x100) != 0)
            {
                multiplicand ^= 0x11D;
            }
        }

        return product;
    }
}
