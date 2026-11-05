using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes information passed from one transaction participant to one or more receivers.</summary>
public class MessageTransmissionDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The transmission name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the transmission.</summary>
    public string Definition = string.Empty;
    /// <summary>The purpose and scope of the transmitted message type.</summary>
    public string MessageTypeDescription = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business transaction that contains the transmission.</summary>
    public RepositoryReference BusinessTransaction = new RepositoryReference();
    /// <summary>The participant that sends the transmission.</summary>
    public RepositoryReference Sender = new RepositoryReference();
    /// <summary>The participants that receive the transmission.</summary>
    public List<RepositoryReference> Receivers = new List<RepositoryReference>();
    /// <summary>The message definitions derived from the transmission.</summary>
    public List<RepositoryReference> DerivedMessageDefinitions = new List<RepositoryReference>();
    /// <summary>The conversations in which the transmission participates.</summary>
    public List<RepositoryReference> Conversations = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the transmission.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the transmission.</summary>
    public SourceLocation Location = new SourceLocation();
}
