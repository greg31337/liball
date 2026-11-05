namespace iso20022;

/// <summary>Generates deterministic restricted-syntax C# models, metadata, codecs, and registries.</summary>
public interface ICSharpModelGenerator
{
    /// <summary>Generates the selected C# files and their ownership manifest.</summary>
    /// <param name="plan">The exact sources, selection, settings, and previous ownership state.</param><returns>The exception-less deterministic generation result.</returns>
    GenerationResult GenerateCSharpModels(GenerationPlan? plan);
}
