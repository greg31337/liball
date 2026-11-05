namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidSystemInfo"/>.</summary>
public interface IPdfCidSystemInfo
{
    /// <summary>Gets the character-collection registry.</summary>
    /// <returns><c>Adobe</c>.</returns>
    string GetRegistry();

    /// <summary>Gets the character-collection ordering.</summary>
    /// <returns><c>Identity</c>.</returns>
    string GetOrdering();

    /// <summary>Gets the character-collection supplement.</summary>
    /// <returns>Zero.</returns>
    int GetSupplement();
}
