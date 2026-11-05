namespace htmlgui;

/// <summary>Reports an unrecoverable observable-state operation failure.</summary>
public class UiStateException : System.Exception
{
    /// <summary>The stable state identifier.</summary>
    public string StateId;
    /// <summary>The state path involved in the failure.</summary>
    public string Path;

    /// <summary>Creates a state exception with state and path context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="stateId">The stable state identifier.</param>
    /// <param name="path">The state path.</param>
    public UiStateException(string message, string stateId, string path)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (state '{Guard.ArgumentNotWhiteSpace(stateId, "stateId")}', path '{Guard.ArgumentNotWhiteSpace(path, "path")}').")
    {
        StateId = stateId;
        Path = path;
    }
}
