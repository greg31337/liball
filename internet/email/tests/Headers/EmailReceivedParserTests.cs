using System.Text;
using email;

namespace email.Tests;

public class EmailReceivedParserTests
{
    [Test]
    public void ParsesStandardClausesInOrderAndPreservesTheCompleteValue()
    {
        const string input = "from mail.example (mail.example [192.0.2.1]) by mx.example via TCP with ESMTPS id abc123@example for <user@example.net>; Fri, 21 Nov 1997 09:55:06 -0600";

        EmailReceivedParseResult result = Parse(input, EmailReceivedParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        EmailReceived value = result.GetValue()!;
        Assert.That(value!.GetClauseCount(), Is.EqualTo(6));
        Assert.That(value.GetClause(0).GetKind(), Is.EqualTo(EmailReceivedClauseKind.From));
        Assert.That(value.GetClause(1).GetKind(), Is.EqualTo(EmailReceivedClauseKind.By));
        Assert.That(value.GetClause(2).GetKind(), Is.EqualTo(EmailReceivedClauseKind.Via));
        Assert.That(value.GetClause(3).GetKind(), Is.EqualTo(EmailReceivedClauseKind.With));
        Assert.That(value.GetClause(4).GetKind(), Is.EqualTo(EmailReceivedClauseKind.Id));
        Assert.That(value.GetClause(5).GetKind(), Is.EqualTo(EmailReceivedClauseKind.For));
        Assert.That(value.GetFrom()!.GetValue(), Is.EqualTo("mail.example (mail.example [192.0.2.1])"));
        Assert.That(value.GetBy()!.GetValue(), Is.EqualTo("mx.example"));
        Assert.That(value.GetVia()!.GetValue(), Is.EqualTo("TCP"));
        Assert.That(value.GetWith()!.GetValue(), Is.EqualTo("ESMTPS"));
        Assert.That(value.GetId()!.GetValue(), Is.EqualTo("abc123@example"));
        Assert.That(value.GetFor()!.GetValue(), Is.EqualTo("<user@example.net>"));
        Assert.That(value.HasDateDelimiter(), Is.True);
        Assert.That(value.HasDate(), Is.True);
        Assert.That(value.GetDate()!.GetYear(), Is.EqualTo(1997));
        Assert.That(Ascii(value.GetTraceBytes()), Is.EqualTo(input[..input.IndexOf(';')]));
        Assert.That(Ascii(value.GetDateBytes()), Is.EqualTo(" Fri, 21 Nov 1997 09:55:06 -0600"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void PreservesRegisteredExtensionClausesVerbatim()
    {
        const string input = "FROM origin.example BY relay.example WITH ESMTP X-Trace \"opaque value\" X-Queue q.17; 1 Jan 2024 00:00 +0000";

        EmailReceivedParseResult result = Parse(input, EmailReceivedParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        EmailReceived value = result.GetValue()!;
        List<EmailReceivedClause> extensions = value!.GetClausesByKind(EmailReceivedClauseKind.Extension);
        Assert.That(extensions, Has.Count.EqualTo(2));
        Assert.That(extensions[0].GetOriginalName(), Is.EqualTo("X-Trace"));
        Assert.That(extensions[0].GetNormalizedName(), Is.EqualTo("x-trace"));
        Assert.That(extensions[0].GetValue(), Is.EqualTo("\"opaque value\""));
        Assert.That(Ascii(extensions[0].GetRawBytes()), Is.EqualTo("X-Trace \"opaque value\" "));
        Assert.That(Ascii(extensions[0].GetRawValueBytes()), Is.EqualTo(" \"opaque value\" "));
        Assert.That(extensions[1].GetOriginalName(), Is.EqualTo("X-Queue"));
        Assert.That(extensions[1].GetValue(), Is.EqualTo("q.17"));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void IgnoresSemicolonsInsideCommentsQuotedStringsAndDomainLiterals()
    {
        const string input = "from [odd;domain] (comment; still trace) by relay X-Note \"a;b\"; 1 Jan 2024 00:00 +0000";

        EmailReceivedParseResult result = Parse(input, EmailReceivedParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue()!.GetClauseCount(), Is.EqualTo(3));
        Assert.That(result.GetValue()!.GetFrom()!.GetValue(), Is.EqualTo("[odd;domain] (comment; still trace)"));
        Assert.That(result.GetValue()!.GetClausesByKind(EmailReceivedClauseKind.Extension)[0].GetValue(), Is.EqualTo("\"a;b\""));
        Assert.That(result.GetValue()!.GetDate()!.GetYear(), Is.EqualTo(2024));
        Assert.That(Ascii(result.GetValue()!.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void UnfoldsConvenienceValuesWithoutChangingRawClauseBytes()
    {
        const string input = "from mail.example\r\n\t(comment) by mx.example; 1 Jan 2024 00:00 +0000";

        EmailReceived value = Parse(input, EmailReceivedParseMode.Strict).GetValue()!;

        Assert.That(value!.GetFrom()!.GetValue(), Is.EqualTo("mail.example (comment)"));
        Assert.That(Ascii(value.GetFrom()!.GetRawBytes()), Is.EqualTo("from mail.example\r\n\t(comment) "));
        Assert.That(Ascii(value.GetRawBytes()), Is.EqualTo(input));
    }

    [Test]
    public void RetainsMalformedClauseOrderWithModeSpecificSeverity()
    {
        const string input = "by relay from origin; 1 Jan 2024 00:00 +0000";

        EmailReceivedParseResult tolerant = Parse(input, EmailReceivedParseMode.Tolerant);
        EmailReceivedParseResult strict = Parse(input, EmailReceivedParseMode.Strict);

        Assert.That(tolerant.GetValue()!.GetClauseCount(), Is.EqualTo(2));
        Assert.That(tolerant.GetValue()!.GetClause(1).GetKind(), Is.EqualTo(EmailReceivedClauseKind.From));
        Assert.That(tolerant.GetValue()!.GetClause(1).IsMalformed(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ReceivedMalformed), Is.True);
        Assert.That(tolerant.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetValue()!.GetClauseCount(), Is.EqualTo(2));
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.ReceivedMalformed), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void RetainsTraceWhenTimestampDelimiterIsMissing()
    {
        const string input = "from origin by relay with ESMTP";

        EmailReceivedParseResult tolerant = Parse(input, EmailReceivedParseMode.Tolerant);
        EmailReceivedParseResult strict = Parse(input, EmailReceivedParseMode.Strict);

        Assert.That(tolerant.HasValue(), Is.True);
        Assert.That(tolerant.GetValue()!.HasDateDelimiter(), Is.False);
        Assert.That(tolerant.GetValue()!.HasDate(), Is.False);
        Assert.That(Ascii(tolerant.GetValue()!.GetTraceBytes()), Is.EqualTo(input));
        Assert.That(Ascii(tolerant.GetValue()!.GetDateBytes()), Is.Empty);
        Assert.That(Ascii(tolerant.GetValue()!.GetRawBytes()), Is.EqualTo(input));
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ReceivedDateMissing), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.HasValue(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void RetainsMalformedTimestampBytesWithoutInventingADate()
    {
        const string input = "from origin by relay; definitely-not-a-date";

        EmailReceivedParseResult result = Parse(input, EmailReceivedParseMode.Tolerant);

        Assert.That(result.HasValue(), Is.True);
        Assert.That(result.GetValue()!.HasDateDelimiter(), Is.True);
        Assert.That(result.GetValue()!.HasDate(), Is.False);
        Assert.That(Ascii(result.GetValue()!.GetDateBytes()), Is.EqualTo(" definitely-not-a-date"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DateMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ReceivedDateMalformed), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void UsesCallerSourcePositionForClausesAndTimestampIssues()
    {
        EmailReceivedParserOptions options = new EmailReceivedParserOptions();
        options.SetMode(EmailReceivedParseMode.Tolerant);
        EmailReceivedParseResult result = new EmailReceivedParser().ParseWithInputAndOptionsAndInitialPosition(Encoding.ASCII.GetBytes("\r\n from host by relay; bad"), options, new EmailSourcePosition(40, 7, 3));

        EmailSourcePosition from = result.GetValue()!.GetFrom()!.GetSourcePosition();
        Assert.That(from.GetByteOffset(), Is.EqualTo(43));
        Assert.That(from.GetLineNumber(), Is.EqualTo(8));
        Assert.That(from.GetColumnNumber(), Is.EqualTo(2));
        EmailIssue malformedDate = result.GetIssues().GetAll().First(issue => issue.GetCode() == EmailIssueCode.ReceivedDateMalformed);
        Assert.That(malformedDate.GetSourcePosition()!.GetByteOffset(), Is.EqualTo(62));
    }

    [Test]
    public void EnforcesInputAndClauseLimits()
    {
        EmailReceivedParserOptions inputOptions = new EmailReceivedParserOptions();
        inputOptions.SetMaximumInputBytes(5);
        EmailReceivedParseResult inputResult = new EmailReceivedParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("from host; 1 Jan 2024 00:00 +0000"), inputOptions);

        EmailReceivedParserOptions clauseOptions = new EmailReceivedParserOptions();
        clauseOptions.SetMaximumClauses(2);
        EmailReceivedParseResult clauseResult = new EmailReceivedParser().ParseWithInputAndOptions(Encoding.ASCII.GetBytes("from a by b with ESMTP; 1 Jan 2024 00:00 +0000"), clauseOptions);

        Assert.That(inputResult.HasValue(), Is.False);
        Assert.That(inputResult.HasFatalError(), Is.True);
        Assert.That(inputResult.GetIssues().ContainsCode(EmailIssueCode.ReceivedLimitExceeded), Is.True);
        Assert.That(clauseResult.HasValue(), Is.True);
        Assert.That(clauseResult.GetValue()!.GetClauseCount(), Is.EqualTo(2));
        Assert.That(clauseResult.HasFatalError(), Is.True);
        Assert.That(clauseResult.IsComplete(), Is.False);
    }

    [Test]
    public void ReturnedRawByteArraysAreDefensiveCopies()
    {
        EmailReceived value = Parse("from origin by relay; 1 Jan 2024 00:00 +0000", EmailReceivedParseMode.Strict).GetValue()!;
        byte[] raw = value!.GetRawBytes();
        byte[] clauseRaw = value.GetFrom()!.GetRawBytes();
        raw[0] = (byte)'X';
        clauseRaw[0] = (byte)'X';

        Assert.That(Ascii(value.GetRawBytes()), Does.StartWith("from"));
        Assert.That(Ascii(value.GetFrom()!.GetRawBytes()), Does.StartWith("from"));
    }

    static EmailReceivedParseResult Parse(string input, EmailReceivedParseMode mode)
    {
        return new EmailReceivedParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(input), mode);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
