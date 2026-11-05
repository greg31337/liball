namespace asn1;

internal class AssignmentClassifier
{
    internal DiagnosticBag Diagnostics;
    internal AssignmentDependencyEdge[] ReferenceEdges = [];

    internal AssignmentClassifier(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal AssignmentClassificationResult ClassifyAssignments(
        SymbolTable[] tables,
        CompilationGraph graph)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (graph == null)
        {
            throw new System.ArgumentNullException("graph");
        }

        SymbolTableEntry[] assignments = this.CopyAssignments(tables);
        int pass = 0;
        bool changed = true;
        while (changed && pass <= assignments.Length)
        {
            changed = false;
            int index = 0;
            while (index < assignments.Length)
            {
                if (!assignments[index].KindClassified
                    && this.TryClassifyEntry(assignments[index], graph))
                {
                    changed = true;
                }

                index = index + 1;
            }

            pass = pass + 1;
        }

        bool success = true;
        int assignmentIndex = 0;
        while (assignmentIndex < assignments.Length)
        {
            if (!assignments[assignmentIndex].KindClassified)
            {
                success = false;
                this.Diagnostics.TryAddDiagnostic(
                    AsnDiagnosticSeverity.Error,
                    AsnDiagnosticCategory.Binding,
                    11,
                    "The assignment category for '" + assignments[assignmentIndex].QualifiedName
                        + "' remains ambiguous after governor and context analysis.",
                    assignments[assignmentIndex].Span,
                    null);
            }

            assignmentIndex = assignmentIndex + 1;
        }

        this.ReindexTables(tables);
        this.UpdateImportedBindingKinds(tables);
        if (!new FormalParameterBinder(this.Diagnostics).BindParameters(tables))
        {
            success = false;
        }

        ClassifiedReference[] references = this.ClassifyReferences(graph.AssignmentEdges);
        int referenceIndex = 0;
        while (referenceIndex < references.Length)
        {
            if (!references[referenceIndex].Success)
            {
                success = false;
            }

            referenceIndex = referenceIndex + 1;
        }

        return new AssignmentClassificationResult
        {
            Assignments = assignments,
            References = references,
            Success = success
        };
    }

    internal bool TryClassifyEntry(SymbolTableEntry entry, CompilationGraph graph)
    {
        if (entry.CandidateKinds.Length == 1)
        {
            return this.SetKind(entry, entry.CandidateKinds[0]);
        }

        AsnToken[] governor = this.GetEffectiveGovernorTokens(entry);
        if (governor.Length == 0)
        {
            AsnToken? rightStart = this.GetRightSideStart(entry);
            if (rightStart != null && rightStart.Text == "CLASS")
            {
                return this.SetKind(entry, AsnAssignmentKind.ObjectClass);
            }

            if (rightStart != null
                && this.IsUsefulObjectClass(rightStart)
                && this.ContainsKind(
                    entry.CandidateKinds,
                    AsnAssignmentKind.ObjectClass))
            {
                return this.SetKind(entry, AsnAssignmentKind.ObjectClass);
            }

            SymbolTableEntry? alias = this.FindClassifiedDependency(entry, rightStart, graph);
            if (alias != null
                && (alias.Kind == AsnAssignmentKind.Type
                    || alias.Kind == AsnAssignmentKind.ObjectClass)
                && this.ContainsKind(entry.CandidateKinds, alias.Kind))
            {
                return this.SetKind(entry, alias.Kind);
            }

            if (entry.Parameterized)
            {
                return this.SetKind(entry, AsnAssignmentKind.Type);
            }

            if (rightStart != null
                && (this.IsUnambiguousTypeStart(rightStart)
                    || this.IsSelectionTypeStart(entry, rightStart)
                    || this.IsQualifiedTypeStart(entry, rightStart))
                && this.ContainsKind(entry.CandidateKinds, AsnAssignmentKind.Type))
            {
                return this.SetKind(entry, AsnAssignmentKind.Type);
            }

            return false;
        }

        AsnToken firstGovernor = governor[0];
        bool lower = this.ContainsKind(entry.CandidateKinds, AsnAssignmentKind.Value)
            || this.ContainsKind(entry.CandidateKinds, AsnAssignmentKind.Object);
        if (this.IsUsefulObjectClass(firstGovernor))
        {
            return this.SetKind(
                entry,
                lower ? AsnAssignmentKind.Object : AsnAssignmentKind.ObjectSet);
        }

        if (this.IsUnambiguousTypeStart(firstGovernor))
        {
            return this.SetKind(
                entry,
                lower ? AsnAssignmentKind.Value : AsnAssignmentKind.ValueSet);
        }

        SymbolTableEntry? governorTarget = this.FindClassifiedDependency(
            entry,
            firstGovernor,
            graph);
        if (governorTarget == null)
        {
            return false;
        }

        if (governorTarget.Kind == AsnAssignmentKind.Type)
        {
            return this.SetKind(
                entry,
                lower ? AsnAssignmentKind.Value : AsnAssignmentKind.ValueSet);
        }

        if (governorTarget.Kind == AsnAssignmentKind.ObjectClass)
        {
            return this.SetKind(
                entry,
                lower ? AsnAssignmentKind.Object : AsnAssignmentKind.ObjectSet);
        }

        return false;
    }

    internal AsnToken[] GetEffectiveGovernorTokens(SymbolTableEntry entry)
    {
        AsnToken[] tokens = entry.DeclarationTokens;
        if (tokens.Length < 2)
        {
            return [];
        }

        int start = 1;
        if (tokens[start].Kind == AsnTokenKind.LeftBrace)
        {
            int depth = 0;
            while (start < tokens.Length)
            {
                if (tokens[start].Kind == AsnTokenKind.LeftBrace)
                {
                    depth = depth + 1;
                }
                else if (tokens[start].Kind == AsnTokenKind.RightBrace)
                {
                    depth = depth - 1;
                    if (depth == 0)
                    {
                        start = start + 1;
                        break;
                    }
                }

                start = start + 1;
            }
        }

        int end = start;
        while (end < tokens.Length && tokens[end].Kind != AsnTokenKind.Assignment)
        {
            end = end + 1;
        }

        if (end <= start)
        {
            return [];
        }

        return this.CopyTokenRange(tokens, start, end);
    }

    internal AsnToken? GetRightSideStart(SymbolTableEntry entry)
    {
        int index = 0;
        while (index < entry.DeclarationTokens.Length)
        {
            if (entry.DeclarationTokens[index].Kind == AsnTokenKind.Assignment)
            {
                return index + 1 < entry.DeclarationTokens.Length
                    ? entry.DeclarationTokens[index + 1]
                    : null;
            }

            index = index + 1;
        }

        return null;
    }

    internal SymbolTableEntry? FindClassifiedDependency(
        SymbolTableEntry source,
        AsnToken? token,
        CompilationGraph graph)
    {
        if (token == null)
        {
            return null;
        }

        int index = 0;
        while (index < graph.AssignmentEdges.Length)
        {
            AssignmentDependencyEdge edge = graph.AssignmentEdges[index];
            if (edge.Source != null
                && edge.Source.Symbol == source
                && edge.ReferenceToken == token
                && edge.Target != null
                && edge.Target.Symbol != null
                && edge.Target.Symbol.KindClassified)
            {
                return edge.Target.Symbol;
            }

            index = index + 1;
        }

        index = 0;
        while (index < graph.AssignmentEdges.Length)
        {
            AssignmentDependencyEdge edge = graph.AssignmentEdges[index];
            if (edge.Source != null
                && edge.Source.Symbol == source
                && edge.Target != null
                && edge.Target.Symbol != null
                && edge.Target.Symbol.Name == token.Text
                && edge.Target.Symbol.KindClassified)
            {
                return edge.Target.Symbol;
            }

            index = index + 1;
        }

        return null;
    }

    internal ClassifiedReference[] ClassifyReferences(AssignmentDependencyEdge[] edges)
    {
        this.ReferenceEdges = edges;
        ClassifiedReference[] references = new ClassifiedReference[edges.Length];
        int index = 0;
        while (index < edges.Length)
        {
            AssignmentDependencyEdge edge = edges[index];
            AsnAssignmentKind[] expected = this.GetExpectedReferenceKinds(edge);
            SymbolTableEntry? target = edge.Target == null ? null : edge.Target.Symbol;
            bool success = target != null
                && target.KindClassified
                && this.ContainsKind(expected, target.Kind);
            references[index] = new ClassifiedReference
            {
                Dependency = edge,
                ExpectedKinds = expected,
                ActualKind = target == null ? AsnAssignmentKind.Unknown : target.Kind,
                Success = success
            };
            if (!success && target != null && target.KindClassified)
            {
                this.AddReferenceConflictDiagnostic(edge, expected, target);
            }

            index = index + 1;
        }

        return references;
    }

    internal AsnAssignmentKind[] GetExpectedReferenceKinds(AssignmentDependencyEdge edge)
    {
        SymbolTableEntry? source = edge.Source == null ? null : edge.Source.Symbol;
        if (source == null || !source.KindClassified)
        {
            return [
                AsnAssignmentKind.Type,
                AsnAssignmentKind.Value,
                AsnAssignmentKind.ValueSet,
                AsnAssignmentKind.ObjectClass,
                AsnAssignmentKind.Object,
                AsnAssignmentKind.ObjectSet
            ];
        }

        AsnAssignmentKind[] actualParameterKinds =
            this.GetActualParameterReferenceKinds(edge);
        if (actualParameterKinds.Length > 0)
        {
            SymbolTableEntry? actualTarget = edge.Target == null
                ? null
                : edge.Target.Symbol;
            if (actualParameterKinds.Length == 1
                && actualParameterKinds[0] == AsnAssignmentKind.Type
                && this.IsParameterizedValueSetTypeContext(
                    source,
                    edge.ReferenceToken,
                    actualTarget))
            {
                return [AsnAssignmentKind.Type, AsnAssignmentKind.ValueSet];
            }

            return actualParameterKinds;
        }

        if (this.IsFormalParameterListContext(source, edge.ReferenceToken))
        {
            return [AsnAssignmentKind.Type, AsnAssignmentKind.ObjectClass];
        }

        if (this.IsObjectClassFieldContext(source, edge.ReferenceToken))
        {
            bool upper = edge.ReferenceToken != null
                && edge.ReferenceToken.Text.Length > 0
                && edge.ReferenceToken.Text[0] >= 'A'
                && edge.ReferenceToken.Text[0] <= 'Z';
            if (source.Kind == AsnAssignmentKind.Type)
            {
                return upper
                    ? [AsnAssignmentKind.ObjectClass]
                    : [AsnAssignmentKind.Object];
            }

            if (source.Kind == AsnAssignmentKind.Value)
            {
                return [AsnAssignmentKind.Object];
            }

            if (source.Kind == AsnAssignmentKind.Object)
            {
                return upper
                    ? [AsnAssignmentKind.ObjectSet]
                    : [AsnAssignmentKind.Object];
            }

            if (source.Kind == AsnAssignmentKind.ValueSet)
            {
                return [AsnAssignmentKind.ObjectSet];
            }

            if (source.Kind == AsnAssignmentKind.ObjectSet)
            {
                return upper
                    ? [AsnAssignmentKind.ObjectSet]
                    : [AsnAssignmentKind.Object];
            }
        }

        if (source.Kind == AsnAssignmentKind.ObjectClass
            && this.IsObjectClassDefaultContext(source, edge.ReferenceToken))
        {
            return [
                AsnAssignmentKind.Type,
                AsnAssignmentKind.Value,
                AsnAssignmentKind.ValueSet,
                AsnAssignmentKind.ObjectClass,
                AsnAssignmentKind.Object,
                AsnAssignmentKind.ObjectSet
            ];
        }

        bool governor = edge.ReferenceToken != null
            && source.Declaration != null
            && source.Declaration.AssignmentToken != null
            && edge.ReferenceToken.Span != null
            && source.Declaration.AssignmentToken.Span != null
            && edge.ReferenceToken.Span.Start.Offset
                < source.Declaration.AssignmentToken.Span.Start.Offset;
        if (governor)
        {
            if (source.Kind == AsnAssignmentKind.Value
                || source.Kind == AsnAssignmentKind.ValueSet)
            {
                return [AsnAssignmentKind.Type];
            }

            if (source.Kind == AsnAssignmentKind.Object
                || source.Kind == AsnAssignmentKind.ObjectSet)
            {
                return [AsnAssignmentKind.ObjectClass];
            }
        }

        switch (source.Kind)
        {
            case AsnAssignmentKind.Type:
                if (this.IsParameterizedValueSetTypeContext(
                    source,
                    edge.ReferenceToken,
                    edge.Target == null ? null : edge.Target.Symbol))
                {
                    return [AsnAssignmentKind.Type, AsnAssignmentKind.ValueSet];
                }

                if (this.IsInstanceOfObjectClassContext(source, edge.ReferenceToken))
                {
                    return [AsnAssignmentKind.ObjectClass];
                }

                if (this.IsObjectClassFieldContext(source, edge.ReferenceToken))
                {
                    return [AsnAssignmentKind.ObjectClass];
                }

                if (this.IsTableObjectSetContext(source, edge.ReferenceToken))
                {
                    return [AsnAssignmentKind.ObjectSet];
                }

                if (this.IsTypeValueContext(source, edge.ReferenceToken))
                {
                    return [AsnAssignmentKind.Type, AsnAssignmentKind.Value];
                }

                return [AsnAssignmentKind.Type];
            case AsnAssignmentKind.Value:
                return [AsnAssignmentKind.Value];
            case AsnAssignmentKind.ValueSet:
                return [AsnAssignmentKind.Value, AsnAssignmentKind.ValueSet];
            case AsnAssignmentKind.ObjectClass:
                return [AsnAssignmentKind.Type, AsnAssignmentKind.ObjectClass];
            case AsnAssignmentKind.Object:
                return [
                    AsnAssignmentKind.Type,
                    AsnAssignmentKind.Value,
                    AsnAssignmentKind.ValueSet,
                    AsnAssignmentKind.Object,
                    AsnAssignmentKind.ObjectSet
                ];
            case AsnAssignmentKind.ObjectSet:
                return [
                    AsnAssignmentKind.Type,
                    AsnAssignmentKind.Value,
                    AsnAssignmentKind.ValueSet,
                    AsnAssignmentKind.Object,
                    AsnAssignmentKind.ObjectSet
                ];
            default:
                return [];
        }
    }

    internal bool IsParameterizedValueSetTypeContext(
        SymbolTableEntry source,
        AsnToken? reference,
        SymbolTableEntry? target)
    {
        int index = this.FindDeclarationTokenIndex(source, reference);
        return target != null
            && target.Parameterized
            && target.Kind == AsnAssignmentKind.ValueSet
            && index >= 0
            && index + 1 < source.DeclarationTokens.Length
            && source.DeclarationTokens[index + 1].Kind
                == AsnTokenKind.LeftBrace;
    }

    internal AsnAssignmentKind[] GetActualParameterReferenceKinds(
        AssignmentDependencyEdge edge)
    {
        SymbolTableEntry? source = edge.Source == null ? null : edge.Source.Symbol;
        int referenceIndex = source == null
            ? -1
            : this.FindDeclarationTokenIndex(source, edge.ReferenceToken);
        if (source == null || referenceIndex < 0)
        {
            return [];
        }

        int bestBrace = -1;
        ParsedParameterSyntax? formal = null;
        int edgeIndex = 0;
        while (edgeIndex < this.ReferenceEdges.Length)
        {
            AssignmentDependencyEdge candidate = this.ReferenceEdges[edgeIndex];
            SymbolTableEntry? target = candidate.Target == null
                ? null
                : candidate.Target.Symbol;
            if (candidate.Source == edge.Source
                && target != null
                && target.Parameterized
                && target.FormalParameterSyntax.Length > 0)
            {
                int targetIndex = this.FindDeclarationTokenIndex(
                    source,
                    candidate.ReferenceToken);
                int braceIndex = targetIndex + 1;
                if (braceIndex < source.DeclarationTokens.Length
                    && source.DeclarationTokens[braceIndex].Kind
                        == AsnTokenKind.LeftBrace
                    && braceIndex < referenceIndex)
                {
                    int ordinal = this.FindActualParameterOrdinal(
                        source.DeclarationTokens,
                        braceIndex,
                        referenceIndex);
                    if (ordinal >= 0
                        && ordinal < target.FormalParameterSyntax.Length
                        && braceIndex > bestBrace)
                    {
                        bestBrace = braceIndex;
                        formal = target.FormalParameterSyntax[ordinal];
                    }
                }
            }

            edgeIndex = edgeIndex + 1;
        }

        return formal == null
            ? []
            : this.MapFormalReferenceKinds(formal.Parameter.Kind);
    }

    internal int FindActualParameterOrdinal(
        AsnToken[] tokens,
        int braceIndex,
        int referenceIndex)
    {
        int braces = 1;
        int parentheses = 0;
        int brackets = 0;
        int ordinal = 0;
        int index = braceIndex + 1;
        while (index <= referenceIndex && index < tokens.Length)
        {
            AsnToken token = tokens[index];
            if (token.Kind == AsnTokenKind.LeftBrace)
            {
                braces = braces + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBrace)
            {
                braces = braces - 1;
                if (braces == 0 && index < referenceIndex)
                {
                    return -1;
                }
            }
            else if (token.Kind == AsnTokenKind.LeftParenthesis)
            {
                parentheses = parentheses + 1;
            }
            else if (token.Kind == AsnTokenKind.RightParenthesis)
            {
                parentheses = parentheses - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftBracket)
            {
                brackets = brackets + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBracket)
            {
                brackets = brackets - 1;
            }
            else if (token.Kind == AsnTokenKind.Comma
                && braces == 1
                && parentheses == 0
                && brackets == 0)
            {
                ordinal = ordinal + 1;
            }

            index = index + 1;
        }

        return braces > 0 ? ordinal : -1;
    }

    internal AsnAssignmentKind[] MapFormalReferenceKinds(AsnParameterKind kind)
    {
        switch (kind)
        {
            case AsnParameterKind.Type:
                return [AsnAssignmentKind.Type];
            case AsnParameterKind.Value:
                return [AsnAssignmentKind.Value];
            case AsnParameterKind.ValueSet:
                return [AsnAssignmentKind.Value, AsnAssignmentKind.ValueSet];
            case AsnParameterKind.ObjectClass:
                return [AsnAssignmentKind.ObjectClass];
            case AsnParameterKind.Object:
                return [AsnAssignmentKind.Object];
            case AsnParameterKind.ObjectSet:
                return [AsnAssignmentKind.Object, AsnAssignmentKind.ObjectSet];
            default:
                return [];
        }
    }

    internal bool IsObjectClassFieldContext(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        int index = this.FindDeclarationTokenIndex(source, reference);
        return index >= 0
            && index + 2 < source.DeclarationTokens.Length
            && source.DeclarationTokens[index + 1].Kind == AsnTokenKind.Dot
            && this.IsObjectFieldReference(source.DeclarationTokens[index + 2]);
    }

    internal bool IsFormalParameterListContext(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        int index = this.FindDeclarationTokenIndex(source, reference);
        if (index < 1
            || source.DeclarationTokens.Length < 3
            || source.DeclarationTokens[1].Kind != AsnTokenKind.LeftBrace)
        {
            return false;
        }

        int depth = 0;
        int end = 1;
        while (end < source.DeclarationTokens.Length)
        {
            if (source.DeclarationTokens[end].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (source.DeclarationTokens[end].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    break;
                }
            }

            end = end + 1;
        }

        return index <= end;
    }

    internal bool IsObjectClassDefaultContext(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        int referenceIndex = this.FindDeclarationTokenIndex(source, reference);
        int braces = 0;
        int parentheses = 0;
        int brackets = 0;
        bool afterDefault = false;
        int index = 0;
        while (index <= referenceIndex)
        {
            AsnToken token = source.DeclarationTokens[index];
            if (token.Kind == AsnTokenKind.LeftBrace)
            {
                braces = braces + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBrace && braces > 0)
            {
                braces = braces - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftParenthesis)
            {
                parentheses = parentheses + 1;
            }
            else if (token.Kind == AsnTokenKind.RightParenthesis && parentheses > 0)
            {
                parentheses = parentheses - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftBracket)
            {
                brackets = brackets + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBracket && brackets > 0)
            {
                brackets = brackets - 1;
            }
            else if (braces == 1
                && parentheses == 0
                && brackets == 0
                && token.Kind == AsnTokenKind.Comma)
            {
                afterDefault = false;
            }
            else if (braces == 1
                && parentheses == 0
                && brackets == 0
                && token.Text == "DEFAULT")
            {
                afterDefault = true;
            }

            index = index + 1;
        }

        return afterDefault;
    }

    internal bool IsInstanceOfObjectClassContext(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        int index = this.FindDeclarationTokenIndex(source, reference);
        bool unqualified = index >= 2
            && source.DeclarationTokens[index - 1].Text == "OF"
            && source.DeclarationTokens[index - 2].Text == "INSTANCE";
        bool qualified = index >= 4
            && source.DeclarationTokens[index - 1].Kind == AsnTokenKind.Dot
            && source.DeclarationTokens[index - 3].Text == "OF"
            && source.DeclarationTokens[index - 4].Text == "INSTANCE";
        return unqualified || qualified;
    }

    internal bool IsTableObjectSetContext(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        int index = this.FindDeclarationTokenIndex(source, reference);
        bool unqualified = index > 0
            && index + 1 < source.DeclarationTokens.Length
            && source.DeclarationTokens[index - 1].Kind == AsnTokenKind.LeftBrace
            && source.DeclarationTokens[index + 1].Kind == AsnTokenKind.RightBrace;
        bool qualified = index >= 3
            && index + 1 < source.DeclarationTokens.Length
            && source.DeclarationTokens[index - 1].Kind == AsnTokenKind.Dot
            && source.DeclarationTokens[index - 3].Kind == AsnTokenKind.LeftBrace
            && source.DeclarationTokens[index + 1].Kind == AsnTokenKind.RightBrace;
        return unqualified || qualified;
    }

    internal int FindDeclarationTokenIndex(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        if (reference == null)
        {
            return -1;
        }

        int index = 0;
        while (index < source.DeclarationTokens.Length)
        {
            if (source.DeclarationTokens[index] == reference)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsObjectFieldReference(AsnToken token)
    {
        return token.Kind == AsnTokenKind.TypeFieldReference
            || token.Kind == AsnTokenKind.ValueFieldReference
            || token.Kind == AsnTokenKind.ValueSetFieldReference
            || token.Kind == AsnTokenKind.ObjectFieldReference
            || token.Kind == AsnTokenKind.ObjectSetFieldReference;
    }

    internal bool IsTypeValueContext(
        SymbolTableEntry source,
        AsnToken? reference)
    {
        if (reference == null)
        {
            return false;
        }

        int referenceIndex = 0;
        while (referenceIndex < source.DeclarationTokens.Length
            && source.DeclarationTokens[referenceIndex] != reference)
        {
            referenceIndex = referenceIndex + 1;
        }

        if (referenceIndex >= source.DeclarationTokens.Length)
        {
            return false;
        }

        int parenthesisDepth = 0;
        int bracketDepth = 0;
        int index = referenceIndex - 1;
        while (index >= 0
            && source.DeclarationTokens[index].Kind != AsnTokenKind.Assignment)
        {
            AsnToken token = source.DeclarationTokens[index];
            if (token.Kind == AsnTokenKind.RightParenthesis)
            {
                parenthesisDepth = parenthesisDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.LeftParenthesis)
            {
                if (parenthesisDepth == 0)
                {
                    return true;
                }

                parenthesisDepth = parenthesisDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.RightBracket)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.LeftBracket)
            {
                if (bracketDepth == 0)
                {
                    return true;
                }

                bracketDepth = bracketDepth - 1;
            }

            if (parenthesisDepth == 0 && bracketDepth == 0)
            {
                if (token.Text == "DEFAULT")
                {
                    return true;
                }

                if (token.Kind == AsnTokenKind.Comma
                    || token.Kind == AsnTokenKind.LeftBrace)
                {
                    return false;
                }
            }

            index = index - 1;
        }

        return false;
    }

    internal void AddReferenceConflictDiagnostic(
        AssignmentDependencyEdge edge,
        AsnAssignmentKind[] expected,
        SymbolTableEntry target)
    {
        AsnDiagnosticRelatedLocation[] related = target.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The referenced declaration is classified as " + target.Kind.ToString() + ".",
                Span = target.Span
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            10,
            "Reference '" + target.Name + "' requires " + this.FormatKinds(expected)
                + " context but resolves to " + target.Kind.ToString() + ".",
            edge.ReferenceToken == null ? null : edge.ReferenceToken.Span,
            related);
    }

    internal void ReindexTables(SymbolTable[] tables)
    {
        int index = 0;
        while (index < tables.Length)
        {
            tables[index].TypeSymbols = this.SelectKind(tables[index].Symbols, AsnAssignmentKind.Type);
            tables[index].ValueSymbols = this.SelectKind(tables[index].Symbols, AsnAssignmentKind.Value);
            tables[index].ValueSetSymbols = this.SelectKind(tables[index].Symbols, AsnAssignmentKind.ValueSet);
            tables[index].ObjectClassSymbols = this.SelectKind(
                tables[index].Symbols,
                AsnAssignmentKind.ObjectClass);
            tables[index].ObjectSymbols = this.SelectKind(tables[index].Symbols, AsnAssignmentKind.Object);
            tables[index].ObjectSetSymbols = this.SelectKind(
                tables[index].Symbols,
                AsnAssignmentKind.ObjectSet);
            index = index + 1;
        }
    }

    internal void UpdateImportedBindingKinds(SymbolTable[] tables)
    {
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int resolutionIndex = 0;
            while (resolutionIndex < tables[tableIndex].ImportResolutions.Length)
            {
                ImportedSymbolBinding[] bindings =
                    tables[tableIndex].ImportResolutions[resolutionIndex].Symbols;
                int bindingIndex = 0;
                while (bindingIndex < bindings.Length)
                {
                    SymbolTableEntry? target = bindings[bindingIndex].Target;
                    if (target != null && target.KindClassified)
                    {
                        bindings[bindingIndex].Kind = target.Kind;
                    }

                    bindingIndex = bindingIndex + 1;
                }

                resolutionIndex = resolutionIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }
    }

    internal SymbolTableEntry[] SelectKind(
        SymbolTableEntry[] symbols,
        AsnAssignmentKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < symbols.Length)
        {
            if (symbols[index].KindClassified && symbols[index].Kind == kind)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        SymbolTableEntry[] result = new SymbolTableEntry[count];
        int resultIndex = 0;
        index = 0;
        while (index < symbols.Length)
        {
            if (symbols[index].KindClassified && symbols[index].Kind == kind)
            {
                result[resultIndex] = symbols[index];
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool SetKind(SymbolTableEntry entry, AsnAssignmentKind kind)
    {
        entry.Kind = kind;
        entry.KindClassified = true;
        return true;
    }

    internal SymbolTableEntry[] CopyAssignments(SymbolTable[] tables)
    {
        int count = 0;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            count = count + tables[tableIndex].Symbols.Length;
            tableIndex = tableIndex + 1;
        }

        SymbolTableEntry[] result = new SymbolTableEntry[count];
        int resultIndex = 0;
        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                result[resultIndex] = tables[tableIndex].Symbols[symbolIndex];
                resultIndex = resultIndex + 1;
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return result;
    }

    internal bool IsUnambiguousTypeStart(AsnToken token)
    {
        if (token.Kind == AsnTokenKind.LeftBracket)
        {
            return true;
        }

        if (token.Kind == AsnTokenKind.TypeReference)
        {
            return true;
        }

        if (token.Kind != AsnTokenKind.ReservedWord)
        {
            return false;
        }

        return token.Text != "CLASS"
            && token.Text != "TYPE-IDENTIFIER"
            && token.Text != "ABSTRACT-SYNTAX"
            && token.Text != "WITH";
    }

    internal bool IsSelectionTypeStart(SymbolTableEntry entry, AsnToken token)
    {
        int index = 0;
        while (index + 1 < entry.DeclarationTokens.Length)
        {
            if (entry.DeclarationTokens[index] == token)
            {
                return entry.DeclarationTokens[index + 1].Kind == AsnTokenKind.LessThan;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsQualifiedTypeStart(SymbolTableEntry entry, AsnToken token)
    {
        int index = 0;
        while (index + 2 < entry.DeclarationTokens.Length)
        {
            if (entry.DeclarationTokens[index] == token)
            {
                return entry.DeclarationTokens[index + 1].Kind == AsnTokenKind.Dot
                    && entry.DeclarationTokens[index + 2].Kind == AsnTokenKind.TypeReference;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsUsefulObjectClass(AsnToken token)
    {
        return token.Text == "TYPE-IDENTIFIER" || token.Text == "ABSTRACT-SYNTAX";
    }

    internal bool ContainsKind(AsnAssignmentKind[] kinds, AsnAssignmentKind expected)
    {
        int index = 0;
        while (index < kinds.Length)
        {
            if (kinds[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnToken[] CopyTokenRange(AsnToken[] tokens, int start, int end)
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

    internal string FormatKinds(AsnAssignmentKind[] kinds)
    {
        string result = "";
        int index = 0;
        while (index < kinds.Length)
        {
            result = result.Length == 0
                ? kinds[index].ToString()
                : result + " or " + kinds[index].ToString();
            index = index + 1;
        }

        return result;
    }
}
