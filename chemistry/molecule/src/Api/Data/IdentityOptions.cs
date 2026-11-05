using System.Collections.Generic;
using System.IO;

namespace molecule;


/// <summary>
/// Controls identifierentity options behavior.
/// </summary>
public class IdentityOptions
{
    /// <summary>
    /// The policy.
    /// </summary>
    public IdentityPolicy Policy;
    /// <summary>
    /// Whether the duplicate detector retains copies of input molecules.
    /// </summary>
    public bool StoreMolecules;
    /// <summary>
    /// Whether isotope mass numbers participate in identity.
    /// </summary>
    public bool IncludeIsotopes;
    /// <summary>
    /// Whether formal charges participate in identity.
    /// </summary>
    public bool IncludeCharges;
    /// <summary>
    /// Whether hydrogen counts and explicit hydrogen atoms participate in identity.
    /// </summary>
    public bool IncludeHydrogens;
    /// <summary>
    /// Whether atom-map numbers participate in identity.
    /// </summary>
    public bool IncludeAtomMaps;
    /// <summary>
    /// Whether aromatic atom and bond representations participate in identity.
    /// </summary>
    public bool IncludeAromaticity;
    /// <summary>
    /// Whether bond directions, bond stereo, stereo records, and enhanced stereo groups participate in identity.
    /// </summary>
    public bool IncludeStereo;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityOptions"/> class.
    /// </summary>
    public IdentityOptions()
    {
        Policy = IdentityPolicy.Exact;
        StoreMolecules = true;
        IncludeIsotopes = true;
        IncludeCharges = true;
        IncludeHydrogens = true;
        IncludeAtomMaps = true;
        IncludeAromaticity = true;
        IncludeStereo = true;
    }
}
