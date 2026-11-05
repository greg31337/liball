using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceIbanTests
{
    [Test]
    public void CatalogExposesPinnedSourceAndCountryFormats()
    {
        FinanceProvider finance = Fake.Create(9951).Finance();
        IbanCatalogMetadata metadata = finance.IbanMetadata();
        IbanFormat germany = finance.IbanFormat("de");
        IbanFormat britain = finance.IbanFormat("GB");
        IbanFormat brazil = finance.IbanFormat("BR");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Source, Is.EqualTo("schwifty IBAN registry transcription"));
            Assert.That(metadata.SourceVersion, Is.EqualTo("schwifty 2026.7.3"));
            Assert.That(metadata.Upstream, Is.EqualTo("SWIFT ISO 13616 IBAN Registry"));
            Assert.That(metadata.RetrievedOn, Is.EqualTo("2026-08-15"));
            Assert.That(metadata.RecordCount, Is.EqualTo(103));
            Assert.That(germany.IbanLength, Is.EqualTo(22));
            Assert.That(germany.BbanPattern, Is.EqualTo("8!n10!n"));
            Assert.That(germany.BankCodeLength, Is.EqualTo(8));
            Assert.That(germany.AccountCodeLength, Is.EqualTo(10));
            Assert.That(germany.IsSepa, Is.True);
            Assert.That(britain.BbanPattern, Is.EqualTo("4!a6!n8!n"));
            Assert.That(britain.BranchCodeStart, Is.EqualTo(4));
            Assert.That(britain.BranchCodeLength, Is.EqualTo(6));
            Assert.That(brazil.BbanPattern, Is.EqualTo("8!n5!n10!n1!a1!c"));
            Assert.That(brazil.IbanLength, Is.EqualTo(29));
        });
    }

    [Test]
    public void EveryCountryGeneratesItsDeclaredBbanShapeAndValidIban()
    {
        FinanceProvider finance = Fake.Create(9952).Finance();

        foreach (IbanFormat format in finance.IbanFormats())
        {
            IbanRecord record = finance.IbanRecordFromCountryCode(format.CountryCode);

            Assert.Multiple(() =>
            {
                Assert.That(record.CountryCode, Is.EqualTo(format.CountryCode));
                Assert.That(record.IbanPrefix, Is.EqualTo(format.IbanPrefix));
                Assert.That(record.Electronic, Does.StartWith(format.IbanPrefix));
                Assert.That(record.Electronic, Has.Length.EqualTo(format.IbanLength));
                Assert.That(record.Bban, Has.Length.EqualTo(format.BbanLength));
                Assert.That(MatchesPattern(record.Bban, format.BbanPattern), Is.True,
                    format.CountryCode + " BBAN did not match " + format.BbanPattern);
                Assert.That(finance.IsIbanValid(record.Electronic), Is.True);
                Assert.That(finance.IsIbanValid(record.Print), Is.True);
                Assert.That(record.IsMod97Valid, Is.True);
            });
        }
    }

    [Test]
    public void GenerationIsDeterministicAndScalarFormsStayCorrelated()
    {
        const long seed = 9953;
        IbanRecord first = Fake.Create(seed).Finance().IbanRecordFromCountryCode("GB");
        IbanRecord second = Fake.Create(seed).Finance().IbanRecordFromCountryCode("GB");
        string electronic = Fake.Create(seed).Finance().IbanFromCountryCode("GB");
        string print = Fake.Create(seed).Finance().IbanPrintFromCountryCode("GB");
        string bban = Fake.Create(seed).Finance().Bban("GB");

        Assert.Multiple(() =>
        {
            AssertEquivalent(first, second);
            Assert.That(electronic, Is.EqualTo(first.Electronic));
            Assert.That(print, Is.EqualTo(first.Print));
            Assert.That(bban, Is.EqualTo(first.Bban));
            Assert.That(first.Electronic, Does.Match("^[A-Z0-9]+$"));
            Assert.That(first.Print.Replace(" ", string.Empty), Is.EqualTo(first.Electronic));
            Assert.That(first.Print.Split(' '), Has.All.Length.LessThanOrEqualTo(4));
        });
    }

    [Test]
    public void RecordFieldsCorrelateWithoutClaimingAnActiveAccount()
    {
        FinanceProvider finance = Fake.Create(9954).Finance();
        IbanFormat format = finance.IbanFormat("GB");
        IbanRecord record = finance.IbanRecordFromCountryCode("GB");

        Assert.Multiple(() =>
        {
            Assert.That(record.BankCode,
                Is.EqualTo(record.Bban.Substring(format.BankCodeStart, format.BankCodeLength)));
            Assert.That(record.BranchCode,
                Is.EqualTo(record.Bban.Substring(format.BranchCodeStart, format.BranchCodeLength)));
            Assert.That(record.AccountCode,
                Is.EqualTo(record.Bban.Substring(format.AccountCodeStart, format.AccountCodeLength)));
            Assert.That(record.Length, Is.EqualTo(record.Electronic.Length));
            Assert.That(record.IsSynthetic, Is.True);
            Assert.That(record.TestMarker, Is.EqualTo("fake.synthetic-iban.v1"));
            Assert.That(record.RepresentsActiveAccount, Is.False);
            Assert.That(record.RegistryVersion, Is.EqualTo("schwifty 2026.7.3"));
        });
    }

    [Test]
    public void TerritorySelectionRetainsItsCountryWhileUsingTheRegistryPrefix()
    {
        FinanceProvider finance = Fake.Create(9955).Finance();
        IbanFormat format = finance.IbanFormat("AX");
        IbanRecord record = finance.IbanRecordFromCountryCode("AX");

        Assert.Multiple(() =>
        {
            Assert.That(format.CountryCode, Is.EqualTo("AX"));
            Assert.That(format.IbanPrefix, Is.EqualTo("FI"));
            Assert.That(record.CountryCode, Is.EqualTo("AX"));
            Assert.That(record.IbanPrefix, Is.EqualTo("FI"));
            Assert.That(record.Electronic, Does.StartWith("FI"));
            Assert.That(finance.IsIbanValid(record.Electronic), Is.True);
        });
    }

    [Test]
    public void ValidationAcceptsKnownValuesAndPrintFormattingNormalizesCase()
    {
        FinanceProvider finance = Fake.Create(9956).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.IsIbanValid("DE89370400440532013000"), Is.True);
            Assert.That(finance.IsIbanValid("gb82 west 1234 5698 7654 32"), Is.True);
            Assert.That(finance.FormatIban("de89370400440532013000"),
                Is.EqualTo("DE89 3704 0044 0532 0130 00"));
            Assert.That(finance.FormatIban("GB82WEST12345698765432"),
                Is.EqualTo("GB82 WEST 1234 5698 7654 32"));
        });
    }

    [Test]
    public void SwiftRegistryElectronicExamplesValidateAndFormat()
    {
        FinanceProvider finance = Fake.Create(9959).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.IsIbanValid("FR1420041010050500013M02606"), Is.True);
            Assert.That(finance.IsIbanValid("GB29NWBK60161331926819"), Is.True);
            Assert.That(finance.IsIbanValid("NL91ABNA0417164300"), Is.True);
            Assert.That(finance.IsIbanValid("NO9386011117947"), Is.True);
            Assert.That(finance.FormatIban("FR1420041010050500013M02606"),
                Is.EqualTo("FR14 2004 1010 0505 0001 3M02 606"));
            Assert.That(finance.FormatIban("GB29NWBK60161331926819"),
                Is.EqualTo("GB29 NWBK 6016 1331 9268 19"));
            Assert.That(finance.FormatIban("NL91ABNA0417164300"),
                Is.EqualTo("NL91 ABNA 0417 1643 00"));
            Assert.That(finance.FormatIban("NO9386011117947"),
                Is.EqualTo("NO93 8601 1117 947"));
        });
    }

    [Test]
    public void RegisteredCountriesWithoutIbanFormatsAreRejected()
    {
        FinanceProvider finance = Fake.Create(9960).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(() => finance.IbanFromCountryCode("US"),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("countryCode"));
            Assert.That(() => finance.Bban("CA"),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("countryCode"));
            Assert.That(() => finance.IbanFormat("JP"),
                Throws.TypeOf<ArgumentException>()
                    .With.Property("ParamName").EqualTo("countryCode"));
            Assert.That(finance.IsIbanValid("US001234567890123456"), Is.False);
        });
    }

    [Test]
    public void ValidationRejectsChecksumLengthCountryAndPatternFailures()
    {
        FinanceProvider finance = Fake.Create(9957).Finance();
        string value = finance.IbanFromCountryCode("DE");
        char[] checksumMutation = value.ToCharArray();
        checksumMutation[3] = checksumMutation[3] == '9' ? '0' : (char)(checksumMutation[3] + 1);
        char[] patternMutation = value.ToCharArray();
        patternMutation[4] = 'A';

        Assert.Multiple(() =>
        {
            Assert.That(finance.IsIbanValid(new string(checksumMutation)), Is.False);
            Assert.That(finance.IsIbanValid(new string(patternMutation)), Is.False);
            Assert.That(finance.IsIbanValid(value.Substring(0, value.Length - 1)), Is.False);
            Assert.That(finance.IsIbanValid("ZZ00" + value.Substring(4)), Is.False);
            Assert.That(finance.IsIbanValid("DE89-370400440532013000"), Is.False);
            Assert.That(finance.IsIbanValid(string.Empty), Is.False);
            Assert.That(() => finance.FormatIban(new string(checksumMutation)),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void FormatCopiesAreIsolatedAndApisHonorArgumentsAndLimits()
    {
        const long seed = 9958;
        FinanceProvider finance = Fake.Create(seed).Finance();
        IbanFormat first = finance.IbanFormat("DE");
        IbanFormat second = finance.IbanFormat("DE");
        IbanFormat[] all = finance.IbanFormats();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 17;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 14;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();

        first.BbanPattern = "changed";
        all[0].CountryCode = "XX";

        Assert.Multiple(() =>
        {
            Assert.That(second.BbanPattern, Is.EqualTo("8!n10!n"));
            Assert.That(finance.IbanFormats()[0].CountryCode, Is.EqualTo("AD"));
            Assert.That(() => inspected.IbanFromCountryCode("ZZ"), Throws.TypeOf<ArgumentException>());
            Assert.That(inspected.Iban(), Is.EqualTo(untouched.Iban()));
            Assert.That(() => finance.IbanFromCountryCode(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => finance.IbanFromCountryCode("D"), Throws.TypeOf<ArgumentException>());
            Assert.That(() => finance.IsIbanValid(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => Fake.Create(countOptions).Finance().IbanFromCountryCode("DE"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(countOptions).Finance().IbanFormats(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(outputOptions).Finance().IbanFromCountryCode("NO"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(outputOptions).Finance().IbanMetadata(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    static bool MatchesPattern(string value, string pattern)
    {
        int patternIndex = 0;
        int valueIndex = 0;
        while (patternIndex < pattern.Length)
        {
            int length = 0;
            while (char.IsDigit(pattern[patternIndex]))
            {
                length = length * 10 + pattern[patternIndex] - '0';
                patternIndex++;
            }
            patternIndex++;
            char kind = pattern[patternIndex++];
            for (int offset = 0; offset < length; offset++)
            {
                char character = value[valueIndex++];
                if (kind == 'n' && !char.IsDigit(character))
                {
                    return false;
                }
                if (kind == 'a' && (character < 'A' || character > 'Z'))
                {
                    return false;
                }
                if (kind == 'c' && !char.IsDigit(character) &&
                    (character < 'A' || character > 'Z'))
                {
                    return false;
                }
            }
        }
        return valueIndex == value.Length;
    }

    static void AssertEquivalent(IbanRecord actual, IbanRecord expected)
    {
        Assert.That(actual.Electronic, Is.EqualTo(expected.Electronic));
        Assert.That(actual.Print, Is.EqualTo(expected.Print));
        Assert.That(actual.Bban, Is.EqualTo(expected.Bban));
        Assert.That(actual.CountryCode, Is.EqualTo(expected.CountryCode));
        Assert.That(actual.IbanPrefix, Is.EqualTo(expected.IbanPrefix));
        Assert.That(actual.BankCode, Is.EqualTo(expected.BankCode));
        Assert.That(actual.BranchCode, Is.EqualTo(expected.BranchCode));
        Assert.That(actual.AccountCode, Is.EqualTo(expected.AccountCode));
        Assert.That(actual.IsMod97Valid, Is.EqualTo(expected.IsMod97Valid));
        Assert.That(actual.TestMarker, Is.EqualTo(expected.TestMarker));
    }
}
