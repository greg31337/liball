using System.Collections.Generic;

namespace fix;

/// <summary>Provides version, session, and external state used during validation.</summary>
public class ValidationContext
{
    /// <summary>The dictionary identity selected for the message.</summary>
    public FixDictionaryIdentity? DictionaryIdentity;
    /// <summary>The BeginString expected by the active session.</summary>
    public string ExpectedBeginString = "";
    /// <summary>The session's default application version.</summary>
    public string DefaultApplicationVersion = "";
    /// <summary>The active dictionary scenario.</summary>
    public string Scenario = "";
    /// <summary>The previous related message when a rule requires it.</summary>
    public FixMessage? PreviousMessage;
    /// <summary>Caller-supplied external state addressed by stable names.</summary>
    public Dictionary<string, string> ExternalState = [];
}
