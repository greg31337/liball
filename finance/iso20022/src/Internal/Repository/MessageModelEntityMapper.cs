using System.Collections.Generic;

namespace iso20022;

internal class MessageModelEntityMapper
{
    DataDictionaryImportContext context;
    DataDictionaryEntityMapper common;

    internal MessageModelEntityMapper(DataDictionaryImportContext context)
    {
        this.context = context;
        common = new DataDictionaryEntityMapper(context);
    }

    internal MessageSetDefinition BuildMessageSet(XmiPassTwoEntity entity)
    {
        MessageSetDefinition definition = new MessageSetDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        common.AppendDirectReferences(entity, "generatedSyntax", definition.GeneratedSyntaxes);
        common.AppendDirectReferences(entity, "validEncoding", definition.ValidEncodings);
        common.AppendDirectReferences(entity, "messageDefinition", definition.MessageDefinitions);
        definition.Location = entity.Location;
        return definition;
    }

    internal MessageDefinitionMetadata BuildMessageDefinition(XmiPassTwoEntity entity)
    {
        MessageDefinitionMetadata definition = new MessageDefinitionMetadata();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.XmlName = common.FindAttributeValue(entity, "xmlName");
        definition.XmlTag = common.FindAttributeValue(entity, "xmlTag");
        string rootElement = common.FindAttributeValue(entity, "rootElement");
        if (rootElement.Length > 0)
        {
            definition.RootElement = rootElement;
        }

        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessArea = FindBusinessArea(entity);
        common.AppendDirectReferences(entity, "messageSet", definition.MessageSets);
        common.AppendDirectReferences(entity, "messageBuildingBlock", definition.BuildingBlocks);
        common.AppendDirectReferences(entity, "xors", definition.Choices);
        common.AppendDirectReferences(entity, "choreography", definition.Choreographies);
        common.AppendDirectReferences(entity, "trace", definition.MessageTransmissions);
        common.AppendDirectReferences(entity, "derivation", definition.SyntaxMessageSchemes);
        PopulateMessageIdentifier(entity, definition);
        definition.Location = entity.Location;
        return definition;
    }

    internal RepositoryReference FindBusinessArea(XmiPassTwoEntity entity)
    {
        RepositoryReference? direct = common.CopyFirstReference(entity, "businessArea");
        if (direct != null)
        {
            return direct;
        }

        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent != null && parent.TypeName == "BusinessArea")
        {
            return context.CreateParentReference(entity, parent, "businessArea");
        }

        return new RepositoryReference();
    }

    internal void PopulateMessageIdentifier(
        XmiPassTwoEntity entity,
        MessageDefinitionMetadata definition)
    {
        List<XmiPassTwoEntity> children = context.GetChildren(entity);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            XmiPassTwoEntity child = children[childIndex];
            if (child.TypeName == "MessageDefinitionIdentifier")
            {
                MessageIdentifier identifier = new MessageIdentifier();
                identifier.BusinessArea = common.FindAttributeValue(child, "businessArea");
                identifier.MessageFunctionality =
                    common.FindAttributeValue(child, "messageFunctionality");
                identifier.Variant = common.FindAttributeValue(child, "flavour");
                identifier.Version = common.FindAttributeValue(child, "version");
                if (identifier.BusinessArea.Length > 0 &&
                    identifier.MessageFunctionality.Length > 0 &&
                    identifier.Variant.Length > 0 &&
                    identifier.Version.Length > 0)
                {
                    identifier.Value = $"{identifier.BusinessArea}." +
                        $"{identifier.MessageFunctionality}.{identifier.Variant}." +
                        identifier.Version;
                    definition.NamespaceUri =
                        Iso20022Namespaces.SchemaUrnPrefix + identifier.Value;
                }

                definition.MessageIdentifier = identifier;
                return;
            }

            childIndex++;
        }
    }

    internal MessageBuildingBlockDefinition BuildMessageBuildingBlock(XmiPassTwoEntity entity)
    {
        MessageBuildingBlockDefinition definition = new MessageBuildingBlockDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.XmlTag = common.FindAttributeValue(entity, "xmlTag");
        definition.Registration = common.BuildRegistration(entity);
        definition.Multiplicity = common.BuildMultiplicity(entity);
        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent != null)
        {
            definition.MessageDefinition =
                context.CreateParentReference(entity, parent, "messageDefinition");
        }

        definition.SimpleType = common.CopyFirstReference(entity, "simpleType");
        definition.ComplexType = common.CopyFirstReference(entity, "complexType");
        RepositoryReference? memberType = common.CopyFirstReference(entity, "xmlMemberType");
        if (memberType == null)
        {
            memberType = definition.SimpleType;
        }

        if (memberType == null)
        {
            memberType = definition.ComplexType;
        }

        if (memberType != null)
        {
            definition.MemberType = memberType;
        }

        definition.Location = entity.Location;
        return definition;
    }

    internal MessageComponentDefinition BuildMessageComponent(XmiPassTwoEntity entity)
    {
        MessageComponentDefinition definition = new MessageComponentDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.DataDictionary = context.CreatePartitionReference(entity, "dataDictionary");
        definition.IsTechnical = common.ReadBooleanAttribute(entity, "isTechnical");
        definition.BusinessComponentTrace = common.CopyFirstReference(entity, "trace");
        common.AppendDirectReferences(
            entity,
            "messageBuildingBlock",
            definition.MessageBuildingBlocks);
        common.AppendInverseReferences(
            entity,
            RepositoryEntityKind.MessageBuildingBlock,
            RepositoryEntityKind.MessageBuildingBlock,
            "complexType",
            "messageBuildingBlock",
            definition.MessageBuildingBlocks);
        common.AppendDirectReferences(entity, "messageElement", definition.MessageElements);
        common.AppendDirectReferences(entity, "xors", definition.Choices);
        definition.Location = entity.Location;
        return definition;
    }

    internal MessageElementDefinition BuildMessageElement(XmiPassTwoEntity entity)
    {
        MessageElementDefinition definition = new MessageElementDefinition();
        definition.Identifier = entity.Identifier;
        if (entity.TypeName == "MessageAttribute")
        {
            definition.Kind = MessageElementKind.Attribute;
        }

        if (entity.TypeName == "MessageAssociationEnd")
        {
            definition.Kind = MessageElementKind.AssociationEnd;
        }

        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.XmlTag = common.FindAttributeValue(entity, "xmlTag");
        definition.Registration = common.BuildRegistration(entity);
        definition.Multiplicity = common.BuildMultiplicity(entity);
        definition.IsTechnical = common.ReadBooleanAttribute(entity, "isTechnical");
        definition.IsDerived = common.ReadBooleanAttribute(entity, "isDerived");
        definition.IsComposite = common.ReadBooleanAttribute(entity, "isComposite");
        definition.ComponentContext = FindComponentContext(entity);
        definition.SimpleType = common.CopyFirstReference(entity, "simpleType");
        definition.ComplexType = common.CopyFirstReference(entity, "complexType");
        if (definition.ComplexType == null &&
            entity.TypeName == "MessageAssociationEnd")
        {
            definition.ComplexType = common.CopyFirstReference(entity, "type");
        }

        RepositoryReference? memberType = common.CopyFirstReference(entity, "xmlMemberType");
        if (memberType == null)
        {
            memberType = definition.SimpleType;
        }

        if (memberType == null)
        {
            memberType = definition.ComplexType;
        }

        if (memberType != null)
        {
            definition.MemberType = memberType;
        }

        definition.BusinessComponentTrace =
            common.CopyFirstReference(entity, "businessComponentTrace");
        definition.BusinessElementTrace =
            common.CopyFirstReference(entity, "businessElementTrace");
        definition.Location = entity.Location;
        return definition;
    }

    internal RepositoryReference FindComponentContext(XmiPassTwoEntity entity)
    {
        RepositoryReference? direct = common.CopyFirstReference(entity, "componentContext");
        if (direct != null)
        {
            return direct;
        }

        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent != null)
        {
            return context.CreateParentReference(entity, parent, "componentContext");
        }

        return new RepositoryReference();
    }

    internal ChoiceDefinition BuildChoice(XmiPassTwoEntity entity)
    {
        ChoiceDefinition definition = new ChoiceDefinition();
        definition.Identifier = entity.Identifier;
        definition.ChoiceType = entity.TypeName;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        if (entity.TypeName == "ChoiceComponent")
        {
            definition.DataDictionary = context.CreatePartitionReference(entity, "dataDictionary");
            definition.IsTechnical = common.ReadBooleanAttribute(entity, "isTechnical");
            definition.BusinessComponentTrace = common.CopyFirstReference(entity, "trace");
            common.AppendDirectReferences(entity, "messageElement", definition.MessageElements);
        }

        if (entity.TypeName == "Xor")
        {
            common.AppendDirectReferences(
                entity,
                "impactedElements",
                definition.ImpactedMessageElements);
            common.AppendDirectReferences(
                entity,
                "impactedMessageBuildingBlocks",
                definition.ImpactedBuildingBlocks);
            definition.OwnerMessageComponent =
                common.CopyFirstReference(entity, "messageComponent");
            definition.OwnerMessageDefinition =
                common.CopyFirstReference(entity, "messageDefinition");
            PopulateChoiceOwnerFromParent(entity, definition);
        }

        definition.Location = entity.Location;
        return definition;
    }

    internal void PopulateChoiceOwnerFromParent(
        XmiPassTwoEntity entity,
        ChoiceDefinition definition)
    {
        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent == null)
        {
            return;
        }

        if (definition.OwnerMessageComponent == null &&
            parent.TypeName == "MessageComponent")
        {
            definition.OwnerMessageComponent =
                context.CreateParentReference(entity, parent, "messageComponent");
        }

        if (definition.OwnerMessageDefinition == null &&
            parent.TypeName == "MessageDefinition")
        {
            definition.OwnerMessageDefinition =
                context.CreateParentReference(entity, parent, "messageDefinition");
        }
    }

    internal SyntaxDefinition BuildSyntax(XmiPassTwoEntity entity)
    {
        SyntaxDefinition definition = new SyntaxDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        common.AppendDirectReferences(entity, "possibleEncodings", definition.PossibleEncodings);
        common.AppendDirectReferences(entity, "generatedFor", definition.GeneratedForMessageSets);
        definition.Location = entity.Location;
        return definition;
    }

    internal EncodingDefinition BuildEncoding(XmiPassTwoEntity entity)
    {
        EncodingDefinition definition = new EncodingDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        RepositoryReference? syntax = common.CopyFirstReference(entity, "syntax");
        if (syntax != null)
        {
            definition.Syntax = syntax;
        }

        common.AppendDirectReferences(entity, "messageSet", definition.MessageSets);
        definition.Location = entity.Location;
        return definition;
    }

    internal SyntaxMessageSchemeDefinition BuildSyntaxMessageScheme(XmiPassTwoEntity entity)
    {
        SyntaxMessageSchemeDefinition definition = new SyntaxMessageSchemeDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = common.FindAttributeValue(entity, "name");
        definition.Definition = common.FindAttributeValue(entity, "definition");
        definition.Registration = common.BuildRegistration(entity);
        definition.BusinessProcessCatalogue =
            context.CreatePartitionReference(entity, "businessProcessCatalogue");
        RepositoryReference? messageDefinition =
            common.CopyFirstReference(entity, "messageDefinitionTrace");
        if (messageDefinition != null)
        {
            definition.MessageDefinitionTrace = messageDefinition;
        }

        definition.Syntax = common.CopyFirstReference(entity, "syntax");
        definition.Encoding = common.CopyFirstReference(entity, "encoding");
        definition.Location = entity.Location;
        return definition;
    }
}
