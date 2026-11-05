using System.Globalization;
using countrycodes;
using currencycodes;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var countries = new CountryCodeService();
var currencies = new CurrencyCodeService();
Console.WriteLine("Country and currency lookup (bundled catalog data)");
Console.WriteLine($"{"Country",-28} {"A2",-3} {"A3",-4} {"Numeric",-8} Currency");
foreach (string code in new[] { "US", "GB", "JP", "CH", "PL" })
{
    var country = countries.GetRequiredCountryByAlpha2(code);
    var local = currencies.GetCurrentCurrenciesForTerritory(code, true);
    string currencyCodes = string.Join(", ", local.Select(currency => currency.AlphabeticCode));
    Console.WriteLine(
        $"{country.ShortName,-28} {country.Alpha2,-3} {country.Alpha3,-4} " +
        $"{country.NumericText,-8} {currencyCodes}");
}

Console.WriteLine();
Console.WriteLine("Amounts stored as integer minor units");

// Minor-unit precision comes from the currency catalog: USD, JPY, and KWD differ.
foreach (var amount in new[]
{
    (Code: "USD", Value: 19.95m),
    (Code: "JPY", Value: 1200m),
    (Code: "KWD", Value: 1.234m)
})
{
    var minor = currencies.ConvertCurrencyMajorToMinor(amount.Value, amount.Code, AmountUsage.IsoMinorUnit);
    if (!minor.Success)
    {
        throw new InvalidOperationException(minor.Reason);
    }

    var major = currencies.ConvertCurrencyMinorToMajor(minor.MinorUnitValue, amount.Code, AmountUsage.IsoMinorUnit);
    if (!major.Success || major.MajorAmount != amount.Value)
    {
        throw new InvalidOperationException("Currency amount failed to round trip.");
    }

    Console.WriteLine(
        $"{amount.Code} {amount.Value,8} -> {minor.MinorUnitValue,5} minor units -> {major.MajorAmount}");
}

// Cash rounding uses the currency increment, which may differ from its ISO precision.
decimal cash = currencies.RoundCurrencyAmount(12.43m, "CHF", AmountUsage.Cash, MidpointRounding.AwayFromZero);
Console.WriteLine($"CHF cash rounding: 12.43 -> {cash:F2}");
