using System;
using System.Collections.Generic;

namespace iso20022;

internal class BusinessProcessCatalogueEntityMapper
{
    DataDictionaryImportContext context;
    DataDictionaryEntityMapper common;

    internal BusinessProcessCatalogueEntityMapper(DataDictionaryImportContext context)
    {
        this.context = context;
        common = new DataDictionaryEntityMapper(context);
    }

    internal BusinessAreaDefinition BuildBusinessArea(XmiPassTwoEntity entity)
    {
        BusinessAreaDefinition definition = new BusinessAreaDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Code = common.FindAttributeValue(entity, "code");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        common.AppendDirectReferences(entity, "messageDefinition", definition.MessageDefinitions);
        definition.Location = entity.Location;
        return definition;
    }

    internal BusinessProcessDefinition BuildBusinessProcess(XmiPassTwoEntity entity)
    {
        BusinessProcessDefinition definition = new BusinessProcessDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        common.AppendDirectReferences(entity, "extender", definition.Extenders);
        common.AppendDirectReferences(entity, "extended", definition.ExtendedProcesses);
        common.AppendDirectReferences(entity, "included", definition.IncludedProcesses);
        common.AppendDirectReferences(entity, "includer", definition.Includers);
        common.AppendDirectReferences(entity, "businessRole", definition.BusinessRoles);
        common.AppendDirectReferences(
            entity,
            "businessProcessTrace",
            definition.BusinessTransactions);
        definition.Location = entity.Location;
        return definition;
    }

    internal BusinessRoleDefinition BuildBusinessRole(XmiPassTwoEntity entity)
    {
        BusinessRoleDefinition definition = new BusinessRoleDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        RepositoryReference? processReference = common.CopyFirstReference(entity, "businessProcess");
        if (processReference != null)
        {
            definition.BusinessProcess = processReference;
        }
        else
        {
            XmiPassTwoEntity? parent = context.FindParent(entity);
            if (parent != null)
            {
                definition.BusinessProcess =
                    context.CreateParentReference(entity, parent, "businessProcess");
            }
        }

        common.AppendDirectReferences(entity, "businessRoleTrace", definition.Participants);
        definition.Location = entity.Location;
        return definition;
    }

    internal ParticipantDefinition BuildParticipant(XmiPassTwoEntity entity)
    {
        ParticipantDefinition definition = new ParticipantDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent != null)
        {
            definition.BusinessTransaction =
                context.CreateParentReference(entity, parent, "businessTransaction");
        }

        RepositoryReference? roleReference = common.CopyFirstReference(entity, "businessRoleTrace");
        if (roleReference != null)
        {
            definition.BusinessRole = roleReference;
        }

        MultiplicityDefinition multiplicity = common.BuildMultiplicity(entity);
        definition.MinimumOccurs = multiplicity.MinimumOccurs;
        definition.HasMaximumOccurs = multiplicity.HasMaximumOccurs;
        definition.MaximumOccurs = multiplicity.MaximumOccurs;
        AppendNormalizedTransmissionReferences(entity, "sends", definition.Sends);
        AppendNormalizedTransmissionReferences(entity, "receives", definition.Receives);
        definition.Location = entity.Location;
        return definition;
    }

    internal BusinessTransactionDefinition BuildBusinessTransaction(XmiPassTwoEntity entity)
    {
        BusinessTransactionDefinition definition = new BusinessTransactionDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        RepositoryReference? processReference =
            common.CopyFirstReference(entity, "businessProcessTrace");
        if (processReference != null)
        {
            definition.BusinessProcess = processReference;
        }

        common.AppendDirectReferences(entity, "participant", definition.Participants);
        common.AppendDirectReferences(entity, "transmission", definition.Transmissions);
        RepositoryReference? transportMode =
            common.CopyFirstReference(entity, "messageTransportMode");
        if (transportMode != null)
        {
            definition.TransportMode = transportMode;
        }

        definition.ParentTransaction = common.CopyFirstReference(entity, "parentTransaction");
        common.AppendDirectReferences(entity, "subTransaction", definition.SubTransactions);
        common.AppendDirectReferences(entity, "trace", definition.Choreographies);
        definition.Location = entity.Location;
        return definition;
    }

    internal MessageChoreographyDefinition BuildMessageChoreography(XmiPassTwoEntity entity)
    {
        MessageChoreographyDefinition definition = new MessageChoreographyDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        XmiPassTwoReference? transactionSource =
            context.FindFirstReference(entity, "businessTransactionTrace");
        if (transactionSource != null)
        {
            definition.BusinessTransaction = context.CopyReference(transactionSource);
            AppendChoreographyTransmissions(
                entity,
                transactionSource,
                definition.Transmissions);
        }

        common.AppendDirectReferences(entity, "messageDefinition", definition.MessageDefinitions);
        definition.Location = entity.Location;
        return definition;
    }

    internal void AppendChoreographyTransmissions(
        XmiPassTwoEntity choreography,
        XmiPassTwoReference transactionReference,
        List<RepositoryReference> destination)
    {
        if (transactionReference.TargetDocumentOrder < 0 ||
            transactionReference.TargetDocumentOrder >= context.Graph.Entities.Count)
        {
            return;
        }

        XmiPassTwoEntity transaction =
            context.Graph.Entities[transactionReference.TargetDocumentOrder];
        List<XmiPassTwoReference> references = context.GetSourceReferences(transaction);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            XmiPassTwoReference reference = references[referenceIndex];
            if (reference.PropertyName == "transmission")
            {
                destination.Add(CreateNormalizedReference(
                    choreography,
                    reference,
                    "transmission",
                    destination.Count));
            }

            referenceIndex++;
        }
    }

    internal MessageTransmissionDefinition BuildMessageTransmission(XmiPassTwoEntity entity)
    {
        MessageTransmissionDefinition definition = new MessageTransmissionDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.MessageTypeDescription =
            common.FindAttributeValue(entity, "messageTypeDescription");
        definition.Registration = common.BuildRegistration(entity);
        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent != null)
        {
            definition.BusinessTransaction =
                context.CreateParentReference(entity, parent, "businessTransaction");
        }

        common.AppendDirectReferences(entity, "derivation", definition.DerivedMessageDefinitions);
        List<XmiPassTwoEntity> children = context.GetChildren(entity);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            XmiPassTwoEntity child = children[childIndex];
            if (child.TypeName == "Send")
            {
                XmiPassTwoReference? sender = context.FindFirstReference(child, "sender");
                if (sender != null)
                {
                    definition.Sender = CreateNormalizedReference(
                        entity,
                        sender,
                        "sender",
                        definition.Sender.Position);
                }
            }

            if (child.TypeName == "Receive")
            {
                XmiPassTwoReference? receiver = context.FindFirstReference(child, "receiver");
                if (receiver != null)
                {
                    definition.Receivers.Add(CreateNormalizedReference(
                        entity,
                        receiver,
                        "receiver",
                        definition.Receivers.Count));
                }
            }

            childIndex++;
        }

        definition.Location = entity.Location;
        return definition;
    }

    internal ConversationDefinition BuildConversation(XmiPassTwoEntity entity)
    {
        ConversationDefinition definition = new ConversationDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.Location = entity.Location;
        return definition;
    }

    internal ConvergenceDefinition BuildConvergence(XmiPassTwoEntity entity)
    {
        ConvergenceDefinition definition = new ConvergenceDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        int markupIndex = 0;
        while (markupIndex < definition.Registration.SemanticMarkup.Count)
        {
            definition.SemanticMarkup.Add(
                definition.Registration.SemanticMarkup[markupIndex]);
            markupIndex++;
        }

        definition.Location = entity.Location;
        return definition;
    }

    internal MessagingEndpointDefinition BuildMessagingEndpoint(XmiPassTwoEntity entity)
    {
        MessagingEndpointDefinition definition = new MessagingEndpointDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        RepositoryReference? transportSystem =
            common.CopyFirstReference(entity, "transportSystem");
        if (transportSystem != null)
        {
            definition.TransportSystem = transportSystem;
        }
        else
        {
            XmiPassTwoEntity? parent = context.FindParent(entity);
            if (parent != null)
            {
                definition.TransportSystem =
                    context.CreateParentReference(entity, parent, "transportSystem");
            }
        }

        common.AppendDirectReferences(entity, "location", definition.Addresses);
        common.AppendDirectReferences(entity, "sentMessage", definition.SentMessages);
        common.AppendDirectReferences(entity, "receivedMessage", definition.ReceivedMessages);
        AppendEndpointBroadcastLists(entity, definition.BroadcastLists);
        definition.Location = entity.Location;
        return definition;
    }

    internal TransportDefinition BuildTransport(XmiPassTwoEntity entity)
    {
        TransportDefinition definition = new TransportDefinition();
        definition.Identifier = entity.Identifier;
        definition.Kind = entity.Kind;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        if (entity.TypeName == "MessageTransportMode")
        {
            definition.BusinessProcessCatalogue =
                context.CreatePartitionReference(entity, "businessProcessCatalogue");
        }

        definition.BoundedCommunicationDelay = DurationLexical.ParseDuration(
            common.FindAttributeValue(entity, "boundedCommunicationDelay").AsSpan());
        definition.MaximumClockVariation = DurationLexical.ParseDuration(
            common.FindAttributeValue(entity, "maximumClockVariation").AsSpan());
        definition.MaximumMessageSizeKilobytes =
            common.ReadIntegerAttribute(entity, "maximumMessageSize");
        definition.MessageDeliveryWindow = DurationLexical.ParseDuration(
            common.FindAttributeValue(entity, "messageDeliveryWindow").AsSpan());
        definition.MessageSendingWindow = DurationLexical.ParseDuration(
            common.FindAttributeValue(entity, "messageSendingWindow").AsSpan());
        definition.DeliveryAssurance = common.FindAttributeValue(entity, "deliveryAssurance");
        definition.Durability = common.FindAttributeValue(entity, "durability");
        definition.MessageCasting = common.FindAttributeValue(entity, "messageCasting");
        definition.MessageDeliveryOrder =
            common.FindAttributeValue(entity, "messageDeliveryOrder");
        definition.MessageValidationLevel =
            common.FindAttributeValue(entity, "messageValidationLevel");
        definition.MessageValidationOnOff =
            common.FindAttributeValue(entity, "messageValidationOnOff");
        definition.MessageValidationResults =
            common.FindAttributeValue(entity, "messageValidationResults");
        definition.ReceiverAsynchronicity =
            common.FindAttributeValue(entity, "receiverAsynchronicity");
        definition.SenderAsynchronicity =
            common.FindAttributeValue(entity, "senderAsynchronicity");
        common.AppendDirectReferences(
            entity,
            "businessTransaction",
            definition.BusinessTransactions);
        common.AppendDirectReferences(entity, "endpoint", definition.Endpoints);
        common.AppendDirectReferences(entity, "location", definition.Addresses);
        common.AppendDirectReferences(entity, "address", definition.Addresses);
        common.AppendDirectReferences(entity, "broadCastList", definition.BroadcastLists);
        common.AppendDirectReferences(entity, "transportMessage", definition.TransportMessages);
        common.AppendDirectReferences(entity, "messageInstance", definition.TransportMessages);
        common.AppendDirectReferences(entity, "sender", definition.Endpoints);
        common.AppendDirectReferences(entity, "receiver", definition.Endpoints);
        definition.Location = entity.Location;
        return definition;
    }

    internal void AppendNormalizedTransmissionReferences(
        XmiPassTwoEntity participant,
        string propertyName,
        List<RepositoryReference> destination)
    {
        List<XmiPassTwoReference> references = context.GetSourceReferences(participant);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            XmiPassTwoReference reference = references[referenceIndex];
            if (reference.PropertyName == propertyName &&
                reference.TargetDocumentOrder >= 0 &&
                reference.TargetDocumentOrder < context.Graph.Entities.Count)
            {
                XmiPassTwoEntity helper = context.Graph.Entities[reference.TargetDocumentOrder];
                XmiPassTwoEntity? transmission = context.FindParent(helper);
                if (transmission != null && transmission.TypeName == "MessageTransmission")
                {
                    destination.Add(CreateNormalizedReferenceToEntity(
                        participant,
                        reference,
                        propertyName,
                        destination.Count,
                        transmission));
                }
            }

            referenceIndex++;
        }
    }

    internal void AppendEndpointBroadcastLists(
        XmiPassTwoEntity endpoint,
        List<RepositoryReference> destination)
    {
        List<XmiPassTwoReference> locationReferences = context.GetSourceReferences(endpoint);
        int locationIndex = 0;
        while (locationIndex < locationReferences.Count)
        {
            XmiPassTwoReference location = locationReferences[locationIndex];
            if (location.PropertyName == "location" &&
                location.TargetDocumentOrder >= 0 &&
                location.TargetDocumentOrder < context.Graph.Entities.Count)
            {
                XmiPassTwoEntity address = context.Graph.Entities[location.TargetDocumentOrder];
                List<XmiPassTwoReference> addressReferences = context.GetSourceReferences(address);
                int addressIndex = 0;
                while (addressIndex < addressReferences.Count)
                {
                    XmiPassTwoReference broadcast = addressReferences[addressIndex];
                    if (broadcast.PropertyName == "broadCastList")
                    {
                        destination.Add(CreateNormalizedReference(
                            endpoint,
                            broadcast,
                            "broadcastList",
                            destination.Count));
                    }

                    addressIndex++;
                }
            }

            locationIndex++;
        }
    }

    internal RepositoryReference CreateNormalizedReference(
        XmiPassTwoEntity source,
        XmiPassTwoReference original,
        string propertyName,
        int position)
    {
        RepositoryReference reference = context.CopyReference(original);
        reference.SourceIdentifier = source.Identifier;
        reference.SourceKind = source.Kind;
        reference.PropertyName = propertyName;
        reference.Position = position;
        return reference;
    }

    internal RepositoryReference CreateNormalizedReferenceToEntity(
        XmiPassTwoEntity source,
        XmiPassTwoReference original,
        string propertyName,
        int position,
        XmiPassTwoEntity target)
    {
        RepositoryReference reference = CreateNormalizedReference(
            source,
            original,
            propertyName,
            position);
        reference.TargetIdentifier = target.Identifier;
        reference.TargetKind = target.Kind;
        reference.IsResolved = target.Identifier.Length > 0;
        reference.ErrorCode = string.Empty;
        reference.ErrorMessage = string.Empty;
        return reference;
    }
}
