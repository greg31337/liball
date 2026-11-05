using System.Net;

namespace diameter.Tests;

public class ProtocolDataTests
{
    [Test]
    public void ProtocolDataDefaultsAreUsable()
    {
        DiameterMessage message = new DiameterMessage();
        DiameterAvp avp = new DiameterAvp();
        DiameterApplicationId application = new DiameterApplicationId();
        DiameterIdentity identity = new DiameterIdentity();
        DiameterUri uri = new DiameterUri();
        DiameterEndpoint endpoint = new DiameterEndpoint();

        Assert.Multiple(() =>
        {
            Assert.That(message.Version, Is.EqualTo(DiameterProtocolConstants.Version));
            Assert.That(message.Flags, Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(message.CommandCode, Is.Zero);
            Assert.That(message.ApplicationId, Is.Zero);
            Assert.That(message.HopByHopIdentifier, Is.Zero);
            Assert.That(message.EndToEndIdentifier, Is.Zero);
            Assert.That(message.Avps, Is.Empty);

            Assert.That(avp.Code, Is.Zero);
            Assert.That(avp.Flags, Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(avp.VendorId, Is.Zero);
            Assert.That(avp.Data, Is.Empty);

            Assert.That(application.Id, Is.Zero);
            Assert.That(application.VendorId, Is.Zero);
            Assert.That(application.IsAccounting, Is.False);
            Assert.That(identity.Value, Is.Empty);

            Assert.That(uri.Scheme, Is.EqualTo("aaa"));
            Assert.That(uri.Host.Value, Is.Empty);
            Assert.That(uri.Port, Is.EqualTo(DiameterProtocolConstants.Port));
            Assert.That(uri.Transport, Is.EqualTo("tcp"));
            Assert.That(uri.Protocol, Is.EqualTo("diameter"));

            Assert.That(endpoint.Identity.Value, Is.Empty);
            Assert.That(endpoint.Host, Is.Empty);
            Assert.That(endpoint.Address, Is.Null);
            Assert.That(endpoint.Port, Is.EqualTo(DiameterProtocolConstants.Port));
            Assert.That(endpoint.Transport, Is.EqualTo(DiameterTransportKind.Tcp));
            Assert.That(endpoint.Security, Is.EqualTo(DiameterSecurityMode.None));
        });
    }

    [Test]
    public void MessagePreservesOrderedAndRepeatedAvps()
    {
        DiameterAvp first = new DiameterAvp();
        first.Code = DiameterAvpCodes.RouteRecord;
        first.Flags = DiameterAvpFlags.Mandatory;
        first.Data = new byte[] { 1, 2 };

        DiameterAvp second = new DiameterAvp();
        second.Code = DiameterAvpCodes.RouteRecord;
        second.Flags = DiameterAvpFlags.Mandatory;
        second.Data = new byte[] { 3, 4 };

        DiameterMessage message = new DiameterMessage();
        message.Version = 2;
        message.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.HopByHopIdentifier = 123;
        message.EndToEndIdentifier = 456;
        message.Avps.Add(first);
        message.Avps.Add(second);

        Assert.Multiple(() =>
        {
            Assert.That(message.Version, Is.EqualTo(2));
            Assert.That(message.Avps, Has.Count.EqualTo(2));
            Assert.That(message.Avps[0], Is.SameAs(first));
            Assert.That(message.Avps[1], Is.SameAs(second));
            Assert.That(message.Avps[0].Code, Is.EqualTo(message.Avps[1].Code));
            Assert.That(message.Flags, Is.EqualTo(DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable));
            Assert.That(message.CommandCode, Is.EqualTo(DiameterCommandCodes.DeviceWatchdog));
            Assert.That(message.ApplicationId, Is.EqualTo(DiameterApplicationIds.Common));
            Assert.That(message.HopByHopIdentifier, Is.EqualTo(123U));
            Assert.That(message.EndToEndIdentifier, Is.EqualTo(456U));
        });
    }

    [Test]
    public void MutableStorageIsOwnedByEachDataObject()
    {
        DiameterMessage firstMessage = new DiameterMessage();
        DiameterMessage secondMessage = new DiameterMessage();
        DiameterAvp firstAvp = new DiameterAvp();
        DiameterAvp secondAvp = new DiameterAvp();
        DiameterUri firstUri = new DiameterUri();
        DiameterUri secondUri = new DiameterUri();
        DiameterEndpoint firstEndpoint = new DiameterEndpoint();
        DiameterEndpoint secondEndpoint = new DiameterEndpoint();

        firstMessage.Avps.Add(firstAvp);
        firstAvp.Data = new byte[] { 9 };
        firstUri.Host.Value = "uri.example";
        firstEndpoint.Identity.Value = "peer.example";

        Assert.Multiple(() =>
        {
            Assert.That(secondMessage.Avps, Is.Empty);
            Assert.That(secondAvp.Data, Is.Empty);
            Assert.That(firstAvp.Data, Is.Not.SameAs(secondAvp.Data));
            Assert.That(secondUri.Host.Value, Is.Empty);
            Assert.That(firstUri.Host, Is.Not.SameAs(secondUri.Host));
            Assert.That(secondEndpoint.Identity.Value, Is.Empty);
            Assert.That(firstEndpoint.Identity, Is.Not.SameAs(secondEndpoint.Identity));
        });
    }

    [Test]
    public void ComponentFieldsAndOperationalEventReferencesRoundTrip()
    {
        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = DiameterApplicationIds.CreditControl;
        application.VendorId = DiameterVendorIds.ThreeGpp;
        application.IsAccounting = true;

        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";

        DiameterUri uri = new DiameterUri();
        uri.Scheme = "aaas";
        uri.Host = identity;
        uri.Port = DiameterProtocolConstants.SecurePort;
        uri.Transport = "sctp";
        uri.Protocol = "diameter";

        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity = identity;
        endpoint.Host = "peer.example";
        endpoint.Address = IPAddress.Parse("2001:db8::1");
        endpoint.Port = DiameterProtocolConstants.SecurePort;
        endpoint.Transport = DiameterTransportKind.Sctp;
        endpoint.Security = DiameterSecurityMode.Dtls;

        DiameterMessage message = new DiameterMessage();
        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.Message = message;
        eventData.Application = application;
        eventData.Uri = uri;
        eventData.Endpoint = endpoint;

        Assert.Multiple(() =>
        {
            Assert.That(application.Id, Is.EqualTo(DiameterApplicationIds.CreditControl));
            Assert.That(application.VendorId, Is.EqualTo(DiameterVendorIds.ThreeGpp));
            Assert.That(application.IsAccounting, Is.True);
            Assert.That(identity.Value, Is.EqualTo("peer.example"));
            Assert.That(uri.Scheme, Is.EqualTo("aaas"));
            Assert.That(uri.Host, Is.SameAs(identity));
            Assert.That(uri.Port, Is.EqualTo(DiameterProtocolConstants.SecurePort));
            Assert.That(uri.Transport, Is.EqualTo("sctp"));
            Assert.That(uri.Protocol, Is.EqualTo("diameter"));
            Assert.That(endpoint.Identity, Is.SameAs(identity));
            Assert.That(endpoint.Host, Is.EqualTo("peer.example"));
            Assert.That(endpoint.Address, Is.EqualTo(IPAddress.Parse("2001:db8::1")));
            Assert.That(endpoint.Port, Is.EqualTo(DiameterProtocolConstants.SecurePort));
            Assert.That(endpoint.Transport, Is.EqualTo(DiameterTransportKind.Sctp));
            Assert.That(endpoint.Security, Is.EqualTo(DiameterSecurityMode.Dtls));
            Assert.That(eventData.Message, Is.SameAs(message));
            Assert.That(eventData.Application, Is.SameAs(application));
            Assert.That(eventData.Uri, Is.SameAs(uri));
            Assert.That(eventData.Endpoint, Is.SameAs(endpoint));
        });
    }
}
