using System;
using System.Text;

namespace iso20022;

/// <summary>Composes selected high-level message stages into one deterministic processing pipeline.</summary>
public class MessageProcessor : IMessageProcessor
{
    /// <summary>Initializes a deterministic high-level message processor.</summary>
    public MessageProcessor()
    {
    }

    /// <summary>Processes one raw, generic, generated, or canonical-model input.</summary>
    /// <param name="request">The input, selected stages, exact identities, and stage requests.</param>
    /// <returns>The complete reached-stage results and final representation.</returns>
    public ProcessingResult ProcessMessage(ProcessingRequest? request)
    {
        if (request == null)
        {
            return CreateProcessingFailure("processing.request_missing", "A processing request is required.");
        }
        ProcessingExecutionState state = new ProcessingExecutionState();
        state.Request = request;
        state.Document = request.Document;
        state.Model = request.Model;
        state.Xml = request.Xml;
        if (!ValidateProcessingConfiguration(state) || !NormalizeProcessingStages(state))
        {
            return state.Result;
        }
        InitializeProcessingIdentity(state);

        int index = 0;
        while (index < state.Result.RequestedStages.Count)
        {
            ProcessingStage stage = state.Result.RequestedStages[index];
            if (request.CancellationToken.IsCancellationRequested)
            {
                return StopProcessing(state, stage, "processing.cancelled", "Processing was cancelled.");
            }
            bool completed = ExecuteProcessingStage(state, stage);
            if (!completed)
            {
                return state.Result;
            }
            state.Result.CompletedStages.Add(stage);
            index++;
        }

        PublishFinalProcessingState(state);
        state.Result.Success = true;
        state.Result.FailedStage = ProcessingStage.Unknown;
        return state.Result;
    }

    internal static bool ValidateProcessingConfiguration(ProcessingExecutionState state)
    {
        ProcessingRequest request = state.Request;
        if (request.Stages == null || request.Limits == null ||
            !MessageValidator.LimitsArePositive(request.Limits) || request.ReadOptions == null ||
            request.ReadOptions.SelectedPaths == null || request.ReadOptions.CustomWrapperNames == null ||
            request.MetadataOptions == null || request.MetadataOptions.AdditionalReferencePaths == null ||
            request.WriteOptions == null || request.WriteOptions.NamespaceDeclarations == null)
        {
            FailProcessingConfiguration(state, "processing.configuration_invalid", "Stages, options, and positive resource limits are required.");
            return false;
        }
        if (request.Stages.Count == 0)
        {
            FailProcessingConfiguration(state, "processing.stages_missing", "At least one processing stage must be selected.");
            return false;
        }
        if (request.Stages.Count > request.Limits.MaxCollectionCount)
        {
            FailProcessingConfiguration(state, "processing.stage_limit", "The selected stage collection exceeds the configured limit.");
            return false;
        }
        int representationCount = 0;
        if (request.Xml.Length > 0)
        {
            representationCount++;
            if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
            {
                FailProcessingConfiguration(state, "processing.input_limit", "The raw input exceeds the configured total-byte limit.");
                return false;
            }
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
            FailProcessingConfiguration(state, "processing.input_invalid", "Exactly one raw, generic, generated, or canonical input is required.");
            return false;
        }
        MessageIdentifier? expected = NormalizeProcessingIdentifier(request.ExpectedSourceMessageIdentifier);
        if (request.ExpectedSourceMessageIdentifier != null && expected == null)
        {
            FailProcessingConfiguration(state, "processing.source_identifier_invalid", "The expected source message identifier is invalid.");
            return false;
        }
        MessageIdentifier? target = NormalizeProcessingIdentifier(request.TargetMessageIdentifier);
        if (request.TargetMessageIdentifier != null && target == null)
        {
            FailProcessingConfiguration(state, "processing.target_identifier_invalid", "The target message identifier is invalid.");
            return false;
        }
        return true;
    }

    internal static bool NormalizeProcessingStages(ProcessingExecutionState state)
    {
        int inputIndex = 0;
        while (inputIndex < state.Request.Stages.Count)
        {
            ProcessingStage stage = state.Request.Stages[inputIndex];
            if (!ProcessingStageIsKnown(stage))
            {
                FailProcessingConfiguration(state, "processing.stage_invalid", "The request contains an unknown processing stage.");
                return false;
            }
            int duplicateIndex = 0;
            while (duplicateIndex < inputIndex)
            {
                if (state.Request.Stages[duplicateIndex] == stage)
                {
                    FailProcessingConfiguration(state, "processing.stage_duplicate", "Each processing stage can be selected only once.");
                    return false;
                }
                duplicateIndex++;
            }
            inputIndex++;
        }

        int stageValue = (int)ProcessingStage.Detection;
        while (stageValue <= (int)ProcessingStage.Writing)
        {
            ProcessingStage candidate = (ProcessingStage)stageValue;
            inputIndex = 0;
            while (inputIndex < state.Request.Stages.Count)
            {
                if (state.Request.Stages[inputIndex] == candidate)
                {
                    state.Result.RequestedStages.Add(candidate);
                }
                inputIndex++;
            }
            stageValue++;
        }
        return true;
    }

    internal static bool ProcessingStageIsKnown(ProcessingStage stage)
    {
        switch (stage)
        {
            case ProcessingStage.Detection:
            case ProcessingStage.Reading:
            case ProcessingStage.Validation:
            case ProcessingStage.MetadataExtraction:
            case ProcessingStage.Migration:
            case ProcessingStage.Mapping:
            case ProcessingStage.Writing:
                return true;
            default:
                return false;
        }
    }

    internal static void InitializeProcessingIdentity(ProcessingExecutionState state)
    {
        if (state.Document != null)
        {
            UpdateProcessingIdentityFromDocument(state, state.Document, true);
        }
        if (state.CurrentIdentifier == null)
        {
            state.CurrentIdentifier = NormalizeProcessingIdentifier(state.Request.ExpectedSourceMessageIdentifier);
        }
        if (state.CurrentNamespaceUri.Length == 0)
        {
            state.CurrentNamespaceUri = state.Request.ExpectedSourceNamespaceUri;
        }
        state.Result.SourceMessageIdentifier = state.CurrentIdentifier;
        state.Result.SourceNamespaceUri = state.CurrentNamespaceUri;
    }

    internal static bool ExecuteProcessingStage(ProcessingExecutionState state, ProcessingStage stage)
    {
        switch (stage)
        {
            case ProcessingStage.Detection:
                return ExecuteDetectionStage(state);
            case ProcessingStage.Reading:
                return ExecuteReadingStage(state);
            case ProcessingStage.Validation:
                return ExecuteValidationStage(state);
            case ProcessingStage.MetadataExtraction:
                return ExecuteMetadataStage(state);
            case ProcessingStage.Migration:
                return ExecuteMigrationStage(state);
            case ProcessingStage.Mapping:
                return ExecuteMappingStage(state);
            case ProcessingStage.Writing:
                return ExecuteWritingStage(state);
            default:
                return false;
        }
    }

    internal static bool ExecuteDetectionStage(ProcessingExecutionState state)
    {
        if (!EnsureProcessingXml(state, ProcessingStage.Detection))
        {
            return false;
        }
        MessageReadOptions options = CopyProcessingReadOptions(state.Request.ReadOptions, state.Request.Limits);
        state.Result.Detection = new MessageDetector().DetectBytes(state.Xml, options);
        if (!state.Result.Detection.Success)
        {
            FailProcessingStage(state, ProcessingStage.Detection, state.Result.Detection.ErrorCode, state.Result.Detection.ErrorMessage);
            return false;
        }
        state.CurrentIdentifier = state.Result.Detection.MessageIdentifier;
        if (state.Result.Detection.MessageNamespace != null)
        {
            state.CurrentNamespaceUri = state.Result.Detection.MessageNamespace.Uri;
        }
        if (!ValidateCurrentProcessingIdentity(state, ProcessingStage.Detection))
        {
            return false;
        }
        PublishSourceProcessingIdentity(state);
        return true;
    }

    internal static bool ExecuteReadingStage(ProcessingExecutionState state)
    {
        if (state.Document != null)
        {
            MessageReadResult materialized = new MessageReadResult();
            materialized.Success = true;
            materialized.Document = state.Document;
            materialized.BytesConsumed = state.Xml.LongLength;
            materialized.EndOfInput = true;
            state.Result.Read = materialized;
        }
        else
        {
            if (!EnsureProcessingXml(state, ProcessingStage.Reading))
            {
                return false;
            }
            MessageReadOptions options = CopyProcessingReadOptions(state.Request.ReadOptions, state.Request.Limits);
            state.Result.Read = new XmlMessageReader().ReadAutoBytes(state.Xml, options);
            if (!state.Result.Read.Success || state.Result.Read.Document == null)
            {
                FailProcessingStage(state, ProcessingStage.Reading, state.Result.Read.ErrorCode, state.Result.Read.ErrorMessage);
                return false;
            }
            state.Document = state.Result.Read.Document;
        }
        UpdateProcessingIdentityFromDocument(state, state.Document, true);
        if (!ValidateCurrentProcessingIdentity(state, ProcessingStage.Reading))
        {
            return false;
        }
        PublishSourceProcessingIdentity(state);
        return true;
    }

    internal static bool ExecuteValidationStage(ProcessingExecutionState state)
    {
        if (state.Request.Validation == null || state.Request.Validation.Policy == null ||
            state.Request.Validation.Context == null || state.Request.Validation.SemanticRules == null)
        {
            FailProcessingStage(state, ProcessingStage.Validation, "processing.validation_request_missing", "The validation stage requires a complete validation request.");
            return false;
        }
        if (state.Document == null && state.Model == null &&
            !EnsureProcessingDocument(state, ProcessingStage.Validation))
        {
            return false;
        }
        ValidationRequest request = CopyProcessingValidationRequest(state.Request.Validation);
        request.Policy = CopyProcessingValidationPolicy(request.Policy, state.Request.Limits);
        ClearProcessingValidationRepresentations(request);
        if (state.Document != null)
        {
            request.Document = state.Document;
        }
        else if (state.Model != null)
        {
            request.Model = state.Model;
        }
        else
        {
            request.Xml = state.Xml;
        }
        if (request.ExpectedIdentifier == null)
        {
            request.ExpectedIdentifier = state.CurrentIdentifier;
        }
        if (request.ExpectedNamespaceUri.Length == 0)
        {
            request.ExpectedNamespaceUri = state.CurrentNamespaceUri;
        }
        state.Result.Validation = new MessageValidator().ValidateMessage(request);
        AddProcessingValidationReport(state, state.Result.Validation);
        AddProcessingDiagnostics(state, state.Result.Validation.Diagnostics);
        if (!state.Result.Validation.Success || !state.Result.Validation.Valid)
        {
            FailProcessingStage(state, ProcessingStage.Validation, "processing.validation_failed", "The current message did not pass direct validation.");
            return false;
        }
        if (state.CurrentIdentifier == null)
        {
            state.CurrentIdentifier = state.Result.Validation.MessageIdentifier;
        }
        if (state.CurrentNamespaceUri.Length == 0)
        {
            state.CurrentNamespaceUri = state.Result.Validation.NamespaceUri;
        }
        PublishSourceProcessingIdentity(state);
        return true;
    }

    internal static bool ExecuteMetadataStage(ProcessingExecutionState state)
    {
        if (!EnsureProcessingDocument(state, ProcessingStage.MetadataExtraction))
        {
            return false;
        }
        MetadataExtractionOptions options = MessageProjector.CopyProjectionMetadataOptions(
            state.Request.MetadataOptions,
            state.Request.Limits);
        state.Result.Metadata = new MetadataExtractor().ExtractMetadata(state.Document, options);
        if (state.Result.Metadata.MessageIdentifier != null)
        {
            state.CurrentIdentifier = state.Result.Metadata.MessageIdentifier;
        }
        if (state.Result.Metadata.NamespaceUri.Length > 0)
        {
            state.CurrentNamespaceUri = state.Result.Metadata.NamespaceUri;
        }
        if (!ValidateCurrentProcessingIdentity(state, ProcessingStage.MetadataExtraction))
        {
            return false;
        }
        PublishSourceProcessingIdentity(state);
        return true;
    }

    internal static bool ExecuteMigrationStage(ProcessingExecutionState state)
    {
        if (state.Request.Migration == null || state.Request.Migration.TargetValidationPolicy == null ||
            state.Request.Migration.ValidationContext == null)
        {
            FailProcessingStage(state, ProcessingStage.Migration, "processing.migration_request_missing", "The migration stage requires a complete migration request.");
            return false;
        }
        if (!PrepareProcessingTransformationIdentity(state, ProcessingStage.Migration))
        {
            return false;
        }
        MigrationRequest request = CopyProcessingMigrationRequest(state.Request.Migration, state);
        state.Result.Migration = new MessageMigrator().MigrateMessage(request);
        AddProcessingDiagnostics(state, state.Result.Migration.Diagnostics);
        if (state.Result.Migration.TargetValidation != null)
        {
            AddProcessingValidationReport(state, state.Result.Migration.TargetValidation);
        }
        if (!state.Result.Migration.Success)
        {
            FailProcessingStage(state, ProcessingStage.Migration, state.Result.Migration.ErrorCode, state.Result.Migration.ErrorMessage);
            return false;
        }
        UpdateProcessingStateFromMigration(state, state.Result.Migration);
        return true;
    }

    internal static bool ExecuteMappingStage(ProcessingExecutionState state)
    {
        if (state.Request.Mapping == null || state.Request.Mapping.TargetValidationPolicy == null ||
            state.Request.Mapping.ValidationContext == null)
        {
            FailProcessingStage(state, ProcessingStage.Mapping, "processing.mapping_request_missing", "The mapping stage requires a complete mapping request.");
            return false;
        }
        if (!PrepareProcessingTransformationIdentity(state, ProcessingStage.Mapping))
        {
            return false;
        }
        MappingRequest request = CopyProcessingMappingRequest(state.Request.Mapping, state);
        state.Result.Mapping = new MessageMapper().MapMessage(request);
        AddProcessingDiagnostics(state, state.Result.Mapping.Diagnostics);
        if (state.Result.Mapping.TargetValidation != null)
        {
            AddProcessingValidationReport(state, state.Result.Mapping.TargetValidation);
        }
        if (!state.Result.Mapping.Success)
        {
            FailProcessingStage(state, ProcessingStage.Mapping, state.Result.Mapping.ErrorCode, state.Result.Mapping.ErrorMessage);
            return false;
        }
        UpdateProcessingStateFromMapping(state, state.Result.Mapping);
        return true;
    }

    internal static bool ExecuteWritingStage(ProcessingExecutionState state)
    {
        if (!EnsureProcessingDocument(state, ProcessingStage.Writing))
        {
            return false;
        }
        if (!state.TargetProduced && !ValidateCurrentProcessingIdentity(state, ProcessingStage.Writing))
        {
            return false;
        }
        MessageWriteOptions options = CopyProcessingWriteOptions(state.Request.WriteOptions, state.Request.Limits);
        state.Result.Write = new XmlMessageWriter().WriteGenericString(state.Document, options);
        if (!state.Result.Write.Success)
        {
            FailProcessingStage(state, ProcessingStage.Writing, state.Result.Write.ErrorCode, state.Result.Write.ErrorMessage);
            return false;
        }
        state.Xml = Encoding.UTF8.GetBytes(state.Result.Write.Text);
        if (state.Xml.LongLength > state.Request.Limits.MaxTotalBytes)
        {
            FailProcessingStage(state, ProcessingStage.Writing, "processing.output_limit", "The written output exceeds the configured total-byte limit.");
            return false;
        }
        return true;
    }

    internal static bool EnsureProcessingXml(ProcessingExecutionState state, ProcessingStage stage)
    {
        if (state.Xml.Length > 0)
        {
            return true;
        }
        if (state.Document == null && !EnsureProcessingDocument(state, stage))
        {
            return false;
        }
        if (state.Document == null)
        {
            FailProcessingStage(state, stage, "processing.content_missing", "No message content is available for materialization.");
            return false;
        }
        MessageWriteOptions options = CopyProcessingWriteOptions(state.Request.WriteOptions, state.Request.Limits);
        MessageWriteResult write = new XmlMessageWriter().WriteGenericString(state.Document, options);
        if (!write.Success)
        {
            FailProcessingStage(state, stage, write.ErrorCode, write.ErrorMessage);
            return false;
        }
        state.Xml = Encoding.UTF8.GetBytes(write.Text);
        return true;
    }

    internal static bool EnsureProcessingDocument(ProcessingExecutionState state, ProcessingStage stage)
    {
        if (state.Document != null)
        {
            return true;
        }
        MessageReadOptions options = CopyProcessingReadOptions(state.Request.ReadOptions, state.Request.Limits);
        if (state.Xml.Length > 0)
        {
            MessageReadResult read = new XmlMessageReader().ReadAutoBytes(state.Xml, options);
            if (!read.Success || read.Document == null)
            {
                FailProcessingStage(state, stage, read.ErrorCode, read.ErrorMessage);
                return false;
            }
            state.Document = read.Document;
            UpdateProcessingIdentityFromDocument(state, state.Document, true);
            return true;
        }
        MessageDocument? modelDocument = state.Model as MessageDocument;
        if (modelDocument != null)
        {
            state.Document = modelDocument;
            UpdateProcessingIdentityFromDocument(state, state.Document, true);
            return true;
        }
        DynamicXmlNode? modelTree = state.Model as DynamicXmlNode;
        if (modelTree != null)
        {
            DynamicXmlNode tree = CanonicalXmlNodeNormalizer.CloneNode(modelTree);
            if (tree.Kind != XmlNodeKind.Document)
            {
                DynamicXmlNode wrapper = new DynamicXmlNode();
                wrapper.Kind = XmlNodeKind.Document;
                wrapper.Children.Add(tree);
                tree = wrapper;
            }
            MessageReadResult materialized = XmlMessageReader.BuildGenericResult(tree, 0, options);
            if (!materialized.Success || materialized.Document == null)
            {
                FailProcessingStage(state, stage, "processing.model_invalid", "The canonical XML model could not be materialized.");
                return false;
            }
            state.Document = materialized.Document;
            UpdateProcessingIdentityFromDocument(state, state.Document, true);
            return true;
        }
        GeneratedModelWriteResult generated = GeneratedModelXmlSerializer.WriteModel(
            state.Model,
            state.CurrentIdentifier,
            state.CurrentNamespaceUri);
        if (!generated.Success)
        {
            FailProcessingStage(state, stage, "processing." + generated.ErrorCode, generated.ErrorMessage);
            return false;
        }
        state.Xml = Encoding.UTF8.GetBytes(generated.Xml);
        MessageReadResult generatedRead = new XmlMessageReader().ReadAutoBytes(state.Xml, options);
        if (!generatedRead.Success || generatedRead.Document == null)
        {
            FailProcessingStage(state, stage, generatedRead.ErrorCode, generatedRead.ErrorMessage);
            return false;
        }
        state.Document = generatedRead.Document;
        UpdateProcessingIdentityFromDocument(state, state.Document, true);
        return true;
    }

    internal static bool PrepareProcessingTransformationIdentity(ProcessingExecutionState state, ProcessingStage stage)
    {
        if (state.TargetProduced)
        {
            return true;
        }
        if (state.Request.ExpectedSourceMessageIdentifier == null &&
            state.Request.ExpectedSourceNamespaceUri.Length == 0)
        {
            return true;
        }
        if (!EnsureProcessingDocument(state, stage))
        {
            return false;
        }
        return ValidateCurrentProcessingIdentity(state, stage);
    }

    internal static MessageReadOptions CopyProcessingReadOptions(MessageReadOptions source, ResourceLimits limits)
    {
        MessageReadOptions copy = new MessageReadOptions();
        copy.NamespaceMode = source.NamespaceMode;
        copy.UnknownContentMode = source.UnknownContentMode;
        copy.VersionSelector = source.VersionSelector;
        copy.Limits = limits;
        copy.PreserveComments = source.PreserveComments;
        copy.PreserveProcessingInstructions = source.PreserveProcessingInstructions;
        copy.PreserveWhitespace = source.PreserveWhitespace;
        copy.PreserveLexicalValues = source.PreserveLexicalValues;
        int index = 0;
        while (index < source.SelectedPaths.Count)
        {
            copy.SelectedPaths.Add(source.SelectedPaths[index]);
            index++;
        }
        index = 0;
        while (index < source.CustomWrapperNames.Count)
        {
            copy.CustomWrapperNames.Add(source.CustomWrapperNames[index]);
            index++;
        }
        return copy;
    }

    internal static MessageWriteOptions CopyProcessingWriteOptions(MessageWriteOptions source, ResourceLimits limits)
    {
        MessageWriteOptions copy = new MessageWriteOptions();
        copy.FormattingMode = source.FormattingMode;
        copy.UnknownContentMode = source.UnknownContentMode;
        copy.Limits = limits;
        copy.WriteXmlDeclaration = source.WriteXmlDeclaration;
        copy.WriteByteOrderMark = source.WriteByteOrderMark;
        copy.PreservePrefixes = source.PreservePrefixes;
        copy.PreserveLexicalValues = source.PreserveLexicalValues;
        copy.IndentText = source.IndentText;
        copy.NewLineText = source.NewLineText;
        int index = 0;
        while (index < source.NamespaceDeclarations.Count)
        {
            copy.NamespaceDeclarations.Add(source.NamespaceDeclarations[index]);
            index++;
        }
        return copy;
    }

    internal static ValidationRequest CopyProcessingValidationRequest(ValidationRequest source)
    {
        ValidationRequest copy = new ValidationRequest();
        copy.Xml = source.Xml;
        copy.Document = source.Document;
        copy.Model = source.Model;
        copy.ExpectedIdentifier = source.ExpectedIdentifier;
        copy.ExpectedNamespaceUri = source.ExpectedNamespaceUri;
        copy.Schema = source.Schema;
        copy.XsdValidator = source.XsdValidator;
        copy.SchemaOptions = source.SchemaOptions;
        copy.Policy = source.Policy;
        copy.Context = source.Context;
        copy.Profile = source.Profile;
        copy.CodeSetProvider = source.CodeSetProvider;
        copy.ProfileProvider = source.ProfileProvider;
        int index = 0;
        while (index < source.SemanticRules.Count)
        {
            copy.SemanticRules.Add(source.SemanticRules[index]);
            index++;
        }
        return copy;
    }

    internal static ValidationPolicy CopyProcessingValidationPolicy(ValidationPolicy source, ResourceLimits limits)
    {
        ValidationPolicy copy = new ValidationPolicy();
        int index = 0;
        while (index < source.Stages.Count)
        {
            copy.Stages.Add(source.Stages[index]);
            index++;
        }
        copy.FailFast = source.FailFast;
        copy.TreatWarningsAsErrors = source.TreatWarningsAsErrors;
        copy.Limits = limits;
        copy.AllowDraftArtifacts = source.AllowDraftArtifacts;
        copy.ExternalCodeEdition = source.ExternalCodeEdition;
        copy.SelectLatestExternalCodeEdition = source.SelectLatestExternalCodeEdition;
        copy.IncludeInactiveExternalCodes = source.IncludeInactiveExternalCodes;
        copy.UnknownContentMode = source.UnknownContentMode;
        copy.RequireTimezone = source.RequireTimezone;
        copy.RequireValidSignature = source.RequireValidSignature;
        copy.ValidatePresentSignatures = source.ValidatePresentSignatures;
        copy.RedactOffendingValues = source.RedactOffendingValues;
        return copy;
    }

    internal static void ClearProcessingValidationRepresentations(ValidationRequest request)
    {
        request.Xml = Array.Empty<byte>();
        request.Document = null;
        request.Model = null;
    }

    internal static MigrationRequest CopyProcessingMigrationRequest(MigrationRequest source, ProcessingExecutionState state)
    {
        MigrationRequest copy = new MigrationRequest();
        copy.SourceRepository = source.SourceRepository;
        copy.TargetRepository = source.TargetRepository;
        copy.SourceMessageIdentifier = source.SourceMessageIdentifier ?? state.CurrentIdentifier;
        copy.TargetMessageIdentifier = state.Request.TargetMessageIdentifier ?? source.TargetMessageIdentifier;
        CopyCurrentProcessingRepresentation(state, copy, null);
        copy.Difference = source.Difference;
        copy.Plan = source.Plan;
        copy.Mapping = source.Mapping;
        copy.AllowSupplementaryFallback = source.AllowSupplementaryFallback;
        copy.ValidateTarget = source.ValidateTarget;
        copy.Validator = source.Validator;
        copy.ValidationContext = source.ValidationContext;
        copy.TargetValidationPolicy = CopyProcessingValidationPolicy(source.TargetValidationPolicy, state.Request.Limits);
        copy.Limits = state.Request.Limits;
        copy.CancellationToken = state.Request.CancellationToken;
        return copy;
    }

    internal static MappingRequest CopyProcessingMappingRequest(MappingRequest source, ProcessingExecutionState state)
    {
        MappingRequest copy = new MappingRequest();
        CopyCurrentProcessingRepresentation(state, null, copy);
        copy.Mapping = source.Mapping;
        copy.TargetMessageIdentifier = state.Request.TargetMessageIdentifier ?? source.TargetMessageIdentifier;
        copy.ValidateTarget = source.ValidateTarget;
        copy.Validator = source.Validator;
        copy.TargetValidationPolicy = CopyProcessingValidationPolicy(source.TargetValidationPolicy, state.Request.Limits);
        copy.ValidationContext = source.ValidationContext;
        copy.Limits = state.Request.Limits;
        copy.CancellationToken = state.Request.CancellationToken;
        return copy;
    }

    internal static void CopyCurrentProcessingRepresentation(ProcessingExecutionState state, MigrationRequest? migration, MappingRequest? mapping)
    {
        if (state.Document != null)
        {
            if (migration != null)
            {
                migration.Document = state.Document;
            }
            if (mapping != null)
            {
                mapping.Document = state.Document;
            }
        }
        else if (state.Model != null)
        {
            if (migration != null)
            {
                migration.Model = state.Model;
            }
            if (mapping != null)
            {
                mapping.Model = state.Model;
            }
        }
        else
        {
            if (migration != null)
            {
                migration.Xml = state.Xml;
            }
            if (mapping != null)
            {
                mapping.Xml = state.Xml;
            }
        }
    }

    internal static void UpdateProcessingStateFromMigration(ProcessingExecutionState state, MigrationResult migration)
    {
        state.Document = migration.Document;
        state.Model = migration.Model;
        state.Xml = migration.Xml;
        state.CurrentIdentifier = migration.TargetMessageIdentifier;
        state.CurrentNamespaceUri = ResolveProcessingTargetNamespace(
            state.Request.TargetNamespaceUri,
            migration.TargetMessageIdentifier,
            migration.Document);
        state.Result.TargetMessageIdentifier = state.CurrentIdentifier;
        state.Result.TargetNamespaceUri = state.CurrentNamespaceUri;
        state.TargetProduced = true;
    }

    internal static void UpdateProcessingStateFromMapping(ProcessingExecutionState state, MappingResult mapping)
    {
        state.Document = mapping.Document;
        state.Model = mapping.Model;
        state.Xml = mapping.Xml;
        state.CurrentIdentifier = mapping.TargetMessageIdentifier;
        state.CurrentNamespaceUri = ResolveProcessingTargetNamespace(
            state.Request.TargetNamespaceUri,
            mapping.TargetMessageIdentifier,
            mapping.Document);
        state.Result.TargetMessageIdentifier = state.CurrentIdentifier;
        state.Result.TargetNamespaceUri = state.CurrentNamespaceUri;
        state.TargetProduced = true;
    }

    internal static string ResolveProcessingTargetNamespace(string requested, MessageIdentifier? identifier, MessageDocument? document)
    {
        if (requested.Length > 0)
        {
            return requested;
        }
        if (document != null && document.Metadata.NamespaceUri.Length > 0)
        {
            return document.Metadata.NamespaceUri;
        }
        MessageIdentifier? normalized = NormalizeProcessingIdentifier(identifier);
        if (normalized != null)
        {
            return Iso20022Namespaces.SchemaUrnPrefix + normalized.Value;
        }
        return string.Empty;
    }

    internal static void UpdateProcessingIdentityFromDocument(ProcessingExecutionState state, MessageDocument? document, bool overwrite)
    {
        if (document == null)
        {
            return;
        }
        MessageMetadata metadata = new MessageMetadata();
        MetadataExtractor.ExtractStructuralIdentity(document, metadata);
        if (metadata.MessageIdentifier != null && (overwrite || state.CurrentIdentifier == null))
        {
            state.CurrentIdentifier = metadata.MessageIdentifier;
        }
        if (metadata.NamespaceUri.Length > 0 && (overwrite || state.CurrentNamespaceUri.Length == 0))
        {
            state.CurrentNamespaceUri = metadata.NamespaceUri;
        }
    }

    internal static bool ValidateCurrentProcessingIdentity(ProcessingExecutionState state, ProcessingStage stage)
    {
        MessageIdentifier? expected = NormalizeProcessingIdentifier(state.Request.ExpectedSourceMessageIdentifier);
        if (expected != null && state.CurrentIdentifier != null &&
            expected.Value != MessageBuilder.GetExactIdentifierValue(state.CurrentIdentifier))
        {
            FailProcessingStage(state, stage, "processing.source_identifier_mismatch", "The resolved source identifier does not match the expected exact identifier.");
            return false;
        }
        if (state.Request.ExpectedSourceNamespaceUri.Length > 0 &&
            state.CurrentNamespaceUri.Length > 0 &&
            state.Request.ExpectedSourceNamespaceUri != state.CurrentNamespaceUri)
        {
            FailProcessingStage(state, stage, "processing.source_namespace_mismatch", "The resolved source namespace does not match the expected exact namespace.");
            return false;
        }
        return true;
    }

    internal static MessageIdentifier? NormalizeProcessingIdentifier(MessageIdentifier? identifier)
    {
        return GeneratedModelXmlSerializer.NormalizeGeneratedIdentifier(identifier);
    }

    internal static void PublishSourceProcessingIdentity(ProcessingExecutionState state)
    {
        if (state.Result.SourceMessageIdentifier == null)
        {
            state.Result.SourceMessageIdentifier = state.CurrentIdentifier;
        }
        if (state.Result.SourceNamespaceUri.Length == 0)
        {
            state.Result.SourceNamespaceUri = state.CurrentNamespaceUri;
        }
    }

    internal static void PublishFinalProcessingState(ProcessingExecutionState state)
    {
        state.Result.Document = state.Document;
        state.Result.Model = state.Model;
        state.Result.Xml = state.Xml;
        PublishSourceProcessingIdentity(state);
    }

    internal static void AddProcessingValidationReport(ProcessingExecutionState state, ValidationReport report)
    {
        if (!state.Result.ValidationReports.Contains(report))
        {
            state.Result.ValidationReports.Add(report);
        }
    }

    internal static void AddProcessingDiagnostics(ProcessingExecutionState state, System.Collections.Generic.List<ValidationDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count && state.Result.Diagnostics.Count < state.Request.Limits.MaxDiagnosticCount)
        {
            state.Result.Diagnostics.Add(diagnostics[index]);
            index++;
        }
    }

    internal static ProcessingResult StopProcessing(ProcessingExecutionState state, ProcessingStage stage, string code, string message)
    {
        FailProcessingStage(state, stage, code, message);
        return state.Result;
    }

    internal static void FailProcessingConfiguration(ProcessingExecutionState state, string code, string message)
    {
        state.Result.ErrorCode = code;
        state.Result.ErrorMessage = message;
        AddProcessingFailureDiagnostic(state.Result, code, message);
        PublishFinalProcessingState(state);
    }

    internal static void FailProcessingStage(ProcessingExecutionState state, ProcessingStage stage, string code, string message)
    {
        state.Result.Success = false;
        state.Result.FailedStage = stage;
        state.Result.ErrorCode = code.Length == 0 ? "processing.stage_failed" : code;
        state.Result.ErrorMessage = message.Length == 0 ? "The selected processing stage failed." : message;
        AddProcessingFailureDiagnostic(state.Result, state.Result.ErrorCode, state.Result.ErrorMessage);
        PublishFinalProcessingState(state);
    }

    internal static ProcessingResult CreateProcessingFailure(string code, string message)
    {
        ProcessingResult result = new ProcessingResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        AddProcessingFailureDiagnostic(result, code, message);
        return result;
    }

    internal static void AddProcessingFailureDiagnostic(ProcessingResult result, string code, string message)
    {
        if (result.Diagnostics.Count > 0 && result.Diagnostics[result.Diagnostics.Count - 1].Code == code)
        {
            return;
        }
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = code;
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.Unknown;
        diagnostic.Kind = DiagnosticKind.Configuration;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }
}
