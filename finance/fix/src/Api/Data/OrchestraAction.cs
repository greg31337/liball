using System.Collections.Generic;

namespace fix;

/// <summary>Retains one parameter or executable action from an Orchestra action block.</summary>
public class OrchestraAction
{
    /// <summary>The source action or parameter kind.</summary>
    public string Kind = "";
    /// <summary>The referenced or declared name.</summary>
    public string Name = "";
    /// <summary>The referenced identifier in source lexical form.</summary>
    public string ReferenceId = "";
    /// <summary>The referenced scenario.</summary>
    public string Scenario = "";
    /// <summary>The referenced message type.</summary>
    public string MessageType = "";
    /// <summary>The owning actor name for a trigger or timer.</summary>
    public string Actor = "";
    /// <summary>The state-machine name for a trigger.</summary>
    public string StateMachine = "";
    /// <summary>The timer operation.</summary>
    public string Operation = "";
    /// <summary>The timer interval in source lexical form.</summary>
    public string Interval = "";
    /// <summary>The assignment expression.</summary>
    public string Expression = "";
    /// <summary>The assignment target parsed from the expression.</summary>
    public string AssignmentTarget = "";
    /// <summary>The assignment rule when this action assigns a value.</summary>
    public OrchestraRule? Rule;
    /// <summary>The typed timer schedule when this action controls a timer.</summary>
    public OrchestraTimer? Timer;
    /// <summary>Nested actions, such as a scheduled timer activity.</summary>
    public List<OrchestraAction> Actions = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
    /// <summary>The complete source XML representation.</summary>
    public string Xml = "";
}
