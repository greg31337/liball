using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes an addressable endpoint capable of sending and receiving transport messages.</summary>
public class MessagingEndpointDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The endpoint name when supplied by the source adapter.</summary>
    public string Name = string.Empty;
    /// <summary>The endpoint description when supplied by the source adapter.</summary>
    public string Definition = string.Empty;
    /// <summary>The lifecycle and model-version metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The transport system that owns and uses the endpoint.</summary>
    public RepositoryReference TransportSystem = new RepositoryReference();
    /// <summary>The addresses that identify the endpoint.</summary>
    public List<RepositoryReference> Addresses = new List<RepositoryReference>();
    /// <summary>The broadcast lists reached through the endpoint addresses.</summary>
    public List<RepositoryReference> BroadcastLists = new List<RepositoryReference>();
    /// <summary>The transport messages sent by the endpoint.</summary>
    public List<RepositoryReference> SentMessages = new List<RepositoryReference>();
    /// <summary>The transport messages received by the endpoint.</summary>
    public List<RepositoryReference> ReceivedMessages = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the endpoint.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the endpoint.</summary>
    public SourceLocation Location = new SourceLocation();
}
