namespace json;

/// <summary>Selects how an initial UTF-8 byte order mark is handled.</summary>
public enum JsonBomPolicy
{
    /// <summary>Rejects a UTF-8 byte order mark.</summary>
    Reject = 0,

    /// <summary>Allows one UTF-8 byte order mark at byte offset zero.</summary>
    AllowInitial = 1
}
