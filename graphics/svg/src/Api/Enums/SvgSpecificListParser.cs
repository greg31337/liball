using System;

namespace svg;

/// <summary>Defines shared keywords accepted by SVG list-valued properties.</summary>
public enum SvgListKeyword
{
    /// <summary>The value contains ordinary list items.</summary>
    Values = 0,
    /// <summary>The <c>none</c> keyword.</summary>
    None = 1,
    /// <summary>The <c>normal</c> keyword.</summary>
    Normal = 2,
    /// <summary>The <c>inherit</c> keyword.</summary>
    Inherit = 3,
    /// <summary>The <c>initial</c> keyword.</summary>
    Initial = 4,
    /// <summary>The <c>unset</c> keyword.</summary>
    Unset = 5,
    /// <summary>The <c>revert</c> keyword.</summary>
    Revert = 6,
    /// <summary>The <c>revert-layer</c> keyword.</summary>
    RevertLayer = 7
}

/// <summary>Defines stable SVG-specific list parsing failures.</summary>
public enum SvgSpecificListParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A nonempty value was required.</summary>
    ExpectedValue = 2,
    /// <summary>A list item was malformed.</summary>
    InvalidItem = 3,
    /// <summary>A required comma or whitespace separator was missing.</summary>
    InvalidSeparator = 4,
    /// <summary>A token exceeded its configured bound.</summary>
    TokenTooLong = 5,
    /// <summary>The configured item count was exceeded.</summary>
    ItemLimitExceeded = 6,
    /// <summary>A dash length was negative.</summary>
    NegativeDash = 7,
    /// <summary>A keyword was unknown or not valid in that position.</summary>
    InvalidKeyword = 8,
    /// <summary>A font family string or identifier sequence was malformed.</summary>
    InvalidFontFamily = 9,
    /// <summary>A paint-order component was repeated.</summary>
    DuplicateItem = 10
}

/// <summary>Defines SVG paint-order components.</summary>
public enum SvgPaintOrderComponent
{
    /// <summary>Fill painting.</summary>
    Fill = 0,
    /// <summary>Stroke painting.</summary>
    Stroke = 1,
    /// <summary>Marker painting.</summary>
    Markers = 2
}
