using System;
using System.Collections.Generic;

namespace xml;

internal class XmlSchemaAssessmentEngine
{
    internal const string XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    internal CompiledXmlSchemaSet SchemaSet;
    internal XmlSchemaValidationOptions Options;
    internal IXmlResourceResolver Resolver;
    internal XmlSchemaContentModelValidator ContentValidator;
    internal List<XmlSchemaNodeAssessment> Assessments;
    internal List<XmlSchemaLocationHint> LocationHints;
    internal List<XmlDiagnostic> Diagnostics;
    internal XmlSchemaPsviProcessor Psvi;

    internal XmlSchemaAssessmentEngine(CompiledXmlSchemaSet schemaSet, XmlSchemaValidationOptions options, IXmlResourceResolver resolver)
    {
        Guard.NotNull(schemaSet, "schemaSet");
        Guard.NotNull(options, "options");
        Guard.NotNull(resolver, "resolver");
        SchemaSet = schemaSet;
        Options = options;
        Resolver = resolver;
        ContentValidator = new XmlSchemaContentModelValidator(schemaSet);
        Assessments = new List<XmlSchemaNodeAssessment>();
        LocationHints = new List<XmlSchemaLocationHint>();
        Diagnostics = new List<XmlDiagnostic>();
        Psvi = new XmlSchemaPsviProcessor(this);
    }

    internal XmlSchemaAssessmentResult AssessXmlSchemaDocument(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        Assessments.Clear();
        LocationHints.Clear();
        Diagnostics.Clear();
        Psvi.ResetXmlSchemaPsviState();
        XmlNode? root = document.DocumentElement;
        if (root == null || root.Name == null)
        {
            ReportXmlSchemaAssessmentError(root, "Schema validation requires a document element.");
        }
        else if (Options.ProcessContents == XmlSchemaProcessContents.Skip)
        {
            AddSkippedXmlSchemaSubtree(root, XmlSchemaProcessContents.Skip);
        }
        else
        {
            ReadXmlSchemaLocationHints(root);
            CompiledXmlSchemaElementDeclaration? declaration = ContentValidator.FindGlobalXmlSchemaElement(root.Name);
            if (declaration == null)
            {
                LoadXmlSchemaLocationHintSet(root.Name);
                declaration = ContentValidator.FindGlobalXmlSchemaElement(root.Name);
            }

            if (declaration == null)
            {
                if (Options.ProcessContents == XmlSchemaProcessContents.Lax)
                {
                    AssessLaxXmlSchemaSubtree(root);
                }
                else
                {
                    AddSkippedXmlSchemaAssessment(root, XmlSchemaProcessContents.Strict);
                    Assessments[Assessments.Count - 1].Validity = XmlSchemaValidity.Invalid;
                    ReportXmlSchemaAssessmentError(root, "The document element has no global element declaration.");
                }
            }
            else
            {
                AssessXmlSchemaElement(root, declaration, Options.ProcessContents);
            }
        }

        new XmlSchemaIdentityConstraintValidator(this).ValidateXmlSchemaIdentityConstraints();
        XmlSchemaTypeInfo[] annotations = Psvi.CreateXmlSchemaTypeAnnotations();

        XmlSchemaAssessmentResult result = new XmlSchemaAssessmentResult();
        result.Validity = Diagnostics.Count == 0 ? XmlSchemaValidity.Valid : XmlSchemaValidity.Invalid;
        if (Diagnostics.Count == 0 && Assessments.Count != 0 && Assessments[0].Validity == XmlSchemaValidity.NotKnown)
        {
            result.Validity = XmlSchemaValidity.NotKnown;
        }

        result.Assessments = Assessments.ToArray();
        result.LocationHints = LocationHints.ToArray();
        result.Diagnostics = Diagnostics.ToArray();
        result.TypeAnnotations = annotations;
        result.Succeeded = true;
        return result;
    }

    internal XmlSchemaAssessmentResult AssessXmlSchemaNodeAgainstType(XmlNode supplied, XmlName typeName)
    {
        Guard.NotNull(supplied, "supplied");
        Guard.NotNull(typeName, "typeName");
        Assessments.Clear();
        LocationHints.Clear();
        Diagnostics.Clear();
        Psvi.ResetXmlSchemaPsviState();
        XmlNode node = supplied;
        if (node.Kind == XmlNodeKind.Document)
        {
            XmlNode[] children = node.Children == null ? [] : node.Children;
            int childIndex = 0;
            while (childIndex < children.Length && children[childIndex].Kind != XmlNodeKind.Element)
            {
                childIndex = childIndex + 1;
            }

            if (childIndex == children.Length)
            {
                ReportXmlSchemaAssessmentError(node, "Typed document validation requires an element child.");
            }
            else
            {
                node = children[childIndex];
            }
        }

        XmlSchemaSelectedType selected = FindXmlSchemaNamedType(typeName);
        if (selected.SimpleType == null && selected.ComplexType == null)
        {
            ReportXmlSchemaAssessmentError(node, "The requested schema type is not declared.");
        }
        else if (node.Kind == XmlNodeKind.Attribute)
        {
            if (selected.SimpleType == null)
            {
                ReportXmlSchemaAssessmentError(node, "An attribute cannot be validated against a complex type.");
            }
            else
            {
                AddXmlSchemaAttributeAssessment(node, null, selected.SimpleType, XmlSchemaProcessContents.Strict, null, null);
            }
        }
        else if (node.Kind != XmlNodeKind.Element)
        {
            ReportXmlSchemaAssessmentError(node, "Only document, element, and attribute nodes can be validated against a named type.");
        }
        else
        {
            int diagnosticStart = Diagnostics.Count;
            XmlSchemaNodeAssessment assessment = new XmlSchemaNodeAssessment(node);
            assessment.SimpleType = selected.SimpleType;
            assessment.ComplexType = selected.ComplexType;
            Assessments.Add(assessment);
            if (selected.ComplexType != null && selected.ComplexType.Abstract)
            {
                ReportXmlSchemaAssessmentError(node, "An abstract complex type cannot be used for element assessment.");
            }

            Psvi.ApplyXmlSchemaAttributeDefaults(node, selected.ComplexType);
            AssessXmlSchemaAttributes(node, selected.ComplexType);
            if (selected.SimpleType != null)
            {
                if (HasXmlSchemaElementChildren(node))
                {
                    ReportXmlSchemaAssessmentError(node, "An element with simple type cannot have element children.");
                }
                else
                {
                    string value = Psvi.PrepareXmlSchemaElementValue(assessment, selected.SimpleType);
                    ValidateXmlSchemaAssessmentSimpleValue(assessment, selected.SimpleType, value);
                }
            }
            else if (selected.ComplexType != null)
            {
                AssessXmlSchemaComplexContent(node, selected.ComplexType, assessment);
            }

            assessment.Validity = Diagnostics.Count == diagnosticStart ? XmlSchemaValidity.Valid : XmlSchemaValidity.Invalid;
        }

        new XmlSchemaIdentityConstraintValidator(this).ValidateXmlSchemaIdentityConstraints();
        XmlSchemaAssessmentResult result = new XmlSchemaAssessmentResult();
        result.Validity = Diagnostics.Count == 0 ? XmlSchemaValidity.Valid : XmlSchemaValidity.Invalid;
        result.Assessments = Assessments.ToArray();
        result.LocationHints = LocationHints.ToArray();
        result.Diagnostics = Diagnostics.ToArray();
        result.TypeAnnotations = Psvi.CreateXmlSchemaTypeAnnotations();
        result.Succeeded = true;
        return result;
    }

    internal void AssessLaxXmlSchemaSubtree(XmlNode node)
    {
        AddSkippedXmlSchemaAssessment(node, XmlSchemaProcessContents.Lax);
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            XmlNode attribute = attributes[attributeIndex];
            if (attribute.Kind == XmlNodeKind.Attribute && attribute.Name != null && !IsRecognizedXmlSchemaInstanceAttribute(attribute.Name))
            {
                CompiledXmlSchemaAttributeDeclaration? declaration = ContentValidator.FindGlobalXmlSchemaAttribute(attribute.Name);
                if (declaration == null)
                {
                    AddXmlSchemaAttributeAssessment(attribute, null, null, XmlSchemaProcessContents.Lax, null, null);
                }
                else
                {
                    AddXmlSchemaAttributeAssessment(attribute, declaration, declaration.Type, XmlSchemaProcessContents.Lax, declaration.FixedValue, declaration.Node);
                }
            }

            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            XmlNode child = children[childIndex];
            if (child.Kind == XmlNodeKind.Element && child.Name != null)
            {
                CompiledXmlSchemaElementDeclaration? declaration = ContentValidator.FindGlobalXmlSchemaElement(child.Name);
                if (declaration == null)
                {
                    AssessLaxXmlSchemaSubtree(child);
                }
                else
                {
                    AssessXmlSchemaElement(child, declaration, XmlSchemaProcessContents.Lax);
                }
            }

            childIndex = childIndex + 1;
        }
    }

    internal void AddSkippedXmlSchemaSubtree(XmlNode node, XmlSchemaProcessContents processing)
    {
        AddSkippedXmlSchemaAssessment(node, processing);
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            XmlNode attribute = attributes[attributeIndex];
            if (attribute.Kind == XmlNodeKind.Attribute && attribute.Name != null && !IsRecognizedXmlSchemaInstanceAttribute(attribute.Name))
            {
                AddXmlSchemaAttributeAssessment(attribute, null, null, processing, null, null);
            }

            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            XmlNode child = children[childIndex];
            if (child.Kind == XmlNodeKind.Element)
            {
                AddSkippedXmlSchemaSubtree(child, processing);
            }

            childIndex = childIndex + 1;
        }
    }

    internal void AssessXmlSchemaElement(XmlNode node, CompiledXmlSchemaElementDeclaration declaration, XmlSchemaProcessContents processContents)
    {
        int diagnosticStart = Diagnostics.Count;
        XmlSchemaNodeAssessment assessment = new XmlSchemaNodeAssessment(node);
        assessment.ElementDeclaration = declaration;
        assessment.ProcessContents = processContents;
        Assessments.Add(assessment);
        ReadXmlSchemaLocationHints(node);
        if (declaration.IsAbstract)
        {
            ReportXmlSchemaAssessmentError(node, "An abstract element declaration cannot be used directly.");
        }

        XmlSchemaSelectedType selected = SelectXmlSchemaElementType(node, declaration);
        assessment.SimpleType = selected.SimpleType;
        assessment.ComplexType = selected.ComplexType;
        if (selected.ComplexType != null && selected.ComplexType.Abstract)
        {
            ReportXmlSchemaAssessmentError(node, "An abstract complex type cannot be used for element assessment.");
        }

        bool nilled = ReadXmlSchemaNilValue(node, declaration);
        assessment.IsNilled = nilled;
        Psvi.ApplyXmlSchemaAttributeDefaults(node, selected.ComplexType);
        AssessXmlSchemaAttributes(node, selected.ComplexType);
        if (nilled)
        {
            if (HasXmlSchemaNilledContent(node))
            {
                ReportXmlSchemaAssessmentError(node, "A nilled element cannot have element or character content.");
            }
        }
        else if (selected.SimpleType != null)
        {
            if (HasXmlSchemaElementChildren(node))
            {
                ReportXmlSchemaAssessmentError(node, "An element with simple type cannot have element children.");
            }
            else
            {
                string value = Psvi.PrepareXmlSchemaElementValue(assessment, selected.SimpleType);
                ValidateXmlSchemaAssessmentSimpleValue(assessment, selected.SimpleType, value);
                Psvi.ValidateXmlSchemaFixedValue(assessment, selected.SimpleType, assessment.SimpleTypeValidation, declaration.FixedValue, declaration.Node);
            }
        }
        else if (selected.ComplexType != null)
        {
            AssessXmlSchemaComplexContent(node, selected.ComplexType, assessment);
        }

        assessment.Validity = Diagnostics.Count == diagnosticStart ? XmlSchemaValidity.Valid : XmlSchemaValidity.Invalid;
    }

    internal XmlSchemaSelectedType SelectXmlSchemaElementType(XmlNode node, CompiledXmlSchemaElementDeclaration declaration)
    {
        XmlSchemaSelectedType result = new XmlSchemaSelectedType();
        result.SimpleType = declaration.SimpleType;
        result.ComplexType = declaration.ComplexType;
        string? value = FindXmlSchemaInstanceAttribute(node, "type");
        if (value == null)
        {
            return result;
        }

        XmlName? name = ResolveXmlSchemaInstanceQName(node, value);
        if (name == null)
        {
            ReportXmlSchemaAssessmentError(node, "The xsi:type value is not a resolvable QName.");
            return result;
        }

        XmlSchemaSelectedType selected = FindXmlSchemaNamedType(name);
        if (selected.SimpleType == null && selected.ComplexType == null)
        {
            ReportXmlSchemaAssessmentError(node, "The xsi:type value does not name a declared type.");
            return result;
        }

        if (!IsXmlSchemaTypeDerivedFrom(selected, result, declaration.Block))
        {
            ReportXmlSchemaAssessmentError(node, "The xsi:type type is not validly derived from the declared element type.");
            return result;
        }

        return selected;
    }

    internal XmlSchemaSelectedType FindXmlSchemaNamedType(XmlName name)
    {
        XmlSchemaSelectedType result = new XmlSchemaSelectedType();
        int simpleIndex = SchemaSet.SimpleTypes.Length - 1;
        while (simpleIndex >= 0)
        {
            XmlName? candidateName = SchemaSet.SimpleTypes[simpleIndex].Name;
            if (candidateName != null && ContentValidator.SameXmlSchemaName(candidateName, name))
            {
                result.SimpleType = SchemaSet.SimpleTypes[simpleIndex];
                return result;
            }

            simpleIndex = simpleIndex - 1;
        }

        int complexIndex = SchemaSet.ComplexTypes.Length - 1;
        while (complexIndex >= 0)
        {
            XmlName? candidateName = SchemaSet.ComplexTypes[complexIndex].Name;
            if (candidateName != null && ContentValidator.SameXmlSchemaName(candidateName, name))
            {
                result.ComplexType = SchemaSet.ComplexTypes[complexIndex];
                return result;
            }

            complexIndex = complexIndex - 1;
        }

        return result;
    }

    internal bool IsXmlSchemaTypeDerivedFrom(XmlSchemaSelectedType candidate, XmlSchemaSelectedType declared, XmlSchemaDerivationMethod blocked)
    {
        if (IsXmlSchemaAnyType(declared.ComplexType))
        {
            return true;
        }

        XmlSchemaDerivationMethod methods = XmlSchemaDerivationMethod.None;
        if (candidate.SimpleType != null && declared.SimpleType != null)
        {
            CompiledXmlSchemaSimpleType? currentSimple = candidate.SimpleType;
            while (currentSimple != null)
            {
                if (currentSimple == declared.SimpleType)
                {
                    return ((int)methods & (int)blocked) == 0;
                }

                methods = methods | currentSimple.DerivationMethod;
                currentSimple = currentSimple.BaseType;
            }

            return false;
        }

        if (candidate.ComplexType != null && declared.ComplexType != null)
        {
            CompiledXmlSchemaComplexType? currentComplex = candidate.ComplexType;
            while (currentComplex != null)
            {
                if (currentComplex == declared.ComplexType)
                {
                    return ((int)methods & (int)blocked) == 0;
                }

                methods = methods | currentComplex.DerivationMethod;
                CompiledXmlSchemaComplexType? next = currentComplex.BaseType;
                currentComplex = next == currentComplex ? null : next;
            }
        }

        return false;
    }

    internal bool IsXmlSchemaAnyType(CompiledXmlSchemaComplexType? type)
    {
        if (type == null || !type.IsBuiltIn || type.Name == null)
        {
            return false;
        }

        return type.Name.LocalName == "anyType" && string.Equals(type.Name.NamespaceUri, XmlNamespaces.XmlSchema, StringComparison.Ordinal);
    }

    internal bool ReadXmlSchemaNilValue(XmlNode node, CompiledXmlSchemaElementDeclaration declaration)
    {
        string? value = FindXmlSchemaInstanceAttribute(node, "nil");
        if (value == null)
        {
            return false;
        }

        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(value);
        bool nilled = normalized == "true" || normalized == "1";
        if (!nilled && normalized != "false" && normalized != "0")
        {
            ReportXmlSchemaAssessmentError(node, "The xsi:nil value must be a valid xs:boolean.");
            return false;
        }

        if (nilled && !declaration.IsNillable)
        {
            ReportXmlSchemaAssessmentError(node, "The element declaration is not nillable.");
        }

        return nilled;
    }

    internal void AssessXmlSchemaComplexContent(XmlNode node, CompiledXmlSchemaComplexType type, XmlSchemaNodeAssessment assessment)
    {
        if (type.ContentKind == XmlSchemaContentKind.Simple)
        {
            if (HasXmlSchemaElementChildren(node))
            {
                ReportXmlSchemaAssessmentError(node, "An element with simple content cannot have element children.");
            }
            else if (type.SimpleContentType != null)
            {
                string value = Psvi.PrepareXmlSchemaElementValue(assessment, type.SimpleContentType);
                ValidateXmlSchemaAssessmentSimpleValue(assessment, type.SimpleContentType, value);
                CompiledXmlSchemaElementDeclaration? declaration = assessment.ElementDeclaration;
                Psvi.ValidateXmlSchemaFixedValue(assessment, type.SimpleContentType, assessment.SimpleTypeValidation, declaration == null ? null : declaration.FixedValue, declaration == null ? null : declaration.Node);
            }

            return;
        }

        if ((type.ContentKind == XmlSchemaContentKind.Empty || type.ContentKind == XmlSchemaContentKind.ElementOnly) && HasXmlSchemaNonWhitespaceText(node))
        {
            ReportXmlSchemaAssessmentError(node, "Character content is not permitted by the complex type.");
        }

        XmlSchemaParticleMatchState? match = ContentValidator.MatchXmlSchemaContent(type, node);
        if (match == null)
        {
            ReportXmlSchemaAssessmentError(node, "The element children do not satisfy the schema content model.");
            return;
        }

        XmlNode[] children = ContentValidator.Elements;
        int index = 0;
        while (index < children.Length)
        {
            CompiledXmlSchemaElementDeclaration? declaration = match.Declarations[index];
            XmlSchemaProcessContents processing = match.Processing[index];
            if (processing == XmlSchemaProcessContents.Skip)
            {
                AddSkippedXmlSchemaAssessment(children[index], processing);
            }
            else if (declaration == null)
            {
                if (processing == XmlSchemaProcessContents.Strict)
                {
                    ReportXmlSchemaAssessmentError(children[index], "A strict wildcard matched an element without a global declaration.");
                }

                AddSkippedXmlSchemaAssessment(children[index], processing);
                if (processing == XmlSchemaProcessContents.Strict)
                {
                    Assessments[Assessments.Count - 1].Validity = XmlSchemaValidity.Invalid;
                }
            }
            else
            {
                AssessXmlSchemaElement(children[index], declaration, processing);
            }

            index = index + 1;
        }
    }

    internal void AssessXmlSchemaAttributes(XmlNode node, CompiledXmlSchemaComplexType? type)
    {
        CompiledXmlSchemaAttributeUse[] uses = type == null ? [] : type.Attributes;
        CompiledXmlSchemaWildcard? wildcard = type == null ? null : type.AttributeWildcard;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (attribute.Kind == XmlNodeKind.Attribute && attribute.Name != null && !IsRecognizedXmlSchemaInstanceAttribute(attribute.Name))
            {
                CompiledXmlSchemaAttributeUse? use = FindXmlSchemaAttributeUse(uses, attribute.Name);
                if (use != null)
                {
                    bool prohibited = use.Use == XmlSchemaAttributeUseKind.Prohibited;
                    if (use.Use == XmlSchemaAttributeUseKind.Prohibited)
                    {
                        ReportXmlSchemaAssessmentError(attribute, "A prohibited attribute is present.");
                    }

                    AddXmlSchemaAttributeAssessment(attribute, use.Declaration, use.Type, XmlSchemaProcessContents.Strict, use.FixedValue, use.Node);
                    if (prohibited)
                    {
                        Assessments[Assessments.Count - 1].Validity = XmlSchemaValidity.Invalid;
                    }
                }
                else if (wildcard == null || !ContentValidator.IsXmlSchemaWildcardMatch(wildcard, attribute.Name, true))
                {
                    ReportXmlSchemaAssessmentError(attribute, "The attribute is not permitted by the element type.");
                    AddXmlSchemaAttributeAssessment(attribute, null, null, XmlSchemaProcessContents.Strict, null, null);
                    Assessments[Assessments.Count - 1].Validity = XmlSchemaValidity.Invalid;
                }
                else
                {
                    CompiledXmlSchemaAttributeDeclaration? declaration = ContentValidator.FindGlobalXmlSchemaAttribute(attribute.Name);
                    if (wildcard.ProcessContents == XmlSchemaProcessContents.Strict && declaration == null)
                    {
                        ReportXmlSchemaAssessmentError(attribute, "A strict wildcard matched an attribute without a global declaration.");
                    }

                    CompiledXmlSchemaSimpleType? simpleType = declaration == null ? null : declaration.Type;
                    AddXmlSchemaAttributeAssessment(attribute, declaration, simpleType, wildcard.ProcessContents, declaration == null ? null : declaration.FixedValue, declaration == null ? null : declaration.Node);
                    if (wildcard.ProcessContents == XmlSchemaProcessContents.Strict && declaration == null)
                    {
                        Assessments[Assessments.Count - 1].Validity = XmlSchemaValidity.Invalid;
                    }
                }
            }

            index = index + 1;
        }

        int useIndex = 0;
        while (useIndex < uses.Length)
        {
            CompiledXmlSchemaAttributeUse use = uses[useIndex];
            if (use.Use == XmlSchemaAttributeUseKind.Required && FindXmlSchemaAttributeNode(attributes, use.Name) == null)
            {
                ReportXmlSchemaAssessmentError(node, "A required attribute is missing.");
            }

            useIndex = useIndex + 1;
        }
    }

    internal CompiledXmlSchemaAttributeUse? FindXmlSchemaAttributeUse(CompiledXmlSchemaAttributeUse[] uses, XmlName name)
    {
        int index = 0;
        while (index < uses.Length)
        {
            if (ContentValidator.SameXmlSchemaName(uses[index].Name, name))
            {
                return uses[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlNode? FindXmlSchemaAttributeNode(XmlNode[] attributes, XmlName name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? candidate = attributes[index].Name;
            if (candidate != null && ContentValidator.SameXmlSchemaName(candidate, name))
            {
                return attributes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void AddXmlSchemaAttributeAssessment(XmlNode node, CompiledXmlSchemaAttributeDeclaration? declaration, CompiledXmlSchemaSimpleType? type, XmlSchemaProcessContents processing, string? fixedValue, XmlNode? fixedNode)
    {
        XmlSchemaNodeAssessment assessment = new XmlSchemaNodeAssessment(node);
        assessment.AttributeDeclaration = declaration;
        assessment.SimpleType = type;
        assessment.ProcessContents = processing;
        assessment.Validity = type == null ? XmlSchemaValidity.NotKnown : XmlSchemaValidity.Valid;
        assessment.IsDefaulted = Psvi.DefaultedNodes.Contains(node);
        assessment.ValueContext = Psvi.FindXmlSchemaDefaultValueContext(node);
        Assessments.Add(assessment);
        if (type != null)
        {
            ValidateXmlSchemaAssessmentSimpleValue(assessment, type, node.Value == null ? "" : node.Value);
            Psvi.ValidateXmlSchemaFixedValue(assessment, type, assessment.SimpleTypeValidation, fixedValue, fixedNode);
        }
    }

    internal void ValidateXmlSchemaAssessmentSimpleValue(XmlSchemaNodeAssessment assessment, CompiledXmlSchemaSimpleType type, string value)
    {
        XmlNode context = assessment.ValueContext == null ? assessment.Node : assessment.ValueContext;
        XmlSchemaSimpleTypeValidationResult validation = new XmlSchemaSimpleTypeValidator().ValidateXmlSchemaSimpleType(type, value, context);
        assessment.SimpleTypeValidation = validation;
        if (!validation.Success)
        {
            assessment.Validity = XmlSchemaValidity.Invalid;
            ReportXmlSchemaAssessmentError(assessment.Node, "The value does not satisfy its XML Schema simple type and facets.");
        }
    }

    internal string ReadXmlSchemaElementStringValue(XmlNode node)
    {
        return new TreeNavigation().GetXmlNodeStringValueOperation(node);
    }

    internal void AddSkippedXmlSchemaAssessment(XmlNode node, XmlSchemaProcessContents processing)
    {
        XmlSchemaNodeAssessment assessment = new XmlSchemaNodeAssessment(node);
        assessment.ProcessContents = processing;
        assessment.Validity = XmlSchemaValidity.NotKnown;
        Assessments.Add(assessment);
    }

    internal bool IsRecognizedXmlSchemaInstanceAttribute(XmlName name)
    {
        if (!string.Equals(name.NamespaceUri, XmlSchemaInstanceNamespace, StringComparison.Ordinal))
        {
            return false;
        }

        return name.LocalName == "type" || name.LocalName == "nil" || name.LocalName == "schemaLocation" || name.LocalName == "noNamespaceSchemaLocation";
    }

    internal string? FindXmlSchemaInstanceAttribute(XmlNode node, string localName)
    {
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && name.LocalName == localName && string.Equals(name.NamespaceUri, XmlSchemaInstanceNamespace, StringComparison.Ordinal))
            {
                return attributes[index].Value == null ? "" : attributes[index].Value;
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlName? ResolveXmlSchemaInstanceQName(XmlNode node, string lexicalName)
    {
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(lexicalName);
        int separator = normalized.IndexOf(':');
        string prefix = separator < 0 ? "" : normalized.Substring(0, separator);
        string localName = separator < 0 ? normalized : normalized.Substring(separator + 1);
        if (normalized.Length == 0 || normalized.IndexOf(':', separator + 1) >= 0 || !new XdmStringDatatype().IsValidXdmStringLexical(localName, XdmAtomicType.NcName) || prefix.Length != 0 && !new XdmStringDatatype().IsValidXdmStringLexical(prefix, XdmAtomicType.NcName))
        {
            return null;
        }

        string? namespaceUri = FindXmlSchemaNamespaceBinding(node, prefix);
        if (prefix.Length != 0 && namespaceUri == null)
        {
            return null;
        }

        XmlName result = new XmlName();
        result.LocalName = localName;
        result.Prefix = prefix.Length == 0 ? null : prefix;
        result.NamespaceUri = namespaceUri;
        return result;
    }

    internal string? FindXmlSchemaNamespaceBinding(XmlNode node, string prefix)
    {
        if (prefix == "xml")
        {
            return XmlNamespaces.Xml;
        }

        XmlNode? current = node;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int index = bindings.Length - 1;
            while (index >= 0)
            {
                string? candidate = bindings[index].Prefix == null ? "" : bindings[index].Prefix;
                if (candidate == prefix)
                {
                    return bindings[index].IsUndeclaration ? null : bindings[index].NamespaceUri;
                }

                index = index - 1;
            }

            current = current.Parent;
        }

        return null;
    }

    internal void ReadXmlSchemaLocationHints(XmlNode node)
    {
        string? pairsValue = FindXmlSchemaInstanceAttribute(node, "schemaLocation");
        if (pairsValue != null)
        {
            string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(pairsValue);
            string[] tokens = normalized.Length == 0 ? [] : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0 || tokens.Length % 2 != 0)
            {
                ReportXmlSchemaAssessmentError(node, "xsi:schemaLocation requires namespace and location pairs.");
            }
            else
            {
                int index = 0;
                while (index < tokens.Length)
                {
                    AddXmlSchemaLocationHint(node, tokens[index], tokens[index + 1]);
                    index = index + 2;
                }
            }
        }

        string? noNamespaceValue = FindXmlSchemaInstanceAttribute(node, "noNamespaceSchemaLocation");
        if (noNamespaceValue != null)
        {
            string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(noNamespaceValue);
            if (normalized.Length == 0 || normalized.IndexOf(' ') >= 0)
            {
                ReportXmlSchemaAssessmentError(node, "xsi:noNamespaceSchemaLocation requires one non-empty location.");
            }
            else
            {
                AddXmlSchemaLocationHint(node, null, normalized);
            }
        }
    }

    internal void AddXmlSchemaLocationHint(XmlNode node, string? namespaceUri, string location)
    {
        int existingIndex = 0;
        while (existingIndex < LocationHints.Count)
        {
            XmlSchemaLocationHint existing = LocationHints[existingIndex];
            if (existing.Node == node && string.Equals(existing.NamespaceUri, namespaceUri, StringComparison.Ordinal) && existing.Location == location)
            {
                return;
            }

            existingIndex = existingIndex + 1;
        }

        XmlSchemaLocationHint hint = new XmlSchemaLocationHint(namespaceUri, location, node);
        hint.AbsoluteUri = ResolveXmlSchemaHintUri(node, location);
        hint.IsAllowed = IsXmlSchemaHintAllowed(hint.AbsoluteUri);
        LocationHints.Add(hint);
    }

    internal void LoadXmlSchemaLocationHintSet(XmlName rootName)
    {
        int index = 0;
        while (index < LocationHints.Count)
        {
            XmlSchemaLocationHint hint = LocationHints[index];
            string rootNamespace = rootName.NamespaceUri == null ? "" : rootName.NamespaceUri;
            string hintNamespace = hint.NamespaceUri == null ? "" : hint.NamespaceUri;
            if (hint.IsAllowed && hint.AbsoluteUri != null && rootNamespace == hintNamespace)
            {
                XmlResolvedResource? resource = Resolver.ResolveXmlResource(XmlResourceKind.Schema, null, hint.AbsoluteUri, null, Options.ResolverPolicy == null ? CreateDefaultXmlSchemaHintPolicy() : Options.ResolverPolicy);
                if (resource == null || resource.Access != XmlResourceAccess.Allowed || resource.Kind != XmlResourceKind.Schema || resource.Input == null)
                {
                    throw CreateXmlSchemaHintResourceError(hint.Node, "The schema location hint did not resolve to an allowed schema input.");
                }

                if (resource.Input.BaseUri == null)
                {
                    resource.Input.BaseUri = resource.AbsoluteUri == null ? hint.AbsoluteUri : resource.AbsoluteUri;
                }

                XmlSchemaSet loaded = new XmlSchemaCompiler().CompileXmlSchemas([resource.Input], Options, Resolver);
                CompiledXmlSchemaSet? compiled = loaded.CompiledSchemaSet == null ? loaded.CompiledSet as CompiledXmlSchemaSet : loaded.CompiledSchemaSet;
                if (compiled == null)
                {
                    throw CreateXmlSchemaHintResourceError(hint.Node, "The schema location hint did not produce a compiled schema set.");
                }

                SchemaSet = MergeXmlSchemaValidationSets(SchemaSet, compiled);
                ContentValidator = new XmlSchemaContentModelValidator(SchemaSet);
                if (ContentValidator.FindGlobalXmlSchemaElement(rootName) != null)
                {
                    return;
                }
            }

            index = index + 1;
        }
    }

    internal CompiledXmlSchemaSet MergeXmlSchemaValidationSets(CompiledXmlSchemaSet first, CompiledXmlSchemaSet second)
    {
        List<XmlSchemaModule> modules = new List<XmlSchemaModule>();
        modules.AddRange(first.Modules);
        modules.AddRange(second.Modules);
        List<CompiledXmlSchemaSimpleType> simpleTypes = new List<CompiledXmlSchemaSimpleType>();
        simpleTypes.AddRange(first.SimpleTypes);
        simpleTypes.AddRange(second.SimpleTypes);
        List<CompiledXmlSchemaComplexType> complexTypes = new List<CompiledXmlSchemaComplexType>();
        complexTypes.AddRange(first.ComplexTypes);
        complexTypes.AddRange(second.ComplexTypes);
        List<CompiledXmlSchemaGroup> groups = new List<CompiledXmlSchemaGroup>();
        groups.AddRange(first.Groups);
        groups.AddRange(second.Groups);
        List<CompiledXmlSchemaAttributeGroup> attributeGroups = new List<CompiledXmlSchemaAttributeGroup>();
        attributeGroups.AddRange(first.AttributeGroups);
        attributeGroups.AddRange(second.AttributeGroups);
        List<CompiledXmlSchemaElementDeclaration> elements = new List<CompiledXmlSchemaElementDeclaration>();
        elements.AddRange(first.Elements);
        elements.AddRange(second.Elements);
        List<CompiledXmlSchemaAttributeDeclaration> attributes = new List<CompiledXmlSchemaAttributeDeclaration>();
        attributes.AddRange(first.Attributes);
        attributes.AddRange(second.Attributes);
        List<CompiledXmlSchemaIdentityConstraint> constraints = new List<CompiledXmlSchemaIdentityConstraint>();
        constraints.AddRange(first.IdentityConstraints);
        constraints.AddRange(second.IdentityConstraints);
        List<CompiledXmlSchemaNotation> notations = new List<CompiledXmlSchemaNotation>();
        notations.AddRange(first.Notations);
        notations.AddRange(second.Notations);
        return new CompiledXmlSchemaSet(modules.ToArray(), simpleTypes.ToArray(), complexTypes.ToArray(), groups.ToArray(), attributeGroups.ToArray(), elements.ToArray(), attributes.ToArray(), constraints.ToArray(), notations.ToArray());
    }

    internal XmlResolverPolicy CreateDefaultXmlSchemaHintPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Denied;
        policy.AllowedSchemes = [];
        return policy;
    }

    internal XmlResourceException CreateXmlSchemaHintResourceError(XmlNode node, string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.ResourceNotFound;
        diagnostic.Category = XmlErrorCategory.Resource;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = node.Range;
        SourceLocation? location = node.Range == null ? null : node.Range.Start;
        return new XmlResourceException(message, diagnostic, location);
    }

    internal string? ResolveXmlSchemaHintUri(XmlNode node, string location)
    {
        if (!Uri.IsWellFormedUriString(location, UriKind.RelativeOrAbsolute))
        {
            ReportXmlSchemaAssessmentError(node, "A schema location hint is not a valid URI reference.");
            return null;
        }

        if (Uri.IsWellFormedUriString(location, UriKind.Absolute))
        {
            return new Uri(location).AbsoluteUri;
        }

        string? baseUri = new TreeNavigation().GetXmlNodeBaseUriOperation(node);
        if (baseUri == null || !Uri.IsWellFormedUriString(baseUri, UriKind.Absolute))
        {
            return null;
        }

        return new Uri(new Uri(baseUri), location).AbsoluteUri;
    }

    internal bool IsXmlSchemaHintAllowed(string? absoluteUri)
    {
        if (!Options.UseLocationHints || absoluteUri == null)
        {
            return false;
        }

        XmlResolverPolicy? policy = Options.ResolverPolicy;
        if (policy == null || policy.Access != XmlResourceAccess.Allowed)
        {
            return false;
        }

        Uri uri = new Uri(absoluteUri);
        ResolverGuard guard = new ResolverGuard(policy, null);
        string[]? schemes = policy.AllowedSchemes;
        if (schemes != null && !guard.ContainsXmlResourceScheme(schemes, uri.Scheme))
        {
            return false;
        }

        string[]? roots = policy.AllowedRoots;
        if (uri.IsFile && roots != null && !guard.IsXmlResourceUnderAllowedRoot(uri, roots))
        {
            return false;
        }

        return true;
    }

    internal bool HasXmlSchemaElementChildren(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasXmlSchemaNilledContent(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.Element || (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && child.Value != null && child.Value.Length != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasXmlSchemaNonWhitespaceText(XmlNode node)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if ((child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData) && ContainsXmlSchemaNonWhitespace(child.Value))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsXmlSchemaNonWhitespace(string? value)
    {
        if (value == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!char.IsWhiteSpace(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ReportXmlSchemaAssessmentError(XmlNode? node, string message)
    {
        if (Options.FailFast && Diagnostics.Count != 0)
        {
            return;
        }

        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.SchemaFailure;
        diagnostic.Category = XmlErrorCategory.Schema;
        diagnostic.Severity = XmlDiagnosticSeverity.Error;
        diagnostic.Message = message;
        diagnostic.Range = node == null ? null : node.Range;
        Diagnostics.Add(diagnostic);
    }
}
