using System.Text;
using iso20022;

namespace iso20022.Tests;

public class MessageModelImporterTests
{
    [Test]
    public void ImporterBuildsMessageSetsDefinitionsIdentifiersAndLifecycleMetadata()
    {
        RepositorySnapshot snapshot = ImportFixture();
        MessageSetDefinition set = snapshot.MessageSets.Single();
        MessageDefinitionMetadata previous = snapshot.MessageDefinitions[0];
        MessageDefinitionMetadata message = snapshot.MessageDefinitions[1];

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Identifier, Is.EqualTo("_repository"));
            Assert.That(snapshot.DataDictionary.TargetIdentifier, Is.EqualTo("_dictionary"));
            Assert.That(snapshot.BusinessProcessCatalogue.TargetIdentifier,
                Is.EqualTo("_catalogue"));
            Assert.That(set.Name, Is.EqualTo("PaymentsMessageSet"));
            Assert.That(set.BusinessProcessCatalogue.TargetIdentifier,
                Is.EqualTo("_catalogue"));
            Assert.That(set.GeneratedSyntaxes.Single().TargetIdentifier,
                Is.EqualTo("_syntax"));
            Assert.That(set.ValidEncodings.Single().TargetIdentifier,
                Is.EqualTo("_encoding"));
            Assert.That(set.MessageDefinitions.Single().TargetIdentifier,
                Is.EqualTo("_message"));
            Assert.That(message.MessageIdentifier.BusinessArea, Is.EqualTo("pacs"));
            Assert.That(message.MessageIdentifier.MessageFunctionality, Is.EqualTo("008"));
            Assert.That(message.MessageIdentifier.Variant, Is.EqualTo("001"));
            Assert.That(message.MessageIdentifier.Version, Is.EqualTo("10"));
            Assert.That(message.MessageIdentifier.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(message.NamespaceUri,
                Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(message.XmlName, Is.EqualTo("FIToFICustomerCreditTransferV10"));
            Assert.That(message.XmlTag, Is.EqualTo("FIToFICstmrCdtTrf"));
            Assert.That(message.RootElement, Is.EqualTo("Document"));
            Assert.That(message.BusinessArea.TargetIdentifier, Is.EqualTo("_area"));
            Assert.That(message.MessageSets.Single().TargetIdentifier, Is.EqualTo("_set"));
            Assert.That(message.BuildingBlocks.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_groupHeader", "_transactionInformation" }));
            Assert.That(message.Choices.Single().TargetIdentifier,
                Is.EqualTo("_definitionXor"));
            Assert.That(message.Choreographies.Single().TargetIdentifier,
                Is.EqualTo("_choreography"));
            Assert.That(message.MessageTransmissions.Single().TargetIdentifier,
                Is.EqualTo("_transmission"));
            Assert.That(message.SyntaxMessageSchemes.Single().TargetIdentifier,
                Is.EqualTo("_scheme"));
            Assert.That(message.Registration.Status, Is.EqualTo(RegistrationStatus.Registered));
            Assert.That(message.Registration.PreviousVersion?.TargetIdentifier,
                Is.EqualTo("_previousMessage"));
            Assert.That(message.Registration.Examples, Is.EqualTo(new[] { "Payment example" }));
            Assert.That(message.Registration.Doclets.Single().Type, Is.EqualTo("usage"));
            Assert.That(message.Registration.Doclets.Single().Value,
                Is.EqualTo("Use for interbank credit transfers."));
            Assert.That(previous.Registration.Status, Is.EqualTo(RegistrationStatus.Obsolete));
            Assert.That(previous.Registration.RemovalDate?.LexicalValue,
                Is.EqualTo("2026-06-30"));
            Assert.That(previous.Registration.NextVersions.Single().TargetIdentifier,
                Is.EqualTo("_message"));
        });
    }

    [Test]
    public void ImporterBuildsMessageComponentsElementsChoicesAndMultiplicities()
    {
        RepositorySnapshot snapshot = ImportFixture();
        MessageComponentDefinition component = snapshot.MessageComponents.Single(
            item => item.Identifier == "_component");
        MessageElementDefinition attribute = snapshot.MessageElements.Single(
            item => item.Identifier == "_attribute");
        MessageElementDefinition association = snapshot.MessageElements.Single(
            item => item.Identifier == "_association");
        ChoiceDefinition reusableChoice = snapshot.Choices.Single(
            item => item.Identifier == "_choice");
        ChoiceDefinition componentXor = snapshot.Choices.Single(
            item => item.Identifier == "_componentXor");
        ChoiceDefinition definitionXor = snapshot.Choices.Single(
            item => item.Identifier == "_definitionXor");
        MessageBuildingBlockDefinition groupHeader = snapshot.MessageBuildingBlocks[0];
        MessageBuildingBlockDefinition transactionInformation =
            snapshot.MessageBuildingBlocks[1];

        Assert.Multiple(() =>
        {
            Assert.That(component.DataDictionary.TargetIdentifier, Is.EqualTo("_dictionary"));
            Assert.That(component.BusinessComponentTrace?.TargetIdentifier,
                Is.EqualTo("_businessComponent"));
            Assert.That(component.MessageBuildingBlocks.Single().TargetIdentifier,
                Is.EqualTo("_groupHeader"));
            Assert.That(component.MessageElements.Select(reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_attribute", "_association" }));
            Assert.That(component.Choices.Single().TargetIdentifier,
                Is.EqualTo("_componentXor"));
            Assert.That(attribute.Kind, Is.EqualTo(MessageElementKind.Attribute));
            Assert.That(attribute.XmlTag, Is.EqualTo("InstrId"));
            Assert.That(attribute.IsTechnical, Is.True);
            Assert.That(attribute.IsDerived, Is.True);
            Assert.That(attribute.Multiplicity.MinimumOccurs, Is.EqualTo(1));
            Assert.That(attribute.Multiplicity.HasMaximumOccurs, Is.True);
            Assert.That(attribute.Multiplicity.MaximumOccurs, Is.EqualTo(1));
            Assert.That(attribute.ComponentContext.TargetIdentifier, Is.EqualTo("_component"));
            Assert.That(attribute.SimpleType?.TargetIdentifier, Is.EqualTo("_text"));
            Assert.That(attribute.MemberType.TargetIdentifier, Is.EqualTo("_text"));
            Assert.That(attribute.BusinessElementTrace?.TargetIdentifier,
                Is.EqualTo("_businessElement"));
            Assert.That(association.Kind, Is.EqualTo(MessageElementKind.AssociationEnd));
            Assert.That(association.IsComposite, Is.True);
            Assert.That(association.ComplexType?.TargetIdentifier,
                Is.EqualTo("_nestedComponent"));
            Assert.That(association.MemberType.TargetIdentifier,
                Is.EqualTo("_nestedComponent"));
            Assert.That(association.BusinessComponentTrace?.TargetIdentifier,
                Is.EqualTo("_businessComponent"));
            Assert.That(reusableChoice.ChoiceType, Is.EqualTo("ChoiceComponent"));
            Assert.That(reusableChoice.MessageElements.Single().TargetIdentifier,
                Is.EqualTo("_choiceElement"));
            Assert.That(componentXor.OwnerMessageComponent?.TargetIdentifier,
                Is.EqualTo("_component"));
            Assert.That(componentXor.ImpactedMessageElements.Select(
                    reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_attribute", "_association" }));
            Assert.That(definitionXor.OwnerMessageDefinition?.TargetIdentifier,
                Is.EqualTo("_message"));
            Assert.That(definitionXor.ImpactedBuildingBlocks.Select(
                    reference => reference.TargetIdentifier),
                Is.EqualTo(new[] { "_groupHeader", "_transactionInformation" }));
            Assert.That(groupHeader.MessageDefinition.TargetIdentifier, Is.EqualTo("_message"));
            Assert.That(groupHeader.ComplexType?.TargetIdentifier, Is.EqualTo("_component"));
            Assert.That(groupHeader.MemberType.TargetIdentifier, Is.EqualTo("_component"));
            Assert.That(transactionInformation.SimpleType?.TargetIdentifier,
                Is.EqualTo("_text"));
            Assert.That(transactionInformation.Multiplicity.HasMaximumOccurs, Is.False);
        });
    }

    [Test]
    public void ImporterBuildsSyntaxEncodingAndMessageSchemeRelationships()
    {
        RepositorySnapshot snapshot = ImportFixture();
        SyntaxDefinition syntax = snapshot.Syntaxes.Single();
        EncodingDefinition encoding = snapshot.Encodings.Single();
        SyntaxMessageSchemeDefinition scheme = snapshot.SyntaxMessageSchemes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(syntax.Identifier, Is.EqualTo("_syntax"));
            Assert.That(syntax.PossibleEncodings.Single().TargetIdentifier,
                Is.EqualTo("_encoding"));
            Assert.That(syntax.GeneratedForMessageSets.Single().TargetIdentifier,
                Is.EqualTo("_set"));
            Assert.That(encoding.Syntax.TargetIdentifier, Is.EqualTo("_syntax"));
            Assert.That(encoding.MessageSets.Single().TargetIdentifier, Is.EqualTo("_set"));
            Assert.That(scheme.BusinessProcessCatalogue.TargetIdentifier,
                Is.EqualTo("_catalogue"));
            Assert.That(scheme.MessageDefinitionTrace.TargetIdentifier,
                Is.EqualTo("_message"));
            Assert.That(snapshot.References.Any(reference => !reference.IsResolved), Is.False);
        });
    }

    [Test]
    public void ImporterPreservesMessageAndReferenceOrderingAcrossRuns()
    {
        RepositorySnapshot first = ImportFixture();
        RepositorySnapshot second = ImportFixture();

        Assert.Multiple(() =>
        {
            Assert.That(first.MessageDefinitions.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_previousMessage", "_message" }));
            Assert.That(first.MessageBuildingBlocks.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_groupHeader", "_transactionInformation" }));
            Assert.That(first.MessageElements.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_attribute", "_association", "_choiceElement" }));
            Assert.That(first.Choices.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_componentXor", "_choice", "_definitionXor" }));
            Assert.That(first.References.Select(ReferenceKey),
                Is.EqualTo(second.References.Select(ReferenceKey)));
        });
    }

    private static string ReferenceKey(RepositoryReference reference)
    {
        return reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
            reference.Position + "|" + reference.TargetIdentifier;
    }

    private static RepositorySnapshot ImportFixture()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateFixture());
        using MemoryStream source = new MemoryStream(bytes);
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            source,
            "message-model.iso20022",
            adapter,
            new XmiPassOneOptions());
        XmiPassTwoResult passTwo = XmiPassTwoReader.BuildGraph(
            source,
            "message-model.iso20022",
            adapter,
            passOne,
            new XmiPassTwoOptions());
        return MessageModelImporter.ImportMessageModel(passTwo);
    }

    private static string CreateFixture()
    {
        return """
            <?xml version='1.0'?>
            <iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore'
                xmlns:xmi='http://www.omg.org/XMI'
                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>
              <dataDictionary xmi:id='_dictionary'>
                <topLevelDictionaryEntry xsi:type='iso:Text' xmi:id='_text' name='Max35Text'/>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent'
                    xmi:id='_businessComponent' name='Payment'>
                  <element xsi:type='iso:BusinessAttribute' xmi:id='_businessElement'
                      name='InstructionIdentification' minOccurs='0' maxOccurs='1'
                      simpleType='_text'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:MessageComponent'
                    xmi:id='_nestedComponent' name='PartyIdentification'/>
                <topLevelDictionaryEntry xsi:type='iso:MessageComponent' xmi:id='_component'
                    name='GroupHeader' definition='Shared group header.' registrationStatus='Registered'
                    trace='_businessComponent'>
                  <messageElement xsi:type='iso:MessageAttribute' xmi:id='_attribute'
                      name='InstructionIdentification' definition='Instruction identifier.'
                      registrationStatus='Provisionally Registered' minOccurs='1' maxOccurs='1'
                      xmlTag='InstrId' isTechnical='true' isDerived='true' simpleType='_text'
                      businessElementTrace='_businessElement'/>
                  <messageElement xsi:type='iso:MessageAssociationEnd' xmi:id='_association'
                      name='Debtor' definition='Debtor party.' minOccurs='0' maxOccurs='1'
                      xmlTag='Dbtr' isComposite='true' type='_nestedComponent'
                      businessComponentTrace='_businessComponent'/>
                  <xors xmi:id='_componentXor' name='IdentifierOrDebtor'
                      impactedElements='_attribute _association'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:ChoiceComponent' xmi:id='_choice'
                    name='PartyChoice' definition='A reusable party choice.' trace='_businessComponent'>
                  <messageElement xsi:type='iso:MessageAttribute' xmi:id='_choiceElement'
                      name='OrganisationIdentification' minOccurs='1' maxOccurs='1'
                      complexType='_nestedComponent'/>
                </topLevelDictionaryEntry>
              </dataDictionary>
              <businessProcessCatalogue xmi:id='_catalogue'>
                <topLevelCatalogueEntry xsi:type='iso:BusinessArea' xmi:id='_area'
                    name='Payments' code='pacs'>
                  <messageDefinition xmi:id='_previousMessage' name='PreviousCreditTransfer'
                      registrationStatus='Obsolete' removalDate='2026-06-30'
                      nextVersions='_message'>
                    <messageDefinitionIdentifier businessArea='pacs'
                        messageFunctionality='008' flavour='001' version='09'/>
                  </messageDefinition>
                  <messageDefinition xmi:id='_message' name='FIToFICustomerCreditTransferV10'
                      definition='Interbank customer credit transfer.' registrationStatus='Registered'
                      previousVersion='_previousMessage' messageSet='_set'
                      xmlName='FIToFICustomerCreditTransferV10' xmlTag='FIToFICstmrCdtTrf'
                      rootElement='Document' choreography='_choreography' trace='_transmission'
                      derivation='_scheme'>
                    <example>Payment example</example>
                    <doclet xmi:id='_doclet' type='usage'
                        content='Use for interbank credit transfers.'/>
                    <messageBuildingBlock xmi:id='_groupHeader' name='GroupHeader'
                        registrationStatus='Provisionally Registered' minOccurs='1' maxOccurs='1'
                        xmlTag='GrpHdr' complexType='_component'/>
                    <messageBuildingBlock xmi:id='_transactionInformation'
                        name='CreditTransferTransactionInformation' minOccurs='0'
                        xmlTag='CdtTrfTxInf' simpleType='_text'/>
                    <xors xmi:id='_definitionXor' name='HeaderOrTransaction'
                        impactedMessageBuildingBlocks='_groupHeader _transactionInformation'/>
                    <messageDefinitionIdentifier businessArea='pacs'
                        messageFunctionality='008' flavour='001' version='10'/>
                  </messageDefinition>
                </topLevelCatalogueEntry>
                <topLevelCatalogueEntry xsi:type='iso:MessageSet' xmi:id='_set'
                    name='PaymentsMessageSet' definition='Payment messages.'
                    registrationStatus='Registered' generatedSyntax='_syntax'
                    validEncoding='_encoding' messageDefinition='_message'/>
                <topLevelCatalogueEntry xsi:type='iso:Syntax' xmi:id='_syntax'
                    name='XML' possibleEncodings='_encoding' generatedFor='_set'/>
                <topLevelCatalogueEntry xsi:type='iso:Encoding' xmi:id='_encoding'
                    name='UTF-8 XML' syntax='_syntax' messageSet='_set'/>
                <topLevelCatalogueEntry xsi:type='iso:SyntaxMessageScheme' xmi:id='_scheme'
                    name='Pacs008Scheme' registrationStatus='Registered'
                    messageDefinitionTrace='_message'/>
                <topLevelCatalogueEntry xsi:type='iso:MessageChoreography'
                    xmi:id='_choreography' name='CreditTransferChoreography'/>
                <topLevelCatalogueEntry xsi:type='iso:BusinessTransaction' xmi:id='_transaction'
                    name='CreditTransferTransaction'>
                  <transmission xmi:id='_transmission' name='CreditTransferTransmission'/>
                </topLevelCatalogueEntry>
              </businessProcessCatalogue>
            </iso:Repository>
            """;
    }
}
