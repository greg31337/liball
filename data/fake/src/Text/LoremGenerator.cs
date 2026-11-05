using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace fake;

internal class LoremCatalog
{
    internal string[] Values = new string[0];
    internal string[] Parts = new string[0];
    internal double[] Weights;
    internal int MaximumWordLength;
    internal int MinimumWordLength;

    internal LoremCatalog(string[] values, string[] parts, double[] weights)
    {
        Values = values;
        Parts = parts;
        Weights = weights;
        MaximumWordLength = 0;
        MinimumWordLength = int.MaxValue;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > MaximumWordLength)
            {
                MaximumWordLength = values[index].Length;
            }
            if (values[index].Length < MinimumWordLength)
            {
                MinimumWordLength = values[index].Length;
            }
            index = index + 1;
        }
    }

    internal string ChooseWord(FakeRandom random)
    {
        return Values[random.WeightedDiscrete(Weights)];
    }

    internal string ChooseWordFromRandomAndPartOfSpeech(FakeRandom random, string partOfSpeech)
    {
        Guard.NotNullOrWhiteSpace(partOfSpeech, "partOfSpeech");
        List<string> values = new List<string>();
        List<double> weights = new List<double>();
        int index = 0;
        while (index < Values.Length)
        {
            if (string.Equals(Parts[index], partOfSpeech, StringComparison.OrdinalIgnoreCase))
            {
                values.Add(Values[index]);
                weights.Add(Weights[index]);
            }
            index = index + 1;
        }
        if (values.Count == 0)
        {
            throw new ArgumentException($"partOfSpeech '{partOfSpeech}' is not available in the resolved locale word data.", "partOfSpeech");
        }
        return values[random.WeightedDiscrete(weights.ToArray())];
    }

    internal string SelectPart(FakeRandom random)
    {
        return Parts[random.WeightedDiscrete(Weights)];
    }

    internal string[] AvailableParts()
    {
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < Parts.Length)
        {
            unique.Add(Parts[index]);
            index = index + 1;
        }
        string[] result = new string[unique.Count];
        unique.CopyTo(result);
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }
}

internal static class LoremGenerator
{
    internal static LoremCatalog Catalog(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "text.words",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        int dataLimit = Math.Max(
            context.CapturedOptions.MaximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        Guard.OutputSize(json.Length, dataLimit, "localeWordData");
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                Invalid("must be an object containing values and weights");
            }
            JsonElement valuesElement = Required(root, "values");
            JsonElement weightsElement = Required(root, "weights");
            if (valuesElement.ValueKind != JsonValueKind.Array || weightsElement.ValueKind != JsonValueKind.Array)
            {
                Invalid("values and weights must be arrays");
            }
            int count = valuesElement.GetArrayLength();
            Guard.Count(count, context.CapturedOptions.MaximumCount, "localeWordData");
            if (count == 0 || weightsElement.GetArrayLength() != count)
            {
                Invalid("values and weights must be nonempty arrays of equal length");
            }
            string[] values = new string[count];
            string[] parts = new string[count];
            double[] weights = new double[count];
            HashSet<string> entries = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            while (index < count)
            {
                JsonElement entry = valuesElement[index];
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    Invalid($"values[{index}] must be an object");
                }
                values[index] = RequiredString(entry, "text", index);
                parts[index] = RequiredString(entry, "partOfSpeech", index);
                ValidateWord(values[index], index);
                ValidatePart(parts[index], index);
                values[index] = UnicodeText.Normalize(
                    values[index],
                    context.NormalizationModeValue,
                    context.CapturedOptions.MaximumOutputSize);
                if (!entries.Add(parts[index] + "\0" + values[index]))
                {
                    Invalid($"values[{index}] duplicates a word and part-of-speech pair");
                }
                JsonElement weight = weightsElement[index];
                if (weight.ValueKind != JsonValueKind.Number)
                {
                    Invalid($"weights[{index}] must be a number");
                }
                weights[index] = weight.GetDouble();
                if (!double.IsFinite(weights[index]) || weights[index] < 0.0)
                {
                    Invalid($"weights[{index}] must be finite and nonnegative");
                }
                index = index + 1;
            }
            ValidatePositiveParts(parts, weights);
            return new LoremCatalog(values, parts, weights);
        }
    }

    internal static string Words(
        FakeRandom random,
        LoremCatalog catalog,
        int count,
        string separator,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(count, maximumCount, "count");
        Guard.NotNull(separator, "separator");
        Guard.OutputSize(separator.Length, maximumOutputSize, "separator");
        long worst = (long)count * catalog.MaximumWordLength;
        if (count > 1)
        {
            worst = worst + ((long)(count - 1) * separator.Length);
        }
        Guard.OutputSize(worst, maximumOutputSize, "count");
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < count)
        {
            if (index > 0)
            {
                builder.Append(separator);
            }
            builder.Append(catalog.ChooseWord(random));
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static string Sentence(
        FakeRandom random,
        LoremCatalog catalog,
        int wordCount,
        int maximumCount,
        int maximumOutputSize,
        string locale)
    {
        Guard.Positive(wordCount, "wordCount");
        EnsureSentences(
            catalog,
            1,
            wordCount,
            "",
            locale,
            maximumOutputSize,
            "wordCount");
        string words = Words(random, catalog, wordCount, " ", maximumCount, maximumOutputSize);
        int[] starts = StringInfo.ParseCombiningCharacters(words);
        int firstLength = starts.Length > 1 ? starts[1] : words.Length;
        string first = TextTransforms.Upper(words.Substring(0, firstLength), locale, maximumOutputSize);
        string result = first + words.Substring(firstLength) + ".";
        Guard.OutputSize(result.Length, maximumOutputSize, "wordCount");
        return result;
    }

    internal static void EnsureSentences(
        LoremCatalog catalog,
        int sentenceCount,
        int maximumWords,
        string separator,
        string locale,
        int maximumOutputSize,
        string parameterName)
    {
        Guard.NonNegative(sentenceCount, parameterName);
        Guard.Positive(maximumWords, "maximumWords");
        Guard.NotNull(separator, "separator");
        long sentenceLength = (long)maximumWords * catalog.MaximumWordLength;
        sentenceLength = sentenceLength + maximumWords;
        sentenceLength = sentenceLength + MaximumFirstExpansion(
            catalog,
            locale,
            maximumOutputSize);
        long total = (long)sentenceCount * sentenceLength;
        if (sentenceCount > 1)
        {
            total = total + ((long)(sentenceCount - 1) * separator.Length);
        }
        Guard.OutputSize(total, maximumOutputSize, parameterName);
    }

    internal static void EnsureParagraphs(
        LoremCatalog catalog,
        int paragraphCount,
        string locale,
        int maximumOutputSize)
    {
        long sentenceLength = (12L * catalog.MaximumWordLength) + 12L;
        sentenceLength = sentenceLength + MaximumFirstExpansion(
            catalog,
            locale,
            maximumOutputSize);
        long paragraphLength = (6L * sentenceLength) + 5L;
        long total = (long)paragraphCount * paragraphLength;
        if (paragraphCount > 1)
        {
            total = total + ((long)(paragraphCount - 1) * 2L);
        }
        Guard.OutputSize(total, maximumOutputSize, "count");
    }

    internal static string Text(
        FakeRandom random,
        LoremCatalog catalog,
        int length,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.OutputSize(length, maximumOutputSize, "length");
        if (length == 0)
        {
            return "";
        }
        long capacity = (long)maximumCount * (catalog.MinimumWordLength + 1L);
        if (length > capacity)
        {
            throw new ArgumentOutOfRangeException("length", length, $"length requires more than the configured maximum of {maximumCount} word selections.");
        }
        StringBuilder builder = new StringBuilder(length);
        int selections = 0;
        while (builder.Length < length)
        {
            Guard.Count(selections + 1, maximumCount, "length");
            string word = catalog.ChooseWord(random);
            selections = selections + 1;
            int remaining = length - builder.Length;
            if (builder.Length > 0)
            {
                if (remaining == 1)
                {
                    builder.Append('.');
                    break;
                }
                builder.Append(' ');
                remaining = remaining - 1;
            }
            if (word.Length <= remaining)
            {
                builder.Append(word);
                continue;
            }
            string prefix = Prefix(word, remaining);
            builder.Append(prefix);
            while (builder.Length < length)
            {
                builder.Append('.');
            }
        }
        return builder.ToString();
    }

    static string Prefix(string value, int maximumLength)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(value);
        int length = 0;
        int index = 0;
        while (index < starts.Length)
        {
            int end = index + 1 < starts.Length ? starts[index + 1] : value.Length;
            if (end > maximumLength)
            {
                break;
            }
            length = end;
            index = index + 1;
        }
        return value.Substring(0, length);
    }

    static int MaximumFirstExpansion(
        LoremCatalog catalog,
        string locale,
        int maximumOutputSize)
    {
        int uppercaseLimit = Math.Max(
            maximumOutputSize,
            FakeLimits.DefaultMaximumOutputSize);
        int maximum = 0;
        int index = 0;
        while (index < catalog.Values.Length)
        {
            string word = catalog.Values[index];
            int[] starts = StringInfo.ParseCombiningCharacters(word);
            int firstLength = starts.Length > 1 ? starts[1] : word.Length;
            string upper = TextTransforms.Upper(
                word.Substring(0, firstLength),
                locale,
                uppercaseLimit);
            int expansion = upper.Length - firstLength;
            if (expansion > maximum)
            {
                maximum = expansion;
            }
            index = index + 1;
        }
        return maximum;
    }

    static JsonElement Required(JsonElement value, string name)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }
        Invalid($"requires member '{name}'");
        return default(JsonElement);
    }

    static string RequiredString(JsonElement value, string name, int index)
    {
        JsonElement member = Required(value, name);
        if (member.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(member.GetString()))
        {
            Invalid($"values[{index}].{name} must be a nonempty string");
        }
        return JsonText.Required(member, "values[" + index + "]." + name);
    }

    static void ValidateWord(string value, int index)
    {
        UnicodeText.Validate(value, "localeWordData");
        int characterIndex = 0;
        while (characterIndex < value.Length)
        {
            if (char.IsWhiteSpace(value[characterIndex]))
            {
                Invalid($"values[{index}].text must contain one token without whitespace");
            }
            characterIndex = characterIndex + 1;
        }
    }

    static void ValidatePart(string value, int index)
    {
        int characterIndex = 0;
        while (characterIndex < value.Length)
        {
            char character = value[characterIndex];
            bool valid = character >= 'a' && character <= 'z';
            valid = valid || character == '-' && characterIndex > 0 && characterIndex < value.Length - 1;
            if (!valid)
            {
                Invalid($"values[{index}].partOfSpeech must use lowercase ASCII letters and internal hyphens");
            }
            characterIndex = characterIndex + 1;
        }
    }

    static void ValidatePositiveParts(string[] parts, double[] weights)
    {
        HashSet<string> checkedParts = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index];
            if (checkedParts.Add(part))
            {
                bool positive = false;
                int weightIndex = 0;
                while (weightIndex < parts.Length)
                {
                    if (parts[weightIndex] == part && weights[weightIndex] > 0.0)
                    {
                        positive = true;
                    }
                    weightIndex = weightIndex + 1;
                }
                if (!positive)
                {
                    Invalid($"part of speech '{part}' must contain a positive weight");
                }
            }
            index = index + 1;
        }
    }

    static void Invalid(string reason)
    {
        throw FakeException.Create($"Resolved locale field 'text.words' {reason}.");
    }
}
