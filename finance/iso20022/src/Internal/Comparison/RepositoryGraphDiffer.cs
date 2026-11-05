using System;
using System.Collections.Generic;
using System.Globalization;

namespace iso20022;

internal static class RepositoryGraphDiffer
{
    internal static RepositoryDiff CreateDifference(
        RepositoryComparisonRequest request,
        RepositoryMatchResult matches)
    {
        RepositoryDiffContext context = InitializeDifferenceContext(request, matches);
        return CompleteDifference(context);
    }

    internal static RepositoryDiff CreateScopedDifference(
        RepositoryComparisonRequest request,
        RepositoryMatchResult matches,
        HashSet<string> sourceIdentifiers,
        HashSet<string> targetIdentifiers)
    {
        RepositoryDiffContext context = InitializeDifferenceContext(request, matches);
        context.ScopeEnabled = true;
        context.SourceScopeIdentifiers = sourceIdentifiers;
        context.TargetScopeIdentifiers = targetIdentifiers;
        return CompleteDifference(context);
    }

    internal static RepositoryDiffContext InitializeDifferenceContext(
        RepositoryComparisonRequest request,
        RepositoryMatchResult matches)
    {
        RepositoryDiffContext context = new RepositoryDiffContext();
        context.Request = request;
        context.MatchResult = matches;
        context.Difference.SourceEdition = request.Source == null ? string.Empty : request.Source.Edition;
        context.Difference.TargetEdition = request.Target == null ? string.Empty : request.Target.Edition;
        context.MaximumDifferenceCount = request.Limits.MaxCollectionCount;
        if (request.Source == null || request.Target == null)
        {
            context.Difference.ErrorCode = "comparison.repository_required";
            context.Difference.ErrorMessage = "Both source and target repository snapshots are required.";
            return context;
        }

        context.SourceIndex = RepositorySnapshotIndexer.BuildIndex(request.Source);
        context.TargetIndex = RepositorySnapshotIndexer.BuildIndex(request.Target);
        BuildMatchMaps(context);
        return context;
    }

    internal static RepositoryDiff CompleteDifference(RepositoryDiffContext context)
    {
        if (context.Difference.ErrorCode.Length > 0)
        {
            return context.Difference;
        }

        CompareMatchedEntities(context);
        if (CancelDifference(context))
        {
            return context.Difference;
        }

        CompareRemovedEntities(context);
        CompareAddedEntities(context);
        CompareRelationships(context);
        if (CancelDifference(context))
        {
            return context.Difference;
        }

        context.Difference.Success = true;
        context.Difference.Equivalent = context.MaterialDifferenceCount == 0;
        if (context.Request.AssessCompatibility)
        {
            CompatibilityClassifier.AppendAssessment(context.Request, context.Difference);
        }

        return context.Difference;
    }

    internal static void BuildMatchMaps(RepositoryDiffContext context)
    {
        int matchIndex = 0;
        while (matchIndex < context.MatchResult.Matches.Count)
        {
            RepositoryEntityMatch match = context.MatchResult.Matches[matchIndex];
            if (match.Target != null)
            {
                if (match.Source.Identifier.Length > 0 &&
                    !context.SourceToTargetIdentifiers.ContainsKey(match.Source.Identifier))
                {
                    context.SourceToTargetIdentifiers.Add(
                        match.Source.Identifier,
                        match.Target.Identifier);
                }

                if (match.Source.Identifier.Length > 0 &&
                    !context.MatchesBySourceIdentifier.ContainsKey(match.Source.Identifier))
                {
                    context.MatchesBySourceIdentifier.Add(match.Source.Identifier, match);
                }
            }

            int alternativeIndex = 0;
            while (alternativeIndex < match.AlternativeTargets.Count)
            {
                context.AmbiguousTargets.Add(match.AlternativeTargets[alternativeIndex]);
                alternativeIndex++;
            }

            matchIndex++;
        }
    }

    internal static void CompareMatchedEntities(RepositoryDiffContext context)
    {
        int matchIndex = 0;
        while (matchIndex < context.MatchResult.Matches.Count)
        {
            if (context.Request.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            RepositoryEntityMatch match = context.MatchResult.Matches[matchIndex];
            if (match.Ambiguous)
            {
                DifferenceItem ambiguity = CreateMatchDifference(
                    context,
                    match,
                    DifferenceKind.AmbiguousMatch,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    match.Rationale);
                ambiguity.Ambiguous = true;
                int alternativeIndex = 0;
                while (alternativeIndex < match.AlternativeTargets.Count)
                {
                    ambiguity.AlternativeTargetIdentifiers.Add(
                        match.AlternativeTargets[alternativeIndex].Identifier);
                    alternativeIndex++;
                }

                context.Difference.HasAmbiguousMatches = true;
                AddDifference(context, ambiguity);
            }
            else if (match.Target != null)
            {
                int materialBefore = context.MaterialDifferenceCount;
                CompareMatchedEntity(context, match);
                if (context.Request.IncludeUnchanged &&
                    context.MaterialDifferenceCount == materialBefore)
                {
                    DifferenceItem unchanged = CreateMatchDifference(
                        context,
                        match,
                        DifferenceKind.Unchanged,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        "The matched repository entity has no retained value change.");
                    AddDifference(context, unchanged);
                }
            }

            matchIndex++;
        }
    }

    internal static void CompareMatchedEntity(
        RepositoryDiffContext context,
        RepositoryEntityMatch match)
    {
        if (match.Target == null)
        {
            return;
        }

        if (match.Source.Kind != RepositoryEntityKind.Repository &&
            match.Source.Name != match.Target.Name)
        {
            DifferenceItem renamed = CreateMatchDifference(
                context,
                match,
                DifferenceKind.Renamed,
                "Name",
                match.Source.Name,
                match.Target.Name,
                "The matched repository entity name changed.");
            AddDifference(context, renamed);
        }

        if (context.Request.DetectMoves)
        {
            RepositoryReference? sourceOwner = RepositoryMatcher.ResolveOwnerReference(match.Source);
            RepositoryReference? targetOwner = RepositoryMatcher.ResolveOwnerReference(match.Target);
            string sourceOwnerValue = FormatReference(context, sourceOwner, true);
            string targetOwnerValue = FormatReference(context, targetOwner, false);
            if (sourceOwnerValue != targetOwnerValue)
            {
                DifferenceItem moved = CreateMatchDifference(
                    context,
                    match,
                    DifferenceKind.Moved,
                    "Owner",
                    SourceReferenceValue(sourceOwner),
                    SourceReferenceValue(targetOwner),
                    "The matched entity belongs to a different containing repository entity.");
                AddDifference(context, moved);
            }
        }

        RegistrationMetadata? sourceRegistration = ResolveRegistration(match.Source);
        RegistrationMetadata? targetRegistration = ResolveRegistration(match.Target);
        if (sourceRegistration != null && targetRegistration != null)
        {
            string sourceRegistrationValue = FormatRegistration(context, sourceRegistration, true);
            string targetRegistrationValue = FormatRegistration(context, targetRegistration, false);
            if (sourceRegistrationValue != targetRegistrationValue)
            {
                DifferenceItem registration = CreateMatchDifference(
                    context,
                    match,
                    DifferenceKind.RegistrationChanged,
                    "Registration",
                    sourceRegistrationValue,
                    targetRegistrationValue,
                    "Registration or lifecycle metadata changed.");
                AddDifference(context, registration);
            }

            CompareOwnedConstraints(
                context,
                match,
                sourceRegistration.Constraints,
                targetRegistration.Constraints);
        }

        List<RepositoryEntityDiffValue> sourceValues = BuildEntityValues(
            context,
            match.Source,
            true);
        List<RepositoryEntityDiffValue> targetValues = BuildEntityValues(
            context,
            match.Target,
            false);
        int valueIndex = 0;
        while (valueIndex < sourceValues.Count)
        {
            RepositoryEntityDiffValue sourceValue = sourceValues[valueIndex];
            RepositoryEntityDiffValue? targetValue = FindStateValue(
                targetValues,
                sourceValue.PropertyName);
            string targetText = targetValue == null ? string.Empty : targetValue.Value;
            if (sourceValue.Value != targetText)
            {
                DifferenceItem changed = CreateMatchDifference(
                    context,
                    match,
                    sourceValue.Kind,
                    sourceValue.PropertyName,
                    sourceValue.Value,
                    targetText,
                    sourceValue.Rationale);
                AddDifference(context, changed);
            }

            valueIndex++;
        }
    }

    internal static void CompareRemovedEntities(RepositoryDiffContext context)
    {
        int sourceIndex = 0;
        while (sourceIndex < context.MatchResult.UnmatchedSources.Count)
        {
            RepositoryIndexEntry source = context.MatchResult.UnmatchedSources[sourceIndex];
            DifferenceKind kind = DifferenceKind.Removed;
            if (source.Kind == RepositoryEntityKind.CodeValue)
            {
                kind = DifferenceKind.CodeRemoved;
            }

            if (source.Kind == RepositoryEntityKind.Constraint)
            {
                kind = DifferenceKind.ConstraintRemoved;
            }

            DifferenceItem removed = CreateUnmatchedDifference(
                context,
                source,
                null,
                kind,
                "The source repository entity has no target match.");
            AddDifference(context, removed);
            sourceIndex++;
        }
    }

    internal static void CompareAddedEntities(RepositoryDiffContext context)
    {
        int targetIndex = 0;
        while (targetIndex < context.MatchResult.UnmatchedTargets.Count)
        {
            RepositoryIndexEntry target = context.MatchResult.UnmatchedTargets[targetIndex];
            if (!context.AmbiguousTargets.Contains(target))
            {
                DifferenceKind kind = DifferenceKind.Added;
                if (target.Kind == RepositoryEntityKind.CodeValue)
                {
                    kind = DifferenceKind.CodeAdded;
                }

                if (target.Kind == RepositoryEntityKind.Constraint)
                {
                    kind = DifferenceKind.ConstraintAdded;
                }

                DifferenceItem added = CreateUnmatchedDifference(
                    context,
                    null,
                    target,
                    kind,
                    "The target repository entity has no source match.");
                AddDifference(context, added);
            }

            targetIndex++;
        }
    }

    internal static List<RepositoryEntityDiffValue> BuildEntityValues(
        RepositoryDiffContext context,
        RepositoryIndexEntry entry,
        bool sourceSide)
    {
        List<RepositoryEntityDiffValue> values = new List<RepositoryEntityDiffValue>();
        string? definition = ResolveDefinition(entry);
        if (definition != null)
        {
            AddStateValue(
                values,
                "Definition",
                definition,
                ResolveEntityChangeKind(entry.Kind),
                "The repository definition text changed.");
        }

        AppendBusinessValues(context, entry, sourceSide, values);
        AppendMessageValues(context, entry, sourceSide, values);
        AppendDataTypeValues(context, entry, sourceSide, values);
        AppendSyntaxValues(context, entry, sourceSide, values);
        AppendTransportValues(entry, values);
        AppendConstraintValues(entry, values);
        return values;
    }

    internal static void AddStateValue(
        List<RepositoryEntityDiffValue> values,
        string propertyName,
        string value,
        DifferenceKind kind,
        string rationale)
    {
        RepositoryEntityDiffValue state = new RepositoryEntityDiffValue();
        state.PropertyName = propertyName;
        state.Value = value;
        state.Kind = kind;
        state.Rationale = rationale;
        values.Add(state);
    }

    internal static RepositoryEntityDiffValue? FindStateValue(
        List<RepositoryEntityDiffValue> values,
        string propertyName)
    {
        int valueIndex = 0;
        while (valueIndex < values.Count)
        {
            if (values[valueIndex].PropertyName == propertyName)
            {
                return values[valueIndex];
            }

            valueIndex++;
        }

        return null;
    }

    internal static void AppendBusinessValues(
        RepositoryDiffContext context,
        RepositoryIndexEntry entry,
        bool sourceSide,
        List<RepositoryEntityDiffValue> values)
    {
        BusinessAreaDefinition? area = entry.Value as BusinessAreaDefinition;
        if (area != null)
        {
            AddStateValue(
                values,
                "BusinessAreaCode",
                area.Code,
                DifferenceKind.Modified,
                "The business-area code changed.");
        }

        ParticipantDefinition? participant = entry.Value as ParticipantDefinition;
        if (participant != null)
        {
            AddStateValue(
                values,
                "Cardinality",
                FormatOccurrenceRange(
                    participant.MinimumOccurs,
                    participant.HasMaximumOccurs,
                    participant.MaximumOccurs),
                DifferenceKind.CardinalityChanged,
                "The participant occurrence range changed.");
        }

        BusinessElementDefinition? element = entry.Value as BusinessElementDefinition;
        if (element != null)
        {
            AddStateValue(
                values,
                "ElementKind",
                ((int)element.Kind).ToString(CultureInfo.InvariantCulture),
                DifferenceKind.TypeChanged,
                "The business-element kind changed.");
            AddStateValue(
                values,
                "Cardinality",
                FormatMultiplicity(element.Multiplicity),
                DifferenceKind.CardinalityChanged,
                "The business-element occurrence range changed.");
            AddStateValue(
                values,
                "MemberType",
                FormatReference(context, element.MemberType, sourceSide) + "|" +
                    FormatReference(context, element.SimpleType, sourceSide) + "|" +
                    FormatReference(context, element.ComplexType, sourceSide),
                DifferenceKind.TypeChanged,
                "The business-element member type changed.");
            AddStateValue(
                values,
                "BusinessElementProperties",
                FormatBoolean(element.IsDerived) + "|" + element.Aggregation,
                DifferenceKind.Modified,
                "Business-element derivation or aggregation metadata changed.");
        }
    }

    internal static void AppendMessageValues(
        RepositoryDiffContext context,
        RepositoryIndexEntry entry,
        bool sourceSide,
        List<RepositoryEntityDiffValue> values)
    {
        MessageTransmissionDefinition? transmission = entry.Value as MessageTransmissionDefinition;
        if (transmission != null)
        {
            AddStateValue(
                values,
                "MessageTypeDescription",
                transmission.MessageTypeDescription,
                DifferenceKind.Modified,
                "The transmission message-type description changed.");
        }

        MessageDefinitionMetadata? definition = entry.Value as MessageDefinitionMetadata;
        if (definition != null)
        {
            AddStateValue(
                values,
                "MessageIdentifier",
                FormatMessageIdentifier(definition.MessageIdentifier),
                DifferenceKind.Modified,
                "The normalized message identifier changed.");
            AddStateValue(
                values,
                "MessageXmlMetadata",
                definition.NamespaceUri + "|" + definition.XmlName + "|" +
                    definition.XmlTag + "|" + definition.RootElement,
                DifferenceKind.Modified,
                "Message namespace or XML root metadata changed.");
        }

        MessageBuildingBlockDefinition? block = entry.Value as MessageBuildingBlockDefinition;
        if (block != null)
        {
            AddStateValue(
                values,
                "XmlTag",
                block.XmlTag,
                DifferenceKind.Modified,
                "The message building-block XML tag changed.");
            AddStateValue(
                values,
                "Cardinality",
                FormatMultiplicity(block.Multiplicity),
                DifferenceKind.CardinalityChanged,
                "The message building-block occurrence range changed.");
            AddStateValue(
                values,
                "MemberType",
                FormatReference(context, block.MemberType, sourceSide) + "|" +
                    FormatReference(context, block.SimpleType, sourceSide) + "|" +
                    FormatReference(context, block.ComplexType, sourceSide),
                DifferenceKind.TypeChanged,
                "The message building-block member type changed.");
        }

        MessageComponentDefinition? component = entry.Value as MessageComponentDefinition;
        if (component != null)
        {
            AddStateValue(
                values,
                "TechnicalComponent",
                FormatBoolean(component.IsTechnical),
                DifferenceKind.Modified,
                "The message-component technical classification changed.");
        }

        MessageElementDefinition? element = entry.Value as MessageElementDefinition;
        if (element != null)
        {
            AddStateValue(
                values,
                "ElementKind",
                ((int)element.Kind).ToString(CultureInfo.InvariantCulture),
                DifferenceKind.TypeChanged,
                "The message-element kind changed.");
            AddStateValue(
                values,
                "Cardinality",
                FormatMultiplicity(element.Multiplicity),
                DifferenceKind.CardinalityChanged,
                "The message-element occurrence range changed.");
            AddStateValue(
                values,
                "MemberType",
                FormatReference(context, element.MemberType, sourceSide) + "|" +
                    FormatReference(context, element.SimpleType, sourceSide) + "|" +
                    FormatReference(context, element.ComplexType, sourceSide),
                DifferenceKind.TypeChanged,
                "The message-element member type changed.");
            AddStateValue(
                values,
                "MessageElementProperties",
                element.XmlTag + "|" + FormatBoolean(element.IsTechnical) + "|" +
                    FormatBoolean(element.IsDerived) + "|" +
                    FormatBoolean(element.IsComposite),
                DifferenceKind.Modified,
                "Message-element XML or derivation metadata changed.");
        }

        ChoiceDefinition? choice = entry.Value as ChoiceDefinition;
        if (choice != null)
        {
            AddStateValue(
                values,
                "Cardinality",
                FormatMultiplicity(choice.Multiplicity),
                DifferenceKind.CardinalityChanged,
                "The choice occurrence range changed.");
            AddStateValue(
                values,
                "ChoiceProperties",
                choice.ChoiceType + "|" + FormatBoolean(choice.IsTechnical),
                DifferenceKind.Modified,
                "Choice type or technical metadata changed.");
        }
    }

    internal static void AppendDataTypeValues(
        RepositoryDiffContext context,
        RepositoryIndexEntry entry,
        bool sourceSide,
        List<RepositoryEntityDiffValue> values)
    {
        DataTypeDefinition? dataType = entry.Value as DataTypeDefinition;
        if (dataType != null)
        {
            AddStateValue(
                values,
                "DataTypeKind",
                ((int)dataType.Kind).ToString(CultureInfo.InvariantCulture),
                DifferenceKind.TypeChanged,
                "The normalized data-type family changed.");
            AddStateValue(
                values,
                "DataTypeProperties",
                dataType.SourceTypeName + "|" + dataType.IdentificationScheme + "|" +
                    dataType.MeaningWhenTrue + "|" + dataType.MeaningWhenFalse + "|" +
                    FormatDecimal(dataType.BaseValue) + "|" + dataType.UnitCode + "|" +
                    ((int)dataType.BinaryEncoding).ToString(CultureInfo.InvariantCulture) + "|" +
                    dataType.SchemaTypeName + "|" +
                    FormatReference(context, dataType.CurrencyIdentifierSet, sourceSide),
                DifferenceKind.Modified,
                "Data-type lexical or semantic metadata changed.");
            AddStateValue(
                values,
                "Facets",
                FormatFacets(dataType.Facets),
                DifferenceKind.FacetChanged,
                "One or more data-type facets changed.");
        }

        CodeSetDefinition? codeSet = entry.Value as CodeSetDefinition;
        if (codeSet != null)
        {
            AddStateValue(
                values,
                "CodeSetProperties",
                ((int)codeSet.Kind).ToString(CultureInfo.InvariantCulture) + "|" +
                    codeSet.IdentificationScheme,
                DifferenceKind.CodeChanged,
                "Code-set kind or identification metadata changed.");
            AddStateValue(
                values,
                "Facets",
                FormatFacets(codeSet.Facets),
                DifferenceKind.FacetChanged,
                "One or more code-set facets changed.");
        }

        CodeValueDefinition? code = entry.Value as CodeValueDefinition;
        if (code != null)
        {
            AddStateValue(
                values,
                "CodeValue",
                code.Value,
                DifferenceKind.CodeChanged,
                "The matched code's lexical value changed.");
        }
    }

    internal static void AppendSyntaxValues(
        RepositoryDiffContext context,
        RepositoryIndexEntry entry,
        bool sourceSide,
        List<RepositoryEntityDiffValue> values)
    {
        ExternalSchemaDefinition? external = entry.Value as ExternalSchemaDefinition;
        if (external != null)
        {
            AddStateValue(
                values,
                "ExternalSchemaKind",
                ((int)external.Kind).ToString(CultureInfo.InvariantCulture),
                DifferenceKind.TypeChanged,
                "The external-schema data-type family changed.");
            AddStateValue(
                values,
                "Cardinality",
                FormatMultiplicity(external.Multiplicity),
                DifferenceKind.CardinalityChanged,
                "The external-schema occurrence range changed.");
            AddStateValue(
                values,
                "ExternalSchemaProperties",
                external.SourceTypeName + "|" + external.NamespaceMode + "|" +
                    FormatStrings(external.NamespaceUris) + "|" + external.ProcessContents + "|" +
                    FormatBoolean(external.IsTechnical) + "|" +
                    FormatReference(context, external.BusinessComponentTrace, sourceSide),
                DifferenceKind.SyntaxChanged,
                "External-schema namespace or processing metadata changed.");
        }
    }

    internal static void AppendTransportValues(
        RepositoryIndexEntry entry,
        List<RepositoryEntityDiffValue> values)
    {
        TransportDefinition? transport = entry.Value as TransportDefinition;
        if (transport == null)
        {
            return;
        }

        string transportValue =
            FormatDuration(transport.BoundedCommunicationDelay) + "|" +
            FormatDuration(transport.MaximumClockVariation) + "|" +
            transport.MaximumMessageSizeKilobytes.ToString(CultureInfo.InvariantCulture) + "|" +
            FormatDuration(transport.MessageDeliveryWindow) + "|" +
            FormatDuration(transport.MessageSendingWindow) + "|" +
            transport.DeliveryAssurance + "|" + transport.Durability + "|" +
            transport.MessageCasting + "|" + transport.MessageDeliveryOrder + "|" +
            transport.MessageValidationLevel + "|" + transport.MessageValidationOnOff + "|" +
            transport.MessageValidationResults + "|" + transport.ReceiverAsynchronicity + "|" +
            transport.SenderAsynchronicity;
        AddStateValue(
            values,
            "TransportProperties",
            transportValue,
            DifferenceKind.TransportChanged,
            "Transport timing, size, assurance, ordering, or validation metadata changed.");
    }

    internal static void AppendConstraintValues(
        RepositoryIndexEntry entry,
        List<RepositoryEntityDiffValue> values)
    {
        ConstraintDefinition? constraint = entry.Value as ConstraintDefinition;
        if (constraint != null)
        {
            AddStateValue(
                values,
                "ConstraintExpression",
                constraint.ExpressionLanguage + "|" + constraint.Expression,
                DifferenceKind.ConstraintChanged,
                "The constraint expression or expression language changed.");
        }
    }

    internal static void CompareOwnedConstraints(
        RepositoryDiffContext context,
        RepositoryEntityMatch ownerMatch,
        List<ConstraintDefinition> sources,
        List<ConstraintDefinition> targets)
    {
        bool[] claimedTargets = new bool[targets.Count];
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            ConstraintDefinition source = sources[sourceIndex];
            int targetIndex = FindOwnedConstraintTarget(source, targets, claimedTargets);
            if (targetIndex < 0)
            {
                DifferenceItem removed = CreateOwnedConstraintDifference(
                    context,
                    ownerMatch,
                    source,
                    null,
                    DifferenceKind.ConstraintRemoved,
                    "The owned constraint has no target match.");
                AddDifference(context, removed);
            }
            else
            {
                claimedTargets[targetIndex] = true;
                ConstraintDefinition target = targets[targetIndex];
                string sourceValue = source.Name + "|" + source.Definition + "|" +
                    source.ExpressionLanguage + "|" + source.Expression + "|" +
                    FormatRegistration(context, source.Registration, true);
                string targetValue = target.Name + "|" + target.Definition + "|" +
                    target.ExpressionLanguage + "|" + target.Expression + "|" +
                    FormatRegistration(context, target.Registration, false);
                if (sourceValue != targetValue)
                {
                    DifferenceItem changed = CreateOwnedConstraintDifference(
                        context,
                        ownerMatch,
                        source,
                        target,
                        DifferenceKind.ConstraintChanged,
                        "The matched owned constraint changed.");
                    changed.SourceValue = sourceValue;
                    changed.TargetValue = targetValue;
                    AddDifference(context, changed);
                }
            }

            sourceIndex++;
        }

        int addedIndex = 0;
        while (addedIndex < targets.Count)
        {
            if (!claimedTargets[addedIndex])
            {
                DifferenceItem added = CreateOwnedConstraintDifference(
                    context,
                    ownerMatch,
                    null,
                    targets[addedIndex],
                    DifferenceKind.ConstraintAdded,
                    "The owned target constraint has no source match.");
                AddDifference(context, added);
            }

            addedIndex++;
        }
    }

    internal static int FindOwnedConstraintTarget(
        ConstraintDefinition source,
        List<ConstraintDefinition> targets,
        bool[] claimedTargets)
    {
        int exactIndex = -1;
        int targetIndex = 0;
        while (targetIndex < targets.Count)
        {
            if (!claimedTargets[targetIndex] &&
                source.Identifier.Length > 0 &&
                targets[targetIndex].Identifier == source.Identifier)
            {
                if (exactIndex >= 0)
                {
                    return -1;
                }

                exactIndex = targetIndex;
            }

            targetIndex++;
        }

        if (exactIndex >= 0)
        {
            return exactIndex;
        }

        int semanticIndex = -1;
        targetIndex = 0;
        while (targetIndex < targets.Count)
        {
            if (!claimedTargets[targetIndex] &&
                source.Name.Length > 0 &&
                RepositoryMatcher.NormalizeKey(source.Name) ==
                    RepositoryMatcher.NormalizeKey(targets[targetIndex].Name))
            {
                if (semanticIndex >= 0)
                {
                    return -1;
                }

                semanticIndex = targetIndex;
            }

            targetIndex++;
        }

        return semanticIndex;
    }

    internal static DifferenceItem CreateOwnedConstraintDifference(
        RepositoryDiffContext context,
        RepositoryEntityMatch ownerMatch,
        ConstraintDefinition? source,
        ConstraintDefinition? target,
        DifferenceKind kind,
        string rationale)
    {
        DifferenceItem difference = CreateMatchDifference(
            context,
            ownerMatch,
            kind,
            "Constraint",
            source == null ? string.Empty : source.Expression,
            target == null ? string.Empty : target.Expression,
            rationale);
        difference.EntityKind = RepositoryEntityKind.Constraint;
        difference.SourceIdentifier = source == null ? string.Empty : source.Identifier;
        difference.TargetIdentifier = target == null ? string.Empty : target.Identifier;
        difference.SourceName = source == null ? string.Empty : source.Name;
        difference.TargetName = target == null ? string.Empty : target.Name;
        difference.SourcePath = CreatePath(
            RepositoryEntityKind.Constraint,
            difference.SourceName,
            difference.SourceIdentifier);
        difference.TargetPath = CreatePath(
            RepositoryEntityKind.Constraint,
            difference.TargetName,
            difference.TargetIdentifier);
        return difference;
    }

    internal static void CompareRelationships(RepositoryDiffContext context)
    {
        if (context.Request.Source == null || context.Request.Target == null)
        {
            return;
        }

        List<RepositoryReference> sources = context.Request.Source.References;
        List<RepositoryReference> targets = context.Request.Target.References;
        bool[] claimedTargets = new bool[targets.Count];
        int sourceIndex = 0;
        while (sourceIndex < sources.Count)
        {
            if (context.Request.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            RepositoryReference source = sources[sourceIndex];
            int targetIndex = FindRelationshipTarget(context, source, targets, claimedTargets);
            if (targetIndex < 0)
            {
                DifferenceItem removed = CreateRelationshipDifference(
                    context,
                    source,
                    null,
                    DifferenceKind.RelationshipRemoved,
                    "The source relationship has no target relationship at the same graph position.");
                AddDifference(context, removed);
            }
            else
            {
                claimedTargets[targetIndex] = true;
                RepositoryReference target = targets[targetIndex];
                if (FormatRelationshipTarget(context, source, true) !=
                    FormatRelationshipTarget(context, target, false))
                {
                    DifferenceItem changed = CreateRelationshipDifference(
                        context,
                        source,
                        target,
                        DifferenceKind.RelationshipChanged,
                        "The target or resolution state of the matched relationship changed.");
                    AddDifference(context, changed);
                }
            }

            sourceIndex++;
        }

        int addedIndex = 0;
        while (addedIndex < targets.Count)
        {
            if (!claimedTargets[addedIndex])
            {
                DifferenceItem added = CreateRelationshipDifference(
                    context,
                    null,
                    targets[addedIndex],
                    DifferenceKind.RelationshipAdded,
                    "The target relationship has no source relationship at the same graph position.");
                AddDifference(context, added);
            }

            addedIndex++;
        }
    }

    internal static int FindRelationshipTarget(
        RepositoryDiffContext context,
        RepositoryReference source,
        List<RepositoryReference> targets,
        bool[] claimedTargets)
    {
        string sourceIdentifier = MapSourceIdentifier(context, source.SourceIdentifier);
        int targetIndex = 0;
        while (targetIndex < targets.Count)
        {
            RepositoryReference target = targets[targetIndex];
            if (!claimedTargets[targetIndex] &&
                target.SourceIdentifier == sourceIdentifier &&
                target.PropertyName == source.PropertyName &&
                target.Position == source.Position)
            {
                return targetIndex;
            }

            targetIndex++;
        }

        return -1;
    }

    internal static string FormatRelationshipTarget(
        RepositoryDiffContext context,
        RepositoryReference reference,
        bool sourceSide)
    {
        return FormatReference(context, reference, sourceSide) + "|" +
            ((int)reference.TargetKind).ToString(CultureInfo.InvariantCulture) + "|" +
            FormatBoolean(reference.IsContainment) + "|" +
            FormatBoolean(reference.IsResolved) + "|" + reference.ErrorCode;
    }

    internal static DifferenceItem CreateRelationshipDifference(
        RepositoryDiffContext context,
        RepositoryReference? source,
        RepositoryReference? target,
        DifferenceKind kind,
        string rationale)
    {
        DifferenceItem difference = new DifferenceItem();
        difference.Kind = kind;
        difference.EntityKind = source == null ? target == null ? RepositoryEntityKind.Unknown :
            target.SourceKind : source.SourceKind;
        difference.SourceIdentifier = source == null ? string.Empty : source.SourceIdentifier;
        difference.TargetIdentifier = target == null ? string.Empty : target.SourceIdentifier;
        difference.PropertyName = source == null ? target == null ? string.Empty :
            target.PropertyName : source.PropertyName;
        difference.SourceValue = source == null ? string.Empty : source.TargetIdentifier;
        difference.TargetValue = target == null ? string.Empty : target.TargetIdentifier;
        difference.Rationale = rationale;
        difference.SourceArtifact = context.Request.Source == null ? null :
            context.Request.Source.Provenance.Artifact;
        difference.TargetArtifact = context.Request.Target == null ? null :
            context.Request.Target.Provenance.Artifact;

        RepositoryEntityMatch? sourceMatch = null;
        if (source != null && context.MatchesBySourceIdentifier.ContainsKey(source.SourceIdentifier))
        {
            sourceMatch = context.MatchesBySourceIdentifier[source.SourceIdentifier];
            difference.MatchConfidence = sourceMatch.Confidence;
            difference.ExactIdentifierMatch = sourceMatch.ExactIdentifierMatch;
            difference.SemanticFallbackMatch = sourceMatch.SemanticFallbackMatch;
            difference.OverrideMatch = sourceMatch.OverrideMatch;
            difference.SourceName = sourceMatch.Source.Name;
            difference.TargetName = sourceMatch.Target == null ? string.Empty : sourceMatch.Target.Name;
        }

        difference.SourcePath = CreatePath(
            difference.EntityKind,
            difference.SourceName,
            difference.SourceIdentifier);
        difference.TargetPath = CreatePath(
            difference.EntityKind,
            difference.TargetName,
            difference.TargetIdentifier);
        return difference;
    }

    internal static DifferenceItem CreateMatchDifference(
        RepositoryDiffContext context,
        RepositoryEntityMatch match,
        DifferenceKind kind,
        string propertyName,
        string sourceValue,
        string targetValue,
        string rationale)
    {
        DifferenceItem difference = new DifferenceItem();
        difference.Kind = kind;
        difference.EntityKind = match.Source.Kind;
        difference.SourceIdentifier = match.Source.Identifier;
        difference.TargetIdentifier = match.Target == null ? string.Empty : match.Target.Identifier;
        difference.SourceName = match.Source.Name;
        difference.TargetName = match.Target == null ? string.Empty : match.Target.Name;
        difference.SourcePath = CreatePath(
            match.Source.Kind,
            match.Source.Name,
            match.Source.Identifier);
        difference.TargetPath = CreatePath(
            match.Target == null ? match.Source.Kind : match.Target.Kind,
            match.Target == null ? string.Empty : match.Target.Name,
            match.Target == null ? string.Empty : match.Target.Identifier);
        difference.PropertyName = propertyName;
        difference.SourceValue = sourceValue;
        difference.TargetValue = targetValue;
        difference.MatchConfidence = match.Confidence;
        difference.ExactIdentifierMatch = match.ExactIdentifierMatch;
        difference.SemanticFallbackMatch = match.SemanticFallbackMatch;
        difference.OverrideMatch = match.OverrideMatch;
        difference.Ambiguous = match.Ambiguous;
        difference.Rationale = rationale;
        difference.SourceArtifact = context.Request.Source == null ? null :
            context.Request.Source.Provenance.Artifact;
        difference.TargetArtifact = context.Request.Target == null ? null :
            context.Request.Target.Provenance.Artifact;
        return difference;
    }

    internal static DifferenceItem CreateUnmatchedDifference(
        RepositoryDiffContext context,
        RepositoryIndexEntry? source,
        RepositoryIndexEntry? target,
        DifferenceKind kind,
        string rationale)
    {
        DifferenceItem difference = new DifferenceItem();
        difference.Kind = kind;
        difference.EntityKind = source == null ? target == null ? RepositoryEntityKind.Unknown :
            target.Kind : source.Kind;
        difference.SourceIdentifier = source == null ? string.Empty : source.Identifier;
        difference.TargetIdentifier = target == null ? string.Empty : target.Identifier;
        difference.SourceName = source == null ? string.Empty : source.Name;
        difference.TargetName = target == null ? string.Empty : target.Name;
        difference.SourcePath = CreatePath(
            difference.EntityKind,
            difference.SourceName,
            difference.SourceIdentifier);
        difference.TargetPath = CreatePath(
            difference.EntityKind,
            difference.TargetName,
            difference.TargetIdentifier);
        difference.Rationale = rationale;
        difference.SourceArtifact = context.Request.Source == null ? null :
            context.Request.Source.Provenance.Artifact;
        difference.TargetArtifact = context.Request.Target == null ? null :
            context.Request.Target.Provenance.Artifact;
        return difference;
    }

    internal static XmlPathData CreatePath(
        RepositoryEntityKind kind,
        string name,
        string identifier)
    {
        XmlPathData path = new XmlPathData();
        string finalName = name.Length == 0 ? identifier : name;
        if (finalName.Length > 0)
        {
            path.Value = "/" + kind.ToString() + "/" + finalName;
        }

        return path;
    }

    internal static void AddDifference(
        RepositoryDiffContext context,
        DifferenceItem difference)
    {
        if (!DifferenceIsInScope(context, difference))
        {
            return;
        }

        if (difference.Kind != DifferenceKind.Unchanged)
        {
            context.MaterialDifferenceCount++;
        }

        if (context.Difference.Differences.Count >= context.MaximumDifferenceCount)
        {
            context.Difference.Truncated = true;
            return;
        }

        context.Difference.Differences.Add(difference);
    }

    internal static bool DifferenceIsInScope(
        RepositoryDiffContext context,
        DifferenceItem difference)
    {
        if (!context.ScopeEnabled)
        {
            return true;
        }

        if (difference.SourceIdentifier.Length > 0 &&
            context.SourceScopeIdentifiers.Contains(difference.SourceIdentifier))
        {
            return true;
        }

        if (difference.TargetIdentifier.Length > 0 &&
            context.TargetScopeIdentifiers.Contains(difference.TargetIdentifier))
        {
            return true;
        }

        return false;
    }

    internal static bool CancelDifference(RepositoryDiffContext context)
    {
        if (!context.Request.CancellationToken.IsCancellationRequested)
        {
            return false;
        }

        context.Difference.Success = false;
        context.Difference.Equivalent = false;
        context.Difference.Truncated = false;
        context.Difference.Differences.Clear();
        context.Difference.ErrorCode = "comparison.cancelled";
        context.Difference.ErrorMessage = "Repository graph comparison was cancelled.";
        return true;
    }

    internal static string MapSourceIdentifier(
        RepositoryDiffContext context,
        string sourceIdentifier)
    {
        if (context.SourceToTargetIdentifiers.ContainsKey(sourceIdentifier))
        {
            return context.SourceToTargetIdentifiers[sourceIdentifier];
        }

        return sourceIdentifier;
    }

    internal static string FormatReference(
        RepositoryDiffContext context,
        RepositoryReference? reference,
        bool sourceSide)
    {
        if (reference == null)
        {
            return string.Empty;
        }

        if (sourceSide)
        {
            return MapSourceIdentifier(context, reference.TargetIdentifier);
        }

        return reference.TargetIdentifier;
    }

    internal static string SourceReferenceValue(RepositoryReference? reference)
    {
        if (reference == null)
        {
            return string.Empty;
        }

        return reference.TargetIdentifier;
    }

    internal static string FormatRegistration(
        RepositoryDiffContext context,
        RegistrationMetadata registration,
        bool sourceSide)
    {
        string value =
            ((int)registration.Status).ToString(CultureInfo.InvariantCulture) + "|" +
            registration.StatusValue + "|" + FormatDate(registration.RegistrationDate) + "|" +
            FormatDate(registration.RemovalDate) + "|" +
            FormatBoolean(registration.IsDeprecated) + "|" + registration.DeprecationReason + "|" +
            FormatReference(context, registration.ReplacedBy, sourceSide) + "|" +
            FormatReference(context, registration.PreviousVersion, sourceSide) + "|";
        int exampleIndex = 0;
        while (exampleIndex < registration.Examples.Count)
        {
            value = value + registration.Examples[exampleIndex] + "\u001f";
            exampleIndex++;
        }

        return value;
    }

    internal static string FormatDate(IsoDateValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.LexicalValue.Length > 0)
        {
            return value.LexicalValue;
        }

        return value.Year.ToString(CultureInfo.InvariantCulture) + "-" +
            value.Month.ToString(CultureInfo.InvariantCulture) + "-" +
            value.Day.ToString(CultureInfo.InvariantCulture) + "|" +
            FormatBoolean(value.HasTimezone) + "|" +
            value.TimezoneOffsetMinutes.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatDuration(IsoDurationValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.LexicalValue.Length > 0)
        {
            return value.LexicalValue;
        }

        return FormatBoolean(value.IsNegative) + "|" +
            value.Years.ToString(CultureInfo.InvariantCulture) + "|" +
            value.Months.ToString(CultureInfo.InvariantCulture) + "|" +
            value.Days.ToString(CultureInfo.InvariantCulture) + "|" +
            value.Hours.ToString(CultureInfo.InvariantCulture) + "|" +
            value.Minutes.ToString(CultureInfo.InvariantCulture) + "|" +
            value.Seconds.ToString(CultureInfo.InvariantCulture) + "|" +
            value.FractionalSecondDigits;
    }

    internal static string FormatDecimal(IsoDecimalValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.LexicalValue.Length > 0)
        {
            return value.LexicalValue;
        }

        return value.Value.ToString(CultureInfo.InvariantCulture) + "|" +
            value.Scale.ToString(CultureInfo.InvariantCulture) + "|" +
            FormatBoolean(value.IsNegativeZero);
    }

    internal static string FormatMultiplicity(MultiplicityDefinition value)
    {
        return FormatOccurrenceRange(
            value.MinimumOccurs,
            value.HasMaximumOccurs,
            value.MaximumOccurs);
    }

    internal static string FormatOccurrenceRange(
        int minimumOccurs,
        bool hasMaximumOccurs,
        int maximumOccurs)
    {
        return minimumOccurs.ToString(CultureInfo.InvariantCulture) + "|" +
            FormatBoolean(hasMaximumOccurs) + "|" +
            maximumOccurs.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatFacets(List<DataTypeFacet> facets)
    {
        string value = string.Empty;
        int facetIndex = 0;
        while (facetIndex < facets.Count)
        {
            DataTypeFacet facet = facets[facetIndex];
            value = value + facet.Name + "=" + facet.Value + ":" +
                FormatBoolean(facet.HasIntegerValue) + ":" +
                facet.IntegerValue.ToString(CultureInfo.InvariantCulture) + ":" +
                FormatDecimal(facet.DecimalValue) + ":" +
                FormatStrings(facet.EnumerationValues) + "\u001e";
            facetIndex++;
        }

        return value;
    }

    internal static string FormatStrings(List<string> values)
    {
        string value = string.Empty;
        int itemIndex = 0;
        while (itemIndex < values.Count)
        {
            value = value + values[itemIndex] + "\u001f";
            itemIndex++;
        }

        return value;
    }

    internal static string FormatMessageIdentifier(MessageIdentifier identifier)
    {
        return identifier.BusinessArea + "|" + identifier.MessageFunctionality + "|" +
            identifier.Variant + "|" + identifier.Version + "|" + identifier.Value;
    }

    internal static string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    internal static DifferenceKind ResolveEntityChangeKind(RepositoryEntityKind kind)
    {
        if (kind == RepositoryEntityKind.CodeSet || kind == RepositoryEntityKind.CodeValue)
        {
            return DifferenceKind.CodeChanged;
        }

        if (kind == RepositoryEntityKind.Constraint)
        {
            return DifferenceKind.ConstraintChanged;
        }

        if (kind == RepositoryEntityKind.Syntax ||
            kind == RepositoryEntityKind.Encoding ||
            kind == RepositoryEntityKind.ExternalSchema ||
            kind == RepositoryEntityKind.SyntaxMessageScheme)
        {
            return DifferenceKind.SyntaxChanged;
        }

        if (kind == RepositoryEntityKind.MessagingEndpoint ||
            kind == RepositoryEntityKind.BroadcastList ||
            kind == RepositoryEntityKind.MessageTransportSystem ||
            kind == RepositoryEntityKind.TransportMode ||
            kind == RepositoryEntityKind.TransportCharacteristic ||
            kind == RepositoryEntityKind.Address ||
            kind == RepositoryEntityKind.TransportMessage)
        {
            return DifferenceKind.TransportChanged;
        }

        return DifferenceKind.Modified;
    }

    internal static string? ResolveDefinition(RepositoryIndexEntry entry)
    {
        BusinessAreaDefinition? businessArea = entry.Value as BusinessAreaDefinition;
        if (businessArea != null)
        {
            return businessArea.Definition;
        }

        BusinessProcessDefinition? businessProcess = entry.Value as BusinessProcessDefinition;
        if (businessProcess != null)
        {
            return businessProcess.Definition;
        }

        BusinessRoleDefinition? businessRole = entry.Value as BusinessRoleDefinition;
        if (businessRole != null)
        {
            return businessRole.Definition;
        }

        ParticipantDefinition? participant = entry.Value as ParticipantDefinition;
        if (participant != null)
        {
            return participant.Definition;
        }

        BusinessTransactionDefinition? transaction = entry.Value as BusinessTransactionDefinition;
        if (transaction != null)
        {
            return transaction.Definition;
        }

        ConversationDefinition? conversation = entry.Value as ConversationDefinition;
        if (conversation != null)
        {
            return conversation.Definition;
        }

        MessageChoreographyDefinition? choreography = entry.Value as MessageChoreographyDefinition;
        if (choreography != null)
        {
            return choreography.Definition;
        }

        MessageTransmissionDefinition? transmission = entry.Value as MessageTransmissionDefinition;
        if (transmission != null)
        {
            return transmission.Definition;
        }

        MessagingEndpointDefinition? endpoint = entry.Value as MessagingEndpointDefinition;
        if (endpoint != null)
        {
            return endpoint.Definition;
        }

        TransportDefinition? transport = entry.Value as TransportDefinition;
        if (transport != null)
        {
            return transport.Definition;
        }

        BusinessComponentDefinition? businessComponent = entry.Value as BusinessComponentDefinition;
        if (businessComponent != null)
        {
            return businessComponent.Definition;
        }

        BusinessElementDefinition? businessElement = entry.Value as BusinessElementDefinition;
        if (businessElement != null)
        {
            return businessElement.Definition;
        }

        MessageSetDefinition? messageSet = entry.Value as MessageSetDefinition;
        if (messageSet != null)
        {
            return messageSet.Definition;
        }

        MessageDefinitionMetadata? messageDefinition = entry.Value as MessageDefinitionMetadata;
        if (messageDefinition != null)
        {
            return messageDefinition.Definition;
        }

        MessageBuildingBlockDefinition? buildingBlock = entry.Value as MessageBuildingBlockDefinition;
        if (buildingBlock != null)
        {
            return buildingBlock.Definition;
        }

        MessageComponentDefinition? messageComponent = entry.Value as MessageComponentDefinition;
        if (messageComponent != null)
        {
            return messageComponent.Definition;
        }

        MessageElementDefinition? messageElement = entry.Value as MessageElementDefinition;
        if (messageElement != null)
        {
            return messageElement.Definition;
        }

        ChoiceDefinition? choice = entry.Value as ChoiceDefinition;
        if (choice != null)
        {
            return choice.Definition;
        }

        DataTypeDefinition? dataType = entry.Value as DataTypeDefinition;
        if (dataType != null)
        {
            return dataType.Definition;
        }

        CodeSetDefinition? codeSet = entry.Value as CodeSetDefinition;
        if (codeSet != null)
        {
            return codeSet.Definition;
        }

        CodeValueDefinition? codeValue = entry.Value as CodeValueDefinition;
        if (codeValue != null)
        {
            return codeValue.Definition;
        }

        SyntaxDefinition? syntax = entry.Value as SyntaxDefinition;
        if (syntax != null)
        {
            return syntax.Definition;
        }

        EncodingDefinition? encoding = entry.Value as EncodingDefinition;
        if (encoding != null)
        {
            return encoding.Definition;
        }

        ExternalSchemaDefinition? externalSchema = entry.Value as ExternalSchemaDefinition;
        if (externalSchema != null)
        {
            return externalSchema.Definition;
        }

        SyntaxMessageSchemeDefinition? scheme = entry.Value as SyntaxMessageSchemeDefinition;
        if (scheme != null)
        {
            return scheme.Definition;
        }

        ConstraintDefinition? constraint = entry.Value as ConstraintDefinition;
        if (constraint != null)
        {
            return constraint.Definition;
        }

        ConvergenceDefinition? convergence = entry.Value as ConvergenceDefinition;
        if (convergence != null)
        {
            return convergence.Definition;
        }

        return null;
    }

    internal static RegistrationMetadata? ResolveRegistration(RepositoryIndexEntry entry)
    {
        BusinessAreaDefinition? businessArea = entry.Value as BusinessAreaDefinition;
        if (businessArea != null)
        {
            return businessArea.Registration;
        }

        BusinessProcessDefinition? businessProcess = entry.Value as BusinessProcessDefinition;
        if (businessProcess != null)
        {
            return businessProcess.Registration;
        }

        BusinessRoleDefinition? businessRole = entry.Value as BusinessRoleDefinition;
        if (businessRole != null)
        {
            return businessRole.Registration;
        }

        ParticipantDefinition? participant = entry.Value as ParticipantDefinition;
        if (participant != null)
        {
            return participant.Registration;
        }

        BusinessTransactionDefinition? transaction = entry.Value as BusinessTransactionDefinition;
        if (transaction != null)
        {
            return transaction.Registration;
        }

        ConversationDefinition? conversation = entry.Value as ConversationDefinition;
        if (conversation != null)
        {
            return conversation.Registration;
        }

        MessageChoreographyDefinition? choreography = entry.Value as MessageChoreographyDefinition;
        if (choreography != null)
        {
            return choreography.Registration;
        }

        MessageTransmissionDefinition? transmission = entry.Value as MessageTransmissionDefinition;
        if (transmission != null)
        {
            return transmission.Registration;
        }

        MessagingEndpointDefinition? endpoint = entry.Value as MessagingEndpointDefinition;
        if (endpoint != null)
        {
            return endpoint.Registration;
        }

        TransportDefinition? transport = entry.Value as TransportDefinition;
        if (transport != null)
        {
            return transport.Registration;
        }

        BusinessComponentDefinition? businessComponent = entry.Value as BusinessComponentDefinition;
        if (businessComponent != null)
        {
            return businessComponent.Registration;
        }

        BusinessElementDefinition? businessElement = entry.Value as BusinessElementDefinition;
        if (businessElement != null)
        {
            return businessElement.Registration;
        }

        MessageSetDefinition? messageSet = entry.Value as MessageSetDefinition;
        if (messageSet != null)
        {
            return messageSet.Registration;
        }

        MessageDefinitionMetadata? messageDefinition = entry.Value as MessageDefinitionMetadata;
        if (messageDefinition != null)
        {
            return messageDefinition.Registration;
        }

        MessageBuildingBlockDefinition? buildingBlock = entry.Value as MessageBuildingBlockDefinition;
        if (buildingBlock != null)
        {
            return buildingBlock.Registration;
        }

        MessageComponentDefinition? messageComponent = entry.Value as MessageComponentDefinition;
        if (messageComponent != null)
        {
            return messageComponent.Registration;
        }

        MessageElementDefinition? messageElement = entry.Value as MessageElementDefinition;
        if (messageElement != null)
        {
            return messageElement.Registration;
        }

        ChoiceDefinition? choice = entry.Value as ChoiceDefinition;
        if (choice != null)
        {
            return choice.Registration;
        }

        DataTypeDefinition? dataType = entry.Value as DataTypeDefinition;
        if (dataType != null)
        {
            return dataType.Registration;
        }

        CodeSetDefinition? codeSet = entry.Value as CodeSetDefinition;
        if (codeSet != null)
        {
            return codeSet.Registration;
        }

        CodeValueDefinition? codeValue = entry.Value as CodeValueDefinition;
        if (codeValue != null)
        {
            return codeValue.Registration;
        }

        SyntaxDefinition? syntax = entry.Value as SyntaxDefinition;
        if (syntax != null)
        {
            return syntax.Registration;
        }

        EncodingDefinition? encoding = entry.Value as EncodingDefinition;
        if (encoding != null)
        {
            return encoding.Registration;
        }

        ExternalSchemaDefinition? externalSchema = entry.Value as ExternalSchemaDefinition;
        if (externalSchema != null)
        {
            return externalSchema.Registration;
        }

        SyntaxMessageSchemeDefinition? scheme = entry.Value as SyntaxMessageSchemeDefinition;
        if (scheme != null)
        {
            return scheme.Registration;
        }

        ConstraintDefinition? constraint = entry.Value as ConstraintDefinition;
        if (constraint != null)
        {
            return constraint.Registration;
        }

        ConvergenceDefinition? convergence = entry.Value as ConvergenceDefinition;
        if (convergence != null)
        {
            return convergence.Registration;
        }

        return null;
    }
}
