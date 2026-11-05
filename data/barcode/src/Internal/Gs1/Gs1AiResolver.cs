namespace barcode;

internal static class Gs1AiResolver
{
    internal static Gs1AiDefinition? ResolveDefinition(string? identifier)
    {
        if (identifier == null)
        {
            return null;
        }

        int nodeIndex = 0;
        int index = 0;
        while (index < identifier.Length)
        {
            char character = identifier[index];
            if (character < '0' || character > '9')
            {
                return null;
            }

            int childIndex = Gs1AiTable.TrieChildIndices[
                (nodeIndex * Gs1AiTable.TrieRadix) + character - '0'];
            if (childIndex < 0)
            {
                return null;
            }

            nodeIndex = childIndex;
            index = index + 1;
        }

        int definitionIndex = Gs1AiTable.TrieDefinitionIndices[nodeIndex];
        if (definitionIndex < 0)
        {
            return null;
        }

        return Gs1AiTable.Definitions[definitionIndex];
    }

    internal static bool MatchesPattern(
        string? identifier,
        string pattern)
    {
        if (identifier == null || identifier.Length != pattern.Length)
        {
            return false;
        }

        int index = 0;
        while (index < pattern.Length)
        {
            char expected = pattern[index];
            char observed = identifier[index];
            if (expected == 'n')
            {
                if (observed < '0' || observed > '9')
                {
                    return false;
                }
            }
            else if (expected != observed)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
