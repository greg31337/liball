namespace cookies.Tests;

public class CookieSecureOriginEvaluatorTests
{
    [Test]
    public void ReplacementPolicyCanTrustAnOriginRejectedByTheDefault()
    {
        Uri origin = new Uri("custom://device.example/resource");
        RecordingSecureOriginPolicy replacement = new RecordingSecureOriginPolicy(true);

        CookieBooleanResult result = CookieSecureOriginEvaluator.Evaluate(replacement, origin);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.True);
        Assert.That(replacement.Origins, Is.EqualTo(new[] { origin }));
    }

    [Test]
    public void ReplacementPolicyCanRejectAnOriginTrustedByTheDefault()
    {
        Uri origin = new Uri("https://example.test/path");
        RecordingSecureOriginPolicy replacement = new RecordingSecureOriginPolicy(false);

        CookieBooleanResult result = CookieSecureOriginEvaluator.Evaluate(replacement, origin);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.False);
        Assert.That(replacement.Origins, Is.EqualTo(new[] { origin }));
    }

    [Test]
    public void MissingOrThrowingReplacementPolicyFailsDeterministically()
    {
        Uri origin = new Uri("https://example.test/path");

        CookieBooleanResult missing = CookieSecureOriginEvaluator.Evaluate(null!, origin);
        CookieBooleanResult throwing = CookieSecureOriginEvaluator.Evaluate(
            new ThrowingSecureOriginPolicy(), origin);

        Assert.That(missing.Success, Is.False);
        Assert.That(missing.Value, Is.False);
        Assert.That(throwing.Success, Is.False);
        Assert.That(throwing.Value, Is.False);
    }
}

public class RecordingSecureOriginPolicy : ISecureOriginPolicy
{
    public bool Result;
    public List<Uri> Origins = [];

    public RecordingSecureOriginPolicy(bool result)
    {
        Result = result;
    }

    public bool IsSecureOrigin(Uri origin)
    {
        Origins.Add(origin);
        return Result;
    }
}

public class ThrowingSecureOriginPolicy : ISecureOriginPolicy
{
    public bool IsSecureOrigin(Uri origin)
    {
        throw new InvalidOperationException("replacement failure");
    }
}
