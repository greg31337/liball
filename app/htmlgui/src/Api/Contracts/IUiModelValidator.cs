namespace htmlgui;

/// <summary>Validates portable UI models without modifying their object graphs.</summary>
public interface IUiModelValidator
{
    /// <summary>Validates a model and returns its diagnostics, throwing when an error is found.</summary>
    /// <param name="application">The application model to validate.</param>
    /// <returns>The ordered validation diagnostics.</returns>
    /// <exception cref="UiModelException">Thrown when validation finds an error.</exception>
    UiDiagnostic[] ValidateModel(UiApplication application);

    /// <summary>Validates a model and reports errors without throwing for invalid model content.</summary>
    /// <param name="application">The optional application model to validate.</param>
    /// <returns>An exceptionless result containing ordered location-aware diagnostics.</returns>
    UiResult TryValidateModel(UiApplication? application);
}
