using System;
using System.Globalization;
using System.Text;

namespace fake;

internal static class TextTransforms
{
    internal static string Lower(string value, string locale, int maximumOutputSize)
    {
        UnicodeText.Validate(value);
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        string result = value.ToLower(Culture(locale));
        Guard.OutputSize(result.Length, maximumOutputSize, "value");
        return result;
    }

    internal static string Upper(string value, string locale, int maximumOutputSize)
    {
        UnicodeText.Validate(value);
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        string result = value.ToUpper(Culture(locale));
        Guard.OutputSize(result.Length, maximumOutputSize, "value");
        return result;
    }

    internal static string TitleCase(string value, string locale, int maximumOutputSize)
    {
        UnicodeText.Validate(value);
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
        string result = Culture(locale).TextInfo.ToTitleCase(value);
        Guard.OutputSize(result.Length, maximumOutputSize, "value");
        return result;
    }

    internal static string FoldAscii(string value, int maximumOutputSize)
    {
        UnicodeText.Validate(value);
        string decomposed = value.Normalize(NormalizationForm.FormKD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        int index = 0;
        while (index < decomposed.Length)
        {
            char character = decomposed[index];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(decomposed, index);
            if (!Mark(category))
            {
                if (character <= 127)
                {
                    builder.Append(character);
                }
                else
                {
                    string? mapped = Latin(character);
                    if (mapped != null)
                    {
                        builder.Append(mapped);
                    }
                }
            }
            index = index + (char.IsHighSurrogate(character) ? 2 : 1);
        }
        Guard.OutputSize(builder.Length, maximumOutputSize, "value");
        return builder.ToString();
    }

    internal static string[] Tables()
    {
        return new string[] { "latin-ascii", "greek-latin", "cyrillic-latin", "arabic-latin", "hebrew-latin" };
    }

    internal static string Transliterate(string value, string table, int maximumOutputSize)
    {
        UnicodeText.Validate(value);
        Guard.NotNullOrWhiteSpace(table, "table");
        if (table == "latin-ascii")
        {
            return FoldAscii(value, maximumOutputSize);
        }
        if (table != "greek-latin"
            && table != "cyrillic-latin"
            && table != "arabic-latin"
            && table != "hebrew-latin")
        {
            throw new ArgumentException($"Transliteration table '{table}' is not declared.", "table");
        }
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (char.IsHighSurrogate(character))
            {
                builder.Append(character);
                builder.Append(value[index + 1]);
                index = index + 2;
                continue;
            }
            AppendTransliterated(builder, character, table);
            index = index + 1;
        }
        Guard.OutputSize(builder.Length, maximumOutputSize, "value");
        return builder.ToString();
    }

    internal static string Slug(string value, char separator, int maximumOutputSize)
    {
        if (separator != '-' && separator != '_')
        {
            throw new ArgumentException("separator must be '-' or '_'.", "separator");
        }
        string ascii = TransliterateAll(value, maximumOutputSize).ToLowerInvariant();
        StringBuilder builder = new StringBuilder(ascii.Length);
        bool pendingSeparator = false;
        int index = 0;
        while (index < ascii.Length)
        {
            char character = ascii[index];
            if (AsciiLetter(character) || AsciiDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append(separator);
                }
                builder.Append(character);
                pendingSeparator = false;
            }
            else if (builder.Length > 0)
            {
                pendingSeparator = true;
            }
            index = index + 1;
        }
        Guard.OutputSize(builder.Length, maximumOutputSize, "value");
        return builder.ToString();
    }

    internal static string Identifier(string value, int maximumOutputSize)
    {
        string ascii = TransliterateAll(value, maximumOutputSize);
        StringBuilder builder = new StringBuilder(ascii.Length + 1);
        bool pendingSeparator = false;
        int index = 0;
        while (index < ascii.Length)
        {
            char character = ascii[index];
            if (AsciiLetter(character) || AsciiDigit(character))
            {
                if (pendingSeparator && builder.Length > 0 && builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
                builder.Append(character);
                pendingSeparator = false;
            }
            else if (character == '_')
            {
                if (builder.Length == 0 || builder[builder.Length - 1] != '_')
                {
                    builder.Append('_');
                }
                pendingSeparator = false;
            }
            else if (builder.Length > 0)
            {
                pendingSeparator = true;
            }
            index = index + 1;
        }
        while (builder.Length > 0 && builder[builder.Length - 1] == '_')
        {
            builder.Length = builder.Length - 1;
        }
        if (builder.Length == 0 || AsciiDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }
        Guard.OutputSize(builder.Length, maximumOutputSize, "value");
        return builder.ToString();
    }

    internal static string TransliterateAll(string value, int maximumOutputSize)
    {
        string result = Transliterate(value, "greek-latin", maximumOutputSize);
        result = Transliterate(result, "cyrillic-latin", maximumOutputSize);
        result = Transliterate(result, "arabic-latin", maximumOutputSize);
        result = Transliterate(result, "hebrew-latin", maximumOutputSize);
        return FoldAscii(result, maximumOutputSize);
    }

    static void AppendTransliterated(StringBuilder builder, char character, string table)
    {
        UnicodeCategory originalCategory = CharUnicodeInfo.GetUnicodeCategory(character.ToString(), 0);
        if (Mark(originalCategory))
        {
            return;
        }
        string? mapped = Table(character, table);
        if (mapped != null)
        {
            builder.Append(mapped);
            return;
        }
        string decomposed = character.ToString().Normalize(NormalizationForm.FormD);
        StringBuilder transformed = new StringBuilder(decomposed.Length);
        bool found = false;
        int index = 0;
        while (index < decomposed.Length)
        {
            char part = decomposed[index];
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(decomposed, index);
            if (!Mark(category))
            {
                mapped = Table(part, table);
                if (mapped != null)
                {
                    found = true;
                }
                transformed.Append(mapped == null ? part.ToString() : mapped);
            }
            index = index + 1;
        }
        builder.Append(found ? transformed.ToString() : character.ToString());
    }

    static CultureInfo Culture(string locale)
    {
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        if (tag.Language == null)
        {
            return CultureInfo.InvariantCulture;
        }
        string full = tag.Language;
        if (tag.Script != null)
        {
            full = full + "-" + tag.Script;
        }
        if (tag.Region != null)
        {
            full = full + "-" + tag.Region;
        }
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        CultureInfo? result = FindCulture(cultures, full);
        if (result != null)
        {
            return result;
        }
        if (tag.Region != null)
        {
            result = FindCulture(cultures, tag.Language + "-" + tag.Region);
            if (result != null)
            {
                return result;
            }
        }
        if (tag.Script != null)
        {
            result = FindCulture(cultures, tag.Language + "-" + tag.Script);
            if (result != null)
            {
                return result;
            }
        }
        result = FindCulture(cultures, tag.Language);
        return result == null ? CultureInfo.InvariantCulture : result;
    }

    static CultureInfo? FindCulture(CultureInfo[] cultures, string name)
    {
        int index = 0;
        while (index < cultures.Length)
        {
            if (string.Equals(cultures[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return cultures[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool Mark(UnicodeCategory category)
    {
        return category == UnicodeCategory.NonSpacingMark
            || category == UnicodeCategory.SpacingCombiningMark
            || category == UnicodeCategory.EnclosingMark;
    }

    static bool AsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
    }

    static bool AsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    static string? Table(char character, string table)
    {
        if (table == "greek-latin")
        {
            return Greek(character);
        }
        if (table == "cyrillic-latin")
        {
            return Cyrillic(character);
        }
        if (table == "arabic-latin")
        {
            return Arabic(character);
        }
        return Hebrew(character);
    }

    static string? Latin(char character)
    {
        switch (character)
        {
            case 'Æ': return "AE";
            case 'æ': return "ae";
            case 'Œ': return "OE";
            case 'œ': return "oe";
            case 'Ø': return "O";
            case 'ø': return "o";
            case 'Ł': return "L";
            case 'ł': return "l";
            case 'Đ': return "D";
            case 'đ': return "d";
            case 'Ð': return "D";
            case 'ð': return "d";
            case 'Þ': return "TH";
            case 'þ': return "th";
            case 'ß': return "ss";
            case 'ẞ': return "SS";
            case 'Ħ': return "H";
            case 'ħ': return "h";
            case 'ı': return "i";
            case 'Ŋ': return "N";
            case 'ŋ': return "n";
            default: return null;
        }
    }

    static string? Greek(char character)
    {
        switch (character)
        {
            case 'Α': return "A"; case 'α': return "a";
            case 'Β': return "V"; case 'β': return "v";
            case 'Γ': return "G"; case 'γ': return "g";
            case 'Δ': return "D"; case 'δ': return "d";
            case 'Ε': return "E"; case 'ε': return "e";
            case 'Ζ': return "Z"; case 'ζ': return "z";
            case 'Η': return "I"; case 'η': return "i";
            case 'Θ': return "Th"; case 'θ': return "th";
            case 'Ι': return "I"; case 'ι': return "i";
            case 'Κ': return "K"; case 'κ': return "k";
            case 'Λ': return "L"; case 'λ': return "l";
            case 'Μ': return "M"; case 'μ': return "m";
            case 'Ν': return "N"; case 'ν': return "n";
            case 'Ξ': return "X"; case 'ξ': return "x";
            case 'Ο': return "O"; case 'ο': return "o";
            case 'Π': return "P"; case 'π': return "p";
            case 'Ρ': return "R"; case 'ρ': return "r";
            case 'Σ': return "S"; case 'σ': return "s"; case 'ς': return "s";
            case 'Τ': return "T"; case 'τ': return "t";
            case 'Υ': return "Y"; case 'υ': return "y";
            case 'Φ': return "F"; case 'φ': return "f";
            case 'Χ': return "Ch"; case 'χ': return "ch";
            case 'Ψ': return "Ps"; case 'ψ': return "ps";
            case 'Ω': return "O"; case 'ω': return "o";
            default: return null;
        }
    }

    static string? Cyrillic(char character)
    {
        switch (character)
        {
            case 'А': return "A"; case 'а': return "a";
            case 'Б': return "B"; case 'б': return "b";
            case 'В': return "V"; case 'в': return "v";
            case 'Г': return "G"; case 'г': return "g";
            case 'Д': return "D"; case 'д': return "d";
            case 'Е': return "E"; case 'е': return "e";
            case 'Ё': return "Yo"; case 'ё': return "yo";
            case 'Ж': return "Zh"; case 'ж': return "zh";
            case 'З': return "Z"; case 'з': return "z";
            case 'И': return "I"; case 'и': return "i";
            case 'Й': return "I"; case 'й': return "i";
            case 'К': return "K"; case 'к': return "k";
            case 'Л': return "L"; case 'л': return "l";
            case 'М': return "M"; case 'м': return "m";
            case 'Н': return "N"; case 'н': return "n";
            case 'О': return "O"; case 'о': return "o";
            case 'П': return "P"; case 'п': return "p";
            case 'Р': return "R"; case 'р': return "r";
            case 'С': return "S"; case 'с': return "s";
            case 'Т': return "T"; case 'т': return "t";
            case 'У': return "U"; case 'у': return "u";
            case 'Ф': return "F"; case 'ф': return "f";
            case 'Х': return "Kh"; case 'х': return "kh";
            case 'Ц': return "Ts"; case 'ц': return "ts";
            case 'Ч': return "Ch"; case 'ч': return "ch";
            case 'Ш': return "Sh"; case 'ш': return "sh";
            case 'Щ': return "Shch"; case 'щ': return "shch";
            case 'Ъ': return ""; case 'ъ': return "";
            case 'Ы': return "Y"; case 'ы': return "y";
            case 'Ь': return ""; case 'ь': return "";
            case 'Э': return "E"; case 'э': return "e";
            case 'Ю': return "Yu"; case 'ю': return "yu";
            case 'Я': return "Ya"; case 'я': return "ya";
            default: return null;
        }
    }

    static string? Arabic(char character)
    {
        switch (character)
        {
            case 'ا': return "a"; case 'أ': return "a"; case 'إ': return "a"; case 'آ': return "a";
            case 'ب': return "b"; case 'ت': return "t"; case 'ث': return "th";
            case 'ج': return "j"; case 'ح': return "h"; case 'خ': return "kh";
            case 'د': return "d"; case 'ذ': return "dh"; case 'ر': return "r"; case 'ز': return "z";
            case 'س': return "s"; case 'ش': return "sh"; case 'ص': return "s"; case 'ض': return "d";
            case 'ط': return "t"; case 'ظ': return "z"; case 'ع': return ""; case 'غ': return "gh";
            case 'ف': return "f"; case 'ق': return "q"; case 'ك': return "k"; case 'ل': return "l";
            case 'م': return "m"; case 'ن': return "n"; case 'ه': return "h"; case 'و': return "w";
            case 'ي': return "y"; case 'ى': return "a"; case 'ة': return "h"; case 'ء': return "";
            case 'ؤ': return "w"; case 'ئ': return "y";
            default: return null;
        }
    }

    static string? Hebrew(char character)
    {
        switch (character)
        {
            case 'א': return "a"; case 'ב': return "b"; case 'ג': return "g"; case 'ד': return "d";
            case 'ה': return "h"; case 'ו': return "v"; case 'ז': return "z"; case 'ח': return "h";
            case 'ט': return "t"; case 'י': return "y"; case 'כ': return "k"; case 'ך': return "k";
            case 'ל': return "l"; case 'מ': return "m"; case 'ם': return "m"; case 'נ': return "n";
            case 'ן': return "n"; case 'ס': return "s"; case 'ע': return "a"; case 'פ': return "p";
            case 'ף': return "p"; case 'צ': return "ts"; case 'ץ': return "ts"; case 'ק': return "q";
            case 'ר': return "r"; case 'ש': return "sh"; case 'ת': return "t";
            default: return null;
        }
    }
}
