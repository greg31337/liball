namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifValidationResult"/>.</summary>
public interface IAvifValidationResult
{
    /// <summary>Returns the validation outcome.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the inspected document.</summary>
    /// <returns>The document, including findings produced before failure.</returns>
    AvifDocument? GetDocument();

    /// <summary>Returns the number of validation findings.</summary>
    /// <returns>The finding count.</returns>
    int GetFindingCount();

    /// <summary>Returns one validation finding.</summary>
    /// <param name="index">The zero-based finding index.</param>
    /// <returns>The finding, or null for an invalid index.</returns>
    AvifDiagnostic? GetFinding(int index);
}
