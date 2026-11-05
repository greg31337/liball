using System.Collections.Generic;

namespace iso20022;

internal class GenerationSymbolTableBuilder
{
    internal GenerationSymbolTable BuildSymbolTable(GenerationModel model, string rootNamespace)
    {
        if (model == null)
        {
            throw new System.ArgumentNullException("model");
        }

        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        GenerationNamespacePartitioner partitioner = new GenerationNamespacePartitioner();
        GenerationShapeIdentifierBuilder shapeBuilder = new GenerationShapeIdentifierBuilder();
        GenerationSymbolTable table = new GenerationSymbolTable();
        table.RootNamespace = namer.CreateNamespaceRoot(rootNamespace);
        int index = 0;
        while (index < model.Concepts.Count)
        {
            GenerationConcept concept = model.Concepts[index];
            GenerationSymbolEntry symbol = new GenerationSymbolEntry();
            symbol.Concept = concept;
            symbol.SourceIdentifier = FindSourceIdentifier(model, concept.SourceOrdinal);
            symbol.NamespaceName = partitioner.CreateNamespace(model, concept, table.RootNamespace);
            string candidateName = FindCandidateName(concept);
            symbol.BaseName = namer.CreateTypeName(candidateName);
            symbol.SymbolName = symbol.BaseName;
            symbol.ShapeIdentifier = shapeBuilder.CreateShapeIdentifier(concept);
            symbol.CollisionIdentity = candidateName + "\n" + symbol.ShapeIdentifier;
            table.Symbols.Add(symbol);
            index++;
        }

        ResolveCollisions(table.Symbols, namer);
        SortSymbols(table.Symbols);
        int ordinal = 0;
        while (ordinal < table.Symbols.Count)
        {
            table.Symbols[ordinal].StableOrdinal = ordinal;
            ordinal++;
        }
        return table;
    }

    internal string FindCandidateName(GenerationConcept concept)
    {
        if (concept.Name.Length > 0)
        {
            return concept.Name;
        }
        if (concept.CodeValue != null && concept.CodeValue.Value.Length > 0)
        {
            return concept.CodeValue.Value;
        }
        if (concept.Identifier.Length > 0)
        {
            return concept.Identifier;
        }
        return concept.Kind.ToString();
    }

    internal string FindSourceIdentifier(GenerationModel model, int sourceOrdinal)
    {
        int index = 0;
        while (index < model.Sources.Count)
        {
            GenerationSource source = model.Sources[index];
            if (source.Ordinal == sourceOrdinal)
            {
                return source.Identifier;
            }
            index++;
        }
        return string.Empty;
    }

    internal void ResolveCollisions(
        List<GenerationSymbolEntry> symbols,
        GenerationIdentifierNamer namer)
    {
        Dictionary<string, string> firstIdentities = new Dictionary<string, string>();
        HashSet<string> collisions = new HashSet<string>();
        int index = 0;
        while (index < symbols.Count)
        {
            GenerationSymbolEntry symbol = symbols[index];
            string key = symbol.NamespaceName + "\n" + symbol.BaseName;
            if (!firstIdentities.ContainsKey(key))
            {
                firstIdentities.Add(key, symbol.CollisionIdentity);
            }
            else if (firstIdentities[key] != symbol.CollisionIdentity)
            {
                collisions.Add(key);
            }
            index++;
        }
        index = 0;
        while (index < symbols.Count)
        {
            GenerationSymbolEntry symbol = symbols[index];
            string key = symbol.NamespaceName + "\n" + symbol.BaseName;
            if (collisions.Contains(key))
            {
                string suffix = GenerationStableHash.CreateHex(symbol.CollisionIdentity);
                symbol.SymbolName = namer.AppendDisambiguator(symbol.BaseName, suffix);
                symbol.HasCollision = true;
            }
            index++;
        }
    }

    internal void SortSymbols(List<GenerationSymbolEntry> symbols)
    {
        symbols.Sort((left, right) => CompareSymbols(left, right));
    }

    internal int CompareSymbols(GenerationSymbolEntry left, GenerationSymbolEntry right)
    {
        int comparison = string.CompareOrdinal(left.NamespaceName, right.NamespaceName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.SymbolName, right.SymbolName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.ShapeIdentifier, right.ShapeIdentifier);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = ((int)left.Concept.Kind).CompareTo((int)right.Concept.Kind);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = ((int)left.Concept.SourceKind).CompareTo((int)right.Concept.SourceKind);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.SourceIdentifier, right.SourceIdentifier);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Concept.Identifier, right.Concept.Identifier);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Concept.Name, right.Concept.Name);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Concept.Definition, right.Concept.Definition);
    }
}
