namespace sip;

internal static class SipUriComparison
{
    internal static bool CompareUriModels(SipUri? left, SipUri? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (SipUriValidator.ValidateUriModel(left).Length != 0 || SipUriValidator.ValidateUriModel(right).Length != 0)
        {
            return false;
        }

        if (System.Object.ReferenceEquals(left, right))
        {
            return true;
        }

        SipUri normalizedLeft = SipUriModelCopy.CreateNormalizedCopy(left);
        SipUri normalizedRight = SipUriModelCopy.CreateNormalizedCopy(right);
        if (normalizedLeft.Scheme != normalizedRight.Scheme)
        {
            return false;
        }

        if (normalizedLeft.Scheme == SipUriScheme.Sip || normalizedLeft.Scheme == SipUriScheme.Sips)
        {
            return CompareSipUris(normalizedLeft, normalizedRight);
        }

        if (normalizedLeft.Scheme == SipUriScheme.Tel)
        {
            return CompareTelephoneUris(normalizedLeft, normalizedRight);
        }

        if (normalizedLeft.Scheme == SipUriScheme.Absolute)
        {
            return SipAscii.EqualsIgnoreCase(normalizedLeft.SchemeToken, normalizedRight.SchemeToken)
                && normalizedLeft.OpaquePart == normalizedRight.OpaquePart;
        }

        return false;
    }

    static bool CompareSipUris(SipUri left, SipUri right)
    {
        if (GetUserComparisonValue(left) != GetUserComparisonValue(right)
            || GetPasswordComparisonValue(left) != GetPasswordComparisonValue(right))
        {
            return false;
        }

        if (!SipAscii.EqualsIgnoreCase(left.Host, right.Host) || left.Port != right.Port)
        {
            return false;
        }

        return CompareSipParameters(left.Parameters, right.Parameters) && CompareHeaders(left.Headers, right.Headers);
    }

    static bool CompareSipParameters(SipUriParameter[] left, SipUriParameter[] right)
    {
        bool[] matched = new bool[right.Length];
        int leftIndex = 0;
        while (leftIndex < left.Length)
        {
            int match = FindMatchingParameter(left[leftIndex], right, matched);
            if (match >= 0)
            {
                matched[match] = true;
            }
            else if (HasParameterName(right, left[leftIndex]) || IsSignificantSipParameter(left[leftIndex].Name))
            {
                return false;
            }

            leftIndex = leftIndex + 1;
        }

        int rightIndex = 0;
        while (rightIndex < right.Length)
        {
            if (!matched[rightIndex]
                && (HasParameterName(left, right[rightIndex]) || IsSignificantSipParameter(right[rightIndex].Name)))
            {
                return false;
            }

            rightIndex = rightIndex + 1;
        }

        return true;
    }

    static bool CompareHeaders(SipUriHeader[] left, SipUriHeader[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        bool[] matched = new bool[right.Length];
        int leftIndex = 0;
        while (leftIndex < left.Length)
        {
            int rightIndex = 0;
            bool found = false;
            while (rightIndex < right.Length)
            {
                if (!matched[rightIndex]
                    && SipAscii.EqualsIgnoreCase(GetHeaderNameComparisonValue(left[leftIndex]),
                        GetHeaderNameComparisonValue(right[rightIndex]))
                    && SipAscii.EqualsIgnoreCase(GetHeaderValueComparisonValue(left[leftIndex]),
                        GetHeaderValueComparisonValue(right[rightIndex])))
                {
                    matched[rightIndex] = true;
                    found = true;
                    break;
                }

                rightIndex = rightIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            leftIndex = leftIndex + 1;
        }

        return true;
    }

    static bool CompareTelephoneUris(SipUri left, SipUri right)
    {
        if (left.User == null || right.User == null)
        {
            return false;
        }

        bool leftGlobal = left.User.Length > 0 && left.User[0] == '+';
        bool rightGlobal = right.User.Length > 0 && right.User[0] == '+';
        if (leftGlobal != rightGlobal || !SipAscii.EqualsIgnoreCase(left.User, right.User))
        {
            return false;
        }

        if (left.Parameters.Length != right.Parameters.Length)
        {
            return false;
        }

        bool[] matched = new bool[right.Parameters.Length];
        int leftIndex = 0;
        while (leftIndex < left.Parameters.Length)
        {
            int match = FindMatchingParameter(left.Parameters[leftIndex], right.Parameters, matched);
            if (match < 0)
            {
                return false;
            }

            matched[match] = true;
            leftIndex = leftIndex + 1;
        }

        return true;
    }

    static int FindMatchingParameter(SipUriParameter target, SipUriParameter[] candidates, bool[] matched)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (!matched[index]
                && SipAscii.EqualsIgnoreCase(GetParameterNameComparisonValue(target),
                    GetParameterNameComparisonValue(candidates[index]))
                && target.HasValue == candidates[index].HasValue
                && SipAscii.EqualsIgnoreCase(GetParameterValueComparisonValue(target),
                    GetParameterValueComparisonValue(candidates[index])))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool HasParameterName(SipUriParameter[] parameters, SipUriParameter target)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (SipAscii.EqualsIgnoreCase(GetParameterNameComparisonValue(parameters[index]),
                GetParameterNameComparisonValue(target)))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsSignificantSipParameter(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, "transport") || SipAscii.EqualsIgnoreCase(name, "user")
            || SipAscii.EqualsIgnoreCase(name, "ttl") || SipAscii.EqualsIgnoreCase(name, "method")
            || SipAscii.EqualsIgnoreCase(name, "maddr");
    }

    static string? GetUserComparisonValue(SipUri uri)
    {
        if (uri.ComparisonUserSource == uri.User && uri.ComparisonUserValue != null)
        {
            return uri.ComparisonUserValue;
        }

        if (uri.User == null)
        {
            return null;
        }

        return SipUriEncoding.EncodeUser(uri.User);
    }

    static string? GetPasswordComparisonValue(SipUri uri)
    {
        if (uri.ComparisonPasswordSource == uri.Password && uri.ComparisonPasswordValue != null)
        {
            return uri.ComparisonPasswordValue;
        }

        if (uri.Password == null)
        {
            return null;
        }

        return SipUriEncoding.EncodePassword(uri.Password);
    }

    static string? GetParameterNameComparisonValue(SipUriParameter parameter)
    {
        if (parameter.ComparisonNameSource == parameter.Name && parameter.ComparisonNameValue != null)
        {
            return parameter.ComparisonNameValue;
        }

        if (parameter.Name == null)
        {
            return null;
        }

        return SipUriEncoding.EncodeParameter(parameter.Name);
    }

    static string? GetParameterValueComparisonValue(SipUriParameter parameter)
    {
        if (parameter.ComparisonValueSource == parameter.Value && parameter.ComparisonValueValue != null)
        {
            return parameter.ComparisonValueValue;
        }

        if (parameter.Value == null)
        {
            return null;
        }

        return SipUriEncoding.EncodeParameter(parameter.Value);
    }

    static string? GetHeaderNameComparisonValue(SipUriHeader header)
    {
        if (header.ComparisonNameSource == header.Name && header.ComparisonNameValue != null)
        {
            return header.ComparisonNameValue;
        }

        if (header.Name == null)
        {
            return null;
        }

        return SipUriEncoding.EncodeHeader(header.Name);
    }

    static string? GetHeaderValueComparisonValue(SipUriHeader header)
    {
        if (header.ComparisonValueSource == header.Value && header.ComparisonValueValue != null)
        {
            return header.ComparisonValueValue;
        }

        if (header.Value == null)
        {
            return null;
        }

        return SipUriEncoding.EncodeHeader(header.Value);
    }
}
