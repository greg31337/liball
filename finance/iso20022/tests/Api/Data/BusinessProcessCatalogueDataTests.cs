using iso20022;

namespace iso20022.Tests;

public class BusinessProcessCatalogueDataTests
{
    [Test]
    public void SnapshotRetainsEveryCataloguePartitionInCallerOrder()
    {
        BusinessAreaDefinition area = new BusinessAreaDefinition { Identifier = "_area", Name = "Payments", Code = "pain" };
        BusinessProcessDefinition process = new BusinessProcessDefinition { Identifier = "_process", Name = "Payment Initiation" };
        BusinessRoleDefinition role = new BusinessRoleDefinition { Identifier = "_role", Name = "Debtor" };
        ParticipantDefinition participant = new ParticipantDefinition { Identifier = "_participant", Name = "Initiator" };
        BusinessTransactionDefinition transaction = new BusinessTransactionDefinition { Identifier = "_transaction", Name = "Credit Transfer" };
        ConversationDefinition conversation = new ConversationDefinition { Identifier = "_conversation", Name = "Transfer Conversation" };
        MessageChoreographyDefinition choreography = new MessageChoreographyDefinition { Identifier = "_choreography", Name = "Transfer Choreography" };
        MessageTransmissionDefinition transmission = new MessageTransmissionDefinition { Identifier = "_transmission", Name = "Initiation" };
        MessagingEndpointDefinition endpoint = new MessagingEndpointDefinition { Identifier = "_endpoint", Name = "Debtor Agent" };
        TransportDefinition transport = new TransportDefinition { Identifier = "_transport", Name = "Reliable Transport" };
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.BusinessAreas.Add(area);
        snapshot.BusinessProcesses.Add(process);
        snapshot.BusinessRoles.Add(role);
        snapshot.Participants.Add(participant);
        snapshot.BusinessTransactions.Add(transaction);
        snapshot.Conversations.Add(conversation);
        snapshot.MessageChoreographies.Add(choreography);
        snapshot.MessageTransmissions.Add(transmission);
        snapshot.MessagingEndpoints.Add(endpoint);
        snapshot.Transports.Add(transport);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.BusinessAreas.Single(), Is.SameAs(area));
            Assert.That(snapshot.BusinessProcesses.Single(), Is.SameAs(process));
            Assert.That(snapshot.BusinessRoles.Single(), Is.SameAs(role));
            Assert.That(snapshot.Participants.Single(), Is.SameAs(participant));
            Assert.That(snapshot.BusinessTransactions.Single(), Is.SameAs(transaction));
            Assert.That(snapshot.Conversations.Single(), Is.SameAs(conversation));
            Assert.That(snapshot.MessageChoreographies.Single(), Is.SameAs(choreography));
            Assert.That(snapshot.MessageTransmissions.Single(), Is.SameAs(transmission));
            Assert.That(snapshot.MessagingEndpoints.Single(), Is.SameAs(endpoint));
            Assert.That(snapshot.Transports.Single(), Is.SameAs(transport));
        });
    }

    [Test]
    public void BusinessAreaProcessAndRoleRetainTypedCatalogueRelationships()
    {
        RepositoryReference catalogue = Reference("_area", RepositoryEntityKind.BusinessArea,
            "businessProcessCatalogue", "_catalogue", RepositoryEntityKind.BusinessProcessCatalogue, 0);
        BusinessAreaDefinition area = new BusinessAreaDefinition
        {
            Identifier = "_area",
            Name = "Securities",
            Definition = "Securities business activities.",
            Code = "sese",
            Registration = new RegistrationMetadata { Status = RegistrationStatus.Registered },
            BusinessProcessCatalogue = catalogue,
            Location = new SourceLocation { HasLineInformation = true, LineNumber = 101 }
        };
        area.MessageDefinitions.Add(Reference("_area", RepositoryEntityKind.BusinessArea,
            "messageDefinition", "_message", RepositoryEntityKind.MessageDefinition, 0));

        BusinessProcessDefinition process = new BusinessProcessDefinition
        {
            Identifier = "_process",
            Name = "Settlement",
            BusinessProcessCatalogue = catalogue
        };
        process.Extenders.Add(Reference("_process", RepositoryEntityKind.BusinessProcess,
            "extender", "_extension", RepositoryEntityKind.BusinessProcess, 0));
        process.ExtendedProcesses.Add(Reference("_process", RepositoryEntityKind.BusinessProcess,
            "extended", "_base", RepositoryEntityKind.BusinessProcess, 0));
        process.IncludedProcesses.Add(Reference("_process", RepositoryEntityKind.BusinessProcess,
            "included", "_included", RepositoryEntityKind.BusinessProcess, 0));
        process.Includers.Add(Reference("_process", RepositoryEntityKind.BusinessProcess,
            "includer", "_includer", RepositoryEntityKind.BusinessProcess, 0));
        process.BusinessRoles.Add(Reference("_process", RepositoryEntityKind.BusinessProcess,
            "businessRole", "_role", RepositoryEntityKind.BusinessRole, 0));
        process.BusinessTransactions.Add(Reference("_process", RepositoryEntityKind.BusinessProcess,
            "businessProcessTrace", "_transaction", RepositoryEntityKind.BusinessTransaction, 0));

        BusinessRoleDefinition role = new BusinessRoleDefinition
        {
            Identifier = "_role",
            Name = "Delivering Party",
            BusinessProcess = Reference("_role", RepositoryEntityKind.BusinessRole,
                "businessProcess", "_process", RepositoryEntityKind.BusinessProcess, 0)
        };
        role.Participants.Add(Reference("_role", RepositoryEntityKind.BusinessRole,
            "businessRoleTrace", "_participant", RepositoryEntityKind.Participant, 0));

        Assert.Multiple(() =>
        {
            Assert.That(area.Code, Is.EqualTo("sese"));
            Assert.That(area.Registration.Status, Is.EqualTo(RegistrationStatus.Registered));
            Assert.That(area.MessageDefinitions.Single().TargetIdentifier, Is.EqualTo("_message"));
            Assert.That(area.Location.LineNumber, Is.EqualTo(101));
            Assert.That(process.Extenders.Single().TargetIdentifier, Is.EqualTo("_extension"));
            Assert.That(process.ExtendedProcesses.Single().TargetIdentifier, Is.EqualTo("_base"));
            Assert.That(process.IncludedProcesses.Single().TargetIdentifier, Is.EqualTo("_included"));
            Assert.That(process.Includers.Single().TargetIdentifier, Is.EqualTo("_includer"));
            Assert.That(process.BusinessRoles.Single().TargetKind, Is.EqualTo(RepositoryEntityKind.BusinessRole));
            Assert.That(process.BusinessTransactions.Single().TargetIdentifier, Is.EqualTo("_transaction"));
            Assert.That(role.BusinessProcess.TargetIdentifier, Is.EqualTo("_process"));
            Assert.That(role.Participants.Single().TargetIdentifier, Is.EqualTo("_participant"));
        });
    }

    [Test]
    public void TransactionChoreographyAndConversationRetainCompleteMessageFlow()
    {
        ParticipantDefinition sender = new ParticipantDefinition
        {
            Identifier = "_sender",
            Name = "Instructing Agent",
            BusinessTransaction = Reference("_sender", RepositoryEntityKind.Participant,
                "businessTransaction", "_transaction", RepositoryEntityKind.BusinessTransaction, 0),
            BusinessRole = Reference("_sender", RepositoryEntityKind.Participant,
                "businessRoleTrace", "_senderRole", RepositoryEntityKind.BusinessRole, 0),
            MinimumOccurs = 1,
            HasMaximumOccurs = true,
            MaximumOccurs = 1
        };
        ParticipantDefinition receiver = new ParticipantDefinition
        {
            Identifier = "_receiver",
            Name = "Instructed Agent",
            MinimumOccurs = 1,
            HasMaximumOccurs = false
        };
        MessageTransmissionDefinition transmission = new MessageTransmissionDefinition
        {
            Identifier = "_transmission",
            Name = "Settlement Instruction",
            MessageTypeDescription = "Provides settlement instructions.",
            BusinessTransaction = Reference("_transmission", RepositoryEntityKind.MessageTransmission,
                "businessTransaction", "_transaction", RepositoryEntityKind.BusinessTransaction, 0),
            Sender = Reference("_transmission", RepositoryEntityKind.MessageTransmission,
                "send", "_sender", RepositoryEntityKind.Participant, 0)
        };
        transmission.Receivers.Add(Reference("_transmission", RepositoryEntityKind.MessageTransmission,
            "receive", "_receiver", RepositoryEntityKind.Participant, 0));
        transmission.DerivedMessageDefinitions.Add(Reference("_transmission", RepositoryEntityKind.MessageTransmission,
            "derivation", "_message", RepositoryEntityKind.MessageDefinition, 0));
        sender.Sends.Add(Reference("_sender", RepositoryEntityKind.Participant,
            "sends", "_transmission", RepositoryEntityKind.MessageTransmission, 0));
        receiver.Receives.Add(Reference("_receiver", RepositoryEntityKind.Participant,
            "receives", "_transmission", RepositoryEntityKind.MessageTransmission, 0));

        BusinessTransactionDefinition transaction = new BusinessTransactionDefinition
        {
            Identifier = "_transaction",
            Name = "Settlement Transaction",
            BusinessProcess = Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
                "businessProcessTrace", "_process", RepositoryEntityKind.BusinessProcess, 0),
            TransportMode = Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
                "messageTransportMode", "_mode", RepositoryEntityKind.TransportMode, 0),
            ParentTransaction = Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
                "parentTransaction", "_parent", RepositoryEntityKind.BusinessTransaction, 0)
        };
        transaction.Participants.Add(Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
            "participant", "_sender", RepositoryEntityKind.Participant, 0));
        transaction.Participants.Add(Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
            "participant", "_receiver", RepositoryEntityKind.Participant, 1));
        transaction.Transmissions.Add(Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
            "transmission", "_transmission", RepositoryEntityKind.MessageTransmission, 0));
        transaction.SubTransactions.Add(Reference("_transaction", RepositoryEntityKind.BusinessTransaction,
            "subTransaction", "_child", RepositoryEntityKind.BusinessTransaction, 0));

        MessageChoreographyDefinition choreography = new MessageChoreographyDefinition
        {
            Identifier = "_choreography",
            Name = "Settlement Choreography",
            BusinessTransaction = Reference("_choreography", RepositoryEntityKind.MessageChoreography,
                "businessTransactionTrace", "_transaction", RepositoryEntityKind.BusinessTransaction, 0)
        };
        choreography.Transmissions.Add(Reference("_choreography", RepositoryEntityKind.MessageChoreography,
            "transmission", "_transmission", RepositoryEntityKind.MessageTransmission, 0));
        choreography.MessageDefinitions.Add(Reference("_choreography", RepositoryEntityKind.MessageChoreography,
            "messageDefinition", "_message", RepositoryEntityKind.MessageDefinition, 0));

        ConversationDefinition conversation = new ConversationDefinition
        {
            Identifier = "_conversation",
            Name = "Settlement Conversation"
        };
        conversation.Participants.AddRange(new[] { transaction.Participants[0], transaction.Participants[1] });
        conversation.Choreographies.Add(Reference("_conversation", RepositoryEntityKind.Conversation,
            "choreography", "_choreography", RepositoryEntityKind.MessageChoreography, 0));
        conversation.Transmissions.Add(choreography.Transmissions[0]);
        transmission.Conversations.Add(Reference("_transmission", RepositoryEntityKind.MessageTransmission,
            "conversation", "_conversation", RepositoryEntityKind.Conversation, 0));

        Assert.Multiple(() =>
        {
            Assert.That(sender.MaximumOccurs, Is.EqualTo(1));
            Assert.That(receiver.HasMaximumOccurs, Is.False);
            Assert.That(sender.Sends.Single().TargetIdentifier, Is.EqualTo("_transmission"));
            Assert.That(receiver.Receives.Single().TargetIdentifier, Is.EqualTo("_transmission"));
            Assert.That(transmission.Sender.TargetIdentifier, Is.EqualTo("_sender"));
            Assert.That(transmission.Receivers.Single().TargetIdentifier, Is.EqualTo("_receiver"));
            Assert.That(transmission.DerivedMessageDefinitions.Single().TargetIdentifier, Is.EqualTo("_message"));
            Assert.That(transaction.Participants.Select(item => item.TargetIdentifier),
                Is.EqualTo(new[] { "_sender", "_receiver" }));
            Assert.That(transaction.ParentTransaction?.TargetIdentifier, Is.EqualTo("_parent"));
            Assert.That(transaction.SubTransactions.Single().TargetIdentifier, Is.EqualTo("_child"));
            Assert.That(choreography.BusinessTransaction.TargetIdentifier, Is.EqualTo("_transaction"));
            Assert.That(conversation.Transmissions.Single().TargetIdentifier, Is.EqualTo("_transmission"));
            Assert.That(transmission.Conversations.Single().TargetIdentifier, Is.EqualTo("_conversation"));
        });
    }

    [Test]
    public void EndpointAndTransportRetainAddressingAndAllModeCharacteristics()
    {
        TransportDefinition mode = new TransportDefinition
        {
            Identifier = "_mode",
            Kind = RepositoryEntityKind.TransportMode,
            Name = "Reliable Mode",
            BoundedCommunicationDelay = new IsoDurationValue { Minutes = 5, HasMinutes = true, LexicalValue = "PT5M" },
            MaximumClockVariation = new IsoDurationValue { Seconds = 30, HasSeconds = true, LexicalValue = "PT30S" },
            MaximumMessageSizeKilobytes = 2048,
            MessageDeliveryWindow = new IsoDurationValue { Seconds = 10, HasSeconds = true, LexicalValue = "PT10S" },
            MessageSendingWindow = new IsoDurationValue { Seconds = 15, HasSeconds = true, LexicalValue = "PT15S" },
            DeliveryAssurance = "AT_LEAST_ONCE",
            Durability = "DURABLE",
            MessageCasting = "UNICAST",
            MessageDeliveryOrder = "PRESERVED",
            MessageValidationLevel = "FULL",
            MessageValidationOnOff = "ON",
            MessageValidationResults = "REJECT",
            ReceiverAsynchronicity = "ASYNCHRONOUS",
            SenderAsynchronicity = "ASYNCHRONOUS"
        };
        mode.BusinessTransactions.Add(Reference("_mode", RepositoryEntityKind.TransportMode,
            "businessTransaction", "_transaction", RepositoryEntityKind.BusinessTransaction, 0));

        TransportDefinition system = new TransportDefinition
        {
            Identifier = "_system",
            Kind = RepositoryEntityKind.MessageTransportSystem,
            Name = "Example Network"
        };
        MessagingEndpointDefinition endpoint = new MessagingEndpointDefinition
        {
            Identifier = "_endpoint",
            Name = "Receiving Endpoint",
            TransportSystem = Reference("_endpoint", RepositoryEntityKind.MessagingEndpoint,
                "transportSystem", "_system", RepositoryEntityKind.MessageTransportSystem, 0)
        };
        endpoint.Addresses.Add(Reference("_endpoint", RepositoryEntityKind.MessagingEndpoint,
            "location", "_address", RepositoryEntityKind.Address, 0));
        endpoint.BroadcastLists.Add(Reference("_endpoint", RepositoryEntityKind.MessagingEndpoint,
            "broadcastList", "_broadcast", RepositoryEntityKind.BroadcastList, 0));
        endpoint.SentMessages.Add(Reference("_endpoint", RepositoryEntityKind.MessagingEndpoint,
            "sentMessage", "_sent", RepositoryEntityKind.TransportMessage, 0));
        endpoint.ReceivedMessages.Add(Reference("_endpoint", RepositoryEntityKind.MessagingEndpoint,
            "receivedMessage", "_received", RepositoryEntityKind.TransportMessage, 0));
        system.Endpoints.Add(Reference("_system", RepositoryEntityKind.MessageTransportSystem,
            "endpoint", "_endpoint", RepositoryEntityKind.MessagingEndpoint, 0));
        system.Addresses.Add(endpoint.Addresses[0]);
        system.BroadcastLists.Add(endpoint.BroadcastLists[0]);
        system.TransportMessages.AddRange(new[] { endpoint.SentMessages[0], endpoint.ReceivedMessages[0] });

        Assert.Multiple(() =>
        {
            Assert.That(mode.BoundedCommunicationDelay?.LexicalValue, Is.EqualTo("PT5M"));
            Assert.That(mode.MaximumClockVariation?.Seconds, Is.EqualTo(30));
            Assert.That(mode.MaximumMessageSizeKilobytes, Is.EqualTo(2048));
            Assert.That(mode.MessageDeliveryWindow?.LexicalValue, Is.EqualTo("PT10S"));
            Assert.That(mode.MessageSendingWindow?.LexicalValue, Is.EqualTo("PT15S"));
            Assert.That(mode.DeliveryAssurance, Is.EqualTo("AT_LEAST_ONCE"));
            Assert.That(mode.Durability, Is.EqualTo("DURABLE"));
            Assert.That(mode.MessageCasting, Is.EqualTo("UNICAST"));
            Assert.That(mode.MessageDeliveryOrder, Is.EqualTo("PRESERVED"));
            Assert.That(mode.MessageValidationLevel, Is.EqualTo("FULL"));
            Assert.That(mode.MessageValidationOnOff, Is.EqualTo("ON"));
            Assert.That(mode.MessageValidationResults, Is.EqualTo("REJECT"));
            Assert.That(mode.ReceiverAsynchronicity, Is.EqualTo("ASYNCHRONOUS"));
            Assert.That(mode.SenderAsynchronicity, Is.EqualTo("ASYNCHRONOUS"));
            Assert.That(endpoint.TransportSystem.TargetIdentifier, Is.EqualTo("_system"));
            Assert.That(endpoint.Addresses.Single().TargetKind, Is.EqualTo(RepositoryEntityKind.Address));
            Assert.That(endpoint.BroadcastLists.Single().TargetIdentifier, Is.EqualTo("_broadcast"));
            Assert.That(system.Endpoints.Single().TargetIdentifier, Is.EqualTo("_endpoint"));
            Assert.That(system.TransportMessages.Select(item => item.TargetIdentifier),
                Is.EqualTo(new[] { "_sent", "_received" }));
        });
    }

    [Test]
    public void MutableCatalogueDefaultsAreIndependentBetweenInstances()
    {
        RepositorySnapshot firstSnapshot = new RepositorySnapshot();
        RepositorySnapshot secondSnapshot = new RepositorySnapshot();
        BusinessAreaDefinition firstArea = new BusinessAreaDefinition();
        BusinessAreaDefinition secondArea = new BusinessAreaDefinition();
        BusinessProcessDefinition firstProcess = new BusinessProcessDefinition();
        BusinessProcessDefinition secondProcess = new BusinessProcessDefinition();
        BusinessRoleDefinition firstRole = new BusinessRoleDefinition();
        BusinessRoleDefinition secondRole = new BusinessRoleDefinition();
        ParticipantDefinition firstParticipant = new ParticipantDefinition();
        ParticipantDefinition secondParticipant = new ParticipantDefinition();
        BusinessTransactionDefinition firstTransaction = new BusinessTransactionDefinition();
        BusinessTransactionDefinition secondTransaction = new BusinessTransactionDefinition();
        ConversationDefinition firstConversation = new ConversationDefinition();
        ConversationDefinition secondConversation = new ConversationDefinition();
        MessageChoreographyDefinition firstChoreography = new MessageChoreographyDefinition();
        MessageChoreographyDefinition secondChoreography = new MessageChoreographyDefinition();
        MessageTransmissionDefinition firstTransmission = new MessageTransmissionDefinition();
        MessageTransmissionDefinition secondTransmission = new MessageTransmissionDefinition();
        MessagingEndpointDefinition firstEndpoint = new MessagingEndpointDefinition();
        MessagingEndpointDefinition secondEndpoint = new MessagingEndpointDefinition();
        TransportDefinition firstTransport = new TransportDefinition();
        TransportDefinition secondTransport = new TransportDefinition();

        firstSnapshot.BusinessAreas.Add(firstArea);
        firstArea.MessageDefinitions.Add(new RepositoryReference());
        firstProcess.BusinessRoles.Add(new RepositoryReference());
        firstRole.Participants.Add(new RepositoryReference());
        firstParticipant.Sends.Add(new RepositoryReference());
        firstTransaction.Participants.Add(new RepositoryReference());
        firstConversation.Transmissions.Add(new RepositoryReference());
        firstChoreography.MessageDefinitions.Add(new RepositoryReference());
        firstTransmission.Receivers.Add(new RepositoryReference());
        firstEndpoint.Addresses.Add(new RepositoryReference());
        firstTransport.Endpoints.Add(new RepositoryReference());

        Assert.Multiple(() =>
        {
            Assert.That(secondSnapshot.BusinessAreas, Is.Empty);
            Assert.That(secondArea.MessageDefinitions, Is.Empty);
            Assert.That(secondProcess.BusinessRoles, Is.Empty);
            Assert.That(secondRole.Participants, Is.Empty);
            Assert.That(secondParticipant.Sends, Is.Empty);
            Assert.That(secondTransaction.Participants, Is.Empty);
            Assert.That(secondConversation.Transmissions, Is.Empty);
            Assert.That(secondChoreography.MessageDefinitions, Is.Empty);
            Assert.That(secondTransmission.Receivers, Is.Empty);
            Assert.That(secondEndpoint.Addresses, Is.Empty);
            Assert.That(secondTransport.Endpoints, Is.Empty);
            Assert.That(firstArea.Registration, Is.Not.SameAs(secondArea.Registration));
            Assert.That(firstTransaction.TransportMode, Is.Not.SameAs(secondTransaction.TransportMode));
            Assert.That(firstEndpoint.Location, Is.Not.SameAs(secondEndpoint.Location));
        });
    }

    private static RepositoryReference Reference(
        string sourceIdentifier,
        RepositoryEntityKind sourceKind,
        string propertyName,
        string targetIdentifier,
        RepositoryEntityKind targetKind,
        int position)
    {
        return new RepositoryReference
        {
            SourceIdentifier = sourceIdentifier,
            SourceKind = sourceKind,
            PropertyName = propertyName,
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            Position = position,
            IsResolved = true
        };
    }
}
