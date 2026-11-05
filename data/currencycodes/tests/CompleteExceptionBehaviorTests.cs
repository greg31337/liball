namespace currencycodes.Tests;

public class CompleteExceptionBehaviorTests
{
    [Test]
    public void RequiredLookupExceptionsHaveEquivalentStructuredLookupResults()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        CurrencyLookupResult malformed = catalog.FindCurrencyByAlphabeticCode(
            null!,
            CurrencySet.All);
        CurrencyLookupResult unassigned = catalog.FindCurrencyByAlphabeticCode(
            "ZZZ",
            CurrencySet.All);
        CurrencyLookupResult outOfScope = catalog.FindCurrencyByAlphabeticCode(
            "BGN",
            CurrencySet.ActiveOnly);
        CurrencyLookupResult ambiguous = catalog.FindCurrencyByAlphabeticCode(
            "EUR",
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(LookupError.MalformedInput));
            Assert.That(unassigned.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(outOfScope.Error, Is.EqualTo(LookupError.OutOfScope));
            Assert.That(ambiguous.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode(null!, CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode("ZZZ", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode(
                    "BGN",
                    CurrencySet.ActiveOnly),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'BGN' is outside the requested scope."));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode("EUR", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'EUR' identifies more than one designation in the requested scope."));
        });
    }

    [Test]
    public void MetadataLookupUsesDedicatedCodeAndNotFoundExceptions()
    {
        CurrencyCodeService service = new CurrencyCodeService();

        CurrencyLookupResult malformed = service.FindCurrencyByAlphabeticCode(
            "US1",
            CurrencySet.All);
        CurrencyLookupResult unassigned = service.FindCurrencyByAlphabeticCode(
            "AAA",
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(LookupError.MalformedInput));
            Assert.That(unassigned.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(
                () => service.GetCurrencyFormatMetadata("US1"),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => service.GetCurrencyFormatMetadata("AAA"),
                Throws.TypeOf<CurrencyNotFoundException>()
                    .With.Message.EqualTo(
                        "Currency format metadata for 'AAA' was not found."));
        });
    }

    [Test]
    public void RoundingExceptionsHaveEquivalentStructuredValidationResults()
    {
        CurrencyCodeService service = new CurrencyCodeService();

        AmountValidationResult malformed = service.ValidateCurrencyAmount(
            1m,
            "US1",
            AmountUsage.StandardFormat);
        AmountValidationResult unassigned = service.ValidateCurrencyAmount(
            1m,
            "ZZZ",
            AmountUsage.StandardFormat);
        AmountValidationResult historical = service.ValidateCurrencyAmount(
            1m,
            "BGN",
            AmountUsage.StandardFormat);
        AmountValidationResult unavailable = service.ValidateCurrencyAmount(
            1m,
            "XAU",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
            Assert.That(unassigned.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
            Assert.That(historical.Error, Is.EqualTo(AmountValidationError.InvalidCurrency));
            Assert.That(unavailable.Error, Is.EqualTo(AmountValidationError.MetadataUnavailable));
            Assert.That(
                () => service.RoundCurrencyAmount(
                    1m,
                    "US1",
                    AmountUsage.StandardFormat,
                    MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => service.RoundCurrencyAmount(
                    1m,
                    "ZZZ",
                    AmountUsage.StandardFormat,
                    MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(
                () => service.RoundCurrencyAmount(
                    1m,
                    "BGN",
                    AmountUsage.StandardFormat,
                    MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'BGN' is outside the requested scope."));
            Assert.That(
                () => service.RoundCurrencyAmount(
                    1m,
                    "XAU",
                    AmountUsage.IsoMinorUnit,
                    MidpointRounding.ToEven),
                Throws.TypeOf<InvalidCurrencyAmountException>()
                    .With.Message.EqualTo(
                        "The selected amount metadata is unavailable for currency 'XAU'."));
        });
    }

    [Test]
    public void RoutineFailuresStayStructuredOnEveryExceptionlessApiFamily()
    {
        CurrencyCodeService service = new CurrencyCodeService();

        NormalizationResult normalization = service.NormalizeCurrencyAlphabeticCode(null);
        CurrencyValidationResult syntax = service.ValidateCurrencyAlphabeticSyntax("US1");
        CurrencyValidationResult assignment = service.ValidateCurrencyAssignment(
            "ZZZ",
            CurrencySet.All);
        AmountValidationResult scale = service.ValidateCurrencyAmount(
            1.001m,
            "USD",
            AmountUsage.IsoMinorUnit);
        AmountValidationResult increment = service.ValidateCurrencyAmount(
            1.03m,
            "CHF",
            AmountUsage.Cash);
        MinorUnitConversionResult invalidConversion = service.ConvertCurrencyMinorToMajor(
            1,
            "ZZZ",
            AmountUsage.StandardFormat);
        MinorUnitConversionResult overflow = service.ConvertCurrencyMajorToMinor(
            decimal.MaxValue,
            "JPY",
            AmountUsage.StandardFormat);
        AmountTextResult invalidFormat = service.ParseInvariantCurrencyAmount(
            "USD1.00",
            AmountUsage.StandardFormat);
        AmountTextResult invalidCurrency = service.FormatInvariantCurrencyAmount(
            1m,
            "ZZZ",
            AmountUsage.StandardFormat,
            MidpointRounding.ToEven);
        AmountTextResult textOverflow = service.ParseInvariantCurrencyAmount(
            "USD 79228162514264337593543950336",
            AmountUsage.StandardFormat);

        Assert.Multiple(() =>
        {
            Assert.That(normalization.Success, Is.False);
            Assert.That(normalization.Error, Is.EqualTo(NormalizationError.NullInput));
            Assert.That(syntax.IsValid, Is.False);
            Assert.That(syntax.Error, Is.EqualTo(CurrencyValidationError.MalformedInput));
            Assert.That(assignment.IsValid, Is.False);
            Assert.That(assignment.Error, Is.EqualTo(CurrencyValidationError.Unassigned));
            Assert.That(scale.IsValid, Is.False);
            Assert.That(scale.Error, Is.EqualTo(AmountValidationError.InvalidScale));
            Assert.That(increment.IsValid, Is.False);
            Assert.That(increment.Error, Is.EqualTo(AmountValidationError.InvalidIncrement));
            Assert.That(invalidConversion.Success, Is.False);
            Assert.That(
                invalidConversion.Error,
                Is.EqualTo(AmountValidationError.InvalidCurrency));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Error, Is.EqualTo(AmountValidationError.OutOfRange));
            Assert.That(invalidFormat.Success, Is.False);
            Assert.That(invalidFormat.Error, Is.EqualTo(AmountValidationError.InvalidFormat));
            Assert.That(invalidCurrency.Success, Is.False);
            Assert.That(
                invalidCurrency.Error,
                Is.EqualTo(AmountValidationError.InvalidCurrency));
            Assert.That(textOverflow.Success, Is.False);
            Assert.That(textOverflow.Error, Is.EqualTo(AmountValidationError.OutOfRange));
        });
    }

    [Test]
    public void EveryPublicSelectorPathRejectsInvalidEnumValues()
    {
        CurrencyCodeService service = new CurrencyCodeService();
        TestDelegate[] invalidCurrencySets =
        {
            () => service.GetCurrencyCatalog((CurrencySet)99),
            () => service.FindCurrencyByAlphabeticCode("USD", (CurrencySet)99),
            () => service.FindCurrenciesByNumericCode("840", (CurrencySet)99),
            () => service.FindCurrenciesByOfficialName(
                "US Dollar",
                NameMatchMode.ExactOrdinal,
                (CurrencySet)99),
            () => service.GetRequiredCurrencyByAlphabeticCode("USD", (CurrencySet)99),
            () => service.ValidateCurrencyAssignment("USD", (CurrencySet)99)
        };
        TestDelegate[] invalidNameModes =
        {
            () => service.FindCurrenciesByOfficialName(
                "US Dollar",
                (NameMatchMode)99,
                CurrencySet.All),
            () => service.FindTerritoriesByOfficialName(
                "United States of America",
                (NameMatchMode)99)
        };
        TestDelegate[] invalidAmountUsages =
        {
            () => service.ValidateCurrencyAmount(1m, "USD", (AmountUsage)99),
            () => service.RoundCurrencyAmount(
                1m,
                "USD",
                (AmountUsage)99,
                MidpointRounding.ToEven),
            () => service.ConvertCurrencyMajorToMinor(1m, "USD", (AmountUsage)99),
            () => service.ConvertCurrencyMinorToMajor(1, "USD", (AmountUsage)99),
            () => service.FormatInvariantCurrencyAmount(
                1m,
                "USD",
                (AmountUsage)99,
                MidpointRounding.ToEven),
            () => service.ParseInvariantCurrencyAmount("USD 1.00", (AmountUsage)99)
        };
        TestDelegate[] invalidMidpointModes =
        {
            () => service.RoundCurrencyAmount(
                1m,
                "USD",
                AmountUsage.StandardFormat,
                (MidpointRounding)99),
            () => service.FormatInvariantCurrencyAmount(
                1m,
                "USD",
                AmountUsage.StandardFormat,
                (MidpointRounding)99)
        };

        AssertArgumentFailures(
            invalidCurrencySets,
            "The currency catalog scope is invalid.");
        AssertArgumentFailures(invalidNameModes, "The name match mode is invalid.");
        AssertArgumentFailures(invalidAmountUsages, "The amount usage is invalid.");
        AssertArgumentFailures(
            invalidMidpointModes,
            "The midpoint rounding mode is invalid.");
    }

    private static void AssertArgumentFailures(TestDelegate[] calls, string message)
    {
        foreach (TestDelegate call in calls)
        {
            Assert.That(
                call,
                Throws.TypeOf<ArgumentException>().With.Message.EqualTo(message));
        }
    }
}
