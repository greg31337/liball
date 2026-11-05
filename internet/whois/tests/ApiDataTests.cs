using whois;

namespace whois.Tests;

public class ApiDataTests
{
    [Test]
    public void LookupModelHasSafeDefaults()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        WhoisLookupResult result = new WhoisLookupResult();

        Assert.That(request.Text, Is.Empty);
        Assert.That(request.Kind, Is.EqualTo(WhoisQueryKind.Automatic));
        Assert.That(request.Options, Is.Not.Null);
        Assert.That(request.EndpointOverride, Is.Null);
        Assert.That(request.CancellationToken.CanBeCanceled, Is.False);
        Assert.That(request.Options.Timeouts, Is.Not.Null);
        Assert.That(request.Options.Limits, Is.Not.Null);
        Assert.That(request.Options.AddressPreference, Is.EqualTo(WhoisAddressPreference.System));
        Assert.That(request.Options.Encoding, Is.Not.Null);
        Assert.That(request.Options.Encoding.DecodingMode, Is.EqualTo(WhoisDecodingMode.Strict));

        Assert.That(result.Query, Is.Not.Null);
        Assert.That(result.Hops, Is.Empty);
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void LineAndReferralHaveUnspecifiedDefaults()
    {
        WhoisLine line = new WhoisLine();
        WhoisReferral referral = new WhoisReferral();

        Assert.That(line.LineNumber, Is.Zero);
        Assert.That(line.RawContent, Is.Empty);
        Assert.That(line.NormalizedContent, Is.Empty);
        Assert.That(line.Kind, Is.EqualTo(WhoisLineKind.Blank));
        Assert.That(line.FieldIndex, Is.EqualTo(-1));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Unspecified));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Unknown));
        Assert.That(referral.SourceField, Is.Empty);
        Assert.That(referral.SourceLine, Is.Zero);
        Assert.That(referral.AdvertisedValue, Is.Empty);
        Assert.That(referral.Endpoint, Is.Null);
        Assert.That(referral.PathSuffix, Is.Empty);
        Assert.That(referral.IsValid, Is.False);
        Assert.That(referral.RejectionReason, Is.Empty);
    }

    [Test]
    public void EndpointDefaultsToAnUnspecifiedWhoisDestination()
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();

        Assert.That(endpoint.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(endpoint.Host, Is.Empty);
        Assert.That(endpoint.Port, Is.EqualTo(WhoisDefaults.WhoisPort));
        Assert.That(endpoint.AdvertisedValue, Is.Empty);
    }

    [Test]
    public void TimeoutOptionsUseConservativeDefaults()
    {
        WhoisTimeoutOptions options = new WhoisTimeoutOptions();

        Assert.That(options.Resolve, Is.EqualTo(WhoisDefaults.ResolveTimeout));
        Assert.That(options.Connect, Is.EqualTo(WhoisDefaults.ConnectTimeout));
        Assert.That(options.Write, Is.EqualTo(WhoisDefaults.WriteTimeout));
        Assert.That(options.ReadIdle, Is.EqualTo(WhoisDefaults.ReadIdleTimeout));
        Assert.That(options.Overall, Is.EqualTo(WhoisDefaults.OverallTimeout));
    }

    [Test]
    public void LimitsUseBoundedDefaults()
    {
        WhoisLimits limits = new WhoisLimits();

        Assert.That(limits.MaximumEncodedQueryBytes, Is.EqualTo(WhoisDefaults.MaximumEncodedQueryBytes));
        Assert.That(limits.MaximumResponseBytes, Is.EqualTo(WhoisDefaults.MaximumResponseBytes));
        Assert.That(limits.MaximumReferralHops, Is.EqualTo(WhoisDefaults.MaximumReferralHops));
        Assert.That(limits.MaximumLines, Is.EqualTo(WhoisDefaults.MaximumLines));
        Assert.That(limits.MaximumFields, Is.EqualTo(WhoisDefaults.MaximumFields));
    }

    [Test]
    public void EncodingOptionsUseStrictAsciiAndAutomaticResponseDetection()
    {
        WhoisEncodingOptions options = new WhoisEncodingOptions();

        Assert.That(options.RequestEncoding.WebName, Is.EqualTo("us-ascii"));
        Assert.That(options.ResponseEncoding, Is.Null);
        Assert.That(options.DetectByteOrderMark, Is.True);
        Assert.That(options.DecodingMode, Is.EqualTo(WhoisDecodingMode.Strict));
        Assert.Throws<System.Text.EncoderFallbackException>(() => options.RequestEncoding.GetBytes("é"));
    }

    [Test]
    public void ClientOptionsOwnIndependentNestedOptionObjects()
    {
        WhoisClientOptions first = new WhoisClientOptions();
        WhoisClientOptions second = new WhoisClientOptions();

        Assert.That(first.Timeouts, Is.Not.SameAs(second.Timeouts));
        Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
        Assert.That(first.Encoding, Is.Not.SameAs(second.Encoding));
    }

    [Test]
    public void ClientOptionsUseSafeTraversalAndEndpointPolicyDefaults()
    {
        WhoisClientOptions options = new WhoisClientOptions();

        Assert.That(options.FollowReferrals, Is.True);
        Assert.That(options.RetainDiscoveryResponses, Is.True);
        Assert.That(options.AllowNonPublicDirectEndpoints, Is.True);
        Assert.That(options.AllowNonPublicAutomaticEndpoints, Is.False);
    }

    [Test]
    public void QueryDefaultsToInvalidUnclassifiedInput()
    {
        WhoisQuery query = new WhoisQuery();

        Assert.That(query.OriginalText, Is.Empty);
        Assert.That(query.NormalizedText, Is.Empty);
        Assert.That(query.RoutingKey, Is.Empty);
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.Automatic));
        Assert.That(query.IsValid, Is.False);
        Assert.That(query.ValidationMessage, Is.Empty);
    }

    [Test]
    public void ServerSelectionDefaultsToAutomaticSelectionWithoutOverride()
    {
        WhoisServerSelection selection = new WhoisServerSelection();

        Assert.That(selection.Endpoint, Is.Not.Null);
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.InitialSelection));
        Assert.That(selection.QueryOverride, Is.Null);
        Assert.That(selection.Reason, Is.Empty);
    }

    [Test]
    public void ExchangeRequestHasIndependentSafeDefaults()
    {
        WhoisRequest first = new WhoisRequest();
        WhoisRequest second = new WhoisRequest();

        Assert.That(first.Endpoint, Is.Not.Null);
        Assert.That(first.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(first.QueryText, Is.Empty);
        Assert.That(first.Encoding, Is.Not.Null);
        Assert.That(first.Timeouts, Is.Not.Null);
        Assert.That(first.Limits, Is.Not.Null);
        Assert.That(first.CancellationToken.CanBeCanceled, Is.False);
        Assert.That(first.Endpoint, Is.Not.SameAs(second.Endpoint));
        Assert.That(first.Encoding, Is.Not.SameAs(second.Encoding));
        Assert.That(first.Timeouts, Is.Not.SameAs(second.Timeouts));
        Assert.That(first.Limits, Is.Not.SameAs(second.Limits));
    }

    [Test]
    public void LookupRequestAcceptsCallerOverridesAndCancellation()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        WhoisLookupRequest request = new WhoisLookupRequest();

        endpoint.Host = "whois.example";
        endpoint.Port = 1043;
        request.Text = "example.test";
        request.Kind = WhoisQueryKind.Domain;
        request.EndpointOverride = endpoint;
        request.CancellationToken = cancellation.Token;

        Assert.That(request.Text, Is.EqualTo("example.test"));
        Assert.That(request.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(request.EndpointOverride, Is.SameAs(endpoint));
        Assert.That(request.CancellationToken, Is.EqualTo(cancellation.Token));
    }
}
