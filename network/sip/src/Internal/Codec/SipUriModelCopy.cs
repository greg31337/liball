namespace sip;

internal static class SipUriModelCopy
{
    internal static SipUri CreateNormalizedCopy(SipUri? source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("uri");
        }

        SipUri copy = new SipUri();
        copy.Scheme = source.Scheme;
        copy.SchemeToken = NormalizeSchemeToken(source);
        copy.User = NormalizeUser(source);
        copy.Password = source.Password;
        CopyUserComparison(source, copy);
        CopyPasswordComparison(source, copy);
        copy.Host = source.Host == null ? null : source.Host.ToLowerInvariant();
        copy.Port = source.Port;
        copy.Parameters = CopyParameters(source.Parameters, source.Scheme);
        copy.Headers = CopyHeaders(source.Headers);
        copy.OpaquePart = NormalizeOpaquePart(source);
        SortParameters(copy.Parameters, copy.Scheme);
        SortHeaders(copy.Headers);
        return copy;
    }

    static string? NormalizeSchemeToken(SipUri source)
    {
        if (source.Scheme == SipUriScheme.Sip)
        {
            return "sip";
        }

        if (source.Scheme == SipUriScheme.Sips)
        {
            return "sips";
        }

        if (source.Scheme == SipUriScheme.Tel)
        {
            return "tel";
        }

        return NormalizeAsciiCase(source.SchemeToken);
    }

    static string? NormalizeUser(SipUri source)
    {
        if (source.User == null)
        {
            return null;
        }

        if (source.Scheme == SipUriScheme.Tel)
        {
            return NormalizeTelephoneDigits(source.User);
        }

        return source.User;
    }

    static string? NormalizeOpaquePart(SipUri source)
    {
        if (source.OpaquePart == null)
        {
            return null;
        }

        if (source.Scheme == SipUriScheme.Absolute)
        {
            return NormalizePercentEscapes(source.OpaquePart);
        }

        return source.OpaquePart;
    }

    static void CopyUserComparison(SipUri source, SipUri copy)
    {
        if (source.ComparisonUserSource == source.User && source.ComparisonUserValue != null)
        {
            copy.ComparisonUserSource = copy.User;
            copy.ComparisonUserValue = source.ComparisonUserValue;
        }
    }

    static void CopyPasswordComparison(SipUri source, SipUri copy)
    {
        if (source.ComparisonPasswordSource == source.Password && source.ComparisonPasswordValue != null)
        {
            copy.ComparisonPasswordSource = copy.Password;
            copy.ComparisonPasswordValue = source.ComparisonPasswordValue;
        }
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[]? source, SipUriScheme scheme)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }

        SipUriParameter[] copy = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter parameter = new SipUriParameter();
            SipUriParameter? sourceParameter = source[index];
            if (sourceParameter != null)
            {
                parameter.Name = NormalizeAsciiCase(sourceParameter.Name);
                parameter.Value = NormalizeParameterValue(sourceParameter.Name, sourceParameter.Value, scheme);
                parameter.HasValue = sourceParameter.HasValue;
                CopyParameterComparison(sourceParameter, parameter, scheme);
            }

            copy[index] = parameter;
            index = index + 1;
        }

        return copy;
    }

    static void CopyParameterComparison(SipUriParameter source, SipUriParameter copy, SipUriScheme scheme)
    {
        if (source.ComparisonNameSource == source.Name && source.ComparisonNameValue != null)
        {
            copy.ComparisonNameSource = copy.Name;
            copy.ComparisonNameValue = NormalizeAsciiCase(source.ComparisonNameValue);
        }

        if (source.ComparisonValueSource == source.Value && source.ComparisonValueValue != null)
        {
            copy.ComparisonValueSource = copy.Value;
            copy.ComparisonValueValue = NormalizeParameterValue(source.Name, source.ComparisonValueValue, scheme);
        }
    }

    static string? NormalizeParameterValue(string? name, string? value, SipUriScheme scheme)
    {
        if (value == null)
        {
            return null;
        }

        string? caseNormalized = NormalizeAsciiCase(value);
        if (caseNormalized == null)
        {
            return null;
        }

        string normalized = caseNormalized;
        if (scheme == SipUriScheme.Tel && SipAscii.EqualsIgnoreCase(name, "ext"))
        {
            normalized = NormalizeTelephoneDigits(normalized);
        }

        if (scheme == SipUriScheme.Tel && SipAscii.EqualsIgnoreCase(name, "phone-context")
            && normalized.Length > 0 && normalized[0] == '+')
        {
            normalized = NormalizeTelephoneDigits(normalized);
        }

        return normalized;
    }

    static SipUriHeader[] CopyHeaders(SipUriHeader[]? source)
    {
        if (source == null)
        {
            return new SipUriHeader[0];
        }

        SipUriHeader[] copy = new SipUriHeader[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriHeader header = new SipUriHeader();
            SipUriHeader? sourceHeader = source[index];
            if (sourceHeader != null)
            {
                header.Name = NormalizeAsciiCase(sourceHeader.Name);
                header.Value = NormalizeAsciiCase(sourceHeader.Value);
                CopyHeaderComparison(sourceHeader, header);
            }

            copy[index] = header;
            index = index + 1;
        }

        return copy;
    }

    static void CopyHeaderComparison(SipUriHeader source, SipUriHeader copy)
    {
        if (source.ComparisonNameSource == source.Name && source.ComparisonNameValue != null)
        {
            copy.ComparisonNameSource = copy.Name;
            copy.ComparisonNameValue = NormalizeAsciiCase(source.ComparisonNameValue);
        }

        if (source.ComparisonValueSource == source.Value && source.ComparisonValueValue != null)
        {
            copy.ComparisonValueSource = copy.Value;
            copy.ComparisonValueValue = NormalizeAsciiCase(source.ComparisonValueValue);
        }
    }

    static string NormalizeTelephoneDigits(string value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character != '-' && character != '.' && character != '(' && character != ')')
            {
                if (character >= 'A' && character <= 'Z')
                {
                    character = character + ('a' - 'A');
                }

                builder.Append((char)character);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    static string? NormalizeAsciiCase(string? value)
    {
        if (value == null)
        {
            return null;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character >= 'A' && character <= 'Z')
            {
                character = character + ('a' - 'A');
            }

            builder.Append((char)character);
            index = index + 1;
        }

        return builder.ToString();
    }

    static string NormalizePercentEscapes(string value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%' && index + 2 < value.Length)
            {
                int high = SipUriSyntax.GetHexValue(value[index + 1]);
                int low = SipUriSyntax.GetHexValue(value[index + 2]);
                if (high < 0 || low < 0)
                {
                    builder.Append(value[index]);
                    index = index + 1;
                    continue;
                }

                int decoded = high * 16 + low;
                if (decoded < 128 && SipUriSyntax.IsUnreserved(decoded))
                {
                    builder.Append((char)decoded);
                }
                else
                {
                    builder.Append('%');
                    builder.Append(GetHexCharacter(high));
                    builder.Append(GetHexCharacter(low));
                }

                index = index + 3;
            }
            else
            {
                builder.Append(value[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    static char GetHexCharacter(int value)
    {
        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + value - 10);
    }

    static void SortParameters(SipUriParameter[] parameters, SipUriScheme scheme)
    {
        int index = 1;
        while (index < parameters.Length)
        {
            SipUriParameter current = parameters[index];
            int insertion = index;
            while (insertion > 0 && CompareParameters(current, parameters[insertion - 1], scheme) < 0)
            {
                parameters[insertion] = parameters[insertion - 1];
                insertion = insertion - 1;
            }

            parameters[insertion] = current;
            index = index + 1;
        }
    }

    static int CompareParameters(SipUriParameter left, SipUriParameter right, SipUriScheme scheme)
    {
        if (scheme == SipUriScheme.Tel)
        {
            int leftRank = GetTelephoneParameterRank(left.Name);
            int rightRank = GetTelephoneParameterRank(right.Name);
            if (leftRank != rightRank)
            {
                return leftRank < rightRank ? -1 : 1;
            }
        }

        int nameComparison = SipAscii.CompareIgnoreCase(left.Name, right.Name);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        int valueComparison = SipAscii.CompareIgnoreCase(left.Value, right.Value);
        if (valueComparison != 0)
        {
            return valueComparison;
        }

        if (left.HasValue == right.HasValue)
        {
            return 0;
        }

        return left.HasValue ? 1 : -1;
    }

    static int GetTelephoneParameterRank(string? name)
    {
        if (SipAscii.EqualsIgnoreCase(name, "ext") || SipAscii.EqualsIgnoreCase(name, "isub"))
        {
            return 0;
        }

        if (SipAscii.EqualsIgnoreCase(name, "phone-context"))
        {
            return 1;
        }

        return 2;
    }

    static void SortHeaders(SipUriHeader[] headers)
    {
        int index = 1;
        while (index < headers.Length)
        {
            SipUriHeader current = headers[index];
            int insertion = index;
            while (insertion > 0 && CompareHeaders(current, headers[insertion - 1]) < 0)
            {
                headers[insertion] = headers[insertion - 1];
                insertion = insertion - 1;
            }

            headers[insertion] = current;
            index = index + 1;
        }
    }

    static int CompareHeaders(SipUriHeader left, SipUriHeader right)
    {
        int nameComparison = SipAscii.CompareIgnoreCase(left.Name, right.Name);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        return SipAscii.CompareIgnoreCase(left.Value, right.Value);
    }
}
