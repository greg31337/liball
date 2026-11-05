using uspto;

namespace uspto.Tests;

internal sealed class PublicClientTestHarness : IDisposable
{
    internal DeterministicHttpHandler Handler;
    internal HttpClient Client;
    internal TemporaryTestRoots Roots;
    internal RecordingDownloadProgress Progress = new RecordingDownloadProgress();
    internal FakeTestClock Clock = new FakeTestClock(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
    internal RecordingRetryDelay RetryDelay = new RecordingRetryDelay();
    internal FixedRetryRandom RetryRandom = new FixedRetryRandom(0.5);
    private bool disposed;

    internal PublicClientTestHarness()
    {
        Handler = new DeterministicHttpHandler();
        Client = new HttpClient(Handler);
        Roots = new TemporaryTestRoots();
    }

    internal UsptoClientOptions CreateOptions()
    {
        UsptoClientOptions options = new UsptoClientOptions();
        options.HttpClient = Client;
        options.DisposeHttpClient = false;
        options.OdpBaseUri = new Uri("https://odp.example.test/api/v1/");
        options.OfficeActionBaseUri = new Uri("https://office-action.example.test/api/v1/");
        options.TsdrBaseUri = new Uri("https://tsdr.example.test/ts/cd/");
        options.PatentsViewBaseUri = new Uri("https://patents-view.example.test/api/v1/");
        options.OdpApiKey = "odp-test-key";
        options.TsdrApiKey = "tsdr-test-key";
        options.PatentsViewApiKey = "patents-view-test-key";
        options.Retry.AttemptCount = 1;
        options.Retry.UseJitter = false;
        Roots.ApplyTo(options);
        return options;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Client.Dispose();
            Roots.Dispose();
        }
    }
}
