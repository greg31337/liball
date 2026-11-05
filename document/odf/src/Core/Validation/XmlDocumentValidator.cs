using System;
using System.Collections.Generic;

namespace odf;

internal class XmlDocumentValidator
{
    OdfValidationOptions Options;
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    HashSet<OdfNode> VisitedNodes = new HashSet<OdfNode>();
    bool Stop;
    int ValidatedPartCount;
    OdfVersion DocumentVersion;
    bool PreserveFutureContent;

    internal XmlDocumentValidator(OdfValidationOptions options)
    {
        if (!OptionsAreValid(options))
        {
            throw new OdfValidationException(
                "Positive bounded XML validation limits are required.");
        }
        Options = options;
    }

    internal XmlValidationResult ValidateDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for XML validation.");
        }
        Diagnostics = new List<OdfDiagnostic>();
        VisitedNodes = new HashSet<OdfNode>();
        Stop = false;
        ValidatedPartCount = 0;
        DocumentVersion = SelectDocumentVersion(document);
        PreserveFutureContent = document.LoadOptions.PreserveUnknownContent
            && document.SaveOptions.PreserveUnknownContent;
        if (Options.ValidateXml)
        {
            ValidateCancellation();
            ValidatePartCollection(document);
        }
        XmlValidationResult result = new XmlValidationResult();
        result.Diagnostics = Diagnostics.ToArray();
        result.ValidatedPartCount = ValidatedPartCount;
        result.StoppedEarly = Stop;
        result.IsValid = !HasErrors();
        return result;
    }

    void ValidatePartCollection(OdfDocument document)
    {
        Dictionary<string, OdfLocation> paths =
            new Dictionary<string, OdfLocation>(StringComparer.Ordinal);
        int index = 0;
        while (index < document.Parts.Length && !Stop)
        {
            OdfPart part = document.Parts[index];
            if (paths.ContainsKey(part.Path))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.UnexpectedElement,
                    "The document contains more than one materialized XML part with this path.",
                    CreatePartLocation(part.Path),
                    OdfValidationSeverity.Error,
                    paths[part.Path]);
            }
            else
            {
                paths.Add(part.Path, CreatePartLocation(part.Path));
            }
            if (!Stop)
            {
                ValidatePart(part, document.PackageKind);
            }
            index = index + 1;
        }
        if (!Stop && document.Parts.Length == 0)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MissingRequiredContent,
                "The document has no materialized XML part.",
                new OdfLocation(),
                OdfValidationSeverity.Error,
                null);
        }
    }

    void ValidatePart(OdfPart part, OdfPackageKind packageKind)
    {
        ValidateCancellation();
        if (Stop)
        {
            return;
        }
        ValidatedPartCount = ValidatedPartCount + 1;
        OdfNode? root = FindPartRoot(part);
        if (root == null || Stop)
        {
            return;
        }
        ValidateRootForPart(root, part, packageKind);
        if (Stop)
        {
            return;
        }
        Dictionary<string, OdfLocation> ids =
            new Dictionary<string, OdfLocation>(StringComparer.Ordinal);
        int nodeIndex = 0;
        while (nodeIndex < part.Nodes.Length && !Stop)
        {
            OdfNode topLevel = part.Nodes[nodeIndex];
            string topLevelPath = NodePath("", topLevel, nodeIndex + 1);
            ValidateNode(
                topLevel,
                null,
                new XmlNamespaceFrame(null),
                ids,
                part.Path,
                topLevelPath,
                1);
            nodeIndex = nodeIndex + 1;
        }
        if (!Stop)
        {
            ValidateSchema(root, part);
        }
    }

    OdfNode? FindPartRoot(OdfPart part)
    {
        OdfNode? root = null;
        int index = 0;
        while (index < part.Nodes.Length && !Stop)
        {
            OdfNode node = part.Nodes[index];
            if (node.Kind == OdfNodeKind.Element)
            {
                if (root == null)
                {
                    root = node;
                }
                else
                {
                    AddDiagnostic(
                        OdfDiagnosticCodes.MalformedXml,
                        "An XML part must contain exactly one top-level element.",
                        LocationForNode(node, part.Path, NodePath("", node, 1)),
                        OdfValidationSeverity.Error,
                        LocationForNode(root, part.Path, NodePath("", root, 1)));
                }
            }
            else if (node.Kind == OdfNodeKind.Text
                && !IsWhitespace(node.Value))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.MalformedXml,
                    "Non-whitespace text is not allowed outside the document element.",
                    LocationForNode(node, part.Path, "/text()"),
                    OdfValidationSeverity.Error,
                    null);
            }
            index = index + 1;
        }
        if (root == null && !Stop)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MissingRequiredContent,
                "The XML part does not contain a document element.",
                CreatePartLocation(part.Path),
                OdfValidationSeverity.Error,
                null);
        }
        return root;
    }

    void ValidateRootForPart(
        OdfNode root,
        OdfPart part,
        OdfPackageKind packageKind)
    {
        if (root.Name == null)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.InvalidXmlName,
                "The XML document element has no name.",
                LocationForNode(root, part.Path, "/"),
                OdfValidationSeverity.Error,
                null);
            return;
        }
        bool formulaRoot = root.Name.NamespaceName == OdfNamespaces.Math
            && root.Name.LocalName == "math"
            && IsNamedPart(part.Path, OdfPartNames.Content);
        if (formulaRoot)
        {
            return;
        }
        if (root.Name.NamespaceName != OdfNamespaces.Office
            || !IsStandardRootName(root.Name.LocalName))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.UnexpectedElement,
                "The XML part does not have a standard OpenDocument office root.",
                LocationForNode(root, part.Path, NodePath("", root, 1)),
                OdfValidationSeverity.Error,
                null);
            return;
        }
        string expected = ExpectedRootName(part.Path);
        if (part.Path.Length == 0 || packageKind == OdfPackageKind.Flat)
        {
            expected = "document";
        }
        if (expected.Length > 0 && root.Name.LocalName != expected)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.UnexpectedElement,
                "The XML document element does not match its package part.",
                LocationForNode(root, part.Path, NodePath("", root, 1)),
                OdfValidationSeverity.Error,
                null);
        }
    }

    void ValidateNode(
        OdfNode node,
        OdfNode? expectedParent,
        XmlNamespaceFrame parentScope,
        Dictionary<string, OdfLocation> ids,
        string partPath,
        string nodePath,
        int depth)
    {
        if (Stop)
        {
            return;
        }
        ValidateCancellation();
        if (Stop)
        {
            return;
        }
        if (VisitedNodes.Contains(node))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "The XML tree contains a cycle or a node shared by multiple parents.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Fatal,
                null);
            return;
        }
        VisitedNodes.Add(node);
        if (!object.ReferenceEquals(node.Parent, expectedParent))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "The XML node parent link is inconsistent with the tree.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
        }
        if (Stop)
        {
            return;
        }
        if (depth > Options.SecurityLimits.MaximumXmlDepth)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.XmlDepthLimitExceeded,
                "The XML tree exceeds the configured element depth limit.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Fatal,
                null);
            return;
        }
        if (node.Kind == OdfNodeKind.Element)
        {
            ValidateElement(node, parentScope, ids, partPath, nodePath, depth);
            return;
        }
        ValidateScalar(node, partPath, nodePath);
    }

    void ValidateElement(
        OdfNode node,
        XmlNamespaceFrame parentScope,
        Dictionary<string, OdfLocation> ids,
        string partPath,
        string nodePath,
        int depth)
    {
        XmlNamespaceFrame scope = new XmlNamespaceFrame(parentScope);
        if (node.Value.Length > 0)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "An XML element cannot carry a scalar node value.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
        }
        if (Stop)
        {
            return;
        }
        ValidateDeclarations(node, scope, partPath, nodePath);
        if (!Stop)
        {
            ValidateElementName(node, scope, partPath, nodePath);
        }
        if (!Stop)
        {
            ValidateAttributes(node, scope, ids, partPath, nodePath);
        }
        int index = 0;
        while (index < node.Children.Length && !Stop)
        {
            OdfNode child = node.Children[index];
            string childPath = NodePath(nodePath, child, SiblingNumber(node, index));
            ValidateNode(
                child,
                node,
                scope,
                ids,
                partPath,
                childPath,
                child.Kind == OdfNodeKind.Element ? depth + 1 : depth);
            index = index + 1;
        }
    }

    void ValidateDeclarations(
        OdfNode node,
        XmlNamespaceFrame scope,
        string partPath,
        string nodePath)
    {
        int index = 0;
        while (index < node.NamespaceDeclarations.Length && !Stop)
        {
            OdfNamespaceDeclaration declaration =
                node.NamespaceDeclarations[index];
            OdfLocation location = LocationForDeclaration(
                declaration,
                partPath,
                nodePath);
            if (!OdfNameRules.IsNamespaceDeclarationValue(declaration))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.InvalidNamespace,
                    "The XML namespace declaration is invalid.",
                    location,
                    OdfValidationSeverity.Error,
                    null);
            }
            else if (scope.ContainsLocalPrefix(declaration.Prefix))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.InvalidNamespace,
                    "The XML element declares the same namespace prefix more than once.",
                    location,
                    OdfValidationSeverity.Error,
                    null);
            }
            else
            {
                scope.AddBinding(
                    declaration.Prefix,
                    declaration.NamespaceName);
            }
            index = index + 1;
        }
    }

    void ValidateElementName(
        OdfNode node,
        XmlNamespaceFrame scope,
        string partPath,
        string nodePath)
    {
        if (node.Name == null
            || !OdfNameRules.IsQualifiedNameValue(node.Name))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.InvalidXmlName,
                "The XML element name is invalid.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
            return;
        }
        OdfQualifiedName name = node.Name;
        bool validBinding = true;
        if (name.Prefix.Length == 0 && name.NamespaceName.Length == 0)
        {
            validBinding = !scope.HasBinding("")
                || scope.FindNamespace("").Length == 0;
        }
        else
        {
            validBinding = scope.HasBinding(name.Prefix)
                && scope.FindNamespace(name.Prefix) == name.NamespaceName;
        }
        if (!validBinding)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.InvalidNamespace,
                "The XML element prefix is not bound to its namespace name.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
        }
    }

    void ValidateAttributes(
        OdfNode node,
        XmlNamespaceFrame scope,
        Dictionary<string, OdfLocation> ids,
        string partPath,
        string nodePath)
    {
        if (node.Attributes.Length
            > Options.SecurityLimits.MaximumXmlAttributesPerElement)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.XmlAttributeLimitExceeded,
                "The XML element exceeds the configured attribute limit.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Fatal,
                null);
        }
        Dictionary<string, OdfLocation> names =
            new Dictionary<string, OdfLocation>(StringComparer.Ordinal);
        string attributeId = "";
        bool hasAttributeId = false;
        OdfLocation attributeIdLocation = new OdfLocation();
        int index = 0;
        while (index < node.Attributes.Length && !Stop)
        {
            OdfAttribute attribute = node.Attributes[index];
            OdfLocation location = LocationForAttribute(
                attribute,
                partPath,
                nodePath);
            if (!OdfNameRules.IsQualifiedNameValue(attribute.Name))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.InvalidXmlName,
                    "The XML attribute name is invalid.",
                    location,
                    OdfValidationSeverity.Error,
                    null);
            }
            else
            {
                ValidateAttributeNamespace(attribute, scope, location);
                string key = attribute.Name.NamespaceName
                    + "\u001f"
                    + attribute.Name.LocalName;
                if (names.ContainsKey(key))
                {
                    AddDiagnostic(
                        OdfDiagnosticCodes.MalformedXml,
                        "The XML element has duplicate expanded attribute names.",
                        location,
                        OdfValidationSeverity.Error,
                        names[key]);
                }
                else
                {
                    names.Add(key, location);
                }
                if (attribute.Name.NamespaceName == OdfNamespaces.Xml
                    && attribute.Name.LocalName == "id")
                {
                    hasAttributeId = true;
                    attributeId = attribute.Value;
                    attributeIdLocation = location;
                }
            }
            index = index + 1;
        }
        if (!Stop)
        {
            ValidateElementId(
                node,
                attributeId,
                hasAttributeId,
                attributeIdLocation,
                ids,
                partPath,
                nodePath);
        }
    }

    void ValidateAttributeNamespace(
        OdfAttribute attribute,
        XmlNamespaceFrame scope,
        OdfLocation location)
    {
        OdfQualifiedName name = attribute.Name;
        if (name.Prefix.Length == 0)
        {
            if (name.NamespaceName.Length > 0)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.InvalidNamespace,
                    "An unprefixed XML attribute cannot have a namespace name.",
                    location,
                    OdfValidationSeverity.Error,
                    null);
            }
            return;
        }
        if (!scope.HasBinding(name.Prefix)
            || scope.FindNamespace(name.Prefix) != name.NamespaceName)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.InvalidNamespace,
                "The XML attribute prefix is not bound to its namespace name.",
                location,
                OdfValidationSeverity.Error,
                null);
        }
    }

    void ValidateElementId(
        OdfNode node,
        string attributeId,
        bool hasAttributeId,
        OdfLocation attributeLocation,
        Dictionary<string, OdfLocation> ids,
        string partPath,
        string nodePath)
    {
        string value = attributeId;
        OdfLocation location = attributeLocation;
        if (!hasAttributeId && node.XmlId.Length > 0)
        {
            value = node.XmlId;
            location = LocationForNode(node, partPath, nodePath);
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "The XML ID cache does not correspond to an xml:id attribute.",
                location,
                OdfValidationSeverity.Error,
                null);
        }
        else if (hasAttributeId && node.XmlId != attributeId)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "The XML ID cache does not match the xml:id attribute.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                attributeLocation);
        }
        if (Stop)
        {
            return;
        }
        if (!hasAttributeId && value.Length == 0)
        {
            return;
        }
        if (!OdfNameRules.IsXmlId(value))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.InvalidXmlName,
                "The xml:id value is not a valid XML ID.",
                location,
                OdfValidationSeverity.Error,
                null);
            return;
        }
        if (ids.ContainsKey(value))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.DuplicateXmlId,
                "The xml:id value is duplicated in this XML part.",
                location,
                OdfValidationSeverity.Error,
                ids[value]);
        }
        else
        {
            ids.Add(value, location);
        }
    }

    void ValidateScalar(OdfNode node, string partPath, string nodePath)
    {
        if (node.Attributes.Length > 0
            || node.NamespaceDeclarations.Length > 0
            || node.Children.Length > 0
            || node.XmlId.Length > 0)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "A non-element XML node contains element-only data.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
        }
        if (Stop)
        {
            return;
        }
        if (node.Value.Length > Options.SecurityLimits.MaximumXmlTextLength)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.XmlTextLimitExceeded,
                "The XML scalar value exceeds the configured text limit.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Fatal,
                null);
        }
        if (Stop)
        {
            return;
        }
        if (node.Kind == OdfNodeKind.ProcessingInstruction)
        {
            if (node.Name == null
                || !OdfNameRules.IsNcName(node.Name.LocalName)
                || node.Name.Prefix.Length > 0
                || node.Name.NamespaceName.Length > 0
                || string.Equals(
                    node.Name.LocalName,
                    "xml",
                    StringComparison.OrdinalIgnoreCase))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.InvalidXmlName,
                    "The XML processing-instruction target is invalid.",
                    LocationForNode(node, partPath, nodePath),
                    OdfValidationSeverity.Error,
                    null);
            }
            if (Stop)
            {
                return;
            }
            if (node.Value.Contains("?>", StringComparison.Ordinal))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.MalformedXml,
                    "The processing-instruction value contains its closing delimiter.",
                    LocationForNode(node, partPath, nodePath),
                    OdfValidationSeverity.Error,
                    null);
            }
            return;
        }
        if (node.Kind == OdfNodeKind.Comment)
        {
            if (node.Name != null)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.MalformedXml,
                    "An XML comment cannot have a qualified name.",
                    LocationForNode(node, partPath, nodePath),
                    OdfValidationSeverity.Error,
                    null);
            }
            if (Stop)
            {
                return;
            }
            if (node.Value.Contains("--", StringComparison.Ordinal)
                || node.Value.EndsWith("-", StringComparison.Ordinal))
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.MalformedXml,
                    "The XML comment value is not well formed.",
                    LocationForNode(node, partPath, nodePath),
                    OdfValidationSeverity.Error,
                    null);
            }
            return;
        }
        if (node.Kind != OdfNodeKind.Text)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "The XML tree contains an unsupported node kind.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
        }
        else if (node.Name != null)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.MalformedXml,
                "An XML text node cannot have a qualified name.",
                LocationForNode(node, partPath, nodePath),
                OdfValidationSeverity.Error,
                null);
        }
    }

    void ValidateSchema(OdfNode root, OdfPart part)
    {
        if (root.Name != null
            && root.Name.NamespaceName == OdfNamespaces.Math
            && root.Name.LocalName == "math"
            && IsNamedPart(part.Path, OdfPartNames.Content))
        {
            return;
        }
        OdfVersion version = SelectPartVersion(part);
        if (version == OdfVersion.Future
            && Options.Version == OdfVersion.Unknown
            && PreserveFutureContent)
        {
            return;
        }
        if (!IsKnownVersion(version))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.VersionUnsupported,
                "The XML part does not select a supported ODF schema version.",
                LocationForNode(root, part.Path, NodePath("", root, 1)),
                OdfValidationSeverity.Error,
                null);
            return;
        }
        SchemaRuleSet? rules = OdfSchemaRuleCatalog.GetRuleSet(
            version,
            "document");
        if (rules == null)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.VersionUnsupported,
                "No document schema is available for the selected ODF version.",
                LocationForNode(root, part.Path, NodePath("", root, 1)),
                OdfValidationSeverity.Error,
                null);
            return;
        }
        SchemaValidationLimits limits = new SchemaValidationLimits();
        limits.MaximumElementDepth = Options.SecurityLimits.MaximumXmlDepth;
        limits.MaximumDatatypeLength = Options.SecurityLimits.MaximumXmlTextLength;
        SchemaValidationResult validation =
            new SchemaPatternValidator(limits).ValidateNode(root, rules);
        if (validation.IsValid)
        {
            return;
        }
        string code = OdfDiagnosticCodes.UnexpectedElement;
        string message = "The XML part does not match the selected ODF schema.";
        OdfValidationSeverity severity = OdfValidationSeverity.Error;
        if (validation.LimitExceeded)
        {
            code = OdfDiagnosticCodes.OccurrenceViolation;
            message = "Schema validation exceeded its configured work or depth limit.";
            severity = OdfValidationSeverity.Fatal;
        }
        else if (validation.InvalidDatatype)
        {
            code = OdfDiagnosticCodes.InvalidDatatype;
            message = "An XML value does not match its ODF schema datatype.";
        }
        AddDiagnostic(
            code,
            message,
            LocationForNode(root, part.Path, NodePath("", root, 1)),
            severity,
            null);
    }

    void ValidateCancellation()
    {
        if (Options.Cancellation.IsCancellationRequested && !Stop)
        {
            string message = "XML validation was cancelled.";
            if (Options.Cancellation.Reason.Length > 0)
            {
                message = message + " " + Options.Cancellation.Reason;
            }
            AddDiagnostic(
                OdfDiagnosticCodes.OperationCancelled,
                message,
                new OdfLocation(),
                OdfValidationSeverity.Fatal,
                null);
            Stop = true;
        }
    }

    void AddDiagnostic(
        string code,
        string message,
        OdfLocation location,
        OdfValidationSeverity severity,
        OdfLocation? related)
    {
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            severity,
            location,
            related,
            DocumentVersion);
        Diagnostics.Add(diagnostic);
        if (IsFailFast()
            && (severity == OdfValidationSeverity.Error
                || severity == OdfValidationSeverity.Fatal))
        {
            Stop = true;
        }
    }

    OdfVersion SelectDocumentVersion(OdfDocument document)
    {
        if (Options.Version != OdfVersion.Unknown)
        {
            return Options.Version;
        }
        if (document.VersionInfo != null)
        {
            return document.VersionInfo.Version;
        }
        return OdfVersion.Unknown;
    }

    OdfVersion SelectPartVersion(OdfPart part)
    {
        if (Options.Version != OdfVersion.Unknown)
        {
            return Options.Version;
        }
        if (part.VersionInfo != null
            && part.VersionInfo.Version != OdfVersion.Unknown)
        {
            return part.VersionInfo.Version;
        }
        if (DocumentVersion == OdfVersion.Unknown)
        {
            return OdfVersion.Version10;
        }
        return DocumentVersion;
    }

    bool HasErrors()
    {
        foreach (OdfDiagnostic diagnostic in Diagnostics)
        {
            if (diagnostic.Severity == OdfValidationSeverity.Error
                || diagnostic.Severity == OdfValidationSeverity.Fatal)
            {
                return true;
            }
        }
        return false;
    }

    bool IsFailFast()
    {
        return Options.Mode == OdfValidationMode.Strict;
    }

    static bool OptionsAreValid(OdfValidationOptions? options)
    {
        return options != null
            && options.SecurityLimits != null
            && options.Cancellation != null
            && options.SecurityLimits.MaximumXmlDepth > 0
            && options.SecurityLimits.MaximumXmlAttributesPerElement > 0
            && options.SecurityLimits.MaximumXmlTextLength > 0;
    }

    static bool IsKnownVersion(OdfVersion version)
    {
        return version == OdfVersion.Version10
            || version == OdfVersion.Version11
            || version == OdfVersion.Version12
            || version == OdfVersion.Version13
            || version == OdfVersion.Version14;
    }

    static bool IsStandardRootName(string name)
    {
        return name == "document"
            || name == "document-content"
            || name == "document-styles"
            || name == "document-meta"
            || name == "document-settings";
    }

    static string ExpectedRootName(string path)
    {
        if (IsNamedPart(path, OdfPartNames.Content))
        {
            return "document-content";
        }
        if (IsNamedPart(path, OdfPartNames.Styles))
        {
            return "document-styles";
        }
        if (IsNamedPart(path, OdfPartNames.Metadata))
        {
            return "document-meta";
        }
        if (IsNamedPart(path, OdfPartNames.Settings))
        {
            return "document-settings";
        }
        return "";
    }

    static bool IsNamedPart(string path, string name)
    {
        return path == name
            || path.EndsWith("/" + name, StringComparison.Ordinal);
    }

    static int SiblingNumber(OdfNode parent, int childIndex)
    {
        OdfNode child = parent.Children[childIndex];
        int number = 1;
        int index = 0;
        while (index < childIndex)
        {
            OdfNode prior = parent.Children[index];
            if (prior.Kind == child.Kind
                && SameNodeName(prior, child))
            {
                number = number + 1;
            }
            index = index + 1;
        }
        return number;
    }

    static bool SameNodeName(OdfNode left, OdfNode right)
    {
        if (left.Name == null || right.Name == null)
        {
            return left.Name == null && right.Name == null;
        }
        return left.Name.NamespaceName == right.Name.NamespaceName
            && left.Name.LocalName == right.Name.LocalName;
    }

    static string NodePath(string parentPath, OdfNode node, int number)
    {
        string segment = "node()";
        if (node.Kind == OdfNodeKind.Element)
        {
            segment = "element()";
            if (node.Name != null)
            {
                segment = node.Name.LocalName;
                if (node.Name.Prefix.Length > 0)
                {
                    segment = node.Name.Prefix + ":" + segment;
                }
            }
        }
        else if (node.Kind == OdfNodeKind.Text)
        {
            segment = "text()";
        }
        else if (node.Kind == OdfNodeKind.Comment)
        {
            segment = "comment()";
        }
        else if (node.Kind == OdfNodeKind.ProcessingInstruction)
        {
            segment = "processing-instruction()";
        }
        return parentPath + "/" + segment + "[" + number + "]";
    }

    static bool IsWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != ' '
                && current != '\t'
                && current != '\r'
                && current != '\n')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static OdfLocation CreatePartLocation(string partPath)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = partPath;
        return location;
    }

    static OdfLocation LocationForNode(
        OdfNode node,
        string partPath,
        string nodePath)
    {
        return CopyLocation(node.Location, partPath, nodePath);
    }

    static OdfLocation LocationForAttribute(
        OdfAttribute attribute,
        string partPath,
        string nodePath)
    {
        return CopyLocation(attribute.Location, partPath, nodePath);
    }

    static OdfLocation LocationForDeclaration(
        OdfNamespaceDeclaration declaration,
        string partPath,
        string nodePath)
    {
        return CopyLocation(declaration.Location, partPath, nodePath);
    }

    static OdfLocation CopyLocation(
        OdfLocation source,
        string partPath,
        string nodePath)
    {
        OdfLocation result = new OdfLocation();
        result.PartPath = partPath;
        result.NodePath = source.NodePath.Length > 0
            ? source.NodePath
            : nodePath;
        result.XmlId = source.XmlId;
        result.Line = source.Line;
        result.Column = source.Column;
        result.ByteOffset = source.ByteOffset;
        result.TextOffset = source.TextOffset;
        result.TextLength = source.TextLength;
        return result;
    }
}
