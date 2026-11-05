using System.Collections.Generic;
using System.Xml;

namespace fix;

internal static class OrchestraInterfacesEmitter
{
    internal static void WriteInterfacesDocument(XmlWriter writer, OrchestraRepository repository, OrchestraWriteOptions options)
    {
        string namespaceUri = OrchestraXmlEmitter.InterfacesNamespace(options.Version);
        writer.WriteStartElement("fixi", "interfaces", namespaceUri);
        writer.WriteAttributeString("xmlns", "dcterms", null, OrchestraXmlEmitter.DublinCoreTermsNamespace);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, repository.Extensions);
        OrchestraXmlEmitter.WriteMetadata(writer, repository.Metadata, namespaceUri);
        List<OrchestraInterface> items = new List<OrchestraInterface>(repository.Interfaces);
        if (options.CanonicalOrder) items.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        for (int index = 0; index < items.Count; index++) WriteInterface(writer, items[index], options, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, repository.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteInterface(XmlWriter writer, OrchestraInterface item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixi", "interface", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", item.Name);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, item.Extensions);
        WriteServices(writer, item.Services, options, namespaceUri);
        WriteProtocols(writer, item.UserInterfaces, "userInterface", options, namespaceUri);
        WriteProtocols(writer, item.Encodings, "encoding", options, namespaceUri);
        WriteProtocols(writer, item.SessionProtocols, "sessionProtocol", options, namespaceUri);
        WriteProtocols(writer, item.Protocols, "protocol", options, namespaceUri);
        WriteProtocols(writer, item.Transports, "transport", options, namespaceUri);
        OrchestraXmlEmitter.WriteAnnotations(writer, item.Annotations, namespaceUri);
        WriteSessions(writer, item.Sessions, options, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteServices(XmlWriter writer, List<OrchestraService> source, OrchestraWriteOptions options, string namespaceUri)
    {
        List<OrchestraService> items = new List<OrchestraService>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        for (int index = 0; index < items.Count; index++) WriteService(writer, items[index], namespaceUri);
    }

    internal static void WriteService(XmlWriter writer, OrchestraService item, string namespaceUri)
    {
        writer.WriteStartElement("fixi", "service", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", item.Name);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "address", item.Address);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "version", item.Version);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "layer", item.Layer);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "reliability", item.Reliability);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "orchestration", item.OrchestrationUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "activationTime", item.ActivationTime);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "deactivationTime", item.DeactivationTime);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "deprecated", item.DeprecatedTime);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, item.Extensions);
        OrchestraXmlEmitter.WriteAnnotations(writer, item.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteProtocols(XmlWriter writer, List<OrchestraProtocol> source, string kind, OrchestraWriteOptions options, string namespaceUri)
    {
        List<OrchestraProtocol> items = new List<OrchestraProtocol>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        for (int index = 0; index < items.Count; index++) WriteProtocol(writer, items[index], kind, namespaceUri);
    }

    internal static void WriteProtocol(XmlWriter writer, OrchestraProtocol item, string fallbackKind, string namespaceUri)
    {
        string kind = item.Kind.Length == 0 ? fallbackKind : item.Kind;
        writer.WriteStartElement("fixi", kind, namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", item.Name);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "version", item.Version);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "layer", item.Layer);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "reliability", item.Reliability);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "orchestration", item.OrchestrationUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "activationTime", item.ActivationTime);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "deactivationTime", item.DeactivationTime);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "deprecated", item.DeprecatedTime);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "address", item.Address);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "messageCast", item.MessageCast);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "use", item.Use);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, item.Extensions);
        OrchestraXmlEmitter.WriteAnnotations(writer, item.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteSessions(XmlWriter writer, List<OrchestraSession> source, OrchestraWriteOptions options, string namespaceUri)
    {
        if (source.Count == 0) return;
        List<OrchestraSession> items = new List<OrchestraSession>(source);
        if (options.CanonicalOrder) items.Sort((left, right) => OrchestraXmlEmitter.CompareNames(left.Name, right.Name));
        writer.WriteStartElement("fixi", "sessions", namespaceUri);
        for (int index = 0; index < items.Count; index++) WriteSession(writer, items[index], options, namespaceUri);
        writer.WriteEndElement();
    }

    internal static void WriteSession(XmlWriter writer, OrchestraSession item, OrchestraWriteOptions options, string namespaceUri)
    {
        writer.WriteStartElement("fixi", "session", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", item.Name);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "activationTime", item.ActivationTime);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "deactivationTime", item.DeactivationTime);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, item.Extensions);
        WriteServices(writer, item.ServiceOverrides, options, namespaceUri);
        WriteProtocols(writer, item.UserInterfaceOverrides, "userInterface", options, namespaceUri);
        WriteProtocols(writer, item.EncodingOverrides, "encoding", options, namespaceUri);
        WriteProtocols(writer, item.SessionProtocolOverrides, "sessionProtocol", options, namespaceUri);
        WriteProtocols(writer, item.ProtocolOverrides, "protocol", options, namespaceUri);
        WriteProtocols(writer, item.TransportOverrides, "transport", options, namespaceUri);
        for (int index = 0; index < item.IdentifierEntries.Count; index++) WriteIdentifier(writer, item.IdentifierEntries[index], namespaceUri);
        if (item.Role.Length != 0) writer.WriteElementString("fixi", "role", namespaceUri, item.Role);
        if (item.SecurityKeys.Length != 0) writer.WriteElementString("fixi", "securityKeys", namespaceUri, item.SecurityKeys);
        OrchestraXmlEmitter.WriteAnnotations(writer, item.Annotations, namespaceUri);
        OrchestraXmlEmitter.WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }

    internal static void WriteIdentifier(XmlWriter writer, OrchestraIdentifier item, string namespaceUri)
    {
        writer.WriteStartElement("fixi", "identifier", namespaceUri);
        OrchestraXmlEmitter.WriteOptionalAttribute(writer, "name", item.Name);
        OrchestraXmlEmitter.WriteExtensionAttributes(writer, item.Extensions);
        writer.WriteString(item.Value);
        OrchestraXmlEmitter.WriteExtensions(writer, item.Extensions);
        writer.WriteEndElement();
    }
}
