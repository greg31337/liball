using System.Collections.Generic;

namespace args;

internal class ArgValuePipeline
{
    internal static string Normalize(string value, System.Func<string, string>? normalizer)
    {
        if (normalizer == null)
        {
            return value;
        }

        string? normalized = normalizer(value);
        if (normalized == null)
        {
            throw new System.InvalidOperationException("A value normalizer returned null.");
        }

        return normalized;
    }

    internal static bool ContainsChoice(List<string> choices, string value, ArgChoiceValidationStage stage, System.Func<string, string>? normalizer, System.Func<string, ArgConvertResult?>? converter)
    {
        if (stage == ArgChoiceValidationStage.Raw)
        {
            return ContainsRawChoice(choices, value);
        }

        string normalizedValue = Normalize(value, normalizer);
        if (stage == ArgChoiceValidationStage.Normalized)
        {
            return ContainsNormalizedChoice(choices, normalizedValue, normalizer);
        }

        if (converter == null)
        {
            return false;
        }

        ArgConvertResult? convertedValue = converter(normalizedValue);
        if (convertedValue == null || !convertedValue.Success)
        {
            return false;
        }

        int index = 0;
        while (index < choices.Count)
        {
            string normalizedChoice = Normalize(choices[index], normalizer);
            ArgConvertResult? convertedChoice = converter(normalizedChoice);
            if (convertedChoice != null && convertedChoice.Success && object.Equals(convertedChoice.Value, convertedValue.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsRawChoice(List<string> choices, string value)
    {
        int index = 0;
        while (index < choices.Count)
        {
            if (choices[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsNormalizedChoice(List<string> choices, string normalizedValue, System.Func<string, string>? normalizer)
    {
        int index = 0;
        while (index < choices.Count)
        {
            if (Normalize(choices[index], normalizer) == normalizedValue)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
