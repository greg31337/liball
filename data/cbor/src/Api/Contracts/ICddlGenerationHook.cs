namespace cbor;

/// <summary>Observes and optionally supplies values during CDDL generation.</summary>
public interface ICddlGenerationHook
{
    /// <summary>Begins one generation operation.</summary>
    /// <param name="context">The initial generation context.</param>
    void BeginGeneration(CddlGenerationContext context);

    /// <summary>Attempts to supply a value for the current schema node.</summary>
    /// <param name="context">The current generation context.</param>
    /// <returns>A supplied value or a not-applicable result.</returns>
    CddlControlResult GenerateNode(CddlGenerationContext context);

    /// <summary>Ends one generation operation.</summary>
    /// <param name="context">The final generation context.</param>
    /// <param name="result">The final generation result.</param>
    void EndGeneration(CddlGenerationContext context, CddlGenerationResult result);
}
