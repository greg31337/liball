namespace currencycodes.Tests;

public class ResultDataTests
{
    [Test]
    public void CurrencyLookupResultPreservesAllMatchesAndPrimaryMatch()
    {
        CurrencyInfo primary = new CurrencyInfo { AlphabeticCode = "USD" };
        CurrencyInfo historical = new CurrencyInfo
        {
            AlphabeticCode = "USD",
            Status = CurrencyStatus.Historical
        };
        CurrencyLookupResult result = new CurrencyLookupResult
        {
            Found = true,
            Error = LookupError.Ambiguous,
            NormalizedQuery = "USD",
            Match = primary,
            Matches = new[] { primary, historical }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(result.NormalizedQuery, Is.EqualTo("USD"));
            Assert.That(result.Match, Is.SameAs(primary));
            Assert.That(result.Matches, Is.EqualTo(new[] { primary, historical }));
        });
    }

    [Test]
    public void TerritoryLookupResultPreservesAllMatchesAndPrimaryMatch()
    {
        TerritoryInfo primary = new TerritoryInfo { CanonicalName = "Georgia" };
        TerritoryInfo second = new TerritoryInfo { CanonicalName = "South Georgia" };
        TerritoryLookupResult result = new TerritoryLookupResult
        {
            Found = true,
            Error = LookupError.Ambiguous,
            NormalizedQuery = "Georgia",
            Match = primary,
            Matches = new[] { primary, second }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(result.NormalizedQuery, Is.EqualTo("Georgia"));
            Assert.That(result.Match, Is.SameAs(primary));
            Assert.That(result.Matches, Is.EqualTo(new[] { primary, second }));
        });
    }

    [Test]
    public void LookupDefaultsUseIndependentEmptyMatchArrays()
    {
        CurrencyLookupResult firstCurrencyResult = new CurrencyLookupResult();
        CurrencyLookupResult secondCurrencyResult = new CurrencyLookupResult();
        TerritoryLookupResult firstTerritoryResult = new TerritoryLookupResult();
        TerritoryLookupResult secondTerritoryResult = new TerritoryLookupResult();

        Assert.Multiple(() =>
        {
            Assert.That(firstCurrencyResult.Matches, Is.Empty);
            Assert.That(firstCurrencyResult.Matches, Is.Not.SameAs(secondCurrencyResult.Matches));
            Assert.That(firstTerritoryResult.Matches, Is.Empty);
            Assert.That(firstTerritoryResult.Matches, Is.Not.SameAs(secondTerritoryResult.Matches));
        });
    }

    [Test]
    public void NormalizationResultPreservesInputOutcomeAndNormalizedText()
    {
        NormalizationResult result = new NormalizationResult
        {
            Success = true,
            Error = NormalizationError.None,
            OriginalText = " usd ",
            NormalizedText = "USD"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(NormalizationError.None));
            Assert.That(result.OriginalText, Is.EqualTo(" usd "));
            Assert.That(result.NormalizedText, Is.EqualTo("USD"));
        });
    }

    [Test]
    public void CurrencyValidationResultPreservesIndeterminateOutcome()
    {
        CurrencyInfo currency = new CurrencyInfo { AlphabeticCode = "XTS" };
        CurrencyValidationResult result = new CurrencyValidationResult
        {
            IsValid = false,
            IsIndeterminate = true,
            Error = CurrencyValidationError.Indeterminate,
            NormalizedText = "XTS",
            Reason = "The available source dates are incomplete.",
            Currency = currency
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsIndeterminate, Is.True);
            Assert.That(result.Error, Is.EqualTo(CurrencyValidationError.Indeterminate));
            Assert.That(result.NormalizedText, Is.EqualTo("XTS"));
            Assert.That(result.Reason, Is.EqualTo("The available source dates are incomplete."));
            Assert.That(result.Currency, Is.SameAs(currency));
        });
    }

    [Test]
    public void AmountValidationResultPreservesRuleAndRoundedCandidate()
    {
        CurrencyInfo currency = new CurrencyInfo { AlphabeticCode = "CHF" };
        AmountValidationResult result = new AmountValidationResult
        {
            IsValid = false,
            Error = AmountValidationError.InvalidIncrement,
            Reason = "The amount is not a cash increment.",
            Currency = currency,
            Amount = 1.03m,
            HasRequiredRule = true,
            RequiredDigits = 2,
            RequiredIncrement = 5,
            HasRoundedCandidate = true,
            RoundedCandidate = 1.05m
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.InvalidIncrement));
            Assert.That(result.Reason, Is.EqualTo("The amount is not a cash increment."));
            Assert.That(result.Currency, Is.SameAs(currency));
            Assert.That(result.Amount, Is.EqualTo(1.03m));
            Assert.That(result.HasRequiredRule, Is.True);
            Assert.That(result.RequiredDigits, Is.EqualTo(2));
            Assert.That(result.RequiredIncrement, Is.EqualTo(5));
            Assert.That(result.HasRoundedCandidate, Is.True);
            Assert.That(result.RoundedCandidate, Is.EqualTo(1.05m));
        });
    }

    [Test]
    public void MinorUnitConversionResultPreservesBothRepresentations()
    {
        CurrencyInfo currency = new CurrencyInfo { AlphabeticCode = "JPY" };
        MinorUnitConversionResult result = new MinorUnitConversionResult
        {
            Success = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = currency,
            MajorAmount = -123m,
            MinorUnitValue = -123
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.SameAs(currency));
            Assert.That(result.MajorAmount, Is.EqualTo(-123m));
            Assert.That(result.MinorUnitValue, Is.EqualTo(-123));
        });
    }

    [Test]
    public void AmountTextResultPreservesAmountAndInvariantText()
    {
        CurrencyInfo currency = new CurrencyInfo { AlphabeticCode = "USD" };
        AmountTextResult result = new AmountTextResult
        {
            Success = true,
            Error = AmountValidationError.None,
            Reason = string.Empty,
            Currency = currency,
            Amount = -12.50m,
            InvariantText = "USD -12.50"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(AmountValidationError.None));
            Assert.That(result.Reason, Is.Empty);
            Assert.That(result.Currency, Is.SameAs(currency));
            Assert.That(result.Amount, Is.EqualTo(-12.50m));
            Assert.That(result.InvariantText, Is.EqualTo("USD -12.50"));
        });
    }

    [Test]
    public void DataVersionInfoPreservesVersionsAndUsesIndependentChecksumArrays()
    {
        DataVersionInfo result = new DataVersionInfo
        {
            IsoPublicationDate = "2026-01-01",
            CldrVersion = "48.2",
            GeneratorSchemaVersion = "1",
            SnapshotIdentifier = "sha256:example",
            SourceChecksums = new[] { "specs/example.xml\t10\tsha256" }
        };
        DataVersionInfo other = new DataVersionInfo();
        DataVersionInfo another = new DataVersionInfo();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsoPublicationDate, Is.EqualTo("2026-01-01"));
            Assert.That(result.CldrVersion, Is.EqualTo("48.2"));
            Assert.That(result.GeneratorSchemaVersion, Is.EqualTo("1"));
            Assert.That(result.SnapshotIdentifier, Is.EqualTo("sha256:example"));
            Assert.That(result.SourceChecksums, Is.EqualTo(new[] { "specs/example.xml\t10\tsha256" }));
            Assert.That(other.SourceChecksums, Is.Empty);
            Assert.That(other.SourceChecksums, Is.Not.SameAs(another.SourceChecksums));
        });
    }
}
