using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Parses the supported SMARTS subset into an executable query graph.
/// </summary>
public class SmartsParser : ISmartsParser
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a parser for the supported SMARTS query subset.
    /// </summary>
    public SmartsParser()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Parses the supported text syntax and returns its object model.
    /// </summary>
    /// <param name="smarts">The smarts.</param>
    /// <returns>The resulting query molecule.</returns>
    public QueryMolecule Parse(string? smarts)
    {
        SmartsParseResult result = ParseWithResult(smarts);
        return result.Query;
    }

    /// <summary>
    /// Parses the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="smarts">The smarts.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public SmartsParseResult ParseWithResult(string? smarts)
    {
        SmartsParserState state = new SmartsParserState();
        state.Text = NormalizeString(smarts);
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
                QueryAtom atom = ReadBracketAtom(state);
                AddAtom(state, atom);
            }
            else
            {
                QueryAtom atom = ReadBareAtom(state);
                if (state.Result.Success)
                {
                    AddAtom(state, atom);
                }
            }
        }

        if (state.Result.Success && state.BranchAtoms.Count > 0)
        {
            Fail(state, "Unclosed branch in SMARTS.", state.Text.Length);
        }

        if (state.Result.Success && state.ComponentGroupStack.Count > 0)
        {
            Fail(state, "Unclosed component group in SMARTS.", state.Text.Length);
        }

        if (state.Result.Success && HasOpenRing(state))
        {
            Fail(state, "Unclosed ring closure in SMARTS.", state.Text.Length);
        }

        state.Result.Query = state.Query;
        return state.Result;
    }

    internal void AddAtom(SmartsParserState state, QueryAtom atom)
    {
        if (!state.Result.Success)
        {
            return;
        }

        int atomId = state.Query.AddAtom(atom);
        state.Query.SetComponentGroupId(atomId, state.CurrentComponentGroupId);
        if (state.CurrentAtomId >= 0)
        {
            QueryBond bond = CreateDefaultBond();
            if (state.HasPendingBond)
            {
                bond = CopyBond(state.PendingBond);
            }

            int bondId = state.Query.AddBond(state.CurrentAtomId, atomId, bond);
            if (bondId < 0)
            {
                Fail(state, "Could not add SMARTS query bond.", state.Index);
                return;
            }
        }

        state.CurrentAtomId = atomId;
        ResetPendingBond(state);
    }

    internal void OpenBranch(SmartsParserState state)
    {
        if (state.CurrentAtomId < 0)
        {
            state.ComponentGroupStack.Add(state.CurrentComponentGroupId);
            state.CurrentComponentGroupId = state.NextComponentGroupId;
            state.NextComponentGroupId = state.NextComponentGroupId + 1;
            state.Index = state.Index + 1;
            return;
        }

        state.BranchAtoms.Add(state.CurrentAtomId);
        state.Index = state.Index + 1;
    }

    internal void CloseBranch(SmartsParserState state)
    {
        if (state.BranchAtoms.Count > 0)
        {
            state.CurrentAtomId = state.BranchAtoms[state.BranchAtoms.Count - 1];
            state.BranchAtoms.RemoveAt(state.BranchAtoms.Count - 1);
            ResetPendingBond(state);
            state.Index = state.Index + 1;
            return;
        }

        if (state.ComponentGroupStack.Count > 0)
        {
            state.CurrentComponentGroupId = state.ComponentGroupStack[state.ComponentGroupStack.Count - 1];
            state.ComponentGroupStack.RemoveAt(state.ComponentGroupStack.Count - 1);
            state.CurrentAtomId = -1;
            ResetPendingBond(state);
            state.Index = state.Index + 1;
            return;
        }

        if (state.BranchAtoms.Count == 0)
        {
            Fail(state, "Branch closed without a matching open branch.", state.Index);
            return;
        }
    }

    internal QueryBond ReadBond(SmartsParserState state)
    {
        QueryBond bond = new QueryBond();
        int startIndex = state.Index;
        bond.Expression = ReadBondLowAndExpression(state, bond);
        bond.RawText = state.Text.Substring(startIndex, state.Index - startIndex);
        return bond;
    }

    internal QueryExpression ReadBondLowAndExpression(SmartsParserState state, QueryBond bond)
    {
        QueryExpression expression = ReadBondOrExpression(state, bond);
        while (state.Result.Success && state.Index < state.Text.Length && state.Text[state.Index] == ';')
        {
            state.Index = state.Index + 1;
            expression = CombineExpressions(QueryOperation.And, expression, ReadBondOrExpression(state, bond));
        }

        return expression;
    }

    internal QueryExpression ReadBondOrExpression(SmartsParserState state, QueryBond bond)
    {
        QueryExpression expression = ReadBondHighAndExpression(state, bond);
        while (state.Result.Success && state.Index < state.Text.Length && state.Text[state.Index] == ',')
        {
            state.Index = state.Index + 1;
            expression = CombineExpressions(QueryOperation.Or, expression, ReadBondHighAndExpression(state, bond));
        }

        return expression;
    }

    internal QueryExpression ReadBondHighAndExpression(SmartsParserState state, QueryBond bond)
    {
        QueryExpression expression = ReadBondUnaryExpression(state, bond);
        bool reading = true;
        while (state.Result.Success && reading && state.Index < state.Text.Length)
        {
            if (state.Text[state.Index] == '&')
            {
                state.Index = state.Index + 1;
                expression = CombineExpressions(QueryOperation.And, expression, ReadBondUnaryExpression(state, bond));
            }
            else if (IsBondPrimitiveStart(state.Text[state.Index]))
            {
                expression = CombineExpressions(QueryOperation.And, expression, ReadBondUnaryExpression(state, bond));
            }
            else
            {
                reading = false;
            }
        }

        return expression;
    }

    internal QueryExpression ReadBondUnaryExpression(SmartsParserState state, QueryBond bond)
    {
        if (state.Index < state.Text.Length && state.Text[state.Index] == '!')
        {
            int startIndex = state.Index;
            state.Index = state.Index + 1;
            QueryExpression expression = new QueryExpression();
            expression.Operation = QueryOperation.Not;
            expression.Children.Add(ReadBondUnaryExpression(state, bond));
            expression.RawText = state.Text.Substring(startIndex, state.Index - startIndex);
            return expression;
        }

        return ReadBondLeafExpression(state, bond);
    }

    internal QueryExpression ReadBondLeafExpression(SmartsParserState state, QueryBond bond)
    {
        if (state.Index >= state.Text.Length || !IsBondPrimitiveStart(state.Text[state.Index]) || state.Text[state.Index] == '!')
        {
            Fail(state, "Expected SMARTS bond primitive.", state.Index);
            return new QueryExpression();
        }

        int startIndex = state.Index;
        char character = state.Text[state.Index];
        QueryPrimitive primitive = QueryPrimitive.None;
        int value = 0;
        if (character == '-')
        {
            bond.SetOrder(BondOrder.Single);
            primitive = QueryPrimitive.BondOrder;
            value = (int)BondOrder.Single;
        }
        else if (character == '=')
        {
            bond.SetOrder(BondOrder.Double);
            primitive = QueryPrimitive.BondOrder;
            value = (int)BondOrder.Double;
        }
        else if (character == '#')
        {
            bond.SetOrder(BondOrder.Triple);
            primitive = QueryPrimitive.BondOrder;
            value = (int)BondOrder.Triple;
        }
        else if (character == ':')
        {
            bond.SetOrder(BondOrder.Aromatic);
            bond.SetAromaticity(true);
            primitive = QueryPrimitive.BondAromatic;
            value = 1;
        }
        else if (character == '~')
        {
            bond.SetAny();
            primitive = QueryPrimitive.Any;
        }
        else if (character == '/')
        {
            bond.SetOrder(BondOrder.Single);
            bond.SetDirection(BondDirection.Up);
            primitive = QueryPrimitive.BondDirection;
            value = (int)BondDirection.Up;
        }
        else if (character == '\\')
        {
            bond.SetOrder(BondOrder.Single);
            bond.SetDirection(BondDirection.Down);
            primitive = QueryPrimitive.BondDirection;
            value = (int)BondDirection.Down;
        }
        else if (character == '@')
        {
            bond.SetRingBond();
            primitive = QueryPrimitive.BondRing;
            value = 1;
        }

        state.Index = state.Index + 1;
        if (character == '/' || character == '\\')
        {
            QueryExpression single = CreateLeafExpression(QueryPrimitive.BondOrder, (int)BondOrder.Single, state.Text.Substring(startIndex, 1));
            QueryExpression direction = CreateLeafExpression(primitive, value, state.Text.Substring(startIndex, 1));
            return CombineExpressions(QueryOperation.And, single, direction);
        }

        return CreateLeafExpression(primitive, value, state.Text.Substring(startIndex, 1));
    }

    internal void ProcessRingClosure(SmartsParserState state)
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

        SmartsRingState ring = GetRingState(state, ringNumber);
        if (!ring.IsOpen)
        {
            ring.RingNumber = ringNumber;
            ring.AtomId = state.CurrentAtomId;
            ring.Bond = CopyBond(state.PendingBond);
            ring.HasBond = state.HasPendingBond;
            ring.IsOpen = true;
            state.RingStates.Add(ring);
        }
        else
        {
            QueryBond bond = SelectRingBond(state, ring);
            int bondId = state.Query.AddBond(ring.AtomId, state.CurrentAtomId, bond);
            if (bondId < 0)
            {
                Fail(state, "Ring closure references an invalid query atom.", state.Index);
                return;
            }

            ring.IsOpen = false;
        }

        ResetPendingBond(state);
    }

    internal QueryBond SelectRingBond(SmartsParserState state, SmartsRingState ring)
    {
        if (state.HasPendingBond)
        {
            return CopyBond(state.PendingBond);
        }

        if (ring.HasBond)
        {
            return CopyBond(ring.Bond);
        }

        return CreateDefaultBond();
    }

    internal SmartsRingState GetRingState(SmartsParserState state, int ringNumber)
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

        return new SmartsRingState();
    }

    internal bool HasOpenRing(SmartsParserState state)
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

    internal int ReadRingNumber(SmartsParserState state)
    {
        if (state.Text[state.Index] == '%')
        {
            state.Index = state.Index + 1;
            return ReadNumber(state);
        }

        int value = state.Text[state.Index] - '0';
        state.Index = state.Index + 1;
        return value;
    }

    internal QueryAtom ReadBareAtom(SmartsParserState state)
    {
        QueryAtom atom = new QueryAtom();
        char character = state.Text[state.Index];
        if (character == '*')
        {
            atom.SetAny();
            atom.RawText = "*";
            atom.Expression = CreateLeafExpression(QueryPrimitive.Any, 0, "*");
            state.Index = state.Index + 1;
            return atom;
        }

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

        atom.SetAtomicNumber(GetAtomicNumber(symbol));
        atom.Expression = CreateLeafExpression(QueryPrimitive.AtomicNumber, atom.AtomicNumber, symbol);
        if (aromatic)
        {
            atom.SetAromaticity(true);
            atom.Expression = CombineExpressions(QueryOperation.And, atom.Expression, CreateLeafExpression(QueryPrimitive.Aromatic, 1, symbol));
        }
        else
        {
            atom.SetAromaticity(false);
            atom.Expression = CombineExpressions(QueryOperation.And, atom.Expression, CreateLeafExpression(QueryPrimitive.Aliphatic, 0, symbol));
        }

        atom.RawText = symbol;
        if (atom.AtomicNumber == 0)
        {
            Fail(state, "Unsupported SMARTS atom primitive.", state.Index - 1);
        }

        return atom;
    }

    internal QueryAtom ReadBracketAtom(SmartsParserState state)
    {
        QueryAtom atom = new QueryAtom();
        int startIndex = state.Index;
        state.Index = state.Index + 1;
        atom.MatchesAny = false;
        atom.Expression = ReadAtomLowAndExpression(state, atom);

        if (state.Index >= state.Text.Length || state.Text[state.Index] != ']')
        {
            Fail(state, "Unclosed SMARTS bracket atom.", state.Index);
            return atom;
        }

        state.Index = state.Index + 1;
        atom.RawText = state.Text.Substring(startIndex, state.Index - startIndex);
        return atom;
    }

    internal QueryExpression ReadAtomLowAndExpression(SmartsParserState state, QueryAtom atom)
    {
        QueryExpression expression = ReadAtomOrExpression(state, atom);
        while (state.Result.Success && state.Index < state.Text.Length && state.Text[state.Index] == ';')
        {
            state.Index = state.Index + 1;
            QueryExpression right = ReadAtomOrExpression(state, atom);
            expression = CombineExpressions(QueryOperation.And, expression, right);
        }

        return expression;
    }

    internal QueryExpression ReadAtomOrExpression(SmartsParserState state, QueryAtom atom)
    {
        QueryExpression expression = ReadAtomHighAndExpression(state, atom);
        while (state.Result.Success && state.Index < state.Text.Length && state.Text[state.Index] == ',')
        {
            state.Index = state.Index + 1;
            QueryExpression right = ReadAtomHighAndExpression(state, atom);
            expression = CombineExpressions(QueryOperation.Or, expression, right);
        }

        return expression;
    }

    internal QueryExpression ReadAtomHighAndExpression(SmartsParserState state, QueryAtom atom)
    {
        QueryExpression expression = ReadAtomUnaryExpression(state, atom);
        bool reading = true;
        while (state.Result.Success && reading && state.Index < state.Text.Length)
        {
            if (state.Text[state.Index] == '&')
            {
                state.Index = state.Index + 1;
                QueryExpression right = ReadAtomUnaryExpression(state, atom);
                expression = CombineExpressions(QueryOperation.And, expression, right);
            }
            else if (IsAtomImplicitAndStart(state.Text[state.Index]))
            {
                QueryExpression right = ReadAtomUnaryExpression(state, atom);
                expression = CombineExpressions(QueryOperation.And, expression, right);
            }
            else
            {
                reading = false;
            }
        }

        return expression;
    }

    internal QueryExpression ReadAtomUnaryExpression(SmartsParserState state, QueryAtom atom)
    {
        if (state.Index < state.Text.Length && state.Text[state.Index] == '!')
        {
            int startIndex = state.Index;
            state.Index = state.Index + 1;
            QueryExpression child = ReadAtomUnaryExpression(state, atom);
            QueryExpression expression = new QueryExpression();
            expression.Operation = QueryOperation.Not;
            expression.RawText = state.Text.Substring(startIndex, state.Index - startIndex);
            expression.Children.Add(child);
            if (child.Primitive == QueryPrimitive.AtomicNumber)
            {
                atom.NegateAtomicNumber = true;
            }

            return expression;
        }

        return ReadAtomPrimaryExpression(state, atom);
    }

    internal QueryExpression ReadAtomPrimaryExpression(SmartsParserState state, QueryAtom atom)
    {
        if (state.Index >= state.Text.Length)
        {
            Fail(state, "Expected SMARTS atom expression.", state.Index);
            return new QueryExpression();
        }

        if (state.Text[state.Index] == '(')
        {
            state.Index = state.Index + 1;
            QueryExpression expression = ReadAtomLowAndExpression(state, atom);
            if (state.Index >= state.Text.Length || state.Text[state.Index] != ')')
            {
                Fail(state, "Unclosed logical group in SMARTS atom expression.", state.Index);
                return expression;
            }

            state.Index = state.Index + 1;
            return expression;
        }

        if (state.Text[state.Index] == '$')
        {
            return ReadRecursiveExpression(state);
        }

        return ReadAtomLeafExpression(state, atom);
    }

    internal QueryExpression ReadRecursiveExpression(SmartsParserState state)
    {
        int startIndex = state.Index;
        if (state.Index + 1 >= state.Text.Length || state.Text[state.Index + 1] != '(')
        {
            Fail(state, "Expected recursive SMARTS parentheses.", state.Index);
            return new QueryExpression();
        }

        int contentStart = state.Index + 2;
        int scan = contentStart;
        int depth = 1;
        while (scan < state.Text.Length && depth > 0)
        {
            char character = state.Text[scan];
            if (character == '(')
            {
                depth = depth + 1;
            }
            else if (character == ')')
            {
                depth = depth - 1;
            }

            scan = scan + 1;
        }

        if (depth != 0)
        {
            Fail(state, "Unclosed recursive SMARTS expression.", state.Index);
            return new QueryExpression();
        }

        string recursiveText = state.Text.Substring(contentStart, scan - contentStart - 1);
        SmartsParseResult recursiveResult = new SmartsParser().ParseWithResult(recursiveText);
        if (!recursiveResult.Success || recursiveResult.Query.Atoms.Count == 0)
        {
            Fail(state, "Invalid recursive SMARTS expression.", state.Index);
            return new QueryExpression();
        }

        state.Index = scan;
        QueryExpression expression = CreateLeafExpression(QueryPrimitive.Recursive, 0, state.Text.Substring(startIndex, scan - startIndex));
        expression.Operation = QueryOperation.Recursive;
        expression.RecursiveQuery = recursiveResult.Query;
        return expression;
    }

    internal QueryExpression ReadAtomLeafExpression(SmartsParserState state, QueryAtom atom)
    {
        if (state.Index >= state.Text.Length || !IsAtomImplicitAndStart(state.Text[state.Index]))
        {
            Fail(state, "Expected SMARTS atom primitive.", state.Index);
            return new QueryExpression();
        }

        int startIndex = state.Index;
        char character = state.Text[state.Index];
        QueryPrimitive primitive = QueryPrimitive.None;
        int value = 0;
        if (IsDigit(character))
        {
            value = ReadNumber(state);
            primitive = QueryPrimitive.Isotope;
            atom.SetIsotopeMassNumber(value);
        }
        else if (character == '#')
        {
            state.Index = state.Index + 1;
            value = ReadNumber(state);
            primitive = QueryPrimitive.AtomicNumber;
            ApplyAtomicNumber(atom, value, false);
        }
        else if (character == '*')
        {
            state.Index = state.Index + 1;
            primitive = QueryPrimitive.Any;
            atom.SetAny();
        }
        else if (character == '+' || character == '-')
        {
            value = ReadCharge(state);
            primitive = QueryPrimitive.FormalCharge;
            atom.SetFormalCharge(value);
        }
        else if (character == 'D')
        {
            state.Index = state.Index + 1;
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.Degree;
            atom.SetDegree(value);
        }
        else if (character == 'X')
        {
            state.Index = state.Index + 1;
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.Connectivity;
        }
        else if (character == 'v')
        {
            state.Index = state.Index + 1;
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.Valence;
            atom.SetValence(value);
        }
        else if (character == 'R')
        {
            state.Index = state.Index + 1;
            bool hasNumber = state.Index < state.Text.Length && IsDigit(state.Text[state.Index]);
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.RingMembership;
            if (hasNumber && value > 0)
            {
                primitive = QueryPrimitive.RingCount;
            }

            atom.SetRingCount(value);
        }
        else if (character == 'r')
        {
            state.Index = state.Index + 1;
            bool hasNumber = state.Index < state.Text.Length && IsDigit(state.Text[state.Index]);
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.RingMembership;
            if (hasNumber)
            {
                primitive = QueryPrimitive.RingSize;
            }
        }
        else if (character == 'x')
        {
            state.Index = state.Index + 1;
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.RingConnectivity;
        }
        else if (character == 'h')
        {
            state.Index = state.Index + 1;
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.ImplicitHydrogenCount;
        }
        else if (character == 'H' && (atom.HasAtomicNumber || atom.UsesAtomicNumberList))
        {
            state.Index = state.Index + 1;
            value = ReadOptionalNumber(state, 1);
            primitive = QueryPrimitive.HydrogenCount;
            atom.SetHydrogenCount(value);
        }
        else if (character == 'a')
        {
            state.Index = state.Index + 1;
            value = 1;
            primitive = QueryPrimitive.Aromatic;
            atom.SetAromaticity(true);
        }
        else if (character == 'A' && !NextCharactersFormKnownElement(state))
        {
            state.Index = state.Index + 1;
            value = 0;
            primitive = QueryPrimitive.Aliphatic;
            atom.SetAromaticity(false);
        }
        else
        {
            string symbol = ReadBracketAtomSymbol(state);
            value = GetAtomicNumber(symbol);
            if (value == 0)
            {
                Fail(state, "Unsupported SMARTS bracket primitive.", startIndex);
                return new QueryExpression();
            }

            primitive = QueryPrimitive.AtomicNumber;
            ApplyAtomicNumber(atom, value, false);
            if (IsLowercaseSymbol(symbol))
            {
                QueryExpression atomicExpression = CreateLeafExpression(primitive, value, symbol);
                QueryExpression aromaticExpression = CreateLeafExpression(QueryPrimitive.Aromatic, 1, symbol);
                atom.SetAromaticity(true);
                return CombineExpressions(QueryOperation.And, atomicExpression, aromaticExpression);
            }

            QueryExpression elementExpression = CreateLeafExpression(primitive, value, symbol);
            QueryExpression aliphaticExpression = CreateLeafExpression(QueryPrimitive.Aliphatic, 0, symbol);
            atom.SetAromaticity(false);
            return CombineExpressions(QueryOperation.And, elementExpression, aliphaticExpression);
        }

        return CreateLeafExpression(primitive, value, state.Text.Substring(startIndex, state.Index - startIndex));
    }

    internal QueryExpression CreateLeafExpression(QueryPrimitive primitive, int value, string rawText)
    {
        QueryExpression expression = new QueryExpression();
        expression.Operation = QueryOperation.Equals;
        expression.Primitive = primitive;
        expression.Value = value;
        expression.RawText = NormalizeString(rawText);
        return expression;
    }

    internal QueryExpression CombineExpressions(QueryOperation operation, QueryExpression left, QueryExpression right)
    {
        QueryExpression expression = new QueryExpression();
        expression.Operation = operation;
        expression.Children.Add(left);
        expression.Children.Add(right);
        expression.RawText = left.RawText + right.RawText;
        return expression;
    }

    internal QueryBond CreateDefaultBond()
    {
        QueryBond bond = new QueryBond();
        QueryExpression single = CreateLeafExpression(QueryPrimitive.BondOrder, (int)BondOrder.Single, "");
        QueryExpression aromatic = CreateLeafExpression(QueryPrimitive.BondAromatic, 1, "");
        bond.Expression = CombineExpressions(QueryOperation.Or, single, aromatic);
        return bond;
    }

    internal bool IsAtomImplicitAndStart(char character)
    {
        if (character == ']' || character == ')' || character == ';' || character == ',' || character == '&')
        {
            return false;
        }

        return true;
    }

    internal bool NextCharactersFormKnownElement(SmartsParserState state)
    {
        if (state.Index + 1 >= state.Text.Length)
        {
            return false;
        }

        char second = state.Text[state.Index + 1];
        if (second < 'a' || second > 'z')
        {
            return false;
        }

        return Elements.IsKnownSymbol(state.Text.Substring(state.Index, 2));
    }

    internal void ReadBracketPrimitive(SmartsParserState state, QueryAtom atom, bool negate)
    {
        if (state.Index >= state.Text.Length)
        {
            Fail(state, "Unclosed SMARTS bracket atom.", state.Index);
            return;
        }

        char character = state.Text[state.Index];
        if (IsDigit(character))
        {
            atom.SetIsotopeMassNumber(ReadNumber(state));
        }
        else if (character == '#')
        {
            state.Index = state.Index + 1;
            ApplyAtomicNumber(atom, ReadNumber(state), negate);
        }
        else if (character == '*')
        {
            atom.SetAny();
            state.Index = state.Index + 1;
        }
        else if (character == 'H')
        {
            ReadHydrogenCount(state, atom);
        }
        else if (character == '+' || character == '-')
        {
            atom.SetFormalCharge(ReadCharge(state));
        }
        else if (character == 'D')
        {
            state.Index = state.Index + 1;
            atom.SetDegree(ReadOptionalNumber(state, 1));
        }
        else if (character == 'R')
        {
            state.Index = state.Index + 1;
            atom.SetRingCount(ReadOptionalNumber(state, 1));
        }
        else if (character == 'v')
        {
            state.Index = state.Index + 1;
            atom.SetValence(ReadOptionalNumber(state, 1));
        }
        else if (character == 'a')
        {
            state.Index = state.Index + 1;
            atom.SetAromaticity(!negate);
        }
        else if (character == 'A')
        {
            state.Index = state.Index + 1;
            atom.SetAromaticity(negate);
        }
        else
        {
            string symbol = ReadBracketAtomSymbol(state);
            if (symbol == "*")
            {
                atom.SetAny();
            }
            else
            {
                int atomicNumber = GetAtomicNumber(symbol);
                if (atomicNumber == 0)
                {
                    Fail(state, "Unsupported SMARTS bracket primitive.", state.Index);
                    return;
                }

                ApplyAtomicNumber(atom, atomicNumber, negate);
                if (IsLowercaseSymbol(symbol))
                {
                    atom.SetAromaticity(!negate);
                }
            }
        }
    }

    internal void ApplyAtomicNumber(QueryAtom atom, int atomicNumber, bool negate)
    {
        if (atom.HasAtomicNumber || atom.UsesAtomicNumberList)
        {
            if (atom.HasAtomicNumber)
            {
                int existingAtomicNumber = atom.AtomicNumber;
                atom.HasAtomicNumber = false;
                atom.AddAtomicNumber(existingAtomicNumber);
            }

            atom.AddAtomicNumber(atomicNumber);
        }
        else
        {
            atom.SetAtomicNumber(atomicNumber);
        }

        if (negate)
        {
            atom.NegateAtomicNumber = true;
        }
    }

    internal string ReadBracketAtomSymbol(SmartsParserState state)
    {
        if (state.Text[state.Index] == '*')
        {
            state.Index = state.Index + 1;
            return "*";
        }

        if (state.Index + 1 < state.Text.Length)
        {
            string two = state.Text.Substring(state.Index, 2);
            if (Elements.IsKnownSymbol(two))
            {
                state.Index = state.Index + 2;
                return two;
            }
        }

        string one = state.Text.Substring(state.Index, 1);
        state.Index = state.Index + 1;
        return one;
    }

    internal void ReadHydrogenCount(SmartsParserState state, QueryAtom atom)
    {
        state.Index = state.Index + 1;
        if (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
        {
            atom.SetHydrogenCount(ReadNumber(state));
        }
        else
        {
            atom.SetHydrogenCount(1);
        }
    }

    internal int ReadCharge(SmartsParserState state)
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

    internal int ReadNumber(SmartsParserState state)
    {
        if (state.Index >= state.Text.Length || !IsDigit(state.Text[state.Index]))
        {
            Fail(state, "Expected number in SMARTS.", state.Index);
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

    internal int ReadOptionalNumber(SmartsParserState state, int defaultValue)
    {
        if (state.Index < state.Text.Length && IsDigit(state.Text[state.Index]))
        {
            return ReadNumber(state);
        }

        return defaultValue;
    }

    internal QueryBond CopyBond(QueryBond? source)
    {
        QueryBond bond = new QueryBond();
        if (source == null)
        {
            return bond;
        }

        bond.RawText = source.RawText;
        bond.Expression = source.Expression;
        bond.MatchesAny = source.MatchesAny;
        bond.HasOrder = source.HasOrder;
        bond.Order = source.Order;
        bond.HasAromaticity = source.HasAromaticity;
        bond.IsAromatic = source.IsAromatic;
        bond.HasDirection = source.HasDirection;
        bond.Direction = source.Direction;
        bond.HasStereo = source.HasStereo;
        bond.Stereo = source.Stereo;
        bond.HasDative = source.HasDative;
        bond.IsDative = source.IsDative;
        bond.IsRingBond = source.IsRingBond;
        return bond;
    }

    internal void ResetPendingBond(SmartsParserState state)
    {
        state.PendingBond = new QueryBond();
        state.HasPendingBond = false;
    }

    internal bool IsBondCharacter(char character)
    {
        return IsBondPrimitiveStart(character);
    }

    internal bool IsBondPrimitiveStart(char character)
    {
        return character == '-' || character == '=' || character == '#' || character == ':' || character == '~' || character == '/' || character == '\\' || character == '@' || character == '!';
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
        if (symbol == "H")
        {
            return 1;
        }

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

        return Elements.GetElementBySymbol(symbol).AtomicNumber;
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal void Fail(SmartsParserState state, string message, int index)
    {
        state.Result.Success = false;
        state.Result.ErrorMessage = NormalizeString(message);
        state.Result.ErrorIndex = index;
        MoleculeError error = new MoleculeError("SMARTS_PARSE_ERROR", state.Result.ErrorMessage, new DiagnosticLocation("SMARTS", 1, index + 1, index, state.Text), -1, -1, ErrorSeverity.Error);
        state.Result.Report.Add(error);
    }
}
