namespace dns.Tests.Api;

public class PublicApiBehaviorCoverageTests
{
    [Test]
    public void DependencyConstructorsRejectEveryMissingService()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsQueryClient queryClient = new DnsQueryClient();
        DnsTransferClient transferClient = new DnsTransferClient();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new DnsAuthoritativeService(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => new DnsServer(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => new DnsTransferService(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => new DnsUpdateService(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => new DnsNotifyService(
                    null!, queryClient, transferClient),
                Throws.ArgumentNullException);
            Assert.That(
                () => new DnsNotifyService(
                    store, null!, transferClient),
                Throws.ArgumentNullException);
            Assert.That(
                () => new DnsNotifyService(
                    store, queryClient, null!),
                Throws.ArgumentNullException);
        });
    }
}
