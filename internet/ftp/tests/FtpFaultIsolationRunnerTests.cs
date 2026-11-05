namespace ftp.Tests;

public class FtpFaultIsolationRunnerTests
{
    [Test]
    public void Run_operation_returns_its_value_once()
    {
        int calls = 0;

        FtpFaultIsolationResult result = FtpFaultIsolationRunner.RunOperation(() =>
        {
            calls++;
            return "completed";
        });

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Canceled, Is.False);
            Assert.That(result.Value, Is.EqualTo("completed"));
            Assert.That(result.Failure, Is.Null);
        });
    }

    [Test]
    public void Run_operation_captures_a_fault_and_allows_followup_work()
    {
        InvalidOperationException failure = new InvalidOperationException("provider fault");

        FtpFaultIsolationResult faulted = FtpFaultIsolationRunner.RunOperation(() => throw failure);
        FtpFaultIsolationResult followup = FtpFaultIsolationRunner.RunOperation(() => "still running");

        Assert.Multiple(() =>
        {
            Assert.That(faulted.Succeeded, Is.False);
            Assert.That(faulted.Canceled, Is.False);
            Assert.That(faulted.Value, Is.Null);
            Assert.That(faulted.Failure, Is.SameAs(failure));
            Assert.That(followup.Succeeded, Is.True);
            Assert.That(followup.Value, Is.EqualTo("still running"));
        });
    }

    [Test]
    public void Observe_task_distinguishes_success_fault_and_cancellation()
    {
        object value = new object();
        ApplicationException failure = new ApplicationException("session fault");
        Task<object?> successfulTask = Task.FromResult<object?>(value);
        Task<object?> faultedTask = Task.FromException<object?>(failure);
        Task<object?> canceledTask = Task.FromCanceled<object?>(new CancellationToken(true));

        FtpFaultIsolationResult successful = FtpFaultIsolationRunner.ObserveTask(successfulTask);
        FtpFaultIsolationResult faulted = FtpFaultIsolationRunner.ObserveTask(faultedTask);
        FtpFaultIsolationResult canceled = FtpFaultIsolationRunner.ObserveTask(canceledTask);

        Assert.Multiple(() =>
        {
            Assert.That(successful.Succeeded, Is.True);
            Assert.That(successful.Canceled, Is.False);
            Assert.That(successful.Value, Is.SameAs(value));
            Assert.That(successful.Failure, Is.Null);
            Assert.That(faulted.Succeeded, Is.False);
            Assert.That(faulted.Canceled, Is.False);
            Assert.That(faulted.Value, Is.Null);
            Assert.That(faulted.Failure, Is.SameAs(failure));
            Assert.That(canceled.Succeeded, Is.False);
            Assert.That(canceled.Canceled, Is.True);
            Assert.That(canceled.Value, Is.Null);
            Assert.That(canceled.Failure, Is.Null);
        });
    }
}
