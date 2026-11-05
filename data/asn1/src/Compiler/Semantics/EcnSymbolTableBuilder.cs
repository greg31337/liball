namespace asn1;

internal class EcnSymbolTableBuilder
{
    internal DiagnosticBag Diagnostics;
    internal EcnSymbolTable[] Tables = [];
    internal AsnModule[] AsnModules = [];

    internal EcnSymbolTableBuilder(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal EcnSymbolTable BuildTable(
        EcnModuleSyntax module,
        AsnModule[] asnModules)
    {
        if (module == null)
        {
            throw new System.ArgumentNullException("module");
        }

        return this.BuildTables([module], asnModules)[0];
    }

    internal EcnSymbolTable[] BuildTables(
        EcnModuleSyntax[] modules,
        AsnModule[] asnModules)
    {
        if (modules == null)
        {
            throw new System.ArgumentNullException("modules");
        }

        if (asnModules == null)
        {
            throw new System.ArgumentNullException("asnModules");
        }

        this.AsnModules = asnModules;
        EcnSymbolTable[] tables = new EcnSymbolTable[modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            tables[index] = this.CreateLocalTable(modules[index]);
            index = index + 1;
        }

        this.Tables = tables;
        index = 0;
        while (index < tables.Length)
        {
            this.ResolveTableImports(tables[index]);
            index = index + 1;
        }

        index = 0;
        while (index < tables.Length)
        {
            this.BindTableGovernors(tables[index]);
            index = index + 1;
        }

        new EcnClassCategoryResolver(this.Diagnostics).ResolveTables(tables);
        new EcnEncodingClassCompiler(this.Diagnostics).CompileTables(tables);
        new EcnEncodingObjectCompiler(this.Diagnostics).CompileTables(tables);
        new EcnPropertyCompiler(this.Diagnostics).CompileTables(tables);
        new EcnConstructorCompiler(this.Diagnostics).CompileTables(tables);
        new EcnLinkCompiler(this.Diagnostics).CompileTables(tables);
        new EcnConversionCompiler(this.Diagnostics).CompileTables(tables);
        new EcnPlanLowerer().LowerTables(tables);
        new EcnPlanValidator(this.Diagnostics).ValidateTables(tables);

        index = 0;
        while (index < tables.Length)
        {
            new EcnDiagnosticContextEnricher().EnrichDiagnostics(
                this.Diagnostics.Items.ToArray(),
                tables[index],
                tables[index].Module,
                "semantic");
            index = index + 1;
        }

        return tables;
    }

    internal EcnSymbolTable CreateLocalTable(EcnModuleSyntax module)
    {
        System.Collections.Generic.List<EcnSymbol> symbols =
            new System.Collections.Generic.List<EcnSymbol>();
        System.Collections.Generic.List<EcnSymbol> parameters =
            new System.Collections.Generic.List<EcnSymbol>();
        EcnSymbolTable table = new EcnSymbolTable
        {
            ModuleName = module.Name,
            Module = module
        };
        int index = 0;
        while (index < module.Assignments.Length)
        {
            EcnAssignmentSyntax assignment = module.Assignments[index];
            EcnSymbol symbol = new EcnSymbol
            {
                Name = assignment.Reference,
                QualifiedName = module.Name + "." + assignment.Reference,
                SourceModuleName = module.Name,
                Kind = this.ClassifyAssignment(assignment),
                DeclarationOrder = symbols.Count,
                Exported = this.IsExported(module, assignment.Reference),
                Assignment = assignment,
                Span = assignment.Span,
                DeclaringScope = table.RootScope
            };
            EcnSymbol? duplicate = this.FindOverlappingLocal(symbols, symbol);
            if (duplicate != null)
            {
                this.AddDuplicateSymbolDiagnostic(symbol, duplicate);
            }

            assignment.Symbol = symbol;
            symbols.Add(symbol);
            this.CreateParameterScope(symbol, assignment, table, parameters);
            index = index + 1;
        }

        table.LocalSymbols = symbols.ToArray();
        table.Parameters = parameters.ToArray();
        table.RootScope.Symbols = table.LocalSymbols;
        this.RefreshIndexes(table);
        module.SymbolTable = table;
        return table;
    }

    internal EcnSymbolKind ClassifyAssignment(EcnAssignmentSyntax assignment)
    {
        if (assignment.Kind == EcnAssignmentKind.EncodingObject)
        {
            return EcnSymbolKind.EncodingObject;
        }

        if (assignment.Kind == EcnAssignmentKind.EncodingObjectSet)
        {
            return EcnSymbolKind.EncodingObjectSet;
        }

        if (assignment.BodyTokens.Length == 0)
        {
            return EcnSymbolKind.EncodingClass;
        }

        return this.IsSimpleClassReference(assignment.BodyTokens)
            ? EcnSymbolKind.EncodingClass
            : EcnSymbolKind.EncodingStructure;
    }

    internal bool IsSimpleClassReference(AsnToken[] tokens)
    {
        if (tokens.Length == 1
            && tokens[0].Kind == AsnTokenKind.EncodingClassReference)
        {
            return true;
        }

        if (tokens.Length == 3
            && tokens[1].Kind == AsnTokenKind.Dot
            && tokens[2].Kind == AsnTokenKind.EncodingClassReference)
        {
            return true;
        }

        if (tokens.Length >= 5
            && tokens[0].Kind == AsnTokenKind.EncodingClassReference
            && tokens[1].Kind == AsnTokenKind.LeftBrace
            && tokens[2].Kind == AsnTokenKind.LessThan
            && tokens[tokens.Length - 2].Kind == AsnTokenKind.GreaterThan
            && tokens[tokens.Length - 1].Kind == AsnTokenKind.RightBrace)
        {
            return true;
        }

        return false;
    }

    internal void CreateParameterScope(
        EcnSymbol owner,
        EcnAssignmentSyntax assignment,
        EcnSymbolTable table,
        System.Collections.Generic.List<EcnSymbol> allParameters)
    {
        EcnScope scope = new EcnScope
        {
            Parent = table.RootScope,
            Owner = owner
        };
        EcnSymbol[] parameters = new EcnSymbol[assignment.Parameters.Length];
        int index = 0;
        while (index < assignment.Parameters.Length)
        {
            EcnParameterSyntax syntax = assignment.Parameters[index];
            EcnSymbol parameter = new EcnSymbol
            {
                Name = syntax.Reference,
                QualifiedName = owner.QualifiedName + "." + syntax.Reference,
                SourceModuleName = table.ModuleName,
                Kind = EcnSymbolKind.Parameter,
                GovernorName = syntax.Governor,
                DeclarationOrder = index,
                Parameter = syntax,
                Span = syntax.Span,
                DeclaringScope = scope
            };
            int earlier = 0;
            while (earlier < index)
            {
                if (parameters[earlier].Name == parameter.Name)
                {
                    this.AddDuplicateParameterDiagnostic(
                        parameter,
                        parameters[earlier]);
                    break;
                }

                earlier = earlier + 1;
            }

            syntax.Symbol = parameter;
            parameters[index] = parameter;
            allParameters.Add(parameter);
            index = index + 1;
        }

        scope.Symbols = parameters;
        owner.Scope = scope;
    }

    internal void ResolveTableImports(EcnSymbolTable table)
    {
        EcnModuleSyntax? module = table.Module;
        if (module == null)
        {
            return;
        }

        System.Collections.Generic.List<EcnSymbol> imports =
            new System.Collections.Generic.List<EcnSymbol>();
        int index = 0;
        while (index < module.ImportedModules.Length)
        {
            EcnImportSyntax import = module.ImportedModules[index];
            int ecnCount = this.CountEcnModules(import.ModuleName);
            int asnCount = this.CountAsnModules(import.ModuleName);
            if (ecnCount + asnCount == 0)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.UnknownImportModule,
                    "No ECN or ASN.1 module named '" + import.ModuleName
                        + "' is available for this import.",
                    import.Span,
                    null);
            }
            else if (ecnCount + asnCount > 1)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.AmbiguousImportModule,
                    "The imported module name '" + import.ModuleName
                        + "' is ambiguous.",
                    import.Span,
                    null);
            }
            else if (ecnCount == 1)
            {
                EcnSymbolTable? target = this.FindEcnModule(import.ModuleName);
                if (target != null)
                {
                    this.ImportEcnSymbols(import, target, table, imports);
                }
            }
            else
            {
                AsnModule? target = this.FindAsnModule(import.ModuleName);
                if (target != null)
                {
                    this.ImportAsnTypes(import, target, table, imports);
                }
            }

            index = index + 1;
        }

        table.ImportedSymbols = imports.ToArray();
        EcnSymbol[] combined = new EcnSymbol[
            table.LocalSymbols.Length + table.ImportedSymbols.Length];
        index = 0;
        while (index < table.LocalSymbols.Length)
        {
            combined[index] = table.LocalSymbols[index];
            index = index + 1;
        }

        int importIndex = 0;
        while (importIndex < table.ImportedSymbols.Length)
        {
            combined[index] = table.ImportedSymbols[importIndex];
            index = index + 1;
            importIndex = importIndex + 1;
        }

        table.RootScope.Symbols = combined;
        this.RefreshIndexes(table);
    }

    internal void ImportEcnSymbols(
        EcnImportSyntax import,
        EcnSymbolTable target,
        EcnSymbolTable destination,
        System.Collections.Generic.List<EcnSymbol> imports)
    {
        int index = 0;
        while (index < import.Symbols.Length)
        {
            string name = import.Symbols[index];
            EcnSymbol? found = null;
            int matchCount = 0;
            int symbolIndex = 0;
            while (symbolIndex < target.LocalSymbols.Length)
            {
                EcnSymbol candidate = target.LocalSymbols[symbolIndex];
                if (candidate.Name == name && candidate.Exported)
                {
                    found = candidate;
                    matchCount = matchCount + 1;
                }

                symbolIndex = symbolIndex + 1;
            }

            if (matchCount != 1 || found == null)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.UnknownImportedSymbol,
                    "The symbol '" + name + "' is not uniquely exported by ECN module '"
                        + target.ModuleName + "'.",
                    import.Span,
                    null);
            }
            else
            {
                imports.Add(new EcnSymbol
                {
                    Name = found.Name,
                    QualifiedName = found.QualifiedName,
                    SourceModuleName = target.ModuleName,
                    Kind = found.Kind,
                    Imported = true,
                    Exported = found.Exported,
                    ImportedTarget = found,
                    Assignment = found.Assignment,
                    Span = import.Span,
                    DeclaringScope = destination.RootScope
                });
            }

            index = index + 1;
        }
    }

    internal void ImportAsnTypes(
        EcnImportSyntax import,
        AsnModule target,
        EcnSymbolTable destination,
        System.Collections.Generic.List<EcnSymbol> imports)
    {
        int index = 0;
        while (index < import.Symbols.Length)
        {
            string name = import.Symbols[index];
            string typeName = name.Length > 1 && name[0] == '#'
                ? name.Substring(1)
                : "";
            AsnAssignment? found = null;
            int matchCount = 0;
            int assignmentIndex = 0;
            while (assignmentIndex < target.Assignments.Length)
            {
                AsnAssignment candidate = target.Assignments[assignmentIndex];
                if (candidate.Kind == AsnAssignmentKind.Type
                    && candidate.Name == typeName)
                {
                    found = candidate;
                    matchCount = matchCount + 1;
                }

                assignmentIndex = assignmentIndex + 1;
            }

            if (matchCount != 1 || found == null)
            {
                this.AddDiagnostic(
                    EcnDiagnosticCodes.UnknownImportedSymbol,
                    "The symbol '" + name + "' does not identify a unique ASN.1 type in module '"
                        + target.Name + "'.",
                    import.Span,
                    null);
            }
            else
            {
                imports.Add(new EcnSymbol
                {
                    Name = name,
                    QualifiedName = target.Name + "." + name,
                    SourceModuleName = target.Name,
                    Kind = EcnSymbolKind.EncodingStructure,
                    Imported = true,
                    ImportedAsnType = true,
                    Exported = true,
                    AsnTypeAssignment = found,
                    Span = import.Span,
                    DeclaringScope = destination.RootScope
                });
            }

            index = index + 1;
        }
    }

    internal void BindTableGovernors(EcnSymbolTable table)
    {
        int index = 0;
        while (index < table.LocalSymbols.Length)
        {
            EcnSymbol symbol = table.LocalSymbols[index];
            EcnScope? scope = symbol.Scope;
            if (scope != null)
            {
                int parameterIndex = 0;
                while (parameterIndex < scope.Symbols.Length)
                {
                    this.BindParameterGovernor(scope.Symbols[parameterIndex], table);
                    parameterIndex = parameterIndex + 1;
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < table.LocalSymbols.Length)
        {
            this.BindAssignmentGovernor(table.LocalSymbols[index], table);
            index = index + 1;
        }
    }

    internal void BindParameterGovernor(EcnSymbol parameter, EcnSymbolTable table)
    {
        string governor = parameter.GovernorName;
        if (governor.Length == 0)
        {
            if (parameter.Name.Length > 1 && parameter.Name[0] == '#')
            {
                parameter.ParameterKind = EcnParameterKind.EncodingClass;
                return;
            }

            parameter.BindingFailed = true;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidGovernorKind,
                "The ungoverned ECN parameter '" + parameter.Name
                    + "' must be an encoding class reference.",
                parameter.Span,
                null);
            return;
        }

        if (governor == "REFERENCE")
        {
            parameter.GovernorKind = EcnGovernorKind.Reference;
            parameter.ParameterKind = EcnParameterKind.Reference;
            parameter.GovernorBuiltIn = true;
            return;
        }

        if (governor == "#ENCODINGS")
        {
            parameter.GovernorKind = EcnGovernorKind.EncodingObjectSet;
            parameter.ParameterKind = EcnParameterKind.EncodingObjectSet;
            parameter.GovernorBuiltIn = true;
            return;
        }

        if (governor[0] == '#'
            || governor.IndexOf(".#", System.StringComparison.Ordinal) >= 0)
        {
            string className = this.ExtractClassReference(governor);
            parameter.GovernorKind = this.IsClassFieldGovernor(governor)
                ? EcnGovernorKind.EncodingClassField
                : EcnGovernorKind.EncodingClass;
            parameter.ParameterKind = parameter.GovernorKind
                == EcnGovernorKind.EncodingClass
                ? EcnParameterKind.EncodingObject
                : EcnParameterKind.AsnValue;
            this.ResolveClassGovernor(parameter, table, className);
            return;
        }

        parameter.GovernorKind = EcnGovernorKind.AsnType;
        parameter.ParameterKind = EcnParameterKind.AsnValue;
        if (this.IsBuiltInAsnType(governor))
        {
            parameter.GovernorBuiltIn = true;
            return;
        }

        EcnSymbol? imported = table.FindClassSymbol("#" + governor);
        if (imported != null && imported.ImportedAsnType)
        {
            parameter.GovernorSymbol = imported;
            return;
        }

        parameter.BindingFailed = true;
        this.AddDiagnostic(
            EcnDiagnosticCodes.UnknownGovernor,
            "The ASN.1 type governor '" + governor + "' could not be resolved.",
            parameter.Span,
            null);
    }

    internal void BindAssignmentGovernor(EcnSymbol symbol, EcnSymbolTable table)
    {
        EcnAssignmentSyntax? assignment = symbol.Assignment;
        if (assignment == null)
        {
            return;
        }

        if (symbol.Kind == EcnSymbolKind.EncodingObjectSet)
        {
            symbol.GovernorName = "#ENCODINGS";
            symbol.GovernorKind = EcnGovernorKind.EncodingObjectSet;
            symbol.GovernorBuiltIn = true;
            return;
        }

        if (symbol.Kind == EcnSymbolKind.EncodingObject)
        {
            symbol.GovernorName = this.ExtractClassReference(assignment.Governor);
            symbol.GovernorKind = EcnGovernorKind.EncodingClass;
            this.ResolveClassGovernor(symbol, table, symbol.GovernorName);
            return;
        }

        string structureGovernor = this.FindStructureGovernor(assignment.BodyTokens);
        if (structureGovernor.Length == 0)
        {
            return;
        }

        symbol.GovernorName = structureGovernor;
        symbol.GovernorKind = EcnGovernorKind.EncodingClass;
        this.ResolveClassGovernor(symbol, table, structureGovernor);
    }

    internal void ResolveClassGovernor(
        EcnSymbol governed,
        EcnSymbolTable table,
        string reference)
    {
        string simple = reference;
        string moduleName = "";
        int external = reference.IndexOf(".#", System.StringComparison.Ordinal);
        if (external >= 0)
        {
            moduleName = reference.Substring(0, external);
            simple = reference.Substring(external + 1);
        }

        if (moduleName.Length == 0 && this.IsBuiltInClass(simple))
        {
            governed.GovernorBuiltIn = true;
            return;
        }

        EcnScope? scope = governed.Scope == null
            ? governed.DeclaringScope
            : governed.Scope;
        if (moduleName.Length == 0 && scope != null && scope.Owner != null)
        {
            EcnSymbol? parameter = scope.FindLocalSymbol(simple);
            if (parameter != null
                && parameter.ParameterKind == EcnParameterKind.EncodingClass)
            {
                governed.GovernorSymbol = parameter;
                return;
            }
        }

        EcnSymbol? found = null;
        int matches = 0;
        int index = 0;
        while (index < table.Symbols.Length)
        {
            EcnSymbol candidate = table.Symbols[index];
            bool classKind = candidate.Kind == EcnSymbolKind.EncodingClass
                || candidate.Kind == EcnSymbolKind.EncodingStructure;
            bool moduleMatches = moduleName.Length == 0
                || candidate.SourceModuleName == moduleName;
            if (classKind && moduleMatches && candidate.Name == simple)
            {
                found = candidate;
                matches = matches + 1;
            }

            index = index + 1;
        }

        if (matches == 1 && found != null)
        {
            governed.GovernorSymbol = found;
            return;
        }

        governed.BindingFailed = true;
        if (matches > 1)
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.AmbiguousGovernor,
                "The encoding class governor '" + reference + "' is ambiguous.",
                governed.Span,
                null);
            return;
        }

        if (this.HasNonClassSymbol(table, simple, moduleName))
        {
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidGovernorKind,
                "The governor '" + reference + "' does not identify an encoding class or structure.",
                governed.Span,
                null);
            return;
        }

        this.AddDiagnostic(
            EcnDiagnosticCodes.UnknownGovernor,
            "The encoding class governor '" + reference + "' could not be resolved.",
            governed.Span,
            null);
    }

    internal bool HasNonClassSymbol(
        EcnSymbolTable table,
        string name,
        string moduleName)
    {
        int index = 0;
        while (index < table.Symbols.Length)
        {
            EcnSymbol candidate = table.Symbols[index];
            if (candidate.Name == name
                && (moduleName.Length == 0
                    || candidate.SourceModuleName == moduleName)
                && candidate.Kind != EcnSymbolKind.EncodingClass
                && candidate.Kind != EcnSymbolKind.EncodingStructure)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string FindStructureGovernor(AsnToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == AsnTokenKind.EncodingClassReference)
            {
                if (index >= 2
                    && tokens[index - 1].Kind == AsnTokenKind.Dot
                    && tokens[index - 2].Kind == AsnTokenKind.ModuleReference)
                {
                    return tokens[index - 2].Text + "." + tokens[index].Text;
                }

                return tokens[index].Text;
            }

            index = index + 1;
        }

        return "";
    }

    internal string ExtractClassReference(string governor)
    {
        int external = governor.IndexOf(".#", System.StringComparison.Ordinal);
        int start = external >= 0 ? external + 1 : 0;
        int end = governor.Length;
        int dot = governor.IndexOf('.', start);
        if (dot >= 0)
        {
            end = dot;
        }

        int brace = governor.IndexOf('{', start);
        if (brace >= 0 && brace < end)
        {
            end = brace;
        }

        string className = governor.Substring(start, end - start);
        return external >= 0
            ? governor.Substring(0, external + 1) + className
            : className;
    }

    internal bool IsClassFieldGovernor(string governor)
    {
        int external = governor.IndexOf(".#", System.StringComparison.Ordinal);
        int searchStart = external >= 0 ? external + 2 : 1;
        return governor.IndexOf('.', searchStart) >= 0;
    }

    internal bool IsExported(EcnModuleSyntax module, string name)
    {
        if (!module.ExportsSpecified || module.ExportsAll)
        {
            return true;
        }

        int index = 0;
        while (index < module.Exports.Length)
        {
            if (module.Exports[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal EcnSymbol? FindOverlappingLocal(
        System.Collections.Generic.List<EcnSymbol> symbols,
        EcnSymbol candidate)
    {
        int index = 0;
        while (index < symbols.Count)
        {
            EcnSymbol current = symbols[index];
            bool classNamespace = (current.Kind == EcnSymbolKind.EncodingClass
                    || current.Kind == EcnSymbolKind.EncodingStructure)
                && (candidate.Kind == EcnSymbolKind.EncodingClass
                    || candidate.Kind == EcnSymbolKind.EncodingStructure);
            if (current.Name == candidate.Name
                && (current.Kind == candidate.Kind || classNamespace))
            {
                return current;
            }

            index = index + 1;
        }

        return null;
    }

    internal int CountEcnModules(string name)
    {
        int count = 0;
        int index = 0;
        while (index < this.Tables.Length)
        {
            if (this.Tables[index].ModuleName == name)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountAsnModules(string name)
    {
        int count = 0;
        int index = 0;
        while (index < this.AsnModules.Length)
        {
            if (this.AsnModules[index].Name == name)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal EcnSymbolTable? FindEcnModule(string name)
    {
        int index = 0;
        while (index < this.Tables.Length)
        {
            if (this.Tables[index].ModuleName == name)
            {
                return this.Tables[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnModule? FindAsnModule(string name)
    {
        int index = 0;
        while (index < this.AsnModules.Length)
        {
            if (this.AsnModules[index].Name == name)
            {
                return this.AsnModules[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void RefreshIndexes(EcnSymbolTable table)
    {
        table.Symbols = table.RootScope.Symbols;
        table.EncodingClasses = this.SelectCategory(
            table.Symbols,
            EcnSymbolKind.EncodingClass);
        table.EncodingObjects = this.SelectCategory(
            table.Symbols,
            EcnSymbolKind.EncodingObject);
        table.EncodingObjectSets = this.SelectCategory(
            table.Symbols,
            EcnSymbolKind.EncodingObjectSet);
        table.EncodingStructures = this.SelectCategory(
            table.Symbols,
            EcnSymbolKind.EncodingStructure);
    }

    internal EcnSymbol[] SelectCategory(
        EcnSymbol[] symbols,
        EcnSymbolKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < symbols.Length)
        {
            if (symbols[index].Kind == kind)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        EcnSymbol[] selected = new EcnSymbol[count];
        int selectedIndex = 0;
        index = 0;
        while (index < symbols.Length)
        {
            if (symbols[index].Kind == kind)
            {
                selected[selectedIndex] = symbols[index];
                selectedIndex = selectedIndex + 1;
            }

            index = index + 1;
        }

        return selected;
    }

    internal bool IsBuiltInAsnType(string name)
    {
        switch (name)
        {
            case "BOOLEAN":
            case "INTEGER":
            case "ENUMERATED":
            case "REAL":
            case "NULL":
            case "OBJECT-IDENTIFIER":
            case "RELATIVE-OID":
            case "BIT-STRING":
            case "OCTET-STRING":
            case "CHARACTER-STRING":
            case "CHOICE":
            case "SEQUENCE":
            case "SET":
            case "SEQUENCE-OF":
            case "SET-OF":
            case "EMBEDDED-PDV":
            case "EXTERNAL":
            case "GeneralizedTime":
            case "UTCTime":
            case "ObjectDescriptor":
            case "BMPString":
            case "GeneralString":
            case "GraphicString":
            case "IA5String":
            case "NumericString":
            case "PrintableString":
            case "TeletexString":
            case "UniversalString":
            case "UTF8String":
            case "VideotexString":
            case "VisibleString":
                return true;
            default:
                return false;
        }
    }

    internal bool IsBuiltInClass(string name)
    {
        return EcnBuiltInClassCatalog.FindClass(name) != null;
    }

    internal void AddDuplicateSymbolDiagnostic(
        EcnSymbol duplicate,
        EcnSymbol original)
    {
        AsnDiagnosticRelatedLocation[] related = original.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The first declaration is here.",
                Span = original.Span
            }];
        this.AddDiagnostic(
            EcnDiagnosticCodes.DuplicateSymbol,
            "The ECN symbol '" + duplicate.Name + "' is declared more than once.",
            duplicate.Span,
            related);
    }

    internal void AddDuplicateParameterDiagnostic(
        EcnSymbol duplicate,
        EcnSymbol original)
    {
        AsnDiagnosticRelatedLocation[] related = original.Span == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The first parameter declaration is here.",
                Span = original.Span
            }];
        this.AddDiagnostic(
            EcnDiagnosticCodes.DuplicateParameter,
            "The ECN parameter '" + duplicate.Name + "' is declared more than once in the same scope.",
            duplicate.Span,
            related);
    }

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span,
        AsnDiagnosticRelatedLocation[]? related)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            code,
            message,
            span,
            related);
    }
}
