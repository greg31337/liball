using System.Text;
using email;

namespace email.Tests;

public class EmailMultipartParserTests
{
    [Test]
    public void ParsesPreambleChildrenAndEpilogueAsExactLazySourceSlices()
    {
        const string source = "Content-Type: multipart/mixed; boundary=outer\r\n\r\npreamble\r\n--outer\r\nContent-Type: text/plain\r\nX-Part: one\r\n\r\nfirst\r\n--outer\r\nContent-Type: application/octet-stream\r\n\r\nsecond\r\n--outer--\r\nepilogue";
        byte[] bytes = Bytes(source);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(bytes);
        MimeEntity entity = result.GetEntity()!;
        MimeEntity first = entity!.GetChildAt(0);
        MimeEntity second = entity.GetChildAt(1);

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(entity.GetChildCount(), Is.EqualTo(2));
        Assert.That(entity.GetPreamble()!.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(entity.GetEpilogue()!.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(Text(entity.GetPreamble()!.Materialize(100)), Is.EqualTo("preamble"));
        Assert.That(Text(entity.GetEpilogue()!.Materialize(100)), Is.EqualTo("epilogue"));
        Assert.That(Text(first.GetRawContent()!.Materialize(100)), Is.EqualTo("first"));
        Assert.That(Text(second.GetRawContent()!.Materialize(100)), Is.EqualTo("second"));
        Assert.That(first.GetHeaders()!.Contains("X-Part"), Is.True);
        Assert.That(first.GetParent(), Is.SameAs(entity));
        Assert.That(second.GetParent(), Is.SameAs(entity));

        int firstStart = source.IndexOf("Content-Type: text/plain", StringComparison.Ordinal);
        int firstEnd = source.IndexOf("\r\n--outer\r\n", firstStart, StringComparison.Ordinal);
        Assert.That(first.GetSourceRange()!.EqualsRange(new EmailRange(firstStart, firstEnd - firstStart)), Is.True);

        bytes[source.IndexOf("preamble", StringComparison.Ordinal)] = (byte)'P';
        bytes[source.LastIndexOf("epilogue", StringComparison.Ordinal)] = (byte)'E';
        Assert.That(Text(entity.GetPreamble()!.Materialize(100)), Is.EqualTo("Preamble"));
        Assert.That(Text(entity.GetEpilogue()!.Materialize(100)), Is.EqualTo("Epilogue"));
    }

    [Test]
    public void RecursivelyParsesNestedMultipartTrees()
    {
        const string source = "Content-Type: multipart/mixed; boundary=outer\r\n\r\n--outer\r\nContent-Type: multipart/alternative; boundary=inner\r\n\r\ninner preamble\r\n--inner\r\nContent-Type: text/html\r\n\r\n<p>deep</p>\r\n--inner--\r\ninner epilogue\r\n--outer--\r\n";

        MimeEntity root = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!;
        MimeEntity nested = root!.GetChildAt(0);
        MimeEntity leaf = nested.GetChildAt(0);

        Assert.That(root.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(nested.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(leaf.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(leaf.GetMediaType()!.IsType("text", "html"), Is.True);
        Assert.That(Text(leaf.GetRawContent()!.Materialize(100)), Is.EqualTo("<p>deep</p>"));
        Assert.That(Text(nested.GetPreamble()!.Materialize(100)), Is.EqualTo("inner preamble"));
        Assert.That(Text(nested.GetEpilogue()!.Materialize(100)), Is.EqualTo("inner epilogue"));
        Assert.That(nested.GetParent(), Is.SameAs(root));
        Assert.That(leaf.GetParent(), Is.SameAs(nested));
    }

    [Test]
    public void MissingClosingDelimiterRetainsThePartialTreeAndMakesResultsIncomplete()
    {
        const string entitySource = "Content-Type: multipart/mixed; boundary=b\r\n\r\npreamble\r\n--b\r\n\r\nbody";
        const string messageSource = "Subject: partial\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\npreamble\r\n--b\r\n\r\nbody";

        EmailEntityParseResult entityResult = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(entitySource));
        EmailParseResult messageResult = new EmailParser().ParseMessageWithInputUsingValueArray(Bytes(messageSource));

        Assert.That(entityResult.IsComplete(), Is.False);
        Assert.That(entityResult.HasEntity(), Is.True);
        Assert.That(entityResult.GetIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
        Assert.That(entityResult.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(entityResult.GetEntity()!.GetChildAt(0).GetContent()!.Materialize(100)), Is.EqualTo("body"));
        Assert.That(entityResult.GetEntity()!.HasEpilogue(), Is.False);
        Assert.That(messageResult.IsComplete(), Is.False);
        Assert.That(messageResult.HasMessage(), Is.True);
        Assert.That(messageResult.GetMessage()!.GetBody()!.GetChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void BoundaryPrefixCollisionsRemainInsideTheChildBody()
    {
        const string source = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nbefore\r\n--b-extra\r\nafter\r\n--b--\r\n";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetEntity()!.GetChildAt(0).GetContent()!.Materialize(100)), Is.EqualTo("before\r\n--b-extra\r\nafter"));
    }

    [Test]
    public void ForensicModeRecoversQuotedDelimiterLinesDeterministically()
    {
        const string source = "Content-Type: multipart/mixed; boundary=abc\r\n\r\n--\"abc\"\r\n\r\nrecovered\r\n--\"abc\"--\r\nepilogue";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateForensic());

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
        Assert.That(result.GetEntity()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetEntity()!.GetChildAt(0).GetContent()!.Materialize(100)), Is.EqualTo("recovered"));
        Assert.That(Text(result.GetEntity()!.GetEpilogue()!.Materialize(100)), Is.EqualTo("epilogue"));
    }

    [Test]
    public void MissingBoundaryRetainsTheBodyAsPreamble()
    {
        const string source = "Content-Type: multipart/mixed\r\n\r\nunparsed body";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.GetEntity()!.GetChildCount(), Is.Zero);
        Assert.That(Text(result.GetEntity()!.GetPreamble()!.Materialize(100)), Is.EqualTo("unparsed body"));
        Assert.That(result.GetEntity()!.HasEpilogue(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
    }

    [Test]
    public void ClosingDelimiterBeforeAnyChildIsRetainedAsAnIncompleteEmptyContainer()
    {
        const string source = "Content-Type: multipart/mixed; boundary=b\r\n\r\npreamble\r\n--b--\r\nepilogue";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.GetEntity()!.GetChildCount(), Is.Zero);
        Assert.That(Text(result.GetEntity()!.GetPreamble()!.Materialize(100)), Is.EqualTo("preamble"));
        Assert.That(Text(result.GetEntity()!.GetEpilogue()!.Materialize(100)), Is.EqualTo("epilogue"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
    }

    [Test]
    public void EnforcesTheSharedPartLimitAcrossMultipartChildren()
    {
        const string source = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\none\r\n--b\r\n\r\ntwo\r\n--b--\r\n";
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxPartCount(1);
        options.SetLimits(limits);
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetReturnPartialResults(false);
        options.SetCompatibilityOptions(compatibility);

        EmailLimitException error = Assert.Throws<EmailLimitException>(() => new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), options));

        Assert.That(error.GetLimitName(), Is.EqualTo("MaxPartCount"));
        Assert.That(error.GetLimit(), Is.EqualTo(1));
        Assert.That(error.GetObserved(), Is.EqualTo(2));
    }

    [Test]
    public void EnforcesTheNestingDepthBeforeBuildingAnOverdeepChild()
    {
        const string source = "Content-Type: multipart/mixed; boundary=outer\r\n\r\n--outer\r\nContent-Type: multipart/mixed; boundary=inner\r\n\r\n--inner\r\n\r\ndeep\r\n--inner--\r\n\r\n--outer--\r\n";
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxNestingDepth(1);
        options.SetLimits(limits);
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetReturnPartialResults(false);
        options.SetCompatibilityOptions(compatibility);

        EmailLimitException error = Assert.Throws<EmailLimitException>(() => new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), options));

        Assert.That(error.GetLimitName(), Is.EqualTo("MaxNestingDepth"));
        Assert.That(error.GetLimit(), Is.EqualTo(1));
        Assert.That(error.GetObserved(), Is.EqualTo(2));
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}
