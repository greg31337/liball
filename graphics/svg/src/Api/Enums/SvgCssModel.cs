namespace svg;

/// <summary>Defines stylesheet cascade origins.</summary>
public enum SvgStyleOrigin
{
    /// <summary>Rules supplied by the user agent.</summary>
    UserAgent = 0,
    /// <summary>Rules supplied by the user.</summary>
    User = 1,
    /// <summary>Rules supplied by the document author.</summary>
    Author = 2,
    /// <summary>Values supplied by CSS animations.</summary>
    Animation = 3,
    /// <summary>Values supplied by CSS transitions.</summary>
    Transition = 4
}

/// <summary>Defines generic CSS rule model kinds.</summary>
public enum SvgCssRuleKind
{
    /// <summary>A qualified style rule.</summary>
    Style = 0,
    /// <summary>An at-rule.</summary>
    AtRule = 1,
    /// <summary>Recovered syntax that could not become another rule kind.</summary>
    Unknown = 2
}

/// <summary>Defines recognized at-rule model kinds.</summary>
public enum SvgCssAtRuleKind
{
    /// <summary>An unknown or vendor-defined at-rule.</summary>
    Unknown = 0,
    /// <summary>A character-encoding declaration.</summary>
    Charset = 1,
    /// <summary>An imported stylesheet rule.</summary>
    Import = 2,
    /// <summary>A namespace binding rule.</summary>
    Namespace = 3,
    /// <summary>A cascade layer rule.</summary>
    Layer = 4,
    /// <summary>A media query rule.</summary>
    Media = 5,
    /// <summary>A feature-support query rule.</summary>
    Supports = 6,
    /// <summary>A font-face descriptor rule.</summary>
    FontFace = 7,
    /// <summary>A keyframes rule.</summary>
    Keyframes = 8
}

/// <summary>Defines parsed CSS component-value model kinds.</summary>
public enum SvgCssComponentValueKind
{
    /// <summary>One non-block lexical token.</summary>
    Token = 0,
    /// <summary>A function and its nested values.</summary>
    Function = 1,
    /// <summary>A matched parenthesis, square-bracket, or curly-bracket block.</summary>
    SimpleBlock = 2,
    /// <summary>Recovered component syntax retained without interpretation.</summary>
    Unknown = 3
}

/// <summary>Defines the context of retained unknown CSS syntax.</summary>
public enum SvgCssUnknownSyntaxKind
{
    /// <summary>An unknown rule.</summary>
    Rule = 0,
    /// <summary>An unknown at-rule.</summary>
    AtRule = 1,
    /// <summary>An unknown or malformed declaration.</summary>
    Declaration = 2,
    /// <summary>An unknown or malformed component value.</summary>
    ComponentValue = 3
}
