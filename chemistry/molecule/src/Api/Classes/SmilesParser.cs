using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Parses the supported SMILES subset into a molecular graph with diagnostics.
/// </summary>
public class SmilesParser : IMoleculeReader, ISmilesParser
{
    /// <summary>
    /// Initializes a parser for the supported SMILES subset.
    /// </summary>
    public SmilesParser()
    {
    }

    /// <summary>
    /// Reads one SMILES molecular graph from text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The resulting molecule.</returns>
    public Molecule Read(string? text)
    {
        return Parse(text);
    }

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule Parse(string? smiles)
    {
        SmilesParseResult result = ParseWithResult(smiles);
        return result.Molecule;
    }

    /// <summary>
    /// Parses the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SmilesParseResult ParseWithResult(string? smiles)
    {
        SmilesParserState state = new SmilesParserState();
        state.Text = NormalizeString(smiles);
        state.Result.Success = true;

        while (state.Index < state.Text.Length && state.Result.Success)
        {
            char character = state.Text[state.Index];
            if (character == '(')
            {
                OpenBranch(state);
            }
            else if (character == ')')
            {
                CloseBranch(state);
            }
            else if (character == '.')
            {
                state.CurrentAtomId = -1;
                ResetPendingBond(state);
                state.Index = state.Index + 1;
            }
            else if (IsBondCharacter(character))
            {
                state.PendingBond = ReadBond(state);
                state.HasPendingBond = true;
            }
            else if (IsRingStart(character))
            {
                ProcessRingClosure(state);
            }
            else if (character == '[')
            {
                AtomSpec spec = ReadBracketAtom(state);
                AddAtom(state, spec);
            }
            else
            {
                AtomSpec spec = ReadBareAtom(state);
                if (state.Result.Success)
                {
                    AddAtom(state, spec);
                }
            }
        }

        if (state.Result.Success && state.BranchAtoms.Count > 0)
        {
            Fail(state, "Unclosed branch in SMILES.", state.Text.Length);
        }

        if (state.Result.Success && HasOpenRing(state))
        {
            Fail(state, "Unclosed ring closure in SMILES.", state.Text.Length);
        }

        state.Result.Molecule = state.Builder.Build();
        return state.Result;
    }

    /// <summary>
    /// Parses the with recovery.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SmilesParseResult ParseWithRecovery(string? smiles)
    {
        SmilesRecoveryText recovery = RecoverText(smiles);
        SmilesParseResult result = ParseWithResult(recovery.Text);
        MergeReports(result.Report, recovery.Report);
        result.Recovered = recovery.RecoveredIssueCount > 0;
        result.RecoveredIssueCount = recovery.RecoveredIssueCount;
        if (result.Recovered && result.Success && result.Report.GetErrorCount() == 0)
        {
            result.ErrorMessage = "";
            result.ErrorIndex = -1;
        }

        return result;
    }

    /// <summary>
    /// Parses SMILES from a non-owning in-memory character span.
    /// </summary>
    /// <param name="smiles">The SMILES characters to parse.</param>
    /// <returns>The resulting molecule.</returns>
    public Molecule ParseSmilesSpan(System.ReadOnlySpan<char> smiles)
    {
        return ParseSmilesSpanWithResult(smiles).Molecule;
    }

    /// <summary>
    /// Parses SMILES from a character span and returns detailed diagnostics.
    /// </summary>
    /// <param name="smiles">The SMILES characters to parse.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SmilesParseResult ParseSmilesSpanWithResult(System.ReadOnlySpan<char> smiles)
    {
        return ParseWithResult(smiles.ToString());
    }

    /// <summary>
    /// Parses owned SMILES text using strict or recovery behavior selected by options.
    /// </summary>
    /// <param name="smiles">The SMILES text to parse.</param>
    /// <param name="options">The SMILES parsing options; <c>null</c> uses strict parsing.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SmilesParseResult ParseSmilesWithOptions(string? smiles, SmilesReadOptions? options)
    {
        SmilesReadOptions safeOptions = NormalizeReadOptions(options);
        if (safeOptions.EnableRecovery)
        {
            return ParseWithRecovery(smiles);
        }

        return ParseWithResult(smiles);
    }

    /// <summary>
    /// Parses SMILES from a character span using strict or recovery behavior selected by options.
    /// </summary>
    /// <param name="smiles">The SMILES characters to parse.</param>
    /// <param name="options">The SMILES parsing options; <c>null</c> uses strict parsing.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SmilesParseResult ParseSmilesSpanWithOptions(System.ReadOnlySpan<char> smiles, SmilesReadOptions? options)
    {
        return ParseSmilesWithOptions(smiles.ToString(), options);
    }

    internal SmilesReadOptions NormalizeReadOptions(SmilesReadOptions? options)
    {
        SmilesReadOptions safeOptions = new SmilesReadOptions();
        if (options != null)
        {
            safeOptions.EnableRecovery = options.EnableRecovery;
        }

        return safeOptions;
    }

    internal SmilesRecoveryText RecoverText(string? smiles)
    {
        SmilesRecoveryText recovery = new SmilesRecoveryText();
        string text = NormalizeString(smiles);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        List<int> branchPositions = new List<int>();
        List<SmilesRecoveryMarker> ringMarkers = new List<SmilesRecoveryMarker>();
        List<SmilesRecoveryRemoval> removals = new List<SmilesRecoveryRemoval>();
        bool bracketOpen = false;

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (!bracketOpen && index + 1 < text.Length && text.Substring(index, 2) == "Cl")
            {
                builder.Append("Cl");
                index = index + 2;
            }
            else if (!bracketOpen && index + 1 < text.Length && text.Substring(index, 2) == "Br")
            {
                builder.Append("Br");
                index = index + 2;
            }
            else if (character == '[')
            {
                bracketOpen = true;
                builder.Append(character);
                index = index + 1;
            }
            else if (character == ']')
            {
                bracketOpen = false;
                builder.Append(character);
                index = index + 1;
            }
            else if (!bracketOpen && character == '(')
            {
                branchPositions.Add(builder.Length);
                builder.Append(character);
                index = index + 1;
            }
            else if (!bracketOpen && character == ')')
            {
                if (branchPositions.Count == 0)
                {
                    AddRecoveryWarning(recovery, "SMILES_UNMATCHED_BRANCH_CLOSE", "Skipped unmatched branch close.", index, text);
                }
                else
                {
                    branchPositions.RemoveAt(branchPositions.Count - 1);
                    builder.Append(character);
                }

                index = index + 1;
            }
            else if (!bracketOpen && IsRingStart(character))
            {
                index = RecoverRingToken(text, index, builder, ringMarkers, recovery);
            }
            else if (!bracketOpen && IsClearlyUnsupportedCharacter(text, index))
            {
                AddRecoveryWarning(recovery, "SMILES_UNSUPPORTED_CHARACTER", "Skipped unsupported character.", index, text);
                index = index + 1;
            }
            else
            {
                builder.Append(character);
                index = index + 1;
            }
        }

        if (bracketOpen)
        {
            builder.Append("]");
            AddRecoveryWarning(recovery, "SMILES_UNCLOSED_BRACKET", "Inserted missing bracket atom close.", text.Length, text);
        }

        AddUnmatchedBranchRemovals(branchPositions, removals, recovery, text);
        AddUnmatchedRingRemovals(ringMarkers, removals, recovery, text);
        SortRemovalsDescending(removals);
        ApplyRemovals(builder, removals);
        RemoveTrailingBond(builder, recovery, text);
        recovery.Text = builder.ToString();
        return recovery;
    }

    internal int RecoverRingToken(string text, int index, System.Text.StringBuilder builder, List<SmilesRecoveryMarker> ringMarkers, SmilesRecoveryText recovery)
    {
        int ringNumber = 0;
        int length = 1;
        string token = text.Substring(index, 1);
        if (text[index] == '%')
        {
            if (index + 1 >= text.Length || !IsDigit(text[index + 1]))
            {
                AddRecoveryWarning(recovery, "SMILES_BAD_PERCENT_RING", "Skipped malformed percent ring closure.", index, text);
                return index + 1;
            }

            int scan = index + 1;
            while (scan < text.Length && IsDigit(text[scan]))
            {
                int digit = text[scan] - '0';
                ringNumber = (int)(((long)ringNumber * 10 + digit) % int.MaxValue);
                scan = scan + 1;
            }

            length = scan - index;
            token = text.Substring(index, length);
        }
        else
        {
            ringNumber = text[index] - '0';
        }

        int position = builder.Length;
        builder.Append(token);
        int markerIndex = FindRingMarker(ringMarkers, ringNumber);
        if (markerIndex >= 0)
        {
            ringMarkers.RemoveAt(markerIndex);
        }
        else
        {
            SmilesRecoveryMarker marker = new SmilesRecoveryMarker();
            marker.Number = ringNumber;
            marker.Position = position;
            marker.Length = length;
            ringMarkers.Add(marker);
        }

        return index + length;
    }

    internal int FindRingMarker(List<SmilesRecoveryMarker> markers, int number)
    {
        int index = 0;
        while (index < markers.Count)
        {
            if (markers[index].Number == number)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void AddUnmatchedBranchRemovals(List<int> branchPositions, List<SmilesRecoveryRemoval> removals, SmilesRecoveryText recovery, string source)
    {
        int index = 0;
        while (index < branchPositions.Count)
        {
            SmilesRecoveryRemoval removal = new SmilesRecoveryRemoval();
            removal.Position = branchPositions[index];
            removal.Length = 1;
            removals.Add(removal);
            AddRecoveryWarning(recovery, "SMILES_UNCLOSED_BRANCH", "Removed unmatched branch open.", source.Length, source);
            index = index + 1;
        }
    }

    internal void AddUnmatchedRingRemovals(List<SmilesRecoveryMarker> ringMarkers, List<SmilesRecoveryRemoval> removals, SmilesRecoveryText recovery, string source)
    {
        int index = 0;
        while (index < ringMarkers.Count)
        {
            SmilesRecoveryRemoval removal = new SmilesRecoveryRemoval();
            removal.Position = ringMarkers[index].Position;
            removal.Length = ringMarkers[index].Length;
            removals.Add(removal);
            AddRecoveryWarning(recovery, "SMILES_UNCLOSED_RING", "Removed unmatched ring closure.", source.Length, source);
            index = index + 1;
        }
    }

    internal void SortRemovalsDescending(List<SmilesRecoveryRemoval> removals)
    {
        int index = 1;
        while (index < removals.Count)
        {
            SmilesRecoveryRemoval value = removals[index];
            int scan = index - 1;
            while (scan >= 0 && removals[scan].Position < value.Position)
            {
                removals[scan + 1] = removals[scan];
                scan = scan - 1;
            }

            removals[scan + 1] = value;
            index = index + 1;
        }
    }

    internal void ApplyRemovals(System.Text.StringBuilder builder, List<SmilesRecoveryRemoval> removals)
    {
        int index = 0;
        while (index < removals.Count)
        {
            if (removals[index].Position >= 0 && removals[index].Position + removals[index].Length <= builder.Length)
            {
                builder.Remove(removals[index].Position, removals[index].Length);
            }

            index = index + 1;
        }
    }

    internal void RemoveTrailingBond(System.Text.StringBuilder builder, SmilesRecoveryText recovery, string source)
    {
        while (builder.Length > 0 && IsBondCharacter(builder[builder.Length - 1]))
        {
            builder.Remove(builder.Length - 1, 1);
            AddRecoveryWarning(recovery, "SMILES_TRAILING_BOND", "Removed trailing bond token.", source.Length, source);
        }
    }

    internal bool IsClearlyUnsupportedCharacter(string text, int index)
    {
        char character = text[index];
        if (character == ' ' || character == '\t' || character == '\r' || character == '\n')
        {
            return true;
        }

        if (character >= 'A' && character <= 'Z')
        {
            if (character == 'B' || character == 'C' || character == 'N' || character == 'O' || character == 'P' || character == 'S' || character == 'F' || character == 'I')
            {
                return false;
            }

            return true;
        }

        if (character >= 'a' && character <= 'z')
        {
            if (IsLowercaseAtomStart(character))
            {
                return false;
            }

            return true;
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

    internal void AddAtom(SmilesParserState state, AtomSpec spec)
    {
        if (!state.Result.Success)
        {
            return;
        }

        int atomId = state.Builder.AddAtom(spec);
        if (state.CurrentAtomId >= 0)
        {
            BondSpec bond = CopyBondSpec(state.PendingBond);
            int bondId = state.Builder.AddBond(state.CurrentAtomId, atomId, bond);
            if (bondId < 0)
            {
                Fail(state, "Could not add bond between parsed atoms.", state.Index);
                return;
            }
        }

        state.CurrentAtomId = atomId;
        ResetPendingBond(state);
    }

    internal void OpenBranch(SmilesParserState state)
    {
        if (state.CurrentAtomId < 0)
        {
            Fail(state, "Branch opened before an atom.", state.Index);
            return;
        }

        state.BranchAtoms.Add(state.CurrentAtomId);
        state.Index = state.Index + 1;
    }

    internal void CloseBranch(SmilesParserState state)
    {
        if (state.BranchAtoms.Count == 0)
        {
            Fail(state, "Branch closed without a matching open branch.", state.Index);
            return;
        }

        state.CurrentAtomId = state.BranchAtoms[state.BranchAtoms.Count - 1];
        state.BranchAtoms.RemoveAt(state.BranchAtoms.Count - 1);
        ResetPendingBond(state);
        state.Index = state.Index + 1;
    }

    internal BondSpec ReadBond(SmilesParserState state)
    {
        BondSpec spec = new BondSpec();
        char character = state.Text[state.Index];
        if (character == '-')
        {
            spec.Order = BondOrder.Single;
        }
        else if (character == '=')
        {
            spec.Order = BondOrder.Double;
        }
        else if (character == '#')
        {
            spec.Order = BondOrder.Triple;
        }
        else if (character == ':')
        {
            spec.Order = BondOrder.Aromatic;
            spec.IsAromatic = true;
        }
        else if (character == '~')
        {
            spec.Order = BondOrder.Unknown;
        }
        else if (character == '/')
        {
            spec.Order = BondOrder.Single;
            spec.Direction = BondDirection.Up;
            spec.Stereo = BondStereo.Wedge;
        }
        else if (character == '\\')
        {
            spec.Order = BondOrder.Single;
            spec.Direction = BondDirection.Down;
            spec.Stereo = BondStereo.Hash;
        }

        state.Index = state.Index + 1;
        return spec;
    }

    internal void ProcessRingClosure(SmilesParserState state)
    {
        if (state.CurrentAtomId < 0)
        {
            Fail(state, "Ring closure appeared before an atom.", state.Index);
            return;
        }

        int ringNumber = ReadRingNumber(state);
        if (!state.Result.Success)
        {
            return;
        }

        SmilesRingState ring = GetRingState(state, ringNumber);
        if (!ring.IsOpen)
        {
            ring.RingNumber = ringNumber;
            ring.AtomId = state.CurrentAtomId;
            ring.BondSpec = CopyBondSpec(state.PendingBond);
            ring.HasBond = state.HasPendingBond;
            ring.IsOpen = true;
            state.RingStates.Add(ring);
        }
        else
        {
            BondSpec bond = SelectRingBond(state, ring);
            int bondId = state.Builder.AddBond(ring.AtomId, state.CurrentAtomId, bond);
            if (bondId < 0)
            {
                Fail(state, "Ring closure references an invalid atom.", state.Index);
                return;
            }

            ring.IsOpen = false;
        }

        ResetPendingBond(state);
    }

    internal BondSpec SelectRingBond(SmilesParserState state, SmilesRingState ring)
    {
        if (state.HasPendingBond)
        {
            return CopyBondSpec(state.PendingBond);
        }

        if (ring.HasBond)
        {
            return CopyBondSpec(ring.BondSpec);
        }

        return new BondSpec();
    }

    internal SmilesRingState GetRingState(SmilesParserState state, int ringNumber)
    {
        int index = 0;
        while (index < state.RingStates.Count)
        {
            if (state.RingStates[index].RingNumber == ringNumber && state.RingStates[index].IsOpen)
            {
                return state.RingStates[index];
            }

            index = index + 1;
        }

        return new SmilesRingState();
    }

    internal bool HasOpenRing(SmilesParserState state)
    {
        int index = 0;
        while (index < state.RingStates.Count)
        {
            if (state.RingStates[index].IsOpen)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int ReadRingNumber(SmilesParserState state)
    {
        if (state.Text[state.Index] == '%')
        {
            state.Index = state.Index + 1;
            if (state.Index >= state.Text.Length || !IsDigit(state.Text[state.Index]))
            {
                Fail(state, "Percent ring closure requires digits.", state.Index);
                return 0;
            }

            return ReadNumber(state);
        }

        int value = state.Text[state.Index] - '0';
        state.Index = state.Index + 1;
        return value;
    }

    internal AtomSpec ReadBareAtom(SmilesParserState state)
    {
        AtomSpec spec = AtomSpec.CreateEmpty();
        char character = state.Text[state.Index];
        bool aromatic = IsLowercaseAtomStart(character);
        string symbol = "";

        if (state.Index + 1 < state.Text.Length)
        {
            string two = state.Text.Substring(state.Index, 2);
            if (two == "Cl" || two == "Br")
            {
                symbol = two;
                state.Index = state.Index + 2;
            }
        }

        if (symbol == "")
        {
            symbol = state.Text.Substring(state.Index, 1);
            state.Index = state.Index + 1;
        }

        spec.AtomicNumber = GetAtomicNumber(symbol);
        spec.IsAromatic = aromatic;
        if (spec.AtomicNumber == 0)
        {
            Fail(state, "Unsupported bare atom symbol.", state.Index - 1);
        }

        return spec;
    }

    internal AtomSpec ReadBracketAtom(SmilesParserState state)
    {
        AtomSpec spec = AtomSpec.CreateEmpty();
        state.Index = state.Index + 1;

        if (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
        {
            spec.IsotopeMassNumber = ReadNumber(state);
        }

        if (state.Index >= state.Text.Length)
        {
            Fail(state, "Unclosed bracket atom.", state.Index);
            return spec;
        }

        string symbol = ReadBracketAtomSymbol(state);
        spec.AtomicNumber = GetAtomicNumber(symbol);
        spec.IsAromatic = IsLowercaseSymbol(symbol);
        if (spec.AtomicNumber == 0 && symbol != "*")
        {
            Fail(state, "Unsupported bracket atom symbol.", state.Index);
            return spec;
        }

        while (state.Index < state.Text.Length && state.Text[state.Index] != ']')
        {
            char character = state.Text[state.Index];
            if (character == '@')
            {
                SkipChirality(state);
            }
            else if (character == 'H')
            {
                ReadHydrogenCount(state, spec);
            }
            else if (character == '+' || character == '-')
            {
                spec.FormalCharge = ReadCharge(state);
            }
            else if (character == ':')
            {
                state.Index = state.Index + 1;
                spec.AtomMapNumber = ReadNumber(state);
            }
            else
            {
                Fail(state, "Unsupported bracket atom token.", state.Index);
                return spec;
            }
        }

        if (state.Index >= state.Text.Length || state.Text[state.Index] != ']')
        {
            Fail(state, "Unclosed bracket atom.", state.Index);
            return spec;
        }

        state.Index = state.Index + 1;
        return spec;
    }

    internal string ReadBracketAtomSymbol(SmilesParserState state)
    {
        if (state.Text[state.Index] == '*')
        {
            state.Index = state.Index + 1;
            return "*";
        }

        if (state.Index + 1 < state.Text.Length)
        {
            string two = state.Text.Substring(state.Index, 2);
            if (two == "Cl" || two == "Br")
            {
                state.Index = state.Index + 2;
                return two;
            }
        }

        string one = state.Text.Substring(state.Index, 1);
        state.Index = state.Index + 1;
        return one;
    }

    internal void ReadHydrogenCount(SmilesParserState state, AtomSpec spec)
    {
        state.Index = state.Index + 1;
        if (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
        {
            spec.ExplicitHydrogenCount = ReadNumber(state);
        }
        else
        {
            spec.ExplicitHydrogenCount = 1;
        }
    }

    internal int ReadCharge(SmilesParserState state)
    {
        char sign = state.Text[state.Index];
        state.Index = state.Index + 1;
        int magnitude = 1;
        if (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
        {
            magnitude = ReadNumber(state);
        }
        else
        {
            magnitude = 1;
            while (state.Index < state.Text.Length && state.Text[state.Index] == sign)
            {
                magnitude = magnitude + 1;
                state.Index = state.Index + 1;
            }
        }

        if (sign == '-')
        {
            return 0 - magnitude;
        }

        return magnitude;
    }

    internal int ReadNumber(SmilesParserState state)
    {
        if (state.Index >= state.Text.Length || !IsDigit(state.Text[state.Index]))
        {
            Fail(state, "Expected number in SMILES.", state.Index);
            return 0;
        }

        int value = 0;
        while (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
        {
            int digit = state.Text[state.Index] - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                int errorIndex = state.Index;
                while (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
                {
                    state.Index = state.Index + 1;
                }

                Fail(state, "Numeric value is out of range.", errorIndex);
                return 0;
            }

            value = value * 10 + digit;
            state.Index = state.Index + 1;
        }

        return value;
    }

    internal void SkipChirality(SmilesParserState state)
    {
        state.Index = state.Index + 1;
        if (state.Index < state.Text.Length && state.Text[state.Index] == '@')
        {
            state.Index = state.Index + 1;
        }
    }

    internal void ResetPendingBond(SmilesParserState state)
    {
        state.PendingBond = new BondSpec();
        state.HasPendingBond = false;
    }

    internal BondSpec CopyBondSpec(BondSpec? source)
    {
        BondSpec copy = new BondSpec();
        if (source == null)
        {
            return copy;
        }

        copy.Order = source.Order;
        copy.IsAromatic = source.IsAromatic;
        copy.Stereo = source.Stereo;
        copy.Direction = source.Direction;
        copy.IsDative = source.IsDative;
        copy.QueryOperation = source.QueryOperation;
        copy.QueryValue = source.QueryValue;
        copy.SourceFlags = source.SourceFlags;
        return copy;
    }

    internal bool IsBondCharacter(char character)
    {
        return character == '-' || character == '=' || character == '#' || character == ':' || character == '~' || character == '/' || character == '\\';
    }

    internal bool IsRingStart(char character)
    {
        return IsDigit(character) || character == '%';
    }

    internal bool IsDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal bool IsLowercaseAtomStart(char character)
    {
        return character == 'b' || character == 'c' || character == 'n' || character == 'o' || character == 'p' || character == 's';
    }

    internal bool IsLowercaseSymbol(string symbol)
    {
        return symbol == "b" || symbol == "c" || symbol == "n" || symbol == "o" || symbol == "p" || symbol == "s";
    }

    internal int GetAtomicNumber(string symbol)
    {
        if (symbol == "B" || symbol == "b")
        {
            return 5;
        }

        if (symbol == "C" || symbol == "c")
        {
            return 6;
        }

        if (symbol == "N" || symbol == "n")
        {
            return 7;
        }

        if (symbol == "O" || symbol == "o")
        {
            return 8;
        }

        if (symbol == "F")
        {
            return 9;
        }

        if (symbol == "P" || symbol == "p")
        {
            return 15;
        }

        if (symbol == "S" || symbol == "s")
        {
            return 16;
        }

        if (symbol == "Cl")
        {
            return 17;
        }

        if (symbol == "Br")
        {
            return 35;
        }

        if (symbol == "I")
        {
            return 53;
        }

        return 0;
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal void Fail(SmilesParserState state, string message, int index)
    {
        state.Result.Success = false;
        state.Result.ErrorMessage = NormalizeString(message);
        state.Result.ErrorIndex = index;
        AddError(state.Result, "SMILES_PARSE_ERROR", message, index, state.Text);
    }

    internal void AddError(SmilesParseResult result, string code, string message, int index, string source)
    {
        MoleculeError error = new MoleculeError(code, NormalizeString(message), CreateLocation(index, source), -1, -1, ErrorSeverity.Error);
        result.Report.Add(error);
    }

    internal void AddRecoveryWarning(SmilesRecoveryText recovery, string code, string message, int index, string source)
    {
        MoleculeError warning = new MoleculeError(code, NormalizeString(message), CreateLocation(index, source), -1, -1, ErrorSeverity.Warning);
        recovery.Report.Add(warning);
        recovery.RecoveredIssueCount = recovery.RecoveredIssueCount + 1;
    }

    internal DiagnosticLocation CreateLocation(int index, string source)
    {
        int safeIndex = index;
        if (safeIndex < 0)
        {
            safeIndex = 0;
        }

        string context = NormalizeString(source);
        if (safeIndex > context.Length)
        {
            safeIndex = context.Length;
        }

        return new DiagnosticLocation("SMILES", 1, safeIndex + 1, safeIndex, context);
    }
}
