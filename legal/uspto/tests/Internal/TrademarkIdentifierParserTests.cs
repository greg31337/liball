using uspto;

namespace uspto.Tests;

public class TrademarkIdentifierParserTests
{
    [TestCase("87650005")]
    [TestCase("sn87650005")]
    [TestCase("SN 87/650,005")]
    [TestCase("sn:87-650005")]
    [TestCase("SN#87650005")]
    public void SerialNumberFormsNormalizeIdentically(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(result.CanonicalNumber, Is.EqualTo("sn87650005"));
            Assert.That(result.DisplayNumber, Is.EqualTo("SN 87/650,005"));
            Assert.That(result.CountryCode, Is.EqualTo("US"));
            Assert.That(result.ApiPrefix, Is.EqualTo("sn"));
            Assert.That(result.IdentifierValue, Is.EqualTo("87650005"));
            Assert.That(result.WasPadded, Is.False);
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase("6556072")]
    [TestCase("rn6556072")]
    [TestCase("RN 6,556,072")]
    [TestCase("r.n.#6-556-072")]
    public void SevenDigitRegistrationFormsNormalizeIdentically(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TrademarkIdentifierKind.RegistrationNumber));
            Assert.That(result.CanonicalNumber, Is.EqualTo("rn6556072"));
            Assert.That(result.DisplayNumber, Is.EqualTo("RN 6,556,072"));
            Assert.That(result.CountryCode, Is.EqualTo("US"));
            Assert.That(result.ApiPrefix, Is.EqualTo("rn"));
            Assert.That(result.IdentifierValue, Is.EqualTo("6556072"));
            Assert.That(result.WasPadded, Is.False);
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase("88725", false)]
    [TestCase("rn88725", true)]
    [TestCase("RN 88,725", true)]
    public void HistoricalRegistrationNumbersArePaddedToSevenPositions(string input, bool hasExplicitPrefix)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo("rn0088725"));
            Assert.That(result.DisplayNumber, Is.EqualTo("RN 88,725"));
            Assert.That(result.IdentifierValue, Is.EqualTo("0088725"));
            Assert.That(result.HasExplicitPrefix, Is.EqualTo(hasExplicitPrefix));
            Assert.That(result.WasPadded, Is.True);
        });
    }

    [TestCase("refZ1231384", "Z1231384")]
    [TestCase("REF Z1231384", "Z1231384")]
    [TestCase("ref:A0126368", "A0126368")]
    [TestCase("r.e.f.-a0126368", "A0126368")]
    public void ReferenceNumberFormsPreserveTheirAlphanumericValue(string input, string identifierValue)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(TrademarkIdentifierKind.ReferenceNumber));
            Assert.That(result.CanonicalNumber, Is.EqualTo("ref" + identifierValue));
            Assert.That(result.DisplayNumber, Is.EqualTo("REF " + identifierValue));
            Assert.That(result.ApiPrefix, Is.EqualTo("ref"));
            Assert.That(result.IdentifierValue, Is.EqualTo(identifierValue));
            Assert.That(result.HasExplicitPrefix, Is.True);
            Assert.That(result.WasPadded, Is.False);
        });
    }

    [Test]
    public void BareWidthsInferSerialAndRegistrationWithoutChangingDigits()
    {
        TrademarkIdentifierResult serial = TrademarkIdentifierParser.ParseTrademarkIdentifier("72131351");
        TrademarkIdentifierResult registration = TrademarkIdentifierParser.ParseTrademarkIdentifier("3500030");

        Assert.Multiple(() =>
        {
            Assert.That(serial.Kind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(serial.HasExplicitPrefix, Is.False);
            Assert.That(serial.IdentifierValue, Is.EqualTo("72131351"));
            Assert.That(registration.Kind, Is.EqualTo(TrademarkIdentifierKind.RegistrationNumber));
            Assert.That(registration.HasExplicitPrefix, Is.False);
            Assert.That(registration.IdentifierValue, Is.EqualTo("3500030"));
        });
    }

    [Test]
    public void GeneratedTrademarkFamiliesNormalizeIdempotentlyAcrossPunctuationAndUnicodeWhitespace()
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();
        for (int value = 1; value <= 260; value += 1)
        {
            string serialValue = (70000000 + value).ToString("D8", System.Globalization.CultureInfo.InvariantCulture);
            string serialInput = "\u2003s.n:#" + serialValue.Substring(0, 2) + "/" + serialValue.Substring(2, 3) + "," + serialValue.Substring(5, 3) + "\t";
            TrademarkIdentifierResult serial = TrademarkIdentifierParser.ParseTrademarkIdentifier(serialInput);

            string registrationInputValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string registrationValue = value.ToString("D7", System.Globalization.CultureInfo.InvariantCulture);
            string registrationInput = "\u00A0r.n.-#" + registrationInputValue + "\r\n";
            TrademarkIdentifierResult registration = TrademarkIdentifierParser.ParseTrademarkIdentifier(registrationInput);

            char referenceLetter = (char)('A' + (value - 1) % 26);
            string referenceDigits = value.ToString("D7", System.Globalization.CultureInfo.InvariantCulture);
            string referenceInput = "\u202Fr.e.f.:" + global::System.Char.ToLowerInvariant(referenceLetter) + referenceDigits.Substring(0, 3) + "-" + referenceDigits.Substring(3, 4) + " ";
            TrademarkIdentifierResult reference = TrademarkIdentifierParser.ParseTrademarkIdentifier(referenceInput);

            Assert.Multiple(() =>
            {
                Assert.That(serial.IsValid, Is.True, serialInput);
                Assert.That(serial.CanonicalNumber, Is.EqualTo("sn" + serialValue), serialInput);
                Assert.That(normalizer.NormalizeTrademarkIdentifier(serialInput), Is.EqualTo(serial.CanonicalNumber), serialInput);
                Assert.That(normalizer.TryNormalizeTrademarkIdentifier(serial.CanonicalNumber), Is.EqualTo(serial.CanonicalNumber), serialInput);

                Assert.That(registration.IsValid, Is.True, registrationInput);
                Assert.That(registration.CanonicalNumber, Is.EqualTo("rn" + registrationValue), registrationInput);
                Assert.That(registration.WasPadded, Is.True, registrationInput);
                Assert.That(normalizer.NormalizeTrademarkIdentifier(registrationInput), Is.EqualTo(registration.CanonicalNumber), registrationInput);
                Assert.That(normalizer.TryNormalizeTrademarkIdentifier(registration.CanonicalNumber), Is.EqualTo(registration.CanonicalNumber), registrationInput);

                Assert.That(reference.IsValid, Is.True, referenceInput);
                Assert.That(reference.CanonicalNumber, Is.EqualTo("ref" + referenceLetter + referenceDigits), referenceInput);
                Assert.That(normalizer.NormalizeTrademarkIdentifier(referenceInput), Is.EqualTo(reference.CanonicalNumber), referenceInput);
                Assert.That(normalizer.TryNormalizeTrademarkIdentifier(reference.CanonicalNumber), Is.EqualTo(reference.CanonicalNumber), referenceInput);
            });
        }
    }

    [TestCase("sn1234567")]
    [TestCase("sn123456789")]
    [TestCase("sn12A45678")]
    [TestCase("sn00000000")]
    [TestCase("sn")]
    public void InvalidSerialNumbersAreRejected(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_TRADEMARK_SERIAL"));
    }

    [TestCase("rn12345678")]
    [TestCase("rn12A4567")]
    [TestCase("rn0000000")]
    [TestCase("rn")]
    public void InvalidRegistrationNumbersAreRejected(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_TRADEMARK_REGISTRATION"));
    }

    [TestCase("ref12345678")]
    [TestCase("refZ123138")]
    [TestCase("refZ12313845")]
    [TestCase("refZ123A384")]
    [TestCase("refZ0000000")]
    [TestCase("ref")]
    public void InvalidReferenceNumbersAreRejected(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_TRADEMARK_REFERENCE"));
    }

    [TestCase("Z1231384")]
    [TestCase("ir0835690")]
    [TestCase("serial87650005")]
    [TestCase("US87650005")]
    public void MissingOrUnsupportedPrefixesAreNotSilentlyDiscarded(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_TRADEMARK_PREFIX"));
    }

    [TestCase("123456789")]
    [TestCase("1234567890")]
    public void UnprefixedNumbersBeyondSupportedWidthsAreRejected(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_TRADEMARK_FORMAT"));
    }

    [Test]
    public void MissingEmptyAndSeparatorOnlyInputsHaveSpecificFailures()
    {
        TrademarkIdentifierResult missing = TrademarkIdentifierParser.ParseTrademarkIdentifier(null!);
        TrademarkIdentifierResult empty = TrademarkIdentifierParser.ParseTrademarkIdentifier(string.Empty);
        TrademarkIdentifierResult separators = TrademarkIdentifierParser.ParseTrademarkIdentifier(" /,.-:# ");

        Assert.Multiple(() =>
        {
            Assert.That(missing.FailureCode, Is.EqualTo("REQUIRED"));
            Assert.That(empty.FailureCode, Is.EqualTo("EMPTY_IDENTIFIER"));
            Assert.That(separators.FailureCode, Is.EqualTo("EMPTY_IDENTIFIER"));
        });
    }

    [TestCase("sn8765_0005")]
    [TestCase("rn６５５６０７２")]
    [TestCase("refZ123@1384")]
    public void UnsupportedOrConfusableCharactersAreRejected(string input)
    {
        TrademarkIdentifierResult result = TrademarkIdentifierParser.ParseTrademarkIdentifier(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
    }
}
