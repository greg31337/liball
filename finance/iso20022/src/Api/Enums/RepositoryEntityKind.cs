namespace iso20022;

/// <summary>Identifies a normalized kind of ISO 20022 repository entity.</summary>
public enum RepositoryEntityKind
{
    /// <summary>The entity kind is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The repository root.</summary>
    Repository = 1,
    /// <summary>The repository data dictionary.</summary>
    DataDictionary = 2,
    /// <summary>The business process catalogue.</summary>
    BusinessProcessCatalogue = 3,
    /// <summary>A business area.</summary>
    BusinessArea = 4,
    /// <summary>A business process.</summary>
    BusinessProcess = 5,
    /// <summary>A business role.</summary>
    BusinessRole = 6,
    /// <summary>A participant.</summary>
    Participant = 7,
    /// <summary>A business transaction.</summary>
    BusinessTransaction = 8,
    /// <summary>A business conversation.</summary>
    Conversation = 9,
    /// <summary>A message choreography.</summary>
    MessageChoreography = 10,
    /// <summary>A message transmission.</summary>
    MessageTransmission = 11,
    /// <summary>A messaging endpoint.</summary>
    MessagingEndpoint = 12,
    /// <summary>A broadcast list.</summary>
    BroadcastList = 13,
    /// <summary>A message transport system.</summary>
    MessageTransportSystem = 14,
    /// <summary>A transport mode.</summary>
    TransportMode = 15,
    /// <summary>A transport characteristic.</summary>
    TransportCharacteristic = 16,
    /// <summary>A business component.</summary>
    BusinessComponent = 17,
    /// <summary>A business attribute.</summary>
    BusinessAttribute = 18,
    /// <summary>A business association end.</summary>
    BusinessAssociationEnd = 19,
    /// <summary>A normalized business element of an unspecified subtype.</summary>
    BusinessElement = 20,
    /// <summary>A message set.</summary>
    MessageSet = 21,
    /// <summary>A message definition.</summary>
    MessageDefinition = 22,
    /// <summary>A message building block.</summary>
    MessageBuildingBlock = 23,
    /// <summary>A message component.</summary>
    MessageComponent = 24,
    /// <summary>A message attribute.</summary>
    MessageAttribute = 25,
    /// <summary>A message association end.</summary>
    MessageAssociationEnd = 26,
    /// <summary>A normalized message element of an unspecified subtype.</summary>
    MessageElement = 27,
    /// <summary>A choice or exclusive-or definition.</summary>
    Choice = 28,
    /// <summary>A data type.</summary>
    DataType = 29,
    /// <summary>A data-type facet.</summary>
    DataTypeFacet = 30,
    /// <summary>A code set.</summary>
    CodeSet = 31,
    /// <summary>A code value.</summary>
    CodeValue = 32,
    /// <summary>A message syntax.</summary>
    Syntax = 33,
    /// <summary>A message encoding.</summary>
    Encoding = 34,
    /// <summary>An external schema.</summary>
    ExternalSchema = 35,
    /// <summary>A syntax message scheme.</summary>
    SyntaxMessageScheme = 36,
    /// <summary>A repository constraint.</summary>
    Constraint = 37,
    /// <summary>A semantic-markup tuple or element.</summary>
    SemanticMarkup = 38,
    /// <summary>A documentation item.</summary>
    Documentation = 39,
    /// <summary>A convergence or reverse-engineering definition.</summary>
    Convergence = 40,
    /// <summary>An address associated with a messaging endpoint.</summary>
    Address = 41,
    /// <summary>A transport message exchanged through a message transport system.</summary>
    TransportMessage = 42
}
