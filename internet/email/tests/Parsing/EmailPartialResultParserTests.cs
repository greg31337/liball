using System.Text;
using email;

namespace email.Tests;

public class EmailPartialResultParserTests
{
    [TestCase("fields")]
    [TestCase("bytes")]
    [TestCase("line")]
    public void RecoverableHeaderLimitsRetainEveryCompletePrefixField(string limitKind)
    {
        const string source = "A:1\r\nB:12345\r\n\r\nbody";
        EmailParseOptions options = HeaderLimitOptions(limitKind);

        EmailParseResult first = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), options);
        EmailParseResult second = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), options);

        Assert.That(first.IsComplete(), Is.False);
        Assert.That(first.HasMessage(), Is.True);
        Assert.That(first.GetMessage()!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(first.GetMessage()!.GetHeaders()!.GetAt(0).GetName(), Is.EqualTo("A"));
        Assert.That(first.GetMessage()!.HasBody(), Is.False);
        Assert.That(first.GetMessage()!.GetSourceInfo()!.GetHeaderBlockRange()!.GetLength(), Is.EqualTo(5));
        Assert.That(first.GetIssues().ContainsCode(EmailIssueCode.LimitExceeded), Is.True);
        Assert.That(first.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
        AssertEquivalentIssue(first.GetIssues().GetAt(0), second.GetIssues().GetAt(0));
        Assert.That(second.GetMessage()!.GetHeaders()!.GetAt(0).GetRawFieldBytes(), Is.EqualTo(first.GetMessage()!.GetHeaders()!.GetAt(0).GetRawFieldBytes()));
    }

    [Test]
    public void HeaderBlockParserCanExplicitlyReturnALimitReachedPrefix()
    {
        EmailHeaderBlockParserOptions options = new EmailHeaderBlockParserOptions();
        EmailParsingLimits limits = options.GetLimits();
        limits.SetMaxHeaderFieldCount(1);
        options.SetLimits(limits);
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetReturnPartialResults(true);
        options.SetCompatibilityOptions(compatibility);

        EmailHeaderBlockParseResult result = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(Bytes("A:1\r\nB:2\r\n\r\n"), options, new EmailSourcePosition(0, 1, 1), true);

        Assert.That(result.WasLimitReached(), Is.True);
        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.GetConsumedByteCount(), Is.EqualTo(5));
        Assert.That(result.GetHeaders().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.LimitExceeded), Is.True);
    }

    [Test]
    public void PartLimitReturnsTheCompleteChildPrefixAndAnIssue()
    {
        const string source = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\none\r\n--b\r\n\r\ntwo\r\n--b--\r\n";
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxPartCount(1);
        options.SetLimits(limits);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), options);

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.HasEntity(), Is.True);
        Assert.That(result.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetEntity()!.GetChildAt(0).GetRawContent()!.Materialize(10)), Is.EqualTo("one"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.LimitExceeded), Is.True);
        Assert.That(result.GetIssues().GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
    }

    [Test]
    public void DepthLimitRetainsTheOverdeepPartAsOpaqueContent()
    {
        const string source = "Content-Type: multipart/mixed; boundary=outer\r\n\r\n--outer\r\nContent-Type: multipart/mixed; boundary=inner\r\n\r\n--inner\r\n\r\ndeep\r\n--inner--\r\n\r\n--outer--\r\n";
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxNestingDepth(1);
        options.SetLimits(limits);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), options);
        MimeEntity inner = result.GetEntity()!.GetChildAt(0);
        MimeEntity opaque = inner.GetChildAt(0);

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(inner.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(opaque.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(Text(opaque.GetRawContent()!.Materialize(10)), Is.EqualTo("deep"));
        Assert.That(opaque.GetSourceRange()!.GetLength(), Is.EqualTo(6));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.LimitExceeded), Is.True);
    }

    [Test]
    public void EndOfInputReturnsTheSameCompleteMultipartPrefixForWholeAndFeedParsing()
    {
        const string source = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nretained";
        byte[] input = Bytes(source);
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailParseResult whole = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
        EmailFeedParser feed = new EmailFeedParserOperations().CreateWithOptions(options);
        int index = 0;
        while (index < input.Length)
        {
            feed.Feed(new ReadOnlySpan<byte>(input, index, 1));
            index++;
        }

        EmailParseResult fragmented = feed.Complete();

        Assert.That(whole.IsComplete(), Is.False);
        Assert.That(fragmented.IsComplete(), Is.False);
        Assert.That(whole.GetMessage()!.GetBody()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(fragmented.GetMessage()!.GetBody()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(whole.GetMessage()!.GetBody()!.GetChildAt(0).GetRawContent()!.Materialize(20)), Is.EqualTo("retained"));
        Assert.That(fragmented.GetMessage()!.GetBody()!.GetChildAt(0).GetRawContent()!.Materialize(20), Is.EqualTo(whole.GetMessage()!.GetBody()!.GetChildAt(0).GetRawContent()!.Materialize(20)));
        AssertEquivalentIssue(fragmented.GetIssues().GetAt(0), whole.GetIssues().GetAt(0));
        feed.Close();
    }

    static EmailParseOptions HeaderLimitOptions(string limitKind)
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        if (limitKind == "fields")
        {
            limits.GetParsingLimits().SetMaxHeaderFieldCount(1);
        }
        else if (limitKind == "bytes")
        {
            limits.GetParsingLimits().SetMaxHeaderBytes(5);
        }
        else
        {
            limits.GetParsingLimits().SetMaxLineBytes(5);
        }

        options.SetLimits(limits);
        return options;
    }

    static void AssertEquivalentIssue(EmailIssue actual, EmailIssue expected)
    {
        Assert.That(actual.GetCode(), Is.EqualTo(expected.GetCode()));
        Assert.That(actual.GetSeverity(), Is.EqualTo(expected.GetSeverity()));
        Assert.That(actual.GetDescription(), Is.EqualTo(expected.GetDescription()));
        Assert.That(actual.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(expected.GetSourcePosition()!.GetByteOffset()));
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
