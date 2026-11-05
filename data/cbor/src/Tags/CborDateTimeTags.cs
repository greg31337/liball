using System.Globalization;
using System.Numerics;

namespace cbor;

internal class CborDateTimeTags : ICborTagHandler
{
    internal CborDateTimeTags()
    {
    }

    public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
    {
        if (tagNumber == CborTagNumbers.DateTimeString)
        {
            if (content.Kind != CborValueKind.TextString
                || content.TextStringValue == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_DATE_TIME_TYPE",
                    "The date-time string tag requires a text string."
                );
            }
            if (!IsDateTimeText(content.TextStringValue))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_DATE_TIME_SYNTAX",
                    "The tagged text is not an RFC 3339 date-time supported by DateTimeOffset."
                );
            }
            return CborBuiltInTags.Valid(null);
        }
        if (tagNumber == CborTagNumbers.EpochDateTime)
        {
            if (content.Kind != CborValueKind.UnsignedInteger
                && content.Kind != CborValueKind.NegativeInteger
                && content.Kind != CborValueKind.Float)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_EPOCH_DATE_TIME_TYPE",
                    "The epoch date-time tag requires an integer or floating-point number."
                );
            }
            if (content.Kind == CborValueKind.Float && content.FloatValue == null)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_EPOCH_DATE_TIME_VALUE_MISSING",
                    "The epoch date-time floating-point value is missing."
                );
            }
            if (content.Kind == CborValueKind.Float && content.FloatValue != null)
            {
                double seconds = content.FloatValue.Value;
                double minimum = (double)(System.DateTimeOffset.MinValue.UtcDateTime.Ticks
                    - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
                    / System.TimeSpan.TicksPerSecond;
                double maximum = (double)(System.DateTimeOffset.MaxValue.UtcDateTime.Ticks
                    - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
                    / System.TimeSpan.TicksPerSecond;
                if (double.IsNaN(seconds)
                    || double.IsInfinity(seconds)
                    || seconds < minimum
                    || seconds > maximum)
                {
                    return CborBuiltInTags.Invalid(
                        "CBOR_EPOCH_DATE_TIME_RANGE",
                        "Epoch date-time seconds must be finite and within the supported date range."
                    );
                }
            }
            if (content.Kind == CborValueKind.UnsignedInteger
                || content.Kind == CborValueKind.NegativeInteger)
            {
                long minimumSeconds = (System.DateTimeOffset.MinValue.UtcDateTime.Ticks
                    - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
                    / System.TimeSpan.TicksPerSecond;
                long maximumSeconds = (System.DateTimeOffset.MaxValue.UtcDateTime.Ticks
                    - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
                    / System.TimeSpan.TicksPerSecond;
                if (content.IntegerValue < new BigInteger(minimumSeconds)
                    || content.IntegerValue > new BigInteger(maximumSeconds))
                {
                    return CborBuiltInTags.Invalid(
                        "CBOR_EPOCH_DATE_TIME_RANGE",
                        "Epoch date-time seconds exceed the supported date range."
                    );
                }
            }
            return CborBuiltInTags.Valid(null);
        }
        return CborBuiltInTags.Unhandled();
    }

    public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
    {
        CborTagResult validation = ValidateTag(tagNumber, content);
        if (!validation.Handled || !validation.IsValid)
        {
            return validation;
        }
        if (tagNumber == CborTagNumbers.DateTimeString)
        {
            string text = content.TextStringValue ?? "";
            if (ReadDigits(text, 17, 2) == 60)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_DATE_TIME_NATIVE_RANGE",
                    "DateTimeOffset cannot represent an RFC 3339 leap second."
                );
            }
            int zoneIndex = 19;
            if (zoneIndex < text.Length && text[zoneIndex] == '.')
            {
                zoneIndex = zoneIndex + 1;
                while (zoneIndex < text.Length
                    && text[zoneIndex] >= '0'
                    && text[zoneIndex] <= '9')
                {
                    zoneIndex = zoneIndex + 1;
                }
            }
            if (zoneIndex < text.Length
                && text[zoneIndex] != 'Z'
                && text[zoneIndex] != 'z'
                && ReadDigits(text, zoneIndex + 1, 2) > 14)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_DATE_TIME_NATIVE_RANGE",
                    "DateTimeOffset cannot represent an offset greater than 14 hours."
                );
            }
            System.DateTimeOffset value = System.DateTimeOffset.Parse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );
            return CborBuiltInTags.Valid(value);
        }
        if (content.Kind == CborValueKind.Float && content.FloatValue != null)
        {
            double seconds = content.FloatValue.Value;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_EPOCH_DATE_TIME_NOT_FINITE",
                    "Epoch date-time seconds must be finite."
                );
            }
            double minimum = (double)(System.DateTimeOffset.MinValue.UtcDateTime.Ticks
                - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
                / System.TimeSpan.TicksPerSecond;
            double maximum = (double)(System.DateTimeOffset.MaxValue.UtcDateTime.Ticks
                - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
                / System.TimeSpan.TicksPerSecond;
            if (seconds < minimum || seconds > maximum)
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_EPOCH_DATE_TIME_RANGE",
                    "Epoch date-time seconds exceed the supported date range."
                );
            }
            return CborBuiltInTags.Valid(
                System.DateTimeOffset.UnixEpoch.AddSeconds(seconds)
            );
        }
        BigInteger integerSeconds = content.IntegerValue;
        long minimumSeconds = (System.DateTimeOffset.MinValue.UtcDateTime.Ticks
            - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
            / System.TimeSpan.TicksPerSecond;
        long maximumSeconds = (System.DateTimeOffset.MaxValue.UtcDateTime.Ticks
            - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks)
            / System.TimeSpan.TicksPerSecond;
        if (integerSeconds < new BigInteger(minimumSeconds)
            || integerSeconds > new BigInteger(maximumSeconds))
        {
            return CborBuiltInTags.Invalid(
                "CBOR_EPOCH_DATE_TIME_RANGE",
                "Epoch date-time seconds exceed the supported date range."
            );
        }
        return CborBuiltInTags.Valid(
            System.DateTimeOffset.UnixEpoch.AddSeconds((long)integerSeconds)
        );
    }

    public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
    {
        if (tagNumber != CborTagNumbers.DateTimeString
            && tagNumber != CborTagNumbers.EpochDateTime)
        {
            return CborBuiltInTags.Unhandled();
        }
        if (!CborBuiltInTags.IsRuntimeType(
                nativeValue,
                "System.DateTimeOffset"))
        {
            return CborBuiltInTags.Invalid(
                "CBOR_DATE_TIME_NATIVE_TYPE",
                "Date-time tags require a DateTimeOffset native value."
            );
        }
        System.DateTimeOffset value = (System.DateTimeOffset)nativeValue;
        if (tagNumber == CborTagNumbers.DateTimeString)
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = value.ToUniversalTime().ToString(
                "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                CultureInfo.InvariantCulture
            );
            return CborBuiltInTags.Encoded(text);
        }
        long ticks = value.UtcDateTime.Ticks
            - System.DateTimeOffset.UnixEpoch.UtcDateTime.Ticks;
        if (ticks % System.TimeSpan.TicksPerSecond == 0)
        {
            return CborBuiltInTags.Encoded(
                CreateInteger(ticks / System.TimeSpan.TicksPerSecond)
            );
        }
        CborFloatValue floatValue = new CborFloatValue();
        floatValue.Value = (double)ticks / System.TimeSpan.TicksPerSecond;
        floatValue.Width = CborFloatWidth.Double;
        CborValue floating = new CborValue();
        floating.Kind = CborValueKind.Float;
        floating.FloatValue = floatValue;
        floating.FloatWidth = CborFloatWidth.Double;
        return CborBuiltInTags.Encoded(floating);
    }

    internal CborValue CreateInteger(long value)
    {
        CborValue integer = new CborValue();
        integer.Kind = value < 0
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        integer.IntegerValue = new BigInteger(value);
        return integer;
    }

    internal bool IsDateTimeText(string value)
    {
        if (value.Length < 20
            || value[4] != '-'
            || value[7] != '-'
            || (value[10] != 'T' && value[10] != 't')
            || value[13] != ':'
            || value[16] != ':')
        {
            return false;
        }
        int year = ReadDigits(value, 0, 4);
        int month = ReadDigits(value, 5, 2);
        int day = ReadDigits(value, 8, 2);
        int hour = ReadDigits(value, 11, 2);
        int minute = ReadDigits(value, 14, 2);
        int second = ReadDigits(value, 17, 2);
        if (year < 1
            || month < 1
            || month > 12
            || day < 1
            || day > DaysInMonth(year, month)
            || hour < 0
            || hour > 23
            || minute < 0
            || minute > 59
            || second < 0
            || second > 60)
        {
            return false;
        }
        int index = 19;
        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            int fractionStart = index;
            while (index < value.Length
                && value[index] >= '0'
                && value[index] <= '9')
            {
                index = index + 1;
            }
            if (index == fractionStart)
            {
                return false;
            }
        }
        if (index == value.Length - 1
            && (value[index] == 'Z' || value[index] == 'z'))
        {
            return true;
        }
        if (index + 6 != value.Length
            || (value[index] != '+' && value[index] != '-')
            || value[index + 3] != ':')
        {
            return false;
        }
        int offsetHour = ReadDigits(value, index + 1, 2);
        int offsetMinute = ReadDigits(value, index + 4, 2);
        return offsetHour >= 0
            && offsetHour <= 23
            && offsetMinute >= 0
            && offsetMinute <= 59;
    }

    internal int ReadDigits(string value, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > value.Length - count)
        {
            return -1;
        }
        int result = 0;
        int index = 0;
        while (index < count)
        {
            char character = value[offset + index];
            if (character < '0' || character > '9')
            {
                return -1;
            }
            result = result * 10 + character - '0';
            index = index + 1;
        }
        return result;
    }

    internal int DaysInMonth(int year, int month)
    {
        switch (month)
        {
            case 2:
                bool leap = year % 4 == 0
                    && (year % 100 != 0 || year % 400 == 0);
                return leap ? 29 : 28;
            case 4:
            case 6:
            case 9:
            case 11:
                return 30;
            default:
                return 31;
        }
    }
}
