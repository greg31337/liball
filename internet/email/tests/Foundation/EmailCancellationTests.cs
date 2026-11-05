using System.Threading;
using email;

namespace email.Tests;

public class EmailCancellationTests
{
    [Test]
    public void CancellationIsExplicitMonotonicAndIdempotent()
    {
        EmailCancellation cancellation = new EmailCancellation();

        Assert.That(cancellation.IsCancellationRequested(), Is.False);
        cancellation.Cancel();
        Assert.That(cancellation.IsCancellationRequested(), Is.True);
        cancellation.Cancel();
        Assert.That(cancellation.IsCancellationRequested(), Is.True);
    }

    [Test]
    public void CancellationRequestedOnAnotherThreadBecomesVisible()
    {
        EmailCancellation cancellation = new EmailCancellation();
        bool observed = false;
        Thread observer = new Thread(() =>
        {
            while (!cancellation.IsCancellationRequested())
            {
                Thread.Yield();
            }

            observed = true;
        });

        observer.Start();
        cancellation.Cancel();
        bool completed = observer.Join(5000);

        Assert.That(completed, Is.True);
        Assert.That(observed, Is.True);
    }
}
