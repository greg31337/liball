namespace sip;

internal static class SipMessageSummarySyntax
{
    internal static string? ValidateSummaryText(string? text, bool compatibleLineEndings,
        int maximumLineBytes)
    {
        if (text == null || text.Length == 0) return "A message-summary body is required.";
        int characterIndex = 0;
        while (characterIndex < text.Length)
        {
            if (text[characterIndex] == '\r'
                && (characterIndex + 1 >= text.Length || text[characterIndex + 1] != '\n'))
                return "A message-summary body contains a bare carriage return.";
            if (text[characterIndex] == '\n'
                && (characterIndex == 0 || text[characterIndex - 1] != '\r')
                && !compatibleLineEndings)
                return "Every message-summary line must end with CRLF.";
            characterIndex = characterIndex + 1;
        }
        if (!compatibleLineEndings
            && (text.Length < 2 || text[text.Length - 2] != '\r' || text[text.Length - 1] != '\n'))
            return "Every message-summary line must end with CRLF.";
        string normalized = text.Replace("\r\n", "\n", System.StringComparison.Ordinal);
        string[] lines = normalized.Split('\n');
        int contentCount = lines.Length;
        if (contentCount > 0 && lines[contentCount - 1].Length == 0) contentCount = contentCount - 1;
        if (contentCount == 0) return "A message-summary body is required.";
        int lineIndex = 0;
        while (lineIndex < contentCount)
        {
            if (System.Text.Encoding.UTF8.GetByteCount(lines[lineIndex]) > maximumLineBytes)
                return "A message-summary line exceeds the configured maximum size.";
            lineIndex = lineIndex + 1;
        }
        if (!IsStatusLine(lines[0])) return "The first message-summary line must be Messages-Waiting: yes or no.";
        int index = 1;
        if (index < contentCount && StartsWithIgnoreCase(lines[index], "Message-Account:"))
        {
            if (!IsAccountLine(lines[index])) return "The Message-Account line is invalid.";
            index = index + 1;
        }
        bool extensionHeaders = false;
        while (index < contentCount)
        {
            string line = lines[index];
            if (line.Length == 0)
            {
                if (extensionHeaders) return "A message-summary body contains an unexpected empty line.";
                extensionHeaders = true;
            }
            else if (extensionHeaders)
            {
                if (!IsExtensionHeader(line)) return "A message-summary extension header is invalid.";
            }
            else if (!IsSummaryLine(line)) return "A message-summary count line is invalid.";
            index = index + 1;
        }
        return null;
    }

    internal static string NormalizeSummaryText(string text)
    {
        string normalized = text.Replace("\r\n", "\n", System.StringComparison.Ordinal);
        string[] lines = normalized.Split('\n');
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int count = lines.Length;
        if (count > 0 && lines[count - 1].Length == 0) count = count - 1;
        int index = 0;
        while (index < count)
        {
            builder.Append(lines[index]);
            builder.Append("\r\n");
            index = index + 1;
        }
        return builder.ToString();
    }

    static bool IsStatusLine(string line)
    {
        int colon = line.IndexOf(':');
        if (colon < 0 || !SipAscii.EqualsIgnoreCase(line.Substring(0, colon).Trim(), "Messages-Waiting"))
            return false;
        string value = line.Substring(colon + 1).Trim();
        return SipAscii.EqualsIgnoreCase(value, "yes") || SipAscii.EqualsIgnoreCase(value, "no");
    }

    static bool IsAccountLine(string line)
    {
        int colon = line.IndexOf(':');
        if (colon < 0) return false;
        string value = line.Substring(colon + 1).Trim();
        if (value.Length == 0 || value.IndexOfAny(new char[] { ' ', '\t', '<', '>' }) >= 0)
            return false;
        return value.IndexOf(':') > 0;
    }

    static bool IsSummaryLine(string line)
    {
        int colon = line.IndexOf(':');
        if (colon <= 0 || !SipSdpSyntax.IsToken(line.Substring(0, colon).Trim())) return false;
        string value = line.Substring(colon + 1).Trim();
        int space = value.IndexOf(' ');
        string counts = space < 0 ? value : value.Substring(0, space);
        if (!IsCountPair(counts)) return false;
        if (space < 0) return true;
        string urgent = value.Substring(space + 1).Trim();
        return urgent.Length >= 3 && urgent[0] == '(' && urgent[urgent.Length - 1] == ')'
            && IsCountPair(urgent.Substring(1, urgent.Length - 2));
    }

    static bool IsCountPair(string value)
    {
        int slash = value.IndexOf('/');
        return slash > 0 && slash == value.LastIndexOf('/') && slash < value.Length - 1
            && IsMessageCount(value.Substring(0, slash))
            && IsMessageCount(value.Substring(slash + 1));
    }

    static bool IsMessageCount(string value)
    {
        if (!SipSdpSyntax.IsDigits(value)) return false;
        int first = 0;
        while (first < value.Length - 1 && value[first] == '0') first = first + 1;
        string significant = value.Substring(first);
        return significant.Length < 10
            || significant.Length == 10 && string.CompareOrdinal(significant, "4294967295") <= 0;
    }

    static bool IsExtensionHeader(string line)
    {
        int colon = line.IndexOf(':');
        return colon > 0 && SipAscii.IsToken(line.Substring(0, colon))
            && line.IndexOf('\0') < 0;
    }

    static bool StartsWithIgnoreCase(string value, string prefix)
    {
        if (value.Length < prefix.Length) return false;
        return SipAscii.EqualsIgnoreCase(value.Substring(0, prefix.Length), prefix);
    }
}
