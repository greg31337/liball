namespace ttf;

internal class Bcp47LanguageTag
{
    string canonicalTag;
    string language;
    string? script;
    string? region;
    string[] extendedLanguages;
    string[] variants;
    string[] extensions;
    string[] privateUse;
    string[] fallbacks;

    internal Bcp47LanguageTag(
        string canonicalTag,
        string language,
        string? script,
        string? region,
        string[] extendedLanguages,
        string[] variants,
        string[] extensions,
        string[] privateUse,
        string[] fallbacks)
    {
        this.canonicalTag = canonicalTag;
        this.language = language;
        this.script = script;
        this.region = region;
        this.extendedLanguages = extendedLanguages;
        this.variants = variants;
        this.extensions = extensions;
        this.privateUse = privateUse;
        this.fallbacks = fallbacks;
    }

    internal string GetCanonicalTag()
    {
        return canonicalTag;
    }

    internal string GetLanguage()
    {
        return language;
    }

    internal string? GetScript()
    {
        return script;
    }

    internal string? GetRegion()
    {
        return region;
    }

    internal int GetExtendedLanguageCount()
    {
        return extendedLanguages.Length;
    }

    internal string GetExtendedLanguage(int index)
    {
        ValidateIndex(index, extendedLanguages.Length, "index");
        return extendedLanguages[index];
    }

    internal int GetVariantCount()
    {
        return variants.Length;
    }

    internal string GetVariant(int index)
    {
        ValidateIndex(index, variants.Length, "index");
        return variants[index];
    }

    internal bool HasVariant(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        return Contains(variants, value.ToLowerInvariant());
    }

    internal int GetExtensionCount()
    {
        return extensions.Length;
    }

    internal string GetExtension(int index)
    {
        ValidateIndex(index, extensions.Length, "index");
        return extensions[index];
    }

    internal int GetPrivateUseCount()
    {
        return privateUse.Length;
    }

    internal string GetPrivateUse(int index)
    {
        ValidateIndex(index, privateUse.Length, "index");
        return privateUse[index];
    }

    internal int GetFallbackCount()
    {
        return fallbacks.Length;
    }

    internal string GetFallback(int index)
    {
        ValidateIndex(index, fallbacks.Length, "index");
        return fallbacks[index];
    }

    static bool Contains(string[] values, string value)
    {
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidateIndex(int index, int count, string parameterName)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal static class Bcp47LanguageTagParser
{
    static readonly string[] grandfathered = new string[]
    {
        "art-lojban", "cel-gaulish", "en-gb-oed", "i-ami", "i-bnn",
        "i-default", "i-enochian", "i-hak", "i-klingon", "i-lux",
        "i-mingo", "i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu",
        "no-bok", "no-nyn", "sgn-be-fr", "sgn-be-nl", "sgn-ch-de",
        "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang"
    };

    internal static Bcp47LanguageTag Parse(string languageTag)
    {
        if (languageTag == null)
        {
            throw new System.ArgumentNullException("languageTag");
        }

        if (languageTag.Length == 0 || languageTag.Length > 255)
        {
            throw InvalidTag();
        }

        string lowerTag = languageTag.ToLowerInvariant();
        int grandfatheredIndex = Find(grandfathered, lowerTag);

        if (grandfatheredIndex >= 0)
        {
            string? preferred = GetGrandfatheredPreferred(lowerTag);

            if (preferred != null)
            {
                return Parse(preferred);
            }

            return new Bcp47LanguageTag(
                lowerTag,
                lowerTag,
                null,
                null,
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[] { lowerTag });
        }

        string[] subtags = languageTag.Split('-');
        ValidateSubtags(subtags);

        if (subtags[0].Length == 1 && IsEqual(subtags[0], "x"))
        {
            return ParsePrivateUseOnly(subtags);
        }

        return ParseNormal(subtags);
    }

    static Bcp47LanguageTag ParsePrivateUseOnly(string[] subtags)
    {
        if (subtags.Length < 2)
        {
            throw InvalidTag();
        }

        string[] privateUse = new string[subtags.Length - 1];
        System.Text.StringBuilder canonical =
            new System.Text.StringBuilder("x");
        int index = 1;

        while (index < subtags.Length)
        {
            string value = subtags[index].ToLowerInvariant();
            privateUse[index - 1] = value;
            canonical.Append('-');
            canonical.Append(value);
            index++;
        }

        string canonicalTag = canonical.ToString();
        return new Bcp47LanguageTag(
            canonicalTag,
            "x",
            null,
            null,
            new string[0],
            new string[0],
            new string[0],
            privateUse,
            CreateFallbacks(canonicalTag));
    }

    static Bcp47LanguageTag ParseNormal(string[] subtags)
    {
        string language = subtags[0].ToLowerInvariant();

        if (!IsAlpha(language) || language.Length < 2 || language.Length > 8)
        {
            throw InvalidTag();
        }

        language = GetPreferredLanguage(language);
        string[] extended = new string[3];
        string[] variants = new string[subtags.Length];
        string[] extensions = new string[subtags.Length];
        string[] privateUse = new string[subtags.Length];
        int extendedCount = 0;
        int variantCount = 0;
        int extensionCount = 0;
        int privateCount = 0;
        int index = 1;

        if (subtags[0].Length <= 3)
        {
            while (index < subtags.Length && extendedCount < 3 &&
                subtags[index].Length == 3 && IsAlpha(subtags[index]))
            {
                extended[extendedCount] = subtags[index].ToLowerInvariant();
                extendedCount++;
                index++;
            }
        }

        string? script = null;

        if (index < subtags.Length && subtags[index].Length == 4 &&
            IsAlpha(subtags[index]))
        {
            script = ToScriptCase(subtags[index]);
            index++;
        }

        string? region = null;

        if (index < subtags.Length &&
            (subtags[index].Length == 2 && IsAlpha(subtags[index]) ||
                subtags[index].Length == 3 && IsDigits(subtags[index])))
        {
            region = subtags[index].ToUpperInvariant();
            index++;
        }

        while (index < subtags.Length && IsVariant(subtags[index]))
        {
            string value = subtags[index].ToLowerInvariant();

            if (Contains(variants, variantCount, value))
            {
                throw InvalidTag();
            }

            variants[variantCount] = value;
            variantCount++;
            index++;
        }

        while (index < subtags.Length && IsExtensionSingleton(subtags[index]))
        {
            string singleton = subtags[index].ToLowerInvariant();

            if (HasExtensionSingleton(extensions, extensionCount, singleton))
            {
                throw InvalidTag();
            }

            index++;
            int extensionStart = index;
            System.Text.StringBuilder extension =
                new System.Text.StringBuilder(singleton);

            while (index < subtags.Length && subtags[index].Length >= 2 &&
                subtags[index].Length <= 8)
            {
                extension.Append('-');
                extension.Append(subtags[index].ToLowerInvariant());
                index++;
            }

            if (index == extensionStart)
            {
                throw InvalidTag();
            }

            extensions[extensionCount] = extension.ToString();
            extensionCount++;
        }

        SortStrings(extensions, extensionCount);

        if (index < subtags.Length && IsEqual(subtags[index], "x"))
        {
            index++;

            if (index == subtags.Length)
            {
                throw InvalidTag();
            }

            while (index < subtags.Length)
            {
                privateUse[privateCount] = subtags[index].ToLowerInvariant();
                privateCount++;
                index++;
            }
        }

        if (index != subtags.Length)
        {
            throw InvalidTag();
        }

        string[] finalExtended = Copy(extended, extendedCount);
        string[] finalVariants = Copy(variants, variantCount);
        string[] finalExtensions = Copy(extensions, extensionCount);
        string[] finalPrivateUse = Copy(privateUse, privateCount);
        string canonicalTag = BuildCanonical(
            language,
            finalExtended,
            script,
            region,
            finalVariants,
            finalExtensions,
            finalPrivateUse);
        return new Bcp47LanguageTag(
            canonicalTag,
            language,
            script,
            region,
            finalExtended,
            finalVariants,
            finalExtensions,
            finalPrivateUse,
            CreateFallbacks(canonicalTag));
    }

    static string BuildCanonical(
        string language,
        string[] extended,
        string? script,
        string? region,
        string[] variants,
        string[] extensions,
        string[] privateUse)
    {
        System.Text.StringBuilder value =
            new System.Text.StringBuilder(language);
        AppendAll(value, extended);

        if (script != null)
        {
            value.Append('-');
            value.Append(script);
        }

        if (region != null)
        {
            value.Append('-');
            value.Append(region);
        }

        AppendAll(value, variants);
        AppendAll(value, extensions);

        if (privateUse.Length != 0)
        {
            value.Append("-x");
            AppendAll(value, privateUse);
        }

        return value.ToString();
    }

    static void AppendAll(System.Text.StringBuilder value, string[] subtags)
    {
        int index = 0;

        while (index < subtags.Length)
        {
            value.Append('-');
            value.Append(subtags[index]);
            index++;
        }
    }

    static string[] CreateFallbacks(string canonicalTag)
    {
        string[] values = new string[canonicalTag.Length / 2 + 1];
        int count = 0;
        string current = canonicalTag;

        while (current.Length != 0)
        {
            values[count] = current;
            count++;
            int separator = current.LastIndexOf('-');

            if (separator < 0)
            {
                current = "";
            }
            else
            {
                current = current.Substring(0, separator);
                int previousSeparator = current.LastIndexOf('-');
                int finalLength = current.Length - previousSeparator - 1;

                if (finalLength == 1)
                {
                    current = previousSeparator < 0
                        ? ""
                        : current.Substring(0, previousSeparator);
                }
            }
        }

        return Copy(values, count);
    }

    static void ValidateSubtags(string[] subtags)
    {
        int index = 0;

        while (index < subtags.Length)
        {
            if (subtags[index].Length == 0 || subtags[index].Length > 8 ||
                !IsAlphaNumeric(subtags[index]))
            {
                throw InvalidTag();
            }

            index++;
        }
    }

    static bool IsVariant(string value)
    {
        return value.Length >= 5 && value.Length <= 8 ||
            value.Length == 4 && IsDigit(value[0]);
    }

    static bool IsExtensionSingleton(string value)
    {
        return value.Length == 1 && !IsEqual(value, "x");
    }

    static bool HasExtensionSingleton(
        string[] values,
        int count,
        string singleton)
    {
        int index = 0;

        while (index < count)
        {
            if (values[index][0] == singleton[0])
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool Contains(string[] values, int count, string value)
    {
        int index = 0;

        while (index < count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void SortStrings(string[] values, int count)
    {
        int index = 1;

        while (index < count)
        {
            string value = values[index];
            int target = index;

            while (target > 0 && System.String.CompareOrdinal(
                values[target - 1], value) > 0)
            {
                values[target] = values[target - 1];
                target--;
            }

            values[target] = value;
            index++;
        }
    }

    static string[] Copy(string[] values, int count)
    {
        string[] result = new string[count];
        System.Array.Copy(values, result, count);
        return result;
    }

    static bool IsAlpha(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            char character = value[index];

            if (character < 'A' || character > 'Z' &&
                character < 'a' || character > 'z')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsDigits(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            if (!IsDigit(value[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsAlphaNumeric(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z' ||
                IsDigit(character);

            if (!valid)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static string ToScriptCase(string value)
    {
        return char.ToUpperInvariant(value[0]) +
            value.Substring(1).ToLowerInvariant();
    }

    static string GetPreferredLanguage(string language)
    {
        if (language == "iw") return "he";
        if (language == "ji") return "yi";
        if (language == "in") return "id";
        if (language == "mo") return "ro";
        return language;
    }

    static string? GetGrandfatheredPreferred(string value)
    {
        if (value == "art-lojban") return "jbo";
        if (value == "en-gb-oed") return "en-GB-oxendict";
        if (value == "i-ami") return "ami";
        if (value == "i-bnn") return "bnn";
        if (value == "i-hak") return "hak";
        if (value == "i-klingon") return "tlh";
        if (value == "i-lux") return "lb";
        if (value == "i-navajo") return "nv";
        if (value == "i-pwn") return "pwn";
        if (value == "i-tao") return "tao";
        if (value == "i-tay") return "tay";
        if (value == "i-tsu") return "tsu";
        if (value == "no-bok") return "nb";
        if (value == "no-nyn") return "nn";
        if (value == "sgn-be-fr") return "sfb";
        if (value == "sgn-be-nl") return "vgt";
        if (value == "sgn-ch-de") return "sgg";
        if (value == "zh-guoyu") return "cmn";
        if (value == "zh-hakka") return "hak";
        if (value == "zh-min-nan") return "nan";
        if (value == "zh-xiang") return "hsn";
        return null;
    }

    static int Find(string[] values, string value)
    {
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static bool IsEqual(string first, string second)
    {
        return System.String.Equals(
            first,
            second,
            System.StringComparison.OrdinalIgnoreCase);
    }

    static System.ArgumentException InvalidTag()
    {
        return new System.ArgumentException(
            "The language tag is not well-formed BCP 47.",
            "languageTag");
    }
}

internal class OpenTypeLanguageTagResolution
{
    Bcp47LanguageTag languageTag;
    int[] languageSystemTags;

    internal OpenTypeLanguageTagResolution(
        Bcp47LanguageTag languageTag,
        int[] languageSystemTags)
    {
        this.languageTag = languageTag;
        this.languageSystemTags = languageSystemTags;
    }

    internal Bcp47LanguageTag GetLanguageTag()
    {
        return languageTag;
    }

    internal int GetLanguageSystemTagCount()
    {
        return languageSystemTags.Length;
    }

    internal int GetLanguageSystemTag(int index)
    {
        if (index < 0 || index >= languageSystemTags.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return languageSystemTags[index];
    }
}

internal class OpenTypeLanguageTagData
{
    string[] keys;
    int[] tags;

    internal OpenTypeLanguageTagData(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (bytes.Length < 12 || bytes[0] != 0x54 || bytes[1] != 0x54 ||
            bytes[2] != 0x46 || bytes[3] != 0x4C || bytes[4] != 0x4E ||
            bytes[5] != 0x47 || bytes[6] != 0 || bytes[7] != 0 ||
            ReadUInt16(bytes, 8) != 1)
        {
            throw FontFormatException.Create(
                "The embedded language tag map header is invalid.");
        }

        int count = ReadUInt16(bytes, 10);

        if (bytes.Length != 12 + count * 8)
        {
            throw FontFormatException.Create(
                "The embedded language tag map length is invalid.");
        }

        keys = new string[count];
        tags = new int[count];
        string previous = "";
        int index = 0;

        while (index < count)
        {
            int offset = 12 + index * 8;
            int keyLength = bytes[offset];

            if (keyLength < 2 || keyLength > 3)
            {
                throw FontFormatException.Create(
                    "The embedded language tag map contains an invalid key.");
            }

            string key = System.Text.Encoding.ASCII.GetString(
                bytes,
                offset + 1,
                keyLength);

            if (index != 0 && System.String.CompareOrdinal(previous, key) >= 0)
            {
                throw FontFormatException.Create(
                    "The embedded language tag map is not ordered.");
            }

            keys[index] = key;
            tags[index] = ReadInt32(bytes, offset + 4);
            previous = key;
            index++;
        }
    }

    internal static OpenTypeLanguageTagData LoadDefault()
    {
        System.Reflection.Assembly assembly =
            System.Reflection.Assembly.GetExecutingAssembly();
        using (System.IO.Stream? stream = assembly.GetManifestResourceStream(
            "ttf.LanguageTagMap.bin"))
        {
            if (stream == null)
            {
                throw FontFormatException.Create(
                    "The embedded language tag map resource is missing.");
            }

            using (System.IO.MemoryStream output = new System.IO.MemoryStream())
            {
                stream.CopyTo(output);
                return new OpenTypeLanguageTagData(output.ToArray());
            }
        }
    }

    internal int Find(string key)
    {
        int low = 0;
        int high = keys.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = System.String.CompareOrdinal(keys[middle], key);

            if (comparison == 0)
            {
                return tags[middle];
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return 0;
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static int ReadInt32(byte[] bytes, int offset)
    {
        return bytes[offset] << 24 |
            bytes[offset + 1] << 16 |
            bytes[offset + 2] << 8 |
            bytes[offset + 3];
    }
}

internal static class OpenTypeLanguageTagMap
{
    static readonly OpenTypeLanguageTagData data = OpenTypeLanguageTagData.LoadDefault();

    internal static OpenTypeLanguageTagResolution Resolve(string languageTag)
    {
        Bcp47LanguageTag parsed = Bcp47LanguageTagParser.Parse(languageTag);
        int[] candidates = new int[10];
        int count = 0;
        count = AddSpecialCandidates(parsed, candidates, count);

        if (parsed.GetExtendedLanguageCount() != 0)
        {
            count = AddCandidate(
                candidates,
                count,
                data.Find(parsed.GetExtendedLanguage(0)));
        }

        count = AddCandidate(
            candidates,
            count,
            data.Find(parsed.GetLanguage()));
        int[] result = new int[count];
        System.Array.Copy(candidates, result, count);
        return new OpenTypeLanguageTagResolution(parsed, result);
    }

    static int AddSpecialCandidates(
        Bcp47LanguageTag parsed,
        int[] candidates,
        int count)
    {
        if (parsed.HasVariant("fonnapa"))
        {
            count = AddCandidate(candidates, count, Tag("APPH"));
        }

        if (parsed.HasVariant("fonipa"))
        {
            count = AddCandidate(candidates, count, Tag("IPPH"));
        }

        if (parsed.HasVariant("fonupa"))
        {
            count = AddCandidate(candidates, count, Tag("UPPH"));
        }

        string? script = parsed.GetScript();

        if (script == "Syre") count = AddCandidate(candidates, count, Tag("SYRE"));
        if (script == "Syrj") count = AddCandidate(candidates, count, Tag("SYRJ"));
        if (script == "Syrn") count = AddCandidate(candidates, count, Tag("SYRN"));

        string language = parsed.GetLanguage();

        if (language == "zh" || language == "zho")
        {
            string? region = parsed.GetRegion();

            if (script == "Bopo")
            {
                count = AddCandidate(candidates, count, Tag("ZHP "));
            }
            else if (script == "Hant" && region == "HK")
            {
                count = AddCandidate(candidates, count, Tag("ZHH "));
                count = AddCandidate(candidates, count, Tag("ZHT "));
            }
            else if (script == "Hant" && region == "MO")
            {
                count = AddCandidate(candidates, count, Tag("ZHTM"));
                count = AddCandidate(candidates, count, Tag("ZHT "));
            }
            else if (script == "Hant")
            {
                count = AddCandidate(candidates, count, Tag("ZHT "));
            }
            else
            {
                count = AddCandidate(candidates, count, Tag("ZHS "));
            }
        }

        if ((language == "ro" || language == "ron") &&
            parsed.GetRegion() == "MD")
        {
            count = AddCandidate(candidates, count, Tag("MOL "));
        }

        return count;
    }

    static int AddCandidate(int[] candidates, int count, int tag)
    {
        if (tag == 0)
        {
            return count;
        }

        int index = 0;

        while (index < count)
        {
            if (candidates[index] == tag)
            {
                return count;
            }

            index++;
        }

        candidates[count] = tag;
        return count + 1;
    }

    static int Tag(string value)
    {
        return value[0] << 24 |
            value[1] << 16 |
            value[2] << 8 |
            value[3];
    }
}
