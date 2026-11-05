using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Provides descriptor registry data and operations for molecular graphs.
/// </summary>
public class DescriptorRegistry : IDescriptorRegistry
{
    internal BasicDescriptors Basic;
    internal List<string> Names;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorRegistry"/> class.
    /// </summary>
    public DescriptorRegistry()
    {
        Basic = new BasicDescriptors();
        Names = new List<string>();
        AddName("Formula");
        AddName("MolecularWeight");
        AddName("ExactMass");
        AddName("NominalMass");
        AddName("AtomCount");
        AddName("ExplicitAtomCount");
        AddName("HeavyAtomCount");
        AddName("HydrogenAtomCount");
        AddName("HeteroAtomCount");
        AddName("CarbonAtomCount");
        AddName("FormalCharge");
        AddName("RotatableBondCount");
        AddName("HydrogenBondDonorCount");
        AddName("HydrogenBondAcceptorCount");
        AddName("RingCount");
        AddName("AromaticRingCount");
        AddName("TopologicalPolarSurfaceArea");
        AddName("FractionCsp3");
        AddName("LogP");
        AddName("MolarRefractivity");
    }

    /// <summary>
    /// Calculates every descriptor registered by the library.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting descriptor set.</returns>
    public DescriptorSet CalculateAll(Molecule? molecule)
    {
        return Basic.Calculate(molecule);
    }

    /// <summary>
    /// Calculates the values represented by this calculator for the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="name">The name.</param>
    /// <returns>The resulting descriptor value.</returns>
    public DescriptorValue Calculate(Molecule? molecule, string? name)
    {
        DescriptorSet set = Basic.Calculate(molecule);
        return GetValue(set, name);
    }

    /// <summary>
    /// Calculates the number.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="name">The name.</param>
    /// <returns>The calculated numeric value.</returns>
    public double CalculateNumber(Molecule? molecule, string? name)
    {
        DescriptorValue value = Calculate(molecule, name);
        if (value.Found && value.IsNumber)
        {
            return value.Number;
        }

        return 0.0;
    }

    /// <summary>
    /// Calculates the text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="name">The name.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string CalculateText(Molecule? molecule, string? name)
    {
        DescriptorValue value = Calculate(molecule, name);
        if (value.Found && value.IsText)
        {
            return value.Text;
        }

        if (value.Found && value.IsNumber)
        {
            return NumberFormatting.FormatDouble(value.Number);
        }

        return "";
    }

    /// <summary>
    /// Determines whether the requested registered name or structural query is present.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool Contains(string? name)
    {
        string canonical = GetCanonicalName(name);
        return canonical != "";
    }

    /// <summary>
    /// Returns the names.
    /// </summary>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<string> GetNames()
    {
        List<string> copy = new List<string>();
        int index = 0;
        while (index < Names.Count)
        {
            copy.Add(Names[index]);
            index = index + 1;
        }

        return copy;
    }

    internal DescriptorValue GetValue(DescriptorSet set, string? name)
    {
        string canonical = GetCanonicalName(name);
        if (canonical == "Formula")
        {
            return CreateTextValue(canonical, set.Formula);
        }

        if (canonical == "MolecularWeight")
        {
            return CreateNumberValue(canonical, set.MolecularWeight);
        }

        if (canonical == "ExactMass")
        {
            return CreateNumberValue(canonical, set.ExactMass);
        }

        if (canonical == "NominalMass")
        {
            return CreateNumberValue(canonical, set.NominalMass);
        }

        if (canonical == "AtomCount")
        {
            return CreateNumberValue(canonical, set.AtomCount);
        }

        if (canonical == "ExplicitAtomCount")
        {
            return CreateNumberValue(canonical, set.ExplicitAtomCount);
        }

        if (canonical == "HeavyAtomCount")
        {
            return CreateNumberValue(canonical, set.HeavyAtomCount);
        }

        if (canonical == "HydrogenAtomCount")
        {
            return CreateNumberValue(canonical, set.HydrogenAtomCount);
        }

        if (canonical == "HeteroAtomCount")
        {
            return CreateNumberValue(canonical, set.HeteroAtomCount);
        }

        if (canonical == "CarbonAtomCount")
        {
            return CreateNumberValue(canonical, set.CarbonAtomCount);
        }

        if (canonical == "FormalCharge")
        {
            return CreateNumberValue(canonical, set.FormalCharge);
        }

        if (canonical == "RotatableBondCount")
        {
            return CreateNumberValue(canonical, set.RotatableBondCount);
        }

        if (canonical == "HydrogenBondDonorCount")
        {
            return CreateNumberValue(canonical, set.HydrogenBondDonorCount);
        }

        if (canonical == "HydrogenBondAcceptorCount")
        {
            return CreateNumberValue(canonical, set.HydrogenBondAcceptorCount);
        }

        if (canonical == "RingCount")
        {
            return CreateNumberValue(canonical, set.RingCount);
        }

        if (canonical == "AromaticRingCount")
        {
            return CreateNumberValue(canonical, set.AromaticRingCount);
        }

        if (canonical == "TopologicalPolarSurfaceArea")
        {
            return CreateNumberValue(canonical, set.TopologicalPolarSurfaceArea);
        }

        if (canonical == "FractionCsp3")
        {
            return CreateNumberValue(canonical, set.FractionCsp3);
        }

        if (canonical == "LogP")
        {
            return CreateEstimateValue(canonical, set.LogP);
        }

        if (canonical == "MolarRefractivity")
        {
            return CreateEstimateValue(canonical, set.MolarRefractivity);
        }

        return new DescriptorValue();
    }

    internal string GetCanonicalName(string? name)
    {
        string normalized = NormalizeName(name);
        if (normalized == "formula" || normalized == "mf")
        {
            return "Formula";
        }

        if (normalized == "molecularweight" || normalized == "molweight" || normalized == "mw")
        {
            return "MolecularWeight";
        }

        if (normalized == "exactmass" || normalized == "monoisotopicmass")
        {
            return "ExactMass";
        }

        if (normalized == "nominalmass")
        {
            return "NominalMass";
        }

        if (normalized == "atomcount" || normalized == "atoms")
        {
            return "AtomCount";
        }

        if (normalized == "explicitatomcount" || normalized == "explicitatoms")
        {
            return "ExplicitAtomCount";
        }

        if (normalized == "heavyatomcount" || normalized == "heavyatoms")
        {
            return "HeavyAtomCount";
        }

        if (normalized == "hydrogenatomcount" || normalized == "hydrogens")
        {
            return "HydrogenAtomCount";
        }

        if (normalized == "heteroatomcount" || normalized == "heteroatoms")
        {
            return "HeteroAtomCount";
        }

        if (normalized == "carbonatomcount" || normalized == "carbons")
        {
            return "CarbonAtomCount";
        }

        if (normalized == "formalcharge" || normalized == "charge")
        {
            return "FormalCharge";
        }

        if (normalized == "rotatablebondcount" || normalized == "rotatablebonds")
        {
            return "RotatableBondCount";
        }

        if (normalized == "hydrogenbonddonorcount" || normalized == "hbd" || normalized == "donors")
        {
            return "HydrogenBondDonorCount";
        }

        if (normalized == "hydrogenbondacceptorcount" || normalized == "hba" || normalized == "acceptors")
        {
            return "HydrogenBondAcceptorCount";
        }

        if (normalized == "ringcount" || normalized == "rings")
        {
            return "RingCount";
        }

        if (normalized == "aromaticringcount" || normalized == "aromaticrings")
        {
            return "AromaticRingCount";
        }

        if (normalized == "topologicalpolarsurfacearea" || normalized == "tpsa")
        {
            return "TopologicalPolarSurfaceArea";
        }

        if (normalized == "fractioncsp3" || normalized == "fractionsp3" || normalized == "fsp3")
        {
            return "FractionCsp3";
        }

        if (normalized == "logp" || normalized == "xlogp" || normalized == "clogp")
        {
            return "LogP";
        }

        if (normalized == "molarrefractivity" || normalized == "mr")
        {
            return "MolarRefractivity";
        }

        return "";
    }

    internal string NormalizeName(string? name)
    {
        if (name == null)
        {
            return "";
        }

        string lowered = name.Trim().ToLowerInvariant();
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < lowered.Length)
        {
            char character = lowered[index];
            if (character != " "[0] && character != "-"[0] && character != "_"[0])
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal void AddName(string name)
    {
        Names.Add(name);
    }

    internal DescriptorValue CreateTextValue(string name, string text)
    {
        DescriptorValue value = new DescriptorValue();
        value.Name = name;
        value.Text = text;
        value.Number = 0.0;
        value.IsText = true;
        value.IsNumber = false;
        value.Found = true;
        value.Available = true;
        return value;
    }

    internal DescriptorValue CreateNumberValue(string name, double number)
    {
        DescriptorValue value = new DescriptorValue();
        value.Name = name;
        value.Text = NumberFormatting.FormatDouble(number);
        value.Number = number;
        value.IsText = false;
        value.IsNumber = true;
        value.Found = true;
        value.Available = true;
        return value;
    }

    internal DescriptorValue CreateEstimateValue(string name, DescriptorEstimate estimate)
    {
        if (estimate.Available)
        {
            return CreateNumberValue(name, estimate.Value);
        }

        DescriptorValue value = new DescriptorValue();
        value.Name = name;
        value.Found = true;
        value.Available = false;
        value.DiagnosticCode = estimate.DiagnosticCode;
        value.DiagnosticMessage = estimate.DiagnosticMessage;
        return value;
    }
}
