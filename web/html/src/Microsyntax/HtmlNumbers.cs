using System;
using System.Collections.Generic;

namespace html;

/// <summary>Parses and formats numeric values used by HTML microsyntaxes.</summary>
internal static class HtmlNumbers
{
    /// <summary>Parses the supplied input as the integer.</summary>
    /// <param name="value">The numeric microsyntax text to parse.</param>
    /// <returns>The parse status and parsed numeric value.</returns>
    public static HtmlIntegerResult ParseInteger(string value)
    {
        return ParseIntegerCore(value, false);
    }

    /// <summary>Parses the supplied input as the non negative integer.</summary>
    /// <param name="value">The numeric microsyntax text to parse.</param>
    /// <returns>The parse status and parsed numeric value.</returns>
    public static HtmlIntegerResult ParseNonNegativeInteger(string value)
    {
        return ParseIntegerCore(value, true);
    }

    /// <summary>Parses the supplied input as the dimension.</summary>
    /// <param name="value">The numeric microsyntax text to parse.</param>
    /// <returns>The parse status and parsed numeric value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlDimensionResult ParseDimension(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = SkipWhitespace(value, 0);
        if (index == value.Length || !Ascii.IsDigit(value[index]))
        {
            return new HtmlDimensionResult(false, 0,
                HtmlDimensionUnit.Length, index);
        }

        double number = 0;
        while (index < value.Length && Ascii.IsDigit(value[index]))
        {
            number = number * 10 + value[index] - '0';
            if (double.IsInfinity(number))
            {
                return new HtmlDimensionResult(false, 0,
                    HtmlDimensionUnit.Length, index + 1);
            }

            index = index + 1;
        }

        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            double divisor = 1;
            while (index < value.Length && Ascii.IsDigit(value[index]))
            {
                divisor = divisor * 10;
                number = number + (value[index] - '0') / divisor;
                index = index + 1;
            }
        }

        HtmlDimensionUnit unit = index < value.Length
                && value[index] == '%'
            ? HtmlDimensionUnit.Percentage : HtmlDimensionUnit.Length;
        if (unit == HtmlDimensionUnit.Percentage)
        {
            index = index + 1;
        }

        return new HtmlDimensionResult(true, number, unit, index);
    }

    /// <summary>Parses the supplied input as the column span.</summary>
    /// <param name="value">The numeric microsyntax text to parse.</param>
    /// <returns>The column span.</returns>
    public static int ParseColumnSpan(string value)
    {
        HtmlIntegerResult parsed = ParseNonNegativeInteger(value);
        if (parsed.WasOverflow())
        {
            return IsNegative(value) ? 1 : 1000;
        }

        if (!parsed.IsSuccess() || parsed.GetValue() == 0)
        {
            return 1;
        }

        return parsed.GetValue() > 1000 ? 1000 : parsed.GetValue();
    }

    /// <summary>Parses the supplied input as the row span.</summary>
    /// <param name="value">The numeric microsyntax text to parse.</param>
    /// <returns>The row span.</returns>
    public static int ParseRowSpan(string value)
    {
        HtmlIntegerResult parsed = ParseNonNegativeInteger(value);
        if (parsed.WasOverflow())
        {
            return IsNegative(value) ? 1 : 65534;
        }

        if (!parsed.IsSuccess())
        {
            return 1;
        }

        return parsed.GetValue() > 65534 ? 65534 : parsed.GetValue();
    }

    /// <summary>Parses a legacy table alignment keyword.</summary>
    /// <param name="value">The alignment attribute value.</param>
    /// <returns>The recognized alignment, or <see cref="HtmlTableAlignment.None"/>.</returns>
    public static HtmlTableAlignment ParseTableAlignment(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int start = SkipWhitespace(value, 0);
        int end = value.Length;
        while (end > start && Ascii.IsHtmlWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        string keyword = value.Substring(start, end - start);
        if (Ascii.EqualsIgnoreCase(keyword.AsSpan(), "left".AsSpan()))
        {
            return HtmlTableAlignment.Left;
        }

        if (Ascii.EqualsIgnoreCase(keyword.AsSpan(), "center".AsSpan())
            || Ascii.EqualsIgnoreCase(keyword.AsSpan(), "middle".AsSpan()))
        {
            return HtmlTableAlignment.Center;
        }

        if (Ascii.EqualsIgnoreCase(keyword.AsSpan(), "right".AsSpan()))
        {
            return HtmlTableAlignment.Right;
        }

        if (Ascii.EqualsIgnoreCase(keyword.AsSpan(), "justify".AsSpan()))
        {
            return HtmlTableAlignment.Justify;
        }

        return Ascii.EqualsIgnoreCase(keyword.AsSpan(), "char".AsSpan())
            ? HtmlTableAlignment.Character : HtmlTableAlignment.None;
    }

    /// <summary>Gets the legacy alignment of a table-related HTML element.</summary>
    /// <param name="element">The table-related element to inspect.</param>
    /// <returns>The recognized alignment, or <see cref="HtmlTableAlignment.None"/>.</returns>
    public static HtmlTableAlignment GetTableAlignment(HtmlNode element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element
            || element.GetNamespaceStorage() != HtmlNamespace.Html
            || !IsTableElementName(element.GetLocalNameStorage()))
        {
            throw new ArgumentException(
                "Table alignment requires a table-related HTML element.",
                "element");
        }

        string? value = element.GetAttribute("align");
        return ParseTableAlignment(value == null ? "" : value);
    }

    /// <summary>Gets the ordered list start.</summary>
    /// <param name="orderedList">The ordered-list element whose numbering is being resolved.</param>
    /// <returns>The ordered list start.</returns>
    public static int GetOrderedListStart(HtmlNode orderedList)
    {
        ValidateOrderedList(orderedList);
        string? startValue = orderedList.GetAttribute("start");
        if (startValue != null)
        {
            HtmlIntegerResult parsed = ParseInteger(startValue);
            if (parsed.IsSuccess())
            {
                return parsed.GetValue();
            }
        }

        return orderedList.HasAttribute("reversed")
            ? GetOwnedListItems(orderedList).Length : 1;
    }

    /// <summary>Gets the ordered list ordinals.</summary>
    /// <param name="orderedList">The ordered-list element whose numbering is being resolved.</param>
    /// <returns>A snapshot of the ordered list ordinals; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public static int[] GetOrderedListOrdinals(HtmlNode orderedList)
    {
        ValidateOrderedList(orderedList);
        HtmlNode[] items = GetOwnedListItems(orderedList);
        int[] result = new int[items.Length];
        int number = GetOrderedListStart(orderedList);
        bool reversed = orderedList.HasAttribute("reversed");
        int index = 0;
        while (index < items.Length)
        {
            string? itemValue = items[index].GetAttribute("value");
            if (itemValue != null)
            {
                HtmlIntegerResult parsed = ParseInteger(itemValue);
                if (parsed.IsSuccess())
                {
                    number = parsed.GetValue();
                }
            }

            result[index] = number;
            if (reversed)
            {
                if (number > int.MinValue)
                {
                    number = number - 1;
                }
            }
            else if (number < int.MaxValue)
            {
                number = number + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the ordered list marker style.</summary>
    /// <param name="orderedList">The ordered-list element whose numbering is being resolved.</param>
    /// <returns>The ordered list marker style.</returns>
    public static HtmlOrderedListMarkerStyle GetOrderedListMarkerStyle(
        HtmlNode orderedList)
    {
        ValidateOrderedList(orderedList);
        string? value = orderedList.GetAttribute("type");
        if (value == "a")
        {
            return HtmlOrderedListMarkerStyle.LowerAlpha;
        }

        if (value == "A")
        {
            return HtmlOrderedListMarkerStyle.UpperAlpha;
        }

        if (value == "i")
        {
            return HtmlOrderedListMarkerStyle.LowerRoman;
        }

        if (value == "I")
        {
            return HtmlOrderedListMarkerStyle.UpperRoman;
        }

        return HtmlOrderedListMarkerStyle.Decimal;
    }

    internal static HtmlIntegerResult ParseIntegerCore(string value,
        bool requireNonNegative)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int start = SkipWhitespace(value, 0);
        IntegerScanResult parsed = NumberParser.ScanDecimal(value.AsSpan(),
            start, true, true);
        if (!parsed.IsSuccess())
        {
            return new HtmlIntegerResult(false, parsed.WasOverflow(), 0,
                start + parsed.GetConsumedLength());
        }

        int number = parsed.GetValue();
        if (requireNonNegative && number < 0)
        {
            return new HtmlIntegerResult(false, false, 0,
                start + parsed.GetConsumedLength());
        }

        return new HtmlIntegerResult(true, false, number,
            start + parsed.GetConsumedLength());
    }

    internal static int SkipWhitespace(string value, int index)
    {
        while (index < value.Length && Ascii.IsHtmlWhitespace(value[index]))
        {
            index = index + 1;
        }

        return index;
    }

    internal static bool IsNegative(string value)
    {
        int index = SkipWhitespace(value, 0);
        return index < value.Length && value[index] == '-';
    }

    internal static HtmlNode[] GetOwnedListItems(HtmlNode orderedList)
    {
        HtmlNode[] children = orderedList.GetChildrenStorage();
        List<HtmlNode> result = new List<HtmlNode>();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Element
                && child.GetNamespaceStorage() == HtmlNamespace.Html
                && child.GetLocalNameStorage() == "li")
            {
                result.Add(child);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal static void ValidateOrderedList(HtmlNode orderedList)
    {
        if (orderedList == null)
        {
            throw new ArgumentNullException("orderedList");
        }

        if (orderedList.GetKindStorage() != HtmlNodeKind.Element
            || orderedList.GetNamespaceStorage() != HtmlNamespace.Html
            || orderedList.GetLocalNameStorage() != "ol")
        {
            throw new ArgumentException(
                "Ordered-list numbering requires an HTML ol element.",
                "orderedList");
        }
    }

    internal static bool IsTableElementName(string name)
    {
        return name == "table" || name == "caption" || name == "col"
            || name == "colgroup" || name == "thead" || name == "tbody"
            || name == "tfoot" || name == "tr" || name == "td"
            || name == "th";
    }
}
