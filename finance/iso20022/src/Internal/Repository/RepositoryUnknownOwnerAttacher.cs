namespace iso20022;

internal static class RepositoryUnknownOwnerAttacher
{
    internal static void AttachUnknownContent(
        RepositorySnapshot snapshot,
        RepositoryUnknownData unknown)
    {
        if (unknown.OwnerIdentifier.Length == 0)
        {
            return;
        }

        int index = 0;
        while (index < snapshot.Constraints.Count)
        {
            if (snapshot.Constraints[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Constraints[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.BusinessAreas.Count)
        {
            if (snapshot.BusinessAreas[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.BusinessAreas[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.BusinessProcesses.Count)
        {
            if (snapshot.BusinessProcesses[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.BusinessProcesses[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.BusinessRoles.Count)
        {
            if (snapshot.BusinessRoles[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.BusinessRoles[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.Participants.Count)
        {
            if (snapshot.Participants[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Participants[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.BusinessTransactions.Count)
        {
            if (snapshot.BusinessTransactions[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.BusinessTransactions[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.Conversations.Count)
        {
            if (snapshot.Conversations[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Conversations[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageChoreographies.Count)
        {
            if (snapshot.MessageChoreographies[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageChoreographies[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageTransmissions.Count)
        {
            if (snapshot.MessageTransmissions[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageTransmissions[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessagingEndpoints.Count)
        {
            if (snapshot.MessagingEndpoints[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessagingEndpoints[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.Transports.Count)
        {
            if (snapshot.Transports[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Transports[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.BusinessComponents.Count)
        {
            if (snapshot.BusinessComponents[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.BusinessComponents[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.BusinessElements.Count)
        {
            if (snapshot.BusinessElements[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.BusinessElements[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageSets.Count)
        {
            if (snapshot.MessageSets[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageSets[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageDefinitions.Count)
        {
            if (snapshot.MessageDefinitions[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageDefinitions[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageBuildingBlocks.Count)
        {
            if (snapshot.MessageBuildingBlocks[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageBuildingBlocks[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageComponents.Count)
        {
            if (snapshot.MessageComponents[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageComponents[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.MessageElements.Count)
        {
            if (snapshot.MessageElements[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.MessageElements[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.Choices.Count)
        {
            if (snapshot.Choices[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Choices[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.DataTypes.Count)
        {
            if (snapshot.DataTypes[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.DataTypes[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.CodeSets.Count)
        {
            CodeSetDefinition codeSet = snapshot.CodeSets[index];
            if (codeSet.Identifier == unknown.OwnerIdentifier)
            {
                codeSet.UnknownContent.Add(unknown);
                return;
            }

            int codeIndex = 0;
            while (codeIndex < codeSet.Codes.Count)
            {
                if (codeSet.Codes[codeIndex].Identifier == unknown.OwnerIdentifier)
                {
                    codeSet.Codes[codeIndex].UnknownContent.Add(unknown);
                    return;
                }

                codeIndex++;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.Syntaxes.Count)
        {
            if (snapshot.Syntaxes[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Syntaxes[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.Encodings.Count)
        {
            if (snapshot.Encodings[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.Encodings[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.ExternalSchemas.Count)
        {
            if (snapshot.ExternalSchemas[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.ExternalSchemas[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.SyntaxMessageSchemes.Count)
        {
            if (snapshot.SyntaxMessageSchemes[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.SyntaxMessageSchemes[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }

        index = 0;
        while (index < snapshot.ConvergenceDefinitions.Count)
        {
            if (snapshot.ConvergenceDefinitions[index].Identifier == unknown.OwnerIdentifier)
            {
                snapshot.ConvergenceDefinitions[index].UnknownContent.Add(unknown);
                return;
            }

            index++;
        }
    }
}
