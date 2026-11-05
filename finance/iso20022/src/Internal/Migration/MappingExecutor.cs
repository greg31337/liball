using System;
using System.Collections.Generic;

namespace iso20022;

internal static class MappingExecutor
{
    internal static MappingResult ExecuteMapping(MappingRequest request)
    {
        MappingExecutionState state = new MappingExecutionState();
        state.Request = request;
        if (request.CancellationToken.IsCancellationRequested)
        {
            return FailMapping(state.Result, "mapping.cancelled", "Mapping was cancelled.");
        }

        if (!ValidateMappingConfiguration(state))
        {
            return state.Result;
        }

        if (!ReadMappingSource(state) || !CreateMappingTarget(state))
        {
            return state.Result;
        }

        MappingDefinition mapping = state.Result.Mapping == null ?
            new MappingDefinition() : state.Result.Mapping;
        int ruleIndex = 0;
        while (ruleIndex < mapping.Rules.Count)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                return FailMapping(state.Result, "mapping.cancelled", "Mapping was cancelled.");
            }

            MappingRuleDefinition rule = mapping.Rules[ruleIndex];
            bool applied = ApplyMappingRule(state, rule);
            if (applied)
            {
                state.Result.AppliedRules.Add(rule);
                if (rule.Lossy || rule.Action == MappingActionKind.Drop)
                {
                    state.Result.DataLoss = true;
                }
            }
            else
            {
                state.Result.FailedRules.Add(rule);
                AddRuleDiagnostic(state, rule);
                RecordMissingRuleTargets(state, rule);
            }

            ruleIndex++;
        }

        CollectUnmappedSource(state);
        if (HasRequiredRuleFailure(state.Result))
        {
            return FailMapping(
                state.Result,
                "mapping.required_rule_failed",
                "At least one required mapping rule failed.");
        }

        if (state.Result.UnmappedSourcePaths.Count > 0 && !mapping.AllowUnmappedSource)
        {
            return FailMapping(
                state.Result,
                "mapping.source_unmapped",
                "The mapping left source content unmapped.");
        }

        if (state.Result.MissingTargetPaths.Count > 0 && mapping.RequireAllTargets)
        {
            return FailMapping(
                state.Result,
                "mapping.target_missing",
                "The mapping did not produce every required target.");
        }

        if (!WriteMappingTarget(state) || !ValidateMappedTarget(state))
        {
            return state.Result;
        }

        state.Result.Success = true;
        return state.Result;
    }

    internal static bool ValidateMappingConfiguration(MappingExecutionState state)
    {
        MappingRequest request = state.Request;
        MappingDefinition? mapping = request.Mapping;
        if (mapping == null)
        {
            FailMapping(state.Result, "mapping.definition_missing", "An explicit mapping definition is required.");
            return false;
        }

        state.Result.Mapping = mapping;
        if (request.Limits == null || request.Limits.MaxCollectionCount < 1 ||
            request.Limits.MaxElementCount < 1 || request.Limits.MaxTotalBytes < 1 ||
            request.Limits.MaxDiagnosticCount < 1)
        {
            FailMapping(state.Result, "mapping.resource_limit_invalid", "Mapping resource limits must be positive.");
            return false;
        }

        if (mapping.Rules.Count > request.Limits.MaxCollectionCount)
        {
            FailMapping(state.Result, "mapping.rule_limit", "The mapping exceeds the configured rule limit.");
            return false;
        }

        MessageIdentifier? targetIdentifier = request.TargetMessageIdentifier == null ?
            mapping.TargetMessageIdentifier : request.TargetMessageIdentifier;
        if (targetIdentifier != null && MessageIdentifierLexical.FormatMessageIdentifier(targetIdentifier) == null)
        {
            FailMapping(state.Result, "mapping.target_identifier_invalid", "The target message identifier is invalid.");
            return false;
        }

        if (targetIdentifier == null && mapping.TargetModelName.Length == 0)
        {
            FailMapping(state.Result, "mapping.target_missing", "A target message identifier or canonical-model name is required.");
            return false;
        }

        if (targetIdentifier != null && mapping.TargetModelName.Length > 0)
        {
            FailMapping(state.Result, "mapping.target_ambiguous", "A mapping cannot select a message and canonical-model target together.");
            return false;
        }

        state.Result.TargetMessageIdentifier = targetIdentifier;
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
            FailMapping(state.Result, "mapping.input_invalid", "Exactly one source representation is required.");
            return false;
        }

        int ruleIndex = 0;
        while (ruleIndex < mapping.Rules.Count)
        {
            MappingRuleDefinition rule = mapping.Rules[ruleIndex];
            if (rule.Sequence != ruleIndex)
            {
                FailMapping(state.Result, "mapping.rule_sequence_invalid", "Mapping rule sequences must be contiguous and deterministic.");
                return false;
            }

            string ruleError = MigrationPlanBuilder.ValidateExplicitRule(rule);
            if (ruleError.Length > 0)
            {
                FailMapping(state.Result, "mapping.rule_invalid", ruleError);
                return false;
            }

            ruleIndex++;
        }

        return true;
    }

    internal static bool ReadMappingSource(MappingExecutionState state)
    {
        MappingRequest request = state.Request;
        if (request.Xml.Length > 0)
        {
            if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
            {
                FailMapping(state.Result, "mapping.input_limit", "The raw mapping input exceeds the configured byte limit.");
                return false;
            }

            MessageReadOptions options = CreateMappingReadOptions(request.Limits);
            state.SourceTree = new GenericXmlNodeReader(options).ReadDocumentFromBytes(request.Xml, null);
            return ValidateSourceIdentity(state);
        }

        if (request.Document != null)
        {
            DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
                request.Document,
                MigrationExecutor.CreateWriteOptions(request.Limits));
            if (tree == null)
            {
                FailMapping(state.Result, "mapping.input_content_missing", "The generic source document has no XML content.");
                return false;
            }

            state.SourceTree = CanonicalXmlNodeNormalizer.CloneNode(tree);
            return ValidateSourceIdentity(state);
        }

        DynamicXmlNode? modelNode = request.Model as DynamicXmlNode;
        if (modelNode != null)
        {
            state.SourceTree = CanonicalXmlNodeNormalizer.CloneNode(modelNode);
            if (state.SourceTree.Kind != XmlNodeKind.Document)
            {
                DynamicXmlNode document = new DynamicXmlNode();
                document.Kind = XmlNodeKind.Document;
                document.Children.Add(state.SourceTree);
                state.SourceTree = document;
            }

            state.InputWasModel = true;
            return ValidateSourceModelName(state);
        }

        MessageDocument? modelDocument = request.Model as MessageDocument;
        if (modelDocument != null)
        {
            DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
                modelDocument,
                MigrationExecutor.CreateWriteOptions(request.Limits));
            if (tree != null)
            {
                state.SourceTree = CanonicalXmlNodeNormalizer.CloneNode(tree);
                state.InputWasModel = true;
                return ValidateSourceIdentity(state);
            }
        }

        FailMapping(
            state.Result,
            "mapping.model_unsupported",
            "Canonical models must be supplied as DynamicXmlNode or MessageDocument; implicit reflection mapping is not supported.");
        return false;
    }

    internal static bool ValidateSourceIdentity(MappingExecutionState state)
    {
        MappingDefinition? mapping = state.Result.Mapping;
        if (mapping == null || mapping.SourceMessageIdentifier == null)
        {
            return true;
        }

        DynamicXmlNode? root = MigrationExecutor.FindFirstElement(state.SourceTree);
        if (root == null || root.Name.NamespaceUri.Length == 0)
        {
            return true;
        }

        string? expected = MessageIdentifierLexical.FormatMessageIdentifier(mapping.SourceMessageIdentifier);
        string suffix = expected == null ? string.Empty : ":" + expected;
        if (suffix.Length == 0 || !root.Name.NamespaceUri.EndsWith(suffix, System.StringComparison.Ordinal))
        {
            FailMapping(state.Result, "mapping.source_identifier_mismatch", "The source message does not match the mapping definition.");
            return false;
        }

        return true;
    }

    internal static bool ValidateSourceModelName(MappingExecutionState state)
    {
        MappingDefinition? mapping = state.Result.Mapping;
        if (mapping == null || mapping.SourceModelName.Length == 0)
        {
            return true;
        }

        DynamicXmlNode? root = MigrationExecutor.FindFirstElement(state.SourceTree);
        if (root == null || root.Name.LocalName != mapping.SourceModelName)
        {
            FailMapping(state.Result, "mapping.source_model_mismatch", "The source canonical model does not match the mapping definition.");
            return false;
        }

        return true;
    }

    internal static bool CreateMappingTarget(MappingExecutionState state)
    {
        MappingDefinition mapping = state.Result.Mapping == null ? new MappingDefinition() : state.Result.Mapping;
        DynamicXmlNode document = new DynamicXmlNode();
        document.Kind = XmlNodeKind.Document;
        DynamicXmlNode root;
        if (state.Result.TargetMessageIdentifier != null)
        {
            string? identifier = MessageIdentifierLexical.FormatMessageIdentifier(state.Result.TargetMessageIdentifier);
            if (identifier == null)
            {
                return false;
            }

            string namespaceUri = "urn:iso:std:iso:20022:tech:xsd:" + identifier;
            root = MigrationExecutor.CreateElementNode("Document", namespaceUri, string.Empty);
            NamespaceDeclarationData declaration = new NamespaceDeclarationData();
            declaration.NamespaceUri = namespaceUri;
            root.NamespaceDeclarations.Add(declaration);
        }
        else
        {
            if (!XmlNameLexical.IsNcName(mapping.TargetModelName.AsSpan()))
            {
                FailMapping(state.Result, "mapping.target_model_invalid", "The canonical target model name is not a valid XML name.");
                return false;
            }

            root = MigrationExecutor.CreateElementNode(mapping.TargetModelName, string.Empty, string.Empty);
            state.TargetIsModel = true;
        }

        document.Children.Add(root);
        state.TargetTree = document;
        return true;
    }

    internal static bool ApplyMappingRule(MappingExecutionState state, MappingRuleDefinition rule)
    {
        MigrationPlan plan = new MigrationPlan();
        MigrationStep? created = MigrationPlanBuilder.CreateExplicitStep(rule, plan);
        if (created == null)
        {
            return false;
        }

        MigrationStep step = created;
        ExpandMemberPaths(state, rule, step);
        if (rule.Action == MappingActionKind.Constant)
        {
            return ApplyMappingConstant(state, step);
        }

        List<MigrationNodeMatch> sources = MigrationExecutor.FindStepSources(state.SourceTree, step);
        if (sources.Count == 0)
        {
            return !rule.Required;
        }

        if (rule.Action == MappingActionKind.Custom || rule.Action == MappingActionKind.Unknown)
        {
            return false;
        }

        if (rule.Action == MappingActionKind.Drop)
        {
            MarkSourcesConsumed(state, sources);
            return true;
        }

        if (rule.Action == MappingActionKind.Merge)
        {
            return ApplyMappingMerge(state, step, sources);
        }

        if (step.TargetPaths.Count == 0)
        {
            return false;
        }

        if (rule.Action == MappingActionKind.CodeMap)
        {
            return ApplyMappingCodeMap(state, step, sources);
        }

        if (rule.Action == MappingActionKind.Convert)
        {
            return ApplyMappingConversion(state, step, sources);
        }

        bool copied = CopyMappingSources(state, step, sources);
        if (copied)
        {
            MarkSourcesConsumed(state, sources);
        }

        return copied;
    }

    internal static void ExpandMemberPaths(
        MappingExecutionState state,
        MappingRuleDefinition rule,
        MigrationStep step)
    {
        DynamicXmlNode? sourceRoot = MigrationExecutor.FindFirstElement(state.SourceTree);
        if (state.InputWasModel && rule.SourcePaths.Count == 0 &&
            rule.SourceMember.Length > 0 && sourceRoot != null && step.SourcePaths.Count > 0)
        {
            step.SourcePaths[0].Value = "/" + sourceRoot.Name.LocalName + "/" + rule.SourceMember;
        }

        DynamicXmlNode? targetRoot = MigrationExecutor.FindFirstElement(state.TargetTree);
        if (state.TargetIsModel && rule.TargetPaths.Count == 0 &&
            rule.TargetMember.Length > 0 && targetRoot != null && step.TargetPaths.Count > 0)
        {
            step.TargetPaths[0].Value = "/" + targetRoot.Name.LocalName + "/" + rule.TargetMember;
        }
    }

    internal static bool ApplyMappingConstant(MappingExecutionState state, MigrationStep step)
    {
        if (step.TargetPaths.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < step.TargetPaths.Count)
        {
            DynamicXmlNode? target = MigrationExecutor.EnsureElementPath(
                state.TargetTree,
                step.TargetPaths[index].Value);
            if (target == null)
            {
                return false;
            }

            MigrationExecutor.SetElementText(target, step.DefaultValue);
            index++;
        }

        return true;
    }

    internal static bool ApplyMappingMerge(
        MappingExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        if (step.TargetPaths.Count != 1)
        {
            return false;
        }

        string value = string.Empty;
        int index = 0;
        while (index < sources.Count)
        {
            value += MigrationExecutor.ReadElementText(sources[index].Node);
            index++;
        }

        DynamicXmlNode? target = MigrationExecutor.EnsureElementPath(
            state.TargetTree,
            step.TargetPaths[0].Value);
        if (target == null)
        {
            return false;
        }

        MigrationExecutor.SetElementText(target, value);
        MarkSourcesConsumed(state, sources);
        return true;
    }

    internal static bool ApplyMappingCodeMap(
        MappingExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        if (step.SourceCodeValues.Count != step.TargetCodeValues.Count)
        {
            return false;
        }

        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            string value = MigrationExecutor.ReadElementText(sources[sourceIndex].Node);
            bool found = false;
            string mappedValue = string.Empty;
            int codeIndex = 0;
            while (codeIndex < step.SourceCodeValues.Count)
            {
                if (step.SourceCodeValues[codeIndex] == value)
                {
                    mappedValue = step.TargetCodeValues[codeIndex];
                    found = true;
                    break;
                }

                codeIndex++;
            }

            if (!found)
            {
                return false;
            }

            DynamicXmlNode converted = CanonicalXmlNodeNormalizer.CloneNode(sources[sourceIndex].Node);
            MigrationExecutor.SetElementText(converted, mappedValue);
            if (!MigrationExecutor.PlaceConvertedNode(state.TargetTree, step.TargetPaths, converted))
            {
                return false;
            }

            sourceIndex++;
        }

        MarkSourcesConsumed(state, sources);
        return true;
    }

    internal static bool ApplyMappingConversion(
        MappingExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        string converter = step.ConverterIdentifier;
        bool identity = converter == "identity" || converter == "decimal-to-text" ||
            converter == "iso20022.convert.type" ||
            converter == "iso20022.convert.cardinality" ||
            converter == "iso20022.convert.facet";
        bool textConverter = converter == "trim" || converter == "uppercase" ||
            converter == "lowercase";
        if (!identity && !textConverter)
        {
            return false;
        }

        int index = 0;
        while (index < sources.Count)
        {
            DynamicXmlNode converted = CanonicalXmlNodeNormalizer.CloneNode(sources[index].Node);
            if (textConverter)
            {
                string value = MigrationExecutor.ReadElementText(converted);
                if (converter == "trim")
                {
                    value = value.Trim();
                }
                else if (converter == "uppercase")
                {
                    value = value.ToUpperInvariant();
                }
                else
                {
                    value = value.ToLowerInvariant();
                }

                MigrationExecutor.SetElementText(converted, value);
            }

            if (!MigrationExecutor.PlaceConvertedNode(state.TargetTree, step.TargetPaths, converted))
            {
                return false;
            }

            index++;
        }

        MarkSourcesConsumed(state, sources);
        return true;
    }

    internal static bool CopyMappingSources(
        MappingExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        if (step.TargetPaths.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < sources.Count)
        {
            if (!MigrationExecutor.PlaceConvertedNode(
                state.TargetTree,
                step.TargetPaths,
                sources[index].Node))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static void MarkSourcesConsumed(
        MappingExecutionState state,
        List<MigrationNodeMatch> sources)
    {
        int index = 0;
        while (index < sources.Count)
        {
            if (!state.ConsumedNodes.Contains(sources[index].Node))
            {
                state.ConsumedNodes.Add(sources[index].Node);
            }

            index++;
        }
    }

    internal static void RecordMissingRuleTargets(
        MappingExecutionState state,
        MappingRuleDefinition rule)
    {
        if (!rule.Required)
        {
            return;
        }

        int index = 0;
        while (index < rule.TargetPaths.Count)
        {
            AddMissingTargetPath(state.Result, rule.TargetPaths[index]);
            index++;
        }

        if (rule.TargetPaths.Count == 0 && rule.TargetMember.Length > 0)
        {
            XmlPathData path = new XmlPathData();
            path.Value = "/" + rule.TargetMember;
            AddMissingTargetPath(state.Result, path);
        }
    }

    internal static void AddMissingTargetPath(MappingResult result, XmlPathData path)
    {
        int index = 0;
        while (index < result.MissingTargetPaths.Count)
        {
            if (result.MissingTargetPaths[index].Value == path.Value)
            {
                return;
            }

            index++;
        }

        result.MissingTargetPaths.Add(path);
    }

    internal static bool HasRequiredRuleFailure(MappingResult result)
    {
        int index = 0;
        while (index < result.FailedRules.Count)
        {
            if (result.FailedRules[index].Required)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static void CollectUnmappedSource(MappingExecutionState state)
    {
        int index = 0;
        while (index < state.SourceTree.Children.Count)
        {
            CollectUnmappedNode(
                state,
                state.SourceTree.Children[index],
                string.Empty,
                false);
            index++;
        }
    }

    internal static void CollectUnmappedNode(
        MappingExecutionState state,
        DynamicXmlNode node,
        string parentPath,
        bool ancestorConsumed)
    {
        string path = parentPath;
        bool consumed = ancestorConsumed || state.ConsumedNodes.Contains(node);
        if (node.Kind == XmlNodeKind.Element)
        {
            path += "/" + node.Name.LocalName;
        }

        bool hasElementChild = false;
        int index = 0;
        while (index < node.Children.Count)
        {
            if (node.Children[index].Kind == XmlNodeKind.Element)
            {
                hasElementChild = true;
            }

            index++;
        }

        if (node.Kind == XmlNodeKind.Element && !hasElementChild && !consumed)
        {
            AddUnmappedSourcePath(state.Result, path);
            return;
        }

        index = 0;
        while (index < node.Children.Count)
        {
            CollectUnmappedNode(state, node.Children[index], path, consumed);
            index++;
        }
    }

    internal static void AddUnmappedSourcePath(MappingResult result, string value)
    {
        int index = 0;
        while (index < result.UnmappedSourcePaths.Count)
        {
            if (result.UnmappedSourcePaths[index].Value == value)
            {
                return;
            }

            index++;
        }

        XmlPathData path = new XmlPathData();
        path.Value = value;
        result.UnmappedSourcePaths.Add(path);
    }

    internal static bool WriteMappingTarget(MappingExecutionState state)
    {
        MessageWriteOptions writeOptions = MigrationExecutor.CreateWriteOptions(state.Request.Limits);
        byte[] bytes = new GenericXmlNodeWriter(writeOptions).WriteBytes(state.TargetTree);
        if (bytes.LongLength > state.Request.Limits.MaxTotalBytes)
        {
            FailMapping(state.Result, "mapping.output_limit", "The mapped output exceeds the configured byte limit.");
            return false;
        }

        state.Result.Xml = bytes;
        DynamicXmlNode? root = MigrationExecutor.FindFirstElement(state.TargetTree);
        if (state.TargetIsModel)
        {
            state.Result.Model = root == null ? null : CanonicalXmlNodeNormalizer.CloneNode(root);
            return root != null;
        }

        MessageReadOptions readOptions = CreateMappingReadOptions(state.Request.Limits);
        MessageReadResult readResult = XmlMessageReader.BuildGenericResult(
            state.TargetTree,
            bytes.LongLength,
            readOptions);
        if (!readResult.Success || readResult.Document == null)
        {
            FailMapping(state.Result, "mapping.output_invalid", "The mapped generic output could not be materialized.");
            return false;
        }

        state.Result.Document = readResult.Document;
        state.Result.Document.Metadata.MessageIdentifier = state.Result.TargetMessageIdentifier;
        int index = 0;
        while (index < state.Result.Document.Payloads.Count)
        {
            state.Result.Document.Payloads[index].Identifier = state.Result.TargetMessageIdentifier;
            index++;
        }

        return true;
    }

    internal static bool ValidateMappedTarget(MappingExecutionState state)
    {
        if (!state.Request.ValidateTarget)
        {
            return true;
        }

        if (state.Request.Validator == null)
        {
            FailMapping(state.Result, "mapping.validator_required", "Target validation was requested but no validator was supplied.");
            return false;
        }

        ValidationRequest request = new ValidationRequest();
        if (state.TargetIsModel)
        {
            request.Model = state.Result.Model;
        }
        else
        {
            request.Document = state.Result.Document;
        }

        request.ExpectedIdentifier = state.Result.TargetMessageIdentifier;
        request.Policy = state.Request.TargetValidationPolicy;
        request.Context = state.Request.ValidationContext;
        ValidationReport report = state.Request.Validator.ValidateMessage(request);
        state.Result.TargetValidation = report;
        CopyMappedDiagnostics(state, report);
        if (!report.Success)
        {
            FailMapping(state.Result, "mapping.target_validation_failed", "Target validation could not complete.");
            return false;
        }

        if (!report.Valid)
        {
            FailMapping(state.Result, "mapping.target_invalid", "The mapped output did not pass target validation.");
            return false;
        }

        return true;
    }

    internal static void CopyMappedDiagnostics(MappingExecutionState state, ValidationReport report)
    {
        int index = 0;
        while (index < report.Diagnostics.Count &&
            state.Result.Diagnostics.Count < state.Request.Limits.MaxDiagnosticCount)
        {
            state.Result.Diagnostics.Add(report.Diagnostics[index]);
            index++;
        }
    }

    internal static void AddRuleDiagnostic(MappingExecutionState state, MappingRuleDefinition rule)
    {
        if (state.Result.Diagnostics.Count >= state.Request.Limits.MaxDiagnosticCount)
        {
            return;
        }

        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = "mapping.rule_failed";
        diagnostic.RuleIdentifier = rule.Identifier.Length == 0 ?
            rule.ConverterIdentifier : rule.Identifier;
        diagnostic.Severity = rule.Required ? ValidationSeverity.Error : ValidationSeverity.Warning;
        diagnostic.Stage = ValidationStage.Structural;
        diagnostic.Kind = rule.Action == MappingActionKind.Custom ?
            DiagnosticKind.UnsupportedConstraint : DiagnosticKind.ValidationFailure;
        diagnostic.Message = "Mapping rule " +
            rule.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            " could not be applied.";
        if (rule.SourcePaths.Count > 0)
        {
            diagnostic.Path = rule.SourcePaths[0];
        }

        state.Result.Diagnostics.Add(diagnostic);
    }

    internal static MessageReadOptions CreateMappingReadOptions(ResourceLimits limits)
    {
        MessageReadOptions options = new MessageReadOptions();
        options.Limits = limits;
        options.NamespaceMode = NamespaceMode.Lenient;
        options.UnknownContentMode = UnknownContentMode.Preserve;
        return options;
    }

    internal static MappingResult FailMapping(MappingResult result, string code, string message)
    {
        result.Success = false;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
