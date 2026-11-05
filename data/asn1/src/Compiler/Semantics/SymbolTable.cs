namespace asn1;

internal class SymbolTable
{
    internal string ModuleName = "";
    internal string ObjectIdentifier = "";
    internal string Iri = "";
    internal string VersionModule = "";
    internal string Version = "";
    internal bool PredeclaredXsdSupport;
    internal ModuleSyntax? Module;
    internal SymbolTableEntry[] Symbols = [];
    internal SymbolTableEntry[] TypeSymbols = [];
    internal SymbolTableEntry[] ValueSymbols = [];
    internal SymbolTableEntry[] ValueSetSymbols = [];
    internal SymbolTableEntry[] ObjectClassSymbols = [];
    internal SymbolTableEntry[] ObjectSymbols = [];
    internal SymbolTableEntry[] ObjectSetSymbols = [];
    internal ImportResolution[] ImportResolutions = [];
    internal SymbolTableEntry[] XerImportedInstructionTargets = [];
    internal AsnEncodingInstruction[] XerImportedInstructions = [];

    internal SymbolTableEntry? FindSymbol(string name, AsnAssignmentKind kind)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        SymbolTableEntry[] category = this.GetCategorySymbols(kind);
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

    internal SymbolTableEntry[] FindSymbols(string name, AsnAssignmentKind kind)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        SymbolTableEntry[] category = this.GetCategorySymbols(kind);
        int count = 0;
        int index = 0;
        while (index < category.Length)
        {
            if (category[index].Name == name)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        SymbolTableEntry[] matches = new SymbolTableEntry[count];
        int resultIndex = 0;
        index = 0;
        while (index < category.Length)
        {
            if (category[index].Name == name)
            {
                matches[resultIndex] = category[index];
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return matches;
    }

    internal bool IsExported(string name, AsnAssignmentKind kind)
    {
        SymbolTableEntry? symbol = this.FindSymbol(name, kind);
        return symbol != null && symbol.Exported;
    }

    internal SymbolTableEntry[] GetCategorySymbols(AsnAssignmentKind kind)
    {
        switch (kind)
        {
            case AsnAssignmentKind.Type:
                return this.TypeSymbols;
            case AsnAssignmentKind.Value:
                return this.ValueSymbols;
            case AsnAssignmentKind.ValueSet:
                return this.ValueSetSymbols;
            case AsnAssignmentKind.ObjectClass:
                return this.ObjectClassSymbols;
            case AsnAssignmentKind.Object:
                return this.ObjectSymbols;
            case AsnAssignmentKind.ObjectSet:
                return this.ObjectSetSymbols;
            default:
                return [];
        }
    }
}
