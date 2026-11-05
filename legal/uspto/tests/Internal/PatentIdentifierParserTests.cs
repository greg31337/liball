using uspto;

namespace uspto.Tests;

public class PatentIdentifierParserTests
{
    [TestCase("16/123,456", "16123456")]
    [TestCase("08/001,002", "08001002")]
    [TestCase("u.s. 2024/0012345-a1", "US20240012345A1")]
    [TestCase("US 11,234,567 B2", "US11234567B2")]
    [TestCase("pct/br00/000001", "PCTBR00000001")]
    [TestCase("RE 047,123 E1", "RE047123E1")]
    [TestCase("PP-035,001-P2", "PP035001P2")]
    [TestCase("D 001,234 S1", "D001234S1")]
    public void CommonPatentFormsRetainEveryIdentifierCharacter(string input, string expected)
    {
        PatentIdentifierCharacterResult result = PatentIdentifierParser.NormalizeCharacters(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.FailureCode, Is.Null);
            Assert.That(result.HasInvalidCharacter, Is.False);
        });
    }

    [Test]
    public void UnicodeWhitespaceIsRemovedWithoutAcceptingUnicodeLettersOrDigits()
    {
        PatentIdentifierCharacterResult result = PatentIdentifierParser.NormalizeCharacters("\tUS\u00A02024/0000001\r\nA1 ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Value, Is.EqualTo("US20240000001A1"));
            Assert.That(result.SeparatorCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void CharacterCountsDescribeTheNormalizedInputWithoutParsingItsKind()
    {
        PatentIdentifierCharacterResult result = PatentIdentifierParser.NormalizeCharacters("PCT/US 2024/000001");

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("PCTUS2024000001"));
            Assert.That(result.LetterCount, Is.EqualTo(5));
            Assert.That(result.DigitCount, Is.EqualTo(10));
            Assert.That(result.SeparatorCount, Is.EqualTo(3));
        });
    }

    [TestCase("US2024_001", 6, '_')]
    [TestCase("US２０２４", 2, '２')]
    [TestCase("US#123", 2, '#')]
    [TestCase("US\\123", 2, '\\')]
    [TestCase("US–123", 2, '–')]
    public void UnsupportedOrConfusableCharactersAreRejected(string input, int expectedIndex, char expectedCharacter)
    {
        PatentIdentifierCharacterResult result = PatentIdentifierParser.NormalizeCharacters(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Value, Is.Empty);
            Assert.That(result.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
            Assert.That(result.HasInvalidCharacter, Is.True);
            Assert.That(result.InvalidCharacterIndex, Is.EqualTo(expectedIndex));
            Assert.That(result.InvalidCharacter, Is.EqualTo(expectedCharacter));
        });
    }

    [Test]
    public void MissingEmptyAndSeparatorOnlyInputsHaveDistinctFailures()
    {
        PatentIdentifierCharacterResult missing = PatentIdentifierParser.NormalizeCharacters(null!);
        PatentIdentifierCharacterResult empty = PatentIdentifierParser.NormalizeCharacters(string.Empty);
        PatentIdentifierCharacterResult separators = PatentIdentifierParser.NormalizeCharacters(" \t-/.,\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(missing.IsValid, Is.False);
            Assert.That(missing.FailureCode, Is.EqualTo("REQUIRED"));
            Assert.That(empty.IsValid, Is.False);
            Assert.That(empty.FailureCode, Is.EqualTo("EMPTY_IDENTIFIER"));
            Assert.That(separators.IsValid, Is.False);
            Assert.That(separators.FailureCode, Is.EqualTo("EMPTY_IDENTIFIER"));
            Assert.That(separators.SeparatorCount, Is.EqualTo(8));
        });
    }

    [TestCase("00000000")]
    [TestCase("A0000B00")]
    [TestCase("US0000001A0")]
    public void AlphanumericOrderAndLeadingZeroesAreNeverInferredAway(string input)
    {
        PatentIdentifierCharacterResult result = PatentIdentifierParser.NormalizeCharacters(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Value, Is.EqualTo(input));
        });
    }

    [TestCase("14412875")]
    [TestCase("14/412875")]
    [TestCase("14/412,875")]
    [TestCase("US 14/412,875")]
    [TestCase("U.S.-14-412,875")]
    public void ApplicationCompactDisplayAndUsPrefixedFormsNormalizeIdentically(string input)
    {
        PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.PatentApplication));
            Assert.That(result.CanonicalNumber, Is.EqualTo("14412875"));
            Assert.That(result.DisplayNumber, Is.EqualTo("14/412,875"));
            Assert.That(result.CountryCode, Is.EqualTo("US"));
            Assert.That(result.SeriesCode, Is.EqualTo("14"));
            Assert.That(result.SerialNumber, Is.EqualTo("412875"));
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase("01")]
    [TestCase("09")]
    [TestCase("10")]
    [TestCase("18")]
    [TestCase("29")]
    [TestCase("35")]
    [TestCase("60")]
    [TestCase("61")]
    [TestCase("62")]
    [TestCase("63")]
    [TestCase("90")]
    [TestCase("95")]
    [TestCase("96")]
    public void DocumentedApplicationSeriesAreAccepted(string seriesCode)
    {
        PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber(seriesCode + "/000,001");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo(seriesCode + "000001"));
            Assert.That(result.DisplayNumber, Is.EqualTo(seriesCode + "/000,001"));
            Assert.That(result.SerialNumber, Is.EqualTo("000001"));
        });
    }

    [Test]
    public void EveryOrdinarySeriesFromOneThroughEighteenIsAccepted()
    {
        for (int series = 1; series <= 18; series += 1)
        {
            string seriesCode = series.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber(seriesCode + "123456");

            Assert.That(result.IsValid, Is.True, seriesCode);
            Assert.That(result.SeriesCode, Is.EqualTo(seriesCode));
        }
    }

    [Test]
    public void LeadingZeroesArePreservedInBothApplicationFormats()
    {
        PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber("US 08/001,002");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NormalizedInput, Is.EqualTo("US08001002"));
            Assert.That(result.HadCountryPrefix, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo("08001002"));
            Assert.That(result.DisplayNumber, Is.EqualTo("08/001,002"));
            Assert.That(result.SerialNumber, Is.EqualTo("001002"));
        });
    }

    [TestCase("00")]
    [TestCase("19")]
    [TestCase("20")]
    [TestCase("28")]
    [TestCase("30")]
    [TestCase("34")]
    [TestCase("36")]
    [TestCase("59")]
    [TestCase("64")]
    [TestCase("89")]
    [TestCase("91")]
    [TestCase("94")]
    [TestCase("97")]
    [TestCase("99")]
    public void UnassignedApplicationSeriesAreRejected(string seriesCode)
    {
        PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber(seriesCode + "/123,456");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_APPLICATION_SERIES"));
        });
    }

    [TestCase("1234567")]
    [TestCase("123456789")]
    [TestCase("US1234567")]
    [TestCase("US 12/345,678 A1")]
    public void WrongLengthAndKindSuffixesAreRejectedAsApplicationFormats(string input)
    {
        PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo("INVALID_APPLICATION_FORMAT"));
        });
    }

    [TestCase("CA 14/412,875")]
    [TestCase("PCT/US24/123456")]
    [TestCase("APPLICATION 14/412,875")]
    public void NonUsOrDescriptivePrefixesAreNotSilentlyDiscarded(string input)
    {
        PatentApplicationNumberResult result = PatentIdentifierParser.ParseApplicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_APPLICATION_PREFIX"));
        });
    }

    [Test]
    public void ZeroSerialAndInvalidCharactersHaveSpecificFailures()
    {
        PatentApplicationNumberResult zero = PatentIdentifierParser.ParseApplicationNumber("18/000,000");
        PatentApplicationNumberResult invalidCharacter = PatentIdentifierParser.ParseApplicationNumber("18/123_456");

        Assert.Multiple(() =>
        {
            Assert.That(zero.IsValid, Is.False);
            Assert.That(zero.FailureCode, Is.EqualTo("INVALID_APPLICATION_SERIAL"));
            Assert.That(invalidCharacter.IsValid, Is.False);
            Assert.That(invalidCharacter.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
        });
    }

    [TestCase("US20240012345A1")]
    [TestCase("US 2024/0012345 A1")]
    [TestCase("2024-0012345-A1")]
    [TestCase("u.s. 2024 0012345 a1")]
    public void PublicationCompactDisplayAndPrefixedFormsNormalizeIdentically(string input)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.PatentPublication));
            Assert.That(result.CanonicalNumber, Is.EqualTo("US20240012345A1"));
            Assert.That(result.DisplayNumber, Is.EqualTo("US 2024/0012345 A1"));
            Assert.That(result.CountryCode, Is.EqualTo("US"));
            Assert.That(result.PublicationNumber, Is.EqualTo("20240012345"));
            Assert.That(result.Year, Is.EqualTo("2024"));
            Assert.That(result.SequenceNumber, Is.EqualTo("0012345"));
            Assert.That(result.KindCode, Is.EqualTo("A1"));
            Assert.That(result.HasKindCode, Is.True);
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase("20240000001")]
    [TestCase("US20240000001")]
    [TestCase("2024/0000001")]
    public void PublicationCountryAndKindAreOptionalOnInput(string input)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo("US20240000001"));
            Assert.That(result.DisplayNumber, Is.EqualTo("US 2024/0000001"));
            Assert.That(result.HasKindCode, Is.False);
            Assert.That(result.KindCode, Is.Empty);
            Assert.That(result.IsRepublication, Is.False);
            Assert.That(result.IsCorrectedPublication, Is.False);
        });
    }

    [TestCase("A1", false, false)]
    [TestCase("A2", true, false)]
    [TestCase("A9", false, true)]
    public void PublicationKindCodesRetainRepublicationAndCorrectionMeaning(string kindCode, bool isRepublication, bool isCorrection)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber("US 2001/0000001 " + kindCode);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.KindCode, Is.EqualTo(kindCode));
            Assert.That(result.IsRepublication, Is.EqualTo(isRepublication));
            Assert.That(result.IsCorrectedPublication, Is.EqualTo(isCorrection));
            Assert.That(result.CanonicalNumber, Is.EqualTo("US20010000001" + kindCode));
        });
    }

    [Test]
    public void PublicationSequenceLeadingZeroesAndPrefixPresenceArePreserved()
    {
        PatentPublicationNumberResult prefixed = PatentIdentifierParser.ParsePublicationNumber("US 2014-0167116 A9");
        PatentPublicationNumberResult unprefixed = PatentIdentifierParser.ParsePublicationNumber("2014/0167116 A9");

        Assert.Multiple(() =>
        {
            Assert.That(prefixed.IsValid, Is.True);
            Assert.That(prefixed.NormalizedInput, Is.EqualTo("US20140167116A9"));
            Assert.That(prefixed.SequenceNumber, Is.EqualTo("0167116"));
            Assert.That(prefixed.HadCountryPrefix, Is.True);
            Assert.That(unprefixed.IsValid, Is.True);
            Assert.That(unprefixed.SequenceNumber, Is.EqualTo("0167116"));
            Assert.That(unprefixed.HadCountryPrefix, Is.False);
        });
    }

    [TestCase("A0")]
    [TestCase("A3")]
    [TestCase("A8")]
    [TestCase("B1")]
    [TestCase("P1")]
    [TestCase("P4")]
    [TestCase("P9")]
    public void NonAOrUnassignedPublicationKindCodesAreRejected(string kindCode)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber("US20240012345" + kindCode);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_PUBLICATION_KIND"));
        });
    }

    [TestCase("2000/0000001 A1")]
    [TestCase("0000/0000001 A1")]
    [TestCase("9999/0000001 A1")]
    public void YearsOutsideThePreGrantPublicationEraAreRejected(string input)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PUBLICATION_YEAR"));
    }

    [Test]
    public void ZeroPublicationSequenceIsRejected()
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber("US 2024/0000000 A1");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PUBLICATION_SEQUENCE"));
    }

    [TestCase("US2024001234A1")]
    [TestCase("US202400123456A1")]
    [TestCase("US20240012345A")]
    [TestCase("US20240012345A10")]
    [TestCase("US2024/0012345/A1/EXTRA")]
    public void IncorrectPublicationLengthsAreRejected(string input)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PUBLICATION_FORMAT"));
    }

    [TestCase("WO 2024/0012345 A1")]
    [TestCase("EP 2024/0012345 A1")]
    [TestCase("CA 2024/0012345 A1")]
    public void NonUsPublicationCountriesAreNotReinterpreted(string input)
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_PUBLICATION_COUNTRY"));
    }

    [Test]
    public void PublicationInvalidCharactersRetainCharacterFailure()
    {
        PatentPublicationNumberResult result = PatentIdentifierParser.ParsePublicationNumber("US 2024/001_2345 A1");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
    }

    [TestCase("US12156579B2")]
    [TestCase("US 12,156,579 B2")]
    [TestCase("12,156,579 B2")]
    [TestCase("u.s.-12156579-b2")]
    public void UtilityGrantFormsNormalizeIdentically(string input)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.PatentGrant));
            Assert.That(result.CanonicalNumber, Is.EqualTo("US12156579B2"));
            Assert.That(result.DisplayNumber, Is.EqualTo("US 12,156,579 B2"));
            Assert.That(result.CountryCode, Is.EqualTo("US"));
            Assert.That(result.DocumentPrefix, Is.Empty);
            Assert.That(result.NumberDigits, Is.EqualTo("12156579"));
            Assert.That(result.KindCode, Is.EqualTo("B2"));
            Assert.That(result.GrantCategory, Is.EqualTo("Utility"));
            Assert.That(result.HasKindCode, Is.True);
            Assert.That(result.WasPadded, Is.False);
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase("5,778 A", "A")]
    [TestCase("0005778 A1", "A1")]
    public void HistoricalUtilityGrantsUseSevenDigitCanonicalNumbers(string input, string kindCode)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo("US0005778" + kindCode));
            Assert.That(result.DisplayNumber, Is.EqualTo("US 5,778 " + kindCode));
            Assert.That(result.NumberDigits, Is.EqualTo("0005778"));
            Assert.That(result.KindCode, Is.EqualTo(kindCode));
            Assert.That(result.WasPadded, Is.EqualTo(input == "5,778 A"));
        });
    }

    [TestCase("D475502S1", "USD475502S1", "US D475,502 S1", "D", "Design")]
    [TestCase("PP12345P3", "USPP12345P3", "US PP12,345 P3", "PP", "Plant")]
    [TestCase("RE38134E1", "USRE38134E1", "US RE38,134 E1", "RE", "Reissue")]
    [TestCase("H002067H1", "USH002067H1", "US H2,067 H1", "H", "StatutoryInventionRegistration")]
    public void SpecialGrantFamiliesRetainTheirPrefixAndKind(string input, string canonical, string display, string prefix, string category)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo(canonical));
            Assert.That(result.DisplayNumber, Is.EqualTo(display));
            Assert.That(result.DocumentPrefix, Is.EqualTo(prefix));
            Assert.That(result.GrantCategory, Is.EqualTo(category));
            Assert.That(result.HasKindCode, Is.True);
        });
    }

    [TestCase("X1I1", "USX000001I1", "US X1 I1", "X", "XPatent")]
    [TestCase("AI126I3", "USAI00126I3", "US AI126 I3", "AI", "AdditionalImprovement")]
    [TestCase("T109201I4", "UST109201I4", "US T109,201 I4", "T", "DefensivePublication")]
    public void HistoricalGrantFamiliesNormalizeToSearchableWidths(string input, string canonical, string display, string prefix, string category)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo(canonical));
            Assert.That(result.DisplayNumber, Is.EqualTo(display));
            Assert.That(result.DocumentPrefix, Is.EqualTo(prefix));
            Assert.That(result.GrantCategory, Is.EqualTo(category));
        });
    }

    [TestCase("1234567A")]
    [TestCase("1234567A1")]
    [TestCase("1234567B1")]
    [TestCase("1234567B2")]
    [TestCase("1234567B9")]
    [TestCase("D123456S")]
    [TestCase("D123456S1")]
    [TestCase("PP12345P")]
    [TestCase("PP12345P1")]
    [TestCase("PP12345P2")]
    [TestCase("PP12345P3")]
    [TestCase("RE12345E")]
    [TestCase("RE12345E1")]
    [TestCase("H123456H")]
    [TestCase("H123456H1")]
    public void PrintedAndMachineReadableGrantKindsAreAccepted(string input)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("1234567B3")]
    [TestCase("1234567A2")]
    [TestCase("D123456B2")]
    [TestCase("PP12345S1")]
    [TestCase("RE12345P2")]
    [TestCase("H123456E1")]
    [TestCase("X000001I3")]
    [TestCase("AI00126I1")]
    [TestCase("T109201H1")]
    public void UnassignedOrMismatchedGrantKindsAreRejected(string input)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
            Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_GRANT_KIND"));
        });
    }

    [TestCase("123456789")]
    [TestCase("D12345678")]
    [TestCase("PP1234567")]
    [TestCase("RE1234567")]
    [TestCase("H1234567")]
    [TestCase("X1234567")]
    [TestCase("AI1234567")]
    [TestCase("T1234567")]
    public void GrantNumbersBeyondTheirFamilyWidthAreRejected(string input)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_GRANT_FORMAT"));
    }

    [TestCase("0000000")]
    [TestCase("D000000")]
    [TestCase("PP00000")]
    [TestCase("RE00000")]
    [TestCase("H000000")]
    public void ZeroGrantNumbersAreRejected(string input)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_GRANT_NUMBER"));
    }

    [Test]
    public void CountryAndKindAreOptionalButTheirPresenceIsReported()
    {
        PatentGrantNumberResult prefixed = PatentIdentifierParser.ParseGrantNumber("US D1,234");
        PatentGrantNumberResult unprefixed = PatentIdentifierParser.ParseGrantNumber("D001234S1");

        Assert.Multiple(() =>
        {
            Assert.That(prefixed.IsValid, Is.True);
            Assert.That(prefixed.CanonicalNumber, Is.EqualTo("USD001234"));
            Assert.That(prefixed.HadCountryPrefix, Is.True);
            Assert.That(prefixed.HasKindCode, Is.False);
            Assert.That(prefixed.WasPadded, Is.True);
            Assert.That(unprefixed.IsValid, Is.True);
            Assert.That(unprefixed.HadCountryPrefix, Is.False);
            Assert.That(unprefixed.HasKindCode, Is.True);
            Assert.That(unprefixed.WasPadded, Is.False);
        });
    }

    [TestCase("GB1234567B2")]
    [TestCase("WO1234567A1")]
    [TestCase("P12345P1")]
    public void UnsupportedGrantPrefixesAreNotReinterpreted(string input)
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_GRANT_PREFIX"));
    }

    [Test]
    public void GrantInvalidCharactersRetainCharacterFailure()
    {
        PatentGrantNumberResult result = PatentIdentifierParser.ParseGrantNumber("US 12_156_579 B2");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
    }

    [TestCase("PCTUS2025058371")]
    [TestCase("PCT/US2025/058371")]
    [TestCase("pct-us-2025-058371")]
    [TestCase("PCT US 2025 058371")]
    public void CurrentPctFormsNormalizeIdentically(string input)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.PctApplication));
            Assert.That(result.CanonicalNumber, Is.EqualTo("PCTUS2025058371"));
            Assert.That(result.DisplayNumber, Is.EqualTo("PCT/US2025/058371"));
            Assert.That(result.StandardizedNumber, Is.EqualTo("PCTUS2025058371"));
            Assert.That(result.StandardizedDisplayNumber, Is.EqualTo("PCT/US2025/058371"));
            Assert.That(result.CountryCode, Is.EqualTo("US"));
            Assert.That(result.ReceivingOfficeCode, Is.EqualTo("US"));
            Assert.That(result.Year, Is.EqualTo("2025"));
            Assert.That(result.SequenceNumber, Is.EqualTo("058371"));
            Assert.That(result.ExpandedYear, Is.EqualTo("2025"));
            Assert.That(result.ExpandedSequenceNumber, Is.EqualTo("058371"));
            Assert.That(result.IsLegacy, Is.False);
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [TestCase("PCTBR2000000001", "BR")]
    [TestCase("PCT/IB2024/500001", "IB")]
    [TestCase("PCT/EP2011/000123", "EP")]
    public void PctReceivingOfficeCodesArePreserved(string input, string receivingOfficeCode)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CountryCode, Is.EqualTo(receivingOfficeCode));
            Assert.That(result.ReceivingOfficeCode, Is.EqualTo(receivingOfficeCode));
            Assert.That(result.CanonicalNumber, Does.StartWith("PCT" + receivingOfficeCode));
        });
    }

    [TestCase("PCTUS0719317", "PCT/US07/19317", "PCTUS2007019317", "PCT/US2007/019317", "07", "19317", "2007", "019317")]
    [TestCase("PCT/BR00/00001", "PCT/BR00/00001", "PCTBR2000000001", "PCT/BR2000/000001", "00", "00001", "2000", "000001")]
    [TestCase("PCT/SE99/00100", "PCT/SE99/00100", "PCTSE1999000100", "PCT/SE1999/000100", "99", "00100", "1999", "000100")]
    [TestCase("PCTAT7800033", "PCT/AT78/00033", "PCTAT1978000033", "PCT/AT1978/000033", "78", "00033", "1978", "000033")]
    public void LegacyPctFormsRemainApiSafeAndExposeStandardizedEquivalents(string input, string display, string standardized, string standardizedDisplay, string year, string sequence, string expandedYear, string expandedSequence)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.CanonicalNumber, Is.EqualTo(result.NormalizedInput));
            Assert.That(result.DisplayNumber, Is.EqualTo(display));
            Assert.That(result.StandardizedNumber, Is.EqualTo(standardized));
            Assert.That(result.StandardizedDisplayNumber, Is.EqualTo(standardizedDisplay));
            Assert.That(result.Year, Is.EqualTo(year));
            Assert.That(result.SequenceNumber, Is.EqualTo(sequence));
            Assert.That(result.ExpandedYear, Is.EqualTo(expandedYear));
            Assert.That(result.ExpandedSequenceNumber, Is.EqualTo(expandedSequence));
            Assert.That(result.IsLegacy, Is.True);
        });
    }

    [TestCase("US2025058371")]
    [TestCase("WO2025058371")]
    [TestCase("PCXUS2025058371")]
    public void NonPctPrefixesAreRejected(string input)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PCT_PREFIX"));
    }

    [TestCase("PCTUS202505837")]
    [TestCase("PCTUS20250583710")]
    [TestCase("PCTUS071931")]
    [TestCase("PCTUS07193170")]
    [TestCase("PCT")]
    public void IncorrectPctLengthsAreRejected(string input)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PCT_FORMAT"));
    }

    [TestCase("PCTU12024012345")]
    [TestCase("PCT1S2024012345")]
    public void NonAlphabeticReceivingOfficeCodesAreRejected(string input)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PCT_RECEIVING_OFFICE"));
    }

    [TestCase("PCTUS1977000001")]
    [TestCase("PCTUS9999000001")]
    [TestCase("PCTUS7700001")]
    [TestCase("PCTUS20A4000001")]
    public void PctYearsOutsideTheSupportedEraAreRejected(string input)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PCT_YEAR"));
    }

    [TestCase("PCTUS2024000000")]
    [TestCase("PCTUS0700000")]
    public void ZeroPctSequencesAreRejected(string input)
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_PCT_SEQUENCE"));
    }

    [Test]
    public void PctInvalidCharactersRetainCharacterFailure()
    {
        PctApplicationNumberResult result = PatentIdentifierParser.ParsePctApplicationNumber("PCT/US2025/058_371");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
    }

    [TestCase("1234567", "01234567", "01/234,567", "US1234567", "US 1,234,567")]
    [TestCase("11646472", "11646472", "11/646,472", "US11646472", "US 11,646,472")]
    [TestCase("17248024", "17248024", "17/248,024", "US17248024", "US 17,248,024")]
    public void BareNumbersWithTwoValidMeaningsReturnBothCandidates(string input, string applicationCanonical, string applicationDisplay, string grantCanonical, string grantDisplay)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsAmbiguous, Is.True);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Ambiguous));
            Assert.That(result.CandidateCount, Is.EqualTo(2));
            Assert.That(result.Candidates, Has.Length.EqualTo(2));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(IdentifierKind.PatentApplication));
            Assert.That(result.Candidates[0].CanonicalNumber, Is.EqualTo(applicationCanonical));
            Assert.That(result.Candidates[0].DisplayNumber, Is.EqualTo(applicationDisplay));
            Assert.That(result.Candidates[0].CountryCode, Is.EqualTo("US"));
            Assert.That(result.Candidates[0].Explanation, Is.Not.Empty);
            Assert.That(result.Candidates[1].Kind, Is.EqualTo(IdentifierKind.PatentGrant));
            Assert.That(result.Candidates[1].CanonicalNumber, Is.EqualTo(grantCanonical));
            Assert.That(result.Candidates[1].DisplayNumber, Is.EqualTo(grantDisplay));
            Assert.That(result.Candidates[1].CountryCode, Is.EqualTo("US"));
            Assert.That(result.Candidates[1].Explanation, Is.Not.Empty);
            Assert.That(result.CanonicalNumber, Is.Empty);
            Assert.That(result.DisplayNumber, Is.Empty);
            Assert.That(result.FailureCode, Is.Null);
        });
    }

    [Test]
    public void OptionalCountryPrefixDoesNotHideBareNumberAmbiguity()
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(" us 11646472 ");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsAmbiguous, Is.True);
            Assert.That(result.NormalizedInput, Is.EqualTo("US11646472"));
            Assert.That(result.NumericValue, Is.EqualTo("11646472"));
            Assert.That(result.HadCountryPrefix, Is.True);
        });
    }

    [TestCase("09123456", "09123456", "09/123,456")]
    [TestCase("01234567", "01234567", "01/234,567")]
    public void LeadingZeroEightDigitNumbersAreUnambiguousApplications(string input, string canonical, string display)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsAmbiguous, Is.False);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.PatentApplication));
            Assert.That(result.CandidateCount, Is.EqualTo(1));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(IdentifierKind.PatentApplication));
            Assert.That(result.CanonicalNumber, Is.EqualTo(canonical));
            Assert.That(result.DisplayNumber, Is.EqualTo(display));
        });
    }

    [TestCase("0005778", "US0005778", "US 5,778")]
    [TestCase("50123456", "US50123456", "US 50,123,456")]
    [TestCase("99999999", "US99999999", "US 99,999,999")]
    [TestCase("1000000", "US1000000", "US 1,000,000")]
    [TestCase("18000000", "US18000000", "US 18,000,000")]
    public void NumbersWithoutAValidApplicationMeaningReturnOnlyGrantCandidate(string input, string canonical, string display)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsAmbiguous, Is.False);
            Assert.That(result.Kind, Is.EqualTo(IdentifierKind.PatentGrant));
            Assert.That(result.CandidateCount, Is.EqualTo(1));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(IdentifierKind.PatentGrant));
            Assert.That(result.CanonicalNumber, Is.EqualTo(canonical));
            Assert.That(result.DisplayNumber, Is.EqualTo(display));
        });
    }

    [TestCase("17/248,024")]
    [TestCase("11,646,472")]
    [TestCase("US-11646472")]
    [TestCase("U.S.11646472")]
    public void ExplicitlyFormattedIdentifiersAreNotTreatedAsBareNumbers(string input)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsAmbiguous, Is.False);
            Assert.That(result.CandidateCount, Is.Zero);
            Assert.That(result.FailureCode, Is.EqualTo("NOT_BARE_NUMERIC_IDENTIFIER"));
        });
    }

    [TestCase("PCTUS2025058371")]
    [TestCase("D123456S1")]
    [TestCase("1234567B2")]
    [TestCase("GB1234567")]
    public void IdentifiersContainingMeaningfulLettersAreNotBareNumbers(string input)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("NOT_BARE_NUMERIC_IDENTIFIER"));
    }

    [TestCase("123456")]
    [TestCase("123456789")]
    [TestCase("US12345")]
    public void BareNumbersOutsideGrantAndApplicationWidthsAreRejected(string input)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_NUMERIC_IDENTIFIER_LENGTH"));
    }

    [TestCase("0000000")]
    [TestCase("01000000")]
    public void BareNumbersWithNoValidCandidateAreRejected(string input)
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.CandidateCount, Is.Zero);
            Assert.That(result.FailureCode, Is.EqualTo("INVALID_NUMERIC_IDENTIFIER"));
        });
    }

    [Test]
    public void BareNumberInvalidCharactersRetainCharacterFailure()
    {
        PatentIdentifierAmbiguityResult result = PatentIdentifierParser.DetectBareNumericPatentCandidates("1164_6472");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("INVALID_CHARACTER"));
    }

    [Test]
    public void GeneratedPatentFamiliesPreserveDigitsAcrossCasePunctuationAndUnicodeWhitespace()
    {
        string[] applicationSeries = new string[] { "01", "09", "18", "29", "35", "60", "63", "90", "96" };
        string[] publicationKinds = new string[] { "A1", "A2", "A9" };
        string[][] grantFamilies = new string[][]
        {
            new string[] { string.Empty, "7", "B2" },
            new string[] { "D", "6", "S1" },
            new string[] { "PP", "5", "P3" },
            new string[] { "RE", "5", "E1" },
            new string[] { "H", "6", "H1" },
            new string[] { "X", "6", "I1" },
            new string[] { "AI", "5", "I3" },
            new string[] { "T", "6", "I4" }
        };
        string[] receivingOffices = new string[] { "US", "IB", "EP", "BR" };
        string publicationYear = DateTime.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);

        for (int value = 1; value <= 128; value += 1)
        {
            string applicationSerial = value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            string applicationSeriesCode = applicationSeries[(value - 1) % applicationSeries.Length];
            string applicationInput = "\u2003u.s. " + applicationSeriesCode + "/" + applicationSerial.Substring(0, 3) + "," + applicationSerial.Substring(3, 3) + "\t";
            PatentApplicationNumberResult application = PatentIdentifierParser.ParseApplicationNumber(applicationInput);

            string publicationSequence = value.ToString("D7", System.Globalization.CultureInfo.InvariantCulture);
            string publicationKind = publicationKinds[(value - 1) % publicationKinds.Length];
            string publicationInput = "\u00A0u.s. " + publicationYear + "/" + publicationSequence + "-" + publicationKind.ToLowerInvariant() + "\r\n";
            PatentPublicationNumberResult publication = PatentIdentifierParser.ParsePublicationNumber(publicationInput);

            string[] grantFamily = grantFamilies[(value - 1) % grantFamilies.Length];
            int grantWidth = Int32.Parse(grantFamily[1], System.Globalization.CultureInfo.InvariantCulture);
            string grantDigits = value.ToString("D" + grantWidth.ToString(System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
            string grantInput = "\u2009u.s.-" + grantFamily[0].ToLowerInvariant() + grantDigits + "-" + grantFamily[2].ToLowerInvariant();
            PatentGrantNumberResult grant = PatentIdentifierParser.ParseGrantNumber(grantInput);

            string pctSequence = value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            string receivingOffice = receivingOffices[(value - 1) % receivingOffices.Length];
            string pctInput = "\u202Fpct/" + receivingOffice.ToLowerInvariant() + publicationYear + "/" + pctSequence + " ";
            PctApplicationNumberResult pct = PatentIdentifierParser.ParsePctApplicationNumber(pctInput);

            Assert.Multiple(() =>
            {
                Assert.That(application.IsValid, Is.True, applicationInput);
                Assert.That(application.CanonicalNumber, Is.EqualTo(applicationSeriesCode + applicationSerial), applicationInput);
                Assert.That(publication.IsValid, Is.True, publicationInput);
                Assert.That(publication.CanonicalNumber, Is.EqualTo("US" + publicationYear + publicationSequence + publicationKind), publicationInput);
                Assert.That(grant.IsValid, Is.True, grantInput);
                Assert.That(grant.CanonicalNumber, Is.EqualTo("US" + grantFamily[0] + grantDigits + grantFamily[2]), grantInput);
                Assert.That(pct.IsValid, Is.True, pctInput);
                Assert.That(pct.CanonicalNumber, Is.EqualTo("PCT" + receivingOffice + publicationYear + pctSequence), pctInput);
            });
        }
    }
}
