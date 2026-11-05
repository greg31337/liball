namespace iso20022;

internal enum GenerationConceptKind
{
    Unknown = 0,
    MessageDefinition = 1,
    MessageBuildingBlock = 2,
    MessageComponent = 3,
    MessageElement = 4,
    Choice = 5,
    DataType = 6,
    CodeSet = 7,
    CodeValue = 8,
    ExternalSchema = 9
}
