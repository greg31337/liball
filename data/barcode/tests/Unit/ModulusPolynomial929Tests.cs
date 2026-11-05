namespace barcode.Tests;

public class ModulusPolynomial929Tests
{
    [Test]
    public void ConstructionNormalizesLeadingZerosAndCopiesCallerData()
    {
        int[] source = new int[] { 0, 0, 5, 0, 7 };

        ModulusPolynomial929 polynomial = new ModulusPolynomial929(source);
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
            Assert.That(new ModulusPolynomial929(new int[] { 0, 0, 0 }).CopyCoefficients(), Is.EqualTo(new int[] { 0 }));
        });
    }

    [Test]
    public void EvaluationUsesHornerArithmeticAndFastSpecialPoints()
    {
        ModulusPolynomial929 polynomial = new ModulusPolynomial929(new int[] { 5, 0, 7 });

        Assert.Multiple(() =>
        {
            Assert.That(polynomial.EvaluateAt(0), Is.EqualTo(7));
            Assert.That(polynomial.EvaluateAt(1), Is.EqualTo(12));
            Assert.That(polynomial.EvaluateAt(2), Is.EqualTo(27));
            Assert.That(polynomial.EvaluateAt(928), Is.EqualTo(12));
            Assert.That(polynomial.EvaluateAt(417), Is.EqualTo(EvaluateReference(new int[] { 5, 0, 7 }, 417)));
        });
    }

    [Test]
    public void AdditionSubtractionAndNegationUsePrimeFieldSigns()
    {
        ModulusPolynomial929 first = new ModulusPolynomial929(new int[] { 900, 928, 3 });
        ModulusPolynomial929 second = new ModulusPolynomial929(new int[] { 40, 5 });

        Assert.Multiple(() =>
        {
            Assert.That(first.AddPolynomial(second).CopyCoefficients(), Is.EqualTo(new int[] { 900, 39, 8 }));
            Assert.That(first.SubtractPolynomial(second).CopyCoefficients(), Is.EqualTo(new int[] { 900, 888, 927 }));
            Assert.That(first.NegatePolynomial().CopyCoefficients(), Is.EqualTo(new int[] { 29, 1, 926 }));
            Assert.That(first.SubtractPolynomial(first).IsZero(), Is.True);
            Assert.That(first.AddPolynomial(ModulusField929.Pdf417.CreateZeroPolynomial()), Is.SameAs(first));
            Assert.That(first.SubtractPolynomial(ModulusField929.Pdf417.CreateZeroPolynomial()), Is.SameAs(first));
            Assert.That(ModulusField929.Pdf417.CreateZeroPolynomial().NegatePolynomial().IsZero(), Is.True);
        });
    }

    [Test]
    public void MultiplicationSupportsPolynomialScalarAndMonomialFactors()
    {
        ModulusPolynomial929 first = new ModulusPolynomial929(new int[] { 2, 3 });
        ModulusPolynomial929 second = new ModulusPolynomial929(new int[] { 4, 5 });
        ModulusPolynomial929 basePolynomial = new ModulusPolynomial929(new int[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(first.MultiplyPolynomial(second).CopyCoefficients(), Is.EqualTo(new int[] { 8, 22, 15 }));
            Assert.That(new ModulusPolynomial929(new int[] { 900 }).MultiplyByScalar(2).CopyCoefficients(), Is.EqualTo(new int[] { 871 }));
            Assert.That(basePolynomial.MultiplyByScalar(2).CopyCoefficients(), Is.EqualTo(new int[] { 2, 4, 6 }));
            Assert.That(basePolynomial.MultiplyByMonomial(2, 2).CopyCoefficients(), Is.EqualTo(new int[] { 2, 4, 6, 0, 0 }));
            Assert.That(basePolynomial.MultiplyByScalar(1), Is.SameAs(basePolynomial));
            Assert.That(basePolynomial.MultiplyByScalar(0).IsZero(), Is.True);
            Assert.That(basePolynomial.MultiplyByMonomial(5, 0).IsZero(), Is.True);
            Assert.That(basePolynomial.MultiplyPolynomial(ModulusField929.Pdf417.CreateZeroPolynomial()).IsZero(), Is.True);
            Assert.That(ModulusField929.Pdf417.CreateZeroPolynomial().MultiplyPolynomial(basePolynomial).IsZero(), Is.True);
        });
    }

    [Test]
    public void DivisionReconstructsDividendForMonicAndNonMonicDivisors()
    {
        ModulusPolynomial929 first = new ModulusPolynomial929(new int[] { 5, 7, 9 });
        ModulusPolynomial929 second = new ModulusPolynomial929(new int[] { 2, 3 });
        ModulusPolynomial929 product = first.MultiplyPolynomial(second);
        ModulusPolynomial929[] result = product.DivideByPolynomial(first);
        ModulusPolynomial929 dividend = new ModulusPolynomial929(new int[] { 1, 0, 928 });
        ModulusPolynomial929 divisor = new ModulusPolynomial929(new int[] { 1, 1 });
        ModulusPolynomial929[] exact = dividend.DivideByPolynomial(divisor);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].CopyCoefficients(), Is.EqualTo(second.CopyCoefficients()));
            Assert.That(result[1].IsZero(), Is.True);
            Assert.That(
                result[0].MultiplyPolynomial(first).AddPolynomial(result[1]).CopyCoefficients(),
                Is.EqualTo(product.CopyCoefficients()));
            Assert.That(exact[0].CopyCoefficients(), Is.EqualTo(new int[] { 1, 928 }));
            Assert.That(exact[1].IsZero(), Is.True);
        });
    }

    [Test]
    public void DivisionByHigherDegreeReturnsZeroAndOriginalRemainder()
    {
        ModulusPolynomial929 dividend = new ModulusPolynomial929(new int[] { 3, 4 });
        ModulusPolynomial929 divisor = new ModulusPolynomial929(new int[] { 1, 2, 3 });

        ModulusPolynomial929[] result = dividend.DivideByPolynomial(divisor);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].IsZero(), Is.True);
            Assert.That(result[1], Is.SameAs(dividend));
        });
    }

    [Test]
    public void FormalDerivativeUsesPrimeFieldDegreeFactors()
    {
        ModulusPolynomial929 cubic = new ModulusPolynomial929(new int[] { 5, 4, 3, 2 });
        ModulusPolynomial929 constant = new ModulusPolynomial929(new int[] { 7 });
        int[] degree929Coefficients = new int[930];
        degree929Coefficients[0] = 1;
        ModulusPolynomial929 degree929 = new ModulusPolynomial929(degree929Coefficients);

        Assert.Multiple(() =>
        {
            Assert.That(cubic.CalculateFormalDerivative().CopyCoefficients(), Is.EqualTo(new int[] { 15, 8, 3 }));
            Assert.That(constant.CalculateFormalDerivative().IsZero(), Is.True);
            Assert.That(degree929.CalculateFormalDerivative().IsZero(), Is.True);
        });
    }

    [Test]
    public void PolynomialOperationsRejectInvalidCoefficientsAndUndefinedOperations()
    {
        ModulusPolynomial929 polynomial = new ModulusPolynomial929(new int[] { 1, 2 });

        Assert.Multiple(() =>
        {
            Assert.That(() => new ModulusPolynomial929(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ModulusPolynomial929(new int[0]), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ModulusPolynomial929(new int[] { -1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ModulusPolynomial929(new int[] { 929 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.GetCoefficient(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.GetCoefficient(2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.EvaluateAt(929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.AddPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.SubtractPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyByScalar(929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyByMonomial(-1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.MultiplyByMonomial(1, 929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => polynomial.DivideByPolynomial(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => polynomial.DivideByPolynomial(ModulusField929.Pdf417.CreateZeroPolynomial()),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static int EvaluateReference(int[] coefficients, int element)
    {
        int result = 0;
        int index = 0;
        while (index < coefficients.Length)
        {
            result = ((result * element) + coefficients[index]) % 929;
            index = index + 1;
        }

        return result;
    }
}
