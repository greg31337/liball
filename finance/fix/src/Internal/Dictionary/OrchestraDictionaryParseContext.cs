using System.Collections.Generic;

namespace fix;

internal class OrchestraDictionaryParseContext
{
    internal FixDictionary Dictionary;
    internal FixDictionaryOptions Options;
    internal string NamespaceName = "";
    internal Dictionary<string, FixDatatypeDefinition> DatatypesByName = new Dictionary<string, FixDatatypeDefinition>(global::System.StringComparer.Ordinal);
    internal Dictionary<string, FixCodeSetDefinition> CodeSetsByName = new Dictionary<string, FixCodeSetDefinition>(global::System.StringComparer.Ordinal);
    internal Dictionary<int, FixFieldDefinition> FieldsById = new Dictionary<int, FixFieldDefinition>();
    internal Dictionary<int, FixComponentDefinition> ComponentsById = new Dictionary<int, FixComponentDefinition>();
    internal Dictionary<int, FixGroupDefinition> GroupsById = new Dictionary<int, FixGroupDefinition>();
    internal Dictionary<string, FixScenarioDefinition> ScenariosByName = new Dictionary<string, FixScenarioDefinition>(global::System.StringComparer.Ordinal);
    internal List<int> EnvelopeComponentIds = [];
    internal bool HasDeferredScenarioDefinitions;

    internal OrchestraDictionaryParseContext(FixDictionary dictionary, FixDictionaryOptions options)
    {
        Dictionary = dictionary;
        Options = options;
    }
}
