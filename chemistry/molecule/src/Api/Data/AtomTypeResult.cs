using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by atom type.
/// </summary>
public class AtomTypeResult
{
    /// <summary>
    /// The atom identifier.
    /// </summary>
    public int AtomId;
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The symbol.
    /// </summary>
    public string Symbol;
    /// <summary>
    /// Whether the value is organic subset.
    /// </summary>
    public bool IsOrganicSubset;
    /// <summary>
    /// Whether the value is metal.
    /// </summary>
    public bool IsMetal;
    /// <summary>
    /// Whether the value is halogen.
    /// </summary>
    public bool IsHalogen;
    /// <summary>
    /// Whether the value is hetero atom.
    /// </summary>
    public bool IsHeteroAtom;
    /// <summary>
    /// Whether the value is hydrogen bond donor.
    /// </summary>
    public bool IsHydrogenBondDonor;
    /// <summary>
    /// Whether the value is hydrogen bond acceptor.
    /// </summary>
    public bool IsHydrogenBondAcceptor;
    /// <summary>
    /// Whether the value is hydroxyl oxygen.
    /// </summary>
    public bool IsHydroxylOxygen;
    /// <summary>
    /// Whether the value is carbonyl carbon.
    /// </summary>
    public bool IsCarbonylCarbon;
    /// <summary>
    /// Whether the value is carboxyl carbon.
    /// </summary>
    public bool IsCarboxylCarbon;
    /// <summary>
    /// Whether the value is amine nitrogen.
    /// </summary>
    public bool IsAmineNitrogen;
    /// <summary>
    /// Whether the value is nitro nitrogen.
    /// </summary>
    public bool IsNitroNitrogen;
    /// <summary>
    /// Whether the value is thiol sulfur.
    /// </summary>
    public bool IsThiolSulfur;
    /// <summary>
    /// The functional group.
    /// </summary>
    public string FunctionalGroup;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomTypeResult"/> class.
    /// </summary>
    public AtomTypeResult()
    {
        AtomId = -1;
        AtomicNumber = 0;
        Symbol = "";
        IsOrganicSubset = false;
        IsMetal = false;
        IsHalogen = false;
        IsHeteroAtom = false;
        IsHydrogenBondDonor = false;
        IsHydrogenBondAcceptor = false;
        IsHydroxylOxygen = false;
        IsCarbonylCarbon = false;
        IsCarboxylCarbon = false;
        IsAmineNitrogen = false;
        IsNitroNitrogen = false;
        IsThiolSulfur = false;
        FunctionalGroup = "";
    }
}

