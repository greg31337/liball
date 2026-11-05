namespace pop3;

/// <summary>Identifies the current state of a POP3 session.</summary>
public enum Pop3SessionState
{
    /// <summary>The session has no attached transport.</summary>
    Disconnected = 0,

    /// <summary>The session is awaiting successful authentication.</summary>
    Authorization = 1,

    /// <summary>The session has an acquired maildrop transaction.</summary>
    Transaction = 2,

    /// <summary>The session is committing requested mailbox changes.</summary>
    Update = 3,

    /// <summary>The session has terminated and cannot accept more operations.</summary>
    Closed = 4
}
