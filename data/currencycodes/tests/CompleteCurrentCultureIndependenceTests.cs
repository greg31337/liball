using System.Globalization;
using System.Text.Json;

namespace currencycodes.Tests;

public class CompleteCurrentCultureIndependenceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        IncludeFields = true
    };

    [TestCase("en-US")]
    [TestCase("tr-TR")]
    [TestCase("ar-SA")]
    [TestCase("de-DE")]
    [TestCase("th-TH")]
    public void EveryPublicOperationMatchesItsInvariantCultureResult(string cultureName)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Dictionary<string, string> expected = CaptureEveryOperation();

            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Dictionary<string, string> actual = CaptureEveryOperation();

            Assert.Multiple(() =>
            {
                foreach (KeyValuePair<string, string> operation in expected)
                {
                    Assert.That(
                        actual[operation.Key],
                        Is.EqualTo(operation.Value),
                        cultureName + ": " + operation.Key);
                }
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static Dictionary<string, string> CaptureEveryOperation()
    {
        CurrencyCodeService service = new CurrencyCodeService();
        DateOnly date = new DateOnly(2020, 1, 1);
        Dictionary<string, string> results = new Dictionary<string, string>(
            StringComparer.Ordinal);

        Add(results, "data version", service.GetCurrencyDataVersion());
        Add(results, "currency catalog", service.GetCurrencyCatalog(CurrencySet.ActiveOnly));
        Add(results, "territory catalog", service.GetTerritoryCatalog());
        Add(results, "usage catalog", service.GetCurrencyUsageCatalog());

        Add(
            results,
            "alphabetic currency lookup",
            service.FindCurrencyByAlphabeticCode(" inr ", CurrencySet.ActiveOnly));
        Add(
            results,
            "numeric currency lookup",
            service.FindCurrenciesByNumericCode("356", CurrencySet.ActiveOnly));
        Add(
            results,
            "currency name lookup",
            service.FindCurrenciesByOfficialName(
                "indian rupee",
                NameMatchMode.ExactOrdinalIgnoreCase,
                CurrencySet.ActiveOnly));
        Add(
            results,
            "required currency lookup",
            service.GetRequiredCurrencyByAlphabeticCode(" inr ", CurrencySet.ActiveOnly));
        Add(results, "territory alpha-2 lookup", service.FindTerritoryByAlpha2Code(" in "));
        Add(results, "territory alpha-3 lookup", service.FindTerritoryByAlpha3Code(" ind "));
        Add(results, "territory numeric lookup", service.FindTerritoriesByNumericCode("356"));
        Add(
            results,
            "territory name lookup",
            service.FindTerritoriesByOfficialName(
                "india",
                NameMatchMode.ExactOrdinalIgnoreCase));
        Add(
            results,
            "current currencies for territory",
            service.GetCurrentCurrenciesForTerritory("in", true));
        Add(
            results,
            "dated currencies for territory",
            service.GetCurrenciesForTerritoryOnDate("in", date, true));
        Add(
            results,
            "territory usage history",
            service.GetCurrencyUsageHistoryForTerritory("in"));
        Add(
            results,
            "current territories for currency",
            service.GetCurrentTerritoriesForCurrency("inr", true));
        Add(
            results,
            "currency usage history",
            service.GetCurrencyUsageHistoryForCurrency("inr"));
        Add(results, "currency format metadata", service.GetCurrencyFormatMetadata("inr"));

        Add(
            results,
            "normalize currency alphabetic",
            service.NormalizeCurrencyAlphabeticCode(" inr "));
        Add(
            results,
            "normalize currency numeric",
            service.NormalizeCurrencyNumericCode("7"));
        Add(
            results,
            "normalize territory alpha-2",
            service.NormalizeTerritoryAlpha2Code(" in "));
        Add(
            results,
            "normalize territory alpha-3",
            service.NormalizeTerritoryAlpha3Code(" ind "));
        Add(
            results,
            "reject native numeric digits",
            service.NormalizeCurrencyNumericCode("\u0667"));

        Add(
            results,
            "validate alphabetic syntax",
            service.ValidateCurrencyAlphabeticSyntax(" inr "));
        Add(
            results,
            "validate numeric syntax",
            service.ValidateCurrencyNumericSyntax("356"));
        Add(
            results,
            "validate native numeric digits",
            service.ValidateCurrencyNumericSyntax("\u0663\u0665\u0666"));
        Add(
            results,
            "validate currency assignment",
            service.ValidateCurrencyAssignment("inr", CurrencySet.ActiveOnly));
        Add(
            results,
            "validate currency date",
            service.ValidateCurrencyOnDate("inr", date));
        Add(
            results,
            "validate territory assignment",
            service.ValidateTerritoryAssignment("in"));
        Add(
            results,
            "validate territory association",
            service.ValidateTerritoryCurrencyAssociation("in", "inr", date, true));
        Add(
            results,
            "validate currency amount",
            service.ValidateCurrencyAmount(
                1234.56m,
                "inr",
                AmountUsage.StandardFormat));

        Add(
            results,
            "round currency amount",
            service.RoundCurrencyAmount(
                1234.565m,
                "inr",
                AmountUsage.StandardFormat,
                MidpointRounding.ToEven));
        Add(
            results,
            "convert major to minor",
            service.ConvertCurrencyMajorToMinor(
                1234.56m,
                "inr",
                AmountUsage.StandardFormat));
        Add(
            results,
            "convert minor to major",
            service.ConvertCurrencyMinorToMajor(
                -123456,
                "inr",
                AmountUsage.StandardFormat));
        Add(
            results,
            "format invariant amount",
            service.FormatInvariantCurrencyAmount(
                -1234.565m,
                "inr",
                AmountUsage.StandardFormat,
                MidpointRounding.AwayFromZero));
        Add(
            results,
            "parse invariant amount",
            service.ParseInvariantCurrencyAmount(
                "inr -1234.50",
                AmountUsage.StandardFormat));
        Add(
            results,
            "reject comma decimal amount",
            service.ParseInvariantCurrencyAmount(
                "INR 1234,50",
                AmountUsage.StandardFormat));
        Add(
            results,
            "reject native digit amount",
            service.ParseInvariantCurrencyAmount(
                "INR \u0661\u0662\u0663\u0664.\u0665\u0660",
                AmountUsage.StandardFormat));

        return results;
    }

    private static void Add(Dictionary<string, string> results, string name, object value)
    {
        results.Add(
            name,
            JsonSerializer.Serialize(value, value.GetType(), SerializerOptions));
    }
}
