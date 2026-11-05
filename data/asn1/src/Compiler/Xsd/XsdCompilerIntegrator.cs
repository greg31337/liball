namespace asn1;

internal class XsdCompilerIntegrator
{
    internal void AttachSourceMappings(
        AsnSourceUnit source,
        ModuleSyntax[] parsedModules,
        XsdModuleGenerationResult generation)
    {
        int generatedIndex = 0;
        while (generatedIndex < generation.Modules.Length)
        {
            XsdGeneratedModule generated = generation.Modules[generatedIndex];
            if (generated.SourceUnit == source)
            {
                int moduleIndex = 0;
                while (moduleIndex < parsedModules.Length)
                {
                    if (parsedModules[moduleIndex].Name
                        == generated.ModuleName)
                    {
                        parsedModules[moduleIndex].XsdSourceMappings =
                            generated.XsdSourceMappings;
                    }
                    moduleIndex = moduleIndex + 1;
                }
            }
            generatedIndex = generatedIndex + 1;
        }
    }

    internal SymbolTable[] AddSupportTable(
        SymbolTable[] tables,
        ModuleSyntax[] modules,
        bool importedXsd)
    {
        if (!importedXsd || !this.NeedsSupportModule(modules)
            || this.ContainsSupportTable(tables))
        {
            return tables;
        }
        SymbolTable[] result = new SymbolTable[tables.Length + 1];
        int index = 0;
        while (index < tables.Length)
        {
            result[index] = tables[index];
            index = index + 1;
        }
        result[index] = new XsdV2SemanticModuleBuilder().BuildTable();
        return result;
    }

    internal bool NeedsSupportModule(ModuleSyntax[] modules)
    {
        int moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            ImportedModuleSyntax[] imports =
                modules[moduleIndex].ImportedModules;
            int importIndex = 0;
            while (importIndex < imports.Length)
            {
                if (imports[importIndex].ModuleName == "XSDv2")
                {
                    return true;
                }
                importIndex = importIndex + 1;
            }
            moduleIndex = moduleIndex + 1;
        }
        return false;
    }

    internal bool ContainsSupportTable(SymbolTable[] tables)
    {
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index].ModuleName == "XSDv2"
                && tables[index].ObjectIdentifier == "2.1.0.0.2.2")
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
