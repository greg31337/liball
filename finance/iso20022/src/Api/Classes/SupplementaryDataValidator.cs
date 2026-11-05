using System.Collections.Generic;
using System.IO;
using System.Text;

namespace iso20022;

/// <summary>Discovers and preserves modern and legacy supplementary data in raw or generic messages.</summary>
public class SupplementaryDataValidator : ISupplementaryDataValidator
{
    /// <summary>Initializes a supplementary-data validator.</summary>
    public SupplementaryDataValidator()
    {
    }

    /// <summary>Processes one raw or generic message under explicit supplementary-data policy.</summary>
    /// <param name="request">The message, local schema services, policy, and resource limits.</param>
    /// <returns>The stable supplementary discovery and validation result.</returns>
    public SupplementaryValidationResult ValidateSupplementaryData(
        SupplementaryValidationRequest? request)
    {
        if (request == null)
        {
            return CreateOperationFailure(
                SupplementaryValidationStatus.Unknown,
                "supplementary.request_missing",
                "A supplementary validation request is required.");
        }
        if (request.Limits == null || !MessageValidator.LimitsArePositive(request.Limits))
        {
            return CreateOperationFailure(
                SupplementaryValidationStatus.Unknown,
                "supplementary.limits_invalid",
                "Every supplementary processing resource limit must be positive.");
        }
        if (request.Policies == null ||
            (request.Profile != null &&
                request.Profile.SupplementaryDataPolicies == null))
        {
            return CreateOperationFailure(
                SupplementaryValidationStatus.Invalid,
                "supplementary.policy_invalid",
                "Supplementary caller and profile policy collections cannot be null.");
        }
        if (request.Xml == null)
        {
            return CreateOperationFailure(
                SupplementaryValidationStatus.Unknown,
                "supplementary.xml_invalid",
                "The supplementary XML byte collection must not be null.");
        }
        if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
        {
            return CreateOperationFailure(
                SupplementaryValidationStatus.ResourceLimitReached,
                "supplementary.total_bytes_limit",
                "The supplementary XML input exceeds the configured byte limit.");
        }

        DynamicXmlNode? tree = CreateInputTree(request);
        if (tree == null)
        {
            return CreateOperationFailure(
                SupplementaryValidationStatus.Unknown,
                "supplementary.input_missing",
                "Raw XML bytes or a generic message document are required.");
        }

        SupplementaryValidationResult result = new SupplementaryValidationResult();
        result.Success = true;
        result.Valid = true;
        result.Status = SupplementaryValidationStatus.NotPresent;
        SupplementaryDiscoveryState state =
            new SupplementaryDiscoveryState(request, result, tree);
        XmlPathTracker path = new XmlPathTracker();
        NamespaceScopeTracker namespaces = new NamespaceScopeTracker();
        int index = 0;
        while (state.Continue && index < tree.Children.Count)
        {
            TraverseNode(
                tree.Children[index],
                state,
                path,
                namespaces,
                null,
                1,
                CountEarlierSupplementarySiblings(tree, index));
            index++;
        }

        if (result.Success && result.Valid && result.Items.Count > 0)
        {
            result.Status = DetermineValidStatus(result.Items);
        }
        return result;
    }

    internal static DynamicXmlNode? CreateInputTree(
        SupplementaryValidationRequest request)
    {
        if (request.Xml.Length > 0)
        {
            MessageReadOptions options = new MessageReadOptions();
            options.NamespaceMode = NamespaceMode.Strict;
            options.UnknownContentMode = UnknownContentMode.Preserve;
            options.Limits = request.Limits;
            options.PreserveComments = true;
            options.PreserveProcessingInstructions = true;
            options.PreserveWhitespace = true;
            options.PreserveLexicalValues = true;
            return new GenericXmlNodeReader(options).ReadDocumentFromBytes(
                request.Xml,
                null);
        }
        if (request.Document == null)
        {
            return null;
        }

        MessageWriteOptions writeOptions = new MessageWriteOptions();
        writeOptions.UnknownContentMode = UnknownContentMode.Preserve;
        writeOptions.Limits = request.Limits;
        return XmlMessageWriter.BuildDocumentTree(request.Document, writeOptions);
    }

    internal static void TraverseNode(
        DynamicXmlNode node,
        SupplementaryDiscoveryState state,
        XmlPathTracker path,
        NamespaceScopeTracker namespaces,
        DynamicXmlNode? parent,
        int depth,
        int occurrenceIndex)
    {
        if (!state.Continue)
        {
            return;
        }
        if (node.Kind != XmlNodeKind.Element)
        {
            ValidateValueNode(node, state);
            return;
        }
        if (!RecordElement(node, state, depth))
        {
            return;
        }

        namespaces.EnterScope();
        AddDeclarations(node, namespaces);
        path.EnterElement(node.Name, node.Location);
        if (IsSupplementaryContainer(node))
        {
            DiscoverContainer(
                node,
                state,
                path,
                namespaces,
                parent,
                occurrenceIndex);
        }
        else
        {
            int index = 0;
            while (state.Continue && index < node.Children.Count)
            {
                TraverseNode(
                    node.Children[index],
                    state,
                    path,
                    namespaces,
                    node,
                    depth + 1,
                    CountEarlierSupplementarySiblings(node, index));
                index++;
            }
        }
        path.ExitElement();
        namespaces.ExitScope();
    }

    internal static bool RecordElement(
        DynamicXmlNode node,
        SupplementaryDiscoveryState state,
        int depth)
    {
        ResourceLimits limits = state.Request.Limits;
        if (depth > limits.MaxDepth)
        {
            SetResourceFailure(
                state,
                "supplementary.depth_limit",
                "The supplementary source exceeds the configured element depth.");
            return false;
        }
        state.ElementCount++;
        if (state.ElementCount > limits.MaxElementCount)
        {
            SetResourceFailure(
                state,
                "supplementary.element_limit",
                "The supplementary source exceeds the configured element count.");
            return false;
        }
        if (node.Attributes.Count + node.NamespaceDeclarations.Count >
            limits.MaxAttributesPerElement)
        {
            SetResourceFailure(
                state,
                "supplementary.attribute_limit",
                "A supplementary source element exceeds the configured attribute count.");
            return false;
        }
        if (node.Children.Count > limits.MaxCollectionCount)
        {
            SetResourceFailure(
                state,
                "supplementary.collection_limit",
                "A supplementary source collection exceeds the configured item count.");
            return false;
        }

        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            if (node.Attributes[attributeIndex].Value.Length > limits.MaxTextLength)
            {
                SetResourceFailure(
                    state,
                    "supplementary.text_limit",
                    "A supplementary source value exceeds the configured text length.");
                return false;
            }
            attributeIndex++;
        }
        return true;
    }

    internal static void ValidateValueNode(
        DynamicXmlNode node,
        SupplementaryDiscoveryState state)
    {
        if (node.Value.Length > state.Request.Limits.MaxTextLength)
        {
            SetResourceFailure(
                state,
                "supplementary.text_limit",
                "A supplementary source value exceeds the configured text length.");
        }
    }

    internal static bool IsSupplementaryContainer(DynamicXmlNode node)
    {
        if (node.Name.LocalName == "SplmtryData")
        {
            return true;
        }
        if (node.Name.LocalName != "Xtsn")
        {
            return false;
        }
        return FindDirectElement(node, "Txt") != null ||
            FindDirectElement(node, "Envlp") != null;
    }

    internal static DynamicXmlNode? FindDirectElement(
        DynamicXmlNode parent,
        string localName)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            DynamicXmlNode child = parent.Children[index];
            if (child.Kind == XmlNodeKind.Element &&
                child.Name.LocalName == localName &&
                child.Name.NamespaceUri == parent.Name.NamespaceUri)
            {
                return child;
            }
            index++;
        }
        return null;
    }

    internal static void DiscoverContainer(
        DynamicXmlNode container,
        SupplementaryDiscoveryState state,
        XmlPathTracker path,
        NamespaceScopeTracker namespaces,
        DynamicXmlNode? parent,
        int occurrenceIndex)
    {
        if (state.Result.Items.Count >= state.Request.Limits.MaxCollectionCount)
        {
            SetResourceFailure(
                state,
                "supplementary.item_limit",
                "The discovered supplementary item count exceeds the configured limit.");
            return;
        }

        SupplementaryDataItem item = new SupplementaryDataItem();
        item.ContainerPath = path.CapturePath();
        item.OccurrenceIndex = occurrenceIndex;
        item.IsLegacyForm = container.Name.LocalName == "Xtsn";
        item.IsLocal = parent == null || parent.Name.LocalName != "Document";
        item.Status = SupplementaryValidationStatus.Valid;
        DynamicXmlNode? placeAndName = FindDirectElement(container, "PlcAndNm");
        if (placeAndName != null)
        {
            item.PlaceAndName = NamespaceService.TrimXmlWhitespace(
                XmlMessageReader.CollectElementText(placeAndName));
        }
        ResolvePlaceAndName(
            item,
            parent,
            state,
            namespaces,
            container.Location);

        DynamicXmlNode? text = FindDirectElement(container, "Txt");
        DynamicXmlNode? envelope = FindDirectElement(container, "Envlp");
        if (state.Continue)
        {
            if (item.IsLegacyForm && text != null)
            {
                DiscoverLegacyText(text, item, state, namespaces);
            }
            else if (envelope != null)
            {
                DiscoverEnvelope(envelope, item, state, namespaces);
            }
            else
            {
                item.Status = SupplementaryValidationStatus.Invalid;
                AddDiagnostic(
                    state,
                    "supplementary.envelope_missing",
                    "Supplementary data requires an envelope or legacy text value.",
                    "One Envlp element, or one Txt element for legacy Extension1.",
                    item.ContainerPath,
                    container.Location,
                    SupplementaryValidationStatus.Invalid,
                    DiagnosticKind.MissingContent);
            }
        }
        if (state.Continue && item.Status == SupplementaryValidationStatus.Valid)
        {
            ApplyPolicyAndSchema(item, state, container.Location);
        }
        if (!state.Request.PreserveContent)
        {
            item.Envelope = null;
            item.Payload = null;
        }

        state.Result.Items.Add(item);
    }

    internal static void DiscoverLegacyText(
        DynamicXmlNode text,
        SupplementaryDataItem item,
        SupplementaryDiscoveryState state,
        NamespaceScopeTracker namespaces)
    {
        item.PayloadName = XmlPathTracker.CopyName(text.Name);
        item.NamespaceUri = string.Empty;
        namespaces.EnterScope();
        AddDeclarations(text, namespaces);
        if (state.Request.PreserveContent)
        {
            item.Payload = CreateRawFragment(text, namespaces, state.Request.Limits);
        }
        namespaces.ExitScope();
        string value = NamespaceService.TrimXmlWhitespace(
            XmlMessageReader.CollectElementText(text));
        if (value.Length == 0)
        {
            item.Status = SupplementaryValidationStatus.Invalid;
            AddDiagnostic(
                state,
                "supplementary.legacy_text_missing",
                "Legacy Extension1 supplementary data requires text content.",
                "One non-empty Txt value.",
                item.ContainerPath,
                text.Location,
                SupplementaryValidationStatus.Invalid,
                DiagnosticKind.MissingContent);
        }
    }

    internal static void DiscoverEnvelope(
        DynamicXmlNode envelope,
        SupplementaryDataItem item,
        SupplementaryDiscoveryState state,
        NamespaceScopeTracker namespaces)
    {
        item.EnvelopeName = XmlPathTracker.CopyName(envelope.Name);
        namespaces.EnterScope();
        AddDeclarations(envelope, namespaces);
        if (state.Request.PreserveContent)
        {
            item.Envelope = CreateRawFragment(
                envelope,
                namespaces,
                state.Request.Limits);
        }

        DynamicXmlNode? payload = null;
        int payloadCount = 0;
        int index = 0;
        while (index < envelope.Children.Count)
        {
            DynamicXmlNode child = envelope.Children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                if (payload == null)
                {
                    payload = child;
                }
                payloadCount++;
            }
            index++;
        }
        if (payload == null)
        {
            item.Status = SupplementaryValidationStatus.Invalid;
            AddDiagnostic(
                state,
                "supplementary.payload_missing",
                "The supplementary envelope does not contain an extension payload.",
                "One namespace-qualified extension payload root.",
                item.ContainerPath,
                envelope.Location,
                SupplementaryValidationStatus.Invalid,
                DiagnosticKind.MissingContent);
        }
        else
        {
            item.PayloadName = XmlPathTracker.CopyName(payload.Name);
            item.NamespaceUri = payload.Name.NamespaceUri;
            namespaces.EnterScope();
            AddDeclarations(payload, namespaces);
            if (state.Request.PreserveContent || state.Request.ValidateSchemas)
            {
                item.Payload = CreateRawFragment(
                    payload,
                    namespaces,
                    state.Request.Limits);
            }
            namespaces.ExitScope();
            if (payload.Name.NamespaceUri.Length == 0)
            {
                item.Status = SupplementaryValidationStatus.Invalid;
                AddDiagnostic(
                    state,
                    "supplementary.payload_namespace_missing",
                    "The structured supplementary payload is not namespace-qualified.",
                    "One extension payload root with a non-empty namespace URI.",
                    item.ContainerPath,
                    payload.Location,
                    SupplementaryValidationStatus.Invalid,
                    DiagnosticKind.InvalidValue);
            }
            else if (payloadCount > 1)
            {
                item.Status = SupplementaryValidationStatus.Invalid;
                AddDiagnostic(
                    state,
                    "supplementary.payload_ambiguous",
                    "The supplementary envelope contains multiple payload roots.",
                    "Exactly one extension payload root.",
                    item.ContainerPath,
                    envelope.Location,
                    SupplementaryValidationStatus.Invalid,
                    DiagnosticKind.Cardinality);
            }
        }
        namespaces.ExitScope();
    }

    internal static RawXmlFragment CreateRawFragment(
        DynamicXmlNode node,
        NamespaceScopeTracker namespaces,
        ResourceLimits limits)
    {
        DynamicXmlNode copy = CanonicalXmlNodeNormalizer.CloneNode(node);
        List<NamespaceDeclarationData> declarations =
            namespaces.CaptureDeclarations();
        copy.NamespaceDeclarations =
            GenericXmlNodeWriter.CopyDeclarations(declarations);
        MessageWriteOptions options = new MessageWriteOptions();
        options.FormattingMode = XmlFormattingMode.Compact;
        options.UnknownContentMode = UnknownContentMode.Preserve;
        options.Limits = limits;
        options.WriteXmlDeclaration = false;
        options.WriteByteOrderMark = false;
        options.PreservePrefixes = true;
        options.PreserveLexicalValues = true;
        string xml = new GenericXmlNodeWriter(options).WriteText(copy);
        RawXmlFragment fragment = new RawXmlFragment();
        fragment.Xml = xml;
        fragment.Utf8Bytes = Encoding.UTF8.GetBytes(xml);
        fragment.RootName = XmlPathTracker.CopyName(copy.Name);
        fragment.NamespaceDeclarations =
            GenericXmlNodeWriter.CopyDeclarations(declarations);
        fragment.Location = XmlPathTracker.CopyLocation(node.Location);
        return fragment;
    }

    internal static void ApplyPolicyAndSchema(
        SupplementaryDataItem item,
        SupplementaryDiscoveryState state,
        SourceLocation location)
    {
        if (item.NamespaceUri.Length == 0)
        {
            return;
        }

        SupplementaryDataPolicy? policy = FindPolicy(
            state.Request.Policies,
            item.NamespaceUri);
        bool profilePolicy = false;
        ProfileDefinition? profile = state.Request.Profile;
        if (policy == null && profile != null)
        {
            policy = FindPolicy(
                profile.SupplementaryDataPolicies,
                item.NamespaceUri);
            profilePolicy = policy != null;
        }
        if (policy != null)
        {
            item.CanIgnore = policy.CanIgnore;
            item.MustUnderstand = policy.MustUnderstand;
            if (policy.Schema != null)
            {
                if (policy.Schema.NamespaceUri != item.NamespaceUri)
                {
                    item.Status = SupplementaryValidationStatus.Invalid;
                    AddDiagnostic(
                        state,
                        "supplementary.policy_schema_namespace_mismatch",
                        "The selected supplementary schema namespace does not match the payload namespace.",
                        item.NamespaceUri,
                        item.ContainerPath,
                        location,
                        SupplementaryValidationStatus.Invalid,
                        DiagnosticKind.Configuration);
                    DecoratePolicyDiagnostic(state, profilePolicy);
                    return;
                }
                item.Schema = policy.Schema;
            }
        }

        SchemaCompilationResult? lookup = LookupSchema(item, policy, state);
        if (lookup != null && !lookup.Success)
        {
            HandleUnavailableSchema(
                item,
                state,
                location,
                "supplementary.schema_lookup_failed",
                lookup.ErrorMessage,
                profilePolicy);
            return;
        }
        if (lookup != null && lookup.Found && lookup.RootSchema != null)
        {
            item.Schema = lookup.RootSchema;
        }

        bool configured = policy != null || state.Request.SchemaRegistry != null ||
            HasProfilePolicies(profile);
        if (policy == null && item.Schema == null)
        {
            if (!configured)
            {
                return;
            }
            item.Status = SupplementaryValidationStatus.UnknownNamespace;
            AddDiagnostic(
                state,
                "supplementary.namespace_unknown",
                "No registered extension or policy matches the supplementary payload namespace.",
                "A registered schema or explicit supplementary namespace policy.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.UnknownNamespace,
                DiagnosticKind.Applicability);
            DecoratePolicyDiagnostic(state, HasProfilePolicies(profile));
            return;
        }

        bool validateSchema = state.Request.ValidateSchemas;
        if (policy != null && !policy.ValidateSchema)
        {
            validateSchema = false;
        }
        if (!validateSchema)
        {
            return;
        }
        if (item.Schema == null || state.Request.XsdValidator == null ||
            item.Payload == null)
        {
            HandleUnavailableSchema(
                item,
                state,
                location,
                "supplementary.schema_unavailable",
                "The selected supplementary policy requires a local schema validator and schema.",
                profilePolicy);
            return;
        }

        SchemaRegistryOptions options = CreateSchemaOptions(state.Request.Limits);
        SchemaCompilationResult validation;
        using (MemoryStream input = new MemoryStream(
            item.Payload.Utf8Bytes,
            false))
        {
            validation = state.Request.XsdValidator.ValidateXmlStream(
                input,
                item.Schema,
                options);
        }
        if (validation.Success && validation.Found)
        {
            return;
        }
        if (validation.ErrorCode == "schema.validation.resource_limit" ||
            validation.ErrorCode == "schema.validation.diagnostic_limit")
        {
            item.Status = SupplementaryValidationStatus.ResourceLimitReached;
            SetResourceFailure(
                state,
                "supplementary.schema_resource_limit",
                validation.ErrorMessage);
            return;
        }
        if (!validation.Found ||
            validation.ErrorCode != "schema.validation.invalid")
        {
            HandleUnavailableSchema(
                item,
                state,
                location,
                "supplementary.schema_unavailable",
                validation.ErrorMessage,
                profilePolicy);
            return;
        }

        item.Status = SupplementaryValidationStatus.SchemaInvalid;
        string message = validation.ErrorMessage;
        if (message.Length == 0)
        {
            message = "The supplementary payload does not satisfy its selected schema.";
        }
        AddDiagnostic(
            state,
            "supplementary.schema_invalid",
            message,
            "A payload valid against the selected supplementary schema closure.",
            item.ContainerPath,
            location,
            SupplementaryValidationStatus.SchemaInvalid,
            DiagnosticKind.ValidationFailure);
        DecorateSchemaDiagnostic(state, item.Schema, profilePolicy);
    }

    internal static SupplementaryDataPolicy? FindPolicy(
        List<SupplementaryDataPolicy> policies,
        string namespaceUri)
    {
        int index = 0;
        while (index < policies.Count)
        {
            if (policies[index].NamespaceUri == namespaceUri)
            {
                return policies[index];
            }
            index++;
        }
        return null;
    }

    internal static SchemaCompilationResult? LookupSchema(
        SupplementaryDataItem item,
        SupplementaryDataPolicy? policy,
        SupplementaryDiscoveryState state)
    {
        ISchemaRegistry? registry = state.Request.SchemaRegistry;
        if (registry == null)
        {
            return null;
        }
        SchemaArtifact selector;
        if (policy != null && policy.Schema != null)
        {
            selector = policy.Schema;
        }
        else
        {
            selector = new SchemaArtifact();
            selector.NamespaceUri = item.NamespaceUri;
            if (state.Request.Profile != null)
            {
                selector.Profile = state.Request.Profile.Identifier;
            }
        }
        SchemaRegistryOptions options = CreateSchemaOptions(state.Request.Limits);
        SchemaCompilationResult lookup = registry.LookupSchema(selector, options);
        if (lookup.Success && !lookup.Found && selector.Profile.Length > 0 &&
            (policy == null || policy.Schema == null))
        {
            selector.Profile = string.Empty;
            lookup = registry.LookupSchema(selector, options);
        }
        return lookup;
    }

    internal static SchemaRegistryOptions CreateSchemaOptions(ResourceLimits limits)
    {
        SchemaRegistryOptions options = new SchemaRegistryOptions();
        options.Limits = limits;
        return options;
    }

    internal static void HandleUnavailableSchema(
        SupplementaryDataItem item,
        SupplementaryDiscoveryState state,
        SourceLocation location,
        string code,
        string message,
        bool profilePolicy)
    {
        if (item.MustUnderstand)
        {
            item.Status = SupplementaryValidationStatus.MustUnderstandRejected;
            AddDiagnostic(
                state,
                "supplementary.must_understand_rejected",
                message,
                "A locally understood and accepted supplementary payload.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.MustUnderstandRejected,
                DiagnosticKind.Applicability);
            DecorateSchemaDiagnostic(state, item.Schema, profilePolicy);
            return;
        }
        if (item.CanIgnore)
        {
            item.Status = SupplementaryValidationStatus.Ignored;
            return;
        }

        item.Status = SupplementaryValidationStatus.SchemaUnavailable;
        AddDiagnostic(
            state,
            code,
            message,
            "A locally available supplementary schema and validator.",
            item.ContainerPath,
            location,
            SupplementaryValidationStatus.SchemaUnavailable,
            DiagnosticKind.Configuration);
        DecorateSchemaDiagnostic(state, item.Schema, profilePolicy);
    }

    internal static void DecorateSchemaDiagnostic(
        SupplementaryDiscoveryState state,
        SchemaArtifact? schema,
        bool profilePolicy)
    {
        if (!state.Continue || state.Result.Diagnostics.Count == 0)
        {
            return;
        }
        ValidationDiagnostic diagnostic =
            state.Result.Diagnostics[state.Result.Diagnostics.Count - 1];
        diagnostic.RuleIdentifier = "iso20022.supplementary.schema";
        if (schema != null)
        {
            diagnostic.SourceArtifact = schema.Artifact;
        }
        DecoratePolicyDiagnostic(state, profilePolicy);
    }

    internal static void DecoratePolicyDiagnostic(
        SupplementaryDiscoveryState state,
        bool profilePolicy)
    {
        if (!state.Continue || !profilePolicy || state.Request.Profile == null ||
            state.Result.Diagnostics.Count == 0)
        {
            return;
        }
        ValidationDiagnostic diagnostic =
            state.Result.Diagnostics[state.Result.Diagnostics.Count - 1];
        diagnostic.ProfileIdentifier = state.Request.Profile.Identifier;
        diagnostic.ProfileVersion = state.Request.Profile.Version;
    }

    internal static bool HasProfilePolicies(ProfileDefinition? profile)
    {
        return profile != null && profile.SupplementaryDataPolicies.Count > 0;
    }

    internal static SupplementaryValidationStatus DetermineValidStatus(
        List<SupplementaryDataItem> items)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].Status != SupplementaryValidationStatus.Ignored)
            {
                return SupplementaryValidationStatus.Valid;
            }
            index++;
        }
        return SupplementaryValidationStatus.Ignored;
    }

    internal static void AddDeclarations(
        DynamicXmlNode node,
        NamespaceScopeTracker namespaces)
    {
        int index = 0;
        while (index < node.NamespaceDeclarations.Count)
        {
            NamespaceDeclarationData declaration =
                node.NamespaceDeclarations[index];
            namespaces.DeclareNamespace(
                declaration.Prefix,
                declaration.NamespaceUri);
            index++;
        }
    }

    internal static int CountEarlierSupplementarySiblings(
        DynamicXmlNode parent,
        int childIndex)
    {
        if (childIndex < 0 || childIndex >= parent.Children.Count)
        {
            return 0;
        }
        DynamicXmlNode selected = parent.Children[childIndex];
        if (!IsSupplementaryContainer(selected))
        {
            return 0;
        }
        int count = 0;
        int index = 0;
        while (index < childIndex)
        {
            DynamicXmlNode earlier = parent.Children[index];
            if (IsSupplementaryContainer(earlier) &&
                earlier.Name.LocalName == selected.Name.LocalName &&
                earlier.Name.NamespaceUri == selected.Name.NamespaceUri)
            {
                count++;
            }
            index++;
        }
        return count;
    }

    internal static void AddDiagnostic(
        SupplementaryDiscoveryState state,
        string code,
        string message,
        string expected,
        XmlPathData path,
        SourceLocation location,
        SupplementaryValidationStatus status,
        DiagnosticKind kind)
    {
        if (state.Result.Diagnostics.Count >=
            state.Request.Limits.MaxDiagnosticCount)
        {
            SetResourceFailure(
                state,
                "supplementary.diagnostic_limit",
                "The supplementary diagnostics exceed the configured limit.");
            return;
        }
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = "iso20022.supplementary.discovery";
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.SupplementaryData;
        diagnostic.Kind = kind;
        diagnostic.Message = message;
        diagnostic.Path = CopyPath(path);
        diagnostic.Location = XmlPathTracker.CopyLocation(location);
        diagnostic.ExpectedConstraint = expected;
        state.Result.Diagnostics.Add(diagnostic);
        state.Result.Valid = false;
        state.Result.Status = status;
    }

    internal static void ResolvePlaceAndName(
        SupplementaryDataItem item,
        DynamicXmlNode? parent,
        SupplementaryDiscoveryState state,
        NamespaceScopeTracker namespaces,
        SourceLocation location)
    {
        XmlPathData parentPath = CreateParentPath(item.ContainerPath);
        if (parent == null)
        {
            item.Status = SupplementaryValidationStatus.InvalidLocation;
            AddDiagnostic(
                state,
                "supplementary.place_and_name_context_missing",
                "The supplementary container has no containing element context.",
                "A containing message element for relative location evaluation.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.InvalidLocation,
                DiagnosticKind.Applicability);
            return;
        }
        if (item.PlaceAndName.Length == 0)
        {
            item.LocationPath = CopyPath(parentPath);
            item.LocationPaths.Add(CopyPath(parentPath));
            return;
        }

        PlaceAndNameParseResult parsed =
            PlaceAndNameParser.ParseExpression(item.PlaceAndName);
        if (!parsed.Success)
        {
            item.Status = SupplementaryValidationStatus.InvalidLocation;
            AddDiagnostic(
                state,
                "supplementary.place_and_name_invalid",
                parsed.ErrorMessage,
                "A syntactically valid XPath 2.0 expression.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.InvalidLocation,
                DiagnosticKind.InvalidValue);
            return;
        }
        if (!parsed.Supported)
        {
            item.Status = SupplementaryValidationStatus.UnsupportedLocation;
            AddDiagnostic(
                state,
                "supplementary.place_and_name_unsupported",
                parsed.ErrorMessage,
                "Qualified child element steps with optional positive positional predicates.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.UnsupportedLocation,
                DiagnosticKind.UnsupportedConstraint);
            return;
        }

        PlaceAndNameEvaluationResult evaluated =
            PlaceAndNameEvaluator.EvaluatePath(
                parsed,
                state.Tree,
                parent,
                parentPath,
                namespaces.CaptureDeclarations(),
                state.Request.Limits);
        if (!evaluated.Success &&
            evaluated.ErrorCode == "supplementary.location_match_limit")
        {
            item.Status = SupplementaryValidationStatus.ResourceLimitReached;
            SetResourceFailure(state, evaluated.ErrorCode, evaluated.ErrorMessage);
            return;
        }
        if (!evaluated.Success)
        {
            item.Status = SupplementaryValidationStatus.InvalidLocation;
            AddDiagnostic(
                state,
                evaluated.ErrorCode,
                evaluated.ErrorMessage,
                "Every qualified-name prefix must be declared in scope.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.InvalidLocation,
                DiagnosticKind.InvalidValue);
            return;
        }
        if (evaluated.Paths.Count == 0)
        {
            item.Status = SupplementaryValidationStatus.InvalidLocation;
            AddDiagnostic(
                state,
                "supplementary.place_and_name_not_found",
                "The PlaceAndName expression does not select an element in its containing message context.",
                "At least one matching message element.",
                item.ContainerPath,
                location,
                SupplementaryValidationStatus.InvalidLocation,
                DiagnosticKind.InvalidValue);
            return;
        }

        int index = 0;
        while (index < evaluated.Paths.Count)
        {
            item.LocationPaths.Add(CopyPath(evaluated.Paths[index]));
            index++;
        }
        item.LocationPath = CopyPath(item.LocationPaths[0]);
    }

    internal static XmlPathData CreateParentPath(XmlPathData containerPath)
    {
        XmlPathData parentPath = new XmlPathData();
        int index = 0;
        while (index + 1 < containerPath.Segments.Count)
        {
            parentPath = PlaceAndNameEvaluator.AppendPath(
                parentPath,
                containerPath.Segments[index],
                false,
                0);
            index++;
        }
        return parentPath;
    }

    internal static XmlPathData CopyPath(XmlPathData source)
    {
        XmlPathData copy = new XmlPathData();
        copy.Value = source.Value;
        int index = 0;
        while (index < source.Segments.Count)
        {
            copy.Segments.Add(XmlPathTracker.CopyName(source.Segments[index]));
            index++;
        }
        return copy;
    }

    internal static void SetResourceFailure(
        SupplementaryDiscoveryState state,
        string code,
        string message)
    {
        state.Continue = false;
        state.Result.Success = false;
        state.Result.Valid = false;
        state.Result.Status = SupplementaryValidationStatus.ResourceLimitReached;
        state.Result.ErrorCode = code;
        state.Result.ErrorMessage = message;
    }

    internal static SupplementaryValidationResult CreateOperationFailure(
        SupplementaryValidationStatus status,
        string code,
        string message)
    {
        SupplementaryValidationResult result =
            new SupplementaryValidationResult();
        result.Status = status;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
