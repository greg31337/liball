namespace asn1;

internal class TableConstraintCompiler
{
    internal ValueResolver ValueResolver;
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];
    internal bool Success = true;

    internal TableConstraintCompiler(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.Diagnostics = valueResolver.Diagnostics;
    }

    internal bool CompileTypes(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int classIndex = 0;
            while (classIndex < tables[tableIndex].ObjectClassSymbols.Length)
            {
                this.ResolveObjectClassSymbol(
                    tables[tableIndex].ObjectClassSymbols[classIndex]);
                classIndex = classIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        System.Collections.Generic.List<ResolvedTypeSyntax> visiting =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.CompileType(
                        type,
                        new System.Collections.Generic.List<ResolvedTypeSyntax>(),
                        null,
                        visiting);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void CompileType(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes,
        ResolvedComponentSyntax? referencingComponent,
        System.Collections.Generic.List<ResolvedTypeSyntax> visiting)
    {
        if (visiting.Contains(type))
        {
            return;
        }

        visiting.Add(type);
        System.Collections.Generic.List<TableConstraintRelationship> relationships =
            new System.Collections.Generic.List<TableConstraintRelationship>();
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        if (type.EffectiveConstraint != null)
        {
            this.CompileGraph(
                type.EffectiveConstraint,
                type,
                type,
                enclosingTypes,
                referencingComponent,
                relationships,
                compiledNodes);
        }

        type.TableConstraints = relationships.ToArray();
        bool structural = this.IsStructuralConstruction(type);
        if (structural)
        {
            enclosingTypes.Add(type);
        }

        int componentIndex = 0;
        while (componentIndex < type.Components.Length)
        {
            ResolvedComponentSyntax component = type.Components[componentIndex];
            if (component.TypeResolution != null)
            {
                this.CompileType(
                    component.TypeResolution,
                    enclosingTypes,
                    component,
                    visiting);
            }

            componentIndex = componentIndex + 1;
        }

        if (type.InnerType != null)
        {
            this.CompileType(type.InnerType, enclosingTypes, null, visiting);
        }

        if (type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            this.CompileType(
                type.SelectedComponent.TypeResolution,
                enclosingTypes,
                type.SelectedComponent,
                visiting);
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            this.CompileType(
                type.ConstraintTypes[constraintIndex],
                new System.Collections.Generic.List<ResolvedTypeSyntax>(),
                null,
                visiting);
            constraintIndex = constraintIndex + 1;
        }

        if (structural)
        {
            enclosingTypes.RemoveAt(enclosingTypes.Count - 1);
        }

        visiting.Remove(type);
    }

    internal void CompileGraph(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax ownerType,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes,
        ResolvedComponentSyntax? referencingComponent,
        System.Collections.Generic.List<TableConstraintRelationship> relationships,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax effectiveGoverningType = graph.GoverningType == null
            ? governingType
            : graph.GoverningType;
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.CompileNode(
                graph.RootPartition.Expression,
                ownerType,
                effectiveGoverningType,
                enclosingTypes,
                referencingComponent,
                relationships,
                compiledNodes);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.CompileNode(
                graph.ExtensionPartition.Expression,
                ownerType,
                effectiveGoverningType,
                enclosingTypes,
                referencingComponent,
                relationships,
                compiledNodes);
        }
    }

    internal void CompileNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax ownerType,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes,
        ResolvedComponentSyntax? referencingComponent,
        System.Collections.Generic.List<TableConstraintRelationship> relationships,
        System.Collections.Generic.List<NormalizedConstraintNode> compiledNodes)
    {
        ResolvedTypeSyntax effectiveGoverningType = node.GoverningType == null
            ? governingType
            : node.GoverningType;
        if ((node.Kind == AsnConstraintKind.SimpleTable
                || node.Kind == AsnConstraintKind.ComponentRelation)
            && node.SourceGeneralConstraint != null
            && !compiledNodes.Contains(node))
        {
            compiledNodes.Add(node);
            TableConstraintRelationship? relationship = this.CompileTableNode(
                node,
                effectiveGoverningType,
                enclosingTypes,
                referencingComponent);
            node.CompiledTableConstraint = relationship;
            bool collectForOwner = node.Domain != null
                && ownerType.ConstraintDomain != null
                && node.Domain == ownerType.ConstraintDomain;
            if (relationship != null && collectForOwner)
            {
                relationship.GoverningType = ownerType;
                relationships.Add(relationship);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.CompileNode(
                node.Children[childIndex],
                ownerType,
                effectiveGoverningType,
                enclosingTypes,
                referencingComponent,
                relationships,
                compiledNodes);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.CompileGraph(
                node.NestedConstraints[nestedIndex],
                ownerType,
                effectiveGoverningType,
                enclosingTypes,
                referencingComponent,
                relationships,
                compiledNodes);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal TableConstraintRelationship? CompileTableNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax governingType,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes,
        ResolvedComponentSyntax? referencingComponent)
    {
        ParsedGeneralConstraintSyntax? source = node.SourceGeneralConstraint;
        if (source == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidTableObjectSet,
                "The table constraint has no retained object-set syntax.",
                null);
            return null;
        }

        ResolvedObjectClassFieldSyntax? constrainedField =
            this.ResolveObjectClassField(governingType, source.Span);
        if (constrainedField == null)
        {
            return null;
        }

        SymbolTableEntry? declaration = governingType.SourceDeclaration == null
            ? governingType.EffectiveDeclaration
            : governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (declaration == null
            || module == null
            || constrainedField.ObjectClassSyntax == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.UnknownTableObjectClass,
                "The governing object class could not be located.",
                source.Span);
            return null;
        }

        TableObjectSetResolution? objectSet = this.ResolveConstraintObjectSet(
            source.ObjectSetTokens,
            module,
            declaration,
            constrainedField.ObjectClassSyntax,
            source.Span);
        if (objectSet == null || objectSet.ObjectSet == null)
        {
            return null;
        }

        TableConstraintRelationship relationship = new TableConstraintRelationship
        {
            Kind = node.Kind,
            GoverningType = governingType,
            ReferencingComponent = referencingComponent,
            ConstrainedField = constrainedField,
            ObjectSetSymbol = objectSet.Symbol,
            ObjectSet = objectSet.ObjectSet,
            SourceNode = node,
            StableId = this.CreateRelationshipId(governingType, objectSet.Symbol, source)
        };
        if (node.Kind == AsnConstraintKind.ComponentRelation)
        {
            relationship.AtNotations = this.ResolveAtNotations(
                source,
                constrainedField,
                objectSet,
                enclosingTypes);
            relationship.ConstrainedType = this.FindConstrainedType(
                relationship.AtNotations,
                enclosingTypes);
        }
        else
        {
            relationship.ConstrainedType = governingType;
        }

        if (node.Kind == AsnConstraintKind.ComponentRelation)
        {
            this.ResolveLookupComponents(
                relationship.AtNotations,
                relationship.ConstrainedType,
                enclosingTypes,
                source.Span);
        }

        relationship.DispatchMap = this.BuildDispatchMap(
            objectSet,
            constrainedField,
            relationship.AtNotations,
            relationship.Kind,
            relationship.StableId,
            source.Span);
        relationship.DispatchEntries = relationship.DispatchMap.Entries;
        this.MergeDispatchEntries(objectSet.ObjectSet, relationship.DispatchEntries);
        return relationship;
    }

    internal ResolvedObjectClassFieldSyntax? ResolveObjectClassField(
        ResolvedTypeSyntax type,
        AsnSourceSpan? span)
    {
        if (type.ObjectClassField != null)
        {
            return type.ObjectClassField;
        }

        ParsedTypeSyntax? syntax = type.SourceSyntax;
        SymbolTableEntry? declaration = type.SourceDeclaration == null
            ? type.EffectiveDeclaration
            : type.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (syntax == null || declaration == null || module == null)
        {
            this.AddInvalidTableTypeDiagnostic(span);
            return null;
        }

        string classModule = "";
        string className = "";
        string fieldName = "";
        if (syntax.InformationFromObjects != null
            && !syntax.InformationFromObjects.ReferencedObjectSet)
        {
            classModule = syntax.InformationFromObjects.ReferenceModule;
            className = syntax.InformationFromObjects.ReferenceName;
            if (syntax.InformationFromObjects.FieldPath.Length > 0)
            {
                fieldName = syntax.InformationFromObjects.FieldPath[
                    syntax.InformationFromObjects.FieldPath.Length - 1];
            }
        }
        else if (syntax.Type.Kind == AsnTypeKind.InstanceOf)
        {
            classModule = syntax.ReferenceModule;
            className = syntax.ReferenceName;
        }
        else
        {
            this.AddInvalidTableTypeDiagnostic(span);
            return null;
        }

        ParsedObjectClassSyntax? objectClass = this.ValueResolver.UsefulObjectClasses
            .ResolveUsefulClass(classModule, className);
        TableSymbolBinding binding = new TableSymbolBinding();
        if (objectClass == null)
        {
            binding = this.FindSymbolBinding(
                module,
                classModule,
                className,
                AsnAssignmentKind.ObjectClass);
            objectClass = binding.Symbol == null
                ? null
                : this.ResolveObjectClassSymbol(binding.Symbol);
        }

        if (objectClass == null)
        {
            if (this.IsFormalObjectClassParameter(declaration, className))
            {
                return null;
            }

            this.AddDiagnostic(
                ConstraintDiagnosticCodes.UnknownTableObjectClass,
                "The object class '" + className + "' could not be resolved.",
                syntax.Span == null ? span : syntax.Span);
            return null;
        }

        AsnObjectClassField? field = fieldName.Length == 0
            ? this.FindFirstTypeField(objectClass)
            : this.FindObjectClassField(objectClass, fieldName);
        if (field == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.UnknownTableObjectClassField,
                "The object-class field '" + fieldName + "' could not be resolved.",
                syntax.Span == null ? span : syntax.Span);
            return null;
        }

        if (!this.IsTableFieldKind(field.Kind))
        {
            this.AddInvalidTableTypeDiagnostic(syntax.Span == null ? span : syntax.Span);
            return null;
        }

        type.ObjectClassField = new ResolvedObjectClassFieldSyntax
        {
            ObjectClassSymbol = binding.Symbol,
            ObjectClassSyntax = objectClass,
            Field = field
        };
        return type.ObjectClassField;
    }

    internal void AddInvalidTableTypeDiagnostic(AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            ConstraintDiagnosticCodes.InvalidTableConstraintType,
            "A table constraint can only be applied to an object-class field type or INSTANCE OF type.",
            span);
    }

    internal bool IsTableFieldKind(AsnObjectFieldKind kind)
    {
        return kind == AsnObjectFieldKind.Type
            || kind == AsnObjectFieldKind.FixedTypeValue
            || kind == AsnObjectFieldKind.VariableTypeValue
            || kind == AsnObjectFieldKind.FixedTypeValueSet
            || kind == AsnObjectFieldKind.VariableTypeValueSet;
    }

    internal AsnObjectClassField? FindFirstTypeField(ParsedObjectClassSyntax objectClass)
    {
        int index = 0;
        while (index < objectClass.ObjectClass.Fields.Length)
        {
            if (objectClass.ObjectClass.Fields[index].Kind == AsnObjectFieldKind.Type)
            {
                return objectClass.ObjectClass.Fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsFormalObjectClassParameter(
        SymbolTableEntry declaration,
        string name)
    {
        int index = 0;
        while (index < declaration.FormalParameters.Length)
        {
            if (declaration.FormalParameters[index].Name == name
                && declaration.FormalParameters[index].Kind
                    == AsnParameterKind.ObjectClass)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnObjectClassField? FindObjectClassField(
        ParsedObjectClassSyntax objectClass,
        string fieldName)
    {
        int index = 0;
        while (index < objectClass.ObjectClass.Fields.Length)
        {
            if (objectClass.ObjectClass.Fields[index].Name == fieldName)
            {
                return objectClass.ObjectClass.Fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal ParsedObjectClassSyntax? ResolveObjectClassSymbol(SymbolTableEntry symbol)
    {
        if (symbol.ObjectClassResolutionState == 2)
        {
            return symbol.ResolvedObjectClassSyntax;
        }

        if (symbol.ObjectClassResolutionState == 1)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.UnknownTableObjectClass,
                "The object-class reference cycle cannot govern a table constraint.",
                symbol.Span);
            return null;
        }

        symbol.ObjectClassResolutionState = 1;
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
        ObjectClassParseResult parsed = new ObjectClassParser(
            this.GetRightSideTokens(symbol),
            this.Diagnostics,
            true).ParseObjectClassSyntax();
        symbol.DeclaredObjectClassSyntax = parsed.Syntax;
        if (!parsed.Success || parsed.Syntax == null || module == null)
        {
            symbol.ObjectClassResolutionState = 2;
            this.Success = false;
            return null;
        }

        ParsedObjectClassSyntax result = parsed.Syntax;
        if (result.ReferenceName.Length > 0)
        {
            TableSymbolBinding binding = this.FindSymbolBinding(
                module,
                result.ReferenceModule,
                result.ReferenceName,
                AsnAssignmentKind.ObjectClass);
            result = binding.Symbol == null
                ? result
                : this.ResolveObjectClassSymbol(binding.Symbol) ?? result;
            if (binding.Symbol == null || result.ObjectClass.Fields.Length == 0)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.UnknownTableObjectClass,
                    "The object class '" + parsed.Syntax.ReferenceName
                        + "' could not be resolved.",
                    parsed.Syntax.Span);
                symbol.ObjectClassResolutionState = 2;
                return null;
            }
        }

        if (result.ObjectClass.Name.Length == 0)
        {
            result.ObjectClass.Name = symbol.Name;
        }

        symbol.ResolvedObjectClassSyntax = result;
        symbol.ObjectClassResolutionState = 2;
        return result;
    }

    internal TableObjectSetResolution? ResolveConstraintObjectSet(
        AsnToken[] tokens,
        SymbolTable module,
        SymbolTableEntry typeDeclaration,
        ParsedObjectClassSyntax expectedClass,
        AsnSourceSpan? span)
    {
        TableSymbolBinding reference = this.FindObjectSetTokenBinding(tokens, module);
        if (reference.Symbol != null)
        {
            ParsedInformationObjectSetSyntax? syntax = this.ResolveObjectSetSymbol(
                reference.Symbol,
                expectedClass);
            if (syntax == null)
            {
                return null;
            }

            return new TableObjectSetResolution
            {
                Module = reference.Module,
                Symbol = reference.Symbol,
                ObjectClassSyntax = syntax.ObjectSet.ObjectClass == expectedClass.ObjectClass
                    ? expectedClass
                    : this.ResolveGovernorClass(reference.Symbol),
                ObjectSet = syntax.ObjectSet
            };
        }

        if (this.IsDefinedObjectSetReference(tokens))
        {
            if (this.IsFormalObjectSetParameter(tokens, typeDeclaration))
            {
                return null;
            }

            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidTableObjectSet,
                "The table constraint object set could not be resolved.",
                span);
            return null;
        }

        InformationObjectSetParseResult parsed = new InformationObjectParser(
            tokens,
            this.Diagnostics,
            true).ParseObjectSetSyntax(expectedClass);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != tokens.Length)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidTableObjectSet,
                "The inline table constraint object set is invalid.",
                span);
            return null;
        }

        parsed.Syntax.ObjectSet.ObjectClass = expectedClass.ObjectClass;
        InformationObjectBinder objectBinder = new InformationObjectBinder(
            this.ValueResolver);
        objectBinder.Tables = this.Tables;
        ObjectSetEvaluator evaluator = new ObjectSetEvaluator(
            this.ValueResolver,
            objectBinder);
        if (!evaluator.EvaluateInlineSet(
            parsed.Syntax,
            module,
            typeDeclaration,
            expectedClass,
            "inline-object-set:"
                + (span == null
                    ? "unknown"
                    : span.Start.SourceName + ":"
                        + span.Start.Offset.ToString(
                            System.Globalization.CultureInfo.InvariantCulture))))
        {
            this.Success = false;
        }

        return new TableObjectSetResolution
        {
            Module = module,
            ObjectClassSyntax = expectedClass,
            ObjectSet = parsed.Syntax.ObjectSet
        };
    }

    internal bool IsFormalObjectSetParameter(
        AsnToken[] tokens,
        SymbolTableEntry declaration)
    {
        if (tokens.Length != 1)
        {
            return false;
        }

        int index = 0;
        while (index < declaration.FormalParameters.Length)
        {
            if (declaration.FormalParameters[index].Name == tokens[0].Text
                && declaration.FormalParameters[index].Kind
                    == AsnParameterKind.ObjectSet)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal ParsedInformationObjectSetSyntax? ResolveObjectSetSymbol(
        SymbolTableEntry symbol,
        ParsedObjectClassSyntax expectedClass)
    {
        if (symbol.ObjectSetResolutionState == 2)
        {
            if (symbol.DeclaredObjectSetSyntax != null
                && symbol.DeclaredObjectSetSyntax.ObjectSet.ObjectClass
                    != expectedClass.ObjectClass)
            {
                this.AddObjectSetClassMismatch(symbol.Span);
                return null;
            }

            return symbol.DeclaredObjectSetSyntax;
        }

        if (symbol.ObjectSetResolutionState == 1)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidTableObjectSet,
                "The object-set reference cycle cannot govern a table constraint.",
                symbol.Span);
            return null;
        }

        symbol.ObjectSetResolutionState = 1;
        ParsedObjectClassSyntax? governor = this.ResolveGovernorClass(symbol);
        if (governor == null)
        {
            symbol.ObjectSetResolutionState = 2;
            return null;
        }

        if (governor.ObjectClass != expectedClass.ObjectClass)
        {
            this.AddObjectSetClassMismatch(symbol.Span);
            symbol.ObjectSetResolutionState = 2;
            return null;
        }

        AsnToken[] rightSide = this.GetRightSideTokens(symbol);
        InformationObjectSetParseResult parsed = new InformationObjectParser(
            rightSide,
            this.Diagnostics,
            true).ParseObjectSetSyntax(governor);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != rightSide.Length)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidTableObjectSet,
                "The object-set assignment '" + symbol.QualifiedName + "' is invalid.",
                symbol.Span);
            symbol.ObjectSetResolutionState = 2;
            return null;
        }

        parsed.Syntax.ObjectSet.Name = symbol.Name;
        parsed.Syntax.ObjectSet.ObjectClass = governor.ObjectClass;
        this.ResolveObjectSetMembers(parsed.Syntax, symbol, governor);
        symbol.DeclaredObjectSetSyntax = parsed.Syntax;
        symbol.ObjectSetResolutionState = 2;
        return parsed.Syntax;
    }

    internal ParsedObjectClassSyntax? ResolveGovernorClass(SymbolTableEntry symbol)
    {
        AssignmentHeadAlternativeSyntax? declared = symbol.DeclaredReference;
        if (declared == null || declared.GovernorTokens.Length == 0)
        {
            return null;
        }

        ObjectClassParseResult parsed = new ObjectClassParser(
            declared.GovernorTokens,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(symbol);
        if (!parsed.Success
            || parsed.Syntax == null
            || module == null
            || parsed.Syntax.ReferenceName.Length == 0)
        {
            return null;
        }

        TableSymbolBinding binding = this.FindSymbolBinding(
            module,
            parsed.Syntax.ReferenceModule,
            parsed.Syntax.ReferenceName,
            AsnAssignmentKind.ObjectClass);
        ParsedObjectClassSyntax? useful = this.ValueResolver.UsefulObjectClasses
            .ResolveUsefulClass(
                parsed.Syntax.ReferenceModule,
                parsed.Syntax.ReferenceName);
        return useful == null
            ? binding.Symbol == null ? null : this.ResolveObjectClassSymbol(binding.Symbol)
            : useful;
    }

    internal void AddObjectSetClassMismatch(AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            ConstraintDiagnosticCodes.TableObjectSetClassMismatch,
            "The constraining object set is not governed by the constrained field's object class.",
            span);
    }

    internal void ResolveObjectSetMembers(
        ParsedInformationObjectSetSyntax syntax,
        SymbolTableEntry setSymbol,
        ParsedObjectClassSyntax governor)
    {
        SymbolTable? module = this.ValueResolver.FindDeclaringModule(setSymbol);
        if (module == null)
        {
            return;
        }

        AsnInformationObject[] objects = syntax.ObjectSet.Objects;
        AsnInformationObject[] resolved = new AsnInformationObject[objects.Length];
        int index = 0;
        while (index < objects.Length)
        {
            if (objects[index].FieldSettings.Length == 0 && objects[index].Name.Length > 0)
            {
                ParsedInformationObjectSyntax? objectSyntax = this.ResolveObjectReference(
                    objects[index].Name,
                    module,
                    governor);
                resolved[index] = objectSyntax == null ? objects[index] : objectSyntax.Object;
            }
            else
            {
                resolved[index] = objects[index];
            }

            resolved[index].ObjectClass = governor.ObjectClass;
            index = index + 1;
        }

        syntax.ObjectSet.Objects = resolved;
        int rootLength = syntax.ObjectSet.RootObjects.Length;
        if (rootLength > resolved.Length)
        {
            rootLength = resolved.Length;
        }

        AsnInformationObject[] roots = new AsnInformationObject[rootLength];
        index = 0;
        while (index < roots.Length)
        {
            roots[index] = resolved[index];
            index = index + 1;
        }

        AsnInformationObject[] additions = new AsnInformationObject[
            resolved.Length - rootLength];
        index = 0;
        while (index < additions.Length)
        {
            additions[index] = resolved[rootLength + index];
            index = index + 1;
        }

        syntax.ObjectSet.RootObjects = roots;
        syntax.ObjectSet.AdditionalObjects = additions;
    }

    internal ParsedInformationObjectSyntax? ResolveObjectReference(
        string name,
        SymbolTable module,
        ParsedObjectClassSyntax governor)
    {
        string moduleName = "";
        string objectName = name;
        int separator = name.IndexOf('.', System.StringComparison.Ordinal);
        if (separator >= 0)
        {
            moduleName = name.Substring(0, separator);
            objectName = name.Substring(separator + 1);
        }

        TableSymbolBinding binding = this.FindSymbolBinding(
            module,
            moduleName,
            objectName,
            AsnAssignmentKind.Object);
        if (binding.Symbol == null)
        {
            return null;
        }

        return this.ResolveObjectSymbol(binding.Symbol, governor);
    }

    internal ParsedInformationObjectSyntax? ResolveObjectSymbol(
        SymbolTableEntry symbol,
        ParsedObjectClassSyntax expectedClass)
    {
        if (symbol.ObjectResolutionState == 2)
        {
            if (symbol.DeclaredObjectSyntax != null
                && symbol.DeclaredObjectSyntax.Object.ObjectClass
                    != expectedClass.ObjectClass)
            {
                this.AddObjectSetClassMismatch(symbol.Span);
                return null;
            }

            return symbol.DeclaredObjectSyntax;
        }

        if (symbol.ObjectResolutionState == 1)
        {
            return null;
        }

        symbol.ObjectResolutionState = 1;
        ParsedObjectClassSyntax? governor = this.ResolveGovernorClass(symbol);
        if (governor == null || governor.ObjectClass != expectedClass.ObjectClass)
        {
            this.AddObjectSetClassMismatch(symbol.Span);
            symbol.ObjectResolutionState = 2;
            return null;
        }

        AsnToken[] rightSide = this.GetRightSideTokens(symbol);
        InformationObjectParseResult parsed = new InformationObjectParser(
            rightSide,
            this.Diagnostics,
            true).ParseObjectSyntax(governor);
        if (!parsed.Success
            || parsed.Syntax == null
            || parsed.ConsumedTokenCount != rightSide.Length)
        {
            symbol.ObjectResolutionState = 2;
            this.Success = false;
            return null;
        }

        parsed.Syntax.Object.Name = symbol.Name;
        parsed.Syntax.Object.ObjectClass = governor.ObjectClass;
        symbol.DeclaredObjectSyntax = parsed.Syntax;
        symbol.ObjectResolutionState = 2;
        return parsed.Syntax;
    }

    internal TableSymbolBinding FindObjectSetTokenBinding(
        AsnToken[] tokens,
        SymbolTable module)
    {
        if (tokens.Length == 1)
        {
            return this.FindSymbolBinding(
                module,
                "",
                tokens[0].Text,
                AsnAssignmentKind.ObjectSet);
        }

        if (tokens.Length == 3 && tokens[1].Kind == AsnTokenKind.Dot)
        {
            return this.FindSymbolBinding(
                module,
                tokens[0].Text,
                tokens[2].Text,
                AsnAssignmentKind.ObjectSet);
        }

        return new TableSymbolBinding();
    }

    internal bool IsDefinedObjectSetReference(AsnToken[] tokens)
    {
        return tokens.Length == 1
            || (tokens.Length == 3 && tokens[1].Kind == AsnTokenKind.Dot);
    }

    internal TableSymbolBinding FindSymbolBinding(
        SymbolTable module,
        string referenceModule,
        string name,
        AsnAssignmentKind kind)
    {
        System.Collections.Generic.List<SymbolTableEntry> matches =
            new System.Collections.Generic.List<SymbolTableEntry>();
        System.Collections.Generic.List<SymbolTable> matchModules =
            new System.Collections.Generic.List<SymbolTable>();
        if (referenceModule.Length == 0)
        {
            SymbolTableEntry[] local = module.FindSymbols(name, kind);
            int localIndex = 0;
            while (localIndex < local.Length)
            {
                matches.Add(local[localIndex]);
                matchModules.Add(module);
                localIndex = localIndex + 1;
            }
        }

        int importIndex = 0;
        while (importIndex < module.ImportResolutions.Length)
        {
            ImportResolution import = module.ImportResolutions[importIndex];
            if (referenceModule.Length == 0
                || (import.TargetModule != null
                    && import.TargetModule.ModuleName == referenceModule))
            {
                int bindingIndex = 0;
                while (bindingIndex < import.Symbols.Length)
                {
                    ImportedSymbolBinding binding = import.Symbols[bindingIndex];
                    if (binding.Target != null
                        && binding.Target.Name == name
                        && binding.Kind == kind)
                    {
                        matches.Add(binding.Target);
                        matchModules.Add(import.TargetModule == null
                            ? module
                            : import.TargetModule);
                    }

                    bindingIndex = bindingIndex + 1;
                }
            }

            importIndex = importIndex + 1;
        }

        return new TableSymbolBinding
        {
            Module = matches.Count == 1 ? matchModules[0] : null,
            Symbol = matches.Count == 1 ? matches[0] : null,
            Ambiguous = matches.Count > 1
        };
    }

    internal AsnToken[] GetRightSideTokens(SymbolTableEntry symbol)
    {
        AsnToken[] declarationTokens = symbol.GetSemanticDeclarationTokens();
        int start = 0;
        while (start < declarationTokens.Length
            && declarationTokens[start].Kind != AsnTokenKind.Assignment)
        {
            start = start + 1;
        }

        start = start + 1;
        int end = declarationTokens.Length;
        while (end > start
            && declarationTokens[end - 1].Kind == AsnTokenKind.Semicolon)
        {
            end = end - 1;
        }

        if (start < 0 || start >= end)
        {
            return [];
        }

        AsnToken[] result = new AsnToken[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = declarationTokens[start + index];
            index = index + 1;
        }

        return result;
    }

    internal TableAtNotationResolution[] ResolveAtNotations(
        ParsedGeneralConstraintSyntax source,
        ResolvedObjectClassFieldSyntax constrainedField,
        TableObjectSetResolution objectSet,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes)
    {
        System.Collections.Generic.List<TableAtNotationResolution> results =
            new System.Collections.Generic.List<TableAtNotationResolution>();
        if (enclosingTypes.Count == 0)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidAtNotationContext,
                "A component-relation constraint must be textually enclosed by a SEQUENCE or SET type.",
                source.Span);
            return [];
        }

        int index = 0;
        while (index < source.AtNotations.Length)
        {
            TableAtNotationResolution? result = this.ResolveAtNotation(
                source.AtNotations[index],
                constrainedField,
                objectSet,
                enclosingTypes);
            if (result != null)
            {
                results.Add(result);
            }

            index = index + 1;
        }

        return results.ToArray();
    }

    internal TableAtNotationResolution? ResolveAtNotation(
        ParsedAtNotationSyntax syntax,
        ResolvedObjectClassFieldSyntax constrainedField,
        TableObjectSetResolution objectSet,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes)
    {
        int startIndex = syntax.Relative
            ? this.FindRelativeAtStart(enclosingTypes, syntax.Levels)
            : this.FindOutermostAtStart(enclosingTypes);
        if (startIndex < 0)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidAtNotationContext,
                "The at-notation level does not identify an enclosing SEQUENCE, SET, or CHOICE.",
                syntax.Span);
            return null;
        }

        ResolvedTypeSyntax current = enclosingTypes[startIndex];
        System.Collections.Generic.List<ResolvedComponentSyntax> components =
            new System.Collections.Generic.List<ResolvedComponentSyntax>();
        int pathIndex = 0;
        while (pathIndex < syntax.ComponentIdentifiers.Length)
        {
            ResolvedComponentSyntax? component = this.FindComponent(
                current,
                syntax.ComponentIdentifiers[pathIndex]);
            if (component == null || component.TypeResolution == null)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.InvalidAtNotationPath,
                    "The at-notation component path cannot be resolved.",
                    syntax.Span);
                return null;
            }

            components.Add(component);
            current = component.TypeResolution;
            if (pathIndex + 1 < syntax.ComponentIdentifiers.Length
                && current.EffectiveKind != AsnTypeKind.Sequence
                && current.EffectiveKind != AsnTypeKind.Set
                && current.EffectiveKind != AsnTypeKind.Choice)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.InvalidAtNotationPath,
                    "Only a SEQUENCE, SET, or CHOICE component can continue an at-notation path.",
                    syntax.Span);
                return null;
            }

            pathIndex = pathIndex + 1;
        }

        ResolvedObjectClassFieldSyntax? referencedField =
            this.ResolveObjectClassField(current, syntax.Span);
        if (referencedField == null)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidAtNotationField,
                "An at-notation must reference an object-class value or value-set field.",
                syntax.Span);
            return null;
        }

        if (referencedField.ObjectClassSyntax != constrainedField.ObjectClassSyntax)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.AtNotationClassMismatch,
                "The referencing and referenced components must use the same object class.",
                syntax.Span);
            return null;
        }

        AsnObjectClassField? field = referencedField.Field;
        if (field == null
            || (field.Kind != AsnObjectFieldKind.FixedTypeValue
                && field.Kind != AsnObjectFieldKind.VariableTypeValue
                && field.Kind != AsnObjectFieldKind.FixedTypeValueSet
                && field.Kind != AsnObjectFieldKind.VariableTypeValueSet))
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidAtNotationField,
                "An at-notation must reference an object-class value or value-set field.",
                syntax.Span);
            return null;
        }

        if (!this.HasConstraintForObjectSet(current, objectSet.Symbol))
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.AtNotationObjectSetMismatch,
                "The referenced component must be constrained by the same object set.",
                syntax.Span);
            return null;
        }

        return new TableAtNotationResolution
        {
            SourceSyntax = syntax,
            StartType = enclosingTypes[startIndex],
            Components = components.ToArray(),
            ReferencedType = current,
            ReferencedField = referencedField
        };
    }

    internal int FindRelativeAtStart(
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes,
        int levels)
    {
        int start = enclosingTypes.Count - 1;
        while (start >= 0
            && enclosingTypes[start].EffectiveKind != AsnTypeKind.Sequence
            && enclosingTypes[start].EffectiveKind != AsnTypeKind.Set)
        {
            start = start - 1;
        }

        if (start < 0)
        {
            return -1;
        }

        int index = start;
        int remaining = levels;
        while (remaining > 0)
        {
            index = index - 1;
            if (index < 0)
            {
                return -1;
            }

            remaining = remaining - 1;
        }

        return index;
    }

    internal int FindOutermostAtStart(
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes)
    {
        int index = 0;
        while (index < enclosingTypes.Count)
        {
            if (enclosingTypes[index].EffectiveKind == AsnTypeKind.Sequence
                || enclosingTypes[index].EffectiveKind == AsnTypeKind.Set
                || enclosingTypes[index].EffectiveKind == AsnTypeKind.Choice)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ResolvedComponentSyntax? FindComponent(
        ResolvedTypeSyntax type,
        string name)
    {
        int index = 0;
        while (index < type.Components.Length)
        {
            if (type.Components[index].Component != null
                && type.Components[index].Component?.Name == name)
            {
                return type.Components[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool HasConstraintForObjectSet(
        ResolvedTypeSyntax type,
        SymbolTableEntry? objectSetSymbol)
    {
        if (objectSetSymbol == null || type.EffectiveConstraint == null)
        {
            return false;
        }

        return this.GraphReferencesObjectSet(type.EffectiveConstraint, objectSetSymbol);
    }

    internal bool GraphReferencesObjectSet(
        NormalizedConstraintGraph graph,
        SymbolTableEntry objectSetSymbol)
    {
        if (graph.RootPartition != null
            && graph.RootPartition.Expression != null
            && this.NodeReferencesObjectSet(
                graph.RootPartition.Expression,
                objectSetSymbol))
        {
            return true;
        }

        return graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null
            && this.NodeReferencesObjectSet(
                graph.ExtensionPartition.Expression,
                objectSetSymbol);
    }

    internal bool NodeReferencesObjectSet(
        NormalizedConstraintNode node,
        SymbolTableEntry objectSetSymbol)
    {
        if ((node.Kind == AsnConstraintKind.SimpleTable
                || node.Kind == AsnConstraintKind.ComponentRelation)
            && node.SourceGeneralConstraint != null
            && this.ObjectSetTokensReferToSymbol(
                node.SourceGeneralConstraint.ObjectSetTokens,
                objectSetSymbol))
        {
            return true;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            if (this.NodeReferencesObjectSet(node.Children[childIndex], objectSetSymbol))
            {
                return true;
            }

            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            if (this.GraphReferencesObjectSet(
                node.NestedConstraints[nestedIndex],
                objectSetSymbol))
            {
                return true;
            }

            nestedIndex = nestedIndex + 1;
        }

        return false;
    }

    internal bool ObjectSetTokensReferToSymbol(
        AsnToken[] tokens,
        SymbolTableEntry objectSetSymbol)
    {
        if (tokens.Length == 1)
        {
            return tokens[0].Text == objectSetSymbol.Name;
        }

        SymbolTable? module = this.ValueResolver.FindDeclaringModule(objectSetSymbol);
        return tokens.Length == 3
            && tokens[1].Kind == AsnTokenKind.Dot
            && tokens[2].Text == objectSetSymbol.Name
            && module != null
            && tokens[0].Text == module.ModuleName;
    }

    internal ResolvedTypeSyntax? FindConstrainedType(
        TableAtNotationResolution[] atNotations,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes)
    {
        if (atNotations.Length == 0)
        {
            return null;
        }

        int constrainedIndex = enclosingTypes.Count - 1;
        int notationIndex = 0;
        while (notationIndex < atNotations.Length)
        {
            ResolvedTypeSyntax? startType = atNotations[notationIndex].StartType;
            int index = startType == null ? -1 : enclosingTypes.IndexOf(startType);
            if (index >= 0 && index < constrainedIndex)
            {
                constrainedIndex = index;
            }

            notationIndex = notationIndex + 1;
        }

        return constrainedIndex < 0 || constrainedIndex >= enclosingTypes.Count
            ? null
            : enclosingTypes[constrainedIndex];
    }

    internal void ResolveLookupComponents(
        TableAtNotationResolution[] atNotations,
        ResolvedTypeSyntax? constrainedType,
        System.Collections.Generic.List<ResolvedTypeSyntax> enclosingTypes,
        AsnSourceSpan? span)
    {
        if (constrainedType == null)
        {
            return;
        }

        int constrainedIndex = enclosingTypes.IndexOf(constrainedType);
        int notationIndex = 0;
        while (notationIndex < atNotations.Length)
        {
            TableAtNotationResolution notation = atNotations[notationIndex];
            int startIndex = notation.StartType == null
                ? -1
                : enclosingTypes.IndexOf(notation.StartType);
            System.Collections.Generic.List<ResolvedComponentSyntax> lookup =
                new System.Collections.Generic.List<ResolvedComponentSyntax>();
            bool valid = constrainedIndex >= 0 && startIndex >= constrainedIndex;
            int index = constrainedIndex;
            while (valid && index < startIndex)
            {
                ResolvedComponentSyntax[] matches = this.FindContainingComponents(
                    enclosingTypes[index],
                    enclosingTypes[index + 1]);
                bool collectionElement = enclosingTypes[index].InnerType
                    == enclosingTypes[index + 1]
                    && (enclosingTypes[index].EffectiveKind == AsnTypeKind.SequenceOf
                        || enclosingTypes[index].EffectiveKind == AsnTypeKind.SetOf);
                if (matches.Length == 0 && !collectionElement)
                {
                    this.AddDiagnostic(
                        ConstraintDiagnosticCodes.MissingDispatchKey,
                        "The component-relation key path cannot be rooted at the constrained type.",
                        span);
                    valid = false;
                }
                else if (matches.Length > 1)
                {
                    this.AddDiagnostic(
                        ConstraintDiagnosticCodes.AmbiguousDispatchKey,
                        "The component-relation key path has more than one route through the constrained type.",
                        span);
                    valid = false;
                }
                else if (matches.Length == 1)
                {
                    lookup.Add(matches[0]);
                }

                index = index + 1;
            }

            if (valid)
            {
                index = 0;
                while (index < notation.Components.Length)
                {
                    lookup.Add(notation.Components[index]);
                    index = index + 1;
                }

                notation.LookupComponents = lookup.ToArray();
            }

            notationIndex = notationIndex + 1;
        }
    }

    internal ResolvedComponentSyntax[] FindContainingComponents(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax child)
    {
        System.Collections.Generic.List<ResolvedComponentSyntax> matches =
            new System.Collections.Generic.List<ResolvedComponentSyntax>();
        int index = 0;
        while (index < parent.Components.Length)
        {
            if (parent.Components[index].TypeResolution == child)
            {
                matches.Add(parent.Components[index]);
            }

            index = index + 1;
        }

        return matches.ToArray();
    }

    internal OpenTypeDispatchMap BuildDispatchMap(
        TableObjectSetResolution objectSet,
        ResolvedObjectClassFieldSyntax constrainedField,
        TableAtNotationResolution[] atNotations,
        AsnConstraintKind kind,
        string stableId,
        AsnSourceSpan? span)
    {
        OpenTypeDispatchMap map = new OpenTypeDispatchMap
        {
            Kind = kind,
            StableId = stableId,
            KeyAtNotations = this.SelectUniqueAtNotations(atNotations)
        };
        if (objectSet.ObjectSet == null
            || constrainedField.ObjectClassSyntax == null
            || constrainedField.Field == null
            || constrainedField.Field.Kind != AsnObjectFieldKind.Type)
        {
            return map;
        }

        AsnObjectClassField? unique = this.FindUniqueField(
            constrainedField.ObjectClassSyntax);
        if (unique == null)
        {
            return map;
        }

        if (kind == AsnConstraintKind.ComponentRelation
            && map.KeyAtNotations.Length == 0)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.MissingDispatchKey,
                "A component-relation dispatch map requires an at-notation that identifies the UNIQUE field.",
                span);
        }

        System.Collections.Generic.List<AsnOpenTypeDispatchEntry> entries =
            new System.Collections.Generic.List<AsnOpenTypeDispatchEntry>();
        System.Collections.Generic.List<OpenTypeDispatchBucket> buckets =
            new System.Collections.Generic.List<OpenTypeDispatchBucket>();
        int index = 0;
        while (index < objectSet.ObjectSet.Objects.Length)
        {
            AsnInformationObject item = objectSet.ObjectSet.Objects[index];
            AsnObjectFieldSetting? keySetting = this.FindObjectSetting(item, unique.Name);
            AsnObjectFieldSetting? typeSetting = this.FindObjectSetting(
                item,
                constrainedField.Field.Name);
            if (keySetting == null || keySetting.Value == null)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.MissingDispatchKey,
                    "Information object '" + item.StableId
                        + "' has no value for UNIQUE field '" + unique.Name + "'.",
                    span);
                index = index + 1;
                continue;
            }

            if (typeSetting == null || typeSetting.Type == null)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.MissingDispatchType,
                    "Information object '" + item.StableId
                        + "' has no type setting for field '"
                        + constrainedField.Field.Name + "'.",
                    span);
                index = index + 1;
                continue;
            }

            NormalizedDispatchKey normalized = map.Normalizer.NormalizeKey(
                keySetting.Value);
            if (!normalized.Success)
            {
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.MissingDispatchKey,
                    "Information object '" + item.StableId
                        + "' has no normalizable UNIQUE value.",
                    span);
                index = index + 1;
                continue;
            }

            AsnOpenTypeDispatchEntry entry = new AsnOpenTypeDispatchEntry
            {
                Key = keySetting.Value,
                Type = typeSetting.Type,
                StableId = (objectSet.Symbol == null
                        ? "inline-object-set"
                        : objectSet.Symbol.QualifiedName)
                    + ":" + constrainedField.Field.Name
                    + ":" + normalized.Identity
            };
            entries.Add(entry);
            if (map.Index.ContainsKey(normalized.Identity))
            {
                OpenTypeDispatchBucket bucket = map.Index[normalized.Identity];
                AsnOpenTypeDispatchEntry[] expanded =
                    new AsnOpenTypeDispatchEntry[bucket.Entries.Length + 1];
                int entryIndex = 0;
                while (entryIndex < bucket.Entries.Length)
                {
                    expanded[entryIndex] = bucket.Entries[entryIndex];
                    entryIndex = entryIndex + 1;
                }

                expanded[expanded.Length - 1] = entry;
                bucket.Entries = expanded;
                this.AddDiagnostic(
                    ConstraintDiagnosticCodes.AmbiguousDispatchKey,
                    "The normalized UNIQUE value identifies more than one information object.",
                    span);
            }
            else
            {
                OpenTypeDispatchBucket bucket = new OpenTypeDispatchBucket
                {
                    Identity = normalized.Identity,
                    Key = keySetting.Value,
                    Entries = [entry]
                };
                map.Index.Add(normalized.Identity, bucket);
                buckets.Add(bucket);
            }

            index = index + 1;
        }

        map.Entries = entries.ToArray();
        map.Buckets = buckets.ToArray();
        return map;
    }

    internal TableAtNotationResolution[] SelectUniqueAtNotations(
        TableAtNotationResolution[] atNotations)
    {
        System.Collections.Generic.List<TableAtNotationResolution> result =
            new System.Collections.Generic.List<TableAtNotationResolution>();
        int index = 0;
        while (index < atNotations.Length)
        {
            AsnObjectClassField? field = atNotations[index].ReferencedField == null
                ? null
                : atNotations[index].ReferencedField?.Field;
            if (field != null && field.Unique)
            {
                result.Add(atNotations[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal AsnObjectClassField? FindUniqueField(ParsedObjectClassSyntax objectClass)
    {
        int index = 0;
        while (index < objectClass.ObjectClass.Fields.Length)
        {
            if (objectClass.ObjectClass.Fields[index].Unique)
            {
                return objectClass.ObjectClass.Fields[index];
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

    internal void MergeDispatchEntries(
        AsnInformationObjectSet objectSet,
        AsnOpenTypeDispatchEntry[] entries)
    {
        System.Collections.Generic.List<AsnOpenTypeDispatchEntry> merged =
            new System.Collections.Generic.List<AsnOpenTypeDispatchEntry>();
        int index = 0;
        while (index < objectSet.DispatchEntries.Length)
        {
            merged.Add(objectSet.DispatchEntries[index]);
            index = index + 1;
        }

        index = 0;
        while (index < entries.Length)
        {
            bool found = false;
            int candidate = 0;
            while (candidate < merged.Count)
            {
                if (merged[candidate].StableId == entries[index].StableId)
                {
                    found = true;
                }

                candidate = candidate + 1;
            }

            if (!found)
            {
                merged.Add(entries[index]);
            }

            index = index + 1;
        }

        objectSet.DispatchEntries = merged.ToArray();
    }

    internal string CreateRelationshipId(
        ResolvedTypeSyntax type,
        SymbolTableEntry? objectSet,
        ParsedGeneralConstraintSyntax source)
    {
        SymbolTableEntry? declaration = type.SourceDeclaration == null
            ? type.EffectiveDeclaration
            : type.SourceDeclaration;
        string owner = declaration == null ? "anonymous" : declaration.QualifiedName;
        string set = objectSet == null
            ? this.JoinTokenText(source.ObjectSetTokens)
            : objectSet.QualifiedName;
        AsnSourceSpan? span = source.Span;
        string offset = span == null
            ? "unknown"
            : span.Start.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return owner + ":table:" + set + ":" + offset;
    }

    internal string JoinTokenText(AsnToken[] tokens)
    {
        string result = "";
        int index = 0;
        while (index < tokens.Length)
        {
            result = result + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal bool IsStructuralConstruction(ResolvedTypeSyntax type)
    {
        return type.EffectiveKind == AsnTypeKind.Sequence
            || type.EffectiveKind == AsnTypeKind.Set
            || type.EffectiveKind == AsnTypeKind.Choice
            || type.EffectiveKind == AsnTypeKind.SequenceOf
            || type.EffectiveKind == AsnTypeKind.SetOf;
    }

    internal void AddDiagnostic(int code, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Constraint,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
