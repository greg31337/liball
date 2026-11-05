using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal class GenerationShapeIdentifierBuilder
{
    internal string CreateShapeIdentifier(GenerationConcept concept)
    {
        if (concept == null)
        {
            throw new System.ArgumentNullException("concept");
        }

        StringBuilder value = new StringBuilder();
        AppendInteger(value, (int)concept.Kind);
        if (concept.MessageDefinition != null)
        {
            AppendMessageDefinition(value, concept.MessageDefinition);
        }
        if (concept.MessageBuildingBlock != null)
        {
            AppendMessageBuildingBlock(value, concept.MessageBuildingBlock);
        }
        if (concept.MessageComponent != null)
        {
            AppendMessageComponent(value, concept.MessageComponent);
        }
        if (concept.MessageElement != null)
        {
            AppendMessageElement(value, concept.MessageElement);
        }
        if (concept.Choice != null)
        {
            AppendChoice(value, concept.Choice);
        }
        if (concept.DataType != null)
        {
            AppendDataType(value, concept.DataType);
        }
        if (concept.CodeSet != null)
        {
            AppendCodeSet(value, concept.CodeSet);
        }
        if (concept.CodeValue != null)
        {
            AppendCodeValue(value, concept.CodeValue);
        }
        if (concept.ExternalSchema != null)
        {
            AppendExternalSchema(value, concept.ExternalSchema);
        }
        return "shape_" + GenerationStableHash.CreateHex(value.ToString());
    }

    internal void AppendMessageDefinition(StringBuilder builder, MessageDefinitionMetadata value)
    {
        AppendMessageIdentifier(builder, value.MessageIdentifier);
        AppendText(builder, value.NamespaceUri);
        AppendText(builder, value.XmlName);
        AppendText(builder, value.XmlTag);
        AppendText(builder, value.RootElement);
        AppendReferenceList(builder, value.BuildingBlocks);
        AppendReferenceList(builder, value.Choices);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendMessageBuildingBlock(
        StringBuilder builder,
        MessageBuildingBlockDefinition value)
    {
        AppendText(builder, value.XmlTag);
        AppendMultiplicity(builder, value.Multiplicity);
        AppendReference(builder, value.MemberType);
        AppendOptionalReference(builder, value.SimpleType);
        AppendOptionalReference(builder, value.ComplexType);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendMessageComponent(StringBuilder builder, MessageComponentDefinition value)
    {
        AppendBoolean(builder, value.IsTechnical);
        AppendReferenceList(builder, value.MessageElements);
        AppendReferenceList(builder, value.Choices);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendMessageElement(StringBuilder builder, MessageElementDefinition value)
    {
        AppendInteger(builder, (int)value.Kind);
        AppendText(builder, value.XmlTag);
        AppendMultiplicity(builder, value.Multiplicity);
        AppendBoolean(builder, value.IsTechnical);
        AppendBoolean(builder, value.IsDerived);
        AppendBoolean(builder, value.IsComposite);
        AppendReference(builder, value.MemberType);
        AppendOptionalReference(builder, value.SimpleType);
        AppendOptionalReference(builder, value.ComplexType);
        AppendReferenceList(builder, value.Derivations);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendChoice(StringBuilder builder, ChoiceDefinition value)
    {
        AppendText(builder, value.ChoiceType);
        AppendBoolean(builder, value.IsTechnical);
        AppendMultiplicity(builder, value.Multiplicity);
        AppendReferenceList(builder, value.MessageElements);
        AppendReferenceList(builder, value.ImpactedMessageElements);
        AppendReferenceList(builder, value.ImpactedBuildingBlocks);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendDataType(StringBuilder builder, DataTypeDefinition value)
    {
        AppendInteger(builder, (int)value.Kind);
        AppendText(builder, value.SourceTypeName);
        AppendText(builder, value.IdentificationScheme);
        AppendText(builder, value.MeaningWhenTrue);
        AppendText(builder, value.MeaningWhenFalse);
        AppendOptionalDecimal(builder, value.BaseValue);
        AppendText(builder, value.UnitCode);
        AppendOptionalReference(builder, value.CurrencyIdentifierSet);
        AppendInteger(builder, (int)value.BinaryEncoding);
        AppendText(builder, value.SchemaTypeName);
        AppendFacetList(builder, value.Facets);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendCodeSet(StringBuilder builder, CodeSetDefinition value)
    {
        AppendInteger(builder, (int)value.Kind);
        AppendText(builder, value.IdentificationScheme);
        AppendOptionalReference(builder, value.BaseCodeSet);
        AppendFacetList(builder, value.Facets);
        AppendCodeValueList(builder, value.Codes);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendCodeValue(StringBuilder builder, CodeValueDefinition value)
    {
        AppendText(builder, value.Value);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendExternalSchema(StringBuilder builder, ExternalSchemaDefinition value)
    {
        AppendInteger(builder, (int)value.Kind);
        AppendText(builder, value.SourceTypeName);
        AppendText(builder, value.NamespaceMode);
        AppendTextList(builder, value.NamespaceUris);
        AppendText(builder, value.ProcessContents);
        AppendMultiplicity(builder, value.Multiplicity);
        AppendBoolean(builder, value.IsTechnical);
        AppendConstraintList(builder, value.Registration.Constraints);
    }

    internal void AppendMessageIdentifier(StringBuilder builder, MessageIdentifier value)
    {
        AppendText(builder, value.BusinessArea);
        AppendText(builder, value.MessageFunctionality);
        AppendText(builder, value.Variant);
        AppendText(builder, value.Version);
    }

    internal void AppendMultiplicity(StringBuilder builder, MultiplicityDefinition value)
    {
        AppendInteger(builder, value.MinimumOccurs);
        AppendBoolean(builder, value.HasMaximumOccurs);
        AppendInteger(builder, value.MaximumOccurs);
    }

    internal void AppendOptionalDecimal(StringBuilder builder, IsoDecimalValue? value)
    {
        if (value == null)
        {
            AppendText(builder, string.Empty);
            return;
        }
        AppendText(builder, value.LexicalValue);
        AppendInteger(builder, value.Scale);
        AppendBoolean(builder, value.IsNegativeZero);
    }

    internal void AppendFacetList(StringBuilder builder, List<DataTypeFacet> values)
    {
        List<DataTypeFacet> ordered = new List<DataTypeFacet>();
        ordered.AddRange(values);
        SortFacets(ordered);
        AppendInteger(builder, ordered.Count);
        int index = 0;
        while (index < ordered.Count)
        {
            DataTypeFacet facet = ordered[index];
            AppendText(builder, facet.Name);
            AppendText(builder, facet.Value);
            AppendBoolean(builder, facet.HasIntegerValue);
            AppendInteger(builder, facet.IntegerValue);
            AppendOptionalDecimal(builder, facet.DecimalValue);
            AppendSortedTextList(builder, facet.EnumerationValues);
            index++;
        }
    }

    internal void SortFacets(List<DataTypeFacet> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            DataTypeFacet value = values[index];
            string identity = CreateFacetIdentity(value);
            int destination = index;
            while (destination > 0 && string.CompareOrdinal(
                identity,
                CreateFacetIdentity(values[destination - 1])) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }

    internal string CreateFacetIdentity(DataTypeFacet value)
    {
        StringBuilder builder = new StringBuilder();
        AppendText(builder, value.Name);
        AppendText(builder, value.Value);
        AppendBoolean(builder, value.HasIntegerValue);
        AppendInteger(builder, value.IntegerValue);
        AppendOptionalDecimal(builder, value.DecimalValue);
        AppendSortedTextList(builder, value.EnumerationValues);
        return builder.ToString();
    }

    internal void AppendCodeValueList(StringBuilder builder, List<CodeValueDefinition> values)
    {
        List<CodeValueDefinition> ordered = new List<CodeValueDefinition>();
        ordered.AddRange(values);
        SortCodeValues(ordered);
        AppendInteger(builder, ordered.Count);
        int index = 0;
        while (index < ordered.Count)
        {
            AppendText(builder, ordered[index].Value);
            index++;
        }
    }

    internal void SortCodeValues(List<CodeValueDefinition> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            CodeValueDefinition value = values[index];
            int destination = index;
            while (destination > 0 &&
                CompareCodeValues(value, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }

    internal int CompareCodeValues(CodeValueDefinition left, CodeValueDefinition right)
    {
        int comparison = string.CompareOrdinal(left.Value, right.Value);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Identifier, right.Identifier);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Name, right.Name);
    }

    internal void AppendConstraintList(StringBuilder builder, List<ConstraintDefinition> values)
    {
        List<ConstraintDefinition> ordered = new List<ConstraintDefinition>();
        ordered.AddRange(values);
        SortConstraints(ordered);
        AppendInteger(builder, ordered.Count);
        int index = 0;
        while (index < ordered.Count)
        {
            ConstraintDefinition constraint = ordered[index];
            AppendText(builder, constraint.Name);
            AppendText(builder, constraint.Expression);
            AppendText(builder, constraint.ExpressionLanguage);
            index++;
        }
    }

    internal void SortConstraints(List<ConstraintDefinition> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            ConstraintDefinition value = values[index];
            int destination = index;
            while (destination > 0 &&
                CompareConstraints(value, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }

    internal int CompareConstraints(ConstraintDefinition left, ConstraintDefinition right)
    {
        int comparison = string.CompareOrdinal(left.Name, right.Name);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Expression, right.Expression);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.ExpressionLanguage, right.ExpressionLanguage);
    }

    internal void AppendReferenceList(StringBuilder builder, List<RepositoryReference> values)
    {
        List<RepositoryReference> ordered = new List<RepositoryReference>();
        ordered.AddRange(values);
        SortReferences(ordered);
        AppendInteger(builder, ordered.Count);
        int index = 0;
        while (index < ordered.Count)
        {
            AppendReference(builder, ordered[index]);
            index++;
        }
    }

    internal void SortReferences(List<RepositoryReference> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            RepositoryReference value = values[index];
            int destination = index;
            while (destination > 0 &&
                CompareReferences(value, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }

    internal int CompareReferences(RepositoryReference left, RepositoryReference right)
    {
        int comparison = left.Position.CompareTo(right.Position);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.PropertyName, right.PropertyName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.TargetIdentifier, right.TargetIdentifier);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = ((int)left.TargetKind).CompareTo((int)right.TargetKind);
        if (comparison != 0)
        {
            return comparison;
        }
        return left.IsContainment.CompareTo(right.IsContainment);
    }

    internal void AppendOptionalReference(StringBuilder builder, RepositoryReference? value)
    {
        if (value == null)
        {
            AppendText(builder, string.Empty);
            return;
        }
        AppendReference(builder, value);
    }

    internal void AppendReference(StringBuilder builder, RepositoryReference value)
    {
        AppendText(builder, value.PropertyName);
        AppendInteger(builder, value.Position);
        AppendText(builder, value.TargetIdentifier);
        AppendInteger(builder, (int)value.TargetKind);
        AppendBoolean(builder, value.IsContainment);
    }

    internal void AppendTextList(StringBuilder builder, List<string> values)
    {
        AppendInteger(builder, values.Count);
        int index = 0;
        while (index < values.Count)
        {
            AppendText(builder, values[index]);
            index++;
        }
    }

    internal void AppendSortedTextList(StringBuilder builder, List<string> values)
    {
        List<string> ordered = new List<string>();
        ordered.AddRange(values);
        XsdLogicalModelEmitter.SortFacetValues(ordered);
        AppendTextList(builder, ordered);
    }

    internal void AppendText(StringBuilder builder, string value)
    {
        builder.Append(value.Length);
        builder.Append(':');
        builder.Append(value);
        builder.Append(';');
    }

    internal void AppendInteger(StringBuilder builder, int value)
    {
        AppendText(builder, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal void AppendBoolean(StringBuilder builder, bool value)
    {
        if (value)
        {
            AppendText(builder, "1");
        }
        else
        {
            AppendText(builder, "0");
        }
    }
}
