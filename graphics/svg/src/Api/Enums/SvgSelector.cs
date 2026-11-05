namespace svg;

/// <summary>Defines namespace constraints on selector names.</summary>
public enum SvgSelectorNamespaceKind
{
    /// <summary>The applicable default namespace.</summary>
    Default = 0,
    /// <summary>Only the null namespace.</summary>
    None = 1,
    /// <summary>Any namespace.</summary>
    Any = 2,
    /// <summary>A named namespace prefix resolved by the later matching context.</summary>
    Named = 3
}

/// <summary>Defines simple selector kinds implemented by the base selector grammar.</summary>
public enum SvgSimpleSelectorKind
{
    /// <summary>A type selector.</summary>
    Type = 0,
    /// <summary>A universal selector.</summary>
    Universal = 1,
    /// <summary>An ID selector.</summary>
    Id = 2,
    /// <summary>A class selector.</summary>
    Class = 3,
    /// <summary>An attribute selector.</summary>
    Attribute = 4,
    /// <summary>A structural, linguistic, or selector-list pseudo-class.</summary>
    PseudoClass = 5
}

/// <summary>Defines the pseudo-classes implemented by the structural selector grammar.</summary>
public enum SvgPseudoClassKind
{
    /// <summary>No pseudo-class applies.</summary>
    None = 0,
    /// <summary>The document root pseudo-class.</summary>
    Root = 1,
    /// <summary>The query scoping-root pseudo-class.</summary>
    Scope = 2,
    /// <summary>The empty-element pseudo-class.</summary>
    Empty = 3,
    /// <summary>The first-child pseudo-class.</summary>
    FirstChild = 4,
    /// <summary>The last-child pseudo-class.</summary>
    LastChild = 5,
    /// <summary>The only-child pseudo-class.</summary>
    OnlyChild = 6,
    /// <summary>The An+B child pseudo-class.</summary>
    NthChild = 7,
    /// <summary>The reverse An+B child pseudo-class.</summary>
    NthLastChild = 8,
    /// <summary>The first sibling of its element type.</summary>
    FirstOfType = 9,
    /// <summary>The last sibling of its element type.</summary>
    LastOfType = 10,
    /// <summary>The only sibling of its element type.</summary>
    OnlyOfType = 11,
    /// <summary>The An+B sibling-of-type pseudo-class.</summary>
    NthOfType = 12,
    /// <summary>The reverse An+B sibling-of-type pseudo-class.</summary>
    NthLastOfType = 13,
    /// <summary>The language-range pseudo-class.</summary>
    Lang = 14,
    /// <summary>The negation selector-list pseudo-class.</summary>
    Not = 15,
    /// <summary>The matches-any selector-list pseudo-class.</summary>
    Is = 16,
    /// <summary>The zero-specificity matches-any pseudo-class.</summary>
    Where = 17,
    /// <summary>The relative-selector relational pseudo-class.</summary>
    Has = 18,
    /// <summary>The unvisited-link pseudo-class.</summary>
    Link = 19,
    /// <summary>The visited-link pseudo-class.</summary>
    Visited = 20,
    /// <summary>The host-provided hover-state pseudo-class.</summary>
    Hover = 21,
    /// <summary>The host-provided active-state pseudo-class.</summary>
    Active = 22,
    /// <summary>The host-provided focus-state pseudo-class.</summary>
    Focus = 23,
    /// <summary>The host-provided URL-target pseudo-class.</summary>
    Target = 24,
    /// <summary>A recognized dynamic pseudo-class that is preserved but does not match.</summary>
    UnsupportedDynamic = 25
}

/// <summary>Defines attribute selector comparison operators.</summary>
public enum SvgAttributeSelectorOperator
{
    /// <summary>Only attribute presence is required.</summary>
    Exists = 0,
    /// <summary>The value must be exactly equal.</summary>
    Equals = 1,
    /// <summary>A whitespace-separated word must equal the value.</summary>
    Includes = 2,
    /// <summary>The value or its hyphen-prefixed language subtag must match.</summary>
    DashMatch = 3,
    /// <summary>The attribute value must begin with the selector value.</summary>
    PrefixMatch = 4,
    /// <summary>The attribute value must end with the selector value.</summary>
    SuffixMatch = 5,
    /// <summary>The attribute value must contain the selector value.</summary>
    SubstringMatch = 6
}

/// <summary>Defines explicit attribute selector value case flags.</summary>
public enum SvgAttributeSelectorCase
{
    /// <summary>No explicit case flag was authored.</summary>
    Default = 0,
    /// <summary>ASCII case-insensitive matching was requested.</summary>
    Insensitive = 1,
    /// <summary>Case-sensitive matching was requested.</summary>
    Sensitive = 2
}

/// <summary>Defines combinators between compound selectors.</summary>
public enum SvgSelectorCombinator
{
    /// <summary>A descendant combinator.</summary>
    Descendant = 0,
    /// <summary>A child combinator.</summary>
    Child = 1,
    /// <summary>An adjacent-sibling combinator.</summary>
    AdjacentSibling = 2,
    /// <summary>A general-sibling combinator.</summary>
    GeneralSibling = 3
}

/// <summary>Defines stable selector parse failures.</summary>
public enum SvgSelectorParseError
{
    /// <summary>No selector failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The selector source character limit was exceeded.</summary>
    CharacterLimitExceeded = 2,
    /// <summary>The selector token limit was exceeded.</summary>
    TokenLimitExceeded = 3,
    /// <summary>A selector or selector-list item was required.</summary>
    ExpectedSelector = 4,
    /// <summary>An identifier or universal name was required.</summary>
    ExpectedName = 5,
    /// <summary>An attribute comparison operator was malformed.</summary>
    ExpectedAttributeOperator = 6,
    /// <summary>An attribute selector value was required.</summary>
    ExpectedAttributeValue = 7,
    /// <summary>An attribute selector closing bracket was required.</summary>
    ExpectedCloseBracket = 8,
    /// <summary>A combinator had no selector on its right side.</summary>
    TrailingCombinator = 9,
    /// <summary>An unexpected token followed a complete selector component.</summary>
    UnexpectedToken = 10,
    /// <summary>A supported pseudo-class name or form was required.</summary>
    ExpectedPseudoClass = 11,
    /// <summary>A pseudo-class argument was missing or malformed.</summary>
    ExpectedPseudoArgument = 12,
    /// <summary>An An+B expression was malformed.</summary>
    InvalidNthExpression = 13,
    /// <summary>The configured nested pseudo selector depth was exceeded.</summary>
    PseudoDepthExceeded = 14,
    /// <summary>A relational pseudo-class was nested inside another relational pseudo-class.</summary>
    NestedHasNotAllowed = 15
}
