namespace cookies.Tests;

public class CookieSpanScannerTests
{
    [Test]
    public void PairScannerTrimsWhitespaceAndKeepsEqualsSignsInTheValue()
    {
        string input = "  session = a=b  ; Path=/account";
        CookieScanResult result = CookieSpanScanner.ScanCookiePairSegment(input.AsSpan(), 0, input.Length);

        Assert.That(result.Success, Is.True);
        Assert.That(result.InvalidBounds, Is.False);
        Assert.That(result.LimitExceeded, Is.False);
        Assert.That(input.AsSpan(result.SegmentStart, result.SegmentLength).ToString(), Is.EqualTo("session = a=b"));
        Assert.That(input.AsSpan(result.NameStart, result.NameLength).ToString(), Is.EqualTo("session"));
        Assert.That(result.HasValue, Is.True);
        Assert.That(input.AsSpan(result.ValueStart, result.ValueLength).ToString(), Is.EqualTo("a=b"));
        Assert.That(input.AsSpan(result.NextOffset).ToString(), Is.EqualTo(" Path=/account"));
    }

    [Test]
    public void AttributeScannerPreservesFlagAndEmptyValueDistinction()
    {
        string input = "Secure; Empty= ;Path=/";
        CookieScanResult flag = CookieSpanScanner.ScanAttributeSegment(input.AsSpan(), 0, input.Length);
        CookieScanResult emptyValue = CookieSpanScanner.ScanAttributeSegment(input.AsSpan(), flag.NextOffset, input.Length);

        Assert.That(input.AsSpan(flag.NameStart, flag.NameLength).ToString(), Is.EqualTo("Secure"));
        Assert.That(flag.HasValue, Is.False);
        Assert.That(input.AsSpan(emptyValue.NameStart, emptyValue.NameLength).ToString(), Is.EqualTo("Empty"));
        Assert.That(emptyValue.HasValue, Is.True);
        Assert.That(emptyValue.ValueLength, Is.Zero);
    }

    [Test]
    public void AttributeScannerPreservesEmptySegments()
    {
        string input = ";Path=/";
        CookieScanResult empty = CookieSpanScanner.ScanAttributeSegment(input.AsSpan(), 0, input.Length);

        Assert.That(empty.Success, Is.True);
        Assert.That(empty.SegmentLength, Is.Zero);
        Assert.That(empty.NameLength, Is.Zero);
        Assert.That(empty.HasValue, Is.False);
        Assert.That(empty.NextOffset, Is.EqualTo(1));
    }

    [Test]
    public void ScannerAcceptsASegmentExactlyAtTheBoundWhenDelimiterFollows()
    {
        string input = "abc;next=value";
        CookieScanResult result = CookieSpanScanner.ScanCookiePairSegment(input.AsSpan(), 0, 3);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LimitExceeded, Is.False);
        Assert.That(input.AsSpan(result.NameStart, result.NameLength).ToString(), Is.EqualTo("abc"));
        Assert.That(result.NextOffset, Is.EqualTo(4));
    }

    [Test]
    public void ScannerReportsLimitWithoutScanningTheRemainingSegment()
    {
        string input = "abcdef;next=value";
        CookieScanResult result = CookieSpanScanner.ScanCookiePairSegment(input.AsSpan(), 0, 3);

        Assert.That(result.Success, Is.False);
        Assert.That(result.LimitExceeded, Is.True);
        Assert.That(result.SegmentStart, Is.Zero);
        Assert.That(result.SegmentLength, Is.EqualTo(3));
        Assert.That(input.AsSpan(result.NameStart, result.NameLength).ToString(), Is.EqualTo("abc"));
        Assert.That(result.NextOffset, Is.EqualTo(3));
    }

    [TestCase(-1, 1)]
    [TestCase(4, 1)]
    [TestCase(0, -1)]
    public void ScannerReportsInvalidBounds(int start, int maximumCharacters)
    {
        CookieScanResult result = CookieSpanScanner.ScanAttributeSegment("abc".AsSpan(), start, maximumCharacters);

        Assert.That(result.Success, Is.False);
        Assert.That(result.InvalidBounds, Is.True);
        Assert.That(result.LimitExceeded, Is.False);
    }
}
