using System.Collections.Generic;

namespace iso20022;

internal class CSharpGenerationContext
{
    internal GenerationPlan Plan = new GenerationPlan();
    internal GenerationResult Result = new GenerationResult();
    internal GenerationModel Model = new GenerationModel();
    internal GenerationSymbolTable Symbols = new GenerationSymbolTable();
    internal List<GenerationConcept> Concepts = new List<GenerationConcept>();
    internal Dictionary<string, GenerationConcept> ConceptsByIdentity =
        new Dictionary<string, GenerationConcept>();
    internal Dictionary<string, GenerationSymbolEntry> SymbolsByIdentity =
        new Dictionary<string, GenerationSymbolEntry>();
    internal HashSet<string> PublicSymbolNames = new HashSet<string>();
    internal HashSet<string> PublicTypeNames = new HashSet<string>();
}
