namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgStyleSheetImportResolver"/>.</summary>
public interface ISvgStyleSheetImportResolver
{
    /// <summary>Expands matching imports before their containing stylesheets.</summary>
    /// <param name="collection">The collected root stylesheets.</param>
    /// <param name="options">The resource provider, policy, cancellation, and limits.</param>
    /// <param name="mediaMatches">A media-condition evaluator, or <c>null</c> to match every condition.</param>
    /// <returns>A new import-expanded collection with resource diagnostics and totals.</returns>
    SvgStyleSheetCollection Resolve(SvgStyleSheetCollection collection, SvgLoadOptions options, System.Func<string, bool>? mediaMatches);
}
