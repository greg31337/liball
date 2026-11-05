using System;
using System.Collections.Generic;

namespace xml;

internal class XsltPackageVersion
{
    internal string Original;
    internal string[] NumericParts;
    internal string? NamePart;

    internal XsltPackageVersion(string original, string[] numericParts, string? namePart)
    {
        Original = original;
        NumericParts = numericParts;
        NamePart = namePart;
    }
}

internal class XsltPackageVersionRules
{
    internal XsltPackageVersion? ParseXsltPackageVersion(string value)
    {
        string normalized = value.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        int hyphen = normalized.IndexOf('-');
        string numeric = hyphen < 0 ? normalized : normalized.Substring(0, hyphen);
        string? name = hyphen < 0 ? null : normalized.Substring(hyphen + 1);
        if (name != null && !new XdmStringDatatype().IsValidXdmName(name, true))
        {
            return null;
        }

        string[] supplied = numeric.Split('.');
        if (supplied.Length == 0)
        {
            return null;
        }

        List<string> parts = new List<string>();
        int index = 0;
        while (index < supplied.Length)
        {
            string part = supplied[index];
            if (!IsXsltPackageInteger(part))
            {
                return null;
            }

            parts.Add(NormalizeXsltPackageInteger(part));
            index = index + 1;
        }

        return new XsltPackageVersion(normalized, parts.ToArray(), name);
    }

    internal bool IsValidXsltPackageVersionRange(string value)
    {
        string normalized = NormalizeXsltPackageRangeWhitespace(value);
        if (normalized == "*")
        {
            return true;
        }

        string[] alternatives = normalized.Split(',');
        if (alternatives.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < alternatives.Length)
        {
            if (!IsValidXsltPackageVersionRangePart(alternatives[index].Trim()))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsValidXsltPackageVersionRangePart(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (value.StartsWith("to ", StringComparison.Ordinal))
        {
            return ParseXsltPackageRangeBound(value.Substring(3)) != null;
        }

        int separator = value.IndexOf(" to ", StringComparison.Ordinal);
        if (separator >= 0)
        {
            string start = value.Substring(0, separator);
            string end = value.Substring(separator + 4);
            return ParseXsltPackageVersion(start) != null && ParseXsltPackageRangeBound(end) != null;
        }

        if (value.EndsWith("+", StringComparison.Ordinal))
        {
            return ParseXsltPackageVersion(value.Substring(0, value.Length - 1)) != null;
        }

        return ParseXsltPackageRangeBound(value) != null;
    }

    internal XsltPackageVersion? ParseXsltPackageRangeBound(string value)
    {
        string normalized = value.Trim();
        if (normalized.EndsWith(".*", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(0, normalized.Length - 2);
        }

        return ParseXsltPackageVersion(normalized);
    }

    internal bool MatchesXsltPackageVersionRange(string versionValue, string rangeValue)
    {
        XsltPackageVersion? version = ParseXsltPackageVersion(versionValue);
        string range = NormalizeXsltPackageRangeWhitespace(rangeValue);
        if (version == null || !IsValidXsltPackageVersionRange(range))
        {
            return false;
        }

        if (range == "*")
        {
            return true;
        }

        string[] alternatives = range.Split(',');
        int index = 0;
        while (index < alternatives.Length)
        {
            if (MatchesXsltPackageVersionRangePart(version, alternatives[index].Trim()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool MatchesXsltPackageVersionRangePart(XsltPackageVersion version, string range)
    {
        if (range.StartsWith("to ", StringComparison.Ordinal))
        {
            return IsXsltPackageVersionAtOrBelowBound(version, range.Substring(3));
        }

        int separator = range.IndexOf(" to ", StringComparison.Ordinal);
        if (separator >= 0)
        {
            XsltPackageVersion? lower = ParseXsltPackageVersion(range.Substring(0, separator));
            return lower != null && CompareXsltPackageVersions(version, lower) >= 0 && IsXsltPackageVersionAtOrBelowBound(version, range.Substring(separator + 4));
        }

        if (range.EndsWith("+", StringComparison.Ordinal))
        {
            XsltPackageVersion? lower = ParseXsltPackageVersion(range.Substring(0, range.Length - 1));
            return lower != null && CompareXsltPackageVersions(version, lower) >= 0;
        }

        if (range.EndsWith(".*", StringComparison.Ordinal))
        {
            XsltPackageVersion? prefix = ParseXsltPackageVersion(range.Substring(0, range.Length - 2));
            return prefix != null && HasXsltPackageVersionPrefix(version, prefix);
        }

        XsltPackageVersion? exact = ParseXsltPackageVersion(range);
        return exact != null && CompareXsltPackageVersions(version, exact) == 0;
    }

    internal bool IsXsltPackageVersionAtOrBelowBound(XsltPackageVersion version, string boundValue)
    {
        string boundText = boundValue.Trim();
        bool prefix = boundText.EndsWith(".*", StringComparison.Ordinal);
        string exactText = prefix ? boundText.Substring(0, boundText.Length - 2) : boundText;
        XsltPackageVersion? bound = ParseXsltPackageVersion(exactText);
        if (bound == null)
        {
            return false;
        }

        if (!prefix)
        {
            return CompareXsltPackageVersions(version, bound) <= 0;
        }

        if (HasXsltPackageVersionPrefix(version, bound))
        {
            return true;
        }

        return CompareXsltPackageVersions(version, bound) < 0;
    }

    internal bool HasXsltPackageVersionPrefix(XsltPackageVersion version, XsltPackageVersion prefix)
    {
        if (prefix.NamePart != null)
        {
            return CompareXsltPackageVersions(version, prefix) == 0;
        }

        if (version.NumericParts.Length < prefix.NumericParts.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.NumericParts.Length)
        {
            if (CompareXsltPackageIntegers(version.NumericParts[index], prefix.NumericParts[index]) != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int CompareXsltPackageVersions(XsltPackageVersion left, XsltPackageVersion right)
    {
        int leftLength = EffectiveXsltPackageNumericLength(left);
        int rightLength = EffectiveXsltPackageNumericLength(right);
        int common = leftLength < rightLength ? leftLength : rightLength;
        int index = 0;
        while (index < common)
        {
            int comparison = CompareXsltPackageIntegers(left.NumericParts[index], right.NumericParts[index]);
            if (comparison != 0)
            {
                return comparison;
            }

            index = index + 1;
        }

        if (leftLength != rightLength)
        {
            return leftLength < rightLength ? -1 : 1;
        }

        if (left.NamePart == null && right.NamePart == null)
        {
            return 0;
        }

        if (left.NamePart == null)
        {
            return 1;
        }

        if (right.NamePart == null)
        {
            return -1;
        }

        return string.CompareOrdinal(left.NamePart, right.NamePart);
    }

    internal string CreateXsltPackageVersionIdentity(string value)
    {
        XsltPackageVersion? version = ParseXsltPackageVersion(value);
        if (version == null)
        {
            return value;
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int length = EffectiveXsltPackageNumericLength(version);
        int index = 0;
        while (index < length)
        {
            if (index > 0)
            {
                result.Append('.');
            }

            result.Append(version.NumericParts[index]);
            index = index + 1;
        }

        if (version.NamePart != null)
        {
            result.Append('-');
            result.Append(version.NamePart);
        }

        return result.ToString();
    }

    internal int EffectiveXsltPackageNumericLength(XsltPackageVersion version)
    {
        int length = version.NumericParts.Length;
        while (length > 1 && version.NumericParts[length - 1] == "0")
        {
            length = length - 1;
        }

        return length;
    }

    internal int CompareXsltPackageIntegers(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return left.Length < right.Length ? -1 : 1;
        }

        return string.CompareOrdinal(left, right);
    }

    internal bool IsXsltPackageInteger(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string NormalizeXsltPackageInteger(string value)
    {
        int index = 0;
        while (index + 1 < value.Length && value[index] == '0')
        {
            index = index + 1;
        }

        return value.Substring(index);
    }

    internal string NormalizeXsltPackageRangeWhitespace(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        bool pending = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool whitespace = character == ' ' || character == '\t' || character == '\r' || character == '\n';
            if (whitespace)
            {
                if (result.Length > 0)
                {
                    pending = true;
                }
            }
            else
            {
                if (pending)
                {
                    result.Append(' ');
                    pending = false;
                }

                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }
}
