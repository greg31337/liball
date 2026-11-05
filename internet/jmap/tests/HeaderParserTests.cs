using System.Text;

namespace jmap.Tests;

public class HeaderParserTests
{
    [Test]
    public void PreservesOrderRepeatedNamesAndOriginalFoldedValues()
    {
        HeaderParser parser = new HeaderParser();
        string message = "Received: first\r\nSubject: hello\r\n\tcontinued\r\nX-Test: one\r\nx-test:\ttwo\r\n\r\nbody";

        HeaderParseResult result = parser.ParseHeaders(Encoding.UTF8.GetBytes(message));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Headers.Select(header => header.Name),
                Is.EqualTo(new[] { "Received", "Subject", "X-Test", "x-test" }));
            Assert.That(result.Headers[0].Value, Is.EqualTo(" first"));
            Assert.That(result.Headers[1].Value, Is.EqualTo(" hello\r\n\tcontinued"));
            Assert.That(result.Headers[2].Value, Is.EqualTo(" one"));
            Assert.That(result.Headers[3].Value, Is.EqualTo("\ttwo"));
            Assert.That(result.Headers.Count(header => string.Equals(header.Name, "x-test", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(2));
            Assert.That(result.BodyOffset, Is.EqualTo(message.IndexOf("body", StringComparison.Ordinal)));
            Assert.That(result.HasHeaderBodySeparator, Is.True);
            Assert.That(result.HasMalformedInput, Is.False);
        }
    }

    [Test]
    public void AcceptsUtf8AndReplacesInvalidOctetsWhileDroppingNuls()
    {
        HeaderParser parser = new HeaderParser();
        List<byte> message = new List<byte>();
        message.AddRange(Encoding.UTF8.GetBytes("Subject: café\r\nX-Bad: value "));
        message.Add(195);
        message.Add(40);
        message.Add(0);
        message.AddRange(Encoding.ASCII.GetBytes(" end\r\n\r\n"));

        HeaderParseResult result = parser.ParseHeaders(message.ToArray());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Headers[0].Value, Is.EqualTo(" café"));
            Assert.That(result.Headers[1].Value, Is.EqualTo(" value �( end"));
            Assert.That(result.Headers[1].Value!.IndexOf('\0'), Is.EqualTo(-1));
            Assert.That(result.HasMalformedInput, Is.True);
            Assert.That(result.IsLimitExceeded, Is.False);
        }
    }

    [Test]
    public void ToleratesMalformedLinesObsoleteNameSpacingAndBareLineEndings()
    {
        HeaderParser parser = new HeaderParser();
        string message = " orphan continuation\r\nBroken line\r\nGood \t: yes\n next\n\nbody";

        HeaderParseResult result = parser.ParseHeaders(Encoding.UTF8.GetBytes(message));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Headers, Has.Count.EqualTo(1));
            Assert.That(result.Headers[0].Name, Is.EqualTo("Good"));
            Assert.That(result.Headers[0].Value, Is.EqualTo(" yes\n next"));
            Assert.That(result.HasMalformedInput, Is.True);
            Assert.That(result.HasHeaderBodySeparator, Is.True);
            Assert.That(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(message), result.BodyOffset, Encoding.UTF8.GetByteCount(message) - result.BodyOffset), Is.EqualTo("body"));
        }
    }

    [Test]
    public void EnforcesPhysicalLineAndHeaderCountLimitsWithoutLosingBodyOffset()
    {
        HeaderParser parser = new HeaderParser();
        parser.MaximumLineBytes = 12;
        parser.MaximumHeaderCount = 2;
        string message = "Long: 123456789\r\nA: 1\r\nB: 2\r\nC: 3\r\n\r\nbody";

        HeaderParseResult result = parser.ParseHeaders(Encoding.ASCII.GetBytes(message));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Headers.Select(header => header.Name), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(result.HasMalformedInput, Is.True);
            Assert.That(result.IsLimitExceeded, Is.True);
            Assert.That(result.BodyOffset, Is.EqualTo(message.IndexOf("body", StringComparison.Ordinal)));
        }
    }

    [Test]
    public void FinalizesHeadersWhenTheMessageEndsWithoutABodySeparator()
    {
        HeaderParser parser = new HeaderParser();
        byte[] message = Encoding.ASCII.GetBytes("One: 1\r\nTwo: 2");

        HeaderParseResult result = parser.ParseHeaders(message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Headers.Select(header => header.Name), Is.EqualTo(new[] { "One", "Two" }));
            Assert.That(result.Headers[1].Value, Is.EqualTo(" 2"));
            Assert.That(result.BodyOffset, Is.EqualTo(message.Length));
            Assert.That(result.HasHeaderBodySeparator, Is.False);
            Assert.That(result.HasMalformedInput, Is.False);
        }
    }
}
