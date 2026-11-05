namespace asn1;

internal class ObjectSetEvaluator
{
    internal ValueResolver ValueResolver;
    internal InformationObjectBinder ObjectBinder;
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];
    internal bool Success = true;

    internal ObjectSetEvaluator(
        ValueResolver valueResolver,
        InformationObjectBinder objectBinder)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.ObjectBinder = objectBinder == null
            ? throw new System.ArgumentNullException("objectBinder")
            : objectBinder;
        this.Diagnostics = valueResolver.Diagnostics;
    }

    internal bool BindObjectSets(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].ObjectSetSymbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].ObjectSetSymbols[symbolIndex];
                symbol.DeclaredObjectSetSyntax = null;
                symbol.ObjectSetResolutionState = 0;
                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].ObjectSetSymbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].ObjectSetSymbols[symbolIndex];
                if (!symbol.Parameterized)
                {
                    this.ResolveDeclaredSet(symbol);
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].ObjectSymbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].ObjectSymbols[symbolIndex];
                ParsedInformationObjectSyntax? syntax = symbol.DeclaredObjectSyntax;
                if (syntax != null)
                {
                    this.EvaluateNestedObjectSets(
                        syntax,
                        tables[tableIndex],
                        symbol,
                        syntax.Object.StableId);
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal ParsedInformationObjectSetSyntax? ResolveDeclaredSet(
        SymbolTableEntry symbol)
    {
        if (symbol.ObjectSetResolutionState == 2)
        {
            return symbol.DeclaredObjectSetSyntax;
        }

        if (symbol.ObjectSetResolutionState == 1)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.ReferenceCycle,
                "The object-set reference cycle containing '" + symbol.QualifiedName
                    + "' cannot be evaluated.",
                symbol.Span);
            return null;
        }

        if (symbol.Parameterized)
        {
            return null;
        }

        symbol.ObjectSetResolutionState = 1;
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
        ParsedObjectClassSyntax? governor = this.ObjectBinder.ResolveGovernorClass(symbol);
        if (module == null || governor == null)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.ClassMismatch,
                "The governing object class for object set '" + symbol.QualifiedName
                    + "' could not be resolved.",
                symbol.Span);
            symbol.ObjectSetResolutionState = 2;
            return null;
        }

        AsnToken[] rightSide = this.ObjectBinder.GetRightSideTokens(symbol);
        InformationObjectSetParseResult parsed = new InformationObjectParser(
            rightSide,
            this.Diagnostics,
            true).ParseObjectSetSyntax(governor);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != rightSide.Length)
        {
            symbol.ObjectSetResolutionState = 2;
            this.Success = false;
            return null;
        }

        ObjectSetEvaluationContext context = new ObjectSetEvaluationContext
        {
            StablePrefix = symbol.QualifiedName
        };
        if (!this.EvaluateSetSyntax(
            parsed.Syntax,
            module,
            symbol,
            governor,
            context))
        {
            this.Success = false;
        }

        parsed.Syntax.ObjectSet.Name = symbol.Name;
        parsed.Syntax.ObjectSet.StableId = symbol.QualifiedName;
        parsed.Syntax.ObjectSet.ObjectClass = governor.ObjectClass;
        symbol.DeclaredObjectSetSyntax = parsed.Syntax;
        symbol.ObjectSetResolutionState = 2;
        return parsed.Syntax;
    }

    internal bool EvaluateInlineSet(
        ParsedInformationObjectSetSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor,
        string stablePrefix)
    {
        this.Tables = this.ValueResolver.Tables;
        this.ObjectBinder.Tables = this.Tables;
        return this.EvaluateSetSyntax(
            syntax,
            module,
            declaration,
            governor,
            new ObjectSetEvaluationContext { StablePrefix = stablePrefix });
    }

    internal bool EvaluateSetSyntax(
        ParsedInformationObjectSetSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor,
        ObjectSetEvaluationContext context)
    {
        bool success = true;
        bool declaredExtensible = syntax.ObjectSet.Extensible;
        EvaluatedObjectSetResult rootResult;
        if (syntax.InformationFromObjects != null)
        {
            ParsedInformationObjectSetSyntax? selected = syntax.InformationFromObjects
                .ResolvedObjectSetSyntax;
            rootResult = selected == null
                ? new EvaluatedObjectSetResult()
                : this.CreateEvaluatedResult(
                    selected.RootMembers,
                    selected.AdditionalMembers,
                    selected.ObjectSet.Extensible);
        }
        else if (syntax.ParameterizedReference != null)
        {
            rootResult = this.EvaluateParameterizedReference(
                syntax.ParameterizedReference,
                module,
                declaration,
                governor,
                context);
            if (rootResult.RootMembers.Length == 0
                && rootResult.AdditionalMembers.Length == 0)
            {
                success = false;
            }
        }
        else
        {
            rootResult = syntax.RootElement == null && syntax.RootMembers.Length > 0
                ? this.CreateEvaluatedResult(
                    syntax.RootMembers,
                    syntax.AdditionalMembers,
                    syntax.ObjectSet.Extensible)
                : this.EvaluateElement(
                    syntax.RootElement,
                    module,
                    declaration,
                    governor,
                    context);
        }

        EvaluatedObjectSetResult additionalResult = this.EvaluateElement(
            syntax.AdditionalElement,
            module,
            declaration,
            governor,
            context);
        EvaluatedObjectSetMember[] root = rootResult.RootMembers;
        EvaluatedObjectSetMember[] explicitAdditional = this.UnionMembers(
            additionalResult.RootMembers,
            additionalResult.AdditionalMembers);
        EvaluatedObjectSetMember[] additional = this.UnionMembers(
            rootResult.AdditionalMembers,
            explicitAdditional);
        additional = this.ExceptMembers(additional, root);
        additional = this.MarkExtensionMembers(additional);
        EvaluatedObjectSetMember[] all = this.UnionMembers(root, additional);
        syntax.RootMembers = root;
        syntax.AdditionalMembers = additional;
        syntax.ObjectSet.Extensible = declaredExtensible
            || rootResult.Extensible
            || additionalResult.Extensible;
        syntax.ObjectSet.ObjectClass = governor.ObjectClass;
        syntax.ObjectSet.StableId = context.StablePrefix;
        syntax.ObjectSet.RootObjects = this.CopyMemberObjects(root);
        syntax.ObjectSet.AdditionalObjects = this.CopyMemberObjects(additional);
        syntax.ObjectSet.Objects = this.CopyMemberObjects(all);
        if (!this.ValidateUniqueKeys(all, governor))
        {
            success = false;
        }

        return success;
    }

    internal EvaluatedObjectSetResult EvaluateElement(
        ParsedObjectSetElementSyntax? element,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor,
        ObjectSetEvaluationContext context)
    {
        if (element == null)
        {
            return new EvaluatedObjectSetResult();
        }

        if (element.Kind == "Object")
        {
            return this.EvaluateObjectOperand(
                element,
                module,
                declaration,
                governor,
                context);
        }

        if (element.Kind == "ObjectSetReference")
        {
            return this.EvaluateSetReferenceOperand(
                element,
                module,
                declaration,
                governor,
                context);
        }

        if (element.Kind == "ParameterizedObjectSet"
            && element.ParameterizedReference != null)
        {
            return this.EvaluateParameterizedReference(
                element.ParameterizedReference,
                module,
                declaration,
                governor,
                context);
        }

        if (element.Kind == "InformationFromObjects"
            && element.InformationFromObjects != null)
        {
            ParsedInformationObjectSetSyntax? selected = element.InformationFromObjects
                .ResolvedObjectSetSyntax;
            return selected == null
                ? new EvaluatedObjectSetResult()
                : this.CreateEvaluatedResult(
                    selected.RootMembers,
                    selected.AdditionalMembers,
                    selected.ObjectSet.Extensible);
        }

        if (element.Kind == "AllExcept" && element.Children.Length == 1)
        {
            EvaluatedObjectSetMember[] universe = this.BuildKnownUniverse(governor);
            EvaluatedObjectSetResult excluded = this.EvaluateElement(
                element.Children[0],
                module,
                declaration,
                governor,
                context);
            EvaluatedObjectSetMember[] root = this.ExceptMembers(
                universe,
                excluded.RootMembers);
            EvaluatedObjectSetMember[] all = this.ExceptMembers(
                universe,
                this.GetAllMembers(excluded));
            return this.CreateEvaluatedResult(
                root,
                this.ExceptMembers(all, root),
                excluded.Extensible);
        }

        if (element.Children.Length == 2)
        {
            EvaluatedObjectSetResult left = this.EvaluateElement(
                element.Children[0],
                module,
                declaration,
                governor,
                context);
            EvaluatedObjectSetResult right = this.EvaluateElement(
                element.Children[1],
                module,
                declaration,
                governor,
                context);
            if (element.Kind == "Union")
            {
                return this.EvaluateUnionResult(left, right);
            }

            if (element.Kind == "Intersection")
            {
                return this.EvaluateIntersectionResult(left, right);
            }

            if (element.Kind == "Except")
            {
                return this.EvaluateExclusionResult(left, right);
            }
        }

        this.AddDiagnostic(
            ObjectSetDiagnosticCodes.UnknownReference,
            "The object-set operand could not be evaluated.",
            element.Span);

        return new EvaluatedObjectSetResult();
    }

    internal EvaluatedObjectSetResult EvaluateObjectOperand(
        ParsedObjectSetElementSyntax element,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor,
        ObjectSetEvaluationContext context)
    {
        ParsedInformationObjectSyntax? objectSyntax = element.ObjectSyntax;
        if (objectSyntax == null)
        {
            return new EvaluatedObjectSetResult();
        }

        int formalIndex = this.FindFormalIndex(
            context,
            objectSyntax.ReferenceName);
        if (objectSyntax.ReferenceModule.Length == 0 && formalIndex >= 0)
        {
            ParsedActualParameterSyntax actual = context.ActualParameters[formalIndex];
            InformationObjectParseResult parsed = new InformationObjectParser(
                actual.SourceTokens,
                this.Diagnostics,
                false).ParseObjectSyntax(governor);
            if (!parsed.Success
                || parsed.Syntax == null
                || parsed.ConsumedTokenCount != actual.SourceTokens.Length)
            {
                this.AddDiagnostic(
                    ObjectSetDiagnosticCodes.InvalidParameterizedOperand,
                    "The actual parameter for '" + context.FormalNames[formalIndex]
                        + "' is not an information object.",
                    actual.Span);
                return new EvaluatedObjectSetResult();
            }

            objectSyntax = parsed.Syntax;
        }

        if (!this.ObjectBinder.BindObjectValue(
            objectSyntax,
            module,
            declaration,
            governor))
        {
            return new EvaluatedObjectSetResult();
        }

        string stableId = objectSyntax.Object.StableId;
        if (stableId.Length == 0)
        {
            stableId = this.CreateInlineObjectId(context.StablePrefix, element.Span);
            objectSyntax.Object.StableId = stableId;
        }

        this.EvaluateNestedObjectSets(
            objectSyntax,
            module,
            declaration,
            stableId);

        return new EvaluatedObjectSetResult
        {
            RootMembers = [new EvaluatedObjectSetMember
            {
                Object = objectSyntax.Object,
                ObjectSyntax = objectSyntax,
                StableId = stableId,
                Span = element.Span
            }]
        };
    }

    internal EvaluatedObjectSetResult EvaluateSetReferenceOperand(
        ParsedObjectSetElementSyntax element,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor,
        ObjectSetEvaluationContext context)
    {
        int formalIndex = element.ReferenceModule.Length == 0
            ? this.FindFormalIndex(context, element.ReferenceName)
            : -1;
        if (formalIndex >= 0)
        {
            ParsedActualParameterSyntax actual = context.ActualParameters[formalIndex];
            InformationObjectSetParseResult parsed = new InformationObjectParser(
                actual.SourceTokens,
                this.Diagnostics,
                false).ParseObjectSetSyntax(governor);
            if (!parsed.Success
                || parsed.Syntax == null
                || parsed.ConsumedTokenCount != actual.SourceTokens.Length)
            {
                this.AddDiagnostic(
                    ObjectSetDiagnosticCodes.InvalidParameterizedOperand,
                    "The actual parameter for '" + context.FormalNames[formalIndex]
                        + "' is not an information object set.",
                    actual.Span);
                return new EvaluatedObjectSetResult();
            }

            ObjectSetEvaluationContext actualContext = new ObjectSetEvaluationContext
            {
                StablePrefix = context.StablePrefix + ":actual:"
                    + context.FormalNames[formalIndex],
                ParameterizedReferences = context.ParameterizedReferences
            };
            this.EvaluateSetSyntax(
                parsed.Syntax,
                module,
                declaration,
                governor,
                actualContext);
            return this.CreateEvaluatedResult(
                parsed.Syntax.RootMembers,
                parsed.Syntax.AdditionalMembers,
                parsed.Syntax.ObjectSet.Extensible);
        }

        SymbolTableEntry? target = this.ObjectBinder.FindSymbol(
            module,
            element.ReferenceModule,
            element.ReferenceName,
            AsnAssignmentKind.ObjectSet);
        ParsedInformationObjectSetSyntax? resolved = target == null
            ? null
            : this.ResolveDeclaredSet(target);
        if (resolved == null)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.UnknownReference,
                "The object set '" + this.FormatReference(
                    element.ReferenceModule,
                    element.ReferenceName) + "' could not be resolved.",
                element.Span);
            return new EvaluatedObjectSetResult();
        }

        if (resolved.ObjectSet.ObjectClass != governor.ObjectClass)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.ClassMismatch,
                "The referenced object set is governed by a different object class.",
                element.Span);
            return new EvaluatedObjectSetResult();
        }

        return this.CreateEvaluatedResult(
            resolved.RootMembers,
            resolved.AdditionalMembers,
            resolved.ObjectSet.Extensible);
    }

    internal EvaluatedObjectSetResult EvaluateParameterizedReference(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTable module,
        SymbolTableEntry declaration,
        ParsedObjectClassSyntax governor,
        ObjectSetEvaluationContext parentContext)
    {
        SymbolTableEntry? target = this.ObjectBinder.FindSymbol(
            module,
            reference.ReferenceModule,
            reference.ReferenceName,
            AsnAssignmentKind.ObjectSet);
        if (target == null || !target.Parameterized)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.InvalidParameterizedOperand,
                "The parameterized object set '" + this.FormatReference(
                    reference.ReferenceModule,
                    reference.ReferenceName) + "' could not be resolved.",
                reference.Span);
            return new EvaluatedObjectSetResult();
        }

        if (target.ParameterNames.Length != reference.ActualParameterSyntax.Length)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.InvalidParameterizedOperand,
                "The parameterized object set '" + target.QualifiedName
                    + "' requires " + target.ParameterNames.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + " actual parameters.",
                reference.Span);
            return new EvaluatedObjectSetResult();
        }

        ParsedObjectClassSyntax? targetGovernor = this.ObjectBinder.ResolveGovernorClass(target);
        SymbolTable? targetModule = this.ValueResolver.FindDeclaringModule(target);
        if (targetGovernor == null
            || targetModule == null
            || targetGovernor.ObjectClass != governor.ObjectClass)
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.ClassMismatch,
                "The parameterized object set is governed by a different object class.",
                reference.Span);
            return new EvaluatedObjectSetResult();
        }

        string key = target.QualifiedName + this.FormatActualParameters(
            reference.ActualParameterSyntax);
        if (parentContext.ParameterizedReferences.Contains(key))
        {
            this.AddDiagnostic(
                ObjectSetDiagnosticCodes.ReferenceCycle,
                "The parameterized object-set reference '" + key
                    + "' forms a non-terminating cycle.",
                reference.Span);
            return new EvaluatedObjectSetResult();
        }

        parentContext.ParameterizedReferences.Add(key);
        InformationObjectSetParseResult parsed = new InformationObjectParser(
            this.ObjectBinder.GetRightSideTokens(target),
            this.Diagnostics,
            true).ParseObjectSetSyntax(targetGovernor);
        if (!parsed.Success || parsed.Syntax == null)
        {
            parentContext.ParameterizedReferences.RemoveAt(
                parentContext.ParameterizedReferences.Count - 1);
            return new EvaluatedObjectSetResult();
        }

        ObjectSetEvaluationContext context = new ObjectSetEvaluationContext
        {
            StablePrefix = key,
            FormalNames = target.ParameterNames,
            ActualParameters = reference.ActualParameterSyntax,
            ParameterizedReferences = parentContext.ParameterizedReferences
        };
        this.EvaluateSetSyntax(
            parsed.Syntax,
            targetModule,
            declaration,
            targetGovernor,
            context);
        parentContext.ParameterizedReferences.RemoveAt(
            parentContext.ParameterizedReferences.Count - 1);
        return this.CreateEvaluatedResult(
            parsed.Syntax.RootMembers,
            parsed.Syntax.AdditionalMembers,
            parsed.Syntax.ObjectSet.Extensible);
    }

    internal EvaluatedObjectSetResult EvaluateUnionResult(
        EvaluatedObjectSetResult left,
        EvaluatedObjectSetResult right)
    {
        EvaluatedObjectSetMember[] root = this.UnionMembers(
            left.RootMembers,
            right.RootMembers);
        EvaluatedObjectSetMember[] all = this.UnionMembers(
            this.GetAllMembers(left),
            this.GetAllMembers(right));
        return this.CreateEvaluatedResult(
            root,
            this.ExceptMembers(all, root),
            left.Extensible || right.Extensible);
    }

    internal EvaluatedObjectSetResult EvaluateIntersectionResult(
        EvaluatedObjectSetResult left,
        EvaluatedObjectSetResult right)
    {
        EvaluatedObjectSetMember[] root = this.IntersectMembers(
            left.RootMembers,
            right.RootMembers);
        EvaluatedObjectSetMember[] all = this.IntersectMembers(
            this.GetAllMembers(left),
            this.GetAllMembers(right));
        return this.CreateEvaluatedResult(
            root,
            this.ExceptMembers(all, root),
            left.Extensible || right.Extensible);
    }

    internal EvaluatedObjectSetResult EvaluateExclusionResult(
        EvaluatedObjectSetResult left,
        EvaluatedObjectSetResult right)
    {
        EvaluatedObjectSetMember[] root = this.ExceptMembers(
            left.RootMembers,
            right.RootMembers);
        EvaluatedObjectSetMember[] all = this.ExceptMembers(
            this.GetAllMembers(left),
            this.GetAllMembers(right));
        return this.CreateEvaluatedResult(
            root,
            this.ExceptMembers(all, root),
            left.Extensible || right.Extensible);
    }

    internal EvaluatedObjectSetResult CreateEvaluatedResult(
        EvaluatedObjectSetMember[] root,
        EvaluatedObjectSetMember[] additional,
        bool extensible)
    {
        return new EvaluatedObjectSetResult
        {
            RootMembers = root,
            AdditionalMembers = this.MarkExtensionMembers(additional),
            Extensible = extensible
        };
    }

    internal EvaluatedObjectSetMember[] GetAllMembers(
        EvaluatedObjectSetResult result)
    {
        return this.UnionMembers(result.RootMembers, result.AdditionalMembers);
    }

    internal EvaluatedObjectSetMember[] BuildKnownUniverse(
        ParsedObjectClassSyntax governor)
    {
        System.Collections.Generic.List<EvaluatedObjectSetMember> members =
            new System.Collections.Generic.List<EvaluatedObjectSetMember>();
        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].ObjectSymbols.Length)
            {
                SymbolTableEntry symbol = this.Tables[tableIndex].ObjectSymbols[symbolIndex];
                ParsedInformationObjectSyntax? syntax = this.ObjectBinder.ResolveDeclaredObject(
                    symbol);
                if (syntax != null
                    && syntax.Object.ObjectClass == governor.ObjectClass
                    && !this.ContainsMemberIdentityInList(
                        members,
                        syntax.Object.StableId))
                {
                    members.Add(new EvaluatedObjectSetMember
                    {
                        Object = syntax.Object,
                        ObjectSyntax = syntax,
                        StableId = syntax.Object.StableId,
                        Span = syntax.Span
                    });
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return members.ToArray();
    }

    internal void EvaluateNestedObjectSets(
        ParsedInformationObjectSyntax syntax,
        SymbolTable module,
        SymbolTableEntry declaration,
        string stablePrefix)
    {
        int index = 0;
        while (index < syntax.FieldSettingSyntax.Length)
        {
            ParsedObjectFieldSettingSyntax setting = syntax.FieldSettingSyntax[index];
            if (setting.ObjectSyntax != null)
            {
                this.EvaluateNestedObjectSets(
                    setting.ObjectSyntax,
                    module,
                    declaration,
                    stablePrefix + ":" + setting.Setting.FieldName);
            }

            if (setting.ObjectSetSyntax != null
                && setting.Setting.ObjectSet != null
                && setting.Setting.ObjectSet.ObjectClass != null)
            {
                ParsedObjectClassSyntax? governor = this.ObjectBinder.FindResolvedClass(
                    setting.Setting.ObjectSet.ObjectClass);
                if (governor != null)
                {
                    string settingPrefix = stablePrefix + ":"
                        + setting.Setting.FieldName;
                    this.EvaluateSetSyntax(
                        setting.ObjectSetSyntax,
                        module,
                        declaration,
                        governor,
                        new ObjectSetEvaluationContext
                        {
                            StablePrefix = settingPrefix
                        });
                    setting.Setting.ObjectSet.StableId = settingPrefix;
                }
            }

            index = index + 1;
        }
    }

    internal EvaluatedObjectSetMember[] UnionMembers(
        EvaluatedObjectSetMember[] left,
        EvaluatedObjectSetMember[] right)
    {
        System.Collections.Generic.List<EvaluatedObjectSetMember> result =
            new System.Collections.Generic.List<EvaluatedObjectSetMember>();
        int index = 0;
        while (index < left.Length)
        {
            if (!this.ContainsMemberIdentityInList(result, left[index].StableId))
            {
                result.Add(left[index]);
            }

            index = index + 1;
        }

        index = 0;
        while (index < right.Length)
        {
            if (!this.ContainsMemberIdentityInList(result, right[index].StableId))
            {
                result.Add(right[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal EvaluatedObjectSetMember[] IntersectMembers(
        EvaluatedObjectSetMember[] left,
        EvaluatedObjectSetMember[] right)
    {
        System.Collections.Generic.List<EvaluatedObjectSetMember> result =
            new System.Collections.Generic.List<EvaluatedObjectSetMember>();
        int index = 0;
        while (index < left.Length)
        {
            if (this.ContainsMemberIdentityInArray(right, left[index].StableId)
                && !this.ContainsMemberIdentityInList(result, left[index].StableId))
            {
                result.Add(left[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal EvaluatedObjectSetMember[] ExceptMembers(
        EvaluatedObjectSetMember[] left,
        EvaluatedObjectSetMember[] right)
    {
        System.Collections.Generic.List<EvaluatedObjectSetMember> result =
            new System.Collections.Generic.List<EvaluatedObjectSetMember>();
        int index = 0;
        while (index < left.Length)
        {
            if (!this.ContainsMemberIdentityInArray(right, left[index].StableId)
                && !this.ContainsMemberIdentityInList(result, left[index].StableId))
            {
                result.Add(left[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal EvaluatedObjectSetMember[] MarkExtensionMembers(
        EvaluatedObjectSetMember[] members)
    {
        EvaluatedObjectSetMember[] result =
            new EvaluatedObjectSetMember[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            result[index] = new EvaluatedObjectSetMember
            {
                Object = members[index].Object,
                ObjectSyntax = members[index].ObjectSyntax,
                StableId = members[index].StableId,
                Span = members[index].Span,
                Extension = true
            };
            index = index + 1;
        }

        return result;
    }

    internal bool ValidateUniqueKeys(
        EvaluatedObjectSetMember[] members,
        ParsedObjectClassSyntax governor)
    {
        AsnObjectClassField? unique = this.FindUniqueField(governor);
        if (unique == null)
        {
            return true;
        }

        bool success = true;
        int index = 0;
        while (index < members.Length)
        {
            AsnObjectFieldSetting? current = this.FindObjectSetting(
                members[index].Object,
                unique.Name);
            int earlier = 0;
            while (current != null && current.Value != null && earlier < index)
            {
                AsnObjectFieldSetting? previous = this.FindObjectSetting(
                    members[earlier].Object,
                    unique.Name);
                if (previous != null
                    && previous.Value != null
                    && this.AreValuesEqual(previous.Value, current.Value))
                {
                    this.AddDiagnostic(
                        ObjectSetDiagnosticCodes.DuplicateUniqueKey,
                        "The object set contains more than one object with UNIQUE field '"
                            + unique.Name + "' equal to '"
                            + this.FormatValue(current.Value) + "'.",
                        members[index].Span);
                    success = false;
                    break;
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }

        return success;
    }

    internal AsnObjectClassField? FindUniqueField(
        ParsedObjectClassSyntax governor)
    {
        int index = 0;
        while (index < governor.ObjectClass.Fields.Length)
        {
            if (governor.ObjectClass.Fields[index].Unique)
            {
                return governor.ObjectClass.Fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnObjectFieldSetting? FindObjectSetting(
        AsnInformationObject item,
        string fieldName)
    {
        int index = 0;
        while (index < item.FieldSettings.Length)
        {
            if (item.FieldSettings[index].FieldName == fieldName)
            {
                return item.FieldSettings[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool AreValuesEqual(AsnValue left, AsnValue right)
    {
        if (left.Kind != right.Kind
            || left.HasIntegerValue != right.HasIntegerValue
            || (left.HasIntegerValue && left.IntegerValue != right.IntegerValue)
            || left.TextValue != right.TextValue
            || left.NamedValues.Length != right.NamedValues.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.NamedValues.Length)
        {
            AsnNamedValue leftItem = left.NamedValues[index];
            AsnNamedValue rightItem = right.NamedValues[index];
            if (leftItem.Name != rightItem.Name
                || leftItem.Value == null
                || rightItem.Value == null
                || !this.AreValuesEqual(leftItem.Value, rightItem.Value))
            {
                return false;
            }

            index = index + 1;
        }

        return this.AreRealValuesEqual(left.RealValue, right.RealValue)
            && this.AreTimeValuesEqual(left.TimeValue, right.TimeValue);
    }

    internal bool AreRealValuesEqual(AsnRealValue? left, AsnRealValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return left.Kind == right.Kind
            && left.Normalized == right.Normalized
            && left.HasDecimalValue == right.HasDecimalValue
            && left.DecimalText == right.DecimalText
            && left.DecimalCoefficient == right.DecimalCoefficient
            && left.DecimalExponent == right.DecimalExponent
            && left.HasBinaryValue == right.HasBinaryValue
            && left.Mantissa == right.Mantissa
            && left.Radix == right.Radix
            && left.Exponent == right.Exponent;
    }

    internal bool AreTimeValuesEqual(AsnTimeValue? left, AsnTimeValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        return left.Kind == right.Kind
            && left.Text == right.Text
            && left.HasUtcOffsetMinutes == right.HasUtcOffsetMinutes
            && left.UtcOffsetMinutes == right.UtcOffsetMinutes
            && left.HasFractionalDigits == right.HasFractionalDigits
            && left.FractionalDigits == right.FractionalDigits;
    }

    internal bool ContainsMemberIdentityInList(
        System.Collections.Generic.List<EvaluatedObjectSetMember> members,
        string stableId)
    {
        int index = 0;
        while (index < members.Count)
        {
            if (members[index].StableId == stableId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsMemberIdentityInArray(
        EvaluatedObjectSetMember[] members,
        string stableId)
    {
        int index = 0;
        while (index < members.Length)
        {
            if (members[index].StableId == stableId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int FindFormalIndex(
        ObjectSetEvaluationContext context,
        string name)
    {
        int index = 0;
        while (index < context.FormalNames.Length)
        {
            if (context.FormalNames[index] == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnInformationObject[] CopyMemberObjects(
        EvaluatedObjectSetMember[] members)
    {
        AsnInformationObject[] result = new AsnInformationObject[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            result[index] = members[index].Object;
            index = index + 1;
        }

        return result;
    }

    internal string CreateInlineObjectId(
        string prefix,
        AsnSourceSpan? span)
    {
        if (span == null)
        {
            return prefix + ":inline";
        }

        return prefix + ":" + span.Start.SourceName + ":"
            + span.Start.Offset.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }

    internal string FormatActualParameters(
        ParsedActualParameterSyntax[] actuals)
    {
        string result = "{";
        int actualIndex = 0;
        while (actualIndex < actuals.Length)
        {
            if (actualIndex > 0)
            {
                result = result + ",";
            }

            int tokenIndex = 0;
            while (tokenIndex < actuals[actualIndex].SourceTokens.Length)
            {
                result = result + actuals[actualIndex].SourceTokens[tokenIndex].Text;
                tokenIndex = tokenIndex + 1;
            }

            actualIndex = actualIndex + 1;
        }

        return result + "}";
    }

    internal string FormatReference(string moduleName, string referenceName)
    {
        return moduleName.Length == 0
            ? referenceName
            : moduleName + "." + referenceName;
    }

    internal string FormatValue(AsnValue value)
    {
        if (value.HasIntegerValue)
        {
            return value.IntegerValue.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        return value.TextValue.Length == 0
            ? value.Kind.ToString()
            : value.TextValue;
    }

    internal void AddDiagnostic(int code, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
