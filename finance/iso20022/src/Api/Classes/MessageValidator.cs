using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace iso20022;

/// <summary>Runs bounded ISO 20022 message validation stages in a stable implementation-defined order.</summary>
public class MessageValidator : IMessageValidator
{
    internal static readonly ValidationStage[] OrderedStages =
        new ValidationStage[]
        {
            ValidationStage.SecureXml,
            ValidationStage.EnvelopeAndHeader,
            ValidationStage.Xsd,
            ValidationStage.Structural,
            ValidationStage.PrimitiveFacet,
            ValidationStage.InternalCode,
            ValidationStage.ExternalCode,
            ValidationStage.RepositoryConstraint,
            ValidationStage.Profile,
            ValidationStage.SupplementaryData,
            ValidationStage.Signature,
            ValidationStage.Semantic
        };

    /// <summary>Initializes a message validator with the built-in deterministic stage pipeline.</summary>
    public MessageValidator()
    {
    }

    /// <summary>Validates one message request and returns expected-invalid outcomes without throwing.</summary>
    /// <param name="request">The message representation, validation policy, profile, and caller context.</param><returns>The stable validation report.</returns>
    public ValidationReport ValidateMessage(ValidationRequest? request)
    {
        if (request == null)
        {
            return CreateOperationFailure(
                "validation.request_missing",
                "A validation request is required.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        ValidationReport? invalid = ValidateRequestConfiguration(request);
        if (invalid != null)
        {
            return invalid;
        }

        ValidationPolicy policy = request.Policy;
        if (request.Xml.LongLength > policy.Limits.MaxTotalBytes)
        {
            return CreateOperationFailure(
                "validation.input_limit",
                "The raw XML input exceeds the configured total-byte limit.",
                DiagnosticKind.ResourceLimit,
                ValidationStage.SecureXml);
        }

        ValidationReport report = CreateBaseReport(request);
        ValidationPipelineState state = new ValidationPipelineState(
            request,
            policy,
            report);
        int stageIndex = 0;
        while (stageIndex < OrderedStages.Length && state.Continue)
        {
            ValidationStage stage = OrderedStages[stageIndex];
            if (StageIsSelected(policy, stage))
            {
                state.Stage = stage;
                ExecuteStage(state);
                report.CompletedStages.Add(stage);
            }

            stageIndex++;
        }

        return report;
    }

    internal static ValidationReport? ValidateRequestConfiguration(
        ValidationRequest request)
    {
        if (request.Policy == null || request.Context == null ||
            request.Xml == null)
        {
            return CreateOperationFailure(
                "validation.request_invalid",
                "The validation request must contain policy, context, and raw-byte state.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        if (request.ExpectedNamespaceUri == null)
        {
            return CreateOperationFailure(
                "validation.expected_namespace_invalid",
                "The expected namespace URI cannot be null.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        int representationCount = 0;
        if (request.Xml.Length > 0)
        {
            representationCount++;
        }
        if (request.Document != null)
        {
            representationCount++;
        }
        if (request.Model != null)
        {
            representationCount++;
        }

        if (representationCount != 1)
        {
            return CreateOperationFailure(
                "validation.representation_invalid",
                "Exactly one raw XML, generic document, or typed model representation is required.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        ValidationPolicy policy = request.Policy;
        if (policy.Limits == null || policy.Stages == null)
        {
            return CreateOperationFailure(
                "validation.policy_invalid",
                "Validation policy limits and stage selection are required.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        if (!LimitsArePositive(policy.Limits))
        {
            return CreateOperationFailure(
                "validation.limits_invalid",
                "Every validation resource limit must be positive.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        if (policy.Stages.Count > policy.Limits.MaxCollectionCount)
        {
            return CreateOperationFailure(
                "validation.stage_limit",
                "The selected stage collection exceeds the configured collection limit.",
                DiagnosticKind.ResourceLimit,
                ValidationStage.Unknown);
        }

        int selectedIndex = 0;
        while (selectedIndex < policy.Stages.Count)
        {
            if (!StageIsKnown(policy.Stages[selectedIndex]))
            {
                return CreateOperationFailure(
                    "validation.stage_invalid",
                    "The validation policy contains an unknown stage selection.",
                    DiagnosticKind.Configuration,
                    ValidationStage.Unknown);
            }
            selectedIndex++;
        }

        if (StageIsSelected(policy, ValidationStage.ExternalCode))
        {
            if (policy.ExternalCodeEdition == null ||
                (policy.SelectLatestExternalCodeEdition &&
                    policy.ExternalCodeEdition.Length > 0) ||
                (!policy.SelectLatestExternalCodeEdition &&
                    policy.ExternalCodeEdition.Length == 0))
            {
                return CreateOperationFailure(
                    "validation.external_code_policy_invalid",
                    "Select either one pinned external-code edition or the latest-edition policy.",
                    DiagnosticKind.Configuration,
                    ValidationStage.ExternalCode);
            }
        }

        if (policy.RequireValidSignature && !policy.ValidatePresentSignatures)
        {
            return CreateOperationFailure(
                "validation.signature_policy_invalid",
                "A required valid signature must also be validated when present.",
                DiagnosticKind.Configuration,
                ValidationStage.Signature);
        }

        if (request.ExpectedIdentifier != null &&
            GetIdentifierText(request.ExpectedIdentifier).Length == 0)
        {
            return CreateOperationFailure(
                "validation.expected_identifier_invalid",
                "The expected message identifier must be complete.",
                DiagnosticKind.Configuration,
                ValidationStage.Unknown);
        }

        if (StageIsSelected(policy, ValidationStage.Xsd) &&
            request.XsdValidator != null && request.SchemaOptions == null)
        {
            return CreateOperationFailure(
                "validation.xsd.options_missing",
                "XSD pipeline validation requires schema registry options.",
                DiagnosticKind.Configuration,
                ValidationStage.Xsd);
        }

        if (StageIsSelected(policy, ValidationStage.Xsd) &&
            request.XsdValidator != null && request.Schema == null)
        {
            return CreateOperationFailure(
                "validation.xsd.schema_missing",
                "XSD pipeline validation requires an exact schema selector.",
                DiagnosticKind.Configuration,
                ValidationStage.Xsd);
        }

        return null;
    }

    internal static bool LimitsArePositive(ResourceLimits limits)
    {
        return limits.MaxDepth > 0 &&
            limits.MaxAttributesPerElement > 0 &&
            limits.MaxTextLength > 0 &&
            limits.MaxElementCount > 0 &&
            limits.MaxCollectionCount > 0 &&
            limits.MaxTotalBytes > 0 &&
            limits.MaxSchemaCount > 0 &&
            limits.MaxDiagnosticCount > 0;
    }

    internal static bool StageIsKnown(ValidationStage stage)
    {
        switch (stage)
        {
            case ValidationStage.SecureXml:
            case ValidationStage.EnvelopeAndHeader:
            case ValidationStage.Xsd:
            case ValidationStage.Structural:
            case ValidationStage.PrimitiveFacet:
            case ValidationStage.InternalCode:
            case ValidationStage.ExternalCode:
            case ValidationStage.RepositoryConstraint:
            case ValidationStage.Profile:
            case ValidationStage.SupplementaryData:
            case ValidationStage.Signature:
            case ValidationStage.Semantic:
                return true;
            default:
                return false;
        }
    }

    internal static bool StageIsSelected(
        ValidationPolicy policy,
        ValidationStage stage)
    {
        if (policy.Stages.Count == 0)
        {
            return true;
        }

        int index = 0;
        while (index < policy.Stages.Count)
        {
            if (policy.Stages[index] == stage)
            {
                return true;
            }
            index++;
        }

        return false;
    }

    internal static ValidationReport CreateBaseReport(ValidationRequest request)
    {
        ValidationReport report = new ValidationReport();
        report.Success = true;
        report.Valid = true;
        MessageIdentifier? actualIdentifier = FindActualIdentifier(request);
        if (actualIdentifier != null)
        {
            report.MessageIdentifier = actualIdentifier;
        }
        else
        {
            report.MessageIdentifier = request.ExpectedIdentifier;
        }

        string actualNamespace = FindActualNamespace(request);
        if (actualNamespace.Length > 0)
        {
            report.NamespaceUri = actualNamespace;
        }
        else
        {
            report.NamespaceUri = request.ExpectedNamespaceUri;
        }

        if (request.Profile != null && request.Profile.Identifier != null)
        {
            report.ProfileIdentifier = request.Profile.Identifier;
        }
        else if (request.Context.ProfileIdentifier != null)
        {
            report.ProfileIdentifier = request.Context.ProfileIdentifier;
        }

        return report;
    }

    internal static void ExecuteStage(ValidationPipelineState state)
    {
        switch (state.Stage)
        {
            case ValidationStage.EnvelopeAndHeader:
                ValidateExpectedIdentity(state);
                break;
            case ValidationStage.Xsd:
                ValidateXsdStage(state);
                break;
            case ValidationStage.Structural:
                ValidateGeneratedStage(state, "ValidateMessageModel");
                break;
            case ValidationStage.PrimitiveFacet:
                ValidateGeneratedStage(state, "ValidateMessagePrimitiveFacets");
                break;
            case ValidationStage.InternalCode:
                ValidateGeneratedStage(state, "ValidateMessageInternalCodes");
                break;
            case ValidationStage.ExternalCode:
                ValidateGeneratedStage(state, "ValidateMessageExternalCodes");
                break;
            case ValidationStage.RepositoryConstraint:
                ValidateGeneratedStage(state, "ValidateMessageRepositoryConstraints");
                break;
            case ValidationStage.Profile:
                ValidateProfileArtifactPolicy(state);
                if (state.Continue)
                {
                    ValidateProfileOverlay(state);
                }
                break;
            case ValidationStage.Semantic:
                ValidateSemanticRules(state);
                break;
            default:
                break;
        }
    }

    internal static void ValidateGeneratedStage(
        ValidationPipelineState state,
        string registryMethod)
    {
        object? model = state.Request.Model;
        if (model == null)
        {
            return;
        }

        Type modelType = model.GetType();
        string modelNamespace = modelType.Namespace ?? string.Empty;
        int messagesIndex = modelNamespace.IndexOf(
            ".Messages.",
            StringComparison.Ordinal);
        if (messagesIndex < 1)
        {
            AddStructuralDispatchFailure(
                state,
                "validation.structural.metadata_unavailable",
                "The typed model does not expose generated structural metadata.");
            return;
        }

        string registryName = modelNamespace.Substring(0, messagesIndex) +
            ".Internal.Iso20022GeneratedRegistry";
        Type? registryType = modelType.Assembly.GetType(registryName);
        if (registryType == null)
        {
            AddStructuralDispatchFailure(
                state,
                "validation.structural.metadata_unavailable",
                "The typed model assembly does not contain its generated structural registry.");
            return;
        }

        MethodInfo? validator = registryType.GetMethod(
            registryMethod,
            BindingFlags.NonPublic | BindingFlags.Static);
        if (validator == null)
        {
            AddStructuralDispatchFailure(
                state,
                "validation.structural.metadata_unavailable",
                "The generated registry does not contain the selected model validator.");
            return;
        }

        string identifier = string.Empty;
        if (state.Report.MessageIdentifier != null)
        {
            identifier = GetIdentifierText(state.Report.MessageIdentifier);
        }
        object?[] arguments = new object?[]
        {
            model,
            identifier,
            state.Report.NamespaceUri,
            state.Policy
        };
        if (state.Stage == ValidationStage.ExternalCode)
        {
            IsoDateValue? activeOn = null;
            if (state.Request.Context.EffectiveAt != null)
            {
                activeOn = state.Request.Context.EffectiveAt.Date;
            }
            arguments = new object?[]
            {
                model,
                identifier,
                state.Report.NamespaceUri,
                state.Policy,
                state.Request.CodeSetProvider,
                activeOn
            };
        }
        object? generatedValue = validator.Invoke(null, arguments);
        ValidationReport? generated = generatedValue as ValidationReport;
        ValidationReportIntegrator.MergeReport(state, generated);
    }

    internal static void AddStructuralDispatchFailure(
        ValidationPipelineState state,
        string code,
        string message)
    {
        state.AddDiagnostic(CreateDiagnostic(
            code,
            "iso20022.structure.dispatch",
            DiagnosticKind.Applicability,
            message,
            string.Empty,
            "A generated model with exact structural metadata.",
            state.Request));
        state.Report.Success = false;
        state.Report.ErrorCode = code;
        state.Report.ErrorMessage = message;
    }

    internal static void ValidateExpectedIdentity(ValidationPipelineState state)
    {
        ValidationRequest request = state.Request;
        if (request.ExpectedIdentifier != null)
        {
            MessageIdentifier? actualIdentifier = FindActualIdentifier(request);
            string expectedText = GetIdentifierText(request.ExpectedIdentifier);
            if (actualIdentifier == null)
            {
                state.AddDiagnostic(CreateDiagnostic(
                    "validation.identifier_missing",
                    "iso20022.identity.expected",
                    DiagnosticKind.MissingContent,
                    "The message identity required by the validation request is unavailable.",
                    string.Empty,
                    expectedText,
                    request));
            }
            else
            {
                string actualText = GetIdentifierText(actualIdentifier);
                if (actualText != expectedText)
                {
                    state.AddDiagnostic(CreateDiagnostic(
                        "validation.identifier_mismatch",
                        "iso20022.identity.expected",
                        DiagnosticKind.InvalidValue,
                        "The resolved message identifier does not match the expected identifier.",
                        actualText,
                        expectedText,
                        request));
                }
            }
        }

        if (!state.Continue)
        {
            return;
        }

        if (request.ExpectedNamespaceUri.Length > 0)
        {
            string actualNamespace = FindActualNamespace(request);
            if (actualNamespace.Length == 0)
            {
                state.AddDiagnostic(CreateDiagnostic(
                    "validation.namespace_missing",
                    "iso20022.namespace.expected",
                    DiagnosticKind.MissingContent,
                    "The message namespace required by the validation request is unavailable.",
                    string.Empty,
                    request.ExpectedNamespaceUri,
                    request));
            }
            else if (actualNamespace != request.ExpectedNamespaceUri)
            {
                state.AddDiagnostic(CreateDiagnostic(
                    "validation.namespace_mismatch",
                    "iso20022.namespace.expected",
                    DiagnosticKind.InvalidValue,
                    "The resolved message namespace does not match the expected namespace.",
                    actualNamespace,
                    request.ExpectedNamespaceUri,
                    request));
            }
        }
    }

    internal static void ValidateSchemaArtifactPolicy(
        ValidationPipelineState state)
    {
        SchemaArtifact? schema = state.Request.Schema;
        if (schema != null && !schema.Artifact.IsApproved &&
            !state.Policy.AllowDraftArtifacts)
        {
            ValidationDiagnostic diagnostic = CreateDiagnostic(
                "validation.schema_draft_disallowed",
                "iso20022.artifact.approved",
                DiagnosticKind.Configuration,
                "The selected schema artifact is not approved by the validation policy.",
                schema.SourceName,
                "An approved schema artifact.",
                state.Request);
            diagnostic.SourceArtifact = schema.Artifact;
            state.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateXsdStage(ValidationPipelineState state)
    {
        ValidateSchemaArtifactPolicy(state);
        if (!state.Continue || state.Request.XsdValidator == null)
        {
            return;
        }
        SchemaRegistryOptions? options = CreatePipelineSchemaOptions(state);
        if (options == null)
        {
            return;
        }
        SchemaCompilationResult? result = null;
        if (state.Request.Xml.Length > 0)
        {
            using (MemoryStream input = new MemoryStream(state.Request.Xml, false))
            {
                result = state.Request.XsdValidator.ValidateXmlStream(
                    input,
                    state.Request.Schema,
                    options);
            }
        }
        else if (state.Request.Document != null)
        {
            result = state.Request.XsdValidator.ValidateMessageDocument(
                state.Request.Document,
                state.Request.Schema,
                options);
        }
        else
        {
            ValidationReport unsupported =
                XsdValidationReportAdapter.CreateAdapterFailure(
                    "validation.xsd.representation_unsupported",
                    "XSD validation requires raw XML or a generic message document.",
                    state.Request.Schema);
            ValidationReportIntegrator.MergeReport(state, unsupported);
            return;
        }
        ValidationReport adapted = XsdValidationReportAdapter.CreateReport(
            result,
            state.Request.Schema);
        ValidationReportIntegrator.MergeReport(state, adapted);
    }

    internal static SchemaRegistryOptions? CreatePipelineSchemaOptions(
        ValidationPipelineState state)
    {
        SchemaRegistryOptions? source = state.Request.SchemaOptions;
        if (source == null || source.LocalSchemas == null)
        {
            ValidationReportIntegrator.AddIntegrationFailure(
                state,
                "validation.xsd.options_invalid",
                "XSD schema options and their local schema list are required.");
            return null;
        }
        SchemaRegistryOptions options = new SchemaRegistryOptions();
        options.LocalRootPath = source.LocalRootPath;
        options.Limits = state.Policy.Limits;
        options.LocalSchemas.AddRange(source.LocalSchemas);
        options.ResolveIncludes = source.ResolveIncludes;
        options.ResolveImports = source.ResolveImports;
        options.AllowRepositoryFallback = source.AllowRepositoryFallback;
        options.AllowReplacement = source.AllowReplacement;
        options.RequireApprovedArtifacts = source.RequireApprovedArtifacts;
        options.CompileOnRegistration = source.CompileOnRegistration;
        options.CacheCompiledSchemas = source.CacheCompiledSchemas;
        options.CancellationToken = source.CancellationToken;
        return options;
    }

    internal static void ValidateProfileArtifactPolicy(
        ValidationPipelineState state)
    {
        ProfileDefinition? profile = state.Request.Profile;
        if (profile != null && !profile.Artifact.IsApproved &&
            !state.Policy.AllowDraftArtifacts)
        {
            ValidationDiagnostic diagnostic = CreateDiagnostic(
                "validation.profile_draft_disallowed",
                "iso20022.artifact.approved",
                DiagnosticKind.Configuration,
                "The selected profile artifact is not approved by the validation policy.",
                profile.Identifier,
                "An approved profile artifact.",
                state.Request);
            diagnostic.SourceArtifact = profile.Artifact;
            diagnostic.ProfileIdentifier = profile.Identifier;
            diagnostic.ProfileVersion = profile.Version;
            state.AddDiagnostic(diagnostic);
        }
    }

    internal static void ValidateProfileOverlay(
        ValidationPipelineState state)
    {
        if (state.Request.Profile == null)
        {
            return;
        }
        ProfileOverlayResolution resolution = ProfileOverlayResolver.ResolveOverlay(
            state.Request,
            state.Report.MessageIdentifier,
            state.Report.NamespaceUri);
        if (!resolution.Success)
        {
            ValidationDiagnostic diagnostic = CreateDiagnostic(
                resolution.ErrorCode,
                "iso20022.profile.overlay",
                resolution.Kind,
                resolution.ErrorMessage,
                state.Request.Profile.Identifier,
                "A compatible, conflict-free profile inheritance chain.",
                state.Request);
            diagnostic.ProfileIdentifier = state.Request.Profile.Identifier;
            diagnostic.ProfileVersion = state.Request.Profile.Version;
            diagnostic.SourceArtifact = state.Request.Profile.Artifact;
            state.AddDiagnostic(diagnostic);
            if (resolution.OperationFailure)
            {
                state.Report.Success = false;
                state.Report.Valid = false;
                state.Report.ErrorCode = resolution.ErrorCode;
                state.Report.ErrorMessage = resolution.ErrorMessage;
                state.Report.StoppedEarly = true;
                state.Continue = false;
            }
            return;
        }
        int profileIndex = 0;
        while (profileIndex < resolution.Profiles.Count && state.Continue)
        {
            ProfileDefinition profile = resolution.Profiles[profileIndex];
            if (profile != state.Request.Profile &&
                !profile.Artifact.IsApproved &&
                !state.Policy.AllowDraftArtifacts)
            {
                ValidationDiagnostic diagnostic = CreateDiagnostic(
                    "validation.profile_draft_disallowed",
                    "iso20022.artifact.approved",
                    DiagnosticKind.Configuration,
                    "An inherited profile artifact is not approved by the validation policy.",
                    profile.Identifier,
                    "An approved profile artifact.",
                    state.Request);
                diagnostic.SourceArtifact = profile.Artifact;
                diagnostic.ProfileIdentifier = profile.Identifier;
                diagnostic.ProfileVersion = profile.Version;
                state.AddDiagnostic(diagnostic);
            }
            profileIndex++;
        }
        if (state.Continue)
        {
            ProfileOverlayValidator.Validate(state, resolution);
        }
    }

    internal static void ValidateSemanticRules(ValidationPipelineState state)
    {
        List<SemanticRuleSelection> selections = ComposeSemanticRuleSelections(state);
        if (!state.Continue)
        {
            return;
        }
        SortSemanticRuleSelections(selections);
        int index = 0;
        while (index < selections.Count && state.Continue)
        {
            SemanticRuleSelection selection = selections[index];
            if (selection.Enabled && SemanticRuleApplies(state, selection))
            {
                ExecuteSemanticRuleSelection(state, selection);
            }
            index++;
        }
    }

    internal static List<SemanticRuleSelection> ComposeSemanticRuleSelections(
        ValidationPipelineState state)
    {
        List<SemanticRuleSelection> selections = new List<SemanticRuleSelection>();
        List<ISemanticRule>? callerRules = state.Request.SemanticRules;
        if (callerRules == null)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.list_missing",
                "The semantic rule list is required.");
            return selections;
        }
        List<ISemanticRule> rules = BuiltInSemanticRuleEvaluator.CreateRules();
        if (callerRules.Count > state.Policy.Limits.MaxCollectionCount)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.list_limit",
                "The semantic rule list exceeds the configured collection limit.");
            return selections;
        }
        rules.AddRange(callerRules);
        int index = 0;
        while (index < rules.Count && state.Continue)
        {
            ISemanticRule? rule = rules[index];
            if (rule == null)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.rule_missing",
                    "The semantic rule list contains no rule implementation.");
                return selections;
            }
            SemanticRuleDescriptor? descriptor = rule.GetSemanticRuleDescriptor();
            if (descriptor == null)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.descriptor_missing",
                    "A semantic rule returned no descriptor.");
                return selections;
            }
            if (descriptor.Identifier == null || descriptor.Identifier.Length == 0)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.identifier_missing",
                    "Every semantic rule must declare a stable non-empty identifier.");
                return selections;
            }
            if (FindSemanticRuleSelection(selections, descriptor.Identifier) != null)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.identifier_duplicate",
                    "Semantic rule identifiers must be unique within a composed rule set.");
                return selections;
            }
            SemanticRuleSelection selection = new SemanticRuleSelection();
            selection.Rule = rule;
            selection.Descriptor = descriptor;
            selection.Identifier = descriptor.Identifier;
            selection.Severity = descriptor.Severity;
            selection.Order = descriptor.Order;
            selection.SourceOrder = index;
            selection.Enabled = descriptor.Enabled;
            selections.Add(selection);
            index++;
        }
        if (state.Continue)
        {
            ApplyProfileSemanticRules(state, selections);
        }
        return selections;
    }

    internal static void ApplyProfileSemanticRules(
        ValidationPipelineState state,
        List<SemanticRuleSelection> selections)
    {
        if (state.Request.Profile == null)
        {
            return;
        }
        ProfileOverlayResolution resolution = ProfileOverlayResolver.ResolveOverlay(
            state.Request,
            state.Report.MessageIdentifier,
            state.Report.NamespaceUri);
        if (!resolution.Success)
        {
            HandleProfileSemanticResolutionFailure(state, resolution);
            return;
        }
        int profileIndex = 0;
        while (profileIndex < resolution.Profiles.Count && state.Continue)
        {
            ApplyProfileSemanticRuleList(
                state,
                selections,
                resolution.Profiles[profileIndex]);
            profileIndex++;
        }
    }

    internal static void ApplyProfileSemanticRuleList(
        ValidationPipelineState state,
        List<SemanticRuleSelection> selections,
        ProfileDefinition profile)
    {
        if (profile.Rules == null)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.profile_list_missing",
                "The selected profile semantic rule list is required.");
            return;
        }
        if (profile.Rules.Count > state.Policy.Limits.MaxCollectionCount)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.profile_list_limit",
                "The selected profile semantic rule list exceeds the collection limit.");
            return;
        }
        int index = 0;
        while (index < profile.Rules.Count && state.Continue)
        {
            ProfileRuleReference? reference = profile.Rules[index];
            if (reference == null)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.profile_reference_missing",
                    "The selected profile contains no semantic rule reference.");
                return;
            }
            string identifier = reference.RuleIdentifier;
            if (identifier.Length == 0 && reference.Descriptor != null)
            {
                identifier = reference.Descriptor.Identifier;
            }
            if (identifier.Length == 0)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.profile_identifier_missing",
                    "Every profile semantic rule reference requires a stable identifier.");
                return;
            }
            SemanticRuleSelection? selection = FindSemanticRuleSelection(
                selections,
                identifier);
            if (selection == null)
            {
                selection = new SemanticRuleSelection();
                selection.Identifier = identifier;
                selection.SourceOrder = selections.Count + index;
                if (reference.Descriptor != null)
                {
                    selection.Descriptor = reference.Descriptor;
                }
                selection.Severity = selection.Descriptor.Severity;
                selection.Order = selection.Descriptor.Order;
                selections.Add(selection);
            }
            ApplyProfileSemanticRuleReference(selection, reference);
            selection.Profile = profile;
            index++;
        }
    }

    internal static void HandleProfileSemanticResolutionFailure(
        ValidationPipelineState state,
        ProfileOverlayResolution resolution)
    {
        int diagnosticIndex = 0;
        while (diagnosticIndex < state.Report.Diagnostics.Count)
        {
            if (state.Report.Diagnostics[diagnosticIndex].Code ==
                resolution.ErrorCode)
            {
                return;
            }
            diagnosticIndex++;
        }
        ProfileDefinition? profile = state.Request.Profile;
        ValidationDiagnostic diagnostic = CreateDiagnostic(
            resolution.ErrorCode,
            "iso20022.profile.overlay",
            resolution.Kind,
            resolution.ErrorMessage,
            string.Empty,
            "A compatible, conflict-free profile inheritance chain.",
            state.Request);
        if (profile != null)
        {
            diagnostic.ProfileIdentifier = profile.Identifier;
            diagnostic.ProfileVersion = profile.Version;
            diagnostic.SourceArtifact = profile.Artifact;
        }
        state.AddDiagnostic(diagnostic);
        if (resolution.OperationFailure)
        {
            state.Report.Success = false;
            state.Report.Valid = false;
            state.Report.ErrorCode = resolution.ErrorCode;
            state.Report.ErrorMessage = resolution.ErrorMessage;
            state.Report.StoppedEarly = true;
            state.Continue = false;
        }
    }

    internal static void ApplyProfileSemanticRuleReference(
        SemanticRuleSelection selection,
        ProfileRuleReference reference)
    {
        if (reference.Descriptor != null)
        {
            selection.Descriptor = reference.Descriptor;
            selection.Severity = reference.Descriptor.Severity;
            selection.Order = reference.Descriptor.Order;
        }
        selection.Enabled = reference.Enabled && !reference.RemoveInheritedRule;
        if (reference.HasSeverityOverride)
        {
            selection.Severity = reference.Severity;
            selection.HasSeverityOverride = true;
        }
        if (reference.HasOrderOverride)
        {
            selection.Order = reference.Order;
        }
    }

    internal static SemanticRuleSelection? FindSemanticRuleSelection(
        List<SemanticRuleSelection> selections,
        string identifier)
    {
        int index = 0;
        while (index < selections.Count)
        {
            if (selections[index].Identifier == identifier)
            {
                return selections[index];
            }
            index++;
        }
        return null;
    }

    internal static void SortSemanticRuleSelections(
        List<SemanticRuleSelection> selections)
    {
        int index = 1;
        while (index < selections.Count)
        {
            SemanticRuleSelection value = selections[index];
            int destination = index;
            while (destination > 0 && SemanticRuleComesBefore(
                value,
                selections[destination - 1]))
            {
                selections[destination] = selections[destination - 1];
                destination--;
            }
            selections[destination] = value;
            index++;
        }
    }

    internal static bool SemanticRuleComesBefore(
        SemanticRuleSelection left,
        SemanticRuleSelection right)
    {
        if (left.Order != right.Order)
        {
            return left.Order < right.Order;
        }
        int identifierOrder = string.CompareOrdinal(left.Identifier, right.Identifier);
        if (identifierOrder != 0)
        {
            return identifierOrder < 0;
        }
        return left.SourceOrder < right.SourceOrder;
    }

    internal static bool SemanticRuleApplies(
        ValidationPipelineState state,
        SemanticRuleSelection selection)
    {
        SemanticRuleDescriptor descriptor = selection.Descriptor;
        if (descriptor.MessageIdentifiers == null ||
            descriptor.ProfileIdentifiers == null || descriptor.Paths == null)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.applicability_list_missing",
                "Semantic rule applicability lists cannot be null.");
            return false;
        }
        if (descriptor.MessageIdentifiers.Count > state.Policy.Limits.MaxCollectionCount ||
            descriptor.ProfileIdentifiers.Count > state.Policy.Limits.MaxCollectionCount ||
            descriptor.Paths.Count > state.Policy.Limits.MaxCollectionCount)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.applicability_list_limit",
                "A semantic rule applicability list exceeds the collection limit.");
            return false;
        }
        MessageIdentifier? identifier = state.Report.MessageIdentifier;
        if (!SemanticMessageIdentifiersApply(descriptor, identifier))
        {
            return false;
        }
        if (!SemanticVersionSelectorApplies(descriptor.VersionSelector, identifier))
        {
            return false;
        }
        bool applies = SemanticProfileIdentifiersApply(
            descriptor,
            FindSemanticProfileIdentifier(state));
        if (!applies && selection.Profile != null)
        {
            applies = SemanticProfileIdentifiersApply(
                descriptor,
                selection.Profile.Identifier);
        }
        return applies;
    }

    internal static bool SemanticMessageIdentifiersApply(
        SemanticRuleDescriptor descriptor,
        MessageIdentifier? actual)
    {
        if (descriptor.MessageIdentifiers.Count == 0)
        {
            return true;
        }
        if (actual == null)
        {
            return false;
        }
        string actualText = GetIdentifierText(actual);
        int index = 0;
        while (index < descriptor.MessageIdentifiers.Count)
        {
            MessageIdentifier? expected = descriptor.MessageIdentifiers[index];
            if (expected != null && GetIdentifierText(expected) == actualText)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool SemanticVersionSelectorApplies(
        VersionSelector? selector,
        MessageIdentifier? actual)
    {
        if (selector == null)
        {
            return true;
        }
        if (actual == null)
        {
            return false;
        }
        switch (selector.Kind)
        {
            case VersionSelectionKind.Exact:
                return selector.BaselineIdentifier != null &&
                    GetIdentifierText(selector.BaselineIdentifier) ==
                    GetIdentifierText(actual);
            case VersionSelectionKind.Current:
                return true;
            case VersionSelectionKind.LatestCompatible:
                return selector.BaselineIdentifier != null &&
                    SemanticVersionsAreCompatible(
                        actual,
                        selector.BaselineIdentifier) &&
                    CompareSemanticVersions(
                        actual.Version,
                        selector.BaselineIdentifier.Version) >= 0;
            case VersionSelectionKind.Range:
                return SemanticVersionIsInRange(
                    actual.Version,
                    selector.MinimumVersion,
                    selector.MaximumVersion);
            default:
                return false;
        }
    }

    internal static bool SemanticVersionsAreCompatible(
        MessageIdentifier actual,
        MessageIdentifier baseline)
    {
        return actual.BusinessArea == baseline.BusinessArea &&
            actual.MessageFunctionality == baseline.MessageFunctionality &&
            actual.Variant == baseline.Variant;
    }

    internal static bool SemanticVersionIsInRange(
        string version,
        string minimum,
        string maximum)
    {
        if (minimum.Length > 0 && CompareSemanticVersions(version, minimum) < 0)
        {
            return false;
        }
        if (maximum.Length > 0 && CompareSemanticVersions(version, maximum) > 0)
        {
            return false;
        }
        return true;
    }

    internal static int CompareSemanticVersions(string left, string right)
    {
        int leftIndex = 0;
        while (leftIndex + 1 < left.Length && left[leftIndex] == '0')
        {
            leftIndex++;
        }
        int rightIndex = 0;
        while (rightIndex + 1 < right.Length && right[rightIndex] == '0')
        {
            rightIndex++;
        }
        int leftLength = left.Length - leftIndex;
        int rightLength = right.Length - rightIndex;
        if (leftLength != rightLength)
        {
            return leftLength < rightLength ? -1 : 1;
        }
        return string.CompareOrdinal(
            left.Substring(leftIndex),
            right.Substring(rightIndex));
    }

    internal static bool SemanticProfileIdentifiersApply(
        SemanticRuleDescriptor descriptor,
        string selectedProfile)
    {
        if (descriptor.ProfileIdentifiers.Count == 0)
        {
            return true;
        }
        int index = 0;
        while (index < descriptor.ProfileIdentifiers.Count)
        {
            if (descriptor.ProfileIdentifiers[index] == selectedProfile)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string FindSemanticProfileIdentifier(
        ValidationPipelineState state)
    {
        if (state.Request.Profile != null &&
            state.Request.Profile.Identifier.Length > 0)
        {
            return state.Request.Profile.Identifier;
        }
        if (state.Report.ProfileIdentifier.Length > 0)
        {
            return state.Report.ProfileIdentifier;
        }
        return state.Request.Context.ProfileIdentifier;
    }

    internal static void ExecuteSemanticRuleSelection(
        ValidationPipelineState state,
        SemanticRuleSelection selection)
    {
        if (selection.Rule == null)
        {
            AddMissingSemanticRuleImplementation(state, selection);
            return;
        }
        if (selection.Descriptor.Paths.Count == 0)
        {
            ExecuteSemanticRulePath(state, selection, new XmlPathData());
            return;
        }
        int index = 0;
        while (index < selection.Descriptor.Paths.Count && state.Continue)
        {
            XmlPathData? path = selection.Descriptor.Paths[index];
            if (path == null)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.path_missing",
                    "A semantic rule applicability path cannot be null.");
                return;
            }
            ExecuteSemanticRulePath(state, selection, path);
            index++;
        }
    }

    internal static void ExecuteSemanticRulePath(
        ValidationPipelineState state,
        SemanticRuleSelection selection,
        XmlPathData path)
    {
        ISemanticRule? rule = selection.Rule;
        if (rule == null)
        {
            return;
        }
        List<ValidationDiagnostic>? diagnostics = rule.ValidateSemanticRule(
            state.Request,
            path);
        if (diagnostics == null)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.result_missing",
                "A semantic rule returned no diagnostic list.");
            return;
        }
        if (diagnostics.Count > state.Policy.Limits.MaxCollectionCount)
        {
            AddSemanticOperationFailure(
                state,
                "validation.semantic_rule.result_limit",
                "A semantic rule result exceeds the configured collection limit.");
            return;
        }
        int index = 0;
        while (index < diagnostics.Count && state.Continue)
        {
            ValidationDiagnostic? returnedDiagnostic = diagnostics[index];
            if (returnedDiagnostic == null)
            {
                AddSemanticOperationFailure(
                    state,
                    "validation.semantic_rule.diagnostic_missing",
                    "A semantic rule result contains no diagnostic value.");
                return;
            }
            ValidationDiagnostic diagnostic =
                ValidationReportIntegrator.CopyDiagnostic(returnedDiagnostic);
            NormalizeSemanticDiagnostic(state, selection, diagnostic, path);
            state.AddDiagnostic(diagnostic);
            index++;
        }
    }

    internal static void NormalizeSemanticDiagnostic(
        ValidationPipelineState state,
        SemanticRuleSelection selection,
        ValidationDiagnostic diagnostic,
        XmlPathData path)
    {
        SemanticRuleDescriptor descriptor = selection.Descriptor;
        diagnostic.RuleIdentifier = selection.Identifier;
        if (selection.HasSeverityOverride ||
            diagnostic.Severity == ValidationSeverity.Unknown)
        {
            diagnostic.Severity = selection.Severity;
        }
        if (diagnostic.Severity == ValidationSeverity.Unknown)
        {
            diagnostic.Severity = ValidationSeverity.Error;
        }
        if (diagnostic.Code == null || diagnostic.Code.Length == 0)
        {
            diagnostic.Code = "validation.semantic_rule.failed";
        }
        if (diagnostic.Kind == DiagnosticKind.Unknown)
        {
            diagnostic.Kind = DiagnosticKind.ValidationFailure;
        }
        if (diagnostic.Message == null || diagnostic.Message.Length == 0)
        {
            diagnostic.Message = descriptor.Definition;
            if (diagnostic.Message.Length == 0)
            {
                diagnostic.Message = descriptor.Name;
            }
            if (diagnostic.Message.Length == 0)
            {
                diagnostic.Message = "A semantic validation rule failed.";
            }
        }
        if (diagnostic.ExpectedConstraint == null ||
            diagnostic.ExpectedConstraint.Length == 0)
        {
            diagnostic.ExpectedConstraint = descriptor.Definition;
        }
        if (diagnostic.SourceIdentifier == null ||
            diagnostic.SourceIdentifier.Length == 0)
        {
            diagnostic.SourceIdentifier = descriptor.SourceIdentifier;
        }
        if (diagnostic.Path == null ||
            (diagnostic.Path.Value.Length == 0 && diagnostic.Path.Segments.Count == 0))
        {
            diagnostic.Path = path;
        }
        ProfileDefinition? profile = selection.Profile;
        if (profile == null)
        {
            profile = state.Request.Profile;
        }
        if (profile != null)
        {
            diagnostic.ProfileIdentifier = profile.Identifier;
            diagnostic.ProfileVersion = profile.Version;
            diagnostic.SourceArtifact = profile.Artifact;
        }
    }

    internal static void AddMissingSemanticRuleImplementation(
        ValidationPipelineState state,
        SemanticRuleSelection selection)
    {
        ValidationDiagnostic diagnostic = CreateDiagnostic(
            "validation.semantic_rule.implementation_missing",
            selection.Identifier,
            DiagnosticKind.Applicability,
            "An enabled semantic rule has no executable implementation.",
            string.Empty,
            selection.Descriptor.Definition,
            state.Request);
        diagnostic.Severity = ValidationSeverity.Warning;
        diagnostic.SourceIdentifier = selection.Descriptor.SourceIdentifier;
        ProfileDefinition? profile = selection.Profile;
        if (profile == null)
        {
            profile = state.Request.Profile;
        }
        if (profile != null)
        {
            diagnostic.ProfileIdentifier = profile.Identifier;
            diagnostic.ProfileVersion = profile.Version;
            diagnostic.SourceArtifact = profile.Artifact;
        }
        state.AddDiagnostic(diagnostic);
    }

    internal static void AddSemanticOperationFailure(
        ValidationPipelineState state,
        string code,
        string message)
    {
        state.AddDiagnostic(CreateDiagnostic(
            code,
            "iso20022.semantic.dispatch",
            DiagnosticKind.Configuration,
            message,
            string.Empty,
            "A bounded semantic rule set with unique stable identifiers.",
            state.Request));
        state.Report.Success = false;
        state.Report.Valid = false;
        state.Report.ErrorCode = code;
        state.Report.ErrorMessage = message;
        state.Report.StoppedEarly = true;
        state.Continue = false;
    }

    internal static ValidationDiagnostic CreateDiagnostic(
        string code,
        string ruleIdentifier,
        DiagnosticKind kind,
        string message,
        string offendingValue,
        string expectedConstraint,
        ValidationRequest request)
    {
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = ruleIdentifier;
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Kind = kind;
        diagnostic.Message = message;
        diagnostic.OffendingValue = offendingValue;
        diagnostic.ExpectedConstraint = expectedConstraint;
        if (request.Document != null)
        {
            diagnostic.Location = request.Document.Location;
        }
        return diagnostic;
    }

    internal static MessageIdentifier? FindActualIdentifier(
        ValidationRequest request)
    {
        if (request.Document != null && request.Document.Metadata != null &&
            request.Document.Metadata.MessageIdentifier != null)
        {
            return request.Document.Metadata.MessageIdentifier;
        }

        if (request.Context.MessageIdentifier != null)
        {
            return request.Context.MessageIdentifier;
        }

        if (request.Document != null && request.Document.Payloads != null &&
            request.Document.Payloads.Count == 1 &&
            request.Document.Payloads[0].Identifier != null)
        {
            return request.Document.Payloads[0].Identifier;
        }

        return null;
    }

    internal static string FindActualNamespace(ValidationRequest request)
    {
        if (request.Document != null && request.Document.Metadata != null &&
            request.Document.Metadata.NamespaceUri != null &&
            request.Document.Metadata.NamespaceUri.Length > 0)
        {
            return request.Document.Metadata.NamespaceUri;
        }

        if (request.Context.NamespaceUri != null &&
            request.Context.NamespaceUri.Length > 0)
        {
            return request.Context.NamespaceUri;
        }

        if (request.Document != null && request.Document.Payloads != null &&
            request.Document.Payloads.Count == 1)
        {
            NamespaceDescriptor? descriptor =
                request.Document.Payloads[0].Namespace;
            if (descriptor != null && descriptor.Uri != null)
            {
                return descriptor.Uri;
            }
        }

        return string.Empty;
    }

    internal static string GetIdentifierText(MessageIdentifier identifier)
    {
        if (identifier.Value != null && identifier.Value.Length > 0)
        {
            MessageIdentifier? parsed =
                MessageIdentifierLexical.ParseMessageIdentifier(
                    identifier.Value.AsSpan());
            if (parsed != null)
            {
                return identifier.Value;
            }
        }

        string? formatted =
            MessageIdentifierLexical.FormatMessageIdentifier(identifier);
        if (formatted != null)
        {
            return formatted;
        }

        return string.Empty;
    }

    internal static ValidationReport CreateOperationFailure(
        string code,
        string message,
        DiagnosticKind kind,
        ValidationStage stage)
    {
        ValidationReport report = new ValidationReport();
        report.Success = false;
        report.Valid = false;
        report.ErrorCode = code;
        report.ErrorMessage = message;
        report.ErrorCount = 1;
        if (kind == DiagnosticKind.ResourceLimit)
        {
            report.StoppedEarly = true;
        }

        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = "iso20022.validation.operation";
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = stage;
        diagnostic.Kind = kind;
        diagnostic.Message = message;
        report.Diagnostics.Add(diagnostic);
        return report;
    }
}
