namespace iso20022;

internal class GenerationSymbolEntry
{
    internal GenerationConcept Concept = new GenerationConcept();
    internal string SourceIdentifier = string.Empty;
    internal string NamespaceName = string.Empty;
    internal string BaseName = string.Empty;
    internal string SymbolName = string.Empty;
    internal string ShapeIdentifier = string.Empty;
    internal string CollisionIdentity = string.Empty;
    internal bool HasCollision;
    internal int StableOrdinal;
}
