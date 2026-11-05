namespace iso20022;

internal class GenerationConcept
{
    internal GenerationSourceKind SourceKind;
    internal GenerationConceptKind Kind;
    internal int SourceOrdinal;
    internal int ConceptOrdinal;
    internal int DeclarationOrdinal;
    internal string Identifier = string.Empty;
    internal string OwnerIdentifier = string.Empty;
    internal string Name = string.Empty;
    internal string Definition = string.Empty;
    internal bool IsRepositoryFallback;
    internal MessageDefinitionMetadata? MessageDefinition;
    internal MessageBuildingBlockDefinition? MessageBuildingBlock;
    internal MessageComponentDefinition? MessageComponent;
    internal MessageElementDefinition? MessageElement;
    internal ChoiceDefinition? Choice;
    internal DataTypeDefinition? DataType;
    internal CodeSetDefinition? CodeSet;
    internal CodeValueDefinition? CodeValue;
    internal ExternalSchemaDefinition? ExternalSchema;
}
