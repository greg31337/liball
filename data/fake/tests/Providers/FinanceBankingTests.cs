using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceBankingTests
{
    [Test]
    public void BicGenerationUsesLocaleCountryAndOptionalBranchShape()
    {
        FinanceProvider american = Fake.Create("en-US", 9961).Finance();
        FinanceProvider german = Fake.Create("de-DE", 9961).Finance();
        FinanceProvider repeated = Fake.Create("de-DE", 9961).Finance();
        string americanBic = american.Bic();
        string germanBic = german.Bic();
        string branchBic = Fake.Create("en-GB", 9961).Finance().BranchBic();

        Assert.Multiple(() =>
        {
            Assert.That(americanBic, Does.Match("^[A-Z]{4}US[A-Z0-9]{2}$"));
            Assert.That(germanBic, Does.Match("^[A-Z]{4}DE[A-Z0-9]{2}$"));
            Assert.That(germanBic, Is.EqualTo(repeated.Bic()));
            Assert.That(branchBic, Does.Match("^[A-Z]{4}GB[A-Z0-9]{5}$"));
            Assert.That(american.IsBicValid(americanBic), Is.True);
            Assert.That(german.IsBicValid(germanBic), Is.True);
            Assert.That(american.IsBicValid(branchBic), Is.True);
            Assert.That(american.BicFromCountryCode("ca"), Does.Match("^[A-Z]{4}CA[A-Z0-9]{2}$"));
            Assert.That(american.BranchBicFromCountryCode("AU"), Does.Match("^[A-Z]{4}AU[A-Z0-9]{5}$"));
        });
    }

    [Test]
    public void BicValidationChecksStructureWithoutMakingDirectoryClaims()
    {
        FinanceProvider finance = Fake.Create(9962).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(finance.IsBicValid("AB1DUS12"), Is.True);
            Assert.That(finance.IsBicValid("ABCDUS12XYZ"), Is.True);
            Assert.That(finance.IsBicValid("abcdUS12"), Is.False);
            Assert.That(finance.IsBicValid("ABCDZZ12"), Is.False);
            Assert.That(finance.IsBicValid("ABCDUS1-"), Is.False);
            Assert.That(finance.IsBicValid("ABCDUS1"), Is.False);
            Assert.That(() => finance.IsBicValid(null), Throws.TypeOf<ArgumentNullException>());
        });
    }

    [Test]
    public void AbaRoutingGenerationIsDeterministicAndChecksumValid()
    {
        const long seed = 9963;
        FinanceProvider first = Fake.Create(seed).Finance();
        FinanceProvider second = Fake.Create(seed).Finance();
        string routing = first.RoutingNumber();
        string alias = Fake.Create(seed).Finance().AbaRoutingNumber();
        char[] mutation = routing.ToCharArray();
        mutation[8] = mutation[8] == '9' ? '0' : (char)(mutation[8] + 1);

        Assert.Multiple(() =>
        {
            Assert.That(routing, Is.EqualTo(second.RoutingNumber()));
            Assert.That(alias, Is.EqualTo(routing));
            Assert.That(routing, Does.Match("^[0-9]{9}$"));
            Assert.That(first.IsRoutingNumberValid(routing), Is.True);
            Assert.That(first.IsAbaRoutingNumberValid(routing), Is.True);
            Assert.That(first.IsRoutingNumberValid(new string(mutation)), Is.False);
            Assert.That(first.IsRoutingNumberValid(routing.Substring(1)), Is.False);
            Assert.That(first.IsRoutingNumberValid("12345678A"), Is.False);
            Assert.That(() => first.IsRoutingNumberValid(null), Throws.TypeOf<ArgumentNullException>());
        });
    }

    [Test]
    public void AbaPrefixSelectionHonorsSafetyModeWhileKeepingChecksumValidity()
    {
        FakeOptions reservedOptions = FakeOptions.Create(9964);
        FakeOptions plausibleOptions = FakeOptions.Create(9964);
        plausibleOptions.SafetyMode = SafetyMode.SyntheticPlausible;
        FakeOptions unrestrictedOptions = FakeOptions.Create(9964);
        unrestrictedOptions.SafetyMode = SafetyMode.UnrestrictedShape;
        FinanceProvider reserved = Fake.Create(reservedOptions).Finance();
        FinanceProvider plausible = Fake.Create(plausibleOptions).Finance();
        FinanceProvider unrestricted = Fake.Create(unrestrictedOptions).Finance();
        string reservedValue = reserved.RoutingNumber();
        string plausibleValue = plausible.RoutingNumber();
        string unrestrictedValue = unrestricted.RoutingNumber();

        Assert.Multiple(() =>
        {
            Assert.That(int.Parse(reservedValue.Substring(0, 2)), Is.InRange(13, 20));
            Assert.That(int.Parse(plausibleValue.Substring(0, 2)), Is.InRange(1, 12));
            Assert.That(reserved.IsRoutingNumberValid(reservedValue), Is.True);
            Assert.That(plausible.IsRoutingNumberValid(plausibleValue), Is.True);
            Assert.That(unrestricted.IsRoutingNumberValid(unrestrictedValue), Is.True);
        });
    }

    [Test]
    public void DomesticAccountMasksFollowLocaleCountryFormats()
    {
        FinanceProvider american = Fake.Create("en-US", 9965).Finance();
        FinanceProvider british = Fake.Create("en-GB", 9965).Finance();
        FinanceProvider german = Fake.Create("de-DE", 9965).Finance();
        FinanceProvider french = Fake.Create("fr-FR", 9965).Finance();
        FinanceProvider australian = Fake.Create("en-AU", 9965).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(american.DomesticAccountMask(), Is.EqualTo("############"));
            Assert.That(american.DomesticAccountNumber(), Does.Match("^[0-9]{12}$"));
            Assert.That(british.DomesticAccountMask(), Is.EqualTo("########"));
            Assert.That(british.DomesticAccountNumber(), Does.Match("^[0-9]{8}$"));
            Assert.That(german.DomesticAccountMask(), Is.EqualTo("##########"));
            Assert.That(german.DomesticAccountNumber(), Does.Match("^[0-9]{10}$"));
            Assert.That(french.DomesticAccountMask(), Is.EqualTo("#####***********##"));
            Assert.That(french.DomesticAccountNumber(), Does.Match("^[0-9]{5}[A-Z0-9]{11}[0-9]{2}$"));
            Assert.That(australian.DomesticAccountMask(), Is.EqualTo("#########"));
            Assert.That(australian.DomesticAccountNumber(), Does.Match("^[0-9]{9}$"));
        });
    }

    [Test]
    public void AccountTypesAndNamesUseLocalizedPresentation()
    {
        FinanceProvider english = Fake.Create("en-US", 9966).Finance();
        FinanceProvider german = Fake.Create("de-DE", 9966).Finance();
        FinanceProvider french = Fake.Create("fr-FR", 9966).Finance();
        BankAccountType first = Fake.Create(9966).Finance().AccountType();
        BankAccountType second = Fake.Create(9966).Finance().AccountType();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(english.AccountTypeNameFromType(BankAccountType.Checking), Is.EqualTo("Checking"));
            Assert.That(german.AccountTypeNameFromType(BankAccountType.Checking), Is.EqualTo("Girokonto"));
            Assert.That(german.AccountTypeNameFromType(BankAccountType.Savings), Is.EqualTo("Sparkonto"));
            Assert.That(french.AccountTypeNameFromType(BankAccountType.Business), Is.EqualTo("Compte professionnel"));
            Assert.That(english.AccountName(), Does.Match("^(Example|Sample|Reference|Test) "));
            Assert.That(german.AccountName(), Does.Match("^(Example|Sample|Reference|Test) "));
            Assert.That(() => english.AccountTypeNameFromType((BankAccountType)999),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property("ParamName").EqualTo("type"));
        });
    }

    [Test]
    public void BankingRecordsAreCorrelatedAndExplicitlySynthetic()
    {
        FinanceProvider american = Fake.Create("en-US", 9967).Finance();
        BankAccountRecord account = american.BankAccount();
        BankRecord bank = Fake.Create("en-US", 9968).Finance().Bank();
        BankBranchRecord branch = Fake.Create("en-US", 9969).Finance().BankBranch();
        BankBranchRecord germanBranch = Fake.Create("de-DE", 9970).Finance().BankBranch();

        Assert.Multiple(() =>
        {
            Assert.That(MatchesMask(account.Number, account.Mask), Is.True);
            Assert.That(account.MaskedNumber, Has.Length.EqualTo(account.Number.Length));
            Assert.That(Alphanumeric(account.MaskedNumber).Substring(
                Alphanumeric(account.MaskedNumber).Length - 4),
                Is.EqualTo(Alphanumeric(account.Number).Substring(
                    Alphanumeric(account.Number).Length - 4)));
            Assert.That(account.TypeName,
                Is.EqualTo(american.AccountTypeNameFromType(account.Type)));
            Assert.That(account.CountryCode, Is.EqualTo("US"));
            Assert.That(account.Locale, Is.EqualTo("en-US"));
            Assert.That(account.IsSynthetic, Is.True);
            Assert.That(account.TestMarker, Is.EqualTo("fake.synthetic-banking.v1"));
            Assert.That(account.RepresentsActiveAccount, Is.False);
            Assert.That(bank.Name, Does.Contain("Bank").Or.Contain("Credit Union"));
            Assert.That(bank.Bic.Substring(4, 2), Is.EqualTo(bank.CountryCode));
            Assert.That(bank.RepresentsRegisteredInstitution, Is.False);
            Assert.That(branch.Bic8, Is.EqualTo(branch.Bic.Substring(0, 8)));
            Assert.That(branch.BranchCode, Is.EqualTo(branch.Bic.Substring(8, 3)));
            Assert.That(branch.CountryCode, Is.EqualTo("US"));
            Assert.That(branch.IsAbaChecksumValid, Is.True);
            Assert.That(american.IsRoutingNumberValid(branch.DomesticRoutingNumber), Is.True);
            Assert.That(branch.RepresentsRegisteredInstitution, Is.False);
            Assert.That(germanBranch.CountryCode, Is.EqualTo("DE"));
            Assert.That(germanBranch.DomesticRoutingMask, Is.EqualTo("########"));
            Assert.That(germanBranch.DomesticRoutingNumber, Does.Match("^[0-9]{8}$"));
            Assert.That(germanBranch.IsAbaChecksumValid, Is.False);
        });
    }

    [Test]
    public void BankingApisRejectInvalidArgumentsAndHonorLimitsBeforeEntropy()
    {
        const long seed = 9971;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 7;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 7;

        Assert.That(() => inspected.BicFromCountryCode(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => inspected.BicFromCountryCode("U"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => inspected.BicFromCountryCode("ZZ"), Throws.TypeOf<ArgumentException>());
        Assert.That(inspected.Bic(), Is.EqualTo(untouched.Bic()));
        Assert.That(() => Fake.Create(countOptions).Finance().Bic(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(countOptions).Finance().RoutingNumber(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Finance().Bic(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Finance().DomesticAccountNumber(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Fake.Create(outputOptions).Finance().Bank(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static bool MatchesMask(string value, string mask)
    {
        if (value.Length != mask.Length)
        {
            return false;
        }
        for (int index = 0; index < mask.Length; index++)
        {
            char actual = value[index];
            char expected = mask[index];
            if (expected == '#' && !char.IsDigit(actual))
            {
                return false;
            }
            if (expected == '?' && (actual < 'A' || actual > 'Z'))
            {
                return false;
            }
            if (expected == '*' && !char.IsDigit(actual) &&
                (actual < 'A' || actual > 'Z'))
            {
                return false;
            }
            if (expected != '#' && expected != '?' && expected != '*' && actual != expected)
            {
                return false;
            }
        }
        return true;
    }

    static string Alphanumeric(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray());
    }
}
