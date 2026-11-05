namespace odf;

/// <summary>
/// Provides ODF version-gate, migration, and downgrade-loss analysis.
/// </summary>
public class OdfVersionService : IOdfVersionService
{
    /// <summary>
    /// Initializes a version service.
    /// </summary>
    public OdfVersionService()
    {
    }

    /// <summary>
    /// Validates document constructs against the version selected by the options.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="options">The validation and target-version options.</param>
    /// <returns>The version analysis.</returns>
    public OdfVersionAnalysis ValidateDocumentVersion(
        OdfDocument document,
        OdfValidationOptions options)
    {
        return ConvertVersionAnalysis(
            new OdfVersionServiceCore(options)
                .ValidateVersionGates(document));
    }

    /// <summary>
    /// Analyzes migration from the document version to a target version.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="targetVersion">The requested target version.</param>
    /// <returns>The ordered migration analysis and loss diagnostics.</returns>
    public OdfVersionAnalysis AnalyzeVersionMigration(
        OdfDocument document,
        OdfVersion targetVersion)
    {
        OdfValidationOptions options = new OdfValidationOptions();
        options.Version = targetVersion;
        return ConvertVersionAnalysis(
            new OdfVersionServiceCore(options)
                .AnalyzeMigration(document, targetVersion));
    }

    internal OdfVersionAnalysis ConvertVersionAnalysis(
        VersionAnalysisResult analysis)
    {
        OdfVersionAnalysis result = new OdfVersionAnalysis();
        result.IsValid = analysis.IsValid;
        result.IsSupported = analysis.IsSupported;
        result.IsUpgrade = analysis.IsUpgrade;
        result.IsDowngrade = analysis.IsDowngrade;
        result.CanConvertWithoutLoss = analysis.CanConvertWithoutLoss;
        result.PreservesFutureContent = analysis.PreservesFutureContent;
        result.VersionWasInferred = analysis.VersionWasInferred;
        result.SourceVersion = analysis.SourceVersion;
        result.TargetVersion = analysis.TargetVersion;
        result.Diagnostics = analysis.Diagnostics;
        result.Steps = new OdfVersionMigrationStep[analysis.Steps.Length];
        int index = 0;
        while (index < analysis.Steps.Length)
        {
            result.Steps[index] = ConvertVersionStep(
                analysis.Steps[index]);
            index = index + 1;
        }
        return result;
    }

    internal OdfVersionMigrationStep ConvertVersionStep(
        VersionMigrationStep step)
    {
        OdfVersionMigrationStep result = new OdfVersionMigrationStep();
        result.SourceVersion = step.SourceVersion;
        result.TargetVersion = step.TargetVersion;
        result.IsDowngrade = step.IsDowngrade;
        result.AddedCount = step.AddedCount;
        result.RemovedCount = step.RemovedCount;
        result.ChangedCount = step.ChangedCount;
        result.IncompatibleConstructCount =
            step.IncompatibleConstructCount;
        result.Changes = new OdfVersionChange[step.Changes.Length];
        int index = 0;
        while (index < step.Changes.Length)
        {
            result.Changes[index] = ConvertVersionChange(
                step.Changes[index]);
            index = index + 1;
        }
        return result;
    }

    internal OdfVersionChange ConvertVersionChange(SchemaRuleDelta change)
    {
        OdfVersionChange result = new OdfVersionChange();
        result.Profile = change.Profile;
        result.Kind = ConvertVersionChangeKind(change.Kind);
        result.Category = ConvertVersionChangeCategory(change.Category);
        result.NamespaceName = change.NamespaceName;
        result.Name = change.Name;
        return result;
    }

    internal OdfVersionChangeKind ConvertVersionChangeKind(
        SchemaRuleDeltaKind kind)
    {
        if (kind == SchemaRuleDeltaKind.Added)
        {
            return OdfVersionChangeKind.Added;
        }
        if (kind == SchemaRuleDeltaKind.Removed)
        {
            return OdfVersionChangeKind.Removed;
        }
        if (kind == SchemaRuleDeltaKind.Changed)
        {
            return OdfVersionChangeKind.Changed;
        }
        return OdfVersionChangeKind.Unknown;
    }

    internal OdfVersionChangeCategory ConvertVersionChangeCategory(
        SchemaRuleDeltaCategory category)
    {
        if (category == SchemaRuleDeltaCategory.Definition)
        {
            return OdfVersionChangeCategory.Definition;
        }
        if (category == SchemaRuleDeltaCategory.Element)
        {
            return OdfVersionChangeCategory.Element;
        }
        if (category == SchemaRuleDeltaCategory.Attribute)
        {
            return OdfVersionChangeCategory.Attribute;
        }
        if (category == SchemaRuleDeltaCategory.Datatype)
        {
            return OdfVersionChangeCategory.Datatype;
        }
        return OdfVersionChangeCategory.Unknown;
    }
}
