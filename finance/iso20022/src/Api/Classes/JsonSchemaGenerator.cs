using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace iso20022;

/// <summary>Generates deterministic JSON Schema Draft 2020-12 files using the ISO 20022 transformation rules.</summary>
public class JsonSchemaGenerator : IJsonSchemaGenerator
{
    /// <summary>Initializes a deterministic logical-model-to-JSON-Schema generator.</summary>
    public JsonSchemaGenerator()
    {
    }

    /// <summary>Generates the selected JSON Schema files and their ownership manifest.</summary>
    /// <param name="plan">The exact sources, selection, settings, and previous ownership state.</param><returns>The exception-less deterministic generation result.</returns>
    public GenerationResult GenerateJsonSchema(GenerationPlan? plan)
    {
        XsdGenerator planner = new XsdGenerator();
        if (plan == null)
        {
            return planner.CreateGenerationFailure(
                null,
                "generation.plan.required",
                "A generation plan is required.");
        }
        GenerationResult? invalid = planner.ValidateGenerationPlan(plan);
        if (invalid != null)
        {
            return invalid;
        }

        GenerationPlan selectedPlan = plan;
        GenerationResult result = new GenerationResult();
        result.Plan = selectedPlan;
        planner.InitializeManifest(result, selectedPlan);
        result.Manifest.GeneratorIdentifier = "iso20022.json-schema-2020-12";
        List<XsdGenerationUnit> units = planner.BuildGenerationUnits(selectedPlan);
        if (units.Count == 0)
        {
            return planner.CreateGenerationFailure(
                selectedPlan,
                "generation.json_schema.selection_empty",
                "The generation plan did not select any repository message or XSD artifact.");
        }
        if (units.Count > selectedPlan.Options.Limits.MaxSchemaCount)
        {
            return planner.CreateGenerationFailure(
                selectedPlan,
                "generation.json_schema.schema_limit",
                "The selected output exceeds the configured schema-count limit.");
        }

        planner.SortGenerationUnits(units);
        long totalBytes = 0;
        int index = 0;
        while (index < units.Count)
        {
            selectedPlan.Options.CancellationToken.ThrowIfCancellationRequested();
            XsdGenerationUnit unit = units[index];
            XsdGenerationContext context = XsdLogicalModelEmitter.CreateContext(
                unit,
                selectedPlan.Options,
                result);
            long itemCount = XsdLogicalModelEmitter.CountGenerationItems(context);
            if (itemCount > selectedPlan.Options.Limits.MaxCollectionCount ||
                itemCount > selectedPlan.Options.Limits.MaxElementCount)
            {
                return planner.CreateGenerationFailure(
                    selectedPlan,
                    "generation.json_schema.model_limit",
                    "A selected logical model exceeds the configured generation limits.");
            }

            byte[] content = JsonSchemaLogicalModelEmitter.EmitJsonSchema(context);
            totalBytes += content.Length;
            if (content.Length > selectedPlan.Options.Limits.MaxTotalBytes ||
                totalBytes > selectedPlan.Options.Limits.MaxTotalBytes)
            {
                return planner.CreateGenerationFailure(
                    selectedPlan,
                    "generation.json_schema.byte_limit",
                    "The generated output exceeds the configured total-byte limit.");
            }

            GeneratedFile file = CreateJsonSchemaFile(unit, context, content, result.Files);
            result.Files.Add(file);
            result.Manifest.Files.Add(file);
            GenerationManifestEmitter.AddSchemaMappings(context, file, result.Manifest);
            index++;
        }

        GenerationManifestEmitter.CompleteManifest(result);
        if (!IncrementalGenerationWriter.ApplyOutput(result))
        {
            return result;
        }
        result.Success = true;
        return result;
    }

    internal GeneratedFile CreateJsonSchemaFile(
        XsdGenerationUnit unit,
        XsdGenerationContext context,
        byte[] content,
        List<GeneratedFile> existing)
    {
        string stem = FindJsonSchemaFileStem(unit, context);
        string path = "JsonSchemas/" + stem + ".schema.json";
        XsdGenerator helper = new XsdGenerator();
        if (helper.OutputPathExists(existing, path))
        {
            path = "JsonSchemas/" + stem + "_" +
                GenerationStableHash.CreateHex(unit.Identity).Substring(0, 16) +
                ".schema.json";
        }
        GeneratedFile file = new GeneratedFile();
        file.Kind = GenerationFileKind.JsonSchema;
        file.RelativePath = path;
        file.MediaType = Iso20022MediaTypes.JsonSchema;
        file.NamespaceName = JsonSchemaLogicalModelEmitter.FindJsonSchemaIdentifier(context);
        file.Content = content;
        file.Hash.Value = SHA256.HashData(content);
        file.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        file.HashAlgorithm = "SHA-256";
        if (unit.Message != null)
        {
            file.SourceIdentifiers.Add(unit.Message.Identifier);
        }
        if (unit.Schema != null)
        {
            int index = 0;
            while (index < unit.Schema.MessageDefinitions.Count)
            {
                file.SourceIdentifiers.Add(unit.Schema.MessageDefinitions[index].Identifier);
                index++;
            }
        }
        return file;
    }

    internal string FindJsonSchemaFileStem(
        XsdGenerationUnit unit,
        XsdGenerationContext context)
    {
        MessageDefinitionMetadata? message = JsonSchemaLogicalModelEmitter.FindPrimaryMessage(
            context);
        if (message != null)
        {
            XsdGenerator helper = new XsdGenerator();
            string identifier = helper.FindIdentifierValue(message.MessageIdentifier);
            string name = JsonSchemaLogicalModelEmitter.FindMessageDefinitionName(message);
            if (identifier != "...")
            {
                return MakeJsonFileStemSafe(identifier + "-" + name);
            }
            return MakeJsonFileStemSafe(name);
        }
        if (unit.Schema != null)
        {
            string source = Path.GetFileNameWithoutExtension(unit.Schema.SourceName);
            if (source.Length > 0)
            {
                return MakeJsonFileStemSafe(source);
            }
        }
        return MakeJsonFileStemSafe(unit.FileStem);
    }

    internal string MakeJsonFileStemSafe(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if ((current >= 'a' && current <= 'z') ||
                (current >= 'A' && current <= 'Z') ||
                (current >= '0' && current <= '9') ||
                current == '.' || current == '-' || current == '_')
            {
                result.Append(current);
            }
            else
            {
                result.Append('_');
            }
            index++;
        }
        if (result.Length == 0)
        {
            return "schema";
        }
        return result.ToString();
    }
}
