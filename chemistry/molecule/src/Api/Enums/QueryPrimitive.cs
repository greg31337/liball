namespace molecule;


/// <summary>
/// Identifies atom and bond primitives stored in a SMARTS query expression.
/// </summary>
public enum QueryPrimitive
{
    /// <summary>
    /// Specifies no primitive.
    /// </summary>
    None = 0,
    /// <summary>
    /// Matches any atom or bond.
    /// </summary>
    Any = 1,
    /// <summary>
    /// Matches an atomic number.
    /// </summary>
    AtomicNumber = 2,
    /// <summary>
    /// Matches an aromatic atom.
    /// </summary>
    Aromatic = 3,
    /// <summary>
    /// Matches an aliphatic atom.
    /// </summary>
    Aliphatic = 4,
    /// <summary>
    /// Matches an isotope mass number.
    /// </summary>
    Isotope = 5,
    /// <summary>
    /// Matches total attached hydrogen count.
    /// </summary>
    HydrogenCount = 6,
    /// <summary>
    /// Matches implicit hydrogen count.
    /// </summary>
    ImplicitHydrogenCount = 7,
    /// <summary>
    /// Matches graph degree.
    /// </summary>
    Degree = 8,
    /// <summary>
    /// Matches total connectivity including hydrogen annotations.
    /// </summary>
    Connectivity = 9,
    /// <summary>
    /// Matches valence.
    /// </summary>
    Valence = 10,
    /// <summary>
    /// Matches whether an atom belongs to a ring.
    /// </summary>
    RingMembership = 11,
    /// <summary>
    /// Matches the number of rings containing an atom.
    /// </summary>
    RingCount = 12,
    /// <summary>
    /// Matches membership in a ring of a selected size.
    /// </summary>
    RingSize = 13,
    /// <summary>
    /// Matches the number of incident ring bonds.
    /// </summary>
    RingConnectivity = 14,
    /// <summary>
    /// Matches formal charge.
    /// </summary>
    FormalCharge = 15,
    /// <summary>
    /// Anchors and evaluates a recursive SMARTS query.
    /// </summary>
    Recursive = 16,
    /// <summary>
    /// Matches bond order.
    /// </summary>
    BondOrder = 17,
    /// <summary>
    /// Matches an aromatic bond.
    /// </summary>
    BondAromatic = 18,
    /// <summary>
    /// Matches whether a bond belongs to a ring.
    /// </summary>
    BondRing = 19,
    /// <summary>
    /// Matches bond direction.
    /// </summary>
    BondDirection = 20
}
