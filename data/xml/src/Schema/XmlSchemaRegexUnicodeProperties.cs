using System.Globalization;

namespace xml;

internal class XmlSchemaRegexUnicodeProperties
{
    internal XmlSchemaRegexUnicodeProperties()
    {
    }

    internal bool IsXmlSchemaRegexPropertyName(string name)
    {
        if (IsXmlSchemaRegexCategoryName(name))
        {
            return true;
        }

        return ReadXmlSchemaRegexBlockRange(name) != null;
    }

    internal bool MatchesXmlSchemaRegexProperty(string name, int codepoint)
    {
        if (name == "s")
        {
            return codepoint == 0x20 || codepoint == 0x9 || codepoint == 0xA || codepoint == 0xD;
        }

        if (name == "i")
        {
            return Xml10CharacterRules.IsXml10NameStartCharacter(codepoint);
        }

        if (name == "c")
        {
            return Xml10CharacterRules.IsXml10NameCharacter(codepoint);
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codepoint), 0);
        if (name == "d")
        {
            return category == UnicodeCategory.DecimalDigitNumber;
        }

        if (name == "w")
        {
            return !IsXmlSchemaRegexPunctuation(category) && !IsXmlSchemaRegexSeparator(category) && !IsXmlSchemaRegexOther(category);
        }

        if (IsXmlSchemaRegexCategoryName(name))
        {
            return MatchesXmlSchemaRegexCategory(name, category);
        }

        int[]? range = ReadXmlSchemaRegexBlockRange(name);
        return range != null && codepoint >= range[0] && codepoint <= range[1];
    }

    internal bool IsXmlSchemaRegexCategoryName(string name)
    {
        return name == "L" || name == "Lu" || name == "Ll" || name == "Lt" || name == "Lm" || name == "Lo" ||
            name == "M" || name == "Mn" || name == "Mc" || name == "Me" ||
            name == "N" || name == "Nd" || name == "Nl" || name == "No" ||
            name == "P" || name == "Pc" || name == "Pd" || name == "Ps" || name == "Pe" || name == "Pi" || name == "Pf" || name == "Po" ||
            name == "Z" || name == "Zs" || name == "Zl" || name == "Zp" ||
            name == "S" || name == "Sm" || name == "Sc" || name == "Sk" || name == "So" ||
            name == "C" || name == "Cc" || name == "Cf" || name == "Cs" || name == "Co" || name == "Cn";
    }

    internal bool MatchesXmlSchemaRegexCategory(string name, UnicodeCategory category)
    {
        if (name.Length == 1)
        {
            if (name == "L")
            {
                return category == UnicodeCategory.UppercaseLetter || category == UnicodeCategory.LowercaseLetter || category == UnicodeCategory.TitlecaseLetter || category == UnicodeCategory.ModifierLetter || category == UnicodeCategory.OtherLetter;
            }

            if (name == "M")
            {
                return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark;
            }

            if (name == "N")
            {
                return category == UnicodeCategory.DecimalDigitNumber || category == UnicodeCategory.LetterNumber || category == UnicodeCategory.OtherNumber;
            }

            if (name == "P")
            {
                return IsXmlSchemaRegexPunctuation(category);
            }

            if (name == "Z")
            {
                return IsXmlSchemaRegexSeparator(category);
            }

            if (name == "S")
            {
                return category == UnicodeCategory.MathSymbol || category == UnicodeCategory.CurrencySymbol || category == UnicodeCategory.ModifierSymbol || category == UnicodeCategory.OtherSymbol;
            }

            return IsXmlSchemaRegexOther(category);
        }

        if (name == "Lu")
        {
            return category == UnicodeCategory.UppercaseLetter;
        }

        if (name == "Ll")
        {
            return category == UnicodeCategory.LowercaseLetter;
        }

        if (name == "Lt")
        {
            return category == UnicodeCategory.TitlecaseLetter;
        }

        if (name == "Lm")
        {
            return category == UnicodeCategory.ModifierLetter;
        }

        if (name == "Lo")
        {
            return category == UnicodeCategory.OtherLetter;
        }

        if (name == "Mn")
        {
            return category == UnicodeCategory.NonSpacingMark;
        }

        if (name == "Mc")
        {
            return category == UnicodeCategory.SpacingCombiningMark;
        }

        if (name == "Me")
        {
            return category == UnicodeCategory.EnclosingMark;
        }

        if (name == "Nd")
        {
            return category == UnicodeCategory.DecimalDigitNumber;
        }

        if (name == "Nl")
        {
            return category == UnicodeCategory.LetterNumber;
        }

        if (name == "No")
        {
            return category == UnicodeCategory.OtherNumber;
        }

        if (name == "Pc")
        {
            return category == UnicodeCategory.ConnectorPunctuation;
        }

        if (name == "Pd")
        {
            return category == UnicodeCategory.DashPunctuation;
        }

        if (name == "Ps")
        {
            return category == UnicodeCategory.OpenPunctuation;
        }

        if (name == "Pe")
        {
            return category == UnicodeCategory.ClosePunctuation;
        }

        if (name == "Pi")
        {
            return category == UnicodeCategory.InitialQuotePunctuation;
        }

        if (name == "Pf")
        {
            return category == UnicodeCategory.FinalQuotePunctuation;
        }

        if (name == "Po")
        {
            return category == UnicodeCategory.OtherPunctuation;
        }

        if (name == "Zs")
        {
            return category == UnicodeCategory.SpaceSeparator;
        }

        if (name == "Zl")
        {
            return category == UnicodeCategory.LineSeparator;
        }

        if (name == "Zp")
        {
            return category == UnicodeCategory.ParagraphSeparator;
        }

        if (name == "Sm")
        {
            return category == UnicodeCategory.MathSymbol;
        }

        if (name == "Sc")
        {
            return category == UnicodeCategory.CurrencySymbol;
        }

        if (name == "Sk")
        {
            return category == UnicodeCategory.ModifierSymbol;
        }

        if (name == "So")
        {
            return category == UnicodeCategory.OtherSymbol;
        }

        if (name == "Cc")
        {
            return category == UnicodeCategory.Control;
        }

        if (name == "Cf")
        {
            return category == UnicodeCategory.Format;
        }

        if (name == "Cs")
        {
            return category == UnicodeCategory.Surrogate;
        }

        if (name == "Co")
        {
            return category == UnicodeCategory.PrivateUse;
        }

        return category == UnicodeCategory.OtherNotAssigned;
    }

    internal bool IsXmlSchemaRegexPunctuation(UnicodeCategory category)
    {
        return category == UnicodeCategory.ConnectorPunctuation || category == UnicodeCategory.DashPunctuation || category == UnicodeCategory.OpenPunctuation || category == UnicodeCategory.ClosePunctuation || category == UnicodeCategory.InitialQuotePunctuation || category == UnicodeCategory.FinalQuotePunctuation || category == UnicodeCategory.OtherPunctuation;
    }

    internal bool IsXmlSchemaRegexSeparator(UnicodeCategory category)
    {
        return category == UnicodeCategory.SpaceSeparator || category == UnicodeCategory.LineSeparator || category == UnicodeCategory.ParagraphSeparator;
    }

    internal bool IsXmlSchemaRegexOther(UnicodeCategory category)
    {
        return category == UnicodeCategory.Control || category == UnicodeCategory.Format || category == UnicodeCategory.Surrogate || category == UnicodeCategory.PrivateUse || category == UnicodeCategory.OtherNotAssigned;
    }

    internal int[]? ReadXmlSchemaRegexBlockRange(string name)
    {
        if (!name.StartsWith("Is", System.StringComparison.Ordinal) || name.Length == 2)
        {
            return null;
        }

        return ReadXmlSchemaRegexCurrentBlockRange(name.Substring(2));
    }

    internal int[]? ReadXmlSchemaRegexCurrentBlockRange(string name)
    {
        switch (name)
        {
            case "BasicLatin": return [0x0000, 0x007F];
            case "Latin-1Supplement": return [0x0080, 0x00FF];
            case "LatinExtended-A": return [0x0100, 0x017F];
            case "LatinExtended-B": return [0x0180, 0x024F];
            case "IPAExtensions": return [0x0250, 0x02AF];
            case "SpacingModifierLetters": return [0x02B0, 0x02FF];
            case "CombiningDiacriticalMarks": return [0x0300, 0x036F];
            case "GreekandCoptic": return [0x0370, 0x03FF];
            case "Cyrillic": return [0x0400, 0x04FF];
            case "CyrillicSupplement": return [0x0500, 0x052F];
            case "Armenian": return [0x0530, 0x058F];
            case "Hebrew": return [0x0590, 0x05FF];
            case "Arabic": return [0x0600, 0x06FF];
            case "Syriac": return [0x0700, 0x074F];
            case "ArabicSupplement": return [0x0750, 0x077F];
            case "Thaana": return [0x0780, 0x07BF];
            case "NKo": return [0x07C0, 0x07FF];
            case "Samaritan": return [0x0800, 0x083F];
            case "Mandaic": return [0x0840, 0x085F];
            case "SyriacSupplement": return [0x0860, 0x086F];
            case "ArabicExtended-B": return [0x0870, 0x089F];
            case "ArabicExtended-A": return [0x08A0, 0x08FF];
            case "Devanagari": return [0x0900, 0x097F];
            case "Bengali": return [0x0980, 0x09FF];
            case "Gurmukhi": return [0x0A00, 0x0A7F];
            case "Gujarati": return [0x0A80, 0x0AFF];
            case "Oriya": return [0x0B00, 0x0B7F];
            case "Tamil": return [0x0B80, 0x0BFF];
            case "Telugu": return [0x0C00, 0x0C7F];
            case "Kannada": return [0x0C80, 0x0CFF];
            case "Malayalam": return [0x0D00, 0x0D7F];
            case "Sinhala": return [0x0D80, 0x0DFF];
            case "Thai": return [0x0E00, 0x0E7F];
            case "Lao": return [0x0E80, 0x0EFF];
            case "Tibetan": return [0x0F00, 0x0FFF];
            case "Myanmar": return [0x1000, 0x109F];
            case "Georgian": return [0x10A0, 0x10FF];
            case "HangulJamo": return [0x1100, 0x11FF];
            case "Ethiopic": return [0x1200, 0x137F];
            case "EthiopicSupplement": return [0x1380, 0x139F];
            case "Cherokee": return [0x13A0, 0x13FF];
            case "UnifiedCanadianAboriginalSyllabics": return [0x1400, 0x167F];
            case "Ogham": return [0x1680, 0x169F];
            case "Runic": return [0x16A0, 0x16FF];
            case "Tagalog": return [0x1700, 0x171F];
            case "Hanunoo": return [0x1720, 0x173F];
            case "Buhid": return [0x1740, 0x175F];
            case "Tagbanwa": return [0x1760, 0x177F];
            case "Khmer": return [0x1780, 0x17FF];
            case "Mongolian": return [0x1800, 0x18AF];
            case "UnifiedCanadianAboriginalSyllabicsExtended": return [0x18B0, 0x18FF];
            case "Limbu": return [0x1900, 0x194F];
            case "TaiLe": return [0x1950, 0x197F];
            case "NewTaiLue": return [0x1980, 0x19DF];
            case "KhmerSymbols": return [0x19E0, 0x19FF];
            case "Buginese": return [0x1A00, 0x1A1F];
            case "TaiTham": return [0x1A20, 0x1AAF];
            case "CombiningDiacriticalMarksExtended": return [0x1AB0, 0x1AFF];
            case "Balinese": return [0x1B00, 0x1B7F];
            case "Sundanese": return [0x1B80, 0x1BBF];
            case "Batak": return [0x1BC0, 0x1BFF];
            case "Lepcha": return [0x1C00, 0x1C4F];
            case "OlChiki": return [0x1C50, 0x1C7F];
            case "CyrillicExtended-C": return [0x1C80, 0x1C8F];
            case "GeorgianExtended": return [0x1C90, 0x1CBF];
            case "SundaneseSupplement": return [0x1CC0, 0x1CCF];
            case "VedicExtensions": return [0x1CD0, 0x1CFF];
            case "PhoneticExtensions": return [0x1D00, 0x1D7F];
            case "PhoneticExtensionsSupplement": return [0x1D80, 0x1DBF];
            case "CombiningDiacriticalMarksSupplement": return [0x1DC0, 0x1DFF];
            case "LatinExtendedAdditional": return [0x1E00, 0x1EFF];
            case "GreekExtended": return [0x1F00, 0x1FFF];
            case "GeneralPunctuation": return [0x2000, 0x206F];
            case "SuperscriptsandSubscripts": return [0x2070, 0x209F];
            case "CurrencySymbols": return [0x20A0, 0x20CF];
            case "CombiningDiacriticalMarksforSymbols": return [0x20D0, 0x20FF];
            case "LetterlikeSymbols": return [0x2100, 0x214F];
            case "NumberForms": return [0x2150, 0x218F];
            case "Arrows": return [0x2190, 0x21FF];
            case "MathematicalOperators": return [0x2200, 0x22FF];
            case "MiscellaneousTechnical": return [0x2300, 0x23FF];
            case "ControlPictures": return [0x2400, 0x243F];
            case "OpticalCharacterRecognition": return [0x2440, 0x245F];
            case "EnclosedAlphanumerics": return [0x2460, 0x24FF];
            case "BoxDrawing": return [0x2500, 0x257F];
            case "BlockElements": return [0x2580, 0x259F];
            case "GeometricShapes": return [0x25A0, 0x25FF];
            case "MiscellaneousSymbols": return [0x2600, 0x26FF];
            case "Dingbats": return [0x2700, 0x27BF];
            case "MiscellaneousMathematicalSymbols-A": return [0x27C0, 0x27EF];
            case "SupplementalArrows-A": return [0x27F0, 0x27FF];
            case "BraillePatterns": return [0x2800, 0x28FF];
            case "SupplementalArrows-B": return [0x2900, 0x297F];
            case "MiscellaneousMathematicalSymbols-B": return [0x2980, 0x29FF];
            case "SupplementalMathematicalOperators": return [0x2A00, 0x2AFF];
            case "MiscellaneousSymbolsandArrows": return [0x2B00, 0x2BFF];
            case "Glagolitic": return [0x2C00, 0x2C5F];
            case "LatinExtended-C": return [0x2C60, 0x2C7F];
            case "Coptic": return [0x2C80, 0x2CFF];
            case "GeorgianSupplement": return [0x2D00, 0x2D2F];
            case "Tifinagh": return [0x2D30, 0x2D7F];
            case "EthiopicExtended": return [0x2D80, 0x2DDF];
            case "CyrillicExtended-A": return [0x2DE0, 0x2DFF];
            case "SupplementalPunctuation": return [0x2E00, 0x2E7F];
            case "CJKRadicalsSupplement": return [0x2E80, 0x2EFF];
            case "KangxiRadicals": return [0x2F00, 0x2FDF];
            case "IdeographicDescriptionCharacters": return [0x2FF0, 0x2FFF];
            case "CJKSymbolsandPunctuation": return [0x3000, 0x303F];
            case "Hiragana": return [0x3040, 0x309F];
            case "Katakana": return [0x30A0, 0x30FF];
            case "Bopomofo": return [0x3100, 0x312F];
            case "HangulCompatibilityJamo": return [0x3130, 0x318F];
            case "Kanbun": return [0x3190, 0x319F];
            case "BopomofoExtended": return [0x31A0, 0x31BF];
            case "CJKStrokes": return [0x31C0, 0x31EF];
            case "KatakanaPhoneticExtensions": return [0x31F0, 0x31FF];
            case "EnclosedCJKLettersandMonths": return [0x3200, 0x32FF];
            case "CJKCompatibility": return [0x3300, 0x33FF];
            case "CJKUnifiedIdeographsExtensionA": return [0x3400, 0x4DBF];
            case "YijingHexagramSymbols": return [0x4DC0, 0x4DFF];
            case "CJKUnifiedIdeographs": return [0x4E00, 0x9FFF];
            case "YiSyllables": return [0xA000, 0xA48F];
            case "YiRadicals": return [0xA490, 0xA4CF];
            case "Lisu": return [0xA4D0, 0xA4FF];
            case "Vai": return [0xA500, 0xA63F];
            case "CyrillicExtended-B": return [0xA640, 0xA69F];
            case "Bamum": return [0xA6A0, 0xA6FF];
            case "ModifierToneLetters": return [0xA700, 0xA71F];
            case "LatinExtended-D": return [0xA720, 0xA7FF];
            case "SylotiNagri": return [0xA800, 0xA82F];
            case "CommonIndicNumberForms": return [0xA830, 0xA83F];
            case "Phags-pa": return [0xA840, 0xA87F];
            case "Saurashtra": return [0xA880, 0xA8DF];
            case "DevanagariExtended": return [0xA8E0, 0xA8FF];
            case "KayahLi": return [0xA900, 0xA92F];
            case "Rejang": return [0xA930, 0xA95F];
            case "HangulJamoExtended-A": return [0xA960, 0xA97F];
            case "Javanese": return [0xA980, 0xA9DF];
            case "MyanmarExtended-B": return [0xA9E0, 0xA9FF];
            case "Cham": return [0xAA00, 0xAA5F];
            case "MyanmarExtended-A": return [0xAA60, 0xAA7F];
            case "TaiViet": return [0xAA80, 0xAADF];
            case "MeeteiMayekExtensions": return [0xAAE0, 0xAAFF];
            case "EthiopicExtended-A": return [0xAB00, 0xAB2F];
            case "LatinExtended-E": return [0xAB30, 0xAB6F];
            case "CherokeeSupplement": return [0xAB70, 0xABBF];
            case "MeeteiMayek": return [0xABC0, 0xABFF];
            case "HangulSyllables": return [0xAC00, 0xD7AF];
            case "HangulJamoExtended-B": return [0xD7B0, 0xD7FF];
            case "HighSurrogates": return [0xD800, 0xDB7F];
            case "HighPrivateUseSurrogates": return [0xDB80, 0xDBFF];
            case "LowSurrogates": return [0xDC00, 0xDFFF];
            case "PrivateUseArea": return [0xE000, 0xF8FF];
            case "CJKCompatibilityIdeographs": return [0xF900, 0xFAFF];
            case "AlphabeticPresentationForms": return [0xFB00, 0xFB4F];
            case "ArabicPresentationForms-A": return [0xFB50, 0xFDFF];
            case "VariationSelectors": return [0xFE00, 0xFE0F];
            case "VerticalForms": return [0xFE10, 0xFE1F];
            case "CombiningHalfMarks": return [0xFE20, 0xFE2F];
            case "CJKCompatibilityForms": return [0xFE30, 0xFE4F];
            case "SmallFormVariants": return [0xFE50, 0xFE6F];
            case "ArabicPresentationForms-B": return [0xFE70, 0xFEFF];
            case "HalfwidthandFullwidthForms": return [0xFF00, 0xFFEF];
            case "Specials": return [0xFFF0, 0xFFFF];
            case "LinearBSyllabary": return [0x10000, 0x1007F];
            case "LinearBIdeograms": return [0x10080, 0x100FF];
            case "AegeanNumbers": return [0x10100, 0x1013F];
            case "AncientGreekNumbers": return [0x10140, 0x1018F];
            case "AncientSymbols": return [0x10190, 0x101CF];
            case "PhaistosDisc": return [0x101D0, 0x101FF];
            case "Lycian": return [0x10280, 0x1029F];
            case "Carian": return [0x102A0, 0x102DF];
            case "CopticEpactNumbers": return [0x102E0, 0x102FF];
            case "OldItalic": return [0x10300, 0x1032F];
            case "Gothic": return [0x10330, 0x1034F];
            case "OldPermic": return [0x10350, 0x1037F];
            case "Ugaritic": return [0x10380, 0x1039F];
            case "OldPersian": return [0x103A0, 0x103DF];
            case "Deseret": return [0x10400, 0x1044F];
            case "Shavian": return [0x10450, 0x1047F];
            case "Osmanya": return [0x10480, 0x104AF];
            case "Osage": return [0x104B0, 0x104FF];
            case "Elbasan": return [0x10500, 0x1052F];
            case "CaucasianAlbanian": return [0x10530, 0x1056F];
            case "Vithkuqi": return [0x10570, 0x105BF];
            case "Todhri": return [0x105C0, 0x105FF];
            case "LinearA": return [0x10600, 0x1077F];
            case "LatinExtended-F": return [0x10780, 0x107BF];
            case "CypriotSyllabary": return [0x10800, 0x1083F];
            case "ImperialAramaic": return [0x10840, 0x1085F];
            case "Palmyrene": return [0x10860, 0x1087F];
            case "Nabataean": return [0x10880, 0x108AF];
            case "Hatran": return [0x108E0, 0x108FF];
            case "Phoenician": return [0x10900, 0x1091F];
            case "Lydian": return [0x10920, 0x1093F];
            case "Sidetic": return [0x10940, 0x1095F];
            case "MeroiticHieroglyphs": return [0x10980, 0x1099F];
            case "MeroiticCursive": return [0x109A0, 0x109FF];
            case "Kharoshthi": return [0x10A00, 0x10A5F];
            case "OldSouthArabian": return [0x10A60, 0x10A7F];
            case "OldNorthArabian": return [0x10A80, 0x10A9F];
            case "Manichaean": return [0x10AC0, 0x10AFF];
            case "Avestan": return [0x10B00, 0x10B3F];
            case "InscriptionalParthian": return [0x10B40, 0x10B5F];
            case "InscriptionalPahlavi": return [0x10B60, 0x10B7F];
            case "PsalterPahlavi": return [0x10B80, 0x10BAF];
            case "OldTurkic": return [0x10C00, 0x10C4F];
            case "OldHungarian": return [0x10C80, 0x10CFF];
            case "HanifiRohingya": return [0x10D00, 0x10D3F];
            case "Garay": return [0x10D40, 0x10D8F];
            case "RumiNumeralSymbols": return [0x10E60, 0x10E7F];
            case "Yezidi": return [0x10E80, 0x10EBF];
            case "ArabicExtended-C": return [0x10EC0, 0x10EFF];
            case "OldSogdian": return [0x10F00, 0x10F2F];
            case "Sogdian": return [0x10F30, 0x10F6F];
            case "OldUyghur": return [0x10F70, 0x10FAF];
            case "Chorasmian": return [0x10FB0, 0x10FDF];
            case "Elymaic": return [0x10FE0, 0x10FFF];
            case "Brahmi": return [0x11000, 0x1107F];
            case "Kaithi": return [0x11080, 0x110CF];
            case "SoraSompeng": return [0x110D0, 0x110FF];
            case "Chakma": return [0x11100, 0x1114F];
            case "Mahajani": return [0x11150, 0x1117F];
            case "Sharada": return [0x11180, 0x111DF];
            case "SinhalaArchaicNumbers": return [0x111E0, 0x111FF];
            case "Khojki": return [0x11200, 0x1124F];
            case "Multani": return [0x11280, 0x112AF];
            case "Khudawadi": return [0x112B0, 0x112FF];
            case "Grantha": return [0x11300, 0x1137F];
            case "Tulu-Tigalari": return [0x11380, 0x113FF];
            case "Newa": return [0x11400, 0x1147F];
            case "Tirhuta": return [0x11480, 0x114DF];
            case "Siddham": return [0x11580, 0x115FF];
            case "Modi": return [0x11600, 0x1165F];
            case "MongolianSupplement": return [0x11660, 0x1167F];
            case "Takri": return [0x11680, 0x116CF];
            case "MyanmarExtended-C": return [0x116D0, 0x116FF];
            case "Ahom": return [0x11700, 0x1174F];
            case "Dogra": return [0x11800, 0x1184F];
            case "WarangCiti": return [0x118A0, 0x118FF];
            case "DivesAkuru": return [0x11900, 0x1195F];
            case "Nandinagari": return [0x119A0, 0x119FF];
            case "ZanabazarSquare": return [0x11A00, 0x11A4F];
            case "Soyombo": return [0x11A50, 0x11AAF];
            case "UnifiedCanadianAboriginalSyllabicsExtended-A": return [0x11AB0, 0x11ABF];
            case "PauCinHau": return [0x11AC0, 0x11AFF];
            case "DevanagariExtended-A": return [0x11B00, 0x11B5F];
            case "SharadaSupplement": return [0x11B60, 0x11B7F];
            case "Sunuwar": return [0x11BC0, 0x11BFF];
            case "Bhaiksuki": return [0x11C00, 0x11C6F];
            case "Marchen": return [0x11C70, 0x11CBF];
            case "MasaramGondi": return [0x11D00, 0x11D5F];
            case "GunjalaGondi": return [0x11D60, 0x11DAF];
            case "TolongSiki": return [0x11DB0, 0x11DEF];
            case "Makasar": return [0x11EE0, 0x11EFF];
            case "Kawi": return [0x11F00, 0x11F5F];
            case "LisuSupplement": return [0x11FB0, 0x11FBF];
            case "TamilSupplement": return [0x11FC0, 0x11FFF];
            case "Cuneiform": return [0x12000, 0x123FF];
            case "CuneiformNumbersandPunctuation": return [0x12400, 0x1247F];
            case "EarlyDynasticCuneiform": return [0x12480, 0x1254F];
            case "Cypro-Minoan": return [0x12F90, 0x12FFF];
            case "EgyptianHieroglyphs": return [0x13000, 0x1342F];
            case "EgyptianHieroglyphFormatControls": return [0x13430, 0x1345F];
            case "EgyptianHieroglyphsExtended-A": return [0x13460, 0x143FF];
            case "AnatolianHieroglyphs": return [0x14400, 0x1467F];
            case "GurungKhema": return [0x16100, 0x1613F];
            case "BamumSupplement": return [0x16800, 0x16A3F];
            case "Mro": return [0x16A40, 0x16A6F];
            case "Tangsa": return [0x16A70, 0x16ACF];
            case "BassaVah": return [0x16AD0, 0x16AFF];
            case "PahawhHmong": return [0x16B00, 0x16B8F];
            case "KiratRai": return [0x16D40, 0x16D7F];
            case "Medefaidrin": return [0x16E40, 0x16E9F];
            case "BeriaErfe": return [0x16EA0, 0x16EDF];
            case "Miao": return [0x16F00, 0x16F9F];
            case "IdeographicSymbolsandPunctuation": return [0x16FE0, 0x16FFF];
            case "Tangut": return [0x17000, 0x187FF];
            case "TangutComponents": return [0x18800, 0x18AFF];
            case "KhitanSmallScript": return [0x18B00, 0x18CFF];
            case "TangutSupplement": return [0x18D00, 0x18D7F];
            case "TangutComponentsSupplement": return [0x18D80, 0x18DFF];
            case "KanaExtended-B": return [0x1AFF0, 0x1AFFF];
            case "KanaSupplement": return [0x1B000, 0x1B0FF];
            case "KanaExtended-A": return [0x1B100, 0x1B12F];
            case "SmallKanaExtension": return [0x1B130, 0x1B16F];
            case "Nushu": return [0x1B170, 0x1B2FF];
            case "Duployan": return [0x1BC00, 0x1BC9F];
            case "ShorthandFormatControls": return [0x1BCA0, 0x1BCAF];
            case "SymbolsforLegacyComputingSupplement": return [0x1CC00, 0x1CEBF];
            case "MiscellaneousSymbolsSupplement": return [0x1CEC0, 0x1CEFF];
            case "ZnamennyMusicalNotation": return [0x1CF00, 0x1CFCF];
            case "ByzantineMusicalSymbols": return [0x1D000, 0x1D0FF];
            case "MusicalSymbols": return [0x1D100, 0x1D1FF];
            case "AncientGreekMusicalNotation": return [0x1D200, 0x1D24F];
            case "KaktovikNumerals": return [0x1D2C0, 0x1D2DF];
            case "MayanNumerals": return [0x1D2E0, 0x1D2FF];
            case "TaiXuanJingSymbols": return [0x1D300, 0x1D35F];
            case "CountingRodNumerals": return [0x1D360, 0x1D37F];
            case "MathematicalAlphanumericSymbols": return [0x1D400, 0x1D7FF];
            case "SuttonSignWriting": return [0x1D800, 0x1DAAF];
            case "LatinExtended-G": return [0x1DF00, 0x1DFFF];
            case "GlagoliticSupplement": return [0x1E000, 0x1E02F];
            case "CyrillicExtended-D": return [0x1E030, 0x1E08F];
            case "NyiakengPuachueHmong": return [0x1E100, 0x1E14F];
            case "Toto": return [0x1E290, 0x1E2BF];
            case "Wancho": return [0x1E2C0, 0x1E2FF];
            case "NagMundari": return [0x1E4D0, 0x1E4FF];
            case "OlOnal": return [0x1E5D0, 0x1E5FF];
            case "TaiYo": return [0x1E6C0, 0x1E6FF];
            case "EthiopicExtended-B": return [0x1E7E0, 0x1E7FF];
            case "MendeKikakui": return [0x1E800, 0x1E8DF];
            case "Adlam": return [0x1E900, 0x1E95F];
            case "IndicSiyaqNumbers": return [0x1EC70, 0x1ECBF];
            case "OttomanSiyaqNumbers": return [0x1ED00, 0x1ED4F];
            case "ArabicMathematicalAlphabeticSymbols": return [0x1EE00, 0x1EEFF];
            case "MahjongTiles": return [0x1F000, 0x1F02F];
            case "DominoTiles": return [0x1F030, 0x1F09F];
            case "PlayingCards": return [0x1F0A0, 0x1F0FF];
            case "EnclosedAlphanumericSupplement": return [0x1F100, 0x1F1FF];
            case "EnclosedIdeographicSupplement": return [0x1F200, 0x1F2FF];
            case "MiscellaneousSymbolsandPictographs": return [0x1F300, 0x1F5FF];
            case "Emoticons": return [0x1F600, 0x1F64F];
            case "OrnamentalDingbats": return [0x1F650, 0x1F67F];
            case "TransportandMapSymbols": return [0x1F680, 0x1F6FF];
            case "AlchemicalSymbols": return [0x1F700, 0x1F77F];
            case "GeometricShapesExtended": return [0x1F780, 0x1F7FF];
            case "SupplementalArrows-C": return [0x1F800, 0x1F8FF];
            case "SupplementalSymbolsandPictographs": return [0x1F900, 0x1F9FF];
            case "ChessSymbols": return [0x1FA00, 0x1FA6F];
            case "SymbolsandPictographsExtended-A": return [0x1FA70, 0x1FAFF];
            case "SymbolsforLegacyComputing": return [0x1FB00, 0x1FBFF];
            case "CJKUnifiedIdeographsExtensionB": return [0x20000, 0x2A6DF];
            case "CJKUnifiedIdeographsExtensionC": return [0x2A700, 0x2B73F];
            case "CJKUnifiedIdeographsExtensionD": return [0x2B740, 0x2B81F];
            case "CJKUnifiedIdeographsExtensionE": return [0x2B820, 0x2CEAF];
            case "CJKUnifiedIdeographsExtensionF": return [0x2CEB0, 0x2EBEF];
            case "CJKUnifiedIdeographsExtensionI": return [0x2EBF0, 0x2EE5F];
            case "CJKCompatibilityIdeographsSupplement": return [0x2F800, 0x2FA1F];
            case "CJKUnifiedIdeographsExtensionG": return [0x30000, 0x3134F];
            case "CJKUnifiedIdeographsExtensionH": return [0x31350, 0x323AF];
            case "CJKUnifiedIdeographsExtensionJ": return [0x323B0, 0x3347F];
            case "Tags": return [0xE0000, 0xE007F];
            case "VariationSelectorsSupplement": return [0xE0100, 0xE01EF];
            case "SupplementaryPrivateUseArea-A": return [0xF0000, 0xFFFFF];
            case "SupplementaryPrivateUseArea-B": return [0x100000, 0x10FFFF];
            case "Greek": return [0x0370, 0x03FF];
            case "CyrillicSupplementary": return [0x0500, 0x052F];
            case "CombiningMarksforSymbols": return [0x20D0, 0x20FF];
            case "SurrogatesArea": return [0xD800, 0xDFFF];
            case "PrivateUse": return [0xE000, 0xF8FF];
            default: return null;
        }
    }

}
