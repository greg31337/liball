using System.Globalization;

namespace cbor;

internal static class CddlXsdUnicode
{
    internal static bool IsXmlCharacter(int codePoint)
    {
        return codePoint == 0x09
            || codePoint == 0x0a
            || codePoint == 0x0d
            || (codePoint >= 0x20 && codePoint <= 0xd7ff)
            || (codePoint >= 0xe000 && codePoint <= 0xfffd)
            || (codePoint >= 0x10000 && codePoint <= 0x10ffff);
    }

    internal static bool MatchesEscape(string name, int codePoint)
    {
        if (name == "s")
        {
            return codePoint == 0x20
                || codePoint == 0x09
                || codePoint == 0x0a
                || codePoint == 0x0d;
        }
        if (name == "d")
        {
            return GetCategory(codePoint) == UnicodeCategory.DecimalDigitNumber;
        }
        if (name == "w")
        {
            UnicodeCategory category = GetCategory(codePoint);
            return IsLetter(category)
                || IsMark(category)
                || IsNumber(category)
                || IsSymbol(category);
        }
        if (name == "i")
        {
            return codePoint == 0x5f
                || codePoint == 0x3a
                || IsLetter(GetCategory(codePoint));
        }
        if (name == "c")
        {
            UnicodeCategory category = GetCategory(codePoint);
            return codePoint == 0x2d
                || codePoint == 0x2e
                || codePoint == 0x3a
                || codePoint == 0x5f
                || IsLetter(category)
                || IsMark(category)
                || IsNumber(category);
        }
        if (name.Length > 2 && name.StartsWith("Is", System.StringComparison.Ordinal))
        {
            return IsBlock(name, codePoint);
        }
        return MatchesCategory(name, GetCategory(codePoint));
    }

    internal static bool IsKnownCategory(string name)
    {
        if (name == "L" || name == "Lu" || name == "Ll"
            || name == "Lt" || name == "Lm" || name == "Lo"
            || name == "M" || name == "Mn" || name == "Mc"
            || name == "Me" || name == "N" || name == "Nd"
            || name == "Nl" || name == "No" || name == "P"
            || name == "Pc" || name == "Pd" || name == "Ps"
            || name == "Pe" || name == "Pi" || name == "Pf"
            || name == "Po" || name == "Z" || name == "Zs"
            || name == "Zl" || name == "Zp" || name == "S"
            || name == "Sm" || name == "Sc" || name == "Sk"
            || name == "So" || name == "C" || name == "Cc"
            || name == "Cf" || name == "Co" || name == "Cn")
        {
            return true;
        }
        return IsKnownBlock(name);
    }

    internal static bool MatchesCategory(
        string name,
        UnicodeCategory category)
    {
        if (name == "L")
        {
            return IsLetter(category);
        }
        if (name == "M")
        {
            return IsMark(category);
        }
        if (name == "N")
        {
            return IsNumber(category);
        }
        if (name == "P")
        {
            return IsPunctuation(category);
        }
        if (name == "Z")
        {
            return IsSeparator(category);
        }
        if (name == "S")
        {
            return IsSymbol(category);
        }
        if (name == "C")
        {
            return IsOther(category);
        }
        return name == CategoryName(category);
    }

    internal static string CategoryName(UnicodeCategory category)
    {
        switch (category)
        {
            case UnicodeCategory.UppercaseLetter: return "Lu";
            case UnicodeCategory.LowercaseLetter: return "Ll";
            case UnicodeCategory.TitlecaseLetter: return "Lt";
            case UnicodeCategory.ModifierLetter: return "Lm";
            case UnicodeCategory.OtherLetter: return "Lo";
            case UnicodeCategory.NonSpacingMark: return "Mn";
            case UnicodeCategory.SpacingCombiningMark: return "Mc";
            case UnicodeCategory.EnclosingMark: return "Me";
            case UnicodeCategory.DecimalDigitNumber: return "Nd";
            case UnicodeCategory.LetterNumber: return "Nl";
            case UnicodeCategory.OtherNumber: return "No";
            case UnicodeCategory.ConnectorPunctuation: return "Pc";
            case UnicodeCategory.DashPunctuation: return "Pd";
            case UnicodeCategory.OpenPunctuation: return "Ps";
            case UnicodeCategory.ClosePunctuation: return "Pe";
            case UnicodeCategory.InitialQuotePunctuation: return "Pi";
            case UnicodeCategory.FinalQuotePunctuation: return "Pf";
            case UnicodeCategory.OtherPunctuation: return "Po";
            case UnicodeCategory.SpaceSeparator: return "Zs";
            case UnicodeCategory.LineSeparator: return "Zl";
            case UnicodeCategory.ParagraphSeparator: return "Zp";
            case UnicodeCategory.MathSymbol: return "Sm";
            case UnicodeCategory.CurrencySymbol: return "Sc";
            case UnicodeCategory.ModifierSymbol: return "Sk";
            case UnicodeCategory.OtherSymbol: return "So";
            case UnicodeCategory.Control: return "Cc";
            case UnicodeCategory.Format: return "Cf";
            case UnicodeCategory.PrivateUse: return "Co";
            default: return "Cn";
        }
    }

    internal static UnicodeCategory GetCategory(int codePoint)
    {
        string value = char.ConvertFromUtf32(codePoint);
        return CharUnicodeInfo.GetUnicodeCategory(value, 0);
    }

    internal static bool IsLetter(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter
            || category == UnicodeCategory.LowercaseLetter
            || category == UnicodeCategory.TitlecaseLetter
            || category == UnicodeCategory.ModifierLetter
            || category == UnicodeCategory.OtherLetter;
    }

    internal static bool IsMark(UnicodeCategory category)
    {
        return category == UnicodeCategory.NonSpacingMark
            || category == UnicodeCategory.SpacingCombiningMark
            || category == UnicodeCategory.EnclosingMark;
    }

    internal static bool IsNumber(UnicodeCategory category)
    {
        return category == UnicodeCategory.DecimalDigitNumber
            || category == UnicodeCategory.LetterNumber
            || category == UnicodeCategory.OtherNumber;
    }

    internal static bool IsPunctuation(UnicodeCategory category)
    {
        return category == UnicodeCategory.ConnectorPunctuation
            || category == UnicodeCategory.DashPunctuation
            || category == UnicodeCategory.OpenPunctuation
            || category == UnicodeCategory.ClosePunctuation
            || category == UnicodeCategory.InitialQuotePunctuation
            || category == UnicodeCategory.FinalQuotePunctuation
            || category == UnicodeCategory.OtherPunctuation;
    }

    internal static bool IsSeparator(UnicodeCategory category)
    {
        return category == UnicodeCategory.SpaceSeparator
            || category == UnicodeCategory.LineSeparator
            || category == UnicodeCategory.ParagraphSeparator;
    }

    internal static bool IsSymbol(UnicodeCategory category)
    {
        return category == UnicodeCategory.MathSymbol
            || category == UnicodeCategory.CurrencySymbol
            || category == UnicodeCategory.ModifierSymbol
            || category == UnicodeCategory.OtherSymbol;
    }

    internal static bool IsOther(UnicodeCategory category)
    {
        return category == UnicodeCategory.Control
            || category == UnicodeCategory.Format
            || category == UnicodeCategory.Surrogate
            || category == UnicodeCategory.PrivateUse
            || category == UnicodeCategory.OtherNotAssigned;
    }

    internal static bool IsKnownBlock(string name)
    {
        return name == "IsBasicLatin"
            || name == "IsLatin-1Supplement"
            || name == "IsLatinExtended-A"
            || name == "IsLatinExtended-B"
            || name == "IsIPAExtensions"
            || name == "IsSpacingModifierLetters"
            || name == "IsCombiningDiacriticalMarks"
            || name == "IsGreek"
            || name == "IsCyrillic"
            || name == "IsArmenian"
            || name == "IsHebrew"
            || name == "IsArabic"
            || name == "IsDevanagari"
            || name == "IsBengali"
            || name == "IsGurmukhi"
            || name == "IsGujarati"
            || name == "IsOriya"
            || name == "IsTamil"
            || name == "IsTelugu"
            || name == "IsKannada"
            || name == "IsMalayalam"
            || name == "IsThai"
            || name == "IsLao"
            || name == "IsTibetan"
            || name == "IsGeorgian"
            || name == "IsHangulJamo"
            || name == "IsLatinExtendedAdditional"
            || name == "IsGreekExtended"
            || name == "IsGeneralPunctuation"
            || name == "IsSuperscriptsandSubscripts"
            || name == "IsCurrencySymbols"
            || name == "IsCombiningMarksforSymbols"
            || name == "IsLetterlikeSymbols"
            || name == "IsNumberForms"
            || name == "IsArrows"
            || name == "IsMathematicalOperators"
            || name == "IsMiscellaneousTechnical"
            || name == "IsControlPictures"
            || name == "IsOpticalCharacterRecognition"
            || name == "IsEnclosedAlphanumerics"
            || name == "IsBoxDrawing"
            || name == "IsBlockElements"
            || name == "IsGeometricShapes"
            || name == "IsMiscellaneousSymbols"
            || name == "IsDingbats"
            || name == "IsCJKSymbolsandPunctuation"
            || name == "IsHiragana"
            || name == "IsKatakana"
            || name == "IsBopomofo"
            || name == "IsHangulCompatibilityJamo"
            || name == "IsCJKUnifiedIdeographs"
            || name == "IsHangulSyllables"
            || name == "IsHighSurrogates"
            || name == "IsLowSurrogates"
            || name == "IsPrivateUse"
            || name == "IsCJKCompatibilityIdeographs"
            || name == "IsAlphabeticPresentationForms"
            || name == "IsArabicPresentationForms-A"
            || name == "IsCombiningHalfMarks"
            || name == "IsCJKCompatibilityForms"
            || name == "IsSmallFormVariants"
            || name == "IsArabicPresentationForms-B"
            || name == "IsHalfwidthandFullwidthForms"
            || name == "IsSpecials";
    }

    internal static bool IsBlock(string name, int codePoint)
    {
        int start = FindBlockStart(name);
        int end = FindBlockEnd(name);
        return start >= 0 && codePoint >= start && codePoint <= end;
    }

    internal static int FindBlockStart(string name)
    {
        switch (name)
        {
            case "IsBasicLatin": return 0x0000;
            case "IsLatin-1Supplement": return 0x0080;
            case "IsLatinExtended-A": return 0x0100;
            case "IsLatinExtended-B": return 0x0180;
            case "IsIPAExtensions": return 0x0250;
            case "IsSpacingModifierLetters": return 0x02b0;
            case "IsCombiningDiacriticalMarks": return 0x0300;
            case "IsGreek": return 0x0370;
            case "IsCyrillic": return 0x0400;
            case "IsArmenian": return 0x0530;
            case "IsHebrew": return 0x0590;
            case "IsArabic": return 0x0600;
            case "IsDevanagari": return 0x0900;
            case "IsBengali": return 0x0980;
            case "IsGurmukhi": return 0x0a00;
            case "IsGujarati": return 0x0a80;
            case "IsOriya": return 0x0b00;
            case "IsTamil": return 0x0b80;
            case "IsTelugu": return 0x0c00;
            case "IsKannada": return 0x0c80;
            case "IsMalayalam": return 0x0d00;
            case "IsThai": return 0x0e00;
            case "IsLao": return 0x0e80;
            case "IsTibetan": return 0x0f00;
            case "IsGeorgian": return 0x10a0;
            case "IsHangulJamo": return 0x1100;
            case "IsLatinExtendedAdditional": return 0x1e00;
            case "IsGreekExtended": return 0x1f00;
            case "IsGeneralPunctuation": return 0x2000;
            case "IsSuperscriptsandSubscripts": return 0x2070;
            case "IsCurrencySymbols": return 0x20a0;
            case "IsCombiningMarksforSymbols": return 0x20d0;
            case "IsLetterlikeSymbols": return 0x2100;
            case "IsNumberForms": return 0x2150;
            case "IsArrows": return 0x2190;
            case "IsMathematicalOperators": return 0x2200;
            case "IsMiscellaneousTechnical": return 0x2300;
            case "IsControlPictures": return 0x2400;
            case "IsOpticalCharacterRecognition": return 0x2440;
            case "IsEnclosedAlphanumerics": return 0x2460;
            case "IsBoxDrawing": return 0x2500;
            case "IsBlockElements": return 0x2580;
            case "IsGeometricShapes": return 0x25a0;
            case "IsMiscellaneousSymbols": return 0x2600;
            case "IsDingbats": return 0x2700;
            case "IsCJKSymbolsandPunctuation": return 0x3000;
            case "IsHiragana": return 0x3040;
            case "IsKatakana": return 0x30a0;
            case "IsBopomofo": return 0x3100;
            case "IsHangulCompatibilityJamo": return 0x3130;
            case "IsCJKUnifiedIdeographs": return 0x4e00;
            case "IsHangulSyllables": return 0xac00;
            case "IsHighSurrogates": return 0xd800;
            case "IsPrivateUse": return 0xe000;
            case "IsCJKCompatibilityIdeographs": return 0xf900;
            case "IsAlphabeticPresentationForms": return 0xfb00;
            case "IsArabicPresentationForms-A": return 0xfb50;
            case "IsCombiningHalfMarks": return 0xfe20;
            case "IsCJKCompatibilityForms": return 0xfe30;
            case "IsSmallFormVariants": return 0xfe50;
            case "IsArabicPresentationForms-B": return 0xfe70;
            case "IsHalfwidthandFullwidthForms": return 0xff00;
            case "IsSpecials": return 0xfff0;
            case "IsLowSurrogates": return 0xdc00;
            default: return -1;
        }
    }

    internal static int FindBlockEnd(string name)
    {
        int start = FindBlockStart(name);
        if (start < 0)
        {
            return -1;
        }
        switch (name)
        {
            case "IsBasicLatin": return 0x007f;
            case "IsLatin-1Supplement": return 0x00ff;
            case "IsLatinExtended-A": return 0x017f;
            case "IsLatinExtended-B": return 0x024f;
            case "IsIPAExtensions": return 0x02af;
            case "IsSpacingModifierLetters": return 0x02ff;
            case "IsCombiningDiacriticalMarks": return 0x036f;
            case "IsGreek": return 0x03ff;
            case "IsCyrillic": return 0x04ff;
            case "IsArmenian": return 0x058f;
            case "IsHebrew": return 0x05ff;
            case "IsArabic": return 0x06ff;
            case "IsDevanagari": return 0x097f;
            case "IsBengali": return 0x09ff;
            case "IsGurmukhi": return 0x0a7f;
            case "IsGujarati": return 0x0aff;
            case "IsOriya": return 0x0b7f;
            case "IsTamil": return 0x0bff;
            case "IsTelugu": return 0x0c7f;
            case "IsKannada": return 0x0cff;
            case "IsMalayalam": return 0x0d7f;
            case "IsThai": return 0x0e7f;
            case "IsLao": return 0x0eff;
            case "IsTibetan": return 0x0fff;
            case "IsGeorgian": return 0x10ff;
            case "IsHangulJamo": return 0x11ff;
            case "IsLatinExtendedAdditional": return 0x1eff;
            case "IsGreekExtended": return 0x1fff;
            case "IsGeneralPunctuation": return 0x206f;
            case "IsSuperscriptsandSubscripts": return 0x209f;
            case "IsCurrencySymbols": return 0x20cf;
            case "IsCombiningMarksforSymbols": return 0x20ff;
            case "IsLetterlikeSymbols": return 0x214f;
            case "IsNumberForms": return 0x218f;
            case "IsArrows": return 0x21ff;
            case "IsMathematicalOperators": return 0x22ff;
            case "IsMiscellaneousTechnical": return 0x23ff;
            case "IsControlPictures": return 0x243f;
            case "IsOpticalCharacterRecognition": return 0x245f;
            case "IsEnclosedAlphanumerics": return 0x24ff;
            case "IsBoxDrawing": return 0x257f;
            case "IsBlockElements": return 0x259f;
            case "IsGeometricShapes": return 0x25ff;
            case "IsMiscellaneousSymbols": return 0x26ff;
            case "IsDingbats": return 0x27bf;
            case "IsCJKSymbolsandPunctuation": return 0x303f;
            case "IsHiragana": return 0x309f;
            case "IsKatakana": return 0x30ff;
            case "IsBopomofo": return 0x312f;
            case "IsHangulCompatibilityJamo": return 0x318f;
            case "IsCJKUnifiedIdeographs": return 0x9fff;
            case "IsHangulSyllables": return 0xd7af;
            case "IsHighSurrogates": return 0xdbff;
            case "IsLowSurrogates": return 0xdfff;
            case "IsPrivateUse": return 0xf8ff;
            case "IsCJKCompatibilityIdeographs": return 0xfaff;
            case "IsAlphabeticPresentationForms": return 0xfb4f;
            case "IsArabicPresentationForms-A": return 0xfdff;
            case "IsCombiningHalfMarks": return 0xfe2f;
            case "IsCJKCompatibilityForms": return 0xfe4f;
            case "IsSmallFormVariants": return 0xfe6f;
            case "IsArabicPresentationForms-B": return 0xfeff;
            case "IsHalfwidthandFullwidthForms": return 0xffef;
            case "IsSpecials": return 0xffff;
            default: return -1;
        }
    }
}
