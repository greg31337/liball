using whois;

namespace whois.Tests;

public class DnsResolverTests
{
    [TestCase("192.0.2.10", WhoisAddressPreference.IPv6First)]
    [TestCase("2001:db8::10", WhoisAddressPreference.IPv4First)]
    public void ResolvesIpLiteralsWithoutDns(string host, WhoisAddressPreference preference)
    {
        IWhoisHostResolver resolver = new WhoisDnsResolver();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = host;

        WhoisResolution result = resolver.ResolveHost(endpoint, preference, default(CancellationToken));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(result.Addresses, Is.EqualTo(new[] { System.Net.IPAddress.Parse(host) }));
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
    }

    [Test]
    public void OrdersFamiliesStablyAndRemovesDuplicates()
    {
        System.Net.IPAddress ipv6First = System.Net.IPAddress.Parse("2001:db8::1");
        System.Net.IPAddress ipv4First = System.Net.IPAddress.Parse("192.0.2.1");
        System.Net.IPAddress ipv6Second = System.Net.IPAddress.Parse("2001:db8::2");
        System.Net.IPAddress ipv4Second = System.Net.IPAddress.Parse("192.0.2.2");
        System.Net.IPAddress[] supplied = new[] { ipv6First, ipv4First, ipv6Second, ipv4First, ipv4Second, ipv6First };

        System.Net.IPAddress[] system = WhoisDnsResolver.OrderAddresses(supplied, WhoisAddressPreference.System);
        System.Net.IPAddress[] ipv4 = WhoisDnsResolver.OrderAddresses(supplied, WhoisAddressPreference.IPv4First);
        System.Net.IPAddress[] ipv6 = WhoisDnsResolver.OrderAddresses(supplied, WhoisAddressPreference.IPv6First);

        Assert.That(system, Is.EqualTo(new[] { ipv6First, ipv4First, ipv6Second, ipv4Second }));
        Assert.That(ipv4, Is.EqualTo(new[] { ipv4First, ipv4Second, ipv6First, ipv6Second }));
        Assert.That(ipv6, Is.EqualTo(new[] { ipv6First, ipv6Second, ipv4First, ipv4Second }));
    }

    [Test]
    public void CompletedTaskProducesOrderedResolution()
    {
        System.Net.IPAddress ipv4 = System.Net.IPAddress.Parse("192.0.2.1");
        System.Net.IPAddress ipv6 = System.Net.IPAddress.Parse("2001:db8::1");
        Task<System.Net.IPAddress[]> task = Task.FromResult(new[] { ipv6, ipv4, ipv4 });

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(task, task, WhoisAddressPreference.IPv4First, default(CancellationToken), TimeSpan.FromMilliseconds(3));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(result.Addresses, Is.EqualTo(new[] { ipv4, ipv6 }));
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(3)));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void IncompleteTaskLosingRaceProducesTimeoutResult()
    {
        TaskCompletionSource<System.Net.IPAddress[]> source = new TaskCompletionSource<System.Net.IPAddress[]>();

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(source.Task, Task.CompletedTask, WhoisAddressPreference.System, default(CancellationToken), TimeSpan.FromMilliseconds(5));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.TypeOf<TimeoutException>());
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(5)));
    }

    [Test]
    public void CancelledDnsTaskWithoutCallerCancellationProducesTimeoutResult()
    {
        using CancellationTokenSource operationCancellation = new CancellationTokenSource();
        operationCancellation.Cancel();
        Task<System.Net.IPAddress[]> task = Task.FromCanceled<System.Net.IPAddress[]>(operationCancellation.Token);

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(task, task, WhoisAddressPreference.System, default(CancellationToken), TimeSpan.FromMilliseconds(7));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.TimedOut));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.TypeOf<TimeoutException>());
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(7)));
    }

    [Test]
    public void FaultedDnsTaskProducesResolutionFailureWithUnderlyingError()
    {
        System.Net.Sockets.SocketException failure = new System.Net.Sockets.SocketException(11001);
        Task<System.Net.IPAddress[]> task = Task.FromException<System.Net.IPAddress[]>(failure);

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(task, task, WhoisAddressPreference.IPv6First, default(CancellationToken), TimeSpan.FromMilliseconds(9));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.ResolveFailed));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.SameAs(failure));
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(9)));
    }

    [Test]
    public void EmptyDnsResultProducesResolutionFailure()
    {
        Task<System.Net.IPAddress[]> task = Task.FromResult(Array.Empty<System.Net.IPAddress>());

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(task, task, WhoisAddressPreference.System, default(CancellationToken), TimeSpan.FromMilliseconds(2));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.ResolveFailed));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(2)));
    }

    [Test]
    public void CallerCancellationTakesPrecedenceOverTimeoutSignal()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        TaskCompletionSource<System.Net.IPAddress[]> source = new TaskCompletionSource<System.Net.IPAddress[]>();
        cancellation.Cancel();

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(source.Task, Task.CompletedTask, WhoisAddressPreference.System, cancellation.Token, TimeSpan.FromMilliseconds(4));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.TypeOf<OperationCanceledException>());
        Assert.That(((OperationCanceledException)result.Error!).CancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public void CallerCancellationTakesPrecedenceOverTaskCompletion()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        InvalidOperationException failure = new InvalidOperationException("DNS failed");
        Task<System.Net.IPAddress[]> task = Task.FromException<System.Net.IPAddress[]>(failure);
        cancellation.Cancel();

        WhoisResolution result = WhoisDnsResolver.CreateResolutionFromTask(task, task, WhoisAddressPreference.System, cancellation.Token, TimeSpan.FromMilliseconds(1));

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void PreCancelledPublicResolutionDoesNotStartDns()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        IWhoisHostResolver resolver = new WhoisDnsResolver();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "not-resolved.invalid";
        cancellation.Cancel();

        WhoisResolution result = resolver.ResolveHost(endpoint, WhoisAddressPreference.System, cancellation.Token);

        Assert.That(result.Status, Is.EqualTo(WhoisExchangeStatus.Cancelled));
        Assert.That(result.Addresses, Is.Empty);
        Assert.That(result.Error, Is.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void RejectsUnknownAddressPreferenceBeforeResolution()
    {
        IWhoisHostResolver resolver = new WhoisDnsResolver();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "192.0.2.10";

        Assert.Throws<ArgumentOutOfRangeException>(() => resolver.ResolveHost(endpoint, (WhoisAddressPreference)99, default(CancellationToken)));
    }
}
