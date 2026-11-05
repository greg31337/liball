namespace asn1;

internal class EcnConversionCompiler
{
    internal DiagnosticBag Diagnostics;
    internal EcnSymbolTable[] Tables = [];
    internal bool Success = true;

    internal EcnConversionCompiler(DiagnosticBag diagnostics)
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

        this.Tables = tables;
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
                    this.ResolveConversion(symbol.ObjectDefinition, tables[tableIndex]);
                }

                objectIndex = objectIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void ResolveConversion(
        EcnEncodingObjectDefinition definition,
        EcnSymbolTable table)
    {
        if (definition.ConversionResolutionState == 2)
        {
            return;
        }

        if (definition.ConversionResolutionState == 1)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.ConversionCycle,
                "The encoding object '" + definition.Name
                    + "' has a cyclic conversion definition.",
                definition.Span);
            return;
        }

        definition.ConversionResolutionState = 1;
        if (definition.ReferenceDefinition)
        {
            EcnEncodingObjectDefinition? referenced =
                definition.ReferencedObjectSymbol == null
                    ? null
                    : definition.ReferencedObjectSymbol.ObjectDefinition;
            if (referenced != null)
            {
                EcnSymbolTable referencedTable = this.FindTableForSymbol(
                    referenced.Symbol,
                    table);
                this.ResolveConversion(referenced, referencedTable);
                definition.Conversion = this.CopyConversion(referenced.Conversion);
                if (!referenced.Valid)
                {
                    definition.Valid = false;
                }
            }
        }
        else
        {
            this.CompileLocalConversion(definition, table);
        }

        definition.ConversionResolutionState = 2;
    }

    internal void CompileLocalConversion(
        EcnEncodingObjectDefinition definition,
        EcnSymbolTable table)
    {
        AsnToken[] tokens = definition.SourceTokens;
        EcnConversionDefinition conversion = new EcnConversionDefinition();
        conversion.Replacements = this.ParseReplacements(
            definition,
            table,
            tokens);
        conversion.OpenTypeMapping = this.ParseOpenTypeMapping(
            definition,
            table,
            tokens);
        conversion.TransformPipelines = this.ParseTransformPipelines(
            definition,
            table,
            tokens);
        conversion.ContainedValueMapping = this.CreateContainedMapping(
            definition,
            conversion.Replacements);
        conversion.Valid = this.ValidateConversion(conversion);
        conversion.Reversible = this.IsConversionReversible(conversion);
        definition.Conversion = conversion;
        if (!conversion.Valid)
        {
            definition.Valid = false;
        }
    }

    internal EcnReplacementDefinition[] ParseReplacements(
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table,
        AsnToken[] tokens)
    {
        System.Collections.Generic.List<EcnReplacementDefinition> result =
            new System.Collections.Generic.List<EcnReplacementDefinition>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (this.TokenIs(tokens[index], "REPLACE"))
            {
                int end = this.FindReplacementEnd(tokens, index);
                EcnReplacementDefinition replacement = this.ParseReplacement(
                    owner,
                    table,
                    tokens,
                    index,
                    end,
                    false);
                result.Add(replacement);
                int secondary = this.FindSequence(
                    tokens,
                    index,
                    end,
                    "AND",
                    "OPTIONALS",
                    "WITH");
                if (secondary >= 0)
                {
                    result.Add(this.ParseReplacement(
                        owner,
                        table,
                        tokens,
                        secondary + 1,
                        end,
                        true));
                }

                index = end;
            }
            else
            {
                index = index + 1;
            }
        }

        return result.ToArray();
    }

    internal EcnReplacementDefinition ParseReplacement(
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table,
        AsnToken[] tokens,
        int start,
        int end,
        bool secondary)
    {
        EcnReplacementDefinition replacement = new EcnReplacementDefinition();
        replacement.Secondary = secondary;
        replacement.Scope = this.ReadReplacementScope(tokens, start, end);
        replacement.Span = tokens[start].Span;
        replacement.Tokens = this.CopyTokenRange(tokens, start, end - start);
        int withIndex = this.FindToken(tokens, start, end, "WITH");
        if (withIndex < 0 || withIndex + 1 >= end)
        {
            replacement.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidReplacement,
                "A replacement must name a replacement encoding class after WITH.",
                replacement.Span);
            return replacement;
        }

        int classEnd = this.FindReferenceEnd(tokens, withIndex + 1, end);
        replacement.ReplacementClassName = this.JoinCompact(
            tokens,
            withIndex + 1,
            classEnd - withIndex - 1);
        replacement.ReplacementClassSymbol = this.FindClassReference(
            table,
            replacement.ReplacementClassName);
        replacement.ReplacementBuiltIn = EcnBuiltInClassCatalog.FindClass(
            replacement.ReplacementClassName) != null;

        int encodedBy = this.FindSequence(
            tokens,
            classEnd,
            end,
            "ENCODED",
            "BY",
            "");
        if (encodedBy >= 0 && encodedBy + 2 < end)
        {
            int objectEnd = this.FindReferenceEnd(tokens, encodedBy + 2, end);
            replacement.EncodingObjectName = this.JoinCompact(
                tokens,
                encodedBy + 2,
                objectEnd - encodedBy - 2);
            replacement.EncodingObjectSymbol = this.FindReference(
                table,
                replacement.EncodingObjectName,
                EcnSymbolKind.EncodingObject);
        }

        int insertAt = this.FindSequence(
            tokens,
            classEnd,
            end,
            "INSERT",
            "AT",
            "HEAD");
        if (insertAt >= 0 && insertAt + 3 < end)
        {
            int headEnd = this.FindReferenceEnd(tokens, insertAt + 3, end);
            replacement.HeadStructureName = this.JoinCompact(
                tokens,
                insertAt + 3,
                headEnd - insertAt - 3);
            replacement.HeadStructureSymbol = this.FindClassReference(
                table,
                replacement.HeadStructureName);
        }

        replacement.Valid = this.ValidateReplacement(owner, replacement);
        return replacement;
    }

    internal bool ValidateReplacement(
        EcnEncodingObjectDefinition owner,
        EcnReplacementDefinition replacement)
    {
        bool valid = true;
        if (replacement.ReplacementClassSymbol == null
            && !replacement.ReplacementBuiltIn)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.UnknownReplacementTarget,
                "The replacement encoding class '"
                    + replacement.ReplacementClassName
                    + "' could not be resolved.",
                replacement.Span);
            valid = false;
        }

        if (replacement.EncodingObjectName.Length > 0
            && replacement.EncodingObjectSymbol == null)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.UnknownReplacementTarget,
                "The replacement encoding object '"
                    + replacement.EncodingObjectName
                    + "' could not be resolved.",
                replacement.Span);
            valid = false;
        }

        if (replacement.EncodingObjectSymbol != null
            && replacement.ReplacementClassSymbol != null
            && !EcnEncodingObjectSelector.AreClassesCompatible(
                replacement.EncodingObjectSymbol,
                replacement.ReplacementClassSymbol))
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.IncompatibleReplacement,
                "The replacement encoding object is not governed by the replacement class.",
                replacement.Span);
            valid = false;
        }

        if (!this.ReplacementScopeIsAllowed(
                owner.Symbol == null
                    ? EcnClassCategory.Unknown
                    : owner.Symbol.ClassCategory,
                replacement.Scope))
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.IncompatibleReplacement,
                "The requested replacement scope is not valid for this encoding class category.",
                replacement.Span);
            valid = false;
        }

        if (replacement.HeadStructureName.Length > 0
            && (replacement.EncodingObjectName.Length == 0
                || replacement.HeadStructureSymbol == null
                || this.SymbolIsParameterized(replacement.HeadStructureSymbol)))
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidReplacement,
                "INSERT AT HEAD requires a resolved, non-parameterized head structure and ENCODED BY object.",
                replacement.Span);
            valid = false;
        }

        if (this.HasConflictingProperties(owner.Properties))
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidReplacement,
                "A replacement cannot be combined with direct encoding properties in the same object.",
                replacement.Span);
            valid = false;
        }

        return valid;
    }

    internal EcnReplacementScope ReadReplacementScope(
        AsnToken[] tokens,
        int start,
        int end)
    {
        if (this.FindToken(tokens, start, end, "NON-OPTIONALS") >= 0)
        {
            return EcnReplacementScope.NonOptionalComponents;
        }

        if (this.FindToken(tokens, start, end, "OPTIONALS") >= 0)
        {
            return EcnReplacementScope.OptionalComponents;
        }

        if (this.FindSequence(tokens, start, end, "ALL", "COMPONENTS", "") >= 0)
        {
            return EcnReplacementScope.AllComponents;
        }

        if (this.FindToken(tokens, start, end, "COMPONENT") >= 0)
        {
            return EcnReplacementScope.Component;
        }

        return EcnReplacementScope.Structure;
    }

    internal bool ReplacementScopeIsAllowed(
        EcnClassCategory category,
        EcnReplacementScope scope)
    {
        if (category == EcnClassCategory.Concatenation)
        {
            return true;
        }

        if (category == EcnClassCategory.Optionality
            || category == EcnClassCategory.EncodingProcedure)
        {
            return scope == EcnReplacementScope.Structure
                || scope == EcnReplacementScope.Component
                || scope == EcnReplacementScope.AllComponents;
        }

        return scope == EcnReplacementScope.Structure;
    }

    internal bool HasConflictingProperties(EcnEncodingProperty[] properties)
    {
        int index = 0;
        while (index < properties.Length)
        {
            if (properties[index].Kind != EcnEncodingPropertyKind.RepetitionEncoding
                && properties[index].Kind != EcnEncodingPropertyKind.OptionalEncoding)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal EcnContainedValueMapping? CreateContainedMapping(
        EcnEncodingObjectDefinition owner,
        EcnReplacementDefinition[] replacements)
    {
        EcnClassCategory category = owner.Symbol == null
            ? EcnClassCategory.Unknown
            : owner.Symbol.ClassCategory;
        bool container = category == EcnClassCategory.BitString
            || category == EcnClassCategory.OctetString
            || category == EcnClassCategory.CharacterString
            || category == EcnClassCategory.Repetition;
        if (!container)
        {
            return null;
        }

        int index = 0;
        while (index < replacements.Length)
        {
            EcnReplacementDefinition replacement = replacements[index];
            if (replacement.Scope == EcnReplacementScope.Structure)
            {
                EcnSymbol? parameter = this.FindElementParameter(
                    replacement.ReplacementClassSymbol);
                EcnContainedValueMapping mapping = new EcnContainedValueMapping
                {
                    ContainerCategory = category,
                    ElementParameter = parameter,
                    Replacement = replacement,
                    Reversible = replacement.Valid && parameter != null,
                    Valid = replacement.Valid && parameter != null,
                    Span = replacement.Span
                };
                if (parameter == null)
                {
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.InvalidContainedMapping,
                        "A contained-value replacement structure must declare an encoding-class element parameter.",
                        replacement.Span);
                }

                return mapping;
            }

            index = index + 1;
        }

        return null;
    }

    internal EcnSymbol? FindElementParameter(EcnSymbol? classSymbol)
    {
        if (classSymbol == null || classSymbol.Scope == null)
        {
            return null;
        }

        int index = 0;
        while (index < classSymbol.Scope.Symbols.Length)
        {
            EcnSymbol parameter = classSymbol.Scope.Symbols[index];
            if (parameter.ParameterKind == EcnParameterKind.EncodingClass)
            {
                return parameter;
            }

            index = index + 1;
        }

        return null;
    }

    internal EcnOpenTypeMapping? ParseOpenTypeMapping(
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table,
        AsnToken[] tokens)
    {
        int encodedWith = this.FindSequence(
            tokens,
            0,
            tokens.Length,
            "ENCODED",
            "WITH",
            "");
        int unknownIs = this.FindSequence(
            tokens,
            0,
            tokens.Length,
            "UNKNOWN",
            "IS",
            "");
        if (encodedWith < 0 && unknownIs < 0)
        {
            return null;
        }

        EcnOpenTypeMapping mapping = new EcnOpenTypeMapping();
        mapping.Span = encodedWith >= 0
            ? tokens[encodedWith].Span
            : tokens[unknownIs].Span;
        bool valid = owner.Symbol != null
            && owner.Symbol.ClassCategory == EcnClassCategory.OpenType;
        if (!valid)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidOpenTypeMapping,
                "ENCODED WITH and UNKNOWN IS mappings are valid only for open types.",
                mapping.Span);
        }

        if (encodedWith >= 0 && (unknownIs < 0 || encodedWith < unknownIs))
        {
            int end = this.FindReferenceEnd(tokens, encodedWith + 2, tokens.Length);
            mapping.KnownEncodingsName = this.JoinCompact(
                tokens,
                encodedWith + 2,
                end - encodedWith - 2);
            mapping.KnownEncodingsBuiltIn = EcnEncodingObjectSetCatalog.IsBuiltIn(
                mapping.KnownEncodingsName);
            mapping.KnownPredeclaredEncodings =
                EcnEncodingObjectSetCatalog.FindSet(
                    mapping.KnownEncodingsName);
            mapping.KnownEncodingsSymbol = this.FindReference(
                table,
                mapping.KnownEncodingsName,
                EcnSymbolKind.EncodingObjectSet);
            if (!mapping.KnownEncodingsBuiltIn
                && mapping.KnownEncodingsSymbol == null)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidOpenTypeMapping,
                    "The known-value encoding set '" + mapping.KnownEncodingsName
                        + "' could not be resolved.",
                    mapping.Span);
                valid = false;
            }
        }

        if (unknownIs >= 0)
        {
            int structureEnd = this.FindReferenceEnd(
                tokens,
                unknownIs + 2,
                tokens.Length);
            mapping.UnknownStructureName = this.JoinCompact(
                tokens,
                unknownIs + 2,
                structureEnd - unknownIs - 2);
            mapping.UnknownStructureSymbol = this.FindClassReference(
                table,
                mapping.UnknownStructureName);
            if (mapping.UnknownStructureSymbol == null
                && EcnBuiltInClassCatalog.FindClass(
                    mapping.UnknownStructureName) == null)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidOpenTypeMapping,
                    "The unknown-value structure '"
                        + mapping.UnknownStructureName
                        + "' could not be resolved.",
                    mapping.Span);
                valid = false;
            }

            int unknownEncoding = this.FindSequence(
                tokens,
                structureEnd,
                tokens.Length,
                "ENCODED",
                "WITH",
                "");
            if (unknownEncoding < 0)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidOpenTypeMapping,
                    "UNKNOWN IS must identify the encoding set used for unknown values.",
                    mapping.Span);
                valid = false;
            }
            else
            {
                int setEnd = this.FindReferenceEnd(
                    tokens,
                    unknownEncoding + 2,
                    tokens.Length);
                mapping.UnknownEncodingsName = this.JoinCompact(
                    tokens,
                    unknownEncoding + 2,
                    setEnd - unknownEncoding - 2);
                mapping.UnknownEncodingsBuiltIn =
                    EcnEncodingObjectSetCatalog.IsBuiltIn(
                        mapping.UnknownEncodingsName);
                mapping.UnknownPredeclaredEncodings =
                    EcnEncodingObjectSetCatalog.FindSet(
                        mapping.UnknownEncodingsName);
                mapping.UnknownEncodingsSymbol = this.FindReference(
                    table,
                    mapping.UnknownEncodingsName,
                    EcnSymbolKind.EncodingObjectSet);
                if (!mapping.UnknownEncodingsBuiltIn
                    && mapping.UnknownEncodingsSymbol == null)
                {
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.InvalidOpenTypeMapping,
                        "The unknown-value encoding set '"
                            + mapping.UnknownEncodingsName
                            + "' could not be resolved.",
                        mapping.Span);
                    valid = false;
                }
            }
        }

        mapping.Valid = valid;
        return mapping;
    }

    internal EcnTransformPipeline[] ParseTransformPipelines(
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table,
        AsnToken[] tokens)
    {
        System.Collections.Generic.List<EcnTransformPipeline> result =
            new System.Collections.Generic.List<EcnTransformPipeline>();
        int index = 0;
        int order = 0;
        while (index < tokens.Length)
        {
            EcnTransformDirection direction = EcnTransformDirection.Value;
            bool found = false;
            if (this.TokenIs(tokens[index], "ENCODER-TRANSFORMS"))
            {
                direction = EcnTransformDirection.Encoder;
                found = true;
            }
            else if (this.TokenIs(tokens[index], "DECODER-TRANSFORMS"))
            {
                direction = EcnTransformDirection.Decoder;
                found = true;
            }
            else if (this.TokenIs(tokens[index], "TRANSFORMS"))
            {
                direction = index > 0 && this.TokenIs(tokens[index - 1], "MAPPING")
                    ? EcnTransformDirection.Mapping
                    : EcnTransformDirection.Value;
                found = true;
            }

            if (!found)
            {
                index = index + 1;
                continue;
            }

            int left = index + 1;
            while (left < tokens.Length && !this.TokenIs(tokens[left], "{"))
            {
                left = left + 1;
            }

            if (left >= tokens.Length)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidTransform,
                    "A transform pipeline must contain an ordered transform list.",
                    tokens[index].Span);
                index = index + 1;
                continue;
            }

            int right = this.FindMatchingBrace(tokens, left);
            if (right < 0)
            {
                right = tokens.Length - 1;
            }

            EcnTransformPipeline pipeline = this.ParseTransformPipeline(
                owner,
                table,
                tokens,
                index,
                left,
                right,
                direction,
                order);
            result.Add(pipeline);
            order = order + 1;
            index = right + 1;
        }

        return result.ToArray();
    }

    internal EcnTransformPipeline ParseTransformPipeline(
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table,
        AsnToken[] tokens,
        int keyword,
        int left,
        int right,
        EcnTransformDirection direction,
        int order)
    {
        EcnTransformPipeline pipeline = new EcnTransformPipeline
        {
            Direction = direction,
            Order = order,
            Span = tokens[keyword].Span,
            Tokens = this.CopyTokenRange(tokens, keyword, right - keyword + 1)
        };
        pipeline.Steps = this.ParseTransformSteps(table, tokens, left, right);
        pipeline.Valid = pipeline.Steps.Length > 0;
        if (pipeline.Steps.Length == 0)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidTransform,
                "A transform pipeline must contain at least one transform.",
                pipeline.Span);
        }
        else
        {
            pipeline.SourceKind = pipeline.Steps[0].SourceKind;
            pipeline.ResultKind = pipeline.Steps[pipeline.Steps.Length - 1].ResultKind;
        }

        int index = 0;
        pipeline.Reversible = true;
        while (index < pipeline.Steps.Length)
        {
            if (!pipeline.Steps[index].Valid)
            {
                pipeline.Valid = false;
            }
            if (!pipeline.Steps[index].Reversible)
            {
                pipeline.Reversible = false;
            }
            if (index > 0
                && pipeline.Steps[index - 1].ResultKind
                    != EcnTransformValueKind.Unknown
                && pipeline.Steps[index].SourceKind
                    != EcnTransformValueKind.Unknown
                && pipeline.Steps[index - 1].ResultKind
                    != pipeline.Steps[index].SourceKind)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.TransformCategoryMismatch,
                    "Adjacent transforms do not have compatible result and source categories.",
                    pipeline.Steps[index].Span);
                pipeline.Valid = false;
            }

            index = index + 1;
        }

        if (!this.ValidateTransformDirection(owner, pipeline))
        {
            pipeline.Valid = false;
        }
        if ((direction == EcnTransformDirection.Value
                || direction == EcnTransformDirection.Mapping
                || direction == EcnTransformDirection.Encoder)
            && !pipeline.Reversible)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.NonReversibleTransform,
                "This transform pipeline must be reversible for every applied abstract value.",
                pipeline.Span);
            pipeline.Valid = false;
        }

        return pipeline;
    }

    internal EcnTransformStep[] ParseTransformSteps(
        EcnSymbolTable table,
        AsnToken[] tokens,
        int left,
        int right)
    {
        System.Collections.Generic.List<EcnTransformStep> result =
            new System.Collections.Generic.List<EcnTransformStep>();
        int contentStart = left + 1;
        int contentEnd = right;
        if (contentStart < contentEnd
            && this.TokenIs(tokens[contentStart], "{")
            && this.FindMatchingBrace(tokens, contentStart) == right - 1)
        {
            contentStart = contentStart + 1;
            contentEnd = contentEnd - 1;
        }

        int start = contentStart;
        int depth = 0;
        int order = 0;
        int index = contentStart;
        while (index <= contentEnd)
        {
            bool boundary = index == contentEnd;
            if (!boundary)
            {
                if (this.TokenIs(tokens[index], "{"))
                {
                    depth = depth + 1;
                }
                else if (this.TokenIs(tokens[index], "}"))
                {
                    depth = depth - 1;
                }
                else if (this.TokenIs(tokens[index], ",") && depth == 0)
                {
                    boundary = true;
                }
            }

            if (boundary)
            {
                int end = index;
                if (end > start)
                {
                    result.Add(this.ParseTransformStep(
                        table,
                        tokens,
                        start,
                        end,
                        order));
                    order = order + 1;
                }
                start = index + 1;
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal EcnTransformStep ParseTransformStep(
        EcnSymbolTable table,
        AsnToken[] tokens,
        int start,
        int end,
        int order)
    {
        while (start < end && this.TokenIs(tokens[start], "{"))
        {
            start = start + 1;
        }
        while (end > start && this.TokenIs(tokens[end - 1], "}"))
        {
            end = end - 1;
        }

        EcnTransformStep step = new EcnTransformStep
        {
            Order = order,
            Span = start < end ? tokens[start].Span : null,
            Tokens = this.CopyTokenRange(tokens, start, end - start)
        };
        if (start >= end)
        {
            step.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidTransform,
                "An empty transform step is not valid.",
                step.Span);
            return step;
        }

        string first = tokens[start].Text;
        int separator = first.IndexOf("-TO-", System.StringComparison.Ordinal);
        if (separator >= 0)
        {
            step.Inline = true;
            step.Signature = first;
            step.SourceKind = this.ParseTransformKind(first.Substring(0, separator));
            step.ResultKind = this.ParseTransformKind(first.Substring(separator + 4));
            if (start + 1 < end)
            {
                step.Operation = tokens[start + 1].Text;
                int argumentStart = start + 2;
                if (argumentStart < end && this.TokenIs(tokens[argumentStart], ":"))
                {
                    argumentStart = argumentStart + 1;
                }
                step.Argument = this.JoinCompact(
                    tokens,
                    argumentStart,
                    end - argumentStart);
            }

            step.Valid = step.SourceKind != EcnTransformValueKind.Unknown
                && step.ResultKind != EcnTransformValueKind.Unknown
                && step.Operation.Length > 0;
            step.RequiresDomainValidation = this.OperationNeedsDomainValidation(
                step.Operation);
            step.Reversible = this.OperationIsReversible(
                step.Operation,
                step.Argument);
        }
        else
        {
            step.ReferenceName = this.JoinCompact(tokens, start, end - start);
            step.ReferenceSymbol = this.FindReference(
                table,
                step.ReferenceName,
                EcnSymbolKind.EncodingObject);
            step.Valid = step.ReferenceSymbol != null
                && step.ReferenceSymbol.ClassCategory
                    == EcnClassCategory.EncodingProcedure;
            step.Reversible = step.Valid;
            step.RequiresDomainValidation = step.Valid;
        }

        if (!step.Valid)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidTransform,
                "The transform step is malformed or does not resolve to a transform encoding object.",
                step.Span);
        }

        return step;
    }

    internal EcnTransformValueKind ParseTransformKind(string text)
    {
        if (text == "BOOL" || text == "BOOLEAN")
        {
            return EcnTransformValueKind.Boolean;
        }
        if (text == "INT" || text == "INTEGER")
        {
            return EcnTransformValueKind.Integer;
        }
        if (text == "REAL")
        {
            return EcnTransformValueKind.Real;
        }
        if (text == "BITS" || text == "BIT-STRING")
        {
            return EcnTransformValueKind.BitString;
        }
        if (text == "OCTETS" || text == "OCTET-STRING")
        {
            return EcnTransformValueKind.OctetString;
        }
        if (text == "CHARS" || text == "CHARACTER-STRING")
        {
            return EcnTransformValueKind.CharacterString;
        }
        if (text == "OID" || text == "OBJECT-IDENTIFIER")
        {
            return EcnTransformValueKind.ObjectIdentifier;
        }
        if (text == "STRUCTURE")
        {
            return EcnTransformValueKind.Structure;
        }
        if (text == "OPEN-TYPE")
        {
            return EcnTransformValueKind.OpenType;
        }

        return EcnTransformValueKind.Unknown;
    }

    internal bool OperationNeedsDomainValidation(string operation)
    {
        return this.TextEquals(operation, "divide")
            || this.TextEquals(operation, "multiply")
            || this.TextEquals(operation, "standard");
    }

    internal bool OperationIsReversible(string operation, string argument)
    {
        if (this.TextEquals(operation, "TRUE-IS")
            || this.TextEquals(operation, "FALSE-IS")
            || this.TextEquals(operation, "modulo")
            || this.TextEquals(operation, "truncate")
            || this.TextEquals(operation, "absolute")
            || this.TextEquals(operation, "clamp")
            || this.TextEquals(operation, "hash"))
        {
            return false;
        }

        if ((this.TextEquals(operation, "divide")
                || this.TextEquals(operation, "multiply"))
            && this.IsZeroArgument(argument))
        {
            return false;
        }

        return true;
    }

    internal bool ValidateTransformDirection(
        EcnEncodingObjectDefinition owner,
        EcnTransformPipeline pipeline)
    {
        EcnDeterminantRelationship? relationship = this.FindNearestRelationship(
            owner.Determinants,
            pipeline.Span);
        if (pipeline.Direction == EcnTransformDirection.Encoder)
        {
            if (relationship == null
                || (relationship.Mode != EcnDeterminationMode.FieldToBeSet
                    && relationship.Mode != EcnDeterminationMode.FlagToBeSet))
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidTransformDirection,
                    "Encoder transforms require a field-to-be-set or flag-to-be-set control dependency.",
                    pipeline.Span);
                return false;
            }

            EcnTransformValueKind expected = this.ExpectedControlKind(relationship);
            if (pipeline.SourceKind != EcnTransformValueKind.Unknown
                && expected != pipeline.SourceKind)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.TransformCategoryMismatch,
                    "The encoder transform source category does not match its control value.",
                    pipeline.Span);
                return false;
            }
        }
        else if (pipeline.Direction == EcnTransformDirection.Decoder)
        {
            if (relationship == null
                || (relationship.Mode != EcnDeterminationMode.FieldToBeUsed
                    && relationship.Mode != EcnDeterminationMode.FlagToBeUsed))
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidTransformDirection,
                    "Decoder transforms require a field-to-be-used or flag-to-be-used control dependency.",
                    pipeline.Span);
                return false;
            }

            EcnTransformValueKind expected = this.ExpectedControlKind(relationship);
            if (pipeline.ResultKind != EcnTransformValueKind.Unknown
                && expected != pipeline.ResultKind)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.TransformCategoryMismatch,
                    "The decoder transform result category does not match its control value.",
                    pipeline.Span);
                return false;
            }
        }
        else if (pipeline.Direction == EcnTransformDirection.Mapping)
        {
            EcnTransformValueKind expected = this.CategoryToTransformKind(
                owner.Symbol == null
                    ? EcnClassCategory.Unknown
                    : owner.Symbol.ClassCategory);
            if (expected != EcnTransformValueKind.Unknown
                && pipeline.SourceKind != EcnTransformValueKind.Unknown
                && expected != pipeline.SourceKind)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.TransformCategoryMismatch,
                    "The mapping transform source category does not match the governed encoding class.",
                    pipeline.Span);
                return false;
            }
        }

        return true;
    }

    internal EcnDeterminantRelationship? FindNearestRelationship(
        EcnDeterminantRelationship[] relationships,
        AsnSourceSpan? span)
    {
        if (relationships.Length == 0)
        {
            return null;
        }

        int offset = span == null ? 0 : span.Start.Offset;
        EcnDeterminantRelationship? nearest = null;
        int distance = 2147483647;
        int index = 0;
        while (index < relationships.Length)
        {
            AsnSourceSpan? candidateSpan = relationships[index].Span;
            int candidate = candidateSpan == null ? 0 : candidateSpan.Start.Offset;
            int currentDistance = offset >= candidate
                ? offset - candidate
                : candidate - offset;
            if (currentDistance < distance)
            {
                nearest = relationships[index];
                distance = currentDistance;
            }
            index = index + 1;
        }

        return nearest;
    }

    internal EcnTransformValueKind ExpectedControlKind(
        EcnDeterminantRelationship relationship)
    {
        return relationship.ControlKind == EcnControlDependencyKind.Presence
            ? EcnTransformValueKind.Boolean
            : EcnTransformValueKind.Integer;
    }

    internal EcnTransformValueKind CategoryToTransformKind(
        EcnClassCategory category)
    {
        switch (category)
        {
            case EcnClassCategory.Boolean:
                return EcnTransformValueKind.Boolean;
            case EcnClassCategory.Integer:
                return EcnTransformValueKind.Integer;
            case EcnClassCategory.Real:
                return EcnTransformValueKind.Real;
            case EcnClassCategory.BitString:
                return EcnTransformValueKind.BitString;
            case EcnClassCategory.OctetString:
                return EcnTransformValueKind.OctetString;
            case EcnClassCategory.CharacterString:
                return EcnTransformValueKind.CharacterString;
            case EcnClassCategory.ObjectIdentifier:
                return EcnTransformValueKind.ObjectIdentifier;
            case EcnClassCategory.OpenType:
                return EcnTransformValueKind.OpenType;
            case EcnClassCategory.Alternatives:
            case EcnClassCategory.Concatenation:
            case EcnClassCategory.Repetition:
                return EcnTransformValueKind.Structure;
            default:
                return EcnTransformValueKind.Unknown;
        }
    }

    internal bool ValidateConversion(EcnConversionDefinition conversion)
    {
        int index = 0;
        while (index < conversion.Replacements.Length)
        {
            if (!conversion.Replacements[index].Valid)
            {
                return false;
            }
            index = index + 1;
        }
        if (conversion.ContainedValueMapping != null
            && !conversion.ContainedValueMapping.Valid)
        {
            return false;
        }
        if (conversion.OpenTypeMapping != null
            && !conversion.OpenTypeMapping.Valid)
        {
            return false;
        }
        index = 0;
        while (index < conversion.TransformPipelines.Length)
        {
            if (!conversion.TransformPipelines[index].Valid)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsConversionReversible(EcnConversionDefinition conversion)
    {
        if (!conversion.Valid)
        {
            return false;
        }
        if (conversion.ContainedValueMapping != null
            && !conversion.ContainedValueMapping.Reversible)
        {
            return false;
        }
        int index = 0;
        while (index < conversion.TransformPipelines.Length)
        {
            EcnTransformPipeline pipeline = conversion.TransformPipelines[index];
            if (pipeline.Direction != EcnTransformDirection.Decoder
                && !pipeline.Reversible)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal EcnConversionDefinition? CopyConversion(
        EcnConversionDefinition? source)
    {
        if (source == null)
        {
            return null;
        }
        EcnReplacementDefinition[] replacements =
            new EcnReplacementDefinition[source.Replacements.Length];
        int index = 0;
        while (index < replacements.Length)
        {
            replacements[index] = source.Replacements[index];
            index = index + 1;
        }
        EcnTransformPipeline[] pipelines =
            new EcnTransformPipeline[source.TransformPipelines.Length];
        index = 0;
        while (index < pipelines.Length)
        {
            pipelines[index] = source.TransformPipelines[index];
            index = index + 1;
        }
        return new EcnConversionDefinition
        {
            Replacements = replacements,
            ContainedValueMapping = source.ContainedValueMapping,
            OpenTypeMapping = source.OpenTypeMapping,
            TransformPipelines = pipelines,
            Reversible = source.Reversible,
            Valid = source.Valid
        };
    }

    internal int FindReplacementEnd(AsnToken[] tokens, int start)
    {
        int depth = 0;
        int index = start + 1;
        while (index < tokens.Length)
        {
            if (this.TokenIs(tokens[index], "{"))
            {
                depth = depth + 1;
            }
            else if (this.TokenIs(tokens[index], "}"))
            {
                if (depth == 0)
                {
                    return index;
                }
                depth = depth - 1;
            }
            index = index + 1;
        }
        return tokens.Length;
    }

    internal int FindMatchingBrace(AsnToken[] tokens, int left)
    {
        int depth = 0;
        int index = left;
        while (index < tokens.Length)
        {
            if (this.TokenIs(tokens[index], "{"))
            {
                depth = depth + 1;
            }
            else if (this.TokenIs(tokens[index], "}"))
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index;
                }
            }
            index = index + 1;
        }
        return -1;
    }

    internal int FindToken(
        AsnToken[] tokens,
        int start,
        int end,
        string text)
    {
        int index = start;
        while (index < end)
        {
            if (this.TokenIs(tokens[index], text))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal int FindSequence(
        AsnToken[] tokens,
        int start,
        int end,
        string first,
        string second,
        string third)
    {
        int index = start;
        while (index < end)
        {
            bool matches = this.TokenIs(tokens[index], first)
                && index + 1 < end
                && this.TokenIs(tokens[index + 1], second);
            if (matches && third.Length > 0)
            {
                matches = index + 2 < end
                    && this.TokenIs(tokens[index + 2], third);
            }
            if (matches)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal int FindReferenceEnd(AsnToken[] tokens, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            string text = tokens[index].Text;
            if (index > start
                && text != "."
                && text != "#"
                && tokens[index - 1].Text != ".")
            {
                break;
            }
            index = index + 1;
        }
        return index;
    }

    internal EcnSymbol? FindClassReference(
        EcnSymbolTable table,
        string reference)
    {
        EcnSymbol? symbol = this.FindReference(
            table,
            reference,
            EcnSymbolKind.EncodingClass);
        return symbol == null
            ? this.FindReference(
                table,
                reference,
                EcnSymbolKind.EncodingStructure)
            : symbol;
    }

    internal EcnSymbol? FindReference(
        EcnSymbolTable table,
        string reference,
        EcnSymbolKind kind)
    {
        int dot = reference.IndexOf(".", System.StringComparison.Ordinal);
        string moduleName = dot < 0 ? "" : reference.Substring(0, dot);
        string name = dot < 0 ? reference : reference.Substring(dot + 1);
        EcnSymbol[] symbols = table.GetCategorySymbols(kind);
        EcnSymbol? found = null;
        int matches = 0;
        int index = 0;
        while (index < symbols.Length)
        {
            if (symbols[index].Name == name
                && (moduleName.Length == 0
                    || symbols[index].SourceModuleName == moduleName))
            {
                found = symbols[index];
                matches = matches + 1;
            }
            index = index + 1;
        }
        return matches == 1 ? found : null;
    }

    internal bool SymbolIsParameterized(EcnSymbol symbol)
    {
        return symbol.Scope != null && symbol.Scope.Symbols.Length > 0;
    }

    internal EcnSymbolTable FindTableForSymbol(
        EcnSymbol? symbol,
        EcnSymbolTable fallback)
    {
        if (symbol == null)
        {
            return fallback;
        }
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].Symbols.Length)
            {
                if (this.Tables[tableIndex].Symbols[symbolIndex] == symbol)
                {
                    return this.Tables[tableIndex];
                }
                symbolIndex = symbolIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
        return fallback;
    }

    internal AsnToken[] CopyTokenRange(
        AsnToken[] tokens,
        int start,
        int count)
    {
        AsnToken[] result = new AsnToken[count];
        int index = 0;
        while (index < count)
        {
            result[index] = tokens[start + index];
            index = index + 1;
        }
        return result;
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

    internal bool TokenIs(AsnToken token, string text)
    {
        return token.Text == text;
    }

    internal bool TextEquals(string left, string right)
    {
        return string.Equals(
            left,
            right,
            System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsZeroArgument(string argument)
    {
        return argument == "0" || argument == "+0" || argument == "-0";
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
