using uspto;

namespace uspto.Tests;

public class TrademarkDecisionCapabilityProviderTests
{
    [Test]
    public void CapabilityRejectsPortalAutomationAndDirectsCallersToTtabBulkDiscovery()
    {
        TrademarkDecisionCapabilityResult result = TrademarkDecisionCapabilityProvider.GetCapability(new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.CapabilityName, Is.EqualTo("Trademark Decisions and Proceedings"));
            Assert.That(result.IsPublishedApiAvailable, Is.False);
            Assert.That(result.UnavailabilityCode, Is.EqualTo("NO_PUBLISHED_API"));
            Assert.That(result.UnavailabilityReason, Does.Contain("not published a stable API"));
            Assert.That(result.IsPortalAutomationSupported, Is.False);
            Assert.That(result.IsBulkDiscoveryAvailable, Is.True);
            Assert.That(result.BulkDatasetFamilies, Is.EqualTo(new[] { DatasetFamily.Ttab }));
            Assert.That(result.BulkProductSearch.Query, Is.EqualTo("Trademark Trial and Appeal Board"));
            Assert.That(result.BulkProductSearch.Context.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(result.Provenance, Is.EqualTo(DataProvenance.ClientDerived));
        });
    }

    [Test]
    public void BulkSearchReceivesASeparateCopyOfCallerControlMetadata()
    {
        DateTimeOffset deadline = new DateTimeOffset(2030, 2, 3, 4, 5, 6, TimeSpan.Zero);
        RequestContext source = new RequestContext
        {
            Service = ApiService.Tsdr,
            RetainRawResponse = true,
            MaximumRawResponseBytes = 321L,
            Deadline = deadline,
            HasDeadline = true,
            CorrelationIdentifier = "capability-request"
        };

        TrademarkDecisionCapabilityResult result = TrademarkDecisionCapabilityProvider.GetCapability(source);
        RequestContext bulkContext = result.BulkProductSearch.Context;

        Assert.Multiple(() =>
        {
            Assert.That(bulkContext, Is.Not.SameAs(source));
            Assert.That(bulkContext.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(bulkContext.RetainRawResponse, Is.True);
            Assert.That(bulkContext.MaximumRawResponseBytes, Is.EqualTo(321L));
            Assert.That(bulkContext.Deadline, Is.EqualTo(deadline));
            Assert.That(bulkContext.HasDeadline, Is.True);
            Assert.That(bulkContext.CorrelationIdentifier, Is.EqualTo("capability-request"));
            Assert.That(source.Service, Is.EqualTo(ApiService.Tsdr));
        });
    }

    [Test]
    public void CapabilityResultsDoNotShareMutableBulkGuidance()
    {
        TrademarkDecisionCapabilityResult first = TrademarkDecisionCapabilityProvider.GetCapability(new RequestContext());
        TrademarkDecisionCapabilityResult second = TrademarkDecisionCapabilityProvider.GetCapability(new RequestContext());

        first.BulkDatasetFamilies[0] = DatasetFamily.Unknown;
        first.BulkProductSearch.Query = "changed";
        first.BulkProductSearch.Context.CorrelationIdentifier = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.BulkDatasetFamilies, Is.EqualTo(new[] { DatasetFamily.Ttab }));
            Assert.That(second.BulkProductSearch.Query, Is.EqualTo("Trademark Trial and Appeal Board"));
            Assert.That(second.BulkProductSearch.Context.CorrelationIdentifier, Is.Null);
        });
    }

    [Test]
    public void MissingOrCancelledContextsStopCapabilityEvaluation()
    {
        ValidationException missing = Assert.Throws<ValidationException>(() => TrademarkDecisionCapabilityProvider.GetCapability(null!));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException cancelled = Assert.Throws<OperationCanceledException>(() => TrademarkDecisionCapabilityProvider.GetCapability(new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(missing!.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(missing.FieldName, Is.EqualTo("context"));
            Assert.That(cancelled!.CancellationToken, Is.EqualTo(cancellation.Token));
        });
    }
}
