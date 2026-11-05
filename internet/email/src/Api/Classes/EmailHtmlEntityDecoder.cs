using System;
using System.Net;
using System.Text;

namespace email;
/// <summary>
/// Incrementally decodes HTML entity.
/// </summary>
public class EmailHtmlEntityDecoder : IEmailHtmlEntityDecoderContract
{
    internal static string DecodeImplementationWithValue(string value)
    {
        return DecodeResultImplementationWithValueAndMaximumOutputCharacters(value, EmailHtmlLimitsConstants.DefaultMaxOutputCharacters).GetText();
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string DecodeWithValue(string value)
    {
        return EmailHtmlEntityDecoder.DecodeImplementationWithValue(value);
    }

    internal static string DecodeImplementationWithValueAndMaximumOutputCharacters(string value, int maximumOutputCharacters)
    {
        return DecodeResultImplementationWithValueAndMaximumOutputCharacters(value, maximumOutputCharacters).GetText();
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "maximumOutputCharacters">The maximum output characters used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string DecodeWithValueAndMaximumOutputCharacters(string value, int maximumOutputCharacters)
    {
        return EmailHtmlEntityDecoder.DecodeImplementationWithValueAndMaximumOutputCharacters(value, maximumOutputCharacters);
    }

    internal static string DecodeImplementationWithValueAndLimits(string value, EmailHtmlLimits limits)
    {
        return DecodeResultImplementationWithValueAndLimits(value, limits).GetText();
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "limits">The limits used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string DecodeWithValueAndLimits(string value, EmailHtmlLimits limits)
    {
        return EmailHtmlEntityDecoder.DecodeImplementationWithValueAndLimits(value, limits);
    }

    internal static EmailHtmlEntityDecodeResult DecodeResultImplementationWithValue(string value)
    {
        return DecodeResultImplementationWithValueAndMaximumOutputCharacters(value, EmailHtmlLimitsConstants.DefaultMaxOutputCharacters);
    }

    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailHtmlEntityDecodeResult DecodeResultWithValue(string value)
    {
        return EmailHtmlEntityDecoder.DecodeResultImplementationWithValue(value);
    }

    internal static EmailHtmlEntityDecodeResult DecodeResultImplementationWithValueAndMaximumOutputCharacters(string value, int maximumOutputCharacters)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (maximumOutputCharacters < 1)
            throw new ArgumentOutOfRangeException("maximumOutputCharacters");
        if (value.Length > EmailHtmlLimitsConstants.DefaultMaxInputCharacters)
            throw new EmailLimitException("MaxHtmlInputCharacters", EmailHtmlLimitsConstants.DefaultMaxInputCharacters, value.Length);
        return DecodeCore(value, maximumOutputCharacters);
    }

    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "maximumOutputCharacters">The maximum output characters used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailHtmlEntityDecodeResult DecodeResultWithValueAndMaximumOutputCharacters(string value, int maximumOutputCharacters)
    {
        return EmailHtmlEntityDecoder.DecodeResultImplementationWithValueAndMaximumOutputCharacters(value, maximumOutputCharacters);
    }

    internal static EmailHtmlEntityDecodeResult DecodeResultImplementationWithValueAndLimits(string value, EmailHtmlLimits limits)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (limits == null)
            throw new ArgumentNullException("limits");
        EmailHtmlLimits snapshot = limits.Clone();
        if (value.Length > snapshot.GetMaxInputCharacters())
            throw new EmailLimitException("MaxHtmlInputCharacters", snapshot.GetMaxInputCharacters(), value.Length);
        return DecodeCore(value, snapshot.GetMaxOutputCharacters());
    }

    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "limits">The limits used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailHtmlEntityDecodeResult DecodeResultWithValueAndLimits(string value, EmailHtmlLimits limits)
    {
        return EmailHtmlEntityDecoder.DecodeResultImplementationWithValueAndLimits(value, limits);
    }

    static EmailHtmlEntityDecodeResult DecodeCore(string value, int maximumOutputCharacters)
    {
        StringBuilder result = new StringBuilder();
        int decodedCount = 0;
        int malformedCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\0')
            {
                AppendBoundedWithResultAndValueAndMaximumOutputCharactersUsingStringBuilderAndStringAndInt32(result, "\uFFFD", maximumOutputCharacters);
                malformedCount++;
                index++;
                continue;
            }

            if (value[index] != '&')
            {
                AppendBoundedWithResultAndValueAndMaximumOutputCharactersUsingStringBuilderAndCharAndInt32(result, value[index], maximumOutputCharacters);
                index++;
                continue;
            }

            EmailHtmlEntityMatch match = Match(value, index);
            AppendBoundedWithResultAndValueAndMaximumOutputCharactersUsingStringBuilderAndStringAndInt32(result, match.GetText(), maximumOutputCharacters);
            if (match.WasDecoded())
                decodedCount++;
            if (match.WasMalformed())
                malformedCount++;
            index += match.GetConsumedCharacters();
        }

        EmailIssueList issues = new EmailIssueList();
        if (malformedCount > 0)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.HtmlEntityMalformed, "HTML text contains " + malformedCount + " malformed or invalid character reference(s).", null, null, null, "Invalid references were preserved when possible; invalid scalar values and null characters were replaced with U+FFFD."));
        }

        return new EmailHtmlEntityDecodeResult(result.ToString(), decodedCount, malformedCount, issues);
    }

    static EmailHtmlEntityMatch Match(string value, int start)
    {
        if (start + 1 >= value.Length)
            return new EmailHtmlEntityMatch("&", 1, false, false);
        if (value[start + 1] == '#')
            return MatchNumeric(value, start);
        int index = start + 1;
        int maximum = start + 65;
        while (index < value.Length && index < maximum && IsAsciiAlphaNumeric(value[index]))
            index++;
        if (index == start + 1 || index >= value.Length || value[index] != ';')
            return new EmailHtmlEntityMatch("&", 1, false, false);
        string candidate = value.Substring(start, index - start + 1);
        string decoded = WebUtility.HtmlDecode(candidate);
        if (string.Equals(candidate, decoded, StringComparison.Ordinal))
            return new EmailHtmlEntityMatch(candidate, candidate.Length, false, true);
        return new EmailHtmlEntityMatch(decoded, candidate.Length, true, false);
    }

    static EmailHtmlEntityMatch MatchNumeric(string value, int start)
    {
        int index = start + 2;
        bool hexadecimal = false;
        if (index < value.Length && (value[index] == 'x' || value[index] == 'X'))
        {
            hexadecimal = true;
            index++;
        }

        int digitStart = index;
        int scalar = 0;
        bool overflow = false;
        while (index < value.Length)
        {
            int digit = GetDigit(value[index], hexadecimal);
            if (digit < 0)
                break;
            if (scalar > 1114111 / (hexadecimal ? 16 : 10))
                overflow = true;
            if (!overflow)
                scalar = scalar * (hexadecimal ? 16 : 10) + digit;
            if (scalar > 1114111)
                overflow = true;
            index++;
        }

        if (index == digitStart)
            return new EmailHtmlEntityMatch("&", 1, false, true);
        bool semicolon = index < value.Length && value[index] == ';';
        int consumed = index - start + (semicolon ? 1 : 0);
        bool malformed = !semicolon;
        if (overflow || scalar == 0 || scalar >= 55296 && scalar <= 57343)
        {
            return new EmailHtmlEntityMatch("\uFFFD", consumed, true, true);
        }

        int mapped = MapControlReference(scalar);
        if (mapped != scalar)
            malformed = true;
        return new EmailHtmlEntityMatch(char.ConvertFromUtf32(mapped), consumed, true, malformed);
    }

    static int MapControlReference(int value)
    {
        switch (value)
        {
            case 128:
                return 8364;
            case 130:
                return 8218;
            case 131:
                return 402;
            case 132:
                return 8222;
            case 133:
                return 8230;
            case 134:
                return 8224;
            case 135:
                return 8225;
            case 136:
                return 710;
            case 137:
                return 8240;
            case 138:
                return 352;
            case 139:
                return 8249;
            case 140:
                return 338;
            case 142:
                return 381;
            case 145:
                return 8216;
            case 146:
                return 8217;
            case 147:
                return 8220;
            case 148:
                return 8221;
            case 149:
                return 8226;
            case 150:
                return 8211;
            case 151:
                return 8212;
            case 152:
                return 732;
            case 153:
                return 8482;
            case 154:
                return 353;
            case 155:
                return 8250;
            case 156:
                return 339;
            case 158:
                return 382;
            case 159:
                return 376;
            default:
                return value;
        }
    }

    static int GetDigit(char value, bool hexadecimal)
    {
        if (value >= '0' && value <= '9')
            return value - '0';
        if (!hexadecimal)
            return -1;
        if (value >= 'a' && value <= 'f')
            return value - 'a' + 10;
        if (value >= 'A' && value <= 'F')
            return value - 'A' + 10;
        return -1;
    }

    static bool IsAsciiAlphaNumeric(char value)
    {
        return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value >= '0' && value <= '9';
    }

    static void AppendBoundedWithResultAndValueAndMaximumOutputCharactersUsingStringBuilderAndCharAndInt32(StringBuilder result, char value, int maximumOutputCharacters)
    {
        if (result.Length >= maximumOutputCharacters)
            throw new EmailLimitException("MaxHtmlOutputCharacters", maximumOutputCharacters, (long)result.Length + 1);
        result.Append(value);
    }

    static void AppendBoundedWithResultAndValueAndMaximumOutputCharactersUsingStringBuilderAndStringAndInt32(StringBuilder result, string value, int maximumOutputCharacters)
    {
        if ((long)result.Length + value.Length > maximumOutputCharacters)
            throw new EmailLimitException("MaxHtmlOutputCharacters", maximumOutputCharacters, (long)result.Length + value.Length);
        result.Append(value);
    }
}
