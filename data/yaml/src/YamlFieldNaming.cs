using System.Collections.Generic;
using System.Text;

namespace yaml;

/// <summary>
/// Converts public field names according to an object-mapping naming convention.
/// </summary>
internal static class YamlFieldNaming
{
    /// <summary>
    /// Converts a CLR field name to the mapping key text selected by the naming convention.
    /// </summary>
    /// <param name="name">The directive, anchor, field, or type name.</param>
    /// <param name="convention">The naming convention to apply.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Apply(string? name, YamlNamingConvention convention)
    {
        string text = name == null ? "" : name;
        if (convention == YamlNamingConvention.Exact)
        {
            return text;
        }

        List<string> words = SplitWords(text);
        if (convention == YamlNamingConvention.CamelCase)
        {
            return ToCamelCase(words);
        }

        if (convention == YamlNamingConvention.PascalCase)
        {
            return ToPascalCase(words);
        }

        if (convention == YamlNamingConvention.SnakeCase)
        {
            return JoinDelimitedLower(words, "_");
        }

        if (convention == YamlNamingConvention.KebabCase)
        {
            return JoinDelimitedLower(words, "-");
        }

        return text;
    }

    internal static List<string> SplitWords(string name)
    {
        List<string> words = new List<string>();
        string text = name == null ? "" : name;
        StringBuilder current = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '_' || value == '-' || value == ' ')
            {
                AddCurrentWord(words, current);
            }
            else
            {
                if (StartsNewWord(text, index, current))
                {
                    AddCurrentWord(words, current);
                }

                current.Append(value);
            }

            index = index + 1;
        }

        AddCurrentWord(words, current);
        return words;
    }

    internal static bool StartsNewWord(string text, int index, StringBuilder current)
    {
        if (current == null || current.Length == 0)
        {
            return false;
        }

        char value = text[index];
        if (!char.IsUpper(value))
        {
            return false;
        }

        char previous = text[index - 1];
        if (!char.IsUpper(previous))
        {
            return true;
        }

        if (index + 1 < text.Length && char.IsLower(text[index + 1]))
        {
            return true;
        }

        return false;
    }

    internal static void AddCurrentWord(List<string> words, StringBuilder current)
    {
        if (words == null || current == null || current.Length == 0)
        {
            return;
        }

        words.Add(current.ToString());
        current.Clear();
    }

    internal static string ToCamelCase(List<string> words)
    {
        if (words == null || words.Count == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(words[0].ToLowerInvariant());
        int index = 1;
        while (index < words.Count)
        {
            builder.Append(ToTitleWord(words[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string ToPascalCase(List<string> words)
    {
        if (words == null || words.Count == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < words.Count)
        {
            builder.Append(ToTitleWord(words[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string JoinDelimitedLower(List<string> words, string delimiter)
    {
        if (words == null || words.Count == 0)
        {
            return "";
        }

        string separator = delimiter == null ? "" : delimiter;
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < words.Count)
        {
            if (index > 0)
            {
                builder.Append(separator);
            }

            builder.Append(words[index].ToLowerInvariant());
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string ToTitleWord(string word)
    {
        string text = word == null ? "" : word.ToLowerInvariant();
        if (text.Length == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(char.ToUpperInvariant(text[0]));
        if (text.Length > 1)
        {
            builder.Append(text.Substring(1));
        }

        return builder.ToString();
    }
}
