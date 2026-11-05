using System;
using System.Collections.Generic;

namespace iso20022;

internal static class BusinessProcessCatalogueImporter
{
    internal static RepositorySnapshot ImportBusinessProcessCatalogue(XmiPassTwoResult graph)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot();
        PopulateBusinessProcessCatalogue(graph, snapshot);
        return snapshot;
    }

    internal static void PopulateBusinessProcessCatalogue(
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
        BusinessProcessCatalogueEntityMapper mapper =
            new BusinessProcessCatalogueEntityMapper(context);
        XmiPassTwoEntity root = graph.Entities[0];
        snapshot.Identifier = root.Identifier;
        XmiPassTwoReference? catalogueReference =
            context.FindFirstReference(root, "businessProcessCatalogue");
        if (catalogueReference != null)
        {
            snapshot.BusinessProcessCatalogue = context.CopyReference(catalogueReference);
            snapshot.References.Add(context.CopyReference(catalogueReference));
        }

        int entityIndex = 0;
        while (entityIndex < graph.Entities.Count)
        {
            XmiPassTwoEntity entity = graph.Entities[entityIndex];
            if (entity.TopLevelPartitionKind == RepositoryEntityKind.BusinessProcessCatalogue ||
                entity.Kind == RepositoryEntityKind.BusinessProcessCatalogue)
            {
                AddCatalogueReferences(context, entity, snapshot.References);
                AddCatalogueEntity(mapper, entity, snapshot);
            }

            entityIndex++;
        }
    }

    internal static void AddCatalogueEntity(
        BusinessProcessCatalogueEntityMapper mapper,
        XmiPassTwoEntity entity,
        RepositorySnapshot snapshot)
    {
        if (entity.Kind == RepositoryEntityKind.BusinessArea)
        {
            snapshot.BusinessAreas.Add(mapper.BuildBusinessArea(entity));
        }

        if (entity.Kind == RepositoryEntityKind.BusinessProcess)
        {
            snapshot.BusinessProcesses.Add(mapper.BuildBusinessProcess(entity));
        }

        if (entity.Kind == RepositoryEntityKind.BusinessRole)
        {
            snapshot.BusinessRoles.Add(mapper.BuildBusinessRole(entity));
        }

        if (entity.Kind == RepositoryEntityKind.Participant)
        {
            snapshot.Participants.Add(mapper.BuildParticipant(entity));
        }

        if (entity.Kind == RepositoryEntityKind.BusinessTransaction)
        {
            snapshot.BusinessTransactions.Add(mapper.BuildBusinessTransaction(entity));
        }

        if (entity.Kind == RepositoryEntityKind.MessageChoreography)
        {
            snapshot.MessageChoreographies.Add(mapper.BuildMessageChoreography(entity));
        }

        if (entity.TypeName == "MessageTransmission")
        {
            snapshot.MessageTransmissions.Add(mapper.BuildMessageTransmission(entity));
        }

        if (entity.Kind == RepositoryEntityKind.Conversation)
        {
            snapshot.Conversations.Add(mapper.BuildConversation(entity));
        }

        if (entity.Kind == RepositoryEntityKind.Convergence)
        {
            snapshot.ConvergenceDefinitions.Add(mapper.BuildConvergence(entity));
        }

        if (entity.TypeName == "MessagingEndpoint")
        {
            snapshot.MessagingEndpoints.Add(mapper.BuildMessagingEndpoint(entity));
        }

        if (IsTransportType(entity.TypeName))
        {
            snapshot.Transports.Add(mapper.BuildTransport(entity));
        }
    }

    internal static bool IsTransportType(string typeName)
    {
        return typeName == "MessageTransportMode" || typeName == "MessageTransportSystem" ||
            typeName == "Address" || typeName == "BroadcastList" ||
            typeName == "TransportMessage" || typeName == "MessageInstance";
    }

    internal static void AddCatalogueReferences(
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
