namespace svg;

/// <summary>Creates the built-in SVG element and legacy compatibility catalogs.</summary>
public interface ISvgCatalogFactory
{
    /// <summary>Creates the complete current-element catalog.</summary>
    /// <returns>A new catalog containing every SVG 2 element kind.</returns>
    SvgElementCatalog CreateElementCatalog();

    /// <summary>Creates the complete SVG 1.1 compatibility catalog.</summary>
    /// <returns>A new catalog with legacy element, attribute, and XLink entries.</returns>
    SvgLegacyCatalog CreateLegacyCatalog();
}
