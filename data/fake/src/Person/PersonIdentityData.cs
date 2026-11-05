using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class PersonIdentityValueData
{
    internal string Value = string.Empty;
    internal double Weight;
}

internal class PersonPronounData
{
    internal string Subject = string.Empty;
    internal string Object = string.Empty;
    internal string PossessiveAdjective = string.Empty;
    internal string PossessivePronoun = string.Empty;
    internal string Reflexive = string.Empty;
    internal string Display = string.Empty;
    internal double Weight;
}

internal class PersonDemographicData
{
    internal string Id = string.Empty;
    internal PersonIdentityValueData[] Values = new PersonIdentityValueData[0];
}

internal class PersonIdentityData
{
    internal PersonIdentityValueData[] Genders = new PersonIdentityValueData[0];
    internal PersonPronounData[] Pronouns = new PersonPronounData[0];
    internal PersonIdentityValueData[] Biographies = new PersonIdentityValueData[0];
    internal PersonIdentityValueData[] ZodiacSigns = new PersonIdentityValueData[0];
    internal PersonIdentityValueData[] MaritalStatuses = new PersonIdentityValueData[0];
    internal PersonIdentityValueData[] Relationships = new PersonIdentityValueData[0];
    internal PersonIdentityValueData[] HouseholdRoles = new PersonIdentityValueData[0];
    internal PersonIdentityValueData[] HouseholdTypes = new PersonIdentityValueData[0];
    internal PersonDemographicData[] Demographics = new PersonDemographicData[0];

    internal string Choose(
        FakeRandom random,
        PersonIdentityValueData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = values[index].Weight;
            index++;
        }
        return values[random.WeightedDiscrete(weights)].Value;
    }

    internal PersonPronounData ChoosePronouns(FakeRandom random)
    {
        double[] weights = new double[Pronouns.Length];
        int index = 0;
        while (index < Pronouns.Length)
        {
            weights[index] = Pronouns[index].Weight;
            index++;
        }
        return Pronouns[random.WeightedDiscrete(weights)];
    }

    internal string ChooseDemographic(
        FakeRandom random,
        string category)
    {
        Guard.NotNullOrWhiteSpace(category, "category");
        int index = 0;
        while (index < Demographics.Length)
        {
            if (string.Equals(
                Demographics[index].Id,
                category,
                StringComparison.OrdinalIgnoreCase))
            {
                return Choose(random, Demographics[index].Values);
            }
            index++;
        }
        throw new ArgumentException(
            $"category '{category}' is not declared by the resolved person identity locale data.",
            "category");
    }

    internal string[] DemographicCategories()
    {
        string[] result = new string[Demographics.Length];
        int index = 0;
        while (index < Demographics.Length)
        {
            result[index] = Demographics[index].Id;
            index++;
        }
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }
}

internal static class PersonIdentityDataParser
{
    internal static PersonIdentityData Resolve(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "person.identity",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localePersonIdentityData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, "must be an object");
            PersonIdentityData result = new PersonIdentityData();
            result.Genders = ParseValues(
                context,
                Required(root, "genders", "root"),
                "genders");
            result.Pronouns = ParsePronouns(
                context,
                Required(root, "pronouns", "root"));
            result.Biographies = ParseValues(
                context,
                Required(root, "biographies", "root"),
                "biographies");
            result.ZodiacSigns = ParseValues(
                context,
                Required(root, "zodiacSigns", "root"),
                "zodiacSigns");
            result.MaritalStatuses = ParseValues(
                context,
                Required(root, "maritalStatuses", "root"),
                "maritalStatuses");
            result.Relationships = ParseValues(
                context,
                Required(root, "relationships", "root"),
                "relationships");
            result.HouseholdRoles = ParseValues(
                context,
                Required(root, "householdRoles", "root"),
                "householdRoles");
            result.HouseholdTypes = ParseValues(
                context,
                Required(root, "householdTypes", "root"),
                "householdTypes");
            result.Demographics = ParseDemographics(
                context,
                Required(root, "demographics", "root"));
            return result;
        }
    }

    static PersonIdentityValueData[] ParseValues(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireKind(values, JsonValueKind.Array, path + " must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonIdentityData");
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        PersonIdentityValueData[] result = new PersonIdentityValueData[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            RequireKind(value, JsonValueKind.Object, $"{path}[{index}] must be an object");
            PersonIdentityValueData entry = new PersonIdentityValueData();
            entry.Value = NormalizedString(
                context,
                value,
                "value",
                $"{path}[{index}]");
            entry.Weight = Weight(value, $"{path}[{index}]");
            if (!unique.Add(entry.Value))
            {
                Invalid($"{path}[{index}].value duplicates '{entry.Value}'");
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

    static PersonPronounData[] ParsePronouns(
        GenerationContext context,
        JsonElement values)
    {
        RequireKind(values, JsonValueKind.Array, "pronouns must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonIdentityData");
        if (count == 0)
        {
            Invalid("pronouns must not be empty");
        }
        PersonPronounData[] result = new PersonPronounData[count];
        HashSet<string> displays = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string path = $"pronouns[{index}]";
            RequireKind(value, JsonValueKind.Object, path + " must be an object");
            PersonPronounData entry = new PersonPronounData();
            entry.Subject = NormalizedString(context, value, "subject", path);
            entry.Object = NormalizedString(context, value, "object", path);
            entry.PossessiveAdjective = NormalizedString(
                context,
                value,
                "possessiveAdjective",
                path);
            entry.PossessivePronoun = NormalizedString(
                context,
                value,
                "possessivePronoun",
                path);
            entry.Reflexive = NormalizedString(context, value, "reflexive", path);
            entry.Display = NormalizedString(context, value, "display", path);
            entry.Weight = Weight(value, path);
            if (!displays.Add(entry.Display))
            {
                Invalid(path + ".display duplicates another pronoun set");
            }
            totalWeight += entry.Weight;
            result[index] = entry;
            index++;
        }
        if (totalWeight <= 0.0)
        {
            Invalid("pronouns must contain a positive weight");
        }
        return result;
    }

    static PersonDemographicData[] ParseDemographics(
        GenerationContext context,
        JsonElement values)
    {
        RequireKind(values, JsonValueKind.Array, "demographics must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonIdentityData");
        if (count == 0)
        {
            Invalid("demographics must not be empty");
        }
        PersonDemographicData[] result = new PersonDemographicData[count];
        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            string path = $"demographics[{index}]";
            RequireKind(value, JsonValueKind.Object, path + " must be an object");
            PersonDemographicData category = new PersonDemographicData();
            category.Id = RequiredString(value, "id", path);
            ValidateIdentifier(category.Id, path + ".id");
            if (!identifiers.Add(category.Id))
            {
                Invalid(path + ".id duplicates '" + category.Id + "'");
            }
            category.Values = ParseValues(
                context,
                Required(value, "values", path),
                path + ".values");
            result[index] = category;
            index++;
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
        UnicodeText.Validate(result, "localePersonIdentityData");
        return UnicodeText.Normalize(
            result,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string RequiredString(
        JsonElement value,
        string name,
        string path)
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

    static JsonElement Required(
        JsonElement value,
        string name,
        string path)
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

    static void RequireKind(
        JsonElement value,
        JsonValueKind kind,
        string reason)
    {
        if (value.ValueKind != kind)
        {
            Invalid(reason);
        }
    }

    static void ValidateIdentifier(string value, string path)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= 'a' && character <= 'z';
            valid = valid || character == '-' && index > 0 && index < value.Length - 1;
            if (!valid)
            {
                Invalid(path + " must use lowercase ASCII letters and internal hyphens");
            }
            index++;
        }
    }

    static void Invalid(string reason)
    {
        throw FakeException.Create(
            "Resolved locale field 'person.identity' " + reason + ".");
    }
}
