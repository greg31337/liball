namespace ttf;

/// <summary>Contains the identity character-collection metadata of a CID font.</summary>
public class PdfCidSystemInfo : IPdfCidSystemInfo
{
    internal PdfCidSystemInfo()
    {
    }

    /// <summary>Gets the character-collection registry.</summary>
    /// <returns><c>Adobe</c>.</returns>
    public string GetRegistry()
    {
        return "Adobe";
    }

    /// <summary>Gets the character-collection ordering.</summary>
    /// <returns><c>Identity</c>.</returns>
    public string GetOrdering()
    {
        return "Identity";
    }

    /// <summary>Gets the character-collection supplement.</summary>
    /// <returns>Zero.</returns>
    public int GetSupplement()
    {
        return 0;
    }
}
