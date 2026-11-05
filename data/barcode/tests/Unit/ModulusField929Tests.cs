namespace barcode.Tests;

public class ModulusField929Tests
{
    [Test]
    public void Pdf417FieldUsesRequiredConstantsAndInitialExponentSequence()
    {
        ModulusField929 field = ModulusField929.Pdf417;

        Assert.Multiple(() =>
        {
            Assert.That(ModulusField929.Modulus, Is.EqualTo(929));
            Assert.That(ModulusField929.Generator, Is.EqualTo(3));
            Assert.That(ModulusField929.MultiplicativeOrder, Is.EqualTo(928));
            Assert.That(field.Exponent(0), Is.EqualTo(1));
            Assert.That(field.Exponent(1), Is.EqualTo(3));
            Assert.That(field.Exponent(2), Is.EqualTo(9));
            Assert.That(field.Exponent(3), Is.EqualTo(27));
            Assert.That(field.Exponent(4), Is.EqualTo(81));
            Assert.That(field.Exponent(5), Is.EqualTo(243));
            Assert.That(field.Exponent(6), Is.EqualTo(729));
            Assert.That(field.Exponent(7), Is.EqualTo(329));
            Assert.That(field.Exponent(8), Is.EqualTo(58));
            Assert.That(field.Exponent(928), Is.EqualTo(1));
        });
    }

    [Test]
    public void LogAndExponentTablesContainEveryNonzeroElementExactlyOnce()
    {
        ModulusField929 field = ModulusField929.Pdf417;
        bool[] found = new bool[929];
        int exponent = 0;
        while (exponent < 928)
        {
            int element = field.Exponent(exponent);
            Assert.That(element, Is.InRange(1, 928));
            Assert.That(found[element], Is.False, $"Duplicate element {element} at exponent {exponent}.");
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
        ModulusField929 field = ModulusField929.Pdf417;
        int[] exponents = field.CopyExponentTable();
        int[] logarithms = field.CopyLogarithmTable();
        exponents[0] = 99;
        logarithms[1] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(exponents.Length, Is.EqualTo(929));
            Assert.That(logarithms.Length, Is.EqualTo(929));
            Assert.That(logarithms[0], Is.EqualTo(-1));
            Assert.That(field.Exponent(0), Is.EqualTo(1));
            Assert.That(field.Logarithm(1), Is.EqualTo(0));
        });
    }

    [Test]
    public void ArithmeticMatchesOrdinaryModulo929ForEveryPair()
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int left = 0;
        while (left < 929)
        {
            int right = 0;
            while (right < 929)
            {
                int expectedProduct = (left * right) % 929;
                int expectedSum = (left + right) % 929;
                int expectedDifference = (929 + left - right) % 929;
                if (field.Multiply(left, right) != expectedProduct)
                {
                    Assert.Fail($"Product mismatch for {left} and {right}.");
                }

                if (ModulusField929.Add(left, right) != expectedSum)
                {
                    Assert.Fail($"Sum mismatch for {left} and {right}.");
                }

                if (ModulusField929.Subtract(left, right) != expectedDifference)
                {
                    Assert.Fail($"Difference mismatch for {left} and {right}.");
                }

                right = right + 1;
            }

            left = left + 1;
        }

        Assert.Pass();
    }

    [Test]
    public void InverseDivisionPowerAndNegationSatisfyFieldIdentities()
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int element = 1;
        while (element < 929)
        {
            int inverse = field.Inverse(element);
            Assert.That(field.Multiply(element, inverse), Is.EqualTo(1));
            Assert.That(field.DivideElements(element, element), Is.EqualTo(1));
            Assert.That(ModulusField929.Add(element, ModulusField929.Negate(element)), Is.Zero);
            element = element + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(ModulusField929.Negate(0), Is.Zero);
            Assert.That(ModulusField929.Negate(1), Is.EqualTo(928));
            Assert.That(field.DivideElements(0, 17), Is.Zero);
            Assert.That(field.DivideElements(1, 3), Is.EqualTo(field.Inverse(3)));
            Assert.That(field.Power(3, 0), Is.EqualTo(1));
            Assert.That(field.Power(3, 7), Is.EqualTo(329));
            Assert.That(field.Power(3, 928), Is.EqualTo(1));
            Assert.That(field.Power(0, 0), Is.EqualTo(1));
            Assert.That(field.Power(0, 9), Is.Zero);
        });
    }

    [Test]
    public void MonomialAndIdentityFactoriesReturnCanonicalPolynomials()
    {
        ModulusField929 field = ModulusField929.Pdf417;

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
        ModulusField929 field = ModulusField929.Pdf417;

        Assert.Multiple(() =>
        {
            Assert.That(() => ModulusField929.Add(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ModulusField929.Subtract(0, 929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => ModulusField929.Negate(929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Exponent(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Exponent(929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Logarithm(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Inverse(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Multiply(1, 929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.DivideElements(1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.Power(3, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.BuildMonomial(-1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => field.BuildMonomial(1, 929), Throws.TypeOf<BarcodeValidationException>());
        });
    }
}
