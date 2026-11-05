using System;
using System.IO;
using System.Xml;

namespace iso20022;

internal static class EcoreMetamodelDiscovery
{
    internal static EcoreMetamodelDiscoveryResult DiscoverMetamodel(Stream source, string sourceName, int maximumCharacters)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (sourceName == null)
        {
            throw new ArgumentNullException("sourceName");
        }

        if (maximumCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumCharacters");
        }

        long originalPosition = 0;
        bool restorePosition = source.CanSeek;
        if (restorePosition)
        {
            originalPosition = source.Position;
            if (source.Length - originalPosition > maximumCharacters)
            {
                throw new XmlException("The repository input exceeds the discovery character limit.");
            }
        }

        XmlReaderSettings settings = CreateDiscoverySettings(maximumCharacters);
        EcoreMetamodelDiscoveryResult result;
        using (XmlReader reader = XmlReader.Create(source, settings, sourceName))
        {
            result = ReadRoot(reader, sourceName);
        }

        if (restorePosition)
        {
            source.Position = originalPosition;
        }

        return result;
    }

    internal static XmlReaderSettings CreateDiscoverySettings(int maximumCharacters)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.CheckCharacters = true;
        settings.CloseInput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = maximumCharacters;
        return settings;
    }

    internal static EcoreMetamodelDiscoveryResult ReadRoot(XmlReader reader, string sourceName)
    {
        bool hasNode = reader.Read();
        while (hasNode && reader.NodeType != XmlNodeType.Element)
        {
            hasNode = reader.Read();
        }

        if (!hasNode)
        {
            return CreateFailure(
                "repository.root.missing",
                "The repository artifact does not contain a root element.");
        }

        QualifiedNameData rootName = new QualifiedNameData();
        rootName.NamespaceUri = reader.NamespaceURI;
        rootName.LocalName = reader.LocalName;
        rootName.Prefix = reader.Prefix;
        if (rootName.LocalName != "Repository")
        {
            EcoreMetamodelDiscoveryResult wrongRoot = CreateFailure(
                "repository.root.unsupported",
                "The artifact root is not an ISO 20022 Repository element.");
            wrongRoot.RootName = rootName;
            wrongRoot.Location = CaptureLocation(reader, sourceName);
            return wrongRoot;
        }

        if (rootName.NamespaceUri != Iso20022Namespaces.Ecore2013)
        {
            EcoreMetamodelDiscoveryResult unsupported = CreateFailure(
                "repository.metamodel.unsupported",
                "No repository adapter supports the discovered metamodel namespace.");
            unsupported.RootName = rootName;
            unsupported.MetamodelNamespace = rootName.NamespaceUri;
            unsupported.Location = CaptureLocation(reader, sourceName);
            return unsupported;
        }

        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        EcoreMetamodelDiscoveryResult result = new EcoreMetamodelDiscoveryResult();
        result.Success = true;
        result.RootName = rootName;
        result.RootIdentifier = GetAttributeValue(reader, Iso20022Namespaces.Xmi, "id");
        result.XmiVersion = GetAttributeValue(reader, Iso20022Namespaces.Xmi, "version");
        result.MetamodelNamespace = rootName.NamespaceUri;
        result.MetamodelVersion = adapter.GetMetamodelVersion();
        result.AdapterIdentifier = adapter.GetAdapterIdentifier();
        result.Adapter = adapter;
        result.Location = CaptureLocation(reader, sourceName);
        return result;
    }

    internal static string GetAttributeValue(XmlReader reader, string namespaceUri, string localName)
    {
        string? value = reader.GetAttribute(localName, namespaceUri);
        if (value == null)
        {
            return string.Empty;
        }

        return value;
    }

    internal static SourceLocation CaptureLocation(XmlReader reader, string sourceName)
    {
        SourceLocation location = new SourceLocation();
        location.SourceName = sourceName;
        IXmlLineInfo lineInformation = (IXmlLineInfo)reader;
        if (lineInformation.HasLineInfo())
        {
            location.HasLineInformation = true;
            location.LineNumber = lineInformation.LineNumber;
            location.ColumnNumber = lineInformation.LinePosition;
        }

        return location;
    }

    internal static EcoreMetamodelDiscoveryResult CreateFailure(string errorCode, string errorMessage)
    {
        EcoreMetamodelDiscoveryResult result = new EcoreMetamodelDiscoveryResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
