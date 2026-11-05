namespace xml;

/// <summary>Provides the public XSLT streamability analyzer shell.</summary>
public class XsltStreamAnalyzer : IXsltStreamAnalyzer
{
    /// <summary>Creates an XSLT streamability analyzer.</summary>
    public XsltStreamAnalyzer()
    {
    }

    /// <summary>Analyzes XSLT streamability.</summary>
    public XsltStreamabilityReport AnalyzeXsltStreamability(XsltExecutable executable)
    {
        Guard.NotNull(executable, "executable");
        if (executable.StreamabilityReport != null)
        {
            return executable.StreamabilityReport;
        }

        XsltStylesheetPlan? compiled = executable.CompiledPlan;
        object? plan = compiled == null ? executable.Plan : compiled;
        if (compiled == null && (!executable.IsImmutable || plan == null || plan.GetType().FullName != "xml.XsltStylesheetPlan"))
        {
            throw new System.ArgumentException("Streamability analysis requires an immutable compiled XSLT executable.", "executable");
        }

        if (plan == null)
        {
            throw new System.ArgumentException("Streamability analysis requires an immutable compiled XSLT executable.", "executable");
        }

        XsltStylesheetPlan stylesheet = (XsltStylesheetPlan)plan;
        XsltStreamabilityAnalysis analysis = stylesheet.StreamabilityAnalysis == null ? new XsltStreamabilityAnalyzer().AnalyzeXsltStreamability(stylesheet) : stylesheet.StreamabilityAnalysis;
        return new XsltStreamabilityReportFactory().CreateXsltStreamabilityReport(analysis, stylesheet.Conformance.Level, stylesheet.Conformance.SchemaAware);
    }
}
