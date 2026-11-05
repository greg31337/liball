namespace svg;

/// <summary>Configures collision-safe identifier prefixing and reference discovery.</summary>
public class SvgIdPrefixOptions
{
    /// <summary>The nonempty text prepended to identifiers that do not already have it.</summary>
    public string Prefix;
    /// <summary>Additional unqualified attributes containing whitespace-separated ID references.</summary>
    public System.Collections.Generic.List<string> AdditionalIdReferenceAttributes;
    /// <summary>The maximum CSS characters examined in one value or style element.</summary>
    public long MaximumCssCharacters;
    /// <summary>The maximum CSS tokens examined in one value or style element.</summary>
    public int MaximumCssTokens;

    /// <summary>Creates secure defaults using the <c>svg-</c> prefix.</summary>
    public SvgIdPrefixOptions()
    {
        Prefix = "svg-";
        AdditionalIdReferenceAttributes = new System.Collections.Generic.List<string>();
        MaximumCssCharacters = 4000000;
        MaximumCssTokens = 1000000;
    }

    internal SvgIdPrefixOptions Copy()
    {
        SvgIdPrefixOptions copy = new SvgIdPrefixOptions();
        copy.Prefix = Prefix;
        copy.AdditionalIdReferenceAttributes = AdditionalIdReferenceAttributes == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : new System.Collections.Generic.List<string>(AdditionalIdReferenceAttributes);
        copy.MaximumCssCharacters = MaximumCssCharacters;
        copy.MaximumCssTokens = MaximumCssTokens;
        return copy;
    }
}

/// <summary>Configures deterministic identifier minification and reference discovery.</summary>
public class SvgIdMinifyOptions
{
    /// <summary>Additional unqualified attributes containing whitespace-separated ID references.</summary>
    public System.Collections.Generic.List<string> AdditionalIdReferenceAttributes;
    /// <summary>The maximum CSS characters examined in one value or style element.</summary>
    public long MaximumCssCharacters;
    /// <summary>The maximum CSS tokens examined in one value or style element.</summary>
    public int MaximumCssTokens;

    /// <summary>Creates secure deterministic minification defaults.</summary>
    public SvgIdMinifyOptions()
    {
        AdditionalIdReferenceAttributes = new System.Collections.Generic.List<string>();
        MaximumCssCharacters = 4000000;
        MaximumCssTokens = 1000000;
    }

    internal SvgIdMinifyOptions Copy()
    {
        SvgIdMinifyOptions copy = new SvgIdMinifyOptions();
        copy.AdditionalIdReferenceAttributes = AdditionalIdReferenceAttributes == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : new System.Collections.Generic.List<string>(AdditionalIdReferenceAttributes);
        copy.MaximumCssCharacters = MaximumCssCharacters;
        copy.MaximumCssTokens = MaximumCssTokens;
        return copy;
    }
}
