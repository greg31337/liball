using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Reads v 2000 data into molecular objects.
/// </summary>
public class V2000Reader : IV2000Reader
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="V2000Reader"/> class.
    /// </summary>
    public V2000Reader()
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
        MolfileReadResult result = ReadWithResult(text);
        return result.Molecule;
    }

    /// <summary>
    /// Reads the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public MolfileReadResult ReadWithResult(string? text)
    {
        return ReadV2000WithOptions(text, null);
    }

    /// <summary>
    /// Reads MOL V2000 using explicit feature and count policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The molfile read options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The molecule, feature counts, and diagnostics.</returns>
    public MolfileReadResult ReadV2000WithOptions(string? text, MolfileReadOptions? options)
    {
        V2000ReaderState state = new V2000ReaderState();
        state.Options = NormalizeReadOptions(options);
        state.Text = NormalizeString(text);
        state.Lines = SplitLines(state.Text);
        state.Result.Success = true;
        state.Result.Version = MolfileVersion.V2000;

        if (state.Lines.Count < 4)
        {
            Fail(state, "MOLFILE_COUNTS_MISSING", "MOL V2000 input is missing the counts line.", 4, -1, -1);
            state.Result.Molecule = state.Builder.Build();
            return state.Result;
        }

        ReadHeader(state);
        ReadCountsLine(state);
        if (!state.Result.Success)
        {
            state.Result.Molecule = state.Builder.Build();
            return state.Result;
        }

        ReadAtomBlock(state);
        ReadBondBlock(state);
        ReadPropertyBlock(state);
        state.Result.Molecule = state.Builder.Build();
        return state.Result;
    }

    internal void ReadHeader(V2000ReaderState state)
    {
        state.Builder.Molecule.Metadata.AddName(state.Lines[0]);
        state.Builder.Molecule.Metadata.AddComment(state.Lines[1]);
        state.Builder.Molecule.Metadata.AddComment(state.Lines[2]);
    }

    internal void ReadCountsLine(V2000ReaderState state)
    {
        string line = state.Lines[3];
        if (line.Contains("V3000"))
        {
            Fail(state, "MOLFILE_NOT_V2000", "MOL input uses V3000, not V2000.", 4, -1, -1);
            return;
        }

        state.AtomCount = ParseIntField(state, Slice(line, 0, 3), 4, "atom count");
        state.BondCount = ParseIntField(state, Slice(line, 3, 3), 4, "bond count");
        state.Result.DeclaredAtomCount = state.AtomCount;
        state.Result.DeclaredBondCount = state.BondCount;

        if (!state.Result.Success)
        {
            return;
        }

        if (state.AtomCount < 0 || state.BondCount < 0)
        {
            Fail(state, "MOLFILE_INVALID_COUNTS", "MOL V2000 counts must be non-negative.", 4, -1, -1);
            return;
        }

        if (state.AtomCount > state.Options.MaximumAtomCount || state.BondCount > state.Options.MaximumBondCount)
        {
            Fail(state, "MOLFILE_COUNT_LIMIT", "MOL V2000 counts exceed the configured structure limits.", 4, -1, -1);
            return;
        }

        int requiredLineCount = 4 + state.AtomCount + state.BondCount;
        if (state.Lines.Count < requiredLineCount)
        {
            Fail(state, "MOLFILE_TRUNCATED", "MOL V2000 input ended before the atom and bond blocks were complete.", state.Lines.Count, -1, -1);
        }
    }

    internal void ReadAtomBlock(V2000ReaderState state)
    {
        int index = 0;
        while (index < state.AtomCount && state.Result.Success)
        {
            int lineIndex = 4 + index;
            ReadAtomLine(state, state.Lines[lineIndex], lineIndex + 1);
            index = index + 1;
        }
    }

    internal void ReadAtomLine(V2000ReaderState state, string line, int lineNumber)
    {
        double x = ParseDoubleField(state, SliceOrToken(line, 0, 10, 0), lineNumber, "x coordinate");
        double y = ParseDoubleField(state, SliceOrToken(line, 10, 10, 1), lineNumber, "y coordinate");
        double z = ParseDoubleField(state, SliceOrToken(line, 20, 10, 2), lineNumber, "z coordinate");
        string symbol = SliceOrToken(line, 31, 3, 3).Trim();
        int massDifference = ParseOptionalIntField(SliceOrToken(line, 34, 2, 4));
        int chargeCode = ParseOptionalIntField(SliceOrToken(line, 36, 3, 5));

        AtomSpec spec = AtomSpec.CreateEmpty();
        spec.Label = symbol;
        spec.AtomicNumber = GetAtomicNumber(state, symbol, lineNumber);
        if (state.Options.ReadCommonProperties)
        {
            spec.IsotopeMassNumber = GetIsotopeMassNumber(spec.AtomicNumber, massDifference);
            ApplyAtomBlockCharge(spec, chargeCode);
        }

        int atomId = state.Builder.AddAtom(spec);
        Atom atom = state.Builder.Molecule.Atoms[atomId];
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
    }

    internal void ReadBondBlock(V2000ReaderState state)
    {
        int index = 0;
        while (index < state.BondCount && state.Result.Success)
        {
            int lineIndex = 4 + state.AtomCount + index;
            ReadBondLine(state, state.Lines[lineIndex], lineIndex + 1);
            index = index + 1;
        }
    }

    internal void ReadBondLine(V2000ReaderState state, string line, int lineNumber)
    {
        int beginAtomId = ParseIntField(state, SliceOrToken(line, 0, 3, 0), lineNumber, "begin atom") - 1;
        int endAtomId = ParseIntField(state, SliceOrToken(line, 3, 3, 1), lineNumber, "end atom") - 1;
        int bondType = ParseIntField(state, SliceOrToken(line, 6, 3, 2), lineNumber, "bond type");
        int stereoCode = ParseOptionalIntField(SliceOrToken(line, 9, 3, 3));

        BondSpec spec = BondSpec.Create(MapBondOrder(bondType), bondType == 4, MapBondStereo(stereoCode), MapBondDirection(stereoCode));
        spec.SourceFlags = line;
        if (bondType == 9)
        {
            spec.IsDative = true;
        }

        int bondId = state.Builder.AddBond(beginAtomId, endAtomId, spec);
        if (bondId < 0)
        {
            Fail(state, "MOLFILE_INVALID_BOND_ATOM", "MOL V2000 bond references an invalid atom.", lineNumber, beginAtomId, -1);
        }
    }

    internal void ReadPropertyBlock(V2000ReaderState state)
    {
        int lineIndex = 4 + state.AtomCount + state.BondCount;
        while (lineIndex < state.Lines.Count && state.Result.Success)
        {
            string line = state.Lines[lineIndex];
            if (line.StartsWith("M  END"))
            {
                return;
            }

            if (line.StartsWith("M  CHG"))
            {
                state.Result.PropertyRecordCount = state.Result.PropertyRecordCount + 1;
                if (state.Options.ReadCommonProperties)
                {
                    ApplyIndexedIntRecord(state, line, lineIndex + 1, "charge");
                }
            }
            else if (line.StartsWith("M  ISO"))
            {
                state.Result.PropertyRecordCount = state.Result.PropertyRecordCount + 1;
                if (state.Options.ReadCommonProperties)
                {
                    ApplyIndexedIntRecord(state, line, lineIndex + 1, "isotope");
                }
            }
            else if (line.StartsWith("M  RAD"))
            {
                state.Result.PropertyRecordCount = state.Result.PropertyRecordCount + 1;
                if (state.Options.ReadCommonProperties)
                {
                    ApplyIndexedIntRecord(state, line, lineIndex + 1, "radical");
                }
            }
            else if (line.StartsWith("M  ALS"))
            {
                state.Result.AtomListCount = state.Result.AtomListCount + 1;
                if (state.Options.ReadAtomLists)
                {
                    ApplyAtomListRecord(state, line, lineIndex + 1);
                }
            }
            else if (line.StartsWith("A"))
            {
                state.Result.AliasCount = state.Result.AliasCount + 1;
                lineIndex = ReadAliasRecord(state, lineIndex);
            }

            lineIndex = lineIndex + 1;
        }
    }

    internal int ReadAliasRecord(V2000ReaderState state, int lineIndex)
    {
        string line = state.Lines[lineIndex];
        int atomNumber = ParseAliasAtomNumber(line) - 1;
        if (!state.Builder.Molecule.IsValidAtomId(atomNumber))
        {
            Fail(state, "MOLFILE_INVALID_ALIAS_ATOM", "MOL V2000 alias record references an invalid atom.", lineIndex + 1, atomNumber, -1);
            return lineIndex;
        }

        if (lineIndex + 1 >= state.Lines.Count)
        {
            Fail(state, "MOLFILE_ALIAS_MISSING_TEXT", "MOL V2000 alias record is missing alias text.", lineIndex + 1, atomNumber, -1);
            return lineIndex;
        }

        if (state.Options.ReadAliases)
        {
            state.Builder.Molecule.Atoms[atomNumber].Alias = state.Lines[lineIndex + 1];
        }

        return lineIndex + 1;
    }

    internal void ApplyAtomListRecord(V2000ReaderState state, string line, int lineNumber)
    {
        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 6)
        {
            Fail(state, "MOLFILE_INVALID_ATOM_LIST", "MOL V2000 atom-list record is incomplete.", lineNumber, -1, -1);
            return;
        }

        int atomId = ParseInt(tokens[2]) - 1;
        int count = ParseInt(tokens[3]);
        if (!state.Builder.Molecule.IsValidAtomId(atomId) || tokens.Length < 5 + count)
        {
            Fail(state, "MOLFILE_INVALID_ATOM_LIST", "MOL V2000 atom-list record has invalid atom or list values.", lineNumber, atomId, -1);
            return;
        }

        Atom atom = state.Builder.Molecule.Atoms[atomId];
        atom.QueryOperation = QueryOperation.InList;
        if (tokens[4] == "T")
        {
            atom.QueryOperation = QueryOperation.NotInList;
        }

        atom.QueryValue = "";
        int index = 0;
        while (index < count)
        {
            if (index > 0)
            {
                atom.QueryValue = atom.QueryValue + ",";
            }

            atom.QueryValue = atom.QueryValue + tokens[5 + index];
            index = index + 1;
        }
    }

    internal MolfileReadOptions NormalizeReadOptions(MolfileReadOptions? options)
    {
        MolfileReadOptions safeOptions = new MolfileReadOptions();
        if (options != null)
        {
            safeOptions.Version = options.Version;
            safeOptions.ReadAtomLists = options.ReadAtomLists;
            safeOptions.ReadAliases = options.ReadAliases;
            safeOptions.ReadCommonProperties = options.ReadCommonProperties;
            safeOptions.ReadEnhancedStereo = options.ReadEnhancedStereo;
            safeOptions.MaximumAtomCount = options.MaximumAtomCount;
            safeOptions.MaximumBondCount = options.MaximumBondCount;
        }

        return safeOptions;
    }

    internal int ParseAliasAtomNumber(string line)
    {
        string value = Slice(line, 1, line.Length - 1).Trim();
        return ParseInt(value);
    }

    internal void ApplyIndexedIntRecord(V2000ReaderState state, string line, int lineNumber, string recordKind)
    {
        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4)
        {
            Fail(state, "MOLFILE_INVALID_M_RECORD", "MOL V2000 property record is incomplete.", lineNumber, -1, -1);
            return;
        }

        int pairCount = ParseInt(tokens[2]);
        int tokenIndex = 3;
        int pairIndex = 0;
        while (pairIndex < pairCount)
        {
            if (tokenIndex + 1 >= tokens.Length)
            {
                Fail(state, "MOLFILE_INVALID_M_RECORD", "MOL V2000 property record has fewer atom/value pairs than declared.", lineNumber, -1, -1);
                return;
            }

            int atomId = ParseInt(tokens[tokenIndex]) - 1;
            int value = ParseInt(tokens[tokenIndex + 1]);
            ApplyPropertyValue(state, recordKind, atomId, value, lineNumber);
            tokenIndex = tokenIndex + 2;
            pairIndex = pairIndex + 1;
        }
    }

    internal void ApplyPropertyValue(V2000ReaderState state, string recordKind, int atomId, int value, int lineNumber)
    {
        if (!state.Builder.Molecule.IsValidAtomId(atomId))
        {
            Fail(state, "MOLFILE_INVALID_PROPERTY_ATOM", "MOL V2000 property record references an invalid atom.", lineNumber, atomId, -1);
            return;
        }

        Atom atom = state.Builder.Molecule.Atoms[atomId];
        if (recordKind == "charge")
        {
            atom.FormalCharge = value;
        }
        else if (recordKind == "isotope")
        {
            atom.IsotopeMassNumber = value;
        }
        else if (recordKind == "radical")
        {
            atom.RadicalElectronCount = MapRadicalElectrons(value);
        }
    }

    internal int GetAtomicNumber(V2000ReaderState state, string symbol, int lineNumber)
    {
        if (symbol == "" || symbol == "*" || symbol == "A" || symbol == "Q" || symbol == "L" || symbol == "R#")
        {
            return 0;
        }

        Element element = Elements.GetElementBySymbol(symbol);
        if (element.AtomicNumber == 0)
        {
            AddWarning(state, "MOLFILE_UNKNOWN_ELEMENT", "MOL V2000 atom symbol is not in the element table.", lineNumber, -1, -1);
        }

        return element.AtomicNumber;
    }

    internal int GetIsotopeMassNumber(int atomicNumber, int massDifference)
    {
        if (atomicNumber == 0 || massDifference == 0)
        {
            return 0;
        }

        Isotope isotope = Elements.GetMajorIsotope(atomicNumber);
        if (isotope.MassNumber == 0)
        {
            return 0;
        }

        return isotope.MassNumber + massDifference;
    }

    internal void ApplyAtomBlockCharge(AtomSpec spec, int chargeCode)
    {
        if (chargeCode == 1)
        {
            spec.FormalCharge = 3;
        }
        else if (chargeCode == 2)
        {
            spec.FormalCharge = 2;
        }
        else if (chargeCode == 3)
        {
            spec.FormalCharge = 1;
        }
        else if (chargeCode == 4)
        {
            spec.RadicalElectronCount = 1;
        }
        else if (chargeCode == 5)
        {
            spec.FormalCharge = -1;
        }
        else if (chargeCode == 6)
        {
            spec.FormalCharge = -2;
        }
        else if (chargeCode == 7)
        {
            spec.FormalCharge = -3;
        }
    }

    internal BondOrder MapBondOrder(int bondType)
    {
        if (bondType == 1)
        {
            return BondOrder.Single;
        }

        if (bondType == 2)
        {
            return BondOrder.Double;
        }

        if (bondType == 3)
        {
            return BondOrder.Triple;
        }

        if (bondType == 4)
        {
            return BondOrder.Aromatic;
        }

        if (bondType == 9)
        {
            return BondOrder.Dative;
        }

        return BondOrder.Unknown;
    }

    internal BondStereo MapBondStereo(int stereoCode)
    {
        if (stereoCode == 1)
        {
            return BondStereo.Wedge;
        }

        if (stereoCode == 6)
        {
            return BondStereo.Hash;
        }

        if (stereoCode == 4)
        {
            return BondStereo.UpOrDown;
        }

        if (stereoCode == 3)
        {
            return BondStereo.Cis;
        }

        return BondStereo.None;
    }

    internal BondDirection MapBondDirection(int stereoCode)
    {
        if (stereoCode == 1)
        {
            return BondDirection.Up;
        }

        if (stereoCode == 6)
        {
            return BondDirection.Down;
        }

        if (stereoCode == 4)
        {
            return BondDirection.Either;
        }

        return BondDirection.None;
    }

    internal int MapRadicalElectrons(int radicalCode)
    {
        if (radicalCode == 2)
        {
            return 1;
        }

        if (radicalCode == 1 || radicalCode == 3)
        {
            return 2;
        }

        return radicalCode;
    }

    internal int ParseIntField(V2000ReaderState state, string value, int lineNumber, string fieldName)
    {
        int result = 0;
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "MOLFILE_INVALID_INTEGER", "MOL V2000 field is not a valid integer: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal int ParseOptionalIntField(string value)
    {
        string trimmed = value.Trim();
        if (trimmed == "")
        {
            return 0;
        }

        return ParseInt(trimmed);
    }

    internal int ParseInt(string value)
    {
        int result = 0;
        int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        return result;
    }

    internal double ParseDoubleField(V2000ReaderState state, string value, int lineNumber, string fieldName)
    {
        double result = 0.0;
        if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "MOLFILE_INVALID_FLOAT", "MOL V2000 field is not a valid floating point number: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal string SliceOrToken(string line, int start, int length, int tokenIndex)
    {
        string value = Slice(line, start, length);
        if (value.Trim() != "")
        {
            return value;
        }

        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokenIndex >= 0 && tokenIndex < tokens.Length)
        {
            return tokens[tokenIndex];
        }

        return value;
    }

    internal string Slice(string line, int start, int length)
    {
        string text = NormalizeString(line);
        if (start < 0 || length <= 0 || start >= text.Length)
        {
            return "";
        }

        int availableLength = text.Length - start;
        if (availableLength < length)
        {
            return text.Substring(start, availableLength);
        }

        return text.Substring(start, length);
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

    internal void Fail(V2000ReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Success = false;
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("MOL", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Error));
    }

    internal void AddWarning(V2000ReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("MOL", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Warning));
    }

    internal string GetContext(V2000ReaderState state, int lineNumber)
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
