using System.Text;
using email;

namespace email.Tests;

public class EmailFeedParserTests
{
    [Test]
    public void EveryTwoChunkBoundaryMatchesWholeParsingForNestedMime()
    {
        byte[] input = Bytes("Subject: outer\r\nContent-Type: multipart/mixed; boundary=edge\r\n\r\npre\r\n--edge\r\nContent-Transfer-Encoding: base64\r\n\r\nYWJj\r\n--edge\r\nContent-Type: message/rfc822\r\n\r\nSubject: nested\r\n\r\ninside\r\n--edge--\r\nepi");
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailParseResult expected = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);

        int split = 0;
        while (split <= input.Length)
        {
            EmailFeedParser parser = new EmailFeedParserOperations().CreateWithOptions(options);
            parser.Feed(new ReadOnlySpan<byte>(input, 0, split));
            parser.Feed(new ReadOnlySpan<byte>(input, split, input.Length - split));
            EmailParseResult actual = parser.Complete();

            AssertEquivalent(actual, expected, "split " + split);
            Assert.That(parser.GetResult(), Is.SameAs(actual), "result " + split);
            parser.Close();
            split++;
        }
    }

    [Test]
    public void SingleByteFeedsMatchWholeParsingAndPreserveTheSource()
    {
        byte[] input = Bytes("From: sender@example.test\r\nContent-Type: text/plain\r\n\r\nfragmented body");
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailParseResult expected = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
        EmailFeedParser parser = new EmailFeedParserOperations().CreateWithOptions(options);

        int index = 0;
        while (index < input.Length)
        {
            parser.Feed(new ReadOnlySpan<byte>(input, index, 1));
            index++;
        }

        EmailParseResult actual = parser.Complete();

        AssertEquivalent(actual, expected, "single-byte feeds");
        Assert.That(actual.GetMessage()!.HasSourceInfo(), Is.True);
        Assert.That(actual.GetMessage()!.GetSourceInfo()!.GetContent()!.Materialize(input.Length), Is.EqualTo(input));
        parser.Close();
    }

    [Test]
    public void ResetDiscardsPriorStateAndAllowsACompletelyIndependentResult()
    {
        EmailFeedParser parser = new EmailFeedParserOperations().CreateWith();
        Assert.That(parser.HasResult(), Is.False);
        Assert.Throws<InvalidOperationException>(() => parser.GetResult());
        parser.Feed(Bytes("Subject: first\r\n\r\none"));
        EmailParseResult first = parser.Complete();

        Assert.That(parser.IsComplete(), Is.True);
        Assert.That(parser.GetReceivedByteCount(), Is.EqualTo(21));
        Assert.That(Text(first.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(10)), Is.EqualTo("one"));
        Assert.Throws<InvalidOperationException>(() => parser.Feed(Bytes("more")));
        Assert.Throws<InvalidOperationException>(() => parser.Complete());

        parser.Reset();
        Assert.That(parser.IsComplete(), Is.False);
        Assert.That(parser.HasResult(), Is.False);
        Assert.That(parser.GetReceivedByteCount(), Is.Zero);
        parser.Feed(Bytes("Subject: second\r\n\r\ntwo"));
        EmailParseResult second = parser.Complete();

        Assert.That(Text(second.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(10)), Is.EqualTo("two"));
        Assert.That(second.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetUnfoldedValue(), Is.EqualTo(Bytes(" second")));
        parser.Close();
    }

    [Test]
    public void FragmentedInputSpoolsAboveThresholdAndResetAndCloseDeleteStorage()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
            EmailStorageOptions storage = options.GetStorageOptions();
            storage.SetTemporaryDirectory(directory);
            storage.GetLimits().SetMemoryThresholdBytes(4);
            options.SetStorageOptions(storage);
            EmailFeedParser parser = new EmailFeedParserOperations().CreateWithOptions(options);
            parser.Feed(Bytes("Sub"));
            Assert.That(Directory.GetFiles(directory), Is.Empty);
            parser.Feed(Bytes("ject: spooled\r\n\r\nbody"));
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));

            EmailParseResult result = parser.Complete();
            Assert.That(Text(result.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(10)), Is.EqualTo("body"));
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));

            parser.Reset();
            Assert.That(Directory.GetFiles(directory), Is.Empty);
            parser.Feed(Bytes("\r\nsecond body"));
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
            parser.Close();
            parser.Close();
            Assert.That(Directory.GetFiles(directory), Is.Empty);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MessageLimitFailureDoesNotAppendTheRejectedFragment()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxMessageBytes(4);
        options.SetLimits(limits);
        EmailFeedParser parser = new EmailFeedParserOperations().CreateWithOptions(options);
        parser.Feed(Bytes("abc"));

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => parser.Feed(Bytes("de")));

        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxMessageBytes"));
        Assert.That(exception.GetObserved(), Is.EqualTo(5));
        Assert.That(parser.GetReceivedByteCount(), Is.EqualTo(3));
        Assert.That(parser.Complete().GetSourceByteCount(), Is.EqualTo(3));
        parser.Close();
    }

    [Test]
    public void ClosedParserRejectsOperationsAndCloseIsIdempotent()
    {
        EmailFeedParser parser = new EmailFeedParserOperations().CreateWith();
        parser.Feed(new byte[0]);
        parser.Close();
        parser.Close();

        Assert.Throws<ObjectDisposedException>(() => parser.Feed(Bytes("x")));
        Assert.Throws<ObjectDisposedException>(() => parser.Complete());
        Assert.Throws<ObjectDisposedException>(() => parser.Reset());
        Assert.Throws<ObjectDisposedException>(() => parser.GetResult());
        Assert.Throws<ObjectDisposedException>(() => parser.HasResult());
        Assert.Throws<ObjectDisposedException>(() => parser.IsComplete());
        Assert.Throws<ObjectDisposedException>(() => parser.GetReceivedByteCount());
    }

    static void AssertEquivalent(EmailParseResult actual, EmailParseResult expected, string label)
    {
        Assert.That(actual.IsComplete(), Is.EqualTo(expected.IsComplete()), label);
        Assert.That(actual.IsSuccess(), Is.EqualTo(expected.IsSuccess()), label);
        Assert.That(actual.GetConsumedByteCount(), Is.EqualTo(expected.GetConsumedByteCount()), label);
        Assert.That(actual.GetSourceByteCount(), Is.EqualTo(expected.GetSourceByteCount()), label);
        AssertIssues(actual.GetIssues(), expected.GetIssues(), label);
        Assert.That(actual.HasMessage(), Is.EqualTo(expected.HasMessage()), label);
        if (actual.HasMessage())
        {
            AssertMessage(actual.GetMessage()!, expected.GetMessage()!, label);
        }
    }

    static void AssertMessage(EmailMessage actual, EmailMessage expected, string label)
    {
        AssertHeaders(actual.GetHeaders()!, expected.GetHeaders()!, label);
        Assert.That(actual.HasBody(), Is.EqualTo(expected.HasBody()), label);
        Assert.That(actual.HasSourceInfo(), Is.EqualTo(expected.HasSourceInfo()), label);
        if (actual.HasSourceInfo())
        {
            Assert.That(actual.GetSourceInfo()!.GetMessageRange()!.EqualsRange(expected.GetSourceInfo()!.GetMessageRange()!), Is.True, label);
        }

        if (actual.HasBody())
        {
            AssertEntity(actual.GetBody()!, expected.GetBody()!, label);
        }
    }

    static void AssertEntity(MimeEntity actual, MimeEntity expected, string label)
    {
        Assert.That(actual.GetKind(), Is.EqualTo(expected.GetKind()), label);
        Assert.That(actual.GetMediaType()!.GetTopLevelType(), Is.EqualTo(expected.GetMediaType()!.GetTopLevelType()), label);
        Assert.That(actual.GetMediaType()!.GetSubtype(), Is.EqualTo(expected.GetMediaType()!.GetSubtype()), label);
        Assert.That(actual.GetEffectiveTransferEncodingName(), Is.EqualTo(expected.GetEffectiveTransferEncodingName()), label);
        AssertHeaders(actual.GetHeaders()!, expected.GetHeaders()!, label);
        Assert.That(actual.GetSourceRange()!.EqualsRange(expected.GetSourceRange()!), Is.True, label);
        AssertContent(actual.HasRawContent(), actual.GetRawContent()!, expected.HasRawContent(), expected.GetRawContent()!, label);
        AssertContent(actual.HasDecodedContent(), actual.GetDecodedContent()!, expected.HasDecodedContent(), expected.GetDecodedContent()!, label);
        AssertContent(actual.HasPreamble(), actual.GetPreamble()!, expected.HasPreamble(), expected.GetPreamble()!, label);
        AssertContent(actual.HasEpilogue(), actual.GetEpilogue()!, expected.HasEpilogue(), expected.GetEpilogue()!, label);
        Assert.That(actual.GetChildCount(), Is.EqualTo(expected.GetChildCount()), label);
        int index = 0;
        while (index < actual.GetChildCount())
        {
            AssertEntity(actual.GetChildAt(index), expected.GetChildAt(index), label);
            index++;
        }

        Assert.That(actual.HasNestedMessage(), Is.EqualTo(expected.HasNestedMessage()), label);
        if (actual.HasNestedMessage())
        {
            AssertMessage(actual.GetNestedMessage()!, expected.GetNestedMessage()!, label);
        }
    }

    static void AssertHeaders(EmailHeaders actual, EmailHeaders expected, string label)
    {
        Assert.That(actual.GetCount(), Is.EqualTo(expected.GetCount()), label);
        int index = 0;
        while (index < actual.GetCount())
        {
            Assert.That(actual.GetAt(index).GetName(), Is.EqualTo(expected.GetAt(index).GetName()), label);
            Assert.That(actual.GetAt(index).GetRawFieldBytes(), Is.EqualTo(expected.GetAt(index).GetRawFieldBytes()), label);
            index++;
        }
    }

    static void AssertContent(bool actualHas, EmailContent actual, bool expectedHas, EmailContent expected, string label)
    {
        Assert.That(actualHas, Is.EqualTo(expectedHas), label);
        if (actualHas)
        {
            Assert.That(actual.GetLength(), Is.EqualTo(expected.GetLength()), label);
            Assert.That(actual.Materialize(1000000), Is.EqualTo(expected.Materialize(1000000)), label);
        }
    }

    static void AssertIssues(EmailIssueList actual, EmailIssueList expected, string label)
    {
        Assert.That(actual.GetCount(), Is.EqualTo(expected.GetCount()), label);
        int index = 0;
        while (index < actual.GetCount())
        {
            Assert.That(actual.GetAt(index).GetCode(), Is.EqualTo(expected.GetAt(index).GetCode()), label);
            Assert.That(actual.GetAt(index).GetSeverity(), Is.EqualTo(expected.GetAt(index).GetSeverity()), label);
            index++;
        }
    }

    static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "email-feed-parser-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
