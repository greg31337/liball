using System;

namespace fake;

internal static class PhoneWordGenerator
{
    internal static void ValidateRequest(
        PhoneLocaleMetadata metadata,
        PhoneNumberFormat format)
    {
        if (format == PhoneNumberFormat.E164)
        {
            throw new ArgumentException(
                "E.164 output cannot contain alphabetic phone-word characters.",
                "format");
        }
        if (format != PhoneNumberFormat.National &&
            format != PhoneNumberFormat.International)
        {
            throw new ArgumentOutOfRangeException(
                "format",
                format,
                "format must be national or international for a phone word.");
        }
        if (!metadata.SupportsPhoneWords)
        {
            throw FakeException.Create(
                "Phone-word formatting is unsupported by the resolved phone locale catalog.");
        }
    }

    internal static string Render(
        PhoneRecord record,
        int maximumOutputSize)
    {
        int digitCount = 0;
        int index = 0;
        while (index < record.Number.Length)
        {
            if (Digit(record.Number[index]))
            {
                digitCount = digitCount + 1;
            }
            index = index + 1;
        }
        int subscriberStart = digitCount - record.SubscriberNumber.Length;
        int seenDigits = 0;
        char[] result = record.Number.ToCharArray();
        index = 0;
        while (index < result.Length)
        {
            if (Digit(result[index]))
            {
                if (seenDigits >= subscriberStart)
                {
                    result[index] = KeypadLetter(result[index]);
                }
                seenDigits = seenDigits + 1;
            }
            index = index + 1;
        }
        Guard.OutputSize(result.Length, maximumOutputSize, "phoneWord");
        return new string(result);
    }

    static bool Digit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static char KeypadLetter(char value)
    {
        switch (value)
        {
            case '2':
                return 'A';
            case '3':
                return 'D';
            case '4':
                return 'G';
            case '5':
                return 'J';
            case '6':
                return 'M';
            case '7':
                return 'P';
            case '8':
                return 'T';
            case '9':
                return 'W';
            default:
                return value;
        }
    }
}
