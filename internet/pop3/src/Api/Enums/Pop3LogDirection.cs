namespace pop3;

/// <summary>Identifies the source and destination of a protocol log entry.</summary>
public enum Pop3LogDirection
{
    /// <summary>Data or metadata sent from the client to the server.</summary>
    ClientToServer = 0,

    /// <summary>Data or metadata sent from the server to the client.</summary>
    ServerToClient = 1,

    /// <summary>A state change generated locally by the library.</summary>
    LocalState = 2
}
