namespace fix;

/// <summary>Performs deterministic lookups in a canonical FIX dictionary.</summary>
public interface IDictionaryQuery
{
    /// <summary>Finds a field definition by tag.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="tag">The field tag.</param>
    /// <returns>The field definition, or <c>null</c> when absent.</returns>
    FixFieldDefinition? ResolveFieldDefinition(FixDictionary dictionary, int tag);
    /// <summary>Finds a message definition by MsgType and scenario.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="msgType">The exact MsgType.</param>
    /// <param name="scenario">The scenario name.</param>
    /// <returns>The message definition, or <c>null</c> when absent.</returns>
    FixMessageDefinition? ResolveMessageDefinition(FixDictionary dictionary, string msgType, string scenario);
    /// <summary>Finds a component definition by identifier.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The component definition, or <c>null</c> when absent.</returns>
    FixComponentDefinition? ResolveComponentDefinition(FixDictionary dictionary, int componentId);
    /// <summary>Finds a group definition by identifier.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The group definition, or <c>null</c> when absent.</returns>
    FixGroupDefinition? ResolveGroupDefinition(FixDictionary dictionary, int groupId);
    /// <summary>Finds a code-set definition by name.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="name">The code-set name.</param>
    /// <returns>The code-set definition, or <c>null</c> when absent.</returns>
    FixCodeSetDefinition? ResolveCodeSetDefinition(FixDictionary dictionary, string name);
    /// <summary>Finds a scenario definition by name.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="name">The scenario name.</param>
    /// <returns>The scenario definition, or <c>null</c> when absent.</returns>
    FixScenarioDefinition? ResolveScenarioDefinition(FixDictionary dictionary, string name);
}
