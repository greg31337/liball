using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Reads mol 2 data into molecular objects.
/// </summary>
public class Mol2Reader : IMoleculeReader, IMol2Reader
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mol2Reader"/> class.
    /// </summary>
    public Mol2Reader()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Reads one molecule from the supplied format text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Read(string? text)
    {
        Mol2ReadResult result = ReadWithResult(text);
        return result.Molecule;
    }

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public Mol2ReadResult ReadWithResult(string? text)
    {
        Mol2ReaderState state = new Mol2ReaderState();
        state.Text = NormalizeString(text);
        state.Lines = SplitLines(state.Text);
        state.Result.Success = true;
        ReadSections(state);
        AttachResidueAtomIds(state);
        if (!state.SawMolecule)
        {
            Fail(state, "MOL2_MOLECULE_MISSING", "MOL2 input is missing the MOLECULE section.", 1, -1, -1);
        }

        state.Result.Molecule = state.Builder.Build();
        state.Result.Residues = state.Result.Molecule.Residues;
        return state.Result;
    }

    internal void ReadSections(Mol2ReaderState state)
    {
        int index = 0;
        while (index < state.Lines.Count && state.Result.Success)
        {
            string line = state.Lines[index].Trim();
            if (line == "@<TRIPOS>MOLECULE")
            {
                index = ReadMoleculeSection(state, index + 1);
            }
            else if (line == "@<TRIPOS>ATOM")
            {
                index = ReadAtomSection(state, index + 1);
            }
            else if (line == "@<TRIPOS>BOND")
            {
                index = ReadBondSection(state, index + 1);
            }
            else if (line == "@<TRIPOS>SUBSTRUCTURE")
            {
                index = ReadSubstructureSection(state, index + 1);
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal int ReadMoleculeSection(Mol2ReaderState state, int index)
    {
        state.SawMolecule = true;
        if (index < state.Lines.Count)
        {
            state.Builder.Molecule.Metadata.AddName(state.Lines[index].Trim());
            index = index + 1;
        }

        if (index < state.Lines.Count)
        {
            ReadCountsLine(state, state.Lines[index], index + 1);
            index = index + 1;
        }

        if (index < state.Lines.Count && !IsSectionHeader(state.Lines[index]))
        {
            state.Result.MoleculeType = state.Lines[index].Trim();
            state.Builder.Molecule.Metadata.SetStringValue("MOL2_MOLECULE_TYPE", state.Result.MoleculeType);
            index = index + 1;
        }

        if (index < state.Lines.Count && !IsSectionHeader(state.Lines[index]))
        {
            state.Result.ChargeType = state.Lines[index].Trim();
            state.Builder.Molecule.Metadata.SetStringValue("MOL2_CHARGE_TYPE", state.Result.ChargeType);
            index = index + 1;
        }

        while (index < state.Lines.Count && !IsSectionHeader(state.Lines[index]))
        {
            state.Builder.Molecule.Metadata.AddComment(state.Lines[index]);
            index = index + 1;
        }

        return index;
    }

    internal void ReadCountsLine(Mol2ReaderState state, string line, int lineNumber)
    {
        string[] tokens = SplitTokens(line);
        if (tokens.Length > 0)
        {
            state.AtomCount = ParseIntField(state, tokens[0], lineNumber, "atom count");
            state.Result.DeclaredAtomCount = state.AtomCount;
        }

        if (tokens.Length > 1)
        {
            state.BondCount = ParseIntField(state, tokens[1], lineNumber, "bond count");
            state.Result.DeclaredBondCount = state.BondCount;
        }

        if (tokens.Length > 2)
        {
            state.SubstructureCount = ParseIntField(state, tokens[2], lineNumber, "substructure count");
            state.Result.DeclaredSubstructureCount = state.SubstructureCount;
        }
    }

    internal int ReadAtomSection(Mol2ReaderState state, int index)
    {
        while (index < state.Lines.Count && !IsSectionHeader(state.Lines[index]) && state.Result.Success)
        {
            if (state.Lines[index].Trim() != "")
            {
                ReadAtomLine(state, state.Lines[index], index + 1);
            }

            index = index + 1;
        }

        return index;
    }

    internal void ReadAtomLine(Mol2ReaderState state, string line, int lineNumber)
    {
        string[] tokens = SplitTokens(line);
        if (tokens.Length < 6)
        {
            Fail(state, "MOL2_INVALID_ATOM", "MOL2 atom line is incomplete.", lineNumber, -1, -1);
            return;
        }

        int sourceAtomId = ParseIntField(state, tokens[0], lineNumber, "atom id");
        string atomName = tokens[1];
        double x = ParseDoubleField(state, tokens[2], lineNumber, "x coordinate");
        double y = ParseDoubleField(state, tokens[3], lineNumber, "y coordinate");
        double z = ParseDoubleField(state, tokens[4], lineNumber, "z coordinate");
        string atomType = tokens[5];

        AtomSpec spec = AtomSpec.CreateEmpty();
        spec.AtomicNumber = GetAtomicNumber(state, atomType, atomName, lineNumber);
        spec.Label = atomName;
        spec.Alias = atomName;
        spec.QueryValue = atomType;

        int atomId = state.Builder.AddAtom(spec);
        Atom atom = state.Builder.Molecule.Atoms[atomId];
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
        atom.AtomType = atomType;
        atom.ResidueId = ReadOptionalTokenInt(tokens, 6);
        if (tokens.Length > 7)
        {
            atom.ResidueName = tokens[7];
        }

        if (tokens.Length > 8)
        {
            atom.PartialCharge = ParseDoubleField(state, tokens[8], lineNumber, "partial charge");
            atom.HasPartialCharge = state.Result.Success;
        }

        state.SourceAtomIds.Add(sourceAtomId);
        state.MoleculeAtomIds.Add(atomId);
        state.AtomResidueIds.Add(atom.ResidueId);
        AddAtomMetadata(state, atomId, tokens);
    }

    internal void AddAtomMetadata(Mol2ReaderState state, int atomId, string[] tokens)
    {
        state.Builder.Molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_TYPE", tokens[5]);
        if (tokens.Length > 6)
        {
            state.Builder.Molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_RESIDUE_ID", tokens[6]);
        }

        if (tokens.Length > 7)
        {
            state.Builder.Molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_RESIDUE_NAME", tokens[7]);
        }

        if (tokens.Length > 8)
        {
            state.Builder.Molecule.Metadata.AddVendorField("MOL2_ATOM_" + atomId.ToString() + "_CHARGE", tokens[8]);
        }
    }

    internal int ReadBondSection(Mol2ReaderState state, int index)
    {
        while (index < state.Lines.Count && !IsSectionHeader(state.Lines[index]) && state.Result.Success)
        {
            if (state.Lines[index].Trim() != "")
            {
                ReadBondLine(state, state.Lines[index], index + 1);
            }

            index = index + 1;
        }

        return index;
    }

    internal void ReadBondLine(Mol2ReaderState state, string line, int lineNumber)
    {
        string[] tokens = SplitTokens(line);
        if (tokens.Length < 4)
        {
            Fail(state, "MOL2_INVALID_BOND", "MOL2 bond line is incomplete.", lineNumber, -1, -1);
            return;
        }

        int beginAtomId = ResolveAtomId(state, ParseIntField(state, tokens[1], lineNumber, "begin atom"));
        int endAtomId = ResolveAtomId(state, ParseIntField(state, tokens[2], lineNumber, "end atom"));
        string bondType = tokens[3];
        BondSpec spec = BondSpec.Create(MapBondOrder(bondType), IsAromaticBondType(bondType), BondStereo.None, BondDirection.None);
        spec.QueryValue = bondType;
        spec.SourceFlags = bondType;
        int bondId = state.Builder.AddBond(beginAtomId, endAtomId, spec);
        if (bondId < 0)
        {
            Fail(state, "MOL2_INVALID_BOND_ATOM", "MOL2 bond references an invalid atom.", lineNumber, beginAtomId, -1);
        }
    }

    internal int ReadSubstructureSection(Mol2ReaderState state, int index)
    {
        while (index < state.Lines.Count && !IsSectionHeader(state.Lines[index]) && state.Result.Success)
        {
            if (state.Lines[index].Trim() != "")
            {
                ReadSubstructureLine(state, state.Lines[index], index + 1);
            }

            index = index + 1;
        }

        return index;
    }

    internal void ReadSubstructureLine(Mol2ReaderState state, string line, int lineNumber)
    {
        string[] tokens = SplitTokens(line);
        if (tokens.Length < 3)
        {
            Fail(state, "MOL2_INVALID_SUBSTRUCTURE", "MOL2 substructure line is incomplete.", lineNumber, -1, -1);
            return;
        }

        Residue residue = new Residue();
        residue.Id = ParseIntField(state, tokens[0], lineNumber, "substructure id");
        residue.Name = tokens[1];
        residue.RootAtomId = ResolveAtomId(state, ParseIntField(state, tokens[2], lineNumber, "root atom"));
        if (tokens.Length > 3)
        {
            residue.Type = tokens[3];
        }

        if (tokens.Length > 5)
        {
            residue.ChainId = tokens[5];
            if (residue.ChainId == "****")
            {
                residue.ChainId = "";
            }
        }

        state.Builder.Molecule.Residues.Add(residue);
        AddChain(state.Builder.Molecule, residue);
        state.Builder.Molecule.Metadata.AddVendorField("MOL2_SUBSTRUCTURE_" + residue.Id.ToString() + "_NAME", residue.Name);
        state.Builder.Molecule.Metadata.AddVendorField("MOL2_SUBSTRUCTURE_" + residue.Id.ToString() + "_CHAIN", residue.ChainId);
    }

    internal void AttachResidueAtomIds(Mol2ReaderState state)
    {
        int atomIndex = 0;
        while (atomIndex < state.AtomResidueIds.Count)
        {
            int residueId = state.AtomResidueIds[atomIndex];
            Residue residue = FindResidue(state.Builder.Molecule.Residues, residueId);
            if (residue.Id >= 0)
            {
                residue.AtomIds.Add(state.MoleculeAtomIds[atomIndex]);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void AddChain(Molecule molecule, Residue residue)
    {
        if (residue.ChainId == "")
        {
            return;
        }

        int index = 0;
        while (index < molecule.Chains.Count)
        {
            if (molecule.Chains[index].Id == residue.ChainId)
            {
                AddUniqueInt(molecule.Chains[index].ResidueIds, residue.Id);
                return;
            }

            index = index + 1;
        }

        Chain chain = new Chain();
        chain.Id = residue.ChainId;
        chain.ResidueIds.Add(residue.Id);
        molecule.Chains.Add(chain);
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

    internal Residue FindResidue(List<Residue> residues, int residueId)
    {
        int index = 0;
        while (index < residues.Count)
        {
            if (residues[index].Id == residueId)
            {
                return residues[index];
            }

            index = index + 1;
        }

        return new Residue();
    }

    internal int ResolveAtomId(Mol2ReaderState state, int sourceAtomId)
    {
        int index = 0;
        while (index < state.SourceAtomIds.Count)
        {
            if (state.SourceAtomIds[index] == sourceAtomId)
            {
                return state.MoleculeAtomIds[index];
            }

            index = index + 1;
        }

        return -1;
    }

    internal int ReadOptionalTokenInt(string[] tokens, int index)
    {
        if (index < 0 || index >= tokens.Length)
        {
            return -1;
        }

        return ParseInt(tokens[index]);
    }

    internal int GetAtomicNumber(Mol2ReaderState state, string atomType, string atomName, int lineNumber)
    {
        string symbol = ExtractElementSymbol(atomType);
        Element element = Elements.GetElementBySymbol(symbol);
        if (element.AtomicNumber == 0)
        {
            symbol = ExtractElementSymbol(atomName);
            element = Elements.GetElementBySymbol(symbol);
        }

        if (element.AtomicNumber == 0)
        {
            AddWarning(state, "MOL2_UNKNOWN_ELEMENT", "MOL2 atom type does not map to a known element.", lineNumber, -1, -1);
        }

        return element.AtomicNumber;
    }

    internal string ExtractElementSymbol(string value)
    {
        string text = NormalizeString(value);
        int dotIndex = text.IndexOf(".");
        if (dotIndex > 0)
        {
            text = text.Substring(0, dotIndex);
        }

        if (text.Length >= 2 && IsLowercaseLetter(text[1]))
        {
            return text.Substring(0, 2);
        }

        if (text.Length >= 2 && IsUppercaseLetter(text[0]) && IsUppercaseLetter(text[1]))
        {
            return text.Substring(0, 1);
        }

        if (text.Length >= 1)
        {
            return text.Substring(0, 1);
        }

        return "";
    }

    internal bool IsUppercaseLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    internal bool IsLowercaseLetter(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal BondOrder MapBondOrder(string bondType)
    {
        string value = NormalizeString(bondType);
        if (value == "1" || value == "am")
        {
            return BondOrder.Single;
        }

        if (value == "2")
        {
            return BondOrder.Double;
        }

        if (value == "3")
        {
            return BondOrder.Triple;
        }

        if (value == "ar")
        {
            return BondOrder.Aromatic;
        }

        if (value == "du" || value == "un" || value == "nc")
        {
            return BondOrder.Unknown;
        }

        return BondOrder.Unknown;
    }

    internal bool IsAromaticBondType(string bondType)
    {
        return NormalizeString(bondType) == "ar";
    }

    internal bool IsSectionHeader(string line)
    {
        return NormalizeString(line).Trim().StartsWith("@<TRIPOS>");
    }

    internal string[] SplitTokens(string text)
    {
        return NormalizeString(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    internal int ParseIntField(Mol2ReaderState state, string value, int lineNumber, string fieldName)
    {
        int result = 0;
        if (!int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "MOL2_INVALID_INTEGER", "MOL2 field is not a valid integer: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal int ParseInt(string value)
    {
        int result = 0;
        int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        return result;
    }

    internal double ParseDoubleField(Mol2ReaderState state, string value, int lineNumber, string fieldName)
    {
        double result = 0.0;
        if (!double.TryParse(NormalizeString(value).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "MOL2_INVALID_FLOAT", "MOL2 field is not a valid floating point number: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal List<string> SplitLines(string text)
    {
        List<string> lines = new List<string>();
        string normalized = NormalizeString(text);
        int start = 0;
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (character == '\r' || character == '\n')
            {
                lines.Add(normalized.Substring(start, index - start));
                if (character == '\r' && index + 1 < normalized.Length && normalized[index + 1] == '\n')
                {
                    index = index + 1;
                }

                index = index + 1;
                start = index;
            }
            else
            {
                index = index + 1;
            }
        }

        if (start < normalized.Length)
        {
            lines.Add(normalized.Substring(start, normalized.Length - start));
        }

        return lines;
    }

    internal void Fail(Mol2ReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Success = false;
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("MOL2", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Error));
    }

    internal void AddWarning(Mol2ReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("MOL2", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Warning));
    }

    internal string GetContext(Mol2ReaderState state, int lineNumber)
    {
        int index = lineNumber - 1;
        if (index < 0 || index >= state.Lines.Count)
        {
            return "";
        }

        return state.Lines[index];
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
