using System;
using System.Collections.Generic;
using System.Globalization;

namespace dns;

internal static class DnsTimeToLiveText
{
    internal static DnsTextNumberResult ParseTimeToLive(string text)
    {
        if (text == null || text.Length == 0)
        {
            return DnsTextNumber.Failure(
                "timeToLive", "TTL text is required.", 0);
        }

        long total = 0;
        int index = 0;
        bool hasUnit = false;
        while (index < text.Length)
        {
            int numberOffset = index;
            while (index < text.Length &&
                text[index] >= '0' && text[index] <= '9')
            {
                index = index + 1;
            }

            if (numberOffset == index)
            {
                return DnsTextNumber.Failure(
                    "timeToLive",
                    "Each TTL component requires a decimal value.",
                    index);
            }

            string numberText = text.Substring(
                numberOffset, index - numberOffset);
            DnsTextNumberResult number = DnsTextNumber.ParseDecimal(
                numberText, uint.MaxValue, "timeToLive");
            if (!number.Success)
            {
                return number;
            }

            long multiplier = 1;
            if (index < text.Length)
            {
                multiplier = ReadMultiplier(text[index]);
                if (multiplier == 0)
                {
                    return DnsTextNumber.Failure(
                        "timeToLive",
                        "A TTL unit must be W, D, H, M, or S.",
                        index);
                }

                hasUnit = true;
                index = index + 1;
            }
            else if (hasUnit)
            {
                return DnsTextNumber.Failure(
                    "timeToLive",
                    "Every component of a compound TTL requires a unit.",
                    numberOffset);
            }

            long component = number.Value * multiplier;
            if (component > uint.MaxValue ||
                total > uint.MaxValue - component)
            {
                return DnsTextNumber.Failure(
                    "timeToLive",
                    "The TTL exceeds the 32-bit DNS field range.",
                    numberOffset);
            }

            total = total + component;
        }

        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = total;
        return result;
    }

    internal static string FormatTimeToLive(uint timeToLive)
    {
        return timeToLive.ToString(CultureInfo.InvariantCulture);
    }

    static long ReadMultiplier(char value)
    {
        char normalized = char.ToUpperInvariant(value);
        switch (normalized)
        {
            case 'W': return 604800;
            case 'D': return 86400;
            case 'H': return 3600;
            case 'M': return 60;
            case 'S': return 1;
            default: return 0;
        }
    }
}
