namespace ttf;

internal enum SfntLocalizedNameMatchKind
{
    ExactLanguage = 0,
    ParentLanguage = 1,
    EnglishFallback = 2,
    FirstValidRecord = 3
}

internal class SfntLocalizedName
{
    int recordIndex;
    SfntNameRecord record;
    SfntDecodedName decodedName;
    string? languageTag;
    string? matchedLanguageRange;
    SfntLocalizedNameMatchKind matchKind;

    internal SfntLocalizedName(
        int recordIndex,
        SfntNameRecord record,
        SfntDecodedName decodedName,
        string? languageTag,
        string? matchedLanguageRange,
        SfntLocalizedNameMatchKind matchKind)
    {
        if (recordIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("recordIndex");
        }

        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        if (decodedName == null)
        {
            throw new System.ArgumentNullException("decodedName");
        }

        if (matchKind < SfntLocalizedNameMatchKind.ExactLanguage ||
            matchKind > SfntLocalizedNameMatchKind.FirstValidRecord)
        {
            throw new System.ArgumentOutOfRangeException("matchKind");
        }

        this.recordIndex = recordIndex;
        this.record = record;
        this.decodedName = decodedName;
        this.languageTag = languageTag;
        this.matchedLanguageRange = matchedLanguageRange;
        this.matchKind = matchKind;
    }

    internal int GetRecordIndex()
    {
        return recordIndex;
    }

    internal SfntNameRecord GetRecord()
    {
        return record;
    }

    internal SfntDecodedName GetDecodedName()
    {
        return decodedName;
    }

    internal string GetValue()
    {
        return decodedName.GetValue();
    }

    internal string? GetLanguageTag()
    {
        return languageTag;
    }

    internal SfntLocalizedNameMatchKind GetMatchKind()
    {
        return matchKind;
    }

    internal string? GetMatchedLanguageRange()
    {
        return matchedLanguageRange;
    }
}

internal static class SfntNameSelector
{
    const int NoRecord = -1;

    internal static SfntLocalizedName? Find(
        SfntNameTable table,
        int nameId,
        string languageTag)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        ValidateNameId(nameId);
        ValidateLanguageTag(languageTag);

        int recordIndex = FindBestExactLanguage(table, nameId, languageTag);

        if (recordIndex != NoRecord)
        {
            return CreateSelection(
                table,
                recordIndex,
                SfntLocalizedNameMatchKind.ExactLanguage,
                languageTag);
        }

        string? parentLanguage = GetParentLanguage(languageTag);

        while (parentLanguage != null)
        {
            recordIndex = FindBestLanguageRange(
                table,
                nameId,
                parentLanguage);

            if (recordIndex != NoRecord)
            {
                return CreateSelection(
                    table,
                    recordIndex,
                    SfntLocalizedNameMatchKind.ParentLanguage,
                    parentLanguage);
            }

            parentLanguage = GetParentLanguage(parentLanguage);
        }

        if (languageTag.IndexOf('-') < 0)
        {
            recordIndex = FindBestLanguageRange(table, nameId, languageTag);

            if (recordIndex != NoRecord)
            {
                return CreateSelection(
                    table,
                    recordIndex,
                    SfntLocalizedNameMatchKind.ParentLanguage,
                    languageTag);
            }
        }

        return SelectEnglishOrFirst(table, nameId);
    }

    internal static SfntLocalizedName? Find(
        SfntNameTable table,
        int nameId)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        ValidateNameId(nameId);
        return SelectEnglishOrFirst(table, nameId);
    }

    static SfntLocalizedName? SelectEnglishOrFirst(
        SfntNameTable table,
        int nameId)
    {
        int recordIndex = FindBestLanguageRange(table, nameId, "en");

        if (recordIndex != NoRecord)
        {
            return CreateSelection(
                table,
                recordIndex,
                SfntLocalizedNameMatchKind.EnglishFallback,
                "en");
        }

        recordIndex = FindFirstValidRecord(table, nameId);

        if (recordIndex == NoRecord)
        {
            return null;
        }

        return CreateSelection(
            table,
            recordIndex,
            SfntLocalizedNameMatchKind.FirstValidRecord,
            null);
    }

    static SfntLocalizedName CreateSelection(
        SfntNameTable table,
        int recordIndex,
        SfntLocalizedNameMatchKind matchKind,
        string? matchedLanguageRange)
    {
        return new SfntLocalizedName(
            recordIndex,
            table.GetRecord(recordIndex),
            table.GetDecodedName(recordIndex),
            table.GetResolvedLanguageTagForRecord(recordIndex),
            matchedLanguageRange,
            matchKind);
    }

    static int FindBestExactLanguage(
        SfntNameTable table,
        int nameId,
        string languageTag)
    {
        int bestIndex = NoRecord;
        int bestPlatformRank = int.MaxValue;
        int bestEncodingRank = int.MaxValue;
        int index = 0;

        while (index < table.GetRecordCount())
        {
            if (IsValidCandidate(table, index, nameId))
            {
                string? candidateLanguage =
                    table.GetResolvedLanguageTagForRecord(index);

                if (candidateLanguage != null && System.String.Equals(
                    candidateLanguage,
                    languageTag,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    SfntNameRecord record = table.GetRecord(index);
                    int platformRank = GetPlatformRank(record.GetPlatformId());
                    int encodingRank = GetEncodingRank(record);

                    if (platformRank < bestPlatformRank ||
                        platformRank == bestPlatformRank &&
                        encodingRank < bestEncodingRank)
                    {
                        bestIndex = index;
                        bestPlatformRank = platformRank;
                        bestEncodingRank = encodingRank;
                    }
                }
            }

            index += 1;
        }

        return bestIndex;
    }

    static int FindBestLanguageRange(
        SfntNameTable table,
        int nameId,
        string languageRange)
    {
        int bestIndex = NoRecord;
        int bestLanguageRank = int.MaxValue;
        int bestPlatformRank = int.MaxValue;
        int bestEncodingRank = int.MaxValue;
        int index = 0;

        while (index < table.GetRecordCount())
        {
            if (IsValidCandidate(table, index, nameId))
            {
                string? candidateLanguage =
                    table.GetResolvedLanguageTagForRecord(index);
                int languageRank = GetLanguageRangeRank(
                    candidateLanguage,
                    languageRange);

                if (languageRank != int.MaxValue)
                {
                    SfntNameRecord record = table.GetRecord(index);
                    int platformRank = GetPlatformRank(record.GetPlatformId());
                    int encodingRank = GetEncodingRank(record);

                    if (platformRank < bestPlatformRank ||
                        platformRank == bestPlatformRank &&
                        languageRank < bestLanguageRank ||
                        platformRank == bestPlatformRank &&
                        languageRank == bestLanguageRank &&
                        encodingRank < bestEncodingRank)
                    {
                        bestIndex = index;
                        bestLanguageRank = languageRank;
                        bestPlatformRank = platformRank;
                        bestEncodingRank = encodingRank;
                    }
                }
            }

            index += 1;
        }

        return bestIndex;
    }

    static int FindFirstValidRecord(SfntNameTable table, int nameId)
    {
        int index = 0;

        while (index < table.GetRecordCount())
        {
            if (IsValidCandidate(table, index, nameId))
            {
                return index;
            }

            index += 1;
        }

        return NoRecord;
    }

    static bool IsValidCandidate(
        SfntNameTable table,
        int recordIndex,
        int nameId)
    {
        SfntNameRecord record = table.GetRecord(recordIndex);

        if (record.GetNameId() != nameId || !record.HasValidLanguage())
        {
            return false;
        }

        return table.GetDecodedName(recordIndex).IsValid();
    }

    static int GetLanguageRangeRank(
        string? candidateLanguage,
        string languageRange)
    {
        if (candidateLanguage == null)
        {
            return int.MaxValue;
        }

        if (System.String.Equals(
            candidateLanguage,
            languageRange,
            System.StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        string prefix = languageRange + "-";

        if (!candidateLanguage.StartsWith(
            prefix,
            System.StringComparison.OrdinalIgnoreCase))
        {
            return int.MaxValue;
        }

        int rank = 1;
        int index = prefix.Length;

        while (index < candidateLanguage.Length)
        {
            if (candidateLanguage[index] == '-')
            {
                rank += 1;
            }

            index += 1;
        }

        return rank;
    }

    static int GetPlatformRank(int platformId)
    {
        if (platformId == 3)
        {
            return 0;
        }

        if (platformId == 0)
        {
            return 1;
        }

        if (platformId == 1)
        {
            return 2;
        }

        return 3;
    }

    static int GetEncodingRank(SfntNameRecord record)
    {
        int platformId = record.GetPlatformId();
        int encodingId = record.GetEncodingId();

        if (platformId == 3)
        {
            if (encodingId == 10)
            {
                return 0;
            }

            if (encodingId == 1)
            {
                return 1;
            }

            if (encodingId == 0)
            {
                return 2;
            }

            return 3;
        }

        if (platformId == 0)
        {
            return 6 - encodingId;
        }

        if (platformId == 1)
        {
            return encodingId == 0 ? 0 : 1;
        }

        return 0;
    }

    static string? GetParentLanguage(string languageTag)
    {
        int separator = languageTag.LastIndexOf('-');

        if (separator < 0)
        {
            return null;
        }

        string parent = languageTag.Substring(0, separator);
        int parentSeparator = parent.LastIndexOf('-');
        int finalSubtagLength = parent.Length - parentSeparator - 1;

        if (finalSubtagLength == 1)
        {
            if (parentSeparator < 0)
            {
                return null;
            }

            parent = parent.Substring(0, parentSeparator);
        }

        return parent;
    }

    static void ValidateNameId(int nameId)
    {
        if (nameId < 0 || nameId > 65535)
        {
            throw new System.ArgumentOutOfRangeException("nameId");
        }
    }

    internal static void ValidateLanguageTag(string languageTag)
    {
        if (languageTag == null)
        {
            throw new System.ArgumentNullException("languageTag");
        }

        if (languageTag.Length == 0 || languageTag.Length > 255)
        {
            throw new System.ArgumentException(
                "The language tag must contain between 1 and 255 characters.",
                "languageTag");
        }

        int subtagLength = 0;
        int index = 0;

        while (index < languageTag.Length)
        {
            char character = languageTag[index];

            if (character == '-')
            {
                if (subtagLength == 0)
                {
                    throw new System.ArgumentException(
                        "The language tag contains an empty subtag.",
                        "languageTag");
                }

                subtagLength = 0;
            }
            else
            {
                bool alphaNumeric = character >= 'A' && character <= 'Z' ||
                    character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9';

                if (!alphaNumeric)
                {
                    throw new System.ArgumentException(
                        "The language tag contains a non-ASCII subtag character.",
                        "languageTag");
                }

                subtagLength += 1;

                if (subtagLength > 8)
                {
                    throw new System.ArgumentException(
                        "The language tag contains a subtag longer than eight characters.",
                        "languageTag");
                }
            }

            index += 1;
        }

        if (subtagLength == 0)
        {
            throw new System.ArgumentException(
                "The language tag contains an empty subtag.",
                "languageTag");
        }
    }
}

internal static class SfntNameLanguageResolver
{
    static readonly System.Globalization.CultureInfo[] cultures =
        System.Globalization.CultureInfo.GetCultures(
            System.Globalization.CultureTypes.AllCultures);

    internal static string? GetLanguageTag(
        SfntNameTable table,
        int recordIndex)
    {
        SfntNameRecord record = table.GetRecord(recordIndex);

        if (!record.HasValidLanguage())
        {
            return null;
        }

        if (record.UsesLanguageTag())
        {
            return table.GetLanguageTagForRecord(recordIndex);
        }

        if (record.GetPlatformId() == 3)
        {
            return GetWindowsLanguageTag(record.GetLanguageId());
        }

        if (record.GetPlatformId() == 1)
        {
            return GetMacintoshLanguageTag(record.GetLanguageId());
        }

        return null;
    }

    static string? GetWindowsLanguageTag(int languageId)
    {
        if (languageId == 0x007F || languageId == 0x1000)
        {
            return null;
        }

        int index = 0;

        while (index < cultures.Length)
        {
            System.Globalization.CultureInfo culture = cultures[index];

            if (culture.LCID == languageId && culture.Name.Length != 0)
            {
                return culture.Name;
            }

            index += 1;
        }

        return null;
    }

    static string? GetMacintoshLanguageTag(int languageId)
    {
        switch (languageId)
        {
            case 0: return "en";
            case 1: return "fr";
            case 2: return "de";
            case 3: return "it";
            case 4: return "nl";
            case 5: return "sv";
            case 6: return "es";
            case 7: return "da";
            case 8: return "pt";
            case 9: return "no";
            case 10: return "he";
            case 11: return "ja";
            case 12: return "ar";
            case 13: return "fi";
            case 14: return "el";
            case 15: return "is";
            case 16: return "mt";
            case 17: return "tr";
            case 18: return "hr";
            case 19: return "zh-Hant";
            case 20: return "ur";
            case 21: return "hi";
            case 22: return "th";
            case 23: return "ko";
            case 24: return "lt";
            case 25: return "pl";
            case 26: return "hu";
            case 27: return "et";
            case 28: return "lv";
            case 29: return "se";
            case 30: return "fo";
            case 31: return "fa";
            case 32: return "ru";
            case 33: return "zh-Hans";
            case 34: return "nl-BE";
            case 35: return "ga";
            case 36: return "sq";
            case 37: return "ro";
            case 38: return "cs";
            case 39: return "sk";
            case 40: return "sl";
            case 41: return "yi";
            case 42: return "sr";
            case 43: return "mk";
            case 44: return "bg";
            case 45: return "uk";
            case 46: return "be";
            case 47: return "uz";
            case 48: return "kk";
            case 49: return "az-Cyrl";
            case 50: return "az-Arab";
            case 51: return "hy";
            case 52: return "ka";
            case 53: return "ro-MD";
            case 54: return "ky";
            case 55: return "tg";
            case 56: return "tk";
            case 57: return "mn-Mong";
            case 58: return "mn-Cyrl";
            case 59: return "ps";
            case 60: return "ku";
            case 61: return "ks";
            case 62: return "sd";
            case 63: return "bo";
            case 64: return "ne";
            case 65: return "sa";
            case 66: return "mr";
            case 67: return "bn";
            case 68: return "as";
            case 69: return "gu";
            case 70: return "pa";
            case 71: return "or";
            case 72: return "ml";
            case 73: return "kn";
            case 74: return "ta";
            case 75: return "te";
            case 76: return "si";
            case 77: return "my";
            case 78: return "km";
            case 79: return "lo";
            case 80: return "vi";
            case 81: return "id";
            case 82: return "tl";
            case 83: return "ms";
            case 84: return "ms-Arab";
            case 85: return "am";
            case 86: return "ti";
            case 87: return "om";
            case 88: return "so";
            case 89: return "sw";
            case 90: return "rw";
            case 91: return "rn";
            case 92: return "ny";
            case 93: return "mg";
            case 94: return "eo";
            case 128: return "cy";
            case 129: return "eu";
            case 130: return "ca";
            case 131: return "la";
            case 132: return "qu";
            case 133: return "gn";
            case 134: return "ay";
            case 135: return "tt";
            case 136: return "ug";
            case 137: return "dz";
            case 138: return "jv";
            case 139: return "su";
            case 140: return "gl";
            case 141: return "af";
            case 142: return "br";
            case 143: return "iu";
            case 144: return "gd";
            case 145: return "gv";
            case 146: return "ga";
            case 147: return "to";
            case 148: return "el-polyton";
            case 149: return "kl";
            case 150: return "az-Latn";
            case 151: return "nn";
            default: return null;
        }
    }
}
