using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Reads pdb data into molecular objects.
/// </summary>
public class PdbReader : IMoleculeReader, IPdbReader
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdbReader"/> class.
    /// </summary>
    public PdbReader()
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
        PdbReadResult result = ReadWithResult(text);
        return result.Molecule;
    }

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public PdbReadResult ReadWithResult(string? text)
    {
        PdbReaderState state = new PdbReaderState();
        state.Text = NormalizeString(text);
        state.Lines = SplitLines(state.Text);
        state.Result.Success = true;
        ReadLines(state);
        state.Result.Molecule = state.Builder.Build();
        state.Result.Residues = state.Result.Molecule.Residues;
        state.Result.Chains = state.Result.Molecule.Chains;
        state.Result.Models = state.Result.Molecule.Models;
        return state.Result;
    }

    /// <summary>
    /// Extracts non-water hetero residues as independently mutable ligand molecules.
    /// </summary>
    /// <param name="molecule">The parsed PDB molecular graph.</param>
    /// <param name="options">Ligand selection options; <c>null</c> uses non-water defaults.</param>
    /// <returns>The extracted ligands, source residues, counts, and diagnostics.</returns>
    public PdbLigandExtractionResult ExtractLigands(Molecule? molecule, PdbLigandOptions? options)
    {
        PdbLigandExtractionResult result = new PdbLigandExtractionResult();
        if (molecule == null)
        {
            result.Report.AddError("PDB_LIGAND_NULL_MOLECULE", "Cannot extract PDB ligands from a null molecule.");
            return result;
        }

        PdbLigandOptions safeOptions = NormalizeLigandOptions(options);
        int residueIndex = 0;
        while (residueIndex < molecule.Residues.Count)
        {
            Residue residue = molecule.Residues[residueIndex];
            if ((safeOptions.ModelId < 0 || residue.ModelId == safeOptions.ModelId) && IsHeteroResidue(molecule, residue))
            {
                result.CandidateCount = result.CandidateCount + 1;
                if (!safeOptions.IncludeWater && IsWaterResidue(residue))
                {
                    result.SkippedWaterCount = result.SkippedWaterCount + 1;
                }
                else if (safeOptions.IncludeSingleAtomResidues || residue.AtomIds.Count > 1)
                {
                    Molecule ligand = CreateLigandMolecule(molecule, residue);
                    if (ligand.Atoms.Count > 0)
                    {
                        result.Ligands.Add(ligand);
                        result.Residues.Add(residue);
                        result.ExtractedCount = result.ExtractedCount + 1;
                    }
                }
            }

            residueIndex = residueIndex + 1;
        }

        result.Success = true;
        return result;
    }

    /// <summary>
    /// Reads PDB text and extracts selected hetero residues as ligand molecules.
    /// </summary>
    /// <param name="text">The source PDB text.</param>
    /// <param name="options">Ligand selection options; <c>null</c> uses non-water defaults.</param>
    /// <returns>The extracted ligands, source residues, counts, and diagnostics.</returns>
    public PdbLigandExtractionResult ReadLigands(string? text, PdbLigandOptions? options)
    {
        PdbReadResult readResult = ReadWithResult(text);
        if (!readResult.Success)
        {
            PdbLigandExtractionResult failed = new PdbLigandExtractionResult();
            MergeReports(failed.Report, readResult.Report);
            return failed;
        }

        PdbLigandExtractionResult result = ExtractLigands(readResult.Molecule, options);
        MergeReports(result.Report, readResult.Report);
        return result;
    }

    internal void ReadLines(PdbReaderState state)
    {
        int index = 0;
        while (index < state.Lines.Count && state.Result.Success)
        {
            string line = state.Lines[index];
            string recordName = Slice(line, 0, 6).Trim();
            if (recordName == "ATOM" || recordName == "HETATM")
            {
                state.Result.AtomRecordCount = state.Result.AtomRecordCount + 1;
                ReadAtomRecord(state, line, index + 1, recordName);
            }
            else if (recordName == "CONECT")
            {
                state.Result.ConectRecordCount = state.Result.ConectRecordCount + 1;
                ReadConectRecord(state, line, index + 1);
            }
            else if (recordName == "MODEL")
            {
                string modelText = Slice(line, 10, 4).Trim();
                if (modelText == "")
                {
                    string[] tokens = SplitTokens(line);
                    if (tokens.Length > 1)
                    {
                        modelText = tokens[1];
                    }
                }

                if (modelText != "")
                {
                    state.CurrentModelId = ParseIntField(state, modelText, index + 1, "model identifier");
                }
            }
            else if (recordName == "ENDMDL")
            {
                state.CurrentModelId = -1;
            }
            else if (recordName == "HEADER")
            {
                state.Builder.Molecule.Metadata.AddPdbField("HEADER", Slice(line, 10, line.Length - 10).Trim());
            }
            else if (recordName == "TITLE")
            {
                state.Builder.Molecule.Metadata.AddComment(Slice(line, 10, line.Length - 10).Trim());
            }

            index = index + 1;
        }
    }

    internal void ReadAtomRecord(PdbReaderState state, string line, int lineNumber, string recordName)
    {
        int serial = ParseIntField(state, Slice(line, 6, 5), lineNumber, "atom serial");
        string atomName = Slice(line, 12, 4).Trim();
        string alternateLocation = Slice(line, 16, 1).Trim();
        string residueName = Slice(line, 17, 3).Trim();
        string chainId = Slice(line, 21, 1).Trim();
        int residueNumber = ParseIntField(state, Slice(line, 22, 4), lineNumber, "residue number");
        string insertionCode = Slice(line, 26, 1).Trim();
        double x = ParseDoubleField(state, Slice(line, 30, 8), lineNumber, "x coordinate");
        double y = ParseDoubleField(state, Slice(line, 38, 8), lineNumber, "y coordinate");
        double z = ParseDoubleField(state, Slice(line, 46, 8), lineNumber, "z coordinate");
        string occupancy = Slice(line, 54, 6).Trim();
        string bFactor = Slice(line, 60, 6).Trim();
        string elementSymbol = Slice(line, 76, 2).Trim();
        string chargeText = Slice(line, 78, 2).Trim();

        if (elementSymbol == "")
        {
            elementSymbol = ExtractElementSymbol(atomName);
        }

        AtomSpec spec = AtomSpec.CreateEmpty();
        spec.AtomicNumber = GetAtomicNumber(state, elementSymbol, lineNumber);
        spec.FormalCharge = ParsePdbCharge(chargeText);
        spec.Label = atomName;
        spec.Alias = atomName;
        spec.QueryValue = recordName;

        int atomId = state.Builder.AddAtom(spec);
        Atom atom = state.Builder.Molecule.Atoms[atomId];
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
        atom.ResidueId = residueNumber;
        atom.ResidueName = residueName;
        atom.ChainId = chainId;
        atom.ModelId = state.CurrentModelId;
        atom.AlternateLocation = alternateLocation;
        atom.InsertionCode = insertionCode;
        if (occupancy != "")
        {
            atom.Occupancy = ParseDoubleField(state, occupancy, lineNumber, "occupancy");
            atom.HasOccupancy = state.Result.Success;
        }

        if (bFactor != "")
        {
            atom.TemperatureFactor = ParseDoubleField(state, bFactor, lineNumber, "temperature factor");
            atom.HasTemperatureFactor = state.Result.Success;
        }

        state.Serials.Add(serial);
        state.AtomIds.Add(atomId);
        state.SerialModelIds.Add(state.CurrentModelId);
        AddAtomToModel(state.Builder.Molecule, state.CurrentModelId, atomId);

        AddAtomMetadata(state, atomId, serial, recordName, alternateLocation, residueName, chainId, residueNumber, insertionCode, occupancy, bFactor, elementSymbol, chargeText);
        AddResidueAndChain(state, residueName, chainId, residueNumber, insertionCode, atomId);
    }

    internal void AddAtomMetadata(PdbReaderState state, int atomId, int serial, string recordName, string alternateLocation, string residueName, string chainId, int residueNumber, string insertionCode, string occupancy, string bFactor, string elementSymbol, string chargeText)
    {
        MoleculeMetadata metadata = state.Builder.Molecule.Metadata;
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_SERIAL", serial.ToString());
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_RECORD", recordName);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_ALT_LOC", alternateLocation);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_RES_NAME", residueName);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_CHAIN_ID", chainId);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_RES_SEQ", residueNumber.ToString());
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_INSERTION_CODE", insertionCode);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_OCCUPANCY", occupancy);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_B_FACTOR", bFactor);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_ELEMENT", elementSymbol);
        metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_CHARGE", chargeText);
        if (state.CurrentModelId >= 0)
        {
            metadata.AddPdbField("PDB_ATOM_" + atomId.ToString() + "_MODEL", state.CurrentModelId.ToString());
        }
    }

    internal void AddAtomToModel(Molecule molecule, int modelId, int atomId)
    {
        if (modelId < 0)
        {
            return;
        }

        int index = 0;
        while (index < molecule.Models.Count)
        {
            if (molecule.Models[index].Id == modelId)
            {
                molecule.Models[index].AtomIds.Add(atomId);
                return;
            }

            index = index + 1;
        }

        MoleculeModel model = new MoleculeModel();
        model.Id = modelId;
        model.Name = "MODEL " + modelId.ToString();
        model.AtomIds.Add(atomId);
        molecule.Models.Add(model);
    }

    internal void AddResidueAndChain(PdbReaderState state, string residueName, string chainId, int residueNumber, string insertionCode, int atomId)
    {
        Residue residue = FindResidue(state.Builder.Molecule.Residues, residueName, chainId, residueNumber, insertionCode, state.CurrentModelId);
        if (residue.Id < 0)
        {
            residue = new Residue();
            residue.Id = residueNumber;
            residue.Name = residueName;
            residue.ChainId = chainId;
            residue.InsertionCode = insertionCode;
            residue.ModelId = state.CurrentModelId;
            residue.RootAtomId = atomId;
            state.Builder.Molecule.Residues.Add(residue);
        }

        residue.AtomIds.Add(atomId);
        Chain chain = FindChain(state.Builder.Molecule.Chains, chainId, state.CurrentModelId);
        if (chain.Id == "")
        {
            chain = new Chain();
            chain.Id = chainId;
            chain.ModelId = state.CurrentModelId;
            state.Builder.Molecule.Chains.Add(chain);
        }

        AddUniqueInt(chain.ResidueIds, residueNumber);
    }

    internal void ReadConectRecord(PdbReaderState state, string line, int lineNumber)
    {
        string[] tokens = SplitTokens(line);
        if (tokens.Length < 3)
        {
            return;
        }

        int beginSerial = ParseIntField(state, tokens[1], lineNumber, "CONECT atom serial");
        int beginAtomId = ResolveSerial(state, beginSerial, state.CurrentModelId);
        int index = 2;
        while (index < tokens.Length)
        {
            int endSerial = ParseIntField(state, tokens[index], lineNumber, "CONECT bonded atom serial");
            int endAtomId = ResolveSerial(state, endSerial, state.CurrentModelId);
            if (beginAtomId >= 0 && endAtomId >= 0 && state.Builder.Molecule.GetBondIdBetween(beginAtomId, endAtomId) < 0)
            {
                state.Builder.AddBond(beginAtomId, endAtomId, new BondSpec());
            }
            else if (beginAtomId < 0 || endAtomId < 0)
            {
                AddWarning(state, "PDB_CONECT_UNKNOWN_SERIAL", "PDB CONECT record references an unknown atom serial.", lineNumber, -1, -1);
            }

            index = index + 1;
        }
    }

    internal Residue FindResidue(List<Residue> residues, string residueName, string chainId, int residueNumber, string insertionCode, int modelId)
    {
        int index = 0;
        while (index < residues.Count)
        {
            Residue residue = residues[index];
            if (residue.Id == residueNumber && residue.Name == residueName && residue.ChainId == chainId && residue.InsertionCode == insertionCode && residue.ModelId == modelId)
            {
                return residue;
            }

            index = index + 1;
        }

        return new Residue();
    }

    internal Chain FindChain(List<Chain> chains, string chainId, int modelId)
    {
        int index = 0;
        while (index < chains.Count)
        {
            if (chains[index].Id == chainId && chains[index].ModelId == modelId)
            {
                return chains[index];
            }

            index = index + 1;
        }

        return new Chain();
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

    internal int ResolveSerial(PdbReaderState state, int serial, int modelId)
    {
        int index = 0;
        while (index < state.Serials.Count)
        {
            if (state.Serials[index] == serial && (modelId < 0 || state.SerialModelIds[index] == modelId))
            {
                return state.AtomIds[index];
            }

            index = index + 1;
        }

        return -1;
    }

    internal PdbLigandOptions NormalizeLigandOptions(PdbLigandOptions? options)
    {
        if (options == null)
        {
            return new PdbLigandOptions();
        }

        return options;
    }

    internal bool IsHeteroResidue(Molecule molecule, Residue residue)
    {
        int index = 0;
        while (index < residue.AtomIds.Count)
        {
            int atomId = residue.AtomIds[index];
            if (atomId >= 0 && atomId < molecule.Atoms.Count)
            {
                Atom atom = molecule.Atoms[atomId];
                if (atom.QueryValue == "HETATM" || molecule.Metadata.GetPdbField("PDB_ATOM_" + atomId.ToString() + "_RECORD") == "HETATM")
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsWaterResidue(Residue residue)
    {
        string name = NormalizeString(residue.Name).Trim();
        return name == "HOH" || name == "WAT" || name == "H2O" || name == "DOD";
    }

    internal Molecule CreateLigandMolecule(Molecule molecule, Residue residue)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.Initialize(molecule);
        int atomId = molecule.Atoms.Count - 1;
        while (atomId >= 0)
        {
            if (!ContainsInt(residue.AtomIds, atomId))
            {
                builder.RemoveAtom(atomId);
            }

            atomId = atomId - 1;
        }

        Molecule ligand = builder.Build();
        ligand.Metadata.PdbFields.Clear();
        return ligand;
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void MergeReports(MoleculeReport target, MoleculeReport source)
    {
        int index = 0;
        while (index < source.GetCount())
        {
            target.Add(source.Get(index));
            index = index + 1;
        }
    }

    internal int GetAtomicNumber(PdbReaderState state, string elementSymbol, int lineNumber)
    {
        if (elementSymbol == "")
        {
            return 0;
        }

        Element element = Elements.GetElementBySymbol(NormalizeElementSymbol(elementSymbol));
        if (element.AtomicNumber == 0)
        {
            AddWarning(state, "PDB_UNKNOWN_ELEMENT", "PDB element symbol is not in the element table.", lineNumber, -1, -1);
        }

        return element.AtomicNumber;
    }

    internal string ExtractElementSymbol(string atomName)
    {
        string text = NormalizeString(atomName).Trim();
        int index = 0;
        while (index < text.Length && (text[index] >= '0' && text[index] <= '9'))
        {
            index = index + 1;
        }

        if (index < text.Length)
        {
            if (index + 1 < text.Length && IsLowercaseLetter(text[index + 1]))
            {
                return text.Substring(index, 2);
            }

            return text.Substring(index, 1);
        }

        return "";
    }

    internal string NormalizeElementSymbol(string symbol)
    {
        string value = NormalizeString(symbol).Trim();
        if (value.Length == 0)
        {
            return "";
        }

        if (value.Length == 1)
        {
            return value;
        }

        if (IsLowercaseLetter(value[1]))
        {
            return value.Substring(0, 2);
        }

        return value.Substring(0, 1);
    }

    internal bool IsLowercaseLetter(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal int ParsePdbCharge(string chargeText)
    {
        string value = NormalizeString(chargeText).Trim();
        if (value.Length < 2)
        {
            return 0;
        }

        int magnitude = ParseInt(Slice(value, 0, 1));
        string sign = Slice(value, 1, 1);
        if (sign == "-")
        {
            return 0 - magnitude;
        }

        if (sign == "+")
        {
            return magnitude;
        }

        return 0;
    }

    internal int ParseIntField(PdbReaderState state, string value, int lineNumber, string fieldName)
    {
        int result = 0;
        if (!int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "PDB_INVALID_INTEGER", "PDB field is not a valid integer: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal int ParseInt(string value)
    {
        int result = 0;
        int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        return result;
    }

    internal double ParseDoubleField(PdbReaderState state, string value, int lineNumber, string fieldName)
    {
        double result = 0.0;
        if (!double.TryParse(NormalizeString(value).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "PDB_INVALID_FLOAT", "PDB field is not a valid floating point number: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal string[] SplitTokens(string text)
    {
        return NormalizeString(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    internal string Slice(string text, int start, int length)
    {
        string value = NormalizeString(text);
        if (start < 0 || length <= 0 || start >= value.Length)
        {
            return "";
        }

        int availableLength = value.Length - start;
        if (availableLength < length)
        {
            return value.Substring(start, availableLength);
        }

        return value.Substring(start, length);
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

    internal void Fail(PdbReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Success = false;
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("PDB", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Error));
    }

    internal void AddWarning(PdbReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("PDB", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Warning));
    }

    internal string GetContext(PdbReaderState state, int lineNumber)
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
