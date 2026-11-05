namespace pop3.Tests;

public class Pop3Utf8ValidatorTests
{
    [TestCaseSource(nameof(ValidUtf8Sequences))]
    public void ValidatorAcceptsRfc3629BoundarySequences(byte[] value)
    {
        Assert.That(new Pop3Utf8Validator().IsValidUtf8(value), Is.True);
    }

    [TestCaseSource(nameof(InvalidUtf8Sequences))]
    public void ValidatorRejectsMalformedOverlongSurrogateAndOutOfRangeSequences(byte[] value)
    {
        Assert.That(new Pop3Utf8Validator().IsValidUtf8(value), Is.False);
    }

    static IEnumerable<TestCaseData> ValidUtf8Sequences()
    {
        yield return new TestCaseData(Array.Empty<byte>());
        yield return new TestCaseData(new byte[] { 0 });
        yield return new TestCaseData(new byte[] { 127 });
        yield return new TestCaseData(new byte[] { 194, 128 });
        yield return new TestCaseData(new byte[] { 223, 191 });
        yield return new TestCaseData(new byte[] { 224, 160, 128 });
        yield return new TestCaseData(new byte[] { 237, 159, 191 });
        yield return new TestCaseData(new byte[] { 238, 128, 128 });
        yield return new TestCaseData(new byte[] { 239, 191, 191 });
        yield return new TestCaseData(new byte[] { 240, 144, 128, 128 });
        yield return new TestCaseData(new byte[] { 244, 143, 191, 191 });
        yield return new TestCaseData(new byte[] { 65, 194, 162, 226, 130, 172, 240, 144, 141, 136 });
    }

    static IEnumerable<TestCaseData> InvalidUtf8Sequences()
    {
        yield return new TestCaseData(new byte[] { 128 });
        yield return new TestCaseData(new byte[] { 191 });
        yield return new TestCaseData(new byte[] { 192, 128 });
        yield return new TestCaseData(new byte[] { 193, 191 });
        yield return new TestCaseData(new byte[] { 194 });
        yield return new TestCaseData(new byte[] { 194, 65 });
        yield return new TestCaseData(new byte[] { 224, 159, 191 });
        yield return new TestCaseData(new byte[] { 224, 160 });
        yield return new TestCaseData(new byte[] { 237, 160, 128 });
        yield return new TestCaseData(new byte[] { 238, 128, 65 });
        yield return new TestCaseData(new byte[] { 240, 143, 191, 191 });
        yield return new TestCaseData(new byte[] { 240, 144, 128 });
        yield return new TestCaseData(new byte[] { 244, 144, 128, 128 });
        yield return new TestCaseData(new byte[] { 245, 128, 128, 128 });
        yield return new TestCaseData(new byte[] { 255 });
    }
}
