using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class AuthorizationOperationsTests
{
    [Test]
    public void AppliesAllowAndDenyNetworksWithDenyPrecedence()
    {
        DnsAuthorizationPolicy policy = new DnsAuthorizationPolicy();
        AddNetwork(policy, false, 4, [192, 0, 2, 0], 24);
        AddNetwork(policy, true, 4, [192, 0, 2, 128], 25);
        DnsName zone = Parse("example");
        DnsClockValue now = Clock(1000);

        DnsAuthorizationDecision allowed =
            AuthorizationOperations.AuthorizeQuery(
                policy,
                Endpoint(4, [192, 0, 2, 10]),
                zone,
                null,
                now,
                new DnsAuthorizationRateState());
        DnsAuthorizationDecision denied =
            AuthorizationOperations.AuthorizeQuery(
                policy,
                Endpoint(4, [192, 0, 2, 200]),
                zone,
                null,
                now,
                new DnsAuthorizationRateState());
        DnsAuthorizationDecision outside =
            AuthorizationOperations.AuthorizeQuery(
                policy,
                Endpoint(4, [198, 51, 100, 10]),
                zone,
                null,
                now,
                new DnsAuthorizationRateState());

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Valid, Is.True);
            Assert.That(allowed.Authorized, Is.True);
            Assert.That(denied.Valid, Is.True);
            Assert.That(denied.Authorized, Is.False);
            Assert.That(outside.Authorized, Is.False);
        });
    }

    [Test]
    public void RejectsMalformedNetworkPolicyAndSupportsIpv6Prefixes()
    {
        DnsAuthorizationPolicy malformed = new DnsAuthorizationPolicy();
        malformed.AllowedClientAddresses.Add([192, 0, 2, 0]);
        DnsAuthorizationPolicy ipv6 = new DnsAuthorizationPolicy();
        AddNetwork(
            ipv6,
            false,
            6,
            [0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0,
             0, 0, 0, 0, 0, 0, 0, 0],
            32);

        DnsAuthorizationDecision invalid =
            AuthorizationOperations.AuthorizeQuery(
                malformed,
                Endpoint(4, [192, 0, 2, 1]),
                Parse("example"),
                null,
                Clock(1000),
                new DnsAuthorizationRateState());
        DnsAuthorizationDecision allowed =
            AuthorizationOperations.AuthorizeQuery(
                ipv6,
                Endpoint(
                    6,
                    [0x20, 0x01, 0x0d, 0xb8, 0, 1, 0, 0,
                     0, 0, 0, 0, 0, 0, 0, 1]),
                Parse("example"),
                null,
                Clock(1000),
                new DnsAuthorizationRateState());

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.Authorized, Is.False);
            Assert.That(allowed.Valid, Is.True);
            Assert.That(allowed.Authorized, Is.True);
        });
    }

    [Test]
    public void AppliesOperationZoneAndTsigRulesToMaintenanceRequests()
    {
        DnsAuthorizationPolicy policy = new DnsAuthorizationPolicy();
        policy.AllowTransfers = true;
        policy.AllowUpdates = true;
        policy.AllowNotify = true;
        policy.AllowedZones.Add(Parse("example"));
        policy.AllowedTsigKeyNames.Add(Parse("allowed-key"));
        DnsTsigKey allowedKey = Key("allowed-key");
        DnsTsigKey wrongKey = Key("wrong-key");
        DnsAuthorizationRateState rateState =
            new DnsAuthorizationRateState();

        DnsTransferRequest transfer = new DnsTransferRequest();
        transfer.Authorization = policy;
        transfer.Zone = Parse("example");
        transfer.Endpoint = Endpoint(4, [192, 0, 2, 1]);
        DnsAuthorizationDecision unsignedTransfer =
            AuthorizationOperations.AuthorizeTransfer(
                transfer, Clock(1000), rateState);
        transfer.TsigKey = wrongKey;
        DnsAuthorizationDecision wrongTransfer =
            AuthorizationOperations.AuthorizeTransfer(
                transfer, Clock(1000), rateState);
        transfer.TsigKey = allowedKey;
        DnsAuthorizationDecision allowedTransfer =
            AuthorizationOperations.AuthorizeTransfer(
                transfer, Clock(1000), rateState);

        DnsUpdateRequest update = new DnsUpdateRequest();
        update.Authorization = policy;
        update.Zone = Parse("example");
        update.ClientEndpoint = Endpoint(4, [192, 0, 2, 2]);
        update.TsigKey = allowedKey;
        DnsAuthorizationDecision allowedUpdate =
            AuthorizationOperations.AuthorizeUpdate(
                update, Clock(1000), rateState);

        DnsNotifyRequest notify = new DnsNotifyRequest();
        notify.Authorization = policy;
        notify.Zone = Parse("other");
        notify.Endpoint = Endpoint(4, [192, 0, 2, 3]);
        notify.TsigKey = allowedKey;
        DnsAuthorizationDecision wrongZoneNotify =
            AuthorizationOperations.AuthorizeNotify(
                notify, Clock(1000), rateState);
        policy.AllowNotify = false;
        notify.Zone = Parse("example");
        DnsAuthorizationDecision disabledNotify =
            AuthorizationOperations.AuthorizeNotify(
                notify, Clock(1000), rateState);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedTransfer.Authorized, Is.False);
            Assert.That(wrongTransfer.Authorized, Is.False);
            Assert.That(allowedTransfer.Authorized, Is.True);
            Assert.That(allowedUpdate.Authorized, Is.True);
            Assert.That(wrongZoneNotify.Authorized, Is.False);
            Assert.That(disabledNotify.Authorized, Is.False);
        });
    }

    [Test]
    public void LimitsEachClientByWindowAndSlipsConfiguredResponses()
    {
        DnsAuthorizationPolicy policy = new DnsAuthorizationPolicy();
        policy.RateLimitResponses = 1;
        policy.RateLimitWindowMilliseconds = 100;
        policy.RateLimitSlip = 2;
        DnsAuthorizationRateState rateState =
            new DnsAuthorizationRateState();
        DnsEndpoint firstClient = Endpoint(4, [192, 0, 2, 1]);
        DnsEndpoint secondClient = Endpoint(4, [192, 0, 2, 2]);
        DnsName zone = Parse("example");

        DnsAuthorizationDecision first = AuthorizeAt(
            policy, firstClient, zone, 1000, rateState);
        DnsAuthorizationDecision limited = AuthorizeAt(
            policy, firstClient, zone, 1001, rateState);
        DnsAuthorizationDecision slipped = AuthorizeAt(
            policy, firstClient, zone, 1002, rateState);
        DnsAuthorizationDecision otherClient = AuthorizeAt(
            policy, secondClient, zone, 1003, rateState);
        DnsAuthorizationDecision reset = AuthorizeAt(
            policy, firstClient, zone, 1100, rateState);

        Assert.Multiple(() =>
        {
            Assert.That(first.Authorized, Is.True);
            Assert.That(first.RateLimited, Is.False);
            Assert.That(limited.Authorized, Is.False);
            Assert.That(limited.RateLimited, Is.True);
            Assert.That(limited.Slipped, Is.False);
            Assert.That(slipped.Authorized, Is.True);
            Assert.That(slipped.RateLimited, Is.True);
            Assert.That(slipped.Slipped, Is.True);
            Assert.That(otherClient.Authorized, Is.True);
            Assert.That(reset.Authorized, Is.True);
            Assert.That(reset.RateLimited, Is.False);
        });
    }

    static DnsAuthorizationDecision AuthorizeAt(
        DnsAuthorizationPolicy policy,
        DnsEndpoint client,
        DnsName zone,
        long time,
        DnsAuthorizationRateState rateState)
    {
        return AuthorizationOperations.AuthorizeQuery(
            policy,
            client,
            zone,
            null,
            Clock(time),
            rateState);
    }

    static void AddNetwork(
        DnsAuthorizationPolicy policy,
        bool denied,
        int family,
        byte[] address,
        int prefixLength)
    {
        if (denied)
        {
            policy.DeniedClientAddresses.Add(address);
            policy.DeniedClientAddressFamilies.Add(family);
            policy.DeniedClientPrefixLengths.Add(prefixLength);
        }
        else
        {
            policy.AllowedClientAddresses.Add(address);
            policy.AllowedClientAddressFamilies.Add(family);
            policy.AllowedClientPrefixLengths.Add(prefixLength);
        }
    }

    static DnsTsigKey Key(string name)
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Parse(name);
        return result;
    }

    static DnsEndpoint Endpoint(int family, byte[] address)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.HasAddress = true;
        result.AddressFamily = family;
        result.Address = address;
        return result;
    }

    static DnsClockValue Clock(long monotonicMilliseconds)
    {
        DnsClockValue result = new DnsClockValue();
        result.MonotonicMilliseconds = monotonicMilliseconds;
        return result;
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        string[] labels = text.Split('.');
        foreach (string label in labels)
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }
}
