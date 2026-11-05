namespace asn1;

internal class EcnEncodingClassCompiler
{
    internal DiagnosticBag Diagnostics;

    internal EcnEncodingClassCompiler(DiagnosticBag diagnostics)
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

        bool success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            EcnSymbolTable table = tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.LocalSymbols.Length)
            {
                EcnSymbol symbol = table.LocalSymbols[symbolIndex];
                if (symbol.Kind == EcnSymbolKind.EncodingClass
                    || symbol.Kind == EcnSymbolKind.EncodingStructure)
                {
                    EcnEncodingClassDefinition definition =
                        new EcnEncodingStructureParser(this.Diagnostics)
                            .ParseClassDefinition(symbol, table);
                    symbol.ClassDefinition = definition;
                    if (!definition.Valid)
                    {
                        success = false;
                    }
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
                    imported.ClassDefinition =
                        imported.ImportedTarget.ClassDefinition;
                }

                importIndex = importIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }
}
