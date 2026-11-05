namespace iso20022;

/// <summary>Generates deterministic XML Schema 1.0 files from normalized repository and XSD concepts.</summary>
public interface IXsdGenerator
{
    /// <summary>Generates the selected XML Schema files and their ownership manifest.</summary>
    /// <param name="plan">The exact sources, selection, settings, and previous ownership state.</param><returns>The exception-less deterministic generation result.</returns>
    GenerationResult GenerateXsd(GenerationPlan? plan);
}
