using System.Collections.Generic;

namespace dns;

internal static class DnsTextOperations
{
    internal static DnsTextResult Failure(
        DnsDiagnosticCode code,
        string field,
        string reason,
        int offset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.HasOffset = true;
        diagnostic.Offset = offset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsTextResult result = new DnsTextResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    internal static List<string> ReadWhitespaceTokens(string text)
    {
        List<string> tokens = new List<string>();
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index = index + 1;
            }

            int start = index;
            bool quoted = index < text.Length && text[index] == '"';
            bool escaped = false;
            while (index < text.Length)
            {
                char character = text[index];
                if (quoted)
                {
                    if (character == '"' && index > start && !escaped)
                    {
                        index = index + 1;
                        break;
                    }

                    if (character == '\\' && !escaped)
                    {
                        escaped = true;
                    }
                    else
                    {
                        escaped = false;
                    }
                }
                else if (char.IsWhiteSpace(character))
                {
                    break;
                }

                index = index + 1;
            }

            if (start < index)
            {
                tokens.Add(text.Substring(start, index - start));
            }
        }

        return tokens;
    }

    internal static DnsTextResult SuccessText(string text)
    {
        DnsTextResult result = new DnsTextResult();
        result.Success = true;
        result.Text = text;
        return result;
    }

    internal static DnsTextResult FromNumberFailure(
        DnsTextNumberResult number)
    {
        DnsTextResult result = new DnsTextResult();
        int index = 0;
        while (index < number.Diagnostics.Count)
        {
            result.Diagnostics.Add(number.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }
}
