using System.Collections.Generic;

namespace xml;

internal enum XsltSchemaValidationMode
{
    Strip = 0,
    Preserve = 1,
    Strict = 2,
    Lax = 3
}

internal enum XsltTypedMode
{
    Unspecified = 0,
    Yes = 1,
    No = 2,
    Strict = 3,
    Lax = 4
}

internal class XsltSchemaIntegration
{
    internal XsltCompileOptions Options;
    internal XmlSchemaSet? SchemaSet;
    internal List<string> ImportedNamespaces;
    internal XdmTypeSystem XdmTypes;

    internal XsltSchemaIntegration(XsltCompileOptions options)
    {
        Guard.NotNull(options, "options");
        Options = options;
        SchemaSet = options.SchemaSet;
        ImportedNamespaces = new List<string>();
        XdmTypes = new XdmTypeSystem();
    }

    internal void ImportXsltSchemaNamespace(string? namespaceUri)
    {
        if (Options.ConformanceLevel != XsltConformanceLevel.SchemaAware)
        {
            throw CreateXsltSchemaStaticError("xsl:import-schema requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1650);
        }

        if (namespaceUri != null && namespaceUri.Length == 0)
        {
            throw CreateXsltSchemaStaticError("An xsl:import-schema namespace cannot be an empty URI.", XsltErrorCodes.XTSE0215);
        }

        string value = namespaceUri == null ? "" : namespaceUri;
        if (!ImportedNamespaces.Contains(value))
        {
            ImportedNamespaces.Add(value);
        }
    }

    internal bool IsXsltSchemaNamespaceImported(string? namespaceUri)
    {
        string value = namespaceUri == null ? "" : namespaceUri;
        return ImportedNamespaces.Contains(value);
    }

    internal bool MatchesXsltSchemaElementPattern(XmlNode candidate, XmlName declarationName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(declarationName, "declarationName");
        if (!IsXsltSchemaNamespaceImported(declarationName.NamespaceUri))
        {
            throw CreateXPathSchemaStaticError("The schema-element pattern does not name an imported declaration.", XPathErrorCodes.XPST0008);
        }

        XPathSchemaNodeTest tests = RequireXsltSchemaNodeTests();
        return tests.MatchesXPathSchemaElement(candidate, declarationName);
    }

    internal bool MatchesXsltSchemaAttributePattern(XmlNode candidate, XmlName declarationName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(declarationName, "declarationName");
        if (!IsXsltSchemaNamespaceImported(declarationName.NamespaceUri))
        {
            throw CreateXPathSchemaStaticError("The schema-attribute pattern does not name an imported declaration.", XPathErrorCodes.XPST0008);
        }

        XPathSchemaNodeTest tests = RequireXsltSchemaNodeTests();
        return tests.MatchesXPathSchemaAttribute(candidate, declarationName);
    }

    internal bool MatchesXsltTypeAwarePattern(XmlNode candidate, XmlName typeName)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(typeName, "typeName");
        if (!string.Equals(typeName.NamespaceUri, XPathNamespaces.XmlSchema, System.StringComparison.Ordinal) && !IsXsltSchemaNamespaceImported(typeName.NamespaceUri))
        {
            throw CreateXPathSchemaStaticError("The type-aware pattern does not name an imported type definition.", XPathErrorCodes.XPST0051);
        }

        XPathSchemaNodeTest tests = RequireXsltSchemaNodeTests();
        return tests.MatchesXPathSchemaType(candidate, typeName);
    }

    internal bool MatchesXsltTypedModeNameTest(XmlNode candidate, XmlName name, XsltTypedMode mode)
    {
        Guard.NotNull(candidate, "candidate");
        Guard.NotNull(name, "name");
        EnsureXsltTypedModeSelection(candidate, mode);
        if (candidate.Kind == XmlNodeKind.Element && mode == XsltTypedMode.Strict)
        {
            XPathSchemaNodeTest tests = RequireXsltSchemaNodeTests();
            if (!IsXsltSchemaNamespaceImported(name.NamespaceUri) || !tests.HasXPathSchemaElementDeclaration(name))
            {
                throw CreateXsltSchemaStaticError("A strict typed mode name test must name an in-scope global element declaration.", XsltErrorCodes.XTSE3105);
            }

            return tests.MatchesXPathSchemaElement(candidate, name);
        }

        if (candidate.Kind == XmlNodeKind.Element && mode == XsltTypedMode.Lax && IsXsltSchemaNamespaceImported(name.NamespaceUri))
        {
            XPathSchemaNodeTest tests = RequireXsltSchemaNodeTests();
            if (tests.HasXPathSchemaElementDeclaration(name))
            {
                return tests.MatchesXPathSchemaElement(candidate, name);
            }
        }

        XmlName? candidateName = candidate.Name;
        return candidateName != null && SameXsltSchemaName(candidateName, name);
    }

    internal void EnsureXsltTypedModeSelection(XmlNode node, XsltTypedMode mode)
    {
        Guard.NotNull(node, "node");
        if (Options.ConformanceLevel != XsltConformanceLevel.SchemaAware && (mode == XsltTypedMode.Yes || mode == XsltTypedMode.Strict))
        {
            throw CreateXsltSchemaStaticError("This typed mode requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660);
        }

        if (node.Kind != XmlNodeKind.Element && node.Kind != XmlNodeKind.Attribute)
        {
            return;
        }

        bool typed = IsXsltTypedSchemaNode(node);
        if ((mode == XsltTypedMode.Yes || mode == XsltTypedMode.Strict || mode == XsltTypedMode.Lax) && !typed)
        {
            throw CreateXsltSchemaTypeError("The typed mode selected an untyped element or attribute.", XsltErrorCodes.XTTE3100);
        }

        if (mode == XsltTypedMode.No && typed)
        {
            throw CreateXsltSchemaTypeError("The untyped mode selected a typed element or attribute.", XsltErrorCodes.XTTE3110);
        }
    }

    internal bool IsXsltTypedSchemaNode(XmlNode node)
    {
        XmlSchemaTypeInfo? annotation = node.TypeInfo;
        if (annotation == null)
        {
            return false;
        }

        XmlName? typeName = annotation.TypeName;
        if (typeName != null && string.Equals(typeName.NamespaceUri, XPathNamespaces.XmlSchema, System.StringComparison.Ordinal) && (typeName.LocalName == "untyped" || typeName.LocalName == "untypedAtomic"))
        {
            return false;
        }

        return annotation.TypeDefinition != null || typeName != null;
    }

    internal XmlSchemaValidationResult ApplyXsltSchemaValidation(XmlDocument document, XsltSchemaValidationMode mode)
    {
        Guard.NotNull(document, "document");
        if (Options.ConformanceLevel != XsltConformanceLevel.SchemaAware)
        {
            if (mode == XsltSchemaValidationMode.Strict)
            {
                throw CreateXsltSchemaStaticError("Strict validation requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660);
            }

            StripXsltSchemaAnnotations(document);
            return CreateXsltUnvalidatedSchemaResult(document);
        }

        if (mode == XsltSchemaValidationMode.Strip)
        {
            StripXsltSchemaAnnotations(document);
            return CreateXsltUnvalidatedSchemaResult(document);
        }

        if (mode == XsltSchemaValidationMode.Preserve)
        {
            return CreateXsltUnvalidatedSchemaResult(document);
        }

        XmlSchemaSet? schemas = SchemaSet;
        if (schemas == null)
        {
            if (mode == XsltSchemaValidationMode.Strict)
            {
                throw CreateXsltSchemaTypeError("Strict validation did not find a matching global declaration.", XsltErrorCodes.XTTE1512);
            }

            StripXsltSchemaAnnotations(document);
            return CreateXsltUnvalidatedSchemaResult(document);
        }

        if (mode == XsltSchemaValidationMode.Strict)
        {
            XmlNode? root = document.DocumentElement;
            XmlName? rootName = root == null ? null : root.Name;
            if (rootName != null && !new XPathSchemaNodeTest(schemas).HasXPathSchemaElementDeclaration(rootName))
            {
                throw CreateXsltSchemaTypeError("Strict validation did not find a matching global declaration.", XsltErrorCodes.XTTE1512);
            }
        }

        XmlSchemaValidationOptions validationOptions = new XmlSchemaValidationOptions();
        validationOptions.ProcessContents = mode == XsltSchemaValidationMode.Lax ? XmlSchemaProcessContents.Lax : XmlSchemaProcessContents.Strict;
        validationOptions.AnnotateTree = true;
        validationOptions.ApplyDefaultValues = true;
        XmlSchemaValidationResult result = new XmlSchemaValidator().ValidateXmlSchema(document, schemas, validationOptions);
        if (result.Validity == XmlSchemaValidity.Invalid)
        {
            string code = mode == XsltSchemaValidationMode.Strict ? XsltErrorCodes.XTTE1510 : XsltErrorCodes.XTTE1515;
            throw CreateXsltSchemaTypeError("The constructed tree is not valid against the in-scope schema components.", code);
        }

        if (mode == XsltSchemaValidationMode.Strict && result.Validity != XmlSchemaValidity.Valid)
        {
            throw CreateXsltSchemaTypeError("Strict validation did not find a matching global declaration.", XsltErrorCodes.XTTE1510);
        }

        return result;
    }

    internal void ApplyXsltSchemaNodeValidation(XmlNode node, XsltSchemaValidationMode mode, XmlName? typeName, bool shallow)
    {
        Guard.NotNull(node, "node");
        if (typeName == null && mode == XsltSchemaValidationMode.Preserve)
        {
            if (shallow && node.Kind == XmlNodeKind.Element)
            {
                XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo();
                annotation.TypeName = XdmTypes.CreateXdmTypeName("anyType");
                annotation.Validity = XmlSchemaValidity.NotKnown;
                node.TypeInfo = annotation;
            }

            return;
        }

        if (typeName == null && mode == XsltSchemaValidationMode.Strip)
        {
            StripXsltSchemaNodeAnnotations(node);
            return;
        }

        if (Options.ConformanceLevel != XsltConformanceLevel.SchemaAware)
        {
            throw CreateXsltSchemaStaticError("Schema validation requires a schema-aware XSLT processor.", XsltErrorCodes.XTSE1660);
        }

        if (typeName != null)
        {
            ValidateXsltNodeAgainstType(node, typeName);
            return;
        }

        if (node.Kind == XmlNodeKind.Attribute)
        {
            ValidateXsltAttributeDeclaration(node, mode);
            return;
        }

        XmlDocument document = node.OwnerDocument == null ? throw new System.InvalidOperationException("A copied node has no owner document.") : node.OwnerDocument;
        XmlNode? container = null;
        if (node.Kind == XmlNodeKind.Element)
        {
            container = new TreeMutation().CreateXmlTreeNodeOperation(document, XmlNodeKind.Document, null, null);
            document.DocumentNode = container;
            new TreeMutation().AppendXmlTreeChildOperation(container, node);
        }

        ApplyXsltSchemaValidation(document, mode);
        if (container != null)
        {
            new TreeMutation().RemoveXmlTreeNodeOperation(container, node);
            document.DocumentNode = null;
            document.DocumentElement = null;
        }
    }

    internal void ValidateXsltNodeAgainstType(XmlNode node, XmlName typeName)
    {
        XmlSchemaSet? schemas = SchemaSet;
        CompiledXmlSchemaSet? compiled = schemas == null ? null : schemas.CompiledSchemaSet == null ? schemas.CompiledSet as CompiledXmlSchemaSet : schemas.CompiledSchemaSet;
        if (compiled == null)
        {
            throw CreateXsltSchemaTypeError("The requested schema type is not available.", XsltErrorCodes.XTTE1540);
        }

        if (node.Kind == XmlNodeKind.Document && !HasXsltValidDocumentContent(node))
        {
            throw CreateXsltSchemaTypeError("A document validated against a type must contain exactly one element and no text nodes.", XsltErrorCodes.XTTE1550);
        }

        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions();
        options.ProcessContents = XmlSchemaProcessContents.Strict;
        options.AnnotateTree = true;
        options.ApplyDefaultValues = true;
        XmlSchemaAssessmentResult result = new XmlSchemaAssessmentEngine(compiled, options, new XmlDefaultResolver()).AssessXmlSchemaNodeAgainstType(node, typeName);
        if (result.Validity != XmlSchemaValidity.Valid)
        {
            throw CreateXsltSchemaTypeError("The copied node is not valid against the requested schema type.", XsltErrorCodes.XTTE1540);
        }
    }

    internal bool HasXsltValidDocumentContent(XmlNode document)
    {
        XmlNode[] children = document.Children == null ? [] : document.Children;
        int elements = 0;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                elements = elements + 1;
            }
            else if (children[index].Kind == XmlNodeKind.Text || children[index].Kind == XmlNodeKind.CData)
            {
                return false;
            }

            index = index + 1;
        }

        return elements == 1;
    }

    internal void ValidateXsltAttributeDeclaration(XmlNode attribute, XsltSchemaValidationMode mode)
    {
        XmlSchemaSet? schemas = SchemaSet;
        CompiledXmlSchemaSet? compiled = schemas == null ? null : schemas.CompiledSchemaSet == null ? schemas.CompiledSet as CompiledXmlSchemaSet : schemas.CompiledSchemaSet;
        if (compiled == null)
        {
            if (mode == XsltSchemaValidationMode.Lax)
            {
                StripXsltSchemaNodeAnnotations(attribute);
                return;
            }

            throw CreateXsltSchemaTypeError("Strict validation did not find an attribute declaration.", XsltErrorCodes.XTTE1512);
        }

        XmlName? name = attribute.Name;
        CompiledXmlSchemaAttributeDeclaration? declaration = name == null ? null : new XmlSchemaContentModelValidator(compiled).FindGlobalXmlSchemaAttribute(name);
        if (declaration == null)
        {
            if (mode == XsltSchemaValidationMode.Lax)
            {
                StripXsltSchemaNodeAnnotations(attribute);
                return;
            }

            throw CreateXsltSchemaTypeError("Strict validation did not find an attribute declaration.", XsltErrorCodes.XTTE1512);
        }

        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions();
        options.ProcessContents = XmlSchemaProcessContents.Strict;
        options.AnnotateTree = true;
        XmlSchemaAssessmentEngine engine = new XmlSchemaAssessmentEngine(compiled, options, new XmlDefaultResolver());
        engine.AddXmlSchemaAttributeAssessment(attribute, declaration, declaration.Type, XmlSchemaProcessContents.Strict, declaration.FixedValue, declaration.Node);
        XmlSchemaTypeInfo[] annotations = engine.Psvi.CreateXmlSchemaTypeAnnotations();
        if (annotations.Length == 0 || annotations[0].Validity != XmlSchemaValidity.Valid)
        {
            throw CreateXsltSchemaTypeError("The copied attribute is not valid against its declaration.", mode == XsltSchemaValidationMode.Strict ? XsltErrorCodes.XTTE1510 : XsltErrorCodes.XTTE1515);
        }
    }

    internal void StripXsltSchemaAnnotations(XmlDocument document)
    {
        XmlNode? root = document.DocumentNode;
        if (root != null)
        {
            StripXsltSchemaNodeAnnotations(root);
        }
    }

    internal void StripXsltSchemaNodeAnnotations(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Element || node.Kind == XmlNodeKind.Attribute)
        {
            XmlSchemaTypeInfo? previous = node.TypeInfo;
            XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo();
            annotation.TypeName = node.Kind == XmlNodeKind.Element ? XdmTypes.GetXdmUntypedNodeTypeName() : XdmTypes.CreateXdmTypeName("untypedAtomic");
            annotation.AtomicType = node.Kind == XmlNodeKind.Attribute ? XdmAtomicType.UntypedAtomic : XdmAtomicType.AnyAtomicType;
            annotation.Validity = XmlSchemaValidity.NotKnown;
            annotation.IsId = previous != null && previous.IsId;
            annotation.IsIdReference = previous != null && previous.IsIdReference;
            node.TypeInfo = annotation;
        }

        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            StripXsltSchemaNodeAnnotations(attributes[attributeIndex]);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            StripXsltSchemaNodeAnnotations(children[childIndex]);
            childIndex = childIndex + 1;
        }
    }

    internal XPathSchemaNodeTest RequireXsltSchemaNodeTests()
    {
        XmlSchemaSet? schemas = SchemaSet;
        if (schemas == null)
        {
            schemas = CreateXsltBuiltInSchemaSet();
            SchemaSet = schemas;
        }

        return new XPathSchemaNodeTest(schemas);
    }

    internal XmlSchemaSet CreateXsltBuiltInSchemaSet()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Denied;
        policy.AllowedSchemes = [];
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions();
        options.ResolverPolicy = policy;
        return new XmlSchemaCompiler().CompileXmlSchemas([], options, new XmlDefaultResolver());
    }

    internal XmlSchemaValidationResult CreateXsltUnvalidatedSchemaResult(XmlDocument document)
    {
        XmlSchemaValidationResult result = new XmlSchemaValidationResult();
        result.Document = document;
        result.SchemaSet = SchemaSet;
        result.Validity = XmlSchemaValidity.NotKnown;
        result.TypeAnnotations = [];
        result.Diagnostics = [];
        result.Succeeded = true;
        return result;
    }

    internal bool SameXsltSchemaName(XmlName left, XmlName right)
    {
        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return string.Equals(leftNamespace, rightNamespace, System.StringComparison.Ordinal) && string.Equals(left.LocalName, right.LocalName, System.StringComparison.Ordinal);
    }

    internal XPathStaticException CreateXPathSchemaStaticError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathStaticException(message, diagnostic, null);
    }

    internal XsltStaticException CreateXsltSchemaStaticError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltStatic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XsltStaticException(message, diagnostic, null);
    }

    internal XsltTypeException CreateXsltSchemaTypeError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XsltTypeException(message, diagnostic, null);
    }
}
