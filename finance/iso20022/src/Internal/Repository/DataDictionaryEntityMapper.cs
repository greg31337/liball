using System;
using System.Collections.Generic;

namespace iso20022;

internal class DataDictionaryEntityMapper
{
    DataDictionaryImportContext context;

    internal DataDictionaryEntityMapper(DataDictionaryImportContext context)
    {
        this.context = context;
    }

    internal BusinessComponentDefinition BuildBusinessComponent(XmiPassTwoEntity entity)
    {
        BusinessComponentDefinition definition = new BusinessComponentDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.Registration = BuildRegistration(entity);
        definition.DataDictionary = context.CreatePartitionReference(entity, "dataDictionary");
        definition.SuperType = CopyFirstReference(entity, "superType");
        AppendDirectReferences(entity, "subType", definition.SubTypes);
        AppendDirectReferences(entity, "element", definition.Elements);
        AppendDirectReferences(entity, "derivationComponent", definition.DerivedMessageComponents);
        AppendDirectReferences(entity, "associationDomain", definition.AssociationDomains);
        AppendDirectReferences(entity, "derivationElement", definition.DerivedMessageElements);
        definition.Location = entity.Location;
        return definition;
    }

    internal BusinessElementDefinition BuildBusinessElement(XmiPassTwoEntity entity)
    {
        BusinessElementDefinition definition = new BusinessElementDefinition();
        definition.Identifier = entity.Identifier;
        if (entity.Kind == RepositoryEntityKind.BusinessAttribute)
        {
            definition.Kind = MessageElementKind.Attribute;
        }

        if (entity.Kind == RepositoryEntityKind.BusinessAssociationEnd)
        {
            definition.Kind = MessageElementKind.AssociationEnd;
        }

        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.Registration = BuildRegistration(entity);
        definition.Multiplicity = BuildMultiplicity(entity);
        definition.IsDerived = ReadBooleanAttribute(entity, "isDerived");
        XmiPassTwoEntity? parent = context.FindParent(entity);
        if (parent != null)
        {
            definition.ElementContext = context.CreateParentReference(entity, parent, "elementContext");
        }

        definition.SimpleType = CopyFirstReference(entity, "simpleType");
        definition.ComplexType = CopyFirstReference(entity, "complexType");
        definition.Opposite = CopyFirstReference(entity, "opposite");
        definition.Aggregation = FindAttributeValue(entity, "aggregation");
        RepositoryReference? memberType = CopyFirstReference(entity, "memberType");
        if (memberType == null)
        {
            memberType = definition.SimpleType;
        }

        if (memberType == null)
        {
            memberType = definition.ComplexType;
        }

        if (memberType == null)
        {
            memberType = CopyFirstReference(entity, "type");
        }

        if (memberType != null)
        {
            definition.MemberType = memberType;
            if (definition.ComplexType == null && entity.Kind == RepositoryEntityKind.BusinessAssociationEnd)
            {
                definition.ComplexType = memberType;
            }
        }

        AppendDirectReferences(entity, "derivation", definition.DerivedMessageElements);
        definition.Location = entity.Location;
        return definition;
    }

    internal DataTypeDefinition BuildDataType(XmiPassTwoEntity entity)
    {
        DataTypeDefinition definition = new DataTypeDefinition();
        definition.Identifier = entity.Identifier;
        definition.Kind = MapDataTypeKind(entity.TypeName);
        definition.SourceTypeName = entity.TypeName;
        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.Registration = BuildRegistration(entity);
        definition.DataDictionary = context.CreatePartitionReference(entity, "dataDictionary");
        AppendFacets(entity, definition.Facets);
        definition.IdentificationScheme = FindAttributeValue(entity, "identificationScheme");
        definition.MeaningWhenTrue = FindAttributeValue(entity, "meaningWhenTrue");
        definition.MeaningWhenFalse = FindAttributeValue(entity, "meaningWhenFalse");
        definition.BaseValue = DecimalLexical.ParseDecimal(
            FindAttributeValue(entity, "baseValue").AsSpan());
        definition.UnitCode = FindAttributeValue(entity, "unitCode");
        if (definition.UnitCode.Length == 0)
        {
            definition.UnitCode = FindAttributeValue(entity, "baseUnitCode");
        }

        definition.CurrencyIdentifierSet = CopyFirstReference(entity, "currencyIdentifierSet");
        definition.SchemaTypeName = FindAttributeValue(entity, "kind");
        AppendInverseReferences(
            entity,
            RepositoryEntityKind.BusinessAttribute,
            RepositoryEntityKind.BusinessElement,
            "simpleType",
            "businessElement",
            definition.BusinessElements);
        AppendInverseReferences(
            entity,
            RepositoryEntityKind.MessageAttribute,
            RepositoryEntityKind.MessageBuildingBlock,
            "simpleType",
            "messageMember",
            definition.MessageMembers);
        definition.Location = entity.Location;
        return definition;
    }

    internal CodeSetDefinition BuildCodeSet(XmiPassTwoEntity entity)
    {
        CodeSetDefinition definition = new CodeSetDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.IdentificationScheme = FindAttributeValue(entity, "identificationScheme");
        definition.Registration = BuildRegistration(entity);
        definition.DataDictionary = context.CreatePartitionReference(entity, "dataDictionary");
        AppendFacets(entity, definition.Facets);
        definition.BaseCodeSet = CopyFirstReference(entity, "trace");
        AppendDirectReferences(entity, "derivation", definition.DerivedCodeSets);
        if (definition.BaseCodeSet != null)
        {
            definition.Kind = CodeSetKind.Derived;
        }
        else
        {
            definition.Kind = CodeSetKind.Internal;
        }

        List<XmiPassTwoEntity> children = context.GetChildren(entity);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            XmiPassTwoEntity child = children[childIndex];
            if (child.Kind == RepositoryEntityKind.CodeValue)
            {
                definition.Codes.Add(BuildCodeValue(child, entity));
            }

            childIndex++;
        }

        if (definition.BaseCodeSet == null &&
            definition.Codes.Count == 0 &&
            definition.IdentificationScheme.Length > 0)
        {
            definition.Kind = CodeSetKind.External;
        }

        definition.Location = entity.Location;
        return definition;
    }

    internal CodeValueDefinition BuildCodeValue(
        XmiPassTwoEntity entity,
        XmiPassTwoEntity owner)
    {
        CodeValueDefinition definition = new CodeValueDefinition();
        definition.Identifier = entity.Identifier;
        definition.Value = FindAttributeValue(entity, "codeName");
        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.Registration = BuildRegistration(entity);
        definition.Owner = context.CreateParentReference(entity, owner, "owner");
        definition.Location = entity.Location;
        return definition;
    }

    internal ExternalSchemaDefinition BuildExternalSchema(XmiPassTwoEntity entity)
    {
        ExternalSchemaDefinition definition = new ExternalSchemaDefinition();
        definition.Identifier = entity.Identifier;
        definition.SourceTypeName = entity.TypeName;
        if (entity.TypeName == "ExternalSchema")
        {
            definition.Kind = DataTypeKind.ExternalSchema;
        }

        if (entity.TypeName == "UserDefined")
        {
            definition.Kind = DataTypeKind.UserDefined;
        }

        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.Registration = BuildRegistration(entity);
        definition.DataDictionary = context.CreatePartitionReference(entity, "dataDictionary");
        definition.NamespaceMode = FindAttributeValue(entity, "namespace");
        AppendAttributeValues(entity, "namespaceList", definition.NamespaceUris);
        definition.ProcessContents = FindAttributeValue(entity, "processContents");
        if (definition.ProcessContents.Length == 0)
        {
            definition.ProcessContents = FindAttributeValue(entity, "processContent");
        }

        definition.IsTechnical = ReadBooleanAttribute(entity, "isTechnical");
        definition.BusinessComponentTrace = CopyFirstReference(entity, "trace");
        AppendDirectReferences(entity, "messageBuildingBlock", definition.MessageBuildingBlocks);
        definition.Location = entity.Location;
        return definition;
    }

    internal RegistrationMetadata BuildRegistration(XmiPassTwoEntity entity)
    {
        RegistrationMetadata registration = new RegistrationMetadata();
        registration.StatusValue = FindAttributeValue(entity, "registrationStatus");
        registration.Status = MapRegistrationStatus(registration.StatusValue);
        string removalDate = FindAttributeValue(entity, "removalDate");
        if (removalDate.Length > 0)
        {
            registration.RemovalDate = DateTimeLexical.ParseDate(removalDate.AsSpan());
        }

        registration.PreviousVersion = CopyFirstReference(entity, "previousVersion");
        AppendDirectReferences(entity, "nextVersions", registration.NextVersions);
        AppendAttributeValues(entity, "example", registration.Examples);
        List<XmiPassTwoEntity> children = context.GetChildren(entity);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            XmiPassTwoEntity child = children[childIndex];
            if (child.TypeName == "SemanticMarkup")
            {
                registration.SemanticMarkup.Add(BuildSemanticMarkup(child));
            }

            if (child.TypeName == "Doclet")
            {
                registration.Doclets.Add(BuildDoclet(child));
            }

            if (child.TypeName == "Constraint")
            {
                registration.Constraints.Add(BuildConstraint(child));
            }

            childIndex++;
        }

        return registration;
    }

    internal ConstraintDefinition BuildConstraint(XmiPassTwoEntity entity)
    {
        ConstraintDefinition definition = new ConstraintDefinition();
        definition.Identifier = entity.Identifier;
        definition.Name = FindAttributeValue(entity, "name");
        definition.Definition = FindAttributeValue(entity, "definition");
        definition.Expression = FindAttributeValue(entity, "expression");
        definition.ExpressionLanguage = FindAttributeValue(entity, "expressionLanguage");
        definition.Registration = BuildRegistration(entity);
        XmiPassTwoReference? ownerReference = context.FindFirstReference(entity, "owner");
        if (ownerReference != null)
        {
            definition.Owner = context.CopyReference(ownerReference);
        }
        else
        {
            XmiPassTwoEntity? parent = context.FindParent(entity);
            if (parent != null)
            {
                definition.Owner = context.CreateParentReference(entity, parent, "owner");
            }
        }

        int markupIndex = 0;
        while (markupIndex < definition.Registration.SemanticMarkup.Count)
        {
            definition.SemanticMarkup.Add(definition.Registration.SemanticMarkup[markupIndex]);
            markupIndex++;
        }

        definition.Location = entity.Location;
        return definition;
    }

    internal SemanticMarkupData BuildSemanticMarkup(XmiPassTwoEntity entity)
    {
        SemanticMarkupData markup = new SemanticMarkupData();
        markup.Identifier = entity.Identifier;
        markup.Type = FindAttributeValue(entity, "type");
        markup.Name = FindAttributeValue(entity, "name");
        markup.Value = FindAttributeValue(entity, "value");
        List<XmiPassTwoEntity> children = context.GetChildren(entity);
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            XmiPassTwoEntity child = children[childIndex];
            if (child.TypeName == "SemanticMarkupElement")
            {
                markup.Elements.Add(BuildSemanticMarkup(child));
            }

            childIndex++;
        }

        markup.Location = entity.Location;
        return markup;
    }

    internal SemanticMarkupData BuildDoclet(XmiPassTwoEntity entity)
    {
        SemanticMarkupData doclet = new SemanticMarkupData();
        doclet.Identifier = entity.Identifier;
        doclet.Type = FindAttributeValue(entity, "type");
        doclet.Value = FindAttributeValue(entity, "content");
        doclet.Location = entity.Location;
        return doclet;
    }

    internal MultiplicityDefinition BuildMultiplicity(XmiPassTwoEntity entity)
    {
        MultiplicityDefinition multiplicity = new MultiplicityDefinition();
        multiplicity.MinimumOccurs = ReadIntegerAttribute(entity, "minOccurs");
        string maximumValue = FindAttributeValue(entity, "maxOccurs");
        if (maximumValue.Length > 0)
        {
            IntegerLexicalResult maximum = IntegerLexical.ParseInt32(maximumValue.AsSpan());
            if (maximum.Success && maximum.Value >= 0)
            {
                multiplicity.HasMaximumOccurs = true;
                multiplicity.MaximumOccurs = maximum.Value;
            }
        }

        multiplicity.Location = entity.Location;
        return multiplicity;
    }

    internal void AppendFacets(XmiPassTwoEntity entity, List<DataTypeFacet> destination)
    {
        int attributeIndex = 0;
        while (attributeIndex < entity.Attributes.Count)
        {
            XmiPassTwoAttribute attribute = entity.Attributes[attributeIndex];
            if (IsFacetName(attribute.LocalName))
            {
                destination.Add(BuildFacet(entity, attribute));
            }

            attributeIndex++;
        }
    }

    internal DataTypeFacet BuildFacet(
        XmiPassTwoEntity entity,
        XmiPassTwoAttribute attribute)
    {
        DataTypeFacet facet = new DataTypeFacet();
        facet.Name = attribute.LocalName;
        facet.Value = attribute.Value;
        if (attribute.LocalName == "length" || attribute.LocalName == "minLength" ||
            attribute.LocalName == "maxLength" || attribute.LocalName == "totalDigits" ||
            attribute.LocalName == "fractionDigits")
        {
            IntegerLexicalResult integer = IntegerLexical.ParseInt32(attribute.Value.AsSpan());
            if (integer.Success)
            {
                facet.HasIntegerValue = true;
                facet.IntegerValue = integer.Value;
            }
        }

        if (attribute.LocalName == "minInclusive" || attribute.LocalName == "minExclusive" ||
            attribute.LocalName == "maxInclusive" || attribute.LocalName == "maxExclusive")
        {
            facet.DecimalValue = DecimalLexical.ParseDecimal(attribute.Value.AsSpan());
        }

        facet.Location = context.CreateAttributeLocation(entity, attribute);
        return facet;
    }

    internal bool IsFacetName(string attributeName)
    {
        return attributeName == "length" || attributeName == "minLength" ||
            attributeName == "maxLength" || attributeName == "pattern" ||
            attributeName == "minInclusive" || attributeName == "minExclusive" ||
            attributeName == "maxInclusive" || attributeName == "maxExclusive" ||
            attributeName == "totalDigits" || attributeName == "fractionDigits";
    }

    internal DataTypeKind MapDataTypeKind(string typeName)
    {
        if (typeName == "Text")
        {
            return DataTypeKind.Text;
        }

        if (typeName == "String")
        {
            return DataTypeKind.String;
        }

        if (typeName == "IdentifierSet")
        {
            return DataTypeKind.Identifier;
        }

        if (typeName == "Boolean")
        {
            return DataTypeKind.Boolean;
        }

        if (typeName == "Indicator")
        {
            return DataTypeKind.Indicator;
        }

        if (typeName == "Decimal")
        {
            return DataTypeKind.Decimal;
        }

        if (typeName == "Amount")
        {
            return DataTypeKind.Amount;
        }

        if (typeName == "Quantity")
        {
            return DataTypeKind.Quantity;
        }

        if (typeName == "Rate")
        {
            return DataTypeKind.Rate;
        }

        if (typeName == "Binary")
        {
            return DataTypeKind.Binary;
        }

        if (typeName == "Date")
        {
            return DataTypeKind.Date;
        }

        if (typeName == "DateTime")
        {
            return DataTypeKind.DateTime;
        }

        if (typeName == "Day")
        {
            return DataTypeKind.Day;
        }

        if (typeName == "Duration")
        {
            return DataTypeKind.Duration;
        }

        if (typeName == "Month")
        {
            return DataTypeKind.Month;
        }

        if (typeName == "MonthDay")
        {
            return DataTypeKind.MonthDay;
        }

        if (typeName == "Time")
        {
            return DataTypeKind.Time;
        }

        if (typeName == "Year")
        {
            return DataTypeKind.Year;
        }

        if (typeName == "YearMonth")
        {
            return DataTypeKind.YearMonth;
        }

        if (typeName == "SchemaType")
        {
            return DataTypeKind.Schema;
        }

        return DataTypeKind.Unknown;
    }

    internal RegistrationStatus MapRegistrationStatus(string statusValue)
    {
        if (statusValue == "Provisionally Registered")
        {
            return RegistrationStatus.ProvisionallyRegistered;
        }

        if (statusValue == "Registered")
        {
            return RegistrationStatus.Registered;
        }

        if (statusValue == "Obsolete")
        {
            return RegistrationStatus.Obsolete;
        }

        return RegistrationStatus.Unknown;
    }

    internal string FindAttributeValue(XmiPassTwoEntity entity, string attributeName)
    {
        int attributeIndex = 0;
        while (attributeIndex < entity.Attributes.Count)
        {
            XmiPassTwoAttribute attribute = entity.Attributes[attributeIndex];
            if (attribute.LocalName == attributeName)
            {
                return attribute.Value;
            }

            attributeIndex++;
        }

        return string.Empty;
    }

    internal void AppendAttributeValues(
        XmiPassTwoEntity entity,
        string attributeName,
        List<string> destination)
    {
        int attributeIndex = 0;
        while (attributeIndex < entity.Attributes.Count)
        {
            XmiPassTwoAttribute attribute = entity.Attributes[attributeIndex];
            if (attribute.LocalName == attributeName)
            {
                destination.Add(attribute.Value);
            }

            attributeIndex++;
        }
    }

    internal int ReadIntegerAttribute(XmiPassTwoEntity entity, string attributeName)
    {
        IntegerLexicalResult result = IntegerLexical.ParseInt32(
            FindAttributeValue(entity, attributeName).AsSpan());
        if (result.Success)
        {
            return result.Value;
        }

        return 0;
    }

    internal bool ReadBooleanAttribute(XmiPassTwoEntity entity, string attributeName)
    {
        IsoBooleanValue? result = BooleanLexical.ParseBoolean(
            FindAttributeValue(entity, attributeName).AsSpan());
        return result != null && result.Value;
    }

    internal RepositoryReference? CopyFirstReference(
        XmiPassTwoEntity entity,
        string propertyName)
    {
        XmiPassTwoReference? reference = context.FindFirstReference(entity, propertyName);
        if (reference == null)
        {
            return null;
        }

        return context.CopyReference(reference);
    }

    internal void AppendDirectReferences(
        XmiPassTwoEntity entity,
        string propertyName,
        List<RepositoryReference> destination)
    {
        List<XmiPassTwoReference> references = context.GetSourceReferences(entity);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            XmiPassTwoReference reference = references[referenceIndex];
            if (reference.PropertyName == propertyName)
            {
                destination.Add(context.CopyReference(reference));
            }

            referenceIndex++;
        }
    }

    internal void AppendInverseReferences(
        XmiPassTwoEntity entity,
        RepositoryEntityKind firstSourceKind,
        RepositoryEntityKind secondSourceKind,
        string sourcePropertyName,
        string destinationPropertyName,
        List<RepositoryReference> destination)
    {
        List<XmiPassTwoReference> references = context.GetTargetReferences(entity.Identifier);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            XmiPassTwoReference reference = references[referenceIndex];
            if (reference.PropertyName == sourcePropertyName &&
                (reference.SourceKind == firstSourceKind || reference.SourceKind == secondSourceKind))
            {
                destination.Add(context.CreateReverseReference(
                    entity,
                    reference,
                    destinationPropertyName));
            }

            referenceIndex++;
        }
    }
}
