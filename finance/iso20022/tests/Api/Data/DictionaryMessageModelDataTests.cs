using iso20022;

namespace iso20022.Tests;

public class DictionaryMessageModelDataTests
{
    [Test]
    public void SnapshotRetainsEveryDictionaryAndMessagePartitionInCallerOrder()
    {
        BusinessComponentDefinition component = new BusinessComponentDefinition { Identifier = "_businessComponent" };
        BusinessElementDefinition businessElement = new BusinessElementDefinition { Identifier = "_businessElement" };
        MessageSetDefinition messageSet = new MessageSetDefinition { Identifier = "_messageSet" };
        MessageDefinitionMetadata messageDefinition = new MessageDefinitionMetadata { Identifier = "_messageDefinition" };
        MessageBuildingBlockDefinition buildingBlock = new MessageBuildingBlockDefinition { Identifier = "_buildingBlock" };
        MessageComponentDefinition messageComponent = new MessageComponentDefinition { Identifier = "_messageComponent" };
        MessageElementDefinition messageElement = new MessageElementDefinition { Identifier = "_messageElement" };
        ChoiceDefinition choice = new ChoiceDefinition { Identifier = "_choice" };
        RepositorySnapshot snapshot = new RepositorySnapshot();
        snapshot.BusinessComponents.Add(component);
        snapshot.BusinessElements.Add(businessElement);
        snapshot.MessageSets.Add(messageSet);
        snapshot.MessageDefinitions.Add(messageDefinition);
        snapshot.MessageBuildingBlocks.Add(buildingBlock);
        snapshot.MessageComponents.Add(messageComponent);
        snapshot.MessageElements.Add(messageElement);
        snapshot.Choices.Add(choice);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.BusinessComponents.Single(), Is.SameAs(component));
            Assert.That(snapshot.BusinessElements.Single(), Is.SameAs(businessElement));
            Assert.That(snapshot.MessageSets.Single(), Is.SameAs(messageSet));
            Assert.That(snapshot.MessageDefinitions.Single(), Is.SameAs(messageDefinition));
            Assert.That(snapshot.MessageBuildingBlocks.Single(), Is.SameAs(buildingBlock));
            Assert.That(snapshot.MessageComponents.Single(), Is.SameAs(messageComponent));
            Assert.That(snapshot.MessageElements.Single(), Is.SameAs(messageElement));
            Assert.That(snapshot.Choices.Single(), Is.SameAs(choice));
        });
    }

    [Test]
    public void BusinessComponentsAndElementsRetainInheritanceAssociationsAndDerivations()
    {
        BusinessComponentDefinition component = new BusinessComponentDefinition
        {
            Identifier = "_account",
            Name = "CashAccount",
            Definition = "Account used to post cash entries.",
            Registration = new RegistrationMetadata { Status = RegistrationStatus.Registered },
            DataDictionary = Reference("_account", RepositoryEntityKind.BusinessComponent,
                "dataDictionary", "_dictionary", RepositoryEntityKind.DataDictionary, 0),
            SuperType = Reference("_account", RepositoryEntityKind.BusinessComponent,
                "superType", "_baseAccount", RepositoryEntityKind.BusinessComponent, 0)
        };
        component.SubTypes.Add(Reference("_account", RepositoryEntityKind.BusinessComponent,
            "subType", "_settlementAccount", RepositoryEntityKind.BusinessComponent, 0));
        component.Elements.Add(Reference("_account", RepositoryEntityKind.BusinessComponent,
            "element", "_currency", RepositoryEntityKind.BusinessAttribute, 0));
        component.Elements.Add(Reference("_account", RepositoryEntityKind.BusinessComponent,
            "element", "_owner", RepositoryEntityKind.BusinessAssociationEnd, 1));
        component.DerivedMessageComponents.Add(Reference("_account", RepositoryEntityKind.BusinessComponent,
            "derivationComponent", "_cashAccountComponent", RepositoryEntityKind.MessageComponent, 0));
        component.AssociationDomains.Add(Reference("_account", RepositoryEntityKind.BusinessComponent,
            "associationDomain", "_relatedAccount", RepositoryEntityKind.BusinessAssociationEnd, 0));
        component.DerivedMessageElements.Add(Reference("_account", RepositoryEntityKind.BusinessComponent,
            "derivationElement", "_accountElement", RepositoryEntityKind.MessageAssociationEnd, 0));

        BusinessElementDefinition attribute = new BusinessElementDefinition
        {
            Identifier = "_currency",
            Kind = MessageElementKind.Attribute,
            Name = "Currency",
            Multiplicity = new MultiplicityDefinition
            {
                MinimumOccurs = 1,
                HasMaximumOccurs = true,
                MaximumOccurs = 1
            },
            ElementContext = Reference("_currency", RepositoryEntityKind.BusinessAttribute,
                "elementContext", "_account", RepositoryEntityKind.BusinessComponent, 0),
            MemberType = Reference("_currency", RepositoryEntityKind.BusinessAttribute,
                "businessElementType", "_currencyCode", RepositoryEntityKind.CodeSet, 0),
            SimpleType = Reference("_currency", RepositoryEntityKind.BusinessAttribute,
                "simpleType", "_currencyCode", RepositoryEntityKind.CodeSet, 0)
        };
        attribute.DerivedMessageElements.Add(Reference("_currency", RepositoryEntityKind.BusinessAttribute,
            "derivation", "_currencyElement", RepositoryEntityKind.MessageAttribute, 0));

        BusinessElementDefinition association = new BusinessElementDefinition
        {
            Identifier = "_owner",
            Kind = MessageElementKind.AssociationEnd,
            Name = "AccountOwner",
            Aggregation = "COMPOSITE",
            ElementContext = Reference("_owner", RepositoryEntityKind.BusinessAssociationEnd,
                "elementContext", "_account", RepositoryEntityKind.BusinessComponent, 0),
            MemberType = Reference("_owner", RepositoryEntityKind.BusinessAssociationEnd,
                "businessElementType", "_party", RepositoryEntityKind.BusinessComponent, 0),
            ComplexType = Reference("_owner", RepositoryEntityKind.BusinessAssociationEnd,
                "type", "_party", RepositoryEntityKind.BusinessComponent, 0),
            Opposite = Reference("_owner", RepositoryEntityKind.BusinessAssociationEnd,
                "opposite", "_accounts", RepositoryEntityKind.BusinessAssociationEnd, 0)
        };

        Assert.Multiple(() =>
        {
            Assert.That(component.SuperType?.TargetIdentifier, Is.EqualTo("_baseAccount"));
            Assert.That(component.SubTypes.Single().TargetIdentifier, Is.EqualTo("_settlementAccount"));
            Assert.That(component.Elements.Select(item => item.TargetIdentifier),
                Is.EqualTo(new[] { "_currency", "_owner" }));
            Assert.That(component.DerivedMessageComponents.Single().TargetIdentifier, Is.EqualTo("_cashAccountComponent"));
            Assert.That(component.AssociationDomains.Single().TargetIdentifier, Is.EqualTo("_relatedAccount"));
            Assert.That(component.DerivedMessageElements.Single().TargetIdentifier, Is.EqualTo("_accountElement"));
            Assert.That(attribute.Kind, Is.EqualTo(MessageElementKind.Attribute));
            Assert.That(attribute.Multiplicity.MinimumOccurs, Is.EqualTo(1));
            Assert.That(attribute.SimpleType?.TargetKind, Is.EqualTo(RepositoryEntityKind.CodeSet));
            Assert.That(attribute.DerivedMessageElements.Single().TargetIdentifier, Is.EqualTo("_currencyElement"));
            Assert.That(association.Kind, Is.EqualTo(MessageElementKind.AssociationEnd));
            Assert.That(association.Aggregation, Is.EqualTo("COMPOSITE"));
            Assert.That(association.ComplexType?.TargetIdentifier, Is.EqualTo("_party"));
            Assert.That(association.Opposite?.TargetIdentifier, Is.EqualTo("_accounts"));
        });
    }

    [Test]
    public void MessageSetDefinitionAndBuildingBlocksRetainExactIdentityRootsAndTypes()
    {
        MessageSetDefinition set = new MessageSetDefinition
        {
            Identifier = "_set",
            Name = "Payments Clearing and Settlement",
            BusinessProcessCatalogue = Reference("_set", RepositoryEntityKind.MessageSet,
                "businessProcessCatalogue", "_catalogue", RepositoryEntityKind.BusinessProcessCatalogue, 0)
        };
        set.GeneratedSyntaxes.Add(Reference("_set", RepositoryEntityKind.MessageSet,
            "generatedSyntax", "_xmlSyntax", RepositoryEntityKind.Syntax, 0));
        set.ValidEncodings.Add(Reference("_set", RepositoryEntityKind.MessageSet,
            "validEncoding", "_xmlEncoding", RepositoryEntityKind.Encoding, 0));
        set.MessageDefinitions.Add(Reference("_set", RepositoryEntityKind.MessageSet,
            "messageDefinition", "_pacs008", RepositoryEntityKind.MessageDefinition, 0));

        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = "_pacs008",
            Name = "FIToFICustomerCreditTransferV10",
            MessageIdentifier = new MessageIdentifier
            {
                BusinessArea = "pacs",
                MessageFunctionality = "008",
                Variant = "001",
                Version = "10",
                Value = "pacs.008.001.10"
            },
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
            XmlName = "FIToFICustomerCreditTransferV10",
            XmlTag = "FIToFICstmrCdtTrf",
            RootElement = "Document",
            BusinessArea = Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
                "businessArea", "_pacs", RepositoryEntityKind.BusinessArea, 0)
        };
        message.MessageSets.Add(Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
            "messageSet", "_set", RepositoryEntityKind.MessageSet, 0));
        message.BuildingBlocks.Add(Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
            "messageBuildingBlock", "_groupHeader", RepositoryEntityKind.MessageBuildingBlock, 0));
        message.BuildingBlocks.Add(Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
            "messageBuildingBlock", "_creditTransfer", RepositoryEntityKind.MessageBuildingBlock, 1));
        message.Choreographies.Add(Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
            "choreography", "_choreography", RepositoryEntityKind.MessageChoreography, 0));
        message.MessageTransmissions.Add(Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
            "trace", "_transmission", RepositoryEntityKind.MessageTransmission, 0));
        message.SyntaxMessageSchemes.Add(Reference("_pacs008", RepositoryEntityKind.MessageDefinition,
            "derivation", "_scheme", RepositoryEntityKind.SyntaxMessageScheme, 0));

        MessageBuildingBlockDefinition block = new MessageBuildingBlockDefinition
        {
            Identifier = "_groupHeader",
            Name = "GroupHeader",
            XmlTag = "GrpHdr",
            Multiplicity = new MultiplicityDefinition
            {
                MinimumOccurs = 1,
                HasMaximumOccurs = true,
                MaximumOccurs = 1
            },
            MessageDefinition = Reference("_groupHeader", RepositoryEntityKind.MessageBuildingBlock,
                "messageDefinition", "_pacs008", RepositoryEntityKind.MessageDefinition, 0),
            MemberType = Reference("_groupHeader", RepositoryEntityKind.MessageBuildingBlock,
                "xmlMemberType", "_groupHeaderComponent", RepositoryEntityKind.MessageComponent, 0),
            ComplexType = Reference("_groupHeader", RepositoryEntityKind.MessageBuildingBlock,
                "complexType", "_groupHeaderComponent", RepositoryEntityKind.MessageComponent, 0)
        };

        Assert.Multiple(() =>
        {
            Assert.That(set.GeneratedSyntaxes.Single().TargetIdentifier, Is.EqualTo("_xmlSyntax"));
            Assert.That(set.ValidEncodings.Single().TargetIdentifier, Is.EqualTo("_xmlEncoding"));
            Assert.That(message.MessageIdentifier.Value, Is.EqualTo("pacs.008.001.10"));
            Assert.That(message.NamespaceUri, Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(message.RootElement, Is.EqualTo("Document"));
            Assert.That(message.BuildingBlocks.Select(item => item.TargetIdentifier),
                Is.EqualTo(new[] { "_groupHeader", "_creditTransfer" }));
            Assert.That(message.Choreographies.Single().TargetIdentifier, Is.EqualTo("_choreography"));
            Assert.That(message.MessageTransmissions.Single().TargetIdentifier, Is.EqualTo("_transmission"));
            Assert.That(message.SyntaxMessageSchemes.Single().TargetIdentifier, Is.EqualTo("_scheme"));
            Assert.That(block.XmlTag, Is.EqualTo("GrpHdr"));
            Assert.That(block.ComplexType?.TargetIdentifier, Is.EqualTo("_groupHeaderComponent"));
            Assert.That(block.Multiplicity.MaximumOccurs, Is.EqualTo(1));
        });
    }

    [Test]
    public void MessageComponentsElementsAndChoicesRetainTypingAndTraceSemantics()
    {
        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = "_component",
            Name = "PaymentIdentification",
            IsTechnical = false,
            BusinessComponentTrace = Reference("_component", RepositoryEntityKind.MessageComponent,
                "trace", "_payment", RepositoryEntityKind.BusinessComponent, 0)
        };
        component.MessageBuildingBlocks.Add(Reference("_component", RepositoryEntityKind.MessageComponent,
            "messageBuildingBlock", "_paymentIdBlock", RepositoryEntityKind.MessageBuildingBlock, 0));
        component.MessageElements.Add(Reference("_component", RepositoryEntityKind.MessageComponent,
            "messageElement", "_instructionId", RepositoryEntityKind.MessageAttribute, 0));
        component.MessageElements.Add(Reference("_component", RepositoryEntityKind.MessageComponent,
            "messageElement", "_debtor", RepositoryEntityKind.MessageAssociationEnd, 1));

        MessageElementDefinition attribute = new MessageElementDefinition
        {
            Identifier = "_instructionId",
            Kind = MessageElementKind.Attribute,
            Name = "InstructionIdentification",
            XmlTag = "InstrId",
            ComponentContext = Reference("_instructionId", RepositoryEntityKind.MessageAttribute,
                "componentContext", "_component", RepositoryEntityKind.MessageComponent, 0),
            MemberType = Reference("_instructionId", RepositoryEntityKind.MessageAttribute,
                "xmlMemberType", "_textType", RepositoryEntityKind.DataType, 0),
            SimpleType = Reference("_instructionId", RepositoryEntityKind.MessageAttribute,
                "simpleType", "_textType", RepositoryEntityKind.DataType, 0),
            BusinessElementTrace = Reference("_instructionId", RepositoryEntityKind.MessageAttribute,
                "businessElementTrace", "_businessInstructionId", RepositoryEntityKind.BusinessAttribute, 0)
        };
        MessageElementDefinition association = new MessageElementDefinition
        {
            Identifier = "_debtor",
            Kind = MessageElementKind.AssociationEnd,
            Name = "Debtor",
            IsComposite = true,
            ComponentContext = attribute.ComponentContext,
            MemberType = Reference("_debtor", RepositoryEntityKind.MessageAssociationEnd,
                "xmlMemberType", "_partyComponent", RepositoryEntityKind.MessageComponent, 0),
            ComplexType = Reference("_debtor", RepositoryEntityKind.MessageAssociationEnd,
                "type", "_partyComponent", RepositoryEntityKind.MessageComponent, 0),
            BusinessComponentTrace = Reference("_debtor", RepositoryEntityKind.MessageAssociationEnd,
                "businessComponentTrace", "_party", RepositoryEntityKind.BusinessComponent, 0)
        };
        association.Derivations.Add(Reference("_debtor", RepositoryEntityKind.MessageAssociationEnd,
            "futureDerivation", "_semanticDomain", RepositoryEntityKind.Unknown, 0));

        ChoiceDefinition reusableChoice = new ChoiceDefinition
        {
            Identifier = "_choiceComponent",
            ChoiceType = "ChoiceComponent",
            Name = "PartyChoice",
            IsTechnical = false,
            BusinessComponentTrace = Reference("_choiceComponent", RepositoryEntityKind.Choice,
                "trace", "_party", RepositoryEntityKind.BusinessComponent, 0)
        };
        reusableChoice.MessageElements.Add(Reference("_choiceComponent", RepositoryEntityKind.Choice,
            "messageElement", "_organisation", RepositoryEntityKind.MessageAssociationEnd, 0));
        reusableChoice.MessageElements.Add(Reference("_choiceComponent", RepositoryEntityKind.Choice,
            "messageElement", "_person", RepositoryEntityKind.MessageAssociationEnd, 1));

        ChoiceDefinition xor = new ChoiceDefinition
        {
            Identifier = "_xor",
            ChoiceType = "Xor",
            Name = "PartyOrAccount",
            OwnerMessageComponent = Reference("_xor", RepositoryEntityKind.Choice,
                "messageComponent", "_component", RepositoryEntityKind.MessageComponent, 0),
            OwnerMessageDefinition = Reference("_xor", RepositoryEntityKind.Choice,
                "messageDefinition", "_message", RepositoryEntityKind.MessageDefinition, 0)
        };
        xor.ImpactedMessageElements.Add(component.MessageElements[0]);
        xor.ImpactedMessageElements.Add(component.MessageElements[1]);
        xor.ImpactedBuildingBlocks.Add(Reference("_xor", RepositoryEntityKind.Choice,
            "impactedMessageBuildingBlocks", "_paymentIdBlock", RepositoryEntityKind.MessageBuildingBlock, 0));
        component.Choices.Add(Reference("_component", RepositoryEntityKind.MessageComponent,
            "xors", "_xor", RepositoryEntityKind.Choice, 0));

        Assert.Multiple(() =>
        {
            Assert.That(component.BusinessComponentTrace?.TargetIdentifier, Is.EqualTo("_payment"));
            Assert.That(component.MessageElements.Select(item => item.TargetIdentifier),
                Is.EqualTo(new[] { "_instructionId", "_debtor" }));
            Assert.That(attribute.SimpleType?.TargetIdentifier, Is.EqualTo("_textType"));
            Assert.That(attribute.BusinessElementTrace?.TargetIdentifier, Is.EqualTo("_businessInstructionId"));
            Assert.That(association.IsComposite, Is.True);
            Assert.That(association.ComplexType?.TargetIdentifier, Is.EqualTo("_partyComponent"));
            Assert.That(association.BusinessComponentTrace?.TargetIdentifier, Is.EqualTo("_party"));
            Assert.That(association.Derivations.Single().PropertyName, Is.EqualTo("futureDerivation"));
            Assert.That(reusableChoice.ChoiceType, Is.EqualTo("ChoiceComponent"));
            Assert.That(reusableChoice.MessageElements.Select(item => item.TargetIdentifier),
                Is.EqualTo(new[] { "_organisation", "_person" }));
            Assert.That(xor.ChoiceType, Is.EqualTo("Xor"));
            Assert.That(xor.ImpactedMessageElements, Has.Count.EqualTo(2));
            Assert.That(xor.ImpactedBuildingBlocks.Single().TargetIdentifier, Is.EqualTo("_paymentIdBlock"));
            Assert.That(xor.OwnerMessageComponent?.TargetIdentifier, Is.EqualTo("_component"));
            Assert.That(xor.OwnerMessageDefinition?.TargetIdentifier, Is.EqualTo("_message"));
        });
    }

    [Test]
    public void MultiplicitiesAndMutableDefaultsRemainIndependent()
    {
        MultiplicityDefinition unbounded = new MultiplicityDefinition
        {
            MinimumOccurs = 0,
            HasMaximumOccurs = false
        };
        MultiplicityDefinition finite = new MultiplicityDefinition
        {
            MinimumOccurs = 1,
            HasMaximumOccurs = true,
            MaximumOccurs = 4
        };
        BusinessComponentDefinition firstBusinessComponent = new BusinessComponentDefinition();
        BusinessComponentDefinition secondBusinessComponent = new BusinessComponentDefinition();
        BusinessElementDefinition firstBusinessElement = new BusinessElementDefinition();
        BusinessElementDefinition secondBusinessElement = new BusinessElementDefinition();
        MessageSetDefinition firstSet = new MessageSetDefinition();
        MessageSetDefinition secondSet = new MessageSetDefinition();
        MessageDefinitionMetadata firstMessage = new MessageDefinitionMetadata();
        MessageDefinitionMetadata secondMessage = new MessageDefinitionMetadata();
        MessageBuildingBlockDefinition firstBlock = new MessageBuildingBlockDefinition();
        MessageBuildingBlockDefinition secondBlock = new MessageBuildingBlockDefinition();
        MessageComponentDefinition firstMessageComponent = new MessageComponentDefinition();
        MessageComponentDefinition secondMessageComponent = new MessageComponentDefinition();
        MessageElementDefinition firstMessageElement = new MessageElementDefinition();
        MessageElementDefinition secondMessageElement = new MessageElementDefinition();
        ChoiceDefinition firstChoice = new ChoiceDefinition();
        ChoiceDefinition secondChoice = new ChoiceDefinition();

        firstBusinessComponent.Elements.Add(new RepositoryReference());
        firstBusinessElement.DerivedMessageElements.Add(new RepositoryReference());
        firstSet.MessageDefinitions.Add(new RepositoryReference());
        firstMessage.BuildingBlocks.Add(new RepositoryReference());
        firstBlock.UnknownContent.Add(new RepositoryUnknownData());
        firstMessageComponent.MessageElements.Add(new RepositoryReference());
        firstMessageElement.Derivations.Add(new RepositoryReference());
        firstChoice.MessageElements.Add(new RepositoryReference());

        Assert.Multiple(() =>
        {
            Assert.That(unbounded.MinimumOccurs, Is.Zero);
            Assert.That(unbounded.HasMaximumOccurs, Is.False);
            Assert.That(finite.MinimumOccurs, Is.EqualTo(1));
            Assert.That(finite.MaximumOccurs, Is.EqualTo(4));
            Assert.That(secondBusinessComponent.Elements, Is.Empty);
            Assert.That(secondBusinessElement.DerivedMessageElements, Is.Empty);
            Assert.That(secondSet.MessageDefinitions, Is.Empty);
            Assert.That(secondMessage.BuildingBlocks, Is.Empty);
            Assert.That(secondBlock.UnknownContent, Is.Empty);
            Assert.That(secondMessageComponent.MessageElements, Is.Empty);
            Assert.That(secondMessageElement.Derivations, Is.Empty);
            Assert.That(secondChoice.MessageElements, Is.Empty);
            Assert.That(firstBusinessElement.Multiplicity, Is.Not.SameAs(secondBusinessElement.Multiplicity));
            Assert.That(firstBlock.Multiplicity, Is.Not.SameAs(secondBlock.Multiplicity));
            Assert.That(firstMessageElement.Multiplicity, Is.Not.SameAs(secondMessageElement.Multiplicity));
            Assert.That(firstMessage.MessageIdentifier, Is.Not.SameAs(secondMessage.MessageIdentifier));
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
