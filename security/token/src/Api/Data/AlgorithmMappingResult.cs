using System;
namespace token;

/// <summary>
/// Represents the result of mapping an algorithm identifier between JOSE and COSE.
/// </summary>
public class AlgorithmMappingResult
{
    /// <summary>Indicates whether an exact standards-defined mapping was found.</summary>
    public bool Mapped;

    /// <summary>The JOSE algorithm name, or <c>null</c> when a COSE label has no mapping.</summary>
    public string? JoseName;

    /// <summary>The COSE algorithm label, or zero when a JOSE name has no mapping.</summary>
    public int CoseLabel;

    internal AlgorithmMappingResult(bool mapped, string? joseName, int coseLabel)
    {
        Mapped = mapped;
        JoseName = joseName;
        CoseLabel = coseLabel;
    }
}
