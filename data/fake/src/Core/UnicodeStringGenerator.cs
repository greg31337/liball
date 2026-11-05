using System;

namespace fake;

internal static class UnicodeStringGenerator
{
    const int HighSurrogateStart = 0xD800;
    const int LowSurrogateEnd = 0xDFFF;
    const int SupplementaryStart = 0x10000;
    const int MaximumScalar = 0x10FFFF;
    const int SurrogateCount = 0x800;
    const int ScalarCount = 0x110000 - SurrogateCount;

    internal static int Scalar(FakeRandom random)
    {
        return FromOrdinal(random.IntFromMaximumExclusive(ScalarCount));
    }

    internal static int Scalar(FakeRandom random, int minimumScalar, int maximumScalar, bool maximumInclusive)
    {
        ValidateRange(minimumScalar, maximumScalar, maximumInclusive);
        int minimumOrdinal = ToOrdinal(minimumScalar);
        int maximumOrdinal = ToOrdinal(maximumScalar);
        return FromOrdinal(random.IntFromMinimumAndMaximumAndMaximumInclusive(minimumOrdinal, maximumOrdinal, maximumInclusive));
    }

    internal static string Generate(FakeRandom random, int count, int minimumScalar, int maximumScalar, bool maximumInclusive, int maximumOutputSize)
    {
        Guard.NonNegative(count, "count");
        ValidateRange(minimumScalar, maximumScalar, maximumInclusive);
        int maximumUnits = MaximumUnits(maximumScalar, maximumInclusive);
        int capacity = Guard.OutputSize((long)count * (long)maximumUnits, maximumOutputSize, "count");
        char[] result = new char[capacity];
        int scalarIndex = 0;
        int outputIndex = 0;
        while (scalarIndex < count)
        {
            int scalar = Scalar(random, minimumScalar, maximumScalar, maximumInclusive);
            if (scalar < SupplementaryStart)
            {
                result[outputIndex] = (char)scalar;
                outputIndex = outputIndex + 1;
            }
            else
            {
                int adjusted = scalar - SupplementaryStart;
                result[outputIndex] = (char)(HighSurrogateStart + (adjusted >> 10));
                result[outputIndex + 1] = (char)(0xDC00 + (adjusted & 0x3FF));
                outputIndex = outputIndex + 2;
            }
            scalarIndex = scalarIndex + 1;
        }
        return new string(result, 0, outputIndex);
    }

    internal static string GenerateScript(FakeRandom random, int count, string script, int maximumOutputSize)
    {
        string canonical = CanonicalScript(script, "script");
        string? alphabet = ScriptAlphabet(canonical);
        if (alphabet == null)
        {
            throw new ArgumentException($"script '{canonical}' does not have a supported generation repertoire.", "script");
        }
        return CharacterStringGenerator.Generate(random, alphabet, count, true, maximumOutputSize, int.MaxValue, false);
    }

    internal static string GenerateLocaleScript(FakeRandom random, int count, string locale, int maximumOutputSize)
    {
        string script = LocaleScript(locale);
        string? alphabet = ScriptAlphabet(script);
        if (alphabet == null)
        {
            throw new ArgumentException($"locale '{locale}' resolves to unsupported script '{script}'.", "locale");
        }
        return CharacterStringGenerator.Generate(random, alphabet, count, true, maximumOutputSize, int.MaxValue, false);
    }

    static void ValidateRange(int minimumScalar, int maximumScalar, bool maximumInclusive)
    {
        ValidateScalar(minimumScalar, "minimumScalar");
        ValidateScalar(maximumScalar, "maximumScalar");
        int minimumOrdinal = ToOrdinal(minimumScalar);
        int maximumOrdinal = ToOrdinal(maximumScalar);
        if (maximumInclusive)
        {
            if (maximumOrdinal < minimumOrdinal)
            {
                throw new ArgumentOutOfRangeException("maximumScalar", maximumScalar, "maximumScalar must be greater than or equal to minimumScalar when it is inclusive.");
            }
            return;
        }
        if (maximumOrdinal <= minimumOrdinal)
        {
            throw new ArgumentOutOfRangeException("maximumScalar", maximumScalar, "maximumScalar must be greater than minimumScalar when it is exclusive.");
        }
    }

    static void ValidateScalar(int scalar, string parameterName)
    {
        if (scalar < 0 || scalar > MaximumScalar || (scalar >= HighSurrogateStart && scalar <= LowSurrogateEnd))
        {
            throw new ArgumentOutOfRangeException(parameterName, scalar, $"{parameterName} must be a Unicode scalar value from U+0000 through U+10FFFF excluding surrogates.");
        }
    }

    static int MaximumUnits(int maximumScalar, bool maximumInclusive)
    {
        int maximumOrdinal = ToOrdinal(maximumScalar);
        int upperOrdinal = maximumInclusive ? maximumOrdinal : maximumOrdinal - 1;
        return FromOrdinal(upperOrdinal) < SupplementaryStart ? 1 : 2;
    }

    static int ToOrdinal(int scalar)
    {
        return scalar < HighSurrogateStart ? scalar : scalar - SurrogateCount;
    }

    static int FromOrdinal(int ordinal)
    {
        return ordinal < HighSurrogateStart ? ordinal : ordinal + SurrogateCount;
    }

    static string LocaleScript(string locale)
    {
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        if (tag.Script != null)
        {
            return CanonicalScript(tag.Script, "locale");
        }
        if (tag.Language == null)
        {
            throw new ArgumentException($"locale '{locale}' does not contain a language or script.", "locale");
        }
        LanguageRegistryEntry? language = LanguageRegistryStore.Current().Language(tag.Language);
        string script = language == null || language.SuppressScript == null
            ? ""
            : language.SuppressScript;
        if (script.Length == 0 && tag.Language == "zh")
        {
            if (tag.Region == "TW" || tag.Region == "HK" || tag.Region == "MO")
            {
                script = "Hant";
            }
            else
            {
                script = "Hans";
            }
        }
        if (script.Length == 0)
        {
            throw new ArgumentException($"locale '{locale}' does not identify a default script; include an explicit script subtag.", "locale");
        }
        return CanonicalScript(script, "locale");
    }

    static string CanonicalScript(string script, string parameterName)
    {
        Guard.NotNullOrWhiteSpace(script, parameterName);
        LanguageRegistryEntry? entry = LanguageRegistryStore.Current().Script(script);
        if (entry == null)
        {
            throw new ArgumentException($"{parameterName} '{script}' is not a registered IANA script subtag.", parameterName);
        }
        if (!string.IsNullOrEmpty(entry.PreferredValue))
        {
            entry = LanguageRegistryStore.Current().Script(entry.PreferredValue);
            if (entry == null)
                throw FakeException.Create("A script preferred value is not registered.");
        }
        return entry.Value;
    }

    static string? ScriptAlphabet(string script)
    {
        switch (script)
        {
            case "Latn":
                return "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            case "Cyrl":
                return "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            case "Grek":
                return "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρσςτυφχψω";
            case "Arab":
                return "ابتثجحخدذرزسشصضطظعغفقكلمنهويءأإآؤئىة";
            case "Hebr":
                return "אבגדהוזחטיךכלםמןנסעףפץצקרשת";
            case "Deva":
                return "अआइईउऊऋएऐओऔकखगघङचछजझञटठडढणतथदधनपफबभमयरलवशषसह";
            case "Thai":
                return "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรลวศษสหฬอฮ";
            case "Beng":
                return "অআইঈউঊঋএঐওঔকখগঘঙচছজঝঞটঠডঢণতথদধনপফবভমযরলশষসহ";
            case "Ethi":
                return "ሀለሐመሠረሰቀበተነአከወዘየደገጠጰጸፀፈፐ";
            case "Tibt":
                return "ཀཁགངཅཆཇཉཏཐདནཔཕབམཙཚཛཝཞཟའཡརལཤསཧཨ";
            case "Thaa":
                return "ހށނރބޅކއވމފދތލގޏސޑޒޓޔޕޖޗޘޙޚޛޜޝޞޟޠޡ";
            case "Armn":
                return "ԱԲԳԴԵԶԷԸԹԺԻԼԽԾԿՀՁՂՃՄՅՆՇՈՉՊՋՌՍՎՏՐՑՒՓՔՕՖաբգդեզէըթժիլխծկհձղճմյնշոչպջռսվտրցւփքօֆ";
            case "Geor":
                return "აბგდევზთიკლმნოპჟრსტუფქღყშჩცძწჭხჯჰ";
            case "Hans":
                return "的一是不了有和人这中大为上个国我以要他时来用们生到作地于出就分对成会可主发年动同工也能下过子说产种面而方后多定行学法所民得经";
            case "Hant":
                return "的一是不了有和人這中大為上個國我以要他時來用們生到作地於出就分對成會可主發年動同工也能下過子說產種面而方後多定行學法所民得經";
            case "Hani":
                return "一二三四五六七八九十天地人日月山水火木金土大小上下左右中本正生年";
            case "Jpan":
                return "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん";
            case "Kore":
                return "가나다라마바사아자차카타파하거너더러머버서어저처커터퍼허고노도로모보소오조초코토포호";
            default:
                return null;
        }
    }
}
