using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class EducationValueData
{
    internal string Value = string.Empty;
    internal double Weight;
}

internal class EducationSubjectData
{
    internal string Id = string.Empty;
    internal string Value = string.Empty;
    internal double Weight;
    internal EducationValueData[] Courses = new EducationValueData[0];
    internal EducationValueData[] Degrees = new EducationValueData[0];
}

internal class EducationLocaleData
{
    internal EducationValueData[] Institutions = new EducationValueData[0];
    internal EducationSubjectData[] Subjects = new EducationSubjectData[0];
    internal EducationValueData[] Terms = new EducationValueData[0];
    internal EducationValueData[] Grades = new EducationValueData[0];
    internal EducationValueData[] StudentIdMasks = new EducationValueData[0];

    internal EducationValueData Choose(FakeRandom random, EducationValueData[] values)
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

    internal EducationSubjectData ChooseSubject(FakeRandom random)
    {
        double[] weights = new double[Subjects.Length];
        int index = 0;
        while (index < Subjects.Length)
        {
            weights[index] = Subjects[index].Weight;
            index++;
        }
        return Subjects[random.WeightedDiscrete(weights)];
    }
}

internal static class EducationLocaleDataParser
{
    internal static EducationLocaleData Resolve(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "education.catalog",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localeEducationData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, "root must be an object");
            EducationLocaleData result = new EducationLocaleData();
            result.Institutions = ParseValues(context, RequireArray(root, "institutions", "root"), "institutions");
            result.Subjects = ParseSubjects(context, RequireArray(root, "subjects", "root"));
            result.Terms = ParseValues(context, RequireArray(root, "terms", "root"), "terms");
            result.Grades = ParseValues(context, RequireArray(root, "grades", "root"), "grades");
            result.StudentIdMasks = ParseValues(context, RequireArray(root, "studentIdMasks", "root"), "studentIdMasks");

            int index = 0;
            while (index < result.StudentIdMasks.Length)
            {
                ValidateMask(context, result.StudentIdMasks[index].Value, "studentIdMasks[" + index + "].value");
                index++;
            }
            return result;
        }
    }

    static EducationSubjectData[] ParseSubjects(GenerationContext context, JsonElement array)
    {
        int count = array.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localeEducationData");
        if (count == 0)
            Invalid("subjects must contain at least one item");

        List<EducationSubjectData> subjects = new List<EducationSubjectData>();
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        double total = 0;
        int index = 0;
        while (index < count)
        {
            JsonElement item = array[index];
            string path = "subjects[" + index + "]";
            if (item.ValueKind != JsonValueKind.Object)
                Invalid(path + " must be an object");

            EducationSubjectData subject = new EducationSubjectData();
            subject.Id = RequireIdentifier(item, "id", path);
            subject.Value = RequireString(context, item, "value", path);
            subject.Weight = ReadWeight(item, path);
            subject.Courses = ParseValues(context, RequireArray(item, "courses", path), path + ".courses");
            subject.Degrees = ParseValues(context, RequireArray(item, "degrees", path), path + ".degrees");
            if (!ids.Add(subject.Id))
                Invalid(path + ".id contains duplicate value " + subject.Id);
            if (!values.Add(subject.Value))
                Invalid(path + ".value contains duplicate value " + subject.Value);
            subjects.Add(subject);
            total += subject.Weight;
            index++;
        }

        if (total <= 0)
            Invalid("subjects must have a positive total weight");
        return subjects.ToArray();
    }

    static EducationValueData[] ParseValues(GenerationContext context, JsonElement array, string path)
    {
        int count = array.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localeEducationData");
        if (count == 0)
            Invalid(path + " must contain at least one item");

        List<EducationValueData> values = new List<EducationValueData>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        double total = 0;
        int index = 0;
        while (index < count)
        {
            JsonElement item = array[index];
            string itemPath = path + "[" + index + "]";
            if (item.ValueKind != JsonValueKind.Object)
                Invalid(itemPath + " must be an object");

            EducationValueData value = new EducationValueData();
            value.Value = RequireString(context, item, "value", itemPath);
            value.Weight = ReadWeight(item, itemPath);
            if (!seen.Add(value.Value))
                Invalid(itemPath + ".value contains duplicate value " + value.Value);
            values.Add(value);
            total += value.Weight;
            index++;
        }

        if (total <= 0)
            Invalid(path + " must have a positive total weight");
        return values.ToArray();
    }

    static JsonElement RequireArray(JsonElement parent, string name, string path)
    {
        JsonElement value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.Array)
            Invalid(path + "." + name + " must be an array");
        return value;
    }

    static string RequireString(GenerationContext context, JsonElement parent, string name, string path)
    {
        JsonElement value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.String || String.IsNullOrWhiteSpace(value.GetString()))
            Invalid(path + "." + name + " must be a nonempty string");
        string result = JsonText.Required(value, path + "." + name);
        UnicodeText.Validate(result, "localeEducationData");
        return UnicodeText.Normalize(
            result,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string RequireIdentifier(JsonElement parent, string name, string path)
    {
        JsonElement value = Required(parent, name, path);
        if (value.ValueKind != JsonValueKind.String)
            Invalid(path + "." + name + " must be a string");

        string identifier = JsonText.Required(value, path + "." + name);
        if (String.IsNullOrEmpty(identifier))
            Invalid(path + "." + name + " cannot be empty");
        int index;
        for (index = 0; index < identifier.Length; index++)
        {
            char character = identifier[index];
            bool letter = character >= 'a' && character <= 'z';
            bool digit = character >= '0' && character <= '9';
            if (!letter && !digit && character != '-')
                Invalid(path + "." + name + " must contain lowercase ASCII letters, digits, or internal hyphens");
        }
        if (identifier[0] == '-' || identifier[identifier.Length - 1] == '-')
            Invalid(path + "." + name + " must contain lowercase ASCII letters, digits, or internal hyphens");
        return identifier;
    }

    static double ReadWeight(JsonElement parent, string path)
    {
        JsonElement value = Required(parent, "weight", path);
        if (value.ValueKind != JsonValueKind.Number)
            Invalid(path + ".weight must be a number");
        double weight = value.GetDouble();
        if (Double.IsNaN(weight) || Double.IsInfinity(weight) || weight < 0)
            Invalid(path + ".weight must be finite and nonnegative");
        return weight;
    }

    static void ValidateMask(GenerationContext context, string mask, string path)
    {
        int outputLength = 0;
        bool hasPlaceholder = false;
        int index;
        for (index = 0; index < mask.Length; index++)
        {
            char character = mask[index];
            if (character == '\\')
            {
                index++;
                if (index >= mask.Length || mask[index] != '#' && mask[index] != '?' && mask[index] != '*' && mask[index] != '\\')
                    Invalid(path + " contains an invalid mask escape");
            }
            else if (character == '#' || character == '?' || character == '*')
                hasPlaceholder = true;
            outputLength++;
        }
        if (!hasPlaceholder)
            Invalid(path + " must contain at least one mask placeholder");
        Guard.OutputSize(outputLength, context.CapturedOptions.MaximumOutputSize, "localeEducationData");
    }

    static JsonElement Required(JsonElement value, string name, string path)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
                return property.Value;
        }
        Invalid(path + " requires member '" + name + "'");
        return default(JsonElement);
    }

    static void RequireKind(JsonElement value, JsonValueKind kind, string reason)
    {
        if (value.ValueKind != kind)
            Invalid(reason);
    }

    static void Invalid(string reason)
    {
        throw FakeException.Create("Resolved locale field 'education.catalog' " + reason + ".");
    }
}
