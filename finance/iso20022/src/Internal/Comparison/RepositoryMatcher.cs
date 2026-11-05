using System;
using System.Collections.Generic;

namespace iso20022;

internal static class RepositoryMatcher
{
    internal static RepositoryMatchResult Match(RepositoryComparisonRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        RepositoryMatchResult result = new RepositoryMatchResult();
        if (request.Source == null || request.Target == null)
        {
            return Fail(
                result,
                "comparison.repository_required",
                "Both source and target repository snapshots are required.");
        }

        if (request.MinimumMatchConfidence < 0m || request.MinimumMatchConfidence > 1m)
        {
            return Fail(
                result,
                "comparison.confidence_invalid",
                "The minimum match confidence must be from zero through one.");
        }

        if (request.CancellationToken.IsCancellationRequested)
        {
            return Fail(result, "comparison.cancelled", "Repository matching was cancelled.");
        }

        RepositorySnapshotIndex sourceIndex = RepositorySnapshotIndexer.BuildIndex(request.Source);
        RepositorySnapshotIndex targetIndex = RepositorySnapshotIndexer.BuildIndex(request.Target);
        if (!WithinLimits(request, sourceIndex, targetIndex))
        {
            return Fail(
                result,
                "comparison.resource_limit",
                "A repository entity count exceeds the configured comparison limits.");
        }

        List<RepositoryMatchCandidate> sourceCandidates = BuildCandidates(request.Source, sourceIndex);
        List<RepositoryMatchCandidate> targetCandidates = BuildCandidates(request.Target, targetIndex);
        Dictionary<RepositoryIndexEntry, RepositoryEntityMatch> decisions =
            new Dictionary<RepositoryIndexEntry, RepositoryEntityMatch>();
        HashSet<RepositoryIndexEntry> claimedTargets = new HashSet<RepositoryIndexEntry>();
        HashSet<RepositoryIndexEntry> reservedOverrideTargets = new HashSet<RepositoryIndexEntry>();
        Dictionary<RepositoryIndexEntry, RepositoryIndexEntry> overrides =
            new Dictionary<RepositoryIndexEntry, RepositoryIndexEntry>();

        if (!CollectOverrides(
            request,
            sourceIndex,
            targetIndex,
            overrides,
            reservedOverrideTargets,
            result))
        {
            return result;
        }

        ApplyOverrides(sourceCandidates, overrides, decisions, claimedTargets);
        ApplyExactIdentityMatches(
            request,
            sourceCandidates,
            targetCandidates,
            decisions,
            claimedTargets,
            reservedOverrideTargets,
            result);
        if (!result.Success && result.ErrorCode.Length > 0)
        {
            return result;
        }

        ApplySemanticMatches(
            request,
            sourceCandidates,
            targetCandidates,
            decisions,
            claimedTargets,
            reservedOverrideTargets,
            result);
        if (!result.Success && result.ErrorCode.Length > 0)
        {
            return result;
        }

        AssembleResult(sourceCandidates, targetCandidates, decisions, claimedTargets, result);
        result.Success = true;
        return result;
    }

    internal static bool WithinLimits(
        RepositoryComparisonRequest request,
        RepositorySnapshotIndex sourceIndex,
        RepositorySnapshotIndex targetIndex)
    {
        if (request.Limits == null ||
            request.Limits.MaxElementCount < 1 ||
            request.Limits.MaxCollectionCount < 1)
        {
            return false;
        }

        if (sourceIndex.Entries.Count > request.Limits.MaxElementCount ||
            targetIndex.Entries.Count > request.Limits.MaxElementCount ||
            sourceIndex.Entries.Count > request.Limits.MaxCollectionCount ||
            targetIndex.Entries.Count > request.Limits.MaxCollectionCount)
        {
            return false;
        }

        return true;
    }

    internal static List<RepositoryMatchCandidate> BuildCandidates(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index)
    {
        List<RepositoryMatchCandidate> candidates = new List<RepositoryMatchCandidate>();
        int entryIndex = 0;
        while (entryIndex < index.Entries.Count)
        {
            RepositoryIndexEntry entry = index.Entries[entryIndex];
            RepositoryMatchCandidate candidate = new RepositoryMatchCandidate();
            candidate.Entry = entry;
            candidate.NameKey = NormalizeKey(entry.Name);
            candidate.QualifierKey = ResolveQualifierKey(entry, index);
            candidate.SemanticKey = ResolveSemanticKey(entry);
            candidates.Add(candidate);
            entryIndex++;
        }

        return candidates;
    }

    internal static bool CollectOverrides(
        RepositoryComparisonRequest request,
        RepositorySnapshotIndex sourceIndex,
        RepositorySnapshotIndex targetIndex,
        Dictionary<RepositoryIndexEntry, RepositoryIndexEntry> overrides,
        HashSet<RepositoryIndexEntry> reservedTargets,
        RepositoryMatchResult result)
    {
        if (request.MatchOverrides != null)
        {
            int ruleIndex = 0;
            while (ruleIndex < request.MatchOverrides.Rules.Count)
            {
                if (!CollectOverrideRule(
                    request.MatchOverrides.Rules[ruleIndex],
                    sourceIndex,
                    targetIndex,
                    overrides,
                    reservedTargets,
                    result))
                {
                    return false;
                }

                ruleIndex++;
            }
        }

        int directIndex = 0;
        while (directIndex < request.MatchOverrideRules.Count)
        {
            if (!CollectOverrideRule(
                request.MatchOverrideRules[directIndex],
                sourceIndex,
                targetIndex,
                overrides,
                reservedTargets,
                result))
            {
                return false;
            }

            directIndex++;
        }

        return true;
    }

    internal static bool CollectOverrideRule(
        MappingRuleDefinition rule,
        RepositorySnapshotIndex sourceIndex,
        RepositorySnapshotIndex targetIndex,
        Dictionary<RepositoryIndexEntry, RepositoryIndexEntry> overrides,
        HashSet<RepositoryIndexEntry> reservedTargets,
        RepositoryMatchResult result)
    {
        if (rule.SourceIdentifier.Length == 0 && rule.TargetIdentifier.Length == 0)
        {
            return true;
        }

        if (rule.SourceIdentifier.Length == 0 || rule.TargetIdentifier.Length == 0)
        {
            Fail(
                result,
                "comparison.override_invalid",
                "A repository match override must specify both source and target identifiers.");
            return false;
        }

        List<RepositoryIndexEntry> sources = FindOverrideEntries(
            sourceIndex,
            rule.SourceIdentifier,
            rule.EntityKind);
        List<RepositoryIndexEntry> targets = FindOverrideEntries(
            targetIndex,
            rule.TargetIdentifier,
            rule.EntityKind);
        if (sources.Count != 1 || targets.Count != 1)
        {
            Fail(
                result,
                "comparison.override_unresolved",
                "A repository match override identifier was missing or ambiguous.");
            return false;
        }

        RepositoryIndexEntry source = sources[0];
        RepositoryIndexEntry target = targets[0];
        if (source.Kind != target.Kind)
        {
            Fail(
                result,
                "comparison.override_kind_mismatch",
                "A repository match override must map entities of the same kind.");
            return false;
        }

        if (overrides.ContainsKey(source))
        {
            if (overrides[source] == target)
            {
                return true;
            }

            Fail(
                result,
                "comparison.override_conflict",
                "More than one target was supplied for a source override.");
            return false;
        }

        if (reservedTargets.Contains(target))
        {
            Fail(
                result,
                "comparison.override_conflict",
                "More than one source was supplied for a target override.");
            return false;
        }

        overrides.Add(source, target);
        reservedTargets.Add(target);
        return true;
    }

    internal static List<RepositoryIndexEntry> FindOverrideEntries(
        RepositorySnapshotIndex index,
        string identifier,
        RepositoryEntityKind kind)
    {
        List<RepositoryIndexEntry> matches = new List<RepositoryIndexEntry>();
        List<RepositoryIndexEntry> entries = index.FindIdentifier(identifier);
        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            RepositoryIndexEntry entry = entries[entryIndex];
            if (kind == RepositoryEntityKind.Unknown || entry.Kind == kind)
            {
                matches.Add(entry);
            }

            entryIndex++;
        }

        return matches;
    }

    internal static void ApplyOverrides(
        List<RepositoryMatchCandidate> sources,
        Dictionary<RepositoryIndexEntry, RepositoryIndexEntry> overrides,
        Dictionary<RepositoryIndexEntry, RepositoryEntityMatch> decisions,
        HashSet<RepositoryIndexEntry> claimedTargets)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            RepositoryIndexEntry source = sources[sourceIndex].Entry;
            if (overrides.ContainsKey(source))
            {
                RepositoryEntityMatch match = new RepositoryEntityMatch();
                match.Source = source;
                match.Target = overrides[source];
                match.Confidence = 1m;
                match.OverrideMatch = true;
                match.Rationale = "The caller explicitly mapped the source XMI identity to the target XMI identity.";
                decisions.Add(source, match);
                claimedTargets.Add(overrides[source]);
            }

            sourceIndex++;
        }
    }

    internal static void ApplyExactIdentityMatches(
        RepositoryComparisonRequest request,
        List<RepositoryMatchCandidate> sources,
        List<RepositoryMatchCandidate> targets,
        Dictionary<RepositoryIndexEntry, RepositoryEntityMatch> decisions,
        HashSet<RepositoryIndexEntry> claimedTargets,
        HashSet<RepositoryIndexEntry> reservedOverrideTargets,
        RepositoryMatchResult result)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                Fail(result, "comparison.cancelled", "Repository matching was cancelled.");
                return;
            }

            RepositoryMatchCandidate source = sources[sourceIndex];
            if (!decisions.ContainsKey(source.Entry) && source.Entry.Identifier.Length > 0)
            {
                List<RepositoryIndexEntry> exactTargets = new List<RepositoryIndexEntry>();
                int targetIndex = 0;
                while (targetIndex < targets.Count)
                {
                    RepositoryIndexEntry target = targets[targetIndex].Entry;
                    if (target.Kind == source.Entry.Kind &&
                        target.Identifier == source.Entry.Identifier &&
                        !claimedTargets.Contains(target) &&
                        !reservedOverrideTargets.Contains(target))
                    {
                        exactTargets.Add(target);
                    }

                    targetIndex++;
                }

                if (exactTargets.Count == 1)
                {
                    RepositoryEntityMatch match = new RepositoryEntityMatch();
                    match.Source = source.Entry;
                    match.Target = exactTargets[0];
                    match.Confidence = 1m;
                    match.ExactIdentifierMatch = true;
                    match.Rationale = "The source and target have the same XMI identity and entity kind.";
                    decisions.Add(source.Entry, match);
                    claimedTargets.Add(exactTargets[0]);
                }

                if (exactTargets.Count > 1)
                {
                    RepositoryEntityMatch ambiguity = CreateAmbiguity(
                        source.Entry,
                        exactTargets,
                        1m,
                        "The XMI identity resolves to more than one target entity of the same kind.");
                    decisions.Add(source.Entry, ambiguity);
                }
            }

            sourceIndex++;
        }
    }

    internal static void ApplySemanticMatches(
        RepositoryComparisonRequest request,
        List<RepositoryMatchCandidate> sources,
        List<RepositoryMatchCandidate> targets,
        Dictionary<RepositoryIndexEntry, RepositoryEntityMatch> decisions,
        HashSet<RepositoryIndexEntry> claimedTargets,
        HashSet<RepositoryIndexEntry> reservedOverrideTargets,
        RepositoryMatchResult result)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                Fail(result, "comparison.cancelled", "Repository matching was cancelled.");
                return;
            }

            RepositoryMatchCandidate source = sources[sourceIndex];
            if (!decisions.ContainsKey(source.Entry) &&
                source.Entry.Kind != RepositoryEntityKind.Unknown)
            {
                decimal bestConfidence = 0m;
                List<RepositoryIndexEntry> bestTargets = new List<RepositoryIndexEntry>();
                int targetIndex = 0;
                while (targetIndex < targets.Count)
                {
                    RepositoryMatchCandidate target = targets[targetIndex];
                    if (target.Entry.Kind == source.Entry.Kind &&
                        !claimedTargets.Contains(target.Entry) &&
                        !reservedOverrideTargets.Contains(target.Entry))
                    {
                        decimal confidence = ScoreSemanticCandidate(request, source, target);
                        if (confidence >= request.MinimumMatchConfidence &&
                            confidence > bestConfidence)
                        {
                            bestConfidence = confidence;
                            bestTargets.Clear();
                            bestTargets.Add(target.Entry);
                        }
                        else if (confidence >= request.MinimumMatchConfidence &&
                            confidence == bestConfidence &&
                            confidence > 0m)
                        {
                            bestTargets.Add(target.Entry);
                        }
                    }

                    targetIndex++;
                }

                if (bestTargets.Count == 1)
                {
                    RepositoryEntityMatch match = new RepositoryEntityMatch();
                    match.Source = source.Entry;
                    match.Target = bestTargets[0];
                    match.Confidence = bestConfidence;
                    match.SemanticFallbackMatch = true;
                    match.Rationale = SemanticRationale(bestConfidence);
                    decisions.Add(source.Entry, match);
                    claimedTargets.Add(bestTargets[0]);
                }

                if (bestTargets.Count > 1)
                {
                    RepositoryEntityMatch ambiguity = CreateAmbiguity(
                        source.Entry,
                        bestTargets,
                        bestConfidence,
                        "More than one target has the same best qualified semantic confidence.");
                    ambiguity.SemanticFallbackMatch = true;
                    decisions.Add(source.Entry, ambiguity);
                }
            }

            sourceIndex++;
        }
    }

    internal static decimal ScoreSemanticCandidate(
        RepositoryComparisonRequest request,
        RepositoryMatchCandidate source,
        RepositoryMatchCandidate target)
    {
        bool sameName = source.NameKey.Length > 0 && source.NameKey == target.NameKey;
        bool sameSemantic = source.SemanticKey.Length > 0 &&
            source.SemanticKey == target.SemanticKey;
        bool sameQualifier = source.QualifierKey == target.QualifierKey;

        if (sameSemantic && sameQualifier &&
            (source.Entry.Kind == RepositoryEntityKind.Repository ||
                source.Entry.Kind == RepositoryEntityKind.DataDictionary ||
                source.Entry.Kind == RepositoryEntityKind.BusinessProcessCatalogue))
        {
            return 0.95m;
        }

        if (sameName && sameQualifier)
        {
            return 0.95m;
        }

        if (sameSemantic && sameQualifier && request.DetectRenames)
        {
            return 0.92m;
        }

        if (sameName && !sameQualifier && request.DetectMoves)
        {
            return 0.84m;
        }

        if (sameSemantic && !sameQualifier && request.DetectMoves && request.DetectRenames)
        {
            return 0.82m;
        }

        return 0m;
    }

    internal static RepositoryEntityMatch CreateAmbiguity(
        RepositoryIndexEntry source,
        List<RepositoryIndexEntry> targets,
        decimal confidence,
        string rationale)
    {
        RepositoryEntityMatch match = new RepositoryEntityMatch();
        match.Source = source;
        match.Confidence = confidence;
        match.Ambiguous = true;
        match.Rationale = rationale;
        SortAmbiguityTargets(targets);
        int targetIndex = 0;
        while (targetIndex < targets.Count)
        {
            match.AlternativeTargets.Add(targets[targetIndex]);
            targetIndex++;
        }

        return match;
    }

    internal static void SortAmbiguityTargets(List<RepositoryIndexEntry> targets)
    {
        int index = 1;
        while (index < targets.Count)
        {
            RepositoryIndexEntry value = targets[index];
            int insertionIndex = index;
            while (insertionIndex > 0 &&
                CompareAmbiguityTargets(value, targets[insertionIndex - 1]) < 0)
            {
                targets[insertionIndex] = targets[insertionIndex - 1];
                insertionIndex--;
            }

            targets[insertionIndex] = value;
            index++;
        }
    }

    internal static int CompareAmbiguityTargets(
        RepositoryIndexEntry left,
        RepositoryIndexEntry right)
    {
        int comparison = string.CompareOrdinal(left.Identifier, right.Identifier);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = ((int)left.Kind).CompareTo((int)right.Kind);
        if (comparison != 0)
        {
            return comparison;
        }

        return string.CompareOrdinal(left.Name, right.Name);
    }

    internal static string SemanticRationale(decimal confidence)
    {
        if (confidence == 0.95m)
        {
            return "The normalized name and qualified owner context match.";
        }

        if (confidence == 0.92m)
        {
            return "A stable semantic identity and qualified owner context match despite a name change.";
        }

        if (confidence == 0.84m)
        {
            return "The normalized name matches uniquely across a changed owner context.";
        }

        return "A stable semantic identity matches uniquely across changed name and owner context.";
    }

    internal static void AssembleResult(
        List<RepositoryMatchCandidate> sources,
        List<RepositoryMatchCandidate> targets,
        Dictionary<RepositoryIndexEntry, RepositoryEntityMatch> decisions,
        HashSet<RepositoryIndexEntry> claimedTargets,
        RepositoryMatchResult result)
    {
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            RepositoryIndexEntry source = sources[sourceIndex].Entry;
            if (decisions.ContainsKey(source))
            {
                result.Matches.Add(decisions[source]);
            }
            else
            {
                result.UnmatchedSources.Add(source);
            }

            sourceIndex++;
        }

        int targetIndex = 0;
        while (targetIndex < targets.Count)
        {
            RepositoryIndexEntry target = targets[targetIndex].Entry;
            if (!claimedTargets.Contains(target))
            {
                result.UnmatchedTargets.Add(target);
            }

            targetIndex++;
        }
    }

    internal static string ResolveQualifierKey(
        RepositoryIndexEntry entry,
        RepositorySnapshotIndex index)
    {
        RepositoryReference? owner = ResolveOwnerReference(entry);
        if (owner == null || owner.TargetIdentifier.Length == 0)
        {
            if (entry.Kind == RepositoryEntityKind.Repository)
            {
                return string.Empty;
            }

            return "ROOT";
        }

        List<RepositoryIndexEntry> owners = index.FindIdentifier(owner.TargetIdentifier);
        if (owners.Count == 1)
        {
            string ownerName = NormalizeKey(owners[0].Name);
            if (ownerName.Length > 0)
            {
                return ((int)owners[0].Kind).ToString() + ":" + ownerName;
            }

            return ((int)owners[0].Kind).ToString();
        }

        return ((int)owner.TargetKind).ToString() + ":" + NormalizeKey(owner.TargetIdentifier);
    }

    internal static RepositoryReference? ResolveOwnerReference(RepositoryIndexEntry entry)
    {
        BusinessAreaDefinition? businessArea = entry.Value as BusinessAreaDefinition;
        if (businessArea != null)
        {
            return businessArea.BusinessProcessCatalogue;
        }

        BusinessProcessDefinition? businessProcess = entry.Value as BusinessProcessDefinition;
        if (businessProcess != null)
        {
            return businessProcess.BusinessProcessCatalogue;
        }

        BusinessRoleDefinition? businessRole = entry.Value as BusinessRoleDefinition;
        if (businessRole != null)
        {
            return businessRole.BusinessProcess;
        }

        ParticipantDefinition? participant = entry.Value as ParticipantDefinition;
        if (participant != null)
        {
            return participant.BusinessTransaction;
        }

        BusinessTransactionDefinition? transaction = entry.Value as BusinessTransactionDefinition;
        if (transaction != null)
        {
            return transaction.BusinessProcess;
        }

        MessageChoreographyDefinition? choreography = entry.Value as MessageChoreographyDefinition;
        if (choreography != null)
        {
            return choreography.BusinessTransaction;
        }

        MessageTransmissionDefinition? transmission = entry.Value as MessageTransmissionDefinition;
        if (transmission != null)
        {
            return transmission.BusinessTransaction;
        }

        MessagingEndpointDefinition? endpoint = entry.Value as MessagingEndpointDefinition;
        if (endpoint != null)
        {
            return endpoint.TransportSystem;
        }

        TransportDefinition? transport = entry.Value as TransportDefinition;
        if (transport != null)
        {
            return transport.BusinessProcessCatalogue;
        }

        BusinessComponentDefinition? businessComponent = entry.Value as BusinessComponentDefinition;
        if (businessComponent != null)
        {
            return businessComponent.DataDictionary;
        }

        BusinessElementDefinition? businessElement = entry.Value as BusinessElementDefinition;
        if (businessElement != null)
        {
            return businessElement.ElementContext;
        }

        MessageSetDefinition? messageSet = entry.Value as MessageSetDefinition;
        if (messageSet != null)
        {
            return messageSet.BusinessProcessCatalogue;
        }

        MessageDefinitionMetadata? messageDefinition = entry.Value as MessageDefinitionMetadata;
        if (messageDefinition != null)
        {
            return messageDefinition.BusinessArea;
        }

        MessageBuildingBlockDefinition? buildingBlock = entry.Value as MessageBuildingBlockDefinition;
        if (buildingBlock != null)
        {
            return buildingBlock.MessageDefinition;
        }

        MessageComponentDefinition? messageComponent = entry.Value as MessageComponentDefinition;
        if (messageComponent != null)
        {
            return messageComponent.DataDictionary;
        }

        MessageElementDefinition? messageElement = entry.Value as MessageElementDefinition;
        if (messageElement != null)
        {
            return messageElement.ComponentContext;
        }

        ChoiceDefinition? choice = entry.Value as ChoiceDefinition;
        if (choice != null)
        {
            if (choice.OwnerMessageComponent != null)
            {
                return choice.OwnerMessageComponent;
            }

            if (choice.OwnerMessageDefinition != null)
            {
                return choice.OwnerMessageDefinition;
            }

            return choice.DataDictionary;
        }

        DataTypeDefinition? dataType = entry.Value as DataTypeDefinition;
        if (dataType != null)
        {
            return dataType.DataDictionary;
        }

        CodeSetDefinition? codeSet = entry.Value as CodeSetDefinition;
        if (codeSet != null)
        {
            return codeSet.DataDictionary;
        }

        CodeValueDefinition? codeValue = entry.Value as CodeValueDefinition;
        if (codeValue != null)
        {
            return codeValue.Owner;
        }

        EncodingDefinition? encoding = entry.Value as EncodingDefinition;
        if (encoding != null)
        {
            return encoding.Syntax;
        }

        ExternalSchemaDefinition? externalSchema = entry.Value as ExternalSchemaDefinition;
        if (externalSchema != null)
        {
            return externalSchema.DataDictionary;
        }

        SyntaxMessageSchemeDefinition? scheme = entry.Value as SyntaxMessageSchemeDefinition;
        if (scheme != null)
        {
            return scheme.MessageDefinitionTrace;
        }

        ConstraintDefinition? constraint = entry.Value as ConstraintDefinition;
        if (constraint != null)
        {
            return constraint.Owner;
        }

        ConvergenceDefinition? convergence = entry.Value as ConvergenceDefinition;
        if (convergence != null)
        {
            return convergence.BusinessProcessCatalogue;
        }

        return null;
    }

    internal static string ResolveSemanticKey(RepositoryIndexEntry entry)
    {
        if (entry.Kind == RepositoryEntityKind.Repository ||
            entry.Kind == RepositoryEntityKind.DataDictionary ||
            entry.Kind == RepositoryEntityKind.BusinessProcessCatalogue)
        {
            return "KIND:" + ((int)entry.Kind).ToString();
        }

        BusinessAreaDefinition? area = entry.Value as BusinessAreaDefinition;
        if (area != null && area.Code.Length > 0)
        {
            return "AREA:" + NormalizeKey(area.Code);
        }

        MessageDefinitionMetadata? message = entry.Value as MessageDefinitionMetadata;
        if (message != null)
        {
            if (message.XmlName.Length > 0)
            {
                return "XML:" + NormalizeKey(message.XmlName);
            }

            if (message.XmlTag.Length > 0)
            {
                return "XML:" + NormalizeKey(message.XmlTag);
            }
        }

        MessageBuildingBlockDefinition? block = entry.Value as MessageBuildingBlockDefinition;
        if (block != null && block.XmlTag.Length > 0)
        {
            return "XML:" + NormalizeKey(block.XmlTag);
        }

        MessageElementDefinition? element = entry.Value as MessageElementDefinition;
        if (element != null && element.XmlTag.Length > 0)
        {
            return "XML:" + NormalizeKey(element.XmlTag);
        }

        CodeValueDefinition? code = entry.Value as CodeValueDefinition;
        if (code != null && code.Value.Length > 0)
        {
            return "CODE:" + NormalizeKey(code.Value);
        }

        DataTypeDefinition? dataType = entry.Value as DataTypeDefinition;
        if (dataType != null)
        {
            if (dataType.SchemaTypeName.Length > 0)
            {
                return "TYPE:" + NormalizeKey(dataType.SchemaTypeName);
            }

            if (dataType.SourceTypeName.Length > 0)
            {
                return "TYPE:" + NormalizeKey(dataType.SourceTypeName);
            }
        }

        ExternalSchemaDefinition? external = entry.Value as ExternalSchemaDefinition;
        if (external != null && external.SourceTypeName.Length > 0)
        {
            return "TYPE:" + NormalizeKey(external.SourceTypeName);
        }

        if (entry.Name.Length > 0)
        {
            return "NAME:" + NormalizeKey(entry.Name);
        }

        return string.Empty;
    }

    internal static string NormalizeKey(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    internal static RepositoryMatchResult Fail(
        RepositoryMatchResult result,
        string code,
        string message)
    {
        result.Success = false;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
