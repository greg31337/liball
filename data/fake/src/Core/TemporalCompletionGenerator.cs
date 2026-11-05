using System;
using System.Globalization;

namespace fake;

internal static class TemporalCompletionGenerator
{
    internal static TimeOnly Time(FakeRandom random)
    {
        return BoundedTemporalGenerator.TimeOnly(
            random,
            TimeOnly.MinValue,
            TimeOnly.MaxValue,
            DateBoundary.Inclusive);
    }

    internal static TimeSpan Duration(FakeRandom random)
    {
        return BoundedTemporalGenerator.TimeSpan(
            random,
            TimeSpan.Zero,
            TimeSpan.FromDays(7.0),
            DateBoundary.Exclusive);
    }

    internal static DateTimeOffset AnytimeFromPrecision(
        FakeRandom random,
        TemporalPrecision precision)
    {
        return BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
            random,
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
            DateBoundary.Inclusive,
            precision);
    }

    internal static DateTimeOffset BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
        FakeRandom random,
        DateTimeOffset minimum,
        DateTimeOffset maximum,
        DateBoundary boundary,
        TemporalPrecision precision)
    {
        BoundedTemporalGenerator.ValidateDateTimeOffsetRange(minimum, maximum, boundary);
        long unit = DatePresentation.PrecisionUnit(precision);
        long first = minimum.UtcTicks / unit;
        if (minimum.UtcTicks % unit != 0L)
        {
            first = first + 1L;
        }
        long last = maximum.UtcTicks / unit;
        if (boundary == DateBoundary.Exclusive && maximum.UtcTicks % unit == 0L)
        {
            last = last - 1L;
        }
        if (first > last)
        {
            throw new ArgumentOutOfRangeException(
                "maximum",
                maximum,
                "The interval contains no value aligned to the requested precision.");
        }
        long selected = random.LongFromMinimumAndMaximumAndMaximumInclusive(
            first,
            last,
            true);
        return new DateTimeOffset(selected * unit, TimeSpan.Zero);
    }

    internal static int Quarter(FakeRandom random)
    {
        return random.IntFromMinimumAndMaximumAndMaximumInclusive(1, 4, true);
    }

    internal static int QuarterFromDate(DateOnly value)
    {
        return (value.Month - 1) / 3 + 1;
    }

    internal static string LocaleDate(
        GenerationContext context,
        DateOnly value)
    {
        string result = value.ToString("d", Culture(context.CapturedOptions.Locale));
        Guard.OutputSize(result.Length, context.CapturedOptions.MaximumOutputSize, "localeDate");
        return result;
    }

    internal static string LocaleTime(
        GenerationContext context,
        TimeOnly value)
    {
        string result = value.ToString("T", Culture(context.CapturedOptions.Locale));
        Guard.OutputSize(result.Length, context.CapturedOptions.MaximumOutputSize, "localeTime");
        return result;
    }

    internal static string LocaleDateTime(
        GenerationContext context,
        DateTimeOffset value)
    {
        CultureInfo culture = Culture(context.CapturedOptions.Locale);
        string result = value.DateTime.ToString("G", culture) + " " +
            value.ToString("zzz", CultureInfo.InvariantCulture);
        Guard.OutputSize(result.Length, context.CapturedOptions.MaximumOutputSize, "localeDateTime");
        return result;
    }

    internal static string RelativePhrase(
        GenerationContext context,
        DateTimeOffset value)
    {
        long difference = value.UtcTicks - context.ReferenceTime().UtcTicks;
        string language = Language(context.CapturedOptions.Locale);
        string result;
        if (difference == 0L)
        {
            result = Now(language);
        }
        else
        {
            long distance = difference < 0L ? -difference : difference;
            int unit = RelativeUnit(distance);
            long unitTicks = RelativeUnitTicks(unit);
            long quantity = (distance + unitTicks / 2L) / unitTicks;
            if (quantity < 1L)
            {
                quantity = 1L;
            }
            result = Relative(language, quantity, unit, difference < 0L);
        }
        Guard.OutputSize(result.Length, context.CapturedOptions.MaximumOutputSize, "relativePhrase");
        return result;
    }

    internal static DateTimeOffset MaximumOffsetValue(FakeRandom random)
    {
        return OffsetBoundaryValue(random, TimeSpan.FromHours(14.0));
    }

    internal static DateTimeOffset MinimumOffsetValue(FakeRandom random)
    {
        return OffsetBoundaryValue(random, TimeSpan.FromHours(-14.0));
    }

    static DateTimeOffset OffsetBoundaryValue(FakeRandom random, TimeSpan offset)
    {
        DateTimeOffset minimum = DateTimeOffset.MinValue.AddHours(14.0);
        DateTimeOffset maximum = DateTimeOffset.MaxValue.AddHours(-14.0);
        DateTimeOffset value = BoundedTemporalGenerator.DateTimeOffset(
            random,
            minimum,
            maximum,
            DateBoundary.Inclusive);
        return value.ToOffset(offset);
    }

    static CultureInfo Culture(string locale)
    {
        if (string.Equals(locale, "invariant", StringComparison.OrdinalIgnoreCase))
        {
            return CultureInfo.InvariantCulture;
        }
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        return CultureInfo.GetCultureInfo(tag.Normalized);
    }

    static string Language(string locale)
    {
        if (string.Equals(locale, "invariant", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }
        LanguageTag tag = LanguageTagParser.Parse(locale, "locale");
        if (tag.Language == null)
        {
            return "en";
        }
        return tag.Language;
    }

    static int RelativeUnit(long ticks)
    {
        if (ticks < TimeSpan.TicksPerMinute)
        {
            return 0;
        }
        if (ticks < TimeSpan.TicksPerHour)
        {
            return 1;
        }
        if (ticks < TimeSpan.TicksPerDay)
        {
            return 2;
        }
        if (ticks < TimeSpan.TicksPerDay * 7L)
        {
            return 3;
        }
        if (ticks < TimeSpan.TicksPerDay * 30L)
        {
            return 4;
        }
        if (ticks < TimeSpan.TicksPerDay * 365L)
        {
            return 5;
        }
        return 6;
    }

    static long RelativeUnitTicks(int unit)
    {
        if (unit == 0)
        {
            return TimeSpan.TicksPerSecond;
        }
        if (unit == 1)
        {
            return TimeSpan.TicksPerMinute;
        }
        if (unit == 2)
        {
            return TimeSpan.TicksPerHour;
        }
        if (unit == 3)
        {
            return TimeSpan.TicksPerDay;
        }
        if (unit == 4)
        {
            return TimeSpan.TicksPerDay * 7L;
        }
        if (unit == 5)
        {
            return TimeSpan.TicksPerDay * 30L;
        }
        return TimeSpan.TicksPerDay * 365L;
    }

    static string Now(string language)
    {
        if (language == "tr")
        {
            return "şimdi";
        }
        if (language == "fr")
        {
            return "maintenant";
        }
        if (language == "de")
        {
            return "jetzt";
        }
        if (language == "es")
        {
            return "ahora";
        }
        if (language == "pt")
        {
            return "agora";
        }
        if (language == "ja")
        {
            return "今";
        }
        if (language == "zh")
        {
            return "现在";
        }
        if (language == "ko")
        {
            return "지금";
        }
        if (language == "ar")
        {
            return "الآن";
        }
        if (language == "pl")
        {
            return "teraz";
        }
        if (language == "vi")
        {
            return "bây giờ";
        }
        if (language == "da" || language == "nb" || language == "sv")
        {
            return language == "sv" ? "nu" : language == "nb" ? "nå" : "nu";
        }
        if (language == "el")
        {
            return "τώρα";
        }
        if (language == "ru")
        {
            return "сейчас";
        }
        if (language == "nl")
        {
            return "nu";
        }
        if (language == "hi")
        {
            return "अभी";
        }
        if (language == "he")
        {
            return "עכשיו";
        }
        if (language == "cs")
        {
            return "nyní";
        }
        if (language == "it")
        {
            return "adesso";
        }
        if (language == "id")
        {
            return "sekarang";
        }
        if (language == "fa")
        {
            return "اکنون";
        }
        if (language == "fi")
        {
            return "nyt";
        }
        if (language == "th")
        {
            return "ตอนนี้";
        }
        if (language == "uk")
        {
            return "зараз";
        }
        return "now";
    }

    static string Relative(
        string language,
        long quantity,
        int unit,
        bool past)
    {
        string number = quantity.ToString(CultureInfo.InvariantCulture);
        string word = Unit(language, unit, quantity == 1L);
        if (language == "ja" || language == "zh")
        {
            return number + word + (past ? "前" : "後");
        }
        if (language == "ko")
        {
            return number + word + (past ? " 전" : " 후");
        }
        if (language == "fr")
        {
            return past ? "il y a " + number + " " + word : "dans " + number + " " + word;
        }
        if (language == "de")
        {
            return past ? "vor " + number + " " + word : "in " + number + " " + word;
        }
        if (language == "es")
        {
            return past ? "hace " + number + " " + word : "dentro de " + number + " " + word;
        }
        if (language == "pt")
        {
            return past ? "há " + number + " " + word : "em " + number + " " + word;
        }
        if (language == "ar")
        {
            return past ? "قبل " + number + " " + word : "خلال " + number + " " + word;
        }
        if (language == "tr")
        {
            return number + " " + word + (past ? " önce" : " sonra");
        }
        if (language == "pl")
        {
            return past ? number + " " + word + " temu" : "za " + number + " " + word;
        }
        if (language == "vi")
        {
            return past ? number + " " + word + " trước" : "sau " + number + " " + word;
        }
        if (language == "da" || language == "nb")
        {
            return past ? "for " + number + " " + word + " siden" : "om " + number + " " + word;
        }
        if (language == "sv")
        {
            return past ? "för " + number + " " + word + " sedan" : "om " + number + " " + word;
        }
        if (language == "el")
        {
            return past ? "πριν από " + number + " " + word : "σε " + number + " " + word;
        }
        if (language == "ru" || language == "uk")
        {
            return past ? number + " " + word + (language == "ru" ? " назад" : " тому") : "через " + number + " " + word;
        }
        if (language == "nl")
        {
            return past ? number + " " + word + " geleden" : "over " + number + " " + word;
        }
        if (language == "hi")
        {
            return past ? number + " " + word + " पहले" : number + " " + word + " में";
        }
        if (language == "he")
        {
            return past ? "לפני " + number + " " + word : "בעוד " + number + " " + word;
        }
        if (language == "cs")
        {
            return past ? "před " + number + " " + word : "za " + number + " " + word;
        }
        if (language == "it")
        {
            return past ? number + " " + word + " fa" : "tra " + number + " " + word;
        }
        if (language == "id")
        {
            return past ? number + " " + word + " yang lalu" : "dalam " + number + " " + word;
        }
        if (language == "fa")
        {
            return number + " " + word + (past ? " پیش" : " دیگر");
        }
        if (language == "fi")
        {
            return number + " " + word + (past ? " sitten" : " kuluttua");
        }
        if (language == "th")
        {
            return past ? number + " " + word + "ที่แล้ว" : "ในอีก " + number + " " + word;
        }
        return past ? number + " " + word + " ago" : "in " + number + " " + word;
    }

    static string Unit(string language, int unit, bool singular)
    {
        if (language == "fr")
        {
            string[] values = new string[] { "seconde", "minute", "heure", "jour", "semaine", "mois", "an" };
            string value = values[unit];
            if (!singular && unit != 5)
            {
                value = value + "s";
            }
            return value;
        }
        if (language == "de")
        {
            string[] singulars = new string[] { "Sekunde", "Minute", "Stunde", "Tag", "Woche", "Monat", "Jahr" };
            string[] plurals = new string[] { "Sekunden", "Minuten", "Stunden", "Tage", "Wochen", "Monate", "Jahre" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "es" || language == "pt")
        {
            string[] spanish = new string[] { "segundo", "minuto", "hora", "día", "semana", "mes", "año" };
            string[] portuguese = new string[] { "segundo", "minuto", "hora", "dia", "semana", "mês", "ano" };
            string value = language == "es" ? spanish[unit] : portuguese[unit];
            if (!singular)
            {
                value = value + (value == "mês" ? "es" : "s");
            }
            return value;
        }
        if (language == "ja")
        {
            string[] values = new string[] { "秒", "分", "時間", "日", "週間", "か月", "年" };
            return values[unit];
        }
        if (language == "zh")
        {
            string[] values = new string[] { "秒", "分钟", "小时", "天", "周", "个月", "年" };
            return values[unit];
        }
        if (language == "ko")
        {
            string[] values = new string[] { "초", "분", "시간", "일", "주", "개월", "년" };
            return values[unit];
        }
        if (language == "ar")
        {
            string[] values = new string[] { "ثانية", "دقيقة", "ساعة", "يوم", "أسبوع", "شهر", "سنة" };
            return values[unit];
        }
        if (language == "tr")
        {
            string[] values = new string[] { "saniye", "dakika", "saat", "gün", "hafta", "ay", "yıl" };
            return values[unit];
        }
        if (language == "pl")
        {
            string[] singulars = new string[] { "sekundę", "minutę", "godzinę", "dzień", "tydzień", "miesiąc", "rok" };
            string[] plurals = new string[] { "sekundy", "minuty", "godziny", "dni", "tygodnie", "miesiące", "lata" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "vi")
        {
            string[] values = new string[] { "giây", "phút", "giờ", "ngày", "tuần", "tháng", "năm" };
            return values[unit];
        }
        if (language == "da" || language == "nb" || language == "sv")
        {
            string[] danish = new string[] { "sekund", "minut", "time", "dag", "uge", "måned", "år" };
            string[] norwegian = new string[] { "sekund", "minutt", "time", "dag", "uke", "måned", "år" };
            string[] swedish = new string[] { "sekund", "minut", "timme", "dag", "vecka", "månad", "år" };
            string[] values = language == "da" ? danish : language == "nb" ? norwegian : swedish;
            if (singular || unit == 6)
            {
                return values[unit];
            }
            string[] endings = language == "nb"
                ? new string[] { "er", "er", "r", "er", "r", "er", "" }
                : new string[] { "er", "ter", "r", "e", "r", "er", "" };
            return values[unit] + endings[unit];
        }
        if (language == "el")
        {
            string[] singulars = new string[] { "δευτερόλεπτο", "λεπτό", "ώρα", "ημέρα", "εβδομάδα", "μήνα", "έτος" };
            string[] plurals = new string[] { "δευτερόλεπτα", "λεπτά", "ώρες", "ημέρες", "εβδομάδες", "μήνες", "έτη" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "ru")
        {
            string[] singulars = new string[] { "секунду", "минуту", "час", "день", "неделю", "месяц", "год" };
            string[] plurals = new string[] { "секунды", "минуты", "часа", "дня", "недели", "месяца", "года" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "uk")
        {
            string[] singulars = new string[] { "секунду", "хвилину", "годину", "день", "тиждень", "місяць", "рік" };
            string[] plurals = new string[] { "секунди", "хвилини", "години", "дні", "тижні", "місяці", "роки" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "nl")
        {
            string[] values = new string[] { "seconde", "minuut", "uur", "dag", "week", "maand", "jaar" };
            if (singular || unit == 2 || unit == 6)
            {
                return values[unit];
            }
            return values[unit] + (unit == 1 ? "en" : unit == 3 ? "en" : "s");
        }
        if (language == "hi")
        {
            string[] values = new string[] { "सेकंड", "मिनट", "घंटा", "दिन", "सप्ताह", "महीना", "वर्ष" };
            return values[unit];
        }
        if (language == "he")
        {
            string[] singulars = new string[] { "שנייה", "דקה", "שעה", "יום", "שבוע", "חודש", "שנה" };
            string[] plurals = new string[] { "שניות", "דקות", "שעות", "ימים", "שבועות", "חודשים", "שנים" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "cs")
        {
            string[] singulars = new string[] { "sekundu", "minutu", "hodinu", "den", "týden", "měsíc", "rok" };
            string[] plurals = new string[] { "sekundy", "minuty", "hodiny", "dny", "týdny", "měsíce", "roky" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "it")
        {
            string[] singulars = new string[] { "secondo", "minuto", "ora", "giorno", "settimana", "mese", "anno" };
            string[] plurals = new string[] { "secondi", "minuti", "ore", "giorni", "settimane", "mesi", "anni" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "id")
        {
            string[] values = new string[] { "detik", "menit", "jam", "hari", "minggu", "bulan", "tahun" };
            return values[unit];
        }
        if (language == "fa")
        {
            string[] values = new string[] { "ثانیه", "دقیقه", "ساعت", "روز", "هفته", "ماه", "سال" };
            return values[unit];
        }
        if (language == "fi")
        {
            string[] singulars = new string[] { "sekunti", "minuutti", "tunti", "päivä", "viikko", "kuukausi", "vuosi" };
            string[] plurals = new string[] { "sekuntia", "minuuttia", "tuntia", "päivää", "viikkoa", "kuukautta", "vuotta" };
            return singular ? singulars[unit] : plurals[unit];
        }
        if (language == "th")
        {
            string[] values = new string[] { "วินาที", "นาที", "ชั่วโมง", "วัน", "สัปดาห์", "เดือน", "ปี" };
            return values[unit];
        }
        string[] english = new string[] { "second", "minute", "hour", "day", "week", "month", "year" };
        return singular ? english[unit] : english[unit] + "s";
    }
}
