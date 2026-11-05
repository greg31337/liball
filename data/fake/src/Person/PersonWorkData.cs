using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class PersonWorkValueData
{
    internal string Value = string.Empty;
    internal double Weight;
}

internal class PersonWorkData
{
    internal PersonWorkValueData[] Titles = new PersonWorkValueData[0];
    internal PersonWorkValueData[] Descriptors = new PersonWorkValueData[0];
    internal PersonWorkValueData[] Areas = new PersonWorkValueData[0];
    internal PersonWorkValueData[] Types = new PersonWorkValueData[0];
    internal PersonWorkValueData[] Seniorities = new PersonWorkValueData[0];
    internal PersonWorkValueData[] Departments = new PersonWorkValueData[0];
    internal PersonWorkValueData[] Statuses = new PersonWorkValueData[0];
    internal PersonWorkValueData[] SalaryBands = new PersonWorkValueData[0];

    internal string Choose(
        FakeRandom random,
        PersonWorkValueData[] values)
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
}

internal static class PersonWorkDataParser
{
    internal static PersonWorkData Resolve(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "person.work",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localePersonWorkData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, "must be an object");
            PersonWorkData result = new PersonWorkData();
            result.Titles = ParseValues(
                context,
                Required(root, "titles", "root"),
                "titles");
            result.Descriptors = ParseValues(
                context,
                Required(root, "descriptors", "root"),
                "descriptors");
            result.Areas = ParseValues(
                context,
                Required(root, "areas", "root"),
                "areas");
            result.Types = ParseValues(
                context,
                Required(root, "types", "root"),
                "types");
            result.Seniorities = ParseValues(
                context,
                Required(root, "seniorities", "root"),
                "seniorities");
            result.Departments = ParseValues(
                context,
                Required(root, "departments", "root"),
                "departments");
            result.Statuses = ParseValues(
                context,
                Required(root, "statuses", "root"),
                "statuses");
            result.SalaryBands = ParseValues(
                context,
                Required(root, "salaryBands", "root"),
                "salaryBands");
            return result;
        }
    }

    static PersonWorkValueData[] ParseValues(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireKind(values, JsonValueKind.Array, path + " must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonWorkData");
        if (count == 0)
        {
            Invalid(path + " must not be empty");
        }
        PersonWorkValueData[] result = new PersonWorkValueData[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            RequireKind(value, JsonValueKind.Object, $"{path}[{index}] must be an object");
            PersonWorkValueData entry = new PersonWorkValueData();
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

    static string NormalizedString(
        GenerationContext context,
        JsonElement value,
        string name,
        string path)
    {
        string result = RequiredString(value, name, path);
        UnicodeText.Validate(result, "localePersonWorkData");
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

    static void Invalid(string reason)
    {
        throw FakeException.Create(
            "Resolved locale field 'person.work' " + reason + ".");
    }
}
