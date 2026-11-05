using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes part relationships data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxPartRelationshipsWriter : IXlsxPartRelationshipsWriter
{
    /// <summary>
    /// Initializes a new part relationships writer with its default state.
    /// </summary>
    public XlsxPartRelationshipsWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRelationshipsWriteResult WriteString(XlsxRelationshipsPart part)
    {
        XlsxRelationshipsWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRelationshipsWriteResult WriteBytes(XlsxRelationshipsPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxRelationshipsWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxRelationshipsWriteResult WriteStream(Stream stream, XlsxRelationshipsPart part)
    {
        XlsxRelationshipsWriteResult result = ValidateRelationshipsPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        List<XlsxRelationship> relationships = CopyRelationships(part);
        relationships.Sort(CompareRelationshipsById);
        using (XmlWriter writer = XmlWriter.Create(stream, XlsxContentTypesWriter.CreateXmlWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "Relationships", XlsxNamespaces.PackageRelationships);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            foreach (XlsxRelationship relationship in relationships)
            {
                WriteRelationship(writer, relationship);
            }

            foreach (string unknownXml in part.UnknownXml)
            {
                if (unknownXml != null && unknownXml.Length > 0)
                {
                    writer.WriteRaw(unknownXml);
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static XlsxRelationshipsWriteResult ValidateRelationshipsPart(XlsxRelationshipsPart part)
    {
        XlsxRelationshipsWriteResult result = new XlsxRelationshipsWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.partNull", "Relationships part must not be null."));
            return result;
        }

        if (part.SourcePartPath == null || part.SourcePartPath.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.sourcePartMissing", "Relationships part must have a source part path."));
        }

        Dictionary<string, bool> relationshipIds = new Dictionary<string, bool>();
        foreach (XlsxRelationship relationship in part.Relationships)
        {
            ValidateRelationship(result, part, relationship, relationshipIds);
        }

        return result;
    }

    internal static void ValidateRelationship(XlsxRelationshipsWriteResult result, XlsxRelationshipsPart part, XlsxRelationship relationship, Dictionary<string, bool> relationshipIds)
    {
        if (relationship == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.relationshipNull", "Relationship entry must not be null."));
            return;
        }

        bool valid = true;
        if (relationship.Id == null || relationship.Id.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.idMissing", "Relationship must have an Id attribute.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "Id");
            result.AddDiagnostic(diagnostic);
            valid = false;
        }

        if (relationship.Type == null || relationship.Type.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.typeMissing", "Relationship must have a Type attribute.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "Type");
            result.AddDiagnostic(diagnostic);
            valid = false;
        }

        if (relationship.Target == null || relationship.Target.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.targetMissing", "Relationship must have a Target attribute.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "Target");
            result.AddDiagnostic(diagnostic);
            valid = false;
        }

        if (relationship.TargetMode != null && relationship.TargetMode.Length > 0 && relationship.TargetMode != "Internal" && relationship.TargetMode != "External")
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.targetModeUnsupported", "Relationship TargetMode must be Internal or External.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "TargetMode");
            diagnostic.SetRelationshipId(relationship.Id);
            result.AddDiagnostic(diagnostic);
            valid = false;
        }

        if (relationship.Id != null && relationship.Id.Length > 0)
        {
            if (relationshipIds.ContainsKey(relationship.Id))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.duplicateId", "Relationships XML contains a duplicate relationship Id.");
                diagnostic.SetLocation(part.PartPath, "Relationship", "Id");
                diagnostic.SetRelationshipId(relationship.Id);
                result.AddDiagnostic(diagnostic);
                valid = false;
            }
            else
            {
                relationshipIds.Add(relationship.Id, true);
            }
        }

        if (valid)
        {
            bool external = relationship.TargetMode == "External" || relationship.IsExternal;
            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(part.SourcePartPath, relationship.Target, external);
            foreach (XlsxDiagnostic diagnostic in target.Validation.Diagnostics)
            {
                diagnostic.SetLocation(part.PartPath, "Relationship", "Target");
                diagnostic.SetRelationshipId(relationship.Id);
                result.AddDiagnostic(diagnostic);
            }
        }
    }

    internal static List<XlsxRelationship> CopyRelationships(XlsxRelationshipsPart part)
    {
        List<XlsxRelationship> relationships = new List<XlsxRelationship>();
        foreach (XlsxRelationship relationship in part.Relationships)
        {
            relationships.Add(relationship);
        }

        return relationships;
    }

    internal static int CompareRelationshipsById(XlsxRelationship left, XlsxRelationship right)
    {
        return string.Compare(left.Id, right.Id, System.StringComparison.Ordinal);
    }

    internal static void WriteRelationship(XmlWriter writer, XlsxRelationship relationship)
    {
        writer.WriteStartElement("", "Relationship", XlsxNamespaces.PackageRelationships);
        writer.WriteAttributeString("Id", relationship.Id);
        writer.WriteAttributeString("Type", relationship.Type);
        writer.WriteAttributeString("Target", relationship.Target);
        if (relationship.TargetMode == "External" || relationship.IsExternal)
        {
            writer.WriteAttributeString("TargetMode", "External");
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, relationship.UnknownAttributes);
        writer.WriteEndElement();
    }
}
