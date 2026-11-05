namespace iso20022;

internal class GenerationNamespacePartitioner
{
    internal string CreateNamespace(
        GenerationModel model,
        GenerationConcept concept,
        string rootNamespace)
    {
        if (model == null)
        {
            throw new System.ArgumentNullException("model");
        }
        if (concept == null)
        {
            throw new System.ArgumentNullException("concept");
        }

        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        string root = namer.CreateNamespaceRoot(rootNamespace);
        GenerationSource? source = FindSource(model, concept.SourceOrdinal);
        if (concept.SourceKind == GenerationSourceKind.Xsd)
        {
            return CreateSchemaNamespace(model, concept, source, root, namer);
        }
        return CreateRepositoryNamespace(model, concept, root, namer);
    }

    internal string CreateSchemaNamespace(
        GenerationModel model,
        GenerationConcept concept,
        GenerationSource? source,
        string root,
        GenerationIdentifierNamer namer)
    {
        MessageIdentifier? identifier = FindMessageIdentifier(model, concept, source);
        if (identifier != null && identifier.BusinessArea.Length > 0)
        {
            string area = namer.CreateTypeName(identifier.BusinessArea);
            string message = CreateMessageSegment(identifier, namer);
            return root + ".Messages." + area + "." + message;
        }

        string namespaceUri = string.Empty;
        if (source != null)
        {
            namespaceUri = source.NamespaceUri;
        }
        string hash = GenerationStableHash.CreateHex(namespaceUri).Substring(0, 16);
        return root + ".Schemas.N" + hash;
    }

    internal string CreateRepositoryNamespace(
        GenerationModel model,
        GenerationConcept concept,
        string root,
        GenerationIdentifierNamer namer)
    {
        if (concept.Kind == GenerationConceptKind.MessageDefinition ||
            concept.Kind == GenerationConceptKind.MessageBuildingBlock)
        {
            MessageIdentifier? identifier = FindMessageIdentifier(model, concept, null);
            if (identifier != null && identifier.BusinessArea.Length > 0)
            {
                return root + ".Messages." + namer.CreateTypeName(identifier.BusinessArea);
            }
            return root + ".Messages.General";
        }
        if (concept.Kind == GenerationConceptKind.MessageComponent ||
            concept.Kind == GenerationConceptKind.MessageElement ||
            concept.Kind == GenerationConceptKind.Choice)
        {
            return root + ".Components";
        }
        if (concept.Kind == GenerationConceptKind.DataType)
        {
            return root + ".Types";
        }
        if (concept.Kind == GenerationConceptKind.CodeSet ||
            concept.Kind == GenerationConceptKind.CodeValue)
        {
            return root + ".Codes";
        }
        if (concept.Kind == GenerationConceptKind.ExternalSchema)
        {
            return root + ".External";
        }
        return root + ".Other";
    }

    internal MessageIdentifier? FindMessageIdentifier(
        GenerationModel model,
        GenerationConcept concept,
        GenerationSource? source)
    {
        if (concept.MessageDefinition != null &&
            concept.MessageDefinition.MessageIdentifier.BusinessArea.Length > 0)
        {
            return concept.MessageDefinition.MessageIdentifier;
        }
        if (concept.MessageBuildingBlock != null)
        {
            string target = concept.MessageBuildingBlock.MessageDefinition.TargetIdentifier;
            int index = 0;
            while (index < model.Concepts.Count)
            {
                GenerationConcept candidate = model.Concepts[index];
                if (candidate.SourceOrdinal == concept.SourceOrdinal &&
                    candidate.Kind == GenerationConceptKind.MessageDefinition &&
                    candidate.Identifier == target && candidate.MessageDefinition != null &&
                    candidate.MessageDefinition.MessageIdentifier.BusinessArea.Length > 0)
                {
                    return candidate.MessageDefinition.MessageIdentifier;
                }
                index++;
            }
        }
        if (source != null && source.Schema != null && source.Schema.MessageIdentifier != null)
        {
            return source.Schema.MessageIdentifier;
        }

        int messageIndex = 0;
        while (source != null && messageIndex < model.Concepts.Count)
        {
            GenerationConcept candidate = model.Concepts[messageIndex];
            if (candidate.SourceOrdinal == concept.SourceOrdinal &&
                candidate.Kind == GenerationConceptKind.MessageDefinition &&
                candidate.MessageDefinition != null &&
                candidate.MessageDefinition.MessageIdentifier.BusinessArea.Length > 0)
            {
                return candidate.MessageDefinition.MessageIdentifier;
            }
            messageIndex++;
        }
        return null;
    }

    internal string CreateMessageSegment(
        MessageIdentifier identifier,
        GenerationIdentifierNamer namer)
    {
        string value = identifier.BusinessArea + identifier.MessageFunctionality +
            identifier.Variant + identifier.Version;
        return namer.CreateTypeName(value);
    }

    internal GenerationSource? FindSource(GenerationModel model, int ordinal)
    {
        int index = 0;
        while (index < model.Sources.Count)
        {
            if (model.Sources[index].Ordinal == ordinal)
            {
                return model.Sources[index];
            }
            index++;
        }
        return null;
    }
}
