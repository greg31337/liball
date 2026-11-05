namespace jmap.Tests;

public class RequestLimitGateTests
{
    [Test]
    public void ApiAdmissionAppliesSizeCallAndConcurrencyLimitsAndReleasesOnce()
    {
        RequestLimitGate gate = new RequestLimitGate(Options());

        RequestLimitAdmission first = gate.AcquireApiRequest(
            100L, 2, CancellationToken.None);
        RequestLimitAdmission concurrent = gate.AcquireApiRequest(
            1L, 1, CancellationToken.None);
        RequestLimitAdmission tooLarge = gate.AcquireApiRequest(
            101L, 1, CancellationToken.None);
        RequestLimitAdmission tooManyCalls = gate.AcquireApiRequest(
            1L, 3, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.IsAllowed, Is.True);
            Assert.That(first.Lease, Is.Not.Null);
            Assert.That(first.Problem, Is.Null);
            AssertLimit(concurrent, JmapProperties.MaxConcurrentRequests);
            AssertLimit(tooLarge, JmapProperties.MaxSizeRequest);
            AssertLimit(tooManyCalls, JmapProperties.MaxCallsInRequest);
        }

        first.Lease!.Dispose();
        first.Lease.Dispose();
        RequestLimitAdmission after = gate.AcquireApiRequest(
            100L, 2, CancellationToken.None);
        Assert.That(after.IsAllowed, Is.True);
        after.Lease!.Dispose();
    }

    [Test]
    public void UploadAdmissionAppliesKnownSizeAndConcurrencyLimitsWithoutConsumingDeniedPermits()
    {
        RequestLimitGate gate = new RequestLimitGate(Options());

        RequestLimitAdmission first = gate.AcquireUpload(50L, true, CancellationToken.None);
        RequestLimitAdmission concurrent = gate.AcquireUpload(1L, true, CancellationToken.None);
        RequestLimitAdmission tooLarge = gate.AcquireUpload(51L, true, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.IsAllowed, Is.True);
            AssertLimit(concurrent, JmapProperties.MaxConcurrentUpload);
            AssertLimit(tooLarge, JmapProperties.MaxSizeUpload);
        }

        first.Lease!.Dispose();
        RequestLimitAdmission unknownLength = gate.AcquireUpload(
            0L, false, CancellationToken.None);
        Assert.That(unknownLength.IsAllowed, Is.True);
        unknownLength.Lease!.Dispose();
    }

    [Test]
    public void CanceledAdmissionDoesNotConsumeAnApiOrUploadPermit()
    {
        RequestLimitGate gate = new RequestLimitGate(Options());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(() => gate.AcquireApiRequest(1L, 1, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(() => gate.AcquireUpload(1L, true, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());

        RequestLimitAdmission api = gate.AcquireApiRequest(1L, 1, CancellationToken.None);
        RequestLimitAdmission upload = gate.AcquireUpload(1L, true, CancellationToken.None);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(api.IsAllowed, Is.True);
            Assert.That(upload.IsAllowed, Is.True);
        }

        api.Lease!.Dispose();
        upload.Lease!.Dispose();
    }

    [Test]
    public void ObjectLimitsReturnRequestTooLargeAtMethodLevelOnlyForExcess()
    {
        RequestLimitGate gate = new RequestLimitGate(Options());

        JmapMethodError? getBoundary = gate.ValidateGetObjectCount(2);
        JmapMethodError? getExcess = gate.ValidateGetObjectCount(3);
        JmapMethodError? setBoundary = gate.ValidateSetObjectCount(2);
        JmapMethodError? setExcess = gate.ValidateSetObjectCount(3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(getBoundary, Is.Null);
            Assert.That(setBoundary, Is.Null);
            Assert.That(getExcess!.Type, Is.EqualTo(JmapErrorCodes.RequestTooLarge));
            Assert.That(getExcess.Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(setExcess!.Type, Is.EqualTo(JmapErrorCodes.RequestTooLarge));
            Assert.That(setExcess.Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
        }
    }

    [Test]
    public void InvalidConfiguredLimitsAreRejectedBeforeCreatingSemaphores()
    {
        JmapServerOptions noRequests = Options();
        noRequests.MaxConcurrentRequests = 0;
        JmapServerOptions negativeObjects = Options();
        negativeObjects.MaxObjectsInSet = -1;
        JmapServerOptions unsafeBytes = Options();
        unsafeBytes.MaxRequestBytes = 9007199254740992L;

        Assert.That(() => new RequestLimitGate(noRequests),
            Throws.TypeOf<ArgumentException>());
        Assert.That(() => new RequestLimitGate(negativeObjects),
            Throws.TypeOf<ArgumentException>());
        Assert.That(() => new RequestLimitGate(unsafeBytes),
            Throws.TypeOf<ArgumentException>());
    }

    private static void AssertLimit(RequestLimitAdmission admission, string limit)
    {
        Assert.That(admission.IsAllowed, Is.False);
        Assert.That(admission.Lease, Is.Null);
        Assert.That(admission.Problem, Is.Not.Null);
        Assert.That(admission.Problem!.ProblemKind, Is.EqualTo(JmapProblemKind.Limit));
        Assert.That(admission.Problem.Limit, Is.EqualTo(limit));
    }

    private static JmapServerOptions Options()
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxRequestBytes = 100L;
        options.MaxUploadBytes = 50L;
        options.MaxConcurrentRequests = 1;
        options.MaxConcurrentUploads = 1;
        options.MaxCallsInRequest = 2;
        options.MaxObjectsInGet = 2;
        options.MaxObjectsInSet = 2;
        return options;
    }
}
