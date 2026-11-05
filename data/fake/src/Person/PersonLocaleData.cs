using System;
using System.Collections.Generic;
using System.Text.Json;

namespace fake;

internal class PersonSexCategoryData
{
    internal string Id = string.Empty;
    internal double Weight;
}

internal class PersonNameEntryData
{
    internal string Value = string.Empty;
    internal double Weight;
    internal string[] SexCategories = new string[0];

    internal bool AppliesTo(string sexCategory)
    {
        if (SexCategories.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < SexCategories.Length)
        {
            if (SexCategories[index] == sexCategory)
            {
                return true;
            }
            index++;
        }
        return false;
    }
}

internal class PersonNameFormatData
{
    internal string Template = string.Empty;
    internal double Weight;
    internal string[] Components = new string[0];
}

internal class PersonLocaleData
{
    internal PersonSexCategoryData[] SexCategories = new PersonSexCategoryData[0];
    internal PersonNameEntryData[] GivenNames = new PersonNameEntryData[0];
    internal PersonNameEntryData[] MiddleNames = new PersonNameEntryData[0];
    internal PersonNameEntryData[] FamilyNames = new PersonNameEntryData[0];
    internal PersonNameEntryData[] MaidenNames = new PersonNameEntryData[0];
    internal PersonNameEntryData[] Patronymics = new PersonNameEntryData[0];
    internal PersonNameEntryData[] Matronymics = new PersonNameEntryData[0];
    internal PersonNameEntryData[] Prefixes = new PersonNameEntryData[0];
    internal PersonNameEntryData[] Suffixes = new PersonNameEntryData[0];
    internal PersonNameFormatData[] FullFormats = new PersonNameFormatData[0];
    internal PersonNameFormatData[] FormalFormats = new PersonNameFormatData[0];
    internal PersonNameFormatData[] InformalFormats = new PersonNameFormatData[0];
    internal PersonNameEntryData[] MononymCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] ParticleCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] PatronymicCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] MultiPartCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] ApostropheCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] HyphenatedCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] DiacriticCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] RightToLeftCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] OptionalComponentCases = new PersonNameEntryData[0];
    internal PersonNameEntryData[] LongCases = new PersonNameEntryData[0];

    internal string ChooseSexCategory(FakeRandom random)
    {
        double[] weights = new double[SexCategories.Length];
        int index = 0;
        while (index < SexCategories.Length)
        {
            weights[index] = SexCategories[index].Weight;
            index++;
        }
        return SexCategories[random.WeightedDiscrete(weights)].Id;
    }

    internal string ChooseSexCategoryFromRandomAndEntries(
        FakeRandom random,
        PersonNameEntryData[] entries)
    {
        double[] weights = new double[SexCategories.Length];
        double totalWeight = 0.0;
        int categoryIndex = 0;
        while (categoryIndex < SexCategories.Length)
        {
            string category = SexCategories[categoryIndex].Id;
            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                if (entries[entryIndex].AppliesTo(category) &&
                    entries[entryIndex].Weight > 0.0)
                {
                    weights[categoryIndex] = SexCategories[categoryIndex].Weight;
                }
                entryIndex++;
            }
            totalWeight += weights[categoryIndex];
            categoryIndex++;
        }
        if (totalWeight <= 0.0)
        {
            throw FakeException.Create("Resolved person locale data has no selectable name entry.");
        }
        return SexCategories[random.WeightedDiscrete(weights)].Id;
    }

    internal void ValidateSexCategory(string sexCategory)
    {
        Guard.NotNullOrWhiteSpace(sexCategory, "sexCategory");
        int categoryIndex = 0;
        while (categoryIndex < SexCategories.Length)
        {
            if (SexCategories[categoryIndex].Id == sexCategory)
            {
                return;
            }
            categoryIndex++;
        }
        throw new ArgumentException(
            $"sexCategory '{sexCategory}' is not declared by the resolved person locale data.",
            "sexCategory");
    }

    internal PersonNameEntryData ChooseEntry(
        FakeRandom random,
        PersonNameEntryData[] entries,
        string sexCategory)
    {
        ValidateSexCategory(sexCategory);
        List<PersonNameEntryData> applicable = new List<PersonNameEntryData>();
        List<double> weights = new List<double>();
        double totalWeight = 0.0;
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].AppliesTo(sexCategory))
            {
                applicable.Add(entries[index]);
                weights.Add(entries[index].Weight);
                totalWeight += entries[index].Weight;
            }
            index++;
        }
        if (applicable.Count == 0 || totalWeight <= 0.0)
        {
            throw FakeException.Create($"Resolved person locale data has no name entry for sex category '{sexCategory}'.");
        }
        return applicable[random.WeightedDiscrete(weights.ToArray())];
    }

    internal PersonNameFormatData ChooseFormat(
        FakeRandom random,
        PersonNameFormatData[] formats)
    {
        double[] weights = new double[formats.Length];
        int index = 0;
        while (index < formats.Length)
        {
            weights[index] = formats[index].Weight;
            index++;
        }
        return formats[random.WeightedDiscrete(weights)];
    }
}

internal static class PersonLocaleDataParser
{
    internal static PersonLocaleData Resolve(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "person.names",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localePersonData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            RequireKind(root, JsonValueKind.Object, "must be an object");
            JsonElement components = Required(root, "components", "root");
            JsonElement formats = Required(root, "formats", "root");
            RequireKind(components, JsonValueKind.Object, "components must be an object");
            RequireKind(formats, JsonValueKind.Object, "formats must be an object");
            PersonLocaleData result = new PersonLocaleData();
            result.SexCategories = ParseSexCategories(
                context,
                Required(root, "sexCategories", "root"));
            result.GivenNames = ParseEntries(
                context,
                Required(components, "given", "components"),
                "components.given",
                result.SexCategories,
                true);
            result.MiddleNames = ParseEntries(
                context,
                Required(components, "middle", "components"),
                "components.middle",
                result.SexCategories,
                false);
            result.FamilyNames = ParseEntries(
                context,
                Required(components, "family", "components"),
                "components.family",
                result.SexCategories,
                false);
            result.MaidenNames = ParseOptionalEntries(
                context,
                components,
                "maiden",
                result.SexCategories,
                result.FamilyNames);
            result.Patronymics = ParseOptionalEntries(
                context,
                components,
                "patronymic",
                result.SexCategories,
                result.MiddleNames);
            result.Matronymics = ParseOptionalEntries(
                context,
                components,
                "matronymic",
                result.SexCategories,
                result.MiddleNames);
            result.Prefixes = ParseEntries(
                context,
                Required(components, "prefix", "components"),
                "components.prefix",
                result.SexCategories,
                false);
            result.Suffixes = ParseEntries(
                context,
                Required(components, "suffix", "components"),
                "components.suffix",
                result.SexCategories,
                false);
            result.FullFormats = ParseFormats(
                context,
                Required(formats, "full", "formats"),
                "formats.full");
            result.FormalFormats = ParseFormats(
                context,
                Required(formats, "formal", "formats"),
                "formats.formal");
            result.InformalFormats = ParseFormats(
                context,
                Required(formats, "informal", "formats"),
                "formats.informal");
            ParseCases(context, root, result);
            ValidateFormatComponents(result);
            ValidateCategoryCoverage(result);
            return result;
        }
    }

    static void ParseCases(
        GenerationContext context,
        JsonElement root,
        PersonLocaleData data)
    {
        data.MononymCases = new PersonNameEntryData[0];
        data.ParticleCases = new PersonNameEntryData[0];
        data.PatronymicCases = new PersonNameEntryData[0];
        data.MultiPartCases = new PersonNameEntryData[0];
        data.ApostropheCases = new PersonNameEntryData[0];
        data.HyphenatedCases = new PersonNameEntryData[0];
        data.DiacriticCases = new PersonNameEntryData[0];
        data.RightToLeftCases = new PersonNameEntryData[0];
        data.OptionalComponentCases = new PersonNameEntryData[0];
        data.LongCases = new PersonNameEntryData[0];
        JsonElement cases = Optional(root, "cases");
        if (cases.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }
        RequireKind(cases, JsonValueKind.Object, "cases must be an object");
        data.MononymCases = ParseCase(context, cases, "mononym", data.SexCategories);
        data.ParticleCases = ParseCase(context, cases, "particle", data.SexCategories);
        data.PatronymicCases = ParseCase(context, cases, "patronymic", data.SexCategories);
        data.MultiPartCases = ParseCase(context, cases, "multiPart", data.SexCategories);
        data.ApostropheCases = ParseCase(context, cases, "apostrophe", data.SexCategories);
        data.HyphenatedCases = ParseCase(context, cases, "hyphenated", data.SexCategories);
        data.DiacriticCases = ParseCase(context, cases, "diacritic", data.SexCategories);
        data.RightToLeftCases = ParseCase(context, cases, "rightToLeft", data.SexCategories);
        data.OptionalComponentCases = ParseCase(context, cases, "optionalComponentOmitted", data.SexCategories);
        data.LongCases = ParseCase(context, cases, "long", data.SexCategories);
    }

    static PersonNameEntryData[] ParseCase(
        GenerationContext context,
        JsonElement cases,
        string name,
        PersonSexCategoryData[] categories)
    {
        return ParseEntries(
            context,
            Required(cases, name, "cases"),
            "cases." + name,
            categories,
            true);
    }

    static PersonSexCategoryData[] ParseSexCategories(
        GenerationContext context,
        JsonElement values)
    {
        RequireKind(values, JsonValueKind.Array, "sexCategories must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonData");
        if (count == 0)
        {
            Invalid("sexCategories must not be empty");
        }
        PersonSexCategoryData[] result = new PersonSexCategoryData[count];
        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            RequireKind(value, JsonValueKind.Object, $"sexCategories[{index}] must be an object");
            PersonSexCategoryData category = new PersonSexCategoryData();
            category.Id = RequiredString(value, "id", $"sexCategories[{index}]");
            ValidateIdentifier(category.Id, $"sexCategories[{index}].id");
            if (!identifiers.Add(category.Id))
            {
                Invalid($"sexCategories[{index}].id duplicates '{category.Id}'");
            }
            category.Weight = RequiredWeight(value, "weight", $"sexCategories[{index}]");
            if (category.Weight <= 0.0)
            {
                Invalid($"sexCategories[{index}].weight must be positive");
            }
            result[index] = category;
            index++;
        }
        return result;
    }

    static PersonNameEntryData[] ParseEntries(
        GenerationContext context,
        JsonElement values,
        string path,
        PersonSexCategoryData[] categories,
        bool required)
    {
        RequireKind(values, JsonValueKind.Array, $"{path} must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonData");
        if (required && count == 0)
        {
            Invalid($"{path} must not be empty");
        }
        PersonNameEntryData[] result = new PersonNameEntryData[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            RequireKind(value, JsonValueKind.Object, $"{path}[{index}] must be an object");
            PersonNameEntryData entry = new PersonNameEntryData();
            entry.Value = RequiredString(value, "value", $"{path}[{index}]");
            UnicodeText.Validate(entry.Value, "localePersonData");
            entry.Value = UnicodeText.Normalize(
                entry.Value,
                context.NormalizationModeValue,
                context.CapturedOptions.MaximumOutputSize);
            entry.Weight = RequiredWeight(value, "weight", $"{path}[{index}]");
            entry.SexCategories = ParseCategoryReferences(
                value,
                $"{path}[{index}]",
                categories,
                context.CapturedOptions.MaximumCount);
            string key = entry.Value + "\0" + string.Join("\0", entry.SexCategories);
            if (!unique.Add(key))
            {
                Invalid($"{path}[{index}] duplicates a value and sex-category combination");
            }
            totalWeight += entry.Weight;
            result[index] = entry;
            index++;
        }
        if (required && totalWeight <= 0.0)
        {
            Invalid($"{path} must contain a positive weight");
        }
        return result;
    }

    static PersonNameEntryData[] ParseOptionalEntries(
        GenerationContext context,
        JsonElement components,
        string name,
        PersonSexCategoryData[] categories,
        PersonNameEntryData[] fallback)
    {
        JsonElement values = Optional(components, name);
        if (values.ValueKind == JsonValueKind.Undefined)
        {
            return fallback;
        }
        return ParseEntries(
            context,
            values,
            "components." + name,
            categories,
            false);
    }

    static string[] ParseCategoryReferences(
        JsonElement value,
        string path,
        PersonSexCategoryData[] categories,
        int maximumCount)
    {
        JsonElement references = Required(value, "sexCategories", path);
        RequireKind(references, JsonValueKind.Array, $"{path}.sexCategories must be an array");
        int count = references.GetArrayLength();
        Guard.Count(count, maximumCount, "localePersonData");
        string[] result = new string[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < count)
        {
            JsonElement reference = references[index];
            if (reference.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(reference.GetString()))
            {
                Invalid($"{path}.sexCategories[{index}] must be a nonempty string");
            }
            string identifier = JsonText.Required(
                reference,
                path + ".sexCategories[" + index + "]");
            if (!ContainsCategory(categories, identifier))
            {
                Invalid($"{path}.sexCategories[{index}] references unknown category '{identifier}'");
            }
            if (!unique.Add(identifier))
            {
                Invalid($"{path}.sexCategories[{index}] duplicates category '{identifier}'");
            }
            result[index] = identifier;
            index++;
        }
        return result;
    }

    static PersonNameFormatData[] ParseFormats(
        GenerationContext context,
        JsonElement values,
        string path)
    {
        RequireKind(values, JsonValueKind.Array, $"{path} must be an array");
        int count = values.GetArrayLength();
        Guard.Count(count, context.CapturedOptions.MaximumCount, "localePersonData");
        if (count == 0)
        {
            Invalid($"{path} must not be empty");
        }
        PersonNameFormatData[] result = new PersonNameFormatData[count];
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        double totalWeight = 0.0;
        int index = 0;
        while (index < count)
        {
            JsonElement value = values[index];
            RequireKind(value, JsonValueKind.Object, $"{path}[{index}] must be an object");
            PersonNameFormatData format = new PersonNameFormatData();
            format.Template = RequiredString(value, "template", $"{path}[{index}]");
            UnicodeText.Validate(format.Template, "localePersonData");
            format.Template = UnicodeText.Normalize(
                format.Template,
                context.NormalizationModeValue,
                context.CapturedOptions.MaximumOutputSize);
            format.Weight = RequiredWeight(value, "weight", $"{path}[{index}]");
            format.Components = TemplateComponents(format.Template, $"{path}[{index}].template");
            if (!unique.Add(format.Template))
            {
                Invalid($"{path}[{index}].template duplicates another format");
            }
            totalWeight += format.Weight;
            result[index] = format;
            index++;
        }
        if (totalWeight <= 0.0)
        {
            Invalid($"{path} must contain a positive weight");
        }
        return result;
    }

    static string[] TemplateComponents(string template, string path)
    {
        List<string> components = new List<string>();
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < template.Length)
        {
            if (template[index] == '}')
            {
                Invalid($"{path} contains an unmatched closing brace");
            }
            if (template[index] != '{')
            {
                index++;
                continue;
            }
            int close = template.IndexOf('}', index + 1);
            if (close < 0)
            {
                Invalid($"{path} contains an unmatched opening brace");
            }
            string component = template.Substring(index + 1, close - index - 1);
            if (!KnownComponent(component))
            {
                Invalid($"{path} contains unknown component '{component}'");
            }
            if (!unique.Add(component))
            {
                Invalid($"{path} repeats component '{component}'");
            }
            components.Add(component);
            index = close + 1;
        }
        if (components.Count == 0)
        {
            Invalid($"{path} must contain at least one component");
        }
        return components.ToArray();
    }

    static void ValidateFormatComponents(PersonLocaleData data)
    {
        ValidateFormats(data, data.FullFormats, "formats.full");
        ValidateFormats(data, data.FormalFormats, "formats.formal");
        ValidateFormats(data, data.InformalFormats, "formats.informal");
    }

    static void ValidateFormats(
        PersonLocaleData data,
        PersonNameFormatData[] formats,
        string path)
    {
        int formatIndex = 0;
        while (formatIndex < formats.Length)
        {
            int componentIndex = 0;
            while (componentIndex < formats[formatIndex].Components.Length)
            {
                string component = formats[formatIndex].Components[componentIndex];
                if (ComponentEntries(data, component).Length == 0)
                {
                    Invalid($"{path}[{formatIndex}] uses empty component '{component}'");
                }
                componentIndex++;
            }
            formatIndex++;
        }
    }

    static void ValidateCategoryCoverage(PersonLocaleData data)
    {
        int categoryIndex = 0;
        while (categoryIndex < data.SexCategories.Length)
        {
            string category = data.SexCategories[categoryIndex].Id;
            ValidateCoverage(data.GivenNames, category, "components.given");
            ValidateCategoryFormats(data, data.FullFormats, category);
            ValidateCategoryFormats(data, data.FormalFormats, category);
            ValidateCategoryFormats(data, data.InformalFormats, category);
            categoryIndex++;
        }
    }

    static void ValidateCategoryFormats(
        PersonLocaleData data,
        PersonNameFormatData[] formats,
        string category)
    {
        int formatIndex = 0;
        while (formatIndex < formats.Length)
        {
            int componentIndex = 0;
            while (componentIndex < formats[formatIndex].Components.Length)
            {
                string component = formats[formatIndex].Components[componentIndex];
                ValidateCoverage(ComponentEntries(data, component), category, component);
                componentIndex++;
            }
            formatIndex++;
        }
    }

    static void ValidateCoverage(
        PersonNameEntryData[] entries,
        string category,
        string path)
    {
        double weight = 0.0;
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].AppliesTo(category))
            {
                weight += entries[index].Weight;
            }
            index++;
        }
        if (weight <= 0.0)
        {
            Invalid($"{path} has no positive weight for sex category '{category}'");
        }
    }

    static PersonNameEntryData[] ComponentEntries(
        PersonLocaleData data,
        string component)
    {
        if (component == "given")
        {
            return data.GivenNames;
        }
        if (component == "middle")
        {
            return data.MiddleNames;
        }
        if (component == "family")
        {
            return data.FamilyNames;
        }
        if (component == "maiden")
        {
            return data.MaidenNames;
        }
        if (component == "patronymic")
        {
            return data.Patronymics;
        }
        if (component == "matronymic")
        {
            return data.Matronymics;
        }
        if (component == "prefix")
        {
            return data.Prefixes;
        }
        if (component == "suffix")
        {
            return data.Suffixes;
        }
        Invalid($"contains unsupported component '{component}'");
        return new PersonNameEntryData[0];
    }

    static bool ContainsCategory(
        PersonSexCategoryData[] categories,
        string identifier)
    {
        int index = 0;
        while (index < categories.Length)
        {
            if (categories[index].Id == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static bool KnownComponent(string value)
    {
        return value == "given" ||
            value == "middle" ||
            value == "family" ||
            value == "maiden" ||
            value == "patronymic" ||
            value == "matronymic" ||
            value == "prefix" ||
            value == "suffix";
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
        Invalid($"{path} requires member '{name}'");
        return default(JsonElement);
    }

    static JsonElement Optional(JsonElement value, string name)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }
        return default(JsonElement);
    }

    static string RequiredString(JsonElement value, string name, string path)
    {
        JsonElement member = Required(value, name, path);
        if (member.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(member.GetString()))
        {
            Invalid($"{path}.{name} must be a nonempty string");
        }
        return JsonText.Required(member, path + "." + name);
    }

    static double RequiredWeight(JsonElement value, string name, string path)
    {
        JsonElement member = Required(value, name, path);
        if (member.ValueKind != JsonValueKind.Number)
        {
            Invalid($"{path}.{name} must be a number");
        }
        double weight = member.GetDouble();
        if (!double.IsFinite(weight) || weight < 0.0)
        {
            Invalid($"{path}.{name} must be finite and nonnegative");
        }
        return weight;
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
                Invalid($"{path} must use lowercase ASCII letters and internal hyphens");
            }
            index++;
        }
    }

    static void Invalid(string reason)
    {
        throw FakeException.Create($"Resolved locale field 'person.names' {reason}.");
    }
}
