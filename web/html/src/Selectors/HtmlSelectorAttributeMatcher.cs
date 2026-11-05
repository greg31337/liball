using System;

namespace html;

internal static class HtmlSelectorAttributeMatcher
{
    internal static readonly string[] htmlAsciiCaseInsensitiveValueAttributes = new string[]
    {
        "accept",
        "accept-charset",
        "align",
        "alink",
        "axis",
        "bgcolor",
        "charset",
        "checked",
        "clear",
        "codetype",
        "color",
        "compact",
        "declare",
        "defer",
        "dir",
        "direction",
        "disabled",
        "enctype",
        "face",
        "frame",
        "hreflang",
        "http-equiv",
        "lang",
        "language",
        "link",
        "media",
        "method",
        "multiple",
        "nohref",
        "noresize",
        "noshade",
        "nowrap",
        "readonly",
        "rel",
        "rev",
        "rules",
        "scope",
        "scrolling",
        "selected",
        "shape",
        "target",
        "text",
        "type",
        "valign",
        "valuetype",
        "vlink"
    };

    internal static HtmlSelectorAttributeCase ResolveValueCase(
        HtmlSelectorAttributeCase requestedCase,
        HtmlNamespace elementNamespace, string attributeNamespace,
        string attributeLocalName)
    {
        ValidateCase(requestedCase);
        ValidateNamespace(elementNamespace);
        if (attributeNamespace == null)
        {
            throw new ArgumentNullException("attributeNamespace");
        }

        if (attributeLocalName == null)
        {
            throw new ArgumentNullException("attributeLocalName");
        }

        if (requestedCase != HtmlSelectorAttributeCase.Default)
        {
            return requestedCase;
        }

        if (elementNamespace == HtmlNamespace.Html
            && attributeNamespace.Length == 0
            && IsHtmlValueAsciiCaseInsensitive(attributeLocalName))
        {
            return HtmlSelectorAttributeCase.Insensitive;
        }

        return HtmlSelectorAttributeCase.Sensitive;
    }

    internal static bool Matches(string attributeValue,
        HtmlSelectorAttributeOperator attributeOperator,
        string selectorValue, HtmlSelectorAttributeCase requestedCase,
        HtmlNamespace elementNamespace, string attributeNamespace,
        string attributeLocalName)
    {
        ValidateOperator(attributeOperator);
        if (selectorValue == null)
        {
            throw new ArgumentNullException("selectorValue");
        }

        HtmlSelectorAttributeCase resolvedCase = ResolveValueCase(
            requestedCase, elementNamespace, attributeNamespace,
            attributeLocalName);
        if (attributeOperator == HtmlSelectorAttributeOperator.Exists)
        {
            return attributeValue != null;
        }

        if (attributeValue == null)
        {
            return false;
        }

        bool insensitive = resolvedCase
            == HtmlSelectorAttributeCase.Insensitive;
        switch (attributeOperator)
        {
            case HtmlSelectorAttributeOperator.Equals:
                return EqualsValue(attributeValue, selectorValue,
                    insensitive);
            case HtmlSelectorAttributeOperator.Includes:
                return IncludesValue(attributeValue, selectorValue,
                    insensitive);
            case HtmlSelectorAttributeOperator.DashMatch:
                return DashMatchesValue(attributeValue, selectorValue,
                    insensitive);
            case HtmlSelectorAttributeOperator.PrefixMatch:
                return selectorValue.Length != 0
                    && StartsWithValue(attributeValue, selectorValue,
                        insensitive);
            case HtmlSelectorAttributeOperator.SuffixMatch:
                return selectorValue.Length != 0
                    && EndsWithValue(attributeValue, selectorValue,
                        insensitive);
            case HtmlSelectorAttributeOperator.SubstringMatch:
                return selectorValue.Length != 0
                    && ContainsValue(attributeValue, selectorValue,
                        insensitive);
            default:
                return false;
        }
    }

    internal static bool IsHtmlValueAsciiCaseInsensitive(string attributeName)
    {
        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }

        int index = 0;
        while (index < htmlAsciiCaseInsensitiveValueAttributes.Length)
        {
            if (Ascii.EqualsIgnoreCase(attributeName.AsSpan(),
                htmlAsciiCaseInsensitiveValueAttributes[index].AsSpan()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IncludesValue(string value, string expected,
        bool insensitive)
    {
        if (expected.Length == 0)
        {
            return false;
        }

        int position = 0;
        while (position < value.Length)
        {
            while (position < value.Length
                && Ascii.IsHtmlWhitespace(value[position]))
            {
                position = position + 1;
            }

            int start = position;
            while (position < value.Length
                && !Ascii.IsHtmlWhitespace(value[position]))
            {
                position = position + 1;
            }

            if (position - start == expected.Length
                && EqualsAt(value, start, expected, insensitive))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool DashMatchesValue(string value, string expected,
        bool insensitive)
    {
        if (EqualsValue(value, expected, insensitive))
        {
            return true;
        }

        return value.Length > expected.Length
            && value[expected.Length] == '-'
            && StartsWithValue(value, expected, insensitive);
    }

    internal static bool StartsWithValue(string value, string expected,
        bool insensitive)
    {
        return expected.Length <= value.Length
            && EqualsAt(value, 0, expected, insensitive);
    }

    internal static bool EndsWithValue(string value, string expected,
        bool insensitive)
    {
        return expected.Length <= value.Length
            && EqualsAt(value, value.Length - expected.Length, expected,
                insensitive);
    }

    internal static bool ContainsValue(string value, string expected,
        bool insensitive)
    {
        int index = 0;
        while (index + expected.Length <= value.Length)
        {
            if (EqualsAt(value, index, expected, insensitive))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool EqualsValue(string value, string expected,
        bool insensitive)
    {
        return value.Length == expected.Length
            && EqualsAt(value, 0, expected, insensitive);
    }

    internal static bool EqualsAt(string value, int start, string expected,
        bool insensitive)
    {
        int index = 0;
        while (index < expected.Length)
        {
            char left = value[start + index];
            char right = expected[index];
            if (insensitive)
            {
                left = Ascii.ToLower(left);
                right = Ascii.ToLower(right);
            }

            if (left != right)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateOperator(
        HtmlSelectorAttributeOperator attributeOperator)
    {
        if ((int)attributeOperator
                < (int)HtmlSelectorAttributeOperator.Exists
            || (int)attributeOperator
                > (int)HtmlSelectorAttributeOperator.SubstringMatch)
        {
            throw new ArgumentOutOfRangeException("attributeOperator",
                (int)attributeOperator,
                "Unknown selector attribute operator.");
        }
    }

    internal static void ValidateCase(HtmlSelectorAttributeCase attributeCase)
    {
        if ((int)attributeCase < (int)HtmlSelectorAttributeCase.Default
            || (int)attributeCase
                > (int)HtmlSelectorAttributeCase.Sensitive)
        {
            throw new ArgumentOutOfRangeException("attributeCase",
                (int)attributeCase,
                "Unknown selector attribute case modifier.");
        }
    }

    internal static void ValidateNamespace(HtmlNamespace elementNamespace)
    {
        if ((int)elementNamespace < (int)HtmlNamespace.None
            || (int)elementNamespace > (int)HtmlNamespace.Other)
        {
            throw new ArgumentOutOfRangeException("elementNamespace",
                (int)elementNamespace, "Unknown element namespace.");
        }
    }
}
