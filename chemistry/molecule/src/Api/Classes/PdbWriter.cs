using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Writes molecular objects as pdb data.
/// </summary>
public class PdbWriter : IMoleculeWriter, IPdbWriter
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdbWriter"/> class.
    /// </summary>
    public PdbWriter()
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
        return WritePdb(molecule);
    }

    /// <summary>
    /// Writes one molecule as PDB text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting PDB text; an empty string indicates that no value could be produced.</returns>
    public string WritePdb(Molecule? molecule)
    {
        return WritePdbWithResult(molecule).Text;
    }

    /// <summary>
    /// Writes one molecule as PDB and returns emitted record counts and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The output text, record counts, and diagnostics.</returns>
    public PdbWriteResult WritePdbWithResult(Molecule? molecule)
    {
        PdbWriteResult result = new PdbWriteResult();
        if (molecule == null)
        {
            result.Report.AddError("PDB_NULL_MOLECULE", "Cannot write a null molecule.");
            return result;
        }

        ValidateMolecule(molecule, result);
        if (result.Report.GetErrorCount() > 0)
        {
            return result;
        }

        StringBuilder builder = new StringBuilder();
        AppendHeader(builder, molecule);
        List<int> modelIds = CollectModelIds(molecule);
        if (modelIds.Count == 0)
        {
            AppendAtomRecords(builder, molecule);
            AppendConectRecords(builder, molecule);
        }
        else
        {
            AppendUnassignedAtomRecords(builder, molecule);
            AppendUnassignedConectRecords(builder, molecule);
            int modelIndex = 0;
            while (modelIndex < modelIds.Count)
            {
                int modelId = modelIds[modelIndex];
                builder.Append("MODEL     ");
                builder.Append(modelId.ToString().PadLeft(4));
                builder.Append('\n');
                AppendAtomRecordsForModel(builder, molecule, modelId);
                AppendConectRecordsForModel(builder, molecule, modelId);
                builder.Append("ENDMDL\n");
                modelIndex = modelIndex + 1;
            }
        }

        builder.Append("END\n");
        result.Text = builder.ToString();
        result.AtomRecordCount = molecule.Atoms.Count;
        result.ConectRecordCount = CountWritableConectRecords(molecule);
        result.ModelCount = modelIds.Count;
        result.ResidueCount = molecule.Residues.Count;
        result.ChainCount = molecule.Chains.Count;
        result.Success = true;
        return result;
    }

    internal void AppendUnassignedAtomRecords(StringBuilder builder, Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (GetAtomModelId(molecule, atomIndex) < 0)
            {
                AppendAtomRecord(builder, molecule, molecule.Atoms[atomIndex], atomIndex);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void AppendAtomRecordsForModel(StringBuilder builder, Molecule molecule, int modelId)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (GetAtomModelId(molecule, atomIndex) == modelId)
            {
                AppendAtomRecord(builder, molecule, molecule.Atoms[atomIndex], atomIndex);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void AppendHeader(StringBuilder builder, Molecule molecule)
    {
        string header = molecule.Metadata.GetPdbField("HEADER");
        if (header != "")
        {
            builder.Append("HEADER    ");
            builder.Append(NormalizeLineText(header));
            builder.Append('\n');
        }

        int index = 0;
        while (index < molecule.Metadata.Comments.Count)
        {
            builder.Append("TITLE     ");
            builder.Append(NormalizeLineText(molecule.Metadata.Comments[index]));
            builder.Append('\n');
            index = index + 1;
        }
    }

    internal void AppendAtomRecords(StringBuilder builder, Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            AppendAtomRecord(builder, molecule, molecule.Atoms[atomIndex], atomIndex);
            atomIndex = atomIndex + 1;
        }
    }

    internal void AppendAtomRecord(StringBuilder builder, Molecule molecule, Atom atom, int atomIndex)
    {
        string recordName = atom.QueryValue;
        if (recordName != "ATOM" && recordName != "HETATM")
        {
            recordName = GetAtomField(molecule, atomIndex, "RECORD");
        }

        if (recordName != "ATOM" && recordName != "HETATM")
        {
            recordName = "ATOM";
        }

        builder.Append(FitLeft(recordName, 6));
        AppendPaddedInt(builder, GetAtomSerial(molecule, atomIndex), 5);
        builder.Append(' ');
        builder.Append(FitRight(GetAtomName(atom), 4));
        builder.Append(FitLeft(GetAlternateLocation(molecule, atom, atomIndex), 1));
        builder.Append(FitLeft(GetResidueName(molecule, atom, atomIndex), 3));
        builder.Append(' ');
        builder.Append(FitLeft(GetChainId(molecule, atom, atomIndex), 1));
        AppendPaddedInt(builder, GetResidueId(molecule, atom, atomIndex), 4);
        builder.Append(FitLeft(GetInsertionCode(molecule, atom, atomIndex), 1));
        builder.Append("   ");
        builder.Append(FormatCoordinate(GetAtomX(atom)));
        builder.Append(FormatCoordinate(GetAtomY(atom)));
        builder.Append(FormatCoordinate(GetAtomZ(atom)));
        builder.Append(FitRight(GetOccupancy(molecule, atom, atomIndex), 6));
        builder.Append(FitRight(GetTemperatureFactor(molecule, atom, atomIndex), 6));
        builder.Append("          ");
        builder.Append(FitRight(GetElementSymbol(molecule, atom, atomIndex), 2));
        builder.Append(FitRight(GetChargeText(molecule, atom, atomIndex), 2));
        builder.Append('\n');
    }

    internal void AppendConectRecords(StringBuilder builder, Molecule molecule)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId))
            {
                builder.Append("CONECT");
                AppendPaddedInt(builder, GetAtomSerial(molecule, bond.BeginAtomId), 5);
                AppendPaddedInt(builder, GetAtomSerial(molecule, bond.EndAtomId), 5);
                builder.Append('\n');
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void AppendUnassignedConectRecords(StringBuilder builder, Molecule molecule)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId) && GetAtomModelId(molecule, bond.BeginAtomId) < 0 && GetAtomModelId(molecule, bond.EndAtomId) < 0)
            {
                AppendConectRecord(builder, molecule, bond);
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void AppendConectRecordsForModel(StringBuilder builder, Molecule molecule, int modelId)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId) && GetAtomModelId(molecule, bond.BeginAtomId) == modelId && GetAtomModelId(molecule, bond.EndAtomId) == modelId)
            {
                AppendConectRecord(builder, molecule, bond);
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void AppendConectRecord(StringBuilder builder, Molecule molecule, Bond bond)
    {
        builder.Append("CONECT");
        AppendPaddedInt(builder, GetAtomSerial(molecule, bond.BeginAtomId), 5);
        AppendPaddedInt(builder, GetAtomSerial(molecule, bond.EndAtomId), 5);
        builder.Append('\n');
    }

    internal List<int> CollectModelIds(Molecule molecule)
    {
        List<int> modelIds = new List<int>();
        int modelIndex = 0;
        while (modelIndex < molecule.Models.Count)
        {
            if (molecule.Models[modelIndex].Id >= 0)
            {
                AddUniqueInt(modelIds, molecule.Models[modelIndex].Id);
            }

            modelIndex = modelIndex + 1;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            int modelId = GetAtomModelId(molecule, atomIndex);
            if (modelId >= 0)
            {
                AddUniqueInt(modelIds, modelId);
            }

            atomIndex = atomIndex + 1;
        }

        return modelIds;
    }

    internal int GetAtomModelId(Molecule molecule, int atomIndex)
    {
        Atom atom = molecule.Atoms[atomIndex];
        if (atom.ModelId >= 0)
        {
            return atom.ModelId;
        }

        string value = GetAtomField(molecule, atomIndex, "MODEL");
        if (value != "")
        {
            return ParseInt(value);
        }

        int modelIndex = 0;
        while (modelIndex < molecule.Models.Count)
        {
            MoleculeModel model = molecule.Models[modelIndex];
            int memberIndex = 0;
            while (memberIndex < model.AtomIds.Count)
            {
                if (model.AtomIds[memberIndex] == atomIndex)
                {
                    return model.Id;
                }

                memberIndex = memberIndex + 1;
            }

            modelIndex = modelIndex + 1;
        }

        return -1;
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

    internal int CountWritableConectRecords(Molecule molecule)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId) && GetAtomModelId(molecule, bond.BeginAtomId) == GetAtomModelId(molecule, bond.EndAtomId))
            {
                count = count + 1;
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal void ValidateMolecule(Molecule molecule, PdbWriteResult result)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (!molecule.IsValidAtomId(bond.BeginAtomId) || !molecule.IsValidAtomId(bond.EndAtomId))
            {
                result.Report.AddError("PDB_INVALID_BOND_ATOM", "PDB bond references an invalid atom.");
            }
            else if (GetAtomModelId(molecule, bond.BeginAtomId) != GetAtomModelId(molecule, bond.EndAtomId))
            {
                result.Report.AddWarning("PDB_CROSS_MODEL_BOND_OMITTED", "PDB connectivity cannot represent a bond between different models.");
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal string GetFirstModelId(Molecule molecule)
    {
        if (molecule.Models.Count > 0 && molecule.Models[0].Id >= 0)
        {
            return molecule.Models[0].Id.ToString();
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            if (molecule.Atoms[atomIndex].ModelId >= 0)
            {
                return molecule.Atoms[atomIndex].ModelId.ToString();
            }

            string modelId = GetAtomField(molecule, atomIndex, "MODEL");
            if (modelId != "")
            {
                return modelId;
            }

            atomIndex = atomIndex + 1;
        }

        return "";
    }

    internal int GetAtomSerial(Molecule molecule, int atomIndex)
    {
        string value = GetAtomField(molecule, atomIndex, "SERIAL");
        if (value != "")
        {
            return ParseInt(value);
        }

        return atomIndex + 1;
    }

    internal string GetAlternateLocation(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.AlternateLocation != "")
        {
            return atom.AlternateLocation;
        }

        return GetAtomFieldOrDefault(molecule, atomIndex, "ALT_LOC", "");
    }

    internal string GetResidueName(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.ResidueName != "")
        {
            return atom.ResidueName;
        }

        return GetAtomFieldOrDefault(molecule, atomIndex, "RES_NAME", "UNK");
    }

    internal string GetChainId(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.ChainId != "")
        {
            return atom.ChainId;
        }

        return GetAtomFieldOrDefault(molecule, atomIndex, "CHAIN_ID", "A");
    }

    internal int GetResidueId(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.ResidueId >= 0)
        {
            return atom.ResidueId;
        }

        return ParseInt(GetAtomFieldOrDefault(molecule, atomIndex, "RES_SEQ", "1"));
    }

    internal string GetInsertionCode(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.InsertionCode != "")
        {
            return atom.InsertionCode;
        }

        return GetAtomFieldOrDefault(molecule, atomIndex, "INSERTION_CODE", "");
    }

    internal string GetOccupancy(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.HasOccupancy)
        {
            return NumberFormatting.FormatDoubleWithPattern(atom.Occupancy, "0.00");
        }

        return GetAtomFieldOrDefault(molecule, atomIndex, "OCCUPANCY", "1.00");
    }

    internal string GetTemperatureFactor(Molecule molecule, Atom atom, int atomIndex)
    {
        if (atom.HasTemperatureFactor)
        {
            return NumberFormatting.FormatDoubleWithPattern(atom.TemperatureFactor, "0.00");
        }

        return GetAtomFieldOrDefault(molecule, atomIndex, "B_FACTOR", "0.00");
    }

    internal string GetAtomFieldOrDefault(Molecule molecule, int atomIndex, string fieldName, string fallback)
    {
        string value = GetAtomField(molecule, atomIndex, fieldName);
        if (value != "")
        {
            return value;
        }

        return fallback;
    }

    internal string GetAtomField(Molecule molecule, int atomIndex, string fieldName)
    {
        return molecule.Metadata.GetPdbField("PDB_ATOM_" + atomIndex.ToString() + "_" + fieldName);
    }

    internal string GetAtomName(Atom atom)
    {
        if (NormalizeString(atom.Label) != "")
        {
            return atom.Label;
        }

        if (NormalizeString(atom.Alias) != "")
        {
            return atom.Alias;
        }

        return GetElementSymbolForAtom(atom);
    }

    internal string GetElementSymbol(Molecule molecule, Atom atom, int atomIndex)
    {
        string typedValue = GetElementSymbolForAtom(atom);
        if (typedValue != "")
        {
            return typedValue;
        }

        string value = GetAtomField(molecule, atomIndex, "ELEMENT");
        if (value != "")
        {
            return value;
        }

        return "";
    }

    internal string GetElementSymbolForAtom(Atom atom)
    {
        if (atom.AtomicNumber > 0)
        {
            Element element = Elements.GetElement(atom.AtomicNumber);
            if (element.Symbol != "")
            {
                return element.Symbol;
            }
        }

        return "";
    }

    internal string GetChargeText(Molecule molecule, Atom atom, int atomIndex)
    {
        string value = GetAtomField(molecule, atomIndex, "CHARGE");
        if (value != "")
        {
            return value;
        }

        if (atom.FormalCharge > 0)
        {
            return atom.FormalCharge.ToString() + "+";
        }

        if (atom.FormalCharge < 0)
        {
            return (0 - atom.FormalCharge).ToString() + "-";
        }

        return "";
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
        return NumberFormatting.FormatDoubleWithPattern(value, "0.000").PadLeft(8);
    }

    internal void AppendPaddedInt(StringBuilder builder, int value, int width)
    {
        builder.Append(value.ToString().PadLeft(width));
    }

    internal string FitLeft(string value, int width)
    {
        string text = NormalizeString(value);
        if (text.Length > width)
        {
            return text.Substring(0, width);
        }

        return text.PadRight(width);
    }

    internal string FitRight(string value, int width)
    {
        string text = NormalizeString(value);
        if (text.Length > width)
        {
            return text.Substring(0, width);
        }

        return text.PadLeft(width);
    }

    internal int ParseInt(string value)
    {
        int result = 0;
        int.TryParse(NormalizeString(value).Trim(), out result);
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
