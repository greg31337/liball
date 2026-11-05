using System.Collections.Generic;

namespace odf;

internal class OdfVersionServiceCore
{
    OdfValidationOptions Options;
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    List<VersionMigrationStep> Steps = new List<VersionMigrationStep>();
    bool VersionWasInferred;
    bool PreservesFutureContent;

    internal OdfVersionServiceCore(OdfValidationOptions options)
    {
        if (options == null)
        {
            throw new OdfValidationException(
                "Validation options are required for version analysis.");
        }
        Options = options;
    }

    internal VersionAnalysisResult ValidateVersionGates(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for version analysis.");
        }
        return AnalyzeMigration(document, Options.Version);
    }

    internal VersionAnalysisResult AnalyzeMigration(
        OdfDocument document,
        OdfVersion targetVersion)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for version analysis.");
        }
        Diagnostics = new List<OdfDiagnostic>();
        Steps = new List<VersionMigrationStep>();
        VersionWasInferred = false;
        PreservesFutureContent = false;
        OdfVersion sourceVersion = SelectSourceVersion(document);
        ValidatePartVersions(document, sourceVersion);
        OdfVersion selectedTarget = targetVersion;
        if (selectedTarget == OdfVersion.Unknown)
        {
            selectedTarget = sourceVersion;
        }
        if (sourceVersion == OdfVersion.Future)
        {
            AnalyzeFutureVersion(document, selectedTarget);
        }
        else if (!IsKnownVersion(sourceVersion))
        {
            if (Diagnostics.Count == 0)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.VersionUnsupported,
                    "The document does not declare a supported ODF version.",
                    OdfValidationSeverity.Error,
                    null,
                    sourceVersion,
                    false);
            }
        }
        else if (!IsKnownVersion(selectedTarget))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.VersionUnsupported,
                "The requested target is not a supported ODF version.",
                OdfValidationSeverity.Error,
                null,
                selectedTarget,
                false);
        }
        else if (sourceVersion != selectedTarget)
        {
            BuildMigrationSteps(document, sourceVersion, selectedTarget);
        }
        return CreateResult(sourceVersion, selectedTarget);
    }

    OdfVersion SelectSourceVersion(OdfDocument document)
    {
        OdfVersionInfo? versionInfo = document.VersionInfo;
        if (versionInfo == null)
        {
            int index = 0;
            while (index < document.Parts.Length && versionInfo == null)
            {
                versionInfo = document.Parts[index].VersionInfo;
                index = index + 1;
            }
        }
        if (versionInfo == null)
        {
            VersionWasInferred = true;
            return OdfVersion.Version10;
        }
        if (!versionInfo.IsValid)
        {
            AddDiagnostic(
                OdfDiagnosticCodes.VersionUnsupported,
                "The declared ODF version is not a valid version value.",
                OdfValidationSeverity.Error,
                null,
                OdfVersion.Unknown,
                false);
            return OdfVersion.Unknown;
        }
        return versionInfo.Version;
    }

    void ValidatePartVersions(OdfDocument document, OdfVersion sourceVersion)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.VersionInfo != null)
            {
                if (!part.VersionInfo.IsValid)
                {
                    AddDiagnostic(
                        OdfDiagnosticCodes.VersionUnsupported,
                        "An XML part declares an invalid ODF version.",
                        OdfValidationSeverity.Error,
                        PartLocation(part),
                        OdfVersion.Unknown,
                        false);
                }
                else if (sourceVersion != OdfVersion.Unknown
                    && VersionsDiffer(
                        document,
                        part.VersionInfo,
                        sourceVersion))
                {
                    AddDiagnostic(
                        OdfDiagnosticCodes.VersionUnsupported,
                        "The XML part version differs from the document version.",
                        OdfValidationSeverity.Error,
                        PartLocation(part),
                        part.VersionInfo.Version,
                        false);
                }
            }
            index = index + 1;
        }
    }

    bool VersionsDiffer(
        OdfDocument document,
        OdfVersionInfo partVersion,
        OdfVersion sourceVersion)
    {
        if (partVersion.Version != sourceVersion)
        {
            return true;
        }
        if (sourceVersion != OdfVersion.Future)
        {
            return false;
        }
        OdfVersionInfo? sourceInfo = document.VersionInfo;
        if (sourceInfo == null)
        {
            int index = 0;
            while (index < document.Parts.Length && sourceInfo == null)
            {
                sourceInfo = document.Parts[index].VersionInfo;
                index = index + 1;
            }
        }
        return sourceInfo != null
            && OdfVersionRules.CompareVersions(sourceInfo, partVersion) != 0;
    }

    void AnalyzeFutureVersion(
        OdfDocument document,
        OdfVersion targetVersion)
    {
        bool preserve = document.LoadOptions.PreserveUnknownContent
            && document.SaveOptions.PreserveUnknownContent;
        if (targetVersion == OdfVersion.Future && preserve)
        {
            PreservesFutureContent = true;
            AddDiagnostic(
                OdfDiagnosticCodes.VersionUnsupported,
                "The future ODF version is preserved without schema conversion.",
                OdfValidationSeverity.Warning,
                null,
                OdfVersion.Future,
                true);
            return;
        }
        if (IsKnownVersion(targetVersion))
        {
            AddDiagnostic(
                OdfDiagnosticCodes.DowngradeLoss,
                "Converting a future ODF version to a known version may lose unknown content.",
                OdfValidationSeverity.Error,
                null,
                targetVersion,
                false);
            return;
        }
        AddDiagnostic(
            OdfDiagnosticCodes.VersionUnsupported,
            "The future ODF version cannot be validated when unknown-content preservation is disabled.",
            OdfValidationSeverity.Error,
            null,
            OdfVersion.Future,
            false);
    }

    void BuildMigrationSteps(
        OdfDocument document,
        OdfVersion sourceVersion,
        OdfVersion targetVersion)
    {
        bool downgrade = (int)sourceVersion > (int)targetVersion;
        OdfVersion current = sourceVersion;
        while (current != targetVersion)
        {
            OdfVersion previousVersion = current;
            OdfVersion nextVersion = current;
            if (downgrade)
            {
                nextVersion = (OdfVersion)((int)current - 1);
                previousVersion = nextVersion;
            }
            else
            {
                nextVersion = (OdfVersion)((int)current + 1);
            }
            SchemaVersionDelta? delta = OdfSchemaRuleCatalog.GetVersionDelta(
                previousVersion,
                downgrade ? current : nextVersion);
            if (delta == null)
            {
                AddDiagnostic(
                    OdfDiagnosticCodes.VersionUnsupported,
                    "No schema delta is available for an ODF migration step.",
                    OdfValidationSeverity.Error,
                    null,
                    nextVersion,
                    false);
                return;
            }
            VersionMigrationStep step = CreateStep(
                delta,
                current,
                nextVersion,
                downgrade);
            FindIncompatibleUses(document, delta, step);
            Steps.Add(step);
            current = nextVersion;
        }
    }

    VersionMigrationStep CreateStep(
        SchemaVersionDelta delta,
        OdfVersion sourceVersion,
        OdfVersion targetVersion,
        bool downgrade)
    {
        VersionMigrationStep result = new VersionMigrationStep();
        result.SourceVersion = sourceVersion;
        result.TargetVersion = targetVersion;
        result.IsDowngrade = downgrade;
        result.Changes = delta.Changes;
        int index = 0;
        while (index < delta.Changes.Length)
        {
            SchemaRuleDelta change = delta.Changes[index];
            if (change.Profile == "document")
            {
                if (change.Kind == SchemaRuleDeltaKind.Added)
                {
                    result.AddedCount = result.AddedCount + 1;
                }
                else if (change.Kind == SchemaRuleDeltaKind.Removed)
                {
                    result.RemovedCount = result.RemovedCount + 1;
                }
                else
                {
                    result.ChangedCount = result.ChangedCount + 1;
                }
            }
            index = index + 1;
        }
        return result;
    }

    void FindIncompatibleUses(
        OdfDocument document,
        SchemaVersionDelta delta,
        VersionMigrationStep step)
    {
        SchemaRuleDeltaKind incompatibleKind = SchemaRuleDeltaKind.Removed;
        string code = OdfDiagnosticCodes.FeatureNotAvailable;
        if (step.IsDowngrade)
        {
            incompatibleKind = SchemaRuleDeltaKind.Added;
            code = OdfDiagnosticCodes.DowngradeLoss;
        }
        int changeIndex = 0;
        while (changeIndex < delta.Changes.Length)
        {
            SchemaRuleDelta change = delta.Changes[changeIndex];
            if (change.Profile == "document"
                && change.Kind == incompatibleKind
                && (change.Category == SchemaRuleDeltaCategory.Element
                    || change.Category
                        == SchemaRuleDeltaCategory.Attribute))
            {
                int partIndex = 0;
                while (partIndex < document.Parts.Length)
                {
                    InspectPartForChange(
                        document.Parts[partIndex],
                        change,
                        step,
                        code);
                    partIndex = partIndex + 1;
                }
            }
            changeIndex = changeIndex + 1;
        }
    }

    void InspectPartForChange(
        OdfPart part,
        SchemaRuleDelta change,
        VersionMigrationStep step,
        string code)
    {
        int index = 0;
        while (index < part.Nodes.Length)
        {
            OdfNode node = part.Nodes[index];
            string path = "/" + NodeName(node) + "["
                + (index + 1) + "]";
            InspectNodeForChange(node, part.Path, path, change, step, code);
            index = index + 1;
        }
    }

    void InspectNodeForChange(
        OdfNode node,
        string partPath,
        string nodePath,
        SchemaRuleDelta change,
        VersionMigrationStep step,
        string code)
    {
        if (change.Category == SchemaRuleDeltaCategory.Element
            && node.Name != null
            && node.Name.NamespaceName == change.NamespaceName
            && node.Name.LocalName == change.Name)
        {
            RecordIncompatibleUse(
                change,
                step,
                code,
                NodeLocation(node, partPath, nodePath));
        }
        if (change.Category == SchemaRuleDeltaCategory.Attribute)
        {
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Length)
            {
                OdfAttribute attribute = node.Attributes[attributeIndex];
                if (attribute.Name.NamespaceName == change.NamespaceName
                    && attribute.Name.LocalName == change.Name)
                {
                    OdfLocation location = OdfDiagnosticFactory.CopyLocation(
                        attribute.Location);
                    location.PartPath = partPath;
                    location.NodePath = nodePath + "/@"
                        + QualifiedName(attribute.Name);
                    RecordIncompatibleUse(change, step, code, location);
                }
                attributeIndex = attributeIndex + 1;
            }
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            OdfNode child = node.Children[childIndex];
            string childPath = nodePath + "/" + NodeName(child) + "["
                + (childIndex + 1) + "]";
            InspectNodeForChange(
                child,
                partPath,
                childPath,
                change,
                step,
                code);
            childIndex = childIndex + 1;
        }
    }

    void RecordIncompatibleUse(
        SchemaRuleDelta change,
        VersionMigrationStep step,
        string code,
        OdfLocation location)
    {
        step.IncompatibleConstructCount =
            step.IncompatibleConstructCount + 1;
        string category = "attribute";
        if (change.Category == SchemaRuleDeltaCategory.Element)
        {
            category = "element";
        }
        string message = "The " + category + " '"
            + change.Name + "' is not available in target ODF "
            + VersionText(step.TargetVersion) + ".";
        AddDiagnostic(
            code,
            message,
            OdfValidationSeverity.Error,
            location,
            step.TargetVersion,
            false);
    }

    VersionAnalysisResult CreateResult(
        OdfVersion sourceVersion,
        OdfVersion targetVersion)
    {
        VersionAnalysisResult result = new VersionAnalysisResult();
        result.SourceVersion = sourceVersion;
        result.TargetVersion = targetVersion;
        result.IsUpgrade = IsKnownVersion(sourceVersion)
            && IsKnownVersion(targetVersion)
            && (int)sourceVersion < (int)targetVersion;
        result.IsDowngrade = IsKnownVersion(sourceVersion)
            && IsKnownVersion(targetVersion)
            && (int)sourceVersion > (int)targetVersion;
        result.Steps = Steps.ToArray();
        result.Diagnostics = Diagnostics.ToArray();
        result.VersionWasInferred = VersionWasInferred;
        result.PreservesFutureContent = PreservesFutureContent;
        result.IsSupported = IsKnownVersion(sourceVersion)
            && IsKnownVersion(targetVersion);
        result.IsValid = !HasErrors();
        result.CanConvertWithoutLoss = result.IsValid
            && sourceVersion != OdfVersion.Future;
        return result;
    }

    void AddDiagnostic(
        string code,
        string message,
        OdfValidationSeverity severity,
        OdfLocation? location,
        OdfVersion version,
        bool recoverable)
    {
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            severity,
            location,
            null,
            version);
        diagnostic.IsRecoverable = recoverable;
        Diagnostics.Add(diagnostic);
    }

    bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            OdfValidationSeverity severity = Diagnostics[index].Severity;
            if (severity == OdfValidationSeverity.Error
                || severity == OdfValidationSeverity.Fatal)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    OdfLocation PartLocation(OdfPart part)
    {
        OdfLocation result = new OdfLocation();
        result.PartPath = part.Path;
        return result;
    }

    OdfLocation NodeLocation(
        OdfNode node,
        string partPath,
        string nodePath)
    {
        OdfLocation result = OdfDiagnosticFactory.CopyLocation(node.Location);
        result.PartPath = partPath;
        result.NodePath = nodePath;
        result.XmlId = node.XmlId;
        return result;
    }

    string NodeName(OdfNode node)
    {
        if (node.Name == null)
        {
            return "node";
        }
        return QualifiedName(node.Name);
    }

    string QualifiedName(OdfQualifiedName name)
    {
        if (name.Prefix.Length == 0)
        {
            return name.LocalName;
        }
        return name.Prefix + ":" + name.LocalName;
    }

    string VersionText(OdfVersion version)
    {
        if (version == OdfVersion.Version10)
        {
            return "1.0";
        }
        if (version == OdfVersion.Version11)
        {
            return "1.1";
        }
        if (version == OdfVersion.Version12)
        {
            return "1.2";
        }
        if (version == OdfVersion.Version13)
        {
            return "1.3";
        }
        if (version == OdfVersion.Version14)
        {
            return "1.4";
        }
        return "unknown";
    }

    bool IsKnownVersion(OdfVersion version)
    {
        return version == OdfVersion.Version10
            || version == OdfVersion.Version11
            || version == OdfVersion.Version12
            || version == OdfVersion.Version13
            || version == OdfVersion.Version14;
    }
}
