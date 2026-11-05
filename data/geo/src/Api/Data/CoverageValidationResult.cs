namespace geo;

/// <summary>Stores the result and diagnostic geometry of polygon coverage validation.</summary>
public class CoverageValidationResult
{
    /// <summary>Whether the polygon coverage is valid.</summary>
    public bool IsValid = true;

    /// <summary>The validation failure, or <see cref="ValidationError.None"/>.</summary>
    public ValidationError Error;

    /// <summary>The validation message, or an empty string for a valid result.</summary>
    public string Message = "";

    /// <summary>The invalid coverage edges or regions, or null when valid.</summary>
    public Geometry? InvalidGeometry;

    /// <summary>The coordinate associated with the failure, or null when unavailable.</summary>
    public Coordinate? Coordinate;

    /// <summary>The component path associated with the failure, or an empty string.</summary>
    public string ComponentPath = "";
}
