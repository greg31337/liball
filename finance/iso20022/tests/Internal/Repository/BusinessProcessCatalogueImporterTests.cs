using System.Text;
using iso20022;

namespace iso20022.Tests;

public class BusinessProcessCatalogueImporterTests
{
    [Test]
    public void ImporterBuildsAreasProcessesRolesTransactionsAndChoreography()
    {
        RepositorySnapshot snapshot = ImportFixture();
        BusinessAreaDefinition area = snapshot.BusinessAreas.Single();
        BusinessProcessDefinition process = snapshot.BusinessProcesses[0];
        BusinessRoleDefinition senderRole = snapshot.BusinessRoles[0];
        BusinessTransactionDefinition transaction = snapshot.BusinessTransactions[0];
        ParticipantDefinition sender = snapshot.Participants[0];
        ParticipantDefinition receiver = snapshot.Participants[1];
        MessageChoreographyDefinition choreography = snapshot.MessageChoreographies.Single();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Identifier, Is.EqualTo("_repository"));
            Assert.That(snapshot.BusinessProcessCatalogue.TargetIdentifier, Is.EqualTo("_catalogue"));
            Assert.That(snapshot.BusinessProcessCatalogue.IsResolved, Is.True);
            Assert.That(area.Code, Is.EqualTo("pain"));
            Assert.That(area.Registration.Status, Is.EqualTo(RegistrationStatus.Registered));
            Assert.That(area.Registration.Examples, Is.EqualTo(new[] { "Payments area example" }));
            Assert.That(area.MessageDefinitions.Single().TargetIdentifier, Is.EqualTo("_message"));
            Assert.That(process.Name, Is.EqualTo("PaymentProcess"));
            Assert.That(process.Extenders.Single().TargetIdentifier, Is.EqualTo("_process2"));
            Assert.That(process.IncludedProcesses.Single().TargetIdentifier, Is.EqualTo("_process2"));
            Assert.That(process.BusinessRoles.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_senderRole", "_receiverRole" }));
            Assert.That(process.BusinessTransactions.Single().TargetIdentifier,
                Is.EqualTo("_transaction"));
            Assert.That(senderRole.BusinessProcess.TargetIdentifier, Is.EqualTo("_process"));
            Assert.That(senderRole.Participants.Single().TargetIdentifier,
                Is.EqualTo("_senderParticipant"));
            Assert.That(transaction.BusinessProcess.TargetIdentifier, Is.EqualTo("_process"));
            Assert.That(transaction.Participants.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_senderParticipant", "_receiverParticipant" }));
            Assert.That(transaction.Transmissions.Single().TargetIdentifier,
                Is.EqualTo("_transmission"));
            Assert.That(transaction.TransportMode.TargetIdentifier, Is.EqualTo("_transportMode"));
            Assert.That(transaction.SubTransactions.Single().TargetIdentifier,
                Is.EqualTo("_subTransaction"));
            Assert.That(transaction.Choreographies.Single().TargetIdentifier,
                Is.EqualTo("_choreography"));
            Assert.That(snapshot.BusinessTransactions[1].ParentTransaction?.TargetIdentifier,
                Is.EqualTo("_transaction"));
            Assert.That(sender.MinimumOccurs, Is.EqualTo(1));
            Assert.That(sender.HasMaximumOccurs, Is.True);
            Assert.That(sender.MaximumOccurs, Is.EqualTo(1));
            Assert.That(sender.BusinessRole.TargetIdentifier, Is.EqualTo("_senderRole"));
            Assert.That(sender.Sends.Single().TargetIdentifier, Is.EqualTo("_transmission"));
            Assert.That(receiver.Receives.Single().TargetIdentifier, Is.EqualTo("_transmission"));
            Assert.That(choreography.BusinessTransaction.TargetIdentifier,
                Is.EqualTo("_transaction"));
            Assert.That(choreography.MessageDefinitions.Single().TargetIdentifier,
                Is.EqualTo("_message"));
            Assert.That(choreography.Transmissions.Single().TargetIdentifier,
                Is.EqualTo("_transmission"));
            Assert.That(area.Location.SourceName, Is.EqualTo("catalogue.iso20022"));
        });
    }

    [Test]
    public void ImporterCollapsesSendReceiveHelpersIntoTransmissionParticipants()
    {
        RepositorySnapshot snapshot = ImportFixture();
        MessageTransmissionDefinition transmission = snapshot.MessageTransmissions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.MessageTransmissions, Has.Count.EqualTo(1));
            Assert.That(transmission.Identifier, Is.EqualTo("_transmission"));
            Assert.That(transmission.BusinessTransaction.TargetIdentifier,
                Is.EqualTo("_transaction"));
            Assert.That(transmission.Sender.TargetIdentifier, Is.EqualTo("_senderParticipant"));
            Assert.That(transmission.Receivers.Single().TargetIdentifier,
                Is.EqualTo("_receiverParticipant"));
            Assert.That(transmission.MessageTypeDescription, Is.EqualTo("Payment instruction."));
            Assert.That(transmission.DerivedMessageDefinitions.Single().TargetIdentifier,
                Is.EqualTo("_message"));
            Assert.That(transmission.Sender.SourceIdentifier, Is.EqualTo("_transmission"));
            Assert.That(transmission.Receivers.Single().SourceIdentifier,
                Is.EqualTo("_transmission"));
            Assert.That(snapshot.Participants.All(participant =>
                participant.Identifier != "_send" && participant.Identifier != "_receive"), Is.True);
        });
    }

    [Test]
    public void ImporterBuildsTransportCharacteristicsEndpointsAndMessages()
    {
        RepositorySnapshot snapshot = ImportFixture();
        TransportDefinition mode = snapshot.Transports.Single(
            transport => transport.Identifier == "_transportMode");
        TransportDefinition system = snapshot.Transports.Single(
            transport => transport.Identifier == "_transportSystem");
        TransportDefinition address = snapshot.Transports.Single(
            transport => transport.Identifier == "_address");
        TransportDefinition broadcast = snapshot.Transports.Single(
            transport => transport.Identifier == "_broadcast");
        TransportDefinition message = snapshot.Transports.Single(
            transport => transport.Identifier == "_transportMessage");
        TransportDefinition instance = snapshot.Transports.Single(
            transport => transport.Identifier == "_messageInstance");
        MessagingEndpointDefinition endpoint = snapshot.MessagingEndpoints.Single();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Transports, Has.Count.EqualTo(6));
            Assert.That(mode.Kind, Is.EqualTo(RepositoryEntityKind.TransportMode));
            Assert.That(mode.BoundedCommunicationDelay?.LexicalValue, Is.EqualTo("PT5S"));
            Assert.That(mode.MaximumClockVariation?.LexicalValue, Is.EqualTo("PT1S"));
            Assert.That(mode.MaximumMessageSizeKilobytes, Is.EqualTo(2048));
            Assert.That(mode.MessageDeliveryWindow?.LexicalValue, Is.EqualTo("PT10S"));
            Assert.That(mode.MessageSendingWindow?.LexicalValue, Is.EqualTo("PT15S"));
            Assert.That(mode.DeliveryAssurance, Is.EqualTo("AtLeastOnce"));
            Assert.That(mode.Durability, Is.EqualTo("Durable"));
            Assert.That(mode.MessageCasting, Is.EqualTo("Unicast"));
            Assert.That(mode.MessageDeliveryOrder, Is.EqualTo("FIFO"));
            Assert.That(mode.MessageValidationLevel, Is.EqualTo("Full"));
            Assert.That(mode.MessageValidationOnOff, Is.EqualTo("On"));
            Assert.That(mode.MessageValidationResults, Is.EqualTo("Report"));
            Assert.That(mode.ReceiverAsynchronicity, Is.EqualTo("Asynchronous"));
            Assert.That(mode.SenderAsynchronicity, Is.EqualTo("Synchronous"));
            Assert.That(mode.BusinessTransactions.Single().TargetIdentifier,
                Is.EqualTo("_transaction"));
            Assert.That(system.Kind, Is.EqualTo(RepositoryEntityKind.MessageTransportSystem));
            Assert.That(system.Endpoints.Single().TargetIdentifier, Is.EqualTo("_endpoint"));
            Assert.That(endpoint.TransportSystem.TargetIdentifier, Is.EqualTo("_transportSystem"));
            Assert.That(endpoint.Addresses.Single().TargetIdentifier, Is.EqualTo("_address"));
            Assert.That(endpoint.BroadcastLists.Single().TargetIdentifier, Is.EqualTo("_broadcast"));
            Assert.That(endpoint.SentMessages.Single().TargetIdentifier,
                Is.EqualTo("_transportMessage"));
            Assert.That(endpoint.ReceivedMessages.Single().TargetIdentifier,
                Is.EqualTo("_transportMessage"));
            Assert.That(address.Kind, Is.EqualTo(RepositoryEntityKind.Address));
            Assert.That(address.Endpoints.Single().TargetIdentifier, Is.EqualTo("_endpoint"));
            Assert.That(address.BroadcastLists.Single().TargetIdentifier, Is.EqualTo("_broadcast"));
            Assert.That(broadcast.Kind, Is.EqualTo(RepositoryEntityKind.BroadcastList));
            Assert.That(broadcast.Addresses.Single().TargetIdentifier, Is.EqualTo("_address"));
            Assert.That(message.Kind, Is.EqualTo(RepositoryEntityKind.TransportMessage));
            Assert.That(message.Endpoints.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_endpoint", "_endpoint" }));
            Assert.That(message.TransportMessages.Single().TargetIdentifier,
                Is.EqualTo("_messageInstance"));
            Assert.That(instance.TransportMessages.Single().TargetIdentifier,
                Is.EqualTo("_transportMessage"));
            Assert.That(snapshot.Conversations.Single().Identifier, Is.EqualTo("_conversation"));
        });
    }

    [Test]
    public void ImporterPreservesCatalogueAndReferenceOrderingAcrossRuns()
    {
        RepositorySnapshot first = ImportFixture();
        RepositorySnapshot second = ImportFixture();

        Assert.Multiple(() =>
        {
            Assert.That(first.BusinessProcesses.Select(process => process.Identifier),
                Is.EqualTo(new[] { "_process", "_process2" }));
            Assert.That(first.BusinessTransactions.Select(transaction => transaction.Identifier),
                Is.EqualTo(new[] { "_transaction", "_subTransaction" }));
            Assert.That(first.Transports.Select(transport => transport.Identifier),
                Is.EqualTo(second.Transports.Select(transport => transport.Identifier)));
            Assert.That(first.References.Any(reference => !reference.IsResolved), Is.False);
            Assert.That(first.References.Select(reference =>
                    reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
                    reference.Position + "|" + reference.TargetIdentifier),
                Is.EqualTo(second.References.Select(reference =>
                    reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
                    reference.Position + "|" + reference.TargetIdentifier)));
        });
    }

    private static RepositorySnapshot ImportFixture()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateCatalogueFixture());
        using MemoryStream source = new MemoryStream(bytes);
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            source,
            "catalogue.iso20022",
            adapter,
            new XmiPassOneOptions());
        XmiPassTwoResult passTwo = XmiPassTwoReader.BuildGraph(
            source,
            "catalogue.iso20022",
            adapter,
            passOne,
            new XmiPassTwoOptions());
        return BusinessProcessCatalogueImporter.ImportBusinessProcessCatalogue(passTwo);
    }

    private static string CreateCatalogueFixture()
    {
        return "<?xml version='1.0'?>\n" +
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' " +
            "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>\n" +
            "  <dataDictionary xmi:id='_dictionary'/>\n" +
            "  <businessProcessCatalogue xmi:id='_catalogue'>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessArea' xmi:id='_area' " +
            "name='Payments' definition='Payments business area.' code='pain' " +
            "registrationStatus='Registered'>\n" +
            "      <example>Payments area example</example>\n" +
            "      <messageDefinition xmi:id='_message' name='PaymentInstruction'/>\n" +
            "    </topLevelCatalogueEntry>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessProcess' xmi:id='_process' " +
            "name='PaymentProcess' extender='_process2' included='_process2' " +
            "businessProcessTrace='_transaction'>\n" +
            "      <businessRole xmi:id='_senderRole' name='DebtorAgent' " +
            "businessRoleTrace='_senderParticipant'/>\n" +
            "      <businessRole xmi:id='_receiverRole' name='CreditorAgent' " +
            "businessRoleTrace='_receiverParticipant'/>\n" +
            "    </topLevelCatalogueEntry>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessProcess' xmi:id='_process2' " +
            "name='ExtendedPaymentProcess' extended='_process' includer='_process'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessTransaction' " +
            "xmi:id='_transaction' name='PaymentTransaction' businessProcessTrace='_process' " +
            "messageTransportMode='_transportMode' subTransaction='_subTransaction' " +
            "trace='_choreography'>\n" +
            "      <participant xmi:id='_senderParticipant' name='DebtorAgentParticipant' " +
            "minOccurs='1' maxOccurs='1' businessRoleTrace='_senderRole' sends='_send'/>\n" +
            "      <participant xmi:id='_receiverParticipant' name='CreditorAgentParticipant' " +
            "minOccurs='1' maxOccurs='1' businessRoleTrace='_receiverRole' receives='_receive'/>\n" +
            "      <transmission xmi:id='_transmission' name='PaymentTransmission' " +
            "messageTypeDescription='Payment instruction.' derivation='_message'>\n" +
            "        <send xmi:id='_send' sender='_senderParticipant'/>\n" +
            "        <receive xmi:id='_receive' receiver='_receiverParticipant'/>\n" +
            "      </transmission>\n" +
            "    </topLevelCatalogueEntry>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessTransaction' " +
            "xmi:id='_subTransaction' name='SubTransaction' parentTransaction='_transaction'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:MessageChoreography' " +
            "xmi:id='_choreography' name='PaymentChoreography' " +
            "businessTransactionTrace='_transaction' messageDefinition='_message'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:MessageTransportMode' " +
            "xmi:id='_transportMode' name='ReliableTransport' boundedCommunicationDelay='PT5S' " +
            "maximumClockVariation='PT1S' maximumMessageSize='2048' " +
            "messageDeliveryWindow='PT10S' messageSendingWindow='PT15S' " +
            "deliveryAssurance='AtLeastOnce' durability='Durable' messageCasting='Unicast' " +
            "messageDeliveryOrder='FIFO' messageValidationLevel='Full' " +
            "messageValidationOnOff='On' messageValidationResults='Report' " +
            "receiverAsynchronicity='Asynchronous' senderAsynchronicity='Synchronous' " +
            "businessTransaction='_transaction'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:Conversation' " +
            "xmi:id='_conversation'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:MessageTransportSystem' " +
            "xmi:id='_transportSystem'>\n" +
            "      <endpoint xmi:id='_endpoint' location='_address' " +
            "sentMessage='_transportMessage' receivedMessage='_transportMessage'/>\n" +
            "    </topLevelCatalogueEntry>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:Address' xmi:id='_address' " +
            "endpoint='_endpoint' broadCastList='_broadcast'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BroadcastList' xmi:id='_broadcast' " +
            "address='_address'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:TransportMessage' " +
            "xmi:id='_transportMessage' sender='_endpoint' receiver='_endpoint' " +
            "messageInstance='_messageInstance'/>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:MessageInstance' " +
            "xmi:id='_messageInstance' transportMessage='_transportMessage'/>\n" +
            "  </businessProcessCatalogue>\n" +
            "</iso:Repository>";
    }
}
