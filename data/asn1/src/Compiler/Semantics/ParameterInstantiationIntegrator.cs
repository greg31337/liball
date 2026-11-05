namespace asn1;

internal class ParameterInstantiationIntegrator
{
    internal bool Integrate(SymbolTable[] tables)
    {
        bool changed = false;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].Symbols[symbolIndex];
                AsnToken[] integrated = this.IntegrateSymbol(symbol);
                if (!this.AreEquivalent(
                    symbol.GetSemanticDeclarationTokens(),
                    integrated))
                {
                    symbol.SemanticDeclarationTokens = integrated;
                    changed = true;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return changed;
    }

    internal AsnToken[] IntegrateSymbol(SymbolTableEntry symbol)
    {
        System.Collections.Generic.List<AsnToken> result =
            new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index < symbol.DeclarationTokens.Length)
        {
            ParsedParameterizedReferenceSyntax? reference =
                this.FindReferenceAt(symbol, index);
            ParameterInstantiationResult? instantiation = reference == null
                ? null
                : reference.Instantiation;
            int end = reference == null
                ? -1
                : this.FindReferenceEnd(symbol.DeclarationTokens, reference);
            if (reference == null
                || instantiation == null
                || !reference.Validated
                || !instantiation.Success
                || end < index)
            {
                result.Add(symbol.DeclarationTokens[index]);
                index = index + 1;
            }
            else
            {
                AsnToken[] replacement = this.GetReplacementTokens(
                    symbol,
                    reference,
                    instantiation,
                    index,
                    end);
                result.AddRange(replacement);
                index = end + 1;
            }
        }

        return result.ToArray();
    }

    internal ParsedParameterizedReferenceSyntax? FindReferenceAt(
        SymbolTableEntry symbol,
        int tokenIndex)
    {
        int index = 0;
        while (index < symbol.ParameterizedReferences.Length)
        {
            ParsedParameterizedReferenceSyntax reference =
                symbol.ParameterizedReferences[index];
            if (reference.SourceTokens.Length > 0
                && reference.SourceTokens[0] == symbol.DeclarationTokens[tokenIndex])
            {
                return reference;
            }

            index = index + 1;
        }

        return null;
    }

    internal int FindReferenceEnd(
        AsnToken[] declarationTokens,
        ParsedParameterizedReferenceSyntax reference)
    {
        if (reference.SourceTokens.Length == 0)
        {
            return -1;
        }

        AsnToken last = reference.SourceTokens[reference.SourceTokens.Length - 1];
        int index = 0;
        while (index < declarationTokens.Length)
        {
            if (declarationTokens[index] == last)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnToken[] GetReplacementTokens(
        SymbolTableEntry symbol,
        ParsedParameterizedReferenceSyntax reference,
        ParameterInstantiationResult instantiation,
        int start,
        int end)
    {
        ParsedParameterizedAssignmentSyntax? assignment =
            instantiation.AssignmentSyntax;
        if (symbol.Kind == AsnAssignmentKind.ValueSet
            && reference.Kind == AsnParameterKind.ValueSet
            && this.IsEntireRightSide(symbol.DeclarationTokens, start, end)
            && assignment != null)
        {
            return assignment.ValueSetTokens;
        }

        if (reference.Kind == AsnParameterKind.ObjectSet
            && !this.IsEntireRightSide(symbol.DeclarationTokens, start, end)
            && instantiation.BodyTokens.Length >= 2
            && instantiation.BodyTokens[0].Kind == AsnTokenKind.LeftBrace
            && instantiation.BodyTokens[instantiation.BodyTokens.Length - 1].Kind
                == AsnTokenKind.RightBrace)
        {
            return this.CopyRange(
                instantiation.BodyTokens,
                1,
                instantiation.BodyTokens.Length - 1);
        }

        return instantiation.BodyTokens;
    }

    internal AsnToken[] CopyRange(AsnToken[] tokens, int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = tokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal bool IsEntireRightSide(AsnToken[] tokens, int start, int end)
    {
        int assignment = 0;
        while (assignment < tokens.Length
            && tokens[assignment].Kind != AsnTokenKind.Assignment)
        {
            assignment = assignment + 1;
        }

        int last = tokens.Length - 1;
        while (last > assignment
            && tokens[last].Kind == AsnTokenKind.Semicolon)
        {
            last = last - 1;
        }

        return start == assignment + 1 && end == last;
    }

    internal bool AreEquivalent(AsnToken[] left, AsnToken[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index].Kind != right[index].Kind
                || left[index].Text != right[index].Text)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
