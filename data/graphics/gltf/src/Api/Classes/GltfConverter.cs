using System.IO;

namespace gltf;

/// <summary>Provides container, resource-layout, and document conversion operations.</summary>
public class GltfConverter : IGltfConverter
{
    internal BoundedStreamReader StreamReader;
    internal RequiredExtensionCapabilityChecker RequiredExtensions;
    internal GltfReader Reader;
    internal FileConversionOutputWriter FileOutput;
    internal GltfToGlbConverter GltfToGlb;
    internal GlbToGltfConverter GlbToGltf;
    internal SameContainerConverter SameContainer;
    internal ConversionManifestBuilder ManifestBuilder;
    internal GltfTransformer Transformer;

    /// <summary>Initializes a glTF converter.</summary>
    public GltfConverter()
    {
        StreamReader = new BoundedStreamReader();
        RequiredExtensions = new RequiredExtensionCapabilityChecker();
        Reader = new GltfReader();
        FileOutput = new FileConversionOutputWriter();
        GltfToGlb = new GltfToGlbConverter();
        GlbToGltf = new GlbToGltfConverter();
        SameContainer = new SameContainerConverter();
        ManifestBuilder = new ConversionManifestBuilder();
        Transformer = new GltfTransformer();
    }

    /// <summary>Converts an input file to an output file.</summary>
    /// <param name="inputPath">The input file path.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    public GltfConversionResult ConvertFile(string inputPath, string outputPath, GltfConversionOptions options)
    {
        if (inputPath == null)
        {
            throw new System.ArgumentNullException("inputPath");
        }

        if (outputPath == null)
        {
            throw new System.ArgumentNullException("outputPath");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.ReadOptions == null)
        {
            throw new System.ArgumentException(
                "The conversion read options must not be null.",
                "options");
        }

        if (options.WriteOptions == null)
        {
            throw new System.ArgumentException(
                "The conversion write options must not be null.",
                "options");
        }

        ValidateConversionOptions(options);
        GltfConversionOptions effectiveOptions = CreateFileConversionOptions(
            inputPath,
            outputPath,
            options);
        GltfReadResult readResult = Reader.ReadConversionFile(
            inputPath,
            effectiveOptions.ReadOptions);
        if (readResult.Status != GltfOperationStatus.Success)
        {
            return CreateReadFailureResult(readResult);
        }

        GltfDocument? document = readResult.Document;
        if (document == null)
        {
            throw new GltfParseException(
                "File conversion did not produce an input document.");
        }

        effectiveOptions.ReadOptions.Validate = false;
        GltfConversionResult result = ConvertDocument(
            document,
            effectiveOptions);
        AddReadDiagnostics(result, readResult);
        ManifestBuilder.FinalizeResult(
            document,
            result,
            effectiveOptions);
        if (result.Status == GltfOperationStatus.Success)
        {
            FileOutput.WriteResult(outputPath, result, effectiveOptions);
            result.PrimaryPath = Path.GetFullPath(outputPath);
            result.PrimaryBytes = [];
        }

        return result;
    }

    /// <summary>Converts an input stream to an output stream.</summary>
    /// <param name="input">The input stream.</param>
    /// <param name="output">The output stream.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    public GltfConversionResult ConvertStream(Stream input, Stream output, GltfConversionOptions options)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (output == null)
        {
            throw new System.ArgumentNullException("output");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.ReadOptions == null)
        {
            throw new System.ArgumentException("The conversion read options must not be null.", "options");
        }

        if (options.WriteOptions == null)
        {
            throw new System.ArgumentException("The conversion write options must not be null.", "options");
        }

        Stream? ownedInput = null;
        Stream? ownedOutput = null;
        if (!options.ReadOptions.LeaveStreamOpen)
        {
            ownedInput = input;
        }

        if (!options.WriteOptions.LeaveStreamOpen)
        {
            ownedOutput = output;
        }

        if (object.ReferenceEquals(ownedInput, ownedOutput))
        {
            ownedOutput = null;
        }

        using (ownedInput)
        {
            using (ownedOutput)
            {
                ValidateConversionOptions(options);
                if (!input.CanRead)
                {
                    throw new System.ArgumentException(
                        "The input stream must be readable.",
                        "input");
                }

                if (!output.CanWrite)
                {
                    throw new System.ArgumentException(
                        "The output stream must be writable.",
                        "output");
                }

                byte[] bytes = StreamReader.ReadAllBytes(
                    input,
                    options.ReadOptions.Limits.MaximumInputByteLength);
                GltfConversionResult result = ConvertBytes(bytes, options);
                if (result.Status == GltfOperationStatus.Success)
                {
                    output.Write(
                        result.PrimaryBytes,
                        0,
                        result.PrimaryBytes.Length);
                    result.PrimaryBytes = [];
                }

                return result;
            }
        }
    }

    /// <summary>Converts input bytes to owned output data.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    public GltfConversionResult ConvertBytes(byte[] bytes, GltfConversionOptions options)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        ValidateConversionOptions(options);
        GltfConversionOptions effectiveOptions =
            CreateMemoryConversionOptions(options);
        GltfReadResult readResult = Reader.ReadConversionBytes(
            bytes,
            effectiveOptions.ReadOptions);
        if (readResult.Status != GltfOperationStatus.Success)
        {
            return CreateReadFailureResult(readResult);
        }

        GltfDocument? document = readResult.Document;
        if (document == null)
        {
            throw new GltfParseException(
                "Byte conversion did not produce an input document.");
        }

        effectiveOptions.ReadOptions.Validate = false;
        GltfConversionResult result = ConvertDocument(
            document,
            effectiveOptions);
        AddReadDiagnostics(result, readResult);
        ManifestBuilder.FinalizeResult(
            document,
            result,
            effectiveOptions);
        return result;
    }

    /// <summary>Converts a constructed document.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    public GltfConversionResult ConvertDocument(GltfDocument document, GltfConversionOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        ValidateConversionOptions(options);
        System.Collections.Generic.List<GltfValidationIssue> issues =
            RequiredExtensions.CheckConversionCapabilities(document, options);
        if (issues.Count > 0)
        {
            throw new GltfUnsupportedFeatureException(issues[0].Message);
        }

        if (options.ReadOptions.Validate)
        {
            GltfValidationReport validation = Reader.Validator.ValidateDocument(
                document,
                Reader.CreateValidationOptions(
                    options.ReadOptions.ValidationOptions,
                    options.ReadOptions.ExtensionHandlers,
                    options.ReadOptions.ImageCodecs));
            if (!validation.IsValid)
            {
                return CreateInputValidationFailureResult(
                    document,
                    validation);
            }
        }

        GltfDocument conversionDocument = document;
        GltfTransformReport? transformReport = null;
        if (options.TransformPlan != null)
        {
            transformReport = Transformer.TransformDocument(
                document,
                options.TransformPlan,
                options.TransformOptions);
            if (transformReport.Status != GltfOperationStatus.Success
                || transformReport.Document == null)
            {
                GltfConversionResult transformFailure =
                    CreateTransformFailureResult(transformReport);
                ManifestBuilder.RebuildDecisions(
                    document,
                    transformFailure,
                    options);
                ManifestBuilder.IssueAggregator.SortIssues(
                    transformFailure.Diagnostics);
                return transformFailure;
            }

            conversionDocument = transformReport.Document;
        }

        GltfContainerFormat inputFormat = ResolveDocumentInputFormat(
            conversionDocument,
            options);
        GltfContainerFormat outputFormat = options.OutputFormat;
        if (outputFormat == GltfContainerFormat.Auto)
        {
            outputFormat = inputFormat;
        }

        if (outputFormat != GltfContainerFormat.Json
            && outputFormat != GltfContainerFormat.Glb)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (inputFormat == GltfContainerFormat.Json
            && outputFormat == GltfContainerFormat.Glb)
        {
            GltfConversionResult glbResult = GltfToGlb.ConvertDocumentToGlb(
                conversionDocument,
                options);
            AttachTransformReport(glbResult, transformReport);
            ManifestBuilder.FinalizeResult(document, glbResult, options);
            return glbResult;
        }

        if (inputFormat == GltfContainerFormat.Glb
            && outputFormat == GltfContainerFormat.Json)
        {
            GltfConversionResult jsonResult = GlbToGltf.ConvertDocumentToGltf(
                conversionDocument,
                options);
            AttachTransformReport(jsonResult, transformReport);
            ManifestBuilder.FinalizeResult(document, jsonResult, options);
            return jsonResult;
        }

        GltfConversionResult sameResult = SameContainer.ConvertDocument(
            conversionDocument,
            options,
            inputFormat);
        AttachTransformReport(sameResult, transformReport);
        ManifestBuilder.FinalizeResult(document, sameResult, options);
        return sameResult;
    }

    internal void ValidateConversionOptions(GltfConversionOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.ReadOptions == null
            || options.ReadOptions.Limits == null
            || options.ReadOptions.ResourcePolicy == null
            || options.ReadOptions.ValidationOptions == null)
        {
            throw new System.ArgumentException(
                "The conversion read options, limits, resource policy, and validation options must not be null.",
                "options");
        }

        if (options.WriteOptions == null
            || options.WriteOptions.ValidationOptions == null)
        {
            throw new System.ArgumentException(
                "The conversion write options and validation options must not be null.",
                "options");
        }

        if (options.TransformPlan != null && options.TransformOptions == null)
        {
            throw new System.ArgumentException(
                "The conversion transform options must not be null when a transform plan is supplied.",
                "options");
        }
    }

    internal GltfConversionResult CreateTransformFailureResult(
        GltfTransformReport report)
    {
        GltfConversionResult result = new GltfConversionResult();
        result.Status = report.Status;
        result.Document = report.Document;
        result.Resources = report.Resources;
        result.TransformReport = report;
        AddTransformDiagnostics(result.Diagnostics, report.Diagnostics);
        return result;
    }

    internal GltfConversionResult CreateInputValidationFailureResult(
        GltfDocument document,
        GltfValidationReport validation)
    {
        GltfConversionResult result = new GltfConversionResult();
        result.Status = GltfOperationStatus.Invalid;
        result.Document = document;
        result.Resources = document.Resources;
        int index = 0;
        while (index < validation.Issues.Count)
        {
            result.Diagnostics.Add(validation.Issues[index]);
            index++;
        }

        return result;
    }

    internal void AttachTransformReport(
        GltfConversionResult result,
        GltfTransformReport? report)
    {
        if (report == null)
        {
            return;
        }

        result.TransformReport = report;
        AddTransformDiagnostics(result.Diagnostics, report.Diagnostics);
    }

    internal void AddTransformDiagnostics(
        System.Collections.Generic.List<GltfValidationIssue> target,
        System.Collections.Generic.List<GltfValidationIssue> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal GltfContainerFormat ResolveDocumentInputFormat(
        GltfDocument document,
        GltfConversionOptions options)
    {
        GltfContainerFormat declared = options.InputFormat;
        GltfContainerFormat provenance = GltfContainerFormat.Auto;
        if (document.Container != null)
        {
            provenance = document.Container.Format;
        }

        if (declared == GltfContainerFormat.Auto)
        {
            if (provenance == GltfContainerFormat.Json
                || provenance == GltfContainerFormat.Glb)
            {
                return provenance;
            }

            return GltfContainerFormat.Json;
        }

        if (declared != GltfContainerFormat.Json
            && declared != GltfContainerFormat.Glb)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if ((provenance == GltfContainerFormat.Json
                || provenance == GltfContainerFormat.Glb)
            && provenance != declared)
        {
            throw new GltfWriteException(
                "The requested input format does not match the document container format.");
        }

        return declared;
    }

    internal GltfConversionOptions CreateFileConversionOptions(
        string inputPath,
        string outputPath,
        GltfConversionOptions source)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = source.InputFormat;
        result.OutputFormat = source.OutputFormat;
        if (result.OutputFormat == GltfContainerFormat.Auto)
        {
            string extension = Path.GetExtension(outputPath);
            if (string.Equals(
                extension,
                ".glb",
                System.StringComparison.OrdinalIgnoreCase))
            {
                result.OutputFormat = GltfContainerFormat.Glb;
            }
            else if (string.Equals(
                extension,
                ".gltf",
                System.StringComparison.OrdinalIgnoreCase))
            {
                result.OutputFormat = GltfContainerFormat.Json;
            }
        }

        result.ResourceMode = source.ResourceMode;
        result.EmbedImages = source.EmbedImages;
        result.PartitionBuffers = source.PartitionBuffers;
        result.MaximumBufferByteLength = source.MaximumBufferByteLength;
        result.AtomicOutput = source.AtomicOutput;
        result.OverwriteResources = source.OverwriteResources;
        result.ReadOptions = Reader.CreateFileReadOptions(
            inputPath,
            source.ReadOptions);
        if (source.InputFormat == GltfContainerFormat.Json
            || source.InputFormat == GltfContainerFormat.Glb)
        {
            result.ReadOptions.Format = source.InputFormat;
        }

        result.ReadOptions.ResolveResources = false;
        result.WriteOptions = source.WriteOptions;
        result.TransformPlan = source.TransformPlan;
        result.TransformOptions = source.TransformOptions;
        return result;
    }

    internal GltfConversionOptions CreateMemoryConversionOptions(
        GltfConversionOptions source)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = source.InputFormat;
        result.OutputFormat = source.OutputFormat;
        result.ResourceMode = source.ResourceMode;
        result.EmbedImages = source.EmbedImages;
        result.PartitionBuffers = source.PartitionBuffers;
        result.MaximumBufferByteLength = source.MaximumBufferByteLength;
        result.AtomicOutput = source.AtomicOutput;
        result.OverwriteResources = source.OverwriteResources;
        result.ReadOptions = Reader.CopyConversionReadOptions(
            source.ReadOptions);
        if (source.InputFormat == GltfContainerFormat.Json
            || source.InputFormat == GltfContainerFormat.Glb)
        {
            result.ReadOptions.Format = source.InputFormat;
        }

        result.WriteOptions = source.WriteOptions;
        result.TransformPlan = source.TransformPlan;
        result.TransformOptions = source.TransformOptions;
        return result;
    }

    internal GltfConversionResult CreateReadFailureResult(
        GltfReadResult readResult)
    {
        GltfConversionResult result = new GltfConversionResult();
        result.Status = readResult.Status;
        result.Document = readResult.Document;
        result.Resources = readResult.Resources;
        AddReadDiagnostics(result, readResult);
        return result;
    }

    internal void AddReadDiagnostics(
        GltfConversionResult result,
        GltfReadResult readResult)
    {
        int diagnosticIndex = 0;
        while (diagnosticIndex < readResult.Diagnostics.Count)
        {
            result.Diagnostics.Add(
                readResult.Diagnostics[diagnosticIndex]);
            diagnosticIndex++;
        }

        int issueIndex = 0;
        while (issueIndex < readResult.Validation.Issues.Count)
        {
            result.Diagnostics.Add(
                readResult.Validation.Issues[issueIndex]);
            issueIndex++;
        }
    }
}
