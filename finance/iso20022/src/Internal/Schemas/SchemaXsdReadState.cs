using System.Text;

namespace iso20022;

internal class SchemaXsdReadState
{
    internal SchemaArtifact Schema = new SchemaArtifact();
    internal SchemaRegistryOptions Options = new SchemaRegistryOptions();
    internal SchemaCompilationResult Result = new SchemaCompilationResult();
    internal bool Success = true;
    internal bool SawSchema;
    internal string ParsedVersion = string.Empty;
    internal DataTypeDefinition? CurrentDataType;
    internal int CurrentDataTypeDepth = -1;
    internal MessageComponentDefinition? CurrentComponent;
    internal int CurrentComponentDepth = -1;
    internal ChoiceDefinition? CurrentChoice;
    internal int CurrentChoiceDepth = -1;
    internal ExternalSchemaDefinition? CurrentExternalSchema;
    internal MessageElementDefinition? CurrentElement;
    internal int CurrentElementDepth = -1;
    internal DataTypeDefinition? DocumentationDataType;
    internal MessageComponentDefinition? DocumentationComponent;
    internal MessageElementDefinition? DocumentationElement;
    internal int DocumentationDepth = -1;
    internal StringBuilder DocumentationText = new StringBuilder();
}
