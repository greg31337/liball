using System;
using System.Globalization;

namespace ftp;

internal static class FtpTimestampCodec
{
    internal static FtpTimestamp ParseTimestampValue(string? text)
    {
        FtpTimestamp timestamp = new FtpTimestamp();
        if (text == null)
        {
            return timestamp;
        }
        if (text.Length < 14 || text.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("An FTP timestamp must contain a bounded fourteen-digit UTC value.", null);
        }

        string fraction = "";
        if (text.Length > 14)
        {
            if (text[14] != '.' || text.Length == 15)
            {
                throw new FtpProtocolException("An FTP fractional timestamp is malformed.", null);
            }
            fraction = text.Substring(15);
            ValidateFractionalDigits(fraction);
        }

        int year = ParseTimestampComponent(text, 0, 4, "year");
        int month = ParseTimestampComponent(text, 4, 2, "month");
        int day = ParseTimestampComponent(text, 6, 2, "day");
        int hour = ParseTimestampComponent(text, 8, 2, "hour");
        int minute = ParseTimestampComponent(text, 10, 2, "minute");
        int second = ParseTimestampComponent(text, 12, 2, "second");
        if (year < 1000 || year > 9999 || month < 1 || month > 12 ||
            hour > 23 || minute > 59 || second > 60)
        {
            throw new FtpProtocolException("An FTP timestamp component is outside its permitted range.", null);
        }

        int maximumDay = DateTime.DaysInMonth(year, month);
        if (day < 1 || day > maximumDay)
        {
            throw new FtpProtocolException("An FTP timestamp contains an invalid Gregorian calendar date.", null);
        }

        bool leapSecond = second == 60;
        int representedSecond = leapSecond ? 59 : second;
        DateTime value = new DateTime(
            year, month, day, hour, minute, representedSecond, DateTimeKind.Utc);
        long fractionalTicks = ParseFractionalTicks(fraction);
        if (fractionalTicks > 0)
        {
            value = value.AddTicks(fractionalTicks);
        }

        timestamp.HasValue = true;
        timestamp.ValueUtc = value;
        timestamp.FractionalSeconds = fraction;
        timestamp.RawValue = text;
        timestamp.LeapSecond = leapSecond;
        return timestamp;
    }

    internal static string FormatTimestampValue(FtpTimestamp? timestamp)
    {
        if (timestamp == null)
        {
            throw new ArgumentNullException("timestamp");
        }
        if (!timestamp.HasValue)
        {
            throw new FtpProtocolException("An unavailable FTP timestamp cannot be formatted.", null);
        }
        if (timestamp.ValueUtc.Kind != DateTimeKind.Utc)
        {
            throw new FtpProtocolException("An FTP timestamp must use UTC DateTime semantics.", null);
        }
        if (timestamp.ValueUtc.Year < 1000)
        {
            throw new FtpProtocolException("An FTP timestamp year must be between 1000 and 9999.", null);
        }
        if (timestamp.LeapSecond && timestamp.ValueUtc.Second != 59)
        {
            throw new FtpProtocolException("An FTP leap-second timestamp must use second 59 as its DateTime representation.", null);
        }

        string fraction = timestamp.FractionalSeconds;
        if (fraction == null)
        {
            throw new FtpProtocolException("FTP timestamp fractional precision cannot be null.", null);
        }
        ValidateFractionalDigitsForFormat(fraction);
        long actualTicks = timestamp.ValueUtc.Ticks % TimeSpan.TicksPerSecond;
        long representedTicks = ParseFractionalTicks(fraction);
        if (actualTicks != representedTicks)
        {
            throw new FtpProtocolException("FTP timestamp fractional digits do not match the UTC DateTime value.", null);
        }

        string secondText = timestamp.LeapSecond ? "60" :
            timestamp.ValueUtc.Second.ToString("D2", CultureInfo.InvariantCulture);
        string text =
            timestamp.ValueUtc.Year.ToString("D4", CultureInfo.InvariantCulture) +
            timestamp.ValueUtc.Month.ToString("D2", CultureInfo.InvariantCulture) +
            timestamp.ValueUtc.Day.ToString("D2", CultureInfo.InvariantCulture) +
            timestamp.ValueUtc.Hour.ToString("D2", CultureInfo.InvariantCulture) +
            timestamp.ValueUtc.Minute.ToString("D2", CultureInfo.InvariantCulture) +
            secondText;
        if (fraction.Length > 0)
        {
            text = text + "." + fraction;
        }
        if (text.Length > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("The formatted FTP timestamp exceeded the control-line size limit.", null);
        }
        return text;
    }

    internal static FtpReply CreateTimestampReply(FtpTimestamp? timestamp)
    {
        FtpReply reply = new FtpReply();
        reply.Code = FtpReplyCodes.FileStatus;
        reply.Text = FormatTimestampValue(timestamp);
        reply.Classification = FtpReplyClassifier.ClassifyCode(reply.Code);
        return reply;
    }

    internal static int ParseTimestampComponent(
        string text, int start, int length, string componentName)
    {
        int value = 0;
        int index = start;
        int end = start + length;
        while (index < end)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                throw new FtpProtocolException(
                    $"The FTP timestamp {componentName} must contain decimal digits.", null);
            }
            value = (value * 10) + (character - '0');
            index++;
        }
        return value;
    }

    internal static void ValidateFractionalDigits(string fraction)
    {
        int index = 0;
        while (index < fraction.Length)
        {
            char character = fraction[index];
            if (character < '0' || character > '9')
            {
                throw new FtpProtocolException(
                    "FTP timestamp fractional seconds must contain only decimal digits.", null);
            }
            index++;
        }
    }

    internal static void ValidateFractionalDigitsForFormat(string fraction)
    {
        ValidateFractionalDigits(fraction);
        if (fraction.Length + 15 > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("FTP timestamp fractional precision exceeded the control-line size limit.", null);
        }
    }

    internal static long ParseFractionalTicks(string fraction)
    {
        long ticks = 0;
        int used = 0;
        while (used < fraction.Length && used < 7)
        {
            ticks = (ticks * 10) + (fraction[used] - '0');
            used++;
        }
        while (used < 7)
        {
            ticks *= 10;
            used++;
        }
        return ticks;
    }
}
