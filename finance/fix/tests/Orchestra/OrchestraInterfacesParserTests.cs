using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraInterfacesParserTests
{
    [Test]
    public void ParsesVersion11InterfacesProtocolLayersSessionsAndOverrides()
    {
        string xml = """
            <fixi:interfaces xmlns:fixi="http://fixprotocol.io/2026/orchestra/interfaces"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:venue="urn:venue:interfaces">
              <fixi:metadata><dcterms:title>Venue connectivity</dcterms:title></fixi:metadata>
              <fixi:interface name="Trading" venue:environment="production">
                <fixi:service name="OrderEntry" version="1.0" reliability="recoverable" orchestration="https://example.test/repositories/orders.xml" activationTime="2026-01-01T00:00:00Z" venue:mode="orders">
                  <fixi:annotation><fixi:documentation purpose="SYNOPSIS">Order service</fixi:documentation></fixi:annotation>
                </fixi:service>
                <fixi:userInterface name="TraderDesktop" version="3"/>
                <fixi:encoding name="SBE" version="2.0" orchestration="https://example.test/repositories/orders.xml">
                  <fixi:annotation><fixi:appinfo><venue:schema href="orders-sbe.xml"/></fixi:appinfo></fixi:annotation>
                </fixi:encoding>
                <fixi:sessionProtocol name="FIXP" version="1.0" reliability="recoverable" orchestration="https://example.test/repositories/session.xml"/>
                <fixi:protocol name="SOFH" version="1.1" layer="session"/>
                <fixi:transport name="TCP" version="6" address="fix.example.test:9876" messageCast="unicast" use="primary" venue:alpn="fixp"/>
                <fixi:annotation><fixi:documentation>Primary trading interface</fixi:documentation></fixi:annotation>
                <fixi:sessions>
                  <fixi:session name="BUY-SELL" activationTime="2026-02-01T00:00:00Z" deactivationTime="2027-02-01T00:00:00Z" venue:region="us-east">
                    <fixi:service name="OrderEntry" version="1.1" orchestration="https://example.test/repositories/orders.xml"/>
                    <fixi:encoding name="SBE" version="2.1" orchestration="https://example.test/repositories/buy-sell.xml"/>
                    <fixi:sessionProtocol name="FIXP" version="1.1"/>
                    <fixi:protocol name="SOFH" version="1.1" layer="session"/>
                    <fixi:transport name="TCP" address="backup.example.test:9876" use="secondary"/>
                    <fixi:annotation><fixi:documentation>Buy-side session</fixi:documentation></fixi:annotation>
                    <fixi:identifier name="SenderCompID" venue:scope="local">BUY</fixi:identifier>
                    <fixi:identifier name="TargetCompID">SELL</fixi:identifier>
                    <fixi:identifier name="SenderCompID">BUY2</fixi:identifier>
                    <fixi:role>initiator</fixi:role>
                    <fixi:securityKeys>-----BEGIN PUBLIC KEY-----
                    ABC123
                    -----END PUBLIC KEY-----</fixi:securityKeys>
                  </fixi:session>
                </fixi:sessions>
              </fixi:interface>
            </fixi:interfaces>
            """;

        OrchestraRepository repository = Parse11(xml, "interfaces-v11.xml");
        OrchestraInterface orchestraInterface = repository.Interfaces.Single();
        OrchestraService service = orchestraInterface.Services.Single();
        OrchestraProtocol encoding = orchestraInterface.Encodings.Single();
        OrchestraProtocol transport = orchestraInterface.Transports.Single();
        OrchestraSession session = orchestraInterface.Sessions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(repository.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(repository.Metadata.Name, Is.EqualTo("Venue connectivity"));
            Assert.That(orchestraInterface.Name, Is.EqualTo("Trading"));
            Assert.That(orchestraInterface.Attributes["venue:environment"], Is.EqualTo("production"));
            Assert.That(orchestraInterface.Annotations.Single().Content, Is.EqualTo("Primary trading interface"));

            Assert.That(service.Name, Is.EqualTo("OrderEntry"));
            Assert.That(service.Version, Is.EqualTo("1.0"));
            Assert.That(service.Layer, Is.EqualTo("application"));
            Assert.That(service.Reliability, Is.EqualTo("recoverable"));
            Assert.That(service.OrchestrationUri, Is.EqualTo("https://example.test/repositories/orders.xml"));
            Assert.That(service.ActivationTime, Is.EqualTo("2026-01-01T00:00:00Z"));
            Assert.That(service.Attributes["venue:mode"], Is.EqualTo("orders"));
            Assert.That(service.Annotations.Single().Content, Is.EqualTo("Order service"));

            Assert.That(orchestraInterface.UserInterfaces.Single().Kind, Is.EqualTo("userInterface"));
            Assert.That(orchestraInterface.UserInterfaces.Single().Layer, Is.EqualTo("application"));
            Assert.That(encoding.Kind, Is.EqualTo("encoding"));
            Assert.That(encoding.Encoding, Is.EqualTo("SBE"));
            Assert.That(encoding.Layer, Is.EqualTo("presentation"));
            Assert.That(encoding.Annotations.Single().Kind, Is.EqualTo("appinfo"));
            Assert.That(encoding.Annotations.Single().Extensions.Single().Xml, Does.Contain("orders-sbe.xml"));
            Assert.That(orchestraInterface.SessionProtocols.Single().Layer, Is.EqualTo("session"));
            Assert.That(orchestraInterface.Protocols.Single().Name, Is.EqualTo("SOFH"));
            Assert.That(transport.Kind, Is.EqualTo("transport"));
            Assert.That(transport.Transport, Is.EqualTo("TCP"));
            Assert.That(transport.Layer, Is.EqualTo("transport"));
            Assert.That(transport.Address, Is.EqualTo("fix.example.test:9876"));
            Assert.That(transport.MessageCast, Is.EqualTo("unicast"));
            Assert.That(transport.Use, Is.EqualTo("primary"));
            Assert.That(transport.Settings["venue:alpn"], Is.EqualTo("fixp"));

            Assert.That(session.Name, Is.EqualTo("BUY-SELL"));
            Assert.That(session.Role, Is.EqualTo("initiator"));
            Assert.That(session.ActivationTime, Is.EqualTo("2026-02-01T00:00:00Z"));
            Assert.That(session.DeactivationTime, Is.EqualTo("2027-02-01T00:00:00Z"));
            Assert.That(session.SecurityKeys, Does.Contain("BEGIN PUBLIC KEY"));
            Assert.That(session.Attributes["venue:region"], Is.EqualTo("us-east"));
            Assert.That(session.Annotations.Single().Content, Is.EqualTo("Buy-side session"));
            Assert.That(session.Service, Is.EqualTo("OrderEntry"));
            Assert.That(session.Protocols, Is.EqualTo(new[] { "OrderEntry", "SBE", "FIXP", "SOFH", "TCP" }));
            Assert.That(session.ServiceOverrides.Single().Version, Is.EqualTo("1.1"));
            Assert.That(session.EncodingOverrides.Single().Version, Is.EqualTo("2.1"));
            Assert.That(session.SessionProtocolOverrides.Single().Name, Is.EqualTo("FIXP"));
            Assert.That(session.ProtocolOverrides.Single().Name, Is.EqualTo("SOFH"));
            Assert.That(session.TransportOverrides.Single().Address, Is.EqualTo("backup.example.test:9876"));
            Assert.That(session.TransportOverrides.Single().Use, Is.EqualTo("secondary"));

            Assert.That(session.IdentifierEntries.Select(item => item.Name), Is.EqualTo(new[] { "SenderCompID", "TargetCompID", "SenderCompID" }));
            Assert.That(session.IdentifierEntries[0].Value, Is.EqualTo("BUY"));
            Assert.That(session.IdentifierEntries[0].Attributes["venue:scope"], Is.EqualTo("local"));
            Assert.That(session.Identifiers["SenderCompID"], Is.EqualTo("BUY2"));
            Assert.That(session.Identifiers["TargetCompID"], Is.EqualTo("SELL"));
            Assert.That(repository.RepositoryLinks, Is.EqualTo(new[]
            {
                "https://example.test/repositories/orders.xml",
                "https://example.test/repositories/session.xml",
                "https://example.test/repositories/buy-sell.xml"
            }));
        });
    }

    [Test]
    public void ParsesVersion10SessionElementTimesAndTransportConfiguration()
    {
        string xml = """
            <fixi:interfaces xmlns:fixi="http://fixprotocol.io/2020/orchestra/interfaces"
                             xmlns:dcterms="http://purl.org/dc/terms/">
              <fixi:metadata><dcterms:title>Legacy connectivity</dcterms:title></fixi:metadata>
              <fixi:interface name="Legacy">
                <fixi:service name="OrderEntry" version="FIX.4.4" orchestration="legacy-repository.xml"/>
                <fixi:encoding name="tagvalue"/>
                <fixi:sessionProtocol name="FIX4" version="4.4"/>
                <fixi:transport name="TCP" address="127.0.0.1:5001" messageCast="multicast" use="alternate"/>
                <fixi:sessions>
                  <fixi:session name="LEGACY">
                    <fixi:transport name="TCP" address="127.0.0.2:5001"/>
                    <fixi:identifier name="SenderCompID">OLD_BUY</fixi:identifier>
                    <fixi:activationTime>2025-01-01T00:00:00Z</fixi:activationTime>
                    <fixi:deactivationTime>2025-12-31T23:59:59Z</fixi:deactivationTime>
                    <fixi:role>acceptor</fixi:role>
                    <fixi:securityKeys>legacy-key</fixi:securityKeys>
                  </fixi:session>
                </fixi:sessions>
              </fixi:interface>
            </fixi:interfaces>
            """;

        OrchestraRepository repository = Parse10(xml, "interfaces-v10.xml");
        OrchestraInterface orchestraInterface = repository.Interfaces.Single();
        OrchestraSession session = orchestraInterface.Sessions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(repository.Version, Is.EqualTo(OrchestraVersion.Version10));
            Assert.That(repository.Metadata.Name, Is.EqualTo("Legacy connectivity"));
            Assert.That(orchestraInterface.Services.Single().OrchestrationUri, Is.EqualTo("legacy-repository.xml"));
            Assert.That(orchestraInterface.Encodings.Single().Name, Is.EqualTo("tagvalue"));
            Assert.That(orchestraInterface.SessionProtocols.Single().Name, Is.EqualTo("FIX4"));
            Assert.That(orchestraInterface.Transports.Single().Address, Is.EqualTo("127.0.0.1:5001"));
            Assert.That(orchestraInterface.Transports.Single().MessageCast, Is.EqualTo("multicast"));
            Assert.That(orchestraInterface.Transports.Single().Use, Is.EqualTo("alternate"));
            Assert.That(session.ActivationTime, Is.EqualTo("2025-01-01T00:00:00Z"));
            Assert.That(session.DeactivationTime, Is.EqualTo("2025-12-31T23:59:59Z"));
            Assert.That(session.Role, Is.EqualTo("acceptor"));
            Assert.That(session.SecurityKeys, Is.EqualTo("legacy-key"));
            Assert.That(session.Identifiers["SenderCompID"], Is.EqualTo("OLD_BUY"));
            Assert.That(session.TransportOverrides.Single().Address, Is.EqualTo("127.0.0.2:5001"));
            Assert.That(repository.RepositoryLinks.Single(), Is.EqualTo("legacy-repository.xml"));
        });
    }

    private static OrchestraRepository Parse11(string xml, string sourceName)
    {
        OrchestraXmlLoadResult loaded = Load(xml, sourceName);
        Assert.That(loaded.Version, Is.EqualTo(OrchestraVersion.Version11));
        return OrchestraVersion11Parser.ParseDocument(loaded);
    }

    private static OrchestraRepository Parse10(string xml, string sourceName)
    {
        OrchestraXmlLoadResult loaded = Load(xml, sourceName);
        Assert.That(loaded.Version, Is.EqualTo(OrchestraVersion.Version10));
        return OrchestraVersion10Parser.ParseDocument(loaded);
    }

    private static OrchestraXmlLoadResult Load(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = sourceName });
        Assert.That(loaded.Success, Is.True, Codes(loaded));
        Assert.That(loaded.InterfacesDocument, Is.True);
        return loaded;
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
