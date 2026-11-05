namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCssDeclarationParser"/>.</summary>
public interface ISvgCssDeclarationParser
{
    /// <summary>Expands a recognized shorthand while preserving its cascade provenance.</summary>
    /// <param name="declaration">The authored declaration.</param>
    /// <param name="catalog">The active property catalog.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The ordered longhands or a located recoverable failure.</returns>
    SvgCssDeclarationExpansionResult Expand(SvgCssDeclaration? declaration, SvgPropertyCatalog catalog, SvgCssLimits limits);
}
