using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a transport mode, system, characteristic, address, broadcast list, or message.</summary>
public class TransportDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The normalized transport entity kind.</summary>
    public RepositoryEntityKind Kind;
    /// <summary>The transport definition name when supplied.</summary>
    public string Name = string.Empty;
    /// <summary>The transport definition meaning when supplied.</summary>
    public string Definition = string.Empty;
    /// <summary>The lifecycle and model-version metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue containing a transport mode.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The maximum time allowed to deliver a transport message.</summary>
    public IsoDurationValue? BoundedCommunicationDelay;
    /// <summary>The maximum inclusive clock variance from UTC.</summary>
    public IsoDurationValue? MaximumClockVariation;
    /// <summary>The maximum transport message size in kilobytes.</summary>
    public int MaximumMessageSizeKilobytes;
    /// <summary>The out-of-order message delivery window.</summary>
    public IsoDurationValue? MessageDeliveryWindow;
    /// <summary>The out-of-order business message sending window.</summary>
    public IsoDurationValue? MessageSendingWindow;
    /// <summary>The exact delivery-assurance code.</summary>
    public string DeliveryAssurance = string.Empty;
    /// <summary>The exact message-durability code.</summary>
    public string Durability = string.Empty;
    /// <summary>The exact message-casting code.</summary>
    public string MessageCasting = string.Empty;
    /// <summary>The exact message-delivery-order code.</summary>
    public string MessageDeliveryOrder = string.Empty;
    /// <summary>The exact message-validation-level code.</summary>
    public string MessageValidationLevel = string.Empty;
    /// <summary>The exact validation enablement code.</summary>
    public string MessageValidationOnOff = string.Empty;
    /// <summary>The exact validation-results handling code.</summary>
    public string MessageValidationResults = string.Empty;
    /// <summary>The exact receiver-asynchronicity code.</summary>
    public string ReceiverAsynchronicity = string.Empty;
    /// <summary>The exact sender-asynchronicity code.</summary>
    public string SenderAsynchronicity = string.Empty;
    /// <summary>The business transactions that select this transport mode.</summary>
    public List<RepositoryReference> BusinessTransactions = new List<RepositoryReference>();
    /// <summary>The messaging endpoints owned by or related to this transport definition.</summary>
    public List<RepositoryReference> Endpoints = new List<RepositoryReference>();
    /// <summary>The addresses owned by or related to this transport definition.</summary>
    public List<RepositoryReference> Addresses = new List<RepositoryReference>();
    /// <summary>The broadcast lists owned by or related to this transport definition.</summary>
    public List<RepositoryReference> BroadcastLists = new List<RepositoryReference>();
    /// <summary>The transport messages owned by or related to this transport definition.</summary>
    public List<RepositoryReference> TransportMessages = new List<RepositoryReference>();
    /// <summary>Unrecognized transport fields and future characteristics.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the transport definition.</summary>
    public SourceLocation Location = new SourceLocation();
}
