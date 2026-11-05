using System.Globalization;
using System.Text;

namespace fake;

internal class PersonProfileName
{
    internal string FirstName = string.Empty;
    internal string MiddleName = string.Empty;
    internal string LastName = string.Empty;
    internal string MaidenName = string.Empty;
    internal string Patronymic = string.Empty;
    internal string Matronymic = string.Empty;
    internal string Prefix = string.Empty;
    internal string Suffix = string.Empty;
    internal string Initials = string.Empty;
    internal string FullName = string.Empty;
    internal string NativeName = string.Empty;
    internal string TransliteratedName = string.Empty;
}

internal static class PersonNameGenerator
{
    internal static PersonProfileName ProfileName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        string sexCategory = data.ChooseSexCategory(random);
        PersonNameEntryData given = data.ChooseEntry(
            random,
            data.GivenNames,
            sexCategory);
        PersonNameEntryData family = data.ChooseEntry(
            random,
            data.FamilyNames,
            sexCategory);
        PersonNameFormatData format = data.ChooseFormat(random, data.FullFormats);
        PersonProfileName result = new PersonProfileName();
        result.FirstName = given.Value;
        result.LastName = family.Value;
        result.FullName = RenderProfile(
            context,
            random,
            data,
            format,
            sexCategory,
            given.Value,
            family.Value,
            result);
        result.NativeName = result.FullName;
        result.TransliteratedName = TextTransforms.TransliterateAll(
            result.FullName,
            context.CapturedOptions.MaximumOutputSize);
        return result;
    }

    internal static string FirstName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.GivenNames);
    }

    internal static string FirstName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.GivenNames, sexCategory);
    }

    internal static string MiddleName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.MiddleNames);
    }

    internal static string MiddleName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.MiddleNames, sexCategory);
    }

    internal static string LastName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.FamilyNames);
    }

    internal static string LastName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.FamilyNames, sexCategory);
    }

    internal static string MaidenName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.MaidenNames);
    }

    internal static string MaidenName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.MaidenNames, sexCategory);
    }

    internal static string Patronymic(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Patronymics);
    }

    internal static string Patronymic(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Patronymics, sexCategory);
    }

    internal static string Matronymic(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Matronymics);
    }

    internal static string Matronymic(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Matronymics, sexCategory);
    }

    internal static string Prefix(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Prefixes);
    }

    internal static string Prefix(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Prefixes, sexCategory);
    }

    internal static string Suffix(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Suffixes);
    }

    internal static string Suffix(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, data.Suffixes, sexCategory);
    }

    internal static string Initial(
        GenerationContext context,
        FakeRandom random)
    {
        return FirstTextElement(FirstName(context, random));
    }

    internal static string Initial(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        return FirstTextElement(FirstName(context, random, sexCategory));
    }

    internal static string Initials(
        GenerationContext context,
        FakeRandom random,
        int count)
    {
        Guard.Count(count, context.CapturedOptions.MaximumCount, "count");
        if (count == 0)
        {
            return string.Empty;
        }
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        string sexCategory = ChooseInitialsSexCategory(data, random, count);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < count)
        {
            PersonNameEntryData[] entries = data.GivenNames;
            if (index == count - 1 && count > 1)
            {
                entries = data.FamilyNames;
            }
            else if (index > 0 && data.MiddleNames.Length > 0)
            {
                entries = data.MiddleNames;
            }
            string initial = FirstTextElement(
                Entry(data, random, entries, sexCategory));
            Append(
                result,
                initial,
                context.CapturedOptions.MaximumOutputSize);
            index++;
        }
        return result.ToString();
    }

    static string ChooseInitialsSexCategory(
        PersonLocaleData data,
        FakeRandom random,
        int count)
    {
        double[] weights = new double[data.SexCategories.Length];
        double totalWeight = 0.0;
        int index = 0;
        while (index < data.SexCategories.Length)
        {
            string category = data.SexCategories[index].Id;
            bool valid = HasSelectableEntry(data.GivenNames, category);
            if (count > 1)
            {
                valid = valid && HasSelectableEntry(data.FamilyNames, category);
            }
            if (count > 2 && data.MiddleNames.Length > 0)
            {
                valid = valid && HasSelectableEntry(data.MiddleNames, category);
            }
            if (valid)
            {
                weights[index] = data.SexCategories[index].Weight;
                totalWeight += weights[index];
            }
            index++;
        }
        if (totalWeight <= 0.0)
        {
            throw FakeException.Create(
                "Resolved person locale data has no naming category for the requested initials.");
        }
        return data.SexCategories[random.WeightedDiscrete(weights)].Id;
    }

    static bool HasSelectableEntry(
        PersonNameEntryData[] entries,
        string sexCategory)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].AppliesTo(sexCategory) &&
                entries[index].Weight > 0.0)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string TransliteratedFirstName(
        GenerationContext context,
        FakeRandom random)
    {
        return TransliteratedComponent(context, FirstName(context, random));
    }

    internal static string TransliteratedMiddleName(
        GenerationContext context,
        FakeRandom random)
    {
        return TransliteratedComponent(context, MiddleName(context, random));
    }

    internal static string TransliteratedLastName(
        GenerationContext context,
        FakeRandom random)
    {
        return TransliteratedComponent(context, LastName(context, random));
    }

    internal static string TransliteratedMaidenName(
        GenerationContext context,
        FakeRandom random)
    {
        return TransliteratedComponent(context, MaidenName(context, random));
    }

    internal static string TransliteratedPatronymic(
        GenerationContext context,
        FakeRandom random)
    {
        return TransliteratedComponent(context, Patronymic(context, random));
    }

    internal static string TransliteratedMatronymic(
        GenerationContext context,
        FakeRandom random)
    {
        return TransliteratedComponent(context, Matronymic(context, random));
    }

    internal static string FullName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Render(context, random, data, data.FullFormats);
    }

    internal static string FullName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Render(context, random, data, data.FullFormats, sexCategory);
    }

    internal static string DisplayName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Render(context, random, data, data.InformalFormats);
    }

    internal static string DisplayName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Render(context, random, data, data.InformalFormats, sexCategory);
    }

    internal static string FormalName(
        GenerationContext context,
        FakeRandom random)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Render(context, random, data, data.FormalFormats);
    }

    internal static string FormalName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Render(context, random, data, data.FormalFormats, sexCategory);
    }

    internal static string TransliteratedName(
        GenerationContext context,
        FakeRandom random)
    {
        return TextTransforms.TransliterateAll(
            FullName(context, random),
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static string TransliteratedName(
        GenerationContext context,
        FakeRandom random,
        string sexCategory)
    {
        return TextTransforms.TransliterateAll(
            FullName(context, random, sexCategory),
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static string NameCase(
        GenerationContext context,
        FakeRandom random,
        PersonNameCase nameCase)
    {
        ValidateNameCase(nameCase);
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(data, random, CaseEntries(data, nameCase));
    }

    internal static string NameCase(
        GenerationContext context,
        FakeRandom random,
        PersonNameCase nameCase,
        string sexCategory)
    {
        ValidateNameCase(nameCase);
        PersonLocaleData data = PersonLocaleDataParser.Resolve(context);
        return Entry(
            data,
            random,
            CaseEntries(data, nameCase),
            sexCategory);
    }

    static string Entry(
        PersonLocaleData data,
        FakeRandom random,
        PersonNameEntryData[] entries)
    {
        string sexCategory = data.ChooseSexCategoryFromRandomAndEntries(random, entries);
        return data.ChooseEntry(random, entries, sexCategory).Value;
    }

    static string Entry(
        PersonLocaleData data,
        FakeRandom random,
        PersonNameEntryData[] entries,
        string sexCategory)
    {
        return data.ChooseEntry(random, entries, sexCategory).Value;
    }

    static string FirstTextElement(string value)
    {
        return StringInfo.GetNextTextElement(value, 0);
    }

    static string TransliteratedComponent(
        GenerationContext context,
        string value)
    {
        return TextTransforms.TransliterateAll(
            value,
            context.CapturedOptions.MaximumOutputSize);
    }

    static string RenderProfile(
        GenerationContext context,
        FakeRandom random,
        PersonLocaleData data,
        PersonNameFormatData format,
        string sexCategory,
        string given,
        string family,
        PersonProfileName profile)
    {
        StringBuilder result = new StringBuilder();
        StringBuilder initials = new StringBuilder();
        int index = 0;
        while (index < format.Template.Length)
        {
            if (format.Template[index] == '{')
            {
                int close = format.Template.IndexOf('}', index + 1);
                string component = format.Template.Substring(
                    index + 1,
                    close - index - 1);
                string value;
                if (component == "given")
                {
                    value = given;
                }
                else if (component == "family")
                {
                    value = family;
                }
                else
                {
                    value = Entry(
                        data,
                        random,
                        Entries(data, component),
                        sexCategory);
                }
                StoreProfileComponent(profile, component, value);
                if (component != "prefix" && component != "suffix")
                {
                    Append(
                        initials,
                        FirstTextElement(value),
                        context.CapturedOptions.MaximumOutputSize);
                }
                Append(
                    result,
                    value,
                    context.CapturedOptions.MaximumOutputSize);
                index = close + 1;
                continue;
            }
            Append(
                result,
                format.Template[index].ToString(),
                context.CapturedOptions.MaximumOutputSize);
            index++;
        }
        profile.Initials = initials.ToString();
        return result.ToString();
    }

    static void StoreProfileComponent(
        PersonProfileName profile,
        string component,
        string value)
    {
        if (component == "middle")
        {
            profile.MiddleName = value;
        }
        else if (component == "maiden")
        {
            profile.MaidenName = value;
        }
        else if (component == "patronymic")
        {
            profile.Patronymic = value;
        }
        else if (component == "matronymic")
        {
            profile.Matronymic = value;
        }
        else if (component == "prefix")
        {
            profile.Prefix = value;
        }
        else if (component == "suffix")
        {
            profile.Suffix = value;
        }
    }

    static string Render(
        GenerationContext context,
        FakeRandom random,
        PersonLocaleData data,
        PersonNameFormatData[] formats)
    {
        string sexCategory = data.ChooseSexCategory(random);
        return Render(context, random, data, formats, sexCategory);
    }

    static string Render(
        GenerationContext context,
        FakeRandom random,
        PersonLocaleData data,
        PersonNameFormatData[] formats,
        string sexCategory)
    {
        data.ValidateSexCategory(sexCategory);
        PersonNameFormatData format = data.ChooseFormat(random, formats);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < format.Template.Length)
        {
            if (format.Template[index] == '{')
            {
                int close = format.Template.IndexOf('}', index + 1);
                string component = format.Template.Substring(
                    index + 1,
                    close - index - 1);
                Append(
                    result,
                    Entry(data, random, Entries(data, component), sexCategory),
                    context.CapturedOptions.MaximumOutputSize);
                index = close + 1;
                continue;
            }
            Append(
                result,
                format.Template[index].ToString(),
                context.CapturedOptions.MaximumOutputSize);
            index++;
        }
        return result.ToString();
    }

    static PersonNameEntryData[] Entries(
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
        return data.Suffixes;
    }

    static PersonNameEntryData[] CaseEntries(
        PersonLocaleData data,
        PersonNameCase nameCase)
    {
        switch (nameCase)
        {
            case PersonNameCase.Mononym:
                return data.MononymCases;
            case PersonNameCase.Particle:
                return data.ParticleCases;
            case PersonNameCase.Patronymic:
                return data.PatronymicCases;
            case PersonNameCase.MultiPart:
                return data.MultiPartCases;
            case PersonNameCase.Apostrophe:
                return data.ApostropheCases;
            case PersonNameCase.Hyphenated:
                return data.HyphenatedCases;
            case PersonNameCase.Diacritic:
                return data.DiacriticCases;
            case PersonNameCase.RightToLeft:
                return data.RightToLeftCases;
            case PersonNameCase.OptionalComponentOmitted:
                return data.OptionalComponentCases;
            case PersonNameCase.Long:
                return data.LongCases;
            default:
                throw new System.ArgumentOutOfRangeException(
                    "nameCase",
                    nameCase,
                    "nameCase must be a defined PersonNameCase value.");
        }
    }

    static void ValidateNameCase(PersonNameCase nameCase)
    {
        switch (nameCase)
        {
            case PersonNameCase.Mononym:
            case PersonNameCase.Particle:
            case PersonNameCase.Patronymic:
            case PersonNameCase.MultiPart:
            case PersonNameCase.Apostrophe:
            case PersonNameCase.Hyphenated:
            case PersonNameCase.Diacritic:
            case PersonNameCase.RightToLeft:
            case PersonNameCase.OptionalComponentOmitted:
            case PersonNameCase.Long:
                return;
            default:
                throw new System.ArgumentOutOfRangeException(
                    "nameCase",
                    nameCase,
                    "nameCase must be a defined PersonNameCase value.");
        }
    }

    static void Append(
        StringBuilder builder,
        string value,
        int maximumOutputSize)
    {
        Guard.OutputSize(
            (long)builder.Length + value.Length,
            maximumOutputSize,
            "personName");
        builder.Append(value);
    }
}
