namespace asn1;

internal class EcnSymbolTable
{
    internal string ModuleName = "";
    internal EcnModuleSyntax? Module;
    internal EcnScope RootScope = new EcnScope();
    internal EcnSymbol[] Symbols = [];
    internal EcnSymbol[] LocalSymbols = [];
    internal EcnSymbol[] ImportedSymbols = [];
    internal EcnSymbol[] EncodingClasses = [];
    internal EcnSymbol[] EncodingObjects = [];
    internal EcnSymbol[] EncodingObjectSets = [];
    internal EcnSymbol[] EncodingStructures = [];
    internal EcnSymbol[] Parameters = [];

    internal EcnSymbol? FindSymbol(string name, EcnSymbolKind kind)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        EcnSymbol[] category = this.GetCategorySymbols(kind);
        int index = 0;
        while (index < category.Length)
        {
            if (category[index].Name == name)
            {
                return category[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal EcnSymbol? FindClassSymbol(string name)
    {
        EcnSymbol? symbol = this.FindSymbol(name, EcnSymbolKind.EncodingClass);
        return symbol == null
            ? this.FindSymbol(name, EcnSymbolKind.EncodingStructure)
            : symbol;
    }

    internal EcnSymbol[] GetCategorySymbols(EcnSymbolKind kind)
    {
        switch (kind)
        {
            case EcnSymbolKind.EncodingClass:
                return this.EncodingClasses;
            case EcnSymbolKind.EncodingObject:
                return this.EncodingObjects;
            case EcnSymbolKind.EncodingObjectSet:
                return this.EncodingObjectSets;
            case EcnSymbolKind.EncodingStructure:
                return this.EncodingStructures;
            case EcnSymbolKind.Parameter:
                return this.Parameters;
            default:
                return [];
        }
    }
}
