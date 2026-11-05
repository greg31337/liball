using System.Xml;

namespace fix;

internal static class OrchestraInterfacesParser
{
    internal static OrchestraRepository ParseDocument(OrchestraXmlLoadResult source)
    {
        OrchestraRepository repository = OrchestraCommonParser.ParseDocument(source);
        XmlElement? root = source.Root;
        if (root == null || !source.InterfacesDocument) return repository;
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlElement? element = root.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == root.NamespaceURI && element.LocalName == "interface")
            {
                repository.Interfaces.Add(ParseInterfaceElement(element, repository, root.NamespaceURI));
            }
        }
        return repository;
    }

    internal static OrchestraInterface ParseInterfaceElement(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraInterface orchestraInterface = new OrchestraInterface();
        orchestraInterface.Name = element.GetAttribute("name").Trim();
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child == null || child.NamespaceURI != namespaceUri) continue;
            if (child.LocalName == "service") orchestraInterface.Services.Add(ParseServiceElement(child, repository, namespaceUri));
            else if (child.LocalName == "userInterface") orchestraInterface.UserInterfaces.Add(ParseProtocolElement(child, repository, namespaceUri));
            else if (child.LocalName == "encoding") orchestraInterface.Encodings.Add(ParseProtocolElement(child, repository, namespaceUri));
            else if (child.LocalName == "sessionProtocol") orchestraInterface.SessionProtocols.Add(ParseProtocolElement(child, repository, namespaceUri));
            else if (child.LocalName == "protocol") orchestraInterface.Protocols.Add(ParseProtocolElement(child, repository, namespaceUri));
            else if (child.LocalName == "transport") orchestraInterface.Transports.Add(ParseProtocolElement(child, repository, namespaceUri));
            else if (child.LocalName == "sessions") ParseSessionsContainer(child, orchestraInterface, repository, namespaceUri);
        }
        OrchestraCommonParser.ParseAnnotations(element, orchestraInterface.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, orchestraInterface.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, orchestraInterface.Extensions);
        return orchestraInterface;
    }

    internal static OrchestraService ParseServiceElement(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraService service = new OrchestraService();
        service.Name = element.GetAttribute("name").Trim();
        service.Address = element.GetAttribute("address").Trim();
        service.Version = element.GetAttribute("version").Trim();
        service.Layer = element.GetAttribute("layer").Trim();
        if (service.Layer.Length == 0) service.Layer = "application";
        service.Reliability = element.GetAttribute("reliability").Trim();
        service.OrchestrationUri = element.GetAttribute("orchestration").Trim();
        service.ActivationTime = element.GetAttribute("activationTime").Trim();
        service.DeactivationTime = element.GetAttribute("deactivationTime").Trim();
        service.DeprecatedTime = element.GetAttribute("deprecated").Trim();
        OrchestraCommonParser.ParseAnnotations(element, service.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, service.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, service.Extensions);
        RegisterRepositoryLink(repository, service.OrchestrationUri);
        return service;
    }

    internal static OrchestraProtocol ParseProtocolElement(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraProtocol protocol = new OrchestraProtocol();
        protocol.Kind = element.LocalName;
        protocol.Name = element.GetAttribute("name").Trim();
        protocol.Version = element.GetAttribute("version").Trim();
        protocol.Layer = element.GetAttribute("layer").Trim();
        if (protocol.Layer.Length == 0) protocol.Layer = ReadDefaultLayer(element.LocalName);
        if (element.LocalName == "encoding") protocol.Encoding = protocol.Name;
        if (element.LocalName == "transport") protocol.Transport = protocol.Name;
        protocol.Reliability = element.GetAttribute("reliability").Trim();
        protocol.OrchestrationUri = element.GetAttribute("orchestration").Trim();
        protocol.ActivationTime = element.GetAttribute("activationTime").Trim();
        protocol.DeactivationTime = element.GetAttribute("deactivationTime").Trim();
        protocol.DeprecatedTime = element.GetAttribute("deprecated").Trim();
        protocol.Address = element.GetAttribute("address").Trim();
        protocol.MessageCast = element.GetAttribute("messageCast").Trim();
        protocol.Use = element.GetAttribute("use").Trim();
        OrchestraCommonParser.ParseAnnotations(element, protocol.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, protocol.Settings);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, protocol.Extensions);
        RegisterRepositoryLink(repository, protocol.OrchestrationUri);
        return protocol;
    }

    internal static string ReadDefaultLayer(string kind)
    {
        if (kind == "service") return "application";
        if (kind == "userInterface") return "application";
        if (kind == "encoding") return "presentation";
        if (kind == "sessionProtocol") return "session";
        if (kind == "transport") return "transport";
        return "";
    }

    internal static void ParseSessionsContainer(XmlElement container, OrchestraInterface orchestraInterface, OrchestraRepository repository, string namespaceUri)
    {
        for (int index = 0; index < container.ChildNodes.Count; index++)
        {
            XmlElement? element = container.ChildNodes[index] as XmlElement;
            if (element != null && element.NamespaceURI == namespaceUri && element.LocalName == "session")
            {
                orchestraInterface.Sessions.Add(ParseSessionElement(element, repository, namespaceUri));
            }
        }
    }

    internal static OrchestraSession ParseSessionElement(XmlElement element, OrchestraRepository repository, string namespaceUri)
    {
        OrchestraSession session = new OrchestraSession();
        session.Name = element.GetAttribute("name").Trim();
        session.ActivationTime = element.GetAttribute("activationTime").Trim();
        session.DeactivationTime = element.GetAttribute("deactivationTime").Trim();
        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlElement? child = element.ChildNodes[index] as XmlElement;
            if (child == null || child.NamespaceURI != namespaceUri) continue;
            if (child.LocalName == "service")
            {
                OrchestraService service = ParseServiceElement(child, repository, namespaceUri);
                session.ServiceOverrides.Add(service);
                if (session.Service.Length == 0) session.Service = service.Name;
                AddProtocolName(session, service.Name);
            }
            else if (child.LocalName == "userInterface")
            {
                OrchestraProtocol protocol = ParseProtocolElement(child, repository, namespaceUri);
                session.UserInterfaceOverrides.Add(protocol);
                AddProtocolName(session, protocol.Name);
            }
            else if (child.LocalName == "encoding")
            {
                OrchestraProtocol protocol = ParseProtocolElement(child, repository, namespaceUri);
                session.EncodingOverrides.Add(protocol);
                AddProtocolName(session, protocol.Name);
            }
            else if (child.LocalName == "sessionProtocol")
            {
                OrchestraProtocol protocol = ParseProtocolElement(child, repository, namespaceUri);
                session.SessionProtocolOverrides.Add(protocol);
                AddProtocolName(session, protocol.Name);
            }
            else if (child.LocalName == "protocol")
            {
                OrchestraProtocol protocol = ParseProtocolElement(child, repository, namespaceUri);
                session.ProtocolOverrides.Add(protocol);
                AddProtocolName(session, protocol.Name);
            }
            else if (child.LocalName == "transport")
            {
                OrchestraProtocol protocol = ParseProtocolElement(child, repository, namespaceUri);
                session.TransportOverrides.Add(protocol);
                AddProtocolName(session, protocol.Name);
            }
            else if (child.LocalName == "identifier") ParseIdentifierElement(child, session, namespaceUri);
            else if (child.LocalName == "role") session.Role = child.InnerText.Trim();
            else if (child.LocalName == "securityKeys") session.SecurityKeys = child.InnerText;
            else if (child.LocalName == "activationTime" && session.ActivationTime.Length == 0) session.ActivationTime = child.InnerText.Trim();
            else if (child.LocalName == "deactivationTime" && session.DeactivationTime.Length == 0) session.DeactivationTime = child.InnerText.Trim();
        }
        OrchestraCommonParser.ParseAnnotations(element, session.Annotations, namespaceUri);
        OrchestraVersion10Parser.CopyAttributes(element, session.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, session.Extensions);
        return session;
    }

    internal static void ParseIdentifierElement(XmlElement element, OrchestraSession session, string namespaceUri)
    {
        OrchestraIdentifier identifier = new OrchestraIdentifier();
        identifier.Name = element.GetAttribute("name").Trim();
        identifier.Value = element.InnerText;
        OrchestraVersion10Parser.CopyAttributes(element, identifier.Attributes);
        OrchestraCommonParser.AddForeignExtensions(element, namespaceUri, identifier.Extensions);
        identifier.Xml = element.OuterXml;
        session.IdentifierEntries.Add(identifier);
        if (identifier.Name.Length != 0) session.Identifiers[identifier.Name] = identifier.Value;
    }

    internal static void AddProtocolName(OrchestraSession session, string name)
    {
        if (name.Length != 0) session.Protocols.Add(name);
    }

    internal static void RegisterRepositoryLink(OrchestraRepository repository, string uri)
    {
        if (uri.Length == 0) return;
        for (int index = 0; index < repository.RepositoryLinks.Count; index++)
        {
            if (repository.RepositoryLinks[index] == uri) return;
        }
        repository.RepositoryLinks.Add(uri);
    }
}
