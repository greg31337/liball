namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgStyleSheetCollector"/>.</summary>
public interface ISvgStyleSheetCollector
{
    /// <summary>Collects stylesheets and resolves matching imports through the caller callback.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="callerStyleSheets">The already-parsed caller stylesheets in caller order.</param>
    /// <param name="options">The resource provider, policy, cancellation, and security limits.</param>
    /// <param name="mediaMatches">A media-condition evaluator, or <c>null</c> to match every condition.</param>
    /// <returns>The import-expanded stylesheet collection.</returns>
    SvgStyleSheetCollection CollectWithDocumentAndCallerStyleSheetsAndOptionsAndMediaMatches(SvgDocument document, System.Collections.Generic.List<SvgStyleSheet> callerStyleSheets, SvgLoadOptions options, System.Func<string, bool>? mediaMatches);
    /// <summary>Collects SVG style elements in preorder and then appends caller stylesheets.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="callerStyleSheets">The already-parsed caller stylesheets in caller order.</param>
    /// <param name="limits">The cumulative CSS limits for collection.</param>
    /// <returns>The collected stylesheets and provenance diagnostics.</returns>
    SvgStyleSheetCollection CollectWithDocumentAndCallerStyleSheetsAndLimits(SvgDocument document, System.Collections.Generic.List<SvgStyleSheet> callerStyleSheets, SvgCssLimits limits);
}
