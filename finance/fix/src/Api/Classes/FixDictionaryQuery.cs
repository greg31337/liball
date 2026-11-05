namespace fix;

/// <summary>Performs deterministic indexed lookups against mutable canonical dictionaries.</summary>
public class FixDictionaryQuery : IDictionaryQuery
{
    /// <summary>Creates a canonical dictionary query service.</summary>
    public FixDictionaryQuery()
    {
    }

    /// <summary>Finds a field definition by tag.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="tag">The field tag.</param>
    /// <returns>The first matching field definition, or <c>null</c> when absent.</returns>
    public FixFieldDefinition? ResolveFieldDefinition(FixDictionary dictionary, int tag)
    {
        DictionaryIndex index = BuildQueryIndex(dictionary);
        FixFieldDefinition? definition = index.FindFieldByTag(tag);
        int matches = CountFieldMatches(dictionary, tag);
        if (matches > 1)
        {
            AddQueryDiagnostic(dictionary, DuplicateSeverity(dictionary), "ambiguous-field-definition", "More than one field has the requested tag; the first definition was returned.", CreateIntegerRequest(tag), true, tag);
        }
        else if (definition == null)
        {
            AddQueryDiagnostic(dictionary, ValidationSeverity.Error, "missing-field-definition", "No field has the requested tag.", CreateIntegerRequest(tag), true, tag);
        }
        return definition;
    }

    /// <summary>Finds a message definition by MsgType and scenario.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="msgType">The exact MsgType.</param>
    /// <param name="scenario">The scenario name.</param>
    /// <returns>The first matching message definition, or <c>null</c> when absent.</returns>
    public FixMessageDefinition? ResolveMessageDefinition(FixDictionary dictionary, string msgType, string scenario)
    {
        DictionaryIndex index = BuildQueryIndex(dictionary);
        FixMessageDefinition? definition = index.FindMessageByType(msgType, scenario);
        int matches = CountMessageMatches(dictionary, msgType, scenario);
        string request = CreateMessageRequest(msgType, scenario, dictionary.Options.NormalizeNames);
        if (matches > 1)
        {
            AddQueryDiagnostic(dictionary, DuplicateSeverity(dictionary), "ambiguous-message-definition", "More than one message has the requested MsgType and scenario; the first definition was returned.", request, false, 0);
        }
        else if (definition == null)
        {
            AddQueryDiagnostic(dictionary, ValidationSeverity.Error, "missing-message-definition", "No message has the requested MsgType and scenario.", request, false, 0);
        }
        return definition;
    }

    /// <summary>Finds a component definition by identifier.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The first matching component definition, or <c>null</c> when absent.</returns>
    public FixComponentDefinition? ResolveComponentDefinition(FixDictionary dictionary, int componentId)
    {
        DictionaryIndex index = BuildQueryIndex(dictionary);
        FixComponentDefinition? definition = index.FindComponentById(componentId);
        int matches = CountComponentMatches(dictionary, componentId);
        if (matches > 1)
        {
            AddQueryDiagnostic(dictionary, DuplicateSeverity(dictionary), "ambiguous-component-definition", "More than one component has the requested identifier; the first definition was returned.", CreateIntegerRequest(componentId), false, 0);
        }
        else if (definition == null)
        {
            AddQueryDiagnostic(dictionary, ValidationSeverity.Error, "missing-component-definition", "No component has the requested identifier.", CreateIntegerRequest(componentId), false, 0);
        }
        return definition;
    }

    /// <summary>Finds a group definition by identifier.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The first matching group definition, or <c>null</c> when absent.</returns>
    public FixGroupDefinition? ResolveGroupDefinition(FixDictionary dictionary, int groupId)
    {
        DictionaryIndex index = BuildQueryIndex(dictionary);
        FixGroupDefinition? definition = index.FindGroupById(groupId);
        int matches = CountGroupMatches(dictionary, groupId);
        if (matches > 1)
        {
            AddQueryDiagnostic(dictionary, DuplicateSeverity(dictionary), "ambiguous-group-definition", "More than one group has the requested identifier; the first definition was returned.", CreateIntegerRequest(groupId), false, 0);
        }
        else if (definition == null)
        {
            AddQueryDiagnostic(dictionary, ValidationSeverity.Error, "missing-group-definition", "No group has the requested identifier.", CreateIntegerRequest(groupId), false, 0);
        }
        return definition;
    }

    /// <summary>Finds a code-set definition by name.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="name">The code-set name.</param>
    /// <returns>The first matching code-set definition, or <c>null</c> when absent.</returns>
    public FixCodeSetDefinition? ResolveCodeSetDefinition(FixDictionary dictionary, string name)
    {
        DictionaryIndex index = BuildQueryIndex(dictionary);
        FixCodeSetDefinition? definition = index.FindCodeSetByName(name);
        int matches = CountCodeSetMatches(dictionary, name);
        string request = DictionaryNormalizer.CreateNameKey(name, dictionary.Options.NormalizeNames);
        if (matches > 1)
        {
            AddQueryDiagnostic(dictionary, DuplicateSeverity(dictionary), "ambiguous-code-set-definition", "More than one code set has the requested name; the first definition was returned.", request, false, 0);
        }
        else if (definition == null)
        {
            AddQueryDiagnostic(dictionary, ValidationSeverity.Error, "missing-code-set-definition", "No code set has the requested name.", request, false, 0);
        }
        return definition;
    }

    /// <summary>Finds a scenario definition by name.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="name">The scenario name.</param>
    /// <returns>The first matching scenario definition, or <c>null</c> when absent.</returns>
    public FixScenarioDefinition? ResolveScenarioDefinition(FixDictionary dictionary, string name)
    {
        DictionaryIndex index = BuildQueryIndex(dictionary);
        FixScenarioDefinition? definition = index.FindScenarioByName(name);
        int matches = CountScenarioMatches(dictionary, name);
        string request = DictionaryNormalizer.CreateNameKey(name, dictionary.Options.NormalizeNames);
        if (matches > 1)
        {
            AddQueryDiagnostic(dictionary, DuplicateSeverity(dictionary), "ambiguous-scenario-definition", "More than one scenario has the requested name; the first definition was returned.", request, false, 0);
        }
        else if (definition == null)
        {
            AddQueryDiagnostic(dictionary, ValidationSeverity.Error, "missing-scenario-definition", "No scenario has the requested name.", request, false, 0);
        }
        return definition;
    }

    internal static DictionaryIndex BuildQueryIndex(FixDictionary dictionary)
    {
        return DictionaryNormalizer.CreateDictionaryIndex(dictionary, dictionary.Options);
    }

    internal static int CountFieldMatches(FixDictionary dictionary, int tag)
    {
        int matches = 0;
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            if (dictionary.Fields[index].Tag == tag)
            {
                matches++;
            }
        }
        return matches;
    }

    internal static int CountMessageMatches(FixDictionary dictionary, string msgType, string scenario)
    {
        int matches = 0;
        string scenarioKey = DictionaryNormalizer.CreateNameKey(scenario, dictionary.Options.NormalizeNames);
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            string definitionScenario = DictionaryNormalizer.CreateNameKey(definition.Scenario, dictionary.Options.NormalizeNames);
            if (definition.MsgType == msgType && definitionScenario == scenarioKey)
            {
                matches++;
            }
        }
        return matches;
    }

    internal static int CountComponentMatches(FixDictionary dictionary, int componentId)
    {
        int matches = 0;
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            if (dictionary.Components[index].Id == componentId)
            {
                matches++;
            }
        }
        return matches;
    }

    internal static int CountGroupMatches(FixDictionary dictionary, int groupId)
    {
        int matches = 0;
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            if (dictionary.Groups[index].Id == groupId)
            {
                matches++;
            }
        }
        return matches;
    }

    internal static int CountCodeSetMatches(FixDictionary dictionary, string name)
    {
        int matches = 0;
        string key = DictionaryNormalizer.CreateNameKey(name, dictionary.Options.NormalizeNames);
        for (int index = 0; index < dictionary.CodeSets.Count; index++)
        {
            string definitionKey = DictionaryNormalizer.CreateNameKey(dictionary.CodeSets[index].Name, dictionary.Options.NormalizeNames);
            if (definitionKey == key)
            {
                matches++;
            }
        }
        return matches;
    }

    internal static int CountScenarioMatches(FixDictionary dictionary, string name)
    {
        int matches = 0;
        string key = DictionaryNormalizer.CreateNameKey(name, dictionary.Options.NormalizeNames);
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            string definitionKey = DictionaryNormalizer.CreateNameKey(dictionary.Scenarios[index].Name, dictionary.Options.NormalizeNames);
            if (definitionKey == key)
            {
                matches++;
            }
        }
        return matches;
    }

    internal static ValidationSeverity DuplicateSeverity(FixDictionary dictionary)
    {
        return dictionary.Options.AllowDuplicateDefinitions ? ValidationSeverity.Warning : ValidationSeverity.Error;
    }

    internal static string CreateMessageRequest(string msgType, string scenario, bool normalizeNames)
    {
        string scenarioKey = DictionaryNormalizer.CreateNameKey(scenario, normalizeNames);
        return scenarioKey.Length == 0 ? msgType : msgType + ":" + scenarioKey;
    }

    internal static string CreateIntegerRequest(int value)
    {
        return value.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void AddQueryDiagnostic(FixDictionary dictionary, ValidationSeverity severity, string code, string message, string definitionName, bool hasTag, int tag)
    {
        for (int index = 0; index < dictionary.Diagnostics.Count; index++)
        {
            DictionaryDiagnostic existing = dictionary.Diagnostics[index];
            if (existing.Code == code && existing.DefinitionName == definitionName && existing.HasTag == hasTag && existing.Tag == tag)
            {
                return;
            }
        }
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.DefinitionName = definitionName;
        diagnostic.HasTag = hasTag;
        diagnostic.Tag = tag;
        dictionary.Diagnostics.Add(diagnostic);
    }
}
