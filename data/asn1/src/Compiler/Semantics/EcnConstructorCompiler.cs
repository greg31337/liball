namespace asn1;

internal class EcnConstructorCompiler
{
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal EcnConstructorCompiler(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool CompileTables(EcnSymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int objectIndex = 0;
            while (objectIndex < tables[tableIndex].EncodingObjects.Length)
            {
                EcnSymbol symbol = tables[tableIndex].EncodingObjects[objectIndex];
                if (!symbol.Imported && symbol.ObjectDefinition != null)
                {
                    this.ResolveConstructor(symbol.ObjectDefinition);
                }

                objectIndex = objectIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal EcnConstructorDefinition ResolveConstructor(
        EcnEncodingObjectDefinition definition)
    {
        if (definition.ConstructorResolutionState == 2
            && definition.Constructor != null)
        {
            return definition.Constructor;
        }

        if (definition.ConstructorResolutionState == 1)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidConstructor,
                "The encoding object '" + definition.Name
                    + "' has a cyclic constructor definition.",
                definition.Span);
            return new EcnConstructorDefinition();
        }

        definition.ConstructorResolutionState = 1;
        EcnConstructorDefinition constructor;
        if (definition.ReferenceDefinition)
        {
            EcnEncodingObjectDefinition? referenced =
                definition.ReferencedObjectSymbol == null
                    ? null
                    : definition.ReferencedObjectSymbol.ObjectDefinition;
            constructor = referenced == null
                ? new EcnConstructorDefinition()
                : this.CopyConstructor(this.ResolveConstructor(referenced));
            if (referenced == null || !referenced.Valid)
            {
                constructor.Valid = false;
            }
        }
        else
        {
            constructor = this.CreateConstructor(definition);
        }

        definition.Constructor = constructor;
        definition.ConstructorResolutionState = 2;
        if (!constructor.Valid)
        {
            definition.Valid = false;
        }

        return constructor;
    }

    internal EcnConstructorDefinition CreateConstructor(
        EcnEncodingObjectDefinition definition)
    {
        EcnSymbol? symbol = definition.Symbol;
        EcnEncodingStructure? structure = this.FindGovernorStructure(symbol);
        EcnConstructorKind kind = this.ResolveKind(symbol, structure);
        EcnAssignmentSyntax? assignment = symbol == null
            ? null
            : symbol.Assignment;
        EcnControlDependency[] controls = assignment == null
            ? []
            : this.ParseControlDependencies(assignment.BodyTokens);
        EcnConditionDefinition[] conditions = assignment == null
            ? []
            : this.ParseConditions(assignment.Clauses);
        EcnConstructorDefinition constructor = new EcnConstructorDefinition
        {
            Kind = kind,
            Structure = structure,
            Fields = structure == null
                ? this.CreateImportedFields(symbol)
                : this.CopyFields(structure.Fields),
            ControlDependencies = controls,
            Conditions = conditions,
            Valid = true
        };

        int index = 0;
        while (index < controls.Length)
        {
            this.ValidateControlDependency(controls[index]);
            this.ValidateModeApplicability(controls[index]);
            if (!controls[index].Valid)
            {
                constructor.Valid = false;
            }

            index = index + 1;
        }

        index = 0;
        while (index < conditions.Length)
        {
            this.ValidateCondition(conditions[index]);
            if (!conditions[index].Valid)
            {
                constructor.Valid = false;
            }

            index = index + 1;
        }

        if (!this.ValidateControlSet(controls, conditions))
        {
            constructor.Valid = false;
        }

        if ((kind == EcnConstructorKind.ConditionalInteger
                || kind == EcnConstructorKind.ConditionalRepetition)
            && conditions.Length == 0)
        {
            constructor.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidConstructor,
                "Conditional encoding object '" + definition.Name
                    + "' requires at least one IF or IF-ALL condition.",
                definition.Span);
        }

        return constructor;
    }

    internal EcnConstructorKind ResolveKind(
        EcnSymbol? symbol,
        EcnEncodingStructure? structure)
    {
        if (structure != null)
        {
            if (structure.Kind == EcnEncodingStructureKind.Concatenation)
            {
                return EcnConstructorKind.Concatenation;
            }

            if (structure.Kind == EcnEncodingStructureKind.Alternatives)
            {
                return EcnConstructorKind.Alternatives;
            }

            if (structure.Kind == EcnEncodingStructureKind.Repetition)
            {
                return EcnConstructorKind.Repetition;
            }

            if (structure.Kind == EcnEncodingStructureKind.Tagged)
            {
                return EcnConstructorKind.Tag;
            }
        }

        if (symbol == null)
        {
            return EcnConstructorKind.Unknown;
        }

        EcnConstructorKind importedKind = this.ResolveImportedAsnKind(symbol);
        if (importedKind != EcnConstructorKind.Unknown)
        {
            return importedKind;
        }

        if (symbol.PrimitiveClassName == "#CONDITIONAL-INT")
        {
            return EcnConstructorKind.ConditionalInteger;
        }

        if (symbol.PrimitiveClassName == "#CONDITIONAL-REPETITION")
        {
            return EcnConstructorKind.ConditionalRepetition;
        }

        switch (symbol.ClassCategory)
        {
            case EcnClassCategory.Concatenation:
                return EcnConstructorKind.Concatenation;
            case EcnClassCategory.Alternatives:
                return EcnConstructorKind.Alternatives;
            case EcnClassCategory.Repetition:
                return EcnConstructorKind.Repetition;
            case EcnClassCategory.Optionality:
                return EcnConstructorKind.Optionality;
            case EcnClassCategory.Tag:
                return EcnConstructorKind.Tag;
            default:
                return EcnConstructorKind.Unknown;
        }
    }

    internal EcnConstructorKind ResolveImportedAsnKind(EcnSymbol symbol)
    {
        EcnSymbol? governor = symbol.GovernorSymbol;
        if (governor == null || !governor.ImportedAsnType
            || governor.AsnTypeAssignment == null
            || governor.AsnTypeAssignment.Type == null)
        {
            return EcnConstructorKind.Unknown;
        }

        switch (governor.AsnTypeAssignment.Type.Kind)
        {
            case AsnTypeKind.Sequence:
            case AsnTypeKind.Set:
                return EcnConstructorKind.Concatenation;
            case AsnTypeKind.Choice:
                return EcnConstructorKind.Alternatives;
            case AsnTypeKind.SequenceOf:
            case AsnTypeKind.SetOf:
                return EcnConstructorKind.Repetition;
            case AsnTypeKind.Tagged:
            case AsnTypeKind.Prefixed:
                return EcnConstructorKind.Tag;
            default:
                return EcnConstructorKind.Unknown;
        }
    }

    internal EcnEncodingField[] CreateImportedFields(EcnSymbol? symbol)
    {
        EcnSymbol? governor = symbol == null ? null : symbol.GovernorSymbol;
        AsnType? type = governor == null
                || !governor.ImportedAsnType
                || governor.AsnTypeAssignment == null
            ? null
            : governor.AsnTypeAssignment.Type;
        if (type == null)
        {
            return [];
        }

        EcnEncodingField[] fields =
            new EcnEncodingField[type.Components.Length];
        int index = 0;
        while (index < type.Components.Length)
        {
            fields[index] = new EcnEncodingField
            {
                Name = type.Components[index].Name,
                Optional = type.Components[index].Optional
                    || type.Components[index].HasDefaultValue
            };
            index = index + 1;
        }

        return fields;
    }

    internal EcnEncodingStructure? FindGovernorStructure(EcnSymbol? symbol)
    {
        if (symbol == null || symbol.GovernorSymbol == null)
        {
            return null;
        }

        EcnSymbol governor = symbol.GovernorSymbol.ImportedTarget == null
            ? symbol.GovernorSymbol
            : symbol.GovernorSymbol.ImportedTarget;
        return governor.ClassDefinition == null
            ? null
            : governor.ClassDefinition.Structure;
    }

    internal EcnControlDependency[] ParseControlDependencies(AsnToken[] tokens)
    {
        System.Collections.Generic.List<EcnControlDependency> dependencies =
            new System.Collections.Generic.List<EcnControlDependency>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (this.IsControlStart(tokens[index].Text))
            {
                int end = this.FindControlEnd(tokens, index + 1);
                int determined = this.FindTextInRange(
                    tokens,
                    index + 1,
                    end,
                    "DETERMINED");
                int usingIndex = this.FindTextInRange(
                    tokens,
                    index + 1,
                    end,
                    "USING");
                EcnControlDependencyKind kind = this.GetControlKind(
                    tokens[index].Text);
                if (determined >= 0 || usingIndex >= 0
                    || kind == EcnControlDependencyKind.Alternative
                    || kind == EcnControlDependencyKind.Presence)
                {
                    dependencies.Add(this.CreateControlDependency(
                        tokens,
                        index,
                        end,
                        dependencies.Count));
                }

                index = end;
            }
            else
            {
                index = index + 1;
            }
        }

        return dependencies.ToArray();
    }

    internal bool IsControlStart(string text)
    {
        return text == "ENCODING-SPACE"
            || text == "ALTERNATIVE"
            || text == "REPETITION-SPACE"
            || text == "PRESENCE";
    }

    internal EcnControlDependencyKind GetControlKind(string text)
    {
        if (text == "ALTERNATIVE")
        {
            return EcnControlDependencyKind.Alternative;
        }

        if (text == "REPETITION-SPACE")
        {
            return EcnControlDependencyKind.RepetitionSpace;
        }

        return text == "PRESENCE"
            ? EcnControlDependencyKind.Presence
            : EcnControlDependencyKind.EncodingSpace;
    }

    internal int FindControlEnd(AsnToken[] tokens, int start)
    {
        int index = start;
        while (index < tokens.Length)
        {
            string text = tokens[index].Text;
            if (this.IsControlStart(text)
                || text == "IF"
                || text == "IF-ALL"
                || text == "MAPPING"
                || text == "TRANSFORMS"
                || text == "OPTIONS"
                || text == "ORDERED"
                || tokens[index].Kind == AsnTokenKind.RightBrace)
            {
                return index;
            }

            index = index + 1;
        }

        return tokens.Length;
    }

    internal EcnControlDependency CreateControlDependency(
        AsnToken[] tokens,
        int start,
        int end,
        int order)
    {
        AsnToken[] source = this.CopyTokenRange(tokens, start, end - start);
        EcnControlDependency dependency = new EcnControlDependency
        {
            Kind = this.GetControlKind(tokens[start].Text),
            Order = order,
            Tokens = source,
            Span = this.CreateSpan(source),
            Valid = true
        };
        int determined = this.FindTextInRange(
            tokens,
            start + 1,
            end,
            "DETERMINED");
        if (determined >= 0
            && determined + 2 < end
            && tokens[determined + 1].Text == "BY")
        {
            dependency.Mode = this.ParseMode(tokens[determined + 2].Text);
        }

        int usingIndex = this.FindTextInRange(
            tokens,
            start + 1,
            end,
            "USING");
        if (usingIndex >= 0 && usingIndex + 1 < end)
        {
            int referenceEnd = this.FindReferenceEnd(
                tokens,
                usingIndex + 1,
                end);
            dependency.Reference = this.JoinCompact(
                tokens,
                usingIndex + 1,
                referenceEnd - usingIndex - 1);
        }

        int auxiliary = dependency.Mode == EcnDeterminationMode.Pointer
            ? this.FindTextInRange(tokens, start + 1, end, "START-POINTER")
            : this.FindTextInRange(tokens, start + 1, end, "HANDLE");
        if (auxiliary >= 0 && auxiliary + 1 < end)
        {
            dependency.AuxiliaryReference = tokens[auxiliary + 1].Text;
        }

        dependency.EncoderWrites = dependency.Mode
                == EcnDeterminationMode.FieldToBeSet
            || dependency.Mode == EcnDeterminationMode.FlagToBeSet;
        dependency.DecoderReads = dependency.EncoderWrites;
        dependency.RequiresPriorValue = dependency.Mode
                == EcnDeterminationMode.FieldToBeUsed
            || dependency.Mode == EcnDeterminationMode.FlagToBeUsed;
        return dependency;
    }

    internal EcnDeterminationMode ParseMode(string text)
    {
        switch (text)
        {
            case "field-to-be-set": return EcnDeterminationMode.FieldToBeSet;
            case "field-to-be-used": return EcnDeterminationMode.FieldToBeUsed;
            case "flag-to-be-set": return EcnDeterminationMode.FlagToBeSet;
            case "flag-to-be-used": return EcnDeterminationMode.FlagToBeUsed;
            case "container": return EcnDeterminationMode.Container;
            case "pattern": return EcnDeterminationMode.Pattern;
            case "handle": return EcnDeterminationMode.Handle;
            case "pointer": return EcnDeterminationMode.Pointer;
            case "not-needed": return EcnDeterminationMode.NotNeeded;
            default: return EcnDeterminationMode.Unknown;
        }
    }

    internal int FindReferenceEnd(AsnToken[] tokens, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (tokens[index].Kind == AsnTokenKind.Comma
                || this.IsReferenceBoundary(tokens[index].Text))
            {
                return index;
            }

            index = index + 1;
        }

        return end;
    }

    internal bool IsReferenceBoundary(string text)
    {
        return text == "DETERMINED"
            || text == "HANDLE"
            || text == "START-POINTER"
            || text == "MAPPING"
            || text == "IF"
            || text == "IF-ALL"
            || text == "TRANSFORMS"
            || text == "ENCODER-TRANSFORMS"
            || text == "DECODER-TRANSFORMS"
            || text == "OPTIONS"
            || text == "ORDERED"
            || this.IsControlStart(text);
    }

    internal void ValidateControlDependency(EcnControlDependency dependency)
    {
        bool requiresReference = dependency.Mode
                == EcnDeterminationMode.FieldToBeSet
            || dependency.Mode == EcnDeterminationMode.FieldToBeUsed
            || dependency.Mode == EcnDeterminationMode.FlagToBeSet
            || dependency.Mode == EcnDeterminationMode.FlagToBeUsed;
        bool forbidsReference = dependency.Mode
                == EcnDeterminationMode.Container
            || dependency.Mode == EcnDeterminationMode.Pattern
            || dependency.Mode == EcnDeterminationMode.Handle
            || dependency.Mode == EcnDeterminationMode.Pointer
            || dependency.Mode == EcnDeterminationMode.NotNeeded;
        if (dependency.Mode == EcnDeterminationMode.Unknown)
        {
            dependency.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidControlDependency,
                "An ECN control dependency requires DETERMINED BY followed by a supported determination mode.",
                dependency.Span);
        }
        else if (requiresReference && dependency.Reference.Length == 0)
        {
            dependency.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidControlDependency,
                "Determination mode '" + this.GetModeText(dependency.Mode)
                    + "' requires a USING reference.",
                dependency.Span);
        }
        else if (forbidsReference && dependency.Reference.Length > 0)
        {
            dependency.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidControlDependency,
                "Determination mode '" + this.GetModeText(dependency.Mode)
                    + "' cannot have a USING reference.",
                dependency.Span);
        }

        if ((dependency.Mode == EcnDeterminationMode.Handle
                || dependency.Mode == EcnDeterminationMode.Pointer)
            && dependency.AuxiliaryReference.Length == 0)
        {
            dependency.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidControlDependency,
                "Determination mode '" + this.GetModeText(dependency.Mode)
                    + "' requires its control reference.",
                dependency.Span);
        }
    }

    internal string GetModeText(EcnDeterminationMode mode)
    {
        switch (mode)
        {
            case EcnDeterminationMode.FieldToBeSet: return "field-to-be-set";
            case EcnDeterminationMode.FieldToBeUsed: return "field-to-be-used";
            case EcnDeterminationMode.FlagToBeSet: return "flag-to-be-set";
            case EcnDeterminationMode.FlagToBeUsed: return "flag-to-be-used";
            case EcnDeterminationMode.Container: return "container";
            case EcnDeterminationMode.Pattern: return "pattern";
            case EcnDeterminationMode.Handle: return "handle";
            case EcnDeterminationMode.Pointer: return "pointer";
            case EcnDeterminationMode.NotNeeded: return "not-needed";
            default: return "unknown";
        }
    }

    internal void ValidateModeApplicability(EcnControlDependency dependency)
    {
        bool allowed = true;
        if (dependency.Kind == EcnControlDependencyKind.Alternative)
        {
            allowed = dependency.Mode == EcnDeterminationMode.FieldToBeSet
                || dependency.Mode == EcnDeterminationMode.FieldToBeUsed
                || dependency.Mode == EcnDeterminationMode.Handle;
        }
        else if (dependency.Kind == EcnControlDependencyKind.Presence)
        {
            allowed = dependency.Mode == EcnDeterminationMode.FieldToBeSet
                || dependency.Mode == EcnDeterminationMode.FieldToBeUsed
                || dependency.Mode == EcnDeterminationMode.Container
                || dependency.Mode == EcnDeterminationMode.Handle
                || dependency.Mode == EcnDeterminationMode.Pointer;
        }
        else if (dependency.Kind == EcnControlDependencyKind.RepetitionSpace)
        {
            allowed = dependency.Mode == EcnDeterminationMode.FieldToBeSet
                || dependency.Mode == EcnDeterminationMode.FieldToBeUsed
                || dependency.Mode == EcnDeterminationMode.FlagToBeSet
                || dependency.Mode == EcnDeterminationMode.FlagToBeUsed
                || dependency.Mode == EcnDeterminationMode.Container
                || dependency.Mode == EcnDeterminationMode.Pattern
                || dependency.Mode == EcnDeterminationMode.Handle
                || dependency.Mode == EcnDeterminationMode.NotNeeded;
        }
        else if (dependency.Kind == EcnControlDependencyKind.EncodingSpace)
        {
            allowed = dependency.Mode == EcnDeterminationMode.FieldToBeSet
                || dependency.Mode == EcnDeterminationMode.FieldToBeUsed
                || dependency.Mode == EcnDeterminationMode.Handle;
        }

        if (!allowed && dependency.Mode != EcnDeterminationMode.Unknown)
        {
            dependency.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidControlDependency,
                "Determination mode '" + this.GetModeText(dependency.Mode)
                    + "' is not valid for ECN control group '"
                    + dependency.Kind.ToString() + "'.",
                dependency.Span);
        }
    }

    internal bool ValidateControlSet(
        EcnControlDependency[] controls,
        EcnConditionDefinition[] conditions)
    {
        bool valid = true;
        int index = 0;
        while (index < controls.Length)
        {
            int earlier = 0;
            while (earlier < index)
            {
                if (controls[earlier].Kind == controls[index].Kind)
                {
                    controls[index].Valid = false;
                    valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.DuplicateControlDependency,
                        "The ECN control group '"
                            + controls[index].Kind.ToString()
                            + "' is specified more than once.",
                        controls[index].Span);
                }
                else if (controls[index].Reference.Length > 0
                    && controls[index].Reference
                        == controls[earlier].Reference)
                {
                    if (controls[index].EncoderWrites
                        && controls[earlier].EncoderWrites)
                    {
                        controls[index].Valid = false;
                        valid = false;
                        this.AddDiagnostic(
                            EcnDiagnosticCodes.ConflictingControlWrite,
                            "More than one ECN control writes determinant '"
                                + controls[index].Reference + "'.",
                            controls[index].Span);
                    }
                    else if (controls[index].EncoderWrites
                        && controls[earlier].RequiresPriorValue)
                    {
                        controls[index].Valid = false;
                        valid = false;
                        this.AddDiagnostic(
                            EcnDiagnosticCodes.InvalidControlDependency,
                            "Determinant '" + controls[index].Reference
                                + "' is consumed before it is produced.",
                            controls[index].Span);
                    }
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < conditions.Length)
        {
            int controlIndex = 0;
            while (controlIndex < controls.Length)
            {
                if (conditions[index].Reference.Length > 0
                    && conditions[index].Reference
                        == controls[controlIndex].Reference
                    && controls[controlIndex].EncoderWrites)
                {
                    conditions[index].Valid = false;
                    valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.InvalidCondition,
                        "Condition determinant '"
                            + conditions[index].Reference
                            + "' is produced by the encoding selected by that condition.",
                        conditions[index].Span);
                }

                controlIndex = controlIndex + 1;
            }

            index = index + 1;
        }

        return valid;
    }

    internal EcnConditionDefinition[] ParseConditions(EcnClauseSyntax[] clauses)
    {
        System.Collections.Generic.List<EcnConditionDefinition> conditions =
            new System.Collections.Generic.List<EcnConditionDefinition>();
        int index = 0;
        while (index < clauses.Length)
        {
            if (clauses[index].Kind == EcnClauseKind.Condition)
            {
                conditions.Add(this.ParseConditionClause(
                    clauses[index],
                    conditions.Count));
            }

            index = index + 1;
        }

        return conditions.ToArray();
    }

    internal EcnConditionDefinition ParseConditionClause(
        EcnClauseSyntax clause,
        int order)
    {
        AsnToken[] tokens = clause.Tokens;
        EcnConditionDefinition condition = new EcnConditionDefinition
        {
            Name = clause.Name,
            All = clause.Name == "IF-ALL",
            Order = order,
            Tokens = tokens,
            Span = clause.Span,
            Valid = true
        };
        if (tokens.Length <= 1)
        {
            return condition;
        }

        int expressionStart = 1;
        int expressionEnd = tokens.Length;
        if (expressionEnd - expressionStart >= 2
            && ((tokens[expressionStart].Kind == AsnTokenKind.LeftBrace
                    && tokens[expressionEnd - 1].Kind
                        == AsnTokenKind.RightBrace)
                || (tokens[expressionStart].Kind
                        == AsnTokenKind.LeftParenthesis
                    && tokens[expressionEnd - 1].Kind
                        == AsnTokenKind.RightParenthesis)))
        {
            expressionStart = expressionStart + 1;
            expressionEnd = expressionEnd - 1;
        }

        condition.Expression = this.JoinCompact(
            tokens,
            expressionStart,
            expressionEnd - expressionStart);
        int comparison = this.FindComparisonInRange(
            tokens,
            expressionStart,
            expressionEnd);
        if (comparison < 0)
        {
            condition.Reference = condition.Expression;
            return condition;
        }

        int comparisonLength = this.GetComparisonLength(tokens, comparison);
        condition.Reference = this.JoinCompact(
            tokens,
            expressionStart,
            comparison - expressionStart);
        condition.Comparison = this.JoinCompact(
            tokens,
            comparison,
            comparisonLength);
        int valueStart = comparison + comparisonLength;
        condition.Value = this.JoinCompact(
            tokens,
            valueStart,
            expressionEnd - valueStart);
        return condition;
    }

    internal int FindComparisonInRange(
        AsnToken[] tokens,
        int start,
        int end)
    {
        int index = start;
        while (index < end)
        {
            AsnTokenKind kind = tokens[index].Kind;
            if (kind == AsnTokenKind.LessThan
                || kind == AsnTokenKind.GreaterThan
                || kind == AsnTokenKind.ExclamationMark
                || tokens[index].Text == "=")
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int GetComparisonLength(AsnToken[] tokens, int index)
    {
        return index + 1 < tokens.Length
                && tokens[index + 1].Text == "="
            ? 2
            : 1;
    }

    internal void ValidateCondition(EcnConditionDefinition condition)
    {
        if (condition.Expression.Length == 0
            || condition.Reference.Length == 0
            || (condition.Comparison.Length > 0
                && condition.Value.Length == 0))
        {
            condition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidCondition,
                "An ECN condition requires an operand and a complete comparison value when a comparison operator is present.",
                condition.Span);
        }
    }

    internal EcnConstructorDefinition CopyConstructor(
        EcnConstructorDefinition source)
    {
        EcnConstructorDefinition copy = new EcnConstructorDefinition
        {
            Kind = source.Kind,
            Structure = source.Structure,
            Fields = this.CopyFields(source.Fields),
            ControlDependencies = new EcnControlDependency[
                source.ControlDependencies.Length],
            Conditions = new EcnConditionDefinition[source.Conditions.Length],
            Valid = source.Valid
        };
        int index = 0;
        while (index < source.ControlDependencies.Length)
        {
            copy.ControlDependencies[index] = source.ControlDependencies[index];
            index = index + 1;
        }

        index = 0;
        while (index < source.Conditions.Length)
        {
            copy.Conditions[index] = source.Conditions[index];
            index = index + 1;
        }

        return copy;
    }

    internal EcnEncodingField[] CopyFields(EcnEncodingField[] fields)
    {
        EcnEncodingField[] copy = new EcnEncodingField[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            copy[index] = fields[index];
            index = index + 1;
        }

        return copy;
    }

    internal int FindTextInRange(
        AsnToken[] tokens,
        int start,
        int end,
        string text)
    {
        int index = start;
        while (index < end)
        {
            if (tokens[index].Text == text)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string JoinCompact(AsnToken[] tokens, int start, int count)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = start;
        int end = start + count;
        while (index < end)
        {
            builder.Append(tokens[index].Text);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal AsnToken[] CopyTokenRange(
        AsnToken[] tokens,
        int start,
        int count)
    {
        AsnToken[] copy = new AsnToken[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = tokens[start + index];
            index = index + 1;
        }

        return copy;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken[] tokens)
    {
        if (tokens.Length == 0)
        {
            return null;
        }

        AsnSourceSpan? first = tokens[0].Span;
        AsnSourceSpan? last = tokens[tokens.Length - 1].Span;
        if (first == null || last == null)
        {
            return first;
        }

        return new AsnSourceSpan
        {
            Start = first.Start,
            End = last.End
        };
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
