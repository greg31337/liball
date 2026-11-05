using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as mol 2 data.
/// </summary>
public class Mol2Writer : IMoleculeWriter, IMol2Writer
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mol2Writer"/> class.
    /// </summary>
    public Mol2Writer()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Writes one molecule to the configured format or output stream.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Write(Molecule? molecule)
    {
        return WriteMol2(molecule);
    }

    /// <summary>
    /// Writes one molecule as MOL2 text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting MOL2 text; an empty string indicates that no value could be produced.</returns>
    public string WriteMol2(Molecule? molecule)
    {
        return WriteMol2WithResult(molecule).Text;
    }

    /// <summary>
    /// Writes one molecule as MOL2 and returns section counts and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The output text, emitted fields, section counts, and diagnostics.</returns>
    public Mol2WriteResult WriteMol2WithResult(Molecule? molecule)
    {
        Mol2WriteResult result = new Mol2WriteResult();
        if (molecule == null)
        {
            result.Report.AddError("MOL2_NULL_MOLECULE", "Cannot write a null molecule.");
            return result;
        }

        ValidateMolecule(molecule, result);
        if (result.Report.GetErrorCount() > 0)
        {
            return result;
        }

        StringBuilder builder = new StringBuilder();
        AppendMoleculeSection(builder, molecule);
        AppendAtomSection(builder, molecule);
        AppendBondSection(builder, molecule);
        AppendSubstructureSection(builder, molecule);
        result.Text = builder.ToString();
        result.AtomCount = molecule.Atoms.Count;
        result.BondCount = molecule.Bonds.Count;
        result.SubstructureCount = CountSubstructures(molecule);
        result.MoleculeType = GetMoleculeType(molecule);
        result.ChargeType = GetChargeType(molecule);
        result.Success = true;
        return result;
    }

    internal void AppendMoleculeSection(StringBuilder builder, Molecule molecule)
    {
        builder.Append("@<TRIPOS>MOLECULE\n");
        builder.Append(NormalizeLineText(molecule.Metadata.GetPrimaryName()));
        builder.Append('\n');
        builder.Append(molecule.Atoms.Count);
        builder.Append(' ');
        builder.Append(molecule.Bonds.Count);
        builder.Append(' ');
        builder.Append(CountSubstructures(molecule));
        builder.Append(" 0 0\n");
        builder.Append(GetMoleculeType(molecule));
        builder.Append('\n');
        builder.Append(GetChargeType(molecule));
        builder.Append('\n');
        int commentIndex = 0;
        while (commentIndex < molecule.Metadata.Comments.Count)
        {
            builder.Append(NormalizeLineText(molecule.Metadata.Comments[commentIndex]));
            builder.Append('\n');
            commentIndex = commentIndex + 1;
        }
    }

    internal void AppendAtomSection(StringBuilder builder, Molecule molecule)
    {
        builder.Append("@<TRIPOS>ATOM\n");
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            AppendAtomLine(builder, molecule, molecule.Atoms[atomIndex], atomIndex);
            atomIndex = atomIndex + 1;
        }
    }

    internal void AppendAtomLine(StringBuilder builder, Molecule molecule, Atom atom, int atomIndex)
    {
        builder.Append(atomIndex + 1);
        builder.Append(' ');
        builder.Append(GetAtomName(atom, atomIndex));
        builder.Append(' ');
        builder.Append(FormatCoordinate(GetAtomX(atom)));
        builder.Append(' ');
        builder.Append(FormatCoordinate(GetAtomY(atom)));
        builder.Append(' ');
        builder.Append(FormatCoordinate(GetAtomZ(atom)));
        builder.Append(' ');
        builder.Append(GetAtomType(molecule, atom, atomIndex));

        if (ShouldWriteAtomOptionalFields(molecule, atomIndex))
        {
            builder.Append(' ');
            builder.Append(GetAtomResidueId(molecule, atomIndex));
            builder.Append(' ');
            builder.Append(GetAtomResidueName(molecule, atomIndex));
            builder.Append(' ');
            builder.Append(GetAtomCharge(molecule, atomIndex));
        }

        builder.Append('\n');
    }

    internal void AppendBondSection(StringBuilder builder, Molecule molecule)
    {
        builder.Append("@<TRIPOS>BOND\n");
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            builder.Append(bondIndex + 1);
            builder.Append(' ');
            builder.Append(bond.BeginAtomId + 1);
            builder.Append(' ');
            builder.Append(bond.EndAtomId + 1);
            builder.Append(' ');
            builder.Append(GetBondType(bond));
            builder.Append('\n');
            bondIndex = bondIndex + 1;
        }
    }

    internal void AppendSubstructureSection(StringBuilder builder, Molecule molecule)
    {
        List<int> residueIds = CollectResidueIds(molecule);
        if (residueIds.Count == 0)
        {
            return;
        }

        builder.Append("@<TRIPOS>SUBSTRUCTURE\n");
        int index = 0;
        while (index < residueIds.Count)
        {
            int residueId = residueIds[index];
            builder.Append(residueId);
            builder.Append(' ');
            builder.Append(GetResidueName(molecule, residueId));
            builder.Append(' ');
            builder.Append(GetResidueRootAtomNumber(molecule, residueId));
            builder.Append(' ');
            builder.Append(GetResidueType(molecule, residueId));
            builder.Append(" 0 ");
            builder.Append(GetResidueChainId(molecule, residueId));
            builder.Append(" **** 0 ROOT\n");
            index = index + 1;
        }
    }

    internal int CountSubstructures(Molecule molecule)
    {
        return CollectResidueIds(molecule).Count;
    }

    internal List<int> CollectResidueIds(Molecule molecule)
    {
        List<int> residueIds = new List<int>();
        int residueIndex = 0;
        while (residueIndex < molecule.Residues.Count)
        {
            if (molecule.Residues[residueIndex].Id >= 0)
            {
                AddUniqueInt(residueIds, molecule.Residues[residueIndex].Id);
            }

            residueIndex = residueIndex + 1;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (ShouldWriteAtomOptionalFields(molecule, atomIndex))
            {
                int residueId = ParseInt(GetAtomResidueId(molecule, atomIndex));
                if (residueId >= 0)
                {
                    AddUniqueInt(residueIds, residueId);
                }
            }

            atomIndex = atomIndex + 1;
        }

        return residueIds;
    }

    internal void AddUniqueInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return;
            }

            index = index + 1;
        }

        values.Add(value);
    }

    internal bool ShouldWriteAtomOptionalFields(Molecule molecule, int atomIndex)
    {
        Atom atom = molecule.Atoms[atomIndex];
        if (atom.ResidueId >= 0 || atom.ResidueName != "" || atom.HasPartialCharge)
        {
            return true;
        }

        if (GetAtomVendorField(molecule, atomIndex, "RESIDUE_ID") != "")
        {
            return true;
        }

        if (GetAtomVendorField(molecule, atomIndex, "RESIDUE_NAME") != "")
        {
            return true;
        }

        if (GetAtomVendorField(molecule, atomIndex, "CHARGE") != "")
        {
            return true;
        }

        if (FindResidueForAtom(molecule, atomIndex).Id >= 0)
        {
            return true;
        }

        return false;
    }

    internal string GetAtomResidueId(Molecule molecule, int atomIndex)
    {
        if (molecule.Atoms[atomIndex].ResidueId >= 0)
        {
            return molecule.Atoms[atomIndex].ResidueId.ToString();
        }

        string value = GetAtomVendorField(molecule, atomIndex, "RESIDUE_ID");
        if (value != "")
        {
            return value;
        }

        Residue residue = FindResidueForAtom(molecule, atomIndex);
        if (residue.Id >= 0)
        {
            return residue.Id.ToString();
        }

        return "1";
    }

    internal string GetAtomResidueName(Molecule molecule, int atomIndex)
    {
        if (molecule.Atoms[atomIndex].ResidueName != "")
        {
            return molecule.Atoms[atomIndex].ResidueName;
        }

        string value = GetAtomVendorField(molecule, atomIndex, "RESIDUE_NAME");
        if (value != "")
        {
            return value;
        }

        int residueId = ParseInt(GetAtomResidueId(molecule, atomIndex));
        Residue residue = FindResidue(molecule, residueId);
        if (residue.Id >= 0 && residue.Name != "")
        {
            return residue.Name;
        }

        return "UNL" + GetAtomResidueId(molecule, atomIndex);
    }

    internal string GetAtomCharge(Molecule molecule, int atomIndex)
    {
        Atom atom = molecule.Atoms[atomIndex];
        if (atom.HasPartialCharge)
        {
            return NumberFormatting.FormatDoubleWithPattern(atom.PartialCharge, "0.0000");
        }

        string value = GetAtomVendorField(molecule, atomIndex, "CHARGE");
        if (value != "")
        {
            return value;
        }

        return "0.0000";
    }

    internal string GetResidueName(Molecule molecule, int residueId)
    {
        Residue residue = FindResidue(molecule, residueId);
        if (residue.Id >= 0 && residue.Name != "")
        {
            return residue.Name;
        }

        string value = molecule.Metadata.GetVendorField("MOL2_SUBSTRUCTURE_" + residueId.ToString() + "_NAME");
        if (value != "")
        {
            return value;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (ShouldWriteAtomOptionalFields(molecule, atomIndex) && ParseInt(GetAtomResidueId(molecule, atomIndex)) == residueId)
            {
                return GetAtomResidueName(molecule, atomIndex);
            }

            atomIndex = atomIndex + 1;
        }

        return "UNL" + residueId.ToString();
    }

    internal string GetResidueChainId(Molecule molecule, int residueId)
    {
        Residue residue = FindResidue(molecule, residueId);
        if (residue.Id >= 0 && residue.ChainId != "")
        {
            return residue.ChainId;
        }

        string value = molecule.Metadata.GetVendorField("MOL2_SUBSTRUCTURE_" + residueId.ToString() + "_CHAIN");
        if (value != "")
        {
            return value;
        }

        return "****";
    }

    internal string GetResidueType(Molecule molecule, int residueId)
    {
        Residue residue = FindResidue(molecule, residueId);
        if (residue.Id >= 0 && residue.Type != "")
        {
            return residue.Type;
        }

        return "GROUP";
    }

    internal int GetResidueRootAtomNumber(Molecule molecule, int residueId)
    {
        Residue residue = FindResidue(molecule, residueId);
        if (residue.Id >= 0 && residue.RootAtomId >= 0)
        {
            return residue.RootAtomId + 1;
        }

        if (residue.Id >= 0 && residue.AtomIds.Count > 0)
        {
            int atomId = residue.AtomIds[0];
            if (atomId >= 0 && atomId < molecule.Atoms.Count)
            {
                return atomId + 1;
            }
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (ShouldWriteAtomOptionalFields(molecule, atomIndex) && ParseInt(GetAtomResidueId(molecule, atomIndex)) == residueId)
            {
                return atomIndex + 1;
            }

            atomIndex = atomIndex + 1;
        }

        return 1;
    }

    internal string GetAtomVendorField(Molecule molecule, int atomIndex, string fieldName)
    {
        return molecule.Metadata.GetVendorField("MOL2_ATOM_" + atomIndex.ToString() + "_" + fieldName);
    }

    internal string GetMoleculeType(Molecule molecule)
    {
        string value = molecule.Metadata.GetStringValue("MOL2_MOLECULE_TYPE");
        if (value != "")
        {
            return value;
        }

        return "SMALL";
    }

    internal string GetChargeType(Molecule molecule)
    {
        string value = molecule.Metadata.GetStringValue("MOL2_CHARGE_TYPE");
        if (value != "")
        {
            return value;
        }

        if (HasAtomCharges(molecule))
        {
            return "USER_CHARGES";
        }

        return "NO_CHARGES";
    }

    internal bool HasAtomCharges(Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (molecule.Atoms[atomIndex].HasPartialCharge)
            {
                return true;
            }

            if (GetAtomVendorField(molecule, atomIndex, "CHARGE") != "")
            {
                return true;
            }

            atomIndex = atomIndex + 1;
        }

        return false;
    }

    internal Residue FindResidue(Molecule molecule, int residueId)
    {
        int index = 0;
        while (index < molecule.Residues.Count)
        {
            if (molecule.Residues[index].Id == residueId)
            {
                return molecule.Residues[index];
            }

            index = index + 1;
        }

        return new Residue();
    }

    internal Residue FindResidueForAtom(Molecule molecule, int atomId)
    {
        int residueIndex = 0;
        while (residueIndex < molecule.Residues.Count)
        {
            Residue residue = molecule.Residues[residueIndex];
            int atomIndex = 0;
            while (atomIndex < residue.AtomIds.Count)
            {
                if (residue.AtomIds[atomIndex] == atomId)
                {
                    return residue;
                }

                atomIndex = atomIndex + 1;
            }

            residueIndex = residueIndex + 1;
        }

        return new Residue();
    }

    internal void ValidateMolecule(Molecule molecule, Mol2WriteResult result)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (bond.BeginAtomId < 0 || bond.BeginAtomId >= molecule.Atoms.Count || bond.EndAtomId < 0 || bond.EndAtomId >= molecule.Atoms.Count)
            {
                result.Report.AddError("MOL2_INVALID_BOND_ATOM", "MOL2 bond references an invalid atom.");
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal string GetAtomName(Atom atom, int atomIndex)
    {
        if (NormalizeString(atom.Label) != "")
        {
            return atom.Label;
        }

        if (NormalizeString(atom.Alias) != "")
        {
            return atom.Alias;
        }

        return GetAtomSymbol(atom) + (atomIndex + 1).ToString();
    }

    internal string GetAtomType(Molecule molecule, Atom atom, int atomIndex)
    {
        if (NormalizeString(atom.AtomType) != "")
        {
            return atom.AtomType;
        }

        string value = GetAtomVendorField(molecule, atomIndex, "TYPE");
        if (value != "")
        {
            return value;
        }

        if (NormalizeString(atom.QueryValue) != "")
        {
            return atom.QueryValue;
        }

        if (atom.IsAromatic)
        {
            return GetAtomSymbol(atom) + ".ar";
        }

        return GetAtomSymbol(atom);
    }

    internal string GetAtomSymbol(Atom atom)
    {
        if (atom.AtomicNumber > 0)
        {
            Element element = Elements.GetElement(atom.AtomicNumber);
            if (element.Symbol != "")
            {
                return element.Symbol;
            }
        }

        if (NormalizeString(atom.Label) != "")
        {
            return atom.Label;
        }

        return "Du";
    }

    internal string GetBondType(Bond bond)
    {
        if (NormalizeString(bond.QueryValue) != "")
        {
            return bond.QueryValue;
        }

        if (NormalizeString(bond.SourceFlags) == "am")
        {
            return "am";
        }

        if (bond.Order == BondOrder.Single)
        {
            return "1";
        }

        if (bond.Order == BondOrder.Double)
        {
            return "2";
        }

        if (bond.Order == BondOrder.Triple)
        {
            return "3";
        }

        if (bond.Order == BondOrder.Aromatic || bond.IsAromatic)
        {
            return "ar";
        }

        return "un";
    }

    internal double GetAtomX(Atom atom)
    {
        if (atom.HasCoordinates)
        {
            return atom.X;
        }

        return 0.0;
    }

    internal double GetAtomY(Atom atom)
    {
        if (atom.HasCoordinates)
        {
            return atom.Y;
        }

        return 0.0;
    }

    internal double GetAtomZ(Atom atom)
    {
        if (atom.HasCoordinates)
        {
            return atom.Z;
        }

        return 0.0;
    }

    internal string FormatCoordinate(double value)
    {
        return NumberFormatting.FormatDoubleWithPattern(value, "0.0000");
    }

    internal int ParseInt(string value)
    {
        int result = 0;
        int.TryParse(NormalizeString(value), out result);
        return result;
    }

    internal string NormalizeLineText(string value)
    {
        string text = NormalizeString(value);
        text = text.Replace('\r', ' ');
        text = text.Replace('\n', ' ');
        return text;
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
