using System.Reflection;

namespace currencycodes.Tests;

public class CurrencyCodeServiceTests
{
    [Test]
    public void PublicShapeContainsOnlyTheDeclaredInterfaceOperations()
    {
        Type serviceType = typeof(CurrencyCodeService);
        Type[] interfaces = serviceType.GetInterfaces();
        Array.Sort(interfaces, (left, right) => string.CompareOrdinal(left.Name, right.Name));
        ConstructorInfo[] constructors = serviceType.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        MethodInfo[] methods = serviceType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        string[] actualMethodNames = Array.ConvertAll(methods, method => method.Name);
        string[] expectedMethodNames = GetInterfaceMethodNames();
        Array.Sort(actualMethodNames, StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(serviceType.IsPublic, Is.True);
            Assert.That(
                Array.ConvertAll(interfaces, type => type.Name),
                Is.EqualTo(new[]
                {
                    "ICurrencyAmountRules",
                    "ICurrencyCatalog",
                    "ICurrencyNormalizer",
                    "ICurrencyValidator"
                }));
            Assert.That(constructors, Has.Length.EqualTo(1));
            Assert.That(constructors[0].GetParameters(), Is.Empty);
            Assert.That(actualMethodNames, Is.EqualTo(expectedMethodNames));
            Assert.That(
                serviceType.GetFields(BindingFlags.Public | BindingFlags.DeclaredOnly),
                Is.Empty);
            Assert.That(
                serviceType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly),
                Is.Empty);
            Assert.That(
                serviceType.GetEvents(BindingFlags.Public | BindingFlags.DeclaredOnly),
                Is.Empty);
            Assert.That(
                serviceType.GetNestedTypes(BindingFlags.Public),
                Is.Empty);
            Assert.That(
                serviceType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly),
                Is.Empty);
        });
    }

    [Test]
    public void CatalogInterfaceRoutesEveryOperationToGeneratedData()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        DataVersionInfo version = catalog.GetCurrencyDataVersion();
        CurrencyInfo[] currencies = catalog.GetCurrencyCatalog(CurrencySet.ActiveOnly);
        TerritoryInfo[] territories = catalog.GetTerritoryCatalog();
        CurrencyUsage[] usages = catalog.GetCurrencyUsageCatalog();
        CurrencyLookupResult alphabetic = catalog.FindCurrencyByAlphabeticCode(
            " usd ",
            CurrencySet.ActiveOnly);
        CurrencyLookupResult numeric = catalog.FindCurrenciesByNumericCode(
            "840",
            CurrencySet.ActiveOnly);
        CurrencyLookupResult currencyName = catalog.FindCurrenciesByOfficialName(
            "US Dollar",
            NameMatchMode.ExactOrdinal,
            CurrencySet.ActiveOnly);
        CurrencyInfo required = catalog.GetRequiredCurrencyByAlphabeticCode(
            "USD",
            CurrencySet.ActiveOnly);
        TerritoryLookupResult alpha2 = catalog.FindTerritoryByAlpha2Code("us");
        TerritoryLookupResult alpha3 = catalog.FindTerritoryByAlpha3Code("usa");
        TerritoryLookupResult territoryNumeric = catalog.FindTerritoriesByNumericCode("840");
        TerritoryLookupResult territoryName = catalog.FindTerritoriesByOfficialName(
            "United States of America",
            NameMatchMode.ExactOrdinal);
        CurrencyInfo[] currentCurrencies = catalog.GetCurrentCurrenciesForTerritory("US", true);
        CurrencyInfo[] datedCurrencies = catalog.GetCurrenciesForTerritoryOnDate(
            "US",
            new DateOnly(2020, 1, 1),
            true);
        CurrencyUsage[] territoryHistory = catalog.GetCurrencyUsageHistoryForTerritory("US");
        TerritoryInfo[] currentTerritories = catalog.GetCurrentTerritoriesForCurrency("USD", true);
        CurrencyUsage[] currencyHistory = catalog.GetCurrencyUsageHistoryForCurrency("USD");
        CurrencyFormatMetadata metadata = catalog.GetCurrencyFormatMetadata("CHF");

        Assert.Multiple(() =>
        {
            Assert.That(version.SnapshotIdentifier, Does.StartWith("sha256:"));
            Assert.That(currencies, Has.Length.EqualTo(179));
            Assert.That(territories, Is.Not.Empty);
            Assert.That(usages, Is.Not.Empty);
            Assert.That(alphabetic.Match?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(numeric.Match?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(currencyName.Match?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(required.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(alpha2.Match?.Alpha3Code, Is.EqualTo("USA"));
            Assert.That(alpha3.Match?.Alpha2Code, Is.EqualTo("US"));
            Assert.That(territoryNumeric.Match?.Alpha2Code, Is.EqualTo("US"));
            Assert.That(territoryName.Match?.Alpha2Code, Is.EqualTo("US"));
            Assert.That(
                Array.ConvertAll(currentCurrencies, currency => currency.AlphabeticCode),
                Does.Contain("USD"));
            Assert.That(
                Array.ConvertAll(datedCurrencies, currency => currency.AlphabeticCode),
                Does.Contain("USD"));
            Assert.That(
                Array.ConvertAll(territoryHistory, usage => usage.CurrencyCode),
                Does.Contain("USD"));
            Assert.That(
                Array.ConvertAll(currentTerritories, territory => territory.Alpha2Code),
                Does.Contain("US"));
            Assert.That(
                Array.ConvertAll(currencyHistory, usage => usage.TerritoryKey),
                Does.Contain("US"));
            Assert.That(metadata.CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(metadata.CashRoundingIncrement, Is.EqualTo(5));
        });
    }

    [Test]
    public void NormalizerAndValidatorInterfacesRouteEveryOperation()
    {
        CurrencyCodeService service = new CurrencyCodeService();
        ICurrencyNormalizer normalizer = service;
        ICurrencyValidator validator = service;

        NormalizationResult alphabetic = normalizer.NormalizeCurrencyAlphabeticCode(" usd ");
        NormalizationResult numeric = normalizer.NormalizeCurrencyNumericCode("7");
        NormalizationResult alpha2 = normalizer.NormalizeTerritoryAlpha2Code(" us ");
        NormalizationResult alpha3 = normalizer.NormalizeTerritoryAlpha3Code(" usa ");
        CurrencyValidationResult alphabeticSyntax = validator.ValidateCurrencyAlphabeticSyntax("usd");
        CurrencyValidationResult numericSyntax = validator.ValidateCurrencyNumericSyntax("840");
        CurrencyValidationResult assignment = validator.ValidateCurrencyAssignment(
            "USD",
            CurrencySet.ActiveOnly);
        CurrencyValidationResult date = validator.ValidateCurrencyOnDate(
            "USD",
            new DateOnly(2020, 1, 1));
        CurrencyValidationResult territory = validator.ValidateTerritoryAssignment("US");
        CurrencyValidationResult association = validator.ValidateTerritoryCurrencyAssociation(
            "US",
            "USD",
            new DateOnly(2020, 1, 1),
            true);
        AmountValidationResult amount = validator.ValidateCurrencyAmount(
            1.03m,
            "CHF",
            AmountUsage.Cash);

        Assert.Multiple(() =>
        {
            Assert.That(alphabetic.NormalizedText, Is.EqualTo("USD"));
            Assert.That(numeric.NormalizedText, Is.EqualTo("007"));
            Assert.That(alpha2.NormalizedText, Is.EqualTo("US"));
            Assert.That(alpha3.NormalizedText, Is.EqualTo("USA"));
            Assert.That(alphabeticSyntax.IsValid, Is.True);
            Assert.That(numericSyntax.IsValid, Is.True);
            Assert.That(assignment.Currency?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(date.IsValid, Is.True);
            Assert.That(territory.IsValid, Is.True);
            Assert.That(association.IsValid, Is.True);
            Assert.That(amount.IsValid, Is.False);
            Assert.That(amount.Error, Is.EqualTo(AmountValidationError.InvalidIncrement));
        });
    }

    [Test]
    public void AmountRulesInterfaceRoutesEveryOperation()
    {
        ICurrencyAmountRules rules = new CurrencyCodeService();

        decimal rounded = rules.RoundCurrencyAmount(
            1.03m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.AwayFromZero);
        MinorUnitConversionResult toMinor = rules.ConvertCurrencyMajorToMinor(
            12.34m,
            "USD",
            AmountUsage.IsoMinorUnit);
        MinorUnitConversionResult toMajor = rules.ConvertCurrencyMinorToMajor(
            1234,
            "USD",
            AmountUsage.IsoMinorUnit);
        AmountTextResult formatted = rules.FormatInvariantCurrencyAmount(
            1.025m,
            "CHF",
            AmountUsage.Cash,
            MidpointRounding.AwayFromZero);
        AmountTextResult parsed = rules.ParseInvariantCurrencyAmount(
            "USD -12.34",
            AmountUsage.IsoMinorUnit);

        Assert.Multiple(() =>
        {
            Assert.That(rounded, Is.EqualTo(1.05m));
            Assert.That(toMinor.Success, Is.True);
            Assert.That(toMinor.MinorUnitValue, Is.EqualTo(1234));
            Assert.That(toMajor.Success, Is.True);
            Assert.That(toMajor.MajorAmount, Is.EqualTo(12.34m));
            Assert.That(formatted.Success, Is.True);
            Assert.That(formatted.InvariantText, Is.EqualTo("CHF 1.05"));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Amount, Is.EqualTo(-12.34m));
        });
    }

    private static string[] GetInterfaceMethodNames()
    {
        Type[] interfaces = new[]
        {
            typeof(ICurrencyCatalog),
            typeof(ICurrencyNormalizer),
            typeof(ICurrencyValidator),
            typeof(ICurrencyAmountRules)
        };
        int count = 0;
        for (int index = 0; index < interfaces.Length; index++)
        {
            count += interfaces[index].GetMethods().Length;
        }

        string[] names = new string[count];
        int resultIndex = 0;
        for (int interfaceIndex = 0; interfaceIndex < interfaces.Length; interfaceIndex++)
        {
            MethodInfo[] methods = interfaces[interfaceIndex].GetMethods();
            for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
            {
                names[resultIndex] = methods[methodIndex].Name;
                resultIndex++;
            }
        }

        Array.Sort(names, StringComparer.Ordinal);
        return names;
    }
}
