using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteCurrencyValidationTests
{
    private static readonly CurrencySet[] CurrencySets = new[]
    {
        CurrencySet.ActiveOnly,
        CurrencySet.HistoricalOnly,
        CurrencySet.All
    };

    [Test]
    public void EveryGeneratedCodePassesIndependentSyntaxValidation()
    {
        ICurrencyValidator validator = new CurrencyCodeService();
        HashSet<string> alphabeticCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> numericCodes = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[index];
            if (alphabeticCodes.Add(row.AlphabeticCode))
            {
                CurrencyValidationResult alphabetic =
                    validator.ValidateCurrencyAlphabeticSyntax(
                        " \t" + row.AlphabeticCode.ToLowerInvariant() + "\u00A0 ");
                AssertValidSyntax(alphabetic, row.AlphabeticCode);
            }

            if (row.HasNumericCode && numericCodes.Add(row.NumericCode))
            {
                CurrencyValidationResult numeric = validator.ValidateCurrencyNumericSyntax(
                    " \t" + row.NumericCode + "\u00A0 ");
                AssertValidSyntax(numeric, row.NumericCode);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(alphabeticCodes, Has.Count.EqualTo(326));
            Assert.That(numericCodes, Has.Count.GreaterThan(0));
            AssertValidSyntax(validator.ValidateCurrencyAlphabeticSyntax("zzz"), "ZZZ");
            AssertValidSyntax(validator.ValidateCurrencyNumericSyntax("000"), "000");
        });
    }

    [Test]
    public void SyntaxValidationCoversEveryMalformedInputCategory()
    {
        ICurrencyValidator validator = new CurrencyCodeService();
        string?[] alphabeticInputs = new string?[]
        {
            null, string.Empty, " \t\u2003 ", "US", "USDD", "U1D", "U D", "U-D", "UŚD", "ＵＳＤ"
        };
        string?[] numericInputs = new string?[]
        {
            null, string.Empty, " \t\u2003 ", "8", "08", "0008", "+08", "0 8", "0-8", "٠٠٨", "００８"
        };

        for (int index = 0; index < alphabeticInputs.Length; index++)
        {
            AssertMalformed(
                validator.ValidateCurrencyAlphabeticSyntax(alphabeticInputs[index]),
                "The currency alphabetic code is malformed.");
        }

        for (int index = 0; index < numericInputs.Length; index++)
        {
            AssertMalformed(
                validator.ValidateCurrencyNumericSyntax(numericInputs[index]),
                "The currency numeric code is malformed.");
        }
    }

    [Test]
    public void AssignmentValidationCoversEveryCodeInEveryScope()
    {
        ICurrencyValidator validator = new CurrencyCodeService();
        HashSet<string> testedCodes = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedCurrencyRows.Rows.Length; index++)
        {
            string code = GeneratedCurrencyRows.Rows[index].AlphabeticCode;
            if (!testedCodes.Add(code))
            {
                continue;
            }

            for (int setIndex = 0; setIndex < CurrencySets.Length; setIndex++)
            {
                CurrencySet currencySet = CurrencySets[setIndex];
                CurrencyRow[] expected = FindRows(code, currencySet);
                CurrencyValidationResult result = validator.ValidateCurrencyAssignment(
                    " \t" + code.ToLowerInvariant() + "\u00A0 ",
                    currencySet);

                if (expected.Length > 0)
                {
                    AssertValidAssignment(result, expected[0]);
                }
                else
                {
                    CurrencyRow[] allRows = FindRows(code, CurrencySet.All);
                    AssertInvalidStatus(result, allRows[0]);
                }
            }
        }

        Assert.That(testedCodes, Has.Count.EqualTo(326));
    }

    [Test]
    public void AssignmentValidationReportsMalformedUnassignedAndInvalidScopeOutcomes()
    {
        ICurrencyValidator validator = new CurrencyCodeService();
        string?[] malformedInputs = new string?[]
        {
            null, string.Empty, " \u2003 ", "US", "USDD", "U1D", "U D", "U-D", "UŚD"
        };

        for (int index = 0; index < malformedInputs.Length; index++)
        {
            AssertMalformed(
                validator.ValidateCurrencyAssignment(malformedInputs[index], CurrencySet.All),
                "The currency alphabetic code is malformed.");
        }

        CurrencyValidationResult unassigned = validator.ValidateCurrencyAssignment(
            "zzz",
            CurrencySet.All);
        Assert.Multiple(() =>
        {
            Assert.That(unassigned.IsValid, Is.False);
            Assert.That(unassigned.IsIndeterminate, Is.False);
            Assert.That(unassigned.Error, Is.EqualTo(CurrencyValidationError.Unassigned));
            Assert.That(unassigned.NormalizedText, Is.EqualTo("ZZZ"));
            Assert.That(
                unassigned.Reason,
                Is.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(unassigned.Currency, Is.Null);
            Assert.That(
                () => validator.ValidateCurrencyAssignment(null, (CurrencySet)99),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The currency catalog scope is invalid."));
        });
    }

    [TestCase("XAU", CurrencyStatus.Active, CurrencyKind.PreciousMetal)]
    [TestCase("XDR", CurrencyStatus.Active, CurrencyKind.SpecialUnit)]
    [TestCase("XEU", CurrencyStatus.Historical, CurrencyKind.SpecialUnit)]
    [TestCase("XTS", CurrencyStatus.Active, CurrencyKind.Testing)]
    [TestCase("XXX", CurrencyStatus.Active, CurrencyKind.NoCurrency)]
    [TestCase("BOV", CurrencyStatus.Active, CurrencyKind.Fund)]
    public void SpecialTestingAndNoCurrencyCodesRetainTheirClassification(
        string code,
        CurrencyStatus status,
        CurrencyKind kind)
    {
        ICurrencyValidator validator = new CurrencyCodeService();
        CurrencySet currencySet = status == CurrencyStatus.Active
            ? CurrencySet.ActiveOnly
            : CurrencySet.HistoricalOnly;

        CurrencyValidationResult result = validator.ValidateCurrencyAssignment(code, currencySet);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None));
            Assert.That(result.NormalizedText, Is.EqualTo(code));
            Assert.That(result.Currency, Is.Not.Null);
            Assert.That(result.Currency?.Status, Is.EqualTo(status));
            Assert.That(result.Currency?.Kind, Is.EqualTo(kind));
        });
    }

    [Test]
    public void ValidAndOutOfScopeMatchesAreDetachedAcrossPublicCalls()
    {
        ICurrencyValidator validator = new CurrencyCodeService();
        CurrencyValidationResult firstValid = validator.ValidateCurrencyAssignment(
            "USD",
            CurrencySet.ActiveOnly);
        CurrencyValidationResult firstOutOfScope = validator.ValidateCurrencyAssignment(
            "BGN",
            CurrencySet.ActiveOnly);

        Assert.That(firstValid.Currency, Is.Not.Null);
        Assert.That(firstOutOfScope.Currency, Is.Not.Null);
        firstValid.Currency!.OfficialName = "changed";
        firstValid.Currency.IntroductionDate.Year = 1;
        firstOutOfScope.Currency!.OfficialName = "changed";
        firstOutOfScope.Currency.WithdrawalDate.Year = 1;

        CurrencyValidationResult secondValid = validator.ValidateCurrencyAssignment(
            "USD",
            CurrencySet.ActiveOnly);
        CurrencyValidationResult secondOutOfScope = validator.ValidateCurrencyAssignment(
            "BGN",
            CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(secondValid, Is.Not.SameAs(firstValid));
            Assert.That(secondValid.Currency, Is.Not.SameAs(firstValid.Currency));
            Assert.That(secondValid.Currency?.IntroductionDate, Is.Not.SameAs(firstValid.Currency.IntroductionDate));
            Assert.That(secondValid.Currency?.OfficialName, Is.EqualTo("US Dollar"));
            Assert.That(secondValid.Currency?.IntroductionDate.Year, Is.EqualTo(1792));
            Assert.That(secondOutOfScope, Is.Not.SameAs(firstOutOfScope));
            Assert.That(secondOutOfScope.Currency, Is.Not.SameAs(firstOutOfScope.Currency));
            Assert.That(secondOutOfScope.Currency?.WithdrawalDate, Is.Not.SameAs(firstOutOfScope.Currency.WithdrawalDate));
            Assert.That(secondOutOfScope.Currency?.OfficialName, Is.EqualTo("Bulgarian Lev"));
            Assert.That(secondOutOfScope.Currency?.WithdrawalDate.Year, Is.EqualTo(2026));
        });
    }

    private static CurrencyRow[] FindRows(string code, CurrencySet currencySet)
    {
        return Array.FindAll(
            GeneratedCurrencyRows.Rows,
            row => row.AlphabeticCode == code
                && (currencySet == CurrencySet.All
                    || (currencySet == CurrencySet.ActiveOnly
                        && row.Status == CurrencyStatus.Active)
                    || (currencySet == CurrencySet.HistoricalOnly
                        && row.Status == CurrencyStatus.Historical)));
    }

    private static void AssertValidSyntax(
        CurrencyValidationResult result,
        string normalizedText)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, normalizedText);
            Assert.That(result.IsIndeterminate, Is.False, normalizedText);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None), normalizedText);
            Assert.That(result.NormalizedText, Is.EqualTo(normalizedText), normalizedText);
            Assert.That(result.Reason, Is.Empty, normalizedText);
            Assert.That(result.Currency, Is.Null, normalizedText);
        });
    }

    private static void AssertMalformed(CurrencyValidationResult result, string reason)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.False);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.MalformedInput));
            Assert.That(result.NormalizedText, Is.Empty);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Currency, Is.Null);
        });
    }

    private static void AssertValidAssignment(
        CurrencyValidationResult result,
        CurrencyRow expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, expected.AlphabeticCode);
            Assert.That(result.IsIndeterminate, Is.False, expected.AlphabeticCode);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.None), expected.AlphabeticCode);
            Assert.That(result.NormalizedText, Is.EqualTo(expected.AlphabeticCode), expected.AlphabeticCode);
            Assert.That(result.Reason, Is.Empty, expected.AlphabeticCode);
            Assert.That(result.Currency, Is.Not.Null, expected.AlphabeticCode);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(expected.AlphabeticCode), expected.AlphabeticCode);
            Assert.That(result.Currency?.OfficialName, Is.EqualTo(expected.OfficialName), expected.AlphabeticCode);
            Assert.That(result.Currency?.Status, Is.EqualTo(expected.Status), expected.AlphabeticCode);
            Assert.That(result.Currency?.Kind, Is.EqualTo(expected.Kind), expected.AlphabeticCode);
        });
    }

    private static void AssertInvalidStatus(
        CurrencyValidationResult result,
        CurrencyRow expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False, expected.AlphabeticCode);
            Assert.That(result.IsIndeterminate, Is.False, expected.AlphabeticCode);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.InvalidStatus), expected.AlphabeticCode);
            Assert.That(result.NormalizedText, Is.EqualTo(expected.AlphabeticCode), expected.AlphabeticCode);
            Assert.That(
                result.Reason,
                Is.EqualTo(
                    "Currency alphabetic code '"
                    + expected.AlphabeticCode
                    + "' is outside the requested scope."),
                expected.AlphabeticCode);
            Assert.That(result.Currency, Is.Not.Null, expected.AlphabeticCode);
            Assert.That(result.Currency?.AlphabeticCode, Is.EqualTo(expected.AlphabeticCode), expected.AlphabeticCode);
            Assert.That(result.Currency?.OfficialName, Is.EqualTo(expected.OfficialName), expected.AlphabeticCode);
            Assert.That(result.Currency?.Status, Is.EqualTo(expected.Status), expected.AlphabeticCode);
            Assert.That(result.Currency?.Kind, Is.EqualTo(expected.Kind), expected.AlphabeticCode);
        });
    }
}
