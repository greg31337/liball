namespace cbor;

/// <summary>Evaluates and generates values for an application CDDL control operator.</summary>
public interface ICddlControlHandler
{
    /// <summary>Evaluates a custom control against an instance value.</summary>
    /// <param name="target">The target schema node.</param>
    /// <param name="controller">The controller schema node.</param>
    /// <param name="value">The instance value.</param>
    /// <param name="options">The validation options and budgets.</param>
    /// <returns>The custom control result.</returns>
    CddlControlResult EvaluateControl(
        CddlNode target,
        CddlNode controller,
        CborValue value,
        CddlValidationOptions options);

    /// <summary>Attempts to generate a value for a custom control.</summary>
    /// <param name="target">The target schema node.</param>
    /// <param name="controller">The controller schema node.</param>
    /// <param name="context">The current generation context.</param>
    /// <returns>The custom control result.</returns>
    CddlControlResult GenerateControl(
        CddlNode target,
        CddlNode controller,
        CddlGenerationContext context);
}
