namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDocumentFeatureAnalyzer"/>.</summary>
public interface ISvgDocumentFeatureAnalyzer
{
    /// <summary>Analyzes one document with bounded default policy and no host capabilities.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <returns>The dynamic feature analysis.</returns>
    SvgFeatureAnalysisResult AnalyzeWithDocument(SvgDocument document);
    /// <summary>Analyzes one document with explicit bounds, policy, and host capabilities.</summary>
    /// <param name="document">The document to inspect without mutation.</param>
    /// <param name="options">The bounds, resource policy, and host capabilities.</param>
    /// <returns>The dynamic feature analysis.</returns>
    SvgFeatureAnalysisResult AnalyzeWithDocumentAndOptions(SvgDocument? document, SvgFeatureAnalysisOptions options);
}
