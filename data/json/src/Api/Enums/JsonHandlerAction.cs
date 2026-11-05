namespace json;

/// <summary>Directs push parsing after a token handler invocation.</summary>
public enum JsonHandlerAction
{
    /// <summary>Continues publishing tokens.</summary>
    Continue = 0,

    /// <summary>Pauses parsing so it can be resumed later.</summary>
    Pause = 1,

    /// <summary>Stops parsing without reporting an error.</summary>
    Stop = 2
}
