namespace htmlgui;

/// <summary>Provides deterministic accessibility expansion and application reporting.</summary>
public class UiAccessibilityEngine : IUiAccessibilityEngine
{
    internal IUiModelValidator Validator;

    /// <summary>Initializes an accessibility engine.</summary>
    public UiAccessibilityEngine()
    {
        Validator = new UiModelValidator();
    }

    /// <summary>Expands a portable node into its deterministic accessibility-aware DOM representation.</summary>
    public DomNode ExpandAccessibility(UiNode node)
    {
        if (node == null)
        {
            throw new System.ArgumentNullException("node");
        }

        SemanticExpander expander = new SemanticExpander();
        return expander.ExpandNode(node);
    }

    /// <summary>Creates an exceptionless location-aware accessibility report for an application graph.</summary>
    public UiAccessibilityReport CreateAccessibilityReport(UiApplication? application)
    {
        UiResult result = Validator.TryValidateModel(application);
        return new UiAccessibilityReport
        {
            Diagnostics = CopyAccessibilityDiagnostics(result.Diagnostics),
            IsValid = result.Success
        };
    }

    internal UiDiagnostic[] CopyAccessibilityDiagnostics(UiDiagnostic[]? diagnostics)
    {
        if (diagnostics == null)
        {
            return System.Array.Empty<UiDiagnostic>();
        }

        UiDiagnostic[] result = new UiDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            UiDiagnostic diagnostic = diagnostics[index];
            result[index] = new UiDiagnostic
            {
                Severity = diagnostic.Severity,
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Location = diagnostic.Location
            };
            index++;
        }

        return result;
    }
}
