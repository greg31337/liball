using System;

namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgLengthListParser"/>.</summary>
public interface ISvgLengthListParser
{
    /// <summary>Parses <c>stroke-dasharray</c>, including <c>none</c> and CSS-wide keywords.</summary>
    /// <param name="value">The dash-array text.</param>
    /// <param name="maxItems">The maximum dash count.</param>
    /// <param name="maxTokenCharacters">The maximum typed-length token length.</param>
    /// <returns>A typed dash list or stable failure.</returns>
    SvgLengthListParseResult ParseDashArray(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses an <c>x</c>, <c>y</c>, <c>dx</c>, or <c>dy</c> text positioning list.</summary>
    /// <param name="value">The positioning list text.</param>
    /// <param name="maxItems">The maximum position count.</param>
    /// <param name="maxTokenCharacters">The maximum typed-length token length.</param>
    /// <returns>A typed positioning list or stable failure.</returns>
    SvgLengthListParseResult ParseTextPosition(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses an SVG text <c>x</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    SvgLengthListParseResult ParseX(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses an SVG text <c>y</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    SvgLengthListParseResult ParseY(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses an SVG text <c>dx</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    SvgLengthListParseResult ParseDx(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses an SVG text <c>dy</c> list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed position list.</returns>
    SvgLengthListParseResult ParseDy(string value, int maxItems, int maxTokenCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgRotateListParser"/>.</summary>
public interface ISvgRotateListParser
{
    /// <summary>Parses unitless degree rotations separated by commas or whitespace.</summary>
    /// <param name="value">The rotate-list text.</param>
    /// <param name="maxItems">The maximum rotation count.</param>
    /// <param name="maxTokenCharacters">The maximum number token length.</param>
    /// <returns>A rotation list or stable failure.</returns>
    SvgRotateListParseResult Parse(string? value, int maxItems, int maxTokenCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgFontFamilyParser"/>.</summary>
public interface ISvgFontFamilyParser
{
    /// <summary>Parses a comma-separated font-family list.</summary>
    /// <param name="value">The font-family text.</param>
    /// <param name="maxFamilies">The maximum family count.</param>
    /// <param name="maxTokenCharacters">The maximum decoded family length.</param>
    /// <returns>A decoded family list or stable failure.</returns>
    SvgFontFamilyParseResult Parse(string? value, int maxFamilies, int maxTokenCharacters);
}

/// <summary>Defines the public operations exposed by <see cref="SvgPaintOrderParser"/>.</summary>
public interface ISvgPaintOrderParser
{
    /// <summary>Parses <c>normal</c>, CSS-wide keywords, or unique fill/stroke/markers components.</summary>
    /// <param name="value">The paint-order text.</param>
    /// <returns>A paint-order value or stable failure.</returns>
    SvgPaintOrderParseResult Parse(string? value);
}
