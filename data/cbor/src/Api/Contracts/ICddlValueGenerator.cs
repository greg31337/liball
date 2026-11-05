namespace cbor;

/// <summary>Generates matching CBOR values from compiled CDDL schemas.</summary>
public interface ICddlValueGenerator
{
    /// <summary>Generates a value for a selected productive schema rule.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="options">The generation strategy and budgets.</param>
    /// <returns>The structured generation result.</returns>
    CddlGenerationResult GenerateValue(
        CddlSchema schema,
        CddlGenerationOptions options);
}
