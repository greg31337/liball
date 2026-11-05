namespace xml;

/// <summary>
/// Performs guaranteed-streamability analysis before execution.
/// </summary>
public interface IXsltStreamAnalyzer
{
    /// <summary>
    /// Analyzes a compiled stylesheet or package.
    /// </summary>
    /// <param name="executable">The executable to analyze.</param>
    /// <returns>The guaranteed-streamability report.</returns>
    XsltStreamabilityReport AnalyzeXsltStreamability(XsltExecutable executable);
}
