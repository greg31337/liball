using System;
using System.Collections.Generic;

namespace iso20022;

internal static class DataDictionaryImporter
{
    internal static RepositorySnapshot ImportDataDictionary(XmiPassTwoResult graph)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot();
        PopulateDataDictionary(graph, snapshot);
        return snapshot;
    }

    internal static void PopulateDataDictionary(
        XmiPassTwoResult graph,
        RepositorySnapshot snapshot)
    {
        if (graph == null)
        {
            throw new ArgumentNullException("graph");
        }

        if (snapshot == null)
        {
            throw new ArgumentNullException("snapshot");
        }

        DataDictionaryImportContext context = new DataDictionaryImportContext(graph);
        DataDictionaryEntityMapper mapper = new DataDictionaryEntityMapper(context);
        if (graph.Entities.Count == 0)
        {
            throw new InvalidOperationException("The XMI graph does not contain a repository root.");
        }

        XmiPassTwoEntity root = graph.Entities[0];
        snapshot.Identifier = root.Identifier;
        XmiPassTwoReference? dictionaryReference = context.FindFirstReference(root, "dataDictionary");
        if (dictionaryReference != null)
        {
            snapshot.DataDictionary = context.CopyReference(dictionaryReference);
            snapshot.References.Add(context.CopyReference(dictionaryReference));
        }

        int entityIndex = 0;
        while (entityIndex < graph.Entities.Count)
        {
            XmiPassTwoEntity entity = graph.Entities[entityIndex];
            if (entity.TopLevelPartitionKind == RepositoryEntityKind.DataDictionary ||
                entity.Kind == RepositoryEntityKind.DataDictionary)
            {
                AddEntityReferences(context, entity, snapshot.References);
                if (entity.Kind == RepositoryEntityKind.BusinessComponent)
                {
                    snapshot.BusinessComponents.Add(mapper.BuildBusinessComponent(entity));
                }

                if (entity.Kind == RepositoryEntityKind.BusinessAttribute ||
                    entity.Kind == RepositoryEntityKind.BusinessAssociationEnd)
                {
                    snapshot.BusinessElements.Add(mapper.BuildBusinessElement(entity));
                }

                if (entity.Kind == RepositoryEntityKind.DataType)
                {
                    snapshot.DataTypes.Add(mapper.BuildDataType(entity));
                }

                if (entity.Kind == RepositoryEntityKind.CodeSet)
                {
                    snapshot.CodeSets.Add(mapper.BuildCodeSet(entity));
                }

                if (entity.Kind == RepositoryEntityKind.ExternalSchema)
                {
                    snapshot.ExternalSchemas.Add(mapper.BuildExternalSchema(entity));
                }
            }

            entityIndex++;
        }
    }

    internal static void AddEntityReferences(
        DataDictionaryImportContext context,
        XmiPassTwoEntity entity,
        List<RepositoryReference> destination)
    {
        List<XmiPassTwoReference> references = context.GetSourceReferences(entity);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            destination.Add(context.CopyReference(references[referenceIndex]));
            referenceIndex++;
        }
    }
}
