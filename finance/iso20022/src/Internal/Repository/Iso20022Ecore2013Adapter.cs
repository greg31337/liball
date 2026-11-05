using System;

namespace iso20022;

internal class Iso20022Ecore2013Adapter
{
    internal string GetAdapterIdentifier()
    {
        return "iso20022.ecore.2013";
    }

    internal string GetMetamodelNamespace()
    {
        return Iso20022Namespaces.Ecore2013;
    }

    internal string GetMetamodelVersion()
    {
        return "2013";
    }

    internal string NormalizeTypeName(string qualifiedTypeName)
    {
        if (qualifiedTypeName == null)
        {
            throw new ArgumentNullException("qualifiedTypeName");
        }

        int fragmentIndex = qualifiedTypeName.LastIndexOf("#//", StringComparison.Ordinal);
        if (fragmentIndex >= 0)
        {
            return qualifiedTypeName.Substring(fragmentIndex + 3);
        }

        int colonIndex = qualifiedTypeName.LastIndexOf(':');
        if (colonIndex >= 0)
        {
            return qualifiedTypeName.Substring(colonIndex + 1);
        }

        return qualifiedTypeName;
    }

    internal RepositoryEntityKind ResolveEntityKind(string qualifiedTypeName)
    {
        string typeName = NormalizeTypeName(qualifiedTypeName);
        if (typeName == "Repository")
        {
            return RepositoryEntityKind.Repository;
        }

        if (typeName == "DataDictionary")
        {
            return RepositoryEntityKind.DataDictionary;
        }

        if (typeName == "BusinessProcessCatalogue")
        {
            return RepositoryEntityKind.BusinessProcessCatalogue;
        }

        if (typeName == "BusinessArea")
        {
            return RepositoryEntityKind.BusinessArea;
        }

        if (typeName == "BusinessProcess")
        {
            return RepositoryEntityKind.BusinessProcess;
        }

        if (typeName == "BusinessRole")
        {
            return RepositoryEntityKind.BusinessRole;
        }

        if (typeName == "Participant")
        {
            return RepositoryEntityKind.Participant;
        }

        if (typeName == "BusinessTransaction")
        {
            return RepositoryEntityKind.BusinessTransaction;
        }

        if (typeName == "Conversation")
        {
            return RepositoryEntityKind.Conversation;
        }

        if (typeName == "MessageChoreography")
        {
            return RepositoryEntityKind.MessageChoreography;
        }

        if (typeName == "MessageTransmission" || typeName == "Send" || typeName == "Receive")
        {
            return RepositoryEntityKind.MessageTransmission;
        }

        if (typeName == "MessagingEndpoint" || typeName == "EndPointCategory")
        {
            return RepositoryEntityKind.MessagingEndpoint;
        }

        if (typeName == "BroadcastList")
        {
            return RepositoryEntityKind.BroadcastList;
        }

        if (typeName == "MessageTransportSystem")
        {
            return RepositoryEntityKind.MessageTransportSystem;
        }

        if (typeName == "MessageTransportMode")
        {
            return RepositoryEntityKind.TransportMode;
        }

        if (typeName == "Address")
        {
            return RepositoryEntityKind.Address;
        }

        if (typeName == "TransportMessage" || typeName == "MessageInstance")
        {
            return RepositoryEntityKind.TransportMessage;
        }

        if (typeName == "BusinessComponent")
        {
            return RepositoryEntityKind.BusinessComponent;
        }

        if (typeName == "BusinessAttribute")
        {
            return RepositoryEntityKind.BusinessAttribute;
        }

        if (typeName == "BusinessAssociationEnd")
        {
            return RepositoryEntityKind.BusinessAssociationEnd;
        }

        if (typeName == "BusinessElement")
        {
            return RepositoryEntityKind.BusinessElement;
        }

        if (typeName == "MessageSet" || typeName == "IndustryMessageSet" ||
            typeName == "ISO15022MessageSet")
        {
            return RepositoryEntityKind.MessageSet;
        }

        if (typeName == "MessageDefinition" || typeName == "MessageDefinitionIdentifier")
        {
            return RepositoryEntityKind.MessageDefinition;
        }

        if (typeName == "MessageBuildingBlock")
        {
            return RepositoryEntityKind.MessageBuildingBlock;
        }

        if (typeName == "MessageComponent")
        {
            return RepositoryEntityKind.MessageComponent;
        }

        if (typeName == "MessageAttribute")
        {
            return RepositoryEntityKind.MessageAttribute;
        }

        if (typeName == "MessageAssociationEnd")
        {
            return RepositoryEntityKind.MessageAssociationEnd;
        }

        if (typeName == "MessageElement")
        {
            return RepositoryEntityKind.MessageElement;
        }

        if (typeName == "ChoiceComponent" || typeName == "Xor")
        {
            return RepositoryEntityKind.Choice;
        }

        if (IsDataTypeName(typeName))
        {
            return RepositoryEntityKind.DataType;
        }

        if (typeName == "CodeSet")
        {
            return RepositoryEntityKind.CodeSet;
        }

        if (typeName == "Code")
        {
            return RepositoryEntityKind.CodeValue;
        }

        if (typeName == "Syntax")
        {
            return RepositoryEntityKind.Syntax;
        }

        if (typeName == "Encoding")
        {
            return RepositoryEntityKind.Encoding;
        }

        if (typeName == "ExternalSchema" || typeName == "UserDefined")
        {
            return RepositoryEntityKind.ExternalSchema;
        }

        if (typeName == "SyntaxMessageScheme")
        {
            return RepositoryEntityKind.SyntaxMessageScheme;
        }

        if (typeName == "Constraint")
        {
            return RepositoryEntityKind.Constraint;
        }

        if (typeName == "SemanticMarkup" || typeName == "SemanticMarkupElement")
        {
            return RepositoryEntityKind.SemanticMarkup;
        }

        if (typeName == "Doclet")
        {
            return RepositoryEntityKind.Documentation;
        }

        if (typeName == "ConvergenceDocumentation")
        {
            return RepositoryEntityKind.Convergence;
        }

        return RepositoryEntityKind.Unknown;
    }

    internal bool IsKnownType(string qualifiedTypeName)
    {
        string typeName = NormalizeTypeName(qualifiedTypeName);
        if (ResolveEntityKind(typeName) != RepositoryEntityKind.Unknown)
        {
            return true;
        }

        return typeName == "ModelEntity" ||
            typeName == "TopLevelCatalogueEntry" ||
            typeName == "RepositoryConcept" ||
            typeName == "TopLevelDictionaryEntry" ||
            typeName == "RepositoryType" ||
            typeName == "MessageElement" ||
            typeName == "MessageConstruct" ||
            typeName == "Construct" ||
            typeName == "MultiplicityEntity" ||
            typeName == "LogicalType" ||
            typeName == "MessageConcept" ||
            typeName == "BusinessElementType" ||
            typeName == "BusinessConcept" ||
            typeName == "MessageComponentType" ||
            typeName == "DataType" ||
            typeName == "MessageElementContainer" ||
            typeName == "AbstractDateTimeConcept";
    }

    internal string ResolveContainedType(string parentTypeName, string elementName, string qualifiedXsiTypeName)
    {
        if (qualifiedXsiTypeName.Length > 0)
        {
            return NormalizeTypeName(qualifiedXsiTypeName);
        }

        string parent = NormalizeTypeName(parentTypeName);
        if (parent == "Repository" && elementName == "dataDictionary")
        {
            return "DataDictionary";
        }

        if (parent == "Repository" && elementName == "businessProcessCatalogue")
        {
            return "BusinessProcessCatalogue";
        }

        if (elementName == "semanticMarkup")
        {
            return "SemanticMarkup";
        }

        if (elementName == "doclet")
        {
            return "Doclet";
        }

        if (elementName == "constraint")
        {
            return "Constraint";
        }

        if (parent == "SemanticMarkup" && elementName == "elements")
        {
            return "SemanticMarkupElement";
        }

        if (parent == "BusinessArea" && elementName == "messageDefinition")
        {
            return "MessageDefinition";
        }

        if (parent == "MessageDefinition" && elementName == "messageBuildingBlock")
        {
            return "MessageBuildingBlock";
        }

        if (parent == "MessageDefinition" && elementName == "messageDefinitionIdentifier")
        {
            return "MessageDefinitionIdentifier";
        }

        if ((parent == "MessageDefinition" || parent == "MessageComponent") && elementName == "xors")
        {
            return "Xor";
        }

        if (parent == "BusinessProcess" && elementName == "businessRole")
        {
            return "BusinessRole";
        }

        if (parent == "BusinessTransaction" && elementName == "participant")
        {
            return "Participant";
        }

        if (parent == "BusinessTransaction" && elementName == "transmission")
        {
            return "MessageTransmission";
        }

        if (parent == "MessageTransmission" && elementName == "send")
        {
            return "Send";
        }

        if (parent == "MessageTransmission" && elementName == "receive")
        {
            return "Receive";
        }

        if (parent == "MessageTransportSystem" && elementName == "endpoint")
        {
            return "MessagingEndpoint";
        }

        if (parent == "CodeSet" && elementName == "code")
        {
            return "Code";
        }

        return string.Empty;
    }

    internal bool IsContainmentFeature(string parentTypeName, string elementName)
    {
        string parent = NormalizeTypeName(parentTypeName);
        if (parent == "Repository" &&
            (elementName == "dataDictionary" || elementName == "businessProcessCatalogue"))
        {
            return true;
        }

        if (parent == "DataDictionary" && elementName == "topLevelDictionaryEntry")
        {
            return true;
        }

        if (parent == "BusinessProcessCatalogue" && elementName == "topLevelCatalogueEntry")
        {
            return true;
        }

        if (elementName == "semanticMarkup" || elementName == "doclet" || elementName == "constraint")
        {
            return true;
        }

        if (parent == "SemanticMarkup" && elementName == "elements")
        {
            return true;
        }

        if (parent == "BusinessArea" && elementName == "messageDefinition")
        {
            return true;
        }

        if (parent == "MessageDefinition" &&
            (elementName == "xors" || elementName == "messageBuildingBlock" ||
            elementName == "messageDefinitionIdentifier"))
        {
            return true;
        }

        if (parent == "BusinessComponent" && elementName == "element")
        {
            return true;
        }

        if ((parent == "MessageComponent" || parent == "ChoiceComponent") && elementName == "messageElement")
        {
            return true;
        }

        if (parent == "MessageComponent" && elementName == "xors")
        {
            return true;
        }

        if (parent == "BusinessProcess" && elementName == "businessRole")
        {
            return true;
        }

        if (parent == "BusinessTransaction" &&
            (elementName == "participant" || elementName == "transmission"))
        {
            return true;
        }

        if (parent == "MessageTransmission" && (elementName == "send" || elementName == "receive"))
        {
            return true;
        }

        if (parent == "MessageTransportSystem" && elementName == "endpoint")
        {
            return true;
        }

        return parent == "CodeSet" && elementName == "code";
    }

    internal bool IsReferenceFeature(string parentTypeName, string featureName)
    {
        string parent = NormalizeTypeName(parentTypeName);
        if (featureName == "type")
        {
            return parent == "BusinessAssociationEnd" || parent == "MessageAssociationEnd";
        }

        if (featureName == "businessArea")
        {
            return parent == "MessageDefinition";
        }

        if (featureName == "code")
        {
            return parent == "CodeSet";
        }

        return featureName == "address" || featureName == "associationDomain" ||
            featureName == "broadCastList" || featureName == "businessComponentTrace" ||
            featureName == "businessElementTrace" || featureName == "businessElementType" ||
            featureName == "businessProcess" || featureName == "businessProcessCatalogue" ||
            featureName == "businessProcessTrace" || featureName == "businessRole" ||
            featureName == "businessRoleTrace" || featureName == "businessTransaction" ||
            featureName == "businessTransactionTrace" || featureName == "choreography" ||
            featureName == "complexType" || featureName == "componentContext" ||
            featureName == "constraint" ||
            featureName == "currencyIdentifierSet" || featureName == "dataDictionary" ||
            featureName == "derivation" || featureName == "derivationComponent" ||
            featureName == "derivationElement" || featureName == "doclet" ||
            featureName == "element" || featureName == "elementContext" ||
            featureName == "elements" ||
            featureName == "endPoints" || featureName == "endpoint" ||
            featureName == "extended" || featureName == "extender" ||
            featureName == "generatedFor" || featureName == "generatedSyntax" ||
            featureName == "impactedElements" ||
            featureName == "impactedMessageBuildingBlocks" || featureName == "included" ||
            featureName == "includer" || featureName == "location" ||
            featureName == "memberType" || featureName == "messageBuildingBlock" ||
            featureName == "messageComponent" || featureName == "messageDefinition" ||
            featureName == "messageDefinitionIdentifier" ||
            featureName == "messageDefinitionTrace" || featureName == "messageElement" ||
            featureName == "messageInstance" || featureName == "messageSet" ||
            featureName == "messageTransmission" || featureName == "messageTransportMode" ||
            featureName == "nextVersions" || featureName == "opposite" ||
            featureName == "owner" || featureName == "parentTransaction" ||
            featureName == "participant" || featureName == "possibleEncodings" ||
            featureName == "previousVersion" || featureName == "receive" ||
            featureName == "receivedMessage" || featureName == "receiver" ||
            featureName == "receives" || featureName == "repository" ||
            featureName == "semanticMarkup" || featureName == "send" ||
            featureName == "sender" || featureName == "sends" ||
            featureName == "sentMessage" || featureName == "simpleType" ||
            featureName == "specification" || featureName == "subTransaction" ||
            featureName == "subType" || featureName == "superType" ||
            featureName == "syntax" || featureName == "topLevelCatalogueEntry" ||
            featureName == "topLevelDictionaryEntry" || featureName == "trace" ||
            featureName == "transmission" || featureName == "transportMessage" ||
            featureName == "transportSystem" || featureName == "validEncoding" ||
            featureName == "xmlMemberType" || featureName == "xors";
    }

    internal bool IsElementAttributeFeature(string parentTypeName, string elementName)
    {
        if (elementName == "example")
        {
            return true;
        }

        string parent = NormalizeTypeName(parentTypeName);
        return elementName == "namespaceList" &&
            (parent == "ExternalSchema" || parent == "UserDefined");
    }

    internal bool IsKnownElementFeature(string parentTypeName, string elementName)
    {
        return IsContainmentFeature(parentTypeName, elementName) ||
            IsElementAttributeFeature(parentTypeName, elementName);
    }

    internal bool IsKnownScalarAttributeFeature(string parentTypeName, string featureName)
    {
        string typeName = NormalizeTypeName(parentTypeName);
        if (featureName == "objectIdentifier" && IsKnownType(typeName))
        {
            return true;
        }

        if (IsRepositoryConceptType(typeName) &&
            (featureName == "name" || featureName == "definition" ||
            featureName == "example" || featureName == "registrationStatus" ||
            featureName == "removalDate"))
        {
            return true;
        }

        if (IsMultiplicityEntityType(typeName) &&
            (featureName == "minOccurs" || featureName == "maxOccurs"))
        {
            return true;
        }

        if ((typeName == "SemanticMarkup" || typeName == "Doclet") &&
            featureName == "type")
        {
            return true;
        }

        if (typeName == "SemanticMarkupElement" &&
            (featureName == "name" || featureName == "value"))
        {
            return true;
        }

        if (typeName == "Doclet" && featureName == "content")
        {
            return true;
        }

        if (typeName == "Constraint" &&
            (featureName == "expression" || featureName == "expressionLanguage"))
        {
            return true;
        }

        if (typeName == "MessageDefinition" &&
            (featureName == "xmlName" || featureName == "xmlTag" ||
            featureName == "rootElement"))
        {
            return true;
        }

        if (typeName == "BusinessArea" && featureName == "code")
        {
            return true;
        }

        if (IsMessageConstructType(typeName) && featureName == "xmlTag")
        {
            return true;
        }

        if (IsMessageElementType(typeName) &&
            (featureName == "isTechnical" || featureName == "isDerived"))
        {
            return true;
        }

        if (IsMessageComponentTypeName(typeName) && featureName == "isTechnical")
        {
            return true;
        }

        if (IsBusinessElementTypeName(typeName) && featureName == "isDerived")
        {
            return true;
        }

        if (typeName == "BusinessAssociationEnd" && featureName == "aggregation")
        {
            return true;
        }

        if (typeName == "MessageTransmission" && featureName == "messageTypeDescription")
        {
            return true;
        }

        if (typeName == "MessageAssociationEnd" && featureName == "isComposite")
        {
            return true;
        }

        if (typeName == "MessageDefinitionIdentifier" &&
            (featureName == "businessArea" || featureName == "messageFunctionality" ||
            featureName == "flavour" || featureName == "version"))
        {
            return true;
        }

        if (typeName == "MessageTransportMode" && IsTransportModeAttribute(featureName))
        {
            return true;
        }

        if (IsStringDataType(typeName) && IsStringFacetAttribute(featureName))
        {
            return true;
        }

        if ((typeName == "IdentifierSet" || typeName == "CodeSet") &&
            featureName == "identificationScheme")
        {
            return true;
        }

        if ((typeName == "Boolean" || typeName == "Indicator") && featureName == "pattern")
        {
            return true;
        }

        if (typeName == "Indicator" &&
            (featureName == "meaningWhenTrue" || featureName == "meaningWhenFalse"))
        {
            return true;
        }

        if (IsDecimalDataType(typeName) && IsDecimalFacetAttribute(featureName))
        {
            return true;
        }

        if (typeName == "Rate" &&
            (featureName == "baseValue" || featureName == "baseUnitCode"))
        {
            return true;
        }

        if (typeName == "Quantity" && featureName == "unitCode")
        {
            return true;
        }

        if (IsDateTimeDataType(typeName) && IsDateTimeFacetAttribute(featureName))
        {
            return true;
        }

        if (typeName == "Binary" && IsStringFacetAttribute(featureName))
        {
            return true;
        }

        if (typeName == "ExternalSchema" &&
            (featureName == "namespaceList" || featureName == "processContent"))
        {
            return true;
        }

        if (typeName == "UserDefined" &&
            (featureName == "namespace" || featureName == "namespaceList" ||
            featureName == "processContents"))
        {
            return true;
        }

        if (typeName == "SchemaType" && featureName == "kind")
        {
            return true;
        }

        return typeName == "Code" && featureName == "codeName";
    }

    internal bool IsRepositoryConceptType(string typeName)
    {
        return typeName == "RepositoryConcept" || typeName == "TopLevelCatalogueEntry" ||
            typeName == "TopLevelDictionaryEntry" || typeName == "RepositoryType" ||
            typeName == "Construct" || typeName == "LogicalType" ||
            typeName == "BusinessElementType" || typeName == "MessageComponentType" ||
            typeName == "MessageElementContainer" || typeName == "DataType" ||
            typeName == "String" || typeName == "Decimal" ||
            typeName == "AbstractDateTimeConcept" || typeName == "Constraint" ||
            typeName == "SyntaxMessageScheme" || typeName == "MessageSet" ||
            typeName == "IndustryMessageSet" || typeName == "ISO15022MessageSet" ||
            typeName == "BusinessArea" || typeName == "Xor" ||
            typeName == "MessageDefinition" || typeName == "BusinessComponent" ||
            typeName == "BusinessElement" || typeName == "BusinessAttribute" ||
            typeName == "BusinessAssociationEnd" || typeName == "MessageComponent" ||
            typeName == "ChoiceComponent" || typeName == "MessageBuildingBlock" ||
            typeName == "MessageElement" || typeName == "MessageAttribute" ||
            typeName == "MessageAssociationEnd" || typeName == "Text" ||
            typeName == "IdentifierSet" || typeName == "Indicator" ||
            typeName == "Boolean" || typeName == "Rate" || typeName == "Quantity" ||
            typeName == "Code" || typeName == "CodeSet" || typeName == "Amount" ||
            typeName == "Binary" || typeName == "Date" || typeName == "DateTime" ||
            typeName == "Day" || typeName == "Duration" || typeName == "Month" ||
            typeName == "MonthDay" || typeName == "Time" || typeName == "Year" ||
            typeName == "YearMonth" || typeName == "ExternalSchema" ||
            typeName == "UserDefined" || typeName == "SchemaType" ||
            typeName == "EndPointCategory" || typeName == "MessageChoreography" ||
            typeName == "BusinessTransaction" || typeName == "BusinessProcess" ||
            typeName == "BusinessRole" || typeName == "Participant" ||
            typeName == "MessageTransmission" || typeName == "MessageTransportMode" ||
            typeName == "ConvergenceDocumentation";
    }

    internal bool IsMultiplicityEntityType(string typeName)
    {
        return typeName == "MultiplicityEntity" || typeName == "Construct" ||
            typeName == "MessageConstruct" || typeName == "MessageBuildingBlock" ||
            IsMessageElementType(typeName) || IsBusinessElementTypeName(typeName) ||
            typeName == "Participant";
    }

    internal bool IsMessageConstructType(string typeName)
    {
        return typeName == "MessageConstruct" || typeName == "MessageBuildingBlock" ||
            IsMessageElementType(typeName);
    }

    internal bool IsMessageElementType(string typeName)
    {
        return typeName == "MessageElement" || typeName == "MessageAttribute" ||
            typeName == "MessageAssociationEnd";
    }

    internal bool IsMessageComponentTypeName(string typeName)
    {
        return typeName == "MessageComponentType" || typeName == "MessageElementContainer" ||
            typeName == "MessageComponent" || typeName == "ChoiceComponent" ||
            typeName == "ExternalSchema" || typeName == "UserDefined";
    }

    internal bool IsBusinessElementTypeName(string typeName)
    {
        return typeName == "BusinessElement" || typeName == "BusinessAttribute" ||
            typeName == "BusinessAssociationEnd";
    }

    internal bool IsTransportModeAttribute(string featureName)
    {
        return featureName == "boundedCommunicationDelay" ||
            featureName == "maximumClockVariation" || featureName == "maximumMessageSize" ||
            featureName == "messageDeliveryWindow" || featureName == "messageSendingWindow" ||
            featureName == "deliveryAssurance" || featureName == "durability" ||
            featureName == "messageCasting" || featureName == "messageDeliveryOrder" ||
            featureName == "messageValidationLevel" ||
            featureName == "messageValidationOnOff" ||
            featureName == "messageValidationResults" ||
            featureName == "receiverAsynchronicity" ||
            featureName == "senderAsynchronicity";
    }

    internal bool IsStringDataType(string typeName)
    {
        return typeName == "String" || typeName == "Text" ||
            typeName == "IdentifierSet" || typeName == "CodeSet";
    }

    internal bool IsStringFacetAttribute(string featureName)
    {
        return featureName == "minLength" || featureName == "maxLength" ||
            featureName == "length" || featureName == "pattern";
    }

    internal bool IsDecimalDataType(string typeName)
    {
        return typeName == "Decimal" || typeName == "Rate" ||
            typeName == "Quantity" || typeName == "Amount";
    }

    internal bool IsDecimalFacetAttribute(string featureName)
    {
        return featureName == "minInclusive" || featureName == "minExclusive" ||
            featureName == "maxInclusive" || featureName == "maxExclusive" ||
            featureName == "totalDigits" || featureName == "fractionDigits" ||
            featureName == "pattern";
    }

    internal bool IsDateTimeDataType(string typeName)
    {
        return typeName == "AbstractDateTimeConcept" || typeName == "Date" ||
            typeName == "DateTime" || typeName == "Day" || typeName == "Duration" ||
            typeName == "Month" || typeName == "MonthDay" || typeName == "Time" ||
            typeName == "Year" || typeName == "YearMonth";
    }

    internal bool IsDateTimeFacetAttribute(string featureName)
    {
        return featureName == "minInclusive" || featureName == "minExclusive" ||
            featureName == "maxInclusive" || featureName == "maxExclusive" ||
            featureName == "pattern";
    }

    internal bool IsTopLevelPartitionType(string qualifiedTypeName)
    {
        string typeName = NormalizeTypeName(qualifiedTypeName);
        return typeName == "DataDictionary" || typeName == "BusinessProcessCatalogue";
    }

    internal bool IsDataTypeName(string typeName)
    {
        return typeName == "Text" || typeName == "String" || typeName == "IdentifierSet" ||
            typeName == "Indicator" || typeName == "Boolean" || typeName == "Rate" ||
            typeName == "Decimal" || typeName == "Quantity" || typeName == "Amount" ||
            typeName == "Binary" || typeName == "Date" || typeName == "DateTime" ||
            typeName == "Day" || typeName == "Duration" || typeName == "Month" ||
            typeName == "MonthDay" || typeName == "Time" || typeName == "Year" ||
            typeName == "YearMonth" || typeName == "SchemaType";
    }
}
