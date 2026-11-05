namespace barcode.Tests;

public class Gs1Modulo10Tests
{
    [TestCase("400638133393", 1)]
    [TestCase("5512345", 7)]
    [TestCase("03600029145", 2)]
    [TestCase("950110153000", 3)]
    [TestCase("10614141123456789", 7)]
    public void CalculateCheckDigitMatchesPublishedEanUpcAndGs1Examples(string digits, int expected)
    {
        Assert.That(Gs1Modulo10.CalculateCheckDigit(digits), Is.EqualTo(expected));
    }

    [TestCase("1", 7)]
    [TestCase("12", 3)]
    [TestCase("123", 6)]
    [TestCase("1234", 8)]
    [TestCase("12345", 7)]
    [TestCase("123456", 5)]
    public void CalculateCheckDigitAlternatesWeightsForArbitraryDataLengths(string digits, int expected)
    {
        Assert.That(Gs1Modulo10.CalculateCheckDigit(digits), Is.EqualTo(expected));
    }

    [Test]
    public void CalculateCheckDigitHandlesLongInputWithoutAccumulationOverflow()
    {
        string digits = new string('9', 100000);

        Assert.That(Gs1Modulo10.CalculateCheckDigit(digits), Is.EqualTo(0));
    }

    [TestCase("4006381333931", true)]
    [TestCase("4006381333932", false)]
    [TestCase("55123457", true)]
    [TestCase("55123450", false)]
    [TestCase("106141411234567897", true)]
    [TestCase("106141411234567890", false)]
    public void VerifyCompleteDigitsDistinguishesMatchingAndMismatchingChecksums(string digits, bool expected)
    {
        Assert.That(Gs1Modulo10.VerifyCompleteDigits(digits), Is.EqualTo(expected));
    }

    [Test]
    public void VerifyCheckDigitAcceptsSeparateObservedValue()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Gs1Modulo10.VerifyCheckDigit("03600029145", 2), Is.True);
            Assert.That(Gs1Modulo10.VerifyCheckDigit("03600029145", 3), Is.False);
        });
    }

    [TestCase("400638133393", "4006381333931")]
    [TestCase("1234567", "12345670")]
    [TestCase("1", "17")]
    public void AppendCheckDigitReturnsCompleteDigits(string digits, string expected)
    {
        string complete = Gs1Modulo10.AppendCheckDigit(digits);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.EqualTo(expected));
            Assert.That(Gs1Modulo10.VerifyCompleteDigits(complete), Is.True);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("12A3")]
    [TestCase("12 3")]
    [TestCase("１２３")]
    public void CalculateCheckDigitRejectsMissingEmptyAndNonAsciiData(string? digits)
    {
        Assert.That(() => Gs1Modulo10.CalculateCheckDigit(digits!), Throws.TypeOf<BarcodeValidationException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("7")]
    [TestCase("123A")]
    [TestCase("１２３４")]
    public void VerifyCompleteDigitsRejectsMalformedInput(string? digits)
    {
        Assert.That(() => Gs1Modulo10.VerifyCompleteDigits(digits!), Throws.TypeOf<BarcodeValidationException>());
    }

    [TestCase(-1)]
    [TestCase(10)]
    [TestCase(100)]
    public void VerifyCheckDigitRejectsObservedValueOutsideDecimalRange(int observed)
    {
        Assert.That(() => Gs1Modulo10.VerifyCheckDigit("123", observed), Throws.TypeOf<BarcodeValidationException>());
    }

    [Test]
    public void PrefixCalculationSupportsCompleteValuesWithoutAllocatingSubstring()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Gs1Modulo10.CalculateCheckDigitForPrefix("4006381333931", 12), Is.EqualTo(1));
            Assert.That(() => Gs1Modulo10.CalculateCheckDigitForPrefix(null!, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Gs1Modulo10.CalculateCheckDigitForPrefix("123", 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Gs1Modulo10.CalculateCheckDigitForPrefix("123", 4), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Gs1Modulo10.CalculateCheckDigitForPrefix("12A3", 3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Gs1Modulo10.CalculateCheckDigitForPrefix("/123", 1), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void AlgorithmNameIsStableForFutureDispatcherResults()
    {
        Assert.That(Gs1Modulo10.AlgorithmName, Is.EqualTo("GS1 modulo-10"));
    }
}
