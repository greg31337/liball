using System.Text;

namespace fake;

internal static class AddressGenerator
{
    internal static string BuildingNumber(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return Mask(context, random, data, data.BuildingNumberMasks);
    }

    internal static string StreetName(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.Choose(random, data.StreetNames).Value;
    }

    internal static string Street(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        string name = data.Choose(random, data.StreetNames).Value;
        string type = data.ChooseType(random, data.StreetTypes).Name;
        return Join(context, name, " ", type, "street");
    }

    internal static string StreetType(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseType(random, data.StreetTypes).Name;
    }

    internal static string StreetTypeAbbreviation(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseType(random, data.StreetTypes).Abbreviation;
    }

    internal static string Unit(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        string type = data.ChooseType(random, data.UnitTypes).Name;
        string number = Mask(context, random, data, data.UnitMasks);
        return Join(context, type, " ", number, "unit");
    }

    internal static string PoBox(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        string number = Mask(context, random, data, data.PoBoxMasks);
        return Join(context, data.PoBoxLabel, " ", number, "poBox");
    }

    internal static string District(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.Choose(random, data.Districts).Value;
    }

    internal static string City(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.Choose(random, data.Cities).Value;
    }

    internal static string County(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.Choose(random, data.Counties).Value;
    }

    internal static string Subdivision(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCoded(random, data.Subdivisions).Name;
    }

    internal static string SubdivisionAbbreviation(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCoded(random, data.Subdivisions).Abbreviation;
    }

    internal static string SubdivisionCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCoded(random, data.Subdivisions).Code;
    }

    internal static string PostalCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return Mask(context, random, data, data.PostalMasks);
    }

    internal static string Country(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).Name;
    }

    internal static string CountryAbbreviation(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).Abbreviation;
    }

    internal static string CountryCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).Alpha2Code;
    }

    internal static string CountryAlpha3Code(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).Alpha3Code;
    }

    internal static string CountryNumericCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).NumericCode;
    }

    internal static string Language(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).Language;
    }

    internal static string LanguageCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).LanguageCode;
    }

    internal static string Currency(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).Currency;
    }

    internal static string CurrencyCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).CurrencyCode;
    }

    internal static string CallingCode(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).CallingCode;
    }

    internal static string TimeZoneId(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        return data.ChooseCountry(random, data.Countries).TimeZoneId;
    }

    internal static CountryDetails CountryDetails(
        GenerationContext context,
        FakeRandom random)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        AddressCountryData country = data.ChooseCountry(random, data.Countries);
        CountryDetails result = new CountryDetails();
        result.Country = country.Name;
        result.Abbreviation = country.Abbreviation;
        result.Alpha2Code = country.Alpha2Code;
        result.Alpha3Code = country.Alpha3Code;
        result.NumericCode = country.NumericCode;
        result.Language = country.Language;
        result.LanguageCode = country.LanguageCode;
        result.Currency = country.Currency;
        result.CurrencyCode = country.CurrencyCode;
        result.CallingCode = country.CallingCode;
        result.TimeZoneId = country.TimeZoneId;
        return result;
    }

    internal static string Mask(
        GenerationContext context,
        FakeRandom random,
        AddressLocaleData data,
        AddressValueData[] masks)
    {
        string mask = data.Choose(random, masks).Value;
        if (mask.Length == 0)
        {
            return "";
        }
        return MaskFormatter.Format(
            random,
            mask,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string Join(
        GenerationContext context,
        string first,
        string separator,
        string second,
        string parameterName)
    {
        StringBuilder result = new StringBuilder();
        Append(
            result,
            first,
            context.CapturedOptions.MaximumOutputSize,
            parameterName);
        Append(
            result,
            separator,
            context.CapturedOptions.MaximumOutputSize,
            parameterName);
        Append(
            result,
            second,
            context.CapturedOptions.MaximumOutputSize,
            parameterName);
        return result.ToString();
    }

    static void Append(
        StringBuilder builder,
        string value,
        int maximumOutputSize,
        string parameterName)
    {
        Guard.OutputSize(
            (long)builder.Length + (long)value.Length,
            maximumOutputSize,
            parameterName);
        builder.Append(value);
    }
}
