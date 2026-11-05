namespace geo;

/// <summary>Specifies range and coincident-location policies for linear indexing.</summary>
public class LinearReferenceOptions
{
    /// <summary>Whether an out-of-range length or measure index is clamped; when false it is rejected.</summary>
    public bool ClampOutOfRange;

    /// <summary>Whether the lower traversal location is selected for coincident indexes and component gaps.</summary>
    public bool ResolveLowerLocation = true;
}
