using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains deterministic component-sorted identifiers for a molecule or mixture.
/// </summary>
public class MixtureIdentityResult
{
    /// <summary>
    /// Component identity hashes sorted by ordinal text order, including repeated components.
    /// </summary>
    public List<string> ComponentIdentifiers;
    /// <summary>
    /// A deterministic hash over the ordered component identifiers.
    /// </summary>
    public string Identifier;
    /// <summary>
    /// The number of normalized connected components.
    /// </summary>
    public int ComponentCount;
    /// <summary>
    /// The identity policy applied before the independent participation options.
    /// </summary>
    public IdentityPolicy Policy;
    /// <summary>
    /// Diagnostics produced while building the identity.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether the identity was produced successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes an unsuccessful mixture identity result.
    /// </summary>
    public MixtureIdentityResult()
    {
        ComponentIdentifiers = new List<string>();
        Identifier = "";
        ComponentCount = 0;
        Policy = IdentityPolicy.Exact;
        Report = new MoleculeReport();
        Success = false;
    }
}
