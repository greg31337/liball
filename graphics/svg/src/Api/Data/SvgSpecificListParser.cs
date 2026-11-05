using System;

namespace svg;

/// <summary>Represents one typed length list item and source span.</summary>
public class SvgLengthListItem
{
    /// <summary>The parsed typed length.</summary>
    public SvgLength Length;
    /// <summary>The item starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive item ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a source-mapped length item.</summary>
    /// <param name="length">The typed length.</param>
    /// <param name="startOffset">The item starting offset.</param>
    /// <param name="endOffset">The exclusive item ending offset.</param>
    public SvgLengthListItem(SvgLength length, int startOffset, int endOffset)
    {
        if (length == null || startOffset < 0 || endOffset <= startOffset) throw new System.ArgumentOutOfRangeException("length");
        Length = length;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Represents a keyword or source-mapped SVG length list.</summary>
public class SvgLengthListValue
{
    /// <summary>The keyword state, or ordinary values.</summary>
    public SvgListKeyword Keyword;
    /// <summary>The ordered typed lengths.</summary>
    public System.Collections.Generic.List<SvgLengthListItem> Items;
    /// <summary>The complete authored source.</summary>
    public string Source;
    /// <summary>Whether the mutable list state is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a parsed length list and copies its items.</summary>
    /// <param name="keyword">The keyword state.</param>
    /// <param name="items">The typed items.</param>
    /// <param name="source">The authored source.</param>
    public SvgLengthListValue(SvgListKeyword keyword, System.Collections.Generic.List<SvgLengthListItem> items, string source)
    {
        if (items == null || source == null || keyword != SvgListKeyword.Values && items.Count != 0 || keyword == SvgListKeyword.Values && items.Count == 0) throw new System.ArgumentOutOfRangeException("items");
        Keyword = keyword;
        Items = new System.Collections.Generic.List<SvgLengthListItem>(items);
        Source = source;
        IsValid = true;
    }

    /// <summary>Formats the keyword or typed list with canonical spacing.</summary>
    /// <returns>Canonical text, or an empty string for invalid state.</returns>
    internal string ToText()
    {
        if (!IsValid || Items == null) return "";
        if (Keyword != SvgListKeyword.Values) return SvgSpecificListRules.KeywordText(Keyword);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < Items.Count)
        {
            if (index > 0) builder.Append(' ');
            builder.Append(Items[index].Length.ToText());
            index = index + 1;
        }
        return builder.ToString();
    }
}

/// <summary>Contains a typed SVG length-list parse result.</summary>
public class SvgLengthListParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgSpecificListParseError Error;
    /// <summary>The parsed value or valid prefix.</summary>
    public SvgLengthListValue? Value;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a length-list parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The parsed value or prefix.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgLengthListParseResult(bool success, SvgSpecificListParseError error, SvgLengthListValue? value, int errorOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one source-mapped SVG text rotation.</summary>
public class SvgRotateItem
{
    /// <summary>The unitless degree angle.</summary>
    public SvgAngle Angle;
    /// <summary>The item starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive item ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a source-mapped rotation item.</summary>
    /// <param name="angle">The unitless degree angle.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The exclusive ending offset.</param>
    public SvgRotateItem(SvgAngle angle, int startOffset, int endOffset)
    {
        Angle = angle;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Contains a bounded SVG text rotate-list parse result.</summary>
public class SvgRotateListParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgSpecificListParseError Error;
    /// <summary>The CSS-wide keyword or ordinary values.</summary>
    public SvgListKeyword Keyword;
    /// <summary>The parsed rotation prefix.</summary>
    public System.Collections.Generic.List<SvgRotateItem> Items;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a rotate-list result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="keyword">The keyword state.</param>
    /// <param name="items">The parsed items.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgRotateListParseResult(bool success, SvgSpecificListParseError error, SvgListKeyword keyword, System.Collections.Generic.List<SvgRotateItem> items, int errorOffset)
    {
        Success = success;
        Error = error;
        Keyword = keyword;
        Items = items;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one parsed CSS font family.</summary>
public class SvgFontFamily
{
    /// <summary>The decoded family name.</summary>
    public string Name;
    /// <summary>Whether the family was quoted.</summary>
    public bool WasQuoted;
    /// <summary>Whether the unquoted name is a generic family keyword.</summary>
    public bool IsGeneric;
    /// <summary>The family starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive family ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a source-mapped font family.</summary>
    /// <param name="name">The decoded family name.</param>
    /// <param name="wasQuoted">Whether it was quoted.</param>
    /// <param name="isGeneric">Whether it is generic.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The exclusive ending offset.</param>
    public SvgFontFamily(string name, bool wasQuoted, bool isGeneric, int startOffset, int endOffset)
    {
        if (name == null || name.Length == 0 && !wasQuoted || startOffset < 0 || endOffset <= startOffset) throw new System.ArgumentOutOfRangeException("name");
        Name = name;
        WasQuoted = wasQuoted;
        IsGeneric = isGeneric;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Contains a bounded font-family list parse result.</summary>
public class SvgFontFamilyParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgSpecificListParseError Error;
    /// <summary>The CSS-wide keyword or ordinary values.</summary>
    public SvgListKeyword Keyword;
    /// <summary>The parsed family prefix.</summary>
    public System.Collections.Generic.List<SvgFontFamily> Families;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a font-family parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="keyword">The keyword state.</param>
    /// <param name="families">The parsed families.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgFontFamilyParseResult(bool success, SvgSpecificListParseError error, SvgListKeyword keyword, System.Collections.Generic.List<SvgFontFamily> families, int errorOffset)
    {
        Success = success;
        Error = error;
        Keyword = keyword;
        Families = families;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a bounded SVG paint-order parse result.</summary>
public class SvgPaintOrderParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgSpecificListParseError Error;
    /// <summary>The normal, CSS-wide, or ordinary-value state.</summary>
    public SvgListKeyword Keyword;
    /// <summary>The authored unique paint components.</summary>
    public System.Collections.Generic.List<SvgPaintOrderComponent> Items;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a paint-order result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="keyword">The keyword state.</param>
    /// <param name="items">The authored components.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgPaintOrderParseResult(bool success, SvgSpecificListParseError error, SvgListKeyword keyword, System.Collections.Generic.List<SvgPaintOrderComponent> items, int errorOffset)
    {
        Success = success;
        Error = error;
        Keyword = keyword;
        Items = items;
        ErrorOffset = errorOffset;
    }
}
