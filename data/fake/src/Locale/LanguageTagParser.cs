using System;
using System.Collections.Generic;

namespace fake;

internal static class LanguageTagParser
{
    internal static LanguageTag Parse(string tag, string parameterName)
    {
        tag = Guard.NotNullOrWhiteSpace(tag, parameterName);
        string[] parts = Split(tag, parameterName);
        LanguageRegistry registry = LanguageRegistryStore.Current();
        LanguageRegistryEntry? registeredTag = registry.Tag(tag);
        if (registeredTag != null)
        {
            if (!string.IsNullOrEmpty(registeredTag.PreferredValue))
            {
                LanguageTag preferred = Parse(registeredTag.PreferredValue, parameterName);
                preferred.Original = tag;
                preferred.Grandfathered = registeredTag.Type == "grandfathered";
                preferred.Aliased = true;
                return preferred;
            }
            if (registeredTag.Type == "grandfathered")
            {
                LanguageTag grandfathered = NewTag(tag);
                grandfathered.Normalized = registeredTag.Value;
                grandfathered.Grandfathered = true;
                return grandfathered;
            }
        }
        if (parts[0].Length == 1 && Lower(parts[0]) == "x")
        {
            return ParsePrivateUse(tag, parts, parameterName);
        }
        return ParseNormal(tag, parts, parameterName, registry);
    }

    static LanguageTag ParseNormal(string tag, string[] parts, string parameterName, LanguageRegistry registry)
    {
        LanguageTag result = NewTag(tag);
        int index = 0;
        string originalLanguage = Lower(parts[index]);
        if (originalLanguage.Length < 2 || originalLanguage.Length > 8 || !Letters(originalLanguage))
        {
            throw Invalid(tag, parameterName, "the primary language subtag is malformed");
        }
        LanguageRegistryEntry? languageEntry = registry.Language(originalLanguage);
        if (languageEntry == null && !PrivateLanguage(originalLanguage))
        {
            throw Invalid(tag, parameterName, $"language subtag '{parts[index]}' is not registered");
        }
        string language = originalLanguage;
        if (languageEntry != null && !string.IsNullOrEmpty(languageEntry.PreferredValue))
        {
            language = Lower(languageEntry.PreferredValue);
            result.Aliased = true;
        }
        index = index + 1;

        List<string> extlangs = new List<string>();
        if (index < parts.Length && parts[index].Length == 3 && Letters(parts[index]))
        {
            string extlang = Lower(parts[index]);
            LanguageRegistryEntry? extlangEntry = registry.Extlang(extlang);
            if (extlangEntry == null)
            {
                throw Invalid(tag, parameterName, $"extended-language subtag '{parts[index]}' is not registered");
            }
            if (!HasPrefix(extlangEntry, originalLanguage))
            {
                throw Invalid(tag, parameterName, $"extended-language subtag '{parts[index]}' does not permit prefix '{parts[0]}'");
            }
            extlangs.Add(extlang);
            if (string.IsNullOrEmpty(extlangEntry.PreferredValue))
                throw Invalid(tag, parameterName, $"extended-language subtag '{parts[index]}' has no preferred value");
            language = Lower(extlangEntry.PreferredValue);
            result.Aliased = true;
            index = index + 1;
            if (index < parts.Length && parts[index].Length == 3 && Letters(parts[index]))
            {
                throw Invalid(tag, parameterName, "second and third extended-language positions are reserved");
            }
        }

        string? script = null;
        if (index < parts.Length && parts[index].Length == 4 && Letters(parts[index]))
        {
            string scriptValue = Lower(parts[index]);
            LanguageRegistryEntry? scriptEntry = registry.Script(scriptValue);
            if (scriptEntry == null && !PrivateScript(scriptValue))
            {
                throw Invalid(tag, parameterName, $"script subtag '{parts[index]}' is not registered");
            }
            if (scriptEntry != null && !string.IsNullOrEmpty(scriptEntry.PreferredValue))
            {
                scriptValue = Lower(scriptEntry.PreferredValue);
                result.Aliased = true;
            }
            script = Title(scriptValue);
            index = index + 1;
        }

        string? region = null;
        if (index < parts.Length && RegionShape(parts[index]))
        {
            string regionValue = Lower(parts[index]);
            LanguageRegistryEntry? regionEntry = registry.Region(regionValue);
            if (regionEntry == null && !PrivateRegion(regionValue))
            {
                throw Invalid(tag, parameterName, $"region subtag '{parts[index]}' is not registered");
            }
            if (regionEntry != null && !string.IsNullOrEmpty(regionEntry.PreferredValue))
            {
                regionValue = Lower(regionEntry.PreferredValue);
                result.Aliased = true;
            }
            region = Letters(regionValue) ? regionValue.ToUpperInvariant() : regionValue;
            index = index + 1;
        }

        List<string> variants = new List<string>();
        HashSet<string> originalVariants = NewSet();
        HashSet<string> canonicalVariants = NewSet();
        while (index < parts.Length && VariantShape(parts[index]))
        {
            string originalVariant = Lower(parts[index]);
            if (!originalVariants.Add(originalVariant))
            {
                throw Invalid(tag, parameterName, $"variant subtag '{parts[index]}' is duplicated");
            }
            LanguageRegistryEntry? variantEntry = registry.Variant(originalVariant);
            if (variantEntry == null)
            {
                throw Invalid(tag, parameterName, $"variant subtag '{parts[index]}' is not registered");
            }
            string variant = originalVariant;
            if (!string.IsNullOrEmpty(variantEntry.PreferredValue))
            {
                variant = Lower(variantEntry.PreferredValue);
                result.Aliased = true;
            }
            if (!canonicalVariants.Add(variant))
            {
                throw Invalid(tag, parameterName, $"variant alias '{variant}' would be duplicated");
            }
            variants.Add(variant);
            index = index + 1;
        }

        List<string> extensions = new List<string>();
        HashSet<string> singletons = NewSet();
        while (index < parts.Length && Singleton(parts[index]) && Lower(parts[index]) != "x")
        {
            string singleton = Lower(parts[index]);
            if (!singletons.Add(singleton))
            {
                throw Invalid(tag, parameterName, $"extension singleton '{parts[index]}' is duplicated");
            }
            List<string> extension = new List<string>();
            extension.Add(singleton);
            index = index + 1;
            int valueCount = 0;
            while (index < parts.Length && parts[index].Length >= 2 && parts[index].Length <= 8 && AlphaNumeric(parts[index]))
            {
                extension.Add(Lower(parts[index]));
                valueCount = valueCount + 1;
                index = index + 1;
            }
            if (valueCount == 0)
            {
                throw Invalid(tag, parameterName, $"extension singleton '{singleton}' has no value");
            }
            extensions.Add(string.Join("-", extension));
        }
        extensions.Sort(StringComparer.Ordinal);

        List<string> privateUse = new List<string>();
        if (index < parts.Length && Lower(parts[index]) == "x")
        {
            index = index + 1;
            if (index == parts.Length)
            {
                throw Invalid(tag, parameterName, "private-use singleton 'x' has no value");
            }
            while (index < parts.Length && parts[index].Length >= 1 && parts[index].Length <= 8 && AlphaNumeric(parts[index]))
            {
                privateUse.Add(Lower(parts[index]));
                index = index + 1;
            }
        }
        if (index != parts.Length)
        {
            throw Invalid(tag, parameterName, $"subtag '{parts[index]}' is malformed or out of order");
        }

        result.Language = language;
        result.Extlangs = extlangs.ToArray();
        result.Script = script;
        result.Region = region;
        result.Variants = variants.ToArray();
        result.Extensions = extensions.ToArray();
        result.PrivateUse = privateUse.ToArray();
        result.Normalized = Build(result);
        return result;
    }

    static LanguageTag ParsePrivateUse(string tag, string[] parts, string parameterName)
    {
        if (parts.Length == 1)
        {
            throw Invalid(tag, parameterName, "private-use singleton 'x' has no value");
        }
        List<string> privateUse = new List<string>();
        int index = 1;
        while (index < parts.Length)
        {
            if (parts[index].Length < 1 || parts[index].Length > 8 || !AlphaNumeric(parts[index]))
            {
                throw Invalid(tag, parameterName, $"private-use subtag '{parts[index]}' is malformed");
            }
            privateUse.Add(Lower(parts[index]));
            index = index + 1;
        }
        LanguageTag result = NewTag(tag);
        result.PrivateUse = privateUse.ToArray();
        result.Normalized = "x-" + string.Join("-", privateUse);
        return result;
    }

    static string Build(LanguageTag tag)
    {
        List<string> parts = new List<string>();
        if (tag.Language == null)
            throw FakeException.Create("A normal language tag has no primary language.");
        parts.Add(tag.Language);
        if (tag.Script != null)
        {
            parts.Add(tag.Script);
        }
        if (tag.Region != null)
        {
            parts.Add(tag.Region);
        }
        int index = 0;
        while (index < tag.Variants.Length)
        {
            parts.Add(tag.Variants[index]);
            index = index + 1;
        }
        index = 0;
        while (index < tag.Extensions.Length)
        {
            parts.Add(tag.Extensions[index]);
            index = index + 1;
        }
        if (tag.PrivateUse.Length > 0)
        {
            parts.Add("x");
            index = 0;
            while (index < tag.PrivateUse.Length)
            {
                parts.Add(tag.PrivateUse[index]);
                index = index + 1;
            }
        }
        return string.Join("-", parts);
    }

    static string[] Split(string tag, string parameterName)
    {
        int index = 0;
        while (index < tag.Length)
        {
            char value = tag[index];
            if (value != '-' && !AsciiLetter(value) && !AsciiDigit(value))
            {
                throw Invalid(tag, parameterName, "only ASCII letters, digits, and hyphens are permitted");
            }
            index = index + 1;
        }
        string[] parts = tag.Split('-');
        index = 0;
        while (index < parts.Length)
        {
            if (parts[index].Length == 0)
            {
                throw Invalid(tag, parameterName, "empty subtags are not permitted");
            }
            if (parts[index].Length > 8)
            {
                throw Invalid(tag, parameterName, $"subtag '{parts[index]}' exceeds eight characters");
            }
            index = index + 1;
        }
        return parts;
    }

    static bool HasPrefix(LanguageRegistryEntry entry, string prefix)
    {
        int index = 0;
        while (index < entry.Prefixes.Length)
        {
            if (Lower(entry.Prefixes[index]) == Lower(prefix))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool PrivateLanguage(string value)
    {
        return value.Length == 3 && string.CompareOrdinal(value, "qaa") >= 0 && string.CompareOrdinal(value, "qtz") <= 0;
    }

    static bool PrivateScript(string value)
    {
        return value.Length == 4 && string.CompareOrdinal(value, "qaaa") >= 0 && string.CompareOrdinal(value, "qabx") <= 0;
    }

    static bool PrivateRegion(string value)
    {
        if (value == "aa" || value == "zz")
        {
            return true;
        }
        if (value.Length != 2)
        {
            return false;
        }
        return (string.CompareOrdinal(value, "qm") >= 0 && string.CompareOrdinal(value, "qz") <= 0)
            || (string.CompareOrdinal(value, "xa") >= 0 && string.CompareOrdinal(value, "xz") <= 0);
    }

    static bool RegionShape(string value)
    {
        return (value.Length == 2 && Letters(value)) || (value.Length == 3 && Digits(value));
    }

    static bool VariantShape(string value)
    {
        if (!AlphaNumeric(value))
        {
            return false;
        }
        if (value.Length >= 5 && value.Length <= 8)
        {
            return true;
        }
        return value.Length == 4 && AsciiDigit(value[0]);
    }

    static bool Singleton(string value)
    {
        return value.Length == 1 && AlphaNumeric(value);
    }

    static bool Letters(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiLetter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    static bool Digits(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    static bool AlphaNumeric(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!AsciiLetter(value[index]) && !AsciiDigit(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return value.Length > 0;
    }

    static bool AsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static string Lower(string value)
    {
        return value.ToLowerInvariant();
    }

    static string Title(string value)
    {
        return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1).ToLowerInvariant();
    }

    static HashSet<string> NewSet()
    {
        return new HashSet<string>(StringComparer.Ordinal);
    }

    static LanguageTag NewTag(string original)
    {
        LanguageTag tag = new LanguageTag();
        tag.Original = original;
        return tag;
    }

    static ArgumentException Invalid(string tag, string parameterName, string reason)
    {
        return new ArgumentException($"Language tag '{tag}' is invalid: {reason}.", parameterName);
    }
}
