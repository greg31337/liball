namespace htmlgui;

/// <summary>Reports a malformed or inconsistent portable UI model.</summary>
public class UiModelException : System.Exception
{
    /// <summary>The stable model diagnostic code.</summary>
    public string Code;
    /// <summary>The model location at which the failure was detected.</summary>
    public string Location;

    /// <summary>Creates a model exception with stable diagnostic context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="location">The model location.</param>
    public UiModelException(string message, string code, string location)
        : base($"{Guard.ArgumentNotWhiteSpace(message, "message")} (model code '{Guard.ArgumentNotWhiteSpace(code, "code")}', location '{Guard.ArgumentNotWhiteSpace(location, "location")}').")
    {
        Code = code;
        Location = location;
    }
}
