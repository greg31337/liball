using System;
using System.Collections.Generic;

namespace iso20022;

internal static class RepositorySnapshotIndexer
{
    internal static RepositorySnapshotIndex BuildIndex(RepositorySnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException("snapshot");
        }

        RepositorySnapshotIndex index = new RepositorySnapshotIndex();
        AddSystemEntries(snapshot, index);
        AppendConstraintEntries(snapshot, index);
        AppendBusinessAreaEntries(snapshot, index);
        AppendBusinessProcessEntries(snapshot, index);
        AppendBusinessRoleEntries(snapshot, index);
        AppendParticipantEntries(snapshot, index);
        AppendBusinessTransactionEntries(snapshot, index);
        AppendConversationEntries(snapshot, index);
        AppendMessageChoreographyEntries(snapshot, index);
        AppendMessageTransmissionEntries(snapshot, index);
        AppendMessagingEndpointEntries(snapshot, index);
        AppendTransportEntries(snapshot, index);
        AppendBusinessComponentEntries(snapshot, index);
        AppendBusinessElementEntries(snapshot, index);
        AppendMessageSetEntries(snapshot, index);
        AppendMessageDefinitionEntries(snapshot, index);
        AppendMessageBuildingBlockEntries(snapshot, index);
        AppendMessageComponentEntries(snapshot, index);
        AppendMessageElementEntries(snapshot, index);
        AppendChoiceEntries(snapshot, index);
        AppendDataTypeEntries(snapshot, index);
        AppendCodeSetEntries(snapshot, index);
        AppendSyntaxEntries(snapshot, index);
        AppendEncodingEntries(snapshot, index);
        AppendExternalSchemaEntries(snapshot, index);
        AppendSyntaxMessageSchemeEntries(snapshot, index);
        AppendConvergenceEntries(snapshot, index);
        AppendUnknownEntries(snapshot, index);
        return index;
    }

    internal static void AddSystemEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        RepositoryIndexEntry repository = new RepositoryIndexEntry();
        repository.Identifier = snapshot.Identifier;
        repository.Name = snapshot.Edition;
        repository.Kind = RepositoryEntityKind.Repository;
        repository.Value = snapshot;
        AddEntry(index, repository);
        if (snapshot.DataDictionary.TargetIdentifier.Length > 0)
        {
            RepositoryIndexEntry dictionary = new RepositoryIndexEntry();
            dictionary.Identifier = snapshot.DataDictionary.TargetIdentifier;
            dictionary.Kind = RepositoryEntityKind.DataDictionary;
            dictionary.Location = snapshot.DataDictionary.Location;
            dictionary.Value = snapshot.DataDictionary;
            AddEntry(index, dictionary);
        }

        if (snapshot.BusinessProcessCatalogue.TargetIdentifier.Length > 0)
        {
            RepositoryIndexEntry catalogue = new RepositoryIndexEntry();
            catalogue.Identifier = snapshot.BusinessProcessCatalogue.TargetIdentifier;
            catalogue.Kind = RepositoryEntityKind.BusinessProcessCatalogue;
            catalogue.Location = snapshot.BusinessProcessCatalogue.Location;
            catalogue.Value = snapshot.BusinessProcessCatalogue;
            AddEntry(index, catalogue);
        }
    }

    internal static void AppendConstraintEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Constraints.Count)
        {
            ConstraintDefinition item = snapshot.Constraints[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Constraint,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendBusinessAreaEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.BusinessAreas.Count)
        {
            BusinessAreaDefinition item = snapshot.BusinessAreas[itemIndex];
            RepositoryIndexEntry entry = CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.BusinessArea,
                item.Registration,
                item.Location,
                item);
            entry.BusinessAreaCode = item.Code;
            AddEntry(index, entry);
            itemIndex++;
        }
    }

    internal static void AppendBusinessProcessEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.BusinessProcesses.Count)
        {
            BusinessProcessDefinition item = snapshot.BusinessProcesses[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.BusinessProcess,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendBusinessRoleEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.BusinessRoles.Count)
        {
            BusinessRoleDefinition item = snapshot.BusinessRoles[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.BusinessRole,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendParticipantEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Participants.Count)
        {
            ParticipantDefinition item = snapshot.Participants[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Participant,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendBusinessTransactionEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.BusinessTransactions.Count)
        {
            BusinessTransactionDefinition item = snapshot.BusinessTransactions[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.BusinessTransaction,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendConversationEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Conversations.Count)
        {
            ConversationDefinition item = snapshot.Conversations[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Conversation,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendMessageChoreographyEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageChoreographies.Count)
        {
            MessageChoreographyDefinition item = snapshot.MessageChoreographies[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessageChoreography,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendMessageTransmissionEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageTransmissions.Count)
        {
            MessageTransmissionDefinition item = snapshot.MessageTransmissions[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessageTransmission,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendMessagingEndpointEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessagingEndpoints.Count)
        {
            MessagingEndpointDefinition item = snapshot.MessagingEndpoints[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessagingEndpoint,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendTransportEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Transports.Count)
        {
            TransportDefinition item = snapshot.Transports[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                item.Kind,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendBusinessComponentEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.BusinessComponents.Count)
        {
            BusinessComponentDefinition item = snapshot.BusinessComponents[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.BusinessComponent,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendBusinessElementEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.BusinessElements.Count)
        {
            BusinessElementDefinition item = snapshot.BusinessElements[itemIndex];
            RepositoryEntityKind kind = RepositoryEntityKind.BusinessElement;
            if (item.Kind == MessageElementKind.Attribute)
            {
                kind = RepositoryEntityKind.BusinessAttribute;
            }

            if (item.Kind == MessageElementKind.AssociationEnd)
            {
                kind = RepositoryEntityKind.BusinessAssociationEnd;
            }

            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                kind,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendMessageSetEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageSets.Count)
        {
            MessageSetDefinition item = snapshot.MessageSets[itemIndex];
            RepositoryIndexEntry entry = CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessageSet,
                item.Registration,
                item.Location,
                item);
            entry.MessageSetIdentifiers.Add(item.Identifier);
            AddEntry(index, entry);
            itemIndex++;
        }
    }

    internal static void AppendMessageDefinitionEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageDefinitions.Count)
        {
            MessageDefinitionMetadata item = snapshot.MessageDefinitions[itemIndex];
            RepositoryIndexEntry entry = CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessageDefinition,
                item.Registration,
                item.Location,
                item);
            entry.BusinessAreaCode = item.MessageIdentifier.BusinessArea;
            entry.MessageIdentifier = item.MessageIdentifier.Value;
            entry.MessageFunctionality = item.MessageIdentifier.MessageFunctionality;
            entry.Variant = item.MessageIdentifier.Variant;
            entry.NamespaceUri = item.NamespaceUri;
            entry.NamespaceUris.Add(item.NamespaceUri);
            entry.Version = item.MessageIdentifier.Version;
            AppendReferenceIdentifiers(item.MessageSets, entry.MessageSetIdentifiers);
            AddEntry(index, entry);
            itemIndex++;
        }
    }

    internal static void AppendMessageBuildingBlockEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageBuildingBlocks.Count)
        {
            MessageBuildingBlockDefinition item = snapshot.MessageBuildingBlocks[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessageBuildingBlock,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendMessageComponentEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageComponents.Count)
        {
            MessageComponentDefinition item = snapshot.MessageComponents[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.MessageComponent,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendMessageElementEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.MessageElements.Count)
        {
            MessageElementDefinition item = snapshot.MessageElements[itemIndex];
            RepositoryEntityKind kind = RepositoryEntityKind.MessageElement;
            if (item.Kind == MessageElementKind.Attribute)
            {
                kind = RepositoryEntityKind.MessageAttribute;
            }

            if (item.Kind == MessageElementKind.AssociationEnd)
            {
                kind = RepositoryEntityKind.MessageAssociationEnd;
            }

            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                kind,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendChoiceEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Choices.Count)
        {
            ChoiceDefinition item = snapshot.Choices[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Choice,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendDataTypeEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.DataTypes.Count)
        {
            DataTypeDefinition item = snapshot.DataTypes[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.DataType,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendCodeSetEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.CodeSets.Count)
        {
            CodeSetDefinition item = snapshot.CodeSets[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.CodeSet,
                item.Registration,
                item.Location,
                item));
            int codeIndex = 0;
            while (codeIndex < item.Codes.Count)
            {
                CodeValueDefinition code = item.Codes[codeIndex];
                AddEntry(index, CreateConceptEntry(
                    code.Identifier,
                    code.Name,
                    RepositoryEntityKind.CodeValue,
                    code.Registration,
                    code.Location,
                    code));
                codeIndex++;
            }

            itemIndex++;
        }
    }

    internal static void AppendSyntaxEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Syntaxes.Count)
        {
            SyntaxDefinition item = snapshot.Syntaxes[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Syntax,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendEncodingEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Encodings.Count)
        {
            EncodingDefinition item = snapshot.Encodings[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Encoding,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendExternalSchemaEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.ExternalSchemas.Count)
        {
            ExternalSchemaDefinition item = snapshot.ExternalSchemas[itemIndex];
            RepositoryIndexEntry entry = CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.ExternalSchema,
                item.Registration,
                item.Location,
                item);
            int namespaceIndex = 0;
            while (namespaceIndex < item.NamespaceUris.Count)
            {
                entry.NamespaceUris.Add(item.NamespaceUris[namespaceIndex]);
                namespaceIndex++;
            }

            AddEntry(index, entry);
            itemIndex++;
        }
    }

    internal static void AppendSyntaxMessageSchemeEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.SyntaxMessageSchemes.Count)
        {
            SyntaxMessageSchemeDefinition item = snapshot.SyntaxMessageSchemes[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.SyntaxMessageScheme,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendConvergenceEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.ConvergenceDefinitions.Count)
        {
            ConvergenceDefinition item = snapshot.ConvergenceDefinitions[itemIndex];
            AddEntry(index, CreateConceptEntry(
                item.Identifier,
                item.Name,
                RepositoryEntityKind.Convergence,
                item.Registration,
                item.Location,
                item));
            itemIndex++;
        }
    }

    internal static void AppendUnknownEntries(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.UnknownContent.Count)
        {
            AppendUnknownEntryTree(snapshot.UnknownContent[itemIndex], index);
            itemIndex++;
        }
    }

    internal static void AppendUnknownEntryTree(
        RepositoryUnknownData unknown,
        RepositorySnapshotIndex index)
    {
        if (!unknown.IsAttribute &&
            (unknown.Identifier.Length > 0 || unknown.TypeName.Length > 0))
        {
            RepositoryIndexEntry entry = new RepositoryIndexEntry();
            entry.Identifier = unknown.Identifier;
            entry.Name = unknown.Name.LocalName;
            entry.Kind = RepositoryEntityKind.Unknown;
            entry.Location = unknown.Location;
            entry.Value = unknown;
            AddEntry(index, entry);
        }

        int childIndex = 0;
        while (childIndex < unknown.Children.Count)
        {
            AppendUnknownEntryTree(unknown.Children[childIndex], index);
            childIndex++;
        }
    }

    internal static RepositoryIndexEntry CreateConceptEntry(
        string identifier,
        string name,
        RepositoryEntityKind kind,
        RegistrationMetadata registration,
        SourceLocation location,
        object value)
    {
        RepositoryIndexEntry entry = new RepositoryIndexEntry();
        entry.Identifier = identifier;
        entry.Name = name;
        entry.Kind = kind;
        entry.HasRegistrationStatus = true;
        entry.RegistrationStatus = registration.Status;
        entry.RegistrationStatusValue = registration.StatusValue;
        entry.Location = location;
        entry.Value = value;
        return entry;
    }

    internal static void AppendReferenceIdentifiers(
        List<RepositoryReference> references,
        List<string> destination)
    {
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            destination.Add(references[referenceIndex].TargetIdentifier);
            referenceIndex++;
        }
    }

    internal static void AddEntry(
        RepositorySnapshotIndex index,
        RepositoryIndexEntry entry)
    {
        entry.Order = index.Entries.Count;
        index.Entries.Add(entry);
        AddStringKey(index.EntriesByIdentifier, entry.Identifier, entry);
        AddStringKey(index.EntriesByName, entry.Name, entry);
        AddKindKey(index.EntriesByKind, entry.Kind, entry);
        if (entry.HasRegistrationStatus)
        {
            AddStatusKey(index.EntriesByStatus, entry.RegistrationStatus, entry);
        }

        AddStringKey(index.EntriesByBusinessArea, entry.BusinessAreaCode, entry);
        int messageSetIndex = 0;
        while (messageSetIndex < entry.MessageSetIdentifiers.Count)
        {
            AddStringKey(
                index.EntriesByMessageSet,
                entry.MessageSetIdentifiers[messageSetIndex],
                entry);
            messageSetIndex++;
        }

        AddStringKey(index.EntriesByMessageIdentifier, entry.MessageIdentifier, entry);
        AddStringKey(
            index.EntriesByMessageFunctionality,
            entry.MessageFunctionality,
            entry);
        AddStringKey(index.EntriesByVariant, entry.Variant, entry);
        AddStringKey(index.EntriesByNamespace, entry.NamespaceUri, entry);
        int namespaceIndex = 0;
        while (namespaceIndex < entry.NamespaceUris.Count)
        {
            string namespaceUri = entry.NamespaceUris[namespaceIndex];
            if (namespaceUri != entry.NamespaceUri)
            {
                AddStringKey(index.EntriesByNamespace, namespaceUri, entry);
            }

            namespaceIndex++;
        }

        AddStringKey(index.EntriesByVersion, entry.Version, entry);
    }

    internal static void AddStringKey(
        Dictionary<string, List<RepositoryIndexEntry>> destination,
        string key,
        RepositoryIndexEntry entry)
    {
        if (key.Length == 0)
        {
            return;
        }

        if (!destination.ContainsKey(key))
        {
            destination.Add(key, new List<RepositoryIndexEntry>());
        }

        List<RepositoryIndexEntry> entries = destination[key];
        if (entries.Count == 0 || entries[entries.Count - 1] != entry)
        {
            entries.Add(entry);
        }
    }

    internal static void AddKindKey(
        Dictionary<RepositoryEntityKind, List<RepositoryIndexEntry>> destination,
        RepositoryEntityKind key,
        RepositoryIndexEntry entry)
    {
        if (!destination.ContainsKey(key))
        {
            destination.Add(key, new List<RepositoryIndexEntry>());
        }

        destination[key].Add(entry);
    }

    internal static void AddStatusKey(
        Dictionary<RegistrationStatus, List<RepositoryIndexEntry>> destination,
        RegistrationStatus key,
        RepositoryIndexEntry entry)
    {
        if (!destination.ContainsKey(key))
        {
            destination.Add(key, new List<RepositoryIndexEntry>());
        }

        destination[key].Add(entry);
    }
}
