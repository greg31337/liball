namespace barcode.Tests;

public class CodabarModulo16Tests
{
    [TestCase("A1234B", 5, '5')]
    [TestCase("A12B", 12, ':')]
    [TestCase("A0A", 0, '0')]
    [TestCase("A9-$:/.+D", 9, '9')]
    [TestCase("A1A", 15, '+')]
    [TestCase("B1A", 14, '.')]
    public void CalculateCheckValueIncludesStartStopAndDataCharacters(string text, int expectedValue, char expectedCharacter)
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodabarModulo16.CalculateCheckValue(text), Is.EqualTo(expectedValue));
            Assert.That(CodabarModulo16.CalculateCheckCharacter(text), Is.EqualTo(expectedCharacter));
        });
    }

    [TestCase("A1234B", "A12345B")]
    [TestCase("A12B", "A12:B")]
    [TestCase("A0A", "A00A")]
    public void AppendCheckCharacterInsertsBeforeStop(string text, string expected)
    {
        string complete = CodabarModulo16.AppendCheckCharacter(text);

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.EqualTo(expected));
            Assert.That(CodabarModulo16.VerifyCompleteText(complete), Is.True);
        });
    }

    [Test]
    public void VerificationDistinguishesMatchingAndMismatchingCodabarChecksums()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodabarModulo16.VerifyCompleteText("A12345B"), Is.True);
            Assert.That(CodabarModulo16.VerifyCompleteText("A12346B"), Is.False);
            Assert.That(CodabarModulo16.VerifyCheckCharacter("A1234B", '5'), Is.True);
            Assert.That(CodabarModulo16.VerifyCheckCharacter("A1234B", '6'), Is.False);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("AB")]
    [TestCase("1234")]
    [TestCase("A12Z")]
    [TestCase("A1AB")]
    [TestCase("a12b")]
    public void CalculationRejectsMalformedFramingAndData(string? text)
    {
        Assert.That(() => CodabarModulo16.CalculateCheckValue(text!), Throws.TypeOf<BarcodeValidationException>());
    }

    [Test]
    public void VerificationRejectsMissingCheckAndNonDataCheckCharacters()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => CodabarModulo16.VerifyCompleteText(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarModulo16.VerifyCompleteText("A1B"), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarModulo16.VerifyCheckCharacter("A1B", 'A'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarModulo16.CalculateFramedSumModulo16(null!, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarModulo16.CalculateFramedSumModulo16("A1B", 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarModulo16.CalculateFramedSumModulo16("A1B", 4), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => CodabarModulo16.CalculateFramedSumModulo16("A?B", 3), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void AlphabetsAndAlgorithmNameAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodabarModulo16.CharacterValues, Is.EqualTo("0123456789-$:/.+ABCD"));
            Assert.That(CodabarModulo16.DataCharacters, Is.EqualTo("0123456789-$:/.+"));
            Assert.That(CodabarModulo16.StartStopCharacters, Is.EqualTo("ABCD"));
            Assert.That(CodabarModulo16.AlgorithmName, Is.EqualTo("Codabar modulo-16"));
        });
    }
}
