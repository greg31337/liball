using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class PublicBaseAvpFactoryTests
{
    [Test]
    public void FactoryCreatesCanonicalSimpleBaseAvpsThatRoundTripAndValidate()
    {
        IDiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        DiameterIdentity node = CreateIdentity("NODE.Example.COM.");
        DiameterIdentity realm = CreateIdentity("EXAMPLE.COM.");
        DiameterUri redirect = new DiameterUri();
        redirect.Scheme = "AAAS";
        redirect.Host.Value = "REDIRECT.Example.COM.";
        redirect.Port = DiameterProtocolConstants.SecurePort;
        redirect.Transport = "SCTP";
        redirect.Protocol = "DIAMETER";

        List<DiameterAvp> avps = new List<DiameterAvp>
        {
            factory.CreateOriginHostAvp(node),
            factory.CreateOriginRealmAvp(realm),
            factory.CreateOriginStateIdAvp(123U),
            factory.CreateDestinationHostAvp(node),
            factory.CreateDestinationRealmAvp(realm),
            factory.CreateAuthApplicationIdAvp(4U),
            factory.CreateAcctApplicationIdAvp(3U),
            factory.CreateSessionIdAvp("node.example.com;3913056000;1"),
            factory.CreateRouteRecordAvp(node),
            factory.CreateRedirectHostAvp(redirect),
            factory.CreateRedirectHostUsageAvp(DiameterRedirectHostUsage.AllSession),
            factory.CreateRedirectMaxCacheTimeAvp(60U)
        };

        int index = 0;
        while (index < avps.Count)
        {
            AssertValidAndRoundTrips(avps[index]);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(avps[0].Data), Is.EqualTo("node.example.com"));
            Assert.That(Encoding.UTF8.GetString(avps[1].Data), Is.EqualTo("example.com"));
            Assert.That(ReadUnsigned32(avps[2]), Is.EqualTo(123U));
            Assert.That(ReadUnsigned32(avps[5]), Is.EqualTo(4U));
            Assert.That(ReadUnsigned32(avps[6]), Is.EqualTo(3U));
            Assert.That(Encoding.UTF8.GetString(avps[9].Data),
                Is.EqualTo("aaas://redirect.example.com;transport=sctp"));
            Assert.That(ReadUnsigned32(avps[10]), Is.EqualTo(1U));
            Assert.That(ReadUnsigned32(avps[11]), Is.EqualTo(60U));
        });
    }

    [Test]
    public void VendorSpecificApplicationHelpersCreateExactlyOneApplicationChild()
    {
        IDiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        DiameterApplicationId authentication = new DiameterApplicationId();
        authentication.VendorId = 10415U;
        authentication.Id = 16777251U;
        DiameterApplicationId accounting = new DiameterApplicationId();
        accounting.VendorId = 10415U;
        accounting.Id = 16777252U;
        accounting.IsAccounting = true;

        DiameterAvp authAvp = factory.CreateVendorSpecificApplicationIdAvp(authentication);
        DiameterAvp acctAvp = factory.CreateVendorSpecificApplicationIdAvp(accounting);
        List<DiameterAvp> authChildren = DecodeGrouped(authAvp);
        List<DiameterAvp> acctChildren = DecodeGrouped(acctAvp);

        AssertValidAndRoundTrips(authAvp);
        AssertValidAndRoundTrips(acctAvp);
        Assert.Multiple(() =>
        {
            Assert.That(authChildren, Has.Count.EqualTo(2));
            Assert.That(authChildren[0].Code, Is.EqualTo(DiameterAvpCodes.VendorId));
            Assert.That(ReadUnsigned32(authChildren[0]), Is.EqualTo(10415U));
            Assert.That(authChildren[1].Code, Is.EqualTo(DiameterAvpCodes.AuthApplicationId));
            Assert.That(ReadUnsigned32(authChildren[1]), Is.EqualTo(16777251U));
            Assert.That(acctChildren, Has.Count.EqualTo(2));
            Assert.That(acctChildren[1].Code, Is.EqualTo(DiameterAvpCodes.AcctApplicationId));
            Assert.That(ReadUnsigned32(acctChildren[1]), Is.EqualTo(16777252U));
        });
    }

    [Test]
    public void ProxyAndExperimentalHelpersCreateOwnedDictionaryValidGroupedValues()
    {
        IDiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        DiameterProxyInformation proxy = new DiameterProxyInformation();
        proxy.ProxyHost.Value = "PROXY.Example.";
        proxy.ProxyState = new byte[] { 1, 2, 3, 4 };
        DiameterAvp extension = new DiameterAvp();
        extension.Code = 999U;
        extension.Data = new byte[] { 5, 6 };
        proxy.ExtensionAvps.Add(extension);
        DiameterExperimentalResult experimental = new DiameterExperimentalResult();
        experimental.VendorId = 10415U;
        experimental.ResultCode = 5000123U;

        DiameterAvp proxyAvp = factory.CreateProxyInfoAvp(proxy);
        DiameterAvp experimentalAvp = factory.CreateExperimentalResultAvp(experimental);
        proxy.ProxyState[0] = 9;
        extension.Data[0] = 9;
        List<DiameterAvp> proxyChildren = DecodeGrouped(proxyAvp);
        List<DiameterAvp> experimentalChildren = DecodeGrouped(experimentalAvp);

        AssertValidAndRoundTrips(proxyAvp);
        AssertValidAndRoundTrips(experimentalAvp);
        Assert.Multiple(() =>
        {
            Assert.That(proxyChildren, Has.Count.EqualTo(3));
            Assert.That(proxyChildren[0].Code, Is.EqualTo(DiameterAvpCodes.ProxyHost));
            Assert.That(Encoding.UTF8.GetString(proxyChildren[0].Data),
                Is.EqualTo("proxy.example"));
            Assert.That(proxyChildren[1].Code, Is.EqualTo(DiameterAvpCodes.ProxyState));
            Assert.That(proxyChildren[1].Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(proxyChildren[2].Data, Is.EqualTo(new byte[] { 5, 6 }));
            Assert.That(experimentalChildren, Has.Count.EqualTo(2));
            Assert.That(ReadUnsigned32(experimentalChildren[0]), Is.EqualTo(10415U));
            Assert.That(ReadUnsigned32(experimentalChildren[1]), Is.EqualTo(5000123U));
        });
    }

    [Test]
    public void FactoryRejectsMalformedBaseAndGroupedValues()
    {
        IDiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        DiameterIdentity invalidIdentity = CreateIdentity("bad_name.example");
        DiameterApplicationId missingVendor = new DiameterApplicationId();
        missingVendor.Id = 4U;
        DiameterProxyInformation duplicateProxyHost = new DiameterProxyInformation();
        duplicateProxyHost.ProxyHost.Value = "proxy.example";
        DiameterAvp duplicate = new DiameterAvp();
        duplicate.Code = DiameterAvpCodes.ProxyHost;
        duplicate.Flags = DiameterAvpFlags.Mandatory;
        duplicate.Data = Encoding.UTF8.GetBytes("other.example");
        duplicateProxyHost.ExtensionAvps.Add(duplicate);
        DiameterUri invalidUri = new DiameterUri();
        invalidUri.Host.Value = "bad_name.example";
        DiameterExperimentalResult missingExperimentalVendor = new DiameterExperimentalResult();
        missingExperimentalVendor.ResultCode = 5001U;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => factory.CreateOriginHostAvp(null));
            Assert.Throws<ArgumentException>(() => factory.CreateDestinationRealmAvp(invalidIdentity));
            Assert.Throws<ArgumentException>(() => factory.CreateSessionIdAvp(""));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateVendorSpecificApplicationIdAvp(missingVendor));
            Assert.Throws<ArgumentException>(() => factory.CreateProxyInfoAvp(null));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateProxyInfoAvp(duplicateProxyHost));
            Assert.Throws<ArgumentException>(() => factory.CreateRedirectHostAvp(invalidUri));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateRedirectHostUsageAvp((DiameterRedirectHostUsage)99));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateExperimentalResultAvp(missingExperimentalVendor));
        });
    }

    [Test]
    public void FactoryRejectsEveryGroupedNullBoundaryAndEnumEndpoint()
    {
        IDiameterBaseAvpFactory factory = new DiameterBaseAvpFactory();
        DiameterProxyInformation nullExtension =
            CreateProxyInformation();
        nullExtension.ExtensionAvps.Add(null!);
        DiameterProxyInformation duplicateState =
            CreateProxyInformation();
        DiameterAvp state = new DiameterAvp();
        state.Code = DiameterAvpCodes.ProxyState;
        duplicateState.ExtensionAvps.Add(state);
        DiameterProxyInformation missingPayload =
            CreateProxyInformation();
        DiameterAvp extension = new DiameterAvp();
        extension.Code = 999U;
        extension.Data = null!;
        missingPayload.ExtensionAvps.Add(extension);
        DiameterProxyInformation permittedIetfExtension =
            CreateProxyInformation();
        DiameterAvp permitted = new DiameterAvp();
        permitted.Code = 999U;
        permitted.Data = new byte[] { 1 };
        permittedIetfExtension.ExtensionAvps.Add(permitted);
        DiameterProxyInformation permittedVendorExtension =
            CreateProxyInformation();
        DiameterAvp vendorExtension = new DiameterAvp();
        vendorExtension.Code = DiameterAvpCodes.ProxyHost;
        vendorExtension.VendorId = 99U;
        vendorExtension.Flags = DiameterAvpFlags.VendorSpecific;
        vendorExtension.Data = new byte[] { 1 };
        permittedVendorExtension.ExtensionAvps.Add(vendorExtension);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                factory.CreateVendorSpecificApplicationIdAvp(null));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateExperimentalResultAvp(null));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateRedirectHostUsageAvp(
                    (DiameterRedirectHostUsage)(-1)));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateProxyInfoAvp(nullExtension));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateProxyInfoAvp(duplicateState));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateProxyInfoAvp(missingPayload));
            Assert.DoesNotThrow(() =>
                factory.CreateProxyInfoAvp(permittedIetfExtension));
            Assert.DoesNotThrow(() =>
                factory.CreateProxyInfoAvp(permittedVendorExtension));
            Assert.Throws<ArgumentException>(() =>
                ((DiameterBaseAvpFactory)factory).CreateGroupedAvp(
                    999U,
                    null!));
            DiameterEncodeResult invalid =
                new DiameterAvpCodec().EncodeAvpToArray(null);
            Assert.That(
                invalid.Status,
                Is.Not.EqualTo(DiameterCodecStatus.Success));
        });
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static DiameterProxyInformation CreateProxyInformation()
    {
        DiameterProxyInformation result =
            new DiameterProxyInformation();
        result.ProxyHost.Value = "proxy.example";
        result.ProxyState = new byte[] { 1 };
        return result;
    }

    private static void AssertValidAndRoundTrips(DiameterAvp avp)
    {
        IDiameterDictionary dictionary = new DiameterDictionary();
        IDiameterValidator validator = new DiameterValidator();
        DiameterValidationResult validation = validator.ValidateGroupedAvp(
            avp,
            dictionary.ReadDictionarySnapshot(),
            new DiameterLimitConfiguration());
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterEncodeResult encoding = codec.EncodeAvpToArray(avp);
        DiameterAvpDecodeResult decoding = codec.DecodeAvp(encoding.Data, false);

        Assert.That(validation.Status, Is.EqualTo(DiameterValidationStatus.Valid),
            string.Join(" | ", validation.Issues.ConvertAll(issue => issue.Code)));
        Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(decoding.Avp, Is.Not.Null);
        Assert.That(decoding.Avp!.Code, Is.EqualTo(avp.Code));
        Assert.That(decoding.Avp.Data, Is.EqualTo(avp.Data));
    }

    private static List<DiameterAvp> DecodeGrouped(DiameterAvp avp)
    {
        DiameterValueResult decoded = new DiameterAvpCodec().DecodeGrouped(
            avp.Data,
            null,
            new DiameterLimitConfiguration());
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Avps;
    }

    private static uint ReadUnsigned32(DiameterAvp avp)
    {
        DiameterValueResult decoded = new DiameterAvpCodec().DecodeUnsigned32(avp.Data);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Unsigned32;
    }
}
