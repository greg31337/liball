namespace geo;

/// <summary>Stores the diagnostic classification produced by validation.</summary>
public class ValidationResult
{
    /// <summary>Whether the examined geometry is valid.</summary>
    public bool IsValid = true;

    /// <summary>The validation failure, or <see cref="ValidationError.None"/>.</summary>
    public ValidationError Error;

    /// <summary>The validation message, or an empty string for a valid result.</summary>
    public string Message = "";

    /// <summary>The coordinate associated with the failure, or null when unavailable.</summary>
    public Coordinate? Coordinate;

    /// <summary>The component path associated with the failure, or an empty string.</summary>
    public string ComponentPath = "";
}
