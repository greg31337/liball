using System;
using System.IO;

namespace gltf;

/// <summary>Provides structural, semantic, resource, and extension validation.</summary>
public class GltfValidator : IGltfValidator
{
    internal RawContainerValidator RawContainers;
    internal JsonSyntaxValidator JsonSyntax;
    internal GltfJsonReader JsonReader;
    internal GltfSchemaReader SchemaReader;
    internal StructuralValidator Structural;
    internal DocumentRuleValidator DocumentRules;
    internal BufferAccessorValidator BufferAccessors;
    internal MeshPrimitiveValidator Meshes;
    internal MaterialTextureValidator Materials;
    internal SceneNodeCameraValidator Scenes;
    internal SkinValidator Skins;
    internal AnimationValidator Animations;
    internal ResourceValidator Resources;
    internal ValidationIssueAggregator Issues;
    internal ValidationStatisticsCollector Statistics;
    internal ExtensionSemanticValidator Extensions;

    /// <summary>Initializes a glTF validator.</summary>
    public GltfValidator()
    {
        RawContainers = new RawContainerValidator();
        JsonSyntax = new JsonSyntaxValidator();
        JsonReader = new GltfJsonReader();
        SchemaReader = new GltfSchemaReader();
        Structural = new StructuralValidator();
        DocumentRules = new DocumentRuleValidator();
        BufferAccessors = new BufferAccessorValidator();
        Meshes = new MeshPrimitiveValidator();
        Materials = new MaterialTextureValidator();
        Scenes = new SceneNodeCameraValidator();
        Skins = new SkinValidator();
        Animations = new AnimationValidator();
        Resources = new ResourceValidator();
        Issues = new ValidationIssueAggregator();
        Statistics = new ValidationStatisticsCollector();
        Extensions = new ExtensionSemanticValidator();
    }

    /// <summary>Validates a constructed document.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    public GltfValidationReport ValidateDocument(GltfDocument document, GltfValidationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        ValidationIssueAggregation aggregation = Issues.CreateAggregation(options);
        AppendDocumentValidation(aggregation, document, options, true);
        GltfValidationReport report = Issues.CompleteAggregation(aggregation);
        Statistics.PopulateValidationStatistics(document, report);
        return CompleteValidation(report, options);
    }

    /// <summary>Validates a glTF or GLB file.</summary>
    /// <param name="path">The input file path.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    public GltfValidationReport ValidateFile(string path, GltfValidationOptions options)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        Issues.ValidateOptions(options);
        if (!File.Exists(path))
        {
            GltfValidationReport missingReport = new GltfValidationReport();
            GltfValidationIssue missingIssue = new GltfValidationIssue();
            missingIssue.Code = GltfIssueCodes.IoError;
            missingIssue.Severity = GltfValidationSeverity.Error;
            missingIssue.Message = $"The input file '{path}' does not exist.";
            missingIssue.ResourceUri = path;
            missingReport.Issues.Add(missingIssue);
            missingReport.IsValid = false;
            return AggregateExternalReport(missingReport, options);
        }

        FileInfo information = new FileInfo(path);
        if (information.Length > GltfConstants.DefaultMaximumInputByteLength)
        {
            GltfValidationReport limitReport = new GltfValidationReport();
            GltfValidationIssue limitIssue = new GltfValidationIssue();
            limitIssue.Code = GltfIssueCodes.InputLimitExceeded;
            limitIssue.Severity = GltfValidationSeverity.Error;
            limitIssue.Message = $"Input length {information.Length} exceeds the supported maximum {GltfConstants.DefaultMaximumInputByteLength}.";
            limitIssue.ResourceUri = path;
            limitReport.Issues.Add(limitIssue);
            limitReport.IsValid = false;
            return AggregateExternalReport(limitReport, options);
        }

        byte[] bytes = File.ReadAllBytes(path);
        return ValidateBytes(bytes, options);
    }

    /// <summary>Validates glTF or GLB bytes.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    public GltfValidationReport ValidateBytes(byte[] bytes, GltfValidationOptions options)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        Issues.ValidateOptions(options);
        RawContainerValidationResult raw = RawContainers.ValidateBytes(bytes);
        return ValidateRawContainer(raw, options);
    }

    /// <summary>Validates parsed container data.</summary>
    /// <param name="container">The parsed container data.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    public GltfValidationReport ValidateContainer(GltfContainerData container, GltfValidationOptions options)
    {
        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        Issues.ValidateOptions(options);
        RawContainerValidationResult raw = RawContainers.ValidateContainer(container);
        return ValidateRawContainer(raw, options);
    }

    internal GltfValidationReport ValidateRawContainer(
        RawContainerValidationResult raw,
        GltfValidationOptions options)
    {
        ValidationIssueAggregation aggregation = Issues.CreateAggregation(options);
        Issues.AppendReport(aggregation, raw.Report);
        if (aggregation.Stopped || !raw.Report.IsValid || raw.Container == null)
        {
            return CompleteValidation(Issues.CompleteAggregation(aggregation), options);
        }

        GltfContainerData container = raw.Container;
        GltfValidationReport syntaxReport = JsonSyntax.ValidateJson(container.JsonBytes);
        Issues.AppendReport(aggregation, syntaxReport);
        if (aggregation.Stopped || !syntaxReport.IsValid)
        {
            return CompleteValidation(Issues.CompleteAggregation(aggregation), options);
        }

        GltfReadLimits limits = new GltfReadLimits();
        GltfJsonReadResult json = JsonReader.Read(
            container.JsonBytes,
            limits,
            GltfDuplicateMemberPolicy.KeepLast);
        GltfValidationReport duplicateReport = ValidateDuplicateMembers(json);
        Issues.AppendReport(aggregation, duplicateReport);
        if (aggregation.Stopped || !duplicateReport.IsValid)
        {
            return CompleteValidation(Issues.CompleteAggregation(aggregation), options);
        }

        GltfValidationReport rawStructure = Structural.ValidateParsedStructure(json);
        Issues.AppendReport(aggregation, rawStructure);
        if (aggregation.Stopped || !rawStructure.IsValid)
        {
            return CompleteValidation(Issues.CompleteAggregation(aggregation), options);
        }

        GltfDocument document = SchemaReader.ReadDocument(
            json,
            limits,
            true,
            options.ExtensionHandlers);
        document.Container = container;
        AppendDocumentValidation(aggregation, document, options, false);
        GltfValidationReport report = Issues.CompleteAggregation(aggregation);
        Statistics.PopulateValidationStatistics(document, report);
        return CompleteValidation(report, options);
    }

    internal GltfValidationReport ValidateDuplicateMembers(GltfJsonReadResult json)
    {
        GltfValidationReport report = new GltfValidationReport();
        int tokenIndex = 0;
        while (tokenIndex < json.Tokens.Count)
        {
            GltfJsonToken token = json.Tokens[tokenIndex];
            if (token.DuplicateMember)
            {
                GltfValidationIssue issue = new GltfValidationIssue();
                issue.Code = GltfIssueCodes.JsonDuplicateMember;
                issue.Severity = GltfValidationSeverity.Error;
                issue.Message = "A JSON object member name is duplicated.";
                issue.JsonPointer = token.JsonPointer;
                issue.HasByteOffset = true;
                issue.ByteOffset = token.ByteOffset;
                report.Issues.Add(issue);
                report.IsValid = false;
            }

            tokenIndex++;
        }

        return report;
    }

    internal void AppendDocumentValidation(
        ValidationIssueAggregation aggregation,
        GltfDocument document,
        GltfValidationOptions options,
        bool includeStructure)
    {
        if (includeStructure && options.ValidateStructure && !aggregation.Stopped)
        {
            Issues.AppendReport(aggregation, Structural.ValidateModelStructure(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(aggregation, DocumentRules.ValidateDocumentRules(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(aggregation, BufferAccessors.ValidateBufferAccessorRules(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(aggregation, Meshes.ValidateDocumentMeshes(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(
                aggregation,
                Materials.ValidateDocumentMaterialTextureRules(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(
                aggregation,
                Scenes.ValidateDocumentSceneNodeCameraRules(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(aggregation, Skins.ValidateDocumentSkinRules(document));
        }

        if (options.ValidateSemantics && !aggregation.Stopped)
        {
            Issues.AppendReport(aggregation, Animations.ValidateDocumentAnimationRules(document));
        }

        if (options.ValidateResources && !aggregation.Stopped)
        {
            Issues.AppendReport(
                aggregation,
                Resources.ValidateDocumentResourceRules(document, options));
        }

        if (options.ValidateExtensions && !aggregation.Stopped)
        {
            Issues.AppendReport(
                aggregation,
                Extensions.ValidateDocumentExtensions(document, options.ExtensionHandlers));
        }
    }

    internal GltfValidationReport CompleteValidation(
        GltfValidationReport report,
        GltfValidationOptions options)
    {
        if (options.Strict && !report.IsValid)
        {
            throw new GltfValidationException(CreateStrictMessage(report));
        }

        return report;
    }

    internal GltfValidationReport AggregateExternalReport(
        GltfValidationReport source,
        GltfValidationOptions options)
    {
        ValidationIssueAggregation aggregation = Issues.CreateAggregation(options);
        Issues.AppendReport(aggregation, source);
        return CompleteValidation(Issues.CompleteAggregation(aggregation), options);
    }

    internal string CreateStrictMessage(GltfValidationReport report)
    {
        int errorCount = 0;
        GltfValidationIssue? firstError = null;
        int issueIndex = 0;
        while (issueIndex < report.Issues.Count)
        {
            GltfValidationIssue issue = report.Issues[issueIndex];
            if (issue.Severity == GltfValidationSeverity.Error)
            {
                errorCount++;
                if (firstError == null)
                {
                    firstError = issue;
                }
            }

            issueIndex++;
        }

        if (firstError == null)
        {
            return "glTF validation failed.";
        }

        string location = firstError.JsonPointer.Length == 0
            ? "the input"
            : $"'{firstError.JsonPointer}'";
        return $"glTF validation failed with {errorCount} error(s); first issue {firstError.Code} at {location}: {firstError.Message}";
    }
}
