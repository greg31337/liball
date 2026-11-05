using System;
using System.Collections.Generic;

namespace odf;

internal class OdfValidationPipeline
{
    OdfValidationOptions Options;
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    bool Stop;
    bool PackageValidated;
    bool VersionValidated;
    bool CompatibilityValidated;
    bool XmlValidated;
    bool SemanticsValidated;
    int RecoveredDiagnosticCount;
    OdfVersion Version;
    VersionAnalysisResult? VersionAnalysis;

    internal OdfValidationPipeline(OdfValidationOptions options)
    {
        if (!OptionsAreValid(options))
        {
            throw new OdfValidationException(
                "Valid bounded validation options and mode are required.");
        }
        Options = options;
    }

    internal ValidationPipelineResult ValidateDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfValidationException(
                "A document is required for validation.");
        }
        ResetPipeline(document);
        ValidateCancellation();
        if (!Stop && ShouldValidatePackage())
        {
            ValidatePackageLayer(document);
        }
        if (!Stop && ShouldValidateVersion())
        {
            ValidateVersionLayer(document);
        }
        if (!Stop && Options.Mode == OdfValidationMode.Profile)
        {
            ValidateCompatibilityLayer(document);
        }
        if (!Stop && ShouldValidateXml())
        {
            ValidateXmlLayer(document);
        }
        if (!Stop && ShouldValidateSemantics())
        {
            ValidateSemanticLayer(document);
        }
        ValidationPipelineResult result = new ValidationPipelineResult();
        result.Diagnostics = Diagnostics.ToArray();
        result.PackageValidated = PackageValidated;
        result.VersionValidated = VersionValidated;
        result.CompatibilityValidated = CompatibilityValidated;
        result.XmlValidated = XmlValidated;
        result.SemanticsValidated = SemanticsValidated;
        result.RecoveredDiagnosticCount = RecoveredDiagnosticCount;
        result.StoppedEarly = Stop;
        result.Profile = SelectedProfile();
        result.VersionAnalysis = VersionAnalysis;
        result.IsValid = !HasErrorDiagnostics();
        return result;
    }

    void ResetPipeline(OdfDocument document)
    {
        Diagnostics = new List<OdfDiagnostic>();
        Stop = false;
        PackageValidated = false;
        VersionValidated = false;
        CompatibilityValidated = false;
        XmlValidated = false;
        SemanticsValidated = false;
        RecoveredDiagnosticCount = 0;
        VersionAnalysis = null;
        Version = SelectVersion(document);
    }

    void ValidateVersionLayer(OdfDocument document)
    {
        VersionValidated = true;
        VersionAnalysis = new OdfVersionServiceCore(Options)
            .ValidateVersionGates(document);
        AppendDiagnostics(VersionAnalysis.Diagnostics, true);
    }

    void ValidateCompatibilityLayer(OdfDocument document)
    {
        CompatibilityValidated = true;
        OdfDiagnostic[] diagnostics =
            new OdfCompatibilityProfileValidator(
                Options.CompatibilityProfile,
                Options.ConformanceClass).ValidateDocument(document);
        AppendDiagnostics(diagnostics, true);
    }

    void ValidatePackageLayer(OdfDocument document)
    {
        OdfPackage? package = SelectPackage(document);
        if (package == null)
        {
            return;
        }
        PackageValidated = true;
        PackageValidationResult result =
            new PackageConformanceValidator(Options.SecurityLimits)
                .ValidatePackage(package);
        AppendDiagnostics(result.Diagnostics, Options.ValidateSecurity);
    }

    void ValidateXmlLayer(OdfDocument document)
    {
        XmlValidated = true;
        OdfValidationOptions layerOptions = CopyOptions(
            ChildCollectionMode());
        layerOptions.ValidateXml = true;
        XmlValidationResult result =
            new XmlDocumentValidator(layerOptions).ValidateDocument(document);
        AppendDiagnostics(result.Diagnostics, true);
        if (result.StoppedEarly && IsStrictMode())
        {
            Stop = true;
        }
    }

    void ValidateSemanticLayer(OdfDocument document)
    {
        SemanticsValidated = true;
        OdfValidationOptions layerOptions = CopyOptions(
            ChildCollectionMode());
        layerOptions.ValidateSemantics = true;
        SemanticValidationResult result =
            new SemanticDocumentValidator(layerOptions)
                .ValidateDocument(document);
        AppendDiagnostics(result.Diagnostics, true);
        if (result.StoppedEarly && IsStrictMode())
        {
            Stop = true;
        }
    }

    void AppendDiagnostics(
        OdfDiagnostic[] source,
        bool includeSecurity)
    {
        int index = 0;
        while (index < source.Length && !Stop)
        {
            OdfDiagnostic diagnostic = OdfDiagnosticFactory.Normalize(
                source[index],
                Version);
            if (includeSecurity || !IsSecurityDiagnostic(diagnostic.Code))
            {
                diagnostic = ApplyRecovery(diagnostic);
                if (ShouldInclude(diagnostic)
                    && !ContainsDiagnostic(diagnostic))
                {
                    Diagnostics.Add(diagnostic);
                }
                if (IsStrictMode()
                    && (diagnostic.Severity == OdfValidationSeverity.Error
                        || diagnostic.Severity
                            == OdfValidationSeverity.Fatal))
                {
                    Stop = true;
                }
            }
            index = index + 1;
        }
    }

    OdfDiagnostic ApplyRecovery(OdfDiagnostic source)
    {
        if (Options.Mode != OdfValidationMode.Recovery
            || source.Severity != OdfValidationSeverity.Error
            || !source.IsRecoverable)
        {
            return source;
        }
        OdfLocation? related = null;
        if (source.HasRelatedLocation)
        {
            related = source.RelatedLocation;
        }
        OdfDiagnostic result = OdfDiagnosticFactory.Create(
            source.Code,
            source.Message,
            OdfValidationSeverity.Warning,
            source.Location,
            related,
            source.Version);
        result.SpecificationSection = source.SpecificationSection;
        RecoveredDiagnosticCount = RecoveredDiagnosticCount + 1;
        return result;
    }

    void ValidateCancellation()
    {
        if (!Options.Cancellation.IsCancellationRequested)
        {
            return;
        }
        string message = "Validation was cancelled.";
        if (Options.Cancellation.Reason.Length > 0)
        {
            message = message + " " + Options.Cancellation.Reason;
        }
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.OperationCancelled,
            message,
            OdfValidationSeverity.Fatal,
            null,
            null,
            Version);
        Diagnostics.Add(diagnostic);
        Stop = true;
    }

    OdfPackage? SelectPackage(OdfDocument document)
    {
        if (document.Package != null)
        {
            return document.Package;
        }
        if (document.PackageKind != OdfPackageKind.Package)
        {
            return null;
        }
        OdfPackage result = new OdfPackage();
        result.Entries = document.PackageEntries;
        result.Manifest = document.Manifest;
        result.Diagnostics = document.Diagnostics;
        result.MediaType = document.MediaType;
        result.IsValid = true;
        return result;
    }

    OdfValidationOptions CopyOptions(OdfValidationMode mode)
    {
        OdfValidationOptions result = new OdfValidationOptions();
        result.Mode = mode;
        result.SecurityLimits = Options.SecurityLimits;
        result.Cancellation = Options.Cancellation;
        result.Version = Options.Version;
        result.ConformanceClass = Options.ConformanceClass;
        result.CompatibilityProfile = Options.CompatibilityProfile;
        result.ValidatePackage = Options.ValidatePackage;
        result.ValidateXml = Options.ValidateXml;
        result.ValidateSemantics = Options.ValidateSemantics;
        result.ValidateFormulas = Options.ValidateFormulas;
        result.ValidateSecurity = Options.ValidateSecurity;
        result.IncludeWarnings = Options.IncludeWarnings;
        result.IncludeInformation = Options.IncludeInformation;
        return result;
    }

    bool ShouldValidatePackage()
    {
        if (Options.Mode == OdfValidationMode.PackageOnly)
        {
            return true;
        }
        if (Options.Mode == OdfValidationMode.XmlOnly
            || Options.Mode == OdfValidationMode.SemanticOnly)
        {
            return false;
        }
        return Options.ValidatePackage;
    }

    bool ShouldValidateVersion()
    {
        return ShouldValidateXml() || ShouldValidateSemantics();
    }

    bool ShouldValidateXml()
    {
        if (Options.Mode == OdfValidationMode.XmlOnly)
        {
            return true;
        }
        if (Options.Mode == OdfValidationMode.PackageOnly
            || Options.Mode == OdfValidationMode.SemanticOnly)
        {
            return false;
        }
        return Options.ValidateXml;
    }

    bool ShouldValidateSemantics()
    {
        if (Options.Mode == OdfValidationMode.SemanticOnly)
        {
            return true;
        }
        if (Options.Mode == OdfValidationMode.PackageOnly
            || Options.Mode == OdfValidationMode.XmlOnly)
        {
            return false;
        }
        return Options.ValidateSemantics;
    }

    bool ShouldInclude(OdfDiagnostic diagnostic)
    {
        if (diagnostic.Severity == OdfValidationSeverity.Warning)
        {
            return Options.IncludeWarnings;
        }
        if (diagnostic.Severity == OdfValidationSeverity.Information)
        {
            return Options.IncludeInformation;
        }
        return true;
    }

    bool ContainsDiagnostic(OdfDiagnostic diagnostic)
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            OdfDiagnostic current = Diagnostics[index];
            if (current.Code == diagnostic.Code
                && current.Message == diagnostic.Message
                && current.Location.PartPath
                    == diagnostic.Location.PartPath
                && current.Location.NodePath
                    == diagnostic.Location.NodePath
                && current.Location.Line == diagnostic.Location.Line
                && current.Location.Column == diagnostic.Location.Column)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    bool HasErrorDiagnostics()
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

    bool IsStrictMode()
    {
        return Options.Mode == OdfValidationMode.Strict;
    }

    OdfValidationMode ChildCollectionMode()
    {
        if (IsStrictMode())
        {
            return OdfValidationMode.Strict;
        }
        return OdfValidationMode.Complete;
    }

    string SelectedProfile()
    {
        if (Options.Mode != OdfValidationMode.Profile)
        {
            return "";
        }
        if (Options.CompatibilityProfile.Length > 0)
        {
            return Options.CompatibilityProfile;
        }
        return OdfVersionRules.StandardProfile;
    }

    OdfVersion SelectVersion(OdfDocument document)
    {
        if (Options.Version != OdfVersion.Unknown)
        {
            return Options.Version;
        }
        if (document.VersionInfo != null)
        {
            return document.VersionInfo.Version;
        }
        return OdfVersion.Unknown;
    }

    static bool IsSecurityDiagnostic(string code)
    {
        return code.StartsWith("ODF-SEC-", StringComparison.Ordinal)
            || code == OdfDiagnosticCodes.InvalidEncryptionStructure;
    }

    static bool OptionsAreValid(OdfValidationOptions? options)
    {
        return options != null
            && options.SecurityLimits != null
            && options.Cancellation != null
            && ModeIsValid(options.Mode);
    }

    static bool ModeIsValid(OdfValidationMode mode)
    {
        return mode == OdfValidationMode.Strict
            || mode == OdfValidationMode.Complete
            || mode == OdfValidationMode.Recovery
            || mode == OdfValidationMode.PackageOnly
            || mode == OdfValidationMode.XmlOnly
            || mode == OdfValidationMode.SemanticOnly
            || mode == OdfValidationMode.Profile;
    }
}
