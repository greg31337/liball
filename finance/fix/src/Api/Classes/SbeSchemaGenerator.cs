namespace fix;

/// <summary>Generates deterministic SBE XML schemas from canonical FIX dictionaries.</summary>
public class SbeSchemaGenerator : ISbeSchemaGenerator
{
    /// <summary>Creates an SBE schema generator.</summary>
    public SbeSchemaGenerator()
    {
    }

    /// <summary>Generates an SBE schema without writing caller files.</summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="options">The SBE schema options.</param>
    /// <returns>The generated schema file and diagnostics.</returns>
    public GenerationResult GenerateSbeSchema(FixDictionary dictionary, SbeGenerationOptions options)
    {
        GenerationResult result = new GenerationResult();
        if (dictionary == null)
        {
            AddSbeGeneratorDiagnostic(result, ValidationSeverity.Error, "missing-sbe-dictionary", "The source dictionary is missing.", "/");
            return result;
        }
        if (options == null)
        {
            AddSbeGeneratorDiagnostic(result, ValidationSeverity.Error, "missing-sbe-options", "The SBE generation options are missing.", "/options");
            return result;
        }

        SbeMappingPlan plan = SbeTypeMapper.MapDictionaryTypes(dictionary, options);
        SbeSchemaValidation.ValidateSchemaPlan(plan);
        CopySbeGeneratorDiagnostics(result.Diagnostics, plan.Diagnostics);
        if (HasSbeGeneratorErrors(result.Diagnostics)) return result;

        GeneratedFile file = new GeneratedFile();
        file.Path = "Schemas/SbeSchema.xml";
        file.Content = SbeSchemaEmitter.EmitSchema(plan);
        file.ContentHash = GenerationManifest.ComputeContentHash(file.Content);
        result.Files.Add(file);
        result.Manifest = GenerationManifest.CreateManifest(result.Files);
        result.Success = true;
        return result;
    }

    internal static bool HasSbeGeneratorErrors(System.Collections.Generic.List<GenerationDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal) return true;
        }
        return false;
    }

    internal static void CopySbeGeneratorDiagnostics(System.Collections.Generic.List<GenerationDiagnostic> destination, System.Collections.Generic.List<GenerationDiagnostic> source)
    {
        for (int index = 0; index < source.Count; index++) destination.Add(source[index]);
    }

    internal static void AddSbeGeneratorDiagnostic(GenerationResult result, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        result.Diagnostics.Add(diagnostic);
    }
}
