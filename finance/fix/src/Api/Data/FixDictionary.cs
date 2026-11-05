using System.Collections.Generic;

namespace fix;

/// <summary>Represents a canonical, version-neutral FIX dictionary graph.</summary>
public class FixDictionary
{
    /// <summary>The dictionary identity.</summary>
    public FixDictionaryIdentity Identity = new FixDictionaryIdentity();
    /// <summary>The options used to load or compose the dictionary.</summary>
    public FixDictionaryOptions Options = new FixDictionaryOptions();
    /// <summary>The datatype definitions in deterministic order.</summary>
    public List<FixDatatypeDefinition> Datatypes = [];
    /// <summary>The code-set definitions in deterministic order.</summary>
    public List<FixCodeSetDefinition> CodeSets = [];
    /// <summary>The field definitions in deterministic order.</summary>
    public List<FixFieldDefinition> Fields = [];
    /// <summary>The standard-header members in wire order.</summary>
    public List<FixMemberDefinition> Header = [];
    /// <summary>The standard-trailer members in wire order.</summary>
    public List<FixMemberDefinition> Trailer = [];
    /// <summary>The component definitions in deterministic order.</summary>
    public List<FixComponentDefinition> Components = [];
    /// <summary>The group definitions in deterministic order.</summary>
    public List<FixGroupDefinition> Groups = [];
    /// <summary>The message definitions in deterministic order.</summary>
    public List<FixMessageDefinition> Messages = [];
    /// <summary>The scenario definitions in deterministic order.</summary>
    public List<FixScenarioDefinition> Scenarios = [];
    /// <summary>Diagnostics retained with a partially usable dictionary.</summary>
    public List<DictionaryDiagnostic> Diagnostics = [];
}
