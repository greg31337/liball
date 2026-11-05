using System.Collections.Generic;

namespace iso20022;

internal static class MessageGraphSelector
{
    internal static MessageGraphSelection SelectMessageGraph(
        RepositorySnapshot snapshot,
        MessageIdentifier identifier,
        string role)
    {
        MessageGraphSelection selection = new MessageGraphSelection();
        string? formatted = MessageIdentifierLexical.FormatMessageIdentifier(identifier);
        if (formatted == null)
        {
            selection.ErrorCode = "comparison." + role + "_message_identifier_invalid";
            selection.ErrorMessage = "The " + role + " message identifier is not valid.";
            return selection;
        }

        int matchCount = 0;
        int messageIndex = 0;
        while (messageIndex < snapshot.MessageDefinitions.Count)
        {
            MessageDefinitionMetadata candidate = snapshot.MessageDefinitions[messageIndex];
            string? candidateIdentifier =
                MessageIdentifierLexical.FormatMessageIdentifier(candidate.MessageIdentifier);
            if (candidateIdentifier == formatted)
            {
                selection.Message = candidate;
                matchCount++;
            }

            messageIndex++;
        }

        if (matchCount == 0)
        {
            selection.Message = null;
            selection.ErrorCode = "comparison." + role + "_message_not_found";
            selection.ErrorMessage = "The exact " + role + " message version was not found.";
            return selection;
        }

        if (matchCount > 1)
        {
            selection.Message = null;
            selection.ErrorCode = "comparison." + role + "_message_ambiguous";
            selection.ErrorMessage = "The exact " + role + " message version is ambiguous.";
            return selection;
        }

        RepositorySnapshotIndex index = RepositorySnapshotIndexer.BuildIndex(snapshot);
        BuildReachableIdentifiers(selection, index);
        selection.Success = true;
        return selection;
    }

    internal static void BuildReachableIdentifiers(
        MessageGraphSelection selection,
        RepositorySnapshotIndex index)
    {
        if (selection.Message == null)
        {
            return;
        }

        List<string> pending = new List<string>();
        AddIdentifier(selection.Identifiers, pending, selection.Message.Identifier);
        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            string identifier = pending[pendingIndex];
            List<RepositoryIndexEntry> entries = index.FindIdentifier(identifier);
            if (entries.Count == 1)
            {
                AppendEntryReferences(selection.Identifiers, pending, entries[0]);
                RegistrationMetadata? registration = RepositoryGraphDiffer.ResolveRegistration(entries[0]);
                if (registration != null)
                {
                    int constraintIndex = 0;
                    while (constraintIndex < registration.Constraints.Count)
                    {
                        AddIdentifier(
                            selection.Identifiers,
                            pending,
                            registration.Constraints[constraintIndex].Identifier);
                        constraintIndex++;
                    }
                }
            }

            pendingIndex++;
        }
    }

    internal static void AppendEntryReferences(
        HashSet<string> identifiers,
        List<string> pending,
        RepositoryIndexEntry entry)
    {
        MessageDefinitionMetadata? message = entry.Value as MessageDefinitionMetadata;
        if (message != null)
        {
            AddReferences(identifiers, pending, message.BuildingBlocks);
            AddReferences(identifiers, pending, message.Choices);
            return;
        }

        MessageBuildingBlockDefinition? block = entry.Value as MessageBuildingBlockDefinition;
        if (block != null)
        {
            AddReference(identifiers, pending, block.MemberType);
            AddReference(identifiers, pending, block.SimpleType);
            AddReference(identifiers, pending, block.ComplexType);
            return;
        }

        MessageComponentDefinition? component = entry.Value as MessageComponentDefinition;
        if (component != null)
        {
            AddReferences(identifiers, pending, component.MessageElements);
            AddReferences(identifiers, pending, component.Choices);
            return;
        }

        MessageElementDefinition? element = entry.Value as MessageElementDefinition;
        if (element != null)
        {
            AddReference(identifiers, pending, element.MemberType);
            AddReference(identifiers, pending, element.SimpleType);
            AddReference(identifiers, pending, element.ComplexType);
            return;
        }

        ChoiceDefinition? choice = entry.Value as ChoiceDefinition;
        if (choice != null)
        {
            AddReferences(identifiers, pending, choice.MessageElements);
            return;
        }

        DataTypeDefinition? dataType = entry.Value as DataTypeDefinition;
        if (dataType != null)
        {
            AddReference(identifiers, pending, dataType.CurrencyIdentifierSet);
            return;
        }

        CodeSetDefinition? codeSet = entry.Value as CodeSetDefinition;
        if (codeSet != null)
        {
            AddReference(identifiers, pending, codeSet.BaseCodeSet);
            int codeIndex = 0;
            while (codeIndex < codeSet.Codes.Count)
            {
                AddIdentifier(identifiers, pending, codeSet.Codes[codeIndex].Identifier);
                codeIndex++;
            }
        }
    }

    internal static void AddReferences(
        HashSet<string> identifiers,
        List<string> pending,
        List<RepositoryReference> references)
    {
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            AddReference(identifiers, pending, references[referenceIndex]);
            referenceIndex++;
        }
    }

    internal static void AddReference(
        HashSet<string> identifiers,
        List<string> pending,
        RepositoryReference? reference)
    {
        if (reference != null)
        {
            AddIdentifier(identifiers, pending, reference.TargetIdentifier);
        }
    }

    internal static void AddIdentifier(
        HashSet<string> identifiers,
        List<string> pending,
        string identifier)
    {
        if (identifier.Length > 0 && identifiers.Add(identifier))
        {
            pending.Add(identifier);
        }
    }
}
