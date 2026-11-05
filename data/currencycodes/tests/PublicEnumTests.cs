using System;

namespace currencycodes.Tests;

public class PublicEnumTests
{
    [Test]
    public void CurrencyStatusValuesRemainStable()
    {
        Assert.That(Enum.GetValues<CurrencyStatus>(), Is.EqualTo(new[]
        {
            CurrencyStatus.Active,
            CurrencyStatus.Historical
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<CurrencyStatus>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void CurrencyKindValuesRemainStable()
    {
        Assert.That(Enum.GetValues<CurrencyKind>(), Is.EqualTo(new[]
        {
            CurrencyKind.MonetaryCurrency,
            CurrencyKind.Fund,
            CurrencyKind.PreciousMetal,
            CurrencyKind.SpecialUnit,
            CurrencyKind.Testing,
            CurrencyKind.NoCurrency,
            CurrencyKind.Unknown
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<CurrencyKind>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void TerritoryKindValuesRemainStable()
    {
        Assert.That(Enum.GetValues<TerritoryKind>(), Is.EqualTo(new[]
        {
            TerritoryKind.Country,
            TerritoryKind.DependentTerritory,
            TerritoryKind.Supranational,
            TerritoryKind.Institution,
            TerritoryKind.Other
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<TerritoryKind>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void CurrencySetValuesRemainStable()
    {
        Assert.That(Enum.GetValues<CurrencySet>(), Is.EqualTo(new[]
        {
            CurrencySet.ActiveOnly,
            CurrencySet.HistoricalOnly,
            CurrencySet.All
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<CurrencySet>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void PartialDatePrecisionValuesRemainStable()
    {
        Assert.That(Enum.GetValues<PartialDatePrecision>(), Is.EqualTo(new[]
        {
            PartialDatePrecision.Unknown,
            PartialDatePrecision.Year,
            PartialDatePrecision.Month,
            PartialDatePrecision.Day
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<PartialDatePrecision>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void NameMatchModeValuesRemainStable()
    {
        Assert.That(Enum.GetValues<NameMatchMode>(), Is.EqualTo(new[]
        {
            NameMatchMode.ExactOrdinal,
            NameMatchMode.ExactOrdinalIgnoreCase
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<NameMatchMode>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void AmountUsageValuesRemainStable()
    {
        Assert.That(Enum.GetValues<AmountUsage>(), Is.EqualTo(new[]
        {
            AmountUsage.IsoMinorUnit,
            AmountUsage.StandardFormat,
            AmountUsage.Cash
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<AmountUsage>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void LookupErrorValuesRemainStable()
    {
        Assert.That(Enum.GetValues<LookupError>(), Is.EqualTo(new[]
        {
            LookupError.None,
            LookupError.MalformedInput,
            LookupError.Unassigned,
            LookupError.OutOfScope,
            LookupError.Ambiguous
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<LookupError>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void NormalizationErrorValuesRemainStable()
    {
        Assert.That(Enum.GetValues<NormalizationError>(), Is.EqualTo(new[]
        {
            NormalizationError.None,
            NormalizationError.NullInput,
            NormalizationError.EmptyInput,
            NormalizationError.InvalidLength,
            NormalizationError.InvalidCharacter,
            NormalizationError.OutOfRange
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<NormalizationError>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void CurrencyValidationErrorValuesRemainStable()
    {
        Assert.That(Enum.GetValues<CurrencyValidationError>(), Is.EqualTo(new[]
        {
            CurrencyValidationError.None,
            CurrencyValidationError.MalformedInput,
            CurrencyValidationError.Unassigned,
            CurrencyValidationError.InvalidStatus,
            CurrencyValidationError.InvalidDate,
            CurrencyValidationError.InvalidTerritory,
            CurrencyValidationError.Indeterminate
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<CurrencyValidationError>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void AmountValidationErrorValuesRemainStable()
    {
        Assert.That(Enum.GetValues<AmountValidationError>(), Is.EqualTo(new[]
        {
            AmountValidationError.None,
            AmountValidationError.InvalidCurrency,
            AmountValidationError.MetadataUnavailable,
            AmountValidationError.InvalidScale,
            AmountValidationError.InvalidIncrement,
            AmountValidationError.OutOfRange,
            AmountValidationError.InvalidFormat
        }));
        Assert.That(Array.ConvertAll(Enum.GetValues<AmountValidationError>(), value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }
}
