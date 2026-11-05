using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace fake;

internal static class DatePresentation
{
    internal static string MonthName(
        GenerationContext context,
        FakeRandom random,
        DateNameStyle style)
    {
        string[] values = Names(context, MonthField(style), 12);
        return DateName(context, values[random.IntFromMaximumExclusive(values.Length)]);
    }

    internal static string MonthName(
        GenerationContext context,
        int month,
        DateNameStyle style)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(
                "month",
                month,
                "month must be between 1 and 12, inclusive.");
        }
        string[] values = Names(context, MonthField(style), 12);
        return DateName(context, values[month - 1]);
    }

    internal static string WeekdayName(
        GenerationContext context,
        FakeRandom random,
        DateNameStyle style)
    {
        string[] values = Names(context, WeekdayField(style), 7);
        return DateName(context, values[random.IntFromMaximumExclusive(values.Length)]);
    }

    internal static string WeekdayName(
        GenerationContext context,
        DayOfWeek weekday,
        DateNameStyle style)
    {
        int index = (int)weekday;
        if (index < 0 || index > 6)
        {
            throw new ArgumentOutOfRangeException(
                "weekday",
                weekday,
                "weekday must be a defined DayOfWeek value.");
        }
        string[] values = Names(context, WeekdayField(style), 7);
        return DateName(context, values[index]);
    }

    internal static DateOrder Order(GenerationContext context)
    {
        string value = StringValue(context, "date.order");
        if (value == "mdy")
        {
            return DateOrder.MonthDayYear;
        }
        if (value == "dmy")
        {
            return DateOrder.DayMonthYear;
        }
        if (value == "ymd")
        {
            return DateOrder.YearMonthDay;
        }
        throw FakeException.Create("Locale field 'date.order' must be 'mdy', 'dmy', or 'ymd'.");
    }

    internal static DateTimeOffset Truncate(
        DateTimeOffset value,
        TemporalPrecision precision)
    {
        long unit = PrecisionUnit(precision);
        long ticks = value.Ticks - value.Ticks % unit;
        return new DateTimeOffset(ticks, value.Offset);
    }

    internal static string Rfc3339(
        DateTimeOffset value,
        TemporalPrecision precision,
        int maximumOutputSize)
    {
        DateTimeOffset truncated = Truncate(value, precision);
        StringBuilder result = new StringBuilder();
        result.Append(truncated.ToString(
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture));
        AppendFraction(result, truncated, precision);
        AppendOffset(result, truncated.Offset);
        string output = result.ToString();
        Guard.OutputSize(output.Length, maximumOutputSize, "rfc3339");
        return output;
    }

    static string DateName(GenerationContext context, string value)
    {
        Guard.OutputSize(
            value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "dateName");
        return value;
    }

    static string MonthField(DateNameStyle style)
    {
        if (style == DateNameStyle.Wide)
        {
            return "date.month.wide";
        }
        if (style == DateNameStyle.Abbreviated)
        {
            return "date.month.abbreviated";
        }
        throw new ArgumentOutOfRangeException(
            "style",
            style,
            "style must be Wide or Abbreviated.");
    }

    static string WeekdayField(DateNameStyle style)
    {
        if (style == DateNameStyle.Wide)
        {
            return "date.weekday.wide";
        }
        if (style == DateNameStyle.Abbreviated)
        {
            return "date.weekday.abbreviated";
        }
        throw new ArgumentOutOfRangeException(
            "style",
            style,
            "style must be Wide or Abbreviated.");
    }

    static string[] Names(
        GenerationContext context,
        string field,
        int expectedCount)
    {
        object resolved = Resolve(context, field);
        string json = JsonSerializer.Serialize(resolved);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() != expectedCount)
            {
                throw FakeException.Create($"Locale field '{field}' must contain exactly {expectedCount} strings.");
            }
            string[] result = new string[expectedCount];
            int index = 0;
            foreach (JsonElement item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                {
                    throw FakeException.Create($"Locale field '{field}' must contain exactly {expectedCount} nonempty strings.");
                }
                result[index] = JsonText.Required(item, field);
                index++;
            }
            return result;
        }
    }

    static string StringValue(GenerationContext context, string field)
    {
        object resolved = Resolve(context, field);
        string json = JsonSerializer.Serialize(resolved);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(root.GetString()))
            {
                throw FakeException.Create($"Locale field '{field}' must be a nonempty string.");
            }
            return JsonText.Required(root, field);
        }
    }

    static object Resolve(GenerationContext context, string field)
    {
        return LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            field,
            context.LocaleValues());
    }

    internal static long PrecisionUnit(TemporalPrecision precision)
    {
        if (precision == TemporalPrecision.Seconds)
        {
            return TimeSpan.TicksPerSecond;
        }
        if (precision == TemporalPrecision.Milliseconds)
        {
            return TimeSpan.TicksPerMillisecond;
        }
        if (precision == TemporalPrecision.Microseconds)
        {
            return 10L;
        }
        if (precision == TemporalPrecision.Ticks)
        {
            return 1L;
        }
        throw new ArgumentOutOfRangeException(
            "precision",
            precision,
            "precision must be Seconds, Milliseconds, Microseconds, or Ticks.");
    }

    static void AppendFraction(
        StringBuilder result,
        DateTimeOffset value,
        TemporalPrecision precision)
    {
        long fractionTicks = value.Ticks % TimeSpan.TicksPerSecond;
        if (precision == TemporalPrecision.Seconds)
        {
            return;
        }
        result.Append('.');
        if (precision == TemporalPrecision.Milliseconds)
        {
            result.Append((fractionTicks / TimeSpan.TicksPerMillisecond).ToString(
                "D3",
                CultureInfo.InvariantCulture));
            return;
        }
        if (precision == TemporalPrecision.Microseconds)
        {
            result.Append((fractionTicks / 10L).ToString(
                "D6",
                CultureInfo.InvariantCulture));
            return;
        }
        if (precision == TemporalPrecision.Ticks)
        {
            result.Append(fractionTicks.ToString(
                "D7",
                CultureInfo.InvariantCulture));
            return;
        }
        PrecisionUnit(precision);
    }

    static void AppendOffset(StringBuilder result, TimeSpan offset)
    {
        if (offset == TimeSpan.Zero)
        {
            result.Append('Z');
            return;
        }
        long ticks = offset.Ticks;
        if (ticks < 0L)
        {
            result.Append('-');
            ticks = -ticks;
        }
        else
        {
            result.Append('+');
        }
        TimeSpan absolute = new TimeSpan(ticks);
        result.Append(absolute.Hours.ToString("D2", CultureInfo.InvariantCulture));
        result.Append(':');
        result.Append(absolute.Minutes.ToString("D2", CultureInfo.InvariantCulture));
    }
}
