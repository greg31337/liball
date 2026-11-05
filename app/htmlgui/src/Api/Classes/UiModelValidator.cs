namespace htmlgui;

/// <summary>Validates portable application graphs and reports stable location-aware diagnostics.</summary>
public class UiModelValidator : IUiModelValidator
{
    /// <summary>Initializes a model validator.</summary>
    public UiModelValidator()
    {
    }

    /// <summary>Validates a model and returns its diagnostics, throwing when an error is found.</summary>
    public UiDiagnostic[] ValidateModel(UiApplication application)
    {
        UiResult result = TryValidateModel(application);
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            UiDiagnostic diagnostic = result.Diagnostics[index];
            if (diagnostic.Severity == UiDiagnosticSeverity.Error)
            {
                string location = "/";
                if (diagnostic.Location != null && diagnostic.Location.Length > 0)
                {
                    location = diagnostic.Location;
                }

                throw new UiModelException(diagnostic.Message, diagnostic.Code, location);
            }

            index++;
        }

        return result.Diagnostics;
    }

    /// <summary>Validates a model and reports errors without throwing for invalid model content.</summary>
    public UiResult TryValidateModel(UiApplication? application)
    {
        int maximumDiagnosticCount = GetMaximumDiagnosticCount(application);
        ModelWalker walker = new ModelWalker(maximumDiagnosticCount);
        walker.ValidateApplication(application);
        if (application != null)
        {
            AccessibilityValidator accessibilityValidator = new AccessibilityValidator(walker.Diagnostics, walker.Nodes, walker.NodeLocations, walker.IdentityLocations);
            accessibilityValidator.ValidateNodes();
        }

        DiagnosticEntry[] entries = walker.Diagnostics.CreateSnapshot();
        UiDiagnostic[] diagnostics = ConvertDiagnostics(entries);
        return new UiResult { Success = !HasErrors(diagnostics), Diagnostics = diagnostics };
    }

    internal int GetMaximumDiagnosticCount(UiApplication? application)
    {
        if (application != null && application.RenderOptions != null && application.RenderOptions.MaximumDiagnostics > 0)
        {
            return application.RenderOptions.MaximumDiagnostics;
        }

        return 100;
    }

    internal UiDiagnostic[] ConvertDiagnostics(DiagnosticEntry[] entries)
    {
        UiDiagnostic[] result = new UiDiagnostic[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            DiagnosticEntry entry = entries[index];
            result[index] = new UiDiagnostic
            {
                Severity = ConvertSeverity(entry.Level),
                Code = entry.Code,
                Message = entry.Message,
                Location = entry.Location
            };
            index++;
        }

        return result;
    }

    internal UiDiagnosticSeverity ConvertSeverity(DiagnosticLevel level)
    {
        switch (level)
        {
            case DiagnosticLevel.Information:
                return UiDiagnosticSeverity.Information;
            case DiagnosticLevel.Warning:
                return UiDiagnosticSeverity.Warning;
            case DiagnosticLevel.Error:
                return UiDiagnosticSeverity.Error;
            default:
                throw new System.ArgumentOutOfRangeException("level", level, "The diagnostic level is not supported.");
        }
    }

    internal bool HasErrors(UiDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Severity == UiDiagnosticSeverity.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
