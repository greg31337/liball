using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class AddressValueData
{
    internal string Value = string.Empty;
    internal double Weight;
    internal int Safety;
}

internal class AddressTypeData
{
    internal string Name = string.Empty;
    internal string Abbreviation = string.Empty;
    internal double Weight;
    internal int Safety;
}

internal class AddressCodedData
{
    internal string Name = string.Empty;
    internal string Abbreviation = string.Empty;
    internal string Code = string.Empty;
    internal string CountryCode = string.Empty;
    internal double Weight;
    internal int Safety;
}

internal class AddressCountryData
{
    internal string Name = string.Empty;
    internal string Abbreviation = string.Empty;
    internal string Alpha2Code = string.Empty;
    internal string Alpha3Code = string.Empty;
    internal string NumericCode = string.Empty;
    internal string Language = string.Empty;
    internal string LanguageCode = string.Empty;
    internal string Currency = string.Empty;
    internal string CurrencyCode = string.Empty;
    internal string CallingCode = string.Empty;
    internal string TimeZoneId = string.Empty;
    internal AddressValueData[] PostalMasks = new AddressValueData[0];
    internal double SouthLatitude;
    internal double WestLongitude;
    internal double NorthLatitude;
    internal double EastLongitude;
    internal double Weight;
    internal int Safety;
}

internal class AddressTemplateLineData
{
    internal string Value = string.Empty;
    internal bool Optional;
}

internal class AddressTemplateData
{
    internal string Id = string.Empty;
    internal double Weight;
    internal AddressTemplateLineData[] Lines = new AddressTemplateLineData[0];
    internal string SingleLineSeparator = string.Empty;
    internal AddressTemplateLineData[] SingleLineParts = new AddressTemplateLineData[0];
}

internal class AddressFormatStyleData
{
    internal string Id = string.Empty;
    internal string StreetType = string.Empty;
    internal string UnitType = string.Empty;
    internal string Subdivision = string.Empty;
    internal string Country = string.Empty;
    internal double Weight;
}

internal class AddressLocaleData
{
    internal SafetyMode SafetyMode;
    internal AddressValueData[] BuildingNumberMasks = new AddressValueData[0];
    internal AddressValueData[] StreetNames = new AddressValueData[0];
    internal AddressTypeData[] StreetTypes = new AddressTypeData[0];
    internal AddressTypeData[] UnitTypes = new AddressTypeData[0];
    internal AddressValueData[] UnitMasks = new AddressValueData[0];
    internal AddressValueData[] PoBoxMasks = new AddressValueData[0];
    internal string PoBoxLabel = string.Empty;
    internal AddressValueData[] Districts = new AddressValueData[0];
    internal AddressValueData[] Cities = new AddressValueData[0];
    internal AddressValueData[] Counties = new AddressValueData[0];
    internal AddressCodedData[] Subdivisions = new AddressCodedData[0];
    internal AddressValueData[] PostalMasks = new AddressValueData[0];
    internal AddressCountryData[] Countries = new AddressCountryData[0];
    internal AddressTemplateData[] Templates = new AddressTemplateData[0];
    internal AddressFormatStyleData[] FormatStyles = new AddressFormatStyleData[0];

    internal AddressValueData Choose(
        FakeRandom random,
        AddressValueData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = Applicable(values[index].Safety)
                ? values[index].Weight
                : 0.0;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    internal AddressTypeData ChooseType(
        FakeRandom random,
        AddressTypeData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = Applicable(values[index].Safety)
                ? values[index].Weight
                : 0.0;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    internal AddressCodedData ChooseCoded(
        FakeRandom random,
        AddressCodedData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = Applicable(values[index].Safety)
                ? values[index].Weight
                : 0.0;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    internal AddressCountryData ChooseCountry(
        FakeRandom random,
        AddressCountryData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = Applicable(values[index].Safety)
                ? values[index].Weight
                : 0.0;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    internal AddressCodedData ChooseSubdivision(
        FakeRandom random,
        string countryCode)
    {
        int count = 0;
        int index = 0;
        while (index < Subdivisions.Length)
        {
            if (Subdivisions[index].CountryCode == countryCode &&
                Applicable(Subdivisions[index].Safety))
            {
                count++;
            }
            index++;
        }
        AddressCodedData[] values = new AddressCodedData[count];
        index = 0;
        int selectedIndex = 0;
        while (index < Subdivisions.Length)
        {
            if (Subdivisions[index].CountryCode == countryCode &&
                Applicable(Subdivisions[index].Safety))
            {
                values[selectedIndex] = Subdivisions[index];
                selectedIndex++;
            }
            index++;
        }
        return ChooseCoded(random, values);
    }

    internal bool Applicable(int declaredSafety)
    {
        if (declaredSafety < 0)
        {
            return true;
        }
        if (SafetyMode == SafetyMode.UnrestrictedShape)
        {
            return declaredSafety == (int)SafetyMode.SyntheticPlausible ||
                declaredSafety == (int)SafetyMode.UnrestrictedShape;
        }
        return declaredSafety == (int)SafetyMode;
    }
}

internal static class AddressLocaleDataParser
{
    internal static AddressLocaleData Resolve(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "address.catalog",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localeAddressData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, "root must be an object");
            JsonElement components = Required(root, "components", "root");
            RequireKind(components, JsonValueKind.Object, "components must be an object");
            AddressLocaleData result = new AddressLocaleData();
            result.SafetyMode = context.SafetyModeValue;
            result.BuildingNumberMasks = ParseMasks(
                context,
                Required(components, "buildingNumberMasks", "components"),
                "components.buildingNumberMasks");
            result.StreetNames = ParseValues(
                context,
                Required(components, "streetNames", "components"),
                "components.streetNames");
            result.StreetTypes = ParseTypes(
                context,
                Required(components, "streetTypes", "components"),
                "components.streetTypes");
            result.UnitTypes = ParseTypes(
                context,
                Required(components, "unitTypes", "components"),
                "components.unitTypes");
            result.UnitMasks = ParseMasks(
                context,
                Required(components, "unitMasks", "components"),
                "components.unitMasks");
            result.PoBoxMasks = ParseMasks(
                context,
                Required(components, "poBoxMasks", "components"),
                "components.poBoxMasks");
            result.PoBoxLabel = NormalizedString(
                context,
                components,
                "poBoxLabel",
                "components");
            result.Districts = ParseValues(
                context,
                Required(components, "districts", "components"),
                "components.districts");
            result.Cities = ParseValues(
                context,
                Required(components, "cities", "components"),
                "components.cities");
            result.Counties = ParseValues(
                context,
                Required(components, "counties", "components"),
                "components.counties");
            result.Subdivisions = ParseCoded(
                context,
                Required(components, "subdivisions", "components"),
                "components.subdivisions");
            result.PostalMasks = ParsePostalMasks(
                context,
                Required(components, "postalMasks", "components"),
                "components.postalMasks");
            result.Countries = ParseCountries(
                context,
                Required(components, "countries", "components"),
                "components.countries");
            ValidateCorrelations(result);
            result.Templates = ParseTemplates(
                context,
                Required(root, "templates", "root"));
            result.FormatStyles = ParseStyles(
                context,
                Required(root, "formatStyles", "root"));
            return result;
        }
    }

    static AddressValueData[] ParseMasks(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        AddressValueData[] result = ParseValues(context, values, path);
        int index = 0;
        while (index < result.Length)
        {
            ValidateMask(context, result[index].Value, path + "[" + index + "].value");
            index++;
        }
        return result;
    }

    static AddressValueData[] ParsePostalMasks(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        AddressValueData[] result = ParseValues(
            context,
            values,
            path,
            true);
        int index = 0;
        while (index < result.Length)
        {
            if (result[index].Value.Length > 0)
            {
                ValidateMask(
                    context,
                    result[index].Value,
                    path + "[" + index + "].value");
            }
            index++;
        }
        return result;
    }

    static AddressValueData[] ParseValues(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        return ParseValues(context, values, path, false);
    }

    static AddressValueData[] ParseValues(
        GenerationContext context,
        JsonElement values,
        string path,
        bool allowEmpty)
    {
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressValueData[] result = new AddressValueData[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressValueData entry = new AddressValueData();
            entry.Value = allowEmpty
                ? NormalizedStringAllowEmpty(
                    context,
                    value,
                    "value",
                    itemPath)
                : NormalizedString(context, value, "value", itemPath);
            entry.Weight = Weight(value, itemPath);
            entry.Safety = EntrySafety(value, itemPath);
            if (!unique.Add(entry.Value))
            {
                Invalid(itemPath + ".value duplicates '" + entry.Value + "'");
            }
            result[index] = entry;
            totalWeight += entry.Weight;
            index++;
        }
        PositiveWeight(totalWeight, path);
        return result;
    }

    static AddressTypeData[] ParseTypes(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressTypeData[] result = new AddressTypeData[count];
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> abbreviations = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressTypeData entry = new AddressTypeData();
            entry.Name = NormalizedString(context, value, "name", itemPath);
            entry.Abbreviation = NormalizedString(
                context,
                value,
                "abbreviation",
                itemPath);
            entry.Weight = Weight(value, itemPath);
            entry.Safety = EntrySafety(value, itemPath);
            AddUnique(names, entry.Name, itemPath + ".name");
            AddUnique(abbreviations, entry.Abbreviation, itemPath + ".abbreviation");
            result[index] = entry;
            totalWeight += entry.Weight;
            index++;
        }
        PositiveWeight(totalWeight, path);
        return result;
    }

    static AddressCodedData[] ParseCoded(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressCodedData[] result = new AddressCodedData[count];
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> abbreviations = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> codes = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressCodedData entry = new AddressCodedData();
            entry.Name = NormalizedString(context, value, "name", itemPath);
            entry.Abbreviation = NormalizedString(
                context,
                value,
                "abbreviation",
                itemPath);
            entry.Code = RequiredString(value, "code", itemPath);
            entry.CountryCode = RequiredString(
                value,
                "countryCode",
                itemPath);
            ValidateCode(entry.Code, itemPath + ".code");
            ValidateUpperCode(
                entry.CountryCode,
                2,
                itemPath + ".countryCode");
            entry.Weight = Weight(value, itemPath);
            entry.Safety = EntrySafety(value, itemPath);
            AddUnique(names, entry.Name, itemPath + ".name");
            AddUnique(abbreviations, entry.Abbreviation, itemPath + ".abbreviation");
            AddUnique(codes, entry.Code, itemPath + ".code");
            result[index] = entry;
            totalWeight += entry.Weight;
            index++;
        }
        PositiveWeight(totalWeight, path);
        return result;
    }

    static AddressCountryData[] ParseCountries(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressCountryData[] result = new AddressCountryData[count];
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> alpha2Codes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> alpha3Codes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> numericCodes = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressCountryData entry = new AddressCountryData();
            entry.Name = NormalizedString(context, value, "name", itemPath);
            entry.Abbreviation = NormalizedString(
                context,
                value,
                "abbreviation",
                itemPath);
            entry.Alpha2Code = RequiredString(value, "alpha2Code", itemPath);
            entry.Alpha3Code = RequiredString(value, "alpha3Code", itemPath);
            entry.NumericCode = RequiredString(value, "numericCode", itemPath);
            entry.Language = NormalizedString(
                context,
                value,
                "language",
                itemPath);
            entry.LanguageCode = RequiredString(
                value,
                "languageCode",
                itemPath);
            entry.Currency = NormalizedString(
                context,
                value,
                "currency",
                itemPath);
            entry.CurrencyCode = RequiredString(
                value,
                "currencyCode",
                itemPath);
            entry.CallingCode = RequiredString(
                value,
                "callingCode",
                itemPath);
            entry.TimeZoneId = RequiredString(
                value,
                "timeZoneId",
                itemPath);
            entry.PostalMasks = ParsePostalMasks(
                context,
                Required(value, "postalMasks", itemPath),
                itemPath + ".postalMasks");
            JsonElement bounds = Required(value, "bounds", itemPath);
            RequireKind(
                bounds,
                JsonValueKind.Object,
                itemPath + ".bounds must be an object");
            entry.SouthLatitude = RequiredNumber(
                bounds,
                "southLatitude",
                itemPath + ".bounds");
            entry.WestLongitude = RequiredNumber(
                bounds,
                "westLongitude",
                itemPath + ".bounds");
            entry.NorthLatitude = RequiredNumber(
                bounds,
                "northLatitude",
                itemPath + ".bounds");
            entry.EastLongitude = RequiredNumber(
                bounds,
                "eastLongitude",
                itemPath + ".bounds");
            ValidateUpperCode(entry.Alpha2Code, 2, itemPath + ".alpha2Code");
            ValidateUpperCode(entry.Alpha3Code, 3, itemPath + ".alpha3Code");
            ValidateDigits(entry.NumericCode, 3, itemPath + ".numericCode");
            ValidateLanguageCode(entry.LanguageCode, itemPath + ".languageCode");
            ValidateUpperCode(entry.CurrencyCode, 3, itemPath + ".currencyCode");
            ValidateCallingCode(entry.CallingCode, itemPath + ".callingCode");
            ValidateTimeZone(entry.TimeZoneId, itemPath + ".timeZoneId");
            ValidateCoordinateBounds(entry, itemPath + ".bounds");
            entry.Weight = Weight(value, itemPath);
            entry.Safety = EntrySafety(value, itemPath);
            AddUnique(names, entry.Name, itemPath + ".name");
            AddUnique(alpha2Codes, entry.Alpha2Code, itemPath + ".alpha2Code");
            AddUnique(alpha3Codes, entry.Alpha3Code, itemPath + ".alpha3Code");
            AddUnique(numericCodes, entry.NumericCode, itemPath + ".numericCode");
            result[index] = entry;
            totalWeight += entry.Weight;
            index++;
        }
        PositiveWeight(totalWeight, path);
        return result;
    }

    static void ValidateCorrelations(AddressLocaleData data)
    {
        int subdivisionIndex = 0;
        while (subdivisionIndex < data.Subdivisions.Length)
        {
            AddressCodedData subdivision = data.Subdivisions[subdivisionIndex];
            AddressCountryData? country = FindCountry(
                data.Countries,
                subdivision.CountryCode);
            if (country == null)
            {
                Invalid(
                    "components.subdivisions[" + subdivisionIndex +
                    "].countryCode does not reference a declared country");
            }
            string prefix = subdivision.CountryCode + "-";
            if (!subdivision.Code.StartsWith(prefix, StringComparison.Ordinal))
            {
                Invalid(
                    "components.subdivisions[" + subdivisionIndex +
                    "].code must start with its countryCode and a hyphen");
            }
            subdivisionIndex++;
        }

        int countryIndex = 0;
        while (countryIndex < data.Countries.Length)
        {
            AddressCountryData country = data.Countries[countryIndex];
            bool subdivision = false;
            subdivisionIndex = 0;
            while (subdivisionIndex < data.Subdivisions.Length)
            {
                if (data.Subdivisions[subdivisionIndex].CountryCode == country.Alpha2Code &&
                    SafetyCompatible(
                        data.Subdivisions[subdivisionIndex].Safety,
                        country.Safety))
                {
                    subdivision = true;
                    break;
                }
                subdivisionIndex++;
            }
            if (!subdivision)
            {
                Invalid(
                    "components.countries[" + countryIndex +
                    "] has no declared subdivision");
            }
            int maskIndex = 0;
            while (maskIndex < country.PostalMasks.Length)
            {
                if (!ContainsMask(
                    data.PostalMasks,
                    country.PostalMasks[maskIndex].Value,
                    country.Safety))
                {
                    Invalid(
                        "components.countries[" + countryIndex +
                        "].postalMasks[" + maskIndex +
                        "].value is not declared in components.postalMasks");
                }
                maskIndex++;
            }
            countryIndex++;
        }
    }

    static AddressCountryData? FindCountry(
        AddressCountryData[] countries,
        string countryCode)
    {
        int index = 0;
        while (index < countries.Length)
        {
            if (countries[index].Alpha2Code == countryCode)
            {
                return countries[index];
            }
            index++;
        }
        return null;
    }

    static bool ContainsMask(
        AddressValueData[] masks,
        string value,
        int safety)
    {
        int index = 0;
        while (index < masks.Length)
        {
            if (masks[index].Value == value &&
                SafetyCompatible(masks[index].Safety, safety))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool SafetyCompatible(int first, int second)
    {
        return first < 0 || second < 0 || first == second;
    }

    static void ValidateCoordinateBounds(
        AddressCountryData country,
        string path)
    {
        ValidateCoordinate(
            country.SouthLatitude,
            -90.0,
            90.0,
            path + ".southLatitude");
        ValidateCoordinate(
            country.NorthLatitude,
            -90.0,
            90.0,
            path + ".northLatitude");
        ValidateCoordinate(
            country.WestLongitude,
            -180.0,
            180.0,
            path + ".westLongitude");
        ValidateCoordinate(
            country.EastLongitude,
            -180.0,
            180.0,
            path + ".eastLongitude");
        if (country.NorthLatitude < country.SouthLatitude)
        {
            Invalid(path + ".northLatitude must be greater than or equal to southLatitude");
        }
    }

    static void ValidateCoordinate(
        double value,
        double minimum,
        double maximum,
        string path)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            Invalid(
                path + " must be finite and between " + minimum +
                " and " + maximum + ", inclusive");
        }
    }

    static AddressTemplateData[] ParseTemplates(
        GenerationContext context,
        JsonElement values)
    {
        string path = "templates";
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressTemplateData[] result = new AddressTemplateData[count];
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressTemplateData entry = new AddressTemplateData();
            entry.Id = Identifier(value, "id", itemPath);
            AddUnique(ids, entry.Id, itemPath + ".id");
            entry.Weight = Weight(value, itemPath);
            entry.Lines = ParseTemplateLines(
                context,
                Required(value, "lines", itemPath),
                itemPath + ".lines");
            entry.SingleLineSeparator = NormalizedString(
                context,
                value,
                "singleLineSeparator",
                itemPath);
            entry.SingleLineParts = ParseTemplateLines(
                context,
                Required(value, "singleLineParts", itemPath),
                itemPath + ".singleLineParts");
            result[index] = entry;
            totalWeight += entry.Weight;
            index++;
        }
        PositiveWeight(totalWeight, path);
        return result;
    }

    static AddressTemplateLineData[] ParseTemplateLines(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressTemplateLineData[] result = new AddressTemplateLineData[count];
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressTemplateLineData entry = new AddressTemplateLineData();
            entry.Value = NormalizedString(context, value, "value", itemPath);
            JsonElement optional = Required(value, "optional", itemPath);
            if (optional.ValueKind != JsonValueKind.True &&
                optional.ValueKind != JsonValueKind.False)
            {
                Invalid(itemPath + ".optional must be a boolean");
            }
            entry.Optional = optional.GetBoolean();
            ValidateTemplate(entry.Value, itemPath + ".value");
            result[index] = entry;
            index++;
        }
        return result;
    }

    static AddressFormatStyleData[] ParseStyles(
        GenerationContext context,
        JsonElement values)
    {
        string path = "formatStyles";
        RequireArray(context, values, path);
        int count = values.GetArrayLength();
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        AddressFormatStyleData[] result = new AddressFormatStyleData[count];
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            AddressFormatStyleData entry = new AddressFormatStyleData();
            entry.Id = Identifier(value, "id", itemPath);
            AddUnique(ids, entry.Id, itemPath + ".id");
            entry.StreetType = StyleValue(value, "streetType", itemPath);
            entry.UnitType = StyleValue(value, "unitType", itemPath);
            entry.Subdivision = StyleValue(value, "subdivision", itemPath);
            entry.Country = StyleValue(value, "country", itemPath);
            entry.Weight = Weight(value, itemPath);
            result[index] = entry;
            totalWeight += entry.Weight;
            index++;
        }
        PositiveWeight(totalWeight, path);
        return result;
    }

    static string StyleValue(JsonElement value, string name, string path)
    {
        string result = RequiredString(value, name, path);
        if (result != "name" && result != "abbreviation")
        {
            Invalid(path + "." + name + " must be 'name' or 'abbreviation'");
        }
        return result;
    }

    static void ValidateTemplate(string value, string path)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '}')
            {
                Invalid(path + " contains an unmatched closing brace");
            }
            if (value[index] != '{')
            {
                index++;
                continue;
            }
            int close = value.IndexOf('}', index + 1);
            if (close < 0)
            {
                Invalid(path + " contains an unmatched opening brace");
            }
            string component = value.Substring(index + 1, close - index - 1);
            if (!TemplateComponent(component))
            {
                Invalid(path + " references unknown component '" + component + "'");
            }
            index = close + 1;
        }
    }

    static bool TemplateComponent(string value)
    {
        return value == "buildingNumber" ||
            value == "streetName" ||
            value == "streetType" ||
            value == "unitType" ||
            value == "unit" ||
            value == "poBox" ||
            value == "district" ||
            value == "city" ||
            value == "county" ||
            value == "subdivision" ||
            value == "postalCode" ||
            value == "country";
    }

    static void ValidateMask(
        GenerationContext context,
        string mask,
        string path)
    {
        bool placeholder = false;
        int length = 0;
        int index = 0;
        while (index < mask.Length)
        {
            char value = mask[index];
            if (value == '\\')
            {
                index++;
                if (index >= mask.Length ||
                    mask[index] != '#' &&
                    mask[index] != '?' &&
                    mask[index] != '*' &&
                    mask[index] != '\\')
                {
                    Invalid(path + " contains an invalid mask escape");
                }
            }
            else if (value == '#' || value == '?' || value == '*')
            {
                placeholder = true;
            }
            length++;
            index++;
        }
        if (!placeholder)
        {
            Invalid(path + " must contain at least one mask placeholder");
        }
        Guard.OutputSize(
            length,
            context.CapturedOptions.MaximumOutputSize,
            "localeAddressData");
    }

    static void ValidateCode(string value, string path)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool upper = character >= 'A' && character <= 'Z';
            bool digit = character >= '0' && character <= '9';
            if (!upper && !digit && character != '-')
            {
                Invalid(path + " must contain uppercase ASCII letters, digits, or internal hyphens");
            }
            index++;
        }
        if (value[0] == '-' || value[value.Length - 1] == '-')
        {
            Invalid(path + " must contain uppercase ASCII letters, digits, or internal hyphens");
        }
    }

    static void ValidateUpperCode(string value, int length, string path)
    {
        if (value.Length != length)
        {
            Invalid(path + " must contain exactly " + length + " uppercase ASCII letters");
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 'A' || value[index] > 'Z')
            {
                Invalid(path + " must contain exactly " + length + " uppercase ASCII letters");
            }
            index++;
        }
    }

    static void ValidateDigits(string value, int length, string path)
    {
        if (value.Length != length)
        {
            Invalid(path + " must contain exactly " + length + " ASCII digits");
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                Invalid(path + " must contain exactly " + length + " ASCII digits");
            }
            index++;
        }
    }

    static void ValidateLanguageCode(string value, string path)
    {
        if (value.Length != 2 && value.Length != 3)
        {
            Invalid(path + " must contain two or three lowercase ASCII letters");
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < 'a' || value[index] > 'z')
            {
                Invalid(path + " must contain two or three lowercase ASCII letters");
            }
            index++;
        }
    }

    static void ValidateCallingCode(string value, string path)
    {
        if (value.Length < 2 || value.Length > 4 || value[0] != '+')
        {
            Invalid(path + " must contain '+' followed by one to three ASCII digits");
        }
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                Invalid(path + " must contain '+' followed by one to three ASCII digits");
            }
            index++;
        }
    }

    static void ValidateTimeZone(string value, string path)
    {
        TimeZoneRegistryEntry[] entries = TimeZoneRegistryStore.Current().EntriesCopy();
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].IanaId == value)
            {
                return;
            }
            index++;
        }
        Invalid(path + " is not a bundled IANA time-zone identifier");
    }

    static string Identifier(JsonElement value, string name, string path)
    {
        string result = RequiredString(value, name, path);
        int index = 0;
        while (index < result.Length)
        {
            char character = result[index];
            bool lower = character >= 'a' && character <= 'z';
            bool digit = character >= '0' && character <= '9';
            if (!lower && !digit && character != '-')
            {
                Invalid(path + "." + name + " must contain lowercase ASCII letters, digits, or internal hyphens");
            }
            index++;
        }
        if (result[0] == '-' || result[result.Length - 1] == '-')
        {
            Invalid(path + "." + name + " must contain lowercase ASCII letters, digits, or internal hyphens");
        }
        return result;
    }

    static int EntrySafety(JsonElement value, string path)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name != "safetyMode")
            {
                continue;
            }
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                Invalid(path + ".safetyMode must be a string");
            }
            string safety = JsonText.Required(property.Value, path + ".safetyMode");
            if (safety == "reserved")
            {
                return (int)SafetyMode.Reserved;
            }
            if (safety == "syntheticPlausible")
            {
                return (int)SafetyMode.SyntheticPlausible;
            }
            if (safety == "unrestrictedShape")
            {
                return (int)SafetyMode.UnrestrictedShape;
            }
            Invalid(
                path +
                ".safetyMode must be 'reserved', 'syntheticPlausible', or 'unrestrictedShape'");
        }
        return -1;
    }

    static string NormalizedString(
        GenerationContext context,
        JsonElement value,
        string name,
        string path)
    {
        string result = RequiredString(value, name, path);
        UnicodeText.Validate(result, "localeAddressData");
        return UnicodeText.Normalize(
            result,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string NormalizedStringAllowEmpty(
        GenerationContext context,
        JsonElement value,
        string name,
        string path)
    {
        JsonElement member = Required(value, name, path);
        if (member.ValueKind != JsonValueKind.String)
        {
            Invalid(path + "." + name + " must be a string");
        }
        string result = JsonText.Required(member, path + "." + name);
        if (result.Length > 0 && string.IsNullOrWhiteSpace(result))
        {
            Invalid(path + "." + name + " must be empty or contain non-whitespace text");
        }
        UnicodeText.Validate(result, "localeAddressData");
        return UnicodeText.Normalize(
            result,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string RequiredString(JsonElement value, string name, string path)
    {
        JsonElement member = Required(value, name, path);
        if (member.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(member.GetString()))
        {
            Invalid(path + "." + name + " must be a nonempty string");
        }
        return JsonText.Required(member, path + "." + name);
    }

    static double Weight(JsonElement value, string path)
    {
        JsonElement member = Required(value, "weight", path);
        if (member.ValueKind != JsonValueKind.Number)
        {
            Invalid(path + ".weight must be a number");
        }
        double weight = member.GetDouble();
        if (!double.IsFinite(weight) || weight < 0.0)
        {
            Invalid(path + ".weight must be finite and nonnegative");
        }
        return weight;
    }

    static double RequiredNumber(
        JsonElement value,
        string name,
        string path)
    {
        JsonElement member = Required(value, name, path);
        if (member.ValueKind != JsonValueKind.Number)
        {
            Invalid(path + "." + name + " must be a number");
        }
        return member.GetDouble();
    }

    static void AddUnique(HashSet<string> values, string value, string path)
    {
        if (!values.Add(value))
        {
            Invalid(path + " duplicates '" + value + "'");
        }
    }

    static void PositiveWeight(double value, string path)
    {
        if (value <= 0.0)
        {
            Invalid(path + " must contain a positive weight");
        }
    }

    static void RequireArray(
        GenerationContext context,
        JsonElement value,
        string path)
    {
        RequireKind(value, JsonValueKind.Array, path + " must be an array");
        Guard.Count(
            value.GetArrayLength(),
            context.CapturedOptions.MaximumCount,
            "localeAddressData");
    }

    static JsonElement Required(JsonElement value, string name, string path)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }
        Invalid(path + " requires member '" + name + "'");
        return default(JsonElement);
    }

    static void RequireKind(JsonElement value, JsonValueKind kind, string reason)
    {
        if (value.ValueKind != kind)
        {
            Invalid(reason);
        }
    }

    static void Invalid(string reason)
    {
        throw FakeException.Create(
            "Resolved locale field 'address.catalog' " + reason + ".");
    }
}
