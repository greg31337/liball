using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaPsviProcessor
{
    internal XmlSchemaAssessmentEngine Engine;
    internal List<XmlNode> DefaultedNodes;
    internal List<XmlNode> DefaultValueContexts;

    internal XmlSchemaPsviProcessor(XmlSchemaAssessmentEngine engine)
    {
        Guard.NotNull(engine, "engine");
        Engine = engine;
        DefaultedNodes = new List<XmlNode>();
        DefaultValueContexts = new List<XmlNode>();
    }

    internal void ResetXmlSchemaPsviState()
    {
        DefaultedNodes.Clear();
        DefaultValueContexts.Clear();
    }

    internal void ApplyXmlSchemaAttributeDefaults(XmlNode element, CompiledXmlSchemaComplexType? type)
    {
        if (!Engine.Options.ApplyDefaultValues || type == null)
        {
            return;
        }

        CompiledXmlSchemaAttributeUse[] uses = type.Attributes;
        int index = 0;
        while (index < uses.Length)
        {
            CompiledXmlSchemaAttributeUse use = uses[index];
            string? constraint = use.FixedValue == null ? use.DefaultValue : use.FixedValue;
            XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
            if (constraint != null && use.Use == XmlSchemaAttributeUseKind.Optional && Engine.FindXmlSchemaAttributeNode(attributes, use.Name) == null)
            {
                string value = NormalizeXmlSchemaDefaultValue(use.Type, constraint, use.Node);
                XmlNode attribute = CreateXmlSchemaDefaultAttribute(element, use.Name, value);
                AppendXmlSchemaDefaultAttribute(element, attribute);
                DefaultedNodes.Add(attribute);
                DefaultValueContexts.Add(use.Node);
            }

            index = index + 1;
        }
    }

    internal string PrepareXmlSchemaElementValue(XmlSchemaNodeAssessment assessment, CompiledXmlSchemaSimpleType type)
    {
        string value = Engine.ReadXmlSchemaElementStringValue(assessment.Node);
        CompiledXmlSchemaElementDeclaration? declaration = assessment.ElementDeclaration;
        string? constraint = null;
        if (declaration != null)
        {
            constraint = declaration.FixedValue == null ? declaration.DefaultValue : declaration.FixedValue;
        }

        if (constraint == null || value.Length != 0)
        {
            return value;
        }

        assessment.IsDefaulted = true;
        assessment.ValueContext = declaration == null ? assessment.Node : declaration.Node;
        string normalized = NormalizeXmlSchemaDefaultValue(type, constraint, assessment.ValueContext);
        if (Engine.Options.ApplyDefaultValues)
        {
            AppendXmlSchemaDefaultText(assessment.Node, normalized);
        }

        return constraint;
    }

    internal void ValidateXmlSchemaFixedValue(XmlSchemaNodeAssessment assessment, CompiledXmlSchemaSimpleType type, XmlSchemaSimpleTypeValidationResult? actual, string? fixedValue, XmlNode? fixedNode)
    {
        if (fixedValue == null || actual == null || !actual.Success)
        {
            return;
        }

        XmlNode context = fixedNode == null ? assessment.Node : fixedNode;
        XmlSchemaSimpleTypeValidationResult expected = new XmlSchemaSimpleTypeValidator().ValidateXmlSchemaSimpleType(type, fixedValue, context);
        if (!expected.Success || !new XmlSchemaSimpleTypeValidator().EqualXmlSchemaSimpleValues(actual, expected))
        {
            assessment.Validity = XmlSchemaValidity.Invalid;
            Engine.ReportXmlSchemaAssessmentError(assessment.Node, "The assessed value does not equal its XML Schema fixed value.");
        }
    }

    internal XmlSchemaTypeInfo[] CreateXmlSchemaTypeAnnotations()
    {
        List<XmlSchemaTypeInfo> result = new List<XmlSchemaTypeInfo>();
        int index = 0;
        while (index < Engine.Assessments.Count)
        {
            XmlSchemaNodeAssessment assessment = Engine.Assessments[index];
            XmlSchemaTypeInfo information = CreateXmlSchemaTypeAnnotation(assessment);
            result.Add(information);
            if (Engine.Options.AnnotateTree)
            {
                assessment.Node.TypeInfo = information;
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal XmlSchemaTypeInfo CreateXmlSchemaTypeAnnotation(XmlSchemaNodeAssessment assessment)
    {
        XmlSchemaTypeInfo result = new XmlSchemaTypeInfo();
        result.Validity = assessment.Validity;
        result.IsNilled = assessment.IsNilled;
        result.IsDefaulted = assessment.IsDefaulted || DefaultedNodes.Contains(assessment.Node);
        CompiledXmlSchemaSimpleType? simpleType = SelectXmlSchemaAssessmentSimpleType(assessment);
        XmlName? typeName = assessment.ComplexType == null ? simpleType == null ? null : simpleType.Name : assessment.ComplexType.Name;
        result.TypeName = CopyXmlSchemaPsviName(typeName);
        if (assessment.ComplexType != null)
        {
            result.TypeDefinition = assessment.ComplexType;
        }
        else
        {
            result.TypeDefinition = simpleType;
        }

        if (assessment.ElementDeclaration != null)
        {
            result.Declaration = assessment.ElementDeclaration;
        }
        else
        {
            result.Declaration = assessment.AttributeDeclaration;
        }

        XmlSchemaSimpleTypeValidationResult? validation = assessment.SimpleTypeValidation;
        if (validation != null)
        {
            result.NormalizedValue = validation.NormalizedValue;
            if (validation.Success)
            {
                result.ActualValue = validation.AtomicValue;
                result.ActualValues = (XdmAtomicValue[])validation.AtomicValues.Clone();
                CompiledXmlSchemaSimpleType? member = validation.MemberType;
                result.MemberTypeName = CopyXmlSchemaPsviName(member == null ? null : member.Name);
                CompiledXmlSchemaSimpleType? actualType = member == null ? simpleType : member;
                result.AtomicType = ReadXmlSchemaPsviAtomicType(actualType);
                result.IsId = assessment.Validity == XmlSchemaValidity.Valid && IsXmlSchemaPsviIdType(actualType);
                result.IsIdReference = assessment.Validity == XmlSchemaValidity.Valid && IsXmlSchemaPsviIdReferenceType(actualType);
                int valueIndex = 0;
                while (valueIndex < result.ActualValues.Length)
                {
                    result.ActualValues[valueIndex].TypeInfo = result;
                    valueIndex = valueIndex + 1;
                }
            }
            else
            {
                result.AtomicType = ReadXmlSchemaPsviAtomicType(simpleType);
            }
        }
        else
        {
            result.AtomicType = ReadXmlSchemaPsviAtomicType(simpleType);
        }

        return result;
    }

    internal CompiledXmlSchemaSimpleType? SelectXmlSchemaAssessmentSimpleType(XmlSchemaNodeAssessment assessment)
    {
        if (assessment.SimpleType != null)
        {
            return assessment.SimpleType;
        }

        return assessment.ComplexType == null ? null : assessment.ComplexType.SimpleContentType;
    }

    internal XdmAtomicType ReadXmlSchemaPsviAtomicType(CompiledXmlSchemaSimpleType? type)
    {
        if (type != null && type.HasAtomicType)
        {
            return type.AtomicType;
        }

        return XdmAtomicType.AnyAtomicType;
    }

    internal bool IsXmlSchemaPsviIdType(CompiledXmlSchemaSimpleType? type)
    {
        return type != null && type.HasAtomicType && type.AtomicType == XdmAtomicType.Id;
    }

    internal bool IsXmlSchemaPsviIdReferenceType(CompiledXmlSchemaSimpleType? type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.HasAtomicType && type.AtomicType == XdmAtomicType.Idref)
        {
            return true;
        }

        return type.Variety == XmlSchemaSimpleVariety.List && type.ItemType != null && IsXmlSchemaPsviIdReferenceType(type.ItemType);
    }

    internal string NormalizeXmlSchemaDefaultValue(CompiledXmlSchemaSimpleType? type, string value, XmlNode context)
    {
        if (type == null)
        {
            return value;
        }

        XmlSchemaSimpleTypeValidationResult validation = new XmlSchemaSimpleTypeValidator().ValidateXmlSchemaSimpleType(type, value, context);
        return validation.Success ? validation.NormalizedValue : value;
    }

    internal XmlNode? FindXmlSchemaDefaultValueContext(XmlNode node)
    {
        int index = 0;
        while (index < DefaultedNodes.Count)
        {
            if (DefaultedNodes[index] == node)
            {
                return DefaultValueContexts[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlNode CreateXmlSchemaDefaultAttribute(XmlNode element, XmlName name, string value)
    {
        XmlNode result = CreateXmlSchemaDefaultNode(element, XmlNodeKind.Attribute, value);
        result.Name = CopyXmlSchemaPsviName(name);
        return result;
    }

    internal XmlNode CreateXmlSchemaDefaultNode(XmlNode parent, XmlNodeKind kind, string value)
    {
        XmlNode result = new XmlNode();
        result.Kind = kind;
        result.Value = value;
        result.Parent = parent;
        result.OwnerDocument = parent.OwnerDocument;
        result.Children = [];
        XmlDocument? document = parent.OwnerDocument;
        if (document != null)
        {
            document.MutationVersion = document.MutationVersion + 1;
            result.Identity = document.NextIdentity;
            document.NextIdentity = document.NextIdentity + 1;
            result.MutationVersion = document.MutationVersion;
            parent.MutationVersion = document.MutationVersion;
        }

        return result;
    }

    internal void AppendXmlSchemaDefaultAttribute(XmlNode element, XmlNode attribute)
    {
        XmlNode[] current = element.Attributes == null ? [] : element.Attributes;
        XmlNode[] expanded = new XmlNode[current.Length + 1];
        Array.Copy(current, expanded, current.Length);
        expanded[current.Length] = attribute;
        element.Attributes = expanded;
    }

    internal void AppendXmlSchemaDefaultText(XmlNode element, string value)
    {
        XmlNode text = CreateXmlSchemaDefaultNode(element, XmlNodeKind.Text, value);
        XmlNode[] current = element.Children == null ? [] : element.Children;
        XmlNode[] expanded = new XmlNode[current.Length + 1];
        Array.Copy(current, expanded, current.Length);
        expanded[current.Length] = text;
        element.Children = expanded;
    }

    internal XmlName? CopyXmlSchemaPsviName(XmlName? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }
}
