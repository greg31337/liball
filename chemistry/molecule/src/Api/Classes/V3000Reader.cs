using System;
using System.Collections.Generic;
using System.Globalization;

namespace molecule;


/// <summary>
/// Reads v 3000 data into molecular objects.
/// </summary>
public class V3000Reader : IV3000Reader
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="V3000Reader"/> class.
    /// </summary>
    public V3000Reader()
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
        return ReadV3000WithOptions(text, null);
    }

    /// <summary>
    /// Reads MOL V3000 using explicit feature and count policies.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The molfile read options; <c>null</c> uses feature-preserving defaults.</param>
    /// <returns>The molecule, feature counts, and diagnostics.</returns>
    public MolfileReadResult ReadV3000WithOptions(string? text, MolfileReadOptions? options)
    {
        V3000ReaderState state = new V3000ReaderState();
        state.Options = NormalizeReadOptions(options);
        state.Text = NormalizeString(text);
        state.Lines = JoinContinuationLines(SplitLines(state.Text));
        state.Result.Success = true;
        state.Result.Version = MolfileVersion.V3000;

        if (state.Lines.Count < 4)
        {
            Fail(state, "MOLFILE_COUNTS_MISSING", "MOL V3000 input is missing the counts line.", 4, -1, -1);
            state.Result.Molecule = state.Builder.Build();
            return state.Result;
        }

        if (!ContainsV3000Marker(state))
        {
            Fail(state, "MOLFILE_NOT_V3000", "MOL input is not marked as V3000.", 4, -1, -1);
            state.Result.Molecule = state.Builder.Build();
            return state.Result;
        }

        ReadHeader(state);
        ReadBlocks(state);
        ApplyStereoHintsAndCollections(state);
        state.Result.Molecule = state.Builder.Build();
        return state.Result;
    }

    internal void ReadHeader(V3000ReaderState state)
    {
        state.Builder.Molecule.Metadata.AddName(state.Lines[0]);
        state.Builder.Molecule.Metadata.AddComment(state.Lines[1]);
        state.Builder.Molecule.Metadata.AddComment(state.Lines[2]);
    }

    internal void ReadBlocks(V3000ReaderState state)
    {
        int index = 0;
        while (index < state.Lines.Count && state.Result.Success)
        {
            string body = GetV30Body(state.Lines[index]);
            if (body.StartsWith("COUNTS "))
            {
                ReadCounts(state, body, index + 1);
            }
            else if (body == "BEGIN ATOM")
            {
                index = ReadAtomBlock(state, index + 1);
            }
            else if (body == "BEGIN BOND")
            {
                index = ReadBondBlock(state, index + 1);
            }
            else if (body == "BEGIN COLLECTION")
            {
                index = ReadCollectionBlock(state, index + 1);
            }

            index = index + 1;
        }

        if (state.Result.Success && state.SawCounts == false)
        {
            Fail(state, "MOLFILE_V3000_COUNTS_MISSING", "MOL V3000 CTAB is missing a COUNTS line.", 4, -1, -1);
        }
    }

    internal void ReadCounts(V3000ReaderState state, string body, int lineNumber)
    {
        string[] tokens = SplitTokens(body);
        if (tokens.Length < 3)
        {
            Fail(state, "MOLFILE_INVALID_COUNTS", "MOL V3000 COUNTS line is incomplete.", lineNumber, -1, -1);
            return;
        }

        state.AtomCount = ParseIntField(state, tokens[1], lineNumber, "atom count");
        state.BondCount = ParseIntField(state, tokens[2], lineNumber, "bond count");
        state.Result.DeclaredAtomCount = state.AtomCount;
        state.Result.DeclaredBondCount = state.BondCount;
        state.SawCounts = true;
        if (state.AtomCount < 0 || state.BondCount < 0)
        {
            Fail(state, "MOLFILE_INVALID_COUNTS", "MOL V3000 counts must be non-negative.", lineNumber, -1, -1);
            return;
        }

        if (state.AtomCount > state.Options.MaximumAtomCount || state.BondCount > state.Options.MaximumBondCount)
        {
            Fail(state, "MOLFILE_COUNT_LIMIT", "MOL V3000 counts exceed the configured structure limits.", lineNumber, -1, -1);
        }
    }

    internal int ReadAtomBlock(V3000ReaderState state, int index)
    {
        while (index < state.Lines.Count && state.Result.Success)
        {
            string body = GetV30Body(state.Lines[index]);
            if (body == "END ATOM")
            {
                return index;
            }

            ReadAtomLine(state, body, index + 1);
            index = index + 1;
        }

        return index;
    }

    internal void ReadAtomLine(V3000ReaderState state, string body, int lineNumber)
    {
        string[] tokens = SplitTokens(body);
        if (tokens.Length < 6)
        {
            Fail(state, "MOLFILE_V3000_INVALID_ATOM", "MOL V3000 atom line is incomplete.", lineNumber, -1, -1);
            return;
        }

        int sourceAtomId = ParseIntField(state, tokens[0], lineNumber, "atom id");
        string symbol = tokens[1];
        double x = ParseDoubleField(state, tokens[2], lineNumber, "x coordinate");
        double y = ParseDoubleField(state, tokens[3], lineNumber, "y coordinate");
        double z = ParseDoubleField(state, tokens[4], lineNumber, "z coordinate");
        int atomMapNumber = ParseInt(tokens[5]);

        AtomSpec spec = AtomSpec.CreateEmpty();
        spec.Label = symbol;
        ApplyAtomSymbol(state, spec, symbol, lineNumber);
        spec.AtomMapNumber = atomMapNumber;
        ApplyAtomProperties(state, spec, tokens, 6, lineNumber, sourceAtomId);
        string alias = GetPropertyText(body, "LABEL=");
        if (alias != "")
        {
            state.Result.AliasCount = state.Result.AliasCount + 1;
            if (state.Options.ReadAliases)
            {
                spec.Alias = alias;
            }
        }

        int atomId = state.Builder.AddAtom(spec);
        Atom atom = state.Builder.Molecule.Atoms[atomId];
        atom.HasCoordinates = true;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
        state.SourceAtomIds.Add(sourceAtomId);
        state.MoleculeAtomIds.Add(atomId);
    }

    internal void ApplyAtomProperties(V3000ReaderState state, AtomSpec spec, string[] tokens, int startIndex, int lineNumber, int sourceAtomId)
    {
        int index = startIndex;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            if (token.StartsWith("CHG="))
            {
                state.Result.PropertyRecordCount = state.Result.PropertyRecordCount + 1;
                if (state.Options.ReadCommonProperties)
                {
                    spec.FormalCharge = ParseInt(Slice(token, 4, token.Length - 4));
                }
            }
            else if (token.StartsWith("MASS="))
            {
                state.Result.PropertyRecordCount = state.Result.PropertyRecordCount + 1;
                if (state.Options.ReadCommonProperties)
                {
                    spec.IsotopeMassNumber = ParseInt(Slice(token, 5, token.Length - 5));
                }
            }
            else if (token.StartsWith("RAD="))
            {
                state.Result.PropertyRecordCount = state.Result.PropertyRecordCount + 1;
                if (state.Options.ReadCommonProperties)
                {
                    spec.RadicalElectronCount = MapRadicalElectrons(ParseInt(Slice(token, 4, token.Length - 4)));
                }
            }
            else if (token.StartsWith("CFG="))
            {
                V3000AtomStereoHint hint = new V3000AtomStereoHint();
                hint.SourceAtomId = sourceAtomId;
                hint.Cfg = ParseInt(Slice(token, 4, token.Length - 4));
                state.AtomStereoHints.Add(hint);
            }
            else if (token.StartsWith("AAMAP="))
            {
                spec.AtomMapNumber = ParseInt(Slice(token, 6, token.Length - 6));
            }

            index = index + 1;
        }
    }

    internal int ReadBondBlock(V3000ReaderState state, int index)
    {
        while (index < state.Lines.Count && state.Result.Success)
        {
            string body = GetV30Body(state.Lines[index]);
            if (body == "END BOND")
            {
                return index;
            }

            ReadBondLine(state, body, index + 1);
            index = index + 1;
        }

        return index;
    }

    internal void ReadBondLine(V3000ReaderState state, string body, int lineNumber)
    {
        string[] tokens = SplitTokens(body);
        if (tokens.Length < 4)
        {
            Fail(state, "MOLFILE_V3000_INVALID_BOND", "MOL V3000 bond line is incomplete.", lineNumber, -1, -1);
            return;
        }

        int sourceBondId = ParseIntField(state, tokens[0], lineNumber, "bond id");
        int bondType = ParseIntField(state, tokens[1], lineNumber, "bond type");
        int beginAtomId = ResolveAtomId(state, ParseIntField(state, tokens[2], lineNumber, "begin atom"));
        int endAtomId = ResolveAtomId(state, ParseIntField(state, tokens[3], lineNumber, "end atom"));
        int cfg = GetCfg(tokens, 4);

        BondSpec spec = BondSpec.Create(MapBondOrder(bondType), bondType == 4, MapBondStereo(cfg), MapBondDirection(cfg));
        spec.SourceFlags = body;
        if (bondType == 9)
        {
            spec.IsDative = true;
        }

        int bondId = state.Builder.AddBond(beginAtomId, endAtomId, spec);
        if (bondId < 0)
        {
            Fail(state, "MOLFILE_INVALID_BOND_ATOM", "MOL V3000 bond references an invalid atom.", lineNumber, beginAtomId, -1);
            return;
        }

        state.SourceBondIds.Add(sourceBondId);
        state.MoleculeBondIds.Add(bondId);
    }

    internal int ReadCollectionBlock(V3000ReaderState state, int index)
    {
        while (index < state.Lines.Count && state.Result.Success)
        {
            string body = GetV30Body(state.Lines[index]);
            if (body == "END COLLECTION")
            {
                return index;
            }

            ReadCollectionLine(state, body);
            index = index + 1;
        }

        return index;
    }

    internal void ReadCollectionLine(V3000ReaderState state, string body)
    {
        string label = GetFirstToken(body);
        string groupType = GetCollectionGroupType(label);
        if (groupType == "")
        {
            return;
        }

        state.Result.StereoCollectionCount = state.Result.StereoCollectionCount + 1;
        if (!state.Options.ReadEnhancedStereo)
        {
            return;
        }

        V3000CollectionRecord collection = new V3000CollectionRecord();
        collection.Type = groupType;
        collection.Label = label;
        ReadAtomList(body, collection.SourceAtomIds);
        state.Collections.Add(collection);
    }

    internal void ApplyAtomSymbol(V3000ReaderState state, AtomSpec spec, string symbol, int lineNumber)
    {
        string value = NormalizeString(symbol);
        bool negated = false;
        if (value.StartsWith("NOT[") && value.EndsWith("]"))
        {
            negated = true;
            value = value.Substring(3);
        }

        if (value.StartsWith("[") && value.EndsWith("]"))
        {
            state.Result.AtomListCount = state.Result.AtomListCount + 1;
            spec.AtomicNumber = 0;
            spec.Label = "L";
            if (state.Options.ReadAtomLists)
            {
                spec.QueryOperation = QueryOperation.InList;
                if (negated)
                {
                    spec.QueryOperation = QueryOperation.NotInList;
                }

                spec.QueryValue = value.Substring(1, value.Length - 2);
            }

            return;
        }

        spec.AtomicNumber = GetAtomicNumber(state, symbol, lineNumber);
    }

    internal string GetPropertyText(string body, string propertyName)
    {
        int start = body.IndexOf(propertyName);
        if (start < 0)
        {
            return "";
        }

        start = start + propertyName.Length;
        if (start >= body.Length)
        {
            return "";
        }

        if (body[start] == '"')
        {
            int endQuote = body.IndexOf("\"", start + 1);
            if (endQuote < 0)
            {
                return body.Substring(start + 1);
            }

            return body.Substring(start + 1, endQuote - start - 1);
        }

        int end = body.IndexOf(" ", start);
        if (end < 0)
        {
            end = body.Length;
        }

        return body.Substring(start, end - start);
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

    internal void ApplyStereoHintsAndCollections(V3000ReaderState state)
    {
        int index = 0;
        while (index < state.AtomStereoHints.Count)
        {
            V3000AtomStereoHint hint = state.AtomStereoHints[index];
            int atomId = ResolveAtomId(state, hint.SourceAtomId);
            if (atomId >= 0)
            {
                EnsureStereoRecordForAtom(state, atomId, MapChirality(hint.Cfg));
            }

            index = index + 1;
        }

        index = 0;
        while (index < state.Collections.Count)
        {
            ApplyCollection(state, state.Collections[index]);
            index = index + 1;
        }
    }

    internal void ApplyCollection(V3000ReaderState state, V3000CollectionRecord collection)
    {
        EnhancedStereoGroup group = state.Builder.Molecule.StereoGroups.AddGroup(collection.Type, "V3000", collection.Label);
        int index = 0;
        while (index < collection.SourceAtomIds.Count)
        {
            int atomId = ResolveAtomId(state, collection.SourceAtomIds[index]);
            if (atomId >= 0)
            {
                int recordId = EnsureStereoRecordForAtom(state, atomId, Chirality.Unknown);
                state.Builder.Molecule.StereoGroups.AssignRecordToGroup(state.Builder.Molecule, recordId, group.Id);
            }

            index = index + 1;
        }
    }

    internal int EnsureStereoRecordForAtom(V3000ReaderState state, int atomId, Chirality chirality)
    {
        int existingRecordId = FindStereoRecordForAtom(state.Builder.Molecule, atomId);
        if (existingRecordId >= 0)
        {
            if (state.Builder.Molecule.StereoRecords[existingRecordId].Chirality == Chirality.Unknown && chirality != Chirality.Unknown)
            {
                state.Builder.Molecule.StereoRecords[existingRecordId].Chirality = chirality;
            }

            return existingRecordId;
        }

        StereoRecord record = new StereoRecord();
        FillTetrahedralRecord(state.Builder.Molecule, record, atomId, chirality);
        record.Id = state.Builder.Molecule.StereoRecords.Count;
        record.Source = "V3000";
        state.Builder.Molecule.StereoRecords.Add(record);
        return record.Id;
    }

    internal void FillTetrahedralRecord(Molecule molecule, StereoRecord record, int atomId, Chirality chirality)
    {
        List<int> neighbors = molecule.GetNeighborAtomIds(atomId);
        int ligand0 = GetLigandOrImplicit(neighbors, 0);
        int ligand1 = GetLigandOrImplicit(neighbors, 1);
        int ligand2 = GetLigandOrImplicit(neighbors, 2);
        int ligand3 = GetLigandOrImplicit(neighbors, 3);
        record.SetTetrahedral(atomId, ligand0, ligand1, ligand2, ligand3, chirality);
    }

    internal int GetLigandOrImplicit(List<int> neighbors, int index)
    {
        if (index >= 0 && index < neighbors.Count)
        {
            return neighbors[index];
        }

        return -1;
    }

    internal int FindStereoRecordForAtom(Molecule molecule, int atomId)
    {
        int index = 0;
        while (index < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[index];
            if (record.IsTetrahedral() && record.CenterAtomId == atomId)
            {
                return record.Id;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void ReadAtomList(string body, List<int> atomIds)
    {
        int listStart = body.IndexOf("ATOMS=(");
        if (listStart < 0)
        {
            return;
        }

        int valueStart = listStart + 7;
        int valueEnd = body.IndexOf(")", valueStart);
        if (valueEnd < 0)
        {
            valueEnd = body.Length;
        }

        string listText = body.Substring(valueStart, valueEnd - valueStart);
        string[] tokens = SplitTokens(listText);
        if (tokens.Length == 0)
        {
            return;
        }

        int count = ParseInt(tokens[0]);
        int index = 0;
        while (index < count && index + 1 < tokens.Length)
        {
            atomIds.Add(ParseInt(tokens[index + 1]));
            index = index + 1;
        }
    }

    internal int ResolveAtomId(V3000ReaderState state, int sourceAtomId)
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

    internal int GetCfg(string[] tokens, int startIndex)
    {
        int index = startIndex;
        while (index < tokens.Length)
        {
            if (tokens[index].StartsWith("CFG="))
            {
                return ParseInt(Slice(tokens[index], 4, tokens[index].Length - 4));
            }

            index = index + 1;
        }

        return 0;
    }

    internal string GetCollectionGroupType(string label)
    {
        if (label.StartsWith("MDLV30/STEABS"))
        {
            return "abs";
        }

        if (label.StartsWith("MDLV30/STEREL"))
        {
            return "or";
        }

        if (label.StartsWith("MDLV30/STERAC"))
        {
            return "and";
        }

        return "";
    }

    internal string GetFirstToken(string text)
    {
        string[] tokens = SplitTokens(text);
        if (tokens.Length == 0)
        {
            return "";
        }

        return tokens[0];
    }

    internal bool ContainsV3000Marker(V3000ReaderState state)
    {
        int index = 0;
        while (index < state.Lines.Count)
        {
            if (state.Lines[index].Contains("V3000") || state.Lines[index].Contains("M  V30"))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal List<string> JoinContinuationLines(List<string> lines)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < lines.Count)
        {
            string line = lines[index];
            if (line.StartsWith("M  V30 "))
            {
                string body = GetV30Body(line);
                while (body.EndsWith("-") && index + 1 < lines.Count)
                {
                    body = body.Substring(0, body.Length - 1);
                    index = index + 1;
                    body = body + " " + GetV30Body(lines[index]).Trim();
                }

                result.Add("M  V30 " + body);
            }
            else
            {
                result.Add(line);
            }

            index = index + 1;
        }

        return result;
    }

    internal string GetV30Body(string line)
    {
        string value = NormalizeString(line);
        if (value.StartsWith("M  V30 "))
        {
            return value.Substring(7).Trim();
        }

        return value.Trim();
    }

    internal int GetAtomicNumber(V3000ReaderState state, string symbol, int lineNumber)
    {
        if (symbol == "" || symbol == "*" || symbol == "A" || symbol == "Q" || symbol == "L" || symbol == "R#")
        {
            return 0;
        }

        Element element = Elements.GetElementBySymbol(symbol);
        if (element.AtomicNumber == 0)
        {
            AddWarning(state, "MOLFILE_UNKNOWN_ELEMENT", "MOL V3000 atom symbol is not in the element table.", lineNumber, -1, -1);
        }

        return element.AtomicNumber;
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

    internal BondStereo MapBondStereo(int cfg)
    {
        if (cfg == 1)
        {
            return BondStereo.Wedge;
        }

        if (cfg == 2)
        {
            return BondStereo.UpOrDown;
        }

        if (cfg == 3)
        {
            return BondStereo.Hash;
        }

        return BondStereo.None;
    }

    internal BondDirection MapBondDirection(int cfg)
    {
        if (cfg == 1)
        {
            return BondDirection.Up;
        }

        if (cfg == 2)
        {
            return BondDirection.Either;
        }

        if (cfg == 3)
        {
            return BondDirection.Down;
        }

        return BondDirection.None;
    }

    internal Chirality MapChirality(int cfg)
    {
        if (cfg == 1)
        {
            return Chirality.Clockwise;
        }

        if (cfg == 2)
        {
            return Chirality.CounterClockwise;
        }

        if (cfg == 3)
        {
            return Chirality.Unspecified;
        }

        return Chirality.Unknown;
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

    internal int ParseIntField(V3000ReaderState state, string value, int lineNumber, string fieldName)
    {
        int result = 0;
        if (!int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "MOLFILE_INVALID_INTEGER", "MOL V3000 field is not a valid integer: " + fieldName + ".", lineNumber, -1, -1);
        }

        return result;
    }

    internal int ParseInt(string value)
    {
        int result = 0;
        int.TryParse(NormalizeString(value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        return result;
    }

    internal double ParseDoubleField(V3000ReaderState state, string value, int lineNumber, string fieldName)
    {
        double result = 0.0;
        if (!double.TryParse(NormalizeString(value).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            Fail(state, "MOLFILE_INVALID_FLOAT", "MOL V3000 field is not a valid floating point number: " + fieldName + ".", lineNumber, -1, -1);
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

    internal void Fail(V3000ReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Success = false;
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("MOL", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Error));
    }

    internal void AddWarning(V3000ReaderState state, string code, string message, int lineNumber, int atomId, int bondId)
    {
        state.Result.Report.Add(new MoleculeError(code, message, new DiagnosticLocation("MOL", lineNumber, 1, -1, GetContext(state, lineNumber)), atomId, bondId, ErrorSeverity.Warning));
    }

    internal string GetContext(V3000ReaderState state, int lineNumber)
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
