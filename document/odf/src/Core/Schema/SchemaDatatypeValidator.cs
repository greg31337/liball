using System.Text;
using System.Text.RegularExpressions;

namespace odf;

internal static class SchemaDatatypeValidator
{
    internal static bool IsValid(
        string value,
        SchemaPattern pattern,
        SchemaRuleSet rules,
        int maximumLength)
    {
        if (value.Length > maximumLength)
        {
            return false;
        }
        if (!IsLexicallyValid(value, pattern.Name))
        {
            return false;
        }

        int childNumber = 0;
        while (childNumber < pattern.ChildCount)
        {
            int childIndex = rules.PatternChildIndexes[
                pattern.FirstChildIndex + childNumber];
            SchemaPattern child = rules.Patterns[childIndex];
            if (child.Kind == SchemaPatternKind.Param
                && !FacetIsValid(value, child.Name, child.Value))
            {
                return false;
            }
            childNumber = childNumber + 1;
        }
        return true;
    }

    internal static bool IsValueMatch(
        string value,
        SchemaPattern pattern)
    {
        if (pattern.Name == "string")
        {
            return value == pattern.Value;
        }
        return CollapseWhitespace(value) == CollapseWhitespace(pattern.Value);
    }

    internal static string[] SplitList(string value)
    {
        global::System.Collections.Generic.List<string> result =
            new global::System.Collections.Generic.List<string>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            if (index > start)
            {
                result.Add(value.Substring(start, index - start));
            }
        }
        return result.ToArray();
    }

    static bool IsLexicallyValid(string value, string datatype)
    {
        if (datatype.Length == 0 || datatype == "string" || datatype == "token")
        {
            return true;
        }
        if (datatype == "boolean")
        {
            return OdfLexicalParser.ParseBoolean(value).IsValid;
        }
        if (datatype == "integer")
        {
            return OdfLexicalParser.ParseInteger(value).IsValid;
        }
        if (datatype == "nonNegativeInteger")
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
            return parsed.IsValid && parsed.IntegerValue >= 0;
        }
        if (datatype == "positiveInteger")
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
            return parsed.IsValid && parsed.IntegerValue > 0;
        }
        if (datatype == "decimal")
        {
            return OdfLexicalParser.ParseDecimal(value).IsValid;
        }
        if (datatype == "double" || datatype == "float")
        {
            return OdfLexicalParser.ParseFloat(value).IsValid;
        }
        if (datatype == "date")
        {
            return OdfLexicalParser.ParseDate(value).IsValid;
        }
        if (datatype == "time")
        {
            return OdfLexicalParser.ParseTime(value).IsValid;
        }
        if (datatype == "dateTime")
        {
            return OdfLexicalParser.ParseDateTime(value).IsValid;
        }
        if (datatype == "duration")
        {
            return OdfLexicalParser.ParseDuration(value).IsValid;
        }
        if (datatype == "base64Binary")
        {
            return OdfLexicalParser.ParseBase64(value).IsValid;
        }
        if (datatype == "hexBinary")
        {
            return IsHexBinary(value);
        }
        if (datatype == "NCName" || datatype == "ID" || datatype == "IDREF")
        {
            return OdfNameRules.IsNcName(value);
        }
        if (datatype == "IDREFS")
        {
            return AreNames(value);
        }
        if (datatype == "QName")
        {
            return OdfNameRules.IsQName(value);
        }
        if (datatype == "language")
        {
            return OdfLanguageRules.IsLanguageTag(value);
        }
        if (datatype == "anyURI")
        {
            return OdfIriResolver.IsIriReference(value);
        }
        return true;
    }

    static bool FacetIsValid(string value, string name, string expected)
    {
        if (name == "pattern")
        {
            return PatternIsMatch(value, expected);
        }
        if (name == "length")
        {
            OdfLexicalValue length = OdfLexicalParser.ParseInteger(expected);
            return length.IsValid && value.Length == length.IntegerValue;
        }
        if (name == "minLength")
        {
            OdfLexicalValue length = OdfLexicalParser.ParseInteger(expected);
            return length.IsValid && value.Length >= length.IntegerValue;
        }
        if (name == "maxLength")
        {
            OdfLexicalValue length = OdfLexicalParser.ParseInteger(expected);
            return length.IsValid && value.Length <= length.IntegerValue;
        }
        if (name == "minInclusive")
        {
            return NumericFacetIsValid(value, expected, true, true);
        }
        if (name == "maxInclusive")
        {
            return NumericFacetIsValid(value, expected, false, true);
        }
        if (name == "minExclusive")
        {
            return NumericFacetIsValid(value, expected, true, false);
        }
        if (name == "maxExclusive")
        {
            return NumericFacetIsValid(value, expected, false, false);
        }
        return true;
    }

    static bool NumericFacetIsValid(
        string value,
        string expected,
        bool isMinimum,
        bool inclusive)
    {
        OdfLexicalValue actualNumber = OdfLexicalParser.ParseDecimal(value);
        OdfLexicalValue expectedNumber = OdfLexicalParser.ParseDecimal(expected);
        if (!actualNumber.IsValid || !expectedNumber.IsValid)
        {
            return false;
        }
        if (isMinimum)
        {
            if (inclusive)
            {
                return actualNumber.DecimalValue >= expectedNumber.DecimalValue;
            }
            return actualNumber.DecimalValue > expectedNumber.DecimalValue;
        }
        if (inclusive)
        {
            return actualNumber.DecimalValue <= expectedNumber.DecimalValue;
        }
        return actualNumber.DecimalValue < expectedNumber.DecimalValue;
    }

    static bool PatternIsMatch(string value, string pattern)
    {
        string translated = TranslatePattern(pattern);
        return Regex.IsMatch(
            value,
            "\\A(?:" + translated + ")\\z",
            RegexOptions.CultureInvariant);
    }

    static string TranslatePattern(string pattern)
    {
        string prepared = pattern.Replace(
            "[\\i-[:]]",
            "[A-Za-z_]",
            global::System.StringComparison.Ordinal);
        prepared = prepared.Replace(
            "[\\c-[:]]",
            "[A-Za-z0-9_.-]",
            global::System.StringComparison.Ordinal);
        StringBuilder result = new StringBuilder();
        bool inClass = false;
        int index = 0;
        while (index < prepared.Length)
        {
            char current = prepared[index];
            if (current == '[')
            {
                inClass = true;
            }
            else if (current == ']')
            {
                inClass = false;
            }
            if (current == '\\' && index + 1 < prepared.Length
                && (prepared[index + 1] == 'i' || prepared[index + 1] == 'c'))
            {
                bool start = prepared[index + 1] == 'i';
                if (inClass)
                {
                    result.Append(start ? "A-Za-z_" : "A-Za-z0-9_.-");
                }
                else
                {
                    result.Append(start ? "[A-Za-z_]" : "[A-Za-z0-9_.-]");
                }
                index = index + 2;
                continue;
            }
            if (!inClass && (current == '$' || current == '^'))
            {
                result.Append('\\');
            }
            result.Append(current);
            index = index + 1;
        }
        return result.ToString();
    }

    static bool IsHexBinary(string value)
    {
        if (value.Length % 2 != 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = character >= '0' && character <= '9';
            valid = valid || character >= 'a' && character <= 'f';
            valid = valid || character >= 'A' && character <= 'F';
            if (!valid)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool AreNames(string value)
    {
        string[] names = SplitList(value);
        if (names.Length == 0)
        {
            return false;
        }
        foreach (string name in names)
        {
            if (!OdfNameRules.IsNcName(name))
            {
                return false;
            }
        }
        return true;
    }

    static string CollapseWhitespace(string value)
    {
        string[] values = SplitList(value);
        return string.Join(" ", values);
    }
}
