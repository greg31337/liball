using System;
namespace token;

/// <summary>
/// Represents the result of mapping a registered claim identifier between JWT and CWT.
/// </summary>
public class ClaimMappingResult
{
    /// <summary>Indicates whether the registries define a cross-format mapping.</summary>
    public bool Mapped;

    /// <summary>The JWT claim name, or <c>null</c> when a CWT label has no mapping.</summary>
    public string? JwtName;

    /// <summary>The CWT claim label, or zero when a JWT name has no mapping.</summary>
    public int CwtLabel;

    internal ClaimMappingResult(bool mapped, string? jwtName, int cwtLabel)
    {
        Mapped = mapped;
        JwtName = jwtName;
        CwtLabel = cwtLabel;
    }
}
