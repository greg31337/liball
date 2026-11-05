using System.Collections.Generic;

namespace iso20022;

internal class XsdGenerationContext
{
    internal XsdGenerationUnit Unit = new XsdGenerationUnit();
    internal GenerationOptions Options = new GenerationOptions();
    internal GenerationResult Result = new GenerationResult();
    internal List<XsdGenerationName> Names = new List<XsdGenerationName>();
    internal List<XsdGenerationImport> Imports = new List<XsdGenerationImport>();
    internal List<MessageDefinitionMetadata> Messages = new List<MessageDefinitionMetadata>();
    internal List<MessageBuildingBlockDefinition> BuildingBlocks =
        new List<MessageBuildingBlockDefinition>();
    internal List<MessageComponentDefinition> Components =
        new List<MessageComponentDefinition>();
    internal List<MessageElementDefinition> Elements = new List<MessageElementDefinition>();
    internal List<ChoiceDefinition> Choices = new List<ChoiceDefinition>();
    internal List<DataTypeDefinition> DataTypes = new List<DataTypeDefinition>();
    internal List<CodeSetDefinition> CodeSets = new List<CodeSetDefinition>();
    internal List<ExternalSchemaDefinition> ExternalSchemas =
        new List<ExternalSchemaDefinition>();
}
