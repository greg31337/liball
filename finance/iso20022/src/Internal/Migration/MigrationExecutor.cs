using System.Collections.Generic;

namespace iso20022;

internal static class MigrationExecutor
{
    internal static MigrationResult ExecuteMigration(MigrationRequest request)
    {
        MigrationExecutionState state = new MigrationExecutionState();
        state.Request = request;
        if (request.CancellationToken.IsCancellationRequested)
        {
            return FailResult(state.Result, "migration.cancelled", "Migration was cancelled.");
        }

        MigrationPlan? plan = request.Plan;
        if (plan == null)
        {
            plan = new MigrationPlanner().PlanMigration(request);
        }

        if (plan == null || !plan.Success)
        {
            string code = plan == null || plan.ErrorCode.Length == 0 ?
                "migration.plan_required" : plan.ErrorCode;
            string message = plan == null || plan.ErrorMessage.Length == 0 ?
                "A successful migration plan is required." : plan.ErrorMessage;
            return FailResult(state.Result, code, message);
        }

        state.Result.Plan = plan;
        state.Result.SourceMessageIdentifier = plan.SourceMessageIdentifier == null ?
            request.SourceMessageIdentifier : plan.SourceMessageIdentifier;
        state.Result.TargetMessageIdentifier = plan.TargetMessageIdentifier == null ?
            request.TargetMessageIdentifier : plan.TargetMessageIdentifier;
        if (!ValidateExecutionConfiguration(state, plan))
        {
            return state.Result;
        }

        if (!MaterializeInput(state))
        {
            return state.Result;
        }

        int stepIndex = 0;
        while (stepIndex < plan.Steps.Count)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                FailResult(state.Result, "migration.cancelled", "Migration was cancelled.");
                MaterializeOutput(state);
                return state.Result;
            }

            MigrationStep step = plan.Steps[stepIndex];
            bool applied = ApplyStep(state, step);
            if (applied)
            {
                state.Result.AppliedSteps.Add(step);
                if (step.Lossy)
                {
                    state.Result.DataLoss = true;
                }
            }
            else
            {
                state.Result.FailedSteps.Add(step);
                AddStepDiagnostic(state, step);
                if (step.Required)
                {
                    state.RequiredStepFailed = true;
                }
            }

            stepIndex++;
        }

        bool fallbackAuthorized = request.AllowSupplementaryFallback &&
            plan.SupplementaryFallbackAuthorized;
        if (state.UnmappedNodes.Count > 0 && fallbackAuthorized)
        {
            ApplySupplementaryFallback(state);
        }

        ApplyTargetNamespace(state);
        if (!MaterializeOutput(state))
        {
            return state.Result;
        }

        if (state.RequiredStepFailed && !state.Result.SupplementaryFallbackUsed)
        {
            return FailResult(
                state.Result,
                "migration.required_step_failed",
                "At least one required migration step failed.");
        }

        if (!ValidateTargetOutput(state))
        {
            return state.Result;
        }

        state.Result.Success = true;
        return state.Result;
    }

    internal static bool ValidateExecutionConfiguration(
        MigrationExecutionState state,
        MigrationPlan plan)
    {
        MigrationRequest request = state.Request;
        if (request.Limits == null || request.Limits.MaxCollectionCount < 1 ||
            request.Limits.MaxElementCount < 1 || request.Limits.MaxTotalBytes < 1)
        {
            FailResult(
                state.Result,
                "migration.resource_limit_invalid",
                "Migration resource limits must be positive.");
            return false;
        }

        if (plan.Steps.Count > request.Limits.MaxCollectionCount)
        {
            FailResult(
                state.Result,
                "migration.step_limit",
                "The supplied migration plan exceeds the configured step limit.");
            return false;
        }

        if (state.Result.SourceMessageIdentifier == null ||
            MessageIdentifierLexical.FormatMessageIdentifier(
                state.Result.SourceMessageIdentifier) == null ||
            state.Result.TargetMessageIdentifier == null ||
            MessageIdentifierLexical.FormatMessageIdentifier(
                state.Result.TargetMessageIdentifier) == null)
        {
            FailResult(
                state.Result,
                "migration.message_identifier_invalid",
                "The migration plan requires valid exact source and target message identifiers.");
            return false;
        }

        int stepIndex = 0;
        while (stepIndex < plan.Steps.Count)
        {
            if (plan.Steps[stepIndex].Sequence != stepIndex)
            {
                FailResult(
                    state.Result,
                    "migration.step_sequence_invalid",
                    "Migration step sequences must be contiguous and deterministic.");
                return false;
            }

            stepIndex++;
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
            FailResult(
                state.Result,
                "migration.input_invalid",
                "Exactly one raw, generic, or model source representation is required.");
            return false;
        }

        return true;
    }

    internal static bool MaterializeInput(MigrationExecutionState state)
    {
        MigrationRequest request = state.Request;
        if (request.Xml.Length > 0)
        {
            if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
            {
                FailResult(
                    state.Result,
                    "migration.input_limit",
                    "The raw migration input exceeds the configured byte limit.");
                return false;
            }

            MessageReadOptions readOptions = new MessageReadOptions();
            readOptions.Limits = request.Limits;
            readOptions.NamespaceMode = NamespaceMode.Lenient;
            readOptions.UnknownContentMode = UnknownContentMode.Preserve;
            DynamicXmlNode parsed = new GenericXmlNodeReader(readOptions)
                .ReadDocumentFromBytes(request.Xml, null);
            state.Tree = CanonicalXmlNodeNormalizer.CloneNode(parsed);
            return true;
        }

        if (request.Document != null)
        {
            MessageWriteOptions writeOptions = CreateWriteOptions(request.Limits);
            DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
                request.Document,
                writeOptions);
            if (tree == null)
            {
                FailResult(
                    state.Result,
                    "migration.input_content_missing",
                    "The generic source document has no XML content.");
                return false;
            }

            state.Tree = CanonicalXmlNodeNormalizer.CloneNode(tree);
            return true;
        }

        DynamicXmlNode? modelNode = request.Model as DynamicXmlNode;
        if (modelNode != null)
        {
            state.Tree = CanonicalXmlNodeNormalizer.CloneNode(modelNode);
            if (state.Tree.Kind != XmlNodeKind.Document)
            {
                DynamicXmlNode document = new DynamicXmlNode();
                document.Kind = XmlNodeKind.Document;
                document.Children.Add(state.Tree);
                state.Tree = document;
            }

            state.InputWasModel = true;
            return true;
        }

        MessageDocument? modelDocument = request.Model as MessageDocument;
        if (modelDocument != null)
        {
            MessageWriteOptions writeOptions = CreateWriteOptions(request.Limits);
            DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
                modelDocument,
                writeOptions);
            if (tree != null)
            {
                state.Tree = CanonicalXmlNodeNormalizer.CloneNode(tree);
                state.InputWasModel = true;
                return true;
            }
        }

        FailResult(
            state.Result,
            "migration.model_unsupported",
            "The supplied model cannot be migrated without an explicit generic XML representation.");
        return false;
    }

    internal static bool ApplyStep(MigrationExecutionState state, MigrationStep step)
    {
        if (step.Action == MigrationActionKind.DefaultValue)
        {
            return ApplyDefaultStep(state, step);
        }

        List<MigrationNodeMatch> sources = FindStepSources(state.Tree, step);
        if (sources.Count == 0)
        {
            return !step.Required;
        }

        if (step.Action == MigrationActionKind.Drop)
        {
            RecordUnmappedMatches(state, sources);
            RemoveMatchedNodes(sources);
            state.Result.DataLoss = true;
            return true;
        }

        if (step.Action == MigrationActionKind.Custom || step.Action == MigrationActionKind.Unknown)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        if (step.Action == MigrationActionKind.Merge)
        {
            return ApplyMergeStep(state, step, sources);
        }

        if (step.TargetPaths.Count == 0)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        if (step.Action == MigrationActionKind.CodeMap)
        {
            return ApplyCodeMapStep(state, step, sources);
        }

        if (step.Action == MigrationActionKind.Convert)
        {
            return ApplyConvertStep(state, step, sources);
        }

        bool copied = CopySourcesToTargets(state, step, sources);
        if (!copied)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        if (step.Action == MigrationActionKind.Rename ||
            step.Action == MigrationActionKind.Move ||
            step.Action == MigrationActionKind.Split)
        {
            RemoveSourcesOutsideTargets(step, sources);
        }

        return step.Action == MigrationActionKind.Copy ||
            step.Action == MigrationActionKind.Rename ||
            step.Action == MigrationActionKind.Move ||
            step.Action == MigrationActionKind.Split;
    }

    internal static bool ApplyDefaultStep(MigrationExecutionState state, MigrationStep step)
    {
        if (step.TargetPaths.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < step.TargetPaths.Count)
        {
            DynamicXmlNode? target = EnsureElementPath(state.Tree, step.TargetPaths[index].Value);
            if (target == null)
            {
                return false;
            }

            SetElementText(target, step.DefaultValue);
            index++;
        }

        return true;
    }

    internal static bool ApplyMergeStep(
        MigrationExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        if (step.TargetPaths.Count != 1)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        string value = string.Empty;
        int index = 0;
        while (index < sources.Count)
        {
            value += ReadElementText(sources[index].Node);
            index++;
        }

        DynamicXmlNode? target = EnsureElementPath(state.Tree, step.TargetPaths[0].Value);
        if (target == null)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        SetElementText(target, value);
        RemoveSourcesOutsideTargets(step, sources);
        return true;
    }

    internal static bool ApplyCodeMapStep(
        MigrationExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        if (step.SourceCodeValues.Count != step.TargetCodeValues.Count)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            string sourceValue = ReadElementText(sources[sourceIndex].Node);
            int mapIndex = 0;
            string targetValue = string.Empty;
            bool mapped = false;
            while (mapIndex < step.SourceCodeValues.Count)
            {
                if (step.SourceCodeValues[mapIndex] == sourceValue)
                {
                    targetValue = step.TargetCodeValues[mapIndex];
                    mapped = true;
                    break;
                }

                mapIndex++;
            }

            if (!mapped)
            {
                RecordUnmappedMatches(state, sources);
                return false;
            }

            DynamicXmlNode converted = CanonicalXmlNodeNormalizer.CloneNode(sources[sourceIndex].Node);
            SetElementText(converted, targetValue);
            if (!PlaceConvertedNode(state.Tree, step.TargetPaths, converted))
            {
                RecordUnmappedMatches(state, sources);
                return false;
            }

            sourceIndex++;
        }

        RemoveSourcesOutsideTargets(step, sources);
        return true;
    }

    internal static bool ApplyConvertStep(
        MigrationExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        string converter = step.ConverterIdentifier;
        bool identity = converter.Length == 0 || converter == "identity" ||
            converter == "iso20022.convert.type" ||
            converter == "iso20022.convert.cardinality" ||
            converter == "iso20022.convert.facet" || converter == "decimal-to-text";
        bool textConverter = converter == "trim" || converter == "uppercase" ||
            converter == "lowercase";
        if (!identity && !textConverter)
        {
            RecordUnmappedMatches(state, sources);
            return false;
        }

        int index = 0;
        while (index < sources.Count)
        {
            DynamicXmlNode converted = CanonicalXmlNodeNormalizer.CloneNode(sources[index].Node);
            string value = ReadElementText(converted);
            if (converter == "trim")
            {
                value = value.Trim();
            }
            else if (converter == "uppercase")
            {
                value = value.ToUpperInvariant();
            }
            else if (converter == "lowercase")
            {
                value = value.ToLowerInvariant();
            }

            if (textConverter)
            {
                SetElementText(converted, value);
            }

            if (!PlaceConvertedNode(state.Tree, step.TargetPaths, converted))
            {
                RecordUnmappedMatches(state, sources);
                return false;
            }

            index++;
        }

        RemoveSourcesOutsideTargets(step, sources);
        return true;
    }

    internal static bool CopySourcesToTargets(
        MigrationExecutionState state,
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            if (!PlaceConvertedNode(state.Tree, step.TargetPaths, sources[sourceIndex].Node))
            {
                return false;
            }

            sourceIndex++;
        }

        return true;
    }

    internal static bool PlaceConvertedNode(
        DynamicXmlNode tree,
        List<XmlPathData> targetPaths,
        DynamicXmlNode source)
    {
        int targetIndex = 0;
        while (targetIndex < targetPaths.Count)
        {
            DynamicXmlNode? target = EnsureElementPath(tree, targetPaths[targetIndex].Value);
            if (target == null)
            {
                return false;
            }

            DynamicXmlNode replacement = CanonicalXmlNodeNormalizer.CloneNode(source);
            replacement.Name.LocalName = target.Name.LocalName;
            replacement.Name.NamespaceUri = target.Name.NamespaceUri;
            replacement.Name.Prefix = target.Name.Prefix;
            target.Attributes = replacement.Attributes;
            target.NamespaceDeclarations = replacement.NamespaceDeclarations;
            target.Children = replacement.Children;
            target.IsNil = replacement.IsNil;
            target.IsEmptyElement = replacement.IsEmptyElement;
            targetIndex++;
        }

        return true;
    }

    internal static List<MigrationNodeMatch> FindStepSources(
        DynamicXmlNode tree,
        MigrationStep step)
    {
        List<MigrationNodeMatch> result = new List<MigrationNodeMatch>();
        int pathIndex = 0;
        while (pathIndex < step.SourcePaths.Count)
        {
            CollectPathMatches(tree, null, -1, string.Empty, step.SourcePaths[pathIndex].Value, result);
            pathIndex++;
        }

        return result;
    }

    internal static void CollectPathMatches(
        DynamicXmlNode node,
        DynamicXmlNode? parent,
        int index,
        string parentPath,
        string requestedPath,
        List<MigrationNodeMatch> matches)
    {
        string path = parentPath;
        if (node.Kind == XmlNodeKind.Element)
        {
            path += "/" + node.Name.LocalName;
            if (path == NormalizePath(requestedPath))
            {
                MigrationNodeMatch match = new MigrationNodeMatch();
                match.Node = node;
                match.Parent = parent;
                match.Index = index;
                match.Path = path;
                matches.Add(match);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            CollectPathMatches(node.Children[childIndex], node, childIndex, path, requestedPath, matches);
            childIndex++;
        }
    }

    internal static string NormalizePath(string path)
    {
        string normalized = path.Trim();
        while (normalized.Length > 1 && normalized.EndsWith("/", System.StringComparison.Ordinal))
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }

        if (normalized.Length > 0 && normalized[0] != '/')
        {
            normalized = "/" + normalized;
        }

        return normalized;
    }

    internal static DynamicXmlNode? EnsureElementPath(DynamicXmlNode tree, string path)
    {
        string[] rawSegments = NormalizePath(path).Split('/');
        List<string> segments = new List<string>();
        int rawIndex = 0;
        while (rawIndex < rawSegments.Length)
        {
            if (rawSegments[rawIndex].Length > 0)
            {
                segments.Add(rawSegments[rawIndex]);
            }

            rawIndex++;
        }

        if (segments.Count == 0)
        {
            return null;
        }

        DynamicXmlNode? current = FindFirstElement(tree);
        if (current == null)
        {
            current = CreateElementNode(segments[0], string.Empty, string.Empty);
            tree.Children.Add(current);
        }

        int segmentIndex = 0;
        if (current.Name.LocalName == segments[0])
        {
            segmentIndex = 1;
        }

        while (segmentIndex < segments.Count)
        {
            DynamicXmlNode? child = FindDirectChild(current, segments[segmentIndex]);
            if (child == null)
            {
                child = CreateElementNode(
                    segments[segmentIndex],
                    current.Name.NamespaceUri,
                    current.Name.Prefix);
                current.Children.Add(child);
            }

            current = child;
            segmentIndex++;
        }

        return current;
    }

    internal static DynamicXmlNode? FindFirstElement(DynamicXmlNode node)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            if (node.Children[index].Kind == XmlNodeKind.Element)
            {
                return node.Children[index];
            }

            index++;
        }

        return null;
    }

    internal static DynamicXmlNode? FindDirectChild(DynamicXmlNode node, string localName)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode child = node.Children[index];
            if (child.Kind == XmlNodeKind.Element && child.Name.LocalName == localName)
            {
                return child;
            }

            index++;
        }

        return null;
    }

    internal static DynamicXmlNode CreateElementNode(
        string localName,
        string namespaceUri,
        string prefix)
    {
        DynamicXmlNode node = new DynamicXmlNode();
        node.Kind = XmlNodeKind.Element;
        node.Name.LocalName = localName;
        node.Name.NamespaceUri = namespaceUri;
        node.Name.Prefix = prefix;
        return node;
    }

    internal static string ReadElementText(DynamicXmlNode node)
    {
        string value = string.Empty;
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode child = node.Children[index];
            if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                value += child.Value;
            }

            index++;
        }

        return value;
    }

    internal static void SetElementText(DynamicXmlNode node, string value)
    {
        node.Children.Clear();
        DynamicXmlNode text = new DynamicXmlNode();
        text.Kind = XmlNodeKind.Text;
        text.Value = value;
        node.Children.Add(text);
        node.IsEmptyElement = false;
    }

    internal static void RemoveSourcesOutsideTargets(
        MigrationStep step,
        List<MigrationNodeMatch> sources)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            bool retained = false;
            int targetIndex = 0;
            while (targetIndex < step.TargetPaths.Count)
            {
                if (sources[sourceIndex].Path == NormalizePath(step.TargetPaths[targetIndex].Value))
                {
                    retained = true;
                }

                targetIndex++;
            }

            DynamicXmlNode? parent = sources[sourceIndex].Parent;
            if (!retained && parent != null)
            {
                parent.Children.Remove(sources[sourceIndex].Node);
            }

            sourceIndex++;
        }
    }

    internal static void RemoveMatchedNodes(List<MigrationNodeMatch> matches)
    {
        int index = matches.Count - 1;
        while (index >= 0)
        {
            DynamicXmlNode? parent = matches[index].Parent;
            if (parent != null)
            {
                parent.Children.Remove(matches[index].Node);
            }

            index--;
        }
    }

    internal static void RecordUnmappedMatches(MigrationExecutionState state, List<MigrationNodeMatch> matches)
    {
        int index = 0;
        while (index < matches.Count)
        {
            MigrationNodeMatch match = matches[index];
            bool knownPath = false;
            int pathIndex = 0;
            while (pathIndex < state.Result.UnmappedSourcePaths.Count)
            {
                if (state.Result.UnmappedSourcePaths[pathIndex].Value == match.Path)
                {
                    knownPath = true;
                }

                pathIndex++;
            }

            if (!knownPath)
            {
                XmlPathData path = new XmlPathData();
                path.Value = match.Path;
                state.Result.UnmappedSourcePaths.Add(path);
            }

            DynamicXmlNode copy = CanonicalXmlNodeNormalizer.CloneNode(match.Node);
            state.UnmappedNodes.Add(copy);
            RawXmlFragment fragment = new RawXmlFragment();
            MessageWriteOptions options = CreateWriteOptions(state.Request.Limits);
            options.WriteXmlDeclaration = false;
            fragment.Xml = new GenericXmlNodeWriter(options).WriteText(copy);
            fragment.Utf8Bytes = System.Text.Encoding.UTF8.GetBytes(fragment.Xml);
            fragment.RootName = CopyQualifiedName(copy.Name);
            fragment.NamespaceDeclarations = GenericXmlNodeWriter.CopyDeclarations(copy.NamespaceDeclarations);
            fragment.Location = copy.Location;
            state.Result.UnmappedContent.Add(fragment);
            index++;
        }
    }

    internal static QualifiedNameData CopyQualifiedName(QualifiedNameData source)
    {
        QualifiedNameData copy = new QualifiedNameData();
        copy.LocalName = source.LocalName;
        copy.NamespaceUri = source.NamespaceUri;
        copy.Prefix = source.Prefix;
        return copy;
    }

    internal static void ApplySupplementaryFallback(MigrationExecutionState state)
    {
        DynamicXmlNode? root = FindFirstElement(state.Tree);
        if (root == null)
        {
            return;
        }

        DynamicXmlNode parent = root;
        DynamicXmlNode? body = FindDirectChild(root, "Document");
        if (body != null)
        {
            parent = body;
        }

        DynamicXmlNode supplementary = CreateElementNode("SplmtryData", parent.Name.NamespaceUri, parent.Name.Prefix);
        DynamicXmlNode envelope = CreateElementNode("Envlp", parent.Name.NamespaceUri, parent.Name.Prefix);
        int index = 0;
        while (index < state.UnmappedNodes.Count)
        {
            envelope.Children.Add(CanonicalXmlNodeNormalizer.CloneNode(state.UnmappedNodes[index]));
            index++;
        }

        supplementary.Children.Add(envelope);
        parent.Children.Add(supplementary);
        state.Result.SupplementaryFallbackUsed = true;
    }

    internal static void ApplyTargetNamespace(MigrationExecutionState state)
    {
        MessageIdentifier? targetIdentifier = state.Result.TargetMessageIdentifier;
        if (targetIdentifier == null)
        {
            return;
        }

        string? formattedValue = MessageIdentifierLexical.FormatMessageIdentifier(targetIdentifier);
        if (formattedValue == null)
        {
            return;
        }

        string formatted = formattedValue;
        string targetNamespace = "urn:iso:std:iso:20022:tech:xsd:" + formatted;
        DynamicXmlNode? root = FindFirstElement(state.Tree);
        if (root == null || root.Name.NamespaceUri.Length == 0)
        {
            return;
        }

        ReplaceNamespace(state.Tree, root.Name.NamespaceUri, targetNamespace);
    }

    internal static void ReplaceNamespace(DynamicXmlNode node, string sourceNamespace, string targetNamespace)
    {
        if (node.Name.NamespaceUri == sourceNamespace)
        {
            node.Name.NamespaceUri = targetNamespace;
        }

        int declarationIndex = 0;
        while (declarationIndex < node.NamespaceDeclarations.Count)
        {
            if (node.NamespaceDeclarations[declarationIndex].NamespaceUri == sourceNamespace)
            {
                node.NamespaceDeclarations[declarationIndex].NamespaceUri = targetNamespace;
            }

            declarationIndex++;
        }

        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            if (node.Attributes[attributeIndex].Name.NamespaceUri == sourceNamespace)
            {
                node.Attributes[attributeIndex].Name.NamespaceUri = targetNamespace;
            }

            attributeIndex++;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            ReplaceNamespace(node.Children[childIndex], sourceNamespace, targetNamespace);
            childIndex++;
        }
    }

    internal static bool MaterializeOutput(MigrationExecutionState state)
    {
        MessageWriteOptions writeOptions = CreateWriteOptions(state.Request.Limits);
        byte[] bytes = new GenericXmlNodeWriter(writeOptions).WriteBytes(state.Tree);
        if (bytes.LongLength > state.Request.Limits.MaxTotalBytes)
        {
            FailResult(state.Result, "migration.output_limit", "The migrated XML exceeds the configured byte limit.");
            return false;
        }

        state.Result.Xml = bytes;
        MessageReadOptions readOptions = new MessageReadOptions();
        readOptions.Limits = state.Request.Limits;
        readOptions.NamespaceMode = NamespaceMode.Lenient;
        readOptions.UnknownContentMode = UnknownContentMode.Preserve;
        MessageReadResult readResult = XmlMessageReader.BuildGenericResult(state.Tree, bytes.LongLength, readOptions);
        if (!readResult.Success || readResult.Document == null)
        {
            FailResult(state.Result, "migration.output_invalid", "The migrated generic output could not be materialized.");
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

        if (state.InputWasModel)
        {
            state.Result.Model = state.Tree;
        }

        return true;
    }

    internal static bool ValidateTargetOutput(MigrationExecutionState state)
    {
        if (!state.Request.ValidateTarget)
        {
            return true;
        }

        if (state.Request.Validator == null)
        {
            FailResult(state.Result, "migration.validator_required", "Target validation was requested but no validator was supplied.");
            return false;
        }

        ValidationRequest request = new ValidationRequest();
        request.Document = state.Result.Document;
        request.ExpectedIdentifier = state.Result.TargetMessageIdentifier;
        request.Policy = state.Request.TargetValidationPolicy;
        request.Context = state.Request.ValidationContext;
        ValidationReport report = state.Request.Validator.ValidateMessage(request);
        state.Result.TargetValidation = report;
        CopyValidationDiagnostics(state, report);
        if (!report.Success)
        {
            FailResult(state.Result, "migration.target_validation_failed", "Target validation could not complete.");
            return false;
        }

        if (!report.Valid)
        {
            FailResult(state.Result, "migration.target_invalid", "The migrated output did not pass target validation.");
            return false;
        }

        return true;
    }

    internal static void CopyValidationDiagnostics(MigrationExecutionState state, ValidationReport report)
    {
        int index = 0;
        while (index < report.Diagnostics.Count && state.Result.Diagnostics.Count < state.Request.Limits.MaxDiagnosticCount)
        {
            state.Result.Diagnostics.Add(report.Diagnostics[index]);
            index++;
        }
    }

    internal static void AddStepDiagnostic(MigrationExecutionState state, MigrationStep step)
    {
        if (state.Result.Diagnostics.Count >= state.Request.Limits.MaxDiagnosticCount)
        {
            return;
        }

        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = "migration.step_failed";
        diagnostic.RuleIdentifier = step.ConverterIdentifier;
        diagnostic.Severity = step.Required ? ValidationSeverity.Error : ValidationSeverity.Warning;
        diagnostic.Stage = ValidationStage.Structural;
        diagnostic.Kind = DiagnosticKind.UnsupportedConstraint;
        diagnostic.Message = "Migration step " + step.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) + " could not be applied.";
        if (step.SourcePaths.Count > 0)
        {
            diagnostic.Path = step.SourcePaths[0];
        }

        state.Result.Diagnostics.Add(diagnostic);
    }

    internal static MessageWriteOptions CreateWriteOptions(ResourceLimits limits)
    {
        MessageWriteOptions options = new MessageWriteOptions();
        options.Limits = limits;
        options.FormattingMode = XmlFormattingMode.Compact;
        options.UnknownContentMode = UnknownContentMode.Preserve;
        options.WriteXmlDeclaration = true;
        options.PreservePrefixes = true;
        return options;
    }

    internal static MigrationResult FailResult(MigrationResult result, string code, string message)
    {
        result.Success = false;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
