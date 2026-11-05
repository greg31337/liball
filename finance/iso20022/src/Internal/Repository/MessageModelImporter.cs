using System;
using System.Collections.Generic;

namespace iso20022;

internal static class MessageModelImporter
{
    internal static RepositorySnapshot ImportMessageModel(XmiPassTwoResult graph)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot();
        PopulateMessageModel(graph, snapshot);
        return snapshot;
    }

    internal static void PopulateMessageModel(
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
        MessageModelEntityMapper mapper = new MessageModelEntityMapper(context);
        XmiPassTwoEntity root = graph.Entities[0];
        snapshot.Identifier = root.Identifier;
        CopyPartitionReferences(context, root, snapshot);
        int entityIndex = 0;
        while (entityIndex < graph.Entities.Count)
        {
            XmiPassTwoEntity entity = graph.Entities[entityIndex];
            if (IsMessageModelEntity(entity))
            {
                AddEntityReferences(context, entity, snapshot.References);
                AddMessageModelEntity(mapper, entity, snapshot);
            }

            entityIndex++;
        }
    }

    internal static void CopyPartitionReferences(
        DataDictionaryImportContext context,
        XmiPassTwoEntity root,
        RepositorySnapshot snapshot)
    {
        XmiPassTwoReference? dictionaryReference =
            context.FindFirstReference(root, "dataDictionary");
        if (dictionaryReference != null)
        {
            snapshot.DataDictionary = context.CopyReference(dictionaryReference);
            snapshot.References.Add(context.CopyReference(dictionaryReference));
        }

        XmiPassTwoReference? catalogueReference =
            context.FindFirstReference(root, "businessProcessCatalogue");
        if (catalogueReference != null)
        {
            snapshot.BusinessProcessCatalogue = context.CopyReference(catalogueReference);
            snapshot.References.Add(context.CopyReference(catalogueReference));
        }
    }

    internal static bool IsMessageModelEntity(XmiPassTwoEntity entity)
    {
        return entity.TypeName == "MessageSet" ||
            entity.TypeName == "IndustryMessageSet" ||
            entity.TypeName == "ISO15022MessageSet" ||
            entity.TypeName == "MessageDefinition" ||
            entity.TypeName == "MessageDefinitionIdentifier" ||
            entity.TypeName == "MessageBuildingBlock" ||
            entity.TypeName == "MessageComponent" ||
            entity.TypeName == "MessageAttribute" ||
            entity.TypeName == "MessageAssociationEnd" ||
            entity.TypeName == "ChoiceComponent" ||
            entity.TypeName == "Xor" ||
            entity.TypeName == "Syntax" ||
            entity.TypeName == "Encoding" ||
            entity.TypeName == "SyntaxMessageScheme";
    }

    internal static void AddMessageModelEntity(
        MessageModelEntityMapper mapper,
        XmiPassTwoEntity entity,
        RepositorySnapshot snapshot)
    {
        if (entity.TypeName == "MessageSet" ||
            entity.TypeName == "IndustryMessageSet" ||
            entity.TypeName == "ISO15022MessageSet")
        {
            snapshot.MessageSets.Add(mapper.BuildMessageSet(entity));
        }

        if (entity.TypeName == "MessageDefinition")
        {
            snapshot.MessageDefinitions.Add(mapper.BuildMessageDefinition(entity));
        }

        if (entity.TypeName == "MessageBuildingBlock")
        {
            snapshot.MessageBuildingBlocks.Add(mapper.BuildMessageBuildingBlock(entity));
        }

        if (entity.TypeName == "MessageComponent")
        {
            snapshot.MessageComponents.Add(mapper.BuildMessageComponent(entity));
        }

        if (entity.TypeName == "MessageAttribute" ||
            entity.TypeName == "MessageAssociationEnd")
        {
            snapshot.MessageElements.Add(mapper.BuildMessageElement(entity));
        }

        if (entity.TypeName == "ChoiceComponent" || entity.TypeName == "Xor")
        {
            snapshot.Choices.Add(mapper.BuildChoice(entity));
        }

        if (entity.TypeName == "Syntax")
        {
            snapshot.Syntaxes.Add(mapper.BuildSyntax(entity));
        }

        if (entity.TypeName == "Encoding")
        {
            snapshot.Encodings.Add(mapper.BuildEncoding(entity));
        }

        if (entity.TypeName == "SyntaxMessageScheme")
        {
            snapshot.SyntaxMessageSchemes.Add(mapper.BuildSyntaxMessageScheme(entity));
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
