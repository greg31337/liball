namespace svg;

internal class SvgFeatureAnalysisState
{
    internal SvgFeatureAnalysisResult Result;
    internal SvgFeatureAnalysisOptions Options;
    internal SvgLegacyCatalog Legacy;
    internal SvgElementCatalog Elements;
    internal SvgPropertyCatalog Properties;

    internal SvgFeatureAnalysisState(SvgFeatureAnalysisResult result, SvgFeatureAnalysisOptions options)
    {
        Result = result;
        Options = options;
        Legacy = new SvgCatalogFactory().CreateLegacyCatalog();
        Elements = new SvgCatalogFactory().CreateElementCatalog();
        Properties = SvgPropertyCatalog.CreateDefault();
    }
}
