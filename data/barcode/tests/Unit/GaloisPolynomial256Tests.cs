namespace barcode.Tests;

public class GaloisPolynomial256Tests
{
    [Test]
    public void ConstructionNormalizesLeadingZerosAndCopiesCallerData()
    {
        int[] source = new int[] { 0, 0, 5, 0, 7 };

        GaloisPolynomial256 polynomial = new GaloisPolynomial256(source);
        source[2] = 99;
        int[] coefficients = polynomial.CopyCoefficients();
        coefficients[0] = 88;

        Assert.Multiple(() =>
        {
            Assert.That(polynomial.CopyCoefficients(), Is.EqualTo(new int[] { 5, 0, 7 }));
            Assert.That(polynomial.GetDegree(), Is.EqualTo(2));
            Assert.That(polynomial.GetCoefficient(2), Is.EqualTo(5));
            Assert.That(polynomial.GetCoefficient(1), Is.EqualTo(0));
            Assert.That(polynomial.GetCoefficient(0), Is.EqualTo(7));
            Assert.That(polynomial.IsZero(), Is.False);
            Assert.That(new GaloisPolynomial256(new int[] { 0, 0, 0 }).CopyCoefficients(), Is.EqualTo(new int[] { 0 }));
        });
    }

    [Test]
    public void EvaluationUsesHornerArithmeticAndFastSpecialPoints()
    {
        GaloisPolynomial256 polynomial = new GaloisPolynomial256(new int[] { 5, 0, 7 });

        Assert.Multiple(() =>
        {
            Assert.That(polynomial.EvaluateAt(0), Is.EqualTo(7));
            Assert.That(polynomial.EvaluateAt(1), Is.EqualTo(2));
            Assert.That(polynomial.EvaluateAt(2), Is.EqualTo(19));
            Assert.That(polynomial.EvaluateAt(255), Is.EqualTo(EvaluateReference(new int[] { 5, 0, 7 }, 255)));
        });
    }

    [Test]
    public void AdditionAlignsTermsAndCancelsEqualPolynomials()
    {
        GaloisPolynomial256 first = new GaloisPolynomial256(new int[] { 1, 2, 3 });
        GaloisPolynomial256 second = new GaloisPolynomial256(new int[] { 4, 5 });

        Assert.Multiple(() =>
        {
            Assert.That(first.AddOrSubtractPolynomial(second).CopyCoefficients(), Is.EqualTo(new int[] { 1, 6, 6 }));
            Assert.That(first.AddOrSubtractPolynomial(first).CopyCoefficients(), Is.EqualTo(new int[] { 0 }));
            Assert.That(first.AddOrSubtractPolynomial(GaloisField256.DataMatrix.CreateZeroPolynomial()), Is.SameAs(first));
            Assert.That(GaloisField256.DataMatrix.CreateZeroPolynomial().AddOrSubtractPolynomial(first), Is.SameAs(first));
        });
    }

    [Test]
    public void MultiplicationSupportsPolynomialScalarAndMonomialFactors()
    {
        GaloisPolynomial256 first = new GaloisPolynomial256(new int[] { 2, 3 });
        GaloisPolynomial256 second = new GaloisPolynomial256(new int[] { 4, 5 });
        GaloisPolynomial256 basePolynomial = new GaloisPolynomial256(new int[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(first.MultiplyPolynomial(second).CopyCoefficients(), Is.EqualTo(new int[] { 8, 6, 15 }));
            Assert.That(basePolynomial.MultiplyByScalar(2).CopyCoefficients(), Is.EqualTo(new int[] { 2, 4, 6 }));
            Assert.That(basePolynomial.MultiplyByMonomial(2, 2).CopyCoefficients(), Is.EqualTo(new int[] { 2, 4, 6, 0, 0 }));
            Assert.That(basePolynomial.MultiplyByScalar(1), Is.SameAs(basePolynomial));
            Assert.That(basePolynomial.MultiplyByScalar(0).IsZero(), Is.True);
            Assert.That(basePolynomial.MultiplyByMonomial(5, 0).IsZero(), Is.True);
            Assert.That(basePolynomial.MultiplyPolynomial(GaloisField256.DataMatrix.CreateZeroPolynomial()).IsZero(), Is.True);
            Assert.That(GaloisField256.DataMatrix.CreateZeroPolynomial().MultiplyPolynomial(basePolynomial).IsZero(), Is.True);
        });
    }

    [Test]
    public void DivisionReconstructsDividendForMonicAndNonMonicDivisors()
    {
        GaloisPolynomial256 monicDivisor = new GaloisPolynomial256(new int[] { 1, 1 });
        GaloisPolynomial256 monicDividend = new GaloisPolynomial256(new int[] { 1, 0, 1 });
        GaloisPolynomial256[] monicResult = monicDividend.DivideByPolynomial(monicDivisor);
        GaloisPolynomial256 first = new GaloisPolynomial256(new int[] { 5, 7, 9 });
        GaloisPolynomial256 second = new GaloisPolynomial256(new int[] { 2, 3 });
        GaloisPolynomial256 product = first.MultiplyPolynomial(second);
        GaloisPolynomial256[] result = product.DivideByPolynomial(first);

        Assert.Multiple(() =>
        {
            Assert.That(monicResult[0].CopyCoefficients(), Is.EqualTo(new int[] { 1, 1 }));
            Assert.That(monicResult[1].IsZero(), Is.True);
            Assert.That(result[0].CopyCoefficients(), Is.EqualTo(second.CopyCoefficients()));
            Assert.That(result[1].IsZero(), Is.True);
            Assert.That(
                result[0].MultiplyPolynomial(first).AddOrSubtractPolynomial(result[1]).CopyCoefficients(),
                Is.EqualTo(product.CopyCoefficients()));
        });
    }

    [Test]
    public void DivisionByHigherDegreeReturnsZeroAndOriginalRemainder()
    {
        GaloisPolynomial256 dividend = new GaloisPolynomial256(new int[] { 3, 4 });
        GaloisPolynomial256 divisor = new GaloisPolynomial256(new int[] { 1, 2, 3 });

        GaloisPolynomial256[] result = dividend.DivideByPolynomial(divisor);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].IsZero(), Is.True);
            Assert.That(result[1], Is.SameAs(dividend));
        });
    }

    [Test]
    public void FormalDerivativeUsesCharacteristicTwoRules()
    {
        GaloisPolynomial256 cubic = new GaloisPolynomial256(new int[] { 5, 4, 3, 2 });
        GaloisPolynomial256 constant = new GaloisPolynomial256(new int[] { 7 });

        Assert.Multiple(() =>
        {
            Assert.That(cubic.CalculateFormalDerivative().CopyCoefficients(), Is.EqualTo(new int[] { 5, 0, 3 }));
            Assert.That(constant.CalculateFormalDerivative().IsZero(), Is.True);
        });
    }

    [Test]
    public void PolynomialOperationsRejectInvalidCoefficientsAndUndefinedOperations()
    {
        GaloisPolynomial256 polynomial = new GaloisPolynomial256(new int[] { 1, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(() => new GaloisPolynomial256(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new GaloisPolynomial256(new int[0]), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new GaloisPolynomial256(new int[] { -1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new GaloisPolynomial256(new int[] { 256 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.GetCoefficient(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.GetCoefficient(2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.EvaluateAt(256), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.AddOrSubtractPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyByScalar(256), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyByMonomial(-1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyByMonomial(1, 256), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.DivideByPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => polynomial.DivideByPolynomial(GaloisField256.DataMatrix.CreateZeroPolynomial()),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static int EvaluateReference(int[] coefficients, int element)
    {
        int result = 0;
        int index = 0;
        while (index < coefficients.Length)
        {
            result = MultiplyReference(result, element) ^ coefficients[index];
            index = index + 1;
        }

        return result;
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
                multiplicand = multiplicand ^ 0x12D;
            }

            bit = bit + 1;
        }

        return product & 0xFF;
    }
}
