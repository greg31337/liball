using System;
using System.Collections.Generic;

namespace iso20022;

internal static class RepositoryIntegrityChecker
{
    internal static RepositoryIntegrityResult ValidateSnapshot(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index,
        int maximumDiagnosticCount)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException("snapshot");
        }

        if (index == null)
        {
            throw new ArgumentNullException("index");
        }

        if (maximumDiagnosticCount <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumDiagnosticCount");
        }

        RepositoryIntegrityResult result = new RepositoryIntegrityResult();
        result.InspectedEntityCount = index.Entries.Count;
        result.InspectedReferenceCount = snapshot.References.Count;
        ValidateIdentifiers(index, result, maximumDiagnosticCount);
        ValidateMessageDefinitions(snapshot, index, result, maximumDiagnosticCount);
        ValidateMultiplicities(snapshot, result, maximumDiagnosticCount);
        ValidateReferences(snapshot, index, result, maximumDiagnosticCount);
        result.IsValid = result.Diagnostics.Count == 0;
        return result;
    }

    internal static void ValidateIdentifiers(
        RepositorySnapshotIndex index,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        int entryIndex = 0;
        while (entryIndex < index.Entries.Count)
        {
            RepositoryIndexEntry entry = index.Entries[entryIndex];
            if (entry.Identifier.Length == 0)
            {
                if (entry.Kind != RepositoryEntityKind.Unknown)
                {
                    AddDiagnostic(
                        result,
                        maximumDiagnosticCount,
                        "repository.integrity.identifier_missing",
                        "A normalized repository entity does not have an identifier.",
                        entry.Identifier,
                        entry.Kind,
                        string.Empty,
                        entry.Location);
                }
            }
            else if (seen.Contains(entry.Identifier))
            {
                AddDiagnostic(
                    result,
                    maximumDiagnosticCount,
                    "repository.integrity.identifier_duplicate",
                    "A normalized repository identifier occurs more than once.",
                    entry.Identifier,
                    entry.Kind,
                    entry.Identifier,
                    entry.Location);
            }
            else
            {
                seen.Add(entry.Identifier);
            }

            entryIndex++;
        }
    }

    internal static void ValidateMessageDefinitions(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        HashSet<string> messageIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        int messageIndex = 0;
        while (messageIndex < snapshot.MessageDefinitions.Count)
        {
            MessageDefinitionMetadata message = snapshot.MessageDefinitions[messageIndex];
            string value = message.MessageIdentifier.Value;
            MessageIdentifier? parsed = MessageIdentifierLexical.ParseMessageIdentifier(
                value.AsSpan());
            if (parsed == null)
            {
                AddDiagnostic(
                    result,
                    maximumDiagnosticCount,
                    "repository.integrity.message_identifier_invalid",
                    "A message definition does not contain a valid four-part message identifier.",
                    message.Identifier,
                    RepositoryEntityKind.MessageDefinition,
                    value,
                    message.Location);
            }
            else
            {
                if (messageIdentifiers.Contains(value))
                {
                    AddDiagnostic(
                        result,
                        maximumDiagnosticCount,
                        "repository.integrity.message_identifier_duplicate",
                        "A message identifier occurs on more than one message definition.",
                        message.Identifier,
                        RepositoryEntityKind.MessageDefinition,
                        value,
                        message.Location);
                }
                else
                {
                    messageIdentifiers.Add(value);
                }

                string expectedNamespace = Iso20022Namespaces.SchemaUrnPrefix + value;
                if (message.NamespaceUri != expectedNamespace)
                {
                    AddDiagnostic(
                        result,
                        maximumDiagnosticCount,
                        "repository.integrity.message_namespace_mismatch",
                        "A message definition namespace does not match its message identifier.",
                        message.Identifier,
                        RepositoryEntityKind.MessageDefinition,
                        message.NamespaceUri,
                        message.Location);
                }

                ValidateMessageBusinessArea(
                    message,
                    index,
                    result,
                    maximumDiagnosticCount);
            }

            messageIndex++;
        }
    }

    internal static void ValidateMessageBusinessArea(
        MessageDefinitionMetadata message,
        RepositorySnapshotIndex index,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        if (message.BusinessArea.TargetIdentifier.Length == 0)
        {
            return;
        }

        List<RepositoryIndexEntry> areas =
            index.FindIdentifier(message.BusinessArea.TargetIdentifier);
        if (areas.Count == 1 &&
            areas[0].Kind == RepositoryEntityKind.BusinessArea &&
            areas[0].BusinessAreaCode.Length > 0 &&
            areas[0].BusinessAreaCode != message.MessageIdentifier.BusinessArea)
        {
            AddDiagnostic(
                result,
                maximumDiagnosticCount,
                "repository.integrity.message_business_area_mismatch",
                "A message identifier business-area code does not match its owning business area.",
                message.Identifier,
                RepositoryEntityKind.MessageDefinition,
                message.BusinessArea.TargetIdentifier,
                message.Location);
        }
    }

    internal static void ValidateMultiplicities(
        RepositorySnapshot snapshot,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        int itemIndex = 0;
        while (itemIndex < snapshot.Participants.Count)
        {
            ParticipantDefinition item = snapshot.Participants[itemIndex];
            ValidateMultiplicityValues(
                item.Identifier,
                RepositoryEntityKind.Participant,
                item.MinimumOccurs,
                item.HasMaximumOccurs,
                item.MaximumOccurs,
                item.Location,
                result,
                maximumDiagnosticCount);
            itemIndex++;
        }

        itemIndex = 0;
        while (itemIndex < snapshot.BusinessElements.Count)
        {
            BusinessElementDefinition item = snapshot.BusinessElements[itemIndex];
            ValidateMultiplicityValues(
                item.Identifier,
                RepositoryEntityKind.BusinessElement,
                item.Multiplicity.MinimumOccurs,
                item.Multiplicity.HasMaximumOccurs,
                item.Multiplicity.MaximumOccurs,
                item.Location,
                result,
                maximumDiagnosticCount);
            itemIndex++;
        }

        itemIndex = 0;
        while (itemIndex < snapshot.MessageBuildingBlocks.Count)
        {
            MessageBuildingBlockDefinition item = snapshot.MessageBuildingBlocks[itemIndex];
            ValidateMultiplicityValues(
                item.Identifier,
                RepositoryEntityKind.MessageBuildingBlock,
                item.Multiplicity.MinimumOccurs,
                item.Multiplicity.HasMaximumOccurs,
                item.Multiplicity.MaximumOccurs,
                item.Location,
                result,
                maximumDiagnosticCount);
            itemIndex++;
        }

        itemIndex = 0;
        while (itemIndex < snapshot.MessageElements.Count)
        {
            MessageElementDefinition item = snapshot.MessageElements[itemIndex];
            ValidateMultiplicityValues(
                item.Identifier,
                RepositoryEntityKind.MessageElement,
                item.Multiplicity.MinimumOccurs,
                item.Multiplicity.HasMaximumOccurs,
                item.Multiplicity.MaximumOccurs,
                item.Location,
                result,
                maximumDiagnosticCount);
            itemIndex++;
        }
    }

    internal static void ValidateMultiplicityValues(
        string identifier,
        RepositoryEntityKind kind,
        int minimumOccurs,
        bool hasMaximumOccurs,
        int maximumOccurs,
        SourceLocation location,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        if (minimumOccurs < 0)
        {
            AddDiagnostic(
                result,
                maximumDiagnosticCount,
                "repository.integrity.multiplicity_minimum_negative",
                "A repository multiplicity has a negative minimum occurrence count.",
                identifier,
                kind,
                minimumOccurs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                location);
        }

        if (hasMaximumOccurs && maximumOccurs < minimumOccurs)
        {
            AddDiagnostic(
                result,
                maximumDiagnosticCount,
                "repository.integrity.multiplicity_range_invalid",
                "A repository multiplicity maximum is less than its minimum.",
                identifier,
                kind,
                maximumOccurs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                location);
        }
    }

    internal static void ValidateReferences(
        RepositorySnapshot snapshot,
        RepositorySnapshotIndex index,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        int referenceIndex = 0;
        while (referenceIndex < snapshot.References.Count)
        {
            RepositoryReference reference = snapshot.References[referenceIndex];
            if (reference.Position < 0)
            {
                AddDiagnostic(
                    result,
                    maximumDiagnosticCount,
                    "repository.integrity.reference_position_negative",
                    "A repository reference has a negative relationship position.",
                    reference.SourceIdentifier,
                    reference.SourceKind,
                    reference.TargetIdentifier,
                    reference.Location);
            }

            if (!reference.IsResolved)
            {
                string code = reference.ErrorCode;
                if (code.Length == 0)
                {
                    code = "repository.integrity.reference_unresolved";
                }

                string message = reference.ErrorMessage;
                if (message.Length == 0)
                {
                    message = "A repository reference target is unresolved.";
                }

                AddDiagnostic(
                    result,
                    maximumDiagnosticCount,
                    code,
                    message,
                    reference.SourceIdentifier,
                    reference.SourceKind,
                    reference.TargetIdentifier,
                    reference.Location);
            }
            else
            {
                ValidateResolvedReference(
                    reference,
                    index,
                    result,
                    maximumDiagnosticCount);
            }

            referenceIndex++;
        }
    }

    internal static void ValidateResolvedReference(
        RepositoryReference reference,
        RepositorySnapshotIndex index,
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount)
    {
        if (reference.TargetIdentifier.Length == 0)
        {
            if (!reference.IsContainment)
            {
                AddDiagnostic(
                    result,
                    maximumDiagnosticCount,
                    "repository.integrity.reference_target_missing",
                    "A resolved repository reference does not contain a target identifier.",
                    reference.SourceIdentifier,
                    reference.SourceKind,
                    string.Empty,
                    reference.Location);
            }

            return;
        }

        List<RepositoryIndexEntry> targets = index.FindIdentifier(reference.TargetIdentifier);
        if (targets.Count == 1 &&
            reference.TargetKind != RepositoryEntityKind.Unknown &&
            targets[0].Kind != RepositoryEntityKind.Unknown &&
            targets[0].Kind != reference.TargetKind)
        {
            AddDiagnostic(
                result,
                maximumDiagnosticCount,
                "repository.integrity.reference_kind_mismatch",
                "A resolved repository reference target kind does not match the indexed entity.",
                reference.SourceIdentifier,
                reference.SourceKind,
                reference.TargetIdentifier,
                reference.Location);
        }
    }

    internal static void AddDiagnostic(
        RepositoryIntegrityResult result,
        int maximumDiagnosticCount,
        string code,
        string message,
        string identifier,
        RepositoryEntityKind kind,
        string relatedIdentifier,
        SourceLocation location)
    {
        if (result.Diagnostics.Count >= maximumDiagnosticCount)
        {
            throw new InvalidOperationException(
                "The repository integrity diagnostics exceed maximumDiagnosticCount.");
        }

        RepositoryIntegrityDiagnostic diagnostic = new RepositoryIntegrityDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Identifier = identifier;
        diagnostic.Kind = kind;
        diagnostic.RelatedIdentifier = relatedIdentifier;
        diagnostic.Order = result.Diagnostics.Count;
        diagnostic.Location = location;
        result.Diagnostics.Add(diagnostic);
    }
}
