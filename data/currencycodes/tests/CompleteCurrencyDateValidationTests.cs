using System;
using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteCurrencyDateValidationTests
{
    [Test]
    public void EveryGeneratedCurrencyBoundaryHasTheExpectedDateOutcome()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        HashSet<string> cases = new HashSet<string>(StringComparer.Ordinal);

        foreach (CurrencyRow row in GeneratedCurrencyRows.Rows)
        {
            AddCase(cases, row.AlphabeticCode, DateOnly.MinValue);
            AddCase(cases, row.AlphabeticCode, DateOnly.MaxValue);

            if (PartialDateRules.HasKnownPrecision(row.IntroductionDate))
            {
                DateOnly start = PartialDateRules.NormalizeInclusiveStartDate(row.IntroductionDate);
                AddCase(cases, row.AlphabeticCode, start);
                if (start != DateOnly.MinValue)
                {
                    AddCase(cases, row.AlphabeticCode, start.AddDays(-1));
                }
            }

            if (PartialDateRules.HasKnownPrecision(row.WithdrawalDate))
            {
                DateOnly end = PartialDateRules.NormalizeInclusiveEndDate(row.WithdrawalDate);
                AddCase(cases, row.AlphabeticCode, end);
                if (end != DateOnly.MaxValue)
                {
                    AddCase(cases, row.AlphabeticCode, end.AddDays(1));
                }
            }
        }

        foreach (string item in cases)
        {
            int separator = item.IndexOf('|');
            string code = item.Substring(0, separator);
            int dayNumber = int.Parse(item.Substring(separator + 1));
            DateOnly date = DateOnly.FromDayNumber(dayNumber);
            CurrencyValidationError expected = CalculateExpectedError(code, date);
            CurrencyValidationResult actual = CurrencyValidationRules.ValidateCurrencyOnDate(
                store,
                code,
                date);

            Assert.Multiple(() =>
            {
                Assert.That(actual.Error, Is.EqualTo(expected), code + " on " + date);
                Assert.That(
                    actual.IsValid,
                    Is.EqualTo(expected == CurrencyValidationError.None),
                    code + " on " + date);
                Assert.That(
                    actual.IsIndeterminate,
                    Is.EqualTo(expected == CurrencyValidationError.Indeterminate),
                    code + " on " + date);
            });
        }
    }

    [Test]
    public void YearPrecisionUsesTheWholeCalendarYearInclusively()
    {
        CurrencyInfo currency = CreateCurrency(
            CurrencyStatus.Historical,
            CreateYearDate(1999),
            CreateYearDate(2001));

        Assert.Multiple(() =>
        {
            AssertDateError(currency, new DateOnly(1998, 12, 31), CurrencyValidationError.InvalidDate);
            AssertDateError(currency, new DateOnly(1999, 1, 1), CurrencyValidationError.None);
            AssertDateError(currency, new DateOnly(2001, 12, 31), CurrencyValidationError.None);
            AssertDateError(currency, new DateOnly(2002, 1, 1), CurrencyValidationError.InvalidDate);
        });
    }

    [Test]
    public void LeapYearMonthWithdrawalIncludesFebruaryTwentyNinth()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult leapDay = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "AON",
            new DateOnly(2000, 2, 29));
        CurrencyValidationResult followingDay = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "AON",
            new DateOnly(2000, 3, 1));

        Assert.Multiple(() =>
        {
            Assert.That(leapDay.Error, Is.EqualTo(CurrencyValidationError.Indeterminate));
            Assert.That(leapDay.IsIndeterminate, Is.True);
            Assert.That(followingDay.Error, Is.EqualTo(CurrencyValidationError.InvalidDate));
            Assert.That(followingDay.IsIndeterminate, Is.False);
        });
    }

    [Test]
    public void MissingIntervalEndpointsProduceIndeterminateOutcomesInsideKnownBounds()
    {
        CurrencyInfo activeWithoutIntroduction = CreateCurrency(
            CurrencyStatus.Active,
            new PartialDate(),
            new PartialDate());
        CurrencyInfo historicalWithoutWithdrawal = CreateCurrency(
            CurrencyStatus.Historical,
            CreateYearDate(1999),
            new PartialDate());
        CurrencyInfo historicalWithoutIntroduction = CreateCurrency(
            CurrencyStatus.Historical,
            new PartialDate(),
            CreateYearDate(2001));

        Assert.Multiple(() =>
        {
            AssertDateError(
                activeWithoutIntroduction,
                new DateOnly(2026, 1, 1),
                CurrencyValidationError.Indeterminate);
            AssertDateError(
                historicalWithoutWithdrawal,
                new DateOnly(1998, 12, 31),
                CurrencyValidationError.InvalidDate);
            AssertDateError(
                historicalWithoutWithdrawal,
                new DateOnly(1999, 1, 1),
                CurrencyValidationError.Indeterminate);
            AssertDateError(
                historicalWithoutIntroduction,
                new DateOnly(2001, 12, 31),
                CurrencyValidationError.Indeterminate);
            AssertDateError(
                historicalWithoutIntroduction,
                new DateOnly(2002, 1, 1),
                CurrencyValidationError.InvalidDate);
        });
    }

    [Test]
    public void ActiveDesignationSupersedesHistoricalUncertaintyAtItsInclusiveStart()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyValidationResult before = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "EUR",
            new DateOnly(1998, 12, 31));
        CurrencyValidationResult start = CurrencyValidationRules.ValidateCurrencyOnDate(
            store,
            "EUR",
            new DateOnly(1999, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(before.Error, Is.EqualTo(CurrencyValidationError.Indeterminate));
            Assert.That(before.Currency?.Status, Is.EqualTo(CurrencyStatus.Historical));
            Assert.That(start.Error, Is.EqualTo(CurrencyValidationError.None));
            Assert.That(start.IsValid, Is.True);
            Assert.That(start.Currency?.Status, Is.EqualTo(CurrencyStatus.Active));
        });
    }

    private static void AddCase(HashSet<string> cases, string code, DateOnly date)
    {
        cases.Add(code + "|" + date.DayNumber);
    }

    private static CurrencyValidationError CalculateExpectedError(string code, DateOnly date)
    {
        bool hasIndeterminateDesignation = false;

        foreach (CurrencyRow row in GeneratedCurrencyRows.Rows)
        {
            if (string.CompareOrdinal(row.AlphabeticCode, code) != 0)
            {
                continue;
            }

            bool invalid = IsExpectedInvalid(row, date);
            if (invalid)
            {
                continue;
            }

            bool hasIntroduction = PartialDateRules.HasKnownPrecision(row.IntroductionDate);
            bool hasWithdrawal = PartialDateRules.HasKnownPrecision(row.WithdrawalDate);
            if (hasIntroduction && (row.Status == CurrencyStatus.Active || hasWithdrawal))
            {
                return CurrencyValidationError.None;
            }

            hasIndeterminateDesignation = true;
        }

        if (hasIndeterminateDesignation)
        {
            return CurrencyValidationError.Indeterminate;
        }

        return CurrencyValidationError.InvalidDate;
    }

    private static bool IsExpectedInvalid(CurrencyRow row, DateOnly date)
    {
        if (PartialDateRules.HasKnownPrecision(row.IntroductionDate))
        {
            DateOnly start = PartialDateRules.NormalizeInclusiveStartDate(row.IntroductionDate);
            if (date < start)
            {
                return true;
            }
        }

        if (PartialDateRules.HasKnownPrecision(row.WithdrawalDate))
        {
            DateOnly end = PartialDateRules.NormalizeInclusiveEndDate(row.WithdrawalDate);
            if (date > end)
            {
                return true;
            }
        }

        return false;
    }

    private static CurrencyInfo CreateCurrency(
        CurrencyStatus status,
        PartialDate introduction,
        PartialDate withdrawal)
    {
        return new CurrencyInfo
        {
            AlphabeticCode = "TST",
            Status = status,
            IntroductionDate = introduction,
            WithdrawalDate = withdrawal
        };
    }

    private static PartialDate CreateYearDate(int year)
    {
        return new PartialDate
        {
            IsKnown = true,
            Year = year,
            Precision = PartialDatePrecision.Year
        };
    }

    private static void AssertDateError(
        CurrencyInfo currency,
        DateOnly date,
        CurrencyValidationError expected)
    {
        Assert.That(
            CurrencyValidationRules.GetCurrencyDateValidationError(currency, date),
            Is.EqualTo(expected),
            date.ToString());
    }
}
