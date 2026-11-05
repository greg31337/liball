using System.Globalization;
using System.Text;

namespace dns;

internal class DnsGenerateRangeResult
{
    internal bool Success;
    internal long Start;
    internal long Stop;
    internal long Step;
    internal long Count;
    internal string Reason = string.Empty;
}

internal class DnsGenerateTextResult
{
    internal bool Success;
    internal string Text = string.Empty;
    internal string Reason = string.Empty;
}

internal class DnsGenerateNumberResult
{
    internal bool Success;
    internal long Value;
}

internal static class DnsGenerateDirective
{
    internal static DnsGenerateRangeResult ParseRange(string text)
    {
        DnsGenerateRangeResult result = new DnsGenerateRangeResult();
        int slash = text.IndexOf('/');
        string bounds = text;
        long step = 1;
        if (slash >= 0)
        {
            if (slash == text.Length - 1 ||
                text.IndexOf('/', slash + 1) >= 0)
            {
                result.Reason = "A generate range has an invalid step.";
                return result;
            }

            bounds = text.Substring(0, slash);
            DnsGenerateNumberResult parsedStep = ParseUnsigned(
                text.Substring(slash + 1), int.MaxValue);
            if (!parsedStep.Success || parsedStep.Value == 0)
            {
                result.Reason = "A generate range step must be positive.";
                return result;
            }

            step = parsedStep.Value;
        }

        int dash = bounds.IndexOf('-');
        if (dash <= 0 || dash == bounds.Length - 1 ||
            bounds.IndexOf('-', dash + 1) >= 0)
        {
            result.Reason = "A generate range must be start-stop or start-stop/step.";
            return result;
        }

        DnsGenerateNumberResult start = ParseUnsigned(
            bounds.Substring(0, dash), int.MaxValue);
        DnsGenerateNumberResult stop = ParseUnsigned(
            bounds.Substring(dash + 1), int.MaxValue);
        if (!start.Success || !stop.Success || start.Value > stop.Value)
        {
            result.Reason = "A generate range requires ordered nonnegative bounds.";
            return result;
        }

        result.Success = true;
        result.Start = start.Value;
        result.Stop = stop.Value;
        result.Step = step;
        result.Count = ((stop.Value - start.Value) / step) + 1;
        return result;
    }

    internal static DnsGenerateTextResult Substitute(
        string template,
        long iterator)
    {
        DnsGenerateTextResult result = new DnsGenerateTextResult();
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < template.Length)
        {
            char value = template[index];
            if (value == '\\' && index + 1 < template.Length &&
                template[index + 1] == '$')
            {
                builder.Append('\\');
                builder.Append('$');
                index = index + 2;
                continue;
            }

            if (value != '$')
            {
                builder.Append(value);
                index = index + 1;
                continue;
            }

            if (index + 1 < template.Length &&
                template[index + 1] == '$')
            {
                builder.Append('\\');
                builder.Append('$');
                index = index + 2;
                continue;
            }

            long offset = 0;
            int width = 0;
            char outputBase = 'd';
            if (index + 1 < template.Length &&
                template[index + 1] == '{')
            {
                int close = template.IndexOf('}', index + 2);
                if (close < 0)
                {
                    result.Reason = "A generate substitution modifier is not closed.";
                    return result;
                }

                string modifier = template.Substring(
                    index + 2, close - index - 2);
                string[] components = modifier.Split(',');
                if (components.Length < 1 || components.Length > 3)
                {
                    result.Reason = "A generate substitution modifier has too many fields.";
                    return result;
                }

                DnsGenerateNumberResult parsedOffset = ParseSigned(
                    components[0], int.MaxValue);
                if (!parsedOffset.Success)
                {
                    result.Reason = "A generate substitution offset is invalid.";
                    return result;
                }

                offset = parsedOffset.Value;
                if (components.Length >= 2)
                {
                    DnsGenerateNumberResult parsedWidth = ParseUnsigned(
                        components[1], 255);
                    if (!parsedWidth.Success)
                    {
                        result.Reason = "A generate substitution width is invalid.";
                        return result;
                    }

                    width = (int)parsedWidth.Value;
                }

                if (components.Length == 3)
                {
                    if (components[2].Length != 1 ||
                        !IsSupportedBase(components[2][0]))
                    {
                        result.Reason = "A generate substitution base is invalid.";
                        return result;
                    }

                    outputBase = components[2][0];
                }

                index = close + 1;
            }
            else
            {
                index = index + 1;
            }

            long generatedValue = iterator + offset;
            if (generatedValue < 0 || generatedValue > int.MaxValue)
            {
                result.Reason = "A generate substitution result is outside its range.";
                return result;
            }

            builder.Append(FormatValue(generatedValue, width, outputBase));
        }

        result.Success = true;
        result.Text = builder.ToString();
        return result;
    }

    static DnsGenerateNumberResult ParseUnsigned(string text, long maximum)
    {
        DnsGenerateNumberResult result = new DnsGenerateNumberResult();
        if (text.Length == 0) return result;
        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9') return result;
            int digit = character - '0';
            if (value > (maximum - digit) / 10) return result;
            value = (value * 10) + digit;
            index = index + 1;
        }

        result.Success = true;
        result.Value = value;
        return result;
    }

    static DnsGenerateNumberResult ParseSigned(string text, long maximum)
    {
        DnsGenerateNumberResult result = new DnsGenerateNumberResult();
        if (text.Length == 0) return result;
        bool negative = text[0] == '-';
        int start = 0;
        if (negative || text[0] == '+') start = 1;
        if (start == text.Length) return result;
        DnsGenerateNumberResult magnitude = ParseUnsigned(
            text.Substring(start), maximum);
        if (!magnitude.Success) return result;
        result.Success = true;
        result.Value = magnitude.Value;
        if (negative) result.Value = -result.Value;
        return result;
    }

    static bool IsSupportedBase(char value)
    {
        return value == 'd' || value == 'o' || value == 'x' ||
            value == 'X' || value == 'n' || value == 'N';
    }

    static string FormatValue(long value, int width, char outputBase)
    {
        bool upper = outputBase == 'X' || outputBase == 'N';
        int radix = 10;
        if (outputBase == 'o') radix = 8;
        if (outputBase == 'x' || outputBase == 'X' ||
            outputBase == 'n' || outputBase == 'N') radix = 16;
        string digits = FormatDigits(value, radix, upper);
        if (digits.Length < width)
        {
            digits = new string('0', width - digits.Length) + digits;
        }

        if (outputBase != 'n' && outputBase != 'N') return digits;
        StringBuilder nibble = new StringBuilder();
        int index = digits.Length - 1;
        while (index >= 0)
        {
            if (nibble.Length > 0) nibble.Append('.');
            nibble.Append(digits[index]);
            index = index - 1;
        }

        return nibble.ToString();
    }

    static string FormatDigits(long value, int radix, bool upper)
    {
        if (radix == 10)
            return value.ToString(CultureInfo.InvariantCulture);
        string alphabet = "0123456789abcdef";
        if (upper) alphabet = "0123456789ABCDEF";
        if (value == 0) return "0";
        StringBuilder reversed = new StringBuilder();
        long remaining = value;
        while (remaining > 0)
        {
            int digit = (int)(remaining % radix);
            reversed.Append(alphabet[digit]);
            remaining = remaining / radix;
        }

        StringBuilder result = new StringBuilder();
        int index = reversed.Length - 1;
        while (index >= 0)
        {
            result.Append(reversed[index]);
            index = index - 1;
        }

        return result.ToString();
    }
}
