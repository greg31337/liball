using System;
using System.Collections.Generic;
using System.Text;

namespace diameter.Tests;

public class PublicErrorAnswerRuleTests
{
    [Test]
    public void ErrorAnswerFlagsAndIdentifiersFollowRequestAndResultClassRules()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterIdentity host = CreateIdentity("node.example");
        DiameterIdentity realm = CreateIdentity("example");
        DiameterMessage request = CreateRequest();
        request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.PotentiallyRetransmitted;

        DiameterMessage protocol = BuildStandardAnswer(
            validator,
            request,
            DiameterResultCodes.UnableToDeliver,
            host,
            realm);
        DiameterMessage transient = BuildStandardAnswer(
            validator,
            request,
            DiameterResultCodes.AuthenticationRejected,
            host,
            realm);
        DiameterMessage permanent = BuildStandardAnswer(
            validator,
            request,
            DiameterResultCodes.UnableToComply,
            host,
            realm);

        Assert.Multiple(() =>
        {
            Assert.That(protocol.Flags, Is.EqualTo(
                DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error));
            Assert.That(transient.Flags, Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That(permanent.Flags, Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That((protocol.Flags & DiameterMessageFlags.Request),
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That((protocol.Flags & DiameterMessageFlags.PotentiallyRetransmitted),
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(protocol.Version, Is.EqualTo(request.Version));
            Assert.That(protocol.CommandCode, Is.EqualTo(request.CommandCode));
            Assert.That(protocol.ApplicationId, Is.EqualTo(request.ApplicationId));
            Assert.That(protocol.HopByHopIdentifier, Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(protocol.EndToEndIdentifier, Is.EqualTo(request.EndToEndIdentifier));
            Assert.That(protocol.Avps[0].Code, Is.EqualTo(DiameterAvpCodes.SessionId));
            Assert.That(protocol.Avps[1].Code, Is.EqualTo(DiameterAvpCodes.OriginHost));
            Assert.That(protocol.Avps[2].Code, Is.EqualTo(DiameterAvpCodes.OriginRealm));
            Assert.That(protocol.Avps[3].Code, Is.EqualTo(DiameterAvpCodes.ResultCode));
        });
    }

    [Test]
    public void ExperimentalErrorIncludesDiagnosticsAndPropagatesOwnedProxyInfoInOrder()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterMessage request = CreateRequest();
        DiameterBaseAvpFactory avps = new DiameterBaseAvpFactory();
        request.Avps.Add(CreateProxyInfo(avps, "first.proxy", new byte[] { 1, 2 }));
        request.Avps.Add(CreateProxyInfo(avps, "second.proxy", new byte[] { 3, 4 }));
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = request;
        error.ExperimentalResult = new DiameterExperimentalResult();
        error.ExperimentalResult.VendorId = 10415U;
        error.ExperimentalResult.ResultCode = 5001U;
        error.ErrorMessage = "failure-α";
        error.ErrorReportingHost = CreateIdentity("UPSTREAM.Example.");

        DiameterMessage answer = validator.BuildResultErrorAnswer(
            error,
            CreateIdentity("LOCAL.Example."),
            CreateIdentity("EXAMPLE."));
        List<DiameterAvp> experimentalChildren = DecodeGrouped(
            FindAvp(answer, DiameterAvpCodes.ExperimentalResult));
        List<DiameterAvp> proxies = FindAvps(answer, DiameterAvpCodes.ProxyInfo);
        byte[] firstProxyData = new byte[proxies[0].Data.Length];
        Array.Copy(proxies[0].Data, firstProxyData, proxies[0].Data.Length);
        request.Avps[1].Data[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(answer.Flags, Is.EqualTo(DiameterMessageFlags.Proxyable));
            Assert.That(FindAvps(answer, DiameterAvpCodes.ResultCode), Is.Empty);
            Assert.That(experimentalChildren, Has.Count.EqualTo(2));
            Assert.That(ReadUnsigned32(experimentalChildren[0]), Is.EqualTo(10415U));
            Assert.That(ReadUnsigned32(experimentalChildren[1]), Is.EqualTo(5001U));
            Assert.That(Encoding.UTF8.GetString(
                FindAvp(answer, DiameterAvpCodes.ErrorMessage).Data),
                Is.EqualTo("failure-α"));
            Assert.That(Encoding.UTF8.GetString(
                FindAvp(answer, DiameterAvpCodes.ErrorReportingHost).Data),
                Is.EqualTo("upstream.example"));
            Assert.That(FindAvp(answer, DiameterAvpCodes.ErrorReportingHost).Flags,
                Is.EqualTo(DiameterAvpFlags.None));
            Assert.That(proxies, Has.Count.EqualTo(2));
            Assert.That(proxies[0].Data, Is.EqualTo(firstProxyData));
            Assert.That(proxies[0], Is.Not.SameAs(request.Avps[1]));
            Assert.That(proxies[0].Data, Is.Not.SameAs(request.Avps[1].Data));
            Assert.That(Encoding.UTF8.GetString(
                DecodeGrouped(proxies[0])[0].Data), Is.EqualTo("first.proxy"));
            Assert.That(Encoding.UTF8.GetString(
                DecodeGrouped(proxies[1])[0].Data), Is.EqualTo("second.proxy"));
            Assert.That(answer.Avps[^2].Code, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
            Assert.That(answer.Avps[^1].Code, Is.EqualTo(DiameterAvpCodes.ProxyInfo));
        });
    }

    [Test]
    public void ExperimentalProtocolFailureSetsErrorBitAndSameReportingHostIsOmitted()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = CreateRequest();
        error.ExperimentalResult = new DiameterExperimentalResult();
        error.ExperimentalResult.VendorId = 10415U;
        error.ExperimentalResult.ResultCode = 3001U;
        error.ErrorReportingHost = CreateIdentity("NODE.Example.");

        DiameterMessage answer = validator.BuildResultErrorAnswer(
            error,
            CreateIdentity("node.example"),
            CreateIdentity("example"));

        Assert.Multiple(() =>
        {
            Assert.That(answer.Flags,
                Is.EqualTo(DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error));
            Assert.That(FindAvps(answer, DiameterAvpCodes.ErrorReportingHost), Is.Empty);
            Assert.That(FindAvps(answer, DiameterAvpCodes.ExperimentalResult), Has.Count.EqualTo(1));
            Assert.That(FindAvps(answer, DiameterAvpCodes.ResultCode), Is.Empty);
        });
    }

    [Test]
    public void ErrorAnswerRejectsAmbiguousAndMalformedOptionalValues()
    {
        IDiameterValidator validator = new DiameterValidator();
        DiameterIdentity valid = CreateIdentity("node.example");
        DiameterErrorAnswerRequest bothResults = CreateExperimentalError(5001U, 10415U);
        bothResults.ResultCode = DiameterResultCodes.UnableToComply;
        DiameterErrorAnswerRequest missingVendor = CreateExperimentalError(5001U, 0U);
        DiameterErrorAnswerRequest successfulExperimental = CreateExperimentalError(2001U, 10415U);
        DiameterErrorAnswerRequest invalidReporter = new DiameterErrorAnswerRequest();
        invalidReporter.Request = CreateRequest();
        invalidReporter.ResultCode = DiameterResultCodes.UnableToComply;
        invalidReporter.ErrorReportingHost = CreateIdentity("bad_name.example");
        DiameterErrorAnswerRequest malformedProxy = new DiameterErrorAnswerRequest();
        malformedProxy.Request = CreateRequest();
        malformedProxy.ResultCode = DiameterResultCodes.UnableToComply;
        DiameterAvp proxy = new DiameterAvp();
        proxy.Code = DiameterAvpCodes.ProxyInfo;
        proxy.Flags = DiameterAvpFlags.Mandatory;
        proxy.Data = null!;
        malformedProxy.Request.Avps.Add(proxy);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(bothResults, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(missingVendor, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(successfulExperimental, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(invalidReporter, valid, valid));
            Assert.Throws<DiameterValidationException>(() =>
                validator.BuildResultErrorAnswer(malformedProxy, valid, valid));
        });

        DiameterErrorAnswerRequest invalidRealm =
            new DiameterErrorAnswerRequest();
        invalidRealm.Request = CreateRequest();
        invalidRealm.ResultCode = DiameterResultCodes.UnableToComply;
        Assert.Throws<DiameterValidationException>(() =>
            validator.BuildResultErrorAnswer(
                invalidRealm,
                valid,
                CreateIdentity("bad realm")));
    }

    private static DiameterMessage BuildStandardAnswer(
        IDiameterValidator validator,
        DiameterMessage request,
        uint resultCode,
        DiameterIdentity host,
        DiameterIdentity realm)
    {
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = request;
        error.ResultCode = resultCode;
        return validator.BuildResultErrorAnswer(error, host, realm);
    }

    private static DiameterErrorAnswerRequest CreateExperimentalError(
        uint resultCode,
        uint vendorId)
    {
        DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
        error.Request = CreateRequest();
        error.ExperimentalResult = new DiameterExperimentalResult();
        error.ExperimentalResult.VendorId = vendorId;
        error.ExperimentalResult.ResultCode = resultCode;
        return error;
    }

    private static DiameterMessage CreateRequest()
    {
        DiameterMessage request = new DiameterMessage();
        request.Version = DiameterProtocolConstants.Version;
        request.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        request.CommandCode = DiameterCommandCodes.ReAuth;
        request.ApplicationId = DiameterApplicationIds.Common;
        request.HopByHopIdentifier = 0x01020304U;
        request.EndToEndIdentifier = 0x05060708U;
        request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.SessionId,
            "node.example;3913056000;1"));
        return request;
    }

    private static DiameterAvp CreateProxyInfo(
        IDiameterBaseAvpFactory factory,
        string host,
        byte[] state)
    {
        DiameterProxyInformation information = new DiameterProxyInformation();
        information.ProxyHost = CreateIdentity(host);
        information.ProxyState = state;
        return factory.CreateProxyInfoAvp(information);
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(value);
        return avp;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
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

    private static List<DiameterAvp> FindAvps(DiameterMessage message, uint code)
    {
        List<DiameterAvp> matches = new List<DiameterAvp>();
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == 0U)
            {
                matches.Add(avp);
            }

            index = index + 1;
        }

        return matches;
    }

    private static DiameterAvp FindAvp(DiameterMessage message, uint code)
    {
        List<DiameterAvp> matches = FindAvps(message, code);
        if (matches.Count == 0)
        {
            throw new AssertionException($"AVP {code} was not found.");
        }

        return matches[0];
    }

    private static uint ReadUnsigned32(DiameterAvp avp)
    {
        DiameterValueResult decoded = new DiameterAvpCodec().DecodeUnsigned32(avp.Data);
        Assert.That(decoded.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return decoded.Unsigned32;
    }
}
