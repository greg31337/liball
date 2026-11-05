namespace molecule;


/// <summary>
/// Contains the descriptors calculated for one molecule.
/// </summary>
public class DescriptorSet
{
    /// <summary>
    /// The formula.
    /// </summary>
    public string Formula;
    /// <summary>
    /// The molecular weight.
    /// </summary>
    public double MolecularWeight;
    /// <summary>
    /// The exact mass.
    /// </summary>
    public double ExactMass;
    /// <summary>
    /// The nominal mass.
    /// </summary>
    public int NominalMass;
    /// <summary>
    /// The atom count.
    /// </summary>
    public int AtomCount;
    /// <summary>
    /// The explicit atom count.
    /// </summary>
    public int ExplicitAtomCount;
    /// <summary>
    /// The heavy atom count.
    /// </summary>
    public int HeavyAtomCount;
    /// <summary>
    /// The hydrogen atom count.
    /// </summary>
    public int HydrogenAtomCount;
    /// <summary>
    /// The hetero atom count.
    /// </summary>
    public int HeteroAtomCount;
    /// <summary>
    /// The carbon atom count.
    /// </summary>
    public int CarbonAtomCount;
    /// <summary>
    /// The formal charge.
    /// </summary>
    public int FormalCharge;
    /// <summary>
    /// The rotatable bond count.
    /// </summary>
    public int RotatableBondCount;
    /// <summary>
    /// The hydrogen bond donor count.
    /// </summary>
    public int HydrogenBondDonorCount;
    /// <summary>
    /// The hydrogen bond acceptor count.
    /// </summary>
    public int HydrogenBondAcceptorCount;
    /// <summary>
    /// The ring count.
    /// </summary>
    public int RingCount;
    /// <summary>
    /// The aromatic ring count.
    /// </summary>
    public int AromaticRingCount;
    /// <summary>
    /// The topological polar surface area.
    /// </summary>
    public double TopologicalPolarSurfaceArea;
    /// <summary>
    /// The fraction csp 3.
    /// </summary>
    public double FractionCsp3;
    /// <summary>
    /// The estimated octanol-water partition coefficient, or an explicit unavailable diagnostic.
    /// </summary>
    public DescriptorEstimate LogP;
    /// <summary>
    /// The estimated molar refractivity, or an explicit unavailable diagnostic.
    /// </summary>
    public DescriptorEstimate MolarRefractivity;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorSet"/> class.
    /// </summary>
    public DescriptorSet()
    {
        Formula = "";
        MolecularWeight = 0.0;
        ExactMass = 0.0;
        NominalMass = 0;
        AtomCount = 0;
        ExplicitAtomCount = 0;
        HeavyAtomCount = 0;
        HydrogenAtomCount = 0;
        HeteroAtomCount = 0;
        CarbonAtomCount = 0;
        FormalCharge = 0;
        RotatableBondCount = 0;
        HydrogenBondDonorCount = 0;
        HydrogenBondAcceptorCount = 0;
        RingCount = 0;
        AromaticRingCount = 0;
        TopologicalPolarSurfaceArea = 0.0;
        FractionCsp3 = 0.0;
        LogP = new DescriptorEstimate();
        LogP.DiagnosticCode = "DESCRIPTOR_LOGP_UNAVAILABLE";
        LogP.DiagnosticMessage = "LogP is unavailable because this build does not include a validated atom-fragment coefficient model.";
        MolarRefractivity = new DescriptorEstimate();
        MolarRefractivity.DiagnosticCode = "DESCRIPTOR_MOLAR_REFRACTIVITY_UNAVAILABLE";
        MolarRefractivity.DiagnosticMessage = "Molar refractivity is unavailable because this build does not include a validated atom-fragment coefficient model.";
    }
}
