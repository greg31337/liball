using System.Collections.Generic;

namespace iso20022;

internal static class CompatibilityClassifier
{
    internal static void AppendAssessment(
        RepositoryComparisonRequest request,
        RepositoryDiff difference)
    {
        CompatibilityAssessment assessment = new CompatibilityAssessment();
        assessment.ReaderCompatible = true;
        assessment.WriterCompatible = true;
        assessment.SchemaCompatible = true;
        assessment.Lossless = true;
        assessment.SemanticallyCompatible = true;
        assessment.Confidence = 1m;
        bool hasWarning = false;
        bool hasConditional = false;
        bool hasDefinitiveIncompatibility = false;
        RepositorySnapshotIndex sourceIndex = request.Source == null ?
            new RepositorySnapshotIndex() : RepositorySnapshotIndexer.BuildIndex(request.Source);
        RepositorySnapshotIndex targetIndex = request.Target == null ?
            new RepositorySnapshotIndex() : RepositorySnapshotIndexer.BuildIndex(request.Target);

        int differenceIndex = 0;
        while (differenceIndex < difference.Differences.Count)
        {
            DifferenceItem item = difference.Differences[differenceIndex];
            if (item.Kind != DifferenceKind.Unchanged)
            {
                CompatibilityImpact impact = ClassifyDifference(item, sourceIndex, targetIndex);
                assessment.ReaderCompatible = assessment.ReaderCompatible && impact.ReaderCompatible;
                assessment.WriterCompatible = assessment.WriterCompatible && impact.WriterCompatible;
                assessment.SchemaCompatible = assessment.SchemaCompatible && impact.SchemaCompatible;
                assessment.Lossless = assessment.Lossless && impact.Lossless;
                assessment.SemanticallyCompatible =
                    assessment.SemanticallyCompatible && impact.SemanticallyCompatible;
                hasWarning = hasWarning || impact.Warning;
                hasConditional = hasConditional || impact.Conditional;
                hasDefinitiveIncompatibility =
                    hasDefinitiveIncompatibility || impact.DefinitiveIncompatibility;
                decimal confidence = impact.Confidence;
                if (item.MatchConfidence > 0m && item.MatchConfidence < confidence)
                {
                    confidence = item.MatchConfidence;
                }

                if (confidence < assessment.Confidence)
                {
                    assessment.Confidence = confidence;
                }

                item.BreakingChange = !impact.ReaderCompatible ||
                    !impact.WriterCompatible ||
                    !impact.SchemaCompatible ||
                    !impact.SemanticallyCompatible;
                item.DataLossRisk = !impact.Lossless;
                assessment.Differences.Add(item);
                AddAffectedPath(assessment, item);
                assessment.Rationale.Add(FormatRationale(item, impact.Rationale));
            }

            differenceIndex++;
        }

        if (difference.Truncated)
        {
            hasConditional = true;
            if (assessment.Confidence > 0.50m)
            {
                assessment.Confidence = 0.50m;
            }

            assessment.Rationale.Add(
                "Compatibility is conditional because resource limits truncated the difference list.");
        }

        if (assessment.Differences.Count == 0 && !difference.Truncated)
        {
            assessment.Kind = CompatibilityKind.Compatible;
            assessment.Rationale.Add("No material difference affects compatibility.");
        }
        else if (!assessment.Lossless)
        {
            assessment.Kind = CompatibilityKind.DataLoss;
        }
        else if (hasDefinitiveIncompatibility)
        {
            assessment.Kind = CompatibilityKind.Incompatible;
        }
        else if (hasConditional)
        {
            assessment.Kind = CompatibilityKind.ConditionallyCompatible;
        }
        else if (hasWarning)
        {
            assessment.Kind = CompatibilityKind.CompatibleWithWarnings;
        }
        else
        {
            assessment.Kind = CompatibilityKind.Compatible;
        }

        difference.Compatibility.Add(assessment);
    }

    internal static CompatibilityImpact ClassifyDifference(
        DifferenceItem difference,
        RepositorySnapshotIndex sourceIndex,
        RepositorySnapshotIndex targetIndex)
    {
        if (difference.Kind == DifferenceKind.Added)
        {
            return ClassifyAddedEntity(difference, targetIndex);
        }

        if (difference.Kind == DifferenceKind.Removed)
        {
            return ClassifyRemovedEntity(difference, sourceIndex);
        }

        if (difference.Kind == DifferenceKind.Renamed ||
            difference.Kind == DifferenceKind.Moved)
        {
            return CreateStructuralImpact(
                false,
                false,
                true,
                "The XML or repository location changed; explicit lossless remapping is required.");
        }

        if (difference.Kind == DifferenceKind.TypeChanged)
        {
            CompatibilityImpact impact = CreateStructuralImpact(
                false,
                false,
                false,
                "The member type changed and conversion may reject or lose values.");
            impact.Lossless = false;
            return impact;
        }

        if (difference.Kind == DifferenceKind.CardinalityChanged)
        {
            return ClassifyCardinality(difference);
        }

        if (difference.Kind == DifferenceKind.FacetChanged)
        {
            CompatibilityImpact impact = CreateStructuralImpact(
                false,
                false,
                true,
                "The accepted lexical or value space changed; direction cannot be proven lossless.");
            impact.Lossless = false;
            impact.Confidence = 0.85m;
            return impact;
        }

        if (difference.Kind == DifferenceKind.CodeAdded)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.ReaderCompatible = false;
            impact.SchemaCompatible = false;
            impact.SemanticallyCompatible = false;
            impact.DefinitiveIncompatibility = true;
            impact.Rationale =
                "Target messages may contain a code unknown to source readers, while source writers remain valid.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.CodeRemoved)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.WriterCompatible = false;
            impact.SchemaCompatible = false;
            impact.Lossless = false;
            impact.SemanticallyCompatible = false;
            impact.DefinitiveIncompatibility = true;
            impact.Rationale =
                "Source writers may emit a code rejected by the target and migration requires dropping or mapping it.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.CodeChanged)
        {
            CompatibilityImpact impact = CreateStructuralImpact(
                false,
                false,
                false,
                "A code's lexical value or meaning changed and requires an explicit semantic mapping.");
            impact.Lossless = false;
            return impact;
        }

        if (difference.Kind == DifferenceKind.ConstraintAdded)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.WriterCompatible = false;
            impact.SemanticallyCompatible = false;
            impact.Conditional = true;
            impact.Confidence = 0.80m;
            impact.Rationale =
                "Existing source writers are compatible only when their output satisfies the new target constraint.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.ConstraintRemoved)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.ReaderCompatible = false;
            impact.SemanticallyCompatible = false;
            impact.Conditional = true;
            impact.Confidence = 0.80m;
            impact.Rationale =
                "Target messages may contain values that violate the removed source constraint.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.ConstraintChanged)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.ReaderCompatible = false;
            impact.WriterCompatible = false;
            impact.SemanticallyCompatible = false;
            impact.Conditional = true;
            impact.Confidence = 0.75m;
            impact.Rationale =
                "Constraint compatibility depends on the values admitted by both expressions.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.RegistrationChanged)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.Warning = true;
            impact.Confidence = 0.95m;
            impact.Rationale =
                "Lifecycle metadata changed without changing structural acceptance.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.SyntaxChanged)
        {
            return CreateStructuralImpact(
                false,
                false,
                false,
                "Syntax or encoding changes require updated readers and writers.");
        }

        if (difference.Kind == DifferenceKind.TransportChanged)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.Conditional = true;
            impact.Confidence = 0.85m;
            impact.Rationale =
                "Message compatibility depends on whether the deployment satisfies the changed transport conditions.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.AmbiguousMatch)
        {
            CompatibilityImpact impact = new CompatibilityImpact();
            impact.ReaderCompatible = false;
            impact.WriterCompatible = false;
            impact.SchemaCompatible = false;
            impact.SemanticallyCompatible = false;
            impact.Conditional = true;
            impact.Confidence = 0.50m;
            impact.Rationale =
                "Compatibility cannot be resolved until the ambiguous entity match is overridden.";
            return impact;
        }

        if (difference.Kind == DifferenceKind.RelationshipAdded ||
            difference.Kind == DifferenceKind.RelationshipRemoved ||
            difference.Kind == DifferenceKind.RelationshipChanged)
        {
            return ClassifyRelationship(difference);
        }

        CompatibilityImpact modified = new CompatibilityImpact();
        if (difference.PropertyName == "Definition")
        {
            modified.SemanticallyCompatible = false;
            modified.Conditional = true;
            modified.Confidence = 0.75m;
            modified.Rationale =
                "Business meaning changed and requires semantic review.";
        }
        else
        {
            modified.ReaderCompatible = false;
            modified.WriterCompatible = false;
            modified.SchemaCompatible = false;
            modified.SemanticallyCompatible = false;
            modified.DefinitiveIncompatibility = true;
            modified.Confidence = 0.85m;
            modified.Rationale =
                "Repository metadata affecting serialized or logical values changed.";
        }

        return modified;
    }

    internal static CompatibilityImpact ClassifyAddedEntity(
        DifferenceItem difference,
        RepositorySnapshotIndex targetIndex)
    {
        if (!IsStructuralEntity(difference.EntityKind))
        {
            CompatibilityImpact warning = new CompatibilityImpact();
            warning.Warning = true;
            warning.Rationale =
                "The target adds a repository entity without changing existing serialized content.";
            return warning;
        }

        OccurrenceInfo occurrence = ResolveOccurrence(targetIndex, difference.TargetIdentifier);
        CompatibilityImpact impact = new CompatibilityImpact();
        impact.ReaderCompatible = false;
        impact.SchemaCompatible = false;
        impact.DefinitiveIncompatibility = true;
        if (occurrence.Available && occurrence.MinimumOccurs > 0)
        {
            impact.WriterCompatible = false;
            impact.SemanticallyCompatible = false;
            impact.Rationale =
                "The target adds required content that source readers reject and source writers cannot supply.";
        }
        else
        {
            impact.Rationale =
                "The target adds optional content that source readers may reject; source writers remain acceptable.";
        }

        return impact;
    }

    internal static CompatibilityImpact ClassifyRemovedEntity(
        DifferenceItem difference,
        RepositorySnapshotIndex sourceIndex)
    {
        CompatibilityImpact impact = new CompatibilityImpact();
        impact.WriterCompatible = false;
        impact.Lossless = false;
        impact.SemanticallyCompatible = false;
        impact.DefinitiveIncompatibility = true;
        if (IsStructuralEntity(difference.EntityKind))
        {
            OccurrenceInfo occurrence = ResolveOccurrence(sourceIndex, difference.SourceIdentifier);
            impact.SchemaCompatible = false;
            if (!occurrence.Available || occurrence.MinimumOccurs > 0)
            {
                impact.ReaderCompatible = false;
            }

            impact.Rationale =
                "Source content was removed; source writers may emit rejected data and migration can discard it.";
        }
        else
        {
            impact.Rationale =
                "A source repository concept was removed and cannot be retained without mapping or loss.";
        }

        return impact;
    }

    internal static CompatibilityImpact ClassifyCardinality(DifferenceItem difference)
    {
        OccurrenceInfo source = ParseOccurrence(difference.SourceValue);
        OccurrenceInfo target = ParseOccurrence(difference.TargetValue);
        CompatibilityImpact impact = new CompatibilityImpact();
        impact.SchemaCompatible = false;
        impact.DefinitiveIncompatibility = true;
        if (!source.Available || !target.Available)
        {
            impact.ReaderCompatible = false;
            impact.WriterCompatible = false;
            impact.Lossless = false;
            impact.Confidence = 0.70m;
            impact.Rationale =
                "The occurrence range changed and its direction could not be classified safely.";
            return impact;
        }

        if (target.MinimumOccurs > source.MinimumOccurs)
        {
            impact.WriterCompatible = false;
        }

        if (target.MinimumOccurs < source.MinimumOccurs)
        {
            impact.ReaderCompatible = false;
        }

        if (MaximumIsGreater(target, source))
        {
            impact.ReaderCompatible = false;
        }

        if (MaximumIsGreater(source, target))
        {
            impact.WriterCompatible = false;
            impact.Lossless = false;
        }

        impact.Rationale =
            "The occurrence range changed directional reader or writer acceptance.";
        return impact;
    }

    internal static CompatibilityImpact ClassifyRelationship(DifferenceItem difference)
    {
        CompatibilityImpact impact = new CompatibilityImpact();
        if (IsStructuralEntity(difference.EntityKind))
        {
            impact.ReaderCompatible = false;
            impact.WriterCompatible = false;
            impact.SchemaCompatible = false;
            impact.DefinitiveIncompatibility = true;
            impact.Rationale =
                "A structural message relationship changed serialized content acceptance.";
        }
        else
        {
            impact.Conditional = true;
            impact.Confidence = 0.80m;
            impact.Rationale =
                "Compatibility depends on the role of the changed repository relationship.";
        }

        return impact;
    }

    internal static CompatibilityImpact CreateStructuralImpact(
        bool readerCompatible,
        bool writerCompatible,
        bool semanticallyCompatible,
        string rationale)
    {
        CompatibilityImpact impact = new CompatibilityImpact();
        impact.ReaderCompatible = readerCompatible;
        impact.WriterCompatible = writerCompatible;
        impact.SchemaCompatible = false;
        impact.SemanticallyCompatible = semanticallyCompatible;
        impact.DefinitiveIncompatibility = true;
        impact.Rationale = rationale;
        return impact;
    }

    internal static bool IsStructuralEntity(RepositoryEntityKind kind)
    {
        return kind == RepositoryEntityKind.MessageDefinition ||
            kind == RepositoryEntityKind.MessageBuildingBlock ||
            kind == RepositoryEntityKind.MessageComponent ||
            kind == RepositoryEntityKind.MessageAttribute ||
            kind == RepositoryEntityKind.MessageAssociationEnd ||
            kind == RepositoryEntityKind.MessageElement ||
            kind == RepositoryEntityKind.Choice ||
            kind == RepositoryEntityKind.DataType ||
            kind == RepositoryEntityKind.CodeSet ||
            kind == RepositoryEntityKind.ExternalSchema;
    }

    internal static OccurrenceInfo ResolveOccurrence(
        RepositorySnapshotIndex index,
        string identifier)
    {
        OccurrenceInfo result = new OccurrenceInfo();
        List<RepositoryIndexEntry> entries = index.FindIdentifier(identifier);
        if (entries.Count != 1)
        {
            return result;
        }

        MessageBuildingBlockDefinition? block =
            entries[0].Value as MessageBuildingBlockDefinition;
        if (block != null)
        {
            CopyMultiplicity(result, block.Multiplicity);
            return result;
        }

        MessageElementDefinition? messageElement =
            entries[0].Value as MessageElementDefinition;
        if (messageElement != null)
        {
            CopyMultiplicity(result, messageElement.Multiplicity);
            return result;
        }

        ChoiceDefinition? choice = entries[0].Value as ChoiceDefinition;
        if (choice != null)
        {
            CopyMultiplicity(result, choice.Multiplicity);
            return result;
        }

        BusinessElementDefinition? businessElement =
            entries[0].Value as BusinessElementDefinition;
        if (businessElement != null)
        {
            CopyMultiplicity(result, businessElement.Multiplicity);
            return result;
        }

        ExternalSchemaDefinition? external = entries[0].Value as ExternalSchemaDefinition;
        if (external != null)
        {
            CopyMultiplicity(result, external.Multiplicity);
            return result;
        }

        ParticipantDefinition? participant = entries[0].Value as ParticipantDefinition;
        if (participant != null)
        {
            result.Available = true;
            result.MinimumOccurs = participant.MinimumOccurs;
            result.HasMaximumOccurs = participant.HasMaximumOccurs;
            result.MaximumOccurs = participant.MaximumOccurs;
        }

        return result;
    }

    internal static void CopyMultiplicity(
        OccurrenceInfo destination,
        MultiplicityDefinition source)
    {
        destination.Available = true;
        destination.MinimumOccurs = source.MinimumOccurs;
        destination.HasMaximumOccurs = source.HasMaximumOccurs;
        destination.MaximumOccurs = source.MaximumOccurs;
    }

    internal static OccurrenceInfo ParseOccurrence(string value)
    {
        OccurrenceInfo result = new OccurrenceInfo();
        string[] parts = value.Split(new char[] { '|' });
        if (parts.Length != 3 ||
            (parts[1] != "true" && parts[1] != "false"))
        {
            return result;
        }

        int minimum = ParseNonNegativeInteger(parts[0]);
        int maximum = ParseNonNegativeInteger(parts[2]);
        if (minimum < 0 || maximum < 0)
        {
            return result;
        }

        result.Available = true;
        result.MinimumOccurs = minimum;
        result.HasMaximumOccurs = parts[1] == "true";
        result.MaximumOccurs = maximum;
        return result;
    }

    internal static int ParseNonNegativeInteger(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            if (result > 214748364 ||
                (result == 214748364 && digit > 7))
            {
                return -1;
            }

            result = (result * 10) + digit;
            index++;
        }

        return result;
    }

    internal static bool MaximumIsGreater(
        OccurrenceInfo first,
        OccurrenceInfo second)
    {
        if (!first.HasMaximumOccurs)
        {
            return second.HasMaximumOccurs;
        }

        if (!second.HasMaximumOccurs)
        {
            return false;
        }

        return first.MaximumOccurs > second.MaximumOccurs;
    }

    internal static void AddAffectedPath(
        CompatibilityAssessment assessment,
        DifferenceItem difference)
    {
        XmlPathData path = difference.TargetPath.Value.Length > 0 ?
            difference.TargetPath : difference.SourcePath;
        if (path.Value.Length == 0)
        {
            return;
        }

        int pathIndex = 0;
        while (pathIndex < assessment.AffectedPaths.Count)
        {
            if (assessment.AffectedPaths[pathIndex].Value == path.Value)
            {
                return;
            }

            pathIndex++;
        }

        assessment.AffectedPaths.Add(path);
    }

    internal static string FormatRationale(
        DifferenceItem difference,
        string rationale)
    {
        string path = difference.TargetPath.Value.Length > 0 ?
            difference.TargetPath.Value : difference.SourcePath.Value;
        if (path.Length == 0)
        {
            return rationale;
        }

        return path + ": " + rationale;
    }
}
