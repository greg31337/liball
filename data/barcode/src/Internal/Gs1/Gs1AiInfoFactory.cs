namespace barcode;

internal static class Gs1AiInfoFactory
{
    internal static Gs1ApplicationIdentifierInfo? Lookup(
        string? identifier)
    {
        Gs1AiDefinition? definition =
            Gs1AiResolver.ResolveDefinition(identifier);
        if (definition == null || identifier == null)
        {
            return null;
        }

        return Create(definition, identifier);
    }

    internal static Gs1ApplicationIdentifierInfo Create(
        Gs1AiDefinition definition,
        string identifier)
    {
        int minimumLength = CalculateMinimumLength(definition);
        int maximumLength = CalculateMaximumLength(definition);
        Gs1ApplicationIdentifierInfo result =
            new Gs1ApplicationIdentifierInfo();
        result.Identifier = identifier;
        result.Title = definition.Title;
        result.MinimumLength = minimumLength;
        result.MaximumLength = maximumLength;
        result.CharacterSet = CombineCharacterSets(definition);
        result.FixedLength = minimumLength == maximumLength;
        result.HasCheckCharacter = HasCheckCharacter(definition);
        result.IsPrimaryKey = definition.DigitalLinkPrimaryKey;
        result.RequiredIdentifiers = FlattenGroups(
            definition.RequiredAssociations);
        result.ExcludedIdentifiers = CopyStrings(definition.Exclusions);
        result.Qualifiers = FlattenGroups(
            definition.DigitalLinkQualifierSequences);
        return result;
    }

    static int CalculateMinimumLength(Gs1AiDefinition definition)
    {
        int length = 0;
        int index = 0;
        while (index < definition.Components.Length)
        {
            Gs1AiComponentDefinition component =
                definition.Components[index];
            if (!component.Optional)
            {
                length = length + component.MinimumLength;
            }

            index = index + 1;
        }

        return length;
    }

    static int CalculateMaximumLength(Gs1AiDefinition definition)
    {
        int length = 0;
        int index = 0;
        while (index < definition.Components.Length)
        {
            length = length
                + definition.Components[index].MaximumLength;
            index = index + 1;
        }

        return length;
    }

    static string CombineCharacterSets(Gs1AiDefinition definition)
    {
        string result = "";
        int index = 0;
        while (index < definition.Components.Length)
        {
            string characterSet =
                definition.Components[index].CharacterSet;
            if (result.Length == 0)
            {
                result = characterSet;
            }
            else if (result.IndexOf(characterSet) < 0)
            {
                result = result + "+" + characterSet;
            }

            index = index + 1;
        }

        return result;
    }

    static bool HasCheckCharacter(Gs1AiDefinition definition)
    {
        int componentIndex = 0;
        while (componentIndex < definition.Components.Length)
        {
            string[] linters =
                definition.Components[componentIndex].Linters;
            int linterIndex = 0;
            while (linterIndex < linters.Length)
            {
                if (linters[linterIndex] == "csum"
                    || linters[linterIndex] == "csumalpha")
                {
                    return true;
                }

                linterIndex = linterIndex + 1;
            }

            componentIndex = componentIndex + 1;
        }

        return false;
    }

    static string[] FlattenGroups(string[][] groups)
    {
        global::System.Collections.Generic.List<string> values =
            new global::System.Collections.Generic.List<string>();
        int groupIndex = 0;
        while (groupIndex < groups.Length)
        {
            string[] group = groups[groupIndex];
            int valueIndex = 0;
            while (valueIndex < group.Length)
            {
                string value = group[valueIndex];
                if (!values.Contains(value))
                {
                    values.Add(value);
                }

                valueIndex = valueIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }

        return values.ToArray();
    }

    static string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }
}
