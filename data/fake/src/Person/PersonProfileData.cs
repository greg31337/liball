using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class PersonProfileValueData
{
    internal string Value = string.Empty;
    internal double Weight;
}

internal class PersonProfileSizeData
{
    internal int Value;
    internal double Weight;
}

internal class PersonProfileLocalityData
{
    internal string City = string.Empty;
    internal string Region = string.Empty;
    internal string PostalCode = string.Empty;
    internal string TimeZoneId = string.Empty;
    internal string EmailDomain = string.Empty;
    internal double Weight;
    internal PersonProfileValueData[] Streets = new PersonProfileValueData[0];
    internal PersonProfileValueData[] PhoneMasks = new PersonProfileValueData[0];
}

internal class PersonProfileData
{
    internal PersonProfileValueData[] Employers = new PersonProfileValueData[0];
    internal PersonProfileLocalityData[] Localities = new PersonProfileLocalityData[0];
    internal PersonProfileSizeData[] HouseholdSizes = new PersonProfileSizeData[0];
    internal string SingleHouseholdLabel = string.Empty;
    internal string MultiHouseholdLabel = string.Empty;

    internal PersonProfileValueData Choose(
        FakeRandom random,
        PersonProfileValueData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = values[index].Weight;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    internal PersonProfileLocalityData ChooseLocality(FakeRandom random)
    {
        double[] weights = new double[Localities.Length];
        int index = 0;
        while (index < Localities.Length)
        {
            weights[index] = Localities[index].Weight;
            index++;
        }
        return Localities[random.WeightedDiscrete(weights)];
    }

    internal int ChooseHouseholdSize(FakeRandom random)
    {
        double[] weights = new double[HouseholdSizes.Length];
        int index = 0;
        while (index < HouseholdSizes.Length)
        {
            weights[index] = HouseholdSizes[index].Weight;
            index++;
        }
        return HouseholdSizes[random.WeightedDiscrete(weights)].Value;
    }
}

internal static class PersonProfileDataParser
{
    internal static PersonProfileData Resolve(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "person.profile",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localePersonProfileData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, "root must be an object");
            PersonProfileData result = new PersonProfileData();
            result.Employers = ParseValues(
                context,
                Required(root, "employers", "root"),
                "employers");
            result.Localities = ParseLocalities(
                context,
                Required(root, "localities", "root"));
            result.HouseholdSizes = ParseSizes(
                context,
                Required(root, "householdSizes", "root"));
            JsonElement labels = Required(root, "householdLabels", "root");
            RequireKind(labels, JsonValueKind.Object, "householdLabels must be an object");
            result.SingleHouseholdLabel = NormalizedString(
                context,
                labels,
                "single",
                "householdLabels");
            result.MultiHouseholdLabel = NormalizedString(
                context,
                labels,
                "multiple",
                "householdLabels");
            return result;
        }
    }

    static PersonProfileLocalityData[] ParseLocalities(
        GenerationContext context,
        JsonElement values)
    {
        RequireKind(values, JsonValueKind.Array, "localities must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonProfileData");
        if (count == 0)
        {
            Invalid("localities must not be empty");
        }
        PersonProfileLocalityData[] result = new PersonProfileLocalityData[count];
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string path = "localities[" + index + "]";
            RequireKind(value, JsonValueKind.Object, path + " must be an object");
            PersonProfileLocalityData entry = new PersonProfileLocalityData();
            entry.City = NormalizedString(context, value, "city", path);
            entry.Region = NormalizedString(context, value, "region", path);
            entry.PostalCode = NormalizedString(context, value, "postalCode", path);
            entry.TimeZoneId = RequiredString(value, "timeZoneId", path);
            ValidateTimeZone(entry.TimeZoneId, path + ".timeZoneId");
            entry.EmailDomain = RequiredString(value, "emailDomain", path);
            ValidateEmailDomain(entry.EmailDomain, path + ".emailDomain");
            entry.Weight = Weight(value, path);
            entry.Streets = ParseValues(
                context,
                Required(value, "streets", path),
                path + ".streets");
            entry.PhoneMasks = ParseValues(
                context,
                Required(value, "phoneMasks", path),
                path + ".phoneMasks");
            ValidatePhoneMasks(context, entry.PhoneMasks, path + ".phoneMasks");
            totalWeight += entry.Weight;
            result[index] = entry;
            index++;
        }
        if (totalWeight <= 0.0)
        {
            Invalid("localities must contain a positive weight");
        }
        return result;
    }

    static PersonProfileValueData[] ParseValues(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireKind(values, JsonValueKind.Array, path + " must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonProfileData");
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        PersonProfileValueData[] result = new PersonProfileValueData[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string itemPath = path + "[" + index + "]";
            RequireKind(value, JsonValueKind.Object, itemPath + " must be an object");
            PersonProfileValueData entry = new PersonProfileValueData();
            entry.Value = NormalizedString(context, value, "value", itemPath);
            entry.Weight = Weight(value, itemPath);
            if (!unique.Add(entry.Value))
            {
                Invalid(itemPath + ".value duplicates '" + entry.Value + "'");
            }
            totalWeight += entry.Weight;
            result[index] = entry;
            index++;
        }
        if (totalWeight <= 0.0)
        {
            Invalid(path + " must contain a positive weight");
        }
        return result;
    }

    static PersonProfileSizeData[] ParseSizes(
        GenerationContext context,
        JsonElement values)
    {
        RequireKind(values, JsonValueKind.Array, "householdSizes must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonProfileData");
        if (count == 0)
        {
            Invalid("householdSizes must not be empty");
        }
        PersonProfileSizeData[] result = new PersonProfileSizeData[count];
        HashSet<int> unique = new HashSet<int>();
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string path = "householdSizes[" + index + "]";
            RequireKind(value, JsonValueKind.Object, path + " must be an object");
            JsonElement size = Required(value, "value", path);
            if (size.ValueKind != JsonValueKind.Number)
            {
                Invalid(path + ".value must be an integer");
            }
            double parsedNumber = size.GetDouble();
            if (!double.IsFinite(parsedNumber) ||
                parsedNumber != Math.Truncate(parsedNumber) ||
                parsedNumber < 1.0 ||
                parsedNumber > context.CapturedOptions.MaximumCount)
            {
                Invalid(path + ".value must be a positive integer within the configured count limit");
            }
            int parsed = (int)parsedNumber;
            Guard.Count(parsed, context.CapturedOptions.MaximumCount, "localePersonProfileData");
            if (!unique.Add(parsed))
            {
                Invalid(path + ".value duplicates " + parsed);
            }
            PersonProfileSizeData entry = new PersonProfileSizeData();
            entry.Value = parsed;
            entry.Weight = Weight(value, path);
            totalWeight += entry.Weight;
            result[index] = entry;
            index++;
        }
        if (totalWeight <= 0.0)
        {
            Invalid("householdSizes must contain a positive weight");
        }
        return result;
    }

    static string NormalizedString(
        GenerationContext context,
        JsonElement value,
        string name,
        string path)
    {
        string result = RequiredString(value, name, path);
        UnicodeText.Validate(result, "localePersonProfileData");
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

    static void ValidateEmailDomain(string value, string path)
    {
        if (value != "example.com" && value != "example.net" && value != "example.org")
        {
            Invalid(path + " must be an RFC 2606 example domain");
        }
    }

    static void ValidatePhoneMasks(
        GenerationContext context,
        PersonProfileValueData[] masks,
        string path)
    {
        int index = 0;
        while (index < masks.Length)
        {
            if (!masks[index].Value.Contains("555-01", StringComparison.Ordinal))
            {
                Invalid(path + "[" + index + "] must use the NANPA fictional 555-01xx range");
            }
            MaskFormatter.OutputLength(
                masks[index].Value,
                context.CapturedOptions.MaximumOutputSize);
            index++;
        }
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
            "Resolved locale field 'person.profile' " + reason + ".");
    }
}
