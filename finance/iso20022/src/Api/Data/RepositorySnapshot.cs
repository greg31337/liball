using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents the normalized identity and shared graph state of one imported repository edition.</summary>
public class RepositorySnapshot
{
    /// <summary>The XMI identifier of the repository root.</summary>
    public string Identifier = string.Empty;
    /// <summary>The caller-selected or artifact-defined repository edition.</summary>
    public string Edition = string.Empty;
    /// <summary>The exact provenance of the imported snapshot.</summary>
    public RepositoryProvenance Provenance = new RepositoryProvenance();
    /// <summary>The reference to the repository data dictionary root.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>The reference to the business process catalogue root.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The complete deterministic relationship graph available at this layer.</summary>
    public List<RepositoryReference> References = new List<RepositoryReference>();
    /// <summary>Repository-level constraints not owned by a more specific normalized entity.</summary>
    public List<ConstraintDefinition> Constraints = new List<ConstraintDefinition>();
    /// <summary>The business areas in deterministic repository order.</summary>
    public List<BusinessAreaDefinition> BusinessAreas = new List<BusinessAreaDefinition>();
    /// <summary>The business processes in deterministic repository order.</summary>
    public List<BusinessProcessDefinition> BusinessProcesses = new List<BusinessProcessDefinition>();
    /// <summary>The business roles in deterministic repository order.</summary>
    public List<BusinessRoleDefinition> BusinessRoles = new List<BusinessRoleDefinition>();
    /// <summary>The transaction participants in deterministic repository order.</summary>
    public List<ParticipantDefinition> Participants = new List<ParticipantDefinition>();
    /// <summary>The business transactions in deterministic repository order.</summary>
    public List<BusinessTransactionDefinition> BusinessTransactions = new List<BusinessTransactionDefinition>();
    /// <summary>The conversations in deterministic repository order.</summary>
    public List<ConversationDefinition> Conversations = new List<ConversationDefinition>();
    /// <summary>The message choreographies in deterministic repository order.</summary>
    public List<MessageChoreographyDefinition> MessageChoreographies = new List<MessageChoreographyDefinition>();
    /// <summary>The message transmissions in deterministic repository order.</summary>
    public List<MessageTransmissionDefinition> MessageTransmissions = new List<MessageTransmissionDefinition>();
    /// <summary>The messaging endpoints in deterministic repository order.</summary>
    public List<MessagingEndpointDefinition> MessagingEndpoints = new List<MessagingEndpointDefinition>();
    /// <summary>The transport definitions in deterministic repository order.</summary>
    public List<TransportDefinition> Transports = new List<TransportDefinition>();
    /// <summary>The business components in deterministic repository order.</summary>
    public List<BusinessComponentDefinition> BusinessComponents = new List<BusinessComponentDefinition>();
    /// <summary>The business elements in deterministic repository order.</summary>
    public List<BusinessElementDefinition> BusinessElements = new List<BusinessElementDefinition>();
    /// <summary>The message sets in deterministic repository order.</summary>
    public List<MessageSetDefinition> MessageSets = new List<MessageSetDefinition>();
    /// <summary>The message definitions in deterministic repository order.</summary>
    public List<MessageDefinitionMetadata> MessageDefinitions = new List<MessageDefinitionMetadata>();
    /// <summary>The message building blocks in deterministic repository order.</summary>
    public List<MessageBuildingBlockDefinition> MessageBuildingBlocks = new List<MessageBuildingBlockDefinition>();
    /// <summary>The message components in deterministic repository order.</summary>
    public List<MessageComponentDefinition> MessageComponents = new List<MessageComponentDefinition>();
    /// <summary>The message elements in deterministic repository order.</summary>
    public List<MessageElementDefinition> MessageElements = new List<MessageElementDefinition>();
    /// <summary>The reusable choices and legacy XOR rules in deterministic repository order.</summary>
    public List<ChoiceDefinition> Choices = new List<ChoiceDefinition>();
    /// <summary>The primitive and schema data types in deterministic repository order.</summary>
    public List<DataTypeDefinition> DataTypes = new List<DataTypeDefinition>();
    /// <summary>The code sets in deterministic repository order.</summary>
    public List<CodeSetDefinition> CodeSets = new List<CodeSetDefinition>();
    /// <summary>The syntax definitions in deterministic repository order.</summary>
    public List<SyntaxDefinition> Syntaxes = new List<SyntaxDefinition>();
    /// <summary>The encoding definitions in deterministic repository order.</summary>
    public List<EncodingDefinition> Encodings = new List<EncodingDefinition>();
    /// <summary>The external schema and user-defined types in deterministic repository order.</summary>
    public List<ExternalSchemaDefinition> ExternalSchemas = new List<ExternalSchemaDefinition>();
    /// <summary>The syntax message schemes in deterministic repository order.</summary>
    public List<SyntaxMessageSchemeDefinition> SyntaxMessageSchemes = new List<SyntaxMessageSchemeDefinition>();
    /// <summary>The convergence definitions in deterministic repository order.</summary>
    public List<ConvergenceDefinition> ConvergenceDefinitions = new List<ConvergenceDefinition>();
    /// <summary>Unrecognized repository-root content and unknown entity records.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
}
