namespace asn1;

internal class AssignmentHeadParser
{
    internal DiagnosticBag Diagnostics;

    internal AssignmentHeadParser(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal AssignmentHeadParseResult ParseHeads(AsnToken[] bodyTokens)
    {
        if (bodyTokens == null)
        {
            throw new System.ArgumentNullException("bodyTokens");
        }

        int[] depths = this.GetTokenDepths(bodyTokens);
        System.Collections.Generic.List<int> operators = new System.Collections.Generic.List<int>();
        bool retiredMacro = false;
        bool macroPending = false;
        int macroDepth = 0;
        int index = 0;
        while (index < bodyTokens.Length)
        {
            AsnToken token = bodyTokens[index];
            if (macroDepth == 0 && token.Text == "MACRO")
            {
                if (!retiredMacro)
                {
                    this.AddDiagnostic(
                        221,
                        "X.208 MACRO definitions are retired and are not supported.",
                        token);
                }

                retiredMacro = true;
                macroPending = true;
            }
            else if (macroPending && token.Text == "BEGIN")
            {
                macroPending = false;
                macroDepth = 1;
            }
            else if (macroDepth > 0 && token.Text == "BEGIN")
            {
                macroDepth = macroDepth + 1;
            }
            else if (macroDepth > 0 && token.Text == "END")
            {
                macroDepth = macroDepth - 1;
            }

            if (macroDepth == 0
                && depths[index] == 0
                && token.Kind == AsnTokenKind.Assignment)
            {
                operators.Add(index);
            }

            index = index + 1;
        }

        if (bodyTokens.Length > 0 && operators.Count == 0)
        {
            this.AddDiagnostic(12, "The module body contains no assignment operator.", bodyTokens[0]);
            return new AssignmentHeadParseResult
            {
                Recoveries = [this.CreateAssignmentRecovery(bodyTokens)]
            };
        }

        System.Collections.Generic.List<AssignmentHeadSyntax> heads =
            new System.Collections.Generic.List<AssignmentHeadSyntax>();
        System.Collections.Generic.List<ParsedRecoverySyntax> recoveries =
            new System.Collections.Generic.List<ParsedRecoverySyntax>();
        bool success = !retiredMacro;
        index = 0;
        while (index < operators.Count)
        {
            int operatorIndex = operators[index];
            int lowerBound = this.FindHeadLowerBound(bodyTokens, depths, operatorIndex, index == 0
                ? -1
                : operators[index - 1]);
            AssignmentHeadSyntax head = this.CreateHead(bodyTokens, depths, lowerBound, operatorIndex);
            if (head.Alternatives.Length == 0)
            {
                this.AddDiagnostic(
                    13,
                    "No valid assignment reference precedes the assignment operator.",
                    bodyTokens[operatorIndex]);
                success = false;
                recoveries.Add(this.CreateAssignmentRecovery(
                    this.CopyTokens(bodyTokens, lowerBound, operatorIndex + 1)));
            }

            heads.Add(head);
            index = index + 1;
        }

        return new AssignmentHeadParseResult
        {
            Success = success,
            Heads = heads.ToArray(),
            Recoveries = recoveries.ToArray()
        };
    }

    internal ParsedRecoverySyntax CreateAssignmentRecovery(AsnToken[] tokens)
    {
        return new ParsedRecoverySyntax
        {
            Boundary = "assignment",
            SkippedTokens = tokens,
            Span = tokens.Length == 0
                ? null
                : this.CreateSpan(tokens[0].Span, tokens[tokens.Length - 1].Span)
        };
    }

    internal int[] GetTokenDepths(AsnToken[] tokens)
    {
        int[] depths = new int[tokens.Length];
        int depth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            if (this.IsClosingDelimiter(tokens[index].Kind) && depth > 0)
            {
                depth = depth - this.GetDelimiterWidth(tokens[index].Kind);
                if (depth < 0)
                {
                    depth = 0;
                }
            }

            depths[index] = depth;
            if (this.IsOpeningDelimiter(tokens[index].Kind))
            {
                depth = depth + this.GetDelimiterWidth(tokens[index].Kind);
            }

            index = index + 1;
        }

        return depths;
    }

    internal int FindHeadLowerBound(
        AsnToken[] tokens,
        int[] depths,
        int operatorIndex,
        int previousOperatorIndex)
    {
        int lowerBound = previousOperatorIndex + 1;
        int index = operatorIndex - 1;
        while (index >= lowerBound)
        {
            if (depths[index] == 0 && tokens[index].Kind == AsnTokenKind.Semicolon)
            {
                return index + 1;
            }

            index = index - 1;
        }

        return lowerBound;
    }

    internal AssignmentHeadSyntax CreateHead(
        AsnToken[] tokens,
        int[] depths,
        int lowerBound,
        int operatorIndex)
    {
        System.Collections.Generic.List<AssignmentHeadAlternativeSyntax> alternatives =
            new System.Collections.Generic.List<AssignmentHeadAlternativeSyntax>();
        int index = operatorIndex - 1;
        while (index >= lowerBound)
        {
            if (depths[index] == 0
                && this.IsAssignmentReference(tokens[index])
                && this.IsAssignmentHeadBoundary(
                    tokens,
                    lowerBound,
                    index,
                    operatorIndex))
            {
                AssignmentHeadAlternativeSyntax? alternative = index == operatorIndex - 1
                    ? this.CreateShortAlternative(tokens[index], tokens[operatorIndex])
                    : this.CreateGovernedAlternative(tokens, index, operatorIndex);
                if (alternative != null)
                {
                    alternatives.Add(alternative);
                }
            }

            index = index - 1;
        }

        AsnSourceSpan? span = alternatives.Count == 0
            ? tokens[operatorIndex].Span
            : this.CreateSpan(alternatives[alternatives.Count - 1].Span, tokens[operatorIndex].Span);
        bool ambiguous = alternatives.Count > 1;
        index = 0;
        while (index < alternatives.Count)
        {
            if (alternatives[index].CandidateKinds.Length > 1)
            {
                ambiguous = true;
            }

            index = index + 1;
        }

        return new AssignmentHeadSyntax
        {
            Alternatives = alternatives.ToArray(),
            DeclaredReference = alternatives.Count == 0
                ? null
                : alternatives[alternatives.Count - 1],
            AssignmentToken = tokens[operatorIndex],
            Ambiguous = ambiguous,
            Span = span
        };
    }

    internal AssignmentHeadAlternativeSyntax? CreateShortAlternative(
        AsnToken reference,
        AsnToken assignment)
    {
        AsnAssignmentKind[] kinds = this.GetShortKinds(reference);
        if (kinds.Length == 0)
        {
            return null;
        }

        return new AssignmentHeadAlternativeSyntax
        {
            Name = reference.Text,
            CandidateKinds = kinds,
            Span = this.CreateSpan(reference.Span, assignment.Span)
        };
    }

    internal AssignmentHeadAlternativeSyntax? CreateGovernedAlternative(
        AsnToken[] tokens,
        int referenceIndex,
        int operatorIndex)
    {
        AsnToken[] governor = this.CopyTokens(tokens, referenceIndex + 1, operatorIndex);
        if (governor.Length == 0 || this.ContainsTopLevelSemicolon(governor))
        {
            return null;
        }

        AsnAssignmentKind[] kinds = this.GetGovernedKinds(tokens[referenceIndex], governor[0]);
        if (kinds.Length == 0)
        {
            return null;
        }

        TypeParseResult parsedGovernor = new TypeParser(governor, this.Diagnostics, false).ParseTypeSyntax();
        ObjectClassParseResult parsedObjectClass = new ObjectClassParser(
            governor,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        bool completeType = parsedGovernor.Success
            && parsedGovernor.ConsumedTokenCount == governor.Length;
        bool completeObjectClass = parsedObjectClass.Success
            && parsedObjectClass.ConsumedTokenCount == governor.Length;
        bool parameterized = governor[0].Kind == AsnTokenKind.LeftBrace;
        if (!completeType && !completeObjectClass && !parameterized)
        {
            return null;
        }

        return new AssignmentHeadAlternativeSyntax
        {
            Name = tokens[referenceIndex].Text,
            CandidateKinds = kinds,
            GovernorTokens = governor,
            GovernorType = completeType
                ? parsedGovernor.Syntax
                : null,
            Span = this.CreateSpan(tokens[referenceIndex].Span, tokens[operatorIndex].Span)
        };
    }

    internal bool IsAssignmentHeadBoundary(
        AsnToken[] tokens,
        int lowerBound,
        int referenceIndex,
        int operatorIndex)
    {
        bool explicitBoundary = lowerBound == 0
            || tokens[lowerBound - 1].Kind == AsnTokenKind.Semicolon;
        if (explicitBoundary)
        {
            return referenceIndex == lowerBound;
        }

        if (referenceIndex <= lowerBound)
        {
            return false;
        }

        if (this.StartsAssignmentLine(tokens, referenceIndex, operatorIndex))
        {
            return true;
        }

        return this.IsCompleteAssignmentRightSide(
            this.CopyTokens(tokens, lowerBound, referenceIndex));
    }

    internal bool StartsAssignmentLine(
        AsnToken[] tokens,
        int referenceIndex,
        int operatorIndex)
    {
        AsnSourceSpan? current = tokens[referenceIndex].Span;
        AsnSourceSpan? previous = tokens[referenceIndex - 1].Span;
        AsnSourceSpan? assignment = tokens[operatorIndex].Span;
        return current != null
            && previous != null
            && assignment != null
            && current.Start.Line > previous.End.Line
            && current.Start.Line == assignment.Start.Line;
    }

    internal bool IsCompleteAssignmentRightSide(AsnToken[] tokens)
    {
        TypeParseResult type = new TypeParser(
            tokens,
            this.Diagnostics,
            false).ParseTypeSyntax();
        if (type.Success && type.ConsumedTokenCount == tokens.Length)
        {
            return true;
        }

        ValueParseResult value = new ValueParser(
            tokens,
            this.Diagnostics,
            false).ParseValueSyntax(null);
        if (value.Success && value.ConsumedTokenCount == tokens.Length)
        {
            return true;
        }

        ObjectClassParseResult objectClass = new ObjectClassParser(
            tokens,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        if (objectClass.Success
            && objectClass.ConsumedTokenCount == tokens.Length)
        {
            return true;
        }

        if (this.IsCompleteRetiredMacroRightSide(tokens))
        {
            return true;
        }

        return this.IsCompleteDelimitedRightSide(tokens);
    }

    internal bool IsCompleteRetiredMacroRightSide(AsnToken[] tokens)
    {
        if (tokens.Length < 2
            || tokens[0].Text != "BEGIN"
            || tokens[tokens.Length - 1].Text != "END")
        {
            return false;
        }

        int depth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == "BEGIN")
            {
                depth = depth + 1;
            }
            else if (tokens[index].Text == "END")
            {
                depth = depth - 1;
                if (depth == 0 && index != tokens.Length - 1)
                {
                    return false;
                }

                if (depth < 0)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return depth == 0;
    }

    internal bool IsCompleteDelimitedRightSide(AsnToken[] tokens)
    {
        if (tokens.Length < 2 || !this.IsOpeningDelimiter(tokens[0].Kind))
        {
            return false;
        }

        int depth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            if (this.IsOpeningDelimiter(tokens[index].Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingDelimiter(tokens[index].Kind))
            {
                depth = depth - 1;
                if (depth == 0 && index != tokens.Length - 1)
                {
                    return false;
                }

                if (depth < 0)
                {
                    return false;
                }
            }

            index = index + 1;
        }

        return depth == 0;
    }

    internal AsnAssignmentKind[] GetShortKinds(AsnToken reference)
    {
        if (reference.Kind == AsnTokenKind.ObjectClassReference)
        {
            return [AsnAssignmentKind.Type, AsnAssignmentKind.ObjectClass];
        }

        if (reference.Kind == AsnTokenKind.TypeReference
            || reference.Kind == AsnTokenKind.ModuleReference)
        {
            return [AsnAssignmentKind.Type];
        }

        return [];
    }

    internal AsnAssignmentKind[] GetGovernedKinds(AsnToken reference, AsnToken governorStart)
    {
        bool lowerReference = reference.Kind == AsnTokenKind.Identifier
            || reference.Kind == AsnTokenKind.ValueReference
            || reference.Kind == AsnTokenKind.ObjectReference;
        bool upperReference = reference.Kind == AsnTokenKind.TypeReference
            || reference.Kind == AsnTokenKind.ObjectClassReference
            || reference.Kind == AsnTokenKind.ObjectSetReference;
        if (!lowerReference && !upperReference)
        {
            return [];
        }

        if (this.IsUsefulObjectClass(governorStart))
        {
            return lowerReference
                ? [AsnAssignmentKind.Object]
                : [AsnAssignmentKind.ObjectSet];
        }

        if (this.IsCertainTypeStart(governorStart))
        {
            return lowerReference
                ? [AsnAssignmentKind.Value]
                : [AsnAssignmentKind.ValueSet];
        }

        return lowerReference
            ? [AsnAssignmentKind.Value, AsnAssignmentKind.Object]
            : [AsnAssignmentKind.ValueSet, AsnAssignmentKind.ObjectSet];
    }

    internal bool IsAssignmentReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.ObjectSetReference;
    }

    internal bool IsUsefulObjectClass(AsnToken token)
    {
        return token.Text == "TYPE-IDENTIFIER" || token.Text == "ABSTRACT-SYNTAX";
    }

    internal bool IsCertainTypeStart(AsnToken token)
    {
        if (token.Kind != AsnTokenKind.ReservedWord)
        {
            return token.Kind == AsnTokenKind.LeftBracket;
        }

        return !this.IsUsefulObjectClass(token)
            && token.Text != "CLASS"
            && token.Text != "WITH";
    }

    internal bool ContainsTopLevelSemicolon(AsnToken[] tokens)
    {
        int[] depths = this.GetTokenDepths(tokens);
        int index = 0;
        while (index < tokens.Length)
        {
            if (depths[index] == 0 && tokens[index].Kind == AsnTokenKind.Semicolon)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnToken[] CopyTokens(AsnToken[] tokens, int start, int end)
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

    internal bool IsOpeningDelimiter(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftBrace
            || kind == AsnTokenKind.LeftParenthesis
            || kind == AsnTokenKind.LeftBracket
            || kind == AsnTokenKind.LeftVersionBracket;
    }

    internal bool IsClosingDelimiter(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.RightBrace
            || kind == AsnTokenKind.RightParenthesis
            || kind == AsnTokenKind.RightBracket
            || kind == AsnTokenKind.RightVersionBracket;
    }

    internal int GetDelimiterWidth(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftVersionBracket
                || kind == AsnTokenKind.RightVersionBracket
            ? 2
            : 1;
    }

    internal AsnSourceSpan? CreateSpan(AsnSourceSpan? first, AsnSourceSpan? last)
    {
        if (first == null || last == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Start,
            End = last.End
        };
    }

    internal void AddDiagnostic(int number, string message, AsnToken token)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            token.Span,
            null);
    }
}
