using whois;

namespace whois.Tests;

public class ApiEnumTests
{
    [Test]
    public void AddressPreferenceValuesAreStable()
    {
        Assert.That((int)WhoisAddressPreference.System, Is.EqualTo(0));
        Assert.That((int)WhoisAddressPreference.IPv4First, Is.EqualTo(1));
        Assert.That((int)WhoisAddressPreference.IPv6First, Is.EqualTo(2));
    }

    [Test]
    public void DecodingModeValuesAreStable()
    {
        Assert.That((int)WhoisDecodingMode.Strict, Is.EqualTo(0));
        Assert.That((int)WhoisDecodingMode.ReplaceInvalidUtf8, Is.EqualTo(1));
        Assert.That((int)WhoisDecodingMode.Latin1Fallback, Is.EqualTo(2));
    }

    [Test]
    public void EndpointSourceValuesAreStable()
    {
        Assert.That((int)WhoisEndpointSource.Direct, Is.EqualTo(0));
        Assert.That((int)WhoisEndpointSource.InitialSelection, Is.EqualTo(1));
        Assert.That((int)WhoisEndpointSource.Referral, Is.EqualTo(2));
    }

    [Test]
    public void ExchangeStatusValuesAreStable()
    {
        Assert.That((int)WhoisExchangeStatus.Complete, Is.EqualTo(0));
        Assert.That((int)WhoisExchangeStatus.Cancelled, Is.EqualTo(1));
        Assert.That((int)WhoisExchangeStatus.TimedOut, Is.EqualTo(2));
        Assert.That((int)WhoisExchangeStatus.ResponseLimitReached, Is.EqualTo(3));
        Assert.That((int)WhoisExchangeStatus.ResolveFailed, Is.EqualTo(4));
        Assert.That((int)WhoisExchangeStatus.ConnectFailed, Is.EqualTo(5));
        Assert.That((int)WhoisExchangeStatus.WriteFailed, Is.EqualTo(6));
        Assert.That((int)WhoisExchangeStatus.ReadFailed, Is.EqualTo(7));
    }

    [Test]
    public void LineKindValuesAreStable()
    {
        Assert.That((int)WhoisLineKind.Blank, Is.EqualTo(0));
        Assert.That((int)WhoisLineKind.Comment, Is.EqualTo(1));
        Assert.That((int)WhoisLineKind.Field, Is.EqualTo(2));
        Assert.That((int)WhoisLineKind.Continuation, Is.EqualTo(3));
        Assert.That((int)WhoisLineKind.Text, Is.EqualTo(4));
    }

    [Test]
    public void OperationStageValuesAreStable()
    {
        Assert.That((int)WhoisOperationStage.None, Is.EqualTo(0));
        Assert.That((int)WhoisOperationStage.Select, Is.EqualTo(1));
        Assert.That((int)WhoisOperationStage.Resolve, Is.EqualTo(2));
        Assert.That((int)WhoisOperationStage.Connect, Is.EqualTo(3));
        Assert.That((int)WhoisOperationStage.Write, Is.EqualTo(4));
        Assert.That((int)WhoisOperationStage.Read, Is.EqualTo(5));
        Assert.That((int)WhoisOperationStage.Decode, Is.EqualTo(6));
        Assert.That((int)WhoisOperationStage.Parse, Is.EqualTo(7));
        Assert.That((int)WhoisOperationStage.Refer, Is.EqualTo(8));
    }

    [Test]
    public void QueryKindValuesAreStable()
    {
        Assert.That((int)WhoisQueryKind.Automatic, Is.EqualTo(0));
        Assert.That((int)WhoisQueryKind.Domain, Is.EqualTo(1));
        Assert.That((int)WhoisQueryKind.IPv4Address, Is.EqualTo(2));
        Assert.That((int)WhoisQueryKind.IPv6Address, Is.EqualTo(3));
        Assert.That((int)WhoisQueryKind.IPv4Prefix, Is.EqualTo(4));
        Assert.That((int)WhoisQueryKind.IPv6Prefix, Is.EqualTo(5));
        Assert.That((int)WhoisQueryKind.AutonomousSystem, Is.EqualTo(6));
        Assert.That((int)WhoisQueryKind.ReverseDomain, Is.EqualTo(7));
        Assert.That((int)WhoisQueryKind.FreeForm, Is.EqualTo(8));
    }

    [Test]
    public void ReferralKindValuesAreStable()
    {
        Assert.That((int)WhoisReferralKind.Iana, Is.EqualTo(0));
        Assert.That((int)WhoisReferralKind.Registry, Is.EqualTo(1));
        Assert.That((int)WhoisReferralKind.Registrar, Is.EqualTo(2));
        Assert.That((int)WhoisReferralKind.Generic, Is.EqualTo(3));
        Assert.That((int)WhoisReferralKind.RWhois, Is.EqualTo(4));
        Assert.That((int)WhoisReferralKind.Web, Is.EqualTo(5));
        Assert.That((int)WhoisReferralKind.Unknown, Is.EqualTo(6));
    }

    [Test]
    public void ReferralSchemeValuesAreStable()
    {
        Assert.That((int)WhoisReferralScheme.Unspecified, Is.EqualTo(0));
        Assert.That((int)WhoisReferralScheme.Whois, Is.EqualTo(1));
        Assert.That((int)WhoisReferralScheme.RWhois, Is.EqualTo(2));
        Assert.That((int)WhoisReferralScheme.Http, Is.EqualTo(3));
        Assert.That((int)WhoisReferralScheme.Https, Is.EqualTo(4));
        Assert.That((int)WhoisReferralScheme.Unsupported, Is.EqualTo(5));
    }
}
