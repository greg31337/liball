namespace barcode.Tests;

public class Code39Modulo43Tests
{
    [TestCase("CODE39", 32, 'W')]
    [TestCase("12345", 15, 'F')]
    [TestCase("ABC", 33, 'X')]
    [TestCase("0", 0, '0')]
    [TestCase("$%", 38, ' ')]
    public void CalculateCheckValueAndCharacterUseStandardAlphabet(string text, int expectedValue, char expectedCharacter)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code39Modulo43.CalculateCheckValue(text), Is.EqualTo(expectedValue));
            Assert.That(Code39Modulo43.CalculateCheckCharacter(text), Is.EqualTo(expectedCharacter));
        });
    }

    [Test]
    public void CompleteAlphabetWrapsToZeroWithoutAccumulationOverflow()
    {
        string repeated = new string('Z', 100000);

        Assert.Multiple(() =>
        {
            Assert.That(Code39Modulo43.CalculateCheckValue(Code39Modulo43.Alphabet), Is.EqualTo(0));
            Assert.That(Code39Modulo43.CalculateCheckValue(repeated), Is.EqualTo(15));
        });
    }

    [Test]
    public void AppendAndVerifyCode39CheckCharacter()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code39Modulo43.AppendCheckCharacter("CODE39"), Is.EqualTo("CODE39W"));
            Assert.That(Code39Modulo43.VerifyCompleteText("CODE39W"), Is.True);
            Assert.That(Code39Modulo43.VerifyCompleteText("CODE39X"), Is.False);
            Assert.That(Code39Modulo43.VerifyCheckCharacter("CODE39", 'W'), Is.True);
            Assert.That(Code39Modulo43.VerifyCheckCharacter("CODE39", '0'), Is.False);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("lower")]
    [TestCase("ABC*")]
    [TestCase("ABC_")]
    public void CalculationRejectsMissingEmptyAndNonAlphabetText(string? text)
    {
        Assert.That(() => Code39Modulo43.CalculateCheckValue(text!), Throws.TypeOf<BarcodeValidationException>());
    }

    [Test]
    public void VerificationRejectsMalformedCompleteTextAndCheckCharacter()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Code39Modulo43.VerifyCompleteText(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Modulo43.VerifyCompleteText("A"), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Modulo43.VerifyCompleteText("ABC*"), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Modulo43.VerifyCheckCharacter("ABC", '*'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Modulo43.CalculateCheckValueForPrefix(null!, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Modulo43.CalculateCheckValueForPrefix("ABC", 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39Modulo43.CalculateCheckValueForPrefix("ABC", 4), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void AlphabetAndAlgorithmNameAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code39Modulo43.Alphabet, Is.EqualTo("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%"));
            Assert.That(Code39Modulo43.AlgorithmName, Is.EqualTo("Code 39 modulo-43"));
        });
    }
}
