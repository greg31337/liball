namespace asn1;

internal class EcnLinkCompiler
{
    internal DiagnosticBag Diagnostics;
    internal EcnSymbolTable[] Tables = [];
    internal bool Success = true;

    internal EcnLinkCompiler(DiagnosticBag diagnostics)
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
                    this.ResolveObjectLinks(
                        symbol.ObjectDefinition,
                        tables[tableIndex]);
                }

                objectIndex = objectIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void ResolveObjectLinks(
        EcnEncodingObjectDefinition definition,
        EcnSymbolTable table)
    {
        if (definition.LinkResolutionState == 2)
        {
            return;
        }

        if (definition.LinkResolutionState == 1)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.LinkCycle,
                "The encoding object '" + definition.Name
                    + "' has a cyclic encoding-link definition.",
                definition.Span);
            return;
        }

        definition.LinkResolutionState = 1;
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
                this.ResolveObjectLinks(referenced, referencedTable);
                definition.Links = this.CopyLinks(referenced.Links);
                definition.Determinants = this.InstantiateInheritedDeterminants(
                    definition,
                    referenced);
                definition.Mappings = this.CopyMappings(referenced.Mappings);
                if (!this.ValidateRelationships(definition.Determinants))
                {
                    definition.Valid = false;
                }
                if (!referenced.Valid)
                {
                    definition.Valid = false;
                }
            }
        }
        else
        {
            this.CompileLocalLinks(definition, table);
        }

        definition.LinkResolutionState = 2;
    }

    internal EcnDeterminantRelationship[] InstantiateInheritedDeterminants(
        EcnEncodingObjectDefinition definition,
        EcnEncodingObjectDefinition referenced)
    {
        EcnDeterminantRelationship[] source = referenced.Determinants;
        EcnDeterminantRelationship[] result =
            new EcnDeterminantRelationship[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            EcnFieldPath? determinant = source[index].DeterminantPath;
            EcnFieldPath? substituted = determinant;
            if (determinant != null && determinant.ParameterSymbol != null)
            {
                int parameterIndex =
                    determinant.ParameterSymbol.DeclarationOrder;
                if (parameterIndex
                    < definition.ReferenceActualParameters.Length)
                {
                    EcnActualParameter actual =
                        definition.ReferenceActualParameters[parameterIndex];
                    string pathText = this.AppendPathSuffix(
                        actual.ReferenceName,
                        determinant.Segments);
                    substituted = this.ResolveFieldPath(
                        pathText,
                        definition,
                        actual.Span);
                }
            }

            result[index] = new EcnDeterminantRelationship
            {
                ControlKind = source[index].ControlKind,
                Mode = source[index].Mode,
                Direction = source[index].Direction,
                ControlledPath = source[index].ControlledPath,
                DeterminantPath = substituted,
                Mapping = source[index].Mapping,
                EncodingLink = source[index].EncodingLink,
                Order = source[index].Order,
                Valid = source[index].Valid
                    && substituted != null && substituted.Valid,
                Span = source[index].Span
            };
            index = index + 1;
        }

        return result;
    }

    internal string AppendPathSuffix(string actual, string[] formalSegments)
    {
        if (formalSegments.Length <= 1)
        {
            return actual;
        }

        return actual + "." + this.JoinSegmentRange(
            formalSegments,
            1,
            formalSegments.Length - 1);
    }

    internal void CompileLocalLinks(
        EcnEncodingObjectDefinition definition,
        EcnSymbolTable table)
    {
        EcnAssignmentSyntax? assignment = definition.Symbol == null
            ? null
            : definition.Symbol.Assignment;
        if (assignment == null)
        {
            return;
        }

        definition.Mappings = this.CompileMappings(assignment.Clauses);
        definition.Links = this.ParseEncodingLinks(
            assignment.BodyTokens,
            definition,
            table);
        System.Collections.Generic.List<EcnDeterminantRelationship> relations =
            new System.Collections.Generic.List<EcnDeterminantRelationship>();
        this.AddDirectRelationships(definition, relations);
        int linkIndex = 0;
        while (linkIndex < definition.Links.Length)
        {
            this.AddInstantiatedRelationships(
                definition,
                definition.Links[linkIndex],
                relations,
                table);
            if (!definition.Links[linkIndex].Valid)
            {
                definition.Valid = false;
            }

            linkIndex = linkIndex + 1;
        }

        definition.Determinants = relations.ToArray();
        this.AssignMappings(definition);
        if (!this.ValidateLinkTargets(definition.Links))
        {
            definition.Valid = false;
        }

        if (!this.ValidateRelationships(definition.Determinants))
        {
            definition.Valid = false;
        }

        int mappingIndex = 0;
        while (mappingIndex < definition.Mappings.Length)
        {
            if (!definition.Mappings[mappingIndex].Valid)
            {
                definition.Valid = false;
            }

            mappingIndex = mappingIndex + 1;
        }
    }

    internal EcnEncodingLink[] ParseEncodingLinks(
        AsnToken[] tokens,
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table)
    {
        System.Collections.Generic.List<EcnEncodingLink> links =
            new System.Collections.Generic.List<EcnEncodingLink>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == "USE"
                || tokens[index].Text == "USE-SET")
            {
                EcnEncodingLink link = this.ParseEncodingLinkAt(
                    tokens,
                    index,
                    owner,
                    table,
                    links.Count);
                links.Add(link);
            }

            index = index + 1;
        }

        return links.ToArray();
    }

    internal EcnEncodingLink ParseEncodingLinkAt(
        AsnToken[] tokens,
        int useIndex,
        EcnEncodingObjectDefinition owner,
        EcnSymbolTable table,
        int order)
    {
        int referenceIndex = useIndex + 1;
        while (referenceIndex < tokens.Length
            && this.IsLinkRole(tokens[referenceIndex].Text))
        {
            referenceIndex = referenceIndex + 1;
        }

        EcnEncodingLink link = new EcnEncodingLink
        {
            Order = order,
            Valid = true,
            Span = tokens[useIndex].Span
        };
        if (referenceIndex >= tokens.Length
            || (!this.IsReferenceToken(tokens[referenceIndex])
                && !EcnEncodingObjectSetCatalog.IsBuiltIn(
                    tokens[referenceIndex].Text)))
        {
            link.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidLink,
                "USE and USE-SET require an encoding class, object, or object-set reference.",
                tokens[useIndex].Span);
            return link;
        }

        int referenceEnd = this.FindReferenceNameEnd(tokens, referenceIndex);
        link.ReferenceName = this.JoinCompact(
            tokens,
            referenceIndex,
            referenceEnd - referenceIndex);
        link.Tokens = this.CopyTokenRange(
            tokens,
            useIndex,
            referenceEnd - useIndex);
        string targetText = this.ReadLinkTarget(tokens, useIndex);
        link.TargetPath = this.ResolveFieldPath(
            targetText,
            owner,
            tokens[useIndex].Span);
        this.ResolveLinkReference(link, table, owner);

        if (link.ReferenceSymbol != null
            && referenceEnd < tokens.Length
            && tokens[referenceEnd].Kind == AsnTokenKind.LeftBrace)
        {
            EcnActualParameterBinder binder = new EcnActualParameterBinder(
                this.Diagnostics);
            link.ActualParameters = binder.BindActualParameters(
                tokens,
                referenceEnd,
                table,
                owner.Symbol == null ? new EcnSymbol() : owner.Symbol,
                link.ReferenceSymbol);
            if (!binder.Success)
            {
                link.Valid = false;
            }
        }
        else if (link.ReferenceSymbol != null
            && link.ReferenceSymbol.Scope != null
            && link.ReferenceSymbol.Scope.Symbols.Length > 0)
        {
            link.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidLink,
                "The encoding reference '" + link.ReferenceName
                    + "' requires actual parameters.",
                link.Span);
        }

        if (link.TargetPath == null || !link.TargetPath.Valid)
        {
            link.Valid = false;
        }

        return link;
    }

    internal bool IsLinkRole(string text)
    {
        return text == "OPTIONAL-ENCODING"
            || text == "REPETITION-ENCODING"
            || text == "ALTERNATIVE-ENCODING"
            || text == "ENCODING"
            || text == "STRUCTURE";
    }

    internal void ResolveLinkReference(
        EcnEncodingLink link,
        EcnSymbolTable table,
        EcnEncodingObjectDefinition owner)
    {
        EcnSymbol? parameter = this.FindLinkParameter(
            owner.Symbol,
            link.ReferenceName);
        if (parameter != null)
        {
            link.ReferenceSymbol = parameter;
            if (parameter.ParameterKind == EcnParameterKind.EncodingClass)
            {
                link.Kind = EcnEncodingLinkKind.EncodingClass;
            }
            else if (parameter.ParameterKind
                == EcnParameterKind.EncodingObject)
            {
                link.Kind = EcnEncodingLinkKind.EncodingObject;
            }
            else
            {
                link.Kind = EcnEncodingLinkKind.EncodingObjectSet;
            }

            return;
        }

        EcnSymbol? symbol = this.FindReferencedSymbol(
            table,
            link.ReferenceName,
            EcnSymbolKind.EncodingObject);
        if (symbol != null)
        {
            link.Kind = EcnEncodingLinkKind.EncodingObject;
            link.ReferenceSymbol = symbol;
            return;
        }

        symbol = this.FindReferencedSymbol(
            table,
            link.ReferenceName,
            EcnSymbolKind.EncodingObjectSet);
        if (symbol != null)
        {
            link.Kind = EcnEncodingLinkKind.EncodingObjectSet;
            link.ReferenceSymbol = symbol;
            return;
        }

        symbol = this.FindReferencedSymbol(
            table,
            link.ReferenceName,
            EcnSymbolKind.EncodingClass);
        if (symbol == null)
        {
            symbol = this.FindReferencedSymbol(
                table,
                link.ReferenceName,
                EcnSymbolKind.EncodingStructure);
        }
        if (symbol != null)
        {
            link.Kind = EcnEncodingLinkKind.EncodingClass;
            link.ReferenceSymbol = symbol;
            return;
        }

        if (EcnBuiltInClassCatalog.FindClass(link.ReferenceName) != null)
        {
            link.Kind = EcnEncodingLinkKind.EncodingClass;
            link.BuiltIn = true;
            return;
        }

        if (EcnEncodingObjectSetCatalog.IsBuiltIn(link.ReferenceName))
        {
            link.Kind = EcnEncodingLinkKind.EncodingObjectSet;
            link.PredeclaredObjectSet = EcnEncodingObjectSetCatalog.FindSet(
                link.ReferenceName);
            link.BuiltIn = true;
            return;
        }

        link.Valid = false;
        this.AddDiagnostic(
            EcnDiagnosticCodes.UnknownLinkTarget,
            "The encoding link target '" + link.ReferenceName
                + "' could not be resolved.",
            link.Span);
    }

    internal EcnSymbol? FindLinkParameter(
        EcnSymbol? owner,
        string name)
    {
        EcnScope? scope = owner == null ? null : owner.Scope;
        EcnSymbol? parameter = scope == null
            ? null
            : scope.FindLocalSymbol(name);
        if (parameter == null)
        {
            return null;
        }

        return parameter.ParameterKind == EcnParameterKind.EncodingClass
                || parameter.ParameterKind == EcnParameterKind.EncodingObject
                || parameter.ParameterKind
                    == EcnParameterKind.EncodingObjectSet
            ? parameter
            : null;
    }

    internal EcnSymbol? FindReferencedSymbol(
        EcnSymbolTable table,
        string reference,
        EcnSymbolKind kind)
    {
        int dot = reference.IndexOf(
            ".",
            System.StringComparison.Ordinal);
        string moduleName = dot < 0
            ? ""
            : reference.Substring(0, dot);
        string name = dot < 0
            ? reference
            : reference.Substring(dot + 1);
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

    internal string ReadLinkTarget(AsnToken[] tokens, int useIndex)
    {
        int start = useIndex;
        int index = useIndex - 1;
        while (index >= 0 && this.IsPathToken(tokens[index]))
        {
            start = index;
            index = index - 1;
        }

        return start == useIndex
            ? "$value"
            : this.JoinCompact(tokens, start, useIndex - start);
    }

    internal bool IsPathToken(AsnToken token)
    {
        return this.IsReferenceToken(token)
            || token.Kind == AsnTokenKind.Dot
            || token.Kind == AsnTokenKind.Slash;
    }

    internal bool IsReferenceToken(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.EncodingReference
            || token.Kind == AsnTokenKind.EncodingClassReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.ObjectSetReference;
    }

    internal int FindReferenceNameEnd(AsnToken[] tokens, int start)
    {
        int end = start + 1;
        while (end + 1 < tokens.Length
            && tokens[end].Kind == AsnTokenKind.Dot
            && this.IsReferenceToken(tokens[end + 1]))
        {
            end = end + 2;
        }

        return end;
    }

    internal void AddDirectRelationships(
        EcnEncodingObjectDefinition owner,
        System.Collections.Generic.List<EcnDeterminantRelationship> relations)
    {
        EcnConstructorDefinition? constructor = owner.Constructor;
        if (constructor == null)
        {
            return;
        }

        int index = 0;
        while (index < constructor.ControlDependencies.Length)
        {
            EcnControlDependency control =
                constructor.ControlDependencies[index];
            if (control.Reference.Length > 0)
            {
                EcnFieldPath controlled = this.ResolveFieldPath(
                    "$control" + index,
                    owner,
                    control.Span);
                controlled.Root = EcnFieldPathRoot.Synthetic;
                relations.Add(new EcnDeterminantRelationship
                {
                    ControlKind = control.Kind,
                    Mode = control.Mode,
                    Direction = this.GetLinkDirection(control.Mode),
                    ControlledPath = controlled,
                    DeterminantPath = this.ResolveFieldPath(
                        control.Reference,
                        owner,
                        control.Span),
                    Order = relations.Count,
                    Valid = control.Valid,
                    Span = control.Span
                });
            }

            index = index + 1;
        }
    }

    internal void AddInstantiatedRelationships(
        EcnEncodingObjectDefinition owner,
        EcnEncodingLink link,
        System.Collections.Generic.List<EcnDeterminantRelationship> relations,
        EcnSymbolTable table)
    {
        if (link.Kind != EcnEncodingLinkKind.EncodingObject
            || link.ReferenceSymbol == null
            || link.ReferenceSymbol.ObjectDefinition == null)
        {
            return;
        }

        EcnEncodingObjectDefinition referenced =
            link.ReferenceSymbol.ObjectDefinition;
        EcnSymbolTable referencedTable = this.FindTableForSymbol(
            referenced.Symbol,
            table);
        this.ResolveObjectLinks(referenced, referencedTable);
        EcnConstructorDefinition? constructor = referenced.Constructor;
        if (constructor == null)
        {
            return;
        }

        int index = 0;
        while (index < constructor.ControlDependencies.Length)
        {
            EcnControlDependency control =
                constructor.ControlDependencies[index];
            if (control.Reference.Length > 0)
            {
                string determinant = this.SubstituteReferenceActual(
                    control.Reference,
                    link,
                    referenced);
                EcnFieldPath determinantPath = this.ResolveFieldPath(
                    determinant,
                    owner,
                    link.Span);
                EcnDeterminantRelationship relation =
                    new EcnDeterminantRelationship
                    {
                        ControlKind = control.Kind,
                        Mode = control.Mode,
                        Direction = this.GetLinkDirection(control.Mode),
                        ControlledPath = link.TargetPath,
                        DeterminantPath = determinantPath,
                        EncodingLink = link,
                        Mapping = this.FindReferencedControlMapping(
                            referenced,
                            index),
                        Order = relations.Count,
                        Valid = link.Valid && control.Valid
                            && determinantPath.Valid,
                        Span = link.Span
                    };
                relations.Add(relation);
                if (link.Mapping == null && relation.Mapping != null)
                {
                    link.Mapping = relation.Mapping;
                }
                if (!relation.Valid)
                {
                    link.Valid = false;
                }
            }

            index = index + 1;
        }
    }

    internal EcnValueMapping? FindReferencedControlMapping(
        EcnEncodingObjectDefinition referenced,
        int controlIndex)
    {
        int index = 0;
        while (index < referenced.Determinants.Length)
        {
            EcnFieldPath? controlled =
                referenced.Determinants[index].ControlledPath;
            if (controlled != null
                && controlled.Root == EcnFieldPathRoot.Synthetic
                && controlled.Text == "$control" + controlIndex)
            {
                return referenced.Determinants[index].Mapping;
            }

            index = index + 1;
        }

        return null;
    }

    internal string SubstituteReferenceActual(
        string reference,
        EcnEncodingLink link,
        EcnEncodingObjectDefinition target)
    {
        EcnSymbol? symbol = target.Symbol;
        EcnScope? scope = symbol == null ? null : symbol.Scope;
        if (scope == null)
        {
            return reference;
        }

        int index = 0;
        while (index < scope.Symbols.Length)
        {
            string formal = scope.Symbols[index].Name;
            if ((reference == formal
                    || (reference.Length > formal.Length
                        && reference.Substring(0, formal.Length) == formal
                        && reference[formal.Length] == '.'))
                && index < link.ActualParameters.Length)
            {
                string actual = link.ActualParameters[index].ReferenceName;
                return reference == formal
                    ? actual
                    : actual + reference.Substring(formal.Length);
            }

            index = index + 1;
        }

        return reference;
    }

    internal EcnLinkDirection GetLinkDirection(EcnDeterminationMode mode)
    {
        return mode == EcnDeterminationMode.FieldToBeSet
                || mode == EcnDeterminationMode.FlagToBeSet
            ? EcnLinkDirection.ContentToDeterminant
            : EcnLinkDirection.DeterminantToContent;
    }

    internal EcnFieldPath ResolveFieldPath(
        string text,
        EcnEncodingObjectDefinition owner,
        AsnSourceSpan? span)
    {
        EcnFieldPath path = new EcnFieldPath
        {
            Text = text,
            Span = span,
            Valid = true
        };
        if (text.Length == 0)
        {
            path.Valid = false;
            this.AddInvalidPathDiagnostic(text, span);
            return path;
        }

        if (text[0] == '$')
        {
            path.Root = EcnFieldPathRoot.Synthetic;
            path.NormalizedText = text;
            path.Segments = [text];
            path.Deferred = true;
            return path;
        }

        path.Segments = this.SplitPath(text);
        if (path.Segments.Length == 0)
        {
            path.Valid = false;
            this.AddInvalidPathDiagnostic(text, span);
            return path;
        }

        path.NormalizedText = this.JoinSegments(path.Segments);
        path.ParentDepth = this.CountParentDepth(text);
        EcnSymbol? parameter = this.FindReferenceParameter(
            owner.Symbol,
            path.Segments[0]);
        if (parameter != null)
        {
            path.Root = EcnFieldPathRoot.Parameter;
            path.ParameterSymbol = parameter;
            path.Deferred = path.Segments.Length > 1;
            path.ResolvedSegments = [new EcnFieldPathSegment
            {
                Name = path.Segments[0],
                Resolved = true
            }];
            return path;
        }

        int start = this.GetPathFieldStart(path, owner);
        path.NormalizedText = this.JoinSegmentRange(
            path.Segments,
            start,
            path.Segments.Length - start);
        if (path.ParentDepth > 0)
        {
            path.Root = EcnFieldPathRoot.Relative;
            path.Deferred = true;
            return path;
        }

        EcnSymbol? governor = owner.Symbol == null
            ? null
            : owner.Symbol.GovernorSymbol;
        AsnType? importedType = governor == null
                || !governor.ImportedAsnType
                || governor.AsnTypeAssignment == null
            ? null
            : governor.AsnTypeAssignment.Type;
        bool resolved = importedType == null
            ? this.ResolveEcnPath(path, owner, start)
            : this.ResolveAsnPath(path, importedType, start);
        if (!resolved)
        {
            bool knownStructure = owner.Constructor != null
                && (owner.Constructor.Fields.Length > 0
                    || owner.Constructor.Structure != null);
            if (!knownStructure && path.Segments.Length > 1)
            {
                path.Deferred = true;
                return path;
            }

            path.Valid = false;
            this.AddInvalidPathDiagnostic(text, span);
        }

        return path;
    }

    internal int CountParentDepth(string text)
    {
        int depth = 0;
        int index = 0;
        while (index + 2 < text.Length
            && text[index] == '.'
            && text[index + 1] == '.'
            && text[index + 2] == '/')
        {
            depth = depth + 1;
            index = index + 3;
        }

        return depth;
    }

    internal string[] SplitPath(string text)
    {
        System.Collections.Generic.List<string> segments =
            new System.Collections.Generic.List<string>();
        System.Text.StringBuilder current = new System.Text.StringBuilder();
        int index = 0;
        while (index <= text.Length)
        {
            bool boundary = index == text.Length;
            if (!boundary)
            {
                char value = text[index];
                boundary = value == '.' || value == '/';
            }

            if (boundary)
            {
                if (current.Length > 0)
                {
                    segments.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(text[index]);
            }

            index = index + 1;
        }

        return segments.ToArray();
    }

    internal string JoinSegments(string[] segments)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(segments[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string JoinSegmentRange(
        string[] segments,
        int start,
        int count)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = start;
        int end = start + count;
        while (index < end)
        {
            if (builder.Length > 0)
            {
                builder.Append('.');
            }

            builder.Append(segments[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal EcnSymbol? FindReferenceParameter(
        EcnSymbol? owner,
        string name)
    {
        EcnScope? scope = owner == null ? null : owner.Scope;
        EcnSymbol? symbol = scope == null ? null : scope.FindLocalSymbol(name);
        return symbol != null
                && symbol.ParameterKind == EcnParameterKind.Reference
            ? symbol
            : null;
    }

    internal int GetPathFieldStart(
        EcnFieldPath path,
        EcnEncodingObjectDefinition owner)
    {
        int start = 0;
        string first = path.Segments[0];
        string moduleName = owner.Symbol == null
            ? ""
            : owner.Symbol.SourceModuleName;
        string governorName = owner.GovernorName.Length == 0
            || owner.GovernorName[0] != '#'
            ? owner.GovernorName
            : owner.GovernorName.Substring(1);
        bool explicitAbsolute = path.Text[0] == '/'
            || first == "Root"
            || first == moduleName
            || first == owner.GovernorName
            || first == governorName;
        path.Root = explicitAbsolute
            ? EcnFieldPathRoot.Absolute
            : EcnFieldPathRoot.Relative;
        if (explicitAbsolute && first != "Root" && first == moduleName)
        {
            start = start + 1;
            if (start < path.Segments.Length
                && (path.Segments[start] == owner.GovernorName
                    || path.Segments[start] == governorName))
            {
                start = start + 1;
            }
        }
        else if (explicitAbsolute && path.Text[0] != '/')
        {
            start = start + 1;
        }

        return start;
    }

    internal bool ResolveEcnPath(
        EcnFieldPath path,
        EcnEncodingObjectDefinition owner,
        int start)
    {
        EcnEncodingField[] fields = owner.Constructor == null
            ? []
            : owner.Constructor.Fields;
        System.Collections.Generic.List<EcnFieldPathSegment> resolved =
            new System.Collections.Generic.List<EcnFieldPathSegment>();
        int index = start;
        while (index < path.Segments.Length)
        {
            int fieldIndex = this.FindEncodingField(
                fields,
                path.Segments[index]);
            if (fieldIndex < 0)
            {
                return false;
            }

            EcnEncodingField field = fields[fieldIndex];
            resolved.Add(new EcnFieldPathSegment
            {
                Name = path.Segments[index],
                EncodingField = field,
                FieldOrder = fieldIndex,
                Resolved = true
            });
            if (index == start)
            {
                path.TopLevelFieldOrder = fieldIndex;
            }

            fields = this.GetNestedFields(field.Structure);
            index = index + 1;
        }

        path.ResolvedSegments = resolved.ToArray();
        return resolved.Count > 0;
    }

    internal int FindEncodingField(
        EcnEncodingField[] fields,
        string name)
    {
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal EcnEncodingField[] GetNestedFields(EcnEncodingStructure? structure)
    {
        if (structure == null)
        {
            return [];
        }

        if (structure.Fields.Length > 0)
        {
            return structure.Fields;
        }

        if (structure.Kind == EcnEncodingStructureKind.Tagged)
        {
            return this.GetNestedFields(structure.InnerStructure);
        }

        EcnSymbol? symbol = structure.ClassSymbol;
        if (symbol != null && symbol.ClassDefinition != null)
        {
            EcnEncodingStructure? target = symbol.ClassDefinition.Structure;
            return target == null ? [] : target.Fields;
        }

        return [];
    }

    internal bool ResolveAsnPath(
        EcnFieldPath path,
        AsnType type,
        int start)
    {
        System.Collections.Generic.List<EcnFieldPathSegment> resolved =
            new System.Collections.Generic.List<EcnFieldPathSegment>();
        AsnType? current = type;
        int index = start;
        while (index < path.Segments.Length && current != null)
        {
            int componentIndex = this.FindAsnComponent(
                current.Components,
                path.Segments[index]);
            if (componentIndex < 0)
            {
                return false;
            }

            AsnComponent component = current.Components[componentIndex];
            resolved.Add(new EcnFieldPathSegment
            {
                Name = path.Segments[index],
                AsnComponent = component,
                FieldOrder = componentIndex,
                Resolved = true
            });
            if (index == start)
            {
                path.TopLevelFieldOrder = componentIndex;
            }

            current = component.Type;
            index = index + 1;
        }

        path.ResolvedSegments = resolved.ToArray();
        return index == path.Segments.Length && resolved.Count > 0;
    }

    internal int FindAsnComponent(AsnComponent[] components, string name)
    {
        int index = 0;
        while (index < components.Length)
        {
            if (components[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void AddInvalidPathDiagnostic(
        string text,
        AsnSourceSpan? span)
    {
        this.AddDiagnostic(
            EcnDiagnosticCodes.InvalidFieldPath,
            "The ECN field path '" + text
                + "' cannot be resolved in the governed encoding structure.",
            span);
    }

    internal EcnValueMapping[] CompileMappings(EcnClauseSyntax[] clauses)
    {
        System.Collections.Generic.List<EcnValueMapping> mappings =
            new System.Collections.Generic.List<EcnValueMapping>();
        int index = 0;
        while (index < clauses.Length)
        {
            if (clauses[index].Kind == EcnClauseKind.Mapping
                && !this.IsTransformMappingClause(clauses[index]))
            {
                mappings.Add(this.CompileMappingClause(clauses[index]));
            }

            index = index + 1;
        }

        return mappings.ToArray();
    }

    internal bool IsTransformMappingClause(EcnClauseSyntax clause)
    {
        return clause.Tokens.Length > 1
            && (clause.Tokens[1].Text == "TRANSFORMS"
                || clause.Tokens[1].Text == "ENCODER-TRANSFORMS"
                || clause.Tokens[1].Text == "DECODER-TRANSFORMS");
    }

    internal EcnValueMapping CompileMappingClause(EcnClauseSyntax clause)
    {
        int brace = this.FindTokenKind(
            clause.Tokens,
            AsnTokenKind.LeftBrace);
        int qualifierCount = brace < 0
            ? clause.Tokens.Length - 1
            : brace - 1;
        EcnValueMapping mapping = new EcnValueMapping
        {
            Kind = EcnMappingKind.Value,
            Qualifier = qualifierCount <= 0
                ? ""
                : this.JoinWords(clause.Tokens, 1, qualifierCount),
            Automatic = clause.MappingEntries.Length == 0 && brace < 0,
            Reversible = true,
            Valid = true,
            Tokens = clause.Tokens,
            Span = clause.Span
        };
        EcnValueMappingEntry[] entries =
            new EcnValueMappingEntry[clause.MappingEntries.Length];
        int index = 0;
        while (index < clause.MappingEntries.Length)
        {
            EcnMappingEntrySyntax syntax = clause.MappingEntries[index];
            EcnValueMappingEntry entry = new EcnValueMappingEntry
            {
                Source = this.JoinCompact(
                    syntax.SourceTokens,
                    0,
                    syntax.SourceTokens.Length),
                Target = this.JoinCompact(
                    syntax.TargetTokens,
                    0,
                    syntax.TargetTokens.Length),
                Order = index,
                Remainder = syntax.SourceTokens.Length == 1
                    && syntax.SourceTokens[0].Text == "REMAINDER",
                Valid = true,
                SourceTokens = syntax.SourceTokens,
                TargetTokens = syntax.TargetTokens,
                Span = syntax.Span
            };
            this.ValidateMappingEntry(entry, entries, index);
            if (!entry.Valid)
            {
                mapping.Valid = false;
                mapping.Reversible = false;
            }

            entries[index] = entry;
            index = index + 1;
        }

        mapping.Entries = entries;
        if (brace >= 0 && entries.Length == 0)
        {
            mapping.Valid = false;
            mapping.Reversible = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidMapping,
                "An explicit ECN mapping requires at least one mapping entry.",
                mapping.Span);
        }

        return mapping;
    }

    internal void ValidateMappingEntry(
        EcnValueMappingEntry entry,
        EcnValueMappingEntry[] earlierEntries,
        int earlierCount)
    {
        if (entry.Source.Length == 0 || entry.Target.Length == 0)
        {
            entry.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidMapping,
                "Each ECN mapping entry requires source and target values separated by TO.",
                entry.Span);
            return;
        }

        EcnMappingInterval entryInterval = this.ParseMappingInterval(
            entry.Source);
        if (entryInterval.Numeric && !entryInterval.Valid)
        {
            entry.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidMapping,
                "ECN mapping range '" + entry.Source
                    + "' has its lower bound above its upper bound.",
                entry.Span);
        }

        if (entry.Remainder && entry.Order + 1 != earlierEntries.Length)
        {
            entry.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidMapping,
                "REMAINDER must be the final entry in an ECN mapping.",
                entry.Span);
        }

        int index = 0;
        while (index < earlierCount)
        {
            EcnValueMappingEntry earlier = earlierEntries[index];
            if (earlier.Source == entry.Source)
            {
                entry.Valid = false;
                this.AddDiagnostic(
                    EcnDiagnosticCodes.AmbiguousMapping,
                    "ECN mapping source '" + entry.Source
                        + "' is mapped more than once.",
                    entry.Span);
            }
            else
            {
                EcnMappingInterval earlierInterval =
                    this.ParseMappingInterval(earlier.Source);
                if (entryInterval.Valid && earlierInterval.Valid
                    && entryInterval.Lower <= earlierInterval.Upper
                    && earlierInterval.Lower <= entryInterval.Upper)
                {
                    entry.Valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.AmbiguousMapping,
                        "ECN mapping source '" + entry.Source
                            + "' overlaps source '" + earlier.Source + "'.",
                        entry.Span);
                }
            }

            if (earlier.Target == entry.Target)
            {
                entry.Valid = false;
                this.AddDiagnostic(
                    EcnDiagnosticCodes.AmbiguousMapping,
                    "ECN mapping target '" + entry.Target
                        + "' is produced by more than one source.",
                    entry.Span);
            }

            index = index + 1;
        }
    }

    internal EcnMappingInterval ParseMappingInterval(string text)
    {
        EcnMappingInterval interval = new EcnMappingInterval();
        if (text == "REMAINDER")
        {
            return interval;
        }

        int range = text.IndexOf(
            "..",
            System.StringComparison.Ordinal);
        string lowerText = range < 0 ? text : text.Substring(0, range);
        string upperText = range < 0
            ? text
            : text.Substring(range + 2);
        EcnIntegerParseResult lower = this.ParseMappingInteger(lowerText);
        EcnIntegerParseResult upper = this.ParseMappingInteger(upperText);
        if (!lower.Success || !upper.Success)
        {
            return interval;
        }

        interval.Lower = lower.Value;
        interval.Upper = upper.Value;
        interval.Numeric = true;
        interval.Valid = lower.Value <= upper.Value;
        return interval;
    }

    internal EcnIntegerParseResult ParseMappingInteger(string text)
    {
        EcnIntegerParseResult result = new EcnIntegerParseResult();
        if (text.Length == 0)
        {
            return result;
        }

        bool negative = text[0] == '-';
        int index = negative || text[0] == '+' ? 1 : 0;
        if (index == text.Length)
        {
            return result;
        }

        long value = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return result;
            }

            int digit = text[index] - '0';
            if (value > (long.MaxValue - digit) / 10)
            {
                return result;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        result.Value = negative ? -value : value;
        result.Success = true;
        return result;
    }

    internal int FindTokenKind(AsnToken[] tokens, AsnTokenKind kind)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == kind)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void AssignMappings(EcnEncodingObjectDefinition owner)
    {
        int mappingIndex = 0;
        while (mappingIndex < owner.Mappings.Length)
        {
            EcnValueMapping mapping = owner.Mappings[mappingIndex];
            EcnDeterminantRelationship? relation =
                this.FindPrecedingRelationship(
                    owner.Determinants,
                    mapping.Span);
            if (relation != null)
            {
                relation.Mapping = mapping;
                mapping.Kind = this.GetMappingKind(relation.ControlKind);
                if (relation.EncodingLink != null)
                {
                    relation.EncodingLink.Mapping = mapping;
                }
            }
            else
            {
                EcnEncodingLink? link = this.FindPrecedingLink(
                    owner.Links,
                    mapping.Span);
                if (link != null)
                {
                    link.Mapping = mapping;
                }
            }

            mappingIndex = mappingIndex + 1;
        }
    }

    internal EcnDeterminantRelationship? FindPrecedingRelationship(
        EcnDeterminantRelationship[] relations,
        AsnSourceSpan? mappingSpan)
    {
        EcnDeterminantRelationship? found = null;
        int foundOffset = -1;
        int mappingOffset = this.GetSpanOffset(mappingSpan);
        int index = 0;
        while (index < relations.Length)
        {
            int offset = this.GetSpanOffset(relations[index].Span);
            if (offset <= mappingOffset && offset >= foundOffset)
            {
                found = relations[index];
                foundOffset = offset;
            }

            index = index + 1;
        }

        return found;
    }

    internal EcnEncodingLink? FindPrecedingLink(
        EcnEncodingLink[] links,
        AsnSourceSpan? mappingSpan)
    {
        EcnEncodingLink? found = null;
        int foundOffset = -1;
        int mappingOffset = this.GetSpanOffset(mappingSpan);
        int index = 0;
        while (index < links.Length)
        {
            int offset = this.GetSpanOffset(links[index].Span);
            if (offset <= mappingOffset && offset >= foundOffset)
            {
                found = links[index];
                foundOffset = offset;
            }

            index = index + 1;
        }

        return found;
    }

    internal int GetSpanOffset(AsnSourceSpan? span)
    {
        return span == null ? -1 : span.Start.Offset;
    }

    internal EcnMappingKind GetMappingKind(EcnControlDependencyKind kind)
    {
        if (kind == EcnControlDependencyKind.Presence)
        {
            return EcnMappingKind.Presence;
        }

        if (kind == EcnControlDependencyKind.Alternative)
        {
            return EcnMappingKind.Choice;
        }

        return kind == EcnControlDependencyKind.EncodingSpace
                || kind == EcnControlDependencyKind.RepetitionSpace
            ? EcnMappingKind.Length
            : EcnMappingKind.Value;
    }

    internal bool ValidateLinkTargets(EcnEncodingLink[] links)
    {
        bool valid = true;
        int index = 0;
        while (index < links.Length)
        {
            int earlier = 0;
            while (earlier < index)
            {
                EcnFieldPath? currentPath = links[index].TargetPath;
                EcnFieldPath? earlierPath = links[earlier].TargetPath;
                if (currentPath != null
                    && earlierPath != null
                    && currentPath.Root
                        != EcnFieldPathRoot.Synthetic
                    && currentPath.NormalizedText
                        == earlierPath.NormalizedText)
                {
                    links[index].Valid = false;
                    valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.InvalidLink,
                        "Encoding field '"
                            + currentPath.NormalizedText
                            + "' has more than one encoding link.",
                        links[index].Span);
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }

        return valid;
    }

    internal bool ValidateRelationships(
        EcnDeterminantRelationship[] relationships)
    {
        bool valid = true;
        int index = 0;
        while (index < relationships.Length)
        {
            EcnDeterminantRelationship relation = relationships[index];
            if (relation.ControlledPath == null
                || relation.DeterminantPath == null
                || !relation.ControlledPath.Valid
                || !relation.DeterminantPath.Valid)
            {
                relation.Valid = false;
                valid = false;
            }
            else if (relation.ControlledPath.TopLevelFieldOrder >= 0
                && relation.DeterminantPath.TopLevelFieldOrder >= 0
                && relation.DeterminantPath.TopLevelFieldOrder
                    >= relation.ControlledPath.TopLevelFieldOrder)
            {
                relation.Valid = false;
                valid = false;
                this.AddDiagnostic(
                    EcnDiagnosticCodes.InvalidLinkDirection,
                    "Determinant field '"
                        + relation.DeterminantPath.NormalizedText
                        + "' is not available before controlled field '"
                        + relation.ControlledPath.NormalizedText + "'.",
                    relation.Span);
            }

            int earlier = 0;
            while (earlier < index)
            {
                EcnDeterminantRelationship prior = relationships[earlier];
                if (relation.Direction
                        == EcnLinkDirection.ContentToDeterminant
                    && prior.Direction
                        == EcnLinkDirection.ContentToDeterminant
                    && relation.DeterminantPath != null
                    && prior.DeterminantPath != null
                    && relation.DeterminantPath.NormalizedText
                        == prior.DeterminantPath.NormalizedText)
                {
                    relation.Valid = false;
                    valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.ConflictingDeterminant,
                        "Determinant field '"
                            + relation.DeterminantPath.NormalizedText
                            + "' is set by more than one encoding link.",
                        relation.Span);
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }

        if (!this.ValidateRelationshipCycles(relationships))
        {
            valid = false;
        }

        return valid;
    }

    internal bool ValidateRelationshipCycles(
        EcnDeterminantRelationship[] relationships)
    {
        System.Collections.Generic.List<string> nodes =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < relationships.Length)
        {
            EcnFieldPath? controlled = relationships[index].ControlledPath;
            EcnFieldPath? determinant = relationships[index].DeterminantPath;
            if (controlled != null && determinant != null
                && controlled.Root != EcnFieldPathRoot.Synthetic
                && determinant.Root != EcnFieldPathRoot.Synthetic
                && controlled.Valid && determinant.Valid)
            {
                this.AddUniqueNode(nodes, controlled.NormalizedText);
                this.AddUniqueNode(nodes, determinant.NormalizedText);
            }

            index = index + 1;
        }

        bool[][] reach = new bool[nodes.Count][];
        index = 0;
        while (index < nodes.Count)
        {
            reach[index] = new bool[nodes.Count];
            index = index + 1;
        }

        index = 0;
        while (index < relationships.Length)
        {
            EcnFieldPath? controlled = relationships[index].ControlledPath;
            EcnFieldPath? determinant = relationships[index].DeterminantPath;
            if (controlled != null && determinant != null
                && controlled.Root != EcnFieldPathRoot.Synthetic
                && determinant.Root != EcnFieldPathRoot.Synthetic
                && controlled.Valid && determinant.Valid)
            {
                int from = this.FindNode(nodes, determinant.NormalizedText);
                int to = this.FindNode(nodes, controlled.NormalizedText);
                reach[from][to] = true;
            }

            index = index + 1;
        }

        int middle = 0;
        while (middle < nodes.Count)
        {
            int from = 0;
            while (from < nodes.Count)
            {
                int to = 0;
                while (to < nodes.Count)
                {
                    if (reach[from][middle] && reach[middle][to])
                    {
                        reach[from][to] = true;
                    }

                    to = to + 1;
                }

                from = from + 1;
            }

            middle = middle + 1;
        }

        index = 0;
        while (index < nodes.Count)
        {
            if (reach[index][index])
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.LinkCycle,
                    "The ECN determinant links contain a cycle through field '"
                        + nodes[index] + "'.",
                    null);
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void AddUniqueNode(
        System.Collections.Generic.List<string> nodes,
        string value)
    {
        if (this.FindNode(nodes, value) < 0)
        {
            nodes.Add(value);
        }
    }

    internal int FindNode(
        System.Collections.Generic.List<string> nodes,
        string value)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
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

    internal EcnEncodingLink[] CopyLinks(EcnEncodingLink[] source)
    {
        EcnEncodingLink[] copy = new EcnEncodingLink[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal EcnValueMapping[] CopyMappings(EcnValueMapping[] source)
    {
        EcnValueMapping[] copy = new EcnValueMapping[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
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

    internal string JoinWords(AsnToken[] tokens, int start, int count)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = start;
        int end = start + count;
        while (index < end)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

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
