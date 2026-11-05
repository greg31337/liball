namespace asn1;

internal class EcnEncodingObjectCompiler
{
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal EcnEncodingObjectCompiler(DiagnosticBag diagnostics)
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
            EcnSymbolTable table = tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.LocalSymbols.Length)
            {
                EcnSymbol symbol = table.LocalSymbols[symbolIndex];
                if (symbol.Kind == EcnSymbolKind.EncodingObject)
                {
                    symbol.ObjectDefinition = this.CompileObject(symbol, table);
                }
                else if (symbol.Kind == EcnSymbolKind.EncodingObjectSet)
                {
                    symbol.ObjectSetDefinition = this.CompileObjectSet(
                        symbol,
                        table);
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int importIndex = 0;
            while (importIndex < tables[tableIndex].ImportedSymbols.Length)
            {
                EcnSymbol imported = tables[tableIndex].ImportedSymbols[importIndex];
                if (imported.ImportedTarget != null)
                {
                    imported.ObjectDefinition =
                        imported.ImportedTarget.ObjectDefinition;
                    imported.ObjectSetDefinition =
                        imported.ImportedTarget.ObjectSetDefinition;
                }

                importIndex = importIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int objectIndex = 0;
            while (objectIndex < tables[tableIndex].EncodingObjects.Length)
            {
                EcnSymbol symbol = tables[tableIndex].EncodingObjects[objectIndex];
                if (!symbol.Imported && symbol.ObjectDefinition != null)
                {
                    this.ResolveObjectDefinition(
                        symbol.ObjectDefinition,
                        tables[tableIndex]);
                }

                objectIndex = objectIndex + 1;
            }

            int setIndex = 0;
            while (setIndex < tables[tableIndex].EncodingObjectSets.Length)
            {
                EcnSymbol symbol = tables[tableIndex].EncodingObjectSets[setIndex];
                if (!symbol.Imported && symbol.ObjectSetDefinition != null)
                {
                    this.ResolveCompletion(
                        symbol.ObjectSetDefinition,
                        tables[tableIndex]);
                    this.EvaluateObjectSet(symbol.ObjectSetDefinition);
                }

                setIndex = setIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal EcnEncodingObjectDefinition CompileObject(
        EcnSymbol symbol,
        EcnSymbolTable table)
    {
        EcnAssignmentSyntax? assignment = symbol.Assignment;
        EcnEncodingObjectDefinition definition =
            new EcnEncodingObjectDefinition
            {
                Name = symbol.Name,
                GovernorName = symbol.GovernorName,
                GovernorSymbol = symbol.GovernorSymbol,
                GovernorBuiltIn = symbol.GovernorBuiltIn,
                Symbol = symbol,
                Parameterized = assignment != null
                    && assignment.Parameters.Length > 0,
                Valid = !symbol.BindingFailed,
                Span = symbol.Span
            };
        if (assignment == null)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidObjectDefinition,
                "The encoding object assignment has no syntax body.",
                symbol.Span);
            return definition;
        }

        definition.SourceTokens = assignment.BodyTokens;
        definition.GovernorActualParameters = this.BindGovernorActuals(
            assignment,
            symbol,
            table);
        definition.Settings = this.ParseObjectSettings(assignment.BodyTokens);
        definition.OrderedLists = this.ParseOrderedLists(
            assignment.Clauses,
            symbol,
            table);
        if (!this.ActualParametersAreValid(
                definition.GovernorActualParameters,
                symbol.GovernorSymbol))
        {
            definition.Valid = false;
        }

        int listIndex = 0;
        while (listIndex < definition.OrderedLists.Length)
        {
            int itemIndex = 0;
            while (itemIndex < definition.OrderedLists[listIndex].Items.Length)
            {
                if (!definition.OrderedLists[listIndex].Items[itemIndex].Valid)
                {
                    definition.Valid = false;
                }

                itemIndex = itemIndex + 1;
            }

            listIndex = listIndex + 1;
        }

        return definition;
    }

    internal EcnEncodingObjectSetDefinition CompileObjectSet(
        EcnSymbol symbol,
        EcnSymbolTable table)
    {
        EcnAssignmentSyntax? assignment = symbol.Assignment;
        EcnEncodingObjectSetDefinition definition =
            new EcnEncodingObjectSetDefinition
            {
                Name = symbol.Name,
                Symbol = symbol,
                Parameterized = assignment != null
                    && assignment.Parameters.Length > 0,
                Valid = !symbol.BindingFailed,
                Span = symbol.Span
            };
        if (assignment == null || assignment.BodyTokens.Length == 0)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidObjectSet,
                "The encoding object set assignment has no set expression.",
                symbol.Span);
            return definition;
        }

        definition.SourceTokens = assignment.BodyTokens;
        int expressionEnd = assignment.BodyTokens.Length;
        int completed = this.FindTopLevelText(
            assignment.BodyTokens,
            "COMPLETED");
        if (completed >= 0)
        {
            expressionEnd = completed;
            if (completed + 2 >= assignment.BodyTokens.Length
                || assignment.BodyTokens[completed + 1].Text != "BY")
            {
                definition.Valid = false;
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidObjectSet,
                    "COMPLETED must be followed by BY and an encoding object set.",
                    assignment.BodyTokens[completed].Span);
            }
            else
            {
                AsnToken[] completionTokens = this.CopyTokens(
                    assignment.BodyTokens,
                    completed + 2,
                    assignment.BodyTokens.Length - completed - 2);
                definition.CompletionName = this.ReadReferenceName(
                    completionTokens);
                EcnSymbol? completion = this.FindSymbolReference(
                    table,
                    symbol,
                    definition.CompletionName,
                    EcnSymbolKind.EncodingObjectSet,
                    EcnParameterKind.EncodingObjectSet);
                definition.CompletionSymbol = completion;
                definition.CompletionBuiltIn =
                    EcnEncodingObjectSetCatalog.IsBuiltIn(
                        definition.CompletionName);
                definition.PredeclaredCompletion =
                    EcnEncodingObjectSetCatalog.FindSet(
                        definition.CompletionName);
                definition.CompletionActualParameters =
                    this.BindReferenceActuals(
                        completionTokens,
                        symbol,
                        table,
                        completion);
                if (completion != null
                    && !this.ActualParametersAreValid(
                        definition.CompletionActualParameters,
                        completion))
                {
                    definition.Valid = false;
                }
            }
        }

        definition.Terms = this.ParseSetTerms(
            this.CopyTokens(assignment.BodyTokens, 0, expressionEnd),
            symbol,
            table);
        if (definition.Terms.Length == 0)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidObjectSet,
                "An encoding object set must contain at least one object or set reference.",
                symbol.Span);
        }

        int termIndex = 0;
        while (termIndex < definition.Terms.Length)
        {
            if (!definition.Terms[termIndex].Valid)
            {
                definition.Valid = false;
            }

            termIndex = termIndex + 1;
        }

        return definition;
    }

    internal EcnActualParameter[] BindGovernorActuals(
        EcnAssignmentSyntax assignment,
        EcnSymbol owner,
        EcnSymbolTable table)
    {
        int position = 1;
        if (assignment.Parameters.Length > 0)
        {
            position = this.FindActualListEnd(assignment.HeadTokens, position);
        }

        while (position + 1 < assignment.HeadTokens.Length)
        {
            if (assignment.HeadTokens[position].Kind == AsnTokenKind.LeftBrace
                && assignment.HeadTokens[position + 1].Kind
                    == AsnTokenKind.LessThan)
            {
                EcnActualParameterBinder binder =
                    new EcnActualParameterBinder(this.Diagnostics);
                EcnActualParameter[] actuals = binder.BindActualParameters(
                    assignment.HeadTokens,
                    position,
                    table,
                    owner,
                    owner.GovernorSymbol);
                if (!binder.Success)
                {
                    this.Success = false;
                }

                return actuals;
            }

            position = position + 1;
        }

        EcnActualParameterBinder missingBinder =
            new EcnActualParameterBinder(this.Diagnostics);
        if (!missingBinder.ValidateParameterPresence(
            owner.GovernorSymbol,
            owner.Span))
        {
            this.Success = false;
        }

        return [];
    }

    internal bool ActualParametersAreValid(
        EcnActualParameter[] actuals,
        EcnSymbol? target)
    {
        EcnSymbol? declaration = target == null
            ? null
            : target.ImportedTarget == null
                ? target
                : target.ImportedTarget;
        int formalCount = declaration == null || declaration.Scope == null
            ? 0
            : declaration.Scope.Symbols.Length;
        if (actuals.Length != formalCount)
        {
            return false;
        }

        int index = 0;
        while (index < actuals.Length)
        {
            if (!actuals[index].Valid)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal EcnEncodingObjectSetting[] ParseObjectSettings(AsnToken[] tokens)
    {
        if (tokens.Length == 0
            || tokens[0].Kind != AsnTokenKind.LeftBrace)
        {
            return [];
        }

        int close = this.FindMatching(tokens, 0);
        if (close < 0)
        {
            return [];
        }

        System.Collections.Generic.List<EcnEncodingObjectSetting> settings =
            new System.Collections.Generic.List<EcnEncodingObjectSetting>();
        int start = 1;
        int position = 1;
        int depth = 0;
        while (position <= close)
        {
            bool boundary = position == close;
            if (!boundary)
            {
                if (this.IsOpening(tokens[position].Kind))
                {
                    depth = depth + 1;
                }
                else if (this.IsClosing(tokens[position].Kind))
                {
                    depth = depth - 1;
                }
                else if (depth == 0
                    && tokens[position].Kind == AsnTokenKind.Comma)
                {
                    boundary = true;
                }
            }

            if (boundary && position > start)
            {
                AsnToken[] settingTokens = this.CopyTokens(
                    tokens,
                    start,
                    position - start);
                settings.Add(this.CreateObjectSetting(settingTokens));
                start = position + 1;
            }

            position = position + 1;
        }

        return settings.ToArray();
    }

    internal EcnEncodingObjectSetting CreateObjectSetting(AsnToken[] tokens)
    {
        EcnEncodingObjectSetting setting = new EcnEncodingObjectSetting
        {
            Name = tokens.Length == 0 ? "" : tokens[0].Text,
            Tokens = tokens,
            ValueTokens = tokens.Length <= 1
                ? []
                : this.CopyTokens(tokens, 1, tokens.Length - 1),
            Span = this.CreateSpan(tokens)
        };
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == "DEFAULT")
            {
                setting.Defaulted = true;
                setting.DefaultTokens = index + 1 >= tokens.Length
                    ? []
                    : this.CopyTokens(
                        tokens,
                        index + 1,
                        tokens.Length - index - 1);
                return setting;
            }

            index = index + 1;
        }

        return setting;
    }

    internal EcnOrderedEncodingObjectList[] ParseOrderedLists(
        EcnClauseSyntax[] clauses,
        EcnSymbol owner,
        EcnSymbolTable table)
    {
        System.Collections.Generic.List<EcnOrderedEncodingObjectList> lists =
            new System.Collections.Generic.List<EcnOrderedEncodingObjectList>();
        int clauseIndex = 0;
        while (clauseIndex < clauses.Length)
        {
            EcnClauseSyntax clause = clauses[clauseIndex];
            if (clause.Kind == EcnClauseKind.OrderedEncodingObjectList)
            {
                EcnOrderedEncodingObject[] items =
                    new EcnOrderedEncodingObject[clause.Items.Length];
                int itemIndex = 0;
                while (itemIndex < clause.Items.Length)
                {
                    items[itemIndex] = this.BindOrderedItem(
                        clause.Items[itemIndex],
                        itemIndex,
                        owner,
                        table);
                    itemIndex = itemIndex + 1;
                }

                lists.Add(new EcnOrderedEncodingObjectList
                {
                    Name = clause.Name,
                    Items = items,
                    Span = clause.Span
                });
            }

            clauseIndex = clauseIndex + 1;
        }

        return lists.ToArray();
    }

    internal EcnOrderedEncodingObject BindOrderedItem(
        EcnTokenGroupSyntax group,
        int order,
        EcnSymbol owner,
        EcnSymbolTable table)
    {
        EcnOrderedEncodingObject item = new EcnOrderedEncodingObject
        {
            Order = order,
            Tokens = group.Tokens,
            Span = group.Span
        };
        if (group.Tokens.Length == 0)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidOrderedList,
                "An ordered encoding object list contains an empty item.",
                group.Span);
            return item;
        }

        if (group.Tokens[0].Kind == AsnTokenKind.LeftBrace)
        {
            item.Inline = true;
            item.Valid = true;
            return item;
        }

        item.ReferenceName = this.ReadReferenceName(group.Tokens);
        item.Symbol = this.FindSymbolReference(
            table,
            owner,
            item.ReferenceName,
            EcnSymbolKind.EncodingObject,
            EcnParameterKind.EncodingObject);
        item.ActualParameters = this.BindReferenceActuals(
            group.Tokens,
            owner,
            table,
            item.Symbol);
        item.Valid = item.Symbol != null
            && this.ActualParametersAreValid(
                item.ActualParameters,
                item.Symbol);
        if (!item.Valid)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.UnknownEncodingObject,
                "The ordered encoding object '" + item.ReferenceName
                    + "' could not be resolved.",
                group.Span);
        }

        return item;
    }

    internal void ResolveObjectDefinition(
        EcnEncodingObjectDefinition definition,
        EcnSymbolTable table)
    {
        AsnToken[] tokens = definition.SourceTokens;
        if (tokens.Length == 0 || tokens[0].Kind == AsnTokenKind.LeftBrace
            || tokens[0].Kind == AsnTokenKind.NonEcnText)
        {
            return;
        }

        string name = this.ReadReferenceName(tokens);
        EcnSymbol? referenced = this.FindSymbolReference(
            table,
            definition.Symbol == null ? new EcnSymbol() : definition.Symbol,
            name,
            EcnSymbolKind.EncodingObject,
            EcnParameterKind.EncodingObject);
        definition.ReferenceDefinition = true;
        definition.ReferencedObjectName = name;
        definition.ReferencedObjectSymbol = referenced;
        definition.ReferenceActualParameters = this.BindReferenceActuals(
            tokens,
            definition.Symbol == null ? new EcnSymbol() : definition.Symbol,
            table,
            referenced);
        if (referenced == null)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.UnknownEncodingObject,
                "The encoding object '" + name + "' could not be resolved.",
                definition.Span);
            return;
        }

        if (!this.ActualParametersAreValid(
            definition.ReferenceActualParameters,
            referenced))
        {
            definition.Valid = false;
            return;
        }

        if (definition.Symbol != null
            && !this.IsReferencedObjectCompatible(definition, referenced))
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.IncompatibleObjectClass,
                "The referenced encoding object '" + name
                    + "' is governed by an incompatible encoding class.",
                definition.Span);
        }
    }

    internal bool IsReferencedObjectCompatible(
        EcnEncodingObjectDefinition definition,
        EcnSymbol referenced)
    {
        EcnSymbol? governor = referenced.GovernorSymbol;
        if (governor != null
            && governor.Kind == EcnSymbolKind.Parameter
            && governor.ParameterKind == EcnParameterKind.EncodingClass)
        {
            int index = governor.DeclarationOrder;
            if (index >= definition.ReferenceActualParameters.Length
                || definition.Symbol == null)
            {
                return false;
            }

            EcnActualParameter actual =
                definition.ReferenceActualParameters[index];
            if (actual.Symbol != null)
            {
                EcnSymbol target = actual.Symbol.ImportedTarget == null
                    ? actual.Symbol
                    : actual.Symbol.ImportedTarget;
                EcnSymbol? current = definition.Symbol.GovernorSymbol;
                if (current != null)
                {
                    EcnSymbol currentTarget = current.ImportedTarget == null
                        ? current
                        : current.ImportedTarget;
                    return target == currentTarget;
                }
            }

            if (actual.BuiltIn)
            {
                EcnBuiltInClassDefinition? builtIn =
                    EcnBuiltInClassCatalog.FindClass(actual.ReferenceName);
                return builtIn != null
                    && builtIn.Category == definition.Symbol.ClassCategory
                    && builtIn.PrimitiveName
                        == definition.Symbol.PrimitiveClassName;
            }

            return false;
        }

        return definition.Symbol != null
            && EcnEncodingObjectSelector.AreClassesCompatible(
                referenced,
                definition.Symbol);
    }

    internal EcnEncodingObjectSetTerm[] ParseSetTerms(
        AsnToken[] tokens,
        EcnSymbol owner,
        EcnSymbolTable table)
    {
        int start = 0;
        int end = tokens.Length;
        if (tokens.Length > 0 && tokens[0].Kind == AsnTokenKind.LeftBrace)
        {
            int close = this.FindMatching(tokens, 0);
            if (close < 0)
            {
                return [];
            }

            start = 1;
            end = close;
        }

        System.Collections.Generic.List<EcnEncodingObjectSetTerm> terms =
            new System.Collections.Generic.List<EcnEncodingObjectSetTerm>();
        EcnObjectSetOperation operation = EcnObjectSetOperation.Union;
        int termStart = start;
        int depth = 0;
        int position = start;
        while (position <= end)
        {
            bool boundary = position == end;
            EcnObjectSetOperation nextOperation =
                EcnObjectSetOperation.Union;
            if (!boundary)
            {
                if (this.IsOpening(tokens[position].Kind))
                {
                    depth = depth + 1;
                }
                else if (this.IsClosing(tokens[position].Kind))
                {
                    depth = depth - 1;
                }
                else if (depth == 0
                    && this.IsSetOperator(tokens[position]))
                {
                    boundary = true;
                    nextOperation = this.GetSetOperation(tokens[position]);
                }
            }

            if (boundary)
            {
                if (position > termStart)
                {
                    AsnToken[] termTokens = this.CopyTokens(
                        tokens,
                        termStart,
                        position - termStart);
                    terms.Add(this.CreateSetTerm(
                        termTokens,
                        operation,
                        owner,
                        table));
                }
                else if (position < end)
                {
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.InvalidObjectSet,
                        "An encoding object set operator is missing an operand.",
                        tokens[position].Span);
                }

                operation = nextOperation;
                termStart = position + 1;
            }

            position = position + 1;
        }

        return terms.ToArray();
    }

    internal EcnEncodingObjectSetTerm CreateSetTerm(
        AsnToken[] tokens,
        EcnObjectSetOperation operation,
        EcnSymbol owner,
        EcnSymbolTable table)
    {
        EcnEncodingObjectSetTerm term = new EcnEncodingObjectSetTerm
        {
            Operation = operation,
            ReferenceName = this.ReadReferenceName(tokens),
            Tokens = tokens,
            Span = this.CreateSpan(tokens)
        };
        if (EcnEncodingObjectSetCatalog.IsBuiltIn(term.ReferenceName))
        {
            term.Kind = EcnObjectSetTermKind.BuiltInSet;
            term.PredeclaredSet = EcnEncodingObjectSetCatalog.FindSet(
                term.ReferenceName);
            term.Valid = true;
            return term;
        }

        EcnSymbol? symbol = this.FindAnySetOperand(
            table,
            owner,
            term.ReferenceName);
        term.Symbol = symbol;
        term.ActualParameters = this.BindReferenceActuals(
            tokens,
            owner,
            table,
            symbol);
        if (symbol == null)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.UnknownEncodingObjectSet,
                "The encoding object or object set '" + term.ReferenceName
                    + "' could not be resolved.",
                term.Span);
            return term;
        }

        bool actualsValid = this.ActualParametersAreValid(
            term.ActualParameters,
            symbol);

        if (symbol.Kind == EcnSymbolKind.EncodingObject)
        {
            term.Kind = EcnObjectSetTermKind.Object;
        }
        else if (symbol.Kind == EcnSymbolKind.EncodingObjectSet)
        {
            term.Kind = EcnObjectSetTermKind.ObjectSet;
        }
        else
        {
            term.Kind = EcnObjectSetTermKind.Parameter;
        }

        term.Valid = actualsValid;
        return term;
    }

    internal void ResolveCompletion(
        EcnEncodingObjectSetDefinition definition,
        EcnSymbolTable table)
    {
        if (definition.CompletionName.Length == 0
            || definition.CompletionBuiltIn)
        {
            return;
        }

        if (definition.CompletionSymbol == null)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.UnknownEncodingObjectSet,
                "The completion encoding object set '"
                    + definition.CompletionName + "' could not be resolved.",
                definition.Span);
        }
    }

    internal EcnSymbol[] EvaluateObjectSet(
        EcnEncodingObjectSetDefinition definition)
    {
        if (definition.ResolutionState == 2)
        {
            return definition.Objects;
        }

        if (definition.ResolutionState == 1)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.ObjectSetCycle,
                "The encoding object set '" + definition.Name
                    + "' has a cyclic reference.",
                definition.Span);
            return [];
        }

        definition.ResolutionState = 1;
        EcnSymbol[] result = definition.Parameterized
            ? []
            : this.EvaluateSetTerms(definition.Terms, []);
        definition.Objects = result;
        definition.PredeclaredSets = this.EvaluatePredeclaredSetTerms(
            definition.Terms);
        definition.PredeclaredObjects = this.EvaluatePredeclaredObjectTerms(
            definition.Terms,
            []);
        definition.ResolutionState = 2;
        if (!definition.Valid)
        {
            this.Success = false;
        }

        return result;
    }

    internal EcnPredeclaredEncodingObjectSet[] EvaluatePredeclaredSetTerms(
        EcnEncodingObjectSetTerm[] terms)
    {
        System.Collections.Generic.List<EcnPredeclaredEncodingObjectSet> result =
            new System.Collections.Generic.List<EcnPredeclaredEncodingObjectSet>();
        int index = 0;
        while (index < terms.Length)
        {
            EcnPredeclaredEncodingObjectSet? set = terms[index].PredeclaredSet;
            if (set != null)
            {
                if (terms[index].Operation == EcnObjectSetOperation.Except)
                {
                    int found = this.FindPredeclaredSet(result, set);
                    if (found >= 0)
                    {
                        result.RemoveAt(found);
                    }
                }
                else if (this.FindPredeclaredSet(result, set) < 0)
                {
                    result.Add(set);
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal EcnPredeclaredEncodingObject[] EvaluatePredeclaredObjectTerms(
        EcnEncodingObjectSetTerm[] terms,
        EcnActualParameter[] actuals)
    {
        EcnPredeclaredEncodingObject[] result = [];
        int index = 0;
        while (index < terms.Length)
        {
            EcnPredeclaredEncodingObject[] operand =
                this.ExpandPredeclaredObjectTerm(terms[index], actuals);
            result = this.ApplyPredeclaredObjectOperation(
                result,
                operand,
                terms[index].Operation);
            index = index + 1;
        }
        return result;
    }

    internal EcnPredeclaredEncodingObject[] ExpandPredeclaredObjectTerm(
        EcnEncodingObjectSetTerm term,
        EcnActualParameter[] actuals)
    {
        if (term.PredeclaredSet != null)
        {
            return term.PredeclaredSet.Objects;
        }
        if (term.Kind == EcnObjectSetTermKind.ObjectSet
            && term.Symbol != null
            && term.Symbol.ObjectSetDefinition != null)
        {
            EcnEncodingObjectSetDefinition definition =
                term.Symbol.ObjectSetDefinition;
            if (term.ActualParameters.Length == 0)
            {
                this.EvaluateObjectSet(definition);
                return definition.PredeclaredObjects;
            }
            return this.EvaluatePredeclaredObjectTerms(
                definition.Terms,
                term.ActualParameters);
        }
        if (term.Kind == EcnObjectSetTermKind.Parameter
            && term.Symbol != null)
        {
            int parameterIndex = term.Symbol.DeclarationOrder;
            if (parameterIndex < actuals.Length)
            {
                EcnActualParameter actual = actuals[parameterIndex];
                if (actual.PredeclaredSet != null)
                {
                    return actual.PredeclaredSet.Objects;
                }
                if (actual.Symbol != null
                    && actual.Symbol.ObjectSetDefinition != null)
                {
                    this.EvaluateObjectSet(actual.Symbol.ObjectSetDefinition);
                    return actual.Symbol.ObjectSetDefinition.PredeclaredObjects;
                }
            }
        }
        return [];
    }

    internal EcnPredeclaredEncodingObject[] ApplyPredeclaredObjectOperation(
        EcnPredeclaredEncodingObject[] current,
        EcnPredeclaredEncodingObject[] operand,
        EcnObjectSetOperation operation)
    {
        if (operation == EcnObjectSetOperation.Intersection)
        {
            return this.IntersectPredeclaredObjects(current, operand);
        }
        if (operation == EcnObjectSetOperation.Except)
        {
            return this.ExceptPredeclaredObjects(current, operand);
        }
        return this.UnionPredeclaredObjects(current, operand);
    }

    internal EcnPredeclaredEncodingObject[] UnionPredeclaredObjects(
        EcnPredeclaredEncodingObject[] first,
        EcnPredeclaredEncodingObject[] second)
    {
        System.Collections.Generic.List<EcnPredeclaredEncodingObject> result =
            new System.Collections.Generic.List<EcnPredeclaredEncodingObject>();
        int index = 0;
        while (index < first.Length)
        {
            result.Add(first[index]);
            index = index + 1;
        }
        index = 0;
        while (index < second.Length)
        {
            if (!this.ContainsPredeclaredObject(result, second[index]))
            {
                result.Add(second[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal EcnPredeclaredEncodingObject[] IntersectPredeclaredObjects(
        EcnPredeclaredEncodingObject[] first,
        EcnPredeclaredEncodingObject[] second)
    {
        System.Collections.Generic.List<EcnPredeclaredEncodingObject> result =
            new System.Collections.Generic.List<EcnPredeclaredEncodingObject>();
        int index = 0;
        while (index < first.Length)
        {
            if (this.ContainsPredeclaredObjectArray(second, first[index]))
            {
                result.Add(first[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal EcnPredeclaredEncodingObject[] ExceptPredeclaredObjects(
        EcnPredeclaredEncodingObject[] first,
        EcnPredeclaredEncodingObject[] second)
    {
        System.Collections.Generic.List<EcnPredeclaredEncodingObject> result =
            new System.Collections.Generic.List<EcnPredeclaredEncodingObject>();
        int index = 0;
        while (index < first.Length)
        {
            if (!this.ContainsPredeclaredObjectArray(second, first[index]))
            {
                result.Add(first[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal bool ContainsPredeclaredObject(
        System.Collections.Generic.List<EcnPredeclaredEncodingObject> objects,
        EcnPredeclaredEncodingObject candidate)
    {
        int index = 0;
        while (index < objects.Count)
        {
            if (objects[index] == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsPredeclaredObjectArray(
        EcnPredeclaredEncodingObject[] objects,
        EcnPredeclaredEncodingObject candidate)
    {
        int index = 0;
        while (index < objects.Length)
        {
            if (objects[index] == candidate)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal int FindPredeclaredSet(
        System.Collections.Generic.List<EcnPredeclaredEncodingObjectSet> sets,
        EcnPredeclaredEncodingObjectSet candidate)
    {
        int index = 0;
        while (index < sets.Count)
        {
            if (sets[index] == candidate)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal EcnSymbol[] EvaluateInstantiatedSet(
        EcnEncodingObjectSetDefinition definition,
        EcnActualParameter[] actuals)
    {
        if (definition.ResolutionState == 1)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.ObjectSetCycle,
                "The parameterized encoding object set '" + definition.Name
                    + "' has a cyclic reference.",
                definition.Span);
            return [];
        }

        int previousState = definition.ResolutionState;
        definition.ResolutionState = 1;
        EcnSymbol[] result = this.EvaluateSetTerms(definition.Terms, actuals);
        definition.ResolutionState = previousState == 2 ? 2 : 0;
        return result;
    }

    internal EcnSymbol[] EvaluateSetTerms(
        EcnEncodingObjectSetTerm[] terms,
        EcnActualParameter[] actuals)
    {
        EcnSymbol[] result = [];
        int index = 0;
        while (index < terms.Length)
        {
            EcnSymbol[] operand = this.ExpandSetTerm(terms[index], actuals);
            result = this.ApplySetOperation(
                result,
                operand,
                terms[index].Operation);
            index = index + 1;
        }

        return result;
    }

    internal EcnSymbol[] ExpandSetTerm(
        EcnEncodingObjectSetTerm term,
        EcnActualParameter[] actuals)
    {
        if (term.Kind == EcnObjectSetTermKind.Object && term.Symbol != null)
        {
            return [term.Symbol];
        }

        if (term.Kind == EcnObjectSetTermKind.ObjectSet
            && term.Symbol != null
            && term.Symbol.ObjectSetDefinition != null)
        {
            return term.ActualParameters.Length == 0
                ? this.EvaluateObjectSet(term.Symbol.ObjectSetDefinition)
                : this.EvaluateInstantiatedSet(
                    term.Symbol.ObjectSetDefinition,
                    term.ActualParameters);
        }

        if (term.Kind == EcnObjectSetTermKind.Parameter
            && term.Symbol != null)
        {
            int parameterIndex = term.Symbol.DeclarationOrder;
            if (parameterIndex < actuals.Length)
            {
                EcnActualParameter actual = actuals[parameterIndex];
                if (actual.Symbol != null
                    && actual.Symbol.Kind == EcnSymbolKind.EncodingObject)
                {
                    return [actual.Symbol];
                }

                if (actual.Symbol != null
                    && actual.Symbol.ObjectSetDefinition != null)
                {
                    return this.EvaluateObjectSet(
                        actual.Symbol.ObjectSetDefinition);
                }
            }
        }

        return [];
    }

    internal EcnSymbol[] ApplySetOperation(
        EcnSymbol[] current,
        EcnSymbol[] operand,
        EcnObjectSetOperation operation)
    {
        if (operation == EcnObjectSetOperation.Intersection)
        {
            return this.IntersectObjects(current, operand);
        }

        if (operation == EcnObjectSetOperation.Except)
        {
            return this.ExceptObjects(current, operand);
        }

        return this.UnionObjects(current, operand);
    }

    internal EcnSymbol[] UnionObjects(EcnSymbol[] first, EcnSymbol[] second)
    {
        System.Collections.Generic.List<EcnSymbol> result =
            new System.Collections.Generic.List<EcnSymbol>();
        int index = 0;
        while (index < first.Length)
        {
            result.Add(first[index]);
            index = index + 1;
        }

        index = 0;
        while (index < second.Length)
        {
            if (!this.ContainsObject(result, second[index]))
            {
                result.Add(second[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal EcnSymbol[] IntersectObjects(
        EcnSymbol[] first,
        EcnSymbol[] second)
    {
        System.Collections.Generic.List<EcnSymbol> result =
            new System.Collections.Generic.List<EcnSymbol>();
        int index = 0;
        while (index < first.Length)
        {
            if (this.ContainsObjectArray(second, first[index]))
            {
                result.Add(first[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal EcnSymbol[] ExceptObjects(EcnSymbol[] first, EcnSymbol[] second)
    {
        System.Collections.Generic.List<EcnSymbol> result =
            new System.Collections.Generic.List<EcnSymbol>();
        int index = 0;
        while (index < first.Length)
        {
            if (!this.ContainsObjectArray(second, first[index]))
            {
                result.Add(first[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool ContainsObject(
        System.Collections.Generic.List<EcnSymbol> symbols,
        EcnSymbol candidate)
    {
        int index = 0;
        while (index < symbols.Count)
        {
            if (this.SameObject(symbols[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsObjectArray(EcnSymbol[] symbols, EcnSymbol candidate)
    {
        int index = 0;
        while (index < symbols.Length)
        {
            if (this.SameObject(symbols[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool SameObject(EcnSymbol first, EcnSymbol second)
    {
        EcnSymbol firstTarget = first.ImportedTarget == null
            ? first
            : first.ImportedTarget;
        EcnSymbol secondTarget = second.ImportedTarget == null
            ? second
            : second.ImportedTarget;
        return firstTarget == secondTarget;
    }

    internal EcnSymbol? FindAnySetOperand(
        EcnSymbolTable table,
        EcnSymbol owner,
        string name)
    {
        EcnSymbol? parameter = this.FindOwnerOperandParameter(owner, name);
        if (parameter != null)
        {
            return parameter;
        }

        EcnSymbol? objectSymbol = this.FindSymbolReference(
            table,
            owner,
            name,
            EcnSymbolKind.EncodingObject,
            EcnParameterKind.EncodingObject);
        return objectSymbol == null
            ? this.FindSymbolReference(
                table,
                owner,
                name,
                EcnSymbolKind.EncodingObjectSet,
                EcnParameterKind.EncodingObjectSet)
            : objectSymbol;
    }

    internal EcnSymbol? FindOwnerOperandParameter(EcnSymbol owner, string name)
    {
        if (owner.Scope == null)
        {
            return null;
        }

        EcnSymbol? parameter = owner.Scope.FindLocalSymbol(name);
        return parameter != null
                && (parameter.ParameterKind == EcnParameterKind.EncodingObject
                    || parameter.ParameterKind
                        == EcnParameterKind.EncodingObjectSet)
            ? parameter
            : null;
    }

    internal EcnSymbol? FindSymbolReference(
        EcnSymbolTable table,
        EcnSymbol owner,
        string reference,
        EcnSymbolKind kind,
        EcnParameterKind parameterKind)
    {
        if (owner.Scope != null)
        {
            EcnSymbol? parameter = owner.Scope.FindLocalSymbol(reference);
            if (parameter != null && parameter.ParameterKind == parameterKind)
            {
                return parameter;
            }
        }

        int dot = reference.IndexOf(".", System.StringComparison.Ordinal);
        string moduleName = dot < 0 ? "" : reference.Substring(0, dot);
        string name = dot < 0 ? reference : reference.Substring(dot + 1);
        EcnSymbol? found = null;
        int matches = 0;
        int index = 0;
        while (index < table.Symbols.Length)
        {
            EcnSymbol candidate = table.Symbols[index];
            if (candidate.Kind == kind
                && candidate.Name == name
                && (moduleName.Length == 0
                    || candidate.SourceModuleName == moduleName))
            {
                found = candidate;
                matches = matches + 1;
            }

            index = index + 1;
        }

        return matches == 1 ? found : null;
    }

    internal EcnActualParameter[] BindReferenceActuals(
        AsnToken[] tokens,
        EcnSymbol owner,
        EcnSymbolTable table,
        EcnSymbol? target)
    {
        int start = this.FindReferenceEnd(tokens);
        if (start + 1 >= tokens.Length
            || tokens[start].Kind != AsnTokenKind.LeftBrace
            || tokens[start + 1].Kind != AsnTokenKind.LessThan)
        {
            EcnActualParameterBinder missingBinder =
                new EcnActualParameterBinder(this.Diagnostics);
            if (!missingBinder.ValidateParameterPresence(
                target,
                this.CreateSpan(tokens)))
            {
                this.Success = false;
            }

            return [];
        }

        EcnActualParameterBinder binder =
            new EcnActualParameterBinder(this.Diagnostics);
        EcnActualParameter[] actuals = binder.BindActualParameters(
            tokens,
            start,
            table,
            owner,
            target);
        if (!binder.Success)
        {
            this.Success = false;
        }

        return actuals;
    }

    internal int FindReferenceEnd(AsnToken[] tokens)
    {
        return tokens.Length >= 3 && tokens[1].Kind == AsnTokenKind.Dot
            ? 3
            : 1;
    }

    internal int FindActualListEnd(AsnToken[] tokens, int start)
    {
        int position = start;
        while (position + 1 < tokens.Length)
        {
            if (tokens[position].Kind == AsnTokenKind.GreaterThan
                && tokens[position + 1].Kind == AsnTokenKind.RightBrace)
            {
                return position + 2;
            }

            position = position + 1;
        }

        return tokens.Length;
    }

    internal string ReadReferenceName(AsnToken[] tokens)
    {
        if (tokens.Length >= 3 && tokens[1].Kind == AsnTokenKind.Dot)
        {
            return tokens[0].Text + "." + tokens[2].Text;
        }

        return tokens.Length == 0 ? "" : tokens[0].Text;
    }

    internal int FindTopLevelText(AsnToken[] tokens, string text)
    {
        int depth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            if (depth == 0 && tokens[index].Text == text)
            {
                return index;
            }

            if (this.IsOpening(tokens[index].Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosing(tokens[index].Kind))
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindMatching(AsnToken[] tokens, int opening)
    {
        if (opening < 0 || opening >= tokens.Length)
        {
            return -1;
        }

        AsnTokenKind closing = this.ClosingKind(tokens[opening].Kind);
        int depth = 1;
        int index = opening + 1;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == tokens[opening].Kind)
            {
                depth = depth + 1;
            }
            else if (tokens[index].Kind == closing)
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

    internal bool IsSetOperator(AsnToken token)
    {
        return token.Kind == AsnTokenKind.VerticalBar
            || token.Kind == AsnTokenKind.Caret
            || token.Text == "UNION"
            || token.Text == "INTERSECTION"
            || token.Text == "EXCEPT";
    }

    internal EcnObjectSetOperation GetSetOperation(AsnToken token)
    {
        if (token.Kind == AsnTokenKind.Caret
            || token.Text == "INTERSECTION")
        {
            return EcnObjectSetOperation.Intersection;
        }

        return token.Text == "EXCEPT"
            ? EcnObjectSetOperation.Except
            : EcnObjectSetOperation.Union;
    }

    internal bool IsOpening(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftBrace
            || kind == AsnTokenKind.LeftParenthesis
            || kind == AsnTokenKind.LeftBracket
            || kind == AsnTokenKind.LeftVersionBracket
            || kind == AsnTokenKind.LessThan;
    }

    internal bool IsClosing(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.RightBrace
            || kind == AsnTokenKind.RightParenthesis
            || kind == AsnTokenKind.RightBracket
            || kind == AsnTokenKind.RightVersionBracket
            || kind == AsnTokenKind.GreaterThan;
    }

    internal AsnTokenKind ClosingKind(AsnTokenKind kind)
    {
        if (kind == AsnTokenKind.LeftBrace)
        {
            return AsnTokenKind.RightBrace;
        }

        if (kind == AsnTokenKind.LeftParenthesis)
        {
            return AsnTokenKind.RightParenthesis;
        }

        if (kind == AsnTokenKind.LeftBracket)
        {
            return AsnTokenKind.RightBracket;
        }

        if (kind == AsnTokenKind.LeftVersionBracket)
        {
            return AsnTokenKind.RightVersionBracket;
        }

        return AsnTokenKind.GreaterThan;
    }

    internal AsnToken[] CopyTokens(AsnToken[] tokens, int start, int count)
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
