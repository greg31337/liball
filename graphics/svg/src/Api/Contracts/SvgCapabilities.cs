namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCapabilities"/>.</summary>
public interface ISvgCapabilities
{
    /// <summary>Analyzes features dynamically used by one document with bounded defaults.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <returns>The policy-aware dynamic feature analysis.</returns>
    SvgFeatureAnalysisResult AnalyzeDocumentWithDocument(SvgDocument document);
    /// <summary>Analyzes features dynamically used by one document with explicit options.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <param name="options">The bounds, resource policy, and host capabilities.</param>
    /// <returns>The policy-aware dynamic feature analysis.</returns>
    SvgFeatureAnalysisResult AnalyzeDocumentWithDocumentAndOptions(SvgDocument document, SvgFeatureAnalysisOptions options);
    /// <summary>Returns the published profile identifiers in contract order.</summary>
    /// <returns>A new profile identifier list.</returns>
    System.Collections.Generic.List<string> GetProfileIdentifiers();
    /// <summary>Creates an honest report of the currently implemented library.</summary>
    /// <returns>A new deterministic library feature report.</returns>
    SvgFeatureReport GetLibraryReport();
    /// <summary>Returns all published feature identifiers in ordinal order.</summary>
    /// <returns>A new deterministic feature identifier list.</returns>
    System.Collections.Generic.List<string> GetFeatureIdentifiers();
    /// <summary>Creates a stable granular identifier from a family and inventory name.</summary>
    /// <param name="family">The feature family.</param>
    /// <param name="name">The nonempty inventory name.</param>
    /// <returns>The normalized public capability identifier.</returns>
    string GetFeatureIdentifierWithFamilyAndName(SvgFeatureFamily family, string name);
    /// <summary>Finds current library status for a feature identifier.</summary>
    /// <param name="identifier">The feature identifier.</param>
    /// <returns>The current library entry, or <c>null</c>.</returns>
    SvgFeatureEntry? FindLibraryFeature(string identifier);
}
