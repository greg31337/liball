namespace svg;

/// <summary>Retains uninterpreted CSS syntax without losing source tokens.</summary>
public class SvgCssUnknownSyntax
{
    /// <summary>The syntactic context.</summary>
    public SvgCssUnknownSyntaxKind Kind;
    /// <summary>The exact authored text.</summary>
    public string RawText;
    /// <summary>The ordered lexical token snapshot.</summary>
    public System.Collections.Generic.List<SvgCssToken> Tokens;
    /// <summary>A stable recovery or preservation reason.</summary>
    public string Reason;
    /// <summary>The source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the retained syntax is grammatically valid despite being unknown.</summary>
    public bool IsValid;

    /// <summary>Creates retained unknown syntax and copies its token collection.</summary>
    /// <param name="kind">The syntactic context.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="tokens">The ordered lexical tokens.</param>
    /// <param name="reason">The stable preservation reason.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    /// <param name="isValid">Whether the syntax is grammatically valid.</param>
    public SvgCssUnknownSyntax(SvgCssUnknownSyntaxKind kind, string rawText, System.Collections.Generic.List<SvgCssToken> tokens, string reason, SvgSourceSpan? source, bool isValid)
    {
        if (kind < SvgCssUnknownSyntaxKind.Rule || kind > SvgCssUnknownSyntaxKind.ComponentValue) throw new System.ArgumentOutOfRangeException("kind");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (tokens == null) throw new System.ArgumentNullException("tokens");
        if (reason == null) throw new System.ArgumentNullException("reason");
        Kind = kind;
        RawText = rawText;
        Tokens = new System.Collections.Generic.List<SvgCssToken>(tokens);
        Reason = reason;
        Source = source;
        IsValid = isValid;
    }
}

/// <summary>Represents one token, function, block, or recovered CSS component value.</summary>
public class SvgCssComponentValue
{
    /// <summary>The component-value kind.</summary>
    public SvgCssComponentValueKind Kind;
    /// <summary>The opening, function, or ordinary token.</summary>
    public SvgCssToken Token;
    /// <summary>The matched closing token for a function or block, or <c>null</c>.</summary>
    public SvgCssToken? EndToken;
    /// <summary>The decoded function name, or an empty string.</summary>
    public string Name;
    /// <summary>The exact authored component text.</summary>
    public string RawText;
    /// <summary>The ordered nested component values.</summary>
    public System.Collections.Generic.List<SvgCssComponentValue> Values;
    /// <summary>Retained unknown syntax, or <c>null</c>.</summary>
    public SvgCssUnknownSyntax? UnknownSyntax;
    /// <summary>The source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the component is structurally valid.</summary>
    public bool IsValid;

    /// <summary>Creates a component value and copies its nested values.</summary>
    /// <param name="kind">The component kind.</param>
    /// <param name="token">The opening, function, or ordinary token.</param>
    /// <param name="endToken">The matched closing token, or <c>null</c>.</param>
    /// <param name="name">The decoded function name, or an empty string.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="values">The ordered nested values.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    /// <param name="isValid">Whether the component is structurally valid.</param>
    public SvgCssComponentValue(SvgCssComponentValueKind kind, SvgCssToken token, SvgCssToken? endToken, string name, string rawText, System.Collections.Generic.List<SvgCssComponentValue> values, SvgSourceSpan? source, bool isValid)
    {
        if (kind < SvgCssComponentValueKind.Token || kind > SvgCssComponentValueKind.Unknown) throw new System.ArgumentOutOfRangeException("kind");
        if (token == null) throw new System.ArgumentNullException("token");
        if (name == null) throw new System.ArgumentNullException("name");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (values == null) throw new System.ArgumentNullException("values");
        Kind = kind;
        Token = token;
        EndToken = endToken;
        Name = name;
        RawText = rawText;
        Values = new System.Collections.Generic.List<SvgCssComponentValue>(values);
        UnknownSyntax = null;
        Source = source;
        IsValid = isValid;
    }
}

/// <summary>Represents one synthetic longhand produced from an authored shorthand declaration.</summary>
public class SvgCssExpandedDeclaration
{
    /// <summary>The normalized longhand property name.</summary>
    public string Name;
    /// <summary>The expanded CSS value text.</summary>
    public string ValueText;
    /// <summary>Whether the originating declaration was important.</summary>
    public bool Important;
    /// <summary>The source order shared with the originating shorthand.</summary>
    public int SourceOrder;
    /// <summary>The originating shorthand name.</summary>
    public string ShorthandName;
    /// <summary>The originating declaration source span.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the expanded longhand is valid.</summary>
    public bool IsValid;

    /// <summary>Creates one expanded shorthand longhand.</summary>
    /// <param name="name">The nonempty normalized longhand name.</param>
    /// <param name="valueText">The nonempty expanded value text.</param>
    /// <param name="important">Whether the originating shorthand was important.</param>
    /// <param name="sourceOrder">The originating declaration source order.</param>
    /// <param name="shorthandName">The nonempty originating shorthand name.</param>
    /// <param name="source">The originating source span, or <c>null</c>.</param>
    /// <param name="isValid">Whether the expanded value is valid.</param>
    public SvgCssExpandedDeclaration(string name, string valueText, bool important, int sourceOrder, string shorthandName, SvgSourceSpan? source, bool isValid)
    {
        if (name == null || name.Length == 0) throw new System.ArgumentOutOfRangeException("name");
        if (valueText == null || valueText.Length == 0) throw new System.ArgumentOutOfRangeException("valueText");
        if (sourceOrder < 0) throw new System.ArgumentOutOfRangeException("sourceOrder");
        if (shorthandName == null || shorthandName.Length == 0) throw new System.ArgumentOutOfRangeException("shorthandName");
        Name = name;
        ValueText = valueText;
        Important = important;
        SourceOrder = sourceOrder;
        ShorthandName = shorthandName;
        Source = source;
        IsValid = isValid;
    }
}

/// <summary>Represents one source-ordered CSS declaration.</summary>
public class SvgCssDeclaration
{
    /// <summary>The decoded property name.</summary>
    public string Name;
    /// <summary>The exact authored property name.</summary>
    public string RawName;
    /// <summary>The exact complete authored declaration text.</summary>
    public string RawText;
    /// <summary>The exact authored value text before any important suffix.</summary>
    public string ValueText;
    /// <summary>The ordered original lexical token snapshot.</summary>
    public System.Collections.Generic.List<SvgCssToken> Tokens;
    /// <summary>The ordered parsed component values.</summary>
    public System.Collections.Generic.List<SvgCssComponentValue> Values;
    /// <summary>The typed or otherwise interpreted value, or <c>null</c>.</summary>
    public object? ParsedValue;
    /// <summary>Whether the declaration has an authored <c>!important</c> suffix.</summary>
    public bool Important;
    /// <summary>The zero-based declaration source order.</summary>
    public int SourceOrder;
    /// <summary>The source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the declaration is valid.</summary>
    public bool IsValid;
    /// <summary>Ordered declaration diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Retained unknown declaration syntax, or <c>null</c>.</summary>
    public SvgCssUnknownSyntax? UnknownSyntax;
    /// <summary>Whether this authored declaration is a recognized shorthand.</summary>
    public bool IsShorthand;
    /// <summary>The ordered synthetic longhands produced from this declaration.</summary>
    public System.Collections.Generic.List<SvgCssExpandedDeclaration> ExpandedDeclarations;

    /// <summary>Creates a declaration and copies its token and component collections.</summary>
    /// <param name="name">The decoded property name.</param>
    /// <param name="rawName">The exact authored property name.</param>
    /// <param name="rawText">The exact complete declaration text.</param>
    /// <param name="valueText">The exact value text before any important suffix.</param>
    /// <param name="tokens">The ordered original tokens.</param>
    /// <param name="values">The ordered component values.</param>
    /// <param name="important">Whether the declaration is important.</param>
    /// <param name="sourceOrder">The zero-based declaration source order.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    /// <param name="isValid">Whether the declaration is valid.</param>
    public SvgCssDeclaration(string name, string rawName, string rawText, string valueText, System.Collections.Generic.List<SvgCssToken> tokens, System.Collections.Generic.List<SvgCssComponentValue> values, bool important, int sourceOrder, SvgSourceSpan? source, bool isValid)
    {
        if (name == null) throw new System.ArgumentNullException("name");
        if (rawName == null) throw new System.ArgumentNullException("rawName");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (valueText == null) throw new System.ArgumentNullException("valueText");
        if (tokens == null) throw new System.ArgumentNullException("tokens");
        if (values == null) throw new System.ArgumentNullException("values");
        if (sourceOrder < 0) throw new System.ArgumentOutOfRangeException("sourceOrder");
        Name = name;
        RawName = rawName;
        RawText = rawText;
        ValueText = valueText;
        Tokens = new System.Collections.Generic.List<SvgCssToken>(tokens);
        Values = new System.Collections.Generic.List<SvgCssComponentValue>(values);
        ParsedValue = null;
        Important = important;
        SourceOrder = sourceOrder;
        Source = source;
        IsValid = isValid;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        UnknownSyntax = null;
        IsShorthand = false;
        ExpandedDeclarations = new System.Collections.Generic.List<SvgCssExpandedDeclaration>();
    }
}

/// <summary>Represents one cascade layer in first-appearance order.</summary>
public class SvgCssLayer
{
    /// <summary>The full dotted layer name, including generated anonymous identity.</summary>
    public string Name;
    /// <summary>The full parent layer name, or an empty string.</summary>
    public string ParentName;
    /// <summary>The zero-based first-appearance order.</summary>
    public int Order;
    /// <summary>Whether the layer is anonymous and cannot be named by later rules.</summary>
    public bool IsAnonymous;
    /// <summary>The source span that first established the layer.</summary>
    public SvgSourceSpan? Source;

    /// <summary>Creates one cascade layer record.</summary>
    /// <param name="name">The nonempty full layer name.</param>
    /// <param name="parentName">The full parent name, or an empty string.</param>
    /// <param name="order">The zero-based layer order.</param>
    /// <param name="isAnonymous">Whether the layer is anonymous.</param>
    /// <param name="source">The establishing source span, or <c>null</c>.</param>
    public SvgCssLayer(string name, string parentName, int order, bool isAnonymous, SvgSourceSpan? source)
    {
        if (name == null || name.Length == 0) throw new System.ArgumentOutOfRangeException("name");
        if (parentName == null) throw new System.ArgumentNullException("parentName");
        if (order < 0) throw new System.ArgumentOutOfRangeException("order");
        Name = name;
        ParentName = parentName;
        Order = order;
        IsAnonymous = isAnonymous;
        Source = source;
    }
}

/// <summary>Stores the ordered descriptors of one font-face rule.</summary>
public class SvgCssFontFace
{
    /// <summary>The ordered parsed descriptors.</summary>
    public System.Collections.Generic.List<SvgCssDeclaration> Descriptors;
    /// <summary>The complete source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the font-face grammar is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a font-face descriptor record.</summary>
    /// <param name="descriptors">The ordered descriptors.</param>
    /// <param name="source">The complete source span.</param>
    /// <param name="isValid">Whether the grammar is valid.</param>
    public SvgCssFontFace(System.Collections.Generic.List<SvgCssDeclaration> descriptors, SvgSourceSpan source, bool isValid)
    {
        if (descriptors == null) throw new System.ArgumentNullException("descriptors");
        Descriptors = new System.Collections.Generic.List<SvgCssDeclaration>(descriptors);
        Source = source;
        IsValid = isValid;
    }

    /// <summary>Finds the last descriptor with a given ASCII-insensitive name.</summary>
    /// <param name="name">The descriptor name.</param>
    /// <returns>The last matching descriptor, or <c>null</c>.</returns>
    internal SvgCssDeclaration? FindDescriptor(string? name)
    {
        if (name == null) return null;
        int index = Descriptors.Count - 1;
        while (index >= 0)
        {
            SvgCssDeclaration descriptor = Descriptors[index];
            if (descriptor != null && string.Equals(descriptor.Name, name, System.StringComparison.OrdinalIgnoreCase)) return descriptor;
            index = index - 1;
        }
        return null;
    }
}

/// <summary>Represents one keyframe selector and declaration block.</summary>
public class SvgCssKeyframe
{
    /// <summary>The normalized offsets in the inclusive zero-to-one range.</summary>
    public System.Collections.Generic.List<double> Offsets;
    /// <summary>The ordered declarations for this keyframe.</summary>
    public System.Collections.Generic.List<SvgCssDeclaration> Declarations;
    /// <summary>The exact complete authored keyframe text.</summary>
    public string RawText;
    /// <summary>The complete source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the selector and declaration block are valid.</summary>
    public bool IsValid;

    /// <summary>Creates one parsed keyframe.</summary>
    /// <param name="offsets">The normalized offsets.</param>
    /// <param name="declarations">The ordered declarations.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="source">The complete source span.</param>
    /// <param name="isValid">Whether the keyframe is valid.</param>
    public SvgCssKeyframe(System.Collections.Generic.List<double> offsets, System.Collections.Generic.List<SvgCssDeclaration> declarations, string rawText, SvgSourceSpan source, bool isValid)
    {
        if (offsets == null) throw new System.ArgumentNullException("offsets");
        if (declarations == null) throw new System.ArgumentNullException("declarations");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        Offsets = new System.Collections.Generic.List<double>(offsets);
        Declarations = new System.Collections.Generic.List<SvgCssDeclaration>(declarations);
        RawText = rawText;
        Source = source;
        IsValid = isValid;
    }
}

/// <summary>Stores one named CSS keyframes rule.</summary>
public class SvgCssKeyframes
{
    /// <summary>The decoded case-sensitive keyframes name.</summary>
    public string Name;
    /// <summary>The exact authored keyframes name.</summary>
    public string RawName;
    /// <summary>The ordered keyframe blocks.</summary>
    public System.Collections.Generic.List<SvgCssKeyframe> Keyframes;
    /// <summary>The complete source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the name and all keyframe blocks are valid.</summary>
    public bool IsValid;

    /// <summary>Creates one parsed keyframes record.</summary>
    /// <param name="name">The decoded keyframes name.</param>
    /// <param name="rawName">The exact authored name.</param>
    /// <param name="keyframes">The ordered keyframe blocks.</param>
    /// <param name="source">The complete source span.</param>
    /// <param name="isValid">Whether the rule is valid.</param>
    public SvgCssKeyframes(string name, string rawName, System.Collections.Generic.List<SvgCssKeyframe> keyframes, SvgSourceSpan source, bool isValid)
    {
        if (name == null) throw new System.ArgumentNullException("name");
        if (rawName == null) throw new System.ArgumentNullException("rawName");
        if (keyframes == null) throw new System.ArgumentNullException("keyframes");
        Name = name;
        RawName = rawName;
        Keyframes = new System.Collections.Generic.List<SvgCssKeyframe>(keyframes);
        Source = source;
        IsValid = isValid;
    }
}

/// <summary>Represents one generic source-preserving CSS at-rule.</summary>
public class SvgCssAtRule
{
    /// <summary>The recognized at-rule kind.</summary>
    public SvgCssAtRuleKind Kind;
    /// <summary>The decoded at-keyword name without the at sign.</summary>
    public string Name;
    /// <summary>The exact complete authored at-rule text.</summary>
    public string RawText;
    /// <summary>The ordered prelude component values.</summary>
    public System.Collections.Generic.List<SvgCssComponentValue> Prelude;
    /// <summary>The ordered generic block component values.</summary>
    public System.Collections.Generic.List<SvgCssComponentValue> Block;
    /// <summary>Whether the at-rule has a curly block rather than a semicolon ending.</summary>
    public bool HasBlock;
    /// <summary>The zero-based rule source order.</summary>
    public int SourceOrder;
    /// <summary>The source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the at-rule is valid.</summary>
    public bool IsValid;
    /// <summary>Ordered at-rule diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Retained unknown at-rule syntax, or <c>null</c>.</summary>
    public SvgCssUnknownSyntax? UnknownSyntax;
    /// <summary>The decoded namespace prefix for a namespace rule, or an empty string for the default namespace.</summary>
    public string NamespacePrefix;
    /// <summary>The decoded namespace URI for a namespace rule, or an empty string.</summary>
    public string NamespaceUri;
    /// <summary>The decoded resource URI for an import rule, or an empty string.</summary>
    public string ImportUri;
    /// <summary>The exact media condition following an import URI, or an empty string.</summary>
    public string ImportMedia;
    /// <summary>The normalized resolved URI after successful import loading, or an empty string.</summary>
    public string ImportResolvedUri;
    /// <summary>The successfully parsed imported stylesheet, or <c>null</c>.</summary>
    public SvgStyleSheet? ImportedStyleSheet;
    /// <summary>The parsed query list for a media rule, or <c>null</c>.</summary>
    public SvgMediaQueryList? MediaQueries;
    /// <summary>The parsed condition for a supports rule, or <c>null</c>.</summary>
    public SvgSupportsQuery? SupportsQuery;
    /// <summary>The full layer names declared or established by a layer rule.</summary>
    public System.Collections.Generic.List<string> LayerNames;
    /// <summary>The primary full layer name, or an empty string.</summary>
    public string LayerName;
    /// <summary>The primary layer order, or <c>-1</c>.</summary>
    public int LayerOrder;
    /// <summary>The parsed font-face descriptors, or <c>null</c>.</summary>
    public SvgCssFontFace? FontFace;
    /// <summary>The parsed keyframes record, or <c>null</c>.</summary>
    public SvgCssKeyframes? Keyframes;

    /// <summary>Creates an at-rule and copies its prelude and block values.</summary>
    /// <param name="kind">The recognized at-rule kind.</param>
    /// <param name="name">The decoded at-keyword name.</param>
    /// <param name="rawText">The exact complete authored text.</param>
    /// <param name="prelude">The ordered prelude values.</param>
    /// <param name="block">The ordered block values.</param>
    /// <param name="hasBlock">Whether a curly block is present.</param>
    /// <param name="sourceOrder">The zero-based rule source order.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    /// <param name="isValid">Whether the at-rule is valid.</param>
    public SvgCssAtRule(SvgCssAtRuleKind kind, string name, string rawText, System.Collections.Generic.List<SvgCssComponentValue> prelude, System.Collections.Generic.List<SvgCssComponentValue> block, bool hasBlock, int sourceOrder, SvgSourceSpan? source, bool isValid)
    {
        if (kind < SvgCssAtRuleKind.Unknown || kind > SvgCssAtRuleKind.Keyframes) throw new System.ArgumentOutOfRangeException("kind");
        if (name == null) throw new System.ArgumentNullException("name");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (prelude == null) throw new System.ArgumentNullException("prelude");
        if (block == null) throw new System.ArgumentNullException("block");
        if (sourceOrder < 0) throw new System.ArgumentOutOfRangeException("sourceOrder");
        Kind = kind;
        Name = name;
        RawText = rawText;
        Prelude = new System.Collections.Generic.List<SvgCssComponentValue>(prelude);
        Block = new System.Collections.Generic.List<SvgCssComponentValue>(block);
        HasBlock = hasBlock;
        SourceOrder = sourceOrder;
        Source = source;
        IsValid = isValid;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        UnknownSyntax = null;
        NamespacePrefix = "";
        NamespaceUri = "";
        ImportUri = "";
        ImportMedia = "";
        ImportResolvedUri = "";
        ImportedStyleSheet = null;
        MediaQueries = null;
        SupportsQuery = null;
        LayerNames = new System.Collections.Generic.List<string>();
        LayerName = "";
        LayerOrder = -1;
        FontFace = null;
        Keyframes = null;
    }
}

/// <summary>Represents one source-ordered CSS rule without a subclass hierarchy.</summary>
public class SvgCssRule
{
    /// <summary>The tagged rule kind.</summary>
    public SvgCssRuleKind Kind;
    /// <summary>The exact complete authored rule text.</summary>
    public string RawText;
    /// <summary>The ordered rule-prelude component values.</summary>
    public System.Collections.Generic.List<SvgCssComponentValue> Prelude;
    /// <summary>The parsed selector representation, or <c>null</c> before selector parsing.</summary>
    public SvgSelector? Selectors;
    /// <summary>The ordered declarations directly owned by the rule.</summary>
    public System.Collections.Generic.List<SvgCssDeclaration> Declarations;
    /// <summary>The ordered nested rules directly owned by the rule.</summary>
    public System.Collections.Generic.List<SvgCssRule> ChildRules;
    /// <summary>The at-rule payload when <see cref="SvgCssRuleKind.AtRule"/> applies.</summary>
    public SvgCssAtRule? AtRule;
    /// <summary>Retained unknown rule syntax, or <c>null</c>.</summary>
    public SvgCssUnknownSyntax? UnknownSyntax;
    /// <summary>The zero-based rule source order.</summary>
    public int SourceOrder;
    /// <summary>The source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;
    /// <summary>Whether the rule is valid.</summary>
    public bool IsValid;
    /// <summary>Ordered rule diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The enclosing full cascade layer name, or an empty string.</summary>
    public string LayerName;
    /// <summary>The enclosing layer order, or <c>-1</c>.</summary>
    public int LayerOrder;

    /// <summary>Creates a rule and copies its ordered owned collections.</summary>
    /// <param name="kind">The rule kind.</param>
    /// <param name="rawText">The exact complete authored text.</param>
    /// <param name="prelude">The ordered prelude values.</param>
    /// <param name="declarations">The ordered declarations.</param>
    /// <param name="childRules">The ordered nested rules.</param>
    /// <param name="atRule">The at-rule payload, or <c>null</c>.</param>
    /// <param name="sourceOrder">The zero-based rule source order.</param>
    /// <param name="source">The source span, or <c>null</c>.</param>
    /// <param name="isValid">Whether the rule is valid.</param>
    public SvgCssRule(SvgCssRuleKind kind, string rawText, System.Collections.Generic.List<SvgCssComponentValue> prelude, System.Collections.Generic.List<SvgCssDeclaration> declarations, System.Collections.Generic.List<SvgCssRule> childRules, SvgCssAtRule? atRule, int sourceOrder, SvgSourceSpan? source, bool isValid)
    {
        if (kind < SvgCssRuleKind.Style || kind > SvgCssRuleKind.Unknown) throw new System.ArgumentOutOfRangeException("kind");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (prelude == null) throw new System.ArgumentNullException("prelude");
        if (declarations == null) throw new System.ArgumentNullException("declarations");
        if (childRules == null) throw new System.ArgumentNullException("childRules");
        if (sourceOrder < 0) throw new System.ArgumentOutOfRangeException("sourceOrder");
        if (kind == SvgCssRuleKind.AtRule && atRule == null) throw new System.ArgumentNullException("atRule");
        if (kind != SvgCssRuleKind.AtRule && atRule != null) throw new System.ArgumentException("Only an at-rule rule can have an at-rule payload.", "atRule");
        Kind = kind;
        RawText = rawText;
        Prelude = new System.Collections.Generic.List<SvgCssComponentValue>(prelude);
        Selectors = null;
        Declarations = new System.Collections.Generic.List<SvgCssDeclaration>(declarations);
        ChildRules = new System.Collections.Generic.List<SvgCssRule>(childRules);
        AtRule = atRule;
        UnknownSyntax = null;
        SourceOrder = sourceOrder;
        Source = source;
        IsValid = isValid;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        LayerName = "";
        LayerOrder = -1;
    }
}

/// <summary>Represents one source-preserving ordered stylesheet.</summary>
public class SvgStyleSheet
{
    /// <summary>The ordered top-level rules.</summary>
    public System.Collections.Generic.List<SvgCssRule> Rules;
    /// <summary>The cascade origin.</summary>
    public SvgStyleOrigin Origin;
    /// <summary>The enclosing layer name, or an empty string for the unlayered order.</summary>
    public string LayerName;
    /// <summary>The zero-based layer order, or <c>-1</c> for unlayered rules.</summary>
    public int LayerOrder;
    /// <summary>The exact complete stylesheet source text.</summary>
    public string SourceText;
    /// <summary>The complete source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The mutable stylesheet version.</summary>
    public long Version;
    /// <summary>Whether the stylesheet is valid.</summary>
    public bool IsValid;
    /// <summary>Ordered stylesheet diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The namespace bindings local to this stylesheet.</summary>
    public SvgSelectorNamespaceBindings Namespaces;
    /// <summary>The cascade layers in first-appearance order.</summary>
    public System.Collections.Generic.List<SvgCssLayer> Layers;

    /// <summary>Creates a stylesheet and copies its ordered rules and diagnostics.</summary>
    /// <param name="rules">The ordered top-level rules.</param>
    /// <param name="origin">The cascade origin.</param>
    /// <param name="layerName">The enclosing layer name, or an empty string.</param>
    /// <param name="layerOrder">The zero-based layer order, or <c>-1</c>.</param>
    /// <param name="sourceText">The exact complete source text.</param>
    /// <param name="source">The complete source span, or <c>null</c>.</param>
    /// <param name="diagnostics">The ordered stylesheet diagnostics.</param>
    public SvgStyleSheet(System.Collections.Generic.List<SvgCssRule> rules, SvgStyleOrigin origin, string layerName, int layerOrder, string sourceText, SvgSourceSpan? source, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (rules == null) throw new System.ArgumentNullException("rules");
        if (origin < SvgStyleOrigin.UserAgent || origin > SvgStyleOrigin.Transition) throw new System.ArgumentOutOfRangeException("origin");
        if (layerName == null) throw new System.ArgumentNullException("layerName");
        if (layerOrder < -1) throw new System.ArgumentOutOfRangeException("layerOrder");
        if (sourceText == null) throw new System.ArgumentNullException("sourceText");
        if (diagnostics == null) throw new System.ArgumentNullException("diagnostics");
        Rules = new System.Collections.Generic.List<SvgCssRule>(rules);
        Origin = origin;
        LayerName = layerName;
        LayerOrder = layerOrder;
        SourceText = sourceText;
        Source = source;
        Version = 0;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        Namespaces = new SvgSelectorNamespaceBindings();
        Layers = new System.Collections.Generic.List<SvgCssLayer>();
        IsValid = !HasErrorDiagnostics(Diagnostics) && AllRulesValid(Rules);
    }

    /// <summary>Marks the stylesheet changed and returns its new version.</summary>
    /// <returns>The incremented stylesheet version.</returns>
    /// <exception cref="System.OverflowException">Thrown when the version cannot be incremented.</exception>
    internal long MarkChanged()
    {
        Version = Version + 1;
        return Version;
    }

    internal static bool AllRulesValid(System.Collections.Generic.List<SvgCssRule> rules)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index] == null || !rules[index].IsValid) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool HasErrorDiagnostics(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] != null && diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
}
