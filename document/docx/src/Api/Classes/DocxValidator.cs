using System.IO;
using System.Threading.Tasks;

namespace docx;

/// <summary>
/// Provides high-level validation and safe repair operations for DOCX packages.
/// </summary>
public class DocxValidator : IDocxValidator
{
    internal DocxPackageValidator PackageValidator;
    internal DocxRepair Repair;

    /// <summary>
    /// Creates a validator instance.
    /// </summary>
    public DocxValidator()
    {
        this.PackageValidator = new DocxPackageValidator();
        this.Repair = new DocxRepair();
    }

    /// <summary>
    /// Inspects package bytes for structural, compatibility, and recoverability observations without modifying them.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <returns>The package diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxPackageDiagnostics GetStreamDiagnostics(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        byte[] bytes = ReadDiagnosticBytes(stream);
        DocxPackageDiagnostics diagnostics = new DocxPackageDiagnostics();
        Task<DocxValidationReport> validationTask = Task.Run(() => ValidateDiagnosticBytes(bytes));
        WaitForDiagnosticTask(validationTask);

        if (validationTask.IsFaulted)
        {
            AddCorruptInputDiagnostic(diagnostics, validationTask);
            return diagnostics;
        }

        diagnostics.Report = validationTask.Result;
        SetDiagnosticFlags(diagnostics);
        Task<DocxPackage> openTask = Task.Run(() => OpenDiagnosticBytes(bytes));
        WaitForDiagnosticTask(openTask);

        if (openTask.IsFaulted)
        {
            AddCorruptInputDiagnostic(diagnostics, openTask);
            return diagnostics;
        }

        DocxPackageDiagnostics packageDiagnostics = CreatePackageDiagnostics(openTask.Result);
        this.PackageValidator.AppendReport(diagnostics.Report, packageDiagnostics.Report);
        diagnostics.IsReadablePackage = true;
        diagnostics.HasMacroEnabledContent = packageDiagnostics.HasMacroEnabledContent;
        diagnostics.NamespaceConformance = packageDiagnostics.NamespaceConformance;
        diagnostics.HasRecoverableDefects = packageDiagnostics.HasRecoverableDefects;
        diagnostics.RecoverableDefects = packageDiagnostics.RecoverableDefects;
        SetDiagnosticFlags(diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Inspects a loaded package for structural, compatibility, and recoverability observations without modifying it.
    /// </summary>
    /// <param name="package">The package to inspect.</param>
    /// <returns>The package diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxPackageDiagnostics GetPackageDiagnostics(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return CreatePackageDiagnostics(package.CreateDiagnosticCopy());
    }

    internal DocxPackageDiagnostics CreatePackageDiagnostics(DocxPackage package)
    {
        DocxPackageDiagnostics diagnostics = new DocxPackageDiagnostics();
        diagnostics.IsReadablePackage = true;
        diagnostics.Report = this.PackageValidator.ValidatePackage(package);
        this.PackageValidator.AppendReport(
            diagnostics.Report,
            this.PackageValidator.ValidateWordprocessingRequiredParts(package));
        diagnostics.HasMacroEnabledContent = DetectMacroEnabledContent(package);
        diagnostics.NamespaceConformance = this.PackageValidator.DetectNamespaceConformance(package);

        DocxPackage repairCopy = package.CreateDiagnosticCopy();
        DocxValidationReport recoverableDefects = new DocxValidationReport();
        this.PackageValidator.AppendReport(recoverableDefects, this.Repair.RepairMissingContentTypes(repairCopy));
        this.PackageValidator.AppendReport(recoverableDefects, this.Repair.RepairMissingRelationshipIds(repairCopy));
        this.PackageValidator.AppendReport(recoverableDefects, this.Repair.RepairMissingRelationships(repairCopy));

        if (!repairCopy.HasMainDocumentPart())
        {
            this.PackageValidator.AppendReport(recoverableDefects, this.Repair.RepairEmptyBody(repairCopy));
        }

        this.PackageValidator.AppendReport(recoverableDefects, this.Repair.RepairOrphanedRelationships(repairCopy));
        diagnostics.RecoverableDefects = recoverableDefects;
        diagnostics.HasRecoverableDefects = recoverableDefects.Errors.Count > 0;
        SetDiagnosticFlags(diagnostics);
        return diagnostics;
    }

    internal bool DetectMacroEnabledContent(IDocxPackageInspector package)
    {
        for (int partIndex = 0; partIndex < package.PartCount(); partIndex = partIndex + 1)
        {
            DocxPart part = package.GetPart(partIndex);

            if (part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
                || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument
                || part.ContentType == DocxConstants.ContentTypeVbaProject
                || part.PartName.EndsWith("/vbaProject.bin"))
            {
                return true;
            }
        }

        for (int setIndex = 0; setIndex < package.RelationshipSetCount(); setIndex = setIndex + 1)
        {
            DocxRelationshipSet relationshipSet = package.GetRelationshipSet(setIndex);

            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (relationship.Type == DocxConstants.RelationshipTypeVbaProject)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal void SetDiagnosticFlags(DocxPackageDiagnostics diagnostics)
    {
        foreach (DocxError error in diagnostics.Report.Errors)
        {
            if (error.Code == DocxPackageValidator.DuplicateZipEntryCode
                || error.Code == DocxPackageValidator.DuplicatePartNameCode)
            {
                diagnostics.HasDuplicateZipEntries = true;
            }

            if (error.Code == DocxPackageValidator.MissingContentTypesCode
                || error.Code == DocxPackageValidator.MissingPackageRelationshipsCode
                || error.Code == DocxPackageValidator.MissingMainDocumentRelationshipCode
                || error.Code == DocxPackageValidator.MissingMainDocumentPartCode)
            {
                diagnostics.HasMissingRequiredItems = true;
            }
        }
    }

    internal void AddCorruptInputDiagnostic(DocxPackageDiagnostics diagnostics, Task task)
    {
        string message = "The input could not be read as an OPC package.";

        if (task.Exception != null)
        {
            message = message + " " + task.Exception.GetBaseException().Message;
        }

        diagnostics.HasCorruptInput = true;
        diagnostics.IsReadablePackage = false;
        diagnostics.Report.Add(
            DocxErrorSeverity.Error,
            DocxDiagnosticCodes.CorruptInput,
            message,
            "/");
    }

    internal byte[] ReadDiagnosticBytes(Stream stream)
    {
        long position = 0;

        if (stream.CanSeek)
        {
            position = stream.Position;
        }

        byte[] bytes = DocxPackage.ReadStreamBytes(stream);

        if (stream.CanSeek)
        {
            stream.Position = position;
        }

        return bytes;
    }

    internal DocxValidationReport ValidateDiagnosticBytes(byte[] bytes)
    {
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return this.PackageValidator.Validate(stream);
        }
    }

    internal DocxPackage OpenDiagnosticBytes(byte[] bytes)
    {
        DocxLoadOptions options = new DocxLoadOptions();
        options.LazyLoadXmlParts = true;
        options.LazyLoadBinaryParts = true;
        options.ValidationMode = DocxValidationMode.None;
        options.RepairMode = DocxRepairMode.Disabled;

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return DocxPackage.Open(stream, options);
        }
    }

    internal void WaitForDiagnosticTask(Task task)
    {
        while (!task.IsCompleted)
        {
        }
    }

    /// <summary>
    /// Checks package and WordprocessingML consistency without changing the supplied data.
    /// </summary>
    /// <param name="stream">The stream to read from or write to. The caller retains ownership of the stream.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport Validate(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        return GetStreamDiagnostics(stream).Report;
    }

    /// <summary>
    /// Checks package and WordprocessingML consistency without changing the supplied data.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidatePackage(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxValidationReport report = this.PackageValidator.ValidatePackage(package);
        this.PackageValidator.AppendReport(report, this.PackageValidator.ValidateWordprocessingRequiredParts(package));
        return report;
    }

    /// <summary>
    /// Checks that the package has one main-document relationship and a reachable main document part.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateWordprocessingRequiredParts(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateWordprocessingRequiredParts(package);
    }

    /// <summary>
    /// Checks content-type syntax, duplicates, missing mappings, and mappings that disagree with known relationships.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateContentTypes(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateContentTypes(package);
    }

    /// <summary>
    /// Parses every XML part and reports malformed XML without modifying the package.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateXmlWellFormedness(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateXmlWellFormedness(package);
    }

    /// <summary>
    /// Checks supported style, numbering, note, comment, bookmark, media, and relationship references.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateReferences(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateReferences(package);
    }

    /// <summary>
    /// Checks relationship target modes and verifies that internal targets resolve to package parts.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateRelationshipTargets(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateRelationshipTargets(package);
    }

    /// <summary>
    /// Checks recognized WordprocessingML part roots, required body structure, and required attributes.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateWordprocessingStructure(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateWordprocessingStructure(package);
    }

    /// <summary>
    /// Checks unique identifiers, numbering-definition links, and bookmark and comment ranges.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateIdentifiersAndRanges(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateIdentifiersAndRanges(package);
    }

    /// <summary>
    /// Checks compatibility-mode settings supported by WordprocessingML consumers.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateCompatibility(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateCompatibility(package);
    }

    /// <summary>
    /// Checks that Strict and Transitional namespace families are not mixed.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateNamespaceConformance(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateNamespaceConformance(package);
    }

    /// <summary>
    /// Checks macro-enabled main-document, VBA part, and relationship consistency.
    /// </summary>
    /// <param name="package">The package information to validate.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport ValidateMacroPackage(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.ValidateMacroPackage(package);
    }

    /// <summary>
    /// Detects namespace conformance for this validator.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting namespace conformance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxNamespaceConformance DetectNamespaceConformance(IDocxPackageInspector package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.PackageValidator.DetectNamespaceConformance(package);
    }

    /// <summary>
    /// Applies all unambiguous package repairs and returns one ordered diagnostic report.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting repair report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport RepairRecoverableDefects(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.Repair.RepairRecoverableDefects(package);
    }

    /// <summary>
    /// Assigns unique identifiers to relationships whose identifiers are missing.
    /// </summary>
    /// <param name="package">The package to repair.</param>
    /// <returns>The resulting repair report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport RepairMissingRelationshipIds(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.Repair.RepairMissingRelationshipIds(package);
    }

    /// <summary>
    /// Repairs missing content types in this validator.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport RepairMissingContentTypes(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.Repair.RepairMissingContentTypes(package);
    }

    /// <summary>
    /// Repairs missing relationships in this validator.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport RepairMissingRelationships(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.Repair.RepairMissingRelationships(package);
    }

    /// <summary>
    /// Repairs orphaned relationships in this validator.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport RepairOrphanedRelationships(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.Repair.RepairOrphanedRelationships(package);
    }

    /// <summary>
    /// Repairs empty body in this validator.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <returns>The resulting validation report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxValidationReport RepairEmptyBody(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        return this.Repair.RepairEmptyBody(package);
    }
}
