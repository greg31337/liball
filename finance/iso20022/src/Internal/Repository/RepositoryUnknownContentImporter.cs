using System;
using System.Collections.Generic;

namespace iso20022;

internal static class RepositoryUnknownContentImporter
{
    internal static RepositorySnapshot ImportUnknownContent(XmiPassTwoResult graph)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot();
        PopulateUnknownContent(graph, snapshot);
        return snapshot;
    }

    internal static void PopulateUnknownContent(
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

        if (graph.Entities.Count == 0)
        {
            throw new InvalidOperationException("The XMI graph does not contain a repository root.");
        }

        DataDictionaryImportContext context = new DataDictionaryImportContext(graph);
        RepositoryUnknownContentMapper mapper = new RepositoryUnknownContentMapper(
            context,
            new Iso20022Ecore2013Adapter());
        snapshot.Identifier = graph.Entities[0].Identifier;
        int entityIndex = 0;
        while (entityIndex < graph.Entities.Count)
        {
            XmiPassTwoEntity entity = graph.Entities[entityIndex];
            if (mapper.IsUnknownElement(entity))
            {
                XmiPassTwoEntity? parent = context.FindParent(entity);
                if (parent != null && !mapper.IsUnknownElement(parent))
                {
                    RepositoryUnknownData unknown = mapper.BuildUnknownElement(
                        entity,
                        parent.Identifier,
                        parent.Kind);
                    snapshot.UnknownContent.Add(unknown);
                    RepositoryUnknownOwnerAttacher.AttachUnknownContent(snapshot, unknown);
                }
            }
            else
            {
                List<RepositoryUnknownData> attributes = new List<RepositoryUnknownData>();
                mapper.AppendUnknownAttributes(entity, attributes);
                int attributeIndex = 0;
                while (attributeIndex < attributes.Count)
                {
                    RepositoryUnknownData unknown = attributes[attributeIndex];
                    snapshot.UnknownContent.Add(unknown);
                    RepositoryUnknownOwnerAttacher.AttachUnknownContent(snapshot, unknown);
                    attributeIndex++;
                }
            }

            entityIndex++;
        }

        AppendPreservedReferences(context, mapper, graph, snapshot.References);
    }

    internal static void AppendPreservedReferences(
        DataDictionaryImportContext context,
        RepositoryUnknownContentMapper mapper,
        XmiPassTwoResult graph,
        List<RepositoryReference> destination)
    {
        int referenceIndex = 0;
        while (referenceIndex < graph.Relationships.Count)
        {
            XmiPassTwoReference source = graph.Relationships[referenceIndex];
            XmiPassTwoEntity entity = graph.Entities[source.SourceDocumentOrder];
            if (!source.IsResolved || mapper.IsUnknownElement(entity))
            {
                RepositoryReference reference = context.CopyReference(source);
                if (!ContainsReference(destination, reference))
                {
                    destination.Add(reference);
                }
            }

            referenceIndex++;
        }
    }

    internal static bool ContainsReference(
        List<RepositoryReference> references,
        RepositoryReference candidate)
    {
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            RepositoryReference existing = references[referenceIndex];
            if (existing.SourceIdentifier == candidate.SourceIdentifier &&
                existing.SourceKind == candidate.SourceKind &&
                existing.PropertyName == candidate.PropertyName &&
                existing.Position == candidate.Position &&
                existing.TargetIdentifier == candidate.TargetIdentifier &&
                existing.RawValue == candidate.RawValue &&
                existing.ErrorCode == candidate.ErrorCode)
            {
                return true;
            }

            referenceIndex++;
        }

        return false;
    }
}
