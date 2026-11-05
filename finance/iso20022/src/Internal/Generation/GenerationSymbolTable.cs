using System.Collections.Generic;

namespace iso20022;

internal class GenerationSymbolTable
{
    internal string RootNamespace = string.Empty;
    internal List<GenerationSymbolEntry> Symbols = new List<GenerationSymbolEntry>();
}
