using System.Collections.Generic;

namespace dns;

internal class DnsTextNumberResult
{
    internal bool Success;
    internal long Value;
    internal List<DnsDiagnostic> Diagnostics = new List<DnsDiagnostic>();
}

internal static class DnsTextNumber
{
    internal static DnsTextNumberResult ParseDecimal(
        string text,
        long maximum,
        string field)
    {
        if (text == null || text.Length == 0)
        {
            return Failure(field, "A decimal value is required.", 0);
        }

        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return Failure(
                    field,
                    "The value must contain only decimal digits.",
                    index);
            }

            int digit = character - '0';
            if (value > (maximum - digit) / 10)
            {
                return Failure(
                    field,
                    "The decimal value is outside the supported range.",
                    index);
            }

            value = (value * 10) + digit;
            index = index + 1;
        }

        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Success = true;
        result.Value = value;
        return result;
    }

    internal static DnsTextNumberResult Failure(
        string field,
        string reason,
        int offset)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidText;
        diagnostic.HasOffset = true;
        diagnostic.Offset = offset;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        DnsTextNumberResult result = new DnsTextNumberResult();
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
