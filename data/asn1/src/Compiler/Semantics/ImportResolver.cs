namespace asn1;

internal class ImportResolver
{
    internal DiagnosticBag Diagnostics;

    internal ImportResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal ImportResolution[] ResolveImports(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        System.Collections.Generic.List<ImportResolution> resolutions =
            new System.Collections.Generic.List<ImportResolution>();
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            SymbolTable source = tables[tableIndex];
            ModuleSyntax? sourceSyntax = source.Module;
            System.Collections.Generic.List<ImportResolution> sourceResolutions =
                new System.Collections.Generic.List<ImportResolution>();
            if (sourceSyntax != null)
            {
                int importIndex = 0;
                while (importIndex < sourceSyntax.ImportedModules.Length)
                {
                    ImportResolution resolution = this.ResolveImportedModule(
                        source,
                        sourceSyntax.ImportedModules[importIndex],
                        tables);
                    sourceSyntax.ImportedModules[importIndex].Resolution = resolution;
                    sourceResolutions.Add(resolution);
                    resolutions.Add(resolution);
                    importIndex = importIndex + 1;
                }
            }

            source.ImportResolutions = sourceResolutions.ToArray();
            this.DiagnoseCrossImportAmbiguity(source.ImportResolutions);
            tableIndex = tableIndex + 1;
        }

        return resolutions.ToArray();
    }

    internal ImportResolution ResolveImportedModule(
        SymbolTable source,
        ImportedModuleSyntax import,
        SymbolTable[] tables)
    {
        System.Collections.Generic.List<SymbolTable> candidates =
            new System.Collections.Generic.List<SymbolTable>();
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index].ModuleName == import.ModuleName
                && this.IdentityMatches(tables[index], import.AssignedIdentifier))
            {
                candidates.Add(tables[index]);
            }

            index = index + 1;
        }

        ImportResolution resolution = new ImportResolution
        {
            SourceModule = source,
            Import = import
        };
        if (candidates.Count == 0)
        {
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Binding,
                3,
                "No module revision matches import '" + this.FormatImportIdentity(import) + "'.",
                this.GetImportIdentitySpan(import),
                null);
            return resolution;
        }

        if (candidates.Count > 1)
        {
            resolution.Ambiguous = true;
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Binding,
                4,
                "The module import '" + this.FormatImportIdentity(import) + "' is ambiguous.",
                this.GetImportIdentitySpan(import),
                this.CreateModuleLocations(candidates));
            return resolution;
        }

        resolution.TargetModule = candidates[0];
        resolution.Symbols = this.ResolveSymbols(import, candidates[0]);
        resolution.Success = resolution.Symbols.Length == import.Symbols.Length;
        return resolution;
    }

    internal ImportedSymbolBinding[] ResolveSymbols(
        ImportedModuleSyntax import,
        SymbolTable target)
    {
        System.Collections.Generic.List<ImportedSymbolBinding> bindings =
            new System.Collections.Generic.List<ImportedSymbolBinding>();
        int importIndex = 0;
        while (importIndex < import.Symbols.Length)
        {
            ImportExportSymbolSyntax importedSymbol = import.Symbols[importIndex];
            System.Collections.Generic.List<SymbolTableEntry> matches =
                new System.Collections.Generic.List<SymbolTableEntry>();
            System.Collections.Generic.List<SymbolTableEntry> exported =
                new System.Collections.Generic.List<SymbolTableEntry>();
            int symbolIndex = 0;
            while (symbolIndex < target.Symbols.Length)
            {
                SymbolTableEntry candidate = target.Symbols[symbolIndex];
                if (candidate.Name == importedSymbol.Name
                    && this.KindsOverlap(candidate.CandidateKinds, importedSymbol.CandidateKinds))
                {
                    matches.Add(candidate);
                    if (candidate.Exported)
                    {
                        exported.Add(candidate);
                    }
                }

                symbolIndex = symbolIndex + 1;
            }

            if (matches.Count == 0)
            {
                this.AddMissingSymbolDiagnostic(importedSymbol, target);
            }
            else if (exported.Count == 0)
            {
                this.AddHiddenSymbolDiagnostic(importedSymbol, target, matches);
            }
            else if (exported.Count > 1)
            {
                this.AddAmbiguousSymbolDiagnostic(importedSymbol, target, exported);
            }
            else
            {
                SymbolTableEntry selected = exported[0];
                bindings.Add(new ImportedSymbolBinding
                {
                    Import = importedSymbol,
                    Target = selected,
                    QualifiedName = selected.QualifiedName,
                    CandidateKinds = this.IntersectKinds(
                        selected.CandidateKinds,
                        importedSymbol.CandidateKinds)
                });
            }

            importIndex = importIndex + 1;
        }

        return bindings.ToArray();
    }

    internal bool IdentityMatches(SymbolTable table, AssignedModuleIdentifierSyntax? identity)
    {
        if (identity == null)
        {
            return true;
        }

        if (identity.HasObjectIdentifier)
        {
            return table.ObjectIdentifier.Length > 0
                && table.ObjectIdentifier == this.BuildAssignedObjectIdentifier(identity);
        }

        if (identity.HasIri)
        {
            return table.Iri.Length > 0 && table.Iri == identity.Iri;
        }

        if (identity.HasDefinedValue)
        {
            return table.Version.Length > 0
                && table.Version == identity.DefinedValue
                && (identity.DefinedValueModule.Length == 0
                    || table.VersionModule == identity.DefinedValueModule);
        }

        return true;
    }

    internal string BuildAssignedObjectIdentifier(AssignedModuleIdentifierSyntax identity)
    {
        string result = "";
        int index = 0;
        while (index < identity.ObjectIdentifierComponents.Length)
        {
            AssignedOidComponentSyntax component = identity.ObjectIdentifierComponents[index];
            string value = this.FormatAssignedOidComponent(component);
            result = result.Length == 0 ? value : result + "." + value;
            index = index + 1;
        }

        return result;
    }

    internal string FormatAssignedOidComponent(AssignedOidComponentSyntax component)
    {
        if (component.HasNumber)
        {
            return component.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (component.HasNumberDefinedValue)
        {
            return component.NumberDefinedValueModule.Length == 0
                ? "$" + component.NumberDefinedValue
                : "$" + component.NumberDefinedValueModule + "." + component.NumberDefinedValue;
        }

        if (component.HasDefinedValue)
        {
            return component.DefinedValueModule.Length == 0
                ? "$" + component.DefinedValue
                : "$" + component.DefinedValueModule + "." + component.DefinedValue;
        }

        return component.Name;
    }

    internal string FormatImportIdentity(ImportedModuleSyntax import)
    {
        AssignedModuleIdentifierSyntax? identity = import.AssignedIdentifier;
        if (identity == null)
        {
            return import.ModuleName;
        }

        if (identity.HasObjectIdentifier)
        {
            return import.ModuleName + " { " + this.BuildAssignedObjectIdentifier(identity) + " }";
        }

        if (identity.HasIri)
        {
            return import.ModuleName + " \"" + identity.Iri + "\"";
        }

        if (identity.HasDefinedValue)
        {
            return import.ModuleName + " " + (identity.DefinedValueModule.Length == 0
                ? identity.DefinedValue
                : identity.DefinedValueModule + "." + identity.DefinedValue);
        }

        return import.ModuleName;
    }

    internal AsnSourceSpan? GetImportIdentitySpan(ImportedModuleSyntax import)
    {
        return import.AssignedIdentifier == null || import.AssignedIdentifier.Span == null
            ? import.Span
            : import.AssignedIdentifier.Span;
    }

    internal AsnDiagnosticRelatedLocation[] CreateModuleLocations(
        System.Collections.Generic.List<SymbolTable> modules)
    {
        int count = 0;
        int index = 0;
        while (index < modules.Count)
        {
            if (modules[index].Module != null && modules[index].Module?.Span != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        AsnDiagnosticRelatedLocation[] locations = new AsnDiagnosticRelatedLocation[count];
        int locationIndex = 0;
        index = 0;
        while (index < modules.Count)
        {
            ModuleSyntax? module = modules[index].Module;
            if (module != null && module.Span != null)
            {
                locations[locationIndex] = new AsnDiagnosticRelatedLocation
                {
                    Message = "A matching module revision is declared here.",
                    Span = module.Span
                };
                locationIndex = locationIndex + 1;
            }

            index = index + 1;
        }

        return locations;
    }

    internal void AddMissingSymbolDiagnostic(ImportExportSymbolSyntax symbol, SymbolTable target)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            5,
            "Module '" + target.ModuleName + "' does not declare imported symbol '" + symbol.Name + "'.",
            symbol.Span,
            null);
    }

    internal void AddHiddenSymbolDiagnostic(
        ImportExportSymbolSyntax symbol,
        SymbolTable target,
        System.Collections.Generic.List<SymbolTableEntry> matches)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            6,
            "Symbol '" + target.ModuleName + "." + symbol.Name + "' is not exported.",
            symbol.Span,
            this.CreateSymbolLocations(matches, "The non-exported declaration is here."));
    }

    internal void AddAmbiguousSymbolDiagnostic(
        ImportExportSymbolSyntax symbol,
        SymbolTable target,
        System.Collections.Generic.List<SymbolTableEntry> matches)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            7,
            "Imported symbol '" + target.ModuleName + "." + symbol.Name + "' is ambiguous.",
            symbol.Span,
            this.CreateSymbolLocations(matches, "A matching exported declaration is here."));
    }

    internal AsnDiagnosticRelatedLocation[] CreateSymbolLocations(
        System.Collections.Generic.List<SymbolTableEntry> symbols,
        string message)
    {
        int count = 0;
        int index = 0;
        while (index < symbols.Count)
        {
            if (symbols[index].Span != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        AsnDiagnosticRelatedLocation[] locations = new AsnDiagnosticRelatedLocation[count];
        int locationIndex = 0;
        index = 0;
        while (index < symbols.Count)
        {
            AsnSourceSpan? span = symbols[index].Span;
            if (span != null)
            {
                locations[locationIndex] = new AsnDiagnosticRelatedLocation
                {
                    Message = message,
                    Span = span
                };
                locationIndex = locationIndex + 1;
            }

            index = index + 1;
        }

        return locations;
    }

    internal void DiagnoseCrossImportAmbiguity(ImportResolution[] resolutions)
    {
        int resolutionIndex = 0;
        while (resolutionIndex < resolutions.Length)
        {
            int bindingIndex = 0;
            while (bindingIndex < resolutions[resolutionIndex].Symbols.Length)
            {
                ImportedSymbolBinding current = resolutions[resolutionIndex].Symbols[bindingIndex];
                ImportedSymbolBinding? previous = this.FindPreviousBinding(
                    resolutions,
                    resolutionIndex,
                    bindingIndex,
                    current);
                if (previous != null && current.Import != null)
                {
                    resolutions[resolutionIndex].Ambiguous = true;
                    resolutions[resolutionIndex].Success = false;
                    AsnDiagnosticRelatedLocation[] related = previous.Import == null
                        || previous.Import.Span == null
                        ? []
                        : [new AsnDiagnosticRelatedLocation
                        {
                            Message = "The other import is here.",
                            Span = previous.Import.Span
                        }];
                    this.Diagnostics.TryAddDiagnostic(
                        AsnDiagnosticSeverity.Error,
                        AsnDiagnosticCategory.Binding,
                        8,
                        "Imported symbol '" + current.Import.Name
                            + "' is supplied by more than one module.",
                        current.Import.Span,
                        related);
                }

                bindingIndex = bindingIndex + 1;
            }

            resolutionIndex = resolutionIndex + 1;
        }
    }

    internal ImportedSymbolBinding? FindPreviousBinding(
        ImportResolution[] resolutions,
        int currentResolution,
        int currentBinding,
        ImportedSymbolBinding current)
    {
        if (current.Import == null)
        {
            return null;
        }

        int resolutionIndex = 0;
        while (resolutionIndex <= currentResolution)
        {
            int limit = resolutionIndex == currentResolution
                ? currentBinding
                : resolutions[resolutionIndex].Symbols.Length;
            int bindingIndex = 0;
            while (bindingIndex < limit)
            {
                ImportedSymbolBinding previous = resolutions[resolutionIndex].Symbols[bindingIndex];
                if (previous.Import != null
                    && previous.Import.Name == current.Import.Name
                    && this.KindsOverlap(previous.CandidateKinds, current.CandidateKinds))
                {
                    return previous;
                }

                bindingIndex = bindingIndex + 1;
            }

            resolutionIndex = resolutionIndex + 1;
        }

        return null;
    }

    internal bool KindsOverlap(AsnAssignmentKind[] first, AsnAssignmentKind[] second)
    {
        int index = 0;
        while (index < first.Length)
        {
            if (this.ContainsKind(second, first[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal AsnAssignmentKind[] IntersectKinds(
        AsnAssignmentKind[] first,
        AsnAssignmentKind[] second)
    {
        int count = 0;
        int index = 0;
        while (index < first.Length)
        {
            if (this.ContainsKind(second, first[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        AsnAssignmentKind[] result = new AsnAssignmentKind[count];
        int resultIndex = 0;
        index = 0;
        while (index < first.Length)
        {
            if (this.ContainsKind(second, first[index]))
            {
                result[resultIndex] = first[index];
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return result;
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
}
