namespace pdf;

/// <summary>Defines operations exposed by <see cref="PdfFormTemplate"/>.</summary>
public interface IPdfFormTemplate
{
    /// <summary>Sets form instructions.</summary>
    /// <param name="instructions">The instructions text.</param>
    void SetInstructions(string instructions);
    /// <summary>Adds an interactive field.</summary>
    /// <param name="field">The field to add.</param>
    void AddField(PdfFormTemplateField field);
    /// <summary>Builds the interactive form.</summary>
    /// <returns>The built document.</returns>
    PdfDocument Build();
}
