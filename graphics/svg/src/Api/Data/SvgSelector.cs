namespace svg;

/// <summary>Represents a parsed An+B expression with an optional selector filter.</summary>
public class SvgNthExpression
{
    /// <summary>The coefficient multiplied by the one-based integer index.</summary>
    public int A;
    /// <summary>The constant offset.</summary>
    public int B;
    /// <summary>The optional selector list following <c>of</c>, or <c>null</c>.</summary>
    public SvgSelector? OfSelector;
    /// <summary>The exact authored argument text.</summary>
    public string RawText;
    /// <summary>Whether the expression is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a parsed An+B expression.</summary>
    /// <param name="a">The integer coefficient.</param>
    /// <param name="b">The integer offset.</param>
    /// <param name="ofSelector">The optional selector filter.</param>
    /// <param name="rawText">The exact authored argument text.</param>
    public SvgNthExpression(int a, int b, SvgSelector? ofSelector, string rawText)
    {
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        A = a;
        B = b;
        OfSelector = ofSelector;
        RawText = rawText;
        IsValid = true;
    }
}

/// <summary>Represents one type, universal, ID, class, or attribute selector.</summary>
public class SvgSimpleSelector
{
    /// <summary>The simple selector kind.</summary>
    public SvgSimpleSelectorKind Kind;
    /// <summary>The decoded type, ID, class, or attribute local name.</summary>
    public string Name;
    /// <summary>The namespace constraint for type, universal, or attribute selectors.</summary>
    public SvgSelectorNamespaceKind NamespaceKind;
    /// <summary>The decoded named namespace prefix, or an empty string.</summary>
    public string NamespacePrefix;
    /// <summary>The attribute comparison operator.</summary>
    public SvgAttributeSelectorOperator AttributeOperator;
    /// <summary>The decoded attribute comparison value, or an empty string.</summary>
    public string Value;
    /// <summary>The explicit attribute value case flag.</summary>
    public SvgAttributeSelectorCase AttributeCase;
    /// <summary>Whether an attribute comparison value was authored as a string.</summary>
    public bool ValueIsString;
    /// <summary>The pseudo-class kind, or <see cref="SvgPseudoClassKind.None"/>.</summary>
    public SvgPseudoClassKind PseudoClass;
    /// <summary>The decoded authored pseudo-class name, or an empty string.</summary>
    public string PseudoName;
    /// <summary>The parsed An+B argument, or <c>null</c>.</summary>
    public SvgNthExpression? Nth;
    /// <summary>The nested selector-list argument, or <c>null</c>.</summary>
    public SvgSelector? SelectorArgument;
    /// <summary>The ordered decoded <c>:lang()</c> ranges.</summary>
    public System.Collections.Generic.List<string> Languages;
    /// <summary>The pseudo-class ID specificity contribution.</summary>
    public int SpecificityIds;
    /// <summary>The pseudo-class class-like specificity contribution.</summary>
    public int SpecificityClasses;
    /// <summary>The pseudo-class type specificity contribution.</summary>
    public int SpecificityTypes;
    /// <summary>The exact authored simple selector text.</summary>
    public string RawText;
    /// <summary>The source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the simple selector is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a source-preserving simple selector.</summary>
    /// <param name="kind">The simple selector kind.</param>
    /// <param name="name">The decoded selector name.</param>
    /// <param name="namespaceKind">The namespace constraint.</param>
    /// <param name="namespacePrefix">The named namespace prefix, or an empty string.</param>
    /// <param name="attributeOperator">The attribute operator.</param>
    /// <param name="value">The decoded attribute value, or an empty string.</param>
    /// <param name="attributeCase">The attribute case flag.</param>
    /// <param name="valueIsString">Whether the value was quoted.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="source">The source span.</param>
    public SvgSimpleSelector(SvgSimpleSelectorKind kind, string name, SvgSelectorNamespaceKind namespaceKind, string namespacePrefix, SvgAttributeSelectorOperator attributeOperator, string value, SvgAttributeSelectorCase attributeCase, bool valueIsString, string rawText, SvgSourceSpan source)
    {
        if (kind < SvgSimpleSelectorKind.Type || kind > SvgSimpleSelectorKind.PseudoClass) throw new System.ArgumentOutOfRangeException("kind");
        if (name == null || namespacePrefix == null || value == null || rawText == null) throw new System.ArgumentNullException("name");
        if (namespaceKind < SvgSelectorNamespaceKind.Default || namespaceKind > SvgSelectorNamespaceKind.Named) throw new System.ArgumentOutOfRangeException("namespaceKind");
        if (attributeOperator < SvgAttributeSelectorOperator.Exists || attributeOperator > SvgAttributeSelectorOperator.SubstringMatch) throw new System.ArgumentOutOfRangeException("attributeOperator");
        if (attributeCase < SvgAttributeSelectorCase.Default || attributeCase > SvgAttributeSelectorCase.Sensitive) throw new System.ArgumentOutOfRangeException("attributeCase");
        if (source == null) throw new System.ArgumentNullException("source");
        Kind = kind;
        Name = name;
        NamespaceKind = namespaceKind;
        NamespacePrefix = namespacePrefix;
        AttributeOperator = attributeOperator;
        Value = value;
        AttributeCase = attributeCase;
        ValueIsString = valueIsString;
        PseudoClass = SvgPseudoClassKind.None;
        PseudoName = "";
        Nth = null;
        SelectorArgument = null;
        Languages = new System.Collections.Generic.List<string>();
        SpecificityIds = 0;
        SpecificityClasses = 0;
        SpecificityTypes = 0;
        RawText = rawText;
        Source = source;
        IsValid = true;
    }
}

/// <summary>Represents one compound selector with no intervening combinator.</summary>
public class SvgCompoundSelector
{
    /// <summary>The ordered simple selectors.</summary>
    public System.Collections.Generic.List<SvgSimpleSelector> SimpleSelectors;
    /// <summary>The exact authored compound selector text.</summary>
    public string RawText;
    /// <summary>The source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the compound selector is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a compound selector and copies its simple selectors.</summary>
    /// <param name="simpleSelectors">The ordered simple selectors.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="source">The source span.</param>
    public SvgCompoundSelector(System.Collections.Generic.List<SvgSimpleSelector> simpleSelectors, string rawText, SvgSourceSpan source)
    {
        if (simpleSelectors == null) throw new System.ArgumentNullException("simpleSelectors");
        if (simpleSelectors.Count == 0) throw new System.ArgumentOutOfRangeException("simpleSelectors");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (source == null) throw new System.ArgumentNullException("source");
        SimpleSelectors = new System.Collections.Generic.List<SvgSimpleSelector>(simpleSelectors);
        RawText = rawText;
        Source = source;
        IsValid = true;
    }
}

/// <summary>Represents one complex selector and its left-to-right combinators.</summary>
public class SvgComplexSelector
{
    /// <summary>The ordered compound selectors.</summary>
    public System.Collections.Generic.List<SvgCompoundSelector> Compounds;
    /// <summary>The combinator before each compound after the first.</summary>
    public System.Collections.Generic.List<SvgSelectorCombinator> Combinators;
    /// <summary>Whether this is a relative selector anchored to a scope element.</summary>
    public bool IsRelative;
    /// <summary>The explicit or implicit leading combinator for a relative selector.</summary>
    public SvgSelectorCombinator LeadingCombinator;
    /// <summary>The ID specificity component.</summary>
    public int IdSpecificity;
    /// <summary>The class-like specificity component.</summary>
    public int ClassSpecificity;
    /// <summary>The type specificity component.</summary>
    public int TypeSpecificity;
    /// <summary>The exact authored complex selector text.</summary>
    public string RawText;
    /// <summary>The source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the complex selector is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a complex selector and copies its ordered collections.</summary>
    /// <param name="compounds">The ordered compounds.</param>
    /// <param name="combinators">The intervening combinators.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="source">The source span.</param>
    public SvgComplexSelector(System.Collections.Generic.List<SvgCompoundSelector> compounds, System.Collections.Generic.List<SvgSelectorCombinator> combinators, string rawText, SvgSourceSpan source)
    {
        if (compounds == null || combinators == null) throw new System.ArgumentNullException("compounds");
        if (compounds.Count == 0 || combinators.Count + 1 != compounds.Count) throw new System.ArgumentOutOfRangeException("compounds");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (source == null) throw new System.ArgumentNullException("source");
        Compounds = new System.Collections.Generic.List<SvgCompoundSelector>(compounds);
        Combinators = new System.Collections.Generic.List<SvgSelectorCombinator>(combinators);
        IsRelative = false;
        LeadingCombinator = SvgSelectorCombinator.Descendant;
        IdSpecificity = 0;
        ClassSpecificity = 0;
        TypeSpecificity = 0;
        RawText = rawText;
        Source = source;
        IsValid = true;
    }
}

/// <summary>Represents a comma-separated selector list.</summary>
public class SvgSelector
{
    /// <summary>The ordered complex selectors.</summary>
    public System.Collections.Generic.List<SvgComplexSelector> Items;
    /// <summary>The exact authored selector-list text without surrounding whitespace.</summary>
    public string RawText;
    /// <summary>The ID specificity component, populated by the specificity stage.</summary>
    public int IdSpecificity;
    /// <summary>The class and attribute specificity component, populated by the specificity stage.</summary>
    public int ClassSpecificity;
    /// <summary>The type specificity component, populated by the specificity stage.</summary>
    public int TypeSpecificity;
    /// <summary>The source span.</summary>
    public SvgSourceSpan Source;
    /// <summary>Whether the complete selector list is valid.</summary>
    public bool IsValid;
    /// <summary>Ordered selector diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates a selector list and copies its items and diagnostics.</summary>
    /// <param name="items">The ordered complex selectors.</param>
    /// <param name="rawText">The exact authored selector text.</param>
    /// <param name="source">The source span.</param>
    /// <param name="isValid">Whether the complete list is valid.</param>
    /// <param name="diagnostics">The ordered diagnostics.</param>
    public SvgSelector(System.Collections.Generic.List<SvgComplexSelector> items, string rawText, SvgSourceSpan source, bool isValid, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (items == null || diagnostics == null) throw new System.ArgumentNullException("items");
        if (rawText == null) throw new System.ArgumentNullException("rawText");
        if (source == null) throw new System.ArgumentNullException("source");
        Items = new System.Collections.Generic.List<SvgComplexSelector>(items);
        RawText = rawText;
        IdSpecificity = 0;
        ClassSpecificity = 0;
        TypeSpecificity = 0;
        Source = source;
        IsValid = isValid;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
    }
}

/// <summary>Contains a bounded selector parse result.</summary>
public class SvgSelectorParseResult
{
    /// <summary>Whether the complete selector list parsed.</summary>
    public bool Success;
    /// <summary>The stable parse failure.</summary>
    public SvgSelectorParseError Error;
    /// <summary>The parsed selector list, including a valid prefix after failure.</summary>
    public SvgSelector Selector;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The stable failure message.</summary>
    public string Message;

    /// <summary>Creates a selector parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="selector">The parsed selector model.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="message">The failure message.</param>
    public SvgSelectorParseResult(bool success, SvgSelectorParseError error, SvgSelector selector, int errorOffset, string message)
    {
        Success = success;
        Error = error;
        Selector = selector;
        ErrorOffset = errorOffset;
        Message = message;
    }
}
