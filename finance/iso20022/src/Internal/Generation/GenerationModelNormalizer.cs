namespace iso20022;

internal class GenerationModelNormalizer
{
    internal GenerationModel NormalizeRepositorySnapshot(RepositorySnapshot snapshot)
    {
        GenerationModel model = new GenerationModel();
        AppendRepositorySnapshot(model, snapshot);
        return model;
    }

    internal GenerationModel NormalizeSchemaArtifact(SchemaArtifact schema)
    {
        GenerationModel model = new GenerationModel();
        AppendSchemaArtifact(model, schema);
        return model;
    }

    internal void AppendRepositorySnapshot(GenerationModel model, RepositorySnapshot snapshot)
    {
        if (model == null)
        {
            throw new System.ArgumentNullException("model");
        }
        if (snapshot == null)
        {
            throw new System.ArgumentNullException("snapshot");
        }

        int sourceOrdinal = model.Sources.Count;
        GenerationSource source = new GenerationSource();
        source.Kind = GenerationSourceKind.Repository;
        source.Ordinal = sourceOrdinal;
        source.Identifier = snapshot.Identifier;
        source.Edition = snapshot.Edition;
        source.Artifact = snapshot.Provenance.Artifact;
        source.Repository = snapshot;
        model.Sources.Add(source);

        AppendMessageDefinitions(model, snapshot.MessageDefinitions, sourceOrdinal,
            GenerationSourceKind.Repository);
        AppendMessageBuildingBlocks(model, snapshot.MessageBuildingBlocks, sourceOrdinal,
            GenerationSourceKind.Repository);
        AppendMessageComponents(model, snapshot.MessageComponents, sourceOrdinal,
            GenerationSourceKind.Repository);
        AppendMessageElements(model, snapshot.MessageElements, sourceOrdinal,
            GenerationSourceKind.Repository);
        AppendChoices(model, snapshot.Choices, sourceOrdinal, GenerationSourceKind.Repository);
        AppendDataTypes(model, snapshot.DataTypes, sourceOrdinal, GenerationSourceKind.Repository);
        AppendCodeSets(model, snapshot.CodeSets, sourceOrdinal, GenerationSourceKind.Repository);
        AppendExternalSchemas(model, snapshot.ExternalSchemas, sourceOrdinal,
            GenerationSourceKind.Repository, false);
    }

    internal void AppendSchemaArtifact(GenerationModel model, SchemaArtifact schema)
    {
        if (model == null)
        {
            throw new System.ArgumentNullException("model");
        }
        if (schema == null)
        {
            throw new System.ArgumentNullException("schema");
        }

        int sourceOrdinal = model.Sources.Count;
        GenerationSource source = new GenerationSource();
        source.Kind = GenerationSourceKind.Xsd;
        source.Ordinal = sourceOrdinal;
        source.Identifier = schema.SourceName;
        source.NamespaceUri = schema.NamespaceUri;
        source.Version = schema.Version;
        source.Profile = schema.Profile;
        source.Artifact = schema.Artifact;
        source.Schema = schema;
        model.Sources.Add(source);

        AppendMessageDefinitions(model, schema.MessageDefinitions, sourceOrdinal,
            GenerationSourceKind.Xsd);
        AppendMessageComponents(model, schema.MessageComponents, sourceOrdinal,
            GenerationSourceKind.Xsd);
        AppendMessageElements(model, schema.MessageElements, sourceOrdinal,
            GenerationSourceKind.Xsd);
        AppendDataTypes(model, schema.DataTypes, sourceOrdinal, GenerationSourceKind.Xsd);
        AppendExternalSchemas(model, schema.RepositoryFallbackDefinitions, sourceOrdinal,
            GenerationSourceKind.Xsd, true);
    }

    internal void AppendMessageDefinitions(
        GenerationModel model,
        System.Collections.Generic.List<MessageDefinitionMetadata> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            MessageDefinitionMetadata value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.MessageDefinition, index, value.Identifier, value.Name,
                value.Definition);
            concept.MessageDefinition = value;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendMessageBuildingBlocks(
        GenerationModel model,
        System.Collections.Generic.List<MessageBuildingBlockDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            MessageBuildingBlockDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.MessageBuildingBlock, index, value.Identifier, value.Name,
                value.Definition);
            concept.MessageBuildingBlock = value;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendMessageComponents(
        GenerationModel model,
        System.Collections.Generic.List<MessageComponentDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            MessageComponentDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.MessageComponent, index, value.Identifier, value.Name,
                value.Definition);
            concept.MessageComponent = value;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendMessageElements(
        GenerationModel model,
        System.Collections.Generic.List<MessageElementDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            MessageElementDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.MessageElement, index, value.Identifier, value.Name,
                value.Definition);
            concept.MessageElement = value;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendChoices(
        GenerationModel model,
        System.Collections.Generic.List<ChoiceDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            ChoiceDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.Choice, index, value.Identifier, value.Name,
                value.Definition);
            concept.Choice = value;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendDataTypes(
        GenerationModel model,
        System.Collections.Generic.List<DataTypeDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            DataTypeDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.DataType, index, value.Identifier, value.Name,
                value.Definition);
            concept.DataType = value;
            model.Concepts.Add(concept);
            AppendFacets(model, value.Facets, sourceOrdinal, sourceKind,
                GenerationConceptKind.DataType, value.Identifier, index);
            index++;
        }
    }

    internal void AppendCodeSets(
        GenerationModel model,
        System.Collections.Generic.List<CodeSetDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind)
    {
        int index = 0;
        while (index < values.Count)
        {
            CodeSetDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.CodeSet, index, value.Identifier, value.Name,
                value.Definition);
            concept.CodeSet = value;
            model.Concepts.Add(concept);
            AppendFacets(model, value.Facets, sourceOrdinal, sourceKind,
                GenerationConceptKind.CodeSet, value.Identifier, index);
            AppendCodeValues(model, value.Codes, sourceOrdinal, sourceKind, value.Identifier);
            index++;
        }
    }

    internal void AppendExternalSchemas(
        GenerationModel model,
        System.Collections.Generic.List<ExternalSchemaDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind,
        bool isRepositoryFallback)
    {
        int index = 0;
        while (index < values.Count)
        {
            ExternalSchemaDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.ExternalSchema, index, value.Identifier, value.Name,
                value.Definition);
            concept.ExternalSchema = value;
            concept.IsRepositoryFallback = isRepositoryFallback;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendCodeValues(
        GenerationModel model,
        System.Collections.Generic.List<CodeValueDefinition> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind,
        string ownerIdentifier)
    {
        int index = 0;
        while (index < values.Count)
        {
            CodeValueDefinition value = values[index];
            GenerationConcept concept = CreateConcept(model, sourceOrdinal, sourceKind,
                GenerationConceptKind.CodeValue, index, value.Identifier, value.Name,
                value.Definition);
            concept.OwnerIdentifier = ownerIdentifier;
            concept.CodeValue = value;
            model.Concepts.Add(concept);
            index++;
        }
    }

    internal void AppendFacets(
        GenerationModel model,
        System.Collections.Generic.List<DataTypeFacet> values,
        int sourceOrdinal,
        GenerationSourceKind sourceKind,
        GenerationConceptKind ownerKind,
        string ownerIdentifier,
        int declarationOrdinal)
    {
        int index = 0;
        while (index < values.Count)
        {
            GenerationFacet facet = new GenerationFacet();
            facet.SourceKind = sourceKind;
            facet.OwnerKind = ownerKind;
            facet.SourceOrdinal = sourceOrdinal;
            facet.DeclarationOrdinal = declarationOrdinal;
            facet.FacetOrdinal = index;
            facet.OwnerIdentifier = ownerIdentifier;
            facet.Facet = values[index];
            model.Facets.Add(facet);
            index++;
        }
    }

    internal GenerationConcept CreateConcept(
        GenerationModel model,
        int sourceOrdinal,
        GenerationSourceKind sourceKind,
        GenerationConceptKind conceptKind,
        int declarationOrdinal,
        string identifier,
        string name,
        string definition)
    {
        GenerationConcept concept = new GenerationConcept();
        concept.SourceKind = sourceKind;
        concept.Kind = conceptKind;
        concept.SourceOrdinal = sourceOrdinal;
        concept.ConceptOrdinal = model.Concepts.Count;
        concept.DeclarationOrdinal = declarationOrdinal;
        concept.Identifier = identifier;
        concept.Name = name;
        concept.Definition = definition;
        return concept;
    }
}
