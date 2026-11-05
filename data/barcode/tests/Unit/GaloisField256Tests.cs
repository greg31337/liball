namespace barcode.Tests;

public class GaloisField256Tests
{
    [Test]
    public void DataMatrixFieldUsesRequiredConstantsAndInitialExponentSequence()
    {
        GaloisField256 field = GaloisField256.DataMatrix;

        Assert.Multiple(() =>
        {
            Assert.That(GaloisField256.FieldSize, Is.EqualTo(256));
            Assert.That(GaloisField256.MultiplicativeOrder, Is.EqualTo(255));
            Assert.That(GaloisField256.PrimitivePolynomial, Is.EqualTo(0x12D));
            Assert.That(GaloisField256.GeneratorBase, Is.EqualTo(1));
            Assert.That(field.Exponent(0), Is.EqualTo(1));
            Assert.That(field.Exponent(1), Is.EqualTo(2));
            Assert.That(field.Exponent(2), Is.EqualTo(4));
            Assert.That(field.Exponent(7), Is.EqualTo(128));
            Assert.That(field.Exponent(8), Is.EqualTo(45));
            Assert.That(field.Exponent(9), Is.EqualTo(90));
            Assert.That(field.Exponent(10), Is.EqualTo(180));
            Assert.That(field.Exponent(11), Is.EqualTo(69));
            Assert.That(field.Exponent(255), Is.EqualTo(1));
        });
    }

    [Test]
    public void LogAndExponentTablesContainEveryNonzeroElementExactlyOnce()
    {
        GaloisField256 field = GaloisField256.DataMatrix;
        bool[] found = new bool[256];
        int exponent = 0;
        while (exponent < 255)
        {
            int element = field.Exponent(exponent);
            Assert.That(element, Is.InRange(1, 255));
            Assert.That(found[element], Is.False, $"Duplicate field element {element} at exponent {exponent}.");
            found[element] = true;
            Assert.That(field.Logarithm(element), Is.EqualTo(exponent));
            exponent = exponent + 1;
        }

        int elementIndex = 1;
        while (elementIndex < found.Length)
        {
            Assert.That(found[elementIndex], Is.True, $"Field element {elementIndex} was absent.");
            elementIndex = elementIndex + 1;
        }
    }

    [Test]
    public void TableCopiesDoNotExposeFieldState()
    {
        GaloisField256 field = GaloisField256.DataMatrix;
        int[] exponents = field.CopyExponentTable();
        int[] logarithms = field.CopyLogarithmTable();
        exponents[0] = 99;
        logarithms[1] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(exponents.Length, Is.EqualTo(256));
            Assert.That(logarithms.Length, Is.EqualTo(256));
            Assert.That(logarithms[0], Is.EqualTo(-1));
            Assert.That(field.Exponent(0), Is.EqualTo(1));
            Assert.That(field.Logarithm(1), Is.EqualTo(0));
        });
    }

    [Test]
    public void MultiplicationMatchesIndependentPolynomialReductionForEveryPair()
    {
        GaloisField256 field = GaloisField256.DataMatrix;
        int left = 0;
        while (left < 256)
        {
            int right = 0;
            while (right < 256)
            {
                int expected = MultiplyReference(left, right);
                int actual = field.Multiply(left, right);
                if (actual != expected)
                {
                    Assert.Fail($"GF(256) product mismatch for {left} and {right}: expected {expected}, got {actual}.");
                }

                right = right + 1;
            }

            left = left + 1;
        }

        Assert.Pass();
    }

    [Test]
    public void InverseDivisionPowerAndAdditionSatisfyFieldIdentities()
    {
        GaloisField256 field = GaloisField256.DataMatrix;
        int element = 1;
        while (element < 256)
        {
            int inverse = field.Inverse(element);
            Assert.That(field.Multiply(element, inverse), Is.EqualTo(1));
            Assert.That(field.DivideElements(element, element), Is.EqualTo(1));
            element = element + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(GaloisField256.AddOrSubtract(0xA5, 0x3C), Is.EqualTo(0x99));
            Assert.That(GaloisField256.AddOrSubtract(0xA5, 0xA5), Is.Zero);
            Assert.That(field.DivideElements(0, 17), Is.Zero);
            Assert.That(field.DivideElements(1, 2), Is.EqualTo(field.Inverse(2)));
            Assert.That(field.Power(2, 0), Is.EqualTo(1));
            Assert.That(field.Power(2, 8), Is.EqualTo(45));
            Assert.That(field.Power(2, 255), Is.EqualTo(1));
            Assert.That(field.Power(0, 0), Is.EqualTo(1));
            Assert.That(field.Power(0, 5), Is.Zero);
        });
    }

    [Test]
    public void MonomialAndIdentityFactoriesReturnCanonicalPolynomials()
    {
        GaloisField256 field = GaloisField256.DataMatrix;

        Assert.Multiple(() =>
        {
            Assert.That(field.CreateZeroPolynomial().CopyCoefficients(), Is.EqualTo(new int[] { 0 }));
            Assert.That(field.CreateOnePolynomial().CopyCoefficients(), Is.EqualTo(new int[] { 1 }));
            Assert.That(field.BuildMonomial(3, 7).CopyCoefficients(), Is.EqualTo(new int[] { 7, 0, 0, 0 }));
            Assert.That(field.BuildMonomial(20, 0), Is.SameAs(field.CreateZeroPolynomial()));
        });
    }

    [Test]
    public void FieldOperationsRejectValuesOutsideTheFieldAndUndefinedOperations()
    {
        GaloisField256 field = GaloisField256.DataMatrix;

        Assert.Multiple(() =>
        {
            Assert.That(() => GaloisField256.AddOrSubtract(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => GaloisField256.AddOrSubtract(0, 256), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Exponent(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Exponent(256), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Logarithm(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Inverse(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Multiply(1, 256), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.DivideElements(1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Power(2, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.BuildMonomial(-1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.BuildMonomial(1, 256), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static int MultiplyReference(int left, int right)
    {
        int product = 0;
        int multiplicand = left;
        int multiplier = right;
        int bit = 0;
        while (bit < 8)
        {
            if ((multiplier & 1) != 0)
            {
                product = product ^ multiplicand;
            }

            multiplier = multiplier >> 1;
            multiplicand = multiplicand << 1;
            if ((multiplicand & 0x100) != 0)
            {
                multiplicand = multiplicand ^ GaloisField256.PrimitivePolynomial;
            }

            bit = bit + 1;
        }

        return product & 0xFF;
    }
}
