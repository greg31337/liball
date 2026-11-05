namespace languagecodes;

internal static class LanguageTagParser
{
    internal static LanguageTag ParseTag(string? value)
    {
        if (value == null || value.Length == 0)
        {
            throw new InvalidLanguageTagException("A language tag is required.");
        }

        LanguageValidationResult validation =
            LanguageTagValidationEngine.ValidateTagWellFormed(value);
        if (!validation.IsWellFormed)
        {
            throw CreateInvalidTagException(value);
        }

        IanaSubtagRow? completeRecord = SubtagIndex.FindCompleteTag(value);
        if (completeRecord != null &&
            completeRecord.RecordType == LanguageSubtagType.Grandfathered)
        {
            return PublicResultFactory.CopyLanguageTag(
                new LanguageTag
                {
                    Value = value,
                    NormalizedValue = completeRecord.Value,
                    IsGrandfathered = true
                });
        }

        string[] subtags = value.Split('-');
        EnsureTagSubtagsArePresent(subtags);
        if (IsPrivateUseSingleton(subtags[0]))
        {
            return ParsePrivateUseOnlyTag(value, subtags);
        }

        global::System.Collections.Generic.List<string> normalized =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> extlangs =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> variants =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> extensions =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> privateUse =
            new global::System.Collections.Generic.List<string>();

        string language = subtags[0];
        if (!IsLanguageSubtag(language))
        {
            throw CreateInvalidTagException(value);
        }

        language = LowercaseTagSubtag(language);
        normalized.Add(language);
        int index = 1;
        if (language.Length <= 3)
        {
            while (index < subtags.Length &&
                extlangs.Count < 3 &&
                IsAlphabeticSubtag(subtags[index], 3, 3))
            {
                string extlang = LowercaseTagSubtag(subtags[index]);
                extlangs.Add(extlang);
                normalized.Add(extlang);
                index++;
            }
        }

        string? script = null;
        if (index < subtags.Length &&
            IsAlphabeticSubtag(subtags[index], 4, 4))
        {
            script = TitlecaseScriptSubtag(subtags[index]);
            normalized.Add(script);
            index++;
        }

        string? region = null;
        if (index < subtags.Length && IsRegionSubtag(subtags[index]))
        {
            region = NormalizeRegionSubtag(subtags[index]);
            normalized.Add(region);
            index++;
        }

        while (index < subtags.Length && IsVariantSubtag(subtags[index]))
        {
            string variant = LowercaseTagSubtag(subtags[index]);
            variants.Add(variant);
            normalized.Add(variant);
            index++;
        }

        while (index < subtags.Length && IsExtensionSingleton(subtags[index]))
        {
            global::System.Collections.Generic.List<string> extensionParts =
                new global::System.Collections.Generic.List<string>();
            string singleton = LowercaseTagSubtag(subtags[index]);
            extensionParts.Add(singleton);
            normalized.Add(singleton);
            index++;
            int payloadCount = 0;
            while (index < subtags.Length &&
                IsAlphanumericSubtag(subtags[index], 2, 8))
            {
                string payload = LowercaseTagSubtag(subtags[index]);
                extensionParts.Add(payload);
                normalized.Add(payload);
                payloadCount++;
                index++;
            }

            if (payloadCount == 0)
            {
                throw CreateInvalidTagException(value);
            }

            extensions.Add(string.Join("-", extensionParts.ToArray()));
        }

        if (index < subtags.Length && IsPrivateUseSingleton(subtags[index]))
        {
            normalized.Add("x");
            index++;
            int privateUseCount = 0;
            while (index < subtags.Length &&
                IsAlphanumericSubtag(subtags[index], 1, 8))
            {
                string privatePart = LowercaseTagSubtag(subtags[index]);
                privateUse.Add(privatePart);
                normalized.Add(privatePart);
                privateUseCount++;
                index++;
            }

            if (privateUseCount == 0)
            {
                throw CreateInvalidTagException(value);
            }
        }

        if (index != subtags.Length)
        {
            throw CreateInvalidTagException(value);
        }

        return PublicResultFactory.CopyLanguageTag(
            new LanguageTag
            {
                Value = value,
                NormalizedValue = string.Join("-", normalized.ToArray()),
                Language = language,
                Extlangs = extlangs.ToArray(),
                Script = script,
                Region = region,
                Variants = variants.ToArray(),
                Extensions = extensions.ToArray(),
                PrivateUse = privateUse.ToArray(),
                IsRedundant = completeRecord != null &&
                    completeRecord.RecordType == LanguageSubtagType.Redundant
            });
    }

    internal static LanguageTag ParsePrivateUseOnlyTag(string value, string[] subtags)
    {
        if (subtags.Length < 2)
        {
            throw CreateInvalidTagException(value);
        }

        global::System.Collections.Generic.List<string> privateUse =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<string> normalized =
            new global::System.Collections.Generic.List<string>();
        normalized.Add("x");
        int index;
        for (index = 1; index < subtags.Length; index++)
        {
            if (!IsAlphanumericSubtag(subtags[index], 1, 8))
            {
                throw CreateInvalidTagException(value);
            }

            string part = LowercaseTagSubtag(subtags[index]);
            privateUse.Add(part);
            normalized.Add(part);
        }

        return PublicResultFactory.CopyLanguageTag(
            new LanguageTag
            {
                Value = value,
                NormalizedValue = string.Join("-", normalized.ToArray()),
                PrivateUse = privateUse.ToArray(),
                IsPrivateUseOnly = true
            });
    }

    internal static void EnsureTagSubtagsArePresent(string[] subtags)
    {
        int index;
        for (index = 0; index < subtags.Length; index++)
        {
            if (subtags[index].Length == 0)
            {
                throw new InvalidLanguageTagException(
                    "Language tags cannot contain empty subtags.");
            }
        }
    }

    internal static bool IsLanguageSubtag(string value)
    {
        return IsAlphabeticSubtag(value, 2, 8);
    }

    internal static bool IsRegionSubtag(string value)
    {
        return IsAlphabeticSubtag(value, 2, 2) ||
            IsNumericSubtag(value, 3, 3);
    }

    internal static bool IsVariantSubtag(string value)
    {
        if (IsAlphanumericSubtag(value, 5, 8))
        {
            return true;
        }

        return value.Length == 4 &&
            value[0] >= '0' && value[0] <= '9' &&
            IsAlphanumericSubtag(value, 4, 4);
    }

    internal static bool IsExtensionSingleton(string value)
    {
        if (value.Length != 1 || IsPrivateUseSingleton(value))
        {
            return false;
        }

        return IsAsciiTagLetter(value[0]) ||
            value[0] >= '0' && value[0] <= '9';
    }

    internal static bool IsPrivateUseSingleton(string value)
    {
        return value.Length == 1 && AsciiCode.ToLowerAscii(value[0]) == 'x';
    }

    internal static bool IsAlphabeticSubtag(string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            if (!IsAsciiTagLetter(value[index]))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsNumericSubtag(string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsAlphanumericSubtag(string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (!IsAsciiTagLetter(character) &&
                (character < '0' || character > '9'))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsAsciiTagLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal static string LowercaseTagSubtag(string value)
    {
        char[] characters = value.ToCharArray();
        int index;
        for (index = 0; index < characters.Length; index++)
        {
            characters[index] = AsciiCode.ToLowerAscii(characters[index]);
        }

        return new string(characters);
    }

    internal static string TitlecaseScriptSubtag(string value)
    {
        char[] characters = LowercaseTagSubtag(value).ToCharArray();
        characters[0] = (char)(characters[0] - ('a' - 'A'));
        return new string(characters);
    }

    internal static string NormalizeRegionSubtag(string value)
    {
        if (value.Length == 3)
        {
            return value;
        }

        char[] characters = value.ToCharArray();
        int index;
        for (index = 0; index < characters.Length; index++)
        {
            if (characters[index] >= 'a' && characters[index] <= 'z')
            {
                characters[index] = (char)(characters[index] - ('a' - 'A'));
            }
        }

        return new string(characters);
    }

    internal static InvalidLanguageTagException CreateInvalidTagException(string value)
    {
        return new InvalidLanguageTagException("Invalid RFC 5646 language tag: " + value);
    }
}
