using System;

namespace odf;

internal static class OdfDiagnosticFactory
{
    internal static OdfDiagnostic Create(
        string code,
        string message,
        OdfValidationSeverity severity,
        OdfLocation? location,
        OdfLocation? relatedLocation,
        OdfVersion version)
    {
        OdfDiagnostic result = new OdfDiagnostic();
        result.Code = code;
        result.Message = message;
        result.Severity = severity;
        result.Location = CopyLocation(location);
        result.Version = version;
        result.SpecificationSection = SpecificationForCode(code);
        result.IsRecoverable = severity != OdfValidationSeverity.Fatal;
        if (relatedLocation != null)
        {
            result.RelatedLocation = CopyLocation(relatedLocation);
            result.HasRelatedLocation = true;
        }
        return result;
    }

    internal static OdfDiagnostic Normalize(
        OdfDiagnostic source,
        OdfVersion fallbackVersion)
    {
        if (source == null)
        {
            throw new OdfValidationException(
                "A diagnostic is required for normalization.");
        }
        OdfVersion version = source.Version;
        if (version == OdfVersion.Unknown)
        {
            version = fallbackVersion;
        }
        OdfLocation? related = null;
        if (source.HasRelatedLocation)
        {
            related = source.RelatedLocation;
        }
        OdfDiagnostic result = Create(
            source.Code,
            source.Message,
            source.Severity,
            source.Location,
            related,
            version);
        result.IsRecoverable = source.IsRecoverable;
        if (source.SpecificationSection.Length > 0)
        {
            result.SpecificationSection = source.SpecificationSection;
        }
        return result;
    }

    internal static OdfLocation CopyLocation(OdfLocation? source)
    {
        OdfLocation result = new OdfLocation();
        if (source == null)
        {
            return result;
        }
        result.PartPath = source.PartPath;
        result.NodePath = source.NodePath;
        result.XmlId = source.XmlId;
        result.Line = source.Line;
        result.Column = source.Column;
        result.ByteOffset = source.ByteOffset;
        result.TextOffset = source.TextOffset;
        result.TextLength = source.TextLength;
        return result;
    }

    internal static string SpecificationForCode(string code)
    {
        if (code == null)
        {
            return "SDK validation";
        }
        if (code.StartsWith("ODF-PKG-", StringComparison.Ordinal))
        {
            return "OpenDocument Part 2: Packages";
        }
        if (code.StartsWith("ODF-XML-", StringComparison.Ordinal))
        {
            return "XML 1.0 and Namespaces in XML";
        }
        if (code.StartsWith("ODF-SCHEMA-", StringComparison.Ordinal))
        {
            return "OpenDocument Part 3: Schema";
        }
        if (code.StartsWith("ODF-SEM-", StringComparison.Ordinal))
        {
            return "OpenDocument Part 3: Semantic constraints";
        }
        if (code.StartsWith("ODF-FORMULA-", StringComparison.Ordinal))
        {
            return "OpenDocument Part 4: Recalculated Formula";
        }
        if (code.StartsWith("ODF-SEC-", StringComparison.Ordinal))
        {
            return "OpenDocument Part 2: Encryption and signatures";
        }
        if (code.StartsWith("ODF-VERSION-", StringComparison.Ordinal))
        {
            return "OpenDocument versioning and conformance";
        }
        if (code.StartsWith("ODF-EDIT-", StringComparison.Ordinal))
        {
            return "OpenDocument editing constraints";
        }
        return "SDK validation and operation control";
    }
}
