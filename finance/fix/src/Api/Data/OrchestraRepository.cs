using System.Collections.Generic;

namespace fix;

/// <summary>Represents loss-aware Orchestra repository, behavior, and interface content.</summary>
public class OrchestraRepository
{
    /// <summary>The source schema version.</summary>
    public OrchestraVersion Version;
    /// <summary>The repository metadata.</summary>
    public OrchestraMetadata Metadata = new OrchestraMetadata();
    /// <summary>The mapped canonical dictionary when available.</summary>
    public FixDictionary? Dictionary;
    /// <summary>Repository-level annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>The business-process categories.</summary>
    public List<OrchestraCategory> Categories = [];
    /// <summary>The large-grained business sections.</summary>
    public List<OrchestraSection> Sections = [];
    /// <summary>The semantic concepts and their member references.</summary>
    public List<OrchestraConcept> Concepts = [];
    /// <summary>The declared physical encoding standards.</summary>
    public List<OrchestraEncodingStandard> EncodingStandards = [];
    /// <summary>The source datatype definitions.</summary>
    public List<OrchestraDatatypeDefinition> Datatypes = [];
    /// <summary>The source code-set definitions.</summary>
    public List<OrchestraCodeSetDefinition> CodeSets = [];
    /// <summary>The default code-set encodings.</summary>
    public List<OrchestraEncoding> CodeSetEncodings = [];
    /// <summary>The source field definitions.</summary>
    public List<OrchestraFieldDefinition> Fields = [];
    /// <summary>The source component definitions.</summary>
    public List<OrchestraComponentDefinition> Components = [];
    /// <summary>The default component encodings.</summary>
    public List<OrchestraEncoding> ComponentEncodings = [];
    /// <summary>The source repeating-group definitions.</summary>
    public List<OrchestraGroupDefinition> Groups = [];
    /// <summary>The default repeating-group encodings.</summary>
    public List<OrchestraEncoding> GroupEncodings = [];
    /// <summary>The source message definitions.</summary>
    public List<OrchestraMessageDefinition> Messages = [];
    /// <summary>The default message encodings.</summary>
    public List<OrchestraEncoding> MessageEncodings = [];
    /// <summary>The scenarios discovered in source order.</summary>
    public List<OrchestraScenarioDefinition> Scenarios = [];
    /// <summary>Unknown namespace-qualified repository extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
    /// <summary>The declared variables.</summary>
    public List<OrchestraVariable> Variables = [];
    /// <summary>The declared actor timers.</summary>
    public List<OrchestraTimer> Timers = [];
    /// <summary>The declared rules.</summary>
    public List<OrchestraRule> Rules = [];
    /// <summary>The declared actors.</summary>
    public List<OrchestraActor> Actors = [];
    /// <summary>The declared flows.</summary>
    public List<OrchestraFlow> Flows = [];
    /// <summary>The declared state machines.</summary>
    public List<OrchestraStateMachine> StateMachines = [];
    /// <summary>The declared interfaces.</summary>
    public List<OrchestraInterface> Interfaces = [];
    /// <summary>The distinct Orchestra repository URIs linked by interface protocols.</summary>
    public List<string> RepositoryLinks = [];
}
