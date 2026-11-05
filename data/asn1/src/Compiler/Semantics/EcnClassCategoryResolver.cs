namespace asn1;

internal class EcnClassCategoryResolver
{
    internal DiagnosticBag Diagnostics;

    internal EcnClassCategoryResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool ResolveTables(EcnSymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        bool success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                EcnSymbol symbol = tables[tableIndex].Symbols[symbolIndex];
                if (this.RequiresCategory(symbol)
                    && !this.ResolveSymbolCategory(symbol))
                {
                    success = false;
                }

                symbolIndex = symbolIndex + 1;
            }

            symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Parameters.Length)
            {
                EcnSymbol parameter = tables[tableIndex].Parameters[symbolIndex];
                if (parameter.GovernorKind == EcnGovernorKind.EncodingClass
                    || parameter.GovernorKind
                        == EcnGovernorKind.EncodingClassField)
                {
                    this.ResolveSymbolCategory(parameter);
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal bool RequiresCategory(EcnSymbol symbol)
    {
        return symbol.Kind == EcnSymbolKind.EncodingClass
            || symbol.Kind == EcnSymbolKind.EncodingStructure
            || symbol.Kind == EcnSymbolKind.EncodingObject;
    }

    internal bool ResolveSymbolCategory(EcnSymbol symbol)
    {
        if (symbol.CategoryResolutionState == 2)
        {
            return true;
        }

        if (symbol.CategoryResolutionState == 3)
        {
            return false;
        }

        if (symbol.CategoryResolutionState == 1)
        {
            symbol.CategoryResolutionState = 3;
            this.Diagnostics.TryAddDiagnostic(
                AsnDiagnosticSeverity.Error,
                AsnDiagnosticCategory.Ecn,
                EcnDiagnosticCodes.ClassCategoryCycle,
                "The ECN encoding class '" + symbol.QualifiedName
                    + "' has a cyclic category definition.",
                symbol.Span,
                null);
            return false;
        }

        symbol.CategoryResolutionState = 1;
        if (symbol.ImportedAsnType
            || symbol.Kind == EcnSymbolKind.EncodingStructure)
        {
            this.ApplyCategory(
                symbol,
                EcnClassCategory.EncodingStructure,
                symbol.Name);
            return true;
        }

        if (symbol.ImportedTarget != null)
        {
            if (this.ResolveSymbolCategory(symbol.ImportedTarget))
            {
                this.CopyCategory(symbol, symbol.ImportedTarget);
                return true;
            }

            symbol.CategoryResolutionState = 3;
            return false;
        }

        if (symbol.GovernorBuiltIn)
        {
            EcnBuiltInClassDefinition? definition =
                EcnBuiltInClassCatalog.FindClass(symbol.GovernorName);
            if (definition != null)
            {
                this.ApplyDefinition(symbol, definition);
                return true;
            }
        }

        if (symbol.GovernorSymbol != null
            && symbol.GovernorSymbol.Kind != EcnSymbolKind.Parameter)
        {
            if (this.ResolveSymbolCategory(symbol.GovernorSymbol))
            {
                this.CopyCategory(symbol, symbol.GovernorSymbol);
                return true;
            }
        }

        symbol.CategoryResolutionState = 3;
        return false;
    }

    internal void ApplyDefinition(
        EcnSymbol symbol,
        EcnBuiltInClassDefinition definition)
    {
        symbol.ClassCategory = definition.Category;
        symbol.ClassCategoryGroups = this.CopyGroups(
            definition.CategoryGroups);
        symbol.PrimitiveClassName = definition.PrimitiveName;
        symbol.CategoryResolutionState = 2;
    }

    internal void ApplyCategory(
        EcnSymbol symbol,
        EcnClassCategory category,
        string primitiveName)
    {
        symbol.ClassCategory = category;
        symbol.ClassCategoryGroups = EcnBuiltInClassCatalog
            .GetGroupsForCategory(category);
        symbol.PrimitiveClassName = primitiveName;
        symbol.CategoryResolutionState = 2;
    }

    internal void CopyCategory(EcnSymbol destination, EcnSymbol source)
    {
        destination.ClassCategory = source.ClassCategory;
        destination.ClassCategoryGroups = this.CopyGroups(
            source.ClassCategoryGroups);
        destination.PrimitiveClassName = source.PrimitiveClassName;
        destination.CategoryResolutionState = 2;
    }

    internal EcnClassCategoryGroup[] CopyGroups(
        EcnClassCategoryGroup[] groups)
    {
        EcnClassCategoryGroup[] copy =
            new EcnClassCategoryGroup[groups.Length];
        int index = 0;
        while (index < groups.Length)
        {
            copy[index] = groups[index];
            index = index + 1;
        }

        return copy;
    }
}
