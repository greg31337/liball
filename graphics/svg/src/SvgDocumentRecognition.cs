namespace svg;

/// <summary>Recognizes complete SVG roots and namespace-qualified SVG fragments.</summary>
internal static class SvgDocumentRecognition
{
    /// <summary>Applies SVG recognition and effective loading metadata to a constructed document.</summary>
    /// <param name="result">The successful constructed load result.</param>
    /// <param name="options">The validated load options.</param>
    /// <returns>The same result after recognition.</returns>
    internal static SvgLoadResult Apply(SvgLoadResult result, SvgLoadOptions options)
    {
        if (!result.Success || result.Document == null || result.Document.DocumentElement == null) return result;
        SvgNode root = result.Document.DocumentElement;
        bool isSvgNamespace = root.NamespaceUri == SvgNamespaces.Svg;
        bool isRoot = root.LocalName == SvgElementNames.Svg && (isSvgNamespace || root.NamespaceUri.Length == 0);
        if (!isRoot && !isSvgNamespace)
        {
            SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidSvgRoot, "The XML document element is not an SVG root or SVG fragment.", root.SourceSpan);
            result.Diagnostics.Add(diagnostic);
            result.Document = null;
            result.Success = false;
            return result;
        }
        result.Document.BaseUri = options.BaseUri.Length == 0 ? (result.Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceUri : options.BaseUri;
        result.Document.ContentType = result.DetectedMediaType.Length == 0 ? SvgMimeTypes.Svg : result.DetectedMediaType;
        result.Document.ProcessingMode = options.Mode;
        result.Document.IsFragment = !isRoot;
        result.Document.Index.Refresh();
        System.Collections.Generic.List<SvgDiagnostic> indexDiagnostics = result.Document.Index.GetDuplicateIdDiagnostics();
        int diagnosticIndex = 0;
        while (diagnosticIndex < indexDiagnostics.Count)
        {
            result.Diagnostics.Add(indexDiagnostics[diagnosticIndex]);
            diagnosticIndex = diagnosticIndex + 1;
        }
        return result;
    }

    /// <summary>Refreshes document media metadata after a resource provider supplies authoritative metadata.</summary>
    /// <param name="result">The completed root-resource load result.</param>
    internal static void ApplyDetectedMediaType(SvgLoadResult result)
    {
        if (result.Success && result.Document != null)
        {
            result.Document.ContentType = result.DetectedMediaType.Length == 0 ? SvgMimeTypes.Svg : result.DetectedMediaType;
        }
    }
}
