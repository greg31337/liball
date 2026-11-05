namespace pop3;

/// <summary>Identifies the operation represented by transfer progress.</summary>
public enum Pop3TransferKind
{
    /// <summary>A LIST response transfer.</summary>
    List = 0,

    /// <summary>A UIDL response transfer.</summary>
    Uidl = 1,

    /// <summary>A RETR message transfer.</summary>
    Retrieve = 2,

    /// <summary>A TOP message transfer.</summary>
    Top = 3,

    /// <summary>A multiline transfer for a caller-defined extension.</summary>
    ExtensionData = 4
}
