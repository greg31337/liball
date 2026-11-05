namespace pop3;

internal class Pop3LanguageParser
{
    static readonly string[] GrandfatheredLanguageTags = new string[]
    {
        "en-GB-oed",
        "i-ami",
        "i-bnn",
        "i-default",
        "i-enochian",
        "i-hak",
        "i-klingon",
        "i-lux",
        "i-mingo",
        "i-navajo",
        "i-pwn",
        "i-tao",
        "i-tay",
        "i-tsu",
        "sgn-BE-FR",
        "sgn-BE-NL",
        "sgn-CH-DE",
        "art-lojban",
        "cel-gaulish",
        "no-bok",
        "no-nyn",
        "zh-guoyu",
        "zh-hakka",
        "zh-min",
        "zh-min-nan",
        "zh-xiang"
    };

    internal Pop3Language ParseLanguage(System.ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            throw new System.IO.InvalidDataException("A POP3 language listing cannot be empty.");
        }
        if (payload.Length > Pop3Defaults.MaximumResponseLineOctets - 2)
        {
            throw new System.IO.InvalidDataException("A POP3 language listing exceeds 512 octets including CRLF.");
        }

        int separator = 0;
        while (separator < payload.Length && payload[separator] != 32)
        {
            separator++;
        }
        ValidateLanguageTagBytes(payload.Slice(0, separator));

        System.ReadOnlySpan<byte> description = payload.Slice(separator, payload.Length - separator);
        if (separator < payload.Length)
        {
            if (separator == payload.Length - 1 || payload[separator + 1] == 32)
            {
                throw new System.IO.InvalidDataException("A POP3 language description must be nonempty and follow one space.");
            }
            description = payload.Slice(separator + 1, payload.Length - separator - 1);
            ValidateDescription(description);
        }

        Pop3Language language = new Pop3Language();
        language.Tag = System.Text.Encoding.ASCII.GetString(payload.Slice(0, separator));
        if (description.Length > 0)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(false, true);
            language.Description = encoding.GetString(description);
        }
        return language;
    }

    internal Pop3Language[] ParseLanguages(byte[][] lines)
    {
        if (lines == null)
        {
            throw new System.ArgumentNullException("lines");
        }

        Pop3Language[] languages = new Pop3Language[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] == null)
            {
                throw new System.IO.InvalidDataException("A POP3 language listing cannot be null.");
            }
            Pop3Language language = ParseLanguage(lines[index]);
            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (string.Equals(languages[previousIndex].Tag, language.Tag, System.StringComparison.OrdinalIgnoreCase))
                {
                    throw new System.IO.InvalidDataException("A POP3 language listing contains a duplicate language tag.");
                }
                previousIndex++;
            }
            languages[index] = language;
            index++;
        }
        return languages;
    }

    internal bool IsValidLanguageTag(string? tag)
    {
        if (tag == null || tag.Length == 0)
        {
            return false;
        }

        byte[] bytes = new byte[tag.Length];
        int index = 0;
        while (index < tag.Length)
        {
            if (tag[index] > 127)
            {
                return false;
            }
            bytes[index] = (byte)tag[index];
            index++;
        }
        return GetLanguageTagError(bytes) == 0;
    }

    int GetLanguageTagError(System.ReadOnlySpan<byte> tag)
    {
        if (tag.Length == 0)
        {
            return 1;
        }

        int byteIndex = 0;
        while (byteIndex < tag.Length)
        {
            byte value = tag[byteIndex];
            bool isLetter = value >= 65 && value <= 90 ||
                value >= 97 && value <= 122;
            bool isDigit = value >= 48 && value <= 57;
            if (!isLetter && !isDigit && value != 45)
            {
                return 4;
            }
            byteIndex++;
        }

        string text = System.Text.Encoding.ASCII.GetString(tag);
        string[] subtags = text.Split('-', System.StringSplitOptions.None);
        int subtagIndex = 0;
        while (subtagIndex < subtags.Length)
        {
            if (subtags[subtagIndex].Length == 0)
            {
                if (subtagIndex == subtags.Length - 1)
                {
                    return 6;
                }
                return 3;
            }
            if (subtags[subtagIndex].Length > 8)
            {
                return 3;
            }
            subtagIndex++;
        }

        if (IsGrandfatheredTag(text) ||
            IsPrivateUseTag(subtags) ||
            IsNormalLanguageTag(subtags))
        {
            return 0;
        }
        if (!IsAlphaText(subtags[0]))
        {
            return 5;
        }
        return 2;
    }

    bool IsGrandfatheredTag(string tag)
    {
        int index = 0;
        while (index < GrandfatheredLanguageTags.Length)
        {
            if (string.Equals(
                tag,
                GrandfatheredLanguageTags[index],
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    bool IsPrivateUseTag(string[] subtags)
    {
        if (subtags.Length < 2 ||
            !string.Equals(subtags[0], "x", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int index = 1;
        while (index < subtags.Length)
        {
            if (!IsAlphanumericText(subtags[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool IsNormalLanguageTag(string[] subtags)
    {
        string language = subtags[0];
        if (language.Length < 2 || language.Length > 8 || !IsAlphaText(language))
        {
            return false;
        }

        int index = 1;
        if (language.Length <= 3)
        {
            int extlangCount = 0;
            while (index < subtags.Length &&
                extlangCount < 3 &&
                subtags[index].Length == 3 &&
                IsAlphaText(subtags[index]))
            {
                index++;
                extlangCount++;
            }
        }

        if (index < subtags.Length &&
            subtags[index].Length == 4 &&
            IsAlphaText(subtags[index]))
        {
            index++;
        }
        if (index < subtags.Length &&
            (subtags[index].Length == 2 && IsAlphaText(subtags[index]) ||
                subtags[index].Length == 3 && IsDigitText(subtags[index])))
        {
            index++;
        }

        System.Collections.Generic.List<string> variants =
            new System.Collections.Generic.List<string>();
        while (index < subtags.Length && IsVariantText(subtags[index]))
        {
            if (ContainsTextIgnoreCase(variants, subtags[index]))
            {
                return false;
            }
            variants.Add(subtags[index]);
            index++;
        }

        string extensionSingletons = "";
        while (index < subtags.Length &&
            subtags[index].Length == 1 &&
            !string.Equals(subtags[index], "x", System.StringComparison.OrdinalIgnoreCase))
        {
            char singleton = char.ToUpperInvariant(subtags[index][0]);
            if (extensionSingletons.IndexOf(singleton) >= 0)
            {
                return false;
            }
            extensionSingletons += singleton;
            index++;

            int extensionStart = index;
            while (index < subtags.Length &&
                subtags[index].Length >= 2 &&
                IsAlphanumericText(subtags[index]))
            {
                index++;
            }
            if (index == extensionStart)
            {
                return false;
            }
        }

        if (index < subtags.Length &&
            string.Equals(subtags[index], "x", System.StringComparison.OrdinalIgnoreCase))
        {
            index++;
            if (index == subtags.Length)
            {
                return false;
            }
            while (index < subtags.Length)
            {
                if (!IsAlphanumericText(subtags[index]))
                {
                    return false;
                }
                index++;
            }
        }
        return index == subtags.Length;
    }

    bool IsVariantText(string value)
    {
        if (!IsAlphanumericText(value))
        {
            return false;
        }
        if (value.Length >= 5 && value.Length <= 8)
        {
            return true;
        }
        return value.Length == 4 && value[0] >= '0' && value[0] <= '9';
    }

    bool IsAlphaText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!(character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z'))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool IsDigitText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool IsAlphanumericText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isLetter = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isLetter && !isDigit)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    bool ContainsTextIgnoreCase(
        System.Collections.Generic.List<string> values,
        string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(
                values[index],
                value,
                System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    void ValidateLanguageTagBytes(System.ReadOnlySpan<byte> tag)
    {
        int error = GetLanguageTagError(tag);
        if (error == 0)
        {
            return;
        }
        if (error == 1)
        {
            throw new System.IO.InvalidDataException("A POP3 language tag cannot be empty.");
        }
        if (error == 2)
        {
            throw new System.IO.InvalidDataException("A POP3 language tag has an invalid primary subtag.");
        }
        if (error == 3)
        {
            throw new System.IO.InvalidDataException("A POP3 language tag contains an invalid subtag.");
        }
        if (error == 4)
        {
            throw new System.IO.InvalidDataException("A POP3 language tag contains an invalid character.");
        }
        if (error == 5)
        {
            throw new System.IO.InvalidDataException("A POP3 primary language subtag must contain only letters.");
        }
        throw new System.IO.InvalidDataException("A POP3 language tag contains an invalid final subtag.");
    }

    void ValidateDescription(System.ReadOnlySpan<byte> description)
    {
        Pop3Utf8Validator validator = new Pop3Utf8Validator();
        if (!validator.IsValidUtf8(description))
        {
            throw new System.IO.InvalidDataException("A POP3 language description is not valid UTF-8.");
        }

        int index = 0;
        while (index < description.Length)
        {
            if (description[index] < 32 || description[index] == 127)
            {
                throw new System.IO.InvalidDataException("A POP3 language description contains a control octet.");
            }
            index++;
        }
    }
}
