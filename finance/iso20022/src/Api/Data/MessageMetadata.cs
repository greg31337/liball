using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains stable business metadata extracted from a message.</summary>
public class MessageMetadata
{
    /// <summary>The exact payload identifier when known.</summary>
    public MessageIdentifier? MessageIdentifier;
    /// <summary>The exact payload namespace URI.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The business message identifier.</summary>
    public string BusinessMessageIdentifier = string.Empty;
    /// <summary>The business service or profile.</summary>
    public string BusinessService = string.Empty;
    /// <summary>The sender identity.</summary>
    public string Sender = string.Empty;
    /// <summary>The receiver identity.</summary>
    public string Receiver = string.Empty;
    /// <summary>The message creation time when present.</summary>
    public IsoDateTimeValue? CreationDateTime;
    /// <summary>Whether a possible-duplicate indicator was present.</summary>
    public bool HasPossibleDuplicate;
    /// <summary>The possible-duplicate indicator value.</summary>
    public bool PossibleDuplicate;
    /// <summary>The message priority.</summary>
    public string Priority = string.Empty;
    /// <summary>The unique end-to-end transaction reference.</summary>
    public string Uetr = string.Empty;
    /// <summary>The primary amount and currency when identified.</summary>
    public IsoAmountValue? PrimaryAmount;
    /// <summary>The primary value date when identified.</summary>
    public IsoDateValue? ValueDate;
    /// <summary>The ordered business references found in the message.</summary>
    public List<string> References = new List<string>();
}
