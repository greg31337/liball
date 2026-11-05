using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads package relationships data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxPackageRelationshipsReader : IXlsxPackageRelationshipsReader
{
    /// <summary>
    /// Initializes a new package relationships reader with its default state.
    /// </summary>
    public XlsxPackageRelationshipsReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadString(string xml)
    {
        return ReadStringWithOptions(xml, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadStringWithOptions(string xml, XlsxReaderOptions options)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(bytes, options);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadBytes(byte[] bytes, XlsxReaderOptions options)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream, options);
        }
    }

    /// <summary>
    /// Parses package relationships from read-only memory.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadMemory(ReadOnlyMemory<byte> bytes, XlsxReaderOptions options)
    {
        return ReadBytes(bytes.ToArray(), options);
    }

    /// <summary>
    /// Parses package relationships from a read-only byte span.
    /// </summary>
    /// <param name="bytes">The OOXML bytes to read.</param>
    /// <param name="options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadSpan(ReadOnlySpan<byte> bytes, XlsxReaderOptions options)
    {
        return ReadBytes(bytes.ToArray(), options);
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The relationships part produced by the operation.</returns>
    public XlsxRelationshipsPart ReadStream(Stream stream, XlsxReaderOptions options)
    {
        return ReadStreamForPart("/", XlsxPackagePaths.PackageRelationships, stream, options);
    }

    internal static XlsxRelationshipsPart ReadStreamForPart(string sourcePartPath, string relationshipPartPath, Stream stream, XlsxReaderOptions options)
    {
        XlsxReaderOptions readerOptions = options;
        if (readerOptions == null)
        {
            readerOptions = new XlsxReaderOptions();
        }

        XlsxRelationshipsPart part = new XlsxRelationshipsPart();
        part.SourcePartPath = sourcePartPath;
        part.PartPath = relationshipPartPath;
        Dictionary<string, bool> relationshipIds = new Dictionary<string, bool>();
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, XlsxContentTypesReader.CreateXmlReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    ReadRootElement(part, reader);
                    reader.Read();
                    continue;
                }

                if (reader.Depth == 1 && reader.NamespaceURI == XlsxNamespaces.PackageRelationships && reader.LocalName == "Relationship")
                {
                    part.RelationshipElementCount = part.RelationshipElementCount + 1;
                    if (part.RelationshipElementCount > readerOptions.MaxRelationshipsPerPart)
                    {
                        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.maxRelationships", "Relationship count exceeds the configured limit.");
                        diagnostic.SetLocation(part.PartPath, "Relationship", "");
                        part.AddDiagnostic(diagnostic);
                    }

                    ReadRelationship(part, reader, relationshipIds);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.missingRoot", "Relationships XML must contain a Relationships root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxRelationshipsPart CreateMissingRootResult()
    {
        return CreateMissingRootResult("/", XlsxPackagePaths.PackageRelationships);
    }

    internal static XlsxRelationshipsPart CreateMissingRootResult(string sourcePartPath, string relationshipPartPath)
    {
        XlsxRelationshipsPart part = new XlsxRelationshipsPart();
        part.SourcePartPath = sourcePartPath;
        part.PartPath = relationshipPartPath;
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.missingRoot", "Relationships XML must contain a Relationships root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxRelationshipsPart part, XmlReader reader)
    {
        if (reader.LocalName != "Relationships" || reader.NamespaceURI != XlsxNamespaces.PackageRelationships)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.invalidRoot", "Relationships XML root must be a Relationships element in the package relationships namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        ReadUnknownRelationshipAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadRelationship(XlsxRelationshipsPart part, XmlReader reader, Dictionary<string, bool> relationshipIds)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = reader.GetAttribute("Id");
        relationship.Type = reader.GetAttribute("Type");
        relationship.Target = reader.GetAttribute("Target");
        relationship.TargetMode = reader.GetAttribute("TargetMode");
        ReadUnknownRelationshipAttributes(reader, relationship.UnknownAttributes);
        bool valid = true;
        if (relationship.Id == null || relationship.Id.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.idMissing", "Relationship must have an Id attribute.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "Id");
            part.AddDiagnostic(diagnostic);
            valid = false;
        }

        if (relationship.Type == null || relationship.Type.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.typeMissing", "Relationship must have a Type attribute.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "Type");
            part.AddDiagnostic(diagnostic);
            valid = false;
        }

        if (relationship.Target == null || relationship.Target.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.targetMissing", "Relationship must have a Target attribute.");
            diagnostic.SetLocation(part.PartPath, "Relationship", "Target");
            part.AddDiagnostic(diagnostic);
            valid = false;
        }

        bool targetModeValid = SetRelationshipTargetMode(part, relationship);
        if (!targetModeValid)
        {
            valid = false;
        }

        if (relationship.Id != null && relationship.Id.Length > 0)
        {
            if (relationshipIds.ContainsKey(relationship.Id))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.duplicateId", "Relationships XML contains a duplicate relationship Id.");
                diagnostic.SetLocation(part.PartPath, "Relationship", "Id");
                diagnostic.SetRelationshipId(relationship.Id);
                part.AddDiagnostic(diagnostic);
                valid = false;
            }
            else
            {
                relationshipIds.Add(relationship.Id, true);
            }
        }

        if (valid)
        {
            bool targetValid = ResolveRelationshipTarget(part, relationship);
            if (targetValid)
            {
                part.Relationships.Add(relationship);
            }
        }

        reader.ReadOuterXml();
    }

    internal static bool SetRelationshipTargetMode(XlsxRelationshipsPart part, XlsxRelationship relationship)
    {
        if (relationship.TargetMode == null || relationship.TargetMode.Length == 0 || relationship.TargetMode == "Internal")
        {
            relationship.TargetMode = "Internal";
            relationship.IsExternal = false;
            return true;
        }

        if (relationship.TargetMode == "External")
        {
            relationship.IsExternal = true;
            return true;
        }

        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "relationships.targetModeUnsupported", "Relationship TargetMode must be Internal or External.");
        diagnostic.SetLocation(part.PartPath, "Relationship", "TargetMode");
        diagnostic.SetRelationshipId(relationship.Id);
        part.AddDiagnostic(diagnostic);
        return false;
    }

    internal static bool ResolveRelationshipTarget(XlsxRelationshipsPart part, XlsxRelationship relationship)
    {
        XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(part.SourcePartPath, relationship.Target, relationship.IsExternal);
        foreach (XlsxDiagnostic diagnostic in target.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, "Relationship", "Target");
            diagnostic.SetRelationshipId(relationship.Id);
            part.AddDiagnostic(diagnostic);
        }

        relationship.TargetPath = target.Path;
        relationship.TargetFragment = target.Fragment;
        relationship.ExternalTarget = target.ExternalTarget;
        return target.Validation.IsValid;
    }

    internal static void ReadUnknownRelationshipAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool knownId = reader.NamespaceURI == "" && reader.LocalName == "Id";
            bool knownType = reader.NamespaceURI == "" && reader.LocalName == "Type";
            bool knownTarget = reader.NamespaceURI == "" && reader.LocalName == "Target";
            bool knownTargetMode = reader.NamespaceURI == "" && reader.LocalName == "TargetMode";
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!knownId && !knownType && !knownTarget && !knownTargetMode && !namespaceDeclaration)
            {
                XlsxXmlAttribute attribute = new XlsxXmlAttribute();
                attribute.NamespaceUri = reader.NamespaceURI;
                attribute.LocalName = reader.LocalName;
                attribute.Prefix = reader.Prefix;
                attribute.Value = reader.Value;
                attributes.Add(attribute);
            }
        }

        reader.MoveToElement();
    }
}
