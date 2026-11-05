namespace svg;

/// <summary>Creates independently allocated built-in SVG catalogs.</summary>
public class SvgCatalogFactory : ISvgCatalogFactory
{
    /// <summary>Creates a catalog factory.</summary>
    public SvgCatalogFactory()
    {
    }

    /// <summary>Creates the complete current-element catalog.</summary>
    /// <returns>A new catalog containing every SVG 2 element kind.</returns>
    public SvgElementCatalog CreateElementCatalog()
    {
        return new SvgElementCatalogBuilder().Build();
    }

    /// <summary>Creates the complete SVG 1.1 compatibility catalog.</summary>
    /// <returns>A new catalog with legacy element, attribute, and XLink entries.</returns>
    public SvgLegacyCatalog CreateLegacyCatalog()
    {
        return new SvgLegacyCatalogBuilder().Build();
    }
}
