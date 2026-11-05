using System;
using System.Globalization;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class ScalarCodecTests
{
    [Test]
    public void IntegerAliasesParseTheirFullRangesAndConstraints()
    {
        ScalarParseResult minimum = ScalarParser.ParseScalar("-2147483648"u8, FixFieldType.Int, 32);
        ScalarParseResult maximum = ScalarParser.ParseScalar("2147483647"u8, FixFieldType.Int, 32);
        ScalarParseResult leadingZeros = ScalarParser.ParseScalar("00023"u8, FixFieldType.Int, 32);
        ScalarParseResult sequence = ScalarParser.ParseScalar("1"u8, FixFieldType.SeqNum, 32);
        ScalarParseResult groupZero = ScalarParser.ParseScalar("0"u8, FixFieldType.NumInGroup, 32);
        ScalarParseResult length = ScalarParser.ParseScalar("12"u8, FixFieldType.Length, 32);
        ScalarParseResult tag = ScalarParser.ParseScalar("35"u8, FixFieldType.TagNum, 32);

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Success, Is.True);
            Assert.That(minimum.IntegerValue, Is.EqualTo(int.MinValue));
            Assert.That(maximum.IntegerValue, Is.EqualTo(int.MaxValue));
            Assert.That(leadingZeros.IntegerValue, Is.EqualTo(23));
            Assert.That(sequence.IntegerValue, Is.EqualTo(1));
            Assert.That(groupZero.Success, Is.True);
            Assert.That(groupZero.IntegerValue, Is.Zero);
            Assert.That(length.IntegerValue, Is.EqualTo(12));
            Assert.That(tag.IntegerValue, Is.EqualTo(35));
        });
    }

    [Test]
    public void IntegerAliasesRejectMalformedOverflowAndIllegalDomainValues()
    {
        ScalarParseResult positiveSign = ScalarParser.ParseScalar("+1"u8, FixFieldType.Int, 32);
        ScalarParseResult overflow = ScalarParser.ParseScalar("2147483648"u8, FixFieldType.Int, 32);
        ScalarParseResult negativeOverflow = ScalarParser.ParseScalar("-2147483649"u8, FixFieldType.Int, 32);
        ScalarParseResult sequenceZero = ScalarParser.ParseScalar("0"u8, FixFieldType.SeqNum, 32);
        ScalarParseResult lengthZero = ScalarParser.ParseScalar("0"u8, FixFieldType.Length, 32);
        ScalarParseResult negativeGroup = ScalarParser.ParseScalar("-1"u8, FixFieldType.NumInGroup, 32);
        ScalarParseResult tagZero = ScalarParser.ParseScalar("0"u8, FixFieldType.TagNum, 32);
        ScalarParseResult tagLeadingZero = ScalarParser.ParseScalar("035"u8, FixFieldType.TagNum, 32);
        ScalarParseResult limited = ScalarParser.ParseScalar("1234"u8, FixFieldType.Int, 3);

        Assert.Multiple(() =>
        {
            Assert.That(positiveSign.ErrorCode, Is.EqualTo("invalid-digit"));
            Assert.That(overflow.ErrorCode, Is.EqualTo("overflow"));
            Assert.That(negativeOverflow.ErrorCode, Is.EqualTo("overflow"));
            Assert.That(sequenceZero.ErrorCode, Is.EqualTo("zero-not-allowed"));
            Assert.That(lengthZero.ErrorCode, Is.EqualTo("zero-not-allowed"));
            Assert.That(negativeGroup.ErrorCode, Is.EqualTo("negative-value"));
            Assert.That(tagZero.ErrorCode, Is.EqualTo("zero-not-allowed"));
            Assert.That(tagLeadingZero.ErrorCode, Is.EqualTo("leading-zero"));
            Assert.That(limited.ErrorCode, Is.EqualTo("limit-exceeded"));
        });
    }

    [Test]
    public void IntegerFormattingIsCanonicalAndReportsDestinationSize()
    {
        byte[] minimumDestination = new byte[11];
        ScalarFormatResult minimum = ScalarFormatter.FormatInteger(int.MinValue, FixFieldType.Int, minimumDestination);
        byte[] tagDestination = new byte[2];
        ScalarFormatResult tag = ScalarFormatter.FormatInteger(35, FixFieldType.TagNum, tagDestination);
        byte[] shortDestination = [88];
        ScalarFormatResult shortResult = ScalarFormatter.FormatInteger(12, FixFieldType.Int, shortDestination);
        ScalarFormatResult invalidSequence = ScalarFormatter.FormatInteger(0, FixFieldType.SeqNum, new byte[1]);
        ScalarFormatResult invalidAlias = ScalarFormatter.FormatInteger(-1, FixFieldType.NumInGroup, new byte[2]);

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Success, Is.True);
            Assert.That(Encoding.ASCII.GetString(minimumDestination), Is.EqualTo("-2147483648"));
            Assert.That(tag.Success, Is.True);
            Assert.That(Encoding.ASCII.GetString(tagDestination), Is.EqualTo("35"));
            Assert.That(shortResult.Required, Is.EqualTo(2));
            Assert.That(shortResult.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 88 }));
            Assert.That(invalidSequence.ErrorCode, Is.EqualTo("zero-not-allowed"));
            Assert.That(invalidAlias.ErrorCode, Is.EqualTo("negative-value"));
        });
    }

    [Test]
    public void DecimalAliasesAcceptInvariantLeadingTrailingAndNegativeForms()
    {
        FixFieldType[] aliases =
        [
            FixFieldType.Float,
            FixFieldType.Qty,
            FixFieldType.Price,
            FixFieldType.PriceOffset,
            FixFieldType.Amt,
            FixFieldType.Percentage
        ];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < aliases.Length; index++)
            {
                ScalarParseResult parsed = ScalarParser.ParseScalar("-00023.2300"u8, aliases[index], 64);
                Assert.That(parsed.Success, Is.True, aliases[index].ToString());
                Assert.That(parsed.DecimalValue, Is.EqualTo(-23.23m), aliases[index].ToString());
                Assert.That(parsed.FractionalDigits, Is.EqualTo(4), aliases[index].ToString());
            }
        });

        ScalarParseResult trailingPoint = ScalarParser.ParseScalar("23."u8, FixFieldType.Float, 64);
        ScalarParseResult maximum = ScalarParser.ParseScalar("79228162514264337593543950335"u8, FixFieldType.Amt, 64);
        Assert.That(trailingPoint.Success, Is.True);
        Assert.That(trailingPoint.DecimalValue, Is.EqualTo(23m));
        Assert.That(maximum.Success, Is.True);
        Assert.That(maximum.DecimalValue, Is.EqualTo(decimal.MaxValue));
    }

    [Test]
    public void DecimalParsingRejectsMalformedOverflowAndUnsupportedPrecision()
    {
        ScalarParseResult leadingPoint = ScalarParser.ParseScalar(".5"u8, FixFieldType.Float, 64);
        ScalarParseResult repeatedPoint = ScalarParser.ParseScalar("1.2.3"u8, FixFieldType.Float, 64);
        ScalarParseResult positiveSign = ScalarParser.ParseScalar("+1.5"u8, FixFieldType.Float, 64);
        ScalarParseResult overflow = ScalarParser.ParseScalar("79228162514264337593543950336"u8, FixFieldType.Float, 64);
        ScalarParseResult precision = ScalarParser.ParseScalar("0.12345678901234567890123456789"u8, FixFieldType.Float, 64);

        Assert.Multiple(() =>
        {
            Assert.That(leadingPoint.ErrorCode, Is.EqualTo("invalid-decimal-point"));
            Assert.That(repeatedPoint.ErrorCode, Is.EqualTo("invalid-decimal-point"));
            Assert.That(positiveSign.ErrorCode, Is.EqualTo("invalid-digit"));
            Assert.That(overflow.ErrorCode, Is.EqualTo("overflow"));
            Assert.That(precision.ErrorCode, Is.EqualTo("precision-exceeded"));
        });
    }

    [Test]
    public void DecimalFormattingIsCanonicalAndIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            byte[] destination = new byte[32];
            ScalarFormatResult formatted = ScalarFormatter.FormatDecimal(-23.2300m, FixFieldType.Price, destination);
            string text = Encoding.ASCII.GetString(destination, 0, formatted.Written);
            ScalarParseResult parsed = ScalarParser.ParseScalar(destination.AsSpan(0, formatted.Written), FixFieldType.Price, 32);

            Assert.Multiple(() =>
            {
                Assert.That(formatted.Success, Is.True);
                Assert.That(text, Is.EqualTo("-23.23"));
                Assert.That(parsed.Success, Is.True);
                Assert.That(parsed.DecimalValue, Is.EqualTo(-23.23m));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        ScalarFormatResult shortResult = ScalarFormatter.FormatDecimal(12.5m, FixFieldType.Float, new byte[3]);
        Assert.That(shortResult.Required, Is.EqualTo(4));
        Assert.That(shortResult.ErrorCode, Is.EqualTo("destination-too-small"));
    }

    [Test]
    public void CharacterAndBooleanValuesUseExactSingleByteLexicalForms()
    {
        ScalarParseResult character = ScalarParser.ParseScalar(new byte[] { 233 }, FixFieldType.Char, 4);
        ScalarParseResult yes = ScalarParser.ParseScalar("Y"u8, FixFieldType.Boolean, 4);
        ScalarParseResult no = ScalarParser.ParseScalar("N"u8, FixFieldType.Boolean, 4);
        ScalarParseResult invalidBoolean = ScalarParser.ParseScalar("y"u8, FixFieldType.Boolean, 4);
        ScalarParseResult longCharacter = ScalarParser.ParseScalar("AB"u8, FixFieldType.Char, 4);
        ScalarParseResult delimiter = ScalarParser.ParseScalar(new byte[] { FixDefaults.Soh }, FixFieldType.Char, 4);
        byte[] booleanDestination = new byte[1];
        ScalarFormatResult formattedBoolean = ScalarFormatter.FormatBoolean(false, booleanDestination);
        byte[] characterDestination = new byte[1];
        ScalarFormatResult formattedCharacter = ScalarFormatter.FormatCharacter((char)233, FixFieldType.Char, characterDestination);

        Assert.Multiple(() =>
        {
            Assert.That(character.CharacterValue, Is.EqualTo((char)233));
            Assert.That(yes.BooleanValue, Is.True);
            Assert.That(no.BooleanValue, Is.False);
            Assert.That(invalidBoolean.ErrorCode, Is.EqualTo("invalid-boolean"));
            Assert.That(longCharacter.ErrorCode, Is.EqualTo("invalid-length"));
            Assert.That(delimiter.ErrorCode, Is.EqualTo("delimiter-not-allowed"));
            Assert.That(formattedBoolean.Success, Is.True);
            Assert.That(booleanDestination[0], Is.EqualTo(78));
            Assert.That(formattedCharacter.Success, Is.True);
            Assert.That(characterDestination[0], Is.EqualTo(233));
        });
    }

    [Test]
    public void TextAliasesPreserveLatin1AndEnforceDatatypeLengths()
    {
        ScalarParseResult text = ScalarParser.ParseScalar(new byte[] { 67, 97, 102, 233 }, FixFieldType.String, 16);
        ScalarParseResult country = ScalarParser.ParseScalar("US"u8, FixFieldType.Country, 16);
        ScalarParseResult currency = ScalarParser.ParseScalar("USD"u8, FixFieldType.Currency, 16);
        ScalarParseResult exchange = ScalarParser.ParseScalar("XNAS"u8, FixFieldType.Exchange, 16);
        ScalarParseResult badCountry = ScalarParser.ParseScalar("USA"u8, FixFieldType.Country, 16);
        ScalarParseResult delimiter = ScalarParser.ParseScalar(new byte[] { 65, 1, 66 }, FixFieldType.String, 16);
        byte[] destination = new byte[4];
        ScalarFormatResult formatted = ScalarFormatter.FormatText("Café", FixFieldType.String, destination);
        ScalarFormatResult nonLatin1 = ScalarFormatter.FormatText("€", FixFieldType.String, new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(text.TextValue, Is.EqualTo("Café"));
            Assert.That(country.Success, Is.True);
            Assert.That(currency.Success, Is.True);
            Assert.That(exchange.Success, Is.True);
            Assert.That(badCountry.ErrorCode, Is.EqualTo("invalid-length"));
            Assert.That(delimiter.ErrorCode, Is.EqualTo("delimiter-not-allowed"));
            Assert.That(formatted.Success, Is.True);
            Assert.That(destination, Is.EqualTo(new byte[] { 67, 97, 102, 233 }));
            Assert.That(nonLatin1.ErrorCode, Is.EqualTo("invalid-character"));
        });
    }

    [Test]
    public void MultipleValuesRoundTripAndRejectEmptyOrInvalidTokens()
    {
        ScalarParseResult characters = ScalarParser.ParseScalar("2 A f"u8, FixFieldType.MultipleCharValue, 32);
        ScalarParseResult strings = ScalarParser.ParseScalar("AV AN A"u8, FixFieldType.MultipleStringValue, 32);
        ScalarParseResult emptyToken = ScalarParser.ParseScalar("AV  AN"u8, FixFieldType.MultipleStringValue, 32);
        ScalarParseResult invalidCharacter = ScalarParser.ParseScalar("A ?"u8, FixFieldType.MultipleCharValue, 32);
        byte[] destination = new byte[16];
        ScalarFormatResult formatted = ScalarFormatter.FormatMultipleValues(["AV", "AN", "A"], FixFieldType.MultipleStringValue, destination);
        ScalarParseResult reparsed = ScalarParser.ParseScalar(destination.AsSpan(0, formatted.Written), FixFieldType.MultipleStringValue, 32);
        ScalarFormatResult embeddedSpace = ScalarFormatter.FormatMultipleValues(["AV AN"], FixFieldType.MultipleStringValue, destination);

        Assert.Multiple(() =>
        {
            Assert.That(characters.Values, Is.EqualTo(new[] { "2", "A", "f" }));
            Assert.That(strings.Values, Is.EqualTo(new[] { "AV", "AN", "A" }));
            Assert.That(emptyToken.ErrorCode, Is.EqualTo("empty-multivalue-token"));
            Assert.That(invalidCharacter.ErrorCode, Is.EqualTo("invalid-multiple-char-value"));
            Assert.That(formatted.Success, Is.True);
            Assert.That(Encoding.ASCII.GetString(destination, 0, formatted.Written), Is.EqualTo("AV AN A"));
            Assert.That(reparsed.Values, Is.EqualTo(new[] { "AV", "AN", "A" }));
            Assert.That(embeddedSpace.ErrorCode, Is.EqualTo("invalid-multivalue-token"));
        });
    }

    [Test]
    public void UnsupportedScalarTypesReturnExplicitFailures()
    {
        ScalarParseResult parsed = ScalarParser.ParseScalar("20260830"u8, FixFieldType.LocalMktDate, 32);
        ScalarFormatResult integer = ScalarFormatter.FormatInteger(1, FixFieldType.Price, new byte[4]);
        ScalarFormatResult text = ScalarFormatter.FormatText("ABC", FixFieldType.Data, new byte[4]);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.ErrorCode, Is.EqualTo("unsupported-field-type"));
            Assert.That(integer.ErrorCode, Is.EqualTo("unsupported-field-type"));
            Assert.That(text.ErrorCode, Is.EqualTo("unsupported-field-type"));
        });
    }
}
