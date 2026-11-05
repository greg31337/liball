using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides fragment choice data and operations for molecular graphs.
/// </summary>
public class FragmentChoice
{
    /// <summary>
    /// The fragment.
    /// </summary>
    public Molecule Fragment;
    /// <summary>
    /// The component details, or <c>null</c> when no component was selected.
    /// </summary>
    public ComponentInfo? Component;
    /// <summary>
    /// The heavy atom count.
    /// </summary>
    public int HeavyAtomCount;
    /// <summary>
    /// The molecular weight.
    /// </summary>
    public double MolecularWeight;
    /// <summary>
    /// Whether the value is organic.
    /// </summary>
    public bool IsOrganic;
    /// <summary>
    /// The canonical key.
    /// </summary>
    public string CanonicalKey;
    /// <summary>
    /// Whether the requested value was found.
    /// </summary>
    public bool Found;

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentChoice"/> class.
    /// </summary>
    public FragmentChoice()
    {
        Fragment = new Molecule();
        Component = new ComponentInfo();
        HeavyAtomCount = 0;
        MolecularWeight = 0.0;
        IsOrganic = false;
        CanonicalKey = "";
        Found = false;
    }
}

